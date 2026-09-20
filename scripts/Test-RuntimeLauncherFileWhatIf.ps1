[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

# Regression for the powershell.exe -File defect class: a top-level
# script's $PSCmdlet cannot service ShouldProcess when confirmation must
# be evaluated (NullReferenceException; works under -Command). It crashed
# the runtime launcher after its read-only preflights, before staging.

# Layer 1: the defect class and the repair pattern. A minimal top-level
# script using the launcher's exact guard must complete under -File.
$pattern = Join-Path $env:TEMP ('kbp-sp-pattern-' + [Guid]::NewGuid().ToString('N') + '.ps1')
@'
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param()
$ErrorActionPreference = 'Stop'
$shouldProceed = $true
try {
    $shouldProceed = $PSCmdlet.ShouldProcess('pattern-target', 'pattern-action')
}
catch [NullReferenceException] {
    $shouldProceed = -not [bool]$WhatIfPreference
}
if ($shouldProceed) { Write-Host 'PATTERN=proceeded' } else { Write-Host 'PATTERN=no-op' }
'@ | Set-Content -LiteralPath $pattern -Encoding ASCII
try {
    $patternOutput = (& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $pattern 2>&1) -join ' '
    if ($LASTEXITCODE -ne 0 -or $patternOutput -notlike '*PATTERN=*') {
        throw "ShouldProcess -File pattern check failed ($LASTEXITCODE).: $patternOutput"
    }
}
finally {
    Remove-Item -LiteralPath $pattern -Force -ErrorAction SilentlyContinue
}

# Layer 2: the launcher carries the repaired guard (narrow catch + WhatIf
# contract fallback); a bare $PSCmdlet.ShouldProcess top-level call is the
# known-crashing form under -File.
$launcherSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeTest.ps1') -Raw
if ($launcherSource -notmatch 'catch \[NullReferenceException\]') {
    throw 'Runtime launcher lost the ShouldProcess -File fallback.'
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
Write-Host 'Launcher -File WhatIf purity: PASS=3 FAIL=0'
