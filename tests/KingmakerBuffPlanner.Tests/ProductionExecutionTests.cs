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
            Run("qualification-allowance-parsing", TestQualificationAllowanceParsing);
            Run("qualification-recipe-selection", TestQualificationRecipeSelection);
            Run("qualification-forecast-and-boundary", TestQualificationForecastAndBoundary);
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
            if (review.Present("long", long1))
                throw new InvalidOperationException("A harmless refresh cleared acceptance.");
            if (!review.Present("long", long2) ||
                review.TrySubmit("long", long2).Reason != "not-accepted" ||
                review.TrySubmit("long", long1).Allowed)
                throw new InvalidOperationException("New material kept an old acceptance.");
            var restored = new CastingReviewCoordinator();
            restored.RestoreAccepted("long", long1.Digest);
            if (!restored.TrySubmit("long", long1).Allowed ||
                restored.TrySubmit("long", long2).Reason != "material-change-requires-review")
                throw new InvalidOperationException("A restored acceptance matched the wrong contents.");
            if (restored.Present("long", long1) ||
                restored.StatusFor("long") != CastingReviewStatus.Accepted)
                throw new InvalidOperationException("Presenting the restored contents lost acceptance.");
            if (!restored.Present("long", long2) || restored.TrySubmit("long", long1).Allowed)
                throw new InvalidOperationException("Different contents did not clear a restored acceptance.");
            var invalid = new CastingReviewCoordinator();
            invalid.RestoreAccepted("long", long1.Digest.ToUpperInvariant());
            invalid.RestoreAccepted("short", "abc");
            if (invalid.TrySubmit("long", long1).Reason != "nothing-presented" ||
                invalid.AcceptedDigests.Count != 0)
                throw new InvalidOperationException("An invalid restored digest was accepted.");
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
            second.PresentForReview(inputs);
            var third = new CastingWorkspaceSession(dir, "workspace-campaign",
                new DisabledCastingDispatchBoundary());
            if (third.Apply(CastingApplyMode.Ordinary, "long", inputs).ReviewReason !=
                    "nothing-presented")
                throw new InvalidOperationException(
                    "Presenting changed material did not clear the stored acceptance.");
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
        }

        private static void TestRunPresentation()
        {
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
                "try { inputs = BuildFreshCastingWorkspaceInputs(); }"
            };
            foreach (string fragment in required)
                if (rootUi.Replace("\r\n", "\n").IndexOf(fragment.Replace("\r\n", "\n"),
                        StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("UI root wiring missing: " + fragment);
            if (rootUi.Contains("UIUtility.SendWarning") || rootUi.Contains("ExecuteLegacyRoutine"))
                throw new InvalidOperationException("A floating result or legacy route came back.");
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
                new KeyValuePair<string, string>("live-workspace-qual", "KBP_ADVANCED")
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

        // Two casters with verified-free pools casting the fixture buff by
        // rule, plus four other party members (optionally a finite pool or
        // a single caster).
        private static CastingWorkspaceInputs QualificationInputs(bool free, bool twoCasters,
            ActiveEffectSnapshot live)
        {
            string[] casters = twoCasters
                ? new[] { "unit-cleric", "unit-wizard" } : new[] { "unit-cleric" };
            string[] others = { "unit-t1", "unit-t2", "unit-t3", "unit-t4" };
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
                { "schemaVersion", 3 },
                { "kind", "kbp-casting-qualification" },
                { "runId", "qual-run-1" },
                { "sourceCommit", new string('c', 40) },
                { "packageSha256", new string('a', 64) },
                { "dllSha256", new string('b', 64) },
                { "assemblyMvid", "11111111-2222-3333-4444-555555555555" },
                { "fixtureGameId", "fixture-game" },
                { "recipe", "zero-cost-mixed" },
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
                valid.MaximumNativeSubmissions != 6 || valid.Recipe != "zero-cost-mixed")
                throw new InvalidOperationException("A valid qualification allowance was refused: " + refusal);
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
            if (shape != "stop=qual-cast-1,qual-cast-2,qual-cast-3;complete=qual-cast-2,qual-cast-3;recast=qual-cast-1" ||
                steps.Any(step => step.ProjectionId == null) ||
                steps.Select(step => step.ProjectionId).Distinct().Count() != 3)
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
            var boundary = new CastingQualificationBoundary(allowance, host, () => null);
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
        }
    }
}
