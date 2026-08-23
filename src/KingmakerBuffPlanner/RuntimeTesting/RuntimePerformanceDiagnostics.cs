using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kingmaker;
using Kingmaker.UI;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal enum RuntimePerformanceOperation
    {
        MainUpdate,
        UiRootTick,
        ScreenTick,
        HudInstall,
        HudObjectFind,
        HudTick,
        PointerTickPrefix,
        CameraScrollPostfix,
        NativeHotkeyPrefix
    }

    internal static class RuntimePerformanceDiagnostics
    {
        private const long MinimumQualifiedSampleMilliseconds = 750;
        private static readonly List<RuntimePerformanceSample> Samples =
            new List<RuntimePerformanceSample>();
        private static readonly MutableOperationTiming MainUpdate = new MutableOperationTiming();
        private static readonly MutableOperationTiming UiRootTick = new MutableOperationTiming();
        private static readonly MutableOperationTiming ScreenTick = new MutableOperationTiming();
        private static readonly MutableOperationTiming HudInstall = new MutableOperationTiming();
        private static readonly MutableOperationTiming HudObjectFind = new MutableOperationTiming();
        private static readonly MutableOperationTiming HudTick = new MutableOperationTiming();
        private static readonly MutableOperationTiming PointerTickPrefix = new MutableOperationTiming();
        private static readonly MutableOperationTiming CameraScrollPostfix = new MutableOperationTiming();
        private static readonly MutableOperationTiming NativeHotkeyPrefix = new MutableOperationTiming();
        private static bool _enabled;
        private static bool _disableHudDiscovery;
        private static int _durationSeconds;
        private static double _minimumFramesPerSecond;
        private static string _runId;
        private static ModLog _log;
        private static DateTime _startedAtUtc;
        private static long _startedAtTimestamp;
        private static long _sampleStartedAtTimestamp;
        private static int _sampleFrameCount;
        private static int _totalFrameCount;
        private static int _sampleHudFindFoundCount;
        private static bool _plannerOpen;
        private static bool _hudInstalled;
        private static bool _staticCanvasAvailable;
        private static string _gameMode;
        private static Camera _previousCamera;
        private static Vector3 _previousCameraPosition;
        private static Quaternion _previousCameraRotation;
        private static bool _previousCameraObserved;
        private static RuntimePerformanceProfile _completedProfile;

        internal static bool Enabled { get { return _enabled; } }
        internal static bool SuppressHudDiscovery { get { return _enabled && _disableHudDiscovery; } }
        internal static bool IsDurationComplete
        {
            get
            {
                return _enabled && Stopwatch.GetTimestamp() - _startedAtTimestamp >=
                    (long)_durationSeconds * Stopwatch.Frequency;
            }
        }

        internal static void Configure(RuntimeTestRequest request, ModLog log)
        {
            Reset();
            if (request == null || !RuntimeTestProtocol.IsPerformanceScenario(request.Scenario)) return;
            _durationSeconds = Convert.ToInt32(request.Parameters["durationSeconds"]);
            _minimumFramesPerSecond = Convert.ToDouble(
                request.Parameters["minimumFramesPerSecond"], System.Globalization.CultureInfo.InvariantCulture);
            _disableHudDiscovery = Convert.ToBoolean(request.Parameters["disableHudDiscovery"]);
            _runId = request.RunId;
            _log = log;
            _startedAtUtc = DateTime.UtcNow;
            _startedAtTimestamp = Stopwatch.GetTimestamp();
            _sampleStartedAtTimestamp = _startedAtTimestamp;
            _gameMode = "game-null";
            _enabled = true;
            _log.Info("[KBP-PERF] probe enabled;run=" + _runId + ";durationSeconds=" +
                _durationSeconds + ";minimumFps=" + _minimumFramesPerSecond.ToString("F2") +
                ";disableHudDiscovery=" + _disableHudDiscovery + ".");
        }

        internal static long BeginOperation()
        {
            return _enabled ? Stopwatch.GetTimestamp() : 0L;
        }

        internal static void RecordOperation(RuntimePerformanceOperation operation, long startedAt)
        {
            if (!_enabled || startedAt == 0L) return;
            long elapsed = Stopwatch.GetTimestamp() - startedAt;
            Timing(operation).Record(elapsed);
        }

        internal static void RecordHudObjectFind(long startedAt, bool found)
        {
            if (!_enabled || startedAt == 0L) return;
            HudObjectFind.Record(Stopwatch.GetTimestamp() - startedAt);
            if (found) _sampleHudFindFoundCount++;
        }

        internal static void FrameStarted()
        {
            if (!_enabled) return;
            long now = Stopwatch.GetTimestamp();
            if (_sampleFrameCount > 0 && now - _sampleStartedAtTimestamp >= Stopwatch.Frequency)
                CloseSample(now);
            _sampleFrameCount++;
            _totalFrameCount++;
        }

        internal static void FrameCompleted()
        {
            if (!_enabled) return;
            _plannerOpen = BuffPlannerUiRoot.IsScreenOpen;
            _hudInstalled = BuffPlannerUiRoot.IsHudInstalled;
            _staticCanvasAvailable = StaticCanvas.Instance != null;
            _gameMode = Game.Instance == null ? "game-null" : Game.Instance.CurrentMode.ToString();
        }

        internal static RuntimePerformanceProfile CompleteProfile()
        {
            if (_completedProfile != null) return _completedProfile;
            if (!_enabled) throw new InvalidOperationException("Performance diagnostics are not enabled.");
            long endedAt = Stopwatch.GetTimestamp();
            if (_sampleFrameCount > 0) CloseSample(endedAt);
            List<RuntimePerformanceSample> qualified = Samples.Where(sample =>
                sample.ElapsedMilliseconds >= MinimumQualifiedSampleMilliseconds).ToList();
            double minimum = qualified.Count == 0 ? 0 : qualified.Min(sample => sample.FramesPerSecond);
            double maximum = qualified.Count == 0 ? 0 : qualified.Max(sample => sample.FramesPerSecond);
            long qualifiedFrames = qualified.Sum(sample => (long)sample.FrameCount);
            long qualifiedTicks = qualified.Sum(sample => sample.ElapsedStopwatchTicks);
            double average = qualifiedTicks == 0 ? 0 : qualifiedFrames *
                (double)Stopwatch.Frequency / qualifiedTicks;
            _completedProfile = new RuntimePerformanceProfile
            {
                SchemaVersion = 1,
                RunId = _runId,
                Version = BuildInfo.Version,
                Commit = BuildInfo.Commit,
                DisableHudDiscovery = _disableHudDiscovery,
                RequestedDurationSeconds = _durationSeconds,
                RequestedMinimumFramesPerSecond = _minimumFramesPerSecond,
                StartedAtUtc = _startedAtUtc.ToString("o"),
                EndedAtUtc = DateTime.UtcNow.ToString("o"),
                ElapsedMilliseconds = TicksToMilliseconds(endedAt - _startedAtTimestamp),
                TotalFrameCount = _totalFrameCount,
                QualifiedSampleCount = qualified.Count,
                MinimumFramesPerSecond = minimum,
                AverageFramesPerSecond = average,
                MaximumFramesPerSecond = maximum,
                MeetsRequestedMinimum = _minimumFramesPerSecond <= 0 ||
                    (qualified.Count > 0 && minimum >= _minimumFramesPerSecond),
                HudObjectFindInvocationCount = Samples.Sum(sample =>
                    (long)sample.HudObjectFind.InvocationCount),
                HudObjectFindTotalMilliseconds = Samples.Sum(sample =>
                    sample.HudObjectFind.TotalMilliseconds),
                HudObjectFindMaximumMilliseconds = Samples.Count == 0 ? 0 : Samples.Max(sample =>
                    sample.HudObjectFind.MaximumMilliseconds),
                Samples = Samples.ToList()
            };
            _enabled = false;
            return _completedProfile;
        }

        private static void CloseSample(long endedAt)
        {
            long elapsed = Math.Max(1L, endedAt - _sampleStartedAtTimestamp);
            Camera camera = Camera.main;
            bool cameraObserved = camera != null;
            bool cameraChanged = cameraObserved && _previousCameraObserved && camera != _previousCamera;
            float cameraPositionDelta = 0f;
            float cameraRotationDelta = 0f;
            if (cameraObserved && _previousCameraObserved && camera == _previousCamera)
            {
                cameraPositionDelta = Vector3.Distance(
                    _previousCameraPosition, camera.transform.position);
                cameraRotationDelta = Quaternion.Angle(
                    _previousCameraRotation, camera.transform.rotation);
            }
            var sample = new RuntimePerformanceSample
            {
                Index = Samples.Count,
                ElapsedStopwatchTicks = elapsed,
                ElapsedMilliseconds = TicksToMilliseconds(elapsed),
                FrameCount = _sampleFrameCount,
                FramesPerSecond = _sampleFrameCount * (double)Stopwatch.Frequency / elapsed,
                GameMode = _gameMode,
                PlannerOpen = _plannerOpen,
                HudInstalled = _hudInstalled,
                StaticCanvasAvailable = _staticCanvasAvailable,
                CameraObserved = cameraObserved,
                CameraChanged = cameraChanged,
                CameraPositionDelta = cameraPositionDelta,
                CameraRotationDeltaDegrees = cameraRotationDelta,
                CameraMoving = cameraChanged || cameraPositionDelta > 0.001f || cameraRotationDelta > 0.01f,
                ApplicationTargetFrameRate = Application.targetFrameRate,
                VSyncCount = QualitySettings.vSyncCount,
                TimeScale = Time.timeScale,
                FixedDeltaTime = Time.fixedDeltaTime,
                MaximumDeltaTime = Time.maximumDeltaTime,
                CaptureFramerate = Time.captureFramerate,
                HudObjectFindFoundCount = _sampleHudFindFoundCount,
                MainUpdate = MainUpdate.Snapshot(),
                UiRootTick = UiRootTick.Snapshot(),
                ScreenTick = ScreenTick.Snapshot(),
                HudInstall = HudInstall.Snapshot(),
                HudObjectFind = HudObjectFind.Snapshot(),
                HudTick = HudTick.Snapshot(),
                PointerTickPrefix = PointerTickPrefix.Snapshot(),
                CameraScrollPostfix = CameraScrollPostfix.Snapshot(),
                NativeHotkeyPrefix = NativeHotkeyPrefix.Snapshot()
            };
            Samples.Add(sample);
            if (cameraObserved)
            {
                _previousCamera = camera;
                _previousCameraPosition = camera.transform.position;
                _previousCameraRotation = camera.transform.rotation;
                _previousCameraObserved = true;
            }
            else
            {
                _previousCamera = null;
                _previousCameraObserved = false;
            }
            _log.Info("[KBP-PERF] sample=" + sample.Index + ";fps=" +
                sample.FramesPerSecond.ToString("F2") + ";frames=" + sample.FrameCount +
                ";mode=" + sample.GameMode + ";cameraMoving=" + sample.CameraMoving +
                ";hudInstalled=" + sample.HudInstalled + ";hudFindCount=" +
                sample.HudObjectFind.InvocationCount + ";hudFindTotalMs=" +
                sample.HudObjectFind.TotalMilliseconds.ToString("F3") +
                ";hudFindMaxMs=" + sample.HudObjectFind.MaximumMilliseconds.ToString("F3") +
                ";pointerCount=" + sample.PointerTickPrefix.InvocationCount +
                ";pointerTotalMs=" + sample.PointerTickPrefix.TotalMilliseconds.ToString("F3") +
                ";cameraPatchCount=" + sample.CameraScrollPostfix.InvocationCount +
                ";cameraPatchTotalMs=" + sample.CameraScrollPostfix.TotalMilliseconds.ToString("F3") +
                ";hotkeyPrefixCount=" + sample.NativeHotkeyPrefix.InvocationCount +
                ";hotkeyPrefixTotalMs=" + sample.NativeHotkeyPrefix.TotalMilliseconds.ToString("F3") + ".");
            ResetSample();
            _sampleStartedAtTimestamp = endedAt;
        }

        private static MutableOperationTiming Timing(RuntimePerformanceOperation operation)
        {
            switch (operation)
            {
                case RuntimePerformanceOperation.MainUpdate: return MainUpdate;
                case RuntimePerformanceOperation.UiRootTick: return UiRootTick;
                case RuntimePerformanceOperation.ScreenTick: return ScreenTick;
                case RuntimePerformanceOperation.HudInstall: return HudInstall;
                case RuntimePerformanceOperation.HudObjectFind: return HudObjectFind;
                case RuntimePerformanceOperation.HudTick: return HudTick;
                case RuntimePerformanceOperation.PointerTickPrefix: return PointerTickPrefix;
                case RuntimePerformanceOperation.CameraScrollPostfix: return CameraScrollPostfix;
                case RuntimePerformanceOperation.NativeHotkeyPrefix: return NativeHotkeyPrefix;
                default: throw new ArgumentOutOfRangeException("operation");
            }
        }

        private static double TicksToMilliseconds(long ticks)
        {
            return ticks * 1000.0 / Stopwatch.Frequency;
        }

        private static void ResetSample()
        {
            _sampleFrameCount = 0;
            _sampleHudFindFoundCount = 0;
            MainUpdate.Reset();
            UiRootTick.Reset();
            ScreenTick.Reset();
            HudInstall.Reset();
            HudObjectFind.Reset();
            HudTick.Reset();
            PointerTickPrefix.Reset();
            CameraScrollPostfix.Reset();
            NativeHotkeyPrefix.Reset();
        }

        private static void Reset()
        {
            _enabled = false;
            _disableHudDiscovery = false;
            _durationSeconds = 0;
            _minimumFramesPerSecond = 0;
            _runId = string.Empty;
            _log = null;
            _startedAtUtc = default(DateTime);
            _startedAtTimestamp = 0;
            _sampleStartedAtTimestamp = 0;
            _totalFrameCount = 0;
            _plannerOpen = false;
            _hudInstalled = false;
            _staticCanvasAvailable = false;
            _gameMode = "game-null";
            _previousCamera = null;
            _previousCameraObserved = false;
            _completedProfile = null;
            Samples.Clear();
            ResetSample();
        }

        private sealed class MutableOperationTiming
        {
            private int _invocationCount;
            private long _totalTicks;
            private long _maximumTicks;

            internal void Record(long elapsed)
            {
                _invocationCount++;
                _totalTicks += elapsed;
                if (elapsed > _maximumTicks) _maximumTicks = elapsed;
            }

            internal RuntimePerformanceOperationSample Snapshot()
            {
                return new RuntimePerformanceOperationSample
                {
                    InvocationCount = _invocationCount,
                    TotalMilliseconds = TicksToMilliseconds(_totalTicks),
                    MaximumMilliseconds = TicksToMilliseconds(_maximumTicks),
                    AverageMilliseconds = _invocationCount == 0 ? 0 :
                        TicksToMilliseconds(_totalTicks) / _invocationCount
                };
            }

            internal void Reset()
            {
                _invocationCount = 0;
                _totalTicks = 0;
                _maximumTicks = 0;
            }
        }
    }

    internal sealed class RuntimePerformanceProfile
    {
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [JsonProperty("runId", Order = 2)] public string RunId { get; set; }
        [JsonProperty("version", Order = 3)] public string Version { get; set; }
        [JsonProperty("commit", Order = 4)] public string Commit { get; set; }
        [JsonProperty("disableHudDiscovery", Order = 5)] public bool DisableHudDiscovery { get; set; }
        [JsonProperty("requestedDurationSeconds", Order = 6)] public int RequestedDurationSeconds { get; set; }
        [JsonProperty("requestedMinimumFramesPerSecond", Order = 7)] public double RequestedMinimumFramesPerSecond { get; set; }
        [JsonProperty("startedAtUtc", Order = 8)] public string StartedAtUtc { get; set; }
        [JsonProperty("endedAtUtc", Order = 9)] public string EndedAtUtc { get; set; }
        [JsonProperty("elapsedMilliseconds", Order = 10)] public double ElapsedMilliseconds { get; set; }
        [JsonProperty("totalFrameCount", Order = 11)] public int TotalFrameCount { get; set; }
        [JsonProperty("qualifiedSampleCount", Order = 12)] public int QualifiedSampleCount { get; set; }
        [JsonProperty("minimumFramesPerSecond", Order = 13)] public double MinimumFramesPerSecond { get; set; }
        [JsonProperty("averageFramesPerSecond", Order = 14)] public double AverageFramesPerSecond { get; set; }
        [JsonProperty("maximumFramesPerSecond", Order = 15)] public double MaximumFramesPerSecond { get; set; }
        [JsonProperty("meetsRequestedMinimum", Order = 16)] public bool MeetsRequestedMinimum { get; set; }
        [JsonProperty("hudObjectFindInvocationCount", Order = 17)] public long HudObjectFindInvocationCount { get; set; }
        [JsonProperty("hudObjectFindTotalMilliseconds", Order = 18)] public double HudObjectFindTotalMilliseconds { get; set; }
        [JsonProperty("hudObjectFindMaximumMilliseconds", Order = 19)] public double HudObjectFindMaximumMilliseconds { get; set; }
        [JsonProperty("samples", Order = 20)] public List<RuntimePerformanceSample> Samples { get; set; }
    }

    internal sealed class RuntimePerformanceSample
    {
        [JsonProperty("index", Order = 1)] public int Index { get; set; }
        [JsonProperty("elapsedStopwatchTicks", Order = 2)] public long ElapsedStopwatchTicks { get; set; }
        [JsonProperty("elapsedMilliseconds", Order = 3)] public double ElapsedMilliseconds { get; set; }
        [JsonProperty("frameCount", Order = 4)] public int FrameCount { get; set; }
        [JsonProperty("framesPerSecond", Order = 5)] public double FramesPerSecond { get; set; }
        [JsonProperty("gameMode", Order = 6)] public string GameMode { get; set; }
        [JsonProperty("plannerOpen", Order = 7)] public bool PlannerOpen { get; set; }
        [JsonProperty("hudInstalled", Order = 8)] public bool HudInstalled { get; set; }
        [JsonProperty("staticCanvasAvailable", Order = 9)] public bool StaticCanvasAvailable { get; set; }
        [JsonProperty("cameraObserved", Order = 10)] public bool CameraObserved { get; set; }
        [JsonProperty("cameraChanged", Order = 11)] public bool CameraChanged { get; set; }
        [JsonProperty("cameraMoving", Order = 12)] public bool CameraMoving { get; set; }
        [JsonProperty("cameraPositionDelta", Order = 13)] public float CameraPositionDelta { get; set; }
        [JsonProperty("cameraRotationDeltaDegrees", Order = 14)] public float CameraRotationDeltaDegrees { get; set; }
        [JsonProperty("applicationTargetFrameRate", Order = 15)] public int ApplicationTargetFrameRate { get; set; }
        [JsonProperty("vSyncCount", Order = 16)] public int VSyncCount { get; set; }
        [JsonProperty("timeScale", Order = 17)] public float TimeScale { get; set; }
        [JsonProperty("fixedDeltaTime", Order = 18)] public float FixedDeltaTime { get; set; }
        [JsonProperty("maximumDeltaTime", Order = 19)] public float MaximumDeltaTime { get; set; }
        [JsonProperty("captureFramerate", Order = 20)] public int CaptureFramerate { get; set; }
        [JsonProperty("hudObjectFindFoundCount", Order = 21)] public int HudObjectFindFoundCount { get; set; }
        [JsonProperty("mainUpdate", Order = 22)] public RuntimePerformanceOperationSample MainUpdate { get; set; }
        [JsonProperty("uiRootTick", Order = 23)] public RuntimePerformanceOperationSample UiRootTick { get; set; }
        [JsonProperty("screenTick", Order = 24)] public RuntimePerformanceOperationSample ScreenTick { get; set; }
        [JsonProperty("hudInstall", Order = 25)] public RuntimePerformanceOperationSample HudInstall { get; set; }
        [JsonProperty("hudObjectFind", Order = 26)] public RuntimePerformanceOperationSample HudObjectFind { get; set; }
        [JsonProperty("hudTick", Order = 27)] public RuntimePerformanceOperationSample HudTick { get; set; }
        [JsonProperty("pointerTickPrefix", Order = 28)] public RuntimePerformanceOperationSample PointerTickPrefix { get; set; }
        [JsonProperty("cameraScrollPostfix", Order = 29)] public RuntimePerformanceOperationSample CameraScrollPostfix { get; set; }
        [JsonProperty("nativeHotkeyPrefix", Order = 30)] public RuntimePerformanceOperationSample NativeHotkeyPrefix { get; set; }
    }

    internal sealed class RuntimePerformanceOperationSample
    {
        [JsonProperty("invocationCount", Order = 1)] public int InvocationCount { get; set; }
        [JsonProperty("totalMilliseconds", Order = 2)] public double TotalMilliseconds { get; set; }
        [JsonProperty("maximumMilliseconds", Order = 3)] public double MaximumMilliseconds { get; set; }
        [JsonProperty("averageMilliseconds", Order = 4)] public double AverageMilliseconds { get; set; }
    }
}
