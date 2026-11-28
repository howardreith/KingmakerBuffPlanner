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
    Add-Case 'clone-build-local-from-detached-commit' ($build.Output -match 'Package validation: PASS=13 FAIL=0') ("detached clone of $commit; package $(Get-KbpSha256 $candidateZip); DLL $($manifest.dllSha256)")
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
    Expect-Accepted 'package-candidate-accepted' (& $v $candidateZip) 'Package validation: PASS=13 FAIL=0'
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
    $legacyRefs = Invoke-Script ('Add-Type -AssemblyName System.IO.Compression.FileSystem; $a = [IO.Compression.ZipFile]::OpenRead(' + (Quote $legacyZip) + '); try { $s = ($a.Entries | Where-Object FullName -eq ''KingmakerBuffPlanner/KingmakerBuffPlanner.dll'').Open(); $m = [IO.MemoryStream]::new(); $s.CopyTo($m); $s.Dispose(); ([Reflection.Assembly]::Load($m.ToArray()).GetReferencedAssemblies() | Where-Object Name -eq ''UnityModManager'').Version.ToString() } finally { $a.Dispose() }')
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

    # 6. Standalone validator: the same pin-definition contract as the build, offline (no cache needed).
    $cacheAway = Join-Path $temp 'cache moved away'
    Move-Item -LiteralPath $references -Destination $cacheAway
    Expect-Accepted 'validator-offline-without-reference-cache-accepted' (& $v $candidateZip) 'Package validation: PASS=13 FAIL=0'
    Move-Item -LiteralPath $cacheAway -Destination $references
    $pinsFile = Join-Path $base 'scripts\loader-reference-pins.json'
    Edit-Json $pinsFile { param($j) $j.references = @($j.references | Where-Object Name -ne '0Harmony12') }
    Expect-Rejected 'validator-pin-definition-missing-harmony12-rejected' (& $v $candidateZip) 'must define exactly'
    & git -C $base checkout -q -- scripts/loader-reference-pins.json
    Edit-Json $pinsFile { param($j) @($j.references | Where-Object Name -eq 'UnityModManager')[0].Version = '0.32.4.0' }
    Expect-Rejected 'validator-umm-pin-inconsistent-with-minimum-rejected' (& $v $candidateZip) 'must equal the declared minimum'
    & git -C $base checkout -q -- scripts/loader-reference-pins.json
    Expect-Accepted 'validator-genuine-control-after-restore-accepted' (& $v $candidateZip) 'Package validation: PASS=13 FAIL=0'

    # 7. A relative cache path follows PowerShell's current location, not the process directory, including a cache
    #    that does not exist yet.
    $processDir = Join-Path $temp 'process dir'; $psDir = Join-Path $temp 'powershell dir'
    Copy-Pins $references (Join-Path $processDir 'cache')
    Copy-Pins $corrupt (Join-Path $psDir 'cache')
    $relative = { param($processLocation, $psLocation, $extra) Invoke-Script ("[Environment]::CurrentDirectory = $(Quote $processLocation); Set-Location -LiteralPath $(Quote $psLocation); & $(Quote $verifier) -VerifyOnly -ReferenceDirectory 'cache'" + $extra) }
    Expect-Rejected 'relative-cache-uses-powershell-location-corrupt-rejected' (& $relative $processDir $psDir '') ('hash mismatch: ' + [regex]::Escape((Join-Path $psDir 'cache')))
    Expect-Accepted 'relative-cache-uses-powershell-location-genuine-accepted' (& $relative $psDir $processDir '') ''
    $emptyDir = Join-Path $temp 'empty location'; New-Item -ItemType Directory -Path $emptyDir | Out-Null
    Expect-Rejected 'relative-cache-not-yet-created-resolves-under-powershell-location' (& $relative $processDir $emptyDir '') ('reference missing: ' + [regex]::Escape((Join-Path $emptyDir 'cache')))

    # 8. Frozen receipt reuse (Read-KbpBuildManifest, the runtime harness's preflight before staging) on the clone's
    #    clean Build-Local package: identities from one validated snapshot, clean producing source, current inputs.
    $runtimeCommon = Join-Path $base 'scripts\RuntimeAutomation.Common.ps1'
    $zipA = $candidateZip; $receiptA = $candidateZip + '.build-local.json'
    $saved = Join-Path $inputs 'receipts'; New-Item -ItemType Directory -Path $saved -Force | Out-Null
    Copy-Item -LiteralPath $zipA -Destination (Join-Path $saved 'a.zip'); Copy-Item -LiteralPath $receiptA -Destination (Join-Path $saved 'a.json')
    function Restore-PairA {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $zipA) | Out-Null
        Copy-Item -LiteralPath (Join-Path $saved 'a.zip') -Destination $zipA -Force
        Copy-Item -LiteralPath (Join-Path $saved 'a.json') -Destination $receiptA -Force
    }
    function Test-FrozenReceipt { Invoke-Script (". $(Quote $runtimeCommon); [void](Read-KbpBuildManifest $(Quote $zipA)); 'RECEIPT ACCEPTED'") }
    $ma = Get-Content -LiteralPath $receiptA -Raw | ConvertFrom-Json
    Add-Case 'receipt-records-clean-producing-build' ($ma.sourceDirty -eq $false -and $ma.commit -eq $commit -and $ma.embeddedCommit -eq $commit -and [string]$ma.buildInputsSha256 -match '^[0-9a-f]{64}$' -and @($ma.compilerInputs).Count -gt 0) ("commit $($ma.commit) dirty $($ma.sourceDirty) embedded $($ma.embeddedCommit) DLL $($ma.dllSha256) MVID $($ma.assemblyMvid) inputs $($ma.buildInputsSha256) ($(@($ma.compilerInputs).Count) compiler inputs)")
    Expect-Accepted 'receipt-matching-archive-and-receipt-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    foreach ($bad in @(
        @{ Name = 'wrong-embedded-revision'; Edit = { param($j) $j.embeddedCommit = ('e' * 40) }; Pattern = 'embedded source revision' },
        @{ Name = 'wrong-dll-digest'; Edit = { param($j) $j.dllSha256 = ('d' * 64) }; Pattern = 'DLL SHA-256' },
        @{ Name = 'wrong-mvid'; Edit = { param($j) $j.assemblyMvid = '00000000-0000-0000-0000-000000000000' }; Pattern = 'MVID' },
        @{ Name = 'dirty-flag-as-string'; Edit = { param($j) $j.sourceDirty = 'false' }; Pattern = 'boolean sourceDirty' },
        @{ Name = 'inputs-digest-array'; Edit = { param($j) $j.buildInputsSha256 = @($j.buildInputsSha256) }; Pattern = 'no string buildInputsSha256' })) {
        Restore-PairA
        Edit-Json $receiptA $bad.Edit
        Expect-Rejected "receipt-$($bad.Name)-rejected" (Test-FrozenReceipt) $bad.Pattern
    }
    # A second real build of the same version from changed source (a dirty development build).
    $marker = Join-Path $base 'src\KingmakerBuffPlanner\Main.cs'
    [IO.File]::AppendAllText($marker, "`r`nnamespace KingmakerBuffPlanner { internal static class LoaderBaselineReceiptMarker { } }`r`n")
    $buildB = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    Copy-Item -LiteralPath $zipA -Destination (Join-Path $saved 'b.zip'); Copy-Item -LiteralPath $receiptA -Destination (Join-Path $saved 'b.json')
    & git -C $base checkout -q -- src/KingmakerBuffPlanner/Main.cs
    $mb = Get-Content -LiteralPath (Join-Path $saved 'b.json') -Raw | ConvertFrom-Json
    Add-Case 'second-same-version-build-is-distinct' ($buildB.ExitCode -eq 0 -and $mb.version -eq $ma.version -and $mb.dllSha256 -ne $ma.dllSha256 -and $mb.packageSha256 -ne $ma.packageSha256 -and $mb.sourceDirty -eq $true) ("version $($mb.version); DLL $($mb.dllSha256) vs $($ma.dllSha256); dirty $($mb.sourceDirty)")
    Expect-Rejected 'receipt-dirty-development-build-not-reused' (Test-FrozenReceipt) 'dirty development build'
    Restore-PairA; Copy-Item -LiteralPath (Join-Path $saved 'b.json') -Destination $receiptA -Force
    Expect-Rejected 'receipt-swapped-receipt-for-other-build-rejected' (Test-FrozenReceipt) 'does not match the exact package'
    Restore-PairA; Copy-Item -LiteralPath (Join-Path $saved 'b.zip') -Destination $zipA -Force
    Expect-Rejected 'receipt-swapped-archive-from-other-build-rejected' (Test-FrozenReceipt) 'does not match the exact package'
    Restore-PairA
    Expect-Accepted 'receipt-pair-a-restored-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    # Material input changes at the same HEAD invalidate the receipt.
    foreach ($mutation in @(
        @{ Name = 'reference-helper'; File = 'scripts\Get-PinnedLoaderReferences.ps1'; Text = "`r`n# loader-baseline test edit`r`n" },
        @{ Name = 'project-file'; File = 'src\KingmakerBuffPlanner\KingmakerBuffPlanner.csproj'; Text = "`r`n<!-- loader-baseline test edit -->`r`n" },
        @{ Name = 'build-wrapper'; File = 'scripts\Build.ps1'; Text = "`r`n# loader-baseline test edit`r`n" })) {
        [IO.File]::AppendAllText((Join-Path $base $mutation.File), $mutation.Text)
        Expect-Rejected "receipt-after-$($mutation.Name)-change-rejected" (Test-FrozenReceipt) 'Build inputs changed'
        & git -C $base checkout -q -- ($mutation.File.Replace('\', '/'))
    }
    # Ignored local references selected by MSBuild: identical bytes relocated are accepted; one changed byte is not.
    $gamePath = Join-Path $base 'GamePath.props'
    $gamePathOriginal = [IO.File]::ReadAllText($gamePath)
    $fakeInstall = Join-Path $temp 'relocated game'
    $fakeManaged = Join-Path $fakeInstall 'Kingmaker_Data\Managed'
    New-Item -ItemType Directory -Path $fakeManaged -Force | Out-Null
    [xml]$projectXml = Get-Content -LiteralPath $project -Raw
    foreach ($hint in @($projectXml.SelectNodes("//*[local-name()='Reference']/*[local-name()='HintPath']") | ForEach-Object { $_.InnerText } | Where-Object { $_.StartsWith('$(KingmakerInstallDir)') })) {
        $relativeHint = $hint.Substring('$(KingmakerInstallDir)'.Length).TrimStart('\')
        Copy-Item -LiteralPath (Join-Path $installDir $relativeHint) -Destination (Join-Path $fakeInstall $relativeHint)
    }
    [IO.File]::WriteAllText($gamePath, "<Project xmlns=`"http://schemas.microsoft.com/developer/msbuild/2003`"><PropertyGroup><KingmakerInstallDir>$([Security.SecurityElement]::Escape($fakeInstall))</KingmakerInstallDir></PropertyGroup></Project>")
    Expect-Accepted 'receipt-identical-local-references-relocated-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    $coreModule = Join-Path $fakeManaged 'UnityEngine.CoreModule.dll'
    $coreBytes = [IO.File]::ReadAllBytes($coreModule); $coreBytes[$coreBytes.Length - 1] = $coreBytes[$coreBytes.Length - 1] -bxor 0x01
    [IO.File]::WriteAllBytes($coreModule, $coreBytes)
    Expect-Rejected 'receipt-after-resolved-local-reference-change-rejected' (Test-FrozenReceipt) 'Build inputs changed'
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal)
    Expect-Accepted 'receipt-after-inputs-restored-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    # The bounded local configuration: any other setting in GamePath.props (here one that only a production build would
    # see) refuses reuse and building.
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal.Replace('</PropertyGroup>', "<DefineConstants Condition=`"'`$(SkipCompilerExecution)' != 'true'`">`$(DefineConstants);PRODUCTION_ONLY</DefineConstants></PropertyGroup>"))
    Expect-Rejected 'receipt-with-production-only-local-setting-rejected' (Test-FrozenReceipt) 'GamePath\.props may only define KingmakerInstallDir'
    Expect-Rejected 'build-with-production-only-local-setting-rejected' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))) 'GamePath\.props may only define KingmakerInstallDir'
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal)
    Restore-PairA
    # Structural comparison: the receipt names the effective compiler first and its entries are compared in order.
    $inv = @((Get-Content -LiteralPath $receiptA -Raw | ConvertFrom-Json).compilerInputs)
    Add-Case 'receipt-records-effective-compiler-first' ($inv.Count -gt 2 -and $inv[0].kind -eq 'compiler' -and $inv[0].name -eq 'csc.exe' -and $inv[0].sha256 -match '^[0-9a-f]{64}$') ("first entries: $($inv[0].kind) $($inv[0].name); $($inv[1].kind) $($inv[1].name)")
    Edit-Json $receiptA { param($j) $x = @($j.compilerInputs); $t = $x[1]; $x[1] = $x[2]; $x[2] = $t; $j.compilerInputs = $x }
    Expect-Rejected 'receipt-with-reordered-compiler-arguments-rejected' (Test-FrozenReceipt) 'compiler inputs differ .*entry'
    Restore-PairA
    Edit-Json $receiptA { param($j) $j.compilerInputs = @(@($j.compilerInputs) | Select-Object -SkipLast 1) }
    Expect-Rejected 'receipt-with-missing-compiler-entry-rejected' (Test-FrozenReceipt) 'entry count'
    Restore-PairA
    # A compiler selection through the ignored props (CscToolPath) is an unsupported override: refused for reuse.
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal.Replace('</PropertyGroup>', "<CscToolPath>$([Security.SecurityElement]::Escape($temp))</CscToolPath></PropertyGroup>"))
    Expect-Rejected 'receipt-with-compiler-selection-override-rejected' (Test-FrozenReceipt) 'GamePath\.props may only define KingmakerInstallDir'
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal)
    Expect-Accepted 'receipt-after-override-removed-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    # Local imports outside GamePath.props: a target selected through the DirectoryBuildTargetsPath environment variable
    # that changes the generated version only for a real compile, and the same target as an untracked
    # src\Directory.Build.targets, are refused for reuse (and by Build.ps1); with them removed the receipt is accepted.
    $localTargets = Join-Path $temp 'local selected.targets'
    [IO.File]::WriteAllText($localTargets, "<Project><PropertyGroup Condition=`"'`$(SkipCompilerExecution)' != 'true'`"><KbpVersion>9.9.9</KbpVersion></PropertyGroup></Project>")
    $env:DirectoryBuildTargetsPath = $localTargets
    try { Expect-Rejected 'receipt-with-import-selected-by-environment-rejected' (Test-FrozenReceipt) 'environment variable DirectoryBuildTargetsPath selects MSBuild imports' }
    finally { Remove-Item Env:\DirectoryBuildTargetsPath }
    $untrackedTargets = Join-Path $base 'src\Directory.Build.targets'
    Copy-Item -LiteralPath $localTargets -Destination $untrackedTargets
    Expect-Rejected 'receipt-with-untracked-directory-build-targets-rejected' (Test-FrozenReceipt) 'Untracked MSBuild configuration files apply to the build: src/Directory\.Build\.targets'
    Expect-Rejected 'build-with-untracked-directory-build-targets-rejected' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'))) 'Untracked MSBuild configuration files apply to the build'
    Remove-Item -LiteralPath $untrackedTargets
    Expect-Accepted 'receipt-after-local-imports-removed-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    # R1-F3: the same production-only target as an ignored project-extension import (obj\KingmakerBuffPlanner.csproj.*.targets).
    # MSBuild's preprocessor shows the production project would import it; reuse and Build.ps1 refuse it; removed, the
    # receipt is accepted again.
    $extensionTargets = Join-Path $base 'src\KingmakerBuffPlanner\obj\KingmakerBuffPlanner.csproj.local.targets'
    New-Item -ItemType Directory -Path (Split-Path -Parent $extensionTargets) -Force | Out-Null
    Copy-Item -LiteralPath $localTargets -Destination $extensionTargets
    $preprocessed = Join-Path $temp 'project-extension-preprocessed.xml'
    $pp = Invoke-MSBuildIsolated @($project, "/pp:$preprocessed")
    $ppText = if (Test-Path -LiteralPath $preprocessed) { [IO.File]::ReadAllText($preprocessed) } else { '' }
    Add-Case 'project-extension-target-is-imported-by-msbuild' ($pp.ExitCode -eq 0 -and $ppText.Contains('KingmakerBuffPlanner.csproj.local.targets') -and $ppText.Contains('<KbpVersion>9.9.9</KbpVersion>')) ("preprocess exit $($pp.ExitCode); imported: $($ppText.Contains('KingmakerBuffPlanner.csproj.local.targets'))")
    Expect-Rejected 'receipt-with-project-extension-import-rejected' (Test-FrozenReceipt) 'Project-extension imports apply to the production build: .*KingmakerBuffPlanner\.csproj\.local\.targets'
    Expect-Rejected 'build-with-project-extension-import-rejected' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'))) 'Project-extension imports apply to the production build'
    Remove-Item -LiteralPath $extensionTargets
    Expect-Accepted 'receipt-after-project-extension-import-removed-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    # R5-F2: an ignored .user file of the test project (not part of the production solution) does not stop the production
    # build or invalidate the unchanged receipt; the production project's own .user file is still refused.
    $userText = "<Project><PropertyGroup><KbpVersion>9.9.9</KbpVersion></PropertyGroup></Project>"
    $testUser = Join-Path $base 'tests\KingmakerBuffPlanner.Tests\KingmakerBuffPlanner.Tests.csproj.user'
    [IO.File]::WriteAllText($testUser, $userText)
    Expect-Accepted 'receipt-with-test-project-user-file-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    $testUserBuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'))
    Add-Case 'build-with-test-project-user-file-succeeds' ($testUserBuild.ExitCode -eq 0) ("exit $($testUserBuild.ExitCode); " + (Get-Reason $testUserBuild.Output))
    Expect-Accepted 'receipt-after-build-with-test-project-user-file-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    Remove-Item -LiteralPath $testUser
    $productionUser = Join-Path $base 'src\KingmakerBuffPlanner\KingmakerBuffPlanner.csproj.user'
    [IO.File]::WriteAllText($productionUser, $userText)
    Expect-Rejected 'receipt-with-production-project-user-file-rejected' (Test-FrozenReceipt) 'Untracked MSBuild configuration files apply to the build: src/KingmakerBuffPlanner/KingmakerBuffPlanner\.csproj\.user'
    Expect-Rejected 'build-with-production-project-user-file-rejected' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'))) 'Untracked MSBuild configuration files apply to the build'
    Remove-Item -LiteralPath $productionUser
    Expect-Accepted 'receipt-after-production-user-file-removed-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'
    # R6-F1: the production project's .user file in another letter case, with a setting that applies only to a real
    # compile. Windows resolves the name and MSBuild's preprocessor shows the import; reuse and Build.ps1 refuse it;
    # removed, the receipt is accepted again. (The test project's .user file stays accepted above.)
    $casedUser = Join-Path $base 'src\KingmakerBuffPlanner\kingmakerbuffplanner.CSPROJ.USER'
    [IO.File]::WriteAllText($casedUser, "<Project><PropertyGroup Condition=`"'`$(SkipCompilerExecution)' != 'true'`"><KbpVersion>9.9.8</KbpVersion></PropertyGroup></Project>")
    $casedPreprocessed = Join-Path $temp 'cased-user-preprocessed.xml'
    $pp = Invoke-MSBuildIsolated @($project, "/pp:$casedPreprocessed")
    $ppText = if (Test-Path -LiteralPath $casedPreprocessed) { [IO.File]::ReadAllText($casedPreprocessed) } else { '' }
    Add-Case 'differently-cased-production-user-file-is-imported-by-msbuild' ($pp.ExitCode -eq 0 -and $ppText.Contains('<KbpVersion>9.9.8</KbpVersion>')) ("preprocess exit $($pp.ExitCode); real-compile-only setting imported: $($ppText.Contains('<KbpVersion>9.9.8</KbpVersion>'))")
    Expect-Rejected 'receipt-with-differently-cased-production-user-file-rejected' (Test-FrozenReceipt) 'Untracked MSBuild configuration files apply to the build: src/KingmakerBuffPlanner/kingmakerbuffplanner\.CSPROJ\.USER'
    Expect-Rejected 'build-with-differently-cased-production-user-file-rejected' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build.ps1'))) 'Untracked MSBuild configuration files apply to the build'
    Remove-Item -LiteralPath $casedUser
    Expect-Accepted 'receipt-after-differently-cased-user-file-removed-accepted' (Test-FrozenReceipt) 'RECEIPT ACCEPTED'

    # 9. The runtime transaction stages only its own copy of the admitted package; a package replaced after admission is
    #    refused before anything is staged or activated (fixture game folder; FixtureMode roots in the temporary folder).
    $harness = Join-Path $base 'scripts\RuntimeHarness.Common.ps1'
    # Inside the clone's harness test root, with a private Kingmaker
    # native-test reservation there (inherited by the isolated child
    # processes); the machine-wide reservation is never touched.
    $fixtureRoot = Join-Path $base 'artifacts\runtime-harness-tests\transaction fixture'
    $priorReservationRoot = $env:KBP_TEST_RESERVATION_ROOT
    $env:KBP_TEST_RESERVATION_ROOT = Join-Path $base 'artifacts\runtime-harness-tests\reservation'
    $txRoots = @('state', 'staging', 'backups') | ForEach-Object { $d = Join-Path $fixtureRoot $_; New-Item -ItemType Directory -Path $d -Force | Out-Null; $d }
    $fixtureGame = Join-Path $fixtureRoot 'game'
    New-Item -ItemType Directory -Path (Join-Path $fixtureGame 'Mods\Existing') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $fixtureGame 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    Set-Content -LiteralPath (Join-Path $fixtureGame 'Mods\Existing\Info.json') -Value '{"Id":"Existing"}' -Encoding Ascii
    $admitted = Get-KbpSha256 $zipA
    $swapped = Join-Path $inputs 'swapped-after-admission.zip'
    Copy-ZipWith $zipA $swapped { param($name, $bytes) if ($name -ceq "$modFolder/NativeEffectOverrides.json") { [pscustomobject]@{ Name = $name; Bytes = [Text.Encoding]::UTF8.GetBytes(([Text.Encoding]::UTF8.GetString($bytes)).TrimEnd() + "`n") } } else { [pscustomobject]@{ Name = $name; Bytes = $bytes } } }
    $tx = { param($zip, $runId) Invoke-Script (". $(Quote $harness); Enter-KbpRuntimeTransaction -PackagePath $(Quote $zip) -KingmakerInstallDir $(Quote $fixtureGame) -StateRoot $(Quote $txRoots[0]) -StagingRoot $(Quote $txRoots[1]) -BackupRoot $(Quote $txRoots[2]) -RunId $runId -FixtureMode -KnownKingmakerProcessIds @() -ExpectedPackageSha256 $admitted | Out-Null; 'ENTERED'") }
    $modsBefore = @(Get-ChildItem -LiteralPath (Join-Path $fixtureGame 'Mods') -Recurse -Force | ForEach-Object { $_.FullName }) -join ';'
    Expect-Rejected 'transaction-refuses-package-replaced-after-admission' (& $tx $swapped 'swapped') 'changed after admission'
    $modsAfter = @(Get-ChildItem -LiteralPath (Join-Path $fixtureGame 'Mods') -Recurse -Force | ForEach-Object { $_.FullName }) -join ';'
    $strandedCopies = @(Get-ChildItem -LiteralPath $txRoots[2] -Recurse -Filter 'package.zip' -ErrorAction SilentlyContinue).Count
    Add-Case 'transaction-refusal-left-mods-untouched' ($modsBefore -ceq $modsAfter -and -not (Test-Path -LiteralPath (Join-Path $fixtureGame 'Mods\KingmakerBuffPlanner')) -and $strandedCopies -eq 0) "fixture Mods unchanged; nothing staged or activated; transaction package copies left: $strandedCopies"
    $entered = & $tx $zipA 'admitted'
    $txState = @(Get-ChildItem -LiteralPath $txRoots[0] -Recurse -Filter '*.json' | Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match '"ownedPackagePath"' } | Select-Object -First 1)
    $st = if ($txState.Count) { Get-Content -LiteralPath $txState[0].FullName -Raw | ConvertFrom-Json } else { $null }
    $ok = $entered.ExitCode -eq 0 -and $entered.Output -match 'ENTERED' -and $st -and $st.packageSha256 -eq $admitted -and (Get-KbpSha256 $st.ownedPackagePath) -eq $admitted -and
        (Get-KbpSha256 (Join-Path $fixtureGame 'Mods\KingmakerBuffPlanner\KingmakerBuffPlanner.dll')) -eq $ma.dllSha256
    Add-Case 'transaction-stages-its-own-admitted-copy' $ok ("state packageSha256 $($st.packageSha256); owned copy $(if ($st) { Split-Path -Leaf $st.ownedPackagePath }); staged DLL $(Get-KbpSha256 (Join-Path $fixtureGame 'Mods\KingmakerBuffPlanner\KingmakerBuffPlanner.dll'))")
    Invoke-Script (". $(Quote $harness); Restore-KbpRuntimeTransaction -RunId admitted -StateRoot $(Quote $txRoots[0]) -FixtureMode -KnownKingmakerProcessIds @() | Out-Null") | Out-Null
    # A state-write failure after the package copy (injected by wrapping Write-KbpJsonAtomic for the state record only):
    # Mods unchanged, no owned ZIP and no lock left, and the same run ID then enters normally.
    $modsBefore = @(Get-ChildItem -LiteralPath (Join-Path $fixtureGame 'Mods') -Recurse -Force | ForEach-Object { $_.FullName }) -join ';'
    $failState = Invoke-Script (". $(Quote $harness); `$original = `${function:Write-KbpJsonAtomic}; function Write-KbpJsonAtomic([string]`$Path, `$Value) { if (`$Value -is [Collections.IDictionary] -and `$Value.Contains('ownedPackagePath')) { throw 'injected state-write failure' }; & `$original `$Path `$Value }; Enter-KbpRuntimeTransaction -PackagePath $(Quote $zipA) -KingmakerInstallDir $(Quote $fixtureGame) -StateRoot $(Quote $txRoots[0]) -StagingRoot $(Quote $txRoots[1]) -BackupRoot $(Quote $txRoots[2]) -RunId statefail -FixtureMode -KnownKingmakerProcessIds @() -ExpectedPackageSha256 $admitted | Out-Null")
    $modsAfter = @(Get-ChildItem -LiteralPath (Join-Path $fixtureGame 'Mods') -Recurse -Force | ForEach-Object { $_.FullName }) -join ';'
    $copiesLeft = @(Get-ChildItem -LiteralPath $txRoots[2] -Recurse -Filter 'package.zip' -ErrorAction SilentlyContinue | Where-Object { $_.FullName -like '*statefail*' }).Count
    $lockLeft = Test-Path -LiteralPath (Join-Path $txRoots[0] 'deployment.lock')
    Add-Case 'transaction-state-write-failure-leaves-no-copy-or-lock' ($failState.ExitCode -ne 0 -and $failState.Output -match 'injected state-write failure' -and $modsBefore -ceq $modsAfter -and $copiesLeft -eq 0 -and -not $lockLeft) "exit $($failState.ExitCode); Mods unchanged: $($modsBefore -ceq $modsAfter); owned copies left: $copiesLeft; lock left: $lockLeft"
    $retryState = & $tx $zipA 'statefail'
    Add-Case 'transaction-same-run-id-retry-after-state-write-failure' ($retryState.ExitCode -eq 0 -and $retryState.Output -match 'ENTERED' -and (Get-KbpSha256 (Join-Path $fixtureGame 'Mods\KingmakerBuffPlanner\KingmakerBuffPlanner.dll')) -eq $ma.dllSha256) "exit $($retryState.ExitCode)"
    Invoke-Script (". $(Quote $harness); Restore-KbpRuntimeTransaction -RunId statefail -StateRoot $(Quote $txRoots[0]) -FixtureMode -KnownKingmakerProcessIds @() | Out-Null") | Out-Null
    # The same state-write failure while the owned ZIP denies deletion (held open by the failing process): Mods unchanged,
    # the token-checked lock is still released, and the error names the owned copy that still needs cleanup.
    $modsBefore = @(Get-ChildItem -LiteralPath (Join-Path $fixtureGame 'Mods') -Recurse -Force | ForEach-Object { $_.FullName }) -join ';'
    $heldState = Invoke-Script (". $(Quote $harness); `$original = `${function:Write-KbpJsonAtomic}; function Write-KbpJsonAtomic([string]`$Path, `$Value) { if (`$Value -is [Collections.IDictionary] -and `$Value.Contains('ownedPackagePath')) { `$global:heldCopy = [IO.File]::Open([string]`$Value['ownedPackagePath'], 'Open', 'Read', 'None'); throw 'injected state-write failure' }; & `$original `$Path `$Value }; Enter-KbpRuntimeTransaction -PackagePath $(Quote $zipA) -KingmakerInstallDir $(Quote $fixtureGame) -StateRoot $(Quote $txRoots[0]) -StagingRoot $(Quote $txRoots[1]) -BackupRoot $(Quote $txRoots[2]) -RunId heldcopy -FixtureMode -KnownKingmakerProcessIds @() -ExpectedPackageSha256 $admitted | Out-Null")
    $modsAfter = @(Get-ChildItem -LiteralPath (Join-Path $fixtureGame 'Mods') -Recurse -Force | ForEach-Object { $_.FullName }) -join ';'
    $lockLeft = Test-Path -LiteralPath (Join-Path $txRoots[0] 'deployment.lock')
    Add-Case 'transaction-cleanup-failure-still-releases-the-lock' ($heldState.ExitCode -ne 0 -and $heldState.Output -match 'injected state-write failure' -and $heldState.Output -match 'its lock was released' -and $heldState.Output -match 'still to clean up: owned package copy' -and $modsBefore -ceq $modsAfter -and -not $lockLeft) ("exit $($heldState.ExitCode); Mods unchanged: $($modsBefore -ceq $modsAfter); lock left: $lockLeft; " + (Get-Reason $heldState.Output))
    Get-ChildItem -LiteralPath $txRoots[2] -Directory -Filter '*heldcopy*' -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
    $env:KBP_TEST_RESERVATION_ROOT = $priorReservationRoot

    # Release reconciliation (Build-Release): the clone's receipt for the clean pair as both deterministic builds; the
    # validated release copy must be byte-identical. A substituted release copy (same DLL, different data) and a dirty
    # producing receipt are refused; the matching clean pair is accepted.
    $common = Join-Path $base 'scripts\Common.ps1'
    $validatorScript = Join-Path $base 'scripts\validate-package.ps1'
    # Two independent producing receipts are read; an edit applies to the second producer only (the first stays clean).
    $reconcile = { param($releaseZip, $receiptEdit)
        $r1 = Join-Path $inputs ('release-receipt-' + [Guid]::NewGuid().ToString('N') + '.json')
        Copy-Item -LiteralPath $receiptA -Destination $r1
        $r = Join-Path $inputs ('release-receipt-' + [Guid]::NewGuid().ToString('N') + '.json')
        Copy-Item -LiteralPath $receiptA -Destination $r
        if ($receiptEdit) { Edit-Json $r $receiptEdit }
        Invoke-Script (". $(Quote $common); `$v = Join-Path $(Quote $inputs) ('release-validation-' + [Guid]::NewGuid().ToString('N') + '.json'); & $(Quote $validatorScript) -PackagePath $(Quote $releaseZip) -ReportPath `$v | Out-Null; `$m1 = Get-Content -LiteralPath $(Quote $r1) -Raw | ConvertFrom-Json; `$m = Get-Content -LiteralPath $(Quote $r) -Raw | ConvertFrom-Json; `$b1 = [pscustomobject]@{ Manifest = `$m1; PackageSha256 = [string]`$m1.packageSha256 }; `$b = [pscustomobject]@{ Manifest = `$m; PackageSha256 = [string]`$m.packageSha256 }; Assert-KbpReleaseReconciliation -ReleaseHead $(Quote ([string]$ma.commit)) -Builds @(`$b1, `$b) -ValidatedRelease (Get-Content -LiteralPath `$v -Raw | ConvertFrom-Json) -ReleasePackageSha256 (Get-KbpSha256 $(Quote $releaseZip)); 'RECONCILED'")
    }
    Expect-Accepted 'release-reconciliation-matching-clean-pair-accepted' (& $reconcile $zipA $null) 'RECONCILED'
    # The validator admits packages under artifacts only, so the substituted copy is placed there.
    $substituted = Join-Path $base 'artifacts\release-substituted.zip'
    Copy-Item -LiteralPath $swapped -Destination $substituted
    Expect-Rejected 'release-reconciliation-substituted-release-copy-rejected' (& $reconcile $substituted $null) 'does not match the deterministic build'
    Remove-Item -LiteralPath $substituted
    Expect-Rejected 'release-reconciliation-dirty-producing-receipt-rejected' (& $reconcile $zipA { param($j) $j.sourceDirty = $true }) 'does not name the clean release commit'
    # A malformed clean flag (an empty array, the string "false", the number 0) on the second producing receipt, with the
    # first receipt clean, is refused.
    foreach ($bad in @(@('empty-array', @()), @('string-false', 'false'), @('number-zero', 0))) {
        $badValue = $bad[1]
        Expect-Rejected "release-reconciliation-$($bad[0])-dirty-flag-rejected" (& $reconcile $zipA { param($j) $j.sourceDirty = $badValue }) 'does not name the clean release commit'
    }

    # 10. The release installer reconciles every identity of the release manifest with a validated private copy of the
    #     release ZIP before any state or staging (Install-Local -WhatIf against a fixture game folder).
    $rel = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Release.ps1'))
    $releaseManifest = Join-Path $base ("artifacts\release\" + $ma.version + '\release-manifest.json')
    $rm = if (Test-Path -LiteralPath $releaseManifest) { Get-Content -LiteralPath $releaseManifest -Raw | ConvertFrom-Json } else { $null }
    Add-Case 'release-manifest-from-validated-release-zip' ($rel.ExitCode -eq 0 -and $rm -and $rm.embeddedCommit -eq $commit -and $rm.commit -eq $commit -and $rm.dllSha256 -eq $ma.dllSha256) ("exit $($rel.ExitCode); commit $($rm.commit) embedded $($rm.embeddedCommit) DLL $($rm.dllSha256) MVID $($rm.assemblyMvid)")
    # The versioned release above is the previous release. Disposable instrumented copies of Build-Release are written
    # under the ignored artifacts folder (the checkout stays clean, so both producing builds really run) with their
    # script root pointing at the checkout's scripts.
    $releaseDir = Split-Path -Parent $releaseManifest
    $releaseFiles = { param($dir = $releaseDir) @(Get-ChildItem -LiteralPath $dir -File | Sort-Object Name | ForEach-Object { $_.Name + '=' + (Get-KbpSha256 $_.FullName) }) -join ';' }
    $releaseBefore = & $releaseFiles
    $releaseText = [IO.File]::ReadAllText((Join-Path $base 'scripts\Build-Release.ps1')).Replace('$PSScriptRoot', "'" + (Join-Path $base 'scripts') + "'")
    $instrumentRelease = { param([string]$Anchor, [string]$Insert, [string]$Name)
        if ($releaseText.IndexOf($Anchor) -lt 0) { throw "Build-Release.ps1 anchor not found: $Anchor" }
        $path = Join-Path $base ('artifacts\' + $Name + '.ps1')
        [IO.File]::WriteAllText($path, $releaseText.Replace($Anchor, $Anchor + "`r`n" + $Insert))
        return $path
    }
    $cleanBeforeRelease = @(& git -C $base status --porcelain).Count
    # R4-F1: the first producer is replaced by a different but valid package: its ZIP re-written under the disposable
    # checkout's artifacts with every entry stored without compression (same names, order, bytes and fixed timestamps),
    # and its receipt naming that ZIP. Both producer ZIPs pass the standalone validator inside the instrumented release;
    # the inter-build reconciliation then refuses, and the previous ZIP, manifest and notes keep their hashes.
    $altInsert = @(
        'Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem',
        '$altDir = Join-Path $root ''artifacts\alt-producer''; [void](New-Item -ItemType Directory -Path $altDir -Force); $altPackage = Join-Path $altDir (''KingmakerBuffPlanner-'' + $version + ''.zip'')',
        '$altIn = [IO.Compression.ZipFile]::OpenRead($firstPackage); $altOut = [IO.Compression.ZipFile]::Open($altPackage, ''Create'')',
        'try { foreach ($e in $altIn.Entries) { $n = $altOut.CreateEntry($e.FullName, [IO.Compression.CompressionLevel]::NoCompression); $n.LastWriteTime = $e.LastWriteTime; $s = $e.Open(); $d = $n.Open(); try { $s.CopyTo($d) } finally { $d.Dispose(); $s.Dispose() } } } finally { $altOut.Dispose(); $altIn.Dispose() }',
        'foreach ($v in @(@(''ALT'', $altPackage), @(''SECOND'', $secondPackage))) { $vr = Join-Path $altDir ($v[0] + ''-validation.json''); & (Join-Path $root ''scripts\validate-package.ps1'') -PackagePath $v[1] -ReportPath $vr; $vj = Get-Content -LiteralPath $vr -Raw | ConvertFrom-Json; Write-Host (''VALIDATOR PASS '' + $v[0] + '' package '' + [string]$vj.package_sha256 + '' DLL '' + [string]$vj.dll_sha256) }',
        '$firstPackage = $altPackage; $firstHash = Get-KbpSha256 $altPackage; $firstManifest.packageSha256 = $firstHash; $builds[0] = [pscustomobject]@{ Manifest = $firstManifest; PackageSha256 = $firstHash }',
        'Write-Host (''ALT PRODUCER package '' + $firstHash + '' DLL '' + [string]$firstManifest.dllSha256)') -join "`r`n"
    $disagreeing = & $instrumentRelease '$builds = @([pscustomobject]@{ Manifest = $firstManifest; PackageSha256 = $firstHash }, [pscustomobject]@{ Manifest = $secondManifest; PackageSha256 = $secondHash })' $altInsert 'release-disagreeing-producers'
    $relBad = Invoke-Isolated @('-File', $disagreeing)
    Remove-Item -LiteralPath $disagreeing -Force
    $prepareLeft = @(Get-ChildItem -LiteralPath (Join-Path $base 'artifacts\release') -Directory -Force -Filter '.prepare-*').Count
    # Console wrapping can split long digests, so the error is matched with all whitespace removed.
    $mm = [regex]::Match(($relBad.Output -replace '\s+', ''), 'Clean-headdeterministicbuildfailed:package([0-9a-f]{64})/([0-9a-f]{64})DLL([0-9a-f]{64})/([0-9a-f]{64})')
    $altDirectory = Join-Path $base 'artifacts\alt-producer'
    $vAlt = [regex]::Match(($relBad.Output -replace '\s+', ''), 'VALIDATORPASSALTpackage([0-9a-f]{64})DLL([0-9a-f]{64})')
    $vSecond = [regex]::Match(($relBad.Output -replace '\s+', ''), 'VALIDATORPASSSECONDpackage([0-9a-f]{64})DLL([0-9a-f]{64})')
    Remove-Item -LiteralPath $altDirectory -Recurse -Force -ErrorAction SilentlyContinue
    $altCleaned = -not (Test-Path -LiteralPath $altDirectory)
    $mismatch = if ($mm.Success) { "inter-build mismatch: package $($mm.Groups[1].Value)/$($mm.Groups[2].Value) DLL $($mm.Groups[3].Value)/$($mm.Groups[4].Value)" } else { 'no inter-build mismatch reported: ' + (Get-Reason $relBad.Output) }
    Add-Case 'release-with-disagreeing-builds-keeps-previous-release' ($cleanBeforeRelease -eq 0 -and $relBad.ExitCode -ne 0 -and $mm.Success -and $mm.Groups[1].Value -cne $mm.Groups[2].Value -and $relBad.Output -match 'ALT PRODUCER' -and
        $vAlt.Success -and $vSecond.Success -and $vAlt.Groups[1].Value -ceq $mm.Groups[1].Value -and $vSecond.Groups[1].Value -ceq $mm.Groups[2].Value -and $vAlt.Groups[2].Value -ceq $vSecond.Groups[2].Value -and $altCleaned -and
        $releaseBefore -notmatch '^$' -and (& $releaseFiles) -ceq $releaseBefore -and $prepareLeft -eq 0) ("validator PASS for both producers: $($vAlt.Success -and $vSecond.Success) (ALT $($vAlt.Groups[1].Value), SECOND $($vSecond.Groups[1].Value)); alt artifacts removed: $altCleaned; " + "clean checkout: $($cleanBeforeRelease -eq 0); exit $($relBad.ExitCode); $mismatch; previous release before/after: $releaseBefore / $(& $releaseFiles); private folders left: $prepareLeft")
    # R5-F1: a terminating unwind injected immediately after the previous release is moved aside. The private folder was
    # marked kept before the move, so the previous ZIP, manifest and notes survive at the reported recovery path.
    $unwound = & $instrumentRelease '[IO.Directory]::Move($finalReleaseRoot, $previousRelease)' 'throw ''FIXTURE: unwind immediately after the backup move''' 'release-unwind-after-backup-move'
    $relUnwound = Invoke-Isolated @('-File', $unwound)
    Remove-Item -LiteralPath $unwound -Force
    $keptUnwound = @(Get-ChildItem -LiteralPath (Join-Path $base 'artifacts\release') -Directory -Force -Filter '.prepare-*' | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'previous') })
    $unwoundRecovery = if ($keptUnwound.Count -eq 1 -and ($relUnwound.Output -replace '\s+', '').Contains(((Join-Path $keptUnwound[0].FullName 'previous') -replace '\s+', '') + '.')) { Join-Path $keptUnwound[0].FullName 'previous' } else { '' }
    $unwoundFiles = if ($unwoundRecovery) { & $releaseFiles $unwoundRecovery } else { '' }
    Add-Case 'release-unwind-after-backup-move-keeps-previous-release-at-recovery-path' ($relUnwound.ExitCode -ne 0 -and $relUnwound.Output -match 'FIXTURE: unwind immediately after the backup move' -and $unwoundRecovery -and $unwoundFiles -ceq $releaseBefore -and -not (Test-Path -LiteralPath $releaseDir)) ("exit $($relUnwound.ExitCode); recovery path reported: $([bool]$unwoundRecovery); previous release there: $unwoundFiles; versioned path present: $(Test-Path -LiteralPath $releaseDir)")
    if ($unwoundRecovery) {
        [IO.Directory]::Move($unwoundRecovery, $releaseDir)
        Remove-Item -LiteralPath (Split-Path -Parent $unwoundRecovery) -Recurse -Force
    }
    Add-Case 'release-fixture-restored-after-unwind' ((& $releaseFiles) -ceq $releaseBefore) ("previous release restored: $((& $releaseFiles) -ceq $releaseBefore)")
    # R5-F1 (control): the versioned path is occupied right after the previous release is moved aside. Promotion and restoration
    # are refused, the occupant is untouched, and the previous ZIP and manifest survive at the reported recovery path.
    $occupyInsert = '[void](New-Item -ItemType Directory -Path $finalReleaseRoot); [IO.File]::WriteAllText((Join-Path $finalReleaseRoot ''occupant.txt''), ''occupant'')'
    $occupied = & $instrumentRelease '[IO.Directory]::Move($finalReleaseRoot, $previousRelease)' $occupyInsert 'release-occupied-destination'
    $relOccupied = Invoke-Isolated @('-File', $occupied)
    Remove-Item -LiteralPath $occupied -Force
    # The preserved private folder is found on disk; the (possibly console-wrapped) error must name it.
    $kept = @(Get-ChildItem -LiteralPath (Join-Path $base 'artifacts\release') -Directory -Force -Filter '.prepare-*' | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'previous') })
    $recovery = if ($kept.Count -eq 1 -and ($relOccupied.Output -replace '\s+', '').Contains(((Join-Path $kept[0].FullName 'previous') -replace '\s+', '') + '.')) { Join-Path $kept[0].FullName 'previous' } else { '' }
    $recoveredFiles = if ($recovery -and (Test-Path -LiteralPath $recovery)) { & $releaseFiles $recovery } else { '' }
    $occupant = @(Get-ChildItem -LiteralPath $releaseDir -Force | ForEach-Object { $_.Name }) -join ','
    Add-Case 'release-occupied-destination-keeps-previous-release-at-recovery-path' ($relOccupied.ExitCode -ne 0 -and $recovery -and $recoveredFiles -ceq $releaseBefore -and $occupant -ceq 'occupant.txt') ("exit $($relOccupied.ExitCode); recovery path reported: $([bool]$recovery); previous release there: $recoveredFiles; versioned path holds: $occupant")
    # Fixture restoration for the cases below: the occupant is removed and the preserved previous release moved back.
    if ($recovery -and (Test-Path -LiteralPath $recovery)) {
        Remove-Item -LiteralPath $releaseDir -Recurse -Force
        [IO.Directory]::Move($recovery, $releaseDir)
        Remove-Item -LiteralPath (Split-Path -Parent $recovery) -Recurse -Force
    }
    Add-Case 'release-fixture-restored-after-occupied-destination' ((& $releaseFiles) -ceq $releaseBefore) ("previous release restored: $((& $releaseFiles) -ceq $releaseBefore)")
    $installGame = Join-Path $temp 'install fixture game'
    New-Item -ItemType Directory -Path (Join-Path $installGame 'Mods') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $installGame 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    $install = { param($manifestPath, $id) Invoke-Isolated @('-File', (Join-Path $base 'scripts\Install-Local.ps1'), '-ReleaseManifestPath', $manifestPath, '-InstallId', $id, '-KingmakerInstallDir', $installGame, '-WhatIf') }
    # A matching release passes the reconciliation; the installer's later host checks may still refuse because of an
    # unrelated pre-existing runtime transaction in the lab (owner state, never touched); that refusal is reported as such.
    $g = & $install $releaseManifest 'genuine'
    $gText = $g.Output -replace '\s+', ' '
    $ok = $gText -notmatch 'does not match the validated release package' -and $gText -match 'Package validation: PASS=13 FAIL=0' -and (($g.ExitCode -eq 0 -and $gText -match 'Local install preflight PASS') -or ($g.ExitCode -ne 0 -and $gText -match 'Unresolved runtime transaction exists|Pathfinder: Kingmaker is running'))
    Add-Case 'install-preflight-matching-release-passes-reconciliation' $ok $(if ($g.ExitCode -eq 0) { 'reconciled; Local install preflight PASS' } else { 'reconciled (validated copy matches the release manifest); then stopped by an unrelated host gate (a pre-existing lab runtime transaction or a running Kingmaker owned by another session): ' + (Get-Reason $g.Output) })
    $savedRelease = Join-Path $inputs 'release-manifest.json'
    Copy-Item -LiteralPath $releaseManifest -Destination $savedRelease
    foreach ($bad in @(
        @{ Name = 'commit'; Edit = { param($j) $j.commit = ('e' * 40) }; Pattern = 'embedded source revision' },
        @{ Name = 'mvid'; Edit = { param($j) $j.assemblyMvid = '00000000-0000-0000-0000-000000000000' }; Pattern = 'MVID' },
        @{ Name = 'dll-digest'; Edit = { param($j) $j.dllSha256 = ('d' * 64) }; Pattern = 'DLL SHA-256' })) {
        Copy-Item -LiteralPath $savedRelease -Destination $releaseManifest -Force
        Edit-Json $releaseManifest $bad.Edit
        Expect-Rejected "install-preflight-wrong-release-$($bad.Name)-rejected" (& $install $releaseManifest ('bad-' + $bad.Name)) $bad.Pattern
    }
    Copy-Item -LiteralPath $savedRelease -Destination $releaseManifest -Force
    Edit-Json $releaseManifest { param($j) $j.embeddedCommit = ('e' * 40) }
    Expect-Rejected 'install-preflight-wrong-embedded-commit-field-rejected' (& $install $releaseManifest 'bad-embedded') 'embeddedCommit field'
    Copy-Item -LiteralPath $savedRelease -Destination $releaseManifest -Force
    $installState = @(Get-ChildItem -LiteralPath (Join-Path $installGame 'Mods') -Force).Count
    Add-Case 'install-preflight-refusals-left-no-staging' ($installState -eq 0) "fixture Mods entries after the refusals: $installState"

    # 11. Deploy-Local -WhatIf (directly and through the compatibility wrapper) with the clone's package, a private game
    #     folder and the selected loader row: the matching row passes the shared preflight; a Harmony2-only mismatch is
    #     refused before staging. The matching case may then stop at an unrelated pre-existing lab runtime transaction.
    $deployGame = Join-Path $temp 'deploy fixture game'
    $deployLoader = Join-Path $deployGame 'Kingmaker_Data\Managed\UnityModManager'
    New-Item -ItemType Directory -Path $deployLoader, (Join-Path $deployGame 'Mods') -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $deployGame 'Kingmaker.exe') -Value 'fixture' -Encoding Ascii
    foreach ($file in @('UnityModManager.dll', '0Harmony12.dll', '0Harmony.dll')) { Copy-Item -LiteralPath (Join-Path $installDir "Kingmaker_Data\Managed\UnityModManager\$file") -Destination $deployLoader }
    $deploy = { param($script, $extra) Invoke-Isolated (@('-File', $script, '-PackagePath', $zipA, '-RunId', 'deploy-preflight', '-KingmakerInstallDir', $deployGame, '-LoaderId', 'owner-0.33.0', '-WhatIf') + $extra) }
    $deployScript = Join-Path $base 'scripts\Deploy-Local.ps1'
    $wrapper = Join-Path $base 'scripts\compatibility\Enter-KingmakerCompatibilityProfile.ps1'
    foreach ($entry in @(@('deploy-local', $deployScript, @('-CompatibilityProfileId', 'native-only')), @('compatibility-wrapper', $wrapper, @('-ProfileId', 'native-only')))) {
        $m = & $deploy $entry[1] $entry[2]
        $mText = $m.Output -replace '\s+', ' '
        $ok = $mText -notmatch 'Installed loader does not match' -and (($m.ExitCode -eq 0 -and $mText -match 'Deployment preflight PASS') -or ($m.ExitCode -ne 0 -and $mText -match 'Unresolved runtime transaction exists|Pathfinder: Kingmaker is running'))
        Add-Case "$($entry[0])-whatif-matching-loader-row-passes-preflight" $ok $(if ($m.ExitCode -eq 0) { 'Deployment preflight PASS' } else { 'loader row matched; then stopped by an unrelated host gate (a pre-existing lab runtime transaction or a running Kingmaker owned by another session): ' + (Get-Reason $m.Output) })
    }
    Copy-Item -LiteralPath (Join-Path $deployLoader '0Harmony12.dll') -Destination (Join-Path $deployLoader '0Harmony.dll') -Force
    foreach ($entry in @(@('deploy-local', $deployScript, @('-CompatibilityProfileId', 'native-only')), @('compatibility-wrapper', $wrapper, @('-ProfileId', 'native-only')))) {
        Expect-Rejected "$($entry[0])-whatif-harmony2-only-mismatch-refused" (& $deploy $entry[1] $entry[2]) 'Installed loader does not match loader row owner-0\.33\.0: harmony2'
    }
    Add-Case 'deploy-fixture-mods-untouched' (@(Get-ChildItem -LiteralPath (Join-Path $deployGame 'Mods') -Force).Count -eq 0) 'private game Mods folder still empty'
    $launcherText = [IO.File]::ReadAllText((Join-Path $base 'scripts\Invoke-KingmakerRuntimeTest.ps1'))
    $ok = ([regex]::Matches($launcherText, '-CompatibilityProfileId \$CompatibilityProfileId -LoaderId \$LoaderId')).Count -eq 2 -and $launcherText.Contains('-Request $requestSnapshot') -and -not $launcherText.Contains('-Request (Read-KbpJson $requestPath)')
    Add-Case 'launcher-forwards-loader-row-and-checks-retained-request' $ok 'both Deploy-Local calls pass -LoaderId; the result is checked against the request snapshot retained before launch'

    $artifactsAfter = @(Get-ChildItem -LiteralPath (Join-Path $source 'artifacts') -File -Recurse -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName + '=' + $_.Length + '@' + $_.LastWriteTimeUtc.Ticks }) -join ';'
    Add-Case 'repository-artifacts-untouched' ($artifactsBefore -ceq $artifactsAfter) 'this checkout''s artifacts are unchanged'
    Add-Case 'legacy-input-preserved' ((Get-KbpSha256 $legacyZip) -ceq $legacySha256) "private copy still $legacySha256"

    $failed = @($cases | Where-Object { -not $_.passed })
    $summary = [ordered]@{
        schema = 'kbp.loader-baseline-tests.v2'
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
