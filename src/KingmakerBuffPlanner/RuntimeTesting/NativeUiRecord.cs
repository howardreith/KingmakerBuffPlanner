using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Native UI finish (KBP_NATIVE_UI_FINISH_2026-10-10): what the no-input
    // live-workspace-native-ui scenario observed, judged here without Unity.
    // The "facts" stage records the native contracts and the before frames
    // (it judges only that they were captured); the "qualify" stage also
    // judges the planner's native integration.
    public sealed class NativeUiWindowSwitch
    {
        // "inventory" | "character" | "spellbook" | "key:<binding name>".
        public string Route { get; set; }
        public bool PlannerOpenBefore { get; set; }
        public bool PausedBefore { get; set; }
        public bool PauseRequested { get; set; }
        public bool PlannerOpenAfter { get; set; }
        public bool InputLeaseHeldAfter { get; set; }
        public bool NativeShownAfter { get; set; }
        public bool FullScreenModeWhileNative { get; set; }
        public bool PausedWhileNative { get; set; }
        public bool NativeReleasedAfterClose { get; set; }
        public bool FullScreenModeAfterClose { get; set; }
        public bool SelectionDisabledAfterClose { get; set; }
        public bool PausedAfterClose { get; set; }
        public bool PlannerReopened { get; set; }
        public string SelectedSourceBefore { get; set; }
        public string SelectedSourceAfterReopen { get; set; }
        public int PlannerRootsAfterReopen { get; set; }
        public string Capture { get; set; }
        public NativeUiKeyBindingObservation KeyBinding { get; set; }
    }

    public sealed class NativeUiSpellbookView
    {
        public string Unit { get; set; }
        public bool Shown { get; set; }
        public bool MetamagicPresent { get; set; }
        public bool MetamagicActive { get; set; }
        public bool MetamagicInteractable { get; set; }
        public NativeUiMetamagicAction MetamagicAction { get; set; }
        public NativeUiUnavailablePlannerAction UnavailablePlannerAction { get; set; }
        public bool PlannerButtonPresent { get; set; }
        public bool PlannerButtonInteractable { get; set; }
        public int PlannerButtonCount { get; set; }
        // Screen rectangles (x, y, width, height) of both buttons.
        public float[] MetamagicRect { get; set; }
        public float[] PlannerRect { get; set; }
        public string PlannerLabelFont { get; set; }
        public string MetamagicLabelFont { get; set; }
        public string Capture { get; set; }
    }

    public sealed class NativeUiKeyBindingObservation
    {
        public string DispatchKind { get; set; }
        public string BindingName { get; set; }
        public string Key { get; set; }
        public string CurrentMode { get; set; }
        public string BindingMode { get; set; }
        public bool KeyboardDisabled { get; set; }
        public bool InputFieldSelected { get; set; }
        public bool Paused { get; set; }
        public bool WorksWhenPaused { get; set; }
        public bool ControllerGamepad { get; set; }
    }

    public sealed class NativeUiMetamagicAction
    {
        public bool ButtonInvoked { get; set; }
        public bool ModeBefore { get; set; }
        public bool ModeWhileOpen { get; set; }
        public bool PlannerOpenWhileMetamagic { get; set; }
        public bool ReturnInvoked { get; set; }
        public bool ModeAfterReturn { get; set; }
        public bool ControllerPresentAfterReturn { get; set; }
        public bool PlannerButtonOfferedAfterReturn { get; set; }
        public string Capture { get; set; }
    }

    public sealed class NativeUiUnavailablePlannerAction
    {
        public bool ButtonInvoked { get; set; }
        public bool PlannerOpened { get; set; }
        public string UnitIdBefore { get; set; }
        public string UnitIdAfterReturn { get; set; }
        public bool NativeBookRestored { get; set; }
        public bool MetamagicStillUnavailable { get; set; }
        public bool PlannerClosedOnReturn { get; set; }
        public string Capture { get; set; }
        public string ReturnCapture { get; set; }
    }

    public sealed class NativeUiCaptionFit
    {
        public string Caption { get; set; }
        public float Size { get; set; }
        public float TextWidth { get; set; }
        public float AvailableWidth { get; set; }
    }

    public sealed class NativeUiCastingPopup
    {
        public string CastingId { get; set; }
        public string ExpectedCasterUnitId { get; set; }
        public string ActualCasterUnitId { get; set; }
        public string ExpectedAbilityGuid { get; set; }
        public string ActualAbilityGuid { get; set; }
        public string ExpectedTitle { get; set; }
        public string ActualTitle { get; set; }
        public string Presenter { get; set; }
        public bool ClosedByEscape { get; set; }
        public string DocumentBefore { get; set; }
        public string DocumentAfter { get; set; }
        public string Capture { get; set; }
    }

    public sealed class NativeUiCatalogueFilter
    {
        public string Category { get; set; }
        public string[] ExpectedSourceIds { get; set; }
        public string[] VisibleSourceIds { get; set; }
        public string DocumentBefore { get; set; }
        public string DocumentAfter { get; set; }
        public string Capture { get; set; }
    }

    public sealed class NativeUiRecord
    {
        public const string FactsStage = "facts";
        public const string QualifyStage = "qualify";

        // Earlier schemas retain their original judgements. Schema 2 adds
        // popup/caption/filter observations; schema 3 adds native action and
        // key-route preconditions. Schema 4 adds deliberately paused switching and
        // the planner action with unavailable Metamagic. Schema 5 adds the
        // human-feedback Resources modal, execution-policy/Run captions and
        // the explicit-edit/continuous-assignment gesture contract.
        // Historical records prove only what they observed.
        public int SchemaVersion { get; set; } = 5;
        public string RunId { get; set; }
        public string Stage { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public float CanvasScale { get; set; }
        public List<string> Captures { get; } = new List<string>();
        public List<string> MissingCaptures { get; } = new List<string>();
        public List<string> FactsFiles { get; } = new List<string>();
        public List<string> Notes { get; } = new List<string>();
        public List<string> Failures { get; } = new List<string>();

        // HUD (native Menu_Buttons48px and the planner's four cells).
        public int NativeHudButtons { get; set; }
        public float[] NativeHudCell { get; set; }
        public int PlannerHudButtons { get; set; }
        public float[] PlannerHudCell { get; set; }
        // Left edges of the planner cells minus the first four native
        // column lefts, in screen pixels.
        public float[] PlannerColumnOffsets { get; set; }
        public bool PlannerRowAboveNativeRows { get; set; }
        public bool PlannerFrameEnclosesRow { get; set; }
        public bool OldRailHiddenUnderRow { get; set; }
        public string PlannerHudSkin { get; set; }
        public bool PlannerHudTooltipShown { get; set; }
        public string PlannerHudTooltipTitle { get; set; }
        public int HudTopHitOwnedCells { get; set; }
        public int NativeActivationsDuringHudProbe { get; set; }
        // The rendered highlight tint of the cells in each captured state:
        // idle (the brightest of the four), Setup hovered, Long pressed.
        public float PlannerHudIdleHighlightAlpha { get; set; } = 1f;
        public float[] PlannerHudHoverHighlight { get; set; }
        public float[] PlannerHudPressedHighlight { get; set; }

        // Spellbook.
        public List<NativeUiSpellbookView> Spellbooks { get; } = new List<NativeUiSpellbookView>();
        public bool SpellbookHandoffOpenedPlanner { get; set; }
        public bool SpellbookTopRightEntryAbsent { get; set; }

        // Spell description: the native one from the spellbook, and the
        // planner's right-click.
        public bool NativePopupOpened { get; set; }
        public string NativePopupTitle { get; set; }
        public string PlannerPopupPresenter { get; set; }
        public string PlannerPopupTitle { get; set; }
        // The same spell in both: the planner's popup title read as the
        // book's (ornament markup aside).
        public bool PopupSameSpell { get; set; }
        public bool PlannerPopupClosedByEscape { get; set; }
        public bool PlannerStillOpenAfterPopupEscape { get; set; }
        // The window's own close button dismisses it as well, leaving the
        // planner open and the plan unchanged.
        public bool PlannerPopupCloseButtonFound { get; set; }
        public bool PlannerPopupClosedByCloseButton { get; set; }
        public string PlannerDocumentBeforePopup { get; set; }
        public string PlannerDocumentAfterPopup { get; set; }

        // Planner presentation.
        public bool PlannerOpened { get; set; }
        public string PlannerTitle { get; set; }
        public List<string> PlannerVisibleLabels { get; } = new List<string>();
        public string PlannerTextFonts { get; set; }
        public string SourceTabLayoutEvidence { get; set; }
        public List<NativeUiCaptionFit> SourceTabs { get; } = new List<NativeUiCaptionFit>();
        public NativeUiCastingPopup CastingPopup { get; set; }
        public List<NativeUiCatalogueFilter> CatalogueFilters { get; } = new List<NativeUiCatalogueFilter>();
        public bool ResourcesOpened { get; set; }
        public bool ResourcesTopmost { get; set; }
        public bool ResourcesCloseFocused { get; set; }
        public bool ResourcesScrollClips { get; set; }
        public float ResourcesScrollOverflow { get; set; }
        public float[] ResourcesPanelSize { get; set; }
        public string ResourcesSelectedSummary { get; set; }
        public string ResourcesAllSummary { get; set; }
        public string ResourcesSaveStatus { get; set; }
        public bool ResourcesClosedByEscape { get; set; }
        public string ResourcesDocumentBefore { get; set; }
        public string ResourcesDocumentAfter { get; set; }
        public string AvailabilityPolicyCaption { get; set; }
        public string RunCaption { get; set; }

        // Window switching and nested Escape.
        public List<NativeUiWindowSwitch> Switches { get; } = new List<NativeUiWindowSwitch>();
        public bool SearchFocusSuppressesNativeBindings { get; set; }
        public bool EscapeClosedFocusFirst { get; set; }
        public bool EscapeClosedPlannerLast { get; set; }
        public bool LeaseReleasedAtEnd { get; set; }
        public bool FullScreenModeAtEnd { get; set; }
        public bool OwnedPauseRestored { get; set; }

        // Native game log.
        public int GameLogLinesBefore { get; set; }
        public int GameLogLinesAfter { get; set; }
        public List<string> GameLogPlannerLines { get; } = new List<string>();
        public bool RoutinePressRefused { get; set; }
        // Case E: an autosave that fails while the plan file is held is
        // shown ("Not saved" with Retry save), Retry save heals it once
        // the file is free, and Undo restores the plan.
        public string SaveFailureFooter { get; set; }
        public bool SaveFailureShown { get; set; }
        public bool SaveRecoveryWorked { get; set; }
        public bool SaveFailureUndoRestored { get; set; }
        // Case C: normal source/recipient clicks continuously author while
        // explicit one-shot controls retarget an inspected casting. Every
        // edit is reversed and the plan ends exactly where it started.
        public List<string> PlanningSteps { get; set; } = new List<string>();
        public bool PlanningRestored { get; set; }
        public int RunsStarted { get; set; }

        public void AddNote(string note)
        {
            if (!string.IsNullOrEmpty(note)) Notes.Add(note);
        }

        public IList<string> Violations()
        {
            var violations = new List<string>();
            if (SchemaVersion != 1 && SchemaVersion != 2 && SchemaVersion != 3 &&
                SchemaVersion != 4 && SchemaVersion != 5) violations.Add("schema:" + SchemaVersion);
            if (Stage != FactsStage && Stage != QualifyStage) violations.Add("stage:" + (Stage ?? "null"));
            violations.AddRange(Failures.Select(value => "failure:" + value));
            violations.AddRange(MissingCaptures.Select(value => "capture-missing:" + value));
            foreach (string required in new[] { "hud", "spellbook", "native-popup" })
                if (!FactsFiles.Contains(required)) violations.Add("facts-missing:" + required);
            if (NativeHudButtons < 12) violations.Add("native-hud-buttons:" + NativeHudButtons);
            if (!NativePopupOpened) violations.Add("native-popup-not-opened");
            if (Spellbooks.Count == 0 || !Spellbooks.Any(view => view.Shown))
                violations.Add("spellbook-not-shown");
            if (!PlannerOpened) violations.Add("planner-not-opened");
            if (RunsStarted != 0) violations.Add("runs-started:" + RunsStarted);
            if (Stage == QualifyStage) violations.AddRange(QualifyViolations());
            return violations;
        }

        // The approved native integration (UI-01, UI-02, UI-07, UI-08,
        // UI-09, UI-10). Appearance itself is judged from the captures.
        private IEnumerable<string> QualifyViolations()
        {
            if (SchemaVersion >= 2)
                foreach (string violation in ObservationViolations()) yield return violation;
            if (SchemaVersion >= 3)
                foreach (string violation in NativeActionViolations()) yield return violation;
            if (SchemaVersion >= 4)
                foreach (string violation in PausedAndUnavailableViolations()) yield return violation;
            if (SchemaVersion >= 5)
                foreach (string violation in HumanFeedbackViolations()) yield return violation;
            // UI-09: four cells the size of the native cells, one per native
            // column, on a third row above the native rows, inside the frame.
            if (PlannerHudButtons != 4) yield return "hud-planner-buttons:" + PlannerHudButtons;
            if (!SameSize(NativeHudCell, PlannerHudCell, 1.5f))
                yield return "hud-cell-size:" + Describe(PlannerHudCell) + "!=" + Describe(NativeHudCell);
            if (PlannerColumnOffsets == null || PlannerColumnOffsets.Length != 4 ||
                PlannerColumnOffsets.Any(offset => Math.Abs(offset) > 1.5f))
                yield return "hud-columns:" + Describe(PlannerColumnOffsets);
            if (!PlannerRowAboveNativeRows) yield return "hud-row-not-above-native-rows";
            if (!PlannerFrameEnclosesRow) yield return "hud-frame-not-enclosing-row";
            if (!OldRailHiddenUnderRow) yield return "hud-old-rail-under-row";
            if (HudTopHitOwnedCells != 4) yield return "hud-top-hit-owned:" + HudTopHitOwnedCells;
            // The native cells' ColorTint: idle hides the hover art, hover
            // shows it, a press shows it dimmed (0.784).
            if (PlannerHudIdleHighlightAlpha > 0.05f)
                yield return "hud-idle-highlight:" + Describe(new[] { PlannerHudIdleHighlightAlpha });
            if (!SameTint(PlannerHudHoverHighlight, 1f, 1f))
                yield return "hud-hover-highlight:" + Describe(PlannerHudHoverHighlight);
            if (!SameTint(PlannerHudPressedHighlight, 0.784f, 1f))
                yield return "hud-pressed-highlight:" + Describe(PlannerHudPressedHighlight);
            if (NativeActivationsDuringHudProbe != 0)
                yield return "hud-native-activations:" + NativeActivationsDuringHudProbe;
            if (!PlannerHudTooltipShown || PlannerHudTooltipTitle != "Buff Planner")
                yield return "hud-tooltip:" + (PlannerHudTooltipTitle ?? "none");
            // UI-10: the pair sits together in the footer at one height and
            // baseline, the planner entry available independently of
            // Metamagic, the old upper-right entry gone.
            foreach (NativeUiSpellbookView view in Spellbooks.Where(value => value.Shown))
            {
                if (!view.PlannerButtonPresent || view.PlannerButtonCount != 1)
                    yield return "spellbook-planner-button:" + view.Unit + ":" + view.PlannerButtonCount;
                else if (!view.PlannerButtonInteractable)
                    yield return "spellbook-planner-button-disabled:" + view.Unit;
                if (view.MetamagicPresent && view.MetamagicActive && view.PlannerRect != null &&
                    view.MetamagicRect != null)
                {
                    if (Math.Abs(view.PlannerRect[3] - view.MetamagicRect[3]) > 1.5f ||
                        Math.Abs(view.PlannerRect[1] - view.MetamagicRect[1]) > 1.5f)
                        yield return "spellbook-pair-height-or-baseline:" + view.Unit;
                    float gap = view.PlannerRect[0] - (view.MetamagicRect[0] + view.MetamagicRect[2]);
                    if (gap < 0f || gap > 40f) yield return "spellbook-pair-gap:" + view.Unit + ":" + gap;
                }
            }
            if (!SpellbookTopRightEntryAbsent) yield return "spellbook-top-right-entry-present";
            if (!SpellbookHandoffOpenedPlanner) yield return "spellbook-handoff-did-not-open-planner";
            // UI-01: the native description window; reading changes nothing.
            if (PlannerPopupPresenter != "native-description-window")
                yield return "popup-presenter:" + (PlannerPopupPresenter ?? "none");
            if (!PlannerPopupClosedByEscape || !PlannerStillOpenAfterPopupEscape)
                yield return "popup-escape-order";
            if (!PlannerPopupClosedByCloseButton)
                yield return "popup-close-button:" + (PlannerPopupCloseButtonFound ? "did-not-close" : "not-found");
            if (PlannerDocumentBeforePopup == null || PlannerDocumentBeforePopup != PlannerDocumentAfterPopup)
                yield return "popup-changed-plan";
            if (!PopupSameSpell) yield return "popup-not-same-spell";
            // UI-07: development wording gone from the running surface.
            foreach (string retired in new[] { "Planner: Casting-first", "casting plan" })
                if (PlannerVisibleLabels.Any(label => label.IndexOf(retired, StringComparison.Ordinal) >= 0))
                    yield return "retired-label:" + retired;
            if (PlannerTitle != "Buff Planner") yield return "planner-title:" + (PlannerTitle ?? "none");
            // UI-02: a native window takes over from the planner and leaves
            // nothing behind; the planner comes back as it was.
            if (Switches.Count < 3) yield return "switch-cases:" + Switches.Count;
            // Both entries: a HUD button's route and a key binding's.
            if (!Switches.Any(item => item.Route != null && item.Route.StartsWith("key:", StringComparison.Ordinal)))
                yield return "switch-no-key-route";
            if (Switches.All(item => item.Route != null && item.Route.StartsWith("key:", StringComparison.Ordinal)))
                yield return "switch-no-hud-route";
            foreach (NativeUiWindowSwitch item in Switches)
            {
                string label = "switch:" + item.Route + ":";
                if (!item.PlannerOpenBefore) yield return label + "planner-not-open-before";
                if (item.PlannerOpenAfter || item.InputLeaseHeldAfter) yield return label + "planner-stayed";
                if (!item.NativeShownAfter) yield return label + "native-not-shown";
                if (!item.FullScreenModeWhileNative) yield return label + "native-lost-full-screen-mode";
                if (!item.NativeReleasedAfterClose || item.FullScreenModeAfterClose)
                    yield return label + "native-close-left-full-screen";
                if (item.SelectionDisabledAfterClose) yield return label + "selection-left-disabled";
                if (item.PausedAfterClose != item.PausedBefore) yield return label + "pause-changed";
                if (!item.PlannerReopened || item.PlannerRootsAfterReopen != 1)
                    yield return label + "reopen:" + item.PlannerRootsAfterReopen;
                if (item.SelectedSourceBefore != item.SelectedSourceAfterReopen)
                    yield return label + "selection-not-kept";
            }
            if (!SearchFocusSuppressesNativeBindings) yield return "search-focus-native-bindings";
            if (!SaveFailureShown) yield return "save-failure-not-shown:" + (SaveFailureFooter ?? "none");
            if (!SaveRecoveryWorked) yield return "save-recovery-failed";
            if (!SaveFailureUndoRestored) yield return "save-failure-undo";
            foreach (string step in PlanningSteps.Where(value => !value.EndsWith(":ok", StringComparison.Ordinal)))
                yield return "planning:" + step;
            if (PlanningSteps.Count < 10) yield return "planning-steps:" + PlanningSteps.Count;
            if (!PlanningRestored) yield return "planning-not-restored";
            if (!EscapeClosedFocusFirst || !EscapeClosedPlannerLast) yield return "escape-order";
            if (!LeaseReleasedAtEnd || FullScreenModeAtEnd) yield return "end-state";
            // UI-08: one concise refusal line in the native game log.
            if (!RoutinePressRefused) yield return "routine-press-not-refused";
            if (GameLogPlannerLines.Count != 1)
                yield return "game-log-planner-lines:" + GameLogPlannerLines.Count;
        }

        private IEnumerable<string> HumanFeedbackViolations()
        {
            if (!ResourcesOpened) yield return "resources:not-opened";
            if (!ResourcesTopmost) yield return "resources:not-topmost";
            if (!ResourcesCloseFocused) yield return "resources:close-not-focused";
            if (!ResourcesScrollClips) yield return "resources:scroll-not-clipped";
            if (ResourcesScrollOverflow < 0f || float.IsNaN(ResourcesScrollOverflow) ||
                float.IsInfinity(ResourcesScrollOverflow)) yield return "resources:invalid-overflow";
            if (ResourcesPanelSize == null || ResourcesPanelSize.Length != 2 ||
                ResourcesPanelSize.Any(value => value <= 0f || float.IsNaN(value) || float.IsInfinity(value)) ||
                (ScreenWidth > 0 && ResourcesPanelSize[0] > ScreenWidth + 1f) ||
                (ScreenHeight > 0 && ResourcesPanelSize[1] > ScreenHeight + 1f))
                yield return "resources:panel-out-of-bounds";
            if (string.IsNullOrWhiteSpace(ResourcesSelectedSummary))
                yield return "resources:selected-summary-missing";
            if (string.IsNullOrWhiteSpace(ResourcesAllSummary) ||
                !ResourcesAllSummary.StartsWith("All routines", StringComparison.Ordinal))
                yield return "resources:all-summary-missing";
            if (string.IsNullOrWhiteSpace(ResourcesSaveStatus)) yield return "resources:save-status-missing";
            if (!ResourcesClosedByEscape) yield return "resources:escape-did-not-close";
            if (string.IsNullOrEmpty(ResourcesDocumentBefore) ||
                ResourcesDocumentBefore != ResourcesDocumentAfter) yield return "resources:changed-plan";
            if (AvailabilityPolicyCaption != "Require full plan" &&
                AvailabilityPolicyCaption != "Cast available")
                yield return "availability-policy-caption:" + (AvailabilityPolicyCaption ?? "none");
            if (string.IsNullOrEmpty(RunCaption) ||
                !RunCaption.StartsWith("Run ", StringComparison.Ordinal))
                yield return "run-caption:" + (RunCaption ?? "none");
            foreach (string required in new[]
            {
                "arm-retarget:ok", "add-without-focus:ok", "arm-change-caster:ok", "remove-normal:ok"
            })
                if (!PlanningSteps.Contains(required)) yield return "planning-contract-missing:" + required;
        }

        // These observations support bounded integration judgements, not a
        // visual PASS, physical input dispatch, or coverage of long content.
        private IEnumerable<string> NativeActionViolations()
        {
            foreach (NativeUiWindowSwitch item in Switches.Where(value => value.Route != null &&
                    value.Route.StartsWith("key:", StringComparison.Ordinal)))
            {
                string label = "key-route:" + item.Route + ":";
                NativeUiKeyBindingObservation key = item.KeyBinding;
                if (key == null) { yield return label + "missing-observation"; continue; }
                if (key.DispatchKind != "binding-callback-forced") yield return label + "dispatch-kind";
                if (key.BindingName != item.Route.Substring(4)) yield return label + "binding-name";
                if (string.IsNullOrEmpty(key.Key) || key.Key == "None") yield return label + "unbound";
                if (string.IsNullOrEmpty(key.CurrentMode) || key.CurrentMode != key.BindingMode)
                    yield return label + "mode";
                if (key.KeyboardDisabled) yield return label + "keyboard-disabled";
                if (key.InputFieldSelected) yield return label + "input-field-selected";
                if (key.ControllerGamepad) yield return label + "controller-gamepad";
                if (key.Paused != item.PausedBefore) yield return label + "pause-observation";
                if (key.Paused && !key.WorksWhenPaused) yield return label + "does-not-work-paused";
            }
            if (!Spellbooks.Any(view => view.MetamagicAction != null)) yield return "metamagic:no-available-action";
            foreach (NativeUiSpellbookView view in Spellbooks.Where(value => value.Shown &&
                    value.MetamagicActive && value.MetamagicInteractable))
            {
                string label = "metamagic:" + view.Unit + ":";
                NativeUiMetamagicAction action = view.MetamagicAction;
                if (action == null) { yield return label + "missing-action"; continue; }
                if (!action.ButtonInvoked || action.ModeBefore || !action.ModeWhileOpen)
                    yield return label + "did-not-open";
                if (action.PlannerOpenWhileMetamagic) yield return label + "opened-planner";
                if (!action.ReturnInvoked || !action.ControllerPresentAfterReturn ||
                    action.ModeAfterReturn != action.ModeBefore) yield return label + "did-not-return";
                if (!action.PlannerButtonOfferedAfterReturn) yield return label + "planner-entry-unavailable";
            }
        }

        private IEnumerable<string> PausedAndUnavailableViolations()
        {
            if (!OwnedPauseRestored) yield return "pause:not-restored";
            foreach (bool keyRoute in new[] { false, true })
                if (!Switches.Any(item => item.PauseRequested && item.Route != null &&
                        item.Route.StartsWith("key:", StringComparison.Ordinal) == keyRoute))
                    yield return keyRoute ? "pause:no-key-route" : "pause:no-hud-route";
            foreach (NativeUiWindowSwitch item in Switches.Where(value => value.PauseRequested))
                if (!item.PausedBefore) yield return "pause:" + item.Route + ":not-paused-before";
            var unavailable = Spellbooks.Where(view => view.Shown &&
                (!view.MetamagicActive || !view.MetamagicInteractable)).ToList();
            if (unavailable.Count == 0) yield return "unavailable-metamagic:no-view";
            foreach (NativeUiSpellbookView view in unavailable)
            {
                string label = "unavailable-metamagic:" + view.Unit + ":";
                NativeUiUnavailablePlannerAction action = view.UnavailablePlannerAction;
                if (action == null) { yield return label + "missing-action"; continue; }
                if (!action.ButtonInvoked || !action.PlannerOpened) yield return label + "did-not-open-planner";
                if (!action.NativeBookRestored || !action.PlannerClosedOnReturn || !action.MetamagicStillUnavailable)
                    yield return label + "did-not-return";
                if (string.IsNullOrEmpty(action.UnitIdBefore) || action.UnitIdBefore != action.UnitIdAfterReturn)
                    yield return label + "character-changed";
            }
        }

        private IEnumerable<string> ObservationViolations()
        {
            string[] categories = { "All", "Spells", "Abilities", "Other" };
            if (SourceTabs.Count != categories.Length ||
                !SourceTabs.Select(tab => PlainTitle(tab.Caption)).OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(categories.OrderBy(value => value, StringComparer.Ordinal)))
                yield return "source-tabs:missing-captions";
            foreach (NativeUiCaptionFit tab in SourceTabs)
            {
                if (!Finite(tab.Size) || tab.Size < 10f) yield return "source-tab:" + tab.Caption + ":too-small";
                if (!Finite(tab.TextWidth) || !Finite(tab.AvailableWidth) || tab.TextWidth <= 0f ||
                    tab.AvailableWidth <= 0f || tab.TextWidth > tab.AvailableWidth + 0.05f)
                    yield return "source-tab:" + tab.Caption + ":clipped";
                if (SourceTabs.Count != 0 && Math.Abs(tab.Size - SourceTabs[0].Size) > 0.05f)
                    yield return "source-tabs:unequal-sizes";
            }
            if (CastingPopup == null) yield return "casting-popup:missing";
            else
            {
                if (CastingPopup.Presenter != "native-description-window") yield return "casting-popup:presenter";
                if (string.IsNullOrEmpty(CastingPopup.CastingId) ||
                    string.IsNullOrEmpty(CastingPopup.ExpectedCasterUnitId) ||
                    CastingPopup.ActualCasterUnitId != CastingPopup.ExpectedCasterUnitId)
                    yield return "casting-popup:caster";
                if (string.IsNullOrEmpty(CastingPopup.ExpectedAbilityGuid) ||
                    CastingPopup.ActualAbilityGuid != CastingPopup.ExpectedAbilityGuid)
                    yield return "casting-popup:ability";
                if (!SameDescriptionTitle(CastingPopup.ExpectedTitle, CastingPopup.ActualTitle))
                    yield return "casting-popup:title";
                if (!CastingPopup.ClosedByEscape) yield return "casting-popup:escape";
                if (CastingPopup.DocumentBefore == null || CastingPopup.DocumentBefore != CastingPopup.DocumentAfter)
                    yield return "casting-popup:changed-plan";
            }
            if (CatalogueFilters.Count != categories.Length ||
                !CatalogueFilters.Select(filter => filter.Category).OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(categories.OrderBy(value => value, StringComparer.Ordinal)))
                yield return "catalogue-filters:missing-categories";
            foreach (NativeUiCatalogueFilter filter in CatalogueFilters)
            {
                if (filter.ExpectedSourceIds == null || filter.VisibleSourceIds == null ||
                    !filter.ExpectedSourceIds.SequenceEqual(filter.VisibleSourceIds))
                    yield return "catalogue-filter:" + filter.Category + ":rows";
                if (filter.DocumentBefore == null || filter.DocumentBefore != filter.DocumentAfter)
                    yield return "catalogue-filter:" + filter.Category + ":changed-plan";
            }
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        // Two description titles name the same spell: their text without the
        // rich-text tags of the ornamented initial.
        public static bool SameDescriptionTitle(string native, string planner)
        {
            string left = PlainTitle(native);
            return left.Length != 0 && left == PlainTitle(planner);
        }

        private static string PlainTitle(string title)
        {
            return System.Text.RegularExpressions.Regex.Replace(title ?? string.Empty, "<[^>]*>", string.Empty).Trim();
        }

        private static bool SameTint(float[] tint, float grey, float alpha)
        {
            return tint != null && tint.Length == 4 && tint.Take(3).All(value => Math.Abs(value - grey) <= 0.05f) &&
                Math.Abs(tint[3] - alpha) <= 0.05f;
        }

        private static bool SameSize(float[] expected, float[] actual, float tolerance)
        {
            return expected != null && actual != null && expected.Length == 2 && actual.Length == 2 &&
                Math.Abs(expected[0] - actual[0]) <= tolerance && Math.Abs(expected[1] - actual[1]) <= tolerance;
        }

        private static string Describe(float[] values)
        {
            return values == null ? "null" : string.Join(",", values.Select(value =>
                value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).ToArray());
        }
    }
}
