using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private static void RunControllerRoutineTests(string root)
        {
            Run("controller-input-neutral-edges-and-transition-frames", TestControllerActivationLatch);
            Run("controller-input-owned-bindings-and-native-takeover", TestControllerLayerOwnership);
            Run("controller-entry-retained-layer-unload-reinstall", TestControllerEntryLifecycle);
            Run("controller-entry-native-detach-keeps-foreign-bindings", TestControllerNativeDetach);
            Run("controller-native-assembly-entry-and-stack-contracts", TestControllerNativeContracts);
            Run("controller-cold-saved-session-selects-each-routine", () => TestControllerColdSession(root));
            Run("controller-browsing-is-read-only", () => TestControllerBrowsing(root));
            Run("controller-latest-session-edit-and-failed-save", () => TestControllerDurability(root));
            Run("controller-policies-modes-combat-and-empty-refusals", () => TestControllerPolicies(root));
            Run("controller-stop-uses-host-and-does-not-reenter", () => TestControllerStop(root));
            Run("controller-stop-before-first-pump-keeps-terminal-result", () => TestControllerImmediateStop(root));
            Run("controller-results-include-omissions-and-refusal-details", () => TestControllerDetails(root));
            Run("controller-unavailable-session-is-explained", TestControllerUnavailable);
            Run("controller-unreadable-profile-refuses-without-overwrite", () => TestControllerCorruptPlan(root));
            Run("controller-persistence-presentation-before-run", () => TestControllerPersistencePresentation(root));
            Run("controller-latest-refusal-survives-reconstruction", () => TestControllerLatestOutcome(root));
            Run("controller-production-entry-does-not-construct-editor", TestControllerProductionWiring);
            Run("controller-configure-author-save-return-same-session", () => TestControllerConfigure(root));
            Run("controller-results-isolate-focus-and-confirm", () => TestControllerResultsNavigation(root));
            Run("controller-native-tmp-initialization-and-mouse-fallback", TestControllerAuthoringContracts);
            Run("controller-spellbook-handoff-waits-for-release", TestControllerBookHandoff);
            Run("controller-spellbook-handoff-cancel-timeout-no-open", TestControllerBookHandoffCancel);
            Run("controller-spellbook-native-close-and-conflict-selection", TestControllerBookContracts);
            Run("controller-queued-mode-actions-own-exact-request", TestControllerModeQueue);
            Run("controller-editor-back-keeps-text-intent-and-single-edge", TestControllerTextBack);
        }

        private static void TestControllerActivationLatch()
        {
            var latch = new ControllerActivationLatch();
            for (int frame = 1; frame < 20; frame++)
            {
                latch.Observe(false, frame);
                Assert(!latch.Take(frame));
            }
            latch.Observe(true, 20);
            Assert(!latch.Take(20));
            Assert(latch.Take(21));
            Assert(!latch.Take(21));
            Assert(!latch.Take(22));
            // A held Run cannot become Stop, even if the selector focus or
            // running state changes. Opening/reconnecting requires neutral.
            for (int frame = 22; frame < 80; frame++)
            {
                latch.Observe(false, frame);
                Assert(!latch.Take(frame));
            }
            latch.Observe(true, 80);
            Assert(!latch.Take(80));
            Assert(latch.Take(81));
            latch.Reset();
            Assert(!latch.Take(82));
            latch.Observe(true, 82);
            Assert(!latch.Ready(82) && latch.Ready(83));
            latch.Reset();
            Assert(!latch.CanRelease(true, 90));
            Assert(!latch.CanRelease(false, 91));
            Assert(!latch.CanRelease(true, 92));
            Assert(latch.CanRelease(true, 93));
        }

        private sealed class ControllerReceipt : IDisposable
        {
            private readonly Action _release;
            internal ControllerReceipt(Action release) { _release = release; }
            public void Dispose() { _release(); }
        }

        private static void TestControllerLayerOwnership()
        {
            object gameplay = new object(), planner = new object(), window = new object();
            object ownedBinding = new object(), nativeBinding = new object();
            var stack = new List<object> { gameplay, planner };
            int releases = 0;
            var owner = new ControllerLayerOwnership(planner, () => stack.Last(), new[] { ownedBinding });
            owner.Acquired(new ControllerReceipt(() => { releases++; stack.Remove(planner); }));
            Assert(owner.IsCurrent && owner.Allows(ownedBinding) && !owner.Allows(nativeBinding));
            // A native push supersedes us immediately. Removing our older
            // middle layer cannot restore a snapshot over that newer owner.
            stack.Add(window);
            Assert(!owner.IsCurrent && owner.Allows(nativeBinding));
            owner.Dispose();
            owner.Dispose();
            Assert(releases == 1 && stack.SequenceEqual(new[] { gameplay, window }));
            Assert(owner.Allows(nativeBinding));
        }

        private static MethodBase[] ControllerIlCalls(MethodBase method)
        {
            byte[] il = method.GetMethodBody().GetILAsByteArray();
            var calls = new List<MethodBase>();
            int position = 0;
            while (position < il.Length)
            {
                int op = il[position++];
                if (op == 0xfe) op = 0xfe00 | il[position++];
                int size;
                if (!IlOperandSize(op, il, position, out size))
                    throw new InvalidOperationException("Unrecognized native IL opcode.");
                if (op == 0x28 || op == 0x6f || op == 0x73)
                    calls.Add((MethodBase)method.Module.ResolveMember(BitConverter.ToInt32(il, position),
                        method.DeclaringType.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null));
                position += size;
            }
            return calls.ToArray();
        }

        private sealed class ControllerEntryBinding
        {
            internal int Action;
            internal Action Callback;
        }

        private static void TestControllerEntryLifecycle()
        {
            var layer = new object();
            var native = new ControllerEntryBinding { Action = 1, Callback = () => { } };
            var thirdParty = new ControllerEntryBinding { Action = 2, Callback = () => { } };
            var bindings = new List<ControllerEntryBinding> { native, thirdParty };
            var device = new List<ControllerEntryBinding>(bindings);
            int opens = 0, registrations = 0, removals = 0;
            var registry = new ControllerEntryRegistry<object, ControllerEntryBinding>((ownedLayer, binding) =>
            {
                Assert(ReferenceEquals(ownedLayer, layer));
                removals++;
                bindings.Remove(binding);
                device.Remove(binding);
            });
            Func<Func<bool>, ControllerEntryBinding[]> register = alive =>
            {
                registrations++;
                var binding = new ControllerEntryBinding { Action = 7, Callback = () => { if (alive()) opens++; } };
                bindings.Add(binding);
                device.Add(binding);
                return new[] { binding };
            };
            Func<bool> install = () => registry.Ensure(layer, bindings, b => b.Action == 7, register);
            Assert(install() && install() && registrations == 1);
            ControllerEntryBinding old = bindings.Last();
            old.Callback();
            Assert(opens == 1);
            registry.Clear();
            registry.Clear();
            old.Callback(); // a captured/in-flight callback also cannot reopen
            Assert(opens == 1 && removals == 1 && bindings.SequenceEqual(new[] { native, thirdParty }) &&
                device.SequenceEqual(bindings));
            Assert(install() && install() && registrations == 2);
            device.Last().Callback();
            Assert(opens == 2 && !device.Contains(old));
            // A later third-party binding on the same action must survive
            // unload, and must be recognized as an external conflict after it.
            var conflicting = new ControllerEntryBinding { Action = 7, Callback = () => { } };
            bindings.Add(conflicting);
            device.Add(conflicting);
            Assert(!install());
            registry.Clear();
            Assert(bindings.SequenceEqual(new[] { native, thirdParty, conflicting }) && device.SequenceEqual(bindings));
            Assert(!install() && registrations == 2 && removals == 2);
        }

        private sealed class ControllerNativeCallback
        {
            internal int Calls;
            public void Receive<T>(T data) { Calls++; }
        }

        private static void TestControllerNativeDetach()
        {
            // Execute the production collection/callback cleanup on actual
            // installed native layer/binding types. The layer is unbound:
            // detached inspection cannot provide a live Rewired Player.
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            Assembly.LoadFrom(Path.Combine(game, "Kingmaker_Data", "Managed", "UnityModManager", "0Harmony12.dll"));
            Assembly native = Assembly.LoadFrom(Path.Combine(game, "Kingmaker_Data", "Managed", "Assembly-CSharp.dll"));
            Assembly product = Assembly.LoadFrom(Path.Combine(FindRepositoryRoot(), "artifacts", "build", "Release", "KingmakerBuffPlanner.dll"));
            Type adapter = product.GetType("KingmakerBuffPlanner.UI.ControllerRoutineIntegration", true);
            Type layerType = native.GetType("Kingmaker.Assets.Console.GamepadInput.InputLayer", true);
            Type bindingType = native.GetType("Kingmaker.Assets.Console.GamepadInput.BindDescription", true);
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags statics = BindingFlags.Static | BindingFlags.NonPublic;
            FieldInfo bindsField = layerType.GetField("m_Binds", instance);
            FieldInfo actionsField = layerType.GetField("m_ActionsUsed", instance);
            adapter.GetField("_binds", statics).SetValue(null, bindsField);
            adapter.GetField("_actionsUsed", statics).SetValue(null, actionsField);
            object layer = Activator.CreateInstance(layerType);
            var bindings = (System.Collections.IList)bindsField.GetValue(layer);
            var actions = (System.Collections.IDictionary)actionsField.GetValue(layer);
            object owned = Activator.CreateInstance(bindingType), foreign = Activator.CreateInstance(bindingType);
            bindings.Add(owned);
            bindings.Add(foreign);
            bindingType.GetField("ActionId").SetValue(owned, 7);
            actions.Add(7, foreign); // newer owner, same action: do not clear it
            FieldInfo callbackField = bindingType.GetField("ActionHandler");
            Type dataType = callbackField.FieldType.GetGenericArguments().Single();
            var callback = new ControllerNativeCallback();
            Delegate handler = Delegate.CreateDelegate(callbackField.FieldType, callback,
                typeof(ControllerNativeCallback).GetMethod("Receive").MakeGenericMethod(dataType));
            callbackField.SetValue(owned, handler);
            object data = dataType.IsValueType ? Activator.CreateInstance(dataType) : null;
            handler.DynamicInvoke(data);
            Assert(callback.Calls == 1);
            MethodInfo detach = adapter.GetMethod("DetachEntry", statics);
            detach.Invoke(null, new[] { layer, owned });
            detach.Invoke(null, new[] { layer, owned });
            Assert(bindings.Count == 1 && ReferenceEquals(bindings[0], foreign) && ReferenceEquals(actions[7], foreign));
            ((Delegate)callbackField.GetValue(owned)).DynamicInvoke(data);
            Assert(callback.Calls == 1);
        }

        private static void TestControllerNativeContracts()
        {
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            Assembly assembly = Assembly.LoadFrom(Path.Combine(game, "Kingmaker_Data", "Managed", "Assembly-CSharp.dll"));
            Assert(assembly.ManifestModule.ModuleVersionId.ToString("D") == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type pad = assembly.GetType("Kingmaker.Assets.Console.GamepadInput.GamePad", true);
            Type layer = assembly.GetType("Kingmaker.Assets.Console.GamepadInput.InputLayer", true);
            Type menu = assembly.GetType("Kingmaker.UI._ConsoleUI.InGameMenu.BaseGameMenuView", true);
            Type binding = assembly.GetType("Kingmaker.Assets.Console.GamepadInput.BindDescription", true);
            Assert(pad.GetMethod("PushLayer", flags).ReturnType == typeof(IDisposable));
            string[] push = ControllerIlCalls(pad.GetMethod("PushLayer", flags)).Select(call => call.Name).ToArray();
            Assert(push.Contains("Unbind") && push.Contains("Bind"));
            string[] pop = ControllerIlCalls(pad.GetMethod("PopLayer", flags)).Select(call => call.Name).ToArray();
            Assert(pop.Contains("Contains") && pop.Contains("Remove") && pop.Contains("Bind"));
            MethodInfo getLayer = menu.GetMethod("GetInputLayer", flags);
            Assert(getLayer != null && getLayer.ReturnType == layer && !getLayer.IsPublic);
            string[] deselect = ControllerIlCalls(menu.GetMethod("Deselect", flags)).Select(call => call.Name).ToArray();
            Assert(deselect.SequenceEqual(new[] { "SetSelected", "SetCartsPositions", "get_ViewModel", "Confirm" }));
            // The base layer remains bound under a pushed native menu, hence
            // the production ownership prefix covers BindDescription.Handler.
            Assert(ControllerIlCalls(pad.GetMethod("SetBaseLayer", flags)).Any(call => call.Name == "Bind"));
            Assert(binding.GetMethod("Handler", flags).GetParameters().Single().ParameterType.FullName == "Rewired.InputActionEventData");
            Assert(layer.GetField("m_Binds", flags).FieldType.GetGenericArguments().Single() == binding);
            Assert(layer.GetMethods(flags).Any(method => method.Name == "AddButton" && method.IsPublic &&
                method.GetParameters().Length == 5 && method.GetParameters()[1].ParameterType == typeof(int)));
            Type actions = assembly.GetType("RewiredConsts.Action", true);
            foreach (string action in new[] { "Func01", "Confirm", "Decline", "DPadUp", "DPadDown", "DPadLeft", "DPadRight" })
                Assert(actions.GetField(action).IsLiteral);
        }

        private sealed class ControllerFixture
        {
            internal readonly string Path;
            internal CastingWorkspaceInputs Inputs;
            internal CastingWorkspaceSession Session;
            internal readonly CastingExecutionHost Host;
            internal readonly ScriptedInstantRuntime Instant = new ScriptedInstantRuntime("none", "none");
            internal readonly ScriptedAnimatedRuntime Animated = new ScriptedAnimatedRuntime("none", "none");
            internal readonly ControllerRoutineModel Menu;
            internal WorkspaceApplyResult LastApply;
            internal ExecutionProfile UsedSettings;
            internal int FreshReads;

            internal ControllerFixture(string root, string name, string mode = "instant", int count = 1,
                bool allRoutines = false)
            {
                Path = System.IO.Path.Combine(root, "controller-" + name);
                Directory.CreateDirectory(Path);
                PartyProviderSnapshot snapshot;
                Inputs = WorkspaceInputs(out snapshot);
                var author = new CastingWorkspaceSession(Path, "workspace-campaign");
                foreach (string routine in allRoutines ? ControllerRoutineModel.RoutineIds : new[] { "long" })
                {
                    for (int i = 0; i < count; i++)
                    {
                        author.SelectBuff("source-bulls");
                        author.SelectRoutine(routine);
                        author.BuildView(Inputs);
                        author.Draft.SourceId = "source-bulls";
                        author.Draft.TargetMode = CastingTargetMode.DirectTarget;
                        author.ChooseDraftCaster(routine == "important" ? "unit-wizard" : "unit-cleric");
                        author.Draft.DirectTargetUnitId = "unit-t" + (i + 1);
                        author.Draft.State = CastingAuthoringState.Ready;
                        Assert(author.AddCastingFromDraft(Inputs).Applied);
                    }
                }
                author.SetExecutionMode(mode);
                Host = new CastingExecutionHost(settings =>
                {
                    UsedSettings = settings;
                    return settings.Mode == "instant" ? (ICastExecutor)new InstantCastExecutor(Instant, true)
                        : new AnimatedCastExecutor(Animated, true);
                }, () => 0L);
                var owner = new CastingSessionOwner(campaign => new CastingWorkspaceSession(Path, campaign,
                    new NativeCastingDispatchBoundary(Host, () => Session.ExecutionSettings)),
                    new CastingWorkspaceRecoveryStore(), null);
                Assert(owner.Ensure("workspace-campaign", out Session) == null);
                // This fresh session has never built a workspace read model.
                // It loads the real saved file through the production owner.
                Assert(Session.LoadStatus == CastingPlanLoadStatus.Loaded);
                Menu = new ControllerRoutineModel(Session, null, Run,
                    () => Host.RequestStop(CastingExecutionHost.PlayerStopReason),
                    () => Host.IsRunning, () => Host.ActiveScopeRoutineId);
                Host.RunCompleted = report =>
                {
                    Session.RecordRunReport(report);
                    Menu.ShowResult(ControllerRoutineDetails.Report(report, Session));
                };
            }

            private bool Run(string routine)
            {
                FreshReads++;
                LastApply = CastingRunPolicy.Apply(Session, null, routine, Inputs);
                if (!LastApply.Allowed)
                {
                    string refusal = CastingRunPresentation.DescribeRefusal(Session.RoutineDisplayName(routine), LastApply, Session);
                    Session.RecordAttempt(refusal);
                    Menu.ShowResult(ControllerRoutineDetails.Refusal(refusal, LastApply, Session));
                }
                return true;
            }

            internal void Pump()
            {
                int limit = 1000;
                while (Host.IsRunning && limit-- > 0) Host.Pump();
                Assert(!Host.IsRunning);
            }
        }

        private static void TestControllerColdSession(string root)
        {
            for (int index = 0; index < 3; index++)
            {
                var fixture = new ControllerFixture(root, "cold-" + index, allRoutines: true);
                string routine = ControllerRoutineModel.RoutineIds[index];
                string signature = fixture.Session.DocumentIntentSignature();
                for (int i = 0; i < index; i++) fixture.Menu.Move(1);
                Assert(fixture.Host.StartedRuns == 0 && fixture.FreshReads == 0);
                fixture.Menu.Confirm();
                Assert(fixture.LastApply.Allowed && fixture.Host.ActiveScopeRoutineId == routine);
                PlannedCasting casting = fixture.Session.Document.Castings.Single(c => c.RoutineId == routine);
                CastStep step = fixture.LastApply.Projection.Plan.Steps.Single();
                Assert(step.AssignmentId == casting.CastingId && step.TargetUnitIds.Single() == casting.DirectTargetUnitId &&
                    step.Provider.CasterUnitId == casting.CasterUnitId &&
                    step.Provider.SpellbookGuid == casting.SpellbookGuid &&
                    step.Provider.Ability.Canonical == casting.Ability.Canonical);
                fixture.Pump();
                Assert(fixture.Host.LastReport.Confirmed == 1 && fixture.Host.LastReport.ResourcesSpent == 1);
                Assert(fixture.Instant.Fired.SequenceEqual(new[] { casting.CastingId }));
                Assert(fixture.Session.DocumentIntentSignature() == signature);
            }
        }

        private static void TestControllerBrowsing(string root)
        {
            var fixture = new ControllerFixture(root, "browse", allRoutines: true);
            var repository = new CastingPlanRepository(fixture.Path);
            string path = repository.GetProfilePath(fixture.Session.CampaignId);
            string bytes = File.ReadAllText(path);
            DateTime write = File.GetLastWriteTimeUtc(path);
            CastingPlanDocument document = fixture.Session.Document;
            string selection = fixture.Session.SelectedRoutineId;
            for (int i = 0; i < 100; i++)
            {
                fixture.Menu.Move(i % 2 == 0 ? -1 : 1);
                Assert(fixture.Menu.Row(i % 4).Length > 0 && fixture.Menu.Settings.Length > 0);
            }
            Assert(ReferenceEquals(document, fixture.Session.Document) && selection == fixture.Session.SelectedRoutineId);
            Assert(bytes == File.ReadAllText(path) && write == File.GetLastWriteTimeUtc(path));
            Assert(fixture.Host.StartedRuns == 0 && fixture.FreshReads == 0 && fixture.Instant.Fired.Count == 0);
        }

        private static void TestControllerDurability(string root)
        {
            var fixture = new ControllerFixture(root, "durability");
            string path = new CastingPlanRepository(fixture.Path).GetProfilePath("workspace-campaign");
            string baseline = File.ReadAllText(path);
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert(AddDraftCasting(fixture.Session, fixture.Inputs, "unit-wizard", "unit-t2").Applied);
                fixture.Menu.Confirm();
                Assert(!fixture.LastApply.Allowed && fixture.LastApply.ReviewReason.StartsWith("persistence-failed", StringComparison.Ordinal));
                Assert(fixture.Host.StartedRuns == 0 && fixture.Instant.Fired.Count == 0 && File.ReadAllText(path) == baseline);
            }
            fixture.Menu.Confirm();
            Assert(fixture.LastApply.Allowed);
            Assert(fixture.LastApply.Projection.Plan.Steps.Count == 2);
            Assert(fixture.LastApply.Projection.Plan.Steps[1].TargetUnitIds.Single() == "unit-t2");
            fixture.Pump();
            Assert(fixture.Host.LastReport.Confirmed == 2 && fixture.Host.LastReport.ResourcesSpent == 2);
        }

        private static void TestControllerPolicies(string root)
        {
            foreach (string mode in new[] { "instant", "animated" })
            {
                var fixture = new ControllerFixture(root, "policy-" + mode, mode, count: 2);
                PartyProviderSnapshot snapshot;
                fixture.Inputs = WorkspaceInputs(out snapshot, remainingPerCaster: 1);
                fixture.Menu.Confirm();
                Assert(!fixture.LastApply.Allowed && fixture.Host.StartedRuns == 0);
                fixture.Session.SetAvailabilityPolicy(ExecutionProfile.CastAvailable);
                fixture.Menu.Confirm();
                Assert(fixture.LastApply.Allowed && fixture.UsedSettings.Mode == mode &&
                    fixture.Host.ActiveScopeRoutineId == "long");
                fixture.Pump();
                Assert(fixture.Host.LastReport.Confirmed == 1 && fixture.Host.LastReport.Omitted == 1 &&
                    fixture.Host.LastReport.ResourcesSpent == 1);
                var combat = new ControllerFixture(root, "combat-" + mode, mode);
                combat.Inputs = PolicyInputs(combat.Inputs, true);
                combat.Menu.Confirm();
                Assert(!combat.LastApply.Allowed && combat.LastApply.ReviewReason == CastingExecutionPolicy.CombatActive);
                Assert(combat.Host.StartedRuns == 0 && combat.Instant.Fired.Count == 0 && combat.Animated.Started.Count == 0);
            }
            var empty = new ControllerFixture(root, "empty", count: 0);
            Assert(empty.Menu.Focus == 4 && empty.Menu.ConfirmHint == "Open planner");
            empty.Menu.Confirm();
            Assert(empty.Menu.ConfigureRequested && empty.LastApply == null && empty.Host.StartedRuns == 0);
            FocusController(empty.Menu, 0);
            empty.Menu.Confirm();
            Assert(empty.LastApply == null && empty.Menu.Message.Contains("no configured castings"));
        }

        private static void TestControllerStop(string root)
        {
            var fixture = new ControllerFixture(root, "stop", "animated", count: 2);
            fixture.Menu.Confirm();
            Assert(fixture.Menu.Focus == 3 && fixture.Host.IsRunning);
            fixture.Host.Pump();
            Assert(fixture.Host.ActiveCastingInFlight);
            // A Run row while active is a refusal, not a second spend or an
            // implicit Stop. Stop is a separate deliberate action.
            FocusController(fixture.Menu, 0);
            fixture.Menu.Confirm();
            Assert(fixture.Host.StartedRuns == 1 && fixture.Host.ActiveStopRequested == null);
            FocusController(fixture.Menu, 3);
            fixture.Menu.Confirm();
            Assert(fixture.Host.IsRunning && fixture.Host.ActiveStopRequested == CastingExecutionHost.PlayerStopReason);
            fixture.Pump();
            Assert(fixture.Animated.Started.Count == 1 && fixture.Host.LastReport.Confirmed == 1 &&
                fixture.Host.LastReport.NotProcessed == 1 && fixture.Host.LastReport.Cancelled);
            fixture.Menu.Confirm();
            Assert(fixture.Host.StartedRuns == 1 && fixture.Menu.Message.Contains("no active routine"));
        }

        private static void TestControllerDetails(string root)
        {
            var fixture = new ControllerFixture(root, "details", count: 2);
            PartyProviderSnapshot snapshot;
            fixture.Inputs = WorkspaceInputs(out snapshot, remainingPerCaster: 1);
            fixture.Menu.Confirm();
            foreach (CastingBlocker blocker in fixture.LastApply.BlockingCastings)
                Assert(fixture.Menu.Message.Contains(fixture.Session.CastingLabel(blocker.CastingId)));
            Assert(fixture.Menu.Message.Contains("mouse/keyboard editor"));
            fixture.Session.SetAvailabilityPolicy(ExecutionProfile.CastAvailable);
            fixture.Menu.Confirm();
            fixture.Pump();
            foreach (CastingOutcomeEntry entry in fixture.Host.LastReport.Entries)
                Assert(fixture.Menu.Message.Contains(CastingRunPresentation.DescribeEntry(entry)));
            Assert(fixture.Menu.Message.Contains("left out of the run"));
        }

        private static void TestControllerImmediateStop(string root)
        {
            var fixture = new ControllerFixture(root, "immediate-stop", "animated", count: 2);
            fixture.Menu.Confirm();
            fixture.Menu.Confirm();
            Assert(!fixture.Host.IsRunning && fixture.Host.LastReport.Cancelled);
            Assert(fixture.Animated.Started.Count == 0 && fixture.Host.LastReport.ResourcesSpent == 0);
            Assert(fixture.Menu.Message == ControllerRoutineDetails.Report(fixture.Host.LastReport, fixture.Session));
        }

        private static void TestControllerUnavailable()
        {
            int runs = 0, stops = 0;
            const string reason = "Classic is selected. Configure the casting-first planner with mouse and keyboard.";
            var menu = new ControllerRoutineModel(null, reason, _ => { runs++; return true; },
                () => stops++, () => false, () => null);
            for (int i = 0; i < 3; i++) { menu.Confirm(); Assert(menu.Message == reason); menu.Move(1); }
            menu.Confirm();
            Assert(runs == 0 && stops == 0 && menu.Message.Contains("no active routine"));
        }

        private static void TestControllerProductionWiring()
        {
            string root = WithoutComments(ProductionSource("UI", "BuffPlannerUiRoot.cs"));
            string open = MemberBody(root, "private void OpenControllerQuick(");
            Assert(open.Contains("EnsureCastingSession(out session, refresh)") &&
                open.Contains("StartCastingFirstRoutine(routine, root.PresentQuickResult, null, false)") &&
                !open.Contains("OpenSetup(") && !open.Contains("new CastingWorkspaceScreenView"));
            string start = MemberBody(root, "private bool StartCastingFirstRoutine(");
            Assert(start.Contains("CastingRunPolicy.Apply(session, requestedMode, routineId, inputs)") &&
                start.Contains("if (openEditorOnRefusal && CastingRunPresentation.OpensPlanner(result)"));
            string integration = WithoutComments(ProductionSource("UI", "ControllerRoutineIntegration.cs"));
            Assert(integration.Contains("bind.ActionId == RewiredConsts.Action.Func01") &&
                integration.Contains("InputActionEventType.ButtonJustPressed"));
        }

        private static void TestControllerCorruptPlan(string root)
        {
            string dir = Path.Combine(root, "controller-corrupt");
            Directory.CreateDirectory(dir);
            var repository = new CastingPlanRepository(dir);
            string path = repository.GetProfilePath("workspace-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{ malformed owned fixture");
            string bytes = File.ReadAllText(path);
            var boundary = new DisabledCastingDispatchBoundary();
            var session = new CastingWorkspaceSession(dir, "workspace-campaign", boundary);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            WorkspaceApplyResult result = null;
            ControllerRoutineModel menu = null;
            menu = new ControllerRoutineModel(session, null, routine =>
            {
                result = CastingRunPolicy.Apply(session, null, routine, inputs);
                menu.ShowResult(ControllerRoutineDetails.Refusal(CastingRunPresentation.DescribeRefusal(
                    "Long", result, session), result, session));
                return true;
            }, () => { throw new InvalidOperationException("Unexpected Stop."); }, () => false, () => null);
            Assert(session.PersistenceBlocked && menu.Message.Contains("could not be loaded"));
            FocusController(menu, 0); // explicitly choose Run Long; default is recovery access
            menu.Confirm();
            Assert(!result.Allowed && boundary.RecordedSubmissions.Count == 0 && File.ReadAllText(path) == bytes);
            Assert(menu.Message.Contains("mouse/keyboard editor"));
        }

        private static ControllerRoutineModel ReopenController(ControllerFixture fixture)
        {
            return new ControllerRoutineModel(fixture.Session, null, _ => false, () => { },
                () => fixture.Host.IsRunning, () => fixture.Host.ActiveScopeRoutineId);
        }

        private static void TestControllerLatestOutcome(string root)
        {
            var fixture = new ControllerFixture(root, "latest-outcome");
            fixture.Menu.Confirm();
            fixture.Pump();
            CastingRunReport first = fixture.Session.LastRunReport;
            string success = ControllerRoutineDetails.Report(first, fixture.Session);
            // Model reconstruction is the real presentation used on reopen,
            // not a test-only call to ShowResult.
            Assert(ReopenController(fixture).Message == success);
            FocusController(fixture.Menu, 0);
            fixture.Inputs = PolicyInputs(fixture.Inputs, true);
            fixture.Menu.Confirm();
            Assert(!fixture.LastApply.Allowed);
            string refusal = CastingRunPresentation.DescribeRefusal("Long", fixture.LastApply, fixture.Session);
            Assert(fixture.Session.LastAttemptMessage == refusal);
            var reopened = ReopenController(fixture);
            Assert(ReferenceEquals(fixture.Session.LastRunReport, first));
            Assert(reopened.Message.Contains(refusal) && reopened.Message != success &&
                reopened.Message.Contains("mouse/keyboard editor"));
            fixture.Inputs = PolicyInputs(fixture.Inputs, false);
            fixture.Menu.Confirm();
            fixture.Pump();
            Assert(fixture.Session.LastAttemptMessage == null && !ReferenceEquals(first, fixture.Session.LastRunReport));
            Assert(ReopenController(fixture).Message ==
                ControllerRoutineDetails.Report(fixture.Session.LastRunReport, fixture.Session));
        }

        private static void TestControllerPersistencePresentation(string root)
        {
            foreach (string state in new[] { "corrupt", "unsupported", "legacy-unresolved", "backup" })
            {
                string dir = Path.Combine(root, "controller-persistence-" + state);
                var repository = new CastingPlanRepository(dir);
                const string campaign = "controller-persistence";
                string path = state == "legacy-unresolved"
                    ? new ProfileRepository(dir).GetProfilePath(campaign) : repository.GetProfilePath(campaign);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (state == "backup")
                {
                    repository.Save(CastingPlanProfile.FromDocument(new CastingPlanDocument(campaign,
                        new[] { new RoutineDefinition("long", "Long") }, new PlannedCasting[0])));
                    File.Copy(path, path + ".bak1");
                }
                File.WriteAllText(path, state == "unsupported" ? "{\"schemaVersion\":999}" : "{ unreadable owned fixture");
                var session = new CastingWorkspaceSession(dir, campaign, new DisabledCastingDispatchBoundary());
                Assert(session.SavesRefused);
                if (state == "legacy-unresolved") Assert(session.LegacyImportBlocked);
                if (state == "backup") Assert(session.LoadStatus == CastingPlanLoadStatus.RecoveredFromBackup);
                string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();
                string[] contents = files.Select(File.ReadAllText).ToArray();
                DateTime[] writes = files.Select(File.GetLastWriteTimeUtc).ToArray();
                var menu = new ControllerRoutineModel(session, null, _ => { throw new Exception("Unexpected Run"); },
                    () => { throw new Exception("Unexpected Stop"); }, () => false, () => null);
                for (int i = 0; i < 12; i++)
                {
                    menu.Move(1);
                    Assert(menu.Settings.EndsWith(" | Not saved", StringComparison.Ordinal));
                    Assert(menu.Message.Contains(WorkspaceFooterText.SaveProblem(session.FooterSaveState)));
                }
                Assert(files.SequenceEqual(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).OrderBy(p => p)));
                Assert(contents.SequenceEqual(files.Select(File.ReadAllText)) && writes.SequenceEqual(files.Select(File.GetLastWriteTimeUtc)));
            }
        }

        private static void TestControllerConfigure(string root)
        {
            var fixture = new ControllerFixture(root, "configure", count: 0);
            Assert(fixture.Menu.Focus == 4 && fixture.Menu.Summary.Contains("Configure plans"));
            fixture.Menu.Confirm();
            Assert(fixture.Menu.ConfigureRequested && fixture.Host.StartedRuns == 0 && fixture.FreshReads == 0);
            // The authoring surface uses THIS existing session; no parallel
            // storage or disk reload. Exercise real authoring and autosave.
            Assert(AddDraftCasting(fixture.Session, fixture.Inputs, "unit-wizard", "unit-t1").Applied);
            var returned = new ControllerRoutineModel(fixture.Session, null, id =>
            {
                fixture.LastApply = CastingRunPolicy.Apply(fixture.Session, null, id, fixture.Inputs);
                return true;
            }, () => { }, () => fixture.Host.IsRunning, () => fixture.Host.ActiveScopeRoutineId);
            Assert(returned.Row(0).Contains("1 castings") && returned.Settings.EndsWith("Saved"));
            Assert(!returned.Available(1) && !returned.Available(2));
            returned.Confirm(); fixture.Pump();
            Assert(fixture.LastApply.Allowed && fixture.Host.LastReport.Confirmed == 1 && fixture.Host.LastReport.ResourcesSpent == 1);
            var loaded = new CastingWorkspaceSession(fixture.Path, fixture.Session.CampaignId);
            Assert(loaded.Document.Castings.Count == 1 && loaded.Document.Castings[0].CasterUnitId == "unit-wizard");
        }

        private static void TestControllerResultsNavigation(string root)
        {
            var fixture = new ControllerFixture(root, "results-navigation");
            FocusController(fixture.Menu, 5);
            Assert(fixture.Menu.Focus == 5 && fixture.Menu.ConfirmHint == "View results");
            fixture.Menu.Confirm();
            for (int i = 0; i < 20; i++) { fixture.Menu.Move(1); fixture.Menu.Confirm(); }
            Assert(fixture.Menu.Focus == 5 && fixture.Host.StartedRuns == 0 && fixture.FreshReads == 0);
            Assert(fixture.Menu.CloseResults() && !fixture.Menu.CloseResults());
            FocusController(fixture.Menu, 4); fixture.Menu.Confirm();
            Assert(fixture.Menu.ConfigureRequested);
            var running = new ControllerFixture(root, "configure-running", "animated");
            running.Menu.Confirm(); FocusController(running.Menu, 4); running.Menu.Confirm();
            Assert(running.Host.IsRunning && !running.Menu.ConfigureRequested && running.Menu.Message.Contains("Stop"));
        }

        private static void TestControllerAuthoringContracts()
        {
            string managed = Path.Combine(Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH"), "Kingmaker_Data", "Managed");
            Assembly tmp = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp-firstpass.dll"));
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type text = tmp.GetType("TMPro.TMP_Text", true);
            Type ui = tmp.GetType("TMPro.TextMeshProUGUI", true);
            Assert(ControllerIlCalls(ui.GetMethod("Awake", flags)).Any(m => m.Name == "LoadDefaultSettings"));
            Assert(ControllerIlCalls(text.GetMethod("LoadDefaultSettings", flags)).Any(m => m.Name == "set_sizeDelta"));
            Assembly game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            Type pointer = game.GetType("Rewired.Integration.UnityUI.RewiredPointerInputModule", true);
            Assert(ControllerIlCalls(pointer.GetMethod("GetMouseInputSource", flags)).Any(m => m.Name == "get_defaultMouseInputSource"));
            Type module = game.GetType("Rewired.Integration.UnityUI.RewiredStandaloneInputModule", true);
            Assert(ControllerIlCalls(module.GetProperty("PlayerMice").GetSetMethod()).Any(m => m.Name == "SetupRewiredVars"));
        }

        private static void FocusController(ControllerRoutineModel menu, int focus)
        {
            for (int moves = 0; menu.Focus != focus && moves < 6; moves++) menu.Move(1);
            Assert(menu.Focus == focus);
        }

        private static void TestControllerBookHandoff()
        {
            int closes = 0, opens = 0;
            bool source = true, released = false;
            var request = new ControllerSurfaceHandoff(() => true, () => source,
                () => { closes++; source = false; }, () => released, () => opens++);
            for (int frame = 0; frame < 10; frame++) Assert(!request.Tick(false, frame));
            Assert(closes == 0 && opens == 0);
            Assert(!request.Tick(true, 10) && !request.Tick(true, 11));
            Assert(closes == 1 && opens == 0);
            for (int frame = 12; frame < 30; frame++) Assert(!request.Tick(true, frame));
            Assert(opens == 0); // neither elapsed time nor source removal grants ownership
            released = true;
            Assert(request.Tick(true, 30) && request.Tick(true, 31));
            Assert(closes == 1 && opens == 1);
        }

        private static void TestControllerModeQueue()
        {
            int own = 0, foreign = 0;
            Action owned = () => own++, other = () => foreign++;
            var queue = new List<Action> { other, owned, other };
            var pending = new ControllerQueuedModeAction(queue, owned);
            Assert(pending.Pending);
            pending.Cancel(); pending.Cancel();
            Assert(!pending.Pending && queue.Count == 2 && queue.All(action => ReferenceEquals(action, other)));
            queue.ForEach(action => action());
            Assert(own == 0 && foreign == 2);
            queue.Insert(1, owned);
            bool stillOwns = true;
            Assert(pending.Guard(() => stillOwns));
            stillOwns = false; // native window takes over AFTER Stop was queued
            queue.ForEach(action => action());
            Assert(own == 0 && foreign == 4);
            stillOwns = true;
            queue.ForEach(action => action());
            Assert(own == 1 && foreign == 6);
            Assert(!pending.Guard(() => true)); // no longer the exact pending identity
        }

        private static void TestControllerTextBack()
        {
            var latch = new ControllerActivationLatch();
            var back = new ControllerBackIntent();
            int texts = 0, windows = 0;
            latch.Observe(true, 1);
            Assert(back.Request(latch, 2, true));
            // Native binding and keyboard polling cannot independently accept
            // this frame. Later field cancellation must not change its intent.
            Assert(!back.Request(latch, 2, false) && !back.Request(latch, 3, false));
            for (int frame = 3; frame < 8; frame++) back.Complete(latch, false, frame, () => texts++, () => windows++);
            back.Complete(latch, true, 8, () => texts++, () => windows++);
            back.Complete(latch, true, 9, () => texts++, () => windows++);
            back.Complete(latch, true, 10, () => texts++, () => windows++);
            Assert(texts == 1 && windows == 0);
            Assert(back.Request(latch, 11, false));
            back.Complete(latch, true, 12, () => texts++, () => windows++);
            back.Complete(latch, true, 13, () => texts++, () => windows++);
            Assert(texts == 1 && windows == 1);
        }

        private static void TestControllerBookHandoffCancel()
        {
            foreach (string failure in new[] { "context", "source", "timeout", "cancel" })
            {
                int opens = 0;
                bool valid = true;
                var request = new ControllerSurfaceHandoff(() => valid, () => failure != "source",
                    () => { }, () => false, () => opens++);
                request.Tick(true, 0);
                if (failure == "context") valid = false;
                if (failure == "cancel") request.Cancel();
                bool complete = false;
                for (int frame = 1; frame < 200; frame++) complete |= request.Tick(true, frame);
                Assert(complete && opens == 0);
            }
        }

        private static void TestControllerBookContracts()
        {
            string gamePath = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            Assembly game = Assembly.LoadFrom(Path.Combine(gamePath, "Kingmaker_Data", "Managed", "Assembly-CSharp.dll"));
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type view = game.GetType("Kingmaker.UI._ConsoleUI.SpellBook.SpellBookView", true);
            Assert(view.GetField("IsShowed", flags).FieldType == typeof(bool));
            Assert(ControllerIlCalls(view.GetMethod("Hide", flags)).Any(call => call.Name == "PopLayer"));
            Type vm = game.GetType("Kingmaker.UI._ConsoleUI.SpellBook.SpellBookVM", true);
            Assert(ControllerIlCalls(vm.GetMethod("Back", flags)).Any(call => call.Name == "Dispose"));
            Type production = Assembly.LoadFrom(Path.Combine(FindRepositoryRoot(), "artifacts", "build", "Release", "KingmakerBuffPlanner.dll"))
                .GetType("KingmakerBuffPlanner.UI.ControllerSpellbookIntegration", true);
            MethodInfo select = production.GetMethod("ChooseAction", BindingFlags.Static | BindingFlags.NonPublic);
            Assert((int)select.Invoke(null, new object[] { new[] { 11 } }) == 19);
            Assert((int)select.Invoke(null, new object[] { new[] { 11, 19 } }) == 17);
            Assert(select.Invoke(null, new object[] { new[] { 19, 17 } }) == null);
        }
    }
}
