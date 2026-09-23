Set-StrictMode -Version Latest

# Windows PowerShell 5.1 only. PowerShell 7's ConvertFrom-Json turns the
# manifests' ISO timestamp strings into DateTime values whose re-serialization
# drops trailing fractional zeros, so exact manifest comparisons (including
# live Mods restoration verification) fail on unchanged files. Refuse rather
# than mis-verify (reproduced 2026-09-22).
if ($PSVersionTable.PSEdition -cne 'Desktop') {
    throw 'The Kingmaker runtime harness requires Windows PowerShell 5.1 (powershell.exe); PowerShell 7 JSON date conversion breaks exact manifest verification.'
}

. (Join-Path $PSScriptRoot 'Common.ps1')

$script:KbpLabRoot = 'C:\Dev\KingmakerBuffPlannerLab'
$script:KbpRuntimeStateRoot = Join-Path $script:KbpLabRoot 'runtime-state'
$script:KbpRuntimeStagingRoot = Join-Path $script:KbpLabRoot 'runtime-staging'
$script:KbpRuntimeBackupRoot = Join-Path $script:KbpLabRoot 'runtime-backups'
$script:KbpRuntimeEvidenceRoot = Join-Path $script:KbpLabRoot 'runtime-evidence'

# Another project's live lease on the same Kingmaker installation: the
# owner's KingmakerGunslinger lab replaces Mods\KingmakerGunslinger and
# launches the game while this lock exists (coordinated with that session
# on 2026-09-23; it checks this lab's deployment.lock in turn). No runtime
# transaction or local install starts while one is held.
$script:KbpForeignRuntimeLeases = @('C:\Dev\KingmakerGunslingerLab\compatibility-state\compatibility.lock')

function Assert-KbpNoForeignRuntimeLease {
    param([string[]]$LeasePaths = $script:KbpForeignRuntimeLeases)
    foreach ($lease in @($LeasePaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        if (Test-Path -LiteralPath $lease) {
            throw "Another project's Kingmaker runtime lease is active: $lease. Wait until it is released."
        }
    }
}

function Assert-KbpNotRunning {
    param([int[]]$KnownProcessIds)
    $ids = if ($PSBoundParameters.ContainsKey('KnownProcessIds')) {
        @($KnownProcessIds)
    } else {
        @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue | ForEach-Object Id)
    }
    $ids = @($ids | Where-Object { $null -ne $_ -and [int]$_ -gt 0 })
    if (@($ids).Count -ne 0) { throw "Pathfinder: Kingmaker is running (PID(s): $($ids -join ', '))." }
}

function Get-KbpRelativePath([string]$Root, [string]$Path) {
    $rootUri = [Uri]([IO.Path]::GetFullPath($Root).TrimEnd('\') + '\')
    return [Uri]::UnescapeDataString(
        $rootUri.MakeRelativeUri([Uri][IO.Path]::GetFullPath($Path)).ToString()).Replace('/', '\')
}

function Get-KbpDirectoryManifest([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) { throw "Directory is missing: $Path" }
    $root = (Resolve-Path -LiteralPath $Path).Path
    $directories = @(Get-ChildItem -LiteralPath $root -Directory -Recurse -Force |
        Sort-Object FullName | ForEach-Object {
            [ordered]@{ path = Get-KbpRelativePath $root $_.FullName; kind = 'directory' }
        })
    $files = @(Get-ChildItem -LiteralPath $root -File -Recurse -Force |
        Sort-Object FullName | ForEach-Object {
            [ordered]@{
                path = Get-KbpRelativePath $root $_.FullName
                kind = 'file'
                length = $_.Length
                lastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o')
                sha256 = Get-KbpSha256 $_.FullName
            }
        })
    return @([ordered]@{ path = '.'; kind = 'directory' }) + $directories + $files
}

function Test-KbpManifestEqual($Expected, $Actual) {
    return (($Expected | ConvertTo-Json -Depth 8 -Compress) -ceq
        ($Actual | ConvertTo-Json -Depth 8 -Compress))
}

function Get-KbpDirectoryContentIdentity([string]$Path) {
    $manifest = @(Get-KbpDirectoryManifest $Path)
    $lines = foreach ($entry in $manifest) {
        if ($entry.kind -ceq 'directory') { "D|$($entry.path)" }
        else { "F|$($entry.path)|$($entry.length)|$($entry.sha256)" }
    }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($lines -join "`n") + "`n")
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $digest = ([BitConverter]::ToString($hasher.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant() }
    finally { $hasher.Dispose() }
    $files = @($manifest | Where-Object kind -ceq 'file')
    return [pscustomobject]@{
        directoryManifestSha256 = $digest
        fileCount = $files.Count
        totalBytes = [long](($files | ForEach-Object { [long]$_['length'] } |
            Measure-Object -Sum).Sum)
        manifest = $manifest
    }
}

# Prospective fixture-seal evidence: a complete per-file inventory bound to
# a seal identity. Writing one at future seal creation makes later drift
# exactly explainable (added/removed/changed paths with hashes and sizes).
# This does not recover missing historical evidence and does not authorize
# any current fixture: an aggregate-only mismatch without a bound inventory
# stays blocked as insufficient historical evidence.
function Write-KbpFixtureSealInventory(
    [Parameter(Mandatory = $true)][string]$ModName,
    [Parameter(Mandatory = $true)]$Identity,
    [Parameter(Mandatory = $true)][string]$DestinationRoot)
{
    $safeName = if ($ModName -cmatch '^[A-Za-z0-9._-]{1,100}$') { $ModName } else { 'unsafe' }
    $inventory = [ordered]@{
        schemaVersion = 1
        modName = $safeName
        boundDirectoryManifestSha256 = [string]$Identity.directoryManifestSha256
        fileCount = [int]$Identity.fileCount
        totalBytes = [long]$Identity.totalBytes
        files = @($Identity.manifest | Where-Object kind -ceq 'file' | ForEach-Object {
            [ordered]@{ path = [string]$_.path; length = [long]$_.length; sha256 = [string]$_.sha256 }
        })
    }
    $fileName = 'fixture-inventory-{0}-{1}.json' -f $safeName, ([string]$Identity.directoryManifestSha256).Substring(0, 12)
    $directory = Join-Path $DestinationRoot 'fixture-inventories'
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $path = Join-Path $directory $fileName
    Write-KbpJsonAtomic $path $inventory
    return $path
}

# Exact comparison of a live identity against a bound per-file inventory.
# Returns a result object: status 'match', 'differs' (with exact added,
# removed, and changed path details), or 'insufficient-historical-evidence'
# when no per-file inventory exists — which callers must keep blocked under
# the existing policy rather than treating as a pass.
function Compare-KbpFixtureInventory(
    [Parameter(Mandatory = $true)]$SealInventory,
    [Parameter(Mandatory = $true)]$ActualIdentity)
{
    $actual = @{}
    foreach ($file in ($ActualIdentity.manifest | Where-Object kind -ceq 'file')) {
        $actual[[string]$file.path] = $file
    }
    $sealed = @{}
    foreach ($file in @($SealInventory.files)) {
        $sealed[[string]$file.path] = $file
    }
    $added = @($actual.Keys | Where-Object { -not $sealed.ContainsKey($_) } | Sort-Object)
    $removed = @($sealed.Keys | Where-Object { -not $actual.ContainsKey($_) } | Sort-Object)
    $changed = @($actual.Keys | Where-Object {
        $sealed.ContainsKey($_) -and (
            [long]$sealed[$_].length -ne [long]$actual[$_].length -or
            [string]$sealed[$_].sha256 -cne [string]$actual[$_].sha256)
    } | Sort-Object)
    if ($added.Count -eq 0 -and $removed.Count -eq 0 -and $changed.Count -eq 0) {
        return [pscustomobject]@{ status = 'match'; added = @(); removed = @(); changed = @() }
    }
    return [pscustomobject]@{
        status = 'differs'
        added = $added
        removed = $removed
        changed = $changed
    }
}

function Get-KbpFixtureSealInventory([string]$InventoryRoot, [string]$ModName, [string]$BoundManifestSha256) {
    if ([string]::IsNullOrWhiteSpace($InventoryRoot) -or -not (Test-Path -LiteralPath $InventoryRoot -PathType Container)) { return $null }
    $fileName = 'fixture-inventory-{0}-{1}.json' -f $ModName, $BoundManifestSha256.Substring(0, 12)
    $path = Join-Path (Join-Path $InventoryRoot 'fixture-inventories') $fileName
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $null }
    return Read-KbpJson $path
}

function Assert-KbpCompatibilityModIdentity($Expected, [string]$Path) {
    if ($Expected.directoryName -notmatch '^[A-Za-z0-9._-]{1,100}$') {
        throw 'Compatibility mod directory name is unsafe.'
    }
    $identity = Get-KbpDirectoryContentIdentity $Path
    if ($identity.directoryManifestSha256 -cne [string]$Expected.directoryManifestSha256 -or
        $identity.fileCount -ne [int]$Expected.fileCount -or
        $identity.totalBytes -ne [long]$Expected.totalBytes) {
        throw "Compatibility fixture identity mismatch: $($Expected.directoryName)"
    }
    $info = Join-Path $Path 'info.json'
    if (-not (Test-Path -LiteralPath $info -PathType Leaf)) { $info = Join-Path $Path 'Info.json' }
    $assembly = Join-Path $Path ([string]$Expected.assemblyName)
    if (-not (Test-Path -LiteralPath $info -PathType Leaf) -or
        -not (Test-Path -LiteralPath $assembly -PathType Leaf) -or
        (Get-KbpSha256 $info) -cne [string]$Expected.infoSha256 -or
        (Get-KbpSha256 $assembly) -cne [string]$Expected.assemblySha256) {
        throw "Compatibility fixture primary-file mismatch: $($Expected.directoryName)"
    }
    return $identity
}

# External exact-copy fixtures (profile entries with fixtureRelativePath)
# live under the lab examples root, never in the repository or a package.
$script:KbpExternalFixtureRoot = Join-Path $script:KbpLabRoot 'examples'

function Get-KbpCompatibilitySourcePath($Expected, [string]$LiveModsPath) {
    if ($Expected.PSObject.Properties.Name -contains 'fixtureRelativePath') {
        $fixtureRoot = $script:KbpExternalFixtureRoot
        $candidate = [IO.Path]::GetFullPath((Join-Path $fixtureRoot ([string]$Expected.fixtureRelativePath)))
        [void](Assert-KbpPathWithin -Path $candidate -Root $fixtureRoot)
        return $candidate
    }
    return Join-Path $LiveModsPath ([string]$Expected.directoryName)
}

function Write-KbpJsonAtomic([string]$Path, $Value) {
    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        throw "Atomic JSON destination directory is missing: $directory"
    }
    $temporary = Join-Path $directory ('.' + [IO.Path]::GetFileName($Path) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    try {
        $json = ($Value | ConvertTo-Json -Depth 20) + [Environment]::NewLine
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes($json)
        $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) }
        finally { $stream.Dispose() }
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            $replacementBackup = $Path + '.' + [Guid]::NewGuid().ToString('N') + '.replace-backup'
            try { [IO.File]::Replace($temporary, $Path, $replacementBackup) }
            finally { if (Test-Path -LiteralPath $replacementBackup) { Remove-Item -LiteralPath $replacementBackup -Force } }
        } else {
            [IO.File]::Move($temporary, $Path)
        }
    }
    finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force } }
}

function Read-KbpJson([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "JSON file is missing: $Path" }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function New-KbpOwnedLock([string]$LockPath, [string]$RunId, [string]$Token) {
    $record = [ordered]@{ schemaVersion = 1; runId = $RunId; token = $Token; createdAtUtc = [DateTime]::UtcNow.ToString('o') }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($record | ConvertTo-Json -Compress) + [Environment]::NewLine)
    try {
        $stream = [IO.File]::Open($LockPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) }
        finally { $stream.Dispose() }
    }
    catch [IO.IOException] { throw "A runtime deployment lock already exists: $LockPath" }
}

function Assert-KbpOwnedLock([string]$LockPath, [string]$RunId, [string]$Token) {
    $lock = Read-KbpJson $LockPath
    if ($lock.schemaVersion -ne 1 -or $lock.runId -cne $RunId -or $lock.token -cne $Token) {
        throw 'Runtime deployment lock ownership is ambiguous.'
    }
}

function Remove-KbpOwnedLock([string]$LockPath, [string]$RunId, [string]$Token) {
    Assert-KbpOwnedLock $LockPath $RunId $Token
    Remove-Item -LiteralPath $LockPath -Force
}

function Assert-KbpNoUnresolvedTransaction([string]$StateRoot) {
    $lock = Join-Path $StateRoot 'deployment.lock'
    if (Test-Path -LiteralPath $lock) { throw "Unresolved runtime deployment lock exists: $lock" }
    foreach ($stateFile in @(Get-ChildItem -LiteralPath $StateRoot -Filter transaction.json -File -Recurse -ErrorAction SilentlyContinue)) {
        $state = Read-KbpJson $stateFile.FullName
        if ($state.status -cne 'Restored') {
            throw "Unresolved runtime transaction exists: $($state.runId) status=$($state.status)"
        }
    }
    # Review K6: an interrupted or unrecoverable install rollback is an
    # unresolved transaction too.
    foreach ($installFile in @(Get-ChildItem -LiteralPath $StateRoot -Filter install.json -File -Recurse -ErrorAction SilentlyContinue)) {
        $record = Read-KbpJson $installFile.FullName
        if (@('RollingBack', 'RollbackRecoveryNeeded') -ccontains [string]$record.status) {
            throw "Unresolved install rollback exists: $($installFile.FullName) status=$($record.status)"
        }
    }
}

function Expand-KbpPackageToStaging {
    param([string]$PackagePath, [string]$StagingRunRoot)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    New-Item -ItemType Directory -Path $StagingRunRoot | Out-Null
    [IO.Compression.ZipFile]::ExtractToDirectory($PackagePath, $StagingRunRoot)
    $mod = Join-Path $StagingRunRoot 'KingmakerBuffPlanner'
    if (-not (Test-Path -LiteralPath (Join-Path $mod 'Info.json') -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $mod 'KingmakerBuffPlanner.dll') -PathType Leaf)) {
        throw 'Staged package does not contain the expected standalone mod.'
    }
    return $mod
}

function Enter-KbpRuntimeTransaction {
    param(
        [Parameter(Mandatory = $true)][string]$PackagePath,
        [Parameter(Mandatory = $true)][string]$KingmakerInstallDir,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$StagingRoot,
        [Parameter(Mandatory = $true)][string]$BackupRoot,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
        [switch]$FixtureMode,
        [int[]]$KnownKingmakerProcessIds,
        $CompatibilityProfile
    )
    if ($PSBoundParameters.ContainsKey('KnownKingmakerProcessIds')) {
        Assert-KbpNotRunning -KnownProcessIds $KnownKingmakerProcessIds
    } else { Assert-KbpNotRunning }
    if (-not $FixtureMode) { Assert-KbpNoForeignRuntimeLease }
    $game = (Resolve-Path -LiteralPath $KingmakerInstallDir).Path
    if (-not (Test-Path -LiteralPath (Join-Path $game 'Kingmaker.exe') -PathType Leaf)) { throw 'Kingmaker executable is missing.' }
    $package = (Resolve-Path -LiteralPath $PackagePath).Path
    foreach ($root in @($StateRoot, $StagingRoot, $BackupRoot)) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) { throw "Runtime root is missing: $root" }
    }
    if (-not $FixtureMode) {
        if (-not [IO.Path]::GetFullPath($StateRoot).TrimEnd('\').Equals($script:KbpRuntimeStateRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not [IO.Path]::GetFullPath($StagingRoot).TrimEnd('\').Equals($script:KbpRuntimeStagingRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not [IO.Path]::GetFullPath($BackupRoot).TrimEnd('\').Equals($script:KbpRuntimeBackupRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Public runtime transaction roots must be the exact project-owned lab roots.'
        }
    }
    Assert-KbpNoUnresolvedTransaction $StateRoot

    $token = [Guid]::NewGuid().ToString('N')
    $lockPath = Join-Path $StateRoot 'deployment.lock'
    $transactionRoot = Join-Path $StateRoot ('transactions\' + $RunId)
    $stagingRunRoot = Join-Path $StagingRoot $RunId
    $backupRunRoot = Join-Path $BackupRoot $RunId
    foreach ($path in @($transactionRoot, $stagingRunRoot, $backupRunRoot)) {
        if (Test-Path -LiteralPath $path) { throw "Run-owned path already exists: $path" }
    }
    New-KbpOwnedLock $lockPath $RunId $token
    New-Item -ItemType Directory -Path $transactionRoot | Out-Null
    New-Item -ItemType Directory -Path $backupRunRoot | Out-Null
    $statePath = Join-Path $transactionRoot 'transaction.json'
    $mods = Join-Path $game 'Mods'
    $profileMods = if ($null -eq $CompatibilityProfile) { @() } else { @($CompatibilityProfile.mods) }
    $originalExisted = Test-Path -LiteralPath $mods -PathType Container
    $originalManifest = if ($originalExisted) { @(Get-KbpDirectoryManifest $mods) } else { @() }
    if ($null -ne $CompatibilityProfile -and -not $originalExisted -and
        $profileMods.Count -ne 0) {
        throw 'Compatibility profile requires an existing exact fixture tree.'
    }
    # A dependency staged from an external exact copy replaces the owner's
    # installed directory only inside this transaction: its live identity
    # (files and settings) is recorded now and re-verified after restore.
    $externallyStaged = @(foreach ($expectedMod in $profileMods) {
        if ($expectedMod.PSObject.Properties.Name -notcontains 'fixtureRelativePath') { continue }
        $liveDirectory = Join-Path $mods ([string]$expectedMod.directoryName)
        $liveExisted = Test-Path -LiteralPath $liveDirectory -PathType Container
        $liveIdentity = if ($liveExisted) { Get-KbpDirectoryContentIdentity $liveDirectory } else { $null }
        [ordered]@{
            directoryName = [string]$expectedMod.directoryName
            fixtureRelativePath = [string]$expectedMod.fixtureRelativePath
            stagedVersion = [string]$expectedMod.version
            stagedDirectoryManifestSha256 = [string]$expectedMod.directoryManifestSha256
            liveExisted = $liveExisted
            liveDirectoryManifestSha256 = if ($liveExisted) { $liveIdentity.directoryManifestSha256 } else { $null }
            liveFileCount = if ($liveExisted) { $liveIdentity.fileCount } else { 0 }
            liveTotalBytes = if ($liveExisted) { [long]$liveIdentity.totalBytes } else { [long]0 }
        }
    })
    $originalBackup = Join-Path $backupRunRoot 'Mods.original'
    $stagedQuarantine = Join-Path $backupRunRoot 'Mods.staged'
    $state = [ordered]@{
        schemaVersion = 1; runId = $RunId; token = $token; status = 'Preparing'
        createdAtUtc = [DateTime]::UtcNow.ToString('o'); packagePath = $package
        packageSha256 = Get-KbpSha256 $package; gameRoot = $game; modsPath = $mods
        originalExisted = $originalExisted; originalManifest = $originalManifest
        originalBackup = $originalBackup; stagedQuarantine = $stagedQuarantine
        stagingRunRoot = $stagingRunRoot; lockPath = $lockPath
        restorationVerified = $false; stagedMutationObserved = $false
        observedStagedManifest = @()
        compatibilityProfileId = if ($null -eq $CompatibilityProfile) { 'native-only' } else { [string]$CompatibilityProfile.profileId }
        compatibilityMods = @()
        externallyStagedMods = @($externallyStaged); externallyStagedModsRestored = $false
        activatedAtUtc = $null; restoredAtUtc = $null; restorationFailure = $null
    }
    Write-KbpJsonAtomic $statePath $state
    try {
        $mod = Expand-KbpPackageToStaging -PackagePath $package -StagingRunRoot $stagingRunRoot
        $stagedMods = Join-Path $stagingRunRoot 'Mods'
        New-Item -ItemType Directory -Path $stagedMods | Out-Null
        Move-Item -LiteralPath $mod -Destination (Join-Path $stagedMods 'KingmakerBuffPlanner')
        foreach ($expectedMod in $profileMods) {
            if ($expectedMod.directoryName -ceq 'KingmakerBuffPlanner') {
                throw 'Compatibility profile cannot replace the project-owned mod.'
            }
            $sourceMod = Get-KbpCompatibilitySourcePath $expectedMod $mods
            $sourceIdentity = Assert-KbpCompatibilityModIdentity $expectedMod $sourceMod
            Copy-Item -LiteralPath $sourceMod -Destination $stagedMods -Recurse
            $stagedMod = Join-Path $stagedMods ([string]$expectedMod.directoryName)
            [void](Assert-KbpCompatibilityModIdentity $expectedMod $stagedMod)
            $state.compatibilityMods += @([ordered]@{
                directoryName = [string]$expectedMod.directoryName
                ummId = [string]$expectedMod.ummId
                version = [string]$expectedMod.version
                directoryManifestSha256 = [string]$sourceIdentity.directoryManifestSha256
                fileCount = [int]$sourceIdentity.fileCount
                totalBytes = [long]$sourceIdentity.totalBytes
            })
        }
        $sentinel = [ordered]@{ schemaVersion = 1; runId = $RunId; token = $token; statePath = $statePath }
        Write-KbpJsonAtomic (Join-Path $stagedMods '.kbp-runtime-sentinel.json') $sentinel
        $state.status = 'Prepared'
        $state.stagedManifest = @(Get-KbpDirectoryManifest $stagedMods)
        Write-KbpJsonAtomic $statePath $state

        if ($originalExisted) { Move-Item -LiteralPath $mods -Destination $originalBackup }
        $state.status = 'OriginalMoved'
        Write-KbpJsonAtomic $statePath $state
        Move-Item -LiteralPath $stagedMods -Destination $mods
        $state.status = 'Active'
        $state.activatedAtUtc = [DateTime]::UtcNow.ToString('o')
        Write-KbpJsonAtomic $statePath $state
        return $statePath
    }
    catch {
        $entryFailure = $_
        try {
            Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot $StateRoot -FixtureMode:$FixtureMode -KnownKingmakerProcessIds $KnownKingmakerProcessIds | Out-Null
        }
        catch {
            throw "Runtime entry failed and restoration also failed. Entry: $($entryFailure.Exception.Message) Restore: $($_.Exception.Message)"
        }
        throw $entryFailure
    }
}

# The owner's installed directory of every externally staged dependency is
# back with exactly the identity recorded before activation (or absent if
# it was absent).
function Assert-KbpExternallyStagedLiveRestored($State, [string]$ModsPath) {
    if ($State.PSObject.Properties.Name -notcontains 'externallyStagedMods') { return }
    foreach ($record in @($State.externallyStagedMods)) {
        $liveDirectory = Join-Path $ModsPath ([string]$record.directoryName)
        $present = Test-Path -LiteralPath $liveDirectory -PathType Container
        if ([bool]$record.liveExisted -ne $present) {
            throw "Restored installed dependency presence changed: $($record.directoryName)"
        }
        if ($present) {
            $identity = Get-KbpDirectoryContentIdentity $liveDirectory
            if ($identity.directoryManifestSha256 -cne [string]$record.liveDirectoryManifestSha256 -or
                $identity.fileCount -ne [int]$record.liveFileCount -or
                [long]$identity.totalBytes -ne [long]$record.liveTotalBytes) {
                throw "Restored installed dependency identity mismatch: $($record.directoryName)"
            }
        }
    }
    $State.externallyStagedModsRestored = $true
}

# A directory rename on one volume either happens or does not. Right after
# Kingmaker exits, a file the run wrote can still be held open for a moment
# (live run casting-ws-import-20260923-i2-01: "Access to the path ...\Mods
# is denied"), so a sharing failure is retried briefly; any other failure,
# an occupied destination or a lock that outlasts the attempts still throws
# and restoration fails closed as before.
function Move-KbpDirectoryWithRetry {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination,
        [int]$Attempts = 10,
        [int]$DelayMilliseconds = 3000)
    for ($attempt = 1; ; $attempt++) {
        try {
            Move-Item -LiteralPath $Source -Destination $Destination -ErrorAction Stop
            return $attempt
        }
        catch {
            $transient = $_.Exception -is [System.UnauthorizedAccessException] -or
                $_.Exception -is [System.IO.IOException]
            if (-not $transient -or $attempt -ge $Attempts -or (Test-Path -LiteralPath $Destination) -or
                -not (Test-Path -LiteralPath $Source)) { throw }
            Start-Sleep -Milliseconds $DelayMilliseconds
        }
    }
}

function Restore-KbpRuntimeTransaction {
    param(
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [switch]$FixtureMode,
        [int[]]$KnownKingmakerProcessIds
    )
    if ($PSBoundParameters.ContainsKey('KnownKingmakerProcessIds')) {
        Assert-KbpNotRunning -KnownProcessIds $KnownKingmakerProcessIds
    } else { Assert-KbpNotRunning }
    $statePath = Join-Path $StateRoot ('transactions\' + $RunId + '\transaction.json')
    $state = Read-KbpJson $statePath
    if ($state.schemaVersion -ne 1 -or $state.runId -cne $RunId) { throw 'Runtime transaction state identity is invalid.' }
    if ($state.status -ceq 'Restored') { return $state }
    Assert-KbpOwnedLock $state.lockPath $RunId $state.token
    $mods = [string]$state.modsPath
    $preActivationNoOp = $false
    try {
        if (Test-Path -LiteralPath $mods -PathType Container) {
            $sentinelPath = Join-Path $mods '.kbp-runtime-sentinel.json'
            if (-not (Test-Path -LiteralPath $sentinelPath -PathType Leaf)) {
                $liveManifest = @(Get-KbpDirectoryManifest $mods)
                $interruptedBeforeActivation = $state.originalExisted -and
                    -not (Test-Path -LiteralPath $state.originalBackup) -and
                    $state.status -in @('Preparing', 'RestorationFailed') -and
                    (Test-KbpManifestEqual @($state.originalManifest) $liveManifest)
                if ($interruptedBeforeActivation) {
                    $preActivationNoOp = $true
                }
                elseif (-not $state.originalExisted -and $state.status -ceq 'Prepared') {
                    throw 'Prepared state contradicts an unexpected live Mods directory.'
                }
                else { throw 'Live Mods ownership sentinel is missing; restoration refuses ambiguous state.' }
            }
            if (-not $preActivationNoOp) {
                $sentinel = Read-KbpJson $sentinelPath
                if ($sentinel.runId -cne $RunId -or $sentinel.token -cne $state.token -or $sentinel.statePath -cne $statePath) {
                    throw 'Live Mods ownership sentinel does not match transaction state.'
                }
                if (Test-Path -LiteralPath $state.stagedQuarantine) { throw 'Staged quarantine already exists.' }
                $currentStaged = @(Get-KbpDirectoryManifest $mods)
                $state.stagedMutationObserved = -not (Test-KbpManifestEqual @($state.stagedManifest) $currentStaged)
                if ($state.stagedMutationObserved) { $state.observedStagedManifest = $currentStaged }
                [void](Move-KbpDirectoryWithRetry -Source $mods -Destination $state.stagedQuarantine)
            }
        }

        if ($state.originalExisted) {
            if (-not $preActivationNoOp) {
                if (-not (Test-Path -LiteralPath $state.originalBackup -PathType Container)) {
                    throw 'Original Mods backup is missing.'
                }
                if (Test-Path -LiteralPath $mods) { throw 'Mods destination is occupied during restore.' }
                [void](Move-KbpDirectoryWithRetry -Source $state.originalBackup -Destination $mods)
                $restoredManifest = @(Get-KbpDirectoryManifest $mods)
                if (-not (Test-KbpManifestEqual @($state.originalManifest) $restoredManifest)) {
                    throw 'Restored Mods manifest/hash mismatch.'
                }
                Assert-KbpExternallyStagedLiveRestored -State $state -ModsPath $mods
            }
        }
        elseif (Test-Path -LiteralPath $mods) { throw 'Mods must remain absent because it was absent before entry.' }

        if (Test-Path -LiteralPath $state.stagedQuarantine -PathType Container) {
            $quarantineSentinel = Read-KbpJson (Join-Path $state.stagedQuarantine '.kbp-runtime-sentinel.json')
            if ($quarantineSentinel.runId -cne $RunId -or $quarantineSentinel.token -cne $state.token) {
                throw 'Staged quarantine ownership is ambiguous.'
            }
            Remove-Item -LiteralPath $state.stagedQuarantine -Recurse -Force
        }
        if (Test-Path -LiteralPath $state.stagingRunRoot -PathType Container) {
            $stagePath = [IO.Path]::GetFullPath([string]$state.stagingRunRoot)
            [void](Assert-KbpPathWithin -Path $stagePath -Root (Split-Path -Parent $stagePath))
            Remove-Item -LiteralPath $stagePath -Recurse -Force
        }
        $state.status = 'Restored'
        $state.restorationVerified = $true
        $state.restoredAtUtc = [DateTime]::UtcNow.ToString('o')
        Write-KbpJsonAtomic $statePath $state
        Remove-KbpOwnedLock $state.lockPath $RunId $state.token
        return $state
    }
    catch {
        $state.status = 'RestorationFailed'
        $state.restorationVerified = $false
        if ($state.PSObject.Properties.Name -contains 'restorationFailure') {
            $state.restorationFailure = $_.Exception.Message
        } else {
            $state | Add-Member -NotePropertyName restorationFailure -NotePropertyValue $_.Exception.Message
        }
        Write-KbpJsonAtomic $statePath $state
        throw "Runtime restoration failed closed for run $RunId. State: $statePath Cause: $($_.Exception.Message)"
    }
}
