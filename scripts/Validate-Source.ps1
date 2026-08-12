[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$root = Get-KbpRepositoryRoot
$required = @(
    'KingmakerBuffPlanner.sln', 'Version.props',
    'src\KingmakerBuffPlanner\KingmakerBuffPlanner.csproj',
    'src\KingmakerBuffPlanner\Info.json',
    'planning\KINGMAKER-BUFF-PLANNER-MISSION.md',
    'AUTONOMOUS-RESUME.md', 'AUTONOMOUS-BLOCKERS.md')
$assertions = 0
foreach ($relative in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
        throw "Required source file is missing: $relative"
    }
    $assertions++
}

$missionSource = Join-Path $root 'planning\CODEX-KINGMAKER-BUFF-PLANNER-AUTONOMOUS-MISSION.md'
$missionCopy = Join-Path $root 'planning\KINGMAKER-BUFF-PLANNER-MISSION.md'
if ((Get-KbpSha256 $missionSource) -cne (Get-KbpSha256 $missionCopy)) {
    throw 'Authoritative mission copy is not byte-identical.'
}
$assertions++

$mainSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\Main.cs') -Raw
$logSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\Infrastructure\ModLog.cs') -Raw
foreach ($bootstrapContract in @('[KBP-BOOT]', 'OnGUI = OnGui', 'Input.GetKeyDown(KeyCode.F10)',
        'F10 handler armed', 'BuffPlannerUiRoot.HandleF10')) {
    if (-not $mainSource.Contains($bootstrapContract)) {
        throw "Live bootstrap instrumentation is missing: $bootstrapContract"
    }
}
if (-not $logSource.Contains('Environment.NewLine + exception')) {
    throw 'Bootstrap exceptions must preserve complete exception text and stack traces.'
}
$assertions++

$version = Get-KbpVersion
$info = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\Info.json') -Raw | ConvertFrom-Json
if ($info.Id -cne 'KingmakerBuffPlanner' -or
    $info.AssemblyName -cne 'KingmakerBuffPlanner.dll' -or
    $info.EntryMethod -cne 'KingmakerBuffPlanner.Main.Load' -or
    $info.Version -cne $version -or
    $info.ManagerVersion -cne '0.28.2') {
    throw 'Info.json standalone identity or version is invalid.'
}
$assertions++

[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\KingmakerBuffPlanner.csproj') -Raw
$namespace = @{ msb = 'http://schemas.microsoft.com/developer/msbuild/2003' }
$target = Select-Xml -Xml $project -Namespace $namespace -XPath '//msb:TargetFrameworkVersion' | Select-Object -First 1 -ExpandProperty Node
$language = Select-Xml -Xml $project -Namespace $namespace -XPath '//msb:LangVersion' | Select-Object -First 1 -ExpandProperty Node
$references = @(Select-Xml -Xml $project -Namespace $namespace -XPath '//msb:Reference[msb:HintPath]' | ForEach-Object Node)
if ($target.InnerText -cne 'v4.7' -or $language.InnerText -cne '7.3') {
    throw 'Production target must remain .NET Framework 4.7 and C# 7.3.'
}
if ($references.Count -lt 3 -or @($references | Where-Object { $_.Private -cne 'False' }).Count -ne 0) {
    throw 'Every installed binary reference must explicitly disable Copy Local.'
}
$assertions += 2

$tracked = @(& git -C $root ls-files)
if ($LASTEXITCODE -ne 0) { throw 'Unable to inventory tracked files.' }
$prohibited = @($tracked | Where-Object {
    $_ -match '(?i)\.(dll|zks|sav|pfx|p12|key)$' -or
    $_ -match '(?i)(^|/)(auth\.json|credentials[^/]*)$'
})
if ($prohibited.Count -ne 0) { throw "Prohibited tracked payloads: $($prohibited -join ', ')" }
$assertions++

$identityFiles = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Recurse -File
$foreignIdentity = @($identityFiles | Select-String -Pattern 'KingmakerGunslinger|TabletopAddedRules')
if ($foreignIdentity.Count -ne 0) { throw 'Foreign product identity was found in production source.' }
$assertions++

$uiRootSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\BuffPlannerUiRoot.cs') -Raw
if ($uiRootSource -match '\bOnGUI\s*\(' -or $uiRootSource -match '\bGUILayout\b' -or
    $uiRootSource -match 'Buff Planner \(F10\)') {
    throw 'The retired floating IMGUI/text-strip HUD returned to the production UI root.'
}
$assertions++

$hudSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\BuffPlannerHudButtonController.cs') -Raw
foreach ($requiredHudContract in @('m_FormationButton', '"Setup"', '"Long"', '"Important"', '"Short"')) {
    if (-not $hudSource.Contains($requiredHudContract)) {
        throw "Native HUD control contract is missing: $requiredHudContract"
    }
}
if ($hudSource -match 'Instantiate\s*\(\s*template\.gameObject' -or
    $hudSource -match 'CreateNativeButton' -or
    -not $hudSource.Contains('icon.raycastTarget = true') -or
    -not $hudSource.Contains('rootLayout.ignoreLayout = true') -or
    -not $hudSource.Contains('Setup|Long|Important|Short') -or
    -not $hudSource.Contains('ValidateHitOwnership')) {
    throw 'The HUD must use an out-of-layout retained row with fresh bounded buttons and explicit top-hit ownership.'
}
$assertions++

$screenSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\BuffPlannerScreenView.cs') -Raw
foreach ($requiredScreenContract in @('CanvasGroup', 'GraphicRaycaster', 'raycastTarget = true',
        'blocksRaycasts = true', 'interactable = true', 'BUFF PLANNER')) {
    if (-not $screenSource.Contains($requiredScreenContract)) {
        throw "Full-screen raycast/visual contract is missing: $requiredScreenContract"
    }
}
$assertions++

$screenControllerSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\BuffPlannerScreenController.cs') -Raw
$stateSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\BuffPlannerUiContracts.cs') -Raw
if (-not $screenControllerSource.Contains('ValidatePresentation()') -or
    -not $screenControllerSource.Contains('AcquireInputLease()') -or
    $screenControllerSource.IndexOf('ValidatePresentation()', [StringComparison]::Ordinal) -gt
        $screenControllerSource.IndexOf('AcquireInputLease()', [StringComparison]::Ordinal) -or
    -not $stateSource.Contains('OpeningPresentation') -or
    -not $stateSource.Contains('FaultedRollback')) {
    throw 'Planner presentation validation must precede the transactional input lease.'
}
$assertions++

foreach ($deferredContract in @('DeferredUiReadinessGate(2)', 'candidate-awaiting-deferred-readiness')) {
    if (-not $screenControllerSource.Contains($deferredContract) -or
        -not $hudSource.Contains($deferredContract)) {
        throw "Both retained UI paths must defer readiness: $deferredContract"
    }
}
foreach ($lifecycleContract in @('ISceneHandler', 'IAreaLoadingStagesHandler',
        'IAreaActivationHandler', 'EventBus.Subscribe')) {
    if (-not $uiRootSource.Contains($lifecycleContract)) {
        throw "Live lifecycle observer is missing: $lifecycleContract"
    }
}
$assertions++

$runtimeHostSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\RuntimeTesting\RuntimeTestHost.cs') -Raw
foreach ($failureContract in @('_completed = true;',
        'Live UI runtime scenario failed.', 'TryWriteFailure(_startedAtUtc, exception)')) {
    if (-not $runtimeHostSource.Contains($failureContract)) {
        throw "Live UI failures must be committed once instead of escaping into the per-frame update loop: $failureContract"
    }
}
$assertions++

$executionSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\Execution\ExecutionModels.cs') -Raw
$sessionSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\PlannerUiSession.cs') -Raw
if ($executionSource.Contains('CastExecutionStatus.Fired') -or $sessionSource.Contains('; fired=') -or
    -not $executionSource.Contains('EffectConfirmed') -or
    -not $executionSource.Contains('TimedOutUnconfirmed')) {
    throw 'Queued/submitted casts must not be reported as applied without effect confirmation.'
}
$assertions++

$inputSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\KingmakerPlannerInputBoundary.cs') -Raw
if (-not $inputSource.Contains('IFullScreenUIHandler') -or
    -not $inputSource.Contains('GameModeType.FullScreenUi') -or
    -not $inputSource.Contains('SelectionManager')) {
    throw 'Native full-screen input isolation contract is incomplete.'
}
$assertions++

$ignored = (& git -C $root check-ignore 'GamePath.props').Trim()
if ($LASTEXITCODE -ne 0 -or $ignored -cne 'GamePath.props') {
    throw 'Machine-local GamePath.props must remain ignored.'
}
[void](Get-KbpGamePath)
$assertions++

$parseErrors = [Collections.Generic.List[System.Management.Automation.Language.ParseError]]::new()
foreach ($scriptFile in @(Get-ChildItem -LiteralPath (Join-Path $root 'scripts') -Filter '*.ps1' -File -Recurse)) {
    $tokens = $null
    $errors = $null
    [void][Management.Automation.Language.Parser]::ParseFile($scriptFile.FullName, [ref]$tokens, [ref]$errors)
    foreach ($error in @($errors)) { $parseErrors.Add($error) }
}
if ($parseErrors.Count -ne 0) { throw "PowerShell parse errors: $($parseErrors.Message -join '; ')" }
$assertions++

Write-Host "Source validation: PASS=$assertions FAIL=0"
