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
        [int[]]$KnownProcessIds,
        # Final review C7: an operation on one game root (the install
        # rollback) is blocked only by a game running from that root.
        [string]$GameRoot,
        [object[]]$Processes,
        # Re-review (harness): a runtime entry re-checks the fixture lock once
        # its own lock is held, as a fixture operation re-checks this lock
        # once it holds its own, so the two can never both proceed.
        [string]$FixtureLockPath)
    try {
        Assert-KbpFixtureLockAbsent $FixtureLockPath
        if (-not $SkipForeignLease) { Assert-KbpNoForeignRuntimeLease -LeasePaths $LeasePaths }
        if (-not [string]::IsNullOrEmpty($GameRoot)) {
            if ($PSBoundParameters.ContainsKey('Processes')) {
                Assert-KbpGameRootNotRunning -GameRoot $GameRoot -Processes $Processes
            } else { Assert-KbpGameRootNotRunning -GameRoot $GameRoot }
        }
        elseif ($PSBoundParameters.ContainsKey('KnownProcessIds')) {
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

# The game's saved graphics options override Unity's -screen-fullscreen
# command line (a windowed run launched fullscreen in
# casting-graph-qual-1080-01/-02 while claiming the windowed mode), so a
# windowed run sets the Unity screen-manager values itself, for this run
# only: the launcher snapshots the key first and restores the owner's
# values exactly afterwards (Restore-KbpRegistryValues above, verified by
# the launcher's registry-restoration check).
function Set-KbpWindowedScreenValues {
    param(
        [Parameter(Mandatory = $true)][string]$KeyPath,
        [Parameter(Mandatory = $true)][string]$Size)
    if ($Size -notmatch '^([0-9]+)x([0-9]+)$') { throw "Display size is invalid: $Size" }
    $subKey = $KeyPath -replace '^HKCU:\\', ''
    $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($subKey)
    try {
        $key.SetValue('Screenmanager Resolution Width_h182942802', [int]$Matches[1],
            [Microsoft.Win32.RegistryValueKind]::DWord)
        $key.SetValue('Screenmanager Resolution Height_h2627697771', [int]$Matches[2],
            [Microsoft.Win32.RegistryValueKind]::DWord)
        $key.SetValue('Screenmanager Fullscreen mode_h3630240806', 0,
            [Microsoft.Win32.RegistryValueKind]::DWord)
        $key.SetValue('Screenmanager Resolution Use Native_h1405027254', 0,
            [Microsoft.Win32.RegistryValueKind]::DWord)
        # The game's own options layer: its DisplayMode enum is serialized
        # as one ASCII character ('0' Windowed, '1' FullScreen - the value
        # observed on this machine while fullscreen); the Unity values
        # above alone are overridden at boot.
        $key.SetValue('DisplayMode_h1925482108', [byte[]](48),
            [Microsoft.Win32.RegistryValueKind]::Binary)
    }
    finally { $key.Dispose() }
}

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

# A windowed-size run does not intentionally change the Windows display
# mode. It nevertheless snapshots the relevant physical geometry while the
# runtime lock is held and verifies it after Kingmaker exits. A mismatch is
# deliberately not rewritten: it may be a newer user display change, so the
# durable pending snapshot blocks later runs for explicit review.
function Get-KbpPhysicalDisplaySnapshotValues {
    param([Parameter(Mandatory = $true)]$Snapshot)
    $required = @('primarySize', 'primaryWidth', 'primaryHeight', 'virtualLeft',
        'virtualTop', 'virtualWidth', 'virtualHeight', 'monitorCount')
    $values = [ordered]@{}
    foreach ($name in $required) {
        $property = $Snapshot.PSObject.Properties[$name]
        if ($null -eq $property) { throw "Physical display snapshot is missing $name." }
        $values[$name] = if ($name -ceq 'primarySize') { [string]$property.Value }
            else { [int]$property.Value }
    }
    if ([string]$values.primarySize -cne
        ([string]$values.primaryWidth + 'x' + [string]$values.primaryHeight) -or
        [int]$values.primaryWidth -le 0 -or [int]$values.primaryHeight -le 0 -or
        [int]$values.virtualWidth -le 0 -or [int]$values.virtualHeight -le 0 -or
        [int]$values.monitorCount -le 0) {
        throw 'Physical display snapshot has invalid geometry.'
    }
    return [pscustomobject]$values
}

function Compare-KbpPhysicalDisplaySnapshot {
    param([Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After)
    $left = Get-KbpPhysicalDisplaySnapshotValues $Before
    $right = Get-KbpPhysicalDisplaySnapshotValues $After
    $differences = New-Object System.Collections.Generic.List[string]
    foreach ($name in @('primarySize', 'primaryWidth', 'primaryHeight', 'virtualLeft',
            'virtualTop', 'virtualWidth', 'virtualHeight', 'monitorCount')) {
        if ([string]$left.$name -cne [string]$right.$name) {
            $differences.Add($name + ':' + [string]$left.$name + '->' + [string]$right.$name)
        }
    }
    return $differences.ToArray()
}

function Save-KbpPhysicalDisplaySnapshotFile {
    param([Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Snapshot,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$ExpectedWindowSize)
    $values = Get-KbpPhysicalDisplaySnapshotValues $Snapshot
    Write-KbpJsonAtomic $Path ([ordered]@{
        schemaVersion = 1; kind = 'kbp-windowed-display-verification'; runId = $RunId
        expectedWindowSize = $ExpectedWindowSize; restored = $false
        savedAtUtc = [DateTime]::UtcNow.ToString('o'); original = $values
    })
}

function Read-KbpPhysicalDisplaySnapshotFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    $file = Read-KbpJson $Path
    if ([int]$file.schemaVersion -ne 1 -or
        [string]$file.kind -cne 'kbp-windowed-display-verification' -or
        [string]::IsNullOrWhiteSpace([string]$file.runId) -or
        [string]$file.expectedWindowSize -notmatch '^[0-9]{3,5}x[0-9]{3,5}$') {
        throw "Physical display snapshot file is invalid: $Path"
    }
    [void](Get-KbpPhysicalDisplaySnapshotValues $file.original)
    return $file
}

function Confirm-KbpPhysicalDisplaySnapshotFile {
    param([Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$CurrentSnapshot,
        [switch]$Reverify)
    $file = Read-KbpPhysicalDisplaySnapshotFile -Path $Path
    if ([bool]$file.restored -and -not $Reverify) { return $file }
    $current = Get-KbpPhysicalDisplaySnapshotValues $CurrentSnapshot
    $differences = @(Compare-KbpPhysicalDisplaySnapshot -Before $file.original -After $current)
    if ($differences.Count -ne 0) {
        throw ("Physical display state changed during run $($file.runId): " +
            ($differences -join ', ') + '. Refusing to overwrite a potentially newer user change; ' +
            'the pending snapshot must be reviewed before another run.')
    }
    $file.restored = $true
    $file | Add-Member -NotePropertyName verifiedAtUtc `
        -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force
    $file | Add-Member -NotePropertyName observedAfter -NotePropertyValue $current -Force
    Write-KbpJsonAtomic $Path $file
    return $file
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
    $enumerated = @(Get-ChildItem -LiteralPath $root -File -Recurse -Force | Sort-Object FullName)
    if (-not ('KbpRuntimeHarness.RuntimeManifestReader' -as [type])) {
        Add-Type -Path (Join-Path $PSScriptRoot 'RuntimeManifestReader.cs') -ErrorAction Stop
    }
    $files = @([KbpRuntimeHarness.RuntimeManifestReader]::Read($root, [IO.FileInfo[]]$enumerated) | ForEach-Object {
            [ordered]@{
                path = $_.Path
                kind = 'file'
                length = $_.Length
                lastWriteTimeUtc = $_.LastWriteTimeUtc
                sha256 = $_.Sha256
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

# Writes the runtime request and returns the decoded snapshot of the bytes just written: the launcher keeps it in memory
# and checks the result against it, never against a later read of the evidence file.
function Save-KbpRuntimeRequest([string]$Path, $Request) {
    # The retained request is decoded from the exact serialization passed to the atomic writer (never a reread of the
    # destination, which another process could replace, and independent of the reader's default encoding).
    $json = ($Request | ConvertTo-Json -Depth 20) + [Environment]::NewLine
    Write-KbpTextAtomic $Path $json
    return ($json | ConvertFrom-Json)
}

function Write-KbpJsonAtomic([string]$Path, $Value) {
    Write-KbpTextAtomic $Path (($Value | ConvertTo-Json -Depth 20) + [Environment]::NewLine)
}

function Write-KbpTextAtomic([string]$Path, [string]$json) {
    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        throw "Atomic JSON destination directory is missing: $directory"
    }
    $temporary = Join-Path $directory ('.' + [IO.Path]::GetFileName($Path) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    try {
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

# --- Kingmaker native-test reservation, protocol 1 (docs/NATIVE-RESERVATION.md) ---
# A machine-wide convention the participating Kingmaker labs each implement on
# their own; it names no other lab and needs none to exist. This lab's rule is
# deliberately small and fails closed: a participating tool acquires the
# reservation before any mutable read and releases it last, only after its
# own verification succeeded. A refusal before any shared change releases it;
# anything unresolved after a shared change keeps it for the owner
# (docs/VALIDATION-POLICY.md, manual procedure). Nothing is adopted or resumed
# automatically.
$script:KbpReservationProtocol = 'kingmaker-native-test-reservation/1'
$script:KbpReservationLab = 'KingmakerBuffPlanner'
$script:KbpReservationMutexName = 'Global\KingmakerNativeTest.Reservation.v1'
$script:KbpReservationRecordPath = Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json'

# The production convention, unless the offline-test seam
# KBP_TEST_RESERVATION_ROOT names a private reservation folder inside this
# checkout's artifacts\runtime-harness-tests. Production entrypoints never set
# it; tests set it and restore it in finally. Private mode is refused unless
# every mutable target of the operation lies in a harness test root too.
function Get-KbpReservationConvention {
    param([string[]]$Targets = @())
    $privateRoot = [string]$env:KBP_TEST_RESERVATION_ROOT
    if ([string]::IsNullOrWhiteSpace($privateRoot)) {
        return [pscustomobject]@{ MutexName = $script:KbpReservationMutexName; RecordPath = $script:KbpReservationRecordPath; Private = $false }
    }
    if (-not (Test-KbpHarnessTestPath $privateRoot)) {
        throw "KBP_TEST_RESERVATION_ROOT is not inside this checkout's artifacts\runtime-harness-tests: $privateRoot"
    }
    foreach ($target in @($Targets | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) {
        if (-not (Test-KbpHarnessTestPath ([string]$target))) {
            throw "A private test reservation is refused for a shared target: $target"
        }
    }
    $full = [IO.Path]::GetFullPath($privateRoot).TrimEnd('\')
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $digest = -join ($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($full.ToLowerInvariant()))[0..7] | ForEach-Object { $_.ToString('x2') }) }
    finally { $sha.Dispose() }
    return [pscustomobject]@{ MutexName = 'Global\KbpHarnessTestReservation-' + $digest; RecordPath = Join-Path $full 'reservation.v1.json'; Private = $true }
}

# Runs $Body while holding the reservation mutex: a short critical section
# around the lease record, never held across a transaction. An abandoned mutex
# is owned all the same; the record stays the authority.
function Invoke-KbpUnderReservationMutex($Convention, [scriptblock]$Body) {
    $mutex = New-Object System.Threading.Mutex($false, [string]$Convention.MutexName)
    $owned = $false
    try {
        try { $owned = $mutex.WaitOne([TimeSpan]::FromSeconds(10)) }
        catch [System.Threading.AbandonedMutexException] { $owned = $true }
        if (-not $owned) { throw "The Kingmaker native-test reservation mutex stayed busy: $($Convention.MutexName)" }
        return (& $Body)
    }
    finally {
        if ($owned) { $mutex.ReleaseMutex() }
        $mutex.Dispose()
    }
}

# Whether a JSON object's top-level keys repeat (PowerShell's conversion would
# silently keep the last value).
function Test-KbpJsonDuplicateKeys([string]$Text) {
    $keys = New-Object 'System.Collections.Generic.HashSet[string]'
    $depth = 0; $i = 0; $expectKey = $false
    while ($i -lt $Text.Length) {
        $c = $Text[$i]
        if ($c -eq '"') {
            $builder = New-Object System.Text.StringBuilder
            $i++
            while ($i -lt $Text.Length -and $Text[$i] -ne '"') {
                if ($Text[$i] -eq '\' -and $i + 1 -lt $Text.Length) { [void]$builder.Append($Text[$i]); $i++ }
                [void]$builder.Append($Text[$i]); $i++
            }
            $j = $i + 1
            while ($j -lt $Text.Length -and [char]::IsWhiteSpace($Text[$j])) { $j++ }
            if ($depth -eq 1 -and $expectKey -and $j -lt $Text.Length -and $Text[$j] -eq ':') {
                if (-not $keys.Add($builder.ToString())) { return $true }
            }
            $expectKey = $false
        }
        elseif ($c -eq '{' -or $c -eq '[') { $depth++; $expectKey = ($c -eq '{' -and $depth -eq 1) }
        elseif ($c -eq '}' -or $c -eq ']') { $depth-- }
        elseif ($c -eq ',' -and $depth -eq 1) { $expectKey = $true }
        $i++
    }
    return $false
}

# The lease record, or $null when the resources are free. Valid UTF-8 (an
# optional UTF-8 BOM aside), one JSON object without repeated keys, and the
# documented common fields: protocol (exact), lab, runId, token (any non-empty
# string), holderProcessId (positive integer) and holderStartUtc. This lab's
# own records must also carry its 32-hex token and its state root. Anything
# else is ambiguous: it refuses and is never changed.
function Read-KbpReservationRecord($Convention) {
    $path = [string]$Convention.RecordPath
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    $record = $null
    try {
        $bytes = [IO.File]::ReadAllBytes($path)
        $offset = if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { 3 } else { 0 }
        $text = [Text.UTF8Encoding]::new($false, $true).GetString($bytes, $offset, $bytes.Length - $offset)
        if (-not (Test-KbpJsonDuplicateKeys $text)) { $record = $text | ConvertFrom-Json }
    }
    catch { $record = $null }
    $valid = $null -ne $record -and $record -is [pscustomobject]
    if ($valid) {
        foreach ($field in @('protocol', 'lab', 'runId', 'token', 'holderProcessId', 'holderStartUtc')) {
            if ($null -eq $record.PSObject.Properties[$field]) { $valid = $false }
        }
    }
    $valid = $valid -and $record.protocol -is [string] -and [string]$record.protocol -ceq $script:KbpReservationProtocol -and
        $record.lab -is [string] -and -not [string]::IsNullOrEmpty([string]$record.lab) -and
        $record.runId -is [string] -and
        $record.token -is [string] -and -not [string]::IsNullOrEmpty([string]$record.token) -and
        ($record.holderProcessId -is [int] -or $record.holderProcessId -is [long]) -and [long]$record.holderProcessId -gt 0 -and
        $record.holderStartUtc -is [string]
    if ($valid -and [string]$record.lab -ceq $script:KbpReservationLab) {
        $valid = [string]$record.token -cmatch '^[0-9a-f]{32}$' -and $null -ne $record.PSObject.Properties['stateRoot'] -and
            $record.stateRoot -is [string] -and -not [string]::IsNullOrEmpty([string]$record.stateRoot)
    }
    if (-not $valid) {
        throw "The Kingmaker native-test reservation record is unreadable, malformed or of an unknown protocol; it is left for the owner: $path"
    }
    return $record
}

# Reserves the shared Kingmaker native-test resources for one run of this lab,
# or refuses naming the holder. A present record is a reservation whoever
# wrote it and whether or not its writer still runs.
function Enter-KbpNativeReservation {
    param(
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$Purpose,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [string[]]$Targets = @())
    $leaseConvention = Get-KbpReservationConvention -Targets (@($StateRoot) + @($Targets))
    $stateFull = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    return Invoke-KbpUnderReservationMutex $leaseConvention {
        $directory = Split-Path -Parent $leaseConvention.RecordPath
        [void][IO.Directory]::CreateDirectory($directory)
        if (((Get-Item -LiteralPath $directory -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "The Kingmaker native-test reservation folder is a link: $directory"
        }
        $current = Read-KbpReservationRecord $leaseConvention
        if ($null -ne $current) {
            throw ("Kingmaker native-test resources are reserved by $($current.lab) run $($current.runId) " +
                "(process $($current.holderProcessId)); $($leaseConvention.RecordPath)")
        }
        $lease = [ordered]@{
            protocol = $script:KbpReservationProtocol; lab = $script:KbpReservationLab; runId = $RunId; purpose = $Purpose
            token = [Guid]::NewGuid().ToString('N'); holderProcessId = $PID
            holderStartUtc = (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o'); host = $env:COMPUTERNAME
            createdUtc = [DateTime]::UtcNow.ToString('o'); stateRoot = $stateFull
        }
        $temporary = $leaseConvention.RecordPath + '.' + $lease.token + '.tmp'
        $text = [Text.UTF8Encoding]::new($false).GetBytes(($lease | ConvertTo-Json))
        $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $stream.Write($text, 0, $text.Length); $stream.Flush($true) } finally { $stream.Dispose() }
        [IO.File]::Move($temporary, $leaseConvention.RecordPath)
        [pscustomobject]$lease
    }
}

# Refuses unless the record is this lab's reservation for exactly this run,
# token and state root. A lost, replaced or unreadable record stops the caller
# before its next shared change; the record is left alone.
function Assert-KbpNativeReservation {
    param([string]$RunId, [string]$Token, [string]$StateRoot, [string[]]$Targets = @())
    $leaseConvention = Get-KbpReservationConvention -Targets (@($StateRoot) + @($Targets))
    $current = $null
    $readFailure = $null
    try { $current = Read-KbpReservationRecord $leaseConvention } catch { $readFailure = 'an unreadable or malformed record' }
    $stateFull = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    if ($null -eq $readFailure -and $null -ne $current -and [string]$current.lab -ceq $script:KbpReservationLab -and
        [string]$current.token -ceq $Token -and [string]$current.runId -ceq $RunId -and
        ([string]$current.stateRoot).Equals($stateFull, [StringComparison]::OrdinalIgnoreCase)) { return }
    $holder = if ($null -ne $readFailure) { $readFailure } elseif ($null -eq $current) { 'no record' } else { "$($current.lab) run $($current.runId)" }
    throw ("Run $RunId does not hold its Kingmaker native-test reservation (now: $holder; $($leaseConvention.RecordPath)). " +
        "Nothing more was changed; the owner resolves the transaction (docs/VALIDATION-POLICY.md).")
}

# Releases this run's reservation, under the mutex and only while it is still
# exactly this run's; otherwise it is left alone and the release fails.
function Exit-KbpNativeReservation {
    param([string]$RunId, [string]$Token, [string]$StateRoot, [string[]]$Targets = @())
    $leaseConvention = Get-KbpReservationConvention -Targets (@($StateRoot) + @($Targets))
    Invoke-KbpUnderReservationMutex $leaseConvention {
        Assert-KbpNativeReservation -RunId $RunId -Token $Token -StateRoot $StateRoot -Targets $Targets
        Remove-Item -LiteralPath $leaseConvention.RecordPath -Force
    }
}

function Get-KbpStateReservationToken($State) {
    if ($null -eq $State -or $null -eq $State.PSObject.Properties['reservationToken']) { return $null }
    $token = [string]$State.reservationToken
    if ([string]::IsNullOrEmpty($token)) { return $null }
    return $token
}

# Whether a run left nothing unresolved of its own in this lab: no run folder
# and no lock of its own, or a transaction Restored, verified and unlocked.
function Test-KbpRunSettled([string]$StateRoot, [string]$RunId) {
    $statePath = Join-Path $StateRoot ('transactions\' + $RunId + '\transaction.json')
    $lockPath = Join-Path $StateRoot 'deployment.lock'
    $ownLock = $false
    if (Test-Path -LiteralPath $lockPath) { try { $ownLock = [string](Read-KbpJson $lockPath).runId -ceq $RunId } catch { $ownLock = $true } }
    if ($ownLock) { return $false }
    if (-not (Test-Path -LiteralPath (Join-Path $StateRoot ('transactions\' + $RunId)))) { return $true }
    if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) { return $false }
    $state = $null
    try { $state = Read-KbpJson $statePath } catch { return $false }
    return [string]$state.status -ceq 'Restored' -and $state.restorationVerified -is [bool] -and $state.restorationVerified
}

# Focused re-review: a harness test path is inside this checkout's
# artifacts\runtime-harness-tests and reached without a junction, link or
# mount point on the way; every other path is treated as the lab's own.
function Test-KbpHarnessTestPath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $root = [IO.Path]::GetFullPath((Join-Path (Get-KbpRepositoryRoot) 'artifacts\runtime-harness-tests')).TrimEnd('\')
    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    if (-not $full.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { return $false }
    $current = $full
    while ($true) {
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { return $false }
        }
        if ($current.Length -le $root.Length) { break }
        $current = Split-Path -Parent $current
    }
    return $true
}

# The lab's fixture lock guards the lab's own runtime state only; a test
# state root has none unless a test names one (re-review, harness).
function Get-KbpDefaultFixtureLockPath([string]$StateRoot) {
    if (-not [string]::IsNullOrWhiteSpace($StateRoot) -and
        [IO.Path]::GetFullPath($StateRoot).TrimEnd('\').Equals($script:KbpRuntimeStateRoot, [StringComparison]::OrdinalIgnoreCase)) {
        return (Join-Path $script:KbpLabRoot 'runtime-fixture-state\fixture.lock')
    }
    return ''
}

# Final review C8 and its re-review: a fixture bootstrap or teardown in
# progress, or one that was interrupted, blocks a runtime entry; the message
# names the lock's run and what ends an interrupted one.
function Assert-KbpFixtureLockAbsent([string]$FixtureLockPath) {
    if ([string]::IsNullOrWhiteSpace($FixtureLockPath) -or -not (Test-Path -LiteralPath $FixtureLockPath)) { return }
    $holder = 'unknown'
    try { $holder = [string](Read-KbpJson $FixtureLockPath).runId } catch { }
    throw ("The fixture lock of run $holder exists: $FixtureLockPath. A fixture bootstrap or teardown is running, " +
        "or one was interrupted: New-KbpAutomationFixture.ps1 -Recover -RunId $holder (with -Family Advanced for " +
        "the advanced fixture) rolls back an interrupted bootstrap; after an interrupted teardown the owner checks " +
        "the save folder before the lock is removed.")
}

# Final review C2: a run whose protected-save comparison is still pending is
# unresolved until Restore-Local.ps1 -RunId finishes it.
function Assert-KbpNoPendingProtectedSaveComparison([string]$StateRoot) {
    foreach ($baselineFile in @(Get-ChildItem -LiteralPath $StateRoot -Filter protected-saves-before.json -File -Recurse -ErrorAction SilentlyContinue)) {
        $baselineRun = Split-Path -Leaf (Split-Path -Parent $baselineFile.FullName)
        try { $baseline = Read-KbpJson $baselineFile.FullName }
        catch {
            throw ("The protected-save baseline of run $baselineRun cannot be read: Restore-Local.ps1 -RunId $baselineRun " +
                "-CloseUnverifiableComparison records it for the owner's review and restores.")
        }
        if (-not [bool]$baseline.compared) {
            throw "The protected-save comparison of run $($baseline.runId) is pending (Restore-Local.ps1 -RunId $($baseline.runId) finishes it once the game has exited)."
        }
    }
}

# Every later run (and fixture change) is refused while a recorded
# protected-save violation has no owner acknowledgement
# (scripts\Confirm-KbpProtectedSaveReview.ps1). Re-review (harness): the
# acknowledgements live in their own folder, and each must name its record's
# run and the exact bytes of the record it acknowledged.
function Assert-KbpNoUnacknowledgedSaveViolation {
    param([string]$StateRoot = $script:KbpRuntimeStateRoot)
    $folder = Join-Path $StateRoot 'protected-save-violations'
    if (-not (Test-Path -LiteralPath $folder -PathType Container)) { return }
    foreach ($record in @(Get-ChildItem -LiteralPath $folder -Filter '*.json' -File | Sort-Object Name)) {
        # Focused re-review: a record or acknowledgement missing a field
        # still blocks, with the record's own name in the message; last
        # review: so does one that cannot be read at all.
        try { $violation = Read-KbpJson $record.FullName }
        catch {
            throw ("The protected-save violation record $($record.Name) cannot be read. No run starts until the " +
                "owner has inspected and resolved it by hand.")
        }
        $recordRun = if ($null -ne $violation.PSObject.Properties['runId']) { [string]$violation.runId } else { '' }
        $blocking = if ($null -ne $violation.PSObject.Properties['blocking']) { @($violation.blocking) -join ', ' } else { 'unknown' }
        $acknowledgement = Join-Path (Join-Path $folder 'acknowledged') ($record.BaseName + '.json')
        $acknowledged = $false
        if ($recordRun -ceq $record.BaseName -and (Test-Path -LiteralPath $acknowledgement -PathType Leaf)) {
            $ack = $null
            try { $ack = Read-KbpJson $acknowledgement } catch { }
            $ackRun = if ($null -ne $ack -and $null -ne $ack.PSObject.Properties['runId']) { [string]$ack.runId } else { '' }
            $ackSha = if ($null -ne $ack -and $null -ne $ack.PSObject.Properties['violationRecordSha256']) {
                [string]$ack.violationRecordSha256 } else { '' }
            $acknowledged = $ackRun -ceq $recordRun -and $ackSha -ceq (Get-KbpSha256 $record.FullName)
        }
        if ($recordRun -cne $record.BaseName) {
            # Targeted review: a record that does not name its own run cannot
            # be acknowledged by the script; the owner resolves it by hand.
            throw ("The protected-save violation record $($record.Name) is malformed (it names run '$recordRun'). " +
                "No run starts until the owner has inspected and resolved it by hand.")
        }
        if (-not $acknowledged) {
            throw ("Run $recordRun changed protected saves ($blocking). " +
                "No run starts until the owner has reviewed it and run scripts\Confirm-KbpProtectedSaveReview.ps1 -RunId $recordRun.")
        }
    }
}

function Assert-KbpNoUnresolvedTransaction([string]$StateRoot, [string]$FixtureLockPath) {
    if (-not $PSBoundParameters.ContainsKey('FixtureLockPath')) {
        $FixtureLockPath = Get-KbpDefaultFixtureLockPath $StateRoot
    }
    $lock = Join-Path $StateRoot 'deployment.lock'
    if (Test-Path -LiteralPath $lock) { throw "Unresolved runtime deployment lock exists: $lock" }
    Assert-KbpFixtureLockAbsent $FixtureLockPath
    Assert-KbpNoPendingProtectedSaveComparison $StateRoot
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
    # A windowed run whose physical-display verification did not complete.
    foreach ($snapshotFile in @(Get-ChildItem -LiteralPath $StateRoot -Filter display-physical.json -File -Recurse -ErrorAction SilentlyContinue)) {
        $snapshotState = Read-KbpPhysicalDisplaySnapshotFile $snapshotFile.FullName
        if (-not [bool]$snapshotState.restored) {
            throw "Unverified physical display snapshot exists: $($snapshotFile.FullName) (Restore-Local.ps1 -RunId $($snapshotState.runId) verifies it without overwriting user settings)."
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
        # The digest admitted by the receipt/validation preflight: the transaction copies the package once into its own
        # folder, refuses a copy with another digest before anything is activated, and extracts only that copy.
        [string]$ExpectedPackageSha256,
        # The bounded retry of the two directory moves (a transient sharing
        # lock on the live Mods folder refused the first move of live run
        # casting-qual-select-20260924-45bff28-01); tests shorten it.
        [ValidateRange(1, 20)][int]$MoveAttempts = 10,
        [ValidateRange(0, 10000)][int]$MoveDelayMilliseconds = 3000,
        # The Kingmaker native-test reservation the calling tool (Deploy-Local
        # or the launcher) already holds for exactly this run; it stays the
        # caller's. Without it the entry acquires its own.
        [string]$ReservationToken
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
    # The Kingmaker native-test reservation comes first (docs/NATIVE-RESERVATION.md):
    # borrowed only for exactly this run, token and state root, or acquired here.
    $leaseTargets = @($StateRoot, $StagingRoot, $BackupRoot, $game)
    if ([string]::IsNullOrEmpty($ReservationToken)) {
        $entryToken = [string](Enter-KbpNativeReservation -RunId $RunId -Purpose 'runtime-transaction' -StateRoot $StateRoot -Targets $leaseTargets).token
        $ownsLease = $true
    } else {
        Assert-KbpNativeReservation -RunId $RunId -Token $ReservationToken -StateRoot $StateRoot -Targets $leaseTargets
        $entryToken = $ReservationToken
        $ownsLease = $false
    }
    # A separate process restoring this run (Restore-Local holds the same
    # per-run mutex) cannot interleave with this entry.
    $runMutex = [Threading.Mutex]::new($false, ('Global\KbpRestore-' + $RunId.ToLowerInvariant()))
    $runMutexHeld = $false
    try {
    try { $runMutexHeld = $runMutex.WaitOne(0) } catch [Threading.AbandonedMutexException] { $runMutexHeld = $true }
    $lockTaken = $false
    try {
    if (-not $runMutexHeld) { throw "Another process is restoring run $RunId." }
    Assert-KbpNoUnresolvedTransaction $StateRoot

    $token = [Guid]::NewGuid().ToString('N')
    $lockPath = Join-Path $StateRoot 'deployment.lock'
    $transactionRoot = Join-Path $StateRoot ('transactions\' + $RunId)
    $stagingRunRoot = Join-Path $StagingRoot $RunId
    $backupRunRoot = Join-Path $BackupRoot $RunId
    foreach ($path in @($transactionRoot, $stagingRunRoot, $backupRunRoot)) {
        if (Test-Path -LiteralPath $path) { throw "Run-owned path already exists: $path" }
    }
    $lockTaken = $true
    New-KbpOwnedLock $lockPath $RunId $token
    $fixtureLock = Get-KbpDefaultFixtureLockPath $StateRoot
    if ($PSBoundParameters.ContainsKey('KnownKingmakerProcessIds')) {
        Confirm-KbpLockedWithoutForeignLease -LockPath $lockPath -RunId $RunId -Token $token `
            -SkipForeignLease:$FixtureMode -KnownProcessIds $KnownKingmakerProcessIds -FixtureLockPath $fixtureLock
    } else {
        Confirm-KbpLockedWithoutForeignLease -LockPath $lockPath -RunId $RunId -Token $token -SkipForeignLease:$FixtureMode `
            -FixtureLockPath $fixtureLock
    }
    }
    catch {
        # Refused before any shared change: an own reservation is released,
        # unless this entry's own lock remains (then it stays for the owner).
        $admissionFailure = $_
        $lockRemains = $false
        if ($lockTaken -and (Test-Path -LiteralPath $lockPath)) {
            try { $lockRemains = [string](Read-KbpJson $lockPath).token -ceq $token } catch { $lockRemains = $true }
        }
        if ($ownsLease -and -not $lockRemains) { Exit-KbpNativeReservation -RunId $RunId -Token $entryToken -StateRoot $StateRoot -Targets $leaseTargets }
        throw $admissionFailure
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
    $ownedPackage = Join-Path $backupRunRoot 'package.zip'
    [IO.File]::Copy($package, $ownedPackage)
    $ownedPackageSha256 = Get-KbpSha256 $ownedPackage
    if ($ExpectedPackageSha256 -and $ownedPackageSha256 -cne $ExpectedPackageSha256) {
        Remove-Item -LiteralPath $ownedPackage -Force
        throw "The package changed after admission (admitted $ExpectedPackageSha256, now $ownedPackageSha256); nothing was staged or activated."
    }
    $state = [ordered]@{
        schemaVersion = 1; runId = $RunId; token = $token; status = 'Preparing'; reservationToken = $entryToken
        createdAtUtc = [DateTime]::UtcNow.ToString('o'); packagePath = $package; ownedPackagePath = $ownedPackage
        packageSha256 = $ownedPackageSha256; gameRoot = $game; modsPath = $mods
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
            # The transaction's own package copy (complete or partial) lives only in this run's backup folder.
            # Each cleanup step is attempted on its own; a failed step names the owned path that still needs cleanup,
            # and the token-checked lock release is attempted regardless.
            $cleanupFailures = @()
            $ownedCopy = Join-Path $backupRunRoot 'package.zip'
            try {
                if ([IO.Path]::GetFullPath($ownedCopy).StartsWith([IO.Path]::GetFullPath($backupRunRoot).TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) -and
                    (Test-Path -LiteralPath $ownedCopy -PathType Leaf)) { Remove-Item -LiteralPath $ownedCopy -Force -ErrorAction Stop }
            }
            catch { $cleanupFailures += "owned package copy $ownedCopy ($($_.Exception.Message))" }
            foreach ($owned in @($backupRunRoot, $transactionRoot)) {
                try {
                    if ((Test-Path -LiteralPath $owned -PathType Container) -and
                        @(Get-ChildItem -LiteralPath $owned -Force).Count -eq 0) {
                        Remove-Item -LiteralPath $owned -Force -ErrorAction Stop
                    }
                }
                catch { $cleanupFailures += "owned folder $owned ($($_.Exception.Message))" }
            }
            $lockFailure = $null
            try { Remove-KbpOwnedLock $lockPath $RunId $token } catch { $lockFailure = $_.Exception.Message }
            # Nothing shared was changed: an own reservation goes last, once nothing of the run remains.
            if ($ownsLease -and $cleanupFailures.Count -eq 0 -and -not $lockFailure) {
                Exit-KbpNativeReservation -RunId $RunId -Token $entryToken -StateRoot $StateRoot -Targets $leaseTargets
            }
            if ($cleanupFailures.Count -gt 0 -or $lockFailure) {
                throw ("Runtime entry failed before its state existed (" + $preStateFailure.Exception.Message + ")" +
                    $(if ($lockFailure) { "; its lock could not be released: $lockFailure" } else { '; its lock was released' }) +
                    $(if ($cleanupFailures.Count -gt 0) { '; still to clean up: ' + ($cleanupFailures -join '; ') } else { '' }))
            }
        }
        throw $preStateFailure
    }
    try {
        $mod = Expand-KbpPackageToStaging -PackagePath $ownedPackage -StagingRunRoot $stagingRunRoot
        $stagedMods = Join-Path $stagingRunRoot 'Mods'
        New-Item -ItemType Directory -Path $stagedMods | Out-Null
        Move-Item -LiteralPath $mod -Destination (Join-Path $stagedMods 'KingmakerBuffPlanner')
        foreach ($expectedMod in $profileMods) {
            if ($expectedMod.directoryName -ceq 'KingmakerBuffPlanner') {
                throw 'Compatibility profile cannot replace the project-owned mod.'
            }
            $sourceMod = Get-KbpCompatibilitySourcePath $expectedMod $mods
            $sourceIdentity = Assert-KbpCompatibilityModIdentity $expectedMod $sourceMod
            # The staged directory takes the profile's directory name, so an
            # external copy may carry its version in its own name (two
            # copies of one mod side by side under the fixture root).
            $stagedMod = Join-Path $stagedMods ([string]$expectedMod.directoryName)
            if (Test-Path -LiteralPath $stagedMod) {
                throw "Compatibility profile stages one directory twice: $($expectedMod.directoryName)"
            }
            Copy-Item -LiteralPath $sourceMod -Destination $stagedMod -Recurse
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
        $leaseCheck = { Assert-KbpNativeReservation -RunId $RunId -Token $entryToken -StateRoot $StateRoot -Targets $leaseTargets }
        if ($originalExisted) {
            $originalMoveAttempts = Move-KbpDirectoryWithRetry -Source $mods -Destination $originalBackup `
                -Attempts $MoveAttempts -DelayMilliseconds $MoveDelayMilliseconds -BeforeAttempt $leaseCheck
        }
        $state.status = 'OriginalMoved'
        $state.originalMoveAttempts = [int]$originalMoveAttempts
        Write-KbpJsonAtomic $statePath $state
        [void](Move-KbpDirectoryWithRetry -Source $stagedMods -Destination $mods `
            -Attempts $MoveAttempts -DelayMilliseconds $MoveDelayMilliseconds -BeforeAttempt $leaseCheck)
        $state.status = 'Active'
        $state.activatedAtUtc = [DateTime]::UtcNow.ToString('o')
        Write-KbpJsonAtomic $statePath $state
        return $statePath
    }
    catch {
        $entryFailure = $_
        try {
            # Final review C8: the process ids are passed on only when the
            # caller gave them; passing an unbound (null) list would switch
            # off the live running-game check of the restoration.
            $restoreArguments = @{ RunId = $RunId; StateRoot = $StateRoot; FixtureMode = $FixtureMode; BorrowedToken = $entryToken }
            if ($PSBoundParameters.ContainsKey('KnownKingmakerProcessIds')) {
                $restoreArguments.KnownKingmakerProcessIds = $KnownKingmakerProcessIds
            }
            Restore-KbpRuntimeTransaction @restoreArguments | Out-Null
        }
        catch {
            throw "Runtime entry failed and restoration also failed. Entry: $($entryFailure.Exception.Message) Restore: $($_.Exception.Message)"
        }
        # The rollback verified the run's restoration: an own reservation goes last.
        if ($ownsLease -and (Test-KbpRunSettled $StateRoot $RunId)) {
            Exit-KbpNativeReservation -RunId $RunId -Token $entryToken -StateRoot $StateRoot -Targets $leaseTargets
        }
        throw $entryFailure
    }
    }
    finally {
        if ($runMutexHeld) { $runMutex.ReleaseMutex() }
        $runMutex.Dispose()
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
        [int]$DelayMilliseconds = 3000,
        # Run before every attempt (the reservation's ownership check); a
        # failure stops the move before it happens.
        [scriptblock]$BeforeAttempt)
    for ($attempt = 1; ; $attempt++) {
        if ($null -ne $BeforeAttempt) { & $BeforeAttempt }
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
        [int[]]$KnownKingmakerProcessIds,
        # The reservation the calling tool holds for exactly this run (its
        # token must be the journaled one); the caller releases it.
        [string]$BorrowedToken
    )
    if ($PSBoundParameters.ContainsKey('KnownKingmakerProcessIds')) {
        Assert-KbpNotRunning -KnownProcessIds $KnownKingmakerProcessIds
    } else { Assert-KbpNotRunning }
    $statePath = Join-Path $StateRoot ('transactions\' + $RunId + '\transaction.json')
    $state = Read-KbpJson $statePath
    if ($state.schemaVersion -ne 1 -or $state.runId -cne $RunId) { throw 'Runtime transaction state identity is invalid.' }
    if ($null -ne $state.PSObject.Properties['modsRecovery']) {
        throw "Display recovery for $RunId must be finalized through Restore-Local.ps1."
    }
    if ($state.status -ceq 'Restored') { return $state }
    foreach ($field in @('status', 'token', 'lockPath', 'modsPath', 'originalExisted', 'originalBackup', 'stagedQuarantine', 'stagingRunRoot')) {
        if ($null -eq $state.PSObject.Properties[$field]) { throw "Runtime transaction state identity is invalid (no $field)." }
    }
    Assert-KbpOwnedLock $state.lockPath $RunId $state.token
    # The Kingmaker native-test reservation: the caller's for exactly this run,
    # the journaled one (a standalone restoration continues only the
    # transaction its record names), or, for a transaction recorded before this
    # lab took part, a free one taken now and journaled. Anything else refuses.
    $leaseTargets = @($StateRoot, [string]$state.modsPath, [string]$state.originalBackup, [string]$state.stagedQuarantine,
        [string]$state.stagingRunRoot, [string]$state.lockPath) | Where-Object { -not [string]::IsNullOrEmpty($_) }
    $journaledToken = Get-KbpStateReservationToken $state
    $releaseLease = $false
    if (-not [string]::IsNullOrEmpty($BorrowedToken)) {
        if ($journaledToken -cne $BorrowedToken) { throw "Run $RunId's journal does not name the caller's reservation; nothing was restored." }
        $restoreToken = $BorrowedToken
    } elseif ($null -ne $journaledToken) {
        $restoreToken = $journaledToken
        $releaseLease = $true
    } else {
        $restoreToken = [string](Enter-KbpNativeReservation -RunId $RunId -Purpose 'runtime-restore' -StateRoot $StateRoot -Targets $leaseTargets).token
        $state | Add-Member -NotePropertyName reservationToken -NotePropertyValue $restoreToken -Force
        Write-KbpJsonAtomic $statePath $state
        $releaseLease = $true
    }
    $leaseCheck = { Assert-KbpNativeReservation -RunId $RunId -Token $restoreToken -StateRoot $StateRoot -Targets $leaseTargets }
    & $leaseCheck
    # Serialized with the owner's other lab (review C3): this run's Mods
    # folder is never moved while that lab's runtime lease is held.
    if (-not $FixtureMode) { Wait-KbpNoForeignRuntimeLease }
    $mods = [string]$state.modsPath
    $preActivationNoOp = $false
    # The original tree must match the recorded manifest at its current
    # location before any move that would install it.
    $assertBackup = {
        if (-not (Test-Path -LiteralPath $state.originalBackup -PathType Container)) { throw 'Original Mods backup is missing.' }
        if (-not (Test-KbpManifestEqual @($state.originalManifest) @(Get-KbpDirectoryManifest $state.originalBackup))) {
            throw 'Original Mods backup does not match the recorded original manifest; nothing was moved.'
        }
    }
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
                if ($state.originalExisted) { & $assertBackup }
                $currentStaged = @(Get-KbpDirectoryManifest $mods)
                $state.stagedMutationObserved = -not (Test-KbpManifestEqual @($state.stagedManifest) $currentStaged)
                if ($state.stagedMutationObserved) { $state.observedStagedManifest = $currentStaged }
                [void](Move-KbpDirectoryWithRetry -Source $mods -Destination $state.stagedQuarantine -BeforeAttempt $leaseCheck)
            }
        }

        if ($state.originalExisted) {
            if (-not $preActivationNoOp) {
                & $assertBackup
                if (Test-Path -LiteralPath $mods) { throw 'Mods destination is occupied during restore.' }
                [void](Move-KbpDirectoryWithRetry -Source $state.originalBackup -Destination $mods -BeforeAttempt $leaseCheck)
                $restoredManifest = @(Get-KbpDirectoryManifest $mods)
                if (-not (Test-KbpManifestEqual @($state.originalManifest) $restoredManifest)) {
                    throw 'Restored Mods manifest/hash mismatch.'
                }
                Assert-KbpExternallyStagedLiveRestored -State $state -ModsPath $mods
            }
        }
        elseif (Test-Path -LiteralPath $mods) { throw 'Mods must remain absent because it was absent before entry.' }

        & $leaseCheck
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
        & $leaseCheck
        $state.status = 'Restored'
        $state.restorationVerified = $true
        $state.restoredAtUtc = [DateTime]::UtcNow.ToString('o')
        Write-KbpJsonAtomic $statePath $state
        Remove-KbpOwnedLock $state.lockPath $RunId $state.token
        # Released last, after the verified restoration and its lock; a failed
        # restoration (below) keeps it for the owner.
        if ($releaseLease) { Exit-KbpNativeReservation -RunId $RunId -Token $restoreToken -StateRoot $StateRoot -Targets $leaseTargets }
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
