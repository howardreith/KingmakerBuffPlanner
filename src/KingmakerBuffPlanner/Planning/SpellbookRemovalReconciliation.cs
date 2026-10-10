using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Planning
{
    public enum SpellbookRemovalReason
    {
        NoLongerPrepared,
        NoLongerKnown
    }

    // One saved casting whose exact spell the caster's own identifiable book
    // positively no longer holds.
    public sealed class SpellbookRemovalCandidate
    {
        internal SpellbookRemovalCandidate(PlannedCasting casting,
            SpellbookRemovalReason reason, string observationKey)
        {
            CastingId = casting.CastingId;
            RoutineId = casting.RoutineId;
            CasterUnitId = casting.CasterUnitId;
            SpellbookGuid = casting.SpellbookGuid;
            Ability = casting.Ability;
            Reason = reason;
            ObservationKey = observationKey;
        }

        public string CastingId { get; private set; }
        public string RoutineId { get; private set; }
        public string CasterUnitId { get; private set; }
        public string SpellbookGuid { get; private set; }
        public AbilityKey Ability { get; private set; }
        public SpellbookRemovalReason Reason { get; private set; }
        // caster|book|member|book-digest: the exact observation that proved
        // the removal (the suppression identity after an Undo).
        public string ObservationKey { get; private set; }
    }

    public sealed class SpellbookReconciliationDecision
    {
        internal SpellbookReconciliationDecision(
            IEnumerable<SpellbookRemovalCandidate> removals,
            IEnumerable<KeyValuePair<string, string>> kept)
        {
            Removals = new ReadOnlyCollection<SpellbookRemovalCandidate>(removals.ToList());
            Kept = new ReadOnlyDictionary<string, string>(kept.ToDictionary(
                pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        }

        public IReadOnlyList<SpellbookRemovalCandidate> Removals { get; private set; }
        // Every spellbook casting not removed, with the reason it stays.
        public IReadOnlyDictionary<string, string> Kept { get; private set; }
    }

    // 0.4.2 (B): decides which saved castings a deliberate spellbook removal
    // retires. Pure: the native adapter supplies the membership facts and
    // the session owns the single compound edit. A casting goes only when
    // its caster's identified book was completely and stably read and holds
    // no copy of the casting's exact spell (base ability, the parent of a
    // variant, and metamagic). Spent or exhausted slots, absent or
    // incapacitated casters, unreadable or unproven books, unstable reads
    // and classifier rejections all keep the casting; nothing is matched by
    // name, effect, catalogue presence or spendable providers.
    public static class SpellbookRemovalReconciliation
    {
        public const string NotSpellbookCasting = "not-a-spellbook-casting";
        public const string NoCasterOrBook = "no-caster-or-book";
        public const string MembershipUnknown = "membership-unknown";
        public const string BookNotObserved = "book-not-observed";
        public const string StillMember = "still-held-by-the-book";
        public const string UndoRestored = "undo-restored-same-observation";

        public static SpellbookReconciliationDecision Decide(
            CastingPlanDocument document,
            PartySpellbookMembership membership,
            IReadOnlyDictionary<string, string> undoRestored)
        {
            if (document == null) throw new ArgumentNullException("document");
            var removals = new List<SpellbookRemovalCandidate>();
            var kept = new List<KeyValuePair<string, string>>();
            foreach (PlannedCasting casting in document.Castings)
            {
                string reason;
                SpellbookRemovalCandidate candidate = Evaluate(casting, membership,
                    undoRestored, out reason);
                if (candidate != null) removals.Add(candidate);
                else if (reason != null)
                    kept.Add(new KeyValuePair<string, string>(casting.CastingId, reason));
            }
            return new SpellbookReconciliationDecision(removals, kept);
        }

        private static SpellbookRemovalCandidate Evaluate(PlannedCasting casting,
            PartySpellbookMembership membership,
            IReadOnlyDictionary<string, string> undoRestored, out string reason)
        {
            reason = null;
            if (casting.Ability == null || casting.Ability.SourceKind != SourceKind.Spellbook)
                return null;
            if (string.IsNullOrWhiteSpace(casting.CasterUnitId) ||
                string.IsNullOrWhiteSpace(casting.SpellbookGuid))
            {
                reason = NoCasterOrBook;
                return null;
            }
            if (membership == null)
            {
                reason = MembershipUnknown;
                return null;
            }
            if (!membership.Stable)
            {
                reason = "membership-unstable:" + membership.InstabilityReason;
                return null;
            }
            SpellbookMembershipFact book = membership.Find(casting.CasterUnitId,
                casting.SpellbookGuid);
            if (book == null)
            {
                reason = BookNotObserved;
                return null;
            }
            if (book.Status != SpellbookMembershipStatus.Complete)
            {
                reason = "book-" + book.Status + ":" + book.StatusReason;
                return null;
            }
            string member = SpellbookMembershipFact.MemberKey(casting.Ability.BaseAbilityGuid,
                casting.Ability.MetamagicMask);
            if (book.Copies(member) > 0)
            {
                reason = StillMember;
                return null;
            }
            string observation = casting.CasterUnitId + "|" + casting.SpellbookGuid + "|" +
                member + "|" + book.Digest;
            string restored;
            if (undoRestored != null &&
                undoRestored.TryGetValue(casting.CastingId, out restored) &&
                string.Equals(restored, observation, StringComparison.Ordinal))
            {
                reason = UndoRestored;
                return null;
            }
            return new SpellbookRemovalCandidate(casting,
                book.Kind == SpellbookMembershipKind.Prepared
                    ? SpellbookRemovalReason.NoLongerPrepared
                    : SpellbookRemovalReason.NoLongerKnown, observation);
        }
    }
}
