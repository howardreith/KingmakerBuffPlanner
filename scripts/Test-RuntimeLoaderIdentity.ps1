[CmdletBinding()]
param(
    # Optional folder holding the official NuGet packages (unitymodmanager.<v>.nupkg, lib.harmony.<v>.nupkg); otherwise
    # they are downloaded. Every package and extracted DLL is hash-checked.
    [string]$PackageDirectory,
    [string]$ReportPath
)
# Offline tests of the selectable runtime loader rows (compatibility/loaders) with private copies of real loader files:
# the installed-loader preflight for each measured row, cross-row and Harmony2-only mismatches, the shared installation
# preflight used by the launcher, Deploy-Local and the profile test, and runtime-result admission against the row the
# request carried (loaded and installed identities). Nothing is launched, deployed or written to the installation.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
. (Join-Path $PSScriptRoot 'compatibility\CompatibilityProfile.Common.ps1')
Add-Type -AssemblyName System.IO.Compression.FileSystem

$cases = [Collections.Generic.List[object]]::new()
function Add-Case([string]$Name, [bool]$Passed, [string]$Detail) {
    $cases.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
    Write-Host ("{0} {1} :: {2}" -f $(if ($Passed) { 'PASS' } else { 'FAIL' }), $Name, $Detail)
}
function Invoke-Expecting([string]$Name, [scriptblock]$Action, [string]$RefusalPattern) {
    try {
        $value = & $Action
        if ($RefusalPattern) { Add-Case $Name $false 'accepted, refusal expected' } else { Add-Case $Name $true 'accepted' }
        return $value
    }
    catch {
        $message = $_.Exception.Message
        if ($RefusalPattern -and $message -match $RefusalPattern) { Add-Case $Name $true ('refused: ' + $message) }
        else { Add-Case $Name $false ('unexpected: ' + $message) }
    }
}
function Copy-Object($Value) { return ($Value | ConvertTo-Json -Depth 8 | ConvertFrom-Json) }

$packages = @{
    'unitymodmanager.0.31.1.nupkg' = @('https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.31.1/unitymodmanager.0.31.1.nupkg', '05a82b66bc44d56d1246f146d16e9fa8f9a42c450c5bc334bcfa9efddbdb53c9')
    'unitymodmanager.0.32.4.nupkg' = @('https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.32.4/unitymodmanager.0.32.4.nupkg', 'a8438e921ca7b0e5805f5a31c196ae65c90f236543885bea0146457d082c9ec9')
    'unitymodmanager.0.33.4.nupkg' = @('https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.33.4/unitymodmanager.0.33.4.nupkg', '7cbab8edccd9db54f8a9e61fdd7969ae1e908c1f53d1509d29ad20fdfdade66b')
    'lib.harmony.2.3.1.1.nupkg' = @('https://api.nuget.org/v3-flatcontainer/lib.harmony/2.3.1.1/lib.harmony.2.3.1.1.nupkg', 'b9096d3e2191ca18b803e2894511a2ee4ec6c92050e53853b46ebfeed46798aa')
    'lib.harmony.2.3.6.nupkg' = @('https://api.nuget.org/v3-flatcontainer/lib.harmony/2.3.6/lib.harmony.2.3.6.nupkg', '498f99c5ca97cfec0e6cb277d721c242071ea9d9f019312cf9eaf015762de075')
    'lib.harmony.2.4.3.nupkg' = @('https://api.nuget.org/v3-flatcontainer/lib.harmony/2.4.3/lib.harmony.2.4.3.nupkg', 'f07af11a0d62447e7e1b6e3fcf1e28fc75d9f8d1abf2d6ee79aabcdc4db43e88')
}
$temp = Join-Path ([IO.Path]::GetTempPath()) ('kbp-loader-identity-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    function Get-Package([string]$Name) {
        $target = Join-Path $temp $Name
        if (-not (Test-Path -LiteralPath $target)) {
            if ($PackageDirectory -and (Test-Path -LiteralPath (Join-Path $PackageDirectory $Name))) { Copy-Item -LiteralPath (Join-Path $PackageDirectory $Name) -Destination $target }
            else { Invoke-WebRequest -Uri $packages[$Name][0] -OutFile $target -UseBasicParsing }
            if ((Get-KbpSha256 $target) -cne $packages[$Name][1]) { throw "Package hash mismatch: $Name" }
        }
        return $target
    }
    function Expand-PackageEntry([string]$Package, [string]$Entry, [string]$Destination) {
        $a = [IO.Compression.ZipFile]::OpenRead((Get-Package $Package))
        try { [IO.Compression.ZipFileExtensions]::ExtractToFile($a.GetEntry($Entry), $Destination, $true) } finally { $a.Dispose() }
    }
    $pins = Join-Path (Get-KbpRepositoryRoot) 'artifacts\references'
    $installedDir = Join-Path (Get-KbpGamePath) 'Kingmaker_Data\Managed\UnityModManager'
    # Private loader folders assembled from the official packages (and a read-only copy of the owner's installation).
    $folders = [ordered]@{}
    foreach ($spec in @(@('0.31.1', 'unitymodmanager.0.31.1.nupkg', 'lib.harmony.2.3.1.1.nupkg'), @('0.32.4', 'unitymodmanager.0.32.4.nupkg', 'lib.harmony.2.3.6.nupkg'), @('0.33.4', 'unitymodmanager.0.33.4.nupkg', 'lib.harmony.2.4.3.nupkg'))) {
        $dir = Join-Path $temp ('loader ' + $spec[0]); New-Item -ItemType Directory -Path $dir | Out-Null
        Expand-PackageEntry $spec[1] 'lib/net35/UnityModManager.dll' (Join-Path $dir 'UnityModManager.dll')
        Expand-PackageEntry $spec[2] 'lib/net48/0Harmony.dll' (Join-Path $dir '0Harmony.dll')
        Copy-Item -LiteralPath (Join-Path $pins '0Harmony12.dll') -Destination $dir
        $folders[$spec[0]] = $dir
    }
    $ownerDir = Join-Path $temp 'loader owner-0.33.0'; New-Item -ItemType Directory -Path $ownerDir | Out-Null
    foreach ($file in @('UnityModManager.dll', '0Harmony12.dll', '0Harmony.dll')) { Copy-Item -LiteralPath (Join-Path $installedDir $file) -Destination $ownerDir }
    $folders['owner-0.33.0'] = $ownerDir
    $mismatchDir = Join-Path $temp 'loader 0.33.4 with harmony 2.3.6'; New-Item -ItemType Directory -Path $mismatchDir | Out-Null
    Copy-Item -LiteralPath (Join-Path $folders['0.33.4'] 'UnityModManager.dll'), (Join-Path $pins '0Harmony12.dll') -Destination $mismatchDir
    Expand-PackageEntry 'lib.harmony.2.3.6.nupkg' 'lib/net48/0Harmony.dll' (Join-Path $mismatchDir '0Harmony.dll')

    # 1. Each measured row is selectable and matches its own private loader folder.
    $rows = [ordered]@{}
    foreach ($id in $folders.Keys) {
        $rows[$id] = Get-KbpLoaderIdentity $id
        Invoke-Expecting "preflight-row-$id-matching-loader-accepted" { Assert-KbpInstalledLoaderIdentity -Loader $rows[$id] -UnityModManagerDir $folders[$id] } '' | Out-Null
    }
    Invoke-Expecting 'preflight-cross-row-0.31.1-files-under-0.32.4-row-refused' { Assert-KbpInstalledLoaderIdentity -Loader $rows['0.32.4'] -UnityModManagerDir $folders['0.31.1'] } 'unityModManager expected=0\.32\.4\.0' | Out-Null
    Invoke-Expecting 'preflight-harmony2-only-mismatch-refused' { Assert-KbpInstalledLoaderIdentity -Loader $rows['0.33.4'] -UnityModManagerDir $mismatchDir } '^Installed loader does not match loader row 0\.33\.4: harmony2 expected=2\.4\.3\.0' | Out-Null
    Invoke-Expecting 'loader-row-unknown-id-refused' { Get-KbpLoaderIdentity 'unknown-row' } 'does not belong to the set' | Out-Null

    # 2. The shared installation preflight (launcher, Deploy-Local, profile test) against a private game-directory fixture.
    $game = Join-Path $temp 'private game'
    New-Item -ItemType Directory -Path (Join-Path $game 'Kingmaker_Data\Managed\UnityModManager'), (Join-Path $game 'Mods') -Force | Out-Null
    $profile = Get-KbpCompatibilityProfile 'native-only'
    foreach ($file in @('UnityModManager.dll', '0Harmony12.dll', '0Harmony.dll')) { Copy-Item -LiteralPath (Join-Path $folders['0.31.1'] $file) -Destination (Join-Path $game 'Kingmaker_Data\Managed\UnityModManager') }
    Invoke-Expecting 'installation-preflight-matching-row-accepted' { Assert-KbpInstallationPreflight -Profile $profile -Loader $rows['0.31.1'] -KingmakerInstallDir $game } '' | Out-Null
    Copy-Item -LiteralPath (Join-Path $mismatchDir '0Harmony.dll') -Destination (Join-Path $game 'Kingmaker_Data\Managed\UnityModManager\0Harmony.dll') -Force
    Invoke-Expecting 'installation-preflight-harmony2-only-mismatch-refused' { Assert-KbpInstallationPreflight -Profile $profile -Loader $rows['0.31.1'] -KingmakerInstallDir $game } 'harmony2 expected=2\.3\.1\.1' | Out-Null
    $profileTest = Join-Path $PSScriptRoot 'compatibility\Test-KingmakerCompatibilityProfile.ps1'
    $ErrorActionPreference = 'Continue'
    $out = (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $profileTest -ProfileId native-only -KingmakerInstallDir $game -LoaderId 0.31.1 2>&1 | ForEach-Object { "$_" }) -join ' '
    $ErrorActionPreference = 'Stop'
    Add-Case 'profile-test-entry-point-refuses-harmony2-only-mismatch' ($LASTEXITCODE -ne 0 -and $out -match 'harmony2 expected=2\.3\.1\.1') ("exit $LASTEXITCODE")
    Copy-Item -LiteralPath (Join-Path $folders['0.31.1'] '0Harmony.dll') -Destination (Join-Path $game 'Kingmaker_Data\Managed\UnityModManager\0Harmony.dll') -Force
    $ErrorActionPreference = 'Continue'
    $out = (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $profileTest -ProfileId native-only -KingmakerInstallDir $game -LoaderId 0.31.1 2>&1 | ForEach-Object { "$_" }) -join ' '
    $ErrorActionPreference = 'Stop'
    Add-Case 'profile-test-entry-point-accepts-matching-row' ($LASTEXITCODE -eq 0 -and $out -match 'PASS=1 FAIL=0 .*loader=0\.31\.1') ("exit ${LASTEXITCODE}: $out")
    $deploy = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Deploy-Local.ps1'))
    $launcher = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1'))
    $ok = $deploy.IndexOf('Assert-KbpInstallationPreflight') -gt 0 -and $deploy.IndexOf('Assert-KbpInstallationPreflight') -lt $deploy.IndexOf('$PSCmdlet.ShouldProcess') -and
        $launcher.IndexOf('Assert-KbpInstallationPreflight') -gt 0 -and $launcher.IndexOf('Assert-KbpInstallationPreflight') -lt $launcher.IndexOf('New-KbpRuntimeRequest -RunId')
    Add-Case 'deploy-and-launcher-run-the-shared-preflight-before-staging' $ok 'Deploy-Local and the launcher call Assert-KbpInstallationPreflight (with the selected row and the caller''s KingmakerInstallDir) before any staging or request'

    # 3. Result admission against the row the request carried.
    $manifest = [pscustomobject]@{ version = '9.9.9'; commit = ('c' * 40); packageSha256 = ('a' * 64); dllSha256 = ('d' * 64) }
    function New-Request($Row) { return (New-KbpRuntimeRequest -RunId 'loader-identity' -EvidenceDirectory 'evidence' -BuildManifest $manifest -TimeoutSeconds 60 -ExitAfterCompletion $true -ExpectedLoader $Row -Scenario 'ui-root-smoke' | ConvertTo-Json -Depth 8 | ConvertFrom-Json) }
    function New-Result($Row) {
        return [pscustomobject]@{
            schemaVersion = 1; runId = 'loader-identity'; scenario = 'ui-root-smoke'; profileId = 'native-only'
            loadedModId = 'KingmakerBuffPlanner'; loadedModVersion = '9.9.9'; commit = ('c' * 40)
            packageSha256 = ('a' * 64); assemblySha256 = ('d' * 64); gameVersion = '2.1.7'
            gameExecutableSha256 = '94a779c5423199fcb0470bd89884a3b3875dee2072eb1a7b1d7bc8e67accb1a1'
            ummVersion = $Row.unityModManager.version; ummSha256 = $Row.unityModManager.sha256
            harmonyVersion = $Row.harmony12.version; harmonySha256 = $Row.harmony12.sha256
            harmony2Version = $Row.harmony2.version; harmony2Sha256 = $Row.harmony2.sha256
            ummInstalledSha256 = $Row.unityModManager.sha256; harmonyInstalledSha256 = $Row.harmony12.sha256; harmony2InstalledSha256 = $Row.harmony2.sha256
            status = 'BLOCKED'; stage = 'campaign-ui-unavailable'; exceptionSummary = 'Campaign UI is required for this scenario.'
            assertions = @(1, 2, 3, 4, 5)
        }
    }
    foreach ($id in $rows.Keys) {
        Invoke-Expecting "result-row-$id-admitted" { Assert-KbpRuntimeResult -Result (New-Result $rows[$id]) -Request (New-Request $rows[$id]) -BuildManifest $manifest } '' | Out-Null
    }
    Invoke-Expecting 'result-cross-row-refused' { Assert-KbpRuntimeResult -Result (New-Result $rows['0.31.1']) -Request (New-Request $rows['owner-0.33.0']) -BuildManifest $manifest } 'ummVersion expected=0\.33\.0\.0 observed=0\.31\.1\.0' | Out-Null
    $r = New-Result $rows['0.33.4']; $r.harmony2Version = '2.3.6.0'; $r.harmony2Sha256 = $rows['0.32.4'].harmony2.sha256; $r.harmony2InstalledSha256 = $rows['0.32.4'].harmony2.sha256
    Invoke-Expecting 'result-harmony2-only-mismatch-refused' { Assert-KbpRuntimeResult -Result $r -Request (New-Request $rows['0.33.4']) -BuildManifest $manifest } '^Runtime result identity/hash mismatch: harmony2Version expected=2\.4\.3\.0' | Out-Null
    $r = New-Result $rows['0.31.1']; $r.ummInstalledSha256 = $rows['0.32.4'].unityModManager.sha256
    Invoke-Expecting 'result-loaded-versus-installed-mismatch-refused' { Assert-KbpRuntimeResult -Result $r -Request (New-Request $rows['0.31.1']) -BuildManifest $manifest } '^Runtime result identity/hash mismatch: ummInstalledSha256 expected=' | Out-Null
    $r = New-Result $rows['0.31.1']; $r.PSObject.Properties.Remove('harmony2Version'); $r.PSObject.Properties.Remove('harmony2Sha256')
    Invoke-Expecting 'result-earlier-shape-without-harmony2-refused' { Assert-KbpRuntimeResult -Result $r -Request (New-Request $rows['0.31.1']) -BuildManifest $manifest } 'harmony2Version expected=2\.3\.1\.1 observed=\(absent\)' | Out-Null
    $noLoader = New-KbpRuntimeRequest -RunId 'loader-identity' -EvidenceDirectory 'evidence' -BuildManifest $manifest -TimeoutSeconds 60 -ExitAfterCompletion $true -Scenario 'ui-root-smoke' | ConvertTo-Json -Depth 8 | ConvertFrom-Json
    Invoke-Expecting 'result-for-request-without-loader-row-refused' { Assert-KbpRuntimeResult -Result (New-Result $rows['0.31.1']) -Request $noLoader -BuildManifest $manifest } 'carries no expected loader row' | Out-Null
    $request = New-Request $rows['0.32.4']
    Add-Case 'request-carries-selected-row' ($request.expectedLoader.loaderId -ceq '0.32.4' -and $request.expectedLoader.harmony2.sha256 -ceq $rows['0.32.4'].harmony2.sha256) "expectedLoader $($request.expectedLoader.loaderId)"

    # 4. The compatibility identity (allowances, earlier-pass admission) changes with the selected row only.
    $d1 = Get-KbpCompatibilityIdentityDigest $profile -Loader $rows['0.31.1']
    $d2 = Get-KbpCompatibilityIdentityDigest $profile -Loader $rows['owner-0.33.0']
    Add-Case 'compatibility-identity-binds-loader-row' ($d1 -cne $d2 -and $d1 -cmatch '^[0-9a-f]{64}$') "native-only + 0.31.1 $d1; + owner-0.33.0 $d2"

    $failed = @($cases | Where-Object { -not $_.passed })
    $summary = [ordered]@{ schema = 'kbp.runtime-loader-identity-tests.v2'; rows = @($rows.Keys); passed = $cases.Count - $failed.Count; total = $cases.Count; cases = @($cases) }
    if ($ReportPath) { $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
    Write-Host ("RESULT {0}/{1} runtime loader identity checks passed" -f $summary.passed, $summary.total)
    if ($failed.Count -gt 0) { throw "$($failed.Count) runtime loader identity check(s) failed." }
}
finally { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
