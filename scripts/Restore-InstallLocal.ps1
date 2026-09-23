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
    [string]$EvidenceRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false

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

$rollStaging = Join-Path $StagingRoot ('rollback-' + $InstallId)
$rollEvidence = Join-Path $EvidenceRoot ('rollback-' + $InstallId)
foreach ($path in @($rollStaging, $rollEvidence)) {
    if (Test-Path -LiteralPath $path) { throw "Rollback-owned path already exists: $path" }
}

if (-not $PSCmdlet.ShouldProcess($planner,
        "roll installation '$InstallId' back to the preserved $priorVersion planner, keeping newer UserSettings profiles")) {
    Write-Host 'Rollback preflight PASS; WhatIf made no install, state, staging, backup, evidence, game, or mod mutation.'
    return
}

$token = [Guid]::NewGuid().ToString('N')
$lockPath = Join-Path $StateRoot 'deployment.lock'
New-KbpOwnedLock $lockPath $InstallId $token
try {
    New-Item -ItemType Directory -Path $rollStaging, $rollEvidence | Out-Null
    $preservedSettings = Join-Path $rollStaging 'usersettings-preserved'
    $currentSettings = Join-Path $planner 'UserSettings'
    $preservedProfiles = @()
    if (Test-Path -LiteralPath $currentSettings -PathType Container) {
        # The owner's profiles written AFTER installation are newer edits:
        # preserve every one of them across the rollback instead of letting
        # the older backup shadow them. Deletion is never part of rollback.
        Copy-Item -LiteralPath $currentSettings -Destination $preservedSettings -Recurse
        $preservedProfiles = @(Get-ChildItem -LiteralPath $preservedSettings -File -Recurse |
            ForEach-Object { Get-KbpRelativePath $preservedSettings $_.FullName })
    }
    $beforeRollbackIdentity = Get-KbpDirectoryContentIdentity $planner
    Set-Content -LiteralPath (Join-Path $rollEvidence 'before-rollback-identity.txt') `
        -Value $beforeRollbackIdentity -Encoding UTF8

    # Swap: current installed folder -> evidence; recorded prior -> Mods.
    $removedPlanner = Join-Path $rollEvidence 'rolled-back-planner'
    Move-Item -LiteralPath $planner -Destination $removedPlanner
    try {
        Move-Item -LiteralPath $backupPlanner -Destination $planner
    }
    catch {
        # Put the tested build back rather than leaving Mods empty.
        Move-Item -LiteralPath $removedPlanner -Destination $planner
        throw
    }

    # Merge the preserved newer profiles back over the restored folder.
    # Casting-first (schema-6) candidate profiles are DEACTIVATED, not
    # merged (charter §7.3): the prior binary cannot read them, so they stay
    # archived with this rollback's evidence (and in the rolled-back
    # planner copy) instead of lingering beside the restored build. Legacy
    # schema-5 profiles keep the newer-edits-win merge.
    $restoredSettings = Join-Path $planner 'UserSettings'
    $deactivatedCandidates = @($preservedProfiles | Where-Object {
        (Split-Path -Leaf $_) -like 'kingmaker-buff-planner-casting-*' })
    if ($deactivatedCandidates.Count -ne 0) {
        $candidateArchive = Join-Path $rollEvidence 'deactivated-candidate-profiles'
        foreach ($relative in $deactivatedCandidates) {
            $target = Join-Path $candidateArchive $relative
            $targetDirectory = Split-Path -Parent $target
            if (-not (Test-Path -LiteralPath $targetDirectory -PathType Container)) {
                New-Item -ItemType Directory -Path $targetDirectory | Out-Null
            }
            Copy-Item -LiteralPath (Join-Path $preservedSettings $relative) -Destination $target
        }
    }
    $preservedProfiles = @($preservedProfiles | Where-Object {
        (Split-Path -Leaf $_) -notlike 'kingmaker-buff-planner-casting-*' })
    if ($preservedProfiles.Count -ne 0) {
        foreach ($relative in $preservedProfiles) {
            $source = Join-Path $preservedSettings $relative
            $target = Join-Path $restoredSettings $relative
            $targetDirectory = Split-Path -Parent $target
            if (-not (Test-Path -LiteralPath $targetDirectory -PathType Container)) {
                New-Item -ItemType Directory -Path $targetDirectory | Out-Null
            }
            # Newer (post-install) edits win over the older backup's files.
            Copy-Item -LiteralPath $source -Destination $target -Force
        }
    }

    Assert-KbpPlannerIdentity $planner $priorVersion
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

    $install.status = 'RolledBack'
    Add-Member -InputObject $install -MemberType NoteProperty -Name rolledBackAtUtc `
        -Value ([DateTime]::UtcNow.ToString('o')) -Force
    Add-Member -InputObject $install -MemberType NoteProperty -Name rollbackEvidenceRoot `
        -Value $rollEvidence -Force
    Add-Member -InputObject $install -MemberType NoteProperty -Name rollbackPreservedProfiles `
        -Value $preservedProfiles -Force
    Add-Member -InputObject $install -MemberType NoteProperty -Name rollbackDeactivatedCandidateProfiles `
        -Value $deactivatedCandidates -Force
    Write-KbpJsonAtomic $installStatePath $install
    $summary = 'Install rollback: PASS=1 FAIL=0 installId={0} restoredVersion={1} preservedProfiles={2} deactivatedCandidateProfiles={3} evidence={4}'
    Write-Host ($summary -f $InstallId, $priorVersion, $preservedProfiles.Count, $deactivatedCandidates.Count, $rollEvidence)
}
catch {
    $install.status = 'Installed'
    Add-Member -InputObject $install -MemberType NoteProperty -Name failure `
        -Value ('rollback: ' + $_.Exception.Message) -Force
    Write-KbpJsonAtomic $installStatePath $install
    throw
}
finally {
    Remove-KbpOwnedLock $lockPath $InstallId $token
}
