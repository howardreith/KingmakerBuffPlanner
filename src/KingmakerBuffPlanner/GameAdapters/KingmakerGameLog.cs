using System;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.UI.Log;
using UnityEngine;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Native UI finish (UI-08): the one production route into the game's own
    // log panel (the bottom-right gameplay log), exact 2.1.7b contract:
    // Game.Instance.UI.BattleLogManager.LogView.AddLogEntry(msg, color,
    // tooltip, prefixIcon), coloured like the game's own explanation and
    // warning lines (BlueprintRoot.Instance.UIRoot.LogColors - the same
    // object BattleLogManager's private LogColors accessor returns). The planner writes run summaries and refusals here; the
    // detail goes into the line's hover text. Fail-soft: without a campaign
    // log nothing is written and the caller keeps its UMM diagnostics.
    internal static class KingmakerGameLog
    {
        internal static bool Write(string message, bool warning, string detail)
        {
            if (string.IsNullOrEmpty(message)) return false;
            BattleLogManager manager = Game.Instance == null || Game.Instance.UI == null
                ? null : Game.Instance.UI.BattleLogManager;
            BattleLogView view = manager == null ? null : manager.LogView;
            LogColors colors = BlueprintRoot.Instance == null || BlueprintRoot.Instance.UIRoot == null
                ? null : BlueprintRoot.Instance.UIRoot.LogColors;
            if (view == null || colors == null) return false;
            Color32 color = warning ? colors.WarningLogColor : colors.ExplanationColor;
            view.AddLogEntry(message, color, string.IsNullOrEmpty(detail) ? null : detail, PrefixIcon.None);
            return true;
        }
    }
}
