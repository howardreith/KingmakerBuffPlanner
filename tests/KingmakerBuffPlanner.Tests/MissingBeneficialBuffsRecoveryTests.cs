using System;
using System.Collections.Generic;
using System.Linq;
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

namespace KingmakerBuffPlanner.Tests
{
    // Missing-beneficial-buffs recovery (owner instruction of 2026-11-22,
    // direct implementation after review round 12). Every finding left open
    // by round 12 gets a reproduction here that runs through the production
    // discovery, admission, targeting, planning, forecast, serialization and
    // audit paths. The file compiles against the round-12 candidate
    // (fb075cb) as well, so the same reproductions were executed there first
    // (planning/MISSING-BENEFICIAL-BUFFS-RECOVERY-LEDGER.md records which
    // failed on the asserted behavior). Each case names its kind: "native"
    // (the unchanged committed 2.1.7b blueprint), "unread" (an injected live
    // read that fails) or "modified" (a deliberately altered blueprint: a
    // robustness test, not an observation of the installed game).
    internal static partial class Program
    {
        private static readonly KeyValuePair<string, Action>[] MbbRecoveryRegressions =
        {
            Mbb("missing-beneficial-buffs-recovery-r8f1-invalidated-firebrand-evidence-stays-invalidated",
                TestMbrInvalidatedEvidence),
            Mbb("missing-beneficial-buffs-recovery-r10f1-possible-reapplication-keeps-a-removal",
                TestMbrPossibleReapplication),
            Mbb("missing-beneficial-buffs-recovery-r9f5-unread-distance-is-not-measured-exclusion",
                TestMbrUnreadDistance),
            Mbb("missing-beneficial-buffs-recovery-r9f4-classic-branch-uses-the-known-mutable-gate",
                TestMbrMutableGate),
            Mbb("missing-beneficial-buffs-recovery-r12f2-ready-row-reassessed-when-beneficiaries-narrow",
                TestMbrReadyRowReassessed),
            Mbb("missing-beneficial-buffs-recovery-r12f3-banned-caster-cannot-veto-a-skip",
                TestMbrBannedCasterSkip),
            Mbb("missing-beneficial-buffs-recovery-r11f2-classic-compares-best-single-cast-centres",
                TestMbrProviderCentres),
            Mbb("missing-beneficial-buffs-recovery-r11f1-nested-on-cast-reference-needs-proof",
                TestMbrNestedOnCastReference),
            Mbb("missing-beneficial-buffs-recovery-r12f4-good-hope-selector-condition-is-honoured",
                TestMbrGoodHopeSelectorCondition),
            Mbb("missing-beneficial-buffs-recovery-r12f5-imposed-daze-is-not-hidden-by-a-benefit",
                TestMbrImposedDaze),
            Mbb("missing-beneficial-buffs-recovery-r12f6-unmodelled-automatic-removal-is-refused",
                TestMbrAutomaticRemoval)
        };

        private static void RunMissingBeneficialBuffsRecoveryTests()
        {
            foreach (KeyValuePair<string, Action> regression in MbbRecoveryRegressions)
                Run(regression.Key, regression.Value);
        }

        private static string MbrDescribe(ResolvedCasting casting)
        {
            return casting == null ? "missing" : casting.CastingId + "=" + casting.Readiness + " cost=" +
                casting.Cost.Sum(c => c.Units) + " [" + string.Join(",", casting.ReadinessReasons) + "] ben=[" +
                string.Join(",", casting.PredictedBeneficiaryUnitIds) + "]";
        }

        private static string MbrDescribe(CastPlan plan)
        {
            return "steps=" + plan.Steps.Count + " [" + string.Join(",", plan.Steps.Select(s =>
                    s.Provider.CasterUnitId + "@" + s.AnchorUnitId + "->" + string.Join("+", s.ExpectedRecipientUnitIds))) +
                "] outcomes=[" + string.Join(",", plan.Outcomes.Select(o => o.AssignmentId + ":" + o.UnitId + "=" +
                    o.Kind + ":" + o.Reason)) + "]";
        }

        // A provider's option from the production targeting planner with an
        // injected per-pair distance read (null: the live read failed) and
        // injected relation/fact reads.
        private static ProviderPlanningOption MbrOption(NativeBlueprintDump dump, ProviderSnapshot provider,
            EffectExpression expression, List<UnitSnapshot> units, Func<string, bool> canTarget,
            Func<string, string, float?> distance, Func<string, string, bool?> reads)
        {
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(provider.Key.Ability.BaseAbilityGuid);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            var casterFacts = EffectConditionBinding.FactIds(expression)
                .ToDictionary(id => id, id => false, StringComparer.Ordinal);
            ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units, true,
                false, area, canTarget, distance, casterFacts, reads, fact =>
                {
                    if (fact == MbbEnemyOfCaster || fact == MbbAllyOfCaster) return true;
                    string type = NativeBlueprintDump.RecordedType(fact);
                    return type != null && !type.StartsWith("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff",
                        StringComparison.Ordinal);
                });
            return ProviderTargetingPlanner.BuildOption(provider, decision, CastExecutionStrategy.DirectRuleCast,
                "ordinary-direct-rule-cast", casterFacts);
        }

        // Positions on a line (metres); one unordered pair's live distance
        // read fails.
        private static Func<string, string, float?> MbrDistance(IDictionary<string, float> positions,
            string unreadA = null, string unreadB = null)
        {
            return (from, to) =>
            {
                if (unreadA != null && ((from == unreadA && to == unreadB) || (from == unreadB && to == unreadA)))
                    return null;
                return positions.ContainsKey(from) && positions.ContainsKey(to)
                    ? Math.Abs(positions[from] - positions[to]) : (float?)null;
            };
        }

        private static ActiveEffectInstance MbrFirebrand(string buff, string caster, int metamagic)
        {
            return new ActiveEffectInstance(EffectKind.Buff, buff, 5000, 20, metamagic, false, true, caster);
        }

        // R8-F1 (native Firebrand; explicit planner). a's sufficient Extended
        // Firebrand is active on everyone. In one routine b casts over that
        // disk, then a casts without Extend, then a's Extended request with
        // skip-if-active follows: it must cast (one slot, one rod use, an
        // executable step). Controls: a genuinely sufficient (Extended)
        // intervening cast by a keeps the skip; a measured disjoint b disk and
        // a disabled b cast keep it. Neighbor: with b disabled, a's own
        // weaker always-recast also voids the opening Extended instance.
        private static void TestMbrInvalidatedEvidence()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            AbilityKey key = MbbSpellKey(firebrand);
            var rod = new CastEnhancementSnapshot("rod-extend", "a", "rod-extend-blueprint", "Extend rod", string.Empty,
                CastEnhancementCategory.MetamagicRod, 8, 9, 3, new string[0], null, null, "rod-extend-pool");
            Func<AuthoredEnhancementSelection[]> extend = () =>
                new[] { new AuthoredEnhancementSelection("rod-extend", true, null) };
            Func<Dictionary<string, float>, string[], CastingAuthoringState, bool, bool, ExplicitCastingPlan> compile =
                (positions, bMembers, bState, middleExtended, middleRecasts) =>
                {
                    MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 4 }, { "b", 3 } },
                        new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                        positions);
                    var live = ActiveEffectSnapshot.FromInstances(positions.Keys.ToDictionary(unit => unit,
                        unit => (IEnumerable<ActiveEffectInstance>)new[] { MbrFirebrand(firebrandBuff, "a", 8) },
                        StringComparer.Ordinal));
                    var middle = new PlannedCasting("a-middle", "long", 0, key.Canonical, key, "a", null,
                        CastingTargetMode.CasterCenteredOrigin, null, CastingOrigin.CasterCentered(),
                        new[] { "a", "ally" }, null, middleExtended ? extend() : null,
                        middleRecasts ? ExistingEffectPolicy.Overwrite : ExistingEffectPolicy.SkipAlreadyActive,
                        null, CastingAuthoringState.Ready, null);
                    return new ExplicitCastingCompiler().Compile(CastingDocument(
                            GroupCasting("b-cast", "long", "b", key.Canonical, key, bMembers, null,
                                CastingTargetMode.CasterCenteredOrigin, null, bState),
                            middle,
                            GroupCasting("a-extended", "long", "a", key.Canonical, key, new[] { "a", "ally" }, extend())),
                        party.Snapshot, party.Options, party.Effects, new[] { rod }, "long", null, false, live);
                };
            var overlap = new Dictionary<string, float>(StringComparer.Ordinal) { { "a", 0f }, { "b", 1f }, { "ally", 2f } };
            var disjoint = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "a", 0f }, { "ally", 1f }, { "b", 100f }, { "ally2", 101f }
            };
            MbbSubcases(
                MbbCase("native: weaker replacement after another caster", () =>
                {
                    ExplicitCastingPlan plan = compile(overlap, new[] { "b", "ally" }, CastingAuthoringState.Ready,
                        false, false);
                    ResolvedCasting b = plan.CastingById("b-cast"), middle = plan.CastingById("a-middle"),
                        last = plan.CastingById("a-extended");
                    MbbExpect(b.Readiness == ResolvedCastingReadiness.Ready && middle.Readiness ==
                        ResolvedCastingReadiness.Ready && middle.Cost.Sum(c => c.Units) == 1,
                        "The intervening casts are not the reported shape: " + MbrDescribe(b) + " / " +
                        MbrDescribe(middle));
                    MbbExpect(last.Readiness == ResolvedCastingReadiness.Ready &&
                        last.Cost.Any(c => c.PoolKey == "rod-extend-pool" && c.Units == 1) &&
                        last.Cost.Any(c => c.PoolKey != "rod-extend-pool" && c.Units == 1),
                        "a's Extended Firebrand reused the invalidated opening skip: " + MbrDescribe(last));
                    CastingApplyDecision gate = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
                    MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", 4 }, { "b", 3 } },
                        new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                        overlap);
                    ExplicitStepConversion steps = ExplicitCastingStepConverter.Convert(plan, gate, party.Options,
                        party.Effects);
                    MbbExpect(steps.Converted && steps.Plan.Steps.Count == 3,
                        "The Extended request has no executable step: " + steps.Refusal + " " +
                        (steps.Converted ? steps.Plan.Steps.Count.ToString() : "-"));
                }),
                MbbCase("native control: sufficient Extended replacement", () =>
                {
                    ResolvedCasting last = compile(overlap, new[] { "b", "ally" }, CastingAuthoringState.Ready,
                        true, false).CastingById("a-extended");
                    MbbExpect(last.Readiness == ResolvedCastingReadiness.AlreadySatisfied && last.Cost.Count == 0,
                        "a's genuinely sufficient Extended replacement no longer satisfies the request: " +
                        MbrDescribe(last));
                }),
                MbbCase("native control: measured disjoint disk", () =>
                {
                    ResolvedCasting last = compile(disjoint, new[] { "b", "ally2" }, CastingAuthoringState.Ready,
                        false, false).CastingById("a-extended");
                    MbbExpect(last.Readiness == ResolvedCastingReadiness.AlreadySatisfied && last.Cost.Count == 0,
                        "b's measured disjoint cast cost a's skip: " + MbrDescribe(last));
                }),
                MbbCase("native control: b disabled", () =>
                {
                    ResolvedCasting last = compile(overlap, new[] { "b", "ally" }, CastingAuthoringState.Disabled,
                        false, false).CastingById("a-extended");
                    MbbExpect(last.Readiness == ResolvedCastingReadiness.AlreadySatisfied && last.Cost.Count == 0,
                        "Without b's cast a's Extended skip no longer holds: " + MbrDescribe(last));
                }),
                MbbCase("native neighbor: own weaker recast", () =>
                {
                    ResolvedCasting last = compile(overlap, new[] { "b", "ally" }, CastingAuthoringState.Disabled,
                        false, true).CastingById("a-extended");
                    MbbExpect(last.Readiness == ResolvedCastingReadiness.Ready && last.Cost.Sum(c => c.Units) == 2,
                        "a's own weaker recast left the opening Extended instance as evidence: " + MbrDescribe(last));
                }));
        }

        // A synthetic direct-target remover of one effect (with a harmless
        // synthetic application so it has something to cast for).
        private static KeyValuePair<AbilityKey, EffectExpression> MbrRemover(string removedEffect)
        {
            const string marker = "e2000000000000000000000000000001";
            var key = new AbilityKey("b2000000000000000000000000000001", string.Empty, 0, SourceKind.Spellbook, string.Empty);
            return new KeyValuePair<AbilityKey, EffectExpression>(key, new SequenceEffectExpression(new EffectExpression[]
            {
                new EffectLeafExpression(EffectKind.Buff, marker, EffectTarget.CurrentTarget, "fixture", "fixture:" + marker),
                new EmptyEffectExpression("restorative-action:ContextActionRemoveBuff",
                    new EffectRemoval(removedEffect, EffectTarget.CurrentTarget))
            }));
        }

        private sealed class MbrFeatherParty
        {
            internal PartyProviderSnapshot Snapshot;
            internal List<ProviderPlanningOption> Options;
            internal Dictionary<string, EffectExpression> Effects;
            internal AbilityKey Feather;
            internal AbilityKey Remover;
        }

        // Native Feather Step, Mass known by w and v; a synthetic remover
        // known by c. m is at 0, k at 4 m, j at 8 m (15-foot disk: k reaches
        // m and j; m does not reach j). w's relation to m is unread unless
        // confirmed.
        private static MbrFeatherParty MbrFeather(bool confirmed)
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            var dump = new NativeBlueprintDump(feather, featherBuff);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "m", 0f }, { "k", 4f }, { "j", 8f }, { "w", 50f }, { "v", 60f }, { "c", 70f }
            };
            List<UnitSnapshot> units = MbbUnits(positions.Keys);
            PartyProviderAdmission admission = MbbAdmission(dump);
            var providers = new List<ProviderSnapshot>();
            var pools = new List<ResourcePoolSnapshot>();
            foreach (string caster in new[] { "w", "v" })
            {
                ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool(caster, "book-" + caster, 3, 3, 3);
                pools.Add(pool);
                admission.AdmitSpell(caster, "book-" + caster, false, "Book",
                    MbbRead(dump, feather, "Feather Step, Mass", SourceKind.Spellbook, 9), 3, 0, pool.PoolKey, 1,
                    new string[0], providers);
            }
            var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
            KeyValuePair<AbilityKey, EffectExpression> remover = MbrRemover(featherBuff);
            effects[remover.Key.Canonical] = remover.Value;
            var removerPool = new ResourcePoolSnapshot("c|slots", ResourcePoolKind.SpontaneousLevel, 9, 9, null);
            pools.Add(removerPool);
            var removerProvider = new ProviderSnapshot(new ProviderKey("c", "book-c", remover.Key, "level-1|heighten-0"),
                "remover", 1, removerPool.PoolKey, 1, null, null, 5, 50, "fixture", "fixture", "Book", 0, "Book");
            var options = providers.Select(provider => MbrOption(dump, provider, effects[provider.Key.Ability.Canonical],
                units, id => id == (provider.Key.CasterUnitId == "w" ? "k" : "m"), MbrDistance(positions),
                (unit, fact) =>
                {
                    bool unread = !confirmed && provider.Key.CasterUnitId == "w" && unit == "m";
                    if (fact == MbbAllyOfCaster) return unread ? (bool?)null : true;
                    if (fact == MbbEnemyOfCaster) return unread ? (bool?)null : false;
                    return false;
                })).ToList();
            options.Add(new ProviderPlanningOption(removerProvider, new[] { "m" }, new[] { "m" }, 5, 50,
                CastExecutionStrategy.DirectRuleCast, "direct"));
            providers.Add(removerProvider);
            return new MbrFeatherParty
            {
                Snapshot = new PartyProviderSnapshot(units, providers, pools),
                Options = options,
                Effects = effects,
                Feather = providers.First(p => p.Key.Ability.BaseAbilityGuid == feather).Key.Ability,
                Remover = remover.Key
            };
        }

        // R10-F1 (native Feather Step, Mass with an injected unread relation;
        // the remover is synthetic). Feather Step is active on m (and k). A
        // funded cast removes it from m; w's funded Mass Feather Step on the
        // known ally k may reach m, whose relation to w is unread; a later
        // request covering m must not regain the opening free skip - in the
        // explicit planner and in Classic. Control: with the relation read as
        // allied the reapplication is confirmed and the skip holds.
        private static void TestMbrPossibleReapplication()
        {
            string featherBuff = MbbPointGroupSpells[1][2];
            Func<bool, ExplicitCastingPlan> compile = confirmed =>
            {
                MbrFeatherParty party = MbrFeather(confirmed);
                var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "m", new[] { new ActiveEffectInstance(EffectKind.Buff, featherBuff, 5000, 20, 0) } },
                    { "k", new[] { new ActiveEffectInstance(EffectKind.Buff, featherBuff, 5000, 20, 0) } }
                });
                return new ExplicitCastingCompiler().Compile(CastingDocument(
                        DirectCasting("remove", "long", "c", "m", party.Remover.Canonical, party.Remover),
                        GroupCasting("mass", "long", "w", party.Feather.Canonical, party.Feather, new[] { "j" }, null,
                            CastingTargetMode.AnchoredOrigin, "k"),
                        GroupCasting("later", "long", "v", party.Feather.Canonical, party.Feather, new[] { "m" }, null,
                            CastingTargetMode.AnchoredOrigin, "m")),
                    party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], "long", null, false,
                    live);
            };
            Func<bool, RoutinePlanResult> classic = confirmed =>
            {
                MbrFeatherParty party = MbrFeather(confirmed);
                var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "m", new[] { new ActiveEffectInstance(EffectKind.Buff, featherBuff, 5000, 20, 0) } },
                    { "k", new[] { new ActiveEffectInstance(EffectKind.Buff, featherBuff, 5000, 20, 0) } }
                });
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbr-possible-reapplication");
                var routine = profile.Routines.First(rt => rt.RoutineId == "long");
                SourceAssignmentProfile remove = Assignment(party.Remover.Canonical, party.Remover, new[] { "m" });
                remove.CastingAssignments[0].CasterUnitId = "c";
                SourceAssignmentProfile mass = Assignment(party.Feather.Canonical, party.Feather, new[] { "j" });
                mass.CastingAssignments[0].CasterUnitId = "w";
                mass.CastingAssignments[0].Order = 1;
                SourceAssignmentProfile later = Assignment(party.Feather.Canonical, party.Feather, new[] { "m" });
                later.CastingAssignments[0].CasterUnitId = "v";
                later.CastingAssignments[0].AssignmentId = "later";
                later.CastingAssignments[0].Order = 2;
                routine.Assignments.Add(remove);
                routine.Assignments.Add(mass);
                routine.Assignments.Add(later);
                return new RoutinePlanService().Plan(profile, "long", party.Snapshot, live, party.Effects,
                    party.Options, new CastEnhancementSnapshot[0]);
            };
            MbbSubcases(
                MbbCase("unread: explicit", () =>
                {
                    ExplicitCastingPlan plan = compile(false);
                    ResolvedCasting mass = plan.CastingById("mass"), later = plan.CastingById("later");
                    MbbExpect(plan.CastingById("remove").Readiness == ResolvedCastingReadiness.Ready &&
                        mass.Readiness == ResolvedCastingReadiness.Ready && !mass.PredictedBeneficiaryUnitIds.Contains("m"),
                        "The removal and possible reapplication are not the reported shape: " +
                        MbrDescribe(plan.CastingById("remove")) + " / " + MbrDescribe(mass));
                    MbbExpect(later.Readiness != ResolvedCastingReadiness.AlreadySatisfied &&
                        later.ReadinessReasons.Any(r => r.StartsWith(
                            ExplicitCastingCompiler.EffectRemovedByEarlierCasting + ":remove:", StringComparison.Ordinal)),
                        "A possible reapplication restored the removed opening skip: " + MbrDescribe(later));
                }),
                MbbCase("unread: Classic", () =>
                {
                    RoutinePlanResult result = classic(false);
                    TargetPlanOutcome later = result.Plan.Outcomes.FirstOrDefault(o => o.AssignmentId == "later");
                    MbbExpect(later != null && later.Kind != TargetOutcomeKind.SkippedAlreadyActive,
                        "Classic restored the removed opening skip on a possible reapplication: " + MbrDescribe(result.Plan));
                }),
                MbbCase("control: confirmed reapplication (explicit)", () =>
                {
                    ResolvedCasting later = compile(true).CastingById("later");
                    MbbExpect(later.Readiness == ResolvedCastingReadiness.AlreadySatisfied && later.Cost.Count == 0,
                        "A confirmed reapplication no longer restores the skip: " + MbrDescribe(later));
                }),
                MbbCase("control: confirmed reapplication (Classic)", () =>
                {
                    RoutinePlanResult result = classic(true);
                    MbbExpect(result.Plan.Outcomes.Any(o => o.AssignmentId == "later" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "Classic no longer accepts a confirmed reapplication: " + MbrDescribe(result.Plan));
                }));
        }

        // R9-F5 (native Firebrand with an injected unread distance). a's live
        // Firebrand is on m. b's legal centre is the known ally k; the live
        // distance between k and m cannot be read. b's funded cast must not
        // leave a's instance on m as evidence: explicit (same routine) and the
        // Classic forecast (next routine). Controls: a measured out-of-radius
        // m keeps the skip; a measured in-radius m is covered by b.
        private static void TestMbrUnreadDistance()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            AbilityKey key = MbbSpellKey(firebrand);
            Func<float, bool, MbbParty> party = (kPosition, unread) =>
            {
                var positions = new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    { "m", 0f }, { "k", kPosition }, { "b", 40f }, { "a", 60f }
                };
                MbbParty built = MbbMassParty(dump, new Dictionary<string, int> { { "a", 3 }, { "b", 3 } },
                    new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                    positions);
                List<UnitSnapshot> units = MbbUnits(positions.Keys);
                built.Options = built.Snapshot.Providers.Select(provider => MbrOption(dump, provider,
                    built.Effects[provider.Key.Ability.Canonical], units,
                    id => id == (provider.Key.CasterUnitId == "b" ? "k" : "m"),
                    unread ? MbrDistance(positions, "k", "m") : MbrDistance(positions),
                    (unit, fact) => fact == MbbAllyOfCaster ? true : fact == MbbEnemyOfCaster ? false : (bool?)false))
                    .ToList();
                return built;
            };
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "m", new[] { MbrFirebrand(firebrandBuff, "a", 0) } }
            });
            Func<MbbParty, ExplicitCastingPlan> compile = built => new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("b-cast", "long", "b", key.Canonical, key, new[] { "k" }, null,
                        CastingTargetMode.AnchoredOrigin, "k"),
                    GroupCasting("a-cast", "long", "a", key.Canonical, key, new[] { "m" }, null,
                        CastingTargetMode.AnchoredOrigin, "m")),
                built.Snapshot, built.Options, built.Effects, new CastEnhancementSnapshot[0], "long", null, false, live);
            Func<MbbParty, SequentialForecastPlanner.Result> forecast = built =>
            {
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbr-unread-distance");
                SourceAssignmentProfile byB = Assignment(key.Canonical, key, new[] { "k" });
                byB.CastingAssignments[0].CasterUnitId = "b";
                SourceAssignmentProfile byA = Assignment(key.Canonical, key, new[] { "m" });
                byA.CastingAssignments[0].CasterUnitId = "a";
                profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(byB);
                profile.Routines.First(rt => rt.RoutineId == "short").Assignments.Add(byA);
                return SequentialForecastPlanner.Compute(profile, new[] { "long", "short" }, built.Snapshot, live,
                    built.Effects, built.Options, new CastEnhancementSnapshot[0]);
            };
            MbbSubcases(
                MbbCase("unread: explicit", () =>
                {
                    ExplicitCastingPlan plan = compile(party(3f, true));
                    ResolvedCasting b = plan.CastingById("b-cast"), a = plan.CastingById("a-cast");
                    MbbExpect(b.Readiness == ResolvedCastingReadiness.Ready && !b.PredictedBeneficiaryUnitIds.Contains("m"),
                        "b's cast is not the reported shape: " + MbrDescribe(b));
                    MbbExpect(a.Readiness == ResolvedCastingReadiness.Ready && a.Cost.Sum(c => c.Units) == 1,
                        "An unread distance preserved a's Firebrand as if m were outside b's disk: " + MbrDescribe(a));
                }),
                MbbCase("unread: Classic forecast", () =>
                {
                    SequentialForecastPlanner.Result result = forecast(party(3f, true));
                    MbbExpect(result.Occurrences[0].Plan.Steps.Count == 1 &&
                        !result.Occurrences[0].Plan.Steps[0].ExpectedRecipientUnitIds.Contains("m"),
                        "b's Classic step is not the reported shape: " + MbrDescribe(result.Occurrences[0].Plan));
                    MbbExpect(result.Occurrences[1].Plan.Steps.Count == 1 &&
                        result.Occurrences[1].Plan.Steps[0].Provider.CasterUnitId == "a",
                        "The Classic forecast kept a's Firebrand on m across an unread distance: " +
                        MbrDescribe(result.Occurrences[1].Plan));
                }),
                MbbCase("control: measured outside (explicit and Classic)", () =>
                {
                    ResolvedCasting a = compile(party(20f, false)).CastingById("a-cast");
                    MbbExpect(a.Readiness == ResolvedCastingReadiness.AlreadySatisfied && a.Cost.Count == 0,
                        "A measured disjoint b disk cost a's skip: " + MbrDescribe(a));
                    SequentialForecastPlanner.Result result = forecast(party(20f, false));
                    MbbExpect(result.Occurrences[1].Plan.Steps.Count == 0,
                        "A measured disjoint b disk cost a's Classic skip: " + MbrDescribe(result.Occurrences[1].Plan));
                }),
                MbbCase("control: measured inside", () =>
                {
                    ExplicitCastingPlan plan = compile(party(3f, false));
                    MbbExpect(plan.CastingById("b-cast").PredictedBeneficiaryUnitIds.Contains("m") &&
                        plan.CastingById("a-cast").Readiness == ResolvedCastingReadiness.Ready,
                        "A measured in-radius m is no longer covered by b: " + MbrDescribe(plan.CastingById("b-cast")) +
                        " / " + MbrDescribe(plan.CastingById("a-cast")));
                }));
        }

        // R9-F4 (modified Prayer: robustness test, not native Prayer).
        // Prayer (Any selector) rebuilt with an outer HasBuff(HasteBuff)
        // condition: its true branch applies PrayerBuff unconditionally, its
        // false branch holds Prayer's original IsEnemy actions. "beast" is
        // read as hostile to the caster and has an effective PrayerBuff.
        // (a) Haste read present: the hostile alternative is unreachable and
        // the Classic skip holds; (b) Haste read absent and (c) unread: no
        // skip; (d) Haste read absent but an earlier funded assignment of the
        // routine certainly applies it: the skip holds.
        private static void TestMbrMutableGate()
        {
            var dump = new NativeBlueprintDump(MbbPrayer, MbbPrayerBuff, MbbPrayerDebuff, MbbHasteBuff);
            BlueprintAbility spell = dump.Get<BlueprintAbility>(MbbPrayer);
            var run = spell.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
            var has = NativeObject<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasBuff>();
            SetNative(has, "Buff", dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbHasteBuff));
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
                { "caster", 0f }, { "beast", radius * 0.5f }, { "other", radius * 0.25f }, { "hasted", radius * 9f }
            };
            MbbGroup group = MbbGroupCaster(dump, MbbPrayer, positions, id => id == "caster");
            EffectExpression expression = group.Effects[group.Ability.Canonical];
            string hasteKey = "buff-predicate:" + MbbHasteBuff;
            Func<bool?, RoutinePlanResult> plan = (hasteRead) => PlanMutableGate(dump, group, expression, positions,
                hasteRead, false);
            MbbSubcases(
                MbbCase("modified: gate read present", () =>
                {
                    MbbExpect(EffectRecipientFactBinding.FactIds(expression).Contains(hasteKey),
                        "The rebuilt Prayer no longer reads the HasBuff gate.");
                    RoutinePlanResult result = plan(true);
                    MbbExpect(result.Plan.Outcomes.Any(o => o.UnitId == "beast" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive) && result.Plan.Steps.Count == 0,
                        "A known mutable gate left the unreachable hostile branch able to veto the skip: " +
                        MbrDescribe(result.Plan));
                }),
                MbbCase("modified control: gate read absent", () =>
                {
                    RoutinePlanResult result = plan(false);
                    MbbExpect(!result.Plan.Outcomes.Any(o => o.UnitId == "beast" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "The hostile branch the absent gate selects no longer prevents the skip: " + MbrDescribe(result.Plan));
                }),
                MbbCase("modified control: gate unread", () =>
                {
                    RoutinePlanResult result = plan(null);
                    MbbExpect(!result.Plan.Outcomes.Any(o => o.UnitId == "beast" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "An unread gate established a skip: " + MbrDescribe(result.Plan));
                }),
                MbbCase("modified: gate set by an earlier funded assignment", () =>
                {
                    RoutinePlanResult result = PlanMutableGate(dump, group, expression, positions, false, true);
                    MbbExpect(result.Plan.Steps.Count == 1 && result.Plan.Outcomes.Any(o => o.UnitId == "beast" &&
                            o.AssignmentId != "gate" && o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "The gate an earlier funded assignment set did not decide the branch: " + MbrDescribe(result.Plan));
                }));
        }

        private static RoutinePlanResult PlanMutableGate(NativeBlueprintDump dump, MbbGroup group,
            EffectExpression expression, Dictionary<string, float> positions, bool? hasteRead, bool earlierGate)
        {
            string hasteKey = "buff-predicate:" + MbbHasteBuff;
            Func<string, string, bool?> reads = (unit, key) =>
            {
                if (key == MbbEnemyOfCaster) return unit == "beast";
                if (key == MbbAllyOfCaster) return unit != "beast";
                if (key == hasteKey) return unit == "beast" ? hasteRead : false;
                return false;
            };
            ProviderPlanningOption option = MbbCustomOption(dump, group.Snapshot.Providers.Single(), expression,
                MbbUnits(positions.Keys), id => id == "caster", positions, reads);
            var options = new List<ProviderPlanningOption> { option };
            var effects = new Dictionary<string, EffectExpression>(group.Effects, StringComparer.Ordinal);
            var providers = group.Snapshot.Providers.ToList();
            var pools = group.Snapshot.ResourcePools.ToList();
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbr-mutable-gate");
            var routine = profile.Routines.First(rt => rt.RoutineId == "long");
            if (earlierGate)
            {
                // A synthetic direct-target application of the gate buff
                // (HasteBuff) on beast, funded first.
                var gateKey = new AbilityKey("b2000000000000000000000000000002", string.Empty, 0, SourceKind.Spellbook,
                    string.Empty);
                effects[gateKey.Canonical] = new EffectLeafExpression(EffectKind.Buff, MbbHasteBuff,
                    EffectTarget.CurrentTarget, "fixture", "fixture:" + MbbHasteBuff);
                var gatePool = new ResourcePoolSnapshot("g|slots", ResourcePoolKind.SpontaneousLevel, 9, 9, null);
                pools.Add(gatePool);
                var gateProvider = new ProviderSnapshot(new ProviderKey("caster", "book-g", gateKey, "level-1|heighten-0"),
                    "gate", 1, gatePool.PoolKey, 1, null, null, 5, 50, "fixture", "fixture", "Book", 0, "Book");
                providers.Add(gateProvider);
                options.Add(new ProviderPlanningOption(gateProvider, new[] { "beast" }, new[] { "beast" }, 5, 50,
                    CastExecutionStrategy.DirectRuleCast, "direct"));
                SourceAssignmentProfile gate = Assignment(gateKey.Canonical, gateKey, new[] { "beast" });
                gate.CastingAssignments[0].AssignmentId = "gate";
                routine.Assignments.Add(gate);
            }
            SourceAssignmentProfile assignment = Assignment(group.Ability.Canonical, group.Ability, new[] { "beast" });
            assignment.CastingAssignments[0].CasterUnitId = "caster";
            assignment.CastingAssignments[0].Order = 1;
            routine.Assignments.Add(assignment);
            var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
            {
                { "beast", new[] { new ActiveEffectInstance(EffectKind.Buff, MbbPrayerBuff, 5000, 20, 0) } }
            });
            return new RoutinePlanService().Plan(profile, "long",
                new PartyProviderSnapshot(group.Snapshot.Units, providers, pools), live, effects, options,
                new CastEnhancementSnapshot[0]);
        }

        // R12-F2 (native Enlarge and Reduce Person, Mass). Mass Enlarge on e's
        // disk reaches m, not k; k has a sufficient live Reduce; Mass Reduce
        // anchored at k reaches m and k (skip-if-active). The Reduce row
        // starts Ready (m needs it); after Enlarge only k remains a
        // beneficiary and k is already satisfied: a zero-cost skip, with
        // Enlarge funded and protected. Control: without k's Reduce the cast
        // would run and remove the planned Enlarge (a conflict).
        private static void TestMbrReadyRowReassessed()
        {
            string enlarge = MbbPointGroupSpells[4][1], reduce = MbbPointGroupSpells[5][1],
                reduceBuff = MbbPointGroupSpells[5][2];
            var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4).SelectMany(row => new[] { row[1], row[2] }).ToArray());
            float r = 15f * 0.3048f;
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "w", 10f * r }, { "e", -0.6f * r }, { "m", 0f }, { "k", 0.9f * r }
            };
            MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "w", 3 } },
                new Dictionary<string, string[]> { { "w", new[] { enlarge, reduce } } }, positions,
                new Dictionary<string, Func<string, bool>> { { enlarge, id => id == "e" }, { reduce, id => id == "k" } });
            AbilityKey enlargeKey = MbbSpellKey(enlarge), reduceKey = MbbSpellKey(reduce);
            Func<bool, ExplicitCastingPlan> compile = kReduced => new ExplicitCastingCompiler().Compile(CastingDocument(
                    GroupCasting("enlarge", "long", "w", enlargeKey.Canonical, enlargeKey, new[] { "m" }, null,
                        CastingTargetMode.AnchoredOrigin, "e"),
                    GroupCasting("reduce", "long", "w", reduceKey.Canonical, reduceKey, new[] { "m", "k" }, null,
                        CastingTargetMode.AnchoredOrigin, "k")),
                party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], "long", null, false,
                ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "k", kReduced ? new[] { new ActiveEffectInstance(EffectKind.Buff, reduceBuff, 5000, 20, 0) }
                        : new ActiveEffectInstance[0] }
                }));
            MbbSubcases(
                MbbCase("native: narrowed to an already satisfied ally", () =>
                {
                    ExplicitCastingPlan opening = new ExplicitCastingCompiler().Compile(CastingDocument(
                            GroupCasting("reduce", "long", "w", reduceKey.Canonical, reduceKey, new[] { "m", "k" }, null,
                                CastingTargetMode.AnchoredOrigin, "k")),
                        party.Snapshot, party.Options, party.Effects, new CastEnhancementSnapshot[0], "long", null, false,
                        ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                        {
                            { "k", new[] { new ActiveEffectInstance(EffectKind.Buff, reduceBuff, 5000, 20, 0) } }
                        }));
                    MbbExpect(opening.CastingById("reduce").Readiness == ResolvedCastingReadiness.Ready &&
                        opening.CastingById("reduce").PreCoveredUnitIds.Contains("k"),
                        "The Reduce row does not start Ready with k pre-covered: " + MbrDescribe(opening.CastingById("reduce")));
                    ExplicitCastingPlan plan = compile(true);
                    ResolvedCasting first = plan.CastingById("enlarge"), second = plan.CastingById("reduce");
                    MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(c => c.Units) == 1,
                        "Mass Enlarge is not funded: " + MbrDescribe(first));
                    MbbExpect(second.Readiness == ResolvedCastingReadiness.AlreadySatisfied && second.Cost.Count == 0 &&
                        second.PredictedBeneficiaryUnitIds.SequenceEqual(new[] { "k" }) &&
                        !second.ReadinessReasons.Any(reason => reason.StartsWith(ExplicitCastingCompiler.RemovesPlannedEffect,
                            StringComparison.Ordinal)),
                        "The narrowed Reduce row was not reassessed as a zero-cost skip: " + MbrDescribe(second));
                }),
                MbbCase("native control: k not yet reduced", () =>
                {
                    ResolvedCasting second = compile(false).CastingById("reduce");
                    MbbExpect(second.Readiness == ResolvedCastingReadiness.Blocked && second.Cost.Count == 0 &&
                        second.ReadinessReasons.Any(reason => reason.StartsWith(ExplicitCastingCompiler.RemovesPlannedEffect,
                            StringComparison.Ordinal)),
                        "A Reduce cast that would undo the planned Enlarge was not refused: " + MbrDescribe(second));
                }));
        }

        // R12-F3 (native Firebrand; profile ban). An unpinned saved Classic
        // Firebrand for "ally"; a and b know it, b is banned and only a's
        // readable effective Firebrand is on the ally: SkippedAlreadyActive,
        // no step, no reservation. Controls: a's instance absent (a casts; b
        // never does); a without a slot but with the effect (still skips).
        private static void TestMbrBannedCasterSkip()
        {
            string firebrand = MbbPointGroupSpells[2][1], firebrandBuff = MbbPointGroupSpells[2][2];
            var dump = new NativeBlueprintDump(firebrand, firebrandBuff);
            AbilityKey key = MbbSpellKey(firebrand);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal) { { "a", 0f }, { "b", 1f }, { "ally", 2f } };
            Func<int, bool, RoutinePlanResult> plan = (aSlots, aEffect) =>
            {
                MbbParty party = MbbMassParty(dump, new Dictionary<string, int> { { "a", aSlots }, { "b", 3 } },
                    new Dictionary<string, string[]> { { "a", new[] { firebrand } }, { "b", new[] { firebrand } } },
                    positions);
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbr-banned-caster");
                ProviderSnapshot b = party.Snapshot.Providers.Single(p => p.Key.CasterUnitId == "b");
                profile.ProviderPreferences.Add(new ProviderPreferenceProfile { ProviderKey = b.Key.Canonical, Banned = true });
                profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(
                    Assignment(key.Canonical, key, new[] { "ally" }));
                var live = ActiveEffectSnapshot.FromInstances(new Dictionary<string, IEnumerable<ActiveEffectInstance>>
                {
                    { "ally", aEffect ? new[] { MbrFirebrand(firebrandBuff, "a", 0) } : new ActiveEffectInstance[0] }
                });
                return new RoutinePlanService().Plan(profile, "long", party.Snapshot, live, party.Effects,
                    party.Options, new CastEnhancementSnapshot[0]);
            };
            MbbSubcases(
                MbbCase("native: banned b, effective a", () =>
                {
                    RoutinePlanResult result = plan(3, true);
                    MbbExpect(result.Plan.Steps.Count == 0 && result.Plan.Outcomes.Any(o => o.UnitId == "ally" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "A banned caster vetoed a's valid skip: " + MbrDescribe(result.Plan));
                }),
                MbbCase("native control: a's instance absent", () =>
                {
                    RoutinePlanResult result = plan(3, false);
                    MbbExpect(result.Plan.Steps.Count == 1 && result.Plan.Steps[0].Provider.CasterUnitId == "a",
                        "Without a's instance a does not cast (or banned b does): " + MbrDescribe(result.Plan));
                }),
                MbbCase("native control: a has no slot but the effect", () =>
                {
                    RoutinePlanResult result = plan(0, true);
                    MbbExpect(result.Plan.Steps.Count == 0 && result.Plan.Outcomes.Any(o => o.UnitId == "ally" &&
                            o.Kind == TargetOutcomeKind.SkippedAlreadyActive),
                        "a's sufficient effect needed a spell slot: " + MbrDescribe(result.Plan));
                }));
        }

        // R11-F2 (native Good Hope). Two unpinned bards with equal (default)
        // priority know Good Hope; bard A (caster level 9) wins the old
        // caster-level tie-break but its legal centres are only the requested
        // allies p and q (20 feet apart: one disk each); bard B (level 7) may
        // centre on the unrequested ally between them, covering both. One
        // paid B step, both fulfilled. Controls: a pin on A, an explicit
        // priority for A, and B without a slot all fall back to A's two casts.
        private static void TestMbrProviderCentres()
        {
            var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bardA", 30f }, { "bardB", 31f }, { "p", 0f }, { "mid", 10f * 0.3048f }, { "q", 20f * 0.3048f }
            };
            Func<int, string, bool, RoutinePlanResult> plan = (bSlots, pin, priorityA) =>
            {
                List<UnitSnapshot> units = MbbUnits(positions.Keys);
                PartyProviderAdmission admission = MbbAdmission(dump);
                var providers = new List<ProviderSnapshot>();
                var pools = new List<ResourcePoolSnapshot>();
                foreach (KeyValuePair<string, int> bard in new Dictionary<string, int> { { "bardA", 9 }, { "bardB", 7 } })
                {
                    int slots = bard.Key == "bardB" ? bSlots : 3;
                    ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool(bard.Key, MbbBardBook, 3,
                        slots, Math.Max(3, slots));
                    pools.Add(pool);
                    admission.AdmitSpell(bard.Key, MbbBardBook, false, "Bard",
                        MbbRead(dump, MbbGoodHope, "Good Hope", SourceKind.Spellbook, bard.Value), 3, 0, pool.PoolKey, 1,
                        new string[0], providers);
                }
                var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
                List<ProviderPlanningOption> options = providers.Select(provider => MbbOption(dump, provider,
                    effects[provider.Key.Ability.Canonical], units,
                    provider.Key.CasterUnitId == "bardA" ? (Func<string, bool>)(id => id == "p" || id == "q")
                        : id => id == "mid", positions)).ToList();
                var snapshot = new PartyProviderSnapshot(units, providers, pools);
                AbilityKey hope = providers[0].Key.Ability;
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("mbr-provider-centres");
                SourceAssignmentProfile assignment = Assignment(hope.Canonical, hope, new[] { "p", "q" });
                assignment.CastingAssignments[0].CasterUnitId = pin;
                profile.Routines.First(rt => rt.RoutineId == "long").Assignments.Add(assignment);
                if (priorityA)
                    profile.ProviderPreferences.Add(new ProviderPreferenceProfile
                    {
                        ProviderKey = providers.Single(p => p.Key.CasterUnitId == "bardA").Key.Canonical,
                        Priority = 0
                    });
                return new RoutinePlanService().Plan(profile, "long", snapshot, new ActiveEffectSnapshot(null), effects,
                    options, new CastEnhancementSnapshot[0]);
            };
            Func<RoutinePlanResult, bool> bothFulfilled = result =>
                result.Plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Fulfilled) == 2 &&
                !result.Plan.Outcomes.Any(o => o.Kind == TargetOutcomeKind.Unfulfilled);
            MbbSubcases(
                MbbCase("native: equally prioritized bards", () =>
                {
                    RoutinePlanResult result = plan(3, null, false);
                    MbbExpect(result.Plan.Steps.Count == 1 && result.Plan.Steps[0].Provider.CasterUnitId == "bardB" &&
                        result.Plan.Steps[0].AnchorUnitId == "mid" && result.Plan.Steps[0].Reservation.Units == 1 &&
                        bothFulfilled(result),
                        "Classic spent a slot per ally instead of B's single covering cast: " + MbrDescribe(result.Plan));
                }),
                MbbCase("native control: pin on A", () =>
                {
                    RoutinePlanResult result = plan(3, "bardA", false);
                    MbbExpect(result.Plan.Steps.Count == 2 && result.Plan.Steps.All(s => s.Provider.CasterUnitId == "bardA") &&
                        bothFulfilled(result), "The pin on A was not honoured: " + MbrDescribe(result.Plan));
                }),
                MbbCase("native control: explicit priority for A", () =>
                {
                    RoutinePlanResult result = plan(3, null, true);
                    MbbExpect(result.Plan.Steps.Count == 2 && result.Plan.Steps.All(s => s.Provider.CasterUnitId == "bardA") &&
                        bothFulfilled(result), "A's explicit priority was not honoured: " + MbrDescribe(result.Plan));
                }),
                MbbCase("native control: B has no slot", () =>
                {
                    RoutinePlanResult result = plan(0, null, false);
                    MbbExpect(result.Plan.Steps.Count == 2 && result.Plan.Steps.All(s => s.Provider.CasterUnitId == "bardA") &&
                        bothFulfilled(result), "An unfunded B blocked A's fallback: " + MbrDescribe(result.Plan));
                }));
        }

        // A detached referenced ability whose run action is empty and whose
        // own AbilityExecuteActionOnCast deals damage (its gameplay lies
        // outside the run-action graph the adapter reads).
        private static BlueprintAbility MbrOnCastDamageChild()
        {
            var child = NativeObject<BlueprintAbility>();
            SetNative(child, "m_AssetGuid", "c3000000000000000000000000000001");
            var run = NativeObject<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>();
            SetNative(run, "Actions", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[0]
            });
            var onCast = NativeObject<Kingmaker.UnitLogic.Abilities.Components.AbilityExecuteActionOnCast>();
            SetNative(onCast, "Actions", new Kingmaker.ElementsSystem.ActionList
            {
                Actions = new Kingmaker.ElementsSystem.GameAction[]
                {
                    NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>()
                }
            });
            SetNative(child, "Components", new Kingmaker.Blueprints.BlueprintComponent[] { run, onCast });
            return child;
        }

        private static Kingmaker.UnitLogic.Mechanics.Actions.ContextActionCastSpell MbrCastSpell(BlueprintAbility spell)
        {
            var cast = NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionCastSpell>();
            SetNative(cast, "Spell", spell);
            return cast;
        }

        private static KeyValuePair<MbbDiscovery, int> MbrDiscoverAndAdmit(NativeBlueprintDump dump, string guid)
        {
            MbbDiscovery discovered = MbbDiscover(dump, guid);
            var providers = new List<ProviderSnapshot>();
            MbbAdmission(dump).AdmitSpell("caster", "book", false, "Book",
                MbbRead(dump, guid, guid, SourceKind.Spellbook, 9), 3, 0, "pool", 1, new string[0], providers);
            return new KeyValuePair<MbbDiscovery, int>(discovered, providers.Count);
        }

        // R11-F1 (modified Feather Step, Mass: robustness test). The
        // unchanged selector and beneficial run action, plus an on-cast
        // ContextActionCastSpell whose referenced ability has an empty run
        // graph and a damaging AbilityExecuteActionOnCast of its own: refused
        // by discovery and provider admission. Neighbor: the same reference
        // as a sibling in the run graph. Control: the unmodified spell.
        private static void TestMbrNestedOnCastReference()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            MbbSubcases(
                MbbCase("modified: on-cast reference", () =>
                {
                    var dump = new NativeBlueprintDump(feather, featherBuff);
                    BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
                    var onCast = NativeObject<Kingmaker.UnitLogic.Abilities.Components.AbilityExecuteActionOnCast>();
                    SetNative(onCast, "Actions", new Kingmaker.ElementsSystem.ActionList
                    {
                        Actions = new Kingmaker.ElementsSystem.GameAction[] { MbrCastSpell(MbrOnCastDamageChild()) }
                    });
                    SetNative(spell, "Components", spell.ComponentsArray.Concat(
                        new Kingmaker.Blueprints.BlueprintComponent[] { onCast }).ToArray());
                    KeyValuePair<MbbDiscovery, int> result = MbrDiscoverAndAdmit(dump, feather);
                    MbbExpect(!result.Key.Included && result.Value == 0,
                        "An allied burst whose on-cast reference runs unread gameplay was admitted: " +
                        result.Key.Repaired.Reason + " diagnostics=[" + string.Join(",",
                            result.Key.Facts.Diagnostics.Select(d => d.Code + ":" + d.Detail)) + "]");
                }),
                MbbCase("modified neighbor: run-graph reference", () =>
                {
                    var dump = new NativeBlueprintDump(feather, featherBuff);
                    BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
                    var targets = spell.ComponentsArray
                        .OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Single();
                    targets.Actions.Actions = targets.Actions.Actions.Concat(new Kingmaker.ElementsSystem.GameAction[]
                    {
                        MbrCastSpell(MbrOnCastDamageChild())
                    }).ToArray();
                    KeyValuePair<MbbDiscovery, int> result = MbrDiscoverAndAdmit(dump, feather);
                    MbbExpect(!result.Key.Included && result.Value == 0,
                        "An allied burst whose run-graph reference runs unread on-cast gameplay was admitted: " +
                        result.Key.Repaired.Reason);
                }),
                MbbCase("native control", () =>
                {
                    KeyValuePair<MbbDiscovery, int> result = MbrDiscoverAndAdmit(
                        new NativeBlueprintDump(feather, featherBuff), feather);
                    MbbExpect(result.Key.Included && result.Value == 1,
                        "Unmodified Feather Step, Mass is no longer admitted: " + result.Key.Repaired.Reason);
                }));
        }

        // R12-F4 (modified Good Hope: robustness test). Good Hope keeps its
        // native flags (CanTargetPoint false), radius and run graph; its
        // AbilityTargetsAround gains its own condition (a), or the condition
        // cannot be read (b): no allied disk is predicted, so nothing is
        // promised or reserved. Control: the native empty checker keeps the
        // 15-foot disk and a one-slot cast.
        private static void TestMbrGoodHopeSelectorCondition()
        {
            var positions = new Dictionary<string, float>(StringComparer.Ordinal)
            {
                { "bard", 0f }, { "near", 3f }, { "far", 10f }
            };
            Func<Action<Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround>, MbbParty> party = alter =>
            {
                var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff);
                var around = dump.Get<BlueprintAbility>(MbbGoodHope).ComponentsArray
                    .OfType<Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround>().Single();
                if (alter != null) alter(around);
                return MbbGoodHopeParty(dump, positions, (unit, fact) => false, 2, id => true);
            };
            Func<MbbParty, ResolvedCasting> compile = built =>
            {
                AbilityKey hope = built.Abilities[MbbGoodHope];
                return new ExplicitCastingCompiler().Compile(CastingDocument(
                        GroupCasting("hope", "long", "bard", hope.Canonical, hope, new[] { "bard", "near" })),
                    built.Snapshot, built.Options, built.Effects, new CastEnhancementSnapshot[0]).CastingById("hope");
            };
            Action<Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround> conditioned = around =>
            {
                var has = NativeObject<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact>();
                var fact = NativeObject<Kingmaker.Blueprints.Classes.BlueprintFeature>();
                SetNative(fact, "m_AssetGuid", "c3000000000000000000000000000002");
                SetNative(has, "Fact", fact);
                SetNative(around, "m_Condition", new Kingmaker.ElementsSystem.ConditionsChecker
                {
                    Conditions = new Kingmaker.ElementsSystem.Condition[] { has }
                });
            };
            MbbSubcases(
                MbbCase("modified: selector with its own condition", () =>
                {
                    MbbParty built = party(conditioned);
                    ResolvedCasting hope = compile(built);
                    MbbExpect(built.Options.Single().LegalAnchorIds.Count == 0 &&
                        hope.Readiness == ResolvedCastingReadiness.Blocked && hope.Cost.Count == 0 &&
                        hope.PredictedBeneficiaryUnitIds.Count == 0,
                        "Good Hope promised a plain disk although its selector filters recipients: " + MbrDescribe(hope) +
                        " anchors=" + string.Join(",", built.Options.Single().LegalAnchorIds));
                }),
                MbbCase("modified: selector condition unread", () =>
                {
                    MbbParty built = party(around => SetNative(around, "m_Condition", null));
                    ResolvedCasting hope = compile(built);
                    MbbExpect(built.Options.Single().LegalAnchorIds.Count == 0 &&
                        hope.Readiness == ResolvedCastingReadiness.Blocked && hope.Cost.Count == 0,
                        "Good Hope promised a plain disk although its selector condition is unread: " + MbrDescribe(hope));
                }),
                MbbCase("native control", () =>
                {
                    MbbParty built = party(null);
                    ResolvedCasting hope = compile(built);
                    MbbExpect(hope.Readiness == ResolvedCastingReadiness.Ready && hope.Cost.Sum(c => c.Units) == 1 &&
                        hope.PredictedBeneficiaryUnitIds.Contains("near") && !hope.PredictedBeneficiaryUnitIds.Contains("far"),
                        "Native Good Hope lost its 15-foot disk or its one-slot cast: " + MbrDescribe(hope));
                }));
        }

        // R12-F5 (modified Feather Step, Mass: robustness test). The native
        // Daze fixture's BuffStatusCondition(Dazed) added to FeatherStepBuff,
        // which keeps its native difficult-terrain immunity: refused by
        // discovery and provider admission. Controls: unmodified Feather Step
        // and the Enlarge/Reduce Person, Mass size tradeoff stay admitted.
        private static void TestMbrImposedDaze()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            MbbSubcases(
                MbbCase("modified: Daze beside a benefit", () =>
                {
                    var dump = new NativeBlueprintDump(feather, featherBuff, MbbDazeBuff);
                    var buff = dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(featherBuff);
                    var dazed = dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbDazeBuff).ComponentsArray
                        .OfType<Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition>().Single();
                    MbbExpect(buff.ComponentsArray.Any(c => c.GetType().Name == "AddConditionImmunity") &&
                        dazed.Condition.ToString() == "Dazed", "The fixture is not the reported shape.");
                    SetNative(buff, "Components", buff.ComponentsArray.Concat(
                        new Kingmaker.Blueprints.BlueprintComponent[] { dazed }).ToArray());
                    KeyValuePair<MbbDiscovery, int> result = MbrDiscoverAndAdmit(dump, feather);
                    MbbExpect(!result.Key.Included && result.Value == 0,
                        "A Daze beside Feather Step's benefit was admitted on a unit anchor: " + result.Key.Repaired.Reason);
                }),
                MbbCase("native control: Feather Step", () =>
                {
                    KeyValuePair<MbbDiscovery, int> result = MbrDiscoverAndAdmit(
                        new NativeBlueprintDump(feather, featherBuff), feather);
                    MbbExpect(result.Key.Included && result.Value == 1,
                        "Unmodified Feather Step, Mass is no longer admitted: " + result.Key.Repaired.Reason);
                }),
                MbbCase("native control: size tradeoff", () =>
                {
                    var dump = new NativeBlueprintDump(MbbPointGroupSpells.Skip(4)
                        .SelectMany(row => new[] { row[1], row[2] }).ToArray());
                    foreach (string spell in new[] { MbbPointGroupSpells[4][1], MbbPointGroupSpells[5][1] })
                    {
                        KeyValuePair<MbbDiscovery, int> result = MbrDiscoverAndAdmit(dump, spell);
                        MbbExpect(result.Key.Included && result.Value == 1,
                            spell + " (a native size tradeoff) is no longer admitted: " + result.Key.Repaired.Reason);
                    }
                }));
        }

        // R12-F6 (modified Good Hope: robustness test). GoodHopeBuff gains an
        // AddFactContextActions whose Activated list removes Haste (an
        // automatic removal the planner's effect model does not represent):
        // refused by discovery and provider admission, so a routine planning
        // Haste then Good Hope cannot claim Haste is preserved. Control: the
        // native Good Hope stays admitted with its ability-level Crushing
        // Despair branch (a removal the planner does model).
        private static void TestMbrAutomaticRemoval()
        {
            MbbSubcases(
                MbbCase("modified: activation removes Haste", () =>
                {
                    var dump = new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff, MbbHasteBuff);
                    var buff = dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbGoodHopeBuff);
                    var remove = NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff>();
                    SetNative(remove, "Buff", dump.Get<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(MbbHasteBuff));
                    var actions = NativeObject<Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions>();
                    SetNative(actions, "Activated", new Kingmaker.ElementsSystem.ActionList
                    {
                        Actions = new Kingmaker.ElementsSystem.GameAction[] { remove }
                    });
                    SetNative(actions, "Deactivated", new Kingmaker.ElementsSystem.ActionList
                    {
                        Actions = new Kingmaker.ElementsSystem.GameAction[0]
                    });
                    SetNative(actions, "NewRound", new Kingmaker.ElementsSystem.ActionList
                    {
                        Actions = new Kingmaker.ElementsSystem.GameAction[0]
                    });
                    SetNative(buff, "Components", buff.ComponentsArray.Concat(
                        new Kingmaker.Blueprints.BlueprintComponent[] { actions }).ToArray());
                    KeyValuePair<MbbDiscovery, int> result = MbrDiscoverAndAdmit(dump, MbbGoodHope);
                    MbbExpect(!result.Key.Included && result.Value == 0,
                        "Good Hope whose buff automatically removes Haste was admitted: " + result.Key.Repaired.Reason);
                }),
                MbbCase("native control: Crushing Despair branch", () =>
                {
                    KeyValuePair<MbbDiscovery, int> result = MbrDiscoverAndAdmit(
                        new NativeBlueprintDump(MbbGoodHope, MbbGoodHopeBuff), MbbGoodHope);
                    EffectChanges changes = EffectChangeAnalysis.Analyze(result.Key.Expression, "bard", "ally");
                    MbbExpect(result.Key.Included && result.Value == 1 &&
                        changes.Removed.Any(pair => pair.Value == MbbCrushingDespairBuff) &&
                        changes.Applied.Any(pair => pair.Value == MbbGoodHopeBuff),
                        "Native Good Hope lost its admission or its Crushing Despair branch: " + result.Key.Repaired.Reason);
                }));
        }
    }
}
