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
$releaseHead = (& git -C $root rev-parse HEAD).Trim()
# Each deterministic build's receipt is read while its output is current; both must name the clean release commit and
# agree with their packages, and the release ZIP (validated below) must equal both.
$firstPackage = & (Join-Path $PSScriptRoot 'Build-Local.ps1') | Select-Object -Last 1
$firstHash = Get-KbpSha256 $firstPackage
$firstManifest = Get-Content -LiteralPath ($firstPackage + '.build-local.json') -Raw | ConvertFrom-Json
$secondPackage = & (Join-Path $PSScriptRoot 'Build-Local.ps1') | Select-Object -Last 1
$secondHash = Get-KbpSha256 $secondPackage
$secondManifest = Get-Content -LiteralPath ($secondPackage + '.build-local.json') -Raw | ConvertFrom-Json
$builds = @([pscustomobject]@{ Manifest = $firstManifest; PackageSha256 = $firstHash }, [pscustomobject]@{ Manifest = $secondManifest; PackageSha256 = $secondHash })

# The replacement release is prepared and reconciled in a private folder under artifacts\release; the versioned release
# directory is replaced only after the ZIP and manifest are ready, so a refusal leaves the previous release untouched.
$releaseParent = Join-Path $root 'artifacts\release'
$finalReleaseRoot = Join-Path $releaseParent $version
[void](Assert-KbpPathWithin -Path ([IO.Path]::GetFullPath($finalReleaseRoot)) -Root $releaseParent)
$privateRelease = Join-Path $releaseParent ('.prepare-' + [Guid]::NewGuid().ToString('N'))
$releaseRoot = Join-Path $privateRelease $version
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
try {
$releasePackage = Join-Path $releaseRoot "KingmakerBuffPlanner-$version.zip"
Copy-Item -LiteralPath $secondPackage -Destination $releasePackage
$releaseValidation = Join-Path ([IO.Path]::GetTempPath()) ('kbp-release-validation-' + [Guid]::NewGuid().ToString('N') + '.json')
try {
    & (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $releasePackage -ReportPath $releaseValidation
    $validatedRelease = Get-Content -LiteralPath $releaseValidation -Raw | ConvertFrom-Json
}
finally { Remove-Item -LiteralPath $releaseValidation -Force -ErrorAction SilentlyContinue }
$releaseCommit = (& git -C $root rev-parse HEAD).Trim()
if ($releaseCommit -cne $releaseHead) { throw 'HEAD changed while building the release.' }
Assert-KbpReleaseReconciliation -ReleaseHead $releaseHead -Builds $builds -ValidatedRelease $validatedRelease -ReleasePackageSha256 (Get-KbpSha256 $releasePackage)
Copy-Item -LiteralPath (Join-Path $root 'docs\RELEASE-NOTES-DRAFT.md') `
    -Destination (Join-Path $releaseRoot 'RELEASE-NOTES-DRAFT.md')

$releaseManifest = [ordered]@{
    schemaVersion = 1
    generator = 'scripts/Build-Release.ps1'
    version = $version
    commit = $releaseCommit
    packagePath = (Join-Path $finalReleaseRoot "KingmakerBuffPlanner-$version.zip")
    packageSha256 = [string]$validatedRelease.package_sha256
    dllSha256 = [string]$validatedRelease.dll_sha256
    assemblyMvid = [string]$validatedRelease.dll_mvid
    embeddedCommit = [string]$validatedRelease.embedded_commit
    deterministicBuilds = 2
    validated = $true
    publicationStatus = 'local-only'
}
$releaseManifest | ConvertTo-Json -Depth 5 | Set-Content `
    -LiteralPath (Join-Path $releaseRoot 'release-manifest.json') -Encoding UTF8
# Replace the versioned release: the previous directory is moved aside, the prepared one moved in, and the previous one
# removed only after that succeeded (restored otherwise).
$previousRelease = Join-Path $privateRelease 'previous'
if (Test-Path -LiteralPath $finalReleaseRoot) { Move-Item -LiteralPath $finalReleaseRoot -Destination $previousRelease }
try { Move-Item -LiteralPath $releaseRoot -Destination $finalReleaseRoot }
catch {
    if ((Test-Path -LiteralPath $previousRelease) -and -not (Test-Path -LiteralPath $finalReleaseRoot)) { Move-Item -LiteralPath $previousRelease -Destination $finalReleaseRoot }
    throw
}
}
finally {
    if (Test-Path -LiteralPath $privateRelease) { Remove-Item -LiteralPath $privateRelease -Recurse -Force -ErrorAction SilentlyContinue }
}
$releasePackage = Join-Path $finalReleaseRoot "KingmakerBuffPlanner-$version.zip"
Write-Host "Release build: PASS=3 FAIL=0 deterministic=2 sha256=$($releaseManifest.packageSha256)"
Write-Output $releasePackage
