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
        'KingmakerBuffPlanner/LICENSE',
        'KingmakerBuffPlanner/NativeEffectOverrides.json',
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
    # Installing over an earlier release depends on the unchanged loader
    # identity; the public UMM listing shows the branded name and author.
    if ($info.EntryMethod -cne 'KingmakerBuffPlanner.Main.Load' -or
        $info.DisplayName -cne "Mr. Bard's Kingmaker Buff Planner" -or
        $info.Author -cne 'Mr. Bard') {
        throw 'Package entry point or public UMM metadata validation failed.'
    }
    # Verify the bytes distributed inside the assembly. Loading from bytes leaves the extracted DLL unlocked.
    $assembly = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes((Join-Path $mod 'KingmakerBuffPlanner.dll')))
    # UMM 0.31.1 baseline: the packaged manifest declares 0.31.1 and the packaged DLL's actual AssemblyRefs name exactly the
    # pinned UnityModManager and 0Harmony12; lowering Info.json on a DLL compiled against a newer loader is refused.
    if (-not ($info.ManagerVersion -is [string]) -or $info.ManagerVersion -cne '0.31.1') {
        throw "Packaged ManagerVersion must be the JSON string 0.31.1."
    }
    $referenced = @($assembly.GetReferencedAssemblies())
    foreach ($pin in @((Get-Content -LiteralPath (Join-Path $root 'scripts\loader-reference-pins.json') -Raw | ConvertFrom-Json).references)) {
        $matches = @($referenced | Where-Object { $_.Name -ceq [string]$pin.Name })
        if ($matches.Count -ne 1 -or $matches[0].Version.ToString() -cne [string]$pin.Version) {
            throw "Packaged DLL must reference pinned $($pin.Name) $($pin.Version); lowering Info.json alone is not sufficient."
        }
    }
    # Documentation and data entries must be text: no assembly can travel under an allowed name.
    foreach ($textEntry in @('THIRD-PARTY-NOTICES.md', 'NativeEffectOverrides.json', 'LICENSE', 'Info.json')) {
        $textBytes = [IO.File]::ReadAllBytes((Join-Path $mod $textEntry))
        if (($textBytes.Length -ge 2 -and $textBytes[0] -eq 0x4D -and $textBytes[1] -eq 0x5A) -or [Array]::IndexOf($textBytes, [byte]0) -ge 0) {
            throw "Package text entry contains binary content: $textEntry"
        }
        try { [void][Text.UTF8Encoding]::new($false, $true).GetString($textBytes) }
        catch { throw "Package text entry is not valid UTF-8 text: $textEntry" }
    }
    foreach ($locale in @('enGB', 'zhCN')) {
        $catalog = $assembly.GetManifestResourceStream("KBP.Text.$locale.json")
        if ($null -eq $catalog) { throw "Missing packaged catalog for $locale." }
        try { $hash = (Get-FileHash -Algorithm SHA256 -InputStream $catalog).Hash.ToLowerInvariant() }
        finally { $catalog.Dispose() }
        if ($hash -cne (Get-KbpSha256 (Join-Path $root "src/KingmakerBuffPlanner/Localization/$locale.json"))) {
            throw "Packaged catalog differs from the source catalog for $locale."
        }
    }
    # The installable package carries the repository's MIT license verbatim.
    if ((Get-KbpSha256 (Join-Path $mod 'LICENSE')) -cne (Get-KbpSha256 (Join-Path $root 'LICENSE'))) {
        throw 'Packaged LICENSE does not match the repository LICENSE.'
    }
}
finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
Write-Host "Package validation: PASS=11 FAIL=0 sha256=$(Get-KbpSha256 $package)"
