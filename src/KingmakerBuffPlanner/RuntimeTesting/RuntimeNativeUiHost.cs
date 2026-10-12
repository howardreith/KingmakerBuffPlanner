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
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;
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
        // The HUD buttons' service-window routes, then the same windows and
        // the journal by their key bindings (as the player has them bound).
        private static readonly string[] NativeUiSwitchRoutes =
        {
            "inventory", "character", "spellbook",
            "key:OpenInventory", "key:OpenChracterScreen", "key:OpenSpells", "key:OpenJournal"
        };
        // UI-01: one spell read in both presenters - the planner's authored
        // buff and the same spell's row in the first caster's book.
        private const string NativeUiComparisonSpell = "Resistance";
        private readonly NativeUiRecord _nativeUi = new NativeUiRecord();
        private int _nativeUiStep;
        private Stopwatch _nativeUiClock = new Stopwatch();
        private bool _nativeUiWritten;
        private NativeUiCaptureBoundary _nativeUiCapture;
        private readonly Stopwatch _nativeUiCaptureClock = new Stopwatch();
        private NativeUiSpellbookView _nativeUiBookView;
        private List<UnitEntityData> _nativeUiUnits;
        private int _nativeUiUnitIndex;
        private int _nativeUiSwitchIndex;
        private NativeUiWindowSwitch _nativeUiSwitch;
        private NativeUiSpellbookView _nativeUiMetamagicView;
        private bool _nativeUiPauseOwned;
        private bool _nativeUiPauseBefore;
        private NativeUiPauseRestoration _nativeUiPauseRestoration;
        private readonly Stopwatch _nativeUiPauseRestoreClock = new Stopwatch();
        private string _nativeUiSourceId;
        private int _nativeUiLogLinesBefore;
        private int _nativeUiRunsBefore;
        private FileStream _nativeUiPlanLock;
        private bool _nativeUiEscapePopupClosed;
        private bool _nativeUiEscapeOpenAfterFirst;
        private List<Func<string>> _nativeUiPlanning;
        private List<string> _nativeUiPlanningNames;
        private int _nativeUiPlanningIndex;
        private string _nativeUiPlanningStart;
        private string _nativeUiDocumentBeforeFault;
        private int _nativeUiFilterIndex;
        private NativeUiCatalogueFilter _nativeUiFilter;
        private static readonly string[] NativeUiFilterCategories = { "Spells", "Abilities", "Other", "All" };
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
            if (_nativeUiCapture != null)
                throw new InvalidOperationException("A native UI capture is already pending.");
            try
            {
                string path = Path.Combine(_request.EvidenceDirectory, file);
                _nativeUiCapture = new NativeUiCaptureBoundary(path, Time.frameCount,
                    WorkspaceEngineCaptureWaitMilliseconds);
                _nativeUiCaptureClock.Restart();
                CaptureScreenshot(path);
            }
            catch (Exception exception)
            {
                _nativeUiCapture = null;
                _nativeUiCaptureClock.Reset();
                _nativeUi.MissingCaptures.Add(file + ":" + exception.GetType().Name);
                throw;
            }
        }

        // Hold the displayed state until a later frame has produced a complete
        // PNG. Even on completion, the next UI action runs in another Update.
        private bool UpdateNativeUiCapture()
        {
            NativeUiCaptureBoundary capture = _nativeUiCapture;
            NativeUiCaptureStatus status = capture.Observe(Time.frameCount,
                _nativeUiCaptureClock.ElapsedMilliseconds);
            if (status == NativeUiCaptureStatus.Pending) return false;
            _nativeUiCapture = null;
            _nativeUiCaptureClock.Reset();
            string file = Path.GetFileName(capture.Path);
            if (status == NativeUiCaptureStatus.TimedOut)
            {
                _nativeUi.MissingCaptures.Add(file + ":capture-timeout");
                return FinishNativeUi("capture-timeout:" + file);
            }
            _nativeUi.Captures.Add(file);
            _nativeUi.AddNote("capture-completed:" + file + ";request-frame=" + capture.RequestFrame +
                ";completed-frame=" + Time.frameCount);
            return false;
        }

        // ---- before the planner opens: HUD, spellbook, native popup -------

        private bool UpdateNativeUiBeforeOpen()
        {
            try
            {
                if (_nativeUiCapture != null) return UpdateNativeUiCapture();
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
                    _nativeUi.PlannerHudIdleHighlightAlpha = new[] { "setup", "long", "important", "short" }
                        .Select(id => HighlightTint(hud.ButtonForRuntime(id)))
                        .Select(tint => tint == null ? 1f : tint[3]).Max();
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
                    _nativeUi.PlannerHudHoverHighlight = HighlightTint(hud.ButtonForRuntime("setup"));
                    NativeUiCapture("nui-hud-hover-planner.png");
                    NativeUiNext(19);
                    return false;
                }
                case 19:
                {
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
                    NativeUiNext(20);
                    return false;
                }
                case 20:
                {
                    Button native = NativeFormationButton(hud);
                    if (native != null) Pointer(native.gameObject, ExecuteEvents.pointerExitHandler);
                    // A press shows only while the pointer is inside the cell
                    // (Selectable.IsPressed): enter, then press.
                    Button longButton = hud.ButtonForRuntime("long");
                    if (longButton != null)
                    {
                        Pointer(longButton.gameObject, ExecuteEvents.pointerEnterHandler);
                        Pointer(longButton.gameObject, ExecuteEvents.pointerDownHandler);
                    }
                    NativeUiNext(4);
                    return false;
                }
                case 4:
                {
                    if (!NativeUiWaited(500)) return false;
                    _nativeUi.PlannerHudPressedHighlight = HighlightTint(hud.ButtonForRuntime("long"));
                    NativeUiCapture("nui-hud-pressed-planner.png");
                    NativeUiNext(21);
                    return false;
                }
                case 21:
                {
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
                    _nativeUiBookView = RecordSpellbookView(_nativeUiUnitIndex);
                    NativeUiNext(14);
                    return false;
                }
                case 14:
                {
                    if (BeginNativeMetamagicObservation(_nativeUiBookView)) return false;
                    return ContinueAfterNativeSpellbookView();
                }
                case 12:
                {
                    if (!NativeUiWaited(1500)) return false;
                    SpellBookController controller = NativeSpellbookController();
                    NativeUiMetamagicAction action = _nativeUiMetamagicView.MetamagicAction;
                    action.ModeWhileOpen = controller != null && NativeMetamagicMode(controller);
                    action.PlannerOpenWhileMetamagic = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    action.Capture = "nui-metamagic-" + _nativeUiUnitIndex + ".png";
                    NativeUiCapture(action.Capture);
                    NativeUiNext(15);
                    return false;
                }
                case 15:
                {
                    NativeUiMetamagicAction action = _nativeUiMetamagicView.MetamagicAction;
                    Button button = NativeMetamagicButton();
                    if (button == null || !button.gameObject.activeInHierarchy || !button.IsInteractable())
                        return FinishNativeUi("metamagic-return-button-unavailable");
                    // The same native footer button returns to the ordinary book.
                    action.ReturnInvoked = true;
                    button.onClick.Invoke();
                    NativeUiNext(13);
                    return false;
                }
                case 13:
                {
                    if (!NativeUiWaited(1500)) return false;
                    SpellBookController controller = NativeSpellbookController();
                    NativeUiMetamagicAction action = _nativeUiMetamagicView.MetamagicAction;
                    action.ModeAfterReturn = controller != null && NativeMetamagicMode(controller);
                    action.ControllerPresentAfterReturn = controller != null;
                    BuffPlannerSpellbookEntryController entry = BuffPlannerUiRoot.SpellbookEntryForRuntime;
                    Button owned = entry == null ? null : entry.OwnedButtonForRuntime;
                    action.PlannerButtonOfferedAfterReturn = owned != null &&
                        owned.gameObject.activeInHierarchy && owned.IsInteractable();
                    NativeUiCapture("nui-metamagic-return-" + _nativeUiUnitIndex + ".png");
                    NativeUiNext(16);
                    return false;
                }
                case 16:
                {
                    // Any failed return is recorded; cleanup restores only the
                    // mode owned by this probe and cannot turn it into a PASS.
                    RestoreNativeUiMetamagicMode();
                    return ContinueAfterNativeSpellbookView();
                }
                case 22:
                {
                    NativeUiUnavailablePlannerAction action = _nativeUiBookView.UnavailablePlannerAction;
                    bool open = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    if (!open && !NativeUiWaited(10000)) return false;
                    if (open && !NativeUiWaited(1500)) return false;
                    action.PlannerOpened = open;
                    action.Capture = "nui-unavailable-metamagic-planner-" + _nativeUiUnitIndex + ".png";
                    NativeUiCapture(action.Capture);
                    NativeUiNext(23);
                    return false;
                }
                case 23:
                {
                    CloseProbeWorkspace();
                    EventBus.RaiseEvent<IServiceWindowUIHandler>(handler => handler.HandleOpenSpellbook());
                    NativeUiNext(24);
                    return false;
                }
                case 24:
                {
                    if (!NativeUiWaited(2800)) return false;
                    NativeUiUnavailablePlannerAction action = _nativeUiBookView.UnavailablePlannerAction;
                    Transform book = SpellbookWindow();
                    action.NativeBookRestored = book != null && book.gameObject.activeInHierarchy;
                    UnitEntityData current = Kingmaker.UI.Common.UIUtility.GetCurrentCharacter();
                    action.UnitIdAfterReturn = current == null ? null : current.UniqueId;
                    Button metamagic = NativeMetamagicButton();
                    action.MetamagicStillUnavailable = metamagic == null ||
                        !metamagic.gameObject.activeInHierarchy || !metamagic.IsInteractable();
                    action.PlannerClosedOnReturn = !BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    action.ReturnCapture = "nui-unavailable-metamagic-return-" + _nativeUiUnitIndex + ".png";
                    NativeUiCapture(action.ReturnCapture);
                    NativeUiNext(25);
                    return false;
                }
                case 25:
                    return ContinueAfterNativeSpellbookView();
                case 11:
                {
                    if (!NativeUiWaited(1500)) return false;
                    return OpenNativeBookPopup();
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
                    NativeUiNext(17);
                    return false;
                }
                case 17:
                {
                    DescriptionWindow window = NativeDescriptionWindow();
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
                        // A full-screen portrait changes only the book's
                        // character. Establish the real world selection before
                        // reopening, so its normal return route has that context.
                        CloseNativeServiceWindow();
                        NativeUiNext(26);
                        return false;
                    }
                    // Back to the first caster; press the owned spellbook
                    // entry exactly as its click delivers it.
                    SelectUnit(_nativeUiUnits[0]);
                    NativeUiNext(8);
                    return false;
                }
                case 26:
                {
                    bool released = !NativeServiceWindowShown() &&
                        !Game.Instance.IsModeActive(GameModeType.FullScreenUi);
                    if (!released && !NativeUiWaited(10000)) return false;
                    if (!released) return FinishNativeUi("spellbook-world-selection-boundary-not-released");
                    SelectUnit(_nativeUiUnits[_nativeUiUnitIndex]);
                    EventBus.RaiseEvent<IServiceWindowUIHandler>(handler => handler.HandleOpenSpellbook());
                    NativeUiNext(5);
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
                    NativeUiNext(18);
                    return false;
                }
                case 18:
                {
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
                if (_nativeUiCapture != null) return UpdateNativeUiCapture();
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
                    _nativeUi.PlannerTitle = view == null ? null : view.TitleTextForRuntime;
                    if (view != null)
                    {
                        _nativeUi.PlannerVisibleLabels.AddRange(view.VisibleLabelsForRuntime().Distinct().Take(400));
                        _nativeUi.PlannerTextFonts = TextFontSummary(view.RootObject);
                        Canvas.ForceUpdateCanvases();
                        _nativeUi.SourceTabLayoutEvidence = view.SourceTabFitForRuntime;
                        foreach (TMP_Text caption in view.SourceTabCaptionsForRuntime)
                            _nativeUi.SourceTabs.Add(new NativeUiCaptionFit
                            {
                                Caption = caption.text,
                                Size = caption.fontSize,
                                TextWidth = caption.GetPreferredValues(caption.text,
                                    float.PositiveInfinity, float.PositiveInfinity).x,
                                AvailableWidth = caption.rectTransform.rect.width
                            });
                    }
                    _nativeUi.PlannerDocumentBeforePopup = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                    NativeUiCapture("nui-planner-casting.png");
                    NativeUiNext(34);
                    return false;
                }
                case 34:
                {
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
                    _nativeUi.PopupSameSpell = NativeUiRecord.SameDescriptionTitle(_nativeUi.NativePopupTitle,
                        _nativeUi.PlannerPopupTitle);
                    NativeUiCapture("nui-planner-popup.png");
                    NativeUiNext(27);
                    return false;
                }
                case 27:
                {
                    BuffPlannerUiRoot.HandleEscapeForRuntime();
                    NativeUiNext(3);
                    return false;
                }
                case 3:
                {
                    if (!NativeUiWaited(700)) return false;
                    DescriptionWindow window = NativeDescriptionWindow();
                    bool nativeOpen = NativeDescriptionPresenter.IsOpen;
                    _nativeUi.PlannerPopupClosedByEscape = !nativeOpen && (view == null || !view.SpellInspectOpen);
                    _nativeUi.PlannerStillOpenAfterPopupEscape = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    _nativeUi.PlannerDocumentAfterPopup = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                    if (nativeOpen) window.CloseWindow();
                    // Once more, closed by the window's own close button.
                    if (view == null || _nativeUiSourceId == null || !view.RightClickForRuntime("tile:" + _nativeUiSourceId))
                        _nativeUi.Failures.Add("close-button-popup-not-invoked");
                    NativeUiNext(21);
                    return false;
                }
                case 21:
                {
                    if (!NativeUiWaited(1500)) return false;
                    DescriptionWindow window = NativeDescriptionWindow();
                    Button close = window == null ? null : window.GetComponentsInChildren<Button>(false)
                        .FirstOrDefault(button => button.name == "Close" && button.IsInteractable());
                    ScrollRect scroll = window == null ? null : window.Scroll;
                    _nativeUi.AddNote("popup-scroll:" + (scroll == null || scroll.content == null || scroll.viewport == null
                        ? "none" : "content=" + scroll.content.rect.height.ToString("0") + ";viewport=" +
                            scroll.viewport.rect.height.ToString("0")));
                    _nativeUi.PlannerPopupCloseButtonFound = close != null && NativeDescriptionPresenter.IsOpen;
                    if (close != null) Pointer(close.gameObject, ExecuteEvents.pointerClickHandler);
                    NativeUiNext(22);
                    return false;
                }
                case 22:
                {
                    if (!NativeUiWaited(700)) return false;
                    _nativeUi.PlannerPopupClosedByCloseButton = _nativeUi.PlannerPopupCloseButtonFound &&
                        !NativeDescriptionPresenter.IsOpen && BuffPlannerUiRoot.IsCastingWorkspaceOpen &&
                        BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime == _nativeUi.PlannerDocumentBeforePopup;
                    DescriptionWindow window = NativeDescriptionWindow();
                    if (NativeDescriptionPresenter.IsOpen && window != null) window.CloseWindow();
                    // Inspect the actual casting card through its existing
                    // right-click callback, without selecting or editing it.
                    CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    CastingGraphView graph = live == null ? null
                        : live.BuildGraph(BuffPlannerUiRoot.CastingWorkspaceInputsForRuntime());
                    CastingGraphCasting casting = graph == null ? null : graph.Castings.FirstOrDefault();
                    _nativeUi.CastingPopup = new NativeUiCastingPopup
                    {
                        CastingId = casting == null ? null : casting.CastingId,
                        ExpectedCasterUnitId = casting == null ? null : casting.CasterUnitId,
                        ExpectedAbilityGuid = casting == null || casting.SpellAbility == null ? null
                            : string.IsNullOrEmpty(casting.SpellAbility.VariantGuid)
                                ? casting.SpellAbility.BaseAbilityGuid : casting.SpellAbility.VariantGuid,
                        ExpectedTitle = casting == null ? null : casting.SpellTitle,
                        DocumentBefore = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime,
                        Capture = "nui-casting-card-popup.png"
                    };
                    if (casting == null || view == null || !view.RightClickForRuntime("chip:" + casting.CastingId))
                        _nativeUi.Failures.Add("casting-popup-not-invoked");
                    NativeUiNext(23);
                    return false;
                }
                case 23:
                {
                    if (!NativeUiWaited(1500)) return false;
                    NativeUiCastingPopup popup = _nativeUi.CastingPopup;
                    DescriptionWindow window = NativeDescriptionWindow();
                    popup.Presenter = NativeDescriptionPresenter.IsOpen ? "native-description-window" : "none";
                    popup.ActualTitle = window == null ? null : DescriptionTitle(window);
                    popup.ActualCasterUnitId = view == null ? null : view.SpellInspectCasterForRuntime;
                    popup.ActualAbilityGuid = view == null ? null : view.SpellInspectAbilityForRuntime;
                    NativeUiCapture(popup.Capture);
                    NativeUiNext(28);
                    return false;
                }
                case 28:
                {
                    BuffPlannerUiRoot.HandleEscapeForRuntime();
                    NativeUiNext(24);
                    return false;
                }
                case 24:
                {
                    if (!NativeUiWaited(700)) return false;
                    _nativeUi.CastingPopup.ClosedByEscape = !NativeDescriptionPresenter.IsOpen &&
                        BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    _nativeUi.CastingPopup.DocumentAfter = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                    // Clean up a failed popup before proceeding, retaining its failure.
                    NativeDescriptionPresenter.CloseIfOpen();
                    _nativeUiFilterIndex = 0;
                    NativeUiNext(25);
                    return false;
                }
                case 25:
                {
                    if (_nativeUiFilterIndex >= NativeUiFilterCategories.Length)
                    {
                        // UI-02: read the game's own hotkey gate with search focused.
                        if (view != null) view.FocusSearchForRuntime(true);
                        NativeUiNext(12);
                        return false;
                    }
                    string category = NativeUiFilterCategories[_nativeUiFilterIndex];
                    _nativeUiFilter = new NativeUiCatalogueFilter
                    {
                        Category = category,
                        DocumentBefore = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime,
                        Capture = "nui-catalogue-" + category.ToLowerInvariant() + ".png"
                    };
                    string result = BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime("SourceTab." + category);
                    if (result != "invoked") _nativeUi.Failures.Add("catalogue-filter:" + category + ":" + result);
                    NativeUiNext(26);
                    return false;
                }
                case 26:
                {
                    if (!NativeUiWaited(350)) return false;
                    CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    CastingGraphView graph = live == null ? null
                        : live.BuildGraph(BuffPlannerUiRoot.CastingWorkspaceInputsForRuntime());
                    PlannerSourceCategory category = (PlannerSourceCategory)Enum.Parse(typeof(PlannerSourceCategory),
                        _nativeUiFilter.Category);
                    // The selected entry intentionally remains listed in every filter.
                    _nativeUiFilter.ExpectedSourceIds = graph == null ? null : graph.Catalogue
                        .Where(entry => entry.Selected || (WorkspaceSourceLabels.MatchesCategory(entry.Source, category) &&
                            WorkspaceSourceLabels.Matches(entry.Source, view == null ? null : view.SearchTextForRuntime)))
                        .Select(entry => entry.SourceId).ToArray();
                    _nativeUiFilter.VisibleSourceIds = view == null ? null : view.CatalogueSourceIdsForRuntime;
                    _nativeUiFilter.DocumentAfter = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                    NativeUiCapture(_nativeUiFilter.Capture);
                    _nativeUi.CatalogueFilters.Add(_nativeUiFilter);
                    _nativeUiFilterIndex++;
                    NativeUiNext(25);
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
                    NativeUiNext(29);
                    return false;
                }
                case 29:
                {
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
                    NativeUiNext(30);
                    return false;
                }
                case 30:
                {
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
                    BuildPlanningScript();
                    NativeUiNext(20);
                    return false;
                }
                case 20:
                {
                    // Case C: one planning click per step, at a player's pace.
                    if (!NativeUiWaited(350)) return false;
                    if (_nativeUiPlanning != null && _nativeUiPlanningIndex < _nativeUiPlanning.Count)
                    {
                        string name = _nativeUiPlanningNames[_nativeUiPlanningIndex];
                        string failure;
                        try { failure = _nativeUiPlanning[_nativeUiPlanningIndex](); }
                        catch (Exception exception) { failure = "threw:" + exception.GetType().Name + ":" + exception.Message; }
                        _nativeUi.PlanningSteps.Add(name + ":" + (failure ?? "ok"));
                        if (name == "add") NativeUiCapture("nui-planning-added.png");
                        if (name == "done") NativeUiCapture("nui-planning-no-selection.png");
                        if (name == "change-caster") NativeUiCapture("nui-planning-changed-caster.png");
                        _nativeUiPlanningIndex++;
                        if (failure == null) { NativeUiNext(20); return false; }
                    }
                    NativeUiNext(33);
                    return false;
                }
                case 33:
                {
                    _nativeUi.PlanningRestored = _nativeUiPlanningStart != null &&
                        BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime == _nativeUiPlanningStart;
                    CastingWorkspaceSession after = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    if (after != null) after.ClearGraphFocus();
                    if (view != null) view.RefreshView();
                    _nativeUiRoutes.Clear();
                    _nativeUiRoutes.AddRange(NativeUiSwitchRoutes);
                    _nativeUiRoutes.Add("inventory");
                    _nativeUiRoutes.Add("key:OpenJournal");
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
                        if (_nativeUiPauseOwned)
                        {
                            _nativeUiPauseRestoration = new NativeUiPauseRestoration(_nativeUiPauseBefore,
                                WorkspaceEngineCaptureWaitMilliseconds);
                            _nativeUiPauseRestoreClock.Restart();
                            CloseProbeWorkspace();
                            NativeUiNext(37);
                        }
                        else NativeUiNext(8);
                        return false;
                    }
                    // Installed 2.1.7b refuses starting Pause over FullScreenUi.
                    // Close our window first, pause in the native world mode,
                    // then reopen it over the established pause state.
                    if (_nativeUiSwitchIndex >= NativeUiSwitchRoutes.Length && !_nativeUiPauseOwned)
                    {
                        CloseProbeWorkspace();
                        NativeUiNext(35);
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
                        PauseRequested = _nativeUiSwitchIndex >= NativeUiSwitchRoutes.Length,
                        PlannerOpenBefore = BuffPlannerUiRoot.IsCastingWorkspaceOpen,
                        PausedBefore = Game.Instance.IsPaused,
                        SelectedSourceBefore = CurrentSelectedSource()
                    };
                    OpenNativeRoute(route);
                    NativeUiNext(5);
                    return false;
                }
                case 35:
                {
                    bool released = !NativeServiceWindowShown() &&
                        !BuffPlannerUiRoot.IsCastingWorkspaceOpen &&
                        !Game.Instance.IsModeActive(GameModeType.FullScreenUi);
                    if (!released && !NativeUiWaited(10000)) return false;
                    if (!released || (Game.Instance.CurrentMode != GameModeType.Default && !Game.Instance.IsPaused))
                        return FinishNativeUi("deliberate-pause-world-boundary-not-released");
                    _nativeUiPauseBefore = Game.Instance.IsPaused;
                    _nativeUiPauseOwned = true;
                    Game.Instance.IsPaused = true;
                    NativeUiNext(36);
                    return false;
                }
                case 36:
                {
                    if (!Game.Instance.IsPaused && !NativeUiWaited(10000)) return false;
                    if (!Game.Instance.IsPaused) return FinishNativeUi("deliberate-pause-did-not-activate");
                    _nativeUi.AddNote("pause:native-Game.IsPaused-setter-in-world-mode;no-os-input");
                    NativeUiNext(4);
                    return false;
                }
                case 37:
                {
                    NativeUiPauseRestoreStatus status = _nativeUiPauseRestoration.Observe(
                        NativeUiPauseWorldReleased(), Game.Instance.IsPaused,
                        _nativeUiPauseRestoreClock.ElapsedMilliseconds, value => Game.Instance.IsPaused = value);
                    if (status == NativeUiPauseRestoreStatus.Pending) return false;
                    if (status == NativeUiPauseRestoreStatus.TimedOut)
                        return FinishNativeUi("pause-restoration-boundary-timed-out");
                    _nativeUi.OwnedPauseRestored = Game.Instance.IsPaused == _nativeUiPauseBefore;
                    _nativeUiPauseOwned = false;
                    _nativeUi.AddNote("pause:original-state-observed-after-world-release;frame=" + Time.frameCount);
                    BuffPlannerUiRoot.HandlePlannerHotkey();
                    NativeUiNext(38);
                    return false;
                }
                case 38:
                {
                    if (!NativeUiWaited(1500)) return false;
                    if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen)
                        return FinishNativeUi("pause-restoration-planner-not-reopened");
                    NativeUiNext(8);
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
                    _nativeUiSwitch.Capture = "nui-switch-" + Safe(_nativeUiSwitch.Route) +
                        (_nativeUiSwitch.PauseRequested ? "-paused" : "") + ".png";
                    NativeUiCapture(_nativeUiSwitch.Capture);
                    NativeUiNext(31);
                    return false;
                }
                case 31:
                {
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
                    // Three Escape presses at a player's pace: the description,
                    // then the focused casting, then the planner itself.
                    if (!NativeUiWaited(1500)) return false;
                    BuffPlannerUiRoot.HandleEscapeForRuntime();
                    NativeUiNext(17);
                    return false;
                }
                case 17:
                {
                    if (!NativeUiWaited(700)) return false;
                    _nativeUiEscapePopupClosed = !NativeDescriptionPresenter.IsOpen &&
                        (view == null || !view.SpellInspectOpen);
                    _nativeUiEscapeOpenAfterFirst = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    BuffPlannerUiRoot.HandleEscapeForRuntime();
                    NativeUiNext(18);
                    return false;
                }
                case 18:
                {
                    if (!NativeUiWaited(700)) return false;
                    CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                    bool focusCleared = live != null && live.EditingFocusCastingId == null;
                    bool stillOpen = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                    _nativeUi.EscapeClosedFocusFirst = _nativeUiEscapePopupClosed && _nativeUiEscapeOpenAfterFirst &&
                        focusCleared && stillOpen;
                    _nativeUi.AddNote("escape:popupClosed=" + _nativeUiEscapePopupClosed + ";openAfter1=" +
                        _nativeUiEscapeOpenAfterFirst + ";focusCleared=" + focusCleared + ";openAfter2=" + stillOpen);
                    BuffPlannerUiRoot.HandleEscapeForRuntime();
                    NativeUiNext(19);
                    return false;
                }
                case 19:
                {
                    if (!NativeUiWaited(700)) return false;
                    _nativeUi.EscapeClosedPlannerLast = !BuffPlannerUiRoot.IsCastingWorkspaceOpen &&
                        !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
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
                    NativeUiNext(32);
                    return false;
                }
                case 32:
                {
                    return FinishNativeUi(null);
                }
            }
        }

        // Case C: the planning clicks, each a named control's own click
        // callback (as BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime
        // delivers it), each checked against the plan it should leave.
        private void BuildPlanningScript()
        {
            _nativeUiPlanning = new List<Func<string>>();
            _nativeUiPlanningNames = new List<string>();
            _nativeUiPlanningIndex = 0;
            _nativeUiPlanningStart = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
            CastingWorkspaceSession live = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            CastingGraphView graph = live == null ? null
                : live.BuildGraph(BuffPlannerUiRoot.CastingWorkspaceInputsForRuntime());
            if (graph == null || graph.Castings.Count == 0)
            {
                _nativeUi.AddNote("planning:no-castings");
                return;
            }
            CastingGraphCasting first = graph.Castings[0];
            string originalTarget = first.TargetUnitId;
            CastingGraphTargetNode free = graph.Targets.FirstOrDefault(value => value.CastingIds.Count == 0 &&
                graph.Castings.All(casting => casting.TargetUnitId != value.UnitId));
            CastingGraphCasterNode other = graph.Casters.FirstOrDefault(value => !value.IsUnresolved &&
                value.UnitId != first.CasterUnitId && value.Sources.Count != 0);
            CastingGraphCasterNode own = graph.Casters.FirstOrDefault(value => value.UnitId == first.CasterUnitId);
            if (free == null || other == null || own == null)
            {
                _nativeUi.AddNote("planning:fixture-shape;free=" + (free != null) + ";other=" + (other != null));
                return;
            }
            int startCount = live.Document.Castings.Count;
            string id = first.CastingId;
            string added = null;
            Func<string, PlannedCasting> planned = value => BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime()
                .Document.Castings.FirstOrDefault(item => item.CastingId == value);
            Func<string, string> click = name =>
            {
                string result = BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime(name);
                return result == "invoked" ? null : name + "=" + result;
            };
            Action<string, Func<string>> step = (name, body) =>
            {
                _nativeUiPlanningNames.Add(name);
                _nativeUiPlanning.Add(body);
            };
            step("focus", () => click("Casting." + id) ??
                (BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime().EditingFocusCastingId == id ? null : "not-focused"));
            step("retarget", () => click("Target." + free.UnitId) ??
                (planned(id).DirectTargetUnitId == free.UnitId ? null : "target=" + planned(id).DirectTargetUnitId));
            step("undo-retarget", () => click("Undo") ??
                (planned(id).DirectTargetUnitId == originalTarget ? null : "target=" + planned(id).DirectTargetUnitId));
            int order = 0;
            step("cast-later", () =>
            {
                order = planned(id).Order;
                return click("Casting." + id) ?? click("FocusedOrder.Later") ??
                    (planned(id).Order == order + 1 ? null : "order=" + planned(id).Order + "<-" + order);
            });
            step("undo-order", () => click("Undo") ?? (planned(id).Order == order ? null : "order=" + planned(id).Order));
            step("done", () => click("DoneEditing") ??
                (BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime().EditingFocusCastingId == null ? null : "still-focused"));
            step("choose-source", () => click("Caster." + other.UnitId) ?? click("Provider." + other.UnitId + ".0"));
            step("add", () =>
            {
                string failure = click("Target." + free.UnitId);
                if (failure != null) return failure;
                CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                if (session.Document.Castings.Count != startCount + 1) return "count=" + session.Document.Castings.Count;
                added = session.EditingFocusCastingId;
                PlannedCasting created = added == null ? null : planned(added);
                return created != null && created.CasterUnitId == other.UnitId && created.DirectTargetUnitId == free.UnitId
                    ? null : "added=" + (created == null ? "none" : created.CasterUnitId + ">" + created.DirectTargetUnitId);
            });
            step("change-caster", () => click("Provider." + own.UnitId + ".0") ??
                (added != null && planned(added).CasterUnitId == own.UnitId ? null : "caster=" +
                    (added == null ? "none" : planned(added).CasterUnitId)));
            step("undo-caster", () => click("Undo") ??
                (added != null && planned(added).CasterUnitId == other.UnitId ? null : "caster-not-restored"));
            step("remove", () => (added == null ? "no-added" : click("Casting." + added)) ?? click("Target." + free.UnitId) ??
                (added != null && planned(added) == null ? null : "not-removed"));
            step("undo-remove", () => click("Undo") ?? (added != null && planned(added) != null ? null : "not-restored"));
            step("undo-add", () => click("Undo") ??
                (BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime().Document.Castings.Count == startCount
                    ? null : "count=" + BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime().Document.Castings.Count));
        }

        private void ReleaseNativeUiPlanLock()
        {
            if (_nativeUiPlanLock == null) return;
            _nativeUiPlanLock.Dispose();
            _nativeUiPlanLock = null;
        }

        private bool FinishNativeUi(string failure)
        {
            ReleaseNativeUiProbes();
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

        // An effect observation after the already approved zero-cost qualification.
        // Its own receipt distinguishes missing active UI state from casting
        // success. Temporary plan copies are undone; native effects are never
        // forced. The driver has completed before this probe opens a window.
        private int _qualifiedUiStep;
        private readonly Stopwatch _qualifiedUiClock = new Stopwatch();
        private NativeUiCaptureBoundary _qualifiedUiCapture;
        private readonly Stopwatch _qualifiedUiCaptureClock = new Stopwatch();
        private readonly JObject _qualifiedUiObservation = new JObject();
        private bool _qualifiedUiOpened;
        private int _qualifiedUiRunsBefore;
        private string _qualifiedUiDocument;
        private CastingWorkspaceSession _qualifiedUiSession;
        private int _qualifiedUiEdits;
        private GameObject _qualifiedUiHovered;

        private bool ObserveNativeUiAfterQualification()
        {
            try
            {
                if (_qualifiedUiCapture != null)
                {
                    NativeUiCaptureStatus status = _qualifiedUiCapture.Observe(Time.frameCount,
                        _qualifiedUiCaptureClock.ElapsedMilliseconds);
                    if (status == NativeUiCaptureStatus.Pending) return false;
                    if (status == NativeUiCaptureStatus.TimedOut)
                        throw new IOException("Post-qualification capture exceeded the ten-second boundary: " +
                            Path.GetFileName(_qualifiedUiCapture.Path));
                    ((JArray)_qualifiedUiObservation["captures"]).Add(new JObject
                    {
                        { "file", Path.GetFileName(_qualifiedUiCapture.Path) },
                        { "requestFrame", _qualifiedUiCapture.RequestFrame }, { "completedFrame", Time.frameCount }
                    });
                    _qualifiedUiCapture = null;
                    // The next UI action belongs to a subsequent Update.
                    return false;
                }
                CastingWorkspaceScreenView view = BuffPlannerUiRoot.CastingWorkspaceViewForRuntime;
                switch (_qualifiedUiStep)
                {
                    case 0:
                        _qualifiedUiObservation["schemaVersion"] = 2;
                        _qualifiedUiObservation["runId"] = _request.RunId;
                        _qualifiedUiObservation["sourceId"] = _qualificationRecord.Selection.SourceId;
                        _qualifiedUiObservation["screenWidth"] = Screen.width;
                        _qualifiedUiObservation["screenHeight"] = Screen.height;
                        _qualifiedUiObservation["captures"] = new JArray();
                        _qualifiedUiObservation["dispatch"] = "temporary-plan-copies-and-Undo;no-os-input;no-Apply";
                        _qualifiedUiObservation["visualReview"] = "NOT_REVIEWED";
                        _qualifiedUiRunsBefore = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                        _qualifiedUiOpened = !BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                        if (_qualifiedUiOpened) BuffPlannerUiRoot.HandlePlannerHotkey();
                        _qualifiedUiStep = 1;
                        _qualifiedUiClock.Restart();
                        return false;
                    case 1:
                        if (_qualifiedUiClock.ElapsedMilliseconds < 1500) return false;
                        if (!BuffPlannerUiRoot.IsCastingWorkspaceOpen || view == null)
                            throw new InvalidOperationException("Post-qualification planner did not open.");
                        CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                        CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
                        _qualifiedUiSession = session;
                        _qualifiedUiDocument = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                        _qualifiedUiObservation["documentBefore"] = _qualifiedUiDocument;
                        foreach (PlannedCasting casting in _qualificationRecord.Selection.Castings)
                        {
                            if (session.Document.Castings.Any(value => value.CastingId == casting.CastingId)) continue;
                            AuthoringEditResult added = session.AddCastingForRuntime(casting);
                            if (!added.Applied) throw new InvalidOperationException("Active probe plan copy refused: " + added.Reason);
                            _qualifiedUiEdits++;
                        }
                        session.SelectGraphBuff(_qualificationRecord.Selection.SourceId, inputs);
                        CastingGraphView graph = session.BuildGraph(inputs);
                        string first = graph.Castings.Select(value => value.CastingId).FirstOrDefault();
                        if (first != null) session.FocusGraphCasting(first);
                        view.RefreshView();
                        _qualifiedUiObservation["temporaryCopies"] = _qualifiedUiEdits;
                        _qualifiedUiStep = 2;
                        _qualifiedUiClock.Restart();
                        return false;
                    case 2:
                        if (_qualifiedUiClock.ElapsedMilliseconds < 1500) return false;
                        _qualifiedUiObservation["castings"] = new JArray(view.LastGraphForRuntime.Castings
                            .Select(value => NativeUiStateJson.Casting(value.CastingId, value.CasterUnitId,
                                value.TargetUnitId, value.StatusLabel, value.Readiness.ToString(),
                                value.SourceProviderKey)).ToArray());
                        _qualifiedUiObservation["visibleLabels"] = JArray.FromObject(view.VisibleLabelsForRuntime());
                        _qualifiedUiObservation["nativeGameLogLines"] = JArray.FromObject(NativeUiFacts.GameLogMessages()
                            .Where(line => line.IndexOf("Buff Planner:", StringComparison.Ordinal) >= 0).ToArray());
                        RequestQualifiedUiCapture("nui-qualified-active.png");
                        _qualifiedUiStep = 3;
                        return false;
                    case 3:
                        string recastId = _qualificationRecord.Selection.Castings[0].CastingId;
                        _qualifiedUiSession.FocusGraphCasting(recastId);
                        if (_qualifiedUiSession.Document.Castings.First(value => value.CastingId == recastId)
                            .ExistingEffectPolicy != ExistingEffectPolicy.Overwrite)
                        {
                            AuthoringEditResult edited = _qualifiedUiSession.SetFocusedRecastPolicy(ExistingEffectPolicy.Overwrite);
                            if (!edited.Applied) throw new InvalidOperationException("Active probe recast edit refused: " + edited.Reason);
                            _qualifiedUiEdits++;
                        }
                        view.RefreshView();
                        _qualifiedUiStep = 8;
                        _qualifiedUiClock.Restart();
                        return false;
                    case 8:
                        if (_qualifiedUiClock.ElapsedMilliseconds < 1500) return false;
                        _qualifiedUiObservation["recastLabels"] = JArray.FromObject(view.VisibleLabelsForRuntime());
                        _qualifiedUiObservation["temporaryEdits"] = _qualifiedUiEdits;
                        RequestQualifiedUiCapture("nui-qualified-recast.png");
                        _qualifiedUiStep = 9;
                        return false;
                    case 9:
                        _qualifiedUiHovered = view.RootObject.GetComponentsInChildren<Button>(false)
                            .Where(button => button.name.StartsWith("Casting.", StringComparison.Ordinal) &&
                                button.IsInteractable()).Select(button => button.gameObject).FirstOrDefault();
                        if (_qualifiedUiHovered != null)
                            Pointer(_qualifiedUiHovered, ExecuteEvents.pointerEnterHandler);
                        _qualifiedUiObservation["hoverDispatch"] = "Unity-pointer-callback;no-os-input";
                        _qualifiedUiObservation["hoveredCard"] = _qualifiedUiHovered == null ? null : _qualifiedUiHovered.name;
                        _qualifiedUiStep = 4;
                        _qualifiedUiClock.Restart();
                        return false;
                    case 4:
                        if (_qualifiedUiClock.ElapsedMilliseconds < 800) return false;
                        _qualifiedUiObservation["hoverTargetStillPresentAtRequest"] = _qualifiedUiHovered != null &&
                            _qualifiedUiHovered.activeInHierarchy;
                        RequestQualifiedUiCapture("nui-qualified-active-hover.png");
                        _qualifiedUiStep = 5;
                        return false;
                    case 5:
                        RestoreQualifiedUiPlan();
                        _qualifiedUiObservation["documentAfter"] = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                        _qualifiedUiObservation["originalRootPlanRestored"] = !string.IsNullOrEmpty(_qualifiedUiDocument) &&
                            _qualifiedUiDocument == BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                        _qualifiedUiObservation["runsBefore"] = _qualifiedUiRunsBefore;
                        _qualifiedUiObservation["runsAfter"] = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                        _qualifiedUiObservation["noAdditionalRun"] =
                            _qualifiedUiRunsBefore == BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                        if (!(bool)_qualifiedUiObservation["originalRootPlanRestored"] ||
                            !(bool)_qualifiedUiObservation["noAdditionalRun"])
                            throw new InvalidOperationException("The active probe did not restore the original root plan or started a run.");
                        ReleaseQualifiedUiWindow();
                        _qualifiedUiStep = 6;
                        _qualifiedUiClock.Restart();
                        return false;
                    case 6:
                        if (_qualifiedUiClock.ElapsedMilliseconds < 800) return false;
                        _qualifiedUiObservation["leaseReleased"] =
                            !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
                        if (!(bool)_qualifiedUiObservation["leaseReleased"])
                            throw new InvalidOperationException("Active probe did not release its input lease.");
                        // Read and picture the actual gameplay log after the
                        // owned planner has closed, not behind its parchment.
                        _qualifiedUiObservation["gameplayVisibleAtLogCapture"] = NativeUiPauseWorldReleased();
                        if (!(bool)_qualifiedUiObservation["gameplayVisibleAtLogCapture"])
                            throw new InvalidOperationException("The gameplay log is still behind a native window.");
                        _qualifiedUiObservation["nativeGameLogAfterClose"] = JArray.FromObject(
                            NativeUiFacts.GameLogMessages().Where(line =>
                                line.IndexOf("Buff Planner:", StringComparison.Ordinal) >= 0).ToArray());
                        RequestQualifiedUiCapture("nui-qualified-game-log.png");
                        _qualifiedUiStep = 7;
                        return false;
                    case 7:
                        // Capture completion already held this state through a
                        // later frame. No Apply or other game action follows it.
                        _qualifiedUiObservation["gameplayVisibleAfterLogCapture"] = NativeUiPauseWorldReleased();
                        if (!(bool)_qualifiedUiObservation["gameplayVisibleAfterLogCapture"])
                            throw new InvalidOperationException("The gameplay log capture lost its native window state.");
                        _qualifiedUiObservation["runsAfter"] = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                        _qualifiedUiObservation["noAdditionalRun"] =
                            _qualifiedUiRunsBefore == BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                        _qualifiedUiObservation["leaseReleased"] =
                            !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
                        if (!(bool)_qualifiedUiObservation["noAdditionalRun"] ||
                            !(bool)_qualifiedUiObservation["leaseReleased"])
                            throw new InvalidOperationException("Gameplay log capture changed the run count or retained input.");
                        WriteQualifiedUiObservation(null);
                        _qualifiedUiStep = 10;
                        return true;
                    default: return true;
                }
            }
            catch (Exception exception)
            {
                _qualificationRecord.Failures.Add("native-ui-active-observation:" + exception.Message);
                try { ReleaseQualifiedUiWindow(); }
                finally { WriteQualifiedUiObservation(exception.Message); }
                _qualifiedUiStep = 10;
                return true;
            }
        }

        private void RequestQualifiedUiCapture(string name)
        {
            string path = Path.Combine(_request.EvidenceDirectory, name);
            _qualifiedUiCapture = new NativeUiCaptureBoundary(path, Time.frameCount,
                WorkspaceEngineCaptureWaitMilliseconds);
            _qualifiedUiCaptureClock.Restart();
            CaptureScreenshot(path);
        }

        private void RestoreQualifiedUiPlan()
        {
            while (_qualifiedUiEdits > 0)
            {
                if (_qualifiedUiSession == null || !_qualifiedUiSession.Undo())
                    throw new InvalidOperationException("Active probe Undo did not restore its owned edit.");
                _qualifiedUiEdits--;
            }
        }

        private void ReleaseQualifiedUiWindow()
        {
            try { RestoreQualifiedUiPlan(); }
            finally
            {
                if (_qualifiedUiHovered != null) Pointer(_qualifiedUiHovered, ExecuteEvents.pointerExitHandler);
                _qualifiedUiHovered = null;
                if (_qualifiedUiOpened && BuffPlannerUiRoot.IsCastingWorkspaceOpen) CloseProbeWorkspace();
                _qualifiedUiOpened = false;
            }
        }

        private void WriteQualifiedUiObservation(string failure)
        {
            _qualifiedUiObservation["captureFailure"] = failure;
            _qualifiedUiObservation["capturePending"] = _qualifiedUiCapture == null ? null
                : Path.GetFileName(_qualifiedUiCapture.Path);
            // No label or screenshot proves physical input or actual execution.
            // Active labels are judged with the native reads and visual review.
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "native-ui-active.json"),
                _qualifiedUiObservation.ToString(Formatting.Indented) + Environment.NewLine);
        }

        // UI-09: a real animated Classic execution disables the owned HUD
        // cells. Hold that execution with the native pause setter for one
        // bounded capture; never set an availability flag or disabled style.
        private readonly JObject _classicHudObservation = new JObject();
        private NativeUiCaptureBoundary _classicHudCapture;
        private readonly Stopwatch _classicHudCaptureClock = new Stopwatch();
        private bool _classicHudCaptureRequested;
        private bool _classicHudPauseOwned;
        private bool _classicHudPauseBefore;
        private NativeUiPauseRestoration _classicHudPauseRestoration;
        private readonly Stopwatch _classicHudPauseRestoreClock = new Stopwatch();

        private void BeginClassicHudAvailabilityObservation()
        {
            if (!_classicRecord.CastingScenario || _classicRecord.ExecutionMode != "animated") return;
            _classicHudObservation["schemaVersion"] = 1;
            _classicHudObservation["runId"] = _request.RunId;
            _classicHudObservation["sourceCommit"] = _request.ExpectedCommit;
            _classicHudObservation["packageSha256"] = _request.ExpectedPackageSha256;
            _classicHudObservation["screenWidth"] = Screen.width;
            _classicHudObservation["screenHeight"] = Screen.height;
            _classicHudObservation["condition"] = "real-classic-execution-held-by-native-pause";
            _classicHudObservation["visualReview"] = "NOT_REVIEWED";
            _classicHudObservation["before"] = ClassicHudAvailabilitySnapshot();
        }

        private bool ObserveClassicHudAvailability()
        {
            if (_classicHudObservation.Count == 0) return false;
            try
            {
                if (_classicHudCapture != null)
                {
                    if (!BuffPlannerUiRoot.IsExecutingForRuntime || !ClassicHudCellsUnavailable())
                        throw new InvalidOperationException("The real disabled condition ended before capture completed.");
                    NativeUiCaptureStatus status = _classicHudCapture.Observe(Time.frameCount,
                        _classicHudCaptureClock.ElapsedMilliseconds);
                    if (status == NativeUiCaptureStatus.Pending) return true;
                    if (status == NativeUiCaptureStatus.TimedOut)
                        throw new IOException("Classic disabled-HUD capture exceeded the ten-second boundary.");
                    _classicHudObservation["completedFrame"] = Time.frameCount;
                    _classicHudObservation["atCompletion"] = ClassicHudAvailabilitySnapshot();
                    _classicHudObservation["status"] = "CAPTURED";
                    _classicHudCapture = null;
                    _classicHudPauseRestoration = new NativeUiPauseRestoration(_classicHudPauseBefore,
                        WorkspaceEngineCaptureWaitMilliseconds);
                    _classicHudPauseRestoreClock.Restart();
                    return true; // release and result work run in subsequent Updates
                }
                if (_classicHudPauseRestoration != null)
                {
                    NativeUiPauseRestoreStatus restore = _classicHudPauseRestoration.Observe(
                        NativeUiPauseWorldReleased(), Game.Instance.IsPaused,
                        _classicHudPauseRestoreClock.ElapsedMilliseconds, value => Game.Instance.IsPaused = value);
                    if (restore == NativeUiPauseRestoreStatus.Pending) return true;
                    if (restore == NativeUiPauseRestoreStatus.TimedOut)
                        throw new InvalidOperationException("Classic capture pause restoration exceeded ten seconds.");
                    _classicHudObservation["pauseRestored"] = Game.Instance.IsPaused == _classicHudPauseBefore;
                    _classicHudPauseOwned = false;
                    _classicHudPauseRestoration = null;
                    return true;
                }
                if (_classicHudCaptureRequested) return false;
                if (!BuffPlannerUiRoot.IsExecutingForRuntime)
                    throw new InvalidOperationException("The animated execution ended before disabled HUD observation.");
                BuffPlannerHudButtonController hud = BuffPlannerUiRoot.HudForRuntime;
                if (hud == null) throw new InvalidOperationException("Classic HUD is missing.");
                hud.RefreshAvailability(); // production condition, no forced appearance
                if (!ClassicHudCellsUnavailable())
                    throw new InvalidOperationException("Executing Classic HUD cells are still available.");
                if (!_classicHudPauseOwned)
                {
                    _classicHudPauseBefore = Game.Instance.IsPaused;
                    _classicHudPauseOwned = true;
                    Game.Instance.IsPaused = true;
                    // StartMode may queue the setter until the next native tick.
                    // Verify the actual held mode in a subsequent Update.
                    _classicHudObservation["pauseBefore"] = _classicHudPauseBefore;
                    _classicHudCaptureClock.Restart();
                    return true;
                }
                if (!Game.Instance.IsPaused)
                {
                    if (_classicHudCaptureClock.ElapsedMilliseconds < WorkspaceEngineCaptureWaitMilliseconds) return true;
                    throw new InvalidOperationException("The real execution could not be paused at its capture boundary.");
                }
                _classicHudObservation["pauseHeld"] = true;
                // Let the native disabled ColorTint transition settle while
                // the actual execution condition remains held.
                if (_classicHudCaptureClock.ElapsedMilliseconds < 200) return true;
                _classicHudObservation["atRequest"] = ClassicHudAvailabilitySnapshot();
                _classicHudObservation["capture"] = "nui-hud-executing-disabled.png";
                _classicHudObservation["requestFrame"] = Time.frameCount;
                _classicHudCapture = new NativeUiCaptureBoundary(Path.Combine(_request.EvidenceDirectory,
                    "nui-hud-executing-disabled.png"), Time.frameCount, WorkspaceEngineCaptureWaitMilliseconds);
                _classicHudCaptureRequested = true;
                _classicHudCaptureClock.Restart();
                CaptureScreenshot(_classicHudCapture.Path);
                return true;
            }
            catch (Exception exception)
            {
                _classicHudObservation["captureFailure"] = exception.Message;
                _classicHudObservation["status"] = "FAILED";
                RestoreClassicHudCapturePause();
                throw;
            }
        }

        private static bool ClassicHudCellsUnavailable()
        {
            BuffPlannerHudButtonController hud = BuffPlannerUiRoot.HudForRuntime;
            return hud != null && new[] { "setup", "long", "important", "short" }.All(id =>
                hud.ButtonForRuntime(id) != null && !hud.ButtonForRuntime(id).interactable);
        }

        private static JObject ClassicHudAvailabilitySnapshot()
        {
            BuffPlannerHudButtonController hud = BuffPlannerUiRoot.HudForRuntime;
            Button[] owned = hud == null ? new Button[0] : new[] { "setup", "long", "important", "short" }
                .Select(id => hud.ButtonForRuntime(id)).Where(button => button != null).ToArray();
            return new JObject
            {
                { "sessionExecuting", BuffPlannerUiRoot.IsExecutingForRuntime },
                { "owned", new JArray(owned.Select(button => NativeUiStateJson.HudButton(
                    button.name, button.interactable, HighlightTint(button))).ToArray()) },
                { "native", new JArray(hud == null || hud.NativeClusterForRuntime == null ? new JObject[0]
                    : hud.NativeClusterForRuntime.GetComponentsInChildren<Button>(false)
                        .Where(button => !owned.Contains(button)).Select(button =>
                            NativeUiStateJson.HudButton(button.name, button.interactable, null)).ToArray()) }
            };
        }

        private void RestoreClassicHudCapturePause()
        {
            if (!_classicHudPauseOwned) return;
            if (Game.Instance == null) throw new InvalidOperationException("Classic capture pause owner lost the game.");
            Game.Instance.IsPaused = _classicHudPauseBefore;
            _classicHudObservation["pauseRestored"] = Game.Instance.IsPaused == _classicHudPauseBefore;
            // Cleanup can queue the request too. Keep ownership and the false
            // receipt until the original native state is actually observed.
            if ((bool)_classicHudObservation["pauseRestored"]) _classicHudPauseOwned = false;
        }

        private void EndClassicHudAvailabilityObservation(string reason)
        {
            if (_classicHudObservation.Count == 0) return;
            try { RestoreClassicHudCapturePause(); }
            finally
            {
                _classicHudObservation["terminalReason"] = reason;
                _classicHudObservation["capturePending"] = _classicHudCapture == null ? null
                    : Path.GetFileName(_classicHudCapture.Path);
                _classicHudObservation["after"] = ClassicHudAvailabilitySnapshot();
                AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "native-ui-classic-hud.json"),
                    _classicHudObservation.ToString(Formatting.Indented) + Environment.NewLine);
            }
        }

        // ---- helpers ------------------------------------------------------

        // The tint the cell's ColorTint transition has rendered on its
        // highlight (the hover art): r, g, b, a.
        private static float[] HighlightTint(Button button)
        {
            Graphic target = button == null ? null : button.targetGraphic;
            if (target == null || target.canvasRenderer == null) return null;
            Color color = target.canvasRenderer.GetColor();
            return new[] { color.r, color.g, color.b, color.a };
        }

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

        // The native portrait switch changes the full-screen character while
        // a service window is shown, and world selection only outside it.
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

        private NativeUiSpellbookView RecordSpellbookView(int index)
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
            return record;
        }

        private bool ContinueAfterNativeSpellbookView()
        {
            if (!_nativeUiBookView.MetamagicActive || !_nativeUiBookView.MetamagicInteractable)
            {
                if (_nativeUiBookView.UnavailablePlannerAction == null)
                {
                    BuffPlannerSpellbookEntryController entry = BuffPlannerUiRoot.SpellbookEntryForRuntime;
                    Button owned = entry == null ? null : entry.OwnedButtonForRuntime;
                    if (owned == null || !owned.gameObject.activeInHierarchy || !owned.IsInteractable())
                        return FinishNativeUi("unavailable-metamagic-planner-entry-unavailable");
                    UnitEntityData current = Kingmaker.UI.Common.UIUtility.GetCurrentCharacter();
                    _nativeUiBookView.UnavailablePlannerAction = new NativeUiUnavailablePlannerAction
                    {
                        ButtonInvoked = true, UnitIdBefore = current == null ? null : current.UniqueId
                    };
                    _nativeUi.AddNote("unavailable-metamagic:" + _nativeUiBookView.Unit +
                        ";planner-button-onClick;no-os-input;no-availability-override");
                    owned.onClick.Invoke();
                    NativeUiNext(22);
                    return false;
                }
            }
            if (_nativeUiUnitIndex == 0)
            {
                WriteSpellbookFacts();
                // The comparison spell's page, through the book's own level tab.
                if (BookSpellTrigger(NativeUiComparisonSpell) == null && ClickBookLevelOf(NativeUiComparisonSpell))
                {
                    NativeUiNext(11);
                    return false;
                }
                return OpenNativeBookPopup();
            }
            NativeUiNext(7);
            return false;
        }

        private static SpellBookController NativeSpellbookController()
        {
            Transform book = SpellbookWindow();
            return book == null ? null : book.GetComponent<SpellBookController>();
        }

        // The installed 2.1.7b mode getter is private; the toggle/restore
        // method is public. Use only this exact, verified read-only contract.
        private static bool NativeMetamagicMode(SpellBookController controller)
        {
            MethodInfo mode = typeof(SpellBookController).GetMethod("get_IsMetamagicMode",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mode == null) throw new InvalidOperationException("Native Metamagic mode getter is unavailable.");
            return (bool)mode.Invoke(controller, null);
        }

        private static Button NativeMetamagicButton()
        {
            Transform book = SpellbookWindow();
            Transform apply = book == null ? null
                : book.Find(BuffPlannerSpellbookEntryController.NativeFooterPath + "/Apply");
            return apply == null ? null : apply.GetComponent<Button>();
        }

        private bool BeginNativeMetamagicObservation(NativeUiSpellbookView view)
        {
            // Never make an unavailable button available for the observation.
            if (!view.MetamagicActive || !view.MetamagicInteractable) return false;
            SpellBookController controller = NativeSpellbookController();
            Button button = NativeMetamagicButton();
            if (controller == null || button == null)
            {
                _nativeUi.Failures.Add("metamagic-native-control-missing:" + view.Unit);
                return false;
            }
            if (NativeMetamagicMode(controller))
            {
                _nativeUi.Failures.Add("metamagic-already-open:" + view.Unit);
                return false;
            }
            view.MetamagicAction = new NativeUiMetamagicAction
            {
                ModeBefore = NativeMetamagicMode(controller), ButtonInvoked = true
            };
            _nativeUiMetamagicView = view;
            _nativeUi.AddNote("metamagic:" + view.Unit + ";native-button-onClick;no-os-input;no-write-spell");
            button.onClick.Invoke();
            NativeUiNext(12);
            return true;
        }

        private void RestoreNativeUiMetamagicMode()
        {
            if (_nativeUiMetamagicView == null) return;
            try
            {
                SpellBookController controller = NativeSpellbookController();
                bool before = _nativeUiMetamagicView.MetamagicAction.ModeBefore;
                if (controller != null && NativeMetamagicMode(controller) != before)
                    controller.SwitchToMetamagicMode(before);
            }
            finally { _nativeUiMetamagicView = null; }
        }

        private void ReleaseNativeUiProbes()
        {
            if (_nativeUiCapture != null)
            {
                _nativeUi.MissingCaptures.Add(Path.GetFileName(_nativeUiCapture.Path) + ":capture-interrupted");
                _nativeUiCapture = null;
                _nativeUiCaptureClock.Reset();
            }
            try { RestoreNativeUiMetamagicMode(); }
            finally
            {
                try { ReleaseNativeUiPlanLock(); }
                finally
                {
                    try
                    {
                        if (_nativeUiPauseOwned)
                        {
                            try
                            {
                                if (BuffPlannerUiRoot.IsCastingWorkspaceOpen) CloseProbeWorkspace();
                            }
                            finally
                            {
                                if (NativeServiceWindowShown()) CloseNativeServiceWindow();
                            }
                        }
                    }
                    finally { RestoreNativeUiPause(); }
                }
            }
        }

        private void RestoreNativeUiPause()
        {
            if (!_nativeUiPauseOwned) return;
            if (Game.Instance != null && Game.Instance.IsPaused != _nativeUiPauseBefore)
                Game.Instance.IsPaused = _nativeUiPauseBefore;
            _nativeUi.OwnedPauseRestored = Game.Instance != null && Game.Instance.IsPaused == _nativeUiPauseBefore;
            if (_nativeUi.OwnedPauseRestored) _nativeUiPauseOwned = false;
        }

        private static bool NativeUiPauseWorldReleased()
        {
            return Game.Instance != null && !BuffPlannerUiRoot.IsCastingWorkspaceOpen &&
                !NativeServiceWindowShown() && !Game.Instance.IsModeActive(GameModeType.FullScreenUi) &&
                (Game.Instance.CurrentMode == GameModeType.Default || Game.Instance.CurrentMode == GameModeType.Pause);
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

        private bool OpenNativeBookPopup()
        {
            TooltipTrigger trigger = BookSpellTrigger(NativeUiComparisonSpell);
            _nativeUi.AddNote("native-popup-row:" + (trigger != null ? NativeUiComparisonSpell : "first-row"));
            if (trigger == null) trigger = FirstSpellTrigger();
            if (trigger == null) return FinishNativeUi("spellbook-no-spell-trigger");
            trigger.OpenDescriptionWindow();
            NativeUiNext(6);
            return false;
        }

        // The open book page's row for one spell, by its blueprint's name.
        private static TooltipTrigger BookSpellTrigger(string spell)
        {
            Transform spellbook = SpellbookWindow();
            Transform book = spellbook == null ? null : spellbook.Find("Container_Book/Book");
            if (book == null) return null;
            return book.GetComponentsInChildren<TooltipTrigger>(false).FirstOrDefault(trigger =>
            {
                SpellItem item = trigger == null || !trigger.isActiveAndEnabled ? null
                    : trigger.GetComponentInParent<SpellItem>();
                return item != null && item.Spell != null && item.Spell.Name == spell;
            });
        }

        // Clicks the level tab of the first caster's book page that lists
        // the spell, as a click on the tab delivers it (its own Toggle).
        private bool ClickBookLevelOf(string spell)
        {
            UnitEntityData unit = _nativeUiUnits == null || _nativeUiUnits.Count == 0 ? null : _nativeUiUnits[0];
            int level = unit == null ? -1 : unit.Descriptor.Spellbooks
                .SelectMany(book => Enumerable.Range(0, book.MaxSpellLevel + 1)
                    .Where(value => book.GetKnownSpells(value).Any(known =>
                        known != null && known.Blueprint != null && known.Blueprint.Name == spell)))
                .DefaultIfEmpty(-1).First();
            Transform spellbook = SpellbookWindow();
            Kingmaker.UI.Common.ExtendedToggleTab tab = level < 0 || spellbook == null ? null
                : spellbook.GetComponentsInChildren<Kingmaker.UI.Common.ExtendedToggleTab>(false).FirstOrDefault(value =>
                    value.LevelIndex == level && value.Toggle != null && value.Toggle.IsInteractable());
            _nativeUi.AddNote("book-level-tab:" + spell + ";level=" + level + ";tab=" + (tab != null));
            if (tab == null) return false;
            Pointer(tab.Toggle.gameObject, ExecuteEvents.pointerClickHandler);
            return true;
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
                string failure = InvokeKeyBinding(route.Substring(4), _nativeUiSwitch);
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

        private static string InvokeKeyBinding(string name, NativeUiWindowSwitch observation)
        {
            KeyboardAccess keyboard = Game.Instance == null ? null : Game.Instance.Keyboard;
            if (keyboard == null) return "keyboard-missing";
            // A key press reaches the callback of the name's binding for the
            // current game mode (KeyboardAccess.Tick); GetBindingByName
            // returns only the name's first binding, whatever its mode.
            FieldInfo field = typeof(KeyboardAccess).GetField("m_Bindings",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var bindings = field == null ? null : field.GetValue(keyboard) as System.Collections.IEnumerable;
            if (bindings == null) return "bindings-missing";
            GameModeType mode = Game.Instance.CurrentMode;
            KeyboardAccess.Binding binding = bindings.OfType<KeyboardAccess.Binding>()
                .FirstOrDefault(value => value.Name == name && value.GameMode == mode);
            if (binding == null) return "binding-missing:" + name + ":" + mode;
            MethodInfo invoke = typeof(KeyboardAccess).GetMethod("OnCallbackByBinding",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (invoke == null) return "callback-route-missing";
            // These are read-only preconditions, not evidence that an OS key
            // reached Tick/InputMatched. The force flag still bypasses input matching.
            observation.KeyBinding = new NativeUiKeyBindingObservation
            {
                DispatchKind = "binding-callback-forced", BindingName = binding.Name,
                Key = binding.Key.ToString(), CurrentMode = mode.ToString(), BindingMode = binding.GameMode.ToString(),
                KeyboardDisabled = keyboard.Disabled, InputFieldSelected = NativeKeyboardStandsDown(),
                Paused = Game.Instance.IsPaused, WorksWhenPaused = binding.WorksWhenPaused,
                ControllerGamepad = Game.Instance.IsControllerGamepad
            };
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
                .Where(value => value.Label != null && value.Label.StartsWith(NativeUiComparisonSpell, StringComparison.Ordinal))
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
