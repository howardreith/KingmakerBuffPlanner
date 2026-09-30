using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    // The pure planning-side Share Transmutation (everyday-use v1.2 §7,
    // review addendum §3, review F2/F4). ONE registration resolves the
    // exact casting's caster through immutable VERIFIED capability facts —
    // the targeting-affecting enhancement snapshots the installed-provider
    // integration produced — so a casting by any capable caster is costed
    // against ITS OWN reservoir regardless of party order. A capability is
    // NOT a caster-level feature flag: it carries the integration's exact
    // source contract (the verified supported ability and spellbook
    // identities) and the verified legal recipients, so an unrelated
    // self-only source of the same caster is refused exactly as the native
    // integration would refuse it. It expands a PERSONAL supported
    // transmutation to those recipients, declares the reservoir demand in
    // the compiler's atomic cost vector, and never arms a live toggle,
    // reads live game state or spends anything. Affordability (including a
    // verified-zero remaining balance) is NOT decided here: the capability
    // and its cost shape survive any balance, and the shared ledger decides
    // whether a cast is needed (an already-active effect needs none) and
    // whether its complete cost is fundable. Deterministic: same facts →
    // same result.
    public sealed class ShareCastingModifier : ICastingTargetingModifier
    {
        public const string Id = "share-transmutation";

        // One verified capability: the feature-owning caster's reservoir
        // pool and per-use cost exactly as the installed provider
        // integration reported them, the exact supported sources it
        // verified for that caster, and the verified legal recipients.
        public sealed class ShareCapability
        {
            public ShareCapability(string casterUnitId, string reservoirPoolId,
                int unitsPerUse, int? remainingUses,
                IEnumerable<string> supportedAbilityGuids,
                IEnumerable<string> supportedSpellbookGuids,
                IEnumerable<string> legalRecipientUnitIds,
                string executionEnhancementId = null)
            {
                ExecutionEnhancementId = string.IsNullOrWhiteSpace(
                    executionEnhancementId) ? null : executionEnhancementId;
                if (string.IsNullOrWhiteSpace(casterUnitId))
                    throw new ArgumentException("Caster unit ID is required.", "casterUnitId");
                CasterUnitId = casterUnitId;
                ReservoirPoolId = reservoirPoolId;
                UnitsPerUse = Math.Max(1, unitsPerUse);
                RemainingUses = remainingUses;
                SupportedAbilityGuids = Ordered(supportedAbilityGuids);
                SupportedSpellbookGuids = Ordered(supportedSpellbookGuids);
                LegalRecipientUnitIds = Ordered(legalRecipientUnitIds);
            }

            public string CasterUnitId { get; private set; }
            public string ReservoirPoolId { get; private set; }
            public int UnitsPerUse { get; private set; }
            // Null when the installed contract could not read a balance:
            // unknown is never free, and never a refusal here (the ledger
            // refuses an unknown demanded pool fail-closed).
            public int? RemainingUses { get; private set; }
            // The verified exact-source contract (review F2): only these
            // selected ability identities in these spellbooks were proven
            // supported for this caster by the installed integration.
            public IReadOnlyList<string> SupportedAbilityGuids { get; private set; }
            public IReadOnlyList<string> SupportedSpellbookGuids { get; private set; }
            // The verified recipients Share may reach for this caster.
            public IReadOnlyList<string> LegalRecipientUnitIds { get; private set; }
            // The integration's own enhancement snapshot id that EXECUTES
            // Share for this caster (strategy resolution + the executor's
            // enhancement lease). Null only in synthetic fixtures; the host
            // always passes the snapshot's id.
            public string ExecutionEnhancementId { get; private set; }
        }

        private readonly Dictionary<string, ShareCapability> _capabilities;

        public ShareCastingModifier(IEnumerable<ShareCapability> capabilities)
        {
            _capabilities = (capabilities ?? new ShareCapability[0])
                .Where(value => value != null)
                .GroupBy(value => value.CasterUnitId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(),
                    StringComparer.Ordinal);
        }

        public string ModifierId
        {
            get { return Id; }
        }

        public CastingModifierResult Apply(PlannedCasting casting, ProviderPlanningOption option)
        {
            if (option == null || option.Provider == null)
                return CastingModifierResult.Unavailable("share-source-unresolved");
            // The casting's authored caster is the intent; the resolved
            // provider's caster is the fallback for a prospective draft.
            string caster = casting != null && !string.IsNullOrWhiteSpace(casting.CasterUnitId)
                ? casting.CasterUnitId
                : option.Provider.Key.CasterUnitId;
            ShareCapability capability;
            if (!_capabilities.TryGetValue(caster, out capability))
                return CastingModifierResult.Unavailable("share-feature-unavailable:" + caster);
            // The exact-source contract the integration verified for THIS
            // caster: a genuine spellbook spell whose spellbook and selected
            // ability identity are both in the verified whitelists. A caster
            // with one supported spell never inherits support for an
            // unrelated self-only source (review F2).
            string sourceFailure = SourceSupportFailure(option.Provider.Key, capability);
            if (sourceFailure != null)
                return CastingModifierResult.Unavailable(
                    "share-source-not-supported:" + sourceFailure);
            if (capability.LegalRecipientUnitIds.Count == 0)
                return CastingModifierResult.Unavailable("share-no-legal-recipient");
            // Personal spells reach only their caster; Share extends exactly
            // those to the verified legal recipients. A spell that can
            // already target others does not need (and cannot use) Share.
            List<string> reachable = (option.ReachableTargetIds ?? new string[0]).ToList();
            bool personal = reachable.Count == 1 &&
                string.Equals(reachable[0], caster, StringComparison.Ordinal);
            if (!personal)
                return CastingModifierResult.Unavailable(
                    "share-not-needed:the spell can already target others");
            // Legality is decided above; affordability never is (review F4):
            // a verified-zero or unknown balance keeps the capability and
            // its cost shape so the compiler/ledger can still conclude
            // AlreadySatisfied for a sufficiently active effect, or block
            // atomically for the real shortage.
            var expanded = new HashSet<string>(reachable, StringComparer.Ordinal);
            foreach (string ally in capability.LegalRecipientUnitIds) expanded.Add(ally);
            return CastingModifierResult.Applied(new ProviderPlanningOption(
                option.Provider, expanded.OrderBy(value => value, StringComparer.Ordinal),
                option.LegalAnchorIds, option.EffectiveCasterLevel,
                option.ExpectedDurationRounds, option.ExecutionStrategy,
                option.ExecutionStrategyReason,
                option.RecipientIdsByAnchor == null
                    ? null
                    : option.RecipientIdsByAnchor.ToDictionary(
                        pair => pair.Key,
                        pair => (IEnumerable<string>)pair.Value,
                        StringComparer.Ordinal)));
        }

        // R579-1: the VERIFIED native execution identity — the executor
        // arms exactly this enhancement for the one cast, and strategy
        // resolution sees it through the existing execution policy.
        public IReadOnlyList<string> ExecutionEnhancementIds(
            PlannedCasting casting, ProviderPlanningOption option)
        {
            ShareCapability capability = CapabilityOf(casting, option);
            return capability == null || capability.ExecutionEnhancementId == null
                ? new string[0]
                : new[] { capability.ExecutionEnhancementId };
        }

        public IReadOnlyList<ModifierUsageDemand> UsageDemands(
            PlannedCasting casting, ProviderPlanningOption option)
        {
            ShareCapability capability = null;
            string caster = casting == null ? null : casting.CasterUnitId;
            if (caster != null) _capabilities.TryGetValue(caster, out capability);
            if (capability == null && option != null && option.Provider != null)
                _capabilities.TryGetValue(option.Provider.Key.CasterUnitId, out capability);
            if (capability == null)
            {
                // An unverified caster must never be costed as free: the
                // demand names the failure and the ledger's fail-closed
                // unknown-pool rule makes it unsatisfiable atomically.
                return new[] { new ModifierUsageDemand("share-unverified-caster", int.MaxValue) };
            }
            return new[]
            {
                new ModifierUsageDemand(capability.ReservoirPoolId, capability.UnitsPerUse)
            };
        }

        private ShareCapability CapabilityOf(PlannedCasting casting,
            ProviderPlanningOption option)
        {
            ShareCapability capability = null;
            string caster = casting == null ? null : casting.CasterUnitId;
            if (caster != null) _capabilities.TryGetValue(caster, out capability);
            if (capability == null && option != null && option.Provider != null)
                _capabilities.TryGetValue(option.Provider.Key.CasterUnitId, out capability);
            return capability;
        }

        // The same applicability semantics as the enhancement snapshots the
        // integration produced (CastEnhancementSnapshot.ApplicabilityFailure
        // for a class feature), carried into the pure capability so the
        // modifier judges the exact resolved source, not a caster flag.
        private static string SourceSupportFailure(ProviderKey key, ShareCapability capability)
        {
            if (key == null || key.Ability == null)
                return "provider-missing";
            if (key.Ability.SourceKind != SourceKind.Spellbook)
                return "source-not-genuine-spellbook-spell";
            if (!capability.SupportedSpellbookGuids.Contains(key.SpellbookGuid))
                return "spellbook-not-qualified:" + key.SpellbookGuid;
            string selected = string.IsNullOrWhiteSpace(key.Ability.VariantGuid)
                ? key.Ability.BaseAbilityGuid
                : key.Ability.VariantGuid;
            return capability.SupportedAbilityGuids.Contains(selected)
                ? null
                : "ability-not-qualified:" + selected;
        }

        private static IReadOnlyList<string> Ordered(IEnumerable<string> values)
        {
            return (values ?? new string[0])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();
        }
    }
}
