using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Execution
{
    // The exact identity of a classic routine plan: every step's provider,
    // source, anchor, targets, expected recipients, mass flag, reservation
    // (pool, units, tokens, unlimited), enhancements and strategy, encoded
    // with length prefixes (native token ids contain "|" and ",", so no
    // delimiter is trusted) and hashed.
    public static class ClassicPlanDigest
    {
        public static string Canonical(CastPlan plan)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            var builder = new StringBuilder();
            Field(builder, "steps", plan.Steps.Count.ToString());
            for (int index = 0; index < plan.Steps.Count; index++)
            {
                CastStep step = plan.Steps[index];
                Field(builder, "index", index.ToString());
                Field(builder, "provider", step.Provider == null ? string.Empty : step.Provider.Canonical);
                Field(builder, "source", step.SourceId);
                Field(builder, "anchor", step.AnchorUnitId);
                List(builder, "targets", step.TargetUnitIds);
                List(builder, "recipients", step.ExpectedRecipientUnitIds);
                Field(builder, "mass", step.MassCast ? "1" : "0");
                ResourceReservation reservation = step.Reservation;
                Field(builder, "pool", reservation == null ? "none" : reservation.PoolKey);
                Field(builder, "units", reservation == null ? "0" : reservation.Units.ToString());
                Field(builder, "unlimited", reservation != null && reservation.Unlimited ? "1" : "0");
                List(builder, "tokens", reservation == null ? null : reservation.TokenIds);
                List(builder, "enhancements", step.EnhancementIds);
                Field(builder, "strategy", step.ExecutionStrategy.ToString());
            }
            return builder.ToString();
        }

        public static string Of(CastPlan plan)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Canonical(plan)));
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
    // for the classic route: exactly one execution of exactly the approved
    // plan (routine, digest, casting mode, at most the approved number of
    // steps). Every other classic execution stays refused.
    public sealed class ClassicCastGrant
    {
        public ClassicCastGrant(string runId, string routineId, string planDigest,
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
        public string LastRefusal { get; private set; }

        public bool TryConsume(string routineId, string planDigest, string executionMode,
            int plannedSteps, out string refusal)
        {
            Attempts++;
            refusal = Consumed ? "classic-grant-consumed"
                : !string.Equals(routineId, RoutineId, StringComparison.Ordinal)
                    ? "classic-grant-routine:" + (routineId ?? "none")
                : !string.Equals(executionMode, ExecutionMode, StringComparison.Ordinal)
                    ? "classic-grant-mode:" + (executionMode ?? "none")
                : !string.Equals(planDigest, PlanDigest, StringComparison.Ordinal)
                    ? "classic-grant-plan-differs"
                : plannedSteps < 1 || plannedSteps > MaximumSubmissions
                    ? "classic-grant-cap:" + plannedSteps + ">" + MaximumSubmissions
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
                ";lastRefusal=" + (LastRefusal ?? "none");
        }
    }

    // The run-bound allowance for one classic cast run (kind
    // kbp-classic-cast, schema 1): the exact build, fixture campaign,
    // casting mode, routine and approved plan digest, and a 1..24
    // submission budget.
    public sealed class ClassicCastAllowance
    {
        private ClassicCastAllowance() { }

        public const int AllowanceSchemaVersion = 1;

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

        private static readonly string[] Members =
        {
            "schemaVersion", "kind", "runId", "sourceCommit", "packageSha256", "dllSha256",
            "assemblyMvid", "fixtureGameId", "executionMode", "routineId", "approvedPlanDigest",
            "maximumNativeSubmissions", "approvedBy", "authority"
        };

        public static ClassicCastAllowance Parse(string json, string expectedRunId, out string refusal)
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
            if (Text(root, "kind") != "kbp-classic-cast") { refusal = "allowance-kind"; return null; }
            if (root["maximumNativeSubmissions"].Type != JTokenType.Integer)
            { refusal = "allowance-submissions"; return null; }
            int maximum = (int)root["maximumNativeSubmissions"];
            if (maximum < 1 || maximum > CastingQualificationAllowance.MaximumSubmissionsCeiling)
            { refusal = "allowance-submissions-range"; return null; }
            var allowance = new ClassicCastAllowance
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

    // One classic plan step as observed around the run: how the executor
    // reported it, how the target's effect changed, and the source's
    // native count before and after (negative: unlimited).
    public sealed class ClassicCastStepResult
    {
        public ClassicCastStepResult(int stepIndex, string providerCanonical, bool levelZeroSpellbook,
            string targetUnitId)
        {
            StepIndex = stepIndex;
            ProviderCanonical = providerCanonical ?? string.Empty;
            LevelZeroSpellbook = levelZeroSpellbook;
            TargetUnitId = targetUnitId ?? string.Empty;
        }

        public int StepIndex { get; private set; }
        public string ProviderCanonical { get; private set; }
        // Authored from a spellbook's level-0 entry (a cantrip): it must be
        // cast through the at-will cantrip ability.
        public bool LevelZeroSpellbook { get; private set; }
        public string TargetUnitId { get; private set; }
        public string FinalStatus { get; set; }
        public string Detail { get; set; }
        public string Transition { get; set; }
        public int? AvailableBefore { get; set; }
        public int? AvailableAfter { get; set; }
    }

    // What one classic cast (or select-only) run did, judged Unity-free.
    public sealed class ClassicCastRecord
    {
        public bool CastingScenario { get; set; }
        public string AllowanceStatus { get; set; } = "not-read";
        public string ExecutionMode { get; set; }
        public string PlanDigest { get; set; }
        public int PlanSteps { get; set; }
        public string Grant { get; set; }
        public bool GrantConsumed { get; set; }
        public int GrantAttempts { get; set; }
        public string QuickDisposition { get; set; }
        public int Planned { get; set; }
        public int Submitted { get; set; }
        public int Confirmed { get; set; }
        public int Failed { get; set; }
        public int CastingFirstRuns { get; set; }
        public List<ClassicCastStepResult> Steps { get; } = new List<ClassicCastStepResult>();
        // "<poolKey>:<remaining before>><remaining after>" for every finite
        // pool of the party: no finite resource may change for free casts.
        public List<string> FinitePools { get; } = new List<string>();
        public List<string> Failures { get; } = new List<string>();

        public IList<string> Violations()
        {
            var violations = new List<string>(Failures);
            if (PlanSteps < 1 || string.IsNullOrEmpty(PlanDigest)) violations.Add("plan:" + PlanSteps);
            if (CastingFirstRuns != 0) violations.Add("casting-first-runs:" + CastingFirstRuns);
            if (!CastingScenario)
            {
                if (GrantAttempts != 0 || GrantConsumed) violations.Add("select-grant-used");
                return violations;
            }
            if (violations.Count != 0) return violations;
            if (AllowanceStatus != "valid") violations.Add("allowance:" + AllowanceStatus);
            if (!GrantConsumed || GrantAttempts != 1) violations.Add("grant:" + (Grant ?? "none"));
            if (QuickDisposition != "Completed") violations.Add("disposition:" + (QuickDisposition ?? "none"));
            if (Planned != PlanSteps || Confirmed != PlanSteps || Submitted != PlanSteps || Failed != 0)
                violations.Add("report:planned=" + Planned + ";submitted=" + Submitted + ";confirmed=" +
                    Confirmed + ";failed=" + Failed + ";steps=" + PlanSteps);
            if (Steps.Count != PlanSteps) violations.Add("steps-observed:" + Steps.Count);
            foreach (ClassicCastStepResult step in Steps)
            {
                string failure = StepFailure(step);
                if (failure != null) violations.Add("step" + step.StepIndex + ":" + failure);
            }
            foreach (string pool in FinitePools)
            {
                string[] parts = pool.Split(new[] { ':' }, 2);
                string[] counts = parts.Length == 2 ? parts[1].Split('>') : new string[0];
                if (counts.Length != 2 || counts[0] != counts[1]) violations.Add("finite-pool:" + pool);
            }
            return violations;
        }

        // A free cast: confirmed, the target's effect newly applied or
        // refreshed, the source's unlimited count unchanged, and a cantrip
        // authored from a spellbook cast through its at-will ability.
        public static string StepFailure(ClassicCastStepResult step)
        {
            if (step == null) return "missing";
            if (step.FinalStatus != "EffectConfirmed") return "status:" + (step.FinalStatus ?? "none");
            if (step.Transition != "new-instance" && step.Transition != "refreshed")
                return "effect:" + (step.Transition ?? "unobserved");
            if (step.AvailableBefore == null || step.AvailableAfter == null)
                return "availability-unread";
            if (step.AvailableBefore.Value >= 0 || step.AvailableAfter.Value != step.AvailableBefore.Value)
                return "availability:" + step.AvailableBefore + ">" + step.AvailableAfter;
            string detail = step.Detail ?? string.Empty;
            if (detail.IndexOf(";resolution:", StringComparison.Ordinal) < 0 &&
                !detail.StartsWith("resolution:", StringComparison.Ordinal))
                return "resolution-unrecorded";
            if (step.LevelZeroSpellbook &&
                detail.IndexOf("resolution:at-will-cantrip-ability", StringComparison.Ordinal) < 0)
                return "cantrip-not-at-will";
            return null;
        }
    }
}
