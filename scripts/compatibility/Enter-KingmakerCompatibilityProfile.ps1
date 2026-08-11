[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[a-z0-9-]{1,100}$')][string]$ProfileId,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [string]$KingmakerInstallDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$deploy = Join-Path (Split-Path -Parent $PSScriptRoot) 'Deploy-Local.ps1'
& $deploy -PackagePath $PackagePath -RunId $RunId -CompatibilityProfileId $ProfileId `
    -KingmakerInstallDir $KingmakerInstallDir -WhatIf:$WhatIfPreference -Confirm:$false
