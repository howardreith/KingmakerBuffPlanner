Set-StrictMode -Version Latest

function Get-KbpRepositoryRoot {
    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
}

function Get-KbpVersion {
    $root = Get-KbpRepositoryRoot
    [xml]$props = Get-Content -LiteralPath (Join-Path $root 'Version.props') -Raw
    return [string]$props.Project.PropertyGroup.KbpVersion
}

# Reads a compiler-input list written by the WriteCompilerReferenceList target (the intermediate directory, then every Csc
# argument) into path-free identities in a canonical order: kind reference/compile/file-option with the file's name
# (repository-relative, obj:<name> for generated files in the intermediate directory, otherwise the file name) and
# SHA-256; kind option for every other argument verbatim; kind output for output locations (file name only).
function ConvertFrom-CompilerInputList([string] $ListPath) {
    $rootPrefix = [IO.Path]::GetFullPath((Get-KbpRepositoryRoot)).TrimEnd('\') + '\'
    $lines = @(Get-Content -LiteralPath $ListPath -Encoding UTF8 | Where-Object { $_ })
    if ($lines.Count -lt 4 -or -not $lines[0].StartsWith('intermediate|') -or -not $lines[1].StartsWith('project|') -or -not $lines[2].StartsWith('compiler|')) { throw "Compiler input list is malformed: $ListPath" }
    $objPrefix = [IO.Path]::GetFullPath($lines[0].Substring('intermediate|'.Length)).TrimEnd('\') + '\'
    $projectDirectory = $lines[1].Substring('project|'.Length)
    # Csc arguments name files relative to the project directory.
    function Resolve-InputPath([string] $Path) { return [IO.Path]::GetFullPath([IO.Path]::Combine($projectDirectory, $Path.Trim('"'))) }
    function Get-InputName([string] $Path) {
        $full = Resolve-InputPath $Path
        # MSBuild's generated target-framework attribute file lives in the intermediate or the temporary directory.
        if ([IO.Path]::GetFileName($full) -like '*.AssemblyAttributes.cs') { return 'generated:' + [IO.Path]::GetFileName($full) }
        if ($full.StartsWith($objPrefix, [StringComparison]::OrdinalIgnoreCase)) { return 'obj:' + $full.Substring($objPrefix.Length).Replace('\', '/') }
        if ($full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { return $full.Substring($rootPrefix.Length).Replace('\', '/') }
        return [IO.Path]::GetFileName($full)
    }
    function Get-InputDigest([string] $Path) { $full = Resolve-InputPath $Path; if (Test-Path -LiteralPath $full -PathType Leaf) { return Get-KbpSha256 $full } else { return 'absent' } }
    $outputSwitches = @('out', 'pdb', 'refout', 'doc', 'errorlog', 'generatedfilesout')
    $fileSwitches = @('win32res', 'win32icon', 'win32manifest', 'keyfile', 'ruleset', 'appconfig', 'analyzer', 'a', 'additionalfile', 'analyzerconfig', 'embed', 'sourcelink', 'addmodule')
    $resourceSwitches = @('resource', 'res', 'linkresource', 'linkres')
    # The effective compiler binary (and, without /noconfig, the implicit csc.rsp beside it) is an input.
    $compiler = $lines[2].Substring('compiler|'.Length)
    $compilerEntry = [ordered]@{ kind = 'compiler'; name = [IO.Path]::GetFileName($compiler); sha256 = $(if (Test-Path -LiteralPath $compiler -PathType Leaf) { Get-KbpSha256 $compiler } else { 'absent' }) }
    $cscArgs = @($lines[3..($lines.Count - 1)] | ForEach-Object { if (-not $_.StartsWith('arg|')) { throw "Compiler input list is malformed: $_" }; $_.Substring(4) })
    $implicit = @()
    if ($cscArgs -notcontains '/noconfig') {
        $rsp = Join-Path (Split-Path -Parent $compiler) 'csc.rsp'
        $implicit += [ordered]@{ kind = 'response-file'; name = 'implicit:csc.rsp'; sha256 = $(if (Test-Path -LiteralPath $rsp -PathType Leaf) { Get-KbpSha256 $rsp } else { 'absent' }) }
    }
    $entries = foreach ($line in @($cscArgs | ForEach-Object { 'arg|' + $_ })) {
        if (-not $line.StartsWith('arg|')) { throw "Compiler input list is malformed: $line" }
        $arg = $line.Substring(4)
        if ($arg.StartsWith('@')) { [ordered]@{ kind = 'response-file'; name = '@' + (Get-InputName $arg.Substring(1)); sha256 = Get-InputDigest $arg.Substring(1) }; continue }
        $m = [regex]::Match($arg, '^[/-]([A-Za-z0-9_+-]+?)(?::(.*))?$')
        if (-not $m.Success) { [ordered]@{ kind = 'compile'; name = Get-InputName $arg; sha256 = Get-InputDigest $arg }; continue }
        $switch = $m.Groups[1].Value.ToLowerInvariant(); $value = $m.Groups[2].Value
        if ($outputSwitches -ccontains $switch) { [ordered]@{ kind = 'output'; name = "/$switch" + ':' + [IO.Path]::GetFileName($value.Trim('"')); sha256 = '' } }
        elseif ($switch -ceq 'reference' -or $switch -ceq 'r') {
            $alias, $path = if ($value -match '^[A-Za-z_][A-Za-z0-9_]*=') { $value -split '=', 2 } else { $null, $value }
            [ordered]@{ kind = 'reference'; name = $(if ($alias) { "$alias=" } else { '' }) + (Get-InputName $path); sha256 = Get-InputDigest $path }
        }
        elseif ($resourceSwitches -ccontains $switch) {
            $path, $rest = $value -split ',', 2
            [ordered]@{ kind = 'file-option'; name = "/$switch" + ':' + (Get-InputName $path) + $(if ($rest) { ',' + $rest } else { '' }); sha256 = Get-InputDigest $path }
        }
        elseif ($fileSwitches -ccontains $switch -and $value) { [ordered]@{ kind = 'file-option'; name = "/$switch" + ':' + (Get-InputName $value); sha256 = Get-InputDigest $value } }
        else { [ordered]@{ kind = 'option'; name = $arg.Replace($objPrefix.TrimEnd('\'), 'obj:'); sha256 = '' } }
    }
    # Argument order is kept: the inventory is the compiler, any implicit response file, then every argument as passed.
    return @(@($compilerEntry) + $implicit + @($entries))
}


# The bounded local build configuration: ignored GamePath.props may only set KingmakerInstallDir (no conditions, imports,
# items, targets or other properties), and no Directory.Build.props/.targets outside the repository may apply. Anything
# else could change the production compile without changing the evaluated inventory, so it is refused at build and reuse.
function Assert-KbpLocalBuildConfiguration {
    $root = Get-KbpRepositoryRoot
    $props = Join-Path $root 'GamePath.props'
    if (-not (Test-Path -LiteralPath $props -PathType Leaf)) { throw 'GamePath.props is missing.' }
    [xml]$xml = Get-Content -LiteralPath $props -Raw
    $project = $xml.DocumentElement
    $groups = @($project.ChildNodes | Where-Object { $_.NodeType -eq 'Element' })
    $ok = $project.LocalName -ceq 'Project' -and @($project.Attributes | Where-Object { $_.Name -notin @('xmlns', 'ToolsVersion') }).Count -eq 0 -and
        $groups.Count -eq 1 -and $groups[0].LocalName -ceq 'PropertyGroup' -and $groups[0].Attributes.Count -eq 0
    if ($ok) {
        $properties = @($groups[0].ChildNodes | Where-Object { $_.NodeType -eq 'Element' })
        $ok = $properties.Count -eq 1 -and $properties[0].LocalName -ceq 'KingmakerInstallDir' -and $properties[0].Attributes.Count -eq 0 -and
            @($properties[0].ChildNodes | Where-Object { $_.NodeType -eq 'Element' }).Count -eq 0
    }
    if (-not $ok) { throw 'GamePath.props may only define KingmakerInstallDir (no conditions, imports, items, targets or other properties).' }
    $dir = Split-Path -Parent ([IO.Path]::GetFullPath($root).TrimEnd('\'))
    while ($dir) {
        foreach ($name in @('Directory.Build.props', 'Directory.Build.targets')) {
            if (Test-Path -LiteralPath (Join-Path $dir $name) -PathType Leaf) { throw "An outside $name applies to the build: $(Join-Path $dir $name)" }
        }
        $parent = Split-Path -Parent $dir
        if ($parent -eq $dir) { break }
        $dir = $parent
    }
}

function ConvertTo-KbpCompilerInventoryText($Inventory) {
    return (@($Inventory | ForEach-Object { [string]$_.kind + '|' + [string]$_.name + '|' + [string]$_.sha256 }) -join "`n")
}

# The production compile's exact Csc command line, evaluated the way Build.ps1 builds: the solution, the configuration and
# the source revision stamped into the generated BuildInfo (generated inputs are hashed by content), with a
# SkipCompilerExecution pass through CoreCompile in a fresh intermediate directory.
function Get-KbpCompilerInputs([string]$Configuration = 'Release', [string]$SourceRevision) {
    Assert-KbpLocalBuildConfiguration
    $root = Get-KbpRepositoryRoot
    if (-not $SourceRevision) { $SourceRevision = (& git -C $root rev-parse HEAD).Trim() }
    $id = [Guid]::NewGuid().ToString('N')
    $list = Join-Path ([IO.Path]::GetTempPath()) ("kbp-compiler-inputs-$id.txt")
    $obj = Join-Path ([IO.Path]::GetTempPath()) ("kbp-compiler-inputs-$id\")
    $msbuild = Get-KbpMsBuild
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = $null | & $msbuild (Join-Path $root 'KingmakerBuffPlanner.sln') /nologo /v:q /nr:false '/t:KingmakerBuffPlanner:PrepareResources;KingmakerBuffPlanner:Compile' /p:SkipCompilerExecution=true `
            "/p:Configuration=$Configuration" "/p:KbpSourceRevision=$SourceRevision" "/p:CompilerReferenceListPath=$list" "/p:IntermediateOutputPath=$obj" "/p:OutputPath=$($obj)bin" 2>&1 | ForEach-Object { "$_" }
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $list)) { throw "Could not resolve the compiler inputs: $($output -join ' ')" }
        return @(ConvertFrom-CompilerInputList $list)
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
function Get-KbpBuildInputFingerprint([string]$Configuration = 'Release', $CompilerInputs = $null) {
    $root = Get-KbpRepositoryRoot
    $paths = @('src', 'KingmakerBuffPlanner.sln', 'Version.props', 'LoaderReferences.targets', 'scripts/loader-reference-pins.json',
        'scripts/Get-PinnedLoaderReferences.ps1', 'scripts/Build.ps1', 'scripts/Build-Local.ps1', 'scripts/Common.ps1')
    $files = @(& git -C $root ls-files --cached --others --exclude-standard -- @paths)
    if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed while fingerprinting build inputs.' }
    $lines = @(foreach ($file in ($files | Sort-Object -Unique)) {
        $full = Join-Path $root $file
        if (Test-Path -LiteralPath $full -PathType Leaf) { $file + "`t" + (Get-KbpSha256 $full) }
    })
    if ($null -eq $CompilerInputs) { $CompilerInputs = @(Get-KbpCompilerInputs $Configuration) }
    foreach ($entry in @($CompilerInputs)) { $lines += 'compiler-' + [string]$entry.kind + ':' + [string]$entry.name + "`t" + [string]$entry.sha256 }
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
