using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.RuntimeTesting;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    // Production casting-first execution slice: live existing-effect policy,
    // review state per routine (restorable), player settings persistence,
    // the production run host and dispatch boundary, and result text.
    internal static partial class Program
    {
        private static void RunProductionExecutionTests(string root)
        {
            Run("existing-effect-sufficiency-rules", TestExistingEffectSufficiencyRules);
            Run("live-existing-effect-skip-in-compiler", TestLiveExistingEffectSkipInCompiler);
            Run("group-existing-effect-uses-intended-recipients",
                TestGroupExistingEffectRecipients);
            Run("group-mixed-coverage-keeps-adequate-recipients",
                TestMixedCoverageGroupCasting);
            Run("review-signature-skip-flip-is-harmless", TestReviewSignatureSkipFlipIsHarmless);
            Run("review-state-is-per-routine-and-restorable", TestReviewStatePerRoutine);
            Run("review-store-round-trip-and-refusals", () => TestReviewStore(root));
            Run("planner-mode-store-defaults-to-classic", () => TestPlannerModeStore(root));
            Run("session-save-preserves-player-settings",
                () => TestSessionSavePreservesSettings(root));
            Run("session-acceptance-survives-reopen",
                () => TestSessionAcceptanceSurvivesReopen(root));
            Run("session-apply-nothing-to-cast", () => TestSessionApplyNothingToCast(root));
            Run("session-never-reissues-a-casting-id", () => TestSessionCastingIdsNotReused(root));
            Run("imported-automatic-casting-becomes-ready-in-place",
                () => TestImportedAutomaticCastingReadyInPlace(root));
            Run("multi-provider-caster-picks-the-exact-source", () => TestExactProviderPicker(root));
            Run("focused-casting-routine-order-and-recast", () => TestFocusedRoutineOrderAndRecast(root));
            Run("planners-show-unusable-files-and-refused-saves", () => TestPersistenceNoticesShown(root));
            Run("provider-choices-name-the-book-and-refuse-twins", () => TestProviderLabelsAndTwins(root));
            Run("next-casting-enhancements-follow-its-caster", () => TestDraftEnhancementsFollowTheCaster(root));
            Run("out-of-combat-setting-is-saved", () => TestOutOfCombatSettingSaved(root));
            Run("footer-counts-only-this-routines-one-pass-shortfalls", () => TestFooterCountsOnlyShortfalls(root));
            Run("reload-without-a-plan-file-never-blocks-or-discards", () => TestReloadWithoutPlanFile(root));
            Run("classic-screen-policy-closes-once-and-scopes-results", TestClassicRunScreenPolicy);
            Run("finite-pool-zero-cost-stays-an-unknown-cost", TestFinitePoolZeroCostStaysUnknown);
            Run("an-interrupted-run-says-whether-it-cast", TestInterruptedRunWording);
            Run("imported-grouping-unknown-becomes-single-target", () => TestImportedGroupingUnknownBecomesSingleTarget(root));
            Run("first-open-import-reads-the-rebound-classic-plan", () => TestImportFromReboundClassicPlan(root));
            Run("classic-file-and-hud-stay-with-their-mode-and-campaign", TestClassicSaveAndHudScoping);
            Run("native-boundary-refuses-non-standard-or-tampered", TestNativeBoundaryRefusals);
            Run("execution-host-runs-reports-and-halts", TestExecutionHostRunsAndHalts);
            Run("execution-host-cancel-deadline-shutdown", TestExecutionHostCancelDeadlineShutdown);
            Run("production-apply-end-to-end-through-host",
                () => TestProductionApplyEndToEnd(root));
            Run("run-presentation-separates-effects-and-spending", TestRunPresentation);
            Run("inspection-and-fixture-family-requests",
                () => TestInspectionAndFixtureFamilyRequests(root));
            Run("card-discloses-limits-and-existing-effects",
                () => TestCardDisclosesLimitsAndExistingEffects(root));
            Run("refusal-feedback-rules", () => TestRefusalFeedbackRules(root));
            Run("host-stops-between-castings", TestHostStopsBetweenCastings);
            Run("host-player-stop-finishes-cast-in-progress", TestHostPlayerStopIsGraceful);
            Run("exhausted-rod-waived-by-active-effect", TestExhaustedRodWithActiveEffect);
            Run("probe-refuses-a-cast-while-the-world-is-held", TestProbeWorldHeldViolation);
            Run("probe-owner-pumps-only-while-the-world-runs", TestProbeOwnerPumpsOnlyWhileTheWorldRuns);
            Run("casting-world-clock-keeps-fractions-and-skips-held-time", TestCastingWorldClock);
            Run("buff-grid-source-type-tabs", () => TestBuffGridSourceTypeTabs(root));
            Run("player-facing-resource-labels", () => TestPlayerFacingResourceLabels(root));
            Run("in-game-reload-is-guarded", TestInGameReloadIsGuarded);
            Run("in-game-first-open-import-is-judged", TestInGameFirstOpenImportIsJudged);
            Run("card-reasons-and-review-items-in-words", TestCardReasonsInWords);
            Run("player-facing-refusals-and-routine-header", () => TestPlayerFacingRefusalsAndHeader(root));
            Run("buff-grid-is-alphabetical", () => TestBuffGridIsAlphabetical(root));
            Run("qualification-allowance-parsing", TestQualificationAllowanceParsing);
            Run("qualification-recipe-selection", TestQualificationRecipeSelection);
            Run("qualification-forecast-and-boundary", TestQualificationForecastAndBoundary);
            Run("qualification-driver-end-to-end", () => TestQualificationDriverEndToEnd(root));
            Run("qualification-animated-player-stop", () => TestQualificationAnimatedPlayerStop(root));
            Run("qualification-on-the-planner-pumped-host", () => TestQualificationOnOwnerPumpedHost(root));
            Run("qualification-disable-step-rules", () => TestQualificationDisableStepRules(root));
            Run("cantrips-cast-at-will-through-the-class-ability", TestCantripsCastAtWill);
            Run("at-will-cantrip-choice-refuses-what-is-not-the-authored-cantrip", TestAtWillCantripChoice);
            Run("classic-run-advances-only-while-the-world-runs", TestWorldGatedClassicRun);
            Run("live-run-stops-at-its-overall-deadline-or-abort-marker", TestLiveRunStopRule);
            Run("confirmation-needs-an-instance-this-attempt-applied", TestAppliedEffectJudgement);
            Run("cantrip-route-follows-the-reservation-and-ambiguity-stays-unresolved", TestCantripRouteAndPricing);
            Run("fact-source-choice-keeps-the-provider-kind-and-reserved-pool", TestFactSourceChoice);
            Run("available-count-judgement-never-reads-unread-as-unlimited", TestAvailableCountJudgement);
            Run("capability-inventory-describes-the-party", TestCapabilityInventory);
            Run("classic-cast-grant-digest-allowance-and-judgement", TestClassicCastCore);
            Run("classic-runs-halt-after-a-failure-and-keep-cleanup", TestClassicHaltingRunner);
            Run("read-only-game-diagnostics-never-act", TestReadOnlyGameDiagnostics);
            Run("physical-workspace-requests-and-judgement", () => TestPhysicalWorkspace(root));
            Run("persistence-round-trip-gaps-and-campaign-isolation",
                () => TestPersistenceRoundTripAndCampaignIsolation(root));
            Run("qualification-finite-recipe", () => TestFiniteQualificationRecipe(root));
            Run("qualification-group-mixed-recipe", () => TestGroupQualificationRecipe(root));
            Run("qualification-driver-refusals-and-deadline",
                () => TestQualificationDriverRefusalsAndDeadline(root));
            Run("qualification-scenario-requests", () => TestQualificationScenarioRequests(root));
            Run("classic-cast-scenario-requests-and-host-order", () => TestClassicScenarioRequests(root));
            // Last: it takes the process-wide runtime-test lock.
            Run("production-execution-wiring-and-session-lock", TestProductionExecutionWiring);
        }

        // A caster whose provider carries a known caster level (10) and a
        // trustworthy per-level duration (1 hour/level = 6000 rounds).
        private static PartyProviderSnapshot LiveEffectParty(int remaining,
            out List<ProviderPlanningOption> options)
        {
            List<UnitSnapshot> units = new[] { "unit-cleric", "unit-t1", "unit-t2", "unit-t3" }
                .Select(id => new UnitSnapshot(id, id, false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))).ToList();
            var provider = new ProviderSnapshot(
                new ProviderKey("unit-cleric", "book-unit-cleric", CastingBuffAbility, "level-2"),
                "Fixture Buff", 2, "pool-unit-cleric", 1, null, null, 10, 6000,
                string.Empty, "1 hour/level");
            var pools = new[]
            {
                new ResourcePoolSnapshot("pool-unit-cleric", ResourcePoolKind.SpontaneousLevel,
                    remaining, remaining, null)
            };
            options = new List<ProviderPlanningOption>
            {
                new ProviderPlanningOption(provider, units.Select(unit => unit.UnitId).ToList(),
                    new[] { "unit-cleric" }, 10, 100)
            };
            return new PartyProviderSnapshot(units, new[] { provider }, pools);
        }

        private static ActiveEffectSnapshot LiveEffects(
            params KeyValuePair<string, ActiveEffectInstance>[] instances)
        {
            var byUnit = new Dictionary<string, IEnumerable<ActiveEffectInstance>>(
                StringComparer.Ordinal);
            foreach (IGrouping<string, KeyValuePair<string, ActiveEffectInstance>> group in
                instances.GroupBy(pair => pair.Key, StringComparer.Ordinal))
                byUnit[group.Key] = group.Select(pair => pair.Value).ToList();
            return ActiveEffectSnapshot.FromInstances(byUnit);
        }

        private static KeyValuePair<string, ActiveEffectInstance> On(string unitId,
            string effectId, double? remainingRounds, int? casterLevel, int? metamagic,
            bool suppressed = false, EffectKind kind = EffectKind.Buff)
        {
            return new KeyValuePair<string, ActiveEffectInstance>(unitId,
                new ActiveEffectInstance(kind, effectId, remainingRounds, casterLevel,
                    metamagic, suppressed));
        }

        // Legacy parity plus the distinctions presence alone cannot make:
        // weaker, differently enhanced, nearly expired, suppressed or a
        // different effect never counts as already satisfied.
        private static void TestExistingEffectSufficiencyRules()
        {
            EffectExpression buff = Leaf("buff-effect");
            var none = new HashSet<string>(StringComparer.Ordinal);
            var plain = new ExistingEffectRequirement(10, 0, 6000, true);
            Func<ExistingEffectRequirement, EffectExpression, ActiveEffectInstance[], ExistingEffectRecipientAssessment> assess =
                (requirement, expression, instances) => ExistingEffectSufficiency.Assess(
                    "unit-t1", expression, instances, none, requirement);
            Func<double?, int?, int?, ActiveEffectInstance> instance = (remaining, level, metamagic) =>
                new ActiveEffectInstance(EffectKind.Buff, "buff-effect", remaining, level, metamagic);
            if (assess(plain, buff, new[] { instance(5000, 10, 0) }).Verdict !=
                    ExistingEffectVerdict.Sufficient)
                throw new InvalidOperationException("An equal, long-lived effect was not sufficient.");
            ExistingEffectRecipientAssessment weaker = assess(plain, buff, new[] { instance(5000, 5, 0) });
            if (weaker.Verdict != ExistingEffectVerdict.Insufficient ||
                !weaker.Reasons.Any(reason => reason.StartsWith("weaker-caster-level:5<10",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("A lower caster level counted as satisfied.");
            // Review of rc4: what cannot be read never proves sufficiency -
            // an unreadable caster level, an unknown planned caster level or
            // an unreadable suppression flag keeps the casting's step.
            ExistingEffectRecipientAssessment unknownLevel = assess(plain, buff,
                new[] { instance(5000, null, 0) });
            if (unknownLevel.Verdict != ExistingEffectVerdict.Insufficient ||
                !unknownLevel.Reasons.Contains("caster-level-unverified:buff-effect"))
                throw new InvalidOperationException("An unreadable caster level proved an effect sufficient.");
            ExistingEffectRecipientAssessment plannedUnknown = assess(
                new ExistingEffectRequirement(0, 0, 6000, true), buff, new[] { instance(5000, 10, 0) });
            if (plannedUnknown.Verdict != ExistingEffectVerdict.Insufficient ||
                !plannedUnknown.Reasons.Contains("caster-level-unverified:planned:buff-effect"))
                throw new InvalidOperationException("An unknown planned caster level proved an effect sufficient.");
            var unreadableSuppression = new ActiveEffectInstance(EffectKind.Buff, "buff-effect", 5000, 10, 0,
                false, false);
            ExistingEffectRecipientAssessment unreadable = assess(plain, buff, new[] { unreadableSuppression });
            if (unreadable.Verdict != ExistingEffectVerdict.Insufficient ||
                !unreadable.Reasons.Contains("suppression-unreadable:buff-effect"))
                throw new InvalidOperationException("An unreadable suppression flag proved an effect sufficient.");
            if (assess(plain, buff, new[] { unreadableSuppression, instance(5000, 10, 0) }).Verdict !=
                    ExistingEffectVerdict.Sufficient)
                throw new InvalidOperationException("A readable sufficient instance was hidden by an unreadable one.");
            if (CastingRunPresentation.DescribeExistingEffectNote(
                    "existing-insufficient:unit-t1:caster-level-unverified:buff-effect") !=
                    "present on unit-t1 but not provably as strong (will recast)" ||
                CastingRunPresentation.DescribeExistingEffectNote(
                    "existing-insufficient:unit-t1:suppression-unreadable:buff-effect") !=
                    "present on unit-t1 but possibly suppressed (will recast)")
                throw new InvalidOperationException("Unreadable existing-effect details are not described.");
            ExistingEffectRecipientAssessment shortLeft = assess(plain, buff,
                new[] { instance(2000, 10, 0) });
            if (shortLeft.Verdict != ExistingEffectVerdict.Insufficient ||
                !shortLeft.Reasons.Any(reason => reason.StartsWith("remaining-duration-short:2000<3000",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("A nearly expired effect counted as satisfied.");
            if (assess(plain, buff, new[] { instance(null, 10, 0) }).Verdict !=
                    ExistingEffectVerdict.Sufficient)
                throw new InvalidOperationException("A permanent effect was not sufficient.");
            var uncomparable = new ExistingEffectRequirement(10, 0, 6000, false);
            ExistingEffectRecipientAssessment unknownDuration = assess(uncomparable, buff,
                new[] { instance(10, 10, 0) });
            if (unknownDuration.Verdict != ExistingEffectVerdict.Sufficient ||
                !unknownDuration.Reasons.Contains("remaining-duration-not-compared"))
                throw new InvalidOperationException(
                    "An uncomparable duration was not disclosed as presence-only.");
            // The absolute floor applies whatever the duration comparability
            // (fixed or localized durations included): under two rounds left
            // is about to expire.
            foreach (ExistingEffectRequirement requirement in new[] { plain, uncomparable })
                foreach (double left in new[] { 0d, 1d, 1.9d })
                {
                    ExistingEffectRecipientAssessment expiring = assess(requirement, buff,
                        new[] { instance(left, 10, 0) });
                    string expected = "expiring:" + Math.Floor(left).ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + "<2";
                    if (expiring.Verdict != ExistingEffectVerdict.Insufficient ||
                        !expiring.Reasons.Any(reason => reason.Contains(expected)))
                        throw new InvalidOperationException("An effect with " + left +
                            " rounds left counted as satisfied: " +
                            string.Join(",", expiring.Reasons.ToArray()));
                }
            if (assess(uncomparable, buff, new[] { instance(2, 10, 0) }).Verdict !=
                    ExistingEffectVerdict.Sufficient)
                throw new InvalidOperationException("The expiry floor is not exactly two rounds.");
            // Extend doubles the expected duration; Quicken does not make an
            // instance weaker.
            var extended = new ExistingEffectRequirement(10, 8 | 4, 6000, true);
            if (assess(extended, buff, new[] { instance(7000, 10, 8) }).Verdict !=
                    ExistingEffectVerdict.Sufficient)
                throw new InvalidOperationException("An extended instance without Quicken was refused.");
            if (assess(extended, buff, new[] { instance(5000, 10, 8) }).Verdict !=
                    ExistingEffectVerdict.Insufficient)
                throw new InvalidOperationException("Extend did not double the expected duration.");
            var empowered = new ExistingEffectRequirement(10, 1, 6000, true);
            ExistingEffectRecipientAssessment missing = assess(empowered, buff,
                new[] { instance(5000, 10, 0) });
            if (missing.Verdict != ExistingEffectVerdict.Insufficient ||
                !missing.Reasons.Any(reason => reason.StartsWith("missing-metamagic:1",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("A missing strength metamagic was satisfied.");
            if (!assess(empowered, buff, new[] { instance(5000, 10, null) }).Reasons
                    .Any(reason => reason.StartsWith("metamagic-unverified:1", StringComparison.Ordinal)))
                throw new InvalidOperationException("Unknown metamagic was assumed present.");
            var unprovable = new ExistingEffectRequirement(10, 0, 6000, true, "class-feature:x");
            if (!assess(unprovable, buff, new[] { instance(5000, 10, 0) }).Reasons
                    .Any(reason => reason.StartsWith("equivalence-unproven:class-feature:x",
                        StringComparison.Ordinal)))
                throw new InvalidOperationException("An unmodeled enhancement inherited satisfaction.");
            if (assess(plain, buff, new[]
                {
                    new ActiveEffectInstance(EffectKind.Buff, "buff-effect", 5000, 10, 0, true)
                }).Verdict != ExistingEffectVerdict.NotPresent)
                throw new InvalidOperationException("A suppressed instance counted as present.");
            if (assess(plain, buff, new[]
                {
                    new ActiveEffectInstance(EffectKind.Buff, "other-effect", 5000, 10, 0)
                }).Verdict != ExistingEffectVerdict.NotPresent)
                throw new InvalidOperationException("A different effect counted as present.");
            // Every leaf must be sufficient: one weaker leaf keeps it casting.
            var pair = new SequenceEffectExpression(new EffectExpression[]
                { Leaf("leaf-a"), Leaf("leaf-b") });
            Func<string, int, ActiveEffectInstance> leaf = (id, level) =>
                new ActiveEffectInstance(EffectKind.Buff, id, 5000, level, 0);
            if (assess(plain, pair, new[] { leaf("leaf-a", 10), leaf("leaf-b", 10) }).Verdict !=
                    ExistingEffectVerdict.Sufficient ||
                assess(plain, pair, new[] { leaf("leaf-a", 10), leaf("leaf-b", 3) }).Verdict !=
                    ExistingEffectVerdict.Insufficient ||
                assess(plain, pair, new[] { leaf("leaf-a", 10) }).Verdict !=
                    ExistingEffectVerdict.NotPresent)
                throw new InvalidOperationException("Multi-effect sufficiency was not all-of.");
            if (!ExistingEffectSufficiency.IsPerLevelDuration("1 hour/level") ||
                !ExistingEffectSufficiency.IsPerLevelDuration("10 minutes per level") ||
                !ExistingEffectSufficiency.IsPerLevelDuration("1 min./level") ||
                ExistingEffectSufficiency.IsPerLevelDuration("24 hours") ||
                ExistingEffectSufficiency.IsPerLevelDuration(string.Empty))
                throw new InvalidOperationException("Per-level duration detection is wrong.");
        }

        // The compiler applies the live policy per casting: an already
        // sufficient effect skips (no reservation, would-be cost kept), even
        // when the pool is exhausted; an insufficient one casts with the
        // reason disclosed; always-recast casts; structural blocks and
        // drafts are never overridden by live effects.
        private static void TestLiveExistingEffectSkipInCompiler()
        {
            var compiler = new ExplicitCastingCompiler();
            Dictionary<string, EffectExpression> effects =
                CastingEffects("source-bulls", "source-communal");
            Func<int, CastingPlanDocument, ActiveEffectSnapshot, ExplicitCastingPlan> compile =
                (remaining, document, live) =>
                {
                    List<ProviderPlanningOption> options;
                    PartyProviderSnapshot snapshot = LiveEffectParty(remaining, out options);
                    return compiler.Compile(document, snapshot, options, effects, null, "long",
                        null, false, live);
                };
            CastingPlanDocument direct = CastingDocument(DirectCasting("cast-1", "long",
                "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility));
            ResolvedCasting plain = compile(3, direct, null).CastingById("cast-1");
            if (plain.Readiness != ResolvedCastingReadiness.Ready ||
                plain.ExistingEffectNotes.Count != 0)
                throw new InvalidOperationException("Without live effects the casting changed.");
            ActiveEffectSnapshot sufficient = LiveEffects(On("unit-t1", "buff-effect", 5000, 10, 0));
            ExplicitCastingPlan skipped = compile(3, direct, sufficient);
            ResolvedCasting satisfied = skipped.CastingById("cast-1");
            if (satisfied.Readiness != ResolvedCastingReadiness.AlreadySatisfied ||
                !satisfied.ReadinessReasons.SequenceEqual(new[] { "already-active:unit-t1" }) ||
                !satisfied.ExistingEffectNotes.Contains("already-active:unit-t1") ||
                satisfied.Cost.Count != 0 ||
                !satisfied.CostShape.SequenceEqual(new[] { "NativePool:pool-unit-cleric:1" }) ||
                skipped.BudgetLines.Any(line => line.AllocatedUsage != 0))
                throw new InvalidOperationException("A sufficient live effect did not skip cleanly: " +
                    string.Join(",", satisfied.ReadinessReasons.ToArray()));
            ResolvedCasting weaker = compile(3, direct,
                LiveEffects(On("unit-t1", "buff-effect", 5000, 4, 0))).CastingById("cast-1");
            if (weaker.Readiness != ResolvedCastingReadiness.Ready ||
                !weaker.ExistingEffectNotes.Any(note => note.StartsWith(
                    "existing-insufficient:unit-t1:weaker-caster-level:4<10", StringComparison.Ordinal)))
                throw new InvalidOperationException("A weaker live effect did not cast with disclosure.");
            // Repeat use after the routine ran: pool spent, effect active.
            ResolvedCasting spentButActive = compile(0, direct, sufficient).CastingById("cast-1");
            if (spentButActive.Readiness != ResolvedCastingReadiness.AlreadySatisfied ||
                !spentButActive.ReadinessReasons.SequenceEqual(new[] { "already-active:unit-t1" }))
                throw new InvalidOperationException(
                    "An exhausted pool blocked a casting whose effect is active: " +
                    string.Join(",", spentButActive.ReadinessReasons.ToArray()));
            ResolvedCasting spentAndAbsent = compile(0, direct,
                LiveEffects()).CastingById("cast-1");
            if (spentAndAbsent.Readiness != ResolvedCastingReadiness.Blocked ||
                spentAndAbsent.ReadinessReasons.Count != 1 ||
                !spentAndAbsent.ReadinessReasons[0].StartsWith("resource-pool-exhausted",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("An exhausted pool without the effect did not block.");
            CastingPlanDocument recast = CastingDocument(DirectCasting("cast-1", "long",
                "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility, null, null,
                CastingAuthoringState.Ready, ExistingEffectPolicy.Overwrite));
            ResolvedCasting always = compile(3, recast, sufficient).CastingById("cast-1");
            if (always.Readiness != ResolvedCastingReadiness.Ready ||
                !always.ExistingEffectNotes.Contains("existing-active-recast:unit-t1"))
                throw new InvalidOperationException("Always-recast did not cast with disclosure.");
            CastingPlanDocument ghost = CastingDocument(DirectCasting("cast-1", "long",
                "unit-cleric", "unit-ghost", "source-bulls", CastingBuffAbility));
            if (compile(3, ghost, LiveEffects(On("unit-ghost", "buff-effect", 5000, 10, 0)))
                    .CastingById("cast-1").Readiness != ResolvedCastingReadiness.Blocked)
                throw new InvalidOperationException("A live effect overrode a structural block.");
            // A required enhancement that is unavailable keeps the resolved
            // source but blocks; an active effect never waives it.
            CastingPlanDocument missingRod = CastingDocument(DirectCasting("cast-1", "long",
                "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility,
                new[] { new AuthoredEnhancementSelection("missing-rod", true, null) }));
            ResolvedCasting rodBlocked = compile(3, missingRod, sufficient).CastingById("cast-1");
            if (rodBlocked.Readiness != ResolvedCastingReadiness.Blocked ||
                !rodBlocked.ReadinessReasons.Any(reason => reason.StartsWith(
                    "enhancement-unavailable:missing-rod", StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "A live effect waived a required enhancement: " +
                    string.Join(",", rodBlocked.ReadinessReasons.ToArray()));
            CastingPlanDocument draft = CastingDocument(DirectCasting("cast-1", "long",
                "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility, null, null,
                CastingAuthoringState.Draft));
            if (compile(3, draft, sufficient).CastingById("cast-1").Readiness !=
                    ResolvedCastingReadiness.Draft)
                throw new InvalidOperationException("A live effect resolved a saved draft.");
            // The gate omits the skipped casting without blocking and without
            // anything to execute.
            CastingApplyDecision decision = new CastingExecutionGate().Evaluate(
                skipped, CastingApplyMode.Ordinary, "long");
            if (!decision.Allowed || decision.ExecutableCastingIds.Count != 0 ||
                decision.Omissions.Count != 1 ||
                !decision.Omissions[0].Reasons.Contains("already-active:unit-t1"))
                throw new InvalidOperationException("The gate misreported an already-active casting.");
        }

        // A group casting is satisfied only when every INTENDED recipient
        // (the required coverage, or all predicted beneficiaries when none
        // is required) already has the effect.
        private static void TestGroupExistingEffectRecipients()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(CastingGroupAbility,
                out options, out enhancements, new[] { "unit-t1", "unit-t2", "unit-t3" }, 3,
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
                {
                    { "unit-cleric", new[] { "unit-t1", "unit-t2", "unit-t3" } }
                });
            Dictionary<string, EffectExpression> effects =
                CastingEffects("source-bulls", "source-communal");
            var compiler = new ExplicitCastingCompiler();
            Func<IEnumerable<string>, ActiveEffectSnapshot, ResolvedCasting> compile =
                (required, live) => compiler.Compile(CastingDocument(GroupCasting("cast-g",
                        "long", "unit-cleric", "source-communal", CastingGroupAbility, required)),
                    snapshot, options, effects, enhancements, "long", null, false, live)
                    .CastingById("cast-g");
            Func<string, KeyValuePair<string, ActiveEffectInstance>> group = unit =>
                On(unit, "group-effect", null, 1, null);
            ResolvedCasting requiredMet = compile(new[] { "unit-t1", "unit-t2" },
                LiveEffects(group("unit-t1"), group("unit-t2")));
            if (requiredMet.Readiness != ResolvedCastingReadiness.AlreadySatisfied)
                throw new InvalidOperationException("Required coverage already active did not skip: " +
                    string.Join(",", requiredMet.ReadinessReasons.ToArray()));
            if (compile(new[] { "unit-t1", "unit-t2" }, LiveEffects(group("unit-t1")))
                    .Readiness != ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException("A missing required recipient did not cast.");
            ResolvedCasting partialBeneficiaries = compile(new string[0],
                LiveEffects(group("unit-t1"), group("unit-t2")));
            if (partialBeneficiaries.Readiness != ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "Without required coverage, an uncovered beneficiary was ignored.");
            var everyone = partialBeneficiaries.PredictedBeneficiaryUnitIds
                .Select(unit => group(unit)).ToArray();
            if (everyone.Length == 0 ||
                compile(new string[0], LiveEffects(everyone)).Readiness !=
                    ResolvedCastingReadiness.AlreadySatisfied)
                throw new InvalidOperationException("Every beneficiary active did not skip.");
        }

        // Mixed coverage (owner, 2026-09-24): one recipient already has an
        // adequate, longer-lasting instance while the others lack it. Under
        // the default skip-if-active the group casting still casts once (one
        // record, one invocation, one unit of cost), names the covered
        // recipient for its step, and confirmation accepts that recipient's
        // kept coverage while every other recipient needs a new or
        // refreshed instance. Under always-recast nothing is exempt, and an
        // unreadable detail never makes a recipient pre-covered.
        private static void TestMixedCoverageGroupCasting()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(CastingGroupAbility,
                out options, out enhancements, new[] { "unit-t1", "unit-t2", "unit-t3" }, 3,
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
                {
                    { "unit-cleric", new[] { "unit-t1", "unit-t2", "unit-t3" } }
                });
            Dictionary<string, EffectExpression> effects =
                CastingEffects("source-bulls", "source-communal");
            var compiler = new ExplicitCastingCompiler();
            Func<ExistingEffectPolicy, ActiveEffectSnapshot, ExplicitCastingPlan> compile =
                (policy, live) => compiler.Compile(CastingDocument(new PlannedCasting(
                        "cast-g", "long", 0, "source-communal", CastingGroupAbility, "unit-cleric", null,
                        CastingTargetMode.CasterCenteredOrigin, null, CastingOrigin.CasterCentered(),
                        new string[0], null, null, policy, null, CastingAuthoringState.Ready, null)),
                    snapshot, options, effects, enhancements, "long", null, false, live);
            Func<ExplicitCastingPlan, CastStep> step = plan =>
            {
                ExplicitStepConversion conversion = ExplicitCastingStepConverter.Convert(plan,
                    new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long"),
                    options, effects);
                if (!conversion.Converted || conversion.Plan.Steps.Count != 1)
                    throw new InvalidOperationException("The group casting did not convert to one step: " +
                        conversion.Refusal);
                return conversion.Plan.Steps[0];
            };
            // unit-t1 holds an adequate instance: readable caster level, no
            // expiry, so it outlasts the planned casting.
            ActiveEffectSnapshot mixed = LiveEffects(On("unit-t1", "group-effect", null, 1, 0));
            ResolvedCasting casting = compile(ExistingEffectPolicy.SkipAlreadyActive, mixed)
                .CastingById("cast-g");
            if (casting.Readiness != ResolvedCastingReadiness.Ready ||
                !casting.PreCoveredUnitIds.SequenceEqual(new[] { "unit-t1" }) ||
                !casting.ExistingEffectNotes.Any(note =>
                    note.StartsWith("already-covered:unit-t1", StringComparison.Ordinal)) ||
                casting.ExistingEffectNotes.Any(note =>
                    note.StartsWith("already-active:", StringComparison.Ordinal)) ||
                casting.Cost.Count(line => line.Category == CastingCostCategory.NativePool) != 1 ||
                casting.Cost.Single(line => line.Category == CastingCostCategory.NativePool).Units != 1)
                throw new InvalidOperationException("The mixed-coverage casting is not one covered-aware cast: " +
                    casting.Readiness + "|" + string.Join(",", casting.PreCoveredUnitIds.ToArray()) + "|" +
                    string.Join(",", casting.ExistingEffectNotes.ToArray()));
            if (CastingRunPresentation.DescribeExistingEffectNote("already-covered:unit-t1") !=
                    "already active on unit-t1 (the cast goes ahead for the others)")
                throw new InvalidOperationException("The covered recipient is not described.");
            CastStep mixedStep = step(compile(ExistingEffectPolicy.SkipAlreadyActive, mixed));
            ExplicitCastingPlan plainPlan = compile(ExistingEffectPolicy.SkipAlreadyActive, LiveEffects());
            ExplicitStepConversion plain = ExplicitCastingStepConverter.Convert(plainPlan,
                new CastingExecutionGate().Evaluate(plainPlan, CastingApplyMode.Ordinary, "long"),
                options, effects);
            ExplicitCastingPlan mixedPlan = compile(ExistingEffectPolicy.SkipAlreadyActive, mixed);
            ExplicitStepConversion mixedConversion = ExplicitCastingStepConverter.Convert(mixedPlan,
                new CastingExecutionGate().Evaluate(mixedPlan, CastingApplyMode.Ordinary, "long"),
                options, effects);
            if (!mixedStep.MassCast || !mixedStep.PreCoveredRecipientUnitIds.SequenceEqual(new[] { "unit-t1" }) ||
                !mixedStep.ExpectedRecipientUnitIds.Contains("unit-t1") ||
                !mixedStep.ExpectedRecipientUnitIds.Contains("unit-t2") ||
                mixedStep.Reservation.Units != 1 || !plain.Converted ||
                plain.Plan.Steps[0].PreCoveredRecipientUnitIds.Count != 0 ||
                plain.CanonicalContract.Contains("preCoveredRecipientUnitIds") ||
                !mixedConversion.CanonicalContract.Contains("\"preCoveredRecipientUnitIds\":[\"unit-t1\"]") ||
                mixedConversion.ProjectionId == plain.ProjectionId)
                throw new InvalidOperationException("The step does not carry the covered recipient in its identity.");

            // Confirmation: the kept coverage of unit-t1 and a new instance on
            // every other recipient.
            EffectExpression expected = effects["source-communal"];
            Func<string, long, ObservedEffectInstance> seen = (key, end) =>
                new ObservedEffectInstance(EffectKind.Buff, "group-effect", key, end, false);
            IReadOnlyList<string> recipients = mixedStep.ExpectedRecipientUnitIds;
            Func<string, IEnumerable<ObservedEffectInstance>, EffectBaseline> baselineWith = (t1, t1Before) =>
                new EffectBaseline(recipients.ToDictionary(unit => unit,
                    unit => unit == t1 ? t1Before : (IEnumerable<ObservedEffectInstance>)new ObservedEffectInstance[0],
                    StringComparer.Ordinal));
            EffectBaseline baseline = baselineWith("unit-t1", new[] { seen("old", 9000) });
            Func<IEnumerable<ObservedEffectInstance>, string, IEnumerable<ObservedEffectInstance>,
                Func<string, IEnumerable<ObservedEffectInstance>>> afterWith = (t1After, missing, missingAfter) =>
                    unit => unit == "unit-t1" ? t1After
                        : unit == missing ? missingAfter
                        : new[] { seen("new-" + unit, 600) };
            IEnumerable<ObservedEffectInstance> kept = new[] { seen("old", 9000) };
            IEnumerable<ObservedEffectInstance> none = new ObservedEffectInstance[0];
            IReadOnlyList<string> covered = mixedStep.PreCoveredRecipientUnitIds;
            if (!AppliedEffectJudgement.AllReached(recipients, expected, baseline,
                    afterWith(kept, null, null), covered))
                throw new InvalidOperationException("Kept coverage on the covered recipient was not accepted.");
            if (AppliedEffectJudgement.AllReached(recipients, expected, baseline,
                    afterWith(kept, null, null)))
                throw new InvalidOperationException("Without the plan's proof, an unchanged instance confirmed.");
            if (AppliedEffectJudgement.AllReached(recipients, expected, baseline,
                    afterWith(kept, "unit-t2", none), covered))
                throw new InvalidOperationException("A recipient that lacked the effect was not required.");
            if (AppliedEffectJudgement.AllReached(recipients, expected, baseline,
                    afterWith(none, null, null), covered))
                throw new InvalidOperationException("Coverage lost during the cast was accepted.");
            if (AppliedEffectJudgement.AllReached(recipients, expected, baseline,
                    afterWith(new[] { new ObservedEffectInstance(EffectKind.Buff, "group-effect", "old", 9000, true) },
                        null, null), covered))
                throw new InvalidOperationException("A suppressed kept instance was accepted.");
            if (!AppliedEffectJudgement.AllReached(recipients, expected, baseline,
                    afterWith(new[] { seen("replaced", 600) }, null, null), covered))
                throw new InvalidOperationException("A replaced instance on the covered recipient was refused.");
            // Coverage gone before the cast (expired since planning): the
            // recipient needs a new instance like any other.
            EffectBaseline expiredBaseline = baselineWith("unit-t1", none);
            if (AppliedEffectJudgement.AllReached(recipients, expected, expiredBaseline,
                    afterWith(none, null, null), covered) ||
                !AppliedEffectJudgement.AllReached(recipients, expected, expiredBaseline,
                    afterWith(new[] { seen("fresh", 600) }, null, null), covered))
                throw new InvalidOperationException("An expired covered recipient was not treated as uncovered.");
            if (AppliedEffectJudgement.AllReached(recipients, expected, baseline,
                    unit => new[] { seen("old", 9000) }, recipients))
                throw new InvalidOperationException("A cast that delivered to no one was confirmed.");

            // Always recast: every recipient must be reached; nothing is exempt.
            ResolvedCasting recast = compile(ExistingEffectPolicy.Overwrite, mixed).CastingById("cast-g");
            if (recast.Readiness != ResolvedCastingReadiness.Ready || recast.PreCoveredUnitIds.Count != 0 ||
                !recast.ExistingEffectNotes.Any(note =>
                    note.StartsWith("existing-active-recast:unit-t1", StringComparison.Ordinal)) ||
                step(compile(ExistingEffectPolicy.Overwrite, mixed)).PreCoveredRecipientUnitIds.Count != 0)
                throw new InvalidOperationException("Always-recast exempted a recipient.");
            // An unreadable caster level never proves the recipient covered.
            ResolvedCasting unreadable = compile(ExistingEffectPolicy.SkipAlreadyActive,
                LiveEffects(On("unit-t1", "group-effect", null, null, 0))).CastingById("cast-g");
            if (unreadable.Readiness != ResolvedCastingReadiness.Ready || unreadable.PreCoveredUnitIds.Count != 0 ||
                !unreadable.ExistingEffectNotes.Any(note =>
                    note.StartsWith("existing-insufficient:unit-t1", StringComparison.Ordinal) &&
                    note.Contains("caster-level-unverified")))
                throw new InvalidOperationException("An unreadable caster level made a recipient pre-covered.");
            // Every recipient covered: one skip, no step.
            ActiveEffectSnapshot all = LiveEffects(casting.PredictedBeneficiaryUnitIds
                .Select(unit => On(unit, "group-effect", null, 1, 0)).ToArray());
            if (compile(ExistingEffectPolicy.SkipAlreadyActive, all).CastingById("cast-g").Readiness !=
                    ResolvedCastingReadiness.AlreadySatisfied)
                throw new InvalidOperationException("A fully covered group casting did not skip.");
        }

        // A skip that flips with live effects is a harmless refresh; what a
        // casting would do or spend, its policy, and a block stay material;
        // a routine scope signs only its own castings.
        private static void TestReviewSignatureSkipFlipIsHarmless()
        {
            var compiler = new ExplicitCastingCompiler();
            Dictionary<string, EffectExpression> effects =
                CastingEffects("source-bulls", "source-communal");
            Func<int, CastingPlanDocument, ActiveEffectSnapshot, ExplicitCastingPlan> compile =
                (remaining, planDocument, live) =>
                {
                    List<ProviderPlanningOption> options;
                    PartyProviderSnapshot snapshot = LiveEffectParty(remaining, out options);
                    return compiler.Compile(planDocument, snapshot, options, effects, null, "long",
                        null, false, live);
                };
            CastingPlanDocument document = CastingDocument(DirectCasting("cast-1", "long",
                "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility));
            ActiveEffectSnapshot active = LiveEffects(On("unit-t1", "buff-effect", 5000, 10, 0));
            CastingPlanSignature ready = CastingPlanSignature.For(compile(3, document, null), "long");
            CastingPlanSignature skipped = CastingPlanSignature.For(compile(3, document, active), "long");
            CastingPlanSignature spentSkipped = CastingPlanSignature.For(compile(0, document, active), "long");
            if (!ready.Matches(skipped) || !ready.Matches(spentSkipped) ||
                ready.Digest != skipped.Digest)
                throw new InvalidOperationException("A live-effect skip demanded re-review.");
            CastingPlanSignature blocked = CastingPlanSignature.For(
                compile(0, document, LiveEffects()), "long");
            if (ready.Matches(blocked))
                throw new InvalidOperationException("A newly blocked casting was not material.");
            CastingPlanDocument overwrite = CastingDocument(DirectCasting("cast-1", "long",
                "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility, null, null,
                CastingAuthoringState.Ready, ExistingEffectPolicy.Overwrite));
            if (ready.Matches(CastingPlanSignature.For(compile(3, overwrite, null), "long")))
                throw new InvalidOperationException("An existing-effect policy change was not material.");
            CastingPlanDocument withShort = CastingDocument(
                DirectCasting("cast-1", "long", "unit-cleric", "unit-t1", "source-bulls",
                    CastingBuffAbility),
                DirectCasting("cast-2", "short", "unit-cleric", "unit-t2", "source-bulls",
                    CastingBuffAbility));
            ExplicitCastingPlan shortPlan = compile(3, withShort, null);
            if (!ready.Matches(CastingPlanSignature.For(shortPlan, "long")) ||
                CastingPlanSignature.For(compile(3, document, null))
                    .Matches(CastingPlanSignature.For(shortPlan)))
                throw new InvalidOperationException("Routine scoping of the signature is wrong.");
            if (ready.Digest.Length != 64 || ready.Digest.Any(c => char.IsUpper(c)))
                throw new InvalidOperationException("The signature digest is not lower-case SHA-256.");
        }

        // Review state per routine: accepting Long survives reviewing Short;
        // new material clears only its own routine; a restored digest
        // authorizes exactly matching contents and nothing else.
        private static void TestReviewStatePerRoutine()
        {
            var long1 = new CastingPlanSignature("long-contents-1");
            var long2 = new CastingPlanSignature("long-contents-2");
            var short1 = new CastingPlanSignature("short-contents-1");
            var review = new CastingReviewCoordinator();
            review.Present("long", long1);
            if (!review.Accept("long", long1).Allowed)
                throw new InvalidOperationException("Presented contents could not be accepted.");
            review.Present("short", short1);
            if (review.StatusFor("long") != CastingReviewStatus.Accepted ||
                !review.TrySubmit("long", long1).Allowed ||
                review.StatusFor("short") != CastingReviewStatus.Presented ||
                review.TrySubmit("short", short1).Reason != "not-accepted")
                throw new InvalidOperationException("Reviewing Short disturbed Long.");
            review.Present("long", long1);
            if (review.StatusFor("long") != CastingReviewStatus.Accepted)
                throw new InvalidOperationException("A harmless refresh cleared acceptance.");
            // New material needs its own acceptance, and showing it does not
            // revoke the old one: that still authorizes exactly its digest.
            review.Present("long", long2);
            if (review.StatusFor("long") != CastingReviewStatus.Presented ||
                review.TrySubmit("long", long2).Reason != "material-change-requires-review" ||
                !review.TrySubmit("long", long1).Allowed)
                throw new InvalidOperationException("New material was authorized or revoked the old acceptance.");
            var restored = new CastingReviewCoordinator();
            restored.RestoreAccepted("long", long1.Digest);
            if (!restored.TrySubmit("long", long1).Allowed ||
                restored.TrySubmit("long", long2).Reason != "material-change-requires-review")
                throw new InvalidOperationException("A restored acceptance matched the wrong contents.");
            restored.Present("long", long1);
            if (restored.StatusFor("long") != CastingReviewStatus.Accepted)
                throw new InvalidOperationException("Presenting the restored contents lost acceptance.");
            restored.Present("long", long2);
            if (restored.StatusFor("long") != CastingReviewStatus.Presented ||
                restored.TrySubmit("long", long2).Allowed || !restored.TrySubmit("long", long1).Allowed)
                throw new InvalidOperationException("A restored acceptance authorized new contents or was revoked.");
            var invalid = new CastingReviewCoordinator();
            invalid.RestoreAccepted("long", long1.Digest.ToUpperInvariant());
            invalid.RestoreAccepted("short", "abc");
            if (invalid.TrySubmit("long", long1).Reason != "nothing-presented" ||
                invalid.AcceptedDigests.Count != 0)
                throw new InvalidOperationException("An invalid restored digest was accepted.");
            // The standing the HUD may state without recomputing the plan.
            var standing = new CastingReviewCoordinator();
            if (standing.StandingFor("long") != CastingAcceptanceStanding.None)
                throw new InvalidOperationException("Nothing accepted was not None.");
            standing.RestoreAccepted("long", long1.Digest);
            if (standing.StandingFor("long") != CastingAcceptanceStanding.OnFile)
                throw new InvalidOperationException("A restored acceptance was not on file.");
            standing.Present("long", long1);
            if (standing.StandingFor("long") != CastingAcceptanceStanding.Current)
                throw new InvalidOperationException("Matching contents were not current.");
            standing.Present("long", long2);
            if (standing.StandingFor("long") != CastingAcceptanceStanding.Changed ||
                !standing.TrySubmit("long", long1).Allowed)
                throw new InvalidOperationException("Different contents were not reported as changed.");
            standing.Present("long", long1);
            if (standing.StandingFor("long") != CastingAcceptanceStanding.Current)
                throw new InvalidOperationException("A temporary difference lost the acceptance.");
            // The unscoped calls keep their single-scope behaviour.
            var legacy = new CastingReviewCoordinator();
            legacy.Present(long1);
            if (!legacy.Accept(long1).Allowed || !legacy.TrySubmit(long1).Allowed ||
                legacy.Status != CastingReviewStatus.Accepted ||
                legacy.StatusFor("long") != CastingReviewStatus.NothingPresented)
                throw new InvalidOperationException("The unscoped review contract changed.");
        }

        private static void TestReviewStore(string root)
        {
            string dir = Path.Combine(root, "review-store");
            Directory.CreateDirectory(dir);
            var store = new CastingReviewStore(dir);
            if (store.Load("campaign-a").AcceptedDigests.Count != 0)
                throw new InvalidOperationException("An absent review file was not empty.");
            string digest = new CastingPlanSignature("contents").Digest;
            store.Save("campaign-a", new Dictionary<string, string> { { "long", digest } });
            CastingReviewStoreLoad loaded = store.Load("campaign-a");
            if (loaded.Warning.Length != 0 || loaded.AcceptedDigests.Count != 1 ||
                loaded.AcceptedDigests["long"] != digest)
                throw new InvalidOperationException("The review state did not round-trip.");
            if (store.Load("campaign-b").AcceptedDigests.Count != 0)
                throw new InvalidOperationException("Review state leaked across campaigns.");
            string path = store.PathFor("campaign-a");
            JObject wrong = JObject.Parse(File.ReadAllText(path));
            wrong["campaignId"] = "campaign-other";
            File.WriteAllText(path, wrong.ToString());
            CastingReviewStoreLoad mismatch = store.Load("campaign-a");
            if (mismatch.AcceptedDigests.Count != 0 ||
                mismatch.Warning != "review-state-ignored:campaign-id-mismatch")
                throw new InvalidOperationException("A mismatched review file was honoured.");
            File.WriteAllText(path, "{ not json");
            CastingReviewStoreLoad corrupt = store.Load("campaign-a");
            if (corrupt.AcceptedDigests.Count != 0 ||
                !corrupt.Warning.StartsWith("review-state-ignored:unreadable", StringComparison.Ordinal))
                throw new InvalidOperationException("A corrupt review file was honoured.");
            File.WriteAllText(path, "{\"schemaVersion\":1,\"campaignId\":\"campaign-a\"," +
                "\"accepted\":{\"long\":\"XYZ\"}}");
            if (store.Load("campaign-a").AcceptedDigests.Count != 0)
                throw new InvalidOperationException("An invalid digest was honoured.");
            // Batch 3, section 11: a file the store refuses to read (newer
            // schema, another campaign, unreadable, invalid) is never
            // replaced by a save; its bytes stay exactly as they were.
            foreach (string content in new[]
                {
                    "{ not json",
                    "{\"schemaVersion\":2,\"campaignId\":\"campaign-a\",\"accepted\":{}}",
                    "{\"schemaVersion\":1,\"campaignId\":\"campaign-other\",\"accepted\":{}}",
                    "{\"schemaVersion\":1,\"campaignId\":\"campaign-a\",\"accepted\":{\"long\":\"XYZ\"}}"
                })
            {
                File.WriteAllText(path, content);
                byte[] before = File.ReadAllBytes(path);
                bool kept = false;
                try { store.Save("campaign-a", new Dictionary<string, string> { { "long", digest } }); }
                catch (InvalidOperationException exception)
                {
                    kept = exception.Message.StartsWith("review-state-file-protected:review-state-ignored:",
                        StringComparison.Ordinal);
                }
                if (!kept || !File.ReadAllBytes(path).SequenceEqual(before))
                    throw new InvalidOperationException("A review file the store refuses was replaced: " + content);
            }
            File.Delete(path);
            store.Save("campaign-a", new Dictionary<string, string> { { "long", digest } });
            if (store.Load("campaign-a").AcceptedDigests.Count != 1)
                throw new InvalidOperationException("A save after removing the protected file failed.");
            File.WriteAllText(path, "{\"schemaVersion\":1,\"campaignId\":\"campaign-a\"," +
                "\"accepted\":{\"long\":\"XYZ\"}}");
            bool refused = false;
            try { store.Save("campaign-a", new Dictionary<string, string> { { "long", "bad" } }); }
            catch (ArgumentException) { refused = true; }
            if (!refused) throw new InvalidOperationException("An invalid digest was written.");
        }

        private static void TestPlannerModeStore(string root)
        {
            string dir = Path.Combine(root, "planner-mode");
            Directory.CreateDirectory(dir);
            var store = new PlannerModeStore(dir);
            string warning;
            if (store.Load(out warning) != PlannerMode.Classic || warning.Length != 0)
                throw new InvalidOperationException("An absent mode file was not Classic.");
            store.Save(PlannerMode.CastingFirst);
            if (store.Load(out warning) != PlannerMode.CastingFirst || warning.Length != 0)
                throw new InvalidOperationException("The casting-first mode did not round-trip.");
            if (!File.ReadAllText(store.FilePath).Contains("\"casting-first\""))
                throw new InvalidOperationException("The mode file is not the documented format.");
            store.Save(PlannerMode.Classic);
            if (store.Load(out warning) != PlannerMode.Classic)
                throw new InvalidOperationException("Classic did not round-trip.");
            File.WriteAllText(store.FilePath, "{\"schemaVersion\":1,\"mode\":\"turbo\"}");
            if (store.Load(out warning) != PlannerMode.Classic ||
                warning != "planner-mode-ignored:unknown-mode")
                throw new InvalidOperationException("An unknown mode activated something.");
            File.WriteAllText(store.FilePath, "{\"schemaVersion\":2,\"mode\":\"casting-first\"}");
            if (store.Load(out warning) != PlannerMode.Classic ||
                warning != "planner-mode-ignored:schema-version")
                throw new InvalidOperationException("A newer mode file activated casting-first.");
            File.WriteAllText(store.FilePath, "garbage");
            if (store.Load(out warning) != PlannerMode.Classic ||
                !warning.StartsWith("planner-mode-ignored:unreadable", StringComparison.Ordinal))
                throw new InvalidOperationException("A corrupt mode file activated casting-first.");
            // Batch 3, section 11: the toggle never replaces a mode file the
            // store refuses to read; it reports why and keeps the bytes.
            foreach (string content in new[]
                {
                    "garbage", "{\"schemaVersion\":2,\"mode\":\"casting-first\"}",
                    "{\"schemaVersion\":1,\"mode\":\"turbo\"}"
                })
            {
                File.WriteAllText(store.FilePath, content);
                byte[] before = File.ReadAllBytes(store.FilePath);
                string message = null;
                try { store.Save(PlannerMode.CastingFirst); }
                catch (InvalidOperationException exception) { message = exception.Message; }
                if (message == null || !message.StartsWith("planner-mode.json was left unchanged (planner-mode-ignored:",
                        StringComparison.Ordinal) || !File.ReadAllBytes(store.FilePath).SequenceEqual(before))
                    throw new InvalidOperationException("A mode file the store refuses was replaced: " + content);
            }
            File.Delete(store.FilePath);
            store.Save(PlannerMode.CastingFirst);
            if (store.Load(out warning) != PlannerMode.CastingFirst || warning.Length != 0)
                throw new InvalidOperationException("A save after removing the protected mode file failed.");
        }

        private static AuthoringEditResult AddDraftCasting(CastingWorkspaceSession session,
            CastingWorkspaceInputs inputs, string casterUnitId, string targetUnitId)
        {
            session.SelectBuff("source-bulls");
            session.SelectRoutine("long");
            session.BuildView(inputs);
            session.Draft.SourceId = "source-bulls";
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.ChooseDraftCaster(casterUnitId);
            session.Draft.DirectTargetUnitId = targetUnitId;
            session.Draft.State = CastingAuthoringState.Ready;
            return session.AddCastingFromDraft(inputs);
        }

        // Save keeps the loaded (or imported) player settings instead of
        // resetting them; a settings change is dirty and saved on request.
        private static void TestSessionSavePreservesSettings(string root)
        {
            string dir = Path.Combine(root, "settings-preserved");
            Directory.CreateDirectory(dir);
            var repository = new CastingPlanRepository(dir);
            repository.Save(CastingPlanProfile.FromDocument(
                new CastingPlanDocument("workspace-campaign", new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                }, new PlannedCasting[0]),
                new UiProfile { Scale = 1.25f, Hotkey = "Ctrl+Shift+P" },
                new ExecutionProfile
                {
                    Mode = "instant", AllowAnimatedFallback = false,
                    OutOfCombatOnly = true, RecastExisting = true
                }));
            var session = new CastingWorkspaceSession(dir, "workspace-campaign");
            if (session.ExecutionMode != "instant" || session.AllowAnimatedFallback ||
                session.IsDirty)
                throw new InvalidOperationException("Loaded execution settings were not adopted.");
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
            session.Save();
            CastingPlanProfile saved = repository.Load("workspace-campaign").Profile;
            if (saved.Castings.Count != 1 || saved.Execution.Mode != "instant" ||
                saved.Execution.AllowAnimatedFallback || !saved.Execution.RecastExisting ||
                !saved.Execution.OutOfCombatOnly || Math.Abs(saved.Ui.Scale - 1.25f) > 0.001f ||
                saved.Ui.Hotkey != "Ctrl+Shift+P")
                throw new InvalidOperationException("Save reset the player settings.");
            session.SetExecutionMode("animated");
            if (!session.IsDirty)
                throw new InvalidOperationException("A settings change was not dirty.");
            session.Save();
            if (repository.Load("workspace-campaign").Profile.Execution.Mode != "animated" ||
                session.IsDirty)
                throw new InvalidOperationException("A settings change was not saved.");
            bool refused = false;
            try { session.SetExecutionMode("bogus"); }
            catch (ArgumentException) { refused = true; }
            if (!refused) throw new InvalidOperationException("An unknown execution mode was set.");
            string freshDir = Path.Combine(root, "settings-default");
            Directory.CreateDirectory(freshDir);
            var fresh = new CastingWorkspaceSession(freshDir, "workspace-campaign");
            if (fresh.ExecutionMode != "animated" || !fresh.AllowAnimatedFallback)
                throw new InvalidOperationException("A new plan did not start from the defaults.");
        }

        // An accepted, unchanged plan stays accepted across close/reopen
        // (so a quick-run after reloading needs no ceremony); a material
        // change refuses, and presenting it clears the stored acceptance.
        private static void TestSessionAcceptanceSurvivesReopen(string root)
        {
            string dir = Path.Combine(root, "acceptance-reopen");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var first = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            Assert(AddDraftCasting(first, inputs, "unit-cleric", "unit-t1").Applied);
            first.Save();
            first.PresentForReview(inputs);
            Assert(first.AcceptPresentedPlan(inputs));
            var second = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            if (second.ReviewStatusFor("long") != CastingReviewStatus.Accepted)
                throw new InvalidOperationException("The accepted state was not restored.");
            WorkspaceApplyResult reopened = second.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (!reopened.ReviewReason.StartsWith("native-submission-disabled", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "An unchanged accepted plan needed re-review after reopening: " +
                    reopened.ReviewReason);
            Assert(AddDraftCasting(second, inputs, "unit-wizard", "unit-t2").Applied);
            WorkspaceApplyResult changed = second.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (changed.Allowed || changed.ReviewReason != "material-change-requires-review")
                throw new InvalidOperationException("A changed plan rode a restored acceptance.");
            // Presenting the unsaved change in another session neither
            // revokes nor rewrites the stored acceptance: a reopened session
            // with the accepted plan on disk still runs it.
            second.PresentForReview(inputs);
            var third = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            if (!third.Apply(CastingApplyMode.Ordinary, "long", inputs).ReviewReason
                    .StartsWith("native-submission-disabled", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Presenting other material revoked the stored acceptance.");
            // A review file from a newer planner: the session ignores it,
            // an acceptance holds for this session only, and the file keeps
            // its bytes (the save is refused and reported).
            string newerDir = Path.Combine(root, "acceptance-newer-review");
            Directory.CreateDirectory(newerDir);
            string newerPath = new CastingReviewStore(newerDir).PathFor("workspace-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(newerPath));
            File.WriteAllText(newerPath, "{\"schemaVersion\":2,\"campaignId\":\"workspace-campaign\"}");
            byte[] newerBytes = File.ReadAllBytes(newerPath);
            var newer = new CastingWorkspaceSession(newerDir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            Assert(AddDraftCasting(newer, inputs, "unit-cleric", "unit-t1").Applied);
            newer.Save();
            newer.PresentForReview(inputs);
            Assert(newer.AcceptPresentedPlan(inputs));
            if (!newer.ReviewStoreWarning.StartsWith(
                    "review-state-save-failed:InvalidOperationException:review-state-file-protected:",
                    StringComparison.Ordinal) ||
                PersistenceMessages.ForReviewWarning(newer.ReviewStoreWarning) == null ||
                !PersistenceMessages.ForReviewWarning(newer.ReviewStoreWarning).StartsWith(
                    "Plan accepted for this session only", StringComparison.Ordinal) ||
                !File.ReadAllBytes(newerPath).SequenceEqual(newerBytes) ||
                !newer.Apply(CastingApplyMode.Ordinary, "long", inputs).ReviewReason
                    .StartsWith("native-submission-disabled", StringComparison.Ordinal))
                throw new InvalidOperationException("A newer review file was replaced or the acceptance lost: " +
                    newer.ReviewStoreWarning);
            // Once the protected file is gone, a save succeeds and the stale
            // warning is cleared.
            File.Delete(newerPath);
            Assert(newer.AcceptPresentedPlan(inputs));
            if (newer.ReviewStoreWarning.Length != 0 || PersistenceMessages.ForReviewWarning(newer.ReviewStoreWarning) != null)
                throw new InvalidOperationException("A saved review kept a stale warning: " + newer.ReviewStoreWarning);
            // The workspace tells the player: Save and Accept show the words.
            DirectoryInfo sourceRoot = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (sourceRoot != null && !File.Exists(Path.Combine(sourceRoot.FullName, "KingmakerBuffPlanner.sln")))
                sourceRoot = sourceRoot.Parent;
            string workspaceView = File.ReadAllText(Path.Combine(sourceRoot.FullName, "src", "KingmakerBuffPlanner",
                "UI", "CastingWorkspaceScreenView.cs"));
            if (!workspaceView.Contains("_footerResult.text = PersistenceMessages.ForSaveFailure(exception);") ||
                !workspaceView.Contains("PersistenceMessages.ForReviewWarning(_session.ReviewStoreWarning)"))
                throw new InvalidOperationException("The workspace does not show persistence refusals.");
        }

        // Final review B4: both planners tell the player when their saved file
        // could not be used or a change was not saved, and tell an
        // unreadable file apart from none at all.
        private static void TestPersistenceNoticesShown(string root)
        {
            string dir = Path.Combine(root, "persistence-notices");
            Directory.CreateDirectory(dir);
            var profiles = new ProfileRepository(dir);
            ProfileLoadResult absent = profiles.Load("notice-campaign");
            string primaryName = Path.GetFileName(profiles.GetProfilePath("notice-campaign"));
            if (PersistenceMessages.ForClassicLoad(absent.SourcePath, absent.RecoveredFromBackup,
                    absent.Warning, primaryName) != null)
                throw new InvalidOperationException("A missing Classic setup was reported as unreadable.");
            string primary = profiles.GetProfilePath("notice-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(primary));
            File.WriteAllText(primary, "{ not json");
            ProfileLoadResult unreadable = profiles.Load("notice-campaign");
            string loadNotice = PersistenceMessages.ForClassicLoad(unreadable.SourcePath,
                unreadable.RecoveredFromBackup, unreadable.Warning, primaryName);
            profiles.Save(unreadable.Profile);
            string saveNotice = PersistenceMessages.ForClassicSaveRefusal(profiles.LastSaveRefusal);
            if (loadNotice == null || !loadNotice.StartsWith("Your saved planner setup could not be read",
                    StringComparison.Ordinal) || saveNotice == null ||
                !saveNotice.StartsWith("Not saved:", StringComparison.Ordinal) ||
                File.ReadAllText(primary) != "{ not json")
                throw new InvalidOperationException("An unreadable Classic setup or refused save was not reported.");
            string recoveredNotice = PersistenceMessages.ForClassicLoad("primary.json.bak1", true,
                "primary.json: bad", "primary.json");
            if (recoveredNotice == null || !recoveredNotice.Contains("changes are not saved") ||
                PersistenceMessages.ForClassicLoad("primary.json", false, string.Empty, "primary.json") != null ||
                PersistenceMessages.ForClassicSaveRefusal(null) != null)
                throw new InvalidOperationException("Classic load notices misjudged a backup or a clean load.");
            // Re-review: only an unreadable primary refuses saves; a missing
            // one is written again, and its notice says so.
            string missingNotice = PersistenceMessages.ForClassicLoad("primary.json.bak1", true, string.Empty,
                "primary.json");
            string badBackupsNotice = PersistenceMessages.ForClassicLoad(string.Empty, false,
                "primary.json.bak1: bad", "primary.json");
            if (missingNotice != "Your saved planner setup file was missing, so its latest readable backup was loaded." ||
                badBackupsNotice == null || !badBackupsNotice.Contains("Saving works") ||
                PersistenceMessages.ClassicPrimaryUnreadable("primary.json.bak1: bad", "primary.json") ||
                !PersistenceMessages.ClassicPrimaryUnreadable("primary.json: bad", "primary.json"))
                throw new InvalidOperationException("A missing Classic file was reported as refusing saves: " +
                    missingNotice + "|" + badBackupsNotice);
            string missingDir = Path.Combine(root, "pn-missing");
            Directory.CreateDirectory(missingDir);
            var missingProfiles = new ProfileRepository(missingDir);
            string missingPath = missingProfiles.GetProfilePath("notice-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(missingPath));
            File.WriteAllText(missingPath + ".bak1", "{ not json");
            ProfileLoadResult onlyBadBackup = missingProfiles.Load("notice-campaign");
            if (PersistenceMessages.ForClassicLoad(onlyBadBackup.SourcePath, onlyBadBackup.RecoveredFromBackup,
                    onlyBadBackup.Warning, Path.GetFileName(missingPath)) != badBackupsNotice ||
                PersistenceMessages.ClassicPrimaryUnreadable(onlyBadBackup.Warning, Path.GetFileName(missingPath)))
                throw new InvalidOperationException("A missing Classic file with a bad backup claimed saves are refused.");
            missingProfiles.Save(onlyBadBackup.Profile);
            if (missingProfiles.LastSaveRefusal != null || !File.Exists(missingPath))
                throw new InvalidOperationException("A missing Classic file was not written again.");
            File.Delete(missingPath + ".bak1");
            File.Move(missingPath, missingPath + ".bak1");
            ProfileLoadResult goodBackup = missingProfiles.Load("notice-campaign");
            if (!goodBackup.RecoveredFromBackup || PersistenceMessages.ForClassicLoad(goodBackup.SourcePath,
                    goodBackup.RecoveredFromBackup, goodBackup.Warning, Path.GetFileName(missingPath)) != missingNotice)
                throw new InvalidOperationException("A backup loaded for a missing Classic file was not announced as such.");
            if (PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.Corrupt, true) == null ||
                PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.UnsupportedSchema, true) == null ||
                PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.RecoveredFromBackup, true) == null ||
                PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.Loaded, true) != null ||
                PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.Absent, false) != null)
                throw new InvalidOperationException("Casting-first load notices misjudged a status.");
            // Re-review: the notice names the remedy that works (the file and
            // its backups moved aside, then Reload), and a backup loaded for a
            // missing file does not claim saves are refused.
            if (!PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.Corrupt, true).Contains(
                    "move that file out of UserSettings, then press Reload") ||
                !PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.RecoveredFromBackup, true).Contains(
                    "saving is refused") ||
                PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.RecoveredFromBackup, false) !=
                    "Your casting plan file was missing, so its latest backup was loaded.")
                throw new InvalidOperationException("Casting-first notices name the wrong remedy or claim.");
            // Focused re-review: the files the plan could not use are named,
            // and only those are to be moved.
            string newerNotice = PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.UnsupportedSchema, true,
                "C:\\x\\UserSettings\\plan.json.bak1", string.Empty);
            string corruptNotice = PersistenceMessages.ForCastingLoad(CastingPlanLoadStatus.Corrupt, true,
                "C:\\x\\plan.json", "plan.json:invalid:x | plan.json.bak2:schema-version-missing");
            string newerDir = Path.Combine(root, "pn-newer");
            string newerPlan = new CastingPlanRepository(newerDir).GetProfilePath("notice-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(newerPlan));
            File.WriteAllText(newerPlan, "{ not json");
            File.WriteAllText(newerPlan + ".bak1", "{\"schemaVersion\":99}");
            CastingPlanLoadResult newerLoad = new CastingPlanRepository(newerDir).Load("notice-campaign");
            string bothFiles = PersistenceMessages.UnusableCastingFiles(newerLoad.Status, newerLoad.SourcePath,
                newerLoad.Warning);
            if (newerLoad.Status != CastingPlanLoadStatus.UnsupportedSchema ||
                bothFiles != Path.GetFileName(newerPlan) + ", " + Path.GetFileName(newerPlan) + ".bak1")
                throw new InvalidOperationException("A newer plan behind an unreadable one did not name both: " + bothFiles);
            if (!newerNotice.Contains("(plan.json.bak1)") || !corruptNotice.Contains("(plan.json, plan.json.bak2)") ||
                !corruptNotice.Contains("move those files out of UserSettings"))
                throw new InvalidOperationException("The casting notices do not name the files to move: " + corruptNotice);
            if (!PersistenceMessages.ForSaveFailure(new InvalidOperationException(
                    "Candidate persistence is blocked: Corrupt x")).EndsWith("then press Reload.", StringComparison.Ordinal) ||
                !PersistenceMessages.ForSaveFailure(new InvalidOperationException(
                    "Candidate persistence is blocked: legacy-import x")).StartsWith(
                    "Not saved: your classic plan could not be imported", StringComparison.Ordinal))
                throw new InvalidOperationException("A blocked Save is not told in the player's words.");
            // A casting-first plan that cannot be read opens blocked, and its
            // notice is what the view shows first.
            string castingDir = Path.Combine(dir, "casting");
            Directory.CreateDirectory(castingDir);
            string plan = new CastingPlanRepository(castingDir).GetProfilePath("notice-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(plan));
            File.WriteAllText(plan, "{ not json");
            var blocked = new CastingWorkspaceSession(castingDir, "notice-campaign");
            if (!blocked.PersistenceBlocked || PersistenceMessages.ForCastingLoad(blocked.LoadStatus,
                    blocked.PrimaryPlanFileExists) == null)
                throw new InvalidOperationException("An unreadable casting plan opened without a notice: " +
                    blocked.LoadStatus);
            // Re-review: the remedy works in the same session - with the
            // unreadable file moved aside, Reload unblocks it and Save writes
            // the plan (and the status is current after the save).
            File.Delete(plan);
            CastingPlanLoadStatus reloadedStatus = blocked.Reload();
            blocked.Save();
            if (reloadedStatus != CastingPlanLoadStatus.Absent || blocked.PersistenceBlocked ||
                !File.Exists(plan) || blocked.LoadStatus != CastingPlanLoadStatus.Loaded)
                throw new InvalidOperationException("Moving the unreadable plan aside did not unblock the session: " +
                    reloadedStatus);
            // Castings authored while the plan was blocked are kept, shown as
            // unsaved, and written by the next Save.
            string keptDir = Path.Combine(root, "pn-kept");
            Directory.CreateDirectory(keptDir);
            string keptPlan = new CastingPlanRepository(keptDir).GetProfilePath("workspace-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(keptPlan));
            File.WriteAllText(keptPlan, "{ not json");
            PartyProviderSnapshot keptSnapshot;
            CastingWorkspaceInputs keptInputs = WorkspaceInputs(out keptSnapshot);
            var kept = new CastingWorkspaceSession(keptDir, "workspace-campaign", new DisabledCastingDispatchBoundary());
            Assert(kept.PersistenceBlocked);
            Assert(AddDraftCasting(kept, keptInputs, "unit-cleric", "unit-t1").Applied);
            File.Delete(keptPlan);
            kept.Reload();
            if (kept.PersistenceBlocked || kept.Document.Castings.Count != 1 || !kept.IsDirty)
                throw new InvalidOperationException("Castings authored while the plan was blocked were lost or looked saved.");
            kept.Save();
            if (new CastingWorkspaceSession(keptDir, "workspace-campaign").Document.Castings.Count != 1)
                throw new InvalidOperationException("The kept casting was not saved.");
            // A backup loaded because the primary is missing saves normally;
            // the save makes the primary current.
            string backupDir = Path.Combine(root, "pn-bak");
            Directory.CreateDirectory(backupDir);
            var first = new CastingWorkspaceSession(backupDir, "workspace-campaign", new DisabledCastingDispatchBoundary());
            Assert(AddDraftCasting(first, keptInputs, "unit-cleric", "unit-t1").Applied);
            first.Save();
            first.Save();
            string backupPlan = new CastingPlanRepository(backupDir).GetProfilePath("workspace-campaign");
            File.Delete(backupPlan);
            var fromBackup = new CastingWorkspaceSession(backupDir, "workspace-campaign", new DisabledCastingDispatchBoundary());
            if (fromBackup.LoadStatus != CastingPlanLoadStatus.RecoveredFromBackup || fromBackup.PrimaryPlanFileExists ||
                PersistenceMessages.ForCastingLoad(fromBackup.LoadStatus, fromBackup.PrimaryPlanFileExists) !=
                    "Your casting plan file was missing, so its latest backup was loaded.")
                throw new InvalidOperationException("A backup loaded for a missing plan was announced wrongly: " +
                    fromBackup.LoadStatus);
            if (fromBackup.SavesRefused)
                throw new InvalidOperationException("A backup loaded for a missing plan was shown as not saving.");
            fromBackup.Save();
            if (fromBackup.LoadStatus != CastingPlanLoadStatus.Loaded || !fromBackup.PrimaryPlanFileExists ||
                PersistenceMessages.ForCastingLoad(fromBackup.LoadStatus, fromBackup.PrimaryPlanFileExists) != null)
                throw new InvalidOperationException("The load status stayed stale after a save.");
            // Focused re-review: a backup loaded because the main file cannot
            // be read shows that saving is refused while that file is there.
            first.Save();
            File.WriteAllText(backupPlan, "{ not json");
            var refusedBackup = new CastingWorkspaceSession(backupDir, "workspace-campaign", new DisabledCastingDispatchBoundary());
            if (refusedBackup.LoadStatus != CastingPlanLoadStatus.RecoveredFromBackup || !refusedBackup.SavesRefused)
                throw new InvalidOperationException("A backup loaded for an unreadable main file did not show that saving is refused.");
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            Func<string, string> source = name => File.ReadAllText(Path.Combine(directory.FullName, "src",
                "KingmakerBuffPlanner", "UI", name)).Replace("\r\n", "\n");
            string session = source("PlannerUiSession.cs");
            string workspaceSource = source("CastingWorkspaceScreenView.cs");
            string castingSession = source("CastingWorkspaceSession.cs");
            if (!session.Contains("string primaryName = System.IO.Path.GetFileName(_profiles.GetProfilePath(campaignId));\n" +
                    "                PersistenceNotice = PersistenceMessages.ForClassicLoad(loaded.SourcePath,\n" +
                    "                    loaded.RecoveredFromBackup, loaded.Warning, primaryName);") ||
                !session.Contains("PersistenceNotice = PersistenceMessages.ForClassicSaveRefusal(refusal);") ||
                !session.Contains("                PersistenceNotice = null;\n                ClassicSavesRefused = false;\n") ||
                !source("BuffPlannerScreenView.cs").Contains("string notice = _session.PersistenceNotice;") ||
                !workspaceSource.Contains("string import = PersistenceMessages.ForCastingLoad(_session.LoadStatus,\n" +
                    "                    _session.PrimaryPlanFileExists, _session.LoadSourcePath, _session.LoadWarning) ??") ||
                !workspaceSource.Contains(": PersistenceMessages.ForCastingLoad(status, _session.PrimaryPlanFileExists,\n" +
                    "                            _session.LoadSourcePath, _session.LoadWarning) ??") ||
                !workspaceSource.Contains("if (_session.IsDirty && Time.unscaledTime > _reloadArmedUntil)") ||
                !source("BuffPlannerScreenView.cs").Contains("if (_session.ClassicSavesRefused) _status.text += \" | changes are not saved\";") ||
                !workspaceSource.Contains("                (!_session.SavesRefused ? string.Empty\n" +
                    "                    : _session.LegacyImportBlocked ? \" · not saved: the classic plan could not be imported\"\n" +
                    "                    : \" · not saved: the plan file cannot be read\");") ||
                !castingSession.Contains("\"Candidate persistence is blocked: \" + (LegacyImportBlocked ? \"legacy-import\" : LoadStatus.ToString()) +"))
                throw new InvalidOperationException("A planner does not show its persistence notice.");
        }

        // Final review B2: an imported casting whose old plan let the planner
        // pick any caster becomes Ready in place, through the controls the
        // focused editor shows: pick who casts it, resolve the review, mark
        // it Ready. Its id and its import provenance are kept.
        private static void TestImportedAutomaticCastingReadyInPlace(string root)
        {
            string dir = Path.Combine(root, "imported-automatic-ready");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var imported = new PlannedCasting("cast-auto", "long", 0, "source-bulls", CastingBuffAbility,
                null, null, CastingTargetMode.DirectTarget, "unit-t1", null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Draft,
                new MigrationProvenance("cast-auto", 5, "long", string.Empty, "unit-t1",
                    new[] { "automatic-caster-pending-review" }, null));
            new CastingPlanRepository(dir).Save(CastingPlanProfile.FromDocument(new CastingPlanDocument(
                "workspace-campaign", new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                }, new[] { imported })));
            var session = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            session.FocusCasting("cast-auto");
            WorkspaceView view = session.BuildView(inputs);
            WorkspaceProviderChoice cleric = view.FocusedProviders.FirstOrDefault(value =>
                value.CasterUnitId == "unit-cleric");
            if (view.FocusedProviders.Count != 2 || cleric == null ||
                view.FocusedProviders.Any(value => value.Selected))
                throw new InvalidOperationException("The focused editor did not offer the capable casters: " +
                    string.Join("|", view.FocusedProviders.Select(value => value.Label).ToArray()));
            if (session.SetFocusedCastingState(CastingAuthoringState.Ready).Applied)
                throw new InvalidOperationException("An imported casting without a caster was marked Ready.");
            AuthoringEditResult chosen = session.SetFocusedProvider(cleric.ProviderKey, inputs);
            AuthoringEditResult resolved = session.ResolveFocusedImportReview();
            AuthoringEditResult ready = session.SetFocusedCastingState(CastingAuthoringState.Ready);
            PlannedCasting after = session.Document.Castings.Single();
            ResolvedCasting compiled = session.CompilePlan(inputs).CastingById("cast-auto");
            if (!chosen.Applied || !resolved.Applied || !ready.Applied || after.CastingId != "cast-auto" ||
                after.CasterUnitId != "unit-cleric" || after.Provenance == null ||
                after.Provenance.UnresolvedReviewItems.Count != 0 || compiled == null || !compiled.IsExecutable ||
                !session.BuildView(inputs).FocusedProviders.Single(value =>
                    value.CasterUnitId == "unit-cleric").Selected)
                throw new InvalidOperationException("The imported casting did not become Ready in place: " +
                    chosen.Reason + "|" + resolved.Reason + "|" + ready.Reason + "|" + (compiled == null ? "none"
                        : string.Join(",", compiled.ReadinessReasons.ToArray())));
        }

        // Final review B3: a caster with the same buff in two spellbooks picks
        // the exact one. Add without a pick is refused with a reason that
        // points at the picker; the focused casting can switch books.
        private static void TestExactProviderPicker(string root)
        {
            string dir = Path.Combine(root, "exact-provider-picker");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot baseSnapshot;
            CastingWorkspaceInputs baseInputs = WorkspaceInputs(out baseSnapshot);
            ProviderPlanningOption firstBook = baseInputs.ProviderOptions.Single(value =>
                value.Provider.Key.CasterUnitId == "unit-cleric");
            var secondBook = new ProviderSnapshot(new ProviderKey("unit-cleric", "book-cleric-second",
                    CastingBuffAbility, "level-2"), CastingBuffAbility.BaseAbilityGuid, 2,
                firstBook.Provider.ResourcePoolKey, 1, null, null, 12);
            var snapshot = new PartyProviderSnapshot(baseSnapshot.Units,
                baseSnapshot.Providers.Concat(new[] { secondBook }).ToList(), baseSnapshot.ResourcePools);
            var inputs = new CastingWorkspaceInputs(snapshot, baseInputs.ProviderOptions.Concat(new[]
                {
                    new ProviderPlanningOption(secondBook, firstBook.ReachableTargetIds,
                        firstBook.LegalAnchorIds, 12, 100)
                }).ToList(), baseInputs.EffectsBySource, baseInputs.Enhancements);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            AuthoringEditResult ambiguous = AddDraftCasting(session, inputs, "unit-cleric", "unit-t1");
            if (ambiguous.Applied || !ambiguous.Reason.StartsWith(
                    "draft-ability-unresolved:exact-source-ambiguous", StringComparison.Ordinal) ||
                WorkspaceRefusalText.Describe(ambiguous.Reason) !=
                    "that character can cast it in more than one way; pick one under Cast from.")
                throw new InvalidOperationException("An ambiguous Add was not refused towards the picker: " +
                    ambiguous.Reason);
            WorkspaceView view = session.BuildView(inputs);
            if (view.DraftProviders.Count != 2 ||
                view.DraftProviders.Select(value => value.Label).Distinct(StringComparer.Ordinal).Count() != 2 ||
                view.DraftProviders.Any(value => value.CasterUnitId != "unit-cleric"))
                throw new InvalidOperationException("The draft did not offer each of the caster's sources: " +
                    string.Join("|", view.DraftProviders.Select(value => value.Label).ToArray()));
            WorkspaceProviderChoice second = view.DraftProviders.Single(value =>
                value.ProviderKey.Contains("book-cleric-second"));
            Assert(session.ChooseDraftProvider(second.ProviderKey, inputs).Applied);
            List<WorkspaceProviderChoice> picked = session.BuildView(inputs).DraftProviders.ToList();
            if (picked.Count(value => value.Selected) != 1 ||
                !picked.Single(value => value.Selected).ProviderKey.Contains("book-cleric-second"))
                throw new InvalidOperationException("The picked source is not the one shown selected.");
            AuthoringEditResult added = session.AddCastingFromDraft(inputs);
            PlannedCasting casting = session.Document.Castings.SingleOrDefault();
            if (!added.Applied || casting == null || casting.SpellbookGuid != "book-cleric-second" ||
                !session.CompilePlan(inputs).CastingById(casting.CastingId).IsExecutable)
                throw new InvalidOperationException("The picked source was not the one authored: " + added.Reason);
            session.FocusCasting(casting.CastingId);
            // Re-review: the same caster keeps its enhancements when it
            // switches books - also one not discovered right now (a rod
            // briefly out of its pack stays visible as unavailable).
            Assert(session.UpdateFocusedCasting(session.Document.Castings.Single().WithEnhancementSelections(new[]
            {
                new AuthoredEnhancementSelection("extend-cleric", false, null),
                new AuthoredEnhancementSelection("rod-not-discovered-now", false, null)
            })).Applied);
            WorkspaceProviderChoice other = session.BuildView(inputs).FocusedProviders.Single(value =>
                value.CasterUnitId == "unit-cleric" && !value.ProviderKey.Contains("book-cleric-second"));
            AuthoringEditResult otherBook = session.SetFocusedProvider(other.ProviderKey, inputs);
            if (!otherBook.Applied || otherBook.Reason.Length != 0 ||
                session.Document.Castings.Single().Enhancements.Count != 2)
                throw new InvalidOperationException("Switching books dropped the caster's own enhancement: " +
                    otherBook.Reason);
            Assert(session.UpdateFocusedCasting(session.Document.Castings.Single().WithEnhancementSelections(
                new AuthoredEnhancementSelection[0])).Applied);
            if (session.Document.Castings.Single().SpellbookGuid != firstBook.Provider.Key.SpellbookGuid ||
                session.SetFocusedProvider(other.ProviderKey, inputs).Applied ||
                !session.CompilePlan(inputs).CastingById(casting.CastingId).IsExecutable)
                throw new InvalidOperationException("The focused casting did not switch books exactly once.");
        }

        // Final review B2: the focused casting moves between routines and
        // within its own, and the recast choice (the focused casting's and
        // the next casting's) is kept. The view wires every control to these
        // session commands.
        private static void TestFocusedRoutineOrderAndRecast(string root)
        {
            string dir = Path.Combine(root, "focused-routine-order-recast");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
            Assert(AddDraftCasting(session, inputs, "unit-wizard", "unit-t2").Applied);
            string first = session.Document.Castings[0].CastingId;
            string second = session.Document.Castings[1].CastingId;
            session.FocusCasting(second);
            Assert(session.MoveFocusedCastingWithinRoutine(-1).Applied);
            string[] longOrder = session.Document.Castings.Where(value => value.RoutineId == "long")
                .Select(value => value.CastingId).ToArray();
            if (longOrder.Length != 2 || longOrder[0] != second || longOrder[1] != first ||
                session.MoveFocusedCastingWithinRoutine(-1).Reason != "already-first")
                throw new InvalidOperationException("The focused casting did not move earlier exactly once.");
            Assert(session.MoveFocusedCastingToRoutine("short").Applied);
            PlannedCasting moved = session.Document.Castings.Single(value => value.CastingId == second);
            if (moved.RoutineId != "short" || session.MoveFocusedCastingToRoutine("short").Applied)
                throw new InvalidOperationException("The focused casting did not move to another routine.");
            Assert(session.SetFocusedRecastPolicy(ExistingEffectPolicy.Overwrite).Applied);
            if (session.Document.Castings.Single(value => value.CastingId == second).ExistingEffectPolicy !=
                    ExistingEffectPolicy.Overwrite ||
                session.Document.Castings.Single(value => value.CastingId == first).ExistingEffectPolicy !=
                    ExistingEffectPolicy.SkipAlreadyActive ||
                session.SetFocusedRecastPolicy(ExistingEffectPolicy.Overwrite).Applied)
                throw new InvalidOperationException("The recast choice did not stay with its own casting.");
            // A new caster keeps only the enhancements that caster has (a rod
            // in someone else's pack is not carried over).
            session.FocusCasting(first);
            PlannedCasting clericCasting = session.Document.Castings.Single(value => value.CastingId == first);
            Assert(session.UpdateFocusedCasting(clericCasting.WithEnhancementSelections(new[]
            {
                new AuthoredEnhancementSelection("extend-cleric", false, null)
            })).Applied);
            WorkspaceProviderChoice wizard = session.BuildView(inputs).FocusedProviders.Single(value =>
                value.CasterUnitId == "unit-wizard");
            AuthoringEditResult toWizard = session.SetFocusedProvider(wizard.ProviderKey, inputs);
            PlannedCasting switched = session.Document.Castings.Single(value => value.CastingId == first);
            if (!toWizard.Applied || switched.CasterUnitId != "unit-wizard" || switched.Enhancements.Count != 0 ||
                toWizard.Reason != "the new caster does not have Fixture extend-cleric (Undo restores it)")
                throw new InvalidOperationException("Another caster's enhancement was carried over or dropped silently: " +
                    toWizard.Reason);
            session.Undo();
            PlannedCasting restored = session.Document.Castings.Single(value => value.CastingId == first);
            if (restored.CasterUnitId != "unit-cleric" || restored.Enhancements.Count != 1)
                throw new InvalidOperationException("Undo did not restore the dropped enhancement.");
            session.FocusCasting(null);
            session.Draft.ExistingEffectPolicy = ExistingEffectPolicy.Overwrite;
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t3").Applied);
            PlannedCasting added = session.Document.Castings.Single(value =>
                value.CastingId != first && value.CastingId != second);
            if (added.ExistingEffectPolicy != ExistingEffectPolicy.Overwrite)
                throw new InvalidOperationException("The next casting's recast choice was not kept.");
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            string view = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "UI",
                "CastingWorkspaceScreenView.cs")).Replace("\r\n", "\n");
            foreach (string wiring in new[]
            {
                "_session.SetFocusedProvider(captured.ProviderKey, _inputs())",
                "_session.ChooseDraftProvider(captured.ProviderKey, _inputs())",
                "_session.MoveFocusedCastingToRoutine(capturedRoutine)",
                "_session.MoveFocusedCastingWithinRoutine(-1)",
                "_session.MoveFocusedCastingWithinRoutine(1)",
                "_session.SetFocusedRecastPolicy(recastFocused",
                "_session.Draft.ExistingEffectPolicy = recastDraft",
                "_session.SetAllowAnimatedFallback(!_session.AllowAnimatedFallback);",
                // Re-review: the out-of-combat rule, the target-mode switches
                // and review items in words.
                "_session.SetOutOfCombatOnly(!_session.OutOfCombatOnly);",
                "bool? groupAbility = _session.FocusedCastingIsGroupAbility(_inputs());",
                "                if (groupAbility == true)\n",
                "                if (groupAbility == false)\n",
                "\"FocusedMode.Group\", _inspectorContent, _theme,",
                "Domain.Authoring.CastingTargetMode.CasterCenteredOrigin, null, null,\n" +
                    "                                focused.DirectTargetUnitId == null ? null : new[] { focused.DirectTargetUnitId }),",
                "CreatePortraitTile(\"FocusedSingle.\" + captured.UnitId, singleRow,",
                ".Select(item => WorkspaceReasonText.DescribeReviewItem(item, reviewUnitName))",
                "WorkspaceFooterText.WholePlan(view.OnePassShortCount);"
            })
                if (!view.Contains(wiring))
                    throw new InvalidOperationException("The focused editor has no control for: " + wiring);
        }

        // Final review B7: a removed casting's id is never issued again in the
        // session, so a new card never shows the removed casting's last run.
        private static void TestSessionCastingIdsNotReused(string root)
        {
            string dir = Path.Combine(root, "casting-ids-not-reused");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
            Assert(AddDraftCasting(session, inputs, "unit-wizard", "unit-t2").Applied);
            string removed = session.Document.Castings[1].CastingId;
            session.FocusCasting(removed);
            Assert(session.RemoveFocusedCasting().Applied);
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t3").Applied);
            string[] ids = session.Document.Castings.Select(value => value.CastingId).ToArray();
            if (ids.Length != 2 || ids.Contains(removed) || ids.Distinct(StringComparer.Ordinal).Count() != 2)
                throw new InvalidOperationException("A removed casting's id was issued again: " +
                    string.Join(",", ids));
            // Re-review: a loaded plan's ids count too - the highest one,
            // removed after the planner reopens, is not issued again.
            Assert(AddDraftCasting(session, inputs, "unit-wizard", "unit-t4").Applied);
            session.Save();
            string highest = session.Document.Castings.Select(value => value.CastingId)
                .OrderBy(value => int.Parse(value.Substring(5), System.Globalization.CultureInfo.InvariantCulture)).Last();
            var reopened = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            reopened.FocusCasting(highest);
            Assert(reopened.RemoveFocusedCasting().Applied);
            Assert(AddDraftCasting(reopened, inputs, "unit-cleric", "unit-t5").Applied);
            if (reopened.Document.Castings.Any(value => value.CastingId == highest))
                throw new InvalidOperationException("A loaded plan's removed casting id was issued again: " + highest);
            // A casting this session showed and another session removed is
            // not given to the next casting after a reload either.
            reopened.Save();
            string top = reopened.Document.Castings.Select(value => value.CastingId)
                .OrderBy(value => int.Parse(value.Substring(5), System.Globalization.CultureInfo.InvariantCulture)).Last();
            var watcher = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            var editor = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            editor.FocusCasting(top);
            Assert(editor.RemoveFocusedCasting().Applied);
            editor.Save();
            watcher.Reload();
            string[] afterReload = watcher.Document.Castings.Select(value => value.CastingId).ToArray();
            Assert(AddDraftCasting(watcher, inputs, "unit-wizard", "unit-t1").Applied);
            string issued = watcher.Document.Castings.Select(value => value.CastingId)
                .Single(value => !afterReload.Contains(value));
            // Every id the session has seen stays below the ones it issues.
            if (int.Parse(issued.Substring(5), System.Globalization.CultureInfo.InvariantCulture) <=
                    int.Parse(top.Substring(5), System.Globalization.CultureInfo.InvariantCulture))
                throw new InvalidOperationException("After a reload the session issued " + issued +
                    ", not above " + top + " which it had shown.");
        }

        // Focused re-review: with no plan file on disk a Reload never blocks
        // saving and never discards castings added in the session; twice in
        // a row changes nothing; castings added while the classic import was
        // blocked are imported into, then saved together.
        private static void TestReloadWithoutPlanFile(string root)
        {
            string dir = Path.Combine(root, "reload-no-plan-file");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign", new DisabledCastingDispatchBoundary());
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                CastingPlanLoadStatus status = session.Reload();
                if (status != CastingPlanLoadStatus.Absent || session.PersistenceBlocked || session.SavesRefused ||
                    session.Document.Castings.Count != 1 || !session.IsDirty || session.LastReloadNote != "kept-unsaved")
                    throw new InvalidOperationException("A reload without a plan file blocked saving or dropped castings: " +
                        status + "|" + session.LastReloadNote);
            }
            session.Save();
            if (new CastingWorkspaceSession(dir, "workspace-campaign").Document.Castings.Count != 1)
                throw new InvalidOperationException("The kept casting was not saved after the reloads.");
            // Castings added while the classic import was blocked are imported
            // into when the classic file is repaired, and saved with it.
            string mergeDir = Path.Combine(root, "reload-merge");
            Directory.CreateDirectory(mergeDir);
            var classic = new ProfileRepository(mergeDir);
            string classicPath = classic.GetProfilePath("workspace-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(classicPath));
            File.WriteAllText(classicPath, "{ not json");
            var blocked = new CastingWorkspaceSession(mergeDir, "workspace-campaign", new DisabledCastingDispatchBoundary(),
                PerTarget("source-bulls"));
            Assert(blocked.LegacyImportBlocked);
            Assert(AddDraftCasting(blocked, inputs, "unit-wizard", "unit-t2").Applied);
            string authored = blocked.Document.Castings.Single().CastingId;
            bool chosenOutOfCombat = !blocked.OutOfCombatOnly;
            blocked.SetOutOfCombatOnly(chosenOutOfCombat);
            File.Delete(classicPath);
            BuffPlannerProfile legacy = BuffPlannerProfile.CreateDefault("workspace-campaign");
            legacy.Routines[0].Assignments.Add(LegacyAssignment("source-bulls", CastingBuffAbility,
                PinnedChild("legacy-bulls", 0, "unit-cleric", "unit-t1")));
            classic.Save(legacy);
            blocked.Reload();
            List<string> merged = blocked.Document.Castings.Select(value => value.CastingId).ToList();
            if (blocked.LegacyImportBlocked || blocked.PersistenceBlocked || merged.Count != 2 ||
                !merged.Contains(authored) || blocked.IsDirty || blocked.LastReloadNote != "imported-into-unsaved" ||
                new CastingWorkspaceSession(mergeDir, "workspace-campaign").Document.Castings.Count != 2 ||
                blocked.OutOfCombatOnly != chosenOutOfCombat ||
                new CastingWorkspaceSession(mergeDir, "workspace-campaign").OutOfCombatOnly != chosenOutOfCombat)
                throw new InvalidOperationException("Castings added while the classic import was blocked were not kept: " +
                    string.Join(",", merged.ToArray()) + "|" + blocked.LastReloadNote);
            // Last review: when the player changed no setting in the session,
            // the classic plan's own settings are imported with the merge.
            string keepDir = Path.Combine(root, "reload-merge-settings");
            Directory.CreateDirectory(keepDir);
            var keepClassic = new ProfileRepository(keepDir);
            string keepPath = keepClassic.GetProfilePath("workspace-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(keepPath));
            File.WriteAllText(keepPath, "{ not json");
            var untouched = new CastingWorkspaceSession(keepDir, "workspace-campaign", new DisabledCastingDispatchBoundary(),
                PerTarget("source-bulls"));
            Assert(AddDraftCasting(untouched, inputs, "unit-wizard", "unit-t2").Applied);
            File.Delete(keepPath);
            BuffPlannerProfile instantLegacy = BuffPlannerProfile.CreateDefault("workspace-campaign");
            instantLegacy.Execution.Mode = "instant";
            instantLegacy.Routines[0].Assignments.Add(LegacyAssignment("source-bulls", CastingBuffAbility,
                PinnedChild("legacy-bulls", 0, "unit-cleric", "unit-t1")));
            keepClassic.Save(instantLegacy);
            untouched.Reload();
            if (untouched.LastReloadNote != "imported-into-unsaved" || untouched.ExecutionMode != "instant" ||
                new CastingWorkspaceSession(keepDir, "workspace-campaign").ExecutionMode != "instant")
                throw new InvalidOperationException("The classic plan's settings were dropped by a merge the player did not change: " +
                    untouched.ExecutionMode);
        }

        // Focused re-review: the close is decided once, at the press, for an
        // accepted run from the open screen; a kept result is shown only for
        // its own campaign.
        private static void TestClassicRunScreenPolicy()
        {
            if (!ClassicRunScreenPolicy.CloseAfterPress(true, true) ||
                ClassicRunScreenPolicy.CloseAfterPress(true, false) ||
                ClassicRunScreenPolicy.CloseAfterPress(false, true) ||
                ClassicRunScreenPolicy.CloseAfterPress(false, false))
                throw new InvalidOperationException("The Classic screen closes for the wrong run.");
            if (!ClassicRunScreenPolicy.ShowStashedResult("campaign-a", "campaign-a") ||
                ClassicRunScreenPolicy.ShowStashedResult("campaign-a", "campaign-b") ||
                ClassicRunScreenPolicy.ShowStashedResult(null, "campaign-a") ||
                ClassicRunScreenPolicy.ShowStashedResult("campaign-a", null))
                throw new InvalidOperationException("A kept Classic result is shown for another campaign.");
        }

        // Review M1, kept after the focused re-review: an unverified zero cost
        // on a finite pool reserves nothing and stays an unknown cost (which
        // the executors and the step converter refuse before any cast).
        private static void TestFinitePoolZeroCostStaysUnknown()
        {
            var pool = new ResourcePoolSnapshot("pool-z", ResourcePoolKind.SpontaneousLevel, 1, 1, null);
            var free = new ProviderSnapshot(new ProviderKey("unit-z", "book-z", CastingBuffAbility, "level-1"),
                CastingBuffAbility.BaseAbilityGuid, 1, "pool-z", 0, null);
            var ledger = new ResourceLedger(new[] { pool });
            ResourceReservation first;
            string reason;
            bool firstOk = ledger.TryReserve(free, out first, out reason);
            if (!firstOk || first.Units != 0 || first.CostKnown || first.Unlimited)
                throw new InvalidOperationException("An unverified zero cost on a finite pool looked known: " +
                    (first == null ? "none" : first.Units + "|" + first.CostKnown));
        }

        // Targeted review: an interrupted run says whether it had put any cast
        // to the game - the animated executor queues and starts, the instant
        // one submits.
        private static void TestInterruptedRunWording()
        {
            CastingWorkspaceInputs inputs = QualificationInputs(true, true, null);
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectZeroCostMixed(
                inputs, "fixture-campaign");
            CastPlan plan = CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign")[0]
                .Projection.Plan;
            foreach (CastExecutionStatus attempted in new[] { CastExecutionStatus.Queued,
                    CastExecutionStatus.Submitted, CastExecutionStatus.CastStarted,
                    CastExecutionStatus.ResourceSpent, CastExecutionStatus.EffectConfirmed })
            {
                var report = new ExecutionReport(plan);
                report.Add(0, plan.Steps[0], attempted, "fixture");
                if (!report.AnyCastAttempted)
                    throw new InvalidOperationException("A run that put a cast to the game said it had not: " + attempted);
            }
            var refused = new ExecutionReport(plan);
            refused.Add(0, plan.Steps[0], CastExecutionStatus.FailedValidation, "fixture");
            if (refused.AnyCastAttempted || new ExecutionReport(plan).AnyCastAttempted)
                throw new InvalidOperationException("A run that cast nothing said it had cast.");
        }

        private static bool? GroupAbilityOfFocus(string root, CastingWorkspaceInputs groupInputs)
        {
            string dir = Path.Combine(root, "group-ability-of-focus");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign", new DisabledCastingDispatchBoundary());
            var casting = new PlannedCasting("cast-g", "long", 0, "source-communal", CastingGroupAbility,
                "unit-cleric", null, CastingTargetMode.CasterCenteredOrigin, null, CastingOrigin.CasterCentered(),
                new[] { "unit-t1" }, null, null, ExistingEffectPolicy.SkipAlreadyActive, null,
                CastingAuthoringState.Ready, null);
            new CastingPlanRepository(dir).Save(CastingPlanProfile.FromDocument(new CastingPlanDocument(
                "workspace-campaign", new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                }, new[] { casting })));
            session = new CastingWorkspaceSession(dir, "workspace-campaign", new DisabledCastingDispatchBoundary());
            session.FocusCasting("cast-g");
            return session.FocusedCastingIsGroupAbility(groupInputs);
        }

        // Re-review: the footer counts this routine's castings that are ready
        // on their own but short of a resource when every routine runs in
        // one pass - not castings already short on their own.
        private static void TestFooterCountsOnlyShortfalls(string root)
        {
            string dir = Path.Combine(root, "footer-short-count");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot, remainingPerCaster: 1);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
            session.SelectRoutine("short");
            session.BuildView(inputs);
            foreach (string target in new[] { "unit-t2", "unit-t3" })
            {
                session.Draft.SourceId = "source-bulls";
                session.Draft.TargetMode = CastingTargetMode.DirectTarget;
                session.ChooseDraftCaster("unit-cleric");
                session.Draft.DirectTargetUnitId = target;
                session.Draft.State = CastingAuthoringState.Ready;
                AuthoringEditResult added = session.AddCastingFromDraft(inputs);
                if (!added.Applied)
                    throw new InvalidOperationException("A short-routine casting was refused: " + added.Reason);
            }
            WorkspaceView view = session.BuildView(inputs);
            if (view.OnePassShortCount != 1)
                throw new InvalidOperationException("The footer did not count exactly the one-pass shortfall: " +
                    view.OnePassShortCount);
        }

        // Re-review: a provider choice names its exact source (the spellbook,
        // its spell level and the kind of slot); two options that differ only
        // by spell level in one spellbook cannot be pinned by a casting and
        // are refused with a reason, never authored; an item's source without
        // a spellbook is the same source whether it is recorded as none or
        // empty.
        private static void TestProviderLabelsAndTwins(string root)
        {
            if (WorkspaceProviderLabels.Describe("Linzi", "Bard", 1, ResourcePoolKind.SpontaneousLevel, "pool", 5, 0) !=
                    "Linzi: Bard level 1 (spell slot), caster level 5" ||
                WorkspaceProviderLabels.Describe("Hedwirg", "Wizard", 2, ResourcePoolKind.PreparedSlots, "pool", 7, 1) !=
                    "Hedwirg: Wizard level 2 (prepared slot), caster level 7, with metamagic" ||
                WorkspaceProviderLabels.Describe("Tartuccio", "Sorcerer", 0, ResourcePoolKind.Unlimited, "pool", 3, 0) !=
                    "Tartuccio: Sorcerer level 0 (free), caster level 3" ||
                WorkspaceProviderLabels.Describe("Linzi", string.Empty, 1, ResourcePoolKind.ItemCharges,
                    "Wand of Shield", 1, 0) != "Linzi: Wand of Shield, caster level 1")
                throw new InvalidOperationException("A provider label does not name its exact source.");
            string dir = Path.Combine(root, "provider-twins");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot baseSnapshot;
            CastingWorkspaceInputs baseInputs = WorkspaceInputs(out baseSnapshot);
            ProviderPlanningOption clericOption = baseInputs.ProviderOptions.Single(value =>
                value.Provider.Key.CasterUnitId == "unit-cleric");
            var twin = new ProviderSnapshot(new ProviderKey("unit-cleric", clericOption.Provider.Key.SpellbookGuid,
                    CastingBuffAbility, "level-3"), CastingBuffAbility.BaseAbilityGuid, 3,
                clericOption.Provider.ResourcePoolKey, 1, null, null, 10, sourceBookName: "Cleric");
            var snapshot = new PartyProviderSnapshot(baseSnapshot.Units,
                baseSnapshot.Providers.Concat(new[] { twin }).ToList(), baseSnapshot.ResourcePools);
            var inputs = new CastingWorkspaceInputs(snapshot, baseInputs.ProviderOptions.Concat(new[]
                {
                    new ProviderPlanningOption(twin, clericOption.ReachableTargetIds,
                        clericOption.LegalAnchorIds, 10, 100)
                }).ToList(), baseInputs.EffectsBySource, baseInputs.Enhancements);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            session.SelectBuff("source-bulls");
            session.SelectRoutine("long");
            session.BuildView(inputs);
            session.Draft.SourceId = "source-bulls";
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.ChooseDraftCaster("unit-cleric");
            WorkspaceProviderChoice twinChoice = session.BuildView(inputs).DraftProviders.SingleOrDefault(value =>
                value.ProviderKey.EndsWith("|level-3", StringComparison.Ordinal));
            AuthoringEditResult pinned = twinChoice == null ? null
                : session.ChooseDraftProvider(twinChoice.ProviderKey, inputs);
            if (twinChoice == null || twinChoice.Label != "unit-cleric: Cleric level 3 (spell slot), caster level 10" ||
                pinned.Applied || !pinned.Reason.StartsWith("provider-not-pinnable:", StringComparison.Ordinal) ||
                !WorkspaceRefusalText.Describe(pinned.Reason).StartsWith(
                    "that character knows this spell at more than one level", StringComparison.Ordinal))
                throw new InvalidOperationException("A source the casting cannot pin was offered or authored: " +
                    (twinChoice == null ? "missing" : twinChoice.Label + "|" + pinned.Reason));
            Assert(AddDraftCasting(session, inputs, "unit-wizard", "unit-t1").Applied);
            string wizardCasting = session.Document.Castings.Single().CastingId;
            session.FocusCasting(wizardCasting);
            List<WorkspaceProviderChoice> focusedChoices = session.BuildView(inputs).FocusedProviders.ToList();
            WorkspaceProviderChoice focusedTwin = focusedChoices.Single(value =>
                value.ProviderKey.EndsWith("|level-3", StringComparison.Ordinal));
            AuthoringEditResult focusedPinned = session.SetFocusedProvider(focusedTwin.ProviderKey, inputs);
            if (focusedPinned.Applied || !focusedPinned.Reason.StartsWith("provider-not-pinnable:", StringComparison.Ordinal) ||
                session.Document.Castings.Single().CasterUnitId != "unit-wizard" ||
                focusedChoices.Count(value => value.Selected) != 1 ||
                focusedChoices.Single(value => value.Selected).CasterUnitId != "unit-wizard")
                throw new InvalidOperationException("The focused casting took a source it cannot pin: " +
                    focusedPinned.Reason);
            // A casting already on the twins' spellbook (an older plan's) shows
            // neither twin as its source.
            Assert(session.UpdateFocusedCasting(session.Document.Castings.Single().WithProvider("unit-cleric",
                CastingBuffAbility, clericOption.Provider.Key.SpellbookGuid,
                new AuthoredEnhancementSelection[0])).Applied);
            if (session.BuildView(inputs).FocusedProviders.Any(value => value.Selected))
                throw new InvalidOperationException("A source the casting cannot pin was shown as its source.");
            // An item's source has no spellbook: "" in its key, none in the
            // casting - the same source, so choosing it again is no change.
            string itemDir = Path.Combine(root, "provider-no-spellbook");
            Directory.CreateDirectory(itemDir);
            var item = new ProviderSnapshot(new ProviderKey("unit-cleric", null, CastingBuffAbility, "item-wand"),
                CastingBuffAbility.BaseAbilityGuid, 1, clericOption.Provider.ResourcePoolKey, 1, null, null, 5);
            var itemSnapshot = new PartyProviderSnapshot(baseSnapshot.Units, baseSnapshot.Providers
                .Where(value => value.Key.CasterUnitId != "unit-cleric").Concat(new[] { item }).ToList(),
                baseSnapshot.ResourcePools);
            var itemInputs = new CastingWorkspaceInputs(itemSnapshot, baseInputs.ProviderOptions
                .Where(value => value.Provider.Key.CasterUnitId != "unit-cleric")
                .Concat(new[] { new ProviderPlanningOption(item, clericOption.ReachableTargetIds,
                    clericOption.LegalAnchorIds, 5, 100) }).ToList(),
                baseInputs.EffectsBySource, baseInputs.Enhancements);
            var itemCasting = new PlannedCasting("cast-item", "long", 0, "source-bulls", CastingBuffAbility,
                "unit-cleric", null, CastingTargetMode.DirectTarget, "unit-t1", null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            new CastingPlanRepository(itemDir).Save(CastingPlanProfile.FromDocument(new CastingPlanDocument(
                "workspace-campaign", new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                }, new[] { itemCasting })));
            var itemSession = new CastingWorkspaceSession(itemDir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            itemSession.FocusCasting("cast-item");
            WorkspaceProviderChoice itemChoice = itemSession.BuildView(itemInputs).FocusedProviders.Single(value =>
                value.CasterUnitId == "unit-cleric");
            AuthoringEditResult same = itemSession.SetFocusedProvider(itemChoice.ProviderKey, itemInputs);
            if (!itemChoice.Selected || same.Applied || same.Reason != "provider-unchanged" || itemSession.IsDirty)
                throw new InvalidOperationException("A source without a spellbook was not recognised as unchanged: " +
                    same.Reason);
            // The game adapter names the spellbook as the game shows it.
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            string builder = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner",
                "GameAdapters", "KingmakerPartySnapshotBuilder.cs")).Replace("\r\n", "\n");
            string forecast = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner",
                "UI", "SequentialForecastPlanner.cs")).Replace("\r\n", "\n");
            if (!builder.Contains("duration, selection.SourceDisplayName, selection.VariantOrder,\n" +
                    "                SpellbookName(spellbook)));") ||
                !builder.Contains("string name = spellbook.Blueprint.DisplayName;") ||
                !builder.Contains("name = spellbook.Blueprint.CharacterClass.Name;") ||
                !forecast.Contains("provider.VariantOrder, provider.SourceBookName));"))
                throw new InvalidOperationException("The spellbook's name is not read or not kept.");
        }

        // Re-review: the next casting's enhancements belong to its caster; a
        // new caster keeps only the ones it has.
        private static void TestDraftEnhancementsFollowTheCaster(string root)
        {
            string dir = Path.Combine(root, "draft-enhancements-follow-caster");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            session.SelectBuff("source-bulls");
            session.SelectRoutine("long");
            session.BuildView(inputs);
            session.Draft.SourceId = "source-bulls";
            session.ChooseDraftCaster("unit-cleric");
            session.Draft.Enhancements.Add(new AuthoredEnhancementSelection("extend-cleric", false, null));
            session.ChooseDraftCaster("unit-cleric");
            int keptForSameCaster = session.Draft.Enhancements.Count;
            session.ChooseDraftCaster("unit-wizard");
            if (keptForSameCaster != 1 || session.Draft.Enhancements.Count != 0)
                throw new InvalidOperationException("The next casting carried an enhancement to a caster without it.");
        }

        // Re-review: the out-of-combat rule has a casting-first control; the
        // change is unsaved until Save, then kept.
        private static void TestOutOfCombatSettingSaved(string root)
        {
            string dir = Path.Combine(root, "out-of-combat-setting");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            bool before = session.OutOfCombatOnly;
            session.SetOutOfCombatOnly(!before);
            if (session.OutOfCombatOnly == before || !session.IsDirty ||
                session.ExecutionSettings.OutOfCombatOnly == before)
                throw new InvalidOperationException("The out-of-combat change did not take or looked saved.");
            session.Save();
            if (new CastingWorkspaceSession(dir, "workspace-campaign").OutOfCombatOnly == before)
                throw new InvalidOperationException("The out-of-combat setting was not saved.");
        }

        // Re-review: an imported casting whose old plan did not say single
        // target or group becomes a single-target casting in the focused
        // editor, then Ready in place.
        private static void TestImportedGroupingUnknownBecomesSingleTarget(string root)
        {
            string dir = Path.Combine(root, "imported-grouping-unknown");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var imported = new PlannedCasting("cast-unknown", "long", 0, "source-bulls", CastingBuffAbility,
                "unit-cleric", "book-unit-cleric", CastingTargetMode.CasterCenteredOrigin, null,
                CastingOrigin.CasterCentered(), new[] { "unit-t1", "unit-t2" }, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Draft,
                new MigrationProvenance("cast-unknown", 5, "long", string.Empty, "grouping-unknown",
                    new[] { "grouping-unknown:single-or-group-pending-review;targets=unit-t1,unit-t2" }, null));
            new CastingPlanRepository(dir).Save(CastingPlanProfile.FromDocument(new CastingPlanDocument(
                "workspace-campaign", new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                }, new[] { imported })));
            var session = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            session.FocusCasting("cast-unknown");
            AuthoringEditResult single = session.SetFocusedTargeting(CastingTargetMode.DirectTarget,
                "unit-t1", null, null);
            AuthoringEditResult resolved = session.ResolveFocusedImportReview();
            AuthoringEditResult ready = session.SetFocusedCastingState(CastingAuthoringState.Ready);
            PlannedCasting after = session.Document.Castings.Single();
            ResolvedCasting compiled = session.CompilePlan(inputs).CastingById("cast-unknown");
            session.BuildView(inputs);
            if (session.FocusedCastingIsGroupAbility(inputs) != false ||
                single.Reason != "it no longer reaches unit-t2 - add a casting for each, or Undo" ||
                WorkspaceReasonText.DescribeReviewItem("grouping-unknown:single-or-group-pending-review;targets=unit-t1,unit-t2",
                    unitId => unitId.ToUpperInvariant()) !=
                    "the old plan did not say single target or group (it named UNIT-T1, UNIT-T2); choose")
                throw new InvalidOperationException("The single/group switch is not the buff's own, or the dropped " +
                    "recipients were not named: " + single.Reason);
            PartyProviderSnapshot groupSnapshot;
            CastingWorkspaceInputs groupInputs = WorkspaceInputs(out groupSnapshot, ability: CastingGroupAbility);
            if (!single.Applied || !resolved.Applied || !ready.Applied || after.CastingId != "cast-unknown" ||
                after.TargetMode != CastingTargetMode.DirectTarget || after.DirectTargetUnitId != "unit-t1" ||
                compiled == null || !compiled.IsExecutable ||
                GroupAbilityOfFocus(root, groupInputs) != true)
                throw new InvalidOperationException("The imported casting did not become a Ready single-target casting: " +
                    single.Reason + "|" + resolved.Reason + "|" + ready.Reason + "|" + (compiled == null ? "none"
                        : string.Join(",", compiled.ReadinessReasons.ToArray())));
        }

        // Re-review: the first-open import reads the classic planner's own
        // in-memory plan of this campaign (its sources rebound to the party);
        // the file must still be readable, is archived exactly and is never
        // written; another campaign's plan in memory is never imported.
        private static void TestImportFromReboundClassicPlan(string root)
        {
            string dir = Path.Combine(root, "import-rebound-classic");
            Directory.CreateDirectory(dir);
            var repository = new ProfileRepository(dir);
            BuffPlannerProfile onDisk = LegacyProfile();
            onDisk.Routines[0].Assignments.Add(LegacyAssignment("source-bulls", CastingBuffAbility,
                PinnedChild("legacy-bulls", 0, "unit-cleric", "unit-t1")));
            repository.Save(onDisk);
            string legacyPath = repository.GetProfilePath("legacy-campaign");
            string bytes = File.ReadAllText(legacyPath);
            BuffPlannerProfile rebound = LegacyProfile();
            rebound.Routines[0].Assignments.Add(LegacyAssignment("source-bulls-rebound", CastingBuffAbility,
                PinnedChild("legacy-bulls", 0, "unit-cleric", "unit-t1")));
            string legacyHash = KingmakerBuffPlanner.Infrastructure.Hashing.Sha256(legacyPath);
            CastingMigrationResult result = new CastingPlanMigrationService(dir).Migrate("legacy-campaign",
                PerTarget("source-bulls-rebound"), new ClassicPlanInMemory(rebound, legacyHash));
            CastingPlanLoadResult candidate = new CastingPlanRepository(dir).Load("legacy-campaign");
            if (result.Status != CastingMigrationStatus.Migrated || candidate.Status != CastingPlanLoadStatus.Loaded ||
                candidate.Profile.ToDocument().Castings.Single().SourceId != "source-bulls-rebound" ||
                File.ReadAllText(legacyPath) != bytes || File.ReadAllText(result.ArchivePath) != bytes)
                throw new InvalidOperationException("The rebound classic plan was not the one imported: " +
                    result.Status + " " + result.Warning);
            string otherDir = Path.Combine(root, "import-rebound-other-campaign");
            Directory.CreateDirectory(otherDir);
            new ProfileRepository(otherDir).Save(onDisk);
            BuffPlannerProfile otherCampaign = LegacyProfile();
            otherCampaign.CampaignId = "another-campaign";
            otherCampaign.Routines[0].Assignments.Add(LegacyAssignment("source-bulls-rebound", CastingBuffAbility,
                PinnedChild("legacy-bulls", 0, "unit-cleric", "unit-t1")));
            CastingMigrationResult other = new CastingPlanMigrationService(otherDir).Migrate("legacy-campaign",
                PerTarget("source-bulls"), new ClassicPlanInMemory(otherCampaign, legacyHash));
            if (other.Status != CastingMigrationStatus.Migrated || new CastingPlanRepository(otherDir)
                    .Load("legacy-campaign").Profile.ToDocument().Castings.Single().SourceId != "source-bulls")
                throw new InvalidOperationException("Another campaign's plan in memory was imported.");
            // Focused re-review: a plan in memory that was not read from the
            // file's current bytes (a default made because the file could not
            // be read, or a read before the file changed) never stands in for
            // the file.
            foreach (string staleHash in new[] { null, new string('0', 64) })
            {
                string staleDir = Path.Combine(root, "import-rebound-stale-" + (staleHash == null ? "none" : "other"));
                Directory.CreateDirectory(staleDir);
                new ProfileRepository(staleDir).Save(onDisk);
                CastingMigrationResult stale = new CastingPlanMigrationService(staleDir).Migrate("legacy-campaign",
                    PerTarget("source-bulls"), new ClassicPlanInMemory(rebound, staleHash));
                if (stale.Status != CastingMigrationStatus.Migrated || new CastingPlanRepository(staleDir)
                        .Load("legacy-campaign").Profile.ToDocument().Castings.Single().SourceId != "source-bulls")
                    throw new InvalidOperationException("A plan in memory not read from the file's bytes was imported.");
            }
            // Last review: the bytes are decoded exactly like File.ReadAllText
            // (with and without a byte-order mark), and the hash is theirs.
            string bomDir = Path.Combine(root, "import-bom");
            Directory.CreateDirectory(bomDir);
            var bomRepository = new ProfileRepository(bomDir);
            bomRepository.Save(onDisk);
            string bomPath = bomRepository.GetProfilePath("legacy-campaign");
            byte[] plain = File.ReadAllBytes(bomPath);
            byte[] withBom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(plain).ToArray();
            File.WriteAllBytes(bomPath, withBom);
            ProfileLoadResult bomLoad = bomRepository.Load("legacy-campaign");
            if (ProfileRepository.DecodeFileText(withBom) != File.ReadAllText(bomPath) ||
                ProfileRepository.DecodeFileText(plain) != System.Text.Encoding.UTF8.GetString(plain) ||
                bomLoad.RecoveredFromBackup || bomLoad.PrimarySha256 != KingmakerBuffPlanner.Infrastructure.Hashing.Sha256(bomPath) ||
                bomLoad.Profile.Routines[0].Assignments.Count != 1)
                throw new InvalidOperationException("A Classic file with a byte-order mark was read or hashed differently.");
            // The Classic load says which bytes it read: the main file's hash,
            // or none for a backup or a new default.
            ProfileLoadResult primaryLoad = repository.Load("legacy-campaign");
            if (primaryLoad.PrimarySha256 != legacyHash)
                throw new InvalidOperationException("The Classic load did not record the main file's bytes.");
            // Targeted review: the Classic planner's groupings of the same
            // refresh are used for the import (a single-target buff is not
            // left as "grouping unknown" when the session was built without).
            string groupingDir = Path.Combine(root, "import-fresh-groupings");
            Directory.CreateDirectory(groupingDir);
            var groupingRepository = new ProfileRepository(groupingDir);
            groupingRepository.Save(onDisk);
            string groupingHash = KingmakerBuffPlanner.Infrastructure.Hashing.Sha256(
                groupingRepository.GetProfilePath("legacy-campaign"));
            var groupingSession = new CastingWorkspaceSession(groupingDir, "legacy-campaign",
                new DisabledCastingDispatchBoundary(), null,
                () => new ClassicPlanInMemory(onDisk, groupingHash, PerTarget("source-bulls")));
            if (groupingSession.Document.Castings.Count != 1 ||
                groupingSession.Document.Castings.Single().TargetMode != CastingTargetMode.DirectTarget)
                throw new InvalidOperationException("The Classic planner's own groupings were not used for the import.");
            // The reviewer's case end to end: the Classic planner holds a new
            // default because its file could not be read; the first import is
            // blocked; the player repairs the file and presses Reload.
            string repairDir = Path.Combine(root, "import-stale-default");
            Directory.CreateDirectory(repairDir);
            var repairRepository = new ProfileRepository(repairDir);
            string repairPath = repairRepository.GetProfilePath("legacy-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(repairPath));
            File.WriteAllText(repairPath, "{ not json");
            ProfileLoadResult defaultLoad = repairRepository.Load("legacy-campaign");
            var repairSession = new CastingWorkspaceSession(repairDir, "legacy-campaign",
                new DisabledCastingDispatchBoundary(), PerTarget("source-bulls"),
                () => new ClassicPlanInMemory(defaultLoad.Profile, defaultLoad.PrimarySha256));
            if (!repairSession.LegacyImportBlocked || defaultLoad.PrimarySha256 != null)
                throw new InvalidOperationException("An unreadable classic file did not block the first import.");
            File.Delete(repairPath);
            repairRepository.Save(onDisk);
            repairSession.Reload();
            if (repairSession.LegacyImportBlocked || repairSession.PersistenceBlocked ||
                repairSession.Document.Castings.Count != 1 ||
                repairSession.Document.Castings.Single().SourceId != "source-bulls")
                throw new InvalidOperationException("The repaired classic plan was replaced by the stale default: " +
                    repairSession.Document.Castings.Count);
            string badDir = Path.Combine(root, "import-rebound-unreadable");
            Directory.CreateDirectory(badDir);
            string badPath = new ProfileRepository(badDir).GetProfilePath("legacy-campaign");
            Directory.CreateDirectory(Path.GetDirectoryName(badPath));
            File.WriteAllText(badPath, "{ not json");
            if (new CastingPlanMigrationService(badDir).Migrate("legacy-campaign", PerTarget("source-bulls-rebound"),
                    new ClassicPlanInMemory(rebound, legacyHash)).Status != CastingMigrationStatus.LegacyUnreadable ||
                File.ReadAllText(badPath) != "{ not json")
                throw new InvalidOperationException("An unreadable classic file was bypassed by the plan in memory.");
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            Func<string, string> source = name => File.ReadAllText(Path.Combine(directory.FullName, "src",
                "KingmakerBuffPlanner", "UI", name)).Replace("\r\n", "\n");
            if (!source("BuffPlannerUiRoot.cs").Contains("_session.Model == null ? null : _session.Model.SourceGroupings(),\n" +
                    "                () => _session.Model == null ? null\n" +
                    "                    : new ClassicPlanInMemory(_session.Model.Profile, _session.ClassicPrimarySha256,\n" +
                    "                        _session.Model.SourceGroupings()));") ||
                !source("CastingWorkspaceSession.cs").Contains(".Migrate(campaignId, effectiveGroupings, inMemory, unsaved,") ||
                !source("PlannerUiSession.cs").Contains("ClassicPrimarySha256 = loaded.PrimarySha256;") ||
                !source("PlannerUiSession.cs").Contains(
                    "ClassicPrimarySha256 = ProfileRepository.TryHash(_profiles.GetProfilePath(profile.CampaignId));"))
                throw new InvalidOperationException("The workspace does not import the rebound classic plan by its bytes.");
        }

        // Final review B5-B7: casting-first refreshes never save the Classic
        // file (checked when the save happens, so Classic saves again once it
        // is the mode); the spellbook handoff waits for the workspace in
        // casting-first mode; the HUD tooltip never describes another
        // campaign's session or press result.
        private static void TestClassicSaveAndHudScoping()
        {
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            Func<string, string> source = name => File.ReadAllText(Path.Combine(directory.FullName, "src",
                "KingmakerBuffPlanner", "UI", name)).Replace("\r\n", "\n");
            string session = source("PlannerUiSession.cs");
            string root = source("BuffPlannerUiRoot.cs");
            if (!session.Contains("_providerOptions, SaveClassicProfile, _enhancements,") ||
                session.Contains("_profiles.Save,") ||
                !session.Contains("if (suppressed != null && suppressed())") ||
                !root.Contains("_session.ClassicSavesSuppressed = () => CastingFirstActive;"))
                throw new InvalidOperationException("Casting-first refreshes can still save the Classic file.");
            if (!root.Contains("() => (_screen != null && _screen.IsOpen) || _castingWorkspace != null,") ||
                !root.Contains("PlannerScreenLifecycleState.Open) || _castingWorkspace != null,"))
                throw new InvalidOperationException("The spellbook handoff ignores the casting-first workspace.");
            if (!root.Contains("if (session != null && !string.Equals(session.CampaignId, loadedCampaignId,") ||
                System.Text.RegularExpressions.Regex.Matches(root, "_lastCastingPress\\[PressKey\\(session, routineId\\)\\]").Count != 2 ||
                !root.Contains("_lastCastingPress.TryGetValue(PressKey(session, routineId), out last);") ||
                System.Text.RegularExpressions.Regex.IsMatch(root, "_lastCastingPress\\[routineId\\]"))
                throw new InvalidOperationException("The HUD tooltip can describe another campaign.");
        }

        // Nothing to submit is an honest no-op before review: every casting
        // already active is disclosed, nothing reaches the boundary.
        private static void TestSessionApplyNothingToCast(string root)
        {
            string dir = Path.Combine(root, "nothing-to-cast");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var boundary = new DisabledCastingDispatchBoundary();
            var session = new CastingWorkspaceSession(dir, "workspace-campaign", boundary);
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
            var live = new CastingWorkspaceInputs(inputs.Snapshot, inputs.ProviderOptions,
                inputs.EffectsBySource, inputs.Enhancements, null,
                LiveEffects(On("unit-t1", "buff-effect", null, 1, null)));
            WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long", live);
            if (result.Allowed || result.ReviewReason != "nothing-to-cast:1" ||
                result.Dispatch != null || boundary.RecordedSubmissions.Count != 0 ||
                !result.GateDecision.Omissions[0].Reasons.Contains("already-active:unit-t1"))
                throw new InvalidOperationException("An all-active routine was not a disclosed no-op: " +
                    result.ReviewReason);
        }

        // Two ready castings (cast-1 cleric->t1, cast-2 wizard->t2) and,
        // optionally, one already-active (cast-3) and one disabled (cast-4).
        private static ExplicitStepConversion HostFixture(out ExplicitCastingPlan plan,
            out CastingApplyDecision decision, bool withSkippedAndDisabled)
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(CastingBuffAbility,
                out options, out enhancements, new[] { "unit-t1", "unit-t2", "unit-t3" }, 3);
            var castings = new List<PlannedCasting>
            {
                DirectCasting("cast-1", "long", "unit-cleric", "unit-t1", "source-bulls",
                    CastingBuffAbility),
                DirectCasting("cast-2", "long", "unit-wizard", "unit-t2", "source-bulls",
                    CastingBuffAbility)
            };
            if (withSkippedAndDisabled)
            {
                castings.Add(DirectCasting("cast-3", "long", "unit-cleric", "unit-t3",
                    "source-bulls", CastingBuffAbility));
                castings.Add(DirectCasting("cast-4", "long", "unit-wizard", "unit-t3",
                    "source-bulls", CastingBuffAbility, null, null,
                    CastingAuthoringState.Disabled));
            }
            Dictionary<string, EffectExpression> effects =
                CastingEffects("source-bulls", "source-communal");
            ActiveEffectSnapshot live = withSkippedAndDisabled
                ? LiveEffects(On("unit-t3", "buff-effect", null, 1, null)) : null;
            plan = new ExplicitCastingCompiler().Compile(CastingDocument(castings.ToArray()),
                snapshot, options, effects, enhancements, "long", null, false, live);
            decision = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
            return ExplicitCastingStepConverter.Convert(plan, decision, options, effects);
        }

        // The production boundary accepts only the exact Standard projection
        // of the decision; refusals never start a run.
        private static void TestNativeBoundaryRefusals()
        {
            ExplicitCastingPlan plan;
            CastingApplyDecision decision;
            ExplicitStepConversion projection = HostFixture(out plan, out decision, false);
            if (!projection.Converted ||
                NativeCastingDispatchBoundary.Validate(plan, decision, projection) != null)
                throw new InvalidOperationException("The exact projection was refused: " +
                    projection.Refusal);
            if (NativeCastingDispatchBoundary.Validate(plan, decision,
                    ExplicitStepConversion.Refuse("fixture")) != "projection-not-converted" ||
                NativeCastingDispatchBoundary.Validate(plan, null, projection) != "decision-not-allowed")
                throw new InvalidOperationException("An unconverted or undecided run was accepted.");
            ExplicitStepConversion probeScoped = ExplicitStepConversion.Success(projection.Plan,
                projection.CastingIds.ToList(), ExplicitProjectionScope.SingleCastProbe,
                projection.ProjectionId, projection.CanonicalContract);
            ExplicitStepConversion tampered = ExplicitStepConversion.Success(projection.Plan,
                projection.CastingIds.ToList(), ExplicitProjectionScope.Standard,
                new string('0', 64), projection.CanonicalContract);
            ExplicitStepConversion reordered = ExplicitStepConversion.Success(projection.Plan,
                projection.CastingIds.Reverse().ToList(), ExplicitProjectionScope.Standard,
                projection.ProjectionId, projection.CanonicalContract);
            if (NativeCastingDispatchBoundary.Validate(plan, decision, probeScoped) !=
                    "projection-scope-not-standard:SingleCastProbe" ||
                NativeCastingDispatchBoundary.Validate(plan, decision, tampered) !=
                    "projection-tampered" ||
                NativeCastingDispatchBoundary.Validate(plan, decision, reordered) !=
                    "projection-decision-mismatch")
                throw new InvalidOperationException("A probe, tampered or reordered projection passed.");
            long now = 0;
            var runtime = new ScriptedInstantRuntime("none", "none");
            var host = new CastingExecutionHost(
                settings => new InstantCastExecutor(runtime, true), () => now);
            var boundary = new NativeCastingDispatchBoundary(host, () => null);
            if (boundary.DispositionReason != "native-casting-enabled" ||
                boundary.Submit(plan, decision, "long", tampered).Submitted ||
                host.StartedRuns != 0)
                throw new InvalidOperationException("A refused projection started a run.");
            CastingDispatchOutcome started = boundary.Submit(plan, decision, "long", projection);
            if (!started.Submitted || host.StartedRuns != 1 ||
                boundary.DispositionReason != "native-casting-busy")
                throw new InvalidOperationException("The exact projection did not start one run.");
            host.Shutdown("fixture");
            if (boundary.DispositionReason != "native-casting-unavailable:fixture" ||
                runtime.Fired.Count != 0)
                throw new InvalidOperationException("Shutdown before the first pump misbehaved.");
        }

        // One run at a time, pumped per frame, reported exactly once with
        // per-casting states; a failed earlier casting stops every later
        // submission; skipped and omitted castings are disclosed.
        private static void TestExecutionHostRunsAndHalts()
        {
            ExplicitCastingPlan plan;
            CastingApplyDecision decision;
            ExplicitStepConversion projection = HostFixture(out plan, out decision, true);
            if (!projection.Converted || projection.CastingIds.Count != 2)
                throw new InvalidOperationException("Host fixture projection failed: " +
                    projection.Refusal);
            long now = 0;
            var runtime = new ScriptedInstantRuntime("none", "none");
            ExecutionProfile seen = null;
            var host = new CastingExecutionHost(settings =>
            {
                seen = settings;
                return new InstantCastExecutor(runtime, true);
            }, () => now);
            var reports = new List<CastingRunReport>();
            host.RunCompleted = completed => reports.Add(completed);
            var requested = new ExecutionProfile
            {
                Mode = "instant", AllowAnimatedFallback = false, OutOfCombatOnly = true,
                RecastExisting = false
            };
            CastingDispatchOutcome started = host.Start(plan, decision, "long", projection, requested);
            if (!started.Submitted || started.Reason != "run-started:run-1" || !host.IsRunning ||
                !ReferenceEquals(seen, requested))
                throw new InvalidOperationException("The host did not start the run.");
            CastingDispatchOutcome busy = host.Start(plan, decision, "long", projection, requested);
            if (busy.Submitted ||
                !busy.Reason.StartsWith("execution-in-progress:run-1", StringComparison.Ordinal))
                throw new InvalidOperationException("A second run started while one was active.");
            int guard = 0;
            while (host.IsRunning && guard++ < 1000) host.Pump();
            if (host.IsRunning || reports.Count != 1 || host.ReportedRuns != 1 ||
                !ReferenceEquals(host.LastReport, reports[0]))
                throw new InvalidOperationException("The run was not reported exactly once.");
            CastingRunReport report = reports[0];
            string states = string.Join(",", report.Entries
                .Select(entry => entry.CastingId + "=" + entry.State).ToArray());
            if (!report.Succeeded || report.Planned != 2 || report.Confirmed != 2 ||
                report.Skipped != 1 || report.Omitted != 1 || report.ResourcesSpent != 2 ||
                report.TerminalReason != "completed" ||
                states != "cast-1=EffectConfirmed,cast-2=EffectConfirmed,cast-3=Skipped,cast-4=Omitted" ||
                !runtime.Fired.SequenceEqual(new[] { "cast-1", "cast-2" }))
                throw new InvalidOperationException("The run report is wrong: " + states);
            var failing = new ScriptedInstantRuntime("cast-1", "rejected");
            var halting = new CastingExecutionHost(
                settings => new InstantCastExecutor(failing, true), () => now);
            halting.Start(plan, decision, "long", projection, null);
            guard = 0;
            while (halting.IsRunning && guard++ < 1000) halting.Pump();
            CastingRunReport halted = halting.LastReport;
            if (halted == null || halted.Succeeded || !halted.Halted || halted.Cancelled ||
                !halted.TerminalReason.StartsWith("halted:", StringComparison.Ordinal) ||
                halted.Entries[0].State != CastingOutcomeState.Failed ||
                halted.Entries[1].State != CastingOutcomeState.NotProcessed ||
                failing.Fired.Contains("cast-2") || failing.Validated.Contains("cast-2"))
                throw new InvalidOperationException("A failed casting did not stop later submissions.");
            var broken = new CastingExecutionHost(settings =>
            {
                throw new InvalidOperationException("no-adapter");
            }, () => now);
            CastingDispatchOutcome refused = broken.Start(plan, decision, "long", projection, null);
            if (refused.Submitted || broken.IsRunning || broken.StartedRuns != 0 ||
                !refused.Reason.StartsWith("executor-unavailable:InvalidOperationException:no-adapter",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("A missing executor started a run.");
        }

        // Every stop path goes through the one terminal: the in-flight
        // executor cleans up, later castings never start, the report says
        // why, and after Shutdown nothing starts until Resume.
        private static void TestExecutionHostCancelDeadlineShutdown()
        {
            ExplicitCastingPlan plan;
            CastingApplyDecision decision;
            ExplicitStepConversion projection = HostFixture(out plan, out decision, false);
            long now = 0;
            var pending = new ScriptedInstantRuntime("cast-1", "pending");
            var host = new CastingExecutionHost(
                settings => new InstantCastExecutor(pending, true), () => now);
            var reports = new List<CastingRunReport>();
            host.RunCompleted = report => reports.Add(report);
            host.Start(plan, decision, "long", projection, null);
            int guard = 0;
            while (pending.Fired.Count == 0 && guard++ < 100) host.Pump();
            if (!host.IsRunning || pending.Fired.Count != 1)
                throw new InvalidOperationException("The cancel fixture did not reach the in-flight cast.");
            if (!host.Cancel("player-stopped") || host.Cancel("again"))
                throw new InvalidOperationException("Cancel was not idempotent.");
            host.Pump();
            CastingRunReport stopped = reports.Single();
            if (!stopped.Cancelled || stopped.TerminalReason != "cancelled:player-stopped" ||
                stopped.Entries[0].State != CastingOutcomeState.Cancelled ||
                !stopped.Entries[0].Submitted ||
                stopped.Entries[1].State != CastingOutcomeState.NotProcessed ||
                !pending.Cleaned.Contains("cast-1") || pending.Fired.Contains("cast-2") ||
                host.IsRunning)
                throw new InvalidOperationException("The player stop misreported or continued.");
            var slow = new ScriptedInstantRuntime("cast-1", "pending");
            var timed = new CastingExecutionHost(
                settings => new InstantCastExecutor(slow, true), () => now);
            timed.Start(plan, decision, "long", projection, null);
            guard = 0;
            while (slow.Fired.Count == 0 && guard++ < 100) timed.Pump();
            now = CastingExecutionHost.BaseDeadlineMillis +
                2 * CastingExecutionHost.PerCastingDeadlineMillis + 1;
            timed.Pump();
            if (timed.IsRunning || timed.LastReport == null ||
                timed.LastReport.TerminalReason != "cancelled:deadline" ||
                !slow.Cleaned.Contains("cast-1") || slow.Fired.Contains("cast-2"))
                throw new InvalidOperationException("The run deadline did not end the run cleanly.");
            now = 0;
            var held = new ScriptedInstantRuntime("cast-1", "pending");
            var owned = new CastingExecutionHost(
                settings => new InstantCastExecutor(held, true), () => now);
            owned.Start(plan, decision, "long", projection, null);
            guard = 0;
            while (held.Fired.Count == 0 && guard++ < 100) owned.Pump();
            owned.Shutdown("mod-disabled");
            if (owned.IsRunning || owned.Accepting ||
                owned.LastReport.TerminalReason != "cancelled:mod-disabled" ||
                !held.Cleaned.Contains("cast-1"))
                throw new InvalidOperationException("Disabling the mod did not end the run cleanly.");
            CastingDispatchOutcome whileDown = owned.Start(plan, decision, "long", projection, null);
            if (whileDown.Submitted || whileDown.Reason != "native-casting-unavailable:mod-disabled")
                throw new InvalidOperationException("A run started while the mod was disabled.");
            owned.Resume();
            if (!owned.Start(plan, decision, "long", projection, null).Submitted)
                throw new InvalidOperationException("Resume did not re-enable runs.");
            owned.Shutdown("root-teardown");
            if (owned.LastReport.TerminalReason != "cancelled:root-teardown" ||
                owned.LastReport.CleanupFailures.Count != 0 ||
                owned.LastReport.Entries[0].Detail != "stopped-before-start")
                throw new InvalidOperationException("A run stopped before its first step misreported.");
        }

        // The whole production contract through the session: review, fresh
        // preflight, gate, exact projection, production boundary, host run,
        // result recorded on the session; a repeat press of the unchanged
        // accepted plan needs no ceremony; a second submission while one
        // runs is refused.
        private static void TestProductionApplyEndToEnd(string root)
        {
            string dir = Path.Combine(root, "production-apply");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            long now = 0;
            var runtime = new ScriptedInstantRuntime("none", "none");
            ExecutionProfile used = null;
            var host = new CastingExecutionHost(settings =>
            {
                used = settings;
                return new InstantCastExecutor(runtime, true);
            }, () => now);
            CastingWorkspaceSession session = null;
            var boundary = new NativeCastingDispatchBoundary(host, () => session.ExecutionSettings);
            session = new CastingWorkspaceSession(dir, "workspace-campaign", boundary);
            host.RunCompleted = completed => session.RecordRunReport(completed);
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
            Assert(AddDraftCasting(session, inputs, "unit-wizard", "unit-t2").Applied);
            if (session.Apply(CastingApplyMode.Ordinary, "long", inputs).ReviewReason !=
                    "nothing-presented" || host.StartedRuns != 0)
                throw new InvalidOperationException("An unreviewed plan reached the host.");
            session.SetExecutionMode("instant");
            session.PresentForReview(inputs);
            Assert(session.AcceptPresentedPlan(inputs));
            WorkspaceApplyResult applied = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (!applied.Allowed || applied.Dispatch == null || !applied.Dispatch.Submitted ||
                applied.Dispatch.Reason != "run-started:run-1" || host.StartedRuns != 1 ||
                used == null || used.Mode != "instant")
                throw new InvalidOperationException("The accepted plan did not start one run: " +
                    applied.ReviewReason);
            WorkspaceApplyResult overlapping = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (overlapping.Allowed ||
                !overlapping.ReviewReason.StartsWith("execution-in-progress:run-1", StringComparison.Ordinal))
                throw new InvalidOperationException("A second run was submitted while one was active.");
            int guard = 0;
            while (host.IsRunning && guard++ < 1000) host.Pump();
            CastingRunReport first = session.LastRunReport;
            if (first == null || !first.Succeeded || first.Confirmed != 2 ||
                !runtime.Fired.SequenceEqual(applied.Projection.CastingIds))
                throw new InvalidOperationException("The run result was not recorded on the session.");
            WorkspaceApplyResult again = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (!again.Allowed || host.StartedRuns != 2)
                throw new InvalidOperationException("Repeat use of the accepted plan demanded ceremony: " +
                    again.ReviewReason);
            guard = 0;
            while (host.IsRunning && guard++ < 1000) host.Pump();
            if (!ReferenceEquals(session.LastRunReport, host.LastReport) ||
                session.LastRunReport.RunId != "run-2")
                throw new InvalidOperationException("The repeat run was not recorded.");
            // Each card carries its own outcome in that run.
            List<WorkspaceCastingCard> cards = session.BuildView(inputs).Cards.ToList();
            if (cards.Count != 2 || cards.Any(card =>
                    card.LastRunOutcome != "cast, effect confirmed"))
                throw new InvalidOperationException("Cards did not show their last-run outcome: " +
                    string.Join(" | ", cards.Select(card => card.LastRunOutcome ?? "none").ToArray()));
        }

        private static void TestRunPresentation()
        {
            // Per-casting outcomes keep the effect and the resource apart.
            var outcomes = new[]
            {
                new { Entry = new CastingOutcomeEntry("c", CastingOutcomeState.EffectConfirmed, true, true, true, "ok"),
                    Text = "cast, effect confirmed" },
                new { Entry = new CastingOutcomeEntry("c", CastingOutcomeState.EffectConfirmed, true, true, true, "ok", true),
                    Text = "cast, effect confirmed (free)" },
                new { Entry = new CastingOutcomeEntry("c", CastingOutcomeState.Skipped, false, false, false, "already-active:u"),
                    Text = "skipped: the effect was already active" },
                new { Entry = new CastingOutcomeEntry("c", CastingOutcomeState.Failed, true, true, true, "effect-not-observed"),
                    Text = "failed (effect-not-observed); its resource was spent" },
                new { Entry = new CastingOutcomeEntry("c", CastingOutcomeState.Failed, true, false, false, "validation"),
                    Text = "failed (validation)" },
                new { Entry = new CastingOutcomeEntry("c", CastingOutcomeState.Cancelled, true, true, true, "stopped"),
                    Text = "interrupted when the run stopped; its resource was spent" },
                new { Entry = new CastingOutcomeEntry("c", CastingOutcomeState.NotProcessed, true, false, false, "stopped-before-start"),
                    Text = "not attempted: the run stopped earlier" }
            };
            foreach (var outcome in outcomes)
                if (CastingRunPresentation.DescribeEntry(outcome.Entry) != outcome.Text)
                    throw new InvalidOperationException("Outcome text for " + outcome.Entry.State +
                        " was: " + CastingRunPresentation.DescribeEntry(outcome.Entry));
            if (CastingRunPresentation.DescribeEntry(null) != null)
                throw new InvalidOperationException("A casting outside the run got an outcome.");
            Func<string, CastingOutcomeState, bool, bool, string, CastingOutcomeEntry> entry =
                (id, state, planned, spent, detail) => new CastingOutcomeEntry(id, state, planned,
                    planned && state != CastingOutcomeState.NotProcessed, spent, detail);
            var success = new CastingRunReport("run-1", "long", CastingApplyMode.Ordinary, "p",
                "completed", false, false, new[]
                {
                    entry("cast-1", CastingOutcomeState.EffectConfirmed, true, true, "ok"),
                    entry("cast-2", CastingOutcomeState.EffectConfirmed, true, true, "ok"),
                    entry("cast-3", CastingOutcomeState.Skipped, false, false, "already-active:unit-t3")
                }, new string[0]);
            string text = CastingRunPresentation.Describe(success, "Long", id => "Label " + id);
            if (text != "Long: 2 of 2 casts confirmed; 1 already active. Resources spent: 2.")
                throw new InvalidOperationException("Success text: " + text);
            var halted = new CastingRunReport("run-2", "long", CastingApplyMode.Ordinary, "p",
                "halted:FailedSubmission:boom", false, true, new[]
                {
                    entry("cast-1", CastingOutcomeState.Failed, true, true, "FailedSubmission:boom"),
                    entry("cast-2", CastingOutcomeState.NotProcessed, true, false, "halted")
                }, new string[0]);
            string failed = CastingRunPresentation.Describe(halted, "Long", id => "Label " + id);
            if (failed != "Long stopped after a cast failed: 0 of 2 casts confirmed; failed: Label cast-1 " +
                    "(FailedSubmission:boom); 1 not attempted. Resources spent: 1.")
                throw new InvalidOperationException("Halted text: " + failed);
            var cancelled = new CastingRunReport("run-3", "long", CastingApplyMode.Ordinary, "p",
                "cancelled:player-stopped", true, false, new[]
                {
                    new CastingOutcomeEntry("cast-1", CastingOutcomeState.EffectConfirmed, true,
                        true, true, "ok", true)
                }, new string[0]);
            string stoppedText = CastingRunPresentation.Describe(cancelled, "Long", null);
            if (!stoppedText.StartsWith("Long stopped (you stopped it): 1 of 1 cast confirmed",
                    StringComparison.Ordinal) ||
                !stoppedText.EndsWith("Resources spent: 0 (plus 1 free).", StringComparison.Ordinal))
                throw new InvalidOperationException("Cancelled text: " + stoppedText);
            if (CastingRunPresentation.ToQuickResult(success, "Long", null).Disposition !=
                    QuickExecutionDisposition.Completed ||
                CastingRunPresentation.ToQuickResult(halted, "Long", null).Disposition !=
                    QuickExecutionDisposition.Failed)
                throw new InvalidOperationException("Quick-result dispositions are wrong.");
            if (!CastingRunPresentation.DescribeRefusal("Long",
                    new WorkspaceApplyResult(false, "nothing-to-cast:2", null, null))
                    .Contains("nothing to cast") ||
                !CastingRunPresentation.DescribeRefusal("Long",
                    new WorkspaceApplyResult(false, "not-accepted", null, null))
                    .Contains("Accept Plan") ||
                !CastingRunPresentation.DescribeRefusal("Long",
                    new WorkspaceApplyResult(false, "material-change-requires-review", null, null))
                    .Contains("changed since you accepted"))
                throw new InvalidOperationException("Refusal explanations are wrong.");
        }

        // Unity-bound wiring, checked at source level: every routine route
        // reaches the casting-first pipeline when it is active (no legacy
        // bypass), the run host is pumped per frame and ended on disable,
        // area change and teardown, and a runtime-test session is locked to
        // the refusing boundary before any planner session exists.
        private static void TestProductionExecutionWiring()
        {
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName,
                    "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null)
                throw new InvalidOperationException("Repository root was not discoverable.");
            Func<string, string> source = relative => File.ReadAllText(Path.Combine(
                directory.FullName, "src", "KingmakerBuffPlanner", relative));
            string rootUi = source(Path.Combine("UI", "BuffPlannerUiRoot.cs"));
            string host = source(Path.Combine("RuntimeTesting", "RuntimeTestHost.cs"));
            string main = source("Main.cs");
            var required = new[]
            {
                "if (CastingFirstActive)\r\n                return StartCastingFirstRoutine(routineId, completed, CastingApplyMode.Ordinary);",
                "if (CastingFirstActive)\r\n                return StartCastingFirstRoutine(routineId, completed,\r\n                    CastingApplyMode.ReadyCastsOnly);",
                "if (CastingFirstActive)\r\n                return OpenCastingWorkspace();",
                "try { _castingHost.Pump(); }",
                "_instance._castingHost.Shutdown(\"mod-disabled\");",
                "_castingHost.Cancel(\"area-unloading\");",
                "_castingHost.Shutdown(\"root-teardown\");",
                "_castingHost.Shutdown(\"ui-root-disabled\");",
                "if (NativeCastingSessionPolicy.Locked)\r\n                return new DisabledCastingDispatchBoundary(NativeCastingSessionPolicy.LockReason);",
                "try { inputs = BuildFreshCastingWorkspaceInputs(); }",
                "_castingHost.RequestStop(CastingExecutionHost.PlayerStopReason);"
            };
            if (CastingExecutionHost.PlayerStopReason != "player-stopped")
                throw new InvalidOperationException("The player stop reason changed.");
            // The disable step expects the reason the root's own disable ends
            // a run with (the literal above).
            if (!string.Equals(CastingQualificationDriver.DisableReason, "mod-disabled", StringComparison.Ordinal))
                throw new InvalidOperationException("The disable reason no longer matches the root's disable.");
            foreach (string fragment in required)
                if (rootUi.Replace("\r\n", "\n").IndexOf(fragment.Replace("\r\n", "\n"),
                        StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("UI root wiring missing: " + fragment);
            if (rootUi.Contains("UIUtility.SendWarning") || rootUi.Contains("ExecuteLegacyRoutine"))
                throw new InvalidOperationException("A floating result or legacy route came back.");
            // The classic routine execution refuses under the session lock
            // before anything is spent or submitted.
            string classic = source(Path.Combine("UI", "PlannerUiSession.cs"));
            // The lock (with its one allowance-bound single-use exception for
            // exactly the approved plan) precedes any spend or executor.
            int classicLock = classic.Replace("\r\n", "\n").IndexOf(
                "if (NativeCastingSessionPolicy.Locked &&\n                !NativeCastingSessionPolicy.TryConsumeClassicGrant(routineId,\n                    Execution.ClassicPlanDigest.Of(preview.Plan), Model.Profile.Execution.Mode,",
                StringComparison.Ordinal);
            classic = classic.Replace("\r\n", "\n");
            int classicSpend = classic.IndexOf("_review.Spent(routineId);", StringComparison.Ordinal);
            int classicExecutor = classic.IndexOf("ICastExecutor executor;", StringComparison.Ordinal);
            if (classicLock < 0 || classicSpend < 0 || classicExecutor < 0 ||
                classicLock > classicSpend || classicLock > classicExecutor)
                throw new InvalidOperationException("The classic routine execution is not locked in automation.");
            int lockAt = host.IndexOf("UI.NativeCastingSessionPolicy.LockForRuntimeTest(request.Scenario);",
                StringComparison.Ordinal);
            int createAt = host.IndexOf("return new RuntimeTestHost(request, modEntry, log);",
                StringComparison.Ordinal);
            if (lockAt < 0 || createAt < 0 || lockAt > createAt)
                throw new InvalidOperationException("The runtime-test lock is not taken before the host.");
            if (!main.Contains("DrawPlannerMode();") ||
                !main.Contains("BuffPlannerUiRoot.TrySetPlannerMode("))
                throw new InvalidOperationException("The planner-mode activation panel is missing.");
            NativeCastingSessionPolicy.LockForRuntimeTest("fixture-scenario");
            if (!NativeCastingSessionPolicy.Locked || NativeCastingSessionPolicy.LockReason !=
                    "native-submission-disabled:runtime-test-session:fixture-scenario" ||
                new DisabledCastingDispatchBoundary(NativeCastingSessionPolicy.LockReason)
                    .DispositionReason != NativeCastingSessionPolicy.LockReason ||
                new DisabledCastingDispatchBoundary("anything-else").DispositionReason !=
                    "native-submission-disabled:no-qualified-casting-first-executor")
                throw new InvalidOperationException("The session lock or its refusal reason is wrong.");
        }

        // The advanced copy's request carries its own profile and that
        // profile's fifteen optional mods (the launcher builds them from
        // compatibility/profiles/advanced-gunslinger-0136.json).
        private static void UseAdvancedProfile(Dictionary<string, object> request, int modCount = 15)
        {
            request["profileId"] = RuntimeTestProtocol.AdvancedProfileId;
            request["expectedOptionalMods"] = Enumerable.Range(0, modCount).Select(index =>
                (object)new Dictionary<string, object>
                {
                    { "ummId", "Fixture" + index }, { "version", "1.0" },
                    { "assemblyName", "Fixture" + index + ".dll" },
                    { "assemblySha256", new string((char)('a' + index % 6), 64) }
                }).ToArray();
        }

        // The inspection scenario is a no-input, non-casting workspace
        // scenario; the advanced family is accepted only for non-casting
        // scenarios, and a mixed pair is always refused.
        private static void TestInspectionAndFixtureFamilyRequests(string root)
        {
            if (!RuntimeTestProtocol.IsInspectionScenario("live-advanced-inspect") ||
                !RuntimeTestProtocol.IsWorkspaceScenario("live-advanced-inspect") ||
                !RuntimeTestProtocol.IsNoInputWorkspaceScenario("live-advanced-inspect") ||
                !RuntimeTestProtocol.IsLiveUiScenario("live-advanced-inspect") ||
                RuntimeTestProtocol.IsProbeScenario("live-advanced-inspect") ||
                RuntimeTestProtocol.IsCastingProbeScenario("live-advanced-inspect") ||
                !RuntimeTestProtocol.IsAdvancedFamilyScenario("live-workspace-manual") ||
                RuntimeTestProtocol.IsAdvancedFamilyScenario("live-cast-probe-select") ||
                RuntimeTestProtocol.IsAdvancedFamilyScenario("live-cast-probe") ||
                RuntimeTestProtocol.IsAdvancedFamilyScenario("live-ui-bootstrap"))
                throw new InvalidOperationException("Inspection or family classification is wrong.");
            Func<string, string, string, Action<Dictionary<string, object>>> set =
                (scenario, workingFamily, baselineFamily) => o =>
                {
                    o["scenario"] = scenario;
                    o["parameters"] = new Dictionary<string, object>
                    {
                        { "workingSaveName", workingFamily + "_WORKING" },
                        { "workingFileName", "Manual_412_" + workingFamily + "_WORKING.zks" },
                        { "workingSha256", new string('a', 64) },
                        { "baselineSaveName", baselineFamily + "_BASELINE" },
                        { "baselineFileName", "Manual_411_" + baselineFamily + "_BASELINE.zks" },
                        { "baselineSha256", new string('b', 64) },
                        { "expectedGameName", "Advanced Campaign" },
                        { "expectedGameId", "66666666-7777-8888-9999-000000000000" },
                        { "executionMode", "instant" }
                    };
                    if (workingFamily == "KBP_ADVANCED") UseAdvancedProfile(o);
                };
            var accepted = new[]
            {
                new KeyValuePair<string, string>("live-advanced-inspect", "KBP_ADVANCED"),
                new KeyValuePair<string, string>("live-advanced-inspect", "KBP_AUTOMATION"),
                new KeyValuePair<string, string>("live-workspace-qual", "KBP_ADVANCED"),
                new KeyValuePair<string, string>("live-workspace-reload", "KBP_AUTOMATION"),
                new KeyValuePair<string, string>("live-workspace-import", "KBP_AUTOMATION")
            };
            foreach (KeyValuePair<string, string> item in accepted)
            {
                string path = WriteRequest(root, "family-ok-" + item.Key + "-" + item.Value,
                    set(item.Key, item.Value, item.Value));
                string rejection;
                RuntimeTestRequest request = ReadProtocol(
                    new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
                if (request == null || rejection.Length != 0)
                    throw new InvalidOperationException("A valid family request was refused: " +
                        item.Key + "/" + item.Value + ":" + rejection);
            }
            var refused = new[]
            {
                new KeyValuePair<string, Action<Dictionary<string, object>>>("advanced-probe-select",
                    set("live-cast-probe-select", "KBP_ADVANCED", "KBP_ADVANCED")),
                new KeyValuePair<string, Action<Dictionary<string, object>>>("advanced-probe",
                    set("live-cast-probe", "KBP_ADVANCED", "KBP_ADVANCED")),
                new KeyValuePair<string, Action<Dictionary<string, object>>>("advanced-bootstrap",
                    set("live-ui-bootstrap", "KBP_ADVANCED", "KBP_ADVANCED")),
                new KeyValuePair<string, Action<Dictionary<string, object>>>("advanced-reload",
                    set("live-workspace-reload", "KBP_ADVANCED", "KBP_ADVANCED")),
                new KeyValuePair<string, Action<Dictionary<string, object>>>("advanced-import",
                    set("live-workspace-import", "KBP_ADVANCED", "KBP_ADVANCED")),
                new KeyValuePair<string, Action<Dictionary<string, object>>>("mixed-pair",
                    set("live-advanced-inspect", "KBP_ADVANCED", "KBP_AUTOMATION")),
                new KeyValuePair<string, Action<Dictionary<string, object>>>("unknown-family",
                    set("live-advanced-inspect", "KBP_OTHER", "KBP_OTHER"))
            };
            foreach (KeyValuePair<string, Action<Dictionary<string, object>>> item in refused)
            {
                string path = WriteRequest(root, "family-bad-" + item.Key, item.Value);
                string rejection;
                RuntimeTestRequest request = ReadProtocol(
                    new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
                if (request != null || string.IsNullOrEmpty(rejection))
                    throw new InvalidOperationException("A refused family request was accepted: " +
                        item.Key);
                // Review of 1332ed8..542cd66, P2-5: an advanced save with a
                // scenario outside the family is refused for exactly that.
                if (item.Key.StartsWith("advanced-", StringComparison.Ordinal) &&
                    !rejection.Contains("live-save-family-scenario"))
                    throw new InvalidOperationException("The advanced-family refusal is for the wrong reason: " +
                        item.Key + ":" + rejection);
            }
            // The advanced copy runs only under its own profile, and that
            // profile only with the advanced copy (mission batch 3, section 5).
            var byReason = new[]
            {
                Tuple.Create("advanced-copy-full-user", (Action<Dictionary<string, object>>)(o =>
                {
                    set("live-advanced-inspect", "KBP_ADVANCED", "KBP_ADVANCED")(o);
                    o["profileId"] = "full-user";
                }), "live-save-family-profile"),
                Tuple.Create("automation-copy-advanced-profile", (Action<Dictionary<string, object>>)(o =>
                {
                    set("live-workspace-qual", "KBP_AUTOMATION", "KBP_AUTOMATION")(o);
                    UseAdvancedProfile(o);
                }), "live-save-family-profile"),
                Tuple.Create("advanced-profile-without-copy",
                    (Action<Dictionary<string, object>>)(o => UseAdvancedProfile(o)),
                    "advanced-profile-without-advanced-copy"),
                Tuple.Create("advanced-profile-mod-count", (Action<Dictionary<string, object>>)(o =>
                {
                    set("live-advanced-inspect", "KBP_ADVANCED", "KBP_ADVANCED")(o);
                    UseAdvancedProfile(o, 14);
                }), "profile-mod-expectation")
            };
            foreach (Tuple<string, Action<Dictionary<string, object>>, string> item in byReason)
            {
                string path = WriteRequest(root, "family-profile-" + item.Item1, item.Item2);
                string rejection;
                RuntimeTestRequest request = ReadProtocol(
                    new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
                if (request != null || rejection == null || !rejection.Contains(item.Item3))
                    throw new InvalidOperationException("The advanced profile case " + item.Item1 +
                        " was not refused for " + item.Item3 + ": " + rejection);
            }
        }

        // Before execution the card says what will happen: already active
        // (skipped), weaker/expiring (recast), or a contract this version
        // cannot execute yet.
        private static void TestCardDisclosesLimitsAndExistingEffects(string root)
        {
            string dir = Path.Combine(root, "card-disclosure");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign");
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
            var live = new CastingWorkspaceInputs(inputs.Snapshot, inputs.ProviderOptions,
                inputs.EffectsBySource, inputs.Enhancements, null,
                LiveEffects(On("unit-t1", "buff-effect", null, 1, null)));
            WorkspaceCastingCard card = session.BuildView(live).Cards.Single();
            if (card.StatusLabel != "Already active" || card.ExecutionLimitation != null ||
                card.ExistingEffectNotes.Count != 1 ||
                CastingRunPresentation.DescribeExistingEffectNote(card.ExistingEffectNotes[0]) !=
                    "already active on unit-t1 (skipped)")
                throw new InvalidOperationException("The card did not disclose the skip: " +
                    card.StatusLabel + "|" + string.Join(",", card.ExistingEffectNotes.ToArray()));
            if (session.BuildView(inputs).Cards.Single().StatusLabel != "Ready")
                throw new InvalidOperationException("Without live effects the card was not Ready.");
            if (CastingRunPresentation.DescribeExistingEffectNote(
                    "existing-insufficient:unit-t1:weaker-caster-level:4<10:buff-effect") !=
                    "present on unit-t1 but weaker (lower caster level) (will recast)" ||
                CastingRunPresentation.DescribeExistingEffectNote(
                    "existing-insufficient:unit-t2:remaining-duration-short:10<3000:buff-effect") !=
                    "present on unit-t2 but about to expire (will recast)" ||
                CastingRunPresentation.DescribeExistingEffectNote("existing-active-recast:unit-t3") !=
                    "active on unit-t3 but set to always recast")
                throw new InvalidOperationException("Existing-effect wording is wrong.");
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot party = CastingParty(CastingBuffAbility,
                out options, out enhancements, new[] { "unit-t1", "unit-t2" }, 3);
            PlannedCasting shared = DirectCasting("cast-share", "long", "unit-cleric",
                "unit-t1", "source-bulls", CastingBuffAbility);
            shared = new PlannedCasting(shared.CastingId, shared.RoutineId, 0,
                shared.SourceId, shared.Ability, shared.CasterUnitId, null,
                shared.TargetMode, shared.DirectTargetUnitId, null, null,
                new[] { new TargetingModifierSelection("share", true, null) }, null,
                shared.ExistingEffectPolicy, null, shared.State, null);
            ResolvedCasting modified = new ExplicitCastingCompiler().Compile(
                CastingDocument(shared), party, options,
                CastingEffects("source-bulls", "source-communal"), enhancements, null,
                new ICastingTargetingModifier[]
                {
                    new FixtureShareCastingModifier("unit-cleric", new[] { "unit-t1", "unit-t2" })
                }).CastingById("cast-share");
            string limitation = ExplicitCastingStepConverter.StandardExecutionLimitation(modified);
            if (!modified.IsExecutable || limitation == null ||
                !limitation.StartsWith("unsupported-contract:targeting-modifier:cast-share",
                    StringComparison.Ordinal) ||
                !CastingRunPresentation.DescribeLimitation(limitation).Contains("Share Transmutation"))
                throw new InvalidOperationException("A modifier casting did not disclose its limit: " +
                    (limitation ?? "none"));
        }

        // Without a floating result, feedback is the planner itself: an
        // actionable refusal opens it with the reason; informational ones
        // do not; the footer shows the last attempt until a run reports.
        private static void TestRefusalFeedbackRules(string root)
        {
            string[] opens = { "nothing-presented", "not-accepted", "material-change-requires-review",
                "apply-refused:blocked-casting:cast-1:x", "import-notices-pending:1",
                "legacy-import-unresolved:x", "execution-projection-refused:unsupported-contract:x" };
            string[] quiet = { "nothing-to-cast:2", "execution-in-progress:run-3",
                "native-submission-disabled:runtime-test-session:x", "native-casting-unavailable:mod-disabled",
                "submission-already-in-flight", string.Empty };
            if (opens.Any(reason => !CastingRunPresentation.OpensPlanner(reason)) ||
                quiet.Any(reason => CastingRunPresentation.OpensPlanner(reason)))
                throw new InvalidOperationException("Planner-opening refusal rules are wrong.");
            string dir = Path.Combine(root, "refusal-feedback");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "workspace-campaign");
            session.RecordAttempt("Long was not cast: open the planner.");
            if (session.LastAttemptMessage != "Long was not cast: open the planner.")
                throw new InvalidOperationException("The last attempt was not recorded.");
            session.RecordRunReport(new CastingRunReport("run-1", "long", CastingApplyMode.Ordinary,
                "p", "completed", false, false, new CastingOutcomeEntry[0], new string[0]));
            if (session.LastAttemptMessage != null || session.LastRunReport == null)
                throw new InvalidOperationException("A run report did not supersede the last attempt.");
        }

        // A stop observed between castings (progress == 1) lands between
        // them: the first is confirmed, the next never starts.
        private static void TestHostStopsBetweenCastings()
        {
            ExplicitCastingPlan plan;
            CastingApplyDecision decision;
            ExplicitStepConversion projection = HostFixture(out plan, out decision, false);
            long now = 0;
            var runtime = new ScriptedInstantRuntime("none", "none");
            var host = new CastingExecutionHost(
                settings => new InstantCastExecutor(runtime, true), () => now);
            if (host.ActiveFinishedCastings != -1)
                throw new InvalidOperationException("Idle progress was not -1.");
            host.Start(plan, decision, "long", projection, null);
            int guard = 0;
            while (host.IsRunning && host.ActiveFinishedCastings < 1 && guard++ < 1000) host.Pump();
            if (!host.IsRunning || host.ActiveFinishedCastings != 1)
                throw new InvalidOperationException("The run did not pause between castings.");
            host.Cancel("qualification-stop");
            CastingRunReport report = host.LastReport;
            if (report == null || !report.Cancelled ||
                report.TerminalReason != "cancelled:qualification-stop" ||
                report.Entries[0].State != CastingOutcomeState.EffectConfirmed ||
                report.Entries[1].State != CastingOutcomeState.NotProcessed ||
                report.Entries[1].Submitted ||
                !runtime.Fired.SequenceEqual(new[] { "cast-1" }) ||
                runtime.Validated.Contains("cast-2"))
                throw new InvalidOperationException("A stop between castings started the next one.");
        }

        // The player stop waits for the cast in progress: that casting
        // completes (effect confirmed), the next never starts, and a stop
        // requested before the first pump ends the run at once.
        private static void TestHostPlayerStopIsGraceful()
        {
            ExplicitCastingPlan plan;
            CastingApplyDecision decision;
            ExplicitStepConversion projection = HostFixture(out plan, out decision, false);
            long now = 0;
            // Animated casts span several pumps, so a stop can arrive while
            // the first one is in flight.
            var runtime = new ScriptedAnimatedRuntime("none", "none");
            var host = new CastingExecutionHost(
                settings => new AnimatedCastExecutor(runtime, true), () => now);
            host.Start(plan, decision, "long", projection, null);
            if (!host.RequestStop("player-stopped") || host.IsRunning ||
                host.LastReport == null ||
                host.LastReport.TerminalReason != "cancelled:player-stopped" ||
                host.LastReport.Entries.Any(entry => entry.Submitted) ||
                runtime.Started.Count != 0)
                throw new InvalidOperationException("A stop before the first pump did not end the run at once.");
            host.Start(plan, decision, "long", projection, null);
            if (host.ActiveCastingInFlight || host.ActiveStopRequested != null)
                throw new InvalidOperationException("A run not pumped yet reported a cast in progress.");
            host.Pump();
            if (!host.IsRunning || host.ActiveFinishedCastings != 0 || !host.ActiveCastingInFlight)
                throw new InvalidOperationException(
                    "The fixture did not leave the first cast in progress after one pump.");
            if (!host.RequestStop("player-stopped") || !host.IsRunning ||
                host.ActiveStopRequested != "player-stopped")
                throw new InvalidOperationException("The player stop interrupted the cast in progress.");
            int guard = 0;
            while (host.IsRunning && guard++ < 1000) host.Pump();
            CastingRunReport report = host.LastReport;
            if (host.IsRunning || report == null || !report.Cancelled ||
                report.TerminalReason != "cancelled:player-stopped" ||
                report.Entries[0].State != CastingOutcomeState.EffectConfirmed ||
                report.Entries[1].State != CastingOutcomeState.NotProcessed ||
                report.Entries[1].Submitted ||
                !runtime.Started.SequenceEqual(new[] { "cast-1" }))
                throw new InvalidOperationException("The player stop did not land after the cast in progress: " +
                    (report == null ? "no report" : report.TerminalReason));
            if (host.RequestStop("player-stopped"))
                throw new InvalidOperationException("A stop without a run reported success.");
            var resting = new ScriptedAnimatedRuntime("none", "none");
            var restingHost = new CastingExecutionHost(
                settings => new AnimatedCastExecutor(resting, true), () => now);
            restingHost.Start(plan, decision, "long", projection, null);
            guard = 0;
            while (restingHost.IsRunning && restingHost.ActiveFinishedCastings < 1 && guard++ < 1000)
                restingHost.Pump();
            if (!restingHost.IsRunning || restingHost.ActiveFinishedCastings != 1 ||
                restingHost.ActiveCastingInFlight)
                throw new InvalidOperationException("A run resting between castings reported a cast in progress.");
            restingHost.Cancel("test-end");
        }

        // A cast submitted while the world was held (paused, dialog, or the
        // planner full-screen window) is never a valid probe: the rule is
        // queued and cannot be confirmed (casting-probe-cast-20260923-p1-01).
        // The host must also never submit before the world runs (source).
        private static void TestProbeWorldHeldViolation()
        {
            Func<bool, SingleCastProbeRunRecord> record = running => new SingleCastProbeRunRecord
            {
                CastingScenario = true, Selected = true, SelectionEvidence = "fixture",
                ProjectionId = "p", AllowanceStatus = "valid", Submitted = true,
                BoundaryDisposed = true, CleanupRecorded = true, WorkspaceClosed = true,
                InputLeaseReleased = true, WorldRunningAtSubmit = running,
                SubmitWorldState = running ? "mode=Default;paused=False" : "mode=FullScreenUi;paused=False"
            };
            if (!record(false).Violations().Contains("probe-submitted-while-world-held:mode=FullScreenUi;paused=False") ||
                record(true).Violations().Any(value => value.StartsWith("probe-submitted-while-world-held",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("The held-world probe rule is wrong.");
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("Repository root was not discoverable.");
            string host = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner",
                "RuntimeTesting", "RuntimeTestHost.cs"));
            int close = host.IndexOf("ProbeWorkspaceCloseResult closedForCast = CloseProbeWorkspace();", StringComparison.Ordinal);
            int worldCheck = host.IndexOf("_probeRecord.WorldRunningAtSubmit = true;", StringComparison.Ordinal);
            int boundary = host.IndexOf("var boundary = new SingleCastProbeBoundary(_probeAllowance,", StringComparison.Ordinal);
            if (close < 0 || worldCheck < 0 || boundary < 0 || !(close < worldCheck && worldCheck < boundary) ||
                host.IndexOf("new SingleCastProbeBoundary(", StringComparison.Ordinal) !=
                    host.LastIndexOf("new SingleCastProbeBoundary(", StringComparison.Ordinal))
                throw new InvalidOperationException("The probe host can submit before the planner closed and the world runs.");
            string root = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner",
                "UI", "BuffPlannerUiRoot.cs"));
            string tick = SourceBlock(root, "private void Tick(float deltaTime)");
            string pumped = tick == null ? null : SourceBlock(tick, "if (worldRuns)");
            if (!root.Contains("bool worldRuns = WorldRunsForCasting && _castingWorkspace == null && !_screen.IsOpen;") ||
                pumped == null || !pumped.Contains("_castingHost.Pump();") ||
                Occurrences(root, "_castingHost.Pump()") != 1 ||
                !tick.Contains("_castingWorldClock.Advance(worldRuns, deltaTime);") ||
                !root.Contains("() => _castingWorldClock.Milliseconds);") || root.Contains("_castingWorldMillis"))
                throw new InvalidOperationException("The production host is pumped, or its deadline counts, while the world is held.");
            // Review of e7c5207..f7726c9, P2-1 and P3-5: the probe waits in
            // elapsed time with nothing submitted while held, every pump of
            // its run passes the world state, and the qualification host
            // counts only running time.
            string await = SourceBlock(host, "private bool UpdateProbeAwaitWorld()");
            string held = await == null ? null : SourceBlock(await, "if (!running)");
            string heldTail = held == null ? null
                : held.Substring(held.LastIndexOf("return false;", StringComparison.Ordinal) + "return false;".Length).Trim();
            if (held == null || heldTail != "}" || held.Contains("Submit(") ||
                !held.Contains("RuntimeTestProtocol.ProbeWorldWaitSeconds * 1000L) return false;") ||
                await.Contains("_probeWorldWaitFrames < ") ||
                await.IndexOf("if (!running)", StringComparison.Ordinal) >
                    await.IndexOf("_probeOwner.Submit(", StringComparison.Ordinal))
                throw new InvalidOperationException("The probe can submit before the world runs, or waits in updates.");
            string probeRun = SourceBlock(host, "private bool UpdateProbeRun()");
            if (probeRun == null || !probeRun.Contains("bool running = ProbeWorldRuns(out state);") ||
                !probeRun.Contains("stop, running, state))") || Occurrences(host, "_probeOwner.Pump(") != 1)
                throw new InvalidOperationException("The probe run is pumped without the world state.");
            // The qualification runs on the planner's own host: its root pumps
            // it (only while the world runs, above) and counts its deadline;
            // the driver waits on it, and the stop is the root's routine
            // press through the HUD's own routine entry.
            string qualification = SourceBlock(host, "private bool UpdateQualification()");
            string press = SourceBlock(root, "internal static bool PressRoutineForRuntime(string routineId)");
            if (qualification == null ||
                !qualification.Contains("_qualificationHost = BuffPlannerUiRoot.CastingHostForRuntime;") ||
                !qualification.Replace("\r\n", "\n").Contains(
                    "() => BuffPlannerUiRoot.WorldRunsForCasting, true,\n                    BuffPlannerUiRoot.PressRoutineForRuntime, Main.SetEnabledForRuntime,\n                    () => \"subscriptions=\" + BuffPlannerUiRoot.ActiveEventSubscriptionsForRuntime +") ||
                qualification.Contains("new CastingExecutionHost(") || host.Contains("_qualificationHost.Pump(") ||
                host.Contains("_qualificationWorldClock") || press == null ||
                !press.Contains("_instance.ExecuteRoutineRequest(routineId)") ||
                !root.Contains("() => { OpenSetup(); }, routineId => ExecuteRoutineRequest(routineId),"))
                throw new InvalidOperationException("The qualification does not run on the planner's own pumped host.");
            // One mod update ticks the root (one pump) before the test host,
            // so the stop press precedes the next pump.
            string modMain = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "Main.cs"));
            string core = SourceBlock(modMain, "private static void OnUpdateCore(");
            int tickOwned = core == null ? -1 : core.IndexOf("BuffPlannerUiRoot.TickOwned(deltaTime);", StringComparison.Ordinal);
            int hostUpdate = core == null ? -1 : core.IndexOf("if (runtime == null || !runtime.Update()) return;", StringComparison.Ordinal);
            if (tickOwned < 0 || hostUpdate < 0 || tickOwned > hostUpdate ||
                Occurrences(core, "BuffPlannerUiRoot.TickOwned(") != 1 || Occurrences(core, "runtime.Update()") != 1)
                throw new InvalidOperationException("The root tick no longer precedes the test host in one mod update.");
            // Review B7: the held disable is the mod toggle's own (the root is
            // not ticked while disabled), without ending the runtime test.
            string runtimeToggle = SourceBlock(modMain, "internal static void SetEnabledForRuntime(bool value)");
            string toggle = SourceBlock(modMain, "private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)");
            if (runtimeToggle == null || toggle == null || !runtimeToggle.Contains("_enabled = value;") ||
                !runtimeToggle.Contains("BuffPlannerUiRoot.SetEnabled(value);") ||
                runtimeToggle.Contains("_runtimeTest") || !toggle.Contains("_enabled = value;") ||
                !toggle.Contains("BuffPlannerUiRoot.SetEnabled(value);") ||
                !core.Replace("\r\n", "\n").Contains(
                    "if (_enabled)\n            {\n                try\n                {\n                    BuffPlannerUiRoot.Ensure(_modPath, _log);"))
                throw new InvalidOperationException("The qualification disable is not the mod toggle's own.");
            // Re-review: the runtime toggle never enables a planner the mod
            // manager disabled; the hold observes the root's own ticks; the
            // optional mods are verified before any cast on every allowance.
            string tickOwnedBody = SourceBlock(root, "internal static void TickOwned(float deltaTime)");
            if (!runtimeToggle.Contains("if (value && !_managerEnabled)") ||
                !toggle.Contains("_managerEnabled = value;") ||
                tickOwnedBody == null || !tickOwnedBody.Contains("_ownedTicks++;") ||
                !qualification.Contains("() => BuffPlannerUiRoot.OwnedTicksForRuntime,") ||
                // Group casting reads: each expected recipient read natively.
                !qualification.Contains("new KingmakerProbeObserver().ObserveRecipient(") ||
                Occurrences(host, "OptionalModMismatch()") != 4)
                throw new InvalidOperationException("The hold or the pre-cast identity checks are not wired.");
            // The launcher's casting mode must be the allowance's, and the
            // acceptance counts the planner host's runs: none before the
            // qualification, exactly the boundary's submissions after it.
            string normalized = qualification.Replace("\r\n", "\n");
            string acceptance = host.Replace("\r\n", "\n");
            string mismatch = SourceBlock(normalized,
                "if (allowance != null && !string.Equals(allowance.ExecutionMode, requestedMode,\n                            StringComparison.Ordinal))");
            if (mismatch == null || !mismatch.Contains("allowance = null;") ||
                !mismatch.Contains("_qualificationRecord.AllowanceStatus = \"execution-mode-mismatch:\" +") ||
                !normalized.Contains("_qualificationRunsBefore = _qualificationHost.StartedRuns;") ||
                !acceptance.Contains("bool playerRoutesQuiet = UI.NativeCastingSessionPolicy.Locked &&\n                        _qualificationRunsBefore == 0 &&\n                        BuffPlannerUiRoot.CastingRunsStartedForRuntime == boundaryRuns;"))
                throw new InvalidOperationException("The live qualification no longer binds its mode or counts the host's runs.");
        }

        // The brace-balanced block after the first occurrence of a header,
        // for source checks of Unity-bound code that cannot run here.
        private static string SourceBlock(string text, string header)
        {
            int start = text.IndexOf(header, StringComparison.Ordinal);
            if (start < 0) return null;
            int open = text.IndexOf('{', start + header.Length);
            if (open < 0) return null;
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0) return text.Substring(open, i - open + 1);
            }
            return null;
        }

        private static int Occurrences(string text, string value)
        {
            int count = 0;
            for (int at = text.IndexOf(value, StringComparison.Ordinal); at >= 0;
                at = text.IndexOf(value, at + value.Length, StringComparison.Ordinal))
                count++;
            return count;
        }

        // Review of e7c5207..f7726c9, P2-1: the probe's rule fires on the
        // first pump after submit and its confirmation frames follow, so
        // every pump waits while the world is held; a stop and the
        // wall-clock deadline still apply while it is held.
        private static void TestProbeOwnerPumpsOnlyWhileTheWorldRuns()
        {
            var effect = new ProbeEffectInstance("buff-effect", "instance-2", 900);
            Func<string, ProbeSequenceClock, ProbeObservation> read = (phase, clock) =>
                phase == "before" ? Obs(phase, clock, -1) : Obs(phase, clock, -1, effect);
            ProbeOwnerRun held = StartProbeOwnerRun("unlimited", "free", read);
            for (int frame = 1; frame <= 50; frame++)
                if (held.Owner.Pump(frame, 60000, false, false, "mode=FullScreenUi;paused=False"))
                    throw new InvalidOperationException("A held world ended the probe run.");
            if ((held.Runtime != null && held.Runtime.Fired.Count != 0) || held.Published.Count != 0)
                throw new InvalidOperationException("The probe fired while the world was held.");
            int guard = 0;
            while (!held.Owner.Pump(100 + guard, 60000, false, true, "mode=Default;paused=False") && guard++ < 1000) { }
            SingleCastProbeRunRecord record = held.Published.Single();
            if (record.Violations().Count != 0 || held.Runtime.Fired.Count != 1 || record.HeldPumps != 50 ||
                record.FirstStepWorldState != "mode=Default;paused=False")
                throw new InvalidOperationException("The probe did not run once the world ran: " +
                    string.Join("|", record.Violations().ToArray()) + ";held=" + record.HeldPumps);
            // Held past the wall-clock deadline: the run ends there, unfired.
            ProbeOwnerRun late = StartProbeOwnerRun("unlimited", "free", read);
            if (late.Owner.Pump(30000, 60000, false, false, "held") ||
                !late.Owner.Pump(60001, 60000, false, false, "held"))
                throw new InvalidOperationException("A held world escaped the probe deadline.");
            if ((late.Runtime != null && late.Runtime.Fired.Count != 0) ||
                !late.Published.Single().Violations().Contains("probe-terminated:deadline"))
                throw new InvalidOperationException("The probe deadline over a held world is wrong.");
            // A stop while held ends the run at once.
            ProbeOwnerRun stopped = StartProbeOwnerRun("unlimited", "free", read);
            if (!stopped.Owner.Pump(1, 60000, true, false, "held") ||
                (stopped.Runtime != null && stopped.Runtime.Fired.Count != 0) ||
                !stopped.Published.Single().Violations().Contains("probe-terminated:stop"))
                throw new InvalidOperationException("A stop while held did not end the probe.");
        }

        // Review of e7c5207..f7726c9, P3-1: run deadlines count running game
        // time only and keep the fraction of a millisecond of every frame.
        private static void TestCastingWorldClock()
        {
            var sixty = new CastingWorldClock();
            for (int frame = 0; frame < 1000; frame++) sixty.Advance(true, 1.0 / 60.0);
            var fast = new CastingWorldClock();
            for (int frame = 0; frame < 5000; frame++) fast.Advance(true, 0.0005);
            var idle = new CastingWorldClock();
            idle.Advance(false, 5.0);
            idle.Advance(true, -1.0);
            idle.Advance(true, double.PositiveInfinity);
            idle.Advance(true, double.NaN);
            if (sixty.Milliseconds < 16665 || sixty.Milliseconds > 16667 ||
                fast.Milliseconds < 2499 || fast.Milliseconds > 2500 || idle.Milliseconds != 0)
                throw new InvalidOperationException("The world clock is wrong: " + sixty.Milliseconds + "/" +
                    fast.Milliseconds + "/" + idle.Milliseconds);
        }

        // An exhausted REQUIRED rod is a resource shortage: the run that
        // spent it left the effect active, so a repeat press skips instead
        // of blocking, with the same would-be cost shape (and so the same
        // review digest) as the accepted plan. An exhausted OPTIONAL rod
        // keeps its would-be demand in the skip shape too, while casting
        // without it (effect absent) is a visible material change.
        private static void TestExhaustedRodWithActiveEffect()
        {
            var compiler = new ExplicitCastingCompiler();
            Dictionary<string, EffectExpression> effects =
                CastingEffects("source-bulls", "source-communal");
            Func<int?, bool, ActiveEffectSnapshot, ExplicitCastingPlan> compile =
                (uses, required, live) =>
                {
                    List<ProviderPlanningOption> options;
                    PartyProviderSnapshot snapshot = LiveEffectParty(3, out options);
                    var rod = new CastEnhancementSnapshot("rod-extend", "unit-cleric",
                        "rod-guid", "Extend Rod", string.Empty,
                        CastEnhancementCategory.MetamagicRod, 8, 3, uses, null);
                    CastingPlanDocument document = CastingDocument(DirectCasting("cast-1", "long",
                        "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility,
                        new[] { new AuthoredEnhancementSelection("rod-extend", required, null) }));
                    return compiler.Compile(document, snapshot, options, effects, new[] { rod },
                        "long", null, false, live);
                };
            ActiveEffectSnapshot extendedActive =
                LiveEffects(On("unit-t1", "buff-effect", 7000, 10, 8));
            foreach (bool required in new[] { true, false })
            {
                string kind = required ? "required" : "optional";
                ExplicitCastingPlan readyPlan = compile(3, required, null);
                ResolvedCasting ready = readyPlan.CastingById("cast-1");
                if (ready.Readiness != ResolvedCastingReadiness.Ready ||
                    !ready.AppliedEnhancementIds.Contains("rod-extend") ||
                    ready.CostShape.Count != 2)
                    throw new InvalidOperationException("The " + kind + " rod did not apply: " +
                        string.Join(",", ready.ReadinessReasons.ToArray()) + " shape=" +
                        string.Join(",", ready.CostShape.ToArray()));
                ExplicitCastingPlan repeatPlan = compile(0, required, extendedActive);
                ResolvedCasting repeat = repeatPlan.CastingById("cast-1");
                if (repeat.Readiness != ResolvedCastingReadiness.AlreadySatisfied ||
                    !repeat.ReadinessReasons.SequenceEqual(new[] { "already-active:unit-t1" }) ||
                    !repeat.CostShape.SequenceEqual(ready.CostShape))
                    throw new InvalidOperationException("An exhausted " + kind +
                        " rod blocked or reshaped a casting whose effect is active: " +
                        repeat.Readiness + " " + string.Join(",", repeat.ReadinessReasons.ToArray()) +
                        " shape=" + string.Join(",", repeat.CostShape.ToArray()));
                if (!CastingPlanSignature.For(readyPlan, "long")
                        .Matches(CastingPlanSignature.For(repeatPlan, "long")))
                    throw new InvalidOperationException("The " + kind +
                        " rod repeat demanded a new review.");
            }
            ResolvedCasting requiredAbsent = compile(0, true, LiveEffects()).CastingById("cast-1");
            if (requiredAbsent.Readiness != ResolvedCastingReadiness.Blocked ||
                !requiredAbsent.ReadinessReasons.Any(reason => reason.StartsWith(
                    "enhancement-exhausted:rod-extend", StringComparison.Ordinal)))
                throw new InvalidOperationException("An exhausted required rod without the effect did not block.");
            // The active effect must carry the rod metamagic to satisfy a
            // casting that requires the rod.
            ResolvedCasting unextended = compile(0, true,
                LiveEffects(On("unit-t1", "buff-effect", 7000, 10, 0))).CastingById("cast-1");
            if (unextended.Readiness != ResolvedCastingReadiness.Blocked)
                throw new InvalidOperationException("An effect without the rod metamagic waived the rod.");
            ResolvedCasting optionalAbsent = compile(0, false, LiveEffects()).CastingById("cast-1");
            if (optionalAbsent.Readiness != ResolvedCastingReadiness.Ready ||
                optionalAbsent.AppliedEnhancementIds.Contains("rod-extend") ||
                optionalAbsent.CostShape.SequenceEqual(
                    compile(3, false, null).CastingById("cast-1").CostShape))
                throw new InvalidOperationException("Casting without the optional rod was not a visible change.");
            // Review P3-3: two required rods that can never be combined (one
            // exclusive group) stay blocked even when one is exhausted and
            // the effect carries both metamagics.
            List<ProviderPlanningOption> pairOptions;
            PartyProviderSnapshot pairSnapshot = LiveEffectParty(3, out pairOptions);
            var extendRod = new CastEnhancementSnapshot("rod-extend", "unit-cleric", "rod-guid",
                "Extend Rod", string.Empty, CastEnhancementCategory.MetamagicRod, 8, 3, 3, null,
                exclusiveGroupId: "one-rod");
            var empowerRod = new CastEnhancementSnapshot("rod-empower", "unit-cleric", "rod-guid-2",
                "Empower Rod", string.Empty, CastEnhancementCategory.MetamagicRod, 1, 3, 0, null,
                exclusiveGroupId: "one-rod");
            ResolvedCasting pair = compiler.Compile(CastingDocument(DirectCasting("cast-1", "long",
                    "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility,
                    new[]
                    {
                        new AuthoredEnhancementSelection("rod-extend", true, null),
                        new AuthoredEnhancementSelection("rod-empower", true, null)
                    })), pairSnapshot, pairOptions, effects, new[] { extendRod, empowerRod },
                "long", null, false, LiveEffects(On("unit-t1", "buff-effect", 7000, 10, 9)))
                .CastingById("cast-1");
            if (pair.Readiness != ResolvedCastingReadiness.Blocked ||
                !pair.ReadinessReasons.Contains("enhancement-incompatible"))
                throw new InvalidOperationException("An incompatible rod set was waived: " +
                    pair.Readiness + " " + string.Join(",", pair.ReadinessReasons.ToArray()));
        }

        // The grid's Spells / Abilities / Other tabs follow the classic
        // catalogue: a buff appears under every kind of provider it has,
        // and All shows everything. The session derives the kinds from the
        // buff's own discovered providers.
        private static void TestBuffGridSourceTypeTabs(string root)
        {
            Func<SourceKind[], WorkspaceSourceOption> option = kinds =>
                new WorkspaceSourceOption("source", "Buff", false, null, null, kinds);
            var cases = new[]
            {
                new { Kinds = new[] { SourceKind.Spellbook }, Expected = "Spells" },
                new { Kinds = new[] { SourceKind.AbilityResource }, Expected = "Abilities" },
                new { Kinds = new[] { SourceKind.Fact }, Expected = "Abilities" },
                new { Kinds = new[] { SourceKind.Item }, Expected = "Other" },
                new { Kinds = new[] { SourceKind.Item, SourceKind.Spellbook }, Expected = "Other,Spells" },
                new { Kinds = new SourceKind[0], Expected = "" }
            };
            foreach (var entry in cases)
            {
                WorkspaceSourceOption source = option(entry.Kinds);
                string actual = string.Join(",", new[]
                    {
                        PlannerSourceCategory.Abilities, PlannerSourceCategory.Other,
                        PlannerSourceCategory.Spells
                    }.Where(category => WorkspaceSourceLabels.MatchesCategory(source, category))
                    .Select(category => category.ToString()).ToArray());
                if (actual != entry.Expected ||
                    !WorkspaceSourceLabels.MatchesCategory(source, PlannerSourceCategory.All))
                    throw new InvalidOperationException("Kinds " +
                        string.Join("+", entry.Kinds.Select(kind => kind.ToString()).ToArray()) +
                        " matched " + actual + " instead of " + entry.Expected + ".");
            }
            CastingWorkspaceInputs inputs = QualificationInputs(true, true, null);
            var session = new CastingWorkspaceSession(Path.Combine(root, "source-tabs"), "campaign-tabs");
            WorkspaceView view = session.BuildView(inputs);
            if (view.Draft == null || view.Draft.Sources.Count == 0 ||
                view.Draft.Sources.Any(source => !source.SourceKinds.SequenceEqual(
                    new[] { SourceKind.Spellbook })))
                throw new InvalidOperationException("The session did not derive the spellbook kind.");
        }

        // Mission section 8 (save/reload): the reload scenario loads only the
        // descriptor the guarded main-menu chain proved, under a fresh
        // read-only saver installed BEFORE the only Game.LoadGame call, and
        // restores the native saver only after the header protocol
        // completed; the host reloads only with the planner closed and
        // judges the reopened plan and ownership (source checks: the loader
        // and host are game-bound).
        private static void TestInGameReloadIsGuarded()
        {
            if (!RuntimeTestProtocol.IsReloadScenario("live-workspace-reload") ||
                !RuntimeTestProtocol.IsWorkspaceScenario("live-workspace-reload") ||
                RuntimeTestProtocol.IsNoInputWorkspaceScenario("live-workspace-reload") ||
                RuntimeTestProtocol.IsAdvancedFamilyScenario("live-workspace-reload") ||
                RuntimeTestProtocol.IsReloadScenario("live-workspace-qual"))
                throw new InvalidOperationException("The reload scenario classification is wrong.");
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("Repository root was not discoverable.");
            string testing = Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "RuntimeTesting");
            string loader = File.ReadAllText(Path.Combine(testing, "LiveCampaignSaveLoader.cs"));
            string begin = SourceBlock(loader, "internal void BeginGuardedReload()");
            int guard = begin == null ? -1 : begin.IndexOf("descriptor.Saver = _reloadSaver;", StringComparison.Ordinal);
            int load = begin == null ? -1 : begin.IndexOf("Game.Instance.LoadGame(descriptor);", StringComparison.Ordinal);
            if (begin == null || !begin.Contains("if (!IsComplete)") || !begin.Contains("if (_reloadStarted)") ||
                !begin.Contains("var descriptor = _workingDescriptor as Kingmaker.EntitySystem.Persistence.SaveInfo;") ||
                !begin.Contains("_reloadSaver = new GuardedReadOnlySaver(descriptor,") ||
                guard < 0 || load < 0 || guard > load)
                throw new InvalidOperationException("The in-game reload is not guarded by the read-only saver.");
            string update = SourceBlock(loader, "private string AdvanceReload(bool areaReloaded)");
            int complete = update == null ? -1 : update.IndexOf("if (!_reloadSaver.Complete || !_reloadCallback) return null;", StringComparison.Ordinal);
            int restore = update == null ? -1 : update.IndexOf("descriptor.Saver = _reloadSaver.Native;", StringComparison.Ordinal);
            int waitArea = update == null ? -1 : update.IndexOf("if (!areaReloaded) return null;", StringComparison.Ordinal);
            int read = update == null ? -1 : update.IndexOf("string fingerprint = CurrentFingerprint(true);", StringComparison.Ordinal);
            if (update == null || complete < 0 || restore < 0 || complete > restore ||
                !update.Contains("if (!ReferenceEquals(descriptor.Saver, _reloadSaver))") ||
                waitArea < 0 || read < 0 || waitArea > read ||
                !loader.Contains("string value = CurrentFingerprint(false);") ||
                !loader.Contains("if (waitWhileLoading && partyCount <= 0) return null;"))
                throw new InvalidOperationException("The reload restores the native saver early or reads the campaign mid-load.");
            string wrapper = SourceBlock(loader, "internal string UpdateReload(bool areaReloaded)");
            if (wrapper == null || !wrapper.Contains("FailReload(") || !wrapper.Contains("throw;"))
                throw new InvalidOperationException("A failed reload does not leave its events.");
            // Review of 1332ed8..542cd66, P1-1: write sentinels and a
            // Game.LoadGame correlation hook cover the reload window; writes,
            // a foreign or repeated load, or a missing correlation fail it;
            // the hooks are removed on success and on failure.
            int sentinels = begin.IndexOf("InstallReloadSentinels();", StringComparison.Ordinal);
            string fail = SourceBlock(loader, "internal void FailReload(string reason)");
            string sentinelInstall = SourceBlock(loader, "private void InstallReloadSentinels()");
            if (sentinels < 0 || sentinels > load || fail == null || !fail.Contains("RemoveHooks();") ||
                !fail.Contains("WriteEventsEvidence();") || sentinelInstall == null ||
                !sentinelInstall.Contains("\"SaveRoutine\"") || !sentinelInstall.Contains("\"SaveStashedArea\"") ||
                !sentinelInstall.Contains("\"LoadGame\"") ||
                !update.Contains("if (_writeObserved)") ||
                !update.Contains("if (_reloadLoadGameCalls > 1 || (_reloadLoadGameCalls == 1 && !_reloadLoadGameCorrelated))") ||
                !update.Contains("if (_reloadLoadGameCalls != 1 || !_reloadLoadGameCorrelated)") ||
                Occurrences(update, "RemoveHooks();") != 1)
                throw new InvalidOperationException("The reload window is not guarded by the write sentinels and the load correlation.");
            int loads = 0;
            foreach (string file in Directory.GetFiles(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner"),
                "*.cs", SearchOption.AllDirectories))
                if (!file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                    loads += Occurrences(File.ReadAllText(file), "Game.Instance.LoadGame(");
            if (loads != 1)
                throw new InvalidOperationException("Game.LoadGame is called outside the guarded reload: " + loads);
            string host = File.ReadAllText(Path.Combine(testing, "RuntimeTestHost.cs"));
            string reload = SourceBlock(host, "if (_liveUiPhase == 80)");
            int closed = reload == null ? -1 : reload.IndexOf("if (BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime) return false;", StringComparison.Ordinal);
            int started = reload == null ? -1 : reload.IndexOf("_liveSaveLoader.BeginGuardedReload();", StringComparison.Ordinal);
            if (reload == null || !reload.Contains("BuffPlannerUiRoot.CloseCastingWorkspaceForRuntime();") ||
                closed < 0 || started < 0 || closed > started ||
                !host.Contains("_liveUiPhase = RuntimeTestProtocol.IsReloadScenario(_request.Scenario) && reloadable ? 80 : 21;") ||
                !host.Contains("_workspaceReloadEvidence = \"passed=False;skipped:interaction-or-reopen-failed\";") ||
                Occurrences(host, "BeginGuardedReload()") != 1)
                throw new InvalidOperationException("The host can reload with the planner open, or outside the reload scenario.");
            string verify = SourceBlock(host, "private string VerifyWorkspaceReload()");
            if (verify == null || !verify.Contains("bool passed = preserved && diskPreserved && campaign && _reloadSubscriptionsBefore == 1 &&") ||
                !verify.Contains("subscriptions == 1 && _reloadHudRootsBefore == 1 && hudRoots == 1 &&") ||
                !verify.Contains("unloads == 1 && completes == 1 && idle && clean;") ||
                !verify.Contains("UI.CastingWorkspaceSession.SavedIntentSignature(") ||
                !verify.Contains("string.Equals(session.CampaignId, expectedGame, StringComparison.Ordinal)"))
                throw new InvalidOperationException("The reload verification is weaker than the scenario claims.");
        }

        // The grid reads alphabetically by name (then detail, then id), not in
        // source-id order, and with nothing authored the first buff shown is
        // the one selected.
        private static void TestBuffGridIsAlphabetical(string root)
        {
            var ordered = WorkspaceSourceLabels.GridOrder(new[]
            {
                new WorkspaceSourceOption("a-id", "Resistance", false),
                new WorkspaceSourceOption("b-id", "aid another", false, "Attack Bonus"),
                new WorkspaceSourceOption("c-id", "Aid Another", false, "AC Bonus"),
                new WorkspaceSourceOption("d-id", "Light", false),
                new WorkspaceSourceOption("0-id", "Light", false)
            }).Select(value => value.SourceId).ToArray();
            if (!ordered.SequenceEqual(new[] { "c-id", "b-id", "0-id", "d-id", "a-id" }))
                throw new InvalidOperationException("The buff grid order is wrong: " + string.Join(",", ordered));
            // Review of 1332ed8..542cd66, P2-5: three castable buffs whose ids
            // sort opposite to their names; the grid shows them by name and
            // commits the first as the selection (P2-2), which a draft then
            // uses without any buff click.
            CastingWorkspaceInputs three = ThreeBuffInputs();
            var gridSession = new CastingWorkspaceSession(Path.Combine(root, "grid-order"), "campaign-grid");
            WorkspaceView gridView = gridSession.BuildView(three);
            string[] shownIds = gridView.Draft.Sources.Select(value => value.SourceId).ToArray();
            if (!shownIds.SequenceEqual(new[] { "c-source", "b-source", "a-source" }) ||
                !gridView.Draft.Sources.First().Selected || gridSession.SelectedSourceId != "c-source" ||
                gridView.SelectedSourceId != "c-source")
                throw new InvalidOperationException("The grid order or the committed selection is wrong: " +
                    string.Join(",", shownIds) + ";selected=" + gridSession.SelectedSourceId);
            gridSession.Draft.TargetMode = CastingTargetMode.DirectTarget;
            gridSession.ChooseDraftCaster("unit-cleric");
            gridSession.Draft.DirectTargetUnitId = "unit-t1";
            AuthoringEditResult added = gridSession.AddCastingFromDraft(three);
            if (!added.Applied || gridSession.Document.Castings.Count != 1 ||
                gridSession.Document.Castings[0].SourceId != "c-source")
                throw new InvalidOperationException("Add did not use the buff shown selected: " + added.Reason);
        }

        // Three castable free buffs on one caster; their source ids sort
        // opposite to their names (Zebra Ward, Mage Armor, Aid).
        private static CastingWorkspaceInputs ThreeBuffInputs()
        {
            List<UnitSnapshot> units = new[] { "unit-cleric", "unit-t1", "unit-t2" }
                .Select(id => new UnitSnapshot(id, id, false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))).ToList();
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
            var providers = new List<ProviderSnapshot>();
            var options = new List<ProviderPlanningOption>();
            foreach (string[] buff in new[]
                {
                    new[] { "a-source", "guid-zebra", "Zebra Ward" },
                    new[] { "b-source", "guid-mage", "Mage Armor" },
                    new[] { "c-source", "guid-aid", "Aid" }
                })
            {
                AbilityKey ability = Ability(buff[1], string.Empty, 0);
                EffectExpression expression = Leaf(buff[1] + "-effect");
                effects[buff[0]] = expression;
                effects[ability.Canonical] = expression;
                var provider = new ProviderSnapshot(new ProviderKey("unit-cleric", "book-cleric", ability, "level-0"),
                    buff[2], 0, "pool-cleric-free", 0, null, null, 1, 10, string.Empty, string.Empty, buff[2]);
                providers.Add(provider);
                options.Add(new ProviderPlanningOption(provider, units.Select(unit => unit.UnitId).ToList(),
                    new[] { "unit-cleric" }, 10, 100, CastExecutionStrategy.DirectRuleCast, "fixture-direct",
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)));
            }
            var pools = new[] { new ResourcePoolSnapshot("pool-cleric-free", ResourcePoolKind.Unlimited, 0, 0, null) };
            return new CastingWorkspaceInputs(new PartyProviderSnapshot(units, providers, pools),
                options, effects, new CastEnhancementSnapshot[0], null, null);
        }

        // Mission section 11 (first-open import in game): the scenario opens
        // with no synthetic input, seeds the classic plan only through the
        // classic repository before the first open and never over an existing
        // plan, and passes only when the production migration imported it,
        // left it byte-unchanged, archived it byte-exact and made nothing
        // Ready (source checks: the host is game-bound).
        private static void TestInGameFirstOpenImportIsJudged()
        {
            if (!RuntimeTestProtocol.IsImportScenario("live-workspace-import") ||
                !RuntimeTestProtocol.IsWorkspaceScenario("live-workspace-import") ||
                !RuntimeTestProtocol.IsNoInputWorkspaceScenario("live-workspace-import") ||
                RuntimeTestProtocol.IsAdvancedFamilyScenario("live-workspace-import"))
                throw new InvalidOperationException("The import scenario classification is wrong.");
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("Repository root was not discoverable.");
            string host = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner",
                "RuntimeTesting", "RuntimeTestHost.cs"));
            string seed = SourceBlock(host, "private void SeedClassicPlanForImport()");
            int exists = seed == null ? -1 : seed.IndexOf("if (File.Exists(path))", StringComparison.Ordinal);
            int save = seed == null ? -1 : seed.IndexOf("repository.Save(profile);", StringComparison.Ordinal);
            if (seed == null || exists < 0 || save < 0 || exists > save ||
                !seed.Contains("CastingQualificationRecipe.SelectZeroCostMixed(inputs, campaignId)") ||
                !seed.Contains("CasterUnitId = null,"))
                throw new InvalidOperationException("The import seed can overwrite a classic plan or is not built from discovery.");
            string verify = SourceBlock(host, "private string VerifyWorkspaceImport()");
            if (verify == null ||
                !verify.Contains("bool passed = _importFailure == null && migrated && classicUnchanged && archived && imported && reviewed;") ||
                !verify.Contains("CastingMigrationStatus.Migrated") ||
                !verify.Contains("report.ReadyCount == 0"))
                throw new InvalidOperationException("The import verification is weaker than the scenario claims.");
            int seedCall = host.IndexOf("if (RuntimeTestProtocol.IsImportScenario(_request.Scenario)) SeedClassicPlanForImport();",
                StringComparison.Ordinal);
            int programmaticOpen = host.IndexOf("if (_liveUiPhase == 22)", StringComparison.Ordinal);
            if (seedCall < 0 || programmaticOpen < 0 || seedCall > programmaticOpen ||
                Occurrences(host, "SeedClassicPlanForImport();") != 1)
                throw new InvalidOperationException("The classic plan is not seeded exactly once before the first open.");
        }

        // Cards explain themselves in words, never in codes or ids.
        private static void TestCardReasonsInWords()
        {
            var expected = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "caster-unresolved", "no caster chosen" },
                { "import-review-unresolved:automatic-caster-pending-review", "imported: needs your review" },
                { "prepared-slots-exhausted:pool-unit-wizard", "no prepared slot left" },
                { "target-not-conscious:unit-t1", "the recipient is unconscious" },
                { "present-effect-not-sufficient", "the active effect is weaker or about to expire" }
            };
            foreach (KeyValuePair<string, string> pair in expected)
                if (WorkspaceReasonText.Describe(pair.Key) != pair.Value)
                    throw new InvalidOperationException("Reason text for " + pair.Key + " is " +
                        WorkspaceReasonText.Describe(pair.Key));
            string[] items =
            {
                "automatic-caster-pending-review", "provider-pin-without-caster-pending-review",
                "group-origin-and-count-pending-review", "no-recipient:pending-review",
                "grouping-unknown:single-or-group-pending-review;targets=unit-a,unit-b",
                "provider-pin:unit-a|book|ability", "enhancement:rod-extend:required:exact-source-pending"
            };
            foreach (string item in items)
            {
                string text = WorkspaceReasonText.DescribeReviewItem(item);
                if (text.Contains(":") || text.Contains("pending-review") || text.Contains("unit-") ||
                    text.Contains("rod-extend"))
                    throw new InvalidOperationException("A review item reached the player as a code: " + item + " -> " + text);
            }
            if (WorkspaceReasonText.DescribeReviewItem("automatic-caster-pending-review") !=
                    "the old plan let the planner pick any caster; choose one")
                throw new InvalidOperationException("The automatic-caster review item is misworded.");
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            string view = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner",
                "UI", "CastingWorkspaceScreenView.cs"));
            string detail = SourceBlock(view, "private static string BuildCardDetail(WorkspaceCastingCard card)");
            if (detail == null || !detail.Contains(".Select(WorkspaceReasonText.Describe)") ||
                !detail.Contains(".Select(WorkspaceReasonText.DescribeReviewItem)") ||
                detail.Contains("string.Join(\", \", card.ReadinessReasons)"))
                throw new InvalidOperationException("Cards still print reason codes.");
        }

        // Refusals tell the player what to do next and the header counts the
        // routine as a whole; no internal code or casting id reaches them
        // (live frames casting-ws-qual-20260923-q2-03).
        private static void TestPlayerFacingRefusalsAndHeader(string root)
        {
            var expected = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "draft-invalid:A direct-target casting requires its direct target.", "choose who receives this casting first." },
                { "draft-ability-unresolved:no-caster-selected", "choose who casts it first." },
                { "draft-ability-unresolved:no-provider-for-source-and-caster", "that character cannot cast this buff." },
                { "no-editing-focus", "select a casting card (Edit) first." },
                { "ready-requires-import-review:cast-4", "review what the import changed before marking it Ready." },
                { "state-unchanged:cast-2", "it already is." },
                { "casting-id-collision:cast-3", "casting id collision." },
                { "targeting-requires-direct-target:pick a recipient to switch back",
                    "choose who receives it to switch back to a single target." },
                { "targeting-invalid:A group casting requires an origin.", "choose the unit the group spell is centred on first." },
                { "draft-invalid:A group casting has an origin, not a direct target.\r\nParameter name: directTargetUnitId",
                    "a group casting has no single recipient; switch to a single target to pick one." },
                { "draft-invalid:A direct-target casting cannot carry a group origin.\r\nParameter name: origin",
                    "a single-target casting has no group area; switch to group targeting for that." },
                { "draft-invalid:A direct-target casting requires its direct target.\r\nParameter name: directTargetUnitId",
                    "choose who receives this casting first." }
            };
            foreach (KeyValuePair<string, string> pair in expected)
                if (WorkspaceRefusalText.Describe(pair.Key) != pair.Value)
                    throw new InvalidOperationException("Refusal text for " + pair.Key + " is " +
                        WorkspaceRefusalText.Describe(pair.Key));
            if (WorkspaceHeaderText.Describe("Long", 0, 0, true, 0) != "Long · no castings yet" ||
                WorkspaceHeaderText.Describe("Long", 3, 2, false, 1) != "Long · 2 of 3 castings ready · Apply blocked" ||
                WorkspaceHeaderText.Describe("Short", 1, 1, true, 0) != "Short · 1 of 1 casting ready · ready to apply")
                throw new InvalidOperationException("The routine header text is wrong.");
            // The session counts the routine, whichever buff is selected, and
            // an Add without a recipient explains itself in words.
            var session = new CastingWorkspaceSession(Path.Combine(root, "header"), "campaign-header");
            var free = QualificationInputs(true, true, null);
            Assert(AddDraftCasting(session, free, "unit-cleric", "unit-t1").Applied);
            Assert(AddDraftCasting(session, free, "unit-wizard", "unit-t2").Applied);
            WorkspaceView view = session.BuildView(free);
            if (view.RoutineCastingCount != 2 || view.RoutineReadyCount != 2)
                throw new InvalidOperationException("The routine counts are wrong: " + view.RoutineCastingCount +
                    "/" + view.RoutineReadyCount);
            // Review of f7726c9..1332ed8, P3-C: the same buff in Short makes
            // the routine's count differ from the selected buff's cards,
            // which span routines (counting the cards would say 3).
            session.SelectBuff("source-bulls");
            session.SelectRoutine("short");
            session.BuildView(free);
            session.Draft.SourceId = "source-bulls";
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.ChooseDraftCaster("unit-wizard");
            session.Draft.DirectTargetUnitId = "unit-t3";
            session.Draft.State = CastingAuthoringState.Ready;
            AuthoringEditResult inShort = session.AddCastingFromDraft(free);
            if (!inShort.Applied)
                throw new InvalidOperationException("The Short casting was not added: " + inShort.Reason);
            session.SelectRoutine("long");
            session.SelectBuff("source-bulls");
            WorkspaceView mixedView = session.BuildView(free);
            if (mixedView.RoutineCastingCount != 2 || mixedView.Cards.Count != 3 ||
                mixedView.Cards.Count(card => card.RoutineId == "long") != 2)
                throw new InvalidOperationException("The header does not count the routine as a whole: routine=" +
                    mixedView.RoutineCastingCount + ";cards=" + mixedView.Cards.Count);
            session.SelectRoutine("short");
            WorkspaceView shortView = session.BuildView(free);
            if (shortView.RoutineCastingCount != 1 || shortView.Cards.Count != 3)
                throw new InvalidOperationException("The short routine count follows the buff's cards: " +
                    shortView.RoutineCastingCount);
            // P3-F: editing a Short casting, then selecting another buff,
            // still names the casting being edited.
            WorkspaceCastingCard shortCard = shortView.Cards.Single(card => card.RoutineId == "short");
            session.FocusCasting(shortCard.CastingId);
            session.SelectBuff("source-communal");
            WorkspaceView switched = session.BuildView(free);
            if (switched.EditingScopeLabel != "Editing casting 1: unit-wizard → unit-t3")
                throw new InvalidOperationException("The editing label lost the casting after a buff switch: " +
                    switched.EditingScopeLabel);
            session.FocusCasting(null);
            session.SelectBuff("source-bulls");
            session.SelectRoutine("long");
            session.Draft.SourceId = "source-bulls";
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.ChooseDraftCaster("unit-cleric");
            session.Draft.DirectTargetUnitId = null;
            AuthoringEditResult refused = session.AddCastingFromDraft(free);
            if (refused.Applied || WorkspaceRefusalText.Describe(refused.Reason).Contains(":") ||
                WorkspaceRefusalText.Describe(refused.Reason).Contains("draft-"))
                throw new InvalidOperationException("An Add without a recipient did not explain itself: " +
                    refused.Reason + " -> " + WorkspaceRefusalText.Describe(refused.Reason));
        }

        // Players read whose resource and what kind, never an internal pool
        // key: card costs, the budget footer and the pool label helper.
        private static void TestPlayerFacingResourceLabels(string root)
        {
            if (WorkspacePoolLabels.Describe(ResourcePoolKind.Unlimited, 0, "Linzi") != "Linzi: free" ||
                WorkspacePoolLabels.Describe(ResourcePoolKind.SpontaneousLevel, 2, "Linzi") != "Linzi: level 2 spell slot" ||
                WorkspacePoolLabels.Describe(ResourcePoolKind.PreparedSlots, 1, "Tartuccio") != "Tartuccio: prepared level 1 slot" ||
                WorkspacePoolLabels.Describe(ResourcePoolKind.ItemCharges, null, null) != "item charge")
                throw new InvalidOperationException("The pool label wording is wrong.");
            var session = new CastingWorkspaceSession(Path.Combine(root, "labels"), "campaign-labels");
            var free = QualificationInputs(true, true, null);
            Assert(AddDraftCasting(session, free, "unit-cleric", "unit-t1").Applied);
            WorkspaceView view = session.BuildView(free);
            WorkspaceCastingCard card = view.Cards.Single();
            if (!card.CostLabels.SequenceEqual(new[] { "unit-cleric: free" }) ||
                card.Subtitle != "Casting 1 in Long" ||
                view.BudgetRows.Select(row => row.Describe()).Where(text => text != null)
                    .Any(text => text.Contains("|") || text.Contains("pool-")))
                throw new InvalidOperationException("Internal keys reached the player: " +
                    string.Join(",", card.CostLabels.ToArray()) + " / " +
                    string.Join(" / ", view.BudgetRows.Select(row => row.Describe() ?? "-").ToArray()));
            var finite = QualificationInputs(false, true, null);
            WorkspaceView finiteView = session.BuildView(finite);
            if (!finiteView.Cards.Single().CostLabels.SequenceEqual(new[] { "unit-cleric: level 2 spell slot" }))
                throw new InvalidOperationException("A finite pool label is wrong: " +
                    string.Join(",", finiteView.Cards.Single().CostLabels.ToArray()));
            // Review of f7726c9..1332ed8, P3-D: shortages first, free pools
            // merged, same-looking finite pools numbered, enhancement and
            // material rows named by kind.
            var freeA = new WorkspaceBudgetRow(new CastingBudgetLine("free-a", CastingCostCategory.NativePool,
                null, 0, 0, new[] { "c1" }, true), "Linzi: free");
            var freeB = new WorkspaceBudgetRow(new CastingBudgetLine("free-b", CastingCostCategory.NativePool,
                null, 0, 0, new[] { "c2" }, true), "Linzi: free");
            var ok = new WorkspaceBudgetRow(new CastingBudgetLine("book-a", CastingCostCategory.NativePool,
                2, 1, 1, new[] { "c3" }), "Linzi: level 1 spell slot");
            var twin = new WorkspaceBudgetRow(new CastingBudgetLine("book-b", CastingCostCategory.NativePool,
                2, 1, 1, new[] { "c4" }), "Linzi: level 1 spell slot");
            var short1 = new WorkspaceBudgetRow(new CastingBudgetLine("prep", CastingCostCategory.NativePool,
                1, 2, 1, new[] { "c5", "c6" }), "Tartuccio: prepared slot");
            var rows = new List<WorkspaceBudgetRow> { freeA, ok, freeB, twin, short1 };
            WorkspaceBudgetRow.DisambiguateLabels(rows);
            var footer = WorkspaceBudgetRow.FooterLines(rows).ToList();
            if (!footer.SequenceEqual(new[]
                {
                    "Tartuccio: prepared slot: 1 of 2 covered, short by 1",
                    "Linzi: level 1 spell slot: 1 of 1 covered",
                    "Linzi: level 1 spell slot (2): 1 of 1 covered",
                    "Linzi: free (2 casts, nothing spent)"
                }))
                throw new InvalidOperationException("The budget footer is wrong: " + string.Join(" / ", footer.ToArray()));
            var rodInputs = new CastingWorkspaceInputs(free.Snapshot, free.ProviderOptions, free.EffectsBySource,
                new[]
                {
                    new CastEnhancementSnapshot("rod-extend", "unit-cleric", "rod-guid", "Extend Rod", string.Empty,
                        CastEnhancementCategory.MetamagicRod, 8, 3, 3, null, null, null, "rod-pool", false, null, 1,
                        false, null, "Lesser Extend Metamagic Rod")
                });
            var rodSession = new CastingWorkspaceSession(Path.Combine(root, "labels-rod"), "campaign-labels");
            rodSession.BuildView(rodInputs);
            string rodLabel = rodSession.BudgetLabel(new CastingBudgetLine("rod-pool", CastingCostCategory.EnhancementPool,
                3, 1, 1, new[] { "c7" }));
            string materialLabel = rodSession.BudgetLabel(new CastingBudgetLine("item-guid", CastingCostCategory.Material,
                1, 1, 1, new[] { "c8" }));
            if (rodLabel != "unit-cleric: Lesser Extend Metamagic Rod uses" || materialLabel != "material component")
                throw new InvalidOperationException("Enhancement or material rows are unnamed: " + rodLabel + " / " + materialLabel);
            // A prepared pool spans every spell level: whose, not which level.
            var world = new FiniteBuffWorld();
            CastingWorkspaceInputs mixed = world.Inputs();
            var both = new CastingWorkspaceSession(Path.Combine(root, "labels-mixed"), "campaign-labels");
            Assert(AddDraftCasting(both, mixed, "unit-wizard", "unit-t1").Applied);
            Assert(AddDraftCasting(both, mixed, "unit-sorcerer", "unit-t2").Applied);
            var labels = both.BuildView(mixed).Cards.SelectMany(value => value.CostLabels).ToList();
            if (!labels.SequenceEqual(new[] { "unit-wizard: prepared slot", "unit-sorcerer: level 2 spell slot" }))
                throw new InvalidOperationException("Prepared or spontaneous pool labels are wrong: " +
                    string.Join(",", labels.ToArray()));
        }

        // Two casters with verified-free pools casting the fixture buff by
        // rule, plus four other party members (optionally a finite pool or
        // a single caster).
        private static CastingWorkspaceInputs QualificationInputs(bool free, bool twoCasters,
            ActiveEffectSnapshot live, int otherUnits = 4)
        {
            string[] casters = twoCasters
                ? new[] { "unit-cleric", "unit-wizard" } : new[] { "unit-cleric" };
            string[] others = new[] { "unit-t1", "unit-t2", "unit-t3", "unit-t4" }.Take(otherUnits).ToArray();
            List<string> all = casters.Concat(others).ToList();
            List<UnitSnapshot> units = all.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                new TargetValidationSnapshot(true, true, true, true))).ToList();
            var providers = new List<ProviderSnapshot>();
            var pools = new List<ResourcePoolSnapshot>();
            var options = new List<ProviderPlanningOption>();
            foreach (string caster in casters)
            {
                string poolKey = "pool-" + caster;
                pools.Add(free
                    ? new ResourcePoolSnapshot(poolKey, ResourcePoolKind.Unlimited, 0, 0, null)
                    : new ResourcePoolSnapshot(poolKey, ResourcePoolKind.SpontaneousLevel, 3, 3, null));
                ProviderSnapshot provider = PlannerProvider(caster, "book-" + caster,
                    CastingBuffAbility, poolKey, free ? 0 : 1);
                providers.Add(provider);
                options.Add(new ProviderPlanningOption(provider, all, new[] { caster }, 10, 100,
                    CastExecutionStrategy.DirectRuleCast, "fixture-direct",
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)));
            }
            return new CastingWorkspaceInputs(new PartyProviderSnapshot(units, providers, pools),
                options, CastingEffectsWithAbilityAlias("source-bulls", "source-communal",
                    CastingBuffAbility), new CastEnhancementSnapshot[0], null, live);
        }

        private static string QualificationAllowanceJson(Action<JObject> mutate = null)
        {
            var root = new JObject
            {
                { "schemaVersion", 5 },
                { "kind", "kbp-casting-qualification" },
                { "runId", "qual-run-1" },
                { "sourceCommit", new string('c', 40) },
                { "packageSha256", new string('a', 64) },
                { "dllSha256", new string('b', 64) },
                { "assemblyMvid", "11111111-2222-3333-4444-555555555555" },
                { "fixtureGameId", "fixture-game" },
                { "recipe", "zero-cost-mixed" },
                { "executionMode", "instant" },
                { "approvedProjectionIds", new JArray(new string('d', 64), new string('e', 64)) },
                { "maximumNativeSubmissions", 6 },
                { "approvedBy", "Howie" },
                { "authority", "owner mission 2026-09-23 section 4" },
                { "compatibilityProfileId", "full-user" },
                { "compatibilityIdentity", new string('f', 64) },
                { "workingSaveSha256", new string('9', 64) },
                { "purpose", "casting-first qualification fixture" }
            };
            if (mutate != null) mutate(root);
            return root.ToString();
        }

        private static void TestQualificationAllowanceParsing()
        {
            string refusal;
            CastingQualificationAllowance valid = CastingQualificationAllowance.Parse(
                QualificationAllowanceJson(), "qual-run-1", out refusal);
            if (valid == null || refusal != null || valid.ApprovedProjectionIds.Count != 2 ||
                valid.MaximumNativeSubmissions != 6 || valid.Recipe != "zero-cost-mixed" ||
                valid.ExecutionMode != "instant")
                throw new InvalidOperationException("A valid qualification allowance was refused: " + refusal);
            // Schema 4 names the casting mode; an allowance without one (the
            // schema-3 shape) or with any other mode authorizes nothing.
            foreach (KeyValuePair<string, Action<JObject>> item in new Dictionary<string, Action<JObject>>
                {
                    { "allowance-missing-member:executionMode", o => o.Remove("executionMode") },
                    { "allowance-schema", o => o["schemaVersion"] = 3 },
                    { "allowance-execution-mode", o => o["executionMode"] = "hybrid" }
                })
            {
                if (CastingQualificationAllowance.Parse(QualificationAllowanceJson(item.Value),
                        "qual-run-1", out refusal) != null || refusal != item.Key)
                    throw new InvalidOperationException("Mode case " + item.Key + " returned " + refusal);
            }
            CastingQualificationAllowance animated = CastingQualificationAllowance.Parse(
                QualificationAllowanceJson(o => o["executionMode"] = "animated"), "qual-run-1", out refusal);
            if (animated == null || animated.ExecutionMode != "animated")
                throw new InvalidOperationException("An animated allowance was refused: " + refusal);
            var cases = new Dictionary<string, Action<JObject>>
            {
                { "allowance-unknown-member:extra", o => o["extra"] = 1 },
                { "allowance-missing-member:authority", o => o.Remove("authority") },
                { "allowance-schema", o => o["schemaVersion"] = 2 },
                { "allowance-kind", o => o["kind"] = "kbp-single-cast-probe" },
                { "allowance-submissions-range", o => o["maximumNativeSubmissions"] = 25 },
                { "allowance-projection-ids", o => o["approvedProjectionIds"] = new JArray("XYZ") },
                { "allowance-artifact-identity", o => o["dllSha256"] = "short" },
                { "allowance-fixture-missing", o => o["fixtureGameId"] = string.Empty },
                { "allowance-recipe-unknown", o => o["recipe"] = "anything" },
                { "allowance-approval-missing", o => o["approvedBy"] = string.Empty },
                // Review C5: the fixture binding is part of the approval.
                { "allowance-missing-member:purpose", o => o.Remove("purpose") },
                { "allowance-profile", o => o["compatibilityProfileId"] = "other-profile" },
                { "allowance-compatibility-identity", o => o["compatibilityIdentity"] = "XYZ" },
                { "allowance-working-save", o => o["workingSaveSha256"] = new string('9', 63) },
                { "allowance-purpose", o => o["purpose"] = " " }
            };
            foreach (KeyValuePair<string, Action<JObject>> item in cases)
            {
                if (CastingQualificationAllowance.Parse(QualificationAllowanceJson(item.Value),
                        "qual-run-1", out refusal) != null || refusal != item.Key)
                    throw new InvalidOperationException("Allowance case " + item.Key + " returned " + refusal);
            }
            if (CastingQualificationAllowance.Parse(QualificationAllowanceJson(), "other-run",
                    out refusal) != null || refusal != "allowance-run-mismatch")
                throw new InvalidOperationException("A different run used the allowance.");
            // A schema-4 allowance (no fixture binding) authorizes nothing.
            if (CastingQualificationAllowance.Parse(QualificationAllowanceJson(o => o["schemaVersion"] = 4),
                    "qual-run-1", out refusal) != null || refusal != "allowance-schema")
                throw new InvalidOperationException("A schema-4 allowance without its binding was accepted.");
            if (CastingQualificationAllowance.Parse(QualificationAllowanceJson(o =>
                    o["maximumNativeSubmissions"] = 0), "qual-run-1", out refusal) != null)
                throw new InvalidOperationException("A zero submission budget was accepted.");
        }

        // The recipe picks one free buff, two casters and fresh targets
        // deterministically, and refuses rather than improvise.
        private static void TestQualificationRecipeSelection()
        {
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectZeroCostMixed(
                QualificationInputs(true, true, null), "fixture-campaign");
            string shape = string.Join(",", selection.Castings.Select(casting =>
                casting.CastingId + "=" + casting.CasterUnitId + ">" + casting.DirectTargetUnitId).ToArray());
            if (!selection.Selected || shape !=
                    "qual-cast-1=unit-cleric>unit-t1,qual-cast-2=unit-wizard>unit-t2,qual-cast-3=unit-cleric>unit-t3" ||
                selection.SourceId != "source-bulls")
                throw new InvalidOperationException("The recipe selection is wrong: " + shape + "|" +
                    selection.Refusal + "|" + string.Join(";", selection.Rejections.ToArray()));
            CastingQualificationSelection freshOnly = CastingQualificationRecipe.SelectZeroCostMixed(
                QualificationInputs(true, true, LiveEffects(On("unit-t1", "buff-effect", null, 1, null))),
                "fixture-campaign");
            if (!freshOnly.Selected || freshOnly.Castings[0].DirectTargetUnitId != "unit-t2")
                throw new InvalidOperationException("A target with the effect already active was chosen.");
            CastingQualificationSelection finite = CastingQualificationRecipe.SelectZeroCostMixed(
                QualificationInputs(false, true, null), "fixture-campaign");
            if (finite.Selected || !finite.Rejections.Any(value => value.EndsWith("|not-verified-free",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("A finite source was used for the free recipe.");
            CastingQualificationSelection single = CastingQualificationRecipe.SelectZeroCostMixed(
                QualificationInputs(true, false, null), "fixture-campaign");
            if (single.Selected || !single.Rejections.Any(value => value.EndsWith(
                    "|fewer-than-two-casters", StringComparison.Ordinal)))
                throw new InvalidOperationException("A single caster satisfied the mixed recipe.");
            // Review RC4: a three-unit party (two casters and one other)
            // qualifies: recipients are chosen per casting, a caster may
            // receive from the other caster, never from itself.
            CastingQualificationSelection three = CastingQualificationRecipe.SelectZeroCostMixed(
                QualificationInputs(true, true, null, 1), "fixture-campaign");
            string threeShape = string.Join(",", three.Castings.Select(casting =>
                casting.CastingId + "=" + casting.CasterUnitId + ">" + casting.DirectTargetUnitId).ToArray());
            if (!three.Selected || threeShape !=
                    "qual-cast-1=unit-cleric>unit-t1,qual-cast-2=unit-wizard>unit-cleric,qual-cast-3=unit-cleric>unit-wizard")
                throw new InvalidOperationException("The three-unit selection is wrong: " + threeShape + "|" +
                    three.Refusal + "|" + string.Join(";", three.Rejections.ToArray()));
            CastingQualificationSelection two = CastingQualificationRecipe.SelectZeroCostMixed(
                QualificationInputs(true, true, null, 0), "fixture-campaign");
            if (!two.Selected || two.Castings.Count != 2 ||
                two.Castings.Any(casting => casting.CasterUnitId == casting.DirectTargetUnitId))
                throw new InvalidOperationException("Two casters could not buff each other, or one buffed itself.");
        }

        // The forecast gives the exact projection of each executing step,
        // and the qualification boundary submits only those, in order,
        // within the submission budget.
        private static void TestQualificationForecastAndBoundary()
        {
            CastingWorkspaceInputs inputs = QualificationInputs(true, true, null);
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectZeroCostMixed(
                inputs, "fixture-campaign");
            IReadOnlyList<CastingQualificationStepForecast> steps = CastingQualificationForecast.Forecast(
                selection, inputs, "fixture-campaign");
            string shape = string.Join(";", steps.Select(step => step.Name + "=" +
                string.Join(",", step.CastingIds.ToArray()) + (step.Refusal ?? string.Empty)).ToArray());
            if (shape != "stop=qual-cast-1,qual-cast-2,qual-cast-3;complete=qual-cast-2,qual-cast-3;recast=qual-cast-1;disable=qual-cast-1;recover=qual-cast-1" ||
                steps.Any(step => step.ProjectionId == null) ||
                steps.Select(step => step.ProjectionId).Distinct().Count() != 3 ||
                steps[3].ProjectionId != steps[2].ProjectionId ||
                steps[4].ProjectionId != steps[3].ProjectionId)
                throw new InvalidOperationException("The step forecast is wrong: " + shape);
            // The real state after the stop step (qual-cast-1 active) projects
            // exactly the forecast complete step.
            CastingPlanDocument document = CastingQualificationForecast.BuildDocument(
                "fixture-campaign", selection.Castings);
            CastingQualificationStepForecast real = CastingQualificationForecast.Project("real",
                document, inputs, LiveEffects(On("unit-t1", "buff-effect", 20, 1, 0)));
            if (real.ProjectionId != steps[1].ProjectionId)
                throw new InvalidOperationException("The complete forecast differs from the real projection.");
            // Boundary: next approved id in order, budget respected.
            string json = QualificationAllowanceJson(o =>
            {
                o["approvedProjectionIds"] = new JArray(steps.Select(step => (object)step.ProjectionId).ToArray());
                o["maximumNativeSubmissions"] = 4;
            });
            string refusal;
            CastingQualificationAllowance allowance = CastingQualificationAllowance.Parse(json,
                "qual-run-1", out refusal);
            long now = 0;
            // "free": a genuine zero-cost cast reports no spend (a spend on a
            // verified free pool would correctly fail the cast).
            var runtime = new ScriptedInstantRuntime("none", "free");
            var host = new CastingExecutionHost(settings => new InstantCastExecutor(runtime, true), () => now);
            var boundary = new CastingQualificationBoundary(allowance, host, () => new ExecutionProfile
            {
                Mode = "instant", AllowAnimatedFallback = true, OutOfCombatOnly = true
            });
            Func<string, ActiveEffectSnapshot, CastingPlanDocument, CastingDispatchOutcome> submit =
                (name, live, doc) =>
                {
                    ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(doc,
                        inputs.Snapshot, inputs.ProviderOptions, inputs.EffectsBySource,
                        inputs.Enhancements, "long", null, false, live);
                    CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan,
                        CastingApplyMode.Ordinary, "long");
                    ExplicitStepConversion projection = ExplicitCastingStepConverter.Convert(plan,
                        decision, inputs.ProviderOptions, inputs.EffectsBySource);
                    CastingDispatchOutcome outcome = boundary.Submit(plan, decision, "long", projection);
                    int guard = 0;
                    while (host.IsRunning && guard++ < 1000) host.Pump();
                    return outcome;
                };
            ActiveEffectSnapshot afterStop = LiveEffects(On("unit-t1", "buff-effect", 20, 1, 0));
            if (submit("early", afterStop, document).Submitted ||
                !boundary.Submissions[0].StartsWith("refused:qualification-projection-not-approved:step=0",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("An out-of-order projection was submitted.");
            if (!submit("stop", null, document).Submitted || boundary.PlannedSubmissions != 3)
                throw new InvalidOperationException("The approved stop projection was refused.");
            CastingDispatchOutcome overBudget = submit("complete", afterStop, document);
            if (overBudget.Submitted ||
                !overBudget.Reason.StartsWith("qualification-submission-cap:4", StringComparison.Ordinal) ||
                runtime.Fired.Count != 3)
                throw new InvalidOperationException("The submission budget was exceeded: " +
                    overBudget.Reason + "|fired=" + runtime.Fired.Count + "|" +
                    string.Join(";", boundary.Submissions.ToArray()));
            if (!boundary.Submissions.Any(value => value.EndsWith(";mode=instant", StringComparison.Ordinal)))
                throw new InvalidOperationException("A submission did not record its casting mode.");
            // The run executes only in the mode the allowance names: another
            // mode, or no settings at all, is refused before the host and
            // consumes neither the approved id nor the budget.
            foreach (string wrongMode in new[] { "animated", null })
            {
                var modeHost = new CastingExecutionHost(settings => new InstantCastExecutor(runtime, true), () => now);
                var modeBoundary = new CastingQualificationBoundary(allowance, modeHost, () => wrongMode == null
                    ? null : new ExecutionProfile { Mode = wrongMode, AllowAnimatedFallback = true, OutOfCombatOnly = true });
                ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document,
                    inputs.Snapshot, inputs.ProviderOptions, inputs.EffectsBySource,
                    inputs.Enhancements, "long", null, false, null);
                CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan,
                    CastingApplyMode.Ordinary, "long");
                ExplicitStepConversion projection = ExplicitCastingStepConverter.Convert(plan,
                    decision, inputs.ProviderOptions, inputs.EffectsBySource);
                CastingDispatchOutcome outcome = modeBoundary.Submit(plan, decision, "long", projection);
                if (outcome.Submitted ||
                    outcome.Reason != "qualification-mode-not-approved:" + (wrongMode ?? "none") ||
                    modeHost.StartedRuns != 0 || modeBoundary.PlannedSubmissions != 0 ||
                    modeBoundary.DispositionReason != "qualification-armed" || runtime.Fired.Count != 3)
                    throw new InvalidOperationException("A run in an unapproved mode (" + (wrongMode ?? "none") +
                        ") reached the host: " + outcome.Reason);
            }
        }

        // A tiny simulated game world for the qualification driver: a rule
        // cast of the fixture buff creates a NEW effect instance on its
        // target (no spend: the sources are verified free); fresh reads and
        // later compiles see exactly the world state.
        private sealed class SimulatedBuffWorld : IInstantCastRuntimeAdapter,
            ICastRuntimeAdapter, ICastEnhancementRuntimeAdapter
        {
            internal readonly Dictionary<string, KeyValuePair<string, long>> Active =
                new Dictionary<string, KeyValuePair<string, long>>(StringComparer.Ordinal);
            internal readonly List<string> Fired = new List<string>();
            // Animated casts: each native command runs this many polls before
            // it lands (the caster walks and casts), then the effect exists.
            internal int AnimatedFrames = 5;
            internal readonly List<string> AnimatedStarts = new List<string>();
            // Failure shape: an interrupted animated cast still lands.
            internal bool LandOnInterrupt;
            // Failure shapes: this casting lands but is never confirmed, or
            // reports a resource spent on its verified-free source.
            internal string UnconfirmedCasting;
            internal string SpendOnFreeCasting;
            // Party members besides the two casters (the automation party
            // has one).
            internal int OtherUnits = 4;
            // Before-read fault for qual-cast-1 in the stop step: throw,
            // failed, null or wrong-target.
            internal string BeforeReadFault;
            private int _instances;
            private long _sequence;
            internal long Now;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            { return CastEnhancementPreparation.Pass(null); }
            public InstantCastResult Fire(CastStep step)
            {
                Land(step);
                return new InstantCastResult(true, true, step.AssignmentId != UnconfirmedCasting,
                    step.AssignmentId == SpendOnFreeCasting, "simulated");
            }

            internal void Land(CastStep step)
            {
                Fired.Add(step.AssignmentId);
                Active[step.TargetUnitIds[0]] = new KeyValuePair<string, long>(
                    "i" + (++_instances), Now + 600);
            }

            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                AnimatedStarts.Add(step.AssignmentId);
                return new SimulatedAnimatedOperation(this, step);
            }
            public bool EffectsObserved(CastStep step)
            {
                return step.AssignmentId != UnconfirmedCasting && Active.ContainsKey(step.TargetUnitIds[0]);
            }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("simulated-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("simulated-clean"); }

            internal ActiveEffectSnapshot Live()
            {
                return LiveEffects(Active.Select(pair => On(pair.Key, "buff-effect", 100, 1, 0)).ToArray());
            }

            internal ProbeObservation Observe(CastStep step, string label)
            {
                KeyValuePair<string, long> instance;
                string target = step.TargetUnitIds[0];
                if (BeforeReadFault != null && label == "stop-before:qual-cast-1")
                {
                    if (BeforeReadFault == "throw") throw new InvalidOperationException("observer-down");
                    if (BeforeReadFault == "failed")
                        return ProbeObservation.Failed(label, ++_sequence, DateTime.UtcNow, "read-refused");
                    if (BeforeReadFault == "null") return null;
                    target = "unit-somebody-else";
                }
                var instances = Active.TryGetValue(target, out instance)
                    ? new[] { new ProbeEffectInstance("buff-effect", instance.Key, instance.Value) }
                    : new ProbeEffectInstance[0];
                return ProbeObservation.Read(label, ++_sequence, DateTime.UtcNow,
                    target, -1, instances);
            }
        }

        private sealed class SimulatedAnimatedOperation : IAnimatedCastOperation
        {
            private readonly SimulatedBuffWorld _world;
            private readonly CastStep _step;
            private int _polls;
            private bool _landed;

            internal SimulatedAnimatedOperation(SimulatedBuffWorld world, CastStep step)
            {
                _world = world;
                _step = step;
            }

            public bool IsCompleted
            {
                get
                {
                    if (++_polls < _world.AnimatedFrames) return false;
                    if (!_landed) { _landed = true; _world.Land(_step); }
                    return true;
                }
            }
            public bool IsStarted { get { return _polls > 0; } }
            public bool TimedOut { get { return false; } }
            public bool Succeeded { get { return _landed; } }
            public bool EffectsObserved { get { return _landed && _world.EffectsObserved(_step); } }
            public bool ResourceSpent { get { return false; } }
            public string ResourceCountViolation { get { return null; } }
            public bool HasResidualDeliveryState { get { return false; } }
            public string Detail { get { return "simulated-animated;polls=" + _polls; } }
            public void Dispose()
            {
                if (_landed || !_world.LandOnInterrupt) return;
                _landed = true;
                _world.Land(_step);
            }
        }

        // The group world (mission batch 3, section 8; the owner's mixed-
        // coverage case of 2026-09-24): an arcanist with a direct buff, a
        // cleric with a caster-centred group buff of the SAME effect (one
        // prepared slot) and a bard with a target-anchored group buff of
        // another effect. A cast lands on its target or on every expected
        // recipient; a recipient already holding the effect keeps that
        // instance unchanged when KeepExisting (a game that keeps a
        // longer-lasting instance), or receives a new one. Confirmation is
        // the production judgement over the reads taken as the cast lands.
        private sealed class GroupBuffWorld : IInstantCastRuntimeAdapter, ICastRuntimeAdapter,
            ICastEnhancementRuntimeAdapter
        {
            internal static readonly AbilityKey Direct =
                new AbilityKey("direct-spell", null, 0, SourceKind.Spellbook, null);
            internal static readonly AbilityKey Group =
                new AbilityKey("group-spell", null, 0, SourceKind.Spellbook, null);
            internal static readonly AbilityKey Anchored =
                new AbilityKey("anchored-spell", null, 0, SourceKind.Spellbook, null);
            internal static readonly string[] Units =
                { "unit-arcanist", "unit-bard", "unit-cleric", "unit-t1", "unit-t2" };
            // "<unit>|<effect>" -> (instance key, end).
            internal readonly Dictionary<string, KeyValuePair<string, long>> Active =
                new Dictionary<string, KeyValuePair<string, long>>(StringComparer.Ordinal);
            internal readonly List<string> Fired = new List<string>();
            internal readonly Dictionary<string, int> Remaining = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "pool-arcanist", 3 }, { "pool-bard", 3 }
            };
            internal readonly string Token = PreparedSlotIds.Format(2, 0, 0);
            internal bool TokenAvailable = true;
            internal bool KeepExisting = true;
            internal string MissRecipient;
            internal string DirectEffect = "shared-effect";
            internal int DirectCasterLevel = 9;
            internal int AnimatedFrames = 3;
            internal long Now;
            private readonly Dictionary<CastStep, EffectBaseline> _baselines =
                new Dictionary<CastStep, EffectBaseline>();
            private int _instances;
            private long _sequence;

            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            { return CastEnhancementPreparation.Pass(null); }
            public InstantCastResult Fire(CastStep step)
            {
                Land(step);
                return new InstantCastResult(true, true, EffectsObserved(step), true, "simulated-group");
            }
            public IAnimatedCastOperation StartAnimated(CastStep step) { return new GroupAnimatedOperation(this, step); }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("simulated-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("simulated-clean"); }

            private static List<string> EffectIds(CastStep step)
            {
                return CastingQualificationForecast.Leaves(step.ExpectedEffects)
                    .Select(leaf => leaf.EffectId).Distinct(StringComparer.Ordinal).ToList();
            }

            private List<ObservedEffectInstance> Instances(string unit, CastStep step)
            {
                var instances = new List<ObservedEffectInstance>();
                foreach (string effect in EffectIds(step))
                {
                    KeyValuePair<string, long> instance;
                    if (Active.TryGetValue(unit + "|" + effect, out instance))
                        instances.Add(new ObservedEffectInstance(EffectKind.Buff, effect, instance.Key,
                            instance.Value, false));
                }
                return instances;
            }

            // Reads every recipient, then lands the cast and spends its source.
            internal void Land(CastStep step)
            {
                Fired.Add(step.AssignmentId);
                _baselines[step] = new EffectBaseline(step.ExpectedRecipientUnitIds.ToDictionary(unit => unit,
                    unit => (IEnumerable<ObservedEffectInstance>)Instances(unit, step), StringComparer.Ordinal));
                foreach (string unit in step.ExpectedRecipientUnitIds)
                {
                    if (unit == MissRecipient) continue;
                    foreach (string effect in EffectIds(step))
                    {
                        string key = unit + "|" + effect;
                        if (KeepExisting && Active.ContainsKey(key)) continue;
                        Active[key] = new KeyValuePair<string, long>("i" + (++_instances), Now + 600);
                    }
                }
                if (step.Reservation.TokenIds.Count != 0) TokenAvailable = false;
                else Remaining[step.Reservation.PoolKey]--;
            }

            public bool EffectsObserved(CastStep step)
            {
                EffectBaseline baseline;
                return _baselines.TryGetValue(step, out baseline) &&
                    AppliedEffectJudgement.AllReached(step.ExpectedRecipientUnitIds, step.ExpectedEffects,
                        baseline, unit => Instances(unit, step), step.PreCoveredRecipientUnitIds);
            }

            internal ProbeObservation Observe(CastStep step, string label)
            {
                return ObserveRecipient(step, step.TargetUnitIds[0], label);
            }

            internal ProbeObservation ObserveRecipient(CastStep step, string unit, string label)
            {
                var instances = new List<ProbeEffectInstance>();
                foreach (string effect in EffectIds(step))
                {
                    KeyValuePair<string, long> instance;
                    if (Active.TryGetValue(unit + "|" + effect, out instance))
                        instances.Add(new ProbeEffectInstance(effect, instance.Key, instance.Value));
                }
                bool prepared = step.Reservation.TokenIds.Count != 0;
                int available = prepared ? (TokenAvailable ? 1 : 0) : Remaining[step.Reservation.PoolKey];
                Dictionary<string, bool> reserved = prepared
                    ? new Dictionary<string, bool>(StringComparer.Ordinal) { { Token, TokenAvailable } } : null;
                return ProbeObservation.Read(label, ++_sequence, DateTime.UtcNow, unit, available,
                    instances, reserved);
            }

            internal ActiveEffectSnapshot Live()
            {
                return LiveEffects(Active.Select(pair => On(pair.Key.Split('|')[0], pair.Key.Split('|')[1],
                    80, 9, 0)).ToArray());
            }

            internal CastingWorkspaceInputs Inputs()
            {
                List<UnitSnapshot> units = Units.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))).ToList();
                var direct = new ProviderSnapshot(new ProviderKey("unit-arcanist", "book-arcanist", Direct,
                    "level-1"), "Direct Ward", 1, "pool-arcanist", 1, null, null, DirectCasterLevel, 90);
                var group = new ProviderSnapshot(new ProviderKey("unit-cleric", "book-cleric", Group,
                    "level-2"), "Group Ward", 2, "pool-cleric", 1, new[] { Token }, null, 9, 90);
                var anchored = new ProviderSnapshot(new ProviderKey("unit-bard", "book-bard", Anchored,
                    "level-3"), "Anchored Arrows", 3, "pool-bard", 1, null, null, 9, 5400);
                var pools = new[]
                {
                    new ResourcePoolSnapshot("pool-arcanist", ResourcePoolKind.SpontaneousLevel, 3,
                        Remaining["pool-arcanist"], null),
                    new ResourcePoolSnapshot("pool-cleric", ResourcePoolKind.PreparedSlots, 1, TokenAvailable ? 1 : 0,
                        new[] { new ResourceTokenSnapshot(Token, Group, 2, PreparedSlotKind.Common,
                            TokenAvailable, true, null) }),
                    new ResourcePoolSnapshot("pool-bard", ResourcePoolKind.SpontaneousLevel, 3,
                        Remaining["pool-bard"], null)
                };
                List<string> everyone = Units.ToList();
                var options = new List<ProviderPlanningOption>
                {
                    new ProviderPlanningOption(direct, everyone, new[] { "unit-arcanist" }, 9, 90,
                        CastExecutionStrategy.DirectRuleCast, "fixture-direct",
                        new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)),
                    new ProviderPlanningOption(group, everyone, new[] { "unit-cleric" }, 9, 90,
                        CastExecutionStrategy.DirectRuleCast, "fixture-group",
                        new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
                        {
                            { "unit-cleric", everyone }
                        }),
                    new ProviderPlanningOption(anchored, everyone, new[] { "unit-t1", "unit-t2" }, 9, 5400,
                        CastExecutionStrategy.DirectRuleCast, "fixture-anchored",
                        new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
                        {
                            { "unit-t1", new[] { "unit-t1", "unit-t2" } },
                            { "unit-t2", new[] { "unit-t1", "unit-t2" } }
                        })
                };
                var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
                {
                    { Direct.Canonical, new EffectLeafExpression(EffectKind.Buff, DirectEffect,
                        EffectTarget.CurrentTarget, "fixture", "fixture/direct") },
                    { Group.Canonical, new EffectLeafExpression(EffectKind.Buff, "shared-effect",
                        EffectTarget.AlliedAreaRecipients, "fixture", "fixture/group") },
                    { Anchored.Canonical, new EffectLeafExpression(EffectKind.Buff, "anchored-effect",
                        EffectTarget.AlliedAreaRecipients, "fixture", "fixture/anchored") }
                };
                return new CastingWorkspaceInputs(new PartyProviderSnapshot(units,
                        new[] { direct, group, anchored }, pools), options, effects,
                    new CastEnhancementSnapshot[0], null, Live());
            }
        }

        private sealed class GroupAnimatedOperation : IAnimatedCastOperation
        {
            private readonly GroupBuffWorld _world;
            private readonly CastStep _step;
            private int _polls;
            private bool _landed;

            internal GroupAnimatedOperation(GroupBuffWorld world, CastStep step)
            {
                _world = world;
                _step = step;
            }

            public bool IsCompleted
            {
                get
                {
                    if (++_polls < _world.AnimatedFrames) return false;
                    if (!_landed) { _landed = true; _world.Land(_step); }
                    return true;
                }
            }
            public bool IsStarted { get { return _polls > 0; } }
            public bool TimedOut { get { return false; } }
            public bool Succeeded { get { return _landed; } }
            public bool EffectsObserved { get { return _landed && _world.EffectsObserved(_step); } }
            public bool ResourceSpent { get { return _landed; } }
            public string ResourceCountViolation { get { return null; } }
            public bool HasResidualDeliveryState { get { return false; } }
            public string Detail { get { return "simulated-group-animated;polls=" + _polls; } }
            public void Dispose() { }
        }

        private static CastingQualificationAllowance GroupAllowance(GroupBuffWorld world, string mode)
        {
            CastingWorkspaceInputs inputs = world.Inputs();
            CastingQualificationSelection selection =
                CastingQualificationRecipe.SelectGroupMixed(inputs, "fixture-campaign");
            IReadOnlyList<CastingQualificationStepForecast> forecast =
                CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign");
            string refusal;
            return CastingQualificationAllowance.Parse(QualificationAllowanceJson(o =>
            {
                o["fixtureGameId"] = "fixture-campaign";
                o["executionMode"] = mode;
                o["recipe"] = CastingQualificationRecipe.GroupMixed;
                o["approvedProjectionIds"] = new JArray(forecast.Select(step => (object)step.ProjectionId).ToArray());
                o["maximumNativeSubmissions"] = 3;
            }), "qual-run-1", out refusal);
        }

        private static CastingQualificationDriver NewGroupDriver(string dir, GroupBuffWorld world,
            CastingQualificationRecord record, CastingQualificationAllowance allowance, Func<long> clock,
            bool recipientReads = true)
        {
            Directory.CreateDirectory(dir);
            var host = new CastingExecutionHost(settings => settings != null && settings.Mode == "animated"
                ? (ICastExecutor)new AnimatedCastExecutor(world, true)
                : new InstantCastExecutor(world, true), clock);
            return new CastingQualificationDriver(record, allowance, "fixture-campaign", world.Inputs,
                boundary => new CastingWorkspaceSession(dir, "fixture-campaign", boundary),
                host, world.Observe, clock, 240000, null, null, false, null, null,
                () => "fixture-lifecycle=1", () => 0L,
                recipientReads ? world.ObserveRecipient : (Func<CastStep, string, string, ProbeObservation>)null);
        }

        // The group recipe (mission batch 3, section 8, and the owner's
        // mixed-coverage case): selection, forecast and the driver run in
        // both modes, with the game keeping or replacing the covered
        // recipient's instance; one invocation and one unit of cost per
        // group casting; a missed recipient stops the routine before the
        // next casting; group reads need the recipient observer.
        private static void TestGroupQualificationRecipe(string root)
        {
            var world = new GroupBuffWorld();
            CastingWorkspaceInputs inputs = world.Inputs();
            CastingQualificationSelection selection =
                CastingQualificationRecipe.SelectGroupMixed(inputs, "fixture-campaign");
            if (!selection.Selected || selection.Recipe != CastingQualificationRecipe.GroupMixed ||
                selection.Castings.Count != 3 ||
                selection.Castings[0].CasterUnitId != "unit-arcanist" ||
                selection.Castings[0].DirectTargetUnitId != "unit-t1" ||
                selection.Castings[1].CasterUnitId != "unit-cleric" ||
                selection.Castings[1].TargetMode != CastingTargetMode.CasterCenteredOrigin ||
                selection.Castings[2].CasterUnitId != "unit-bard" ||
                selection.Castings[2].TargetMode != CastingTargetMode.AnchoredOrigin ||
                selection.Castings[2].Origin.AnchorUnitId != "unit-t1" ||
                !selection.Coverage.SequenceEqual(new[]
                    { "caster-centred", "mixed-coverage", "target-anchored", "spontaneous", "prepared" }))
                throw new InvalidOperationException("The group selection was wrong: " + selection.Refusal + " " +
                    string.Join(",", selection.Castings.Select(casting => casting.CastingId + "=" +
                        casting.CasterUnitId + ">" + casting.DirectTargetUnitId).ToArray()) + " coverage=" +
                    string.Join(",", selection.Coverage.ToArray()) + " rejections=" +
                    string.Join(",", selection.Rejections.ToArray()));
            IReadOnlyList<CastingQualificationStepForecast> forecast =
                CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign");
            CastStep groupStep = forecast.Count == 2 && forecast[1].Projection != null
                ? forecast[1].Projection.Plan.Steps[0] : null;
            if (forecast.Count != CastingQualificationRecipe.ForecastSteps(CastingQualificationRecipe.GroupMixed) ||
                !forecast[0].CastingIds.SequenceEqual(new[] { "qual-cast-1" }) ||
                !forecast[1].CastingIds.SequenceEqual(new[] { "qual-cast-2", "qual-cast-3" }) ||
                groupStep == null || !groupStep.MassCast || groupStep.ExpectedRecipientUnitIds.Count != 5 ||
                !groupStep.PreCoveredRecipientUnitIds.SequenceEqual(new[] { "unit-t1" }) ||
                groupStep.Reservation.TokenIds.Single() != world.Token ||
                !forecast[1].Projection.Plan.Steps[1].ExpectedRecipientUnitIds.SequenceEqual(
                    new[] { "unit-t1", "unit-t2" }))
                throw new InvalidOperationException("The group forecast was wrong: " +
                    string.Join(" | ", forecast.Select(step => step.Name + ":" + (step.Refusal ??
                        string.Join(",", step.CastingIds.ToArray()))).ToArray()));
            // Refused shapes: no direct source of the same effects, a
            // recipient already covered, a weaker direct caster.
            var different = new GroupBuffWorld { DirectEffect = "other-effect" };
            CastingQualificationSelection noShared =
                CastingQualificationRecipe.SelectGroupMixed(different.Inputs(), "fixture-campaign");
            var covered = new GroupBuffWorld();
            covered.Active["unit-t2|shared-effect"] = new KeyValuePair<string, long>("old", 5000);
            CastingQualificationSelection alreadyCovered =
                CastingQualificationRecipe.SelectGroupMixed(covered.Inputs(), "fixture-campaign");
            var weaker = new GroupBuffWorld { DirectCasterLevel = 5 };
            CastingQualificationSelection weakerDirect =
                CastingQualificationRecipe.SelectGroupMixed(weaker.Inputs(), "fixture-campaign");
            if (noShared.Selected || !noShared.Rejections.Any(value =>
                    value.EndsWith("|no-direct-source-with-the-same-effects", StringComparison.Ordinal)) ||
                alreadyCovered.Selected || !alreadyCovered.Rejections.Any(value =>
                    value.EndsWith("|a-recipient-already-covered", StringComparison.Ordinal)) ||
                weakerDirect.Selected || !weakerDirect.Rejections.Any(value =>
                    value.EndsWith("|direct-caster-level-lower", StringComparison.Ordinal)))
                throw new InvalidOperationException("A group selection that cannot prove mixed coverage was made.");
            foreach (string mode in new[] { "instant", "animated" })
                foreach (bool keep in new[] { true, false })
                {
                    var run = new GroupBuffWorld { KeepExisting = keep };
                    var record = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
                    long now = 0;
                    CastingQualificationDriver driver = NewGroupDriver(
                        Path.Combine(root, "qg-" + mode[0] + (keep ? "k" : "r")), run, record,
                        GroupAllowance(run, mode), () => now);
                    for (int i = 0; i < 4000 && !driver.Completed; i++) { now += 16; run.Now = now; driver.Update(); }
                    IList<string> violations = record.Violations();
                    CastingQualificationStepResult mixed = record.Step("mixed");
                    if (violations.Count != 0 || record.TerminalReason != "completed" ||
                        record.ExecutionMode != mode ||
                        !run.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-3" }) ||
                        run.TokenAvailable || run.Remaining["pool-arcanist"] != 2 || run.Remaining["pool-bard"] != 2 ||
                        mixed == null ||
                        !mixed.Transitions.Contains("qual-cast-2@unit-t1:" + (keep ? "unchanged" : "new-instance")) ||
                        !mixed.Transitions.Contains("qual-cast-2@unit-t2:new-instance") ||
                        !mixed.Transitions.Contains("qual-cast-2@unit-cleric:new-instance") ||
                        !mixed.Transitions.Contains("qual-cast-3@unit-t2:new-instance") ||
                        !mixed.Availability.Contains("qual-cast-2:1>0") ||
                        !mixed.Availability.Contains("qual-cast-1:2>2") ||
                        !mixed.Availability.Contains("qual-cast-3:3>2") ||
                        record.Step("repeat").ApplyReason != "nothing-to-cast:3")
                        throw new InvalidOperationException("The group qualification (" + mode + ", keep=" + keep +
                            ") did not pass exactly: " + record.TerminalReason + "|" +
                            string.Join("|", violations.ToArray()) + "|fired=" + string.Join(",", run.Fired.ToArray()) +
                            "|" + (mixed == null ? "no-mixed" : string.Join(",", mixed.Transitions.ToArray()) + "|" +
                                string.Join(",", mixed.Availability.ToArray())));
                }
            // A recipient the group cast misses: the casting is not confirmed
            // and the routine stops before the anchored casting is submitted.
            var missed = new GroupBuffWorld { MissRecipient = "unit-t2" };
            var missedRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            long missedNow = 0;
            CastingQualificationDriver missedDriver = NewGroupDriver(Path.Combine(root, "qg-miss"),
                missed, missedRecord, GroupAllowance(missed, "instant"), () => missedNow);
            for (int i = 0; i < 6000 && !missedDriver.Completed; i++) { missedNow += 16; missed.Now = missedNow; missedDriver.Update(); }
            if (missedRecord.TerminalReason == "completed" || missedRecord.Violations().Count == 0 ||
                !missed.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2" }))
                throw new InvalidOperationException("A group cast that missed a recipient passed or went on: " +
                    missedRecord.TerminalReason + "|fired=" + string.Join(",", missed.Fired.ToArray()));
            // Without the recipient reads a group step never starts.
            var blind = new GroupBuffWorld();
            var blindRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            long blindNow = 0;
            CastingQualificationDriver blindDriver = NewGroupDriver(Path.Combine(root, "qg-blind"),
                blind, blindRecord, GroupAllowance(blind, "instant"), () => blindNow, false);
            for (int i = 0; i < 4000 && !blindDriver.Completed; i++) { blindNow += 16; blind.Now = blindNow; blindDriver.Update(); }
            if (blindRecord.TerminalReason == "completed" || !blind.Fired.SequenceEqual(new[] { "qual-cast-1" }) ||
                !blindRecord.Failures.Any(value => value.Contains("recipient-observer-missing")))
                throw new InvalidOperationException("A group step ran without its recipient reads: " +
                    blindRecord.TerminalReason + "|" + string.Join("|", blindRecord.Failures.ToArray()));
        }

        // A finite world: a prepared caster (exact slot tokens with native
        // ids, optionally linked pairs) and a spontaneous caster (one level
        // count), both casting the fixture buff by rule. Firing spends
        // exactly the step reservation unless a failure shape says
        // otherwise; observation shapes make reads fail or go missing.
        private sealed class FiniteBuffWorld : IInstantCastRuntimeAdapter,
            ICastEnhancementRuntimeAdapter
        {
            internal readonly Dictionary<string, KeyValuePair<string, long>> Active =
                new Dictionary<string, KeyValuePair<string, long>>(StringComparer.Ordinal);
            internal readonly List<string> Fired = new List<string>();
            internal readonly SortedDictionary<string, bool> Tokens =
                new SortedDictionary<string, bool>(StringComparer.Ordinal);
            internal readonly List<string> Primaries = new List<string>();
            internal readonly Dictionary<string, string> LinkOf =
                new Dictionary<string, string>(StringComparer.Ordinal);
            internal int SpontaneousRemaining = 1;
            internal AbilityKey Ability = CastingBuffAbility;
            internal string WrongTokenCasting;
            internal string PartialCasting;
            internal bool NoSpend;
            internal bool MissingAfterTokens;
            internal bool MissingBeforeTokens;
            private int _instances;
            private long _sequence;

            // Native slot ids (level-2, type 0); linked pairs are primary
            // index 0/2 with secondary index 1/3.
            internal FiniteBuffWorld(bool linkedPairs = false)
            {
                if (linkedPairs)
                {
                    for (int pair = 0; pair < 2; pair++)
                    {
                        string primary = PreparedSlotIds.Format(2, 0, pair * 2);
                        string secondary = PreparedSlotIds.Format(2, 0, pair * 2 + 1);
                        Tokens[primary] = true;
                        Tokens[secondary] = true;
                        Primaries.Add(primary);
                        LinkOf[primary] = secondary;
                        LinkOf[secondary] = primary;
                    }
                }
                else
                {
                    for (int index = 0; index < 3; index++)
                    {
                        string token = PreparedSlotIds.Format(2, 0, index);
                        Tokens[token] = true;
                        Primaries.Add(token);
                    }
                }
            }

            internal string Token(int index) { return PreparedSlotIds.Format(2, 0, index); }
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            { return CastEnhancementPreparation.Pass(null); }
            public InstantCastResult Fire(CastStep step)
            {
                Fired.Add(step.AssignmentId);
                Active[step.TargetUnitIds[0]] = new KeyValuePair<string, long>("i" + (++_instances), 600);
                bool spent = false;
                if (!NoSpend && !step.Reservation.Unlimited)
                {
                    if (step.Reservation.TokenIds.Count != 0)
                    {
                        List<string> spend = step.AssignmentId == WrongTokenCasting
                            ? Primaries.Where(key => Tokens[key] &&
                                !step.Reservation.TokenIds.Contains(key)).Take(1).ToList()
                            : step.AssignmentId == PartialCasting
                                ? step.Reservation.TokenIds.Take(1).ToList()
                                : step.Reservation.TokenIds.ToList();
                        foreach (string token in spend) Tokens[token] = false;
                    }
                    else SpontaneousRemaining -= step.Reservation.Units;
                    spent = true;
                }
                return new InstantCastResult(true, true, true, spent, "simulated");
            }
            public bool EffectsObserved(CastStep step) { return Active.ContainsKey(step.TargetUnitIds[0]); }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("simulated-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("simulated-clean"); }

            internal ActiveEffectSnapshot Live()
            {
                return LiveEffects(Active.Select(pair => On(pair.Key, "buff-effect", 100, 1, 0)).ToArray());
            }

            internal ProbeObservation Observe(CastStep step, string label)
            {
                KeyValuePair<string, long> instance;
                string target = step.TargetUnitIds[0];
                var instances = Active.TryGetValue(target, out instance)
                    ? new[] { new ProbeEffectInstance("buff-effect", instance.Key, instance.Value) }
                    : new ProbeEffectInstance[0];
                bool prepared = step.Reservation.TokenIds.Count != 0;
                // AvailableForCast counts casts: available primaries.
                int available = prepared ? Primaries.Count(key => Tokens[key]) : SpontaneousRemaining;
                bool dropTokens = (MissingAfterTokens && label.Contains("-after")) ||
                    (MissingBeforeTokens && label.Contains("-before"));
                Dictionary<string, bool> reserved = prepared && !dropTokens
                    ? step.Reservation.TokenIds.ToDictionary(id => id, id => Tokens[id], StringComparer.Ordinal)
                    : null;
                return ProbeObservation.Read(label, ++_sequence, DateTime.UtcNow, target, available,
                    instances, reserved);
            }
            internal CastingWorkspaceInputs Inputs()
            {
                string[] others = { "unit-t1", "unit-t2", "unit-t3", "unit-t4" };
                List<string> all = new[] { "unit-sorcerer", "unit-wizard" }.Concat(others).ToList();
                List<UnitSnapshot> units = all.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))).ToList();
                var wizard = new ProviderSnapshot(new ProviderKey("unit-wizard", "book-wizard",
                    Ability, "level-2"), Ability.BaseAbilityGuid, 2,
                    "pool-unit-wizard", 1, Primaries, null, 1);
                var sorcerer = new ProviderSnapshot(new ProviderKey("unit-sorcerer", "book-sorcerer",
                    Ability, "level-2"), Ability.BaseAbilityGuid, 2,
                    "pool-unit-sorcerer", 1, null, null, 1);
                var pools = new[]
                {
                    new ResourcePoolSnapshot("pool-unit-wizard", ResourcePoolKind.PreparedSlots, Tokens.Count,
                        Tokens.Count(pair => pair.Value), Tokens.Select(pair => new ResourceTokenSnapshot(
                            pair.Key, Ability, 2, PreparedSlotKind.Common, pair.Value,
                            Primaries.Contains(pair.Key),
                            LinkOf.ContainsKey(pair.Key) ? new[] { LinkOf[pair.Key] } : null))),
                    new ResourcePoolSnapshot("pool-unit-sorcerer", ResourcePoolKind.SpontaneousLevel, 3,
                        SpontaneousRemaining, null)
                };
                var options = new[] { wizard, sorcerer }.Select(provider => new ProviderPlanningOption(
                    provider, all, new[] { provider.Key.CasterUnitId }, 10, 100,
                    CastExecutionStrategy.DirectRuleCast, "fixture-direct",
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal))).ToList();
                return new CastingWorkspaceInputs(new PartyProviderSnapshot(units,
                        new[] { wizard, sorcerer }, pools), options,
                    CastingEffectsWithAbilityAlias("source-bulls", "source-communal", Ability),
                    new CastEnhancementSnapshot[0], null, Live());
            }
        }

        private static CastingQualificationAllowance FiniteAllowance(FiniteBuffWorld world, int maximum = 4)
        {
            CastingWorkspaceInputs inputs = world.Inputs();
            CastingQualificationSelection selection =
                CastingQualificationRecipe.SelectFiniteDirectMixed(inputs, "fixture-campaign");
            IReadOnlyList<CastingQualificationStepForecast> forecast =
                CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign");
            string refusal;
            return CastingQualificationAllowance.Parse(QualificationAllowanceJson(o =>
            {
                o["fixtureGameId"] = "fixture-campaign";
                o["recipe"] = CastingQualificationRecipe.FiniteDirectMixed;
                o["approvedProjectionIds"] = new JArray(forecast.Select(step => (object)step.ProjectionId).ToArray());
                o["maximumNativeSubmissions"] = maximum;
            }), "qual-run-1", out refusal);
        }

        private static CastingQualificationDriver NewFiniteDriver(string dir, FiniteBuffWorld world,
            CastingQualificationRecord record, CastingQualificationAllowance allowance, Func<long> clock,
            string recipe = null)
        {
            Directory.CreateDirectory(dir);
            var host = new CastingExecutionHost(settings => new InstantCastExecutor(world, true), clock);
            return new CastingQualificationDriver(record, allowance, "fixture-campaign", world.Inputs,
                boundary => new CastingWorkspaceSession(dir, "fixture-campaign", boundary),
                host, world.Observe, clock, 240000, recipe);
        }

        // The finite-direct-mixed recipe: a prepared caster with exact slot
        // tokens and a spontaneous caster. The forecast spends exactly what
        // each step reserves (the recast takes the NEXT prepared token), and
        // the judged run checks availability and the exact tokens per step,
        // stopping at the first wrong one.
        private static void TestFiniteQualificationRecipe(string root)
        {
            var world = new FiniteBuffWorld();
            CastingWorkspaceInputs inputs = world.Inputs();
            CastingQualificationSelection selection =
                CastingQualificationRecipe.SelectFiniteDirectMixed(inputs, "fixture-campaign");
            if (!selection.Selected || selection.Recipe != CastingQualificationRecipe.FiniteDirectMixed ||
                selection.Castings.Count != 2 ||
                selection.Castings[0].CasterUnitId != "unit-wizard" ||
                selection.Castings[1].CasterUnitId != "unit-sorcerer" ||
                selection.Castings[0].DirectTargetUnitId != "unit-t1" ||
                selection.Castings[1].DirectTargetUnitId != "unit-t2" ||
                !selection.Coverage.SequenceEqual(new[] { "prepared", "spontaneous", "mixed-caster" }))
                throw new InvalidOperationException("The finite selection was wrong: " + selection.Refusal +
                    " " + string.Join(",", selection.Castings.Select(casting => casting.CastingId + "=" +
                        casting.CasterUnitId + ">" + casting.DirectTargetUnitId).ToArray()) +
                    " coverage=" + string.Join(",", selection.Coverage.ToArray()));
            IReadOnlyList<CastingQualificationStepForecast> forecast =
                CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign");
            if (forecast.Count != 3 || forecast.Any(step => step.ProjectionId == null) ||
                !forecast[0].Projection.Plan.Steps[0].Reservation.TokenIds.SequenceEqual(new[] { world.Token(0) }) ||
                !forecast[1].CastingIds.SequenceEqual(new[] { "qual-cast-2" }) ||
                forecast[1].Projection.Plan.Steps[0].Reservation.Units != 1 ||
                !forecast[2].CastingIds.SequenceEqual(new[] { "qual-cast-1" }) ||
                !forecast[2].Projection.Plan.Steps[0].Reservation.TokenIds.SequenceEqual(new[] { world.Token(1) }))
                throw new InvalidOperationException("The finite forecast did not spend exactly: " +
                    string.Join(" | ", forecast.Select(step => step.Name + ":" + (step.Refusal ??
                        string.Join(",", step.CastingIds.ToArray()))).ToArray()));
            var poor = new FiniteBuffWorld();
            poor.Tokens[poor.Token(1)] = false;
            poor.Tokens[poor.Token(2)] = false;
            CastingQualificationSelection refused =
                CastingQualificationRecipe.SelectFiniteDirectMixed(poor.Inputs(), "fixture-campaign");
            if (refused.Selected || !refused.Rejections.Any(value => value.EndsWith(
                    "|no-caster-pair-with-casts:2+1", StringComparison.Ordinal)))
                throw new InvalidOperationException("A party without two casts for one caster was selected.");
            if (CastingQualificationRecipe.SelectFiniteDirectMixed(QualificationInputs(true, true, null),
                    "fixture-campaign").Selected)
                throw new InvalidOperationException("Verified-free pools satisfied the finite recipe.");
            // A metamagic variant (Extend): the forecast grants the effect
            // WITH that metamagic, so the complete step still skips the
            // first casting instead of recasting it as "missing Extend".
            var extended = new FiniteBuffWorld
            {
                Ability = new AbilityKey(CastingBuffAbility.BaseAbilityGuid, CastingBuffAbility.VariantGuid,
                    8, SourceKind.Spellbook, null)
            };
            CastingWorkspaceInputs extendedInputs = extended.Inputs();
            CastingQualificationSelection extendedSelection =
                CastingQualificationRecipe.SelectFiniteDirectMixed(extendedInputs, "fixture-campaign");
            IReadOnlyList<CastingQualificationStepForecast> extendedForecast = !extendedSelection.Selected ? null
                : CastingQualificationForecast.Forecast(extendedSelection, extendedInputs, "fixture-campaign");
            if (extendedForecast == null || !extendedSelection.Coverage.Contains("metamagic") ||
                !extendedForecast[1].CastingIds.SequenceEqual(new[] { "qual-cast-2" }) ||
                !extendedForecast[2].CastingIds.SequenceEqual(new[] { "qual-cast-1" }))
                throw new InvalidOperationException("The metamagic forecast was wrong: " + extendedSelection.Refusal +
                    (extendedForecast == null ? string.Empty : " " + string.Join(" | ", extendedForecast
                        .Select(step => step.Name + ":" + (step.Refusal ??
                            string.Join(",", step.CastingIds.ToArray()))).ToArray())));
            long now = 0;
            var run = new FiniteBuffWorld();
            var record = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            CastingQualificationDriver driver = NewFiniteDriver(Path.Combine(root, "qualification-finite"),
                run, record, FiniteAllowance(run), () => now);
            for (int i = 0; i < 2000 && !driver.Completed; i++) driver.Update();
            if (record.Violations().Count != 0 || record.TerminalReason != "completed" ||
                !run.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-1" }) ||
                run.Tokens[run.Token(0)] || run.Tokens[run.Token(1)] || !run.Tokens[run.Token(2)] ||
                run.SpontaneousRemaining != 0 ||
                !record.Step("recast").TokenReadings.Any(reading => reading.CastingId == "qual-cast-1" &&
                    reading.TokenId == run.Token(1) && reading.Before == true && reading.After == false) ||
                !record.Step("complete").Availability.Contains("qual-cast-2:1>0"))
                throw new InvalidOperationException("The finite run was not accepted exactly: " +
                    string.Join("|", record.Violations().ToArray()) + " fired=" +
                    string.Join(",", run.Fired.ToArray()));
            // Native slot ids contain "|"; every shape must still be judged
            // exactly (review RC1), and a missing before-read casts nothing
            // (review RC2).
            foreach (string shape in new[] { "wrong-token", "no-spend", "partial", "missing-after-tokens",
                "missing-before-tokens" })
            {
                var bad = new FiniteBuffWorld(shape == "partial");
                CastingQualificationAllowance allowance = FiniteAllowance(bad);
                if (shape == "wrong-token") bad.WrongTokenCasting = "qual-cast-1";
                else if (shape == "no-spend") bad.NoSpend = true;
                else if (shape == "partial") bad.PartialCasting = "qual-cast-1";
                else if (shape == "missing-after-tokens") bad.MissingAfterTokens = true;
                else bad.MissingBeforeTokens = true;
                var badRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
                CastingQualificationDriver badDriver = NewFiniteDriver(
                    Path.Combine(root, "qf-" + string.Join(string.Empty, shape.Split(new[] { "-" },
                        StringSplitOptions.None).Select(part => part.Substring(0, 1)).ToArray())),
                    bad, badRecord, allowance, () => now);
                for (int i = 0; i < 2000 && !badDriver.Completed; i++) badDriver.Update();
                string expected =
                    shape == "wrong-token" ? "stop-wait:step:tokens:qual-cast-1:" + bad.Token(0) + "=T>T"
                    : shape == "no-spend" ? "stop-wait:step:resource:qual-cast-1:3>3:expected-spend=1"
                    : shape == "partial" ? "stop-wait:step:tokens:qual-cast-1:" + bad.Token(1) + "=T>T"
                    : shape == "missing-after-tokens" ? "stop-wait:step:tokens:qual-cast-1:unread"
                    : "stop:before-read:qual-cast-1:tokens-unread";
                string[] fired = shape == "missing-before-tokens" ? new string[0] : new[] { "qual-cast-1" };
                if (!bad.Fired.SequenceEqual(fired) || !badRecord.Failures.Contains(expected))
                    throw new InvalidOperationException("The " + shape + " shape was not stopped at the stop step: " +
                        string.Join("|", badRecord.Failures.ToArray()) + " fired=" +
                        string.Join(",", bad.Fired.ToArray()));
            }
            // Linked prepared slots: each cast spends its primary and linked
            // token, and exactly those, in every step.
            var linked = new FiniteBuffWorld(true);
            var linkedRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            CastingQualificationDriver linkedDriver = NewFiniteDriver(
                Path.Combine(root, "qf-linked"), linked, linkedRecord,
                FiniteAllowance(linked), () => now);
            for (int i = 0; i < 2000 && !linkedDriver.Completed; i++) linkedDriver.Update();
            if (linkedRecord.Violations().Count != 0 || linked.Tokens.Values.Any(value => value) ||
                !linked.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-1" }) ||
                linkedRecord.Step("stop").TokenReadings.Count(reading => reading.CastingId == "qual-cast-1" &&
                    reading.Before == true && reading.After == false) != 2)
                throw new InvalidOperationException("The linked-slot run was not judged exactly: " +
                    string.Join("|", linkedRecord.Violations().ToArray()));
            var mismatched = new FiniteBuffWorld();
            var mismatchedRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            CastingQualificationDriver mismatchedDriver = NewFiniteDriver(
                Path.Combine(root, "qualification-finite-recipe-mismatch"), mismatched, mismatchedRecord,
                FiniteAllowance(mismatched), () => now, CastingQualificationRecipe.ZeroCostMixed);
            for (int i = 0; i < 20 && !mismatchedDriver.Completed; i++) mismatchedDriver.Update();
            if (mismatched.Fired.Count != 0 || mismatchedRecord.AllowanceStatus != "recipe-differs-from-request")
                throw new InvalidOperationException("A recipe different from the allowance ran.");
            string unknownRefusal;
            if (CastingQualificationAllowance.Parse(QualificationAllowanceJson(o => o["recipe"] = "improvised"),
                    "qual-run-1", out unknownRefusal) != null || unknownRefusal != "allowance-recipe-unknown")
                throw new InvalidOperationException("An unknown recipe was accepted.");
        }


        // The production executor choice: animated settings get the animated
        // executor, instant settings the instant one.
        private static CastingExecutionHost QualificationHost(SimulatedBuffWorld world, Func<long> clock)
        {
            return new CastingExecutionHost(settings => settings != null && settings.Mode == "animated"
                ? (ICastExecutor)new AnimatedCastExecutor(world, true)
                : new InstantCastExecutor(world, true), clock);
        }

        private static CastingQualificationDriver NewQualificationDriver(string dir,
            SimulatedBuffWorld world, CastingQualificationRecord record,
            CastingQualificationAllowance allowance, Func<long> clock, Func<bool> worldRunning = null,
            Func<long> hostClock = null, CastingExecutionHost host = null, bool ownerPumpsHost = false,
            Func<string, bool> pressRoutine = null, Action<bool> setPlannerEnabled = null,
            Func<string> lifecycleProbe = null, Func<long> ownerTicks = null)
        {
            host = host ?? QualificationHost(world, hostClock ?? clock);
            lifecycleProbe = lifecycleProbe ?? (() => "fixture-lifecycle=1");
            ownerTicks = ownerTicks ?? (() => 0L);
            return new CastingQualificationDriver(record, allowance, "fixture-campaign",
                () => QualificationInputs(true, true, world.Live(), world.OtherUnits),
                boundary => new CastingWorkspaceSession(dir, "fixture-campaign", boundary),
                host, world.Observe, clock, 240000, null, worldRunning, ownerPumpsHost, pressRoutine,
                setPlannerEnabled, lifecycleProbe, ownerTicks);
        }

        private static CastingQualificationAllowance ForecastAllowance(SimulatedBuffWorld world,
            Func<IReadOnlyList<CastingQualificationStepForecast>, IEnumerable<string>> ids = null,
            string mode = "instant")
        {
            CastingWorkspaceInputs inputs = QualificationInputs(true, true, world.Live(), world.OtherUnits);
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectZeroCostMixed(
                inputs, "fixture-campaign");
            IReadOnlyList<CastingQualificationStepForecast> forecast =
                CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign");
            IEnumerable<string> approved = ids == null
                ? forecast.Select(step => step.ProjectionId) : ids(forecast);
            string refusal;
            return CastingQualificationAllowance.Parse(QualificationAllowanceJson(o =>
            {
                o["fixtureGameId"] = "fixture-campaign";
                o["executionMode"] = mode;
                o["approvedProjectionIds"] = new JArray(approved.Cast<object>().ToArray());
                o["maximumNativeSubmissions"] = 8;
            }), "qual-run-1", out refusal);
        }

        // Animated, the player default: the player's stop is pressed while
        // the first cast is in progress; that cast completes, nothing after
        // it starts, and every run executes in the approved mode.
        private static void TestQualificationAnimatedPlayerStop(string root)
        {
            var world = new SimulatedBuffWorld();
            CastingQualificationAllowance allowance = ForecastAllowance(world, null, "animated");
            var record = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string dir = Path.Combine(root, "qa-animated");
            Directory.CreateDirectory(dir);
            long now = 0;
            CastingQualificationDriver driver = NewQualificationDriver(dir, world, record, allowance, () => now);
            for (int i = 0; i < 5000 && !driver.Completed; i++) { now += 16; world.Now = now; driver.Update(); }
            IList<string> violations = record.Violations();
            if (record.TerminalReason != "completed" || violations.Count != 0 ||
                record.ExecutionMode != "animated" || record.StopPressedInFlight != true ||
                !record.StopPressHandled ||
                record.StopPress != "host-request;handled=True;inFlight=True;pending=player-stopped" ||
                !world.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-3", "qual-cast-1", "qual-cast-1" }) ||
                !world.AnimatedStarts.SequenceEqual(world.Fired.Concat(new[] { "qual-cast-1" })) ||
                record.Submissions.Count != 5 ||
                record.Step("recover").Report.TerminalReason != "completed" ||
                record.RunsStarted != 5 || record.RunsReported != 5 || record.CallbackFailure != null ||
                record.LifecycleBefore != "fixture-lifecycle=1" || record.LifecycleAfter != "fixture-lifecycle=1" ||
                record.Submissions.Any(value => !value.EndsWith(";mode=animated", StringComparison.Ordinal)) ||
                record.Step("stop").Report.TerminalReason != "cancelled:" + CastingExecutionHost.PlayerStopReason ||
                record.DisabledAt != "in-flight" || !record.AcceptingAfterEnable ||
                record.DisableHeldUpdates != CastingQualificationDriver.DisableHoldUpdates ||
                record.AcceptingWhileDisabled || record.RunningWhileDisabled ||
                record.OwnerTicksDuringHold != 0 || record.RunsStartedDuringHold != 0 ||
                record.Disable != "host-shutdown;at=in-flight;ended=True;held=5;acceptingWhileDisabled=False;" +
                    "runningWhileDisabled=False;ownerTicks=0;runsDuringHold=0;accepting=True" ||
                record.Step("disable").Report.Entries.First(entry => entry.CastingId == "qual-cast-1").State !=
                    CastingOutcomeState.Cancelled)
                throw new InvalidOperationException("The animated qualification was not accepted exactly: " +
                    record.TerminalReason + "|" + string.Join("|", violations.ToArray()) + "|" + record.StopPress +
                    "|fired=" + string.Join(",", world.Fired.ToArray()) + "|" +
                    string.Join(";", record.Submissions.ToArray()));
            // The rule: in animated mode a press that arrived only after the
            // first cast finished is not the in-flight stop the step claims
            // (in instant mode either is the player stop); a press the host
            // did not take, or none, never passes.
            record.StopPressedInFlight = false;
            if (!record.Violations().SequenceEqual(new[] { "stop-press:not-in-flight:" + record.StopPress }))
                throw new InvalidOperationException("A late animated stop press passed: " +
                    string.Join("|", record.Violations().ToArray()));
            record.ExecutionMode = "instant";
            if (record.Violations().Any(value => value.StartsWith("stop-press:", StringComparison.Ordinal)))
                throw new InvalidOperationException("An instant stop press between castings was refused.");
            record.ExecutionMode = "animated";
            record.StopPressedInFlight = true;
            record.StopPressHandled = false;
            if (!record.Violations().SequenceEqual(new[] { "stop-press:not-handled:" + record.StopPress }))
                throw new InvalidOperationException("A stop press the host did not take passed.");
            string stopPress = record.StopPress;
            record.StopPress = null;
            if (!record.Violations().SequenceEqual(new[] { "stop-press:none" }))
                throw new InvalidOperationException("A run without a stop press passed.");
            record.StopPress = stopPress;
            record.StopPressHandled = true;
            // The disable rule: in animated mode it lands while the cast is
            // in progress, and the host accepts runs again once enabled.
            record.DisabledAt = "between-castings";
            if (!record.Violations().SequenceEqual(new[] { "disable:not-in-flight:" + record.Disable }))
                throw new InvalidOperationException("A late animated disable passed: " +
                    string.Join("|", record.Violations().ToArray()));
            record.DisabledAt = "in-flight";
            record.AcceptingAfterEnable = false;
            if (!record.Violations().SequenceEqual(new[] { "disable:not-resumed:" + record.Disable }))
                throw new InvalidOperationException("A host that never resumed passed.");
            record.AcceptingAfterEnable = true;
            // Review B7: the disable is held over whole updates with nothing
            // running or accepted, and the lifecycle is a real probe's line.
            record.DisableHeldUpdates = CastingQualificationDriver.DisableHoldUpdates - 1;
            if (!record.Violations().SequenceEqual(new[] { "disable:not-held:" + record.Disable }))
                throw new InvalidOperationException("A disable enabled again at once passed.");
            record.DisableHeldUpdates = CastingQualificationDriver.DisableHoldUpdates;
            foreach (Action<CastingQualificationRecord> active in new Action<CastingQualificationRecord>[]
                { value => value.AcceptingWhileDisabled = true, value => value.RunningWhileDisabled = true })
            {
                active(record);
                if (!record.Violations().SequenceEqual(new[] { "disable:active-while-disabled:" + record.Disable }))
                    throw new InvalidOperationException("A host active while disabled passed.");
                record.AcceptingWhileDisabled = false;
                record.RunningWhileDisabled = false;
            }
            record.OwnerTicksDuringHold = 2;
            if (!record.Violations().SequenceEqual(new[] { "disable:owner-ticked-while-disabled:" + record.Disable }))
                throw new InvalidOperationException("An owner that ticked during the disable passed.");
            record.OwnerTicksDuringHold = null;
            if (!record.Violations().SequenceEqual(new[] { "disable:owner-ticked-while-disabled:" + record.Disable }))
                throw new InvalidOperationException("An unread owner tick count passed.");
            record.OwnerTicksDuringHold = 0;
            record.RunsStartedDuringHold = 1;
            if (!record.Violations().SequenceEqual(new[] { "disable:run-started-while-disabled:" + record.Disable }))
                throw new InvalidOperationException("A run started during the disable passed.");
            record.RunsStartedDuringHold = 0;
            foreach (string stand in new[] { CastingQualificationDriver.Unprobed, "null", "probe-failed:Exception", "" })
            {
                record.LifecycleBefore = stand;
                record.LifecycleAfter = stand;
                if (!record.Violations().SequenceEqual(new[] { "lifecycle:" + stand + ">" + stand }))
                    throw new InvalidOperationException("A lifecycle without a real probe passed: " + stand);
            }
            record.LifecycleBefore = "fixture-lifecycle=1";
            record.LifecycleAfter = "fixture-lifecycle=1";
            if (record.Violations().Count != 0)
                throw new InvalidOperationException("The restored record is not clean.");
            record.Disable = null;
            if (!record.Violations().SequenceEqual(new[] { "disable:none" }))
                throw new InvalidOperationException("A run without the disable step passed.");
        }

        // The disable step fails closed: an interrupted cast whose effect
        // lands anyway fails the step (and ends the run there), and a host
        // that never resumes after the planner is enabled is a violation.
        private static void TestQualificationDisableStepRules(string root)
        {
            long now = 0;
            var landing = new SimulatedBuffWorld { LandOnInterrupt = true };
            var landingRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string landingDir = Path.Combine(root, "qd-landing");
            Directory.CreateDirectory(landingDir);
            CastingQualificationDriver landingDriver = NewQualificationDriver(landingDir, landing, landingRecord,
                ForecastAllowance(landing, null, "animated"), () => now);
            for (int i = 0; i < 5000 && !landingDriver.Completed; i++) { now += 16; landing.Now = now; landingDriver.Update(); }
            if (landingRecord.TerminalReason != "failed:disable-wait" ||
                !landingRecord.Failures.Any(value => value.StartsWith("disable-wait:step:effects:", StringComparison.Ordinal)) ||
                !landing.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-3", "qual-cast-1", "qual-cast-1" }))
                throw new InvalidOperationException("An interrupted cast that landed passed the disable step: " +
                    landingRecord.TerminalReason + "|" + string.Join("|", landingRecord.Failures.ToArray()));
            var stuck = new SimulatedBuffWorld();
            CastingExecutionHost stuckHost = QualificationHost(stuck, () => now);
            var stuckRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string stuckDir = Path.Combine(root, "qd-stuck");
            Directory.CreateDirectory(stuckDir);
            CastingQualificationDriver stuckDriver = NewQualificationDriver(stuckDir, stuck, stuckRecord,
                ForecastAllowance(stuck, null, "animated"), () => now, null, null, stuckHost, false, null,
                enabled => { if (!enabled) stuckHost.Shutdown(CastingQualificationDriver.DisableReason); });
            for (int i = 0; i < 5000 && !stuckDriver.Completed; i++) { now += 16; stuck.Now = now; stuckDriver.Update(); }
            // The disable step itself fails (the host never resumed): the
            // run ends there and the recover run is never submitted.
            if (stuckRecord.AcceptingAfterEnable || stuckRecord.TerminalReason != "failed:disable-wait" ||
                !stuckRecord.Violations().SequenceEqual(new[]
                {
                    "disable-wait:step:disable:not-resumed:" + stuckRecord.Disable
                }) ||
                stuck.AnimatedStarts.Count != 5 ||
                stuckRecord.Disable != "planner-disable;at=in-flight;ended=True;held=5;" +
                    "acceptingWhileDisabled=False;runningWhileDisabled=False;ownerTicks=0;runsDuringHold=0;accepting=False")
                throw new InvalidOperationException("A host that never resumed passed: " +
                    string.Join("|", stuckRecord.Violations().ToArray()));
            // Re-review: a failed disable rule stops the run at the disable
            // step, before the recover run is submitted. A disable that
            // resumes the host at once (accepting while disabled) and an
            // owner that keeps ticking while disabled each fail there.
            foreach (string shape in new[] { "resumes-at-once", "owner-ticks" })
            {
                var world = new SimulatedBuffWorld();
                CastingExecutionHost shapeHost = QualificationHost(world, () => now);
                var shapeRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
                string shapeDir = Path.Combine(root, "qd-" + shape);
                Directory.CreateDirectory(shapeDir);
                long ticking = 0;
                CastingQualificationDriver shapeDriver = NewQualificationDriver(shapeDir, world, shapeRecord,
                    ForecastAllowance(world, null, "animated"), () => now, null, null, shapeHost, false, null,
                    enabled =>
                    {
                        if (enabled) { shapeHost.Resume(); return; }
                        shapeHost.Shutdown(CastingQualificationDriver.DisableReason);
                        if (shape == "resumes-at-once") shapeHost.Resume();
                    }, null, () => ticking);
                for (int i = 0; i < 5000 && !shapeDriver.Completed; i++)
                {
                    now += 16;
                    world.Now = now;
                    if (shape == "owner-ticks") ticking++;
                    shapeDriver.Update();
                }
                string expected = shape == "resumes-at-once" ? "active-while-disabled" : "owner-ticked-while-disabled";
                if (shapeRecord.TerminalReason != "failed:disable-wait" || world.AnimatedStarts.Count != 5 ||
                    !shapeRecord.Failures.Any(value => value.StartsWith("disable-wait:step:disable:" + expected,
                        StringComparison.Ordinal)))
                    throw new InvalidOperationException("A failed disable rule (" + shape + ") did not stop the run " +
                        "before recover: " + shapeRecord.TerminalReason + "|started=" + world.AnimatedStarts.Count + "|" +
                        string.Join("|", shapeRecord.Failures.ToArray()));
            }
            // A lifecycle probe that fails before the disable stops the run at
            // the disable step: the recover run is never submitted.
            {
                var world = new SimulatedBuffWorld();
                var probeRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
                string probeDir = Path.Combine(root, "qd-probe-fails");
                Directory.CreateDirectory(probeDir);
                CastingQualificationDriver probeDriver = NewQualificationDriver(probeDir, world, probeRecord,
                    ForecastAllowance(world, null, "animated"), () => now, null, null, null, false, null, null,
                    () => { throw new InvalidOperationException("fixture-probe"); });
                for (int i = 0; i < 5000 && !probeDriver.Completed; i++)
                {
                    now += 16;
                    world.Now = now;
                    probeDriver.Update();
                }
                if (probeRecord.TerminalReason != "failed:disable-wait" || world.AnimatedStarts.Count != 5 ||
                    !probeRecord.Failures.Contains(
                        "disable-wait:step:lifecycle-unprobed:probe-failed:InvalidOperationException"))
                    throw new InvalidOperationException("A failed lifecycle probe did not stop the run before recover: " +
                        probeRecord.TerminalReason + "|started=" + world.AnimatedStarts.Count + "|" +
                        string.Join("|", probeRecord.Failures.ToArray()));
            }
            // A run that ends during the hold (here its deadline) enables the
            // planner again; it never leaves it disabled.
            {
                var world = new SimulatedBuffWorld();
                CastingExecutionHost endHost = QualificationHost(world, () => now);
                var endRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
                string endDir = Path.Combine(root, "qd-end-in-hold");
                Directory.CreateDirectory(endDir);
                var endToggles = new List<bool>();
                CastingQualificationDriver endDriver = NewQualificationDriver(endDir, world, endRecord,
                    ForecastAllowance(world, null, "animated"), () => now, null, null, endHost, false, null,
                    enabled =>
                    {
                        endToggles.Add(enabled);
                        if (enabled) endHost.Resume();
                        else endHost.Shutdown(CastingQualificationDriver.DisableReason);
                    });
                bool jumped = false;
                for (int i = 0; i < 5000 && !endDriver.Completed; i++)
                {
                    now += 16;
                    world.Now = now;
                    if (!jumped && endRecord.DisableHeldUpdates >= 2)
                    {
                        now += 300000;
                        jumped = true;
                    }
                    endDriver.Update();
                }
                if (!jumped || endRecord.TerminalReason != "qualification-deadline" ||
                    !endToggles.SequenceEqual(new[] { false, true }) || !endHost.Accepting ||
                    endRecord.Disable == null || !endRecord.Disable.EndsWith(";enabled-at-finish", StringComparison.Ordinal))
                    throw new InvalidOperationException("A run that ended during the hold left the planner disabled: " +
                        endRecord.TerminalReason + "|" + endRecord.Disable + "|" +
                        string.Join(",", endToggles.Select(value => value.ToString()).ToArray()));
            }
            // A disable that leaves the host running and accepting is seen in
            // the held updates, and the run never passes.
            var deaf = new SimulatedBuffWorld();
            var deafRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string deafDir = Path.Combine(root, "qd-deaf-disable");
            Directory.CreateDirectory(deafDir);
            var toggles = new List<bool>();
            CastingQualificationDriver deafDriver = NewQualificationDriver(deafDir, deaf, deafRecord,
                ForecastAllowance(deaf, null, "animated"), () => now, null, null, null, false, null,
                enabled => toggles.Add(enabled));
            for (int i = 0; i < 5000 && !deafDriver.Completed; i++) { now += 16; deaf.Now = now; deafDriver.Update(); }
            if (!deafRecord.AcceptingWhileDisabled || !deafRecord.RunningWhileDisabled ||
                deafRecord.TerminalReason == "completed" || deafRecord.Violations().Count == 0 ||
                !toggles.SequenceEqual(new[] { false, true }))
                throw new InvalidOperationException("A disable that stopped nothing passed: " +
                    deafRecord.TerminalReason + "|" + deafRecord.Disable + "|" +
                    string.Join("|", deafRecord.Violations().ToArray()));
            // The step rule on a judged report: an instant run that ended
            // before its first step submitted nothing; a report claiming a
            // submission for it (or an animated one claiming none) is wrong.
            foreach (string mode in new[] { "instant", "animated" })
                foreach (bool submitted in new[] { false, true })
                {
                    var judged = new CastingQualificationRecord { CastingScenario = true, ExecutionMode = mode };
                    judged.Selection = new CastingQualificationSelection(null, "source-bulls", CastingBuffAbility,
                        new[]
                        {
                            DirectCasting("qual-cast-1", "long", "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility),
                            DirectCasting("qual-cast-2", "long", "unit-wizard", "unit-t2", "source-bulls", CastingBuffAbility)
                        }, 1, null);
                    var step = new CastingQualificationStepResult("disable");
                    bool animated = mode == "animated";
                    step.Report = new CastingRunReport("run-4", "long", CastingApplyMode.Ordinary, "p",
                        "cancelled:" + CastingQualificationDriver.DisableReason, true, false,
                        new[]
                        {
                            new CastingOutcomeEntry("qual-cast-1", animated
                                    ? CastingOutcomeState.Cancelled : CastingOutcomeState.NotProcessed,
                                true, submitted, false, animated
                                    ? "Cancelled:cancelled-in-flight;last:FailedExecution:abandoned"
                                    : "stopped-before-start", true),
                            new CastingOutcomeEntry("qual-cast-2", CastingOutcomeState.Skipped, false, false, false,
                                "already-satisfied")
                        }, new string[0]);
                    step.Transitions.Add("qual-cast-1:unchanged");
                    step.Transitions.Add("qual-cast-2:unchanged");
                    step.Availability.Add("qual-cast-1:-1>-1");
                    judged.Steps.Add(step);
                    string failure = judged.StepFailure("disable");
                    bool expected = submitted == animated;
                    if (expected ? failure != null : failure == null || !failure.StartsWith("states:", StringComparison.Ordinal))
                        throw new InvalidOperationException("The disable rule judged " + mode + " submitted=" +
                            submitted + " as " + (failure ?? "pass"));
                }
        }

        // Live runs casting-qual-cast-20260923-a1-anim-01 and
        // casting-qual-select-20260923-d1-01: the game casts a cantrip at
        // will through the ability its class grants; a spellbook's level-0
        // entry needs a level-0 slot those books never have, and the cast
        // command refuses it. Execution resolves the at-will ability first,
        // validation uses the command's own guard, and discovery prices a
        // level-0 entry as free only with the at-will ability behind it.
        private static void TestCantripsCastAtWill()
        {
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("Repository root was not discoverable.");
            Func<string, string> source = name => File.ReadAllText(Path.Combine(directory.FullName, "src",
                "KingmakerBuffPlanner", "GameAdapters", name)).Replace("\r\n", "\n");
            string adapter = source("KingmakerAnimatedCastAdapter.cs");
            string atWill = source("KingmakerAtWillCantrips.cs");
            string builder = source("KingmakerPartySnapshotBuilder.cs");
            string resolve = SourceBlock(adapter, "private static AbilityData ResolveAbility(\n            UnitEntityData caster,\n            ProviderKey provider,\n            ResourceReservation reservation,");
            int atWillAt = resolve == null ? -1 : resolve.IndexOf(
                "KingmakerAtWillCantrips.Resolve(caster, provider.Ability,", StringComparison.Ordinal);
            int memorizedAt = resolve == null ? -1 : resolve.IndexOf("book.GetAllMemorizedSpells()", StringComparison.Ordinal);
            if (atWillAt < 0 || memorizedAt < atWillAt ||
                !resolve.Contains("CantripRoute route = AtWillCantripChoice.Route(provider.SourceInstanceId == AtWillSourceInstance,\n                    reservation == null ? (bool?)null : reservation.Unlimited);") ||
                !resolve.Contains("if (route == CantripRoute.Refused)\n                {\n                    refusal = AtWillCantripChoice.FreeReservationRefusal;\n                    return null;") ||
                !resolve.Contains("if (route == CantripRoute.AtWillOnly || route == CantripRoute.AtWillThenSlot)") ||
                !resolve.Contains("if (route == CantripRoute.AtWillOnly)\n                    {\n                        refusal = AtWillCantripChoice.MissingRefusal;\n                        return null;") ||
                !adapter.Contains("AbilityData ability = ResolveAbility(caster, step.Provider, step.Reservation,\n                out resolution, out refusal);") ||
                // Re-review: observation reads a consumed reserved slot (it is
                // still the source); execution needs it available.
                !adapter.Contains("return ResolveAbility(caster, step.Provider, step.Reservation, out resolution, out refusal, true);") ||
                !resolve.Contains("s.Spell != null && (forObservation || s.Available) && s.IsMainSlot &&") ||
                !resolve.Contains("atWillProvenance + \";pool=\" + (reservation == null ? \"unreserved\" : reservation.PoolKey);") ||
                !atWill.Contains("provenance = \"ability=\" + chosen.Identity + \"@cl\" + chosen.CasterLevel + \";at-will-candidates=\" +") ||
                !source("KingmakerProbeObserver.cs").Contains(
                    "AbilityData ability = KingmakerAnimatedCastAdapter.ResolveAbility(caster, step);") ||
                !resolve.Contains("if (refusal != null) return null;") ||
                !resolve.Contains("resolution = \"at-will-cantrip-ability;authored=spellbook:\" + provider.SpellbookGuid +") ||
                !adapter.Contains("internal const string AtWillSourceInstance = \"level-0|heighten-0\";"))
                throw new InvalidOperationException("A cantrip's level-0 entry is not cast through its at-will ability first.");
            // Review A3: a fact casting is bound to its kind and reserved
            // pool, priced by the same rule discovery uses.
            if (!resolve.Contains("string poolKey = KingmakerPartySnapshotBuilder.FactPoolKey(caster.UniqueId, selection, out kind);") ||
                !resolve.Contains("FactSourceCandidate chosen = FactSourceChoice.Choose(candidates.Select(pair => pair.Key),\n                    provider.Ability.SourceKind == SourceKind.AbilityResource,\n                    reservation == null ? null : reservation.PoolKey, out equivalents, out refusal);") ||
                !resolve.Contains("SafeSpellbookBound(fact.Data), kind == SourceKind.AbilityResource, poolKey,\n                            SafeCasterLevel(selection.Concrete))") ||
                !resolve.Contains(".OrderBy(value => value.Blueprint.AssetGuid, StringComparer.Ordinal))") ||
                !resolve.Contains("(blueprint.Parent == null || blueprint.Parent.AssetGuid != provider.Ability.BaseAbilityGuid))") ||
                !builder.Contains("existing.EffectiveCasterLevel != CasterLevel(data))") ||
                !builder.Contains("_ambiguousFactKeys.Add(keyForProvider);") ||
                !builder.Contains("string factPoolKey = FactPoolKey(unit.UniqueId, selection, out sourceKind);") ||
                Occurrences(builder, "string key = factPoolKey;") != 2)
                throw new InvalidOperationException("A fact casting is not bound to its kind and reserved pool.");
            // Review A7: an unread count is null, and the free check is the
            // Unity-free judgement in both paths.
            string instantSource = source("KingmakerInstantCastAdapter.cs");
            if (!adapter.Contains("internal static int? SafeAvailableCount(AbilityData ability)") ||
                !adapter.Contains("catch (Exception) { return null; }") ||
                !adapter.Contains("return AvailableCountJudgement.Violation(_step.Reservation, _availableBefore,") ||
                !instantSource.Contains("bool spent = AvailableCountJudgement.Spent(availableBefore, availableAfter);") ||
                !instantSource.Contains("string countViolation = AvailableCountJudgement.Violation(step.Reservation, availableBefore, availableAfter);") ||
                !instantSource.Contains("\";effects-observed-at-submit:\" + observed, countViolation);"))
                throw new InvalidOperationException("Unread cast counts can still pass as unlimited.");
            string validate = SourceBlock(adapter, "internal CastRuntimeValidation ValidateSource(CastStep step,");
            if (validate == null || validate.Contains("resolved.Ability.IsAvailableForCast") ||
                !validate.Contains("if (!resolved.Ability.IsAvailable) return CastRuntimeValidation.Fail(\"ability-unavailable\");"))
                throw new InvalidOperationException("Validation is not the cast command's own availability guard.");
            string resolveAtWill = SourceBlock(atWill, "internal static AbilityData Resolve(UnitEntityData caster, AbilityKey requested,\n            int preferredCasterLevel, out string refusal, out string provenance)");
            int matchGuardAt = resolveAtWill == null ? -1 : resolveAtWill.IndexOf("if (match == null) continue;",
                StringComparison.Ordinal);
            int judgedAt = resolveAtWill == null ? -1 : resolveAtWill.IndexOf(
                "SafeHasSpellbook(match), SafeAvailable(match), SafeCount(match), CasterLevel(match)",
                StringComparison.Ordinal);
            if (resolveAtWill == null || !resolveAtWill.Contains("AtWillCantripChoice.Choose(") ||
                matchGuardAt < 0 || judgedAt < matchGuardAt ||
                !atWill.Contains("try { return data.IsAvailable; }") ||
                !atWill.Contains("try { return data.GetAvailableForCastCount(); }"))
                throw new InvalidOperationException("An ability is taken as at will without the game's own judgement.");
            // Both execution paths validate with the command's own guard and
            // resolve through the same adapter, which records provenance.
            string instant = source("KingmakerInstantCastAdapter.cs");
            if (!instant.Contains(".ValidateSource(step, false);") ||
                Occurrences(instant, "KingmakerAnimatedCastAdapter.TryResolve(") < 2 ||
                !instant.Contains("\";resolution:\" + (resolved.Resolution ?? \"unrecorded\") +") ||
                !adapter.Contains("\";resolution:\" + _resolution +"))
                throw new InvalidOperationException("An execution path skips the shared guard or its provenance.");
            string spontaneous = SourceBlock(builder, "private void ScanSpontaneousSpellbook(");
            string zero = SourceBlock(builder, "private void ScanSpontaneousLevelZero(");
            string prepared = SourceBlock(builder, "private void ScanPreparedSpellbook(");
            if (spontaneous == null || spontaneous.Contains("ResourcePoolKind.Unlimited") ||
                !spontaneous.Contains("ScanSpontaneousLevelZero(unit, spellbook, providers, pools);") ||
                zero == null ||
                !zero.Contains("CantripPricing pricing = PriceCantrip(unit, spellbook, selection, out ambiguity);\n                if (pricing == CantripPricing.Unresolved)\n                {\n                    TraceUnresolvedCantrip(unit, spellbook, selection, ambiguity);\n                    continue;\n                }\n                if (pricing == CantripPricing.Free)") ||
                !zero.Contains("ResourcePoolKind.SpontaneousLevel") ||
                prepared == null ||
                !prepared.Contains("int firstUnresolved = pricings.IndexOf(CantripPricing.Unresolved);") ||
                !prepared.Contains(": \"group-of:\" + ambiguities[firstUnresolved]);\n                        foreach (SpellSlot slot in group) unresolvedSlots.Add(slot);\n                        continue;") ||
                !prepared.Contains("if (!pricings.All(pricing => pricing == CantripPricing.Free)) continue;") ||
                !prepared.Contains("var slots = allSlots.Where(s => !atWillSlots.Contains(s) && !unresolvedSlots.Contains(s)).ToList();") ||
                !builder.Contains("return AtWillCantripChoice.Price(atWill != null, ambiguity);"))
                throw new InvalidOperationException("A level-0 entry is priced as free without an at-will ability.");
        }

        // The classic cast core: a plan digest that sees every step field; a
        // single-use grant for exactly the approved routine, mode, plan and
        // budget; the run-bound allowance; and the Unity-free judgement of a
        // free classic cast (confirmed, effect applied, unlimited count
        // unchanged, provenance recorded, a spellbook cantrip cast at will,
        // no finite pool touched, no casting-first run).
        private static void TestClassicCastCore()
        {
            CastingWorkspaceInputs inputs = QualificationInputs(true, true, null);
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectZeroCostMixed(
                inputs, "fixture-campaign");
            IReadOnlyList<CastingQualificationStepForecast> forecast = CastingQualificationForecast.Forecast(
                selection, inputs, "fixture-campaign");
            CastPlan stop = forecast[0].Projection.Plan;
            CastPlan complete = forecast[1].Projection.Plan;
            string digest = ClassicPlanDigest.Of(stop);
            if (digest.Length != 64 || digest != ClassicPlanDigest.Of(stop) ||
                digest == ClassicPlanDigest.Of(complete) ||
                !ClassicPlanDigest.Canonical(stop).StartsWith("steps=1:3;", StringComparison.Ordinal))
                throw new InvalidOperationException("The classic plan digest is not exact: " +
                    ClassicPlanDigest.Canonical(stop));
            // Every field of every step, length-exact, in order.
            foreach (CastPlan plan in new[] { stop, complete })
            {
                List<KeyValuePair<string, string>> parsed = ParseClassicCanonical(ClassicPlanDigest.Canonical(plan));
                List<KeyValuePair<string, string>> expected = ExpectedClassicCanonical(plan);
                if (!parsed.SequenceEqual(expected))
                    throw new InvalidOperationException("The classic canonical plan misses or alters a field: " +
                        ClassicPlanDigest.Canonical(plan));
            }
            // Any change to what the step would do changes the digest, and
            // ids with delimiter characters stay unambiguous.
            CastStep template = stop.Steps[0];
            Func<IEnumerable<string>, ResourceReservation, MaterialReservation, IEnumerable<string>,
                IDictionary<string, int>, CastPlan> variant = (targets, reservation, material, enhancements, usage) =>
                new CastPlan(new[]
                {
                    new CastStep(template.SourceId, template.AssignmentId, template.Provider, template.AnchorUnitId,
                        targets ?? template.TargetUnitIds, template.ExpectedRecipientUnitIds,
                        reservation ?? template.Reservation, material ?? template.MaterialReservation,
                        template.ExpectedEffects, template.MassCast, template.ExecutionStrategy,
                        template.ExecutionStrategyReason, enhancements ?? template.EnhancementIds,
                        usage ?? template.EnhancementUsageByPool.ToDictionary(pair => pair.Key, pair => pair.Value),
                        template.OmittedEnhancementIds)
                }, new TargetPlanOutcome[0], new string[0]);
            string same = ClassicPlanDigest.Of(variant(null, null, null, null, null));
            var joined = new ResourceReservation("pool|a,b", 1, new[] { "tok|1,2;x=3:4" });
            var split = new ResourceReservation("pool|a,b", 1, new[] { "tok|1,2", "x=3:4" });
            string joinedDigest = ClassicPlanDigest.Of(variant(null, joined, null, null, null));
            List<string> tokens = ParseClassicCanonical(ClassicPlanDigest.Canonical(variant(null, joined, null, null, null)))
                .Where(pair => pair.Key == "tokens").Select(pair => pair.Value).ToList();
            if (same != ClassicPlanDigest.Of(new CastPlan(new[] { template }, new TargetPlanOutcome[0], new string[0])) ||
                ClassicPlanDigest.Of(variant(new[] { "unit-other" }, null, null, null, null)) == same ||
                joinedDigest == same || joinedDigest == ClassicPlanDigest.Of(variant(null, split, null, null, null)) ||
                tokens.Count != 1 || tokens[0] != "tok|1,2;x=3:4" ||
                ClassicPlanDigest.Of(variant(null, null, new MaterialReservation("item-guid", 1), null, null)) == same ||
                ClassicPlanDigest.Of(variant(null, null, null, new[] { "metamagic-extend" }, null)) == same ||
                ClassicPlanDigest.Of(variant(null, null, null, null, new Dictionary<string, int> { { "rod-pool", 1 } })) == same ||
                ClassicPlanDigest.Of(variant(null, null, null, null, new Dictionary<string, int> { { "rod-pool", 1 } })) ==
                    ClassicPlanDigest.Of(variant(null, null, null, null, new Dictionary<string, int> { { "rod-pool", 2 } })))
                throw new InvalidOperationException("A different classic step kept the approved digest.");
            var grant = new ClassicCastGrant("run-1", "long", digest, "animated", 3);
            string refusal;
            if (grant.TryConsume("short", digest, "animated", 3, out refusal) ||
                refusal != "classic-grant-routine:short" ||
                grant.TryConsume("long", digest, "instant", 3, out refusal) ||
                refusal != "classic-grant-mode:instant" ||
                grant.TryConsume("long", new string('0', 64), "animated", 3, out refusal) ||
                refusal != "classic-grant-plan-differs" ||
                grant.TryConsume("long", digest, "animated", 4, out refusal) ||
                refusal != "classic-grant-cap:4>3" ||
                grant.TryConsume("long", digest, "animated", 0, out refusal) ||
                refusal != "classic-grant-cap:0>3" ||
                !grant.TryConsume("long", digest, "animated", 3, out refusal) || refusal != null ||
                grant.TryConsume("long", digest, "animated", 3, out refusal) ||
                refusal != "classic-grant-consumed" || grant.Attempts != 7 || !grant.Consumed)
                throw new InvalidOperationException("The classic grant is not single-use and exact: " + refusal);
            // A grant whose scenario ended is disarmed, used or not.
            var unused = new ClassicCastGrant("run-2", "long", digest, "animated", 3);
            unused.Disarm();
            if (unused.TryConsume("long", digest, "animated", 3, out refusal) || refusal != "classic-grant-disarmed" ||
                unused.Consumed || !unused.Describe().Contains("disarmed=True"))
                throw new InvalidOperationException("A disarmed classic grant executed: " + refusal);
            Func<Action<JObject>, string> allowanceJson = mutate =>
            {
                var root = new JObject
                {
                    { "schemaVersion", 2 }, { "kind", "kbp-classic-cast" }, { "runId", "classic-run-1" },
                    { "sourceCommit", new string('c', 40) }, { "packageSha256", new string('a', 64) },
                    { "dllSha256", new string('b', 64) }, { "assemblyMvid", "11111111-2222-3333-4444-555555555555" },
                    { "fixtureGameId", "fixture-game" }, { "executionMode", "animated" }, { "routineId", "long" },
                    { "approvedPlanDigest", digest }, { "maximumNativeSubmissions", 3 },
                    { "approvedBy", "Howie" }, { "authority", "owner mission 2026-09-23 batch 3 section 6" },
                    { "compatibilityProfileId", "full-user" }, { "compatibilityIdentity", new string('f', 64) },
                    { "workingSaveSha256", new string('9', 64) }, { "purpose", "classic cast qualification fixture" }
                };
                if (mutate != null) mutate(root);
                return root.ToString();
            };
            ClassicCastAllowance allowance = ClassicCastAllowance.Parse(allowanceJson(null), "classic-run-1", out refusal);
            if (allowance == null || refusal != null || allowance.ApprovedPlanDigest != digest ||
                allowance.ExecutionMode != "animated" || allowance.MaximumNativeSubmissions != 3 ||
                allowance.CompatibilityProfileId != "full-user" || allowance.WorkingSaveSha256 != new string('9', 64) ||
                allowance.CompatibilityIdentity != new string('f', 64) ||
                allowance.Purpose != "classic cast qualification fixture")
                throw new InvalidOperationException("A valid classic allowance was refused: " + refusal);
            // The host's re-check: the request's profile and WORKING save.
            if (AllowanceFixtureBinding.RequestMismatch("full-user", new string('9', 64), "full-user",
                    new string('9', 64)) != null ||
                AllowanceFixtureBinding.RequestMismatch("full-user", new string('9', 64), "native-only",
                    new string('9', 64)) != "profile-mismatch" ||
                AllowanceFixtureBinding.RequestMismatch("full-user", new string('9', 64), "full-user",
                    new string('8', 64)) != "working-save-mismatch" ||
                AllowanceFixtureBinding.RequestMismatch("full-user", new string('9', 64), "full-user", null) !=
                    "working-save-mismatch")
                throw new InvalidOperationException("The host does not re-check the approved profile and save.");
            if (!AllowanceFixtureBinding.IsKnownProfile("advanced-gunslinger-0136") ||
                AllowanceFixtureBinding.IsKnownProfile("advanced-gunslinger-0133") ||
                AllowanceFixtureBinding.Refusal("advanced-gunslinger-0136", new string('f', 64),
                    new string('9', 64), "advanced qualification") != null)
                throw new InvalidOperationException("The advanced profile is not a known allowance profile.");
            var cases = new Dictionary<string, Action<JObject>>
            {
                { "allowance-unknown-member:extra", o => o["extra"] = 1 },
                { "allowance-missing-member:approvedPlanDigest", o => o.Remove("approvedPlanDigest") },
                { "allowance-schema", o => o["schemaVersion"] = 1 },
                { "allowance-missing-member:workingSaveSha256", o => o.Remove("workingSaveSha256") },
                { "allowance-profile", o => o["compatibilityProfileId"] = "Full-User" },
                { "allowance-compatibility-identity", o => o["compatibilityIdentity"] = new string('F', 64) },
                { "allowance-working-save", o => o["workingSaveSha256"] = "short" },
                { "allowance-purpose", o => o["purpose"] = new string('p', AllowanceFixtureBinding.MaximumPurposeLength + 1) },
                { "allowance-kind", o => o["kind"] = "kbp-casting-qualification" },
                { "allowance-submissions-range", o => o["maximumNativeSubmissions"] = 25 },
                { "allowance-artifact-identity", o => o["dllSha256"] = "short" },
                { "allowance-execution-mode", o => o["executionMode"] = "hybrid" },
                { "allowance-routine", o => o["routineId"] = "all" },
                { "allowance-plan-digest", o => o["approvedPlanDigest"] = "XYZ" },
                { "allowance-approval-missing", o => o["approvedBy"] = string.Empty }
            };
            foreach (KeyValuePair<string, Action<JObject>> item in cases)
                if (ClassicCastAllowance.Parse(allowanceJson(item.Value), "classic-run-1", out refusal) != null ||
                    refusal != item.Key)
                    throw new InvalidOperationException("Classic allowance case " + item.Key + " returned " + refusal);
            if (ClassicCastAllowance.Parse(allowanceJson(null), "other-run", out refusal) != null ||
                refusal != "allowance-run-mismatch")
                throw new InvalidOperationException("A classic allowance served another run.");
            if (ClassicCastAllowance.Parse(allowanceJson(o => o["assemblyMvid"] = "11111111-2222-3333-4444-55555555555A"),
                    "classic-run-1", out refusal) != null || refusal != "allowance-artifact-identity" ||
                ClassicCastAllowance.Parse(allowanceJson(o => o["fixtureGameId"] = string.Empty),
                    "classic-run-1", out refusal) != null || refusal != "allowance-fixture-missing" ||
                ClassicCastAllowance.Parse(allowanceJson(o => o["maximumNativeSubmissions"] = "3"),
                    "classic-run-1", out refusal) != null || refusal != "allowance-submissions" ||
                ClassicCastAllowance.Parse("not json", "classic-run-1", out refusal) != null ||
                refusal != "allowance-unreadable")
                throw new InvalidOperationException("A malformed classic allowance was accepted: " + refusal);
            Func<ClassicCastRecord> good = () =>
            {
                var record = new ClassicCastRecord
                {
                    CastingScenario = true, AllowanceStatus = "valid", ExecutionMode = "animated",
                    PlanDigest = digest, PlanSteps = 1, Grant = "consumed", GrantConsumed = true,
                    GrantAttempts = 1, QuickDisposition = "Completed", Planned = 1, Queued = 1, CastStarted = 1,
                    Submitted = 0, Confirmed = 1, Failed = 0, CastingFirstRuns = 0
                };
                record.Steps.Add(new ClassicCastStepResult(0, "provider", true, "unit-t1")
                {
                    FinalStatus = "EffectConfirmed", Transition = "new-instance", AvailableBefore = -1,
                    AvailableAfter = -1, Detail = "expected-effects-observed;resolution:at-will-cantrip-ability;authored=spellbook:b/level-0"
                });
                record.FinitePools.Add("unit|book|spontaneous-1:2>2");
                return record;
            };
            // The executor's own record of issuing: animated queues and starts
            // a command (as observed live in classic-cast-20260924-00aca73-anim-01:
            // queued=1;started=1;submitted=0), instant submits the rule.
            Func<ClassicCastRecord> instant = () =>
            {
                ClassicCastRecord record = good();
                record.ExecutionMode = "instant";
                record.Queued = 0;
                record.CastStarted = 0;
                record.Submitted = 1;
                // The instant executor records Submitted, then CastStarted.
                record.CastStarted = 1;
                return record;
            };
            if (good().Violations().Count != 0 || instant().Violations().Count != 0)
                throw new InvalidOperationException("A clean classic cast was refused: " +
                    string.Join("|", good().Violations().ToArray()) + " / " +
                    string.Join("|", instant().Violations().ToArray()));
            var instantShapes = new Dictionary<string, Action<ClassicCastRecord>>
            {
                { "report:planned=1;queued=0;started=1;submitted=0;confirmed=1;failed=0;steps=1;mode=instant",
                    r => r.Submitted = 0 },
                { "report:planned=1;queued=1;started=1;submitted=0;confirmed=1;failed=0;steps=1;mode=instant",
                    r => { r.Submitted = 0; r.Queued = 1; r.CastStarted = 1; } }
            };
            foreach (KeyValuePair<string, Action<ClassicCastRecord>> shape in instantShapes)
            {
                ClassicCastRecord bad = instant();
                shape.Value(bad);
                if (!bad.Violations().Contains(shape.Key))
                    throw new InvalidOperationException("Classic instant judgement missed " + shape.Key + ": " +
                        string.Join("|", bad.Violations().ToArray()));
            }
            var shapes = new Dictionary<string, Action<ClassicCastRecord>>
            {
                { "step0:status:TimedOutUnconfirmed", r => r.Steps[0].FinalStatus = "TimedOutUnconfirmed" },
                { "step0:effect:unchanged", r => r.Steps[0].Transition = "unchanged" },
                { "step0:availability:-1>0", r => r.Steps[0].AvailableAfter = 0 },
                { "step0:availability:0>0", r => { r.Steps[0].AvailableBefore = 0; r.Steps[0].AvailableAfter = 0; } },
                { "step0:availability-unread", r => r.Steps[0].AvailableBefore = null },
                { "step0:resolution-unrecorded", r => r.Steps[0].Detail = "expected-effects-observed" },
                { "step0:cantrip-not-at-will", r => r.Steps[0].Detail = "ok;resolution:known-spell:spellbook:b" },
                { "grant:consumed", r => r.GrantAttempts = 2 },
                { "disposition:Refused", r => r.QuickDisposition = "Refused" },
                { "report:planned=1;queued=1;started=1;submitted=0;confirmed=0;failed=1;steps=1;mode=animated",
                    r => { r.Confirmed = 0; r.Failed = 1; } },
                { "finite-pool:unit|book|spontaneous-1:2>1", r => r.FinitePools[0] = "unit|book|spontaneous-1:2>1" },
                { "casting-first-runs:1", r => r.CastingFirstRuns = 1 },
                { "allowance:execution-mode-mismatch", r => r.AllowanceStatus = "execution-mode-mismatch" },
                { "steps-observed:0", r => r.Steps.Clear() },
                { "report:planned=2;queued=1;started=1;submitted=0;confirmed=1;failed=0;steps=1;mode=animated",
                    r => r.Planned = 2 },
                { "report:planned=1;queued=1;started=0;submitted=0;confirmed=1;failed=0;steps=1;mode=animated",
                    r => r.CastStarted = 0 },
                { "report:planned=1;queued=0;started=1;submitted=0;confirmed=1;failed=0;steps=1;mode=animated",
                    r => r.Queued = 0 },
                { "report:planned=1;queued=0;started=0;submitted=1;confirmed=1;failed=0;steps=1;mode=animated",
                    r => { r.Queued = 0; r.CastStarted = 0; r.Submitted = 1; } },
                { "report:planned=1;queued=1;started=1;submitted=0;confirmed=1;failed=1;steps=1;mode=animated",
                    r => r.Failed = 1 },
                { "mode:hybrid", r => r.ExecutionMode = "hybrid" },
                { "grant-still-armed", r => r.GrantArmedAtEnd = true },
                { "finite-pool:unit|book|spontaneous-1:2", r => r.FinitePools[0] = "unit|book|spontaneous-1:2" }
            };
            foreach (KeyValuePair<string, Action<ClassicCastRecord>> shape in shapes)
            {
                ClassicCastRecord bad = good();
                shape.Value(bad);
                if (!bad.Violations().Contains(shape.Key))
                    throw new InvalidOperationException("Classic judgement missed " + shape.Key + ": " +
                        string.Join("|", bad.Violations().ToArray()));
            }
            var select = new ClassicCastRecord { CastingScenario = false, PlanDigest = digest, PlanSteps = 1 };
            if (select.Violations().Count != 0)
                throw new InvalidOperationException("A clean classic selection was refused.");
            select.GrantAttempts = 1;
            if (!select.Violations().Contains("select-grant-used"))
                throw new InvalidOperationException("A selection run used the classic grant.");
            // An executor that wrote no provenance says so explicitly; that
            // is never accepted as a recorded resolution.
            ClassicCastRecord unrecorded = good();
            unrecorded.Steps[0].Detail = "ok;resolution:unrecorded";
            if (!unrecorded.Violations().Contains("step0:resolution-unrecorded"))
                throw new InvalidOperationException("An unrecorded resolution was accepted.");
            var seen = new ClassicCastRecord { CastingScenario = false, PlanDigest = digest, PlanSteps = 1,
                ClassicRunSeen = true };
            var armed = new ClassicCastRecord { CastingScenario = false, PlanDigest = digest, PlanSteps = 1,
                GrantArmedAtEnd = true };
            if (!seen.Violations().Contains("select-classic-run") || !armed.Violations().Contains("grant-still-armed"))
                throw new InvalidOperationException("A selection run's dispatch readings were not judged.");
        }

        // Batch 3, section 6 (reviews A5, B2, C2): a Classic routine stops
        // after the first cast that did not confirm, records the rest as
        // halted, keeps every record the executor wrote (cleanup included),
        // and a run stopped by its owner disposes the step in progress.
        private static void TestClassicHaltingRunner()
        {
            CastingWorkspaceInputs inputs = QualificationInputs(true, true, null);
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectZeroCostMixed(
                inputs, "fixture-campaign");
            CastPlan plan = CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign")[0]
                .Projection.Plan;
            if (plan.Steps.Count != 3) throw new InvalidOperationException("The runner fixture needs three steps.");
            Func<int, ScriptedExecutor> failing = failAt => new ScriptedExecutor((single, stepReport) =>
            {
                int position = plan.Steps.ToList().IndexOf(single.Steps[0]);
                return new ScriptedIterator(2, false, false, () => stepReport.Add(0, single.Steps[0],
                    position == failAt ? CastExecutionStatus.FailedExecution : CastExecutionStatus.EffectConfirmed,
                    position == failAt ? "fixture-failed" : "fixture-confirmed"));
            });
            // A failure at step 1 (the second cast): step 2 never starts.
            ScriptedExecutor second = failing(1);
            var runner = new HaltingPlanRunner(second);
            var report = new ExecutionReport(plan);
            System.Collections.IEnumerator run = runner.Run(plan, report);
            int guard = 0;
            while (run.MoveNext() && guard++ < 1000) { }
            if (second.Executed.Count != 2 || runner.HaltedAfterStep != 1 || report.Confirmed != 1 ||
                !report.Records.Any(record => record.StepIndex == 2 &&
                    record.Detail == HaltingPlanRunner.HaltedDetailPrefix + "1") ||
                !report.Records.Any(record => record.StepIndex == 1 && record.Detail == "fixture-failed"))
                throw new InvalidOperationException("A Classic run continued after a failed cast: executed=" +
                    second.Executed.Count + ";halted=" + runner.HaltedAfterStep);
            // A confirmed step that also recorded a failure halts too.
            var mixed = new ScriptedExecutor((single, report2) => new ScriptedIterator(1, false, false, () =>
            {
                report2.Add(0, single.Steps[0], CastExecutionStatus.EffectConfirmed, "fixture-confirmed");
                report2.Add(0, single.Steps[0], CastExecutionStatus.FailedExecution,
                    "unexpected-resource-spent-on-unlimited-source");
            }));
            var mixedRunner = new HaltingPlanRunner(mixed);
            System.Collections.IEnumerator mixedRun = mixedRunner.Run(plan, new ExecutionReport(plan));
            guard = 0;
            while (mixedRun.MoveNext() && guard++ < 1000) { }
            if (mixed.Executed.Count != 1 || mixedRunner.HaltedAfterStep != 0)
                throw new InvalidOperationException("A confirmed step with a failure record did not halt the run.");
            // All confirmed: every step runs, nothing halted.
            ScriptedExecutor clean = failing(-1);
            var cleanRunner = new HaltingPlanRunner(clean);
            var cleanReport = new ExecutionReport(plan);
            System.Collections.IEnumerator cleanRun = cleanRunner.Run(plan, cleanReport);
            guard = 0;
            while (cleanRun.MoveNext() && guard++ < 1000) { }
            if (clean.Executed.Count != 3 || cleanRunner.HaltedAfterStep != null || cleanReport.Confirmed != 3)
                throw new InvalidOperationException("A clean Classic run did not cast every step.");
            // Stopped by its owner mid-step: disposing the run disposes the
            // step in progress exactly once, and nothing later starts.
            ScriptedIterator inProgress = null;
            var hanging = new ScriptedExecutor((single, report3) =>
                inProgress = new ScriptedIterator(int.MaxValue, false, false, null));
            var hangingRunner = new HaltingPlanRunner(hanging);
            System.Collections.IEnumerator hangingRun = hangingRunner.Run(plan, new ExecutionReport(plan));
            hangingRun.MoveNext();
            hangingRun.MoveNext();
            ((IDisposable)hangingRun).Dispose();
            if (inProgress == null || inProgress.Disposed != 1 || hanging.Executed.Count != 1)
                throw new InvalidOperationException("A stopped Classic run abandoned the cast in progress.");
            // The production wiring: the Classic session runs through the
            // runner, and the root ends a Classic run on disable, area change
            // and teardown, disposing the session's iterator.
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("Repository root was not discoverable.");
            string ui = Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "UI");
            string session = File.ReadAllText(Path.Combine(ui, "PlannerUiSession.cs")).Replace("\r\n", "\n");
            string root = File.ReadAllText(Path.Combine(ui, "BuffPlannerUiRoot.cs")).Replace("\r\n", "\n");
            string enable = SourceBlock(root, "internal static void SetEnabled(bool enabled)");
            string unload = SourceBlock(root, "public void OnAreaBeginUnloading()");
            string teardown = SourceBlock(root, "private void ReleaseAll()");
            string quick = SourceBlock(root, "private IEnumerator ExecuteQuickRoutine(");
            if (!session.Contains("var runner = new HaltingPlanRunner(executor);") ||
                !session.Contains("IEnumerator work = new WorldGatedEnumerator(runner.Run(preview.Plan, LastExecutionReport),") ||
                session.Contains("executor.Execute(preview.Plan, LastExecutionReport)") ||
                enable == null || !enable.Contains("_instance.EndClassicRun(\"mod-disabled\");") ||
                unload == null || !unload.Contains("EndClassicRun(\"area-unloading\");") ||
                teardown == null || teardown.IndexOf("EndClassicRun(\"root-teardown\");", StringComparison.Ordinal) < 0 ||
                teardown.IndexOf("EndClassicRun(\"root-teardown\");", StringComparison.Ordinal) >
                    teardown.IndexOf("StopAllCoroutines();", StringComparison.Ordinal) ||
                quick == null || !quick.Contains("IDisposable inner = routine as IDisposable;") ||
                Occurrences(root, "StartCoroutine(ExecuteQuickRoutine(") != 0)
                throw new InvalidOperationException("The Classic run is not owned: halting, disable, area change or teardown.");
        }

        // Batch 3, section 11: disk round trips of what the other tests leave
        // out (one caster casting the same spell from two spellbooks, a
        // variant child, metamagic, a non-default existing-effect policy),
        // two campaigns in ONE settings directory never touching each
        // other, and a copied file refused for the other campaign and never
        // overwritten.
        private static void TestPersistenceRoundTripAndCampaignIsolation(string root)
        {
            string dir = Path.Combine(root, "persistence-gaps");
            Directory.CreateDirectory(dir);
            var variant = new AbilityKey(CastingBuffAbility.BaseAbilityGuid, "variant-child-guid", 0,
                CastingBuffAbility.SourceKind, null);
            var empowered = new AbilityKey(CastingBuffAbility.BaseAbilityGuid, string.Empty, 2,
                CastingBuffAbility.SourceKind, null);
            CastingPlanDocument a = CampaignDocument("campaign-a",
                DirectCasting("book-a", "long", "unit-sorc", "unit-t1", "source-bulls", CastingBuffAbility,
                    null, "spellbook-a"),
                DirectCasting("book-b", "long", "unit-sorc", "unit-t2", "source-bulls", CastingBuffAbility,
                    null, "spellbook-b"),
                DirectCasting("child", "long", "unit-cleric", "unit-t1", "source-bulls", variant, null, "spellbook-c"),
                DirectCasting("meta", "short", "unit-cleric", "unit-t2", "source-bulls", empowered, null, "spellbook-c"),
                DirectCasting("always", "short", "unit-wizard", "unit-t3", "source-bulls", CastingBuffAbility,
                    null, "spellbook-w", CastingAuthoringState.Ready, ExistingEffectPolicy.Overwrite));
            var repository = new CastingPlanRepository(dir);
            repository.Save(CastingPlanProfile.FromDocument(a));
            CastingPlanLoadResult loadedA = repository.Load("campaign-a");
            if (loadedA.Status != CastingPlanLoadStatus.Loaded)
                throw new InvalidOperationException("The gap document did not load: " + loadedA.Warning);
            Dictionary<string, PlannedCasting> byId = loadedA.Profile.ToDocument().Castings
                .ToDictionary(casting => casting.CastingId, StringComparer.Ordinal);
            if (byId.Count != 5 ||
                byId["book-a"].SpellbookGuid != "spellbook-a" || byId["book-b"].SpellbookGuid != "spellbook-b" ||
                byId["book-a"].Ability.Canonical != byId["book-b"].Ability.Canonical ||
                byId["book-a"].DirectTargetUnitId != "unit-t1" || byId["book-b"].DirectTargetUnitId != "unit-t2" ||
                byId["child"].Ability.VariantGuid != "variant-child-guid" ||
                byId["child"].Ability.Canonical != variant.Canonical ||
                byId["meta"].Ability.MetamagicMask != 2 || byId["meta"].Ability.Canonical != empowered.Canonical ||
                byId["always"].ExistingEffectPolicy != ExistingEffectPolicy.Overwrite ||
                byId["book-a"].ExistingEffectPolicy != ExistingEffectPolicy.SkipAlreadyActive)
                throw new InvalidOperationException("A spellbook, variant, metamagic or policy drifted on disk.");
            string pathA = repository.GetProfilePath("campaign-a");
            byte[] bytesA = File.ReadAllBytes(pathA);
            repository.Save(CastingPlanProfile.FromDocument(loadedA.Profile.ToDocument()));
            if (!File.ReadAllBytes(pathA).SequenceEqual(bytesA))
                throw new InvalidOperationException("The gap document is not byte-stable across a round trip.");
            // Campaign B in the same settings directory.
            CastingPlanDocument b = CampaignDocument("campaign-b",
                DirectCasting("b-only", "long", "unit-wizard", "unit-t1", "source-bulls", CastingBuffAbility));
            repository.Save(CastingPlanProfile.FromDocument(b));
            string pathB = repository.GetProfilePath("campaign-b");
            if (string.Equals(pathA, pathB, StringComparison.OrdinalIgnoreCase) ||
                Path.GetDirectoryName(pathA) != Path.GetDirectoryName(pathB) ||
                !File.ReadAllBytes(pathA).SequenceEqual(bytesA) ||
                repository.Load("campaign-a").Profile.ToDocument().Castings.Count != 5 ||
                repository.Load("campaign-b").Profile.ToDocument().Castings.Single().CastingId != "b-only")
                throw new InvalidOperationException("Two campaigns in one directory touched each other.");
            // A's file copied over B's name is refused for B and never
            // overwritten by B's save.
            File.Copy(pathA, pathB, true);
            byte[] copied = File.ReadAllBytes(pathB);
            CastingPlanLoadResult mismatch = repository.Load("campaign-b");
            bool refusedSave = false;
            try { repository.Save(CastingPlanProfile.FromDocument(b)); }
            catch (InvalidDataException exception)
            {
                refusedSave = exception.Message == "refusing-to-overwrite-another-campaigns-primary";
            }
            // Review B1: a primary that parses but that this repository cannot
            // load (an unknown member here) is never replaced either.
            string invalidPath = repository.GetProfilePath("campaign-c");
            repository.Save(CastingPlanProfile.FromDocument(CampaignDocument("campaign-c",
                DirectCasting("c-only", "long", "unit-wizard", "unit-t1", "source-bulls", CastingBuffAbility))));
            JObject invalidRoot = JObject.Parse(File.ReadAllText(invalidPath));
            invalidRoot["memberFromTheFuture"] = 1;
            File.WriteAllText(invalidPath, invalidRoot.ToString());
            byte[] invalidBytes = File.ReadAllBytes(invalidPath);
            string invalidRefusal = null;
            try { repository.Save(CastingPlanProfile.FromDocument(CampaignDocument("campaign-c"))); }
            catch (InvalidDataException exception) { invalidRefusal = exception.Message; }
            if (invalidRefusal == null ||
                !invalidRefusal.StartsWith("refusing-to-overwrite-invalid-primary:", StringComparison.Ordinal) ||
                !File.ReadAllBytes(invalidPath).SequenceEqual(invalidBytes) ||
                PersistenceMessages.ForSaveFailure(new InvalidDataException(invalidRefusal)).IndexOf(
                    "left unchanged", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("An invalid primary was replaced: " + invalidRefusal);
            if (mismatch.Status == CastingPlanLoadStatus.Loaded || mismatch.Profile != null && mismatch.Profile
                    .ToDocument().Castings.Any(casting => casting.CastingId == "book-a") ||
                !refusedSave || !File.ReadAllBytes(pathB).SequenceEqual(copied) ||
                !File.ReadAllBytes(pathA).SequenceEqual(bytesA))
                throw new InvalidOperationException("A copied campaign file was honoured or overwritten: " +
                    mismatch.Status + " " + mismatch.Warning + " refusedSave=" + refusedSave);
        }

        private static CastingPlanDocument CampaignDocument(string campaignId, params PlannedCasting[] castings)
        {
            CastingPlanDocument fixture = CastingDocument(castings);
            return new CastingPlanDocument(campaignId, fixture.Routines, fixture.Castings);
        }

        // Batch 3, section 10: the physical-input workspace scenario (a
        // workspace scenario with physical input, automation fixture only,
        // an optional expected screen size) and its Unity-free judgement.
        private static void TestPhysicalWorkspace(string root)
        {
            if (!RuntimeTestProtocol.IsPhysicalWorkspaceScenario("live-workspace-physical") ||
                !RuntimeTestProtocol.IsWorkspaceScenario("live-workspace-physical") ||
                RuntimeTestProtocol.IsNoInputWorkspaceScenario("live-workspace-physical") ||
                RuntimeTestProtocol.IsAdvancedFamilyScenario("live-workspace-physical") ||
                !RuntimeTestProtocol.IsScreenSize("1920x1080") || !RuntimeTestProtocol.IsScreenSize("2560x1440") ||
                RuntimeTestProtocol.IsScreenSize("1920X1080") || RuntimeTestProtocol.IsScreenSize("100x100") ||
                RuntimeTestProtocol.IsScreenSize("1920x") || RuntimeTestProtocol.IsScreenSize("-1920x1080"))
                throw new InvalidOperationException("Physical workspace classification is wrong.");
            Func<string, string, string, Action<Dictionary<string, object>>> set = (scenario, family, screen) => o =>
            {
                o["scenario"] = scenario;
                var parameters = new Dictionary<string, object>
                {
                    { "workingSaveName", family + "_WORKING" },
                    { "workingFileName", "Manual_305_" + family + "_WORKING.zks" },
                    { "workingSha256", new string('a', 64) },
                    { "baselineSaveName", family + "_BASELINE" },
                    { "baselineFileName", "Manual_304_" + family + "_BASELINE.zks" },
                    { "baselineSha256", new string('b', 64) },
                    { "expectedGameName", "Hedwirg" },
                    { "expectedGameId", "df33d1ff-4ec8-4707-bfa0-5e059bf9a049" },
                    { "executionMode", "instant" }
                };
                if (screen != null) parameters["expectedScreen"] = screen;
                o["parameters"] = parameters;
            };
            string rejection;
            foreach (Action<Dictionary<string, object>> ok in new[]
                {
                    set("live-workspace-physical", "KBP_AUTOMATION", null),
                    set("live-workspace-physical", "KBP_AUTOMATION", "1920x1080"),
                    // The layout at another resolution, without physical input.
                    set("live-workspace-qual", "KBP_AUTOMATION", "1920x1080")
                })
            {
                string path = WriteRequest(root, "physical-ok-" + Guid.NewGuid().ToString("N"), ok);
                if (ReadProtocol(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path },
                        out rejection) == null || rejection.Length != 0)
                    throw new InvalidOperationException("A valid physical request was refused: " + rejection);
            }
            foreach (Action<Dictionary<string, object>> bad in new[]
                {
                    set("live-workspace-physical", "KBP_ADVANCED", null),
                    set("live-workspace-physical", "KBP_AUTOMATION", "wide"),
                    set("live-workspace-qual", "KBP_AUTOMATION", "wide"),
                    set("live-workspace-import", "KBP_AUTOMATION", "1920x1080"),
                    set("live-classic-select", "KBP_AUTOMATION", "1920x1080")
                })
            {
                string path = WriteRequest(root, "physical-bad-" + Guid.NewGuid().ToString("N"), bad);
                if (ReadProtocol(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path },
                        out rejection) != null || string.IsNullOrEmpty(rejection))
                    throw new InvalidOperationException("An invalid physical request was accepted.");
            }
            // The query comes from a tile label: four letters of its first
            // word, then the fifth after the focus loss (review B5).
            string derivedQuery;
            string derivedSuffix;
            if (!PhysicalWorkspaceRecord.DeriveQuery("Resistance", out derivedQuery, out derivedSuffix) ||
                derivedQuery != "resi" || derivedSuffix != "s" ||
                PhysicalWorkspaceRecord.DeriveQuery("Aid Another — AC Bonus", out derivedQuery, out derivedSuffix) ||
                PhysicalWorkspaceRecord.DeriveQuery("Bull's Strength", out derivedQuery, out derivedSuffix) ||
                !PhysicalWorkspaceRecord.DeriveQuery("Light", out derivedQuery, out derivedSuffix) ||
                derivedQuery != "ligh" || derivedSuffix != "t")
                throw new InvalidOperationException("The physical query is not derived from a tile's first word.");
            Func<PhysicalWorkspaceRecord> good = () =>
            {
                var record = new PhysicalWorkspaceRecord
                {
                    Query = "resi", QuerySuffix = "s", TargetSource = "source-resistance",
                    ExpectedScreen = "1920x1080", ScreenWidth = 1920, ScreenHeight = 1080, OpenedPhysically = true,
                    SearchFocused = true, SearchText = "resi", ModeAfterTyping = "planner", GridOverflows = true,
                    ScrollBefore = 1f, ScrollAfter = 0.4f, SelectedBeforeClick = "source-aid",
                    SelectedAfterClick = "source-resistance",
                    FocusCycle = "minimized=True;lostForeground=True;restored=True;foreground=True",
                    FocusRegained = true, WorkspaceOpenAfterFocus = true, SearchTextAfterFocus = "resis",
                    ClosedByEscape = true, LeaseReleased = true, ModeAfterClose = "Default",
                    SelectionUnchanged = true, CameraUnchanged = true
                };
                record.Acknowledged.AddRange(PhysicalWorkspaceRecord.Actions);
                record.VisibleAfterQuery.Add("source-resistance|Resistance|False");
                record.VisibleAfterQuery.Add("source-aid|Aid Another|True");
                return record;
            };
            if (good().Violations().Count != 0 || good().WheelEvidence != "scrolled")
                throw new InvalidOperationException("A clean physical run was refused: " +
                    string.Join("|", good().Violations().ToArray()));
            var shapes = new Dictionary<string, Action<PhysicalWorkspaceRecord>>
            {
                { "screen:1920x1200!=1920x1080", r => r.ScreenHeight = 1200 },
                { "workspace-opened-programmatically", r => r.OpenedPhysically = false },
                { "unacknowledged:ws-focus-cycle", r => r.Acknowledged.Remove("ws-focus-cycle") },
                { "query-not-derived", r => r.Query = null },
                { "search-not-focused", r => r.SearchFocused = false },
                { "typed:res", r => r.SearchText = "res" },
                { "unfiltered:source-light|Light|False", r => r.VisibleAfterQuery.Add("source-light|Light|False") },
                { "target-not-shown:source-resistance", r => r.VisibleAfterQuery.RemoveAt(0) },
                { "typing-changed-mode:Inventory", r => r.ModeAfterTyping = "Inventory" },
                { "wheel:no-scroll:1>1", r => r.ScrollAfter = 1f },
                { "wheel:moved-without-overflow:1>0.4", r => r.GridOverflows = false },
                { "scroll-unread", r => r.ScrollAfter = null },
                { "target-already-selected:source-resistance", r => r.SelectedBeforeClick = "source-resistance" },
                { "tile-not-selected:source-resistance>source-aid", r => r.SelectedAfterClick = "source-aid" },
                { "focus-cycle:minimized=False", r => r.FocusCycle = "minimized=False" },
                { "focus-not-regained", r => r.FocusRegained = false },
                { "workspace-lost-on-focus", r => r.WorkspaceOpenAfterFocus = false },
                { "typing-after-focus:resi", r => r.SearchTextAfterFocus = "resi" },
                { "escape-did-not-close", r => r.ClosedByEscape = false },
                { "lease-held-after-close", r => r.LeaseReleased = false },
                { "mode-after-close:EscMode", r => r.ModeAfterClose = "EscMode" },
                { "world-input-leaked:commands=1/0/0;events=0/0;selectionUnchanged=True;cameraUnchanged=True",
                    r => r.PlayerCommands = 1 },
                { "world-input-leaked:commands=0/0/0;events=1/0;selectionUnchanged=True;cameraUnchanged=True",
                    r => r.SelectionEvents = 1 },
                { "world-input-leaked:commands=0/0/0;events=0/1;selectionUnchanged=True;cameraUnchanged=True",
                    r => r.AbilityTargetEvents = 1 },
                { "world-input-leaked:commands=0/0/0;events=0/0;selectionUnchanged=True;cameraUnchanged=False",
                    r => r.CameraUnchanged = false }
            };
            foreach (KeyValuePair<string, Action<PhysicalWorkspaceRecord>> shape in shapes)
            {
                PhysicalWorkspaceRecord bad = good();
                shape.Value(bad);
                if (!bad.Violations().Contains(shape.Key))
                    throw new InvalidOperationException("Physical judgement missed " + shape.Key + ": " +
                        string.Join("|", bad.Violations().ToArray()));
            }
            // A grid whose content fits cannot scroll: the wheel is labeled
            // not applicable (never claimed as scrolling), and passes.
            PhysicalWorkspaceRecord fits = good();
            fits.GridOverflows = false;
            fits.ScrollAfter = 1f;
            if (fits.Violations().Count != 0 || fits.WheelEvidence != "not-applicable:no-overflow")
                throw new InvalidOperationException("A non-overflowing grid was judged as a scroll claim.");
            PhysicalWorkspaceRecord owner = good();
            owner.ExpectedScreen = null;
            owner.ScreenHeight = 1200;
            if (owner.Violations().Count != 0)
                throw new InvalidOperationException("The owner's own display was judged against a size.");
        }

        // The area and cantrip diagnostics only read: no transition is used,
        // evaluated or loaded, nothing is saved, cast, spent or moved.
        private static void TestReadOnlyGameDiagnostics()
        {
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("Repository root was not discoverable.");
            string adapters = Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "GameAdapters");
            string[] forbidden =
            {
                "LoadArea(", "ExecuteTransition", "CheckRestrictions(", "Teleport", "SaveManager", "MakeAutoSave",
                "AreaTransitionGroupCommand", "UnitAreaTransition", ".Run(", ".Spend(", "Commands.Run",
                "CurrentValue =", "Rulebook", ".Position =", "Interact("
            };
            foreach (string name in new[] { "KingmakerAreaDiagnostics.cs", "KingmakerCantripDiagnostics.cs" })
            {
                // Code only: comments may name what the code never calls.
                string text = string.Join("\n", File.ReadAllText(Path.Combine(adapters, name)).Split('\n')
                    .Select(line => line.IndexOf("//", StringComparison.Ordinal) < 0 ? line
                        : line.Substring(0, line.IndexOf("//", StringComparison.Ordinal))).ToArray());
                string found = forbidden.FirstOrDefault(value => text.Contains(value));
                if (found != null)
                    throw new InvalidOperationException(name + " acts on the game: " + found);
            }
            // Batch 3, section 12: builds are worktree-independent (the
            // compiler maps the project directory in every embedded path).
            string project = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner",
                "KingmakerBuffPlanner.csproj"));
            if (!project.Contains("<Deterministic>true</Deterministic>") ||
                !project.Contains("<PathMap>$(MSBuildProjectDirectory)=/_/src/KingmakerBuffPlanner</PathMap>"))
                throw new InvalidOperationException("The build embeds the checkout path.");
            string area = File.ReadAllText(Path.Combine(adapters, "KingmakerAreaDiagnostics.cs"));
            if (!area.Contains("FindObjectsOfType<AreaTransition>()") || !area.Contains("AutosaveEnabled.CurrentValue") ||
                !area.Contains("transition.AutoSaveMode"))
                throw new InvalidOperationException("The area diagnostics do not record transitions and autosave.");
        }

        // The length-prefixed classic canonical form read back field by field.
        private static List<KeyValuePair<string, string>> ParseClassicCanonical(string canonical)
        {
            var fields = new List<KeyValuePair<string, string>>();
            int at = 0;
            while (at < canonical.Length)
            {
                int equals = canonical.IndexOf('=', at);
                int colon = canonical.IndexOf(':', equals);
                int length = int.Parse(canonical.Substring(equals + 1, colon - equals - 1));
                if (colon + 1 + length >= canonical.Length || canonical[colon + 1 + length] != ';')
                    throw new InvalidOperationException("A classic canonical field is not length-exact at " + at + ".");
                fields.Add(new KeyValuePair<string, string>(canonical.Substring(at, equals - at),
                    canonical.Substring(colon + 1, length)));
                at = colon + 2 + length;
            }
            return fields;
        }

        // The specified classic canonical fields of a plan, in order.
        private static List<KeyValuePair<string, string>> ExpectedClassicCanonical(CastPlan plan)
        {
            var fields = new List<KeyValuePair<string, string>>();
            Action<string, string> add = (name, value) =>
                fields.Add(new KeyValuePair<string, string>(name, value ?? string.Empty));
            Action<string, IEnumerable<string>> addList = (name, values) =>
            {
                List<string> items = (values ?? new string[0]).ToList();
                add(name + "#", items.Count.ToString());
                foreach (string item in items) add(name, item);
            };
            add("steps", plan.Steps.Count.ToString());
            for (int index = 0; index < plan.Steps.Count; index++)
            {
                CastStep step = plan.Steps[index];
                add("index", index.ToString());
                add("provider", step.Provider.Canonical);
                add("source", step.SourceId);
                add("assignment", step.AssignmentId);
                add("anchor", step.AnchorUnitId);
                addList("targets", step.TargetUnitIds);
                addList("recipients", step.ExpectedRecipientUnitIds);
                add("mass", step.MassCast ? "1" : "0");
                add("pool", step.Reservation == null ? "none" : step.Reservation.PoolKey);
                add("units", step.Reservation == null ? "0" : step.Reservation.Units.ToString());
                add("unlimited", step.Reservation != null && step.Reservation.Unlimited ? "1" : "0");
                addList("tokens", step.Reservation == null ? null : step.Reservation.TokenIds);
                add("material", step.MaterialReservation == null ? "none" : step.MaterialReservation.ItemGuid);
                add("material-count", step.MaterialReservation == null ? "0" : step.MaterialReservation.Count.ToString());
                addList("enhancements", step.EnhancementIds);
                addList("omitted", step.OmittedEnhancementIds);
                add("usage#", step.EnhancementUsageByPool.Count.ToString());
                foreach (KeyValuePair<string, int> pair in step.EnhancementUsageByPool.OrderBy(pair => pair.Key,
                    StringComparer.Ordinal))
                {
                    add("usage-pool", pair.Key);
                    add("usage-units", pair.Value.ToString());
                }
                add("strategy", step.ExecutionStrategy.ToString());
            }
            return fields;
        }

        // The read-only capability inventory lists every unit, pool and
        // provider option the planner's own discovery sees, with the
        // effect's recipient shape (direct, self, pet, party, area).
        private static void TestCapabilityInventory()
        {
            IList<string> lines = CastingCapabilityInventory.Describe(QualificationInputs(true, true, null));
            if (!lines.Any(line => line.StartsWith("unit=unit-cleric;", StringComparison.Ordinal)) ||
                !lines.Any(line => line.StartsWith("pool=", StringComparison.Ordinal) &&
                    line.Contains(";kind=Unlimited;")) ||
                !lines.Any(line => line.StartsWith("provider=", StringComparison.Ordinal) &&
                    line.Contains(";caster=unit-cleric;") && line.Contains(";pool=Unlimited:")) ||
                lines.Any(line => line.Contains(";pool=missing:")))
                throw new InvalidOperationException("The inventory missed units, pools or providers: " +
                    string.Join(" | ", lines.ToArray()));
            var direct = new EffectLeafExpression(EffectKind.Buff, "buff-a", EffectTarget.CurrentTarget, null, null);
            var area = new EffectLeafExpression(EffectKind.AreaBuff, "buff-b", EffectTarget.AlliedAreaRecipients, null, null);
            if (CastingCapabilityInventory.Shape(direct) != "direct" ||
                CastingCapabilityInventory.Shape(area) != "allied-area" ||
                CastingCapabilityInventory.Shape(new SequenceEffectExpression(new EffectExpression[] { direct, area })) !=
                    "allied-area+direct" ||
                CastingCapabilityInventory.Shape(null) != "none" ||
                CastingCapabilityInventory.Describe(null).Single() != "inputs-unavailable")
                throw new InvalidOperationException("Effect shapes were not described exactly.");
            // A variant provider's effect is wrapped in a reference to the
            // variant it casts: still a plain buff of the cast ability itself
            // (advanced fixture, 2026-09-24), while a reference to any other
            // ability is not.
            var variantAbility = new AbilityKey("base-guid", "variant-guid", 0, SourceKind.Spellbook, null);
            var wrapped = new ReferencedAbilityExpression("variant-guid", direct);
            if (!ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(wrapped, variantAbility) ||
                !ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(
                    new ReferencedAbilityExpression("base-guid", direct), variantAbility) ||
                ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(
                    new ReferencedAbilityExpression("other-guid", direct), variantAbility) ||
                ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(wrapped,
                    new AbilityKey("base-guid", null, 0, SourceKind.Spellbook, null)) ||
                CastingCapabilityInventory.Structure(wrapped) != "ref:variant-(leaf:Buff:direct)" ||
                CastingCapabilityInventory.Structure(new SequenceEffectExpression(new EffectExpression[] { direct, area })) !=
                    "seq(leaf:Buff:direct,leaf:AreaBuff:allied-area)")
                throw new InvalidOperationException("A variant's own reference was not a plain buff, or structure is wrong: " +
                    CastingCapabilityInventory.Structure(wrapped));
            if (CastingCapabilityInventory.Leaves(new SequenceEffectExpression(new EffectExpression[] { direct, area })) !=
                    "AreaBuff:buff-b,Buff:buff-a" ||
                CastingCapabilityInventory.Leaves(null) != "none" ||
                !lines.Any(line => line.StartsWith("provider=", StringComparison.Ordinal) && line.Contains(";leaves=")))
                throw new InvalidOperationException("Effect leaves were not listed exactly.");
        }

        // The at-will choice (live runs a1-anim-01 and d1-01): only a cantrip
        // of exactly the authored ability, unbound to a spellbook, available
        // and unlimited, qualifies; name- or effect-alikes, variants,
        // metamagic forms, spellbook-bound and finite abilities never do;
        // abilities from several classes resolve by the authored spellbook's
        // caster level, and are refused when that cannot decide.
        private static void TestAtWillCantripChoice()
        {
            Func<string, bool, bool, bool, bool, int, int, AtWillCantripCandidate> make =
                (id, cantrip, matches, book, available, count, cl) =>
                    new AtWillCantripCandidate(id, cantrip, matches, book, available, count, cl);
            string refusal;
            if (AtWillCantripChoice.Choose(new AtWillCantripCandidate[0], 1, out refusal) != null || refusal != null)
                throw new InvalidOperationException("A missing grant chose something or refused.");
            var misfits = new[]
            {
                make("same-name-not-cantrip", false, true, false, true, -1, 1),
                make("other-variant-or-metamagic", true, false, false, true, -1, 1),
                make("spellbook-bound", true, true, true, true, -1, 1),
                make("unavailable", true, true, false, false, -1, 1),
                make("finite", true, true, false, true, 3, 1),
                make("no-count", true, true, false, true, 0, 1)
            };
            foreach (AtWillCantripCandidate misfit in misfits)
                if (AtWillCantripChoice.Choose(new[] { misfit }, 1, out refusal) != null || refusal != null)
                    throw new InvalidOperationException("Not an at-will cantrip, but chosen: " + misfit.Identity);
            AtWillCantripCandidate fact = make("fact", true, true, false, true, -1, 1);
            if (AtWillCantripChoice.Choose(misfits.Concat(new[] { fact }), 1, out refusal) != fact || refusal != null)
                throw new InvalidOperationException("The one at-will cantrip was not chosen among misfits.");
            AtWillCantripCandidate bard = make("bard", true, true, false, true, -1, 3);
            AtWillCantripCandidate sorcerer = make("sorcerer", true, true, false, true, -1, 1);
            if (AtWillCantripChoice.Choose(new[] { bard, sorcerer }, 1, out refusal) != sorcerer ||
                AtWillCantripChoice.Choose(new[] { bard, sorcerer }, 3, out refusal) != bard)
                throw new InvalidOperationException("The authored spellbook's caster level did not decide.");
            AtWillCantripCandidate twin = make("twin", true, true, false, true, -1, 3);
            if (AtWillCantripChoice.Choose(new[] { bard, twin }, 1, out refusal) != bard || refusal != null)
                throw new InvalidOperationException("Equivalent at-will abilities were refused.");
            if (AtWillCantripChoice.Choose(new[] { bard, sorcerer }, 2, out refusal) != null ||
                refusal != AtWillCantripChoice.AmbiguousPrefix + "bard@cl3,sorcerer@cl1")
                throw new InvalidOperationException("An ambiguous choice was guessed: " + refusal);
        }

        // Final review C3: a live run stops itself before the launcher's own
        // deadline (TimeoutSeconds from the game's start, less a 10-45 s
        // margin) and at once when the launcher's abort marker appears; the
        // host checks it at the top of every live update, load included, and
        // the launcher writes the marker at its deadline and waits a bounded
        // grace.
        private static void TestLiveRunStopRule()
        {
            DateTime start = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
            if (RuntimeTestProtocol.OverallDeadlineMarginSeconds(900) != 45 ||
                RuntimeTestProtocol.OverallDeadlineMarginSeconds(180) != 18 ||
                RuntimeTestProtocol.OverallDeadlineMarginSeconds(5) != 10 ||
                RuntimeTestProtocol.RunStopReason(start.AddSeconds(854), start, 900, false) != null ||
                RuntimeTestProtocol.RunStopReason(start.AddSeconds(855), start, 900, false) != "overall-deadline" ||
                RuntimeTestProtocol.RunStopReason(start.AddSeconds(1), start, 900, true) != "aborted-by-launcher" ||
                RuntimeTestProtocol.RunStopReason(start.AddSeconds(161), start, 180, false) != null ||
                RuntimeTestProtocol.RunStopReason(start.AddSeconds(162), start, 180, false) != "overall-deadline")
                throw new InvalidOperationException("The live run's stop rule is wrong.");
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            string host = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner",
                "RuntimeTesting", "RuntimeTestHost.cs")).Replace("\r\n", "\n");
            string launcher = File.ReadAllText(Path.Combine(directory.FullName, "scripts",
                "Invoke-KingmakerRuntimeTest.ps1")).Replace("\r\n", "\n");
            if (!host.Contains("private bool UpdateLiveUiScenario()\n        {\n            string stop = LiveRunStopReason();\n" +
                    "            if (stop != null)\n                throw new TimeoutException(") ||
                !host.Contains("RuntimeTestProtocol.AbortMarkerFileName") ||
                !launcher.Contains("(Join-Path $evidence 'abort.json')") ||
                !launcher.Contains("$abortWrittenUtc.AddSeconds(120)"))
                throw new InvalidOperationException("The host or the launcher does not enforce the run's stop rule.");
            // Re-review (harness): checked on every update, the menu
            // diagnostics included; the deadline is logged once; a PASS
            // published after the abort marker is a failure.
            if (host.Contains("_stopCheckCountdown") ||
                !host.Contains("string waitingStop = LiveRunStopReason();") ||
                !host.Contains("        private bool UpdateMenuDiagnosticScenario()\n        {\n            string stop = LiveRunStopReason();\n" +
                    "            if (stop != null)\n                throw new TimeoutException(\"Menu diagnostics stopped;\" + stop);") ||
                !host.Contains("_log.Info(\"[KBP-RT] overall deadline \" + _processStartUtc.Value") ||
                !launcher.Contains("if ($null -ne $abortWrittenUtc -and [string]$result.status -ceq 'PASS') {"))
                throw new InvalidOperationException("The stop rule is not checked on every update or in the menu diagnostics, " +
                    "the deadline is not logged, or a PASS after the abort is accepted.");
        }

        // Final review A3: presence alone never confirms. Only an instance
        // that is new or refreshed against the read taken before submission,
        // and not suppressed, confirms a recipient; every recipient must be
        // reached, and an empty set or a missing read confirms nothing. Both
        // live adapters read before submitting and judge by this rule.
        private static void TestAppliedEffectJudgement()
        {
            EffectExpression expected = new EffectLeafExpression(EffectKind.Buff, "buff-a",
                EffectTarget.CurrentTarget, "fixture", "fixture/a");
            Func<string, long, bool, ObservedEffectInstance> instance = (key, end, suppressed) =>
                new ObservedEffectInstance(EffectKind.Buff, "buff-a", key, end, suppressed);
            var none = new ObservedEffectInstance[0];
            var old = new[] { instance("1", 100, false) };
            var cases = new[]
            {
                new { Name = "new-instance", Before = none, After = new[] { instance("2", 200, false) }, Reached = true },
                new { Name = "unchanged-old-instance", Before = old, After = old, Reached = false },
                new { Name = "refreshed", Before = old, After = new[] { instance("1", 160, false) }, Reached = true },
                new { Name = "shortened", Before = old, After = new[] { instance("1", 90, false) }, Reached = false },
                new { Name = "replaced", Before = old, After = new[] { instance("3", 100, false) }, Reached = true },
                new { Name = "new-but-suppressed", Before = none, After = new[] { instance("2", 200, true) }, Reached = false },
                new { Name = "old-now-unsuppressed", Before = new[] { instance("1", 100, true) }, After = old, Reached = false },
                new { Name = "gone", Before = old, After = none, Reached = false },
                new { Name = "other-effect", Before = none,
                    After = new[] { new ObservedEffectInstance(EffectKind.Buff, "buff-b", "9", 500, false) }, Reached = false },
                new { Name = "other-kind", Before = none,
                    After = new[] { new ObservedEffectInstance(EffectKind.AreaBuff, "buff-a", "4", 500, false) }, Reached = false },
                new { Name = "old-kept-plus-new", Before = old, After = new[] { old[0], instance("5", 300, false) }, Reached = true },
                new { Name = "new-suppressed-beside-old", Before = old, After = new[] { old[0], instance("6", 300, true) }, Reached = false },
                new { Name = "old-buff-with-new-area-instance", Before = old,
                    After = new[] { old[0], new ObservedEffectInstance(EffectKind.AreaBuff, "buff-a", "7", 900, false) }, Reached = false }
            };
            foreach (var value in cases)
                if (AppliedEffectJudgement.Reached(expected, value.Before, value.After) != value.Reached)
                    throw new InvalidOperationException("Effect confirmation misjudged: " + value.Name + ".");
            if (AppliedEffectJudgement.Reached(expected, null, old) ||
                AppliedEffectJudgement.Reached(expected, none, null))
                throw new InvalidOperationException("A missing read confirmed an effect.");
            // Both leaves of a sequence must be present (not suppressed); one
            // of them new is enough to show this attempt landed.
            EffectExpression pair = new SequenceEffectExpression(new EffectExpression[]
            {
                expected,
                new EffectLeafExpression(EffectKind.Buff, "buff-b", EffectTarget.CurrentTarget, "fixture", "fixture/b")
            });
            var oldB = new ObservedEffectInstance(EffectKind.Buff, "buff-b", "7", 100, false);
            if (!AppliedEffectJudgement.Reached(pair, new[] { oldB }, new[] { oldB, instance("8", 200, false) }) ||
                AppliedEffectJudgement.Reached(pair, none, new[] { instance("8", 200, false) }) ||
                AppliedEffectJudgement.Reached(pair, new[] { old[0], oldB }, new[] { old[0], oldB }))
                throw new InvalidOperationException("A sequence of expected effects was misjudged.");
            // Focused re-review: production expressions are rooted in the
            // ability reference, party buffs sit inside a targeted
            // expression, and some branch on a condition; the leaf rule holds
            // through each.
            EffectExpression referenced = new ReferencedAbilityExpression("ability-a",
                new TargetedEffectExpression(EffectTarget.Party, expected));
            EffectExpression conditional = new ReferencedAbilityExpression("ability-b",
                new ConditionalEffectExpression("fixture-condition", expected, expected));
            foreach (EffectExpression wrapped in new[] { referenced, conditional })
                if (!AppliedEffectJudgement.Reached(wrapped, none, new[] { instance("9", 200, false) }) ||
                    AppliedEffectJudgement.Reached(wrapped, old, old) ||
                    AppliedEffectJudgement.Reached(wrapped, old, new[] { old[0],
                        new ObservedEffectInstance(EffectKind.AreaBuff, "buff-a", "10", 900, false) }))
                    throw new InvalidOperationException("A wrapped expression was judged by another rule: " +
                        ((ReferencedAbilityExpression)wrapped).AbilityId + ".");
            // Every recipient; none is never enough.
            var baseline = new EffectBaseline(new Dictionary<string, IEnumerable<ObservedEffectInstance>>(StringComparer.Ordinal)
            {
                { "unit-a", none },
                { "unit-b", old }
            });
            Func<string, IEnumerable<ObservedEffectInstance>> fresh = unitId => new[] { instance("n-" + unitId, 400, false) };
            if (!AppliedEffectJudgement.AllReached(new[] { "unit-a", "unit-b" }, expected, baseline, fresh) ||
                AppliedEffectJudgement.AllReached(new string[0], expected, baseline, fresh) ||
                AppliedEffectJudgement.AllReached(new[] { "unit-a", "unit-c" }, expected, baseline, fresh) ||
                AppliedEffectJudgement.AllReached(new[] { "unit-a", "unit-b" }, expected, baseline,
                    unitId => unitId == "unit-b" ? old : fresh(unitId)) ||
                AppliedEffectJudgement.AllReached(new[] { "unit-a" }, expected, baseline, unitId => null) ||
                AppliedEffectJudgement.AllReached(new[] { "unit-a" }, expected, null, fresh))
                throw new InvalidOperationException("Recipients were confirmed without each being reached.");
            // The live adapters read before submitting and judge by the rule.
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            Func<string, string> source = name => File.ReadAllText(Path.Combine(directory.FullName, "src",
                "KingmakerBuffPlanner", "GameAdapters", name)).Replace("\r\n", "\n");
            string instant = source("KingmakerInstantCastAdapter.cs");
            string animated = source("KingmakerAnimatedCastAdapter.cs");
            string reader = source("KingmakerEffectInstanceReader.cs");
            const string read = "EffectBaseline baseline = KingmakerEffectInstanceReader.ReadBaseline(step, out baselineFailure);";
            int instantRead = instant.IndexOf(read, StringComparison.Ordinal);
            int animatedRead = animated.IndexOf(read, StringComparison.Ordinal);
            if (instantRead < 0 || instantRead > instant.IndexOf("rule = Rulebook.Trigger(new RuleCastSpell(", StringComparison.Ordinal) ||
                instantRead > instant.IndexOf("BrownFurDirectCastCompatibility.TryBegin(", StringComparison.Ordinal) ||
                !instant.Contains("return KingmakerEffectInstanceReader.AppliedByThisAttempt(step, baseline);") ||
                animatedRead < 0 || animatedRead > animated.IndexOf("resolved.Caster.Commands.AddToQueue(command);", StringComparison.Ordinal) ||
                !animated.Contains("bool observed = KingmakerEffectInstanceReader.AppliedByThisAttempt(_step, _baseline);") ||
                animated.Contains("new KingmakerActiveEffectSnapshotBuilder().Build();") ||
                instant.Contains("new KingmakerActiveEffectSnapshotBuilder().Build();") ||
                !reader.Contains("return AppliedEffectJudgement.AllReached(step.ExpectedRecipientUnitIds,") ||
                !reader.Contains("catch (Exception) { return true; }"))
                throw new InvalidOperationException("A live adapter confirms by presence or without its read before submission.");
            // Re-review: a party effect that also reaches pets is planned as a
            // party effect, before the pet-only branch.
            string optionBuilder = source("KingmakerProviderOptionBuilder.cs");
            int partyAt = optionBuilder.IndexOf("                else if (party)\n", StringComparison.Ordinal);
            int petAt = optionBuilder.IndexOf("                else if (EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Pet))", StringComparison.Ordinal);
            if (partyAt < 0 || petAt < 0 || partyAt > petAt)
                throw new InvalidOperationException("A party-and-pet effect is planned as pet-only.");
        }

        // Final review A1: a Classic routine held by a paused world (or an
        // open full-screen window) is not advanced at all, so a step's
        // frame-counted confirmation window cannot expire while its cast
        // cannot land; the halting runner then halts nothing. Disposing the
        // gate runs the routine's cleanup.
        private static void TestWorldGatedClassicRun()
        {
            CastingWorkspaceInputs inputs = QualificationInputs(true, true, null);
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectZeroCostMixed(
                inputs, "fixture-campaign");
            CastPlan plan = CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign")[0]
                .Projection.Plan;
            bool worldRuns = false;
            var windows = new List<WindowIterator>();
            Func<ScriptedExecutor> executorFor = () => new ScriptedExecutor((single, stepReport) =>
            {
                var window = new WindowIterator(() => worldRuns, confirmed => stepReport.Add(0, single.Steps[0],
                    confirmed ? CastExecutionStatus.EffectConfirmed : CastExecutionStatus.TimedOutUnconfirmed,
                    "fixture-window"));
                windows.Add(window);
                return window;
            });
            // Without the gate a held world uses up the first window and the
            // halting runner abandons the rest (the regression).
            ScriptedExecutor ungatedExecutor = executorFor();
            var ungatedReport = new ExecutionReport(plan);
            System.Collections.IEnumerator ungated = new HaltingPlanRunner(ungatedExecutor).Run(plan, ungatedReport);
            for (int frame = 0; frame < 50 && ungated.MoveNext(); frame++) { }
            worldRuns = true;
            int guard = 0;
            while (ungated.MoveNext() && guard++ < 1000) { }
            if (ungatedReport.Confirmed != 0 || ungatedExecutor.Executed.Count != 1)
                throw new InvalidOperationException("The fixture does not reproduce the held-world regression.");
            // With the gate nothing advances while the world is held.
            worldRuns = false;
            windows.Clear();
            ScriptedExecutor executor = executorFor();
            var report = new ExecutionReport(plan);
            var gate = new WorldGatedEnumerator(new HaltingPlanRunner(executor).Run(plan, report), () => worldRuns);
            for (int frame = 0; frame < 50; frame++) gate.MoveNext();
            worldRuns = true;
            guard = 0;
            while (gate.MoveNext() && guard++ < 1000) { }
            gate.Dispose();
            if (gate.HeldFrames != 50 || windows.Any(window => window.HeldAdvances != 0) || report.Confirmed != 3 ||
                report.Failed != 0 || executor.Executed.Count != 3)
                throw new InvalidOperationException("A held world used up a Classic step's window: held=" +
                    gate.HeldFrames + ";confirmed=" + report.Confirmed + ";executed=" + executor.Executed.Count);
            // Disposed while held, the routine's cleanup still runs.
            bool disposed = false;
            var cleanup = new WorldGatedEnumerator(new DisposalProbe(() => disposed = true), () => false);
            cleanup.MoveNext();
            cleanup.Dispose();
            if (!disposed || cleanup.MoveNext())
                throw new InvalidOperationException("Disposing a held Classic run skipped its cleanup.");
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            string rootSource = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "UI",
                "BuffPlannerUiRoot.cs")).Replace("\r\n", "\n");
            // Re-review: the session's checks run at the press and only its
            // casting phase is gated, after it is marked executing; the gate
            // uses the casting-first host's rule; an accepted run closes the
            // open Classic screen (a frame later), and a result that arrives
            // while the screen is closed is shown when it next opens.
            string sessionSource = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "UI",
                "PlannerUiSession.cs")).Replace("\r\n", "\n");
            string controllerSource = File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "UI",
                "BuffPlannerScreenController.cs")).Replace("\r\n", "\n");
            const string gatedRun = "IEnumerator work = new WorldGatedEnumerator(runner.Run(preview.Plan, LastExecutionReport),\n                worldRuns);";
            int gatedAt = sessionSource.IndexOf(gatedRun, StringComparison.Ordinal);
            int executingAt = sessionSource.IndexOf("            IsExecuting = true;\n", StringComparison.Ordinal);
            if (gatedAt < 0 || executingAt < 0 || executingAt > gatedAt ||
                !sessionSource.Contains("Func<bool> worldRuns = ClassicWorldRuns ?? (() => true);") ||
                rootSource.Contains("new WorldGatedEnumerator(_session.ExecuteRoutine(") ||
                !rootSource.Contains("_session.ClassicWorldRuns = () => WorldRunsForCasting && _castingWorkspace == null &&\n                (_screen == null || !_screen.IsOpen);") ||
                !rootSource.Contains("                    if (!closeDecided)\n                    {\n                        closeDecided = true;\n" +
                    "                        if (ClassicRunScreenPolicy.CloseAfterPress(_session.IsExecuting,") ||
                System.Text.RegularExpressions.Regex.Matches(rootSource, "_closeScreenForClassicRun = true;").Count != 1 ||
                !rootSource.Contains("        private void EndClassicRun(string reason)\n        {\n            _closeScreenForClassicRun = false;") ||
                !rootSource.Contains("        private void ReleasePlayerUi()\n        {\n            _closeScreenForClassicRun = false;") ||
                !rootSource.Contains("                if (_closeScreenForClassicRun)\n                {\n                    _closeScreenForClassicRun = false;") ||
                !rootSource.Contains("if (_screen != null && !CastingFirstActive)\n                _screen.Present(result,") ||
                System.Text.RegularExpressions.Regex.Matches(rootSource, "_screen.DiscardUnshownResult\\(\\);").Count != 2 ||
                !rootSource.Contains("if (_screen != null) _screen.DiscardUnshownResult();\n            EndClassicRun(\"area-unloading\");") ||
                !sessionSource.Contains("\" casts were confirmed; a cast in progress, if any, was cleaned up, and nothing after it was attempted.\"") ||
                !controllerSource.Contains("                _unshownResult = result;\n                _unshownResultCampaign = campaignId;") ||
                !controllerSource.Contains("ClassicRunScreenPolicy.ShowStashedResult(_unshownResultCampaign,") ||
                !controllerSource.Contains("                    _view.ShowResult(_unshownResult);") ||
                !sessionSource.Contains("\"it had not cast anything yet, and nothing was attempted.\"") ||
                !sessionSource.Contains("bool submitted = LastExecutionReport != null && LastExecutionReport.AnyCastAttempted;") ||
                !File.ReadAllText(Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner", "UI",
                    "BuffPlannerHudButtonController.cs")).Contains("return _session.ClassicRunHeld"))
                throw new InvalidOperationException("The Classic run's checks, gate, screen close or result are not where they belong.");
        }

        // A cast's confirmation window of three frames: it confirms only if
        // every frame passed while the world ran (a cast cannot land while
        // the world is held).
        private sealed class WindowIterator : System.Collections.IEnumerator, IDisposable
        {
            private readonly Func<bool> _worldRuns;
            private readonly Action<bool> _finish;
            private int _frames;
            private bool _done;
            internal WindowIterator(Func<bool> worldRuns, Action<bool> finish)
            {
                _worldRuns = worldRuns;
                _finish = finish;
            }
            internal int HeldAdvances { get; private set; }
            public object Current { get { return null; } }
            public bool MoveNext()
            {
                if (_done) return false;
                if (!_worldRuns()) HeldAdvances++;
                if (_frames < 3)
                {
                    _frames++;
                    return true;
                }
                _done = true;
                _finish(HeldAdvances == 0);
                return false;
            }
            public void Reset() { }
            public void Dispose() { }
        }

        private sealed class DisposalProbe : System.Collections.IEnumerator, IDisposable
        {
            private readonly Action _disposed;
            internal DisposalProbe(Action disposed) { _disposed = disposed; }
            public object Current { get { return null; } }
            public bool MoveNext() { return true; }
            public void Reset() { }
            public void Dispose() { _disposed(); }
        }

        // Review A2/A4: the reservation, never the entry alone, routes a
        // level-0 casting; an ambiguous at-will choice is priced unresolved.
        private static void TestCantripRouteAndPricing()
        {
            var routes = new Dictionary<string, CantripRoute>
            {
                { "level0/free", AtWillCantripChoice.Route(true, true) },
                { "level0/finite", AtWillCantripChoice.Route(true, false) },
                { "level0/unreserved", AtWillCantripChoice.Route(true, null) },
                { "other/free", AtWillCantripChoice.Route(false, true) },
                { "other/finite", AtWillCantripChoice.Route(false, false) },
                { "other/unreserved", AtWillCantripChoice.Route(false, null) }
            };
            var expected = new Dictionary<string, CantripRoute>
            {
                { "level0/free", CantripRoute.AtWillOnly },
                { "level0/finite", CantripRoute.SlotOnly },
                { "level0/unreserved", CantripRoute.AtWillThenSlot },
                { "other/free", CantripRoute.Refused },
                { "other/finite", CantripRoute.Normal },
                { "other/unreserved", CantripRoute.Normal }
            };
            foreach (KeyValuePair<string, CantripRoute> item in expected)
                if (routes[item.Key] != item.Value)
                    throw new InvalidOperationException("Cantrip route " + item.Key + " was " + routes[item.Key]);
            if (AtWillCantripChoice.Price(true, null) != CantripPricing.Free ||
                AtWillCantripChoice.Price(false, null) != CantripPricing.Finite ||
                AtWillCantripChoice.Price(false, AtWillCantripChoice.AmbiguousPrefix + "bard@cl3,sorcerer@cl1") !=
                    CantripPricing.Unresolved)
                throw new InvalidOperationException("An ambiguous at-will cantrip was priced as a slot or free.");
            if (AtWillCantripChoice.MissingRefusal != "at-will-cantrip-missing" ||
                AtWillCantripChoice.FreeReservationRefusal != "free-reservation-for-slot-entry")
                throw new InvalidOperationException("The cantrip refusals changed.");
        }

        // Review A3: a Fact or AbilityResource casting is cast through an
        // owned ability of its own kind and reserved pool, never another.
        private static void TestFactSourceChoice()
        {
            var free = new FactSourceCandidate("free-a#1", false, false, "u|free|a");
            var freeTwin = new FactSourceCandidate("free-a#4", false, false, "u|free|a");
            var resource = new FactSourceCandidate("res-a#2", false, true, "u|resource|r1");
            var otherResource = new FactSourceCandidate("res-b#3", false, true, "u|resource|r2");
            var bound = new FactSourceCandidate("book#5", true, false, "u|free|a");
            var all = new[] { bound, resource, free, otherResource, freeTwin };
            int equivalents;
            string refusal;
            if (FactSourceChoice.Choose(all, false, "u|free|a", out equivalents, out refusal) != free ||
                equivalents != 2 || refusal != null)
                throw new InvalidOperationException("A free casting did not take its free, unbound source.");
            if (FactSourceChoice.Choose(all, true, "u|resource|r2", out equivalents, out refusal) != otherResource ||
                equivalents != 1 || refusal != null)
                throw new InvalidOperationException("A resource casting did not take its reserved pool.");
            // Free never becomes paid, paid never becomes free.
            if (FactSourceChoice.Choose(new[] { resource, bound }, false, "u|free|a", out equivalents, out refusal) !=
                    null || refusal == null ||
                !refusal.StartsWith(FactSourceChoice.UnavailablePrefix + "free:u|free|a;seen=", StringComparison.Ordinal) ||
                refusal.IndexOf("res-a#2/resource/u|resource|r1", StringComparison.Ordinal) < 0 ||
                refusal.IndexOf("book#5/spellbook/u|free|a", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("A free casting moved to a paid or spellbook source: " + refusal);
            if (FactSourceChoice.Choose(new[] { free }, true, "u|resource|r1", out equivalents, out refusal) != null ||
                refusal == null)
                throw new InvalidOperationException("A paid casting moved to a free source.");
            // A resource casting whose reserved pool is gone is refused even
            // when another resource of the same ability remains.
            if (FactSourceChoice.Choose(new[] { otherResource }, true, "u|resource|r1", out equivalents,
                    out refusal) != null || refusal == null)
                throw new InvalidOperationException("A casting moved to another resource pool.");
            // Unreserved reads (discovery, targeting) keep the kind only.
            if (FactSourceChoice.Choose(all, true, null, out equivalents, out refusal) != resource ||
                equivalents != 1 || refusal != null ||
                FactSourceChoice.Choose(new[] { free, resource }, true, null, out equivalents, out refusal) !=
                    resource ||
                FactSourceChoice.Choose(new[] { resource, free }, false, null, out equivalents, out refusal) != free)
                throw new InvalidOperationException("An unreserved read ignored the kind.");
            // Re-review: one pool at two caster levels (a cantrip granted by
            // two classes) is ambiguous, reserved or not; one level is not.
            var clThree = new FactSourceCandidate("class-a#1", false, false, "u|free|r", 3);
            var clOne = new FactSourceCandidate("class-b#2", false, false, "u|free|r", 1);
            var clThreeTwin = new FactSourceCandidate("class-c#3", false, false, "u|free|r", 3);
            foreach (string reserved in new[] { "u|free|r", null })
                if (FactSourceChoice.Choose(new[] { clThree, clOne }, false, reserved, out equivalents, out refusal) != null ||
                    refusal == null || !refusal.StartsWith(FactSourceChoice.AmbiguousPrefix, StringComparison.Ordinal) ||
                    refusal.IndexOf("class-a#1/free/u|free|r@cl3", StringComparison.Ordinal) < 0 ||
                    refusal.IndexOf("class-b#2/free/u|free|r@cl1", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Two caster levels were guessed between: " + refusal);
            if (FactSourceChoice.Choose(new[] { clThree, clThreeTwin }, false, "u|free|r", out equivalents, out refusal) !=
                    clThree || equivalents != 2 || refusal != null)
                throw new InvalidOperationException("Equivalent sources at one caster level were refused.");
            // The kind decides on its own, even where a pool key would match.
            var mislabeled = new FactSourceCandidate("odd#9", false, true, "u|free|a");
            if (FactSourceChoice.Choose(new[] { mislabeled }, false, "u|free|a", out equivalents, out refusal) != null ||
                refusal == null)
                throw new InvalidOperationException("A resource-bound ability served a free casting by pool key.");
        }

        // Review A7: an unread count is never the game's unlimited.
        private static void TestAvailableCountJudgement()
        {
            if (!AvailableCountJudgement.Spent(3, 2) || AvailableCountJudgement.Spent(3, 3) ||
                AvailableCountJudgement.Spent(-1, -1) || AvailableCountJudgement.Spent(null, 2) ||
                AvailableCountJudgement.Spent(3, null) || AvailableCountJudgement.Spent(-1, 2))
                throw new InvalidOperationException("A spend was judged from unread or unlimited counts.");
            if (AvailableCountJudgement.FreeViolation(-1, -1) != null ||
                AvailableCountJudgement.FreeViolation(null, -1) != "available-count-unread:unread>-1" ||
                AvailableCountJudgement.FreeViolation(-1, null) != "available-count-unread:-1>unread" ||
                AvailableCountJudgement.FreeViolation(null, null) != "available-count-unread:unread>unread" ||
                AvailableCountJudgement.FreeViolation(2, 2) != "available-count-not-unlimited:2>2" ||
                AvailableCountJudgement.FreeViolation(-1, 0) != "available-count-not-unlimited:-1>0" ||
                AvailableCountJudgement.FreeViolation(0, 0) != "available-count-not-unlimited:0>0")
                throw new InvalidOperationException("A free casting was judged free without unlimited counts.");
            // The last slot or charge is a spend.
            if (!AvailableCountJudgement.Spent(1, 0) || AvailableCountJudgement.Spent(0, 0))
                throw new InvalidOperationException("The last use was not judged as a spend.");
            // Re-review: a finite casting must read both counts as finite.
            if (AvailableCountJudgement.FiniteViolation(3, 2) != null || AvailableCountJudgement.FiniteViolation(0, 0) != null ||
                AvailableCountJudgement.FiniteViolation(null, 2) != "available-count-unread:unread>2" ||
                AvailableCountJudgement.FiniteViolation(3, null) != "available-count-unread:3>unread" ||
                AvailableCountJudgement.FiniteViolation(-1, -1) != "available-count-not-finite:-1>-1")
                throw new InvalidOperationException("A finite casting's counts were judged wrongly.");
            var free = new ResourceReservation("pool-free", 0, new string[0], true);
            var paid = new ResourceReservation("pool-paid", 1, new string[0]);
            if (AvailableCountJudgement.Violation(null, null, null) != null ||
                AvailableCountJudgement.Violation(free, -1, -1) != null ||
                AvailableCountJudgement.Violation(free, 3, 3) != "available-count-not-unlimited:3>3" ||
                AvailableCountJudgement.Violation(paid, 3, 2) != null ||
                AvailableCountJudgement.Violation(paid, null, 2) != "available-count-unread:unread>2" ||
                AvailableCountJudgement.PrefixFor(free) != AvailableCountJudgement.UncertainPrefix ||
                AvailableCountJudgement.PrefixFor(paid) != AvailableCountJudgement.FiniteUncertainPrefix)
                throw new InvalidOperationException("The reservation's count rule is wrong.");
        }

        // The live shape: the planner's root owns and pumps the host, the
        // driver never pumps it, and the stop is the root's routine press
        // (here the host call that press makes).
        private static void TestQualificationOnOwnerPumpedHost(string root)
        {
            var world = new SimulatedBuffWorld();
            long now = 0;
            CastingExecutionHost host = QualificationHost(world, () => now);
            var presses = new List<string>();
            // The owner (the planner root) is not ticked while the mod is
            // disabled, as Main's update gates it; its ticks are counted.
            bool ownerEnabled = true;
            long ownerTickCount = 0;
            var record = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string dir = Path.Combine(root, "qa-owner");
            Directory.CreateDirectory(dir);
            CastingQualificationDriver driver = NewQualificationDriver(dir, world, record,
                ForecastAllowance(world, null, "animated"), () => now, null, null, host, true,
                routine =>
                {
                    presses.Add(routine);
                    return host.RequestStop(CastingExecutionHost.PlayerStopReason);
                },
                enabled =>
                {
                    ownerEnabled = enabled;
                    if (!enabled) host.Shutdown(CastingQualificationDriver.DisableReason);
                    else host.Resume();
                }, null, () => ownerTickCount);
            // Unpumped by its owner, the approved stop run never advances.
            for (int i = 0; i < 50 && !driver.Completed; i++) { now += 16; driver.Update(); }
            if (!host.IsRunning || world.AnimatedStarts.Count != 0 || driver.Phase != "stop-wait" ||
                presses.Count != 0)
                throw new InvalidOperationException("The driver pumped a host its owner pumps.");
            // The owner pumps once per frame, after the driver's update.
            for (int i = 0; i < 5000 && !driver.Completed; i++)
            {
                now += 16;
                world.Now = now;
                driver.Update();
                if (ownerEnabled)
                {
                    ownerTickCount++;
                    host.Pump();
                }
            }
            if (record.Violations().Count != 0 || record.TerminalReason != "completed" ||
                !presses.SequenceEqual(new[] { CastingQualificationRecipe.RoutineId }) ||
                record.StopPress != "routine-press;handled=True;inFlight=True;pending=player-stopped" ||
                record.Disable != "planner-disable;at=in-flight;ended=True;held=5;acceptingWhileDisabled=False;" +
                    "runningWhileDisabled=False;ownerTicks=0;runsDuringHold=0;accepting=True" ||
                host.StartedRuns != 5 ||
                !world.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-3", "qual-cast-1", "qual-cast-1" }))
                throw new InvalidOperationException("The owner-pumped qualification was not accepted: " +
                    string.Join("|", record.Violations().ToArray()) + "|" + record.StopPress);
            // A press that never reaches the running host fails closed AT the
            // stop step: that run completes, the step is judged wrong and
            // nothing after it is submitted.
            var deaf = new SimulatedBuffWorld();
            CastingExecutionHost deafHost = QualificationHost(deaf, () => now);
            var deafRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string deafDir = Path.Combine(root, "qa-deaf");
            Directory.CreateDirectory(deafDir);
            CastingQualificationDriver deafDriver = NewQualificationDriver(deafDir, deaf, deafRecord,
                ForecastAllowance(deaf, null, "animated"), () => now, null, null, deafHost, true, routine => true);
            for (int i = 0; i < 5000 && !deafDriver.Completed; i++)
            {
                now += 16;
                deaf.Now = now;
                deafDriver.Update();
                deafHost.Pump();
            }
            if (deafRecord.StopPressHandled || deafHost.StartedRuns != 1 ||
                !deaf.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-3" }) ||
                !deafRecord.Failures.Contains("stop-wait:step:report:completed") ||
                deafRecord.StopPress != "routine-press;handled=True;inFlight=True;pending=none" ||
                deafRecord.Violations().Count == 0)
                throw new InvalidOperationException("A stop press that never reached the host did not fail closed: " +
                    string.Join("|", deafRecord.Violations().ToArray()) + "|fired=" +
                    string.Join(",", deaf.Fired.ToArray()));
        }

        // The whole zero-cost-mixed qualification through the production
        // session, gate, converter, qualification boundary and host.
        private static void TestQualificationDriverEndToEnd(string root)
        {
            var world = new SimulatedBuffWorld();
            CastingQualificationAllowance allowance = ForecastAllowance(world);
            var record = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string dir = Path.Combine(root, "qualification-e2e");
            Directory.CreateDirectory(dir);
            long now = 0;
            CastingQualificationDriver driver = NewQualificationDriver(dir, world, record, allowance, () => now);
            int guard = 0;
            while (!driver.Completed && guard++ < 5000) { now += 16; world.Now = now; driver.Update(); }
            IList<string> violations = record.Violations();
            string steps = string.Join(";", record.Steps.Select(step => step.Name + "=" +
                (step.Report == null ? step.ApplyReason : step.Report.TerminalReason)).ToArray());
            if (!driver.Completed || record.TerminalReason != "completed" || violations.Count != 0)
                throw new InvalidOperationException("The qualification did not pass: " +
                    record.TerminalReason + "|" + string.Join("|", violations.ToArray()) + "|" + steps);
            if (!world.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-3", "qual-cast-1", "qual-cast-1" }) ||
                record.PlannedSubmissions != 3 + 2 + 1 + 1 + 1)
                throw new InvalidOperationException("Unexpected submissions: " +
                    string.Join(",", world.Fired.ToArray()) + "|" + record.PlannedSubmissions);
            // The instant disable-before-start case (not an in-flight
            // interruption): the disable lands before the run's first step,
            // nothing is submitted, it is held, and the host resumes.
            CastingOutcomeEntry disabled = record.Step("disable").Report.Entries
                .First(entry => entry.CastingId == "qual-cast-1");
            if (record.DisabledAt != "before-start" || !record.AcceptingAfterEnable ||
                record.Disable != "host-shutdown;at=before-start;ended=True;held=5;acceptingWhileDisabled=False;" +
                    "runningWhileDisabled=False;ownerTicks=0;runsDuringHold=0;accepting=True" ||
                disabled.State != CastingOutcomeState.NotProcessed || disabled.Submitted ||
                record.Step("disable").Report.TerminalReason != "cancelled:" + CastingQualificationDriver.DisableReason)
                throw new InvalidOperationException("The instant disable step was not before the run started: " +
                    record.Disable + "|" + disabled.State + "|" + record.Step("disable").Report.TerminalReason);
            // The recover step is a NEW run after the enable (the stopped
            // run never resumed): qual-cast-1 recast, the rest skipped, and
            // every started run reported exactly once.
            CastingQualificationStepResult recover = record.Step("recover");
            if (recover == null || recover.Report.TerminalReason != "completed" ||
                recover.Report.RunId == record.Step("disable").Report.RunId ||
                recover.StateOf("qual-cast-1") != CastingOutcomeState.EffectConfirmed ||
                recover.StateOf("qual-cast-2") != CastingOutcomeState.Skipped ||
                record.RunsStarted != 5 || record.RunsReported != 5)
                throw new InvalidOperationException("The recover step was not a new completed run: " +
                    (recover == null ? "missing" : recover.Report.RunId + "|" + recover.Report.TerminalReason) +
                    "|" + record.RunsStarted + "/" + record.RunsReported);
            // The owner's lifecycle probe: the same state before the disable
            // and after the recover passes; any change (a duplicated
            // subscription, root or lease) fails the record.
            foreach (bool drifting in new[] { false, true })
            {
                var probeWorld = new SimulatedBuffWorld();
                var probeRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
                string probeDir = Path.Combine(root, drifting ? "qualification-probe-drift" : "qualification-probe-same");
                Directory.CreateDirectory(probeDir);
                int reads = 0;
                long probeNow = 0;
                CastingQualificationDriver probeDriver = NewQualificationDriver(probeDir, probeWorld, probeRecord,
                    ForecastAllowance(probeWorld), () => probeNow, null, null, null, false, null, null,
                    () => drifting ? "subscriptions=" + (++reads) : "subscriptions=1");
                for (int i = 0; i < 5000 && !probeDriver.Completed; i++)
                {
                    probeNow += 16;
                    probeWorld.Now = probeNow;
                    probeDriver.Update();
                }
                IList<string> probeViolations = probeRecord.Violations();
                bool flagged = probeViolations.Contains("lifecycle:subscriptions=1>subscriptions=2");
                if (probeRecord.TerminalReason != "completed" || (drifting ? !flagged || probeViolations.Count != 1
                        : probeViolations.Count != 0 || probeRecord.LifecycleBefore != "subscriptions=1" ||
                            probeRecord.LifecycleAfter != "subscriptions=1"))
                    throw new InvalidOperationException("The lifecycle probe was judged wrong (drifting=" + drifting +
                        "): " + string.Join("|", probeViolations.ToArray()));
            }
            // Run counts: a started run without its single report, or a
            // callback failure, fails the record.
            var counted = new CastingQualificationRecord { CastingScenario = true };
            foreach (Action<CastingQualificationRecord> shape in new Action<CastingQualificationRecord>[]
                {
                    value => value.RunsReported = 4,
                    value => value.RunsStarted = 6,
                    value => value.CallbackFailure = "InvalidOperationException:boom"
                })
            {
                CastingQualificationRecord copy = RecordWith(record, shape);
                if (copy.Violations().Count == 0)
                    throw new InvalidOperationException("A run-count or callback defect passed the record.");
            }
        }

        // A judged copy of a completed record with one field changed.
        private static CastingQualificationRecord RecordWith(CastingQualificationRecord source,
            Action<CastingQualificationRecord> change)
        {
            var copy = new CastingQualificationRecord
            {
                CastingScenario = source.CastingScenario, AllowanceStatus = source.AllowanceStatus,
                TerminalReason = source.TerminalReason, Selection = source.Selection, Roster = source.Roster,
                Forecast = source.Forecast, Submissions = source.Submissions,
                PlannedSubmissions = source.PlannedSubmissions, MaximumSubmissions = source.MaximumSubmissions,
                ExecutionMode = source.ExecutionMode, StopPress = source.StopPress,
                StopPressHandled = source.StopPressHandled, StopPressedInFlight = source.StopPressedInFlight,
                Disable = source.Disable, DisabledAt = source.DisabledAt,
                DisableHeldUpdates = source.DisableHeldUpdates,
                AcceptingWhileDisabled = source.AcceptingWhileDisabled,
                RunningWhileDisabled = source.RunningWhileDisabled,
                OwnerTicksDuringHold = source.OwnerTicksDuringHold,
                RunsStartedDuringHold = source.RunsStartedDuringHold,
                AcceptingAfterEnable = source.AcceptingAfterEnable, LifecycleBefore = source.LifecycleBefore,
                LifecycleAfter = source.LifecycleAfter, RunsStarted = source.RunsStarted,
                RunsReported = source.RunsReported, CallbackFailure = source.CallbackFailure
            };
            copy.Steps.AddRange(source.Steps);
            copy.Failures.AddRange(source.Failures);
            if (copy.Violations().Count != 0)
                throw new InvalidOperationException("The copied record is not clean: " +
                    string.Join("|", copy.Violations().ToArray()));
            change(copy);
            return copy;
        }

        // Nothing reaches the world unless the approved projections are
        // exactly the forecast; selection-only never submits; the run
        // deadline ends it through the host terminal.
        private static void TestQualificationDriverRefusalsAndDeadline(string root)
        {
            var mismatchWorld = new SimulatedBuffWorld();
            CastingQualificationAllowance wrong = ForecastAllowance(mismatchWorld,
                forecast => forecast.Select(step => step.ProjectionId).Reverse());
            var mismatch = new CastingQualificationRecord { CastingScenario = true };
            long now = 0;
            CastingQualificationDriver refused = NewQualificationDriver(
                Path.Combine(root, "qualification-mismatch"), mismatchWorld, mismatch, wrong, () => now);
            for (int i = 0; i < 20 && !refused.Completed; i++) refused.Update();
            if (!refused.Completed || mismatchWorld.Fired.Count != 0 ||
                mismatch.AllowanceStatus != "projections-differ-from-forecast" ||
                mismatch.Violations().Count == 0)
                throw new InvalidOperationException("Mismatched projections reached the world.");
            var selectWorld = new SimulatedBuffWorld();
            var selectOnly = new CastingQualificationRecord { CastingScenario = false };
            CastingQualificationDriver select = NewQualificationDriver(
                Path.Combine(root, "qualification-select"), selectWorld, selectOnly, null, () => now);
            for (int i = 0; i < 20 && !select.Completed; i++) select.Update();
            if (!select.Completed || selectWorld.Fired.Count != 0 || selectOnly.Violations().Count != 0 ||
                selectOnly.Forecast.Count != 5)
                throw new InvalidOperationException("The selection-only run misbehaved.");
            var slowWorld = new SimulatedBuffWorld();
            var slow = new CastingQualificationRecord { CastingScenario = true };
            string slowDir = Path.Combine(root, "qualification-deadline");
            Directory.CreateDirectory(slowDir);
            CastingQualificationDriver deadline = NewQualificationDriver(slowDir, slowWorld, slow,
                ForecastAllowance(slowWorld), () => now);
            for (int i = 0; i < 3; i++) deadline.Update();
            now += 240001;
            deadline.Update();
            if (!deadline.Completed || slow.TerminalReason != "qualification-deadline" ||
                slow.Violations().All(value => !value.StartsWith("terminal:", StringComparison.Ordinal)))
                throw new InvalidOperationException("The qualification deadline did not end the run.");
            // Review P0 (repeat): anything but the exact no-op ends the run
            // AT the repeat step - here an effect expired after complete,
            // so the repeat Apply is refused by the boundary.
            var expiring = new SimulatedBuffWorld();
            CastingQualificationAllowance expiringAllowance = ForecastAllowance(expiring);
            var expiringRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string expiringDir = Path.Combine(root, "qualification-repeat-expired");
            Directory.CreateDirectory(expiringDir);
            CastingQualificationDriver repeatDriver = NewQualificationDriver(expiringDir, expiring,
                expiringRecord, expiringAllowance, () => now);
            for (int i = 0; i < 400 && !repeatDriver.Completed && repeatDriver.Phase != "repeat"; i++)
                repeatDriver.Update();
            if (repeatDriver.Phase != "repeat" || expiring.Fired.Count != 3)
                throw new InvalidOperationException("The repeat fixture did not reach the repeat step.");
            expiring.Active.Remove(expiring.Active.Keys.OrderBy(key => key, StringComparer.Ordinal).Last());
            for (int i = 0; i < 400 && !repeatDriver.Completed; i++) repeatDriver.Update();
            if (expiring.Fired.Count != 3 || expiringRecord.TerminalReason != "failed:repeat" ||
                !expiringRecord.Failures.Any(value => value.StartsWith("repeat:step:not-a-no-op",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("A refused repeat did not end the run at the repeat step: " +
                    expiringRecord.TerminalReason + " " + string.Join("|", expiringRecord.Failures.ToArray()));
            // The step rule itself: a report with a cleanup failure never
            // passes, even when every casting landed as forecast.
            var judged = new CastingQualificationRecord { CastingScenario = true };
            judged.Selection = new CastingQualificationSelection(null, "source-bulls", CastingBuffAbility,
                new[]
                {
                    DirectCasting("qual-cast-1", "long", "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility),
                    DirectCasting("qual-cast-2", "long", "unit-wizard", "unit-t2", "source-bulls", CastingBuffAbility)
                }, 1, null);
            foreach (bool cleanupFailed in new[] { false, true })
            {
                judged.Steps.Clear();
                var stopStep = new CastingQualificationStepResult("stop");
                stopStep.Report = new CastingRunReport("run-1", "long", CastingApplyMode.Ordinary, "p",
                    "cancelled:" + CastingExecutionHost.PlayerStopReason, true, false,
                    new[]
                    {
                        new CastingOutcomeEntry("qual-cast-1", CastingOutcomeState.EffectConfirmed, true, true, false, "ok", true),
                        new CastingOutcomeEntry("qual-cast-2", CastingOutcomeState.NotProcessed, true, false, false, "stopped")
                    },
                    cleanupFailed ? new[] { "restore-failed" } : new string[0]);
                stopStep.Transitions.Add("qual-cast-1:new-instance");
                stopStep.Transitions.Add("qual-cast-2:absent");
                stopStep.Availability.Add("qual-cast-1:-1>-1");
                judged.Steps.Add(stopStep);
                string failure = judged.StepFailure("stop");
                if (cleanupFailed ? failure != "cleanup:restore-failed" : failure != null)
                    throw new InvalidOperationException("The stop rule judged cleanup=" + cleanupFailed +
                        " as " + (failure ?? "pass"));
            }
            // Review RC4: the whole judged sequence on a three-unit party
            // (two casters buffing each other and the one other member).
            var small = new SimulatedBuffWorld { OtherUnits = 1 };
            CastingQualificationAllowance smallAllowance = ForecastAllowance(small);
            var smallRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string smallDir = Path.Combine(root, "q3");
            Directory.CreateDirectory(smallDir);
            CastingQualificationDriver smallDriver = NewQualificationDriver(smallDir, small, smallRecord,
                smallAllowance, () => now);
            for (int i = 0; i < 400 && !smallDriver.Completed; i++) smallDriver.Update();
            if (smallRecord.Violations().Count != 0 || small.Fired.Count != 5 || smallRecord.Roster.Count != 3)
                throw new InvalidOperationException("The three-unit qualification was not accepted: " +
                    string.Join("|", smallRecord.Violations().ToArray()));
            // A held world (paused, dialog, full-screen window) holds the
            // steps: nothing is cast until it runs, then the run completes.
            var heldWorld = new SimulatedBuffWorld();
            CastingQualificationAllowance heldAllowance = ForecastAllowance(heldWorld);
            var heldRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string heldDir = Path.Combine(root, "qh");
            Directory.CreateDirectory(heldDir);
            bool worldRuns = false;
            CastingQualificationDriver heldDriver = NewQualificationDriver(heldDir, heldWorld, heldRecord,
                heldAllowance, () => now, () => worldRuns);
            for (int i = 0; i < 30; i++) heldDriver.Update();
            // Review P3-5: held from the start, no step even begins (nothing
            // applied, not only nothing fired).
            if (heldWorld.Fired.Count != 0 || heldDriver.Completed || heldDriver.HeldUpdates == 0 ||
                heldRecord.Steps.Count != 0)
                throw new InvalidOperationException("A held world was cast into.");
            worldRuns = true;
            for (int i = 0; i < 400 && !heldDriver.Completed; i++) heldDriver.Update();
            if (heldRecord.Violations().Count != 0 || heldWorld.Fired.Count != 5)
                throw new InvalidOperationException("The run did not resume once the world ran: " +
                    string.Join("|", heldRecord.Violations().ToArray()));
            // Held in the middle of a step (after the complete step's first
            // cast): its next casting waits, then the run completes.
            var midWorld = new SimulatedBuffWorld();
            CastingQualificationAllowance midAllowance = ForecastAllowance(midWorld);
            var midRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            string midDir = Path.Combine(root, "qm");
            Directory.CreateDirectory(midDir);
            bool midRuns = true;
            CastingQualificationDriver midDriver = NewQualificationDriver(midDir, midWorld, midRecord,
                midAllowance, () => now, () => midRuns);
            for (int i = 0; i < 400 && midWorld.Fired.Count < 2; i++) midDriver.Update();
            midRuns = false;
            int heldBefore = midDriver.HeldUpdates;
            string heldPhase = midDriver.Phase;
            for (int i = 0; i < 30; i++) midDriver.Update();
            if (midWorld.Fired.Count != 2 || midDriver.HeldUpdates - heldBefore != 30 || midDriver.Completed ||
                heldPhase != "complete-wait")
                throw new InvalidOperationException("A run advanced while the world was held mid-step: fired=" +
                    midWorld.Fired.Count + ";phase=" + heldPhase);
            midRuns = true;
            for (int i = 0; i < 400 && !midDriver.Completed; i++) midDriver.Update();
            if (midRecord.Violations().Count != 0 || midWorld.Fired.Count != 5)
                throw new InvalidOperationException("The run did not resume after a mid-step hold: " +
                    string.Join("|", midRecord.Violations().ToArray()));
            // Review P3-2: the host deadline counts only running time. Held
            // mid-step for 200 s (over the 110 s host budget of the two
            // castings, inside the 240 s run deadline) the step still
            // completes; the same hold under a wall-clock host fails it.
            foreach (bool worldClock in new[] { true, false })
            {
                var longWorld = new SimulatedBuffWorld();
                CastingQualificationAllowance longAllowance = ForecastAllowance(longWorld);
                var longRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
                string longDir = Path.Combine(root, worldClock ? "ql-w" : "ql-c");
                Directory.CreateDirectory(longDir);
                long wall = 0, running = 0;
                bool longRuns = true;
                CastingQualificationDriver longDriver = NewQualificationDriver(longDir, longWorld, longRecord,
                    longAllowance, () => wall, () => longRuns, worldClock ? () => running : (Func<long>)(() => wall));
                for (int i = 0; i < 400 && longWorld.Fired.Count < 2; i++) { longDriver.Update(); wall += 16; running += 16; }
                longRuns = false;
                for (int i = 0; i < 20; i++) { wall += 10000; longDriver.Update(); }
                longRuns = true;
                for (int i = 0; i < 400 && !longDriver.Completed; i++) { longDriver.Update(); wall += 16; running += 16; }
                bool passed = longRecord.Violations().Count == 0 && longWorld.Fired.Count == 5;
                if (passed != worldClock)
                    throw new InvalidOperationException("A long hold " + (worldClock ? "failed a world-clock host: " :
                        "did not fail a wall-clock host: ") + string.Join("|", longRecord.Violations().ToArray()));
            }
            // Review RC2: a failed, missing, throwing or wrong-target
            // before-read ends the run before Apply: nothing is submitted.
            foreach (string fault in new[] { "throw", "failed", "null", "wrong-target" })
            {
                var faulty = new SimulatedBuffWorld { BeforeReadFault = fault };
                CastingQualificationAllowance faultyAllowance = ForecastAllowance(faulty);
                var faultyRecord = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
                string faultyDir = Path.Combine(root, "qb-" + fault.Substring(0, 4));
                Directory.CreateDirectory(faultyDir);
                CastingQualificationDriver faultyDriver = NewQualificationDriver(faultyDir, faulty,
                    faultyRecord, faultyAllowance, () => now);
                for (int i = 0; i < 400 && !faultyDriver.Completed; i++) faultyDriver.Update();
                string reason = fault == "throw" ? "failed:observer-exception:InvalidOperationException"
                    : fault == "failed" ? "failed:read-refused"
                    : fault == "null" ? "missing" : "wrong-target:unit-somebody-else";
                if (faulty.Fired.Count != 0 || faultyRecord.Submissions.Count != 0 ||
                    faultyRecord.TerminalReason != "failed:stop" ||
                    !faultyRecord.Failures.Contains("stop:before-read:qual-cast-1:" + reason))
                    throw new InvalidOperationException("A " + fault + " before-read did not refuse the step: " +
                        string.Join("|", faultyRecord.Failures.ToArray()) + " fired=" + faulty.Fired.Count);
            }
            // Review P0: a step that ends any other way than forecast stops
            // the run at once; nothing further is submitted.
            foreach (string shape in new[] { "unconfirmed", "spend-on-free" })
            {
                var world = new SimulatedBuffWorld();
                CastingQualificationAllowance allowance = ForecastAllowance(world);
                if (shape == "unconfirmed") world.UnconfirmedCasting = "qual-cast-1";
                else world.SpendOnFreeCasting = "qual-cast-1";
                var record = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
                string dir = Path.Combine(root, "qualification-halt-" + shape);
                Directory.CreateDirectory(dir);
                CastingQualificationDriver driver = NewQualificationDriver(dir, world, record, allowance, () => now);
                for (int i = 0; i < 400 && !driver.Completed; i++) driver.Update();
                CastingQualificationStepResult stop = record.Step("stop");
                if (!driver.Completed || !world.Fired.SequenceEqual(new[] { "qual-cast-1" }) ||
                    record.Step("complete") != null ||
                    record.TerminalReason != "failed:stop-wait" ||
                    !record.Failures.Any(value => value.StartsWith("stop-wait:step:", StringComparison.Ordinal)) ||
                    stop == null || stop.Report == null || !stop.Report.Halted)
                    throw new InvalidOperationException("A " + shape + " stop step did not end the run: fired=" +
                        string.Join(",", world.Fired.ToArray()) + " terminal=" + record.TerminalReason +
                        " failures=" + string.Join("|", record.Failures.ToArray()));
            }
        }

        // The classic cast scenarios: no-input workspace scenarios on the
        // automation fixture only; the classic allowance only on the cast
        // scenario; either casting mode. The host reads the allowance and
        // every before-state before it arms the single-use grant, presses
        // only after arming, never arms in a selection run, and judges only
        // a result the press itself produced.
        private static void TestClassicScenarioRequests(string root)
        {
            if (!RuntimeTestProtocol.IsClassicCastScenario("live-classic-select") ||
                !RuntimeTestProtocol.IsClassicCastScenario("live-classic-cast") ||
                RuntimeTestProtocol.IsCastingClassicScenario("live-classic-select") ||
                !RuntimeTestProtocol.IsCastingClassicScenario("live-classic-cast") ||
                !RuntimeTestProtocol.IsNoInputWorkspaceScenario("live-classic-cast") ||
                !RuntimeTestProtocol.IsWorkspaceScenario("live-classic-select") ||
                RuntimeTestProtocol.IsQualificationScenario("live-classic-cast") ||
                RuntimeTestProtocol.IsAdvancedFamilyScenario("live-classic-cast") ||
                RuntimeTestProtocol.IsProbeScenario("live-classic-cast") ||
                RuntimeTestProtocol.IsClassicCastScenario("live-cast-qual"))
                throw new InvalidOperationException("Classic scenario classification is wrong.");
            Func<string, string, string, string, Action<Dictionary<string, object>>> set =
                (scenario, family, extra, mode) => o =>
                {
                    o["scenario"] = scenario;
                    var parameters = new Dictionary<string, object>
                    {
                        { "workingSaveName", family + "_WORKING" },
                        { "workingFileName", "Manual_305_" + family + "_WORKING.zks" },
                        { "workingSha256", new string('a', 64) },
                        { "baselineSaveName", family + "_BASELINE" },
                        { "baselineFileName", "Manual_304_" + family + "_BASELINE.zks" },
                        { "baselineSha256", new string('b', 64) },
                        { "expectedGameName", "Hedwirg" },
                        { "expectedGameId", "df33d1ff-4ec8-4707-bfa0-5e059bf9a049" },
                        { "executionMode", mode }
                    };
                    if (extra == "classic") parameters["classicAllowance"] = "{}";
                    if (extra == "classic-number") parameters["classicAllowance"] = 1;
                    if (extra == "qualification") parameters["qualificationAllowance"] = "{}";
                    if (extra == "recipe") parameters["qualificationRecipe"] = "zero-cost-mixed";
                    o["parameters"] = parameters;
                };
            var accepted = new Dictionary<string, Action<Dictionary<string, object>>>
            {
                { "select-animated", set("live-classic-select", "KBP_AUTOMATION", null, "animated") },
                { "select-instant", set("live-classic-select", "KBP_AUTOMATION", null, "instant") },
                { "cast-animated", set("live-classic-cast", "KBP_AUTOMATION", "classic", "animated") },
                { "cast-instant", set("live-classic-cast", "KBP_AUTOMATION", "classic", "instant") },
                // Without the allowance the host refuses before any grant.
                { "cast-without-allowance", set("live-classic-cast", "KBP_AUTOMATION", null, "animated") }
            };
            var refused = new Dictionary<string, Action<Dictionary<string, object>>>
            {
                { "select-with-allowance", set("live-classic-select", "KBP_AUTOMATION", "classic", "animated") },
                { "allowance-not-string", set("live-classic-cast", "KBP_AUTOMATION", "classic-number", "animated") },
                { "advanced-select", set("live-classic-select", "KBP_ADVANCED", null, "animated") },
                { "advanced-cast", set("live-classic-cast", "KBP_ADVANCED", "classic", "animated") },
                { "qualification-allowance", set("live-classic-cast", "KBP_AUTOMATION", "qualification", "animated") },
                { "recipe", set("live-classic-select", "KBP_AUTOMATION", "recipe", "animated") },
                { "classic-on-qualification", set("live-cast-qual", "KBP_AUTOMATION", "classic", "instant") },
                { "classic-on-workspace", set("live-workspace-qual", "KBP_AUTOMATION", "classic", "instant") },
                { "unknown-mode", set("live-classic-cast", "KBP_AUTOMATION", "classic", "hybrid") }
            };
            string rejection;
            foreach (KeyValuePair<string, Action<Dictionary<string, object>>> item in accepted)
            {
                string path = WriteRequest(root, "classic-ok-" + item.Key, item.Value);
                if (ReadProtocol(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path },
                        out rejection) == null || rejection.Length != 0)
                    throw new InvalidOperationException("A valid classic request was refused: " +
                        item.Key + ":" + rejection);
            }
            foreach (KeyValuePair<string, Action<Dictionary<string, object>>> item in refused)
            {
                string path = WriteRequest(root, "classic-bad-" + item.Key, item.Value);
                if (ReadProtocol(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path },
                        out rejection) != null || string.IsNullOrEmpty(rejection))
                    throw new InvalidOperationException("An invalid classic request was accepted: " + item.Key);
            }
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null) throw new InvalidOperationException("Repository root was not discoverable.");
            string source = Path.Combine(directory.FullName, "src", "KingmakerBuffPlanner");
            string host = File.ReadAllText(Path.Combine(source, "RuntimeTesting", "RuntimeTestHost.cs"))
                .Replace("\r\n", "\n");
            string classic = SourceBlock(host, "private bool UpdateClassicCast()");
            string arming = classic == null ? null : SourceBlock(classic, "if (_classicStage == 2)");
            string authoring = classic == null ? null : SourceBlock(classic, "if (_classicStage == 1)");
            string judging = classic == null ? null : SourceBlock(classic, "if (_classicStage == 3)");
            if (arming == null || authoring == null || judging == null)
                throw new InvalidOperationException("The classic host stages were not found.");
            int allowanceRead = arming.IndexOf("ReadClassicAllowance();", StringComparison.Ordinal);
            int beforeRead = arming.IndexOf("new KingmakerProbeObserver().Observe(step, \"classic-before:\"", StringComparison.Ordinal);
            int poolsRead = arming.IndexOf("_classicPoolsBefore = ClassicFinitePools();", StringComparison.Ordinal);
            int arm = arming.IndexOf("UI.NativeCastingSessionPolicy.ArmClassicGrant(_classicGrant)", StringComparison.Ordinal);
            int quickBefore = arming.IndexOf("_classicQuickBefore = BuffPlannerUiRoot.QuickResultForRuntime(\"long\");", StringComparison.Ordinal);
            int press = arming.IndexOf("if (!BuffPlannerUiRoot.PressRoutineForRuntime(\"long\"))", StringComparison.Ordinal);
            if (allowanceRead < 0 || beforeRead < 0 || poolsRead < 0 || arm < 0 || quickBefore < 0 || press < 0 ||
                !(allowanceRead < beforeRead && beforeRead < poolsRead && poolsRead < arm && arm < quickBefore &&
                    quickBefore < press) ||
                !arming.Contains("if (allowance == null) return FailClassic(") ||
                !arming.Contains("before.AvailableForCast == null)") ||
                Occurrences(host, "ArmClassicGrant(") != 1 ||
                Occurrences(host, "PressRoutineForRuntime(\"long\")") != 1)
                throw new InvalidOperationException("The classic host arms or presses before its allowance and before-reads.");
            int selectReturn = authoring.IndexOf("if (!_classicRecord.CastingScenario)", StringComparison.Ordinal);
            if (selectReturn < 0 || authoring.Contains("ArmClassicGrant") || authoring.Contains("PressRoutineForRuntime") ||
                !SourceBlock(authoring, "if (!_classicRecord.CastingScenario)").Contains("return true;") ||
                !authoring.Contains("BuffPlannerUiRoot.CloseRuntimeSmoke();"))
                throw new InvalidOperationException("A classic selection run can reach the grant or the press.");
            if (!judging.Contains("ReferenceEquals(quick, _classicQuickBefore)") ||
                !judging.Contains("BuffPlannerUiRoot.IsExecutingForRuntime") ||
                !judging.Contains("RuntimeTestProtocol.ClassicRunDeadlineSeconds"))
                throw new InvalidOperationException("The classic host can judge a result its press did not produce.");
            // The classic scenario routes to its own phase before the
            // qualification routing, and its record survives a shutdown.
            int classicRoute = host.IndexOf("if (RuntimeTestProtocol.IsClassicCastScenario(_request.Scenario))\n" +
                "                    {\n                        // Classic: back to the Classic planner and its routes.\n" +
                "                        _liveUiPhase = 110;", StringComparison.Ordinal);
            int qualificationRoute = host.IndexOf("                        // Qualification: the production-path driver.",
                StringComparison.Ordinal);
            if (classicRoute < 0 || qualificationRoute < 0 || classicRoute > qualificationRoute ||
                !host.Contains("if (_liveUiPhase == 110)\n            {\n                return UpdateClassicCast();") ||
                !host.Contains("_classicRecord.Failures.Add(\"shutdown:\" + reason);"))
                throw new InvalidOperationException("The classic scenario is not routed or recorded as designed.");
            string policy = File.ReadAllText(Path.Combine(source, "UI", "NativeCastingSessionPolicy.cs"))
                .Replace("\r\n", "\n");
            string armBlock = SourceBlock(policy, "internal static bool ArmClassicGrant(");
            if (armBlock == null || !armBlock.Contains("if (!Locked || grant == null || ClassicGrant != null) return false;") ||
                Occurrences(policy, "ClassicGrant = ") != 1)
                throw new InvalidOperationException("The classic grant can be armed outside a locked session or twice.");
        }

        // The qualification scenarios: no-input workspace scenarios, the
        // allowance only on the casting one, instant only, automation only.
        private static void TestQualificationScenarioRequests(string root)
        {
            if (!RuntimeTestProtocol.IsQualificationScenario("live-cast-qual-select") ||
                !RuntimeTestProtocol.IsCastingQualificationScenario("live-cast-qual") ||
                RuntimeTestProtocol.IsCastingQualificationScenario("live-cast-qual-select") ||
                !RuntimeTestProtocol.IsNoInputWorkspaceScenario("live-cast-qual") ||
                !RuntimeTestProtocol.IsWorkspaceScenario("live-cast-qual-select") ||
                RuntimeTestProtocol.IsAdvancedFamilyScenario("live-cast-qual") ||
                RuntimeTestProtocol.IsProbeScenario("live-cast-qual"))
                throw new InvalidOperationException("Qualification scenario classification is wrong.");
            Func<string, string, bool, string, Action<Dictionary<string, object>>> set =
                (scenario, family, allowance, mode) => o =>
                {
                    o["scenario"] = scenario;
                    var parameters = new Dictionary<string, object>
                    {
                        { "workingSaveName", family + "_WORKING" },
                        { "workingFileName", "Manual_305_" + family + "_WORKING.zks" },
                        { "workingSha256", new string('a', 64) },
                        { "baselineSaveName", family + "_BASELINE" },
                        { "baselineFileName", "Manual_304_" + family + "_BASELINE.zks" },
                        { "baselineSha256", new string('b', 64) },
                        { "expectedGameName", "Hedwirg" },
                        { "expectedGameId", "df33d1ff-4ec8-4707-bfa0-5e059bf9a049" },
                        { "executionMode", mode }
                    };
                    if (allowance) parameters["qualificationAllowance"] = "{}";
                    o["parameters"] = parameters;
                    if (family == "KBP_ADVANCED") UseAdvancedProfile(o);
                };
            Func<Action<Dictionary<string, object>>, string, Action<Dictionary<string, object>>> recipe =
                (inner, name) => o =>
                {
                    inner(o);
                    ((Dictionary<string, object>)o["parameters"])["qualificationRecipe"] = name;
                };
            var accepted = new Dictionary<string, Action<Dictionary<string, object>>>
            {
                { "select", set("live-cast-qual-select", "KBP_AUTOMATION", false, "instant") },
                { "cast-without-allowance", set("live-cast-qual", "KBP_AUTOMATION", false, "instant") },
                { "cast-with-allowance", set("live-cast-qual", "KBP_AUTOMATION", true, "instant") },
                // The advanced copy: selection (non-casting), and a casting
                // qualification only with its run-bound allowance.
                { "advanced-select", set("live-cast-qual-select", "KBP_ADVANCED", false, "instant") },
                { "advanced-cast-with-allowance", set("live-cast-qual", "KBP_ADVANCED", true, "instant") },
                { "select-finite-recipe", recipe(set("live-cast-qual-select", "KBP_ADVANCED", false, "instant"),
                    "finite-direct-mixed") },
                { "cast-zero-cost-recipe", recipe(set("live-cast-qual", "KBP_AUTOMATION", true, "instant"),
                    "zero-cost-mixed") },
                // A casting run in the mode its allowance approves.
                { "cast-animated", set("live-cast-qual", "KBP_AUTOMATION", true, "animated") }
            };
            var refused = new Dictionary<string, Action<Dictionary<string, object>>>
            {
                { "select-with-allowance", set("live-cast-qual-select", "KBP_AUTOMATION", true, "instant") },
                { "select-animated", set("live-cast-qual-select", "KBP_AUTOMATION", false, "animated") },
                { "advanced-cast-without-allowance", set("live-cast-qual", "KBP_ADVANCED", false, "instant") },
                { "advanced-probe", set("live-cast-probe-select", "KBP_ADVANCED", false, "instant") },
                { "allowance-on-workspace", set("live-workspace-qual", "KBP_AUTOMATION", true, "instant") },
                { "unknown-recipe", recipe(set("live-cast-qual-select", "KBP_AUTOMATION", false, "instant"),
                    "improvised") },
                { "recipe-on-workspace", recipe(set("live-workspace-qual", "KBP_AUTOMATION", false, "instant"),
                    "zero-cost-mixed") }
            };
            string rejection;
            foreach (KeyValuePair<string, Action<Dictionary<string, object>>> item in accepted)
            {
                string path = WriteRequest(root, "qual-ok-" + item.Key, item.Value);
                if (ReadProtocol(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path },
                        out rejection) == null || rejection.Length != 0)
                    throw new InvalidOperationException("A valid qualification request was refused: " +
                        item.Key + ":" + rejection);
            }
            foreach (KeyValuePair<string, Action<Dictionary<string, object>>> item in refused)
            {
                string path = WriteRequest(root, "qual-bad-" + item.Key, item.Value);
                if (ReadProtocol(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path },
                        out rejection) != null || string.IsNullOrEmpty(rejection))
                    throw new InvalidOperationException("An invalid qualification request was accepted: " +
                        item.Key);
            }
        }
    }
}
