using System;
using System.Linq;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // Owned Buff Planner button inside the native spellbook window, plus the
    // guarded handoff into the planner. The button is created and destroyed
    // only by this controller, is marked with an owned component, never
    // joins the native layout, and never touches native listeners. Discovery
    // is a bounded single-path lookup on a fixed cadence — no per-frame
    // global hierarchy search. Rendered placement and the same-context
    // return trip require the live campaign lane and remain unqualified.
    internal sealed class BuffPlannerSpellbookEntryController
    {
        internal const string SpellBookPath = "ServiceWindow/SpellBook";
        internal const string ButtonName = "BuffPlannerSpellbookButton";
        private const int TickCadence = 15;

        private readonly Action<string> _log;
        private readonly Func<bool> _openPlanner;
        private readonly Func<bool> _plannerOpen;
        private readonly PlannerUiTheme _theme;
        private readonly SpellbookHandoffStateMachine _handoff =
            new SpellbookHandoffStateMachine();
        private int _tickSkip;
        private int _spellbookInstanceId;
        private Transform _spellbook;
        private Button _ownedButton;
        private bool _handoffActive;

        internal BuffPlannerSpellbookEntryController(
            Action<string> log, Func<bool> openPlanner, Func<bool> plannerOpen,
            PlannerUiTheme theme)
        {
            _log = log ?? (value => { });
            _openPlanner = openPlanner ?? throw new ArgumentNullException("openPlanner");
            _plannerOpen = plannerOpen ?? throw new ArgumentNullException("plannerOpen");
            _theme = theme ?? PlannerUiTheme.Resolve(null);
        }

        internal SpellbookHandoffState HandoffState { get { return _handoff.State; } }
        internal bool IsAttached
        {
            get { return _ownedButton != null && _ownedButton.gameObject != null; }
        }

        internal void Tick()
        {
            if (_handoffActive) TickHandoff();
            if (++_tickSkip < TickCadence) return;
            _tickSkip = 0;
            try
            {
                ObserveSpellbook();
            }
            catch (Exception exception)
            {
                _log("[KBP-SPELLBOOK] observe failed: " + exception.Message);
            }
        }

        private void TickHandoff()
        {
            bool fullScreenActive = Game.Instance != null &&
                Game.Instance.IsModeActive(GameModeType.FullScreenUi);
            if (_handoff.Observe(fullScreenActive))
            {
                _handoffActive = false;
                RestoreButton();
                if (_plannerOpen())
                {
                    _log("[KBP-SPELLBOOK] handoff completed;planner=open.");
                    return;
                }
                _handoff.Rollback("planner-open-refused");
            }
            if (_handoff.State == SpellbookHandoffState.Failed)
            {
                _handoffActive = false;
                RestoreButton();
                _log("[KBP-SPELLBOOK] handoff failed;reason=" + _handoff.Failure +
                    ";spellbook remains usable.");
            }
        }

        private void ObserveSpellbook()
        {
            StaticCanvas canvas = StaticCanvas.Instance;
            Transform window = canvas == null
                ? null
                : canvas.transform.Find(SpellBookPath);
            if (window == null || !window.gameObject.activeInHierarchy)
            {
                // The native window destroyed with the scene takes our button
                // with it; forget the dead reference and wait for the next
                // window instance instead of recreating anything here.
                if (_ownedButton != null && _ownedButton.gameObject != null)
                    UnityEngine.Object.Destroy(_ownedButton.gameObject);
                _ownedButton = null;
                _spellbook = null;
                _spellbookInstanceId = 0;
                return;
            }
            int instanceId = window.GetInstanceID();
            if (instanceId != _spellbookInstanceId || _ownedButton == null)
            {
                DetachOwnedButton();
                _spellbook = window;
                _spellbookInstanceId = instanceId;
                AttachOwnedButton();
            }
        }

        private void AttachOwnedButton()
        {
            if (_spellbook == null || _ownedButton != null) return;
            _ownedButton = KingmakerUiFactory.CreateButton(ButtonName, _spellbook, _theme,
                "BUFF PLANNER", BeginHandoff);
            // Out-of-layout, like the HUD row: a planner-owned control must
            // never participate in native spellbook layout or the native
            // layout will re-anchor it.
            LayoutElement layout = _ownedButton.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            RectTransform buttonRect = (RectTransform)_ownedButton.transform;
            KingmakerUiFactory.SetAnchors(buttonRect, 0.965f, 0.925f, 0.995f, 0.975f);
            Text label = _ownedButton.GetComponentInChildren<Text>(true);
            if (label != null) label.fontSize = 13;
            _log("[KBP-SPELLBOOK] owned button attached;window=" + _spellbookInstanceId +
                ";path=" + SpellBookPath + ".");
        }

        private void DetachOwnedButton()
        {
            if (_ownedButton == null) return;
            // Only the owned control is destroyed; nothing native is touched.
            if (_ownedButton.gameObject != null)
                UnityEngine.Object.Destroy(_ownedButton.gameObject);
            _ownedButton = null;
        }

        private void BeginHandoff()
        {
            if (_handoffActive) return;
            if (Game.Instance == null)
            {
                _log("[KBP-SPELLBOOK] handoff refused;reason=no-game.");
                return;
            }
            if (_plannerOpen())
            {
                _log("[KBP-SPELLBOOK] handoff refused;reason=planner-already-open.");
                return;
            }
            bool fullScreenActive = Game.Instance.IsModeActive(GameModeType.FullScreenUi);
            _log("[KBP-SPELLBOOK] handoff begin;fullScreenActive=" + fullScreenActive +
                ";window=" + _spellbookInstanceId + ".");
            if (fullScreenActive)
            {
                // Request native closure through the spellbook's own close
                // affordance; the planner never seizes the mode itself.
                Button nativeClose = _spellbook == null ? null : _spellbook
                    .GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(button => button != null &&
                        button.name == "Close" && button.gameObject != null);
                if (nativeClose == null)
                {
                    _log("[KBP-SPELLBOOK] handoff refused;reason=native-close-affordance-missing.");
                    return;
                }
                nativeClose.onClick.Invoke();
                _handoff.Begin();
                _handoffActive = true;
                if (_ownedButton != null) _ownedButton.interactable = false;
                return;
            }
            if (_openPlanner())
                _log("[KBP-SPELLBOOK] planner opened directly;no-fullscreen-owner.");
            else
                _log("[KBP-SPELLBOOK] direct open refused.");
        }

        private void RestoreButton()
        {
            if (_ownedButton != null && _ownedButton.gameObject != null)
                _ownedButton.interactable = true;
        }

        internal void Release()
        {
            _handoff.Reset();
            _handoffActive = false;
            DetachOwnedButton();
            _spellbook = null;
            _spellbookInstanceId = 0;
        }
    }
}
