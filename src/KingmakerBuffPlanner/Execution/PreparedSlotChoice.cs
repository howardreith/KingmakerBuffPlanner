using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Execution
{
    // One memorized slot of the step's spellbook as the resolver sees it.
    // EquivalenceKey groups slots the game's own spend treats as the same
    // preparation: an equal AbilityData (blueprint, caster, fact, spellbook,
    // conversion and metamagic, AbilityData.Equals), at the same spell level,
    // of the same slot type, opposition flag and linked-slot count. Slots of
    // one key are interchangeable to the game; slots of different keys never
    // are.
    public sealed class PreparedSlotCandidate
    {
        public PreparedSlotCandidate(string slotId, bool available, bool isMain,
            bool matchesProvider, string equivalenceKey, IEnumerable<string> linkedSlotIds)
        {
            if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("Slot ID is required.", "slotId");
            SlotId = slotId;
            Available = available;
            IsMain = isMain;
            MatchesProvider = matchesProvider;
            EquivalenceKey = equivalenceKey ?? string.Empty;
            LinkedSlotIds = (linkedSlotIds ?? new string[0])
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != slotId)
                .Distinct(StringComparer.Ordinal).ToList().AsReadOnly();
        }

        public string SlotId { get; private set; }
        public bool Available { get; private set; }
        public bool IsMain { get; private set; }
        // The slot's spell resolves to the step's authored ability/variant.
        public bool MatchesProvider { get; private set; }
        public string EquivalenceKey { get; private set; }
        public IReadOnlyList<string> LinkedSlotIds { get; private set; }
    }

    // Read-only native spend evidence: which memorized slots of the step's
    // book went from available to unavailable around a cast. A diagnostic
    // only; it never changes a verdict.
    public static class PreparedSpendEvidence
    {
        public static string Describe(IList<KeyValuePair<string, bool>> before,
            IList<KeyValuePair<string, bool>> after)
        {
            if (before == null && after == null) return "prepared-consumed:not-prepared";
            if (before == null || after == null) return "prepared-consumed:unread";
            var was = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, bool> slot in before) was[slot.Key] = slot.Value;
            List<string> consumed = after.Where(slot => !slot.Value && was.ContainsKey(slot.Key) && was[slot.Key])
                .Select(slot => slot.Key).Distinct(StringComparer.Ordinal).ToList();
            List<string> restored = after.Where(slot => slot.Value && was.ContainsKey(slot.Key) && !was[slot.Key])
                .Select(slot => slot.Key).Distinct(StringComparer.Ordinal).ToList();
            return "prepared-consumed:" + (consumed.Count == 0 ? "none" : string.Join(",", consumed.ToArray())) +
                (restored.Count == 0 ? string.Empty : ";prepared-restored:" + string.Join(",", restored.ToArray()));
        }
    }

    // A run coordinator's step context: the prepared-slot tokens of the
    // run's already confirmed castings from the step's own pool. Steps
    // without prepared tokens are returned unchanged.
    public static class PreparedRunContext
    {
        public static CastStep ForStep(CastStep step, IEnumerable<CastStep> confirmedEarlier)
        {
            if (step == null) throw new ArgumentNullException("step");
            if (step.Reservation == null || step.Reservation.TokenIds.Count == 0) return step;
            List<string> tokens = (confirmedEarlier ?? new CastStep[0])
                .Where(value => value != null && value.Reservation != null &&
                    value.Reservation.PoolKey == step.Reservation.PoolKey)
                .SelectMany(value => value.Reservation.TokenIds).ToList();
            return tokens.Count == 0 ? step
                : step.WithReservation(step.Reservation.WithEarlierConfirmedTokens(tokens));
        }
    }

    // Which memorized slot a prepared-spell step casts from.
    //
    // The planner reserves one preparation per casting, but the game's spend
    // is not bound to it: Spellbook.SpendInternal consumes the LAST available
    // equal slot of the level, so a first casting resolved from its own slot
    // can consume the slot a later casting of the same preparation reserved
    // (0.6.12 owner log: second Heroism, provider-ability-not-found). Since
    // the game cannot be told which copy to spend, the authorization that
    // survives execution is a count per preparation: a step whose own slot is
    // gone may resolve an equivalent slot reserved by a casting of this run
    // that already confirmed - the copy that casting's spend left behind.
    // It never takes a slot reserved by a later casting or one the plan did
    // not reserve, never a different preparation (spell, variant,
    // metamagic, heighten, slot type, linked cost), and every linked slot
    // must still be available.
    public static class PreparedSlotChoice
    {
        public const string MissingPrefix = "prepared-source-missing:";
        public const string ExhaustedPrefix = "prepared-source-exhausted:";

        // reservedTokenIds: the step's own frozen reservation (its main slot
        // and any linked slots). releasedTokenIds: slots reserved by castings
        // of the same run that already confirmed. Candidates are in the
        // game's own list order.
        public static string Choose(IReadOnlyList<string> reservedTokenIds,
            IReadOnlyList<string> releasedTokenIds, IReadOnlyList<PreparedSlotCandidate> candidates,
            out string resolution, out string refusal)
        {
            resolution = null;
            refusal = null;
            if (reservedTokenIds == null || reservedTokenIds.Count == 0)
                throw new ArgumentException("A reserved prepared slot is required.", "reservedTokenIds");
            List<PreparedSlotCandidate> slots = (candidates ?? new PreparedSlotCandidate[0])
                .Where(value => value != null).ToList();
            Dictionary<string, PreparedSlotCandidate> byId = slots.GroupBy(slot => slot.SlotId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            PreparedSlotCandidate available = slots.FirstOrDefault(slot => slot.Available &&
                slot.IsMain && slot.MatchesProvider && reservedTokenIds.Contains(slot.SlotId));
            if (available != null)
            {
                resolution = "reserved-memorized-slot:" + available.SlotId;
                return available.SlotId;
            }
            string reservedText = string.Join(",", reservedTokenIds.ToArray());
            PreparedSlotCandidate own = slots.FirstOrDefault(slot => slot.IsMain && slot.MatchesProvider &&
                reservedTokenIds.Contains(slot.SlotId));
            if (own == null)
            {
                // The reserved preparation is no longer in the book (forgotten
                // or replaced): a missing source, not a spent one.
                refusal = MissingPrefix + "reserved=" + reservedText + ";book-slot=" +
                    (reservedTokenIds.Any(byId.ContainsKey) ? "other-preparation" : "absent");
                return null;
            }
            List<string> released = (releasedTokenIds ?? new string[0])
                .Where(id => !reservedTokenIds.Contains(id)).Distinct(StringComparer.Ordinal).ToList();
            List<PreparedSlotCandidate> equivalent = released.Where(byId.ContainsKey).Select(id => byId[id])
                .Where(slot => slot.IsMain && slot.MatchesProvider &&
                    string.Equals(slot.EquivalenceKey, own.EquivalenceKey, StringComparison.Ordinal))
                .ToList();
            PreparedSlotCandidate reconciled = equivalent.FirstOrDefault(slot => slot.Available &&
                slot.LinkedSlotIds.All(id => byId.ContainsKey(id) && byId[id].Available &&
                    released.Contains(id)));
            if (reconciled == null)
            {
                refusal = ExhaustedPrefix + "reserved=" + reservedText + ";reserved-available=false" +
                    ";released=" + released.Count + ";released-equivalent=" + equivalent.Count +
                    ";released-equivalent-available=0";
                return null;
            }
            resolution = "reconciled-memorized-slot:" + reconciled.SlotId + ";reserved=" + own.SlotId +
                ";reason=native-spend-consumed-reserved-equivalent;released-equivalent-available=" +
                equivalent.Count(slot => slot.Available);
            return reconciled.SlotId;
        }
    }
}
