[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$root = Get-KbpRepositoryRoot
$version = Get-KbpVersion
$package = Join-Path $root "artifacts\local-runtime\$version\KingmakerBuffPlanner-$version-local-runtime.zip"
if (-not (Test-Path -LiteralPath $package -PathType Leaf)) { throw 'Validated local package is missing.' }
$game = Get-KbpGamePath
$targets = @(
    $script:KbpRuntimeStateRoot,
    $script:KbpRuntimeStagingRoot,
    $script:KbpRuntimeBackupRoot,
    $script:KbpRuntimeEvidenceRoot,
    (Join-Path $game 'Mods'))
Invoke-KbpLivePurityWindow -Label 'Deployment WhatIf' -Targets $targets -Action {
    & (Join-Path $PSScriptRoot 'Deploy-Local.ps1') -PackagePath $package `
        -RunId 'whatif-source-proof' -WhatIf -Confirm:$false
}
Write-Host 'Deployment WhatIf purity: PASS=5 FAIL=0'
