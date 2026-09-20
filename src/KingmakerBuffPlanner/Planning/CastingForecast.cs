using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Planning
{
    // One forecast view over the canonical casting document. A routine view
    // budgets only that routine's castings; the one-pass view budgets every
    // routine in its declared sequence against one shared ledger. Every view
    // is a preview: computing it reserves nothing in the game and approves
    // nothing for execution.
    public sealed class CastingForecast
    {
        internal CastingForecast(
            string scopeRoutineId,
            IReadOnlyList<string> routineSequence,
            ExplicitCastingPlan plan,
            IEnumerable<string> assumptions)
        {
            ScopeRoutineId = scopeRoutineId;
            RoutineSequence = routineSequence;
            Plan = plan;
            Assumptions = new ReadOnlyCollection<string>(
                (assumptions ?? new string[0]).ToList());
        }

        // Null for the one-pass sequence view.
        public string ScopeRoutineId { get; private set; }
        public IReadOnlyList<string> RoutineSequence { get; private set; }
        public ExplicitCastingPlan Plan { get; private set; }
        public IReadOnlyList<string> Assumptions { get; private set; }

        public IReadOnlyList<ResolvedCasting> InScopeCastings
        {
            get
            {
                return Plan.Castings
                    .Where(value => ScopeRoutineId == null ||
                        value.RoutineId == ScopeRoutineId)
                    .ToList();
            }
        }

        public int ReadyInvocations
        {
            get { return InScopeCastings.Count(value => value.IsExecutable); }
        }
    }

    // Builds preview forecasts on the same resolved representation the
    // executor will consume. Independent routine previews are computed
    // against fresh ledgers — previewing several routines separately never
    // multiplies charges — while the one-pass sequence carries balances
    // forward across routines in one shared budget.
    public sealed class CastingForecastService
    {
        private static readonly string[] ForecastAssumptions =
        {
            "no-rest",
            "no-elapsed-game-time",
            "resources-carried-forward-within-the-view",
            "coverage-and-strength-are-predictions",
            "projected-effects-are-structural-presence-only"
        };

        private readonly ExplicitCastingCompiler _compiler =
            new ExplicitCastingCompiler();

        public CastingForecast ForecastRoutine(
            CastingPlanDocument document,
            string routineId,
            PartyProviderSnapshot snapshot,
            IEnumerable<ProviderPlanningOption> providerOptions,
            IDictionary<string, EffectExpression> effectsBySource,
            IEnumerable<CastEnhancementSnapshot> enhancements = null)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (document.Routines.All(value => value.RoutineId != routineId))
                throw new ArgumentException("Unknown routine.", "routineId");
            ExplicitCastingPlan plan = _compiler.Compile(
                document, snapshot, providerOptions, effectsBySource, enhancements,
                routineId);
            return new CastingForecast(
                routineId,
                new[] { routineId },
                plan,
                ForecastAssumptions);
        }

        public CastingForecast ForecastOnePass(
            CastingPlanDocument document,
            PartyProviderSnapshot snapshot,
            IEnumerable<ProviderPlanningOption> providerOptions,
            IDictionary<string, EffectExpression> effectsBySource,
            IEnumerable<CastEnhancementSnapshot> enhancements = null,
            IEnumerable<ICastingTargetingModifier> targetingModifiers = null)
        {
            if (document == null) throw new ArgumentNullException("document");
            // The one-pass sequence carries structural effect presence
            // forward between routines as well as resource balances.
            ExplicitCastingPlan plan = _compiler.Compile(
                document, snapshot, providerOptions, effectsBySource, enhancements,
                null, targetingModifiers, true);
            return new CastingForecast(
                null,
                document.Routines.Select(value => value.RoutineId).ToList(),
                plan,
                ForecastAssumptions);
        }
    }
}
