using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Assets.Console.GamepadInput;
using Kingmaker.UI._ConsoleUI.Common;
using Kingmaker.UI._ConsoleUI.InGameMenu;
using Kingmaker.UI._ConsoleUI.Utils.MultiNavigationTool;
using KingmakerBuffPlanner.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // Native contract pattern credited in THIRD-PARTY-NOTICES. Unlike cloning
    // a bound card, this copies ONLY passive rect/image/text presentation into
    // a new native entity view: no foreign VM, callback, event or animator.
    internal static class ControllerNativeMenuCard
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo Rest = typeof(InGameMenuView).GetField("m_RestView", Fields);
        private static readonly FieldInfo Collection = typeof(BaseGameMenuView).GetField("m_CartCollection", Fields);
        private static readonly FieldInfo Cards = typeof(CartCollectionManagerView).GetField("m_CartList", Fields);
        private static readonly FieldInfo Models = typeof(InGameMenuVM).GetField("m_EntityVms", Fields);
        private static readonly FieldInfo Title = typeof(InGameMenuEntityView).GetField("m_Title", Fields);
        private static readonly FieldInfo Rect = typeof(InGameMenuEntityView).GetField("m_Rect", Fields);
        private static readonly FieldInfo Navigation = typeof(BaseGameMenuView).GetField("m_NavigationCollection", Fields);
        private static readonly Dictionary<InGameMenuView, Entry> Entries = new Dictionary<InGameMenuView, Entry>();
        private static readonly ControllerMenuLayerCapture<InGameMenuView, InputLayer> LayerCaptures =
            new ControllerMenuLayerCapture<InGameMenuView, InputLayer>();
        private static ModLog _log;
        private static bool _enabled;
        internal static Func<InGameMenuView, InputLayer, InputLayer, TMP_Text, Canvas, int, bool> OpenRequested { get; set; }
        private sealed class Entry
        {
            internal InGameMenuView Menu;
            internal InGameMenuEntityView Card;
            internal CartCollectionManagerView Collection;
            internal InGameMenuVM MenuModel;
            internal InGameMenuEntityVM Model;
            internal List<List<IConsoleMultiNavigationEntity>> Rows;
            internal InputLayer Layer, Below;
            internal PlannerNativeKit Kit;
            internal Canvas Host;
            internal Texture2D Texture;
            internal Sprite Icon;
            internal Transform OwnedRoot;
            internal bool Removed;
            internal int Generation;
        }

        internal static void Install(HarmonyInstance harmony, ModLog log)
        {
            _log = log;
            if (Rest == null || Collection == null || Cards == null || Models == null || Title == null || Rect == null || Navigation == null ||
                Cards.FieldType != typeof(List<ICartCollectionEntity>) || Models.FieldType != typeof(List<InGameMenuEntityVM>))
                throw new InvalidOperationException("Native campaign card contracts are unavailable.");
            harmony.Patch(typeof(InGameMenuView).GetMethod("InitializeCartCollection", Fields), null,
                new HarmonyMethod(typeof(ControllerNativeMenuCard).GetMethod("Created", BindingFlags.Static | BindingFlags.NonPublic)), null);
            harmony.Patch(typeof(InGameMenuView).GetMethod("BindEntities", Fields), null,
                new HarmonyMethod(typeof(ControllerNativeMenuCard).GetMethod("Bound", BindingFlags.Static | BindingFlags.NonPublic)), null);
        }

        internal static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled)
            {
                foreach (Entry entry in Entries.Values.ToArray()) SafeRemove(entry);
                LayerCaptures.Clear();
            }
        }

        private static void Created(InGameMenuView __instance)
        {
            if (!_enabled) return;
            Entry entry = null;
            try
            {
                foreach (Entry stale in Entries.Values.Where(e => e.Menu == null).ToArray()) SafeRemove(stale);
                if (!Entries.TryGetValue(__instance, out entry) || entry.Card == null)
                {
                    if (entry != null) SafeRemove(entry);
                    InGameMenuEntityView donor = (InGameMenuEntityView)Rest.GetValue(__instance);
                    if (donor == null) throw new InvalidOperationException("Native campaign card art is not ready.");
                    Canvas host = ControllerPresentationHost.Resolve(__instance);
                    entry = new Entry { Menu = __instance, Collection = (CartCollectionManagerView)Collection.GetValue(__instance),
                        Host = host, Kit = PlannerNativeKit.Controller(host) };
                    var map = new Dictionary<Transform, Transform>();
                    Transform owned = CopyVisual(donor.transform, donor.transform.parent, entry.Kit, map,
                        created => { entry.OwnedRoot = created; created.gameObject.SetActive(false); });
                    owned.name = "KingmakerBuffPlanner.NativeCard";
                    owned.gameObject.SetActive(false);
                    entry.Card = owned.gameObject.AddComponent<InGameMenuEntityView>();
                    Rect.SetValue(entry.Card, owned as RectTransform);
                    TMP_Text donorTitle = (TMP_Text)Title.GetValue(donor);
                    Title.SetValue(entry.Card, map[donorTitle.transform].GetComponent<TextMeshProUGUI>());
                    MakeIcon(entry);
                    foreach (string name in new[] { "BigIcon", "SmallIcon" })
                    {
                        Transform icon = owned.Find("Decor/" + name);
                        Image image = icon == null ? null : icon.GetComponent<Image>();
                        if (image != null) { image.sprite = entry.Icon; image.preserveAspect = true; image.color = Color.white; }
                    }
                    Entries[__instance] = entry;
                    InputLayer source, below;
                    if (LayerCaptures.TryGet(__instance, out source, out below))
                    { entry.Layer = source; entry.Below = below; }
                }
                var current = (List<ICartCollectionEntity>)Cards.GetValue(entry.Collection);
                if (!current.Any(card => ReferenceEquals(card, entry.Card)))
                {
                    var updated = new List<ICartCollectionEntity>(current) { entry.Card };
                    Component previous = current.LastOrDefault() as Component;
                    if (previous != null && current.Count > 1 && current[0] is Component && current[1] is Component)
                    {
                        bool over = ((Component)current[1]).transform.GetSiblingIndex() > ((Component)current[0]).transform.GetSiblingIndex();
                        entry.Card.transform.SetAsLastSibling();
                        entry.Card.transform.SetSiblingIndex(previous.transform.GetSiblingIndex() + (over ? 1 : 0));
                    }
                    entry.Collection.Initialize(updated);
                }
            }
            catch (Exception exception)
            {
                if (entry != null) SafeRemove(entry);
                _log.Error("[KBP-CONTROLLER-CARD] construction failed;native entries retained.", exception);
            }
        }

        private static Transform CopyVisual(Transform donor, Transform parent, PlannerNativeKit kit, Dictionary<Transform, Transform> map, Action<Transform> created = null)
        {
            RectTransform original = donor as RectTransform;
            if (original == null) throw new InvalidOperationException("Unexpected native card transform.");
            RectTransform owned = KingmakerUiFactory.CreateRect(donor.name, parent);
            if (created != null) created(owned);
            owned.gameObject.layer = donor.gameObject.layer;
            map.Add(donor, owned);
            TMP_Text text = donor.GetComponent<TMP_Text>();
            if (text != null)
            {
                UnityEngine.Object.DestroyImmediate(owned.gameObject);
                TextMeshProUGUI label = kit.Text(parent, donor.name, text.text, PlannerTextRole.Title, text.fontSize, text.alignment);
                owned = label.rectTransform; map[donor] = owned;
                label.color = text.color; label.enableWordWrapping = text.enableWordWrapping;
            }
            owned.anchorMin = original.anchorMin; owned.anchorMax = original.anchorMax; owned.pivot = original.pivot;
            owned.sizeDelta = original.sizeDelta; owned.anchoredPosition3D = original.anchoredPosition3D;
            owned.localRotation = original.localRotation; owned.localScale = original.localScale;
            Image image = donor.GetComponent<Image>();
            if (image != null)
            {
                Image copy = owned.gameObject.AddComponent<Image>();
                copy.sprite = image.sprite; copy.type = image.type; copy.color = image.color;
                copy.preserveAspect = image.preserveAspect; copy.raycastTarget = false;
            }
            for (int i = 0; i < donor.childCount; i++) CopyVisual(donor.GetChild(i), owned, kit, map);
            owned.gameObject.SetActive(donor.gameObject.activeSelf);
            return owned;
        }

        private static void MakeIcon(Entry entry)
        {
            // Owned code-drawn open ledger/checklist; no exported game pixels.
            entry.Texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                bool border = x >= 8 && x <= 55 && y >= 7 && y <= 56 && (x < 12 || x > 51 || y < 11 || y > 52 || x == 31 || x == 32);
                bool line = y >= 17 && y <= 46 && y % 12 < 3 && ((x >= 17 && x <= 26) || (x >= 37 && x <= 47));
                pixels[y * 64 + x] = border || line ? new Color32(230, 192, 112, 255) : new Color32(0, 0, 0, 0);
            }
            entry.Texture.SetPixels32(pixels); entry.Texture.Apply();
            entry.Icon = Sprite.Create(entry.Texture, new UnityEngine.Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
        }

        private static void Bound(InGameMenuView __instance, InGameMenuVM viewModel, List<List<IConsoleMultiNavigationEntity>> __result)
        {
            if (!_enabled) return;
            Created(__instance);
            Entry entry;
            if (!Entries.TryGetValue(__instance, out entry) || entry.Card == null || __result == null || __result.Count == 0) return;
            try
            {
                Unbind(entry);
                // Native BindEntities precedes GetInputLayer on this build. A
                // capture used by an earlier binding must not serve a new VM.
                InputLayer observed, predecessor;
                if (LayerCaptures.TryGet(__instance, out observed, out predecessor) &&
                    ReferenceEquals(observed, entry.Layer) && entry.Generation > 0)
                { LayerCaptures.Remove(__instance); entry.Layer = null; entry.Below = null; }
                entry.Generation++;
                bool fired = false;
                Game game = Game.Instance;
                object area = game.CurrentlyLoadedArea;
                object ui = game.RootUiContext.InGameUiContext;
                string campaign = game.Player.GameId;
                entry.Model = new InGameMenuEntityVM(() =>
                {
                    if (fired) return;
                    fired = true;
                    _log.Info("[KBP-CONTROLLER-CARD] card-selected;" + Describe(entry));
                    if (entry.Removed || !_enabled || entry.Menu == null || Game.Instance != game ||
                        game.Player == null || game.Player.GameId != campaign || !ReferenceEquals(game.CurrentlyLoadedArea, area) ||
                        game.RootUiContext == null || !ReferenceEquals(game.RootUiContext.InGameUiContext, ui))
                    { _log.Info("[KBP-CONTROLLER-CARD] request-refused:stale-context;" + Describe(entry)); return; }
                    if (entry.Layer == null || !LayerCaptures.TryGet(entry.Menu, out observed, out predecessor) ||
                        !ReferenceEquals(observed, entry.Layer))
                    { _log.Info("[KBP-CONTROLLER-CARD] request-refused:missing-layer-capture;" + Describe(entry)); return; }
                    try
                    {
                        if (OpenRequested == null)
                            _log.Info("[KBP-CONTROLLER-CARD] request-refused:handler-unavailable;" + Describe(entry));
                        else if (!OpenRequested(entry.Menu, entry.Layer, entry.Below,
                            (TMP_Text)Title.GetValue(entry.Card), entry.Host, entry.Generation))
                            _log.Info("[KBP-CONTROLLER-CARD] request-refused:" +
                                (BuffPlannerUiRoot.HasInstance ? "admission" : "root-absent") + ";" + Describe(entry));
                    }
                    catch (Exception exception)
                    { _log.Error("[KBP-CONTROLLER-CARD] request-refused:exception;" + Describe(entry), exception); }
                }, "Buff Planner", false, true);
                entry.MenuModel = viewModel; entry.Rows = __result;
                ControllerOwnedItems.Ensure((List<InGameMenuEntityVM>)Models.GetValue(viewModel), entry.Model);
                entry.Card.Bind(entry.Model);
                ControllerOwnedItems.Ensure(__result[0], entry.Card);
            }
            catch (Exception exception) { SafeRemove(entry); _log.Error("[KBP-CONTROLLER-CARD] binding failed;native menu retained.", exception); }
        }

        internal static void NoteLayer(BaseGameMenuView menu, InputLayer layer)
        {
            if (!(menu is InGameMenuView) || !GamePad.HasInstance) return;
            var native = (InGameMenuView)menu;
            var pad = GamePad.Instance;
            if (!LayerCaptures.Observe(native, layer, pad.CurrentInputLayer, pad.BaseLayer, pad.Layers))
            { _log.Info("[KBP-CONTROLLER-CARD] layer-capture-refused;menu=" + native.GetInstanceID()); return; }
            Entry entry;
            if (Entries.TryGetValue(native, out entry))
            { LayerCaptures.TryGet(native, out entry.Layer, out entry.Below); }
            _log.Info("[KBP-CONTROLLER-CARD] layer-captured;menu=" + native.GetInstanceID() +
                ";generation=" + (entry == null ? 0 : entry.Generation) + ";source=" + DescribeLayer(layer) +
                ";below=" + DescribeLayer(entry == null ? CapturedBelow(native) : entry.Below) +
                ";current=" + DescribeLayer(pad.CurrentInputLayer) + ";base=" + DescribeLayer(pad.BaseLayer) +
                ";sourceInStack=" + pad.Layers.Contains(layer) +
                ";mode=" + (Game.Instance == null ? "null" : Game.Instance.CurrentMode.ToString()) +
                ";visible=" + native.gameObject.activeInHierarchy);
        }

        private static InputLayer CapturedBelow(InGameMenuView menu)
        { InputLayer source, below; LayerCaptures.TryGet(menu, out source, out below); return below; }

        private static string DescribeLayer(InputLayer layer)
        { return layer == null ? "null" : layer.ContextName + "#" + RuntimeHelpers.GetHashCode(layer); }

        private static string Describe(Entry entry)
        { return "menu=" + (entry.Menu == null ? "null" : entry.Menu.GetInstanceID().ToString()) +
            ";card=" + (entry.Card == null ? "null" : entry.Card.GetInstanceID().ToString()) +
            ";generation=" + entry.Generation + ";source=" + DescribeLayer(entry.Layer) +
            ";below=" + DescribeLayer(entry.Below); }

        private static void Unbind(Entry entry)
        {
            if (entry.Rows != null) foreach (var row in entry.Rows) ControllerOwnedItems.Remove(row, entry.Card);
            if (entry.Model != null)
            {
                entry.Model.IsSelected.Value = false;
                if (entry.MenuModel != null) ControllerOwnedItems.Remove((List<InGameMenuEntityVM>)Models.GetValue(entry.MenuModel), entry.Model);
                entry.Model.Dispose(); entry.Model = null;
            }
        }

        private static void Remove(Entry entry)
        {
            entry.Removed = true; // stale native VM callbacks must never reopen after partial cleanup
            Entries.Remove(entry.Menu);
            LayerCaptures.Remove(entry.Menu);
            try
            {
                // Operate on the CURRENT native collection; never feed an old
                // rows snapshot back over entries another mod added later.
                var navigation = entry.Menu == null ? null : Navigation.GetValue(entry.Menu) as ConsoleMultiNavigationCollection;
                if (navigation != null && entry.Card != null) navigation.RemoveEntity(entry.Card);
            }
            finally
            {
                try { Unbind(entry); }
                finally
                {
                    try
                    {
                        if (entry.Collection != null)
                        {
                            var cards = (List<ICartCollectionEntity>)Cards.GetValue(entry.Collection);
                            ControllerOwnedItems.Remove(cards, entry.Card);
                            entry.Collection.SetCartsPositions();
                        }
                    }
                    finally
                    {
                        if (entry.OwnedRoot != null) { entry.OwnedRoot.gameObject.SetActive(false); UnityEngine.Object.Destroy(entry.OwnedRoot.gameObject); }
                        if (entry.Kit != null) entry.Kit.Dispose();
                        if (entry.Icon != null) UnityEngine.Object.Destroy(entry.Icon);
                        if (entry.Texture != null) UnityEngine.Object.Destroy(entry.Texture);
                    }
                }
            }
        }

        internal static void Tick()
        {
            foreach (Entry entry in Entries.Values.Where(e => e.Menu == null).ToArray()) SafeRemove(entry);
        }

        private static void SafeRemove(Entry entry)
        {
            try { Remove(entry); }
            catch (Exception exception) { _log.Error("[KBP-CONTROLLER-CARD] owned cleanup failed;native callbacks not interrupted.", exception); }
        }
    }
}
