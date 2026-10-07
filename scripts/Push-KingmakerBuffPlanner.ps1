[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [switch] $AllowDefaultBranch
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Read-Git {
    param([string[]] $Arguments)
    $result = & git -C $RepositoryRoot @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Git guard check failed: $($Arguments[0])" }
    return (($result | ForEach-Object { [string]$_ }) -join "`n").Trim()
}
$resolvedRoot = Read-Git -Arguments @('rev-parse', '--show-toplevel')
if ([IO.Path]::GetFullPath($resolvedRoot) -ne [IO.Path]::GetFullPath($RepositoryRoot)) {
    throw 'RepositoryRoot must be the exact Git worktree root.'
}
$branch = Read-Git -Arguments @('symbolic-ref', '--quiet', '--short', 'HEAD')
# The owner explicitly named this WP1 branch. Keep the original project
# scope and add only that exact authorized name.
if ($branch -cne 'codex/kbp-interrupted-run-reload-recovery-2026-10-07' -and
    $branch -notmatch '^codex/kingmaker-buff-planner(?:[-/]|$)' -and
    -not ($AllowDefaultBranch -and $branch -ceq 'main')) {
    throw "Branch is outside the approved scope: $branch"
}
foreach ($state in @('MERGE_HEAD', 'CHERRY_PICK_HEAD', 'REVERT_HEAD', 'rebase-merge', 'rebase-apply')) {
    $statePath = Read-Git -Arguments @('rev-parse', '--git-path', $state)
    if (-not [IO.Path]::IsPathRooted($statePath)) { $statePath = Join-Path $RepositoryRoot $statePath }
    if (Test-Path -LiteralPath $statePath) { throw "Unfinished Git operation: $state" }
}
if (Read-Git -Arguments @('status', '--porcelain')) { throw 'Guarded push requires a clean worktree.' }
$origin = Read-Git -Arguments @('remote', 'get-url', 'origin')
if ($origin -cnotin @('https://github.com/howardreith/KingmakerBuffPlanner.git',
    'git@github.com:howardreith/KingmakerBuffPlanner.git')) {
    throw 'Origin is not the approved project repository.'
}
$files = @((Read-Git -Arguments @('ls-files')) -split "`n")
foreach ($file in $files) {
    if ($file -match '(?i)(^|/)(artifacts|private|saves?|\.git)(/|$)|\.(dll|pdb|zks|pem|pfx|key)$|(^|/)(GamePath\.props|\.env)$') {
        throw "Protected or generated tracked file: $file"
    }
    if ($file -match '\.(cs|ps1|py|json|xml|props|md|yml|yaml|txt|config)$') {
        $contents = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $file))
        if ($contents -match '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9]{36,}|github_pat_[A-Za-z0-9_]{60,}') {
            throw "Possible credential in tracked file: $file"
        }
    }
}
$head = Read-Git -Arguments @('rev-parse', 'HEAD')
$remote = Read-Git -Arguments @('ls-remote', '--heads', 'origin', "refs/heads/$branch")
if ($remote) {
    $remoteHead = ($remote -split '\s+')[0]
    & git -C $RepositoryRoot merge-base --is-ancestor $remoteHead $head
    if ($LASTEXITCODE -ne 0) { throw 'Remote is not an ancestor; fetch and reconcile without force.' }
}
if ($WhatIfPreference) {
    Write-Output "Guarded push WhatIf PASS: $branch at $head; no local or remote writes."
    return
}
if ($PSCmdlet.ShouldProcess("$origin refs/heads/$branch", "Fast-forward push $head")) {
    & git -C $RepositoryRoot push --set-upstream origin "HEAD:refs/heads/$branch"
    if ($LASTEXITCODE -ne 0) { throw 'Guarded push failed.' }
    $verified = Read-Git -Arguments @('ls-remote', '--heads', 'origin', "refs/heads/$branch")
    if (($verified -split '\s+')[0] -cne $head) { throw 'Remote verification failed.' }
    Write-Output "Guarded push PASS: $branch $head"
}
