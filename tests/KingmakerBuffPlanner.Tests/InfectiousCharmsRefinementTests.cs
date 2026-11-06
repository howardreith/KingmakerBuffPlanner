using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // 0.6.11 review findings H1 (transient follow-up markers decided lasting
    // coverage) and H2 (planning-time nearby owners frozen across native
    // application). The session, compiler, gate, converter, executors,
    // coordinator and report are production; RepeatBuffWorld substitutes only
    // the native boundary (buff reads, spending, positions). No native PASS is
    // claimed from these simulations.
    internal static partial class Program
    {
        private const string CharmsCasterMarker = "6d949ebb548bd6c31e8d5bd45f96a7b6";
        private const string CharmsTargetMarker = "c20c575b6352d1692b3074a25232e699";

        // Feat present, native nearby selection, and the installed marker
        // lifetime (3 s: discovered as 6 s worst-case, gone by the next round).
        private static RepeatSession CharmsSession(string root, string mode, string name,
            Action<RepeatBuffWorld> configure = null, bool feat = true)
        {
            var run = new RepeatSession(root, mode, name, capturedHeroism: true, infectiousCharms: feat,
                nearbyRecipients: true, configure: world =>
                {
                    world.MarkerSeconds = 6f;
                    world.MarkerLifetime = 1;
                    world.NativeSelection = true;
                    world.PredictedOwners = new[] { "unit-t2" };
                    if (configure != null) configure(world);
                });
            return run;
        }

        private static string Outcome(RepeatSession run)
        {
            CastingRunReport report = run.Host.LastReport;
            return report == null ? "no-report" : "confirmed=" + report.Confirmed + ";skipped=" + report.Skipped +
                ";notProcessed=" + report.NotProcessed + ";spent=" + report.ResourcesSpent + ";fired=" +
                string.Join(",", run.World.State.Fired.ToArray()) + ";first=" +
                (report.Entries.Count == 0 ? "" : report.Entries[0].Detail);
        }

        private static int PreparedHeroism(RepeatSession run)
        {
            return run.World.State.Tokens.Values.Count(value => value);
        }

        private static void TestTransientMarkerCoverage(string root, string mode)
        {
            var run = CharmsSession(root, mode, "h1");
            RepeatAssert(run.Apply().Allowed, "H1 first admission");
            RepeatAssert(run.Host.LastReport.Confirmed == 2 && run.Host.LastReport.NotProcessed == 0,
                "Realistic short markers stopped Heroism or its successor: " + Outcome(run));
            int slots = PreparedHeroism(run);
            var live = run.World.Inputs().LiveEffects;
            RepeatAssert(live.GetInstances("unit-wizard").Any(value => value.EffectId == CharmsCasterMarker &&
                value.RemainingRounds < ExistingEffectSufficiency.MinimumRemainingRounds), "fixture lost the live caster marker");

            Action<string> skipped = label =>
            {
                run.World.State.Active.Remove("unit-t4"); // the distinct successor still has work
                // Fixture bookkeeping: only the successor's spontaneous pool is
                // refilled; Heroism's prepared slots never are.
                run.World.State.SpontaneousRemaining = 3;
                RepeatAssert(run.Apply().Allowed, label + " admission");
                RepeatAssert(run.Host.LastReport.Skipped == 1 && run.Host.LastReport.Confirmed == 1 &&
                    run.Host.LastReport.ResourcesSpent == 1 && PreparedHeroism(run) == slots,
                    label + ": adequate Heroism was recast or its successor stopped: " + Outcome(run));
            };
            skipped("before marker expiry");
            run.World.Round += 1;
            RepeatAssert(!run.World.Inputs().LiveEffects.GetInstances("unit-wizard").Any(value => value.EffectId == CharmsCasterMarker),
                "fixture marker did not expire");
            skipped("after marker expiry");
            // A later spell's follow-up state replaced the caster marker.
            run.World.ReplaceAdditional("unit-wizard", CharmsCasterMarker);
            skipped("after marker replacement");

            // Intentional recast still casts and spends.
            run.World.Round += 1;
            run.World.State.SpontaneousRemaining = 3;
            RepeatAssert(run.Apply(true).Allowed && run.Host.LastReport.Confirmed == 2 &&
                PreparedHeroism(run) == slots - 1, "intentional recast: " + Outcome(run));
            // An inadequate unchanged primary duration is still insufficient.
            run.World.State.Active["unit-t1"] = new KeyValuePair<string, long>(run.World.State.Active["unit-t1"].Key, run.World.Round + 1);
            run.World.State.Active.Remove("unit-t4");
            run.World.State.SpontaneousRemaining = 3;
            RepeatAssert(run.Apply().Allowed && run.Host.LastReport.Skipped == 0 && run.Host.LastReport.Confirmed == 2 &&
                PreparedHeroism(run) == slots - 2, "nearly expired primary was treated as coverage: " + Outcome(run));

            // Primary and required-marker confirmation negatives keep failing.
            int index = 0;
            foreach (string observation in new[] { "missing", "suppressed", "unreadable", "wrong-kind", "wrong-recipient",
                "caster-missing", "caster-wrong-owner", "marker-missing", "caster-unreadable" })
            {
                var negative = CharmsSession(root, mode, "h1n" + (++index), world => world.Observation = observation);
                RepeatAssert(negative.Apply(true).Allowed, observation + " admission");
                RepeatAssert(negative.Host.LastReport.Confirmed == 0 && negative.Host.LastReport.NotProcessed == 1 &&
                    negative.Host.LastReport.ResourcesSpent == 1 && negative.World.State.Fired.Count == 1,
                    observation + " falsely confirmed or continued: " + Outcome(negative));
            }
            // A missing or unadopted primary is never coverage, whatever the markers.
            var noPrimary = CharmsSession(root, mode, "h1-primary-missing", world => world.Observation = "missing");
            Assert(noPrimary.Apply(true).Allowed);
            noPrimary.World.Observation = "replace";
            RepeatAssert(noPrimary.Apply().Allowed && noPrimary.Host.LastReport.Skipped == 0 &&
                noPrimary.Host.LastReport.Confirmed == 2, "markers without the primary counted as coverage: " + Outcome(noPrimary));

            // Time-aware confirmation: markers seen while the cast resolved
            // confirm even though they expire before the executor's final read;
            // nothing seen is never assumed.
            var delayed = CharmsSession(root, mode, "h1-delayed", world => world.DelayFinalRead = true);
            RepeatAssert(delayed.Apply(true).Allowed && delayed.Host.LastReport.Confirmed == 2,
                "expired-before-final-read markers were judged never applied: " + Outcome(delayed));
            var unseen = CharmsSession(root, mode, "h1-unseen", world => { world.DelayFinalRead = true; world.RecordAtApplication = false; });
            RepeatAssert(unseen.Apply(true).Allowed && unseen.Host.LastReport.Confirmed == 0 &&
                unseen.Host.LastReport.NotProcessed == 1, "unobserved expired markers were assumed: " + Outcome(unseen));

            // Unknown lifetime stays a lasting obligation (the conservative,
            // pre-fix behaviour, retained only where the contract is unread).
            var unknown = CharmsSession(root, mode, "h1-unknown", world => world.MarkerSeconds = null);
            Assert(unknown.Apply().Allowed && unknown.Host.LastReport.Confirmed == 2);
            unknown.World.Round += 1;
            unknown.World.State.Active.Remove("unit-t4");
            RepeatAssert(unknown.Apply().Allowed && unknown.Host.LastReport.Skipped == 0,
                "an unread marker lifetime was assumed transient: " + Outcome(unknown));

            // Feat absent: ordinary first/skip unchanged.
            var absent = CharmsSession(root, mode, "h1-absent", feat: false);
            Assert(absent.Apply().Allowed && absent.Host.LastReport.Confirmed == 2);
            absent.World.Round += 1;
            absent.World.State.Active.Remove("unit-t4");
            RepeatAssert(absent.Apply().Allowed && absent.Host.LastReport.Skipped == 1 && absent.Host.LastReport.Confirmed == 1,
                "feat-absent skip regressed: " + Outcome(absent));
            Console.WriteLine("[INFECTIOUS-CHARMS-H1] mode=" + mode + ";before-expiry=skip;after-expiry=skip;replaced=skip;" +
                "recast=cast;short-primary=cast;negatives=9-unconfirmed;delayed-read=confirmed;unseen=unconfirmed;unknown-lifetime=cast;feat-absent=skip");
        }

        private static void TestChangingSecondaryRecipient(string root, string mode)
        {
            var stable = CharmsSession(root, mode, "h2-stable", world => world.ApplicationOwners = new[] { "unit-t2" });
            RepeatAssert(stable.Apply(true).Allowed && stable.Host.LastReport.Confirmed == 2, "stable owners: " + Outcome(stable));
            CastStep step = stable.Steps[0];
            RepeatAssert(new[] { "unit-sorcerer", "unit-t2", "unit-t3", "unit-t4", "unit-wizard" }.All(step.EffectObservationUnitIds.Contains),
                "authorized candidates were not observed from before submission: " + string.Join(",", step.EffectObservationUnitIds.ToArray()));
            EffectLeafExpression secondary = EffectApplicationLifetime.Leaves(step.ExpectedEffects).Single(leaf => leaf.SecondarySelection != null);
            RepeatAssert(secondary.RecipientUnitId == "unit-t2" && secondary.EffectId == CharmsTargetMarker &&
                !secondary.SecondarySelection.CandidateUnitIds.Contains("unit-t1"), "prediction/candidate separation lost");

            Func<string, Action<RepeatBuffWorld>, RepeatSession> confirmed = (label, configure) =>
            {
                var run = CharmsSession(root, mode, "h2-" + label, configure);
                WorkspaceApplyResult admitted = run.Apply(true);
                RepeatAssert(admitted.Allowed, label + " admission: " + admitted.ReviewReason);
                RepeatAssert(run.Host.LastReport.Confirmed == 2 && run.Host.LastReport.NotProcessed == 0 &&
                    run.Host.LastReport.ResourcesSpent == 2 && run.World.State.Fired.Count == 2,
                    label + ": legitimate native selection was not confirmed: " + Outcome(run));
                return run;
            };
            Action<string, Action<RepeatBuffWorld>> refused = (label, configure) =>
            {
                var run = CharmsSession(root, mode, "h2-" + label, configure);
                WorkspaceApplyResult admitted = run.Apply(true);
                RepeatAssert(admitted.Allowed, label + " admission: " + admitted.ReviewReason);
                RepeatAssert(run.Host.LastReport.Confirmed == 0 && run.Host.LastReport.NotProcessed == 1 &&
                    run.Host.LastReport.ResourcesSpent == 1 && run.World.State.Fired.Count == 1,
                    label + ": an unconfirmable secondary outcome was accepted: " + Outcome(run));
            };

            // A nearest at admission, B legitimately selected at application.
            confirmed("a-to-b", world =>
            {
                world.ApplicationOwners = new[] { "unit-t3" };
                world.ApplicationDistances["unit-t2"] = 8.5f; world.ApplicationDistances["unit-t3"] = 2.5f;
            });
            // Nothing predicted; B arrives before application.
            confirmed("empty-to-b", world =>
            {
                world.PredictedOwners = new string[0]; world.ApplicationOwners = new[] { "unit-t3" };
            });
            // A predicted; every eligible unit readably out of range when Heroism landed.
            confirmed("a-to-empty", world =>
            {
                world.ApplicationOwners = new string[0];
                foreach (string unit in new[] { "unit-sorcerer", "unit-wizard", "unit-t2", "unit-t3", "unit-t4" })
                    world.ApplicationDistances[unit] = 25f;
            });
            confirmed("none", world =>
            {
                world.PredictedOwners = new string[0]; world.ApplicationOwners = new string[0];
            });

            refused("out-of-range", world =>
            {
                world.ApplicationOwners = new[] { "unit-t3" }; world.ApplicationDistances["unit-t3"] = 20f;
            });
            refused("over-max", world => world.ApplicationOwners = new[] { "unit-t2", "unit-t3" });
            refused("wrong-owner", world => world.Observation = "marker-wrong-owner");
            refused("vanished", world => world.ApplicationOwners = new string[0]);
            refused("cand-unread", world => { world.ApplicationOwners = new[] { "unit-t3" }; world.UnreadableUnit = "unit-t3"; });
            refused("no-geometry", world => { world.ApplicationOwners = new[] { "unit-t3" }; world.NoGeometry = true; });
            // A stale marker on the predicted owner is not this attempt's selection.
            refused("stale", world =>
            {
                world.ApplicationOwners = new[] { "unit-t3" }; world.ApplicationDistances["unit-t3"] = 20f;
                world.ReplaceAdditional("unit-t2", CharmsTargetMarker);
            });

            // Feature absent: no secondary obligation at all.
            var absent = CharmsSession(root, mode, "h2-absent", world => world.ApplicationOwners = new[] { "unit-t3" }, feat: false);
            RepeatAssert(absent.Apply(true).Allowed && absent.Host.LastReport.Confirmed == 2, "feat absent: " + Outcome(absent));

            // Authorization: what confirmation may accept is projection identity.
            Func<EffectExpression, string> identity = expected => ExplicitCastingStepConverter.Identity(new[] { new CastStep(step.SourceId,
                step.AssignmentId, step.Provider, step.AnchorUnitId, step.TargetUnitIds, step.ExpectedRecipientUnitIds, step.Reservation,
                step.MaterialReservation, expected, false, step.ExecutionStrategy, step.ExecutionStrategyReason, exactEnhancements: true) },
                ExplicitProjectionScope.Standard);
            Func<IEnumerable<string>, float?, EffectExpression> bound = (candidates, seconds) => EffectRecipientBinding.Bind(
                EffectConditionBinding.Bind(CapturedHeroismExpression(true, seconds), new Dictionary<string, bool> { { "infectious-charms-feature", true } }),
                rule => new NearbyRecipientSelection(new[] { "unit-t2" }, candidates));
            string original = identity(bound(new[] { "unit-t3", "unit-wizard" }, 6f));
            RepeatAssert(original == identity(bound(new[] { "unit-wizard", "unit-t3" }, 6f)), "candidate order changed identity");
            RepeatAssert(original != identity(bound(new[] { "unit-t3", "unit-wizard", "unit-t4" }, 6f)), "a widened candidate set reused the identity");
            RepeatAssert(original != identity(bound(new[] { "unit-t3", "unit-wizard" }, null)), "lifetime classification reused the identity");
            RepeatAssert(original != identity(EffectRecipientBinding.Bind(EffectConditionBinding.Bind(CapturedHeroismExpression(true, 6f),
                new Dictionary<string, bool> { { "infectious-charms-feature", true } }), rule => new[] { "unit-t2" })),
                "prediction-only binding reused a native-selection identity");
            Console.WriteLine("[INFECTIOUS-CHARMS-H2] mode=" + mode + ";stable=confirmed;a-to-b=confirmed;empty-to-b=confirmed;" +
                "a-to-empty(out-of-range)=confirmed;out-of-range/over-maximum/wrong-owner/vanished/unreadable/no-geometry/stale=refused;feat-absent=confirmed");
        }

        // Exact installed native field contract and the production reader.
        private static void TestNativeTransientLifetimeContract()
        {
            var native = typeof(Kingmaker.Blueprints.BlueprintScriptableObject).Assembly;
            MethodInfo read = ViewInputProduct().GetType("KingmakerBuffPlanner.GameAdapters.KingmakerActionGraphAdapter", true)
                .GetMethod("MaximumDurationSeconds", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            object apply = DetachedViewObject(native.GetType("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff", true));
            Func<float?> lifetime = () => (float?)read.Invoke(null, new[] { apply });
            // Installed Call of the Wild Infectious Charms: UseDurationSeconds, 3 s.
            SetNative(apply, "UseDurationSeconds", true); SetNative(apply, "DurationSeconds", 3f);
            RepeatAssert(lifetime() == 6f, "seconds lifetime: " + lifetime());
            SetNative(apply, "Permanent", true);
            RepeatAssert(lifetime() == null, "permanent was fixed");
            SetNative(apply, "Permanent", false); SetNative(apply, "UseDurationSeconds", false);
            RepeatAssert(lifetime() == null, "missing duration value was fixed");
            Type durationType = native.GetType("Kingmaker.UnitLogic.Mechanics.ContextDurationValue", true);
            Type valueType = native.GetType("Kingmaker.UnitLogic.Mechanics.ContextValue", true);
            object duration = DetachedViewObject(durationType), bonus = DetachedViewObject(valueType), dice = DetachedViewObject(valueType);
            SetNative(apply, "DurationValue", duration); SetNative(duration, "BonusValue", bonus); SetNative(duration, "DiceCountValue", dice);
            SetNative(bonus, "Value", 1);
            RepeatAssert(lifetime() == 12f, "one constant round: " + lifetime()); // 2 rounds worst case: durable
            SetNative(duration, "Rate", Enum.Parse(durationType.GetField("Rate").FieldType, "Hours"));
            RepeatAssert(lifetime() == 7200f, "one constant hour: " + lifetime());
            SetNative(bonus, "ValueType", Enum.Parse(valueType.GetField("ValueType").FieldType, "Rank"));
            RepeatAssert(lifetime() == null, "a rank-dependent duration was fixed");
            SetNative(bonus, "ValueType", Enum.Parse(valueType.GetField("ValueType").FieldType, "Simple"));
            SetNative(duration, "DiceType", Enum.Parse(durationType.GetField("DiceType").FieldType, "D4"));
            SetNative(dice, "Value", 1);
            RepeatAssert(lifetime() == null, "a dice duration was fixed");

            RepeatAssert(EffectApplicationLifetime.MinimumCoverageSeconds == ExistingEffectSufficiency.MinimumRemainingRounds * 6,
                "transient threshold drifted from the sufficiency threshold");
            var marker = new EffectLeafExpression(EffectKind.Buff, CharmsCasterMarker, EffectTarget.Caster, "ContextActionApplyBuff", "p", null, 6f);
            var round = new EffectLeafExpression(EffectKind.Buff, CharmsCasterMarker, EffectTarget.Caster, "ContextActionApplyBuff", "p", null, 12f);
            RepeatAssert(EffectApplicationLifetime.IsTransient(marker) && !EffectApplicationLifetime.IsTransient(round) &&
                !EffectApplicationLifetime.IsTransient(new EffectLeafExpression(EffectKind.Buff, "x", EffectTarget.CurrentTarget, "c", "p")),
                "transient classification");
            // Discovery carries the lifetime; catalogue identity ignores it.
            RepeatAssert(EffectAggregateIdentity.For(CapturedHeroismExpression(true, 6f), "") ==
                EffectAggregateIdentity.For(CapturedHeroismExpression(true), ""), "lifetime metadata changed catalogue identity");
            RepeatAssert(EffectApplicationLifetime.Leaves(CapturedHeroismExpression(true, 6f)).Count(EffectApplicationLifetime.IsTransient) == 2,
                "scanner dropped discovered lifetimes");
            // Every leaf transient: never lasting coverage.
            var assessment = ExistingEffectSufficiency.AssessScoped("unit-wizard", "unit-wizard", marker,
                unit => new[] { new ActiveEffectInstance(EffectKind.Buff, CharmsCasterMarker, 0.5, 10, 0, false) }, null,
                new ExistingEffectRequirement(10, 0, 100, true));
            RepeatAssert(assessment.Verdict == ExistingEffectVerdict.NotPresent &&
                assessment.Reasons.Contains(ExistingEffectSufficiency.NoLastingCoverage), "a transient-only casting was covered");
        }
    }
}
