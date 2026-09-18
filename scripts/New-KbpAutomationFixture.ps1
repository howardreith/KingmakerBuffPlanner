[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId = 'bootstrap-automation-fixture',
    # -SaveRoot/-ArchiveRoot/-StateRoot/-ProcessName exist for the source-test
    # harness and for narrowly controlled process boundaries; defaults are the
    # exact production locations and the exact production process name.
    [string]$SaveRoot = (Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games'),
    [string]$ArchiveRoot,
    [string]$StateRoot,
    [string]$ProcessName = 'Kingmaker',
    [switch]$Teardown,
    [string]$ManifestPath,
    [switch]$Recover,
    # Test-only failure injection. Never pass in production.
    [ValidateSet('SeedValidated', 'Staged', 'Archived', 'ManifestWritten', 'Published')]
    [string]$FailAfterStage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Guarded bootstrap for the disposable automation fixture pair.
#
# Safety contract:
#   - Real running-game discovery every time (Assert-KbpNotRunning without a
#     supplied id list), re-checked immediately before publishing any save.
#     -ProcessName exists only so tests can exercise THIS script against a
#     fake process name; production always uses the default.
#   - One writer through the shared owned lock; interrupted transactions are
#     detected and only recovered through -Recover using the ownership record.
#   - Everything is staged and verified outside SaveRoot first; publication
#     happens only after the archive and manifest exist. Failures roll back
#     exactly the owned published/staged paths and never touch anything else.
#   - Teardown deletes only the exact paths recorded in the bootstrap
#     manifest (baseline additionally hash-verified; the working fixture is
#     legitimately mutable so it is verified by exact path + header name).
#     Unknown or matching-but-unrecorded files are never deleted.
#
# Seed contract (the only accepted input): exactly one
# Manual_<n>_KBP_AUTOMATION_SEED.zks whose header.json Name is exactly
# KBP_AUTOMATION_SEED — a deliberately disposable campaign saved by the
# human through the ordinary in-game dialog.

$repo = Get-KbpRepositoryRoot
$lab = Split-Path -Parent (Split-Path -Parent $repo)
if ([string]::IsNullOrWhiteSpace($ArchiveRoot)) {
    $ArchiveRoot = Join-Path $lab ("runtime-backups\automation-fixture\" + $RunId)
}
if ([string]::IsNullOrWhiteSpace($StateRoot)) {
    $StateRoot = Join-Path $lab 'runtime-state\automation-fixture'
}
$lockPath = Join-Path $StateRoot 'fixture.lock'
$runRoot = Join-Path $StateRoot $RunId
$stagingRoot = Join-Path $runRoot 'staging'
$transactionPath = Join-Path $runRoot 'transaction.json'

function Read-KbpSaveHeader {
    param([string]$Path)
    $archive = $null; $reader = $null
    try {
        $archive = [IO.Compression.ZipFile]::OpenRead($Path)
        $entry = $archive.GetEntry('header.json')
        if ($null -eq $entry) { throw "Save has no header.json: $(Split-Path -Leaf $Path)" }
        $reader = [IO.StreamReader]::new($entry.Open())
        return ($reader.ReadToEnd() | ConvertFrom-Json)
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $archive) { $archive.Dispose() }
    }
}

function Copy-KbpSaveWithHeaderName {
    param([string]$SourcePath, [string]$DestinationPath, [string]$NewName)
    if (Test-Path -LiteralPath $DestinationPath) {
        throw "Refusing to overwrite an existing artifact: $DestinationPath"
    }
    # The temporary name is unique per call; unknown pre-existing files are
    # never deleted, and the staged file is created with CreateNew semantics.
    $temporary = $DestinationPath + '.' + [Guid]::NewGuid().ToString('N') + '.partial'
    $source = $null; $target = $null; $reader = $null; $writer = $null
    try {
        $source = [IO.Compression.ZipFile]::OpenRead($SourcePath)
        $target = [IO.Compression.ZipFile]::Open($temporary,
            [IO.Compression.ZipArchiveMode]::Create, [Text.Encoding]::UTF8)
        foreach ($entry in $source.Entries) {
            $newEntry = $target.CreateEntry($entry.FullName,
                [IO.Compression.CompressionLevel]::Optimal)
            if ($entry.FullName -ceq 'header.json') {
                $reader = [IO.StreamReader]::new($entry.Open())
                $header = $reader.ReadToEnd()
                $reader.Dispose(); $reader = $null
                $pattern = '"Name"\s*:\s*"KBP_AUTOMATION_SEED"'
                if ([regex]::Matches($header, $pattern).Count -ne 1) {
                    throw 'Seed header.json does not contain exactly one Name field to rewrite.'
                }
                $header = [regex]::Replace($header, $pattern, ('"Name":"' + $NewName + '"'))
                $writer = [IO.StreamWriter]::new($newEntry.Open(), [Text.Encoding]::UTF8)
                $writer.Write($header)
                $writer.Dispose(); $writer = $null
            }
            else {
                $inStream = $entry.Open()
                $outStream = $newEntry.Open()
                try { $inStream.CopyTo($outStream) }
                finally { $inStream.Dispose(); $outStream.Dispose() }
            }
        }
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $writer) { $writer.Dispose() }
        if ($null -ne $source) { $source.Dispose() }
        if ($null -ne $target) { $target.Dispose() }
        if (Test-Path -LiteralPath $temporary) {
            try { [IO.File]::Move($temporary, $DestinationPath) }
            catch { Remove-Item -LiteralPath $temporary -Force; throw }
        }
    }
}

function Get-KbpProtectedInventory {
    param([string]$Root, [string[]]$ExcludeNames)
    @(Get-ChildItem -LiteralPath $Root -Filter '*.zks' -File |
        Where-Object { $ExcludeNames -notcontains $_.Name } |
        Sort-Object Name | ForEach-Object { "$($_.Name):$(Get-KbpSha256 $_.FullName)" })
}

# Real running-game discovery. Never supply KnownProcessIds here: supplying
# it (even $null) would replace live discovery with an empty list.
function Assert-KbpFixtureGameClosed {
    Assert-KbpNotRunning
    if ($ProcessName -cne 'Kingmaker') {
        # Test-only boundary: the seam must still observe a real process of
        # the injected name through the same code path.
        $ids = @(Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | ForEach-Object Id)
        if (@($ids).Count -ne 0) {
            throw "$ProcessName is running (PID(s): $($ids -join ', ')); fixture bootstrap refused."
        }
    }
}

function Get-KbpTransaction {
    if (-not (Test-Path -LiteralPath $transactionPath)) { return $null }
    return (Read-KbpJson $transactionPath)
}

function Invoke-KbpOwnedRollback {
    param($Transaction)
    foreach ($owned in @($Transaction.publishedPaths)) {
        if ($owned -and (Test-Path -LiteralPath $owned -PathType Leaf)) {
            Remove-Item -LiteralPath $owned -Force
        }
    }
    if ($Transaction.stagingRoot -and (Test-Path -LiteralPath $Transaction.stagingRoot -PathType Container)) {
        Remove-Item -LiteralPath $Transaction.stagingRoot -Force -Recurse
    }
    # Live transactions are ordered dictionaries; recovery records read
    # back from JSON are PSCustomObjects. Support both shapes.
    if ($Transaction -is [System.Collections.IDictionary]) {
        $Transaction['status'] = 'RolledBack'
        $Transaction['rolledBackAtUtc'] = [DateTime]::UtcNow.ToString('o')
    }
    else {
        $Transaction | Add-Member -NotePropertyName status -NotePropertyValue 'RolledBack' -Force
        $Transaction | Add-Member -NotePropertyName rolledBackAtUtc `
            -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force
    }
    Write-KbpJsonAtomic $transactionPath $Transaction
}

# --- Recover mode: roll back a specific interrupted run only. ---
if ($Recover) {
    if (-not (Test-Path -LiteralPath $lockPath)) {
        throw "No fixture lock exists; nothing to recover at $StateRoot."
    }
    $existing = Get-KbpTransaction
    if ($null -eq $existing) { throw "No transaction record found for run $RunId." }
    if ($existing.runId -cne $RunId) { throw "Transaction belongs to $($existing.runId), not $RunId." }
    $lock = Read-KbpJson $lockPath
    if ($existing.status -ceq 'Completed') {
        throw "Run $RunId already completed; use -Teardown instead."
    }
    Invoke-KbpOwnedRollback $existing
    Remove-KbpOwnedLock $lockPath $lock.runId $lock.token
    Write-Host "Recovery PASS: rolled back owned partial artifacts of $RunId; lock released."
    return
}

if ($Teardown) {
    if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
        $candidates = @(Get-ChildItem -LiteralPath $ArchiveRoot -Filter 'fixture-manifest.json' -File -Recurse -ErrorAction SilentlyContinue)
        if ($candidates.Count -ne 1) {
            throw "Expected exactly one fixture manifest under $ArchiveRoot; found $($candidates.Count). Pass -ManifestPath explicitly."
        }
        $ManifestPath = $candidates[0].FullName
    }
    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        throw "Fixture manifest is missing: $ManifestPath"
    }
    New-Item -ItemType Directory -Path $StateRoot -Force | Out-Null
    New-KbpOwnedLock $lockPath $RunId ([Guid]::NewGuid().ToString('N'))
    $lock = Read-KbpJson $lockPath
    try {
        Assert-KbpFixtureGameClosed
        $manifest = Read-KbpJson $ManifestPath
        if ($manifest.schemaVersion -ne 1 -or
            -not $manifest.baseline.fileName -or -not $manifest.baseline.sha256 -or
            -not $manifest.working.fileName) {
            throw 'Fixture manifest is not valid; refusing teardown.'
        }
        $baselinePath = Join-Path $SaveRoot $manifest.baseline.fileName
        $workingPath = Join-Path $SaveRoot $manifest.working.fileName
        foreach ($fixture in @(
                @{ Path = $baselinePath; Name = 'KBP_AUTOMATION_BASELINE'; Hash = [string]$manifest.baseline.sha256 },
                @{ Path = $workingPath; Name = 'KBP_AUTOMATION_WORKING'; Hash = $null })) {
            if (-not (Test-Path -LiteralPath $fixture.Path -PathType Leaf)) {
                throw "Owned fixture file is absent: $($fixture.Path)"
            }
            $header = Read-KbpSaveHeader -Path $fixture.Path
            if ([string]$header.Name -cne $fixture.Name) {
                throw "Owned fixture header mismatch at $($fixture.Path): $($header.Name)"
            }
            # The baseline is sealed and must still hash exactly; the working
            # fixture is legitimately mutable, so only its identity is proven.
            if ($null -ne $fixture.Hash -and $fixture.Hash -cne '') {
                $actual = Get-KbpSha256 $fixture.Path
                if ($actual -cne $fixture.Hash) {
                    throw "Sealed baseline hash drifted: $($fixture.Path)"
                }
            }
        }
        $protectedBefore = Get-KbpProtectedInventory -Root $SaveRoot `
            -ExcludeNames @($manifest.baseline.fileName, $manifest.working.fileName)
        if (-not $PSCmdlet.ShouldProcess("$baselinePath; $workingPath",
            'Remove the manifest-owned disposable fixture pair')) {
            Write-Host 'Fixture teardown WhatIf PASS; no save was removed.'
            return
        }
        Remove-Item -LiteralPath $baselinePath -Force
        Remove-Item -LiteralPath $workingPath -Force
        $protectedAfter = Get-KbpProtectedInventory -Root $SaveRoot `
            -ExcludeNames @($manifest.baseline.fileName, $manifest.working.fileName)
        if (($protectedBefore -join "`n") -cne ($protectedAfter -join "`n")) {
            throw 'Teardown touched a protected save; investigate immediately.'
        }
        Write-Host "Fixture teardown PASS: removed exactly the manifest-owned pair; $($protectedBefore.Count) other save(s) byte-identical."
    }
    finally {
        if (Test-Path -LiteralPath $lockPath) {
            Remove-KbpOwnedLock $lockPath $lock.runId $lock.token
        }
    }
    return
}

# --- Bootstrap ---
if (Test-Path -LiteralPath $lockPath) {
    throw "A fixture lock exists: $lockPath. Another bootstrap/teardown is active, or an interrupted run awaits -Recover."
}
$interruptedTransactions = @()
if (Test-Path -LiteralPath $StateRoot -PathType Container) {
    $interruptedTransactions = @(Get-ChildItem -LiteralPath $StateRoot -Filter 'transaction.json' -File -Recurse -ErrorAction SilentlyContinue)
}
foreach ($interrupted in $interruptedTransactions) {
    $record = Read-KbpJson $interrupted.FullName
    # Completed and RolledBack runs are settled; anything else may still own
    # published artifacts and must be recovered explicitly.
    if ($record.status -cne 'Completed' -and $record.status -cne 'RolledBack') {
        throw "Unresolved fixture transaction '$($record.runId)' status=$($record.status). Recover it with -Recover -RunId '$($record.runId)' first."
    }
}
if (Test-Path -LiteralPath $runRoot) {
    $prior = Get-KbpTransaction
    if ($null -eq $prior -or $prior.status -cne 'RolledBack') {
        throw "Run paths already exist: $runRoot (choose a new RunId or recover first)."
    }
    # A fully rolled-back prior attempt owns nothing; its record is replaced.
    Remove-Item -LiteralPath $runRoot -Recurse -Force
}
if (-not (Test-Path -LiteralPath $SaveRoot -PathType Container)) {
    throw "The exact Kingmaker save root is unavailable: $SaveRoot"
}
Assert-KbpFixtureGameClosed

$automationFiles = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*KBP_AUTOMATION*' -File |
    Where-Object { $_.Name -notmatch '^Manual_[0-9]+_KBP_AUTOMATION_SEED\.zks$' })
if ($automationFiles.Count -ne 0) {
    throw "Existing KBP_AUTOMATION artifact(s) present: $(($automationFiles | ForEach-Object Name) -join ', '). Tear down explicitly first."
}
$seedFiles = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*_KBP_AUTOMATION_SEED.zks' -File)
if ($seedFiles.Count -ne 1) {
    throw "Expected exactly one KBP_AUTOMATION_SEED save; found $($seedFiles.Count). Create one disposable campaign save named exactly KBP_AUTOMATION_SEED, then rerun."
}
$seed = $seedFiles[0]
if ($seed.Name -notmatch '^Manual_[0-9]+_KBP_AUTOMATION_SEED\.zks$') {
    throw "Seed file name is not the exact in-game shape: $($seed.Name)"
}
$seedHeader = Read-KbpSaveHeader -Path $seed.FullName
if ([string]$seedHeader.Name -cne 'KBP_AUTOMATION_SEED') {
    throw "Seed header Name is not exactly KBP_AUTOMATION_SEED: $($seedHeader.Name)"
}

$allSaves = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*.zks' -File)
$maxIndex = 0
foreach ($file in $allSaves) {
    if ($file.Name -match '^Manual_([0-9]+)_') {
        $index = [int]$Matches[1]
        if ($index -gt $maxIndex) { $maxIndex = $index }
    }
}
$baselineName = 'Manual_' + ($maxIndex + 1) + '_KBP_AUTOMATION_BASELINE.zks'
$workingName = 'Manual_' + ($maxIndex + 2) + '_KBP_AUTOMATION_WORKING.zks'
$baselinePath = Join-Path $SaveRoot $baselineName
$workingPath = Join-Path $SaveRoot $workingName
if ((Test-Path -LiteralPath $baselinePath) -or (Test-Path -LiteralPath $workingPath)) {
    throw 'A computed fixture destination already exists; refusing.'
}
$protectedBefore = @{}
foreach ($file in $allSaves) { $protectedBefore[$file.Name] = Get-KbpSha256 $file.FullName }

if (-not $PSCmdlet.ShouldProcess($SaveRoot,
    "create the disposable fixture pair $baselineName and $workingName from the seed")) {
    Write-Host "Fixture bootstrap WhatIf PASS: would seal $baselineName and $workingName from $($seed.Name)."
    return
}

$token = [Guid]::NewGuid().ToString('N')
New-Item -ItemType Directory -Path $StateRoot -Force | Out-Null
New-KbpOwnedLock $lockPath $RunId $token
$transaction = [ordered]@{
    schemaVersion = 1; runId = $RunId; token = $token
    status = 'Staging'; createdAtUtc = [DateTime]::UtcNow.ToString('o')
    saveRoot = $SaveRoot; seedPath = $seed.FullName
    baselineName = $baselineName; workingName = $workingName
    publishedPaths = @(); stagingRoot = $stagingRoot; archiveRoot = $ArchiveRoot
}
try {
    New-Item -ItemType Directory -Path $stagingRoot | Out-Null
    New-Item -ItemType Directory -Path $ArchiveRoot | Out-Null
    Write-KbpJsonAtomic $transactionPath $transaction

    $stagedBaseline = Join-Path $stagingRoot $baselineName
    $stagedWorking = Join-Path $stagingRoot $workingName
    $seedHash = Get-KbpSha256 $seed.FullName
    Copy-KbpSaveWithHeaderName -SourcePath $seed.FullName -DestinationPath $stagedBaseline -NewName 'KBP_AUTOMATION_BASELINE'
    Copy-KbpSaveWithHeaderName -SourcePath $seed.FullName -DestinationPath $stagedWorking -NewName 'KBP_AUTOMATION_WORKING'
    foreach ($staged in @(
            @{ Path = $stagedBaseline; Expected = 'KBP_AUTOMATION_BASELINE' },
            @{ Path = $stagedWorking; Expected = 'KBP_AUTOMATION_WORKING' })) {
        $header = Read-KbpSaveHeader -Path $staged.Path
        if ([string]$header.Name -cne $staged.Expected -or
            [string]$header.GameName -cne [string]$seedHeader.GameName -or
            [string]$header.GameId -cne [string]$seedHeader.GameId) {
            throw "Staged fixture failed descriptor verification: $(Split-Path -Leaf $staged.Path)"
        }
    }
    $transaction.status = 'Staged'
    Write-KbpJsonAtomic $transactionPath $transaction
    if ($FailAfterStage -ceq 'Staged') { throw "Injected failure after stage Staged." }

    $seedBackup = Join-Path $ArchiveRoot $seed.Name
    $baselineBackup = Join-Path $ArchiveRoot $baselineName
    if ((Test-Path -LiteralPath $seedBackup) -or (Test-Path -LiteralPath $baselineBackup)) {
        throw 'Archive destination already exists; refusing to overwrite preserved provenance.'
    }
    Copy-Item -LiteralPath $seed.FullName -Destination $seedBackup
    Copy-Item -LiteralPath $stagedBaseline -Destination $baselineBackup
    $transaction.status = 'Archived'
    Write-KbpJsonAtomic $transactionPath $transaction
    if ($FailAfterStage -ceq 'Archived') { throw "Injected failure after stage Archived." }

    $manifest = [ordered]@{
        schemaVersion = 1
        runId = $RunId
        createdAtUtc = [DateTime]::UtcNow.ToString('o')
        seed = @{ fileName = $seed.Name; sha256 = $seedHash }
        baseline = @{ fileName = $baselineName; sha256 = (Get-KbpSha256 $stagedBaseline) }
        working = @{ fileName = $workingName; sha256 = (Get-KbpSha256 $stagedWorking) }
        seedBackupSha256 = (Get-KbpSha256 $seedBackup)
        baselineBackupSha256 = (Get-KbpSha256 $baselineBackup)
        gameName = [string]$seedHeader.GameName
        gameId = [string]$seedHeader.GameId
    }
    $manifestPath = Join-Path $ArchiveRoot 'fixture-manifest.json'
    if (Test-Path -LiteralPath $manifestPath) {
        throw 'A fixture manifest already exists for this archive; refusing to overwrite.'
    }
    Write-KbpJsonAtomic $manifestPath $manifest
    $transaction.status = 'ManifestWritten'
    $transaction.manifestPath = $manifestPath
    Write-KbpJsonAtomic $transactionPath $transaction
    if ($FailAfterStage -ceq 'ManifestWritten') { throw "Injected failure after stage ManifestWritten." }

    # Real running-game recheck immediately before touching SaveRoot.
    Assert-KbpFixtureGameClosed
    [IO.File]::Move($stagedBaseline, $baselinePath)
    $transaction.publishedPaths = @($baselinePath)
    Write-KbpJsonAtomic $transactionPath $transaction
    [IO.File]::Move($stagedWorking, $workingPath)
    $transaction.publishedPaths = @($baselinePath, $workingPath)
    $transaction.status = 'Published'
    Write-KbpJsonAtomic $transactionPath $transaction
    if ($FailAfterStage -ceq 'Published') { throw "Injected failure after stage Published." }

    foreach ($file in $allSaves) {
        if ((Get-KbpSha256 $file.FullName) -cne $protectedBefore[$file.Name]) {
            throw "A protected save changed during bootstrap: $($file.Name)"
        }
    }
    if ((Test-Path -LiteralPath $stagedBaseline) -or (Test-Path -LiteralPath $stagedWorking)) {
        throw 'Staging residue after publication; rolling back.'
    }
    $transaction.status = 'Completed'
    $transaction.completedAtUtc = [DateTime]::UtcNow.ToString('o')
    Write-KbpJsonAtomic $transactionPath $transaction
}
catch {
    Invoke-KbpOwnedRollback $transaction
    throw
}
finally {
    # The lock is always released exactly once, success or failure.
    if (Test-Path -LiteralPath $lockPath) {
        $lockRecord = Read-KbpJson $lockPath
        Remove-KbpOwnedLock $lockPath $lockRecord.runId $lockRecord.token
    }
}

Write-Host "Fixture bootstrap PASS:"
Write-Host "  baseline $baselineName sha256=$($manifest.baseline.sha256) (sealed; offline copy archived)"
Write-Host "  working  $workingName sha256=$($manifest.working.sha256) (only mutable fixture)"
Write-Host "  seed     $($seed.Name) untouched; $($allSaves.Count) pre-existing save(s) byte-identical"
Write-Host "  archive  $ArchiveRoot"
