[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    # Recover the original Mods under the held lock after an isolated display mismatch.
    [switch]$RestoreModsOnly,
    # The launcher compares the protected saves itself (before calling this).
    [switch]$SkipProtectedSaveComparison,
    # Focused re-review: for a comparison that can never be made (its
    # baseline or a save cannot be read), this records it as an unverifiable
    # change - so no later run or fixture change starts until the owner has
    # reviewed it - then restores the Mods folder and releases the lock.
    [switch]$CloseUnverifiableComparison,
    # Test seams: the lab's own roots unless a test names others.
    [string]$StateRoot,
    [string]$EvidenceRoot,
    # The Kingmaker native-test reservation the public launcher holds for this
    # run (it keeps and releases it); standalone recovery takes its own.
    [ValidatePattern('^([0-9a-f]{32})?$')][string]$ReservationToken)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
if ([string]::IsNullOrEmpty($StateRoot)) { $StateRoot = $script:KbpRuntimeStateRoot }
if ([string]::IsNullOrEmpty($EvidenceRoot)) { $EvidenceRoot = $script:KbpRuntimeEvidenceRoot }

$statePath = Join-Path $StateRoot ('transactions\' + $RunId + '\transaction.json')
if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) { throw "Runtime transaction state is missing: $statePath" }
$baselinePath = Join-Path (Split-Path -Parent $statePath) 'protected-saves-before.json'
$pending = $false
if (Test-Path -LiteralPath $baselinePath -PathType Leaf) {
    # An unreadable baseline is a pending comparison that cannot be made.
    try { $pending = -not [bool](Read-KbpJson $baselinePath).compared }
    catch { $pending = $true }
}
# Re-review (harness): the launcher skips the comparison only after making
# it; while it is pending, it is never skipped.
if ($SkipProtectedSaveComparison -and $pending) {
    throw "Refusing -SkipProtectedSaveComparison: the protected-save comparison of run $RunId is pending (run Restore-Local.ps1 -RunId $RunId without it)."
}
if ($CloseUnverifiableComparison -and -not $pending) {
    throw "Refusing -CloseUnverifiableComparison: no protected-save comparison of run $RunId is pending."
}
if (-not $PSCmdlet.ShouldProcess($RunId, 'restore and hash-verify the exact pre-run Mods state')) { return }
# Recovery admission, before any registry, display, save-evidence or Mods
# step (docs/NATIVE-RESERVATION.md): the journal must be valid; the
# reservation is the launcher's for exactly this run, the journaled one
# (continued only while the record names this run, token and state root), or,
# for a finished run or a transaction recorded before this lab took part, a
# free one taken now. Anything else refuses and leaves everything.
function Enter-KbpLocalRecoveryLease {
    $journal = Read-KbpJson $statePath
    if ($journal.schemaVersion -isnot [int] -or [int]$journal.schemaVersion -ne 1 -or [string]$journal.runId -cne $RunId -or
        @('Preparing', 'Prepared', 'OriginalMoved', 'Active', 'RestorationFailed', 'Restored') -cnotcontains [string]$journal.status) {
        throw "Runtime transaction state identity is invalid; recovery admits nothing: $statePath"
    }
    $targets = @($StateRoot, $EvidenceRoot, [string](Get-KbpRecordValue $journal 'modsPath'), [string](Get-KbpRecordValue $journal 'originalBackup'),
        [string](Get-KbpRecordValue $journal 'stagedQuarantine'), [string](Get-KbpRecordValue $journal 'stagingRunRoot'),
        [string](Get-KbpRecordValue $journal 'lockPath')) | Where-Object { -not [string]::IsNullOrEmpty($_) }
    $registryFile = Join-Path (Split-Path -Parent $statePath) 'display-registry.json'
    if ((Get-KbpReservationConvention -Targets $targets).Private -and (Test-Path -LiteralPath $registryFile)) {
        throw 'A private test reservation is refused for a run that restores a game registry key.'
    }
    $journaled = Get-KbpStateReservationToken $journal
    if (-not [string]::IsNullOrEmpty($ReservationToken)) {
        if ($journaled -cne $ReservationToken) { throw "Run $RunId's journal does not name the launcher's reservation; nothing was changed." }
        Assert-KbpNativeReservation -RunId $RunId -Token $ReservationToken -StateRoot $StateRoot -Targets $targets
        return [pscustomobject]@{ Token = $ReservationToken; Own = $false; Targets = $targets }
    }
    if ($null -ne $journaled -and [string]$journal.status -cne 'Restored') {
        Assert-KbpNativeReservation -RunId $RunId -Token $journaled -StateRoot $StateRoot -Targets $targets
        return [pscustomobject]@{ Token = $journaled; Own = $true; Targets = $targets }
    }
    $fresh = Enter-KbpNativeReservation -RunId $RunId -Purpose 'restore-local' -StateRoot $StateRoot -Targets $targets
    if ($null -eq $journaled) {
        $journal | Add-Member -NotePropertyName reservationToken -NotePropertyValue ([string]$fresh.token) -Force
        Write-KbpJsonAtomic $statePath $journal
    }
    return [pscustomobject]@{ Token = [string]$fresh.token; Own = $true; Targets = $targets }
}

function Get-KbpRecordValue($Record, [string]$Name) {
    if ($null -eq $Record.PSObject.Properties[$Name]) { return $null }
    return $Record.$Name
}

function Invoke-LocalRecovery {
    $transaction = Read-KbpJson $statePath
    if ($RestoreModsOnly -or $null -ne $transaction.PSObject.Properties['modsRecovery']) {
        if ($SkipProtectedSaveComparison -or $CloseUnverifiableComparison) { throw 'Display recovery requires clean save evidence.' }
        . (Join-Path $PSScriptRoot 'RuntimeDisplayRecovery.ps1')
        $script:KbpRecoveryLease = [pscustomobject]@{ RunId = $RunId; Token = $recoveryLease.Token; StateRoot = $StateRoot; Targets = $recoveryLease.Targets }
        $recovered = Invoke-KbpDisplayRecovery $RunId $StateRoot $EvidenceRoot -ModsOnly:$RestoreModsOnly
        Write-Host "Mods recovery: run=$RunId phase=$($recovered.status) restorationVerified=$($recovered.restorationVerified)"
        return
    }
# A display-mode run's saved game registry comes back first (the game must
# not run); an already restored snapshot is left as it is.
$registryPath = Join-Path (Split-Path -Parent $statePath) 'display-registry.json'
if (Test-Path -LiteralPath $registryPath -PathType Leaf) {
    $registryRestored = @(Restore-KbpRegistrySnapshotFile -Path $registryPath)
    Write-Host "Game registry restoration: verified=True run=$RunId values=$($registryRestored.Count)"
}
$physicalDisplayPath = Join-Path (Split-Path -Parent $statePath) 'display-physical.json'
if (Test-Path -LiteralPath $physicalDisplayPath -PathType Leaf) {
    $displayTransactionActive = [string](Read-KbpJson $statePath).status -cne 'Restored'
    $physicalDisplay = Confirm-KbpPhysicalDisplaySnapshotFile -Path $physicalDisplayPath `
        -CurrentSnapshot (Get-KbpSessionDisplaySnapshot) -Reverify:$displayTransactionActive
    Write-Host ("Physical display restoration: verified=" + [bool]$physicalDisplay.restored +
        " run=$RunId size=" + [string]$physicalDisplay.original.primarySize)
}
# Final review C2: a protected-save comparison the launcher could not make
# is finished here, before the Mods restoration releases this run's lock
# (the other lab checks that lock, so it cannot have run in between). A
# violation is reported after the Mods folder is restored. Re-review
# (harness): a comparison that cannot be made keeps the lock (nothing is
# restored), and one whose lock was already released is recorded as
# unverifiable for the owner's review instead of being compared.
$saveFailure = $null
$evidenceDirectory = Join-Path $EvidenceRoot $RunId
if ($pending) {
    # Focused re-review: compared only while this run still holds its lock
    # (a lock removed by hand, or already released, means another operation
    # may have changed the saves since); otherwise it is recorded as
    # unverifiable for the owner's review instead.
    $state = Read-KbpJson $statePath
    $unverifiableReason = if ([string]$state.status -ceq 'Restored') { 'lock-released-before-comparison' }
        elseif (-not (Test-KbpRunLockHeld -State $state -RunId $RunId)) { 'lock-not-held' }
        else { $null }
    if ($null -ne $unverifiableReason) {
        $unverifiable = Close-KbpUnverifiableProtectedSaveComparison -BaselinePath $baselinePath `
            -Reason $unverifiableReason -EvidenceDirectory $evidenceDirectory -StateRoot $StateRoot -RunId $RunId
        $recorded = "recorded as $(@($unverifiable) -join ', ') for the owner's review"
        if ($unverifiableReason -ceq 'lock-not-held') {
            throw ("The protected saves of run $RunId can no longer be compared under its lock; $recorded. Its " +
                "deployment lock ($([string]$state.lockPath)) is missing or not its own, so no script can restore the " +
                "Mods folder either: the owner compares the Mods folder with the run's backup under runtime-backups " +
                "and resolves the transaction by hand.")
        }
        throw "The protected saves of run $RunId can no longer be compared under its lock; $recorded."
    }
}
if ($pending) {
    $comparison = $null
    try {
        $comparison = Complete-KbpProtectedSaveComparison -BaselinePath $baselinePath -EvidenceDirectory $evidenceDirectory `
            -StateRoot $StateRoot
    }
    catch {
        $compareError = $_.Exception.Message
        if (-not $CloseUnverifiableComparison) {
            throw ("The protected-save comparison of run $RunId failed; the Mods folder was not restored and the run's lock " +
                "is kept. Fix the cause and run Restore-Local.ps1 -RunId $RunId again; if it can never be made, " +
                "Restore-Local.ps1 -RunId $RunId -CloseUnverifiableComparison records it for the owner's review and " +
                "restores: " + $compareError)
        }
        # Targeted review: closed only after the comparison was tried and
        # failed, never while the game runs (a passing cause), and with why;
        # last review: tried once more once the game is known to be closed.
        Assert-KbpNotRunning
        try {
            $comparison = Complete-KbpProtectedSaveComparison -BaselinePath $baselinePath -EvidenceDirectory $evidenceDirectory `
                -StateRoot $StateRoot
        }
        catch {
            $compareError = $_.Exception.Message
            $unverifiable = Close-KbpUnverifiableProtectedSaveComparison -BaselinePath $baselinePath `
                -Reason 'comparison-failed' -Detail $compareError -EvidenceDirectory $evidenceDirectory `
                -StateRoot $StateRoot -RunId $RunId
            $saveFailure = "The protected saves of run $RunId could not be compared ($compareError); recorded as " +
                "$(@($unverifiable) -join ', ') for the owner's review."
        }
    }
    if ($null -ne $comparison) {
        if (@($comparison.blocking).Count -ne 0) {
            $saveFailure = "Protected saves changed during run ${RunId}: " + (@($comparison.blocking) -join ', ')
        }
        else { Write-Host "Protected saves: clean run=$RunId" }
    }
}
# Re-review (harness): a restoration failure never hides a save violation.
try {
    $restoreBorrow = @{}
    if ([string](Read-KbpJson $statePath).status -cne 'Restored') { $restoreBorrow.BorrowedToken = $recoveryLease.Token }
    $state = Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot $StateRoot @restoreBorrow
}
catch {
    if ($null -ne $saveFailure) { throw ($saveFailure + ' | Restoration: ' + $_.Exception.Message) }
    throw
}
Write-Host "Runtime restoration: verified=$($state.restorationVerified) run=$RunId"
if ($null -ne $saveFailure) { throw $saveFailure }
}

# Serializes retries, including ordinary restoration racing an explicit recovery.
# A process interruption abandons the mutex but keeps the durable deployment lock.
$mutex = [Threading.Mutex]::new($false, ('Global\KbpRestore-' + $RunId.ToLowerInvariant()))
$held = $false
try {
    try { $held = $mutex.WaitOne(0) } catch [Threading.AbandonedMutexException] { $held = $true }
    if (-not $held) { throw "Another restore is active for $RunId." }
    $recoveryLease = Enter-KbpLocalRecoveryLease
    Invoke-LocalRecovery
    # Released last, only after the verified recovery left the run settled;
    # any failure above keeps it for the owner (the launcher releases its own).
    if ($recoveryLease.Own -and (Test-KbpRunSettled $StateRoot $RunId)) {
        Exit-KbpNativeReservation -RunId $RunId -Token $recoveryLease.Token -StateRoot $StateRoot -Targets $recoveryLease.Targets
    }
} finally {
    if ($held) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
}
