[CmdletBinding()]
param(
    [string]$LegacyReleaseZip,
    [string]$WrongLoaderPackage,
    [string]$ReportPath
)
# Failure-boundary tests for the pinned UMM 0.31.1 / Harmony12 1.2.0.1 compile baseline. Runs on the committed HEAD in a
# temporary clone whose path contains a space (built and packaged there with Build-Local.ps1); every check runs in its own
# PowerShell or MSBuild process, so this checkout's artifacts and the installed game are never written. Fixtures are
# downloaded (or supplied), hash-checked and only read through private copies.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$source = Get-KbpRepositoryRoot
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Genuine published release 0.6.20, compiled against UnityModManager 0.33.0.0 (manifest 0.28.2).
$legacyUrl = 'https://github.com/howardreith/KingmakerBuffPlanner/releases/download/v0.6.20/KingmakerBuffPlanner-0.6.20.zip'
$legacySha256 = '54153abb96c9aa5a3e0cb87dc18401dc23aeb47e603b9a69148b5a944668f780'
$wrongLoaderUrl = 'https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.32.4/unitymodmanager.0.32.4.nupkg'
$wrongLoaderPackageSha256 = 'a8438e921ca7b0e5805f5a31c196ae65c90f236543885bea0146457d082c9ec9'
$wrongLoaderDllSha256 = '1387468bc3af41c50fe51859a3bb7af4922891aa8f13a6187e7a348ceaabfd88'
$modFolder = 'KingmakerBuffPlanner'
$pinFiles = @('UnityModManager.dll', '0Harmony12.dll')

function Quote([string]$Value) { return "'" + $Value.Replace("'", "''") + "'" }
$commit = (& git -C $source rev-parse HEAD).Trim()
if ([bool](& git -C $source status --porcelain)) { Write-Host "NOTE: uncommitted changes are not tested; the suite tests committed HEAD $commit." }
$artifactsBefore = @(Get-ChildItem -LiteralPath (Join-Path $source 'artifacts') -File -Recurse -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName + '=' + $_.Length + '@' + $_.LastWriteTimeUtc.Ticks }) -join ';'
$msbuild = Get-KbpMsBuild
[xml]$gamePaths = Get-Content -Raw -LiteralPath (Join-Path $source 'GamePath.props')
$installDir = [string](@($gamePaths.Project.PropertyGroup) | Where-Object { $_.KingmakerInstallDir } | Select-Object -First 1).KingmakerInstallDir
$managedDir = Join-Path $installDir 'Kingmaker_Data\Managed'
$installedLoader = Join-Path $managedDir 'UnityModManager\UnityModManager.dll'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('kbp-loader-baseline-' + [Guid]::NewGuid().ToString('N'))
$base = Join-Path $temp 'clone with space'
$inputs = Join-Path $temp 'inputs'
New-Item -ItemType Directory -Path $inputs -Force | Out-Null
$cases = [Collections.Generic.List[object]]::new()

function ConvertTo-PlainText($Records) { return (@($Records | ForEach-Object { "$_" }) -join "`n") }
function Invoke-Isolated([string[]]$Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $records = $null | & powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass @Arguments 2>&1 }
    finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = (ConvertTo-PlainText $records) }
}
function Invoke-Script([string]$Text) {
    $wrapped = 'try { ' + $Text + ' } catch { [Console]::Out.WriteLine(''ERROR: '' + $_.Exception.Message); exit 1 }'
    return Invoke-Isolated @('-EncodedCommand', [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($wrapped)))
}
function Invoke-MSBuildIsolated([string[]]$Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = ConvertTo-PlainText ($null | & $msbuild /nologo /v:m /nr:false @Arguments 2>&1) }
    finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}
function Add-Case([string]$Name, [bool]$Passed, [string]$Detail) {
    $cases.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
    if ($Passed) { Write-Host "PASS $Name :: $Detail" } else { Write-Host "FAIL $Name :: $Detail" }
}
function Get-Reason([string]$Output) {
    $line = @($Output -split "`n" | Where-Object { $_ -match '^ERROR: |: error |must|mismatch|missing|does not|allowlist|exactly|refused|binary' -and $_ -notmatch '^PASS ' } | Select-Object -First 1)
    if ($line) { return $line[0].Trim() } else { return ($Output.Trim() -split "`n" | Select-Object -Last 1) }
}
function Expect-Rejected([string]$Name, $Result, [string]$Pattern) {
    $ok = $Result.ExitCode -ne 0 -and ($Result.Output -replace '\s+', ' ') -match $Pattern
    $detail = if ($ok) { 'rejected: ' + (Get-Reason $Result.Output) } else { "exit $($Result.ExitCode); expected /$Pattern/; output: $($Result.Output.Trim())" }
    Add-Case $Name $ok $detail
}
function Expect-Accepted([string]$Name, $Result, [string]$Pattern) {
    $ok = $Result.ExitCode -eq 0 -and (-not $Pattern -or ($Result.Output -replace '\s+', ' ') -match $Pattern)
    $detail = if ($ok -and $Pattern) { 'accepted: ' + [regex]::Match(($Result.Output -replace '\s+', ' '), $Pattern).Value } elseif ($ok) { 'accepted' } else { "exit $($Result.ExitCode); output: $($Result.Output.Trim())" }
    Add-Case $Name $ok $detail
}
function Copy-Pins([string]$From, [string]$Destination) {
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($file in $pinFiles) { Copy-Item -LiteralPath (Join-Path $From $file) -Destination $Destination }
}
function Edit-Json([string]$Path, [scriptblock]$Edit) {
    $j = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    & $Edit $j
    $j | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Path -Encoding UTF8
}
# Rewrites a ZIP entry by entry, keeping the original entry timestamps (the validator requires deterministic times).
function Copy-ZipWith([string]$From, [string]$Destination, [scriptblock]$Transform) {
    $in = [IO.Compression.ZipFile]::OpenRead($From)
    $out = [IO.Compression.ZipFile]::Open($Destination, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $in.Entries) {
            $stream = $entry.Open(); $buffer = [IO.MemoryStream]::new()
            try { $stream.CopyTo($buffer) } finally { $stream.Dispose() }
            $result = & $Transform $entry.FullName ($buffer.ToArray())
            foreach ($item in @($result)) {
                if ($null -eq $item) { continue }
                $new = $out.CreateEntry($item.Name)
                $new.LastWriteTime = $entry.LastWriteTime
                $writer = $new.Open()
                try { $writer.Write($item.Bytes, 0, $item.Bytes.Length) } finally { $writer.Dispose() }
            }
        }
    }
    finally { $out.Dispose(); $in.Dispose() }
}
function Get-CompilerReferences([string]$Output, [string]$File) {
    $line = @($Output -split "`n" | Where-Object { $_ -match 'csc\.exe' }) -join "`n"
    return @([regex]::Matches($line, '/reference:(?:"([^"]+)"|(\S+))') | ForEach-Object {
        if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value } } | Where-Object { [IO.Path]::GetFileName($_) -ieq $File })
}

try {
    $legacyZip = Join-Path $inputs 'legacy-0.6.20.zip'
    if ($LegacyReleaseZip) { Copy-Item -LiteralPath $LegacyReleaseZip -Destination $legacyZip } else { Invoke-WebRequest -Uri $legacyUrl -OutFile $legacyZip -UseBasicParsing }
    if ((Get-KbpSha256 $legacyZip) -cne $legacySha256) { throw 'Legacy release ZIP hash mismatch.' }
    $wrongPackage = Join-Path $inputs 'umm-0.32.4.nupkg'
    if ($WrongLoaderPackage) { Copy-Item -LiteralPath $WrongLoaderPackage -Destination $wrongPackage } else { Invoke-WebRequest -Uri $wrongLoaderUrl -OutFile $wrongPackage -UseBasicParsing }
    if ((Get-KbpSha256 $wrongPackage) -cne $wrongLoaderPackageSha256) { throw 'UnityModManager 0.32.4 package hash mismatch.' }
    $wrongUmm = Join-Path $inputs 'UnityModManager-0.32.4.dll'
    $archive = [IO.Compression.ZipFile]::OpenRead($wrongPackage)
    try { [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry('lib/net35/UnityModManager.dll'), $wrongUmm) } finally { $archive.Dispose() }
    if ((Get-KbpSha256 $wrongUmm) -cne $wrongLoaderDllSha256) { throw 'UnityModManager 0.32.4 DLL hash mismatch.' }
    $owner = [ordered]@{ version = [Reflection.AssemblyName]::GetAssemblyName($installedLoader).Version.ToString(); sha256 = Get-KbpSha256 $installedLoader }
    Write-Host "Owner-installed UnityModManager (measured only, not used as a fixture): $($owner.version) $($owner.sha256)"

    & git clone -q --no-hardlinks $source $base
    if ($LASTEXITCODE -ne 0) { throw 'git clone failed.' }
    & git -C $base checkout -q --detach $commit
    Copy-Item -LiteralPath (Join-Path $source 'GamePath.props') -Destination $base
    $references = Join-Path $base 'artifacts\references'
    Copy-Pins (Join-Path $source 'artifacts\references') $references
    $build = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    $packageMatch = [regex]::Match($build.Output, 'Local package: PASS=1 FAIL=0 path=(.+\.zip)')
    if ($build.ExitCode -ne 0 -or -not $packageMatch.Success) { throw "Clone Build-Local failed: $($build.Output)" }
    $candidateZip = $packageMatch.Groups[1].Value.Trim()
    $manifest = Get-Content -LiteralPath ($candidateZip + '.build-local.json') -Raw | ConvertFrom-Json
    Add-Case 'clone-build-local-from-detached-commit' ($build.Output -match 'Package validation: PASS=11 FAIL=0') ("detached clone of $commit; package $(Get-KbpSha256 $candidateZip); DLL $($manifest.dllSha256)")
    $verifier = Join-Path $base 'scripts\Get-PinnedLoaderReferences.ps1'
    $validator = Join-Path $base 'scripts\validate-package.ps1'
    $project = Join-Path $base 'src\KingmakerBuffPlanner\KingmakerBuffPlanner.csproj'
    $pinned = @{}
    foreach ($file in $pinFiles) { $pinned[$file] = [IO.Path]::GetFullPath((Join-Path $references $file)) }
    $testZips = Join-Path $base 'artifacts\loader-baseline-tests'
    New-Item -ItemType Directory -Path $testZips -Force | Out-Null

    # 1. Reference cache.
    Expect-Accepted 'cache-genuine-verified' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $references)) ''
    $corrupt = Join-Path $temp 'cache corrupt'
    Copy-Pins $references $corrupt
    $bytes = [IO.File]::ReadAllBytes((Join-Path $corrupt 'UnityModManager.dll'))
    $bytes[$bytes.Length - 1] = $bytes[$bytes.Length - 1] -bxor 0xFF
    [IO.File]::WriteAllBytes((Join-Path $corrupt 'UnityModManager.dll'), $bytes)
    Expect-Rejected 'cache-corrupt-rejected' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $corrupt)) 'hash mismatch'
    $missing = Join-Path $temp 'cache missing'
    Copy-Pins $references $missing
    Remove-Item -LiteralPath (Join-Path $missing '0Harmony12.dll')
    Expect-Rejected 'cache-missing-rejected' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $missing)) 'reference missing'
    $wrongCache = Join-Path $temp 'cache wrong loader'
    Copy-Pins $references $wrongCache
    Copy-Item -LiteralPath $wrongUmm -Destination (Join-Path $wrongCache 'UnityModManager.dll') -Force
    Expect-Rejected 'cache-umm-0.32.4-fixture-rejected' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $wrongCache)) 'hash mismatch'

    # 2. Pin definition and manifest contract on copies of the clone's scripts.
    function New-ManifestFixture([string]$Name, [scriptblock]$Edit) {
        $fixture = Join-Path $temp $Name
        New-Item -ItemType Directory -Force -Path (Join-Path $fixture 'scripts'), (Join-Path $fixture 'src\KingmakerBuffPlanner') | Out-Null
        Copy-Item -LiteralPath $verifier, (Join-Path $base 'scripts\loader-reference-pins.json') -Destination (Join-Path $fixture 'scripts')
        Copy-Item -LiteralPath (Join-Path $base 'src\KingmakerBuffPlanner\Info.json') -Destination (Join-Path $fixture 'src\KingmakerBuffPlanner')
        & $Edit $fixture
        return (Join-Path $fixture 'scripts\Get-PinnedLoaderReferences.ps1')
    }
    $f = New-ManifestFixture 'manifest control' { param($d) }
    Expect-Accepted 'manifest-control-accepted' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) ''
    $f = New-ManifestFixture 'pin harmony dropped' { param($d) Edit-Json (Join-Path $d 'scripts\loader-reference-pins.json') { param($j) $j.references = @($j.references | Where-Object Name -ne '0Harmony12') } }
    Expect-Rejected 'pin-definition-without-harmony12-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'must define exactly'
    $f = New-ManifestFixture 'pin harmony renamed' { param($d) Edit-Json (Join-Path $d 'scripts\loader-reference-pins.json') { param($j) @($j.references | Where-Object Name -eq '0Harmony12')[0].File = 'Harmony12.dll' } }
    Expect-Rejected 'pin-definition-misnamed-harmony12-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'must define 0Harmony12 exactly once'
    $f = New-ManifestFixture 'manifest raised' { param($d) Edit-Json (Join-Path $d 'src\KingmakerBuffPlanner\Info.json') { param($j) $j.ManagerVersion = '0.33.0' } }
    Expect-Rejected 'manifest-above-compiler-reference-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'ManagerVersion must equal'
    $f = New-ManifestFixture 'manifest array' { param($d) Edit-Json (Join-Path $d 'src\KingmakerBuffPlanner\Info.json') { param($j) $j.ManagerVersion = @('0.31.1') } }
    Expect-Rejected 'manifest-array-version-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'ManagerVersion must equal'
    $f = New-ManifestFixture 'pin raised' { param($d) Edit-Json (Join-Path $d 'scripts\loader-reference-pins.json') { param($j) @($j.references | Where-Object Name -eq 'UnityModManager')[0].Version = '0.33.0.0' } }
    Expect-Rejected 'compiler-pin-above-minimum-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'must equal the declared minimum'

    # 3. Direct and incremental MSBuild of the production project.
    $obj = Join-Path $temp 'direct-obj\'
    $out = Join-Path $temp 'direct-bin\'
    Expect-Rejected 'msbuild-corrupt-cache-rejected' (Invoke-MSBuildIsolated @($project, '/t:Build', '/p:Configuration=Release', "/p:PinnedLoaderReferenceDir=$corrupt", "/p:IntermediateOutputPath=$obj", "/p:OutputPath=$out")) 'hash mismatch'
    $direct = Invoke-MSBuildIsolated @($project, '/t:Rebuild', '/v:n', '/p:Configuration=Release', "/p:IntermediateOutputPath=$obj", "/p:OutputPath=$out")
    $ok = $direct.ExitCode -eq 0
    $detail = @()
    foreach ($file in $pinFiles) {
        $found = @(Get-CompilerReferences $direct.Output $file)
        $ok = $ok -and $found.Count -eq 1 -and $found[0] -ieq $pinned[$file]
        $detail += "$file -> $($found -join ' | ')"
    }
    Add-Case 'msbuild-direct-compiles-against-pinned-inputs' $ok ("exit $($direct.ExitCode); " + ($detail -join '; '))
    $inc = @($project, '/t:Build', '/p:Configuration=Release', ('/p:IntermediateOutputPath=' + (Join-Path $temp 'incremental-obj\')), ('/p:OutputPath=' + (Join-Path $temp 'incremental-bin\')))
    Expect-Accepted 'incremental-build-genuine-accepted' (Invoke-MSBuildIsolated $inc) ''
    Expect-Accepted 'incremental-build-up-to-date-accepted' (Invoke-MSBuildIsolated $inc) ''
    Edit-Json (Join-Path $base 'src\KingmakerBuffPlanner\Info.json') { param($j) $j.ManagerVersion = '0.33.0' }
    Expect-Rejected 'incremental-build-after-manifest-only-change-rejected' (Invoke-MSBuildIsolated $inc) 'ManagerVersion must equal'
    & git -C $base checkout -q -- src/KingmakerBuffPlanner/Info.json
    Edit-Json (Join-Path $base 'scripts\loader-reference-pins.json') { param($j) @($j.references | Where-Object Name -eq '0Harmony12')[0].Sha256 = ('0' * 64) }
    Expect-Rejected 'incremental-build-after-pin-only-change-rejected' (Invoke-MSBuildIsolated $inc) 'hash mismatch'
    & git -C $base checkout -q -- scripts/loader-reference-pins.json
    Expect-Accepted 'incremental-build-after-restore-accepted' (Invoke-MSBuildIsolated $inc) ''

    # 4. Csc-input guard (after reference-assembly selection).
    $stray = Join-Path $temp 'stray copy'
    Copy-Pins $references $stray
    Copy-Item -LiteralPath $pinned['UnityModManager.dll'] -Destination (Join-Path $references 'UnityModManager.exe')
    Copy-Item -LiteralPath $pinned['UnityModManager.dll'] -Destination (Join-Path $references 'umm-alias.dll')
    $targets = Join-Path $base 'LoaderReferences.targets'
    function New-ProbeProject([string]$Name, [string]$Umm, [string]$Extra) {
        $path = Join-Path $temp "probe $Name.csproj"
        $text = '<Project ToolsVersion="15.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">' +
            '<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists(''$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props'')" />' +
            '<PropertyGroup><TargetFrameworkVersion>v4.7</TargetFrameworkVersion><OutputType>Library</OutputType>' +
            '<OutputPath>' + $temp + '\probe bin\' + $Name + '\</OutputPath><IntermediateOutputPath>' + $temp + '\probe obj\' + $Name + '\</IntermediateOutputPath>' +
            '<ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch>true</ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch></PropertyGroup>' +
            '<Import Project="' + $targets + '" /><ItemGroup>' +
            '<Reference Include="UnityModManager"><HintPath>' + $Umm + '</HintPath><Private>False</Private></Reference>' +
            '<Reference Include="0Harmony12"><HintPath>' + $pinned['0Harmony12.dll'] + '</HintPath><Private>False</Private></Reference></ItemGroup>' +
            $Extra + '<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" /></Project>'
        [IO.File]::WriteAllText($path, $text)
        return $path
    }
    $t = '/t:ResolveReferences;FindReferenceAssembliesForReferences'
    Expect-Accepted 'csc-input-pinned-accepted' (Invoke-MSBuildIsolated @((New-ProbeProject 'pinned' $pinned['UnityModManager.dll'] ''), $t)) ''
    Expect-Rejected 'csc-input-identical-copy-elsewhere-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'stray' (Join-Path $stray 'UnityModManager.dll') ''), $t)) 'must be the verified files'
    Expect-Rejected 'csc-input-same-stem-file-in-pinned-dir-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'samestem' (Join-Path $references 'UnityModManager.exe') ''), $t)) 'must be the verified files'
    Expect-Rejected 'csc-input-renamed-loader-in-pinned-dir-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'alias' (Join-Path $references 'umm-alias.dll') ''), $t)) 'must be the verified files'
    Expect-Rejected 'csc-input-umm-0.32.4-fixture-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'wrong' $wrongUmm ''), $t)) 'must be the verified files'
    $substitute = '<Target Name="SubstituteLoaderReferenceAssembly" AfterTargets="ResolveAssemblyReferences" BeforeTargets="FindReferenceAssembliesForReferences"><ItemGroup>' +
        '<ReferencePath Condition="''%(Filename)'' == ''UnityModManager''"><ReferenceAssembly>' + (Join-Path $stray 'UnityModManager.dll') + '</ReferenceAssembly></ReferencePath></ItemGroup></Target>'
    Expect-Rejected 'csc-input-substituted-reference-assembly-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'substituted' $pinned['UnityModManager.dll'] $substitute), $t)) 'must be the verified files'
    Remove-Item -LiteralPath (Join-Path $references 'UnityModManager.exe'), (Join-Path $references 'umm-alias.dll')

    # 5. Package validator on real archives (inside the clone's artifacts, as the validator requires).
    $v = { param($zip) Invoke-Isolated @('-File', $validator, '-PackagePath', $zip) }
    Expect-Accepted 'package-candidate-accepted' (& $v $candidateZip) 'Package validation: PASS=11 FAIL=0'
    $dllBytes = $null
    $p = Join-Path $testZips 'extra-harmony.zip'
    Copy-ZipWith $candidateZip $p { param($name, $bytes) [pscustomobject]@{ Name = $name; Bytes = $bytes }; if ($name -ceq "$modFolder/Info.json") { [pscustomobject]@{ Name = "$modFolder/0Harmony12.dll"; Bytes = [IO.File]::ReadAllBytes($pinned['0Harmony12.dll']) } } }
    Expect-Rejected 'package-extra-harmony-rejected' (& $v $p) 'allowlist mismatch'
    $p = Join-Path $testZips 'swap-loader.zip'
    Copy-ZipWith $candidateZip $p { param($name, $bytes) if ($name -ceq "$modFolder/THIRD-PARTY-NOTICES.md") { [pscustomobject]@{ Name = "$modFolder/UnityModManager.dll"; Bytes = [IO.File]::ReadAllBytes($pinned['UnityModManager.dll']) } } else { [pscustomobject]@{ Name = $name; Bytes = $bytes } } }
    Expect-Rejected 'package-swapped-in-loader-rejected' (& $v $p) 'allowlist mismatch'
    $p = Join-Path $testZips 'harmony-as-notices.zip'
    Copy-ZipWith $candidateZip $p { param($name, $bytes) if ($name -ceq "$modFolder/THIRD-PARTY-NOTICES.md") { [pscustomobject]@{ Name = $name; Bytes = [IO.File]::ReadAllBytes($pinned['0Harmony12.dll']) } } else { [pscustomobject]@{ Name = $name; Bytes = $bytes } } }
    Expect-Rejected 'package-harmony-bytes-under-notices-name-rejected' (& $v $p) 'binary content: THIRD-PARTY-NOTICES\.md'
    $p = Join-Path $testZips 'game-as-overrides.zip'
    Copy-ZipWith $candidateZip $p { param($name, $bytes) if ($name -ceq "$modFolder/NativeEffectOverrides.json") { [pscustomobject]@{ Name = $name; Bytes = [IO.File]::ReadAllBytes((Join-Path $managedDir 'Assembly-CSharp-firstpass.dll')) } } else { [pscustomobject]@{ Name = $name; Bytes = $bytes } } }
    Expect-Rejected 'package-game-bytes-under-data-name-rejected' (& $v $p) 'binary content: NativeEffectOverrides\.json'
    foreach ($bad in @(@{ Name = 'array'; Value = '["0.31.1"]' }, @{ Name = 'boolean'; Value = 'true' })) {
        $p = Join-Path $testZips "manager-$($bad.Name).zip"
        $replacement = '"ManagerVersion": ' + $bad.Value
        Copy-ZipWith $candidateZip $p { param($name, $bytes) if ($name -ceq "$modFolder/Info.json") { $text = [Text.Encoding]::UTF8.GetString($bytes) -replace '"ManagerVersion":\s*"0\.31\.1"', $replacement; [pscustomobject]@{ Name = $name; Bytes = [Text.UTF8Encoding]::new($false).GetBytes($text) } } else { [pscustomobject]@{ Name = $name; Bytes = $bytes } } }
        Expect-Rejected "package-manager-version-$($bad.Name)-rejected" (& $v $p) 'ManagerVersion must be the JSON string 0\.31\.1'
    }
    $legacyRefs = Invoke-Script ('$a = [IO.Compression.ZipFile]::OpenRead(' + (Quote $legacyZip) + '); try { $s = ($a.Entries | Where-Object FullName -eq ''KingmakerBuffPlanner/KingmakerBuffPlanner.dll'').Open(); $m = [IO.MemoryStream]::new(); $s.CopyTo($m); $s.Dispose(); ([Reflection.Assembly]::Load($m.ToArray()).GetReferencedAssemblies() | Where-Object Name -eq ''UnityModManager'').Version.ToString() } finally { $a.Dispose() }')
    $legacyRefsPreamble = $legacyRefs
    Add-Case 'legacy-release-references-umm-0.33.0' ($legacyRefs.Output -match '0\.33\.0\.0') ("genuine 0.6.20 ZIP $legacySha256; DLL references UnityModManager " + ($legacyRefs.Output.Trim() -split "`n" | Select-Object -Last 1))
    $lowered = Join-Path $testZips 'KingmakerBuffPlanner-0.6.20-lowered.zip'
    Copy-ZipWith $legacyZip $lowered { param($name, $bytes) if ($name -ceq "$modFolder/Info.json") { $text = [Text.Encoding]::UTF8.GetString($bytes) -replace '"ManagerVersion":\s*"[0-9.]+"', '"ManagerVersion": "0.31.1"'; [pscustomobject]@{ Name = $name; Bytes = [Text.UTF8Encoding]::new($false).GetBytes($text) } } else { [pscustomobject]@{ Name = $name; Bytes = $bytes } } }
    Expect-Rejected 'package-legacy-0.33.0-dll-with-lowered-manifest-rejected' (& $v $lowered) 'must reference pinned UnityModManager 0\.31\.1\.0'
    foreach ($order in @(@($candidateZip, $lowered), @($lowered, $candidateZip))) {
        $text = 'foreach ($zip in @(' + (Quote $order[0]) + ', ' + (Quote $order[1]) + ')) { try { & ' + (Quote $validator) + ' -PackagePath $zip | Out-Null; ''ACCEPTED '' + [IO.Path]::GetFileName($zip) } catch { ''REJECTED '' + [IO.Path]::GetFileName($zip) + '': '' + $_.Exception.Message } }'
        $r = Invoke-Script $text
        $ok = $r.Output -match ('ACCEPTED ' + [regex]::Escape([IO.Path]::GetFileName($candidateZip))) -and $r.Output -match 'REJECTED KingmakerBuffPlanner-0\.6\.20-lowered\.zip: Packaged DLL must reference pinned UnityModManager 0\.31\.1\.0'
        Add-Case "same-host-validation-$([IO.Path]::GetFileNameWithoutExtension($order[0]))-first" $ok ($r.Output.Trim() -replace "`n", ' | ')
    }

    $artifactsAfter = @(Get-ChildItem -LiteralPath (Join-Path $source 'artifacts') -File -Recurse -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName + '=' + $_.Length + '@' + $_.LastWriteTimeUtc.Ticks }) -join ';'
    Add-Case 'repository-artifacts-untouched' ($artifactsBefore -ceq $artifactsAfter) 'this checkout''s artifacts are unchanged'
    Add-Case 'legacy-input-preserved' ((Get-KbpSha256 $legacyZip) -ceq $legacySha256) "private copy still $legacySha256"

    $failed = @($cases | Where-Object { -not $_.passed })
    $summary = [ordered]@{
        schema = 'kbp.loader-baseline-tests.v1'
        source_commit = $commit
        candidate_package_sha256 = Get-KbpSha256 $candidateZip
        candidate_dll_sha256 = $manifest.dllSha256
        legacy_release_zip_sha256 = $legacySha256
        wrong_loader_fixture = "UnityModManager 0.32.4.0 $wrongLoaderDllSha256"
        owner_install_measured = $owner
        passed = $cases.Count - $failed.Count
        total = $cases.Count
        cases = @($cases)
    }
    if ($ReportPath) { $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
    Write-Host ("RESULT {0}/{1} loader-baseline checks passed" -f $summary.passed, $summary.total)
    if ($failed.Count -gt 0) { throw "$($failed.Count) loader-baseline check(s) failed." }
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
