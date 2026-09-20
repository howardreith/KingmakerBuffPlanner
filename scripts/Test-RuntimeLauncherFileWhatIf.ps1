[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

# Regression: powershell.exe -File leaves a top-level script's
# $PSCmdlet.ShouldProcess unusable (NullReferenceException), which crashed
# the runtime launcher after its read-only preflights. The launcher must
# complete its -WhatIf contract cleanly under -File and mutate nothing.
$root = Get-KbpRepositoryRoot
$game = Get-KbpGamePath
$targets = @(
    $script:KbpRuntimeStateRoot,
    $script:KbpRuntimeStagingRoot,
    $script:KbpRuntimeBackupRoot,
    $script:KbpRuntimeEvidenceRoot,
    (Join-Path $game 'Mods'))
$before = @{}
foreach ($target in $targets) {
    $before[$target] = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
}
$launcher = Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1'
$output = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher `
        -Scenario 'mod-load-smoke' -WhatIf -Confirm:$false 2>&1)
if ($LASTEXITCODE -ne 0) {
    throw "Runtime launcher -File -WhatIf failed with exit code $LASTEXITCODE.: $($output -join ' ')"
}
if (-not (@($output | Where-Object { $_ -like '*Runtime WhatIf preflight PASS*' }).Count -ge 1)) {
    throw "Runtime launcher -File -WhatIf did not report the purity message.: $($output -join ' ')"
}
$changed = @()
foreach ($target in $targets) {
    $after = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
    if ($null -eq $before[$target]) {
        if ($null -ne $after) { $changed += @($target) }
    }
    elseif (-not (Test-KbpManifestEqual @($before[$target]) @($after))) {
        $changed += @($target)
    }
}
if (@($changed).Count -ne 0) { throw "Runtime launcher -File -WhatIf changed: $($changed -join ', ')" }
Write-Host 'Launcher -File WhatIf purity: PASS=1 FAIL=0'
