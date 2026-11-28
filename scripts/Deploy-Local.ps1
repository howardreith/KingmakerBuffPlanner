[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    [ValidatePattern('^[a-z0-9-]{1,100}$')][string]$CompatibilityProfileId = 'native-only',
    [ValidateSet('0.31.1', '0.32.4', 'owner-0.33.0', '0.33.4')][string]$LoaderId = 'owner-0.33.0',
    [string]$KingmakerInstallDir,
    # The digest the caller admitted (the launcher passes its receipt's package digest).
    [ValidatePattern('^([0-9a-f]{64})?$')][string]$ExpectedPackageSha256,
    # The Kingmaker native-test reservation the public launcher holds for this
    # run (it keeps it); a real standalone deployment acquires its own first.
    [ValidatePattern('^([0-9a-f]{32})?$')][string]$ReservationToken
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'compatibility\CompatibilityProfile.Common.ps1')

$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false
# A real standalone deployment reserves before any mutable read and keeps the
# reservation with the transaction it enters; a refusal before that releases.
$deployLease = $null
if (-not $requestedWhatIf -and [string]::IsNullOrEmpty($ReservationToken)) {
    $gameForLease = if ($KingmakerInstallDir) { $KingmakerInstallDir } else { Get-KbpGamePath }
    $deployLease = Enter-KbpNativeReservation -RunId $RunId -Purpose 'deploy-local' -StateRoot $script:KbpRuntimeStateRoot `
        -Targets @($script:KbpRuntimeStagingRoot, $script:KbpRuntimeBackupRoot, $gameForLease)
    $ReservationToken = [string]$deployLease.token
}
try {
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
    -CompatibilityProfile $compatibilityProfile -ExpectedPackageSha256 $ExpectedPackageSha256 -ReservationToken $ReservationToken
}
finally {
    # An own reservation stays with an entered (active) transaction; it is
    # released only when nothing of the run remains unresolved (refused before
    # any change, or rolled back and verified).
    if ($null -ne $deployLease -and (Test-KbpRunSettled $script:KbpRuntimeStateRoot $RunId)) {
        Exit-KbpNativeReservation -RunId $RunId -Token $deployLease.token -StateRoot $script:KbpRuntimeStateRoot `
            -Targets @($script:KbpRuntimeStagingRoot, $script:KbpRuntimeBackupRoot, $gameForLease)
    }
}
