using System;
using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.UI
{
    // Owner request after 0.6.11: every footer action explains itself in a
    // tooltip and the idle explanatory/status row is gone. Pure content and
    // lifecycle models; the Unity view only renders them.
    public sealed class WorkspaceTooltipContent
    {
        public WorkspaceTooltipContent(string title, string body)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
        }

        public string Title { get; private set; }
        public string Body { get; private set; }
        public string Text { get { return Title.Length == 0 ? Body : Title + "\n" + Body; } }
    }

    public static class WorkspaceFooterTooltips
    {
        public static string CastAvailableCaption { get { return PlannerText.Get("footer.policy.available"); } }
        public static string RequireFullCaption { get { return PlannerText.Get("footer.policy.full"); } }

        // The caption states the CURRENT policy, never the next action.
        public static string PolicyCaption(string availabilityPolicy)
        {
            return availabilityPolicy == ExecutionProfile.RequireFullPlan ? RequireFullCaption : CastAvailableCaption;
        }

        public static string ModeCaption(string mode)
        {
            return mode == "instant" ? PlannerText.Get("footer.mode.instant") : PlannerText.Get("footer.mode.animated");
        }

        public static WorkspaceTooltipContent Resources()
        {
            return new WorkspaceTooltipContent(PlannerText.Get("footer.resources.title"),
                PlannerText.Get("footer.resources.body"));
        }

        public static WorkspaceTooltipContent Policy(string availabilityPolicy)
        {
            if (availabilityPolicy == ExecutionProfile.RequireFullPlan)
                return new WorkspaceTooltipContent(RequireFullCaption,
                    PlannerText.Get("footer.policy.fullHint"));
            return new WorkspaceTooltipContent(CastAvailableCaption,
                PlannerText.Get("footer.policy.availableHint"));
        }

        public static WorkspaceTooltipContent Mode(string mode)
        {
            if (mode == "instant")
                return new WorkspaceTooltipContent(ModeCaption(mode),
                    PlannerText.Get("footer.mode.instantHint"));
            return new WorkspaceTooltipContent(ModeCaption(mode),
                PlannerText.Get("footer.mode.animatedHint"));
        }

        public static WorkspaceTooltipContent ClearPlan(string unavailableReason, int castingCount)
        {
            if (!string.IsNullOrEmpty(unavailableReason))
                return new WorkspaceTooltipContent(PlannerText.Get("footer.clear.title"), PlannerText.Get("footer.clear.unavailable", unavailableReason));
            return new WorkspaceTooltipContent(PlannerText.Get("footer.clear.title"),
                (castingCount == 1 ? PlannerText.Get("footer.clear.open.one", castingCount) : PlannerText.Get("footer.clear.open.many", castingCount)));
        }

        // restoresCleared: the castings the next Undo brings back after a
        // Clear plan (0 when the next Undo reverts some other edit).
        public static WorkspaceTooltipContent Undo(bool canUndo, int restoresCleared)
        {
            if (!canUndo)
                return new WorkspaceTooltipContent(PlannerText.Get("footer.undo.title"), PlannerText.Get("footer.undo.empty"));
            if (restoresCleared > 0)
                return new WorkspaceTooltipContent(PlannerText.Get("footer.undo.title"), (restoresCleared == 1 ? PlannerText.Get("footer.undo.restore.one", restoresCleared) : PlannerText.Get("footer.undo.restore.many", restoresCleared)));
            return new WorkspaceTooltipContent(PlannerText.Get("footer.undo.title"), PlannerText.Get("footer.undo.last"));
        }

        // status: the Run line the footer used to show (WorkspaceFooterText.
        // RunStatus) - what a Run does now, or exactly why it cannot.
        public static WorkspaceTooltipContent Run(string routineName, string availabilityPolicy, string mode,
            string status, bool enabled)
        {
            string body = PlannerText.Get("footer.run.body", routineName, PolicyCaption(availabilityPolicy), ModeCaption(mode));
            if (!string.IsNullOrEmpty(status)) body += "\n" + status;
            if (!enabled) body = PlannerText.Get("footer.run.disabled", body);
            return new WorkspaceTooltipContent(WorkspaceFooterText.RunCaption(routineName), body);
        }

        public static WorkspaceTooltipContent ClearCancel()
        {
            return new WorkspaceTooltipContent(PlannerText.Get("footer.clear.cancel"), PlannerText.Get("footer.clear.cancelHint"));
        }

        public static WorkspaceTooltipContent ClearConfirm(int castingCount)
        {
            return new WorkspaceTooltipContent(PlannerText.Get("footer.clear.title"),
                (castingCount == 1 ? PlannerText.Get("footer.clear.confirm.one", castingCount) : PlannerText.Get("footer.clear.confirm.many", castingCount)));
        }

        public static WorkspaceTooltipContent SaveRecovery(string recoveryAction, string problem)
        {
            if (recoveryAction == WorkspaceFooterText.ReloadAction)
                return new WorkspaceTooltipContent(WorkspaceFooterText.RecoveryCaption(recoveryAction),
                    PlannerText.Get("footer.recovery.reloadHint", problem ?? string.Empty));
            return new WorkspaceTooltipContent(WorkspaceFooterText.RecoveryCaption(recoveryAction ?? WorkspaceFooterText.RetrySaveAction),
                PlannerText.Get("footer.recovery.retryHint", problem ?? string.Empty));
        }
    }

    public enum WorkspaceTooltipSource
    {
        None,
        Pointer,
        Focus
    }

    // One tooltip at a time. Pointer hover and keyboard/controller focus are
    // tracked separately (hovering never moves focus); the latest event
    // shows, and leaving it falls back to the other still-valid owner.
    // Activation keeps the owner (the view re-reads its live content), and
    // a modal, rebuild without the owner, close or reload dismisses it.
    public sealed class WorkspaceTooltipState
    {
        private string _pointerKey;
        private string _focusKey;

        public string Key { get; private set; }
        public WorkspaceTooltipSource Source { get; private set; }
        public bool Visible { get { return Key != null; } }

        public void PointerEnter(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _pointerKey = key;
            Show(key, WorkspaceTooltipSource.Pointer);
        }

        public void PointerExit(string key)
        {
            if (key == null || !string.Equals(_pointerKey, key, StringComparison.Ordinal)) return;
            _pointerKey = null;
            if (Source == WorkspaceTooltipSource.Pointer) Fallback();
        }

        // key: the focused control's tooltip key, or null when focus is on a
        // control without one (or keyboard focus is inactive).
        public void FocusChanged(string key)
        {
            if (string.Equals(_focusKey, key, StringComparison.Ordinal)) return;
            _focusKey = string.IsNullOrEmpty(key) ? null : key;
            if (_focusKey != null) Show(_focusKey, WorkspaceTooltipSource.Focus);
            else if (Source == WorkspaceTooltipSource.Focus) Fallback();
        }

        // Rebuild/destroy: an owner whose control no longer exists (or is
        // hidden) is forgotten; no floating tooltip for a destroyed control.
        public void Retain(Func<string, bool> exists)
        {
            if (exists == null) return;
            if (_pointerKey != null && !exists(_pointerKey)) _pointerKey = null;
            if (_focusKey != null && !exists(_focusKey)) _focusKey = null;
            if (Key != null && !exists(Key)) Fallback();
        }

        // Modal transitions, Resources, close and reload: nothing stays shown
        // and neither owner is resumed until a fresh hover or focus change.
        public void Dismiss()
        {
            _pointerKey = null;
            _focusKey = null;
            Key = null;
            Source = WorkspaceTooltipSource.None;
        }

        private void Show(string key, WorkspaceTooltipSource source)
        {
            Key = key;
            Source = source;
        }

        private void Fallback()
        {
            if (_pointerKey != null) Show(_pointerKey, WorkspaceTooltipSource.Pointer);
            else if (_focusKey != null) Show(_focusKey, WorkspaceTooltipSource.Focus);
            else { Key = null; Source = WorkspaceTooltipSource.None; }
        }
    }

    // The workspace's vertical layout in canvas units (the workspace canvas
    // has no scaler, so a unit is a screen pixel). 0.6.11 reserved the
    // bottom 16% of the frame for a two-row footer (status line + actions);
    // the refinement keeps one native-size action row and gives the rest to
    // the catalogue, graph, recipients and inspector.
    public static class WorkspaceFooterLayout
    {
        public const float FrameInsetBottom = 24f;
        public const float FrameInsetTopMouse = 60f;
        public const float FrameInsetTopController = 112f;
        public const float LaneTopFraction = 0.895f;
        public const float LegacyLaneBottomFraction = 0.16f;

        // The action row: native-size buttons (the spellbook's own button
        // art reads well at 50+ units) with breathing room to the frame edge.
        public const float RowBottom = 10f;
        public const float RowHeight = 54f;
        public const float RuleGap = 8f;
        public const float LaneGap = 10f;

        public static float FrameHeight(float screenHeight, bool controller)
        {
            return screenHeight - FrameInsetBottom - (controller ? FrameInsetTopController : FrameInsetTopMouse);
        }

        public static float RowTop { get { return RowBottom + RowHeight; } }

        // Where the footer rule sits, measured up from the frame's bottom.
        public static float RuleOffset { get { return RowTop + RuleGap; } }

        // Where every lane now ends, measured up from the frame's bottom.
        public static float LaneBottom { get { return RuleOffset + LaneGap; } }

        public static float ContentHeight(float frameHeight)
        {
            return Math.Max(0f, frameHeight * LaneTopFraction - LaneBottom);
        }

        public static float LegacyContentHeight(float frameHeight)
        {
            return Math.Max(0f, frameHeight * (LaneTopFraction - LegacyLaneBottomFraction));
        }

        public static string Describe(float screenWidth, float screenHeight, bool controller)
        {
            float frame = FrameHeight(screenHeight, controller);
            float before = LegacyContentHeight(frame);
            float after = ContentHeight(frame);
            return screenWidth.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "x" +
                screenHeight.ToString("0", System.Globalization.CultureInfo.InvariantCulture) +
                (controller ? ":controller" : ":mouse") +
                ";frame=" + frame.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) +
                ";laneBefore=" + before.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) +
                ";laneAfter=" + after.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) +
                ";recovered=" + (after - before).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
