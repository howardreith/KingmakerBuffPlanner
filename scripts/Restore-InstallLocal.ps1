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
    [ValidateSet('', 'candidate-archive', 'settings-merge', 'identity', 'move-installed', 'move-prior', 'move-prior-and-reverse', 'verify', 'record', 'record-and-reverse')]
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
# The lock is released only after a CLEAN state has been durably recorded
# (review L4): any ambiguous outcome keeps it.
$keepLock = $true
$installedDisplaced = $false
$priorPlaced = $false
$removedPlanner = Join-Path $rollEvidence 'rolled-back-planner'
$stagedPlanner = Join-Path $rollStaging 'restored-planner'
$candidatePattern = 'kingmaker-buff-planner-casting-*'
$beforeRollbackIdentity = $null

function Set-KbpRollbackRecord([string]$Status, [string]$Phase, [string]$Failure) {
    $install.status = $Status
    Add-Member -InputObject $install -MemberType NoteProperty -Name rollbackPhase -Value $Phase -Force
    if (-not [string]::IsNullOrEmpty($Failure)) {
        Add-Member -InputObject $install -MemberType NoteProperty -Name failure -Value $Failure -Force
    }
    Write-KbpJsonAtomic $installStatePath $install
}

function Set-KbpRollbackField([string]$Name, $Value) {
    Add-Member -InputObject $install -MemberType NoteProperty -Name $Name -Value $Value -Force
}

function Invoke-KbpInjectedFailure([string]$Phase) {
    if ($InjectFailureAt -ceq $Phase) { throw "injected rollback failure at $Phase" }
}

function Get-KbpContentLines([string]$Path) {
    return @(Get-KbpDirectoryManifest $Path | ForEach-Object {
        if ($_.kind -ceq 'file') { "F|$($_.path)|$($_.sha256)" } else { "D|$($_.path)" } })
}

function Get-KbpIdentityDigest([string]$Path) {
    return (Get-KbpDirectoryContentIdentity $Path).directoryManifestSha256
}

# Review L5: candidate-profile compatibility is a FORMAT contract, not a
# file-name family. The restored binary declares the newest candidate
# format it reads ("<schema>.<revision>") in an AssemblyMetadata attribute;
# it is read in a separate process (reflection-only, no execution, no
# assembly-identity clash with this session). No declaration = unknown =
# no candidate is kept active.
function Get-KbpTargetCandidateFormat([string]$DllPath) {
    $script = @'
param([string]$Path)
$ErrorActionPreference = 'Stop'
try {
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($Path))
    foreach ($data in $assembly.GetCustomAttributesData()) {
        if ($data.AttributeType.FullName -ceq 'System.Reflection.AssemblyMetadataAttribute' -and
            $data.ConstructorArguments.Count -eq 2 -and
            [string]$data.ConstructorArguments[0].Value -ceq 'KingmakerBuffPlanner.CandidateProfileFormat') {
            [Console]::Out.WriteLine('FORMAT=' + [string]$data.ConstructorArguments[1].Value)
        }
    }
    [Console]::Out.WriteLine('DONE')
}
catch { [Console]::Out.WriteLine('ERROR=' + $_.Exception.GetType().Name) }
'@
    $probeFile = Join-Path $rollStaging ('format-probe-' + [Guid]::NewGuid().ToString('N') + '.ps1')
    Set-Content -LiteralPath $probeFile -Value $script -Encoding UTF8
    try {
        $output = @(& powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $probeFile -Path $DllPath 2>$null)
    }
    finally { Remove-Item -LiteralPath $probeFile -Force -ErrorAction SilentlyContinue }
    if (-not ($output -ccontains 'DONE')) { return $null }
    $formats = @($output | Where-Object { $_ -like 'FORMAT=*' } | ForEach-Object { $_.Substring(7) })
    if ($formats.Count -ne 1 -or $formats[0] -notmatch '^\d+\.\d+$') { return $null }
    return $formats[0]
}

# Members each candidate format revision may contain (the schema-6 profile
# model, including nested ui/execution/ability objects). Kept in step with
# CastingPlanProfileModels.cs by the protocol test
# rollback-candidate-format-members-match-model.
$candidateMembersRev1 = @('schemaVersion', 'campaignId', 'routines', 'castings', 'ui', 'execution',
    'routineId', 'name', 'castingId', 'order', 'sourceId', 'ability', 'casterUnitId', 'spellbookGuid',
    'targetMode', 'directTargetUnitId', 'origin', 'requiredCoverageUnitIds', 'targetingModifiers',
    'enhancements', 'existingEffectPolicy', 'ignoredPresenceMarkers', 'state', 'provenance',
    'casterCentered', 'anchorUnitId', 'modifierId', 'enabled', 'enhancementId', 'required',
    'exactSourceRef', 'legacyAssignmentId', 'legacySchemaVersion', 'legacyRoutineId', 'note',
    'baseAbilityGuid', 'variantGuid', 'metamagicMask', 'sourceKind', 'specialSourceId',
    'scale', 'hotkey', 'mode', 'allowAnimatedFallback', 'outOfCombatOnly', 'recastExisting')
$candidateMembersRev2 = @('importNotices', 'legacyRecipientKey', 'reviewItems')
$candidateMembersRev3 = @('acknowledgedImportNotices', 'resolvedReviewItems', 'formatRevision')

function Get-KbpJsonMemberNames($Node) {
    $names = New-Object System.Collections.Generic.List[string]
    $stack = New-Object System.Collections.Stack
    $stack.Push($Node)
    while ($stack.Count -ne 0) {
        $current = $stack.Pop()
        if ($current -is [System.Management.Automation.PSCustomObject]) {
            foreach ($property in $current.PSObject.Properties) {
                $names.Add($property.Name)
                $stack.Push($property.Value)
            }
        }
        elseif ($current -is [System.Collections.IEnumerable] -and -not ($current -is [string])) {
            foreach ($item in $current) { $stack.Push($item) }
        }
    }
    return ,$names
}

# The format a candidate file REQUIRES, or $null when it is not a readable
# schema-6 candidate at all (malformed, other schema, unknown members).
function Get-KbpCandidateRequirement([string]$Path) {
    try {
        $raw = [IO.File]::ReadAllText($Path)
        $json = $raw | ConvertFrom-Json
    }
    catch { return $null }
    if ($null -eq $json -or -not ($json -is [System.Management.Automation.PSCustomObject])) { return $null }
    $schemaProperty = $json.PSObject.Properties['schemaVersion']
    if ($null -eq $schemaProperty -or -not ($schemaProperty.Value -is [int] -or $schemaProperty.Value -is [long])) { return $null }
    $schema = [int]$schemaProperty.Value
    $required = 1
    foreach ($name in (Get-KbpJsonMemberNames $json)) {
        if ($candidateMembersRev1 -ccontains $name) { continue }
        if ($candidateMembersRev2 -ccontains $name) { if ($required -lt 2) { $required = 2 }; continue }
        if ($candidateMembersRev3 -ccontains $name) { if ($required -lt 3) { $required = 3 }; continue }
        return $null
    }
    $revisionProperty = $json.PSObject.Properties['formatRevision']
    if ($null -ne $revisionProperty) {
        if (-not ($revisionProperty.Value -is [int] -or $revisionProperty.Value -is [long])) { return $null }
        if ([int]$revisionProperty.Value -gt $required) { $required = [int]$revisionProperty.Value }
    }
    return [pscustomobject]@{ schema = $schema; revision = $required }
}

function Test-KbpTargetReadsCandidate([string]$TargetFormat, $Requirement) {
    if ([string]::IsNullOrEmpty($TargetFormat) -or $null -eq $Requirement) { return $false }
    $parts = $TargetFormat.Split('.')
    return ([int]$parts[0] -eq $Requirement.schema) -and ([int]$parts[1] -ge $Requirement.revision)
}

try {
    # Phase 1 (prepare): nothing under Mods or the recorded backup changes.
    Set-KbpRollbackField 'rollbackEvidenceRoot' $rollEvidence
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
    $beforeRollbackIdentity = Get-KbpIdentityDigest $planner
    Set-Content -LiteralPath (Join-Path $rollEvidence 'before-rollback-identity.txt') `
        -Value $beforeRollbackIdentity -Encoding UTF8
    $backupIdentity = Get-KbpIdentityDigest $backupPlanner
    Copy-Item -LiteralPath $backupPlanner -Destination $stagedPlanner -Recurse
    if ((Get-KbpIdentityDigest $stagedPlanner) -cne $backupIdentity) {
        throw 'Staged prior build does not match the recorded backup.'
    }
    $targetFormat = Get-KbpTargetCandidateFormat (Join-Path $stagedPlanner 'KingmakerBuffPlanner.dll')

    # Every candidate from BOTH sides is checked against the restored
    # binary's declared format; any it cannot verifiably read is archived
    # byte-for-byte and deactivated. Kept candidates are recorded too.
    Set-KbpRollbackRecord 'RollingBack' 'candidate-archive' $null
    Invoke-KbpInjectedFailure 'candidate-archive'
    $stagedSettings = Join-Path $stagedPlanner 'UserSettings'
    $candidateArchive = Join-Path $rollEvidence 'deactivated-candidate-profiles'
    $deactivatedCandidates = @()
    $keptCandidates = @()
    foreach ($relative in @($preservedProfiles | Where-Object { (Split-Path -Leaf $_) -like $candidatePattern })) {
        $requirement = Get-KbpCandidateRequirement (Join-Path $preservedSettings $relative)
        if (Test-KbpTargetReadsCandidate $targetFormat $requirement) {
            $keptCandidates += @('installed\' + $relative)
            continue
        }
        $target = Join-Path $candidateArchive ('installed\' + $relative)
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $preservedSettings $relative) -Destination $target
        $deactivatedCandidates += @('installed\' + $relative)
        $preservedProfiles = @($preservedProfiles | Where-Object { $_ -cne $relative })
    }
    if (Test-Path -LiteralPath $stagedSettings -PathType Container) {
        foreach ($file in @(Get-ChildItem -LiteralPath $stagedSettings -File -Recurse |
                Where-Object { $_.Name -like $candidatePattern })) {
            $relative = Get-KbpRelativePath $stagedSettings $file.FullName
            if (Test-KbpTargetReadsCandidate $targetFormat (Get-KbpCandidateRequirement $file.FullName)) {
                $keptCandidates += @('prior\' + $relative)
                continue
            }
            $target = Join-Path $candidateArchive ('prior\' + $relative)
            New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
            Move-Item -LiteralPath $file.FullName -Destination $target
            $deactivatedCandidates += @('prior\' + $relative)
        }
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
    $stagedIdentity = Get-KbpIdentityDigest $stagedPlanner

    # Phase 2 (swap). Recovery data is recorded BEFORE Mods changes.
    Set-KbpRollbackField 'rollbackInstalledIdentity' $beforeRollbackIdentity
    Set-KbpRollbackField 'rollbackStagedIdentity' $stagedIdentity
    Set-KbpRollbackField 'rollbackInstalledDisplacedPath' $removedPlanner
    Set-KbpRollbackField 'rollbackStagedPriorPath' $stagedPlanner
    Set-KbpRollbackRecord 'RollingBack' 'swap' $null
    Invoke-KbpInjectedFailure 'move-installed'
    Move-Item -LiteralPath $planner -Destination $removedPlanner
    $installedDisplaced = $true
    try {
        Invoke-KbpInjectedFailure 'move-prior'
        Invoke-KbpInjectedFailure 'move-prior-and-reverse'
        Move-Item -LiteralPath $stagedPlanner -Destination $planner
        $priorPlaced = $true
    }
    catch {
        $moveFailure = $_
        # Put the tested build back rather than leaving Mods empty; only a
        # verified return clears the displaced state.
        try {
            if ($InjectFailureAt -ceq 'move-prior-and-reverse') { throw 'injected reverse failure' }
            Move-Item -LiteralPath $removedPlanner -Destination $planner
            if ((Get-KbpIdentityDigest $planner) -ceq $beforeRollbackIdentity) { $installedDisplaced = $false }
        }
        catch { }
        throw $moveFailure
    }

    Set-KbpRollbackRecord 'RollingBack' 'verify' $null
    Invoke-KbpInjectedFailure 'verify'
    Assert-KbpPlannerIdentity $planner $priorVersion
    if ((Get-KbpIdentityDigest $planner) -cne $stagedIdentity) {
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
    Set-KbpRollbackField 'rolledBackAtUtc' ([DateTime]::UtcNow.ToString('o'))
    Set-KbpRollbackField 'rollbackPreservedProfiles' $preservedProfiles
    Set-KbpRollbackField 'rollbackDeactivatedCandidateProfiles' $deactivatedCandidates
    Set-KbpRollbackField 'rollbackKeptCandidateProfiles' $keptCandidates
    Set-KbpRollbackField 'rollbackTargetCandidateFormat' $(if ($targetFormat) { $targetFormat } else { 'none' })
    Set-KbpRollbackRecord 'RolledBack' 'complete' $null
    $keepLock = $false
    $summary = 'Install rollback: PASS=1 FAIL=0 installId={0} restoredVersion={1} preservedProfiles={2} deactivatedCandidateProfiles={3} keptCandidateProfiles={4} evidence={5}'
    Write-Host ($summary -f $InstallId, $priorVersion, $preservedProfiles.Count, $deactivatedCandidates.Count, $keptCandidates.Count, $rollEvidence)
}
catch {
    $primary = 'rollback: ' + $_.Exception.Message
    try {
        if (-not $installedDisplaced -and -not $priorPlaced) {
            # The installed build never left Mods, or it was put back and
            # verified byte-identical: confirm before calling it clean.
            if ($null -ne $beforeRollbackIdentity -and
                (Get-KbpIdentityDigest $planner) -cne $beforeRollbackIdentity) {
                throw 'installed planner identity changed during a failed rollback'
            }
            Set-KbpRollbackRecord 'Installed' 'not-applied' $primary
            $keepLock = $false
        }
        elseif ($priorPlaced) {
            # A failure after the complete swap reverses it.
            if ($InjectFailureAt -ceq 'record-and-reverse') { throw 'injected reverse failure' }
            Move-Item -LiteralPath $planner -Destination $stagedPlanner
            $priorPlaced = $false
            Move-Item -LiteralPath $removedPlanner -Destination $planner
            $installedDisplaced = $false
            Assert-KbpPlannerIdentity $planner ([string]$install.version)
            if ((Get-KbpIdentityDigest $planner) -cne $beforeRollbackIdentity) {
                throw 'Reversed Mods planner does not match the pre-rollback identity.'
            }
            Set-KbpRollbackRecord 'Installed' 'reversed' $primary
            $keepLock = $false
        }
        else {
            throw 'installed planner displaced and not restored'
        }
    }
    catch {
        # No verified consistent state: say so (best effort) and keep the
        # lock so no deployment or rollback runs over an ambiguous Mods tree.
        $keepLock = $true
        try {
            Set-KbpRollbackRecord 'RollbackRecoveryNeeded' 'recovery-needed' `
                ($primary + '; recovery: ' + $_.Exception.Message +
                 '; installed build (identity ' + $beforeRollbackIdentity + ') expected at ' +
                 $(if ($installedDisplaced) { $removedPlanner } else { $planner }) +
                 '; staged prior at ' + $stagedPlanner +
                 '; recorded prior backup untouched at ' + $backupPlanner)
        }
        catch { }
    }
    throw
}
finally {
    if (-not $keepLock) { Remove-KbpOwnedLock $lockPath $InstallId $token }
}
