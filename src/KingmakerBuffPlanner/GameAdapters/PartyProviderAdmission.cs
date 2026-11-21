using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.GameAdapters
{
    // The game reads one owned ability selection contributes to provider
    // admission. KingmakerPartySnapshotBuilder fills it from live AbilityData
    // (spellbook, fact or item); offline tests fill it from detached native
    // blueprints (review R1-F8), so the decisions below are the production
    // decisions in both cases.
    internal sealed class ProviderAbilityRead
    {
        // The concrete blueprint the discovery gate judges.
        internal BlueprintAbility Blueprint;
        internal AbilityKey Ability;
        internal string DisplayName = string.Empty;
        internal string SourceDisplayName = string.Empty;
        internal int VariantOrder;
        // Read only once discovery has admitted the ability (as before the
        // extraction: caster level, texts and material are not read for a
        // refused candidate).
        internal Func<int> CasterLevelRead = () => 0;
        internal Func<string> DescriptionRead = () => string.Empty;
        internal Func<string> DurationTextRead = () => string.Empty;
        internal Func<MaterialRequirementSnapshot> MaterialRead = () => null;

        private int? _casterLevel;
        internal int CasterLevel
        {
            get { if (!_casterLevel.HasValue) _casterLevel = CasterLevelRead(); return _casterLevel.Value; }
        }
    }

    // The game-free half of party provider discovery: the castable-catalogue
    // gate (KingmakerBuffSourceDiscovery), its traces, the resource pools a
    // spellbook level or native ability resource contributes, and the
    // provider keys, costs and duplicate/ambiguity rules. Unity and Kingmaker
    // state stays in KingmakerPartySnapshotBuilder, which only reads values.
    internal sealed class PartyProviderAdmission
    {
        private readonly KingmakerBuffSourceDiscovery _discovery;

        internal PartyProviderAdmission(KingmakerBuffSourceDiscovery discovery)
        {
            _discovery = discovery ?? throw new ArgumentNullException("discovery");
        }

        internal readonly Dictionary<string, EffectExpression> EffectsBySource =
            new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
        internal readonly List<PartySourceDiscoveryTrace> Traces = new List<PartySourceDiscoveryTrace>();
        // Fact providers refused as ambiguous (one ability, one pool, two
        // caster levels); later duplicates are refused too.
        internal readonly HashSet<ProviderKey> AmbiguousFactKeys = new HashSet<ProviderKey>();
        internal int RawCandidateCount;
        internal int BeneficialCandidateCount;

        internal const string UnresolvedFactPrefix = "unresolved-fact:";

        internal void Clear()
        {
            EffectsBySource.Clear();
            Traces.Clear();
            AmbiguousFactKeys.Clear();
            RawCandidateCount = 0;
            BeneficialCandidateCount = 0;
        }

        internal static string PoolKey(string unitId, string spellbookGuid, string suffix)
        {
            return unitId + "|spellbook|" + spellbookGuid + "|" + suffix;
        }

        // One spontaneous spell level (above 0): its slots are one pool.
        internal static ResourcePoolSnapshot SpontaneousLevelPool(string unitId, string spellbookGuid,
            int level, int remainingSlots, int spellsPerDay)
        {
            int remaining = Math.Max(0, remainingSlots);
            return new ResourcePoolSnapshot(PoolKey(unitId, spellbookGuid, "spontaneous-" + level),
                ResourcePoolKind.SpontaneousLevel, Math.Max(remaining, spellsPerDay), remaining, null);
        }

        // A spellbook spell: admitted only when discovery admits it into the
        // castable catalogue; one provider per (caster, book, ability, spell
        // level and heighten).
        internal ProviderSnapshot AdmitSpell(string unitId, string spellbookGuid, bool prepared,
            string spellbookName, ProviderAbilityRead read, int spellLevel, int heighten,
            string poolKey, int cost, IEnumerable<string> tokens, List<ProviderSnapshot> providers)
        {
            if (read == null || read.Blueprint == null) return null;
            RawCandidateCount++;
            EffectExpression expression;
            string reason;
            bool beneficial = _discovery.TryDiscover(read.Blueprint, out expression, out reason);
            Traces.Add(new PartySourceDiscoveryTrace(read.Ability.Canonical, read.Blueprint.AssetGuid,
                read.DisplayName, unitId, spellbookGuid, prepared, beneficial, reason));
            if (!beneficial) return null;
            BeneficialCandidateCount++;
            EffectsBySource[read.Ability.Canonical] = expression;
            var key = new ProviderKey(unitId, spellbookGuid, read.Ability,
                "level-" + spellLevel + "|heighten-" + heighten);
            if (providers.Any(p => p.Key.Equals(key))) return null;
            string duration = read.DurationTextRead();
            int level = read.CasterLevel;
            DurationEstimate estimate = KingmakerDurationAdapter.Estimate(read.Blueprint, level);
            var provider = new ProviderSnapshot(key, read.DisplayName, spellLevel, poolKey, cost, tokens,
                read.MaterialRead(), level, estimate.BaseRounds, read.DescriptionRead(),
                duration, read.SourceDisplayName, read.VariantOrder,
                spellbookName ?? string.Empty, duration: estimate);
            providers.Add(provider);
            return provider;
        }

        // A class/race/item ability: admitted when discovery admits it AND
        // its native resource contract was read (KingmakerFactResources);
        // the provider spends exactly that native pool and cost.
        internal ProviderSnapshot AdmitFact(string unitId, ProviderAbilityRead read, bool resourceKnown,
            KingmakerFactResource resource, string resourceRefusal, string slotSelector,
            List<ProviderSnapshot> providers, List<ResourcePoolSnapshot> pools, HashSet<string> poolKeys)
        {
            if (read == null || read.Blueprint == null) return null;
            RawCandidateCount++;
            EffectExpression expression;
            string reason;
            bool beneficial = _discovery.TryDiscover(read.Blueprint, out expression, out reason);
            Traces.Add(new PartySourceDiscoveryTrace(read.Ability.Canonical, read.Blueprint.AssetGuid,
                read.DisplayName, unitId, string.Empty, false, beneficial, reason));
            if (!beneficial) return null;
            BeneficialCandidateCount++;
            EffectsBySource[read.Ability.Canonical] = expression;
            if (!resourceKnown || resource == null)
            {
                Traces.Add(new PartySourceDiscoveryTrace(read.Ability.Canonical, read.Blueprint.AssetGuid,
                    read.DisplayName, unitId, string.Empty, false, false, resourceRefusal));
                return null;
            }
            ResourcePoolSnapshot pool = resource.Pool;
            if (poolKeys.Add(pool.PoolKey)) pools.Add(pool);
            var key = new ProviderKey(unitId, string.Empty, read.Ability, resource.InstanceId);
            if (AmbiguousFactKeys.Contains(key)) return null;
            ProviderSnapshot existing = providers.FirstOrDefault(provider => provider.Key.Equals(key));
            if (existing != null)
            {
                // Re-review of A4: the same ability from the same pool at two
                // caster levels (one cantrip granted by two classes) is
                // ambiguous; neither is offered, the refusal stays traced.
                if (string.Equals(existing.ResourcePoolKey, pool.PoolKey, StringComparison.Ordinal) &&
                    existing.EffectiveCasterLevel != read.CasterLevel)
                {
                    providers.Remove(existing);
                    AmbiguousFactKeys.Add(key);
                    Traces.Add(new PartySourceDiscoveryTrace(read.Ability.Canonical, read.Blueprint.AssetGuid,
                        read.DisplayName, unitId, string.Empty, false, false, UnresolvedFactPrefix +
                        "caster-levels:" + existing.EffectiveCasterLevel + "," + read.CasterLevel));
                }
                return null;
            }
            string duration = read.DurationTextRead();
            int level = read.CasterLevel;
            DurationEstimate estimate = KingmakerDurationAdapter.Estimate(read.Blueprint, level);
            var created = new ProviderSnapshot(key, read.DisplayName, 0, pool.PoolKey, resource.Cost, null,
                read.MaterialRead(), level, estimate.BaseRounds, read.DescriptionRead(), duration,
                read.SourceDisplayName, read.VariantOrder, string.Empty, slotSelector ?? string.Empty, estimate);
            providers.Add(created);
            return created;
        }
    }

    internal sealed class PartySourceDiscoveryTrace
    {
        internal PartySourceDiscoveryTrace(
            string sourceId,
            string blueprintGuid,
            string displayName,
            string casterUnitId,
            string spellbookGuid,
            bool prepared,
            bool beneficial,
            string reason)
        {
            SourceId = sourceId ?? string.Empty;
            BlueprintGuid = blueprintGuid ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CasterUnitId = casterUnitId ?? string.Empty;
            SpellbookGuid = spellbookGuid ?? string.Empty;
            Prepared = prepared;
            Beneficial = beneficial;
            Reason = reason ?? string.Empty;
        }

        internal string SourceId { get; private set; }
        internal string BlueprintGuid { get; private set; }
        internal string DisplayName { get; private set; }
        internal string CasterUnitId { get; private set; }
        internal string SpellbookGuid { get; private set; }
        internal bool Prepared { get; private set; }
        internal bool Beneficial { get; private set; }
        internal string Reason { get; private set; }
    }
}
