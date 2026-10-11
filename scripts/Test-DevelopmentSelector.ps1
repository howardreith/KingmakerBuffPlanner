[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$boundary = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) ('kbp-development-selector-' + [Guid]::NewGuid().ToString('N'))))
$priorTrace = $env:KBP_WORKFLOW_TEST_TRACE
$priorGame = $env:KBP_TEST_GAME_PATH
$hadGame = Test-Path Env:\KBP_TEST_GAME_PATH
New-Item -ItemType Directory -Path (Join-Path $boundary 'scripts') -Force | Out-Null
try {
    $scripts = Join-Path $boundary 'scripts'
    $trace = Join-Path $boundary 'calls.txt'
    $env:KBP_WORKFLOW_TEST_TRACE = $trace
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Test-Development.ps1') -Destination $scripts
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Build.ps1') -Destination $scripts
    @'
function Get-KbpRepositoryRoot { Split-Path -Parent $PSScriptRoot }
function Get-KbpGamePath { Join-Path (Get-KbpRepositoryRoot) 'fake-game' }
function Get-KbpMsBuild { Join-Path $PSScriptRoot 'fake-msbuild.ps1' }
function Get-KbpSha256($Path) { (Get-FileHash -LiteralPath $Path).Hash }
'@ | Set-Content -LiteralPath (Join-Path $scripts 'Common.ps1') -Encoding ASCII
    "Add-Content -LiteralPath `$env:KBP_WORKFLOW_TEST_TRACE -Value 'source'" | Set-Content -LiteralPath (Join-Path $scripts 'Validate-Source.ps1')
    @'
Add-Content -LiteralPath $env:KBP_WORKFLOW_TEST_TRACE -Value ('compile:' + $args[0])
$root = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path (Join-Path $root 'artifacts/build/Release') -Force | Out-Null
Set-Content -LiteralPath (Join-Path $root 'artifacts/build/Release/KingmakerBuffPlanner.dll') -Value 'fixture-only'
$global:LASTEXITCODE = 0
'@ | Set-Content -LiteralPath (Join-Path $scripts 'fake-msbuild.ps1') -Encoding ASCII
    "Add-Content -LiteralPath `$env:KBP_WORKFLOW_TEST_TRACE -Value 'reader'" | Set-Content -LiteralPath (Join-Path $scripts 'Test-ProblemNavigationEvidence.ps1')
    $runner = Join-Path $boundary 'artifacts/tests/KingmakerBuffPlanner.Tests.exe'
    New-Item -ItemType Directory -Path (Split-Path -Parent $runner) -Force | Out-Null
    Add-Type -OutputAssembly $runner -OutputType ConsoleApplication -TypeDefinition @'
using System;
using System.IO;
public static class SelectorFixture {
  public static int Main(string[] args) {
    File.AppendAllText(Environment.GetEnvironmentVariable("KBP_WORKFLOW_TEST_TRACE"), "runner:" + String.Join(",", args) + Environment.NewLine);
    if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH"))) return 2;
    int count = Array.IndexOf(args, "no-match") >= 0 ? 0 : args.Length - 1;
    int failed = Array.IndexOf(args, "fail") >= 0 ? 1 : 0;
    Console.WriteLine("Protocol tests: PASS=" + count + " FAIL=" + failed);
    return failed == 0 ? 0 : 1;
  }
}
'@
    & git -C $boundary init --quiet
    & git -C $boundary -c user.name=WorkflowFixture -c user.email=fixture@example.invalid commit --allow-empty -qm fixture
    if ($LASTEXITCODE -ne 0) { throw 'Temporary Git fixture failed.' }
    $selector = Join-Path $scripts 'Test-Development.ps1'
    $passed = 0
    $refused = $false
    try { & $selector -Only ' , ; ' } catch { $refused = $true }
    if (-not $refused -or (Test-Path -LiteralPath $trace)) { throw 'Empty selection was not refused before work.' }
    $passed++
    $env:KBP_TEST_GAME_PATH = 'prior-value'
    & $selector -Only 'alpha,beta;alpha' -Check ProblemNavigationEvidence
    $calls = @(Get-Content -LiteralPath $trace)
    if (@($calls | Where-Object { $_ -like 'compile:*' }).Count -ne 2 -or
        @($calls | Where-Object { $_ -like 'compile:*.sln' }).Count -ne 1 -or
        @($calls | Where-Object { $_ -like 'compile:*.csproj' }).Count -ne 1 -or
        @($calls | Where-Object { $_ -ceq 'runner:--only,alpha,beta' }).Count -ne 1 -or
        @($calls | Where-Object { $_ -ceq 'reader' }).Count -ne 1 -or
        $env:KBP_TEST_GAME_PATH -cne 'prior-value') { throw 'The executed nested call graph or environment restoration is wrong.' }
    $passed++
    foreach ($prefix in @('no-match', 'fail')) {
        $refused = $false
        try { & $selector -Only $prefix } catch { $refused = $true }
        if (-not $refused -or $env:KBP_TEST_GAME_PATH -cne 'prior-value') { throw "Selection $prefix did not fail with environment restoration." }
        $passed++
    }
    Remove-Item Env:\KBP_TEST_GAME_PATH
    & $selector -Only alpha
    if (Test-Path Env:\KBP_TEST_GAME_PATH) { throw 'An initially unset game-path variable was left behind.' }
    $passed++
    $refused = $false
    try { & $selector -Only alpha -Check PublishRelease } catch { $refused = $true }
    if (-not $refused) { throw 'The selector accepted a publisher/live check.' }
    $passed++
    Write-Host "Development selector: PASS=$passed FAIL=0; nested calls in an isolated fixture, no live/publisher qualification."
}
finally {
    $env:KBP_WORKFLOW_TEST_TRACE = $priorTrace
    if ($hadGame) { $env:KBP_TEST_GAME_PATH = $priorGame }
    else { Remove-Item Env:\KBP_TEST_GAME_PATH -ErrorAction SilentlyContinue }
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $boundary.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe cleanup path.' }
    Remove-Item -LiteralPath $boundary -Recurse -Force
}
