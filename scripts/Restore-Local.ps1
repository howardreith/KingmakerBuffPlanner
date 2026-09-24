[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    # The launcher compares the protected saves itself (before calling this).
    [switch]$SkipProtectedSaveComparison)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')

$statePath = Join-Path $script:KbpRuntimeStateRoot ('transactions\' + $RunId + '\transaction.json')
if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) { throw "Runtime transaction state is missing: $statePath" }
$baselinePath = Join-Path (Split-Path -Parent $statePath) 'protected-saves-before.json'
$pending = (Test-Path -LiteralPath $baselinePath -PathType Leaf) -and -not [bool](Read-KbpJson $baselinePath).compared
# Re-review (harness): the launcher skips the comparison only after making
# it; while it is pending, it is never skipped.
if ($SkipProtectedSaveComparison -and $pending) {
    throw "Refusing -SkipProtectedSaveComparison: the protected-save comparison of run $RunId is pending (run Restore-Local.ps1 -RunId $RunId without it)."
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
$evidenceDirectory = Join-Path $script:KbpRuntimeEvidenceRoot $RunId
if ($pending -and [string](Read-KbpJson $statePath).status -ceq 'Restored') {
    $unverifiable = Close-KbpUnverifiableProtectedSaveComparison -BaselinePath $baselinePath `
        -Reason 'lock-released-before-comparison' -EvidenceDirectory $evidenceDirectory
    throw ("The protected saves of run $RunId can no longer be compared (its lock was released first); " +
        "recorded as $(@($unverifiable) -join ', ') for the owner's review.")
}
if ($pending) {
    try {
        $comparison = Complete-KbpProtectedSaveComparison -BaselinePath $baselinePath -EvidenceDirectory $evidenceDirectory
    }
    catch {
        throw ("The protected-save comparison of run $RunId failed; the Mods folder was not restored and the run's lock " +
            "is kept (fix the cause, then run Restore-Local.ps1 -RunId $RunId again): " + $_.Exception.Message)
    }
    if (@($comparison.blocking).Count -ne 0) {
        $saveFailure = "Protected saves changed during run ${RunId}: " + (@($comparison.blocking) -join ', ')
    }
    else { Write-Host "Protected saves: clean run=$RunId" }
}
# Re-review (harness): a restoration failure never hides a save violation.
try {
    $state = Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot $script:KbpRuntimeStateRoot
}
catch {
    if ($null -ne $saveFailure) { throw ($saveFailure + ' | Restoration: ' + $_.Exception.Message) }
    throw
}
Write-Host "Runtime restoration: verified=$($state.restorationVerified) run=$RunId"
if ($null -ne $saveFailure) { throw $saveFailure }
