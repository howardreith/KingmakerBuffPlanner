using System;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.UI
{
    // Orchestration protocol for review acknowledgment, extracted so the
    // exact call order the session performs is provable without Unity:
    // computing a plan never acknowledges; only a presentation acknowledgment
    // for the exact routine and campaign whose plan was just bound to the
    // visible controls does. Refusals, preflight computation, resource
    // inspection, and forecasts call only the compute side.
    public sealed class PlannerReviewCoordinator
    {
        private readonly PlannerReviewState _state = new PlannerReviewState();
        private string _lastComputedRoutine;
        private string _lastComputedCampaign;

        public PlannerReviewState State { get { return _state; } }

        // Side-effect-free with respect to review: records only what a later
        // presentation acknowledgment must match.
        public void PlanComputed(string campaignId, string routineId)
        {
            _lastComputedCampaign = campaignId;
            _lastComputedRoutine = routineId;
            _state.ObserveCampaign(campaignId);
        }

        // Presentation acknowledgment: valid only when the routine whose plan
        // was just bound on screen is the routine whose plan was last
        // computed for this campaign — binding stale controls must not
        // acknowledge.
        public void Presented(string campaignId, string routineId, CastPlan plan)
        {
            if (plan == null) return;
            if (!string.Equals(_lastComputedRoutine, routineId,
                    StringComparison.Ordinal) ||
                !string.Equals(_lastComputedCampaign, campaignId,
                    StringComparison.Ordinal))
                return;
            _state.Acknowledge(campaignId, routineId, plan);
        }

        public CastPlan BaselineFor(string campaignId, string routineId)
        {
            return _state.ReviewedPlan(campaignId, routineId);
        }

        public void Spent(string routineId)
        {
            _state.Invalidate(routineId);
        }

        public void CampaignChanged(string campaignId)
        {
            _state.ObserveCampaign(campaignId);
        }
    }
}
