using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.UI
{
    // Review F1: the explicit, reachable owner for intent whose
    // discard-time flush failed. A controlled transition (campaign switch,
    // root teardown) never simply drops a session whose edits could not be
    // made durable: the latest intent is captured — bound to its ORIGINAL
    // mod path and campaign — and handed to this store, so a later session
    // for that exact campaign adopts it without any test- or caller-held
    // reference to the old session object. Recovery is not a second
    // ordinary writer: adoption goes through a fresh session whose
    // repository, files and campaign binding are the same as any other.
    public sealed class PendingSessionRecovery
    {
        internal PendingSessionRecovery(string modPath, string campaignId,
            CastingPlanDocument document, UiProfile uiSettings,
            ExecutionProfile executionSettings, string autosaveStatus,
            IEnumerable<string> archiveObligations = null)
        {
            if (string.IsNullOrWhiteSpace(modPath))
                throw new ArgumentException("Absolute mod path is required.", "modPath");
            if (string.IsNullOrWhiteSpace(campaignId))
                throw new ArgumentException("Exact campaign ID is required.", "campaignId");
            ModPath = modPath;
            CampaignId = campaignId;
            Document = document ?? throw new ArgumentNullException("document");
            UiSettings = uiSettings ?? UiProfile.Default();
            ExecutionSettings = executionSettings ?? ExecutionProfile.Default();
            AutosaveStatus = autosaveStatus ?? string.Empty;
            ArchiveObligations = (archiveObligations ?? new string[0])
                .Where(value => !string.IsNullOrEmpty(value)).ToList().AsReadOnly();
        }

        public string ModPath { get; private set; }
        public string CampaignId { get; private set; }
        public CastingPlanDocument Document { get; private set; }
        public UiProfile UiSettings { get; private set; }
        public ExecutionProfile ExecutionSettings { get; private set; }
        // Why the intent is here (the failed flush's last status), for
        // honest reporting when it is adopted or inspected.
        public string AutosaveStatus { get; private set; }
        // PR #7 review R1: the mandatory pre-write archives the intent still
        // owes (CastingPlanRepository archive labels: retired 0.3.0
        // semantics, the 0.4.2 source-identity migration, a spellbook
        // removal). The adopting session restores them before its first
        // write, so recovery can never turn a failed archive into a save.
        public IReadOnlyList<string> ArchiveObligations { get; private set; }
    }

    // Process-wide (or test-scoped) recovery registry keyed by the exact
    // mod path AND campaign the intent belongs to. Newest capture wins per
    // key; entries leave only by adoption. There is DELIBERATELY NO
    // capacity bound (C853-3): these entries are the only current copies of
    // failed-save intent, so a bound is a data-loss path whether it evicts
    // or refuses a non-refusable teardown; growth is bounded in practice by
    // the distinct campaigns visited in one process, each holding one
    // document. A forced process kill with an unwritable store remains a
    // physical limit this registry cannot span.
    public sealed class CastingWorkspaceRecoveryStore
    {
        public static readonly CastingWorkspaceRecoveryStore Default =
            new CastingWorkspaceRecoveryStore();

        private readonly object _sync = new object();
        private readonly Dictionary<string, PendingSessionRecovery> _pending =
            new Dictionary<string, PendingSessionRecovery>(StringComparer.Ordinal);

        // C853-3: registering intent that could not be made durable always
        // preserves it - no bound, no eviction, no refusal. Updating an
        // existing key replaces that campaign's own older capture with its
        // newest intent.
        public void Register(PendingSessionRecovery pending)
        {
            if (pending == null) throw new ArgumentNullException("pending");
            lock (_sync)
            {
                _pending[Key(pending.ModPath, pending.CampaignId)] = pending;
            }
        }

        // The pending recovery for the EXACT mod path and campaign, without
        // removing it (transactional adoption reads first).
        public PendingSessionRecovery Peek(string modPath, string campaignId)
        {
            if (string.IsNullOrWhiteSpace(modPath) ||
                string.IsNullOrWhiteSpace(campaignId)) return null;
            lock (_sync)
            {
                PendingSessionRecovery pending;
                return _pending.TryGetValue(Key(modPath, campaignId),
                    out pending) ? pending : null;
            }
        }

        // R579-3: adoption is transactional. The entry leaves the store only
        // AFTER the replacement session was constructed successfully - a
        // construction failure leaves the only recoverable copy exactly
        // where it was, available to the next attempt. Ownership transfer is
        // the removal; nothing is dropped on the way.
        public CastingWorkspaceSession Adopt(string modPath, string campaignId,
            Func<PendingSessionRecovery, CastingWorkspaceSession> constructWithRecovery,
            Func<CastingWorkspaceSession> constructWithoutRecovery)
        {
            if (constructWithRecovery == null)
                throw new ArgumentNullException("constructWithRecovery");
            if (constructWithoutRecovery == null)
                throw new ArgumentNullException("constructWithoutRecovery");
            PendingSessionRecovery pending = Peek(modPath, campaignId);
            CastingWorkspaceSession session = pending == null
                ? constructWithoutRecovery()
                : constructWithRecovery(pending);
            if (pending != null)
                lock (_sync)
                {
                    _pending.Remove(Key(modPath, campaignId));
                }
            return session;
        }

        public bool HasPending(string modPath, string campaignId)
        {
            if (string.IsNullOrWhiteSpace(modPath) ||
                string.IsNullOrWhiteSpace(campaignId)) return false;
            lock (_sync)
            {
                return _pending.ContainsKey(Key(modPath, campaignId));
            }
        }

        public int Count
        {
            get { lock (_sync) { return _pending.Count; } }
        }

        private static string Key(string modPath, string campaignId)
        {
            return modPath + "|" + campaignId;
        }
    }

    // The production transition owner for casting workspace sessions
    // (review F1). Every controlled transition the UI root performs goes
    // through here: resolving (or reusing) the session for the loaded
    // campaign, and teardown. A session whose discard-time flush FAILS is
    // never silently relinquished — its latest intent is registered with
    // the recovery store, bound to its own mod path and campaign, before
    // ownership passes on; the next session for that exact campaign adopts
    // it through the normal factory. Not a service locator: the factory,
    // store and log are explicit constructor dependencies.
    public sealed class CastingSessionOwner
    {
        private readonly Func<string, CastingWorkspaceSession> _create;
        private readonly Action<string> _log;

        public CastingSessionOwner(
            Func<string, CastingWorkspaceSession> create,
            CastingWorkspaceRecoveryStore recovery,
            Action<string> log)
        {
            _create = create ?? throw new ArgumentNullException("create");
            Recovery = recovery ?? throw new ArgumentNullException("recovery");
            _log = log ?? (message => { });
        }

        public CastingWorkspaceRecoveryStore Recovery { get; private set; }
        public CastingWorkspaceSession Current { get; private set; }

        // Resolves (or reuses) the session for campaignId. Returns null on
        // success, otherwise why none exists — the same contract the root's
        // EnsureCastingSession reports. A transition whose failed-flush
        // intent cannot be PRESERVED (the recovery store is at its bound) is
        // refused: the current session keeps ownership and its campaign
        // stays bound, so unrecovered intent is never silently dropped
        // (review R579-3).
        public string Ensure(string campaignId, out CastingWorkspaceSession session)
        {
            session = CastingWorkspaceSessionBinding.Resolve(Current, campaignId,
                _create, _log);
            if (session == null) return "campaign identity unresolved";
            if (!ReferenceEquals(session, Current))
                // Review F1/R579-3/C853-3: the outgoing campaign's intent
                // gets a durability attempt, and a failure is ALWAYS
                // preserved with the recovery owner (the store has no
                // lossy bound), before the binding changes.
                ReleaseCurrent("campaign-switch:" + campaignId);
            Current = session;
            return null;
        }

        // Controlled teardown (root destruction, mod shutdown): the same
        // durability attempt and recovery handoff. The current session is
        // not nulled — the object stays reachable for the process's
        // remainder and the failure stays reported — while a replacement
        // root adopts the intent through the store.
        public void Release(string cause)
        {
            // Teardown cannot refuse; a preservation failure is reported
            // loudly and the session stays held by this owner.
            ReleaseCurrent(cause);
        }

        // C853-3: preservation is unconditional - the failed-flush intent
        // is registered with the recovery owner (durable save attempted
        // first), for refusable transitions AND for teardown alike.
        private void ReleaseCurrent(string cause)
        {
            CastingWorkspaceSession retained = Current;
            if (retained == null) return;
            if (!retained.IsDirty) return;
            _log("[KBP-WORKSPACE] flush-before-discard;cause=" + cause +
                ";autosave=" + retained.AutosaveStatus + ".");
            if (retained.RetryFailedSave()) return;
            PendingSessionRecovery pending = retained.CaptureRecovery();
            Recovery.Register(pending);
            _log("[KBP-WORKSPACE] discard-time flush failed;cause=" + cause +
                ";campaign=" + pending.CampaignId + ";autosave=" +
                pending.AutosaveStatus + ";the latest intent is registered " +
                "for recovery under its own campaign and mod path.");
        }
    }
}
