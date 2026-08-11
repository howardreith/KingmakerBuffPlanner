[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
$status = @(& git -C $root status --porcelain)
if ($LASTEXITCODE -ne 0 -or $status.Count -ne 0) {
    throw 'Release packaging requires a clean Git worktree.'
}
$version = Get-KbpVersion
$firstPackage = & (Join-Path $PSScriptRoot 'Build-Local.ps1') | Select-Object -Last 1
$firstHash = Get-KbpSha256 $firstPackage
$firstDllHash = (Get-Content -LiteralPath ($firstPackage + '.build-local.json') -Raw | ConvertFrom-Json).dllSha256
$secondPackage = & (Join-Path $PSScriptRoot 'Build-Local.ps1') | Select-Object -Last 1
$secondHash = Get-KbpSha256 $secondPackage
$secondManifest = Get-Content -LiteralPath ($secondPackage + '.build-local.json') -Raw | ConvertFrom-Json
if ($firstHash -cne $secondHash -or $firstDllHash -cne [string]$secondManifest.dllSha256) {
    throw "Clean-head deterministic build failed: package $firstHash/$secondHash DLL $firstDllHash/$($secondManifest.dllSha256)"
}

$releaseRoot = Join-Path $root "artifacts\release\$version"
if (Test-Path -LiteralPath $releaseRoot) {
    $resolvedRelease = [IO.Path]::GetFullPath($releaseRoot)
    [void](Assert-KbpPathWithin -Path $resolvedRelease -Root (Join-Path $root 'artifacts\release'))
    Remove-Item -LiteralPath $resolvedRelease -Recurse -Force
}
New-Item -ItemType Directory -Path $releaseRoot | Out-Null
$releasePackage = Join-Path $releaseRoot "KingmakerBuffPlanner-$version.zip"
Copy-Item -LiteralPath $secondPackage -Destination $releasePackage
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $releasePackage
Copy-Item -LiteralPath (Join-Path $root 'docs\RELEASE-NOTES-DRAFT.md') `
    -Destination (Join-Path $releaseRoot 'RELEASE-NOTES-DRAFT.md')

$releaseManifest = [ordered]@{
    schemaVersion = 1
    generator = 'scripts/Build-Release.ps1'
    version = $version
    commit = (& git -C $root rev-parse HEAD).Trim()
    packagePath = $releasePackage
    packageSha256 = Get-KbpSha256 $releasePackage
    dllSha256 = [string]$secondManifest.dllSha256
    deterministicBuilds = 2
    validated = $true
    publicationStatus = 'local-only'
}
$releaseManifest | ConvertTo-Json -Depth 5 | Set-Content `
    -LiteralPath (Join-Path $releaseRoot 'release-manifest.json') -Encoding UTF8
Write-Host "Release build: PASS=3 FAIL=0 deterministic=2 sha256=$($releaseManifest.packageSha256)"
Write-Output $releasePackage
