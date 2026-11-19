using System.Reflection;
using Harmony12;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // The installed fields finish editing on Return/Escape before testing the IME preedit.
    // Keep the game's ordinary field event processing for committed Unicode characters.
    internal sealed class PlannerImeGuard : MonoBehaviour
    {
        private static int _lastPreedit = -3;
        private static MethodInfo FieldKeys(System.Type field)
        {
            return field.GetMethod("KeyPressed", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(Event) }, null);
        }

        internal static void Install(HarmonyInstance harmony)
        {
            var prefix = new HarmonyMethod(typeof(PlannerImeGuard).GetMethod("BeforeFieldKey",
                BindingFlags.Static | BindingFlags.NonPublic));
            harmony.Patch(FieldKeys(typeof(TMP_InputField)), prefix, null, null);
            harmony.Patch(FieldKeys(typeof(InputField)), prefix, null, null);
        }

        internal static void Uninstall(HarmonyInstance harmony, string id)
        {
            harmony.Unpatch(FieldKeys(typeof(TMP_InputField)), HarmonyPatchType.Prefix, id);
            harmony.Unpatch(FieldKeys(typeof(InputField)), HarmonyPatchType.Prefix, id);
            _lastPreedit = -3;
        }

        internal static bool Active
        {
            get
            {
                bool ownedPreedit = false;
                if (!string.IsNullOrEmpty(Input.compositionString))
                {
                    var events = EventSystem.current;
                    var selected = events == null ? null : events.currentSelectedGameObject;
                    ownedPreedit = selected != null && selected.GetComponent<PlannerImeGuard>() != null;
                }
                return ObservePreedit(ownedPreedit, Time.frameCount);
            }
        }

        private static bool ObservePreedit(bool ownedPreedit, int frame)
        {
            if (ownedPreedit) _lastPreedit = frame;
            return frame - _lastPreedit <= 1;
        }

        private static bool BeforeFieldKey(Component __instance, Event evt)
        {
            if (__instance.GetComponent<PlannerImeGuard>() == null || !Active || evt.character > ' ') return true;
            switch (evt.keyCode)
            {
                case KeyCode.Return: case KeyCode.KeypadEnter: case KeyCode.Escape:
                case KeyCode.LeftArrow: case KeyCode.RightArrow: case KeyCode.UpArrow: case KeyCode.DownArrow:
                case KeyCode.Space: case KeyCode.Tab: return false;
                default: return true;
            }
        }
    }
}
