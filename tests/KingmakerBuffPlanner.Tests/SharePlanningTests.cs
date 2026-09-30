using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using KingmakerBuffPlanner.Execution;

namespace KingmakerBuffPlanner.Tests
{
    // Everyday-use v1.2 §7 Share Transmutation planning pipeline (review
    // F2/F4/F5): the pure modifier carries the VERIFIED exact-source
    // contract (supported ability + spellbook whitelists, per-caster legal
    // recipients), separates legality from affordability (a zero reservoir
    // never refuses an already-satisfied or structurally-valid casting),
    // and the shared ledger fails closed on demanded pools it has no
    // verified balance for. The graph gesture carries the draft's Share
    // selection through the same resolver; disabling Share keeps an
    // existing ally casting blocked, repairable, persisted.
    internal static partial class Program
    {
        private const string ShareActivatableGuid =
            "8641e6c39ff133ad71f669e35e1ee688";
        private const string WizardReservoir = "class-feature-resource|unit-wiz|reservoir";

        private static void RunSharePlanningTests(string root)
        {
            Run("share-expands-personal-targets-purely", TestSharePureExpansion);
            Run("share-graph-gesture-carries-and-persists", () => TestShareGraphPersistence(root));
            Run("share-compile-budget-is-atomic-and-fail-closed", () => TestShareCompileBudget(root));
            Run("share-zero-reservoir-versus-active-effects", () => TestShareZeroReservoir(root));
            Run("share-ordinary-discovery-never-arms-the-native-toggle",
                TestShareDiscoveryBoundary);
            Run("share-projection-executes-only-the-verified-modifier-contract",
                TestShareProjectionContract);
            Run("share-alone-selects-the-provider-direct-strategy",
                TestShareStrategyResolution);
            Run("enhancement-cleanup-is-observable-and-halts-later-casts",
                TestEnhancementCleanupObservable);
            Run("recovery-preservation-never-drops-intent",
                () => TestRecoveryPreservation(root));
            Run("native-cleanup-verifies-the-expected-final-state",
                TestNativeCleanupExpectedState);
        }

        private static AbilityKey ShapeAbility()
        {
            return new AbilityKey("beast-shape", null, 0, SourceKind.Spellbook, "book");
        }

        private static ProviderSnapshot ShapeProvider(string caster)
        {
            return new ProviderSnapshot(
                new ProviderKey(caster, "book", ShapeAbility(), "pool-" + caster),
                "Beast Shape II — Bear", 2, "pool-" + caster, 1, null, null, 5, 600,
                "You become a bear.", "10 minutes", string.Empty, 0, "book");
        }

        private static ProviderPlanningOption PersonalOption(string caster)
        {
            // A personal transmutation reaches only its caster.
            return new ProviderPlanningOption(ShapeProvider(caster),
                new[] { caster }, new string[0], 5, 600);
        }

        // The VERIFIED capability shape the host builds from the installed
        // integration's snapshot: reservoir identity and per-use cost, the
        // exact supported sources, and the verified recipients.
        private static ShareCastingModifier.ShareCapability Capability(
            string caster, string poolId, int unitsPerUse, int? remaining,
            IEnumerable<string> recipients = null)
        {
            return new ShareCastingModifier.ShareCapability(caster, poolId,
                unitsPerUse, remaining, new[] { "beast-shape" }, new[] { "book" },
                recipients ?? new[] { "unit-cleric", "unit-fighter" });
        }

        // The Share enhancement snapshot exactly as the integration emits
        // it (whitelists = the verified supported sources; the reservoir
        // pool the ledger must know to fund the modifier's demand).
        private static CastEnhancementSnapshot ShareEnhancement(
            string caster, string poolId, int? remaining,
            string directCastProviderId = null, bool requiresNativeCommand = false)
        {
            return new CastEnhancementSnapshot(
                "share-transmutation|" + caster + "|" + ShareActivatableGuid,
                caster, ShareActivatableGuid,
                "Share Transmutation", "Share a personal transmutation with an ally.",
                CastEnhancementCategory.ClassFeature, 0, 0, remaining,
                new[] { "beast-shape" }, "Share Transmutation", new[] { "book" },
                poolId, requiresNativeCommand, "brown-fur-share-transmutation", 1, true,
                "brown-fur-share-transmutation", "Arcane Reservoir",
                directCastProviderId);
        }

        private static PlannedCasting SharedCasting(string id, string caster, string target,
            int order = 0, ExistingEffectPolicy policy = ExistingEffectPolicy.SkipAlreadyActive)
        {
            return new PlannedCasting(id, "long", order, "source-shape",
                ShapeAbility(), caster, "book", CastingTargetMode.DirectTarget,
                target, null, null,
                new[] { new TargetingModifierSelection(ShareCastingModifier.Id, true, null) },
                null, policy, null, CastingAuthoringState.Ready, null);
        }

        private static void TestSharePureExpansion()
        {
            // Review addendum §3 + F2: verified per-caster capabilities
            // carrying the EXACT-SOURCE contract; the reservoir and cost
            // follow the casting's OWN caster regardless of
            // registration/party order.
            var capabilities = new[]
            {
                Capability("unit-wiz", "class-feature-resource|unit-wiz|reservoir", 1, 3),
                Capability("unit-sorcerer", "class-feature-resource|unit-sorcerer|reservoir",
                    2, 1)
            };
            var share = new ShareCastingModifier(capabilities);
            var casting = new PlannedCasting("cast-share", "long", 0, "source-shape",
                ShapeAbility(), "unit-wiz", "book", CastingTargetMode.DirectTarget,
                "unit-cleric", null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            CastingModifierResult result = share.Apply(casting, PersonalOption("unit-wiz"));
            if (!result.IsApplied)
                throw new InvalidOperationException("Share did not apply to a personal spell: " +
                    result.UnavailableReason);
            if (!result.Option.ReachableTargetIds.Contains("unit-cleric") ||
                !result.Option.ReachableTargetIds.Contains("unit-fighter"))
                throw new InvalidOperationException("Share did not expand to the legal allies.");
            if (!result.Option.ReachableTargetIds.Contains("unit-wiz"))
                throw new InvalidOperationException("Share lost the caster's own eligibility.");
            // Purity: same inputs → same result (recompilation cannot leak).
            CastingModifierResult again = share.Apply(casting, PersonalOption("unit-wiz"));
            if (!again.IsApplied || !again.Option.ReachableTargetIds.SequenceEqual(
                    result.Option.ReachableTargetIds))
                throw new InvalidOperationException("Share is not deterministic.");
            // A spell that can already target others does not need Share.
            var sharedAlready = new ProviderPlanningOption(ShapeProvider("unit-wiz"),
                new[] { "unit-wiz", "unit-cleric" }, new string[0], 5, 600);
            CastingModifierResult notNeeded = share.Apply(casting, sharedAlready);
            if (notNeeded.IsApplied || !notNeeded.UnavailableReason.StartsWith("share-not-needed"))
                throw new InvalidOperationException("Share applied to an already-shareable spell.");
            // The verified reservoir demand: the same pool as Powerful Change.
            IReadOnlyList<ModifierUsageDemand> demands = share.UsageDemands(casting, result.Option);
            if (demands.Count != 1 || demands[0].Units != 1 ||
                demands[0].UsagePoolId != "class-feature-resource|unit-wiz|reservoir")
                throw new InvalidOperationException("Share did not declare the verified " +
                    "reservoir demand.");
            // No legal recipients in the CAPABILITY: honest refusal.
            var none = new ShareCastingModifier(new[]
            {
                Capability("unit-wiz", "class-feature-resource|unit-wiz|reservoir", 1, 3,
                    new string[0])
            });
            if (none.Apply(casting, PersonalOption("unit-wiz")).IsApplied)
                throw new InvalidOperationException("Share applied with no legal recipients.");
            // §3: a NON-FIRST capable caster costs its OWN reservoir with
            // its OWN per-use cost (the sorcerer costs 2, not the first
            // registrant's 1), independent of registration order.
            var sorcererCasting = new PlannedCasting("cast-sorc", "long", 0, "source-shape",
                ShapeAbility(), "unit-sorcerer", "book", CastingTargetMode.DirectTarget,
                "unit-fighter", null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            CastingModifierResult sorcerer = share.Apply(sorcererCasting,
                PersonalOption("unit-sorcerer"));
            if (!sorcerer.IsApplied)
                throw new InvalidOperationException("the non-first capable caster was " +
                    "refused: " + sorcerer.UnavailableReason);
            IReadOnlyList<ModifierUsageDemand> sorcererDemands = share.UsageDemands(
                sorcererCasting, sorcerer.Option);
            if (sorcererDemands.Count != 1 ||
                sorcererDemands[0].UsagePoolId != "class-feature-resource|unit-sorcerer|reservoir" ||
                sorcererDemands[0].Units != 2)
                throw new InvalidOperationException("the non-first caster was costed against " +
                    "another caster's reservoir or cost.");
            // Reversed registration order resolves identically (F2:
            // reordering capability records never changes legality or cost).
            var reversed = new ShareCastingModifier(capabilities.Reverse());
            CastingModifierResult reversedApply = reversed.Apply(sorcererCasting,
                PersonalOption("unit-sorcerer"));
            IReadOnlyList<ModifierUsageDemand> reversedDemands = reversed.UsageDemands(
                sorcererCasting, PersonalOption("unit-sorcerer"));
            if (!reversedApply.IsApplied ||
                reversedDemands[0].UsagePoolId != sorcererDemands[0].UsagePoolId ||
                reversedDemands[0].Units != sorcererDemands[0].Units)
                throw new InvalidOperationException("registration order changed the outcome.");
            // An INELIGIBLE caster: an incapable caster is refused (honest
            // feature-unavailable) and never costed as free.
            var plain = new PlannedCasting("cast-plain", "long", 0, "source-shape",
                ShapeAbility(), "unit-fighter", "book", CastingTargetMode.DirectTarget,
                "unit-cleric", null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            CastingModifierResult incapable = share.Apply(plain, PersonalOption("unit-fighter"));
            if (incapable.IsApplied || !incapable.UnavailableReason.StartsWith(
                "share-feature-unavailable"))
                throw new InvalidOperationException("an incapable caster was not refused: " +
                    incapable.UnavailableReason);
            IReadOnlyList<ModifierUsageDemand> incapableDemand = share.UsageDemands(
                plain, PersonalOption("unit-fighter"));
            if (incapableDemand[0].Units != int.MaxValue)
                throw new InvalidOperationException("an unverified caster was costed as free.");

            // F2: the exact-source contract. The SAME verified caster, but
            // sources the integration never verified — each is refused.
            // A different spellbook's copy of the shape spell.
            var wrongBook = new ProviderKey("unit-wiz", "other-book", ShapeAbility(),
                "pool-unit-wiz");
            CastingModifierResult wrongSpellbook = share.Apply(casting,
                OptionOf(wrongBook, "unit-wiz"));
            if (wrongSpellbook.IsApplied || !wrongSpellbook.UnavailableReason.Contains(
                    "share-source-not-supported:spellbook-not-qualified"))
                throw new InvalidOperationException("an unverified spellbook was accepted: " +
                    wrongSpellbook.UnavailableReason);
            // An unrelated personal self-only spell in the right book.
            var otherAbility = new AbilityKey("unrelated-self-buff", null, 0,
                SourceKind.Spellbook, "book");
            CastingModifierResult wrongAbility = share.Apply(casting,
                OptionOf(new ProviderKey("unit-wiz", "book", otherAbility, "pool-unit-wiz"),
                    "unit-wiz"));
            if (wrongAbility.IsApplied || !wrongAbility.UnavailableReason.Contains(
                    "share-source-not-supported:ability-not-qualified"))
                throw new InvalidOperationException("an unverified ability was accepted: " +
                    wrongAbility.UnavailableReason);
            // A non-spellbook source kind (an item or special source).
            var itemAbility = new AbilityKey("beast-shape", null, 0, SourceKind.Item, null);
            CastingModifierResult wrongKind = share.Apply(casting,
                OptionOf(new ProviderKey("unit-wiz", "book", itemAbility, "pool-unit-wiz"),
                    "unit-wiz"));
            if (wrongKind.IsApplied || !wrongKind.UnavailableReason.Contains(
                    "share-source-not-supported:source-not-genuine-spellbook-spell"))
                throw new InvalidOperationException("a non-genuine spellbook source was " +
                    "accepted: " + wrongKind.UnavailableReason);
            // A VERIFIED VARIANT guid is supported (the selected identity,
            // not just the base).
            var variantAbility = new AbilityKey("beast-shape", "beast-shape-bear-variant", 0,
                SourceKind.Spellbook, "book");
            var variantCapability = new ShareCastingModifier(new[]
            {
                new ShareCastingModifier.ShareCapability("unit-wiz",
                    "class-feature-resource|unit-wiz|reservoir", 1, 3,
                    new[] { "beast-shape-bear-variant" }, new[] { "book" },
                    new[] { "unit-cleric" })
            });
            CastingModifierResult variant = variantCapability.Apply(
                new PlannedCasting("cast-variant", "long", 0, "source-shape",
                    variantAbility, "unit-wiz", "book", CastingTargetMode.DirectTarget,
                    "unit-cleric", null, null, null, null, ExistingEffectPolicy.SkipAlreadyActive,
                    null, CastingAuthoringState.Ready, null),
                OptionOf(new ProviderKey("unit-wiz", "book", variantAbility, "pool-unit-wiz"),
                    "unit-wiz"));
            if (!variant.IsApplied)
                throw new InvalidOperationException("a verified variant was refused: " +
                    variant.UnavailableReason);

            // F4: a verified-zero remaining balance is NOT a refusal —
            // legality and cost shape survive, the ledger decides funding.
            var drained = new ShareCastingModifier(new[]
            {
                Capability("unit-wiz", "class-feature-resource|unit-wiz|reservoir", 1, 0)
            });
            CastingModifierResult drainedResult = drained.Apply(casting,
                PersonalOption("unit-wiz"));
            if (!drainedResult.IsApplied)
                throw new InvalidOperationException("a zero reservoir refused a legal " +
                    "casting: " + drainedResult.UnavailableReason);
            IReadOnlyList<ModifierUsageDemand> drainedDemands = drained.UsageDemands(
                casting, drainedResult.Option);
            if (drainedDemands[0].UsagePoolId != "class-feature-resource|unit-wiz|reservoir" ||
                drainedDemands[0].Units != 1)
                throw new InvalidOperationException("a zero reservoir changed the cost shape.");
        }

        private static ProviderPlanningOption OptionOf(ProviderKey key, string caster)
        {
            return new ProviderPlanningOption(
                new ProviderSnapshot(key, "Beast Shape II — Bear", 2,
                    "pool-" + key.CasterUnitId, 1, null, null, 5, 600,
                    "You become a bear.", "10 minutes", string.Empty, 0,
                    string.IsNullOrEmpty(key.SpellbookGuid) ? "book" : key.SpellbookGuid),
                new[] { caster }, new string[0], 5, 600);
        }

        private static void TestShareGraphPersistence(string root)
        {
            string dir = Path.Combine(root, "share-graph-persist");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "campaign:share",
                new DisabledCastingDispatchBoundary());
            // The party: a wizard caster owning Share, two legal allies.
            var units = new[]
            {
                new UnitSnapshot("unit-wiz", "Wiz", false, null,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-cleric", "Cleric", false, null,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-fighter", "Fighter", false, null,
                    new TargetValidationSnapshot(true, true, true, true))
            };
            var providers = new[]
            {
                new ProviderSnapshot(
                    new ProviderKey("unit-wiz", "book", ShapeAbility(), "pool-unit-wiz"),
                    "Beast Shape II — Bear", 2, "pool-unit-wiz", 1, null, null, 5, 600,
                    "You become a bear.", "10 minutes", string.Empty, 0, "book")
            };
            var snapshot = new PartyProviderSnapshot(units, providers, new[]
            {
                new ResourcePoolSnapshot("pool-unit-wiz", ResourcePoolKind.SpontaneousLevel,
                    10, 10, null)
            });
            // §3: ONE registration with the wizard's VERIFIED capability
            // (the shape the host builds from verified facts); §4: the same
            // expression INSTANCE under both catalogue keys. F5: the real
            // enhancement snapshot supplies the reservoir balance the
            // ledger funds.
            var shapeExpression = new Domain.Effects.EffectLeafExpression(
                Domain.Effects.EffectKind.Buff, "buff-shape",
                Domain.Effects.EffectTarget.CurrentTarget, "shape", "shape/a");
            var effects = new Dictionary<string, Domain.Effects.EffectExpression>
            {
                { "source-shape", shapeExpression },
                { ShapeAbility().Canonical, shapeExpression }
            };
            var shareModifiers = new ICastingTargetingModifier[]
            {
                new ShareCastingModifier(new[] { Capability("unit-wiz", WizardReservoir, 1, 3,
                    units.Where(u => u.TargetValidation.Friendly).Select(u => u.UnitId)) })
            };
            var inputs = new CastingWorkspaceInputs(snapshot,
                new[] { PersonalOption("unit-wiz") },
                effects, new[] { ShareEnhancement("unit-wiz", WizardReservoir, 3) },
                shareModifiers);
            // Enable Share on the NEXT casting before choosing the target
            // (the owner's authoring order), then click the ally.
            session.SelectGraphBuff("source-shape", inputs);
            session.SelectGraphCaster("unit-wiz", inputs);
            var graph = session.BuildGraph(inputs);
            var wizNode = graph.Casters.FirstOrDefault(value => value.UnitId == "unit-wiz");
            if (wizNode == null)
                throw new InvalidOperationException("caster node missing: nodes=" +
                    string.Join(",", graph.Casters.Select(value => value.UnitId).ToArray()) +
                    " sources=" + (wizNode == null ? 0 : wizNode.Sources.Count));
            var row = wizNode.Sources.First();
            session.SelectGraphSource(row.ProviderKey, inputs);
            // E16: the Share control appears between the exact source and
            // the target click, offered through the same resolver the
            // compiler judges with; enabling it is the UI command.
            var withModifier = session.BuildGraph(inputs);
            CastingGraphModifierOption offered = withModifier.NextCastingModifiers == null
                ? null : withModifier.NextCastingModifiers.FirstOrDefault(
                    value => value.ModifierId == ShareCastingModifier.Id);
            if (offered == null || !offered.Available || offered.Selected)
                throw new InvalidOperationException("the Share control was not offered " +
                    "available after the source was chosen: " +
                    (offered == null ? "missing" : offered.UnavailableReason));
            if (!session.ToggleDraftTargetingModifier(ShareCastingModifier.Id, inputs).Applied)
                throw new InvalidOperationException("enabling Share on the next casting " +
                    "was refused.");
            var shareOn = session.BuildGraph(inputs);
            CastingGraphModifierOption onRow = shareOn.NextCastingModifiers.First(
                value => value.ModifierId == ShareCastingModifier.Id);
            if (!onRow.Selected || onRow.CostText.Length == 0)
                throw new InvalidOperationException("the armed Share control does not show " +
                    "its selection or verified cost.");
            CastingGraphEditResult added = session.AddGraphCasting("unit-cleric", inputs);
            if (!added.Applied)
                throw new InvalidOperationException("the shared authoring was refused: " +
                    (added.Edit == null ? "none" : added.Edit.Reason));
            // F5: through the compiler the shared casting is READY with the
            // exact combined cost vector (one spell slot + one reservoir
            // use) reserved atomically.
            ExplicitCastingPlan plan = session.CompilePlan(inputs);
            ResolvedCasting compiled = plan.CastingById(added.CastingId);
            if (compiled == null || compiled.Readiness != ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException("the shared casting did not compile " +
                    "Ready: " + (compiled == null ? "missing" : string.Join(";",
                        compiled.ReadinessReasons.ToArray())));
            if (!compiled.PredictedBeneficiaryUnitIds.Contains("unit-cleric"))
                throw new InvalidOperationException("the shared target was not predicted.");
            if (compiled.Cost.Count(line => line.Category == CastingCostCategory.EnhancementPool &&
                    line.PoolKey == WizardReservoir) != 1 ||
                compiled.Cost.First(line => line.PoolKey == WizardReservoir).Units != 1)
                throw new InvalidOperationException("the reservoir cost was not reserved.");
            // The persisted record carries the per-casting Share intent.
            var repository = new CastingPlanRepository(dir);
            PlannedCasting persisted = repository.Load("campaign:share").Profile
                .ToDocument().Castings.First();
            if (persisted.TargetingModifiers.Count != 1 ||
                !string.Equals(persisted.TargetingModifiers[0].ModifierId,
                    ShareCastingModifier.Id, StringComparison.Ordinal) ||
                !persisted.TargetingModifiers[0].Enabled)
                throw new InvalidOperationException("Share intent did not persist per casting.");
            if (!string.Equals(persisted.DirectTargetUnitId, "unit-cleric",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("the shared ally target did not persist.");
            // Two allies are two explicit casts (the graph shows existing
            // intent; a second ally needs a second deliberate gesture).
            CastingGraphEditResult second = session.AddGraphCasting("unit-fighter", inputs);
            if (!second.Applied)
                throw new InvalidOperationException("the second shared casting was refused.");
            if (session.Document.Castings.Count != 2)
                throw new InvalidOperationException("two allies did not mean two records.");
            // Disabling Share on the FIRST casting leaves its non-self
            // target visibly blocked and repairable — never deleted or
            // retargeted to the caster — and Undo restores the Ready plan.
            session.FocusGraphCasting(added.CastingId);
            // E17: the focused casting's own Share control disables through
            // the normal authoring boundary (autosaves, undoable).
            if (!session.ToggleFocusedTargetingModifier(ShareCastingModifier.Id, inputs).Applied)
                throw new InvalidOperationException("disabling Share was refused.");
            PlannedCasting disabled = session.Document.Castings.First(
                value => value.CastingId == added.CastingId);
            if (!string.Equals(disabled.DirectTargetUnitId, "unit-cleric",
                    StringComparison.Ordinal) || disabled.TargetingModifiers.Count != 0)
                throw new InvalidOperationException("disabling Share changed the target or " +
                    "left the selection.");
            ExplicitCastingPlan blockedPlan = session.CompilePlan(inputs);
            ResolvedCasting blocked = blockedPlan.CastingById(added.CastingId);
            if (blocked == null || blocked.Readiness != ResolvedCastingReadiness.Blocked ||
                !blocked.ReadinessReasons.Any(reason => reason.StartsWith("target-unreachable:unit-cleric",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("the unshared ally casting is not visibly " +
                    "blocked for the honest reason.");
            if (!session.Undo())
                throw new InvalidOperationException("undo of the Share-off edit was refused.");
            ExplicitCastingPlan restoredPlan = session.CompilePlan(inputs);
            ResolvedCasting restored = restoredPlan.CastingById(added.CastingId);
            if (restored == null || restored.Readiness != ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException("undo did not restore the Ready shared " +
                    "casting: " + (restored == null ? "missing" : string.Join(";",
                        restored.ReadinessReasons.ToArray())));
            var reload = new CastingWorkspaceSession(dir, "campaign:share");
            PlannedCasting reloaded = reload.Document.Castings.First(
                value => value.CastingId == added.CastingId);
            if (!string.Equals(reloaded.DirectTargetUnitId, "unit-cleric",
                    StringComparison.Ordinal) || reloaded.TargetingModifiers.Count != 1)
                throw new InvalidOperationException("reload lost the blocked ally intent or " +
                    "the restored Share selection.");
        }

        // F5: the integrated compile/budget contract. Real enhancement and
        // resource snapshots feed the ledger; the modifier's demand is part
        // of the atomic vector; an unfundable vector reserves NOTHING; a
        // demanded pool with no verified balance (missing or unknown) can
        // never fund a cast; Share + Powerful Change on ONE shared pool
        // validate as combined demand.
        private static void TestShareCompileBudget(string root)
        {
            var units = new[]
            {
                new UnitSnapshot("unit-wiz", "Wiz", false, null,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-cleric", "Cleric", false, null,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-fighter", "Fighter", false, null,
                    new TargetValidationSnapshot(true, true, true, true))
            };
            var shapeExpression = new Domain.Effects.EffectLeafExpression(
                Domain.Effects.EffectKind.Buff, "buff-shape",
                Domain.Effects.EffectTarget.CurrentTarget, "shape", "shape/a");
            var effects = new Dictionary<string, Domain.Effects.EffectExpression>
            {
                { "source-shape", shapeExpression },
                { ShapeAbility().Canonical, shapeExpression }
            };
            var party = new PartyProviderSnapshot(units, new[]
            {
                new ProviderSnapshot(
                    new ProviderKey("unit-wiz", "book", ShapeAbility(), "pool-unit-wiz"),
                    "Beast Shape II — Bear", 2, "pool-unit-wiz", 1, null, null, 5, 600,
                    "You become a bear.", "10 minutes", string.Empty, 0, "book")
            }, new[]
            {
                new ResourcePoolSnapshot("pool-unit-wiz", ResourcePoolKind.SpontaneousLevel,
                    10, 10, null)
            });
            // Two allies are TWO explicit invocations with a COMBINED
            // reservoir demand, both funded atomically.
            {
                var share = new ShareCastingModifier(new[]
                    { Capability("unit-wiz", WizardReservoir, 1, 2) });
                var document = new CastingPlanDocument("campaign:budget",
                    new[] { new RoutineDefinition("long", "Long") },
                    new[]
                    {
                        SharedCasting("share-1", "unit-wiz", "unit-cleric", 0),
                        SharedCasting("share-2", "unit-wiz", "unit-fighter", 1)
                    }, null);
                ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document,
                    party, new[] { PersonalOption("unit-wiz") }, effects,
                    new[] { ShareEnhancement("unit-wiz", WizardReservoir, 2) },
                    null, new[] { share });
                foreach (string id in new[] { "share-1", "share-2" })
                {
                    ResolvedCasting casting = plan.CastingById(id);
                    if (casting == null || casting.Readiness != ResolvedCastingReadiness.Ready)
                        throw new InvalidOperationException(id + " did not compile Ready: " +
                            (casting == null ? "missing" : string.Join(";",
                                casting.ReadinessReasons.ToArray())));
                    if (casting.Cost.Count(line => line.Category == CastingCostCategory.NativePool) != 1)
                        throw new InvalidOperationException(id + " lost its native slot cost.");
                    if (casting.Cost.First(line => line.PoolKey == WizardReservoir).Units != 1)
                        throw new InvalidOperationException(id + " lost its reservoir cost.");
                }
                CastingBudgetLine reservoir = plan.BudgetLines.First(
                    line => line.PoolKey == WizardReservoir);
                if (reservoir.AllocatedUsage != 2 || reservoir.RequestedUsage != 2 ||
                    reservoir.ForecastRemaining != 0)
                    throw new InvalidOperationException("the shared reservoir was not " +
                        "accounted as combined demand: " + reservoir.AllocatedUsage);
            }
            // A reservoir that funds only the FIRST casting: the second is
            // blocked for the real shortage, reserves NOTHING (no partial
            // reservation, no native slot), and the first keeps its exact
            // allocation.
            {
                var share = new ShareCastingModifier(new[]
                    { Capability("unit-wiz", WizardReservoir, 1, 1) });
                var document = new CastingPlanDocument("campaign:budget",
                    new[] { new RoutineDefinition("long", "Long") },
                    new[]
                    {
                        SharedCasting("share-1", "unit-wiz", "unit-cleric", 0),
                        SharedCasting("share-2", "unit-wiz", "unit-fighter", 1)
                    }, null);
                ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document,
                    party, new[] { PersonalOption("unit-wiz") }, effects,
                    new[] { ShareEnhancement("unit-wiz", WizardReservoir, 1) },
                    null, new[] { share });
                ResolvedCasting first = plan.CastingById("share-1");
                ResolvedCasting second = plan.CastingById("share-2");
                if (first == null || first.Readiness != ResolvedCastingReadiness.Ready)
                    throw new InvalidOperationException("the fundable shared casting was " +
                        "blocked: " + (first == null ? "missing" : string.Join(";",
                            first.ReadinessReasons.ToArray())));
                if (second == null || second.Readiness != ResolvedCastingReadiness.Blocked ||
                    !second.ReadinessReasons.Any(reason => reason.StartsWith(
                        "enhancement-pool-exhausted:" + WizardReservoir,
                        StringComparison.Ordinal)))
                    throw new InvalidOperationException("the unfundable shared casting was " +
                        "not blocked for the real shortage.");
                if (second.Cost.Count != 0)
                    throw new InvalidOperationException("the blocked casting reserved a " +
                        "partial cost vector.");
                CastingBudgetLine native = plan.BudgetLines.First(
                    line => line.PoolKey == "pool-unit-wiz");
                CastingBudgetLine reservoir = plan.BudgetLines.First(
                    line => line.PoolKey == WizardReservoir);
                if (native.AllocatedUsage != 1 || reservoir.AllocatedUsage != 1)
                    throw new InvalidOperationException("a rejected vector still reserved " +
                        "native slots or reservoir units.");
            }
            // A demanded pool whose verified balance is UNKNOWN (null):
            // unfundable (fail-closed), never silently free.
            {
                var share = new ShareCastingModifier(new[]
                    { Capability("unit-wiz", WizardReservoir, 1, null) });
                var document = new CastingPlanDocument("campaign:budget",
                    new[] { new RoutineDefinition("long", "Long") },
                    new[] { SharedCasting("share-1", "unit-wiz", "unit-cleric") }, null);
                ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document,
                    party, new[] { PersonalOption("unit-wiz") }, effects,
                    new[] { ShareEnhancement("unit-wiz", WizardReservoir, null) },
                    null, new[] { share });
                ResolvedCasting casting = plan.CastingById("share-1");
                if (casting == null || casting.Readiness != ResolvedCastingReadiness.Blocked ||
                    !casting.ReadinessReasons.Any(reason => reason.StartsWith(
                        "enhancement-balance-unknown:" + WizardReservoir,
                        StringComparison.Ordinal)))
                    throw new InvalidOperationException("an unknown balance funded a cast: " +
                        (casting == null ? "missing" : string.Join(";",
                            casting.ReadinessReasons.ToArray())));
            }
            {
                // No enhancement snapshot AT ALL: the demanded pool is
                // unknown to the ledger and must refuse.
                var share = new ShareCastingModifier(new[]
                    { Capability("unit-wiz", WizardReservoir, 1, 3) });
                var document = new CastingPlanDocument("campaign:budget",
                    new[] { new RoutineDefinition("long", "Long") },
                    new[] { SharedCasting("share-1", "unit-wiz", "unit-cleric") }, null);
                ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document,
                    party, new[] { PersonalOption("unit-wiz") }, effects,
                    new CastEnhancementSnapshot[0], null, new[] { share });
                ResolvedCasting casting = plan.CastingById("share-1");
                if (casting == null || casting.Readiness != ResolvedCastingReadiness.Blocked ||
                    !casting.ReadinessReasons.Any(reason => reason.StartsWith(
                        "enhancement-pool-unknown:" + WizardReservoir,
                        StringComparison.Ordinal)))
                    throw new InvalidOperationException("a missing demanded pool funded a " +
                        "cast: " + (casting == null ? "missing" : string.Join(";",
                            casting.ReadinessReasons.ToArray())));
            }
            // Share + Powerful Change spend ONE shared reservoir: combined
            // demand funds with 2 left, blocks with 1, and the blocked
            // vector reserves nothing (the ordinary fundable casting after
            // it still gets its full allocation).
            {
                // Both snapshots report ONE shared reservoir, so they agree
                // on its balance (the ledger takes the conservative minimum
                // of the reports for one pool).
                Func<int?, CastEnhancementSnapshot> powerfulChangeOf = remainingUses =>
                    new CastEnhancementSnapshot(
                    "powerful-change|unit-wiz", "unit-wiz", "powerful-change-guid",
                    "Powerful Change", "Spend the reservoir for a stronger form.",
                    CastEnhancementCategory.ClassFeature, 0, 0, remainingUses,
                    new[] { "beast-shape" }, "Powerful Change", new[] { "book" },
                    WizardReservoir, false, "powerful-change", 1, false,
                    "powerful-change", "Arcane Reservoir", null);
                var share = new ShareCastingModifier(new[]
                    { Capability("unit-wiz", WizardReservoir, 1, 2) });
                var combined = new PlannedCasting("share-pc", "long", 0, "source-shape",
                    ShapeAbility(), "unit-wiz", "book", CastingTargetMode.DirectTarget,
                    "unit-cleric", null, null,
                    new[] { new TargetingModifierSelection(ShareCastingModifier.Id, true, null) },
                    new[] { new AuthoredEnhancementSelection("powerful-change|unit-wiz", true, null) },
                    ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
                var ordinary = new PlannedCasting("plain-1", "long", 1, "source-shape",
                    ShapeAbility(), "unit-wiz", "book", CastingTargetMode.DirectTarget,
                    "unit-wiz", null, null, null, null,
                    ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
                var document = new CastingPlanDocument("campaign:budget",
                    new[] { new RoutineDefinition("long", "Long") },
                    new[] { combined, ordinary }, null);
                ExplicitCastingPlan funded = new ExplicitCastingCompiler().Compile(document,
                    party, new[] { PersonalOption("unit-wiz") }, effects,
                    new[] { ShareEnhancement("unit-wiz", WizardReservoir, 2), powerfulChangeOf(2) },
                    null, new[] { share });
                ResolvedCasting fundedCombined = funded.CastingById("share-pc");
                if (fundedCombined == null || fundedCombined.Readiness != ResolvedCastingReadiness.Ready)
                    throw new InvalidOperationException("the combined shared-pool casting did " +
                        "not fund: " + (fundedCombined == null ? "missing" : string.Join(";",
                            fundedCombined.ReadinessReasons.ToArray())));
                if (fundedCombined.Cost.First(line => line.PoolKey == WizardReservoir).Units != 2)
                    throw new InvalidOperationException("the combined vector did not carry the " +
                        "shared pool's full demand.");
                // Same plan with only ONE use left: the combined casting
                // blocks atomically; the ordinary casting still funds.
                var shortDocument = new CastingPlanDocument("campaign:budget",
                    new[] { new RoutineDefinition("long", "Long") },
                    new[] { combined, ordinary }, null);
                ExplicitCastingPlan shortPlan = new ExplicitCastingCompiler().Compile(
                    shortDocument, party, new[] { PersonalOption("unit-wiz") }, effects,
                    new[] { ShareEnhancement("unit-wiz", WizardReservoir, 1), powerfulChangeOf(1) },
                    null, new[] { share });
                ResolvedCasting blockedCombined = shortPlan.CastingById("share-pc");
                ResolvedCasting ordinaryResolved = shortPlan.CastingById("plain-1");
                if (blockedCombined == null || blockedCombined.Readiness != ResolvedCastingReadiness.Blocked ||
                    !blockedCombined.ReadinessReasons.Any(reason => reason.StartsWith(
                        "enhancement-pool-exhausted:" + WizardReservoir,
                        StringComparison.Ordinal)))
                    throw new InvalidOperationException("the combined shortage did not block " +
                        "for the shared pool's real deficit.");
                if (blockedCombined.Cost.Count != 0)
                    throw new InvalidOperationException("the blocked combined casting " +
                        "reserved a partial vector.");
                if (ordinaryResolved == null || ordinaryResolved.Readiness != ResolvedCastingReadiness.Ready)
                    throw new InvalidOperationException("the independently fundable ordinary " +
                        "casting was harmed by the earlier shortage.");
                CastingBudgetLine nativeAfterShortage = shortPlan.BudgetLines.First(
                    line => line.PoolKey == "pool-unit-wiz");
                if (nativeAfterShortage.AllocatedUsage != 1)
                    throw new InvalidOperationException("the blocked combined vector still " +
                        "reserved its native slot.");
            }
        }

        // F4: legality versus affordability at a verified-zero reservoir.
        // A sufficiently active effect is AlreadySatisfied with zero
        // reservation; a missing/insufficient effect is a resource block;
        // Overwrite (always recast) is a resource block even with the
        // active effect; and a routine containing the skipped shared
        // casting leaves an independently fundable ordinary casting
        // untouched.
        private static void TestShareZeroReservoir(string root)
        {
            var units = new[]
            {
                new UnitSnapshot("unit-wiz", "Wiz", false, null,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-cleric", "Cleric", false, null,
                    new TargetValidationSnapshot(true, true, true, true))
            };
            var shapeExpression = new Domain.Effects.EffectLeafExpression(
                Domain.Effects.EffectKind.Buff, "buff-shape",
                Domain.Effects.EffectTarget.CurrentTarget, "shape", "shape/a");
            var effects = new Dictionary<string, Domain.Effects.EffectExpression>
            {
                { "source-shape", shapeExpression },
                { ShapeAbility().Canonical, shapeExpression }
            };
            var snapshot = new PartyProviderSnapshot(units, new[]
            {
                new ProviderSnapshot(
                    new ProviderKey("unit-wiz", "book", ShapeAbility(), "pool-unit-wiz"),
                    "Beast Shape II — Bear", 2, "pool-unit-wiz", 1, null, null, 5, 600,
                    "You become a bear.", "10 minutes", string.Empty, 0, "book")
            }, new[]
            {
                new ResourcePoolSnapshot("pool-unit-wiz", ResourcePoolKind.SpontaneousLevel,
                    10, 10, null)
            });
            var share = new ShareCastingModifier(new[]
                { Capability("unit-wiz", WizardReservoir, 1, 0, new[] { "unit-cleric" }) });
            var enhancements = new[] { ShareEnhancement("unit-wiz", WizardReservoir, 0) };
            // A sufficient live instance of the exact effect on the ally.
            var active = ActiveEffectSnapshot.FromInstances(
                new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    {
                        "unit-cleric",
                        new[] { new ActiveEffectInstance(Domain.Effects.EffectKind.Buff,
                            "buff-shape", 5000, 10, 0) }
                    }
                });
            Func<PlannedCasting, ActiveEffectSnapshot, ExplicitCastingPlan> compile =
                (casting, live) =>
                {
                    var document = new CastingPlanDocument("campaign:zero",
                        new[] { new RoutineDefinition("long", "Long") },
                        new[] { casting }, null);
                    return new ExplicitCastingCompiler().Compile(document, snapshot,
                        new[] { PersonalOption("unit-wiz") }, effects, enhancements,
                        null, new[] { share }, false, live);
                };
            // Sufficient active effect + zero reservoir => AlreadySatisfied,
            // zero reservation/submission cost.
            ResolvedCasting satisfied = compile(
                SharedCasting("share-skip", "unit-wiz", "unit-cleric"), active)
                .CastingById("share-skip");
            if (satisfied == null || satisfied.Readiness != ResolvedCastingReadiness.AlreadySatisfied)
                throw new InvalidOperationException("a satisfied shared casting was blocked at " +
                    "a zero reservoir: " + (satisfied == null ? "missing" : string.Join(";",
                        satisfied.ReadinessReasons.ToArray())));
            if (satisfied.Cost.Count != 0)
                throw new InvalidOperationException("the skipped casting reserved a cost.");
            // Missing effect + zero reservoir => resource block.
            ResolvedCasting blocked = compile(
                SharedCasting("share-need", "unit-wiz", "unit-cleric"), null)
                .CastingById("share-need");
            if (blocked == null || blocked.Readiness != ResolvedCastingReadiness.Blocked ||
                !blocked.ReadinessReasons.Any(reason => reason.StartsWith(
                    "enhancement-pool-exhausted:" + WizardReservoir,
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("a needed shared casting at a zero " +
                    "reservoir was not blocked as a resource shortage: " +
                    (blocked == null ? "missing" : string.Join(";",
                        blocked.ReadinessReasons.ToArray())));
            // Overwrite (always recast) + zero reservoir => resource block,
            // never AlreadySatisfied.
            ResolvedCasting recast = compile(
                SharedCasting("share-recast", "unit-wiz", "unit-cleric",
                    0, ExistingEffectPolicy.Overwrite), active)
                .CastingById("share-recast");
            if (recast == null || recast.Readiness != ResolvedCastingReadiness.Blocked ||
                !recast.ReadinessReasons.Any(reason => reason.StartsWith(
                    "enhancement-pool-exhausted:" + WizardReservoir,
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("an always-recast shared casting was " +
                    "satisfied by an active effect at a zero reservoir: " +
                    (recast == null ? "missing" : string.Join(";",
                        recast.ReadinessReasons.ToArray())));
            // A routine containing the skipped shared casting and an
            // independently fundable ordinary casting: the ordinary one
            // keeps its full funding.
            var ordinary = new PlannedCasting("plain-1", "long", 1, "source-shape",
                ShapeAbility(), "unit-wiz", "book", CastingTargetMode.DirectTarget,
                "unit-wiz", null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            var mixed = new CastingPlanDocument("campaign:zero",
                new[] { new RoutineDefinition("long", "Long") },
                new[]
                {
                    SharedCasting("share-skip", "unit-wiz", "unit-cleric"),
                    ordinary
                }, null);
            ExplicitCastingPlan mixedPlan = new ExplicitCastingCompiler().Compile(mixed,
                snapshot, new[] { PersonalOption("unit-wiz") }, effects, enhancements,
                null, new[] { share }, false, active);
            ResolvedCasting mixedSkip = mixedPlan.CastingById("share-skip");
            ResolvedCasting mixedPlain = mixedPlan.CastingById("plain-1");
            if (mixedSkip == null || mixedSkip.Readiness != ResolvedCastingReadiness.AlreadySatisfied)
                throw new InvalidOperationException("the mixed routine lost the skip.");
            if (mixedPlain == null || mixedPlain.Readiness != ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException("the ordinary fundable casting was harmed " +
                    "by the skipped shared casting.");
            CastingBudgetLine native = mixedPlan.BudgetLines.FirstOrDefault(
                line => line.PoolKey == "pool-unit-wiz");
            if (native == null || native.AllocatedUsage != 1)
                throw new InvalidOperationException("the ordinary casting did not reserve its " +
                    "native slot.");
        }

        // R579-1: the COMPLETE executable enhancement contract - including
        // the modifier-backed Share enhancement - resolves BEFORE the
        // strategy freezes, through the existing execution policy. Share
        // ALONE (no ordinary enhancements) must select the verified
        // provider-direct route; a native-command requirement routes
        // accordingly; Share + Powerful Change resolve once, together,
        // without double-charging the shared reservoir; a modifier with no
        // execution identity keeps the base strategy.
        private static void TestShareStrategyResolution()
        {
            var units = new[]
            {
                new UnitSnapshot("unit-wiz", "Wiz", false, null,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-cleric", "Cleric", false, null,
                    new TargetValidationSnapshot(true, true, true, true))
            };
            var shapeExpression = new Domain.Effects.EffectLeafExpression(
                Domain.Effects.EffectKind.Buff, "buff-shape",
                Domain.Effects.EffectTarget.CurrentTarget, "shape", "shape/a");
            var effects = new Dictionary<string, Domain.Effects.EffectExpression>
            {
                { "source-shape", shapeExpression },
                { ShapeAbility().Canonical, shapeExpression }
            };
            var snapshot = new PartyProviderSnapshot(units, new[]
            {
                ShapeProvider("unit-wiz")
            }, new[]
            {
                new ResourcePoolSnapshot("pool-unit-wiz", ResourcePoolKind.SpontaneousLevel,
                    10, 10, null)
            });
            string shareId = ShareEnhancement("unit-wiz", WizardReservoir, 3).EnhancementId;
            Func<string, bool, string, ShareCastingModifier.ShareCapability> capability =
                (providerId, nativeCommand, executionId) =>
                    new ShareCastingModifier.ShareCapability("unit-wiz", WizardReservoir,
                        1, 3, new[] { "beast-shape" }, new[] { "book" },
                        new[] { "unit-cleric" }, executionId);
            Func<string, bool, ShareCastingModifier> shareWith =
                (providerId, nativeCommand) => new ShareCastingModifier(new[]
                {
                    capability(providerId, nativeCommand, shareId)
                });
            Func<CastEnhancementSnapshot[], ShareCastingModifier, ExplicitCastingPlan> compile =
                (enhancementList, share) => new ExplicitCastingCompiler().Compile(
                    new CastingPlanDocument("campaign:strategy",
                        new[] { new RoutineDefinition("long", "Long") },
                        new[] { SharedCasting("share-1", "unit-wiz", "unit-cleric") }, null),
                    snapshot, new[] { PersonalOption("unit-wiz") }, effects, enhancementList,
                    null, new[] { share });
            // Share ALONE with a verified direct provider: the provider
            // transaction route is selected (the base direct-rule spell
            // would otherwise bypass it), from the compiler through the
            // projected executor step, with the reservoir charged ONCE.
            {
                var share = shareWith("brown-fur-direct-cast-v1", false);
                ExplicitCastingPlan plan = compile(
                    new[] { ShareEnhancement("unit-wiz", WizardReservoir, 3,
                        "brown-fur-direct-cast-v1") }, share);
                ResolvedCasting resolved = plan.CastingById("share-1");
                if (resolved == null || resolved.Readiness != ResolvedCastingReadiness.Ready ||
                    resolved.ExecutionStrategy != CastExecutionStrategy.ProviderDirectRuleCast)
                    throw new InvalidOperationException("Share alone did not select the " +
                        "provider-direct route: " + (resolved == null ? "missing" :
                        resolved.ExecutionStrategy + ";" + string.Join(";",
                            resolved.ReadinessReasons.ToArray())));
                CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan,
                    CastingApplyMode.Ordinary, "long");
                ExplicitStepConversion projection = ExplicitCastingStepConverter.Convert(
                    plan, decision, new[] { PersonalOption("unit-wiz") }, effects,
                    ExplicitProjectionScope.Standard,
                    CastingWorkspaceSession.TargetingModifierEnhancementMap(
                        new CastingWorkspaceInputs(snapshot,
                            new[] { PersonalOption("unit-wiz") }, effects,
                            new[] { ShareEnhancement("unit-wiz", WizardReservoir, 3,
                                "brown-fur-direct-cast-v1") },
                            new ICastingTargetingModifier[] { share })));
                if (!projection.Converted)
                    throw new InvalidOperationException("the provider-direct Share step " +
                        "did not project: " + projection.Refusal);
                if (projection.Plan.Steps[0].ExecutionStrategy !=
                        CastExecutionStrategy.ProviderDirectRuleCast ||
                    !projection.Plan.Steps[0].EnhancementIds.Contains(shareId))
                    throw new InvalidOperationException("the projected step lost the " +
                        "route or the verified toggle identity.");
                int reservoir = projection.Plan.Steps[0]
                    .EnhancementUsageByPool[WizardReservoir];
                if (reservoir != 1)
                    throw new InvalidOperationException("the Share reservoir was charged " +
                        reservoir + " times.");
            }
            // A verified native-command requirement routes accordingly.
            {
                ExplicitCastingPlan plan = compile(
                    new[] { ShareEnhancement("unit-wiz", WizardReservoir, 3, null, true) },
                    shareWith(null, true));
                ResolvedCasting resolved = plan.CastingById("share-1");
                if (resolved == null || resolved.ExecutionStrategy !=
                        CastExecutionStrategy.NativeCommandRequired)
                    throw new InvalidOperationException("a native-command Share did not " +
                        "require the native route: " + (resolved == null ? "missing" :
                        resolved.ExecutionStrategy.ToString()));
            }
            // Share + Powerful Change: one shared reservoir, combined demand
            // exactly once, one provider route.
            {
                var powerfulChange = new CastEnhancementSnapshot(
                    "powerful-change|unit-wiz", "unit-wiz", "powerful-change-guid",
                    "Powerful Change", "Spend the reservoir for a stronger form.",
                    CastEnhancementCategory.ClassFeature, 0, 0, 2,
                    new[] { "beast-shape" }, "Powerful Change", new[] { "book" },
                    WizardReservoir, false, "powerful-change", 1, false,
                    "powerful-change", "Arcane Reservoir", "brown-fur-direct-cast-v1");
                var share = shareWith("brown-fur-direct-cast-v1", false);
                var combined = new PlannedCasting("share-pc", "long", 0, "source-shape",
                    ShapeAbility(), "unit-wiz", "book", CastingTargetMode.DirectTarget,
                    "unit-cleric", null, null,
                    new[] { new TargetingModifierSelection(ShareCastingModifier.Id, true, null) },
                    new[] { new AuthoredEnhancementSelection("powerful-change|unit-wiz", true, null) },
                    ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
                ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(
                    new CastingPlanDocument("campaign:strategy",
                        new[] { new RoutineDefinition("long", "Long") },
                        new[] { combined }, null),
                    snapshot, new[] { PersonalOption("unit-wiz") }, effects,
                    new[]
                    {
                        ShareEnhancement("unit-wiz", WizardReservoir, 2,
                            "brown-fur-direct-cast-v1"),
                        powerfulChange
                    },
                    null, new[] { share });
                ResolvedCasting resolved = plan.CastingById("share-pc");
                if (resolved == null || resolved.Readiness != ResolvedCastingReadiness.Ready ||
                    resolved.ExecutionStrategy != CastExecutionStrategy.ProviderDirectRuleCast)
                    throw new InvalidOperationException("the combined features did not " +
                        "resolve one provider route: " + (resolved == null ? "missing" :
                        resolved.ExecutionStrategy + ";" + string.Join(";",
                            resolved.ReadinessReasons.ToArray())));
                if (resolved.Cost.First(line => line.PoolKey == WizardReservoir).Units != 2)
                    throw new InvalidOperationException("the shared reservoir was not " +
                        "charged exactly the combined demand.");
            }
            // No execution identity declared (synthetic capability): the base
            // strategy is preserved and no toggle id is armed.
            {
                ExplicitCastingPlan plan = compile(
                    new[] { ShareEnhancement("unit-wiz", WizardReservoir, 3) },
                    new ShareCastingModifier(new[]
                    {
                        new ShareCastingModifier.ShareCapability("unit-wiz", WizardReservoir,
                            1, 3, new[] { "beast-shape" }, new[] { "book" },
                            new[] { "unit-cleric" })
                    }));
                ResolvedCasting resolved = plan.CastingById("share-1");
                if (resolved == null || resolved.Readiness != ResolvedCastingReadiness.Ready)
                    throw new InvalidOperationException("a modifier without an execution " +
                        "identity did not stay Ready.");
                if (resolved.ExecutionStrategy != CastExecutionStrategy.DirectRuleCast ||
                    resolved.AppliedEnhancementIds.Contains(shareId))
                    throw new InvalidOperationException("a modifier without an execution " +
                        "identity changed the route or armed the toggle.");
            }
        }

        // R579-2: cleanup is OBSERVABLE. A lease that cannot verify its
        // native restoration reports the failure through the preparation,
        // the run report carries it as unsettled state (never an ordinary
        // success), and NO later cast in the same run inherits the uncertain
        // feature state - in BOTH executors, using true runtime-adapter
        // stubs (no mocked planner services).
        private sealed class FailingCleanupLease : IDisposable,
            IEnhancementCleanupOutcome
        {
            public string CleanupFailure
            { get { return "restore-mismatch:share-toggle;expected=True;actual=False"; } }
            public void Dispose() { }
        }

        private sealed class CleanupStubWorld : IInstantCastRuntimeAdapter,
            ICastRuntimeAdapter, ICastEnhancementRuntimeAdapter
        {
            internal bool FailCleanup;
            internal bool RejectPreparation;
            internal readonly System.Collections.Generic.List<string> AnimatedStarts =
                new System.Collections.Generic.List<string>();
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step)
            { return CastRuntimeValidation.Pass(); }
            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                AnimatedStarts.Add(step.AssignmentId);
                return new SucceedingAnimatedOperation();
            }
            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            {
                if (RejectPreparation)
                    return CastEnhancementPreparation.Fail("activation-refused",
                        FailCleanup ? "restore-mismatch:share-toggle;expected=True;actual=False"
                            : string.Empty);
                return FailCleanup
                    ? CastEnhancementPreparation.Pass(new FailingCleanupLease())
                    : CastEnhancementPreparation.Pass(null);
            }
            public InstantCastResult Fire(CastStep step)
            { return new InstantCastResult(true, true, true, false, "simulated"); }
            public bool EffectsObserved(CastStep step) { return true; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("simulated-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("simulated-clean"); }
        }

        private sealed class SucceedingAnimatedOperation : IAnimatedCastOperation
        {
            public bool IsCompleted { get { return true; } }
            public bool IsStarted { get { return true; } }
            public bool TimedOut { get { return false; } }
            public bool Succeeded { get { return true; } }
            public bool EffectsObserved { get { return true; } }
            public bool ResourceSpent { get { return false; } }
            public string ResourceCountViolation { get { return null; } }
            public bool HasResidualDeliveryState { get { return false; } }
            public string Detail { get { return "simulated"; } }
            public void Dispose() { }
        }

        // R736-2: a runtime whose operation NEVER completes, so the
        // executor is genuinely suspended in flight when disposed.
        private sealed class PendingAnimatedWorld : ICastRuntimeAdapter,
            ICastEnhancementRuntimeAdapter
        {
            private readonly CleanupStubWorld _inner;
            internal PendingAnimatedWorld(CleanupStubWorld inner) { _inner = inner; }
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step)
            { return CastRuntimeValidation.Pass(); }
            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                _inner.AnimatedStarts.Add(step.AssignmentId);
                return new PendingAnimatedOperation();
            }
            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            { return _inner.PrepareEnhancements(step); }
        }

        private sealed class PendingAnimatedOperation : IAnimatedCastOperation
        {
            public bool IsCompleted { get { return false; } }
            public bool IsStarted { get { return true; } }
            public bool TimedOut { get { return false; } }
            public bool Succeeded { get { return false; } }
            public bool EffectsObserved { get { return false; } }
            public bool ResourceSpent { get { return false; } }
            public string ResourceCountViolation { get { return null; } }
            public bool HasResidualDeliveryState { get { return false; } }
            public string Detail { get { return "pending"; } }
            public void Dispose() { }
        }

        private static CastStep CleanupStep(string id)
        {
            return new CastStep("source-shape", id,
                ShapeProvider("unit-wiz").Key, null,
                new[] { "unit-cleric" }, new[] { "unit-cleric" },
                new ResourceReservation("pool-unit-wiz", 1, new string[0], false),
                null,
                new Domain.Effects.EffectLeafExpression(Domain.Effects.EffectKind.Buff,
                    "buff-shape", Domain.Effects.EffectTarget.CurrentTarget, "shape", "shape/a"),
                false, CastExecutionStrategy.DirectRuleCast, string.Empty,
                new[] { "share-transmutation|unit-wiz|" + ShareActivatableGuid });
        }

        private static void TestEnhancementCleanupObservable()
        {
            var failing = new CleanupStubWorld { FailCleanup = true };
            var plan = new CastPlan(new[] { CleanupStep("share-1"), CleanupStep("share-2") },
                new TargetPlanOutcome[0], new string[0]);
            var animatedReport = new ExecutionReport(plan);
            var animated = new AnimatedCastExecutor(failing, false);
            var iterator = animated.Execute(plan, animatedReport);
            while (iterator.MoveNext()) { }
            if (!animatedReport.Records.Any(record =>
                    record.Detail.Contains("enhancement-cleanup-failed:") &&
                    record.Detail.Contains("restore-mismatch")))
                throw new InvalidOperationException("the Animated run did not surface the " +
                    "cleanup failure.");
            if (!animatedReport.Records.Any(record =>
                    record.Status == CastExecutionStatus.ResidualStateUnsettled &&
                    record.AssignmentId == "share-1"))
                throw new InvalidOperationException("the failed cleanup was not terminal " +
                    "unsettled state for its own casting.");
            if (!animatedReport.Records.Any(record =>
                    record.AssignmentId == "share-2" &&
                    record.Detail.Contains("prior-animated-transaction-unsettled")))
                throw new InvalidOperationException("a later cast inherited the uncertain " +
                    "enhancement state.");
            var instantReport = new ExecutionReport(plan);
            var instant = new InstantCastExecutor(failing, false, 8);
            var instantIterator = instant.Execute(plan, instantReport);
            while (instantIterator.MoveNext()) { }
            if (!instantReport.Records.Any(record =>
                    record.Detail.Contains("enhancement-cleanup-failed:")))
                throw new InvalidOperationException("the Instant run did not surface the " +
                    "cleanup failure.");
            if (!instantReport.Records.Any(record =>
                    record.AssignmentId == "share-2" &&
                    record.Status == CastExecutionStatus.FailedValidation))
                throw new InvalidOperationException("the Instant run let a later cast " +
                    "proceed after uncertain enhancement state.");
            // R736-2: cancellation. The operation stays in flight; the
            // iterator is DISPOSED while suspended (never drained). The
            // enhancement cleanup failure must survive into the report
            // beside the abandonment record - exactly one unsettled-state
            // record with the original detail - and the second casting
            // never starts. A clean lease cancels without inventing a
            // failure.
            {
                var cancelled = new CleanupStubWorld { FailCleanup = true };
                var cancelPlan = new CastPlan(
                    new[] { CleanupStep("share-1"), CleanupStep("share-2") },
                    new TargetPlanOutcome[0], new string[0]);
                var cancelReport = new ExecutionReport(cancelPlan);
                var pendingCoroutine = new AnimatedCastExecutor(
                    new PendingAnimatedWorld(cancelled), false)
                    .Execute(cancelPlan, cancelReport);
                if (!pendingCoroutine.MoveNext())
                    throw new InvalidOperationException("the executor never yielded " +
                        "with the operation in flight.");
                ((System.IDisposable)pendingCoroutine).Dispose();
                var unsettled = cancelReport.Records.Where(record =>
                    record.Status == CastExecutionStatus.ResidualStateUnsettled &&
                    record.Detail.Contains("enhancement-cleanup-failed")).ToList();
                if (unsettled.Count != 1 ||
                    !unsettled[0].Detail.Contains("restore-mismatch:share-toggle"))
                    throw new InvalidOperationException("the cancellation path lost or " +
                        "duplicated the enhancement cleanup outcome: " + unsettled.Count);
                if (!cancelReport.Records.Any(record =>
                        record.Detail.Contains("animated-operation-abandoned-in-flight")))
                    throw new InvalidOperationException("the interruption record was " +
                        "lost.");
                if (cancelReport.Records.Any(record =>
                        record.AssignmentId == "share-2" && record.Status ==
                            CastExecutionStatus.CastStarted))
                    throw new InvalidOperationException("a later casting started after " +
                        "cancellation.");
                var cleanCancel = new CleanupStubWorld();
                var cleanCancelReport = new ExecutionReport(cancelPlan);
                var cleanPending = new AnimatedCastExecutor(
                    new PendingAnimatedWorld(cleanCancel), false)
                    .Execute(cancelPlan, cleanCancelReport);
                cleanPending.MoveNext();
                ((System.IDisposable)cleanPending).Dispose();
                if (cleanCancelReport.Records.Any(record =>
                        record.Detail.Contains("enhancement-cleanup-failed")))
                    throw new InvalidOperationException("a clean cancellation invented " +
                        "a cleanup failure.");
            }
            // R736/C853-2A: a REJECTED preparation keeps both facts - the
            // setup refusal AND its own cleanup outcome - and neither the
            // rejected cast nor any later cast starts.
            {
                var rejected = new CleanupStubWorld
                { RejectPreparation = true, FailCleanup = true };
                var rejectPlan = new CastPlan(
                    new[] { CleanupStep("share-1"), CleanupStep("share-2") },
                    new TargetPlanOutcome[0], new string[0]);
                var rejectReport = new ExecutionReport(rejectPlan);
                var rejectIterator = new AnimatedCastExecutor(rejected, false)
                    .Execute(rejectPlan, rejectReport);
                while (rejectIterator.MoveNext()) { }
                if (rejected.AnimatedStarts.Count != 0)
                    throw new InvalidOperationException("a rejected preparation " +
                        "attempted the cast.");
                if (!rejectReport.Records.Any(record =>
                        record.Detail.Contains("enhancement-unavailable:activation-refused")))
                    throw new InvalidOperationException("the setup refusal was lost.");
                if (!rejectReport.Records.Any(record =>
                        record.Status == CastExecutionStatus.ResidualStateUnsettled &&
                        record.Detail.Contains("enhancement-cleanup-failed")))
                    throw new InvalidOperationException("the rejected preparation's " +
                        "cleanup outcome was lost.");
                if (!rejectReport.Records.Any(record =>
                        record.AssignmentId == "share-2" &&
                        record.Detail.Contains("prior-animated-transaction-unsettled")))
                    throw new InvalidOperationException("a later casting was not halted " +
                        "after the rejected preparation's residual state.");
            }
            // A clean lease stays an ordinary success with no such entries.
            var clean = new CleanupStubWorld();
            var cleanReport = new ExecutionReport(new CastPlan(new[] { CleanupStep("share-1"), CleanupStep("share-2") },
                    new TargetPlanOutcome[0], new string[0]));
            var cleanIterator = new AnimatedCastExecutor(clean, false)
                .Execute(new CastPlan(new[] { CleanupStep("share-1"), CleanupStep("share-2") },
                    new TargetPlanOutcome[0], new string[0]), cleanReport);
            while (cleanIterator.MoveNext()) { }
            if (cleanReport.Records.Any(record =>
                    record.Detail.Contains("enhancement-cleanup-failed")))
                throw new InvalidOperationException("a verified-clean cleanup reported a " +
                    "failure.");
        }

        // R579-3: unrecovered intent is never dropped. Nine distinct
        // failed-flush recoveries all stay retrievable; the transition that
        // cannot be preserved is REFUSED with ownership retained; and a
        // factory failure after acquisition leaves the exact recovery for
        // the next attempt (no test-held session references anywhere).
        private static void TestRecoveryPreservation(string root)
        {
            string dir = Path.Combine(root, "recovery-preservation");
            Directory.CreateDirectory(dir);
            var store = new CastingWorkspaceRecoveryStore();
            var messages = new System.Collections.Generic.List<string>();
            var owner = new CastingSessionOwner(
                id => store.Adopt(dir, id,
                    pending => new CastingWorkspaceSession(dir, id,
                        new DisabledCastingDispatchBoundary(), null, null, pending),
                    () => new CastingWorkspaceSession(dir, id,
                        new DisabledCastingDispatchBoundary())),
                store, messages.Add);
            // Nine distinct failed-flush recoveries ALL register (no
            // lossy bound); each campaign's latest intent stays retrievable
            // exactly.
            for (int index = 1; index <= 9; index++)
            {
                string campaign = "campaign:pad" + index;
                CastingWorkspaceSession session;
                if (owner.Ensure(campaign, out session) != null)
                    throw new InvalidOperationException("owning " + campaign + " failed.");
                string casting = "cast-p" + index;
                if (!session.AddCastingForRuntime(LifecycleCasting(casting, "unit-t1")).Applied)
                    throw new InvalidOperationException(campaign + " authoring refused.");
                string planPath = new CastingPlanRepository(dir).GetProfilePath(campaign);
                using (var hold = new FileStream(planPath, FileMode.Open,
                    FileAccess.Read, FileShare.Read))
                {
                    if (!session.AddCastingForRuntime(
                            LifecycleCasting(casting + "b", "unit-t2")).Applied)
                        throw new InvalidOperationException("locked authoring refused.");
                    CastingWorkspaceSession hub;
                    if (owner.Ensure("campaign:hub", out hub) != null)
                        throw new InvalidOperationException("transition " + index +
                            " was refused.");
                }
                if (!store.HasPending(dir, campaign))
                    throw new InvalidOperationException("campaign " + index + "'s intent " +
                        "was not preserved.");
            }
            if (store.Count != 9)
                throw new InvalidOperationException("expected nine preserved recoveries," +
                    " found " + store.Count);
            for (int index = 1; index <= 9; index++)
            {
                string campaign = "campaign:pad" + index;
                PendingSessionRecovery pending = store.Peek(dir, campaign);
                if (pending == null ||
                    pending.Document.Castings.Count != 2 ||
                    !pending.Document.Castings.Any(value =>
                        value.CastingId == "cast-p" + index + "b"))
                    throw new InvalidOperationException("recovery " + index + " lost the " +
                        "latest intent.");
            }
            // C853-3's own exit: NON-REFUSABLE TEARDOWN while the store is
            // full and the current campaign's flush cannot succeed. The
            // intent must still be preserved, and a REPLACEMENT owner (a new
            // root in the same process) recovers it - no test-held session
            // references anywhere.
            {
                string campaign = "campaign:teardown";
                CastingWorkspaceSession session;
                if (owner.Ensure(campaign, out session) != null)
                    throw new InvalidOperationException("owning the teardown campaign " +
                        "failed.");
                if (!session.AddCastingForRuntime(LifecycleCasting("cast-t1", "unit-t1")).Applied)
                    throw new InvalidOperationException("teardown authoring refused.");
                string planPath = new CastingPlanRepository(dir).GetProfilePath(campaign);
                using (var hold = new FileStream(planPath, FileMode.Open,
                    FileAccess.Read, FileShare.Read))
                {
                    if (!session.AddCastingForRuntime(
                            LifecycleCasting("cast-t2", "unit-t2")).Applied)
                        throw new InvalidOperationException("locked teardown authoring " +
                            "refused.");
                    owner.Release("root-teardown");
                }
                if (!store.HasPending(dir, campaign))
                    throw new InvalidOperationException("teardown at a full store dropped " +
                        "the intent.");
                // Drop every reference; a replacement owner adopts through
                // the production factory once storage heals.
                var replacementOwner = new CastingSessionOwner(
                    id => store.Adopt(dir, id,
                        pending => new CastingWorkspaceSession(dir, id,
                            new DisabledCastingDispatchBoundary(), null, null, pending),
                        () => new CastingWorkspaceSession(dir, id,
                            new DisabledCastingDispatchBoundary())),
                    store, messages.Add);
                CastingWorkspaceSession revived;
                if (replacementOwner.Ensure(campaign, out revived) != null)
                    throw new InvalidOperationException("the replacement owner refused " +
                        "the teardown campaign.");
                if (revived.Document.Castings.Count != 2 ||
                    !revived.Document.Castings.Any(value => value.CastingId == "cast-t2") ||
                    revived.IsDirty || revived.AutosaveStatus != "saved")
                    throw new InvalidOperationException("the replacement owner did not " +
                        "recover the teardown intent: " + revived.Document.Castings.Count +
                        ";" + revived.AutosaveStatus);
            }
            // A construction failure AFTER acquisition (inside the adoption
            // callback, once the expected pending intent is in hand) leaves
            // the exact recovery for the next attempt.
            {
                string campaign = "campaign:factory";
                CastingWorkspaceSession session;
                if (owner.Ensure(campaign, out session) != null)
                    throw new InvalidOperationException("owning the factory campaign failed.");
                if (!session.AddCastingForRuntime(LifecycleCasting("cast-f1", "unit-t1")).Applied)
                    throw new InvalidOperationException("factory authoring refused.");
                string planPath = new CastingPlanRepository(dir).GetProfilePath(campaign);
                using (var hold = new FileStream(planPath, FileMode.Open,
                    FileAccess.Read, FileShare.Read))
                {
                    if (!session.AddCastingForRuntime(
                            LifecycleCasting("cast-f2", "unit-t2")).Applied)
                        throw new InvalidOperationException("locked factory authoring " +
                            "refused.");
                    CastingWorkspaceSession hub;
                    if (owner.Ensure("campaign:hub", out hub) != null)
                        throw new InvalidOperationException("switch-away failed.");
                }
                bool failedOnce = false;
                var retryOwner = new CastingSessionOwner(
                    id => store.Adopt(dir, id,
                        pending =>
                        {
                            if (string.Equals(id, campaign, StringComparison.Ordinal) &&
                                !failedOnce)
                            {
                                failedOnce = true;
                                if (pending == null ||
                                    pending.Document.Castings.Count != 2 ||
                                    !pending.Document.Castings.Any(value =>
                                        value.CastingId == "cast-f2"))
                                    throw new InvalidOperationException("the adoption " +
                                        "callback did not receive the expected pending " +
                                        "intent.");
                                throw new InvalidOperationException("simulated construction " +
                                    "failure after acquisition");
                            }
                            return new CastingWorkspaceSession(dir, id,
                                new DisabledCastingDispatchBoundary(), null, null, pending);
                        },
                        () => new CastingWorkspaceSession(dir, id,
                            new DisabledCastingDispatchBoundary())),
                    store, messages.Add);
                bool threw = false;
                try
                {
                    CastingWorkspaceSession failed;
                    retryOwner.Ensure(campaign, out failed);
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }
                if (!threw || !store.HasPending(dir, campaign))
                    throw new InvalidOperationException("a construction failure after " +
                        "acquisition consumed the only recoverable copy.");
                CastingWorkspaceSession recovered;
                if (retryOwner.Ensure(campaign, out recovered) != null)
                    throw new InvalidOperationException("the retry was refused.");
                if (recovered.Document.Castings.Count != 2 ||
                    !recovered.Document.Castings.Any(value => value.CastingId == "cast-f2"))
                    throw new InvalidOperationException("the retry did not recover the " +
                        "latest intent.");
                if (store.HasPending(dir, campaign))
                    throw new InvalidOperationException("successful adoption left a stale " +
                        "entry.");
            }
        }

        // C853-1: the PRODUCTION cleanup algorithm (EnhancementLeaseCleanup,
        // stubbed only at native state access) verifies the POLICY-EXPECTED
        // FINAL VALUE - not the restore decision. The old algorithm
        // compared IsOn against the boolean decision, so an ordinary OFF
        // rod correctly restored to OFF was misreported as a mismatch and
        // halted the whole routine (the first assertion below fails against
        // it). Consumed one-shots stay consumed; unreadable consumption
        // makes NO positive restoration decision; failures stay observable
        // and independent states keep being cleaned.
        private sealed class StubActivatable : IActivatableStateAccess
        {
            internal bool On;
            internal bool Running;
            internal bool SetterThrows;
            internal bool StopLies;
            internal bool GetterThrowsAfterSet;
            internal bool LyingSetter;
            internal int Writes;
            public string Identity { get { return "stub-ability"; } }
            public bool IsOn
            {
                get
                {
                    if (GetterThrowsAfterSet) throw new InvalidOperationException("unreadable");
                    return On;
                }
                set
                {
                    if (SetterThrows) throw new InvalidOperationException("setter");
                    if (LyingSetter) return;
                    Writes++;
                    On = value;
                }
            }
            public bool IsRunning { get { return Running; } }
            public void Stop()
            {
                if (StopLies) return;
                Running = false;
            }
        }

        private static EnhancementLeaseCleanup.OwnedState Owned(
            IActivatableStateAccess access, bool original, bool oneShot = false,
            bool selected = false, bool armed = false, bool rod = false,
            string group = null)
        {
            return new EnhancementLeaseCleanup.OwnedState
            {
                Access = access, OriginalIsOn = original, OneShot = oneShot,
                Selected = selected, ArmedByLease = armed, Rod = rod,
                ActivationGroupId = group ?? string.Empty
            };
        }

        private static void TestNativeCleanupExpectedState()
        {
            // An ordinary OFF rod, restored to OFF: verified CLEAN (the
            // pre-repair algorithm misreported this as restore-mismatch).
            var offRod = new StubActivatable { On = true };
            var states = new List<EnhancementLeaseCleanup.OwnedState>
            {
                Owned(offRod, false, rod: true)
            };
            offRod.On = false; // pre-cast original captured as false; the cast armed it.
            offRod.On = true;
            string failure = EnhancementLeaseCleanup.Cleanup(states, true);
            if (failure.Length != 0)
                throw new InvalidOperationException("a correctly restored OFF rod " +
                    "was reported as a cleanup failure: " + failure);
            // An ordinary ON feature stays ON; an unselected OFF rod stays OFF.
            var onFeature = new StubActivatable { On = false };
            var idleRod = new StubActivatable { On = false };
            onFeature.On = true;
            var mixed = new List<EnhancementLeaseCleanup.OwnedState>
            {
                Owned(onFeature, true),
                Owned(idleRod, false, rod: true)
            };
            failure = EnhancementLeaseCleanup.Cleanup(mixed, true);
            if (failure.Length != 0 || !onFeature.On || idleRod.On)
                throw new InvalidOperationException("ordinary states were not restored " +
                    "and verified: " + failure);
            // A verified CONSUMED one-shot stays OFF (not resurrected) and
            // reports no failure; an unconsumed one is restored.
            var consumed = new StubActivatable { On = false };
            var unconsumed = new StubActivatable { On = false };
            unconsumed.On = true;
            var oneShots = new List<EnhancementLeaseCleanup.OwnedState>
            {
                Owned(consumed, true, oneShot: true, selected: true, armed: true,
                    group: "g1"),
                Owned(unconsumed, true, oneShot: true, selected: true, armed: true,
                    group: "g2")
            };
            failure = EnhancementLeaseCleanup.Cleanup(oneShots, false);
            if (failure.Length != 0 || consumed.On || !unconsumed.On)
                throw new InvalidOperationException("one-shot consumption policy was " +
                    "wrong: " + failure + ";consumed=" + consumed.On);
            // Unreadable consumption: NO positive restoration of one-shots,
            // the failure is reported, and ordinary states still restore.
            var unreadable = new StubActivatable();
            var ordinary = new StubActivatable { On = true };
            var unreadableStates = new List<EnhancementLeaseCleanup.OwnedState>
            {
                Owned(unreadable, true, oneShot: true, selected: true, armed: true),
                Owned(ordinary, true)
            };
            // The one-shot's read throws, so consumption cannot be known.
            var throwing = new StubActivatable { GetterThrowsAfterSet = true };
            throwing.On = true; // its CURRENT value; NO write may occur to it
            unreadableStates[0].Access = throwing;
            // The independent ordinary state currently DIFFERS from its
            // original; it must still be restored despite the read failure.
            ordinary.On = false;
            failure = EnhancementLeaseCleanup.Cleanup(unreadableStates, false);
            if (!failure.Contains("consumption-unreadable"))
                throw new InvalidOperationException("unreadable consumption was not " +
                    "reported: " + failure);
            // No positive one-shot write occurred: the uncertain one-shot's
            // setter was never invoked and its current value is untouched.
            if (throwing.Writes != 0 || !throwing.On)
                throw new InvalidOperationException("an uncertain one-shot was written.");
            if (!ordinary.On)
                throw new InvalidOperationException("an ordinary state was not restored " +
                    "after an unrelated read failure.");
            // A setter exception is reported and the OTHER state still cleans.
            var setterThrows = new StubActivatable { On = true, SetterThrows = true };
            var sibling = new StubActivatable { On = true };
            failure = EnhancementLeaseCleanup.Cleanup(new List<
                EnhancementLeaseCleanup.OwnedState>
            {
                Owned(setterThrows, false),
                Owned(sibling, false)
            }, false);
            if (!failure.Contains("restore-exception") || sibling.On)
                throw new InvalidOperationException("a setter exception was swallowed or " +
                    "stopped independent cleanup: " + failure);
            // A LYING setter (accepts the write without applying it) is a
            // postcondition mismatch naming the state's identity.
            var liar = new StubActivatable { On = true, LyingSetter = true };
            failure = EnhancementLeaseCleanup.Cleanup(new List<
                EnhancementLeaseCleanup.OwnedState> { Owned(liar, false) }, false);
            if (!failure.Contains("restore-mismatch:stub-ability;expected=False") ||
                !liar.On)
                throw new InvalidOperationException("a lying setter was not exposed " +
                    "as a postcondition mismatch: " + failure);
            var mismatch = new StubActivatable { On = true };
            var mismatchStates = new List<EnhancementLeaseCleanup.OwnedState>
            { Owned(mismatch, false, rod: true) };
            // Force: cleanup sets false; verify reads true -> mismatch.
            failure = EnhancementLeaseCleanup.Cleanup(mismatchStates, true);
            if (failure.Length != 0 || mismatch.On)
                throw new InvalidOperationException("the rod was not restored and " +
                    "verified clean: " + failure);
            // Rod running residue: a Stop that does not stop is a mismatch.
            var rod = new StubActivatable { On = false, Running = true, StopLies = true };
            failure = EnhancementLeaseCleanup.Cleanup(new List<
                EnhancementLeaseCleanup.OwnedState>
            {
                Owned(rod, false, rod: true)
            }, true);
            if (!failure.Contains("rod-stop-mismatch"))
                throw new InvalidOperationException("an un-stopped rod was not reported: " +
                    failure);
            // The same rod with a working Stop is verified clean.
            var rod2 = new StubActivatable { On = false, Running = true };
            failure = EnhancementLeaseCleanup.Cleanup(new List<
                EnhancementLeaseCleanup.OwnedState>
            {
                Owned(rod2, false, rod: true)
            }, true);
            if (failure.Length != 0 || rod2.Running)
                throw new InvalidOperationException("a stopped rod was misreported: " +
                    failure);
        }

        // F3: the ordinary discovery path NEVER mutates native activatable
        // state. This is an exact assembly-backed boundary proof over the
        // BUILT production assembly: every method on the ordinary
        // discovery path (Discover, ForCast, IsSupportedSpell, Snapshot,
        // TryDescribePersisted, TryResolveToggle, the blueprint validators,
        // and the enhancement adapter's Discover) contains zero calls to
        // ActivatableAbility state mutation; the isolated probe
        // TryProbeShareTargeting is the ONLY method that arms the toggle
        // (and doubles as the scan's positive control — if the scanner can
        // see the probe's arming, it would see any arming re-added to the
        // ordinary path).
        // The execution-only Share bridge (v1.2 §7): the converter executes
        // a targeting modifier ONLY through its VERIFIED enhancement
        // identity for the exact caster — the step carries that id (so the
        // proven enhancement lease arms the exact toggle for this one cast
        // and restores it on every terminal path) plus the reservoir usage,
        // the projection hash covers both, and everything else stays
        // fail-closed: no mapping, the probe scope, and a merely-disabled
        // selection never execute a modifier.
        private static void TestShareProjectionContract()
        {
            var units = new[]
            {
                new UnitSnapshot("unit-wiz", "Wiz", false, null,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-cleric", "Cleric", false, null,
                    new TargetValidationSnapshot(true, true, true, true))
            };
            var shapeExpression = new Domain.Effects.EffectLeafExpression(
                Domain.Effects.EffectKind.Buff, "buff-shape",
                Domain.Effects.EffectTarget.CurrentTarget, "shape", "shape/a");
            var effects = new Dictionary<string, Domain.Effects.EffectExpression>
            {
                { "source-shape", shapeExpression },
                { ShapeAbility().Canonical, shapeExpression }
            };
            var snapshot = new PartyProviderSnapshot(units, new[]
            {
                ShapeProvider("unit-wiz")
            }, new[]
            {
                new ResourcePoolSnapshot("pool-unit-wiz", ResourcePoolKind.SpontaneousLevel,
                    10, 10, null)
            });
            var share = new ShareCastingModifier(new[]
                { Capability("unit-wiz", WizardReservoir, 1, 3, new[] { "unit-cleric" }) });
            var enhancements = new[] { ShareEnhancement("unit-wiz", WizardReservoir, 3) };
            var gate = new CastingExecutionGate();
            // The session's verified map: modifier|caster -> enhancement id.
            var inputs = new CastingWorkspaceInputs(snapshot,
                new[] { PersonalOption("unit-wiz") }, effects, enhancements,
                new ICastingTargetingModifier[] { share });
            var map = CastingWorkspaceSession.TargetingModifierEnhancementMap(inputs);
            if (map.Count != 1 ||
                !string.Equals(map["share-transmutation|unit-wiz"],
                    ShareEnhancement("unit-wiz", WizardReservoir, 3).EnhancementId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("the verified modifier map is wrong.");
            ExplicitCastingPlan sharedPlan = new ExplicitCastingCompiler().Compile(
                new CastingPlanDocument("campaign:bridge",
                    new[] { new RoutineDefinition("long", "Long") },
                    new[] { SharedCasting("share-1", "unit-wiz", "unit-cleric") }, null),
                snapshot, new[] { PersonalOption("unit-wiz") }, effects, enhancements,
                null, new[] { share });
            CastingApplyDecision decision = gate.Evaluate(sharedPlan, CastingApplyMode.Ordinary,
                "long");
            if (!decision.Allowed || decision.ExecutableCastingIds.Count != 1)
                throw new InvalidOperationException("the gate did not allow the shared " +
                    "casting: " + string.Join(";", decision.BlockingReasons.ToArray()));
            // WITHOUT the verified mapping: still fail-closed.
            ExplicitStepConversion refused = ExplicitCastingStepConverter.Convert(
                sharedPlan, decision, new[] { PersonalOption("unit-wiz") }, effects);
            if (refused.Converted || !refused.Refusal.Contains(
                    "unsupported-contract:targeting-modifier:share-1:share-transmutation:" +
                    "execution-enhancement-unverified"))
                throw new InvalidOperationException("an unverified modifier execution was " +
                    "not refused fail-closed: " + refused.Refusal);
            // WITH the verified mapping: one step carrying the exact toggle
            // enhancement and the reservoir usage, hashed into the identity.
            ExplicitStepConversion converted = ExplicitCastingStepConverter.Convert(
                sharedPlan, decision, new[] { PersonalOption("unit-wiz") }, effects,
                ExplicitProjectionScope.Standard, map);
            if (!converted.Converted)
                throw new InvalidOperationException("the verified modifier execution was " +
                    "refused: " + converted.Refusal);
            CastStep step = converted.Plan.Steps[0];
            string shareEnhancementId = ShareEnhancement("unit-wiz", WizardReservoir, 3)
                .EnhancementId;
            if (!step.EnhancementIds.Contains(shareEnhancementId))
                throw new InvalidOperationException("the step does not arm the exact " +
                    "verified Share toggle enhancement.");
            if (!step.EnhancementUsageByPool.ContainsKey(WizardReservoir) ||
                step.EnhancementUsageByPool[WizardReservoir] != 1)
                throw new InvalidOperationException("the step does not carry the verified " +
                    "reservoir usage.");
            if (converted.ProjectionId.Length == 0 || !converted.CanonicalContract
                    .Contains(shareEnhancementId))
                throw new InvalidOperationException("the projection hash does not cover the " +
                    "modifier's identity.");
            if (!converted.CanonicalContract.Contains(WizardReservoir))
                throw new InvalidOperationException("the projection hash does not cover the " +
                    "modifier's cost.");
            // Identity sensitivity: the same spell cast WITHOUT Share
            // (self-target, the personal spell's own legal target) hashes
            // differently and arms nothing.
            // The probe scope stays refuse-first for targeting modifiers.
            ExplicitStepConversion probe = ExplicitCastingStepConverter.Convert(
                sharedPlan, decision, new[] { PersonalOption("unit-wiz") }, effects,
                ExplicitProjectionScope.SingleCastProbe, map);
            if (probe.Converted || !probe.Refusal.StartsWith("probe-unsupported:targeting-modifier",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("the probe scope accepted a targeting " +
                    "modifier: " + probe.Refusal);
            // A merely-DISABLED selection executes nothing.
            ExplicitCastingPlan disabledPlan = new ExplicitCastingCompiler().Compile(
                new CastingPlanDocument("campaign:bridge",
                    new[] { new RoutineDefinition("long", "Long") },
                    new[]
                    {
                        new PlannedCasting("share-1", "long", 0, "source-shape",
                            ShapeAbility(), "unit-wiz", "book", CastingTargetMode.DirectTarget,
                            "unit-wiz", null, null,
                            new[] { new TargetingModifierSelection(
                                ShareCastingModifier.Id, false, null) }, null,
                            ExistingEffectPolicy.SkipAlreadyActive, null,
                            CastingAuthoringState.Ready, null)
                    }, null),
                snapshot, new[] { PersonalOption("unit-wiz") }, effects, enhancements,
                null, new[] { share });
            CastingApplyDecision disabledDecision = gate.Evaluate(disabledPlan,
                CastingApplyMode.Ordinary, "long");
            ExplicitStepConversion disabledProjection = ExplicitCastingStepConverter.Convert(
                disabledPlan, disabledDecision, new[] { PersonalOption("unit-wiz") }, effects,
                ExplicitProjectionScope.Standard, map);
            if (!disabledProjection.Converted ||
                disabledProjection.Plan.Steps[0].EnhancementIds.Contains(shareEnhancementId))
                throw new InvalidOperationException("a disabled modifier selection armed " +
                    "the toggle.");
        }

        private static void TestShareDiscoveryBoundary()
        {
            string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "..", "build", "Release", "KingmakerBuffPlanner.dll");
            if (!File.Exists(assemblyPath))
                throw new InvalidOperationException("the built production assembly was not " +
                    "found at " + assemblyPath);
            Assembly production = Assembly.LoadFrom(assemblyPath);
            Type compatibility = production.GetType(
                "KingmakerBuffPlanner.Compatibility.BrownFurShareTransmutationCompatibility");
            if (compatibility == null)
                throw new InvalidOperationException("the compatibility type is missing.");
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            MethodInfo probe = compatibility.GetMethod("TryProbeShareTargeting", flags);
            if (probe == null)
                throw new InvalidOperationException("the isolated probe is missing.");
            // The positive control FIRST: the scanner must find the probe's
            // own arming (set_IsOn), proving it detects exactly the pattern
            // the ordinary path must not contain.
            if (FindActivatableMutations(probe).Count == 0)
                throw new InvalidOperationException("the scan did not detect the probe's " +
                    "own arming call (the boundary proof is blind).");
            string[] ordinaryNames =
            {
                "Discover", "ForCast", "TryDescribePersisted", "IsSupportedSpell",
                "TryResolveToggle", "TryValidateNativeRuntime", "Snapshot",
                "ValidToggleBlueprint", "ValidFeatureBlueprint"
            };
            foreach (string name in ordinaryNames)
            {
                foreach (MethodInfo method in compatibility.GetMethods(flags)
                    .Where(value => value.Name == name))
                {
                    List<string> mutations = FindActivatableMutations(method);
                    if (mutations.Count != 0)
                        throw new InvalidOperationException("ordinary discovery path method " +
                            name + " mutates native activatable state: " +
                            string.Join(",", mutations.ToArray()));
                }
            }
            // The probe is the ONLY method of the compatibility type that
            // touches activatable state mutation.
            foreach (MethodInfo method in compatibility.GetMethods(flags)
                .Where(value => value.GetMethodBody() != null))
            {
                bool isProbe = method == probe;
                List<string> mutations = FindActivatableMutations(method);
                if (isProbe && mutations.Count == 0)
                    throw new InvalidOperationException("the probe lost its arming call.");
                if (!isProbe && mutations.Count != 0)
                    throw new InvalidOperationException("a non-probe method (" + method.Name +
                        ") mutates native activatable state: " +
                        string.Join(",", mutations.ToArray()));
            }
            // The enhancement adapter's own ordinary discovery (Discover,
            // Combine, TryDescribePersisted, RodEntries) is equally pure.
            Type adapter = production.GetType(
                "KingmakerBuffPlanner.GameAdapters.KingmakerCastEnhancementAdapter");
            if (adapter == null)
                throw new InvalidOperationException("the enhancement adapter type is missing.");
            foreach (string name in new[] { "Discover", "Combine", "TryDescribePersisted" })
            {
                foreach (MethodInfo method in adapter.GetMethods(flags)
                    .Where(value => value.Name == name && value.GetMethodBody() != null))
                {
                    List<string> mutations = FindActivatableMutations(method);
                    if (mutations.Count != 0)
                        throw new InvalidOperationException("the adapter's ordinary discovery (" +
                            name + ") mutates native activatable state: " +
                            string.Join(",", mutations.ToArray()));
                }
            }
            // Static ctors of both types stay pure too.
            foreach (Type type in new[] { compatibility, adapter })
            {
                ConstructorInfo ctor = type.TypeInitializer;
                if (ctor != null && FindActivatableMutations(ctor).Count != 0)
                    throw new InvalidOperationException(type.Name +
                        "'s static initializer mutates native activatable state.");
            }
        }

        // Walks a method body's IL and returns every call/callvirt whose
        // target is a state-mutating member declared by ActivatableAbility
        // (the set_IsOn setter, TurnOn/TurnOff, Stop, set_ResourceCount).
        private static List<string> FindActivatableMutations(MethodBase method)
        {
            var found = new List<string>();
            MethodBody body = method.GetMethodBody();
            if (body == null) return found;
            byte[] il = body.GetILAsByteArray();
            Module module = method.Module;
            int position = 0;
            while (position < il.Length)
            {
                int op = il[position];
                if (op == 0xFE && position + 1 < il.Length)
                {
                    op = 0xFE00 | il[position + 1];
                    position += 2;
                }
                else position++;
                int operandSize;
                if (!IlOperandSize(op, il, position, out operandSize)) break;
                if ((op == 0x28 || op == 0x6F) && position + 4 <= il.Length)
                {
                    int token = BitConverter.ToInt32(il, position);
                    try
                    {
                        MethodBase target = module.ResolveMember(token,
                            method.DeclaringType == null ? null
                                : method.DeclaringType.GetGenericArguments(),
                            null) as MethodBase;
                        if (target != null && IsActivatableMutation(target))
                            found.Add(target.Name);
                    }
                    catch (Exception)
                    {
                        // Unresolvable tokens are not activatable mutations;
                        // the resolved-set assertion above stays exact.
                    }
                }
                position += operandSize;
            }
            return found;
        }

        // True only for the state-mutating members DECLARED on (or
        // inherited by) the game's ActivatableAbility: the toggle setter,
        // explicit turn on/off, stop, and the resource-count setter.
        private static bool IsActivatableMutation(MethodBase target)
        {
            if (target.Name != "set_IsOn" && target.Name != "TurnOn" &&
                target.Name != "TurnOff" && target.Name != "Stop" &&
                target.Name != "set_ResourceCount")
                return false;
            for (Type walker = target.DeclaringType; walker != null; walker = walker.BaseType)
                if (walker.FullName ==
                    "Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbility")
                    return true;
            return false;
        }

        // Operand sizes for the single- and two-byte opcodes that appear in
        // these method bodies (table-driven from System.Reflection.Emit so
        // the walker cannot misparse a prefix).
        private static bool IlOperandSize(int op, byte[] il, int position, out int size)
        {
            size = 0;
            System.Reflection.Emit.OpCode code;
            if (op < 0x100 && IlSingleByte().TryGetValue(op, out code) ||
                op >= 0xFE00 && IlTwoByte().TryGetValue(op & 0xFF, out code))
            {
                switch (code.OperandType)
                {
                    case System.Reflection.Emit.OperandType.InlineNone:
                        size = 0;
                        return true;
                    case System.Reflection.Emit.OperandType.ShortInlineBrTarget:
                    case System.Reflection.Emit.OperandType.ShortInlineI:
                    case System.Reflection.Emit.OperandType.ShortInlineVar:
                        size = 1;
                        return true;
                    case System.Reflection.Emit.OperandType.InlineVar:
                        size = 2;
                        return true;
                    case System.Reflection.Emit.OperandType.InlineI8:
                    case System.Reflection.Emit.OperandType.InlineR:
                        size = 8;
                        return true;
                    case System.Reflection.Emit.OperandType.InlineSwitch:
                        if (position + 4 <= il.Length)
                        {
                            size = 4 + 4 * BitConverter.ToInt32(il, position);
                            return true;
                        }
                        return false;
                    default:
                        size = 4;
                        return true;
                }
            }
            return false;
        }

        private static Dictionary<int, System.Reflection.Emit.OpCode> _ilSingleByte;
        private static Dictionary<int, System.Reflection.Emit.OpCode> _ilTwoByte;

        private static Dictionary<int, System.Reflection.Emit.OpCode> IlSingleByte()
        {
            if (_ilSingleByte != null) return _ilSingleByte;
            _ilSingleByte = new Dictionary<int, System.Reflection.Emit.OpCode>();
            foreach (System.Reflection.Emit.OpCode code in
                typeof(System.Reflection.Emit.OpCodes).GetFields(
                    BindingFlags.Public | BindingFlags.Static)
                .Select(field => (System.Reflection.Emit.OpCode)field.GetValue(null)))
            {
                if (code.Value >= 0 && code.Value < 0x100 && !_ilSingleByte.ContainsKey(code.Value))
                    _ilSingleByte[code.Value] = code;
            }
            return _ilSingleByte;
        }

        private static Dictionary<int, System.Reflection.Emit.OpCode> IlTwoByte()
        {
            if (_ilTwoByte != null) return _ilTwoByte;
            _ilTwoByte = new Dictionary<int, System.Reflection.Emit.OpCode>();
            foreach (System.Reflection.Emit.OpCode code in
                typeof(System.Reflection.Emit.OpCodes).GetFields(
                    BindingFlags.Public | BindingFlags.Static)
                .Select(field => (System.Reflection.Emit.OpCode)field.GetValue(null)))
            {
                if (code.Value < 0 && !_ilTwoByte.ContainsKey(code.Value & 0xFF))
                    _ilTwoByte[code.Value & 0xFF] = code;
            }
            return _ilTwoByte;
        }
    }
}
