[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[a-z0-9-]{1,100}$')][string]$ProfileId,
    [string]$KingmakerInstallDir,
    [ValidateSet('0.31.1', '0.32.4', 'owner-0.33.0', '0.33.4')][string]$LoaderId = 'owner-0.33.0'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'CompatibilityProfile.Common.ps1')
$profile = Get-KbpCompatibilityProfile $ProfileId
if ($profile.status -cne 'available') {
    Write-Host "Compatibility profile unavailable: $ProfileId reason=$($profile.reason)"
    return
}
[void](Assert-KbpInstallationPreflight -Profile $profile -Loader (Get-KbpLoaderIdentity $LoaderId) -KingmakerInstallDir $KingmakerInstallDir)
Write-Host "Compatibility profile: PASS=1 FAIL=0 id=$ProfileId mods=$(@($profile.mods).Count) loader=$LoaderId"
