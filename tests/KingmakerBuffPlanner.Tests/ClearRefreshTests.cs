using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // Owner playtest after 0.6.11: "Leinna's text still said 'none left
    // after plan' even after I cleared it." The production session, compiler,
    // ledger and graph read models are real; only native resource snapshots
    // are fixtures. Clear releases planned reservations and never refunds.
    internal static partial class Program
    {
        private static void RunClearRefreshTests(string root)
        {
            Run("clear-refresh-reservation-only-exhaustion-restores-projection",
                () => TestClearRefreshReservationOnly(root));
            Run("clear-refresh-genuinely-spent-sources-say-so-without-plan-blame",
                () => TestClearRefreshGenuinelySpent(root));
        }

        private static CastingWorkspaceInputs ClearRefreshInputs(int bardLevel2, int wandCharges,
            bool clericSlotsSpent = false)
        {
            CastingWorkspaceInputs baseline = GraphInputs(bardLevel2: bardLevel2);
            ProviderSnapshot wandBard = GraphProvider("unit-bard", "wand-a", GraphAbilityA, "item", 1,
                "party-wand-a", 1, null, "Wand");
            ProviderSnapshot wandCleric = GraphProvider("unit-cleric", "wand-a", GraphAbilityA, "item", 1,
                "party-wand-a", 1, null, "Wand");
            IEnumerable<ResourcePoolSnapshot> pools = baseline.Snapshot.ResourcePools.Select(pool =>
            {
                if (pool.PoolKey == "bard-l2")
                    return new ResourcePoolSnapshot(pool.PoolKey, pool.Kind, 4, bardLevel2, null);
                if (pool.PoolKey != "cleric-prepared" || !clericSlotsSpent) return pool;
                // The two exact A slots were genuinely cast today.
                var tokens = pool.Tokens.Select(token => token.SlottedAbility.Equals(GraphAbilityA)
                    ? new ResourceTokenSnapshot(token.TokenId, token.SlottedAbility, token.SpellLevel,
                        token.SlotKind, false, token.IsPrimary, token.LinkedTokenIds) : token).ToList();
                return new ResourcePoolSnapshot(pool.PoolKey, pool.Kind, pool.Capacity,
                    tokens.Count(token => token.Available), tokens);
            }).Concat(new[] { new ResourcePoolSnapshot("party-wand-a", ResourcePoolKind.ItemCharges,
                Math.Max(2, wandCharges), wandCharges, null) });
            var snapshot = new PartyProviderSnapshot(baseline.Snapshot.Units,
                baseline.Snapshot.Providers.Concat(new[] { wandBard, wandCleric }), pools);
            var options = baseline.ProviderOptions.Concat(new[]
            {
                new ProviderPlanningOption(wandBard, GraphUnits, new[] { "unit-bard" }, 5, 100),
                new ProviderPlanningOption(wandCleric, GraphUnits, new[] { "unit-cleric" }, 5, 100)
            });
            return new CastingWorkspaceInputs(snapshot, options, baseline.EffectsBySource, baseline.Enhancements);
        }

        private static CastingWorkspaceSession ClearRefreshSession(string root, string name)
        {
            string dir = Path.Combine(root, "clear-refresh-" + name);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            return new CastingWorkspaceSession(dir, "clear-refresh-campaign");
        }

        private static string SourceLineOf(CastingGraphSourceRow row)
        {
            return CastingGraphText.SourceLine(row.Usable, row.BlockedReason, row.CapacityText,
                row.PoolLabel, row.SharedPoolWith);
        }

        private static readonly string ClearBardBook = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
        private static readonly string ClearBardB = GraphProviderKey("unit-bard", "book-bard", GraphAbilityB, "level-2");
        private static readonly string ClearClericBook = GraphProviderKey("unit-cleric", "book-cleric", GraphAbilityA, "level-2");
        private static readonly string ClearBardWand = GraphProviderKey("unit-bard", "wand-a", GraphAbilityA, "item");
        private static readonly string ClearClericWand = GraphProviderKey("unit-cleric", "wand-a", GraphAbilityA, "item");

        // 1, 3, 4: resources exhausted ONLY by reservations of castings spread
        // across all three routines (shared spontaneous pool, exact prepared
        // slots, independent party item charges). Clear restores the
        // projection to the actual unspent counts on the very next build,
        // with a populated inspector/draft; Undo restores the exact plan and
        // recomputes against the same (unrefunded) native resources.
        private static void TestClearRefreshReservationOnly(string root)
        {
            CastingWorkspaceInputs inputs = ClearRefreshInputs(2, 2);
            CastingWorkspaceSession session = ClearRefreshSession(root, "reserved");
            session.SelectRoutine("long");
            GraphAdd(session, inputs, GraphSourceA, "unit-bard", ClearBardBook, "unit-t1");
            GraphAdd(session, inputs, GraphSourceA, "unit-cleric", ClearClericBook, "unit-t2");
            GraphAdd(session, inputs, GraphSourceA, "unit-bard", ClearBardWand, "unit-t3");
            session.SelectRoutine("important");
            // Distinct recipients: one-pass projection would otherwise skip
            // a repeat of an earlier routine's identical casting.
            GraphAdd(session, inputs, GraphSourceA, "unit-bard", ClearBardBook, "unit-bard");
            GraphAdd(session, inputs, GraphSourceA, "unit-cleric", ClearClericBook, "unit-cleric");
            GraphAdd(session, inputs, GraphSourceA, "unit-cleric", ClearClericWand, "unit-alch");
            session.SelectRoutine("short");
            string mutagen = GraphAdd(session, inputs, GraphSourceM, "unit-alch", null, "unit-alch");
            var before = session.Document;
            Expect(before.Castings.Count == 7 && before.Castings.Select(c => c.RoutineId).Distinct().Count() == 3,
                "fixture plan does not span the three routines");

            // Exhausted by the plan: honestly attributed to the plan.
            session.SelectRoutine("long");
            session.SelectGraphBuff(GraphSourceA, inputs);
            session.SelectGraphCaster("unit-cleric", inputs);
            Expect(session.SelectGraphSource(ClearClericBook, inputs).Applied, "draft source refused");
            CastingGraphView view = session.BuildGraph(inputs);
            foreach (string key in new[] { ClearBardBook, ClearBardWand })
                Expect(!Row(view, "unit-bard", key).Usable &&
                    Row(view, "unit-bard", key).BlockedReason == CastingGraphText.ReservedByPlanReason &&
                    SourceLineOf(Row(view, "unit-bard", key)).StartsWith("None left after the plan", StringComparison.Ordinal),
                    "reserved bard source was not attributed to the plan: " + SourceLineOf(Row(view, "unit-bard", key)));
            Expect(!Row(view, "unit-cleric", ClearClericBook).Usable &&
                Row(view, "unit-cleric", ClearClericBook).BlockedReason == CastingGraphText.ReservedByPlanReason,
                "reserved prepared slots were not attributed to the plan");
            // Populate the inspector too, then clear while it is open.
            string focused = view.Castings.First().CastingId;
            session.FocusGraphCasting(focused);
            Expect(session.BuildGraph(inputs).Inspector != null, "fixture inspector did not open");

            ClearPlanConfirmation confirmation = session.BeginClearPlan(inputs);
            Expect(confirmation != null && confirmation.CastingCount == 7, "confirmation miscounted the plan");
            Expect(session.ClearPlan(confirmation, inputs).Applied, "clear refused");
            // The FIRST build after Clear, with no other action.
            view = session.BuildGraph(inputs);
            Expect(view.Castings.Count == 0 && view.Routines.All(r => r.CastingCount == 0) &&
                view.RoutineCastingCount == 0 && view.RoutineReadyCount == 0 && view.OtherRoutineCastings == 0,
                "planned counts survived Clear");
            Expect(view.FocusedCastingId == null && view.Inspector == null && session.EditingFocusCastingId == null,
                "the cleared casting is still inspected");
            Expect(view.SelectedSourceId == GraphSourceA, "a still-valid buff selection was discarded");
            Expect(Row(view, "unit-bard", ClearBardBook).Usable &&
                Row(view, "unit-bard", ClearBardBook).CapacityText == "2 of 2 casts remaining" &&
                Row(view, "unit-cleric", ClearClericBook).Usable &&
                Row(view, "unit-cleric", ClearClericBook).CapacityText == "2 exact slots ready" &&
                Row(view, "unit-bard", ClearBardWand).CapacityText == "2 of 2 casts remaining" &&
                Row(view, "unit-cleric", ClearClericWand).CapacityText == "2 of 2 casts remaining",
                "projected availability did not return to the unspent counts: " + string.Join(" | ",
                    view.Casters.SelectMany(c => c.Sources).Select(SourceLineOf).ToArray()));
            Expect(view.Casters.SelectMany(c => c.Sources).All(row => !SourceLineOf(row).Contains("after the plan")),
                "an empty plan is still blamed for a shortage");
            session.SelectGraphBuff(GraphSourceB, inputs);
            Expect(Row(session.BuildGraph(inputs), "unit-bard", ClearBardB).CapacityText == "2 of 2 casts remaining",
                "the shared pool still carries the cleared reservations for another buff");
            WorkspaceResourceReport report = session.BuildResourceReport(inputs, WorkspaceResourceScope.AllRoutinesNoRest);
            List<WorkspaceResourceRow> rows = report.Groups.SelectMany(g => g.Rows).ToList();
            Expect(report.SavedCastingCount == 0 && rows.All(r => r.Needed == 0 && r.WouldSpend == 0 && r.ShortBy == 0),
                "Resources still reserves for the cleared plan");
            Expect(inputs.Snapshot.ResourcePools.Single(p => p.PoolKey == "bard-l2").Remaining == 2 &&
                inputs.Snapshot.ResourcePools.Single(p => p.PoolKey == "party-wand-a").Remaining == 2,
                "the native fixture was mutated");

            // Undo: the exact authored plan, recomputed against current resources.
            Expect(session.Undo() && ReferenceEquals(before, session.Document), "Undo did not restore the exact plan");
            session.SelectGraphBuff(GraphSourceA, inputs);
            view = session.BuildGraph(inputs);
            Expect(!Row(view, "unit-bard", ClearBardBook).Usable &&
                Row(view, "unit-bard", ClearBardBook).BlockedReason == CastingGraphText.ReservedByPlanReason &&
                !Row(view, "unit-cleric", ClearClericBook).Usable && !Row(view, "unit-bard", ClearBardWand).Usable,
                "Undo restored stale availability instead of re-reserving");
            Expect(view.Routines.Single(r => r.RoutineId == "long").CastingCount == 3 &&
                view.Routines.Single(r => r.RoutineId == "important").CastingCount == 3 &&
                view.Routines.Single(r => r.RoutineId == "short").CastingCount == 1, "routine counts after Undo");
            session.SelectRoutine("short");
            session.SelectGraphBuff(GraphSourceM, inputs);
            view = session.BuildGraph(inputs);
            Expect(view.Castings.Count == 1 && view.Castings[0].CastingId == mutagen, "routine switch after Undo");
            report = session.BuildResourceReport(inputs, WorkspaceResourceScope.AllRoutinesNoRest);
            Expect(report.SavedCastingCount == 7 && report.Groups.SelectMany(g => g.Rows)
                .Any(r => r.PoolKey == "bard-l2" && r.Needed == 2 && r.AvailableNow == 2),
                "reopened Resources after Undo did not recompute the reservations");
            // A fresh session over the persisted file (reopening the workspace).
            var reopened = new CastingWorkspaceSession(Path.Combine(root, "clear-refresh-reserved"), "clear-refresh-campaign");
            Expect(reopened.Document.Castings.Count == 7, "Undo did not persist");
        }

        // 2: genuinely spent native resources. Zero stays zero before and
        // after Clear (nothing is refunded), and the wording never implies
        // the (now empty) plan is consuming them.
        private static void TestClearRefreshGenuinelySpent(string root)
        {
            CastingWorkspaceInputs inputs = ClearRefreshInputs(0, 0, true);
            CastingWorkspaceSession session = ClearRefreshSession(root, "spent");
            session.SelectRoutine("long");
            GraphAdd(session, inputs, GraphSourceM, "unit-alch", null, "unit-alch");
            for (int pass = 0; pass < 2; pass++)
            {
                session.SelectGraphBuff(GraphSourceA, inputs);
                session.SelectGraphCaster("unit-cleric", inputs);
                session.SelectGraphSource(ClearClericBook, inputs);
                CastingGraphView view = session.BuildGraph(inputs);
                CastingGraphSourceRow bard = Row(view, "unit-bard", ClearBardBook);
                CastingGraphSourceRow cleric = Row(view, "unit-cleric", ClearClericBook);
                CastingGraphSourceRow wand = Row(view, "unit-bard", ClearBardWand);
                Expect(!bard.Usable && bard.BlockedReason == CastingGraphText.NoUsesReason &&
                    !cleric.Usable && cleric.BlockedReason == CastingGraphText.NoPreparedUsesReason &&
                    !wand.Usable && wand.BlockedReason == CastingGraphText.NoUsesReason,
                    "pass " + pass + ": genuinely spent sources: " + bard.BlockedReason + " | " +
                    cleric.BlockedReason + " | " + wand.BlockedReason);
                Expect(SourceLineOf(cleric).StartsWith("No prepared uses remaining", StringComparison.Ordinal) &&
                    view.Casters.SelectMany(c => c.Sources).All(row => !SourceLineOf(row).Contains("after the plan")),
                    "pass " + pass + ": spent resources blamed on the plan: " + string.Join(" | ",
                        view.Casters.SelectMany(c => c.Sources).Select(SourceLineOf).ToArray()));
                Expect(bard.Capacity.NativeAvailableNow == 0 && bard.Capacity.AdditionalCastings == 0,
                    "pass " + pass + ": a spent pool was refunded");
                if (pass == 0) Expect(session.ClearPlan(session.BeginClearPlan(inputs), inputs).Applied, "clear refused");
            }
            Expect(session.Document.Castings.Count == 0, "plan not empty after Clear");
        }
    }
}
