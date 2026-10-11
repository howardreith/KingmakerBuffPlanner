[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$boundary = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) ('kbp-launch-arguments-' + [Guid]::NewGuid().ToString('N'))))
New-Item -ItemType Directory -Path $boundary | Out-Null
try {
    $launcher = Join-Path $boundary 'Invoke-KingmakerRuntimeTest.ps1'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1') -Destination $launcher
    # Only the external boundary is replaced. No live guards/purity proof run
    # here and this test cannot qualify a launcher for live use.
    Set-Content -LiteralPath (Join-Path $boundary 'RuntimeAutomation.Common.ps1') -Encoding ASCII -Value "throw 'TEST_EXTERNAL_BOUNDARY_REACHED'"
    $before = @(Get-ChildItem -LiteralPath $boundary -File | ForEach-Object { $_.Name + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash }) -join '|'
    $passed = 0
    foreach ($case in @(
        @{ Scenario = 'live-workspace-import'; DisplayMode = 'windowed-1920x1080'; Refuse = $true },
        @{ Scenario = 'live-workspace-import'; DisplayMode = 'owner'; Refuse = $false },
        @{ Scenario = 'live-workspace-physical'; DisplayMode = 'windowed-1920x1080'; Refuse = $false },
        @{ Scenario = 'live-workspace-qual'; DisplayMode = 'windowed-1920x1080'; Refuse = $false },
        @{ Scenario = 'LIVE-WORKSPACE-PHYSICAL'; DisplayMode = 'WINDOWED-1920X1080'; Refuse = $false },
        @{ Scenario = 'LIVE-WORKSPACE-IMPORT'; DisplayMode = 'OWNER'; Refuse = $false }
    )) {
        $observed = $null
        try { & $launcher -Scenario $case.Scenario -DisplayMode $case.DisplayMode -WhatIf }
        catch { $observed = $_ }
        if ($null -eq $observed) { throw 'Argument test crossed its external boundary.' }
        if ($case.Refuse) {
            if ($observed.FullyQualifiedErrorId -notlike 'KBP_DISPLAY_MODE_SCENARIO*') {
                throw "Expected stable display refusal code; got $($observed.FullyQualifiedErrorId)."
            }
        } elseif ($observed.Exception.Message -cne 'TEST_EXTERNAL_BOUNDARY_REACHED') {
            throw "An allowed display/scenario pair was refused: $($observed.Exception.Message)"
        }
        $after = @(Get-ChildItem -LiteralPath $boundary -File | ForEach-Object { $_.Name + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash }) -join '|'
        if ($after -cne $before -or @(Get-ChildItem -LiteralPath $boundary -Directory).Count -ne 0) {
            throw 'Argument validation mutated its bounded filesystem.'
        }
        $passed++
    }
    Write-Host "Launcher argument selection: PASS=$passed FAIL=0; bounded offline checks, no live qualification."
}
finally {
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $boundary.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe test cleanup path.' }
    Remove-Item -LiteralPath $boundary -Recurse -Force
}
