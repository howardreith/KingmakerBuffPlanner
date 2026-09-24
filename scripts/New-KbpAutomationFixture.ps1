[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    # Fixture family. 'Automation' is the approved WORKING/BASELINE pair from
    # KBP_AUTOMATION_SEED. 'Advanced' is the separately approved advanced-
    # party copy from an owner-saved KBP_ADVANCED_SEED (see
    # docs/ADVANCED-SAVE-COPY-INSPECTION-REQUEST.md); it shares every
    # safeguard and never touches the automation pair or ordinary saves.
    [ValidateSet('Automation', 'Advanced')][string]$Family = 'Automation',
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
    [ValidateSet('', 'SeedValidated', 'Staged', 'Archived', 'ManifestWritten', 'Published')]
    [string]$FailAfterStage = '',
    # Test-only: interrupt immediately after a final publication move.
    [ValidateSet('', 'baseline', 'working')]
    [string]$FailAfterMove = '',
    # Test-only: interrupt immediately after the write-ahead journal entry
    # but BEFORE the corresponding move.
    [ValidateSet('', 'baseline', 'working')]
    [string]$FailAfterJournal = '',
    # Test-only: make the rollback itself fail (foreign content in staging).
    [switch]$FailRollback
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

$fixturePrefix = if ($Family -ceq 'Advanced') { 'KBP_ADVANCED' } else { 'KBP_AUTOMATION' }
$seedLabel = $fixturePrefix + '_SEED'
$baselineLabel = $fixturePrefix + '_BASELINE'
$workingLabel = $fixturePrefix + '_WORKING'
if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = if ($Family -ceq 'Advanced') { 'bootstrap-advanced-fixture' } else { 'bootstrap-automation-fixture' }
}
$repo = Get-KbpRepositoryRoot
$lab = Split-Path -Parent (Split-Path -Parent $repo)
if ([string]::IsNullOrWhiteSpace($ArchiveRoot)) {
    $ArchiveRoot = Join-Path $lab ("runtime-backups\" + $(if ($Family -ceq 'Advanced') {
        'advanced-fixture' } else { 'automation-fixture' }) + "\" + $RunId)
}
if ([string]::IsNullOrWhiteSpace($StateRoot)) {
    # Fixture lifecycle state lives in its OWN root, distinct from the
    # deployment transaction state root the runtime harness guards: a
    # completed fixture transaction must never appear in (or block) a
    # deployment scan, and a deployment lock must never be mistaken for
    # fixture activity.
    $StateRoot = Join-Path $lab 'runtime-fixture-state'
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
                $pattern = '"Name"\s*:\s*"' + $seedLabel + '"'
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

# Every public mode mutates only under this single gate. WhatIf performs
# read-only validation and returns before any direct I/O — locks,
# transaction JSON, staging directories, archives, and recovery records
# included, because those helpers use direct .NET I/O that WhatIf
# preferences never suppress.
function Assert-KbpFixtureDeploymentIdle {
    $lab2 = Split-Path -Parent (Split-Path -Parent $repo)
    $deploymentLock = Join-Path $lab2 'runtime-state\deployment.lock'
    if (Test-Path -LiteralPath $deploymentLock -PathType Leaf) {
        throw "A runtime deployment transaction is active: $deploymentLock. Coordinate before fixture mutation."
    }
}

# Journal-first publication: the destination enters publishedPaths and is
# persisted before the file moves, closing the move/journal interruption
# window. The post-move existence check turns a silent partial into a
# recoverable failure.
function Publish-KbpStagedFixture {
    param([string]$StagedPath, [string]$DestinationPath)
    $list = @($transaction.publishedPaths) + @($DestinationPath)
    $transaction.publishedPaths = $list
    Write-KbpJsonAtomic $transactionPath $transaction
    # Test-only: interrupt AFTER the journal write but BEFORE the move —
    # the journaled destination does not exist yet, and recovery must
    # reconcile that as an unperformed operation.
    if ($script:failAfterJournal -ceq 'baseline' -and
        $DestinationPath -cmatch $baselineLabel) {
        throw "Injected failure after journaling baseline."
    }
    if ($script:failAfterJournal -ceq 'working' -and
        $DestinationPath -cmatch $workingLabel) {
        throw "Injected failure after journaling working."
    }
    [IO.File]::Move($StagedPath, $DestinationPath)
    if (-not (Test-Path -LiteralPath $DestinationPath -PathType Leaf)) {
        throw "Publication verification failed: $DestinationPath"
    }
}

# Final review C8: the other lab's lease is checked with the deployment lock,
# at the start and again right before the save folder is touched. A test
# save root is not the game's and is not shared with that lab.
function Assert-KbpFixtureForeignLeaseIdle {
    $production = Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games'
    if ([IO.Path]::GetFullPath($SaveRoot).TrimEnd('\') -ieq [IO.Path]::GetFullPath($production).TrimEnd('\')) {
        Assert-KbpNoForeignRuntimeLease
    }
}

function Assert-KbpFixturePreconditions {
    Assert-KbpFixtureGameClosed
    Assert-KbpFixtureDeploymentIdle
    Assert-KbpFixtureForeignLeaseIdle
    Assert-KbpFixtureSavesSettled
}

# Re-review (harness): the game's save folder is not changed while a runtime
# run's protected-save comparison is pending or an unexpected save change
# waits for the owner's review.
function Assert-KbpFixtureSavesSettled {
    $production = Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games'
    if ([IO.Path]::GetFullPath($SaveRoot).TrimEnd('\') -ieq [IO.Path]::GetFullPath($production).TrimEnd('\')) {
        Assert-KbpNoPendingProtectedSaveComparison $script:KbpRuntimeStateRoot
        Assert-KbpNoUnacknowledgedSaveViolation -StateRoot $script:KbpRuntimeStateRoot
    }
}

# Containment + identity validation for any path about to be removed by
# rollback or recovery. Records are JSON on disk; nothing in them is
# trusted until the path is proven to be our owned artifact.
function Assert-KbpOwnedArtifactPath {
    param([string]$Path, [string]$Role, [string]$GameId, [switch]$AllowAbsent)
    if ([string]::IsNullOrWhiteSpace($Path)) { throw 'Owned artifact path is empty.' }
    $fullSave = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $fullState = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    $full = [IO.Path]::GetFullPath($Path)
    $inSave = $full.StartsWith($fullSave + '\', [StringComparison]::OrdinalIgnoreCase)
    $inState = $full.StartsWith($fullState + '\', [StringComparison]::OrdinalIgnoreCase)
    if (-not $inSave -and -not $inState) {
        throw "Recorded artifact escapes the owned roots: $Path"
    }
    if ($inSave) {
        if ($full -cnotmatch ('\\Manual_[0-9]+_' + $fixturePrefix + '_' + $Role + '\.zks$')) {
            throw "Recorded artifact does not match its role filename contract: $Path"
        }
        if ($Path -cmatch '(\.\.[\\/]|[\\/]\.\.)') {
            throw "Recorded artifact path is escaped: $Path"
        }
        if ($AllowAbsent -and -not (Test-Path -LiteralPath $full -PathType Leaf)) {
            # Journaled-but-never-created destination: the write-ahead
            # intent recorded the move before it happened, so absence means
            # the operation never performed. Reconcile as unperformed; never
            # treat as an error and never delete anything unverified.
            return
        }
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
            throw "Recorded artifact is absent: $Path"
        }
        $header = Read-KbpSaveHeader -Path $full
        if ([string]$header.Name -cne ($fixturePrefix + '_' + $Role)) {
            throw "Artifact header does not match its role: $Path"
        }
        # Provenance: the stable campaign identity recorded for the run, not
        # a display name. A substituted or foreign file at the same pathname
        # is refused.
        if (-not [string]::IsNullOrWhiteSpace($GameId) -and
            [string]$header.GameId -cne $GameId) {
            throw "Artifact campaign identity drifted from the run record: $Path"
        }
    }
    if ($inState -and -not (Test-Path -LiteralPath $full -PathType Container)) {
        throw "Recorded state path is absent: $Path"
    }
}

# StrictMode-safe record field read: absent fields are $null, and callers
# decide whether absence is acceptable for that field.
function Get-KbpRecordField {
    param($Record, [string]$Name)
    if ($null -eq $Record) { return $null }
    if ($Record -is [System.Collections.IDictionary]) {
        if ($Record.Contains($Name)) { return $Record[$Name] }
        return $null
    }
    $property = $Record.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Invoke-KbpOwnedRollback {
    param($Transaction)
    $gameId = [string](Get-KbpRecordField $Transaction 'gameId')
    if ([string]::IsNullOrWhiteSpace($gameId)) {
        throw 'Recovery record lacks campaign provenance; refusing to act on it. Re-run bootstrap after tearing down this state manually.'
    }
    foreach ($owned in @($Transaction.publishedPaths)) {
        if (-not $owned) { continue }
        $role = if ($owned -cmatch $baselineLabel) { 'BASELINE' } else { 'WORKING' }
        # Validates containment, filename contract, header role, and campaign
        # provenance before any removal. A journaled-but-absent destination
        # (write-ahead intent never performed, or cleanup already completed
        # by an earlier recovery attempt) reconciles as unperformed; anything
        # present must prove identity or the whole rollback refuses.
        Assert-KbpOwnedArtifactPath -Path $owned -Role $role -GameId $gameId -AllowAbsent
        if (Test-Path -LiteralPath $owned -PathType Leaf) {
            Remove-Item -LiteralPath $owned -Force
        }
    }
    if ($Transaction.stagingRoot) {
        # Staging cleanup is bound to THIS run's exact staging directory,
        # not merely any directory beneath the shared state root.
        $expectedStaging = Join-Path (Join-Path $StateRoot ([string]$Transaction.runId)) 'staging'
        if ([IO.Path]::GetFullPath([string]$Transaction.stagingRoot) -cne
            [IO.Path]::GetFullPath($expectedStaging)) {
            throw "Recorded staging root is not this run's staging directory: $($Transaction.stagingRoot)"
        }
        Assert-KbpOwnedArtifactPath -Path $Transaction.stagingRoot -Role 'WORKING' -GameId $null -AllowAbsent
        # Staging lives under StateRoot and was created empty by this run:
        # refuse to recurse through foreign content.
        $allowed = @(('Manual_[0-9]+_' + $baselineLabel + '\.zks'),
            ('Manual_[0-9]+_' + $workingLabel + '\.zks'),
            '^[0-9a-f]{32}\.partial$')
        foreach ($item in @(Get-ChildItem -LiteralPath $Transaction.stagingRoot -Force -ErrorAction SilentlyContinue)) {
            $ok = $false
            foreach ($pattern in $allowed) {
                if ($item.Name -cmatch $pattern) { $ok = $true; break }
            }
            if (-not $ok) { throw "Unknown staging content refuses rollback: $($item.FullName)" }
        }
        if (Test-Path -LiteralPath $Transaction.stagingRoot -PathType Container) {
            Remove-Item -LiteralPath $Transaction.stagingRoot -Force -Recurse
        }
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
    # Real process state first — recovery mutates the save directory too.
    Assert-KbpFixturePreconditions
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
    # The lock must belong to THIS transaction's run and carry its token:
    # a foreign or ambiguous lock is preserved, never released.
    if ([string](Get-KbpRecordField $lock 'runId') -cne [string](Get-KbpRecordField $existing 'runId') -or
        [string](Get-KbpRecordField $lock 'token') -cne [string](Get-KbpRecordField $existing 'token')) {
        throw "Fixture lock does not match the transaction record (lock run $($lock.runId) vs transaction run $($existing.runId)); refusing to release a foreign lock."
    }
    if (-not $PSCmdlet.ShouldProcess($runRoot,
        "roll back owned partial artifacts of interrupted run $RunId")) {
        Write-Host "Recovery WhatIf PASS: would roll back run $RunId; no artifact, record, or lock changed."
        return
    }
    Invoke-KbpOwnedRollback $existing
    Remove-KbpOwnedLock $lockPath $existing.runId $existing.token
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
    Assert-KbpFixturePreconditions
    $manifest = Read-KbpJson $ManifestPath
        if ($manifest.schemaVersion -ne 1 -or
            -not $manifest.baseline.fileName -or -not $manifest.baseline.sha256 -or
            -not $manifest.working.fileName) {
            throw 'Fixture manifest is not valid; refusing teardown.'
        }
        if ([string]::IsNullOrWhiteSpace([string]$manifest.gameId)) {
            throw 'Manifest lacks the stable campaign identity required for teardown provenance.'
        }
        $baselinePath = Join-Path $SaveRoot $manifest.baseline.fileName
        $workingPath = Join-Path $SaveRoot $manifest.working.fileName
        # Validate EVERYTHING (canonical containment, exact leaf filename,
        # role contract, mandatory campaign identity, sealed baseline hash)
        # before deleting anything: a partial teardown must never happen, and
        # an escaped/absolute/foreign manifest path is refused whole-set.
        foreach ($fixture in @(
                @{ Path = $baselinePath; Role = 'BASELINE'; Hash = [string]$manifest.baseline.sha256 },
                @{ Path = $workingPath; Role = 'WORKING'; Hash = $null })) {
            if ([string]::IsNullOrWhiteSpace($fixture.Hash)) { $fixture.Hash = $null }
            Assert-KbpOwnedArtifactPath -Path $fixture.Path -Role $fixture.Role `
                -GameId ([string]$manifest.gameId)
            if ($null -ne $fixture.Hash) {
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
            Write-Host 'Fixture teardown WhatIf PASS; no save, lock, or state changed.'
            return
        }
        # The mutation gate is behind us: take the lock now (never during a
        # dry run) and hold it across the deletion.
        New-Item -ItemType Directory -Path $StateRoot -Force | Out-Null
        $teardownToken = [Guid]::NewGuid().ToString('N')
        New-KbpOwnedLock $lockPath $RunId $teardownToken
        $deleted = $false
        $deleting = $false
        try {
            # Re-review (harness): rechecked while this lock is held (a
            # runtime entry re-checks this lock once it holds its own), before
            # anything is deleted.
            Assert-KbpFixturePreconditions
            $deleting = $true
            Remove-Item -LiteralPath $baselinePath -Force
            Remove-Item -LiteralPath $workingPath -Force
            $deleted = $true
        }
        finally {
            # A refusal before any deletion releases the lock; an interrupted
            # deletion keeps it for the owner's inspection.
            if ($deleted -or -not $deleting) {
                Remove-KbpOwnedLock $lockPath $RunId $teardownToken
            }
        }
        $protectedAfter = Get-KbpProtectedInventory -Root $SaveRoot `
            -ExcludeNames @($manifest.baseline.fileName, $manifest.working.fileName)
        if (($protectedBefore -join "`n") -cne ($protectedAfter -join "`n")) {
            throw 'Teardown touched a protected save; investigate immediately.'
        }
        Write-Host "Fixture teardown PASS: removed exactly the manifest-owned pair; $($protectedBefore.Count) other save(s) byte-identical."
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
    # Review O1: an existing run directory is NEVER removed here - not even
    # for a RolledBack attempt. That removal used to happen before the
    # preconditions, the seed checks and the outer ShouldProcess decision,
    # so a refused retry destroyed the prior transaction and its evidence.
    # A previous attempt is kept as recovery history; a new attempt needs a
    # fresh RunId. This check mutates nothing.
    $prior = Get-KbpTransaction
    $priorStatus = if ($null -eq $prior) { 'no-transaction-record' } else { [string]$prior.status }
    throw ("Run paths already exist: $runRoot (status=$priorStatus). They are preserved as history; " +
        "use a fresh -RunId for a new attempt, or -Recover an interrupted one.")
}
if (-not (Test-Path -LiteralPath $SaveRoot -PathType Container)) {
    throw "The exact Kingmaker save root is unavailable: $SaveRoot"
}
Assert-KbpFixturePreconditions

$automationFiles = @(Get-ChildItem -LiteralPath $SaveRoot -Filter ('*' + $fixturePrefix + '*') -File |
    Where-Object { $_.Name -notmatch ('^Manual_[0-9]+_' + $seedLabel + '\.zks$') })
if ($automationFiles.Count -ne 0) {
    throw ("Existing $fixturePrefix artifact(s) present: " +
        (($automationFiles | Select-Object -ExpandProperty Name) -join ', ') +
        ". Tear down explicitly first.")
}
$seedFiles = @(Get-ChildItem -LiteralPath $SaveRoot -Filter ('*_' + $seedLabel + '.zks') -File)
if ($seedFiles.Count -ne 1) {
    throw "Expected exactly one $seedLabel save; found $($seedFiles.Count). Create one disposable campaign save named exactly $seedLabel, then rerun."
}
$seed = $seedFiles[0]
if ($seed.Name -notmatch ('^Manual_[0-9]+_' + $seedLabel + '\.zks$')) {
    throw "Seed file name is not the exact in-game shape: $($seed.Name)"
}
$seedHeader = Read-KbpSaveHeader -Path $seed.FullName
if ([string]$seedHeader.Name -cne $seedLabel) {
    throw "Seed header Name is not exactly ${seedLabel}: $($seedHeader.Name)"
}

$allSaves = @(Get-ChildItem -LiteralPath $SaveRoot -Filter '*.zks' -File)
$maxIndex = 0
foreach ($file in $allSaves) {
    if ($file.Name -match '^Manual_([0-9]+)_') {
        $index = [int]$Matches[1]
        if ($index -gt $maxIndex) { $maxIndex = $index }
    }
}
$baselineName = 'Manual_' + ($maxIndex + 1) + '_' + $baselineLabel + '.zks'
$workingName = 'Manual_' + ($maxIndex + 2) + '_' + $workingLabel + '.zks'
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
$script:rollbackSucceeded = $false
$script:failAfterJournal = $FailAfterJournal
New-KbpOwnedLock $lockPath $RunId $token
$transaction = [ordered]@{
    schemaVersion = 1; runId = $RunId; token = $token
    status = 'Staging'; createdAtUtc = [DateTime]::UtcNow.ToString('o')
    saveRoot = $SaveRoot; seedPath = $seed.FullName
    gameId = [string]$seedHeader.GameId; gameName = [string]$seedHeader.GameName
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
    Copy-KbpSaveWithHeaderName -SourcePath $seed.FullName -DestinationPath $stagedBaseline -NewName $baselineLabel
    Copy-KbpSaveWithHeaderName -SourcePath $seed.FullName -DestinationPath $stagedWorking -NewName $workingLabel
    foreach ($staged in @(
            @{ Path = $stagedBaseline; Expected = $baselineLabel },
            @{ Path = $stagedWorking; Expected = $workingLabel })) {
        $header = Read-KbpSaveHeader -Path $staged.Path
        if ([string]$header.Name -cne $staged.Expected -or
            [string]$header.GameName -cne [string]$seedHeader.GameName -or
            [string]$header.GameId -cne [string]$seedHeader.GameId) {
            throw "Staged fixture failed descriptor verification: $(Split-Path -Leaf $staged.Path)"
        }
    }
    $transaction.status = 'Staged'
    Write-KbpJsonAtomic $transactionPath $transaction
    if ($FailRollback) {
        Set-Content -LiteralPath (Join-Path $stagingRoot 'foreign-rollback-blocker.txt') `
            -Value 'not ours'
    }
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
    if ([string]::IsNullOrWhiteSpace([string]$manifest.gameId)) {
        throw 'Manifest lacks the stable campaign identity required for teardown provenance.'
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

    # Real running-game, deployment-lock and other-lab lease recheck
    # immediately before touching SaveRoot (final review C8).
    Assert-KbpFixturePreconditions
    # Write-ahead ownership journal: each destination is recorded BEFORE its
    # move, so an interruption at any point leaves the published file
    # represented in the recovery record. Recovery validates identity before
    # removing anything, so a bystander at the same pathname is refused.
    Publish-KbpStagedFixture -StagedPath $stagedBaseline -DestinationPath $baselinePath
    if ($FailAfterMove -ceq 'baseline') { throw "Injected failure after publishing baseline." }
    Publish-KbpStagedFixture -StagedPath $stagedWorking -DestinationPath $workingPath
    if ($FailAfterMove -ceq 'working') { throw "Injected failure after publishing working." }
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
    $script:rollbackSucceeded = $true
}
catch {
    # Roll back owned state; the ORIGINAL failure always repropagates so a
    # failed bootstrap is never reported as success. If the rollback itself
    # fails, the lock STAYS so -Recover can finish it with the transaction's
    # verified ownership; never release another operation's lock and never
    # claim successful restoration.
    try {
        Invoke-KbpOwnedRollback $transaction
        $script:rollbackSucceeded = $true
    }
    catch {
        throw
    }
    throw
}
finally {
    # Release the lock only on success or completed rollback, and only after
    # verifying it still belongs to this run.
    if (Test-Path -LiteralPath $lockPath) {
        try {
            $lockRecord = Read-KbpJson $lockPath
            if ($lockRecord.runId -ceq $RunId -and
                ($script:rollbackSucceeded -or $transaction.status -ceq 'Completed')) {
                Remove-KbpOwnedLock $lockPath $lockRecord.runId $lockRecord.token
            }
        } catch { }
    }
}

Write-Host "Fixture bootstrap PASS:"
Write-Host "  baseline $baselineName sha256=$($manifest.baseline.sha256) (sealed; offline copy archived)"
Write-Host "  working  $workingName sha256=$($manifest.working.sha256) (only mutable fixture)"
Write-Host "  seed     $($seed.Name) untouched; $($allSaves.Count) pre-existing save(s) byte-identical"
Write-Host "  archive  $ArchiveRoot"
