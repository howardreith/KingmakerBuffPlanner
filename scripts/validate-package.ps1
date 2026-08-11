[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$PackagePath)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
$package = (Resolve-Path -LiteralPath $PackagePath).Path
[void](Assert-KbpPathWithin -Path $package -Root (Join-Path $root 'artifacts'))
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $actual = @($archive.Entries | ForEach-Object FullName | Sort-Object)
    $expected = @(
        'KingmakerBuffPlanner/Info.json',
        'KingmakerBuffPlanner/KingmakerBuffPlanner.dll',
        'KingmakerBuffPlanner/THIRD-PARTY-NOTICES.md') | Sort-Object
    if (($actual -join "`n") -cne ($expected -join "`n")) {
        throw "Package entry allowlist mismatch. Observed: $($actual -join ', ')"
    }
    $fixedZipTime = [DateTime]::SpecifyKind([DateTime]'2000-01-01T00:00:00', [DateTimeKind]::Unspecified)
    foreach ($entry in $archive.Entries) {
        if ($entry.LastWriteTime.DateTime -ne $fixedZipTime) {
            throw "Package timestamp is not deterministic: $($entry.FullName)"
        }
    }
}
finally { $archive.Dispose() }

$temporary = Join-Path ([IO.Path]::GetTempPath()) ('kbp-package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    [IO.Compression.ZipFile]::ExtractToDirectory($package, $temporary)
    $mod = Join-Path $temporary 'KingmakerBuffPlanner'
    $info = Get-Content -LiteralPath (Join-Path $mod 'Info.json') -Raw | ConvertFrom-Json
    $name = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $mod 'KingmakerBuffPlanner.dll'))
    if ($info.Id -cne 'KingmakerBuffPlanner' -or
        $info.AssemblyName -cne 'KingmakerBuffPlanner.dll' -or
        $name.Name -cne 'KingmakerBuffPlanner' -or
        $info.Version -cne (Get-KbpVersion)) {
        throw 'Package identity/version validation failed.'
    }
}
finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
Write-Host "Package validation: PASS=4 FAIL=0 sha256=$(Get-KbpSha256 $package)"
