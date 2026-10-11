using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UI.Constructor;
using Kingmaker.UI.Selection;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UI.Tooltip;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Native UI finish: live-workspace-native-ui, with zero OS input. The
    // native controls are driven through the game's own contracts (service
    // window handler, spellbook TooltipTrigger, key-binding callbacks, Unity
    // pointer events on owned and native HUD cells); the planner through its
    // production open/close, its own Escape handling and session commands
    // (labelled direct session calls). Every native window opened here is
    // closed through its own close control; nothing is saved to the game and
    // the session lock refuses the one routine press.
    internal sealed partial class RuntimeTestHost
    {
        private const int NativeUiPrePhase = 150;
        private const int NativeUiOpenPhase = 151;
        private static readonly string[] NativeUiSwitchRoutes = { "inventory", "character", "spellbook" };
        private readonly NativeUiRecord _nativeUi = new NativeUiRecord();
        private int _nativeUiStep;
        private Stopwatch _nativeUiClock = new Stopwatch();
        private bool _nativeUiWritten;
        private List<UnitEntityData> _nativeUiUnits;
        private int _nativeUiUnitIndex;
        private int _nativeUiSwitchIndex;
        private NativeUiWindowSwitch _nativeUiSwitch;
        private string _nativeUiSourceId;
        private int _nativeUiLogLinesBefore;
        private int _nativeUiRunsBefore;
        private FileStream _nativeUiPlanLock;
        private string _nativeUiDocumentBeforeFault;
        private readonly List<string> _nativeUiRoutes = new List<string>();

        private string NativeUiStage
        {
            get
            {
                object value;
                return _request.Parameters.TryGetValue("nativeUiStage", out value) ? value as string : null;
            }
        }

        private bool NativeUiWaited(long milliseconds)
        {
            if (!_nativeUiClock.IsRunning) _nativeUiClock.Restart();
            return _nativeUiClock.ElapsedMilliseconds >= milliseconds;
        }

        private void NativeUiNext(int step)
        {
            _nativeUiStep = step;
            _nativeUiClock.Reset();
        }

        private void NativeUiCapture(string file)
        {
            try
            {
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, file));
                _nativeUi.Captures.Add(file);
            }
            catch (Exception exception)
            {
                _nativeUi.MissingCaptures.Add(file + ":" + exception.GetType().Name);
            }
        }

        // ---- before the planner opens: HUD, spellbook, native popup -------

        private bool UpdateNativeUiBeforeOpen()
        {
            try
            {
                return NativeUiBeforeOpenStep();
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-NUI] before-open step " + _nativeUiStep + " failed.", exception);
                return FinishNativeUi("before-open:" + _nativeUiStep + ":" + exception.GetType().Name + ":" +
                    exception.Message);
            }
        }

        private bool NativeUiBeforeOpenStep()
        {
            BuffPlannerHudButtonController hud = BuffPlannerUiRoot.HudForRuntime;
            switch (_nativeUiStep)
            {
                case 0:
                {
                    _nativeUi.RunId = _request.RunId;
                    _nativeUi.Stage = NativeUiStage;
                    _nativeUi.ScreenWidth = Screen.width;
                    _nativeUi.ScreenHeight = Screen.height;
                    Canvas canvas = StaticCanvas.Instance == null ? null : StaticCanvas.Instance.GetComponent<Canvas>();
                    _nativeUi.CanvasScale = canvas == null ? 0f : canvas.scaleFactor;
                    _nativeUiRunsBefore = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                    RectTransform cluster = hud == null ? null : hud.NativeClusterForRuntime;
                    if (cluster == null) return FinishNativeUi("hud-native-cluster-missing");
                    var hudFacts = new JObject
                    {
                        { "panel", NativeUiFacts.Subtree(cluster, "Menu_Buttons48px") },
                        { "log", NativeUiFacts.Subtree(NativeUiFacts.GameLogPanel(), "game-log") },
                        { "plannerHud", hud.ObjectEvidence }
                    };
                    NativeUiFacts.Write(_request.EvidenceDirectory, "nui-facts-hud.json", hudFacts);
                    _nativeUi.FactsFiles.Add("hud");
                    NativeUiFacts.Write(_request.EvidenceDirectory, "nui-facts-text.json", new JObject
                    {
                        { "textMesh", NativeUiFacts.TextMeshInventory() },
                        { "bindings", NativeUiFacts.KeyboardBindings() }
                    });
                    _nativeUi.FactsFiles.Add("text");
                    foreach (string file in NativeUiFacts.Previews(NativeUiFacts.Sprites(cluster),
                            _request.EvidenceDirectory, 48, _nativeUi.Notes))
                        _nativeUi.AddNote("preview:" + file);
                    WriteNamedPreviews();
                    RecordHudGeometry(hud);
                    NativeUiCapture("nui-hud.png");
                    hud.BeginRuntimePhysicalObservation();
                    NativeUiNext(1);
                    return false;
                }
                case 1:
                {
                    if (!NativeUiWaited(800)) return false;
                    Button setup = hud.ButtonForRuntime("setup");
                    if (setup != null) Pointer(setup.gameObject, ExecuteEvents.pointerEnterHandler);
                    NativeUiNext(2);
                    return false;
                }
                case 2:
                {
                    if (!NativeUiWaited(1800)) return false;
                    HudTooltipRuntimeDiagnostics tooltip = BuffPlannerUiRoot.TooltipDiagnosticsForRuntime();
                    _nativeUi.PlannerHudTooltipShown = tooltip != null && tooltip.Active && tooltip.NativeTriggerCount == 4;
                    string text = hud.TooltipForRuntime("setup");
                    _nativeUi.PlannerHudTooltipTitle = text == null ? null : text.Split('\n')[0];
                    NativeUiCapture("nui-hud-hover-planner.png");
                    Button setup = hud.ButtonForRuntime("setup");
                    if (setup != null) Pointer(setup.gameObject, ExecuteEvents.pointerExitHandler);
                    Button native = NativeFormationButton(hud);
                    if (native != null) Pointer(native.gameObject, ExecuteEvents.pointerEnterHandler);
                    NativeUiNext(3);
                    return false;
                }
                case 3:
                {
                    if (!NativeUiWaited(1800)) return false;
                    NativeUiCapture("nui-hud-hover-native.png");
                    Button native = NativeFormationButton(hud);
                    if (native != null) Pointer(native.gameObject, ExecuteEvents.pointerExitHandler);
                    Button longButton = hud.ButtonForRuntime("long");
                    if (longButton != null) Pointer(longButton.gameObject, ExecuteEvents.pointerDownHandler);
                    NativeUiNext(4);
                    return false;
                }
                case 4:
                {
                    if (!NativeUiWaited(500)) return false;
                    NativeUiCapture("nui-hud-pressed-planner.png");
                    Button longButton = hud.ButtonForRuntime("long");
                    // Up without a click: the pressed state ends, nothing runs.
                    if (longButton != null)
                    {
                        Pointer(longButton.gameObject, ExecuteEvents.pointerUpHandler);
                        Pointer(longButton.gameObject, ExecuteEvents.pointerExitHandler);
                    }
                    _nativeUi.NativeActivationsDuringHudProbe = hud.RuntimeUnderlyingNativeActivationCount;
                    hud.EndRuntimePhysicalObservation();
                    _nativeUiUnits = SpellbookUnitsInOrder();
                    _nativeUi.AddNote("units:" + string.Join(",", _nativeUiUnits.Select(UnitLabel).ToArray()));
                    if (_nativeUiUnits.Count == 0) return FinishNativeUi("no-party-units");
                    _nativeUiUnitIndex = 0;
                    SelectUnit(_nativeUiUnits[0]);
                    EventBus.RaiseEvent<IServiceWindowUIHandler>(handler => handler.HandleOpenSpellbook());
                    NativeUiNext(5);
                    return false;
                }
                case 5:
                {
                    if (!NativeUiWaited(2800)) return false;
                    RecordSpellbookView(_nativeUiUnitIndex);
                    if (_nativeUiUnitIndex == 0)
                    {
                        WriteSpellbookFacts();
                        TooltipTrigger trigger = FirstSpellTrigger();
                        if (trigger == null) return FinishNativeUi("spellbook-no-spell-trigger");
                        trigger.OpenDescriptionWindow();
                        NativeUiNext(6);
                        return false;
                    }
                    NativeUiNext(7);
                    return false;
                }
                case 6:
                {
                    if (!NativeUiWaited(2000)) return false;
                    DescriptionWindow window = NativeDescriptionWindow();
                    _nativeUi.NativePopupOpened = window != null && window.gameObject.activeInHierarchy;
                    _nativeUi.NativePopupTitle = DescriptionTitle(window);
                    NativeUiCapture("nui-native-popup.png");
                    Transform constructor = StaticCanvas.Instance.transform.Find("Tooltips/DescWinConstructor");
                    NativeUiFacts.Write(_request.EvidenceDirectory, "nui-facts-description.json",
                        NativeUiFacts.Subtree(constructor, "DescWinConstructor"));
                    _nativeUi.FactsFiles.Add("native-popup");
                    foreach (string file in NativeUiFacts.Previews(NativeUiFacts.Sprites(constructor),
                            _request.EvidenceDirectory, 32, _nativeUi.Notes))
                        _nativeUi.AddNote("preview:" + file);
                    if (window != null) window.CloseWindow();
                    NativeUiNext(7);
                    return false;
                }
                case 7:
                {
                    if (!NativeUiWaited(900)) return false;
                    _nativeUiUnitIndex++;
                    if (_nativeUiUnitIndex < Math.Min(3, _nativeUiUnits.Count))
                    {
                        SelectUnit(_nativeUiUnits[_nativeUiUnitIndex]);
                        _nativeUiStep = 5;
                        _nativeUiClock.Restart();
                        return false;
                    }
                    // Back to the first caster; press the owned spellbook
                    // entry exactly as its click delivers it.
                    SelectUnit(_nativeUiUnits[0]);
                    NativeUiNext(8);
                    return false;
                }
                case 8:
                {
                    if (!NativeUiWaited(1500)) return false;
                    BuffPlannerSpellbookEntryController entry = BuffPlannerUiRoot.SpellbookEntryForRuntime;
                    Button owned = entry == null ? null : entry.OwnedButtonForRuntime;
                    if (owned == null || !owned.IsInteractable())
                    {
                        _nativeUi.Failures.Add("spellbook-entry-not-offered");
                        CloseNativeServiceWindow();
                        NativeUiNext(10);
                        return false;
                    }
                    _nativeUi.AddNote("spellbook-entry:click-invoked;no-os-input");
                    owned.onClick.Invoke();
                    NativeUiNext(9);
                    return false;
                }
                case 9:
                {
                    bool open = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    if (!open && !NativeUiWaited(10000)) return false;
                    if (open && !NativeUiWaited(1500)) return false;
                    _nativeUi.SpellbookHandoffOpenedPlanner = open;
                    if (open) NativeUiCapture("nui-planner-from-spellbook.png");
                    CloseProbeWorkspace();
                    NativeUiNext(10);
                    return false;
                }
                default:
                {
                    // Every native window released before the production open.
                    bool released = !NativeServiceWindowShown() && Game.Instance != null &&
                        !Game.Instance.IsModeActive(GameModeType.FullScreenUi);
                    if (!released && !NativeUiWaited(8000)) return false;
                    if (!released)
                    {
                        CloseNativeServiceWindow();
                        _nativeUi.AddNote("native-window-closed-before-open");
                    }
                    _nativeUiStep = 0;
                    _nativeUiClock.Reset();
                    _liveUiPhase = 22;
                    return false;
                }
            }
        }

        // ---- the planner is open: presentation, popup, switching, Escape, log

        private bool UpdateNativeUiOpened()
        {
            try
            {
                return NativeUiOpenedStep();
            }
            catch (Exception exception)
            {
                _log.Error("[KBP-NUI] opened step " + _nativeUiStep + " failed.", exception);
                return FinishNativeUi("opened:" + _nativeUiStep + ":" + exception.GetType().Name + ":" +
                    exception.Message);
            }
        }

        private bool NativeUiOpenedStep()
        {
            CastingWorkspaceScreenView view = BuffPlannerUiRoot.CastingWorkspaceViewForRuntime;
            CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            switch (_nativeUiStep)
            {
                case 0:
                {
                    _nativeUi.PlannerOpened = view != null;
                    if (view == null || session == null) return FinishNativeUi("planner-not-open");
                    string failure = AuthorNativeUiCastings(session);
                    if (failure != null) _nativeUi.Failures.Add(failure);
                    view.RefreshView();
                    NativeUiNext(1);
                    return false;
                }
                case 1:
                {
                    if (!NativeUiWaited(1500)) return false;
                    NativeUiCapture("nui-planner-casting.png");
                    _nativeUi.PlannerTitle = view == null ? null : view.TitleTextForRuntime;
                    if (view != null)
                    {
                        _nativeUi.PlannerVisibleLabels.AddRange(view.VisibleLabelsForRuntime().Distinct().Take(400));
                        _nativeUi.PlannerTextFonts = TextFontSummary(view.RootObject);
                    }
                    _nativeUi.PlannerDocumentBeforePopup = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                    if (view == null || _nativeUiSourceId == null ||
                        !view.RightClickForRuntime("tile:" + _nativeUiSourceId))
                        _nativeUi.Failures.Add("planner-popup-not-invoked");
                    NativeUiNext(2);
                    return false;
                }
                case 2:
                {
                    if (!NativeUiWaited(2000)) return false;
                    DescriptionWindow window = NativeDescriptionWindow();
                    bool native = window != null && window.gameObject.activeInHierarchy;
                    _nativeUi.PlannerPopupPresenter = native ? "native-description-window"
                        : view != null && view.SpellInspectOpen ? "planner-scroll" : "none";
                    _nativeUi.PlannerPopupTitle = native ? DescriptionTitle(window)
                        : view == null ? null : view.SpellInspectTitleForRuntime;
                    NativeUiCapture("nui-planner-popup.png");
                    BuffPlannerUiRoot.HandleEscapeForRuntime();
                    NativeUiNext(3);
                    return false;
                }
                case 3:
                {
                    if (!NativeUiWaited(700)) return false;
                    DescriptionWindow window = NativeDescriptionWindow();
                    bool nativeOpen = window != null && window.gameObject.activeInHierarchy;
                    _nativeUi.PlannerPopupClosedByEscape = !nativeOpen && (view == null || !view.SpellInspectOpen);
                    _nativeUi.PlannerStillOpenAfterPopupEscape = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    _nativeUi.PlannerDocumentAfterPopup = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                    if (nativeOpen) window.CloseWindow();
                    // UI-02: typing in the search is text entry - focus it as a
                    // click does, then read the game's own gate for its hotkeys.
                    if (view != null) view.FocusSearchForRuntime(true);
                    NativeUiNext(12);
                    return false;
                }
                case 12:
                {
                    if (!NativeUiWaited(600)) return false;
                    bool focused = view != null && view.SearchFocusedForRuntime;
                    bool standsDown = NativeKeyboardStandsDown();
                    _nativeUi.SearchFocusSuppressesNativeBindings = focused && standsDown;
                    _nativeUi.AddNote("search-focus:focused=" + focused + ";KeyboardAccess.IsInputFieldSelected=" +
                        standsDown);
                    if (view != null) view.FocusSearchForRuntime(false);
                    NativeUiNext(13);
                    return false;
                }
                case 13:
                {
                    // Case E: one sidebar edit autosaves while the plan file
                    // is held by another reader (an OS sharing lock, as the
                    // C# footer test), so the save fails for real.
                    if (!NativeUiWaited(300)) return false;
                    CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    CastingGraphView graph = live == null ? null
                        : live.BuildGraph(BuffPlannerUiRoot.CastingWorkspaceInputsForRuntime());
                    string casting = graph == null ? null
                        : graph.Castings.Select(value => value.CastingId).FirstOrDefault();
                    if (live == null || casting == null || !File.Exists(live.PlanPathForRuntime))
                    {
                        _nativeUi.AddNote("save-fault:skipped;casting=" + (casting ?? "none"));
                        NativeUiNext(16);
                        return false;
                    }
                    live.FocusGraphCasting(casting);
                    if (view != null) view.RefreshView();
                    _nativeUiDocumentBeforeFault = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                    _nativeUiPlanLock = new FileStream(live.PlanPathForRuntime, FileMode.Open, FileAccess.Read,
                        FileShare.Read);
                    _nativeUi.AddNote("save-fault:locked;edit=" +
                        BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime("FocusedRecastPolicy"));
                    NativeUiNext(14);
                    return false;
                }
                case 14:
                {
                    if (!NativeUiWaited(900)) return false;
                    CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    _nativeUi.SaveFailureFooter = (view == null ? "no-view" : view.FooterSaveTextForRuntime + "|" +
                        view.FooterResultForRuntime) + "|state=" + (live == null ? "none" : live.FooterSaveState);
                    _nativeUi.SaveFailureShown = view != null && view.FooterSaveTextForRuntime == "Not saved" &&
                        view.SaveRecoveryShownForRuntime && live != null &&
                        live.FooterSaveState.StartsWith("save-failed:", StringComparison.Ordinal);
                    NativeUiCapture("nui-save-failed.png");
                    ReleaseNativeUiPlanLock();
                    _nativeUi.AddNote("save-fault:retry=" +
                        BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime("SaveRecovery"));
                    NativeUiNext(15);
                    return false;
                }
                case 15:
                {
                    if (!NativeUiWaited(900)) return false;
                    CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    _nativeUi.SaveRecoveryWorked = live != null && live.FooterSaveState == "saved" &&
                        view != null && view.FooterSaveTextForRuntime == "Saved" && !view.SaveRecoveryShownForRuntime;
                    NativeUiCapture("nui-save-recovered.png");
                    _nativeUi.AddNote("save-fault:undo=" + BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime("Undo"));
                    NativeUiNext(16);
                    return false;
                }
                case 16:
                {
                    if (!NativeUiWaited(900)) return false;
                    CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    _nativeUi.SaveFailureUndoRestored = _nativeUiDocumentBeforeFault != null && live != null &&
                        BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime == _nativeUiDocumentBeforeFault &&
                        live.FooterSaveState == "saved";
                    if (live != null) live.ClearGraphFocus();
                    if (view != null) view.RefreshView();
                    _nativeUiRoutes.Clear();
                    _nativeUiRoutes.AddRange(NativeUiSwitchRoutes);
                    _nativeUiSwitchIndex = 0;
                    NativeUiNext(4);
                    return false;
                }
                case 4:
                {
                    // One window switch: the planner open, then a native window
                    // through the game's own route.
                    if (_nativeUiSwitchIndex >= _nativeUiRoutes.Count)
                    {
                        NativeUiNext(8);
                        return false;
                    }
                    if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen)
                    {
                        if (!_nativeUiClock.IsRunning) BuffPlannerUiRoot.HandlePlannerHotkey();
                        if (!NativeUiWaited(1500)) return false;
                        if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen)
                            return FinishNativeUi("switch-planner-not-reopened:" + _nativeUiRoutes[_nativeUiSwitchIndex]);
                    }
                    string route = _nativeUiRoutes[_nativeUiSwitchIndex];
                    _nativeUiSwitch = new NativeUiWindowSwitch
                    {
                        Route = route,
                        PlannerOpenBefore = BuffPlannerUiRoot.IsCastingWorkspaceOpen,
                        PausedBefore = Game.Instance.IsPaused,
                        SelectedSourceBefore = CurrentSelectedSource()
                    };
                    OpenNativeRoute(route);
                    NativeUiNext(5);
                    return false;
                }
                case 5:
                {
                    if (!NativeUiWaited(2800)) return false;
                    _nativeUiSwitch.PlannerOpenAfter = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    _nativeUiSwitch.InputLeaseHeldAfter = BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
                    _nativeUiSwitch.NativeShownAfter = NativeServiceWindowShown();
                    _nativeUiSwitch.FullScreenModeWhileNative = Game.Instance.IsModeActive(GameModeType.FullScreenUi);
                    _nativeUiSwitch.PausedWhileNative = Game.Instance.IsPaused;
                    _nativeUiSwitch.Capture = "nui-switch-" + Safe(_nativeUiSwitch.Route) + ".png";
                    NativeUiCapture(_nativeUiSwitch.Capture);
                    CloseNativeServiceWindow();
                    NativeUiNext(6);
                    return false;
                }
                case 6:
                {
                    bool released = !NativeServiceWindowShown();
                    if (!released && !NativeUiWaited(8000)) return false;
                    if (!NativeUiWaited(1000)) return false;
                    _nativeUiSwitch.NativeReleasedAfterClose = !NativeServiceWindowShown();
                    _nativeUiSwitch.FullScreenModeAfterClose = Game.Instance.IsModeActive(GameModeType.FullScreenUi) &&
                        !BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    _nativeUiSwitch.SelectionDisabledAfterClose = SelectionManager.Instance != null &&
                        SelectionManager.Instance.IsDisabled && !BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    _nativeUiSwitch.PausedAfterClose = Game.Instance.IsPaused;
                    // The hotkey toggles: reopen only a planner that closed.
                    if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen) BuffPlannerUiRoot.HandlePlannerHotkey();
                    NativeUiNext(7);
                    return false;
                }
                case 7:
                {
                    if (!NativeUiWaited(1500)) return false;
                    _nativeUiSwitch.PlannerReopened = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    _nativeUiSwitch.PlannerRootsAfterReopen = BuffPlannerUiRoot.PlannerRootCountForRuntime();
                    _nativeUiSwitch.SelectedSourceAfterReopen = CurrentSelectedSource();
                    _nativeUi.Switches.Add(_nativeUiSwitch);
                    _nativeUiSwitchIndex++;
                    NativeUiNext(4);
                    return false;
                }
                case 8:
                {
                    // Nested Escape: the description, then the focused
                    // casting, then the planner itself.
                    if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen) return FinishNativeUi("escape-planner-not-open");
                    CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    CastingGraphView graph = live == null ? null
                        : live.BuildGraph(BuffPlannerUiRoot.CastingWorkspaceInputsForRuntime());
                    string casting = graph == null ? null : graph.Castings.Select(value => value.CastingId).FirstOrDefault();
                    if (casting != null) live.FocusGraphCasting(casting);
                    if (view != null) view.RefreshView();
                    if (view == null || _nativeUiSourceId == null || !view.RightClickForRuntime("tile:" + _nativeUiSourceId))
                        _nativeUi.Failures.Add("escape-popup-not-invoked");
                    NativeUiNext(9);
                    return false;
                }
                case 9:
                {
                    if (!NativeUiWaited(1500)) return false;
                    BuffPlannerUiRoot.HandleEscapeForRuntime();
                    DescriptionWindow window = NativeDescriptionWindow();
                    bool popupClosed = !(window != null && window.gameObject.activeInHierarchy) &&
                        (view == null || !view.SpellInspectOpen);
                    bool open = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    BuffPlannerUiRoot.HandleEscapeForRuntime();
                    CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    bool focusCleared = live != null && live.EditingFocusCastingId == null;
                    bool stillOpen = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    _nativeUi.EscapeClosedFocusFirst = popupClosed && open && focusCleared && stillOpen;
                    BuffPlannerUiRoot.HandleEscapeForRuntime();
                    _nativeUi.EscapeClosedPlannerLast = !BuffPlannerUiRoot.IsCastingWorkspaceOpen &&
                        !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
                    _nativeUi.AddNote("escape:popupClosed=" + popupClosed + ";openAfter1=" + open +
                        ";focusCleared=" + focusCleared + ";openAfter2=" + stillOpen);
                    NativeUiNext(10);
                    return false;
                }
                case 10:
                {
                    if (!NativeUiWaited(1000)) return false;
                    _nativeUiLogLinesBefore = NativeUiFacts.GameLogMessages().Count;
                    _nativeUi.GameLogLinesBefore = _nativeUiLogLinesBefore;
                    _nativeUiRunsBefore = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                    _nativeUi.AddNote("log-press:BuffPlannerUiRoot.PressRoutineForRuntime(long);no-os-input");
                    BuffPlannerUiRoot.PressRoutineForRuntime("long");
                    NativeUiNext(11);
                    return false;
                }
                default:
                {
                    if (!NativeUiWaited(3500)) return false;
                    IList<string> lines = NativeUiFacts.GameLogMessages();
                    _nativeUi.GameLogLinesAfter = lines.Count;
                    foreach (string line in lines.Skip(Math.Min(_nativeUiLogLinesBefore, lines.Count)))
                        if (line.IndexOf("Buff Planner", StringComparison.Ordinal) >= 0 ||
                            line.IndexOf("Long", StringComparison.Ordinal) >= 0)
                            _nativeUi.GameLogPlannerLines.Add(line);
                    _nativeUi.RoutinePressRefused = BuffPlannerUiRoot.CastingRunsStartedForRuntime == _nativeUiRunsBefore;
                    NativeUiCapture("nui-gamelog.png");
                    return FinishNativeUi(null);
                }
            }
        }

        private void ReleaseNativeUiPlanLock()
        {
            if (_nativeUiPlanLock == null) return;
            _nativeUiPlanLock.Dispose();
            _nativeUiPlanLock = null;
        }

        private bool FinishNativeUi(string failure)
        {
            ReleaseNativeUiPlanLock();
            if (failure != null) _nativeUi.Failures.Add(failure);
            if (BuffPlannerUiRoot.IsCastingWorkspaceOpen) CloseProbeWorkspace();
            if (NativeServiceWindowShown()) CloseNativeServiceWindow();
            _nativeUi.LeaseReleasedAtEnd = !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
            _nativeUi.FullScreenModeAtEnd = Game.Instance != null &&
                Game.Instance.IsModeActive(GameModeType.FullScreenUi) && !NativeServiceWindowShown();
            _nativeUi.RunsStarted = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
            var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
            JObject record = JObject.FromObject(_nativeUi, JsonSerializer.Create(settings));
            record["violations"] = new JArray(_nativeUi.Violations().Cast<object>().ToArray());
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "native-ui.json"),
                record.ToString(Formatting.Indented) + Environment.NewLine);
            _nativeUiWritten = true;
            _completed = true;
            return true;
        }

        // ---- helpers ------------------------------------------------------

        private static void Pointer<T>(GameObject target, ExecuteEvents.EventFunction<T> handler)
            where T : IEventSystemHandler
        {
            if (target == null || EventSystem.current == null) return;
            RectTransform rect = target.transform as RectTransform;
            float[] screen = NativeUiFacts.ScreenRect(rect);
            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = screen == null ? Vector2.zero
                    : new Vector2(screen[0] + screen[2] / 2f, screen[1] + screen[3] / 2f)
            };
            ExecuteEvents.Execute(target, data, handler);
        }

        private static Button NativeFormationButton(BuffPlannerHudButtonController hud)
        {
            Transform cluster = hud == null ? null : hud.NativeClusterForRuntime;
            Transform formation = cluster == null ? null : cluster.Find("Formation");
            return formation == null ? null : formation.GetComponent<Button>();
        }

        // The native grid cells and the planner's owned cells, measured on
        // screen: size, column alignment and row order.
        private void RecordHudGeometry(BuffPlannerHudButtonController hud)
        {
            RectTransform cluster = hud.NativeClusterForRuntime;
            var native = new List<float[]>();
            foreach (Transform child in cluster)
            {
                if (child == null || !child.gameObject.activeInHierarchy ||
                    child.name == BuffPlannerHudButtonController.RootName) continue;
                if (child.GetComponent<ButtonPF>() == null) continue;
                GridLayoutGroup grid = cluster.GetComponent<GridLayoutGroup>();
                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout) continue;
                float[] rect = NativeUiFacts.ScreenRect(child as RectTransform);
                if (rect != null && grid != null) native.Add(rect);
            }
            _nativeUi.NativeHudButtons = native.Count;
            if (native.Count != 0)
                _nativeUi.NativeHudCell = new[] { native[0][2], native[0][3] };
            var planner = new List<float[]>();
            foreach (string id in new[] { "setup", "long", "important", "short" })
            {
                Button button = hud.ButtonForRuntime(id);
                if (button != null) planner.Add(NativeUiFacts.ScreenRect((RectTransform)button.transform));
            }
            _nativeUi.PlannerHudButtons = planner.Count;
            if (planner.Count != 0) _nativeUi.PlannerHudCell = new[] { planner[0][2], planner[0][3] };
            List<float> columns = native.Select(rect => rect[0]).Distinct().OrderBy(value => value).ToList();
            if (planner.Count == 4 && columns.Count >= 4)
                _nativeUi.PlannerColumnOffsets = planner.Select((rect, index) => rect[0] - columns[index]).ToArray();
            float nativeTop = native.Count == 0 ? 0f : native.Max(rect => rect[1] + rect[3]);
            _nativeUi.PlannerRowAboveNativeRows = planner.Count != 0 && planner.All(rect => rect[1] >= nativeTop - 0.5f);
            int owned = 0;
            if (EventSystem.current != null)
                foreach (string id in new[] { "setup", "long", "important", "short" })
                {
                    Button button = hud.ButtonForRuntime(id);
                    float[] rect = button == null ? null : NativeUiFacts.ScreenRect((RectTransform)button.transform);
                    if (rect == null) continue;
                    var data = new PointerEventData(EventSystem.current)
                    {
                        position = new Vector2(rect[0] + rect[2] / 2f, rect[1] + rect[3] / 2f)
                    };
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(data, hits);
                    if (hits.Count != 0 && hits[0].gameObject != null &&
                        (hits[0].gameObject.transform == button.transform ||
                         hits[0].gameObject.transform.IsChildOf(button.transform))) owned++;
                }
            _nativeUi.HudTopHitOwnedCells = owned;
            string frame = hud.FrameEvidenceForRuntime;
            _nativeUi.PlannerHudSkin = hud.SkinEvidenceForRuntime;
            _nativeUi.PlannerFrameEnclosesRow = frame.IndexOf("encloses=True", StringComparison.Ordinal) >= 0;
            _nativeUi.OldRailHiddenUnderRow = frame.IndexOf("railUnderRow=hidden", StringComparison.Ordinal) >= 0;
            _nativeUi.AddNote("hud-frame:" + frame);
        }

        // Party members with a spellbook first (stable), then one without.
        private static List<UnitEntityData> SpellbookUnitsInOrder()
        {
            var party = Game.Instance == null || Game.Instance.Player == null
                ? new List<UnitEntityData>()
                : Game.Instance.Player.Party.Where(unit => unit != null && unit.Descriptor != null).ToList();
            List<UnitEntityData> casters = party.Where(unit => unit.Descriptor.Spellbooks.Any()).ToList();
            List<UnitEntityData> others = party.Where(unit => !unit.Descriptor.Spellbooks.Any()).ToList();
            return casters.Take(2).Concat(others.Take(1)).ToList();
        }

        private static string UnitLabel(UnitEntityData unit)
        {
            return unit == null ? "null" : unit.CharacterName + "#" + unit.Descriptor.Spellbooks.Count();
        }

        // The service window's own character switch (its party portraits);
        // the world selection follows it.
        private static void SelectUnit(UnitEntityData unit)
        {
            if (unit == null) return;
            Kingmaker.UI.Group.GroupController group = Kingmaker.UI.Group.GroupController.Instance;
            if (group != null) group.SelectUnit(unit);
            else if (unit.View != null && SelectionManager.Instance != null)
                SelectionManager.Instance.SelectUnit(unit.View, true, true, false);
        }

        // Native sprites the planner borrows or matches, previewed by name
        // wherever the campaign canvas holds them (bounded).
        internal static readonly string[] NativeUiPreviewNames =
        {
            "spellbook_frame", "spellbook_frame_back", "spellbook_frame_back2", "map_mask_frame",
            "spellbook_metamagic_decor_2", "button_normal", "button_hover", "button_pressed",
            "main_menu_back", "spellbook_esc", "Inventory_Filter_Icon_Background_Normal",
            "Inventory_Filter_Icon_Background_Selected", "dialogue_backsheet", "back_blockscroll",
            "back_blockscroll_simple", "class_blockframe__no_left_border", "camp_frameportrait",
            "camp_frameportrait_selected", "Separator_left", "Separator_middle", "Separator_right",
            "border_line", "blockscroll_bottom", "CheckMark", "Checkmark", "SliderArea",
            "ScrollBar_Big_Handler", "save_framemini", "Portrait_Frame", "Group_Char_Frame_Normal_Simple",
            "blockframe", "Stat_Line", "Log_Decor_01", "camp_iconpointlist", "globalmap_ok",
            "class_blockframe_#a29069_#", "SlotBackgroundFilled", "Character_Header_01"
        };

        private void WriteNamedPreviews()
        {
            var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (Image image in StaticCanvas.Instance.GetComponentsInChildren<Image>(true))
                if (image != null && image.sprite != null && !sprites.ContainsKey(image.sprite.name) &&
                        Array.IndexOf(NativeUiPreviewNames, image.sprite.name) >= 0)
                    sprites[image.sprite.name] = image.sprite;
            foreach (string missing in NativeUiPreviewNames.Where(name => !sprites.ContainsKey(name)))
                _nativeUi.AddNote("preview-absent:" + missing);
            foreach (string file in NativeUiFacts.Previews(sprites.Values, _request.EvidenceDirectory,
                    NativeUiPreviewNames.Length, _nativeUi.Notes))
                _nativeUi.AddNote("preview:" + file);
        }

        private static Transform SpellbookWindow()
        {
            StaticCanvas canvas = StaticCanvas.Instance;
            return canvas == null || canvas.ServiceWindow == null ? null
                : canvas.ServiceWindow.transform.Find("SpellBook");
        }

        private void RecordSpellbookView(int index)
        {
            Transform spellbook = SpellbookWindow();
            UnitEntityData current = null;
            try { current = Kingmaker.UI.Common.UIUtility.GetCurrentCharacter(); }
            catch (Exception exception) { _nativeUi.AddNote("current-character:" + exception.GetType().Name); }
            var record = new NativeUiSpellbookView
            {
                Unit = current == null ? "unknown" : current.CharacterName,
                Shown = spellbook != null && spellbook.gameObject.activeInHierarchy
            };
            Transform place = spellbook == null ? null : spellbook.Find("Container_Book/MetamagicButtonPlace");
            Selectable metamagic = place == null ? null : place.GetComponentsInChildren<Selectable>(true)
                .FirstOrDefault(value => value != null &&
                    value.name.IndexOf(BuffPlannerSpellbookEntryController.ButtonName, StringComparison.Ordinal) < 0);
            record.MetamagicPresent = metamagic != null;
            record.MetamagicActive = metamagic != null && metamagic.gameObject.activeInHierarchy;
            record.MetamagicInteractable = metamagic != null && metamagic.IsInteractable();
            if (metamagic != null) record.MetamagicRect = NativeUiFacts.ScreenRect((RectTransform)metamagic.transform);
            TMP_Text metamagicLabel = metamagic == null ? null : metamagic.GetComponentInChildren<TMP_Text>(true);
            record.MetamagicLabelFont = metamagicLabel == null || metamagicLabel.font == null ? null
                : metamagicLabel.font.name + "|" + metamagicLabel.fontSize;
            BuffPlannerSpellbookEntryController entry = BuffPlannerUiRoot.SpellbookEntryForRuntime;
            Button owned = entry == null ? null : entry.OwnedButtonForRuntime;
            record.PlannerButtonPresent = owned != null && owned.gameObject.activeInHierarchy;
            record.PlannerButtonInteractable = owned != null && owned.IsInteractable();
            record.PlannerButtonCount = entry == null ? 0 : entry.OwnedButtonCountForRuntime;
            if (owned != null) record.PlannerRect = NativeUiFacts.ScreenRect((RectTransform)owned.transform);
            TMP_Text tmpLabel = owned == null ? null : owned.GetComponentInChildren<TMP_Text>(true);
            Text legacyLabel = owned == null ? null : owned.GetComponentInChildren<Text>(true);
            record.PlannerLabelFont = tmpLabel != null && tmpLabel.font != null ? tmpLabel.font.name + "|" + tmpLabel.fontSize
                : legacyLabel != null && legacyLabel.font != null ? "legacy:" + legacyLabel.font.name + "|" +
                    legacyLabel.fontSize : null;
            record.Capture = "nui-spellbook-" + index + ".png";
            NativeUiCapture(record.Capture);
            // The footer pair as laid out for this character: the native
            // Metamagic place and the planner's place, with their captions.
            NativeUiFacts.Write(_request.EvidenceDirectory, "nui-facts-footer-" + index + ".json", new JObject
            {
                { "native", NativeUiFacts.Subtree(place, "MetamagicButtonPlace") },
                { "planner", NativeUiFacts.Subtree(owned == null ? null : owned.transform.parent,
                    BuffPlannerSpellbookEntryController.FooterPlaceName) }
            });
            _nativeUi.Spellbooks.Add(record);
            // The retired entry sat at the service window's top right, above
            // the top bar; the footer pair's planner button lives in its own
            // footer place on the spellbook.
            _nativeUi.SpellbookTopRightEntryAbsent = owned == null || (owned.transform.parent != null &&
                owned.transform.parent.name == BuffPlannerSpellbookEntryController.FooterPlaceName &&
                owned.transform.parent.parent == spellbook);
        }

        private void WriteSpellbookFacts()
        {
            Transform spellbook = SpellbookWindow();
            if (spellbook == null) { _nativeUi.Failures.Add("spellbook-window-missing"); return; }
            TooltipTrigger trigger = FirstSpellTrigger();
            SpellItemBase item = trigger == null ? null : trigger.GetComponentInParent<SpellItemBase>();
            Transform itemRow = item == null ? null : item.transform;
            _nativeUi.AddNote("spell-row:" + (itemRow == null ? "none" : NativeUiFacts.HierarchyPath(itemRow)));
            var facts = new JObject
            {
                { "footer", NativeUiFacts.Subtree(spellbook.Find("Container_Book/MetamagicButtonPlace"), "MetamagicButtonPlace") },
                { "description", NativeUiFacts.Subtree(spellbook.Find("Container_Book/BookDescription"), "BookDescription") },
                { "spellItem", NativeUiFacts.Subtree(itemRow, "SpellItemRow") },
                { "serviceWindowTop", NativeUiFacts.Subtree(spellbook.parent == null ? null : spellbook.parent.Find("Top"), "Top") }
            };
            NativeUiFacts.Write(_request.EvidenceDirectory, "nui-facts-spellbook.json", facts);
            _nativeUi.FactsFiles.Add("spellbook");
            var sprites = new List<Sprite>();
            sprites.AddRange(NativeUiFacts.Sprites(spellbook.Find("Container_Book/MetamagicButtonPlace")));
            sprites.AddRange(NativeUiFacts.Sprites(itemRow));
            foreach (string file in NativeUiFacts.Previews(sprites, _request.EvidenceDirectory, 32, _nativeUi.Notes))
                _nativeUi.AddNote("preview:" + file);
        }

        // A spell row of the open book page (never the metamagic mixer's or
        // memorizing panel's slots).
        private static TooltipTrigger FirstSpellTrigger()
        {
            Transform spellbook = SpellbookWindow();
            Transform book = spellbook == null ? null : spellbook.Find("Container_Book/Book");
            if (book == null) return null;
            return book.GetComponentsInChildren<TooltipTrigger>(false)
                .FirstOrDefault(trigger => trigger != null && trigger.isActiveAndEnabled &&
                    trigger.GetComponentInParent<SpellItem>() != null);
        }

        // The game's own gate: KeyboardAccess.Tick runs no binding while a
        // TMP_InputField is the selected object (exact 2.1.7b contract,
        // private static IsInputFieldSelected).
        private static bool NativeKeyboardStandsDown()
        {
            MethodInfo gate = typeof(KeyboardAccess).GetMethod("IsInputFieldSelected",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            return gate != null && (bool)gate.Invoke(null, null);
        }

        private static DescriptionWindow NativeDescriptionWindow()
        {
            DescriptionController controller = Game.Instance == null || Game.Instance.UI == null
                ? null : Game.Instance.UI.DescriptionController;
            return controller == null ? null : controller.DescWindow;
        }

        private static string DescriptionTitle(DescriptionWindow window)
        {
            if (window == null) return null;
            TMP_Text title = window.GetComponentsInChildren<TMP_Text>(false)
                .Where(text => text != null && !string.IsNullOrEmpty(text.text))
                .OrderByDescending(text => text.fontSize).FirstOrDefault();
            return title == null ? null : title.text;
        }

        private static void CloseNativeServiceWindow()
        {
            StaticCanvas canvas = StaticCanvas.Instance;
            Transform service = canvas == null || canvas.ServiceWindow == null ? null : canvas.ServiceWindow.transform;
            Transform close = service == null ? null : service.Find("Top/Close");
            Button button = close == null ? null : close.GetComponent<Button>();
            if (button != null) button.onClick.Invoke();
        }

        // The game's own routes: the service-window handler (what the HUD
        // buttons raise) or a key binding's own callback (what the key press
        // runs, delivered without OS input).
        private void OpenNativeRoute(string route)
        {
            if (route.StartsWith("key:", StringComparison.Ordinal))
            {
                string failure = InvokeKeyBinding(route.Substring(4));
                if (failure != null) _nativeUi.Failures.Add("switch-key:" + failure);
                return;
            }
            switch (route)
            {
                case "inventory":
                    EventBus.RaiseEvent<IServiceWindowUIHandler>(handler => handler.HandleOpenInventory());
                    break;
                case "character":
                    EventBus.RaiseEvent<IServiceWindowUIHandler>(handler => handler.HandleOpenCharScreen());
                    break;
                default:
                    EventBus.RaiseEvent<IServiceWindowUIHandler>(handler => handler.HandleOpenSpellbook());
                    break;
            }
        }

        private static string InvokeKeyBinding(string name)
        {
            KeyboardAccess keyboard = Game.Instance == null ? null : Game.Instance.Keyboard;
            if (keyboard == null) return "keyboard-missing";
            KeyboardAccess.Binding binding = keyboard.GetBindingByName(name);
            if (binding == null) return "binding-missing:" + name;
            MethodInfo invoke = typeof(KeyboardAccess).GetMethod("OnCallbackByBinding",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (invoke == null) return "callback-route-missing";
            invoke.Invoke(keyboard, new object[] { binding, true });
            return null;
        }

        private static string CurrentSelectedSource()
        {
            CastingWorkspaceScreenView view = BuffPlannerUiRoot.CastingWorkspaceViewForRuntime;
            return view == null ? null : view.SelectedSourceIdForRuntime;
        }

        // A representative casting state through session commands: one
        // single-target buff both fixture casters know, one caster and
        // source, two recipients, the first casting focused.
        private string AuthorNativeUiCastings(CastingWorkspaceSession session)
        {
            CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceInputsForRuntime();
            if (inputs == null) return "author:no-inputs";
            CastingGraphView view = session.BuildGraph(inputs);
            CastingGraphCatalogueEntry entry = view.Catalogue
                .Where(value => value.Label != null && value.Label.StartsWith("Resistance", StringComparison.Ordinal))
                .FirstOrDefault() ?? view.Catalogue.FirstOrDefault();
            if (entry == null) return "author:empty-catalogue";
            session.SelectGraphBuff(entry.SourceId, inputs);
            _nativeUiSourceId = entry.SourceId;
            view = session.BuildGraph(inputs);
            CastingGraphCasterNode caster = view.Casters.FirstOrDefault(value =>
                !value.IsUnresolved && value.Sources.Any(source => source.Usable));
            if (caster == null) return "author:no-caster:" + entry.Label;
            session.ClickGraphCaster(caster.UnitId, inputs);
            view = session.BuildGraph(inputs);
            CastingGraphCasterNode selected = view.CasterById(caster.UnitId);
            CastingGraphSourceRow row = selected == null ? null : selected.Sources.FirstOrDefault(value => value.Usable);
            if (row != null && !row.Selected) session.ClickGraphSource(row.ProviderKey, inputs);
            view = session.BuildGraph(inputs);
            int added = 0;
            foreach (CastingGraphTargetNode target in view.Targets.Where(value =>
                    value.Legality == CastingGraphTargetLegality.Legal && value.CastingIds.Count == 0).Take(2))
            {
                CastingGraphClickResult result = session.ClickGraphRecipient(target.UnitId, inputs);
                if (result != null && result.Outcome == CastingGraphClickOutcome.Added) added++;
                session.ClearGraphFocus();
            }
            view = session.BuildGraph(inputs);
            string first = view.Castings.Select(value => value.CastingId).FirstOrDefault();
            if (first != null) session.FocusGraphCasting(first);
            _nativeUi.AddNote("author:" + entry.Label + ";caster=" + caster.DisplayName + ";added=" + added +
                ";castings=" + view.Castings.Count + ";direct-session-calls");
            return null;
        }

        private static string TextFontSummary(GameObject root)
        {
            if (root == null) return "none";
            var fonts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
            {
                string key = "tmp:" + (text.font == null ? "null" : text.font.name);
                int count;
                fonts.TryGetValue(key, out count);
                fonts[key] = count + 1;
            }
            foreach (Text text in root.GetComponentsInChildren<Text>(false))
            {
                string key = "legacy:" + (text.font == null ? "null" : text.font.name);
                int count;
                fonts.TryGetValue(key, out count);
                fonts[key] = count + 1;
            }
            return string.Join(";", fonts.Select(pair => pair.Key + "=" + pair.Value).ToArray());
        }

        private static string Safe(string value)
        {
            return new string((value ?? string.Empty).Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        }
    }
}
