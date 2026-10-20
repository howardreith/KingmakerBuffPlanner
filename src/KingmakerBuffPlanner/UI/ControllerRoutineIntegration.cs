using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Assets.Console.GamepadInput;
using Kingmaker.GameModes;
using Kingmaker.UI._ConsoleUI.InGameMenu;
using KingmakerBuffPlanner.Infrastructure;
using Rewired;
using TMPro;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    // Optional, exact 2.1.7b controller adapter. Failure leaves the existing
    // mouse routes intact; nothing changes the game's input-mode setting.
    internal static class ControllerRoutineIntegration
    {
        private const string HarmonyId = "KingmakerBuffPlanner.ControllerRoutines";
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static HarmonyInstance _harmony;
        private static MethodInfo _getLayer;
        private static MethodInfo _dispatch;
        private static MethodInfo _deactivateText;
        private static MethodInfo _deselect;
        private static FieldInfo _binds;
        private static FieldInfo _actionsUsed;
        private static ModLog _log;
        private static bool _enabled;
        private static readonly Dictionary<BaseGameMenuView, GameObject> Hints =
            new Dictionary<BaseGameMenuView, GameObject>();
        private static readonly Dictionary<BaseGameMenuView, InputLayer> EntryLayers =
            new Dictionary<BaseGameMenuView, InputLayer>();
        private static readonly ControllerEntryRegistry<InputLayer, BindDescription> Entries =
            new ControllerEntryRegistry<InputLayer, BindDescription>(DetachEntry);

        internal static ControllerRoutineMenu ActiveMenu { get; set; }

        internal static void Install(ModLog log)
        {
            if (_harmony != null) return;
            _log = log;
            _getLayer = typeof(BaseGameMenuView).GetMethod("GetInputLayer", InstanceMembers, null, Type.EmptyTypes, null);
            _deselect = typeof(BaseGameMenuView).GetMethod("Deselect", InstanceMembers, null, Type.EmptyTypes, null);
            _dispatch = typeof(BindDescription).GetMethod("Handler", InstanceMembers, null,
                new[] { typeof(InputActionEventData) }, null);
            _binds = typeof(InputLayer).GetField("m_Binds", InstanceMembers);
            _actionsUsed = typeof(InputLayer).GetField("m_ActionsUsed", InstanceMembers);
            _deactivateText = typeof(TMP_InputField).GetMethod("DeactivateInputField", InstanceMembers, null, Type.EmptyTypes, null);
            if (_getLayer == null || _getLayer.ReturnType != typeof(InputLayer) || _deselect == null ||
                _dispatch == null || _binds == null || _binds.FieldType != typeof(List<BindDescription>) ||
                _actionsUsed == null || _actionsUsed.FieldType != typeof(Dictionary<int, BindDescription>) ||
                _deactivateText == null || _deactivateText.ReturnType != typeof(void))
                throw new InvalidOperationException("The native controller routine contracts do not match this installation.");
            _harmony = HarmonyInstance.Create(HarmonyId);
            try
            {
                _harmony.Patch(_getLayer, null, new HarmonyMethod(typeof(ControllerRoutineIntegration)
                    .GetMethod("MenuLayerPostfix", BindingFlags.Static | BindingFlags.NonPublic)), null);
                _harmony.Patch(_dispatch, new HarmonyMethod(typeof(ControllerRoutineIntegration)
                    .GetMethod("DispatchPrefix", BindingFlags.Static | BindingFlags.NonPublic)), null, null);
                _harmony.Patch(_deactivateText, new HarmonyMethod(typeof(ControllerRoutineIntegration)
                    .GetMethod("TextDeactivatePrefix", BindingFlags.Static | BindingFlags.NonPublic)), null, null);
                try { ControllerSpellbookIntegration.Install(_harmony, log); }
                catch (Exception exception) { log.Error("[KBP-CONTROLLER-BOOK] optional footer contracts unavailable;quick access retained.", exception); }
                ControllerNativeMenuCard.OpenRequested = BuffPlannerUiRoot.RequestControllerEditorFromCard;
                ControllerNativeMenuCard.Install(_harmony, log);
            }
            catch { Uninstall(); throw; }
            _log.Info("[KBP-CONTROLLER] native menu entry and scoped binding ownership installed;action=Func01;event=ButtonJustPressed.");
        }

        internal static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            ControllerSpellbookIntegration.SetEnabled(enabled);
            ControllerNativeMenuCard.SetEnabled(enabled);
            if (!enabled && ActiveMenu != null) ActiveMenu.Dispose();
            foreach (GameObject hint in Hints.Values)
                if (hint != null) hint.SetActive(enabled);
        }

        private static void MenuLayerPostfix(BaseGameMenuView __instance, InputLayer __result)
        {
            if (!_enabled || __instance == null || __result == null) return;
            // Capture the campaign card's layer even if the optional Func01
            // entry is unavailable in the current game mode. The card has its
            // own native Confirm route and is bound before GetInputLayer.
            try { ControllerNativeMenuCard.NoteLayer(__instance, __result); }
            catch (Exception exception) { _log.Error("[KBP-CONTROLLER-CARD] layer capture failed.", exception); }
            if (Game.Instance == null ||
                !Game.Instance.IsControllerGamepad || Game.Instance.CurrentMode != GameModeType.Default ||
                Game.Instance.RootUiContext == null || Game.Instance.RootUiContext.InGameUiContext == null) return;
            GameObject hint = null;
            try
            {
                foreach (BaseGameMenuView stale in EntryLayers.Keys.Where(candidate => candidate == null).ToArray())
                    RemoveEntry(stale);
                InputLayer previousLayer;
                if (EntryLayers.TryGetValue(__instance, out previousLayer) && !ReferenceEquals(previousLayer, __result))
                    RemoveEntry(__instance);
                GameObject previous;
                if (Hints.TryGetValue(__instance, out previous) && previous != null) UnityEngine.Object.Destroy(previous);
                Hints.Remove(__instance);
                TMP_Text donor = __instance.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(label => label != null && label.font != null);
                if (donor == null) throw new InvalidOperationException("Native controller menu text is unavailable.");
                hint = ControllerRoutineView.EntryHint(__instance.transform, donor);
                BaseGameMenuView view = __instance;
                InputLayer entryLayer = __result;
                bool installed = Entries.Ensure(entryLayer, (List<BindDescription>)_binds.GetValue(entryLayer),
                    bind => bind.ActionId == RewiredConsts.Action.Func01, alive =>
                {
                    InputBindStruct registration = entryLayer.AddButton(_ =>
                    {
                        if (!alive() || !_enabled || !Application.isFocused || view == null || !view.gameObject.activeInHierarchy ||
                            !GamePad.HasInstance || GamePad.Instance.CurrentInputLayer != entryLayer ||
                            GamePad.Instance.Player.controllers.joystickCount == 0) return;
                        BuffPlannerUiRoot.OpenControllerRoutines(view, donor);
                    }, RewiredConsts.Action.Func01, InputActionEventType.ButtonJustPressed, false, false);
                    return registration.Binds.ToArray();
                });
                if (!installed)
                {
                    UnityEngine.Object.Destroy(hint);
                    _log.Info("[KBP-CONTROLLER] entry unavailable;native-menu-action-conflict=Func01.");
                    return;
                }
                EntryLayers[view] = entryLayer;
                Hints.Add(view, hint);
            }
            catch (Exception exception)
            {
                if (hint != null) UnityEngine.Object.Destroy(hint);
                try { RemoveEntry(__instance); }
                catch (Exception cleanup) { _log.Error("[KBP-CONTROLLER] entry cleanup failed.", cleanup); }
                _log.Error("[KBP-CONTROLLER] native entry unavailable;mouse routes remain available.", exception);
            }
        }

        private static void RemoveEntry(BaseGameMenuView view)
        {
            InputLayer layer;
            if (EntryLayers.TryGetValue(view, out layer)) Entries.Remove(layer);
            EntryLayers.Remove(view);
            GameObject hint;
            if (Hints.TryGetValue(view, out hint) && hint != null) UnityEngine.Object.Destroy(hint);
            Hints.Remove(view);
        }

        internal static void DetachEntry(InputLayer layer, BindDescription binding)
        {
            // AddButton has no disposable/remove API in 2.1.7b. Mirror its
            // verified Rewired unbind, but never unbind/rebind the whole layer
            // or clear an action slot a newer foreign binding now owns.
            binding.ActionHandler = _ => { };
            try
            {
                if (binding.Bound && GamePad.HasInstance)
                {
                    var handler = (Action<InputActionEventData>)Delegate.CreateDelegate(
                        typeof(Action<InputActionEventData>), binding, _dispatch);
                    GamePad.Instance.Player.RemoveInputEventDelegate(handler, UpdateLoopType.Update,
                        binding.EventType, binding.ActionId);
                }
                binding.Bound = false;
            }
            finally
            {
                ((List<BindDescription>)_binds.GetValue(layer)).RemoveAll(candidate => ReferenceEquals(candidate, binding));
                var actions = (Dictionary<int, BindDescription>)_actionsUsed.GetValue(layer);
                BindDescription current;
                if (actions.TryGetValue(binding.ActionId, out current) && ReferenceEquals(current, binding))
                    actions.Remove(binding.ActionId);
            }
        }

        // Deselect clears the menu's authored native selection before Confirm
        // closes it, preventing an Inventory/Rest/etc. action from also firing.
        internal static void CloseNativeMenu(BaseGameMenuView view)
        {
            _deselect.Invoke(view, null);
        }

        private static void TextDeactivatePrefix(TMP_InputField __instance)
        {
            // Observe only, never cancel/replace TMP's own text behavior. The
            // active boundary filters exact selection and owned input lifetime.
            ControllerEditorInputBoundary editor = ControllerEditorInputBoundary.Active;
            if (editor == null) return;
            try { editor.ObserveTextCancellation(__instance); }
            catch (Exception exception)
            { _log.Error("[KBP-CONTROLLER] text cancellation observation failed.", exception); }
        }

        private static bool DispatchPrefix(BindDescription __instance)
        {
            try
            {
                if (ControllerTransitionInputGuard.Active != null) return ControllerTransitionInputGuard.Active.Allows(__instance);
                if (!ControllerSpellbookIntegration.AllowsDispatch(__instance)) return false;
                ControllerEditorInputBoundary editor = ControllerEditorInputBoundary.Active;
                if (editor != null) return editor.Allows(__instance);
                ControllerRoutineMenu menu = ActiveMenu;
                return menu == null || menu.AllowsNativeDispatch(__instance);
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-CONTROLLER] input ownership failed;releasing owned layer.", exception);
                try
                {
                    if (ActiveMenu != null) ActiveMenu.Dispose();
                    if (ControllerEditorInputBoundary.Active != null) ControllerEditorInputBoundary.Active.RestoreState(null);
                    ControllerSpellbookIntegration.SetEnabled(false);
                }
                catch (Exception cleanup) { _log.Error("[KBP-CONTROLLER] input cleanup failed;native state uncertain.", cleanup); }
                return true;
            }
        }

        internal static void Uninstall()
        {
            try
            {
                try { SetEnabled(false); }
                finally
                {
                    try { if (ControllerTransitionInputGuard.Active != null) ControllerTransitionInputGuard.Active.Dispose(); }
                    finally { Entries.Clear(); }
                }
            }
            finally
            {
                foreach (GameObject hint in Hints.Values) if (hint != null) UnityEngine.Object.Destroy(hint);
                Hints.Clear();
                EntryLayers.Clear();
                if (_harmony != null)
                {
                    if (_getLayer != null) _harmony.Unpatch(_getLayer, HarmonyPatchType.Postfix, HarmonyId);
                    if (_dispatch != null) _harmony.Unpatch(_dispatch, HarmonyPatchType.Prefix, HarmonyId);
                    if (_deactivateText != null) _harmony.Unpatch(_deactivateText, HarmonyPatchType.Prefix, HarmonyId);
                    foreach (string method in new[] { "InitializeCartCollection", "BindEntities" })
                        _harmony.Unpatch(typeof(InGameMenuView).GetMethod(method, InstanceMembers), HarmonyPatchType.Postfix, HarmonyId);
                    foreach (string method in new[] { "CreateInputs", "Hide" })
                        _harmony.Unpatch(typeof(Kingmaker.UI._ConsoleUI.SpellBook.SpellBookView).GetMethod(method, InstanceMembers), HarmonyPatchType.Postfix, HarmonyId);
                }
                _harmony = null;
                ControllerNativeMenuCard.OpenRequested = null;
            }
        }
    }
}
