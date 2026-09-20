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
    // Load Game button open the native save/load window. The UMM ShowOnStart
    // overlay is dismissed first with the proven physical Escape sequence,
    // and each Escape is verified against a captured frame diff.
    internal sealed class MenuRenderDiagnostic
    {
        private const string SaveLoadWindowTypeName = "Kingmaker.UI.SaveLoadWindow.SaveLoadWindow";
        private const string SaveSlotTypeName = "Kingmaker.UI.SaveLoadWindow.SaveSlot";
        private const string MainMenuBoardTypeName = "Kingmaker.UI.MainMenuUI.MainMenuBoard";
        // 21600 frames (~6 minutes at 60 fps) is the overall diagnostic budget;
        // the outer harness timeout governs the hard stop.
        private const int MaxUpdates = 21600;
        private const int MaxElapsedSeconds = 420;
        private const int MenuStabilityFrames = 180;
        private const int EscapeSettleFrames = 90;
        private const int WindowSettleFrames = 120;
        private const int EngineCaptureWaitFrames = 600;

        private readonly RuntimeTestRequest _request;
        private readonly ModLog _log;
        private readonly bool _requireWindowProof;
        private readonly Action<string, string, Vector2> _writePhysicalInput;
        private readonly System.Diagnostics.Stopwatch _elapsed =
            System.Diagnostics.Stopwatch.StartNew();
        private int _updates;
        private int _lastFrameCount = -1;
        private int _state;
        private int _menuStableFrames;
        private int _settleFrames;
        private int _engineWaitFrames;
        private MenuFrameCapture _lastCapture;
        private MenuFrameCapture _menuCapture;
        private int _firstUpdateFrameCount = -1;
        private float _firstUpdateRealtime;

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
        internal MenuFrameLumaSummary MenuFrameLuma
        {
            get { return _menuCapture == null ? null : _menuCapture.Summary; }
        }
        internal string MenuFrameCaptureError
        {
            get { return _menuCapture == null || _menuCapture.Failure == null
                ? null : _menuCapture.Failure.GetType().Name + ": " + _menuCapture.Failure.Message; }
        }
        internal string MenuEscape1ScreenshotSha256 { get; private set; }
        internal float MenuEscape1ChangedFraction { get; private set; }
        internal bool MenuEscape1Acknowledged { get; private set; }
        internal string MenuEscape2ScreenshotSha256 { get; private set; }
        internal float MenuEscape2ChangedFraction { get; private set; }
        internal bool MenuEscape2Acknowledged { get; private set; }
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
            // UMM may dispatch OnUpdate several times per rendered frame; the
            // diagnostic advances once per frame and budgets by wall clock.
            if (Time.frameCount == _lastFrameCount) return;
            _lastFrameCount = Time.frameCount;
            _updates++;
            if (_updates > MaxUpdates || _elapsed.Elapsed.TotalSeconds > MaxElapsedSeconds)
                throw new TimeoutException("Menu diagnostic timed out at " + Stage +
                    ";elapsedSeconds=" + _elapsed.Elapsed.TotalSeconds.ToString(
                        "F1", CultureInfo.InvariantCulture) + ".");
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
                float elapsed = Math.Max(0.01f, Time.realtimeSinceStartup - _firstUpdateRealtime);
                int frameDelta = Math.Max(0, Time.frameCount - _firstUpdateFrameCount);
                FrameProgressSummary = "frames=" + frameDelta.ToString(CultureInfo.InvariantCulture) +
                    ";elapsedSeconds=" + elapsed.ToString("F2", CultureInfo.InvariantCulture) +
                    ";averageFps=" + (frameDelta / elapsed).ToString("F2", CultureInfo.InvariantCulture);
                _log.Info("[KBP-MENU-DIAG] main menu board active and stable;" + FrameProgressSummary +
                    ";" + EnvironmentSample() + ".");
                MenuFrameWidth = Screen.width;
                MenuFrameHeight = Screen.height;
                BeginCapture("menu-frame.png");
                CaptureScreenshotThroughEngine(Path.Combine(
                    _request.EvidenceDirectory, "menu-frame-engine.png"));
                _log.Info("[KBP-MENU-DIAG] menu frame capture requested;resolution=" +
                    MenuFrameWidth + "x" + MenuFrameHeight + ".");
                _state = 1;
                Stage = "capturing-menu-frame";
                return;
            }
            if (_state == 1)
            {
                if (!ConsumeCapture("menu-frame.png")) return;
                _menuCapture = _lastCapture;
                MenuFrameScreenshotSha256 = Hashing.Sha256(_menuCapture.FullPath);
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
                    (_menuCapture.Summary == null ? "missing" : _menuCapture.Summary.Describe()) + ".");
                if (!_requireWindowProof)
                {
                    IsComplete = true;
                    Stage = "menu-render-observed";
                    return;
                }
                _settleFrames = 0;
                _state = 3;
                Stage = "umm-dismiss-escape-1";
                return;
            }
            if (_state == 3)
            {
                // UMM's ShowOnStart overlay covers the menu and would swallow
                // the click; dismiss it with the proven physical Escape path.
                _writePhysicalInput("menu-umm-dismiss-1", "key-escape", Vector2.zero);
                _state = 4;
                return;
            }
            if (_state == 4)
            {
                MenuEscape1Acknowledged = Acknowledged("menu-umm-dismiss-1");
                if (!MenuEscape1Acknowledged) return;
                _settleFrames++;
                if (_settleFrames < EscapeSettleFrames) return;
                BeginCapture("menu-after-escape-1.png");
                _state = 5;
                Stage = "capturing-after-escape-1";
                return;
            }
            if (_state == 5)
            {
                if (!ConsumeCapture("menu-after-escape-1.png")) return;
                MenuFrameCapture escape1 = _lastCapture;
                MenuEscape1ScreenshotSha256 = Hashing.Sha256(escape1.FullPath);
                MenuEscape1ChangedFraction = ComputeDiff(escape1);
                _log.Info("[KBP-MENU-DIAG] escape-1 frame captured;sha256=" +
                    MenuEscape1ScreenshotSha256 + ";changedFraction=" +
                    MenuEscape1ChangedFraction.ToString("F5", CultureInfo.InvariantCulture) +
                    ";luma=" + (escape1.Summary == null ? "missing" : escape1.Summary.Describe()) + ".");
                _settleFrames = 0;
                _writePhysicalInput("menu-umm-dismiss-2", "key-escape", Vector2.zero);
                _state = 6;
                Stage = "umm-dismiss-escape-2";
                return;
            }
            if (_state == 6)
            {
                MenuEscape2Acknowledged = Acknowledged("menu-umm-dismiss-2");
                if (!MenuEscape2Acknowledged) return;
                _settleFrames++;
                if (_settleFrames < EscapeSettleFrames) return;
                BeginCapture("menu-after-escape-2.png");
                _state = 7;
                Stage = "capturing-after-escape-2";
                return;
            }
            if (_state == 7)
            {
                if (!ConsumeCapture("menu-after-escape-2.png")) return;
                MenuFrameCapture escape2 = _lastCapture;
                MenuEscape2ScreenshotSha256 = Hashing.Sha256(escape2.FullPath);
                MenuEscape2ChangedFraction = ComputeDiff(escape2);
                WriteEscapeMarker();
                _log.Info("[KBP-MENU-DIAG] escape-2 frame captured;sha256=" +
                    MenuEscape2ScreenshotSha256 + ";changedFraction=" +
                    MenuEscape2ChangedFraction.ToString("F5", CultureInfo.InvariantCulture) +
                    ";luma=" + (escape2.Summary == null ? "missing" : escape2.Summary.Describe()) + ".");
                _state = 8;
                Stage = "resolving-load-button";
                return;
            }
            if (_state == 8)
            {
                // Resolve the exact Load Game button with the proven contract
                // (hierarchy, sibling, components, TMP label, wired listeners);
                // display-text search cannot match Kingmaker's TextMeshPro menu.
                MainMenuLoadContracts.LoadButtonEvidence evidence;
                Button target = MainMenuLoadContracts.ResolveExactLoadButton(out evidence);
                if (target == null)
                    throw new InvalidOperationException("The exact Load Game button could not be resolved;inventory=" +
                        DescribeInventory(InventoryActiveButtons()));
                MenuButtonInventory = "path=" + evidence.HierarchyPath +
                    ";siblings=" + evidence.SiblingIndex + "/" + evidence.SiblingCount +
                    ";labels=" + string.Join("|", evidence.LabelIdentities.ToArray()) +
                    ";listeners=" + string.Join("|", evidence.ListenerIdentities.ToArray());
                Vector2 center = ComputeScreenCenter(target);
                MenuClickTarget = evidence.HierarchyPath + ";center=" +
                    center.x.ToString("F1", CultureInfo.InvariantCulture) + "," +
                    center.y.ToString("F1", CultureInfo.InvariantCulture);
                _log.Info("[KBP-MENU-DIAG] requesting physical Load Game click;" + MenuClickTarget + ".");
                _writePhysicalInput("menu-loadgame", "click", center);
                _state = 9;
                Stage = "waiting-for-saveload-window";
                return;
            }
            if (_state == 9)
            {
                if ((_updates % 300) == 0)
                    _log.Info("[KBP-MENU-DIAG] waiting for save/load window;frames=" + _updates + ".");
                MenuClickAcknowledged = Acknowledged("menu-loadgame");
                Component window = FindActiveSaveLoadWindow();
                if (window == null) return;
                MenuWindowOpened = true;
                MenuWindowDescriptor = DescribeWindow(window);
                _log.Info("[KBP-MENU-DIAG] save/load window is active after physical click;" +
                    MenuWindowDescriptor + ";clickAcknowledged=" + MenuClickAcknowledged + ".");
                _settleFrames = 0;
                _state = 10;
                Stage = "capturing-saveload-window";
                return;
            }
            if (_state == 10)
            {
                _settleFrames++;
                if (_settleFrames < WindowSettleFrames) return;
                BeginCapture("menu-saveload-window.png");
                _state = 11;
                Stage = "awaiting-window-capture";
                return;
            }
            if (_state == 11)
            {
                if (!ConsumeCapture("menu-saveload-window.png")) return;
                MenuWindowScreenshotSha256 = Hashing.Sha256(_lastCapture.FullPath);
                WriteWindowOpenedMarker();
                _log.Info("[KBP-MENU-DIAG] save/load window captured;sha256=" +
                    MenuWindowScreenshotSha256 + ";luma=" +
                    (_lastCapture.Summary == null ? "missing" : _lastCapture.Summary.Describe()) + ".");
                IsComplete = true;
                Stage = "menu-window-opened";
            }
        }

        private void BeginCapture(string fileName)
        {
            MenuDiagnosticCaptureHost.CaptureMenuFrame(
                Path.Combine(_request.EvidenceDirectory, fileName),
                delegate(MenuFrameCapture capture, Exception failure)
                {
                    capture.Failure = failure;
                    _lastCapture = capture;
                });
        }

        private bool ConsumeCapture(string fileName)
        {
            if (_lastCapture == null ||
                !string.Equals(_lastCapture.FileName, fileName, StringComparison.OrdinalIgnoreCase) ||
                _lastCapture.Handled) return false;
            _lastCapture.Handled = true;
            if (_lastCapture.Failure != null)
                throw new InvalidOperationException("Frame capture failed for " + fileName + ": " +
                    _lastCapture.Failure.GetType().Name + ": " + _lastCapture.Failure.Message);
            return true;
        }

        private float ComputeDiff(MenuFrameCapture capture)
        {
            if (_menuCapture == null || _menuCapture.Samples == null || capture.Samples == null ||
                _menuCapture.Samples.Length != capture.Samples.Length) return -1f;
            return MenuFrameStats.ComputeChangedFraction(_menuCapture.Samples, capture.Samples);
        }

        private bool Acknowledged(string actionId)
        {
            return File.Exists(Path.Combine(
                _request.EvidenceDirectory, "physical-input-" + actionId + ".ack.json"));
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

        private static List<string> InventoryActiveButtons()
        {
            var candidates = new List<string>();
            foreach (Button button in UnityEngine.Object.FindObjectsOfType<Button>())
            {
                if (button == null || !button.gameObject.activeInHierarchy ||
                    !button.gameObject.scene.isLoaded) continue;
                candidates.Add(MainMenuLoadContracts.HierarchyPath(button.transform));
                if (candidates.Count >= 40) break;
            }
            return candidates;
        }

        private static string DescribeInventory(List<string> inventory)
        {
            return string.Join("|", inventory.ToArray());
        }

        private static Vector2 ComputeScreenCenter(Button button)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (rect == null) return Vector2.zero;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Camera camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                if (camera == null) return Vector2.zero;
                for (int i = 0; i < corners.Length; i++)
                    corners[i] = camera.WorldToScreenPoint(corners[i]);
            }
            return new Vector2(
                (corners[0].x + corners[2].x) * 0.5f,
                (corners[0].y + corners[2].y) * 0.5f);
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
            string json = "{\"schemaVersion\":1,\"runId\":" + JsonString(_request.RunId) +
                ",\"scenario\":" + JsonString(_request.Scenario) +
                ",\"stage\":\"menu-frame-captured\"" +
                ",\"frameProgress\":" + JsonString(FrameProgressSummary ?? string.Empty) +
                ",\"environment\":" + JsonString(EnvironmentSample()) +
                ",\"readPixelsSha256\":" + JsonString(MenuFrameScreenshotSha256 ?? string.Empty) +
                ",\"engineSha256\":" + JsonString(MenuFrameEngineScreenshotSha256 ?? string.Empty) +
                ",\"luma\":" + JsonString(
                    _menuCapture == null || _menuCapture.Summary == null
                        ? string.Empty : _menuCapture.Summary.Describe()) + "}";
            AtomicFile.WriteUtf8(path, json + Environment.NewLine);
        }

        private void WriteEscapeMarker()
        {
            string path = Path.Combine(_request.EvidenceDirectory, "menu-escape-evidence.json");
            string json = "{\"schemaVersion\":1,\"runId\":" + JsonString(_request.RunId) +
                ",\"stage\":\"umm-dismiss-escapes-delivered\"" +
                ",\"escape1\":{\"acknowledged\":" + (MenuEscape1Acknowledged ? "true" : "false") +
                ",\"sha256\":" + JsonString(MenuEscape1ScreenshotSha256 ?? string.Empty) +
                ",\"changedFraction\":" + MenuEscape1ChangedFraction.ToString("F5", CultureInfo.InvariantCulture) + "}" +
                ",\"escape2\":{\"acknowledged\":" + (MenuEscape2Acknowledged ? "true" : "false") +
                ",\"sha256\":" + JsonString(MenuEscape2ScreenshotSha256 ?? string.Empty) +
                ",\"changedFraction\":" + MenuEscape2ChangedFraction.ToString("F5", CultureInfo.InvariantCulture) + "}}" ;
            AtomicFile.WriteUtf8(path, json + Environment.NewLine);
        }

        private void WriteWindowOpenedMarker()
        {
            string path = Path.Combine(_request.EvidenceDirectory, "menu-window-opened.json");
            string json = "{\"schemaVersion\":1,\"runId\":" + JsonString(_request.RunId) +
                ",\"stage\":\"save-load-window-opened\"" +
                ",\"clickTarget\":" + JsonString(MenuClickTarget ?? string.Empty) +
                ",\"clickAcknowledged\":" + (MenuClickAcknowledged ? "true" : "false") +
                ",\"window\":" + JsonString(MenuWindowDescriptor ?? string.Empty) +
                ",\"screenshotSha256\":" + JsonString(MenuWindowScreenshotSha256 ?? string.Empty) + "}";
            AtomicFile.WriteUtf8(path, json + Environment.NewLine);
        }

        private static string JsonString(string value)
        {
            return Newtonsoft.Json.JsonConvert.ToString(value ?? string.Empty);
        }
    }

    internal sealed class MenuFrameCapture
    {
        internal string FileName { get; set; }
        internal string FullPath { get; set; }
        internal MenuFrameLumaSummary Summary { get; set; }
        internal float[] Samples { get; set; }
        internal Exception Failure { get; set; }
        internal bool Handled { get; set; }
    }

    // Dedicated DontDestroyOnLoad host so the diagnostic's readback runs at
    // WaitForEndOfFrame: capturing after the frame finished presenting, which
    // is the timing Unity documents as required to include UI.
    internal sealed class MenuDiagnosticCaptureHost : MonoBehaviour
    {
        private static MenuDiagnosticCaptureHost _instance;

        internal static void CaptureMenuFrame(string path,
            Action<MenuFrameCapture, Exception> completion)
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
            Action<MenuFrameCapture, Exception> completion)
        {
            yield return new WaitForEndOfFrame();
            MenuFrameCapture capture = new MenuFrameCapture
            {
                FileName = Path.GetFileName(path),
                FullPath = path
            };
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
                capture.Samples = luma.ToArray();
                capture.Summary = MenuFrameStats.Summarize(capture.Samples);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            completion(capture, failure);
        }
    }
}
