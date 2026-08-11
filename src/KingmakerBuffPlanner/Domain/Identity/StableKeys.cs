using System;

namespace KingmakerBuffPlanner.Domain.Identity
{
    public enum SourceKind
    {
        Spellbook,
        AbilityResource,
        Item,
        Fact
    }

    public sealed class AbilityKey : IEquatable<AbilityKey>
    {
        public AbilityKey(
            string baseAbilityGuid,
            string variantGuid,
            int metamagicMask,
            SourceKind sourceKind,
            string specialSourceId)
        {
            if (string.IsNullOrWhiteSpace(baseAbilityGuid))
                throw new ArgumentException("Base ability GUID is required.", "baseAbilityGuid");
            if (metamagicMask < 0) throw new ArgumentOutOfRangeException("metamagicMask");
            BaseAbilityGuid = baseAbilityGuid;
            VariantGuid = variantGuid ?? string.Empty;
            MetamagicMask = metamagicMask;
            SourceKind = sourceKind;
            SpecialSourceId = specialSourceId ?? string.Empty;
            Canonical = ((int)sourceKind) + "|" + BaseAbilityGuid + "|" + VariantGuid + "|" +
                MetamagicMask + "|" + SpecialSourceId;
        }

        public string BaseAbilityGuid { get; private set; }
        public string VariantGuid { get; private set; }
        public int MetamagicMask { get; private set; }
        public SourceKind SourceKind { get; private set; }
        public string SpecialSourceId { get; private set; }
        public string Canonical { get; private set; }

        public bool Equals(AbilityKey other)
        {
            return other != null && string.Equals(Canonical, other.Canonical, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) { return Equals(obj as AbilityKey); }
        public override int GetHashCode() { return Canonical.GetHashCode(); }
        public override string ToString() { return Canonical; }
    }

    public sealed class ProviderKey : IEquatable<ProviderKey>
    {
        public ProviderKey(
            string casterUnitId,
            string spellbookGuid,
            AbilityKey ability,
            string sourceInstanceId)
        {
            if (string.IsNullOrWhiteSpace(casterUnitId))
                throw new ArgumentException("Caster unit ID is required.", "casterUnitId");
            Ability = ability ?? throw new ArgumentNullException("ability");
            CasterUnitId = casterUnitId;
            SpellbookGuid = spellbookGuid ?? string.Empty;
            SourceInstanceId = sourceInstanceId ?? string.Empty;
            Canonical = CasterUnitId + "|" + SpellbookGuid + "|" + Ability.Canonical + "|" + SourceInstanceId;
        }

        public string CasterUnitId { get; private set; }
        public string SpellbookGuid { get; private set; }
        public AbilityKey Ability { get; private set; }
        public string SourceInstanceId { get; private set; }
        public string Canonical { get; private set; }

        public bool Equals(ProviderKey other)
        {
            return other != null && string.Equals(Canonical, other.Canonical, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) { return Equals(obj as ProviderKey); }
        public override int GetHashCode() { return Canonical.GetHashCode(); }
        public override string ToString() { return Canonical; }
    }
}
