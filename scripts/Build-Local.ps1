[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
$version = Get-KbpVersion
$dllOutput = & (Join-Path $PSScriptRoot 'Build.ps1') -Configuration Release | Select-Object -Last 1
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

$escapedDllOutput = $dllOutput.Replace("'", "''")
$assemblyMvid = (& powershell.exe -NoProfile -NonInteractive -Command `
    "[Reflection.Assembly]::ReflectionOnlyLoadFrom('$escapedDllOutput').ManifestModule.ModuleVersionId.ToString('D')").Trim()
if ($LASTEXITCODE -ne 0 -or $assemblyMvid -notmatch '^[0-9a-f-]{36}$') {
    throw 'Short-lived MVID inspection failed.'
}
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $package
$manifest = [ordered]@{
    schemaVersion = 1
    generator = 'scripts/Build-Local.ps1'
    version = $version
    commit = (& git -C $root rev-parse HEAD).Trim()
    packagePath = $package
    packageSha256 = Get-KbpSha256 $package
    dllSha256 = Get-KbpSha256 $dllOutput
    assemblyMvid = $assemblyMvid
    validated = $true
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath ($package + '.build-local.json') -Encoding UTF8
Write-Host "Local package: PASS=1 FAIL=0 path=$package"
Write-Output $package
