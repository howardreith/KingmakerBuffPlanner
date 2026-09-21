[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

# Regression for the powershell.exe -File defect class: a top-level
# script's $PSCmdlet cannot service ShouldProcess when confirmation must
# be evaluated (NullReferenceException; works under -Command). It crashed
# the runtime launcher after its read-only preflights. Safety contract:
# a decision that cannot be evaluated is a REFUSED decision — the
# launcher must abort with an error and mutate nothing.

# Layer 1: the defect class and the fail-closed repair pattern. A minimal
# top-level script using the launcher's exact guard must REFUSE (non-zero
# exit, refusal marker, never proceed) under -File.
$pattern = Join-Path $env:TEMP ('kbp-sp-pattern-' + [Guid]::NewGuid().ToString('N') + '.ps1')
@'
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param()
$ErrorActionPreference = 'Stop'
$shouldProceed = $false
$decisionFailure = $null
try {
    $shouldProceed = $PSCmdlet.ShouldProcess('pattern-target', 'pattern-action')
}
catch [NullReferenceException] {
    $decisionFailure = $_.Exception
}
if ($null -ne $decisionFailure) {
    throw ("Decision evaluation failed; refusing: " + $decisionFailure.Message)
}
if ($shouldProceed) { Write-Host 'PATTERN=proceeded' } else { Write-Host 'PATTERN=no-op' }
'@ | Set-Content -LiteralPath $pattern -Encoding ASCII
try {
    $patternOutput = (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $pattern 2>&1) -join ' '
    if ($LASTEXITCODE -eq 0 -or $patternOutput -like '*PATTERN=proceeded*') {
        throw "ShouldProcess -File pattern check must refuse; got exit $LASTEXITCODE.: $patternOutput"
    }
    if ($patternOutput -notlike '*refusing*') {
        throw "ShouldProcess -File pattern check lacks refusal propagation.: $patternOutput"
    }
}
finally {
    Remove-Item -LiteralPath $pattern -Force -ErrorAction SilentlyContinue
}

# Layer 2: the launcher carries the fail-closed guard (narrow catch +
# refusal with error propagation); neither a bare call nor a
# WhatIf-derived fallback is acceptable.
$launcherSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1') -Raw
if ($launcherSource -notmatch 'catch \[NullReferenceException\]' -or
    $launcherSource -notmatch 'decisionFailure' -or
    $launcherSource -match 'shouldProceed = -not') {
    throw 'Runtime launcher lost the fail-closed ShouldProcess decision guard.'
}

# Layer 3: the launcher completes its -WhatIf contract under -File and
# mutates nothing. The child writes WhatIf notices to stderr; under Stop
# PS 5.1 turns the first redirected stderr line into a terminating error.
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
$ErrorActionPreference = 'Continue'
$output = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher `
        -Scenario 'mod-load-smoke' -WhatIf 2>&1)
$childExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($childExit -ne 0) {
    throw "Runtime launcher -File -WhatIf failed with exit code $childExit.: $($output -join ' ')"
}
if (-not (@($output | Where-Object { "$_" -like '*Runtime WhatIf preflight PASS*' }).Count -ge 1)) {
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

# Layer 4 (decision-evaluation failure sentinel): a REAL run under -File
# (no -WhatIf, so the confirmation decision must be evaluated) must be
# refused with a non-zero exit and must mutate nothing — the exact
# mutation path a naive fallback would have enabled.
$beforeReal = @{}
foreach ($target in $targets) {
    $beforeReal[$target] = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
}
$ErrorActionPreference = 'Continue'
$realOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher `
        -Scenario 'mod-load-smoke' 2>&1)
$realExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($realExit -eq 0) {
    throw "Runtime launcher -File real run must refuse (decision cannot be evaluated); it exited 0.: $($realOutput -join ' ')"
}
if (-not (@($realOutput | Where-Object { "$_" -like '*decision could not be evaluated*' }).Count -ge 1)) {
    throw "Runtime launcher -File real run lacks the decision refusal message.: $($realOutput -join ' ')"
}
$changedReal = @()
foreach ($target in $targets) {
    $after = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
    if ($null -eq $beforeReal[$target]) {
        if ($null -ne $after) { $changedReal += @($target) }
    }
    elseif (-not (Test-KbpManifestEqual @($beforeReal[$target]) @($after))) {
        $changedReal += @($target)
    }
}
if (@($changedReal).Count -ne 0) { throw "Runtime launcher -File refused run changed: $($changedReal -join ', ')" }
Write-Host 'Launcher -File WhatIf purity: PASS=4 FAIL=0'
