[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param([Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$statePath = Join-Path $script:KbpRuntimeStateRoot ('transactions\' + $RunId + '\transaction.json')
if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) { throw "Runtime transaction state is missing: $statePath" }
if (-not $PSCmdlet.ShouldProcess($RunId, 'restore and hash-verify the exact pre-run Mods state')) { return }
$state = Restore-KbpRuntimeTransaction -RunId $RunId -StateRoot $script:KbpRuntimeStateRoot
Write-Host "Runtime restoration: verified=$($state.restorationVerified) run=$RunId"
