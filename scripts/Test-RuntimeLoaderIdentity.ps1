[CmdletBinding()]
param([string]$ReportPath)
# Offline tests of the runtime loader identity (no game launch, no deployment, no write to the installation):
# compatibility-profile contract, installed-loader preflight on a private copy of the configured loader files, and
# runtime-result admission (Assert-KbpRuntimeResult) for UnityModManager, 0Harmony12 and the bound Harmony2 module.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
. (Join-Path $PSScriptRoot 'compatibility\CompatibilityProfile.Common.ps1')

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

$temp = Join-Path ([IO.Path]::GetTempPath()) ('kbp-loader-identity-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    # 1. Every available profile carries a verified loader identity of the right shape; a malformed one is refused.
    $profileIds = @(Get-ChildItem -LiteralPath (Join-Path (Get-KbpRepositoryRoot) 'compatibility\profiles') -Filter '*.json' | ForEach-Object BaseName | Sort-Object)
    foreach ($id in $profileIds) {
        $p = Get-KbpCompatibilityProfile $id
        $ok = [string]$p.status -cne 'available' -or ($p.PSObject.Properties.Name -contains 'loader')
        Add-Case "profile-$id-loader-identity" $ok $(if ([string]$p.status -ceq 'available') { "UMM $($p.loader.unityModManager.version) / Harmony12 $($p.loader.harmony12.version) / Harmony2 $($p.loader.harmony2.version)" } else { "status $($p.status)" })
    }
    $profile = Get-KbpCompatibilityProfile 'native-only'
    $bad = Copy-Object $profile.loader; $bad.harmony2.sha256 = 'not-a-digest'
    Invoke-Expecting 'loader-contract-malformed-harmony2-row-refused' { Assert-KbpLoaderIdentityContract $bad 'test' } 'row is invalid: test harmony2' | Out-Null
    $bad = Copy-Object $profile.loader; $bad.PSObject.Properties.Remove('harmony2')
    Invoke-Expecting 'loader-contract-missing-harmony2-refused' { Assert-KbpLoaderIdentityContract $bad 'test' } 'must name exactly' | Out-Null

    # 2. Preflight on a private copy of the configured loader files (read only from the installation).
    $installedDir = Join-Path (Get-KbpGamePath) 'Kingmaker_Data\Managed\UnityModManager'
    $copy = Join-Path $temp 'loader copy'
    New-Item -ItemType Directory -Path $copy | Out-Null
    foreach ($file in @('UnityModManager.dll', '0Harmony12.dll', '0Harmony.dll')) { Copy-Item -LiteralPath (Join-Path $installedDir $file) -Destination $copy }
    $observed = Invoke-Expecting 'preflight-matching-installation-accepted' { Assert-KbpInstalledLoaderIdentity -Profile $profile -UnityModManagerDir $copy } ''
    $pins = Join-Path (Get-KbpRepositoryRoot) 'artifacts\references'
    $otherUmm = Copy-Object $profile
    $otherUmm.loader.unityModManager.version = '0.31.1.0'
    $otherUmm.loader.unityModManager.sha256 = Get-KbpSha256 (Join-Path $pins 'UnityModManager.dll')
    Invoke-Expecting 'preflight-different-umm-row-refused' { Assert-KbpInstalledLoaderIdentity -Profile $otherUmm -UnityModManagerDir $copy } 'unityModManager expected=0\.31\.1\.0' | Out-Null
    $otherHarmony2 = Copy-Object $profile
    $otherHarmony2.loader.harmony2.sha256 = ('0' * 64)
    Invoke-Expecting 'preflight-harmony2-only-mismatch-refused' { Assert-KbpInstalledLoaderIdentity -Profile $otherHarmony2 -UnityModManagerDir $copy } '^Installed loader does not match .*: harmony2 expected=' | Out-Null
    $swapped = Join-Path $temp 'loader swapped'
    New-Item -ItemType Directory -Path $swapped | Out-Null
    Copy-Item -Path (Join-Path $copy '*') -Destination $swapped
    Copy-Item -LiteralPath (Join-Path $pins 'UnityModManager.dll') -Destination (Join-Path $swapped 'UnityModManager.dll') -Force
    Invoke-Expecting 'preflight-installed-file-swapped-refused' { Assert-KbpInstalledLoaderIdentity -Profile $profile -UnityModManagerDir $swapped } 'unityModManager expected=' | Out-Null
    Remove-Item -LiteralPath (Join-Path $swapped '0Harmony.dll')
    Copy-Item -LiteralPath (Join-Path $copy 'UnityModManager.dll') -Destination (Join-Path $swapped 'UnityModManager.dll') -Force
    Invoke-Expecting 'preflight-missing-harmony2-refused' { Assert-KbpInstalledLoaderIdentity -Profile $profile -UnityModManagerDir $swapped } 'harmony2 missing' | Out-Null

    # 3. Result admission: a result is admitted only when its loader rows equal the expected identity.
    $manifest = [pscustomobject]@{ version = '9.9.9'; commit = ('c' * 40); packageSha256 = ('a' * 64); dllSha256 = ('d' * 64) }
    $request = [pscustomobject]@{ runId = 'loader-identity'; scenario = 'ui-root-smoke'; profileId = 'native-only'; expectedOptionalMods = @() }
    $loader = $profile.loader
    $template = [pscustomobject]@{
        schemaVersion = 1; runId = 'loader-identity'; scenario = 'ui-root-smoke'; profileId = 'native-only'
        loadedModId = 'KingmakerBuffPlanner'; loadedModVersion = '9.9.9'; commit = ('c' * 40)
        packageSha256 = ('a' * 64); assemblySha256 = ('d' * 64); gameVersion = '2.1.7'
        gameExecutableSha256 = '94a779c5423199fcb0470bd89884a3b3875dee2072eb1a7b1d7bc8e67accb1a1'
        ummVersion = $loader.unityModManager.version; ummSha256 = $loader.unityModManager.sha256
        harmonyVersion = $loader.harmony12.version; harmonySha256 = $loader.harmony12.sha256
        harmony2Version = $loader.harmony2.version; harmony2Sha256 = $loader.harmony2.sha256
        status = 'BLOCKED'; stage = 'campaign-ui-unavailable'; exceptionSummary = 'Campaign UI is required for this scenario.'
        assertions = @(1, 2, 3, 4, 5)
    }
    Invoke-Expecting 'result-matching-loader-admitted' { Assert-KbpRuntimeResult -Result $template -Request $request -BuildManifest $manifest -ExpectedLoader $loader } '' | Out-Null
    $r = Copy-Object $template; $r.ummVersion = '0.31.1.0'; $r.ummSha256 = Get-KbpSha256 (Join-Path $pins 'UnityModManager.dll')
    Invoke-Expecting 'result-different-umm-row-refused' { Assert-KbpRuntimeResult -Result $r -Request $request -BuildManifest $manifest -ExpectedLoader $loader } 'ummVersion expected=0\.33\.0\.0 observed=0\.31\.1\.0' | Out-Null
    $r = Copy-Object $template; $r.harmony2Sha256 = ('0' * 64)
    Invoke-Expecting 'result-harmony2-only-mismatch-refused' { Assert-KbpRuntimeResult -Result $r -Request $request -BuildManifest $manifest -ExpectedLoader $loader } '^Runtime result identity/hash mismatch: harmony2Sha256 expected=' | Out-Null
    $r = Copy-Object $template; $r.harmony2Version = 'not-loaded'; $r.harmony2Sha256 = 'not-loaded'
    Invoke-Expecting 'result-harmony2-not-loaded-refused' { Assert-KbpRuntimeResult -Result $r -Request $request -BuildManifest $manifest -ExpectedLoader $loader } 'harmony2Version expected=' | Out-Null
    $r = Copy-Object $template; $r.PSObject.Properties.Remove('harmony2Version'); $r.PSObject.Properties.Remove('harmony2Sha256')
    Invoke-Expecting 'result-earlier-shape-without-harmony2-refused' { Assert-KbpRuntimeResult -Result $r -Request $request -BuildManifest $manifest -ExpectedLoader $loader } 'harmony2Version expected=2\.3\.6\.0 observed=\(absent\)' | Out-Null
    $otherProfile = Copy-Object $loader; $otherProfile.harmony2.version = '2.4.3.0'
    Invoke-Expecting 'result-under-other-expected-harmony2-refused' { Assert-KbpRuntimeResult -Result $template -Request $request -BuildManifest $manifest -ExpectedLoader $otherProfile } 'harmony2Version expected=2\.4\.3\.0' | Out-Null

    # 4. The compatibility identity (allowances, earlier-pass admission) changes when only the loader changes.
    $digest = Get-KbpCompatibilityIdentityDigest $profile
    $digestOther = Get-KbpCompatibilityIdentityDigest $otherHarmony2
    Add-Case 'compatibility-identity-binds-loader' ($digest -cne $digestOther -and $digest -cmatch '^[0-9a-f]{64}$') "native-only $digest; Harmony2-only change $digestOther"

    $failed = @($cases | Where-Object { -not $_.passed })
    $summary = [ordered]@{ schema = 'kbp.runtime-loader-identity-tests.v1'; installed_loader_observed = $observed; passed = $cases.Count - $failed.Count; total = $cases.Count; cases = @($cases) }
    if ($ReportPath) { $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
    Write-Host ("RESULT {0}/{1} runtime loader identity checks passed" -f $summary.passed, $summary.total)
    if ($failed.Count -gt 0) { throw "$($failed.Count) runtime loader identity check(s) failed." }
}
finally { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
