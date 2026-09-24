using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.Execution
{
    // How discovery prices an authored level-0 spellbook entry (batch 3
    // review A4): free through its at-will ability, a finite level-0 slot
    // without one, or unresolved when the at-will choice is ambiguous.
    public enum CantripPricing
    {
        Free,
        Finite,
        Unresolved
    }

    // How execution casts an authored spellbook entry (batch 3 review A2).
    public enum CantripRoute
    {
        // Not a level-0 entry: its slot or known spell, as always.
        Normal,
        // A free level-0 casting: only through the at-will ability.
        AtWillOnly,
        // A finite level-0 casting: only through its slot or known entry.
        SlotOnly,
        // No reservation (discovery-time reads): the at-will ability first.
        AtWillThenSlot,
        // A free reservation for an entry that is never cast at will.
        Refused
    }

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
        // A free level-0 casting whose at-will ability is gone: refused,
        // never paid from a level-0 slot instead.
        public const string MissingRefusal = "at-will-cantrip-missing";
        // A free reservation for a spellbook entry that is not cast at will
        // would spend a slot the plan never reserved.
        public const string FreeReservationRefusal = "free-reservation-for-slot-entry";

        // Discovery (review A4): an ambiguous at-will choice stays an
        // unresolved refusal; it is never repriced as a level-0 slot.
        public static CantripPricing Price(bool atWillFound, string refusal)
        {
            if (atWillFound) return CantripPricing.Free;
            return refusal == null ? CantripPricing.Finite : CantripPricing.Unresolved;
        }

        // Execution (review A2): the step's reservation decides, never the
        // entry alone. A free level-0 casting is cast only at will; a finite
        // one never takes the free route; without a reservation (reads made
        // for discovery and targeting) the at-will ability is preferred.
        public static CantripRoute Route(bool levelZeroEntry, bool? reservationUnlimited)
        {
            if (!levelZeroEntry)
                return reservationUnlimited == true ? CantripRoute.Refused : CantripRoute.Normal;
            if (reservationUnlimited == null) return CantripRoute.AtWillThenSlot;
            return reservationUnlimited.Value ? CantripRoute.AtWillOnly : CantripRoute.SlotOnly;
        }

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
