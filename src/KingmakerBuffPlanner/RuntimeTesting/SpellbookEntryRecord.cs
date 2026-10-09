using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // WP2B: one physical spellbook -> Buff Planner -> Escape cycle, observed
    // in game and judged here without Unity.
    public sealed class SpellbookCycleObservation
    {
        public int Cycle { get; set; }
        public bool FaultInjected { get; set; }
        // Spellbook opened by the native key binding, before the click.
        public bool SpellbookShown { get; set; }
        public bool ButtonAttached { get; set; }
        public bool ButtonInteractable { get; set; }
        public int OwnedButtonCount { get; set; }
        public int ListenerCount { get; set; }
        public float? ButtonX { get; set; }
        public float? ButtonY { get; set; }
        public float ButtonWidth { get; set; }
        public float ButtonHeight { get; set; }
        public bool TopmostHitIsOwned { get; set; }
        public string TopmostHitPath { get; set; }
        public string Placement { get; set; }
        public bool PlannerOpenBeforeClick { get; set; }
        // After the physical click.
        public int NativeCloseInvocations { get; set; }
        public int OpenerInvocations { get; set; }
        public int NativeReleases { get; set; }
        public int WorkspaceOpens { get; set; }
        public string HandoffState { get; set; }
        public string HandoffFailure { get; set; }
        public bool PlannerOpenAfterClick { get; set; }
        public bool InputLeaseHeldAfterClick { get; set; }
        public bool SpellbookShownAfterClick { get; set; }
        // The native service window itself, and whether the FullScreenUi
        // mode now active is the planner's own input lease (the planner
        // raises that mode and refuses to open while another owner holds
        // it).
        public bool ServiceWindowShownAfterClick { get; set; }
        public bool PlannerOwnsFullScreenAfterClick { get; set; }
        public int PlannerRootsAfterClick { get; set; }
        public bool ButtonRestoredAfterRecovery { get; set; }
        public int OwnedButtonsAfterRecovery { get; set; }
        // After the physical Escape.
        public bool PlannerClosedAfterEscape { get; set; }
        public bool InputLeaseReleasedAfterEscape { get; set; }
        public bool SpellbookShownAfterEscape { get; set; }
        public bool NativeOwnerActiveAfterEscape { get; set; }
        public bool NativeMenuOpenAfterEscape { get; set; }
        public int OwnedButtonsAfterEscape { get; set; }
        public int PlannerRootsAfterEscape { get; set; }
    }

    public sealed class SpellbookEntryRecord
    {
        public const int OrdinaryCycles = 3;
        public string RunId { get; set; }
        public string SourceCommit { get; set; }
        public string PackageSha256 { get; set; }
        public string DllSha256 { get; set; }
        public string AssemblyMvid { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public bool CastingFirst { get; set; }
        public bool PlannerClosedAtStart { get; set; }
        public string OpenSpellsBinding { get; set; }
        public int OpenSpellsVirtualKey { get; set; }
        public string DocumentBefore { get; set; }
        public string DocumentAfter { get; set; }
        public string ProfileBeforeSha256 { get; set; }
        public string ProfileAfterSha256 { get; set; }
        public string ResourcesBefore { get; set; }
        public string ResourcesAfter { get; set; }
        public string EffectsBefore { get; set; }
        public string EffectsAfter { get; set; }
        public int RunsStarted { get; set; }
        public bool FaultArmed { get; set; }
        public int MovementCommands { get; set; } = -1;
        public int AbilityCommands { get; set; } = -1;
        public int AbilityTargetEvents { get; set; } = -1;
        public bool SelectionUnchanged { get; set; }
        public List<string> Acknowledged { get; } = new List<string>();
        public List<string> Failures { get; } = new List<string>();
        public List<SpellbookCycleObservation> Cycles { get; } = new List<SpellbookCycleObservation>();

        public static IList<string> ExpectedActions()
        {
            var actions = new List<string>();
            for (int cycle = 1; cycle <= OrdinaryCycles + 1; cycle++)
            {
                string suffix = CycleSuffix(cycle);
                actions.Add("sb-open-" + suffix);
                actions.Add("sb-click-" + suffix);
                actions.Add("sb-escape-" + suffix);
            }
            return actions;
        }

        public static string CycleSuffix(int cycle)
        {
            return cycle > OrdinaryCycles ? "fault" : cycle.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        public IList<string> Violations()
        {
            var failures = new List<string>(Failures);
            IList<string> expected = ExpectedActions();
            if (expected.Any(action => !Acknowledged.Contains(action)) ||
                Acknowledged.Any(action => !expected.Contains(action) &&
                    !action.StartsWith("sb-menu-close-", StringComparison.Ordinal)))
                failures.Add("physical-actions-incomplete");
            if (!CastingFirst || !PlannerClosedAtStart)
                failures.Add("not-a-cold-casting-first-start");
            if (OpenSpellsVirtualKey < 0x41 || OpenSpellsVirtualKey > 0x5A)
                failures.Add("spellbook-binding-not-a-plain-letter");
            if (Cycles.Count != OrdinaryCycles + 1)
                failures.Add("cycles-incomplete:" + Cycles.Count);
            foreach (SpellbookCycleObservation cycle in Cycles)
            {
                string at = ":" + CycleSuffix(cycle.Cycle);
                if (cycle.FaultInjected != (cycle.Cycle > OrdinaryCycles))
                    failures.Add("cycle-fault-identity" + at);
                if (!cycle.SpellbookShown || !cycle.ButtonAttached || !cycle.ButtonInteractable ||
                    cycle.PlannerOpenBeforeClick)
                    failures.Add("button-not-offered-on-open-spellbook" + at);
                if (cycle.OwnedButtonCount != 1 || cycle.ListenerCount != 1)
                    failures.Add("owned-button-or-listener-not-single" + at);
                if (!OnScreen(cycle.ButtonX, cycle.ButtonY) || cycle.ButtonWidth < 40f ||
                    cycle.ButtonHeight < 16f || !cycle.TopmostHitIsOwned)
                    failures.Add("button-not-visible-and-topmost" + at);
                if (cycle.Placement == null || !cycle.Placement.Contains("conflictFree=True"))
                    failures.Add("button-placement-conflicts" + at);
                if (cycle.NativeCloseInvocations != 1 || cycle.OpenerInvocations != 1 ||
                    cycle.NativeReleases != 1)
                    failures.Add("handoff-not-exactly-once" + at);
                if (cycle.FaultInjected)
                {
                    if (cycle.WorkspaceOpens != 0 || cycle.HandoffState != "Failed" ||
                        cycle.HandoffFailure != "planner-open-refused" || cycle.PlannerOpenAfterClick ||
                        cycle.InputLeaseHeldAfterClick || cycle.PlannerRootsAfterClick != 0)
                        failures.Add("simulated-failure-not-contained" + at);
                    if (!cycle.SpellbookShownAfterClick || !cycle.ButtonRestoredAfterRecovery ||
                        cycle.OwnedButtonsAfterRecovery != 1)
                        failures.Add("failure-did-not-recover-native-spellbook" + at);
                }
                else if (cycle.WorkspaceOpens != 1 || cycle.HandoffState != "Completed" ||
                    !cycle.PlannerOpenAfterClick || !cycle.InputLeaseHeldAfterClick ||
                    cycle.SpellbookShownAfterClick || cycle.ServiceWindowShownAfterClick ||
                    !cycle.PlannerOwnsFullScreenAfterClick || cycle.PlannerRootsAfterClick != 1)
                    failures.Add("planner-not-opened-once-after-native-close" + at);
                if (!cycle.PlannerClosedAfterEscape || !cycle.InputLeaseReleasedAfterEscape ||
                    cycle.SpellbookShownAfterEscape || cycle.NativeOwnerActiveAfterEscape ||
                    cycle.NativeMenuOpenAfterEscape || cycle.OwnedButtonsAfterEscape != 0 ||
                    cycle.PlannerRootsAfterEscape != 0)
                    failures.Add("escape-did-not-return-a-usable-game-ui" + at);
            }
            if (string.IsNullOrEmpty(DocumentBefore) || DocumentBefore != DocumentAfter ||
                string.IsNullOrEmpty(ProfileBeforeSha256) || ProfileBeforeSha256 != ProfileAfterSha256)
                failures.Add("handoff-changed-authored-or-persisted-intent");
            if (RunsStarted != 0 || string.IsNullOrEmpty(ResourcesBefore) || ResourcesBefore != ResourcesAfter ||
                string.IsNullOrEmpty(EffectsBefore) || EffectsBefore != EffectsAfter)
                failures.Add("handoff-dispatched-or-changed-native-state");
            if (!FaultArmed) failures.Add("fault-simulation-not-armed");
            if (MovementCommands != 0 || AbilityCommands != 0 || AbilityTargetEvents != 0 ||
                !SelectionUnchanged)
                failures.Add("input-leaked-to-the-game-world");
            return failures;
        }

        private bool OnScreen(float? x, float? y)
        {
            return x.HasValue && y.HasValue && x > 0 && x < ScreenWidth && y > 0 && y < ScreenHeight;
        }
    }
}
