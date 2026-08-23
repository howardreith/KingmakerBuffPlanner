using System;
using System.Reflection;
using Harmony12;
using Kingmaker.UI;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.RuntimeTesting;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    internal static class PlannerHotkey
    {
        private const string HarmonyId = "KingmakerBuffPlanner.PlannerHotkey";
        private static HarmonyInstance _harmony;
        private static MethodInfo _inputMatched;
        private static string _binding = PlannerHotkeyBinding.Default;

        internal static string Binding { get { return _binding; } }
        internal static bool IsInstalled { get { return _harmony != null; } }

        internal static void Install(ModLog log)
        {
            if (IsInstalled) return;
            Type bindingType = typeof(KeyboardAccess).GetNestedType("Binding",
                BindingFlags.Public | BindingFlags.NonPublic);
            _inputMatched = bindingType == null ? null : bindingType.GetMethod("InputMatched",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (_inputMatched == null) throw new MissingMethodException(
                "Kingmaker.UI.KeyboardAccess+Binding", "InputMatched");
            MethodInfo prefix = typeof(PlannerHotkey).GetMethod("InputMatchedPrefix",
                BindingFlags.Static | BindingFlags.NonPublic);
            _harmony = HarmonyInstance.Create(HarmonyId);
            _harmony.Patch(_inputMatched, new HarmonyMethod(prefix), null, null);
            log.Info("[KBP-HOTKEY] exact native binding isolation installed;target=" +
                _inputMatched.DeclaringType.FullName + ".InputMatched;default=" + _binding + ".");
        }

        internal static void SetBinding(string value)
        {
            _binding = PlannerHotkeyBinding.Normalize(value);
        }

        internal static bool GetKeyDown()
        {
            return ModifiersHeld() && Input.GetKeyDown(PrimaryKey());
        }

        internal static bool ShouldSuppressNativeBinding(
            KeyCode nativeKey, string nativeName, bool ctrl, bool shift, bool alt)
        {
            return PlannerHotkeyBinding.ShouldSuppress(_binding, nativeKey.ToString(), nativeName,
                ctrl, shift, alt);
        }

        internal static void Uninstall()
        {
            if (_harmony != null && _inputMatched != null)
                _harmony.Unpatch(_inputMatched, HarmonyPatchType.Prefix, HarmonyId);
            _harmony = null;
            _inputMatched = null;
            _binding = PlannerHotkeyBinding.Default;
        }

        private static bool InputMatchedPrefix(object __instance, ref bool __result)
        {
            long startedAt = RuntimePerformanceDiagnostics.BeginOperation();
            try
            {
                bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                PropertyInfo keyProperty = __instance == null ? null : __instance.GetType()
                    .GetProperty("Key", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (keyProperty == null) return true;
                PropertyInfo nameProperty = __instance.GetType().GetProperty("Name",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                string name = nameProperty == null ? string.Empty :
                    Convert.ToString(nameProperty.GetValue(__instance, null));
                if (!ShouldSuppressNativeBinding((KeyCode)keyProperty.GetValue(__instance, null),
                        name, ctrl, shift, alt)) return true;
                __result = false;
                return false;
            }
            finally
            {
                RuntimePerformanceDiagnostics.RecordOperation(
                    RuntimePerformanceOperation.NativeHotkeyPrefix, startedAt);
            }
        }

        private static bool ModifiersHeld()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            return ctrl && shift && !alt;
        }

        private static KeyCode PrimaryKey()
        {
            return PlannerHotkeyBinding.PrimaryKey(_binding) == "P" ? KeyCode.P : KeyCode.B;
        }
    }
}
