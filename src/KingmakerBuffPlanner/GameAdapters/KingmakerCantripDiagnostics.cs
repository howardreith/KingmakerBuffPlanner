using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Read-only evidence of how the game itself judges level-0 spells for
    // each party caster (live run casting-qual-cast-20260923-a1-anim-01: the
    // native command for a spellbook cantrip ended with Fail): each
    // spellbook's spells per day and remaining level-0 slots, each level-0
    // spellbook spell's availability (the command's own IsAvailable guard,
    // CanSpend and count), and each cantrip ability fact with its spellbook
    // binding; and each book's level-1 known (and, for prepared books,
    // memorized) spells, the party's finite spells. Nothing is cast, spent
    // or changed.
    internal static class KingmakerCantripDiagnostics
    {
        internal static IList<string> Describe()
        {
            var lines = new List<string>();
            if (Game.Instance == null || Game.Instance.Player == null)
            {
                lines.Add("player-state-unavailable");
                return lines;
            }
            foreach (UnitEntityData unit in Game.Instance.Player.Party ?? new List<UnitEntityData>())
            {
                if (unit == null || unit.Descriptor == null) continue;
                lines.Add("unit=" + unit.UniqueId + ";name=" + Safe(() => unit.CharacterName));
                foreach (Spellbook book in Safe(() => unit.Descriptor.Spellbooks.ToList(), new List<Spellbook>()))
                {
                    if (book == null || book.Blueprint == null) continue;
                    bool spontaneous = book.Blueprint.Spontaneous;
                    lines.Add("  book=" + book.Blueprint.AssetGuid + ";name=" + book.Blueprint.name +
                        ";spontaneous=" + spontaneous + ";casterLevel=" + Safe(() => book.CasterLevel.ToString()) +
                        ";maxLevel=" + Safe(() => book.MaxSpellLevel.ToString()) +
                        ";perDay=" + Levels(level => book.GetSpellsPerDay(level)) +
                        (spontaneous
                            ? ";slots=" + Levels(level => book.GetSpontaneousSlots(level))
                            : ";memorized0=" + Safe(() => book.GetMemorizedSpells(0).Count().ToString()) +
                                ";available0=" + Safe(() => book.GetMemorizedSpells(0)
                                    .Count(slot => slot != null && slot.Available).ToString())));
                    foreach (AbilityData known in Safe(() => book.GetKnownSpells(0).ToList(), new List<AbilityData>()))
                        lines.Add("  known0=" + Describe(known) + ";canSpend=" +
                            Safe(() => book.CanSpend(known, false).ToString()));
                    foreach (AbilityData known in Safe(() => book.GetKnownSpells(1).ToList(), new List<AbilityData>()))
                        lines.Add("  known1=" + Describe(known));
                    if (!spontaneous)
                        foreach (SpellSlot slot in Safe(() => book.GetMemorizedSpells(1).ToList(), new List<SpellSlot>()))
                            lines.Add("  memorized1=" + (slot == null ? "none"
                                : Describe(slot.Spell) + ";slotAvailable=" + slot.Available));
                }
                foreach (Ability fact in Safe(() => unit.Descriptor.Abilities.Enumerable.ToList(), new List<Ability>()))
                {
                    if (fact == null || fact.Blueprint == null || !Safe(() => fact.Blueprint.IsCantrip, false)) continue;
                    lines.Add("  fact=" + Describe(fact.Data) + ";factBook=" +
                        (fact.Spellbook == null || fact.Spellbook.Blueprint == null
                            ? "none" : fact.Spellbook.Blueprint.AssetGuid));
                }
            }
            return lines;
        }

        private static string Describe(AbilityData ability)
        {
            if (ability == null || ability.Blueprint == null) return "none";
            return ability.Blueprint.AssetGuid + ";name=" + ability.Blueprint.name +
                ";isCantrip=" + Safe(() => ability.Blueprint.IsCantrip.ToString()) +
                ";level=" + Safe(() => ability.SpellLevel.ToString()) +
                ";dataBook=" + Safe(() => ability.Spellbook == null || ability.Spellbook.Blueprint == null
                    ? "none" : ability.Spellbook.Blueprint.AssetGuid) +
                ";available=" + Safe(() => ability.IsAvailable.ToString()) +
                ";availableForCast=" + Safe(() => ability.IsAvailableForCast.ToString()) +
                ";count=" + Safe(() => ability.GetAvailableForCastCount().ToString());
        }

        private static string Levels(Func<int, int> read)
        {
            return string.Join(",", Enumerable.Range(0, 4).Select(level =>
                Safe(() => read(level).ToString())).ToArray());
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
