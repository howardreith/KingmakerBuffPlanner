[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param([Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

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
$state = Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot $script:KbpRuntimeStateRoot
Write-Host "Runtime restoration: verified=$($state.restorationVerified) run=$RunId"
