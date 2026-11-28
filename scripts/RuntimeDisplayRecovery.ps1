# Restore-Local serializes recovery attempts for the same run.
function Assert-KbpDisplayRecoveryOwner($State, [switch]$BeforeAdmission) {
    Assert-KbpNotRunning
    Assert-KbpNoForeignRuntimeLease
    Assert-KbpFixtureLockAbsent (Get-KbpDefaultFixtureLockPath $script:KbpRuntimeStateRoot)
    Assert-KbpOwnedLock $State.lockPath $State.runId $State.token
    if ($BeforeAdmission) { return }
    # The recovery admission (Invoke-KbpDisplayRecovery) made this process the
    # reservation's writer; it is checked again before each recovery move.
    $reservationToken = Get-KbpStateReservationToken $State
    if ($null -eq $reservationToken) { throw 'Display recovery has no Kingmaker native-test reservation.' }
    Assert-KbpNativeReservationHeld -StateRoot $script:KbpRuntimeStateRoot -Token $reservationToken -RunId ([string]$State.runId) `
        -Targets (Get-KbpTransactionTargets $State $script:KbpRuntimeStateRoot) `
        -IncidentDirectory (Join-Path $script:KbpRuntimeStateRoot ('transactions\' + [string]$State.runId)) -Progress ([string]$State.status)
}

function Get-KbpRecoveryManifestDigest($Manifest) {
    $bytes = [Text.Encoding]::UTF8.GetBytes(($Manifest | ConvertTo-Json -Depth 8 -Compress))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Assert-KbpDisplayOnlyFailure($Completion, [string]$RunId) {
    # Reject any additional or unrecognized launcher cleanup failure.
    $parts = @($Completion.restorationFailure -split ' \| ')
    $mods = "Mods restoration failed for $RunId (Restore-Local.ps1 -RunId $RunId recovers it once the cause is fixed): "
    $settings = "Display/game-settings restoration verification failed after the windowed run (Restore-Local.ps1 -RunId $RunId retries or preserves it for review): "
    $start = 'Physical display state changed during run ' + $RunId + ': '
    $end = '. Refusing to overwrite a potentially newer user change; the pending snapshot must be reviewed before another run.'
    $field = '(?:primarySize|primaryWidth|primaryHeight|virtualLeft|virtualTop|virtualWidth|virtualHeight|monitorCount):[-0-9x]+->[-0-9x]+'
    $diagnostic = [regex]::Escape($start) + $field + '(?:, ' + $field + ')*' + [regex]::Escape($end)
    if ($Completion.complete -or $Completion.restorationVerified -or -not $Completion.kingmakerExited -or
        -not $Completion.protectedSavesApplicable -or -not $Completion.protectedSavesCompared -or
        -not $Completion.protectedSavesClean -or $parts.Count -ne 2 -or
        $parts[0] -cnotmatch ('^' + [regex]::Escape($mods) + $diagnostic + '$') -or
        $parts[1] -cnotmatch ('^' + [regex]::Escape($settings) + $diagnostic + '$')) {
        throw 'Completion receipt does not establish an isolated display verification failure.'
    }
}

function Move-KbpRecoveryDirectory($State, [string]$Source, [string]$Destination) {
    Assert-KbpDisplayRecoveryOwner $State
    # A same-volume rename refuses an occupied destination and cannot merge trees.
    [IO.Directory]::Move($Source, $Destination)
}

function Invoke-KbpDisplayRecovery([string]$RunId, [string]$StateRoot, [string]$EvidenceRoot, [switch]$ModsOnly) {
    if ($StateRoot -ine $script:KbpRuntimeStateRoot -or $EvidenceRoot -ine $script:KbpRuntimeEvidenceRoot -or
        $RunId -in @('.', '..') -or $RunId -notmatch '^[A-Za-z0-9._-]{1,100}$') { throw 'Invalid display recovery roots or identity.' }
    $dir = Join-Path $StateRoot "transactions\$RunId"
    $ev = Join-Path $EvidenceRoot $RunId
    $statePath = Join-Path $dir 'transaction.json'
    $s = Read-KbpJson $statePath
    $backupRoot = Join-Path $script:KbpRuntimeBackupRoot $RunId
    if ($s.schemaVersion -ne 1 -or $s.runId -cne $RunId -or $s.token -cnotmatch '^[a-f0-9]{32}$' -or
        $s.gameRoot -ine (Get-KbpGamePath) -or $s.modsPath -ine (Join-Path $s.gameRoot 'Mods') -or
        $s.originalBackup -ine (Join-Path $backupRoot 'Mods.original') -or
        $s.stagedQuarantine -ine (Join-Path $backupRoot 'Mods.staged') -or
        $s.stagingRunRoot -ine (Join-Path $script:KbpRuntimeStagingRoot $RunId) -or
        $s.lockPath -ine (Join-Path $StateRoot 'deployment.lock') -or -not $s.originalExisted) {
        throw 'Display recovery transaction identity or paths disagree.'
    }
    $resuming = $null -ne $s.PSObject.Properties['modsRecovery']
    if ($s.status -ceq 'Restored' -and $resuming -and -not (Test-Path -LiteralPath $s.lockPath)) {
        # A release interrupted after the finished recovery: admitted, then released.
        $finishedToken = Enter-KbpTransactionRecovery -State $s -StatePath $statePath -StateRoot $StateRoot -ExtraTargets @($EvidenceRoot)
        $s = Read-KbpJson $statePath
        if ($null -ne $finishedToken) {
            [void](Complete-KbpRunReservation -StateRoot $StateRoot -RunId $RunId -Token $finishedToken -EvidenceRoot $EvidenceRoot `
                -Targets (Get-KbpTransactionTargets $s $StateRoot))
        }
        return $s
    }
    Assert-KbpDisplayRecoveryOwner $s -BeforeAdmission
    if ((-not $resuming -and ($s.status -cne 'Active' -or -not $ModsOnly -or $s.restorationVerified)) -or
        ($resuming -and $s.status -cnotin @('RecoveringMods','ModsRestoredPendingDisplay','Restored'))) {
        throw 'Transaction phase is ineligible for display recovery.'
    }

    $displayPath = Join-Path $dir 'display-physical.json'
    $originalDisplay = Join-Path $dir 'display-physical.original.json'
    $display = Read-KbpPhysicalDisplaySnapshotFile $displayPath
    $registryPath = Join-Path $dir 'display-registry.json'
    $registry = Read-KbpRegistrySnapshotFile $registryPath
    $baselinePath = Join-Path $dir 'protected-saves-before.json'
    $savePath = Join-Path $ev 'protected-saves.json'
    $completionPath = Join-Path $ev 'run-completion.json'
    $baseline = Read-KbpJson $baselinePath
    $saves = Read-KbpJson $savePath
    $completion = Read-KbpJson $completionPath
    foreach ($record in @($baseline,$saves,$completion,$display,$registry)) {
        if ($record.runId -cne $RunId) { throw 'Foreign display recovery receipt.' }
    }
    Assert-KbpDisplayOnlyFailure $completion $RunId
    $lockTime = [DateTimeOffset](Read-KbpJson $s.lockPath).createdAtUtc
    $activated = [DateTimeOffset]$s.activatedAtUtc
    $completed = [DateTimeOffset]$completion.completedAtUtc
    $registryEvidence = Read-KbpJson $registryPath
    if ($lockTime -gt [DateTimeOffset]$s.createdAtUtc -or [DateTimeOffset]$s.createdAtUtc -gt $activated -or
        [DateTimeOffset]$display.savedAtUtc -lt $activated -or [DateTimeOffset]$display.savedAtUtc -gt $completed -or
        [DateTimeOffset]$registryEvidence.savedAtUtc -lt $activated -or
        [DateTimeOffset]$registryEvidence.restoredAtUtc -lt [DateTimeOffset]$registryEvidence.savedAtUtc -or
        [DateTimeOffset]$registryEvidence.restoredAtUtc -gt $completed -or
        [DateTimeOffset]$baseline.comparedAtUtc -lt $activated -or [DateTimeOffset]$baseline.comparedAtUtc -gt $completed) {
        throw 'Cleanup receipts fall outside the held transaction.'
    }
    if (-not $baseline.compared -or @($baseline.blocking).Count -or @($saves.violations).Count -or
        @($saves.blocking).Count -or $baseline.scenario -cne $completion.scenario -or
        $baseline.fixtureFamily -cne $saves.fixtureFamily -or $saves.fixtureFamily -cne $completion.fixtureFamily -or
        $baseline.workingFileName -cne $completion.fixture.workingFileName -or
        (Test-Path -LiteralPath (Join-Path $StateRoot "protected-save-violations\$RunId.json"))) {
        throw 'Missing clean protected-save comparison under the transaction lock.'
    }
    $before = [ordered]@{}
    foreach ($file in $baseline.files) { $before.Add($file.name,$file) }
    $policy = Get-KbpProtectedSavePolicy $baseline.scenario $baseline.fixtureFamily $baseline.workingFileName
    $after = Get-KbpSaveFolderSnapshot $baseline.saveRoot
    $violations = Compare-KbpSaveFolderSnapshot $before $after -AllowedChangedFileNames @($policy.allowedChanged)
    if ($violations.Count) {
        throw 'Protected saves differ from the recorded clean comparison.'
    }
    if (-not $registry.restored -or @(Compare-KbpRegistrySnapshot $registry.snapshot (Get-KbpRegistryValueSnapshot $registry.keyPath)).Count) {
        throw 'Game registry restoration is unverified.'
    }
    $current = Get-KbpSessionDisplaySnapshot
    $diff = @(Compare-KbpPhysicalDisplaySnapshot $display.original $current)
    if (-not $resuming -and ($display.restored -or -not $diff.Count)) { throw 'Use ordinary recovery for a matching display.' }

    $proof = [ordered]@{ completion = Get-KbpSha256 $completionPath; registry = Get-KbpSha256 $registryPath
        saveBaseline = Get-KbpSha256 $baselinePath; saveComparison = Get-KbpSha256 $savePath
        lock = Get-KbpSha256 $s.lockPath; originalManifest = Get-KbpRecoveryManifestDigest $s.originalManifest }
    if ($resuming) {
        foreach ($key in $proof.Keys) {
            if ($s.modsRecovery.proof.$key -cne $proof[$key]) { throw "Recovery evidence changed: $key" }
        }
        if ((Get-KbpSha256 $originalDisplay) -cne $s.modsRecovery.displaySha256 -or
            (-not $display.restored -and (Get-KbpSha256 $displayPath) -cne $s.modsRecovery.displaySha256) -or
            @(Compare-KbpPhysicalDisplaySnapshot (Read-KbpPhysicalDisplaySnapshotFile $originalDisplay).original $display.original).Count) {
            throw 'Original display evidence changed.'
        }
    }

    $live = Test-Path -LiteralPath $s.modsPath -PathType Container
    foreach ($path in @($s.modsPath,$s.originalBackup,$s.stagedQuarantine,$s.stagingRunRoot,$dir,$ev)) {
        while ($path) {
            if ((Test-Path -LiteralPath $path) -and
                ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw "Ambiguous reparse point in recovery paths: $path"
            }
            $path = Split-Path -Parent $path
        }
    }
    $backup = Test-Path -LiteralPath $s.originalBackup -PathType Container
    $kept = Test-Path -LiteralPath $s.stagedQuarantine -PathType Container
    if (-not (($backup -and $live -and -not $kept) -or
            ($resuming -and $kept -and ($live -xor $backup)))) { throw 'Ambiguous live Mods, backup or quarantine layout.' }
    $original = if ($backup) { $s.originalBackup } else { $s.modsPath }
    if (-not (Test-KbpManifestEqual $s.originalManifest @(Get-KbpDirectoryManifest $original))) {
        throw 'Original Mods manifest/hash mismatch.'
    }
    Assert-KbpExternallyStagedLiveRestored $s $original
    $candidate = if ($kept) { $s.stagedQuarantine } else { $s.modsPath }
    $sentinel = Read-KbpJson (Join-Path $candidate '.kbp-runtime-sentinel.json')
    if ($sentinel.schemaVersion -ne 1 -or $sentinel.runId -cne $RunId -or $sentinel.token -cne $s.token -or
        $sentinel.statePath -cne $statePath) { throw 'Candidate sentinel does not own this transaction.' }
    $observed = @(Get-KbpDirectoryManifest $candidate)
    # Recovery admission after every read-only check and before the first
    # display, save-evidence or Mods write: this process becomes the
    # reservation's writer (or takes a free one for a transaction recorded
    # before this lab took part).
    $recoveryToken = Enter-KbpTransactionRecovery -State $s -StatePath $statePath -StateRoot $StateRoot -ExtraTargets @($EvidenceRoot)
    $s | Add-Member -NotePropertyName reservationToken -NotePropertyValue $recoveryToken -Force
    Assert-KbpDisplayRecoveryOwner $s
    if (-not $resuming) {
        if (-not (Test-Path -LiteralPath $originalDisplay)) { [IO.File]::Copy($displayPath, $originalDisplay, $false) }
        if ((Get-KbpSha256 $originalDisplay) -cne (Get-KbpSha256 $displayPath)) { throw 'Archived display evidence differs.' }
        $s.stagedMutationObserved = -not (Test-KbpManifestEqual $s.stagedManifest $observed)
        $s.observedStagedManifest = $observed
        $s | Add-Member -NotePropertyName modsRecovery -NotePropertyValue ([pscustomobject]@{
            proof = $proof; displaySha256 = Get-KbpSha256 $displayPath; stagedManifest = Get-KbpRecoveryManifestDigest $observed })
        $s.status = 'RecoveringMods'
        Write-KbpJsonAtomic $statePath $s
    } elseif (-not (Test-KbpManifestEqual $s.observedStagedManifest $observed) -or
        (Get-KbpRecoveryManifestDigest $observed) -cne $s.modsRecovery.stagedManifest) { throw 'Retained candidate evidence changed.' }

    if (-not $kept) { Move-KbpRecoveryDirectory $s $s.modsPath $s.stagedQuarantine }
    if ($backup) { Move-KbpRecoveryDirectory $s $s.originalBackup $s.modsPath }
    if (-not $kept -and -not (Test-KbpManifestEqual $observed @(Get-KbpDirectoryManifest $s.stagedQuarantine))) {
        throw 'Quarantined candidate manifest/hash mismatch.'
    }
    if (-not (Test-KbpManifestEqual $s.originalManifest @(Get-KbpDirectoryManifest $s.modsPath))) {
        throw 'Restored Mods manifest/hash mismatch.'
    }
    Assert-KbpExternallyStagedLiveRestored $s $s.modsPath
    Assert-KbpDisplayRecoveryOwner $s
    $receiptPath = Join-Path $ev 'mods-recovery.json'
    if (-not (Test-Path -LiteralPath $receiptPath)) {
        $head = git -C (Get-KbpRepositoryRoot) rev-parse HEAD
        if ($LASTEXITCODE -ne 0 -or $head -notmatch '^[0-9a-f]{40}$') { throw 'Missing recovery harness identity.' }
        Write-KbpJsonAtomic $receiptPath ([ordered]@{ schemaVersion = 1; runId = $RunId; verifiedAtUtc = [DateTime]::UtcNow.ToString('o')
            harnessCommit = $head; recoveryScriptSha256 = Get-KbpSha256 $PSCommandPath; packageSha256 = $s.packageSha256
            verifiedManifestEntries = @($s.originalManifest).Count; verifiedLiveManifestSha256 = $proof.originalManifest
            proof = $proof; candidateManifestSha256 = $s.modsRecovery.stagedManifest; candidateEvidence = $s.stagedQuarantine
            stagedMutationObserved = $s.stagedMutationObserved; originalDisplaySha256 = $s.modsRecovery.displaySha256
            remainingObligation = 'Physical display verification and full transaction finalization'; displayDifferences = $diff })
    }
    $receipt = Read-KbpJson $receiptPath
    if ($receipt.runId -cne $RunId -or $receipt.verifiedLiveManifestSha256 -cne $proof.originalManifest -or
        $receipt.proof.completion -cne $proof.completion -or $receipt.candidateManifestSha256 -cne $s.modsRecovery.stagedManifest) {
        throw 'Conflicting recovery receipt.'
    }
    if ($s.status -ceq 'RecoveringMods') {
        $s.status = 'ModsRestoredPendingDisplay'
        $s | Add-Member -NotePropertyName stagedQuarantineKept -NotePropertyValue $true -Force
        Write-KbpJsonAtomic $statePath $s
    }
    if ($ModsOnly) { return $s }
    # Keep the original byte-exact snapshot even after the active copy is confirmed.
    $verified = Confirm-KbpPhysicalDisplaySnapshotFile -Path $displayPath -CurrentSnapshot (Get-KbpSessionDisplaySnapshot) -Reverify
    Assert-KbpDisplayRecoveryOwner $s
    if (Test-Path -LiteralPath $s.stagingRunRoot) {
        # The candidate was renamed out of staging. Never remove unexpected contents.
        [IO.Directory]::Delete($s.stagingRunRoot, $false)
    }
    $s.status = 'Restored'
    $s.restorationVerified = [bool]$verified.restored
    $s.restoredAtUtc = [DateTime]::UtcNow.ToString('o')
    Write-KbpJsonAtomic $statePath $s
    Remove-KbpOwnedLock $s.lockPath $RunId $s.token
    [void](Complete-KbpRunReservation -StateRoot $StateRoot -RunId $RunId -Token (Get-KbpStateReservationToken $s) -EvidenceRoot $EvidenceRoot `
        -Targets (Get-KbpTransactionTargets $s $StateRoot))
    return (Read-KbpJson $statePath)
}
