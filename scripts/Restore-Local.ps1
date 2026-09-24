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
# (the other lab checks that lock, so it cannot have run in between). The
# Mods folder is restored even when the comparison fails; the comparison's
# failure or violation is reported afterwards.
$saveFailure = $null
$baselinePath = Join-Path (Split-Path -Parent $statePath) 'protected-saves-before.json'
if (-not $SkipProtectedSaveComparison -and (Test-Path -LiteralPath $baselinePath -PathType Leaf)) {
    try {
        $comparison = Complete-KbpProtectedSaveComparison -BaselinePath $baselinePath `
            -EvidenceDirectory (Join-Path $script:KbpRuntimeEvidenceRoot $RunId)
        if (@($comparison.blocking).Count -ne 0) {
            $saveFailure = "Protected saves changed during run ${RunId}: " + (@($comparison.blocking) -join ', ')
        }
        else { Write-Host "Protected saves: clean run=$RunId" }
    }
    catch { $saveFailure = "The protected-save comparison of run $RunId failed: " + $_.Exception.Message }
}
$state = Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot $script:KbpRuntimeStateRoot
Write-Host "Runtime restoration: verified=$($state.restorationVerified) run=$RunId"
if ($null -ne $saveFailure) { throw $saveFailure }
