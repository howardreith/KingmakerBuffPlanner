# Shared by the broad -File/WhatIf gate and its focused child-process checks.
# This file imports no runtime harness and cannot stage or launch the game.
function Get-KbpRuntimeLauncherAdvancedFamilyCases {
    $legacyRefusal = '*may only be loaded by the non-casting scenarios*'
    @(
        @{ Name = 'advanced-probe-select'; Expect = $legacyRefusal; Args = @('-Scenario', 'live-cast-probe-select', '-FixtureFamily', 'Advanced', '-TimeoutSeconds', '600', '-WhatIf') },
        @{ Name = 'advanced-bootstrap'; Expect = $legacyRefusal; Args = @('-Scenario', 'live-ui-bootstrap', '-FixtureFamily', 'Advanced', '-WhatIf') },
        @{ Name = 'advanced-smoke'; Expect = $legacyRefusal; Args = @('-Scenario', 'mod-load-smoke', '-FixtureFamily', 'Advanced', '-WhatIf') },
        @{ Name = 'advanced-reload'; Expect = $legacyRefusal; Args = @('-Scenario', 'live-workspace-reload', '-FixtureFamily', 'Advanced', '-TimeoutSeconds', '600', '-WhatIf') },
        @{ Name = 'advanced-import'; Expect = $legacyRefusal; Args = @('-Scenario', 'live-workspace-import', '-FixtureFamily', 'Advanced', '-WhatIf') },
        @{ Name = 'advanced-removal'; Expect = $legacyRefusal; Args = @('-Scenario', 'live-workspace-removal', '-FixtureFamily', 'Advanced', '-TimeoutSeconds', '600', '-WhatIf') },
        @{ Name = 'advanced-native-ui'; Expect = '*KBP_NATIVE_UI_FIXTURE*'; Args = @('-Scenario', 'live-workspace-native-ui', '-NativeUiStage', 'facts', '-FixtureFamily', 'Advanced', '-TimeoutSeconds', '600', '-WhatIf') }
    )
}

function Invoke-KbpRuntimeLauncherFileRefusal {
    param([string]$Launcher, [hashtable]$Case)
    $savedErrorPreference = $ErrorActionPreference
    try {
        # Windows PowerShell 5.1 redirects child stderr as ErrorRecord objects.
        # Continue captures all output and the child's actual exit status.
        $ErrorActionPreference = 'Continue'
        $caseArgs = $Case.Args
        $caseOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Launcher @caseArgs 2>&1)
        $caseExit = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $savedErrorPreference }
    if ($caseExit -eq 0 -or -not (@($caseOutput | Where-Object {
            "$_" -like $Case.Expect }).Count -ge 1)) {
        throw "Advanced family case $($Case.Name) was not refused as expected (exit $caseExit, expected $($Case.Expect)).: $($caseOutput -join ' ')"
    }
    [pscustomobject]@{ Name = $Case.Name; ExitCode = $caseExit; Output = @($caseOutput | ForEach-Object { "$_" }) }
}
