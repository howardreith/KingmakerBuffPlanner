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
        // The mod manager's own toggle; the runtime qualification never
        // enables a planner the manager has disabled.
        private static bool _managerEnabled = true;
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
            _managerEnabled = value;
            _enabled = value;
            _log.Info("[KBP-BOOT] OnToggle invoked;value=" + value +
                ";modEntry.Enabled=" + (modEntry != null && modEntry.Enabled) + ".");
            // Review M3: disabling the mod must not leave a runtime probe run
            // able to continue on its own; its owner terminates and cleans up.
            if (!value && _runtimeTest != null)
            {
                try { _runtimeTest.Shutdown("mod-disabled"); }
                catch (Exception exception) { _log.Error("[KBP-PROBE] disable shutdown failed.", exception); }
            }
            BuffPlannerUiRoot.SetEnabled(value);
            return true;
        }

        // The mod manager's toggle as the runtime qualification drives it
        // (batch 3 review B7): the same two effects as OnToggle (the update
        // gate and the root's SetEnabled) without ending the runtime test
        // that drives it. While disabled the planner root is not ticked, as
        // with the manager's toggle (which also stops this mod's update as a
        // whole, and would end the runtime test). It never enables a planner
        // that the mod manager itself has disabled.
        internal static void SetEnabledForRuntime(bool value)
        {
            if (value && !_managerEnabled)
            {
                _log.Info("[KBP-BOOT] runtime toggle;value=True refused: the mod manager disabled the mod.");
                return;
            }
            _enabled = value;
            _log.Info("[KBP-BOOT] runtime toggle;value=" + value + ".");
            BuffPlannerUiRoot.SetEnabled(value);
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            RuntimePerformanceDiagnostics.FrameStarted();
            long startedAt = RuntimePerformanceDiagnostics.BeginOperation();
            try
            {
                OnUpdateCore(modEntry, deltaTime);
            }
            finally
            {
                RuntimePerformanceDiagnostics.RecordOperation(
                    RuntimePerformanceOperation.MainUpdate, startedAt);
                RuntimePerformanceDiagnostics.FrameCompleted();
            }
        }

        private static void OnUpdateCore(UnityModManager.ModEntry modEntry, float deltaTime)
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

        private static string _modeMessage = string.Empty;

        private static void OnGui(UnityModManager.ModEntry modEntry)
        {
            try
            {
                DrawPlannerMode();
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-MODE] planner mode panel failed.", exception);
            }
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

        // Everyday-use v1.2 §3: the mod settings page is a normal entry
        // point, so it shows the casting-first planner as THE planner - no
        // Classic choice, no "experimental", no acceptance step. A stored
        // explicit classic choice from an earlier version gets one way
        // forward (PlannerSettingsText decides; this only draws it).
        private static void DrawPlannerMode()
        {
            KingmakerBuffPlanner.Persistence.PlannerMode? mode = BuffPlannerUiRoot.SelectedPlannerMode;
            GUILayout.Label(PlannerSettingsText.Heading);
            GUILayout.Label(PlannerSettingsText.Describe(mode, PlannerHotkey.Binding));
            if (PlannerSettingsText.OffersSwitch(mode) &&
                GUILayout.Button(PlannerSettingsText.SwitchToCastingFirst, GUILayout.ExpandWidth(false)))
                _modeMessage = BuffPlannerUiRoot.TrySetPlannerMode(
                    KingmakerBuffPlanner.Persistence.PlannerMode.CastingFirst) ??
                    PlannerSettingsText.Switched(PlannerHotkey.Binding);
            if (!string.IsNullOrEmpty(_modeMessage)) GUILayout.Label(_modeMessage);
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            _enabled = false;
            if (_runtimeTest != null)
            {
                try { _runtimeTest.Shutdown("mod-unload"); }
                catch (Exception exception) { _log.Error("[KBP-PROBE] unload shutdown failed.", exception); }
            }
            _runtimeTest = null;
            BuffPlannerUiRoot.DestroyOwned();
            PlannerPointerOwnership.Uninstall();
            PlannerHotkey.Uninstall();
            _log.Info("[KBP-BOOT] unloaded;disposed=true.");
            return true;
        }
    }
}
