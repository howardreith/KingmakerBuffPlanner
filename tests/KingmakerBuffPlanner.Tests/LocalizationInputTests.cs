using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private static void RunLocalizationInputTests(string root)
        {
            Run("localization-input-refresh-enabled", () => TestRefreshOwnership(root, true, null));
            Run("localization-input-refresh-disabled", () => TestRefreshOwnership(root, false, null));
            Run("localization-input-refresh-build-failure", () => TestRefreshOwnership(root, true, "build"));
            Run("localization-input-refresh-restore-failure", () => TestRefreshOwnership(root, false, "restore"));
            foreach (string command in new[] { "Confirm", "Inspect", "PreviousRoutine", "NextRoutine" })
            {
                Run("localization-input-composition-" + command, () => TestCompositionCommand(command));
            }
        }

        // Only construction of Unity's rendered hierarchy is substituted. Use the shipped
        // navigator, root's replacement/close paths and the installed EventSystem property.
        private static void TestRefreshOwnership(string root, bool initial, string failure)
        {
            var product = ViewInputProduct();
            Type viewType = product.GetType("KingmakerBuffPlanner.UI.CastingWorkspaceScreenView", true);
            InvokeInputFixture("ExerciseViewSwap", viewType, product, root, initial, failure);
        }

        private static void ExerciseViewSwap<TView>(Assembly product, string root, bool initial, string failure)
            where TView : class
        {
            var ui = Assembly.LoadFrom(Path.Combine(Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH"),
                "Kingmaker_Data", "Managed", "UnityEngine.UI.dll"));
            Type eventsType = ui.GetType("UnityEngine.EventSystems.EventSystem", true);
            object events = DetachedViewObject(eventsType);
            PropertyInfo navigation = eventsType.GetProperty("sendNavigationEvents");
            navigation.SetValue(events, initial, null);
            var app = DetachedViewObject(product.GetType("KingmakerBuffPlanner.UI.BuffPlannerUiRoot", true));
            var session = ProductSession(product, Path.Combine(root, Guid.NewGuid().ToString("N")));
            ViewInputCall(session, "SelectRoutine", "short");
            var sessionType = session.GetType();
            string intent = (string)ViewInputCall(session, "DocumentIntentSignature");
            var views = new List<TView>();
            Func<TView> construct = () =>
            {
                if (views.Count > 0) Expect((bool)typeof(TView).GetField("_disposed", ViewInputMembers)
                    .GetValue(views[views.Count - 1]), "Two views would own input during replacement.");
                Expect((bool)navigation.GetValue(events, null) == initial, "Prior navigator still owns input at construction.");
                var view = (TView)DetachedViewObject(typeof(TView));
                object nav = DetachedViewObject(product.GetType("KingmakerBuffPlanner.UI.ControllerWorkspaceNavigator", true));
                ViewInputCall(nav, "CaptureKeyboardNavigation", events);
                SetNative(view, "_controllerNavigation", nav);
                SetNative(view, "_session", session);
                views.Add(view);
                return view;
            };
            SetNative(app, "_castingWorkspace", construct());
            var current = app.GetType().GetField("_castingWorkspace", ViewInputMembers);
            for (int i = 0; i < 3; i++)
            {
                bool inject = failure != null && i == 2;
                Func<TView> build = () =>
                {
                    if (inject && failure == "build") throw new ApplicationException("injected build");
                    return construct();
                };
                Action<TView> restore = view =>
                {
                    Expect(ReferenceEquals(view, current.GetValue(app)), "Replacement is not installed.");
                    if (inject && failure == "restore") throw new ApplicationException("injected restore");
                };
                try { ViewInputCall(app, "ReplaceWorkspaceForLanguage", build, restore); }
                catch (InvalidOperationException ex)
                {
                    if (!inject || !(ex.InnerException is TargetInvocationException) ||
                        !(ex.InnerException.InnerException is ApplicationException)) throw;
                }
                if (!inject) Expect(current.GetValue(app) == views[views.Count - 1] &&
                    !(bool)navigation.GetValue(events, null), "Active replacement lost semantic input isolation.");
            }
            if (failure != null) Expect(current.GetValue(app) == null, "Failed rebuild retained a workspace.");
            ViewInputCall(app, "CloseCastingWorkspace");
            Expect((bool)navigation.GetValue(events, null) == initial, "Closing changed the original native navigation setting.");
            foreach (var view in views) Expect((bool)typeof(TView).GetField("_disposed", ViewInputMembers).GetValue(view),
                "A navigator's workspace survived cleanup.");
            Expect((string)sessionType.GetProperty("SelectedRoutineId").GetValue(session, null) == "short" &&
                intent == (string)ViewInputCall(session, "DocumentIntentSignature"), "Language refresh changed authoring state.");
        }

        private static void TestCompositionCommand(string name)
        {
            Assembly product = ViewInputProduct();
            Type type = product.GetType("KingmakerBuffPlanner.UI.ControllerEditorInputBoundary", true);
            Type commandType = product.GetType("KingmakerBuffPlanner.UI.ControllerWorkspaceCommand", true);
            InvokeInputFixture("ExerciseComposition", commandType, type, name);
        }

        private static void InvokeInputFixture(string method, Type argument, params object[] values)
        {
            try
            {
                typeof(Program).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(argument).Invoke(null, values);
            }
            catch (TargetInvocationException error)
            {
                throw new InvalidOperationException(error.InnerException.ToString(), error.InnerException);
            }
        }

        private static int _lastCompositionFixtureFrame;

        private static void ExerciseComposition<TCommand>(Type type, string name) where TCommand : struct
        {
            var boundary = Activator.CreateInstance(type, ViewInputMembers, null, new object[] { (Action)(() => {}) }, null);
            var received = new List<TCommand>();
            type.GetProperty("Command", ViewInputMembers).SetValue(boundary, (Action<TCommand>)received.Add, null);
            var command = (TCommand)Enum.Parse(typeof(TCommand), name);
            var direction = (TCommand)Enum.Parse(typeof(TCommand), "Down");
            Type guard = type.Assembly.GetType("KingmakerBuffPlanner.UI.PlannerImeGuard", true);
            var observe = (Func<bool, int, bool>)Delegate.CreateDelegate(typeof(Func<bool, int, bool>),
                guard.GetMethod("ObservePreedit", BindingFlags.Static | BindingFlags.NonPublic));
            // Each detached input stream uses a later epoch on the guard's shared frame clock.
            int frame = _lastCompositionFixtureFrame + 10;
            Action<bool, bool, TCommand?> tick = (preedit, neutral, held) =>
            {
                frame++;
                ViewInputCall(boundary, "DispatchInput", observe(preedit, frame), neutral, frame, frame * .2f, held);
            };
            Action<bool> press = preedit => ViewInputCall(boundary, "CaptureCommand", command, observe(preedit, frame), frame);
            tick(false, true, null);
            tick(false, true, null);
            press(false);                         // Callback immediately before composition begins.
            tick(true, false, direction);         // Start preedit with an already pending command.
            press(true);
            tick(true, true, null);               // Released during composition.
            tick(false, true, null);              // Preedit committed; its grace frame still suppresses callbacks.
            press(false);
            tick(false, true, null);
            tick(false, true, null);
            Expect(received.Count == 0, "Command survived commit and release: " + name);

            press(true);                          // Callback observes a new preedit before Tick.
            tick(false, false, direction);        // Preedit canceled; keep button and direction held through grace.
            press(false);
            tick(false, false, direction);
            press(false);
            tick(false, false, direction);
            Expect(received.Count == 0, "Held input escaped suppression after cancel: " + name);
            tick(false, true, null);              // The neutral observation itself cannot activate.
            press(false);
            tick(false, true, null);
            Expect(received.Count == 0, "The rearming frame dispatched stale input: " + name);
            press(true);                          // Entire composition observed by callbacks between ticks.
            frame += 2;
            tick(false, false, null);
            Expect(received.Count == 0, "Callback captured a command during preedit: " + name);
            tick(false, true, null);
            tick(false, true, null);
            press(false);
            tick(false, false, null);
            Expect(received.Count == 1 && received[0].Equals(command), "New command did not dispatch exactly once: " + name);
            tick(false, true, null);
            tick(false, false, direction);
            Expect(received.Count == 2 && received[1].Equals(direction), "Normal navigation did not resume.");
            _lastCompositionFixtureFrame = frame;
        }
    }
}
