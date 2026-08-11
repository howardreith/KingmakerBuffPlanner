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
$before = @{}
foreach ($target in $targets) {
    $before[$target] = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
}
& (Join-Path $PSScriptRoot 'Deploy-Local.ps1') -PackagePath $package `
    -RunId 'whatif-source-proof' -WhatIf -Confirm:$false
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
if (@($changed).Count -ne 0) { throw "Deployment WhatIf changed: $($changed -join ', ')" }
Write-Host 'Deployment WhatIf purity: PASS=5 FAIL=0'
