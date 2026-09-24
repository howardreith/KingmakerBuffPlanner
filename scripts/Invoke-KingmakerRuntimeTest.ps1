[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidateSet('mod-load-smoke', 'native-buff-catalog', 'ui-root-smoke', 'live-ui-bootstrap', 'ui-native-contract-probe', 'final-no-save-core', 'performance-probe', 'launch-render-diagnostic', 'menu-input-diagnostic', 'live-workspace-qual', 'live-workspace-reload', 'live-workspace-import', 'live-workspace-manual', 'live-cast-probe-select', 'live-cast-probe', 'live-advanced-inspect', 'live-cast-qual-select', 'live-cast-qual', 'live-classic-select', 'live-classic-cast', 'live-workspace-physical')][string]$Scenario = 'mod-load-smoke',
    [ValidateSet('native-only', 'call-of-the-wild', 'human-reproduction', 'full-user')][string]$CompatibilityProfileId = 'native-only',
    [ValidateRange(5, 1800)][int]$TimeoutSeconds = 180,
    [ValidateRange(5, 300)][int]$LaunchTimeoutSeconds = 60,
    [ValidateSet('animated', 'instant')][string]$ExecutionMode = 'instant',
    [ValidateRange(5, 60)][int]$PerformanceDurationSeconds = 20,
    [ValidateRange(0, 240)][double]$MinimumFramesPerSecond = 0,
    [switch]$DiagnosticDisableHudDiscovery,
    [bool]$ExitAfterCompletion = $true,
    [string]$SteamPath = 'C:\Program Files (x86)\Steam\steam.exe',
    [ValidatePattern('^[A-Za-z0-9._-]{1,100}$')][string]$RunId,
    # Supervised manual-inspection hold (live-workspace-manual only): the
    # harness performs NO synthetic input; the host acknowledges
    # manual-ready and holds for the operator until manual-done.json /
    # manual-stop.json appears in the evidence directory or this deadline
    # passes (a deadline is never acceptance). RehearseDone exercises the
    # done path without an operator by writing the done marker 20 seconds
    # after manual-ready, clearly labeled as a rehearsal.
    [ValidateRange(30, 1200)][int]$ManualHoldSeconds = 300,
    [switch]$ManualRehearseDone,
    # Single-cast probe (live-cast-probe only): the OWNER's run-bound,
    # one-shot allowance file, kept outside the repository under the lab's
    # approvals directory. Without it the casting probe cannot be launched;
    # live-cast-probe-select never takes one and never constructs a
    # dispatch boundary.
    [string]$ProbeAllowancePath,
    # Guarded casting qualification (live-cast-qual only): the run-bound
    # schema-3 allowance under the lab approvals directory naming the exact
    # forecast projections (from a live-cast-qual-select run) and a 1..24
    # submission budget.
    [string]$QualificationAllowancePath,
    # Qualification recipe (qualification scenarios only): the selection
    # run defaults to zero-cost-mixed; a casting run takes its recipe from
    # the allowance, and when this is given as well it must name the same.
    [ValidateSet('zero-cost-mixed', 'finite-direct-mixed')][string]$QualificationRecipe,
    # Classic cast (live-classic-cast only): the run-bound kbp-classic-cast
    # allowance under the lab approvals directory naming the exact classic
    # plan digest (from a live-classic-select run), the casting mode and a
    # 1..24 submission budget.
    [string]$ClassicAllowancePath,
    # Fixture family: the approved automation pair (default) or the
    # owner-designated advanced copy. The advanced copy is loaded only by
    # non-casting scenarios and only when it matches its guarded bootstrap
    # manifest exactly.
    [ValidateSet('Automation', 'Advanced')][string]$FixtureFamily = 'Automation',
    # Game window mode for live-workspace-physical (mission batch 3,
    # section 10): the owner's own settings, or a borderless window of an
    # exact size through Unity's launch arguments. A size larger than this
    # session's display is refused before anything changes; the game's
    # registry key (Unity PlayerPrefs) is restored byte-exact after exit.
    [ValidateSet('owner', 'windowed-1920x1080', 'windowed-2560x1440')][string]$DisplayMode = 'owner'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RuntimeAutomation.Common.ps1')
. (Join-Path $PSScriptRoot 'compatibility\CompatibilityProfile.Common.ps1')

$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false
# Review of f7726c9..1332ed8, P3-J: ValidateSet binds case-insensitively,
# but every later comparison is case-sensitive; continue with the
# canonical spelling of each value (for example -Scenario LIVE-CAST-QUAL).
foreach ($canonicalName in @('Scenario', 'CompatibilityProfileId', 'ExecutionMode', 'FixtureFamily', 'QualificationRecipe', 'DisplayMode')) {
    $bound = Get-Variable -Name $canonicalName -ValueOnly -ErrorAction SilentlyContinue
    if ([string]::IsNullOrEmpty([string]$bound)) { continue }
    $validSet = @((Get-Command -Name $PSCommandPath).Parameters[$canonicalName].Attributes |
        Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
        ForEach-Object { $_.ValidValues })
    $canonical = @($validSet | Where-Object { [string]$_ -ieq [string]$bound })
    if ($canonical.Count -eq 1) { Set-Variable -Name $canonicalName -Value ([string]$canonical[0]) }
}
$root = Get-KbpRepositoryRoot
$version = Get-KbpVersion
$package = (Resolve-Path -LiteralPath (Join-Path $root "artifacts\local-runtime\$version\KingmakerBuffPlanner-$version-local-runtime.zip")).Path
& (Join-Path $PSScriptRoot 'Validate-Source.ps1')
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $package
$gitStatus = @(& git -C $root status --porcelain)
if ($LASTEXITCODE -ne 0 -or @($gitStatus).Count -ne 0) { throw 'Runtime qualification requires a clean Git worktree.' }
$buildManifest = Read-KbpBuildManifest $package
$probeAllowanceJson = $null
if ($Scenario -ceq 'live-cast-probe') {
    if ([string]::IsNullOrWhiteSpace($ProbeAllowancePath)) {
        throw "live-cast-probe requires -ProbeAllowancePath (the owner's run-bound one-shot allowance)."
    }
    if ([string]::IsNullOrWhiteSpace($RunId)) { throw 'live-cast-probe requires an explicit -RunId matching the allowance.' }
    if ($ExecutionMode -cne 'instant') { throw 'live-cast-probe runs in instant mode only.' }
    $approvalsRoot = [IO.Path]::GetFullPath((Join-Path $root '..\..\approvals')).TrimEnd('\') + '\'
    $allowanceFull = [IO.Path]::GetFullPath($ProbeAllowancePath)
    if (-not $allowanceFull.StartsWith($approvalsRoot, [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $allowanceFull -PathType Leaf)) {
        throw "The probe allowance must be an existing file under $approvalsRoot"
    }
    $probeAllowanceJson = [IO.File]::ReadAllText($allowanceFull)
    # Review N1: run id, commit, one submission AND the frozen package/DLL/
    # MVID must all match this build before anything is deployed; the host
    # re-measures the loaded DLL and MVID before any submission.
    $allowanceRefusal = Get-KbpProbeAllowanceBuildRefusal -AllowanceJson $probeAllowanceJson `
        -RunId $RunId -BuildManifest $buildManifest
    if ($null -ne $allowanceRefusal) { throw "The probe allowance was refused: $allowanceRefusal" }
}
elseif (-not [string]::IsNullOrWhiteSpace($ProbeAllowancePath)) {
    throw '-ProbeAllowancePath is only valid with -Scenario live-cast-probe.'
}
$qualificationAllowanceJson = $null
if ($Scenario -ceq 'live-cast-qual') {
    if ([string]::IsNullOrWhiteSpace($QualificationAllowancePath)) {
        throw 'live-cast-qual requires -QualificationAllowancePath (the run-bound qualification allowance).'
    }
    if ([string]::IsNullOrWhiteSpace($RunId)) { throw 'live-cast-qual requires an explicit -RunId matching the allowance.' }
    # The casting run executes in the mode its allowance approves (the
    # selection run is mode-independent: projections do not sign it).
    $qualificationApprovals = [IO.Path]::GetFullPath((Join-Path $root '..\..\approvals')).TrimEnd('\') + '\'
    $qualificationFull = [IO.Path]::GetFullPath($QualificationAllowancePath)
    if (-not $qualificationFull.StartsWith($qualificationApprovals, [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $qualificationFull -PathType Leaf)) {
        throw "The qualification allowance must be an existing file under $qualificationApprovals"
    }
    $qualificationAllowanceJson = [IO.File]::ReadAllText($qualificationFull)
    $qualificationRefusal = Get-KbpQualificationAllowanceBuildRefusal -AllowanceJson $qualificationAllowanceJson `
        -RunId $RunId -BuildManifest $buildManifest -Recipe $QualificationRecipe -ExecutionMode $ExecutionMode
    if ($null -ne $qualificationRefusal) { throw "The qualification allowance was refused: $qualificationRefusal" }
}
elseif (-not [string]::IsNullOrWhiteSpace($QualificationAllowancePath)) {
    throw '-QualificationAllowancePath is only valid with -Scenario live-cast-qual.'
}
$classicAllowanceJson = $null
if ($Scenario -ceq 'live-classic-cast') {
    if ([string]::IsNullOrWhiteSpace($ClassicAllowancePath)) {
        throw 'live-classic-cast requires -ClassicAllowancePath (the run-bound classic allowance).'
    }
    if ([string]::IsNullOrWhiteSpace($RunId)) { throw 'live-classic-cast requires an explicit -RunId matching the allowance.' }
    $classicApprovals = [IO.Path]::GetFullPath((Join-Path $root '..\..\approvals')).TrimEnd('\') + '\'
    $classicFull = [IO.Path]::GetFullPath($ClassicAllowancePath)
    if (-not $classicFull.StartsWith($classicApprovals, [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $classicFull -PathType Leaf)) {
        throw "The classic allowance must be an existing file under $classicApprovals"
    }
    $classicAllowanceJson = [IO.File]::ReadAllText($classicFull)
    $classicRefusal = Get-KbpClassicAllowanceBuildRefusal -AllowanceJson $classicAllowanceJson `
        -RunId $RunId -BuildManifest $buildManifest -ExecutionMode $ExecutionMode
    if ($null -ne $classicRefusal) { throw "The classic allowance was refused: $classicRefusal" }
}
elseif (-not [string]::IsNullOrWhiteSpace($ClassicAllowancePath)) {
    throw '-ClassicAllowancePath is only valid with -Scenario live-classic-cast.'
}
$displaySize = $null
if ($DisplayMode -cne 'owner') {
    if ($Scenario -cne 'live-workspace-physical') {
        throw '-DisplayMode is only valid with -Scenario live-workspace-physical.'
    }
    $displaySize = $DisplayMode.Substring('windowed-'.Length)
    $sessionDisplay = Get-KbpSessionDisplaySize
    if (-not (Test-KbpDisplayModeSupported -Size $displaySize -DisplaySize $sessionDisplay)) {
        throw "DisplayMode $DisplayMode is unsupported on this session's display ($sessionDisplay); nothing was changed."
    }
}
if ($Scenario -ceq 'live-workspace-physical' -and $TimeoutSeconds -lt 900) {
    throw "TimeoutSeconds must be at least 900 for $Scenario (boot/load plus the physical sequence); got $TimeoutSeconds."
}
if (($Scenario -ceq 'live-classic-cast' -or $Scenario -ceq 'live-classic-select') -and
    $TimeoutSeconds -lt 900) {
    throw "TimeoutSeconds must be at least 900 for $Scenario (boot/load plus the classic run deadline); got $TimeoutSeconds."
}
if (-not [string]::IsNullOrWhiteSpace($QualificationRecipe) -and
    $Scenario -cne 'live-cast-qual' -and $Scenario -cne 'live-cast-qual-select') {
    throw '-QualificationRecipe is only valid with -Scenario live-cast-qual-select or live-cast-qual.'
}
# Review P2-2: a qualification run needs the boot/load budget (600 s) plus
# its own deadline (240 s) inside the harness wait, or the harness would
# abandon a live run with the Mods folder unrestored.
if (($Scenario -ceq 'live-cast-qual' -or $Scenario -ceq 'live-cast-qual-select') -and
    $TimeoutSeconds -lt 900) {
    throw "TimeoutSeconds must be at least 900 for $Scenario (boot/load plus the qualification deadline); got $TimeoutSeconds."
}
# Review of f7726c9..1332ed8, P3-H: a probe waits up to 30 s for the world
# and has a 60 s run deadline after boot and load; the harness wait must
# cover them, or it would abandon a live run with the Mods folder
# unrestored.
if (($Scenario -ceq 'live-cast-probe' -or $Scenario -ceq 'live-cast-probe-select' -or
        $Scenario -ceq 'live-workspace-reload') -and
    $TimeoutSeconds -lt 600) {
    throw "TimeoutSeconds must be at least 600 for $Scenario (boot/load plus the probe's world wait and deadline); got $TimeoutSeconds."
}
if ($Scenario -ceq 'live-cast-qual-select' -and $ExecutionMode -cne 'instant') {
    throw 'live-cast-qual-select runs in instant mode only.'
}
# The advanced copy: the non-casting scenarios, plus the allowance-bound
# casting qualification once a non-casting inspection of the same bound
# pair has passed (checked below, before anything is deployed).
$advancedScenarios = @('live-advanced-inspect', 'live-workspace-qual', 'live-workspace-manual', 'live-cast-qual-select')
if ($FixtureFamily -ceq 'Advanced' -and $advancedScenarios -cnotcontains $Scenario -and
    $Scenario -cne 'live-cast-qual') {
    throw ("The advanced copy may only be loaded by the non-casting scenarios (" +
        ($advancedScenarios -join ', ') + ") or an allowance-bound live-cast-qual; refused: $Scenario.")
}
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
$savePair = if ($Scenario -ceq 'live-ui-bootstrap' -or $Scenario -ceq 'live-workspace-qual' -or
    $Scenario -ceq 'live-workspace-reload' -or $Scenario -ceq 'live-workspace-import' -or
    $Scenario -ceq 'live-workspace-manual' -or
    $Scenario -ceq 'live-cast-probe-select' -or $Scenario -ceq 'live-cast-probe' -or
    $Scenario -ceq 'live-advanced-inspect' -or $Scenario -ceq 'live-cast-qual-select' -or
    $Scenario -ceq 'live-cast-qual' -or $Scenario -ceq 'live-classic-select' -or
    $Scenario -ceq 'live-classic-cast' -or $Scenario -ceq 'live-workspace-physical') {
    Get-KbpDisposableSavePair -Family $FixtureFamily } else { $null }
$advancedBinding = if ($FixtureFamily -ceq 'Advanced') { Assert-KbpAdvancedFixtureBinding -Pair $savePair } else { $null }
$advancedInspectionRunId = if ($FixtureFamily -ceq 'Advanced' -and $Scenario -ceq 'live-cast-qual') {
    Assert-KbpAdvancedInspectionPassed -Binding $advancedBinding -Pair $savePair `
        -ProfileId $CompatibilityProfileId `
        -CompatibilityIdentity (Get-KbpCompatibilityIdentityDigest $compatibilityProfile)
} else { $null }
$steamSafety = Assert-KbpSteamSafety -SteamPath $SteamPath
& (Join-Path $PSScriptRoot 'Deploy-Local.ps1') -PackagePath $package `
    -RunId 'runtime-whatif-preflight' -CompatibilityProfileId $CompatibilityProfileId `
    -WhatIf -Confirm:$false
$WhatIfPreference = $requestedWhatIf
$shouldProceed = $false
$decisionFailure = $null
try {
    $shouldProceed = $PSCmdlet.ShouldProcess(
        'Steam App ID 640820 and exact live Mods transaction',
        "run guarded $Scenario for version $version")
}
catch [NullReferenceException] {
    # powershell.exe -File cannot evaluate a confirmation decision in a
    # top-level script (NullReferenceException; -Command/direct
    # invocations work). A decision that cannot be evaluated is a
    # REFUSED decision for real runs. A -WhatIf request is exempt only
    # because its outcome is deterministically negative: honoring it can
    # never create permission to stage, deploy, or launch.
    if ([bool]$WhatIfPreference) {
        $shouldProceed = $false
    }
    else {
        $decisionFailure = $_.Exception
    }
}
if ($null -ne $decisionFailure) {
    throw ("Runtime launch decision could not be evaluated under powershell.exe -File (" +
        $decisionFailure.Message + "). Invoke the launcher from PowerShell directly, e.g. " +
        "& 'scripts/Invoke-KingmakerRuntimeTest.ps1' -Scenario <scenario>, so the guarded " +
        "confirmation decision is honored. Nothing was staged, deployed, launched, or modified.")
}
if (-not $shouldProceed) {
    Write-Host 'Runtime WhatIf preflight PASS; no evidence, deployment, process, game, mod, or save mutation occurred.'
    return
}

if ($Scenario -ceq 'live-workspace-manual' -and
    $TimeoutSeconds -lt ($ManualHoldSeconds + 420)) {
    throw "TimeoutSeconds must be at least ManualHoldSeconds + 420 (boot/load budget); got $TimeoutSeconds for hold $ManualHoldSeconds."
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
# Protected-save comparison: every save-folder file before launch, compared
# after restoration. Only the run WORKING copy may change; a changed or
# removed file always fails the run, and a new file (for example an
# autosave) fails an advanced-copy run and is recorded otherwise.
$protectedSaveRoot = Join-Path $env:USERPROFILE 'AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games'
$protectedBefore = if ($null -ne $savePair) { Get-KbpSaveFolderSnapshot -SaveRoot $protectedSaveRoot } else { $null }
$protectedSaveFailure = $null
$protectedSavesCompared = $false
# A display mode changes the game's registry settings for this run only:
# the whole key is recorded now and restored byte-exact after exit.
$displayRegistryBefore = if ($null -ne $displaySize) {
    Get-KbpRegistryValueSnapshot -KeyPath $script:KbpGameRegistryKey } else { $null }
$restoreFailure = $null
$runFailure = $null
$runSucceeded = $false
$result = $null
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
    if ($Scenario -ceq 'live-workspace-manual') {
        # The manual scenario always stages the WORKING save pair; its hold
        # parameter merges into that parameter set (never replaces it).
        $scenarioParameters.manualHoldSeconds = $ManualHoldSeconds
    }
    if ($null -ne $probeAllowanceJson) {
        # The host re-parses the allowance strictly against this run id.
        $scenarioParameters.probeAllowance = $probeAllowanceJson
    }
    if ($null -ne $classicAllowanceJson) {
        # The host re-parses the classic allowance strictly against this run.
        $scenarioParameters.classicAllowance = $classicAllowanceJson
    }
    if ($null -ne $displaySize) {
        # The host judges the screen it actually got against this size.
        $scenarioParameters.expectedScreen = $displaySize
    }
    if ($null -ne $qualificationAllowanceJson) {
        $scenarioParameters.qualificationAllowance = $qualificationAllowanceJson
    }
    if (-not [string]::IsNullOrWhiteSpace($QualificationRecipe)) {
        $scenarioParameters.qualificationRecipe = $QualificationRecipe
    }
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
        fixtureFamily = $FixtureFamily
        advancedBindingManifest = if ($null -eq $advancedBinding) { $null } else { $advancedBinding.manifestPath }
        advancedInspectionRunId = $advancedInspectionRunId
        status = 'IN PROGRESS'; stage = 'request-written'; steamSafety = $steamSafety
        packagePath = $package; packageSha256 = $buildManifest.packageSha256
        transactionStatePath = $statePath; startedAtUtc = [DateTime]::UtcNow.ToString('o')
    }
    Write-KbpJsonAtomic (Join-Path $evidence 'orchestration.json') $orchestration
    $preexisting = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue | ForEach-Object Id)
    $arguments = @('-applaunch', '640820') + @(Get-KbpDisplayModeArguments -Size $displaySize) +
        @('-kbpRuntimeTestRequest', ('"' + $requestPath + '"'))
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
    # The reload scenario is the workspace scenario plus an in-game reload:
    # the same launcher input sequence (UMM dismiss, planner hotkey).
    $workspaceInputScenario = ($Scenario -ceq 'live-workspace-qual') -or ($Scenario -ceq 'live-workspace-reload') -or
        ($Scenario -ceq 'live-workspace-physical')
    $physicalInputScenario = ($Scenario -ceq 'live-ui-bootstrap') -or $workspaceInputScenario -or
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
  [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int command);
  [StructLayout(LayoutKind.Sequential)] public struct Point { public int X; public int Y; }
  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
  public static void KeyDown(IntPtr window, byte key) {
    if (window == IntPtr.Zero || !Activate(window)) throw new InvalidOperationException("Kingmaker foreground activation failed.");
    if (GetForegroundWindow() != window) throw new InvalidOperationException("Kingmaker is not the verified foreground target; refusing blind input.");
    keybd_event(key, 0, 0, UIntPtr.Zero);
    lock (HeldKeys) { if (!HeldKeys.Contains(key)) HeldKeys.Add(key); }
  }
  public static void KeyUp(byte key) {
    keybd_event(key, 0, 2, UIntPtr.Zero);
    lock (HeldKeys) { HeldKeys.Remove(key); }
  }
  // Only keys THIS harness injected are tracked; release is idempotent and
  // never touches unrelated user-owned state.
  private static readonly System.Collections.Generic.List<byte> HeldKeys = new System.Collections.Generic.List<byte>();
  public static void ReleaseTrackedKeys() {
    byte[] keys;
    lock (HeldKeys) { keys = HeldKeys.ToArray(); HeldKeys.Clear(); }
    foreach (byte key in keys) { keybd_event(key, 0, 2, UIntPtr.Zero); }
  }
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
    if (GetForegroundWindow() == window) return true;
    // Foreground activation ONLY: no synthetic shell input of any kind.
    // If the OS foreground lock refuses, delivery fails closed rather than
    // injecting keys into whichever window currently owns focus.
    return SetForegroundWindow(window);
  }
  public static void Click(IntPtr window) {
    // Revalidate ownership immediately before injection (review F7): the
    // game must still be foreground AND the cursor must still be inside
    // its client rect; focus or pointer drift aborts without clicking.
    if (window == IntPtr.Zero || GetForegroundWindow() != window)
      throw new InvalidOperationException("Kingmaker lost foreground before click; refusing blind click.");
    Point cursor;
    Rect client;
    if (!GetCursorPos(out cursor) || !GetClientRect(window, out client) ||
        !ScreenToClient(window, ref cursor))
      throw new InvalidOperationException("Kingmaker click-position verification failed.");
    if (cursor.X < 0 || cursor.Y < 0 || cursor.X > client.Right || cursor.Y > client.Bottom)
      throw new InvalidOperationException("Cursor drifted outside Kingmaker client; refusing blind click.");
    mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
    mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
  }
  // Lowercase letters and digits only, each a verified foreground key press
  // (KeyDown refuses when the game is not the foreground window).
  public static void TypeText(IntPtr window, string text) {
    if (string.IsNullOrEmpty(text) || text.Length > 32) throw new InvalidOperationException("Physical typing needs 1..32 characters.");
    foreach (char c in text) {
      byte key;
      if (c >= 'a' && c <= 'z') key = (byte)('A' + (c - 'a'));
      else if (c >= '0' && c <= '9') key = (byte)c;
      else throw new InvalidOperationException("Physical typing accepts lowercase letters and digits only.");
      KeyDown(window, key);
      System.Threading.Thread.Sleep(30);
      KeyUp(key);
      System.Threading.Thread.Sleep(70);
    }
  }
  public static void Wheel(IntPtr window, int delta) {
    // The same ownership revalidation as a click.
    if (window == IntPtr.Zero || GetForegroundWindow() != window)
      throw new InvalidOperationException("Kingmaker lost foreground before the wheel; refusing blind input.");
    Point cursor;
    Rect client;
    if (!GetCursorPos(out cursor) || !GetClientRect(window, out client) || !ScreenToClient(window, ref cursor))
      throw new InvalidOperationException("Kingmaker wheel-position verification failed.");
    if (cursor.X < 0 || cursor.Y < 0 || cursor.X > client.Right || cursor.Y > client.Bottom)
      throw new InvalidOperationException("Cursor drifted outside Kingmaker client; refusing blind wheel.");
    mouse_event(0x0800, 0, 0, unchecked((uint)delta), UIntPtr.Zero);
  }
  // The game window's own focus loss: minimized, then restored and
  // activated again. No other window is touched or activated.
  public static string FocusCycle(IntPtr window) {
    if (window == IntPtr.Zero) throw new InvalidOperationException("No Kingmaker window.");
    ShowWindow(window, 6);
    System.Threading.Thread.Sleep(1500);
    bool minimized = IsIconic(window);
    bool lostForeground = GetForegroundWindow() != window;
    ShowWindow(window, 9);
    System.Threading.Thread.Sleep(750);
    bool active = Activate(window);
    return "minimized=" + minimized + ";lostForeground=" + lostForeground + ";restored=" + !IsIconic(window) +
      ";foreground=" + active;
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
    $manualReadySeen = $false
    $manualRehearsalDoneWritten = $false
    while (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        $process.Refresh()
        if ($Scenario -ceq 'live-workspace-manual') {
            $manualReadyPath = Join-Path $evidence 'manual-ready.json'
            if (-not $manualReadySeen -and
                (Test-Path -LiteralPath $manualReadyPath -PathType Leaf)) {
                $manualReadySeen = $true
                Write-Host ("MANUAL-READY acknowledged; evidence: " + $manualReadyPath)
                if ($ManualRehearseDone) {
                    Write-Host "REHEARSAL: manual-done.json will be written 20s after manual-ready (labeled rehearsal)."
                }
            }
            if ($manualReadySeen -and $ManualRehearseDone -and
                -not $manualRehearsalDoneWritten) {
                $readyAt = (Get-Item -LiteralPath $manualReadyPath).LastWriteTimeUtc
                if ([DateTime]::UtcNow -ge $readyAt.AddSeconds(20)) {
                    $manualRehearsalDoneWritten = $true
                    [IO.File]::WriteAllText(
                        (Join-Path $evidence 'manual-done.json'),
                        '{"stage":"manual-done","by":"rehearsal"}' + [Environment]::NewLine)
                    Write-Host "REHEARSAL: manual-done.json written."
                }
            }
        }
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
        # The programmatic-umm-closed marker is a TERMINAL state for every
        # pending UMM dismissal input, not only the planner chord: a stale
        # Escape could close the candidate or open another menu after the
        # host already dismissed the overlay itself (review F7).
        if (($Scenario -ceq 'live-ui-bootstrap' -or $workspaceInputScenario) -and -not $ummDismissSent -and
            (Test-Path -LiteralPath $ummMarker -PathType Leaf) -and
            -not (Test-Path -LiteralPath (Join-Path $evidence 'programmatic-umm-closed.json') -PathType Leaf)) {
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
        if (($Scenario -ceq 'live-ui-bootstrap' -or $workspaceInputScenario) -and $ummDismissSent -and
            -not $ummDismissRecoverySent -and -not (Test-Path -LiteralPath $hotkeyMarker -PathType Leaf) -and
            [DateTime]::UtcNow -ge $ummDismissSentAtUtc.AddSeconds(2) -and
            -not (Test-Path -LiteralPath (Join-Path $evidence 'programmatic-umm-closed.json') -PathType Leaf)) {
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
                [KbpPhysicalInput]::ReleaseTrackedKeys()
                $orchestration.lastUmmRecoveryError = $_.Exception.Message
            }
        }
        if (($Scenario -ceq 'live-ui-bootstrap' -or $workspaceInputScenario) -and -not $plannerHotkeySent -and
            (Test-Path -LiteralPath $hotkeyMarker -PathType Leaf) -and
            -not (Test-Path -LiteralPath (Join-Path $evidence 'programmatic-open.json') -PathType Leaf)) {
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
                # Any key this harness still holds is released before the
                # retry; all-or-nothing is an effect, never a leaked hold.
                [KbpPhysicalInput]::ReleaseTrackedKeys()
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
                $deliveryDetail = $null
                # Typing and the focus cycle are never repeated: a retry
                # after a partial delivery would change what was delivered.
                $singleShot = @('type', 'focus-cycle') -ccontains [string]$physical.action
                $maxAttempts = if ($singleShot) { 1 } else { 3 }
                for ($attempt = 1; $attempt -le $maxAttempts -and -not $delivered; $attempt++) {
                    try {
                        if ([string]$physical.action -eq 'key-escape') {
                            [KbpPhysicalInput]::KeyDown($process.MainWindowHandle, [byte]0x1B)
                            Start-Sleep -Milliseconds 100
                            [KbpPhysicalInput]::KeyUp([byte]0x1B)
                        } elseif ([string]$physical.action -eq 'type') {
                            [KbpPhysicalInput]::TypeText($process.MainWindowHandle, [string]$physical.text)
                        } elseif ([string]$physical.action -eq 'focus-cycle') {
                            $deliveryDetail = [KbpPhysicalInput]::FocusCycle($process.MainWindowHandle)
                        } else {
                            [KbpPhysicalInput]::Move($process.MainWindowHandle,
                                [double]$physical.x, [double]$physical.y,
                                [int]$physical.unityScreenWidth, [int]$physical.unityScreenHeight)
                            Start-Sleep -Milliseconds 250
                            if ([string]$physical.action -eq 'click') {
                                [KbpPhysicalInput]::Click($process.MainWindowHandle)
                            } elseif ([string]$physical.action -eq 'wheel') {
                                [KbpPhysicalInput]::Wheel($process.MainWindowHandle, [int]$physical.delta)
                            } elseif ([string]$physical.action -ne 'hover') {
                                throw "Unknown physical input action: $($physical.action)"
                            }
                        }
                        $delivered = $true
                    }
                    catch {
                        [KbpPhysicalInput]::ReleaseTrackedKeys()
                        $deliveryError = $_.Exception.Message
                        $physicalDeliveryAttempts[$actionId]++
                        Start-Sleep -Milliseconds 250
                    }
                }
                if (-not $delivered -and ($singleShot -or [int]$physicalDeliveryAttempts[$actionId] -ge 20)) {
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
                    detail = $deliveryDetail
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
    $runSucceeded = $true
}
catch {
    # Held until the restoration and records below are done, then rethrown
    # (with any restoration failure folded in).
    $runFailure = $_
}
finally {
    try {
        # In non-interactive hosts the finally's own Write-Error can displace
        # the original terminating error from the output stream; persist the
        # pending errors first so no failure cause is ever lost.
        if (-not $runSucceeded -and @($Error).Count -gt 0 -and
            $null -ne (Get-Variable -Name evidence -ErrorAction SilentlyContinue)) {
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
        # Review of e7c5207..f7726c9, P3-3: a failed or blocked restoration
        # must not skip the protected-save comparison or the completion
        # record; it is reported after both are written.
        if ($running.Count -eq 0) {
            try { & (Join-Path $PSScriptRoot 'Restore-Local.ps1') -RunId $runId -Confirm:$false }
            catch {
                $restoreFailure = "Mods restoration failed for $runId (Restore-Local.ps1 -RunId $runId recovers it once the cause is fixed): " +
                    $_.Exception.Message
            }
        } else {
            $restoreFailure = "Kingmaker remains running; exact Mods restoration is intentionally blocked. Transaction: $runId"
        }
        if ($null -ne $restoreFailure) {
            Write-Warning $restoreFailure
            # Review of f7726c9..1332ed8, P3-A: the reason is kept beside
            # the run's evidence, not only on the console.
            try {
                if ($null -ne (Get-Variable -Name evidence -ErrorAction SilentlyContinue) -and
                    -not [string]::IsNullOrWhiteSpace([string]$evidence) -and (Test-Path -LiteralPath $evidence)) {
                    [IO.File]::WriteAllText((Join-Path $evidence 'restoration-failure.txt'), $restoreFailure + [Environment]::NewLine)
                }
            }
            catch { Write-Warning "Restoration failure not recorded: $($_.Exception.Message)" }
        }
    }
    # A display-mode run: the game's registry key back byte-exact once the game
    # exited; never a throw here (the save comparison and the completion
    # record still follow), a failure is folded into the restoration failure.
    if ($null -ne $displayRegistryBefore) {
        $displayFailure = $null
        if (@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue).Count -eq 0) {
            try {
                $displayDifferences = Restore-KbpRegistryValues -KeyPath $script:KbpGameRegistryKey `
                    -Snapshot $displayRegistryBefore
                Write-KbpJsonAtomic (Join-Path $evidence 'display-mode.json') ([ordered]@{
                    schemaVersion = 1; runId = $runId; displayMode = $DisplayMode; size = $displaySize
                    restoredValues = @($displayDifferences); restorationVerified = $true
                })
            }
            catch { $displayFailure = 'Game registry restoration failed after the display-mode run: ' + $_.Exception.Message }
        } else { $displayFailure = 'Kingmaker remains running; the game registry restoration is blocked.' }
        if ($null -ne $displayFailure) {
            Write-Warning $displayFailure
            $restoreFailure = if ($null -eq $restoreFailure) { $displayFailure } else { $restoreFailure + ' | ' + $displayFailure }
            try { [IO.File]::WriteAllText((Join-Path $evidence 'display-restoration-failure.txt'), $displayFailure + [Environment]::NewLine) }
            catch { Write-Warning "Display restoration failure not recorded: $($_.Exception.Message)" }
        }
    }
    if ($null -ne $protectedBefore -and @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue).Count -eq 0) {
        try {
            $savePolicy = Get-KbpProtectedSavePolicy -Scenario $Scenario -FixtureFamily $FixtureFamily `
                -WorkingFileName ([string]$savePair.working.fileName)
            $allowedChanged = @($savePolicy.allowedChanged)
            $violations = Compare-KbpSaveFolderSnapshot -Before $protectedBefore `
                -After (Get-KbpSaveFolderSnapshot -SaveRoot $protectedSaveRoot) `
                -AllowedChangedFileNames $allowedChanged
            # A casting qualification writes no save at all (review P3-9):
            # even the WORKING save and new autosaves count against it.
            $blocking = @($violations | Where-Object { $_ -notlike 'new:*' -or $savePolicy.newFilesBlocking })
            Write-KbpJsonAtomic (Join-Path $evidence 'protected-saves.json') ([ordered]@{
                schemaVersion = 1; runId = $runId; fixtureFamily = $FixtureFamily
                allowedChanged = @($allowedChanged)
                violations = @($violations); blocking = @($blocking)
            })
            $protectedSavesCompared = $true
            if ($blocking.Count -ne 0) {
                $protectedSaveFailure = 'Protected saves changed during the run: ' + ($blocking -join ', ')
            }
        }
        catch { $protectedSaveFailure = 'Protected-save comparison failed: ' + $_.Exception.Message }
    }
    # Review RC3: the whole-run terminal record, written last. Game-level
    # success (runtime-result.json) is kept separate: a run is complete
    # only when the harness itself succeeded, Kingmaker exited, the Mods
    # transaction was restored and verified, and the protected saves were
    # compared clean. Later gates (advanced casting) read only this record.
    if ($null -ne (Get-Variable -Name evidence -ErrorAction SilentlyContinue) -and
        -not [string]::IsNullOrWhiteSpace([string]$evidence) -and (Test-Path -LiteralPath $evidence)) {
        try {
            $completionTransaction = if ($transactionEntered) {
                Join-Path $script:KbpRuntimeStateRoot "transactions\$runId\transaction.json" } else { $null }
            $completionIdentity = if ($null -ne $compatibilityProfile) {
                Get-KbpCompatibilityIdentityDigest $compatibilityProfile } else { $null }
            $completionBinding = if ($null -eq $advancedBinding) { $null } else { [string]$advancedBinding.manifestPath }
            $completionGame = if ($null -ne $result) { [string]$result.status } else { $null }
            $completionExited = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue).Count -eq 0
            Write-KbpJsonAtomic (Join-Path $evidence 'run-completion.json') (New-KbpRunCompletionRecord `
                -RunId $runId -Scenario $Scenario -FixtureFamily $FixtureFamily -ProfileId $CompatibilityProfileId `
                -CompatibilityIdentity $completionIdentity -AdvancedBindingManifest $completionBinding `
                -SavePair $savePair -GameResultStatus $completionGame -HarnessSucceeded $runSucceeded `
                -KingmakerExited $completionExited -TransactionStatePath $completionTransaction `
                -RestoreFailure $restoreFailure `
                -ProtectedSavesCompared $protectedSavesCompared -ProtectedSaveFailure $protectedSaveFailure)
        }
        catch { Write-Warning "Run completion record not written: $($_.Exception.Message)" }
    }
}
# The run's own failure wins; a restoration failure is folded into it, and
# a restoration failure and a save violation are reported together.
if ($null -ne $runFailure) {
    if ($null -ne $restoreFailure) { throw ($runFailure.Exception.Message + ' | Restoration: ' + $restoreFailure) }
    throw $runFailure
}
if ($null -ne $restoreFailure -and $null -ne $protectedSaveFailure) {
    throw ($restoreFailure + ' | ' + $protectedSaveFailure)
}
if ($null -ne $restoreFailure) { throw $restoreFailure }
if ($null -ne $protectedSaveFailure) { throw $protectedSaveFailure }
