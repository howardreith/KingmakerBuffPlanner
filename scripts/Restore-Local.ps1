[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    # The launcher compares the protected saves itself (before calling this).
    [switch]$SkipProtectedSaveComparison,
    # Focused re-review: for a comparison that can never be made (its
    # baseline or a save cannot be read), this records it as an unverifiable
    # change - so no later run or fixture change starts until the owner has
    # reviewed it - then restores the Mods folder and releases the lock.
    [switch]$CloseUnverifiableComparison,
    # Test seams: the lab's own roots unless a test names others.
    [string]$StateRoot,
    [string]$EvidenceRoot)

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
# A display-mode run's saved game registry comes back first (the game must
# not run); an already restored snapshot is left as it is.
$registryPath = Join-Path (Split-Path -Parent $statePath) 'display-registry.json'
if (Test-Path -LiteralPath $registryPath -PathType Leaf) {
    $registryRestored = @(Restore-KbpRegistrySnapshotFile -Path $registryPath)
    Write-Host "Game registry restoration: verified=True run=$RunId values=$($registryRestored.Count)"
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
                "deployment lock is missing or not its own, so this script cannot restore the Mods folder either: " +
                "the owner inspects runtime-state\deployment.lock and the Mods folder.")
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
        # failed, never while the game runs (a passing cause), and with why.
        Assert-KbpNotRunning
        $unverifiable = Close-KbpUnverifiableProtectedSaveComparison -BaselinePath $baselinePath `
            -Reason 'comparison-failed' -Detail $compareError -EvidenceDirectory $evidenceDirectory `
            -StateRoot $StateRoot -RunId $RunId
        $saveFailure = "The protected saves of run $RunId could not be compared ($compareError); recorded as " +
            "$(@($unverifiable) -join ', ') for the owner's review."
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
    $state = Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot $StateRoot
}
catch {
    if ($null -ne $saveFailure) { throw ($saveFailure + ' | Restoration: ' + $_.Exception.Message) }
    throw
}
Write-Host "Runtime restoration: verified=$($state.restorationVerified) run=$RunId"
if ($null -ne $saveFailure) { throw $saveFailure }
