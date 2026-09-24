using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using KingmakerBuffPlanner.Compatibility;
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

        // Schema 4 named the casting mode the run is approved for (the
        // boundary refuses a run in any other mode); schema 5 also binds
        // the compatibility profile, its identity, the WORKING save and the
        // purpose (batch 3 review C5).
        public const int AllowanceSchemaVersion = 5;
        public const int MaximumSubmissionsCeiling = 24;

        public string RunId { get; private set; }
        public string SourceCommit { get; private set; }
        public string PackageSha256 { get; private set; }
        public string DllSha256 { get; private set; }
        public string AssemblyMvid { get; private set; }
        public string FixtureGameId { get; private set; }
        public string Recipe { get; private set; }
        public string ExecutionMode { get; private set; }
        public IReadOnlyList<string> ApprovedProjectionIds { get; private set; }
        public int MaximumNativeSubmissions { get; private set; }
        public string ApprovedBy { get; private set; }
        public string Authority { get; private set; }
        public string CompatibilityProfileId { get; private set; }
        public string CompatibilityIdentity { get; private set; }
        public string WorkingSaveSha256 { get; private set; }
        public string Purpose { get; private set; }

        private static readonly string[] Members =
        {
            "schemaVersion", "kind", "runId", "sourceCommit", "packageSha256", "dllSha256",
            "assemblyMvid", "fixtureGameId", "recipe", "executionMode", "approvedProjectionIds",
            "maximumNativeSubmissions", "approvedBy", "authority", "compatibilityProfileId", "compatibilityIdentity", "workingSaveSha256", "purpose"
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
                ExecutionMode = Text(root, "executionMode"),
                ApprovedProjectionIds = new ReadOnlyCollection<string>(
                    ids.Select(token => (string)token).ToList()),
                MaximumNativeSubmissions = maximum,
                ApprovedBy = Text(root, "approvedBy"),
                Authority = Text(root, "authority"),
                CompatibilityProfileId = Text(root, "compatibilityProfileId"),
                CompatibilityIdentity = Text(root, "compatibilityIdentity"),
                WorkingSaveSha256 = Text(root, "workingSaveSha256"),
                Purpose = Text(root, "purpose")
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
            refusal = AllowanceFixtureBinding.Refusal(allowance.CompatibilityProfileId,
                allowance.CompatibilityIdentity, allowance.WorkingSaveSha256, allowance.Purpose);
            if (refusal != null) return null;
            if (!CastingQualificationRecipe.IsKnown(allowance.Recipe))
            { refusal = "allowance-recipe-unknown"; return null; }
            if (allowance.ExecutionMode != "instant" && allowance.ExecutionMode != "animated")
            { refusal = "allowance-execution-mode"; return null; }
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

    // The fixture binding every casting allowance carries (batch 3, section
    // 3; review C5): the compatibility profile and its identity digest (the
    // exact external mod copies), the WORKING save's bytes and the run's
    // purpose. The launcher checks all of them against the profile and the
    // save pair it resolves; the host re-checks the profile and the save.
    public static class AllowanceFixtureBinding
    {
        public const int MaximumPurposeLength = 400;

        public static bool IsKnownProfile(string profileId)
        {
            return profileId == "native-only" || profileId == "call-of-the-wild" ||
                profileId == "human-reproduction" || profileId == "full-user" ||
                profileId == "advanced-gunslinger-0136";
        }

        // Null when the binding is well formed, else the refusal.
        public static string Refusal(string profileId, string identity, string workingSaveSha256, string purpose)
        {
            if (!IsKnownProfile(profileId)) return "allowance-profile";
            if (!SingleCastProbeAllowance.IsLowerHex64(identity)) return "allowance-compatibility-identity";
            if (!SingleCastProbeAllowance.IsLowerHex64(workingSaveSha256)) return "allowance-working-save";
            if (string.IsNullOrWhiteSpace(purpose) || purpose.Length > MaximumPurposeLength) return "allowance-purpose";
            return null;
        }

        // The host's own check: the request's profile and WORKING save are
        // the approved ones. Null when they are.
        public static string RequestMismatch(string allowedProfileId, string allowedWorkingSaveSha256,
            string requestProfileId, string requestWorkingSaveSha256)
        {
            if (!string.Equals(allowedProfileId, requestProfileId, StringComparison.Ordinal)) return "profile-mismatch";
            if (!string.Equals(allowedWorkingSaveSha256, requestWorkingSaveSha256, StringComparison.Ordinal))
                return "working-save-mismatch";
            return null;
        }
    }

    public sealed class CastingQualificationSelection
    {
        internal CastingQualificationSelection(string refusal, string sourceId, AbilityKey ability,
            IList<PlannedCasting> castings, int candidatesConsidered, IList<string> rejections,
            string recipe = CastingQualificationRecipe.ZeroCostMixed,
            IEnumerable<string> coverage = null, CastingQualificationEnhancement enhancement = null)
        {
            Enhancement = enhancement;
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
        // The enhanced recipe's enhancement (null for the other recipes).
        public CastingQualificationEnhancement Enhancement { get; private set; }
    }

    // The per-casting enhancement the enhanced recipe exercises: its id and
    // name, the caster resource that pays for it and the units one cast
    // spends, and what it must do: raise the named stat's modifier of the
    // named descriptor by the given amount over the plain casting's.
    public sealed class CastingQualificationEnhancement
    {
        internal CastingQualificationEnhancement(string enhancementId, string displayName,
            string casterUnitId, string usagePoolId, int unitsPerCast, string stat, string descriptor,
            int increase)
        {
            EnhancementId = enhancementId;
            DisplayName = displayName;
            CasterUnitId = casterUnitId;
            UsagePoolId = usagePoolId;
            UnitsPerCast = unitsPerCast;
            Stat = stat;
            Descriptor = descriptor;
            Increase = increase;
        }

        public string EnhancementId { get; private set; }
        public string DisplayName { get; private set; }
        public string CasterUnitId { get; private set; }
        public string UsagePoolId { get; private set; }
        public int UnitsPerCast { get; private set; }
        public string Stat { get; private set; }
        public string Descriptor { get; private set; }
        public int Increase { get; private set; }

        // "<stat>/<descriptor>/" - the modifier this enhancement raises.
        public string ModifierPrefix { get { return Stat + "/" + Descriptor + "/"; } }
    }

    // Deterministic authoring recipes for guarded qualification runs. The
    // recipe only chooses; every rule about what may execute is enforced by
    // the ordinary compiler, gate and Standard converter.
    public static class CastingQualificationRecipe
    {
        public const string ZeroCostMixed = "zero-cost-mixed";
        public const string FiniteDirectMixed = "finite-direct-mixed";
        public const string GroupMixed = "group-mixed";
        public const string EnhancedDirect = "enhanced-direct";
        public const string RoutineId = "long";

        public static bool IsKnown(string recipe)
        {
            return recipe == ZeroCostMixed || recipe == FiniteDirectMixed || recipe == GroupMixed ||
                recipe == EnhancedDirect;
        }

        // The recipes that cast their first casting alone and then add the
        // rest to the plan (group-mixed: prime, mixed; enhanced-direct:
        // plain, enhanced) instead of the stop / complete / repeat / recast
        // sequence.
        public static bool IsTwoPhase(string recipe)
        {
            return recipe == GroupMixed || recipe == EnhancedDirect;
        }

        // The group recipe's own steps (prime, mixed, repeat) replace the
        // stop / complete / repeat / recast sequence.
        public static bool IsGroupRecipe(string recipe)
        {
            return recipe == GroupMixed;
        }

        // The zero-cost recipe ends with the disable and recover steps (its
        // sources are free, so further approved castings need no further
        // resource); the finite recipe keeps its prepared slots for the four
        // steps.
        public static bool HasDisableStep(string recipe)
        {
            return recipe == ZeroCostMixed;
        }

        public static int ForecastSteps(string recipe)
        {
            if (IsTwoPhase(recipe)) return 2;
            return HasDisableStep(recipe) ? 5 : 3;
        }

        public static CastingQualificationSelection Select(string recipe,
            CastingWorkspaceInputs inputs, string campaignId)
        {
            if (recipe == FiniteDirectMixed) return SelectFiniteDirectMixed(inputs, campaignId);
            if (recipe == ZeroCostMixed) return SelectZeroCostMixed(inputs, campaignId);
            if (recipe == GroupMixed) return SelectGroupMixed(inputs, campaignId);
            if (recipe == EnhancedDirect) return SelectEnhancedDirect(inputs, campaignId);
            return new CastingQualificationSelection("unknown-recipe:" + recipe, null, null,
                null, 0, null, recipe);
        }
        public const int MaximumRecordedRejections = 160;
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
                else if (sourceId == null)
                    reject(provider.Key.Canonical + "|effect-shape:no-source");
                else if (!ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(
                        inputs.EffectsBySource[sourceId], ability))
                    reject(provider.Key.Canonical + "|effect-shape:" +
                        CastingCapabilityInventory.Structure(inputs.EffectsBySource[sourceId]));
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
                else if (sourceId == null)
                    reject(provider.Key.Canonical + "|effect-shape:no-source");
                else if (!ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(
                        inputs.EffectsBySource[sourceId], ability))
                    reject(provider.Key.Canonical + "|effect-shape:" +
                        CastingCapabilityInventory.Structure(inputs.EffectsBySource[sourceId]));
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

        // One provider option the group recipe considers: its source, the
        // expected effects and the exact effects ("Kind:id") it applies.
        private sealed class GroupSource
        {
            internal GroupSource(ProviderPlanningOption option, string sourceId, EffectExpression expected)
            {
                Option = option;
                SourceId = sourceId;
                Expected = expected;
                Leaves = new HashSet<string>(CastingQualificationForecast.Leaves(expected)
                    .Select(leaf => leaf.Kind + ":" + leaf.EffectId), StringComparer.Ordinal);
            }

            internal ProviderPlanningOption Option { get; private set; }
            internal ProviderSnapshot Provider { get { return Option.Provider; } }
            internal string Caster { get { return Option.Provider.Key.CasterUnitId; } }
            internal string SourceId { get; private set; }
            internal EffectExpression Expected { get; private set; }
            internal HashSet<string> Leaves { get; private set; }
        }

        // Every provider, in order, reserves one cast from the current pools
        // by the same ledger the compiler budgets with.
        internal static bool CastableTogether(CastingWorkspaceInputs inputs,
            IEnumerable<ProviderSnapshot> providers)
        {
            var ledger = new ResourceLedger(inputs.Snapshot.ResourcePools);
            foreach (ProviderSnapshot provider in providers)
            {
                ResourceReservation reservation;
                string reason;
                if (!ledger.TryReserve(provider, out reservation, out reason)) return false;
            }
            return true;
        }

        private static bool IsAreaShaped(EffectExpression expected)
        {
            string shape = CastingCapabilityInventory.Shape(expected);
            return shape.Contains("allied-area") || shape.Contains("party");
        }

        // Group coverage (mission batch 3, section 8; the owner's mixed-
        // coverage case, 2026-09-24): a caster-centred group source G and a
        // direct source D that applies exactly the same effects, so one
        // recipient X already holds an adequate instance (D, cast alone
        // first) while G's other predicted recipients lack it; and, where
        // one exists, a target-anchored group source A with another effect,
        // anchored on a member other than its caster.
        //   qual-cast-1 = D on X (direct; the prime step casts it alone),
        //   qual-cast-2 = G around its caster (the mixed step: one invocation
        //                 and one unit of cost; X pre-covered, the others
        //                 newly covered),
        //   qual-cast-3 = A on its anchor (the mixed step), when found.
        // D's caster level and duration are at least G's, so X's instance is
        // provably as strong; the sources can pay every cast together; and
        // the forecast must show X pre-covered in G's step.
        public static CastingQualificationSelection SelectGroupMixed(
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
            var pools = inputs.Snapshot.ResourcePools.ToDictionary(
                pool => pool.PoolKey, pool => pool, StringComparer.Ordinal);
            var sources = new List<GroupSource>();
            foreach (ProviderPlanningOption option in inputs.ProviderOptions
                .Where(value => value != null && value.Provider != null)
                .OrderBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal))
            {
                ProviderSnapshot provider = option.Provider;
                AbilityKey ability = provider.Key.Ability;
                if (ability.SourceKind != SourceKind.Spellbook || ability.MetamagicMask != 0 ||
                    !string.IsNullOrEmpty(ability.SpecialSourceId))
                    continue;
                string sourceId = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, ability);
                if (option.ExecutionStrategy != CastExecutionStrategy.DirectRuleCast)
                    reject(provider.Key.Canonical + "|strategy:" + option.ExecutionStrategy);
                else if (sourceId == null)
                    reject(provider.Key.Canonical + "|no-source");
                else if (CastsAvailable(inputs, provider, 1) < 1)
                    reject(provider.Key.Canonical + "|no-cast");
                else
                    sources.Add(new GroupSource(option, sourceId, inputs.EffectsBySource[sourceId]));
            }
            var casters = new HashSet<string>(sources.Select(value => value.Caster), StringComparer.Ordinal);
            int considered = 0;
            foreach (GroupSource group in sources.Where(value => IsAreaShaped(value.Expected) &&
                value.Option.LegalAnchorIds.Contains(value.Caster)))
            {
                considered++;
                string groupKey = group.Provider.Key.Canonical;
                List<string> covered = group.Option.CoveredTargetIdsForAnchor(group.Caster)
                    .Where(unit => targetable.Contains(unit)).ToList();
                if (covered.Count < 2) { reject(groupKey + "|fewer-than-two-recipients"); continue; }
                if (covered.Any(unit => EffectActive(inputs.LiveEffects, unit, group.Expected)))
                {
                    reject(groupKey + "|a-recipient-already-covered");
                    continue;
                }
                List<GroupSource> directs = sources.Where(value => value != group &&
                        value.Leaves.SetEquals(group.Leaves) &&
                        ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(value.Expected,
                            value.Provider.Key.Ability))
                    .ToList();
                if (directs.Count == 0) { reject(groupKey + "|no-direct-source-with-the-same-effects"); continue; }
                foreach (GroupSource direct in directs)
                {
                    string pair = direct.Provider.Key.Canonical + "+" + groupKey;
                    if (direct.Provider.EffectiveCasterLevel < group.Provider.EffectiveCasterLevel)
                    { reject(pair + "|direct-caster-level-lower"); continue; }
                    if (direct.Provider.ExpectedDurationRounds < group.Provider.ExpectedDurationRounds)
                    { reject(pair + "|direct-duration-shorter"); continue; }
                    string recipient = covered.Where(unit =>
                            !string.Equals(unit, direct.Caster, StringComparison.Ordinal) &&
                            direct.Option.ReachableTargetIds.Contains(unit))
                        .OrderBy(unit => string.Equals(unit, group.Caster, StringComparison.Ordinal) ? 1 : 0)
                        .ThenBy(unit => casters.Contains(unit) ? 1 : 0)
                        .ThenBy(unit => unit, StringComparer.Ordinal)
                        .FirstOrDefault();
                    if (recipient == null) { reject(pair + "|no-direct-recipient"); continue; }
                    if (!CastableTogether(inputs, new[] { direct.Provider, group.Provider }))
                    { reject(pair + "|not-castable-together"); continue; }
                    // Where one exists: a target-anchored group source with
                    // another effect, on a member other than its caster,
                    // whose covered members all lack it; the longest-lasting
                    // first.
                    GroupSource anchored = null;
                    string anchor = null;
                    foreach (GroupSource candidate in sources.Where(value => value != group && value != direct &&
                            IsAreaShaped(value.Expected) && !value.Leaves.Overlaps(group.Leaves))
                        .OrderByDescending(value => value.Provider.ExpectedDurationRounds)
                        .ThenBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal))
                    {
                        string origin = candidate.Option.LegalAnchorIds.Where(unit =>
                                !string.Equals(unit, candidate.Caster, StringComparison.Ordinal) &&
                                targetable.Contains(unit) &&
                                candidate.Option.CoveredTargetIdsForAnchor(unit).Count >= 2 &&
                                candidate.Option.CoveredTargetIdsForAnchor(unit).All(member =>
                                    !EffectActive(inputs.LiveEffects, member, candidate.Expected)))
                            .OrderBy(unit => unit, StringComparer.Ordinal)
                            .FirstOrDefault();
                        if (origin == null || !CastableTogether(inputs,
                                new[] { direct.Provider, group.Provider, candidate.Provider }))
                            continue;
                        anchored = candidate;
                        anchor = origin;
                        break;
                    }
                    var castings = new List<PlannedCasting>
                    {
                        new PlannedCasting(CastingIds[0], RoutineId, 0, direct.SourceId,
                            direct.Provider.Key.Ability, direct.Caster, direct.Provider.Key.SpellbookGuid,
                            CastingTargetMode.DirectTarget, recipient, null, null, null, null,
                            ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null),
                        new PlannedCasting(CastingIds[1], RoutineId, 1, group.SourceId,
                            group.Provider.Key.Ability, group.Caster, group.Provider.Key.SpellbookGuid,
                            CastingTargetMode.CasterCenteredOrigin, null, CastingOrigin.CasterCentered(),
                            new string[0], null, null, ExistingEffectPolicy.SkipAlreadyActive, null,
                            CastingAuthoringState.Ready, null)
                    };
                    if (anchored != null)
                        castings.Add(new PlannedCasting(CastingIds[2], RoutineId, 2, anchored.SourceId,
                            anchored.Provider.Key.Ability, anchored.Caster, anchored.Provider.Key.SpellbookGuid,
                            CastingTargetMode.AnchoredOrigin, null, CastingOrigin.Anchored(anchor),
                            new string[0], null, null, ExistingEffectPolicy.SkipAlreadyActive, null,
                            CastingAuthoringState.Ready, null));
                    var coverage = new List<string> { "caster-centred", "mixed-coverage" };
                    if (anchored != null) coverage.Add("target-anchored");
                    foreach (GroupSource used in new[] { direct, group, anchored }.Where(value => value != null))
                    {
                        ResourcePoolSnapshot pool;
                        string kind = !pools.TryGetValue(used.Provider.ResourcePoolKey, out pool) ? "unknown"
                            : pool.Kind == ResourcePoolKind.PreparedSlots ? "prepared"
                            : pool.Kind == ResourcePoolKind.SpontaneousLevel ? "spontaneous"
                            : pool.Kind == ResourcePoolKind.Unlimited ? "free" : pool.Kind.ToString();
                        if (!coverage.Contains(kind)) coverage.Add(kind);
                    }
                    var selection = new CastingQualificationSelection(null, group.SourceId,
                        group.Provider.Key.Ability, castings, considered, rejections, GroupMixed, coverage);
                    string check = GroupForecastRefusal(CastingQualificationForecast.Forecast(
                        selection, inputs, campaignId), castings, recipient);
                    if (check != null) { reject(pair + "|" + check); continue; }
                    return selection;
                }
            }
            return new CastingQualificationSelection("no-eligible-qualification-recipe", null, null,
                null, considered, rejections, GroupMixed);
        }

        // enhanced-direct (mission batch 3, section 8: a supported non-rod
        // per-casting enhancement through the real UI, service and execution
        // chain). The one such enhancement in this version is Brown-Fur
        // Powerful Change (Share Transmutation, a targeting modifier, stays
        // refused; rods are metamagic): one Arcane Reservoir point raises the
        // enhancement bonus a qualifying transmutation gives to the chosen
        // score by 2, the provider's documented increase below level 20. One
        // plain direct buff the enhancement applies to, cast twice by its
        // caster from the same source on two recipients without it:
        //   qual-cast-1 = plain (the plain step casts it alone),
        //   qual-cast-2 = with the enhancement, chosen on the casting through
        //                 the workspace's own option for it (the enhanced
        //                 step; qual-cast-1 is skipped as active).
        public const int PowerfulChangeIncrease = 2;

        public static CastingQualificationSelection SelectEnhancedDirect(
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
            int considered = 0;
            foreach (CastEnhancementSnapshot enhancement in (inputs.Enhancements ??
                    (IEnumerable<CastEnhancementSnapshot>)new CastEnhancementSnapshot[0])
                .Where(value => value != null)
                .OrderBy(value => value.EnhancementId, StringComparer.Ordinal))
            {
                BrownFurPowerfulChangeToggleContract contract =
                    BrownFurPowerfulChangeProfile.Find(enhancement.SourceBlueprintGuid);
                if (contract == null || enhancement.Category != CastEnhancementCategory.ClassFeature ||
                    enhancement.AffectsTargeting || enhancement.MetamagicMask != 0 ||
                    enhancement.EnhancementId != BrownFurPowerfulChangeProfile.EnhancementId(
                        enhancement.CasterUnitId, contract.ActivatableGuid) ||
                    enhancement.UsagePoolId != BrownFurPowerfulChangeProfile.UsagePoolId(enhancement.CasterUnitId))
                {
                    reject(enhancement.EnhancementId + "|not-a-supported-non-rod-enhancement");
                    continue;
                }
                considered++;
                if (enhancement.RemainingUses == null ||
                    enhancement.RemainingUses.Value < enhancement.UsageUnitsPerCast)
                {
                    reject(enhancement.EnhancementId + "|uses:" + (enhancement.RemainingUses == null ? "unread"
                        : enhancement.RemainingUses.Value.ToString(CultureInfo.InvariantCulture)));
                    continue;
                }
                bool applicable = false;
                foreach (ProviderPlanningOption option in inputs.ProviderOptions
                    .Where(value => value != null && value.Provider != null &&
                        value.Provider.Key.CasterUnitId == enhancement.CasterUnitId)
                    .OrderBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal))
                {
                    ProviderSnapshot provider = option.Provider;
                    AbilityKey ability = provider.Key.Ability;
                    if (ability.SourceKind != SourceKind.Spellbook || ability.MetamagicMask != 0 ||
                        !string.IsNullOrEmpty(ability.SpecialSourceId) || !enhancement.IsApplicable(provider))
                        continue;
                    applicable = true;
                    string key = enhancement.EnhancementId + "+" + provider.Key.Canonical;
                    string sourceId = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, ability);
                    ResourcePoolSnapshot pool;
                    if (option.ExecutionStrategy != CastExecutionStrategy.DirectRuleCast)
                    { reject(key + "|strategy:" + option.ExecutionStrategy); continue; }
                    if (!pools.TryGetValue(provider.ResourcePoolKey, out pool))
                    { reject(key + "|pool-unread"); continue; }
                    if (sourceId == null) { reject(key + "|no-source"); continue; }
                    EffectExpression expected = inputs.EffectsBySource[sourceId];
                    if (!ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(expected, ability))
                    {
                        reject(key + "|effect-shape:" + CastingCapabilityInventory.Structure(expected));
                        continue;
                    }
                    if (CastsAvailable(inputs, provider, 2) < 2) { reject(key + "|fewer-than-two-casts"); continue; }
                    List<string> targets = RecipientsPerCasting(inputs, targetable, expected,
                        new[] { option, option });
                    if (targets.Count < 2) { reject(key + "|fewer-than-two-fresh-recipients"); continue; }
                    var castings = new List<PlannedCasting>
                    {
                        new PlannedCasting(CastingIds[0], RoutineId, 0, sourceId, ability,
                            provider.Key.CasterUnitId, provider.Key.SpellbookGuid,
                            CastingTargetMode.DirectTarget, targets[0], null, null, null, null,
                            ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null),
                        new PlannedCasting(CastingIds[1], RoutineId, 1, sourceId, ability,
                            provider.Key.CasterUnitId, provider.Key.SpellbookGuid,
                            CastingTargetMode.DirectTarget, targets[1], null, null, null,
                            new[] { new AuthoredEnhancementSelection(enhancement.EnhancementId, true, null) },
                            ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null)
                    };
                    var chosen = new CastingQualificationEnhancement(enhancement.EnhancementId,
                        enhancement.DisplayName, enhancement.CasterUnitId, enhancement.UsagePoolId,
                        enhancement.UsageUnitsPerCast, contract.Score.ToString(), "Enhancement",
                        PowerfulChangeIncrease);
                    var coverage = new List<string>
                    {
                        pool.Kind == ResourcePoolKind.PreparedSlots ? "prepared"
                            : pool.Kind == ResourcePoolKind.SpontaneousLevel ? "spontaneous"
                            : pool.Kind == ResourcePoolKind.Unlimited ? "free" : pool.Kind.ToString(),
                        "class-feature-enhancement", "powerful-change:" + contract.Score
                    };
                    var selection = new CastingQualificationSelection(null, sourceId, ability, castings,
                        considered, rejections, EnhancedDirect, coverage, chosen);
                    IReadOnlyList<CastingQualificationStepForecast> forecast =
                        CastingQualificationForecast.Forecast(selection, inputs, campaignId);
                    string check = EnhancedForecastRefusal(forecast, castings, enhancement.EnhancementId);
                    if (check != null) { reject(key + "|" + check); continue; }
                    // The route the enhanced cast takes, reported with the
                    // selection (and checked against what ran).
                    coverage.Add("route:" + forecast[1].Projection.Plan.Steps[0].ExecutionStrategy);
                    return new CastingQualificationSelection(null, sourceId, ability, castings,
                        considered, rejections, EnhancedDirect, coverage, chosen);
                }
                if (!applicable) reject(enhancement.EnhancementId + "|no-applicable-plain-spell");
            }
            return new CastingQualificationSelection("no-eligible-qualification-recipe", null, null,
                null, considered, rejections, EnhancedDirect);
        }

        // The enhanced recipe's forecast must be exactly: plain - the plain
        // casting alone, with no enhancement; enhanced - the enhanced casting
        // alone (the plain one skipped as active), carrying exactly the
        // enhancement. Null when it is.
        internal static string EnhancedForecastRefusal(IReadOnlyList<CastingQualificationStepForecast> forecast,
            IList<PlannedCasting> castings, string enhancementId)
        {
            if (forecast == null || forecast.Count != 2) return "forecast-steps";
            foreach (CastingQualificationStepForecast step in forecast)
                if (step.Refusal != null || step.Projection == null)
                    return "not-executable:" + step.Name + ":" + (step.Refusal ?? "none");
            if (!forecast[0].CastingIds.SequenceEqual(new[] { castings[0].CastingId }) ||
                forecast[0].Projection.Plan.Steps[0].EnhancementIds.Count != 0)
                return "plain-castings:" + string.Join(",", forecast[0].CastingIds.ToArray());
            if (!forecast[1].CastingIds.SequenceEqual(new[] { castings[1].CastingId }))
                return "enhanced-castings:" + string.Join(",", forecast[1].CastingIds.ToArray());
            CastStep enhanced = forecast[1].Projection.Plan.Steps[0];
            if (!enhanced.EnhancementIds.SequenceEqual(new[] { enhancementId }))
                return "enhancement-not-forecast:" + string.Join(",", enhanced.EnhancementIds.ToArray());
            // Only a route the enhancement's provider takes part in: its own
            // transaction or the game's own command (a plain rule cast never
            // enrols it, so the enhancement would not be applied).
            if (enhanced.ExecutionStrategy != CastExecutionStrategy.ProviderDirectRuleCast &&
                enhanced.ExecutionStrategy != CastExecutionStrategy.NativeCommandRequired)
                return "enhancement-route-unenrolled:" + enhanced.ExecutionStrategy;
            return null;
        }

        // The group recipe's forecast must be exactly: prime - the direct
        // casting alone; mixed - the group casting (and the anchored one),
        // the direct casting skipped, and the group casting's step naming
        // the direct casting's recipient as pre-covered. Null when it is.
        internal static string GroupForecastRefusal(IReadOnlyList<CastingQualificationStepForecast> forecast,
            IList<PlannedCasting> castings, string preCovered)
        {
            if (forecast == null || forecast.Count != 2) return "forecast-steps";
            foreach (CastingQualificationStepForecast step in forecast)
                if (step.Refusal != null || step.Projection == null)
                    return "not-executable:" + step.Name + ":" + (step.Refusal ?? "none");
            if (!forecast[0].CastingIds.SequenceEqual(new[] { castings[0].CastingId }))
                return "prime-castings:" + string.Join(",", forecast[0].CastingIds.ToArray());
            if (!forecast[1].CastingIds.SequenceEqual(castings.Skip(1).Select(value => value.CastingId)))
                return "mixed-castings:" + string.Join(",", forecast[1].CastingIds.ToArray());
            CastStep group = forecast[1].Projection.Plan.Steps[0];
            if (!group.MassCast || group.ExpectedRecipientUnitIds.Count < 2 ||
                !group.PreCoveredRecipientUnitIds.SequenceEqual(new[] { preCovered }))
                return "mixed-coverage-not-forecast:" +
                    string.Join(",", group.PreCoveredRecipientUnitIds.ToArray());
            return null;
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
    //              so only it executes (after a close and reopen),
    //   disable  - (zero-cost recipe) the same plan again; the planner is
    //              disabled while that cast is in progress (animated) or
    //              before the run's first step (instant), so nothing lands,
    //   recover  - (zero-cost recipe) after the planner is enabled again,
    //              the same plan as a NEW run: nothing resumed the stopped
    //              run, and a fresh press is accepted and completes.
    // The repeat step (everything active) submits nothing. A later step
    // whose real projection differs from its forecast is refused.
    public static class CastingQualificationForecast
    {
        public const string Stop = "stop";
        public const string Complete = "complete";
        public const string Recast = "recast";
        public const string Disable = "disable";
        public const string Recover = "recover";
        public const string Prime = "prime";
        public const string Mixed = "mixed";
        public const string Plain = "plain";
        public const string Enhanced = "enhanced";

        public static IReadOnlyList<CastingQualificationStepForecast> Forecast(
            CastingQualificationSelection selection, CastingWorkspaceInputs inputs,
            string campaignId)
        {
            if (selection == null || !selection.Selected)
                throw new ArgumentException("A selected recipe is required.", "selection");
            if (CastingQualificationRecipe.IsGroupRecipe(selection.Recipe))
                return ForecastTwoPhase(selection, inputs, campaignId, Prime, Mixed);
            if (selection.Recipe == CastingQualificationRecipe.EnhancedDirect)
                return ForecastTwoPhase(selection, inputs, campaignId, Plain, Enhanced);
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
            CastingPlanDocument recastDocument = WithPolicy(document, firstCasting.CastingId,
                ExistingEffectPolicy.Overwrite);
            CastingQualificationStepForecast recast = Project(Recast, recastDocument, afterComplete,
                WithGranted(afterComplete, expected, selection.Castings.Select(grant)));
            steps.Add(recast);
            if (CastingQualificationRecipe.HasDisableStep(selection.Recipe))
            {
                // disable: everything active again; the same Always recast
                // plan is submitted and the planner is disabled during it.
                if (recast.Projection != null)
                    spent.AddRange(recast.Projection.Plan.Steps.Select(step => step.Reservation));
                CastingWorkspaceInputs afterRecast = WithSpent(inputs, spent);
                steps.Add(Project(Disable, recastDocument, afterRecast,
                    WithGranted(afterRecast, expected, selection.Castings.Select(grant))));
                // recover: nothing landed and nothing was spent in the
                // disabled run (free sources), so the same state again.
                steps.Add(Project(Recover, recastDocument, afterRecast,
                    WithGranted(afterRecast, expected, selection.Castings.Select(grant))));
            }
            return new ReadOnlyCollection<CastingQualificationStepForecast>(steps);
        }

        // The two-phase recipes. group-mixed: prime - the direct casting
        // alone (its recipient covered); mixed - every casting, the direct
        // one skipped (its recipient holds the effect at the direct caster's
        // level), the group casting covering the others with that recipient
        // pre-covered, and the anchored casting (if any) covering its
        // anchor's area. enhanced-direct: plain - the plain casting alone;
        // enhanced - both, the plain one skipped, the enhanced one cast with
        // its enhancement.
        private static IReadOnlyList<CastingQualificationStepForecast> ForecastTwoPhase(
            CastingQualificationSelection selection, CastingWorkspaceInputs inputs, string campaignId,
            string firstName, string secondName)
        {
            PlannedCasting direct = selection.Castings[0];
            var steps = new List<CastingQualificationStepForecast>();
            CastingQualificationStepForecast prime = Project(firstName,
                BuildDocument(campaignId, new[] { direct }), inputs, inputs.LiveEffects);
            steps.Add(prime);
            var spent = new List<ResourceReservation>();
            if (prime.Projection != null) spent.Add(prime.Projection.Plan.Steps[0].Reservation);
            CastingWorkspaceInputs afterPrime = WithSpent(inputs, spent);
            ProviderSnapshot provider = inputs.Snapshot.Providers.FirstOrDefault(value =>
                value.Key.CasterUnitId == direct.CasterUnitId &&
                value.Key.Ability.Canonical == direct.Ability.Canonical);
            steps.Add(Project(secondName, BuildDocument(campaignId, selection.Castings), afterPrime,
                WithGranted(afterPrime, inputs.EffectsBySource[direct.SourceId], new[]
                {
                    new EffectGrant(direct.DirectTargetUnitId,
                        provider == null ? 0 : provider.EffectiveCasterLevel, direct.Ability.MetamagicMask)
                })));
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
            // The run executes only in the casting mode the allowance names.
            string mode = settings == null ? null : settings.Mode;
            if (!string.Equals(mode, _allowance.ExecutionMode, StringComparison.Ordinal))
            {
                string modeRefusal = "qualification-mode-not-approved:" + (mode ?? "none");
                Submissions.Add("refused:" + modeRefusal);
                return new CastingDispatchOutcome(false, modeRefusal, ids);
            }
            CastingDispatchOutcome outcome = _host.Start(plan, decision, scopeRoutineId,
                projection, settings);
            Submissions.Add((outcome.Submitted ? "submitted:" : "refused:") + outcome.Reason +
                ";projection=" + projection.ProjectionId + ";mode=" + mode);
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
