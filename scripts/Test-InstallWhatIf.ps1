[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$root = Get-KbpRepositoryRoot
$version = Get-KbpVersion
$manifest = Join-Path $root "artifacts\release\$version\release-manifest.json"
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw 'Validated release manifest is missing.'
}
$game = Get-KbpGamePath
$targets = @($script:KbpRuntimeStateRoot, $script:KbpRuntimeStagingRoot,
    $script:KbpRuntimeBackupRoot, $script:KbpRuntimeEvidenceRoot, (Join-Path $game 'Mods'))
$before = @{}
foreach ($target in $targets) {
    $before[$target] = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
}
& (Join-Path $PSScriptRoot 'Install-Local.ps1') -ReleaseManifestPath $manifest `
    -InstallId 'whatif-install-proof' -WhatIf -Confirm:$false
foreach ($target in $targets) {
    $after = if (Test-Path -LiteralPath $target -PathType Container) {
        @(Get-KbpDirectoryManifest $target)
    } else { $null }
    if (($null -eq $before[$target] -and $null -ne $after) -or
        ($null -ne $before[$target] -and -not (Test-KbpManifestEqual @($before[$target]) @($after)))) {
        throw "Install WhatIf changed: $target"
    }
}
Write-Host 'Install WhatIf purity: PASS=5 FAIL=0'
