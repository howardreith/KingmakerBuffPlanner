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
        private static FieldInfo _mouseDown;
        private static FieldInfo _mouseDrag;
        private static FieldInfo _mouseDownOn;
        private static FieldInfo _mouseDownHandler;
        private static ModLog _log;

        internal static bool IsInstalled { get { return _harmony != null && _target != null; } }

        internal static void Install(ModLog log)
        {
            if (IsInstalled) return;
            _log = log ?? throw new ArgumentNullException("log");
            _target = typeof(PointerController).GetMethod("Tick",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (_target == null) throw new MissingMethodException(
                typeof(PointerController).FullName, "Tick");
            _mouseDown = Field("m_MouseDown");
            _mouseDrag = Field("m_MouseDrag");
            _mouseDownOn = Field("m_MouseDownOn");
            _mouseDownHandler = Field("m_MouseDownHandler");
            MethodInfo prefix = typeof(PlannerPointerOwnership).GetMethod("Prefix",
                BindingFlags.Static | BindingFlags.NonPublic);
            _harmony = HarmonyInstance.Create(HarmonyId);
            _harmony.Patch(_target, new HarmonyMethod(prefix), null, null);
            _log.Info("[KBP-INPUT] conditional PointerController.Tick ownership installed;" +
                "target=" + _target.DeclaringType.FullName + "." + _target.Name +
                ";scope=active-planner-regions-only.");
        }

        internal static void Uninstall()
        {
            Regions.Clear();
            if (_harmony != null && _target != null)
            {
                _harmony.Unpatch(_target, HarmonyPatchType.Prefix, HarmonyId);
                if (_log != null) _log.Info("[KBP-INPUT] conditional pointer ownership removed.");
            }
            _target = null;
            _mouseDown = null;
            _mouseDrag = null;
            _mouseDownOn = null;
            _mouseDownHandler = null;
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

        private static bool Prefix(PointerController __instance)
        {
            if (!Contains(Input.mousePosition)) return true;
            if (__instance != null)
            {
                _mouseDown.SetValue(__instance, false);
                _mouseDrag.SetValue(__instance, false);
                _mouseDownOn.SetValue(__instance, null);
                _mouseDownHandler.SetValue(__instance, null);
            }
            return false;
        }

        private static FieldInfo Field(string name)
        {
            FieldInfo field = typeof(PointerController).GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(
                typeof(PointerController).FullName, name);
            return field;
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
