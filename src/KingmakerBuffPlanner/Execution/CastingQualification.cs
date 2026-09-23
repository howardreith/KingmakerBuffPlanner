using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Execution
{
    // Authorization for ONE guarded casting-qualification run through the
    // production casting path (mission section 4): bound to the run id, the
    // exact built artifact (commit, package, DLL, MVID), the fixture
    // campaign, the named recipe and the ORDERED projection ids that the
    // selection run forecast for each executing step, with a hard cap of
    // native submissions (1..24). Unknown members are refused.
    public sealed class CastingQualificationAllowance
    {
        private CastingQualificationAllowance() { }

        public const int AllowanceSchemaVersion = 3;
        public const int MaximumSubmissionsCeiling = 24;

        public string RunId { get; private set; }
        public string SourceCommit { get; private set; }
        public string PackageSha256 { get; private set; }
        public string DllSha256 { get; private set; }
        public string AssemblyMvid { get; private set; }
        public string FixtureGameId { get; private set; }
        public string Recipe { get; private set; }
        public IReadOnlyList<string> ApprovedProjectionIds { get; private set; }
        public int MaximumNativeSubmissions { get; private set; }
        public string ApprovedBy { get; private set; }
        public string Authority { get; private set; }

        private static readonly string[] Members =
        {
            "schemaVersion", "kind", "runId", "sourceCommit", "packageSha256", "dllSha256",
            "assemblyMvid", "fixtureGameId", "recipe", "approvedProjectionIds",
            "maximumNativeSubmissions", "approvedBy", "authority"
        };

        public static CastingQualificationAllowance Parse(string json, string expectedRunId,
            out string refusal)
        {
            refusal = null;
            JObject root;
            try { root = JObject.Parse(json ?? string.Empty); }
            catch (Exception) { refusal = "allowance-unreadable"; return null; }
            List<string> names = root.Properties().Select(property => property.Name).ToList();
            string unknown = names.FirstOrDefault(name => !Members.Contains(name));
            if (unknown != null) { refusal = "allowance-unknown-member:" + unknown; return null; }
            string missing = Members.FirstOrDefault(name => root[name] == null);
            if (missing != null) { refusal = "allowance-missing-member:" + missing; return null; }
            if (root["schemaVersion"].Type != JTokenType.Integer ||
                (int)root["schemaVersion"] != AllowanceSchemaVersion)
            { refusal = "allowance-schema"; return null; }
            if (Text(root, "kind") != "kbp-casting-qualification")
            { refusal = "allowance-kind"; return null; }
            if (root["maximumNativeSubmissions"].Type != JTokenType.Integer)
            { refusal = "allowance-submissions"; return null; }
            int maximum = (int)root["maximumNativeSubmissions"];
            if (maximum < 1 || maximum > MaximumSubmissionsCeiling)
            { refusal = "allowance-submissions-range"; return null; }
            var ids = root["approvedProjectionIds"] as JArray;
            if (ids == null || ids.Count == 0 || ids.Count > 8 ||
                ids.Any(token => token.Type != JTokenType.String ||
                    !SingleCastProbeAllowance.IsLowerHex64((string)token)))
            { refusal = "allowance-projection-ids"; return null; }
            var allowance = new CastingQualificationAllowance
            {
                RunId = Text(root, "runId"),
                SourceCommit = Text(root, "sourceCommit"),
                PackageSha256 = Text(root, "packageSha256"),
                DllSha256 = Text(root, "dllSha256"),
                AssemblyMvid = Text(root, "assemblyMvid"),
                FixtureGameId = Text(root, "fixtureGameId"),
                Recipe = Text(root, "recipe"),
                ApprovedProjectionIds = new ReadOnlyCollection<string>(
                    ids.Select(token => (string)token).ToList()),
                MaximumNativeSubmissions = maximum,
                ApprovedBy = Text(root, "approvedBy"),
                Authority = Text(root, "authority")
            };
            if (string.IsNullOrEmpty(expectedRunId) ||
                !string.Equals(allowance.RunId, expectedRunId, StringComparison.Ordinal))
            { refusal = "allowance-run-mismatch"; return null; }
            Guid mvid;
            if (string.IsNullOrEmpty(allowance.SourceCommit) ||
                !SingleCastProbeAllowance.IsLowerHex64(allowance.PackageSha256) ||
                !SingleCastProbeAllowance.IsLowerHex64(allowance.DllSha256) ||
                allowance.AssemblyMvid == null || !Guid.TryParse(allowance.AssemblyMvid, out mvid) ||
                mvid.ToString("D") != allowance.AssemblyMvid)
            { refusal = "allowance-artifact-identity"; return null; }
            if (string.IsNullOrEmpty(allowance.FixtureGameId))
            { refusal = "allowance-fixture-missing"; return null; }
            if (allowance.Recipe != CastingQualificationRecipe.ZeroCostMixed)
            { refusal = "allowance-recipe-unknown"; return null; }
            if (string.IsNullOrEmpty(allowance.ApprovedBy) || string.IsNullOrEmpty(allowance.Authority))
            { refusal = "allowance-approval-missing"; return null; }
            return allowance;
        }

        private static string Text(JObject root, string name)
        {
            JToken token = root[name];
            return token != null && token.Type == JTokenType.String ? (string)token : null;
        }
    }

    public sealed class CastingQualificationSelection
    {
        internal CastingQualificationSelection(string refusal, string sourceId, AbilityKey ability,
            IList<PlannedCasting> castings, int candidatesConsidered, IList<string> rejections)
        {
            Refusal = refusal ?? string.Empty;
            SourceId = sourceId;
            Ability = ability;
            Castings = new ReadOnlyCollection<PlannedCasting>(
                (castings ?? new PlannedCasting[0]).ToList());
            CandidatesConsidered = candidatesConsidered;
            Rejections = new ReadOnlyCollection<string>((rejections ?? new string[0]).ToList());
        }

        public bool Selected { get { return Castings.Count >= 2; } }
        public string Refusal { get; private set; }
        public string SourceId { get; private set; }
        public AbilityKey Ability { get; private set; }
        public IReadOnlyList<PlannedCasting> Castings { get; private set; }
        public int CandidatesConsidered { get; private set; }
        public IReadOnlyList<string> Rejections { get; private set; }
    }

    // Deterministic authoring recipes for guarded qualification runs. The
    // recipe only chooses; every rule about what may execute is enforced by
    // the ordinary compiler, gate and Standard converter.
    public static class CastingQualificationRecipe
    {
        public const string ZeroCostMixed = "zero-cost-mixed";
        public const string RoutineId = "long";
        public const int MaximumRecordedRejections = 40;
        internal static readonly string[] CastingIds = { "qual-cast-1", "qual-cast-2", "qual-cast-3" };

        // Plain spellbook buffs from verified free sources, cast by rule.
        internal static List<ProviderPlanningOption> EligibleOptions(
            CastingWorkspaceInputs inputs, Action<string> reject)
        {
            var pools = inputs.Snapshot.ResourcePools.ToDictionary(
                pool => pool.PoolKey, pool => pool, StringComparer.Ordinal);
            var eligible = new List<ProviderPlanningOption>();
            foreach (ProviderPlanningOption option in inputs.ProviderOptions
                .Where(value => value != null && value.Provider != null)
                .OrderBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal))
            {
                ProviderSnapshot provider = option.Provider;
                AbilityKey ability = provider.Key.Ability;
                ResourcePoolSnapshot pool;
                string sourceId = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, ability);
                if (ability.SourceKind != SourceKind.Spellbook || ability.MetamagicMask != 0 ||
                    !string.IsNullOrEmpty(ability.SpecialSourceId))
                    reject(provider.Key.Canonical + "|not-plain-spellbook");
                else if (option.ExecutionStrategy != CastExecutionStrategy.DirectRuleCast)
                    reject(provider.Key.Canonical + "|strategy:" + option.ExecutionStrategy);
                else if (!pools.TryGetValue(provider.ResourcePoolKey, out pool) ||
                    pool.Kind != ResourcePoolKind.Unlimited)
                    reject(provider.Key.Canonical + "|not-verified-free");
                else if (sourceId == null ||
                    !ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(
                        inputs.EffectsBySource[sourceId], ability.BaseAbilityGuid))
                    reject(provider.Key.Canonical + "|effect-shape");
                else
                    eligible.Add(option);
            }
            return eligible;
        }

        internal static bool EffectActive(ActiveEffectSnapshot live, string unitId,
            EffectExpression expected)
        {
            if (live == null || expected == null) return false;
            return new EffectPresenceEvaluator().EvaluateTyped(expected, live.GetEffects(unitId),
                null).Kind == EffectPresenceKind.Complete;
        }

        // One verified-free plain buff that two DIFFERENT casters can cast,
        // on two or three distinct other party members without the effect:
        // qual-cast-1 = first caster on the first target, qual-cast-2 =
        // second caster on the second target, qual-cast-3 = first caster on
        // the third target (when one exists).
        public static CastingQualificationSelection SelectZeroCostMixed(
            CastingWorkspaceInputs inputs, string campaignId)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            var rejections = new List<string>();
            Action<string> reject = value =>
            {
                if (rejections.Count < MaximumRecordedRejections) rejections.Add(value);
            };
            var targetable = new HashSet<string>(inputs.Snapshot.Units
                .Where(unit => unit.TargetValidation.Alive && unit.TargetValidation.Conscious &&
                    unit.TargetValidation.Friendly && unit.TargetValidation.Targetable)
                .Select(unit => unit.UnitId), StringComparer.Ordinal);
            int considered = 0;
            foreach (IGrouping<string, ProviderPlanningOption> group in EligibleOptions(inputs, reject)
                .GroupBy(option => option.Provider.Key.Ability.Canonical, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                considered++;
                List<ProviderPlanningOption> byCaster = group
                    .GroupBy(option => option.Provider.Key.CasterUnitId, StringComparer.Ordinal)
                    .OrderBy(perCaster => perCaster.Key, StringComparer.Ordinal)
                    .Select(perCaster => perCaster.OrderBy(option => option.Provider.Key.Canonical,
                        StringComparer.Ordinal).First())
                    .ToList();
                if (byCaster.Count < 2) { reject(group.Key + "|fewer-than-two-casters"); continue; }
                ProviderPlanningOption first = byCaster[0];
                ProviderPlanningOption second = byCaster[1];
                AbilityKey ability = first.Provider.Key.Ability;
                string sourceId = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, ability);
                EffectExpression expected = inputs.EffectsBySource[sourceId];
                var casters = new HashSet<string>(new[]
                    { first.Provider.Key.CasterUnitId, second.Provider.Key.CasterUnitId },
                    StringComparer.Ordinal);
                List<string> targets = inputs.Snapshot.Units.Select(unit => unit.UnitId)
                    .Where(unit => targetable.Contains(unit) && !casters.Contains(unit) &&
                        first.ReachableTargetIds.Contains(unit) &&
                        second.ReachableTargetIds.Contains(unit) &&
                        !EffectActive(inputs.LiveEffects, unit, expected))
                    .OrderBy(unit => unit, StringComparer.Ordinal).Take(3).ToList();
                if (targets.Count < 2) { reject(group.Key + "|fewer-than-two-fresh-targets"); continue; }
                var castings = new List<PlannedCasting>();
                for (int index = 0; index < targets.Count; index++)
                {
                    ProviderPlanningOption option = index == 1 ? second : first;
                    castings.Add(new PlannedCasting(CastingIds[index], RoutineId, index, sourceId,
                        ability, option.Provider.Key.CasterUnitId, option.Provider.Key.SpellbookGuid,
                        CastingTargetMode.DirectTarget, targets[index], null, null, null, null,
                        ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null));
                }
                CastingQualificationStepForecast check = CastingQualificationForecast.Project(
                    "check", CastingQualificationForecast.BuildDocument(campaignId, castings),
                    inputs, inputs.LiveEffects);
                if (check.Refusal != null || check.CastingIds.Count != castings.Count)
                {
                    reject(group.Key + "|not-executable:" + (check.Refusal ?? "partial"));
                    continue;
                }
                return new CastingQualificationSelection(null, sourceId, ability, castings,
                    considered, rejections);
            }
            return new CastingQualificationSelection("no-eligible-qualification-recipe", null, null,
                null, considered, rejections);
        }
    }

    public sealed class CastingQualificationStepForecast
    {
        internal CastingQualificationStepForecast(string name, string refusal,
            ExplicitStepConversion projection, IList<string> castingIds)
        {
            Projection = projection != null && projection.Converted ? projection : null;
            Name = name ?? string.Empty;
            Refusal = refusal;
            ProjectionId = projection == null || !projection.Converted
                ? null : projection.ProjectionId;
            CanonicalContract = projection == null || !projection.Converted
                ? null : projection.CanonicalContract;
            CastingIds = new ReadOnlyCollection<string>((castingIds ?? new string[0]).ToList());
        }

        public string Name { get; private set; }
        // The converted projection itself (its steps name each caster,
        // target and source to observe).
        internal ExplicitStepConversion Projection { get; private set; }
        // Null when the step projects; otherwise why it does not.
        public string Refusal { get; private set; }
        public string ProjectionId { get; private set; }
        public string CanonicalContract { get; private set; }
        public IReadOnlyList<string> CastingIds { get; private set; }
    }

    // The exact projection each executing step of the zero-cost-mixed
    // qualification will submit, forecast from the selection-time state:
    //   stop     - the fresh plan (every casting; the run is stopped after
    //              the first casting finishes),
    //   complete - qual-cast-1 already active (skipped), the rest execute,
    //   recast   - every target active, qual-cast-1 set to Always recast,
    //              so only it executes (after a close and reopen).
    // The repeat step (everything active) submits nothing. A later step
    // whose real projection differs from its forecast is refused.
    public static class CastingQualificationForecast
    {
        public const string Stop = "stop";
        public const string Complete = "complete";
        public const string Recast = "recast";

        public static IReadOnlyList<CastingQualificationStepForecast> Forecast(
            CastingQualificationSelection selection, CastingWorkspaceInputs inputs,
            string campaignId)
        {
            if (selection == null || !selection.Selected)
                throw new ArgumentException("A selected recipe is required.", "selection");
            CastingPlanDocument document = BuildDocument(campaignId, selection.Castings);
            EffectExpression expected = inputs.EffectsBySource[selection.SourceId];
            PlannedCasting firstCasting = selection.Castings[0];
            var casterLevels = inputs.Snapshot.Providers
                .GroupBy(provider => provider.Key.CasterUnitId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group
                    .Where(provider => provider.Key.Ability.Canonical == selection.Ability.Canonical)
                    .Select(provider => provider.EffectiveCasterLevel).DefaultIfEmpty(0).Max(),
                    StringComparer.Ordinal);
            Func<PlannedCasting, KeyValuePair<string, int>> grant = casting =>
                new KeyValuePair<string, int>(casting.DirectTargetUnitId,
                    casterLevels.ContainsKey(casting.CasterUnitId) ? casterLevels[casting.CasterUnitId] : 0);
            var steps = new List<CastingQualificationStepForecast>
            {
                Project(Stop, document, inputs, inputs.LiveEffects),
                Project(Complete, document, inputs, WithGranted(inputs, expected,
                    new[] { grant(firstCasting) })),
                Project(Recast, WithPolicy(document, firstCasting.CastingId,
                    ExistingEffectPolicy.Overwrite), inputs, WithGranted(inputs, expected,
                    selection.Castings.Select(grant)))
            };
            return new ReadOnlyCollection<CastingQualificationStepForecast>(steps);
        }

        // The document exactly as a fresh session holds it after the recipe
        // castings are added in order through the authoring service.
        public static CastingPlanDocument BuildDocument(string campaignId,
            IEnumerable<PlannedCasting> castings)
        {
            var authoring = new CastingAuthoringService(new CastingPlanDocument(campaignId,
                new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                }, new PlannedCasting[0]));
            foreach (PlannedCasting casting in castings)
            {
                AuthoringEditResult added = authoring.AddCasting(casting);
                if (!added.Applied)
                    throw new InvalidOperationException("recipe-casting-refused:" + casting.CastingId);
            }
            return authoring.Document;
        }

        internal static CastingPlanDocument WithPolicy(CastingPlanDocument document,
            string castingId, ExistingEffectPolicy policy)
        {
            var authoring = new CastingAuthoringService(document);
            PlannedCasting current = document.Castings.First(value => value.CastingId == castingId);
            AuthoringEditResult updated = authoring.UpdateCasting(new PlannedCasting(
                current.CastingId, current.RoutineId, current.Order, current.SourceId,
                current.Ability, current.CasterUnitId, current.SpellbookGuid, current.TargetMode,
                current.DirectTargetUnitId, current.Origin, current.RequiredCoverageUnitIds,
                current.TargetingModifiers, current.Enhancements, policy,
                current.IgnoredPresenceMarkers, current.State, current.Provenance));
            if (!updated.Applied)
                throw new InvalidOperationException("recipe-policy-refused:" + castingId);
            return authoring.Document;
        }

        // The compile, gate and Standard conversion the production Apply
        // performs, for the routine scope, against the given live effects.
        internal static CastingQualificationStepForecast Project(string name,
            CastingPlanDocument document, CastingWorkspaceInputs inputs, ActiveEffectSnapshot live)
        {
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document,
                inputs.Snapshot, inputs.ProviderOptions, inputs.EffectsBySource, inputs.Enhancements,
                CastingQualificationRecipe.RoutineId, inputs.TargetingModifiers, false, live);
            CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan,
                CastingApplyMode.Ordinary, CastingQualificationRecipe.RoutineId);
            if (!decision.Allowed)
                return new CastingQualificationStepForecast(name,
                    "gate-refused:" + string.Join(",", decision.BlockingReasons.ToArray()), null, null);
            if (decision.ExecutableCastingIds.Count == 0)
                return new CastingQualificationStepForecast(name, "nothing-to-cast", null, null);
            ExplicitStepConversion projection = ExplicitCastingStepConverter.Convert(plan, decision,
                inputs.ProviderOptions, inputs.EffectsBySource);
            if (!projection.Converted)
                return new CastingQualificationStepForecast(name,
                    "projection-refused:" + projection.Refusal, null, null);
            return new CastingQualificationStepForecast(name, null, projection,
                projection.CastingIds.ToList());
        }

        // The live effects plus the expected effect on each given unit, at
        // the granting caster level, with no expiry (a fresh instance).
        internal static ActiveEffectSnapshot WithGranted(CastingWorkspaceInputs inputs,
            EffectExpression expected, IEnumerable<KeyValuePair<string, int>> grants)
        {
            var byUnit = new Dictionary<string, List<ActiveEffectInstance>>(StringComparer.Ordinal);
            foreach (UnitSnapshot unit in inputs.Snapshot.Units)
                byUnit[unit.UnitId] = inputs.LiveEffects == null
                    ? new List<ActiveEffectInstance>()
                    : inputs.LiveEffects.HasInstanceDetail
                        ? inputs.LiveEffects.GetInstances(unit.UnitId).ToList()
                        : inputs.LiveEffects.GetEffects(unit.UnitId).Select(marker =>
                            new ActiveEffectInstance(marker.Kind, marker.EffectId, null, null, null)).ToList();
            List<EffectLeafExpression> leaves = Leaves(expected).ToList();
            foreach (KeyValuePair<string, int> grant in grants)
            {
                List<ActiveEffectInstance> instances;
                if (!byUnit.TryGetValue(grant.Key, out instances)) continue;
                foreach (EffectLeafExpression leaf in leaves)
                    instances.Add(new ActiveEffectInstance(leaf.Kind, leaf.EffectId, null,
                        grant.Value > 0 ? (int?)grant.Value : null, 0));
            }
            return ActiveEffectSnapshot.FromInstances(byUnit.ToDictionary(pair => pair.Key,
                pair => (IEnumerable<ActiveEffectInstance>)pair.Value, StringComparer.Ordinal));
        }

        internal static IEnumerable<EffectLeafExpression> Leaves(EffectExpression expression)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) { yield return leaf; yield break; }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children)
                    foreach (EffectLeafExpression inner in Leaves(child)) yield return inner;
                yield break;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                foreach (EffectLeafExpression inner in Leaves(targeted.Child)) yield return inner;
                yield break;
            }
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null)
            {
                foreach (EffectLeafExpression inner in Leaves(referenced.Child)) yield return inner;
                yield break;
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                foreach (EffectLeafExpression inner in Leaves(conditional.WhenTrue)) yield return inner;
                foreach (EffectLeafExpression inner in Leaves(conditional.WhenFalse)) yield return inner;
            }
        }
    }

    // The qualification dispatch boundary: the production validation
    // (exact Standard projection of the decision), then the allowance - the
    // projection must be the NEXT approved id in order and the planned steps
    // must fit the remaining submission budget - then the production
    // execution host. Every submission is recorded; nothing is retried.
    public sealed class CastingQualificationBoundary : ICastingDispatchBoundary
    {
        private readonly CastingQualificationAllowance _allowance;
        private readonly CastingExecutionHost _host;
        private readonly Func<ExecutionProfile> _settings;
        private int _nextApproved;

        public CastingQualificationBoundary(CastingQualificationAllowance allowance,
            CastingExecutionHost host, Func<ExecutionProfile> settings)
        {
            _allowance = allowance ?? throw new ArgumentNullException("allowance");
            _host = host ?? throw new ArgumentNullException("host");
            _settings = settings ?? throw new ArgumentNullException("settings");
        }

        public readonly List<string> Submissions = new List<string>();
        public int PlannedSubmissions { get; private set; }

        public string DispositionReason
        {
            get
            {
                return _nextApproved >= _allowance.ApprovedProjectionIds.Count
                    ? "qualification-allowance-exhausted" : "qualification-armed";
            }
        }

        public CastingDispatchOutcome Submit(ExplicitCastingPlan plan,
            CastingApplyDecision decision, string scopeRoutineId,
            ExplicitStepConversion projection)
        {
            IReadOnlyList<string> ids = projection == null
                ? (IReadOnlyList<string>)new string[0] : projection.CastingIds;
            string refusal = NativeCastingDispatchBoundary.Validate(plan, decision, projection);
            if (refusal == null && _nextApproved >= _allowance.ApprovedProjectionIds.Count)
                refusal = "qualification-allowance-exhausted";
            if (refusal == null && !string.Equals(projection.ProjectionId,
                    _allowance.ApprovedProjectionIds[_nextApproved], StringComparison.Ordinal))
                refusal = "qualification-projection-not-approved:step=" + _nextApproved;
            if (refusal == null &&
                PlannedSubmissions + projection.Plan.Steps.Count > _allowance.MaximumNativeSubmissions)
                refusal = "qualification-submission-cap:" + _allowance.MaximumNativeSubmissions;
            if (refusal != null)
            {
                Submissions.Add("refused:" + refusal);
                return new CastingDispatchOutcome(false, refusal, ids);
            }
            ExecutionProfile settings;
            try { settings = _settings(); }
            catch (Exception exception)
            {
                Submissions.Add("refused:settings");
                return new CastingDispatchOutcome(false, "execution-settings-unavailable:" +
                    exception.GetType().Name, ids);
            }
            CastingDispatchOutcome outcome = _host.Start(plan, decision, scopeRoutineId,
                projection, settings);
            Submissions.Add((outcome.Submitted ? "submitted:" : "refused:") + outcome.Reason +
                ";projection=" + projection.ProjectionId);
            if (outcome.Submitted)
            {
                // An approved id is consumed by its submission, whatever the
                // run then does; the budget counts planned invocations.
                _nextApproved++;
                PlannedSubmissions += projection.Plan.Steps.Count;
            }
            return outcome;
        }
    }
}
