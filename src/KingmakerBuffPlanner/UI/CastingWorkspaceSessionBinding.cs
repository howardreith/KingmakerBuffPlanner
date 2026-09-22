using System;

namespace KingmakerBuffPlanner.UI
{
    // Campaign-bound session reuse (review G3). Pure policy, no Unity
    // dependencies, so the isolated lifecycle tests exercise it directly:
    // a retained session is reused ONLY when its verified campaign
    // identity matches the current one; a campaign switch creates the
    // matching binding (disclosing any dirty retained intent) so campaign
    // A's document can never be exposed or saved against campaign B; an
    // unresolved identity refuses rather than binding to an
    // unknown-campaign fallback.
    internal static class CastingWorkspaceSessionBinding
    {
        public static CastingWorkspaceSession Resolve(
            CastingWorkspaceSession retained,
            string campaignId,
            Func<string, CastingWorkspaceSession> create,
            Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(campaignId)) return null;
            if (retained != null)
            {
                if (string.Equals(retained.CampaignId, campaignId,
                        StringComparison.Ordinal))
                    return retained;
                if (log != null)
                    log("[KBP-WORKSPACE] campaign identity changed;retained=" +
                        retained.CampaignId + ";current=" + campaignId +
                        ";dirty=" + retained.IsDirty +
                        ";switching to the matching candidate binding; " +
                        "unsaved retained intent is NOT relabeled.");
                return create(campaignId);
            }
            return create(campaignId);
        }
    }
}
