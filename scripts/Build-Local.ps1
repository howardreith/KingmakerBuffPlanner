[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
$version = Get-KbpVersion
# The receipt names what produced the DLL: the commit and dirty state before building, and the build-input fingerprint,
# which must not change while building. Only a clean build is eligible for frozen reuse by runtime qualification.
$commit = (& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $commit -cnotmatch '^[0-9a-f]{40}$') { throw 'Unable to read Git HEAD.' }
$sourceDirty = [bool](& git -C $root status --porcelain)
[void](& (Join-Path $PSScriptRoot 'Get-PinnedLoaderReferences.ps1') -ReferenceDirectory (Join-Path $root 'artifacts\references'))
$compilerInputs = @(Get-KbpCompilerInputs 'Release' $commit)
$buildInputs = Get-KbpBuildInputFingerprint 'Release' $compilerInputs
$productionInventory = Join-Path ([IO.Path]::GetTempPath()) ('kbp-production-inputs-' + [Guid]::NewGuid().ToString('N') + '.txt')
try {
    $dllOutput = & (Join-Path $PSScriptRoot 'Build.ps1') -Configuration Release -CompilerInventoryPath $productionInventory | Select-Object -Last 1
    $produced = @(ConvertFrom-CompilerInputList $productionInventory)
}
finally { Remove-Item -LiteralPath $productionInventory -Force -ErrorAction SilentlyContinue }
if ((ConvertTo-KbpCompilerInventoryText $produced) -cne (ConvertTo-KbpCompilerInventoryText $compilerInputs)) {
    throw 'The production compile used different compiler inputs (command line, generated or file-valued inputs) from the evaluated inventory; the build is not recorded.'
}
if ((& git -C $root rev-parse HEAD).Trim() -cne $commit -or (Get-KbpBuildInputFingerprint 'Release') -cne $buildInputs) {
    throw 'HEAD or a build input changed while building; the build is not recorded.'
}
$artifactRoot = Join-Path $root "artifacts\local-runtime\$version"
$staging = Join-Path $artifactRoot 'staging\KingmakerBuffPlanner'
if (Test-Path -LiteralPath $artifactRoot) {
    $resolved = [IO.Path]::GetFullPath($artifactRoot)
    [void](Assert-KbpPathWithin -Path $resolved -Root (Join-Path $root 'artifacts\local-runtime'))
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Copy-Item -LiteralPath $dllOutput -Destination (Join-Path $staging 'KingmakerBuffPlanner.dll')
Copy-Item -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\Info.json') -Destination (Join-Path $staging 'Info.json')
Copy-Item -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\NativeEffectOverrides.json') -Destination (Join-Path $staging 'NativeEffectOverrides.json')
Copy-Item -LiteralPath (Join-Path $root 'THIRD-PARTY-NOTICES.md') -Destination (Join-Path $staging 'THIRD-PARTY-NOTICES.md')
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $staging 'LICENSE')

$package = Join-Path $artifactRoot "KingmakerBuffPlanner-$version-local-runtime.zip"
Add-Type -AssemblyName System.IO.Compression
$stream = [IO.File]::Open($package, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        foreach ($file in @(Get-ChildItem -LiteralPath $staging -File | Sort-Object Name)) {
            $entry = $archive.CreateEntry('KingmakerBuffPlanner/' + $file.Name, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = [DateTimeOffset]'2000-01-01T00:00:00Z'
            $input = [IO.File]::OpenRead($file.FullName)
            $output = $entry.Open()
            try { $input.CopyTo($output) }
            finally { $output.Dispose(); $input.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}
finally { $stream.Dispose() }

# The validator hashes and inspects one private snapshot of the ZIP; the manifest takes the ZIP digest, the packaged
# DLL's digest, MVID and embedded source revision from that snapshot, and they must agree with the files produced.
$validationReport = Join-Path ([IO.Path]::GetTempPath()) ('kbp-build-local-validation-' + [Guid]::NewGuid().ToString('N') + '.json')
try {
    & (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $package -ReportPath $validationReport
    $validated = Get-Content -LiteralPath $validationReport -Raw | ConvertFrom-Json
}
finally { Remove-Item -LiteralPath $validationReport -Force -ErrorAction SilentlyContinue }
if ([string]$validated.package_sha256 -cne (Get-KbpSha256 $package) -or [string]$validated.dll_sha256 -cne (Get-KbpSha256 $dllOutput) -or
    [string]$validated.embedded_commit -cne $commit) {
    throw 'The validated package snapshot does not match the produced package, DLL or source revision.'
}
$manifest = [ordered]@{
    schemaVersion = 1
    generator = 'scripts/Build-Local.ps1'
    version = $version
    commit = $commit
    sourceDirty = $sourceDirty
    buildInputsSha256 = $buildInputs
    compilerInputs = $compilerInputs
    packagePath = $package
    packageSha256 = [string]$validated.package_sha256
    dllSha256 = [string]$validated.dll_sha256
    assemblyMvid = [string]$validated.dll_mvid
    embeddedCommit = [string]$validated.embedded_commit
    loaderReferences = $validated.dll_loader_references
    validated = $true
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath ($package + '.build-local.json') -Encoding UTF8
Write-Host "Local package: PASS=1 FAIL=0 path=$package"
Write-Output $package
