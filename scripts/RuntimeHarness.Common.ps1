Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'Common.ps1')

$script:KbpLabRoot = 'C:\Dev\KingmakerBuffPlannerLab'
$script:KbpRuntimeStateRoot = Join-Path $script:KbpLabRoot 'runtime-state'
$script:KbpRuntimeStagingRoot = Join-Path $script:KbpLabRoot 'runtime-staging'
$script:KbpRuntimeBackupRoot = Join-Path $script:KbpLabRoot 'runtime-backups'
$script:KbpRuntimeEvidenceRoot = Join-Path $script:KbpLabRoot 'runtime-evidence'

function Assert-KbpNotRunning {
    param([int[]]$KnownProcessIds)
    $ids = if ($PSBoundParameters.ContainsKey('KnownProcessIds')) {
        @($KnownProcessIds)
    } else {
        @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue | ForEach-Object Id)
    }
    $ids = @($ids)
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
            $sourceMod = Join-Path $mods ([string]$expectedMod.directoryName)
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
    try {
        if (Test-Path -LiteralPath $mods -PathType Container) {
            $sentinelPath = Join-Path $mods '.kbp-runtime-sentinel.json'
            if (-not (Test-Path -LiteralPath $sentinelPath -PathType Leaf)) {
                if (-not $state.originalExisted -and $state.status -ceq 'Prepared') {
                    throw 'Prepared state contradicts an unexpected live Mods directory.'
                }
                throw 'Live Mods ownership sentinel is missing; restoration refuses ambiguous state.'
            }
            $sentinel = Read-KbpJson $sentinelPath
            if ($sentinel.runId -cne $RunId -or $sentinel.token -cne $state.token -or $sentinel.statePath -cne $statePath) {
                throw 'Live Mods ownership sentinel does not match transaction state.'
            }
            if (Test-Path -LiteralPath $state.stagedQuarantine) { throw 'Staged quarantine already exists.' }
            $currentStaged = @(Get-KbpDirectoryManifest $mods)
            $state.stagedMutationObserved = -not (Test-KbpManifestEqual @($state.stagedManifest) $currentStaged)
            if ($state.stagedMutationObserved) { $state.observedStagedManifest = $currentStaged }
            Move-Item -LiteralPath $mods -Destination $state.stagedQuarantine
        }

        if ($state.originalExisted) {
            if (-not (Test-Path -LiteralPath $state.originalBackup -PathType Container)) {
                throw 'Original Mods backup is missing.'
            }
            if (Test-Path -LiteralPath $mods) { throw 'Mods destination is occupied during restore.' }
            Move-Item -LiteralPath $state.originalBackup -Destination $mods
            $restoredManifest = @(Get-KbpDirectoryManifest $mods)
            if (-not (Test-KbpManifestEqual @($state.originalManifest) $restoredManifest)) {
                throw 'Restored Mods manifest/hash mismatch.'
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
