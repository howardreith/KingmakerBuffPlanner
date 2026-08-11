using System;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.RuntimeTesting;
using UnityModManagerNet;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner
{
    public static class Main
    {
        private static ModLog _log;
        private static RuntimeTestHost _runtimeTest;
        private static string _modPath;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            if (modEntry == null) throw new ArgumentNullException("modEntry");

            _log = new ModLog(modEntry.Logger);
            _modPath = modEntry.Path;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload;
            modEntry.OnUpdate = OnUpdate;
            _runtimeTest = RuntimeTestHost.TryCreate(
                Environment.GetCommandLineArgs(),
                modEntry,
                _log);
            _log.Info("Loaded Kingmaker Buff Planner " + BuildInfo.Version +
                " commit=" + BuildInfo.Commit + ".");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            _log.Info(value ? "Enabled." : "Disabled.");
            BuffPlannerUiRoot.SetEnabled(value);
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            BuffPlannerUiRoot.Ensure(_modPath, _log);
            RuntimeTestHost runtime = _runtimeTest;
            if (runtime == null || !runtime.Update()) return;
            _runtimeTest = null;
            BuffPlannerUiRoot.DestroyOwned();
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            _runtimeTest = null;
            _log.Info("Unloaded.");
            return true;
        }
    }
}
