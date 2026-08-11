[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidateSet('mod-load-smoke')][string]$Scenario = 'mod-load-smoke',
    [ValidateRange(5, 1800)][int]$TimeoutSeconds = 180,
    [ValidateRange(5, 300)][int]$LaunchTimeoutSeconds = 60,
    [bool]$ExitAfterCompletion = $true,
    [string]$SteamPath = 'C:\Program Files (x86)\Steam\steam.exe'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')

$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false
$root = Get-KbpRepositoryRoot
$version = Get-KbpVersion
$package = (Resolve-Path -LiteralPath (Join-Path $root "artifacts\local-runtime\$version\KingmakerBuffPlanner-$version-local-runtime.zip")).Path
& (Join-Path $PSScriptRoot 'Validate-Source.ps1')
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $package
$gitStatus = @(& git -C $root status --porcelain)
if ($LASTEXITCODE -ne 0 -or @($gitStatus).Count -ne 0) { throw 'Runtime qualification requires a clean Git worktree.' }
$buildManifest = Read-KbpBuildManifest $package
$steamSafety = Assert-KbpSteamSafety -SteamPath $SteamPath
& (Join-Path $PSScriptRoot 'Deploy-Local.ps1') -PackagePath $package `
    -RunId 'runtime-whatif-preflight' -WhatIf -Confirm:$false
$WhatIfPreference = $requestedWhatIf
if (-not $PSCmdlet.ShouldProcess(
    'Steam App ID 640820 and exact live Mods transaction',
    "run guarded $Scenario for version $version")) {
    Write-Host 'Runtime WhatIf preflight PASS; no evidence, deployment, process, game, mod, or save mutation occurred.'
    return
}

$ConfirmPreference = 'None'
$WhatIfPreference = $false
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ') + '-mod-load-smoke'
$evidence = Join-Path $script:KbpRuntimeEvidenceRoot $runId
$transactionEntered = $false
$process = $null
New-Item -ItemType Directory -Path $evidence | Out-Null
try {
    $statePath = & (Join-Path $PSScriptRoot 'Deploy-Local.ps1') -PackagePath $package `
        -RunId $runId -Confirm:$false | Select-Object -Last 1
    $transactionEntered = $true
    $request = New-KbpRuntimeRequest -RunId $runId -EvidenceDirectory $evidence `
        -BuildManifest $buildManifest -TimeoutSeconds $TimeoutSeconds `
        -ExitAfterCompletion $ExitAfterCompletion
    $requestPath = Join-Path $evidence 'runtime-request.json'
    Write-KbpJsonAtomic $requestPath $request
    $orchestration = [ordered]@{
        schemaVersion = 1; runId = $runId; scenario = $Scenario
        status = 'IN PROGRESS'; stage = 'request-written'; steamSafety = $steamSafety
        packagePath = $package; packageSha256 = $buildManifest.packageSha256
        transactionStatePath = $statePath; startedAtUtc = [DateTime]::UtcNow.ToString('o')
    }
    Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
    $preexisting = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue | ForEach-Object Id)
    $arguments = @('-applaunch', '640820', '-kbpRuntimeTestRequest', ('"' + $requestPath + '"'))
    [void](Start-Process -FilePath $SteamPath -ArgumentList $arguments -PassThru)
    $process = Wait-KbpNewKingmakerProcess -PreexistingIds $preexisting -TimeoutSeconds $LaunchTimeoutSeconds
    $orchestration.stage = 'waiting-for-result'
    $orchestration.kingmakerProcessId = $process.Id
    $orchestration.kingmakerStartedAtUtc = $process.StartTime.ToUniversalTime().ToString('o')
    Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
    $resultPath = Join-Path $evidence 'runtime-result.json'
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds + 15)
    while (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        $process.Refresh()
        if ($process.HasExited) { throw 'Kingmaker exited before committing the atomic runtime result.' }
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Runtime result timed out; launched Kingmaker was left running and restoration is blocked.' }
        Start-Sleep -Milliseconds 250
    }
    $result = Read-KbpJson $resultPath
    Assert-KbpRuntimeResult -Result $result -Request $request -BuildManifest $buildManifest
    if (-not $process.WaitForExit(30000)) { throw 'Kingmaker did not exit after committing its result; restoration is blocked.' }
    $orchestration.status = $result.status
    $orchestration.stage = 'result-validated'
    $orchestration.completedAtUtc = [DateTime]::UtcNow.ToString('o')
    Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
    if ($result.status -cne 'PASS') { throw "Runtime scenario returned $($result.status)." }
    Write-Host "Runtime result PASS: $resultPath"
}
finally {
    if ($transactionEntered) {
        if ($null -ne $process) {
            try { [void]$process.WaitForExit(30000) }
            catch { Write-Warning "Unable to wait for launched Kingmaker exit: $($_.Exception.Message)" }
        }
        $running = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
        if ($running.Count -eq 0) {
            & (Join-Path $PSScriptRoot 'Restore-Local.ps1') -RunId $runId -Confirm:$false
        } else {
            Write-Error "Kingmaker remains running; exact Mods restoration is intentionally blocked. Transaction: $runId"
        }
    }
}
