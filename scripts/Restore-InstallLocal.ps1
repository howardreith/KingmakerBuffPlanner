[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$InstallId,
    # Root overrides exist for isolated-state testing only; each must already
    # exist, and every path this script touches stays inside the supplied
    # roots. Defaults are the machine-local lab roots.
    [string]$GameRoot,
    [string]$StateRoot,
    [string]$BackupRoot,
    [string]$StagingRoot,
    [string]$EvidenceRoot,
    # Isolated-test failure injection (review K6); refused unless every root
    # override is supplied, so it can never act on the live lab roots.
    [ValidateSet('', 'candidate-archive', 'settings-merge', 'identity', 'verify', 'record', 'record-and-reverse')]
    [string]$InjectFailureAt = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false
if ($InjectFailureAt -ne '' -and (-not $PSBoundParameters.ContainsKey('GameRoot') -or
        -not $PSBoundParameters.ContainsKey('StateRoot') -or
        -not $PSBoundParameters.ContainsKey('BackupRoot') -or
        -not $PSBoundParameters.ContainsKey('StagingRoot') -or
        -not $PSBoundParameters.ContainsKey('EvidenceRoot'))) {
    throw 'Failure injection is only permitted with fully isolated root overrides.'
}

$labRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = Get-KbpGamePath }
if ([string]::IsNullOrWhiteSpace($StateRoot)) { $StateRoot = Join-Path $labRoot 'runtime-state' }
if ([string]::IsNullOrWhiteSpace($BackupRoot)) { $BackupRoot = Join-Path $labRoot 'runtime-backups' }
if ([string]::IsNullOrWhiteSpace($StagingRoot)) { $StagingRoot = Join-Path $labRoot 'runtime-staging' }
if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) { $EvidenceRoot = Join-Path $labRoot 'runtime-evidence' }
foreach ($required in @($GameRoot, $StateRoot, $BackupRoot, $StagingRoot, $EvidenceRoot)) {
    if (-not (Test-Path -LiteralPath $required -PathType Container)) {
        throw "Required root is missing: $required"
    }
}
$gameRootFull = [IO.Path]::GetFullPath($GameRoot).TrimEnd('\')

function Assert-KbpPlannerIdentity([string]$Path, [string]$ExpectedVersion) {
    $infoPath = Join-Path $Path 'Info.json'
    $dllPath = Join-Path $Path 'KingmakerBuffPlanner.dll'
    if (-not (Test-Path -LiteralPath $infoPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
        throw "Planner folder is missing its primary files: $Path"
    }
    $info = Read-KbpJson $infoPath
    $assemblyName = [Reflection.AssemblyName]::GetAssemblyName($dllPath)
    $numericExpected = ($ExpectedVersion -split '-')[0] + '.0'
    if ($info.Id -cne 'KingmakerBuffPlanner' -or
        $info.AssemblyName -cne 'KingmakerBuffPlanner.dll' -or
        $info.Version -cne $ExpectedVersion -or
        $assemblyName.Name -cne 'KingmakerBuffPlanner' -or
        $assemblyName.Version.ToString() -cne $numericExpected) {
        throw "Planner identity/version does not match '$ExpectedVersion' at $Path."
    }
}

$installStateDir = Join-Path $StateRoot ('installations\' + $InstallId)
$installStatePath = Join-Path $installStateDir 'install.json'
if (-not (Test-Path -LiteralPath $installStatePath -PathType Leaf)) {
    throw "No installation record exists for '$InstallId': $installStatePath"
}
$install = Read-KbpJson $installStatePath
if ([string]$install.schemaVersion -ne '1') {
    throw "Unknown installation record schema: $($install.schemaVersion)"
}
if ([string]$install.status -cne 'Installed') {
    throw "Installation '$InstallId' has status '$($install.status)'; only an Installed record can roll back."
}
$backupPlanner = [IO.Path]::GetFullPath([string]$install.backupPlanner)
Assert-KbpPathWithin -Path $backupPlanner -Root $BackupRoot
$priorVersion = [string]$install.expectedPriorVersion
$mods = [string]$install.modsPath
if ([string]::IsNullOrWhiteSpace($mods)) { $mods = Join-Path $gameRootFull 'Mods' }
Assert-KbpPathWithin -Path $mods -Root $gameRootFull -AllowRoot
if (-not (Test-Path -LiteralPath $mods -PathType Container)) { throw "Live Mods directory is missing: $mods" }
$planner = Join-Path $mods 'KingmakerBuffPlanner'
if (-not (Test-Path -LiteralPath $planner -PathType Container)) {
    throw "Installed planner folder is missing: $planner"
}
Assert-KbpPlannerIdentity $planner ([string]$install.version)
if (-not (Test-Path -LiteralPath $backupPlanner -PathType Container)) {
    throw "Recorded prior backup is missing: $backupPlanner"
}
Assert-KbpPlannerIdentity $backupPlanner $priorVersion
Assert-KbpNotRunning
Assert-KbpNoUnresolvedTransaction $StateRoot

# A failed attempt keeps its evidence; a retry gets fresh owned paths.
$attempt = 1
while ($true) {
    $suffix = if ($attempt -eq 1) { '' } else { '-attempt' + $attempt }
    $rollStaging = Join-Path $StagingRoot ('rollback-' + $InstallId + $suffix)
    $rollEvidence = Join-Path $EvidenceRoot ('rollback-' + $InstallId + $suffix)
    if (-not (Test-Path -LiteralPath $rollStaging) -and -not (Test-Path -LiteralPath $rollEvidence)) { break }
    if (++$attempt -gt 20) { throw "Too many earlier rollback attempts for '$InstallId'." }
}

if (-not $PSCmdlet.ShouldProcess($planner,
        "roll installation '$InstallId' back to the preserved $priorVersion planner, keeping newer UserSettings profiles")) {
    Write-Host 'Rollback preflight PASS; WhatIf made no install, state, staging, backup, evidence, game, or mod mutation.'
    return
}

$token = [Guid]::NewGuid().ToString('N')
$lockPath = Join-Path $StateRoot 'deployment.lock'
New-KbpOwnedLock $lockPath $InstallId $token
$keepLock = $false
$swapped = $false
$removedPlanner = Join-Path $rollEvidence 'rolled-back-planner'
$stagedPlanner = Join-Path $rollStaging 'restored-planner'
$candidatePattern = 'kingmaker-buff-planner-casting-*'

function Set-KbpRollbackRecord([string]$Status, [string]$Phase, [string]$Failure) {
    $install.status = $Status
    Add-Member -InputObject $install -MemberType NoteProperty -Name rollbackPhase -Value $Phase -Force
    if (-not [string]::IsNullOrEmpty($Failure)) {
        Add-Member -InputObject $install -MemberType NoteProperty -Name failure -Value $Failure -Force
    }
    Write-KbpJsonAtomic $installStatePath $install
}

function Invoke-KbpInjectedFailure([string]$Phase) {
    if ($InjectFailureAt -ceq $Phase) { throw "injected rollback failure at $Phase" }
}

function Get-KbpContentLines([string]$Path) {
    return @(Get-KbpDirectoryManifest $Path | ForEach-Object {
        if ($_.kind -ceq 'file') { "F|$($_.path)|$($_.sha256)" } else { "D|$($_.path)" } })
}

# Review K6: whether the RESTORED binary reads casting-first candidate
# profiles is decided from that binary (its UTF-16 candidate file-name
# literal), never from a file-name prefix alone.
function Test-KbpBinaryReadsCandidateProfiles([string]$DllPath) {
    $bytes = [IO.File]::ReadAllBytes($DllPath)
    $needle = [Text.Encoding]::Unicode.GetBytes('kingmaker-buff-planner-casting-')
    $first = $needle[0]
    $limit = $bytes.Length - $needle.Length
    $index = [Array]::IndexOf($bytes, $first, 0)
    while ($index -ge 0 -and $index -le $limit) {
        $match = $true
        for ($j = 1; $j -lt $needle.Length; $j++) {
            if ($bytes[$index + $j] -ne $needle[$j]) { $match = $false; break }
        }
        if ($match) { return $true }
        if ($index + 1 -gt $limit) { break }
        $index = [Array]::IndexOf($bytes, $first, $index + 1)
    }
    return $false
}

try {
    # Phase 1 (prepare): nothing under Mods or the recorded backup changes.
    # The prior build is COPIED into staging, and every settings transition
    # happens on that copy, so a failure here leaves the install untouched
    # and the recorded backup byte-exact.
    Add-Member -InputObject $install -MemberType NoteProperty -Name rollbackEvidenceRoot `
        -Value $rollEvidence -Force
    Set-KbpRollbackRecord 'RollingBack' 'prepare' $null
    New-Item -ItemType Directory -Path $rollStaging, $rollEvidence | Out-Null
    $preservedSettings = Join-Path $rollStaging 'usersettings-preserved'
    $currentSettings = Join-Path $planner 'UserSettings'
    $preservedProfiles = @()
    if (Test-Path -LiteralPath $currentSettings -PathType Container) {
        # The owner's post-install profiles are newer edits: archive every
        # byte, verified, before any transition.
        Copy-Item -LiteralPath $currentSettings -Destination $preservedSettings -Recurse
        if (-not (Test-KbpManifestEqual (Get-KbpContentLines $currentSettings) (Get-KbpContentLines $preservedSettings))) {
            throw 'Preserved settings copy does not match the installed settings.'
        }
        $preservedProfiles = @(Get-ChildItem -LiteralPath $preservedSettings -File -Recurse |
            ForEach-Object { Get-KbpRelativePath $preservedSettings $_.FullName })
    }
    $beforeRollbackIdentity = (Get-KbpDirectoryContentIdentity $planner).directoryManifestSha256
    Set-Content -LiteralPath (Join-Path $rollEvidence 'before-rollback-identity.txt') `
        -Value $beforeRollbackIdentity -Encoding UTF8
    $backupIdentity = (Get-KbpDirectoryContentIdentity $backupPlanner).directoryManifestSha256
    Copy-Item -LiteralPath $backupPlanner -Destination $stagedPlanner -Recurse
    if ((Get-KbpDirectoryContentIdentity $stagedPlanner).directoryManifestSha256 -cne $backupIdentity) {
        throw 'Staged prior build does not match the recorded backup.'
    }
    $readsCandidates = Test-KbpBinaryReadsCandidateProfiles (Join-Path $stagedPlanner 'KingmakerBuffPlanner.dll')

    # Casting-first candidates the restored binary cannot read are
    # DEACTIVATED from BOTH sides (the newer install's and any that came
    # back with the prior backup) and archived with the evidence. A binary
    # that reads them keeps them deliberately (charter 7.3).
    Set-KbpRollbackRecord 'RollingBack' 'candidate-archive' $null
    Invoke-KbpInjectedFailure 'candidate-archive'
    $stagedSettings = Join-Path $stagedPlanner 'UserSettings'
    $candidateArchive = Join-Path $rollEvidence 'deactivated-candidate-profiles'
    $deactivatedCandidates = @()
    if (-not $readsCandidates) {
        foreach ($relative in @($preservedProfiles | Where-Object { (Split-Path -Leaf $_) -like $candidatePattern })) {
            $target = Join-Path $candidateArchive ('installed\' + $relative)
            New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
            Copy-Item -LiteralPath (Join-Path $preservedSettings $relative) -Destination $target
            $deactivatedCandidates += @('installed\' + $relative)
        }
        if (Test-Path -LiteralPath $stagedSettings -PathType Container) {
            foreach ($file in @(Get-ChildItem -LiteralPath $stagedSettings -File -Recurse |
                    Where-Object { $_.Name -like $candidatePattern })) {
                $relative = Get-KbpRelativePath $stagedSettings $file.FullName
                $target = Join-Path $candidateArchive ('prior\' + $relative)
                New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
                Move-Item -LiteralPath $file.FullName -Destination $target
                $deactivatedCandidates += @('prior\' + $relative)
            }
        }
        $preservedProfiles = @($preservedProfiles | Where-Object {
            (Split-Path -Leaf $_) -notlike $candidatePattern })
    }

    # Merge the preserved newer profiles over the staged prior build.
    Set-KbpRollbackRecord 'RollingBack' 'settings-merge' $null
    Invoke-KbpInjectedFailure 'settings-merge'
    foreach ($relative in $preservedProfiles) {
        $source = Join-Path $preservedSettings $relative
        $target = Join-Path $stagedSettings $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        # Newer (post-install) edits win over the older backup's files.
        Copy-Item -LiteralPath $source -Destination $target -Force
    }

    Set-KbpRollbackRecord 'RollingBack' 'identity' $null
    Invoke-KbpInjectedFailure 'identity'
    Assert-KbpPlannerIdentity $stagedPlanner $priorVersion
    $stagedIdentity = (Get-KbpDirectoryContentIdentity $stagedPlanner).directoryManifestSha256

    # Phase 2 (swap): the record says so BEFORE Mods changes, so an
    # interruption is never recorded as the installed version.
    Set-KbpRollbackRecord 'RollingBack' 'swap' $null
    Move-Item -LiteralPath $planner -Destination $removedPlanner
    try {
        Move-Item -LiteralPath $stagedPlanner -Destination $planner
    }
    catch {
        # Put the tested build back rather than leaving Mods empty.
        Move-Item -LiteralPath $removedPlanner -Destination $planner
        throw
    }
    $swapped = $true

    Set-KbpRollbackRecord 'RollingBack' 'verify' $null
    Invoke-KbpInjectedFailure 'verify'
    Assert-KbpPlannerIdentity $planner $priorVersion
    if ((Get-KbpDirectoryContentIdentity $planner).directoryManifestSha256 -cne $stagedIdentity) {
        throw 'Mods planner does not match the verified staged prior build.'
    }
    # Unrelated mods may have legitimately changed since the install (the
    # owner updates their own mods); that never blocks a planner rollback,
    # but it is reported rather than silently ignored.
    $afterOther = @(Get-KbpDirectoryManifest $mods |
        Where-Object { $_.path -cne 'KingmakerBuffPlanner' -and
            -not ($_.path).StartsWith('KingmakerBuffPlanner\', [StringComparison]::OrdinalIgnoreCase) })
    $beforeOtherJson = @($install.beforeOtherModsManifest) | ConvertTo-Json -Depth 8 -Compress
    $afterOtherJson = $afterOther | ConvertTo-Json -Depth 8 -Compress
    if ($beforeOtherJson -cne $afterOtherJson) {
        Write-Host ('WARNING: other mods changed since the install was recorded; ' +
            'the planner rollback proceeded and only KingmakerBuffPlanner was replaced.')
    }

    Invoke-KbpInjectedFailure 'record'
    Invoke-KbpInjectedFailure 'record-and-reverse'
    Add-Member -InputObject $install -MemberType NoteProperty -Name rolledBackAtUtc `
        -Value ([DateTime]::UtcNow.ToString('o')) -Force
    Add-Member -InputObject $install -MemberType NoteProperty -Name rollbackPreservedProfiles `
        -Value $preservedProfiles -Force
    Add-Member -InputObject $install -MemberType NoteProperty -Name rollbackDeactivatedCandidateProfiles `
        -Value $deactivatedCandidates -Force
    Add-Member -InputObject $install -MemberType NoteProperty -Name rollbackTargetReadsCandidateProfiles `
        -Value $readsCandidates -Force
    Set-KbpRollbackRecord 'RolledBack' 'complete' $null
    $summary = 'Install rollback: PASS=1 FAIL=0 installId={0} restoredVersion={1} preservedProfiles={2} deactivatedCandidateProfiles={3} evidence={4}'
    Write-Host ($summary -f $InstallId, $priorVersion, $preservedProfiles.Count, $deactivatedCandidates.Count, $rollEvidence)
}
catch {
    $primary = 'rollback: ' + $_.Exception.Message
    if (-not $swapped) {
        # Mods and the recorded backup were never changed: the installed
        # build is still live, and the record says exactly that.
        Set-KbpRollbackRecord 'Installed' 'not-applied' $primary
        throw
    }
    # Review K6: a failure after the swap reverses it, so Mods and the
    # record agree again. The recorded backup was never moved.
    try {
        if ($InjectFailureAt -ceq 'record-and-reverse') { throw 'injected reverse failure' }
        Move-Item -LiteralPath $planner -Destination $stagedPlanner
        Move-Item -LiteralPath $removedPlanner -Destination $planner
        Assert-KbpPlannerIdentity $planner ([string]$install.version)
        if ((Get-KbpDirectoryContentIdentity $planner).directoryManifestSha256 -cne $beforeRollbackIdentity) {
            throw 'Reversed Mods planner does not match the pre-rollback identity.'
        }
        Set-KbpRollbackRecord 'Installed' 'reversed' $primary
    }
    catch {
        # No consistent state could be re-established: say so, and keep
        # the lock so no deployment runs over an ambiguous Mods tree.
        $keepLock = $true
        Set-KbpRollbackRecord 'RollbackRecoveryNeeded' 'reverse-failed' `
            ($primary + '; reverse: ' + $_.Exception.Message +
             '; installed build expected at ' + $removedPlanner +
             '; recorded prior backup at ' + $backupPlanner)
    }
    throw
}
finally {
    if (-not $keepLock) { Remove-KbpOwnedLock $lockPath $InstallId $token }
}
