using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KingmakerBuffPlanner.Domain.Providers
{
    // 0.4.2 (B): what one native spellbook holds, read from the book itself
    // (never from discovery, the catalogue or spendable providers). A
    // prepared book holds the spells memorized in its slots - a spent slot,
    // and a slot awaiting the next rest, still holds its spell; a
    // spontaneous book holds its known, special and custom (metamagic)
    // spells.
    public enum SpellbookMembershipKind
    {
        Prepared,
        Known
    }

    // Only Complete proves anything; every other state keeps saved castings.
    public enum SpellbookMembershipStatus
    {
        Complete,
        Unreadable,
        RoleNotProven,
        CasterUnavailable
    }

    public sealed class SpellbookMembershipFact
    {
        private readonly Dictionary<string, int> _copies;

        public SpellbookMembershipFact(
            string casterUnitId,
            string spellbookGuid,
            SpellbookMembershipKind kind,
            SpellbookMembershipStatus status,
            string statusReason,
            IEnumerable<KeyValuePair<string, int>> members)
        {
            if (string.IsNullOrWhiteSpace(casterUnitId))
                throw new ArgumentException("Caster unit ID is required.", "casterUnitId");
            if (string.IsNullOrWhiteSpace(spellbookGuid))
                throw new ArgumentException("Spellbook GUID is required.", "spellbookGuid");
            CasterUnitId = casterUnitId;
            SpellbookGuid = spellbookGuid;
            Kind = kind;
            Status = status;
            StatusReason = statusReason ?? string.Empty;
            _copies = new Dictionary<string, int>(StringComparer.Ordinal);
            if (status == SpellbookMembershipStatus.Complete)
                foreach (KeyValuePair<string, int> member in members ??
                    new KeyValuePair<string, int>[0])
                {
                    if (string.IsNullOrWhiteSpace(member.Key) || member.Value < 1) continue;
                    int prior;
                    _copies.TryGetValue(member.Key, out prior);
                    _copies[member.Key] = prior + member.Value;
                }
            Members = new ReadOnlyCollection<string>(_copies.Keys
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
            Digest = ComputeDigest();
        }

        public string CasterUnitId { get; private set; }
        public string SpellbookGuid { get; private set; }
        public SpellbookMembershipKind Kind { get; private set; }
        public SpellbookMembershipStatus Status { get; private set; }
        public string StatusReason { get; private set; }
        public IReadOnlyList<string> Members { get; private set; }

        // Identity of what the book holds (which spells, never how many
        // copies or how many are spendable): it changes only with a native
        // edit of the book's contents.
        public string Digest { get; private set; }

        // One member: the spell as the book holds it (a variant's parent,
        // since the book memorizes or knows the parent) and its metamagic.
        public static string MemberKey(string baseAbilityGuid, int metamagicMask)
        {
            return (baseAbilityGuid ?? string.Empty) + "|" +
                metamagicMask.ToString(CultureInfo.InvariantCulture);
        }

        public int Copies(string memberKey)
        {
            int copies;
            return memberKey != null && _copies.TryGetValue(memberKey, out copies) ? copies : 0;
        }

        private string ComputeDigest()
        {
            string text = Kind + "|" + Status + "|" + string.Join(";", Members.ToArray());
            using (var hash = SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(text));
                var builder = new StringBuilder(16);
                for (int index = 0; index < 8; index++)
                    builder.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }
    }

    // One observation of the party's spellbooks. Unstable (loading, a
    // native spellbook or level-up window open, the read failed) proves
    // nothing about any book; a book that was not observed at all (an
    // absent caster, a book the caster no longer has) proves nothing either.
    public sealed class PartySpellbookMembership
    {
        public PartySpellbookMembership(
            bool stable,
            string instabilityReason,
            IEnumerable<SpellbookMembershipFact> books,
            IEnumerable<KeyValuePair<string, string>> abilityNames = null,
            Func<string, string> nameResolver = null,
            Func<string, bool> spellResolver = null)
        {
            _nameResolver = nameResolver;
            _spellResolver = spellResolver;
            Stable = stable;
            InstabilityReason = stable ? string.Empty : instabilityReason ?? "unstable";
            var list = stable ? (books ?? new SpellbookMembershipFact[0])
                .Where(value => value != null).ToList() : new List<SpellbookMembershipFact>();
            if (list.Select(value => value.CasterUnitId + "|" + value.SpellbookGuid)
                    .Distinct(StringComparer.Ordinal).Count() != list.Count)
                throw new ArgumentException("A spellbook was observed twice.", "books");
            Books = new ReadOnlyCollection<SpellbookMembershipFact>(list);
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in abilityNames ??
                new KeyValuePair<string, string>[0])
                if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                    names[pair.Key] = pair.Value;
            AbilityNames = new ReadOnlyDictionary<string, string>(names);
        }

        private readonly Func<string, string> _nameResolver;
        private readonly Func<string, bool> _spellResolver;

        // Whether the running game knows the spell at all. A spell whose
        // blueprint does not resolve (its mod is not loaded) cannot be held
        // by any book, so its absence proves nothing. Without a resolver
        // (Unity-free fixtures) every spell resolves; a resolver that throws
        // is an unknown, never a proof.
        public bool SpellResolves(string baseAbilityGuid)
        {
            if (string.IsNullOrWhiteSpace(baseAbilityGuid)) return false;
            if (_spellResolver == null) return true;
            try
            {
                return _spellResolver(baseAbilityGuid);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // The spell's localized name for the notice: the observed names,
        // then the game's own blueprint name; null when neither is known.
        public string NameOf(string baseAbilityGuid)
        {
            string name;
            if (string.IsNullOrWhiteSpace(baseAbilityGuid)) return null;
            if (AbilityNames.TryGetValue(baseAbilityGuid, out name)) return name;
            if (_nameResolver == null) return null;
            try
            {
                name = _nameResolver(baseAbilityGuid);
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static PartySpellbookMembership Unstable(string reason)
        {
            return new PartySpellbookMembership(false, reason, null);
        }

        public bool Stable { get; private set; }
        public string InstabilityReason { get; private set; }
        public IReadOnlyList<SpellbookMembershipFact> Books { get; private set; }
        // Localized names of the spells the book held or the plan names,
        // keyed by base ability GUID, for the player's notice only.
        public IReadOnlyDictionary<string, string> AbilityNames { get; private set; }

        public SpellbookMembershipFact Find(string casterUnitId, string spellbookGuid)
        {
            return Books.FirstOrDefault(value =>
                string.Equals(value.CasterUnitId, casterUnitId, StringComparison.Ordinal) &&
                string.Equals(value.SpellbookGuid, spellbookGuid, StringComparison.Ordinal));
        }
    }
}
