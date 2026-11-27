[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    [ValidatePattern('^[a-z0-9-]{1,100}$')][string]$CompatibilityProfileId = 'native-only',
    [ValidateSet('0.31.1', '0.32.4', 'owner-0.33.0', '0.33.4')][string]$LoaderId = 'owner-0.33.0',
    [string]$KingmakerInstallDir,
    # The digest the caller admitted (the launcher passes its receipt's package digest).
    [ValidatePattern('^([0-9a-f]{64})?$')][string]$ExpectedPackageSha256
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'compatibility\CompatibilityProfile.Common.ps1')

$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false
if (-not $KingmakerInstallDir) { $KingmakerInstallDir = Get-KbpGamePath }
$deployValidation = Join-Path ([IO.Path]::GetTempPath()) ('kbp-deploy-validation-' + [Guid]::NewGuid().ToString('N') + '.json')
try {
    & (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $PackagePath -ReportPath $deployValidation
    $validatedPackageSha256 = [string](Get-Content -LiteralPath $deployValidation -Raw | ConvertFrom-Json).package_sha256
}
finally { Remove-Item -LiteralPath $deployValidation -Force -ErrorAction SilentlyContinue }
if ($ExpectedPackageSha256 -and $validatedPackageSha256 -cne $ExpectedPackageSha256) {
    throw "The package does not match the admitted digest $ExpectedPackageSha256."
}
if (-not $ExpectedPackageSha256) { $ExpectedPackageSha256 = $validatedPackageSha256 }
$compatibilityProfile = Get-KbpCompatibilityProfile $CompatibilityProfileId
[void](Assert-KbpInstallationPreflight -Profile $compatibilityProfile -Loader (Get-KbpLoaderIdentity $LoaderId) `
    -KingmakerInstallDir $KingmakerInstallDir)
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
    -BackupRoot $script:KbpRuntimeBackupRoot -RunId $RunId `
    -CompatibilityProfile $compatibilityProfile -ExpectedPackageSha256 $ExpectedPackageSha256
