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
    // Casting-graph workspace (addendum v1.1, G02-G09, G12): the production
    // CastingWorkspaceSession, authoring service, compiler, capacity ledger
    // and repository are the code under test; only the game boundary (party,
    // providers, pools, effects, enhancements) is a deterministic fixture.
    internal static partial class Program
    {
        private static void RunCastingGraphTests(string root)
        {
            Run("graph-capacity-comes-from-the-plan-ledger", TestGraphCapacityFromPlanLedger);
            Run("graph-capacity-text-says-what-is-left", TestGraphCapacityText);
            Run("graph-buff-caster-target-creates-one-casting", () => TestGraphCreatesOneCasting(root));
            Run("graph-selection-and-focus-never-mutate", () => TestGraphSelectionNeverMutates(root));
            Run("graph-two-casters-three-targets-edit-one", () => TestGraphTwoCastersThreeTargets(root));
            Run("graph-multiple-sources-exact-counts", () => TestGraphMultipleSources(root));
            Run("graph-spontaneous-pool-propagates-across-buffs",
                () => TestGraphSpontaneousPoolAcrossBuffs(root));
            Run("graph-shared-enhancement-pool-blocks-atomically",
                () => TestGraphSharedEnhancementPool(root));
            Run("graph-enhancement-edits-one-casting-and-budgets",
                () => TestGraphEnhancementEditsOneCasting(root));
            Run("graph-parallel-castings-stay-distinct", () => TestGraphParallelCastings(root));
            Run("graph-group-casting-one-origin-derived-branches", () => TestGraphGroupCasting(root));
            Run("graph-target-legality-and-refusals", () => TestGraphTargetLegality(root));
            Run("graph-save-reload-reconstructs-connections", () => TestGraphSaveReload(root));
            Run("graph-unresolved-casting-keeps-its-connection", () => TestGraphUnresolvedCasting(root));
            Run("graph-layout-chips-never-overlap", TestGraphLayoutGeometry);
        }

        private const string GraphSourceA = "source-graph-a";
        private const string GraphSourceB = "source-graph-b";
        private const string GraphSourceC = "source-graph-c";
        private const string GraphSourceD = "source-graph-d";
        private const string GraphSourceM = "source-graph-m";
        private static readonly AbilityKey GraphAbilityA = Ability("aa000000000000000000000000000001", string.Empty, 0);
        private static readonly AbilityKey GraphAbilityB = Ability("bb000000000000000000000000000001", string.Empty, 0);
        private static readonly AbilityKey GraphAbilityC = Ability("cc000000000000000000000000000001", string.Empty, 0);
        private static readonly AbilityKey GraphAbilityD = Ability("dd000000000000000000000000000001", string.Empty, 0);
        private static readonly AbilityKey GraphAbilityM = new AbilityKey(
            "ee000000000000000000000000000001", string.Empty, 0, SourceKind.AbilityResource, "mutagen");

        private static readonly string[] GraphUnits =
            { "unit-bard", "unit-cleric", "unit-alch", "unit-t1", "unit-t2", "unit-t3", "unit-t4" };

        // Party: a bard (spontaneous: A and B share its level-2 pool, A also
        // from a second spellbook, cantrip D unlimited), a cleric (prepared:
        // two exact A slots and one B slot with a diamond it lacks; group C
        // from a spontaneous pool), an alchemist (Mutagen: one use, self
        // only), and recipients t1-t4 (t3 out of B's reach, t4 unconscious).
        private static CastingWorkspaceInputs GraphInputs(int bardLevel2 = 4, int reservoirUses = 1,
            int rodUses = 3, int diamonds = 0)
        {
            var units = GraphUnits.Select(id => new UnitSnapshot(id, DisplayOf(id), false, string.Empty,
                new TargetValidationSnapshot(true, id != "unit-t4", true, true))).ToList();
            var pools = new List<ResourcePoolSnapshot>
            {
                new ResourcePoolSnapshot("bard-l2", ResourcePoolKind.SpontaneousLevel, bardLevel2, bardLevel2, null),
                new ResourcePoolSnapshot("bard2-l1", ResourcePoolKind.SpontaneousLevel, 2, 2, null),
                new ResourcePoolSnapshot("bard-cantrips", ResourcePoolKind.Unlimited, 0, 0, null),
                new ResourcePoolSnapshot("cleric-prepared", ResourcePoolKind.PreparedSlots, 3, 3, new[]
                {
                    new ResourceTokenSnapshot("c-a1", GraphAbilityA, 2, PreparedSlotKind.Common, true, true, null),
                    new ResourceTokenSnapshot("c-a2", GraphAbilityA, 2, PreparedSlotKind.Common, true, true, null),
                    new ResourceTokenSnapshot("c-b1", GraphAbilityB, 2, PreparedSlotKind.Common, true, true, null)
                }),
                new ResourcePoolSnapshot("cleric-group", ResourcePoolKind.SpontaneousLevel, 2, 2, null),
                new ResourcePoolSnapshot("alch-mutagen", ResourcePoolKind.AbilityResource, 1, 1, null)
            };
            ProviderSnapshot bardA = GraphProvider("unit-bard", "book-bard", GraphAbilityA, "level-2", 2, "bard-l2", 1, null, "Bard");
            ProviderSnapshot bardA2 = GraphProvider("unit-bard", "book-bard-2", GraphAbilityA, "level-1", 1, "bard2-l1", 1, null, "Sorcerer");
            ProviderSnapshot bardB = GraphProvider("unit-bard", "book-bard", GraphAbilityB, "level-2", 2, "bard-l2", 1, null, "Bard");
            ProviderSnapshot bardD = GraphProvider("unit-bard", "book-bard", GraphAbilityD, "level-0", 0, "bard-cantrips", 0, null, "Bard");
            ProviderSnapshot clericA = GraphProvider("unit-cleric", "book-cleric", GraphAbilityA, "level-2", 2, "cleric-prepared", 1,
                new[] { "c-a1", "c-a2" }, "Cleric");
            ProviderSnapshot clericB = new ProviderSnapshot(new ProviderKey("unit-cleric", "book-cleric", GraphAbilityB, "level-2"),
                "Buff B", 2, "cleric-prepared", 1, new[] { "c-b1" },
                new MaterialRequirementSnapshot("diamond-dust", 1, diamonds), 5, 100, string.Empty, string.Empty, "Buff B", 0, "Cleric");
            ProviderSnapshot clericC = GraphProvider("unit-cleric", "book-cleric-group", GraphAbilityC, "level-3", 3, "cleric-group", 1, null, "Cleric");
            ProviderSnapshot alchM = new ProviderSnapshot(new ProviderKey("unit-alch", string.Empty, GraphAbilityM, "mutagen"),
                "Mutagen", 0, "alch-mutagen", 1, null, null, 5, 100, string.Empty, string.Empty, "Mutagen", 0, string.Empty);
            var snapshot = new PartyProviderSnapshot(units,
                new[] { bardA, bardA2, bardB, bardD, clericA, clericB, clericC, alchM }, pools);
            string[] all = GraphUnits;
            string[] exceptT3 = all.Where(id => id != "unit-t3").ToArray();
            var options = new List<ProviderPlanningOption>
            {
                new ProviderPlanningOption(bardA, all, new[] { "unit-bard" }, 5, 100),
                new ProviderPlanningOption(bardA2, all, new[] { "unit-bard" }, 5, 100),
                new ProviderPlanningOption(bardB, exceptT3, new[] { "unit-bard" }, 5, 100),
                new ProviderPlanningOption(bardD, all, new[] { "unit-bard" }, 5, 100),
                new ProviderPlanningOption(clericA, all, new[] { "unit-cleric" }, 5, 100),
                new ProviderPlanningOption(clericB, exceptT3, new[] { "unit-cleric" }, 5, 100),
                new ProviderPlanningOption(clericC, all, new[] { "unit-cleric", "unit-t1" }, 5, 100,
                    CastExecutionStrategy.DirectRuleCast, "fixture-direct",
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
                    {
                        { "unit-cleric", new[] { "unit-cleric", "unit-bard", "unit-t1", "unit-t2" } },
                        { "unit-t1", new[] { "unit-t1", "unit-t3" } }
                    }),
                new ProviderPlanningOption(alchM, new[] { "unit-alch" }, new[] { "unit-alch" }, 5, 100)
            };
            EffectExpression a = Leaf("graph-a");
            EffectExpression b = Leaf("graph-b");
            EffectExpression c = new EffectLeafExpression(EffectKind.Buff, "graph-c", EffectTarget.Party,
                "fixture", "fixture/graph-c");
            EffectExpression d = Leaf("graph-d");
            EffectExpression m = Leaf("graph-m");
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { GraphSourceA, a }, { GraphAbilityA.Canonical, a },
                { GraphSourceB, b }, { GraphAbilityB.Canonical, b },
                { GraphSourceC, c }, { GraphAbilityC.Canonical, c },
                { GraphSourceD, d }, { GraphAbilityD.Canonical, d },
                { GraphSourceM, m }, { GraphAbilityM.Canonical, m }
            };
            var enhancements = new List<CastEnhancementSnapshot>
            {
                new CastEnhancementSnapshot("rod-extend-bard", "unit-bard", "rod-extend",
                    "Lesser Metamagic Rod of Extend", string.Empty, CastEnhancementCategory.MetamagicRod,
                    ExistingEffectSufficiency.ExtendMetamagicFlag, 3, rodUses, null, "Extend Spell",
                    null, "rod-extend-bard", false, null, 1, false, null, "Rod charges"),
                new CastEnhancementSnapshot("rod-empower-bard", "unit-bard", "rod-empower",
                    "Lesser Metamagic Rod of Empower", string.Empty, CastEnhancementCategory.MetamagicRod,
                    1, 3, 1, null, "Empower Spell", null, "rod-empower-bard", false, null, 1, false, null,
                    "Rod charges"),
                new CastEnhancementSnapshot("reservoir-extend", "unit-bard", "reservoir-extend-bp",
                    "Arcane Reservoir: Extend", "Spends reservoir to extend.", CastEnhancementCategory.ClassFeature,
                    0, 9, reservoirUses, new[] { GraphAbilityA.BaseAbilityGuid }, "Reservoir Extend",
                    new[] { "book-bard" }, "arcane-reservoir", false, "reservoir-extend", 1, false, null,
                    "Arcane reservoir"),
                new CastEnhancementSnapshot("reservoir-potent", "unit-bard", "reservoir-potent-bp",
                    "Arcane Reservoir: Potent Magic", "Spends reservoir for +2 caster level.",
                    CastEnhancementCategory.ClassFeature, 0, 9, reservoirUses,
                    new[] { GraphAbilityA.BaseAbilityGuid }, "Potent Magic", new[] { "book-bard" },
                    "arcane-reservoir", false, "reservoir-potent", 1, false, null, "Arcane reservoir"),
                new CastEnhancementSnapshot("powerful-change-cleric", "unit-cleric", "powerful-change-bp",
                    "Powerful Change", "The buff's bonus increases by 2.", CastEnhancementCategory.ClassFeature,
                    0, 9, 2, new[] { GraphAbilityA.BaseAbilityGuid }, "Powerful Change",
                    new[] { "book-cleric" }, "powerful-change", false, null, 1, false, null, "Powerful Change")
            };
            return new CastingWorkspaceInputs(snapshot, options, effects, enhancements);
        }

        private static string DisplayOf(string unitId)
        {
            switch (unitId)
            {
                case "unit-bard": return "Linzi";
                case "unit-cleric": return "Harrim";
                case "unit-alch": return "Jaethal";
                case "unit-t1": return "Valerie";
                case "unit-t2": return "Amiri";
                case "unit-t3": return "Octavia";
                default: return "Tristian";
            }
        }

        private static ProviderSnapshot GraphProvider(string caster, string book, AbilityKey ability,
            string instance, int level, string pool, int units, string[] tokens, string bookName)
        {
            return new ProviderSnapshot(new ProviderKey(caster, book, ability, instance), "Buff",
                level, pool, units, tokens, null, 5, 100, string.Empty, string.Empty, "Buff", 0, bookName);
        }

        private static string GraphProviderKey(string caster, string book, AbilityKey ability, string instance)
        {
            return new ProviderKey(caster, book, ability, instance).Canonical;
        }

        private static CastingWorkspaceSession GraphSession(string root, string name,
            out CastingWorkspaceInputs inputs, int bardLevel2 = 4, int reservoirUses = 1, int rodUses = 3)
        {
            string dir = Path.Combine(root, "graph-" + name);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            inputs = GraphInputs(bardLevel2, reservoirUses, rodUses);
            return new CastingWorkspaceSession(dir, "graph-campaign");
        }

        private static void Expect(bool condition, string failure)
        {
            if (!condition) throw new InvalidOperationException(failure);
        }

        private static CastingGraphSourceRow Row(CastingGraphView view, string caster, string providerKey)
        {
            CastingGraphCasterNode node = view.CasterById(caster);
            Expect(node != null, "caster missing from the lane: " + caster);
            CastingGraphSourceRow row = node.Sources.FirstOrDefault(value => value.ProviderKey == providerKey);
            Expect(row != null, "source row missing: " + providerKey);
            return row;
        }

        // Adds one direct casting through the real graph gestures.
        private static string GraphAdd(CastingWorkspaceSession session, CastingWorkspaceInputs inputs,
            string source, string caster, string providerKey, string target)
        {
            session.SelectGraphBuff(source, inputs);
            session.SelectGraphCaster(caster, inputs);
            if (providerKey != null) Expect(session.SelectGraphSource(providerKey, inputs).Applied,
                "source refused: " + providerKey);
            CastingGraphEditResult added = session.AddGraphCasting(target, inputs);
            Expect(added.Applied && added.CastingId != null, "add refused: " +
                (added.Edit == null ? "?" : added.Edit.Reason));
            return added.CastingId;
        }

        private static void TestGraphCapacityFromPlanLedger()
        {
            CastingWorkspaceInputs inputs = GraphInputs(bardLevel2: 4, reservoirUses: 1);
            var service = new CastingAuthoringService(CastingDocument());
            Assert(service.AddCasting(DirectCasting("cast-1", "long", "unit-bard", "unit-t1",
                GraphSourceA, GraphAbilityA, null, "book-bard")).Applied);
            Assert(service.AddCasting(DirectCasting("cast-2", "short", "unit-bard", "unit-t2",
                GraphSourceB, GraphAbilityB, null, "book-bard")).Applied);
            Assert(service.AddCasting(DirectCasting("cast-3", "important", "unit-cleric", "unit-t1",
                GraphSourceA, GraphAbilityA, null, "book-cleric")).Applied);
            // Regression (graph review): a required material the party has
            // NONE of must block the casting; an untracked zero never did.
            Assert(service.AddCasting(DirectCasting("cast-4", "important", "unit-cleric", "unit-t2",
                GraphSourceB, GraphAbilityB, null, "book-cleric")).Applied);
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(service.Document,
                inputs.Snapshot, inputs.ProviderOptions, inputs.EffectsBySource, inputs.Enhancements);
            Expect(plan.Capacity != null, "a compiled plan carries no capacity");
            ResolvedCasting noDiamond = plan.CastingById("cast-4");
            Expect(noDiamond.Readiness == ResolvedCastingReadiness.Blocked && noDiamond.Cost.Count == 0 &&
                noDiamond.ReadinessReasons.Any(reason => reason.StartsWith("material-unavailable:diamond-dust",
                    StringComparison.Ordinal)), "a casting without its material component was not blocked");
            string budgetBefore = string.Join("|", plan.BudgetLines.Select(line =>
                line.PoolKey + ":" + line.RequestedUsage + ":" + line.AllocatedUsage).ToArray());
            ProviderSnapshot ProviderOf(string caster, AbilityKey ability, string book) =>
                inputs.Snapshot.Providers.First(value => value.Key.CasterUnitId == caster &&
                    value.Key.Ability.Canonical == ability.Canonical && value.Key.SpellbookGuid == book);
            // Spontaneous: A and B share the bard's level-2 pool; two castings
            // leave two, and each source reports the SHARED two (never a sum).
            CastingCapacityEstimate bardA = plan.Capacity.AdditionalCastings(ProviderOf("unit-bard", GraphAbilityA, "book-bard"));
            CastingCapacityEstimate bardB = plan.Capacity.AdditionalCastings(ProviderOf("unit-bard", GraphAbilityB, "book-bard"));
            Expect(bardA.Kind == CastingCapacityKind.Finite && bardA.AdditionalCastings == 2 &&
                bardA.NativeRemaining == 2 && bardA.NativeAvailableNow == 4 && !bardA.IsLowerBound,
                "the bard's shared level-2 pool does not report 2 of 4 after two castings");
            Expect(bardB.AdditionalCastings == 2, "a second buff on the same pool did not see the shared balance");
            // The other spellbook is its own pool.
            Expect(plan.Capacity.AdditionalCastings(ProviderOf("unit-bard", GraphAbilityA, "book-bard-2"))
                .AdditionalCastings == 2, "a separate spellbook pool was charged");
            // Prepared: two exact A slots, one used.
            CastingCapacityEstimate clericA = plan.Capacity.AdditionalCastings(ProviderOf("unit-cleric", GraphAbilityA, "book-cleric"));
            Expect(clericA.AdditionalCastings == 1 && clericA.LimitingCategory == CastingCostCategory.NativePool,
                "the cleric's exact A slots do not report 1 left");
            // Material: the diamond the cleric lacks blocks B before its slot.
            CastingCapacityEstimate clericB = plan.Capacity.AdditionalCastings(ProviderOf("unit-cleric", GraphAbilityB, "book-cleric"));
            Expect(clericB.AdditionalCastings == 0 && clericB.LimitingCategory == CastingCostCategory.Material,
                "a missing material component did not block");
            // Unlimited cantrip, and an ability pool.
            Expect(plan.Capacity.AdditionalCastings(ProviderOf("unit-bard", GraphAbilityD, "book-bard")).Kind ==
                CastingCapacityKind.Unlimited, "a verified unlimited source is not unlimited");
            Expect(plan.Capacity.AdditionalCastings(ProviderOf("unit-alch", GraphAbilityM, string.Empty))
                .AdditionalCastings == 1, "the one Mutagen use was not reported");
            // Enhancements: two reservoir features need two uses from one pool
            // that has one: combined demand refuses atomically.
            CastEnhancementSnapshot extend = inputs.Enhancements.First(value => value.EnhancementId == "reservoir-extend");
            CastEnhancementSnapshot potent = inputs.Enhancements.First(value => value.EnhancementId == "reservoir-potent");
            CastingCapacityEstimate both = plan.Capacity.AdditionalCastings(
                ProviderOf("unit-bard", GraphAbilityA, "book-bard"), new[] { extend, potent });
            Expect(both.AdditionalCastings == 0 && both.LimitingCategory == CastingCostCategory.EnhancementPool &&
                both.LimitingPoolKey == "arcane-reservoir", "combined reservoir demand was not refused on its pool");
            Expect(plan.Capacity.AdditionalCastings(ProviderOf("unit-bard", GraphAbilityA, "book-bard"),
                new[] { extend }).AdditionalCastings == 1, "one reservoir use did not fund one casting");
            // An unverified zero cost on a finite pool is unknown, not free.
            var zero = new ProviderSnapshot(new ProviderKey("unit-bard", "book-bard", GraphAbilityB, "zero"),
                "Zero", 2, "bard-l2", 0, null);
            Expect(plan.Capacity.AdditionalCastings(zero).Kind == CastingCapacityKind.Unknown,
                "a zero cost on a finite pool was counted");
            // Asking never changes the plan or the next answer.
            string budgetAfter = string.Join("|", plan.BudgetLines.Select(line =>
                line.PoolKey + ":" + line.RequestedUsage + ":" + line.AllocatedUsage).ToArray());
            Expect(budgetBefore == budgetAfter && plan.Capacity.AdditionalCastings(
                ProviderOf("unit-bard", GraphAbilityA, "book-bard")).AdditionalCastings == 2,
                "a capacity question changed the plan");
        }

        private static void TestGraphCapacityText()
        {
            Func<CastingCapacityKind, int, bool, CastingCostCategory?, int?, CastingCapacityEstimate> make =
                (kind, count, lower, category, available) => new CastingCapacityEstimate(kind, count, lower,
                    category, null, null, null, available, null);
            Expect(CastingGraphText.Capacity(make(CastingCapacityKind.Finite, 3, false,
                    CastingCostCategory.NativePool, 4), ResourcePoolKind.SpontaneousLevel, 1) ==
                "3 of 4 casts remaining", "spontaneous text");
            Expect(CastingGraphText.Capacity(make(CastingCapacityKind.Finite, 1, false,
                    CastingCostCategory.NativePool, 2), ResourcePoolKind.PreparedSlots, 1) ==
                "1 exact slot ready", "prepared text");
            Expect(CastingGraphText.Capacity(make(CastingCapacityKind.Finite, 0, false,
                    CastingCostCategory.NativePool, 1), ResourcePoolKind.AbilityResource, 1) ==
                "0 / 1 remaining", "exhausted ability text");
            Expect(CastingGraphText.Capacity(make(CastingCapacityKind.Unlimited, 0, false, null, null),
                ResourcePoolKind.Unlimited, 0) == "Unlimited", "unlimited text");
            Expect(CastingGraphText.Capacity(make(CastingCapacityKind.Finite, 0, false,
                    CastingCostCategory.Material, 1), ResourcePoolKind.PreparedSlots, 1) ==
                "Blocked: material unavailable", "material text");
            Expect(CastingGraphText.Capacity(make(CastingCapacityKind.Unknown, 0, false, null, null),
                ResourcePoolKind.SpontaneousLevel, 0) == "Remaining unknown", "unknown text");
            Expect(CastingGraphText.Capacity(make(CastingCapacityKind.Finite, 99, true,
                    null, 250), ResourcePoolKind.ItemCharges, 1) == "99+ casts remaining", "lower bound text");
        }

        // G02: select buff -> caster -> exact source -> target: one casting,
        // one connection, focused; nothing before the target click mutates.
        private static void TestGraphCreatesOneCasting(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "g02", out inputs);
            CastingPlanDocument before = session.Document;
            session.SelectGraphBuff(GraphSourceA, inputs);
            session.SelectGraphCaster("unit-bard", inputs);
            CastingGraphView view = session.BuildGraph(inputs);
            // Two materially distinct sources: nothing is chosen for the player.
            Expect(view.SelectedProviderKey == null && view.CasterById("unit-bard").Sources.Count == 2,
                "a caster with two sources had one chosen silently");
            Expect(view.Targets.All(target => target.Legality == CastingGraphTargetLegality.Unknown),
                "targets claimed legality before a source was chosen");
            CastingGraphEditResult ambiguous = session.AddGraphCasting("unit-t1", inputs);
            Expect(!ambiguous.Applied && ambiguous.Edit.Reason.StartsWith("draft-ability-unresolved:exact-source-ambiguous",
                StringComparison.Ordinal), "an ambiguous source was not refused");
            string key = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            Expect(session.SelectGraphSource(key, inputs).Applied, "source choice refused");
            view = session.BuildGraph(inputs);
            Expect(view.SelectedProviderKey == key && Row(view, "unit-bard", key).Selected,
                "the chosen source is not shown chosen");
            Expect(view.TargetById("unit-t1").Legality == CastingGraphTargetLegality.Legal,
                "a reachable target is not legal");
            Expect(ReferenceEquals(before, session.Document), "selection mutated the document");
            CastingGraphEditResult added = session.AddGraphCasting("unit-t1", inputs);
            Expect(added.Applied && session.Document.Castings.Count == 1, "the target click did not add one casting");
            PlannedCasting casting = session.Document.Castings[0];
            Expect(casting.CasterUnitId == "unit-bard" && casting.SpellbookGuid == "book-bard" &&
                casting.Ability.Canonical == GraphAbilityA.Canonical && casting.DirectTargetUnitId == "unit-t1" &&
                casting.TargetMode == CastingTargetMode.DirectTarget && casting.Enhancements.Count == 0 &&
                casting.State == CastingAuthoringState.Ready && casting.RoutineId == "long",
                "the casting is not exactly bard/Bard book/A -> t1");
            Expect(session.EditingFocusCastingId == casting.CastingId, "focus did not move to the new casting");
            view = session.BuildGraph(inputs);
            Expect(view.Castings.Count == 1 && view.Castings[0].SourceProviderKey == key &&
                view.Castings[0].TargetUnitId == "unit-t1" && view.Castings[0].Selected &&
                view.Inspector != null && view.Inspector.CastingId == casting.CastingId,
                "the graph does not show one selected connection with its inspector");
            GraphLayoutResult layout = CastingGraphLayout.Compute(view, new GraphLayoutMetrics());
            GraphConnection connection = layout.ConnectionFor(casting.CastingId);
            GraphLayoutMetrics m = new GraphLayoutMetrics();
            float rowCentre = layout.SourceRowTops[CastingGraphLayout.SourceRowKey("unit-bard", key)] +
                m.SourceRowHeight / 2f;
            Expect(connection != null && Math.Abs(connection.Inbound.From.Y - rowCentre) < 0.01f &&
                Math.Abs(connection.Inbound.From.X - m.CasterLaneWidth) < 0.01f &&
                Math.Abs(connection.Outbound.To.X - m.TargetLaneLeft) < 0.01f,
                "the connection does not run from the source row to the target");
            // The source row now reports what the whole plan leaves.
            Expect(Row(view, "unit-bard", key).CapacityText == "3 of 4 casts remaining",
                "the source count did not drop to 3 of 4: " + Row(view, "unit-bard", key).CapacityText);
        }

        private static void TestGraphSelectionNeverMutates(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "focus", out inputs);
            string first = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t1");
            string signature = session.DocumentIntentSignature();
            CastingPlanDocument document = session.Document;
            session.SelectGraphBuff(GraphSourceB, inputs);
            session.SelectGraphCaster("unit-bard", inputs);
            session.SelectGraphSource(GraphProviderKey("unit-bard", "book-bard", GraphAbilityB, "level-2"), inputs);
            session.SelectRoutine("short");
            session.BuildGraph(inputs);
            session.FocusGraphCasting(first);
            CastingGraphView view = session.BuildGraph(inputs);
            session.ClearGraphFocus();
            session.BuildGraph(inputs);
            Expect(ReferenceEquals(document, session.Document) && signature == session.DocumentIntentSignature(),
                "browsing, focus or preview mutated the document");
            // Focusing a casting shows it where it lives (its buff and routine).
            Expect(view.SelectedSourceId == GraphSourceA && view.SelectedRoutineId == "long" &&
                view.Inspector != null && view.Inspector.CastingId == first,
                "focus did not bring the casting's buff and routine into view");
            // The cleric has one source for A: choosing the caster chose it.
            session.SelectGraphBuff(GraphSourceA, inputs);
            session.SelectGraphCaster("unit-cleric", inputs);
            Expect(session.BuildGraph(inputs).SelectedProviderKey ==
                GraphProviderKey("unit-cleric", "book-cleric", GraphAbilityA, "level-2"),
                "a caster's single source was not chosen with the caster");
        }

        // G03: two casters, three targets; one enhancement edit touches one.
        private static void TestGraphTwoCastersThreeTargets(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "g03", out inputs);
            string bardKey = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            string one = GraphAdd(session, inputs, GraphSourceA, "unit-bard", bardKey, "unit-t1");
            string two = GraphAdd(session, inputs, GraphSourceA, "unit-bard", bardKey, "unit-t2");
            string three = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t3");
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(view.Castings.Count == 3 && view.Castings.Select(value => value.CastingId).Distinct().Count() == 3,
                "three recipients are not three castings");
            Expect(CastingGraphLayout.Compute(view, null).Connections.Count == 3, "three castings are not three connections");
            PlannedCasting oneBefore = session.Document.Castings.First(value => value.CastingId == one);
            PlannedCasting threeBefore = session.Document.Castings.First(value => value.CastingId == three);
            session.FocusGraphCasting(two);
            AuthoringEditResult edit = session.ToggleFocusedEnhancement("rod-extend-bard", inputs);
            Expect(edit.Applied && edit.AffectedCastingIds.Count == 1 && edit.AffectedCastingIds[0] == two,
                "the enhancement edit did not disclose exactly one casting");
            Expect(ReferenceEquals(oneBefore, session.Document.Castings.First(value => value.CastingId == one)) &&
                ReferenceEquals(threeBefore, session.Document.Castings.First(value => value.CastingId == three)),
                "a sibling casting changed");
            Expect(session.Document.Castings.First(value => value.CastingId == two).Enhancements
                .Select(value => value.EnhancementId).SequenceEqual(new[] { "rod-extend-bard" }),
                "the edited casting did not get exactly the rod");
            view = session.BuildGraph(inputs);
            Expect(view.CastingById(two).EnhancementBadges.SequenceEqual(new[] { "Extend Spell" }) &&
                view.CastingById(one).EnhancementBadges.Count == 0, "the chips do not show per-casting enhancements");
        }

        // G04: exact source rows, no summed caster total, no double counting.
        private static void TestGraphMultipleSources(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "g04", out inputs);
            string bardKey = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            string sorcKey = GraphProviderKey("unit-bard", "book-bard-2", GraphAbilityA, "level-1");
            session.SelectGraphBuff(GraphSourceA, inputs);
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(Row(view, "unit-bard", bardKey).Label == "Bard level 2" &&
                Row(view, "unit-bard", sorcKey).Label == "Sorcerer level 1",
                "source rows do not name the exact spellbook and level");
            Expect(Row(view, "unit-bard", bardKey).CapacityText == "4 of 4 casts remaining" &&
                Row(view, "unit-bard", sorcKey).CapacityText == "2 of 2 casts remaining" &&
                Row(view, "unit-cleric", GraphProviderKey("unit-cleric", "book-cleric", GraphAbilityA, "level-2"))
                    .CapacityText == "2 exact slots ready", "source rows do not report their own pools");
            Expect(Row(view, "unit-bard", bardKey).PoolLabel == "level 2 spell slot",
                "the controlling pool is not named");
            GraphAdd(session, inputs, GraphSourceA, "unit-bard", sorcKey, "unit-t1");
            view = session.BuildGraph(inputs);
            Expect(Row(view, "unit-bard", sorcKey).CapacityText == "1 of 2 casts remaining" &&
                Row(view, "unit-bard", bardKey).CapacityText == "4 of 4 casts remaining",
                "one source's casting was charged to the other");
            Expect(view.CasterById("unit-alch") == null, "a caster who cannot cast the buff is listed as capable");
        }

        // G05: a level-2 casting of A reduces what is left for B on the same
        // pool; removing or disabling it restores the count.
        private static void TestGraphSpontaneousPoolAcrossBuffs(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "g05", out inputs, bardLevel2: 2);
            string bKey = GraphProviderKey("unit-bard", "book-bard", GraphAbilityB, "level-2");
            string aKey = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            session.SelectGraphBuff(GraphSourceB, inputs);
            Expect(Row(session.BuildGraph(inputs), "unit-bard", bKey).CapacityText == "2 of 2 casts remaining",
                "B does not start with the whole shared pool");
            string a = GraphAdd(session, inputs, GraphSourceA, "unit-bard", aKey, "unit-t1");
            session.SelectGraphBuff(GraphSourceB, inputs);
            Expect(Row(session.BuildGraph(inputs), "unit-bard", bKey).CapacityText == "1 of 2 casts remaining",
                "a level-2 casting of A did not reduce B's remaining level-2 capacity");
            // Another routine's casting spends the same pool in one pass.
            session.SelectRoutine("short");
            string a2 = GraphAdd(session, inputs, GraphSourceA, "unit-bard", aKey, "unit-t2");
            session.SelectGraphBuff(GraphSourceB, inputs);
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(Row(view, "unit-bard", bKey).CapacityText == "0 / 2 remaining" &&
                !Row(view, "unit-bard", bKey).Usable, "the one-pass pool across routines is not exhausted for B");
            // Disabling one restores one; removing the other restores the rest.
            session.FocusGraphCasting(a);
            Expect(session.SetFocusedCastingState(CastingAuthoringState.Disabled).Applied, "disable refused");
            session.SelectGraphBuff(GraphSourceB, inputs);
            Expect(Row(session.BuildGraph(inputs), "unit-bard", bKey).CapacityText == "1 of 2 casts remaining",
                "disabling a casting did not restore capacity");
            session.FocusGraphCasting(a2);
            Expect(session.RemoveFocusedCasting().Applied, "remove refused");
            session.SelectGraphBuff(GraphSourceB, inputs);
            Expect(Row(session.BuildGraph(inputs), "unit-bard", bKey).CapacityText == "2 of 2 casts remaining",
                "removing a casting did not restore capacity");
            Expect(session.Undo(), "undo refused");
            session.SelectGraphBuff(GraphSourceB, inputs);
            Expect(Row(session.BuildGraph(inputs), "unit-bard", bKey).CapacityText == "1 of 2 casts remaining",
                "undo of the removal did not take the capacity back");
        }

        // G06: two enhancements on one class-resource pool: combined demand
        // blocks atomically (the native slot is not reserved either), and
        // spending the pool elsewhere updates every other choice.
        private static void TestGraphSharedEnhancementPool(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "g06", out inputs, bardLevel2: 4, reservoirUses: 1);
            string aKey = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            string one = GraphAdd(session, inputs, GraphSourceA, "unit-bard", aKey, "unit-t1");
            Expect(session.ToggleFocusedEnhancement("reservoir-extend", inputs).Applied, "first reservoir refused");
            Expect(session.ToggleFocusedEnhancement("reservoir-potent", inputs).Applied,
                "a verified, compatible second reservoir feature was refused at authoring");
            CastingGraphView view = session.BuildGraph(inputs);
            CastingGraphCasting chip = view.CastingById(one);
            Expect(chip.Readiness == ResolvedCastingReadiness.Blocked,
                "combined demand above the pool did not block the casting");
            Expect(Row(view, "unit-bard", aKey).CapacityText == "4 of 4 casts remaining",
                "a blocked casting reserved its native slot (not atomic)");
            Expect(view.Inspector.Reasons.Any(reason => reason.IndexOf("enhancement pool exhausted",
                StringComparison.OrdinalIgnoreCase) >= 0 || reason.IndexOf("enhancement", StringComparison.OrdinalIgnoreCase) >= 0),
                "the inspector does not explain the enhancement shortage");
            // One feature fits: ready, and the pool is spent for everyone else.
            Expect(session.ToggleFocusedEnhancement("reservoir-potent", inputs).Applied, "removal refused");
            string two = GraphAdd(session, inputs, GraphSourceA, "unit-bard", aKey, "unit-t2");
            view = session.BuildGraph(inputs);
            Expect(view.CastingById(one).Readiness == ResolvedCastingReadiness.Ready,
                "one reservoir use did not fund the casting");
            CastingGraphEnhancementOption other = view.Inspector.Enhancements.First(value =>
                value.EnhancementId == "reservoir-extend");
            Expect(view.Inspector.CastingId == two && other.PoolRemaining == 0 &&
                other.UnavailableReason.StartsWith("Not enough uses left", StringComparison.Ordinal),
                "the shared pool spent by one casting still looks available to another");
        }

        // G07: the inspector lists only verified enhancements of this casting's
        // caster and source, with their budget; a change edits one casting,
        // updates the budgets and undoes.
        private static void TestGraphEnhancementEditsOneCasting(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "g07", out inputs, rodUses: 1);
            string aKey = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            string one = GraphAdd(session, inputs, GraphSourceA, "unit-bard", aKey, "unit-t1");
            string cleric = GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t2");
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(view.Inspector.CastingId == cleric &&
                view.Inspector.Enhancements.Select(value => value.EnhancementId)
                    .SequenceEqual(new[] { "powerful-change-cleric" }),
                "the cleric's casting offers another caster's enhancements");
            CastingGraphEnhancementOption powerful = view.Inspector.Enhancements[0];
            Expect(powerful.Name == "Powerful Change" && powerful.Mechanism == "Class feature" &&
                powerful.CanAdd && powerful.ExpectedEffect == "The buff's bonus increases by 2." &&
                powerful.BudgetText == "Powerful Change: 2 of 2 left after the whole plan",
                "Powerful Change is not described with its mechanism, effect and budget");
            Expect(session.ToggleFocusedEnhancement("powerful-change-cleric", inputs).Applied, "Powerful Change refused");
            Expect(session.Document.Castings.First(value => value.CastingId == one).Enhancements.Count == 0,
                "the other casting changed");
            session.FocusGraphCasting(one);
            view = session.BuildGraph(inputs);
            CastingGraphEnhancementOption rod = view.Inspector.Enhancements.First(value => value.EnhancementId == "rod-extend-bard");
            Expect(rod.Name == "Extend Spell" && rod.Mechanism == "Metamagic rod" &&
                rod.ExpectedEffect == "Duration x2." && rod.CanAdd, "the rod is not described as a duration doubling");
            Expect(session.ToggleFocusedEnhancement("rod-extend-bard", inputs).Applied, "rod refused");
            // A second rod on the same casting is refused with its reason,
            // never swapped for the chosen one.
            AuthoringEditResult second = session.ToggleFocusedEnhancement("rod-empower-bard", inputs);
            Expect(!second.Applied && second.Reason.StartsWith("enhancement-incompatible:", StringComparison.Ordinal) &&
                session.Document.Castings.First(value => value.CastingId == one).Enhancements
                    .Select(value => value.EnhancementId).SequenceEqual(new[] { "rod-extend-bard" }),
                "an incompatible rod was added or swapped in");
            view = session.BuildGraph(inputs);
            CastingGraphEnhancementOption rodAfter = view.Inspector.Enhancements.First(value => value.EnhancementId == "rod-extend-bard");
            Expect(rodAfter.Selected && rodAfter.PoolRemaining == 0,
                "the rod's single use is not shown spent after choosing it");
            Expect(view.Inspector.Enhancements.First(value => value.EnhancementId == "rod-empower-bard")
                .UnavailableReason.StartsWith("Cannot be combined with Extend Spell", StringComparison.Ordinal),
                "the incompatible rod does not say why it is unavailable");
            Expect(session.Undo(), "undo refused");
            view = session.BuildGraph(inputs);
            Expect(view.Inspector.Enhancements.First(value => value.EnhancementId == "rod-extend-bard").PoolRemaining == 1 &&
                session.Document.Castings.First(value => value.CastingId == one).Enhancements.Count == 0,
                "undo did not restore the casting and the rod budget");
        }

        // G08: a target that already has a casting is shown, not doubled; an
        // explicit duplicate is a distinct record, chip, line and cost.
        private static void TestGraphParallelCastings(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "g08", out inputs);
            string aKey = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            string one = GraphAdd(session, inputs, GraphSourceA, "unit-bard", aKey, "unit-t1");
            CastingGraphEditResult again = session.AddGraphCasting("unit-t1", inputs);
            Expect(!again.Applied && again.ShowedExisting && again.CastingId == one &&
                session.Document.Castings.Count == 1, "clicking an assigned target added a second casting");
            CastingGraphEditResult copy = session.DuplicateFocusedCasting();
            Expect(copy.Applied && copy.CastingId != one && session.Document.Castings.Count == 2,
                "duplicate did not add a distinct record");
            CastingGraphView view = session.BuildGraph(inputs);
            CastingGraphCasting first = view.CastingById(one);
            CastingGraphCasting second = view.CastingById(copy.CastingId);
            Expect(first.ParallelCount == 2 && second.ParallelCount == 2 &&
                first.ParallelIndex != second.ParallelIndex && second.Selected && !first.Selected,
                "parallel castings collapsed or share selection");
            // An identical copy with "skip if active" adds nothing in one pass:
            // the first already gives the effect, so it is shown as redundant
            // and spends nothing (as the executor would skip it).
            Expect(second.RedundantInOnePass && !first.RedundantInOnePass &&
                Row(view, "unit-bard", aKey).CapacityText == "3 of 4 casts remaining",
                "an identical parallel casting was charged or not called redundant");
            // Made distinct (Extend), it costs its own slot and rod use.
            Expect(session.ToggleFocusedEnhancement("rod-extend-bard", inputs).Applied, "rod refused");
            view = session.BuildGraph(inputs);
            Expect(!view.CastingById(copy.CastingId).RedundantInOnePass &&
                Row(view, "unit-bard", aKey).CapacityText == "2 of 4 casts remaining" &&
                view.CastingById(one).EnhancementBadges.Count == 0,
                "the distinct parallel casting did not cost its own slot");
            GraphLayoutResult layout = CastingGraphLayout.Compute(view, null);
            GraphChipPlacement a = layout.ChipFor(one);
            GraphChipPlacement b = layout.ChipFor(copy.CastingId);
            GraphLayoutMetrics m = new GraphLayoutMetrics();
            Expect(Math.Abs(a.Top - b.Top) >= m.ChipHeight, "parallel chips overlap");
            GraphConnection ca = layout.ConnectionFor(one);
            GraphConnection cb = layout.ConnectionFor(copy.CastingId);
            Expect(Math.Abs(ca.Inbound.From.Y - cb.Inbound.From.Y) > 0.5f &&
                Math.Abs(ca.Outbound.To.Y - cb.Outbound.To.Y) > 0.5f,
                "parallel connections share their exact anchors");
        }

        // G09: one group casting = one origin, one chip, one cost; derived
        // beneficiary branches; missed required coverage visible; clicking a
        // covered member never adds a second group cast.
        private static void TestGraphGroupCasting(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "g09", out inputs);
            session.SelectGraphBuff(GraphSourceC, inputs);
            session.SelectGraphCaster("unit-cleric", inputs);
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(view.SelectedSourceIsGroup == true && view.TargetById("unit-cleric").Legality ==
                CastingGraphTargetLegality.Legal && view.TargetById("unit-t2").Legality ==
                CastingGraphTargetLegality.Illegal, "group origins are not the legal anchors");
            CastingGraphEditResult added = session.AddGraphCasting("unit-cleric", inputs);
            Expect(added.Applied, "group casting refused: " + added.Edit.Reason);
            PlannedCasting casting = session.Document.Castings.Single();
            Expect(casting.TargetMode == CastingTargetMode.CasterCenteredOrigin && casting.Origin.IsCasterCentered &&
                casting.RequiredCoverageUnitIds.Count == 0, "the group casting is not caster-centred");
            view = session.BuildGraph(inputs);
            CastingGraphCasting chip = view.Castings.Single();
            Expect(chip.IsGroup && chip.TargetUnitId == "unit-cleric" &&
                chip.Beneficiaries.OrderBy(value => value).SequenceEqual(
                    new[] { "unit-bard", "unit-cleric", "unit-t1", "unit-t2" }),
                "the group chip does not carry its origin and predicted beneficiaries");
            GraphConnection connection = CastingGraphLayout.Compute(view, null).ConnectionFor(chip.CastingId);
            Expect(connection.Branches.Select(value => value.Key).OrderBy(value => value)
                    .SequenceEqual(new[] { "unit-bard", "unit-t1", "unit-t2" }),
                "derived beneficiary branches are wrong (the origin keeps its solid line)");
            CastingGraphEditResult covered = session.AddGraphCasting("unit-t1", inputs);
            Expect(!covered.Applied && covered.ShowedExisting && session.Document.Castings.Count == 1,
                "clicking a covered member added a second group cast");
            Expect(session.SetFocusedCoverage("unit-t3", true).Applied, "coverage edit refused");
            view = session.BuildGraph(inputs);
            Expect(view.Castings.Single().CoverageGaps.SequenceEqual(new[] { "unit-t3" }) &&
                view.Inspector.CoverageText.IndexOf("MISSED: Octavia", StringComparison.Ordinal) >= 0 &&
                session.Document.Castings.Count == 1,
                "missed required coverage is not visible, or a second casting was added");
            Expect(Row(view, "unit-cleric", GraphProviderKey("unit-cleric", "book-cleric-group", GraphAbilityC, "level-3"))
                .CapacityText == "1 of 2 casts remaining", "the group casting did not cost exactly one cast");
        }

        private static void TestGraphTargetLegality(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "legal", out inputs);
            session.SelectGraphBuff(GraphSourceB, inputs);
            session.SelectGraphCaster("unit-bard", inputs);
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(view.TargetById("unit-t3").Legality == CastingGraphTargetLegality.Illegal &&
                view.TargetById("unit-t3").IllegalReason == "Out of this caster's reach." &&
                view.TargetById("unit-t4").Legality == CastingGraphTargetLegality.Illegal &&
                view.TargetById("unit-t4").IllegalReason == "the recipient is unconscious",
                "illegal targets are not visible with reasons");
            string signature = session.DocumentIntentSignature();
            Expect(!session.AddGraphCasting("unit-t3", inputs).Applied &&
                !session.AddGraphCasting("unit-t4", inputs).Applied &&
                session.DocumentIntentSignature() == signature, "an illegal target produced a casting");
            Expect(WorkspaceRefusalText.Describe("target-unreachable:unit-t3").Length != 0,
                "a refusal has no player text");
        }

        // G12: the graph reconstructs from canonical records alone.
        private static void TestGraphSaveReload(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "g12", out inputs);
            string aKey = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            string one = GraphAdd(session, inputs, GraphSourceA, "unit-bard", aKey, "unit-t1");
            session.ToggleFocusedEnhancement("rod-extend-bard", inputs);
            session.DuplicateFocusedCasting();
            GraphAdd(session, inputs, GraphSourceA, "unit-cleric", null, "unit-t2");
            session.SelectGraphBuff(GraphSourceA, inputs);
            session.ClearGraphFocus();
            CastingGraphView before = session.BuildGraph(inputs);
            GraphLayoutResult beforeLayout = CastingGraphLayout.Compute(before, null);
            session.Save();
            string file = Directory.GetFiles(Path.Combine(root, "graph-g12"), "*.json", SearchOption.AllDirectories)
                .FirstOrDefault(path => File.ReadAllText(path).Contains(one));
            Expect(file != null, "the saved plan was not found");
            string saved = File.ReadAllText(file);
            Expect(saved.IndexOf("\"Top\"", StringComparison.OrdinalIgnoreCase) < 0 &&
                saved.IndexOf("parallel", StringComparison.OrdinalIgnoreCase) < 0 &&
                saved.IndexOf("remaining", StringComparison.OrdinalIgnoreCase) < 0,
                "graph geometry or balances were persisted");
            var reopened = new CastingWorkspaceSession(Path.Combine(root, "graph-g12"), "graph-campaign");
            reopened.SelectGraphBuff(GraphSourceA, inputs);
            CastingGraphView after = reopened.BuildGraph(inputs);
            Func<CastingGraphView, string> shape = view => string.Join(";", view.Castings.Select(value =>
                value.CastingId + ">" + value.CasterUnitId + ">" + value.SourceProviderKey + ">" + value.TargetUnitId +
                ">" + string.Join(",", value.EnhancementBadges.ToArray()) + ">" + value.ParallelIndex).ToArray());
            Expect(shape(before) == shape(after), "the reloaded graph differs: " + shape(after));
            GraphLayoutResult afterLayout = CastingGraphLayout.Compute(after, null);
            Expect(beforeLayout.Chips.Select(value => value.CastingId + "@" + value.Top)
                    .SequenceEqual(afterLayout.Chips.Select(value => value.CastingId + "@" + value.Top)),
                "the reconstructed layout moved");
        }

        // An imported casting with no caster keeps a visible connection from
        // an explicit "needs a caster" anchor; the view never picks one.
        private static void TestGraphUnresolvedCasting(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = GraphSession(root, "unresolved", out inputs);
            var imported = new PlannedCasting("cast-7", "long", 0, GraphSourceA, GraphAbilityA, null, null,
                CastingTargetMode.DirectTarget, "unit-t1", null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Draft,
                new MigrationProvenance("legacy-1", 5, "long", "imported", "unit-t1",
                    new[] { "automatic-caster-pending-review" }));
            Expect(session.AddCastingForRuntime(imported).Applied, "fixture casting refused");
            session.SelectGraphBuff(GraphSourceA, inputs);
            CastingGraphView view = session.BuildGraph(inputs);
            CastingGraphCasterNode anchor = view.Casters.FirstOrDefault(value => value.IsUnresolved);
            Expect(anchor != null && view.CastingById("cast-7").CasterUnitId == null &&
                view.CastingById("cast-7").NeedsReview, "the unresolved casting lost its anchor or review state");
            GraphConnection connection = CastingGraphLayout.Compute(view, null).ConnectionFor("cast-7");
            Expect(connection != null, "the unresolved casting has no connection");
            session.FocusGraphCasting("cast-7");
            view = session.BuildGraph(inputs);
            Expect(view.Inspector.CasterName == "No caster chosen" && view.Inspector.ReviewItems.Count == 1 &&
                view.Inspector.Providers.Count >= 3, "the inspector does not offer the explicit caster choice");
        }

        private static void TestGraphLayoutGeometry()
        {
            var metrics = new GraphLayoutMetrics();
            var segment = new GraphSegment(new GraphPoint(0f, 0f), new GraphPoint(100f, 0f));
            Expect(Math.Abs(segment.Length - 100f) < 0.01f && Math.Abs(segment.AngleDegrees) < 0.01f &&
                Math.Abs(segment.DistanceTo(new GraphPoint(50f, 12f)) - 12f) < 0.01f &&
                Math.Abs(segment.DistanceTo(new GraphPoint(-5f, 0f)) - 5f) < 0.01f,
                "segment geometry");
            var diagonal = new GraphSegment(new GraphPoint(0f, 0f), new GraphPoint(10f, 10f));
            Expect(Math.Abs(diagonal.AngleDegrees - 45f) < 0.01f, "segment angle");
            // Many castings converging on one target: chips never overlap and
            // stay in the chip lane.
            var view = new CastingGraphView
            {
                Casters = new[]
                {
                    new CastingGraphCasterNode("c1", "One", false, null, null, false),
                    new CastingGraphCasterNode("c2", "Two", false, null, null, false)
                },
                Targets = new[] { new CastingGraphTargetNode("t1", "T", CastingGraphTargetLegality.Unknown,
                    null, null, WorkspaceRecipientCoverage.None, null) },
                Castings = Enumerable.Range(0, 7).Select(index => new CastingGraphCasting("k" + index, "long",
                    index, index % 2 == 0 ? "c1" : "c2", null, CastingTargetMode.DirectTarget, "t1", null, null,
                    null, ResolvedCastingReadiness.Ready, "Ready", null, null, null, false,
                    CastingAuthoringState.Ready, false, false)).ToList()
            };
            GraphLayoutResult layout = CastingGraphLayout.Compute(view, metrics);
            List<GraphChipPlacement> ordered = layout.Chips.OrderBy(value => value.Top).ToList();
            for (int index = 1; index < ordered.Count; index++)
                Expect(ordered[index].Top - ordered[index - 1].Top >= metrics.ChipHeight + metrics.ChipGap - 0.01f,
                    "chips overlap");
            Expect(layout.Chips.All(chip => chip.Left.X >= metrics.ChipLaneLeft &&
                chip.Right.X <= metrics.TargetLaneLeft), "a chip left its lane");
            Expect(layout.Height >= ordered.Last().Top + metrics.ChipHeight, "the content height clips a chip");
            Expect(layout.Chips.Select(value => value.CastingId).SequenceEqual(
                Enumerable.Range(0, 7).Select(index => "k" + index)), "chips are not listed in routine order");
        }
    }
}
