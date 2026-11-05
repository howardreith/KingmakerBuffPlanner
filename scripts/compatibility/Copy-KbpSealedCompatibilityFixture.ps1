[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
    [Parameter(Mandatory = $true)][string]$ProfileId,
    [Parameter(Mandatory = $true)][string]$UmmId,
    [Parameter(Mandatory = $true)][string]$ArchivePath
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'CompatibilityProfile.Common.ps1')
$profile = Get-KbpCompatibilityProfile $ProfileId
$matches = @($profile.mods | Where-Object { [string]$_.ummId -ceq $UmmId })
if ($matches.Count -ne 1) { throw 'Expected exactly one sealed profile entry.' }
$expected = $matches[0]
if ($expected.PSObject.Properties.Name -notcontains 'fixtureRelativePath') {
    throw 'The reviewed profile must name an external fixture destination.'
}
$source = [IO.Path]::GetFullPath($ArchivePath)
[void](Assert-KbpPathWithin -Path $source -Root (Join-Path $script:KbpLabRoot 'runtime-backups'))
$destination = Get-KbpCompatibilitySourcePath $expected (Join-Path (Get-KbpGamePath) 'Mods')
[void](Assert-KbpPathWithin -Path $destination -Root $script:KbpExternalFixtureRoot)
[void](Assert-KbpCompatibilityModIdentity $expected $source)
if (Test-Path -LiteralPath $destination) {
    [void](Assert-KbpCompatibilityModIdentity $expected $destination)
    Write-Output "Verified existing sealed fixture: $destination"
    return
}
if ($PSCmdlet.ShouldProcess($destination, 'Copy archived bytes matching the unchanged sealed profile')) {
    [void](New-Item -ItemType Directory -Path $destination -ErrorAction Stop)
    # Copy only the verified immutable source. Never edit or replace the live mod.
    Get-ChildItem -LiteralPath $source -Force | Copy-Item -Destination $destination -Recurse -Force -ErrorAction Stop
    [void](Assert-KbpCompatibilityModIdentity $expected $destination)
    Write-Output "Copied and verified sealed fixture: $destination"
}
