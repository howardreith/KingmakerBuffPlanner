using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Planning;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Execution
{
    // The exact identity of a casting-first routine plan: every persisted
    // casting's authored intent (id, order, source, ability, caster and
    // provider, target mode and direct target, targeting modifiers,
    // enhancements, existing-effect policy), encoded with length prefixes
    // (native token ids contain "|" and ",", so no delimiter is trusted)
    // and hashed. Deterministic for the same authored document; it never
    // covers live party state, so an allowance bound to it approves the
    // plan, not one snapshot.
    public static class CastingFirstPlanDigest
    {
        public static string Canonical(string routineId, ExplicitCastingPlan plan)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            var builder = new StringBuilder();
            Field(builder, "routine", routineId);
            Field(builder, "castings", plan.Castings.Count.ToString());
            foreach (ResolvedCasting casting in plan.Castings)
            {
                Field(builder, "id", casting.CastingId);
                Field(builder, "order", casting.Order.ToString());
                Field(builder, "source", casting.SourceId);
                Field(builder, "ability", casting.Ability == null ? string.Empty : casting.Ability.Canonical);
                Field(builder, "caster", casting.CasterUnitId);
                Field(builder, "provider", casting.Provider == null ? string.Empty : casting.Provider.Canonical);
                Field(builder, "mode", casting.TargetMode.ToString());
                Field(builder, "target", casting.DirectTargetUnitId);
                List(builder, "modifiers", (casting.TargetingModifiers ?? new TargetingModifierSelection[0])
                    .Select(value => (value == null ? string.Empty : value.ModifierId) + "|" +
                        (value != null && value.Enabled ? "1" : "0") + "|" +
                        (value == null || value.ExactSourceRef == null ? string.Empty : value.ExactSourceRef)));
                List(builder, "enhancements", (casting.Enhancements ?? new AuthoredEnhancementSelection[0])
                    .Select(value => (value == null ? string.Empty : value.EnhancementId) + "|" +
                        (value != null && value.Required ? "1" : "0") + "|" +
                        (value == null || value.ExactSourceRef == null ? string.Empty : value.ExactSourceRef)));
                List(builder, "omitted", casting.OmittedEnhancementIds);
                Field(builder, "policy", casting.ExistingEffectPolicy.ToString());
            }
            return builder.ToString();
        }

        public static string Of(string routineId, ExplicitCastingPlan plan)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Canonical(routineId, plan)));
                return string.Concat(hash.Select(value => value.ToString("x2")).ToArray());
            }
        }

        private static void Field(StringBuilder builder, string name, string value)
        {
            value = value ?? string.Empty;
            builder.Append(name).Append('=').Append(value.Length).Append(':').Append(value).Append(';');
        }

        private static void List(StringBuilder builder, string name, IEnumerable<string> values)
        {
            List<string> items = (values ?? new string[0]).ToList();
            Field(builder, name + "#", items.Count.ToString());
            foreach (string item in items) Field(builder, name, item);
        }
    }

    // A single-use exception to an automated session's native-casting lock
    // for the casting-first route (the physical cold-moon scenario): exactly
    // one execution of exactly the approved routine plan (digest, mode, at
    // most the approved number of executable castings). Every other
    // casting-first execution in the session stays refused. Mirrors
    // ClassicCastGrant.
    public sealed class CastingFirstCastGrant
    {
        public CastingFirstCastGrant(string runId, string routineId, string planDigest,
            string executionMode, int maximumSubmissions)
        {
            RunId = runId ?? string.Empty;
            RoutineId = routineId ?? string.Empty;
            PlanDigest = planDigest ?? string.Empty;
            ExecutionMode = executionMode ?? string.Empty;
            MaximumSubmissions = maximumSubmissions;
        }

        public string RunId { get; private set; }
        public string RoutineId { get; private set; }
        public string PlanDigest { get; private set; }
        public string ExecutionMode { get; private set; }
        public int MaximumSubmissions { get; private set; }
        public int Attempts { get; private set; }
        public bool Consumed { get; private set; }
        // Set when the scenario that armed the grant ends, used or not: no
        // later casting-first execution in the session can use it.
        public bool Disarmed { get; private set; }
        public string LastRefusal { get; private set; }

        public void Disarm()
        {
            Disarmed = true;
        }

        public bool TryConsume(string routineId, string planDigest, string executionMode,
            int plannedSteps, out string refusal)
        {
            Attempts++;
            refusal = Disarmed ? "cf-grant-disarmed"
                : Consumed ? "cf-grant-consumed"
                : !string.Equals(routineId, RoutineId, StringComparison.Ordinal)
                    ? "cf-grant-routine:" + (routineId ?? "none")
                : !string.Equals(executionMode, ExecutionMode, StringComparison.Ordinal)
                    ? "cf-grant-mode:" + (executionMode ?? "none")
                : !string.Equals(planDigest, PlanDigest, StringComparison.Ordinal)
                    ? "cf-grant-plan-differs"
                : plannedSteps < 1 || plannedSteps > MaximumSubmissions
                    ? "cf-grant-cap:" + plannedSteps + ">" + MaximumSubmissions
                : null;
            LastRefusal = refusal;
            if (refusal != null) return false;
            Consumed = true;
            return true;
        }

        public string Describe()
        {
            return "run=" + RunId + ";routine=" + RoutineId + ";mode=" + ExecutionMode + ";max=" +
                MaximumSubmissions + ";attempts=" + Attempts + ";consumed=" + Consumed +
                ";disarmed=" + Disarmed + ";lastRefusal=" + (LastRefusal ?? "none");
        }
    }

    // The run-bound allowance for one physical cold-moon casting-first run
    // (kind kbp-cf-physical-cast, schema 1): the exact frozen build, the
    // fixture campaign, the execution mode, the routine, the approved plan
    // digest recorded by the selection run, a 1..24 submission budget, the
    // compatibility profile with its identity, the WORKING save and the
    // purpose.
    public sealed class CastingFirstAllowance
    {
        private CastingFirstAllowance() { }

        public const int AllowanceSchemaVersion = 1;
        public const string AllowanceKind = "kbp-cf-physical-cast";

        public string RunId { get; private set; }
        public string SourceCommit { get; private set; }
        public string PackageSha256 { get; private set; }
        public string DllSha256 { get; private set; }
        public string AssemblyMvid { get; private set; }
        public string FixtureGameId { get; private set; }
        public string ExecutionMode { get; private set; }
        public string RoutineId { get; private set; }
        public string ApprovedPlanDigest { get; private set; }
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
            "assemblyMvid", "fixtureGameId", "executionMode", "routineId", "approvedPlanDigest",
            "maximumNativeSubmissions", "approvedBy", "authority", "compatibilityProfileId",
            "compatibilityIdentity", "workingSaveSha256", "purpose"
        };

        public static CastingFirstAllowance Parse(string json, string expectedRunId, out string refusal)
        {
            refusal = null;
            JObject root;
            try { root = JObject.Parse(json ?? string.Empty); }
            catch (Exception) { refusal = "allowance-unreadable"; return null; }
            string unknown = root.Properties().Select(property => property.Name)
                .FirstOrDefault(name => !Members.Contains(name));
            if (unknown != null) { refusal = "allowance-unknown-member:" + unknown; return null; }
            string missing = Members.FirstOrDefault(name => root[name] == null);
            if (missing != null) { refusal = "allowance-missing-member:" + missing; return null; }
            if (root["schemaVersion"].Type != JTokenType.Integer ||
                (int)root["schemaVersion"] != AllowanceSchemaVersion)
            { refusal = "allowance-schema"; return null; }
            if (Text(root, "kind") != AllowanceKind) { refusal = "allowance-kind"; return null; }
            if (root["maximumNativeSubmissions"].Type != JTokenType.Integer)
            { refusal = "allowance-submissions"; return null; }
            int maximum = (int)root["maximumNativeSubmissions"];
            if (maximum < 1 || maximum > CastingQualificationAllowance.MaximumSubmissionsCeiling)
            { refusal = "allowance-submissions-range"; return null; }
            var allowance = new CastingFirstAllowance
            {
                RunId = Text(root, "runId"),
                SourceCommit = Text(root, "sourceCommit"),
                PackageSha256 = Text(root, "packageSha256"),
                DllSha256 = Text(root, "dllSha256"),
                AssemblyMvid = Text(root, "assemblyMvid"),
                FixtureGameId = Text(root, "fixtureGameId"),
                ExecutionMode = Text(root, "executionMode"),
                RoutineId = Text(root, "routineId"),
                ApprovedPlanDigest = Text(root, "approvedPlanDigest"),
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
            if (allowance.ExecutionMode != "instant" && allowance.ExecutionMode != "animated")
            { refusal = "allowance-execution-mode"; return null; }
            if (allowance.RoutineId != "long" && allowance.RoutineId != "important" &&
                allowance.RoutineId != "short")
            { refusal = "allowance-routine"; return null; }
            if (!SingleCastProbeAllowance.IsLowerHex64(allowance.ApprovedPlanDigest))
            { refusal = "allowance-plan-digest"; return null; }
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
}
