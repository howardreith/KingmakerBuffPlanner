using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // What one physical-input workspace run did (mission batch 3, section
    // 10): every action is delivered by the operating system's input (the
    // harness acknowledges each request it sent through the game window),
    // and the view's own state is read after it. Programmatic callbacks
    // never count here: the workspace itself must have opened through the
    // physical planner hotkey. Judged Unity-free.
    public sealed class PhysicalWorkspaceRecord
    {
        // Every physical action the run requests, in order.
        public static readonly string[] Actions =
        {
            "ws-click-search", "ws-wheel-grid", "ws-type-query", "ws-click-tile",
            "ws-focus-cycle", "ws-click-search-again", "ws-type-more", "ws-escape"
        };

        // The casting-first (v1.2 default route) action set: one TRULY cold
        // moon click - before the planner was ever opened in this game
        // session, with the plan a previous session stored - runs Long with
        // the editor closed; the workspace then opens through the launcher's
        // physical planner hotkey, the continuous scroll answers the wheel,
        // a right-click opens the native spell description - visibly, with
        // the chip's own native text - without any mutation, the wheel over
        // it scrolls the description and never the graph (proved on long
        // native text too), and Escape closes the description first and the
        // workspace second.
        // D13: the Short routine's tab is clicked first - it holds enough
        // castings that the continuous scroll overflows, so the wheel's
        // scroll is real (every earlier run's graph fitted: no-overflow).
        public static readonly string[] CastingActions =
        {
            "cf-moon", "cf-routine-short", "cf-wheel", "cf-right-click", "cf-inspect-wheel", "cf-long-wheel",
            "cf-escape-inspect", "cf-escape-close"
        };

        // 0.4.0 (WP3) authoring run: the cold press, then the direct graph
        // gestures in place of the browse and description gestures. The tile
        // click (when the tile is on screen) and the provider change (when a
        // second caster of the buff is on screen) are conditional.
        public static readonly string[] AuthoringActions =
        {
            "cf-moon", "auth-caster", "auth-source", "auth-add", "auth-remove", "auth-readd", "auth-retarget",
            "auth-undo", "auth-focus", "auth-escape-focus", "cf-escape-close"
        };

        // The actions this run's expectation requires acknowledged.
        public IEnumerable<string> ExpectedCastingActions
        {
            get
            {
                if (MoonExpectation == "authoring") return AuthoringActions;
                if (MoonExpectation == "combat") return new[] { "cf-moon-combat" }.Concat(CastingActions);
                return CastingActions;
            }
        }

        // Parallel Short castings the run seeds so the graph overflows
        // (chips are 56 px apart; the graph viewport is ~705 px at 1080p).
        public const int GraphOverflowCastings = 14;

        // The typed query comes from the label of a tile that was NOT
        // selected (review B5): the first four letters of its first word,
        // then its fifth letter after the focus loss. Clicking that tile must
        // then change the selection.
        public static bool DeriveQuery(string label, out string query, out string suffix)
        {
            query = null;
            suffix = null;
            if (string.IsNullOrEmpty(label)) return false;
            string word = new string(label.ToLowerInvariant().TakeWhile(c => c >= 'a' && c <= 'z').ToArray());
            if (word.Length < 5) return false;
            query = word.Substring(0, 4);
            suffix = word.Substring(4, 1);
            return true;
        }

        public string Query { get; set; }
        public string QuerySuffix { get; set; }
        // The tile the query names; it was not selected before the click.
        public string TargetSource { get; set; }
        // "<width>x<height>" the launcher asked for, or null for the owner's
        // own display settings.
        public string ExpectedScreen { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public bool FullScreen { get; set; }
        // The workspace opened through the physical planner hotkey (not the
        // host's labeled programmatic fallback).
        public bool OpenedPhysically { get; set; }
        public List<string> Acknowledged { get; } = new List<string>();
        public bool SearchFocused { get; set; }
        public string SearchText { get; set; }
        // "<sourceId>|<label>|<selected>" for every tile after the query.
        public List<string> VisibleAfterQuery { get; } = new List<string>();
        // "planner" while the game mode is the one the open workspace set.
        public string ModeAfterTyping { get; set; }
        public bool GridOverflows { get; set; }
        public float? ScrollBefore { get; set; }
        public float? ScrollAfter { get; set; }
        public string SelectedBeforeClick { get; set; }
        public string SelectedAfterClick { get; set; }
        // The launcher's own report of the game window's minimize and
        // restore; the game itself may not update while minimized.
        public string FocusCycle { get; set; }
        public bool FocusRegained { get; set; }
        public bool WorkspaceOpenAfterFocus { get; set; }
        public string SearchTextAfterFocus { get; set; }
        public bool ClosedByEscape { get; set; }
        public bool LeaseReleased { get; set; }
        // The game mode after Escape; the world mode is "Default".
        public string ModeAfterClose { get; set; }
        // Casting-first evidence (v1.2 E04/E05/E06/E12).
        public bool CastingFirst { get; set; }
        // The native Escape-menu veil a stale launcher dismissal escape can
        // leave open behind the workspace; it absorbs HUD clicks, so the
        // scenario closes it physically before the cold moon. Diagnostics:
        // whether it was open, and whether the escapes closed it. Never a
        // violation by itself (the scenario fails outright when it cannot).
        public bool? MenuVeilOpenBeforeMoon { get; set; }
        public bool MenuVeilClosedByEscape { get; set; }
        // The moon click ran Long while the editor stayed closed.
        public bool MoonRunStarted { get; set; }
        public bool MoonWorkspaceStayedClosed { get; set; }
        // The moon-run contract of this run: "cast" (an allowance-bound
        // grant must let the press run Long once) or "select" (no grant is
        // armed; the press must be REFUSED by the session lock and the
        // approved plan digest is published for the allowance).
        public string MoonExpectation { get; set; }
        // 0.4.0 (WP4) combat run: the party member held in combat (no enemy;
        // the group's leave timer kept at zero), whether the party was in
        // combat before the stimulus and at the press, how many updates held
        // it, and the press's outcome: no run, the editor never opened, no
        // dispatch refusal (the combat refusal precedes the native boundary),
        // the player-facing refusal, the Long source's availability
        // ("before>after") and effect, and the state cleared before the
        // ordinary selection press.
        // 0.4.2 (B) "removal": the spell removed from its book through the
        // game's own RemoveSpell (known before, not after), the other spell
        // whose level was spent through the game's own spend (slots before
        // and after, still known), the castings authored, what the HUD press
        // removed, what the stored plan holds after it, the archive, the
        // notice and whether the opened planner's footer shows it.
        public List<string> RemovalAuthored { get; } = new List<string>();
        public string RemovalKnownCaster { get; set; }
        public string RemovalKnownBook { get; set; }
        public string RemovalKnownSpell { get; set; }
        public string RemovalKnownName { get; set; }
        public bool? RemovalKnownBefore { get; set; }
        public bool? RemovalKnownAfter { get; set; }
        public string RemovalSpentCaster { get; set; }
        public string RemovalSpentBook { get; set; }
        public string RemovalSpentSpell { get; set; }
        public string RemovalSpentName { get; set; }
        public int? RemovalSpentLevel { get; set; }
        public int? RemovalSpentSlotsBefore { get; set; }
        public int? RemovalSpentSlotsAfter { get; set; }
        public bool? RemovalSpentKnownAfter { get; set; }
        public string RemovalStatus { get; set; }
        public string RemovalNotice { get; set; }
        public bool RemovalDurable { get; set; }
        public List<string> RemovalRemoved { get; } = new List<string>();
        public bool RemovalArchived { get; set; }
        public List<string> RemovalStored { get; } = new List<string>();
        public string RemovalFooter { get; set; }
        public bool RemovalNoticeShown { get; set; }
        // The reconciled plan the ordinary run evaluated, the exhausted
        // spell's casting still blocking, every other casting's saved intent
        // and order unchanged, and no resource pool changed by the press.
        public List<string> RemovalLongPlan { get; } = new List<string>();
        public string RemovalSpentReadiness { get; set; }
        public bool RemovalIntentKept { get; set; }
        public string RemovalIntentDiff { get; set; }
        public string RemovalPoolsBefore { get; set; }
        public string RemovalPoolsAfter { get; set; }

        public string CombatUnitId { get; set; }
        public bool? CombatInCombatBefore { get; set; }
        public int CombatHeldUpdates { get; set; }
        public bool? CombatAtPress { get; set; }
        public bool? CombatRunStarted { get; set; }
        public bool? CombatEditorOpened { get; set; }
        public int? CombatDispatchRefusals { get; set; }
        public string CombatRefusal { get; set; }
        public string CombatAvailability { get; set; }
        public bool? CombatEffectAfter { get; set; }
        public bool? CombatClearedBeforeMoon { get; set; }
        // rc4 review finding 2: the Classic route pressed in the same held
        // combat - refused at its admission, before any preparation.
        public string ClassicCombatRefusal { get; set; }
        public int? ClassicCombatYielded { get; set; }
        public int? ClassicCombatRefreshes { get; set; }
        public int? ClassicCombatPreviews { get; set; }
        public bool? ClassicCombatReportChanged { get; set; }
        public bool? ClassicCombatExecuting { get; set; }
        public bool? ClassicCombatProfileUnchanged { get; set; }
        // 0.4.0 (WP3) authoring run: the chosen buff, caster and recipients
        // ("A,B") in the shown routine, and each gesture's observed result.
        public string AuthoringRoutine { get; set; }
        public string AuthoringSource { get; set; }
        public string AuthoringCaster { get; set; }
        public string AuthoringTargets { get; set; }
        public bool? AuthoringBuffSelected { get; set; }
        public int? AuthoringCountBefore { get; set; }
        public int? AuthoringCountAfter { get; set; }
        public bool? AuthoringAdded { get; set; }
        public bool? AuthoringRemoved { get; set; }
        public bool? AuthoringReadded { get; set; }
        public bool? AuthoringRetargeted { get; set; }
        public bool? AuthoringProviderChanged { get; set; }
        public bool? AuthoringProviderKeptTarget { get; set; }
        public bool? AuthoringProviderUndone { get; set; }
        public bool? AuthoringUndoRestored { get; set; }
        public bool? AuthoringFocusedBeforeEscape { get; set; }
        public bool? AuthoringEscapeClearedFocus { get; set; }
        public bool? AuthoringOpenAfterFirstEscape { get; set; }
        public bool? AuthoringDurable { get; set; }
        // The refusal the moon press produced (selection runs), and the
        // allowance/grant evidence (cast runs).
        public string MoonRefusal { get; set; }
        public string MoonAllowanceStatus { get; set; }
        public string MoonGrantDescribe { get; set; }
        public bool? MoonGrantConsumed { get; set; }
        public int? MoonGrantAttempts { get; set; }
        // E12 cold start: before the moon click no planner session existed
        // and the editor had never been opened in this game session; the
        // plan was stored by an earlier session (Long AND Important
        // castings, Important authored last).
        public bool? ColdSessionBeforeMoon { get; set; }
        public bool? EditorNeverOpenedBeforeMoon { get; set; }
        public List<string> SeedLongCastings { get; } = new List<string>();
        public List<string> SeedImportantCastings { get; } = new List<string>();
        // D13: the Short routine's seeded castings (never run by the moon).
        public List<string> SeedShortCastings { get; } = new List<string>();
        // The Short tab was selected by the physical click (browsing only).
        public bool? RoutineSelected { get; set; }
        public int GraphCastingsShown { get; set; } = -1;
        // The moon run itself (cast runs): runs started by the press after a
        // settle (exactly one), the run's scope, terminal, submissions and
        // per-casting outcomes ("<id>=<state>").
        public int? MoonRunsStarted { get; set; }
        public string MoonRunRoutine { get; set; }
        public string MoonRunTerminal { get; set; }
        public int? MoonRunSubmitted { get; set; }
        public int? MoonRunResourcesSpent { get; set; }
        public List<string> MoonRunEntries { get; } = new List<string>();
        // Native reads around the press: the Long casting's target has its
        // effect afterwards (it had none before); the Important casting's
        // target still has none; the Long casting's own source availability
        // ("<before>><after>") moved by exactly the run's spend.
        public bool? LongEffectBefore { get; set; }
        public bool? LongEffectAfter { get; set; }
        public bool? ImportantEffectBefore { get; set; }
        public bool? ImportantEffectAfter { get; set; }
        public string LongSourceAvailability { get; set; }
        public bool LongSourceFree { get; set; }
        // After the final Escape: the game's own Escape menu must not be
        // open (the planner took that Escape), and the game mode is the one
        // before the planner opened.
        public string ModeBeforeOpen { get; set; }
        public bool? EscMenuOpenAfterClose { get; set; }
        // The continuous scroll answered the physical wheel.
        public bool GraphOverflow { get; set; }
        public float? GraphScrollBefore { get; set; }
        public float? GraphScrollAfter { get; set; }
        public string GraphWheelEvidence
        {
            get
            {
                if (GraphScrollBefore == null || GraphScrollAfter == null) return "unread";
                bool moved = Math.Abs(GraphScrollAfter.Value - GraphScrollBefore.Value) >= 0.001f;
                if (!GraphOverflow) return moved ? "moved-without-overflow" : "not-applicable:no-overflow";
                return moved ? "scrolled" : "no-scroll";
            }
        }
        // The right-click opened the native spell inspect; no document
        // revision was created by browsing or inspecting.
        public string InspectChip { get; set; }
        public bool InspectOpened { get; set; }
        public bool InspectClosedByEscape { get; set; }
        // What the player saw (D11): the panel's on-screen size in pixels
        // (zero when it was partly off screen), its title, and whether its
        // body is exactly the chip's native description read independently.
        public float? InspectPanelWidth { get; set; }
        public float? InspectPanelHeight { get; set; }
        public string InspectTitle { get; set; }
        public int InspectBodyChars { get; set; } = -1;
        public int InspectExpectedChars { get; set; } = -1;
        public bool? InspectBodyNative { get; set; }
        // D12: the title names the spell as the game does (it showed the
        // provider key's last segment, "heighten-0", in r14).
        public bool? InspectTitleNative { get; set; }
        // The chip's own description: overflow (content minus viewport) and
        // the scroll position around the physical wheel over it.
        public float InspectOverflow { get; set; }
        public float? InspectScrollBefore { get; set; }
        public float? InspectScrollAfter { get; set; }
        // The labelled long-text probe in the same open panel: every distinct
        // native description joined, then the physical wheel.
        public int LongProbeChars { get; set; } = -1;
        public float LongProbeOverflow { get; set; }
        public float? LongScrollBefore { get; set; }
        public float? LongScrollAfter { get; set; }
        // Neither wheel closed the description or moved the graph beneath.
        public bool? InspectOpenAfterWheels { get; set; }
        public float? GraphScrollUnderInspectBefore { get; set; }
        public float? GraphScrollUnderInspectAfter { get; set; }
        // WP7 paper evidence, diagnostic only (never a violation: the
        // readable flat fallback is a legitimate outcome): which scroll
        // donors were borrowed and what the workspace frame, its rules and
        // the open spell scroll drew - layer scale, canvas reference, drawn
        // borders, size - and the description's input policy.
        public string WorkspacePaperEvidence { get; set; }
        public string InspectPaperEvidence { get; set; }

        // The description's own judgement (E05/E06), Unity-free.
        public IList<string> InspectViolations()
        {
            var violations = new List<string>();
            if (InspectPanelWidth == null || InspectPanelHeight == null ||
                InspectPanelWidth.Value < 0.25f * ScreenWidth || InspectPanelHeight.Value < 0.25f * ScreenHeight)
                violations.Add("inspect:panel-not-visible:" + (InspectPanelWidth == null ? "unread"
                    : InspectPanelWidth.Value.ToString("0") + "x" + InspectPanelHeight.Value.ToString("0")));
            if (string.IsNullOrWhiteSpace(InspectTitle)) violations.Add("inspect:title-empty");
            else if (InspectTitleNative != true) violations.Add("inspect:title-not-spell-name:" + InspectTitle);
            if (InspectBodyNative != true)
                violations.Add("inspect:body-not-native:" + InspectBodyChars + "/" + InspectExpectedChars);
            if (InspectOverflow > 1f && (InspectScrollBefore == null || InspectScrollAfter == null ||
                InspectScrollAfter.Value >= InspectScrollBefore.Value - 0.001f))
                violations.Add("inspect:overflowing-description-not-scrolled");
            if (LongProbeChars <= 0) violations.Add("inspect:long-probe-empty");
            if (LongProbeOverflow <= 1f)
                violations.Add("inspect:long-text-does-not-overflow:" + LongProbeOverflow.ToString("0.#"));
            if (LongScrollBefore == null || LongScrollAfter == null ||
                LongScrollAfter.Value >= LongScrollBefore.Value - 0.001f)
                violations.Add("inspect:long-text-not-scrolled:" + LongScrollBefore + ">" + LongScrollAfter);
            if (InspectOpenAfterWheels != true) violations.Add("inspect:closed-by-wheel");
            if (GraphScrollUnderInspectBefore == null || GraphScrollUnderInspectAfter == null ||
                Math.Abs(GraphScrollUnderInspectAfter.Value - GraphScrollUnderInspectBefore.Value) >= 0.001f)
                violations.Add("inspect:wheel-moved-graph:" + GraphScrollUnderInspectBefore + ">" +
                    GraphScrollUnderInspectAfter);
            return violations;
        }
        public string DocumentSignatureBeforeBrowse { get; set; }
        public string DocumentSignatureAfterInspect { get; set; }
        // The world-input isolation probe over the whole sequence.
        public int PlayerCommands { get; set; }
        public int MovementCommands { get; set; }
        public int AbilityCommands { get; set; }
        public int SelectionEvents { get; set; }
        public int AbilityTargetEvents { get; set; }
        public bool SelectionUnchanged { get; set; }
        public bool CameraUnchanged { get; set; }
        public List<string> Failures { get; } = new List<string>();
        // Diagnostic notes (seed outcome, etc.) - evidence only, never a
        // violation by themselves.
        public List<string> Notes { get; } = new List<string>();

        public void AddNote(string note)
        {
            if (!string.IsNullOrEmpty(note)) Notes.Add(note);
        }

        // What the wheel proved: "scrolled", or "not-applicable:no-overflow"
        // when the grid's content fits (the wheel was delivered; there was
        // nothing to scroll), or "no-scroll".
        public string WheelEvidence
        {
            get
            {
                if (ScrollBefore == null || ScrollAfter == null) return "unread";
                bool moved = Math.Abs(ScrollAfter.Value - ScrollBefore.Value) >= 0.001f;
                if (!GridOverflows) return moved ? "moved-without-overflow" : "not-applicable:no-overflow";
                return moved ? "scrolled" : "no-scroll";
            }
        }

        // The cold moon press (v1.2 E12). Both expectations: it was truly
        // cold (no planner session, the editor never opened this game
        // session), the stored plan held Long AND Important castings, and
        // the editor stayed closed. A selection run's press is refused BY
        // THE LOCK (no grant) and changes nothing. A cast run's press ran
        // exactly one run - Long, completed, its castings and only its
        // castings, each confirmed - under its consumed single-use grant
        // (one attempt); the Long target gained its effect, the Important
        // target did not; the Long source moved by exactly the run's spend.
        internal IList<string> MoonViolations()
        {
            var violations = new List<string>();
            if (ColdSessionBeforeMoon != true)
                violations.Add("moon:not-cold:session-" + (ColdSessionBeforeMoon == null ? "unread" : "existed"));
            if (EditorNeverOpenedBeforeMoon != true)
                violations.Add("moon:not-cold:editor-" + (EditorNeverOpenedBeforeMoon == null ? "unread" : "opened"));
            if (SeedLongCastings.Count == 0 || SeedImportantCastings.Count == 0)
                violations.Add("moon:seed:long=" + SeedLongCastings.Count + ";important=" + SeedImportantCastings.Count);
            if (!MoonWorkspaceStayedClosed) violations.Add("moon:editor-opened");
            if (LongEffectBefore != false || ImportantEffectBefore != false)
                violations.Add("moon:effects-present-before:long=" + LongEffectBefore + ";important=" +
                    ImportantEffectBefore);
            if (MoonExpectation == "select" || MoonExpectation == "combat" || MoonExpectation == "authoring" ||
                MoonExpectation == "removal")
            {
                // No grant exists, so the press must be refused BY THE LOCK
                // (a run without an allowance would be a native-casting lock
                // failure, never a pass) - and nothing may land.
                if (MoonRunStarted) violations.Add("moon:run-without-grant");
                else if (string.IsNullOrEmpty(MoonRefusal) ||
                    MoonRefusal.IndexOf("native-submission-disabled", StringComparison.Ordinal) < 0)
                    violations.Add("moon:not-refused-by-lock:" + (MoonRefusal ?? "none"));
                if (LongEffectAfter != false || ImportantEffectAfter != false)
                    violations.Add("moon:refused-press-landed:long=" + LongEffectAfter + ";important=" +
                        ImportantEffectAfter);
                return violations;
            }
            if (!MoonRunStarted) violations.Add("moon:no-run");
            if (MoonRunsStarted != 1) violations.Add("moon:runs:" + (MoonRunsStarted == null ? "unread" : MoonRunsStarted.ToString()));
            if (MoonGrantConsumed != true) violations.Add("moon:grant-not-consumed");
            if (MoonGrantAttempts != 1) violations.Add("moon:grant-attempts:" + (MoonGrantAttempts == null ? "unread" : MoonGrantAttempts.ToString()));
            if (MoonRunRoutine != "long") violations.Add("moon:routine:" + (MoonRunRoutine ?? "none"));
            if (MoonRunTerminal != "completed") violations.Add("moon:terminal:" + (MoonRunTerminal ?? "none"));
            if (MoonRunSubmitted != SeedLongCastings.Count)
                violations.Add("moon:submitted:" + (MoonRunSubmitted == null ? "unread" : MoonRunSubmitted.ToString()) +
                    "!=" + SeedLongCastings.Count);
            foreach (string id in SeedLongCastings)
                if (!MoonRunEntries.Contains(id + "=EffectConfirmed")) violations.Add("moon:long-not-confirmed:" + id);
            foreach (string entry in MoonRunEntries)
                if (SeedImportantCastings.Concat(SeedShortCastings)
                        .Any(id => entry.StartsWith(id + "=", StringComparison.Ordinal)))
                    violations.Add("moon:routine-crossover:" + entry);
            if (LongEffectAfter != true) violations.Add("moon:long-effect-missing");
            if (ImportantEffectAfter != false) violations.Add("moon:important-ran");
            string[] availability = (LongSourceAvailability ?? string.Empty).Split('>');
            int before;
            int after;
            int spent = MoonRunResourcesSpent ?? -1;
            if (availability.Length != 2 || !int.TryParse(availability[0], out before) ||
                !int.TryParse(availability[1], out after) || spent < 0 ||
                before - after != (LongSourceFree ? 0 : spent) || (LongSourceFree && spent != 0))
                violations.Add("moon:cost:" + (LongSourceAvailability ?? "unread") + ";spent=" + spent +
                    ";free=" + LongSourceFree);
            return violations;
        }

        // 0.4.0 (WP4): the press in combat was refused for combat, before
        // anything reached the native boundary, and changed nothing; the
        // state was cleared before the ordinary press (judged by
        // MoonViolations as a selection press).
        internal IList<string> CombatViolations()
        {
            var violations = new List<string>();
            if (CombatInCombatBefore != false)
                violations.Add("combat:party-already-in-combat:" + (CombatInCombatBefore == null ? "unread" : "true"));
            if (CombatAtPress != true) violations.Add("combat:not-in-combat-at-press");
            if (CombatRunStarted != false) violations.Add("combat:run-started");
            if (CombatEditorOpened != false) violations.Add("combat:editor-opened");
            if (CombatDispatchRefusals != 0)
                violations.Add("combat:reached-dispatch:" + (CombatDispatchRefusals == null ? "unread"
                    : CombatDispatchRefusals.ToString()));
            if (string.IsNullOrEmpty(CombatRefusal) ||
                CombatRefusal.IndexOf(UI.CastingRunPresentation.CombatRefusalText, StringComparison.Ordinal) < 0)
                violations.Add("combat:not-refused-for-combat:" + (CombatRefusal ?? "none"));
            string[] availability = (CombatAvailability ?? string.Empty).Split('>');
            if (availability.Length != 2 || availability[0] != availability[1])
                violations.Add("combat:resource-changed:" + (CombatAvailability ?? "unread"));
            if (CombatEffectAfter != false) violations.Add("combat:effect-landed");
            if (CombatClearedBeforeMoon != true) violations.Add("combat:not-cleared-before-moon");
            if (string.IsNullOrEmpty(ClassicCombatRefusal) ||
                ClassicCombatRefusal.IndexOf(UI.CastingRunPresentation.CombatRefusalText, StringComparison.Ordinal) < 0)
                violations.Add("combat:classic-not-refused-for-combat:" + (ClassicCombatRefusal ?? "none"));
            if (ClassicCombatYielded != 0)
                violations.Add("combat:classic-yielded:" + (ClassicCombatYielded == null ? "unread" : ClassicCombatYielded.ToString()));
            if (ClassicCombatRefreshes != 0)
                violations.Add("combat:classic-refreshed:" + (ClassicCombatRefreshes == null ? "unread" : ClassicCombatRefreshes.ToString()));
            if (ClassicCombatPreviews != 0)
                violations.Add("combat:classic-previewed:" + (ClassicCombatPreviews == null ? "unread" : ClassicCombatPreviews.ToString()));
            if (ClassicCombatReportChanged != false) violations.Add("combat:classic-report-changed");
            if (ClassicCombatExecuting != false) violations.Add("combat:classic-executing");
            if (ClassicCombatProfileUnchanged != true) violations.Add("combat:classic-profile-changed");
            return violations;
        }

        // 0.4.0 (WP3): each physical graph gesture did exactly what the
        // direct-manipulation contract says, the edits saved themselves, and
        // Escape left the focused casting before it closed the planner. The
        // provider change is judged when it was exercised.
        internal IList<string> AuthoringViolations()
        {
            var violations = new List<string>();
            if (string.IsNullOrEmpty(AuthoringSource) || (AuthoringTargets ?? string.Empty).Split(',').Length != 2)
                violations.Add("authoring:not-chosen");
            if (AuthoringBuffSelected != true) violations.Add("authoring:buff-not-selected");
            if (AuthoringAdded != true) violations.Add("authoring:add");
            if (AuthoringRemoved != true) violations.Add("authoring:same-portrait-remove");
            if (AuthoringReadded != true) violations.Add("authoring:readd");
            if (AuthoringRetargeted != true) violations.Add("authoring:retarget");
            if (AuthoringProviderChanged.HasValue &&
                (AuthoringProviderChanged != true || AuthoringProviderKeptTarget != true ||
                 AuthoringProviderUndone != true))
                violations.Add("authoring:provider-change:changed=" + AuthoringProviderChanged + ";kept=" +
                    AuthoringProviderKeptTarget + ";undone=" + AuthoringProviderUndone);
            if (AuthoringUndoRestored != true) violations.Add("authoring:undo");
            if (AuthoringFocusedBeforeEscape != true) violations.Add("authoring:card-focus");
            if (AuthoringEscapeClearedFocus != true || AuthoringOpenAfterFirstEscape != true)
                violations.Add("authoring:escape-did-not-leave-focus-first");
            if (AuthoringDurable != true) violations.Add("authoring:not-saved");
            if (AuthoringCountBefore == null || AuthoringCountAfter != AuthoringCountBefore + 1)
                violations.Add("authoring:count:" + AuthoringCountBefore + ">" + AuthoringCountAfter);
            return violations;
        }

        // 0.4.2 (B): the native removal took exactly the removed spell's
        // castings (one archived, saved edit with its notice, shown in the
        // planner) while the spell whose slots were all spent kept its own.
        private IEnumerable<string> RemovalViolations()
        {
            var violations = new List<string>();
            var expectedRemoved = new[] { "rm-known-important", "rm-known-long" };
            if (RemovalAuthored.Count != 3) violations.Add("removal:authored:" + RemovalAuthored.Count);
            if (RemovalKnownBefore != true || RemovalKnownAfter != false)
                violations.Add("removal:native-remove:" + RemovalKnownBefore + ">" + RemovalKnownAfter);
            if (RemovalSpentSlotsBefore == null || RemovalSpentSlotsBefore <= 0 || RemovalSpentSlotsAfter != 0 ||
                RemovalSpentKnownAfter != true)
                violations.Add("removal:native-spend:" + RemovalSpentSlotsBefore + ">" + RemovalSpentSlotsAfter +
                    ";known=" + RemovalSpentKnownAfter);
            if (RemovalStatus != "Applied" || !RemovalDurable)
                violations.Add("removal:outcome:" + (RemovalStatus ?? "none") + ";durable=" + RemovalDurable);
            if (!RemovalRemoved.SequenceEqual(expectedRemoved))
                violations.Add("removal:removed:" + string.Join(",", RemovalRemoved.ToArray()));
            if (RemovalStored.Any(id => expectedRemoved.Contains(id)) || !RemovalStored.Contains("rm-spent-important") ||
                SeedLongCastings.Concat(SeedImportantCastings).Concat(SeedShortCastings)
                    .Any(id => !RemovalStored.Contains(id)))
                violations.Add("removal:stored:" + string.Join(",", RemovalStored.ToArray()));
            if (!RemovalArchived) violations.Add("removal:not-archived");
            if (RemovalNotice == null || RemovalNotice.IndexOf("no longer known", StringComparison.Ordinal) < 0 ||
                RemovalNotice.IndexOf("Undo available.", StringComparison.Ordinal) < 0)
                violations.Add("removal:notice:" + (RemovalNotice ?? "none"));
            if (!RemovalNoticeShown) violations.Add("removal:notice-not-shown:" + (RemovalFooter ?? "none"));
            if (!RemovalLongPlan.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(
                    SeedLongCastings.OrderBy(id => id, StringComparer.Ordinal)))
                violations.Add("removal:long-plan:" + string.Join(",", RemovalLongPlan.ToArray()));
            if (RemovalSpentReadiness == null ||
                !RemovalSpentReadiness.StartsWith("Blocked:", StringComparison.Ordinal))
                violations.Add("removal:spent-not-blocking:" + (RemovalSpentReadiness ?? "none"));
            if (!RemovalIntentKept) violations.Add("removal:intent-changed:" + (RemovalIntentDiff ?? "unread"));
            if (string.IsNullOrEmpty(RemovalPoolsBefore) || RemovalPoolsAfter != RemovalPoolsBefore)
                violations.Add("removal:resources-changed-by-press");
            return violations;
        }

        public IList<string> Violations()
        {
            var violations = new List<string>(Failures);
            string screen = ScreenWidth + "x" + ScreenHeight;
            if (!string.IsNullOrEmpty(ExpectedScreen) && ExpectedScreen != screen)
                violations.Add("screen:" + screen + "!=" + ExpectedScreen);
            if (!OpenedPhysically) violations.Add("workspace-opened-programmatically");
            if (CastingFirst)
            {
                foreach (string action in ExpectedCastingActions)
                    if (!Acknowledged.Contains(action))
                        violations.Add("unacknowledged:" + action);
                violations.AddRange(MoonViolations());
                if (MoonExpectation == "combat") violations.AddRange(CombatViolations());
                if (MoonExpectation == "removal") violations.AddRange(RemovalViolations());
                if (MoonExpectation == "authoring") violations.AddRange(AuthoringViolations());
                else
                {
                    // D13: the continuous scroll must really scroll - a graph
                    // that fits is no longer an acceptable outcome here.
                    if (SeedShortCastings.Count < GraphOverflowCastings)
                        violations.Add("graph-seed:short=" + SeedShortCastings.Count);
                    if (RoutineSelected != true) violations.Add("routine-tab:not-selected");
                    if (GraphWheelEvidence != "scrolled") violations.Add("graph-scroll:" + GraphWheelEvidence);
                    if (!InspectOpened) violations.Add("inspect:not-opened");
                    else violations.AddRange(InspectViolations());
                    if (!InspectClosedByEscape) violations.Add("inspect:not-closed");
                    if (DocumentSignatureBeforeBrowse == null ||
                        DocumentSignatureAfterInspect == null ||
                        DocumentSignatureBeforeBrowse != DocumentSignatureAfterInspect)
                        violations.Add("inspect:document-mutated");
                }
                // The final state (E06/E12): the planner took its closing
                // Escape (the game's menu is not open; the game is back in
                // the mode it was in before the planner opened), its input
                // lease is released, and nothing reached the world.
                if (!ClosedByEscape) violations.Add("escape-did-not-close");
                if (!LeaseReleased) violations.Add("lease-held-after-close");
                if (EscMenuOpenAfterClose != false)
                    violations.Add("escape:native-menu-opened:" + (EscMenuOpenAfterClose == null ? "unread" : "open"));
                if (string.IsNullOrEmpty(ModeBeforeOpen) || ModeAfterClose != ModeBeforeOpen)
                    violations.Add("mode-after-close:" + (ModeAfterClose ?? "none") + "!=" + (ModeBeforeOpen ?? "none"));
                if (PlayerCommands != 0 || MovementCommands != 0 || AbilityCommands != 0 ||
                    SelectionEvents != 0 || AbilityTargetEvents != 0 || !SelectionUnchanged)
                    violations.Add("world-input-leaked:commands=" + PlayerCommands + "/" + MovementCommands + "/" +
                        AbilityCommands + ";events=" + SelectionEvents + "/" + AbilityTargetEvents +
                        ";selectionUnchanged=" + SelectionUnchanged);
                return violations;
            }
            foreach (string action in Actions)
                if (!Acknowledged.Contains(action)) violations.Add("unacknowledged:" + action);
            if (string.IsNullOrEmpty(Query) || Query.Length != 4 || string.IsNullOrEmpty(QuerySuffix))
                violations.Add("query-not-derived");
            if (!SearchFocused) violations.Add("search-not-focused");
            if (Query == null || SearchText != Query) violations.Add("typed:" + (SearchText ?? "none"));
            if (VisibleAfterQuery.Count == 0) violations.Add("no-visible-tile");
            foreach (string tile in VisibleAfterQuery)
            {
                string[] parts = tile.Split('|');
                bool selected = parts.Length == 3 && parts[2] == "True";
                bool matches = parts.Length == 3 && Query != null &&
                    parts[1].IndexOf(Query, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!selected && !matches) violations.Add("unfiltered:" + tile);
            }
            if (TargetSource == null || !VisibleAfterQuery.Any(tile => tile.StartsWith(TargetSource + "|",
                    StringComparison.Ordinal)))
                violations.Add("target-not-shown:" + (TargetSource ?? "none"));
            if (ModeAfterTyping != "planner")
                violations.Add("typing-changed-mode:" + (ModeAfterTyping ?? "none"));
            string wheel = WheelEvidence;
            if (wheel == "unread") violations.Add("scroll-unread");
            else if (wheel == "no-scroll" || wheel == "moved-without-overflow")
                violations.Add("wheel:" + wheel + ":" + ScrollBefore + ">" + ScrollAfter);
            if (TargetSource != null && SelectedBeforeClick == TargetSource)
                violations.Add("target-already-selected:" + TargetSource);
            if (TargetSource == null || SelectedAfterClick != TargetSource)
                violations.Add("tile-not-selected:" + (TargetSource ?? "none") + ">" + (SelectedAfterClick ?? "none"));
            if (FocusCycle == null || FocusCycle.IndexOf("minimized=True", StringComparison.Ordinal) < 0)
                violations.Add("focus-cycle:" + (FocusCycle ?? "none"));
            if (!FocusRegained) violations.Add("focus-not-regained");
            if (!WorkspaceOpenAfterFocus) violations.Add("workspace-lost-on-focus");
            if (Query == null || QuerySuffix == null || SearchTextAfterFocus != Query + QuerySuffix)
                violations.Add("typing-after-focus:" + (SearchTextAfterFocus ?? "none"));
            if (!ClosedByEscape) violations.Add("escape-did-not-close");
            if (!LeaseReleased) violations.Add("lease-held-after-close");
            if (ModeAfterClose != "Default") violations.Add("mode-after-close:" + (ModeAfterClose ?? "none"));
            if (PlayerCommands != 0 || MovementCommands != 0 || AbilityCommands != 0 ||
                SelectionEvents != 0 || AbilityTargetEvents != 0 || !SelectionUnchanged || !CameraUnchanged)
                violations.Add("world-input-leaked:commands=" + PlayerCommands + "/" + MovementCommands + "/" +
                    AbilityCommands + ";events=" + SelectionEvents + "/" + AbilityTargetEvents +
                    ";selectionUnchanged=" + SelectionUnchanged + ";cameraUnchanged=" + CameraUnchanged);
            return violations;
        }
    }
}
