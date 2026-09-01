using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KingmakerBuffPlanner.Compatibility
{
    internal enum SpellbookRole
    {
        CastingCapable,
        PreparationOnly,
        Ambiguous
    }

    // This input deliberately contains no availability or resource state. A depleted book is
    // still structurally cast-capable and must stay visible to provider policy.
    internal sealed class SpellbookRoleInput
    {
        internal SpellbookRoleInput(
            string spellbookGuid,
            bool spontaneous,
            bool declaresCannotUseSpells,
            string companionSpellbookGuid,
            string memorizationSpellbookGuid)
        {
            if (string.IsNullOrWhiteSpace(spellbookGuid))
                throw new ArgumentException("Spellbook GUID is required.", "spellbookGuid");
            SpellbookGuid = spellbookGuid;
            Spontaneous = spontaneous;
            DeclaresCannotUseSpells = declaresCannotUseSpells;
            CompanionSpellbookGuid = companionSpellbookGuid ?? string.Empty;
            MemorizationSpellbookGuid = memorizationSpellbookGuid ?? string.Empty;
        }

        internal string SpellbookGuid { get; private set; }
        internal bool Spontaneous { get; private set; }
        internal bool DeclaresCannotUseSpells { get; private set; }
        internal string CompanionSpellbookGuid { get; private set; }
        internal string MemorizationSpellbookGuid { get; private set; }
    }

    internal sealed class SpellbookRoleResolution
    {
        internal SpellbookRoleResolution(
            SpellbookRole role,
            string relationshipTargetGuid,
            bool included,
            string reason)
        {
            Role = role;
            RelationshipTargetGuid = relationshipTargetGuid ?? string.Empty;
            Included = included;
            Reason = reason ?? string.Empty;
        }

        internal SpellbookRole Role { get; private set; }
        internal string RelationshipTargetGuid { get; private set; }
        internal bool Included { get; private set; }
        internal string Reason { get; private set; }
    }

    internal static class SpellbookRoleResolver
    {
        internal static IReadOnlyDictionary<string, SpellbookRoleResolution> Resolve(
            IEnumerable<SpellbookRoleInput> inputs)
        {
            List<SpellbookRoleInput> values = (inputs ?? new SpellbookRoleInput[0])
                .Where(value => value != null)
                .OrderBy(value => value.SpellbookGuid, StringComparer.Ordinal)
                .ToList();
            if (values.Select(value => value.SpellbookGuid)
                .Distinct(StringComparer.Ordinal).Count() != values.Count)
                throw new ArgumentException("Spellbook role inputs contain duplicate GUIDs.", "inputs");

            var byGuid = values.ToDictionary(value => value.SpellbookGuid,
                StringComparer.Ordinal);
            var results = new Dictionary<string, SpellbookRoleResolution>(StringComparer.Ordinal);
            foreach (SpellbookRoleInput value in values)
                results.Add(value.SpellbookGuid, ResolveOne(value, byGuid));
            return new ReadOnlyDictionary<string, SpellbookRoleResolution>(results);
        }

        private static SpellbookRoleResolution ResolveOne(
            SpellbookRoleInput value,
            IDictionary<string, SpellbookRoleInput> byGuid)
        {
            SpellbookRoleInput relationship;
            if (value.DeclaresCannotUseSpells &&
                IsOwnedCastingTarget(value.CompanionSpellbookGuid, byGuid, out relationship))
            {
                return new SpellbookRoleResolution(SpellbookRole.PreparationOnly,
                    relationship.SpellbookGuid, false,
                    "cannot-use-spells-with-owned-companion-casting-book");
            }

            if (value.DeclaresCannotUseSpells)
            {
                SpellbookRoleInput castingReference = byGuid.Values.FirstOrDefault(candidate =>
                    string.Equals(candidate.MemorizationSpellbookGuid, value.SpellbookGuid,
                        StringComparison.Ordinal) && !candidate.DeclaresCannotUseSpells);
                if (castingReference != null)
                {
                    return new SpellbookRoleResolution(SpellbookRole.PreparationOnly,
                        castingReference.SpellbookGuid, false,
                        "cannot-use-spells-with-owned-casting-reference");
                }
                return new SpellbookRoleResolution(SpellbookRole.Ambiguous,
                    RelationshipTarget(value), true,
                    "cannot-use-spells-relationship-unproven");
            }

            if (IsOwnedPreparationTarget(value.MemorizationSpellbookGuid, byGuid, out relationship))
            {
                return new SpellbookRoleResolution(SpellbookRole.CastingCapable,
                    relationship.SpellbookGuid, true,
                    "casting-book-with-owned-memorization-reference");
            }
            return new SpellbookRoleResolution(SpellbookRole.CastingCapable,
                RelationshipTarget(value), true, "ordinary-or-unproven-casting-book");
        }

        private static bool IsOwnedCastingTarget(
            string relationshipGuid,
            IDictionary<string, SpellbookRoleInput> byGuid,
            out SpellbookRoleInput target)
        {
            if (!string.IsNullOrWhiteSpace(relationshipGuid) &&
                byGuid.TryGetValue(relationshipGuid, out target) &&
                !target.DeclaresCannotUseSpells)
                return true;
            target = null;
            return false;
        }

        private static bool IsOwnedPreparationTarget(
            string relationshipGuid,
            IDictionary<string, SpellbookRoleInput> byGuid,
            out SpellbookRoleInput target)
        {
            if (!string.IsNullOrWhiteSpace(relationshipGuid) &&
                byGuid.TryGetValue(relationshipGuid, out target))
                return true;
            target = null;
            return false;
        }

        private static string RelationshipTarget(SpellbookRoleInput value)
        {
            return !string.IsNullOrWhiteSpace(value.CompanionSpellbookGuid)
                ? value.CompanionSpellbookGuid
                : value.MemorizationSpellbookGuid;
        }
    }
}
