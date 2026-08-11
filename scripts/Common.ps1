Set-StrictMode -Version Latest

function Get-KbpRepositoryRoot {
    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
}

function Get-KbpVersion {
    $root = Get-KbpRepositoryRoot
    [xml]$props = Get-Content -LiteralPath (Join-Path $root 'Version.props') -Raw
    return [string]$props.Project.PropertyGroup.KbpVersion
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
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
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
