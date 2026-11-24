[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release', [string]$CompilerInventoryPath)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
# The bounded local build configuration applies to every build route that records or reuses a receipt (Build.ps1,
# Build-Local.ps1, Build-Release.ps1, receipt admission); direct MSBuild produces no receipt.
Assert-KbpLocalBuildConfiguration
& (Join-Path $PSScriptRoot 'Validate-Source.ps1')
# Compile against the pinned UMM 0.31.1 / Harmony12 references, never the installed loader (fetched and verified here;
# LoaderReferences.targets re-verifies them and the exact compiler inputs on every MSBuild build).
[void](& (Join-Path $PSScriptRoot 'Get-PinnedLoaderReferences.ps1') -ReferenceDirectory (Join-Path $root 'artifacts\references'))
$revision = (& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $revision -notmatch '^[0-9a-f]{40}$') { throw 'Exact Git revision is unavailable.' }
$msbuild = Get-KbpMsBuild
$inventoryArgument = @(if ($CompilerInventoryPath) { "/p:CompilerReferenceListPath=$CompilerInventoryPath" })
& $msbuild (Join-Path $root 'KingmakerBuffPlanner.sln') /t:Rebuild "/p:Configuration=$Configuration" "/p:KbpSourceRevision=$revision" @inventoryArgument /m /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE." }
$output = Join-Path $root "artifacts\build\$Configuration\KingmakerBuffPlanner.dll"
if (-not (Test-Path -LiteralPath $output -PathType Leaf)) { throw 'Expected product DLL was not produced.' }
Write-Host "Build: PASS=1 FAIL=0 configuration=$Configuration sha256=$(Get-KbpSha256 $output)"
Write-Output $output
