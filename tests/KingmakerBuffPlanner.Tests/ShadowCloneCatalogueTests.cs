using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // 0.4.2 (A): Call of the Wild's Ninja trick Shadow Clone
    // (NinjaShadowCloneMirrorImage 335a48ec..., CotW 1.14.4c-2.1) was missing
    // from the casting-first buff list. Facts are the installed ability's
    // own (0.4.1 catalogue export kbp041-rel-catalog-full): Supernatural,
    // Personal (self only), AbilityResourceLogic on NinjaKiResource
    // 74b56c9e..., cost 1, one ContextActionApplyBuff of MirrorImageBuff
    // 98dc7e7c... on the caster - the exact effect tree of the wizard spell
    // Mirror Image (3e4ab69a...). Under the shared effect identity both
    // abilities had one catalogue id; the workspace serves an id only with
    // the representative's expression instance (Mirror Image sorts first),
    // so Shadow Clone served nothing and vanished. The tests run the
    // production catalogue model, workspace graph, authoring, compiler,
    // gate, projection and repository; only the party snapshot is a fixture.
    internal static partial class Program
    {
        private const string ShadowCloneGuid = "335a48ec4134454fb491b3d64f468ada";
        private const string MirrorImageGuid = "3e4ab69ada402d145a5e0ad3ad4b8564";
        private const string MirrorImageBuffGuid = "98dc7e7cc6ef59f4abe20c65708ac623";
        private const string NinjaKiResourceGuid = "74b56c9eba8f43ac9166b5fa93e50197";
        private const string NinjaKiPool = "unit-ninja|resource|" + NinjaKiResourceGuid;

        private static readonly AbilityKey ShadowCloneAbility = new AbilityKey(
            ShadowCloneGuid, string.Empty, 0, SourceKind.AbilityResource, string.Empty);
        private static readonly AbilityKey MirrorImageAbility = new AbilityKey(
            MirrorImageGuid, string.Empty, 0, SourceKind.Spellbook, string.Empty);

        private static void RunShadowCloneCatalogueTests(string root)
        {
            Run("shadow-clone-has-its-own-catalogue-entry", TestShadowCloneOwnEntry);
            Run("shadow-clone-authors-on-its-owner-with-one-ki",
                () => TestShadowCloneAuthorsWithKi(root));
            Run("shadow-clone-exhausted-ki-keeps-entry-and-casting",
                () => TestShadowCloneExhaustedKi(root));
            Run("shadow-clone-absent-without-owner-or-optional-mod", TestShadowCloneAbsent);
            Run("shadow-clone-saved-under-shared-identity-migrates-once",
                () => TestShadowCloneSavedIdentityMigrates(root));
            Run("shadow-clone-identity-archive-obligation-survives-recovery",
                () => TestShadowCloneArchiveObligationRecovery(root));
            Run("class-ability-identity-keeps-spells-aggregated", TestClassAbilityIdentityRule);
            Run("classic-mirror-image-never-plans-shadow-clone", TestClassicKeepsShadowCloneSeparate);
            Run("classic-legacy-shadow-clone-rebinds-to-its-own-ability", TestClassicLegacyShadowCloneRebinds);
            Run("classic-legacy-shadow-clone-never-becomes-mirror-image", TestClassicLegacyShadowCloneWithoutNinja);
            Run("classic-legacy-mirror-image-stays-the-spell", TestClassicLegacyMirrorImageStays);
            Run("classic-legacy-rebinding-saves-and-reopens-idempotently",
                () => TestClassicLegacyRebindingIdempotent(root));
        }

        // The native expression shape both abilities export: the ability
        // reference, its run-action sequence, the apply-buff leaf and the
        // remove-buff adjunct (a non-persistent native action).
        private static EffectExpression ShadowCloneShape(string abilityGuid)
        {
            return new ReferencedAbilityExpression(abilityGuid,
                new SequenceEffectExpression(new EffectExpression[]
                {
                    new SequenceEffectExpression(new EffectExpression[]
                    {
                        new EffectLeafExpression(EffectKind.Buff, MirrorImageBuffGuid,
                            EffectTarget.CurrentTarget, "ContextActionApplyBuff",
                            abilityGuid + "/0:ActionList/0:ContextActionApplyBuff"),
                        new EmptyEffectExpression()
                    })
                }));
        }

        private sealed class ShadowCloneParty
        {
            public PartyProviderSnapshot Snapshot;
            public List<ProviderPlanningOption> Options;
            public Dictionary<string, EffectExpression> Effects;
            public ProviderSnapshot Ninja;
            public ProviderSnapshot Wizard;
        }

        // A wizard with Mirror Image prepared and, when present, a Ninja who
        // owns Shadow Clone through the trick feature.
        private static ShadowCloneParty BuildShadowCloneParty(bool withNinja, int kiRemaining = 6)
        {
            var unitIds = new List<string> { "unit-wizard", "unit-t1" };
            if (withNinja) unitIds.Add("unit-ninja");
            var units = unitIds.Select(id => new UnitSnapshot(id,
                id == "unit-ninja" ? "Raine" : id == "unit-wizard" ? "Leinna" : "Tias",
                false, string.Empty, new TargetValidationSnapshot(true, true, true, true))).ToList();
            var pools = new List<ResourcePoolSnapshot>
            {
                new ResourcePoolSnapshot("unit-wizard|spellbook|wizard-book|prepared",
                    ResourcePoolKind.PreparedSlots, 1, 1, new[]
                    {
                        new ResourceTokenSnapshot("2|0|0", MirrorImageAbility, 2,
                            PreparedSlotKind.Common, true, true, null)
                    })
            };
            ProviderSnapshot wizard = new ProviderSnapshot(
                new ProviderKey("unit-wizard", "wizard-book", MirrorImageAbility, "level-2|heighten-0"),
                "Mirror Image", 2, "unit-wizard|spellbook|wizard-book|prepared", 1, new[] { "2|0|0" },
                null, 11, 110, "Several illusory duplicates of you pop into being.",
                "1 minute/level", "Mirror Image", 0, "Wizard");
            var providers = new List<ProviderSnapshot> { wizard };
            var options = new List<ProviderPlanningOption>
            {
                new ProviderPlanningOption(wizard, new[] { "unit-wizard" }, new[] { "unit-wizard" }, 11, 110)
            };
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { MirrorImageAbility.Canonical, ShadowCloneShape(MirrorImageGuid) }
            };
            ProviderSnapshot ninja = null;
            if (withNinja)
            {
                pools.Add(new ResourcePoolSnapshot(NinjaKiPool, ResourcePoolKind.AbilityResource,
                    6, kiRemaining, null));
                ninja = new ProviderSnapshot(
                    new ProviderKey("unit-ninja", string.Empty, ShadowCloneAbility, string.Empty),
                    "Shadow Clone", 0, NinjaKiPool, 1, null, null, 10, 100,
                    "The ninja can create 1d4 shadowy duplicates of herself.",
                    "1 minute/level", "Shadow Clone", 0);
                providers.Add(ninja);
                options.Add(new ProviderPlanningOption(ninja, new[] { "unit-ninja" },
                    new[] { "unit-ninja" }, 10, 100));
                // Discovery's own instance per ability (never shared).
                effects[ShadowCloneAbility.Canonical] = ShadowCloneShape(ShadowCloneGuid);
            }
            return new ShadowCloneParty
            {
                Snapshot = new PartyProviderSnapshot(units, providers, pools),
                Options = options,
                Effects = effects,
                Ninja = ninja,
                Wizard = wizard
            };
        }

        // The production catalogue model aliases each entry's effects; the
        // workspace consumes exactly those, as BuffPlannerUiRoot does.
        private static CastingWorkspaceInputs ShadowCloneInputs(ShadowCloneParty party)
        {
            var model = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("shadow-clone"),
                party.Snapshot, new ActiveEffectSnapshot(null), party.Effects, party.Options,
                ignored => { });
            return new CastingWorkspaceInputs(party.Snapshot, party.Options,
                model.EffectsBySource, new CastEnhancementSnapshot[0]);
        }

        private static string MirrorImageSourceId()
        {
            return EffectAggregateIdentity.For(ShadowCloneShape(MirrorImageGuid),
                MirrorImageAbility.Canonical);
        }

        private static CastingWorkspaceSession ShadowCloneSession(string root, string name)
        {
            string dir = Path.Combine(root, "shadow-clone-" + name);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            return new CastingWorkspaceSession(dir, "campaign:shadow-clone");
        }

        private static void TestShadowCloneOwnEntry()
        {
            ShadowCloneParty party = BuildShadowCloneParty(true);
            CastingWorkspaceInputs inputs = ShadowCloneInputs(party);
            var session = new CastingWorkspaceSession(Path.Combine(Path.GetTempPath(),
                "kbp-shadow-clone-entry-" + Guid.NewGuid().ToString("N")), "campaign:shadow-clone");
            CastingGraphView view = session.BuildGraph(inputs);
            CastingGraphCatalogueEntry clone = view.Catalogue.SingleOrDefault(
                entry => entry.Label == "Shadow Clone");
            Expect(clone != null, "Shadow Clone is not in the buff list: " +
                string.Join(", ", view.Catalogue.Select(entry => entry.Label).ToArray()));
            Expect(clone.SourceId == "ability|" + ShadowCloneGuid,
                "Shadow Clone has no identity of its own: " + clone.SourceId);
            Expect(clone.Source.SourceKinds.SequenceEqual(new[] { SourceKind.AbilityResource }),
                "Shadow Clone is not an ability entry");
            Expect(clone.Source.IconAbility != null && clone.Source.IconAbility.Equals(ShadowCloneAbility),
                "Shadow Clone does not use its own ability's icon");
            Expect(clone.Source.Description == party.Ninja.Description,
                "Shadow Clone does not show its own description");
            Expect(WorkspaceSourceLabels.MatchesCategory(clone.Source, PlannerSourceCategory.Abilities) &&
                !WorkspaceSourceLabels.MatchesCategory(clone.Source, PlannerSourceCategory.Spells),
                "Shadow Clone is not under Abilities only");
            Expect(WorkspaceSourceLabels.Matches(clone.Source, "shadow clone"),
                "searching for Shadow Clone does not find it");
            // The wizard spell keeps the identity 0.4.1 stored for it.
            CastingGraphCatalogueEntry mirror = view.Catalogue.SingleOrDefault(
                entry => entry.Label == "Mirror Image");
            Expect(mirror != null && mirror.SourceId == MirrorImageSourceId(),
                "Mirror Image lost its saved identity");
            Expect(!WorkspaceSourceLabels.Matches(mirror.Source, "shadow"),
                "Mirror Image answers a search for Shadow Clone");
            // Only the Ninja casts Shadow Clone; only the wizard casts Mirror Image.
            session.SelectGraphBuff(clone.SourceId, inputs);
            view = session.BuildGraph(inputs);
            CastingGraphCasterNode ninja = view.CasterById("unit-ninja");
            Expect(ninja != null && ninja.Sources.Count == 1 &&
                ninja.Sources[0].ProviderKey == party.Ninja.Key.Canonical &&
                ninja.Sources[0].PoolKey == NinjaKiPool,
                "the Ninja's own Shadow Clone source is not offered");
            CastingGraphCasterNode wizard = view.CasterById("unit-wizard");
            Expect(wizard == null || wizard.Sources.Count == 0,
                "the wizard's Mirror Image was offered as Shadow Clone");
            session.SelectGraphBuff(mirror.SourceId, inputs);
            view = session.BuildGraph(inputs);
            ninja = view.CasterById("unit-ninja");
            Expect(ninja == null || ninja.Sources.Count == 0,
                "Shadow Clone was offered as Mirror Image");
            Expect(view.CasterById("unit-wizard").Sources.Count == 1,
                "the wizard's Mirror Image source is missing");
        }

        private static void TestShadowCloneAuthorsWithKi(string root)
        {
            ShadowCloneParty party = BuildShadowCloneParty(true);
            CastingWorkspaceInputs inputs = ShadowCloneInputs(party);
            CastingWorkspaceSession session = ShadowCloneSession(root, "author");
            string source = "ability|" + ShadowCloneGuid;
            string castingId = GraphAdd(session, inputs, source, "unit-ninja",
                party.Ninja.Key.Canonical, "unit-ninja");
            PlannedCasting casting = session.Document.Castings.Single(value => value.CastingId == castingId);
            Expect(casting.SourceId == source && casting.Ability.Equals(ShadowCloneAbility) &&
                casting.CasterUnitId == "unit-ninja" && casting.SpellbookGuid == null &&
                casting.DirectTargetUnitId == "unit-ninja",
                "the casting does not name the Ninja's exact Shadow Clone on the Ninja");
            // Personal: no other recipient is legal (nothing is invented).
            CastingGraphEditResult other = session.AddGraphCasting("unit-wizard", inputs);
            Expect(!other.Applied || session.Document.Castings.All(value =>
                    value.DirectTargetUnitId != "unit-wizard"),
                "Shadow Clone was authored on someone other than its owner");
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(session.Document,
                inputs.Snapshot, inputs.ProviderOptions, inputs.EffectsBySource,
                inputs.Enhancements, null, null, false, null, true);
            ResolvedCasting resolved = plan.CastingById(castingId);
            Expect(resolved.Readiness == ResolvedCastingReadiness.Ready,
                "Shadow Clone is not ready: " + string.Join(",", resolved.ReadinessReasons.ToArray()));
            Expect(resolved.Provider.Equals(party.Ninja.Key) && resolved.Cost.Count == 1 &&
                resolved.Cost[0].PoolKey == NinjaKiPool && resolved.Cost[0].Units == 1 &&
                !resolved.Cost[0].Unlimited,
                "Shadow Clone is not priced at one Ninja ki point");
            // The ordinary (strict Instant) run reaches the boundary with one
            // exact step for that provider; the disabled boundary refuses it.
            WorkspaceApplyResult applied = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            Expect(applied.GateDecision != null && applied.GateDecision.Allowed &&
                applied.Projection != null && applied.Projection.Converted,
                "the Shadow Clone run was refused before submission: " + applied.ReviewReason);
            Expect(applied.Projection.Plan.Steps.Count == 1 &&
                applied.Projection.Plan.Steps[0].Provider.Equals(party.Ninja.Key) &&
                applied.Projection.Plan.Steps[0].SourceId == source,
                "the projection is not one exact Shadow Clone step");
            Expect(applied.Dispatch != null && !applied.Dispatch.Submitted &&
                applied.Dispatch.Reason.StartsWith("native-submission-disabled", StringComparison.Ordinal),
                "the fixture boundary did not refuse the submission");
        }

        private static void TestShadowCloneExhaustedKi(string root)
        {
            ShadowCloneParty ready = BuildShadowCloneParty(true);
            CastingWorkspaceInputs readyInputs = ShadowCloneInputs(ready);
            CastingWorkspaceSession session = ShadowCloneSession(root, "exhausted");
            string source = "ability|" + ShadowCloneGuid;
            string castingId = GraphAdd(session, readyInputs, source, "unit-ninja",
                ready.Ninja.Key.Canonical, "unit-ninja");
            ShadowCloneParty spent = BuildShadowCloneParty(true, 0);
            CastingWorkspaceInputs spentInputs = ShadowCloneInputs(spent);
            CastingGraphView view = session.BuildGraph(spentInputs);
            Expect(view.Catalogue.Any(entry => entry.SourceId == source && entry.Label == "Shadow Clone"),
                "exhausted ki erased Shadow Clone from the buff list");
            ResolvedCasting resolved = new ExplicitCastingCompiler().Compile(session.Document,
                spentInputs.Snapshot, spentInputs.ProviderOptions, spentInputs.EffectsBySource,
                spentInputs.Enhancements, null, null, false, null, true).CastingById(castingId);
            Expect(resolved != null && resolved.Readiness != ResolvedCastingReadiness.Ready,
                "a Shadow Clone with no ki left is reported ready");
            WorkspaceApplyResult applied = session.Apply(CastingApplyMode.Ordinary, "long", spentInputs);
            Expect(!applied.Allowed && applied.BlockingCastings.Any(value => value.CastingId == castingId),
                "the exhausted casting does not block honestly");
            Expect(session.Document.Castings.Any(value => value.CastingId == castingId),
                "exhausted ki removed the saved Shadow Clone casting");
        }

        private static void TestShadowCloneAbsent()
        {
            CastingWorkspaceInputs withNinja = ShadowCloneInputs(BuildShadowCloneParty(true));
            CastingWorkspaceInputs without = ShadowCloneInputs(BuildShadowCloneParty(false));
            var session = new CastingWorkspaceSession(Path.Combine(Path.GetTempPath(),
                "kbp-shadow-clone-absent-" + Guid.NewGuid().ToString("N")), "campaign:shadow-clone");
            CastingGraphView view = session.BuildGraph(without);
            Expect(view.Catalogue.All(entry => entry.Label != "Shadow Clone" &&
                    !entry.SourceId.StartsWith("ability|", StringComparison.Ordinal)),
                "Shadow Clone was listed without its owner or Call of the Wild");
            CastingGraphView full = session.BuildGraph(withNinja);
            Expect(view.Catalogue.Single(entry => entry.Label == "Mirror Image").SourceId ==
                full.Catalogue.Single(entry => entry.Label == "Mirror Image").SourceId,
                "the optional ability changed Mirror Image's identity");
        }

        // A plan that holds a Shadow Clone casting under the shared effect
        // identity (a classic import made one) loads under the ability's
        // own entry; ids, order and intent stay; the stored file is
        // archived byte-exact once before the first write; reloading is a
        // no-op.
        private static void TestShadowCloneSavedIdentityMigrates(string root)
        {
            string dir = Path.Combine(root, "shadow-clone-migrate");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            string shared = MirrorImageSourceId();
            var stale = new PlannedCasting("cast-clone", "long", 0, shared, ShadowCloneAbility,
                "unit-ninja", null, CastingTargetMode.DirectTarget, "unit-ninja", null, null, null,
                null, ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            var spell = new PlannedCasting("cast-mirror", "long", 1, shared, MirrorImageAbility,
                "unit-wizard", "wizard-book", CastingTargetMode.DirectTarget, "unit-wizard", null, null,
                null, null, ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            var document = new CastingPlanDocument("campaign:shadow-clone", new[]
            {
                new RoutineDefinition("long", "Long"), new RoutineDefinition("important", "Important"),
                new RoutineDefinition("short", "Short")
            }, new[] { stale, spell });
            var repository = new CastingPlanRepository(dir);
            repository.Save(CastingPlanProfile.FromDocument(document, UiProfile.Default(),
                ExecutionProfile.Default()));
            string primary = repository.GetProfilePath("campaign:shadow-clone");
            byte[] storedBefore = File.ReadAllBytes(primary);

            var session = new CastingWorkspaceSession(dir, "campaign:shadow-clone");
            Expect(session.MigratedSourceIdentityCount == 1, "the stale identity was not migrated");
            PlannedCasting clone = session.Document.Castings.Single(value => value.CastingId == "cast-clone");
            PlannedCasting mirror = session.Document.Castings.Single(value => value.CastingId == "cast-mirror");
            Expect(clone.SourceId == "ability|" + ShadowCloneGuid && clone.Order == 0 &&
                clone.Ability.Equals(ShadowCloneAbility) && clone.CasterUnitId == "unit-ninja" &&
                clone.DirectTargetUnitId == "unit-ninja" && clone.State == CastingAuthoringState.Ready,
                "the migrated casting changed more than its catalogue identity");
            Expect(mirror.SourceId == shared && mirror.Order == 1,
                "the spell casting's aggregate identity was changed");
            Expect(!session.IsDirty, "the deterministic migration made the session dirty");
            CastingWorkspaceInputs inputs = ShadowCloneInputs(BuildShadowCloneParty(true));
            ResolvedCasting resolved = new ExplicitCastingCompiler().Compile(session.Document,
                inputs.Snapshot, inputs.ProviderOptions, inputs.EffectsBySource,
                inputs.Enhancements, null, null, false, null, true).CastingById("cast-clone");
            Expect(resolved.Readiness == ResolvedCastingReadiness.Ready,
                "the migrated Shadow Clone casting does not compile ready: " +
                string.Join(",", resolved.ReadinessReasons.ToArray()));
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(view.Catalogue.Single(entry => entry.SourceId == "ability|" + ShadowCloneGuid).RoutineCount == 1,
                "the migrated casting is not counted under Shadow Clone");
            // The first write archives the exact stored bytes once.
            Expect(File.ReadAllBytes(primary).SequenceEqual(storedBefore),
                "loading alone rewrote the stored plan");
            var extra = new PlannedCasting("cast-extra", "short", 0, MirrorImageSourceId(),
                MirrorImageAbility, "unit-wizard", "wizard-book", CastingTargetMode.DirectTarget,
                "unit-wizard", null, null, null, null, ExistingEffectPolicy.SkipAlreadyActive, null,
                CastingAuthoringState.Ready, null);
            Expect(session.AddCastingForRuntime(extra).Applied, "the edit was refused");
            Expect(session.SourceIdentityArchivePath != null &&
                File.ReadAllBytes(session.SourceIdentityArchivePath).SequenceEqual(storedBefore),
                "the pre-migration plan was not archived byte-exact");
            var reloaded = new CastingWorkspaceSession(dir, "campaign:shadow-clone");
            Expect(reloaded.MigratedSourceIdentityCount == 0 &&
                reloaded.Document.Castings.Single(value => value.CastingId == "cast-clone").SourceId ==
                    "ability|" + ShadowCloneGuid,
                "the migration was not durable or not idempotent");
        }

        // PR #7 review R1: the one-time pre-migration archive survives a
        // recovery handoff. The recovered document is already normalized,
        // so the replacement session cannot rediscover the obligation from
        // it; it must arrive with the captured intent.
        private static void TestShadowCloneArchiveObligationRecovery(string root)
        {
            const string campaign = "campaign:shadow-clone-recovery";
            string dir = Path.Combine(root, "shadow-clone-archive-recovery");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            string shared = MirrorImageSourceId();
            var stale = new PlannedCasting("cast-clone", "long", 0, shared, ShadowCloneAbility,
                "unit-ninja", null, CastingTargetMode.DirectTarget, "unit-ninja", null, null, null,
                null, ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            var document = new CastingPlanDocument(campaign, new[]
            {
                new RoutineDefinition("long", "Long"), new RoutineDefinition("important", "Important"),
                new RoutineDefinition("short", "Short")
            }, new[] { stale });
            var repository = new CastingPlanRepository(dir);
            repository.Save(CastingPlanProfile.FromDocument(document, UiProfile.Default(),
                ExecutionProfile.Default()));
            string primary = repository.GetProfilePath(campaign);
            byte[] original = File.ReadAllBytes(primary);
            string archive = Path.Combine(Path.GetDirectoryName(primary),
                Path.GetFileNameWithoutExtension(primary) + CastingPlanRepository.SourceIdentityArchiveLabel + ".orig");
            Directory.CreateDirectory(archive);
            var store = new CastingWorkspaceRecoveryStore();
            var messages = new List<string>();
            Func<string, CastingWorkspaceSession> factory = id => store.Adopt(dir, id,
                pending => new CastingWorkspaceSession(dir, id, new DisabledCastingDispatchBoundary(), null, null, pending),
                () => new CastingWorkspaceSession(dir, id, new DisabledCastingDispatchBoundary()));
            var owner = new CastingSessionOwner(factory, store, messages.Add);
            CastingWorkspaceSession session;
            Expect(owner.Ensure(campaign, out session) == null && session.MigratedSourceIdentityCount == 1,
                "the stale identity was not migrated on load");
            var extra = new PlannedCasting("cast-extra", "short", 0, shared, MirrorImageAbility,
                "unit-wizard", "wizard-book", CastingTargetMode.DirectTarget, "unit-wizard", null, null, null,
                null, ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            Expect(session.AddCastingForRuntime(extra).Applied, "the edit was refused");
            Expect(session.IsDirty && File.ReadAllBytes(primary).SequenceEqual(original),
                "the migrated plan was saved without its archive");
            owner.Release("root-teardown");
            Expect(store.HasPending(dir, campaign), "the unarchived intent was not handed to recovery");
            PendingSessionRecovery captured = store.Peek(dir, campaign);
            Expect(captured.ModPath == dir && captured.CampaignId == campaign &&
                captured.ArchiveObligations.SequenceEqual(new[] { CastingPlanRepository.SourceIdentityArchiveLabel }),
                "the captured intent does not carry its archive obligation: " +
                    string.Join(",", captured.ArchiveObligations.ToArray()));
            session = null;
            var replacement = new CastingSessionOwner(factory, store, messages.Add);
            CastingWorkspaceSession revived;
            Expect(replacement.Ensure(campaign, out revived) == null, "the replacement owner refused");
            Expect(File.ReadAllBytes(primary).SequenceEqual(original),
                "recovery wrote the migrated plan without the mandatory archive");
            Expect(revived.PendingArchiveObligations.SequenceEqual(
                    new[] { CastingPlanRepository.SourceIdentityArchiveLabel }) && revived.MigratedSourceIdentityCount == 0,
                "the replacement session does not owe the archive");
            Expect(revived.IsDirty && revived.AutosaveStatus.StartsWith("save-failed", StringComparison.Ordinal) &&
                revived.Document.Castings.Single(value => value.CastingId == "cast-clone").SourceId ==
                    "ability|" + ShadowCloneGuid,
                "the recovered unarchived intent looks durable or lost its migration: " + revived.AutosaveStatus);
            Expect(!revived.RetryFailedSave() && File.ReadAllBytes(primary).SequenceEqual(original),
                "a retry under the obstruction wrote without the archive");
            WorkspaceApplyResult blocked = revived.Apply(CastingApplyMode.Ordinary, "long",
                ShadowCloneInputs(BuildShadowCloneParty(true)));
            Expect(!blocked.Allowed && blocked.Dispatch == null &&
                blocked.ReviewReason.StartsWith("persistence-failed", StringComparison.Ordinal),
                "an undurable recovered plan was let through: " + blocked.ReviewReason);
            Directory.Delete(archive);
            Expect(revived.RetryFailedSave() && revived.PendingArchiveObligations.Count == 0,
                "the healed retry did not save");
            Expect(File.ReadAllBytes(archive).SequenceEqual(original),
                "the archive is not the exact pre-migration plan");
            var reloaded = new CastingWorkspaceSession(dir, campaign);
            Expect(reloaded.MigratedSourceIdentityCount == 0 && reloaded.Document.Castings.Count == 2 &&
                reloaded.Document.Castings.Single(value => value.CastingId == "cast-clone").SourceId ==
                    "ability|" + ShadowCloneGuid,
                "the migrated plan is not durable after the archive");
        }

        private static void TestClassAbilityIdentityRule()
        {
            EffectExpression mirror = ShadowCloneShape(MirrorImageGuid);
            EffectExpression copy = ShadowCloneShape("copyofmirrorimage000000000000001");
            var copyAbility = new AbilityKey("copyofmirrorimage000000000000001", string.Empty, 0,
                SourceKind.Spellbook, string.Empty);
            Expect(CatalogSourceIdentity.For(MirrorImageAbility, mirror) ==
                    CatalogSourceIdentity.For(copyAbility, copy),
                "two spells with one effect no longer share their entry");
            Expect(CatalogSourceIdentity.For(ShadowCloneAbility, ShadowCloneShape(ShadowCloneGuid)) ==
                    "ability|" + ShadowCloneGuid,
                "a class ability does not keep its own identity");
            var factAbility = new AbilityKey(ShadowCloneGuid, string.Empty, 0, SourceKind.Fact, string.Empty);
            Expect(CatalogSourceIdentity.For(factAbility, null) == "ability|" + ShadowCloneGuid &&
                CatalogSourceIdentity.MatchesAbility("ability|" + ShadowCloneGuid, factAbility) &&
                !CatalogSourceIdentity.MatchesAbility("ability|" + ShadowCloneGuid, MirrorImageAbility),
                "a free fact ability does not share its blueprint's own identity");
            var variant = new AbilityKey("parent-guid", "child-guid", 0, SourceKind.AbilityResource, string.Empty);
            Expect(CatalogSourceIdentity.For(variant, mirror) == "variant|parent-guid|child-guid",
                "a concrete variant changed identity");
        }

        // Classic planning (RoutinePlanService) resolves an entry's members
        // by the same identity: the Mirror Image entry never spends ki.
        // PR #7 review R2: a Classic profile saved before 0.4.2 holds the
        // Ninja's Shadow Clone under the old shared effect aggregate (Mirror
        // Image's id). Its persisted exact ability decides where it belongs;
        // the spell's entry, which still carries that aggregate, must never
        // capture it. The child casting assignment keeps its pins, target,
        // enhancements and order; the assignment its policy and markers.
        private const string LegacyCloneChild = "ca-clone";
        private const string LegacyCloneSecondChild = "ca-clone-2";

        private static BuffPlannerProfile LegacyClassicProfile(string campaign, bool shadowClone, string ninjaProvider)
        {
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(campaign);
            AbilityKey ability = shadowClone ? ShadowCloneAbility : MirrorImageAbility;
            profile.Routines.Single(routine => routine.RoutineId == "long").Assignments.Add(new SourceAssignmentProfile
            {
                SourceId = MirrorImageSourceId(),
                Ability = AbilityKeyProfile.FromKey(ability),
                ExistingEffectPolicy = ExistingEffectPolicy.Overwrite,
                IgnoredPresenceMarkers = new List<string> { "marker-legacy" },
                CastingAssignments = new List<CastingAssignmentProfile>
                {
                    new CastingAssignmentProfile
                    {
                        AssignmentId = LegacyCloneSecondChild, Order = 7,
                        CasterUnitId = shadowClone ? "unit-ninja" : "unit-wizard",
                        SpellbookGuid = shadowClone ? null : "wizard-book",
                        ProviderKey = ninjaProvider,
                        TargetUnitIds = new List<string> { shadowClone ? "unit-ninja" : "unit-wizard" },
                        Enhancements = new List<EnhancementSelectionProfile>()
                    },
                    new CastingAssignmentProfile
                    {
                        AssignmentId = LegacyCloneChild, Order = 3,
                        CasterUnitId = shadowClone ? "unit-ninja" : "unit-wizard",
                        SpellbookGuid = shadowClone ? null : "wizard-book",
                        ProviderKey = ninjaProvider,
                        TargetUnitIds = new List<string> { shadowClone ? "unit-ninja" : "unit-wizard" },
                        Enhancements = new List<EnhancementSelectionProfile>()
                    }
                }
            });
            return profile;
        }

        private static SourceAssignmentProfile LongAssignment(BuffPlannerProfile profile)
        {
            return profile.Routines.Single(routine => routine.RoutineId == "long").Assignments.Single();
        }

        // Child identities, pins, targets and enhancements stay; their
        // relative order stays (a changed routine is renumbered densely).
        private static void ExpectLegacyChildKept(SourceAssignmentProfile assignment, string caster, string book,
            string provider, string target)
        {
            List<CastingAssignmentProfile> children = assignment.CastingAssignments
                .OrderBy(child => child.Order).ToList();
            Expect(children.Select(child => child.AssignmentId)
                    .SequenceEqual(new[] { LegacyCloneChild, LegacyCloneSecondChild }) &&
                children.All(child => child.CasterUnitId == caster && child.SpellbookGuid == book &&
                    child.ProviderKey == provider && child.TargetUnitIds.SequenceEqual(new[] { target }) &&
                    child.Enhancements.Count == 0) &&
                assignment.ExistingEffectPolicy == ExistingEffectPolicy.Overwrite &&
                assignment.IgnoredPresenceMarkers.SequenceEqual(new[] { "marker-legacy" }),
                "the legacy assignment's other intent was not preserved");
        }

        private static RoutinePlanResult PlanLong(BuffPlannerProfile profile, ShadowCloneParty party,
            PlannerSetupModel model)
        {
            return new RoutinePlanService().Plan(profile, "long", party.Snapshot, new ActiveEffectSnapshot(null),
                model.EffectsBySource.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                party.Options);
        }

        // 1. Ninja and wizard present: Shadow Clone moves to its own entry,
        // stays Shadow Clone, and plans on the Ninja's own ki provider.
        private static void TestClassicLegacyShadowCloneRebinds()
        {
            ShadowCloneParty party = BuildShadowCloneParty(true);
            BuffPlannerProfile profile = LegacyClassicProfile("legacy-clone-1", true, party.Ninja.Key.Canonical);
            var model = new PlannerSetupModel(profile, party.Snapshot, new ActiveEffectSnapshot(null),
                party.Effects, party.Options, ignored => { });
            SourceAssignmentProfile assignment = LongAssignment(profile);
            Expect(assignment.SourceId == "ability|" + ShadowCloneGuid &&
                assignment.Ability.ToKey().Equals(ShadowCloneAbility) && model.AssignmentMigrationApplied,
                "the legacy Shadow Clone assignment was not rebound to its own ability: " + assignment.SourceId +
                    " / " + assignment.Ability.ToKey().Canonical);
            ExpectLegacyChildKept(assignment, "unit-ninja", null, party.Ninja.Key.Canonical, "unit-ninja");
            RoutinePlanResult plan = PlanLong(profile, party, model);
            Expect(plan.Plan.Steps.Count >= 1 && plan.Plan.Steps.All(step => step.Provider.Equals(party.Ninja.Key)),
                "the rebound Shadow Clone did not plan exactly the Ninja's own ability");
        }

        // 2. Only Mirror Image available: the assignment keeps its exact
        // ability under its own identity, unresolved; it is never rewritten
        // to the spell and never plans through the wizard's spell.
        private static void TestClassicLegacyShadowCloneWithoutNinja()
        {
            ShadowCloneParty party = BuildShadowCloneParty(false);
            BuffPlannerProfile profile = LegacyClassicProfile("legacy-clone-2", true,
                "unit-ninja||" + ShadowCloneAbility.Canonical + "|");
            string pinned = LongAssignment(profile).CastingAssignments.First().ProviderKey;
            var model = new PlannerSetupModel(profile, party.Snapshot, new ActiveEffectSnapshot(null),
                party.Effects, party.Options, ignored => { });
            SourceAssignmentProfile assignment = LongAssignment(profile);
            Expect(assignment.Ability.ToKey().Equals(ShadowCloneAbility) &&
                assignment.SourceId == "ability|" + ShadowCloneGuid &&
                model.Sources.All(source => source.SourceId != assignment.SourceId),
                "the unavailable Shadow Clone assignment was rewritten or resolved to another entry: " +
                    assignment.SourceId + " / " + assignment.Ability.ToKey().Canonical);
            ExpectLegacyChildKept(assignment, "unit-ninja", null, pinned, "unit-ninja");
            RoutinePlanResult plan = PlanLong(profile, party, model);
            Expect(!plan.Plan.Steps.Any(step => step.Provider.Equals(party.Wizard.Key)),
                "the Shadow Clone assignment silently planned the wizard's Mirror Image");
        }

        // 3. An old Mirror Image assignment with both providers present stays
        // the spell aggregate and never acquires the Ninja's ki provider.
        private static void TestClassicLegacyMirrorImageStays()
        {
            ShadowCloneParty party = BuildShadowCloneParty(true);
            BuffPlannerProfile profile = LegacyClassicProfile("legacy-mirror", false, party.Wizard.Key.Canonical);
            var model = new PlannerSetupModel(profile, party.Snapshot, new ActiveEffectSnapshot(null),
                party.Effects, party.Options, ignored => { });
            SourceAssignmentProfile assignment = LongAssignment(profile);
            Expect(assignment.SourceId == MirrorImageSourceId() &&
                assignment.Ability.ToKey().Equals(MirrorImageAbility),
                "the Mirror Image assignment left the spell aggregate: " + assignment.SourceId);
            ExpectLegacyChildKept(assignment, "unit-wizard", "wizard-book", party.Wizard.Key.Canonical, "unit-wizard");
            RoutinePlanResult plan = PlanLong(profile, party, model);
            Expect(plan.Plan.Steps.Count >= 1 && plan.Plan.Steps.All(step => step.Provider.Equals(party.Wizard.Key)),
                "the Mirror Image assignment did not plan exactly the wizard's spell");
        }

        // 4. The rebinding saves through the real profile repository, and a
        // reopened profile is unchanged and needs no second migration.
        private static void TestClassicLegacyRebindingIdempotent(string root)
        {
            string dir = Path.Combine(root, "legacy-clone-reopen");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var repository = new ProfileRepository(dir);
            ShadowCloneParty party = BuildShadowCloneParty(true);
            BuffPlannerProfile profile = LegacyClassicProfile("legacy-clone-reopen", true, party.Ninja.Key.Canonical);
            repository.Save(profile);
            BuffPlannerProfile loaded = repository.Load("legacy-clone-reopen").Profile;
            int saves = 0;
            var first = new PlannerSetupModel(loaded, party.Snapshot, new ActiveEffectSnapshot(null),
                party.Effects, party.Options, value => { saves++; repository.Save(value); });
            Expect(first.AssignmentMigrationApplied && saves == 1, "the rebinding was not saved once");
            string migrated = Newtonsoft.Json.JsonConvert.SerializeObject(LongAssignment(loaded));
            BuffPlannerProfile reopened = repository.Load("legacy-clone-reopen").Profile;
            var second = new PlannerSetupModel(reopened, party.Snapshot, new ActiveEffectSnapshot(null),
                party.Effects, party.Options, value => { saves++; repository.Save(value); });
            Expect(!second.AssignmentMigrationApplied && saves == 1 &&
                Newtonsoft.Json.JsonConvert.SerializeObject(LongAssignment(reopened)) == migrated &&
                LongAssignment(reopened).SourceId == "ability|" + ShadowCloneGuid,
                "the reopened profile was migrated again or changed");
            ExpectLegacyChildKept(LongAssignment(reopened), "unit-ninja", null, party.Ninja.Key.Canonical, "unit-ninja");
        }

        private static void TestClassicKeepsShadowCloneSeparate()
        {
            ShadowCloneParty party = BuildShadowCloneParty(true);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("shadow-clone-classic");
            var model = new PlannerSetupModel(profile, party.Snapshot, new ActiveEffectSnapshot(null),
                party.Effects, party.Options, ignored => { });
            Expect(model.Sources.Count == 2 &&
                model.Sources.Any(source => source.SourceId == "ability|" + ShadowCloneGuid &&
                    source.DisplayName == "Shadow Clone") &&
                model.Sources.Any(source => source.SourceId == MirrorImageSourceId() &&
                    source.Abilities.Count == 1),
                "the classic catalogue pooled Shadow Clone with Mirror Image");
            // Under the shared identity the Mirror Image card could target
            // the Ninja through his Shadow Clone; now only the wizard's own
            // personal spell answers for it.
            model.SelectSource(MirrorImageSourceId());
            bool refused = false;
            try { model.ToggleTarget("long", "unit-ninja"); }
            catch (InvalidOperationException) { refused = true; }
            Expect(refused, "the Mirror Image card still targets the Ninja through Shadow Clone");
            model.SelectSource("ability|" + ShadowCloneGuid);
            model.ToggleTarget("long", "unit-ninja");
            RoutinePlanResult plan = new RoutinePlanService().Plan(profile, "long", party.Snapshot,
                new ActiveEffectSnapshot(null),
                model.EffectsBySource.ToDictionary(pair => pair.Key, pair => pair.Value,
                    StringComparer.Ordinal), party.Options);
            Expect(plan.Plan.Steps.Count == 1 && plan.Plan.Steps[0].Provider.Equals(party.Ninja.Key),
                "the Shadow Clone card did not plan exactly the Ninja's own ability");
        }
    }
}
