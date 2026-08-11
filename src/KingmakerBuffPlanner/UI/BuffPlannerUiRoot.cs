using System;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class BuffPlannerUiRoot : MonoBehaviour
    {
        private const string ObjectName = "KingmakerBuffPlanner.UiRoot";
        private static BuffPlannerUiRoot _instance;
        private PlannerUiSession _session;
        private ModLog _log;
        private bool _enabled = true;
        private bool _open;
        private Rect _window = new Rect(120, 80, 1120, 720);
        private Vector2 _sourceScroll;
        private Vector2 _detailScroll;
        private string _search = string.Empty;
        private string _routineId = "long";
        private bool _configuredOnly;
        private bool _showHidden;
        private string _uiError = string.Empty;
        private int _renderedOpenFrames;
        private int _runtimeOpenCycles;
        private string _previewRoutineId = string.Empty;

        internal static void Ensure(string modPath, ModLog log)
        {
            if (_instance != null) return;
            var gameObject = new GameObject(ObjectName);
            DontDestroyOnLoad(gameObject);
            _instance = gameObject.AddComponent<BuffPlannerUiRoot>();
            _instance._session = new PlannerUiSession(modPath, log);
            _instance._log = log;
        }

        internal static void SetEnabled(bool enabled)
        {
            if (_instance == null) return;
            _instance._enabled = enabled;
            if (!enabled) _instance._open = false;
        }

        internal static void DestroyOwned()
        {
            if (_instance == null) return;
            Destroy(_instance.gameObject);
            _instance = null;
        }

        internal static void BeginRuntimeSmoke()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            if (_instance._runtimeOpenCycles == 0) _instance._renderedOpenFrames = 0;
            _instance._runtimeOpenCycles++;
            _instance._open = true;
            _instance._session.Refresh();
        }

        internal static UiRootDiagnostics EndRuntimeSmoke()
        {
            if (_instance == null) throw new InvalidOperationException("UI root is absent.");
            _instance._open = false;
            return new UiRootDiagnostics
            {
                RootCount = FindObjectsOfType<BuffPlannerUiRoot>().Length,
                RenderedOpenFrames = _instance._renderedOpenFrames,
                OpenCloseCycles = _instance._runtimeOpenCycles,
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                RoutineButtonCount = 3,
                CriticalControlsOnScreen = Screen.width >= 512 && Screen.height >= 240
            };
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (_enabled && Input.GetKeyDown(KeyCode.F10)) ToggleOpen();
        }

        private void OnGUI()
        {
            if (!_enabled || _session == null) return;
            float scale = _session.Model == null ? 1.0f : _session.Model.Profile.Ui.Scale;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            try
            {
                float logicalWidth = Screen.width / scale;
                float logicalHeight = Screen.height / scale;
                if (GUI.Button(new Rect(18, logicalHeight - 54, 150, 36), "Buff Planner (F10)")) ToggleOpen();
                DrawRoutineHud(logicalHeight);
                if (!_open) return;
                _window.width = Mathf.Min(_window.width, logicalWidth - 24);
                _window.height = Mathf.Min(_window.height, logicalHeight - 24);
                _window.x = Mathf.Clamp(_window.x, 0, Mathf.Max(0, logicalWidth - _window.width));
                _window.y = Mathf.Clamp(_window.y, 0, Mathf.Max(0, logicalHeight - _window.height));
                _window = GUI.Window(847261, _window, DrawWindow, "Kingmaker Buff Planner");
                _renderedOpenFrames++;
            }
            catch (Exception exception)
            {
                _uiError = exception.GetType().Name + ": " + exception.Message;
                _log.Error("Planner UI rendering failed.", exception);
            }
            finally { GUI.matrix = oldMatrix; }
        }

        private void ToggleOpen()
        {
            _open = !_open;
            _uiError = string.Empty;
            if (_open) _session.Refresh();
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(_session.Status, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Refresh", GUILayout.Width(75))) _session.Refresh();
            if (GUILayout.Button("Close", GUILayout.Width(60))) _open = false;
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_uiError)) GUILayout.Label("UI error: " + _uiError);
            PlannerSetupModel model = _session.Model;
            if (model == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Load a campaign, then press Refresh. No save data is modified by this screen.");
                GUILayout.FlexibleSpace();
                GUI.DragWindow(new Rect(0, 0, 10000, 24));
                GUILayout.EndVertical();
                return;
            }

            DrawRoutineControls(model);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", GUILayout.Width(50));
            _search = GUILayout.TextField(_search ?? string.Empty, GUILayout.Width(260));
            _configuredOnly = GUILayout.Toggle(_configuredOnly, "Configured only", GUILayout.Width(130));
            _showHidden = GUILayout.Toggle(_showHidden, "Show hidden", GUILayout.Width(110));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Scale", GUILayout.Width(40));
            foreach (float scale in new[] { 0.8f, 1.0f, 1.25f, 1.5f })
                if (GUILayout.Button(scale.ToString("0.##"), GUILayout.Width(48))) Safe(() => model.SetScale(scale));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawSourceList(model);
            DrawDetails(model);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
        }

        private void DrawSourceList(PlannerSetupModel model)
        {
            GUILayout.BeginVertical(GUILayout.Width(390));
            GUILayout.Label("Discovered buff sources");
            _sourceScroll = GUILayout.BeginScrollView(_sourceScroll, GUI.skin.box);
            foreach (SetupSourceRow source in model.Sources.Where(MatchesFilter))
            {
                bool selected = source.SourceId == model.SelectedSourceId;
                string configured = model.Profile.Routines.Any(r => r.Assignments.Any(a => a.SourceId == source.SourceId))
                    ? "[x] " : "[ ] ";
                if (GUILayout.Button((selected ? "> " : "") + configured + source.DisplayName +
                    "  L" + source.SpellLevel + "  (" + source.Providers.Count + ")"))
                    Safe(() => model.SelectSource(source.SourceId));
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawRoutineHud(float logicalHeight)
        {
            PlannerSetupModel model = _session.Model;
            bool oldEnabled = GUI.enabled;
            GUI.enabled = model != null && !_session.IsExecuting;
            float x = 176;
            foreach (string routineId in new[] { "long", "important", "short" })
            {
                RoutineProfile routine = model == null ? null : model.Profile.Routines
                    .FirstOrDefault(r => r.RoutineId == routineId);
                string name = routine == null
                    ? routineId.Substring(0, 1).ToUpperInvariant() + routineId.Substring(1)
                    : routine.Name;
                if (GUI.Button(new Rect(x, logicalHeight - 54, 104, 36),
                    new GUIContent(name, routine == null
                        ? "Load a campaign to run this routine."
                        : RoutineTooltip(routine))))
                {
                    _previewRoutineId = routineId;
                    StartCoroutine(_session.ExecuteRoutine(routineId));
                }
                x += 110;
            }
            GUI.enabled = oldEnabled;
            if (!string.IsNullOrEmpty(GUI.tooltip))
                GUI.Label(new Rect(18, logicalHeight - 80, 700, 22), GUI.tooltip, GUI.skin.box);
        }

        private void DrawRoutineControls(PlannerSetupModel model)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Routines", GUILayout.Width(55));
            foreach (RoutineProfile routine in model.Profile.Routines.OrderBy(r => RoutineOrder(r.RoutineId)))
            {
                GUI.enabled = !_session.IsExecuting;
                if (GUILayout.Button("Preview " + routine.Name, GUILayout.Width(105)))
                    Safe(() => { _session.PreviewRoutine(routine.RoutineId); _previewRoutineId = routine.RoutineId; });
                if (GUILayout.Button("Run " + routine.Name, GUILayout.Width(85)))
                {
                    _previewRoutineId = routine.RoutineId;
                    StartCoroutine(_session.ExecuteRoutine(routine.RoutineId));
                }
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Mode: " + model.Profile.Execution.Mode, GUILayout.Width(120)))
                Safe(model.ToggleExecutionMode);
            if (GUILayout.Button(model.Profile.Execution.OutOfCombatOnly
                ? "Combat: blocked" : "Combat: allowed", GUILayout.Width(120)))
                Safe(model.ToggleOutOfCombatOnly);
            GUILayout.EndHorizontal();
            if (_session.LastPreview != null && !string.IsNullOrEmpty(_previewRoutineId))
                GUILayout.Label("Preview " + _previewRoutineId + ": " +
                    PlanSummary(_session.LastPreview));
            if (_session.LastExecutionReport != null)
            {
                var report = _session.LastExecutionReport;
                GUILayout.Label("Last execution: planned=" + report.Planned +
                    "; fired=" + report.Fired + "; observed=" + report.SuccessfullyObserved +
                    "; resources spent=" + report.ResourcesSpent + "; failed=" + report.Failed +
                    "; skipped=" + report.Skipped + "; unfulfilled=" + report.Unfulfilled + ".");
            }
        }

        private string RoutineTooltip(RoutineProfile routine)
        {
            int targets = routine.Assignments.Sum(a => a.WantedTargetUnitIds.Count);
            return routine.Name + ": sources=" + routine.Assignments.Count +
                "; requested targets=" + targets + "; mode=" +
                (_session.Model == null ? "unavailable" : _session.Model.Profile.Execution.Mode) + ".";
        }

        private static string PlanSummary(RoutinePlanResult preview)
        {
            int fulfilled = preview.Plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Fulfilled);
            int skipped = preview.Plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.SkippedAlreadyActive);
            int unfulfilled = preview.Plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Unfulfilled);
            int resources = preview.Plan.Steps.Sum(s => s.Reservation.Units);
            int materials = preview.Plan.Steps.Sum(s => s.MaterialReservation == null
                ? 0 : s.MaterialReservation.Count);
            return "planned casts=" + preview.Plan.Steps.Count + "; fulfilled=" + fulfilled +
                "; skipped=" + skipped + "; unfulfilled=" + unfulfilled +
                "; unsupported=" + preview.UnsupportedSourceIds.Count +
                "; resource units=" + resources + "; material units=" + materials + ".";
        }

        private static int RoutineOrder(string routineId)
        {
            if (routineId == "long") return 0;
            if (routineId == "important") return 1;
            if (routineId == "short") return 2;
            return 3;
        }

        private bool MatchesFilter(SetupSourceRow source)
        {
            PlannerSetupModel model = _session.Model;
            if (!string.IsNullOrWhiteSpace(_search) &&
                source.DisplayName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) return false;
            bool configured = model.Profile.Routines.Any(r => r.Assignments.Any(a => a.SourceId == source.SourceId));
            if (_configuredOnly && !configured) return false;
            if (!_showHidden && model.Profile.HiddenSourceIds.Contains(source.SourceId)) return false;
            return true;
        }

        private void DrawDetails(PlannerSetupModel model)
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUI.skin.box);
            SetupSourceRow source = model.SelectedSource;
            if (source == null)
            {
                GUILayout.Label("No structurally discovered party provider is available.");
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }
            GUILayout.Label(source.DisplayName + " — level " + source.SpellLevel);
            GUILayout.BeginHorizontal();
            bool hidden = model.Profile.HiddenSourceIds.Contains(source.SourceId);
            if (GUILayout.Button(hidden ? "Unhide source" : "Hide source", GUILayout.Width(110)))
                Safe(model.ToggleHidden);
            ExistingEffectPolicy policy = model.GetExistingEffectPolicy(_routineId);
            if (GUILayout.Button(policy == ExistingEffectPolicy.SkipAlreadyActive
                ? "Skip active" : "Overwrite active", GUILayout.Width(130)))
                Safe(() => model.ToggleExistingEffectPolicy(_routineId));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            foreach (RoutineProfile routine in model.Profile.Routines)
            {
                bool assigned = model.IsAssigned(routine.RoutineId);
                if (GUILayout.Button((assigned ? "Remove " : "Add to ") + routine.Name))
                    Safe(() => model.ToggleRoutine(routine.RoutineId));
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Edit targets for:", GUILayout.Width(100));
            foreach (RoutineProfile routine in model.Profile.Routines)
                if (GUILayout.Toggle(_routineId == routine.RoutineId, routine.Name, GUI.skin.button))
                    _routineId = routine.RoutineId;
            GUILayout.EndHorizontal();

            GUILayout.Label("Party and pet target matrix");
            GUILayout.BeginHorizontal();
            foreach (UnitSnapshot unit in model.Snapshot.Units)
            {
                bool wanted = model.IsTargetWanted(_routineId, unit.UnitId);
                EffectPresenceKind presence = model.GetPresence(unit.UnitId);
                string state = !unit.TargetValidation.Targetable || !unit.TargetValidation.Alive
                    ? " invalid" : presence == EffectPresenceKind.Complete ? " active" : string.Empty;
                if (GUILayout.Button((wanted ? "[x] " : "[ ] ") + unit.DisplayName + state,
                    GUILayout.MinWidth(120))) Safe(() => model.ToggleTarget(_routineId, unit.UnitId));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Providers — click state to cycle Auto → Priority → Banned → Auto");
            foreach (ProviderSnapshot provider in source.Providers)
            {
                ProviderPreferenceProfile preference = model.GetProviderPreference(provider.Key.Canonical);
                string state = preference == null ? "Auto" : preference.Banned ? "Banned" : "Priority";
                string cap = preference == null || preference.MaximumCasts == null
                    ? "none" : preference.MaximumCasts.Value.ToString();
                GUILayout.BeginHorizontal();
                GUILayout.Label(provider.DisplayName + " L" + provider.SpellLevel +
                    " pool=" + provider.ResourcePoolKey, GUILayout.ExpandWidth(true));
                if (GUILayout.Button(state, GUILayout.Width(75)))
                    Safe(() => model.CycleProviderPreference(provider.Key.Canonical));
                GUILayout.Label("cap " + cap, GUILayout.Width(60));
                if (GUILayout.Button("-", GUILayout.Width(28)))
                    Safe(() => model.AdjustProviderCap(provider.Key.Canonical, -1));
                if (GUILayout.Button("+", GUILayout.Width(28)))
                    Safe(() => model.AdjustProviderCap(provider.Key.Canonical, 1));
                GUILayout.EndHorizontal();
            }

            int wantedCount = model.Snapshot.Units.Count(u => model.IsTargetWanted(_routineId, u.UnitId));
            int activeCount = model.Snapshot.Units.Count(u => model.IsTargetWanted(_routineId, u.UnitId) &&
                model.GetPresence(u.UnitId) == EffectPresenceKind.Complete);
            GUILayout.Label("Preview: requested targets=" + wantedCount +
                "; already active=" + activeCount +
                "; providers=" + source.Providers.Count +
                "; execution=" + model.Profile.Execution.Mode + ".");
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void Safe(Action action)
        {
            try
            {
                action();
                _uiError = string.Empty;
            }
            catch (Exception exception)
            {
                _uiError = exception.Message;
                _log.Error("Planner UI action failed.", exception);
            }
        }
    }

    internal sealed class UiRootDiagnostics
    {
        internal int RootCount;
        internal int RenderedOpenFrames;
        internal int OpenCloseCycles;
        internal int ScreenWidth;
        internal int ScreenHeight;
        internal int RoutineButtonCount;
        internal bool CriticalControlsOnScreen;
    }
}
