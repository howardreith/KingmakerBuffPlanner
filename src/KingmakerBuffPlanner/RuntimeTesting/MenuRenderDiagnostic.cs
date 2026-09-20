using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Kingmaker;
using KingmakerBuffPlanner.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Launch/menu diagnostic scenario runner. It answers two questions with
    // in-game evidence and never loads a save: (1) does the automated session
    // actually present a rendered main-menu frame (game-generated screenshot
    // captured after the frame finished, with luma statistics), and (2) for
    // the menu-input scenario, does one real physical click on the visible
    // Load Game button open the native save/load window.
    internal sealed class MenuRenderDiagnostic
    {
        private const string MainMenuBoardTypeName = "Kingmaker.UI.MainMenuUI.MainMenuBoard";
        private const string SaveLoadWindowTypeName = "Kingmaker.UI.SaveLoadWindow.SaveLoadWindow";
        private const string SaveSlotTypeName = "Kingmaker.UI.SaveLoadWindow.SaveSlot";
        // 21600 frames (~6 minutes at 60 fps) is the overall diagnostic budget;
        // the outer harness timeout governs the hard stop.
        private const int MaxUpdates = 21600;
        private const int MenuStabilityFrames = 180;
        private const int WindowSettleFrames = 120;
        private const int EngineCaptureWaitFrames = 600;

        private readonly RuntimeTestRequest _request;
        private readonly ModLog _log;
        private readonly bool _requireWindowProof;
        private readonly Action<string, string, Vector2> _writePhysicalInput;
        private int _updates;
        private int _state;
        private int _menuStableFrames;
        private int _windowSettleFrames;
        private int _engineWaitFrames;
        private bool _captureRequested;
        private bool _captureCompleted;
        private bool _windowCaptureRequested;
        private bool _windowCaptureCompleted;
        private MenuFrameLumaSummary _menuFrameLuma;
        private string _menuFrameCaptureError;
        private MenuFrameLumaSummary _windowFrameLuma;
        private string _windowFrameCaptureError;
        private int _firstUpdateFrameCount = -1;
        private float _firstUpdateRealtime;
        private int _menuReadyFrameCount = -1;
        private float _menuReadyRealtime;

        internal MenuRenderDiagnostic(
            RuntimeTestRequest request,
            ModLog log,
            bool requireWindowProof,
            Action<string, string, Vector2> writePhysicalInput)
        {
            _request = request ?? throw new ArgumentNullException("request");
            _log = log ?? throw new ArgumentNullException("log");
            _requireWindowProof = requireWindowProof;
            _writePhysicalInput = writePhysicalInput;
        }

        internal bool IsComplete { get; private set; }
        internal string Stage { get; private set; } = "waiting-for-main-menu";
        internal string MenuFrameScreenshotSha256 { get; private set; }
        internal string MenuFrameEngineScreenshotSha256 { get; private set; }
        internal int MenuFrameWidth { get; private set; }
        internal int MenuFrameHeight { get; private set; }
        internal MenuFrameLumaSummary MenuFrameLuma { get { return _menuFrameLuma; } }
        internal string MenuFrameCaptureError { get { return _menuFrameCaptureError; } }
        internal string MenuWindowScreenshotSha256 { get; private set; }
        internal string MenuWindowCaptureError { get; private set; }
        internal bool MenuWindowOpened { get; private set; }
        internal string MenuWindowDescriptor { get; private set; }
        internal string MenuButtonInventory { get; private set; }
        internal string MenuClickTarget { get; private set; }
        internal bool MenuClickAcknowledged { get; private set; }
        internal string FrameProgressSummary { get; private set; }

        internal void Update()
        {
            if (IsComplete) return;
            _updates++;
            if (_updates > MaxUpdates)
                throw new TimeoutException("Menu diagnostic timed out at " + Stage + ".");
            if (_firstUpdateFrameCount < 0)
            {
                _firstUpdateFrameCount = Time.frameCount;
                _firstUpdateRealtime = Time.realtimeSinceStartup;
            }
            if (_state == 0)
            {
                if ((_updates % 300) == 1) LogEnvironmentSample();
                if (FindActiveMainMenuBoard() == null) return;
                _menuStableFrames++;
                if (_menuStableFrames < MenuStabilityFrames) return;
                _menuReadyFrameCount = Time.frameCount;
                _menuReadyRealtime = Time.realtimeSinceStartup;
                float elapsed = Math.Max(0.01f, _menuReadyRealtime - _firstUpdateRealtime);
                int frameDelta = Math.Max(0, _menuReadyFrameCount - _firstUpdateFrameCount);
                FrameProgressSummary = "frames=" + frameDelta.ToString(CultureInfo.InvariantCulture) +
                    ";elapsedSeconds=" + elapsed.ToString("F2", CultureInfo.InvariantCulture) +
                    ";averageFps=" + (frameDelta / elapsed).ToString("F2", CultureInfo.InvariantCulture);
                _log.Info("[KBP-MENU-DIAG] main menu board active and stable;" + FrameProgressSummary +
                    ";" + EnvironmentSample() + ".");
                _state = 1;
                Stage = "capturing-menu-frame";
                return;
            }
            if (_state == 1)
            {
                if (!_captureRequested)
                {
                    _captureRequested = true;
                    MenuDiagnosticCaptureHost.CaptureMenuFrame(
                        Path.Combine(_request.EvidenceDirectory, "menu-frame.png"),
                        delegate(MenuFrameLumaSummary summary, Exception failure)
                        {
                            _menuFrameLuma = summary;
                            _menuFrameCaptureError = failure == null
                                ? null : failure.GetType().Name + ": " + failure.Message;
                            _captureCompleted = true;
                        });
                    CaptureScreenshotThroughEngine(Path.Combine(
                        _request.EvidenceDirectory, "menu-frame-engine.png"));
                    MenuFrameWidth = Screen.width;
                    MenuFrameHeight = Screen.height;
                    _log.Info("[KBP-MENU-DIAG] menu frame capture requested;resolution=" +
                        MenuFrameWidth + "x" + MenuFrameHeight + ".");
                }
                if (!_captureCompleted) return;
                if (_menuFrameCaptureError != null)
                    throw new InvalidOperationException("Menu frame capture failed: " + _menuFrameCaptureError);
                string menuPng = Path.Combine(_request.EvidenceDirectory, "menu-frame.png");
                MenuFrameScreenshotSha256 = Hashing.Sha256(menuPng);
                _state = 2;
                Stage = "waiting-for-engine-capture";
                return;
            }
            if (_state == 2)
            {
                // The engine capture is written asynchronously by Unity; wait
                // briefly, then record whatever the engine produced.
                _engineWaitFrames++;
                string enginePng = Path.Combine(_request.EvidenceDirectory, "menu-frame-engine.png");
                if (File.Exists(enginePng) && new FileInfo(enginePng).Length >= 1000)
                    MenuFrameEngineScreenshotSha256 = Hashing.Sha256(enginePng);
                else if (_engineWaitFrames < EngineCaptureWaitFrames) return;
                WriteMenuRenderMarker();
                _log.Info("[KBP-MENU-DIAG] menu frame captured;readPixelsSha256=" +
                    MenuFrameScreenshotSha256 + ";engineSha256=" +
                    (MenuFrameEngineScreenshotSha256 ?? "missing") + ";luma=" +
                    (_menuFrameLuma == null ? "missing" : _menuFrameLuma.Describe()) + ".");
                if (!_requireWindowProof)
                {
                    IsComplete = true;
                    Stage = "menu-render-observed";
                    return;
                }
                _state = 3;
                Stage = "resolving-load-button";
                return;
            }
            if (_state == 3)
            {
                List<MenuButtonCandidate> inventory = InventoryActiveButtons();
                MenuButtonInventory = DescribeInventory(inventory);
                List<MenuButtonCandidate> loadButtons = inventory.Where(candidate =>
                    candidate.Text.IndexOf("load", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                if (loadButtons.Count != 1)
                    throw new InvalidOperationException("Expected exactly one visible Load menu button, found " +
                        loadButtons.Count + ";inventory=" + MenuButtonInventory);
                MenuButtonCandidate target = loadButtons[0];
                Vector2 center = target.ScreenCenter;
                MenuClickTarget = target.Path + ";text=" + target.Text + ";center=" +
                    center.x.ToString("F1", CultureInfo.InvariantCulture) + "," +
                    center.y.ToString("F1", CultureInfo.InvariantCulture) + ";renderMode=" + target.RenderMode;
                _log.Info("[KBP-MENU-DIAG] requesting physical Load Game click;" + MenuClickTarget + ".");
                _writePhysicalInput("menu-loadgame", "click", center);
                _state = 4;
                Stage = "waiting-for-saveload-window";
                return;
            }
            if (_state == 4)
            {
                if ((_updates % 300) == 0)
                    _log.Info("[KBP-MENU-DIAG] waiting for save/load window;frames=" + _updates + ".");
                MenuClickAcknowledged = File.Exists(Path.Combine(
                    _request.EvidenceDirectory, "physical-input-menu-loadgame.ack.json"));
                Component window = FindActiveSaveLoadWindow();
                if (window == null) return;
                MenuWindowOpened = true;
                MenuWindowDescriptor = DescribeWindow(window);
                _log.Info("[KBP-MENU-DIAG] save/load window is active after physical click;" +
                    MenuWindowDescriptor + ";clickAcknowledged=" + MenuClickAcknowledged + ".");
                _state = 5;
                Stage = "capturing-saveload-window";
                return;
            }
            if (_state == 5)
            {
                _windowSettleFrames++;
                if (_windowSettleFrames < WindowSettleFrames) return;
                if (!_windowCaptureRequested)
                {
                    _windowCaptureRequested = true;
                    MenuDiagnosticCaptureHost.CaptureMenuFrame(
                        Path.Combine(_request.EvidenceDirectory, "menu-saveload-window.png"),
                        delegate(MenuFrameLumaSummary summary, Exception failure)
                        {
                            _windowFrameLuma = summary;
                            _windowFrameCaptureError = failure == null
                                ? null : failure.GetType().Name + ": " + failure.Message;
                            _windowCaptureCompleted = true;
                        });
                    _log.Info("[KBP-MENU-DIAG] save/load window capture requested.");
                }
                if (!_windowCaptureCompleted) return;
                if (_windowFrameCaptureError != null)
                    throw new InvalidOperationException("Save/load window capture failed: " + _windowFrameCaptureError);
                string windowPng = Path.Combine(_request.EvidenceDirectory, "menu-saveload-window.png");
                MenuWindowScreenshotSha256 = Hashing.Sha256(windowPng);
                WriteWindowOpenedMarker();
                _log.Info("[KBP-MENU-DIAG] save/load window captured;sha256=" +
                    MenuWindowScreenshotSha256 + ";luma=" +
                    (_windowFrameLuma == null ? "missing" : _windowFrameLuma.Describe()) + ".");
                IsComplete = true;
                Stage = "menu-window-opened";
            }
        }

        private static Component FindActiveMainMenuBoard()
        {
            Type boardType = typeof(Game).Assembly.GetType(MainMenuBoardTypeName, true);
            return UnityEngine.Object.FindObjectOfType(boardType) as Component;
        }

        private static Component FindActiveSaveLoadWindow()
        {
            Type windowType = typeof(Game).Assembly.GetType(SaveLoadWindowTypeName, true);
            return Resources.FindObjectsOfTypeAll(windowType).OfType<Component>()
                .FirstOrDefault(component => component != null && component.gameObject != null &&
                    component.gameObject.activeInHierarchy);
        }

        private static List<MenuButtonCandidate> InventoryActiveButtons()
        {
            var candidates = new List<MenuButtonCandidate>();
            Button[] buttons = UnityEngine.Object.FindObjectsOfType<Button>();
            foreach (Button button in buttons)
            {
                if (button == null || !button.gameObject.activeInHierarchy ||
                    !button.gameObject.scene.isLoaded) continue;
                Text[] texts = button.GetComponentsInChildren<Text>(true);
                var builder = new StringBuilder();
                foreach (Text text in texts)
                {
                    if (text == null || string.IsNullOrEmpty(text.text)) continue;
                    if (builder.Length > 0) builder.Append(' ');
                    builder.Append(text.text.Trim());
                }
                candidates.Add(new MenuButtonCandidate(button, builder.ToString()));
                if (candidates.Count >= 40) break;
            }
            return candidates;
        }

        private static string DescribeInventory(List<MenuButtonCandidate> inventory)
        {
            return string.Join("|", inventory
                .Select(candidate => candidate.Path + ";text=" + candidate.Text)
                .ToArray());
        }

        private static string DescribeWindow(Component window)
        {
            Type slotType = typeof(Game).Assembly.GetType(SaveSlotTypeName, false);
            int activeSlots = 0;
            if (slotType != null)
                activeSlots = Resources.FindObjectsOfTypeAll(slotType).OfType<Component>()
                    .Count(slot => slot != null && slot.gameObject != null &&
                        slot.gameObject.activeInHierarchy);
            return "type=" + window.GetType().FullName + ";activeSlots=" + activeSlots;
        }

        private static void CaptureScreenshotThroughEngine(string path)
        {
            Type screenCapture = Type.GetType(
                "UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule", false);
            MethodInfo capture = screenCapture == null ? null : screenCapture.GetMethod(
                "CaptureScreenshot", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(string) }, null);
            if (capture == null)
                throw new MissingMethodException("Unity screenshot capture API is unavailable.");
            capture.Invoke(null, new object[] { path });
        }

        private string EnvironmentSample()
        {
            return "scene=" + SceneManager.GetActiveScene().name +
                ";resolution=" + Screen.width + "x" + Screen.height +
                ";fullscreen=" + Screen.fullScreen +
                ";focused=" + Application.isFocused +
                ";runInBackground=" + Application.runInBackground +
                ";vSync=" + QualitySettings.vSyncCount;
        }

        private void LogEnvironmentSample()
        {
            _log.Info("[KBP-MENU-DIAG] waiting for main menu;frames=" + _updates + ";frameCount=" +
                Time.frameCount + ";" + EnvironmentSample() + ".");
        }

        private void WriteMenuRenderMarker()
        {
            string path = Path.Combine(_request.EvidenceDirectory, "menu-render.json");
            string json = "{\"schemaVersion\":1,\"runId\":" + JsonConvertToString(_request.RunId) +
                ",\"scenario\":" + JsonConvertToString(_request.Scenario) +
                ",\"stage\":\"menu-frame-captured\"" +
                ",\"frameProgress\":" + JsonConvertToString(FrameProgressSummary ?? string.Empty) +
                ",\"environment\":" + JsonConvertToString(EnvironmentSample()) +
                ",\"readPixelsSha256\":" + JsonConvertToString(MenuFrameScreenshotSha256 ?? string.Empty) +
                ",\"engineSha256\":" + JsonConvertToString(MenuFrameEngineScreenshotSha256 ?? string.Empty) +
                ",\"luma\":" + JsonConvertToString(
                    _menuFrameLuma == null ? string.Empty : _menuFrameLuma.Describe()) + "}";
            AtomicFile.WriteUtf8(path, json + Environment.NewLine);
        }

        private void WriteWindowOpenedMarker()
        {
            string path = Path.Combine(_request.EvidenceDirectory, "menu-window-opened.json");
            string json = "{\"schemaVersion\":1,\"runId\":" + JsonConvertToString(_request.RunId) +
                ",\"stage\":\"save-load-window-opened\"" +
                ",\"clickTarget\":" + JsonConvertToString(MenuClickTarget ?? string.Empty) +
                ",\"clickAcknowledged\":" + (MenuClickAcknowledged ? "true" : "false") +
                ",\"window\":" + JsonConvertToString(MenuWindowDescriptor ?? string.Empty) +
                ",\"screenshotSha256\":" + JsonConvertToString(MenuWindowScreenshotSha256 ?? string.Empty) + "}";
            AtomicFile.WriteUtf8(path, json + Environment.NewLine);
        }

        private static string JsonConvertToString(string value)
        {
            return Newtonsoft.Json.JsonConvert.ToString(value ?? string.Empty);
        }

        private sealed class MenuButtonCandidate
        {
            internal MenuButtonCandidate(Button button, string text)
            {
                Button = button;
                Text = text;
                RectTransform = button.GetComponent<RectTransform>();
                Canvas canvas = button.GetComponentInParent<Canvas>();
                RenderMode = canvas == null ? "no-canvas" : canvas.renderMode.ToString();
                Vector2? center = ComputeScreenCenter(RectTransform, canvas);
                ScreenCenter = center == null ? Vector2.zero : center.Value;
                Path = BuildPath(button.transform);
            }

            internal Button Button { get; private set; }
            internal string Text { get; private set; }
            internal RectTransform RectTransform { get; private set; }
            internal string RenderMode { get; private set; }
            internal Vector2 ScreenCenter { get; private set; }
            internal string Path { get; private set; }

            private static Vector2? ComputeScreenCenter(RectTransform rect, Canvas canvas)
            {
                if (rect == null) return null;
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    Camera camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                    if (camera == null) return null;
                    for (int i = 0; i < corners.Length; i++)
                        corners[i] = camera.WorldToScreenPoint(corners[i]);
                }
                return new Vector2(
                    (corners[0].x + corners[2].x) * 0.5f,
                    (corners[0].y + corners[2].y) * 0.5f);
            }

            private static string BuildPath(Transform transform)
            {
                var names = new List<string>();
                for (Transform node = transform; node != null; node = node.parent)
                    names.Add(node.name);
                names.Reverse();
                return string.Join("/", names.ToArray());
            }
        }
    }

    // Dedicated DontDestroyOnLoad host so the diagnostic's readback runs at
    // WaitForEndOfFrame: capturing after the frame finished presenting, which
    // is the timing Unity documents as required to include UI.
    internal sealed class MenuDiagnosticCaptureHost : MonoBehaviour
    {
        private static MenuDiagnosticCaptureHost _instance;

        internal static void CaptureMenuFrame(string path,
            Action<MenuFrameLumaSummary, Exception> completion)
        {
            if (_instance == null)
            {
                GameObject host = new GameObject("KBP-MenuDiagnosticCaptureHost");
                _instance = host.AddComponent<MenuDiagnosticCaptureHost>();
                DontDestroyOnLoad(host);
            }
            _instance.StartCoroutine(CaptureRoutine(path, completion));
        }

        private static IEnumerator CaptureRoutine(string path,
            Action<MenuFrameLumaSummary, Exception> completion)
        {
            yield return new WaitForEndOfFrame();
            MenuFrameLumaSummary summary = null;
            Exception failure = null;
            try
            {
                int width = Screen.width;
                int height = Screen.height;
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                texture.Apply(false, false);
                Color[] pixels = texture.GetPixels();
                int stride = Math.Max(1, pixels.Length / 120000);
                var luma = new List<float>();
                for (int i = 0; i < pixels.Length; i += stride)
                {
                    Color color = pixels[i];
                    luma.Add(color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f);
                }
                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.Destroy(texture);
                File.WriteAllBytes(path, png);
                summary = MenuFrameStats.Summarize(luma.ToArray());
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            completion(summary, failure);
        }
    }
}
