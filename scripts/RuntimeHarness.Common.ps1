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

# Double-checked cross-project lease, run once this project's own lock is
# held: a foreign lease or a game process that appeared after the first
# checks refuses the operation before anything is created or moved, and this
# project's lock is released again. The other lab checks this lab's lock
# before it takes its lease, so this narrows the remaining race to that
# lab's own check-then-lease step.
function Confirm-KbpLockedWithoutForeignLease {
    param(
        [Parameter(Mandatory = $true)][string]$LockPath,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$Token,
        [string[]]$LeasePaths = $script:KbpForeignRuntimeLeases,
        [switch]$SkipForeignLease,
        [int[]]$KnownProcessIds)
    try {
        if (-not $SkipForeignLease) { Assert-KbpNoForeignRuntimeLease -LeasePaths $LeasePaths }
        if ($PSBoundParameters.ContainsKey('KnownProcessIds')) {
            Assert-KbpNotRunning -KnownProcessIds $KnownProcessIds
        } else { Assert-KbpNotRunning }
    }
    catch {
        Remove-KbpOwnedLock $LockPath $RunId $Token
        throw
    }
}

# The game's own registry key: Unity PlayerPrefs (screen size and mode) and
# the game's settings. A display-mode run restores it byte-exact.
$script:KbpGameRegistryKey = 'HKCU:\Software\Owlcat Games\Pathfinder Kingmaker'

function Get-KbpRegistryCanonical([string]$Kind, $Value) {
    $data = if ($Value -is [byte[]]) { [Convert]::ToBase64String([byte[]]$Value) }
        elseif ($Value -is [string[]]) { (@($Value) | ForEach-Object {
            [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes([string]$_)) }) -join ',' }
        else { [string]$Value }
    return $Kind + ':' + $data
}

# Every value under one registry key, with its kind and exact data, in a
# comparable canonical form.
function Get-KbpRegistryValueSnapshot {
    param([Parameter(Mandatory = $true)][string]$KeyPath)
    $snapshot = [ordered]@{}
    $key = Get-Item -LiteralPath $KeyPath -ErrorAction Stop
    try {
        foreach ($name in @($key.GetValueNames() | Sort-Object)) {
            $kind = $key.GetValueKind($name)
            $value = $key.GetValue($name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
            $snapshot[$name] = [pscustomobject]@{ kind = [string]$kind; value = $value
                canonical = Get-KbpRegistryCanonical ([string]$kind) $value }
        }
    }
    finally { $key.Dispose() }
    return $snapshot
}

function Compare-KbpRegistrySnapshot {
    param([Parameter(Mandatory = $true)]$Before, [Parameter(Mandatory = $true)]$After)
    $differences = New-Object System.Collections.Generic.List[string]
    foreach ($name in @($Before.Keys)) {
        if (-not $After.Contains($name)) { $differences.Add('removed:' + $name) }
        elseif ($After[$name].canonical -cne $Before[$name].canonical) { $differences.Add('changed:' + $name) }
    }
    foreach ($name in @($After.Keys)) { if (-not $Before.Contains($name)) { $differences.Add('added:' + $name) } }
    # Plain output: callers wrap it in @() (an empty result is no output).
    return $differences.ToArray()
}

# Puts every value of the key back exactly as in the snapshot (changed and
# removed values rewritten with their kind, added values deleted), then
# verifies; returns what it restored.
function Restore-KbpRegistryValues {
    param([Parameter(Mandatory = $true)][string]$KeyPath, [Parameter(Mandatory = $true)]$Snapshot)
    $differences = @(Compare-KbpRegistrySnapshot -Before $Snapshot -After (Get-KbpRegistryValueSnapshot -KeyPath $KeyPath))
    if ($differences.Count -ne 0) {
        if ($KeyPath -notmatch '^HKCU:\\(.+)$') { throw "Only HKCU keys are restored: $KeyPath" }
        $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($Matches[1], $true)
        if ($null -eq $key) { throw "Registry key is missing: $KeyPath" }
        try {
            foreach ($difference in $differences) {
                $name = $difference.Substring($difference.IndexOf(':') + 1)
                if ($difference.StartsWith('added:')) { $key.DeleteValue($name, $false) }
                else {
                    $key.SetValue($name, $Snapshot[$name].value,
                        [Microsoft.Win32.RegistryValueKind]([string]$Snapshot[$name].kind))
                }
            }
        }
        finally { $key.Dispose() }
    }
    $remaining = @(Compare-KbpRegistrySnapshot -Before $Snapshot -After (Get-KbpRegistryValueSnapshot -KeyPath $KeyPath))
    if ($remaining.Count -ne 0) { throw 'Registry restoration mismatch: ' + ($remaining -join ', ') }
    # Plain output: what was restored (nothing when the key was unchanged).
    return $differences
}

# A display-mode run's registry snapshot, kept beside its transaction: what a
# blocked or interrupted restoration needs to finish later (Restore-Local.ps1
# -RunId), and what keeps any later run or install from starting before it
# did (Assert-KbpNoUnresolvedTransaction).
function Save-KbpRegistrySnapshotFile {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$KeyPath,
        [Parameter(Mandatory = $true)]$Snapshot, [Parameter(Mandatory = $true)][string]$RunId)
    $values = [ordered]@{}
    foreach ($name in @($Snapshot.Keys)) {
        $entry = $Snapshot[$name]
        $data = if ($entry.value -is [byte[]]) { [Convert]::ToBase64String([byte[]]$entry.value) }
            elseif ($entry.value -is [string[]]) { ,@($entry.value) }
            else { [string]$entry.value }
        $values[$name] = [ordered]@{ kind = [string]$entry.kind; data = $data }
    }
    Write-KbpJsonAtomic $Path ([ordered]@{
        schemaVersion = 1; runId = $RunId; keyPath = $KeyPath; restored = $false
        savedAtUtc = [DateTime]::UtcNow.ToString('o'); values = $values
    })
}

function Read-KbpRegistrySnapshotFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    $file = Read-KbpJson $Path
    if ([int]$file.schemaVersion -ne 1 -or [string]::IsNullOrWhiteSpace([string]$file.keyPath)) {
        throw "Registry snapshot file is invalid: $Path"
    }
    $snapshot = [ordered]@{}
    foreach ($property in @($file.values.PSObject.Properties)) {
        $kind = [string]$property.Value.kind
        $data = $property.Value.data
        $value = switch ($kind) {
            'Binary' { ,[Convert]::FromBase64String([string]$data) }
            'DWord' { [int][string]$data }
            'QWord' { [long][string]$data }
            'MultiString' { ,[string[]]@($data) }
            default { [string]$data }
        }
        $snapshot[$property.Name] = [pscustomobject]@{ kind = $kind; value = $value
            canonical = Get-KbpRegistryCanonical $kind $value }
    }
    return [pscustomobject]@{ keyPath = [string]$file.keyPath; restored = [bool]$file.restored
        runId = [string]$file.runId; snapshot = $snapshot }
}

function Complete-KbpRegistrySnapshotFile {
    param([Parameter(Mandatory = $true)][string]$Path, [string[]]$RestoredValues)
    $file = Read-KbpJson $Path
    $file.restored = $true
    $file | Add-Member -NotePropertyName restoredAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force
    $file | Add-Member -NotePropertyName restoredValues -NotePropertyValue @($RestoredValues) -Force
    Write-KbpJsonAtomic $Path $file
}

# Puts back a saved snapshot that is not restored yet; the game must not run.
function Restore-KbpRegistrySnapshotFile {
    param([Parameter(Mandatory = $true)][string]$Path, [int[]]$KnownKingmakerProcessIds)
    $saved = Read-KbpRegistrySnapshotFile -Path $Path
    if ($saved.restored) { return }
    if ($PSBoundParameters.ContainsKey('KnownKingmakerProcessIds')) {
        Assert-KbpNotRunning -KnownProcessIds $KnownKingmakerProcessIds
    } else { Assert-KbpNotRunning }
    $restored = @(Restore-KbpRegistryValues -KeyPath $saved.keyPath -Snapshot $saved.snapshot)
    Complete-KbpRegistrySnapshotFile -Path $Path -RestoredValues $restored
    return $restored
}

# Waits (bounded) until no foreign runtime lease is held; throws otherwise.
function Wait-KbpNoForeignRuntimeLease {
    param([string[]]$LeasePaths = $script:KbpForeignRuntimeLeases,
        [ValidateRange(0, 3600)][int]$TimeoutSeconds = 600, [ValidateRange(1, 60)][int]$PollSeconds = 5)
    $clock = [Diagnostics.Stopwatch]::StartNew()
    while ($true) {
        $held = @($LeasePaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_) })
        if ($held.Count -eq 0) { return }
        if ($clock.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
            throw "Another project's Kingmaker runtime lease is still active after $TimeoutSeconds s: $($held -join ', '). The restoration waits for it (Restore-Local.ps1 -RunId finishes it)."
        }
        Start-Sleep -Seconds $PollSeconds
    }
}

# The other lab's activity signature for a live comparison window: its
# lease files, the entries of its state folders (each of its runs adds one)
# and how many Kingmaker processes run. Equal quiet signatures around a
# window mean the other lab did not start, run or end a run inside it.
function Get-KbpForeignActivitySignature {
    param([string[]]$LeasePaths = $script:KbpForeignRuntimeLeases,
        [scriptblock]$GameCount = { @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue).Count })
    $parts = New-Object System.Collections.Generic.List[string]
    foreach ($lease in @($LeasePaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $parts.Add('lease=' + [bool](Test-Path -LiteralPath $lease))
        $state = Split-Path -Parent $lease
        $entries = if (Test-Path -LiteralPath $state -PathType Container) {
            @(Get-ChildItem -LiteralPath $state -Force -ErrorAction SilentlyContinue | ForEach-Object Name | Sort-Object) -join ','
        } else { '' }
        $parts.Add('entries=' + $entries)
    }
    $parts.Add('game=' + [int](& $GameCount))
    return ($parts -join ';')
}

# A live-state purity window. The owner's other lab rewrites the shared Mods
# folder under its own lease, so a comparison it overlapped proves nothing
# either way. The action runs between two snapshots of the targets. A
# change in a window the other lab left quiet fails at once. A change in a
# window it overlapped is inconclusive: the case waits for quiet and runs
# again, at most $Attempts times, and fails if it never gets a quiet window.
function Invoke-KbpLivePurityWindow {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string[]]$Targets,
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [ValidateRange(1, 5)][int]$Attempts = 3,
        [string[]]$LeasePaths = $script:KbpForeignRuntimeLeases,
        [scriptblock]$GameCount = { @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue).Count },
        [ValidateRange(0, 3600)][int]$LeaseWaitSeconds = 1800)
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        Wait-KbpNoForeignRuntimeLease -LeasePaths $LeasePaths -TimeoutSeconds $LeaseWaitSeconds
        $signatureBefore = Get-KbpForeignActivitySignature -LeasePaths $LeasePaths -GameCount $GameCount
        $before = @{}
        foreach ($target in $Targets) {
            $before[$target] = if (Test-Path -LiteralPath $target -PathType Container) {
                @(Get-KbpDirectoryManifest $target)
            } else { $null }
        }
        & $Action
        $changed = @()
        foreach ($target in $Targets) {
            $after = if (Test-Path -LiteralPath $target -PathType Container) {
                @(Get-KbpDirectoryManifest $target)
            } else { $null }
            if (($null -eq $before[$target]) -ne ($null -eq $after) -or
                ($null -ne $after -and -not (Test-KbpManifestEqual @($before[$target]) @($after)))) {
                $changed += @($target)
            }
        }
        $signatureAfter = Get-KbpForeignActivitySignature -LeasePaths $LeasePaths -GameCount $GameCount
        if (@($changed).Count -eq 0) { return }
        $quiet = $signatureBefore -ceq $signatureAfter -and $signatureBefore -notmatch 'lease=True' -and
            $signatureBefore -match ';game=0$'
        if ($quiet) { throw "$Label changed: $($changed -join ', ')" }
        Write-Host "${Label}: the other lab was active during the comparison; the case runs again once it is quiet."
    }
    throw "$Label changed in each of $Attempts comparisons, each overlapped by the other lab."
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

# No Kingmaker may run from this game root. A game running from another
# installation (for example the owner's other lab testing an isolated copy)
# holds no file here; a process whose path cannot be read blocks (fail
# closed).
function Assert-KbpGameRootNotRunning {
    param([Parameter(Mandatory = $true)][string]$GameRoot, [object[]]$Processes)
    $root = [IO.Path]::GetFullPath($GameRoot).TrimEnd('\') + '\'
    $candidates = if ($PSBoundParameters.ContainsKey('Processes')) { @($Processes) }
        else { @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue) }
    $blocking = @($candidates | Where-Object { $null -ne $_ } | Where-Object {
        $path = $null
        try { $path = [string]$_.Path } catch { $path = $null }
        [string]::IsNullOrEmpty($path) -or $path.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)
    })
    if ($blocking.Count -ne 0) {
        throw "Pathfinder: Kingmaker is running from $root (PID(s): $(@($blocking | ForEach-Object { $_.Id }) -join ', '))."
    }
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
    # A display-mode run whose game registry was not restored yet.
    foreach ($snapshotFile in @(Get-ChildItem -LiteralPath $StateRoot -Filter display-registry.json -File -Recurse -ErrorAction SilentlyContinue)) {
        $snapshotState = Read-KbpJson $snapshotFile.FullName
        if (-not [bool]$snapshotState.restored) {
            throw "Unrestored game registry snapshot exists: $($snapshotFile.FullName) (Restore-Local.ps1 -RunId $($snapshotState.runId) restores it)."
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
        $CompatibilityProfile,
        # The bounded retry of the two directory moves (a transient sharing
        # lock on the live Mods folder refused the first move of live run
        # casting-qual-select-20260924-45bff28-01); tests shorten it.
        [ValidateRange(1, 20)][int]$MoveAttempts = 10,
        [ValidateRange(0, 10000)][int]$MoveDelayMilliseconds = 3000
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
    if ($PSBoundParameters.ContainsKey('KnownKingmakerProcessIds')) {
        Confirm-KbpLockedWithoutForeignLease -LockPath $lockPath -RunId $RunId -Token $token `
            -SkipForeignLease:$FixtureMode -KnownProcessIds $KnownKingmakerProcessIds
    } else {
        Confirm-KbpLockedWithoutForeignLease -LockPath $lockPath -RunId $RunId -Token $token -SkipForeignLease:$FixtureMode
    }
    $statePath = Join-Path $transactionRoot 'transaction.json'
    try {
    New-Item -ItemType Directory -Path $transactionRoot | Out-Null
    New-Item -ItemType Directory -Path $backupRunRoot | Out-Null
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
    }
    catch {
        # Before the transaction state exists nothing was moved or staged
        # (the live Mods was only read): the run's empty directories go and
        # the lock is released, so a failed read never leaves an unresolved
        # lock without a record to restore from.
        $preStateFailure = $_
        if (-not (Test-Path -LiteralPath $statePath)) {
            foreach ($owned in @($backupRunRoot, $transactionRoot)) {
                if ((Test-Path -LiteralPath $owned -PathType Container) -and
                    @(Get-ChildItem -LiteralPath $owned -Force).Count -eq 0) {
                    Remove-Item -LiteralPath $owned -Force
                }
            }
            try { Remove-KbpOwnedLock $lockPath $RunId $token }
            catch {
                throw ("Runtime entry failed before its state existed (" + $preStateFailure.Exception.Message +
                    ") and its lock could not be released: " + $_.Exception.Message)
            }
        }
        throw $preStateFailure
    }
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

        $originalMoveAttempts = 0
        if ($originalExisted) {
            $originalMoveAttempts = Move-KbpDirectoryWithRetry -Source $mods -Destination $originalBackup `
                -Attempts $MoveAttempts -DelayMilliseconds $MoveDelayMilliseconds
        }
        $state.status = 'OriginalMoved'
        $state.originalMoveAttempts = [int]$originalMoveAttempts
        Write-KbpJsonAtomic $statePath $state
        [void](Move-KbpDirectoryWithRetry -Source $stagedMods -Destination $mods `
            -Attempts $MoveAttempts -DelayMilliseconds $MoveDelayMilliseconds)
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
    # Serialized with the owner's other lab (review C3): this run's Mods
    # folder is never moved while that lab's runtime lease is held.
    if (-not $FixtureMode) { Wait-KbpNoForeignRuntimeLease }
    $mods = [string]$state.modsPath
    $preActivationNoOp = $false
    try {
        if (Test-Path -LiteralPath $mods -PathType Container) {
            $sentinelPath = Join-Path $mods '.kbp-runtime-sentinel.json'
            if (-not (Test-Path -LiteralPath $sentinelPath -PathType Leaf)) {
                $liveManifest = @(Get-KbpDirectoryManifest $mods)
                # Prepared included: the original's move can fail after the
                # staged tree is ready (live run
                # casting-qual-select-20260924-45bff28-01), leaving the
                # original in place and no backup.
                $interruptedBeforeActivation = $state.originalExisted -and
                    -not (Test-Path -LiteralPath $state.originalBackup) -and
                    $state.status -in @('Preparing', 'Prepared', 'RestorationFailed') -and
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
            # A staged tree that changed during the run is evidence (the
            # change may be another lab's): it is kept, never deleted.
            if ([bool]$state.stagedMutationObserved) {
                $state | Add-Member -NotePropertyName stagedQuarantineKept -NotePropertyValue $true -Force
            }
            else { Remove-Item -LiteralPath $state.stagedQuarantine -Recurse -Force }
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
