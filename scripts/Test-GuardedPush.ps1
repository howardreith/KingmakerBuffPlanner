[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
$lab = Split-Path -Parent (Split-Path -Parent $root)
$helper = Join-Path $lab 'codex-policy\Push-KingmakerBuffPlanner.ps1'
if (-not (Test-Path -LiteralPath $helper -PathType Leaf)) {
    throw "Guarded push helper is missing: $helper"
}
$source = Get-Content -LiteralPath $helper -Raw
$required = @(
    'symbolic-ref', 'codex/kingmaker-buff-planner', 'MERGE_HEAD',
    'get-url', 'ls-files', 'PRIVATE KEY', 'git -C', 'push', 'ls-remote')
foreach ($marker in $required) {
    if ($source.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Guarded push helper is missing required guard marker: $marker"
    }
}
$beforeHead = (& git -C $root rev-parse HEAD).Trim()
$beforeStatus = @(& git -C $root status --porcelain)
$output = @(& $helper -WhatIf -Confirm:$false)
$afterHead = (& git -C $root rev-parse HEAD).Trim()
$afterStatus = @(& git -C $root status --porcelain)
if ($beforeHead -cne $afterHead -or ($beforeStatus -join "`n") -cne ($afterStatus -join "`n")) {
    throw 'Guarded push WhatIf mutated repository state.'
}
if (($output -join "`n") -notmatch 'Guarded push WhatIf PASS') {
    throw 'Guarded push WhatIf did not report PASS.'
}
Write-Host 'Guarded push helper: PASS=6 FAIL=0'
