using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal static class NativeUiContractProbe
    {
        internal static bool IsReady
        {
            get
            {
                return Game.Instance != null && Game.Instance.UI != null &&
                    Game.Instance.UI.Canvas != null && StaticCanvas.Instance != null &&
                    EventSystem.current != null;
            }
        }

        internal static NativeUiContract Capture()
        {
            if (!IsReady) throw new InvalidOperationException("Native UI contract is not ready.");
            StaticCanvas canvas = StaticCanvas.Instance;
            var contract = new NativeUiContract
            {
                SchemaVersion = 1,
                CapturedAtUtc = DateTime.UtcNow.ToString("o"),
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                CurrentGameMode = Game.Instance.CurrentMode.ToString(),
                EventSystemPath = GetPath(EventSystem.current.transform),
                StaticCanvasPath = GetPath(canvas.transform),
                StaticCanvasActive = canvas.gameObject.activeInHierarchy,
                StaticCanvasBlocksRaycasts = canvas.CanvasGroup != null && canvas.CanvasGroup.blocksRaycasts,
                HudState = canvas.HUDController == null ? "missing" : canvas.HUDController.CurrentState.ToString(),
                ServiceWindowPath = canvas.ServiceWindow == null
                    ? string.Empty : GetPath(canvas.ServiceWindow.transform),
                ServiceWindowTabsPath = canvas.ServiceWindow == null ||
                    canvas.ServiceWindow.WindowTabs == null
                    ? string.Empty : GetPath(canvas.ServiceWindow.WindowTabs.transform),
                ServiceWindowTabsActive = canvas.ServiceWindow != null &&
                    canvas.ServiceWindow.WindowTabs != null &&
                    canvas.ServiceWindow.WindowTabs.gameObject.activeInHierarchy,
                ServiceWindowBlocksRaycasts = canvas.ServiceWindow != null &&
                    canvas.ServiceWindow.WindowTabs != null &&
                    canvas.ServiceWindow.WindowTabs.GetComponent<CanvasGroup>() != null &&
                    canvas.ServiceWindow.WindowTabs.GetComponent<CanvasGroup>().blocksRaycasts,
                Buttons = CaptureButtons(canvas),
                Canvases = CaptureCanvases(canvas),
                Raycasters = CaptureRaycasters(canvas),
                CandidateAnchors = CaptureCandidateAnchors(canvas),
                Visuals = CaptureVisuals(canvas),
                Fonts = CaptureFonts(canvas),
                Portraits = CapturePortraits(canvas)
            };
            return contract;
        }

        private static List<NativeUiVisualContract> CaptureVisuals(StaticCanvas canvas)
        {
            Transform service = canvas.ServiceWindow == null ? null : canvas.ServiceWindow.transform;
            if (service == null) return new List<NativeUiVisualContract>();
            return service.GetComponentsInChildren<RectTransform>(true)
                .Where(rect => rect != null && (rect.GetComponent<Image>() != null ||
                    rect.GetComponent<Text>() != null || rect.GetComponent<Button>() != null ||
                    rect.GetComponent<Toggle>() != null))
                .OrderBy(rect => GetPath(rect), StringComparer.Ordinal)
                .Select(CaptureVisual)
                .ToList();
        }

        private static List<NativeUiFontContract> CaptureFonts(StaticCanvas canvas)
        {
            return canvas.GetComponentsInChildren<Text>(true)
                .Where(text => text != null && text.font != null)
                .GroupBy(text => text.font.name, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    Text sample = group.OrderBy(text => GetPath(text.transform),
                        StringComparer.Ordinal).First();
                    return new NativeUiFontContract
                    {
                        Name = group.Key,
                        SamplePath = GetPath(sample.transform),
                        SampleSize = sample.fontSize,
                        SampleStyle = sample.fontStyle.ToString(),
                        SampleColor = Format(sample.color),
                        UsageCount = group.Count()
                    };
                }).ToList();
        }

        private static List<NativeUiVisualContract> CapturePortraits(StaticCanvas canvas)
        {
            return canvas.GetComponentsInChildren<RectTransform>(true)
                .Where(rect => rect != null &&
                    GetPath(rect).IndexOf("Party", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (rect.GetComponent<Image>() != null || rect.GetComponent<Button>() != null))
                .OrderBy(rect => GetPath(rect), StringComparer.Ordinal)
                .Select(CaptureVisual)
                .ToList();
        }

        private static NativeUiVisualContract CaptureVisual(RectTransform rect)
        {
            Image image = rect.GetComponent<Image>();
            Text text = rect.GetComponent<Text>();
            Button button = rect.GetComponent<Button>();
            Toggle toggle = rect.GetComponent<Toggle>();
            return new NativeUiVisualContract
            {
                Path = GetPath(rect),
                ActiveSelf = rect.gameObject.activeSelf,
                ActiveInHierarchy = rect.gameObject.activeInHierarchy,
                Size = Format(rect.rect.size),
                Components = rect.GetComponents<Component>()
                    .Where(component => component != null)
                    .Select(component => component.GetType().FullName)
                    .OrderBy(name => name, StringComparer.Ordinal).ToArray(),
                ImageSprite = image == null || image.sprite == null ? string.Empty : image.sprite.name,
                ImageType = image == null ? string.Empty : image.type.ToString(),
                ImageColor = image == null ? string.Empty : Format(image.color),
                ImageMaterial = image == null || image.material == null
                    ? string.Empty : image.material.name,
                TextFont = text == null || text.font == null ? string.Empty : text.font.name,
                TextSize = text == null ? 0 : text.fontSize,
                TextStyle = text == null ? string.Empty : text.fontStyle.ToString(),
                TextColor = text == null ? string.Empty : Format(text.color),
                TextAlignment = text == null ? string.Empty : text.alignment.ToString(),
                TextSample = text == null || string.IsNullOrWhiteSpace(text.text)
                    ? string.Empty : Truncate(text.text.Trim(), 80),
                ButtonTransition = button == null ? string.Empty : button.transition.ToString(),
                ButtonTargetGraphic = button == null || button.targetGraphic == null
                    ? string.Empty : GetPath(button.targetGraphic.transform),
                ToggleIsOn = toggle != null && toggle.isOn,
                ToggleGraphic = toggle == null || toggle.graphic == null
                    ? string.Empty : GetPath(toggle.graphic.transform)
            };
        }

        private static string Truncate(string value, int maximum)
        {
            string normalized = value.Replace('\r', ' ').Replace('\n', ' ');
            return normalized.Length <= maximum ? normalized : normalized.Substring(0, maximum);
        }

        private static List<NativeUiButtonContract> CaptureButtons(StaticCanvas canvas)
        {
            return canvas.GetComponentsInChildren<Button>(true)
                .Where(button => button != null)
                .OrderBy(button => GetPath(button.transform), StringComparer.Ordinal)
                .Select(button =>
                {
                    RectTransform rect = button.transform as RectTransform;
                    Image image = button.GetComponent<Image>();
                    string[] labels = button.GetComponentsInChildren<Text>(true)
                        .Where(text => text != null && !string.IsNullOrWhiteSpace(text.text))
                        .Select(text => text.text.Trim()).ToArray();
                    return new NativeUiButtonContract
                    {
                        Path = GetPath(button.transform),
                        ActiveSelf = button.gameObject.activeSelf,
                        ActiveInHierarchy = button.gameObject.activeInHierarchy,
                        Interactable = button.interactable,
                        PersistentListenerCount = button.onClick.GetPersistentEventCount(),
                        AnchorMin = rect == null ? string.Empty : Format(rect.anchorMin),
                        AnchorMax = rect == null ? string.Empty : Format(rect.anchorMax),
                        Pivot = rect == null ? string.Empty : Format(rect.pivot),
                        AnchoredPosition = rect == null ? string.Empty : Format(rect.anchoredPosition),
                        SizeDelta = rect == null ? string.Empty : Format(rect.sizeDelta),
                        ImageSprite = image == null || image.sprite == null ? string.Empty : image.sprite.name,
                        Labels = labels,
                        Components = button.GetComponents<Component>()
                            .Where(component => component != null)
                            .Select(component => component.GetType().FullName)
                            .OrderBy(name => name, StringComparer.Ordinal).ToArray()
                    };
                }).ToList();
        }

        private static List<NativeUiCanvasContract> CaptureCanvases(StaticCanvas canvas)
        {
            return canvas.GetComponentsInChildren<Canvas>(true)
                .Where(item => item != null)
                .OrderBy(item => GetPath(item.transform), StringComparer.Ordinal)
                .Select(item => new NativeUiCanvasContract
                {
                    Path = GetPath(item.transform),
                    ActiveInHierarchy = item.gameObject.activeInHierarchy,
                    RenderMode = item.renderMode.ToString(),
                    SortingOrder = item.sortingOrder,
                    OverrideSorting = item.overrideSorting,
                    PixelPerfect = item.pixelPerfect
                }).ToList();
        }

        private static List<NativeUiRaycasterContract> CaptureRaycasters(StaticCanvas canvas)
        {
            return canvas.GetComponentsInChildren<GraphicRaycaster>(true)
                .Where(item => item != null)
                .OrderBy(item => GetPath(item.transform), StringComparer.Ordinal)
                .Select(item => new NativeUiRaycasterContract
                {
                    Path = GetPath(item.transform),
                    ActiveAndEnabled = item.isActiveAndEnabled,
                    BlockingObjects = item.blockingObjects.ToString(),
                    IgnoreReversedGraphics = item.ignoreReversedGraphics
                }).ToList();
        }

        private static List<NativeUiAnchorContract> CaptureCandidateAnchors(StaticCanvas canvas)
        {
            string[] terms = { "hud", "menu", "action", "bottom", "service", "staticcanvas" };
            return canvas.GetComponentsInChildren<RectTransform>(true)
                .Where(rect => rect != null && terms.Any(term =>
                    rect.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(rect => GetPath(rect), StringComparer.Ordinal)
                .Select(rect => new NativeUiAnchorContract
                {
                    Path = GetPath(rect),
                    ActiveSelf = rect.gameObject.activeSelf,
                    ActiveInHierarchy = rect.gameObject.activeInHierarchy,
                    AnchorMin = Format(rect.anchorMin),
                    AnchorMax = Format(rect.anchorMax),
                    Pivot = Format(rect.pivot),
                    AnchoredPosition = Format(rect.anchoredPosition),
                    SizeDelta = Format(rect.sizeDelta),
                    Components = rect.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName)
                        .OrderBy(name => name, StringComparer.Ordinal).ToArray()
                }).ToList();
        }

        private static string GetPath(Transform transform)
        {
            var names = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static string Format(Vector2 value)
        {
            return value.x.ToString("0.###") + "," + value.y.ToString("0.###");
        }

        private static string Format(Color value)
        {
            return value.r.ToString("0.###") + "," + value.g.ToString("0.###") + "," +
                value.b.ToString("0.###") + "," + value.a.ToString("0.###");
        }
    }

    internal sealed class NativeUiContract
    {
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [JsonProperty("capturedAtUtc", Order = 2)] public string CapturedAtUtc { get; set; }
        [JsonProperty("screenWidth", Order = 3)] public int ScreenWidth { get; set; }
        [JsonProperty("screenHeight", Order = 4)] public int ScreenHeight { get; set; }
        [JsonProperty("currentGameMode", Order = 5)] public string CurrentGameMode { get; set; }
        [JsonProperty("eventSystemPath", Order = 6)] public string EventSystemPath { get; set; }
        [JsonProperty("staticCanvasPath", Order = 7)] public string StaticCanvasPath { get; set; }
        [JsonProperty("staticCanvasActive", Order = 8)] public bool StaticCanvasActive { get; set; }
        [JsonProperty("staticCanvasBlocksRaycasts", Order = 9)] public bool StaticCanvasBlocksRaycasts { get; set; }
        [JsonProperty("hudState", Order = 10)] public string HudState { get; set; }
        [JsonProperty("serviceWindowPath", Order = 11)] public string ServiceWindowPath { get; set; }
        [JsonProperty("serviceWindowTabsPath", Order = 12)] public string ServiceWindowTabsPath { get; set; }
        [JsonProperty("serviceWindowTabsActive", Order = 13)] public bool ServiceWindowTabsActive { get; set; }
        [JsonProperty("serviceWindowBlocksRaycasts", Order = 14)] public bool ServiceWindowBlocksRaycasts { get; set; }
        [JsonProperty("buttons", Order = 15)] public List<NativeUiButtonContract> Buttons { get; set; }
        [JsonProperty("canvases", Order = 16)] public List<NativeUiCanvasContract> Canvases { get; set; }
        [JsonProperty("raycasters", Order = 17)] public List<NativeUiRaycasterContract> Raycasters { get; set; }
        [JsonProperty("candidateAnchors", Order = 18)] public List<NativeUiAnchorContract> CandidateAnchors { get; set; }
        [JsonProperty("visuals", Order = 19)] public List<NativeUiVisualContract> Visuals { get; set; }
        [JsonProperty("fonts", Order = 20)] public List<NativeUiFontContract> Fonts { get; set; }
        [JsonProperty("portraits", Order = 21)] public List<NativeUiVisualContract> Portraits { get; set; }
    }

    internal sealed class NativeUiButtonContract
    {
        [JsonProperty("path", Order = 1)] public string Path { get; set; }
        [JsonProperty("activeSelf", Order = 2)] public bool ActiveSelf { get; set; }
        [JsonProperty("activeInHierarchy", Order = 3)] public bool ActiveInHierarchy { get; set; }
        [JsonProperty("interactable", Order = 4)] public bool Interactable { get; set; }
        [JsonProperty("persistentListenerCount", Order = 5)] public int PersistentListenerCount { get; set; }
        [JsonProperty("anchorMin", Order = 6)] public string AnchorMin { get; set; }
        [JsonProperty("anchorMax", Order = 7)] public string AnchorMax { get; set; }
        [JsonProperty("pivot", Order = 8)] public string Pivot { get; set; }
        [JsonProperty("anchoredPosition", Order = 9)] public string AnchoredPosition { get; set; }
        [JsonProperty("sizeDelta", Order = 10)] public string SizeDelta { get; set; }
        [JsonProperty("imageSprite", Order = 11)] public string ImageSprite { get; set; }
        [JsonProperty("labels", Order = 12)] public string[] Labels { get; set; }
        [JsonProperty("components", Order = 13)] public string[] Components { get; set; }
    }

    internal sealed class NativeUiCanvasContract
    {
        [JsonProperty("path", Order = 1)] public string Path { get; set; }
        [JsonProperty("activeInHierarchy", Order = 2)] public bool ActiveInHierarchy { get; set; }
        [JsonProperty("renderMode", Order = 3)] public string RenderMode { get; set; }
        [JsonProperty("sortingOrder", Order = 4)] public int SortingOrder { get; set; }
        [JsonProperty("overrideSorting", Order = 5)] public bool OverrideSorting { get; set; }
        [JsonProperty("pixelPerfect", Order = 6)] public bool PixelPerfect { get; set; }
    }

    internal sealed class NativeUiRaycasterContract
    {
        [JsonProperty("path", Order = 1)] public string Path { get; set; }
        [JsonProperty("activeAndEnabled", Order = 2)] public bool ActiveAndEnabled { get; set; }
        [JsonProperty("blockingObjects", Order = 3)] public string BlockingObjects { get; set; }
        [JsonProperty("ignoreReversedGraphics", Order = 4)] public bool IgnoreReversedGraphics { get; set; }
    }

    internal sealed class NativeUiAnchorContract
    {
        [JsonProperty("path", Order = 1)] public string Path { get; set; }
        [JsonProperty("activeSelf", Order = 2)] public bool ActiveSelf { get; set; }
        [JsonProperty("activeInHierarchy", Order = 3)] public bool ActiveInHierarchy { get; set; }
        [JsonProperty("anchorMin", Order = 4)] public string AnchorMin { get; set; }
        [JsonProperty("anchorMax", Order = 5)] public string AnchorMax { get; set; }
        [JsonProperty("pivot", Order = 6)] public string Pivot { get; set; }
        [JsonProperty("anchoredPosition", Order = 7)] public string AnchoredPosition { get; set; }
        [JsonProperty("sizeDelta", Order = 8)] public string SizeDelta { get; set; }
        [JsonProperty("components", Order = 9)] public string[] Components { get; set; }
    }

    internal sealed class NativeUiVisualContract
    {
        [JsonProperty("path", Order = 1)] public string Path { get; set; }
        [JsonProperty("activeSelf", Order = 2)] public bool ActiveSelf { get; set; }
        [JsonProperty("activeInHierarchy", Order = 3)] public bool ActiveInHierarchy { get; set; }
        [JsonProperty("size", Order = 4)] public string Size { get; set; }
        [JsonProperty("components", Order = 5)] public string[] Components { get; set; }
        [JsonProperty("imageSprite", Order = 6)] public string ImageSprite { get; set; }
        [JsonProperty("imageType", Order = 7)] public string ImageType { get; set; }
        [JsonProperty("imageColor", Order = 8)] public string ImageColor { get; set; }
        [JsonProperty("imageMaterial", Order = 9)] public string ImageMaterial { get; set; }
        [JsonProperty("textFont", Order = 10)] public string TextFont { get; set; }
        [JsonProperty("textSize", Order = 11)] public int TextSize { get; set; }
        [JsonProperty("textStyle", Order = 12)] public string TextStyle { get; set; }
        [JsonProperty("textColor", Order = 13)] public string TextColor { get; set; }
        [JsonProperty("textAlignment", Order = 14)] public string TextAlignment { get; set; }
        [JsonProperty("textSample", Order = 15)] public string TextSample { get; set; }
        [JsonProperty("buttonTransition", Order = 16)] public string ButtonTransition { get; set; }
        [JsonProperty("buttonTargetGraphic", Order = 17)] public string ButtonTargetGraphic { get; set; }
        [JsonProperty("toggleIsOn", Order = 18)] public bool ToggleIsOn { get; set; }
        [JsonProperty("toggleGraphic", Order = 19)] public string ToggleGraphic { get; set; }
    }

    internal sealed class NativeUiFontContract
    {
        [JsonProperty("name", Order = 1)] public string Name { get; set; }
        [JsonProperty("samplePath", Order = 2)] public string SamplePath { get; set; }
        [JsonProperty("sampleSize", Order = 3)] public int SampleSize { get; set; }
        [JsonProperty("sampleStyle", Order = 4)] public string SampleStyle { get; set; }
        [JsonProperty("sampleColor", Order = 5)] public string SampleColor { get; set; }
        [JsonProperty("usageCount", Order = 6)] public int UsageCount { get; set; }
    }
}
