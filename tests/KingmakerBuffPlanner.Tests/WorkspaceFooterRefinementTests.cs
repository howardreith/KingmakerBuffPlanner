using System;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // Owner request after 0.6.11: the Require full plan checkbox becomes a
    // native state button, every footer action has a tooltip, the idle
    // status row and "Saved" text are gone and their space goes to the
    // lanes, and save failures stay visible and retryable.
    internal static partial class Program
    {
        private static void RunWorkspaceFooterRefinementTests(string root)
        {
            Run("footer-refinement-policy-and-action-tooltips-name-state-and-next-action", TestFooterTooltipContent);
            Run("footer-refinement-tooltip-lifecycle-hover-focus-and-dismissal", TestFooterTooltipLifecycle);
            Run("footer-refinement-reclaimed-layout-at-owner-and-compact-sizes", TestFooterReclaimedLayout);
            Run("footer-refinement-policy-toggle-is-one-persisted-step", () => TestFooterPolicyToggle(root));
            Run("footer-refinement-notice-is-error-only-and-save-failure-stays", () => TestFooterNoticeErrorOnly(root));
            Run("footer-refinement-undo-names-a-pending-cleared-plan", () => TestFooterUndoNamesClear(root));
        }

        private static void TestFooterTooltipContent()
        {
            Expect(WorkspaceFooterTooltips.PolicyCaption(ExecutionProfile.CastAvailable) == "Plan: Cast available" &&
                WorkspaceFooterTooltips.PolicyCaption(ExecutionProfile.RequireFullPlan) == "Plan: Require full",
                "the policy caption does not state the current policy");
            WorkspaceTooltipContent available = WorkspaceFooterTooltips.Policy(ExecutionProfile.CastAvailable);
            WorkspaceTooltipContent full = WorkspaceFooterTooltips.Policy(ExecutionProfile.RequireFullPlan);
            Expect(available.Title == "Plan: Cast available" && available.Body.StartsWith("Current: Cast available", StringComparison.Ordinal) &&
                available.Body.Contains("saved order") && available.Body.Contains("game log") &&
                available.Body.EndsWith("Click to switch to Require full.", StringComparison.Ordinal),
                "Cast available tooltip: " + available.Text);
            Expect(full.Title == "Plan: Require full" && full.Body.StartsWith("Current: Require full", StringComparison.Ordinal) &&
                full.Body.Contains("nothing is cast") && full.Body.Contains("not an undo") &&
                full.Body.EndsWith("Click to switch to Cast available.", StringComparison.Ordinal),
                "Require full tooltip: " + full.Text);
            Expect(!full.Body.Contains("rollback") && !full.Body.Contains("all-or-nothing"),
                "Require full promises rollback");
            foreach (string mode in new[] { "instant", "animated" })
            {
                WorkspaceTooltipContent tip = WorkspaceFooterTooltips.Mode(mode);
                string other = mode == "instant" ? "Animated" : "Instant";
                Expect(tip.Title == WorkspaceFooterTooltips.ModeCaption(mode) &&
                    tip.Body.StartsWith("Current: " + (mode == "instant" ? "Instant" : "Animated"), StringComparison.Ordinal) &&
                    tip.Body.Contains("stops the run") && tip.Body.EndsWith("Click to switch to " + other + ".", StringComparison.Ordinal),
                    "mode tooltip: " + tip.Text);
            }
            Expect(WorkspaceFooterTooltips.Mode("instant").Body.Contains("not ready in this mode"),
                "Instant implies it makes unsupported routes work");
            Expect(WorkspaceFooterTooltips.Resources().Body.Contains("spends nothing"), "Resources spending");
            WorkspaceTooltipContent clear = WorkspaceFooterTooltips.ClearPlan(null, 16);
            Expect(clear.Body.Contains("all 16 planned castings") && clear.Body.Contains("every routine") &&
                clear.Body.Contains("Active buffs stay") && clear.Body.Contains("nothing is refunded") && clear.Body.Contains("Undo"),
                "Clear plan tooltip: " + clear.Text);
            Expect(WorkspaceFooterTooltips.ClearPlan("The entire plan is already empty.", 0).Body ==
                "Unavailable: The entire plan is already empty.", "disabled Clear plan has no readable reason");
            foreach (WorkspaceTooltipContent undo in new[] { WorkspaceFooterTooltips.Undo(false, 0),
                WorkspaceFooterTooltips.Undo(true, 0), WorkspaceFooterTooltips.Undo(true, 7) })
                Expect(undo.Body.Contains("never reverses spells already cast or resources spent"), "Undo implies reversing casts");
            Expect(WorkspaceFooterTooltips.Undo(true, 7).Body.StartsWith("Restores the cleared plan (7 castings).", StringComparison.Ordinal) &&
                WorkspaceFooterTooltips.Undo(false, 0).Body.StartsWith("Nothing to undo", StringComparison.Ordinal),
                "Undo does not name its real pending action");
            WorkspaceTooltipContent run = WorkspaceFooterTooltips.Run("Long", ExecutionProfile.RequireFullPlan, "instant",
                "Long cannot run: 1 casting is not ready (marked in red).", false);
            Expect(run.Title == "Run Long" && run.Body.StartsWith("Unavailable now.", StringComparison.Ordinal) &&
                run.Body.Contains("Plan: Require full") && run.Body.Contains("Mode: Instant") && run.Body.Contains("not ready"),
                "Run tooltip: " + run.Text);
            Expect(WorkspaceFooterTooltips.ClearCancel().Body.Contains("Nothing changes") &&
                WorkspaceFooterTooltips.ClearConfirm(1).Body.Contains("the 1 planned casting"), "modal tooltips");
        }

        private static void TestFooterTooltipLifecycle()
        {
            var state = new WorkspaceTooltipState();
            state.PointerEnter("plan/mode");
            Expect(state.Key == "plan/mode" && state.Source == WorkspaceTooltipSource.Pointer, "hover did not show");
            state.PointerExit("plan/undo");
            Expect(state.Key == "plan/mode", "leaving another control hid the tooltip");
            state.FocusChanged("plan/run");
            Expect(state.Key == "plan/run" && state.Source == WorkspaceTooltipSource.Focus, "focus did not show");
            state.FocusChanged(null);
            Expect(state.Key == "plan/mode" && state.Source == WorkspaceTooltipSource.Pointer,
                "focus leaving did not fall back to the still-hovered control");
            state.PointerExit("plan/mode");
            Expect(!state.Visible, "tooltip stayed after the pointer left");
            // Focus is separate from hover; hover never moves focus.
            state.FocusChanged("plan/clear");
            state.PointerEnter("plan/undo");
            Expect(state.Key == "plan/undo", "latest hover did not show");
            state.PointerExit("plan/undo");
            Expect(state.Key == "plan/clear" && state.Source == WorkspaceTooltipSource.Focus, "focus owner lost to hover");
            // Rebuild/destroy: a vanished owner is forgotten.
            state.PointerEnter("plan/undo");
            state.Retain(key => key != "plan/undo");
            Expect(state.Key == "plan/clear", "a destroyed control kept its tooltip");
            state.Retain(key => false);
            Expect(!state.Visible, "no owner exists but a tooltip shows");
            // A modal/close dismisses everything; nothing resumes on its own.
            state.FocusChanged("plan/run");
            state.PointerEnter("plan/mode");
            state.Dismiss();
            Expect(!state.Visible, "dismissed tooltip still visible");
            state.Retain(key => true);
            Expect(!state.Visible, "a dismissed owner resumed without a new hover or focus change");
            state.FocusChanged("clear/cancel");
            Expect(state.Key == "clear/cancel", "modal focus did not show its tooltip");
        }

        private static void TestFooterReclaimedLayout()
        {
            foreach (int height in new[] { 1200, 1080 })
            {
                float frame = WorkspaceFooterLayout.FrameHeight(height, false);
                float before = WorkspaceFooterLayout.LegacyContentHeight(frame);
                float after = WorkspaceFooterLayout.ContentHeight(frame);
                Console.WriteLine("[FOOTER-LAYOUT] " + WorkspaceFooterLayout.Describe(1920, height, false));
                Console.WriteLine("[FOOTER-LAYOUT] " + WorkspaceFooterLayout.Describe(1920, height, true));
                Expect(after - before >= 60f, "the reclaimed band is not given to the lanes at 1920x" + height);
            }
            Expect(WorkspaceFooterLayout.RowHeight >= 50f, "footer buttons shrank below native size");
            Expect(WorkspaceFooterLayout.LaneBottom > WorkspaceFooterLayout.RuleOffset &&
                WorkspaceFooterLayout.RuleOffset > WorkspaceFooterLayout.RowTop, "lanes, rule and action row overlap");
            // The view lays the lanes out on the same numbers and has no
            // idle row, Saved label or ledger left behind.
            string view = ProductionSource("UI", "CastingWorkspaceScreenView.cs");
            string footer = MemberBody(view, "private void BuildFooter(");
            Expect(!footer.Contains("\"Result\"") && !footer.Contains("\"SaveStatus\"") && !footer.Contains("FooterLedger") &&
                !footer.Contains("Checkbox(") && footer.Contains("WorkspaceFooterLayout.RowTop"),
                "BuildFooter still builds the idle row, Saved label, ledger or checkbox");
            Expect(view.Split(new[] { "WorkspaceFooterLayout.LaneBottom" }, StringSplitOptions.None).Length - 1 == 3,
                "the catalogue, graph and inspector do not all extend into the reclaimed space");
            Expect(!view.Contains(".16f, .742f") && !view.Contains("0.155f"), "a lane or rule still uses the two-row footer anchors");
        }

        private static void TestFooterPolicyToggle(string root)
        {
            string dir;
            ClearFixture(root, "policy-toggle", out dir);
            Assembly product = ViewInputProduct();
            object session = ProductSession(product, dir);
            Type sessionType = session.GetType();
            Func<string> policy = () => (string)sessionType.GetProperty("AvailabilityPolicy").GetValue(session, null);
            Func<string> mode = () => (string)sessionType.GetProperty("ExecutionMode").GetValue(session, null);
            ViewInputCall(session, "SetExecutionMode", "animated");
            ViewInputCall(session, "SetAvailabilityPolicy", ExecutionProfile.CastAvailable);
            string intent = (string)ViewInputCall(session, "AuthoredPlanIntentSignature");
            object view = DetachedViewObject(product.GetType("KingmakerBuffPlanner.UI.CastingWorkspaceScreenView", true));
            SetNative(view, "_session", session);
            foreach (string expected in new[] { ExecutionProfile.RequireFullPlan, ExecutionProfile.CastAvailable, ExecutionProfile.RequireFullPlan })
            {
                ViewInputCall(view, "ToggleAvailabilityPolicyCommand");
                Expect(policy() == expected, "one activation did not toggle exactly once: " + policy());
                Expect(mode() == "animated", "the policy toggle changed the execution mode");
                Expect((string)ViewInputCall(session, "AuthoredPlanIntentSignature") == intent, "the policy toggle changed assignments");
                object reopened = ProductSession(product, dir);
                Expect((string)reopened.GetType().GetProperty("AvailabilityPolicy").GetValue(reopened, null) == expected &&
                    (string)reopened.GetType().GetProperty("ExecutionMode").GetValue(reopened, null) == "animated",
                    "the policy did not persist through autosave, or another preference was reset");
            }
            Expect((string)ViewInputCall(view, "PolicyCaption") == "Plan: Require full", "caption does not state the current policy");
        }

        private static void TestFooterNoticeErrorOnly(string root)
        {
            Expect(WorkspaceFooterText.SaveIndicator("saved") == string.Empty &&
                WorkspaceFooterText.SaveIndicator("saving") == string.Empty &&
                WorkspaceFooterText.SaveIndicator("save-failed:IOException") == "Not saved" &&
                WorkspaceFooterText.SaveIndicator("save-refused:legacy-import") == "Not saved",
                "the save indicator is not error-only");
            string dir;
            ClearFixture(root, "notice", out dir);
            Assembly product = ViewInputProduct();
            object session = ProductSession(product, dir);
            object view = DetachedViewObject(product.GetType("KingmakerBuffPlanner.UI.CastingWorkspaceScreenView", true));
            SetNative(view, "_session", session);
            Func<string, string> standing = save => (string)ViewInputCall(view, "StandingNotice", save);
            ViewInputCall(view, "Feedback", "Casting added (Undo removes it).", false);
            Expect(standing(null) == null, "routine success chatter is shown");
            Expect((string)ViewInputCall(view, "get_LastCommandMessageForRuntime") == "Casting added (Undo removes it).",
                "the command message was not recorded for evidence");
            string failed = WorkspaceFooterText.SaveProblem("save-failed:IOException");
            Expect(standing(failed) == failed && failed.Contains("Retry save"), "a failed save is not shown with its recovery");
            ViewInputCall(view, "Feedback", "Long was not cast: 2 castings are not ready.", true);
            Expect(standing(failed) == "Long was not cast: 2 castings are not ready.", "a refusal was hidden");
            SetNative(view, "_feedbackShown", false);
            Expect(standing(failed) == failed, "after the next command the standing save failure is not shown");
        }

        private static void TestFooterUndoNamesClear(string root)
        {
            string dir;
            CastingWorkspaceSession session = ClearFixture(root, "undo-names", out dir);
            Expect(session.UndoRestoresClearedCount == 0, "nothing was cleared yet");
            Expect(session.ClearPlan(session.BeginClearPlan()).Applied, "clear refused");
            Expect(session.UndoRestoresClearedCount == 3, "Undo does not name the cleared plan");
            Expect(session.Undo() && session.UndoRestoresClearedCount == 0 && session.Document.Castings.Count == 3,
                "Undo after Clear did not restore, or still claims a cleared plan");
            Expect(session.ClearPlan(session.BeginClearPlan()).Applied, "second clear refused");
            Expect(session.AddCastingForRuntime(LifecycleCasting("cast-after-clear", "unit-t1")).Applied, "later edit");
            Expect(session.UndoRestoresClearedCount == 0, "a later edit's Undo is described as restoring the cleared plan");
        }
    }
}
