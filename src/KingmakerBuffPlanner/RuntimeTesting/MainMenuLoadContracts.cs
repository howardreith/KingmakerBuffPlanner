using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Exact main-menu load contracts adapted from the qualified Gunslinger
    // working-save-smoke reference (Kingmaker 2.1.7b). These resolvers prove
    // the active MainMenuButtons lifecycle receiver and the exact Load Game
    // button by hierarchy, sibling position, component set, TMP label
    // identity, and wired listeners — never by display text search.
    internal static class MainMenuLoadContracts
    {
        internal const string MainMenuButtonsTypeName =
            "Kingmaker.UI.MainMenuUI.MainMenuButtons";
        internal const string SaveLoadWindowTypeName =
            "Kingmaker.UI.SaveLoadWindow.SaveLoadWindow";
        internal const string SaveSlotTypeName = "Kingmaker.UI.SaveLoadWindow.SaveSlot";
        internal const string ListOfSavesTypeName =
            "Kingmaker.UI.SaveLoadWindow.ListOfSaves";
        internal const string SaveInfoTypeName =
            "Kingmaker.EntitySystem.Persistence.SaveInfo";
        internal const string MainMenuTypeName = "Kingmaker.MainMenu";

        internal const string ButtonPath =
            "!LIGHT_SETUP/SceneUICanvas/SideBar/Buttons/LoadGame";
        private const int ButtonSiblingIndex = 2;
        private const int ButtonSiblingCount = 7;
        private const string MainMenuRootName = "!LIGHT_SETUP";
        private static readonly string[] ExactComponents =
        {
            "UnityEngine.CanvasRenderer", "UnityEngine.RectTransform",
            "UnityEngine.UI.Button"
        };
        private const string ExactLabel =
            "<font=\"Saber_Dist32\"><color=#983F1D><size=140%>L</size></color></font>oad game";
        private static readonly string[] ExactLabelIdentities =
        {
            "TMPro.TextMeshProUGUI.m_text=" + ExactLabel,
            "TMPro.TextMeshProUGUI.old_text=" + ExactLabel,
            "TMPro.TextMeshProUGUI.text=" + ExactLabel
        };

        internal const BindingFlags AllInstance = BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;

        internal sealed class LoadButtonEvidence
        {
            public string HierarchyPath;
            public string MainMenuRoot;
            public int SiblingIndex;
            public int SiblingCount;
            public bool Interactable;
            public List<string> LabelIdentities;
            public List<string> ListenerIdentities;
        }

        internal static object ResolveActiveMainMenuButtons()
        {
            Type type = typeof(Kingmaker.Game).Assembly.GetType(MainMenuButtonsTypeName, true);
            return Resources.FindObjectsOfTypeAll(type)
                .OfType<Component>()
                .Where(component => component != null &&
                    component.gameObject.activeInHierarchy &&
                    Root(component.transform).gameObject.name == MainMenuRootName)
                .Cast<object>()
                .SingleOrDefault();
        }

        internal static Button ResolveExactLoadButton(out LoadButtonEvidence evidence)
        {
            evidence = null;
            Button match = null;
            foreach (Button button in Resources.FindObjectsOfTypeAll(typeof(Button))
                .OfType<Button>())
            {
                LoadButtonEvidence candidate;
                if (TryExactButton(button, out candidate))
                {
                    if (match != null) return null;
                    match = button;
                    evidence = candidate;
                }
            }
            return match;
        }

        private static bool TryExactButton(Button button, out LoadButtonEvidence evidence)
        {
            evidence = null;
            if (button == null || !button.gameObject.activeSelf ||
                !button.gameObject.activeInHierarchy || !button.interactable ||
                button.GetType().FullName != "UnityEngine.UI.Button" ||
                HierarchyPath(button.transform) != ButtonPath ||
                button.transform.GetSiblingIndex() != ButtonSiblingIndex ||
                button.transform.parent == null ||
                button.transform.parent.childCount != ButtonSiblingCount ||
                Root(button.transform).gameObject.name != MainMenuRootName)
                return false;
            List<string> components = button.gameObject.GetComponents<Component>()
                .Where(value => value != null).Select(value => value.GetType().FullName)
                .OrderBy(value => value).ToList();
            if (!components.SequenceEqual(ExactComponents.OrderBy(value => value)))
                return false;
            List<string> labels = SafeLabelIdentities(button.gameObject);
            if (!ExactLabelIdentities.All(labels.Contains)) return false;
            List<string> listeners = ListenerIdentities(button.onClick);
            bool persistent = listeners.Any(value =>
                value.StartsWith("persistent;", StringComparison.Ordinal) &&
                value.EndsWith(";OnButtonLoadGame", StringComparison.Ordinal));
            int runtime = listeners.Count(value =>
                value.StartsWith("runtime;" + MainMenuButtonsTypeName + ";", StringComparison.Ordinal) &&
                value.EndsWith(";OnButtonLoadGame", StringComparison.Ordinal));
            if (!persistent || runtime != 1) return false;
            evidence = new LoadButtonEvidence
            {
                HierarchyPath = ButtonPath,
                MainMenuRoot = MainMenuRootName,
                SiblingIndex = ButtonSiblingIndex,
                SiblingCount = ButtonSiblingCount,
                Interactable = true,
                LabelIdentities = labels,
                ListenerIdentities = listeners
            };
            return true;
        }

        internal static List<string> ListenerIdentities(UnityEvent action)
        {
            var result = new List<string>();
            for (int index = 0; index < action.GetPersistentEventCount(); index++)
            {
                UnityEngine.Object target = action.GetPersistentTarget(index);
                result.Add("persistent;" + (target == null
                    ? "<null>" : target.GetType().FullName) + ";" +
                    action.GetPersistentMethodName(index));
            }
            foreach (Delegate item in RuntimeDelegates(action))
                result.Add("runtime;" + (item.Target == null
                    ? "<static>" : item.Target.GetType().FullName) + ";" + item.Method.Name);
            return result.Distinct().OrderBy(value => value).ToList();
        }

        internal static List<Delegate> RuntimeDelegates(UnityEvent action)
        {
            var result = new List<Delegate>();
            object calls = ReadField(action, "m_Calls");
            foreach (object invokable in EnumerateCalls(calls))
            {
                Delegate callback = FindDelegate(invokable);
                if (callback == null) continue;
                result.AddRange(callback.GetInvocationList());
            }
            return result;
        }

        internal static List<string> SafeLabelIdentities(GameObject gameObject)
        {
            var values = new List<string>();
            foreach (Component component in
                gameObject.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                Type type = component.GetType();
                foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (member.Name.IndexOf("text",
                        StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string text = null;
                    try
                    {
                        FieldInfo field = member as FieldInfo;
                        PropertyInfo property = member as PropertyInfo;
                        if (field != null && field.FieldType == typeof(string))
                            text = field.GetValue(component) as string;
                        else if (property != null &&
                            property.PropertyType == typeof(string) &&
                            property.GetIndexParameters().Length == 0 &&
                            property.CanRead)
                            text = property.GetValue(component, null) as string;
                    }
                    catch { continue; }
                    if (!string.IsNullOrWhiteSpace(text) && text.Length <= 160)
                        values.Add(type.FullName + "." + member.Name + "=" + text);
                }
            }
            return values.Distinct().OrderBy(value => value).ToList();
        }

        internal static object ReadMember(object value, string name)
        {
            if (value == null) return null;
            PropertyInfo property = value.GetType().GetProperty(
                name, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(value, null);
            FieldInfo field = value.GetType().GetField(
                name, BindingFlags.Instance | BindingFlags.Public);
            return field == null ? null : field.GetValue(value);
        }

        internal static string Read(object value, string name)
        {
            return Convert.ToString(ReadMember(value, name)) ?? "";
        }

        internal static string Leaf(string value)
        {
            try { return System.IO.Path.GetFileName(value) ?? ""; }
            catch { return ""; }
        }

        internal static int ReadCount(object value)
        {
            int count;
            return int.TryParse(Read(value, "Count"), out count) ? count : -1;
        }

        internal static bool ContainsReference(object collection, object item)
        {
            IEnumerable values = collection as IEnumerable;
            return values != null && values.Cast<object>().Any(
                value => ReferenceEquals(value, item));
        }

        internal static string ObjectIdentity(object value)
        {
            if (value == null) return "";
            return value.GetType().FullName + "#" +
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
        }

        internal static string HierarchyPath(Transform value)
        {
            var names = new Stack<string>();
            for (Transform item = value; item != null; item = item.parent)
                names.Push(item.gameObject.name);
            return string.Join("/", names.ToArray());
        }

        internal static Transform Root(Transform value)
        {
            while (value.parent != null) value = value.parent;
            return value;
        }

        internal static bool IsSaveInfoList(Type type)
        {
            return type != null && type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(List<>) &&
                type.GetGenericArguments()[0].FullName == SaveInfoTypeName;
        }

        internal static MethodInfo ExactPatchableMethod(Type declaringType,
            string name, Type[] parameterTypes, Type returnType)
        {
            MethodInfo method = declaringType.GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null, parameterTypes, null);
            if (method == null || method.DeclaringType != declaringType ||
                method.Name != name || method.ReturnType != returnType ||
                method.IsAbstract || method.IsGenericMethodDefinition ||
                method.ContainsGenericParameters || method.GetMethodBody() == null ||
                !method.GetParameters().Select(value => value.ParameterType)
                    .SequenceEqual(parameterTypes))
                throw new MissingMethodException(declaringType.FullName,
                    name + " exact managed patchable contract");
            return method;
        }

        private static IEnumerable EnumerateCalls(object calls)
        {
            if (calls == null) yield break;
            foreach (string name in new[] { "m_PersistentCalls", "m_RuntimeCalls",
                "m_ExecutingCalls" })
            {
                IEnumerable list = ReadField(calls, name) as IEnumerable;
                if (list == null) continue;
                foreach (object value in list) if (value != null) yield return value;
            }
        }

        private static Delegate FindDelegate(object value)
        {
            for (Type type = value == null ? null : value.GetType();
                type != null; type = type.BaseType)
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly))
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                        try { return field.GetValue(value) as Delegate; } catch { return null; }
            return null;
        }

        private static object ReadField(object value, string name)
        {
            for (Type type = value == null ? null : value.GetType();
                type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
                if (field != null) try { return field.GetValue(value); } catch { return null; }
            }
            return null;
        }
    }
}
