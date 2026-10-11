using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UI.Tooltip;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Identity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // Native UI finish (UI-01): a spell's or ability's description is the
    // game's own description window - the scroll the spellbook opens on a
    // right click (StaticCanvas/Tooltips/DescWinConstructor, opened through
    // TooltipTrigger.SetObject + OpenDescriptionWindow, exact 2.1.7b
    // contract) - never a planner-drawn panel.
    //
    // The planner's canvas sorts above StaticCanvas, so while a planner
    // window is open the game's tooltip layer is lifted above it through an
    // owned override canvas (with its own raycaster) on StaticCanvas/
    // Tooltips; Dispose removes both and leaves the native layer as it was.
    // Showing a description never changes the plan.
    internal sealed class NativeDescriptionPresenter : IDisposable
    {
        internal const string TooltipLayerPath = "Tooltips";
        private readonly Action<string> _log;
        private readonly Canvas _plannerCanvas;
        private GameObject _triggerObject;
        private TooltipTrigger _trigger;
        private Canvas _liftCanvas;
        private GraphicRaycaster _liftRaycaster;
        private bool _disposed;

        internal NativeDescriptionPresenter(Canvas plannerCanvas, Transform owner, Action<string> log)
        {
            _plannerCanvas = plannerCanvas ?? throw new ArgumentNullException("plannerCanvas");
            _log = log ?? (value => { });
            // The trigger the native window is opened through: owned, hidden
            // from pointer input (no graphic), never hovered.
            _triggerObject = new GameObject("KBP.NativeDescriptionTrigger", typeof(RectTransform));
            _triggerObject.transform.SetParent(owner, false);
            _trigger = _triggerObject.AddComponent<TooltipTrigger>();
        }

        internal string LastSubject { get; private set; }
        internal string LastFailure { get; private set; }

        internal string LiftEvidence
        {
            get
            {
                return _liftCanvas == null ? "lift=none" : "lift=override-canvas;order=" + _liftCanvas.sortingOrder +
                    ";planner=" + _plannerCanvas.sortingOrder + ";raycaster=" + (_liftRaycaster != null);
            }
        }

        internal static DescriptionWindow Window
        {
            get
            {
                DescriptionController controller = Game.Instance == null || Game.Instance.UI == null
                    ? null : Game.Instance.UI.DescriptionController;
                return controller == null ? null : controller.DescWindow;
            }
        }

        // Shown, as the window itself counts it: CloseWindow clears its
        // m_IsShowed at once and deactivates only after the disappear
        // animation (exact 2.1.7b contract), so a closing window is closed.
        private static readonly System.Reflection.FieldInfo ShownField =
            typeof(DescriptionWindowBase).GetField("m_IsShowed",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        internal static bool IsOpen
        {
            get
            {
                DescriptionWindow window = Window;
                if (window == null || !window.gameObject.activeInHierarchy) return false;
                return ShownField == null || (bool)ShownField.GetValue(window);
            }
        }

        // The shown description's title (its largest text) and body text.
        internal static string Title
        {
            get
            {
                DescriptionWindow window = Window;
                if (window == null || !IsOpen) return null;
                TMP_Text title = window.GetComponentsInChildren<TMP_Text>(false)
                    .Where(text => text != null && !string.IsNullOrEmpty(text.text))
                    .OrderByDescending(text => text.fontSize).FirstOrDefault();
                return title == null ? null : title.text;
            }
        }

        internal static string Body
        {
            get
            {
                DescriptionWindow window = Window;
                if (window == null || !IsOpen) return null;
                return string.Join("\n", window.GetComponentsInChildren<TMP_Text>(false)
                    .Where(text => text != null && !string.IsNullOrEmpty(text.text))
                    .Select(text => text.text).ToArray());
            }
        }

        internal static ScrollRect Scroll
        {
            get { return IsOpen ? Window.Scroll : null; }
        }

        internal static RectTransform Panel
        {
            get
            {
                DescriptionWindow window = Window;
                if (window == null || !IsOpen) return null;
                return (window.MainContainer as RectTransform) ?? (RectTransform)window.transform;
            }
        }

        // Opens the native description of an ability: the caster's own
        // ability (their spellbook spell or ability, the exact variant when
        // the casting names it) when the caster is known, otherwise the
        // ability's blueprint (the base spell unless exactVariant).
        internal bool Open(AbilityKey ability, bool exactVariant, string casterUnitId)
        {
            LastFailure = null;
            if (_disposed || ability == null)
            {
                LastFailure = _disposed ? "presenter-disposed" : "no-ability";
                return false;
            }
            object subject;
            try
            {
                subject = Resolve(ability, exactVariant, casterUnitId);
            }
            catch (Exception exception)
            {
                LastFailure = "resolve:" + exception.GetType().Name + ":" + exception.Message;
                _log("[KBP-DESCRIPTION] " + LastFailure);
                return false;
            }
            if (subject == null)
            {
                LastFailure = "ability-not-loaded:" + ability.BaseAbilityGuid;
                _log("[KBP-DESCRIPTION] " + LastFailure);
                return false;
            }
            Lift();
            try
            {
                _trigger.SetObject(subject);
                _trigger.OpenDescriptionWindow();
            }
            catch (Exception exception)
            {
                LastFailure = "open:" + exception.GetType().Name + ":" + exception.Message;
                _log("[KBP-DESCRIPTION] " + LastFailure);
                return false;
            }
            LastSubject = Describe(subject);
            return true;
        }

        // A titled text in the same native window (the runtime layout probe
        // of long content; the HUD tooltips use the same native text route).
        internal bool OpenText(string title, string text)
        {
            LastFailure = null;
            if (_disposed) return false;
            Lift();
            try
            {
                _trigger.SetObject(null);
                _trigger.SetNameAndDescription(title ?? string.Empty, text ?? string.Empty);
                _trigger.OpenDescriptionWindow();
            }
            catch (Exception exception)
            {
                LastFailure = "open-text:" + exception.GetType().Name + ":" + exception.Message;
                _log("[KBP-DESCRIPTION] " + LastFailure);
                return false;
            }
            LastSubject = "text:" + (title ?? string.Empty);
            return IsOpen;
        }

        // Escape closes the description first (the innermost window).
        internal static bool CloseIfOpen()
        {
            if (!IsOpen) return false;
            Window.CloseWindow();
            return true;
        }

        private void Lift()
        {
            if (_liftCanvas != null) return;
            StaticCanvas canvas = StaticCanvas.Instance;
            Transform layer = canvas == null ? null : canvas.transform.Find(TooltipLayerPath);
            if (layer == null)
            {
                _log("[KBP-DESCRIPTION] tooltip layer missing;path=" + TooltipLayerPath + ".");
                return;
            }
            if (layer.GetComponent<Canvas>() != null)
            {
                // Never take over a canvas the planner did not add.
                _log("[KBP-DESCRIPTION] tooltip layer already a canvas; not lifted.");
                return;
            }
            _liftCanvas = layer.gameObject.AddComponent<Canvas>();
            _liftCanvas.overrideSorting = true;
            _liftCanvas.sortingLayerID = _plannerCanvas.sortingLayerID;
            _liftCanvas.sortingOrder = _plannerCanvas.sortingOrder + 1;
            _liftRaycaster = layer.gameObject.AddComponent<GraphicRaycaster>();
            _log("[KBP-DESCRIPTION] tooltip layer lifted;" + LiftEvidence + ".");
        }

        private static object Resolve(AbilityKey ability, bool exactVariant, string casterUnitId)
        {
            BlueprintAbility baseBlueprint = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(ability.BaseAbilityGuid);
            BlueprintAbility variant = exactVariant && !string.IsNullOrWhiteSpace(ability.VariantGuid)
                ? ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(ability.VariantGuid) : null;
            BlueprintAbility shown = variant ?? baseBlueprint;
            if (shown == null) return null;
            UnitEntityData caster = string.IsNullOrEmpty(casterUnitId) ? null : FindUnit(casterUnitId);
            if (caster == null || caster.Descriptor == null) return shown;
            foreach (Spellbook book in caster.Descriptor.Spellbooks)
            {
                AbilityData known = KnownSpells(book).FirstOrDefault(data => data != null &&
                    (data.Blueprint == baseBlueprint || data.Blueprint == shown));
                if (known == null) continue;
                return variant != null && known.Blueprint != variant ? new AbilityData(known, variant) : known;
            }
            return new AbilityData(shown, caster.Descriptor);
        }

        private static IEnumerable<AbilityData> KnownSpells(Spellbook book)
        {
            if (book == null) yield break;
            foreach (AbilityData data in book.GetAllKnownSpells()) yield return data;
            for (int level = 0; level <= book.MaxSpellLevel; level++)
                foreach (AbilityData data in book.GetCustomSpells(level)) yield return data;
        }

        private static UnitEntityData FindUnit(string unitId)
        {
            if (Game.Instance == null || Game.Instance.Player == null) return null;
            foreach (UnitEntityData unit in Game.Instance.Player.Party ?? new List<UnitEntityData>())
            {
                if (unit == null) continue;
                if (unit.UniqueId == unitId) return unit;
                if (unit.Descriptor != null && unit.Descriptor.Pet != null && unit.Descriptor.Pet.UniqueId == unitId)
                    return unit.Descriptor.Pet;
            }
            return null;
        }

        private static string Describe(object subject)
        {
            var data = subject as AbilityData;
            if (data != null)
                return "ability-data:" + (data.Blueprint == null ? "?" : data.Blueprint.AssetGuid) +
                    (data.Spellbook == null ? string.Empty : ";spellbook=" + data.Spellbook.Blueprint.AssetGuid);
            var blueprint = subject as BlueprintAbility;
            return blueprint == null ? subject.GetType().Name : "blueprint:" + blueprint.AssetGuid;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { CloseIfOpen(); }
            catch (Exception exception) { _log("[KBP-DESCRIPTION] close on dispose failed: " + exception.Message); }
            // The raycaster depends on the canvas: it goes first.
            if (_liftRaycaster != null) UnityEngine.Object.Destroy(_liftRaycaster);
            if (_liftCanvas != null) UnityEngine.Object.Destroy(_liftCanvas);
            if (_triggerObject != null) UnityEngine.Object.Destroy(_triggerObject);
            _liftRaycaster = null;
            _liftCanvas = null;
            _triggerObject = null;
            _trigger = null;
        }
    }
}
