using System;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.RuntimeTesting;
using UnityModManagerNet;
using KingmakerBuffPlanner.UI;
using UnityEngine;

namespace KingmakerBuffPlanner
{
    public static class Main
    {
        private static ModLog _log;
        private static RuntimeTestHost _runtimeTest;
        private static string _modPath;
        private static bool _enabled;
        private static bool _firstUpdateLogged;
        private static bool _f10ArmedLogged;
        private static string _lastSnapshot = "bootstrap-not-loaded";
        private static int _f10KeydownCount;

        internal static bool F10Armed { get { return _f10ArmedLogged && _enabled; } }
        internal static int F10KeydownCount { get { return _f10KeydownCount; } }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            if (modEntry == null) throw new ArgumentNullException("modEntry");

            _log = new ModLog(modEntry.Logger);
            _log.Info("[KBP-BOOT] Main.Load entered; assembly=" +
                typeof(Main).Assembly.FullName + ";modEntry.Enabled=" + modEntry.Enabled + ".");
            _modPath = modEntry.Path;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload;
            modEntry.OnUpdate = OnUpdate;
            modEntry.OnGUI = OnGui;
            _log.Info("[KBP-BOOT] callbacks assigned;OnToggle=true;OnUpdate=true;" +
                "OnUnload=true;OnGUI=true.");
            try
            {
                PlannerPointerOwnership.Install(_log);
                _log.Info("[KBP-BOOT] Harmony patch result;required=true;patchCount=2;" +
                    "targets=PointerController.Tick,CameraRig.GetCameraScrollShiftByMouse;" +
                    "scope=planner-pointer-regions.");
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-BOOT] Harmony pointer ownership install failed;" +
                    "F10ArmedByOnUpdate=true;HUDRetryable=true.", exception);
            }
            _runtimeTest = RuntimeTestHost.TryCreate(
                Environment.GetCommandLineArgs(),
                modEntry,
                _log);
            _log.Info("[KBP-BOOT] Main.Load exited;version=" + BuildInfo.Version +
                ";commit=" + BuildInfo.Commit + ";result=true.");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            _enabled = value;
            _log.Info("[KBP-BOOT] OnToggle invoked;value=" + value +
                ";modEntry.Enabled=" + (modEntry != null && modEntry.Enabled) + ".");
            BuffPlannerUiRoot.SetEnabled(value);
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            if (!_firstUpdateLogged)
            {
                _firstUpdateLogged = true;
                _log.Info("[KBP-BOOT] OnUpdate first tick;enabled=" + _enabled +
                    ";modEntry.Active=" + (modEntry != null && modEntry.Active) + ".");
            }

            bool f10Down = false;
            try
            {
                f10Down = _enabled && Input.GetKeyDown(KeyCode.F10);
                if (_enabled && !_f10ArmedLogged)
                {
                    _f10ArmedLogged = true;
                    _log.Info("[KBP-BOOT] F10 handler armed;source=UMM.OnUpdate.");
                }
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-BOOT] F10 polling failed.", exception);
            }

            if (_enabled)
            {
                try
                {
                    BuffPlannerUiRoot.Ensure(_modPath, _log);
                    if (f10Down)
                    {
                        _f10KeydownCount++;
                        _log.Info("[KBP-BOOT] F10 keydown observed;source=UMM.OnUpdate.");
                        BuffPlannerUiRoot.HandleF10();
                    }
                    BuffPlannerUiRoot.TickOwned(deltaTime);
                }
                catch (Exception exception)
                {
                    _log.Error("[KBP-BOOT] root update failed;F10 remains armed.", exception);
                }
            }

            RuntimeTestHost runtime = _runtimeTest;
            if (runtime == null || !runtime.Update()) return;
            _runtimeTest = null;
            BuffPlannerUiRoot.DestroyOwned();
            PlannerPointerOwnership.Uninstall();
        }

        private static void OnGui(UnityModManager.ModEntry modEntry)
        {
            try
            {
                _lastSnapshot = BuffPlannerUiRoot.GetSnapshot();
                GUILayout.Label("Kingmaker Buff Planner bootstrap diagnostics");
                GUILayout.TextArea(_lastSnapshot, GUILayout.MinHeight(100f));
                if (GUILayout.Button("Log bootstrap snapshot"))
                    _log.Info("[KBP-BOOT] snapshot;" + _lastSnapshot);
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-BOOT] diagnostics panel failed.", exception);
            }
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            _enabled = false;
            _runtimeTest = null;
            BuffPlannerUiRoot.DestroyOwned();
            PlannerPointerOwnership.Uninstall();
            _log.Info("[KBP-BOOT] unloaded;disposed=true.");
            return true;
        }
    }
}
