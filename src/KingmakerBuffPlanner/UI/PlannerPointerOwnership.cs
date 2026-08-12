using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker.Controllers.Clicks;
using KingmakerBuffPlanner.Infrastructure;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    internal static class PlannerPointerOwnership
    {
        private const string HarmonyId = "KingmakerBuffPlanner.PlannerPointerOwnership";
        private static readonly List<RectTransform> Regions = new List<RectTransform>();
        private static HarmonyInstance _harmony;
        private static MethodInfo _target;
        private static ModLog _log;

        internal static bool IsInstalled { get { return _harmony != null && _target != null; } }

        internal static void Install(ModLog log)
        {
            if (IsInstalled) return;
            _log = log ?? throw new ArgumentNullException("log");
            // The exact 2.1.7b assembly exposes get_InGui as a method without a
            // corresponding Property metadata row, so PropertyInfo lookup is invalid.
            _target = typeof(PointerController).GetMethod("get_InGui",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (_target == null) throw new MissingMethodException(
                typeof(PointerController).FullName, "get_InGui");
            MethodInfo postfix = typeof(PlannerPointerOwnership).GetMethod("Postfix",
                BindingFlags.Static | BindingFlags.NonPublic);
            _harmony = HarmonyInstance.Create(HarmonyId);
            _harmony.Patch(_target, null, new HarmonyMethod(postfix), null);
            _log.Info("[KBP-INPUT] conditional PointerController.InGui ownership installed;" +
                "target=" + _target.DeclaringType.FullName + "." + _target.Name +
                ";scope=active-planner-regions-only.");
        }

        internal static void Uninstall()
        {
            Regions.Clear();
            if (_harmony != null && _target != null)
            {
                _harmony.Unpatch(_target, HarmonyPatchType.Postfix, HarmonyId);
                if (_log != null) _log.Info("[KBP-INPUT] conditional pointer ownership removed.");
            }
            _target = null;
            _harmony = null;
        }

        internal static void Register(RectTransform region)
        {
            if (region == null) return;
            Cleanup();
            if (!Regions.Contains(region)) Regions.Add(region);
        }

        internal static void Unregister(RectTransform region)
        {
            if (region != null) Regions.Remove(region);
            Cleanup();
        }

        internal static bool Contains(Vector2 screenPosition)
        {
            Cleanup();
            return Regions.Any(region => region != null && region.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(region, screenPosition,
                    ResolveEventCamera(region)));
        }

        private static void Postfix(ref bool __result)
        {
            if (!__result && Contains(Input.mousePosition)) __result = true;
        }

        private static Camera ResolveEventCamera(RectTransform region)
        {
            Canvas canvas = region == null ? null : region.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera;
        }

        private static void Cleanup()
        {
            for (int index = Regions.Count - 1; index >= 0; index--)
                if (Regions[index] == null) Regions.RemoveAt(index);
        }
    }
}
