using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // Everyday-use v1.2 §7 Share Transmutation planning pipeline: the pure
    // modifier expands a personal transmutation to the verified legal allies
    // with the verified reservoir demand in the atomic cost vector; the
    // graph gesture carries the draft's Share selection; disabling Share
    // keeps an existing ally casting as blocked, repairable, persisted.
    internal static partial class Program
    {
        private static void RunSharePlanningTests(string root)
        {
            Run("share-expands-personal-targets-purely", TestSharePureExpansion);
            Run("share-graph-gesture-carries-and-persists", () => TestShareGraphPersistence(root));
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

        private static void TestSharePureExpansion()
        {
            // Review addendum §3: verified per-caster capabilities; the
            // reservoir and cost follow the casting's OWN caster regardless
            // of registration/party order.
            var capabilities = new[]
            {
                new ShareCastingModifier.ShareCapability("unit-wiz",
                    "class-feature-resource|unit-wiz|reservoir", 1, 3),
                new ShareCastingModifier.ShareCapability("unit-sorcerer",
                    "class-feature-resource|unit-sorcerer|reservoir", 2, 1)
            };
            var share = new ShareCastingModifier(capabilities,
                new[] { "unit-cleric", "unit-fighter", "unit-wiz", "unit-sorcerer" });
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
            // No legal allies → feature unavailable, honestly.
            var none = new ShareCastingModifier(capabilities, new string[0]);
            if (none.Apply(casting, PersonalOption("unit-wiz")).IsApplied)
                throw new InvalidOperationException("Share applied with no legal allies.");
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
            // Reversed registration order resolves identically.
            var reversed = new ShareCastingModifier(capabilities.Reverse(),
                new[] { "unit-cleric", "unit-fighter", "unit-wiz", "unit-sorcerer" });
            IReadOnlyList<ModifierUsageDemand> reversedDemands = reversed.UsageDemands(
                sorcererCasting, PersonalOption("unit-sorcerer"));
            if (reversedDemands[0].UsagePoolId != sorcererDemands[0].UsagePoolId ||
                reversedDemands[0].Units != sorcererDemands[0].Units)
                throw new InvalidOperationException("registration order changed the cost.");
            // An INELIGIBLE first member: an incapable caster is refused
            // (honest feature-unavailable) and never costed as free — the
            // demand is unsatisfiable, so the compiler blocks atomically.
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
            // Reservoir exhausted (verified remaining 0): honest refusal.
            var drained = new[]
            {
                new ShareCastingModifier.ShareCapability("unit-wiz",
                    "class-feature-resource|unit-wiz|reservoir", 1, 0)
            };
            var drainedShare = new ShareCastingModifier(drained,
                new[] { "unit-cleric" });
            if (drainedShare.Apply(casting, PersonalOption("unit-wiz")).IsApplied)
                throw new InvalidOperationException("Share applied with an exhausted reservoir.");
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
            // expression INSTANCE under both catalogue keys.
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
                new ShareCastingModifier(
                    new[]
                    {
                        new ShareCastingModifier.ShareCapability("unit-wiz",
                            "class-feature-resource|unit-wiz|reservoir", 1, 3)
                    },
                    units.Where(u => u.TargetValidation.Friendly).Select(u => u.UnitId))
            };
            var inputs = new CastingWorkspaceInputs(snapshot,
                new[]
                {
                    new ProviderPlanningOption(providers[0],
                        new[] { "unit-wiz" }, new string[0], 5, 600)
                },
                effects, new CastEnhancementSnapshot[0], shareModifiers);
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
            session.Draft.TargetingModifiers.Add(
                new TargetingModifierSelection(ShareCastingModifier.Id, true, null));
            CastingGraphEditResult added = session.AddGraphCasting("unit-cleric", inputs);
            if (!added.Applied)
                throw new InvalidOperationException("the shared authoring was refused: " +
                    (added.Edit == null ? "none" : added.Edit.Reason));
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
            // retargeted to the caster.
            session.FocusGraphCasting(added.CastingId);
            var withoutShare = new PlannedCasting(persisted.CastingId, persisted.RoutineId,
                persisted.Order, persisted.SourceId, persisted.Ability, persisted.CasterUnitId,
                persisted.SpellbookGuid, persisted.TargetMode, persisted.DirectTargetUnitId,
                persisted.Origin, persisted.RequiredCoverageUnitIds, new TargetingModifierSelection[0],
                persisted.Enhancements, persisted.ExistingEffectPolicy,
                persisted.IgnoredPresenceMarkers, persisted.State, persisted.Provenance);
            if (!session.UpdateFocusedCasting(withoutShare).Applied)
                throw new InvalidOperationException("disabling Share was refused.");
            PlannedCasting disabled = session.Document.Castings.First(
                value => value.CastingId == added.CastingId);
            if (!string.Equals(disabled.DirectTargetUnitId, "unit-cleric",
                    StringComparison.Ordinal) || disabled.TargetingModifiers.Count != 0)
                throw new InvalidOperationException("disabling Share changed the target or " +
                    "left the selection.");
            var reload = new CastingWorkspaceSession(dir, "campaign:share");
            PlannedCasting reloaded = reload.Document.Castings.First(
                value => value.CastingId == added.CastingId);
            if (!string.Equals(reloaded.DirectTargetUnitId, "unit-cleric",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("reload lost the blocked ally intent.");
        }
    }
}
