[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidateSet('mod-load-smoke', 'native-buff-catalog', 'ui-root-smoke', 'live-ui-bootstrap', 'ui-native-contract-probe', 'final-no-save-core')][string]$Scenario = 'mod-load-smoke',
    [ValidateSet('native-only', 'call-of-the-wild', 'human-reproduction')][string]$CompatibilityProfileId = 'native-only',
    [ValidateRange(5, 1800)][int]$TimeoutSeconds = 180,
    [ValidateRange(5, 300)][int]$LaunchTimeoutSeconds = 60,
    [bool]$ExitAfterCompletion = $true,
    [string]$SteamPath = 'C:\Program Files (x86)\Steam\steam.exe',
    [ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
. (Join-Path $PSScriptRoot 'compatibility\CompatibilityProfile.Common.ps1')

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
$compatibilityProfile = Get-KbpCompatibilityProfile $CompatibilityProfileId
Assert-KbpCompatibilityProfileFixtures -Profile $compatibilityProfile
$expectedOptionalMods = @($compatibilityProfile.mods | ForEach-Object {
    [ordered]@{
        ummId = $_.ummId
        version = $_.version
        assemblyName = $_.assemblyName
        assemblySha256 = if ($_.PSObject.Properties.Name -contains 'loadedAssemblySha256') {
            $_.loadedAssemblySha256
        } else { $_.assemblySha256 }
    }
})
$savePair = if ($Scenario -ceq 'live-ui-bootstrap') { Get-KbpDisposableSavePair } else { $null }
$steamSafety = Assert-KbpSteamSafety -SteamPath $SteamPath
& (Join-Path $PSScriptRoot 'Deploy-Local.ps1') -PackagePath $package `
    -RunId 'runtime-whatif-preflight' -CompatibilityProfileId $CompatibilityProfileId `
    -WhatIf -Confirm:$false
$WhatIfPreference = $requestedWhatIf
if (-not $PSCmdlet.ShouldProcess(
    'Steam App ID 640820 and exact live Mods transaction',
    "run guarded $Scenario for version $version")) {
    Write-Host 'Runtime WhatIf preflight PASS; no evidence, deployment, process, game, mod, or save mutation occurred.'
    return
}

$ConfirmPreference = 'None'
$WhatIfPreference = $false
$runId = if ([string]::IsNullOrWhiteSpace($RunId)) {
    [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ') + '-' + $Scenario
} else { $RunId }
$evidence = Join-Path $script:KbpRuntimeEvidenceRoot $runId
$transactionRecord = Join-Path $script:KbpRuntimeStateRoot "transactions\$runId"
if ((Test-Path -LiteralPath $evidence) -or (Test-Path -LiteralPath $transactionRecord)) {
    throw "Runtime run ID is already present and cannot be reused: $runId"
}
$transactionEntered = $false
$process = $null
New-Item -ItemType Directory -Path $evidence | Out-Null
try {
    $statePath = & (Join-Path $PSScriptRoot 'Deploy-Local.ps1') -PackagePath $package `
        -RunId $runId -CompatibilityProfileId $CompatibilityProfileId `
        -Confirm:$false | Select-Object -Last 1
    $transactionEntered = $true
    $request = New-KbpRuntimeRequest -RunId $runId -EvidenceDirectory $evidence `
        -BuildManifest $buildManifest -TimeoutSeconds $TimeoutSeconds `
        -ExitAfterCompletion $ExitAfterCompletion -Scenario $Scenario `
        -ProfileId $CompatibilityProfileId -ExpectedOptionalMods $expectedOptionalMods `
        -ExpectedBlueprintGuids @($compatibilityProfile.expectedBlueprints) `
        -Parameters $(if ($null -eq $savePair) { @{} } else { @{
            workingSaveName = $savePair.working.name; workingFileName = $savePair.working.fileName
            workingSha256 = $savePair.working.sha256; baselineSaveName = $savePair.baseline.name
            baselineFileName = $savePair.baseline.fileName; baselineSha256 = $savePair.baseline.sha256
            expectedGameName = $savePair.working.gameName; expectedGameId = $savePair.working.gameId
        } })
    $requestPath = Join-Path $evidence 'runtime-request.json'
    Write-KbpJsonAtomic $requestPath $request
    $orchestration = [ordered]@{
        schemaVersion = 1; runId = $runId; scenario = $Scenario; profileId = $CompatibilityProfileId
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
    $f10Sent = $false
    $ummDismissSent = $false
    if ($Scenario -ceq 'live-ui-bootstrap') {
        Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class KbpPhysicalInput {
  [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
  [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr hWnd, ref Point point);
  [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out Rect rect);
  [DllImport("user32.dll")] static extern bool GetCursorPos(out Point point);
  [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, ref Point point);
  [StructLayout(LayoutKind.Sequential)] public struct Point { public int X; public int Y; }
  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
  public static void KeyDown(IntPtr window, byte key) {
    if (window == IntPtr.Zero || !SetForegroundWindow(window)) throw new InvalidOperationException("Kingmaker foreground activation failed.");
    keybd_event(key, 0, 0, UIntPtr.Zero);
  }
  public static void KeyUp(byte key) { keybd_event(key, 0, 2, UIntPtr.Zero); }
  public static void Move(IntPtr window, double x, double y, int unityWidth, int unityHeight) {
    if (window == IntPtr.Zero || !SetForegroundWindow(window)) throw new InvalidOperationException("Kingmaker foreground activation failed.");
    Rect rect;
    if (!GetClientRect(window, out rect)) throw new InvalidOperationException("Kingmaker client bounds lookup failed.");
    if (unityWidth <= 0 || unityHeight <= 0) throw new InvalidOperationException("Unity screen bounds are invalid.");
    int scaledX = (int)Math.Round(x * rect.Right / unityWidth);
    int scaledY = (int)Math.Round(y * rect.Bottom / unityHeight);
    Point point = new Point { X = scaledX, Y = Math.Max(0, rect.Bottom - scaledY) };
    if (!ClientToScreen(window, ref point) || !SetCursorPos(point.X, point.Y)) throw new InvalidOperationException("Kingmaker cursor movement failed.");
  }
  public static void Click() {
    mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
    mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
  }
  public static string ClientCursor(IntPtr window) {
    Point point;
    if (!GetCursorPos(out point) || !ScreenToClient(window, ref point)) return "unavailable";
    return point.X.ToString() + "," + point.Y.ToString();
  }
}
'@
    }
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds + 15)
    while (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        $process.Refresh()
        if ($process.HasExited) { throw 'Kingmaker exited before committing the atomic runtime result.' }
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Runtime result timed out; launched Kingmaker was left running and restoration is blocked.' }
        $ummMarker = Join-Path $evidence 'umm-overlay-ready.json'
        if ($Scenario -ceq 'live-ui-bootstrap' -and -not $ummDismissSent -and
            (Test-Path -LiteralPath $ummMarker -PathType Leaf)) {
            $process.Refresh()
            [KbpPhysicalInput]::KeyDown($process.MainWindowHandle, [byte]0x1B)
            Start-Sleep -Milliseconds 100
            [KbpPhysicalInput]::KeyUp([byte]0x1B)
            $ummDismissSent = $true
            $orchestration.stage = 'physical-umm-dismiss-sent'
            $orchestration.ummDismissSentAtUtc = [DateTime]::UtcNow.ToString('o')
            Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
        }
        $f10Marker = Join-Path $evidence 'f10-ready.json'
        if ($Scenario -ceq 'live-ui-bootstrap' -and -not $f10Sent -and
            (Test-Path -LiteralPath $f10Marker -PathType Leaf)) {
            $process.Refresh()
            [KbpPhysicalInput]::KeyDown($process.MainWindowHandle, [byte]0x79)
            Start-Sleep -Milliseconds 100
            [KbpPhysicalInput]::KeyUp([byte]0x79)
            $f10Sent = $true
            $orchestration.stage = 'physical-f10-sent'
            $orchestration.f10SentAtUtc = [DateTime]::UtcNow.ToString('o')
            Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
        }
        if ($Scenario -ceq 'live-ui-bootstrap') {
            $physicalRequests = @(Get-ChildItem -LiteralPath $evidence -Filter 'physical-input-*.json' `
                -File -ErrorAction SilentlyContinue | Where-Object Name -NotLike '*.ack.json' |
                Sort-Object Name)
            foreach ($physicalFile in $physicalRequests) {
                $physical = Read-KbpJson $physicalFile.FullName
                $ackPath = Join-Path $evidence ("physical-input-{0}.ack.json" -f $physical.actionId)
                if (Test-Path -LiteralPath $ackPath -PathType Leaf) { continue }
                $process.Refresh()
                if ([string]$physical.action -eq 'key-escape') {
                    [KbpPhysicalInput]::KeyDown($process.MainWindowHandle, [byte]0x1B)
                    Start-Sleep -Milliseconds 100
                    [KbpPhysicalInput]::KeyUp([byte]0x1B)
                } else {
                    [KbpPhysicalInput]::Move($process.MainWindowHandle,
                        [double]$physical.x, [double]$physical.y,
                        [int]$physical.unityScreenWidth, [int]$physical.unityScreenHeight)
                    Start-Sleep -Milliseconds 250
                    if ([string]$physical.action -eq 'click') {
                        [KbpPhysicalInput]::Click()
                    } elseif ([string]$physical.action -ne 'hover') {
                        throw "Unknown physical input action: $($physical.action)"
                    }
                }
                Write-KbpJsonAtomic $ackPath ([ordered]@{
                    schemaVersion = 1; runId = $runId; actionId = [string]$physical.actionId
                    action = [string]$physical.action; sentAtUtc = [DateTime]::UtcNow.ToString('o')
                    processId = $process.Id
                    windowsClientCursor = [KbpPhysicalInput]::ClientCursor($process.MainWindowHandle)
                })
                $orchestration.stage = "physical-$($physical.actionId)-sent"
                Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
            }
        }
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
    if ($Scenario -ceq 'live-ui-bootstrap') {
        $afterPair = Get-KbpDisposableSavePair
        if ($afterPair.baseline.sha256 -cne $savePair.baseline.sha256) {
            throw 'Immutable KBP_AUTOMATION_BASELINE changed during the live scenario.'
        }
        $orchestration.workingSaveSha256Before = $savePair.working.sha256
        $orchestration.workingSaveSha256After = $afterPair.working.sha256
        $orchestration.baselineSaveSha256 = $afterPair.baseline.sha256
        Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
    }
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
