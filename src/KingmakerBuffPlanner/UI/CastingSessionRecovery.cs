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
            ExecutionProfile executionSettings, string autosaveStatus)
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
        }

        public string ModPath { get; private set; }
        public string CampaignId { get; private set; }
        public CastingPlanDocument Document { get; private set; }
        public UiProfile UiSettings { get; private set; }
        public ExecutionProfile ExecutionSettings { get; private set; }
        // Why the intent is here (the failed flush's last status), for
        // honest reporting when it is adopted or inspected.
        public string AutosaveStatus { get; private set; }
    }

    // Process-wide (or test-scoped) recovery registry keyed by the exact
    // mod path AND campaign the intent belongs to. Newest capture wins per
    // key; entries leave only by adoption. A forced process kill with an
    // unwritable store remains a physical limit this registry cannot span.
    public sealed class CastingWorkspaceRecoveryStore
    {
        public static readonly CastingWorkspaceRecoveryStore Default =
            new CastingWorkspaceRecoveryStore();

        // A bound on distinct pending campaigns so unbounded growth is
        // impossible; reaching it REFUSES a new registration (the
        // relinquishing transition is refused) rather than dropping
        // any unrecovered intent (review R579-3).
        private const int Capacity = 8;
        private readonly object _sync = new object();
        private readonly Dictionary<string, PendingSessionRecovery> _pending =
            new Dictionary<string, PendingSessionRecovery>(StringComparer.Ordinal);
        private readonly List<string> _order = new List<string>();

        // R579-3: registering intent that could not be made durable NEVER
        // evicts an unrecovered entry - these are the only current copies of
        // failed-save edits, not a cache. Returns false when a NEW key would
        // exceed the bound (nothing is dropped; the CALLER must then refuse
        // the relinquishing transition so the session keeps ownership).
        // Updating an existing key is always allowed (the newest intent for
        // that campaign replaces its own older capture).
        public bool Register(PendingSessionRecovery pending)
        {
            if (pending == null) throw new ArgumentNullException("pending");
            lock (_sync)
            {
                string key = Key(pending.ModPath, pending.CampaignId);
                if (!_pending.ContainsKey(key))
                {
                    if (_pending.Count >= Capacity) return false;
                    _order.Add(key);
                }
                _pending[key] = pending;
                return true;
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
                    string key = Key(modPath, campaignId);
                    _pending.Remove(key);
                    _order.Remove(key);
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
            if (!ReferenceEquals(session, Current) &&
                !ReleaseCurrent("campaign-switch:" + campaignId))
            {
                // Refuse the replaceable transition: ownership never passes
                // without a durable save or a registered recovery owner.
                session = null;
                return "transition-refused:recovery-preservation-unavailable:" +
                    campaignId;
            }
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

        // True when the current session's intent is durable or safely
        // registered with the recovery owner; false when ownership must NOT
        // pass (the caller refuses the transition).
        private bool ReleaseCurrent(string cause)
        {
            CastingWorkspaceSession retained = Current;
            if (retained == null) return true;
            if (!retained.IsDirty) return true;
            _log("[KBP-WORKSPACE] flush-before-discard;cause=" + cause +
                ";autosave=" + retained.AutosaveStatus + ".");
            if (retained.RetryFailedSave()) return true;
            PendingSessionRecovery pending = retained.CaptureRecovery();
            if (!Recovery.Register(pending))
            {
                _log("[KBP-WORKSPACE] discard-time flush failed AND recovery " +
                    "preservation is unavailable;cause=" + cause +
                    ";campaign=" + pending.CampaignId + ";the transition is " +
                    "REFUSED - the retained session keeps ownership of its " +
                    "latest intent.");
                return false;
            }
            _log("[KBP-WORKSPACE] discard-time flush failed;cause=" + cause +
                ";campaign=" + pending.CampaignId + ";autosave=" +
                pending.AutosaveStatus + ";the latest intent is registered " +
                "for recovery under its own campaign and mod path.");
            return true;
        }
    }
}
