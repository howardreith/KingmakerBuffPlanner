using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Raw physical UI observations, independently judged without Unity.
    // Focus alone is insufficient: every observed card needs a clipped
    // screen point and substantial measured overlap with its viewport.
    public sealed class ProblemScreenObservation
    {
        public string CastingId { get; set; }
        public string RoutineId { get; set; }
        public string SourceId { get; set; }
        public int Position { get; set; }
        public int Count { get; set; }
        public float? ChipX { get; set; }
        public float? ChipY { get; set; }
        public float ChipVisibleFraction { get; set; }
        public float? CatalogueX { get; set; }
        public float? CatalogueY { get; set; }
        public float? RoutineX { get; set; }
        public float? RoutineY { get; set; }
        public float InspectorScroll { get; set; }
        public string InspectorText { get; set; }
        public string FooterText { get; set; }
        public bool? PreviousEnabled { get; set; }
        public bool? NextEnabled { get; set; }
        public string DocumentSignature { get; set; }
        public List<string> MachineReasons { get; } = new List<string>();
    }

    public sealed class ProblemNavigationRecord
    {
        public static readonly string[] Actions = {
            "problem-moon", "problem-next", "problem-previous",
            "problem-escape-focus", "problem-escape-close" };
        public string RunId { get; set; }
        public string SourceCommit { get; set; }
        public string PackageSha256 { get; set; }
        public string DllSha256 { get; set; }
        public string AssemblyMvid { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public bool PlannerClosedBeforeHud { get; set; }
        public bool ColdSessionBeforeHud { get; set; }
        public bool PlannerOpenedByHud { get; set; }
        public bool GraphOverflow { get; set; }
        public bool FirstChipVisibleBeforeReveal { get; set; }
        public float GraphScrollBefore { get; set; }
        public float GraphScrollAfter { get; set; }
        public string SourceId { get; set; }
        public string FirstCastingId { get; set; }
        public string SecondCastingId { get; set; }
        public string DocumentBefore { get; set; }
        public string DocumentAfter { get; set; }
        public string ProfileBeforeSha256 { get; set; }
        public string ProfileAfterSha256 { get; set; }
        public string ResourcesBefore { get; set; }
        public string ResourcesAfter { get; set; }
        public string EffectsBefore { get; set; }
        public string EffectsAfter { get; set; }
        public int RunsStarted { get; set; }
        public int DispatchAttempts { get; set; }
        public bool UndoBefore { get; set; }
        public bool UndoAfter { get; set; }
        public string AcceptedDigestAfter { get; set; }
        public bool EscapeLeftFocus { get; set; }
        public bool PlannerClosedAfterEscape { get; set; }
        public bool InputLeaseReleased { get; set; }
        public List<string> Acknowledged { get; } = new List<string>();
        public List<string> Failures { get; } = new List<string>();
        public List<ProblemScreenObservation> Observations { get; } = new List<ProblemScreenObservation>();

        public IList<string> Violations()
        {
            var failures = new List<string>(Failures);
            if (!PlannerClosedBeforeHud || !ColdSessionBeforeHud || !PlannerOpenedByHud)
                failures.Add("hud-did-not-open-a-closed-cold-planner");
            if (!Actions.All(action => Acknowledged.Contains(action)) ||
                Acknowledged.Any(action => !Actions.Contains(action) &&
                    !action.StartsWith("problem-menu-close-", StringComparison.Ordinal)))
                failures.Add("physical-actions-incomplete");
            if (!GraphOverflow || FirstChipVisibleBeforeReveal || GraphScrollAfter >= GraphScrollBefore)
                failures.Add("offscreen-card-was-not-revealed-by-scrolling");
            if (string.IsNullOrEmpty(DocumentBefore) || DocumentBefore != DocumentAfter ||
                string.IsNullOrEmpty(ProfileBeforeSha256) || ProfileBeforeSha256 != ProfileAfterSha256)
                failures.Add("navigation-changed-authored-or-persisted-intent");
            if (RunsStarted != 0 || DispatchAttempts != 0 || ResourcesBefore == null ||
                ResourcesBefore != ResourcesAfter || EffectsBefore == null || EffectsBefore != EffectsAfter)
                failures.Add("blocked-path-dispatched-or-changed-native-state");
            if (UndoBefore != UndoAfter || AcceptedDigestAfter != null)
                failures.Add("navigation-changed-undo-or-authorization");
            if (!EscapeLeftFocus || !PlannerClosedAfterEscape || !InputLeaseReleased)
                failures.Add("nested-escape-or-lease-release-failed");
            if (Observations.Count != 3) failures.Add("problem-observations-incomplete");
            string[] expected = { FirstCastingId, SecondCastingId, FirstCastingId };
            for (int index = 0; index < Observations.Count; index++)
            {
                ProblemScreenObservation observation = Observations[index];
                int position = index == 1 ? 2 : 1;
                if (index >= expected.Length || observation.CastingId != expected[index] ||
                    observation.RoutineId != "long" || observation.SourceId != SourceId ||
                    observation.Position != position || observation.Count != 2 ||
                    observation.DocumentSignature != DocumentBefore)
                    failures.Add("wrong-focused-problem:" + index);
                if (!OnScreen(observation.ChipX, observation.ChipY) || observation.ChipVisibleFraction < 0.9f ||
                    !OnScreen(observation.CatalogueX, observation.CatalogueY) ||
                    !OnScreen(observation.RoutineX, observation.RoutineY))
                    failures.Add("problem-card-catalogue-or-routine-not-visible:" + index);
                if (Math.Abs(observation.InspectorScroll - 1f) > 0.001f ||
                    observation.PreviousEnabled != (position > 1) || observation.NextEnabled != (position < 2) ||
                    observation.MachineReasons.Count == 0 || observation.InspectorText == null ||
                    !observation.InspectorText.Contains("Problem " + position + " of 2") ||
                    observation.MachineReasons.Any(reason =>
                        !observation.InspectorText.Contains(WorkspaceReasonText.Describe(reason))) ||
                    observation.FooterText == null || !observation.FooterText.Contains("Problem " + position + " of 2"))
                    failures.Add("problem-inspector-or-controls-not-actionable:" + index);
            }
            return failures;
        }

        private bool OnScreen(float? x, float? y)
        {
            return x.HasValue && y.HasValue && x > 0 && x < ScreenWidth && y > 0 && y < ScreenHeight;
        }
    }
}
