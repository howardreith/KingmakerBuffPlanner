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
foreach ($bootstrapContract in @('[KBP-BOOT]', 'OnGUI = OnGui', 'PlannerHotkey',
        'Ctrl+Shift+B', 'BuffPlannerUiRoot.HandlePlannerHotkey')) {
    if (-not $mainSource.Contains($bootstrapContract)) {
        throw "Live bootstrap instrumentation is missing: $bootstrapContract"
    }
}
if (-not $logSource.Contains('Environment.NewLine + exception')) {
    throw 'Bootstrap exceptions must preserve complete exception text and stack traces.'
}
$assertions++

$version = Get-KbpVersion
$assemblyInfo = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\Properties\AssemblyInfo.cs') -Raw
if (-not $assemblyInfo.Contains('[assembly: AssemblyVersion("' + $version + '.0")]') -or
    -not $assemblyInfo.Contains('[assembly: AssemblyFileVersion("' + $version + '.0")]') -or
    -not $assemblyInfo.Contains('[assembly: AssemblyInformationalVersion("' + $version + '")]')) {
    throw 'CLR assembly versions do not match Version.props.'
}
$assertions++
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
$wrathUi = @($identityFiles | Select-String -Pattern `
    'ServiceWindowsPCView|SpellbookPCView|OwlcatButton|bubbly_overlay|bubble_overlay_full|BubbleBuffs')
if ($wrathUi.Count -ne 0) { throw 'Wrath/BubbleBuffs UI types, paths, or assets entered production source.' }
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
    -not $hudSource.Contains('RectTransformUtility.WorldToScreenPoint') -or
    -not $hudSource.Contains('Setup|Long|Important|Short') -or
    -not $hudSource.Contains('ValidateHitOwnership')) {
    throw 'The HUD must use an out-of-layout retained row with fresh bounded buttons and explicit top-hit ownership.'
}
$assertions++

if ($hudSource.Contains('"Feedback"') -or
    $hudSource.Contains('_feedback') -or
    $hudSource.Contains('void Present(QuickExecutionResult') -or
    $uiRootSource.Contains('_hud.Present(result)') -or
    -not $uiRootSource.Contains('_screen.Present(result)') -or
    -not $uiRootSource.Contains('Routine UI result:')) {
    throw 'Quick results must remain in the planner footer and UMM log, never a floating HUD object.'
}
$nativeLogCalls = @($identityFiles | Select-String -Pattern `
    'MessageLogThread|AddMessage\(|CombatLog|EventLog')
if ($nativeLogCalls.Count -ne 0) {
    throw 'Production source must not route quick results to a native common/combat/event log.'
}
$assertions++

$hudLifecycleSource = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\UI\BuffPlannerUiContracts.cs') -Raw
foreach ($lifecycleStateContract in @('HudInstallAttemptResult', 'HudCandidateTickResult',
        'HudInstallationState', 'RetryableNotReady', 'CandidateExpired',
        'StaleAnchor', 'HudHostingChainValidator')) {
    if (-not $hudLifecycleSource.Contains($lifecycleStateContract)) {
        throw "Explicit HUD lifecycle state contract is missing: $lifecycleStateContract"
    }
}
if ($hudSource.Contains('FindObjectOfType<IngameMenuController>') -or
    $hudSource.Contains('Resources.FindObjectsOfTypeAll') -or
    -not $hudSource.Contains('hudHost.GetComponentInChildren<IngameMenuController>(true)') -or
    -not $uiRootSource.Contains('_hud.TryInstall(hudHost)')) {
    throw 'HUD discovery must remain scoped beneath the invalidated active HUD host.'
}
$assertions++

if (-not $hudSource.Contains('Destroy(_root.gameObject)') -or
    $hudSource -match 'Destroy\s*\(\s*(_nativeCluster|_anchorController|hudHost)') {
    throw 'HUD disposal must remain bounded to Buff Planner-owned UI.'
}
$assertions++

$screenSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\BuffPlannerScreenView.cs') -Raw
foreach ($requiredScreenContract in @('CanvasGroup', 'GraphicRaycaster', 'raycastTarget = true',
        'blocksRaycasts = true', 'interactable = true', 'BUFF PLANNER',
        'CanaryEvidence', 'BuffGridView', 'PlannerSelectedBuffView')) {
    if (-not $screenSource.Contains($requiredScreenContract)) {
        throw "Full-screen raycast/visual contract is missing: $requiredScreenContract"
    }
}
foreach ($obsoleteScreenContract in @('Configured only', 'Show hidden', 'Advanced Filters',
        'CASTING SOURCE', 'Advanced Casting Source', 'Add to Long', 'Add to Important',
        'Add to Short', 'ToggleHidden', 'CycleProviderPreference', 'AdjustProviderCap')) {
    if ($screenSource.Contains($obsoleteScreenContract)) {
        throw "Obsolete planner UI remains in production: $obsoleteScreenContract"
    }
}
foreach ($retiredPrimaryLabel in @('CONFIG: ', 'DURATION: ', 'SOURCE: ', 'SORT: ',
        'HIDDEN: ', 'AVAIL: ', 'PROVIDERS AND RESOURCES', 'CAP ANY', '"MODE"')) {
    if ($screenSource.Contains($retiredPrimaryLabel)) {
        throw "Retired technical/duplicate UI label remains: $retiredPrimaryLabel"
    }
}
$viewSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\PlannerViews.cs') -Raw
$viewModelSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\PlannerScreenViewModel.cs') -Raw
$gridMetricsSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\BuffGridMetrics.cs') -Raw
$hotkeySource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\PlannerHotkey.cs') -Raw
$hotkeyBindingSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\PlannerHotkeyBinding.cs') -Raw
foreach ($workflowContract in @('PlannerSourceCategory', 'SelectedOnly',
        'SelectAllValid', 'ClearTargets')) {
    if (-not ($viewSource.Contains($workflowContract) -or $viewModelSource.Contains($workflowContract))) {
        throw "Direct assignment workflow contract is missing: $workflowContract"
    }
}
if ($screenSource.Contains('_viewModel.SetCategory(category);' + [Environment]::NewLine +
        '                RefreshCatalog(false);') -or
    $screenSource.Contains('_viewModel.ToggleSelectedOnly();' + [Environment]::NewLine +
        '                RefreshCatalog(false);')) {
    throw 'Category and Selected only callbacks must rebind their selected visual state.'
}
if ([regex]::Matches($viewSource, [regex]::Escape('"Casting mode: "')).Count -ne 1) {
    throw 'Exactly one player-facing Casting mode control must remain.'
}
foreach ($gridContract in @('ColumnCount = 4', 'PoolCapacity = 32',
        'HorizontalScrolling = false', 'Selected only',
        'Show buffs with one or more selected targets in the active routine.',
        'BuffCardGridScrollSink')) {
    if (-not ($viewSource.Contains($gridContract) -or $viewModelSource.Contains($gridContract) -or
        $gridMetricsSource.Contains($gridContract))) {
        throw "Four-column grid contract is missing: $gridContract"
    }
}
if ($screenSource.Contains('CreateDiagnosticRenderCanary') -or
    $screenSource.Contains('KBP RENDER CANARY')) {
    throw 'The temporary live render canary must not remain in production UI.'
}
foreach ($retiredSummary in @('targets covered', ' blocked')) {
    if ($viewModelSource.Contains($retiredSummary)) {
        throw "Ambiguous selected-buff summary returned: $retiredSummary"
    }
}
foreach ($hotkeyContract in @('KeyboardAccess', 'InputMatched',
        'ShouldSuppressNativeBinding', 'return false;', 'Ctrl+Shift+B')) {
    if (-not ($hotkeySource.Contains($hotkeyContract) -or
        $hotkeyBindingSource.Contains($hotkeyContract))) {
        throw "Planner hotkey native-isolation contract is missing: $hotkeyContract"
    }
}
if ($hotkeySource.Contains('KeyCode.F10') -or $mainSource.Contains('KeyCode.F10')) {
    throw 'F10 must not remain as an active planner hotkey.'
}
$assertions++

$setupModelSource = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\UI\PlannerSetupModel.cs') -Raw
$presentationSource = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\UI\PlannerPresentationModels.cs') -Raw
foreach ($casterPolicyContract in @('SetProviderEnabled', 'SetProviderMaximumCasts',
        'MoveProviderEarlier', 'MoveProviderLater',
        'ResetSelectedSourceProvidersToAutomatic', 'CASTER POLICY',
        'MAX/RUN', 'DO NOT USE', 'Planned casters:')) {
    if (-not ($setupModelSource.Contains($casterPolicyContract) -or
        $presentationSource.Contains($casterPolicyContract) -or
        $viewSource.Contains($casterPolicyContract))) {
        throw "Explicit per-buff caster-policy contract is missing: $casterPolicyContract"
    }
}
$assertions++

$plannerSource = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\Planning\CastPlanner.cs') -Raw
$sessionSource = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\UI\PlannerUiSession.cs') -Raw
if (-not $plannerSource.Contains('provider-policy-refusal') -or
    -not $sessionSource.Contains('[KBP-PLAN-DIAGNOSTIC]')) {
    throw 'Provider-policy refusals are not retained in structured UMM diagnostics.'
}
$assertions++

$variantSource = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\GameAdapters\KingmakerAbilityVariants.cs') -Raw
$actionSource = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\GameAdapters\KingmakerActionGraphAdapter.cs') -Raw
$classifierSource = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\Discovery\NativeCandidateClassifier.cs') -Raw
foreach ($variantContract in @('.IsVisible()', 'directly-owned-concrete-source',
        'native-selectable-child', 'variant-not-granted',
        'variant-native-validation-failed', 'variant-contract-unavailable')) {
    if (-not $variantSource.Contains($variantContract)) {
        throw "Native variant ownership contract is missing: $variantContract"
    }
}
if ($variantSource.Contains('.IsAvailableForCast') -or
    -not $actionSource.Contains('AlliedAreaRecipients') -or
    -not $actionSource.Contains('EnemyAreaRecipients') -or
    -not $actionSource.Contains('AmbiguousAreaRecipients') -or
    -not $classifierSource.Contains('offensive-carrier-only') -or
    -not $classifierSource.Contains('hidden-marker-only')) {
    throw 'Variant ownership must ignore transient cast availability and discovery must preserve recipient/offensive semantics.'
}
$assertions++

$factorySource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\KingmakerUiFactory.cs') -Raw
if ($screenSource.Contains('AddComponent<CanvasScaler>') -or
    -not $screenSource.Contains('ForceLayoutAndSnap(_root)') -or
    -not $factorySource.Contains('resizeTextForBestFit = false') -or
    -not $factorySource.Contains('LayoutRebuilder.ForceRebuildLayoutImmediate(root)')) {
    throw 'Planner text must use fixed native-font rendering without a nested CanvasScaler and must pixel-snap after forced layout.'
}
if (-not $factorySource.Contains('viewportImage.color = Color.white') -or
    -not $factorySource.Contains('showMaskGraphic = false') -or
    -not $factorySource.Contains('layout.childControlHeight = true') -or
    -not $factorySource.Contains('SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height)') -or
    $factorySource.Contains('new Color(1, 1, 1, 0.001f)')) {
    throw 'Scroll viewports must use an opaque hidden stencil source and explicit controlled child heights.'
}
$assertions++

$animatedAdapterSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\GameAdapters\KingmakerAnimatedCastAdapter.cs') -Raw
$materialPolicySource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\Execution\MaterialComponentAvailability.cs') -Raw
if (-not $animatedAdapterSource.Contains('MaterialComponentAvailability.IsSatisfied(') -or
    -not $materialPolicySource.Contains('if (!required) return true;') -or
    -not $animatedAdapterSource.Contains('() => resolved.Ability.HasEnoughMaterialComponent')) {
    throw 'Material sufficiency must be checked only when Kingmaker requires a consumable component.'
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

$pointerOwnershipSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\PlannerPointerOwnership.cs') -Raw
foreach ($pointerContract in @('PointerController', 'GetMethod("Tick"',
        'RectangleContainsScreenPoint', 'scope=active-planner-regions-only',
        'HarmonyPatchType.Prefix', 'm_MouseDown', 'm_MouseDrag',
        'return false', 'GetCameraScrollShiftByMouse', 'CameraPostfix',
        '__result = Vector2.zero')) {
    if (-not $pointerOwnershipSource.Contains($pointerContract)) {
        throw "Conditional physical pointer ownership is missing: $pointerContract"
    }
}
if ($pointerOwnershipSource.Contains('GetProperty("InGui"') -or
    $pointerOwnershipSource.Contains('GetMethod("get_InGui"')) {
    throw 'Exact 2.1.7b pointer ownership must not patch the metadata-less InGui getter.'
}
$mainSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\Main.cs') -Raw
foreach ($failSoftPatchContract in @('callbacks assigned;OnToggle=true',
        'Harmony pointer ownership install failed', 'HotkeyArmedByOnUpdate=true',
        'PlannerPointerOwnership.Uninstall()')) {
    if (-not $mainSource.Contains($failSoftPatchContract)) {
        throw "Pointer patch failures must preserve callback/hotkey registration and unload cleanly: $failSoftPatchContract"
    }
}
foreach ($tooltipContract in @('layout.ignoreLayout = true', 'group.blocksRaycasts = false',
        'group.interactable = false', 'ClampToScreen', '360f')) {
    if (-not $hudSource.Contains($tooltipContract)) {
        throw "Stable cached tooltip contract is missing: $tooltipContract"
    }
}
foreach ($catalogContract in @('RefreshCatalog', 'BuffGridView',
        'VisibleRows', 'SelectedDetailsBound', 'CatalogFilterDiagnostics')) {
    if (-not $screenSource.Contains($catalogContract)) {
        throw "Catalog visibility/empty-state contract is missing: $catalogContract"
    }
}
$sessionSource = Get-Content -LiteralPath (Join-Path $root 'src\KingmakerBuffPlanner\UI\PlannerUiSession.cs') -Raw
foreach ($quickFailureContract in @('MaterialReservation == null',
        'AbortUnexpectedExecution', 'unexpected execution-stage failure',
        'completedCalled')) {
    if (-not $sessionSource.Contains($quickFailureContract) -and
        -not $uiRootSource.Contains($quickFailureContract)) {
        throw "Quick execution must fail visibly and clear state: $quickFailureContract"
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

$runtimeScriptSource = Get-Content -LiteralPath (Join-Path $root 'scripts\Invoke-KingmakerRuntimeTest.ps1') -Raw
if (-not $runtimeScriptSource.Contains('physical-umm-dismiss-recovery-sent') -or
    -not $runtimeScriptSource.Contains('$ummDismissSentAtUtc.AddSeconds(2)')) {
    throw 'Live UI orchestration must include one bounded Escape-menu recovery after UMM dismissal.'
}
$assertions++

$plannerViewsSource = Get-Content -LiteralPath (Join-Path $root `
    'src\KingmakerBuffPlanner\UI\PlannerViews.cs') -Raw
if (-not $plannerViewsSource.Contains('_plan.horizontalOverflow = HorizontalWrapMode.Wrap;') -or
    -not $plannerViewsSource.Contains('_plan.verticalOverflow = VerticalWrapMode.Overflow;')) {
    throw 'Selected-buff availability and planned-use lines must remain visibly wrapped.'
}
$assertions++
foreach ($physicalContract in @('umm-overlay-ready.json',
        'physical-umm-dismiss-sent', '[byte]0x1B', 'hotkey-ready.json',
        '[byte]0x11', '[byte]0x10', '[byte]0x42',
        'physical-input-*.json', 'ClientToScreen', 'SetCursorPos',
        '[KbpPhysicalInput]::Click()')) {
    if (-not $runtimeHostSource.Contains($physicalContract) -and
        -not $runtimeScriptSource.Contains($physicalContract)) {
        throw "Live qualification must physically dismiss ShowOnStart UMM and then deliver the planner hotkey: $physicalContract"
    }
}
foreach ($livePhysicalContract in @('ui-physical-tooltip-stable',
        'ui-physical-pointer-isolation', 'ui-live-catalog-visible',
        'ui-quick-visible-results', 'SelectAndConfigureBlessForRuntime',
        'CatalogVisibleViewModels', 'PhysicalInputMovementCommandCount')) {
    if (-not $runtimeHostSource.Contains($livePhysicalContract) -and
        -not $uiRootSource.Contains($livePhysicalContract)) {
        throw "Live catalog/physical input qualification is missing: $livePhysicalContract"
    }
}
$assertions++

if (-not $runtimeHostSource.Contains('CaptureRuntimeBaseline(true);') -or
    -not $runtimeScriptSource.Contains('loadedAssemblySha256')) {
    throw 'Live qualification must capture pre-hotkey state and distinguish an exact UMM cache assembly from its primary fixture file.'
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
