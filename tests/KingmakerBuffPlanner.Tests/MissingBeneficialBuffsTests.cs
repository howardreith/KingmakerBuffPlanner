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

        private static readonly KeyValuePair<string, Action>[] MbbRegressions =
        {
            Mbb("missing-beneficial-buffs-native-fixtures-retain-misleading-flags", TestMbbFixturesRetainFlags),
            Mbb("missing-beneficial-buffs-good-hope-and-haste-reach-the-castable-catalogue", TestMbbGoodHopeDiscovery),
            Mbb("missing-beneficial-buffs-provider-admission-known-and-unknown-bard", TestMbbKnownAndUnknownBard),
            Mbb("missing-beneficial-buffs-good-hope-area-plan-is-one-native-cast", TestMbbGoodHopeAreaPlan),
            Mbb("missing-beneficial-buffs-good-hope-existing-effect-and-crushing-despair", TestMbbGoodHopeExistingAndDespair),
            Mbb("missing-beneficial-buffs-firebelly-included-breath-excluded", TestMbbFirebelly),
            Mbb("missing-beneficial-buffs-ally-disposition-exception-is-evidence-bound", TestMbbAllyDispositionException),
            Mbb("missing-beneficial-buffs-abjuration-selections-discoverable-and-personal", TestMbbAbjurationDiscovery),
            Mbb("missing-beneficial-buffs-abjuration-selections-share-the-native-pool", TestMbbAbjurationSharedPool),
            Mbb("missing-beneficial-buffs-abjuration-selections-replace-not-stack", TestMbbAbjurationPlanning),
            Mbb("missing-beneficial-buffs-switch-invalidates-a-later-already-active-skip", TestMbbSwitchInvalidatesSkip),
            Mbb("missing-beneficial-buffs-unfunded-casting-does-not-block-a-funded-one", TestMbbUnfundedDoesNotBlock),
            Mbb("missing-beneficial-buffs-removal-conflicts-follow-bound-branches", TestMbbRemovalBranches),
            Mbb("missing-beneficial-buffs-one-pass-forecast-forgets-removed-grants", TestMbbOnePassRemoval),
            Mbb("missing-beneficial-buffs-classic-planner-refuses-exclusive-choices", TestMbbClassicExclusion),
            Mbb("missing-beneficial-buffs-point-capable-group-spells-unit-anchored", TestMbbPointGroupSpells),
            Mbb("missing-beneficial-buffs-point-group-unit-anchor-refused-fails-closed", TestMbbPointGroupAnchorFailsClosed),
            Mbb("missing-beneficial-buffs-nested-allied-burst-refused", TestMbbNestedBurstRefused),
            Mbb("missing-beneficial-buffs-negative-controls-stay-excluded", TestMbbNegativeControls),
            Mbb("missing-beneficial-buffs-irresistible-dance-loaded-facts-stay-excluded", TestMbbIrresistibleDance),
            Mbb("missing-beneficial-buffs-native-target-anchor-contract", TestMbbTargetAnchorContract),
            Mbb("missing-beneficial-buffs-removal-conflict-reason-is-worded", TestMbbRemovalReasonText),
            Mbb("missing-beneficial-buffs-existing-metadata-overrides-preserved", TestMbbExistingOverridesPreserved)
        };

        private static KeyValuePair<string, Action> Mbb(string name, Action action)
        {
            return new KeyValuePair<string, Action>(name, action);
        }

        private static void RunMissingBeneficialBuffsTests(string root)
        {
            foreach (KeyValuePair<string, Action> regression in MbbRegressions)
                Run(regression.Key, regression.Value);
            RunExplicit("evidence-native-fixtures-missing-beneficial-buffs-capture", CaptureMbbFixtures);
            RunExplicit("evidence-native-fixtures-missing-beneficial-buffs-verify", VerifyMbbFixtures);
        }

        // The committed loaded-blueprint facts of Call of the Wild's
        // Irresistible Dance, copied verbatim from the guarded 0.4.1
        // full-user export by the capture check below.
        private static string MbbDanceFixture
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(NativeBlueprintDump.FixtureRoot), "LoadedBlueprintFacts",
                    "IrresistibleDanceCast." + FullUserExport + ".json");
            }
        }

        // Review R1-F9 (explicit evidence check): re-runs every regression
        // against the full 2.1.7b dump and rewrites the committed fixture
        // subset with exactly the blueprints they resolved (files copied
        // byte for byte; referenced-only blueprints listed with their native
        // type), plus the Irresistible Dance entry of the full-user export.
        private static void CaptureMbbFixtures()
        {
            string source = NativeBlueprintDump.LabDump;
            if (!Directory.Exists(source))
                throw new InvalidOperationException("Requested evidence input absent (KBP_NATIVE_BLUEPRINT_DUMP): " + source);
            string export = Path.Combine(CandidateAuditEvidenceRoot, FullUserExport, "native-buff-catalog.json");
            if (!File.Exists(export))
                throw new InvalidOperationException("Requested evidence input absent (KBP_CANDIDATE_AUDIT_EVIDENCE_ROOT): " + export);
            NativeBlueprintDump.CapturedFull.Clear();
            NativeBlueprintDump.CapturedReferences.Clear();
            NativeBlueprintDump.CaptureSource = source;
            try
            {
                foreach (KeyValuePair<string, Action> regression in MbbRegressions)
                {
                    // The Dance facts come from the export, not the dump.
                    if (regression.Key.Contains("irresistible-dance")) continue;
                    try { regression.Value(); }
                    catch (Exception exception)
                    {
                        throw new InvalidOperationException("Capture run failed in " + regression.Key + ": " +
                            exception.Message);
                    }
                }
            }
            finally { NativeBlueprintDump.CaptureSource = null; }
            string fixtures = NativeBlueprintDump.FixtureRoot;
            if (Directory.Exists(fixtures)) Directory.Delete(fixtures, true);
            // The superseded per-type layout (paths over MAX_PATH; never
            // committed) is removed if an earlier capture left it.
            string superseded = Path.Combine(Path.GetDirectoryName(fixtures), "NativeBlueprints2.1.7b");
            if (Directory.Exists(superseded)) Directory.Delete(superseded, true);
            Directory.CreateDirectory(fixtures);
            var index = new List<string>();
            foreach (KeyValuePair<string, string> full in NativeBlueprintDump.CapturedFull)
            {
                File.Copy(full.Value, Path.Combine(fixtures, full.Key + ".json"), true);
                index.Add(full.Key + "\t" + Path.GetFileName(Path.GetDirectoryName(full.Value)) + "\t" +
                    Path.GetFileName(full.Value) + "\tfull");
            }
            foreach (KeyValuePair<string, string> reference in NativeBlueprintDump.CapturedReferences
                .Where(pair => !NativeBlueprintDump.CapturedFull.ContainsKey(pair.Key)))
                index.Add(reference.Key + "\t" + Path.GetFileName(Path.GetDirectoryName(reference.Value)) + "\t" +
                    Path.GetFileName(reference.Value) + "\tref");
            index.Sort(StringComparer.Ordinal);
            File.WriteAllLines(Path.Combine(fixtures, NativeBlueprintDump.IndexFile), index.ToArray(),
                new System.Text.UTF8Encoding(false));
            JObject dance = ReadExportAbilities(export, ignored => { })
                .First(value => (string)value["abilityGuid"] == MbbIrresistibleDanceCast);
            Directory.CreateDirectory(Path.GetDirectoryName(MbbDanceFixture));
            File.WriteAllText(MbbDanceFixture, dance.ToString(Newtonsoft.Json.Formatting.Indented) + "\n",
                new System.Text.UTF8Encoding(false));
            Console.WriteLine("NATIVE-FIXTURES captured full=" + NativeBlueprintDump.CapturedFull.Count +
                " referenced=" + NativeBlueprintDump.CapturedReferences.Count(pair =>
                    !NativeBlueprintDump.CapturedFull.ContainsKey(pair.Key)) + " source=" + source);
        }

        // Review R1-F9 (explicit evidence check): every committed fixture
        // blueprint is byte-identical to the dump file of the same type and
        // name, every referenced type matches the dump, and the Dance facts
        // equal the export entry.
        private static void VerifyMbbFixtures()
        {
            string source = NativeBlueprintDump.LabDump;
            if (!Directory.Exists(source))
                throw new InvalidOperationException("Requested evidence input absent (KBP_NATIVE_BLUEPRINT_DUMP): " + source);
            string fixtures = NativeBlueprintDump.FixtureRoot;
            int files = 0, references = 0;
            var indexed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (NativeBlueprintDump.IndexEntry entry in NativeBlueprintDump.ReadIndex(fixtures))
            {
                string original = Path.Combine(source, entry.Type, entry.FileName);
                if (!File.Exists(original) || !entry.FileName.EndsWith("." + entry.Guid + ".json", StringComparison.Ordinal))
                    throw new InvalidOperationException("Fixture index entry is not in the dump: " + entry.Guid);
                if (!entry.Full) { references++; continue; }
                string committed = Path.Combine(fixtures, entry.Guid + ".json");
                if (!File.Exists(committed) || !File.ReadAllBytes(original).SequenceEqual(File.ReadAllBytes(committed)))
                    throw new InvalidOperationException("Fixture differs from the dump: " + entry.Type + "/" + entry.FileName);
                indexed.Add(Path.GetFileName(committed));
                files++;
            }
            string stray = Directory.EnumerateFiles(fixtures, "*.json").Select(Path.GetFileName)
                .FirstOrDefault(name => !indexed.Contains(name));
            if (stray != null) throw new InvalidOperationException("Unindexed fixture file: " + stray);
            string export = Path.Combine(CandidateAuditEvidenceRoot, FullUserExport, "native-buff-catalog.json");
            if (!File.Exists(export))
                throw new InvalidOperationException("Requested evidence input absent (KBP_CANDIDATE_AUDIT_EVIDENCE_ROOT): " + export);
            JObject dance = ReadExportAbilities(export, ignored => { })
                .First(value => (string)value["abilityGuid"] == MbbIrresistibleDanceCast);
            if (!JToken.DeepEquals(dance, JObject.Parse(File.ReadAllText(MbbDanceFixture))))
                throw new InvalidOperationException("The Irresistible Dance fixture differs from the export entry.");
            if (files == 0) throw new InvalidOperationException("No committed native fixtures were found.");
            Console.WriteLine("NATIVE-FIXTURES verified files=" + files + " referenced=" + references +
                " dance-entry=identical source=" + source);
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
                result.Facts = NativeCatalogExporter.AuditFacts(result.Ability, true, effects, result.Scan.Diagnostics,
                    MbbOverrides().AllyDispositionExceptions(guid));
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
            // A bard who knows both receives castable catalogue entries,
            // through the production provider admission, also after a
            // refresh (a second, independent admission of the same reads).
            foreach (MbbParty party in new[] { MbbBardParty(dump, MbbGoodHope, MbbHaste),
                MbbBardParty(new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff),
                    MbbGoodHope, MbbHaste) })
                MbbExpect(party.Catalogue.Sources.Any(source => source.Providers.Any(p => p.Key.Ability.BaseAbilityGuid == MbbGoodHope)) &&
                    party.Catalogue.Sources.Any(source => source.Providers.Any(p => p.Key.Ability.BaseAbilityGuid == MbbHaste)) &&
                    EffectAggregateIdentity.Describe(party.Effects[party.Abilities[MbbGoodHope].Canonical]) ==
                        EffectAggregateIdentity.Describe(hope.Expression),
                    "The bard's Good Hope or Haste has no castable catalogue entry, or its expression changed.");
        }

        private sealed class MbbParty
        {
            internal PartyProviderSnapshot Snapshot;
            internal List<ProviderPlanningOption> Options;
            internal Dictionary<string, EffectExpression> Effects;
            internal PlannerSetupModel Catalogue;
            internal Dictionary<string, AbilityKey> Abilities = new Dictionary<string, AbilityKey>(StringComparer.Ordinal);
            internal PartyProviderAdmission Admission;
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

        // Review R1-F8: the party is assembled by the PRODUCTION decisions.
        // Only the live reads are injected: which spells a caster knows
        // (spellbook), slot and resource balances, the native
        // AbilityData.CanTarget verdict per unit, and unit positions. The
        // providers, pools, costs (PartyProviderAdmission,
        // KingmakerFactResources), legal anchors and per-anchor recipients
        // (ProviderTargetingPlanner, KingmakerAreaCoverageResolver) and the
        // castable catalogue (PlannerSetupModel) are derived by product code.
        private const string MbbBardBook = "bard-spellbook";

        private static List<UnitSnapshot> MbbUnits(IEnumerable<string> ids)
        {
            return ids.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                new TargetValidationSnapshot(true, true, true, true))).ToList();
        }

        private static PartyProviderAdmission MbbAdmission(NativeBlueprintDump dump)
        {
            return new PartyProviderAdmission(new KingmakerBuffSourceDiscovery(MbbOverrides(), dump.Lookup));
        }

        // The live values the snapshot builder reads for one owned ability.
        private static ProviderAbilityRead MbbRead(NativeBlueprintDump dump, string guid, string name,
            SourceKind kind, int casterLevel)
        {
            return new ProviderAbilityRead
            {
                Blueprint = dump.Get<BlueprintAbility>(guid),
                Ability = new AbilityKey(guid, string.Empty, 0, kind, string.Empty),
                DisplayName = name,
                SourceDisplayName = "fixture",
                VariantOrder = 0,
                CasterLevelRead = () => casterLevel,
                DescriptionRead = () => "fixture",
                DurationTextRead = () => "fixture"
            };
        }

        // Unit positions on a line (metres) are the injected live distances.
        private static Func<string, string, float?> MbbDistance(IDictionary<string, float> positions)
        {
            return (from, to) => positions.ContainsKey(from) && positions.ContainsKey(to)
                ? Math.Abs(positions[from] - positions[to]) : (float?)null;
        }

        private static ProviderPlanningOption MbbOption(NativeBlueprintDump dump, ProviderSnapshot provider,
            EffectExpression expression, IEnumerable<UnitSnapshot> units, Func<string, bool> nativeCanTarget,
            IDictionary<string, float> positions)
        {
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(provider.Key.Ability.BaseAbilityGuid);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units, true,
                false, area, nativeCanTarget, MbbDistance(positions));
            return ProviderTargetingPlanner.BuildOption(provider, decision, CastExecutionStrategy.DirectRuleCast,
                "ordinary-direct-rule-cast");
        }

        private static MbbParty MbbBardParty(NativeBlueprintDump dump, params string[] knownLevelThreeSpells)
        {
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 0f }, { "ally-near", 3f }, { "ally-far", 10f }
            };
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            // The bard's level-3 spontaneous slots (2 of 2 left).
            ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool("bard", MbbBardBook, 3, 2, 2);
            var providers = new List<ProviderSnapshot>();
            foreach (string guid in knownLevelThreeSpells)
                admission.AdmitSpell("bard", MbbBardBook, false, "Bard",
                    MbbRead(dump, guid, guid == MbbGoodHope ? "Good Hope" : guid == MbbHaste ? "Haste" : guid,
                        SourceKind.Spellbook, 7), 3, 0, pool.PoolKey, 1, new string[0], providers);
            var party = new MbbParty
            {
                Effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal),
                Admission = admission
            };
            foreach (ProviderSnapshot provider in providers)
                party.Abilities[provider.Key.Ability.BaseAbilityGuid] = provider.Key.Ability;
            // Native CanTarget: Good Hope and Haste are self/friend unit
            // spells; every friendly unit is a legal target here.
            party.Options = providers.Select(provider => MbbOption(dump, provider,
                party.Effects[provider.Key.Ability.Canonical], units, id => true, positions)).ToList();
            party.Snapshot = new PartyProviderSnapshot(units, providers, new[] { pool });
            party.Catalogue = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("mbb-campaign"), party.Snapshot,
                new ActiveEffectSnapshot(null), party.Effects, party.Options, ignored => { });
            return party;
        }

        // A bard that knows Good Hope gets a castable entry; one that does
        // not, never does - the catalogue follows the production provider
        // admission, not the test.
        private static void TestMbbKnownAndUnknownBard()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff);
            MbbParty knows = MbbBardParty(dump, MbbGoodHope, MbbHaste);
            MbbParty lacks = MbbBardParty(dump, MbbHaste);
            Func<MbbParty, string, bool> listed = (party, guid) => party.Catalogue.Sources.Any(source =>
                source.Providers.Any(p => p.Key.Ability.BaseAbilityGuid == guid));
            MbbExpect(listed(knows, MbbGoodHope) && listed(knows, MbbHaste) &&
                knows.Snapshot.Providers.Count == 2 &&
                knows.Snapshot.Providers.All(p => p.ResourcePoolKey == "bard|spellbook|" + MbbBardBook +
                    "|spontaneous-3" && p.UnitsPerCast == 1 && p.SpellLevel == 3),
                "The bard who knows Good Hope lacks a castable entry or its native slot cost.");
            MbbExpect(!listed(lacks, MbbGoodHope) && listed(lacks, MbbHaste) &&
                !lacks.Effects.Keys.Any(key => key.Contains(MbbGoodHope)),
                "A bard who does not know Good Hope was offered it.");
            MbbExpect(knows.Admission.Traces.Any(trace => trace.BlueprintGuid == MbbGoodHope && trace.Beneficial) &&
                knows.Admission.BeneficialCandidateCount == 2,
                "Provider admission did not trace Good Hope as an admitted candidate.");
            // Without the evidence-backed registry entry the same production
            // path refuses Good Hope (the exception is the only reason it is
            // admitted; the classifier holds no identities).
            var bare = new PartyProviderAdmission(new KingmakerBuffSourceDiscovery(EffectOverrideRegistry.Empty(), dump.Lookup));
            var none = new List<ProviderSnapshot>();
            ProviderSnapshot refused = bare.AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(dump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, "pool", 1, new string[0], none);
            MbbExpect(refused == null && none.Count == 0 &&
                bare.Traces.Single().Reason.StartsWith("harmful-ally-disposition", StringComparison.Ordinal),
                "Good Hope was admitted without its evidence-backed exception: " + bare.Traces.Single().Reason);
        }

        private static void TestMbbGoodHopeAreaPlan()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff);
            MbbParty party = MbbBardParty(dump, MbbGoodHope, MbbHaste);
            AbilityKey hope = party.Abilities[MbbGoodHope];
            // The production option: every unit a legal anchor (the injected
            // native verdict); the caster's recipients are exactly the units
            // within the native 15-foot radius of its position.
            ProviderPlanningOption hopeOption = party.Options.Single(o => o.Provider.Key.Ability.Equals(hope));
            MbbExpect(hopeOption.CoveredTargetIdsForAnchor("bard").SequenceEqual(new[] { "ally-near", "bard" }) &&
                hopeOption.CoveredTargetIdsForAnchor("ally-far").SequenceEqual(new[] { "ally-far" }),
                "The production anchor coverage of Good Hope is not its 15-foot area.");
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
                casting.Cost.Count == 1 && casting.Cost[0].PoolKey == "bard|spellbook|" + MbbBardBook + "|spontaneous-3" &&
                casting.Cost[0].Units == 1,
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

        private const string MbbAbjurationResource = "d16e573dc50e8b548b6e7e1044a21373";

        // The wizard's five selections through the production fact-provider
        // path: each blueprint's own AbilityResourceLogic is priced by
        // KingmakerFactResources (native resource, native cost, the native
        // count the game reports) and admitted by PartyProviderAdmission.
        // remaining: uses left of the native resource (its 2.1.7b maximum is
        // 1 per day: BlueprintAbilityResource m_MaxAmount.BaseValue = 1).
        private static MbbParty MbbWizardParty(NativeBlueprintDump dump, int remaining)
        {
            List<UnitSnapshot> units = MbbUnits(new[] { "wizard", "ally" });
            var positions = new Dictionary<string, float>(StringComparer.Ordinal) { { "wizard", 0f }, { "ally", 2f } };
            PartyProviderAdmission admission = MbbAdmission(dump);
            var providers = new List<ProviderSnapshot>();
            var pools = new List<ResourcePoolSnapshot>();
            var poolKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string[] row in MbbAbjuration)
            {
                var logic = dump.Get<BlueprintAbility>(row[1]).ComponentsArray
                    .OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>().Single();
                KingmakerFactResource resource;
                string refusal;
                bool known = KingmakerFactResources.TryPriceAbilityResource("wizard", logic.RequiredResource.AssetGuid,
                    logic.Amount, remaining, () => 1, remaining / logic.Amount, false, string.Empty, string.Empty,
                    out resource, out refusal);
                admission.AdmitFact("wizard", MbbRead(dump, row[1], "Resistance - " + row[0], SourceKind.AbilityResource, 7),
                    known, resource, refusal, string.Empty, providers, pools, poolKeys);
            }
            var party = new MbbParty
            {
                Effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal),
                Admission = admission
            };
            foreach (string[] row in MbbAbjuration)
            {
                ProviderSnapshot provider = providers.Single(p => p.Key.Ability.BaseAbilityGuid == row[1]);
                party.Abilities[row[0]] = provider.Key.Ability;
            }
            // Native CanTarget of a Personal ability admits only its owner
            // (the injected verdict; its anchor is the owner).
            party.Options = providers.Select(provider => MbbOption(dump, provider,
                party.Effects[provider.Key.Ability.Canonical], units, id => id == "wizard", positions)).ToList();
            party.Snapshot = new PartyProviderSnapshot(units, providers, pools);
            return party;
        }

        // All five valid selections are offered for the eligible wizard,
        // personal only, each spending one use of the ONE native resource.
        private static void TestMbbAbjurationSharedPool()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbParty party = MbbWizardParty(dump, 1);
            string pool = "wizard|resource|" + MbbAbjurationResource;
            MbbExpect(party.Snapshot.Providers.Count == 5 &&
                party.Snapshot.Providers.All(p => p.ResourcePoolKey == pool && p.UnitsPerCast == 1) &&
                party.Snapshot.ResourcePools.Count == 1 && party.Snapshot.ResourcePools[0].PoolKey == pool &&
                party.Snapshot.ResourcePools[0].Kind == ResourcePoolKind.AbilityResource &&
                party.Snapshot.ResourcePools[0].Remaining == 1,
                "The five selections do not share the native resource: " + string.Join(",",
                    party.Snapshot.Providers.Select(p => p.ResourcePoolKey + "x" + p.UnitsPerCast)));
            MbbExpect(party.Options.All(o => o.ReachableTargetIds.SequenceEqual(new[] { "wizard" }) &&
                o.LegalAnchorIds.SequenceEqual(new[] { "wizard" })),
                "A personal selection can be aimed at someone other than its owner.");
            var catalogue = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("mbb-campaign"), party.Snapshot,
                new ActiveEffectSnapshot(null), party.Effects, party.Options, ignored => { });
            MbbExpect(MbbAbjuration.All(row => catalogue.Sources.Any(source =>
                    source.Providers.Any(p => p.Key.Ability.BaseAbilityGuid == row[1]))),
                "A resistance selection has no castable catalogue entry.");
            // A count the native resource does not explain is refused, never
            // priced as free (fail closed).
            KingmakerFactResource bad;
            string refusal;
            MbbExpect(!KingmakerFactResources.TryPriceAbilityResource("wizard", MbbAbjurationResource, 1, 1, () => 1, 3,
                false, string.Empty, string.Empty, out bad, out refusal) && bad == null &&
                refusal.Contains("ability-resource-cost-or-count-mismatch"),
                "An inconsistent native count was priced.");
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

        private static ActiveEffectSnapshot MbbActive(string unit, string effect)
        {
            return ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { unit, new[] { new ActiveEffectInstance(EffectKind.Buff, effect, null, 7, 0) } }
            });
        }

        // Review R1-F1: Fire active, one daily use, Cold then Fire in one
        // skip-if-active routine. Cold is the native switch (one use); Fire
        // can no longer be counted as already active after it.
        private static void TestMbbSwitchInvalidatesSkip()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbParty party = MbbWizardParty(dump, 1);
            AbilityKey fire = party.Abilities["Fire"], cold = party.Abilities["Cold"];
            string fireBuff = MbbAbjuration[3][2];
            ActiveEffectSnapshot fireActive = MbbActive("wizard", fireBuff);
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                    DirectCasting("cold", "long", "wizard", "wizard", cold.Canonical, cold),
                    DirectCasting("fire", "long", "wizard", "wizard", fire.Canonical, fire)),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false,
                fireActive);
            ResolvedCasting switched = plan.CastingById("cold");
            ResolvedCasting kept = plan.CastingById("fire");
            MbbExpect(switched.Readiness == ResolvedCastingReadiness.Ready &&
                switched.Cost.Count == 1 && switched.Cost[0].PoolKey == "wizard|resource|" + MbbAbjurationResource &&
                switched.Cost[0].Units == 1,
                "The switch to Cold is not one native use: " + switched.Readiness + " " +
                string.Join(",", switched.ReadinessReasons));
            string expected = ExplicitCastingCompiler.EffectRemovedByEarlierCasting + ":cold:" + fireBuff + ":wizard";
            MbbExpect(kept.Readiness == ResolvedCastingReadiness.Blocked && kept.Cost.Count == 0 &&
                kept.ReadinessReasons.Contains(expected) &&
                !kept.ReadinessReasons.Any(r => r.StartsWith("already-active", StringComparison.Ordinal)),
                "Fire was still counted as already active after the planned switch: " + kept.Readiness + " " +
                string.Join(",", kept.ReadinessReasons));
            MbbExpect(plan.ReadyInvocationCount == 1 &&
                plan.Castings.Count(c => c.Readiness == ResolvedCastingReadiness.AlreadySatisfied) == 0 &&
                plan.Castings.SelectMany(c => c.Cost).Sum(c => c.Units) == 1,
                "The plan claims both resistances or spends more than the switch.");
            MbbExpect(WorkspaceReasonText.Describe(expected) == PlannerText.Get("reason.effect-removed-by-earlier-casting"),
                "The invalidated skip has no player-facing wording: " + WorkspaceReasonText.Describe(expected));
            // The Cold-only switch stays an ordinary one-use cast.
            ExplicitCastingPlan only = new ExplicitCastingCompiler().Compile(CastingDocument(
                    DirectCasting("cold", "long", "wizard", "wizard", cold.Canonical, cold)),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false,
                fireActive);
            MbbExpect(only.Castings.Single().Readiness == ResolvedCastingReadiness.Ready &&
                only.Castings.Single().Cost.Sum(c => c.Units) == 1, "The Cold-only switch was refused.");
            // Fire first (already active) then Cold: Cold would remove the
            // kept Fire, so it is the one refused.
            ExplicitCastingPlan reversed = new ExplicitCastingCompiler().Compile(CastingDocument(
                    DirectCasting("fire", "long", "wizard", "wizard", fire.Canonical, fire),
                    DirectCasting("cold", "long", "wizard", "wizard", cold.Canonical, cold)),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false,
                fireActive);
            MbbExpect(reversed.CastingById("fire").Readiness == ResolvedCastingReadiness.AlreadySatisfied &&
                reversed.CastingById("cold").Readiness == ResolvedCastingReadiness.Blocked &&
                reversed.CastingById("cold").ReadinessReasons.Contains(
                    ExplicitCastingCompiler.RemovesPlannedEffect + ":fire:" + fireBuff + ":wizard"),
                "Re-selecting Fire then switching to Cold was planned as both.");
        }

        // Group-spell party for the Enlarge/Reduce Person, Mass scenarios:
        // casters with their own level-4 slots; allies on a line.
        private static MbbParty MbbMassParty(NativeBlueprintDump dump, IDictionary<string, int> slotsByCaster,
            IDictionary<string, string[]> knownByCaster, IDictionary<string, float> positions)
        {
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            var providers = new List<ProviderSnapshot>();
            var pools = new List<ResourcePoolSnapshot>();
            foreach (KeyValuePair<string, string[]> caster in knownByCaster)
            {
                ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool(caster.Key, "book-" + caster.Key, 4,
                    slotsByCaster[caster.Key], Math.Max(1, slotsByCaster[caster.Key]));
                pools.Add(pool);
                foreach (string guid in caster.Value)
                    admission.AdmitSpell(caster.Key, "book-" + caster.Key, false, "Book",
                        MbbRead(dump, guid, guid, SourceKind.Spellbook, 9), 4, 0, pool.PoolKey, 1, new string[0], providers);
            }
            var party = new MbbParty
            {
                Effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal),
                Admission = admission
            };
            // Native CanTarget admits every friendly unit as a centre here.
            party.Options = providers.Select(provider => MbbOption(dump, provider,
                party.Effects[provider.Key.Ability.Canonical], units, id => true, positions)).ToList();
            party.Snapshot = new PartyProviderSnapshot(units, providers, pools);
            return party;
        }

        private static AbilityKey MbbSpellKey(string guid)
        {
            return new AbilityKey(guid, string.Empty, 0, SourceKind.Spellbook, string.Empty);
        }

        // Review R1-F2: an earlier cast takes caster A's last slot, so A's
        // Enlarge Person, Mass is unfunded; caster B's funded Reduce Person,
        // Mass on the same allies must stay Ready and reserve B's own slot.
        private static void TestMbbUnfundedDoesNotBlock()
        {
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float radius = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "a", 0f }, { "b", radius * 0.5f }, { "x", radius * 0.25f }, { "far", radius * 10f }
            };
            MbbParty party = MbbMassParty(dump,
                new Dictionary<string, int> { { "a", 1 }, { "b", 1 } },
                new Dictionary<string, string[]> { { "a", new[] { enlarge } }, { "b", new[] { reduce } } }, positions);
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                    // Takes A's only slot; reaches only the distant ally.
                    GroupCasting("first", "long", "a", enlargeKey.Canonical, enlargeKey, new[] { "far" },
                        null, CastingTargetMode.AnchoredOrigin, "far"),
                    GroupCasting("unfunded", "long", "a", enlargeKey.Canonical, enlargeKey, new[] { "x" },
                        null, CastingTargetMode.AnchoredOrigin, "x"),
                    GroupCasting("funded", "long", "b", reduceKey.Canonical, reduceKey, new[] { "x" },
                        null, CastingTargetMode.AnchoredOrigin, "x")),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0]);
            ResolvedCasting unfunded = plan.CastingById("unfunded");
            ResolvedCasting funded = plan.CastingById("funded");
            MbbExpect(plan.CastingById("first").Readiness == ResolvedCastingReadiness.Ready &&
                unfunded.Readiness == ResolvedCastingReadiness.Blocked && unfunded.Cost.Count == 0 &&
                !unfunded.ReadinessReasons.Any(r => r.StartsWith(ExplicitCastingCompiler.RemovesPlannedEffect, StringComparison.Ordinal)),
                "The unfunded Enlarge is not blocked for its resource: " + unfunded.Readiness + " " +
                string.Join(",", unfunded.ReadinessReasons));
            MbbExpect(funded.Readiness == ResolvedCastingReadiness.Ready && funded.Cost.Count == 1 &&
                funded.Cost[0].PoolKey == "b|spellbook|book-b|spontaneous-4" && funded.Cost[0].Units == 1,
                "An unfunded casting blocked the funded Reduce Person, Mass: " + funded.Readiness + " " +
                string.Join(",", funded.ReadinessReasons));
        }

        // Review R1-F3: conflicts follow the provider-bound expression and
        // its branches. Synthetic graphs isolate each branch shape on the
        // production compiler: P provides effect E on the target; later
        // castings may remove E.
        private static void TestMbbRemovalBranches()
        {
            const string e = "e0000000000000000000000000000001", fact = "f0000000000000000000000000000001";
            Func<string, EffectExpression> apply = id =>
                new EffectLeafExpression(EffectKind.Buff, id, EffectTarget.CurrentTarget, "fixture", "fixture:" + id);
            EffectExpression removeE = new EmptyEffectExpression("restorative-action:ContextActionRemoveBuff",
                new EffectRemoval(e, EffectTarget.CurrentTarget));
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
            var shapes = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { "p", apply(e) },
                // A caster-fact gate whose true branch removes E.
                { "gated", new SequenceEffectExpression(new[] { apply("e0000000000000000000000000000002"),
                    new ConditionalEffectExpression("ContextConditionCasterHasFact", removeE,
                        new SequenceEffectExpression(new EffectExpression[0]),
                        new CasterFactGate(false, new[] { new KeyValuePair<string, bool>(fact, false) })) }) },
                // Alternatives: remove E on one branch, apply E on the other.
                { "alternative", new ConditionalEffectExpression("ContextConditionHasBuff", removeE, apply(e)) },
                // Remove then certainly re-apply E.
                { "restored", new SequenceEffectExpression(new[] { removeE, apply(e) }) }
            };
            var units = MbbUnits(new[] { "c", "t" });
            var pool = new ResourcePoolSnapshot("c|slots", ResourcePoolKind.SpontaneousLevel, 9, 9, null);
            var providers = new List<ProviderSnapshot>();
            var keys = new Dictionary<string, AbilityKey>(StringComparer.Ordinal);
            int index = 0;
            foreach (KeyValuePair<string, EffectExpression> shape in shapes)
            {
                var key = new AbilityKey("a" + (++index).ToString("D31"), string.Empty, 0, SourceKind.Spellbook, string.Empty);
                keys[shape.Key] = key;
                effects[key.Canonical] = shape.Value;
                providers.Add(new ProviderSnapshot(new ProviderKey("c", "book", key, "level-1|heighten-0"), shape.Key, 1,
                    pool.PoolKey, 1, null, null, 5, 50, "fixture", "fixture", "Book", 0, "Book"));
            }
            var snapshot = new PartyProviderSnapshot(units, providers, new[] { pool });
            Func<IReadOnlyDictionary<string, bool>, List<ProviderPlanningOption>> optionsWith = facts =>
                providers.Select(provider => new ProviderPlanningOption(provider, new[] { "t" }, new[] { "t" }, 5, 50,
                    CastExecutionStrategy.DirectRuleCast, "direct", null, facts)).ToList();
            Func<string, IReadOnlyDictionary<string, bool>, ResolvedCasting> later = (shape, facts) =>
                new ExplicitCastingCompiler().Compile(CastingDocument(
                        DirectCasting("p", "long", "c", "t", keys["p"].Canonical, keys["p"]),
                        DirectCasting(shape, "long", "c", "t", keys[shape].Canonical, keys[shape])),
                    snapshot, optionsWith(facts), effects, new CastEnhancementSnapshot[0]).CastingById(shape);
            string conflict = ExplicitCastingCompiler.RemovesPlannedEffect + ":p:" + e + ":t";
            // The caster lacks the fact: the bound expression has no removal.
            ResolvedCasting falseBranch = later("gated", new Dictionary<string, bool> { { fact, false } });
            // The caster has it: the removal is realizable.
            ResolvedCasting trueBranch = later("gated", new Dictionary<string, bool> { { fact, true } });
            // The fact is unread: either branch may run.
            ResolvedCasting unknown = later("gated", null);
            MbbExpect(falseBranch.Readiness == ResolvedCastingReadiness.Ready &&
                trueBranch.Readiness == ResolvedCastingReadiness.Blocked && trueBranch.ReadinessReasons.Contains(conflict) &&
                unknown.Readiness == ResolvedCastingReadiness.Blocked && unknown.ReadinessReasons.Contains(conflict),
                "A bound-false removal constrained the plan or a realizable one did not: " + falseBranch.Readiness + "/" +
                trueBranch.Readiness + "/" + unknown.Readiness);
            ResolvedCasting alternative = later("alternative", null);
            ResolvedCasting restored = later("restored", null);
            MbbExpect(alternative.Readiness == ResolvedCastingReadiness.Blocked &&
                alternative.ReadinessReasons.Contains(conflict),
                "An application on the alternative branch was taken to restore the removed effect.");
            MbbExpect(restored.Readiness == ResolvedCastingReadiness.Ready,
                "A removal certainly followed by re-application blocked the plan: " +
                string.Join(",", restored.ReadinessReasons));
        }

        // Review R1-F4: the one-pass forecast over successive routines must
        // not skip a repeat whose earlier coverage an intervening opposing
        // cast removed.
        private static void TestMbbOnePassRemoval()
        {
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float radius = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "w", 0f }, { "ally", radius * 0.5f }
            };
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 5 } },
                new Dictionary<string, string[]> { { "w", new[] { enlarge, reduce } } }, positions);
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            Func<string, string, AbilityKey, PlannedCasting> mass = (id, routine, key) =>
                GroupCasting(id, routine, "w", key.Canonical, key, new[] { "w", "ally" });
            CastingForecast opposed = new CastingForecastService().ForecastOnePass(CastingDocument(
                    mass("enlarge-1", "long", enlargeKey), mass("reduce", "important", reduceKey),
                    mass("enlarge-2", "short", enlargeKey)),
                party.Snapshot, party.Options, party.Effects);
            ResolvedCasting repeat = opposed.Plan.CastingById("enlarge-2");
            MbbExpect(repeat.Readiness == ResolvedCastingReadiness.Ready && repeat.Cost.Sum(c => c.Units) == 1 &&
                opposed.Plan.CastingById("reduce").Readiness == ResolvedCastingReadiness.Ready &&
                opposed.Plan.Castings.SelectMany(c => c.Cost).Sum(c => c.Units) == 3,
                "The one-pass forecast skipped Enlarge after Reduce removed it: " + repeat.Readiness + " " +
                string.Join(",", repeat.ReadinessReasons));
            // Without the opposing routine the structural repeat is still
            // recognized (the projection itself is unchanged).
            CastingForecast repeated = new CastingForecastService().ForecastOnePass(CastingDocument(
                    mass("enlarge-1", "long", enlargeKey), mass("enlarge-2", "short", enlargeKey)),
                party.Snapshot, party.Options, party.Effects);
            MbbExpect(repeated.Plan.CastingById("enlarge-2").Readiness == ResolvedCastingReadiness.AlreadySatisfied,
                "The structural repeat projection regressed.");
        }

        // Review R1-F5: the retired Classic planner (still reachable through
        // an explicit legacy mode file) applies the same exclusion.
        private static void TestMbbClassicExclusion()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbParty party = MbbWizardParty(dump, 3);
            string fireBuff = MbbAbjuration[3][2];
            Func<string, string, ExistingEffectPolicy, int, BuffCastRequest> request = (element, assignment, policy, order) =>
                new BuffCastRequest(new BuffSourceDefinition(element, party.Abilities[element],
                        party.Effects[party.Abilities[element].Canonical], CastGroupingKind.PerTarget),
                    new[] { "wizard" }, policy, null, new EnhancementRequest[0], assignment, null, null, null, order);
            // Fire active; Cold (the switch), then a skip-if-active Fire.
            CastPlan switched = new CastPlanner().PlanRoutine(party.Snapshot, new[]
                {
                    request("Cold", "cold", ExistingEffectPolicy.Overwrite, 0),
                    request("Fire", "fire", ExistingEffectPolicy.SkipAlreadyActive, 1)
                }, party.Options, new ProviderSelectionPolicy(null, null, null), MbbActive("wizard", fireBuff));
            TargetPlanOutcome fireOutcome = switched.Outcomes.Single(o => o.AssignmentId == "fire");
            MbbExpect(switched.Steps.Count == 1 && switched.Steps[0].AssignmentId == "cold" &&
                fireOutcome.Kind == TargetOutcomeKind.Unfulfilled &&
                fireOutcome.Reason == "fire:" + ExplicitCastingCompiler.EffectRemovedByEarlierCasting + ":cold:" +
                    fireBuff + ":wizard",
                "Classic still claims Fire is active after the planned switch: " + fireOutcome.Kind + " " +
                fireOutcome.Reason);
            // Nothing active; Fire then Cold in one routine.
            CastPlan both = new CastPlanner().PlanRoutine(party.Snapshot, new[]
                {
                    request("Fire", "fire", ExistingEffectPolicy.Overwrite, 0),
                    request("Cold", "cold", ExistingEffectPolicy.Overwrite, 1)
                }, party.Options, new ProviderSelectionPolicy(null, null, null), new ActiveEffectSnapshot(null));
            TargetPlanOutcome coldOutcome = both.Outcomes.Single(o => o.AssignmentId == "cold");
            MbbExpect(both.Steps.Count == 1 && both.Steps[0].AssignmentId == "fire" &&
                coldOutcome.Kind == TargetOutcomeKind.Unfulfilled &&
                coldOutcome.Reason == "cold:" + ExplicitCastingCompiler.RemovesPlannedEffect + ":fire:" + fireBuff +
                    ":wizard",
                "Classic planned two mutually exclusive resistances: " + coldOutcome.Kind + " " + coldOutcome.Reason);
            // A lone selection is unaffected.
            CastPlan single = new CastPlanner().PlanRoutine(party.Snapshot,
                new[] { request("Cold", "cold", ExistingEffectPolicy.Overwrite, 0) }, party.Options,
                new ProviderSelectionPolicy(null, null, null), MbbActive("wizard", fireBuff));
            MbbExpect(single.Steps.Count == 1, "Classic refused an ordinary switch.");
        }

        private sealed class MbbGroup
        {
            internal PartyProviderSnapshot Snapshot;
            internal List<ProviderPlanningOption> Options;
            internal Dictionary<string, EffectExpression> Effects;
            internal AbilityKey Ability;
        }

        // One caster ("caster") who knows the group spell: provider and pool
        // by PartyProviderAdmission, option by ProviderTargetingPlanner from
        // the injected native CanTarget verdict and positions.
        private static MbbGroup MbbGroupCaster(NativeBlueprintDump dump, string guid,
            IDictionary<string, float> positions, Func<string, bool> nativeCanTarget)
        {
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool("caster", "book", 6, 3, 3);
            var providers = new List<ProviderSnapshot>();
            ProviderSnapshot provider = admission.AdmitSpell("caster", "book", false, "Book",
                MbbRead(dump, guid, guid, SourceKind.Spellbook, 15), 6, 0, pool.PoolKey, 1, new string[0], providers);
            MbbExpect(provider != null, guid + " was not admitted as a provider: " +
                string.Join(",", admission.Traces.Select(t => t.Reason)));
            var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
            return new MbbGroup
            {
                Snapshot = new PartyProviderSnapshot(units, providers, new[] { pool }),
                Options = new List<ProviderPlanningOption>
                {
                    MbbOption(dump, provider, effects[provider.Key.Ability.Canonical], units, nativeCanTarget, positions)
                },
                Effects = effects,
                Ability = provider.Key.Ability
            };
        }

        // Review R1-F6: the misleading "harmful to allies" flag is overridden
        // only for a payload an evidence-backed versioned entry names, on an
        // ability that cannot be aimed at an enemy, and only when that
        // payload blueprint was read. Adapter-backed negatives keep the
        // native flags.
        private static void TestMbbAllyDispositionException()
        {
            // 1. The payload buff lookup fails (blueprint not loaded): the
            // exception cannot apply; the ability stays excluded.
            var unread = new NativeBlueprintDump(MbbGoodHope);
            MbbDiscovery missing = MbbDiscover(unread, MbbGoodHope);
            MbbExpect(missing.Facts.Effects.Any(e => e.EffectId == MbbGoodHopeBuff && e.Harmful == null &&
                    (e.ComponentTypes ?? new string[0]).Count == 0) &&
                missing.Facts.AllyDispositionExceptionEffectIds.Contains(MbbGoodHopeBuff) &&
                missing.Repaired.Disposition == "exclude" && missing.Repaired.ReasonCode == "harmful-ally-disposition" &&
                !missing.Included,
                "An unread payload was admitted through the ally-disposition exception: " + missing.Repaired.Reason);
            // 2. A friend-only "harmful to allies" ability whose payload is a
            // penalty buff with a clear m_Harmful flag (the native Crushing
            // Despair buff: -2 attack, damage, saves, skills, ability checks,
            // no harmful flag, no harmful condition). Built from native Good
            // Hope with its application re-pointed at that buff.
            var penalty = new NativeBlueprintDump(MbbGoodHope, MbbCrushingDespairBuff);
            BlueprintAbility hope = penalty.Get<BlueprintAbility>(MbbGoodHope);
            var despair = penalty.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbCrushingDespairBuff);
            MbbExpect(!despair.Harmful, "The native Crushing Despair buff gained a harmful flag.");
            int repointed = MbbReplaceApplications(hope, apply => SetNative(apply, "Buff", despair));
            MbbDiscovery misled = MbbDiscover(penalty, MbbGoodHope);
            MbbExpect(repointed == 1 && !misled.Ability.CanTargetEnemies &&
                misled.Ability.EffectOnAlly == AbilityEffectOnUnit.Harmful &&
                misled.Facts.Effects.Any(e => e.EffectId == MbbCrushingDespairBuff && e.Harmful == false &&
                    (e.GrantedConditions ?? new string[0]).Count == 0),
                "The ally-penalty fixture is not the intended native shape.");
            MbbExpect(misled.Repaired.Disposition == "exclude" &&
                misled.Repaired.ReasonCode == "harmful-ally-disposition" && !misled.Included,
                "A friend-only penalty buff was admitted because the ability cannot target enemies: " +
                misled.Repaired.Reason);
            // 3. The real payloads with their entries are admitted (the
            // positive cases are in the Good Hope and Firebelly tests); the
            // same facts without the entry are not.
            var real = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbFirebelly, MbbFirebellyBuff);
            foreach (string guid in new[] { MbbGoodHope, MbbFirebelly })
            {
                MbbDiscovery admitted = MbbDiscover(real, guid);
                NativeCandidateAuditFacts withoutEntry = admitted.Facts;
                withoutEntry.AllyDispositionExceptionEffectIds = new string[0];
                NativeCandidateAuditDecision bare = new NativeCandidateClassifier().Classify(withoutEntry);
                MbbExpect(admitted.Included && admitted.Repaired.Disposition == "include" &&
                    bare.Disposition == "exclude" && bare.ReasonCode == "harmful-ally-disposition",
                    guid + " is admitted without its evidence-backed entry, or not with it.");
            }
            // 4. The registry keeps the discovered graph for such an entry
            // (it is not an "include" replacement) and validates its shape.
            EffectOverrideRegistry registry = MbbOverrides();
            EffectExpression detected = MbbDiscover(real, MbbGoodHope).Scan.Expression;
            EffectOverrideApplication application = registry.Apply(MbbGoodHope, detected);
            MbbExpect(ReferenceEquals(application.Expression, detected) && application.Entry == null &&
                application.ClassifierException != null &&
                registry.AllyDispositionExceptions(MbbGoodHope).SequenceEqual(new[] { MbbGoodHopeBuff }) &&
                registry.AllyDispositionExceptions(MbbHaste).Count == 0,
                "The ally-disposition entry replaced the discovered graph.");
            bool rejected = false;
            try
            {
                EffectOverrideRegistry.Parse("{\"schemaVersion\":1,\"entries\":[{\"abilityGuid\":\"" + MbbGoodHope +
                    "\",\"disposition\":\"admit-despite-ally-disposition\",\"sourceAssembly\":\"native\"," +
                    "\"effectMode\":\"allOf\",\"effects\":[],\"reason\":\"no payload\"}]}");
            }
            catch (InvalidDataException) { rejected = true; }
            MbbExpect(rejected, "An ally-disposition entry without a named payload was accepted.");
        }

        // Every ContextActionApplyBuff in the ability's run-action graph
        // (through action lists and conditionals) is passed to change.
        private static int MbbReplaceApplications(BlueprintAbility ability,
            Action<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff> change)
        {
            int count = 0;
            Action<Kingmaker.ElementsSystem.ActionList> visit = null;
            visit = list =>
            {
                if (list == null || list.Actions == null) return;
                foreach (Kingmaker.ElementsSystem.GameAction action in list.Actions)
                {
                    var apply = action as Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff;
                    if (apply != null) { change(apply); count++; }
                    var conditional = action as Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional;
                    if (conditional != null) { visit(conditional.IfTrue); visit(conditional.IfFalse); }
                }
            };
            foreach (var run in ability.ComponentsArray
                .OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>())
                visit(run.Actions);
            return count;
        }

        // Review R1-F7: a point-capable allied burst whose action casts a
        // second same-radius allied burst selects around EACH outer
        // recipient; one anchor disk cannot represent that, so it is refused
        // even though its first selector is its own allied
        // AbilityTargetsAround. Built from native Feather Step, Mass (15 ft)
        // casting native Enlarge Person, Mass (15 ft) instead of applying.
        private static void TestMbbNestedBurstRefused()
        {
            string feather = MbbPointGroupSpells[1][1], enlarge = MbbPointGroupSpells[4][1];
            var dump = new NativeBlueprintDump(feather, MbbPointGroupSpells[1][2], enlarge, MbbPointGroupSpells[4][2],
                MbbPointGroupSpells[5][2]);
            BlueprintAbility outer = dump.Get<BlueprintAbility>(feather);
            BlueprintAbility inner = dump.Get<BlueprintAbility>(enlarge);
            int replaced = 0;
            foreach (var run in outer.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>())
            {
                Kingmaker.ElementsSystem.GameAction[] actions = run.Actions.Actions;
                for (int index = 0; index < actions.Length; index++)
                {
                    if (!(actions[index] is Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff)) continue;
                    var cast = (Kingmaker.UnitLogic.Mechanics.Actions.ContextActionCastSpell)
                        FormatterServices.GetUninitializedObject(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionCastSpell));
                    if (cast is UnityEngine.Object) SetNative(cast, "m_CachedPtr", new IntPtr(1));
                    SetNative(cast, "Spell", inner);
                    actions[index] = cast;
                    replaced++;
                }
            }
            MbbExpect(replaced == 1, "Feather Step, Mass no longer applies its buff directly (" + replaced + ").");
            MbbDiscovery nested = MbbDiscover(dump, feather);
            NativeCandidateEffectFacts leaf = nested.Facts.Effects.FirstOrDefault(e => e.EffectId == MbbPointGroupSpells[4][2]);
            MbbExpect(leaf != null && leaf.Target == "AlliedAreaRecipients" &&
                leaf.ActionPath.Split('/')[1] == "0:AbilityTargetsAround" &&
                // The cast is recorded as the referenced ability, whose own
                // allied selector follows.
                leaf.ActionPath.Split('/').Count(segment => segment == "0:AbilityTargetsAround") == 2 &&
                leaf.ActionPath.Contains("/0:" + enlarge + "/"),
                "The nested fixture is not an outer allied burst casting an inner one: " +
                (leaf == null ? "no leaf" : leaf.ActionPath));
            AlliedAreaCoverage outerArea = new KingmakerAreaCoverageResolver().Resolve(outer);
            AlliedAreaCoverage innerArea = new KingmakerAreaCoverageResolver().Resolve(inner);
            MbbExpect(outerArea != null && innerArea != null && Math.Abs(outerArea.Radius - innerArea.Radius) < 0.001f,
                "The nested bursts no longer share one radius.");
            MbbExpect(nested.Repaired.Disposition == "exclude" &&
                nested.Repaired.ReasonCode == "point-target-without-placement" && !nested.Included,
                "A nested allied burst was admitted as a single unit-anchored area: " + nested.Repaired.Reason);
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
                // Production provider, pool and option for a caster who
                // knows the spell; every friendly unit is a native-legal
                // centre in this case (the injected CanTarget verdict).
                MbbGroup group = MbbGroupCaster(dump, row[1], positions, id => true);
                var snapshot = group.Snapshot;
                var options = group.Options;
                var effects = group.Effects;
                AbilityKey ability = group.Ability;
                MbbExpect(options.Single().CoveredTargetIdsForAnchor("anchor").SequenceEqual(new[] { "anchor", "beside" }) &&
                    options.Single().LegalAnchorIds.SequenceEqual(positions.Keys.OrderBy(v => v, StringComparer.Ordinal)),
                    row[0] + "'s production anchors or per-anchor recipients are wrong.");
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
            // Planned on the same anchor in one routine, the later casting
            // would strip the earlier one's buff from the shared recipients:
            // it is blocked (removal of an allied-area recipient's effect),
            // reserves nothing, and the earlier casting stays Ready.
            float massRadius = 15f * 0.3048f;
            var massPositions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "ally", massRadius * 0.5f }, { "far", massRadius * 4f }
            };
            MbbParty mass = MbbMassParty(dump, new Dictionary<string, int> { { "caster", 4 } },
                new Dictionary<string, string[]> { { "caster", new[] { enlarge, reduce } } }, massPositions);
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            ExplicitCastingPlan opposed = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("enlarge", "long", "caster", enlargeKey.Canonical, enlargeKey,
                        new[] { "caster", "ally" }),
                    GroupCasting("reduce", "long", "caster", reduceKey.Canonical, reduceKey,
                        new[] { "caster", "ally" })),
                mass.Snapshot, mass.Options, mass.Effects, new CastEnhancementSnapshot[0]);
            ResolvedCasting enlarged = opposed.Castings.Single(c => c.CastingId == "enlarge");
            ResolvedCasting reduced = opposed.Castings.Single(c => c.CastingId == "reduce");
            MbbExpect(enlarged.Readiness == ResolvedCastingReadiness.Ready &&
                enlarged.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                    .SequenceEqual(new[] { "ally", "caster" }) &&
                reduced.Readiness == ResolvedCastingReadiness.Blocked &&
                reduced.ReadinessReasons.Any(r => r.StartsWith(ExplicitCastingCompiler.RemovesPlannedEffect +
                    ":enlarge:" + MbbPointGroupSpells[4][2] + ":", StringComparison.Ordinal)) &&
                opposed.Castings.Where(c => c.Readiness == ResolvedCastingReadiness.Ready)
                    .SelectMany(c => c.Cost).Sum(c => c.Units) == 1,
                "Enlarge and Reduce Person, Mass were planned together on the same allies: " + reduced.Readiness + " " +
                string.Join(",", reduced.ReadinessReasons));
        }

        // Owner addendum §9.3: whether native AbilityData.CanTarget admits a
        // unit target for these point-anchored spells (their native
        // TargetAnchor is Point) is NOT verified in game. Everything that
        // depends on it fails closed. The provider option builder offers only
        // the units native CanTarget admits (LegalAnchorIds; an allied-area
        // effect has no caster fallback), so a refusal leaves no anchor, no
        // recipients and no reachable unit - the exact option shape below.
        // The compiler then refuses with a worded reason and reserves nothing,
        // and a step whose anchor the native check refuses at cast time is
        // never submitted by either execution engine (the adapters' Validate
        // re-checks CanTarget on the step's AnchorUnitId: "target-invalid").
        private static void TestMbbPointGroupAnchorFailsClosed()
        {
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Take(4)
                .SelectMany(row => new[] { row[1], row[2] }).ToArray());
            foreach (string[] row in MbbPointGroupSpells.Take(4))
            {
                MbbDiscovery spell = MbbDiscover(dump, row[1]);
                MbbExpect(spell.Included, row[0] + " is not discovered: " + spell.Reason);
                float radius = float.Parse(row[3]) * 0.3048f;
                var positions = new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { "caster", 0f }, { "anchor", radius * 2f }, { "beside", radius * 2.5f }
                };
                // The production option builder's decisions under each
                // injected native CanTarget verdict.
                Func<IEnumerable<string>, ProviderPlanningOption> optionFor = legal =>
                {
                    var admitted = new HashSet<string>(legal, StringComparer.Ordinal);
                    return MbbGroupCaster(dump, row[1], positions, admitted.Contains).Options.Single();
                };
                MbbGroup group = MbbGroupCaster(dump, row[1], positions, id => true);
                var snapshot = group.Snapshot;
                var effects = group.Effects;
                AbilityKey ability = group.Ability;
                Func<ProviderPlanningOption, PlannedCasting, ExplicitCastingPlan> compile = (option, casting) =>
                    new ExplicitCastingCompiler().Compile(CastingDocument(casting), snapshot,
                        new List<ProviderPlanningOption> { option }, effects, new CastEnhancementSnapshot[0]);

                // 1. Native CanTarget admits no unit: neither an anchored nor a
                // caster-centred casting is planned, nothing is reserved,
                // nobody is claimed and no step is produced.
                ProviderPlanningOption none = optionFor(new string[0]);
                // The production decision has no caster fallback for an
                // allied-area effect (unlike a party effect).
                MbbExpect(none.LegalAnchorIds.Count == 0 && none.ReachableTargetIds.Count == 0 &&
                    none.RecipientIdsByAnchor.Count == 0,
                    row[0] + " acquired an anchor although native targeting admitted none.");
                foreach (PlannedCasting casting in new[]
                {
                    GroupCasting("group", "long", "caster", ability.Canonical, ability, new[] { "anchor" },
                        null, CastingTargetMode.AnchoredOrigin, "anchor"),
                    GroupCasting("group", "long", "caster", ability.Canonical, ability, new[] { "caster" })
                })
                {
                    ExplicitCastingPlan plan = compile(none, casting);
                    ResolvedCasting resolved = plan.Castings.Single();
                    string expected = casting.TargetMode == CastingTargetMode.AnchoredOrigin
                        ? "origin-anchor-illegal:anchor" : "origin-caster-illegal:caster";
                    MbbExpect(resolved.Readiness == ResolvedCastingReadiness.Blocked && resolved.Cost.Count == 0 &&
                        resolved.PredictedBeneficiaryUnitIds.Count == 0 &&
                        resolved.ReadinessReasons.Contains(expected),
                        row[0] + " was planned although native targeting admitted no unit: " + resolved.Readiness + " " +
                        string.Join(",", resolved.ReadinessReasons));
                    string reasonCode = expected.Substring(0, expected.IndexOf(':'));
                    MbbExpect(WorkspaceReasonText.Describe(expected) == PlannerText.Get("reason." + reasonCode),
                        row[0] + "'s refusal has no truthful player wording: " + WorkspaceReasonText.Describe(expected));
                    CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
                    ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, decision,
                        new List<ProviderPlanningOption> { none }, effects);
                    MbbExpect(!steps.Converted || steps.Plan.Steps.Count == 0,
                        row[0] + " produced a cast step without a native-legal anchor.");
                }

                // 2. Native CanTarget admits only the caster: the authored
                // anchor stays refused (no fallback to another centre); a
                // caster-centred casting covers only the caster's own radius.
                ProviderPlanningOption casterOnly = optionFor(new[] { "caster" });
                ResolvedCasting refused = compile(casterOnly, GroupCasting("group", "long", "caster", ability.Canonical,
                    ability, new[] { "anchor" }, null, CastingTargetMode.AnchoredOrigin, "anchor")).Castings.Single();
                ResolvedCasting centred = compile(casterOnly, GroupCasting("group", "long", "caster", ability.Canonical,
                    ability, new[] { "caster", "anchor" })).Castings.Single();
                MbbExpect(refused.Readiness == ResolvedCastingReadiness.Blocked && refused.Cost.Count == 0 &&
                    refused.ReadinessReasons.Contains("origin-anchor-illegal:anchor"),
                    row[0] + " fell back to another centre for a refused anchor: " +
                    string.Join(",", refused.ReadinessReasons));
                MbbExpect(centred.Readiness == ResolvedCastingReadiness.Ready &&
                    centred.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "caster" }) &&
                    centred.Cost.Sum(c => c.Units) == 1,
                    row[0] + "'s caster-centred casting claimed out-of-radius allies: " +
                    string.Join(",", centred.PredictedBeneficiaryUnitIds));

                // 3. Legal while planning, refused by native CanTarget at cast
                // time: both engines record the refusal and never submit the
                // cast, spend nothing and confirm nothing.
                ProviderPlanningOption anchored = optionFor(positions.Keys);
                ExplicitCastingPlan ready = compile(anchored, GroupCasting("group", "long", "caster", ability.Canonical,
                    ability, new[] { "anchor", "beside" }, null, CastingTargetMode.AnchoredOrigin, "anchor"));
                CastingApplyDecision go = new CastingExecutionGate().Evaluate(ready, CastingApplyMode.Ordinary, "long");
                ExplicitStepConversion converted = ExplicitCastingStepConverter.Convert(ready, go,
                    new List<ProviderPlanningOption> { anchored }, effects);
                MbbExpect(converted.Converted && converted.Plan.Steps.Count == 1 &&
                    converted.Plan.Steps[0].MassCast && converted.Plan.Steps[0].AnchorUnitId == "anchor",
                    row[0] + " did not convert to one anchored mass step: " + converted.Refusal);
                var instant = new MbbRefusingRuntime();
                var instantReport = new ExecutionReport(converted.Plan);
                MbbDrain(new InstantCastExecutor(instant, true).Execute(converted.Plan, instantReport));
                var animated = new MbbRefusingRuntime();
                var animatedReport = new ExecutionReport(converted.Plan);
                MbbDrain(new AnimatedCastExecutor(animated, true).Execute(converted.Plan, animatedReport));
                foreach (var engine in new[]
                {
                    new KeyValuePair<MbbRefusingRuntime, ExecutionReport>(instant, instantReport),
                    new KeyValuePair<MbbRefusingRuntime, ExecutionReport>(animated, animatedReport)
                })
                {
                    MbbExpect(engine.Key.ValidatedAnchors.SequenceEqual(new[] { "anchor" }) &&
                        engine.Key.Submissions == 0 && engine.Value.ResourcesSpent == 0 &&
                        engine.Value.Confirmed == 0 &&
                        engine.Value.Records.Any(r => r.Status == CastExecutionStatus.FailedValidation &&
                            r.Detail == "target-invalid") &&
                        !engine.Value.Records.Any(r => r.Status == CastExecutionStatus.Submitted ||
                            r.Status == CastExecutionStatus.CastStarted || r.Status == CastExecutionStatus.Queued),
                        row[0] + " was cast although native targeting refused its unit anchor.");
                }
            }
        }

        private static void MbbDrain(System.Collections.IEnumerator run)
        {
            int moves = 0;
            while (run.MoveNext())
                if (++moves > 50) throw new InvalidOperationException("Executor did not terminate.");
        }

        // Stands in for the Kingmaker adapters when native AbilityData.CanTarget
        // refuses the resolved anchor: their Validate returns "target-invalid"
        // for a non-sticky step (KingmakerAnimatedCastAdapter.Validate,
        // KingmakerInstantCastAdapter.Validate).
        private sealed class MbbRefusingRuntime : ICastRuntimeAdapter, IInstantCastRuntimeAdapter
        {
            internal readonly List<string> ValidatedAnchors = new List<string>();
            internal int Submissions;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step)
            {
                ValidatedAnchors.Add(step.MassCast ? step.AnchorUnitId : step.TargetUnitIds.FirstOrDefault());
                return CastRuntimeValidation.Fail("target-invalid");
            }
            public IAnimatedCastOperation StartAnimated(CastStep step) { Submissions++; return null; }
            public InstantCastResult Fire(CastStep step) { Submissions++; return null; }
            public bool EffectsObserved(CastStep step) { return false; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("fixture"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("fixture"); }
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
            // The committed verbatim entry (review R1-F9; the explicit verify
            // check proves it equals the export).
            JObject entry = JObject.Parse(File.ReadAllText(MbbDanceFixture));
            MbbExpect((string)entry["abilityGuid"] == MbbIrresistibleDanceCast, "The Dance fixture is not the export entry.");
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
