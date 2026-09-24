using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.Execution
{
    // One of a caster's abilities considered for an authored level-0
    // spellbook entry, as the game judges it.
    public sealed class AtWillCantripCandidate
    {
        public AtWillCantripCandidate(string identity, bool isCantrip, bool matchesAuthoredAbility,
            bool hasSpellbook, bool available, int count, int casterLevel)
        {
            Identity = identity ?? string.Empty;
            IsCantrip = isCantrip;
            MatchesAuthoredAbility = matchesAuthoredAbility;
            HasSpellbook = hasSpellbook;
            Available = available;
            Count = count;
            CasterLevel = casterLevel;
        }

        public string Identity { get; private set; }
        // The ability blueprint is a cantrip (the game's own flag).
        public bool IsCantrip { get; private set; }
        // Same base ability, variant and metamagic as the authored entry.
        public bool MatchesAuthoredAbility { get; private set; }
        // Bound to a spellbook: then it spends that book's level-0 slots.
        public bool HasSpellbook { get; private set; }
        public bool Available { get; private set; }
        // The game's cast count; negative means unlimited.
        public int Count { get; private set; }
        public int CasterLevel { get; private set; }

        public bool AtWill
        {
            get { return IsCantrip && MatchesAuthoredAbility && !HasSpellbook && Available && Count < 0; }
        }
    }

    // Which at-will cantrip ability an authored level-0 spellbook entry is
    // cast through (the game casts a cantrip at will only through the
    // ability its class grants; the spellbook's own entry needs a level-0
    // slot). Only a cantrip of exactly the authored ability that the game
    // judges available and unlimited qualifies; a name- or effect-alike,
    // a spellbook-bound or a finite ability never does. Several at-will
    // abilities (one per class) resolve to the one at the authored
    // spellbook's caster level; several at other, different caster levels
    // are ambiguous and refused, never guessed.
    public static class AtWillCantripChoice
    {
        public const string AmbiguousPrefix = "at-will-cantrip-ambiguous:";

        // The chosen candidate, or null: with no refusal when no at-will
        // ability exists (the authored entry is then cast only if the game
        // allows it), with a refusal when the choice is ambiguous.
        public static AtWillCantripCandidate Choose(IEnumerable<AtWillCantripCandidate> candidates,
            int preferredCasterLevel, out string refusal)
        {
            refusal = null;
            List<AtWillCantripCandidate> atWill = (candidates ?? new AtWillCantripCandidate[0])
                .Where(value => value != null && value.AtWill).ToList();
            if (atWill.Count == 0) return null;
            if (atWill.Select(value => value.CasterLevel).Distinct().Count() == 1) return atWill[0];
            AtWillCantripCandidate preferred = atWill.FirstOrDefault(value =>
                value.CasterLevel == preferredCasterLevel);
            if (preferred != null) return preferred;
            refusal = AmbiguousPrefix + string.Join(",", atWill
                .Select(value => value.Identity + "@cl" + value.CasterLevel).ToArray());
            return null;
        }
    }
}
