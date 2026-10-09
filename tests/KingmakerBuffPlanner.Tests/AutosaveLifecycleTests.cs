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
            Run("autosave-recovers-at-lifecycle-boundaries", () => TestAutosaveLifecycleBoundaries(root));
            Run("acknowledge-import-notices-persists-like-any-edit", () => TestAcknowledgePersistence(root));
            Run("failed-flush-intent-has-a-reachable-recovery-owner",
                () => TestRecoveryOwnerTransition(root));
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
            // §5: durable disk/session state verified after EVERY step — a
            // later save can never conceal an earlier skipped one.
            Action<string> verify = label =>
            {
                CastingWorkspaceSession check = new CastingWorkspaceSession(dir, "campaign:life");
                if (check.IsDirty || check.AutosaveStatus != "saved")
                    throw new InvalidOperationException(label + ": fresh session not durable.");
                if (!string.Equals(check.DocumentIntentSignature(),
                        session.DocumentIntentSignature(), StringComparison.Ordinal))
                    throw new InvalidOperationException(label + ": durable file is not the " +
                        "live intent.");
                if (!string.Equals(check.ExecutionMode, session.ExecutionMode,
                        StringComparison.Ordinal) ||
                    check.ExecutionSettings.AllowAnimatedFallback !=
                        session.ExecutionSettings.AllowAnimatedFallback ||
                    check.ExecutionSettings.OutOfCombatOnly != session.ExecutionSettings.OutOfCombatOnly)
                    throw new InvalidOperationException(label + ": durable settings are not " +
                        "the live settings.");
            };
            // A settings change, then a document edit: the document edit
            // must persist even though the settings save just ran (the old
            // cross-counter bug skipped exactly this).
            session.SetExecutionMode("animated");
            verify("after settings-1");
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-1", "unit-t1")).Applied)
                throw new InvalidOperationException("authoring refused.");
            if (session.IsDirty || session.AutosaveStatus != "saved")
                throw new InvalidOperationException("edit after settings did not autosave: " +
                    session.AutosaveStatus + " dirty=" + session.IsDirty);
            verify("after add-1");
            // Interleave the other direction and repeatedly (WP4: the mode
            // is the remaining execution setting).
            session.SetExecutionMode("instant");
            verify("after settings-2");
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-2", "unit-t2")).Applied)
                throw new InvalidOperationException("second authoring refused.");
            verify("after add-2");
            session.SetExecutionMode("animated");
            verify("after settings-3");
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-3", "unit-t3")).Applied)
                throw new InvalidOperationException("third authoring refused.");
            verify("after add-3");
            session.SetExecutionMode("instant");
            verify("after settings-4");
            session.Undo();
            if (session.IsDirty)
                throw new InvalidOperationException("undo did not autosave.");
            verify("after undo");
            // The durable file holds the LATEST settings and the post-undo
            // castings (cast-3 gone, cast-1/cast-2 present), read back by a
            // genuinely fresh session.
            var fresh = new CastingWorkspaceSession(dir, "campaign:life");
            if (fresh.Document.Castings.Count != 2 ||
                fresh.Document.Castings.Any(value => value.CastingId == "cast-3"))
                throw new InvalidOperationException("durable file is not the post-undo intent.");
            if (fresh.ExecutionMode != "instant" || fresh.ExecutionSettings.AllowAnimatedFallback ||
                !fresh.ExecutionSettings.OutOfCombatOnly)
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
            // §5/R2: repeat replacement, then EDIT on the newest instance,
            // then a genuine Undo of that edit (the replacement's empty
            // history means Undo alone proves nothing) — and compare the
            // retained LIVE document with the durable result.
            session.Reload();
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-3", "unit-t3")).Applied)
                throw new InvalidOperationException("post-second-reload authoring refused.");
            string beforeUndo = session.DocumentIntentSignature();
            if (!session.Undo())
                throw new InvalidOperationException("undo after the second reload had no " +
                    "history to restore.");
            if (string.Equals(session.DocumentIntentSignature(), beforeUndo,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("undo did not change the live document.");
            if (session.IsDirty)
                throw new InvalidOperationException("undo after second reload did not autosave.");
            var durableRead = new CastingWorkspaceSession(dir, "campaign:life");
            if (!string.Equals(durableRead.DocumentIntentSignature(),
                    session.DocumentIntentSignature(), StringComparison.Ordinal))
                throw new InvalidOperationException("durable file is not the LIVE post-undo " +
                    "intent.");
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
            var boundary = new DisabledCastingDispatchBoundary();
            var session = new CastingWorkspaceSession(dir, "campaign:life", boundary);
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
            // latest intent (BOTH castings, exact identities and targets —
            // an omitted cast-2 must fail this) with no manual Save. The
            // injected boundary is held directly (§5, no reflection).
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
                !boundary.RecordedSubmissions[0].Contains("cast-1") ||
                !boundary.RecordedSubmissions[0].Contains("cast-2"))
                throw new InvalidOperationException("the recovered run did not submit the " +
                    "complete latest durable intent: " +
                    boundary.RecordedSubmissions[0]);
            // §5/E1: the SUBMITTED PROJECTION the dispatch boundary
            // received — read from the boundary itself, never re-derived
            // from the document — proves the exact ordered steps, targets,
            // provider/caster/source identities and reserved costs. A
            // target changed only during conversion is detected here.
            ExplicitStepConversion submitted = boundary.LastProjection;
            if (submitted == null || !submitted.Converted)
                throw new InvalidOperationException("the boundary saw no converted " +
                    "projection for the recovered run.");
            if (applied.Projection != null &&
                !ReferenceEquals(applied.Projection, submitted))
                throw new InvalidOperationException("the returned projection is not the " +
                    "projection the boundary received.");
            if (submitted.CastingIds.Count != 2 ||
                !string.Equals(submitted.CastingIds[0], "cast-1", StringComparison.Ordinal) ||
                !string.Equals(submitted.CastingIds[1], "cast-2", StringComparison.Ordinal))
                throw new InvalidOperationException("the submitted projection's ordered " +
                    "casting ids are not exactly [cast-1, cast-2].");
            if (submitted.Plan.Steps.Count != 2)
                throw new InvalidOperationException("the submitted projection does not " +
                    "carry exactly two executor steps.");
            for (int index = 0; index < 2; index++)
            {
                CastStep step = submitted.Plan.Steps[index];
                string expectedCasting = index == 0 ? "cast-1" : "cast-2";
                string expectedTarget = index == 0 ? "unit-t1" : "unit-t2";
                if (!string.Equals(step.AssignmentId, expectedCasting, StringComparison.Ordinal))
                    throw new InvalidOperationException("step " + index + " is not " +
                        expectedCasting + ".");
                if (step.TargetUnitIds.Count != 1 ||
                    !string.Equals(step.TargetUnitIds[0], expectedTarget,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException("step " + index + " target is not " +
                        expectedTarget + ".");
                if (!string.Equals(step.SourceId, "source-lifecycle", StringComparison.Ordinal))
                    throw new InvalidOperationException("step " + index + " source drifted.");
                if (step.Provider == null ||
                    !string.Equals(step.Provider.CasterUnitId, "unit-cleric",
                        StringComparison.Ordinal) ||
                    !string.Equals(step.Provider.SpellbookGuid, "book",
                        StringComparison.Ordinal) ||
                    !string.Equals(step.Provider.Ability.Canonical,
                        LifecycleAbility().Canonical, StringComparison.Ordinal))
                    throw new InvalidOperationException("step " + index + " provider identity " +
                        "drifted.");
                if (step.Reservation == null || step.Reservation.Units != 1 ||
                    !string.Equals(step.Reservation.PoolKey, "pool-1",
                        StringComparison.Ordinal))
                    throw new InvalidOperationException("step " + index + " did not reserve " +
                        "its exact native cost.");
            }
            if (submitted.ProjectionId.Length == 0 || submitted.CanonicalContract.Length == 0)
                throw new InvalidOperationException("the submitted projection has no " +
                    "identity hash.");
            // The run flush restored the LIVE session's durability too.
            if (session.IsDirty || session.AutosaveStatus != "saved")
                throw new InvalidOperationException("the run flush did not restore the live " +
                    "session's durability: " + session.AutosaveStatus);
            // E1: durability from an actual FRESH READ, not IsDirty.
            var durableRead = new CastingWorkspaceSession(dir, "campaign:life");
            if (durableRead.Document.Castings.Count != 2 || durableRead.IsDirty)
                throw new InvalidOperationException("the durable file is not exactly the " +
                    "submitted intent.");
            // E1: the submitted intent is IMMUTABLE after a later editor
            // mutation — the projection object, its steps and their targets
            // do not change when the document gains a third casting.
            string[] submittedTargets = submitted.Plan.Steps
                .SelectMany(step => step.TargetUnitIds).ToArray();
            if (!session.AddCastingForRuntime(LifecycleCasting("cast-3", "unit-t3")).Applied)
                throw new InvalidOperationException("the later edit was refused.");
            if (!ReferenceEquals(boundary.LastProjection, submitted) ||
                submitted.Plan.Steps.Count != 2)
                throw new InvalidOperationException("a later editor mutation changed the " +
                    "submitted projection.");
            if (!submitted.Plan.Steps.SelectMany(step => step.TargetUnitIds)
                    .SequenceEqual(submittedTargets))
                throw new InvalidOperationException("a later editor mutation changed the " +
                    "submitted steps' targets.");
            // The later mutation is itself durable (fresh read), and never
            // replaced the submitted projection's identity on the boundary.
            var afterMutation = new CastingWorkspaceSession(dir, "campaign:life");
            if (afterMutation.Document.Castings.Count != 3)
                throw new InvalidOperationException("the later edit was not durable.");
        }


        // Review addendum §2: controlled lifecycle boundaries attempt
        // durability before discarding; a campaign switch never relabels
        // campaign A's intent as campaign B's; RetryFailedSave is the
        // recovery surface. Real repository files throughout.
        private static void TestAutosaveLifecycleBoundaries(string root)
        {
            string dirA = Path.Combine(root, "lifecycle-campaign-A");
            string dirB = Path.Combine(root, "lifecycle-campaign-B");
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(dirB);
            var sessionA = new CastingWorkspaceSession(dirA, "campaign:A",
                new DisabledCastingDispatchBoundary());
            if (!sessionA.AddCastingForRuntime(LifecycleCasting("cast-a", "unit-t1")).Applied)
                throw new InvalidOperationException("A authoring refused.");
            // Failed-save → the retry surface recovers once storage heals.
            string planA = RepositoryOf(dirA).GetProfilePath("campaign:A");
            string baseline = File.ReadAllText(planA);
            using (var hold = new FileStream(planA, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (!sessionA.AddCastingForRuntime(LifecycleCasting("cast-a2", "unit-t2")).Applied)
                    throw new InvalidOperationException("locked A authoring refused.");
                if (sessionA.RetryFailedSave())
                    throw new InvalidOperationException("retry succeeded while the store " +
                        "was locked.");
                if (File.ReadAllText(planA) != baseline)
                    throw new InvalidOperationException("the last good A file was disturbed.");
            }
            if (!sessionA.RetryFailedSave() || sessionA.IsDirty)
                throw new InvalidOperationException("retry failed after storage healed: " +
                    sessionA.AutosaveStatus);
            // Campaign switch with a FAILED save on A: A's edits stay with
            // A (RetryFailedSave refused), A's file is never written into
            // B, and B starts empty of A's castings.
            using (var hold = new FileStream(planA, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (!sessionA.AddCastingForRuntime(LifecycleCasting("cast-a3", "unit-t3")).Applied)
                    throw new InvalidOperationException("second locked A authoring refused.");
                var sessionB = new CastingWorkspaceSession(dirB, "campaign:B",
                    new DisabledCastingDispatchBoundary());
                var binding = CastingWorkspaceSessionBinding.Resolve(sessionA, "campaign:B",
                    id => new CastingWorkspaceSession(id == "campaign:B" ? dirB : dirA, id),
                    message => { });
                if (ReferenceEquals(binding, sessionA))
                    throw new InvalidOperationException("the binding reused A's session for B.");
                if (binding.CampaignId != "campaign:B" || binding.Document.Castings.Count != 0)
                    throw new InvalidOperationException("B started with foreign intent.");
            }
            // A recovers its own intent under its own campaign afterwards.
            if (!sessionA.RetryFailedSave() || sessionA.IsDirty)
                throw new InvalidOperationException("A's recovery failed.");
            var freshA = new CastingWorkspaceSession(dirA, "campaign:A");
            if (freshA.Document.Castings.Count != 3)
                throw new InvalidOperationException("A's durable file is not its latest " +
                    "intent: " + freshA.Document.Castings.Count);
            var freshB = new CastingWorkspaceSession(dirB, "campaign:B");
            if (freshB.Document.Castings.Count != 0)
                throw new InvalidOperationException("B was contaminated by A.");
        }

        // Review addendum §2: acknowledging import notices is a deliberate
        // document mutation — it must announce, autosave, persist across a
        // fresh load, and stay undoable.
        private static void TestAcknowledgePersistence(string root)
        {
            string dir = Path.Combine(root, "acknowledge-persist");
            Directory.CreateDirectory(dir);
            // A plan with a pending import notice (an unresolved provenance
            // marker) persisted directly through the repository.
            var ability = LifecycleAbility();
            var casting = new PlannedCasting("cast-imp", "long", 0, "source-lifecycle",
                ability, "unit-cleric", "book", CastingTargetMode.DirectTarget,
                "unit-t1", null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null,
                CastingAuthoringState.Draft,
                new MigrationProvenance("legacy-assign-1", 5, "long",
                    "imported:cap-3", null, new[] { "legacy-buff-cap:3" }));
            var document = new CastingPlanDocument("campaign:ack",
                new[] { new RoutineDefinition("long", "Long") },
                new[] { casting },
                new[] { "legacy-provider-ban:Fireball" });
            RepositoryOf(dir).Save(CastingPlanProfile.FromDocument(document));
            var session = new CastingWorkspaceSession(dir, "campaign:ack",
                new DisabledCastingDispatchBoundary());
            if (session.Document.PendingImportNotices.Count == 0)
                throw new InvalidOperationException("the fixture produced no pending notice.");
            var acknowledged = session.AcknowledgeImportNotices();
            if (!acknowledged.Applied)
                throw new InvalidOperationException("acknowledgement refused: " +
                    acknowledged.Reason);
            if (session.IsDirty || session.AutosaveStatus != "saved")
                throw new InvalidOperationException("acknowledgement did not autosave: " +
                    session.AutosaveStatus);
            var fresh = new CastingWorkspaceSession(dir, "campaign:ack");
            if (fresh.Document.PendingImportNotices.Count != 0 ||
                fresh.Document.AcknowledgedImportNotices.Count == 0)
                throw new InvalidOperationException("acknowledgement did not survive a fresh " +
                    "load.");
        }

        // Review F1: the PRODUCTION transition owner (the same
        // CastingSessionOwner the UI root uses) retains a failed-flush
        // intent through campaign switches AND root teardown. The mod
        // directory is SHARED with campaign-keyed files (exactly like the
        // installed mod), the old-session reference is deliberately
        // dropped, and recovery happens purely through the owner's own
        // factory — nothing is recoverable only via a test-held object.
        private static void TestRecoveryOwnerTransition(string root)
        {
            string dir = Path.Combine(root, "recovery-owner-shared-mod");
            Directory.CreateDirectory(dir);
            var store = new CastingWorkspaceRecoveryStore();
            var messages = new System.Collections.Generic.List<string>();
            Func<string, CastingWorkspaceSession> factory = id =>
                store.Adopt(dir, id,
                    pending => new CastingWorkspaceSession(dir, id,
                        new DisabledCastingDispatchBoundary(), null, null, pending),
                    () => new CastingWorkspaceSession(dir, id,
                        new DisabledCastingDispatchBoundary()));
            var owner = new CastingSessionOwner(factory, store, messages.Add);
            CastingWorkspaceSession sessionA;
            if (owner.Ensure("campaign:A", out sessionA) != null)
                throw new InvalidOperationException("the owner refused campaign A.");
            if (!sessionA.AddCastingForRuntime(LifecycleCasting("cast-a1", "unit-t1")).Applied)
                throw new InvalidOperationException("A baseline authoring refused.");
            if (sessionA.IsDirty)
                throw new InvalidOperationException("A baseline was not durable.");
            string planA = RepositoryOf(dir).GetProfilePath("campaign:A");
            string baseline = File.ReadAllText(planA);
            // Campaign switch WHILE A's latest edit cannot persist: the
            // owner registers the intent for recovery (bound to campaign A
            // and this mod path) before the binding changes.
            using (var hold = new FileStream(planA, FileMode.Open, FileAccess.Read,
                FileShare.Read))
            {
                if (!sessionA.AddCastingForRuntime(LifecycleCasting("cast-a2", "unit-t2")).Applied)
                    throw new InvalidOperationException("locked A authoring refused.");
                CastingWorkspaceSession sessionB;
                if (owner.Ensure("campaign:B", out sessionB) != null)
                    throw new InvalidOperationException("the owner refused campaign B.");
                if (sessionB.CampaignId != "campaign:B" || sessionB.Document.Castings.Count != 0)
                    throw new InvalidOperationException("B started contaminated by A.");
                if (File.ReadAllText(planA) != baseline)
                    throw new InvalidOperationException("the last good A file was disturbed.");
            }
            if (!store.HasPending(dir, "campaign:A"))
                throw new InvalidOperationException("the failed-flush intent was not given a " +
                    "recovery owner.");
            if (!messages.Any(message => message.Contains("discard-time flush failed") &&
                    message.Contains("campaign=campaign:A")))
                throw new InvalidOperationException("the failure was not reported with its " +
                    "campaign.");
            // NO test-held reference to A's session: recovery goes through
            // the owner alone, after storage healed.
            sessionA = null;
            CastingWorkspaceSession back;
            if (owner.Ensure("campaign:A", out back) != null)
                throw new InvalidOperationException("the owner refused the return to A.");
            if (back.Document.Castings.Count != 2 ||
                !back.Document.Castings.Any(value => value.CastingId == "cast-a2"))
                throw new InvalidOperationException("returning to A did not recover the " +
                    "latest intent: " + back.Document.Castings.Count);
            if (back.IsDirty || back.AutosaveStatus != "saved")
                throw new InvalidOperationException("the recovered intent was not made " +
                    "durable on adoption: " + back.AutosaveStatus);
            var freshA = new CastingWorkspaceSession(dir, "campaign:A");
            if (freshA.Document.Castings.Count != 2)
                throw new InvalidOperationException("A's durable file is not the recovered " +
                    "intent.");
            if (store.Count != 0)
                throw new InvalidOperationException("adoption left a stale recovery entry.");
            // Root teardown with a FAILED flush: the SAME owner releases;
            // a REPLACEMENT owner (a new UI root in the same process)
            // adopts the intent through the shared store.
            string planA2 = RepositoryOf(dir).GetProfilePath("campaign:A");
            string baseline2 = File.ReadAllText(planA2);
            using (var hold = new FileStream(planA2, FileMode.Open, FileAccess.Read,
                FileShare.Read))
            {
                if (!back.AddCastingForRuntime(LifecycleCasting("cast-a3", "unit-t3")).Applied)
                    throw new InvalidOperationException("locked teardown authoring refused.");
                owner.Release("root-teardown");
                if (!store.HasPending(dir, "campaign:A"))
                    throw new InvalidOperationException("teardown did not hand the intent to " +
                        "the recovery owner.");
                if (File.ReadAllText(planA2) != baseline2)
                    throw new InvalidOperationException("teardown disturbed the last good " +
                        "file.");
            }
            back = null;
            var replacementOwner = new CastingSessionOwner(factory, store, messages.Add);
            CastingWorkspaceSession revived;
            if (replacementOwner.Ensure("campaign:A", out revived) != null)
                throw new InvalidOperationException("the replacement owner refused A.");
            if (revived.Document.Castings.Count != 3 ||
                revived.IsDirty || revived.AutosaveStatus != "saved")
                throw new InvalidOperationException("the replacement root did not recover the " +
                    "latest teardown intent: " + revived.Document.Castings.Count + ";" +
                    revived.AutosaveStatus);
            // Storage heals BEFORE the controlled discard: the transition's
            // own retry succeeds, nothing is registered, and a later
            // session loads the durable file normally.
            string planA3 = RepositoryOf(dir).GetProfilePath("campaign:A");
            string baseline3 = File.ReadAllText(planA3);
            CastingWorkspaceSession current;
            if (replacementOwner.Ensure("campaign:A", out current) != null)
                throw new InvalidOperationException("the replacement owner lost its session.");
            using (var hold = new FileStream(planA3, FileMode.Open, FileAccess.Read,
                FileShare.Read))
            {
                if (!current.AddCastingForRuntime(LifecycleCasting("cast-a4", "unit-t1")).Applied)
                    throw new InvalidOperationException("heal-case authoring refused.");
            }
            // Storage healed (lock released) BEFORE switching away.
            CastingWorkspaceSession sessionB2;
            if (replacementOwner.Ensure("campaign:B", out sessionB2) != null)
                throw new InvalidOperationException("the healed switch was refused.");
            if (store.Count != 0)
                throw new InvalidOperationException("a healed flush still registered a " +
                    "recovery entry.");
            var freshA2 = new CastingWorkspaceSession(dir, "campaign:A");
            if (freshA2.Document.Castings.Count != 4)
                throw new InvalidOperationException("the healed flush did not persist the " +
                    "latest intent: " + freshA2.Document.Castings.Count);
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
