using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // Missing-beneficial-buffs mission §4. Every blueprint below is the
        // native Kingmaker 2.1.7b blueprint, materialized from the dump as a
        // detached Assembly-CSharp object (NativeBlueprintDump), and runs the
        // production adapter -> scanner -> exporter facts -> classifier ->
        // live discovery path (KingmakerBuffSourceDiscovery, the exact gate of
        // the castable catalogue), then the production planning compiler,
        // step converter and confirmation judgement. The frozen baseline
        // classifier (dd3e997) judges the same facts, so each repair is shown
        // failing on the baseline and passing now.
        private const string MbbGoodHope = "a5e23522eda32dc45801e32c05dc9f96";
        private const string MbbGoodHopeBuff = "85af9f0c5d29e5e4fa2e75ca70442487";
        private const string MbbCrushingDespairBuff = "36897b36a4bd63146b1d8509a17fc5ad";
        private const string MbbGoodHopeTrollLair = "8b9916170be016a49a94cc41634b3a24";
        private const string MbbHaste = "486eaff58293f6441a5c2759c4872f98";
        private const string MbbHasteBuff = "03464790f40c3c24aa684b57155f3280";
        private const string MbbFirebelly = "b065231094a21d14dbf1c3832f776871";
        private const string MbbFirebellyBuff = "7c33de68880aa444bbb916271b653016";
        private const string MbbFirebellyBreath = "5e5b663f988ece84b9346f6d7d541e66";
        private const string MbbHideousLaughter = "fd4d9fd7f87575d47aafe2a64a6e2d8d";
        private const string MbbHideousLaughterBuff = "4b1f07a71a982824988d7f48cd49f3f8";
        private const string MbbCallLightning = "2a9ef0e0b5822a24d88b16673a267456";
        private const string MbbCallLightningBuff = "dd91a3c3df275984592edcadb8e90749";
        private const string MbbDazingTouch = "84cd24a110af59140b066bc2c69619bd";
        private const string MbbDazeBuff = "9934fedff1b14994ea90205d189c8759";
        private const string MbbTreatAffliction = "4843cb4c23951f54290c5149a4907f54";
        private const string MbbTreatAfflictionCooldown = "b89dcf508d48da74f8dd5234e6a5eb84";
        private const string MbbIrresistibleDanceCast = "7733b3550b4f03002c59bc590e07d9cc";

        // Element -> (selection ability, its resistance buff).
        private static readonly string[][] MbbAbjuration =
        {
            new[] { "Acid", "9b415e09847398644ad6e57a9e3ab06a", "b87a0e8a961c63a44a952822fe975edb" },
            new[] { "Cold", "fb1afa622509aee4db0d859e21090709", "90ce7dac4b9001e449eb44861397a65f" },
            new[] { "Electricity", "6d29b28765958f144b00cae5e1e13e0c", "453201f110f49714fb1d713c1f7bc06d" },
            new[] { "Fire", "1ecf7a23be0faa3459b8e318452a0a70", "b9ce1623447547946b8c58efd069f7c4" },
            new[] { "Sonic", "8c123d8750aa15a4ba732666db96768e", "63e2a6323c2bbff40a87fcd522778032" }
        };

        // Point-capable group spells: ability, buff, native ally radius (ft).
        // The first four are the mission's; Enlarge and Reduce Person, Mass
        // are the same structural shape found by the candidate audit.
        private static readonly string[][] MbbPointGroupSpells =
        {
            new[] { "Blessing of Luck and Resolve, Mass", "462c21cebf7820c40a87f5e4d03e17cf", "4d2564805938c6449b2dde17a24a66f4", "30" },
            new[] { "Feather Step, Mass", "d219494150ac1f24f9ce14a3d4f66d26", "c748cceadcab2614b942e56ff257cfbc", "15" },
            new[] { "Firebrand", "98734a2665c18cd4db71878b0532024a", "c6cc1c5356db4674dbd2be20ea205c86", "30" },
            new[] { "Heroic Invocation", "43740dab07286fe4aa00a6ee104ce7c1", "fd8fb2c1d622556468a04bea949eb7da", "30" },
            new[] { "Enlarge Person, Mass", "66dc49bf154863148bd217287079245e", "4f139d125bb602f48bfaec3d3e1937cb", "15" },
            new[] { "Reduce Person, Mass", "2427f2e3ca22ae54ea7337bbab555b16", "b0793973c61a19744a8630468e8f4174", "15" }
        };

        private static void RunMissingBeneficialBuffsTests(string root)
        {
            Run("missing-beneficial-buffs-native-fixtures-retain-misleading-flags", TestMbbFixturesRetainFlags);
            Run("missing-beneficial-buffs-good-hope-and-haste-reach-the-castable-catalogue", TestMbbGoodHopeDiscovery);
            Run("missing-beneficial-buffs-good-hope-area-plan-is-one-native-cast", TestMbbGoodHopeAreaPlan);
            Run("missing-beneficial-buffs-good-hope-existing-effect-and-crushing-despair", TestMbbGoodHopeExistingAndDespair);
            Run("missing-beneficial-buffs-firebelly-included-breath-excluded", TestMbbFirebelly);
            Run("missing-beneficial-buffs-abjuration-selections-discoverable-and-personal", TestMbbAbjurationDiscovery);
            Run("missing-beneficial-buffs-abjuration-selections-replace-not-stack", TestMbbAbjurationPlanning);
            Run("missing-beneficial-buffs-point-capable-group-spells-unit-anchored", TestMbbPointGroupSpells);
            Run("missing-beneficial-buffs-negative-controls-stay-excluded", TestMbbNegativeControls);
            Run("missing-beneficial-buffs-irresistible-dance-loaded-facts-stay-excluded", TestMbbIrresistibleDance);
            Run("missing-beneficial-buffs-native-target-anchor-contract", TestMbbTargetAnchorContract);
            Run("missing-beneficial-buffs-removal-conflict-reason-is-worded", TestMbbRemovalReasonText);
            Run("missing-beneficial-buffs-existing-metadata-overrides-preserved", TestMbbExistingOverridesPreserved);
        }

        // Freedom of Movement, True Seeing and Strength Surge keep their
        // existing evidence-backed overrides (harmful-enemy metadata on a
        // friendly delivery): live discovery still admits exactly the
        // override's buff, and the classifier alone still would not - the
        // repair neither replaces nor duplicates those exceptions.
        private static void TestMbbExistingOverridesPreserved()
        {
            var cases = new[]
            {
                new[] { "Freedom of Movement", "0087fc2d64b6095478bc7b8d7d512caf", "1533e782fca42b84ea370fc1dcbf4fc1" },
                new[] { "True Seeing", "4cf3d0fae3239ec478f51e86f49161cb", "09b4b69169304474296484c74aa12027" },
                new[] { "Strength Surge", "1d6364123e1f6a04c88313d83d3b70ee", "94dfcf5f3a72ce8478c8de5db69e752b" }
            };
            var dump = new NativeBlueprintDump(cases.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            foreach (string[] row in cases)
            {
                MbbDiscovery result = MbbDiscover(dump, row[1]);
                MbbExpect(result.Included && result.Reason.Contains(row[0]) &&
                    EffectApplicationLifetime.Leaves(result.Expression).Select(leaf => leaf.EffectId).Distinct()
                        .SequenceEqual(new[] { row[2] }),
                    row[0] + " lost its override inclusion: " + result.Reason);
                MbbExpect(result.Ability.CanTargetEnemies && result.Ability.EffectOnEnemy == AbilityEffectOnUnit.Harmful &&
                    result.Before.Disposition == result.Repaired.Disposition,
                    row[0] + "'s classifier-only judgement changed: " + result.Before.Reason + " -> " + result.Repaired.Reason);
            }
        }

        private sealed class MbbDiscovery
        {
            internal BlueprintAbility Ability;
            internal DiscoveryScanResult Scan;
            internal NativeCandidateAuditFacts Facts;
            internal NativeCandidateAuditDecision Repaired;
            internal NativeCandidateAuditDecision RepairedPre040;
            internal Baseline.NativeCandidateAuditDecision Before;
            internal Baseline.NativeCandidateAuditDecision BeforePre040;
            internal bool Included;
            internal EffectExpression Expression;
            internal string Reason;
        }

        private static EffectOverrideRegistry MbbOverrides()
        {
            return EffectOverrideRegistry.Load(Path.Combine(FindRepositoryRoot(), "src", "KingmakerBuffPlanner",
                "NativeEffectOverrides.json"));
        }

        private static MbbDiscovery MbbDiscover(NativeBlueprintDump dump, string guid)
        {
            var result = new MbbDiscovery { Ability = dump.Get<BlueprintAbility>(guid) };
            string stage = "adapt";
            try
            {
                result.Scan = new ActionGraphScanner().Scan(new KingmakerActionGraphAdapter().Adapt(result.Ability));
                stage = "effects";
                NativeEffectRecord[] effects = NativeCatalogExporter.GetEffects(result.Scan.Expression, dump.Lookup);
                stage = "facts";
                result.Facts = NativeCatalogExporter.AuditFacts(result.Ability, true, effects, result.Scan.Diagnostics);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(guid + " " + stage + ": " + exception);
            }
            result.Repaired = new NativeCandidateClassifier().Classify(result.Facts);
            result.RepairedPre040 = new NativeCandidateClassifier(NativeCandidateRuleSet.Pre040).Classify(result.Facts);
            result.Before = new Baseline.NativeCandidateClassifier().Classify(BaselineFacts(result.Facts));
            result.BeforePre040 = new Baseline.NativeCandidateClassifier(Baseline.NativeCandidateRuleSet.Pre040)
                .Classify(BaselineFacts(result.Facts));
            EffectExpression expression;
            string reason;
            result.Included = new KingmakerBuffSourceDiscovery(MbbOverrides(), dump.Lookup)
                .TryDiscover(result.Ability, out expression, out reason);
            result.Expression = expression;
            result.Reason = reason;
            return result;
        }

        private static void MbbExpect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        // Every node of type T, outermost first (not descending into a match).
        private static List<T> MbbFind<T>(EffectExpression expression) where T : EffectExpression
        {
            var found = new List<T>();
            var match = expression as T;
            if (match != null) { found.Add(match); return found; }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) foreach (EffectExpression child in sequence.Children) found.AddRange(MbbFind<T>(child));
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                found.AddRange(MbbFind<T>(conditional.WhenTrue));
                found.AddRange(MbbFind<T>(conditional.WhenFalse));
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) found.AddRange(MbbFind<T>(targeted.Child));
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null) found.AddRange(MbbFind<T>(reference.Child));
            return found;
        }

        private static IEnumerable<EffectLeafExpression> MbbLeaves(EffectExpression expression, EffectTarget? outer,
            List<EffectTarget> roles)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null)
            {
                roles.Add(EffectRecipientScope.Resolve(leaf.Target, outer));
                return new[] { leaf };
            }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.SelectMany(child => MbbLeaves(child, outer, roles)).ToList();
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return MbbLeaves(conditional.WhenTrue, outer, roles).Concat(MbbLeaves(conditional.WhenFalse, outer, roles)).ToList();
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) return MbbLeaves(targeted.Child, EffectRecipientScope.Resolve(targeted.Target, outer), roles);
            var reference = expression as ReferencedAbilityExpression;
            return reference == null ? new EffectLeafExpression[0] : MbbLeaves(reference.Child, outer, roles);
        }

        private static void TestMbbFixturesRetainFlags()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbFirebelly, MbbAbjuration[3][1], MbbPointGroupSpells[3][1]);
            BlueprintAbility goodHope = dump.Get<BlueprintAbility>(MbbGoodHope);
            BlueprintAbility firebelly = dump.Get<BlueprintAbility>(MbbFirebelly);
            BlueprintAbility fire = dump.Get<BlueprintAbility>(MbbAbjuration[3][1]);
            BlueprintAbility heroic = dump.Get<BlueprintAbility>(MbbPointGroupSpells[3][1]);
            // The misleading native metadata is retained, never normalized.
            MbbExpect(goodHope.AssetGuid == MbbGoodHope && goodHope.EffectOnAlly == AbilityEffectOnUnit.Harmful &&
                goodHope.EffectOnEnemy == AbilityEffectOnUnit.Harmful && goodHope.CanTargetSelf &&
                goodHope.CanTargetFriends && !goodHope.CanTargetEnemies && !goodHope.CanTargetPoint &&
                goodHope.Range == AbilityRange.Medium, "Good Hope fixture is not the native blueprint.");
            MbbExpect(firebelly.EffectOnAlly == AbilityEffectOnUnit.Harmful && firebelly.Range == AbilityRange.Personal &&
                !firebelly.CanTargetEnemies, "Firebelly fixture is not the native blueprint.");
            MbbExpect(fire.Range == AbilityRange.Personal && fire.CanTargetSelf && fire.CanTargetEnemies &&
                !fire.CanTargetFriends && fire.EffectOnEnemy == AbilityEffectOnUnit.Harmful,
                "Abjuration resistance fixture is not the native blueprint.");
            MbbExpect(heroic.CanTargetPoint && heroic.CanTargetSelf && heroic.CanTargetFriends && !heroic.CanTargetEnemies,
                "Heroic Invocation fixture is not the native blueprint.");
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(goodHope);
            MbbExpect(area != null && Math.Abs(area.Radius - 15f * 0.3048f) < 0.001f && area.SourceAbilityGuid == MbbGoodHope,
                "Good Hope's native 15-foot allied radius was not read: " + (area == null ? "none" : area.Radius.ToString("R")));
        }

        private static void TestMbbGoodHopeDiscovery()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff);
            MbbDiscovery hope = MbbDiscover(dump, MbbGoodHope);
            // Baseline reproduction: the blanket harmful-ally veto.
            MbbExpect(hope.Before.Disposition == "exclude" && hope.Before.ReasonCode == "harmful-ally-disposition" &&
                hope.BeforePre040.Disposition == "exclude",
                "The baseline no longer reproduces the Good Hope exclusion: " + hope.Before.Reason);
            MbbExpect(hope.Repaired.Disposition == "include" && hope.Repaired.ReasonCode == "valid-beneficial-party-effect" &&
                hope.RepairedPre040.Disposition == "include" &&
                hope.Repaired.Payloads.Select(e => e.EffectId).Distinct().SequenceEqual(new[] { MbbGoodHopeBuff }),
                "Good Hope is not a beneficial party buff: " + hope.Repaired.Reason);
            MbbExpect(hope.Included, "Live discovery refused Good Hope: " + hope.Reason);
            // The whole discovered expression is kept: the allied
            // AbilityTargetsAround area and the Crushing Despair branch.
            TargetedEffectExpression area = hope.Expression is ReferencedAbilityExpression
                ? MbbFind<TargetedEffectExpression>(hope.Expression).SingleOrDefault() : null;
            ConditionalEffectExpression branch = area == null ? null
                : MbbFind<ConditionalEffectExpression>(area.Child).SingleOrDefault();
            MbbExpect(area != null && area.Target == EffectTarget.AlliedAreaRecipients && branch != null &&
                branch.ConditionContract.Contains("ContextConditionHasBuff"),
                "Good Hope lost its allied area or conditional branch.");
            var roles = new List<EffectTarget>();
            List<EffectLeafExpression> whenFalse = MbbLeaves(branch.WhenFalse, EffectTarget.AlliedAreaRecipients, roles).ToList();
            MbbExpect(!MbbLeaves(branch.WhenTrue, null, new List<EffectTarget>()).Any() &&
                whenFalse.Count == 1 && whenFalse[0].EffectId == MbbGoodHopeBuff &&
                roles.All(role => role == EffectTarget.AlliedAreaRecipients),
                "Good Hope's application is not on the no-Despair branch only.");
            IReadOnlyList<EffectRemoval> removals = EffectRemovals.Collect(hope.Expression);
            MbbExpect(removals.Count == 1 && removals[0].EffectId == MbbCrushingDespairBuff &&
                removals[0].Target == EffectTarget.AlliedAreaRecipients,
                "Good Hope's Crushing Despair removal was not kept.");
            CastGroupingKind grouping;
            MbbExpect(EffectExpressionTargetAnalysis.TryGetGrouping(hope.Expression, out grouping) &&
                grouping == CastGroupingKind.MassConfiguredTargets, "Good Hope is not planned as one group cast.");
            // Haste: the same remove-opposing/otherwise-apply structure, ally
            // disposition None - available before and after.
            MbbDiscovery haste = MbbDiscover(dump, MbbHaste);
            MbbExpect(haste.Before.Disposition == "include" && haste.Repaired.Disposition == "include" && haste.Included,
                "Haste is no longer available: " + haste.Repaired.Reason);
            // Reopening/refreshing the planner rediscovers the same castable
            // source from scratch.
            MbbDiscovery again = MbbDiscover(new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff), MbbGoodHope);
            MbbExpect(again.Included && EffectAggregateIdentity.Describe(again.Expression) ==
                EffectAggregateIdentity.Describe(hope.Expression) &&
                EffectAggregateIdentity.For(again.Expression, "x") == EffectAggregateIdentity.For(hope.Expression, "x"),
                "A refresh changed Good Hope's discovery.");
            // A bard who knows both receives castable catalogue entries.
            MbbParty party = MbbBardParty(hope.Expression, haste.Expression);
            MbbExpect(party.Catalogue.Sources.Any(source => source.Providers.Any(p => p.Key.Ability.BaseAbilityGuid == MbbGoodHope)) &&
                party.Catalogue.Sources.Any(source => source.Providers.Any(p => p.Key.Ability.BaseAbilityGuid == MbbHaste)),
                "The bard's Good Hope or Haste has no castable catalogue entry.");
        }

        private sealed class MbbParty
        {
            internal PartyProviderSnapshot Snapshot;
            internal List<ProviderPlanningOption> Options;
            internal Dictionary<string, EffectExpression> Effects;
            internal PlannerSetupModel Catalogue;
            internal Dictionary<string, AbilityKey> Abilities = new Dictionary<string, AbilityKey>(StringComparer.Ordinal);
        }

        // Unit positions on a line (metres) and the native allied radius give
        // each legal anchor's recipients exactly as the provider option
        // builder computes them (anchor.DistanceTo(unit) <= radius + 0.01).
        private static Dictionary<string, IEnumerable<string>> MbbRecipients(IDictionary<string, float> positions,
            float radius)
        {
            return positions.ToDictionary(anchor => anchor.Key, anchor => (IEnumerable<string>)positions
                .Where(unit => Math.Abs(unit.Value - anchor.Value) <= radius + 0.01f).Select(unit => unit.Key)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        }

        private static MbbParty MbbBardParty(EffectExpression goodHope, EffectExpression haste)
        {
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 0f }, { "ally-near", 3f }, { "ally-far", 10f }
            };
            var units = positions.Keys.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                new TargetValidationSnapshot(true, true, true, true))).ToList();
            var pool = new ResourcePoolSnapshot("bard|level-3", ResourcePoolKind.SpontaneousLevel, 2, 2, null);
            var party = new MbbParty { Effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal) };
            var providers = new List<ProviderSnapshot>();
            party.Options = new List<ProviderPlanningOption>();
            foreach (KeyValuePair<string, EffectExpression> spell in new[]
            {
                new KeyValuePair<string, EffectExpression>(MbbGoodHope, goodHope),
                new KeyValuePair<string, EffectExpression>(MbbHaste, haste)
            })
            {
                var ability = new AbilityKey(spell.Key, string.Empty, 0, SourceKind.Spellbook, string.Empty);
                party.Abilities[spell.Key] = ability;
                var provider = new ProviderSnapshot(new ProviderKey("bard", "bard-book", ability, "level-3|heighten-0"),
                    spell.Key == MbbGoodHope ? "Good Hope" : "Haste", 3, pool.PoolKey, 1, null, null, 7, 70,
                    "fixture", "7 minutes", "Bard", 0, "Bard");
                providers.Add(provider);
                party.Effects[ability.Canonical] = spell.Value;
                // Good Hope's native radius is 15 feet; Haste's 30 feet.
                float radius = (spell.Key == MbbGoodHope ? 15f : 30f) * 0.3048f;
                Dictionary<string, IEnumerable<string>> recipients = MbbRecipients(positions, radius);
                party.Options.Add(new ProviderPlanningOption(provider,
                    recipients.Values.SelectMany(value => value).Distinct().ToArray(), positions.Keys.ToArray(), 7, 70,
                    CastExecutionStrategy.DirectRuleCast, "direct", recipients));
            }
            party.Snapshot = new PartyProviderSnapshot(units, providers, new[] { pool });
            party.Catalogue = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("mbb-campaign"), party.Snapshot,
                new ActiveEffectSnapshot(null), party.Effects, party.Options, ignored => { });
            return party;
        }

        private static void TestMbbGoodHopeAreaPlan()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff);
            MbbParty party = MbbBardParty(MbbDiscover(dump, MbbGoodHope).Expression, MbbDiscover(dump, MbbHaste).Expression);
            AbilityKey hope = party.Abilities[MbbGoodHope];
            // A caster-centred Good Hope wanted on the whole party: one
            // invocation and one spell slot. The group cast reaches whoever is
            // inside its native 15-foot radius (WP3: members are predictions,
            // not constraints), so the far ally is never claimed.
            CastingPlanDocument document = CastingDocument(GroupCasting("hope", "long", "bard", hope.Canonical, hope,
                new[] { "bard", "ally-near", "ally-far" }));
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document, party.Snapshot, party.Options,
                party.Effects, new CastEnhancementSnapshot[0]);
            ResolvedCasting casting = plan.Castings.Single();
            MbbExpect(casting.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                    .SequenceEqual(new[] { "ally-near", "bard" }),
                "Good Hope's 15-foot area claimed the out-of-radius ally or lost an in-radius one: " +
                string.Join(",", casting.PredictedBeneficiaryUnitIds) + " reasons=" +
                string.Join(",", casting.ReadinessReasons));
            MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready &&
                casting.Cost.Count == 1 && casting.Cost[0].PoolKey == "bard|level-3" && casting.Cost[0].Units == 1,
                "Good Hope did not reserve exactly one native slot: " + casting.Readiness + " " +
                string.Join(",", casting.ReadinessReasons) + " " +
                string.Join(",", casting.Cost.Select(c => c.PoolKey + "x" + c.Units)));
            CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
            ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, decision, party.Options, party.Effects);
            MbbExpect(steps.Converted && steps.Plan.Steps.Count == 1 && steps.Plan.Steps[0].MassCast &&
                steps.Plan.Steps[0].ExpectedRecipientUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                    .SequenceEqual(new[] { "ally-near", "bard" }) &&
                !steps.Plan.Steps[0].ExpectedRecipientUnitIds.Contains("ally-far"),
                "Good Hope was not one mass cast over its in-radius recipients: " + steps.Refusal);
        }

        private static void TestMbbGoodHopeExistingAndDespair()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            EffectExpression hope = MbbDiscover(dump, MbbGoodHope).Expression;
            var requirement = new ExistingEffectRequirement(7, 0, 70, true);
            // Existing-buff decisions read the real persistent effect.
            ExistingEffectRecipientAssessment active = ExistingEffectSufficiency.Assess("ally", hope,
                new[] { new ActiveEffectInstance(EffectKind.Buff, MbbGoodHopeBuff, 60, 7, 0) }, null, requirement);
            ExistingEffectRecipientAssessment despairOnly = ExistingEffectSufficiency.Assess("ally", hope,
                new[] { new ActiveEffectInstance(EffectKind.Buff, MbbCrushingDespairBuff, 60, 9, 0) }, null, requirement);
            ExistingEffectRecipientAssessment weaker = ExistingEffectSufficiency.Assess("ally", hope,
                new[] { new ActiveEffectInstance(EffectKind.Buff, MbbGoodHopeBuff, 60, 5, 0) }, null, requirement);
            MbbExpect(active.Verdict == ExistingEffectVerdict.Sufficient &&
                despairOnly.Verdict == ExistingEffectVerdict.NotPresent &&
                weaker.Verdict == ExistingEffectVerdict.Insufficient,
                "Good Hope's existing-effect verdicts are wrong: " + active.Verdict + "/" + despairOnly.Verdict + "/" +
                weaker.Verdict);
            // The Crushing Despair branch: the despairing ally has Despair
            // removed instead of receiving Good Hope on that cast. Confirmation
            // reports only the ally who actually received Good Hope.
            Func<string, string, long, ObservedEffectInstance> seen = (id, key, end) =>
                new ObservedEffectInstance(EffectKind.Buff, id, key, end, false);
            var before = new Dictionary<string, IEnumerable<ObservedEffectInstance>>(StringComparer.Ordinal)
            {
                { "despairing", new[] { seen(MbbCrushingDespairBuff, "despair-1", 1000) } },
                { "hopeful", new ObservedEffectInstance[0] },
                { "bard", new ObservedEffectInstance[0] }
            };
            var after = new Dictionary<string, IEnumerable<ObservedEffectInstance>>(StringComparer.Ordinal)
            {
                { "despairing", new ObservedEffectInstance[0] },
                { "hopeful", new[] { seen(MbbGoodHopeBuff, "hope-1", 5000) } },
                { "bard", new[] { seen(MbbGoodHopeBuff, "hope-2", 5000) } }
            };
            var baseline = new EffectBaseline(before);
            IReadOnlyList<string> reached = AppliedEffectJudgement.ReachedRecipients(
                new[] { "bard", "despairing", "hopeful" }, hope, baseline, unit => after[unit], "bard");
            MbbExpect(reached.OrderBy(v => v, StringComparer.Ordinal).SequenceEqual(new[] { "bard", "hopeful" }),
                "The despairing ally was reported as receiving Good Hope: " + string.Join(",", reached));
            MbbExpect(!AppliedEffectJudgement.AllReached(new[] { "bard", "despairing", "hopeful" }, hope,
                new EffectBaseline(before), unit => after[unit], null, "bard"),
                "Every predicted recipient was claimed although one only lost Crushing Despair.");
            // Everyone in the area despairing: the cast removed Despair and
            // applied nothing, so nothing confirms it as applied.
            var allDespair = new Dictionary<string, IEnumerable<ObservedEffectInstance>>(StringComparer.Ordinal)
            {
                { "despairing", new[] { seen(MbbCrushingDespairBuff, "despair-1", 1000) } }
            };
            MbbExpect(!AppliedEffectJudgement.AnyReached(new[] { "despairing" }, hope, new EffectBaseline(allDespair),
                unit => new ObservedEffectInstance[0], "bard"),
                "A cast that only removed Crushing Despair was confirmed as Good Hope.");
            // After that cast the ally still lacks Good Hope: a later run's
            // existing-effect assessment plans it again explicitly.
            MbbExpect(ExistingEffectSufficiency.Assess("despairing", hope, new ActiveEffectInstance[0], null, requirement)
                .Verdict == ExistingEffectVerdict.NotPresent, "A Despair-removed ally was treated as covered.");
        }

        private static void TestMbbFirebelly()
        {
            var dump = new NativeBlueprintDump(MbbFirebelly, MbbFirebellyBuff, MbbFirebellyBreath);
            MbbDiscovery belly = MbbDiscover(dump, MbbFirebelly);
            MbbExpect(belly.Before.Disposition == "exclude" && belly.Before.ReasonCode == "harmful-ally-disposition",
                "The baseline no longer reproduces the Firebelly exclusion: " + belly.Before.Reason);
            MbbExpect(belly.Repaired.Disposition == "include" && belly.Repaired.ReasonCode == "valid-beneficial-self-effect" &&
                belly.Repaired.Payloads.Select(e => e.EffectId).Distinct().SequenceEqual(new[] { MbbFirebellyBuff }) &&
                belly.Repaired.Payloads.All(e => e.Target == "Caster") && belly.Included,
                "Firebelly is not a beneficial caster buff: " + belly.Repaired.Reason);
            // The breath the buff grants (AddFacts FirebellyAbility) is a
            // separate damaging cone and stays out of the catalogue.
            BlueprintAbility granted = (dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbFirebellyBuff)
                .ComponentsArray ?? new Kingmaker.Blueprints.BlueprintComponent[0])
                .OfType<Kingmaker.UnitLogic.FactLogic.AddFacts>().SelectMany(c => c.Facts)
                .OfType<BlueprintAbility>().SingleOrDefault();
            MbbExpect(granted != null && granted.AssetGuid == MbbFirebellyBreath, "Firebelly no longer grants its breath.");
            MbbDiscovery breath = MbbDiscover(dump, MbbFirebellyBreath);
            MbbExpect(breath.Repaired.Disposition == "exclude" && breath.RepairedPre040.Disposition == "exclude" &&
                breath.Before.Disposition == "exclude" && !breath.Included &&
                breath.Scan.Diagnostics.Any(d => d.Code == "offensive-action"),
                "Firebelly's damaging breath entered the catalogue: " + breath.Repaired.Reason);
        }

        private static void TestMbbAbjurationDiscovery()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            foreach (string[] row in MbbAbjuration)
            {
                MbbDiscovery selection = MbbDiscover(dump, row[1]);
                MbbExpect(selection.Before.Disposition == "exclude" &&
                    selection.Before.Reason.Contains("current-target payload has harmful enemy disposition"),
                    "The baseline no longer reproduces the " + row[0] + " exclusion: " + selection.Before.Reason);
                MbbExpect(selection.Repaired.Disposition == "include" &&
                    selection.Repaired.ReasonCode == "valid-beneficial-self-effect" &&
                    selection.RepairedPre040.Disposition == "include" && selection.Included &&
                    selection.Repaired.Payloads.Select(e => e.EffectId).Distinct().SequenceEqual(new[] { row[2] }),
                    "Resistance - " + row[0] + " is not a personal buff: " + selection.Repaired.Reason);
                // Each selection removes exactly the other four resistances
                // from its (personal) target.
                string[] others = MbbAbjuration.Where(other => other != row).Select(other => other[2])
                    .OrderBy(v => v, StringComparer.Ordinal).ToArray();
                IReadOnlyList<EffectRemoval> removals = EffectRemovals.Collect(selection.Expression);
                MbbExpect(removals.Select(r => r.EffectId).OrderBy(v => v, StringComparer.Ordinal).SequenceEqual(others) &&
                    removals.All(r => r.Target == EffectTarget.CurrentTarget),
                    "Resistance - " + row[0] + " does not remove the other four selections.");
                // The native cost is the selection resource's own spend.
                var cost = selection.Ability.ComponentsArray
                    .OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>().Single();
                MbbExpect(cost.IsSpendResource && cost.Amount == 1 && cost.RequiredResource != null &&
                    cost.RequiredResource.AssetGuid == "d16e573dc50e8b548b6e7e1044a21373",
                    "Resistance - " + row[0] + " lost its native resource cost.");
                // Permanent: no finite lifetime is invented for it.
                MbbExpect(EffectApplicationLifetime.Leaves(selection.Expression).All(leaf => leaf.MaximumDurationSeconds == null),
                    "Resistance - " + row[0] + " acquired a finite lifetime.");
            }
        }

        private static MbbParty MbbWizardParty(NativeBlueprintDump dump, int remaining)
        {
            var units = new[] { "wizard", "ally" }.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                new TargetValidationSnapshot(true, true, true, true))).ToList();
            var pool = new ResourcePoolSnapshot("wizard|AbjurationResistanceResource", ResourcePoolKind.AbilityResource,
                Math.Max(1, remaining), remaining, null);
            var party = new MbbParty { Effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal) };
            var providers = new List<ProviderSnapshot>();
            party.Options = new List<ProviderPlanningOption>();
            foreach (string[] row in MbbAbjuration)
            {
                var ability = new AbilityKey(row[1], string.Empty, 0, SourceKind.AbilityResource, string.Empty);
                party.Abilities[row[0]] = ability;
                var provider = new ProviderSnapshot(new ProviderKey("wizard", string.Empty, ability, "fact"),
                    "Resistance - " + row[0], 0, pool.PoolKey, 1, null, null, 7, 0, "fixture", string.Empty,
                    "Wizard", 0, string.Empty);
                providers.Add(provider);
                party.Effects[ability.Canonical] = MbbDiscover(dump, row[1]).Expression;
                // Personal: the native target check admits only the caster.
                party.Options.Add(new ProviderPlanningOption(provider, new[] { "wizard" }, new[] { "wizard" }, 7, 0));
            }
            party.Snapshot = new PartyProviderSnapshot(units, providers, new[] { pool });
            return party;
        }

        private static void TestMbbAbjurationPlanning()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            // Enough resource for both, so only the selections' own native
            // removals decide: Cold after Fire would undo Fire.
            MbbParty party = MbbWizardParty(dump, 3);
            AbilityKey fire = party.Abilities["Fire"], cold = party.Abilities["Cold"];
            CastingPlanDocument both = CastingDocument(
                DirectCasting("fire", "long", "wizard", "wizard", fire.Canonical, fire),
                DirectCasting("cold", "long", "wizard", "wizard", cold.Canonical, cold));
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(both, party.Snapshot, party.Options,
                party.Effects, new CastEnhancementSnapshot[0]);
            ResolvedCasting first = plan.Castings.Single(c => c.CastingId == "fire");
            ResolvedCasting second = plan.Castings.Single(c => c.CastingId == "cold");
            MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(c => c.Units) == 1,
                "The first selection is not castable: " + string.Join(",", first.ReadinessReasons));
            MbbExpect(second.Readiness == ResolvedCastingReadiness.Blocked && second.Cost.Count == 0 &&
                second.ReadinessReasons.Any(reason => reason.StartsWith(ExplicitCastingCompiler.RemovesPlannedEffect +
                    ":fire:" + MbbAbjuration[3][2] + ":wizard", StringComparison.Ordinal)),
                "Two mutually exclusive resistances were planned as stackable: " + second.Readiness + " " +
                string.Join(",", second.ReadinessReasons));
            MbbExpect(plan.Castings.Where(c => c.Readiness == ResolvedCastingReadiness.Ready)
                .SelectMany(c => c.Cost).Sum(c => c.Units) == 1, "The conflicting selection reserved a use.");
            // Different units never conflict: the ally's planned effects are
            // not on the wizard (a personal selection only reaches its caster).
            // Re-selecting the active choice: already satisfied, nothing spent.
            var fireActive = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "wizard", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbAbjuration[3][2], null, 7, 0) } }
            });
            party = MbbWizardParty(dump, 1);
            ExplicitCastingPlan again = new ExplicitCastingCompiler().Compile(
                CastingDocument(DirectCasting("fire", "long", "wizard", "wizard", fire.Canonical, fire)),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, fireActive);
            MbbExpect(again.Castings.Single().Readiness == ResolvedCastingReadiness.AlreadySatisfied &&
                again.Castings.Single().Cost.Count == 0,
                "Re-selecting the active resistance recast it: " + again.Castings.Single().Readiness + " " +
                string.Join(",", again.Castings.Single().ReadinessReasons));
            // Switching from the active choice is the native switch: it casts
            // (one use) and the native action removes the previous one.
            ExplicitCastingPlan change = new ExplicitCastingCompiler().Compile(
                CastingDocument(DirectCasting("cold", "long", "wizard", "wizard", cold.Canonical, cold)),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, fireActive);
            MbbExpect(change.Castings.Single().Readiness == ResolvedCastingReadiness.Ready &&
                change.Castings.Single().Cost.Sum(c => c.Units) == 1,
                "Switching resistance was refused: " + string.Join(",", change.Castings.Single().ReadinessReasons));
            MbbExpect(EffectRemovals.Collect(party.Effects[cold.Canonical]).Any(r => r.EffectId == MbbAbjuration[3][2]),
                "Switching to Cold does not remove Fire.");
            // With the native single daily use, a second selection is never
            // funded either.
            ExplicitCastingPlan native = new ExplicitCastingCompiler().Compile(both, party.Snapshot, party.Options,
                party.Effects, new CastEnhancementSnapshot[0]);
            MbbExpect(native.Castings.Count(c => c.Readiness == ResolvedCastingReadiness.Ready) == 1,
                "Both selections were Ready on one native use.");
        }

        private static void TestMbbPointGroupSpells()
        {
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            foreach (string[] row in MbbPointGroupSpells)
            {
                MbbDiscovery spell = MbbDiscover(dump, row[1]);
                MbbExpect(spell.Ability.CanTargetPoint && !spell.Ability.CanTargetEnemies,
                    row[0] + " fixture is not point-capable and friend-only.");
                MbbExpect(spell.Before.Disposition == "exclude" && spell.Before.ReasonCode == "point-target-without-placement",
                    "The baseline no longer reproduces the " + row[0] + " point-target exclusion: " + spell.Before.Reason);
                MbbExpect(spell.Repaired.Disposition == "include" &&
                    spell.Repaired.ReasonCode == "valid-beneficial-party-effect" && spell.Included &&
                    spell.Repaired.Payloads.Select(e => e.EffectId).Distinct().SequenceEqual(new[] { row[2] }) &&
                    spell.Repaired.Payloads.All(e => e.Target == "AlliedAreaRecipients"),
                    row[0] + " is not admitted through its unit-anchored allied area: " + spell.Repaired.Reason);
                CastGroupingKind grouping;
                MbbExpect(EffectExpressionTargetAnalysis.TryGetGrouping(spell.Expression, out grouping) &&
                    grouping == CastGroupingKind.MassConfiguredTargets, row[0] + " is not one group cast.");
                float radius = float.Parse(row[3]) * 0.3048f;
                AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(spell.Ability);
                MbbExpect(area != null && Math.Abs(area.Radius - radius) < 0.001f,
                    row[0] + " lost its native " + row[3] + "-foot radius.");
                // Planning on a legal unit anchor (the native target check
                // decides which units are legal; never a ground coordinate):
                // the anchor's in-radius allies are covered, the others not,
                // and one cast reserves one slot.
                var positions = new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { "caster", 0f }, { "anchor", radius * 2f }, { "beside", radius * 2.5f }, { "far", radius * 4f }
                };
                var units = positions.Keys.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))).ToList();
                var pool = new ResourcePoolSnapshot("caster|slots", ResourcePoolKind.SpontaneousLevel, 3, 3, null);
                var ability = new AbilityKey(row[1], string.Empty, 0, SourceKind.Spellbook, string.Empty);
                var provider = new ProviderSnapshot(new ProviderKey("caster", "book", ability, "level-6|heighten-0"),
                    row[0], 6, pool.PoolKey, 1, null, null, 15, 150, "fixture", "15 minutes", "Caster", 0, "Book");
                Dictionary<string, IEnumerable<string>> recipients = MbbRecipients(positions, radius);
                var options = new List<ProviderPlanningOption>
                {
                    new ProviderPlanningOption(provider, recipients.Values.SelectMany(v => v).Distinct().ToArray(),
                        positions.Keys.ToArray(), 15, 150, CastExecutionStrategy.DirectRuleCast, "direct", recipients)
                };
                var snapshot = new PartyProviderSnapshot(units, new[] { provider }, new[] { pool });
                var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
                {
                    { ability.Canonical, spell.Expression }
                };
                ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("group", "long", "caster", ability.Canonical, ability, new[] { "anchor", "beside" },
                            null, CastingTargetMode.AnchoredOrigin, "anchor")),
                    snapshot, options, effects, new CastEnhancementSnapshot[0]);
                ResolvedCasting casting = plan.Castings.Single();
                MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready &&
                    casting.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                        .SequenceEqual(new[] { "anchor", "beside" }) &&
                    !casting.PredictedBeneficiaryUnitIds.Contains("far") &&
                    !casting.PredictedBeneficiaryUnitIds.Contains("caster") &&
                    casting.Cost.Sum(c => c.Units) == 1,
                    row[0] + " anchored plan is wrong: " + casting.Readiness + " " +
                    string.Join(",", casting.PredictedBeneficiaryUnitIds) + " " + string.Join(",", casting.ReadinessReasons));
                CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
                ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, decision, options, effects);
                MbbExpect(steps.Converted && steps.Plan.Steps.Count == 1 && steps.Plan.Steps[0].MassCast &&
                    steps.Plan.Steps[0].AnchorUnitId == "anchor",
                    row[0] + " was not one cast on its unit anchor: " + steps.Refusal);
            }
            // Enlarge and Reduce Person, Mass remove each other instead of
            // applying: planning both on the same allies is refused.
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            MbbExpect(EffectRemovals.Collect(MbbDiscover(dump, enlarge).Expression).Any(r => r.EffectId == MbbPointGroupSpells[5][2]) &&
                EffectRemovals.Collect(MbbDiscover(dump, reduce).Expression).Any(r => r.EffectId == MbbPointGroupSpells[4][2]),
                "Enlarge/Reduce Person, Mass lost their native mutual removal.");
        }

        private static void TestMbbNegativeControls()
        {
            var dump = new NativeBlueprintDump(MbbHideousLaughter, MbbHideousLaughterBuff, MbbCallLightning,
                MbbCallLightningBuff, MbbFirebellyBreath, MbbDazingTouch, MbbDazeBuff, MbbTreatAffliction,
                MbbTreatAfflictionCooldown, MbbGoodHopeTrollLair, MbbGoodHopeBuff);
            var expected = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // A save-gated harmful condition aimed at an enemy.
                { MbbHideousLaughter, null },
                // Harmful to allies AND able to target enemies: the veto
                // still applies (its caster buff is an offensive rider).
                { MbbCallLightning, "harmful-ally-disposition" },
                // The damaging breath Firebelly grants.
                { MbbFirebellyBreath, "point-target-without-placement" },
                // A touch whose only payload is a harmful condition (Daze).
                { MbbDazingTouch, null },
                // A dispel whose only buff is its own cooldown marker.
                { MbbTreatAffliction, "reactive-restoration-marker-only" },
                // Point-capable but its selector is "Any": recipients not
                // proven allied, so no unit anchor is admitted.
                { MbbGoodHopeTrollLair, "point-target-without-placement" }
            };
            foreach (KeyValuePair<string, string> control in expected)
            {
                // Live discovery (Current rules) refuses it, and the export's
                // historical Pre040 judgement is exactly the baseline's.
                MbbDiscovery result = MbbDiscover(dump, control.Key);
                MbbExpect(result.Repaired.Disposition == "exclude" && result.Before.Disposition == "exclude" &&
                    result.RepairedPre040.Disposition == result.BeforePre040.Disposition &&
                    !result.Included && (control.Value == null || result.Repaired.ReasonCode == control.Value),
                    "Negative control " + control.Key + " entered the catalogue or changed reason: " + result.Repaired.Reason +
                    " (pre040 " + result.RepairedPre040.Disposition + " vs baseline " + result.BeforePre040.Disposition + ")");
            }
            MbbExpect(dump.Get<BlueprintAbility>(MbbCallLightning).EffectOnAlly == AbilityEffectOnUnit.Harmful &&
                dump.Get<BlueprintAbility>(MbbCallLightning).CanTargetEnemies, "Call Lightning fixture changed.");
        }

        // The loaded-blueprint facts of Call of the Wild's Irresistible Dance
        // (the everyday-use v1.2 report the harmful-ally veto was made for),
        // as the guarded 0.4.1 full-user catalogue export recorded them in the
        // running game. Replaces the earlier synthetic restatement of the
        // blanket veto.
        private static void TestMbbIrresistibleDance()
        {
            string export = Path.Combine(CandidateAuditEvidenceRoot, "kbp041-rel-catalog-full", "native-buff-catalog.json");
            if (!File.Exists(export))
                throw new InvalidOperationException("The loaded-blueprint export is absent: " + export);
            JObject entry = ReadExportAbilities(export, ignored => { })
                .First(value => (string)value["abilityGuid"] == MbbIrresistibleDanceCast);
            NativeCandidateAuditFacts facts = AuditFactsFromExport(entry, true);
            MbbExpect(facts.EffectOnAlly == "Harmful" && facts.CanTargetEnemies && facts.CanTargetFriends &&
                facts.Effects.Any(e => e.Harmful == false && e.Target == "CurrentTarget"),
                "The Irresistible Dance facts are not the recorded loaded blueprint.");
            foreach (NativeCandidateRuleSet rules in new[] { NativeCandidateRuleSet.Current, NativeCandidateRuleSet.Pre040 })
            {
                NativeCandidateAuditDecision decision = new NativeCandidateClassifier(rules).Classify(facts);
                MbbExpect(decision.Disposition == "exclude" && decision.ReasonCode == "harmful-ally-disposition",
                    "Irresistible Dance entered the catalogue under " + rules + ": " + decision.Reason);
            }
        }

        // The exact Assembly-CSharp 2.1.7b anchoring contract the classifier
        // and planner rely on: a Personal-range ability anchors on its owner;
        // a point-capable spell's native anchor is a point, so the planner's
        // unit target (TargetWrapper(unit)) is the only centre it supplies.
        private static void TestMbbTargetAnchorContract()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration[3][1], MbbPointGroupSpells[3][1], MbbGoodHope);
            Func<BlueprintAbility, AbilityTargetAnchor> anchor = blueprint =>
            {
                var data = (AbilityData)FormatterServices.GetUninitializedObject(typeof(AbilityData));
                SetNative(data, "Blueprint", blueprint);
                return data.TargetAnchor;
            };
            MbbExpect(anchor(dump.Get<BlueprintAbility>(MbbAbjuration[3][1])) == AbilityTargetAnchor.Owner,
                "A Personal-range ability is not anchored on its owner.");
            MbbExpect(anchor(dump.Get<BlueprintAbility>(MbbPointGroupSpells[3][1])) == AbilityTargetAnchor.Point,
                "A point-capable spell's native anchor changed.");
            MbbExpect(anchor(dump.Get<BlueprintAbility>(MbbGoodHope)) == AbilityTargetAnchor.Unit,
                "A unit-target spell's native anchor changed.");
        }

        private static void TestMbbRemovalReasonText()
        {
            string text = WorkspaceReasonText.Describe(ExplicitCastingCompiler.RemovesPlannedEffect +
                ":fire:b9ce1623447547946b8c58efd069f7c4:wizard");
            MbbExpect(text == PlannerText.Get("reason.removes-planned-effect") && !text.Contains("removes planned effect"),
                "The removal conflict has no player-facing wording: " + text);
        }
    }
}
