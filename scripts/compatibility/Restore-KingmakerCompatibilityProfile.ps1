[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param([Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$restore = Join-Path (Split-Path -Parent $PSScriptRoot) 'Restore-Local.ps1'
& $restore -RunId $RunId -WhatIf:$WhatIfPreference -Confirm:$false
