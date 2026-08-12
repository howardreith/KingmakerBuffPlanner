[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][string]$ReleaseManifestPath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$InstallId,
    [string]$KingmakerInstallDir,
    [string]$ExpectedPriorVersion = '0.0.1'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false

function Get-KbpNonPlannerManifest([string]$ModsPath) {
    return @(Get-KbpDirectoryManifest $ModsPath | Where-Object {
        $_.path -cne 'KingmakerBuffPlanner' -and
        -not ([string]$_.path).StartsWith('KingmakerBuffPlanner\', [StringComparison]::OrdinalIgnoreCase)
    })
}

function Assert-KbpPlannerPrimaryIdentity([string]$Path, [string]$ExpectedVersion) {
    $infoPath = Join-Path $Path 'Info.json'
    $dllPath = Join-Path $Path 'KingmakerBuffPlanner.dll'
    if (-not (Test-Path -LiteralPath $infoPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
        throw 'Planner installation is missing its primary files.'
    }
    $info = Read-KbpJson $infoPath
    $assemblyName = [Reflection.AssemblyName]::GetAssemblyName($dllPath)
    if ($info.Id -cne 'KingmakerBuffPlanner' -or
        $info.AssemblyName -cne 'KingmakerBuffPlanner.dll' -or
        $info.Version -cne $ExpectedVersion -or
        $assemblyName.Name -cne 'KingmakerBuffPlanner' -or
        $assemblyName.Version.ToString() -cne ($ExpectedVersion + '.0')) {
        throw "Planner installation identity/version is invalid at $Path."
    }
}

$root = Get-KbpRepositoryRoot
$manifestPath = (Resolve-Path -LiteralPath $ReleaseManifestPath).Path
[void](Assert-KbpPathWithin -Path $manifestPath -Root (Join-Path $root 'artifacts\release'))
$release = Read-KbpJson $manifestPath
$version = Get-KbpVersion
if ($release.schemaVersion -ne 1 -or -not [bool]$release.validated -or
    $release.publicationStatus -cne 'local-only' -or $release.version -cne $version -or
    $release.commit -notmatch '^[0-9a-f]{40}$' -or
    [string]::IsNullOrWhiteSpace([string]$release.assemblyMvid)) {
    throw 'Release manifest is not an exact validated local-only release.'
}
$package = (Resolve-Path -LiteralPath ([string]$release.packagePath)).Path
[void](Assert-KbpPathWithin -Path $package -Root (Join-Path $root 'artifacts\release'))
if ((Get-KbpSha256 $package) -cne [string]$release.packageSha256) {
    throw 'Release package hash does not match its manifest.'
}
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $package
if (-not $KingmakerInstallDir) { $KingmakerInstallDir = Get-KbpGamePath }
$game = (Resolve-Path -LiteralPath $KingmakerInstallDir).Path
Assert-KbpNotRunning
if (@(Get-Process -Name UnityModManager -ErrorAction SilentlyContinue).Count -ne 0) {
    throw 'Unity Mod Manager is running.'
}
Assert-KbpNoUnresolvedTransaction $script:KbpRuntimeStateRoot
$mods = Join-Path $game 'Mods'
if (-not (Test-Path -LiteralPath $mods -PathType Container)) { throw 'The live Mods directory is missing.' }
$planner = Join-Path $mods 'KingmakerBuffPlanner'
$priorExists = Test-Path -LiteralPath $planner -PathType Container
if ($priorExists) { Assert-KbpPlannerPrimaryIdentity $planner $ExpectedPriorVersion }

$WhatIfPreference = $requestedWhatIf
if (-not $PSCmdlet.ShouldProcess($planner,
    "replace the validated $ExpectedPriorVersion planner with validated $version for local testing")) {
    Write-Host 'Local install preflight PASS; WhatIf made no install, state, evidence, staging, backup, game, or mod mutation.'
    return
}

$token = [Guid]::NewGuid().ToString('N')
$lockPath = Join-Path $script:KbpRuntimeStateRoot 'deployment.lock'
$stateRoot = Join-Path $script:KbpRuntimeStateRoot ('installations\' + $InstallId)
$stageRoot = Join-Path $script:KbpRuntimeStagingRoot ('install-' + $InstallId)
$backupRoot = Join-Path $script:KbpRuntimeBackupRoot ('install-' + $InstallId)
$evidenceRoot = Join-Path $script:KbpRuntimeEvidenceRoot ('install-' + $InstallId)
foreach ($path in @($stateRoot, $stageRoot, $backupRoot, $evidenceRoot)) {
    if (Test-Path -LiteralPath $path) { throw "Install-owned path already exists: $path" }
}
New-KbpOwnedLock $lockPath $InstallId $token
New-Item -ItemType Directory -Path $stateRoot, $backupRoot, $evidenceRoot | Out-Null
$statePath = Join-Path $stateRoot 'install.json'
$backupPlanner = Join-Path $backupRoot 'KingmakerBuffPlanner.prior'
$failedPlanner = Join-Path $backupRoot 'KingmakerBuffPlanner.failed'
$beforeMods = @(Get-KbpDirectoryManifest $mods)
$beforeOther = @(Get-KbpNonPlannerManifest $mods)
$priorIdentity = if ($priorExists) { Get-KbpDirectoryContentIdentity $planner } else { $null }
$state = [ordered]@{
    schemaVersion = 1; installId = $InstallId; token = $token; status = 'Preparing'
    createdAtUtc = [DateTime]::UtcNow.ToString('o'); completedAtUtc = $null
    releaseManifestPath = $manifestPath; packagePath = $package
    version = $version; commit = [string]$release.commit
    packageSha256 = [string]$release.packageSha256; dllSha256 = [string]$release.dllSha256
    assemblyMvid = [string]$release.assemblyMvid; gameRoot = $game; modsPath = $mods
    priorExisted = $priorExists; expectedPriorVersion = $ExpectedPriorVersion
    backupPlanner = $backupPlanner; stageRoot = $stageRoot; evidenceRoot = $evidenceRoot
    beforeModsManifest = $beforeMods; beforeOtherModsManifest = $beforeOther
    priorPlannerIdentity = $priorIdentity; installedPlannerIdentity = $null
    otherModsVerified = $false; settingsPreserved = $false; rollbackVerified = $false
    failure = $null
}
Write-KbpJsonAtomic $statePath $state
$activated = $false
try {
    $stagedPlanner = Expand-KbpPackageToStaging -PackagePath $package -StagingRunRoot $stageRoot
    Assert-KbpPlannerPrimaryIdentity $stagedPlanner $version
    if ((Get-KbpSha256 (Join-Path $stagedPlanner 'KingmakerBuffPlanner.dll')) -cne [string]$release.dllSha256) {
        throw 'Staged DLL hash does not match the release manifest.'
    }
    if ($priorExists) {
        Move-Item -LiteralPath $planner -Destination $backupPlanner
        Assert-KbpPlannerPrimaryIdentity $backupPlanner $ExpectedPriorVersion
        $settings = Join-Path $backupPlanner 'UserSettings'
        if (Test-Path -LiteralPath $settings -PathType Container) {
            Copy-Item -LiteralPath $settings -Destination $stagedPlanner -Recurse
            $state.settingsPreserved = Test-KbpManifestEqual `
                @(Get-KbpDirectoryManifest $settings) `
                @(Get-KbpDirectoryManifest (Join-Path $stagedPlanner 'UserSettings'))
            if (-not $state.settingsPreserved) { throw 'External profile settings were not preserved exactly.' }
        } else { $state.settingsPreserved = $true }
    } else { $state.settingsPreserved = $true }
    $state.status = 'PriorBackedUp'
    Write-KbpJsonAtomic $statePath $state
    Move-Item -LiteralPath $stagedPlanner -Destination $planner
    $activated = $true
    Assert-KbpPlannerPrimaryIdentity $planner $version
    if ((Get-KbpSha256 (Join-Path $planner 'KingmakerBuffPlanner.dll')) -cne [string]$release.dllSha256) {
        throw 'Installed DLL hash does not match the release manifest.'
    }
    $afterOther = @(Get-KbpNonPlannerManifest $mods)
    if (-not (Test-KbpManifestEqual $beforeOther $afterOther)) {
        throw 'A non-planner mod changed during installation.'
    }
    $state.otherModsVerified = $true
    $state.installedPlannerIdentity = Get-KbpDirectoryContentIdentity $planner
    $state.status = 'Installed'
    $state.completedAtUtc = [DateTime]::UtcNow.ToString('o')
    Write-KbpJsonAtomic $statePath $state
    Copy-Item -LiteralPath $statePath -Destination (Join-Path $evidenceRoot 'install-result.json')
    Write-Host "Local install: PASS version=$version package=$($release.packageSha256) dll=$($release.dllSha256) evidence=$evidenceRoot"
}
catch {
    $failure = $_
    try {
        if ($activated -and (Test-Path -LiteralPath $planner -PathType Container)) {
            Assert-KbpPlannerPrimaryIdentity $planner $version
            Move-Item -LiteralPath $planner -Destination $failedPlanner
        }
        if ($priorExists -and (Test-Path -LiteralPath $backupPlanner -PathType Container) -and
            -not (Test-Path -LiteralPath $planner)) {
            Move-Item -LiteralPath $backupPlanner -Destination $planner
        }
        $state.rollbackVerified = Test-KbpManifestEqual $beforeMods @(Get-KbpDirectoryManifest $mods)
        if (-not $state.rollbackVerified) { throw 'Rollback did not restore the exact pre-install Mods manifest.' }
        $state.status = 'RolledBack'
        $state.failure = $failure.Exception.Message
        $state.completedAtUtc = [DateTime]::UtcNow.ToString('o')
        Write-KbpJsonAtomic $statePath $state
    }
    catch {
        $state.status = 'RollbackFailed'
        $state.failure = $failure.Exception.Message + ' | Rollback: ' + $_.Exception.Message
        Write-KbpJsonAtomic $statePath $state
        throw "Local install and rollback failed closed. State: $statePath Cause: $($state.failure)"
    }
    throw $failure
}
finally {
    if ($state.status -in @('Installed', 'RolledBack')) {
        if (Test-Path -LiteralPath $stageRoot -PathType Container) {
            $resolvedStage = [IO.Path]::GetFullPath($stageRoot)
            [void](Assert-KbpPathWithin -Path $resolvedStage -Root $script:KbpRuntimeStagingRoot)
            Remove-Item -LiteralPath $resolvedStage -Recurse -Force
        }
        if (Test-Path -LiteralPath $lockPath -PathType Leaf) {
            Remove-KbpOwnedLock $lockPath $InstallId $token
        }
    }
}
