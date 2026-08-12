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
                CandidateAnchors = CaptureCandidateAnchors(canvas)
            };
            return contract;
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
}
