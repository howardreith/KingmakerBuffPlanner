Set-StrictMode -Version Latest

function Get-KbpRepositoryRoot {
    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
}

function Get-KbpVersion {
    $root = Get-KbpRepositoryRoot
    [xml]$props = Get-Content -LiteralPath (Join-Path $root 'Version.props') -Raw
    return [string]$props.Project.PropertyGroup.KbpVersion
}

# The compiler inputs of the production project for a configuration as MSBuild itself evaluates them (GamePath.props,
# Version.props and every other import included): the exact references Csc receives after reference-assembly selection,
# source/resource/analyzer items and compile-affecting properties, as path-free identities.
function Get-KbpCompilerInputs([string]$Configuration = 'Release') {
    $root = Get-KbpRepositoryRoot
    $id = [Guid]::NewGuid().ToString('N')
    $list = Join-Path ([IO.Path]::GetTempPath()) ("kbp-compiler-inputs-$id.txt")
    $obj = Join-Path ([IO.Path]::GetTempPath()) ("kbp-compiler-inputs-$id\")
    $project = Join-Path $root 'src\KingmakerBuffPlanner\KingmakerBuffPlanner.csproj'
    $msbuild = Get-KbpMsBuild
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = $null | & $msbuild $project /nologo /v:q /nr:false '/t:ResolveReferences;FindReferenceAssembliesForReferences' `
            "/p:Configuration=$Configuration" "/p:CompilerReferenceListPath=$list" "/p:IntermediateOutputPath=$obj" 2>&1 | ForEach-Object { "$_" }
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $list)) { throw "Could not resolve the compiler inputs: $($output -join ' ')" }
        $rootPrefix = [IO.Path]::GetFullPath($root).TrimEnd('\') + '\'
        return @(Get-Content -LiteralPath $list -Encoding UTF8 | Where-Object { $_ } | ForEach-Object {
            $kind, $value = $_ -split '\|', 2
            if ($kind -ceq 'setting') { [ordered]@{ kind = $kind; name = $value; sha256 = '' } }
            else {
                $name = if ($value.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { $value.Substring($rootPrefix.Length).Replace('\', '/') } else { [IO.Path]::GetFileName($value) }
                $digest = if (Test-Path -LiteralPath $value -PathType Leaf) { Get-KbpSha256 $value } else { 'absent' }
                [ordered]@{ kind = $kind; name = $name; sha256 = $digest }
            }
        } | Sort-Object { $_.kind + '|' + $_.name + '|' + $_.sha256 })
    }
    finally {
        $ErrorActionPreference = $previous
        Remove-Item -LiteralPath $list -Force -ErrorAction SilentlyContinue
        if ([IO.Directory]::Exists($obj)) { [IO.Directory]::Delete($obj, $true) }
    }
}

# Material inputs of the production DLL: tracked or new files under src (ignored build output excluded), the solution,
# version and loader-pin definitions, the build wrappers, and the MSBuild-evaluated compiler inputs (which include
# ignored local references such as the game assemblies selected through GamePath.props).
function Get-KbpBuildInputFingerprint([string]$Configuration = 'Release') {
    $root = Get-KbpRepositoryRoot
    $paths = @('src', 'KingmakerBuffPlanner.sln', 'Version.props', 'LoaderReferences.targets', 'scripts/loader-reference-pins.json',
        'scripts/Get-PinnedLoaderReferences.ps1', 'scripts/Build.ps1', 'scripts/Build-Local.ps1', 'scripts/Common.ps1')
    $files = @(& git -C $root ls-files --cached --others --exclude-standard -- @paths)
    if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed while fingerprinting build inputs.' }
    $lines = @(foreach ($file in ($files | Sort-Object -Unique)) {
        $full = Join-Path $root $file
        if (Test-Path -LiteralPath $full -PathType Leaf) { $file + "`t" + (Get-KbpSha256 $full) }
    })
    foreach ($input in (Get-KbpCompilerInputs $Configuration)) { $lines += 'compiler-' + $input.kind + ':' + $input.name + "`t" + $input.sha256 }
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $digest = $hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))) }
    finally { $hasher.Dispose() }
    return ([BitConverter]::ToString($digest)).Replace('-', '').ToLowerInvariant()
}

function Get-KbpGamePath {
    $root = Get-KbpRepositoryRoot
    $propsPath = Join-Path $root 'GamePath.props'
    if (-not (Test-Path -LiteralPath $propsPath -PathType Leaf)) {
        throw 'GamePath.props is missing. Create the ignored machine-local file first.'
    }
    [xml]$props = Get-Content -LiteralPath $propsPath -Raw
    $path = [string]$props.Project.PropertyGroup.KingmakerInstallDir
    if ([string]::IsNullOrWhiteSpace($path) -or
        -not (Test-Path -LiteralPath (Join-Path $path 'Kingmaker.exe') -PathType Leaf)) {
        throw 'GamePath.props does not identify an installed Kingmaker root.'
    }
    return (Resolve-Path -LiteralPath $path).Path
}

function Get-KbpMsBuild {
    $vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) { throw 'vswhere.exe is missing.' }
    $matches = @(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe')
    if ($LASTEXITCODE -ne 0 -or $matches.Count -ne 1) { throw 'MSBuild must resolve exactly once.' }
    return (Resolve-Path -LiteralPath $matches[0]).Path
}

function Get-KbpSha256([string]$Path) {
    # Get-FileHash's LiteralPath branch asks the filesystem provider for its
    # ProviderPath. Under an inherited WhatIfPreference that provider access
    # is simulated and the command returns no hash object. Hash an explicitly
    # read-only stream instead: WhatIf remains armed for every mutating
    # operation while real bytes still pass through the built-in SHA256 tool.
    # Resolve-Path applies PowerShell's current-location and provider semantics;
    # IO.Path.GetFullPath instead uses the process current directory, which can
    # legitimately differ after Set-Location or Push-Location. The literal form
    # also keeps wildcard metacharacters in filenames literal.
    $resolved = @(Resolve-Path -LiteralPath $Path -ErrorAction Stop)
    if ($resolved.Count -ne 1 -or $null -eq $resolved[0].Provider -or
        [string]$resolved[0].Provider.Name -cne 'FileSystem') {
        throw "SHA256 hashing supports exactly one FileSystem path: $Path"
    }
    $stream = [IO.File]::OpenRead([string]$resolved[0].Path)
    try {
        $result = @(Get-FileHash -InputStream $stream -Algorithm SHA256)
    }
    finally {
        $stream.Dispose()
    }
    if ($result.Count -ne 1 -or $null -eq $result[0].PSObject.Properties['Hash'] -or
        [string]::IsNullOrWhiteSpace([string]$result[0].Hash)) {
        throw "SHA256 hashing returned no usable Hash for: $Path"
    }
    return ([string]$result[0].Hash).ToLowerInvariant()
}

function Assert-KbpPathWithin {
    param([Parameter(Mandatory = $true)][string]$Path,
          [Parameter(Mandatory = $true)][string]$Root,
          [switch]$AllowRoot)
    $fullPath = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    $fullRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    if ((-not $AllowRoot -and $fullPath -eq $fullRoot) -or
        -not $fullPath.StartsWith($fullRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes required root '$fullRoot': $fullPath"
    }
    return $fullPath
}
