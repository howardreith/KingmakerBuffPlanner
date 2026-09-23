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
            if (!CastingQualificationRecipe.IsKnown(allowance.Recipe))
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
            IList<PlannedCasting> castings, int candidatesConsidered, IList<string> rejections,
            string recipe = CastingQualificationRecipe.ZeroCostMixed,
            IEnumerable<string> coverage = null)
        {
            Recipe = recipe ?? CastingQualificationRecipe.ZeroCostMixed;
            Coverage = new ReadOnlyCollection<string>((coverage ?? new string[0]).ToList());
            Refusal = refusal ?? string.Empty;
            SourceId = sourceId;
            Ability = ability;
            Castings = new ReadOnlyCollection<PlannedCasting>(
                (castings ?? new PlannedCasting[0]).ToList());
            CandidatesConsidered = candidatesConsidered;
            Rejections = new ReadOnlyCollection<string>((rejections ?? new string[0]).ToList());
        }

        public bool Selected { get { return Castings.Count >= 2; } }
        public string Recipe { get; private set; }
        // What the selected castings exercise (for example "prepared",
        // "spontaneous", "metamagic"); reported, never assumed.
        public IReadOnlyList<string> Coverage { get; private set; }
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
        public const string FiniteDirectMixed = "finite-direct-mixed";
        public const string RoutineId = "long";

        public static bool IsKnown(string recipe)
        {
            return recipe == ZeroCostMixed || recipe == FiniteDirectMixed;
        }

        public static CastingQualificationSelection Select(string recipe,
            CastingWorkspaceInputs inputs, string campaignId)
        {
            if (recipe == FiniteDirectMixed) return SelectFiniteDirectMixed(inputs, campaignId);
            if (recipe == ZeroCostMixed) return SelectZeroCostMixed(inputs, campaignId);
            return new CastingQualificationSelection("unknown-recipe:" + recipe, null, null,
                null, 0, null, recipe);
        }
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
        // on two or three distinct recipients without the effect, chosen per
        // casting (a caster may receive from the other caster, never from
        // itself): qual-cast-1 = first caster, qual-cast-2 = second caster,
        // qual-cast-3 = first caster again (when a third recipient exists).
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
                List<string> targets = RecipientsPerCasting(inputs, targetable, expected,
                    new[] { first, second, first });
                if (targets.Count < 2) { reject(group.Key + "|fewer-than-two-fresh-recipients"); continue; }
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

        // Finite spellbook resources: one plain buff source that two
        // DIFFERENT casters each cast from their own finite pool (prepared
        // exact slots or spontaneous levels), on two other party members
        // without the effect. qual-cast-1 = caster A on target 1 (cast in
        // the stop step and again in the recast step, so A needs two
        // casts); qual-cast-2 = caster B on target 2 (cast once, in the
        // complete step). A pair with one prepared and one spontaneous
        // caster is preferred so one run covers both kinds.
        public static CastingQualificationSelection SelectFiniteDirectMixed(
            CastingWorkspaceInputs inputs, string campaignId)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            var rejections = new List<string>();
            Action<string> reject = value =>
            {
                if (rejections.Count < MaximumRecordedRejections) rejections.Add(value);
            };
            var pools = inputs.Snapshot.ResourcePools.ToDictionary(
                pool => pool.PoolKey, pool => pool, StringComparer.Ordinal);
            var targetable = new HashSet<string>(inputs.Snapshot.Units
                .Where(unit => unit.TargetValidation.Alive && unit.TargetValidation.Conscious &&
                    unit.TargetValidation.Friendly && unit.TargetValidation.Targetable)
                .Select(unit => unit.UnitId), StringComparer.Ordinal);
            var eligible = new List<KeyValuePair<string, ProviderPlanningOption>>();
            foreach (ProviderPlanningOption option in inputs.ProviderOptions
                .Where(value => value != null && value.Provider != null)
                .OrderBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal))
            {
                ProviderSnapshot provider = option.Provider;
                AbilityKey ability = provider.Key.Ability;
                ResourcePoolSnapshot pool;
                string sourceId = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, ability);
                if (ability.SourceKind != SourceKind.Spellbook || !string.IsNullOrEmpty(ability.SpecialSourceId))
                    reject(provider.Key.Canonical + "|not-plain-spellbook");
                else if (option.ExecutionStrategy != CastExecutionStrategy.DirectRuleCast)
                    reject(provider.Key.Canonical + "|strategy:" + option.ExecutionStrategy);
                else if (!pools.TryGetValue(provider.ResourcePoolKey, out pool) ||
                    (pool.Kind != ResourcePoolKind.PreparedSlots &&
                     pool.Kind != ResourcePoolKind.SpontaneousLevel))
                    reject(provider.Key.Canonical + "|not-finite-spellbook-pool");
                else if (sourceId == null ||
                    !ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(
                        inputs.EffectsBySource[sourceId], ability.BaseAbilityGuid))
                    reject(provider.Key.Canonical + "|effect-shape");
                else
                    eligible.Add(new KeyValuePair<string, ProviderPlanningOption>(sourceId, option));
            }
            int considered = 0;
            foreach (IGrouping<string, KeyValuePair<string, ProviderPlanningOption>> group in eligible
                .GroupBy(pair => pair.Key, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                considered++;
                string sourceId = group.Key;
                EffectExpression expected = inputs.EffectsBySource[sourceId];
                // Per caster: the option (canonical order) with the most
                // casts available, up to two.
                List<KeyValuePair<ProviderPlanningOption, int>> byCaster = group
                    .Select(pair => pair.Value)
                    .GroupBy(option => option.Provider.Key.CasterUnitId, StringComparer.Ordinal)
                    .OrderBy(perCaster => perCaster.Key, StringComparer.Ordinal)
                    .Select(perCaster => perCaster
                        .Select(option => new KeyValuePair<ProviderPlanningOption, int>(
                            option, CastsAvailable(inputs, option.Provider, 2)))
                        .OrderByDescending(pair => pair.Value)
                        .ThenBy(pair => pair.Key.Provider.Key.Canonical, StringComparer.Ordinal)
                        .First())
                    .ToList();
                ProviderPlanningOption first = null;
                ProviderPlanningOption second = null;
                int bestScore = -1;
                foreach (KeyValuePair<ProviderPlanningOption, int> a in byCaster.Where(value => value.Value >= 2))
                    foreach (KeyValuePair<ProviderPlanningOption, int> other in byCaster.Where(value => value.Value >= 1))
                    {
                        if (a.Key.Provider.Key.CasterUnitId == other.Key.Provider.Key.CasterUnitId) continue;
                        int score = pools[a.Key.Provider.ResourcePoolKey].Kind !=
                            pools[other.Key.Provider.ResourcePoolKey].Kind ? 1 : 0;
                        if (score <= bestScore) continue;
                        bestScore = score;
                        first = a.Key;
                        second = other.Key;
                    }
                if (first == null) { reject(sourceId + "|no-caster-pair-with-casts:2+1"); continue; }
                List<string> targets = RecipientsPerCasting(inputs, targetable, expected,
                    new[] { first, second });
                if (targets.Count < 2) { reject(sourceId + "|fewer-than-two-fresh-recipients"); continue; }
                var castings = new List<PlannedCasting>();
                for (int index = 0; index < 2; index++)
                {
                    ProviderPlanningOption option = index == 0 ? first : second;
                    castings.Add(new PlannedCasting(CastingIds[index], RoutineId, index, sourceId,
                        option.Provider.Key.Ability, option.Provider.Key.CasterUnitId,
                        option.Provider.Key.SpellbookGuid, CastingTargetMode.DirectTarget,
                        targets[index], null, null, null, null,
                        ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null));
                }
                CastingQualificationStepForecast check = CastingQualificationForecast.Project(
                    "check", CastingQualificationForecast.BuildDocument(campaignId, castings),
                    inputs, inputs.LiveEffects);
                if (check.Refusal != null || check.CastingIds.Count != castings.Count)
                {
                    reject(sourceId + "|not-executable:" + (check.Refusal ?? "partial"));
                    continue;
                }
                var coverage = new List<string>();
                foreach (ProviderPlanningOption option in new[] { first, second })
                {
                    string kind = pools[option.Provider.ResourcePoolKey].Kind ==
                        ResourcePoolKind.PreparedSlots ? "prepared" : "spontaneous";
                    if (!coverage.Contains(kind)) coverage.Add(kind);
                    if (option.Provider.Key.Ability.MetamagicMask != 0 && !coverage.Contains("metamagic"))
                        coverage.Add("metamagic");
                }
                coverage.Add("mixed-caster");
                return new CastingQualificationSelection(null, sourceId, first.Provider.Key.Ability,
                    castings, considered, rejections, FiniteDirectMixed, coverage);
            }
            return new CastingQualificationSelection("no-eligible-qualification-recipe", null, null,
                null, considered, rejections, FiniteDirectMixed);
        }

        // Review RC4: recipients are chosen per casting, in recipe order.
        // Each is targetable, reachable by THAT casting's own option, without
        // the effect, not yet a recipient in this recipe, and never the
        // casting's own caster (the recipe observes a buff given to someone
        // else; a self-cast is the only exclusion). Another caster may be a
        // recipient; non-casters come first only to keep the observation
        // simple. Stops at the first casting without a legal recipient.
        internal static List<string> RecipientsPerCasting(CastingWorkspaceInputs inputs,
            ISet<string> targetable, EffectExpression expected, IList<ProviderPlanningOption> order)
        {
            var casters = new HashSet<string>(order.Select(option => option.Provider.Key.CasterUnitId),
                StringComparer.Ordinal);
            var used = new HashSet<string>(StringComparer.Ordinal);
            var recipients = new List<string>();
            foreach (ProviderPlanningOption option in order)
            {
                string caster = option.Provider.Key.CasterUnitId;
                string recipient = inputs.Snapshot.Units.Select(unit => unit.UnitId)
                    .Where(unit => targetable.Contains(unit) && !used.Contains(unit) &&
                        !string.Equals(unit, caster, StringComparison.Ordinal) &&
                        option.ReachableTargetIds.Contains(unit) &&
                        !EffectActive(inputs.LiveEffects, unit, expected))
                    .OrderBy(unit => casters.Contains(unit) ? 1 : 0)
                    .ThenBy(unit => unit, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (recipient == null) break;
                used.Add(recipient);
                recipients.Add(recipient);
            }
            return recipients;
        }

        // How many casts (up to the limit) this provider can reserve from
        // the current pools, by the same ledger the compiler budgets with.
        internal static int CastsAvailable(CastingWorkspaceInputs inputs, ProviderSnapshot provider,
            int limit)
        {
            var ledger = new ResourceLedger(inputs.Snapshot.ResourcePools);
            int casts = 0;
            ResourceReservation reservation;
            string reason;
            while (casts < limit && ledger.TryReserve(provider, out reservation, out reason)) casts++;
            return casts;
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

    // The exact projection each executing step of a qualification recipe
    // (zero-cost-mixed, finite-direct-mixed) will submit, forecast from the
    // selection-time state plus what the earlier steps grant (effects at
    // the caster's level and metamagic) and spend (the exact reserved
    // prepared tokens, spontaneous units; free pools never change):
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
            // Each casting grants the effect at ITS provider's caster level
            // and with ITS metamagic, as the real instance will carry them.
            Func<PlannedCasting, EffectGrant> grant = casting =>
            {
                ProviderSnapshot provider = inputs.Snapshot.Providers.FirstOrDefault(value =>
                    value.Key.CasterUnitId == casting.CasterUnitId &&
                    value.Key.Ability.Canonical == casting.Ability.Canonical);
                return new EffectGrant(casting.DirectTargetUnitId,
                    provider == null ? 0 : provider.EffectiveCasterLevel,
                    casting.Ability.MetamagicMask);
            };
            var steps = new List<CastingQualificationStepForecast>();
            // stop: every casting planned; only the first executes.
            CastingQualificationStepForecast stop = Project(Stop, document, inputs, inputs.LiveEffects);
            steps.Add(stop);
            var spent = new List<ResourceReservation>();
            if (stop.Projection != null) spent.Add(stop.Projection.Plan.Steps[0].Reservation);
            CastingWorkspaceInputs afterStop = WithSpent(inputs, spent);
            // complete: the first is active (skipped); the rest execute.
            CastingQualificationStepForecast complete = Project(Complete, document, afterStop,
                WithGranted(afterStop, expected, new[] { grant(firstCasting) }));
            steps.Add(complete);
            if (complete.Projection != null)
                spent.AddRange(complete.Projection.Plan.Steps.Select(step => step.Reservation));
            CastingWorkspaceInputs afterComplete = WithSpent(inputs, spent);
            // recast: everything active; the first, set to Always recast,
            // executes again.
            steps.Add(Project(Recast, WithPolicy(document, firstCasting.CastingId,
                    ExistingEffectPolicy.Overwrite), afterComplete,
                WithGranted(afterComplete, expected, selection.Castings.Select(grant))));
            return new ReadOnlyCollection<CastingQualificationStepForecast>(steps);
        }

        internal sealed class EffectGrant
        {
            internal EffectGrant(string unitId, int casterLevel, int metamagicMask)
            {
                UnitId = unitId;
                CasterLevel = casterLevel;
                MetamagicMask = metamagicMask;
            }

            internal string UnitId { get; private set; }
            internal int CasterLevel { get; private set; }
            internal int MetamagicMask { get; private set; }
        }

        // The inputs after the given reservations were spent: reserved
        // prepared tokens (with their links) unavailable, numeric pools
        // reduced by the reserved units; verified-free pools unchanged.
        internal static CastingWorkspaceInputs WithSpent(CastingWorkspaceInputs inputs,
            IEnumerable<ResourceReservation> spent)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);
            var units = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ResourceReservation reservation in spent ?? new ResourceReservation[0])
            {
                if (reservation == null || reservation.Unlimited) continue;
                foreach (string token in reservation.TokenIds) tokens.Add(token);
                if (reservation.TokenIds.Count != 0) continue;
                int prior;
                units.TryGetValue(reservation.PoolKey, out prior);
                units[reservation.PoolKey] = prior + reservation.Units;
            }
            if (tokens.Count == 0 && units.Count == 0) return inputs;
            List<ResourcePoolSnapshot> pools = inputs.Snapshot.ResourcePools.Select(pool =>
            {
                if (pool.Kind == ResourcePoolKind.PreparedSlots)
                {
                    List<ResourceTokenSnapshot> updated = pool.Tokens.Select(token =>
                        tokens.Contains(token.TokenId) && token.Available
                            ? new ResourceTokenSnapshot(token.TokenId, token.SlottedAbility,
                                token.SpellLevel, token.SlotKind, false, token.IsPrimary,
                                token.LinkedTokenIds)
                            : token).ToList();
                    return new ResourcePoolSnapshot(pool.PoolKey, pool.Kind, pool.Capacity,
                        updated.Count(token => token.Available), updated);
                }
                int used;
                if (!units.TryGetValue(pool.PoolKey, out used) || used == 0) return pool;
                return new ResourcePoolSnapshot(pool.PoolKey, pool.Kind, pool.Capacity,
                    Math.Max(0, pool.Remaining - used), pool.Tokens);
            }).ToList();
            return new CastingWorkspaceInputs(new PartyProviderSnapshot(inputs.Snapshot.Units,
                    inputs.Snapshot.Providers, pools), inputs.ProviderOptions,
                inputs.EffectsBySource, inputs.Enhancements, inputs.TargetingModifiers,
                inputs.LiveEffects);
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
        // the granting caster level and metamagic, with no expiry (a fresh
        // instance).
        internal static ActiveEffectSnapshot WithGranted(CastingWorkspaceInputs inputs,
            EffectExpression expected, IEnumerable<EffectGrant> grants)
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
            foreach (EffectGrant grant in grants)
            {
                List<ActiveEffectInstance> instances;
                if (!byUnit.TryGetValue(grant.UnitId, out instances)) continue;
                foreach (EffectLeafExpression leaf in leaves)
                    instances.Add(new ActiveEffectInstance(leaf.Kind, leaf.EffectId, null,
                        grant.CasterLevel > 0 ? (int?)grant.CasterLevel : null, grant.MetamagicMask));
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
