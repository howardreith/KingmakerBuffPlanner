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
    }

    public sealed class NativeUiSpellbookView
    {
        public string Unit { get; set; }
        public bool Shown { get; set; }
        public bool MetamagicPresent { get; set; }
        public bool MetamagicActive { get; set; }
        public bool MetamagicInteractable { get; set; }
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

    public sealed class NativeUiRecord
    {
        public const string FactsStage = "facts";
        public const string QualifyStage = "qualify";

        public int SchemaVersion { get; set; } = 1;
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
        public string PlannerDocumentBeforePopup { get; set; }
        public string PlannerDocumentAfterPopup { get; set; }

        // Planner presentation.
        public bool PlannerOpened { get; set; }
        public string PlannerTitle { get; set; }
        public List<string> PlannerVisibleLabels { get; } = new List<string>();
        public string PlannerTextFonts { get; set; }

        // Window switching and nested Escape.
        public List<NativeUiWindowSwitch> Switches { get; } = new List<NativeUiWindowSwitch>();
        public bool SearchFocusSuppressesNativeBindings { get; set; }
        public bool EscapeClosedFocusFirst { get; set; }
        public bool EscapeClosedPlannerLast { get; set; }
        public bool LeaseReleasedAtEnd { get; set; }
        public bool FullScreenModeAtEnd { get; set; }

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
        // Case C: the redesigned controls drive the plan as clicks do -
        // retarget, reorder, add, change caster, remove - each undone, and
        // the plan ends exactly where it started.
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
