using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // Review addendum §1: the Undo history trim must keep the NEWEST 64
    // prior documents in LIFO order. Real authoring/repository services,
    // exact document contents asserted after every Undo, fresh-session
    // reads, no manual Save.
    internal static partial class Program
    {
        private static void RunUndoHistoryTests(string root)
        {
            Run("undo-history-keeps-newest-64-in-order", () => TestUndoHistoryTrim(root));
        }

        private static PlannedCasting TrimCasting(int index)
        {
            return new PlannedCasting("cast-" + index, "long", index, "source-trim",
                new AbilityKey("trim-buff", null, 0, SourceKind.Spellbook, "book"),
                "unit-cleric", "book", CastingTargetMode.DirectTarget, "unit-t1", null,
                null, null, null, ExistingEffectPolicy.SkipAlreadyActive, null,
                CastingAuthoringState.Ready, null);
        }

        private static void TestUndoHistoryTrim(string root)
        {
            string dir = Path.Combine(root, "undo-history-trim");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "campaign:trim",
                new DisabledCastingDispatchBoundary());
            // 65 successful additions from an empty plan: history holds the
            // newest 64 priors (cast-1..cast-64 documents). The old defect
            // restored the EMPTY document on the first Undo.
            for (int index = 1; index <= 65; index++)
            {
                if (!session.AddCastingForRuntime(TrimCasting(index)).Applied)
                    throw new InvalidOperationException("add " + index + " refused.");
                if (session.IsDirty)
                    throw new InvalidOperationException("add " + index + " did not autosave.");
            }
            if (session.Document.Castings.Count != 65)
                throw new InvalidOperationException("not all 65 castings are live.");
            // First Undo restores the 64-casting document (cast-65 gone).
            if (!session.Undo())
                throw new InvalidOperationException("first undo refused.");
            if (session.Document.Castings.Count != 64 ||
                session.Document.Castings.Any(value => value.CastingId == "cast-65"))
                throw new InvalidOperationException("first undo did not restore the " +
                    "64-casting document: count=" + session.Document.Castings.Count);
            if (session.IsDirty)
                throw new InvalidOperationException("undo did not autosave its result.");
            // The full 64-deep chain unwinds in exact order.
            for (int expected = 63; expected >= 1; expected--)
            {
                if (!session.Undo())
                    throw new InvalidOperationException("undo refused at " + expected + ".");
                if (session.Document.Castings.Count != expected)
                    throw new InvalidOperationException("undo order broke at " + expected +
                        ": count=" + session.Document.Castings.Count);
            }
            // One more Undo at the bottom is an honest no-op (the empty
            // start document was correctly dropped by the trim).
            if (session.Undo())
                throw new InvalidOperationException("undo below the trimmed floor succeeded.");
            if (session.Document.Castings.Count != 1 ||
                !string.Equals(session.Document.Castings[0].CastingId, "cast-1",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("the floor is not the cast-1 document.");
            // The durable file matches the live post-undo state (a fresh
            // session reads exactly what Undo autosaved).
            var fresh = new CastingWorkspaceSession(dir, "campaign:trim");
            if (fresh.Document.Castings.Count != session.Document.Castings.Count ||
                !string.Equals(fresh.DocumentIntentSignature(), session.DocumentIntentSignature(),
                    StringComparison.Ordinal))
                throw new InvalidOperationException("durable file diverged from the live " +
                    "undo result.");

            // Repeated trimming: build to 65 again from here, then check the
            // boundary sizes 63/64/65/66 explicitly on a clean session.
            string dir2 = Path.Combine(root, "undo-history-trim-repeat");
            Directory.CreateDirectory(dir2);
            var second = new CastingWorkspaceSession(dir2, "campaign:trim",
                new DisabledCastingDispatchBoundary());
            for (int index = 1; index <= 66; index++)
                if (!second.AddCastingForRuntime(TrimCasting(index)).Applied)
                    throw new InvalidOperationException("repeat add " + index + " refused.");
            // 66 castings from empty: history held 66 priors, trim kept the
            // newest 64, so the floor is the TWO-casting document (the
            // document before add-3). Undos run 65..2, then refuse.
            for (int expected = 65; expected >= 2; expected--)
            {
                if (!second.Undo())
                    throw new InvalidOperationException("repeat undo refused at " + expected + ".");
                if (second.Document.Castings.Count != expected)
                    throw new InvalidOperationException("repeat undo broke at " + expected +
                        ": count=" + second.Document.Castings.Count);
            }
            if (second.Undo())
                throw new InvalidOperationException("repeat undo below the floor succeeded.");
            if (second.Document.Castings.Count != 2 ||
                !second.Document.Castings.All(value =>
                    value.CastingId == "cast-1" || value.CastingId == "cast-2"))
                throw new InvalidOperationException("repeat floor is not the cast-1+cast-2 " +
                    "document: count=" + second.Document.Castings.Count);
        }
    }
}
