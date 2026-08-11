[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
& (Join-Path $PSScriptRoot 'Validate-Source.ps1')
$revision = (& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $revision -notmatch '^[0-9a-f]{40}$') { throw 'Exact Git revision is unavailable.' }
$msbuild = Get-KbpMsBuild
& $msbuild (Join-Path $root 'KingmakerBuffPlanner.sln') /t:Rebuild "/p:Configuration=$Configuration" "/p:KbpSourceRevision=$revision" /m /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE." }
$output = Join-Path $root "artifacts\build\$Configuration\KingmakerBuffPlanner.dll"
if (-not (Test-Path -LiteralPath $output -PathType Leaf)) { throw 'Expected product DLL was not produced.' }
Write-Host "Build: PASS=1 FAIL=0 configuration=$Configuration sha256=$(Get-KbpSha256 $output)"
Write-Output $output
