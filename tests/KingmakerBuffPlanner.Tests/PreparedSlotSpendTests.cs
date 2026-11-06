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
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // 0.6.12 owner reproduction: the second prepared Heroism of one book
        // stopped the routine with provider-ability-not-found.
        //
        // A simulated native prepared spellbook following the installed
        // 2.1.7b contract (Assembly-CSharp, decompiled):
        //  - Spellbook.Memorize: a two-slot (opposition-school) preparation
        //    links its slots; LinkedSlots[0] is the main slot.
        //  - Spellbook.Spend -> SpendInternal(blueprint, spell, doSpend): walks
        //    the level's slot list from the LAST slot back and spends the
        //    first available slot whose Spell Equals the cast AbilityData
        //    (value equality: blueprint, caster, fact, spellbook, conversion,
        //    metamagic). It is given no slot.
        //  - SpellSlot.Spend: the slot, and for an opposition preparation
        //    every linked slot, becomes unavailable.
        //  - GetAvailableForCastSpellCount: available slots of the blueprint,
        //    halved for an opposition school.
        // The spender never reads step.Reservation. The session, compiler,
        // gate, converter, run coordinator, executors, report and the
        // production slot choice (PreparedSlotChoice) are real.
        private sealed class NativePreparedWorld : IInstantCastRuntimeAdapter, ICastRuntimeAdapter
        {
            internal sealed class Slot
            {
                internal int Level;
                internal int Type = 0; // SpellSlotType.Common
                internal int Index;
                internal string Spell;
                internal bool Available = true;
                internal bool Opposition;
                internal Slot[] Linked;
                internal string Id { get { return PreparedSlotIds.Format(Level, Type, Index); } }
                internal bool IsMain { get { return Linked == null || Linked.Length == 0 || Linked[0] == this; } }
                internal int Busy { get { return Linked == null ? 0 : Linked.Length; } }
            }

            internal static readonly AbilityKey Heroism = new AbilityKey("fixture-heroism", null, 0, SourceKind.Spellbook, null);
            internal static readonly AbilityKey Successor = new AbilityKey("fixture-successor", null, 0, SourceKind.Spellbook, null);
            internal const string Caster = "unit-wizard";
            internal const string Pool = "pool-unit-wizard";
            internal readonly List<Slot> Slots = new List<Slot>();
            internal readonly Dictionary<string, KeyValuePair<string, long>> Active =
                new Dictionary<string, KeyValuePair<string, long>>(StringComparer.Ordinal);
            internal readonly List<string> Fired = new List<string>();
            // Per submission: the slot the step resolved from, the slots the
            // native spend consumed, and the native available count around it.
            internal readonly List<string> Resolutions = new List<string>();
            internal readonly List<string[]> Consumed = new List<string[]>();
            internal readonly List<string> Counts = new List<string>();
            private readonly HashSet<CastStep> _applied = new HashSet<CastStep>();
            private int _instances;
            internal long Round = 0;

            internal void Prepare(AbilityKey spell, int level, int count, bool opposition)
            {
                for (int copy = 0; copy < count; copy++)
                {
                    int next = Slots.Where(s => s.Level == level && s.Type == 0).Select(s => s.Index + 1)
                        .DefaultIfEmpty(0).Max();
                    var main = new Slot { Level = level, Index = next, Spell = spell.Canonical, Opposition = opposition };
                    Slots.Add(main);
                    if (!opposition) continue;
                    var second = new Slot { Level = level, Index = next + 1, Spell = spell.Canonical, Opposition = true };
                    Slots.Add(second);
                    main.Linked = second.Linked = new[] { main, second };
                }
            }

            // Spellbook.GetAllMemorizedSpells: each level's list, sorted by
            // (Type, Index), concatenated.
            private IEnumerable<Slot> Ordered()
            {
                return Slots.OrderBy(s => s.Level).ThenBy(s => s.Type).ThenBy(s => s.Index);
            }

            internal int Count(string spell, int level)
            {
                int count = Slots.Count(s => s.Level == level && s.Available && s.Spell == spell);
                return Slots.Any(s => s.Spell == spell && s.Opposition) ? count / 2 : count;
            }

            internal int Count(AbilityKey spell)
            {
                return Count(spell.Canonical, Slots.First(s => s.Spell == spell.Canonical).Level);
            }

            public bool IsInCombat { get { return false; } }

            // The production adapter's prepared branch: the same candidate
            // view and the same PreparedSlotChoice.
            private Slot Resolve(CastStep step, out string resolution, out string refusal)
            {
                var candidates = Ordered().Select(s => new PreparedSlotCandidate(s.Id, s.Available, s.IsMain,
                    s.Spell == step.Provider.Ability.Canonical,
                    s.Spell + "|" + s.Level + "|" + s.Type + "|" + s.Opposition + "|" + s.Busy,
                    s.Linked == null ? null : s.Linked.Select(l => l.Id))).ToList();
                string chosen = PreparedSlotChoice.Choose(step.Reservation.TokenIds,
                    step.Reservation.EarlierConfirmedTokenIds, candidates, out resolution, out refusal);
                return chosen == null ? null : Slots.Single(s => s.Id == chosen);
            }

            public CastRuntimeValidation Validate(CastStep step)
            {
                string resolution, refusal;
                Slot slot = Resolve(step, out resolution, out refusal);
                if (slot == null) return CastRuntimeValidation.Fail(refusal ?? "provider-ability-not-found");
                string countRefusal = AvailableCountJudgement.BeforeSubmission(step.Reservation,
                    Count(slot.Spell, slot.Level));
                return countRefusal == null ? CastRuntimeValidation.Pass() : CastRuntimeValidation.Fail(countRefusal);
            }

            public InstantCastResult Fire(CastStep step)
            {
                CastRuntimeValidation validation = Validate(step);
                if (!validation.Valid)
                    return new InstantCastResult(false, false, false, false, false, "final-validation:" + validation.Reason);
                string resolution, refusal;
                Slot source = Resolve(step, out resolution, out refusal);
                int before = Count(source.Spell, source.Level);
                Fired.Add(step.AssignmentId);
                Resolutions.Add(resolution);
                // SpendInternal: last slot of the level first; the cast's
                // AbilityData is equal for every copy of the preparation.
                List<Slot> level = Slots.Where(s => s.Level == source.Level)
                    .OrderBy(s => s.Type).ThenBy(s => s.Index).ToList();
                var consumed = new List<string>();
                for (int index = level.Count - 1; index >= 0; index--)
                {
                    Slot slot = level[index];
                    if (!slot.Available || slot.Spell != source.Spell) continue;
                    slot.Available = false;
                    consumed.Add(slot.Id);
                    if (slot.Linked != null && slot.Opposition)
                        foreach (Slot linked in slot.Linked)
                        {
                            if (linked.Available) consumed.Add(linked.Id);
                            linked.Available = false;
                        }
                    break;
                }
                Consumed.Add(consumed.OrderBy(v => v, StringComparer.Ordinal).ToArray());
                int after = Count(source.Spell, source.Level);
                Counts.Add(step.AssignmentId + ":" + before + ">" + after);
                Active[step.TargetUnitIds[0]] = new KeyValuePair<string, long>(
                    step.Provider.Ability.Canonical + "#" + (++_instances), Round + 100);
                _applied.Add(step);
                return new InstantCastResult(true, true, true, after < before, true,
                    "simulated-native;resolution:" + resolution + ";native-consumed:" +
                    string.Join(",", consumed.ToArray()),
                    AvailableCountJudgement.Violation(step.Reservation, before, after));
            }

            public bool EffectsObserved(CastStep step) { return _applied.Contains(step); }
            public InstantCastCompletion InspectCompletion(CastStep step) { return InstantCastCompletion.Settled("simulated-settled"); }
            public InstantCastCompletion Cleanup(CastStep step) { return InstantCastCompletion.Settled("simulated-clean"); }

            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                CastRuntimeValidation validation = Validate(step);
                if (!validation.Valid) throw new InvalidOperationException(validation.Reason);
                return new Operation(this, step, Fire(step));
            }

            private sealed class Operation : IAnimatedCastOperation
            {
                private readonly NativePreparedWorld _world;
                private readonly CastStep _step;
                private readonly InstantCastResult _result;
                internal Operation(NativePreparedWorld world, CastStep step, InstantCastResult result)
                { _world = world; _step = step; _result = result; }
                public bool IsCompleted { get { return true; } }
                public bool IsStarted { get { return true; } }
                public bool TimedOut { get { return false; } }
                public bool Succeeded { get { return _result.Succeeded; } }
                public bool EffectsObserved { get { return _world.EffectsObserved(_step); } }
                public bool ResourceSpent { get { return _result.ResourceSpent; } }
                public string ResourceCountViolation { get { return _result.ResourceCountViolation; } }
                public bool HasResidualDeliveryState { get { return false; } }
                public string Detail { get { return _result.Detail; } }
                public void Dispose() { }
            }

            // KingmakerPartySnapshotBuilder.ScanPreparedSpellbook's shape:
            // every memorized slot a token (linked ids include the slot
            // itself), one provider per preparation group whose eligible
            // tokens are its main slots in list order.
            internal CastingWorkspaceInputs Inputs()
            {
                string[] all = { Caster, "unit-t1", "unit-t2", "unit-t3", "unit-t4" };
                List<UnitSnapshot> units = all.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))).ToList();
                List<Slot> ordered = Ordered().ToList();
                var tokens = ordered.Select(s => new ResourceTokenSnapshot(s.Id, AbilityFor(s.Spell), s.Level,
                    s.Opposition ? PreparedSlotKind.Opposition : PreparedSlotKind.Common, s.Available, s.IsMain,
                    s.Linked == null ? null : s.Linked.Select(l => l.Id))).ToList();
                var pools = new[] { new ResourcePoolSnapshot(Pool, ResourcePoolKind.PreparedSlots,
                    tokens.Count, tokens.Count(t => t.Available), tokens) };
                var providers = ordered.GroupBy(s => s.Spell, StringComparer.Ordinal).Select(group =>
                    new ProviderSnapshot(new ProviderKey(Caster, "book-wizard", AbilityFor(group.Key),
                        "level-" + group.First().Level + "|heighten-0"), group.Key, group.First().Level, Pool, 1,
                        group.Where(s => s.IsMain).Select(s => s.Id), null, 10, 100, "", "10 minutes/level")).ToList();
                var options = providers.Select(provider => new ProviderPlanningOption(provider, all,
                    new[] { Caster }, 10, 100, CastExecutionStrategy.DirectRuleCast, "simulated-direct",
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal))).ToList();
                var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
                {
                    { "source-heroism", Leaf(Heroism.Canonical) },
                    { "source-successor", Leaf(Successor.Canonical) }
                };
                effects[Heroism.Canonical] = effects["source-heroism"];
                effects[Successor.Canonical] = effects["source-successor"];
                var live = LiveEffects(Active.Where(pair => pair.Value.Value > Round).Select(pair =>
                    On(pair.Key, pair.Value.Key.Split('#')[0], pair.Value.Value - Round, 10, 0)).ToArray());
                return new CastingWorkspaceInputs(new PartyProviderSnapshot(units, providers, pools), options,
                    effects, new CastEnhancementSnapshot[0], null, live);
            }

            private static AbilityKey AbilityFor(string canonical)
            {
                return canonical == Heroism.Canonical ? Heroism : Successor;
            }
        }

        private sealed class PreparedSpendSession
        {
            internal readonly NativePreparedWorld World;
            internal readonly CastingExecutionHost Host;
            internal readonly CastingWorkspaceSession Session;
            internal readonly List<CastStep> Steps = new List<CastStep>();

            internal PreparedSpendSession(string root, string mode, string name, NativePreparedWorld world,
                params KeyValuePair<string, string>[] castings)
            {
                World = world;
                Host = new CastingExecutionHost(settings => mode == "instant"
                    ? (ICastExecutor)new InstantCastExecutor(World, true)
                    : new AnimatedCastExecutor(World, true), () => World.Round * 6000);
                var boundary = new NativeCastingDispatchBoundary(Host, () => Session.ExecutionSettings);
                Session = new CastingWorkspaceSession(Path.Combine(root, "prepared-slot-" + mode + "-" + name),
                    "prepared-campaign", boundary);
                Host.RunCompleted = report => Session.RecordRunReport(report);
                Session.SetExecutionMode(mode);
                CastingWorkspaceInputs inputs = World.Inputs();
                foreach (KeyValuePair<string, string> casting in castings)
                {
                    Session.SelectBuff(casting.Key);
                    Session.SelectRoutine("long");
                    Session.BuildView(inputs);
                    Session.Draft.SourceId = casting.Key;
                    Session.Draft.TargetMode = CastingTargetMode.DirectTarget;
                    Session.ChooseDraftCaster(NativePreparedWorld.Caster);
                    Session.Draft.DirectTargetUnitId = casting.Value;
                    Session.Draft.State = CastingAuthoringState.Ready;
                    PreparedAssert(Session.AddCastingFromDraft(inputs).Applied, "could not author " + casting.Key);
                }
            }

            internal WorkspaceApplyResult Apply(CastingApplyMode mode)
            {
                WorkspaceApplyResult result = CastingRunPolicy.Apply(Session, mode, "long", World.Inputs());
                if (result.Projection != null) Steps.AddRange(result.Projection.Plan.Steps);
                int frames = 0;
                while (Host.IsRunning && frames++ < 200) Host.Pump();
                PreparedAssert(!Host.IsRunning, "fixture run did not settle");
                return result;
            }
        }

        private static void PreparedAssert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static KeyValuePair<string, string> Casting(string source, string target)
        {
            return new KeyValuePair<string, string>(source, target);
        }

        // Two castings of the SAME prepared spell from the SAME caster/book on
        // DIFFERENT recipients, then a distinct affordable successor from the
        // same book, in ONE run.
        private static void TestPreparedSlotRepeat(string root, string mode, bool linked)
        {
            var world = new NativePreparedWorld();
            world.Prepare(NativePreparedWorld.Heroism, 3, 2, linked);
            world.Prepare(NativePreparedWorld.Successor, 3, 1, false);
            var run = new PreparedSpendSession(root, mode, linked ? "linked" : "single", world,
                Casting("source-heroism", "unit-t1"), Casting("source-heroism", "unit-t2"),
                Casting("source-successor", "unit-t3"));
            WorkspaceApplyResult first = run.Apply(CastingApplyMode.ReadyCastsOnly);
            PreparedAssert(first.Allowed, "first: " + first.ReviewReason);
            CastingRunReport report = run.Host.LastReport;
            string entries = string.Join(" | ", report.Entries.Select(e => e.State + ":" + e.Detail).ToArray());
            PreparedAssert(report.Confirmed == 3 && report.Failed == 0 && report.NotProcessed == 0 &&
                report.ResourcesSpent == 3, "one run must confirm both castings and the successor: " + entries);
            List<CastStep> heroism = run.Steps.Where(s => s.Provider.Ability.Canonical == NativePreparedWorld.Heroism.Canonical).ToList();
            PreparedAssert(heroism.Count == 2 && heroism.All(s => s.Reservation.Units == (linked ? 2 : 1)) &&
                heroism.SelectMany(s => s.Reservation.TokenIds).Distinct().Count() == (linked ? 4 : 2),
                "the plan must reserve two distinct preparations");
            // The native spend does not follow the forecast: the first
            // Heroism consumed the preparation reserved for the second.
            PreparedAssert(world.Consumed[0].SequenceEqual(heroism[1].Reservation.TokenIds),
                "native order must differ from the forecast: consumed=" + string.Join(",", world.Consumed[0]));
            PreparedAssert(world.Resolutions[0] == "reserved-memorized-slot:" + heroism[0].Reservation.TokenIds[0] &&
                world.Resolutions[1].StartsWith("reconciled-memorized-slot:" + heroism[0].Reservation.TokenIds[0] +
                    ";reserved=" + heroism[1].Reservation.TokenIds[0], StringComparison.Ordinal),
                "second casting must resolve the first casting's left preparation: " + string.Join(" / ", world.Resolutions));
            PreparedAssert(world.Fired.Count == 3 && world.Fired.Distinct().Count() == 3, "duplicate submission");
            PreparedAssert(world.Counts.Take(2).Select(c => c.Substring(c.IndexOf(':') + 1)).SequenceEqual(new[] { "2>1", "1>0" }),
                "native Heroism casts must go 2 -> 1 -> 0: " + string.Join(",", world.Counts));
            PreparedAssert(world.Consumed.All(c => c.Length == (c == world.Consumed[2] ? 1 : (linked ? 2 : 1))) &&
                world.Consumed.SelectMany(c => c).Distinct().Count() == world.Consumed.Sum(c => c.Length),
                "every submission must consume exactly one preparation, never one twice");
            PreparedAssert(world.Slots.All(s => !s.Available), "a planned preparation was left unspent");
            PreparedAssert(world.Count(NativePreparedWorld.Successor) == 0, "the successor was not spent");
            // Re-running the completed plan skips the adequately active buffs
            // and spends nothing.
            WorkspaceApplyResult again = run.Apply(CastingApplyMode.ReadyCastsOnly);
            PreparedAssert(!again.Allowed && again.ReviewReason.StartsWith("nothing-to-cast:", StringComparison.Ordinal) &&
                world.Fired.Count == 3 && run.Host.StartedRuns == 1, "re-run must spend nothing: " + again.ReviewReason);
            Console.WriteLine("[PREPARED-SLOT-REPEAT] mode=" + mode + ";shape=" + (linked ? "linked-two-token" : "single") +
                ";confirmed=" + report.Confirmed + ";counts=" + string.Join(",", world.Counts) +
                ";consumed=" + string.Join("/", world.Consumed.Select(c => string.Join("+", c)).ToArray()) +
                ";second=" + world.Resolutions[1]);
        }

        // True shortage: one prepared Heroism, two castings. Cast available
        // casts what is there; Require full refuses the whole run.
        private static void TestPreparedSlotShortage(string root, string mode)
        {
            foreach (CastingApplyMode policy in new[] { CastingApplyMode.ReadyCastsOnly, CastingApplyMode.Ordinary })
            {
                var world = new NativePreparedWorld();
                world.Prepare(NativePreparedWorld.Heroism, 3, 1, true);
                world.Prepare(NativePreparedWorld.Successor, 3, 1, false);
                var run = new PreparedSpendSession(root, mode, "shortage-" + policy, world,
                    Casting("source-heroism", "unit-t1"), Casting("source-heroism", "unit-t2"),
                    Casting("source-successor", "unit-t3"));
                WorkspaceApplyResult result = run.Apply(policy);
                if (policy == CastingApplyMode.Ordinary)
                {
                    PreparedAssert(!result.Allowed && world.Fired.Count == 0 && world.Slots.All(s => s.Available),
                        "Require full must refuse a short plan before any cast: " + result.ReviewReason);
                    continue;
                }
                PreparedAssert(result.Allowed, "cast available: " + result.ReviewReason);
                CastingRunReport report = run.Host.LastReport;
                PreparedAssert(report.Confirmed == 2 && report.Failed == 0 && report.Omitted == 1 &&
                    world.Fired.Count == 2 && world.Slots.All(s => !s.Available) &&
                    world.Counts[0].EndsWith(":1>0", StringComparison.Ordinal),
                    "cast available must cast one Heroism and the successor: " +
                    string.Join(" | ", report.Entries.Select(e => e.State + ":" + e.Detail).ToArray()));
            }
        }

        // The production choice itself: what a step may resolve when its own
        // reserved slot is gone, and the exact refusals otherwise.
        private static void TestPreparedSlotChoiceContract()
        {
            Func<string, bool, bool, string, string[], PreparedSlotCandidate> slot = (id, available, main, key, linked) =>
                new PreparedSlotCandidate(id, available, main, key.StartsWith("heroism", StringComparison.Ordinal), key, linked);
            string resolution, refusal;
            var reserved = new[] { "a" };
            // Own slot available: it is the source, whatever is released.
            Assert(PreparedSlotChoice.Choose(reserved, new[] { "b" }, new[]
                { slot("a", true, true, "heroism", null), slot("b", true, true, "heroism", null) },
                out resolution, out refusal) == "a" && resolution == "reserved-memorized-slot:a");
            // Own slot spent; an earlier confirmed casting's equivalent slot is left.
            Assert(PreparedSlotChoice.Choose(reserved, new[] { "b" }, new[]
                { slot("a", false, true, "heroism", null), slot("b", true, true, "heroism", null) },
                out resolution, out refusal) == "b" && resolution.StartsWith(
                    "reconciled-memorized-slot:b;reserved=a;", StringComparison.Ordinal));
            // A future casting's or an unreserved equivalent slot is never borrowed.
            Assert(PreparedSlotChoice.Choose(reserved, new string[0], new[]
                { slot("a", false, true, "heroism", null), slot("c", true, true, "heroism", null) },
                out resolution, out refusal) == null && refusal.StartsWith(PreparedSlotChoice.ExhaustedPrefix +
                    "reserved=a;", StringComparison.Ordinal) && refusal.Contains("released-equivalent-available=0"));
            // Genuinely distinct preparations (metamagic, heighten, variant,
            // slot type, linked cost) are never interchangeable.
            foreach (string other in new[] { "heroism-empowered", "heroism-heighten-4", "heroism-favorite", "heroism-two-slot" })
                Assert(PreparedSlotChoice.Choose(reserved, new[] { "b" }, new[]
                    { slot("a", false, true, "heroism", null), slot("b", true, true, other, null) },
                    out resolution, out refusal) == null && refusal.StartsWith(PreparedSlotChoice.ExhaustedPrefix, StringComparison.Ordinal));
            Assert(PreparedSlotChoice.Choose(reserved, new[] { "b" }, new[]
                { slot("a", false, true, "heroism", null), new PreparedSlotCandidate("b", true, true, false, "heroism", null) },
                out resolution, out refusal) == null);
            // A released two-slot preparation needs every linked slot.
            Assert(PreparedSlotChoice.Choose(new[] { "a", "a2" }, new[] { "b", "b2" }, new[]
                { slot("a", false, true, "heroism", new[] { "a", "a2" }), slot("a2", false, false, "heroism", new[] { "a", "a2" }),
                  slot("b", true, true, "heroism", new[] { "b", "b2" }), slot("b2", false, false, "heroism", new[] { "b", "b2" }) },
                out resolution, out refusal) == null && refusal.StartsWith(PreparedSlotChoice.ExhaustedPrefix, StringComparison.Ordinal));
            Assert(PreparedSlotChoice.Choose(new[] { "a", "a2" }, new[] { "b", "b2" }, new[]
                { slot("a", false, true, "heroism", new[] { "a", "a2" }), slot("a2", false, false, "heroism", new[] { "a", "a2" }),
                  slot("b", true, true, "heroism", new[] { "b", "b2" }), slot("b2", true, false, "heroism", new[] { "b", "b2" }) },
                out resolution, out refusal) == "b");
            // The reserved preparation is gone from the book: missing, not exhausted.
            Assert(PreparedSlotChoice.Choose(reserved, new[] { "b" }, new[]
                { slot("a", true, true, "other-spell", null), slot("b", true, true, "heroism", null) },
                out resolution, out refusal) == null && refusal.StartsWith(PreparedSlotChoice.MissingPrefix +
                    "reserved=a", StringComparison.Ordinal));
            // Run context: only confirmed castings of the step's own pool.
            var plan = new CastStepFixture();
            CastStep step = plan.Step("pool", "a");
            Assert(ReferenceEquals(PreparedRunContext.ForStep(step, new CastStep[0]), step));
            CastStep contextual = PreparedRunContext.ForStep(step, new[] { plan.Step("pool", "b"), plan.Step("other", "c") });
            Assert(contextual.Reservation.EarlierConfirmedTokenIds.SequenceEqual(new[] { "b" }) &&
                contextual.Reservation.TokenIds.SequenceEqual(new[] { "a" }) &&
                step.Reservation.EarlierConfirmedTokenIds.Count == 0 && contextual.AssignmentId == step.AssignmentId);
            // Native spend evidence names the consumed slots, not the reserved ones.
            Func<string, bool, KeyValuePair<string, bool>> at = (id, available) => new KeyValuePair<string, bool>(id, available);
            var table = new[] { at("a", true), at("a2", true), at("b", true) };
            Assert(PreparedSpendEvidence.Describe(table, new[] { at("a", false), at("a2", false), at("b", true) }) ==
                "prepared-consumed:a,a2");
            Assert(PreparedSpendEvidence.Describe(table, table) == "prepared-consumed:none");
            Assert(PreparedSpendEvidence.Describe(table, null) == "prepared-consumed:unread");
            Assert(PreparedSpendEvidence.Describe(null, null) == "prepared-consumed:not-prepared");
            Assert(PreparedSpendEvidence.Describe(new[] { at("a", false) }, new[] { at("a", true) }) ==
                "prepared-consumed:none;prepared-restored:a");
        }

        private sealed class CastStepFixture
        {
            private int _next;
            internal CastStep Step(string pool, string token)
            {
                return new CastStep("source", "casting-" + (++_next), new ProviderKey("unit-wizard", "book",
                    NativePreparedWorld.Heroism, "level-3|heighten-0"), "unit-t1", new[] { "unit-t1" }, null,
                    new ResourceReservation(pool, 1, new[] { token }), null, Leaf("buff"), false,
                    CastExecutionStrategy.DirectRuleCast, "fixture");
            }
        }
    }
}
