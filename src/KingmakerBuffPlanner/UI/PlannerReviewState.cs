using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.UI
{
    // Explicitly acknowledged review state, separate from computed previews.
    // A preview becomes a reviewed baseline only when the player actually
    // sees it in the planner; incidental previews, forecasts, and refused
    // executions never acknowledge anything. Scoped by campaign and routine;
    // a campaign switch or profile reload invalidates it.
    public sealed class PlannerReviewState
    {
        private string _campaignId;
        private string _routineId;
        private CastPlan _plan;
        private int _acknowledgement;

        public int Acknowledgement { get { return _acknowledgement; } }

        public bool HasReviewed(string campaignId, string routineId)
        {
            return _plan != null &&
                string.Equals(_campaignId, campaignId, StringComparison.Ordinal) &&
                string.Equals(_routineId, routineId, StringComparison.Ordinal);
        }

        // Acknowledging a different routine replaces the baseline; only one
        // routine's preview is on screen at a time.
        public void Acknowledge(string campaignId, string routineId, CastPlan plan)
        {
            if (string.IsNullOrWhiteSpace(campaignId))
                throw new ArgumentException("Campaign is required.", "campaignId");
            if (string.IsNullOrWhiteSpace(routineId))
                throw new ArgumentException("Routine is required.", "routineId");
            if (plan == null) throw new ArgumentNullException("plan");
            _campaignId = campaignId;
            _routineId = routineId;
            _plan = plan;
            _acknowledgement++;
        }

        public CastPlan ReviewedPlan(string campaignId, string routineId)
        {
            return HasReviewed(campaignId, routineId) ? _plan : null;
        }

        // A campaign change invalidates any prior acknowledgement.
        public void ObserveCampaign(string campaignId)
        {
            if (_plan != null && !string.Equals(_campaignId, campaignId,
                    StringComparison.Ordinal))
            {
                _plan = null;
                _campaignId = null;
                _routineId = null;
            }
        }

        // After an execution the native state changed by design; the next
        // application must review the new state first.
        public void Invalidate(string routineId)
        {
            if (_plan != null && string.Equals(_routineId, routineId,
                    StringComparison.Ordinal))
            {
                _plan = null;
                _routineId = null;
            }
        }
    }
}
