[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    [string]$KingmakerInstallDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false
if (-not $KingmakerInstallDir) { $KingmakerInstallDir = Get-KbpGamePath }
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $PackagePath
Assert-KbpNotRunning
Assert-KbpNoUnresolvedTransaction $script:KbpRuntimeStateRoot
$WhatIfPreference = $requestedWhatIf
if (-not $PSCmdlet.ShouldProcess(
    (Join-Path $KingmakerInstallDir 'Mods'),
    "transactionally stage Kingmaker Buff Planner for run $RunId")) {
    Write-Host 'Deployment preflight PASS; WhatIf made no runtime-state, staging, backup, game, or save mutation.'
    return
}

Enter-KbpRuntimeTransaction -PackagePath $PackagePath -KingmakerInstallDir $KingmakerInstallDir `
    -StateRoot $script:KbpRuntimeStateRoot -StagingRoot $script:KbpRuntimeStagingRoot `
    -BackupRoot $script:KbpRuntimeBackupRoot -RunId $RunId
