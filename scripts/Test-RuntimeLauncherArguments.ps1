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
        @{ Name = 'unsupported-display'; Args = @{ Scenario = 'live-workspace-import'; DisplayMode = 'windowed-1920x1080' }; ErrorId = 'KBP_DISPLAY_MODE_SCENARIO' },
        @{ Name = 'owner-display'; Args = @{ Scenario = 'live-workspace-import'; DisplayMode = 'owner' }; ErrorId = $null },
        @{ Name = 'physical-windowed'; Args = @{ Scenario = 'live-workspace-physical'; DisplayMode = 'windowed-1920x1080' }; ErrorId = $null },
        @{ Name = 'workspace-windowed'; Args = @{ Scenario = 'live-workspace-qual'; DisplayMode = 'windowed-1920x1080' }; ErrorId = $null },
        @{ Name = 'physical-uppercase'; Args = @{ Scenario = 'LIVE-WORKSPACE-PHYSICAL'; DisplayMode = 'WINDOWED-1920X1080' }; ErrorId = $null },
        @{ Name = 'owner-uppercase'; Args = @{ Scenario = 'LIVE-WORKSPACE-IMPORT'; DisplayMode = 'OWNER' }; ErrorId = $null },
        @{ Name = 'unsupported-display-uppercase'; Args = @{ Scenario = 'LIVE-WORKSPACE-IMPORT'; DisplayMode = 'WINDOWED-1280X720' }; ErrorId = 'KBP_DISPLAY_MODE_SCENARIO' },
        @{ Name = 'native-facts-owner'; Args = @{ Scenario = 'live-workspace-native-ui'; NativeUiStage = 'facts'; TimeoutSeconds = 600 }; ErrorId = $null },
        @{ Name = 'native-qualify-owner'; Args = @{ Scenario = 'live-workspace-native-ui'; NativeUiStage = 'qualify'; TimeoutSeconds = 900 }; ErrorId = $null },
        @{ Name = 'native-facts-windowed'; Args = @{ Scenario = 'live-workspace-native-ui'; NativeUiStage = 'facts'; DisplayMode = 'windowed-1280x720'; TimeoutSeconds = 600 }; ErrorId = $null },
        @{ Name = 'native-qualify-windowed'; Args = @{ Scenario = 'live-workspace-native-ui'; NativeUiStage = 'qualify'; DisplayMode = 'windowed-1920x1080'; TimeoutSeconds = 900 }; ErrorId = $null },
        @{ Name = 'native-mixed-case'; Args = @{ Scenario = 'LiVe-WoRkSpAcE-NaTiVe-Ui'; NativeUiStage = 'QuAlIfY'; DisplayMode = 'WINDOWED-1280X720'; FixtureFamily = 'aUtOmAtIoN'; CompatibilityProfileId = 'FULL-USER'; TimeoutSeconds = 600 }; ErrorId = $null },
        @{ Name = 'native-stage-required-owner'; Args = @{ Scenario = 'live-workspace-native-ui' }; ErrorId = 'KBP_NATIVE_UI_STAGE_REQUIRED' },
        @{ Name = 'native-stage-required-windowed'; Args = @{ Scenario = 'live-workspace-native-ui'; DisplayMode = 'windowed-1280x720' }; ErrorId = 'KBP_NATIVE_UI_STAGE_REQUIRED' },
        @{ Name = 'native-stage-required-uppercase'; Args = @{ Scenario = 'LIVE-WORKSPACE-NATIVE-UI'; DisplayMode = 'OWNER' }; ErrorId = 'KBP_NATIVE_UI_STAGE_REQUIRED' },
        @{ Name = 'native-stage-other-scenario'; Args = @{ Scenario = 'live-workspace-import'; NativeUiStage = 'facts' }; ErrorId = 'KBP_NATIVE_UI_STAGE_SCENARIO' },
        @{ Name = 'native-stage-other-windowed'; Args = @{ Scenario = 'live-workspace-qual'; NativeUiStage = 'qualify'; DisplayMode = 'windowed-1280x720' }; ErrorId = 'KBP_NATIVE_UI_STAGE_SCENARIO' },
        @{ Name = 'native-stage-other-uppercase'; Args = @{ Scenario = 'LIVE-WORKSPACE-PHYSICAL'; NativeUiStage = 'FACTS'; DisplayMode = 'OWNER' }; ErrorId = 'KBP_NATIVE_UI_STAGE_SCENARIO' },
        @{ Name = 'native-invalid-stage'; Args = @{ Scenario = 'live-workspace-native-ui'; NativeUiStage = 'unknown' }; ErrorId = 'ParameterArgumentValidationError' },
        @{ Name = 'native-advanced-facts'; Args = @{ Scenario = 'live-workspace-native-ui'; NativeUiStage = 'facts'; FixtureFamily = 'Advanced'; CompatibilityProfileId = 'advanced-gunslinger-0136'; TimeoutSeconds = 600 }; ErrorId = 'KBP_NATIVE_UI_FIXTURE' },
        @{ Name = 'native-advanced-qualify-uppercase'; Args = @{ Scenario = 'LIVE-WORKSPACE-NATIVE-UI'; NativeUiStage = 'QUALIFY'; FixtureFamily = 'ADVANCED'; TimeoutSeconds = 900 }; ErrorId = 'KBP_NATIVE_UI_FIXTURE' },
        @{ Name = 'native-advanced-profile-with-automation'; Args = @{ Scenario = 'live-workspace-native-ui'; NativeUiStage = 'qualify'; CompatibilityProfileId = 'advanced-gunslinger-0136'; TimeoutSeconds = 900 }; ErrorId = 'KBP_NATIVE_UI_FIXTURE' },
        @{ Name = 'native-facts-short-timeout'; Args = @{ Scenario = 'live-workspace-native-ui'; NativeUiStage = 'facts'; TimeoutSeconds = 599 }; ErrorId = 'KBP_NATIVE_UI_TIMEOUT' },
        @{ Name = 'native-qualify-default-timeout'; Args = @{ Scenario = 'live-workspace-native-ui'; NativeUiStage = 'qualify' }; ErrorId = 'KBP_NATIVE_UI_TIMEOUT' }
    )) {
        $observed = $null
        $caseArgs = $case.Args
        try { & $launcher @caseArgs -WhatIf }
        catch { $observed = $_ }
        if ($null -eq $observed) { throw 'Argument test crossed its external boundary.' }
        if ($null -ne $case.ErrorId) {
            if ($observed.FullyQualifiedErrorId -notlike ($case.ErrorId + '*')) {
                throw "Expected $($case.ErrorId) for $($case.Name); got $($observed.FullyQualifiedErrorId)."
            }
        } elseif ($observed.Exception.Message -cne 'TEST_EXTERNAL_BOUNDARY_REACHED') {
            throw "Allowed argument case $($case.Name) was refused: $($observed.Exception.Message)"
        }
        $after = @(Get-ChildItem -LiteralPath $boundary -File | ForEach-Object { $_.Name + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash }) -join '|'
        if ($after -cne $before -or @(Get-ChildItem -LiteralPath $boundary -Directory).Count -ne 0) {
            throw 'Argument validation mutated its bounded filesystem.'
        }
        Write-Host "PASS $($case.Name)"
        $passed++
    }
    Write-Host "Launcher argument selection: PASS=$passed FAIL=0; bounded offline checks, no live qualification."
}
finally {
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $boundary.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe test cleanup path.' }
    Remove-Item -LiteralPath $boundary -Recurse -Force
}
