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
            Run("review-signature-skip-flip-is-harmless", TestReviewSignatureSkipFlipIsHarmless);
            Run("review-state-is-per-routine-and-restorable", TestReviewStatePerRoutine);
            Run("review-store-round-trip-and-refusals", () => TestReviewStore(root));
            Run("planner-mode-store-defaults-to-classic", () => TestPlannerModeStore(root));
            Run("session-save-preserves-player-settings",
                () => TestSessionSavePreservesSettings(root));
            Run("session-acceptance-survives-reopen",
                () => TestSessionAcceptanceSurvivesReopen(root));
            Run("session-apply-nothing-to-cast", () => TestSessionApplyNothingToCast(root));
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
            Run("capability-inventory-describes-the-party", TestCapabilityInventory);
            Run("qualification-finite-recipe", () => TestFiniteQualificationRecipe(root));
            Run("qualification-driver-refusals-and-deadline",
                () => TestQualificationDriverRefusalsAndDeadline(root));
            Run("qualification-scenario-requests", () => TestQualificationScenarioRequests(root));
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
            if (assess(plain, buff, new[] { instance(5000, null, 0) }).Verdict !=
                    ExistingEffectVerdict.Sufficient)
                throw new InvalidOperationException("An unknown caster level was treated as weaker.");
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
                On(unit, "group-effect", null, null, null);
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
                LiveEffects(On("unit-t1", "buff-effect", null, null, null)));
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
                ? LiveEffects(On("unit-t3", "buff-effect", null, null, null)) : null;
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
            int classicLock = classic.IndexOf("if (NativeCastingSessionPolicy.Locked)",
                StringComparison.Ordinal);
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
                LiveEffects(On("unit-t1", "buff-effect", null, null, null)));
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
                    "() => BuffPlannerUiRoot.WorldRunsForCasting, true,\n                    BuffPlannerUiRoot.PressRoutineForRuntime, BuffPlannerUiRoot.SetEnabled);") ||
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
                phase == "before" ? Obs(phase, clock, 7) : Obs(phase, clock, 7, effect);
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
                { "schemaVersion", 4 },
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
                { "authority", "owner mission 2026-09-23 section 4" }
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
                { "allowance-approval-missing", o => o["approvedBy"] = string.Empty }
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
                QualificationInputs(true, true, LiveEffects(On("unit-t1", "buff-effect", null, null, null))),
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
            if (shape != "stop=qual-cast-1,qual-cast-2,qual-cast-3;complete=qual-cast-2,qual-cast-3;recast=qual-cast-1;disable=qual-cast-1" ||
                steps.Any(step => step.ProjectionId == null) ||
                steps.Select(step => step.ProjectionId).Distinct().Count() != 3 ||
                steps[3].ProjectionId != steps[2].ProjectionId)
                throw new InvalidOperationException("The step forecast is wrong: " + shape);
            // The real state after the stop step (qual-cast-1 active) projects
            // exactly the forecast complete step.
            CastingPlanDocument document = CastingQualificationForecast.BuildDocument(
                "fixture-campaign", selection.Castings);
            CastingQualificationStepForecast real = CastingQualificationForecast.Project("real",
                document, inputs, LiveEffects(On("unit-t1", "buff-effect", 20, null, 0)));
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
            ActiveEffectSnapshot afterStop = LiveEffects(On("unit-t1", "buff-effect", 20, null, 0));
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
                return LiveEffects(Active.Select(pair => On(pair.Key, "buff-effect", 100, null, 0)).ToArray());
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
            public bool HasResidualDeliveryState { get { return false; } }
            public string Detail { get { return "simulated-animated;polls=" + _polls; } }
            public void Dispose()
            {
                if (_landed || !_world.LandOnInterrupt) return;
                _landed = true;
                _world.Land(_step);
            }
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
                return LiveEffects(Active.Select(pair => On(pair.Key, "buff-effect", 100, null, 0)).ToArray());
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
                    "pool-unit-wizard", 1, Primaries);
                var sorcerer = new ProviderSnapshot(new ProviderKey("unit-sorcerer", "book-sorcerer",
                    Ability, "level-2"), Ability.BaseAbilityGuid, 2,
                    "pool-unit-sorcerer", 1, null);
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
            Func<string, bool> pressRoutine = null, Action<bool> setPlannerEnabled = null)
        {
            host = host ?? QualificationHost(world, hostClock ?? clock);
            return new CastingQualificationDriver(record, allowance, "fixture-campaign",
                () => QualificationInputs(true, true, world.Live(), world.OtherUnits),
                boundary => new CastingWorkspaceSession(dir, "fixture-campaign", boundary),
                host, world.Observe, clock, 240000, null, worldRunning, ownerPumpsHost, pressRoutine,
                setPlannerEnabled);
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
                !world.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-3", "qual-cast-1" }) ||
                !world.AnimatedStarts.SequenceEqual(world.Fired.Concat(new[] { "qual-cast-1" })) ||
                record.Submissions.Count != 4 ||
                record.Submissions.Any(value => !value.EndsWith(";mode=animated", StringComparison.Ordinal)) ||
                record.Step("stop").Report.TerminalReason != "cancelled:" + CastingExecutionHost.PlayerStopReason ||
                record.DisabledAt != "in-flight" || !record.AcceptingAfterEnable ||
                record.Disable != "host-shutdown;at=in-flight;ended=True;accepting=True" ||
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
            if (stuckRecord.AcceptingAfterEnable ||
                !stuckRecord.Violations().SequenceEqual(new[] { "disable:not-resumed:" + stuckRecord.Disable }) ||
                stuckRecord.Disable != "planner-disable;at=in-flight;ended=True;accepting=False")
                throw new InvalidOperationException("A host that never resumed passed: " +
                    string.Join("|", stuckRecord.Violations().ToArray()));
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
            string resolve = SourceBlock(adapter, "private static AbilityData ResolveAbility(\n            UnitEntityData caster,\n            ProviderKey provider,\n            IReadOnlyList<string> reservedTokenIds)");
            int atWillAt = resolve == null ? -1 : resolve.IndexOf(
                "KingmakerAtWillCantrips.Resolve(caster, provider.Ability,", StringComparison.Ordinal);
            int memorizedAt = resolve == null ? -1 : resolve.IndexOf("book.GetAllMemorizedSpells()", StringComparison.Ordinal);
            if (atWillAt < 0 || memorizedAt < atWillAt ||
                !resolve.Contains("if (provider.SourceInstanceId == AtWillSourceInstance)") ||
                !adapter.Contains("internal const string AtWillSourceInstance = \"level-0|heighten-0\";"))
                throw new InvalidOperationException("A cantrip's level-0 entry is not cast through its at-will ability first.");
            string validate = SourceBlock(adapter, "internal CastRuntimeValidation ValidateSource(CastStep step,");
            if (validate == null || validate.Contains("resolved.Ability.IsAvailableForCast") ||
                !validate.Contains("if (!resolved.Ability.IsAvailable) return CastRuntimeValidation.Fail(\"ability-unavailable\");"))
                throw new InvalidOperationException("Validation is not the cast command's own availability guard.");
            string isAtWill = SourceBlock(atWill, "internal static bool IsAtWill(AbilityData data)");
            if (isAtWill == null || !isAtWill.Contains("data.Spellbook == null && data.IsAvailable &&") ||
                !isAtWill.Contains("data.GetAvailableForCastCount() < 0"))
                throw new InvalidOperationException("An ability is taken as at will without the game's own judgement.");
            string spontaneous = SourceBlock(builder, "private void ScanSpontaneousSpellbook(");
            string zero = SourceBlock(builder, "private void ScanSpontaneousLevelZero(");
            string prepared = SourceBlock(builder, "private void ScanPreparedSpellbook(");
            if (spontaneous == null || spontaneous.Contains("ResourcePoolKind.Unlimited") ||
                !spontaneous.Contains("ScanSpontaneousLevelZero(unit, spellbook, providers, pools);") ||
                zero == null || !zero.Contains("if (HasAtWillCantrip(unit, spellbook, selection))") ||
                !zero.Contains("ResourcePoolKind.SpontaneousLevel") ||
                prepared == null ||
                !prepared.Contains("!selections.All(selection => HasAtWillCantrip(unit, spellbook, selection)))") ||
                !prepared.Contains("var slots = allSlots.Where(s => !atWillSlots.Contains(s)).ToList();"))
                throw new InvalidOperationException("A level-0 entry is priced as free without an at-will ability.");
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
                    if (!enabled) host.Shutdown(CastingQualificationDriver.DisableReason);
                    else host.Resume();
                });
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
                host.Pump();
            }
            if (record.Violations().Count != 0 || record.TerminalReason != "completed" ||
                !presses.SequenceEqual(new[] { CastingQualificationRecipe.RoutineId }) ||
                record.StopPress != "routine-press;handled=True;inFlight=True;pending=player-stopped" ||
                record.Disable != "planner-disable;at=in-flight;ended=True;accepting=True" ||
                host.StartedRuns != 4 ||
                !world.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-3", "qual-cast-1" }))
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
            if (!world.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-3", "qual-cast-1" }) ||
                record.PlannedSubmissions != 3 + 2 + 1 + 1)
                throw new InvalidOperationException("Unexpected submissions: " +
                    string.Join(",", world.Fired.ToArray()) + "|" + record.PlannedSubmissions);
            // Instant casts are atomic: the disable lands before the run's
            // first step, nothing is submitted, and the host resumes.
            CastingOutcomeEntry disabled = record.Step("disable").Report.Entries
                .First(entry => entry.CastingId == "qual-cast-1");
            if (record.DisabledAt != "before-start" || !record.AcceptingAfterEnable ||
                record.Disable != "host-shutdown;at=before-start;ended=True;accepting=True" ||
                disabled.State != CastingOutcomeState.NotProcessed || disabled.Submitted ||
                record.Step("disable").Report.TerminalReason != "cancelled:" + CastingQualificationDriver.DisableReason)
                throw new InvalidOperationException("The instant disable step was not before the run started: " +
                    record.Disable + "|" + disabled.State + "|" + record.Step("disable").Report.TerminalReason);
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
                selectOnly.Forecast.Count != 4)
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
            if (smallRecord.Violations().Count != 0 || small.Fired.Count != 4 || smallRecord.Roster.Count != 3)
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
            if (heldRecord.Violations().Count != 0 || heldWorld.Fired.Count != 4)
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
            if (midRecord.Violations().Count != 0 || midWorld.Fired.Count != 4)
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
                bool passed = longRecord.Violations().Count == 0 && longWorld.Fired.Count == 4;
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
