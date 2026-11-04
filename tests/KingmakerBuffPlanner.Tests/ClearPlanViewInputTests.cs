using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private const BindingFlags ViewInputMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static void RunClearPlanViewInputTests(string root)
        {
            Run("clear-plan-view-focus-loss-with-confirmation-open", () => TestClearViewTeardown(root, false));
            Run("clear-plan-view-focus-loss-during-release-wait", () => TestClearViewTeardown(root, true));
            Run("clear-plan-view-description-blocks-enter-and-closing-edge", () => TestDescriptionKeyboard(root, KeyCode.Return));
            Run("clear-plan-view-description-blocks-space-and-closing-edge", () => TestDescriptionKeyboard(root, KeyCode.Space));
            Run("clear-plan-view-description-blocks-semantic-navigation", () => TestDescriptionNavigation(root));
        }

        private static Assembly ViewInputProduct()
        {
            string managed = Path.Combine(Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH"), "Kingmaker_Data", "Managed");
            Assembly.LoadFrom(Path.Combine(managed, "UnityModManager", "UnityModManager.dll"));
            Assembly.LoadFrom(Path.Combine(managed, "UnityModManager", "0Harmony12.dll"));
            return Assembly.LoadFrom(Path.Combine(FindRepositoryRoot(), "artifacts", "build", "Release", "KingmakerBuffPlanner.dll"));
        }

        private static object DetachedViewObject(Type type)
        {
            object value = FormatterServices.GetUninitializedObject(type);
            if (value is UnityEngine.Object) SetNative(value, "m_CachedPtr", new IntPtr(1));
            return value;
        }

        private static object ViewInputCall(object target, string name, params object[] arguments)
        {
            try { return target.GetType().GetMethod(name, ViewInputMembers).Invoke(target, arguments); }
            catch (TargetInvocationException exception)
            {
                throw new InvalidOperationException(target.GetType().Name + "." + name + ": " +
                    (exception.InnerException ?? exception), exception);
            }
        }

        private static object ProductSession(Assembly product, string dir)
        {
            return Activator.CreateInstance(product.GetType("KingmakerBuffPlanner.UI.CastingWorkspaceSession", true),
                new object[] { dir, "clear-campaign", null, null, null, null, null });
        }

        // Execute the real application's focus-loss callback -> input lease ->
        // controller boundary -> view Dispose sequence. Only native rendering,
        // gamepad acquisition and the captured EventSystem are detached.
        private static void TestClearViewTeardown(string root, bool releaseWait)
        {
            string dir;
            ClearFixture(root, releaseWait ? "view-wait" : "view-open", out dir);
            Assembly product = ViewInputProduct();
            Type eventsType = Assembly.LoadFrom(Path.Combine(Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH"),
                "Kingmaker_Data", "Managed", "UnityEngine.UI.dll")).GetType("UnityEngine.EventSystems.EventSystem", true);
            foreach (bool original in new[] { true, false })
            {
                object events = DetachedViewObject(eventsType);
                PropertyInfo navigation = eventsType.GetProperty("sendNavigationEvents");
                navigation.SetValue(events, original, null);
                object boundary = DetachedViewObject(product.GetType("KingmakerBuffPlanner.UI.ControllerEditorInputBoundary", true));
                SetNative(boundary, "_events", events);
                SetNative(boundary, "_navigation", original);
                navigation.SetValue(events, false, null); // editor owns navigation
                object session = ProductSession(product, dir);
                object view = DetachedViewObject(product.GetType("KingmakerBuffPlanner.UI.CastingWorkspaceScreenView", true));
                SetNative(view, "_session", session);
                SetNative(view, "_controllerAuthoring", true);
                SetNative(view, "_clearEvents", events);
                // The old implementation captured the editor's intermediate false.
                FieldInfo oldCapture = view.GetType().GetField("_clearPriorNavigation", ViewInputMembers);
                if (oldCapture != null) oldCapture.SetValue(view, false);
                object gate = Activator.CreateInstance(product.GetType("KingmakerBuffPlanner.UI.WorkspaceModalInputGate", true), true);
                ViewInputCall(gate, "Open", 10);
                if (releaseWait) ViewInputCall(gate, "Close", 11);
                SetNative(view, "_clearInput", gate);
                ViewInputCall(session, "BeginClearPlan", new object[] { null });
                object lease = Activator.CreateInstance(product.GetType("KingmakerBuffPlanner.UI.BuffPlannerInputLease", true),
                    ViewInputMembers, null, new[] { boundary, (object)null }, null);
                object app = DetachedViewObject(product.GetType("KingmakerBuffPlanner.UI.BuffPlannerUiRoot", true));
                SetNative(app, "_workspaceInputLease", lease);
                SetNative(app, "_controllerEditor", boundary);
                SetNative(app, "_castingWorkspace", view);
                ViewInputCall(app, "OnApplicationFocus", false);
                Expect((bool)navigation.GetValue(events, null) == original,
                    "focus-loss teardown overwrote original EventSystem navigation (release-wait=" + releaseWait + ")");
                Expect((bool)view.GetType().GetField("_disposed", ViewInputMembers).GetValue(view), "view was not disposed");
                Expect((bool)lease.GetType().GetProperty("IsReleased").GetValue(lease, null), "editor lease was not released");
            }
        }

        private sealed class DescriptionViewFixture
        {
            internal readonly object View, Navigator, Session, Focus;
            internal readonly Assembly Product;
            internal bool DescriptionOpen, Neutral = true;
            internal int Frame = 10, Activations;
            internal KeyCode? Pressed;

            internal DescriptionViewFixture(string root, string name)
            {
                string dir;
                ClearFixture(root, name, out dir);
                Product = ViewInputProduct();
                Session = ProductSession(Product, dir);
                ViewInputCall(Session, "FocusCasting", "cast-7");
                ViewInputCall(Session, "RemoveFocusedCasting"); // real Undo is now enabled
                View = DetachedViewObject(Product.GetType("KingmakerBuffPlanner.UI.CastingWorkspaceScreenView", true));
                SetNative(View, "_session", Session);
                SetNative(View, "_clearInput", Activator.CreateInstance(Product.GetType("KingmakerBuffPlanner.UI.WorkspaceModalInputGate", true), true));
                Navigator = DetachedViewObject(Product.GetType("KingmakerBuffPlanner.UI.ControllerWorkspaceNavigator", true));
                SetNative(Navigator, "_workspace", View);
                SetNative(Navigator, "_keyboardFocusActive", true);
                SetNative(Navigator, "_rects", new Dictionary<string, RectTransform>());
                SetNative(Navigator, "_refreshLayout", (Action)(() => { })); // rendering boundaries
                SetNative(Navigator, "_rebuild", (Action)(() => { }));
                SetNative(Navigator, "_drawFocus", (Action<bool>)(reveal => { }));
                SetNative(View, "_controllerNavigation", Navigator);
                object input = Activator.CreateInstance(Product.GetType("KingmakerBuffPlanner.UI.WorkspaceKeyboardInput", true),
                    ViewInputMembers, null, new object[]
                    {
                        (Func<KeyCode, bool>)(key => Pressed == key), (Func<KeyCode, bool>)(key => false),
                        (Func<bool>)(() => DescriptionOpen), (Func<bool>)(() => Neutral), (Func<int>)(() => Frame)
                    }, null);
                SetNative(View, "_workspaceKeyboardInput", input);
                Focus = Activator.CreateInstance(Product.GetType("KingmakerBuffPlanner.UI.ControllerWorkspaceFocus", true), true);
                Type itemType = Product.GetType("KingmakerBuffPlanner.UI.ControllerFocusItem", true);
                Type boundsType = Product.GetType("KingmakerBuffPlanner.UI.ControllerFocusBounds", true);
                Array items = Array.CreateInstance(itemType, 2);
                for (int index = 0; index < 2; index++)
                {
                    object bounds = Activator.CreateInstance(boundsType, ViewInputMembers, null,
                        new object[] { index * 120f, 0f, index * 120f + 100f, 40f }, null);
                    Action activate = () =>
                    {
                        Activations++;
                        ViewInputCall(Session, "Undo");
                    };
                    items.SetValue(Activator.CreateInstance(itemType, ViewInputMembers, null,
                        new object[] { index == 0 ? "plan/undo" : "plan/run", "Footer", "Action", true,
                            activate, null, null, bounds, true, null }, null), index);
                }
                ViewInputCall(Focus, "Replace", new object[] { items });
                ViewInputCall(Focus, "Restore", "plan/undo");
                SetNative(Navigator, "_focus", Focus);
                // A new description-input gate is initialized by the view in
                // production; detached construction supplies its real instance.
                FieldInfo gateField = View.GetType().GetField("_descriptionInput", ViewInputMembers);
                if (gateField != null) gateField.SetValue(View, Activator.CreateInstance(gateField.FieldType, true));
            }

            internal string Signature { get { return (string)ViewInputCall(Session, "DocumentIntentSignature"); } }
            internal string FocusKey { get { return (string)Focus.GetType().GetProperty("Key", ViewInputMembers).GetValue(Focus, null); } }
            internal void Tick(KeyCode? key, bool neutral)
            {
                Frame++; Pressed = key; Neutral = neutral;
                ViewInputCall(View, "TickWorkspaceKeyboard");
            }
        }

        private static void TestDescriptionKeyboard(string root, KeyCode confirm)
        {
            var fixture = new DescriptionViewFixture(root, "description-" + confirm);
            string before = fixture.Signature;
            fixture.DescriptionOpen = true;
            fixture.Tick(confirm, false);
            Expect(fixture.Activations == 0 && fixture.Signature == before, "description allowed underlying Undo/Run activation");
            fixture.DescriptionOpen = false; // native close happens before planner polling
            fixture.Tick(confirm, false);
            fixture.Tick(confirm, false);
            Expect(fixture.Activations == 0 && fixture.Signature == before, "closing/repeated confirm activated underlying action");
            fixture.Tick(null, true);
            fixture.Tick(confirm, false);
            Expect(fixture.Activations == 1 && fixture.Signature != before, "fresh confirm did not restore normal real Undo");
        }

        private static void TestDescriptionNavigation(string root)
        {
            var fixture = new DescriptionViewFixture(root, "description-navigation");
            fixture.DescriptionOpen = true;
            foreach (KeyCode key in new[] { KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.Tab })
            {
                fixture.Tick(key, false);
                Expect(fixture.FocusKey == "plan/undo" && fixture.Activations == 0, "description allowed underlying semantic focus to move");
            }
        }
    }
}
