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
        private const string MbbPrayer = "faabd2cc67efa4646ac58c7bb3e40fcc";
        private const string MbbPrayerBuff = "789bae3802e7b6b4c8097aaf566a1cf5";
        private const string MbbPrayerDebuff = "890182fa30a5f724c86ce41f237cf95f";
        // The relation gate keys (RecipientFactGate.EnemyOfCaster /
        // AllyOfCaster), spelled out so the helpers also compile against the
        // preceding candidate for the executed baseline.
        private const string MbbEnemyOfCaster = "relation:enemy-of-caster";
        private const string MbbAllyOfCaster = "relation:ally-of-caster";

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
            Mbb("missing-beneficial-buffs-structural-skip-protects-its-effect", TestMbbStructuralSkipProtects),
            Mbb("missing-beneficial-buffs-one-pass-grants-only-guaranteed-effects", TestMbbOnePassConditionalGrant),
            Mbb("missing-beneficial-buffs-classic-planner-refuses-exclusive-choices", TestMbbClassicExclusion),
            Mbb("missing-beneficial-buffs-classic-sequence-forecast-applies-removals", TestMbbClassicSequenceRemoval),
            Mbb("missing-beneficial-buffs-classic-picks-a-conflict-free-centre", TestMbbClassicConflictFreeCentre),
            Mbb("missing-beneficial-buffs-area-coverage-follows-recipient-conditions", TestMbbRecipientConditions),
            Mbb("missing-beneficial-buffs-removal-only-ally-is-counted", TestMbbRemovalOnlyAllyCounted),
            Mbb("missing-beneficial-buffs-classic-sequence-uses-bound-net-changes", TestMbbClassicSequenceBoundRemoval),
            Mbb("missing-beneficial-buffs-offensive-branch-refuses-point-admission", TestMbbOffensiveReachRefused),
            Mbb("missing-beneficial-buffs-payload-activation-damage-refuses-ally-exception", TestMbbActivationDamageRefused),
            Mbb("missing-beneficial-buffs-unaffected-animal-creates-no-conflict", TestMbbUnaffectedAnimalNoConflict),
            Mbb("missing-beneficial-buffs-blocked-mixed-casting-notes-do-not-promise-a-cast", TestMbbBlockedMixedNotes),
            Mbb("missing-beneficial-buffs-caster-only-gate-controls-area-coverage", TestMbbCasterOnlyGate),
            Mbb("missing-beneficial-buffs-point-burst-harmful-buff-refused", TestMbbPointHarmfulBuffRefused),
            Mbb("missing-beneficial-buffs-payload-activation-debuff-refuses-ally-exception", TestMbbActivationDebuffRefused),
            Mbb("missing-beneficial-buffs-mixed-caster-recipient-condition-withholds-coverage", TestMbbMixedConditionWithheld),
            Mbb("missing-beneficial-buffs-skipped-mass-casting-protects-only-its-recipients", TestMbbSkipProtectsOnlyRecipients),
            Mbb("missing-beneficial-buffs-sequential-forecast-follows-opposing-casts", TestMbbSequentialOpposingCoverage),
            Mbb("missing-beneficial-buffs-classic-sequential-forecast-follows-opposing-casts",
                TestMbbClassicSequentialOpposingCoverage),
            Mbb("missing-beneficial-buffs-unfunded-mixed-casting-notes-do-not-promise-a-cast", TestMbbUnfundedMixedNotes),
            Mbb("missing-beneficial-buffs-removed-earlier-note-names-only-the-affected-ally", TestMbbRemovedEarlierNoteIsolated),
            Mbb("missing-beneficial-buffs-native-condition-contracts-pinned", TestMbbNativeConditionContracts),
            Mbb("missing-beneficial-buffs-point-burst-penalty-buff-refused", TestMbbPointPenaltyBuffRefused),
            Mbb("missing-beneficial-buffs-payload-activation-penalty-refuses-ally-exception", TestMbbActivationPenaltyRefused),
            Mbb("missing-beneficial-buffs-sequential-forecast-empty-coverage-clears-beneficiaries",
                TestMbbSequentialEmptyCoverage),
            Mbb("missing-beneficial-buffs-exhausted-mixed-casting-notes-do-not-promise-a-cast", TestMbbExhaustedMixedNotes),
            Mbb("missing-beneficial-buffs-prayer-friendly-recipients-keep-coverage", TestMbbPrayerFriendlyCoverage),
            Mbb("missing-beneficial-buffs-good-hope-despair-read-is-the-native-buff-check", TestMbbGoodHopeDespairRead),
            Mbb("missing-beneficial-buffs-empty-checker-follows-the-branch-that-runs", TestMbbEmptyCheckerBranch),
            Mbb("missing-beneficial-buffs-kill-action-refuses-both-exceptions", TestMbbKillActionRefused),
            Mbb("missing-beneficial-buffs-personal-penalty-payload-refused", TestMbbPersonalPenaltyRefused),
            Mbb("missing-beneficial-buffs-resistance-reselect-in-tier-is-free", TestMbbResistanceReselectInTier),
            Mbb("missing-beneficial-buffs-prayer-penalty-never-covers-a-friendly-ally", TestMbbPrayerPenaltyNotCoverage),
            Mbb("missing-beneficial-buffs-empty-checker-unreachable-removal-is-no-conflict",
                TestMbbEmptyCheckerRemovalNoConflict),
            Mbb("missing-beneficial-buffs-sequential-good-hope-gains-allies", TestMbbSequentialGoodHopeGainsAllies),
            Mbb("missing-beneficial-buffs-classic-sequential-good-hope-gains-allies",
                TestMbbClassicSequentialGoodHopeGainsAllies),
            Mbb("missing-beneficial-buffs-incomplete-scan-refuses-both-exceptions", TestMbbIncompleteScanRefused),
            Mbb("missing-beneficial-buffs-firebrand-needs-its-own-caster", TestMbbFirebrandCasterOrigin),
            Mbb("missing-beneficial-buffs-personal-rescue-judges-the-whole-cast", TestMbbPersonalRescueWhole),
            Mbb("missing-beneficial-buffs-same-routine-good-hope-gains-allies", TestMbbSameRoutineGoodHopeGainsAllies),
            Mbb("missing-beneficial-buffs-classic-prayer-follows-the-friendly-branch", TestMbbClassicPrayerBranch),
            Mbb("missing-beneficial-buffs-empty-checker-presence-follows-the-true-branch", TestMbbEmptyCheckerPresence),
            Mbb("missing-beneficial-buffs-unreadable-anchor-inherits-nothing", TestMbbUnreadableAnchor),
            Mbb("missing-beneficial-buffs-unread-buff-refuses-point-admission", TestMbbUnreadBuffRefused),
            Mbb("missing-beneficial-buffs-caster-directed-rescue-judges-the-whole-cast", TestMbbCasterDirectedRescueWhole),
            Mbb("missing-beneficial-buffs-firebrand-other-caster-overlap-invalidates-grant", TestMbbFirebrandOverlap),
            Mbb("missing-beneficial-buffs-satisfied-good-hope-gains-a-newly-eligible-ally",
                TestMbbSatisfiedGoodHopeGainsAlly),
            Mbb("missing-beneficial-buffs-classic-prayer-skip-protects-its-branch", TestMbbClassicPrayerSkipBranch),
            Mbb("missing-beneficial-buffs-failed-geometry-read-keeps-anchors-separate", TestMbbFailedGeometryRead),
            Mbb("missing-beneficial-buffs-repeated-resistance-selection-costs-one-use", TestMbbResistanceRepeat),
            Mbb("missing-beneficial-buffs-firebrand-confirmation-needs-the-submitted-caster",
                TestMbbFirebrandConfirmationCaster),
            Mbb("missing-beneficial-buffs-prayer-hostile-member-is-no-beneficiary", TestMbbPrayerHostileMember),
            Mbb("missing-beneficial-buffs-empty-checker-unreachable-predicate-keeps-coverage",
                TestMbbEmptyCheckerUnreachablePredicate),
            Mbb("missing-beneficial-buffs-classic-firebrand-uses-effective-instances", TestMbbClassicFirebrandInstances),
            Mbb("missing-beneficial-buffs-ally-selector-honours-the-caster-relation", TestMbbAllySelectorRelation),
            Mbb("missing-beneficial-buffs-weapon-delivery-refuses-point-admission", TestMbbWeaponDeliveryRefused),
            Mbb("missing-beneficial-buffs-firebelly-reaches-the-castable-catalogue", TestMbbFirebellyCatalogue),
            Mbb("missing-beneficial-buffs-classic-suppressed-good-hope-is-no-coverage", TestMbbClassicSuppressedGoodHope),
            Mbb("missing-beneficial-buffs-firebrand-live-skip-after-other-caster-recasts", TestMbbFirebrandLiveOverlap),
            Mbb("missing-beneficial-buffs-personal-neutral-disposition-rescue-is-whole-cast",
                TestMbbPersonalNeutralRescueWhole),
            Mbb("missing-beneficial-buffs-classic-sequence-keeps-inherited-coverage", TestMbbClassicInheritedCoverage),
            Mbb("missing-beneficial-buffs-classic-repeated-resistance-costs-one-use", TestMbbClassicResistanceRepeat),
            Mbb("missing-beneficial-buffs-classic-prayer-confirmation-needs-the-friendly-buff",
                TestMbbClassicPrayerConfirmation),
            Mbb("missing-beneficial-buffs-empty-checker-unreachable-relation-keeps-coverage",
                TestMbbEmptyCheckerUnreachableRelation),
            Mbb("missing-beneficial-buffs-classic-sequence-firebrand-replaces-other-caster",
                TestMbbClassicSequenceFirebrand),
            Mbb("missing-beneficial-buffs-unread-ally-relation-never-becomes-definite", TestMbbUnreadAllyNeverDefinite),
            Mbb("missing-beneficial-buffs-firebrand-reassessment-uses-landing-units", TestMbbFirebrandLandingUnits),
            Mbb("missing-beneficial-buffs-known-outer-condition-makes-relation-unreachable",
                TestMbbOuterConditionRelation),
            Mbb("missing-beneficial-buffs-classic-firebrand-possible-replacement-invalidates",
                TestMbbClassicPossibleReplacement),
            Mbb("missing-beneficial-buffs-on-cast-action-refuses-point-admission", TestMbbOnCastActionRefused),
            Mbb("missing-beneficial-buffs-classic-mass-centre-covers-most-requested", TestMbbClassicBestCentre),
            Mbb("missing-beneficial-buffs-saved-prayer-forecast-uses-complete-expression",
                TestMbbSavedPrayerForecastExpression),
            Mbb("missing-beneficial-buffs-personal-cast-needs-live-owner-verdict", TestMbbPersonalOwnerVerdict),
            Mbb("missing-beneficial-buffs-area-boundary-is-the-recorded-radius", TestMbbAreaBoundary),
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
            RunMissingBeneficialBuffsRecoveryTests();
            RunMissingBeneficialBuffsRecoveryExportTests();
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
        // builder computes them (anchor.DistanceTo(unit) <= radius; review
        // R11-F5: no margin).
        private static Dictionary<string, IEnumerable<string>> MbbRecipients(IDictionary<string, float> positions,
            float radius)
        {
            return positions.ToDictionary(anchor => anchor.Key, anchor => (IEnumerable<string>)positions
                .Where(unit => Math.Abs(unit.Value - anchor.Value) <= radius).Select(unit => unit.Key)
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
            IDictionary<string, float> positions, Func<string, string, bool> unitHasFact = null)
        {
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(provider.Key.Ability.BaseAbilityGuid);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            // Injected live fact reads (as KingmakerProviderOptionBuilder
            // reads them): the caster has none of the gated caster facts and
            // every fixture ally is a humanoid with none of the gated
            // recipient facts (creature types, opposing buffs).
            var casterFacts = EffectConditionBinding.FactIds(expression)
                .ToDictionary(id => id, id => false, StringComparer.Ordinal);
            ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units, true,
                false, area, nativeCanTarget, MbbDistance(positions), casterFacts,
                // Every fixture unit is a friendly party member: the native
                // relation reads (IsEnemy / IsAlly with the caster) are false /
                // true; other keys come from the injected facts.
                (unit, fact) => fact == MbbEnemyOfCaster ? false : fact == MbbAllyOfCaster ? true
                    : unitHasFact != null && unitHasFact(unit, fact),
                // As the builder decides it (NativeRecipientFactRead.IsFixed):
                // a relation to the caster, or a recorded unit fact that is not
                // a buff (a creature type), is fixed; anything else is not.
                fact =>
                {
                    if (fact == MbbEnemyOfCaster || fact == MbbAllyOfCaster) return true;
                    string type = NativeBlueprintDump.RecordedType(fact);
                    return type != null && !type.StartsWith("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff",
                        StringComparison.Ordinal);
                });
            return ProviderTargetingPlanner.BuildOption(provider, decision, CastExecutionStrategy.DirectRuleCast,
                "ordinary-direct-rule-cast", casterFacts);
        }

        // Review R10-F1: the production reader (NativeRecipientFactRead) also
        // answers the caster relation; every fixture unit is a friendly party
        // member (IsEnemy false, IsAlly true). Other keys: the given reads.
        private static Func<string, string, bool?> MbbFriendlyReads(Func<string, string, bool?> facts)
        {
            return (unit, fact) => fact == MbbEnemyOfCaster ? false : fact == MbbAllyOfCaster ? true
                : facts == null ? (bool?)null : facts(unit, fact);
        }

        private static MbbParty MbbBardParty(NativeBlueprintDump dump, params string[] knownLevelThreeSpells)
        {
            return MbbBardPartyWithSlots(dump, 2, knownLevelThreeSpells);
        }

        private static MbbParty MbbBardPartyWithSlots(NativeBlueprintDump dump, int remainingSlots,
            params string[] knownLevelThreeSpells)
        {
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 0f }, { "ally-near", 3f }, { "ally-far", 10f }
            };
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            // The bard's level-3 spontaneous slots (remainingSlots of 2 left).
            ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool("bard", MbbBardBook, 3,
                remainingSlots, 2);
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
            // Review R2 (R1-F1): the coverage notes the inspector shows agree
            // with the Blocked status - no "already active (skipped)" note,
            // and the earlier removal is named instead.
            List<string> shown = kept.ExistingEffectNotes.Select(note =>
                CastingRunPresentation.DescribeExistingEffectNote(note)).ToList();
            string skipped = PlannerText.Get("run.effect.active", "wizard");
            MbbExpect(shown.Count != 0 && !shown.Contains(skipped) &&
                shown.Contains(PlannerText.Get("run.effect.removed", "wizard")) &&
                !kept.ExistingEffectNotes.Any(note => note.StartsWith("already-active:", StringComparison.Ordinal)),
                "The inspector still reports the invalidated Fire selection as skipped: " + string.Join(" | ", shown));
            MbbExpect(switched.ExistingEffectNotes.All(note => !note.StartsWith("already-active:", StringComparison.Ordinal)),
                "The Cold switch carries a skip note.");
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
            IDictionary<string, string[]> knownByCaster, IDictionary<string, float> positions,
            IDictionary<string, Func<string, bool>> canTargetBySpell = null,
            Func<string, string, bool> unitHasFact = null)
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
            // Native CanTarget admits every friendly unit as a centre unless
            // a per-spell verdict is injected.
            party.Options = providers.Select(provider =>
            {
                Func<string, bool> verdict;
                if (canTargetBySpell == null ||
                    !canTargetBySpell.TryGetValue(provider.Key.Ability.BaseAbilityGuid, out verdict))
                    verdict = id => true;
                return MbbOption(dump, provider, party.Effects[provider.Key.Ability.Canonical], units, verdict,
                    positions, unitHasFact);
            }).ToList();
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
            // Review R2 (R1-F3), reversed order: E is active; an admitted
            // overwrite casting may remove E (one alternative) or apply it
            // (the other); a later skip-if-active request for E must not
            // claim satisfaction. A certain re-application keeps it.
            ActiveEffectSnapshot eActive = MbbActive("t", e);
            Func<string, ResolvedCasting> keptAfter = first =>
                new ExplicitCastingCompiler().Compile(CastingDocument(
                        DirectCasting(first, "long", "c", "t", keys[first].Canonical, keys[first], null, null,
                            CastingAuthoringState.Ready, ExistingEffectPolicy.Overwrite),
                        DirectCasting("p", "long", "c", "t", keys["p"].Canonical, keys["p"])),
                    snapshot, optionsWith(null), effects, new CastEnhancementSnapshot[0], null, null, false, eActive)
                    .CastingById("p");
            ResolvedCasting afterAlternative = keptAfter("alternative");
            ResolvedCasting afterRestored = keptAfter("restored");
            MbbExpect(afterAlternative.Readiness == ResolvedCastingReadiness.Blocked &&
                afterAlternative.ReadinessReasons.Contains(ExplicitCastingCompiler.EffectRemovedByEarlierCasting +
                    ":alternative:" + e + ":t"),
                "A possible re-application cancelled a recorded removal: " + afterAlternative.Readiness + " " +
                string.Join(",", afterAlternative.ReadinessReasons));
            MbbExpect(afterRestored.Readiness == ResolvedCastingReadiness.AlreadySatisfied,
                "A certain re-application did not keep the effect: " + afterRestored.Readiness + " " +
                string.Join(",", afterRestored.ReadinessReasons));
        }

        // Review R1-F4: the one-pass forecast over successive routines must
        // not skip a repeat whose earlier coverage an intervening opposing
        // cast removed.
        // Since review round 2 only a guaranteed application is a structural
        // grant, so the opposing pair is two Abjuration selections (each
        // applies its own resistance unconditionally, then removes the other
        // four): Fire, then Cold, then Fire again in successive routines.
        private static void TestMbbOnePassRemoval()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbParty party = MbbWizardParty(dump, 3);
            AbilityKey fire = party.Abilities["Fire"], cold = party.Abilities["Cold"];
            Func<string, string, AbilityKey, PlannedCasting> select = (id, routine, key) =>
                DirectCasting(id, routine, "wizard", "wizard", key.Canonical, key);
            CastingForecast opposed = new CastingForecastService().ForecastOnePass(CastingDocument(
                    select("fire-1", "long", fire), select("cold", "important", cold),
                    select("fire-2", "short", fire)),
                party.Snapshot, party.Options, party.Effects);
            ResolvedCasting repeat = opposed.Plan.CastingById("fire-2");
            MbbExpect(repeat.Readiness == ResolvedCastingReadiness.Ready && repeat.Cost.Sum(c => c.Units) == 1 &&
                opposed.Plan.CastingById("cold").Readiness == ResolvedCastingReadiness.Ready &&
                opposed.Plan.Castings.SelectMany(c => c.Cost).Sum(c => c.Units) == 3,
                "The one-pass forecast skipped Fire after Cold removed it: " + repeat.Readiness + " " +
                string.Join(",", repeat.ReadinessReasons));
            // Without the opposing routine the structural repeat is still
            // recognized (a guaranteed grant).
            CastingForecast repeated = new CastingForecastService().ForecastOnePass(CastingDocument(
                    select("fire-1", "long", fire), select("fire-2", "short", fire)),
                party.Snapshot, party.Options, party.Effects);
            MbbExpect(repeated.Plan.CastingById("fire-2").Readiness == ResolvedCastingReadiness.AlreadySatisfied &&
                repeated.Plan.CastingById("fire-2").Cost.Count == 0,
                "The structural repeat projection regressed.");
        }

        // Review R2-F2: a proven structural repeat protects the effect it
        // keeps for its own routine, exactly as a live already-active skip.
        private static void TestMbbStructuralSkipProtects()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbParty party = MbbWizardParty(dump, 3);
            AbilityKey fire = party.Abilities["Fire"], cold = party.Abilities["Cold"];
            string fireBuff = MbbAbjuration[3][2];
            Func<string, string, AbilityKey, PlannedCasting> select = (id, routine, key) =>
                DirectCasting(id, routine, "wizard", "wizard", key.Canonical, key);
            CastingForecast both = new CastingForecastService().ForecastOnePass(CastingDocument(
                    select("fire-1", "long", fire), select("fire-2", "important", fire),
                    select("cold", "important", cold)),
                party.Snapshot, party.Options, party.Effects);
            ResolvedCasting kept = both.Plan.CastingById("fire-2");
            ResolvedCasting removing = both.Plan.CastingById("cold");
            MbbExpect(kept.Readiness == ResolvedCastingReadiness.AlreadySatisfied && kept.Cost.Count == 0 &&
                removing.Readiness == ResolvedCastingReadiness.Blocked && removing.Cost.Count == 0 &&
                removing.ReadinessReasons.Contains(ExplicitCastingCompiler.RemovesPlannedEffect + ":fire-2:" +
                    fireBuff + ":wizard"),
                "A structural skip left its effect unprotected in its routine: " + kept.Readiness + " / " +
                removing.Readiness + " " + string.Join(",", removing.ReadinessReasons));
            CastingForecast switchOnly = new CastingForecastService().ForecastOnePass(CastingDocument(
                    select("fire-1", "long", fire), select("cold", "important", cold)),
                party.Snapshot, party.Options, party.Effects);
            MbbExpect(switchOnly.Plan.CastingById("cold").Readiness == ResolvedCastingReadiness.Ready &&
                switchOnly.Plan.CastingById("cold").Cost.Sum(c => c.Units) == 1,
                "A routine containing only the switch was refused.");
        }

        // Review R4-F1/R4-F2: an offensive action anywhere the cast reaches
        // refuses the narrow admissions - damage on another branch of a
        // point-capable allied burst (Mass Blessing's empty construct
        // branch), and damage the approved Good Hope buff itself runs on its
        // bearer when it activates. Unmodified spells stay admitted.
        private static void TestMbbOffensiveReachRefused()
        {
            string blessing = MbbPointGroupSpells[0][1];
            var burstDump = new NativeBlueprintDump(blessing, MbbPointGroupSpells[0][2]);
            BlueprintAbility burst = burstDump.Get<BlueprintAbility>(blessing);
            MbbExpect(MbbAppendToFirstConditionalTrue(burst,
                    NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>()),
                "Mass Blessing's construct branch could not be extended.");
            MbbDiscovery damagingBurst = MbbDiscover(burstDump, blessing);
            var burstProviders = new List<ProviderSnapshot>();
            ProviderSnapshot burstProvider = MbbAdmission(burstDump).AdmitSpell("caster", "book", false, "Book",
                MbbRead(burstDump, blessing, blessing, SourceKind.Spellbook, 17), 6, 0, "pool", 1, new string[0],
                burstProviders);
            MbbExpect(damagingBurst.Scan.Diagnostics.Any(d => d.Code == "offensive-action") &&
                damagingBurst.Repaired.Disposition == "exclude" && !damagingBurst.Included && burstProvider == null,
                "A point-capable allied burst that damages constructs was admitted: " + damagingBurst.Repaired.Reason);
            MbbExpect(MbbDiscover(new NativeBlueprintDump(blessing, MbbPointGroupSpells[0][2]), blessing).Included,
                "Unmodified Mass Blessing is no longer admitted.");
        }

        private static void TestMbbActivationDamageRefused()
        {
            var buffDump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            var buff = buffDump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbGoodHopeBuff);
            var activation = NativeObject<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>();
            SetNative(activation, "Activated", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[]
                {
                    NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>()
                }
            });
            SetNative(activation, "Deactivated", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            SetNative(activation, "NewRound", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            SetNative(buff, "Components", (buff.ComponentsArray ?? new Kingmaker.Blueprints.BlueprintComponent[0])
                .Concat(new Kingmaker.Blueprints.BlueprintComponent[] { activation }).ToArray());
            MbbDiscovery selfHarm = MbbDiscover(buffDump, MbbGoodHope);
            var hopeProviders = new List<ProviderSnapshot>();
            ProviderSnapshot hopeProvider = MbbAdmission(buffDump).AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(buffDump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, "pool", 1,
                new string[0], hopeProviders);
            MbbExpect(selfHarm.Facts.Effects.Any(e4 => e4.EffectId == MbbGoodHopeBuff && e4.Harmful == false &&
                    (e4.FactActions ?? new NativeCandidateFactActions[0]).Any(list =>
                        list.List == "Activated" && (list.Offensive ?? new string[0]).Count != 0)),
                "The exporter did not record the buff's activation damage.");
            MbbExpect(selfHarm.Repaired.Disposition == "exclude" && !selfHarm.Included && hopeProvider == null,
                "Good Hope was admitted although its buff damages its bearer on activation: " + selfHarm.Repaired.Reason);
        }

        // Review R4-F3: two bursts reach different humanoids and overlap
        // only on an animal companion; neither Enlarge nor Reduce Person,
        // Mass does anything to an animal (a fixed creature type), so both
        // casts are planned, each with one slot and its own beneficiary.
        private static void TestMbbUnaffectedAnimalNoConflict()
        {
            const string animalType = "a95311b3dc996964cbaa30ff9965aaf6";
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float r = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "w", 10f * r }, { "h1", 0f }, { "pet", 0.8f * r }, { "h2", 1.6f * r }
            };
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                new Dictionary<string, string[]> { { "w", new[] { enlarge, reduce } } }, positions, null,
                (unit, fact) => unit == "pet" && fact == animalType);
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("enlarge", "long", "w", enlargeKey.Canonical, enlargeKey, new[] { "h1" },
                        null, CastingTargetMode.AnchoredOrigin, "h1"),
                    GroupCasting("reduce", "long", "w", reduceKey.Canonical, reduceKey, new[] { "h2" },
                        null, CastingTargetMode.AnchoredOrigin, "h2")),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0]);
            ResolvedCasting first = plan.CastingById("enlarge"), second = plan.CastingById("reduce");
            MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(c => c.Units) == 1 &&
                first.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "h1" }) &&
                second.Readiness == ResolvedCastingReadiness.Ready && second.Cost.Sum(c => c.Units) == 1 &&
                second.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "h2" }),
                "An unaffected animal companion created a false Enlarge/Reduce conflict: " + second.Readiness + " " +
                string.Join(",", second.ReadinessReasons));
            // Classic: the same two requests are both planned.
            Func<string, string, string, int, BuffCastRequest> request = (guid, assignment, target, order) =>
                new BuffCastRequest(new BuffSourceDefinition(assignment, MbbSpellKey(guid),
                        party.Effects[MbbSpellKey(guid).Canonical], CastGroupingKind.MassConfiguredTargets),
                    new[] { target }, ExistingEffectPolicy.Overwrite, null, new EnhancementRequest[0], assignment,
                    null, null, null, order);
            CastPlan classic = new CastPlanner().PlanRoutine(party.Snapshot, new[]
                {
                    request(enlarge, "enlarge", "h1", 0), request(reduce, "reduce", "h2", 1)
                }, party.Options, new ProviderSelectionPolicy(null, null, null), new ActiveEffectSnapshot(null));
            MbbExpect(classic.Steps.Count == 2 && classic.Outcomes.All(o => o.Kind == TargetOutcomeKind.Fulfilled),
                "Classic refused a cast whose only overlap is an unaffected animal: " +
                string.Join(",", classic.Outcomes.Select(o => o.Reason)));
        }

        // Review R4-F4: a mixed-coverage group casting blocked for a removal
        // conflict no longer says it will run or may shorten an effect.
        private static void TestMbbBlockedMixedNotes()
        {
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            string enlargeBuff = MbbPointGroupSpells[4][2], reduceBuff = MbbPointGroupSpells[5][2];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float r = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "w", 10f * r }, { "x", -0.8f * r }, { "a", 0f }, { "b", 0.4f * r }, { "c", 0.6f * r }
            };
            Func<string, string, bool> facts = (unit, fact) =>
                (fact == enlargeBuff && (unit == "x" || unit == "a")) || (fact == reduceBuff && unit == "b");
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                new Dictionary<string, string[]> { { "w", new[] { enlarge, reduce } } }, positions, null, facts);
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "x", new[] { new ActiveEffectInstance(EffectKind.Buff, enlargeBuff, null, 9, 0) } },
                { "a", new[] { new ActiveEffectInstance(EffectKind.Buff, enlargeBuff, null, 9, 0) } },
                { "b", new[] { new ActiveEffectInstance(EffectKind.Buff, reduceBuff, null, 9, 0) } }
            });
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("keep", "long", "w", enlargeKey.Canonical, enlargeKey, new[] { "x", "a" },
                        null, CastingTargetMode.AnchoredOrigin, "x"),
                    GroupCasting("reduce", "long", "w", reduceKey.Canonical, reduceKey, new[] { "b", "c" },
                        null, CastingTargetMode.AnchoredOrigin, "b")),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live);
            ResolvedCasting blocked = plan.CastingById("reduce");
            List<string> shown = blocked.ExistingEffectNotes.Select(note =>
                CastingRunPresentation.DescribeExistingEffectNote(note)).ToList();
            MbbExpect(blocked.Readiness == ResolvedCastingReadiness.Blocked && blocked.Cost.Count == 0 &&
                blocked.ReadinessReasons.Contains(ExplicitCastingCompiler.RemovesPlannedEffect + ":keep:" +
                    enlargeBuff + ":a"),
                "The mixed Mass Reduce was not refused for removing the kept Enlarge: " + blocked.Readiness + " " +
                string.Join(",", blocked.ReadinessReasons));
            MbbExpect(shown.Contains(PlannerText.Get("run.effect.keptblocked", "b")) &&
                !shown.Contains(PlannerText.Get("run.effect.covered", "b")) &&
                !shown.Contains(PlannerText.Get("run.effect.longer", "b")) &&
                !blocked.ExistingEffectNotes.Any(note => note.StartsWith("already-covered", StringComparison.Ordinal) ||
                    note.StartsWith("existing-active-recast:", StringComparison.Ordinal) ||
                    note.StartsWith("existing-insufficient:", StringComparison.Ordinal) ||
                    note.StartsWith("existing-incomplete:", StringComparison.Ordinal)),
                "The blocked casting's notes still promise a cast: " + string.Join(" | ", shown));
        }

        // Review R4-F5: a caster fact as the ONLY gate around an allied
        // point-capable buff (native ContextConditionCasterHasFact wrapping
        // Feather Step, Mass's application): known true keeps the in-radius
        // allies; known false or unread yields no beneficiaries and a
        // zero-cost refusal.
        private static void TestMbbCasterOnlyGate()
        {
            string feather = MbbPointGroupSpells[1][1];
            const string fact = "f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0";
            var dump = new NativeBlueprintDump(feather, MbbPointGroupSpells[1][2]);
            BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
            var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            var feature = NativeObject<Kingmaker.Blueprints.Classes.BlueprintFeature>();
            SetNative(feature, "m_AssetGuid", fact);
            var condition = NativeObject<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact>();
            SetNative(condition, "Fact", feature);
            var gate = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
            SetNative(gate, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
            {
                Conditions = new Kingmaker.ElementsSystem.Condition[] { condition }
            });
            SetNative(gate, "IfTrue", new Kingmaker.ElementsSystem.ActionList { Actions = run.Actions.Actions });
            SetNative(gate, "IfFalse", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            run.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { gate };
            MbbDiscovery gated = MbbDiscover(dump, feather);
            MbbExpect(gated.Included && MbbFind<ConditionalEffectExpression>(gated.Expression)
                    .Any(c => c.CasterFactGate != null && c.CasterFactGate.Facts.Any(f => f.Key == fact)),
                "The caster-gated Feather Step, Mass is not the intended admitted shape: " + gated.Reason);
            float radius = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "ally", radius * 0.5f }
            };
            MbbGroup group = MbbGroupCaster(dump, feather, positions, id => true);
            ProviderSnapshot provider = group.Snapshot.Providers.Single();
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(spell, spell);
            foreach (bool? known in new bool?[] { true, false, null })
            {
                var casterFacts = known.HasValue ? new Dictionary<string, bool> { { fact, known.Value } }
                    : new Dictionary<string, bool>();
                ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, gated.Expression,
                    group.Snapshot.Units, true, false, area, id => true, MbbDistance(positions), casterFacts,
                    MbbFriendlyReads((unit, f) => false));
                ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                    CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast", casterFacts);
                ResolvedCasting casting = new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("step", "long", "caster", group.Ability.Canonical, group.Ability,
                            new[] { "caster", "ally" })),
                    group.Snapshot, new List<ProviderPlanningOption> { option }, group.Effects,
                    new CastEnhancementSnapshot[0]).Castings.Single();
                if (known == true)
                    MbbExpect(decision.RecipientIdsByAnchor["caster"].SequenceEqual(new[] { "ally", "caster" }) &&
                        casting.Readiness == ResolvedCastingReadiness.Ready && casting.Cost.Sum(c => c.Units) == 1,
                        "A known-true caster gate lost its in-radius allies.");
                else
                    MbbExpect(decision.RecipientIdsByAnchor["caster"].Count() == 0 &&
                        casting.Readiness == ResolvedCastingReadiness.Blocked && casting.Cost.Count == 0 &&
                        casting.PredictedBeneficiaryUnitIds.Count == 0,
                        "A " + (known == false ? "known-false" : "unread") +
                        " caster gate left beneficiaries or a cost: " +
                        string.Join(",", decision.RecipientIdsByAnchor["caster"]) + " " + casting.Readiness);
            }
        }

        // Review R6-F2: the members a native method body calls or reads,
        // decoded from its IL in the referenced 2.1.7b Assembly-CSharp (no
        // game launch; metadata only, nothing is executed).
        private static List<string> MbbIlMembers(System.Reflection.MethodBase method)
        {
            var single = new Dictionary<short, System.Reflection.Emit.OpCode>();
            var multi = new Dictionary<short, System.Reflection.Emit.OpCode>();
            foreach (System.Reflection.FieldInfo field in typeof(System.Reflection.Emit.OpCodes).GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                var code = (System.Reflection.Emit.OpCode)field.GetValue(null);
                if (code.Size == 1) single[code.Value] = code; else multi[code.Value] = code;
            }
            byte[] il = method.GetMethodBody().GetILAsByteArray();
            var members = new List<string>();
            Type[] typeArgs = method.DeclaringType != null && method.DeclaringType.IsGenericType
                ? method.DeclaringType.GetGenericArguments() : null;
            Type[] methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;
            int position = 0;
            while (position < il.Length)
            {
                System.Reflection.Emit.OpCode code;
                if (il[position] == 0xFE) { code = multi[(short)(0xFE00 | il[position + 1])]; position += 2; }
                else { code = single[il[position]]; position += 1; }
                switch (code.OperandType)
                {
                    case System.Reflection.Emit.OperandType.InlineNone:
                        if (MbbIlAllOpcodes) members.Add(code.Name);
                        break;
                    case System.Reflection.Emit.OperandType.ShortInlineBrTarget:
                    case System.Reflection.Emit.OperandType.ShortInlineI:
                    case System.Reflection.Emit.OperandType.ShortInlineVar:
                        if (MbbIlAllOpcodes) members.Add(code.Name + " " + (sbyte)il[position]);
                        position += 1; break;
                    case System.Reflection.Emit.OperandType.InlineVar: position += 2; break;
                    case System.Reflection.Emit.OperandType.InlineI8:
                    case System.Reflection.Emit.OperandType.InlineR: position += 8; break;
                    case System.Reflection.Emit.OperandType.InlineSwitch:
                        position += 4 + 4 * BitConverter.ToInt32(il, position); break;
                    case System.Reflection.Emit.OperandType.InlineMethod:
                    case System.Reflection.Emit.OperandType.InlineField:
                    {
                        int token = BitConverter.ToInt32(il, position);
                        position += 4;
                        try
                        {
                            System.Reflection.MemberInfo member = method.Module.ResolveMember(token, typeArgs, methodArgs);
                            members.Add(code.Name + " " + (member.DeclaringType == null ? string.Empty
                                : member.DeclaringType.FullName) + "::" + member.Name);
                        }
                        catch (Exception error) { members.Add(code.Name + " unresolved:" + error.GetType().Name); }
                        break;
                    }
                    default:
                        if (MbbIlAllOpcodes) members.Add(code.Name + " " + BitConverter.ToInt32(il, position));
                        position += 4; break;
                }
            }
            return members;
        }

        private static bool MbbIlAllOpcodes;

        private static System.Reflection.MethodInfo MbbCheckCondition(Type condition)
        {
            return condition.GetMethod("CheckCondition", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.DeclaredOnly);
        }

        // Review R6-F1/R6-F2/R6-F3: what the native 2.1.7b predicates check,
        // observed in the referenced Assembly-CSharp IL (no game launch), and
        // that the planner's live read (NativeRecipientFactRead.Read) makes
        // exactly those calls. Also the exporter's modifier signs (R5-F1/F2).
        private static void TestMbbNativeConditionContracts()
        {
            Func<Type, List<string>> check = type =>
            {
                List<string> members = MbbIlMembers(MbbCheckCondition(type));
                Console.WriteLine("[NATIVE-CONTRACT] " + type.Name + ": " + string.Join(" | ", members));
                return members;
            };
            List<string> hasBuff = check(typeof(Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasBuff));
            List<string> hasFact = check(typeof(Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact));
            List<string> isEnemy = check(typeof(Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionIsEnemy));
            List<string> isAlly = check(typeof(Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionIsAlly));
            const string buffs = "ldfld Kingmaker.UnitLogic.UnitDescriptor::Buffs",
                collectionHas = "callvirt Kingmaker.Blueprints.FactCollection::HasFact",
                helperHas = "call Kingmaker.UnitLogic.UnitHelper::HasFact",
                enemy = "callvirt Kingmaker.EntitySystem.Entities.UnitEntityData::IsEnemy",
                ally = "callvirt Kingmaker.EntitySystem.Entities.UnitEntityData::IsAlly",
                caster = "callvirt Kingmaker.UnitLogic.Mechanics.MechanicsContext::get_MaybeCaster";
            MbbExpect(hasBuff.Contains(buffs) && hasBuff.Contains(collectionHas) && !hasBuff.Contains(helperHas),
                "ContextConditionHasBuff no longer checks descriptor.Buffs.HasFact.");
            MbbExpect(hasFact.Contains(helperHas) && !hasFact.Contains(buffs),
                "ContextConditionHasFact no longer checks UnitHelper.HasFact.");
            MbbExpect(isEnemy.Contains(enemy) && isEnemy.Contains(caster) && isAlly.Contains(ally) &&
                isAlly.Contains(caster), "ContextConditionIsEnemy / IsAlly no longer compare with the caster.");
            MbbIlAllOpcodes = true;
            List<string> checker;
            try
            {
                checker = MbbIlMembers(typeof(Kingmaker.ElementsSystem.ConditionsChecker).GetMethods(
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly)
                    .Single(m => m.Name == "Check" && m.GetMethodBody() != null));
            }
            finally { MbbIlAllOpcodes = false; }
            Console.WriteLine("[NATIVE-CONTRACT] ConditionsChecker.Check: " + string.Join(" | ", checker.Take(5)));
            MbbExpect(checker.Take(5).SequenceEqual(new[]
                {
                    "ldarg.0", "call Kingmaker.ElementsSystem.ConditionsChecker::get_HasConditions", "brtrue.s 2",
                    "ldc.i4.1", "ret"
                }),
                "ConditionsChecker.Check no longer returns true for a checker without conditions.");
            List<string> read = MbbIlMembers(typeof(NativeRecipientFactRead).GetMethod("Read",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
            MbbExpect(read.Contains(buffs) && read.Contains(collectionHas) && read.Contains(helperHas) &&
                read.Contains(enemy) && read.Contains(ally),
                "The planner's recipient read is not the native predicates' read: " + string.Join(" | ", read));
            // Review R8-F1: AddImmunityFirebrand's benefit is relative to the
            // buff's caster - its IL reads the context caster.
            List<string> firebrand = typeof(Kingmaker.UnitLogic.FactLogic.AddImmunityFirebrand)
                .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(m => m.GetMethodBody() != null).SelectMany(m => MbbIlMembers(m)).ToList();
            Console.WriteLine("[NATIVE-CONTRACT] AddImmunityFirebrand: " + string.Join(" | ", firebrand.Distinct()));
            MbbExpect(firebrand.Any(member => member.Contains("MaybeCaster")),
                "AddImmunityFirebrand no longer reads the buff's caster.");
            MbbExpect(RecipientFactGate.EnemyOfCaster == MbbEnemyOfCaster &&
                RecipientFactGate.AllyOfCaster == MbbAllyOfCaster &&
                RecipientFactGate.BuffPredicate("x") == "buff-predicate:x",
                "The test's relation keys differ from the production gate keys.");
            // R5-F1/F2: the exporter's modifier signs from the native buffs.
            var dump = new NativeBlueprintDump(MbbCrushingDespairBuff, MbbPointGroupSpells[4][2],
                MbbPointGroupSpells[5][2]);
            Func<string, string[]> signs = guid => NativeCatalogExporter.ModifierSigns(
                dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(guid).ComponentsArray);
            MbbExpect(signs(MbbCrushingDespairBuff).Length == 5 &&
                signs(MbbCrushingDespairBuff).All(s => s.EndsWith(":penalty", StringComparison.Ordinal)) &&
                signs(MbbPointGroupSpells[5][2]).Any(s => s.EndsWith(":bonus", StringComparison.Ordinal)) &&
                signs(MbbPointGroupSpells[5][2]).Any(s => s.EndsWith(":penalty", StringComparison.Ordinal)),
                "Modifier signs: despair " + string.Join(",", signs(MbbCrushingDespairBuff)) + "; reduce " +
                string.Join(",", signs(MbbPointGroupSpells[5][2])) + "; enlarge " +
                string.Join(",", signs(MbbPointGroupSpells[4][2])));
        }

        // Appends to the ability's top-level run actions (the actions its own
        // AbilityTargetsAround selector runs on every recipient) a copy of its
        // first ContextActionApplyBuff applying the given buff instead.
        private static bool MbbAppendApplication(BlueprintAbility ability,
            Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff buff)
        {
            var copy = MbbApplicationCopy(ability, buff);
            if (copy == null) return false;
            var run = ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            run.Actions.Actions = (run.Actions.Actions ?? new Kingmaker.ElementsSystem.GameAction[0])
                .Concat(new Kingmaker.ElementsSystem.GameAction[] { copy }).ToArray();
            return true;
        }

        // A copy of the ability's first ContextActionApplyBuff (its native
        // duration contract) applying the given buff, or null.
        private static Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff MbbApplicationCopy(
            BlueprintAbility ability, Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff buff)
        {
            Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff template = null;
            MbbReplaceApplications(ability, apply => template = template ?? apply);
            if (template == null) return null;
            var copy = (Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff)typeof(object)
                .GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(template, null);
            SetNative(copy, "Buff", buff);
            return copy;
        }

        // Review R5-F1: Feather Step, Mass's own allied selector also applies
        // the native DazeBuff (a harmful condition) to every recipient. The
        // valid Feather Step leaf no longer admits the point-capable burst;
        // the unmodified spell stays admitted.
        private static void TestMbbPointHarmfulBuffRefused()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            var dump = new NativeBlueprintDump(feather, featherBuff, MbbDazeBuff);
            BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
            MbbExpect(MbbAppendApplication(spell, dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbDazeBuff)),
                "Feather Step, Mass's allied actions could not be extended.");
            MbbDiscovery dazing = MbbDiscover(dump, feather);
            var providers = new List<ProviderSnapshot>();
            ProviderSnapshot provider = MbbAdmission(dump).AdmitSpell("caster", "book", false, "Book",
                MbbRead(dump, feather, feather, SourceKind.Spellbook, 9), 3, 0, "pool", 1, new string[0], providers);
            MbbExpect(dazing.Facts.Effects.Any(e => e.EffectId == MbbDazeBuff && e.Target == "AlliedAreaRecipients" &&
                    (e.GrantedConditions ?? new string[0]).Contains("Dazed")) &&
                dazing.Facts.Effects.Any(e => e.EffectId == featherBuff),
                "The Daze application is not the intended allied-selector shape.");
            MbbExpect(dazing.Repaired.Disposition == "exclude" && !dazing.Included && provider == null &&
                providers.Count == 0,
                "A point-capable allied burst that also dazes its recipients was admitted: " + dazing.Repaired.Reason);
            MbbExpect(MbbDiscover(new NativeBlueprintDump(feather, featherBuff), feather).Included,
                "Unmodified Feather Step, Mass is no longer admitted.");
        }

        // Review R5-F2: the approved GoodHopeBuff keeps its bonuses but, when
        // it activates, applies the native DazeBuff to its bearer. The waiver
        // no longer overrides the harmful-to-allies metadata; the unmodified
        // Good Hope and Firebelly (whose breath is a separately activated
        // granted ability) stay admitted.
        private static void TestMbbActivationDebuffRefused()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbDazeBuff);
            var buff = dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbGoodHopeBuff);
            var apply = MbbApplicationCopy(dump.Get<BlueprintAbility>(MbbGoodHope),
                dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbDazeBuff));
            MbbExpect(apply != null && !apply.ToCaster, "No bearer-targeted Daze application could be built.");
            var activation = NativeObject<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>();
            SetNative(activation, "Activated", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[] { apply }
            });
            SetNative(activation, "Deactivated", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            SetNative(activation, "NewRound", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            SetNative(buff, "Components", (buff.ComponentsArray ?? new Kingmaker.Blueprints.BlueprintComponent[0])
                .Concat(new Kingmaker.Blueprints.BlueprintComponent[] { activation }).ToArray());
            MbbDiscovery dazed = MbbDiscover(dump, MbbGoodHope);
            var providers = new List<ProviderSnapshot>();
            ProviderSnapshot provider = MbbAdmission(dump).AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(dump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, "pool", 1,
                new string[0], providers);
            MbbExpect(dazed.Facts.Effects.Any(e => e.EffectId == MbbGoodHopeBuff && e.Harmful == false &&
                    (e.FactActions ?? new NativeCandidateFactActions[0]).Any(list => list.List == "Activated" &&
                        (list.AppliedEffects ?? new NativeCandidateEffectFacts[0]).Any(applied =>
                            applied.EffectId == MbbDazeBuff &&
                            (applied.GrantedConditions ?? new string[0]).Contains("Dazed")))),
                "The exporter did not record the Daze the buff applies on activation.");
            MbbExpect(dazed.Repaired.Disposition == "exclude" && !dazed.Included && provider == null &&
                providers.Count == 0,
                "Good Hope was admitted although its buff dazes its bearer on activation: " + dazed.Repaired.Reason);
            var real = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbFirebelly, MbbFirebellyBuff,
                MbbFirebellyBreath);
            MbbExpect(MbbDiscover(real, MbbGoodHope).Included && MbbDiscover(real, MbbFirebelly).Included,
                "The unmodified Good Hope or Firebelly is no longer admitted.");
        }

        // Review R5-F3: Mass Blessing's caster Undead Arcana and recipient
        // undead checks in ONE native AND around the sole application. A
        // mixed caster/recipient checker is not a modelled gate, so the
        // application is conditional on an unmodelled predicate: no unit gets
        // a definite coverage claim (arcana absent, and - the predicate being
        // unsupported - also arcana present), and the casting is refused at
        // zero cost.
        private static void TestMbbMixedConditionWithheld()
        {
            const string undead = "734a29b693e9ec346ba2951b27987e33", arcana = "1a5e7191279e7cd479b17a6ca438498c";
            string blessing = MbbPointGroupSpells[0][1];
            var dump = new NativeBlueprintDump(blessing, MbbPointGroupSpells[0][2]);
            BlueprintAbility spell = dump.Get<BlueprintAbility>(blessing);
            var application = MbbApplicationCopy(spell,
                dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbPointGroupSpells[0][2]));
            Func<string, Kingmaker.Blueprints.Classes.BlueprintFeature> feature = guid =>
            {
                var value = NativeObject<Kingmaker.Blueprints.Classes.BlueprintFeature>();
                SetNative(value, "m_AssetGuid", guid);
                return value;
            };
            var casterHas = NativeObject<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact>();
            SetNative(casterHas, "Fact", feature(arcana));
            var targetHas = NativeObject<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact>();
            SetNative(targetHas, "Fact", feature(undead));
            var gate = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
            SetNative(gate, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
            {
                Conditions = new Kingmaker.ElementsSystem.Condition[] { casterHas, targetHas }
            });
            SetNative(gate, "IfTrue", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[] { application }
            });
            SetNative(gate, "IfFalse", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            run.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { gate };
            MbbDiscovery mixed = MbbDiscover(dump, blessing);
            MbbExpect(mixed.Included && MbbFind<ConditionalEffectExpression>(mixed.Expression)
                    .Any(c => c.CasterFactGate == null && c.RecipientFactGate == null),
                "The mixed-condition Mass Blessing is not the intended admitted shape: " + mixed.Repaired.Reason);
            float radius = 30f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "living", radius * 0.2f }, { "undead", radius * 0.2f }
            };
            MbbGroup group = MbbGroupCaster(dump, blessing, positions, id => true);
            ProviderSnapshot provider = group.Snapshot.Providers.Single();
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(spell, spell);
            foreach (bool hasArcana in new[] { false, true })
            {
                var casterFacts = new Dictionary<string, bool> { { arcana, hasArcana } };
                ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider,
                    group.Effects[group.Ability.Canonical], group.Snapshot.Units, true, false, area, id => true,
                    MbbDistance(positions), casterFacts,
                    MbbFriendlyReads((unit, fact) => fact == undead && unit == "undead"));
                ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                    CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast", casterFacts);
                ResolvedCasting casting = new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("bless", "long", "caster", group.Ability.Canonical, group.Ability,
                            new[] { "caster", "living", "undead" })),
                    group.Snapshot, new List<ProviderPlanningOption> { option }, group.Effects,
                    new CastEnhancementSnapshot[0]).Castings.Single();
                MbbExpect(decision.RecipientIdsByAnchor["caster"].Count() == 0 &&
                    casting.Readiness == ResolvedCastingReadiness.Blocked && casting.Cost.Count == 0 &&
                    casting.PredictedBeneficiaryUnitIds.Count == 0,
                    "A mixed caster/recipient condition (arcana=" + hasArcana + ") was treated as unconditional: " +
                    string.Join(",", decision.RecipientIdsByAnchor["caster"]) + " " + casting.Readiness + " " +
                    casting.Cost.Sum(c => c.Units));
            }
        }

        // Review R5-F4: two Enlarge/Reduce disks overlap only on a humanoid
        // who already has Reduce. The skipped Mass Enlarge (its ally e
        // already enlarged) protects Enlarge on e only - not on the reduced
        // humanoid it never benefits - so the funded Mass Reduce over the
        // unbuffed ally runs for one slot. Control: a Reduce disk that also
        // reaches e is refused for removing the kept Enlarge.
        private static void TestMbbSkipProtectsOnlyRecipients()
        {
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            string enlargeBuff = MbbPointGroupSpells[4][2], reduceBuff = MbbPointGroupSpells[5][2];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float r = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "w", 10f * r }, { "e", 0f }, { "h", 0.8f * r }, { "u", 1.6f * r }
            };
            Func<string, string, bool> facts = (unit, fact) =>
                (fact == enlargeBuff && unit == "e") || (fact == reduceBuff && unit == "h");
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                new Dictionary<string, string[]> { { "w", new[] { enlarge, reduce } } }, positions, null, facts);
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "e", new[] { new ActiveEffectInstance(EffectKind.Buff, enlargeBuff, null, 9, 0) } },
                { "h", new[] { new ActiveEffectInstance(EffectKind.Buff, reduceBuff, null, 9, 0) } }
            });
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            PlannedCasting skip = GroupCasting("skip", "long", "w", enlargeKey.Canonical, enlargeKey, new[] { "e" },
                null, CastingTargetMode.AnchoredOrigin, "e");
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(skip,
                    GroupCasting("reduce", "long", "w", reduceKey.Canonical, reduceKey, new[] { "u" },
                        null, CastingTargetMode.AnchoredOrigin, "u")),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live);
            ResolvedCasting skipped = plan.CastingById("skip"), funded = plan.CastingById("reduce");
            MbbExpect(skipped.Readiness == ResolvedCastingReadiness.AlreadySatisfied &&
                skipped.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "e" }) &&
                funded.Readiness == ResolvedCastingReadiness.Ready && funded.Cost.Sum(c => c.Units) == 1 &&
                funded.PredictedBeneficiaryUnitIds.Contains("u"),
                "A skipped Mass Enlarge protected Enlarge on a humanoid it never benefits: " + skipped.Readiness +
                " / " + funded.Readiness + " " + string.Join(",", funded.ReadinessReasons));
            ExplicitCastingPlan control = new ExplicitCastingCompiler().Compile(CastingDocument(skip,
                    GroupCasting("reduce-e", "long", "w", reduceKey.Canonical, reduceKey, new[] { "u" },
                        null, CastingTargetMode.AnchoredOrigin, "h")),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live);
            ResolvedCasting refused = control.CastingById("reduce-e");
            MbbExpect(refused.Readiness == ResolvedCastingReadiness.Blocked && refused.Cost.Count == 0 &&
                refused.ReadinessReasons.Contains(ExplicitCastingCompiler.RemovesPlannedEffect + ":skip:" +
                    enlargeBuff + ":e"),
                "A Reduce disk reaching the kept Enlarge was not refused: " + refused.Readiness + " " +
                string.Join(",", refused.ReadinessReasons));
        }

        // Review R5-F5: two unaffected humanoids; Mass Enlarge reaches a in
        // the first routine, Mass Reduce reaches a and b in the next (anchored
        // on an animal companion it does nothing to). Under the successful-
        // cast assumption both cost one slot and only b is predicted to
        // receive Reduce (on a it removes the projected Enlarge) - in the
        // one-pass compiler and the Classic sequential forecast. An
        // independent routine preview keeps its own starting snapshot.
        private static void TestMbbSequentialOpposingCoverage()
        {
            const string animalType = "a95311b3dc996964cbaa30ff9965aaf6";
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float r = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "w", 10f * r }, { "a", 0f }, { "pet", 0.8f * r }, { "b", 1.6f * r }
            };
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                new Dictionary<string, string[]> { { "w", new[] { enlarge, reduce } } }, positions,
                new Dictionary<string, Func<string, bool>> { { enlarge, id => id == "a" }, { reduce, id => id == "pet" } },
                (unit, fact) => unit == "pet" && fact == animalType);
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            CastingPlanDocument document = CastingDocument(
                GroupCasting("enlarge", "long", "w", enlargeKey.Canonical, enlargeKey, new[] { "a" },
                    null, CastingTargetMode.AnchoredOrigin, "a"),
                GroupCasting("reduce", "important", "w", reduceKey.Canonical, reduceKey, new[] { "a", "b" },
                    null, CastingTargetMode.AnchoredOrigin, "pet"));
            CastingForecast onePass = new CastingForecastService().ForecastOnePass(document,
                party.Snapshot, party.Options, party.Effects);
            ResolvedCasting first = onePass.Plan.CastingById("enlarge"), second = onePass.Plan.CastingById("reduce");
            MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(c => c.Units) == 1 &&
                first.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "a" }) &&
                second.Readiness == ResolvedCastingReadiness.Ready && second.Cost.Sum(c => c.Units) == 1 &&
                second.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "b" }),
                "The one-pass forecast predicts Reduce on the ally an earlier Enlarge reached: " + second.Readiness +
                " " + string.Join(",", second.PredictedBeneficiaryUnitIds) + " " +
                string.Join(",", second.ReadinessReasons));
            ResolvedCasting independent = new ExplicitCastingCompiler().Compile(document, party.Snapshot,
                party.Options, party.Effects, new CastEnhancementSnapshot[0]).CastingById("reduce");
            MbbExpect(independent.Readiness == ResolvedCastingReadiness.Ready &&
                independent.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                    .SequenceEqual(new[] { "a", "b" }),
                "An independent routine preview no longer starts from its own snapshot: " +
                string.Join(",", independent.PredictedBeneficiaryUnitIds));
        }

        private static void TestMbbClassicSequentialOpposingCoverage()
        {
            const string animalType = "a95311b3dc996964cbaa30ff9965aaf6";
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float r = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "w", 10f * r }, { "a", 0f }, { "pet", 0.8f * r }, { "b", 1.6f * r }
            };
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                new Dictionary<string, string[]> { { "w", new[] { enlarge, reduce } } }, positions,
                new Dictionary<string, Func<string, bool>> { { enlarge, id => id == "a" }, { reduce, id => id == "pet" } },
                (unit, fact) => unit == "pet" && fact == animalType);
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-sequential-opposing");
            profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(Assignment(
                enlargeKey.Canonical, enlargeKey, new[] { "a" }));
            profile.Routines.First(rt => rt.RoutineId == "short").Assignments.Add(Assignment(
                reduceKey.Canonical, reduceKey, new[] { "a", "b" }));
            SequentialForecastPlanner.Result classic = SequentialForecastPlanner.Compute(profile,
                new[] { "long", "short" }, party.Snapshot, new ActiveEffectSnapshot(null), party.Effects,
                party.Options, new CastEnhancementSnapshot[0]);
            CastStep enlargeStep = classic.Occurrences[0].Plan.Steps.SingleOrDefault();
            CastStep reduceStep = classic.Occurrences[1].Plan.Steps.SingleOrDefault();
            MbbExpect(enlargeStep != null && enlargeStep.Reservation.Units == 1 &&
                enlargeStep.ExpectedRecipientUnitIds.SequenceEqual(new[] { "a" }) &&
                reduceStep != null && reduceStep.Reservation.Units == 1 &&
                reduceStep.ExpectedRecipientUnitIds.SequenceEqual(new[] { "b" }),
                "The Classic sequential forecast predicts Reduce on the ally an earlier Enlarge reached: " +
                (reduceStep == null ? "no step" : string.Join(",", reduceStep.ExpectedRecipientUnitIds)) + " / " +
                string.Join(",", classic.Occurrences[1].Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
        }

        // Review R5-F6: the bard's last level-3 slot goes to Haste; the
        // mixed-coverage Good Hope after it (ally-near already has a longer
        // Good Hope, the bard has none) is refused for resources, reserves
        // nothing, and its notes no longer say the cast goes ahead or may
        // shorten the existing effect.
        private static void TestMbbUnfundedMixedNotes()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff);
            MbbParty party = MbbBardPartyWithSlots(dump, 1, MbbGoodHope, MbbHaste);
            AbilityKey hope = party.Abilities[MbbGoodHope], haste = party.Abilities[MbbHaste];
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "ally-near", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbGoodHopeBuff, 5000, 7, 0) } }
            });
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("haste", "long", "bard", haste.Canonical, haste, new[] { "bard" }),
                    GroupCasting("hope", "long", "bard", hope.Canonical, hope, new[] { "bard", "ally-near" })),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live);
            ResolvedCasting funded = plan.CastingById("haste"), refused = plan.CastingById("hope");
            List<string> shown = refused.ExistingEffectNotes.Select(note =>
                CastingRunPresentation.DescribeExistingEffectNote(note)).ToList();
            MbbExpect(funded.Readiness == ResolvedCastingReadiness.Ready && funded.Cost.Sum(c => c.Units) == 1 &&
                refused.Readiness == ResolvedCastingReadiness.Blocked && refused.Cost.Count == 0 &&
                refused.ReadinessReasons.Count != 0,
                "The unfunded Good Hope was not a zero-cost resource refusal: " + refused.Readiness + " " +
                string.Join(",", refused.ReadinessReasons));
            MbbExpect(shown.Contains(PlannerText.Get("run.effect.keptblocked", "ally-near")) &&
                !shown.Contains(PlannerText.Get("run.effect.covered", "ally-near")) &&
                !shown.Contains(PlannerText.Get("run.effect.longer", "ally-near")) &&
                !refused.ExistingEffectNotes.Any(note => note.StartsWith("already-", StringComparison.Ordinal) ||
                    note.StartsWith("existing-active-recast:", StringComparison.Ordinal) ||
                    note.StartsWith("existing-insufficient:", StringComparison.Ordinal) ||
                    note.StartsWith("existing-incomplete:", StringComparison.Ordinal)),
                "The unfunded casting's notes still promise a cast: " + string.Join(" | ", shown));
        }

        // Review R4-F6 (R3-F4 isolated): a direct casting removes E from a
        // (a is its beneficiary); a later skip-if-active group casting relies
        // on E on a and b. Only a is described as losing E; b keeps it while
        // the blocked casting does not run. Synthetic shapes; no fixture.
        private static void TestMbbRemovedEarlierNoteIsolated()
        {
            const string e = "e1000000000000000000000000000001", x = "e1000000000000000000000000000002";
            var units = MbbUnits(new[] { "c", "a", "b" });
            var pool = new ResourcePoolSnapshot("c|slots", ResourcePoolKind.SpontaneousLevel, 9, 9, null);
            var removeKey = new AbilityKey("b1000000000000000000000000000001", string.Empty, 0, SourceKind.Spellbook, string.Empty);
            var groupKey = new AbilityKey("b1000000000000000000000000000002", string.Empty, 0, SourceKind.Spellbook, string.Empty);
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { removeKey.Canonical, new SequenceEffectExpression(new EffectExpression[]
                    {
                        new EffectLeafExpression(EffectKind.Buff, x, EffectTarget.CurrentTarget, "fixture", "fixture:" + x),
                        new EmptyEffectExpression("restorative-action:ContextActionRemoveBuff",
                            new EffectRemoval(e, EffectTarget.CurrentTarget))
                    }) },
                { groupKey.Canonical, new EffectLeafExpression(EffectKind.Buff, e, EffectTarget.AlliedAreaRecipients,
                    "fixture", "fixture:" + e) }
            };
            ProviderSnapshot remover = new ProviderSnapshot(new ProviderKey("c", "book", removeKey, "level-1|heighten-0"),
                "remover", 1, pool.PoolKey, 1, null, null, 5, 50, "fixture", "fixture", "Book", 0, "Book");
            ProviderSnapshot grouper = new ProviderSnapshot(new ProviderKey("c", "book", groupKey, "level-1|heighten-0"),
                "group", 1, pool.PoolKey, 1, null, null, 5, 50, "fixture", "fixture", "Book", 0, "Book");
            var snapshot = new PartyProviderSnapshot(units, new[] { remover, grouper }, new[] { pool });
            var options = new List<ProviderPlanningOption>
            {
                new ProviderPlanningOption(remover, new[] { "a" }, new[] { "a" }, 5, 50,
                    CastExecutionStrategy.DirectRuleCast, "direct"),
                new ProviderPlanningOption(grouper, new[] { "a", "b" }, new[] { "a" }, 5, 50,
                    CastExecutionStrategy.DirectRuleCast, "direct",
                    new Dictionary<string, IEnumerable<string>> { { "a", new[] { "a", "b" } } })
            };
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "a", new[] { new ActiveEffectInstance(EffectKind.Buff, e, null, 7, 0) } },
                { "b", new[] { new ActiveEffectInstance(EffectKind.Buff, e, null, 7, 0) } }
            });
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                    DirectCasting("remove", "long", "c", "a", removeKey.Canonical, removeKey),
                    GroupCasting("group", "long", "c", groupKey.Canonical, groupKey, new[] { "a", "b" },
                        null, CastingTargetMode.AnchoredOrigin, "a")),
                snapshot, options, effects, new CastEnhancementSnapshot[0], null, null, false, live);
            ResolvedCasting blocked = plan.CastingById("group");
            string noteA = blocked.ExistingEffectNotes.FirstOrDefault(note => note.Contains(":a"));
            string noteB = blocked.ExistingEffectNotes.FirstOrDefault(note => note.Contains(":b"));
            MbbExpect(blocked.Readiness == ResolvedCastingReadiness.Blocked &&
                blocked.ReadinessReasons.Any(reason => reason.StartsWith("effect-removed-by-earlier-casting:remove:" + e,
                    StringComparison.Ordinal)),
                "The skip relying on the removed effect was not refused: " + blocked.Readiness + " " +
                string.Join(",", blocked.ReadinessReasons));
            MbbExpect(noteA != null && noteA.StartsWith("existing-removed-earlier:a", StringComparison.Ordinal) &&
                noteB != null && noteB.StartsWith("existing-kept-blocked:b", StringComparison.Ordinal),
                "The inspector reports removal for an unaffected ally: b note '" + noteB + "' rendered '" +
                (noteB == null ? string.Empty : CastingRunPresentation.DescribeExistingEffectNote(noteB)) + "'");
        }

        // A bard who knows Good Hope, with explicit positions, injected live
        // recipient facts and remaining level-3 slots.
        private static MbbParty MbbGoodHopeParty(NativeBlueprintDump dump, IDictionary<string, float> positions,
            Func<string, string, bool> unitHasFact, int remainingSlots, Func<string, bool> canTarget)
        {
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool("bard", MbbBardBook, 3,
                remainingSlots, Math.Max(2, remainingSlots));
            var providers = new List<ProviderSnapshot>();
            admission.AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(dump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, pool.PoolKey, 1,
                new string[0], providers);
            var party = new MbbParty
            {
                Effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal),
                Admission = admission
            };
            foreach (ProviderSnapshot provider in providers)
                party.Abilities[provider.Key.Ability.BaseAbilityGuid] = provider.Key.Ability;
            party.Options = providers.Select(provider => MbbOption(dump, provider,
                party.Effects[provider.Key.Ability.Canonical], units, canTarget, positions, unitHasFact)).ToList();
            party.Snapshot = new PartyProviderSnapshot(units, providers, new[] { pool });
            return party;
        }

        // Review R7-F1: a native ContextActionKillTarget (an action the adapter
        // cannot classify) on Mass Blessing's construct branch, and on Good
        // Hope's Despair branch, refuses both narrow exceptions through
        // discovery and provider admission; the unmodified spells stay.
        private static void TestMbbKillActionRefused()
        {
            string blessing = MbbPointGroupSpells[0][1];
            var burstDump = new NativeBlueprintDump(blessing, MbbPointGroupSpells[0][2]);
            MbbExpect(MbbAppendToFirstConditionalTrue(burstDump.Get<BlueprintAbility>(blessing),
                    NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKillTarget>()),
                "Mass Blessing's construct branch could not be extended.");
            MbbDiscovery killingBurst = MbbDiscover(burstDump, blessing);
            var burstProviders = new List<ProviderSnapshot>();
            ProviderSnapshot burstProvider = MbbAdmission(burstDump).AdmitSpell("caster", "book", false, "Book",
                MbbRead(burstDump, blessing, blessing, SourceKind.Spellbook, 17), 6, 0, "pool", 1, new string[0],
                burstProviders);
            MbbExpect(killingBurst.Scan.Diagnostics.Any(d => d.NodeIdentity.Contains("ContextActionKillTarget")) &&
                !killingBurst.Included && burstProvider == null,
                "A point-capable allied burst that can kill a construct was admitted: " + killingBurst.Repaired.Reason);
            var hopeDump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            MbbExpect(MbbAppendToFirstConditionalTrue(hopeDump.Get<BlueprintAbility>(MbbGoodHope),
                    NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKillTarget>()),
                "Good Hope's Despair branch could not be extended.");
            MbbDiscovery killingHope = MbbDiscover(hopeDump, MbbGoodHope);
            var hopeProviders = new List<ProviderSnapshot>();
            ProviderSnapshot hopeProvider = MbbAdmission(hopeDump).AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(hopeDump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, "pool", 1,
                new string[0], hopeProviders);
            MbbExpect(!killingHope.Included && hopeProvider == null,
                "Good Hope with a kill action on its Despair branch was admitted: " + killingHope.Repaired.Reason);
            MbbExpect(MbbDiscover(new NativeBlueprintDump(blessing, MbbPointGroupSpells[0][2]), blessing).Included &&
                MbbDiscover(new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff), MbbGoodHope).Included,
                "Unmodified Mass Blessing or Good Hope is no longer admitted.");
        }

        // Review R7-F2: the native Resistance - Fire selection (Personal,
        // self, enemy and harmful-enemy flags, its native resource) with its
        // sole application repointed to the committed Crushing Despair. The
        // personal targeting rescue no longer admits the penalty; the five
        // real selections stay admitted.
        private static void TestMbbPersonalPenaltyRefused()
        {
            string fire = MbbAbjuration[3][1];
            var all = MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToList();
            var dump = new NativeBlueprintDump(all.Concat(new[] { MbbCrushingDespairBuff }).ToArray());
            BlueprintAbility ability = dump.Get<BlueprintAbility>(fire);
            var despair = dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbCrushingDespairBuff);
            MbbExpect(MbbReplaceApplications(ability, apply => SetNative(apply, "Buff", despair)) == 1,
                "Resistance - Fire's application could not be repointed.");
            MbbDiscovery penalty = MbbDiscover(dump, fire);
            PartyProviderAdmission admission = MbbAdmission(dump);
            var logic = ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>()
                .Single();
            KingmakerFactResource resource;
            string refusal;
            bool known = KingmakerFactResources.TryPriceAbilityResource("wizard", logic.RequiredResource.AssetGuid,
                logic.Amount, 1, () => 1, 1, false, string.Empty, string.Empty, out resource, out refusal);
            var providers = new List<ProviderSnapshot>();
            admission.AdmitFact("wizard", MbbRead(dump, fire, "Resistance - Fire", SourceKind.AbilityResource, 7),
                known, resource, refusal, string.Empty, providers, new List<ResourcePoolSnapshot>(),
                new HashSet<string>(StringComparer.Ordinal));
            MbbExpect(penalty.Facts.Range == "Personal" && penalty.Facts.CanTargetEnemies &&
                penalty.Facts.Effects.Any(e => e.EffectId == MbbCrushingDespairBuff),
                "The repointed selection is not the intended Personal shape.");
            MbbExpect(!penalty.Included && penalty.Repaired.Disposition == "exclude" && providers.Count == 0,
                "A Personal selection applying only penalties to its caster was admitted: " + penalty.Repaired.Reason);
            var real = new NativeBlueprintDump(all.ToArray());
            MbbExpect(MbbAbjuration.All(row => MbbDiscover(real, row[1]).Included),
                "A real Abjuration resistance selection is no longer admitted.");
        }

        // Review R7-F3: Abjuration resistance strength depends on caster level
        // only through its native custom rank progression (5 up to level 10,
        // 10 up to level 20). A permanent Fire recorded at level 6 satisfies
        // the level-7 wizard's Fire for free; Cold still switches for one use.
        // Tier transitions and the native progression are checked separately.
        private static void TestMbbResistanceReselectInTier()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbParty party = MbbWizardParty(dump, 1);
            AbilityKey fire = party.Abilities["Fire"], cold = party.Abilities["Cold"];
            string fireBuff = MbbAbjuration[3][2];
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "wizard", new[] { new ActiveEffectInstance(EffectKind.Buff, fireBuff, null, 6, 0) } }
            });
            ResolvedCasting reselect = new ExplicitCastingCompiler().Compile(CastingDocument(
                    DirectCasting("fire", "long", "wizard", "wizard", fire.Canonical, fire)),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live)
                .CastingById("fire");
            ResolvedCasting change = new ExplicitCastingCompiler().Compile(CastingDocument(
                    DirectCasting("cold", "long", "wizard", "wizard", cold.Canonical, cold)),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live)
                .CastingById("cold");
            MbbExpect(reselect.Readiness == ResolvedCastingReadiness.AlreadySatisfied && reselect.Cost.Count == 0 &&
                change.Readiness == ResolvedCastingReadiness.Ready && change.Cost.Sum(c => c.Units) == 1,
                "Reselecting Fire from level 6 at level 7: " + reselect.Readiness + " " +
                string.Join(",", reselect.ReadinessReasons) + " / Cold " + change.Readiness);
            EffectLeafExpression leaf = EffectApplicationLifetime.Leaves(party.Effects[fire.Canonical])
                .Single(l => l.EffectId == fireBuff);
            MbbExpect(leaf.CasterLevelStrength != null &&
                leaf.CasterLevelStrength.Items.SequenceEqual(new[]
                {
                    new KeyValuePair<int, int>(10, 5), new KeyValuePair<int, int>(20, 10)
                }),
                "Resistance - Fire's native custom progression was not recorded.");
            // The native progression itself (ContextRankConfig's own method on
            // the detached native component) agrees below the last breakpoint.
            var config = dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(fireBuff).ComponentsArray
                .OfType<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>().Single();
            System.Reflection.MethodInfo progression = typeof(Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)
                .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "ApplyProgression" && m.ReturnType == typeof(int) &&
                    m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
            MbbExpect(progression != null, "ContextRankConfig has no ApplyProgression(int): " + string.Join(",",
                typeof(Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig).GetMethods(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
                    .Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ")")));
            for (int level = 1; level < 20; level++)
            {
                int native = (int)progression.Invoke(config, new object[] { level });
                MbbExpect(native == leaf.CasterLevelStrength.ValueAt(level),
                    "Level " + level + ": native progression " + native + " vs modelled " +
                    leaf.CasterLevelStrength.ValueAt(level));
            }
            Func<int, int, ExistingEffectVerdict> verdict = (existing, planned) => ExistingEffectSufficiency.Assess(
                "wizard", party.Effects[fire.Canonical],
                new[] { new ActiveEffectInstance(EffectKind.Buff, fireBuff, null, existing, 0) }, null,
                new ExistingEffectRequirement(planned, 0, 0, false)).Verdict;
            MbbExpect(verdict(6, 7) == ExistingEffectVerdict.Sufficient &&
                verdict(10, 11) == ExistingEffectVerdict.Insufficient &&
                verdict(19, 20) == ExistingEffectVerdict.Insufficient &&
                verdict(12, 19) == ExistingEffectVerdict.Sufficient,
                "Tier transitions: 6/7 " + verdict(6, 7) + ", 10/11 " + verdict(10, 11) + ", 19/20 " +
                verdict(19, 20) + ", 12/19 " + verdict(12, 19));
        }

        // Review R7-F4: native Prayer with skip-if-active. A sufficient
        // PrayerBuff on the caster and only a sufficient enemy PrayerDebuff on
        // the friendly ally: one funded cast and no already-covered note for
        // the ally. PrayerBuff on both: a zero-cost skip.
        private static void TestMbbPrayerPenaltyNotCoverage()
        {
            var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff);
            float radius = 40f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "ally", radius * 0.5f }
            };
            MbbGroup group = MbbGroupCaster(dump, MbbPrayer, positions, id => id == "caster");
            Func<string, ActiveEffectSnapshot> live = allyEffect => ActiveEffectSnapshot.FromInstances(
                new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "caster", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerBuff, 5000, 20, 0) } },
                    { "ally", new[] { new ActiveEffectInstance(EffectKind.Buff, allyEffect, 5000, 20, 0) } }
                });
            Func<ActiveEffectSnapshot, ResolvedCasting> compile = effects => new ExplicitCastingCompiler().Compile(
                    CastingDocument(GroupCasting("prayer", "long", "caster", group.Ability.Canonical, group.Ability,
                        new[] { "caster", "ally" })),
                    group.Snapshot, group.Options, group.Effects, new CastEnhancementSnapshot[0], null, null, false,
                    effects).Castings.Single();
            ResolvedCasting penalized = compile(live(MbbPrayerDebuff));
            MbbExpect(penalized.Readiness == ResolvedCastingReadiness.Ready && penalized.Cost.Sum(c => c.Units) == 1 &&
                !penalized.ExistingEffectNotes.Any(note => note.StartsWith("already-", StringComparison.Ordinal) &&
                    note.Contains(":ally")),
                "Prayer's enemy penalty satisfied a friendly ally: " + penalized.Readiness + " " +
                string.Join(" | ", penalized.ExistingEffectNotes));
            ResolvedCasting covered = compile(live(MbbPrayerBuff));
            MbbExpect(covered.Readiness == ResolvedCastingReadiness.AlreadySatisfied && covered.Cost.Count == 0,
                "Prayer on both was not a zero-cost skip: " + covered.Readiness);
        }

        // Review R7-F5: Mass Blessing is funded first; then Feather Step,
        // Mass whose application sits on an EMPTY native checker's true branch
        // and whose false branch (never run) removes Mass Blessing's buff. The
        // second casting is Ready for one slot with no removal conflict; with
        // the removal on the true branch it is refused.
        private static void TestMbbEmptyCheckerRemovalNoConflict()
        {
            string blessing = MbbPointGroupSpells[0][1], blessingBuff = MbbPointGroupSpells[0][2];
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            var positions = new Dictionary<string, float>(StringComparer.Ordinal) { { "w", 0f }, { "ally", 1f } };
            foreach (bool reachable in new[] { false, true })
            {
                var dump = new NativeBlueprintDump(blessing, blessingBuff, feather, featherBuff);
                BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
                var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
                var remove = NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff>();
                SetNative(remove, "Buff", dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(blessingBuff));
                var gate = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
                SetNative(gate, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
                {
                    Conditions = new Kingmaker.ElementsSystem.Condition[0]
                });
                var applications = run.Actions.Actions;
                SetNative(gate, "IfTrue", new Kingmaker.ElementsSystem.ActionList
                {
                    Actions = reachable ? applications.Concat(new Kingmaker.ElementsSystem.GameAction[] { remove })
                        .ToArray() : applications
                });
                SetNative(gate, "IfFalse", new Kingmaker.ElementsSystem.ActionList
                {
                    Actions = reachable ? new Kingmaker.ElementsSystem.GameAction[0]
                        : new Kingmaker.ElementsSystem.GameAction[] { remove }
                });
                run.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { gate };
                MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                    new Dictionary<string, string[]> { { "w", new[] { blessing, feather } } }, positions);
                AbilityKey blessingKey = MbbSpellKey(blessing), featherKey = MbbSpellKey(feather);
                ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("bless", "long", "w", blessingKey.Canonical, blessingKey, new[] { "w", "ally" }),
                        GroupCasting("step", "long", "w", featherKey.Canonical, featherKey, new[] { "w", "ally" })),
                    party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0]);
                ResolvedCasting first = plan.CastingById("bless"), second = plan.CastingById("step");
                if (!reachable)
                    MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready &&
                        second.Readiness == ResolvedCastingReadiness.Ready && second.Cost.Sum(c => c.Units) == 1 &&
                        !second.ReadinessReasons.Any(r => r.StartsWith(ExplicitCastingCompiler.RemovesPlannedEffect,
                            StringComparison.Ordinal)),
                        "An empty checker's unreachable removal blocked the casting: " + second.Readiness + " " +
                        string.Join(",", second.ReadinessReasons));
                else
                    MbbExpect(second.Readiness == ResolvedCastingReadiness.Blocked && second.Cost.Count == 0 &&
                        second.ReadinessReasons.Any(r => r.StartsWith(ExplicitCastingCompiler.RemovesPlannedEffect +
                            ":bless:" + blessingBuff, StringComparison.Ordinal)),
                        "A reachable removal of the planned Mass Blessing was not refused: " + second.Readiness + " " +
                        string.Join(",", second.ReadinessReasons));
            }
        }

        // Review R7-F6: allies at 0, 10 and 20 feet; the latter two under
        // Crushing Despair. Good Hope centred at 10 feet (first routine)
        // benefits the ally at 0 and clears both Despairs; Good Hope centred
        // at 20 feet (next routine) then benefits the allies at 10 and 20 for
        // its own slot. Previewed alone, the second routine has nobody.
        private static void TestMbbSequentialGoodHopeGainsAllies()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            string despair = "buff-predicate:" + MbbCrushingDespairBuff;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 30f }, { "a", 0f }, { "m", 10f * 0.3048f }, { "f", 20f * 0.3048f }
            };
            MbbParty party = MbbGoodHopeParty(dump, positions,
                (unit, fact) => fact == despair && (unit == "m" || unit == "f"), 2, id => id == "m" || id == "f");
            AbilityKey hope = party.Abilities[MbbGoodHope];
            PlannedCasting second = GroupCasting("second", "important", "bard", hope.Canonical, hope,
                new[] { "m", "f" }, null, CastingTargetMode.AnchoredOrigin, "f");
            CastingForecast onePass = new CastingForecastService().ForecastOnePass(CastingDocument(
                    GroupCasting("first", "long", "bard", hope.Canonical, hope, new[] { "a" }, null,
                        CastingTargetMode.AnchoredOrigin, "m"), second),
                party.Snapshot, party.Options, party.Effects);
            ResolvedCasting first = onePass.Plan.CastingById("first"), later = onePass.Plan.CastingById("second");
            MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(c => c.Units) == 1 &&
                first.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "a" }) &&
                later.Readiness == ResolvedCastingReadiness.Ready && later.Cost.Sum(c => c.Units) == 1 &&
                later.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal).SequenceEqual(new[] { "f", "m" }),
                "The sequential Good Hope did not gain the allies whose Despair was removed: " + later.Readiness +
                " [" + string.Join(",", later.PredictedBeneficiaryUnitIds) + "] " + string.Join(",", later.ReadinessReasons));
            ResolvedCasting alone = new ExplicitCastingCompiler().Compile(CastingDocument(second), party.Snapshot,
                party.Options, party.Effects, new CastEnhancementSnapshot[0]).Castings.Single();
            MbbExpect(alone.Readiness == ResolvedCastingReadiness.Blocked && alone.Cost.Count == 0 &&
                alone.PredictedBeneficiaryUnitIds.Count == 0,
                "The second routine previewed alone no longer starts from its own snapshot: " + alone.Readiness);
        }

        // The Classic sequential forecast of the same two routines.
        private static void TestMbbClassicSequentialGoodHopeGainsAllies()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            string despair = "buff-predicate:" + MbbCrushingDespairBuff;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 30f }, { "a", 0f }, { "m", 10f * 0.3048f }, { "f", 20f * 0.3048f }
            };
            MbbParty party = MbbGoodHopeParty(dump, positions,
                (unit, fact) => fact == despair && (unit == "m" || unit == "f"), 2, id => id == "m" || id == "f");
            AbilityKey hope = party.Abilities[MbbGoodHope];
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-sequential-good-hope");
            profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(Assignment(
                hope.Canonical, hope, new[] { "a" }));
            profile.Routines.First(rt => rt.RoutineId == "short").Assignments.Add(Assignment(
                hope.Canonical, hope, new[] { "m", "f" }));
            SequentialForecastPlanner.Result classic = SequentialForecastPlanner.Compute(profile,
                new[] { "long", "short" }, party.Snapshot, new ActiveEffectSnapshot(null), party.Effects,
                party.Options, new CastEnhancementSnapshot[0]);
            CastStep later = classic.Occurrences[1].Plan.Steps.SingleOrDefault();
            MbbExpect(classic.Occurrences[0].Plan.Steps.Count == 1 && later != null &&
                later.Reservation.Units == 1 && later.ExpectedRecipientUnitIds.Contains("m") &&
                later.ExpectedRecipientUnitIds.Contains("f"),
                "The Classic sequential Good Hope did not gain the allies whose Despair was removed: " +
                (later == null ? "no step" : string.Join(",", later.ExpectedRecipientUnitIds)) + " / " +
                string.Join(",", classic.Occurrences[1].Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
        }

        // A buff gains an AddFactContextActions running the given actions when
        // it activates.
        private static void MbbAddActivation(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff buff,
            params Kingmaker.ElementsSystem.GameAction[] actions)
        {
            var activation = NativeObject<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>();
            SetNative(activation, "Activated", new Kingmaker.ElementsSystem.ActionList { Actions = actions });
            SetNative(activation, "Deactivated", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            SetNative(activation, "NewRound", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            SetNative(buff, "Components", (buff.ComponentsArray ?? new Kingmaker.Blueprints.BlueprintComponent[0])
                .Concat(new Kingmaker.Blueprints.BlueprintComponent[] { activation }).ToArray());
        }

        // A native action nested inside `levels` empty-checker Conditionals.
        private static Kingmaker.ElementsSystem.GameAction MbbDeeplyNested(Kingmaker.ElementsSystem.GameAction action,
            int levels)
        {
            Kingmaker.ElementsSystem.GameAction current = action;
            for (int level = 0; level < levels; level++)
            {
                var conditional = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
                SetNative(conditional, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
                {
                    Conditions = new Kingmaker.ElementsSystem.Condition[0]
                });
                SetNative(conditional, "IfTrue", new Kingmaker.ElementsSystem.ActionList
                {
                    Actions = new Kingmaker.ElementsSystem.GameAction[] { current }
                });
                SetNative(conditional, "IfFalse", new Kingmaker.ElementsSystem.ActionList
                {
                    Actions = new Kingmaker.ElementsSystem.GameAction[0]
                });
                current = conditional;
            }
            return current;
        }

        // Review R8 (R7-F1): (a) Good Hope keeps its shallow approved
        // application while its Despair branch holds damage beyond the scan
        // bound (the scanner stops with maximum-depth); (b) Feather Step,
        // Mass's buff activates a second buff whose own activation damages its
        // bearer (an automatic chain the export does not read). Both refused
        // through discovery and provider admission; the unmodified spells stay.
        private static void TestMbbIncompleteScanRefused()
        {
            var hopeDump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            MbbExpect(MbbAppendToFirstConditionalTrue(hopeDump.Get<BlueprintAbility>(MbbGoodHope),
                    MbbDeeplyNested(NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>(), 70)),
                "Good Hope's Despair branch could not be extended.");
            MbbDiscovery deep = MbbDiscover(hopeDump, MbbGoodHope);
            var hopeProviders = new List<ProviderSnapshot>();
            ProviderSnapshot hopeProvider = MbbAdmission(hopeDump).AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(hopeDump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, "pool", 1,
                new string[0], hopeProviders);
            MbbExpect(deep.Scan.Diagnostics.Any(d => d.Code == "maximum-depth") &&
                !deep.Scan.Diagnostics.Any(d => d.Code == "offensive-action") &&
                !deep.Included && hopeProvider == null,
                "Good Hope with an unread action beyond the scan bound was admitted: " + deep.Repaired.Reason);
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            var chainDump = new NativeBlueprintDump(feather, featherBuff, MbbHasteBuff);
            var second = chainDump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbHasteBuff);
            MbbAddActivation(second, NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>());
            MbbAddActivation(chainDump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(featherBuff),
                MbbApplicationCopy(chainDump.Get<BlueprintAbility>(feather), second));
            MbbDiscovery chain = MbbDiscover(chainDump, feather);
            var chainProviders = new List<ProviderSnapshot>();
            ProviderSnapshot chainProvider = MbbAdmission(chainDump).AdmitSpell("caster", "book", false, "Book",
                MbbRead(chainDump, feather, feather, SourceKind.Spellbook, 9), 3, 0, "pool", 1, new string[0],
                chainProviders);
            MbbExpect(chain.Facts.Effects.Any(e => e.EffectId == featherBuff &&
                    (e.FactActions ?? new NativeCandidateFactActions[0]).Any(list =>
                        (list.AppliedEffects ?? new NativeCandidateEffectFacts[0]).Any(a => a.EffectId == MbbHasteBuff))),
                "The export did not record the automatically applied second buff.");
            MbbExpect(!chain.Included && chainProvider == null,
                "A point-capable burst with an uninspected harmful activation chain was admitted: " + chain.Repaired.Reason);
            MbbExpect(MbbDiscover(new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff), MbbGoodHope).Included &&
                MbbDiscover(new NativeBlueprintDump(feather, featherBuff), feather).Included,
                "Unmodified Good Hope or Feather Step, Mass is no longer admitted.");
        }

        // Review R8-F1: two equal-level Firebrand casters a and b over the
        // same allies. An existing Firebrand from a, or a funded earlier
        // Firebrand from a, does not satisfy b's casting (Firebrand's
        // immunity is to its own caster's fire); b's own instance does.
        private static void TestMbbFirebrandCasterOrigin()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "a", 0f }, { "b", 1f }, { "ally", 2f }
            };
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                positions);
            AbilityKey key = MbbSpellKey(firebrand);
            MbbExpect(EffectApplicationLifetime.Leaves(party.Effects[key.Canonical])
                    .Any(leaf => leaf.EffectId == firebrandBuff && leaf.CasterRelative),
                "Firebrand's buff was not recognized as caster-relative.");
            Func<string, ActiveEffectSnapshot> live = origin => ActiveEffectSnapshot.FromInstances(
                new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "b", new[] { new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, null, 20, 0, false, true, origin) } },
                    { "ally", new[] { new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, null, 20, 0, false, true, origin) } },
                    { "a", new[] { new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, null, 20, 0, false, true, origin) } }
                });
            PlannedCasting byB = GroupCasting("b-cast", "important", "b", key.Canonical, key, new[] { "b", "ally" });
            Func<string, ResolvedCasting> compileB = origin => new ExplicitCastingCompiler().Compile(
                    CastingDocument(byB), party.Snapshot, party.Options, party.Effects,
                    new CastEnhancementSnapshot[0], null, null, false, live(origin)).CastingById("b-cast");
            ResolvedCasting otherCaster = compileB("a"), sameCaster = compileB("b");
            MbbExpect(otherCaster.Readiness == ResolvedCastingReadiness.Ready && otherCaster.Cost.Sum(c => c.Units) == 1,
                "Wizard a's Firebrand satisfied wizard b's casting: " + otherCaster.Readiness + " " +
                string.Join(" | ", otherCaster.ExistingEffectNotes));
            MbbExpect(sameCaster.Readiness == ResolvedCastingReadiness.AlreadySatisfied && sameCaster.Cost.Count == 0,
                "Wizard b's own Firebrand no longer satisfies b's casting: " + sameCaster.Readiness);
            CastingForecast projected = new CastingForecastService().ForecastOnePass(CastingDocument(
                    GroupCasting("a-cast", "long", "a", key.Canonical, key, new[] { "a", "ally" }), byB),
                party.Snapshot, party.Options, party.Effects);
            ResolvedCasting projectedB = projected.Plan.CastingById("b-cast");
            MbbExpect(projected.Plan.CastingById("a-cast").Readiness == ResolvedCastingReadiness.Ready &&
                projectedB.Readiness == ResolvedCastingReadiness.Ready && projectedB.Cost.Sum(c => c.Units) == 1,
                "A funded Firebrand from a satisfied b's later casting: " + projectedB.Readiness + " " +
                string.Join(",", projectedB.ReadinessReasons));
            CastingForecast repeated = new CastingForecastService().ForecastOnePass(CastingDocument(
                    GroupCasting("a-cast", "long", "a", key.Canonical, key, new[] { "a", "ally" }),
                    GroupCasting("a-again", "important", "a", key.Canonical, key, new[] { "a", "ally" })),
                party.Snapshot, party.Options, party.Effects);
            MbbExpect(repeated.Plan.CastingById("a-again").Readiness == ResolvedCastingReadiness.AlreadySatisfied &&
                repeated.Plan.CastingById("a-again").Cost.Count == 0,
                "The same caster's repeated Firebrand lost its structural grant.");
        }

        // Review R8-F2: Resistance - Fire keeps its native resistance
        // application but also applies the committed Crushing Despair, or
        // runs a native ContextActionKillTarget beside it. Personal targeting
        // no longer admits either; the five real selections stay.
        private static void TestMbbPersonalRescueWhole()
        {
            string fire = MbbAbjuration[3][1];
            var all = MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToList();
            foreach (bool kill in new[] { false, true })
            {
                var dump = new NativeBlueprintDump(all.Concat(new[] { MbbCrushingDespairBuff }).ToArray());
                BlueprintAbility ability = dump.Get<BlueprintAbility>(fire);
                if (kill)
                {
                    var run = ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
                    run.Actions.Actions = run.Actions.Actions.Concat(new Kingmaker.ElementsSystem.GameAction[]
                    {
                        NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKillTarget>()
                    }).ToArray();
                }
                else
                    MbbExpect(MbbAppendApplication(ability,
                            dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbCrushingDespairBuff)),
                        "Resistance - Fire could not be extended.");
                MbbDiscovery harmed = MbbDiscover(dump, fire);
                var logic = ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>()
                    .Single();
                KingmakerFactResource resource;
                string refusal;
                bool known = KingmakerFactResources.TryPriceAbilityResource("wizard", logic.RequiredResource.AssetGuid,
                    logic.Amount, 1, () => 1, 1, false, string.Empty, string.Empty, out resource, out refusal);
                var providers = new List<ProviderSnapshot>();
                MbbAdmission(dump).AdmitFact("wizard", MbbRead(dump, fire, "Resistance - Fire", SourceKind.AbilityResource, 7),
                    known, resource, refusal, string.Empty, providers, new List<ResourcePoolSnapshot>(),
                    new HashSet<string>(StringComparer.Ordinal));
                MbbExpect(harmed.Facts.Effects.Any(e => e.EffectId == MbbAbjuration[3][2]),
                    "The resistance application was not retained.");
                MbbExpect(!harmed.Included && providers.Count == 0,
                    "Resistance - Fire with " + (kill ? "a kill action" : "Crushing Despair") +
                    " beside the resistance was admitted: " + harmed.Repaired.Reason);
            }
            var real = new NativeBlueprintDump(all.ToArray());
            MbbExpect(MbbAbjuration.All(row => MbbDiscover(real, row[1]).Included),
                "A real Abjuration resistance selection is no longer admitted.");
        }

        // Review R8-F3: the 0/10/20-foot Good Hope fixture with both casts in
        // ONE routine and two slots: two funded casts, the second for the
        // allies at 10 and 20 feet, in the plain compiler, the selected-run
        // compiler and the one-pass forecast. With the first disabled, the
        // second is empty.
        private static void TestMbbSameRoutineGoodHopeGainsAllies()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            string despair = "buff-predicate:" + MbbCrushingDespairBuff;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 30f }, { "a", 0f }, { "m", 10f * 0.3048f }, { "f", 20f * 0.3048f }
            };
            MbbParty party = MbbGoodHopeParty(dump, positions,
                (unit, fact) => fact == despair && (unit == "m" || unit == "f"), 2, id => id == "m" || id == "f");
            AbilityKey hope = party.Abilities[MbbGoodHope];
            Func<CastingAuthoringState, CastingPlanDocument> document = firstState => CastingDocument(
                GroupCasting("first", "long", "bard", hope.Canonical, hope, new[] { "a" }, null,
                    CastingTargetMode.AnchoredOrigin, "m", firstState),
                GroupCasting("second", "long", "bard", hope.Canonical, hope, new[] { "m", "f" }, null,
                    CastingTargetMode.AnchoredOrigin, "f"));
            var plans = new Dictionary<string, ExplicitCastingPlan>(StringComparer.Ordinal)
            {
                { "compiler", new ExplicitCastingCompiler().Compile(document(CastingAuthoringState.Ready),
                    party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0]) },
                { "selected-run", new ExplicitCastingCompiler().Compile(document(CastingAuthoringState.Ready),
                    party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], "long") },
                { "one-pass", new CastingForecastService().ForecastOnePass(document(CastingAuthoringState.Ready),
                    party.Snapshot, party.Options, party.Effects).Plan }
            };
            foreach (KeyValuePair<string, ExplicitCastingPlan> plan in plans)
            {
                ResolvedCasting first = plan.Value.CastingById("first"), second = plan.Value.CastingById("second");
                MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(c => c.Units) == 1 &&
                    first.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "a" }) &&
                    second.Readiness == ResolvedCastingReadiness.Ready && second.Cost.Sum(c => c.Units) == 1 &&
                    second.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                        .SequenceEqual(new[] { "f", "m" }),
                    plan.Key + ": the same-routine Good Hope did not gain the allies whose Despair was removed: " +
                    second.Readiness + " [" + string.Join(",", second.PredictedBeneficiaryUnitIds) + "] " +
                    string.Join(",", second.ReadinessReasons));
            }
            ResolvedCasting alone = new ExplicitCastingCompiler().Compile(document(CastingAuthoringState.Disabled),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0]).CastingById("second");
            MbbExpect(alone.Readiness == ResolvedCastingReadiness.Blocked && alone.Cost.Count == 0 &&
                alone.PredictedBeneficiaryUnitIds.Count == 0,
                "With the first Good Hope disabled the second is not empty: " + alone.Readiness);
        }

        // Review R8-F4: the Classic planner (RoutinePlanService, pinned
        // provider) with PrayerBuff on the caster and only the enemy
        // PrayerDebuff on the friendly ally: one funded step. PrayerBuff on
        // both: a free skip.
        private static void TestMbbClassicPrayerBranch()
        {
            var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff);
            float radius = 40f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "ally", radius * 0.5f }
            };
            MbbGroup group = MbbGroupCaster(dump, MbbPrayer, positions, id => id == "caster");
            Func<string, RoutinePlanResult> plan = allyEffect =>
            {
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-prayer");
                SourceAssignmentProfile assignment = Assignment(group.Ability.Canonical, group.Ability,
                    new[] { "caster", "ally" });
                assignment.CastingAssignments[0].CasterUnitId = "caster";
                profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
                var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "caster", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerBuff, 5000, 20, 0) } },
                    { "ally", new[] { new ActiveEffectInstance(EffectKind.Buff, allyEffect, 5000, 20, 0) } }
                });
                return new RoutinePlanService().Plan(profile, "long", group.Snapshot, live, group.Effects,
                    group.Options, new CastEnhancementSnapshot[0]);
            };
            RoutinePlanResult penalized = plan(MbbPrayerDebuff), covered = plan(MbbPrayerBuff);
            MbbExpect(penalized.Plan.Steps.Count == 1 &&
                !penalized.Plan.Outcomes.Any(o => o.UnitId == "ally" && o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                "Classic counted Prayer's enemy penalty as friendly coverage: " +
                string.Join(",", penalized.Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
            MbbExpect(covered.Plan.Steps.Count == 0 &&
                covered.Plan.Outcomes.All(o => o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                "Classic no longer skips Prayer active on both: " +
                string.Join(",", covered.Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
        }

        // Review R8-F5: Feather Step, Mass with its application on an empty
        // native checker's true branch and a different beneficial buff
        // (Haste's) on the never-run false branch. With only that buff active
        // on everyone, one funded Feather Step; a fresh observation of only
        // that buff does not confirm delivery.
        private static void TestMbbEmptyCheckerPresence()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            var dump = new NativeBlueprintDump(feather, featherBuff, MbbHasteBuff);
            BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
            var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            var other = MbbApplicationCopy(spell, dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbHasteBuff));
            var gate = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
            SetNative(gate, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
            {
                Conditions = new Kingmaker.ElementsSystem.Condition[0]
            });
            SetNative(gate, "IfTrue", new Kingmaker.ElementsSystem.ActionList { Actions = run.Actions.Actions });
            SetNative(gate, "IfFalse", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[] { other }
            });
            run.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { gate };
            float radius = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "ally", radius * 0.5f }
            };
            MbbGroup group = MbbGroupCaster(dump, feather, positions, id => true);
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "caster", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbHasteBuff, null, 20, 0) } },
                { "ally", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbHasteBuff, null, 20, 0) } }
            });
            ResolvedCasting casting = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("step", "long", "caster", group.Ability.Canonical, group.Ability,
                        new[] { "caster", "ally" })),
                group.Snapshot, group.Options, group.Effects, new CastEnhancementSnapshot[0], null, null, false, live)
                .Castings.Single();
            MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready && casting.Cost.Sum(c => c.Units) == 1,
                "An unreachable false-branch buff satisfied the empty-checker cast: " + casting.Readiness);
            EffectExpression expected = group.Effects[group.Ability.Canonical];
            var before = new Dictionary<string, IEnumerable<ObservedEffectInstance>>(StringComparer.Ordinal)
            {
                { "caster", new ObservedEffectInstance[0] }, { "ally", new ObservedEffectInstance[0] }
            };
            MbbExpect(!AppliedEffectJudgement.AnyReached(new[] { "caster", "ally" }, expected, new EffectBaseline(before),
                    unit => new[] { new ObservedEffectInstance(EffectKind.Buff, MbbHasteBuff, unit + "-haste", 5000, false) },
                    "caster"),
                "A fresh observation of only the false-branch buff confirmed delivery.");
        }

        // Review R8-F6: native CanTarget admits two centres for Feather Step,
        // Mass but one centre's own position cannot be read. That centre is
        // not a legal anchor (no cast, no reservation); the readable centre
        // casts once for its own recipients.
        private static void TestMbbUnreadableAnchor()
        {
            string feather = MbbPointGroupSpells[1][1];
            var dump = new NativeBlueprintDump(feather, MbbPointGroupSpells[1][2]);
            float radius = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 20f }, { "ally", 0f }, { "near", radius * 0.5f }
            };
            MbbGroup group = MbbGroupCaster(dump, feather, positions, id => true);
            List<UnitSnapshot> units = MbbUnits(new[] { "caster", "ally", "near", "ghost" });
            var snapshot = new PartyProviderSnapshot(units, group.Snapshot.Providers, group.Snapshot.ResourcePools);
            ProviderSnapshot provider = group.Snapshot.Providers.Single();
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(feather);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            Func<string, string, float?> distance = (from, to) =>
                from == "ghost" || to == "ghost" ? (float?)null : MbbDistance(positions)(from, to);
            ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider,
                group.Effects[group.Ability.Canonical], units, true, false, area, id => true, distance, null,
                MbbFriendlyReads(null));
            ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast");
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("at-ghost", "long", "caster", group.Ability.Canonical, group.Ability,
                        new[] { "ghost" }, null, CastingTargetMode.AnchoredOrigin, "ghost"),
                    GroupCasting("at-ally", "long", "caster", group.Ability.Canonical, group.Ability,
                        new[] { "ally" }, null, CastingTargetMode.AnchoredOrigin, "ally")),
                snapshot, new List<ProviderPlanningOption> { option }, group.Effects, new CastEnhancementSnapshot[0]);
            ResolvedCasting ghost = plan.CastingById("at-ghost"), ally = plan.CastingById("at-ally");
            MbbExpect(!decision.Anchors.Contains("ghost") && ghost.Readiness == ResolvedCastingReadiness.Blocked &&
                ghost.Cost.Count == 0 && ghost.PredictedBeneficiaryUnitIds.Count == 0,
                "An unreadable centre inherited coverage: " + ghost.Readiness + " [" +
                string.Join(",", ghost.PredictedBeneficiaryUnitIds) + "]");
            MbbExpect(ally.Readiness == ResolvedCastingReadiness.Ready && ally.Cost.Sum(c => c.Units) == 1 &&
                ally.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                    .SequenceEqual(new[] { "ally", "near" }),
                "The readable centre lost its own recipients: " + ally.Readiness + " [" +
                string.Join(",", ally.PredictedBeneficiaryUnitIds) + "]");
        }

        // Review R9: every subcase runs and reports, so one failing subcase
        // never masks another (in the executed baseline too).
        private static void MbbSubcases(params KeyValuePair<string, Action>[] subcases)
        {
            var failures = new List<string>();
            foreach (KeyValuePair<string, Action> subcase in subcases)
            {
                try { subcase.Value(); }
                catch (Exception exception) { failures.Add(subcase.Key + ": " + exception.Message); }
            }
            MbbExpect(failures.Count == 0, string.Join(" || ", failures.ToArray()));
        }

        private static KeyValuePair<string, Action> MbbCase(string name, Action body)
        {
            return new KeyValuePair<string, Action>(name, body);
        }

        // Review R9 (R7-F1): Feather Step, Mass (a) with its direct buff
        // withheld from the exporter's blueprint lookup, (b) with its buff
        // read but the second buff that buff applies on activation withheld.
        // Both are refused by discovery and provider admission; the fully
        // resolved spell stays admitted.
        private static void TestMbbUnreadBuffRefused()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            MbbSubcases(
                MbbCase("direct buff unread", () =>
                {
                    var dump = new NativeBlueprintDump(feather);
                    MbbDiscovery unread = MbbDiscover(dump, feather);
                    var providers = new List<ProviderSnapshot>();
                    ProviderSnapshot provider = MbbAdmission(dump).AdmitSpell("caster", "book", false, "Book",
                        MbbRead(dump, feather, feather, SourceKind.Spellbook, 9), 3, 0, "pool", 1, new string[0],
                        providers);
                    MbbExpect(unread.Facts.Effects.Any(e => e.EffectId == featherBuff && e.Harmful == null),
                        "The withheld direct buff is not an unread record.");
                    MbbExpect(!unread.Included && provider == null && providers.Count == 0,
                        "A point-capable burst whose direct buff could not be read was admitted: " +
                        unread.Repaired.Reason);
                }),
                MbbCase("activation buff unread", () =>
                {
                    var dump = new NativeBlueprintDump(feather, featherBuff);
                    // A detached copy from another dump: this dump's lookup
                    // cannot resolve it.
                    var withheld = new NativeBlueprintDump(MbbHasteBuff)
                        .Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbHasteBuff);
                    MbbAddActivation(dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(featherBuff),
                        MbbApplicationCopy(dump.Get<BlueprintAbility>(feather), withheld));
                    MbbDiscovery unread = MbbDiscover(dump, feather);
                    var providers = new List<ProviderSnapshot>();
                    ProviderSnapshot provider = MbbAdmission(dump).AdmitSpell("caster", "book", false, "Book",
                        MbbRead(dump, feather, feather, SourceKind.Spellbook, 9), 3, 0, "pool", 1, new string[0],
                        providers);
                    MbbExpect(unread.Facts.Effects.Any(e => e.EffectId == featherBuff && e.Harmful == false &&
                            (e.FactActions ?? new NativeCandidateFactActions[0]).Any(list =>
                                (list.AppliedEffects ?? new NativeCandidateEffectFacts[0]).Any(applied =>
                                    applied.EffectId == MbbHasteBuff && applied.Harmful == null))),
                        "The withheld activation buff is not an unread applied record.");
                    MbbExpect(!unread.Included && provider == null && providers.Count == 0,
                        "A point-capable burst whose buff applies an unread buff was admitted: " +
                        unread.Repaired.Reason);
                }),
                MbbCase("resolved control", () => MbbExpect(
                    MbbDiscover(new NativeBlueprintDump(feather, featherBuff), feather).Included,
                    "Unmodified Feather Step, Mass is no longer admitted.")));
        }

        // Review R9 (R8-F2): Resistance - Fire with its own application set
        // to ToCaster and the native ContextActionKillTarget beside it is
        // refused by discovery and fact-provider admission; ToCaster alone
        // stays admitted, and the five real selections keep their removals
        // of the other four resistances.
        private static void TestMbbCasterDirectedRescueWhole()
        {
            string fire = MbbAbjuration[3][1], fireBuff = MbbAbjuration[3][2];
            string[] all = MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray();
            Func<bool, KeyValuePair<MbbDiscovery, int>> run = kill =>
            {
                var dump = new NativeBlueprintDump(all);
                BlueprintAbility ability = dump.Get<BlueprintAbility>(fire);
                MbbExpect(MbbReplaceApplications(ability, apply => SetNative(apply, "ToCaster", true)) == 1,
                    "Resistance - Fire's application could not be redirected.");
                if (kill)
                {
                    var effect = ability.ComponentsArray
                        .OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
                    effect.Actions.Actions = effect.Actions.Actions.Concat(new Kingmaker.ElementsSystem.GameAction[]
                    {
                        NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKillTarget>()
                    }).ToArray();
                }
                MbbDiscovery result = MbbDiscover(dump, fire);
                var logic = ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>()
                    .Single();
                KingmakerFactResource resource;
                string refusal;
                bool known = KingmakerFactResources.TryPriceAbilityResource("wizard", logic.RequiredResource.AssetGuid,
                    logic.Amount, 1, () => 1, 1, false, string.Empty, string.Empty, out resource, out refusal);
                var providers = new List<ProviderSnapshot>();
                MbbAdmission(dump).AdmitFact("wizard", MbbRead(dump, fire, "Resistance - Fire", SourceKind.AbilityResource, 7),
                    known, resource, refusal, string.Empty, providers, new List<ResourcePoolSnapshot>(),
                    new HashSet<string>(StringComparer.Ordinal));
                MbbExpect(result.Facts.Effects.Any(e => e.EffectId == fireBuff && e.Target == "Caster") &&
                    !result.Facts.Effects.Any(e => e.Target == "CurrentTarget"),
                    "The resistance application is not caster-directed.");
                return new KeyValuePair<MbbDiscovery, int>(result, providers.Count);
            };
            MbbSubcases(
                MbbCase("caster-directed with kill", () =>
                {
                    KeyValuePair<MbbDiscovery, int> killing = run(true);
                    MbbExpect(!killing.Key.Included && killing.Value == 0,
                        "A caster-directed Personal selection with a kill action was admitted: " +
                        killing.Key.Repaired.Reason);
                }),
                MbbCase("caster-directed control", () =>
                {
                    KeyValuePair<MbbDiscovery, int> plain = run(false);
                    MbbExpect(plain.Key.Included && plain.Value == 1,
                        "A harmless caster-directed resistance selection is no longer admitted: " +
                        plain.Key.Repaired.Reason);
                }),
                MbbCase("real selections and removals", () =>
                {
                    var real = new NativeBlueprintDump(all);
                    foreach (string[] row in MbbAbjuration)
                    {
                        MbbDiscovery selection = MbbDiscover(real, row[1]);
                        IReadOnlyList<EffectRemoval> removals = EffectRemovals.Collect(selection.Expression);
                        MbbExpect(selection.Included && MbbAbjuration.Where(other => other[0] != row[0])
                                .All(other => removals.Any(removal => removal.EffectId == other[2])),
                            "Resistance - " + row[0] + " is no longer admitted with its removals: " + selection.Reason);
                    }
                }));
        }

        // Review R9 (R8-F1): Firebrand by wizard a, then by wizard b over the
        // same allies, then a again (one-pass forecast): b's application may
        // replace a's (unverified offline), so a's repeat casts for its slot.
        // Controls: disjoint bursts, and the same overlap with b's casting
        // disabled, keep a's free structural repeat.
        private static void TestMbbFirebrandOverlap()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            AbilityKey key = MbbSpellKey(firebrand);
            Func<Dictionary<string, float>, string[], string[], CastingAuthoringState, ResolvedCasting> again =
                (positions, aMembers, bMembers, bState) =>
                {
                    MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                        new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                        positions);
                    return new CastingForecastService().ForecastOnePass(CastingDocument(
                            GroupCasting("a-cast", "long", "a", key.Canonical, key, aMembers),
                            GroupCasting("b-cast", "important", "b", key.Canonical, key, bMembers, null,
                                CastingTargetMode.CasterCenteredOrigin, null, bState),
                            GroupCasting("a-again", "short", "a", key.Canonical, key, aMembers)),
                        party.Snapshot, party.Options, party.Effects).Plan.CastingById("a-again");
                };
            var overlap = new Dictionary<string, float>(StringComparer.Ordinal) { { "a", 0f }, { "b", 1f }, { "ally", 2f } };
            var disjoint = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "a", 0f }, { "ally", 1f }, { "b", 100f }, { "ally2", 101f }
            };
            MbbSubcases(
                MbbCase("a, b, a overlapping", () =>
                {
                    ResolvedCasting repeat = again(overlap, new[] { "a", "ally" }, new[] { "b", "ally" },
                        CastingAuthoringState.Ready);
                    MbbExpect(repeat.Readiness == ResolvedCastingReadiness.Ready && repeat.Cost.Sum(c => c.Units) == 1,
                        "a's Firebrand stayed free after b applied Firebrand to the same allies: " + repeat.Readiness +
                        " " + string.Join(",", repeat.ReadinessReasons));
                }),
                MbbCase("disjoint control", () =>
                {
                    ResolvedCasting repeat = again(disjoint, new[] { "a", "ally" }, new[] { "b", "ally2" },
                        CastingAuthoringState.Ready);
                    MbbExpect(repeat.Readiness == ResolvedCastingReadiness.AlreadySatisfied && repeat.Cost.Count == 0,
                        "b's disjoint Firebrand cost a's free repeat: " + repeat.Readiness);
                }),
                MbbCase("uninterrupted control", () =>
                {
                    ResolvedCasting repeat = again(overlap, new[] { "a", "ally" }, new[] { "b", "ally" },
                        CastingAuthoringState.Disabled);
                    MbbExpect(repeat.Readiness == ResolvedCastingReadiness.AlreadySatisfied && repeat.Cost.Count == 0,
                        "a's uninterrupted repeat lost its free structural grant: " + repeat.Readiness);
                }));
        }

        // Review R9 (R8-F3): allies at 0, 10 and 20 feet; the middle one under
        // Crushing Despair, the far one with a sufficient Good Hope. Two slots;
        // the first Good Hope centred at 0 clears the middle ally's Despair, so
        // the second, centred at 20 and initially satisfied, now casts for its
        // slot for the newly eligible middle ally (plain and selected-run
        // compilers). Without the first, the far ally's free skip stays. A
        // later Classic assignment for the middle ally is planned too.
        private static void TestMbbSatisfiedGoodHopeGainsAlly()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            string despair = "buff-predicate:" + MbbCrushingDespairBuff;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 30f }, { "a", 0f }, { "m", 10f * 0.3048f }, { "f", 20f * 0.3048f }
            };
            MbbParty party = MbbGoodHopeParty(dump, positions, (unit, fact) => fact == despair && unit == "m", 2,
                id => id == "a" || id == "f");
            AbilityKey hope = party.Abilities[MbbGoodHope];
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "f", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbGoodHopeBuff, 5000, 20, 0) } }
            });
            Func<CastingAuthoringState, CastingPlanDocument> document = firstState => CastingDocument(
                GroupCasting("first", "long", "bard", hope.Canonical, hope, new[] { "a" }, null,
                    CastingTargetMode.AnchoredOrigin, "a", firstState),
                GroupCasting("second", "long", "bard", hope.Canonical, hope, new[] { "f" }, null,
                    CastingTargetMode.AnchoredOrigin, "f"));
            var cases = new List<KeyValuePair<string, Action>>();
            foreach (string scope in new[] { null, "long" })
            {
                string view = scope == null ? "compiler" : "selected-run";
                cases.Add(MbbCase(view, () =>
                {
                    ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document(CastingAuthoringState.Ready),
                        party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], scope, null, false,
                        live);
                    ResolvedCasting first = plan.CastingById("first"), second = plan.CastingById("second");
                    MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(c => c.Units) == 1 &&
                        first.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "a" }),
                        "The first Good Hope is not one funded cast for the near ally: " + first.Readiness);
                    MbbExpect(second.Readiness == ResolvedCastingReadiness.Ready && second.Cost.Sum(c => c.Units) == 1 &&
                        second.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                            .SequenceEqual(new[] { "f", "m" }),
                        "The initially satisfied Good Hope skipped the newly eligible ally: " + second.Readiness +
                        " [" + string.Join(",", second.PredictedBeneficiaryUnitIds) + "] " +
                        string.Join(",", second.ReadinessReasons));
                }));
            }
            cases.Add(MbbCase("first disabled", () =>
            {
                ResolvedCasting alone = new ExplicitCastingCompiler().Compile(document(CastingAuthoringState.Disabled),
                    party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live)
                    .CastingById("second");
                MbbExpect(alone.Readiness == ResolvedCastingReadiness.AlreadySatisfied && alone.Cost.Count == 0,
                    "Without the first Good Hope the far ally's skip is not free: " + alone.Readiness);
            }));
            cases.Add(MbbCase("classic later assignment", () =>
            {
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-good-hope-routine");
                SourceAssignmentProfile assignment = Assignment(hope.Canonical, hope, new[] { "a" });
                assignment.CastingAssignments[0].CasterUnitId = "bard";
                assignment.CastingAssignments.Add(new CastingAssignmentProfile
                {
                    AssignmentId = "later-middle",
                    Order = 1,
                    CasterUnitId = "bard",
                    TargetUnitIds = new List<string> { "m" },
                    Enhancements = new List<EnhancementSelectionProfile>()
                });
                profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
                RoutinePlanResult classic = new RoutinePlanService().Plan(profile, "long", party.Snapshot,
                    new ActiveEffectSnapshot(null), party.Effects, party.Options, new CastEnhancementSnapshot[0]);
                MbbExpect(classic.Plan.Steps.Count == 2 &&
                    classic.Plan.Outcomes.Any(o => o.UnitId == "m" && o.Kind == TargetOutcomeKind.Fulfilled) &&
                    classic.Plan.Steps[1].ExpectedRecipientUnitIds.Contains("m"),
                    "The later Classic assignment missed the ally whose Despair the first step cleared: " +
                    string.Join(",", classic.Plan.Outcomes.Select(o => o.AssignmentId + ":" + o.UnitId + "=" + o.Kind)));
            }));
            MbbSubcases(cases.ToArray());
        }

        // Review R9 (R8-F4): a Classic Prayer skip on a friendly ally protects
        // only PrayerBuff: a later Feather Step, Mass that also removes
        // PrayerDebuff is funded; one removing PrayerBuff is refused. A
        // friendly target outside every measured disk with only PrayerDebuff
        // and no recorded relation is not skipped.
        private static void TestMbbClassicPrayerSkipBranch()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            Func<string, RoutinePlanResult> plan = removed =>
            {
                var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff, feather, featherBuff);
                BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
                var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
                var remove = NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff>();
                SetNative(remove, "Buff", dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(removed));
                run.Actions.Actions = run.Actions.Actions.Concat(new Kingmaker.ElementsSystem.GameAction[] { remove })
                    .ToArray();
                var positions = new Dictionary<string, float>(StringComparer.Ordinal) { { "w", 0f }, { "ally", 1f } };
                MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                    new Dictionary<string, string[]> { { "w", new[] { MbbPrayer, feather } } }, positions);
                AbilityKey prayerKey = MbbSpellKey(MbbPrayer), featherKey = MbbSpellKey(feather);
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-prayer-skip");
                SourceAssignmentProfile prayer = Assignment(prayerKey.Canonical, prayerKey, new[] { "w", "ally" });
                prayer.CastingAssignments[0].CasterUnitId = "w";
                SourceAssignmentProfile step = Assignment(featherKey.Canonical, featherKey, new[] { "ally" });
                step.CastingAssignments[0].CasterUnitId = "w";
                step.CastingAssignments[0].Order = 1;
                var routine = profile.Routines.First(rt => rt.RoutineId == "long");
                routine.Assignments.Add(prayer);
                routine.Assignments.Add(step);
                var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "w", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerBuff, 5000, 20, 0) } },
                    { "ally", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerBuff, 5000, 20, 0) } }
                });
                return new RoutinePlanService().Plan(profile, "long", party.Snapshot, live, party.Effects,
                    party.Options, new CastEnhancementSnapshot[0]);
            };
            Func<RoutinePlanResult, string> describe = result => string.Join(",", result.Plan.Outcomes
                .Select(o => o.AssignmentId + ":" + o.UnitId + "=" + o.Kind + ":" + o.Reason));
            MbbSubcases(
                MbbCase("removing only the penalty", () =>
                {
                    RoutinePlanResult debuff = plan(MbbPrayerDebuff);
                    MbbExpect(debuff.Plan.Steps.Count == 1 &&
                        debuff.Plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.SkippedAlreadyActive) == 2 &&
                        debuff.Plan.Outcomes.Any(o => o.UnitId == "ally" && o.Kind == TargetOutcomeKind.Fulfilled),
                        "The friendly Prayer skip protected the enemy penalty: " + describe(debuff));
                }),
                MbbCase("removing the friendly buff", () =>
                {
                    RoutinePlanResult buff = plan(MbbPrayerBuff);
                    MbbExpect(buff.Plan.Steps.Count == 0 && buff.Plan.Outcomes.Any(o => o.UnitId == "ally" &&
                            o.Kind == TargetOutcomeKind.Unfulfilled &&
                            o.Reason.Contains(ExplicitCastingCompiler.RemovesPlannedEffect)),
                        "A cast removing the skipped PrayerBuff was not refused: " + describe(buff));
                }),
                MbbCase("unread relation", () =>
                {
                    var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff);
                    float radius = 40f * 0.3048f;
                    var positions = new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "caster", 0f }, { "ally", radius * 0.5f }, { "far", radius * 5f }
                    };
                    MbbGroup group = MbbGroupCaster(dump, MbbPrayer, positions, id => id == "caster");
                    BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-prayer-unread");
                    SourceAssignmentProfile assignment = Assignment(group.Ability.Canonical, group.Ability,
                        new[] { "caster", "ally", "far" });
                    assignment.CastingAssignments[0].CasterUnitId = "caster";
                    profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
                    var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                    {
                        { "caster", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerBuff, 5000, 20, 0) } },
                        { "ally", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerBuff, 5000, 20, 0) } },
                        { "far", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerDebuff, 5000, 20, 0) } }
                    });
                    RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long", group.Snapshot, live,
                        group.Effects, group.Options, new CastEnhancementSnapshot[0]);
                    MbbExpect(!result.Plan.Outcomes.Any(o => o.UnitId == "far" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive) &&
                        result.Plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.SkippedAlreadyActive) == 2,
                        "An unread relation let Prayer's penalty alone skip a friendly target: " + describe(result));
                }));
        }

        // Review R9 (R8-F6): Feather Step, Mass with a centre whose position
        // reads once and then becomes unavailable, and (separately) a centre
        // whose distance reads throw. Neither inherits another centre's
        // recipients; the throwing centre is no anchor (no cost, no step); the
        // readable centre keeps its own disk.
        private static void TestMbbFailedGeometryRead()
        {
            string feather = MbbPointGroupSpells[1][1];
            var dump = new NativeBlueprintDump(feather, MbbPointGroupSpells[1][2]);
            float radius = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 20f }, { "ally", 0f }, { "near", radius * 0.5f }, { "flaky", 50f }, { "broken", 80f }
            };
            MbbGroup group = MbbGroupCaster(dump, feather, positions, id => true);
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            var snapshot = new PartyProviderSnapshot(units, group.Snapshot.Providers, group.Snapshot.ResourcePools);
            ProviderSnapshot provider = group.Snapshot.Providers.Single();
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(feather);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            Func<string, Func<string, string, float?>, ExplicitCastingPlan> compile = (centre, distance) =>
            {
                ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider,
                    group.Effects[group.Ability.Canonical], units, true, false, area, id => true, distance, null,
                    MbbFriendlyReads(null));
                ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                    CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast");
                return new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("at-" + centre, "long", "caster", group.Ability.Canonical, group.Ability,
                            new[] { centre }, null, CastingTargetMode.AnchoredOrigin, centre),
                        GroupCasting("at-ally", "long", "caster", group.Ability.Canonical, group.Ability,
                            new[] { "ally" }, null, CastingTargetMode.AnchoredOrigin, "ally")),
                    snapshot, new List<ProviderPlanningOption> { option }, group.Effects, new CastEnhancementSnapshot[0]);
            };
            Action<ExplicitCastingPlan> readable = plan =>
            {
                ResolvedCasting ally = plan.CastingById("at-ally");
                MbbExpect(ally.Readiness == ResolvedCastingReadiness.Ready && ally.Cost.Sum(c => c.Units) == 1 &&
                    ally.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                        .SequenceEqual(new[] { "ally", "near" }),
                    "The readable centre lost its own recipients: " + ally.Readiness + " [" +
                    string.Join(",", ally.PredictedBeneficiaryUnitIds) + "]");
            };
            Action<float> nonFinite = reading =>
            {
                ExplicitCastingPlan plan = compile("flaky", (from, to) =>
                {
                    if (from == "broken" || to == "broken") return null;
                    if (from == "flaky" || to == "flaky") return from == to ? (float?)reading : null;
                    return MbbDistance(positions)(from, to);
                });
                ResolvedCasting flaky = plan.CastingById("at-flaky");
                MbbExpect(flaky.Readiness == ResolvedCastingReadiness.Blocked && flaky.Cost.Count == 0 &&
                    flaky.PredictedBeneficiaryUnitIds.Count == 0,
                    "A centre with a " + reading + " self-distance was planned: " + flaky.Readiness + " [" +
                    string.Join(",", flaky.PredictedBeneficiaryUnitIds) + "]");
                CastingApplyDecision gate = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
                ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, gate,
                    new List<ProviderPlanningOption>(), group.Effects);
                MbbExpect(!steps.Converted || steps.Plan.Steps.All(s => s.AnchorUnitId != "flaky"),
                    "A step was produced for the centre with unusable geometry.");
                readable(plan);
            };
            MbbSubcases(
                MbbCase("self read once, then unavailable", () =>
                {
                    int flakySelf = 0;
                    ExplicitCastingPlan plan = compile("flaky", (from, to) =>
                    {
                        if (from == "broken" || to == "broken") return null;
                        if (from == "flaky" || to == "flaky")
                            return from == to && flakySelf++ == 0 ? (float?)0f : null;
                        return MbbDistance(positions)(from, to);
                    });
                    ResolvedCasting flaky = plan.CastingById("at-flaky");
                    MbbExpect(!flaky.PredictedBeneficiaryUnitIds.Any(id => id == "ally" || id == "near"),
                        "A centre whose second read failed inherited another centre's recipients: [" +
                        string.Join(",", flaky.PredictedBeneficiaryUnitIds) + "]");
                    readable(plan);
                }),
                MbbCase("read failure for one of two centres", () =>
                {
                    ExplicitCastingPlan plan = compile("broken", (from, to) =>
                    {
                        if (from == "broken" || to == "broken")
                            throw new InvalidOperationException("position unreadable");
                        if (from == "flaky" || to == "flaky") return null;
                        return MbbDistance(positions)(from, to);
                    });
                    ResolvedCasting broken = plan.CastingById("at-broken");
                    MbbExpect(broken.Readiness == ResolvedCastingReadiness.Blocked && broken.Cost.Count == 0 &&
                        broken.PredictedBeneficiaryUnitIds.Count == 0,
                        "A centre whose read failed was planned: " + broken.Readiness + " [" +
                        string.Join(",", broken.PredictedBeneficiaryUnitIds) + "]");
                    CastingApplyDecision gate = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
                    ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, gate,
                        new List<ProviderPlanningOption>(), group.Effects);
                    MbbExpect(!steps.Converted || steps.Plan.Steps.All(s => s.AnchorUnitId != "broken"),
                        "A step was produced for the unreadable centre.");
                    readable(plan);
                }),
                // Review R10 (R8-F6): a non-finite reading is unusable geometry.
                MbbCase("NaN self-distance", () => nonFinite(float.NaN)),
                MbbCase("positive-infinity self-distance", () => nonFinite(float.PositiveInfinity)));
        }

        // Review R9-F1: no active resistance and ONE daily use; Fire then
        // Fire in one routine (selected-run and plain compilers): one paid
        // cast, one executable step, one reserved use, and a free second row.
        // Fire then Cold keeps its removal conflict.
        private static void TestMbbResistanceRepeat()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbParty party = MbbWizardParty(dump, 1);
            AbilityKey fire = party.Abilities["Fire"], cold = party.Abilities["Cold"];
            var cases = new List<KeyValuePair<string, Action>>();
            foreach (string scope in new[] { "long", null })
            {
                string view = scope == null ? "compiler" : "selected-run";
                cases.Add(MbbCase(view + " fire twice", () =>
                {
                    ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                            DirectCasting("fire-1", "long", "wizard", "wizard", fire.Canonical, fire),
                            DirectCasting("fire-2", "long", "wizard", "wizard", fire.Canonical, fire)),
                        party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], scope);
                    ResolvedCasting first = plan.CastingById("fire-1"), second = plan.CastingById("fire-2");
                    MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(c => c.Units) == 1 &&
                        second.Readiness == ResolvedCastingReadiness.AlreadySatisfied && second.Cost.Count == 0,
                        "A repeated Fire selection in one routine: " + first.Readiness + " / " + second.Readiness +
                        " " + string.Join(",", second.ReadinessReasons));
                    CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary,
                        "long");
                    ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, decision, party.Options,
                        party.Effects);
                    MbbExpect(steps.Converted && steps.Plan.Steps.Count == 1 && steps.Plan.Steps[0].Reservation.Units == 1,
                        "The repeated Fire is not one executable step for one use: " + steps.Refusal);
                }));
            }
            cases.Add(MbbCase("fire then cold", () =>
            {
                ResolvedCasting change = new ExplicitCastingCompiler().Compile(CastingDocument(
                        DirectCasting("fire", "long", "wizard", "wizard", fire.Canonical, fire),
                        DirectCasting("cold", "long", "wizard", "wizard", cold.Canonical, cold)),
                    party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], "long")
                    .CastingById("cold");
                MbbExpect(change.Readiness == ResolvedCastingReadiness.Blocked && change.ReadinessReasons.Any(reason =>
                        reason.StartsWith(ExplicitCastingCompiler.RemovesPlannedEffect, StringComparison.Ordinal)),
                    "Fire then Cold lost its removal conflict: " + change.Readiness + " " +
                    string.Join(",", change.ReadinessReasons));
            }));
            MbbSubcases(cases.ToArray());
        }

        // Review R9-F2: wizard b's Firebrand step; a fresh Firebrand from
        // wizard a, a fresh one of unread origin, and a fresh one from b on
        // the recipients. Only b's confirms b's delivery (group confirmation).
        private static void TestMbbFirebrandConfirmationCaster()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal) { { "a", 0f }, { "b", 1f }, { "ally", 2f } };
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } }, positions);
            AbilityKey key = MbbSpellKey(firebrand);
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("b-cast", "long", "b", key.Canonical, key, new[] { "b", "ally" })),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0]);
            CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
            ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, decision, party.Options, party.Effects);
            MbbExpect(steps.Converted && steps.Plan.Steps.Count == 1, "b's Firebrand is not one step: " + steps.Refusal);
            CastStep step = steps.Plan.Steps[0];
            Func<string, bool> confirms = origin =>
            {
                var before = positions.Keys.ToDictionary(unit => unit,
                    unit => (IEnumerable<ObservedEffectInstance>)new ObservedEffectInstance[0], StringComparer.Ordinal);
                return AppliedEffectJudgement.AnyReached(step.ExpectedRecipientUnitIds, step.ExpectedEffects,
                    new EffectBaseline(before), unit => new[]
                    {
                        new ObservedEffectInstance(EffectKind.Buff, firebrandBuff, unit + "-firebrand", 5000, false, null,
                            origin)
                    }, "b");
            };
            MbbSubcases(
                MbbCase("another caster's instance", () => MbbExpect(!confirms("a"),
                    "A fresh Firebrand from a confirmed b's delivery.")),
                MbbCase("unread origin", () => MbbExpect(!confirms(null),
                    "A fresh Firebrand of unread origin confirmed b's delivery.")),
                MbbCase("submitted caster's instance", () => MbbExpect(confirms("b"),
                    "b's own fresh Firebrand does not confirm b's delivery.")));
        }

        // Review R9-F3: unmodified Prayer; one in-radius party member whose
        // native relation read selects the enemy branch (IsEnemy true) and a
        // friendly ally. The hostile member is affected but no beneficiary;
        // observing only a fresh PrayerDebuff never confirms friendly delivery.
        private static void TestMbbPrayerHostileMember()
        {
            var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff);
            float radius = 40f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "ally", radius * 0.5f }, { "turned", radius * 0.25f }
            };
            MbbGroup group = MbbGroupCaster(dump, MbbPrayer, positions, id => id == "caster");
            ProviderSnapshot provider = group.Snapshot.Providers.Single();
            EffectExpression expression = group.Effects[group.Ability.Canonical];
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(MbbPrayer);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            var casterFacts = EffectConditionBinding.FactIds(expression)
                .ToDictionary(id => id, id => false, StringComparer.Ordinal);
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units, true, false,
                area, id => id == "caster", MbbDistance(positions), casterFacts,
                (unit, fact) => fact == MbbEnemyOfCaster ? unit == "turned"
                    : fact == MbbAllyOfCaster ? unit != "turned" : (bool?)false,
                fact => fact == MbbEnemyOfCaster || fact == MbbAllyOfCaster);
            ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast", casterFacts);
            var options = new List<ProviderPlanningOption> { option };
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("prayer", "long", "caster", group.Ability.Canonical, group.Ability,
                        new[] { "caster", "ally" })),
                group.Snapshot, options, group.Effects, new CastEnhancementSnapshot[0]);
            ResolvedCasting casting = plan.Castings.Single();
            MbbSubcases(
                MbbCase("prediction", () => MbbExpect(
                    casting.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                        .SequenceEqual(new[] { "ally", "caster" }) &&
                    option.AffectedTargetIdsForAnchor("caster").Contains("turned"),
                    "Prayer's hostile-branch member counted as a beneficiary (or was not affected): [" +
                    string.Join(",", casting.PredictedBeneficiaryUnitIds) + "]")),
                MbbCase("confirmation", () =>
                {
                    CastingApplyDecision gate = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
                    ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, gate, options, group.Effects);
                    MbbExpect(steps.Converted && steps.Plan.Steps.Count == 1, "Prayer is not one step: " + steps.Refusal);
                    EffectExpression expected = steps.Plan.Steps[0].ExpectedEffects;
                    Func<string, bool> confirms = effect =>
                    {
                        var before = positions.Keys.ToDictionary(unit => unit,
                            unit => (IEnumerable<ObservedEffectInstance>)new ObservedEffectInstance[0],
                            StringComparer.Ordinal);
                        return AppliedEffectJudgement.AnyReached(new[] { "ally" }, expected, new EffectBaseline(before),
                            unit => new[] { new ObservedEffectInstance(EffectKind.Buff, effect, unit + "-prayer", 5000, false) },
                            "caster");
                    };
                    MbbExpect(!confirms(MbbPrayerDebuff), "A fresh PrayerDebuff alone confirmed friendly delivery.");
                    MbbExpect(confirms(MbbPrayerBuff), "A fresh PrayerBuff no longer confirms friendly delivery.");
                }));
        }

        // Review R9-F4: Feather Step, Mass with its application on an empty
        // native checker's true branch and an unread recipient predicate (a
        // HasFact whose read is unavailable) solely on the never-run false
        // branch: the burst keeps its recipients and casts for one slot. With
        // that predicate gating the reachable application instead, coverage
        // still fails closed.
        private static void TestMbbEmptyCheckerUnreachablePredicate()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            const string unreadFact = "734a29b693e9ec346ba2951b27987e33";
            Func<bool, ResolvedCasting> compile = reachable =>
            {
                var dump = new NativeBlueprintDump(feather, featherBuff, MbbHasteBuff);
                BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
                var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
                Kingmaker.ElementsSystem.GameAction[] applications = run.Actions.Actions;
                var fact = NativeObject<Kingmaker.Blueprints.Classes.BlueprintFeature>();
                SetNative(fact, "m_AssetGuid", unreadFact);
                var has = NativeObject<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact>();
                SetNative(has, "Fact", fact);
                var inner = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
                SetNative(inner, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
                {
                    Conditions = new Kingmaker.ElementsSystem.Condition[] { has }
                });
                SetNative(inner, "IfTrue", new Kingmaker.ElementsSystem.ActionList
                {
                    Actions = reachable ? applications : new Kingmaker.ElementsSystem.GameAction[]
                    {
                        MbbApplicationCopy(spell, dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbHasteBuff))
                    }
                });
                SetNative(inner, "IfFalse", new Kingmaker.ElementsSystem.ActionList
                {
                    Actions = new Kingmaker.ElementsSystem.GameAction[0]
                });
                var gate = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
                SetNative(gate, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
                {
                    Conditions = new Kingmaker.ElementsSystem.Condition[0]
                });
                SetNative(gate, "IfTrue", new Kingmaker.ElementsSystem.ActionList
                {
                    Actions = reachable ? new Kingmaker.ElementsSystem.GameAction[] { inner } : applications
                });
                SetNative(gate, "IfFalse", new Kingmaker.ElementsSystem.ActionList
                {
                    Actions = reachable ? new Kingmaker.ElementsSystem.GameAction[0]
                        : new Kingmaker.ElementsSystem.GameAction[] { inner }
                });
                run.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { gate };
                MbbExpect(MbbDiscover(dump, feather).Included,
                    "The empty-checker Feather Step is not the intended admitted shape.");
                float radius = 15f * 0.3048f;
                var positions = new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { "caster", 0f }, { "ally", radius * 0.5f }
                };
                MbbGroup group = MbbGroupCaster(dump, feather, positions, id => true);
                ProviderSnapshot provider = group.Snapshot.Providers.Single();
                EffectExpression expression = group.Effects[group.Ability.Canonical];
                AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(spell, spell);
                var casterFacts = EffectConditionBinding.FactIds(expression)
                    .ToDictionary(id => id, id => false, StringComparer.Ordinal);
                List<UnitSnapshot> units = MbbUnits(positions.Keys);
                // The relation reads are friendly; every other fact read
                // (the HasFact) is unavailable.
                ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units, true,
                    false, area, id => true, MbbDistance(positions), casterFacts,
                    (unit, key) => key == MbbEnemyOfCaster ? false : key == MbbAllyOfCaster ? true : (bool?)null,
                    key => key == MbbEnemyOfCaster || key == MbbAllyOfCaster);
                ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                    CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast", casterFacts);
                return new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("step", "long", "caster", group.Ability.Canonical, group.Ability,
                            new[] { "caster", "ally" })),
                    group.Snapshot, new List<ProviderPlanningOption> { option }, group.Effects,
                    new CastEnhancementSnapshot[0]).Castings.Single();
            };
            MbbSubcases(
                MbbCase("unread predicate on the never-run branch", () =>
                {
                    ResolvedCasting casting = compile(false);
                    MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready && casting.Cost.Sum(c => c.Units) == 1 &&
                        casting.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                            .SequenceEqual(new[] { "ally", "caster" }),
                        "An unread predicate on the never-run branch suppressed coverage: " + casting.Readiness + " [" +
                        string.Join(",", casting.PredictedBeneficiaryUnitIds) + "] " +
                        string.Join(",", casting.ReadinessReasons));
                }),
                MbbCase("unread predicate on the reachable branch", () =>
                {
                    ResolvedCasting casting = compile(true);
                    MbbExpect(casting.Readiness == ResolvedCastingReadiness.Blocked && casting.Cost.Count == 0 &&
                        casting.PredictedBeneficiaryUnitIds.Count == 0,
                        "An unread predicate gating the reachable application was ignored: " + casting.Readiness);
                }));
        }

        // Review R9-F5: wizard b pinned for Firebrand over b and the ally.
        // An active instance from a beside a SUPPRESSED instance from b: one
        // funded Classic cast. A readable active instance from b: a free
        // skip, also through the first Classic sequential-forecast
        // occurrence (the live instance detail is carried).
        private static void TestMbbClassicFirebrandInstances()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal) { { "a", 0f }, { "b", 1f }, { "ally", 2f } };
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } }, positions);
            AbilityKey key = MbbSpellKey(firebrand);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-firebrand-instances");
            SourceAssignmentProfile assignment = Assignment(key.Canonical, key, new[] { "b", "ally" });
            assignment.CastingAssignments[0].CasterUnitId = "b";
            profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
            Func<Func<ActiveEffectInstance[]>, ActiveEffectSnapshot> live = instances => ActiveEffectSnapshot.FromInstances(
                new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "b", instances() }, { "ally", instances() }
                });
            ActiveEffectSnapshot suppressedOwn = live(() => new[]
            {
                new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, false, true, "a"),
                new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, true, true, "b")
            });
            ActiveEffectSnapshot effectiveOwn = live(() => new[]
            {
                new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, false, true, "b")
            });
            Func<ActiveEffectSnapshot, RoutinePlanResult> classic = effects => new RoutinePlanService().Plan(profile,
                "long", party.Snapshot, effects, party.Effects, party.Options, new CastEnhancementSnapshot[0]);
            MbbSubcases(
                MbbCase("a's active and b's suppressed instance", () =>
                {
                    RoutinePlanResult result = classic(suppressedOwn);
                    MbbExpect(result.Plan.Steps.Count == 1 && result.Plan.Steps[0].Provider.CasterUnitId == "b",
                        "A suppressed Firebrand from b skipped b's Classic cast: " + string.Join(",",
                            result.Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
                }),
                MbbCase("b's effective instance", () =>
                {
                    RoutinePlanResult result = classic(effectiveOwn);
                    MbbExpect(result.Plan.Steps.Count == 0 &&
                        result.Plan.Outcomes.All(o => o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "b's effective Firebrand did not skip b's Classic cast.");
                }),
                MbbCase("b's effective instance in the sequential forecast", () =>
                {
                    SequentialForecastPlanner.Result forecast = SequentialForecastPlanner.Compute(profile,
                        new[] { "long" }, party.Snapshot, effectiveOwn, party.Effects, party.Options,
                        new CastEnhancementSnapshot[0]);
                    MbbExpect(forecast.Occurrences[0].Plan.Steps.Count == 0,
                        "The sequential forecast dropped b's known Firebrand origin and charged a slot: " +
                        string.Join(",", forecast.Occurrences[0].Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
                }));
        }

        // Relation reads: "hostile" is not allied to the caster, "unread" has
        // no relation read, everyone else is a friend.
        private static bool? MbbMixedRelations(string unit, string fact)
        {
            if (fact == MbbAllyOfCaster) return unit == "hostile" ? false : unit == "unread" ? (bool?)null : true;
            if (fact == MbbEnemyOfCaster) return unit == "hostile" ? true : unit == "unread" ? (bool?)null : false;
            return null;
        }

        // Review R10-F1: unmodified Feather Step, Mass (native selector
        // TargetType.Ally), one legal centre, a friend, a party member hostile
        // to the caster and one with an unread relation inside its radius.
        // The hostile member is not reached; the unread one is affected but
        // not predicted; one reservation. Recomputed coverage keeps the
        // restriction. Prayer (Any selector) carries no such restriction.
        private static void TestMbbAllySelectorRelation()
        {
            string feather = MbbPointGroupSpells[1][1];
            var dump = new NativeBlueprintDump(feather, MbbPointGroupSpells[1][2]);
            float radius = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "friend", radius * 0.25f }, { "hostile", radius * 0.5f }, { "unread", radius * 0.75f }
            };
            MbbGroup group = MbbGroupCaster(dump, feather, positions, id => id == "caster");
            ProviderSnapshot provider = group.Snapshot.Providers.Single();
            EffectExpression expression = group.Effects[group.Ability.Canonical];
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(feather);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units, true, false,
                area, id => id == "caster", MbbDistance(positions), null, MbbMixedRelations,
                fact => fact == MbbEnemyOfCaster || fact == MbbAllyOfCaster);
            ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast");
            MbbSubcases(
                MbbCase("selector contract", () => MbbExpect(area != null && area.RequiresAlly,
                    "Feather Step, Mass's native Ally selector was not read.")),
                MbbCase("affected and predicted units", () =>
                {
                    ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                            GroupCasting("step", "long", "caster", group.Ability.Canonical, group.Ability,
                                new[] { "caster", "friend" })),
                        group.Snapshot, new List<ProviderPlanningOption> { option }, group.Effects,
                        new CastEnhancementSnapshot[0]);
                    ResolvedCasting casting = plan.Castings.Single();
                    MbbExpect(decision.AffectedIdsByAnchor["caster"].OrderBy(v => v, StringComparer.Ordinal)
                            .SequenceEqual(new[] { "caster", "friend", "unread" }) &&
                        casting.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                            .SequenceEqual(new[] { "caster", "friend" }) &&
                        casting.Readiness == ResolvedCastingReadiness.Ready && casting.Cost.Sum(c => c.Units) == 1,
                        "The Ally-only burst claimed or reached units its selector excludes: affected [" +
                        string.Join(",", decision.AffectedIdsByAnchor["caster"]) + "] predicted [" +
                        string.Join(",", casting.PredictedBeneficiaryUnitIds) + "] " + casting.Readiness);
                }),
                MbbCase("recomputed coverage", () =>
                {
                    var state = new SequentialRecipientState();
                    state.EnterRoutine("long");
                    state.Record(option, expression, new[] { "friend" }, "caster");
                    ProviderPlanningOption again = state.Reevaluate(option, expression);
                    MbbExpect(again.CoveredTargetIdsForAnchor("caster").OrderBy(v => v, StringComparer.Ordinal)
                            .SequenceEqual(new[] { "caster", "friend" }),
                        "Recomputed coverage lost the Ally restriction: [" +
                        string.Join(",", again.CoveredTargetIdsForAnchor("caster")) + "]");
                }),
                MbbCase("Any-selector control", () =>
                {
                    var prayerDump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff);
                    BlueprintAbility prayer = prayerDump.Get<BlueprintAbility>(MbbPrayer);
                    AlliedAreaCoverage prayerArea = new KingmakerAreaCoverageResolver().Resolve(prayer, prayer);
                    MbbExpect(prayerArea != null && !prayerArea.RequiresAlly,
                        "Prayer's Any selector was read as Ally-only.");
                }));
        }

        // Review R10-F2: Feather Step, Mass with the native
        // AbilityDeliverAttackWithWeapon component added (selector and actions
        // unchanged) is refused by discovery and provider admission; the
        // original stays admitted.
        private static void TestMbbWeaponDeliveryRefused()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            MbbSubcases(
                MbbCase("weapon delivery", () =>
                {
                    var dump = new NativeBlueprintDump(feather, featherBuff);
                    BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
                    SetNative(spell, "Components", spell.ComponentsArray.Concat(new Kingmaker.Blueprints.BlueprintComponent[]
                    {
                        NativeObject<Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverAttackWithWeapon>()
                    }).ToArray());
                    MbbDiscovery weapon = MbbDiscover(dump, feather);
                    var providers = new List<ProviderSnapshot>();
                    ProviderSnapshot provider = MbbAdmission(dump).AdmitSpell("caster", "book", false, "Book",
                        MbbRead(dump, feather, feather, SourceKind.Spellbook, 9), 3, 0, "pool", 1, new string[0],
                        providers);
                    MbbExpect((weapon.Facts.AbilityComponentTypes ?? new string[0]).Any(type =>
                            type.Contains("AbilityDeliverAttackWithWeapon")) &&
                        !weapon.Facts.CanTargetEnemies,
                        "The weapon-delivery Feather Step is not the intended friend-only shape.");
                    MbbExpect(!weapon.Included && provider == null && providers.Count == 0,
                        "A friend-only weapon delivery passed the point-target exception: " + weapon.Repaired.Reason);
                }),
                MbbCase("original control", () => MbbExpect(
                    MbbDiscover(new NativeBlueprintDump(feather, featherBuff), feather).Included,
                    "Unmodified Feather Step, Mass is no longer admitted.")));
        }

        private const string MbbClericSpellbook = "4673d19a0cf2fab4f885cc4d1353da33";

        // Review R10-F3 / R11 (R10-F3): Firebelly prepared in the native
        // cleric spellbook (a prepared book whose spell list carries Firebelly
        // at level 1) with one matching memorized slot, through the production
        // spellbook admission, provider targeting, castable catalogue and the
        // casting compiler: one caster-only entry, and its cast reserves that
        // slot. The breath the buff grants, passed through fact admission, is
        // refused by discovery itself (no beneficial trace, no effect entry),
        // and has no provider or catalogue entry.
        private static void TestMbbFirebellyCatalogue()
        {
            var dump = new NativeBlueprintDump(MbbFirebelly, MbbFirebellyBuff, MbbFirebellyBreath, MbbClericSpellbook);
            var book = dump.Get<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook>(MbbClericSpellbook);
            var clericList = dump.Get<BlueprintAbility>(MbbFirebelly).ComponentsArray
                .OfType<Kingmaker.Blueprints.Classes.Spells.SpellListComponent>()
                .SingleOrDefault(list => list.SpellList != null && book.SpellList != null &&
                    list.SpellList.AssetGuid == book.SpellList.AssetGuid);
            MbbExpect(!book.Spontaneous && clericList != null && clericList.SpellLevel == 1,
                "The native cleric spellbook is not a prepared book carrying Firebelly at level 1.");
            var positions = new Dictionary<string, float>(StringComparer.Ordinal) { { "cleric", 0f }, { "ally", 2f } };
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            AbilityKey bellyKey = MbbSpellKey(MbbFirebelly);
            const string token = "level-1|type-0|index-0";
            var pool = new ResourcePoolSnapshot(PartyProviderAdmission.PoolKey("cleric", MbbClericSpellbook, "prepared"),
                ResourcePoolKind.PreparedSlots, 1, 1, new[]
                {
                    new ResourceTokenSnapshot(token, bellyKey, 1, PreparedSlotKind.Common, true, true, new string[0])
                });
            var providers = new List<ProviderSnapshot>();
            var pools = new List<ResourcePoolSnapshot> { pool };
            ProviderSnapshot belly = admission.AdmitSpell("cleric", MbbClericSpellbook, true, "Cleric",
                MbbRead(dump, MbbFirebelly, "Firebelly", SourceKind.Spellbook, 5), 1, 0, pool.PoolKey, 1,
                new[] { token }, providers);
            ProviderSnapshot breath = admission.AdmitFact("cleric",
                MbbRead(dump, MbbFirebellyBreath, "Firebelly breath", SourceKind.AbilityResource, 5), false, null,
                "fixture-breath-resource", string.Empty, providers, pools, new HashSet<string>(StringComparer.Ordinal));
            var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
            List<ProviderPlanningOption> options = providers.Select(provider => MbbOption(dump, provider,
                effects[provider.Key.Ability.Canonical], units, id => true, positions)).ToList();
            var snapshot = new PartyProviderSnapshot(units, providers, pools);
            var catalogue = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("mbb-firebelly"), snapshot,
                new ActiveEffectSnapshot(null), effects, options, ignored => { });
            MbbSubcases(
                MbbCase("Firebelly entry", () =>
                {
                    MbbExpect(belly != null && catalogue.Sources.Any(source =>
                            source.Providers.Any(p => p.Key.Ability.BaseAbilityGuid == MbbFirebelly)),
                        "Firebelly has no castable catalogue entry: " +
                        string.Join(",", admission.Traces.Select(t => t.BlueprintGuid + "=" + t.Reason)));
                    ProviderPlanningOption option = options.Single(o => o.Provider.Key.Ability.BaseAbilityGuid == MbbFirebelly);
                    MbbExpect(option.ReachableTargetIds.SequenceEqual(new[] { "cleric" }) &&
                        option.LegalAnchorIds.SequenceEqual(new[] { "cleric" }),
                        "Firebelly's option is not caster-only: [" + string.Join(",", option.ReachableTargetIds) + "]");
                }),
                MbbCase("prepared slot reserved", () =>
                {
                    ResolvedCasting cast = new ExplicitCastingCompiler().Compile(CastingDocument(
                            DirectCasting("belly", "long", "cleric", "cleric", belly.Key.Ability.Canonical,
                                belly.Key.Ability, null, MbbClericSpellbook)),
                        snapshot, options, effects, new CastEnhancementSnapshot[0]).CastingById("belly");
                    MbbExpect(cast.Readiness == ResolvedCastingReadiness.Ready && cast.Cost.Count == 1 &&
                        cast.Cost[0].PoolKey == pool.PoolKey && cast.Cost[0].TokenIds.SequenceEqual(new[] { token }),
                        "Prepared Firebelly does not reserve its memorized slot: " + cast.Readiness + " " +
                        string.Join(",", cast.ReadinessReasons) + " " +
                        string.Join(",", cast.Cost.Select(c => c.PoolKey + "[" + string.Join(";", c.TokenIds) + "]")));
                }),
                MbbCase("breath refused by discovery", () =>
                {
                    List<PartySourceDiscoveryTrace> traces = admission.Traces
                        .Where(t => t.BlueprintGuid == MbbFirebellyBreath).ToList();
                    MbbExpect(breath == null && traces.Count == 1 && !traces[0].Beneficial &&
                        traces[0].Reason != "fixture-breath-resource" &&
                        !admission.EffectsBySource.Keys.Any(k => k.Contains(MbbFirebellyBreath)),
                        "Firebelly's breath was not refused by discovery itself: " +
                        string.Join(" | ", traces.Select(t => t.Beneficial + ":" + t.Reason)));
                    MbbExpect(!providers.Any(p => p.Key.Ability.BaseAbilityGuid == MbbFirebellyBreath) &&
                        !catalogue.Sources.Any(source =>
                            source.Providers.Any(p => p.Key.Ability.BaseAbilityGuid == MbbFirebellyBreath)),
                        "Firebelly's granted breath reached the providers or the catalogue.");
                }));
        }

        // Review R10-F4: a saved skip-if-active Good Hope (Classic, pinned
        // bard) whose only matching live instance is suppressed, or has an
        // unread suppression flag, claims no existing coverage; a readable,
        // effective Good Hope still skips.
        private static void TestMbbClassicSuppressedGoodHope()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            MbbParty party = MbbBardParty(dump, MbbGoodHope);
            AbilityKey hope = party.Abilities[MbbGoodHope];
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-suppressed-good-hope");
            SourceAssignmentProfile assignment = Assignment(hope.Canonical, hope, new[] { "ally-near" });
            assignment.CastingAssignments[0].CasterUnitId = "bard";
            profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
            Func<bool, bool, RoutinePlanResult> plan = (suppressed, readable) => new RoutinePlanService().Plan(profile,
                "long", party.Snapshot, ActiveEffectSnapshot.FromInstances(
                    new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                    {
                        { "ally-near", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbGoodHopeBuff, 5000, 20, 0,
                            suppressed, readable) } }
                    }), party.Effects, party.Options, new CastEnhancementSnapshot[0]);
            Func<RoutinePlanResult, bool> skipped = result => result.Plan.Outcomes.Any(o => o.UnitId == "ally-near" &&
                o.Kind == TargetOutcomeKind.SkippedAlreadyActive);
            MbbSubcases(
                MbbCase("suppressed instance", () => MbbExpect(!skipped(plan(true, true)),
                    "A suppressed Good Hope was treated as active coverage.")),
                MbbCase("unread suppression", () => MbbExpect(!skipped(plan(false, false)),
                    "A Good Hope with unread suppression was treated as active coverage.")),
                MbbCase("effective control", () =>
                {
                    RoutinePlanResult effective = plan(false, true);
                    MbbExpect(skipped(effective) && effective.Plan.Steps.Count == 0,
                        "An effective Good Hope no longer skips.");
                }));
        }

        // Review R10 (R8-F1): a selected run starting with sufficient live
        // Firebrand from wizard a; b's funded Firebrand over the same disk,
        // then a's skip-if-active casting: a pays one slot and has an
        // executable step. b disabled, or a disjoint disk: a's free skip.
        private static void TestMbbFirebrandLiveOverlap()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            AbilityKey key = MbbSpellKey(firebrand);
            Func<Dictionary<string, float>, string[], CastingAuthoringState, ExplicitCastingPlan> compile =
                (positions, bMembers, bState) =>
                {
                    MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                        new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                        positions);
                    // a's Firebrand is active on everyone at the start.
                    var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                    {
                        { "a", new[] { new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, false, true, "a") } },
                        { "b", new[] { new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, false, true, "a") } },
                        { "ally", new[] { new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, false, true, "a") } }
                    });
                    return new ExplicitCastingCompiler().Compile(CastingDocument(
                            GroupCasting("b-cast", "long", "b", key.Canonical, key, bMembers, null,
                                CastingTargetMode.CasterCenteredOrigin, null, bState),
                            GroupCasting("a-cast", "long", "a", key.Canonical, key, new[] { "a", "ally" })),
                        party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], "long", null, false,
                        live);
                };
            var overlap = new Dictionary<string, float>(StringComparer.Ordinal) { { "a", 0f }, { "b", 1f }, { "ally", 2f } };
            var disjoint = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "a", 0f }, { "ally", 1f }, { "b", 100f }, { "ally2", 101f }
            };
            MbbSubcases(
                MbbCase("b recasts over a's disk", () =>
                {
                    ExplicitCastingPlan plan = compile(overlap, new[] { "b", "ally" }, CastingAuthoringState.Ready);
                    ResolvedCasting a = plan.CastingById("a-cast");
                    MbbExpect(plan.CastingById("b-cast").Readiness == ResolvedCastingReadiness.Ready &&
                        a.Readiness == ResolvedCastingReadiness.Ready && a.Cost.Sum(c => c.Units) == 1,
                        "a's live Firebrand skip survived b's overlapping cast: " + a.Readiness + " " +
                        string.Join(",", a.ReadinessReasons));
                    CastingApplyDecision gate = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
                    MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                        new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                        overlap);
                    ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, gate, party.Options,
                        party.Effects);
                    MbbExpect(steps.Converted && steps.Plan.Steps.Count == 2 &&
                        steps.Plan.Steps.Any(s => s.Provider.CasterUnitId == "a"),
                        "a's Firebrand has no executable step: " + steps.Refusal);
                }),
                MbbCase("b disabled", () =>
                {
                    ResolvedCasting a = compile(overlap, new[] { "b", "ally" }, CastingAuthoringState.Disabled)
                        .CastingById("a-cast");
                    MbbExpect(a.Readiness == ResolvedCastingReadiness.AlreadySatisfied && a.Cost.Count == 0,
                        "Without b's cast a's live Firebrand no longer skips: " + a.Readiness);
                }),
                MbbCase("disjoint disk", () =>
                {
                    ResolvedCasting a = compile(disjoint, new[] { "b", "ally2" }, CastingAuthoringState.Ready)
                        .CastingById("a-cast");
                    MbbExpect(a.Readiness == ResolvedCastingReadiness.AlreadySatisfied && a.Cost.Count == 0,
                        "b's disjoint cast cost a's live skip: " + a.Readiness);
                }));
        }

        // Review R10 (R8-F2): the caster-directed Resistance - Fire with the
        // native ContextActionKillTarget, now with EffectOnEnemy = None and
        // CanTargetFriends = false (rescued by Personal range from the
        // no-friend hostile rule): refused by the frozen baseline and by
        // discovery and fact-provider admission. Without the kill action it
        // stays admitted.
        private static void TestMbbPersonalNeutralRescueWhole()
        {
            string fire = MbbAbjuration[3][1], fireBuff = MbbAbjuration[3][2];
            string[] all = MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray();
            Func<bool, KeyValuePair<MbbDiscovery, int>> run = kill =>
            {
                var dump = new NativeBlueprintDump(all);
                BlueprintAbility ability = dump.Get<BlueprintAbility>(fire);
                MbbExpect(MbbReplaceApplications(ability, apply => SetNative(apply, "ToCaster", true)) == 1,
                    "Resistance - Fire's application could not be redirected.");
                SetNative(ability, "EffectOnEnemy", AbilityEffectOnUnit.None);
                SetNative(ability, "CanTargetFriends", false);
                if (kill)
                {
                    var effect = ability.ComponentsArray
                        .OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
                    effect.Actions.Actions = effect.Actions.Actions.Concat(new Kingmaker.ElementsSystem.GameAction[]
                    {
                        NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionKillTarget>()
                    }).ToArray();
                }
                MbbDiscovery result = MbbDiscover(dump, fire);
                var logic = ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic>()
                    .Single();
                KingmakerFactResource resource;
                string refusal;
                bool known = KingmakerFactResources.TryPriceAbilityResource("wizard", logic.RequiredResource.AssetGuid,
                    logic.Amount, 1, () => 1, 1, false, string.Empty, string.Empty, out resource, out refusal);
                var providers = new List<ProviderSnapshot>();
                MbbAdmission(dump).AdmitFact("wizard", MbbRead(dump, fire, "Resistance - Fire", SourceKind.AbilityResource, 7),
                    known, resource, refusal, string.Empty, providers, new List<ResourcePoolSnapshot>(),
                    new HashSet<string>(StringComparer.Ordinal));
                MbbExpect(result.Facts.EffectOnEnemy == "None" && !result.Facts.CanTargetFriends &&
                    result.Facts.CanTargetEnemies && result.Facts.Effects.Any(e => e.EffectId == fireBuff && e.Target == "Caster"),
                    "The neutral-disposition selection is not the intended shape.");
                return new KeyValuePair<MbbDiscovery, int>(result, providers.Count);
            };
            MbbSubcases(
                MbbCase("neutral disposition with kill", () =>
                {
                    KeyValuePair<MbbDiscovery, int> killing = run(true);
                    MbbExpect(killing.Key.Before.Disposition == "exclude",
                        "The frozen baseline admitted the kill variant: " + killing.Key.Before.Reason);
                    MbbExpect(!killing.Key.Included && killing.Value == 0,
                        "A neutral-disposition Personal selection with a kill action was admitted: " +
                        killing.Key.Repaired.Reason);
                }),
                MbbCase("neutral disposition control", () =>
                {
                    KeyValuePair<MbbDiscovery, int> plain = run(false);
                    MbbExpect(plain.Key.Included && plain.Value == 1,
                        "A harmless neutral-disposition Personal selection is no longer admitted: " +
                        plain.Key.Repaired.Reason);
                }));
        }

        // Review R10 (R8-F3): two Classic routines (sequential forecast).
        // The first Good Hope clears the 10- and 20-foot allies' Despair; the
        // second routine first funds an unrelated Haste, then Good Hope on
        // those allies: they stay covered and the Good Hope reserves one slot.
        private static void TestMbbClassicInheritedCoverage()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff);
            string despair = "buff-predicate:" + MbbCrushingDespairBuff;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 30f }, { "a", 0f }, { "m", 10f * 0.3048f }, { "f", 20f * 0.3048f }
            };
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool("bard", MbbBardBook, 3, 3, 3);
            var providers = new List<ProviderSnapshot>();
            admission.AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(dump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, pool.PoolKey, 1,
                new string[0], providers);
            admission.AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(dump, MbbHaste, "Haste", SourceKind.Spellbook, 7), 3, 0, pool.PoolKey, 1,
                new string[0], providers);
            var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
            List<ProviderPlanningOption> options = providers.Select(provider => MbbOption(dump, provider,
                effects[provider.Key.Ability.Canonical], units,
                provider.Key.Ability.BaseAbilityGuid == MbbGoodHope ? (Func<string, bool>)(id => id == "m" || id == "f")
                    : id => true, positions,
                (unit, fact) => fact == despair && (unit == "m" || unit == "f"))).ToList();
            var snapshot = new PartyProviderSnapshot(units, providers, new[] { pool });
            AbilityKey hope = providers.Single(p => p.Key.Ability.BaseAbilityGuid == MbbGoodHope).Key.Ability;
            AbilityKey haste = providers.Single(p => p.Key.Ability.BaseAbilityGuid == MbbHaste).Key.Ability;
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-inherited-coverage");
            profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(Assignment(
                hope.Canonical, hope, new[] { "a" }));
            SourceAssignmentProfile unrelated = Assignment(haste.Canonical, haste, new[] { "a" });
            unrelated.CastingAssignments[0].Order = 0;
            SourceAssignmentProfile later = Assignment(hope.Canonical, hope, new[] { "m", "f" });
            later.CastingAssignments[0].Order = 1;
            var shortRoutine = profile.Routines.First(rt => rt.RoutineId == "short");
            shortRoutine.Assignments.Add(unrelated);
            shortRoutine.Assignments.Add(later);
            SequentialForecastPlanner.Result classic = SequentialForecastPlanner.Compute(profile,
                new[] { "long", "short" }, snapshot, new ActiveEffectSnapshot(null), effects, options,
                new CastEnhancementSnapshot[0]);
            CastPlan second = classic.Occurrences[1].Plan;
            CastStep hopeStep = second.Steps.SingleOrDefault(s => s.Provider.Ability.Equals(hope));
            MbbExpect(classic.Occurrences[0].Plan.Steps.Count == 1 &&
                second.Steps.Any(s => s.Provider.Ability.Equals(haste)) && hopeStep != null &&
                hopeStep.Reservation.Units == 1 && hopeStep.ExpectedRecipientUnitIds.Contains("m") &&
                hopeStep.ExpectedRecipientUnitIds.Contains("f"),
                "The later Classic Good Hope lost the allies whose Despair the first routine cleared: " +
                string.Join(",", second.Outcomes.Select(o => o.AssignmentId + ":" + o.UnitId + "=" + o.Kind)));
        }

        // Review R10 (R9-F1): two saved Classic Resistance - Fire assignments
        // pinned to the wizard, one daily use, no opening resistance: one step,
        // one reservation, the second SkippedAlreadyActive. Fire then Cold is
        // refused as a removal of the planned Fire.
        private static void TestMbbClassicResistanceRepeat()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbParty party = MbbWizardParty(dump, 1);
            AbilityKey fire = party.Abilities["Fire"], cold = party.Abilities["Cold"];
            Func<AbilityKey, RoutinePlanResult> plan = second =>
            {
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-resistance-repeat");
                SourceAssignmentProfile first = Assignment(fire.Canonical, fire, new[] { "wizard" });
                first.CastingAssignments[0].CasterUnitId = "wizard";
                var routine = profile.Routines.First(rt => rt.RoutineId == "long");
                routine.Assignments.Add(first);
                if (second.Equals(fire))
                    first.CastingAssignments.Add(new CastingAssignmentProfile
                    {
                        AssignmentId = "z-fire-again",
                        Order = 1,
                        CasterUnitId = "wizard",
                        TargetUnitIds = new List<string> { "wizard" },
                        Enhancements = new List<EnhancementSelectionProfile>()
                    });
                else
                {
                    SourceAssignmentProfile other = Assignment(second.Canonical, second, new[] { "wizard" });
                    other.CastingAssignments[0].CasterUnitId = "wizard";
                    other.CastingAssignments[0].Order = 1;
                    routine.Assignments.Add(other);
                }
                return new RoutinePlanService().Plan(profile, "long", party.Snapshot, new ActiveEffectSnapshot(null),
                    party.Effects, party.Options, new CastEnhancementSnapshot[0]);
            };
            Func<RoutinePlanResult, string> describe = result => string.Join(",", result.Plan.Outcomes
                .Select(o => o.AssignmentId + ":" + o.UnitId + "=" + o.Kind + ":" + o.Reason));
            MbbSubcases(
                MbbCase("fire twice", () =>
                {
                    RoutinePlanResult repeat = plan(fire);
                    MbbExpect(repeat.Plan.Steps.Count == 1 && repeat.Plan.Steps[0].Reservation.Units == 1 &&
                        repeat.Plan.Outcomes.Any(o => o.AssignmentId == "z-fire-again" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "A repeated Classic Fire selection was charged again: " + describe(repeat));
                }),
                MbbCase("fire then cold", () =>
                {
                    RoutinePlanResult change = plan(cold);
                    MbbExpect(change.Plan.Steps.Count == 1 && change.Plan.Outcomes.Any(o =>
                            o.Kind == TargetOutcomeKind.Unfulfilled &&
                            o.Reason.Contains(ExplicitCastingCompiler.RemovesPlannedEffect)),
                        "Fire then Cold lost its removal conflict: " + describe(change));
                }));
        }

        // Review R10 (R9-F3): a Classic Prayer step built by RoutinePlanService
        // from the committed fixture: a fresh PrayerDebuff alone does not
        // confirm delivery on a predicted friendly recipient; PrayerBuff does.
        private static void TestMbbClassicPrayerConfirmation()
        {
            var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff);
            float radius = 40f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "ally", radius * 0.5f }
            };
            MbbGroup group = MbbGroupCaster(dump, MbbPrayer, positions, id => id == "caster");
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-prayer-confirmation");
            SourceAssignmentProfile assignment = Assignment(group.Ability.Canonical, group.Ability,
                new[] { "caster", "ally" });
            assignment.CastingAssignments[0].CasterUnitId = "caster";
            profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
            RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long", group.Snapshot,
                new ActiveEffectSnapshot(null), group.Effects, group.Options, new CastEnhancementSnapshot[0]);
            MbbExpect(result.Plan.Steps.Count == 1 && result.Plan.Steps[0].ExpectedRecipientUnitIds.Contains("ally"),
                "Classic Prayer is not one step over the ally.");
            CastStep step = result.Plan.Steps[0];
            Func<string, bool> confirms = effect =>
            {
                var before = positions.Keys.ToDictionary(unit => unit,
                    unit => (IEnumerable<ObservedEffectInstance>)new ObservedEffectInstance[0], StringComparer.Ordinal);
                return AppliedEffectJudgement.AnyReached(new[] { "ally" }, step.ExpectedEffects, new EffectBaseline(before),
                    unit => new[] { new ObservedEffectInstance(EffectKind.Buff, effect, unit + "-prayer", 5000, false) },
                    "caster");
            };
            MbbSubcases(
                MbbCase("penalty only", () => MbbExpect(!confirms(MbbPrayerDebuff),
                    "A fresh PrayerDebuff alone confirmed Classic Prayer's friendly delivery.")),
                MbbCase("friendly buff", () => MbbExpect(confirms(MbbPrayerBuff),
                    "A fresh PrayerBuff no longer confirms Classic Prayer's delivery.")));
        }

        // Review R10 (R9-F4): Prayer (Any selector) rebuilt with an empty outer
        // checker whose true branch applies PrayerBuff unconditionally and
        // whose never-run false branch holds Prayer's original IsEnemy
        // actions. A member read as hostile is still predicted (the true
        // branch reaches everyone); a Classic target outside every disk, with
        // no relation read and an effective PrayerBuff, is skipped.
        private static void TestMbbEmptyCheckerUnreachableRelation()
        {
            var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff);
            BlueprintAbility spell = dump.Get<BlueprintAbility>(MbbPrayer);
            var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            var unconditional = MbbApplicationCopy(spell,
                dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbPrayerBuff));
            var gate = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
            SetNative(gate, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
            {
                Conditions = new Kingmaker.ElementsSystem.Condition[0]
            });
            SetNative(gate, "IfTrue", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[] { unconditional }
            });
            SetNative(gate, "IfFalse", new Kingmaker.ElementsSystem.ActionList { Actions = run.Actions.Actions });
            run.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { gate };
            float radius = 40f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "hostile", radius * 0.5f }, { "far", radius * 5f }
            };
            MbbGroup group = MbbGroupCaster(dump, MbbPrayer, positions, id => id == "caster");
            MbbExpect(EffectRecipientFactBinding.FactIds(group.Effects[group.Ability.Canonical])
                    .Contains(MbbEnemyOfCaster),
                "The rebuilt Prayer no longer mentions the relation on its false branch.");
            MbbSubcases(
                MbbCase("hostile read on the never-run branch", () =>
                {
                    ProviderSnapshot provider = group.Snapshot.Providers.Single();
                    EffectExpression expression = group.Effects[group.Ability.Canonical];
                    AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(spell, spell);
                    List<UnitSnapshot> units = MbbUnits(positions.Keys);
                    ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units,
                        true, false, area, id => id == "caster", MbbDistance(positions), null, MbbMixedRelations,
                        fact => fact == MbbEnemyOfCaster || fact == MbbAllyOfCaster);
                    MbbExpect(decision.RecipientIdsByAnchor["caster"].OrderBy(v => v, StringComparer.Ordinal)
                            .SequenceEqual(new[] { "caster", "hostile" }),
                        "An unreachable relation predicate vetoed the unconditional benefit: [" +
                        string.Join(",", decision.RecipientIdsByAnchor["caster"]) + "]");
                }),
                MbbCase("Classic skip with an unread relation", () =>
                {
                    BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-empty-checker-relation");
                    SourceAssignmentProfile assignment = Assignment(group.Ability.Canonical, group.Ability,
                        new[] { "far" });
                    assignment.CastingAssignments[0].CasterUnitId = "caster";
                    profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
                    var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                    {
                        { "far", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerBuff, 5000, 20, 0) } }
                    });
                    RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long", group.Snapshot, live,
                        group.Effects, group.Options, new CastEnhancementSnapshot[0]);
                    MbbExpect(result.Plan.Outcomes.Any(o => o.UnitId == "far" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "An unreachable relation predicate refused a valid Classic skip: " +
                        string.Join(",", result.Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
                }));
        }

        // Review R10 (R9-F5): the Classic sequence forecast starts with
        // effective Firebrand instances from wizard a; b's Firebrand is funded
        // in the first routine, a's is requested in the second. Overlapping
        // disks: a pays a slot; a disjoint disk: a's free skip.
        private static void TestMbbClassicSequenceFirebrand()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            AbilityKey key = MbbSpellKey(firebrand);
            Func<Dictionary<string, float>, string[], SequentialForecastPlanner.Result> forecast = (positions, bTargets) =>
            {
                MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                    new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                    positions);
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-sequence-firebrand");
                SourceAssignmentProfile byB = Assignment(key.Canonical, key, bTargets);
                byB.CastingAssignments[0].CasterUnitId = "b";
                SourceAssignmentProfile byA = Assignment(key.Canonical, key, new[] { "a", "ally" });
                byA.CastingAssignments[0].CasterUnitId = "a";
                profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(byB);
                profile.Routines.First(rt => rt.RoutineId == "short").Assignments.Add(byA);
                var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "a", new[] { new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, false, true, "a") } },
                    { "ally", new[] { new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, false, true, "a") } }
                });
                return SequentialForecastPlanner.Compute(profile, new[] { "long", "short" }, party.Snapshot, live,
                    party.Effects, party.Options, new CastEnhancementSnapshot[0]);
            };
            MbbSubcases(
                MbbCase("overlapping disks", () =>
                {
                    SequentialForecastPlanner.Result result = forecast(
                        new Dictionary<string, float>(StringComparer.Ordinal) { { "a", 0f }, { "b", 1f }, { "ally", 2f } },
                        new[] { "b", "ally" });
                    MbbExpect(result.Occurrences[0].Plan.Steps.Count == 1 &&
                        result.Occurrences[1].Plan.Steps.Count == 1 &&
                        result.Occurrences[1].Plan.Steps[0].Provider.CasterUnitId == "a",
                        "The Classic forecast skipped a's Firebrand on instances b's cast may have replaced: " +
                        string.Join(",", result.Occurrences[1].Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
                }),
                MbbCase("disjoint disk", () =>
                {
                    SequentialForecastPlanner.Result result = forecast(new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "a", 0f }, { "ally", 1f }, { "b", 100f }, { "ally2", 101f }
                    }, new[] { "b", "ally2" });
                    MbbExpect(result.Occurrences[1].Plan.Steps.Count == 0,
                        "b's disjoint cast cost a's free Classic skip: " +
                        string.Join(",", result.Occurrences[1].Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
                }));
        }

        // A provider's option from the production targeting planner with
        // injected relation/fact reads (fixed: relations and recorded non-buff
        // facts, as the builder decides) and an optional coverage override.
        private static ProviderPlanningOption MbbCustomOption(NativeBlueprintDump dump, ProviderSnapshot provider,
            EffectExpression expression, List<UnitSnapshot> units, Func<string, bool> canTarget,
            IDictionary<string, float> positions, Func<string, string, bool?> reads,
            Func<string, bool> isFixed = null)
        {
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(provider.Key.Ability.BaseAbilityGuid);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            var casterFacts = EffectConditionBinding.FactIds(expression)
                .ToDictionary(id => id, id => false, StringComparer.Ordinal);
            ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units, true,
                false, area, canTarget, MbbDistance(positions), casterFacts, reads, isFixed ?? (fact =>
                {
                    if (fact == MbbEnemyOfCaster || fact == MbbAllyOfCaster) return true;
                    string type = NativeBlueprintDump.RecordedType(fact);
                    return type != null && !type.StartsWith("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff",
                        StringComparison.Ordinal);
                }));
            return ProviderTargetingPlanner.BuildOption(provider, decision, CastExecutionStrategy.DirectRuleCast,
                "ordinary-direct-rule-cast", casterFacts);
        }

        // Review R11 (R10-F1): (a) two bards' Good Hope; the member "m" under
        // Despair has an unread relation to bard A and is allied to bard B.
        // A's funded cast on another ally may reach m but cannot establish
        // that it cleared m's Despair, so B's later Good Hope does not promise
        // m. (b) An execution-strategy copy of an Ally-only option (an
        // enhancement requiring the native command) keeps the restriction.
        private static void TestMbbUnreadAllyNeverDefinite()
        {
            MbbSubcases(
                MbbCase("state after a possibly reaching cast", () =>
                {
                    var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
                    string despair = "buff-predicate:" + MbbCrushingDespairBuff;
                    var positions = new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "bardA", 30f }, { "bardB", 60f }, { "k", 0f }, { "m", 3f }
                    };
                    List<UnitSnapshot> units = MbbUnits(positions.Keys);
                    PartyProviderAdmission admission = MbbAdmission(dump);
                    var providers = new List<ProviderSnapshot>();
                    var pools = new List<ResourcePoolSnapshot>();
                    foreach (string bard in new[] { "bardA", "bardB" })
                    {
                        ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool(bard, MbbBardBook, 3, 2, 2);
                        pools.Add(pool);
                        admission.AdmitSpell(bard, MbbBardBook, false, "Bard",
                            MbbRead(dump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, pool.PoolKey, 1,
                            new string[0], providers);
                    }
                    var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
                    Func<string, Func<string, string, bool?>> readsFor = caster => (unit, fact) =>
                    {
                        bool unread = caster == "bardA" && unit == "m";
                        if (fact == MbbAllyOfCaster) return unread ? (bool?)null : true;
                        if (fact == MbbEnemyOfCaster) return unread ? (bool?)null : false;
                        if (fact == despair) return unit == "m";
                        return false;
                    };
                    List<ProviderPlanningOption> options = providers.Select(provider => MbbCustomOption(dump, provider,
                        effects[provider.Key.Ability.Canonical], units, id => id == "k" || id == "m", positions,
                        readsFor(provider.Key.CasterUnitId))).ToList();
                    var snapshot = new PartyProviderSnapshot(units, providers, pools);
                    AbilityKey key = providers[0].Key.Ability;
                    ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                            GroupCasting("a-hope", "long", "bardA", key.Canonical, key, new[] { "k" }, null,
                                CastingTargetMode.AnchoredOrigin, "k"),
                            GroupCasting("b-hope", "long", "bardB", key.Canonical, key, new[] { "k" }, null,
                                CastingTargetMode.AnchoredOrigin, "m")),
                        snapshot, options, effects, new CastEnhancementSnapshot[0]);
                    ResolvedCasting a = plan.CastingById("a-hope"), b = plan.CastingById("b-hope");
                    MbbExpect(a.Readiness == ResolvedCastingReadiness.Ready &&
                        options.Single(o => o.Provider.Key.CasterUnitId == "bardA").AffectedTargetIdsForAnchor("k")
                            .Contains("m"),
                        "A's Good Hope is not the intended possibly-reaching cast.");
                    MbbExpect(!b.PredictedBeneficiaryUnitIds.Contains("m"),
                        "B promised Good Hope to a member whose Despair A only possibly cleared: [" +
                        string.Join(",", b.PredictedBeneficiaryUnitIds) + "]");
                }),
                MbbCase("strategy copy", () =>
                {
                    string feather = MbbPointGroupSpells[1][1];
                    var dump = new NativeBlueprintDump(feather, MbbPointGroupSpells[1][2]);
                    float radius = 15f * 0.3048f;
                    var positions = new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "caster", 0f }, { "friend", radius * 0.25f }, { "unread", radius * 0.75f }
                    };
                    MbbGroup group = MbbGroupCaster(dump, feather, positions, id => id == "caster");
                    EffectExpression expression = group.Effects[group.Ability.Canonical];
                    ProviderPlanningOption option = MbbCustomOption(dump, group.Snapshot.Providers.Single(), expression,
                        MbbUnits(positions.Keys), id => id == "caster", positions, MbbMixedRelations);
                    var rod = new CastEnhancementSnapshot("rod-native", "caster", "rod-blueprint", "Rod", string.Empty,
                        CastEnhancementCategory.MetamagicRod, 1, 9, 3, new string[0], null, null, "rod-pool", true);
                    ProviderPlanningOption copy = CastEnhancementExecutionPolicy.Apply(new[] { rod }, option);
                    var state = new SequentialRecipientState();
                    state.EnterRoutine("long");
                    state.Record(copy, expression, new[] { "friend" }, "caster");
                    ProviderPlanningOption again = state.Reevaluate(copy, expression);
                    MbbExpect(copy.ExecutionStrategy == CastExecutionStrategy.NativeCommandRequired &&
                        copy.RequiresAllyRelation && again.CoveredTargetIdsForAnchor("caster")
                            .OrderBy(v => v, StringComparer.Ordinal).SequenceEqual(new[] { "caster", "friend" }),
                        "A strategy-changing copy lost the Ally restriction: [" +
                        string.Join(",", again.CoveredTargetIdsForAnchor("caster")) + "]");
                }));
        }

        // Review R11 (R8-F1): (a) b casts Firebrand on a remote ally anchor
        // whose disk excludes b; a's later caster-centred disk includes b, and
        // a's existing Firebrand on b keeps a's free skip. (b) a's row starts
        // Ready with one recipient protected by a and one missing; b's earlier
        // cast overlaps the protected one, and a's converted step no longer
        // credits it as pre-covered.
        private static void TestMbbFirebrandLandingUnits()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            AbilityKey key = MbbSpellKey(firebrand);
            Func<string, ActiveEffectInstance> fromA = unit =>
                new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, false, true, "a");
            MbbSubcases(
                MbbCase("remote anchor keeps the caster's evidence", () =>
                {
                    var positions = new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "a", 0f }, { "b", 1f }, { "x", 50f }
                    };
                    MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                        new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                        positions);
                    var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                    {
                        { "a", new[] { fromA("a") } }, { "b", new[] { fromA("b") } }
                    });
                    ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                            GroupCasting("b-cast", "long", "b", key.Canonical, key, new[] { "x" }, null,
                                CastingTargetMode.AnchoredOrigin, "x"),
                            GroupCasting("a-cast", "long", "a", key.Canonical, key, new[] { "a", "b" })),
                        party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false,
                        live);
                    ResolvedCasting b = plan.CastingById("b-cast"), a = plan.CastingById("a-cast");
                    MbbExpect(b.Readiness == ResolvedCastingReadiness.Ready && !b.PredictedBeneficiaryUnitIds.Contains("b"),
                        "b's remote cast is not the intended shape: " + b.Readiness);
                    MbbExpect(a.Readiness == ResolvedCastingReadiness.AlreadySatisfied && a.Cost.Count == 0,
                        "b's remote cast voided a's protection on b outside its disk: " + a.Readiness + " " +
                        string.Join(",", a.ReadinessReasons));
                }),
                MbbCase("partial row drops stale credit", () =>
                {
                    var positions = new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "a", 0f }, { "ally1", 8f }, { "ally2", -8f }, { "b", 30f }, { "x", 16f }
                    };
                    MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                        new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                        positions);
                    var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                    {
                        { "a", new[] { fromA("a") } }, { "ally1", new[] { fromA("ally1") } }
                    });
                    ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                            GroupCasting("b-cast", "long", "b", key.Canonical, key, new[] { "ally1" }, null,
                                CastingTargetMode.AnchoredOrigin, "x"),
                            GroupCasting("a-cast", "long", "a", key.Canonical, key, new[] { "a", "ally1", "ally2" })),
                        party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false,
                        live);
                    CastingApplyDecision gate = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
                    ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, gate, party.Options,
                        party.Effects);
                    CastStep a = steps.Converted ? steps.Plan.Steps.SingleOrDefault(s => s.Provider.CasterUnitId == "a")
                        : null;
                    MbbExpect(plan.CastingById("b-cast").PredictedBeneficiaryUnitIds.Contains("ally1") && a != null,
                        "The partial-coverage plan is not the intended shape: " + steps.Refusal);
                    MbbExpect(!a.PreCoveredRecipientUnitIds.Contains("ally1"),
                        "a's step still credits protection b's cast may have replaced: [" +
                        string.Join(",", a.PreCoveredRecipientUnitIds) + "]");
                }));
        }

        // Review R11 (R9-F4): Prayer (Any selector) rebuilt with an outer
        // creature-fact condition (a read, fixed HasFact): its true branch
        // applies PrayerBuff unconditionally, its false branch holds Prayer's
        // original IsEnemy actions. "beast" has the fact and is read as hostile:
        // it is still covered (initially and when recomputed), and a Classic
        // skip with its effective PrayerBuff holds.
        private static void TestMbbOuterConditionRelation()
        {
            const string creature = "734a29b693e9ec346ba2951b27987e33";
            var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff);
            BlueprintAbility spell = dump.Get<BlueprintAbility>(MbbPrayer);
            var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            var fact = NativeObject<Kingmaker.Blueprints.Classes.BlueprintFeature>();
            SetNative(fact, "m_AssetGuid", creature);
            var has = NativeObject<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact>();
            SetNative(has, "Fact", fact);
            var outer = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
            SetNative(outer, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
            {
                Conditions = new Kingmaker.ElementsSystem.Condition[] { has }
            });
            SetNative(outer, "IfTrue", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[]
                {
                    MbbApplicationCopy(spell, dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbPrayerBuff))
                }
            });
            SetNative(outer, "IfFalse", new Kingmaker.ElementsSystem.ActionList { Actions = run.Actions.Actions });
            run.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { outer };
            float radius = 40f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "beast", radius * 0.5f }, { "other", radius * 0.25f }
            };
            MbbGroup group = MbbGroupCaster(dump, MbbPrayer, positions, id => id == "caster");
            EffectExpression expression = group.Effects[group.Ability.Canonical];
            Func<string, string, bool?> reads = (unit, key) => key == creature ? unit == "beast"
                : key == MbbEnemyOfCaster ? unit == "beast" : key == MbbAllyOfCaster ? unit != "beast" : (bool?)false;
            ProviderPlanningOption option = MbbCustomOption(dump, group.Snapshot.Providers.Single(), expression,
                MbbUnits(positions.Keys), id => id == "caster", positions, reads,
                key => key == creature || key == MbbEnemyOfCaster || key == MbbAllyOfCaster);
            MbbSubcases(
                MbbCase("initial coverage", () => MbbExpect(option.CoveredTargetIdsForAnchor("caster").Contains("beast"),
                    "A relation predicate on the unselected branch vetoed beast's unconditional benefit: [" +
                    string.Join(",", option.CoveredTargetIdsForAnchor("caster")) + "]")),
                MbbCase("recomputed coverage", () =>
                {
                    var state = new SequentialRecipientState();
                    state.EnterRoutine("long");
                    state.Record(option, expression, new[] { "other" }, "caster");
                    MbbExpect(state.Reevaluate(option, expression).CoveredTargetIdsForAnchor("caster").Contains("beast"),
                        "Recomputed coverage applied the unselected branch's relation veto.");
                }),
                MbbCase("Classic skip", () =>
                {
                    BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-outer-condition-relation");
                    SourceAssignmentProfile assignment = Assignment(group.Ability.Canonical, group.Ability,
                        new[] { "beast" });
                    assignment.CastingAssignments[0].CasterUnitId = "caster";
                    profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
                    var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                    {
                        { "beast", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerBuff, 5000, 20, 0) } }
                    });
                    RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long", group.Snapshot, live,
                        group.Effects, new List<ProviderPlanningOption> { option }, new CastEnhancementSnapshot[0]);
                    MbbExpect(result.Plan.Outcomes.Any(o => o.UnitId == "beast" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "The unselected branch's relation refused a valid Classic skip: " +
                        string.Join(",", result.Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
                }));
        }

        // Review R11 (R9-F5): Classic forecast; a's live Firebrand on m. b's
        // funded Firebrand (pinned, on the known ally k) can reach m, whose
        // relation to b is unread; in the next routine a's request for m gets
        // no free skip. A disjoint b disk keeps a's skip.
        private static void TestMbbClassicPossibleReplacement()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            AbilityKey key = MbbSpellKey(firebrand);
            Func<Dictionary<string, float>, SequentialForecastPlanner.Result> forecast = positions =>
            {
                MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                    new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                    positions);
                List<UnitSnapshot> units = MbbUnits(positions.Keys);
                List<ProviderPlanningOption> options = party.Snapshot.Providers.Select(provider => MbbCustomOption(dump,
                    provider, party.Effects[provider.Key.Ability.Canonical], units, id => true, positions,
                    (unit, fact) =>
                    {
                        bool unread = provider.Key.CasterUnitId == "b" && unit == "m";
                        if (fact == MbbAllyOfCaster) return unread ? (bool?)null : true;
                        if (fact == MbbEnemyOfCaster) return unread ? (bool?)null : false;
                        return false;
                    })).ToList();
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-possible-replacement");
                SourceAssignmentProfile byB = Assignment(key.Canonical, key, new[] { "k" });
                byB.CastingAssignments[0].CasterUnitId = "b";
                SourceAssignmentProfile byA = Assignment(key.Canonical, key, new[] { "m" });
                byA.CastingAssignments[0].CasterUnitId = "a";
                profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(byB);
                profile.Routines.First(rt => rt.RoutineId == "short").Assignments.Add(byA);
                var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "m", new[] { new ActiveEffectInstance(EffectKind.Buff, firebrandBuff, 5000, 20, 0, false, true, "a") } }
                });
                return SequentialForecastPlanner.Compute(profile, new[] { "long", "short" }, party.Snapshot, live,
                    party.Effects, options, new CastEnhancementSnapshot[0]);
            };
            MbbSubcases(
                MbbCase("b may reach m", () =>
                {
                    SequentialForecastPlanner.Result result = forecast(new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "a", 0f }, { "m", 2f }, { "b", 4f }, { "k", 5f }
                    });
                    MbbExpect(result.Occurrences[0].Plan.Steps.Count == 1 &&
                        !result.Occurrences[0].Plan.Steps[0].ExpectedRecipientUnitIds.Contains("m"),
                        "b's step is not the intended possibly-reaching cast.");
                    MbbExpect(result.Occurrences[1].Plan.Steps.Count == 1 &&
                        result.Occurrences[1].Plan.Steps[0].Provider.CasterUnitId == "a",
                        "a's Firebrand on m skipped although b's cast may have replaced it: " +
                        string.Join(",", result.Occurrences[1].Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
                }),
                MbbCase("disjoint b disk", () =>
                {
                    SequentialForecastPlanner.Result result = forecast(new Dictionary<string, float>(StringComparer.Ordinal)
                    {
                        { "a", 0f }, { "m", 2f }, { "b", 100f }, { "k", 101f }
                    });
                    MbbExpect(result.Occurrences[1].Plan.Steps.Count == 0,
                        "b's disjoint cast cost a's free Classic skip.");
                }));
        }

        // Review R11-F1: Feather Step, Mass with its selector and beneficial
        // run action unchanged plus a native AbilityExecuteActionOnCast whose
        // list deals damage: refused by discovery and provider admission; the
        // unmodified spell stays admitted.
        private static void TestMbbOnCastActionRefused()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            MbbSubcases(
                MbbCase("damaging on-cast action", () =>
                {
                    var dump = new NativeBlueprintDump(feather, featherBuff);
                    BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
                    var onCast = NativeObject<Kingmaker.UnitLogic.Abilities.Components.AbilityExecuteActionOnCast>();
                    SetNative(onCast, "Actions", new Kingmaker.ElementsSystem.ActionList
                    {
                        Actions = new Kingmaker.ElementsSystem.GameAction[]
                        {
                            NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>()
                        }
                    });
                    SetNative(spell, "Components", spell.ComponentsArray.Concat(
                        new Kingmaker.Blueprints.BlueprintComponent[] { onCast }).ToArray());
                    MbbDiscovery discovered = MbbDiscover(dump, feather);
                    var providers = new List<ProviderSnapshot>();
                    ProviderSnapshot provider = MbbAdmission(dump).AdmitSpell("caster", "book", false, "Book",
                        MbbRead(dump, feather, feather, SourceKind.Spellbook, 9), 3, 0, "pool", 1, new string[0],
                        providers);
                    MbbExpect(discovered.Facts.Diagnostics.Any(d => d.Code == NativeCatalogExporter.OnCastActionCode),
                        "The on-cast action was not reported.");
                    MbbExpect(!discovered.Included && provider == null && providers.Count == 0,
                        "An allied burst with a damaging on-cast action was admitted: " + discovered.Repaired.Reason);
                }),
                MbbCase("original control", () => MbbExpect(
                    MbbDiscover(new NativeBlueprintDump(feather, featherBuff), feather).Included,
                    "Unmodified Feather Step, Mass is no longer admitted.")));
        }

        // Review R11-F2: a saved Classic Good Hope for allies at 0 and 20 feet
        // with one slot; the ally at 10 feet (not requested) is a legal centre
        // covering both: one paid step anchored there, both fulfilled.
        private static void TestMbbClassicBestCentre()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 30f }, { "a", 0f }, { "m", 10f * 0.3048f }, { "f", 20f * 0.3048f }
            };
            MbbParty party = MbbGoodHopeParty(dump, positions, (unit, fact) => false, 1, id => true);
            AbilityKey hope = party.Abilities[MbbGoodHope];
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-best-centre");
            SourceAssignmentProfile assignment = Assignment(hope.Canonical, hope, new[] { "a", "f" });
            assignment.CastingAssignments[0].CasterUnitId = "bard";
            profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
            RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long", party.Snapshot,
                new ActiveEffectSnapshot(null), party.Effects, party.Options, new CastEnhancementSnapshot[0]);
            MbbExpect(result.Plan.Steps.Count == 1 && result.Plan.Steps[0].AnchorUnitId == "m" &&
                result.Plan.Steps[0].Reservation.Units == 1 &&
                result.Plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Fulfilled) == 2 &&
                !result.Plan.Outcomes.Any(o => o.Kind == TargetOutcomeKind.Unfulfilled),
                "Classic did not pick the one centre covering both requested allies: " +
                string.Join(",", result.Plan.Steps.Select(s => s.AnchorUnitId)) + " / " +
                string.Join(",", result.Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
        }

        // Review R11-F3: the Classic forecast of a catalogue-saved Prayer (its
        // source id is the aggregate catalogue identity; the effects are keyed
        // by ability, as PlannerUiSession passes them). "turned" is inside the
        // radius and hostile to the Prayer caster (it takes PrayerDebuff). The
        // next routine's Feather Step, Mass lands only on a unit with
        // PrayerBuff: it does not reach "turned".
        private static void TestMbbSavedPrayerForecastExpression()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff, feather, featherBuff);
            BlueprintAbility step = dump.Get<BlueprintAbility>(feather);
            var run = step.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            var hasPrayer = NativeObject<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasBuff>();
            SetNative(hasPrayer, "Buff", dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbPrayerBuff));
            var gate = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
            SetNative(gate, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
            {
                Conditions = new Kingmaker.ElementsSystem.Condition[] { hasPrayer }
            });
            SetNative(gate, "IfTrue", new Kingmaker.ElementsSystem.ActionList { Actions = run.Actions.Actions });
            SetNative(gate, "IfFalse", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            run.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { gate };
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "turned", 3f }, { "w", 5f }
            };
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            var providers = new List<ProviderSnapshot>();
            ResourcePoolSnapshot prayerPool = PartyProviderAdmission.SpontaneousLevelPool("caster", "book-caster", 3, 2, 2);
            ResourcePoolSnapshot featherPool = PartyProviderAdmission.SpontaneousLevelPool("w", "book-w", 3, 2, 2);
            admission.AdmitSpell("caster", "book-caster", false, "Book", MbbRead(dump, MbbPrayer, "Prayer",
                SourceKind.Spellbook, 9), 3, 0, prayerPool.PoolKey, 1, new string[0], providers);
            admission.AdmitSpell("w", "book-w", false, "Book", MbbRead(dump, feather, "Feather Step, Mass",
                SourceKind.Spellbook, 9), 3, 0, featherPool.PoolKey, 1, new string[0], providers);
            var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
            ProviderSnapshot prayerProvider = providers.Single(p => p.Key.Ability.BaseAbilityGuid == MbbPrayer);
            ProviderSnapshot featherProvider = providers.Single(p => p.Key.Ability.BaseAbilityGuid == feather);
            string prayerBuffKey = "buff-predicate:" + MbbPrayerBuff;
            var options = new List<ProviderPlanningOption>
            {
                MbbCustomOption(dump, prayerProvider, effects[prayerProvider.Key.Ability.Canonical], units,
                    id => id == "caster", positions, (unit, fact) => fact == MbbEnemyOfCaster ? unit == "turned"
                        : fact == MbbAllyOfCaster ? unit != "turned" : (bool?)false),
                MbbCustomOption(dump, featherProvider, effects[featherProvider.Key.Ability.Canonical], units,
                    id => true, positions, (unit, fact) => fact == MbbEnemyOfCaster ? false
                        : fact == MbbAllyOfCaster ? true : fact == prayerBuffKey ? false : (bool?)false)
            };
            var snapshot = new PartyProviderSnapshot(units, providers, new[] { prayerPool, featherPool });
            AbilityKey prayerKey = prayerProvider.Key.Ability, featherKey = featherProvider.Key.Ability;
            string savedPrayer = CatalogSourceIdentity.For(prayerKey, effects[prayerKey.Canonical]);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-saved-prayer-forecast");
            SourceAssignmentProfile prayer = Assignment(savedPrayer, prayerKey, new[] { "caster" });
            prayer.CastingAssignments[0].CasterUnitId = "caster";
            SourceAssignmentProfile later = Assignment(featherKey.Canonical, featherKey, new[] { "turned" });
            later.CastingAssignments[0].CasterUnitId = "w";
            profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(prayer);
            profile.Routines.First(rt => rt.RoutineId == "short").Assignments.Add(later);
            SequentialForecastPlanner.Result result = SequentialForecastPlanner.Compute(profile,
                new[] { "long", "short" }, snapshot, new ActiveEffectSnapshot(null), effects, options,
                new CastEnhancementSnapshot[0]);
            MbbExpect(savedPrayer != prayerKey.Canonical && result.Occurrences[0].Plan.Steps.Count == 1 &&
                result.Occurrences[0].Plan.Steps[0].SourceId == savedPrayer,
                "The saved Prayer is not planned under its catalogue identity: " + savedPrayer);
            MbbExpect(!result.Occurrences[1].Plan.Outcomes.Any(o => o.UnitId == "turned" &&
                    o.Kind == TargetOutcomeKind.Fulfilled),
                "The forecast credited PrayerBuff to a member Prayer's hostile branch reached: " +
                string.Join(",", result.Occurrences[1].Plan.Outcomes.Select(o => o.UnitId + "=" + o.Kind)));
        }

        // Review R11-F4: Firebelly (caster-only) with the live caster
        // unreadable, and with native CanTarget refusing the owner: no legal
        // anchor, no reservation, no executable step. Accepted owner: one slot.
        private static void TestMbbPersonalOwnerVerdict()
        {
            var dump = new NativeBlueprintDump(MbbFirebelly, MbbFirebellyBuff, MbbFirebellyBreath);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal) { { "cleric", 0f }, { "ally", 2f } };
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool("cleric", "cleric-book", 1, 2, 2);
            var providers = new List<ProviderSnapshot>();
            ProviderSnapshot provider = admission.AdmitSpell("cleric", "cleric-book", false, "Cleric",
                MbbRead(dump, MbbFirebelly, "Firebelly", SourceKind.Spellbook, 5), 1, 0, pool.PoolKey, 1,
                new string[0], providers);
            var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
            var snapshot = new PartyProviderSnapshot(units, providers, new[] { pool });
            AbilityKey key = provider.Key.Ability;
            Func<bool, Func<string, bool>, KeyValuePair<ProviderTargetingDecision, ExplicitCastingPlan>> plan =
                (readable, canTarget) =>
                {
                    ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider,
                        effects[key.Canonical], units, readable, false, null, canTarget, MbbDistance(positions));
                    var options = new List<ProviderPlanningOption>
                    {
                        ProviderTargetingPlanner.BuildOption(provider, decision, CastExecutionStrategy.DirectRuleCast,
                            "ordinary-direct-rule-cast")
                    };
                    return new KeyValuePair<ProviderTargetingDecision, ExplicitCastingPlan>(decision,
                        new ExplicitCastingCompiler().Compile(CastingDocument(
                                DirectCasting("belly", "long", "cleric", "cleric", key.Canonical, key)),
                            snapshot, options, effects, new CastEnhancementSnapshot[0]));
                };
            Action<string, bool, Func<string, bool>> refused = (name, readable, canTarget) =>
            {
                KeyValuePair<ProviderTargetingDecision, ExplicitCastingPlan> result = plan(readable, canTarget);
                ResolvedCasting cast = result.Value.CastingById("belly");
                CastingApplyDecision gate = new CastingExecutionGate().Evaluate(result.Value, CastingApplyMode.Ordinary,
                    "long");
                ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(result.Value, gate,
                    new List<ProviderPlanningOption>(), effects);
                MbbExpect(result.Key.Anchors.Length == 0 && cast.Readiness == ResolvedCastingReadiness.Blocked &&
                    cast.Cost.Count == 0 && (!steps.Converted || steps.Plan.Steps.Count == 0),
                    name + ": Firebelly was offered or reserved without a valid owner target: anchors [" +
                    string.Join(",", result.Key.Anchors) + "] " + cast.Readiness);
            };
            MbbSubcases(
                MbbCase("live caster unreadable", () => refused("unreadable caster", false, id => true)),
                MbbCase("owner target refused", () => refused("refused owner", true, id => false)),
                MbbCase("accepted owner control", () =>
                {
                    ResolvedCasting cast = plan(true, id => id == "cleric").Value.CastingById("belly");
                    MbbExpect(cast.Readiness == ResolvedCastingReadiness.Ready && cast.Cost.Sum(c => c.Units) == 1,
                        "Firebelly on an accepted owner is no longer one slot: " + cast.Readiness);
                }));
        }

        // Review R11-F5: native Feather Step, Mass (15 ft = 4.572 m): a unit
        // at 4.57 m is covered, one at 4.575 m is not (no margin without
        // native evidence for one). A non-finite radius offers no centre and
        // reserves nothing.
        private static void TestMbbAreaBoundary()
        {
            string feather = MbbPointGroupSpells[1][1];
            var dump = new NativeBlueprintDump(feather, MbbPointGroupSpells[1][2]);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "inside", 4.57f }, { "outside", 4.575f }
            };
            MbbGroup group = MbbGroupCaster(dump, feather, positions, id => id == "caster");
            ProviderSnapshot provider = group.Snapshot.Providers.Single();
            EffectExpression expression = group.Effects[group.Ability.Canonical];
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(feather);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            Func<AlliedAreaCoverage, ProviderTargetingDecision> decide = coverage => ProviderTargetingPlanner.Decide(
                provider, expression, units, true, false, coverage, id => id == "caster", MbbDistance(positions), null,
                MbbFriendlyReads(null));
            Action<float> unusable = radius =>
            {
                ProviderTargetingDecision decision = decide(new AlliedAreaCoverage(radius, area.SourceAbilityGuid, true));
                ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                    CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast");
                ResolvedCasting cast = new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("step", "long", "caster", group.Ability.Canonical, group.Ability, new[] { "caster" })),
                    group.Snapshot, new List<ProviderPlanningOption> { option }, group.Effects,
                    new CastEnhancementSnapshot[0]).Castings.Single();
                MbbExpect(decision.Anchors.Length == 0 && cast.Readiness == ResolvedCastingReadiness.Blocked &&
                    cast.Cost.Count == 0,
                    "A " + radius + " radius offered a centre or reserved a slot: anchors [" +
                    string.Join(",", decision.Anchors) + "] " + cast.Readiness);
            };
            MbbSubcases(
                MbbCase("boundary", () =>
                {
                    ProviderTargetingDecision decision = decide(area);
                    MbbExpect(Math.Abs(area.Radius - 15f * 0.3048f) < 0.0001f &&
                        decision.RecipientIdsByAnchor["caster"].OrderBy(v => v, StringComparer.Ordinal)
                            .SequenceEqual(new[] { "caster", "inside" }),
                        "The disk boundary is not the recorded radius: [" +
                        string.Join(",", decision.RecipientIdsByAnchor["caster"]) + "]");
                }),
                MbbCase("infinite radius", () => unusable(float.PositiveInfinity)),
                MbbCase("NaN radius", () => unusable(float.NaN)));
        }

        // GoodHopeBuff gains an AddFactContextActions whose Activated list
        // applies the given committed buff to its bearer (a copy of Good
        // Hope's own ContextActionApplyBuff, ToCaster false).
        private static void MbbGoodHopeBuffActivation(NativeBlueprintDump dump, string appliedBuff)
        {
            var buff = dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbGoodHopeBuff);
            var apply = MbbApplicationCopy(dump.Get<BlueprintAbility>(MbbGoodHope),
                dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(appliedBuff));
            MbbExpect(apply != null && !apply.ToCaster, "No bearer-targeted application could be built.");
            var activation = NativeObject<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>();
            SetNative(activation, "Activated", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[] { apply }
            });
            SetNative(activation, "Deactivated", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            SetNative(activation, "NewRound", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            SetNative(buff, "Components", (buff.ComponentsArray ?? new Kingmaker.Blueprints.BlueprintComponent[0])
                .Concat(new Kingmaker.Blueprints.BlueprintComponent[] { activation }).ToArray());
        }

        // Review R6 (R5-F1): Feather Step, Mass's own application repointed to
        // the committed Crushing Despair buff (m_Harmful clear, no condition,
        // only Penalty modifiers). Refused by discovery and provider
        // admission; unmodified Feather Step and the size spells (mixed
        // adjustments) stay admitted.
        private static void TestMbbPointPenaltyBuffRefused()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            var dump = new NativeBlueprintDump(feather, featherBuff, MbbCrushingDespairBuff);
            BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
            var despair = dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbCrushingDespairBuff);
            MbbExpect(MbbReplaceApplications(spell, apply => SetNative(apply, "Buff", despair)) != 0,
                "Feather Step, Mass's application could not be repointed.");
            MbbDiscovery penalizing = MbbDiscover(dump, feather);
            var providers = new List<ProviderSnapshot>();
            ProviderSnapshot provider = MbbAdmission(dump).AdmitSpell("caster", "book", false, "Book",
                MbbRead(dump, feather, feather, SourceKind.Spellbook, 9), 3, 0, "pool", 1, new string[0], providers);
            MbbExpect(penalizing.Facts.Effects.Any(e => e.EffectId == MbbCrushingDespairBuff && e.Harmful == false &&
                    e.Target == "AlliedAreaRecipients" && (e.GrantedConditions ?? new string[0]).Count == 0),
                "The repointed application is not the intended Crushing Despair shape.");
            MbbExpect(penalizing.Repaired.Disposition == "exclude" && !penalizing.Included && provider == null &&
                providers.Count == 0,
                "A point-capable allied burst applying only penalties was admitted: " + penalizing.Repaired.Reason);
            MbbExpect(MbbDiscover(new NativeBlueprintDump(feather, featherBuff), feather).Included,
                "Unmodified Feather Step, Mass is no longer admitted.");
            var sizes = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbExpect(MbbDiscover(sizes, MbbPointGroupSpells[4][1]).Included &&
                MbbDiscover(sizes, MbbPointGroupSpells[5][1]).Included,
                "Enlarge or Reduce Person, Mass (mixed adjustments) is no longer admitted.");
        }

        // Review R6 (R5-F2): the approved GoodHopeBuff applies the committed
        // Crushing Despair buff to its bearer when it activates. The waiver
        // no longer applies; ordinary Good Hope and Firebelly stay available.
        private static void TestMbbActivationPenaltyRefused()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbCrushingDespairBuff);
            MbbGoodHopeBuffActivation(dump, MbbCrushingDespairBuff);
            MbbDiscovery penalized = MbbDiscover(dump, MbbGoodHope);
            var providers = new List<ProviderSnapshot>();
            ProviderSnapshot provider = MbbAdmission(dump).AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(dump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, "pool", 1,
                new string[0], providers);
            MbbExpect(penalized.Facts.Effects.Any(e => e.EffectId == MbbGoodHopeBuff &&
                    (e.FactActions ?? new NativeCandidateFactActions[0]).Any(list => list.List == "Activated" &&
                        (list.AppliedEffects ?? new NativeCandidateEffectFacts[0]).Any(applied =>
                            applied.EffectId == MbbCrushingDespairBuff && applied.Harmful == false))),
                "The exporter did not record the Crushing Despair the buff applies on activation.");
            MbbExpect(penalized.Repaired.Disposition == "exclude" && !penalized.Included && provider == null &&
                providers.Count == 0,
                "Good Hope was admitted although its buff applies Crushing Despair to its bearer: " +
                penalized.Repaired.Reason);
            var real = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbFirebelly, MbbFirebellyBuff,
                MbbFirebellyBreath);
            MbbExpect(MbbDiscover(real, MbbGoodHope).Included && MbbDiscover(real, MbbFirebelly).Included,
                "The unmodified Good Hope or Firebelly is no longer admitted.");
        }

        // Review R6 (R5-F5): Mass Enlarge in the first routine reaches every
        // humanoid the later Mass Reduce would benefit. The one-pass forecast
        // stores an empty beneficiary list for Reduce, refuses it with the
        // empty-coverage reason and reserves nothing.
        private static void TestMbbSequentialEmptyCoverage()
        {
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float r = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "w", 10f * r }, { "a", 0f }, { "b", 0.5f * r }
            };
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                new Dictionary<string, string[]> { { "w", new[] { enlarge, reduce } } }, positions,
                new Dictionary<string, Func<string, bool>> { { enlarge, id => id == "a" }, { reduce, id => id == "b" } });
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            CastingForecast onePass = new CastingForecastService().ForecastOnePass(CastingDocument(
                    GroupCasting("enlarge", "long", "w", enlargeKey.Canonical, enlargeKey, new[] { "a", "b" },
                        null, CastingTargetMode.AnchoredOrigin, "a"),
                    GroupCasting("reduce", "important", "w", reduceKey.Canonical, reduceKey, new[] { "a", "b" },
                        null, CastingTargetMode.AnchoredOrigin, "b")),
                party.Snapshot, party.Options, party.Effects);
            ResolvedCasting first = onePass.Plan.CastingById("enlarge"), second = onePass.Plan.CastingById("reduce");
            MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(c => c.Units) == 1 &&
                second.Readiness == ResolvedCastingReadiness.Blocked && second.Cost.Count == 0 &&
                second.PredictedBeneficiaryUnitIds.Count == 0 &&
                second.ReadinessReasons.Contains("predicted-coverage-empty:b"),
                "The empty sequential forecast kept its original beneficiaries: " + second.Readiness + " [" +
                string.Join(",", second.PredictedBeneficiaryUnitIds) + "] " + string.Join(",", second.ReadinessReasons));
        }

        // Review R6 (R5-F6): the bard starts with no level-3 slot; Good Hope
        // over an ally with a longer Good Hope and the unbuffed bard is a
        // resource refusal whose notes promise neither a cast nor shortening.
        private static void TestMbbExhaustedMixedNotes()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff);
            MbbParty party = MbbBardPartyWithSlots(dump, 0, MbbGoodHope, MbbHaste);
            AbilityKey hope = party.Abilities[MbbGoodHope];
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "ally-near", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbGoodHopeBuff, 5000, 7, 0) } }
            });
            ResolvedCasting refused = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("hope", "long", "bard", hope.Canonical, hope, new[] { "bard", "ally-near" })),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live)
                .CastingById("hope");
            List<string> shown = refused.ExistingEffectNotes.Select(note =>
                CastingRunPresentation.DescribeExistingEffectNote(note)).ToList();
            MbbExpect(refused.Readiness == ResolvedCastingReadiness.Blocked && refused.Cost.Count == 0 &&
                refused.ReadinessReasons.Any(reason => reason.StartsWith("resource-pool-exhausted:",
                    StringComparison.Ordinal)),
                "The exhausted Good Hope was not a zero-cost resource refusal: " + refused.Readiness + " " +
                string.Join(",", refused.ReadinessReasons));
            MbbExpect(shown.Contains(PlannerText.Get("run.effect.keptblocked", "ally-near")) &&
                !refused.ExistingEffectNotes.Any(note => note.StartsWith("already-", StringComparison.Ordinal) ||
                    note.StartsWith("existing-active-recast:", StringComparison.Ordinal) ||
                    note.StartsWith("existing-insufficient:", StringComparison.Ordinal) ||
                    note.StartsWith("existing-incomplete:", StringComparison.Ordinal)),
                "The exhausted casting's notes still promise a cast: " + string.Join(" | ", shown));
        }

        // Review R6-F1: native Prayer (Personal, AbilityTargetsAround Any,
        // PrayerBuff on the false branch of ContextConditionIsEnemy). A
        // friendly in-radius ally keeps its coverage: one funded cast whose
        // bound branch for the ally is PrayerBuff, never PrayerDebuff.
        private static void TestMbbPrayerFriendlyCoverage()
        {
            var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff);
            MbbDiscovery prayer = MbbDiscover(dump, MbbPrayer);
            MbbExpect(prayer.Included, "Prayer is not admitted: " + prayer.Repaired.Reason);
            float radius = 40f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "ally", radius * 0.5f }, { "far", radius * 3f }
            };
            MbbGroup group = MbbGroupCaster(dump, MbbPrayer, positions, id => id == "caster");
            ProviderPlanningOption option = group.Options.Single();
            ResolvedCasting casting = new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("prayer", "long", "caster", group.Ability.Canonical, group.Ability,
                        new[] { "caster", "ally" })),
                group.Snapshot, group.Options, group.Effects, new CastEnhancementSnapshot[0]).Castings.Single();
            EffectChanges allyChanges = EffectChangeAnalysis.Analyze(
                option.BindForRecipient(group.Effects[group.Ability.Canonical], "ally"), "caster", "ally");
            MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready && casting.Cost.Sum(c => c.Units) == 1 &&
                casting.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "ally", "caster" }),
                "Prayer lost its friendly recipients: " + casting.Readiness + " [" +
                string.Join(",", casting.PredictedBeneficiaryUnitIds) + "] " + string.Join(",", casting.ReadinessReasons));
            MbbExpect(allyChanges.Applied.Contains(new KeyValuePair<string, string>("ally", MbbPrayerBuff)) &&
                !allyChanges.Applied.Any(pair => pair.Value == MbbPrayerDebuff),
                "Prayer's bound branch for a friendly ally is not PrayerBuff: " +
                string.Join(",", allyChanges.Applied.Select(pair => pair.Key + "=" + pair.Value)));
        }

        // Review R6-F2: Good Hope's Despair check is the native buff predicate
        // (keyed as one, read with descriptor.Buffs.HasFact - pinned in the
        // native contract test). In the bard's disk the near ally's Despair is
        // present, absent or unreadable; the bard (no Despair) still receives
        // Good Hope in every case.
        private static void TestMbbGoodHopeDespairRead()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff);
            MbbParty party = MbbBardParty(dump, MbbGoodHope, MbbHaste);
            AbilityKey hope = party.Abilities[MbbGoodHope];
            EffectExpression expression = party.Effects[hope.Canonical];
            string key = MbbFind<ConditionalEffectExpression>(expression)
                .Where(c => c.RecipientFactGate != null).SelectMany(c => c.RecipientFactGate.Facts)
                .Select(f => f.Key).SingleOrDefault(k => k != null && k.Contains(MbbCrushingDespairBuff));
            MbbExpect(key == "buff-predicate:" + MbbCrushingDespairBuff,
                "Good Hope's Despair check is not keyed as the native buff predicate: " + key);
            ProviderSnapshot provider = party.Snapshot.Providers.Single(p => p.Key.Ability.Equals(hope));
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(MbbGoodHope);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 0f }, { "ally-near", 3f }, { "ally-far", 10f }
            };
            foreach (bool? despair in new bool?[] { true, false, null })
            {
                ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression,
                    party.Snapshot.Units, true, false, area, id => true, MbbDistance(positions), null,
                    MbbFriendlyReads((unit, fact) => fact != key ? (bool?)false : unit == "ally-near" ? despair : false));
                string[] expected = despair == false ? new[] { "ally-near", "bard" } : new[] { "bard" };
                MbbExpect(decision.RecipientIdsByAnchor["bard"].SequenceEqual(expected),
                    "Good Hope coverage with Despair " + (despair.HasValue ? despair.Value.ToString() : "unreadable") +
                    ": " + string.Join(",", decision.RecipientIdsByAnchor["bard"]));
                ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                    CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast");
                ResolvedCasting casting = new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("hope", "long", "bard", hope.Canonical, hope, new[] { "bard", "ally-near" })),
                    party.Snapshot, new List<ProviderPlanningOption> { option }, party.Effects,
                    new CastEnhancementSnapshot[0]).Castings.Single();
                MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready && casting.Cost.Sum(c => c.Units) == 1 &&
                    casting.PredictedBeneficiaryUnitIds.SequenceEqual(expected),
                    "Good Hope's mixed disk was not one cast for the ally without Despair: " + casting.Readiness);
            }
        }

        // Review R6-F3: Feather Step, Mass's sole application inside a native
        // Conditional with an EMPTY AND checker (which always runs its true
        // branch). On the true branch: covered, one slot. On the false branch
        // only: nobody covered, refused at zero cost.
        private static void TestMbbEmptyCheckerBranch()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            float radius = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "ally", radius * 0.5f }
            };
            foreach (bool onTrue in new[] { true, false })
            {
                var dump = new NativeBlueprintDump(feather, featherBuff);
                BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
                var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
                var actions = run.Actions.Actions;
                var none = new Kingmaker.ElementsSystem.GameAction[0];
                var gate = NativeObject<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>();
                SetNative(gate, "ConditionsChecker", new Kingmaker.ElementsSystem.ConditionsChecker
                {
                    Conditions = new Kingmaker.ElementsSystem.Condition[0]
                });
                SetNative(gate, "IfTrue", new Kingmaker.ElementsSystem.ActionList { Actions = onTrue ? actions : none });
                SetNative(gate, "IfFalse", new Kingmaker.ElementsSystem.ActionList { Actions = onTrue ? none : actions });
                run.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { gate };
                MbbGroup group = MbbGroupCaster(dump, feather, positions, id => true);
                ResolvedCasting casting = new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("step", "long", "caster", group.Ability.Canonical, group.Ability,
                            new[] { "caster", "ally" })),
                    group.Snapshot, group.Options, group.Effects, new CastEnhancementSnapshot[0]).Castings.Single();
                if (onTrue)
                    MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready && casting.Cost.Sum(c => c.Units) == 1 &&
                        casting.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "ally", "caster" }),
                        "The empty checker's running branch lost its coverage: " + casting.Readiness);
                else
                    MbbExpect(casting.Readiness == ResolvedCastingReadiness.Blocked && casting.Cost.Count == 0 &&
                        casting.PredictedBeneficiaryUnitIds.Count == 0,
                        "The empty checker's unreachable branch supplied coverage: " + casting.Readiness + " [" +
                        string.Join(",", casting.PredictedBeneficiaryUnitIds) + "]");
            }
        }

        // Review R3-F1: native Mass Reduce on an ally who has Enlarge only
        // removes Enlarge (its HasFact(EnlargePersonBuff) branch applies
        // nothing), so that ally is affected but not a beneficiary. A
        // routine that keeps Enlarge on A refuses the later Mass Reduce
        // over A and B; without the keep, Reduce casts once, grants B and
        // its removal on A is accounted (a later Enlarge skip on A blocks).
        private static void TestMbbRemovalOnlyAllyCounted()
        {
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            string enlargeBuff = MbbPointGroupSpells[4][2];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float r = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "w", 10f * r }, { "x", -0.8f * r }, { "a", 0f }, { "b", 0.5f * r }
            };
            // x and A have Enlarge now (a live fact the native condition reads).
            Func<string, string, bool> facts = (unit, fact) => fact == enlargeBuff && (unit == "x" || unit == "a");
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                new Dictionary<string, string[]> { { "w", new[] { enlarge, reduce } } }, positions, null, facts);
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "x", new[] { new ActiveEffectInstance(EffectKind.Buff, enlargeBuff, null, 9, 0) } },
                { "a", new[] { new ActiveEffectInstance(EffectKind.Buff, enlargeBuff, null, 9, 0) } }
            });
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            ProviderPlanningOption reduceOption = party.Options.Single(o => o.Provider.Key.Ability.Equals(reduceKey));
            // The removal-only ally a is not a beneficiary (the conflict
            // assertions below prove its removal is still analyzed).
            MbbExpect(reduceOption.CoveredTargetIdsForAnchor("b").SequenceEqual(new[] { "b" }),
                "Mass Reduce's burst on b claims the removal-only ally a: covered " +
                string.Join(",", reduceOption.CoveredTargetIdsForAnchor("b")));
            PlannedCasting keep = GroupCasting("keep", "long", "w", enlargeKey.Canonical, enlargeKey, new[] { "x", "a" },
                null, CastingTargetMode.AnchoredOrigin, "x");
            PlannedCasting strip = GroupCasting("reduce", "long", "w", reduceKey.Canonical, reduceKey, new[] { "b" },
                null, CastingTargetMode.AnchoredOrigin, "b");
            ExplicitCastingPlan kept = new ExplicitCastingCompiler().Compile(CastingDocument(keep, strip),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live);
            ResolvedCasting refused = kept.CastingById("reduce");
            MbbExpect(kept.CastingById("keep").Readiness == ResolvedCastingReadiness.AlreadySatisfied &&
                refused.Readiness == ResolvedCastingReadiness.Blocked && refused.Cost.Count == 0 &&
                refused.ReadinessReasons.Contains(ExplicitCastingCompiler.RemovesPlannedEffect + ":keep:" +
                    enlargeBuff + ":a"),
                "Mass Reduce undid Enlarge on a removal-only ally the routine keeps: " + refused.Readiness + " " +
                string.Join(",", refused.ReadinessReasons));
            ExplicitCastingPlan free = new ExplicitCastingCompiler().Compile(CastingDocument(strip,
                    GroupCasting("keep-after", "long", "w", enlargeKey.Canonical, enlargeKey, new[] { "x", "a" },
                        null, CastingTargetMode.AnchoredOrigin, "x")),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], null, null, false, live);
            ResolvedCasting cast = free.CastingById("reduce");
            ResolvedCasting after = free.CastingById("keep-after");
            MbbExpect(cast.Readiness == ResolvedCastingReadiness.Ready && cast.Cost.Sum(c => c.Units) == 1 &&
                cast.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "b" }) &&
                after.Readiness == ResolvedCastingReadiness.Blocked &&
                after.ReadinessReasons.Contains(ExplicitCastingCompiler.EffectRemovedByEarlierCasting + ":reduce:" +
                    enlargeBuff + ":a"),
                "The removal on the removal-only ally was not accounted: " + cast.Readiness + " / " +
                after.Readiness + " " + string.Join(",", after.ReadinessReasons));
            // R3-F4: only a is described as losing Enlarge; x keeps it.
            List<string> shown = after.ExistingEffectNotes.Select(note =>
                CastingRunPresentation.DescribeExistingEffectNote(note)).ToList();
            MbbExpect(shown.Contains(PlannerText.Get("run.effect.removed", "a")) &&
                shown.Contains(PlannerText.Get("run.effect.keptblocked", "x")) &&
                !shown.Contains(PlannerText.Get("run.effect.removed", "x")) &&
                !shown.Any(text => text == PlannerText.Get("run.effect.active", "a") ||
                    text == PlannerText.Get("run.effect.active", "x")),
                "The inspector reports removal for an unaffected ally: " + string.Join(" | ", shown));
        }

        // Review R3-F3: the Classic sequence projects the funded step's
        // provider-bound net changes in action order.
        private static void TestMbbClassicSequenceBoundRemoval()
        {
            const string e = "e0000000000000000000000000000001", fact = "f0000000000000000000000000000001";
            Func<string, EffectExpression> apply = id =>
                new EffectLeafExpression(EffectKind.Buff, id, EffectTarget.CurrentTarget, "fixture", "fixture:" + id);
            EffectExpression removeE = new EmptyEffectExpression("restorative-action:ContextActionRemoveBuff",
                new EffectRemoval(e, EffectTarget.CurrentTarget));
            var shapes = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { "p", apply(e) },
                { "gated", new SequenceEffectExpression(new[] { apply("e0000000000000000000000000000002"),
                    new ConditionalEffectExpression("ContextConditionCasterHasFact", removeE,
                        new SequenceEffectExpression(new EffectExpression[0]),
                        new CasterFactGate(false, new[] { new KeyValuePair<string, bool>(fact, false) })) }) },
                { "apply-then-remove", new SequenceEffectExpression(new[] { apply(e), removeE }) }
            };
            var units = MbbUnits(new[] { "c", "t" });
            var pool = new ResourcePoolSnapshot("c|slots", ResourcePoolKind.SpontaneousLevel, 9, 9, null);
            var providers = new List<ProviderSnapshot>();
            var keys = new Dictionary<string, AbilityKey>(StringComparer.Ordinal);
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
            int index = 0;
            foreach (KeyValuePair<string, EffectExpression> shape in shapes)
            {
                var key = new AbilityKey("b" + (++index).ToString("D31"), string.Empty, 0, SourceKind.Spellbook, string.Empty);
                keys[shape.Key] = key;
                effects[key.Canonical] = shape.Value;
                providers.Add(new ProviderSnapshot(new ProviderKey("c", "book", key, "level-1|heighten-0"), shape.Key, 1,
                    pool.PoolKey, 1, null, null, 5, 50, "fixture", "fixture", "Book", 0, "Book"));
            }
            var snapshot = new PartyProviderSnapshot(units, providers, new[] { pool });
            Func<string, bool?, bool, SequentialForecastPlanner.Result> forecast = (first, gate, eActive) =>
            {
                IReadOnlyDictionary<string, bool> casterFacts = gate.HasValue
                    ? new Dictionary<string, bool> { { fact, gate.Value } } : null;
                var options = providers.Select(provider => new ProviderPlanningOption(provider, new[] { "t" },
                    new[] { "t" }, 5, 50, CastExecutionStrategy.DirectRuleCast, "direct", null, casterFacts)).ToList();
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-bound");
                profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(Assignment(
                    keys[first].Canonical, keys[first], new[] { "t" }, null, ExistingEffectPolicy.Overwrite));
                profile.Routines.First(rt => rt.RoutineId == "short").Assignments.Add(Assignment(
                    keys["p"].Canonical, keys["p"], new[] { "t" }));
                return SequentialForecastPlanner.Compute(profile, new[] { "long", "short" }, snapshot,
                    eActive ? MbbActive("t", e) : new ActiveEffectSnapshot(null), effects, options,
                    new CastEnhancementSnapshot[0]);
            };
            SequentialForecastPlanner.Result gateFalse = forecast("gated", false, true);
            SequentialForecastPlanner.Result gateTrue = forecast("gated", true, true);
            SequentialForecastPlanner.Result applyRemove = forecast("apply-then-remove", null, false);
            MbbExpect(gateFalse.Occurrences[1].Plan.Steps.Count == 0 &&
                gateFalse.Occurrences[1].Plan.Outcomes.All(o => o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                "A known-false removal erased E between Classic routines.");
            MbbExpect(gateTrue.Occurrences[1].Plan.Steps.Count == 1,
                "A known-true removal left E carried into the next Classic routine.");
            MbbExpect(applyRemove.Occurrences[1].Plan.Steps.Count == 1,
                "Applying then removing E left an E marker for the next Classic routine.");
        }

        // Review R2-F5: units inside the burst are not all eligible. Native
        // Mass Blessing of Luck and Resolve applies nothing to a construct,
        // and to an undead only if the caster has Undead Arcana; its
        // coverage follows the recipient's and the caster's facts, and a
        // unit whose facts cannot be read gets no coverage claim. A
        // point-capable selector with its own condition yields no coverage.
        private static void TestMbbRecipientConditions()
        {
            const string construct = "fd389783027d63343b4a5634bd81645f", undead = "734a29b693e9ec346ba2951b27987e33",
                arcana = "1a5e7191279e7cd479b17a6ca438498c";
            string blessing = MbbPointGroupSpells[0][1];
            var dump = new NativeBlueprintDump(blessing, MbbPointGroupSpells[0][2]);
            float radius = 30f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "caster", 0f }, { "living", radius * 0.2f }, { "undead", radius * 0.2f },
                { "construct", radius * 0.2f }, { "unread", radius * 0.2f }, { "far", radius * 3f }
            };
            MbbGroup group = MbbGroupCaster(dump, blessing, positions, id => true);
            EffectExpression expression = group.Effects[group.Ability.Canonical];
            MbbExpect(MbbFind<ConditionalEffectExpression>(expression).Any(c => c.RecipientFactGate != null &&
                    c.RecipientFactGate.Facts.Any(f => f.Key == construct)),
                "The construct condition was not modelled as a recipient-fact gate.");
            ProviderSnapshot provider = group.Snapshot.Providers.Single();
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(blessing);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            Func<string, string, bool?> unitFacts = (unit, fact) => unit == "unread" ? (bool?)null
                : fact == construct ? unit == "construct" : fact == undead ? unit == "undead" : (bool?)false;
            // Review R3 (R2-F5): with the caster's arcana UNREAD (absent from
            // the caster facts) the known-undead ally gets no definite
            // coverage; the living ally, whose branch never reads it, does.
            foreach (bool? hasArcana in new bool?[] { false, true, null })
            {
                var casterFacts = hasArcana.HasValue
                    ? new Dictionary<string, bool> { { arcana, hasArcana.Value } }
                    : new Dictionary<string, bool>();
                ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression,
                    group.Snapshot.Units, true, false, area, id => true, MbbDistance(positions),
                    casterFacts, MbbFriendlyReads(unitFacts));
                string[] expected = hasArcana == true ? new[] { "caster", "living", "undead" } : new[] { "caster", "living" };
                MbbExpect(decision.RecipientIdsByAnchor["caster"].OrderBy(v => v, StringComparer.Ordinal)
                        .SequenceEqual(expected.OrderBy(v => v, StringComparer.Ordinal)),
                    "Mass Blessing coverage ignores recipient conditions (arcana=" + hasArcana + "): " +
                    string.Join(",", decision.RecipientIdsByAnchor["caster"]));
                ProviderPlanningOption option = ProviderTargetingPlanner.BuildOption(provider, decision,
                    CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast", casterFacts);
                ResolvedCasting casting = new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("bless", "long", "caster", group.Ability.Canonical, group.Ability,
                            new[] { "caster", "living", "undead", "construct" })),
                    group.Snapshot, new List<ProviderPlanningOption> { option }, group.Effects,
                    new CastEnhancementSnapshot[0]).Castings.Single();
                MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready &&
                    casting.PredictedBeneficiaryUnitIds.OrderBy(v => v, StringComparer.Ordinal)
                        .SequenceEqual(expected.OrderBy(v => v, StringComparer.Ordinal)) &&
                    !casting.PredictedBeneficiaryUnitIds.Contains("construct") &&
                    !casting.PredictedBeneficiaryUnitIds.Contains("unread") &&
                    casting.Cost.Sum(c => c.Units) == 1,
                    "Mass Blessing claimed an ineligible ally: " + string.Join(",", casting.PredictedBeneficiaryUnitIds));
            }
            // The selector's own condition: none natively; a restrictive one
            // (or an unread one) withholds coverage entirely.
            var selector = blueprint.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround>().Single();
            var checker = new Kingmaker.ElementsSystem.ConditionsChecker
            {
                Conditions = new Kingmaker.ElementsSystem.Condition[]
                {
                    NativeObject<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionIsAlly>()
                }
            };
            SetNative(selector, "m_Condition", checker);
            var resolver = new KingmakerAreaCoverageResolver();
            AlliedAreaCoverage conditioned = resolver.Resolve(blueprint, blueprint);
            string conditionedReason = resolver.LastFailureReason;
            SetNative(selector, "m_Condition", null);
            AlliedAreaCoverage unreadSelector = resolver.Resolve(blueprint, blueprint);
            MbbExpect(conditioned == null && conditionedReason == KingmakerAreaCoverageResolver.ConditionedAreaSelector &&
                unreadSelector == null && resolver.LastFailureReason == KingmakerAreaCoverageResolver.UnreadAreaSelector,
                "A point-capable selector with its own or an unread condition still produced coverage.");
            ProviderTargetingDecision withheld = ProviderTargetingPlanner.Decide(provider, expression,
                group.Snapshot.Units, true, false, conditioned, id => true, MbbDistance(positions));
            MbbExpect(withheld.Anchors.Length == 0 && withheld.ReachableIds.Length == 0,
                "A conditioned point selector still offered a centre.");
        }

        // Review R2-F1: Good Hope's Despair branch removes Despair and
        // applies nothing, so a funded Good Hope is no structural grant; a
        // repeated unconditional group buff (Feather Step, Mass) still is.
        private static void TestMbbOnePassConditionalGrant()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHaste, MbbHasteBuff);
            MbbParty bard = MbbBardParty(dump, MbbGoodHope, MbbHaste);
            AbilityKey hope = bard.Abilities[MbbGoodHope];
            CastingForecast twice = new CastingForecastService().ForecastOnePass(CastingDocument(
                    GroupCasting("hope-1", "long", "bard", hope.Canonical, hope, new[] { "bard", "ally-near" }),
                    GroupCasting("hope-2", "short", "bard", hope.Canonical, hope, new[] { "bard", "ally-near" })),
                bard.Snapshot, bard.Options, bard.Effects);
            ResolvedCasting second = twice.Plan.CastingById("hope-2");
            MbbExpect(second.Readiness == ResolvedCastingReadiness.Ready && second.Cost.Sum(c => c.Units) == 1 &&
                !second.ReadinessReasons.Contains("already-satisfied-structural"),
                "The one-pass forecast treated a possible Despair removal as a Good Hope grant: " +
                second.Readiness + " " + string.Join(",", second.ReadinessReasons));
            var featherDump = new NativeBlueprintDump(MbbPointGroupSpells[1][1], MbbPointGroupSpells[1][2]);
            float radius = 15f * 0.3048f;
            MbbGroup feather = MbbGroupCaster(featherDump, MbbPointGroupSpells[1][1],
                new Dictionary<string, float>(StringComparer.Ordinal) { { "caster", 0f }, { "ally", radius * 0.5f } },
                id => true);
            CastingForecast repeated = new CastingForecastService().ForecastOnePass(CastingDocument(
                    GroupCasting("step-1", "long", "caster", feather.Ability.Canonical, feather.Ability,
                        new[] { "caster", "ally" }),
                    GroupCasting("step-2", "short", "caster", feather.Ability.Canonical, feather.Ability,
                        new[] { "caster", "ally" })),
                feather.Snapshot, feather.Options, feather.Effects);
            MbbExpect(repeated.Plan.CastingById("step-2").Readiness == ResolvedCastingReadiness.AlreadySatisfied &&
                repeated.Plan.CastingById("step-2").Cost.Count == 0,
                "An unconditional repeated group buff lost its structural grant.");
        }

        // Review R2-F3: the Classic sequence forecast no longer carries an
        // effect a funded step removed into the next routine.
        private static void TestMbbClassicSequenceRemoval()
        {
            var dump = new NativeBlueprintDump(MbbAbjuration.SelectMany(row => new[] { row[1], row[2] }).ToArray());
            MbbParty party = MbbWizardParty(dump, 1);
            AbilityKey fire = party.Abilities["Fire"], cold = party.Abilities["Cold"];
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbb-classic-sequence");
            profile.Routines.First(r => r.RoutineId == "long").Assignments.Add(Assignment(
                cold.Canonical, cold, new[] { "wizard" }, null, ExistingEffectPolicy.Overwrite));
            profile.Routines.First(r => r.RoutineId == "short").Assignments.Add(Assignment(
                fire.Canonical, fire, new[] { "wizard" }));
            SequentialForecastPlanner.Result result = SequentialForecastPlanner.Compute(profile,
                new[] { "long", "short" }, party.Snapshot, MbbActive("wizard", MbbAbjuration[3][2]), party.Effects,
                party.Options, new CastEnhancementSnapshot[0]);
            string pool = "wizard|resource|" + MbbAbjurationResource;
            CastPlan second = result.Occurrences[1].Plan;
            MbbExpect(result.Occurrences[0].Plan.Steps.Count == 1 && second.Steps.Count == 0 &&
                second.Outcomes.All(o => o.Kind == TargetOutcomeKind.Unfulfilled) &&
                result.ForecastRemainingByNativePool[pool] == 0,
                "The Classic forecast carried Fire into the routine after the Cold switch: " +
                string.Join(",", second.Outcomes.Select(o => o.Kind + ":" + o.Reason)));
        }

        // Review R2-F4: Classic tries every legal centre before refusing a
        // group provider for a removal conflict.
        private static void TestMbbClassicConflictFreeCentre()
        {
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float r = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "e", -0.9f * r }, { "p", 0f }, { "t", 0.6f * r }, { "q", 1.5f * r }
            };
            Func<Func<string, bool>, CastPlan> plan = reduceCentres =>
            {
                MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "e", 1 }, { "q", 1 } },
                    new Dictionary<string, string[]> { { "e", new[] { enlarge } }, { "q", new[] { reduce } } },
                    positions, new Dictionary<string, Func<string, bool>>
                    {
                        { enlarge, id => id == "e" }, { reduce, reduceCentres }
                    });
                Func<string, string, string, int, BuffCastRequest> request = (guid, assignment, target, order) =>
                    new BuffCastRequest(new BuffSourceDefinition(assignment, MbbSpellKey(guid),
                            party.Effects[MbbSpellKey(guid).Canonical], CastGroupingKind.MassConfiguredTargets),
                        new[] { target }, ExistingEffectPolicy.Overwrite, null, new EnhancementRequest[0], assignment,
                        null, null, null, order);
                return new CastPlanner().PlanRoutine(party.Snapshot, new[]
                    {
                        request(enlarge, "enlarge", "p", 0), request(reduce, "reduce", "t", 1)
                    }, party.Options, new ProviderSelectionPolicy(null, null, null), new ActiveEffectSnapshot(null));
            };
            // Centre t (preferred: it is the requested unit) also reaches p,
            // which the earlier Enlarge covers; centre q reaches t only.
            CastPlan free = plan(id => id == "t" || id == "q");
            CastStep reduced = free.Steps.SingleOrDefault(s => s.AssignmentId == "reduce");
            MbbExpect(free.Steps.Count == 2 && reduced != null && reduced.AnchorUnitId == "q" &&
                !reduced.ExpectedRecipientUnitIds.Contains("p") && reduced.Reservation.Units == 1,
                "Classic refused or mis-centred a conflict-free Reduce: " +
                (reduced == null ? string.Join(",", free.Outcomes.Select(o => o.Reason)) : reduced.AnchorUnitId));
            // Review R3-F5: the safe centre exists but Reduce's caster has no
            // slot: the refusal names the resource, not a removal conflict.
            MbbParty broke = MbbMassParty(dump, new Dictionary<string, int> { { "e", 1 }, { "q", 0 } },
                new Dictionary<string, string[]> { { "e", new[] { enlarge } }, { "q", new[] { reduce } } },
                positions, new Dictionary<string, Func<string, bool>>
                {
                    { enlarge, id => id == "e" }, { reduce, id => id == "t" || id == "q" }
                });
            Func<string, string, string, int, BuffCastRequest> brokeRequest = (guid, assignment, target, order) =>
                new BuffCastRequest(new BuffSourceDefinition(assignment, MbbSpellKey(guid),
                        broke.Effects[MbbSpellKey(guid).Canonical], CastGroupingKind.MassConfiguredTargets),
                    new[] { target }, ExistingEffectPolicy.Overwrite, null, new EnhancementRequest[0], assignment,
                    null, null, null, order);
            CastPlan unfunded = new CastPlanner().PlanRoutine(broke.Snapshot, new[]
                {
                    brokeRequest(enlarge, "enlarge", "p", 0), brokeRequest(reduce, "reduce", "t", 1)
                }, broke.Options, new ProviderSelectionPolicy(null, null, null), new ActiveEffectSnapshot(null));
            TargetPlanOutcome poor = unfunded.Outcomes.Single(o => o.AssignmentId == "reduce");
            MbbExpect(unfunded.Steps.Count == 1 && unfunded.Steps.All(s => s.AssignmentId == "enlarge") &&
                poor.Kind == TargetOutcomeKind.Unfulfilled &&
                poor.Reason == "reduce:no-valid-mass-provider-or-resource",
                "A safe but unfunded Reduce centre was reported as a removal conflict: " + poor.Reason);
            CastPlan blocked = plan(id => id == "t");
            TargetPlanOutcome refused = blocked.Outcomes.Single(o => o.AssignmentId == "reduce");
            MbbExpect(blocked.Steps.Count == 1 && refused.Kind == TargetOutcomeKind.Unfulfilled &&
                refused.Reason.StartsWith("reduce:" + ExplicitCastingCompiler.RemovesPlannedEffect + ":enlarge:",
                    StringComparison.Ordinal),
                "Classic planned a Reduce whose only centre undoes the earlier Enlarge: " + refused.Reason);
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
            internal int Level;
            internal string PoolKey;
            internal string Spellbook;
        }

        // One caster ("caster") who knows the group spell: provider and pool
        // by PartyProviderAdmission, option by ProviderTargetingPlanner from
        // the injected native CanTarget verdict and positions.
        private static MbbGroup MbbGroupCaster(NativeBlueprintDump dump, string guid,
            IDictionary<string, float> positions, Func<string, bool> nativeCanTarget)
        {
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            // Review R3 (R1-F8): the caster casts from a REAL native
            // spontaneous spellbook that serves one of the spell's own
            // SpellListComponent lists (BlueprintSpellbook.SpellList equals
            // the component's list): Inquisitor for Mass Blessing (6), Bard
            // for Feather Step, Mass (3), Sorcerer (wizard list) for
            // Firebrand (7), Heroic Invocation (9) and Enlarge/Reduce Person,
            // Mass (4). The pool is that book's spontaneous level, keyed by
            // the book's own GUID exactly as the snapshot builder keys it.
            var books = new NativeBlueprintDump(MbbSpontaneousBooks);
            var lists = dump.Get<BlueprintAbility>(guid).ComponentsArray
                .OfType<Kingmaker.Blueprints.Classes.Spells.SpellListComponent>().ToList();
            Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook spellbook = null;
            Kingmaker.Blueprints.Classes.Spells.SpellListComponent list = null;
            foreach (string bookGuid in MbbSpontaneousBooks)
            {
                var candidate = books.Get<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook>(bookGuid);
                list = lists.FirstOrDefault(entry => entry.SpellList != null && candidate.SpellList != null &&
                    entry.SpellList.AssetGuid == candidate.SpellList.AssetGuid);
                if (list != null) { spellbook = candidate; break; }
            }
            MbbExpect(spellbook != null && spellbook.Spontaneous,
                guid + " has no supported native spontaneous spellbook among the fixtures.");
            string book = spellbook.AssetGuid;
            int level = list.SpellLevel;
            ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool("caster", book, level, 3, 3);
            var providers = new List<ProviderSnapshot>();
            ProviderSnapshot provider = admission.AdmitSpell("caster", book, false, "Book",
                MbbRead(dump, guid, guid, SourceKind.Spellbook, 17), level, 0, pool.PoolKey, 1, new string[0], providers);
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
                Ability = provider.Key.Ability,
                Level = level,
                PoolKey = pool.PoolKey,
                Spellbook = book
            };
        }

        // The native book and spell level each group spell is cast from.
        private static readonly Dictionary<string, string> MbbExpectedBookAndLevel =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "462c21cebf7820c40a87f5e4d03e17cf", "57fab75111f377248810ece84193a5a5|6" }, // Inquisitor
                { "d219494150ac1f24f9ce14a3d4f66d26", "bc04fc157a8801d41b877ad0d9af03dd|3" }, // Bard
                { "98734a2665c18cd4db71878b0532024a", "b3db3766a4b605040b366265e2af0e50|7" }, // Sorcerer
                { "43740dab07286fe4aa00a6ee104ce7c1", "b3db3766a4b605040b366265e2af0e50|9" }, // Sorcerer
                { "66dc49bf154863148bd217287079245e", "b3db3766a4b605040b366265e2af0e50|4" }, // Sorcerer
                { "2427f2e3ca22ae54ea7337bbab555b16", "b3db3766a4b605040b366265e2af0e50|4" }  // Sorcerer
            };

        // Native spontaneous spellbooks: Inquisitor, Bard, Sorcerer.
        private static readonly string[] MbbSpontaneousBooks =
        {
            "57fab75111f377248810ece84193a5a5", "bc04fc157a8801d41b877ad0d9af03dd", "b3db3766a4b605040b366265e2af0e50"
        };

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
            // 2b. Review R2 (R1-F6): Good Hope KEEPS its approved GoodHopeBuff
            // application and another branch (the Despair branch) also
            // applies an unapproved payload - the native Crushing Despair
            // penalty buff, then an unread buff. Both are refused: one
            // approved leaf never admits an unapproved branch.
            foreach (bool unreadExtra in new[] { false, true })
            {
                var mixedDump = unreadExtra ? new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff)
                    : new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbCrushingDespairBuff);
                BlueprintAbility mixed = mixedDump.Get<BlueprintAbility>(MbbGoodHope);
                Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff extra;
                if (unreadExtra)
                {
                    extra = NativeObject<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>();
                    SetNative(extra, "m_AssetGuid", "e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1e1");
                }
                else extra = mixedDump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbCrushingDespairBuff);
                MbbExpect(MbbAddBranchApplication(mixed, extra), "The Despair branch could not be extended.");
                MbbDiscovery both = MbbDiscover(mixedDump, MbbGoodHope);
                MbbExpect(both.Facts.Effects.Any(e2 => e2.EffectId == MbbGoodHopeBuff && e2.Harmful == false) &&
                    both.Facts.Effects.Any(e2 => e2.EffectId == extra.AssetGuid) &&
                    both.Repaired.Disposition == "exclude" &&
                    both.Repaired.ReasonCode == "harmful-ally-disposition" && !both.Included,
                    "An approved Good Hope leaf admitted an unapproved " + (unreadExtra ? "unread" : "penalty") +
                    " branch: " + both.Repaired.Reason);
            }
            // 2c. Review R3-F2: Good Hope keeps its approved buff branch but
            // its Despair branch also DAMAGES the allied recipient (native
            // ContextActionDealDamage: no persistent leaf, an offensive
            // action). Refused by discovery and by production provider
            // admission; the unmodified spell stays admitted.
            var damageDump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            BlueprintAbility damaging = damageDump.Get<BlueprintAbility>(MbbGoodHope);
            MbbExpect(MbbAppendToFirstConditionalTrue(damaging,
                    NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>()),
                "The Despair branch could not be extended with damage.");
            MbbDiscovery hurt = MbbDiscover(damageDump, MbbGoodHope);
            var hurtProviders = new List<ProviderSnapshot>();
            ProviderSnapshot hurtProvider = MbbAdmission(damageDump).AdmitSpell("bard", MbbBardBook, false, "Bard",
                MbbRead(damageDump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, 7), 3, 0, "pool", 1,
                new string[0], hurtProviders);
            MbbExpect(hurt.Scan.Diagnostics.Any(d => d.Code == "offensive-action") &&
                hurt.Facts.Effects.All(e3 => e3.EffectId == MbbGoodHopeBuff) &&
                hurt.Repaired.Disposition == "exclude" && !hurt.Included &&
                hurtProvider == null && hurtProviders.Count == 0,
                "A Good Hope variant that damages an ally on its Despair branch was admitted: " + hurt.Repaired.Reason);
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

        // Appends to the true branch of the ability's first run-action
        // conditional a copy of its first ContextActionApplyBuff (same
        // native duration contract) applying the given buff instead.
        private static bool MbbAddBranchApplication(BlueprintAbility ability,
            Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff buff)
        {
            var run = ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            var conditional = run.Actions.Actions
                .OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>().FirstOrDefault();
            Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff template = null;
            MbbReplaceApplications(ability, apply => template = template ?? apply);
            if (conditional == null || template == null) return false;
            var copy = (Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff)typeof(object)
                .GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(template, null);
            SetNative(copy, "Buff", buff);
            conditional.IfTrue.Actions = (conditional.IfTrue.Actions ?? new Kingmaker.ElementsSystem.GameAction[0])
                .Concat(new Kingmaker.ElementsSystem.GameAction[] { copy }).ToArray();
            return true;
        }

        private static bool MbbAppendToFirstConditionalTrue(BlueprintAbility ability,
            Kingmaker.ElementsSystem.GameAction action)
        {
            var run = ability.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            var conditional = run.Actions.Actions
                .OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>().FirstOrDefault();
            if (conditional == null) return false;
            conditional.IfTrue.Actions = (conditional.IfTrue.Actions ?? new Kingmaker.ElementsSystem.GameAction[0])
                .Concat(new[] { action }).ToArray();
            return true;
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
                    casting.Cost.Count == 1 && casting.Cost[0].PoolKey == group.PoolKey &&
                    casting.Cost[0].Units == 1 &&
                    group.Spellbook + "|" + group.Level == MbbExpectedBookAndLevel[row[1]] &&
                    group.PoolKey == "caster|spellbook|" + group.Spellbook + "|spontaneous-" + group.Level &&
                    casting.Provider.SpellbookGuid == group.Spellbook &&
                    snapshot.Providers.Single().SpellLevel == group.Level &&
                    casting.Provider.Equals(snapshot.Providers.Single().Key),
                    row[0] + " anchored plan is wrong: " + casting.Readiness + " " +
                    string.Join(",", casting.PredictedBeneficiaryUnitIds) + " " + string.Join(",", casting.ReadinessReasons));
                CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
                ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, decision, options, effects);
                MbbExpect(steps.Converted && steps.Plan.Steps.Count == 1 && steps.Plan.Steps[0].MassCast &&
                    steps.Plan.Steps[0].AnchorUnitId == "anchor" &&
                    steps.Plan.Steps[0].Reservation.PoolKey == group.PoolKey &&
                    steps.Plan.Steps[0].Reservation.Units == 1 &&
                    steps.Plan.Steps[0].Provider.Equals(snapshot.Providers.Single().Key),
                    row[0] + " was not one cast of a level-" + group.Level + " slot on its unit anchor: " + steps.Refusal);
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
