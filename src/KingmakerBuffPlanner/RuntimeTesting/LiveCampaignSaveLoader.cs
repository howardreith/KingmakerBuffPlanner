using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Harmony12;
using Kingmaker;
using KingmakerBuffPlanner.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Guarded campaign loader adapted from the qualified Gunslinger
    // working-save-smoke autonomous route. It drives and OBSERVES the native
    // chain — exact Load Game button click, MainMenuButtons.OnButtonLoadGame,
    // ListOfSaves.Initialize catalog capture, exact working descriptor, its
    // owning SaveSlot and SaveLoadWindow, SaveSlot.OnButtonSaveLoad,
    // SaveLoadWindow.HandleHardcodeMainMenuSaveLoad(same descriptor),
    // MainMenu.LoadGame(same descriptor), after-load callback — with strict
    // invocation ordering, a read-only native saver, save-write sentinels,
    // per-stage wall-clock budgets, and a stable post-load fingerprint.
    internal sealed class LiveCampaignSaveLoader
    {
        private const string HarmonyId = "kingmaker.buffplanner.runtimetesting.saveload";
        private static LiveCampaignSaveLoader _active;

        private readonly RuntimeTestRequest _request;
        private readonly ModLog _log;
        private readonly Stopwatch _elapsed;
        private readonly int _gameThreadId;
        private readonly List<string> _events = new List<string>();
        private readonly List<MethodBase> _patched = new List<MethodBase>();
        private HarmonyInstance _harmony;
        private MethodInfo _handler;
        private MethodInfo _initialize;
        private MethodInfo _loadEntry;
        private MethodInfo _slotAction;
        private MethodInfo _windowHandler;
        private string _stage = "harmony-install";
        private long _stageStartedMillis;
        private int _lastFrameCount = -1;
        private Button _button;
        private object _mainMenuButtons;
        private object _catalogObject;
        private object _catalogReceiver;
        private object _workingDescriptor;
        private object _baselineDescriptor;
        private GuardedReadOnlySaver _readOnlySaver;
        private object _receiverBoundSlot;
        private object _receiverBoundWindow;
        private string _slotMemberIdentity = "";
        private string _windowListIdentity = "";
        private int _buttonInvocations;
        private int _handlerInvocations;
        private int _catalogInvocations;
        private int _descriptorCount;
        private int _workingCount;
        private int _baselineCount;
        private int _slotActionInvocations;
        private int _windowHandlerInvocations;
        private int _loadEntryInvocations;
        private int _slotActionSequence;
        private int _windowHandlerSequence;
        private int _loadEntrySequence;
        private int _completionSequence;
        private int _fingerprintSequence;
        private int _stableFingerprints;
        private string _lastFingerprint = "";
        private bool _slotReceiverCorrelated;
        private bool _windowReceiverCorrelated;
        private bool _windowArgumentCorrelated;
        private bool _loadEntryCorrelated;
        private bool _completionCallback;
        private bool _callbackRegistered;
        private bool _writeObserved;
        private bool _wrongThread;
        private bool _orderingViolation;
        private bool _baselineLoadObserved;
        private bool _otherLoadObserved;
        private bool _hooksRemoved;
        private bool _buttonResolutionAttempted;
        private bool _scopeResolutionAttempted;
        private bool _loadInvoked;

        internal LiveCampaignSaveLoader(RuntimeTestRequest request, ModLog log)
        {
            _request = request ?? throw new ArgumentNullException("request");
            _log = log ?? throw new ArgumentNullException("log");
            _elapsed = Stopwatch.StartNew();
            _gameThreadId = Thread.CurrentThread.ManagedThreadId;
            _stageStartedMillis = 0;
        }

        internal bool IsComplete { get; private set; }
        internal string Stage { get { return _stage; } }
        internal string WorkingDescriptor { get; private set; }
        internal string BaselineDescriptor { get; private set; }
        internal int LoadActionCount { get { return _loadInvoked ? 1 : 0; } }

        // Observed-chain evidence surfaced by the runtime host.
        internal int HandlerInvocationCount { get { return _handlerInvocations; } }
        internal int CatalogInvocationCount { get { return _catalogInvocations; } }
        internal int CatalogDescriptorCount { get { return _descriptorCount; } }
        internal int WorkingMatchCount { get { return _workingCount; } }
        internal int BaselineMatchCount { get { return _baselineCount; } }
        internal bool SlotReceiverCorrelated { get { return _slotReceiverCorrelated; } }
        internal bool WindowReceiverCorrelated { get { return _windowReceiverCorrelated; } }
        internal bool WindowArgumentCorrelated { get { return _windowArgumentCorrelated; } }
        internal bool LoadEntryCorrelated { get { return _loadEntryCorrelated; } }
        internal bool CompletionCallbackObserved { get { return _completionCallback; } }
        internal bool UnexpectedSaveWriteObserved { get { return _writeObserved; } }
        internal bool WrongThreadObserved { get { return _wrongThread; } }
        internal bool OrderingViolationObserved { get { return _orderingViolation; } }
        internal string ChainSequences
        {
            get
            {
                return "slot=" + _slotActionSequence + ";window=" + _windowHandlerSequence +
                    ";loadEntry=" + _loadEntrySequence + ";completion=" + _completionSequence +
                    ";fingerprint=" + _fingerprintSequence;
            }
        }
        internal string FingerprintEvidence { get; private set; }

        internal void Update()
        {
            if (IsComplete) return;
            // UMM may dispatch OnUpdate several times per rendered frame; the
            // loader advances once per frame and budgets by wall clock.
            if (Time.frameCount == _lastFrameCount) return;
            _lastFrameCount = Time.frameCount;
            if (_stage == "harmony-install")
            {
                InstallHooks();
                Transition("main-menu-readiness");
                return;
            }
            if (!_callbackRegistered) RegisterCompletionCallback();
            FailFastOnViolations();
            if (StageBudgetExpired())
                throw new TimeoutException("Live save load timed out at " + _stage + ".");
            if (_stage == "main-menu-readiness")
            {
                _mainMenuButtons = MainMenuLoadContracts.ResolveActiveMainMenuButtons();
                if (_mainMenuButtons != null)
                {
                    Add("main-menu-ready", "receiver=" +
                        MainMenuLoadContracts.ObjectIdentity(_mainMenuButtons));
                    Transition("load-game-action-resolution");
                }
                return;
            }
            if (_stage == "load-game-action-resolution")
            {
                if (_buttonResolutionAttempted)
                    throw new InvalidOperationException(
                        "The exact Load Game action could not be proven at the main-menu lifecycle point.");
                _buttonResolutionAttempted = true;
                MainMenuLoadContracts.LoadButtonEvidence evidence;
                Button button = MainMenuLoadContracts.ResolveExactLoadButton(out evidence);
                if (button == null)
                    throw new InvalidOperationException(
                        "The exact Load Game button could not be resolved; evidence=" +
                        DescribeButtonSearch());
                _button = button;
                Add("load-button-resolved", "path=" + evidence.HierarchyPath +
                    ";siblings=" + evidence.SiblingIndex + "/" + evidence.SiblingCount +
                    ";labels=" + evidence.LabelIdentities.Count +
                    ";listeners=" + string.Join("|", evidence.ListenerIdentities.ToArray()));
                Transition("action-invocation");
                return;
            }
            if (_stage == "action-invocation")
            {
                Add("load-game-action-invoke-start", "");
                _buttonInvocations++;
                _button.onClick.Invoke();
                Add("button-onclick-invoke", "route=UnityEngine.UI.Button.onClick.Invoke;count=1");
                if (_buttonInvocations != 1 || _handlerInvocations != 1)
                    throw new InvalidOperationException(
                        "Normal Load Game action did not invoke exactly once; handler=" +
                        _handlerInvocations + ".");
                Transition("catalog-initialization",
                    "normal Unity event returned after one handler invocation");
                return;
            }
            if (_stage == "catalog-initialization")
            {
                if (_catalogInvocations != 1) return;
                Add("catalog-complete", "descriptorCount=" + _descriptorCount);
                Transition("descriptor-resolution");
                return;
            }
            if (_stage == "descriptor-resolution")
            {
                ResolveDescriptors();
                if (_workingCount == 1 && _baselineCount == 1)
                {
                    WorkingDescriptor = Describe(_workingDescriptor);
                    BaselineDescriptor = Describe(_baselineDescriptor);
                    Add("working-descriptor-resolved", WorkingDescriptor);
                    Add("baseline-excluded", BaselineDescriptor);
                    Transition("working-entry-readiness");
                }
                return;
            }
            if (_stage == "working-entry-readiness")
            {
                ResolveReceiverBoundScope();
                if (_receiverBoundSlot != null && _receiverBoundWindow != null)
                    Transition("receiver-bound-action-invocation");
                return;
            }
            if (_stage == "receiver-bound-action-invocation")
            {
                var descriptor = (Kingmaker.EntitySystem.Persistence.SaveInfo)_workingDescriptor;
                if (_readOnlySaver != null)
                    throw new InvalidOperationException("Guarded load was already started.");
                _readOnlySaver = new GuardedReadOnlySaver(descriptor,
                    detail => Add("read-only-native-save-load", detail));
                descriptor.Saver = _readOnlySaver;
                _slotAction.Invoke(_receiverBoundSlot, null);
                _loadInvoked = true;
                Add("receiver-bound-action-invoke-return",
                    "receiver=" + MainMenuLoadContracts.ObjectIdentity(_receiverBoundSlot));
                if (_slotActionInvocations != 1 || !_slotReceiverCorrelated)
                    throw new InvalidOperationException(
                        "Autonomous receiver-bound action did not enter exactly once on the exact working slot.");
                Transition("slot-action-invocation",
                    "autonomous exact normal action entered SaveSlot boundary");
                return;
            }
            if (_stage == "slot-action-invocation")
            {
                if (_windowHandlerInvocations != 1) return;
                if (!_windowReceiverCorrelated || !_windowArgumentCorrelated)
                    throw new InvalidOperationException(
                        "SaveLoadWindow handler receiver or argument was not correlated.");
                Transition("window-handler-invocation",
                    "exact owning SaveLoadWindow handler observed with exact descriptor");
                return;
            }
            if (_stage == "window-handler-invocation")
            {
                if (_loadEntryInvocations != 1) return;
                if (!_loadEntryCorrelated)
                    throw new InvalidOperationException(
                        "MainMenu.LoadGame argument was not correlated to the exact working descriptor.");
                Transition("load-entry-invocation",
                    "exact MainMenu.LoadGame invoked once with the working descriptor");
                return;
            }
            if (_stage == "load-entry-invocation")
            {
                if (!_completionCallback) return;
                var descriptor = (Kingmaker.EntitySystem.Persistence.SaveInfo)_workingDescriptor;
                if (_readOnlySaver == null || !_readOnlySaver.Complete ||
                    !ReferenceEquals(descriptor.Saver, _readOnlySaver))
                    throw new InvalidOperationException(
                        "Native read-only load header protocol did not complete exactly once.");
                descriptor.Saver = _readOnlySaver.Native;
                Add("read-only-native-save-load-verified",
                    "headerUpdateSuppressed=1;commitSuppressed=1;nativeDescriptorRestored=true");
                Transition("load-completion", "after-load callback observed");
                return;
            }
            if (_stage == "load-completion")
            {
                Transition("post-load-fingerprint", "fingerprinting persistent state");
                return;
            }
            if (_stage == "post-load-fingerprint")
            {
                PollFingerprint();
            }
        }

        private void InstallHooks()
        {
            if (_active != null)
                throw new InvalidOperationException("A guarded save load is already active.");
            try
            {
                Assembly assembly = typeof(Game).Assembly;
                Type owner = assembly.GetType(MainMenuLoadContracts.MainMenuButtonsTypeName, true);
                _handler = owner.GetMethods(MainMenuLoadContracts.AllInstance).Single(method =>
                    method.Name == "OnButtonLoadGame" &&
                    method.GetParameters().Length == 0 &&
                    method.ReturnType == typeof(void));
                Type list = assembly.GetType(MainMenuLoadContracts.ListOfSavesTypeName, true);
                _initialize = list.GetMethods(MainMenuLoadContracts.AllInstance).Single(method =>
                    method.Name == "Initialize" &&
                    method.GetParameters().Length == 2 &&
                    MainMenuLoadContracts.IsSaveInfoList(method.GetParameters()[0].ParameterType) &&
                    method.GetParameters()[1].ParameterType == typeof(bool) &&
                    method.ReturnType == typeof(void));
                Type menu = assembly.GetType(MainMenuLoadContracts.MainMenuTypeName, true);
                _loadEntry = menu.GetMethods(MainMenuLoadContracts.AllInstance).Single(method =>
                    method.Name == "LoadGame" &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType.FullName ==
                        MainMenuLoadContracts.SaveInfoTypeName &&
                    method.ReturnType == typeof(void));
                Type slot = assembly.GetType(MainMenuLoadContracts.SaveSlotTypeName, true);
                Type window = assembly.GetType(MainMenuLoadContracts.SaveLoadWindowTypeName, true);
                Type descriptorType = assembly.GetType(MainMenuLoadContracts.SaveInfoTypeName, true);
                _slotAction = MainMenuLoadContracts.ExactPatchableMethod(slot,
                    "OnButtonSaveLoad", Type.EmptyTypes, typeof(void));
                _windowHandler = MainMenuLoadContracts.ExactPatchableMethod(window,
                    "HandleHardcodeMainMenuSaveLoad", new[] { descriptorType }, typeof(void));
                Type manager = assembly.GetType(
                    "Kingmaker.EntitySystem.Persistence.SaveManager", true);
                Type areaState = assembly.GetType(
                    "Kingmaker.EntitySystem.AreaPersistentState", true);
                MethodInfo prefix = typeof(LiveCampaignSaveLoader).GetMethod(
                    "Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                _harmony = HarmonyInstance.Create(HarmonyId);
                Patch(_handler, prefix);
                Patch(_initialize, prefix);
                Patch(_loadEntry, prefix);
                Patch(_slotAction, prefix);
                Patch(_windowHandler, prefix);
                Patch(MainMenuLoadContracts.ExactPatchableMethod(manager,
                    "DeleteSave", new[] { descriptorType }, typeof(void)), prefix);
                Patch(MainMenuLoadContracts.ExactPatchableMethod(manager,
                    "DeleteSave", new[] { typeof(string) }, typeof(void)), prefix);
                Patch(MainMenuLoadContracts.ExactPatchableMethod(manager,
                    "RemoveSaveFromList", new[] { descriptorType }, typeof(void)), prefix);
                Patch(MainMenuLoadContracts.ExactPatchableMethod(manager,
                    "SaveStashedArea", new[] { descriptorType, areaState }, typeof(void)), prefix);
                Patch(MainMenuLoadContracts.ExactPatchableMethod(manager,
                    "SaveRoutine", new[] { descriptorType, typeof(bool) },
                    typeof(System.Collections.Generic.IEnumerator<object>)), prefix);
                _active = this;
                Add("contracts-installed", "hooks=" + _patched.Count);
            }
            catch
            {
                RemoveHooks();
                throw;
            }
        }

        private static bool Prefix(MethodBase __originalMethod, object __instance,
            object[] __args)
        {
            LiveCampaignSaveLoader active = _active;
            if (active == null) return true;
            try
            {
                active.ObserveEnter(__originalMethod, __instance, __args);
            }
            catch (Exception exception)
            {
                // A diagnostic failure must never escape into the game handler.
                active.Add("observation-hook-error", "hook=prefix;" +
                    exception.GetType().FullName + ": " + exception.Message);
            }
            return true;
        }

        private void ObserveEnter(MethodBase method, object receiver, object[] args)
        {
            if (Thread.CurrentThread.ManagedThreadId != _gameThreadId)
            {
                _wrongThread = true;
                return;
            }
            if (method == _handler)
            {
                _handlerInvocations++;
                Add("load-game-handler-enter", "count=" + _handlerInvocations);
            }
            else if (method == _initialize)
            {
                _catalogInvocations++;
                if (_buttonInvocations != 1 || _handlerInvocations != 1)
                {
                    _orderingViolation = true;
                    Add("catalog-ordering-violation",
                        "button=" + _buttonInvocations + ";handler=" + _handlerInvocations);
                }
                _catalogObject = args == null || args.Length == 0 ? null : args[0];
                _catalogReceiver = receiver;
                _descriptorCount = MainMenuLoadContracts.ReadCount(_catalogObject);
                Add("catalog-initialize-enter", "capturedExactList=True;count=" +
                    _catalogInvocations + ";descriptorCount=" + _descriptorCount);
            }
            else if (method == _slotAction)
            {
                _slotActionInvocations++;
                _slotReceiverCorrelated = _slotReceiverCorrelated ||
                    ReferenceEquals(receiver, _receiverBoundSlot);
                _slotActionSequence = _events.Count + 1;
                Add("receiver-bound-slot-action-enter", "count=" +
                    _slotActionInvocations + ";exactWorkingSlot=" +
                    ReferenceEquals(receiver, _receiverBoundSlot));
            }
            else if (method == _windowHandler)
            {
                _windowHandlerInvocations++;
                object argument = args == null || args.Length != 1 ? null : args[0];
                _windowReceiverCorrelated = _windowReceiverCorrelated ||
                    ReferenceEquals(receiver, _receiverBoundWindow);
                _windowArgumentCorrelated = _windowArgumentCorrelated ||
                    ReferenceEquals(argument, _workingDescriptor);
                _windowHandlerSequence = _events.Count + 1;
                Add("receiver-bound-window-handler-enter", "count=" +
                    _windowHandlerInvocations + ";exactWindow=" +
                    ReferenceEquals(receiver, _receiverBoundWindow) +
                    ";exactWorkingDescriptor=" + ReferenceEquals(argument, _workingDescriptor));
            }
            else if (method == _loadEntry)
            {
                object argument = args == null || args.Length == 0 ? null : args[0];
                _baselineLoadObserved = _baselineLoadObserved || IsBaseline(argument);
                _otherLoadObserved = _otherLoadObserved ||
                    (!IsBaseline(argument) && !ReferenceEquals(argument, _workingDescriptor));
                _loadEntryCorrelated = ReferenceEquals(argument, _workingDescriptor) &&
                    MainMenuLoadContracts.ContainsReference(_catalogObject, _workingDescriptor);
                _loadEntryInvocations++;
                _loadEntrySequence = _events.Count + 1;
                Add("load-entry-enter", "objectReferenceCorrelated=" + _loadEntryCorrelated);
            }
            else
            {
                _writeObserved = true;
                Add("unexpected-save-write", "method=" + method.DeclaringType.FullName +
                    "." + method.Name);
            }
        }

        private void ResolveDescriptors()
        {
            IEnumerable values = _catalogObject as IEnumerable;
            if (values == null) return;
            List<object> entries = values.Cast<object>().ToList();
            _descriptorCount = entries.Count;
            List<object> working = entries.Where(IsWorking).ToList();
            List<object> baseline = entries.Where(IsBaseline).ToList();
            _workingCount = working.Count;
            _baselineCount = baseline.Count;
            if (_workingCount == 1) _workingDescriptor = working[0];
            if (_baselineCount == 1) _baselineDescriptor = baseline[0];
        }

        private void ResolveReceiverBoundScope()
        {
            if (_scopeResolutionAttempted) return;
            _scopeResolutionAttempted = true;
            Type slotType = typeof(Game).Assembly.GetType(
                MainMenuLoadContracts.SaveSlotTypeName, true);
            List<Component> slots = Resources.FindObjectsOfTypeAll(slotType)
                .OfType<Component>()
                .Where(component => component != null &&
                    component.GetType() == slotType &&
                    ContainsExactDescriptor(component, _workingDescriptor))
                .ToList();
            if (slots.Count != 1)
            {
                _scopeResolutionAttempted = false;
                return;
            }
            Component slot = slots[0];
            string memberIdentity;
            if (!TryFindExactDescriptorMember(slot, _workingDescriptor, out memberIdentity))
            {
                _scopeResolutionAttempted = false;
                return;
            }
            _slotMemberIdentity = memberIdentity;
            Type windowType = typeof(Game).Assembly.GetType(
                MainMenuLoadContracts.SaveLoadWindowTypeName, true);
            List<Component> windows = new List<Component>();
            for (Transform current = slot.transform; current != null; current = current.parent)
                windows.AddRange(current.gameObject.GetComponents<Component>()
                    .Where(component => component != null &&
                        component.GetType() == windowType));
            windows = windows.Distinct().ToList();
            if (windows.Count != 1)
            {
                _scopeResolutionAttempted = false;
                return;
            }
            _receiverBoundSlot = slot;
            _receiverBoundWindow = windows[0];
            List<Component> lists = windows[0].gameObject
                .GetComponentsInChildren<Component>(true)
                .Where(component => component != null &&
                    component.GetType().FullName == MainMenuLoadContracts.ListOfSavesTypeName &&
                    ReferenceEquals(component, _catalogReceiver as Component))
                .ToList();
            if (lists.Count != 1)
            {
                _receiverBoundSlot = null;
                _receiverBoundWindow = null;
                _scopeResolutionAttempted = false;
                return;
            }
            _windowListIdentity = MainMenuLoadContracts.ObjectIdentity(lists[0]);
            Add("working-receiver-bound-action-ready",
                "slot=" + MainMenuLoadContracts.ObjectIdentity(slot) +
                ";window=" + MainMenuLoadContracts.ObjectIdentity(windows[0]) +
                ";list=" + _windowListIdentity +
                ";descriptorMember=" + _slotMemberIdentity +
                ";path=" + MainMenuLoadContracts.HierarchyPath(slot.transform));
        }

        private void RegisterCompletionCallback()
        {
            if (!RegisterAfterLoad(new Action(OnLoadCompleted))) return;
            _callbackRegistered = true;
            Add("completion-callback-registered", "read-only callback registration");
        }

        private static bool RegisterAfterLoad(Action callback)
        {
            object manager = Game.Instance == null
                ? null : MainMenuLoadContracts.ReadMember(Game.Instance, "SaveManager");
            if (manager == null) return false;
            MethodInfo method = manager.GetType().GetMethod("AddCallbackAfterLoad",
                BindingFlags.Instance | BindingFlags.Public, null,
                new[] { typeof(Action) }, null);
            if (method == null)
                throw new MissingMethodException("SaveManager.AddCallbackAfterLoad(Action)");
            method.Invoke(manager, new object[] { callback });
            return true;
        }

        // Mission section 8 (save/reload): the SAME working descriptor is
        // loaded again from inside the campaign through the game's own
        // Game.LoadGame, as a player's in-game load would. A native load
        // bumps the header's load counter and commits it, so the reload runs
        // under a fresh read-only saver exactly like the first load: one
        // header update and one commit, both suppressed, and the native
        // saver restored afterwards. Only the descriptor the guarded
        // main-menu chain proved is ever passed.
        internal const int ReloadBudgetSeconds = 240;
        private GuardedReadOnlySaver _reloadSaver;
        private bool _reloadStarted;
        private bool _reloadCallback;
        private bool _reloadWrongThread;
        private bool _reloadComplete;
        private long _reloadStartedMillis;
        private int _reloadLastFrame = -1;
        private int _reloadStableFingerprints;
        private string _reloadLastFingerprint = "";

        internal string ReloadEvidence { get; private set; }

        internal void BeginGuardedReload()
        {
            if (!IsComplete)
                throw new InvalidOperationException("The guarded first load has not completed.");
            if (_reloadStarted)
                throw new InvalidOperationException("The guarded reload was already started.");
            var descriptor = _workingDescriptor as Kingmaker.EntitySystem.Persistence.SaveInfo;
            if (descriptor == null || Game.Instance == null)
                throw new InvalidOperationException("No proven working descriptor to reload.");
            _reloadStarted = true;
            _reloadStartedMillis = _elapsed.ElapsedMilliseconds;
            _reloadSaver = new GuardedReadOnlySaver(descriptor,
                detail => Add("reload-read-only-native-save-load", detail));
            descriptor.Saver = _reloadSaver;
            Game.Instance.LoadGame(descriptor);
            Add("reload-load-game-invoked", "Game.LoadGame(exact working descriptor)");
            if (RegisterAfterLoad(new Action(OnReloadCompleted)))
                Add("reload-callback-registered", "read-only callback registration");
        }

        // Null while the reload is in progress; its evidence once the header
        // protocol completed, the native saver is restored and the reloaded
        // campaign's identity is stable over two frames. Throws on a
        // violation or when the budget is spent.
        internal string UpdateReload()
        {
            if (!_reloadStarted) return null;
            if (_reloadComplete) return ReloadEvidence;
            if (_reloadWrongThread)
                throw new InvalidOperationException("The reload callback ran off the game thread.");
            if (_elapsed.ElapsedMilliseconds - _reloadStartedMillis > ReloadBudgetSeconds * 1000L)
                throw new TimeoutException("The guarded in-game reload did not complete within " +
                    ReloadBudgetSeconds + " s.");
            if (Time.frameCount == _reloadLastFrame) return null;
            _reloadLastFrame = Time.frameCount;
            if (_reloadSaver != null)
            {
                if (!_reloadSaver.Complete) return null;
                var descriptor = (Kingmaker.EntitySystem.Persistence.SaveInfo)_workingDescriptor;
                if (!ReferenceEquals(descriptor.Saver, _reloadSaver))
                    throw new InvalidOperationException(
                        "The reload's read-only header protocol did not complete exactly once.");
                descriptor.Saver = _reloadSaver.Native;
                _reloadSaver = null;
                Add("reload-read-only-native-save-load-verified",
                    "headerUpdateSuppressed=1;commitSuppressed=1;nativeDescriptorRestored=true");
            }
            string fingerprint = CurrentFingerprint();
            if (fingerprint == null) return null;
            if (string.Equals(fingerprint, _reloadLastFingerprint, StringComparison.Ordinal))
                _reloadStableFingerprints++;
            else
            {
                _reloadLastFingerprint = fingerprint;
                _reloadStableFingerprints = 1;
            }
            if (_reloadStableFingerprints < 2) return null;
            _reloadComplete = true;
            ReloadEvidence = fingerprint + ";afterLoadCallback=" + _reloadCallback +
                ";elapsedMs=" + (_elapsed.ElapsedMilliseconds - _reloadStartedMillis);
            Add("reload-stable-fingerprint", ReloadEvidence);
            WriteEventsEvidence();
            return ReloadEvidence;
        }

        private void OnReloadCompleted()
        {
            if (Thread.CurrentThread.ManagedThreadId != _gameThreadId)
            {
                _reloadWrongThread = true;
                return;
            }
            _reloadCallback = true;
            Add("reload-after-load-callback", "SaveManager after-load callback invoked");
        }

        private void OnLoadCompleted()
        {
            if (Thread.CurrentThread.ManagedThreadId != _gameThreadId)
            {
                _wrongThread = true;
                return;
            }
            _completionCallback = true;
            _completionSequence = _events.Count + 1;
            Add("after-load-callback", "SaveManager after-load callback invoked");
        }

        private void PollFingerprint()
        {
            string value = CurrentFingerprint();
            if (value == null) return;
            if (string.Equals(value, _lastFingerprint, StringComparison.Ordinal))
                _stableFingerprints++;
            else
            {
                _lastFingerprint = value;
                _stableFingerprints = 1;
            }
            if (_stableFingerprints == 2)
            {
                _fingerprintSequence = _events.Count + 1;
                FingerprintEvidence = value;
                Add("stable-post-load-fingerprint", value);
                CompleteLoad();
            }
        }

        // The loaded campaign's identity; null before a player exists.
        // Throws when it is not the expected working campaign.
        private string CurrentFingerprint()
        {
            object player = Game.Instance == null
                ? null : MainMenuLoadContracts.ReadMember(Game.Instance, "Player");
            if (player == null) return null;
            string gameId = MainMenuLoadContracts.Read(player, "GameId");
            object area = MainMenuLoadContracts.ReadMember(Game.Instance, "CurrentlyLoadedArea");
            object scene = MainMenuLoadContracts.ReadMember(Game.Instance, "CurrentScene");
            object party = MainMenuLoadContracts.ReadMember(player, "Party");
            object main = MainMenuLoadContracts.ReadMember(player, "MainCharacter");
            int partyCount = MainMenuLoadContracts.ReadCount(party);
            string value = "gameId=" + gameId +
                ";areaType=" + (area == null ? "" : area.GetType().FullName) +
                ";sceneType=" + (scene == null ? "" : scene.GetType().FullName) +
                ";partyCount=" + partyCount +
                ";mainCharacterType=" + (main == null ? "" : main.GetType().FullName);
            if (!string.Equals(gameId, Parameter("expectedGameId"), StringComparison.Ordinal) ||
                partyCount <= 0)
                throw new InvalidOperationException(
                    "Loaded campaign fingerprint mismatch: " + value + ".");
            return value;
        }

        private void CompleteLoad()
        {
            if (!StrictOrder())
                throw new InvalidOperationException(
                    "Observed native chain ordering was not strict: " + ChainSequences + ".");
            RemoveHooks();
            WriteEventsEvidence();
            IsComplete = true;
            _log.Info("[KBP-BOOT] exact working campaign loaded through observed native chain;" +
                FingerprintEvidence + ";events=" + _events.Count + ".");
        }

        private bool StrictOrder()
        {
            return _slotActionSequence > 0 &&
                _slotActionSequence < _windowHandlerSequence &&
                _windowHandlerSequence < _loadEntrySequence &&
                _loadEntrySequence < _completionSequence &&
                _completionSequence < _fingerprintSequence;
        }

        private void FailFastOnViolations()
        {
            if (_writeObserved)
                throw new InvalidOperationException(
                    "A native save-writing or migration method was observed.");
            if (_wrongThread)
                throw new InvalidOperationException(
                    "A scenario callback ran off the game thread.");
            if (_orderingViolation)
                throw new InvalidOperationException(
                    "Catalog initialization was not ordered after one action.");
            if (_baselineLoadObserved)
                throw new InvalidOperationException(
                    "The baseline descriptor entered MainMenu.LoadGame.");
            if (_otherLoadObserved)
                throw new InvalidOperationException(
                    "A descriptor other than the working save entered MainMenu.LoadGame.");
        }

        private bool StageBudgetExpired()
        {
            return _elapsed.ElapsedMilliseconds - _stageStartedMillis >=
                StageBudgetSeconds(_stage) * 1000L;
        }

        private static int StageBudgetSeconds(string stage)
        {
            if (stage == "main-menu-readiness") return 240;
            if (stage == "load-game-action-resolution") return 30;
            if (stage == "action-invocation") return 30;
            if (stage == "catalog-initialization") return 60;
            if (stage == "descriptor-resolution") return 30;
            if (stage == "working-entry-readiness") return 60;
            if (stage == "receiver-bound-action-invocation") return 30;
            if (stage == "slot-action-invocation") return 120;
            if (stage == "window-handler-invocation") return 120;
            if (stage == "load-entry-invocation") return 600;
            if (stage == "load-completion") return 30;
            if (stage == "post-load-fingerprint") return 120;
            return 60;
        }

        private void Transition(string stage)
        {
            Transition(stage, "");
        }

        private void Transition(string stage, string detail)
        {
            _stage = stage;
            _stageStartedMillis = _elapsed.ElapsedMilliseconds;
            Add("stage-enter", "stage=" + stage + (string.IsNullOrEmpty(detail)
                ? "" : ";" + detail));
        }

        private void Patch(MethodBase method, MethodInfo prefix)
        {
            _harmony.Patch(method, new HarmonyMethod(prefix), null, null);
            _patched.Add(method);
        }

        private void RemoveHooks()
        {
            if (_hooksRemoved) return;
            foreach (MethodBase method in _patched.ToArray())
            {
                try
                {
                    _harmony.Unpatch(method, HarmonyPatchType.All, HarmonyId);
                }
                catch (Exception exception)
                {
                    Add("unpatch-failure", method.DeclaringType.FullName + "." +
                        method.Name + ";" + exception.GetType().Name);
                }
            }
            _patched.Clear();
            _hooksRemoved = true;
            if (ReferenceEquals(_active, this)) _active = null;
        }

        private void WriteEventsEvidence()
        {
            try
            {
                var builder = new System.Text.StringBuilder();
                builder.Append("{\"schemaVersion\":1,\"runId\":");
                builder.Append(Newtonsoft.Json.JsonConvert.ToString(_request.RunId));
                builder.Append(",\"stage\":");
                builder.Append(Newtonsoft.Json.JsonConvert.ToString(_stage));
                builder.Append(",\"sequences\":\"").Append(ChainSequences).Append("\"");
                builder.Append(",\"events\":[");
                for (int i = 0; i < _events.Count; i++)
                {
                    if (i > 0) builder.Append(',');
                    int separator = _events[i].IndexOf('|');
                    string kind = separator < 0 ? _events[i] : _events[i].Substring(0, separator);
                    string detail = separator < 0 ? "" : _events[i].Substring(separator + 1);
                    builder.Append("{\"sequence\":").Append((i + 1).ToString(
                        CultureInfo.InvariantCulture));
                    builder.Append(",\"kind\":").Append(Newtonsoft.Json.JsonConvert.ToString(kind));
                    builder.Append(",\"detail\":").Append(Newtonsoft.Json.JsonConvert.ToString(detail));
                    builder.Append('}');
                }
                builder.Append("]}");
                AtomicFile.WriteUtf8(System.IO.Path.Combine(
                    _request.EvidenceDirectory, "saveload-chain-events.json"),
                    builder.ToString() + Environment.NewLine);
            }
            catch (Exception exception)
            {
                _log.Error("Save-load chain evidence flush failed.", exception);
            }
        }

        private void Add(string kind, string detail)
        {
            if (_events.Count >= 4000) return;
            _events.Add(kind + "|" + (detail ?? string.Empty));
            _log.Info("[KBP-SAVE-LOAD] " + kind + ";" + detail);
        }

        private string DescribeButtonSearch()
        {
            int activeButtons = Resources.FindObjectsOfTypeAll(typeof(Button))
                .OfType<Button>().Count(button => button != null &&
                    button.gameObject.activeInHierarchy);
            return "activeButtons=" + activeButtons + ";expectedPath=" +
                MainMenuLoadContracts.ButtonPath;
        }

        private bool IsWorking(object value)
        {
            return value != null && value.GetType().FullName ==
                MainMenuLoadContracts.SaveInfoTypeName &&
                MainMenuLoadContracts.Read(value, "Name") == Parameter("workingSaveName") &&
                MainMenuLoadContracts.Leaf(MainMenuLoadContracts.Read(value, "FolderName")) ==
                    Parameter("workingFileName") &&
                MainMenuLoadContracts.Leaf(MainMenuLoadContracts.Read(value, "FileName")) ==
                    Parameter("workingFileName") &&
                MainMenuLoadContracts.Read(value, "GameName") == Parameter("expectedGameName") &&
                MainMenuLoadContracts.Read(value, "GameId") == Parameter("expectedGameId");
        }

        private bool IsBaseline(object value)
        {
            return value != null && value.GetType().FullName ==
                MainMenuLoadContracts.SaveInfoTypeName &&
                MainMenuLoadContracts.Read(value, "Name") == Parameter("baselineSaveName") &&
                MainMenuLoadContracts.Leaf(MainMenuLoadContracts.Read(value, "FolderName")) ==
                    Parameter("baselineFileName") &&
                MainMenuLoadContracts.Leaf(MainMenuLoadContracts.Read(value, "FileName")) ==
                    Parameter("baselineFileName");
        }

        private static string Describe(object descriptor)
        {
            return "name=" + MainMenuLoadContracts.Read(descriptor, "Name") +
                ";file=" + MainMenuLoadContracts.Leaf(
                    MainMenuLoadContracts.Read(descriptor, "FileName")) +
                ";gameName=" + MainMenuLoadContracts.Read(descriptor, "GameName") +
                ";gameId=" + MainMenuLoadContracts.Read(descriptor, "GameId") +
                ";area=" + MainMenuLoadContracts.Read(descriptor, "Area");
        }

        private static bool ContainsExactDescriptor(object owner, object expected)
        {
            string ignored;
            return TryFindExactDescriptorMember(owner, expected, out ignored);
        }

        private static bool TryFindExactDescriptorMember(object owner, object expected,
            out string identity)
        {
            identity = "";
            for (Type type = owner == null ? null : owner.GetType();
                type != null; type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly))
                {
                    if (field.FieldType.IsValueType || field.FieldType == typeof(string))
                        continue;
                    object value;
                    try { value = field.GetValue(owner); } catch { continue; }
                    if (!ReferenceEquals(value, expected)) continue;
                    identity = field.DeclaringType.FullName + "." + field.Name;
                    return true;
                }
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly))
                {
                    if (!property.CanRead || property.GetIndexParameters().Length != 0 ||
                        property.PropertyType.IsValueType ||
                        property.PropertyType == typeof(string)) continue;
                    object value;
                    try { value = property.GetValue(owner, null); } catch { continue; }
                    if (!ReferenceEquals(value, expected)) continue;
                    identity = property.DeclaringType.FullName + "." + property.Name;
                    return true;
                }
            }
            return false;
        }

        private string Parameter(string name)
        {
            return (string)_request.Parameters[name];
        }
    }
}
