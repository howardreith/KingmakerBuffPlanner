[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[a-z0-9-]{1,100}$')][string]$ProfileId,
    [string]$KingmakerInstallDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'CompatibilityProfile.Common.ps1')
$profile = Get-KbpCompatibilityProfile $ProfileId
if ($profile.status -cne 'available') {
    Write-Host "Compatibility profile unavailable: $ProfileId reason=$($profile.reason)"
    return
}
Assert-KbpCompatibilityProfileFixtures -Profile $profile -KingmakerInstallDir $KingmakerInstallDir
Write-Host "Compatibility profile: PASS=1 FAIL=0 id=$ProfileId mods=$(@($profile.mods).Count)"
