using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Assets.Console.GamepadInput;
using Kingmaker.UI._ConsoleUI.SpellBook;
using KingmakerBuffPlanner.Infrastructure;
using Rewired;
using TMPro;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    internal static class ControllerSpellbookIntegration
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo BookLayer = typeof(SpellBookView).GetField("m_InputLayer", Members);
        private static readonly FieldInfo Binds = typeof(InputLayer).GetField("m_Binds", Members);
        private static readonly FieldInfo IsShowed = typeof(SpellBookView).GetField("IsShowed", Members);
        private static readonly PropertyInfo ViewModel = typeof(SpellBookView).GetProperty("ViewModel", Members);
        private static readonly ControllerEntryRegistry<InputLayer, BindDescription> Entries =
            new ControllerEntryRegistry<InputLayer, BindDescription>(ControllerRoutineIntegration.DetachEntry);
        private static readonly Dictionary<SpellBookView, Entry> Views = new Dictionary<SpellBookView, Entry>();
        private static ModLog _log;
        private static bool _enabled;
        private sealed class Entry
        {
            internal SpellBookView View;
            internal InputLayer Layer;
            internal GameObject Hint;
            internal int Action;
            internal BindDescription[] Bindings;
        }

        internal static void Install(HarmonyInstance harmony, ModLog log)
        {
            _log = log;
            if (BookLayer == null || BookLayer.FieldType != typeof(InputLayer) || Binds == null ||
                IsShowed == null || IsShowed.FieldType != typeof(bool) || ViewModel == null || ViewModel.PropertyType != typeof(SpellBookVM))
                throw new InvalidOperationException("Unverified controller spellbook input contract.");
            harmony.Patch(typeof(SpellBookView).GetMethod("CreateInputs", Members), null,
                new HarmonyMethod(typeof(ControllerSpellbookIntegration).GetMethod("Created", BindingFlags.Static | BindingFlags.NonPublic)), null);
            harmony.Patch(typeof(SpellBookView).GetMethod("Hide", Members), null,
                new HarmonyMethod(typeof(ControllerSpellbookIntegration).GetMethod("Hidden", BindingFlags.Static | BindingFlags.NonPublic)), null);
        }

        private static bool Ready(Entry entry)
        {
            return _enabled && BuffPlannerUiRoot.ControllerAuthoringAvailable && Application.isFocused && Game.Instance != null && Game.Instance.IsControllerGamepad &&
                GamePad.HasInstance && GamePad.Instance.Player.controllers.joystickCount > 0 &&
                Game.Instance.CurrentMode == Kingmaker.GameModes.GameModeType.FullScreenUi &&
                entry.View != null && Showing(entry.View) && entry.View.gameObject.activeInHierarchy &&
                Model(entry.View) != null && !Model(entry.View).MetamagicBuilderMode.Value &&
                ReferenceEquals(GamePad.Instance.CurrentInputLayer, entry.Layer) && InputLayer.CanReceiveInput();
        }

        internal static bool Showing(SpellBookView view) { return view != null && (bool)IsShowed.GetValue(view); }
        internal static SpellBookVM Model(SpellBookView view) { return view == null ? null : (SpellBookVM)ViewModel.GetValue(view, null); }

        private static void Created(SpellBookView __instance)
        {
            if (!_enabled) return;
            if (Game.Instance == null || !Game.Instance.IsControllerGamepad)
            {
                _log.Info("[KBP-CONTROLLER-BOOK] entry-skipped:controller-unavailable;view=" +
                    (__instance == null ? "null" : __instance.GetInstanceID().ToString()));
                return;
            }
            Entry entry = null;
            try
            {
                Hidden(__instance);
                entry = new Entry { View = __instance, Layer = (InputLayer)BookLayer.GetValue(__instance) };
                // Y/Func02 is RESERVED by native SwitchSpellBook, even when
                // HasManySpellbooks hides its hint. Never borrow it.
                var used = new HashSet<int>(((List<BindDescription>)Binds.GetValue(entry.Layer)).Select(b => b.ActionId));
                // Older PUSHED gameplay layers are unbound while the book is
                // current. Only the book and always-bound base can conflict.
                foreach (InputLayer layer in new[] { GamePad.Instance.BaseLayer })
                    if (layer != null) foreach (BindDescription bind in (List<BindDescription>)Binds.GetValue(layer)) used.Add(bind.ActionId);
                int? action = ChooseAction(used);
                if (!action.HasValue) { _log.Info("[KBP-CONTROLLER-BOOK] no conflict-free footer action;native controls unchanged."); return; }
                entry.Action = action.Value;
                TMP_Text donor = __instance.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t != null && t.font != null);
                if (donor == null)
                {
                    _log.Info("[KBP-CONTROLLER-BOOK] entry-skipped:missing-donor;view=" + __instance.GetInstanceID());
                    return;
                }
                entry.Hint = ControllerRoutineView.BookHint(__instance.transform, donor, action.Value);
                bool added = Entries.Ensure(entry.Layer, (List<BindDescription>)Binds.GetValue(entry.Layer),
                    bind => bind.ActionId == action.Value, alive =>
                    {
                        entry.Bindings = entry.Layer.AddButton(_ =>
                        {
                            if (alive() && entry.Hint != null && entry.Hint.activeInHierarchy && Ready(entry))
                                BuffPlannerUiRoot.RequestControllerEditorFromBook(entry.View, entry.Layer, donor);
                        }, action.Value, InputActionEventType.ButtonJustPressed, false, false).Binds.ToArray();
                        return entry.Bindings;
                    });
                if (!added)
                {
                    UnityEngine.Object.Destroy(entry.Hint);
                    _log.Info("[KBP-CONTROLLER-BOOK] entry-skipped:action-conflict;view=" + __instance.GetInstanceID());
                    return;
                }
                Views.Add(__instance, entry);
                entry.Hint.SetActive(false); // shown only by active-owner Tick
                _log.Info("[KBP-CONTROLLER-BOOK] footer registered;action=" + action.Value + ";destination=actual-editor;partyWide=true.");
            }
            catch (Exception exception)
            {
                Hidden(__instance);
                if (entry != null)
                {
                    if (entry.Layer != null) Entries.Remove(entry.Layer);
                    if (entry.Hint != null) UnityEngine.Object.Destroy(entry.Hint);
                }
                _log.Error("[KBP-CONTROLLER-BOOK] entry unavailable;native spellbook retained.", exception);
            }
        }

        internal static int? ChooseAction(IEnumerable<int> used)
        {
            var occupied = new HashSet<int>(used);
            return new[] { RewiredConsts.Action.RightStickButton, RewiredConsts.Action.FuncAdditional }
                .Where(id => !occupied.Contains(id)).Select(id => (int?)id).FirstOrDefault();
        }

        internal static bool AllowsDispatch(BindDescription binding)
        {
            foreach (Entry entry in Views.Values)
                if (binding.ActionId == entry.Action && entry.Hint != null && entry.Hint.activeInHierarchy && Ready(entry))
                    return entry.Bindings != null && entry.Bindings.Any(owned => ReferenceEquals(owned, binding));
            return true;
        }

        private static void Hidden(SpellBookView __instance)
        {
            Entry entry;
            if (!Views.TryGetValue(__instance, out entry)) return;
            Views.Remove(__instance);
            try { Entries.Remove(entry.Layer); }
            finally { if (entry.Hint != null) UnityEngine.Object.Destroy(entry.Hint); }
        }

        internal static void Tick()
        {
            foreach (Entry entry in Views.Values.ToArray())
            {
                if (entry.View == null) { Hidden(entry.View); continue; }
                bool show = Ready(entry);
                if (entry.Hint != null && entry.Hint.activeSelf != show) entry.Hint.SetActive(show);
            }
        }

        internal static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled) foreach (SpellBookView view in Views.Keys.ToArray()) Hidden(view);
        }
    }
}
