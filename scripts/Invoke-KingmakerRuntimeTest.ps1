[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidateSet('mod-load-smoke', 'native-buff-catalog', 'ui-root-smoke', 'live-ui-bootstrap', 'ui-native-contract-probe', 'final-no-save-core', 'performance-probe', 'launch-render-diagnostic', 'menu-input-diagnostic', 'live-workspace-qual')][string]$Scenario = 'mod-load-smoke',
    [ValidateSet('native-only', 'call-of-the-wild', 'human-reproduction', 'full-user')][string]$CompatibilityProfileId = 'native-only',
    [ValidateRange(5, 1800)][int]$TimeoutSeconds = 180,
    [ValidateRange(5, 300)][int]$LaunchTimeoutSeconds = 60,
    [ValidateSet('animated', 'instant')][string]$ExecutionMode = 'instant',
    [ValidateRange(5, 60)][int]$PerformanceDurationSeconds = 20,
    [ValidateRange(0, 240)][double]$MinimumFramesPerSecond = 0,
    [switch]$DiagnosticDisableHudDiscovery,
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
    $scenarioParameters = if ($null -ne $savePair) { @{
        workingSaveName = $savePair.working.name; workingFileName = $savePair.working.fileName
        workingSha256 = $savePair.working.sha256; baselineSaveName = $savePair.baseline.name
        baselineFileName = $savePair.baseline.fileName; baselineSha256 = $savePair.baseline.sha256
        expectedGameName = $savePair.working.gameName; expectedGameId = $savePair.working.gameId
        executionMode = $ExecutionMode
    } } elseif ($Scenario -ceq 'performance-probe') { @{
        durationSeconds = $PerformanceDurationSeconds
        disableHudDiscovery = [bool]$DiagnosticDisableHudDiscovery
        minimumFramesPerSecond = $MinimumFramesPerSecond
    } } else { @{} }
    $request = New-KbpRuntimeRequest -RunId $runId -EvidenceDirectory $evidence `
        -BuildManifest $buildManifest -TimeoutSeconds $TimeoutSeconds `
        -ExitAfterCompletion $ExitAfterCompletion -Scenario $Scenario `
        -ProfileId $CompatibilityProfileId -ExpectedOptionalMods $expectedOptionalMods `
        -ExpectedBlueprintGuids @($compatibilityProfile.expectedBlueprints) `
        -Parameters $scenarioParameters
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
    $processInfo = Get-CimInstance Win32_Process -Filter ("ProcessId = " + $process.Id) -ErrorAction SilentlyContinue
    if ($null -ne $processInfo) {
        $orchestration.kingmakerCommandLine = [string]$processInfo.CommandLine
        $orchestration.kingmakerSessionId = [int]$processInfo.SessionId
    }
    Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
    $resultPath = Join-Path $evidence 'runtime-result.json'
    $physicalInputScenario = ($Scenario -ceq 'live-ui-bootstrap') -or
        ($Scenario -ceq 'menu-input-diagnostic')
    $plannerHotkeySent = $false
    $ummDismissSent = $false
    $ummDismissRecoverySent = $false
    $ummDismissAttempts = 0
    $ummDismissSentAtUtc = [DateTime]::MinValue
    try {
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
  [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
  [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
  [StructLayout(LayoutKind.Sequential)] public struct Point { public int X; public int Y; }
  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
  public static void KeyDown(IntPtr window, byte key) {
    if (window == IntPtr.Zero || !Activate(window)) throw new InvalidOperationException("Kingmaker foreground activation failed.");
    keybd_event(key, 0, 0, UIntPtr.Zero);
  }
  public static void KeyUp(byte key) { keybd_event(key, 0, 2, UIntPtr.Zero); }
  public static void Move(IntPtr window, double x, double y, int unityWidth, int unityHeight) {
    if (window == IntPtr.Zero || !Activate(window)) throw new InvalidOperationException("Kingmaker foreground activation failed.");
    Rect rect;
    if (!GetClientRect(window, out rect)) throw new InvalidOperationException("Kingmaker client bounds lookup failed.");
    if (unityWidth <= 0 || unityHeight <= 0) throw new InvalidOperationException("Unity screen bounds are invalid.");
    int scaledX = (int)Math.Round(x * rect.Right / unityWidth);
    int scaledY = (int)Math.Round(y * rect.Bottom / unityHeight);
    Point point = new Point { X = scaledX, Y = Math.Max(0, rect.Bottom - scaledY) };
    if (!ClientToScreen(window, ref point) || !SetCursorPos(point.X, point.Y)) throw new InvalidOperationException("Kingmaker cursor movement failed.");
  }
  private static bool Activate(IntPtr window) {
    if (SetForegroundWindow(window)) return true;
    // Foreground-lock workaround: a neutral ALT tap registers shell input,
    // after which activation from a background process is permitted.
    keybd_event(0x12, 0, 0, UIntPtr.Zero);
    keybd_event(0x12, 0, 2, UIntPtr.Zero);
    System.Threading.Thread.Sleep(50);
    return SetForegroundWindow(window);
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
  public static string WindowState(IntPtr window) {
    if (window == IntPtr.Zero) return "no-window";
    return "minimized=" + IsIconic(window) + ";foreground=" + (GetForegroundWindow() == window);
  }
}
'@
    $windowObservations = New-Object System.Collections.Generic.List[object]
    $physicalDeliveryAttempts = @{}
    $nextWindowSampleUtc = [DateTime]::UtcNow
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds + 15)
    }
    catch {
        # Preserve the exact pre-loop failure with its position and stack so
        # the finally's own Write-Error can never displace the diagnosis.
        $detail = $_.Exception.ToString() + [Environment]::NewLine +
            $_.InvocationInfo.PositionMessage + [Environment]::NewLine +
            (Get-PSCallStack | Out-String)
        [IO.File]::WriteAllText((Join-Path $evidence 'harness-preloop-error.txt'), $detail)
        throw
    }
    try {
    while (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        $process.Refresh()
        if ($process.HasExited) { throw 'Kingmaker exited before committing the atomic runtime result.' }
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Runtime result timed out; launched Kingmaker was left running and restoration is blocked.' }
        if ([DateTime]::UtcNow -ge $nextWindowSampleUtc) {
            $nextWindowSampleUtc = [DateTime]::UtcNow.AddSeconds(5)
            # Passive observation only: a sampler failure must never abort the
            # guarded run or block the result/restore paths.
            try {
                $sample = [ordered]@{
                    atUtc = [DateTime]::UtcNow.ToString('o')
                    windowState = [KbpPhysicalInput]::WindowState($process.MainWindowHandle)
                    mainWindowTitle = $process.MainWindowTitle
                    responding = $process.Responding
                }
            } catch {
                $sample = [ordered]@{
                    atUtc = [DateTime]::UtcNow.ToString('o')
                    observationError = $_.Exception.Message
                }
            }
            $windowObservations.Add($sample)
            if ($windowObservations.Count -gt 60) { $windowObservations.RemoveAt(0) }
            # Do NOT wrap the generic List in @(); PowerShell 5.1's array-
            # subexpression binder fails on List[object] with
            # "Argument types do not match" (captured in menuinput-3 evidence).
            $orchestration['windowObservations'] = $windowObservations.ToArray()
            try { Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration } catch { }
        }
        $ummMarker = Join-Path $evidence 'umm-overlay-ready.json'
        if ($Scenario -ceq 'live-ui-bootstrap' -and -not $ummDismissSent -and
            (Test-Path -LiteralPath $ummMarker -PathType Leaf)) {
            $process.Refresh()
            try {
                [KbpPhysicalInput]::KeyDown($process.MainWindowHandle, [byte]0x1B)
                Start-Sleep -Milliseconds 100
                [KbpPhysicalInput]::KeyUp([byte]0x1B)
                $ummDismissSent = $true
                $ummDismissSentAtUtc = [DateTime]::UtcNow
                $orchestration.stage = 'physical-umm-dismiss-sent'
                $orchestration.ummDismissSentAtUtc = $ummDismissSentAtUtc.ToString('o')
                Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
            }
            catch {
                # Foreground-lock delivery failures retry on later polls and
                # must never abort the guarded run.
                $ummDismissAttempts++
                $orchestration.lastUmmDismissError = $_.Exception.Message
                if (($ummDismissAttempts % 10) -eq 1) {
                    Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
                }
            }
        }
        $hotkeyMarker = Join-Path $evidence 'hotkey-ready.json'
        if ($Scenario -ceq 'live-ui-bootstrap' -and $ummDismissSent -and
            -not $ummDismissRecoverySent -and -not (Test-Path -LiteralPath $hotkeyMarker -PathType Leaf) -and
            [DateTime]::UtcNow -ge $ummDismissSentAtUtc.AddSeconds(2)) {
            # Depending on the active UMM overlay layer, the physical dismissal can also
            # open Kingmaker's Escape menu. One bounded follow-up closes that native veil;
            # production HUD ownership and input suppression remain unchanged.
            $process.Refresh()
            try {
                [KbpPhysicalInput]::KeyDown($process.MainWindowHandle, [byte]0x1B)
                Start-Sleep -Milliseconds 100
                [KbpPhysicalInput]::KeyUp([byte]0x1B)
                $ummDismissRecoverySent = $true
                $orchestration.stage = 'physical-umm-dismiss-recovery-sent'
                $orchestration.ummDismissRecoverySentAtUtc = [DateTime]::UtcNow.ToString('o')
                Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
            }
            catch {
                $orchestration.lastUmmRecoveryError = $_.Exception.Message
            }
        }
        if ($Scenario -ceq 'live-ui-bootstrap' -and -not $plannerHotkeySent -and
            (Test-Path -LiteralPath $hotkeyMarker -PathType Leaf)) {
            $process.Refresh()
            try {
                [KbpPhysicalInput]::KeyDown($process.MainWindowHandle, [byte]0x11)
                [KbpPhysicalInput]::KeyDown($process.MainWindowHandle, [byte]0x10)
                [KbpPhysicalInput]::KeyDown($process.MainWindowHandle, [byte]0x42)
                Start-Sleep -Milliseconds 100
                [KbpPhysicalInput]::KeyUp([byte]0x42)
                [KbpPhysicalInput]::KeyUp([byte]0x10)
                [KbpPhysicalInput]::KeyUp([byte]0x11)
                $plannerHotkeySent = $true
                $orchestration.stage = 'physical-planner-hotkey-sent'
                $orchestration.plannerHotkeySentAtUtc = [DateTime]::UtcNow.ToString('o')
                Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
            }
            catch {
                # All three keydowns must land together or not at all; retry
                # the whole chord on later polls.
                $orchestration.lastHotkeyError = $_.Exception.Message
            }
        }
        if ($physicalInputScenario) {
            if ($null -eq $physicalDeliveryAttempts) { $physicalDeliveryAttempts = @{} }
            $physicalRequests = @(Get-ChildItem -LiteralPath $evidence -Filter 'physical-input-*.json' `
                -File -ErrorAction SilentlyContinue | Where-Object Name -NotLike '*.ack.json' |
                Sort-Object Name)
            foreach ($physicalFile in $physicalRequests) {
                $physical = Read-KbpJson $physicalFile.FullName
                $ackPath = Join-Path $evidence ("physical-input-{0}.ack.json" -f $physical.actionId)
                if (Test-Path -LiteralPath $ackPath -PathType Leaf) { continue }
                $process.Refresh()
                $actionId = [string]$physical.actionId
                if (-not $physicalDeliveryAttempts.ContainsKey($actionId)) { $physicalDeliveryAttempts[$actionId] = 0 }
                $delivered = $false
                $deliveryError = $null
                for ($attempt = 1; $attempt -le 3 -and -not $delivered; $attempt++) {
                    try {
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
                        $delivered = $true
                    }
                    catch {
                        $deliveryError = $_.Exception.Message
                        $physicalDeliveryAttempts[$actionId]++
                        Start-Sleep -Milliseconds 250
                    }
                }
                if (-not $delivered -and [int]$physicalDeliveryAttempts[$actionId] -ge 20) {
                    # The in-game waiter must not hang forever: after bounded
                    # retries, acknowledge the failure explicitly so the
                    # scenario can fail honestly with evidence.
                    Write-KbpJsonAtomic $ackPath ([ordered]@{
                        schemaVersion = 1; runId = $runId; actionId = $actionId
                        action = [string]$physical.action; sentAtUtc = [DateTime]::UtcNow.ToString('o')
                        processId = $process.Id; deliveryFailed = $true
                        error = [string]$deliveryError
                    })
                    $orchestration.lastPhysicalDeliveryError = [string]$deliveryError
                    continue
                }
                if (-not $delivered) { continue }
                Write-KbpJsonAtomic $ackPath ([ordered]@{
                    schemaVersion = 1; runId = $runId; actionId = $actionId
                    action = [string]$physical.action; sentAtUtc = [DateTime]::UtcNow.ToString('o')
                    processId = $process.Id
                    windowsClientCursor = [KbpPhysicalInput]::ClientCursor($process.MainWindowHandle)
                })
                $orchestration.stage = "physical-$actionId-sent"
                Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
            }
        }
        Start-Sleep -Milliseconds 250
    }
    }
    catch {
        $detail = $_.Exception.ToString() + [Environment]::NewLine +
            $_.InvocationInfo.PositionMessage + [Environment]::NewLine +
            (Get-PSCallStack | Out-String)
        [IO.File]::WriteAllText((Join-Path $evidence 'harness-loop-error.txt'), $detail)
        throw
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
    try {
        # In non-interactive hosts the finally's own Write-Error can displace
        # the original terminating error from the output stream; persist the
        # pending errors first so no failure cause is ever lost.
        if (@($Error).Count -gt 0 -and $null -ne (Get-Variable -Name evidence -ErrorAction SilentlyContinue)) {
            $lines = foreach ($entry in @($Error | Select-Object -First 5)) { $entry.ToString() }
            [IO.File]::WriteAllLines((Join-Path $evidence 'harness-error.txt'), [string[]]$lines)
        }
    }
    catch { }
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
