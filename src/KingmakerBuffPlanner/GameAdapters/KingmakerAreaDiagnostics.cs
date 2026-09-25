using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.View.MapObjects;
using UnityEngine;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Read-only evidence for the area-transition question (mission batch 3,
    // section 9): the loaded area, the game mode, whether the game's own
    // autosave setting is on, and every area transition in the loaded scenes
    // with its destination, its autosave mode (Game.LoadArea writes an
    // autosave before leaving for BeforeExit while the setting is on), whether
    // it runs its own blueprint actions instead of loading
    // (AreaTransitionGroupCommand.ExecuteTransition), its restriction count,
    // its visibility and its distance from the main character. Nothing is
    // used, moved, clicked, evaluated or saved.
    internal static class KingmakerAreaDiagnostics
    {
        internal static IList<string> Describe()
        {
            var lines = new List<string>();
            Game game = Game.Instance;
            if (game == null || game.Player == null)
            {
                lines.Add("game-unavailable");
                return lines;
            }
            BlueprintArea area = Safe(() => game.CurrentlyLoadedArea, null);
            lines.Add("area=" + (area == null ? "none" : area.AssetGuid + ";name=" + area.name) +
                ";mode=" + Safe(() => game.CurrentMode.ToString()) +
                ";combat=" + Safe(() => game.Player.IsInCombat.ToString()) +
                ";autosaveEnabled=" + Safe(() =>
                    Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveEnabled.CurrentValue.ToString()));
            Vector3? leader = Safe<Vector3?>(() => game.Player.MainCharacter.Value.Position, null);
            List<AreaTransition> transitions = Safe(() =>
                UnityEngine.Object.FindObjectsOfType<AreaTransition>().ToList(), new List<AreaTransition>());
            lines.Add("transitions=" + transitions.Count);
            foreach (AreaTransition transition in transitions.OrderBy(value => Safe(() => value.name)))
            {
                if (transition == null) continue;
                BlueprintAreaEnterPoint enter = Safe(() => transition.AreaEnterPoint, null);
                BlueprintArea destination = enter == null ? null : Safe(() => enter.Area, null);
                BlueprintAreaTransition blueprint = Safe(() => transition.Blueprint, null);
                lines.Add("transition=" + Safe(() => transition.name) +
                    ";active=" + Safe(() => transition.gameObject.activeInHierarchy.ToString()) +
                    ";enterPoint=" + (enter == null ? "none" : enter.AssetGuid + ":" + enter.name) +
                    ";destination=" + (destination == null ? "none" : destination.AssetGuid + ":" + destination.name) +
                    ";sameArea=" + (destination != null && area != null && destination == area) +
                    ";autoSave=" + Safe(() => transition.AutoSaveMode.ToString()) +
                    ";blueprintActions=" + (blueprint == null ? "0"
                        : Safe(() => blueprint.Actions.Count().ToString())) +
                    ";restrictions=" + RestrictionCount(transition) +
                    ";unlocked=" + Safe(() =>
                    {
                        // The persistent data type is not public: its flag
                        // is read, never written.
                        object data = ((MapObjectComponent)transition).Data;
                        FieldInfo field = data == null ? null : data.GetType().GetField("AlreadyUnlocked",
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        return data == null ? "no-data" : field == null ? "unknown"
                            : Convert.ToString(field.GetValue(data));
                    }) +
                    ";distance=" + Safe(() => leader == null ? "unknown"
                        : Vector3.Distance(leader.Value, transition.transform.position).ToString("0.0")));
            }
            return lines;
        }

        // The restriction list's size only (the restrictions are never
        // evaluated here).
        private static string RestrictionCount(AreaTransition transition)
        {
            return Safe(() =>
            {
                FieldInfo field = typeof(AreaTransition).GetField("m_Restrictions",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                IList list = field == null ? null : field.GetValue(transition) as IList;
                return list == null ? "unknown" : list.Count.ToString();
            });
        }

        private static string Safe(Func<string> read)
        {
            try { return read() ?? "null"; }
            catch (Exception exception) { return "error:" + exception.GetType().Name; }
        }

        private static T Safe<T>(Func<T> read, T fallback)
        {
            try { return read(); }
            catch (Exception) { return fallback; }
        }
    }
}
