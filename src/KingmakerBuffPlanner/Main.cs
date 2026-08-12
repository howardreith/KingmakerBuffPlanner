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
        private const string DefaultPlannerHotkey = "Ctrl+Shift+B";
        private static ModLog _log;
        private static RuntimeTestHost _runtimeTest;
        private static string _modPath;
        private static bool _enabled;
        private static bool _firstUpdateLogged;
        private static bool _hotkeyArmedLogged;
        private static string _lastSnapshot = "bootstrap-not-loaded";
        private static int _hotkeyKeydownCount;

        internal static bool HotkeyArmed { get { return _hotkeyArmedLogged && _enabled; } }
        internal static int HotkeyKeydownCount { get { return _hotkeyKeydownCount; } }

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
                _log.Info("[KBP-BOOT] Harmony patch result;required=true;pointerPatchCount=2;" +
                    "targets=PointerController.Tick,CameraRig.GetCameraScrollShiftByMouse;" +
                    "scope=planner-pointer-regions.");
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-BOOT] Harmony pointer ownership install failed;" +
                    "HotkeyArmedByOnUpdate=true;HUDRetryable=true.", exception);
            }
            try
            {
                PlannerHotkey.Install(_log);
                _log.Info("[KBP-HOTKEY] isolation patch result;required=true;" +
                    "plannerHotkey=" + DefaultPlannerHotkey + ".");
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-HOTKEY] native binding isolation install failed;" +
                    "planner opening remains available from the HUD.", exception);
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

            bool hotkeyDown = false;
            try
            {
                hotkeyDown = _enabled && PlannerHotkey.GetKeyDown();
                if (_enabled && !_hotkeyArmedLogged)
                {
                    _hotkeyArmedLogged = true;
                    _log.Info("[KBP-BOOT] PlannerHotkey handler armed;binding=" +
                        PlannerHotkey.Binding + ";source=UMM.OnUpdate.");
                }
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-BOOT] Planner hotkey polling failed.", exception);
            }

            if (_enabled)
            {
                try
                {
                    BuffPlannerUiRoot.Ensure(_modPath, _log);
                    if (hotkeyDown)
                    {
                        _hotkeyKeydownCount++;
                        _log.Info("[KBP-BOOT] PlannerHotkey keydown observed;binding=" +
                            PlannerHotkey.Binding + ";source=UMM.OnUpdate.");
                        BuffPlannerUiRoot.HandlePlannerHotkey();
                    }
                    BuffPlannerUiRoot.TickOwned(deltaTime);
                }
                catch (Exception exception)
                {
                    _log.Error("[KBP-BOOT] root update failed;planner hotkey remains armed.", exception);
                }
            }

            RuntimeTestHost runtime = _runtimeTest;
            if (runtime == null || !runtime.Update()) return;
            _runtimeTest = null;
            BuffPlannerUiRoot.DestroyOwned();
            PlannerPointerOwnership.Uninstall();
            PlannerHotkey.Uninstall();
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
            PlannerHotkey.Uninstall();
            _log.Info("[KBP-BOOT] unloaded;disposed=true.");
            return true;
        }
    }
}
