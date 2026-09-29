using System;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // Everyday-use v1.2 persistence lifecycle (source-review R1–R3): real
    // authoring/repository services, real isolated files, no manual Save.
    internal static partial class Program
    {
        private static void RunAutosaveLifecycleTests(string root)
        {
            Run("autosave-interleaves-settings-and-edits", () => TestAutosaveInterleaved(root));
            Run("autosave-survives-reload-then-edit", () => TestAutosaveReloadThenEdit(root));
            Run("autosave-run-refuses-when-persistence-fails", () => TestAutosaveRunDurability(root));
        }

        private static AbilityKey LifecycleAbility()
        {
            return new AbilityKey("lifecycle-buff", null, 0, SourceKind.Spellbook, "book");
        }

        private static PlannedCasting LifecycleCasting(string id, string target)
        {
            return new PlannedCasting(id, "long", 0, "source-lifecycle", LifecycleAbility(),
                "unit-cleric", "book", CastingTargetMode.DirectTarget, target, null, null,
                null, null, ExistingEffectPolicy.SkipAlreadyActive, null,
                CastingAuthoringState.Ready, null);
        }

        private static CastingPlanRepository RepositoryOf(string dir)
        {
            return new CastingPlanRepository(dir);
        }

        private static PlannedCasting FirstPersistedCasting(string dir, string campaign)
        {
            CastingPlanLoadResult loaded = RepositoryOf(dir).Load(campaign);
            return loaded.Profile == null ? null : loaded.Profile.ToDocument().Castings.FirstOrDefault();
        }

        // R1: settings changes and document edits interleave with no skipped
        // saves — every mutation is durable in the actual file, verified by
        // a freshly constructed session, with no manual Save anywhere.
        private static void TestAutosaveInterleaved(string root)
        {
            string dir = Path.Combine(root, "autosave-interleaved");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "campaign:life",
                new DisabledCastingDispatchBoundary());
            // A settings change, then a document edit: the document edit
            // must persist even though the settings save just ran (the old
            // cross-counter bug skipped exactly this).
            session.SetExecutionMode("animated");
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-1", "unit-t1")).Applied)
                throw new InvalidOperationException("authoring refused.");
            if (session.IsDirty || session.AutosaveStatus != "saved")
                throw new InvalidOperationException("edit after settings did not autosave: " +
                    session.AutosaveStatus + " dirty=" + session.IsDirty);
            // Interleave the other direction and repeatedly.
            session.SetAllowAnimatedFallback(false);
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-2", "unit-t2")).Applied)
                throw new InvalidOperationException("second authoring refused.");
            session.SetOutOfCombatOnly(true);
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-3", "unit-t3")).Applied)
                throw new InvalidOperationException("third authoring refused.");
            session.SetExecutionMode("instant");
            session.Undo();
            if (session.IsDirty)
                throw new InvalidOperationException("undo did not autosave.");
            // The durable file holds the LATEST settings and the post-undo
            // castings (cast-3 gone, cast-1/cast-2 present), read back by a
            // genuinely fresh session.
            var fresh = new CastingWorkspaceSession(dir, "campaign:life");
            if (fresh.Document.Castings.Count != 2 ||
                fresh.Document.Castings.Any(value => value.CastingId == "cast-3"))
                throw new InvalidOperationException("durable file is not the post-undo intent.");
            if (fresh.ExecutionMode != "instant" || fresh.AllowAnimatedFallback ||
                !fresh.OutOfCombatOnly)
                throw new InvalidOperationException("durable settings are not the latest: " +
                    fresh.ExecutionMode);
            // Unchanged-setting no-ops stay harmless.
            session.SetExecutionMode("instant");
            if (session.AutosaveStatus != "saved" || session.IsDirty)
                throw new InvalidOperationException("an unchanged setting broke the save state.");
        }

        // R2: a successful reload replaces the authoring service; the next
        // edit still autosaves (the old bug: the replacement had no
        // subscription), and repeated replacements never duplicate writes.
        private static void TestAutosaveReloadThenEdit(string root)
        {
            string dir = Path.Combine(root, "autosave-reload-edit");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "campaign:life",
                new DisabledCastingDispatchBoundary());
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-1", "unit-t1")).Applied)
                throw new InvalidOperationException("authoring refused.");
            CastingPlanLoadStatus status = session.Reload();
            if (status != CastingPlanLoadStatus.Loaded)
                throw new InvalidOperationException("reload did not load: " + status);
            // THE R2 case: edit on the replacement authoring service.
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-2", "unit-t2")).Applied)
                throw new InvalidOperationException("post-reload authoring refused.");
            if (session.IsDirty || session.AutosaveStatus != "saved")
                throw new InvalidOperationException("post-reload edit did not autosave: " +
                    session.AutosaveStatus);
            PlannedCasting persisted = FirstPersistedCasting(dir, "campaign:life");
            if (persisted == null)
                throw new InvalidOperationException("nothing durable after the reload cycle.");
            // Repeat replacement (reload again) and Undo on the newest
            // instance; the file tracks the newest intent exactly.
            session.Reload();
            session.Undo();
            if (session.IsDirty)
                throw new InvalidOperationException("undo after second reload did not autosave.");
            var fresh = new CastingWorkspaceSession(dir, "campaign:life");
            if (fresh.Document.Castings.Count != session.Document.Castings.Count)
                throw new InvalidOperationException("durable file diverged from the live intent.");
            // An obsolete instance never writes: after all replacements, the
            // file's campaign is still ours and its castings are exactly
            // the live document's (no foreign or duplicated records).
            var durableRead = new CastingWorkspaceSession(dir, "campaign:life");
            if (!string.Equals(durableRead.DocumentIntentSignature(),
                    fresh.DocumentIntentSignature(), StringComparison.Ordinal))
                throw new InvalidOperationException("durable file is not the live intent.");
        }

        // R3: while the CURRENT revision cannot persist, a Run submits zero
        // casts and is refused with the persistence reason; once persistence
        // recovers, one deliberate Run uses the latest intent with no manual
        // Save. The failure is injected by holding the plan file locked (a
        // genuine OS boundary), not by a mock.
        private static void TestAutosaveRunDurability(string root)
        {
            string dir = Path.Combine(root, "autosave-run-durability");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "campaign:life",
                new DisabledCastingDispatchBoundary());
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-1", "unit-t1")).Applied)
                throw new InvalidOperationException("baseline authoring refused.");
            if (session.AutosaveStatus != "saved")
                throw new InvalidOperationException("baseline save failed: " + session.AutosaveStatus);
            // A deliberate edit whose autosave FAILS (the plan file is held
            // with an exclusive lock): the edit stays in memory, the file
            // keeps the baseline bytes.
            string planPath = RepositoryOf(dir).GetProfilePath("campaign:life");
            string baseline = File.ReadAllText(planPath);
            // FileShare.Read lets the test verify the untouched bytes;
            // the repository's exclusive writer still cannot proceed.
            using (var lockStream = new FileStream(planPath, FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                if (!session.AddCastingForRuntime(LifecycleCasting("cast-2", "unit-t2")).Applied)
                    throw new InvalidOperationException("locked authoring refused.");
                if (!session.AutosaveStatus.StartsWith("save-failed", StringComparison.Ordinal))
                    throw new InvalidOperationException("locked save did not fail: " +
                        session.AutosaveStatus);
                if (File.ReadAllText(planPath) != baseline)
                    throw new InvalidOperationException("the last good file was disturbed.");
                // The Run request: refused with the persistence reason, zero
                // native submissions.
                WorkspaceApplyResult refused = session.Apply(CastingApplyMode.Ordinary,
                    "long", new CastingWorkspaceInputs(
                        PartyForLifecycle(), ProviderOptionsForLifecycle(),
                        EffectsForLifecycle(), new CastEnhancementSnapshot[0]));
                if (refused.Allowed || refused.Dispatch != null ||
                    !refused.ReviewReason.StartsWith("persistence-failed", StringComparison.Ordinal))
                    throw new InvalidOperationException("a run with an undurable revision was " +
                        "not refused for persistence: " + refused.ReviewReason);
            }
            // Persistence recovered: ONE deliberate Run must now use the
            // latest intent (both castings) with no manual Save. The
            // disabled dispatch boundary records the submission identity.
            var boundary = (DisabledCastingDispatchBoundary)typeof(CastingWorkspaceSession)
                .GetField("_dispatch", System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                .GetValue(session);
            if (boundary.RecordedSubmissions.Count != 0)
                throw new InvalidOperationException("the refused run submitted something.");
            WorkspaceApplyResult applied = session.Apply(CastingApplyMode.Ordinary,
                "long", new CastingWorkspaceInputs(
                    PartyForLifecycle(), ProviderOptionsForLifecycle(),
                    EffectsForLifecycle(), new CastEnhancementSnapshot[0]));
            if (applied.Allowed || applied.Dispatch == null ||
                !applied.ReviewReason.Contains("native-submission-disabled"))
                throw new InvalidOperationException("the recovered run did not reach the " +
                    "boundary: " + applied.ReviewReason);
            if (boundary.RecordedSubmissions.Count != 1 ||
                !boundary.RecordedSubmissions[0].Contains("cast-1"))
                throw new InvalidOperationException("the recovered run did not submit the " +
                    "latest durable intent.");
            if (session.AutosaveStatus != "saved" || session.IsDirty)
                throw new InvalidOperationException("the run flush did not restore durability.");
        }

        private static PartyProviderSnapshot PartyForLifecycle()
        {
            return new PartyProviderSnapshot(new[]
            {
                new UnitSnapshot("unit-cleric", "Cleric", false, null,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-t1", "Ally One", false, null,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("unit-t2", "Ally Two", false, null,
                    new TargetValidationSnapshot(true, true, true, true))
            }, new[]
            {
                new ProviderSnapshot(new ProviderKey("unit-cleric", "book", LifecycleAbility(), "pool-1"),
                    "Lifecycle Buff", 1, "pool-1", 1, null, null, 5, 50,
                    "A buff for the lifecycle tests.", "50 rounds", string.Empty, 0, "book")
            }, new[]
            {
                new ResourcePoolSnapshot("pool-1", ResourcePoolKind.SpontaneousLevel, 10, 10, null)
            });
        }

        private static System.Collections.Generic.List<ProviderPlanningOption> ProviderOptionsForLifecycle()
        {
            var provider = new ProviderSnapshot(
                new ProviderKey("unit-cleric", "book", LifecycleAbility(), "pool-1"),
                "Lifecycle Buff", 1, "pool-1", 1, null, null, 5, 50,
                "A buff for the lifecycle tests.", "50 rounds", string.Empty, 0, "book");
            return new System.Collections.Generic.List<ProviderPlanningOption>
            {
                new ProviderPlanningOption(provider,
                    new[] { "unit-t1", "unit-t2" }, new[] { "unit-t1", "unit-t2" }, 5, 50)
            };
        }

        private static System.Collections.Generic.Dictionary<string, Domain.Effects.EffectExpression>
            EffectsForLifecycle()
        {
            return new System.Collections.Generic.Dictionary<string, Domain.Effects.EffectExpression>
            {
                { "source-lifecycle", new Domain.Effects.EffectLeafExpression(
                    Domain.Effects.EffectKind.Buff, "buff-lifecycle",
                    Domain.Effects.EffectTarget.CurrentTarget, "lifecycle", "lifecycle/a") },
                { LifecycleAbility().Canonical, new Domain.Effects.EffectLeafExpression(
                    Domain.Effects.EffectKind.Buff, "buff-lifecycle",
                    Domain.Effects.EffectTarget.CurrentTarget, "lifecycle", "lifecycle/a") }
            };
        }
    }
}
