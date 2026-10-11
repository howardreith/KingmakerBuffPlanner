using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.UI;
using Kingmaker.UI.Log;
using KingmakerBuffPlanner.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Native UI finish (runtime-test sessions only, read-only): the exact
    // geometry, artwork, typography and state sets of the native controls
    // the planner matches - the HUD menu panel, the spellbook footer, the
    // spell description window, the game log - recorded from the live
    // StaticCanvas so every borrowed metric is a measured fact. Nothing
    // native is changed; previews stay in the guarded evidence directory.
    internal static class NativeUiFacts
    {
        internal const int MaximumNodes = 700;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        // Every node under root (inactive included, bounded): rect geometry,
        // on-screen rectangle, image, TextMeshPro, selectable and layout facts.
        internal static JObject Subtree(Transform root, string label)
        {
            var nodes = new JArray();
            var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            int count = 0;
            bool truncated = false;
            if (root != null)
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (count++ >= MaximumNodes) { truncated = true; break; }
                    nodes.Add(Node(root, node, sprites));
                }
            }
            return new JObject
            {
                { "label", label },
                { "root", root == null ? null : HierarchyPath(root) },
                { "nodes", nodes },
                { "truncated", truncated },
                { "spriteNames", new JArray(sprites.Keys.OrderBy(value => value, StringComparer.Ordinal)
                    .Cast<object>().ToArray()) }
            };
        }

        // The distinct sprites drawn under root, by name (for previews).
        internal static IList<Sprite> Sprites(Transform root)
        {
            var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            if (root == null) return new List<Sprite>();
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image != null && image.sprite != null && !sprites.ContainsKey(image.sprite.name))
                    sprites[image.sprite.name] = image.sprite;
            return sprites.Values.ToList();
        }

        private static JObject Node(Transform root, Transform node, IDictionary<string, Sprite> sprites)
        {
            var result = new JObject
            {
                { "path", RelativePath(root, node) },
                { "activeSelf", node.gameObject.activeSelf },
                { "active", node.gameObject.activeInHierarchy },
                { "sibling", node.GetSiblingIndex() },
                { "layer", node.gameObject.layer }
            };
            var rect = node as RectTransform;
            if (rect != null)
            {
                result["anchorMin"] = Vec(rect.anchorMin);
                result["anchorMax"] = Vec(rect.anchorMax);
                result["pivot"] = Vec(rect.pivot);
                result["anchoredPosition"] = Vec(rect.anchoredPosition);
                result["sizeDelta"] = Vec(rect.sizeDelta);
                result["rectSize"] = Vec(rect.rect.size);
                result["scale"] = Vec(rect.localScale);
                float[] screen = ScreenRect(rect);
                if (screen != null) result["screen"] = new JArray(screen.Cast<object>().ToArray());
            }
            var components = new JArray();
            foreach (Component component in node.GetComponents<Component>())
                if (component != null) components.Add(component.GetType().FullName);
            result["components"] = components;
            Image image = node.GetComponent<Image>();
            if (image != null)
            {
                var facts = new JObject
                {
                    { "type", image.type.ToString() },
                    { "color", Col(image.color) },
                    { "material", image.material == null ? null : image.material.name },
                    { "raycastTarget", image.raycastTarget },
                    { "preserveAspect", image.preserveAspect },
                    { "fillCenter", image.fillCenter },
                    { "enabled", image.enabled }
                };
                if (image.sprite != null)
                {
                    Sprite sprite = image.sprite;
                    sprites[sprite.name] = sprite;
                    facts["sprite"] = sprite.name;
                    facts["spriteRect"] = Rec(sprite.rect);
                    facts["spriteBorder"] = Vec4(sprite.border);
                    facts["spritePpu"] = sprite.pixelsPerUnit;
                    facts["texture"] = sprite.texture == null ? null
                        : sprite.texture.name + "@" + sprite.texture.width + "x" + sprite.texture.height;
                }
                Canvas canvas = image.canvas;
                if (canvas != null) facts["canvasReferencePpu"] = canvas.referencePixelsPerUnit;
                result["image"] = facts;
            }
            TMP_Text text = node.GetComponent<TMP_Text>();
            if (text != null) result["tmp"] = Text(text);
            Text legacy = node.GetComponent<Text>();
            if (legacy != null)
                result["legacyText"] = new JObject
                {
                    { "font", legacy.font == null ? null : legacy.font.name },
                    { "size", legacy.fontSize },
                    { "style", legacy.fontStyle.ToString() },
                    { "color", Col(legacy.color) },
                    { "text", Sample(legacy.text) }
                };
            Selectable selectable = node.GetComponent<Selectable>();
            if (selectable != null)
            {
                ColorBlock colors = selectable.colors;
                SpriteState states = selectable.spriteState;
                result["selectable"] = new JObject
                {
                    { "type", selectable.GetType().FullName },
                    { "interactable", selectable.interactable },
                    { "transition", selectable.transition.ToString() },
                    { "target", selectable.targetGraphic == null ? null
                        : RelativePath(root, selectable.targetGraphic.transform) },
                    { "normal", Col(colors.normalColor) },
                    { "highlighted", Col(colors.highlightedColor) },
                    { "pressed", Col(colors.pressedColor) },
                    { "disabled", Col(colors.disabledColor) },
                    { "multiplier", colors.colorMultiplier },
                    { "fade", colors.fadeDuration },
                    { "spriteHighlighted", states.highlightedSprite == null ? null : states.highlightedSprite.name },
                    { "spritePressed", states.pressedSprite == null ? null : states.pressedSprite.name },
                    { "spriteDisabled", states.disabledSprite == null ? null : states.disabledSprite.name }
                };
            }
            LayoutGroup layout = node.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                var facts = new JObject
                {
                    { "type", layout.GetType().FullName },
                    { "padding", layout.padding.left + "," + layout.padding.right + "," +
                        layout.padding.top + "," + layout.padding.bottom },
                    { "childAlignment", layout.childAlignment.ToString() }
                };
                var grid = layout as GridLayoutGroup;
                if (grid != null)
                {
                    facts["cellSize"] = Vec(grid.cellSize);
                    facts["spacing"] = Vec(grid.spacing);
                    facts["startCorner"] = grid.startCorner.ToString();
                    facts["startAxis"] = grid.startAxis.ToString();
                    facts["constraint"] = grid.constraint.ToString();
                    facts["constraintCount"] = grid.constraintCount;
                }
                var line = layout as HorizontalOrVerticalLayoutGroup;
                if (line != null) facts["spacing"] = line.spacing;
                result["layout"] = facts;
            }
            LayoutElement element = node.GetComponent<LayoutElement>();
            if (element != null)
                result["layoutElement"] = new JObject
                {
                    { "ignore", element.ignoreLayout },
                    { "min", element.minWidth + "," + element.minHeight },
                    { "preferred", element.preferredWidth + "," + element.preferredHeight },
                    { "flexible", element.flexibleWidth + "," + element.flexibleHeight }
                };
            CanvasGroup group = node.GetComponent<CanvasGroup>();
            if (group != null)
                result["canvasGroup"] = group.alpha.ToString("0.###", Invariant) + ";interactable=" +
                    group.interactable + ";blocks=" + group.blocksRaycasts;
            return result;
        }

        internal static JObject Text(TMP_Text text)
        {
            return new JObject
            {
                { "font", text.font == null ? null : text.font.name },
                { "fontFace", text.font == null || text.font.fontInfo == null ? null : text.font.fontInfo.Name },
                { "fontPointSize", text.font == null || text.font.fontInfo == null ? 0f : text.font.fontInfo.PointSize },
                { "material", text.fontSharedMaterial == null ? null : text.fontSharedMaterial.name },
                { "shader", text.fontSharedMaterial == null || text.fontSharedMaterial.shader == null ? null
                    : text.fontSharedMaterial.shader.name },
                { "size", text.fontSize },
                { "autoSize", text.enableAutoSizing },
                { "sizeMin", text.fontSizeMin },
                { "sizeMax", text.fontSizeMax },
                { "style", text.fontStyle.ToString() },
                { "weight", text.fontWeight },
                { "color", Col(text.color) },
                { "alignment", text.alignment.ToString() },
                { "characterSpacing", text.characterSpacing },
                { "wordSpacing", text.wordSpacing },
                { "lineSpacing", text.lineSpacing },
                { "paragraphSpacing", text.paragraphSpacing },
                { "wrap", text.enableWordWrapping },
                { "overflow", text.overflowMode.ToString() },
                { "margin", Vec4(text.margin) },
                { "richText", text.richText },
                { "raycastTarget", text.raycastTarget },
                { "text", Sample(text.text) }
            };
        }

        // Every TextMeshPro font asset and font material the game has loaded.
        internal static JObject TextMeshInventory()
        {
            var fonts = new JArray();
            foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .Where(value => value != null).OrderBy(value => value.name, StringComparer.Ordinal).Take(200))
            {
                fonts.Add(new JObject
                {
                    { "name", font.name },
                    { "face", font.fontInfo == null ? null : font.fontInfo.Name },
                    { "pointSize", font.fontInfo == null ? 0f : font.fontInfo.PointSize },
                    { "lineHeight", font.fontInfo == null ? 0f : font.fontInfo.LineHeight },
                    { "material", font.material == null ? null : font.material.name },
                    { "smallCaps", font.HasCharacter('ᴀ') }
                });
            }
            var materials = new JArray();
            foreach (Material material in Resources.FindObjectsOfTypeAll<Material>()
                .Where(value => value != null && value.shader != null &&
                    value.shader.name.IndexOf("TextMeshPro", StringComparison.Ordinal) >= 0)
                .OrderBy(value => value.name, StringComparer.Ordinal).Take(300))
                materials.Add(material.name + "|" + material.shader.name);
            return new JObject { { "fonts", fonts }, { "materials", materials } };
        }

        // The native key bindings as the player has them now (remapped
        // bindings included): name, key, modifiers and game mode.
        internal static JArray KeyboardBindings()
        {
            var result = new JArray();
            object keyboard = Game.Instance == null ? null : Game.Instance.Keyboard;
            FieldInfo field = keyboard == null ? null : keyboard.GetType().GetField("m_Bindings",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var bindings = field == null ? null : field.GetValue(keyboard) as IEnumerable;
            if (bindings == null) return result;
            foreach (KeyboardAccess.Binding binding in bindings.OfType<KeyboardAccess.Binding>())
                result.Add(binding.Name + "|" + binding.Key + "|ctrl=" + binding.IsCtrlDown + "|alt=" +
                    binding.IsAltDown + "|shift=" + binding.IsShiftDown + "|mode=" + binding.GameMode);
            return result;
        }

        // The native game log's HUD panel (read-only evidence).
        internal static Transform GameLogPanel()
        {
            StaticCanvas canvas = StaticCanvas.Instance;
            return canvas == null ? null : canvas.transform.Find("HUDLayout/CombatLog");
        }

        // The native game log's current lines (the visible channel).
        internal static IList<string> GameLogMessages()
        {
            var lines = new List<string>();
            BattleLogManager manager = Game.Instance == null || Game.Instance.UI == null
                ? null : Game.Instance.UI.BattleLogManager;
            BattleLogView view = manager == null ? null : manager.LogView;
            // VirtualList.GetDataList is protected: read it by reflection
            // (evidence only; this reader never writes a line).
            MethodInfo read = view == null ? null : view.GetType().GetMethod("GetDataList",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            var data = read == null ? null : read.Invoke(view, null) as IEnumerable<LogItemData>;
            if (data == null) return lines;
            foreach (LogItemData item in data)
                if (item != null) lines.Add(item.Msg ?? string.Empty);
            return lines;
        }

        internal static string Write(string evidenceDirectory, string file, JToken facts)
        {
            AtomicFile.WriteUtf8(Path.Combine(evidenceDirectory, file),
                facts.ToString(Formatting.Indented) + Environment.NewLine);
            return file;
        }

        // GPU copies of the named sprites (bounded); returns the files written.
        internal static IList<string> Previews(IEnumerable<Sprite> sprites, string evidenceDirectory,
            int maximum, IList<string> notes)
        {
            var files = new List<string>();
            foreach (Sprite sprite in sprites.Where(value => value != null).Take(maximum))
            {
                try { files.Add(NativePaperDonorInventory.WritePreview(sprite, evidenceDirectory)); }
                catch (Exception exception)
                {
                    notes.Add("preview-failed:" + sprite.name + ":" + exception.GetType().Name);
                }
            }
            return files;
        }

        // (x, y, width, height) in screen pixels, origin bottom-left.
        internal static float[] ScreenRect(RectTransform rect)
        {
            if (rect == null) return null;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Canvas root = canvas == null ? null : canvas.rootCanvas;
            Camera camera = root == null || root.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : root.worldCamera;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return new[] { Round(low.x), Round(low.y), Round(high.x - low.x), Round(high.y - low.y) };
        }

        internal static string HierarchyPath(Transform transform)
        {
            var parts = new List<string>();
            for (Transform current = transform; current != null; current = current.parent)
                parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static string RelativePath(Transform root, Transform node)
        {
            if (node == root) return ".";
            var parts = new List<string>();
            for (Transform current = node; current != null && current != root; current = current.parent)
                parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static float Round(float value)
        {
            return (float)Math.Round(value, 1);
        }

        private static string Sample(string text)
        {
            if (text == null) return null;
            text = text.Replace("\r", " ").Replace("\n", " ");
            return text.Length <= 80 ? text : text.Substring(0, 77) + "...";
        }

        private static string Vec(Vector2 value)
        {
            return value.x.ToString("0.##", Invariant) + "," + value.y.ToString("0.##", Invariant);
        }

        private static string Vec(Vector3 value)
        {
            return value.x.ToString("0.##", Invariant) + "," + value.y.ToString("0.##", Invariant) + "," +
                value.z.ToString("0.##", Invariant);
        }

        private static string Vec4(Vector4 value)
        {
            return value.x.ToString("0.##", Invariant) + "," + value.y.ToString("0.##", Invariant) + "," +
                value.z.ToString("0.##", Invariant) + "," + value.w.ToString("0.##", Invariant);
        }

        private static string Rec(Rect value)
        {
            return value.x.ToString("0.#", Invariant) + "," + value.y.ToString("0.#", Invariant) + "," +
                value.width.ToString("0.#", Invariant) + "," + value.height.ToString("0.#", Invariant);
        }

        private static string Col(Color value)
        {
            return value.r.ToString("0.000", Invariant) + "," + value.g.ToString("0.000", Invariant) + "," +
                value.b.ToString("0.000", Invariant) + "," + value.a.ToString("0.000", Invariant);
        }
    }
}
