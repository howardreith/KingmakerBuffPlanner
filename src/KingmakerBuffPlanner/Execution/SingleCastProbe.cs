using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Execution
{
    // The owner's run-specific, one-shot permission for the first native
    // probe cast. It exists only as a file the OWNER writes for one run id;
    // nothing in the mod, UI, settings or hotkeys can create or widen it.
    // It binds the exact approved projection (ProjectionId covers every
    // executable field, review L3) and the selection that produced it.
    public sealed class SingleCastProbeAllowance
    {
        private SingleCastProbeAllowance() { }

        public string RunId { get; private set; }
        public string SourceCommit { get; private set; }
        public string ApprovedProjectionId { get; private set; }
        public string CasterUnitId { get; private set; }
        public string TargetUnitId { get; private set; }
        public string SourceId { get; private set; }
        // Review N1: the frozen artifact the owner approved. A same-commit
        // replacement binary does not satisfy it.
        public string PackageSha256 { get; private set; }
        public string DllSha256 { get; private set; }
        public string AssemblyMvid { get; private set; }

        public const int AllowanceSchemaVersion = 2;

        private static readonly string[] Members =
        {
            "schemaVersion", "kind", "runId", "sourceCommit", "packageSha256", "dllSha256",
            "assemblyMvid", "approvedProjectionId", "casterUnitId", "targetUnitId", "sourceId",
            "maximumNativeSubmissions", "approvedBy"
        };

        // Strict: exact member set, exact kind, the run id this process was
        // launched for, a 64-hex projection id, and at most ONE submission.
        public static SingleCastProbeAllowance Parse(string json, string expectedRunId,
            out string refusal)
        {
            refusal = null;
            JObject root;
            try { root = JObject.Parse(json ?? string.Empty); }
            catch (Exception) { refusal = "allowance-unreadable"; return null; }
            var names = root.Properties().Select(property => property.Name).ToList();
            string unknown = names.FirstOrDefault(name => !Members.Contains(name));
            if (unknown != null) { refusal = "allowance-unknown-member:" + unknown; return null; }
            string missing = Members.FirstOrDefault(name => root[name] == null);
            if (missing != null) { refusal = "allowance-missing-member:" + missing; return null; }
            if (root["schemaVersion"].Type != JTokenType.Integer ||
                (int)root["schemaVersion"] != AllowanceSchemaVersion)
            { refusal = "allowance-schema"; return null; }
            if (Text(root, "kind") != "kbp-single-cast-probe")
            { refusal = "allowance-kind"; return null; }
            if (root["maximumNativeSubmissions"].Type != JTokenType.Integer ||
                (int)root["maximumNativeSubmissions"] != 1)
            { refusal = "allowance-submissions-not-one"; return null; }
            var allowance = new SingleCastProbeAllowance
            {
                RunId = Text(root, "runId"),
                SourceCommit = Text(root, "sourceCommit"),
                ApprovedProjectionId = Text(root, "approvedProjectionId"),
                CasterUnitId = Text(root, "casterUnitId"),
                TargetUnitId = Text(root, "targetUnitId"),
                SourceId = Text(root, "sourceId"),
                PackageSha256 = Text(root, "packageSha256"),
                DllSha256 = Text(root, "dllSha256"),
                AssemblyMvid = Text(root, "assemblyMvid")
            };
            if (string.IsNullOrEmpty(Text(root, "approvedBy")))
            { refusal = "allowance-approver-missing"; return null; }
            if (string.IsNullOrEmpty(expectedRunId) ||
                !string.Equals(allowance.RunId, expectedRunId, StringComparison.Ordinal))
            { refusal = "allowance-run-mismatch"; return null; }
            if (!IsLowerHex64(allowance.ApprovedProjectionId))
            { refusal = "allowance-projection-id"; return null; }
            Guid mvid;
            if (!IsLowerHex64(allowance.PackageSha256) || !IsLowerHex64(allowance.DllSha256) ||
                allowance.AssemblyMvid == null || !Guid.TryParse(allowance.AssemblyMvid, out mvid) ||
                mvid.ToString("D") != allowance.AssemblyMvid)
            { refusal = "allowance-artifact-identity"; return null; }
            if (string.IsNullOrEmpty(allowance.SourceCommit) ||
                string.IsNullOrEmpty(allowance.CasterUnitId) ||
                string.IsNullOrEmpty(allowance.TargetUnitId) ||
                string.IsNullOrEmpty(allowance.SourceId) ||
                string.Equals(allowance.CasterUnitId, allowance.TargetUnitId, StringComparison.Ordinal))
            { refusal = "allowance-selection-invalid"; return null; }
            return allowance;
        }

        internal static bool IsLowerHex64(string value)
        {
            return value != null && value.Length == 64 &&
                value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
        }

        private static string Text(JObject root, string name)
        {
            JToken token = root[name];
            return token != null && token.Type == JTokenType.String ? (string)token : null;
        }
    }

    // Review N1: the identity of the code actually loaded in this process,
    // measured at submit time (commit compiled in, the launcher-verified
    // package, the SHA-256 of the loaded DLL file and the loaded module's
    // MVID).
    public sealed class SingleCastProbeRuntimeIdentity
    {
        public SingleCastProbeRuntimeIdentity(string sourceCommit, string packageSha256,
            string dllSha256, string assemblyMvid)
        {
            SourceCommit = sourceCommit;
            PackageSha256 = packageSha256;
            DllSha256 = dllSha256;
            AssemblyMvid = assemblyMvid;
        }

        public string SourceCommit { get; private set; }
        public string PackageSha256 { get; private set; }
        public string DllSha256 { get; private set; }
        public string AssemblyMvid { get; private set; }

        public string Describe()
        {
            return "commit=" + SourceCommit + ";package=" + PackageSha256 + ";dll=" + DllSha256 +
                ";mvid=" + AssemblyMvid;
        }
    }

    public sealed class SingleCastProbeSelection
    {
        internal SingleCastProbeSelection(string refusal, string casterUnitId,
            string targetUnitId, string sourceId, ProviderKey provider,
            ExplicitCastingPlan plan, CastingApplyDecision decision,
            ExplicitStepConversion projection, int candidatesConsidered,
            IList<string> rejections = null)
        {
            Rejections = new List<string>(rejections ?? new string[0]);
            Refusal = refusal ?? string.Empty;
            CasterUnitId = casterUnitId;
            TargetUnitId = targetUnitId;
            SourceId = sourceId;
            Provider = provider;
            Plan = plan;
            Decision = decision;
            Projection = projection;
            CandidatesConsidered = candidatesConsidered;
        }

        public bool Selected { get { return Projection != null && Projection.Converted; } }
        public string Refusal { get; private set; }
        public string CasterUnitId { get; private set; }
        public string TargetUnitId { get; private set; }
        public string SourceId { get; private set; }
        public ProviderKey Provider { get; private set; }
        public ExplicitCastingPlan Plan { get; private set; }
        public CastingApplyDecision Decision { get; private set; }
        public ExplicitStepConversion Projection { get; private set; }
        public int CandidatesConsidered { get; private set; }
        // Why each discovered option/target was not chosen (bounded), so a
        // selection-only run explains an empty result.
        public IReadOnlyList<string> Rejections { get; private set; }
    }

    // Deterministic, discovery-driven choice of the probe casting: the
    // first (ordinal order of provider, then target) plain spellbook spell
    // whose single-casting plan compiles Ready, passes the ordinary gate and
    // converts under SingleCastProbe. Every subset rule is enforced by the
    // converter (review L6), not by this selector; the selector only picks.
    public static class SingleCastProbeSelector
    {
        public const string ProbeCastingId = "probe-cast-1";
        public const int MaximumRecordedRejections = 60;

        // effectPresentOnTarget (optional, a FRESH native read): targets
        // without the expected effect are preferred, unknown next, present
        // last (review M2); a present effect then needs a verified refresh.
        public static SingleCastProbeSelection Select(CastingWorkspaceInputs inputs,
            string campaignId, Func<string, EffectExpression, bool?> effectPresentOnTarget = null)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            var units = new HashSet<string>(inputs.Snapshot.Units.Select(unit => unit.UnitId),
                StringComparer.Ordinal);
            int considered = 0;
            var rejections = new List<string>();
            Action<string> reject = value => { if (rejections.Count < MaximumRecordedRejections) rejections.Add(value); };
            foreach (ProviderPlanningOption option in inputs.ProviderOptions
                .Where(value => value.Provider != null)
                .OrderBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal))
            {
                AbilityKey ability = option.Provider.Key.Ability;
                string provider = option.Provider.Key.Canonical;
                if (ability.SourceKind != SourceKind.Spellbook || ability.MetamagicMask != 0)
                {
                    reject(provider + "|source-kind-or-metamagic");
                    continue;
                }
                string sourceId = SourceIdFor(inputs.EffectsBySource, ability);
                if (sourceId == null) { reject(provider + "|no-expected-effects"); continue; }
                string caster = option.Provider.Key.CasterUnitId;
                if (!option.ReachableTargetIds.Any(value => units.Contains(value) &&
                        !string.Equals(value, caster, StringComparison.Ordinal)))
                    reject(provider + "|no-other-reachable-target");
                EffectExpression expectedEffects = inputs.EffectsBySource[sourceId];
                Func<string, int> presenceRank = target =>
                {
                    if (effectPresentOnTarget == null) return 1;
                    bool? present;
                    try { present = effectPresentOnTarget(target, expectedEffects); }
                    catch (Exception) { present = null; }
                    return present == false ? 0 : present == null ? 1 : 2;
                };
                foreach (string target in option.ReachableTargetIds
                    .Where(value => units.Contains(value) &&
                        !string.Equals(value, caster, StringComparison.Ordinal))
                    .OrderBy(presenceRank)
                    .ThenBy(value => value, StringComparer.Ordinal))
                {
                    considered++;
                    var casting = new PlannedCasting(ProbeCastingId, "long", 0, sourceId,
                        ability, caster, option.Provider.Key.SpellbookGuid,
                        CastingTargetMode.DirectTarget, target, null, null, null, null,
                        ExistingEffectPolicy.SkipAlreadyActive, null,
                        CastingAuthoringState.Ready, null);
                    var document = new CastingPlanDocument(campaignId ?? "probe",
                        new[] { new RoutineDefinition("long", "Long") }, new[] { casting });
                    ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(
                        document, inputs.Snapshot, inputs.ProviderOptions,
                        inputs.EffectsBySource, inputs.Enhancements, "long",
                        inputs.TargetingModifiers);
                    CastingApplyDecision decision = new CastingExecutionGate().Evaluate(
                        plan, CastingApplyMode.Ordinary, "long");
                    if (!decision.Allowed)
                    {
                        ResolvedCasting refused = plan.CastingById(ProbeCastingId);
                        reject(provider + "|" + target + "|not-ready:" + string.Join(",",
                            refused.ReadinessReasons.ToArray()));
                        continue;
                    }
                    ExplicitStepConversion projection = ExplicitCastingStepConverter.Convert(
                        plan, decision, inputs.ProviderOptions, inputs.EffectsBySource,
                        ExplicitProjectionScope.SingleCastProbe);
                    if (!projection.Converted)
                    {
                        reject(provider + "|" + target + "|" + projection.Refusal);
                        continue;
                    }
                    ResolvedCasting resolved = plan.CastingById(ProbeCastingId);
                    return new SingleCastProbeSelection(null, caster, target, sourceId,
                        resolved.Provider, plan, decision, projection, considered, rejections);
                }
            }
            return new SingleCastProbeSelection("no-eligible-probe-casting", null, null, null,
                null, null, null, null, considered, rejections);
        }

        // The discovered catalogue source for an ability: the production
        // alias contract maps effects[ability.Canonical] and
        // effects[sourceId] to the SAME instance; the ordinal-first such
        // source id is used, else the canonical key itself.
        internal static string SourceIdFor(IReadOnlyDictionary<string, EffectExpression> effects,
            AbilityKey ability)
        {
            EffectExpression expected;
            if (!effects.TryGetValue(ability.Canonical, out expected) || expected == null)
                return null;
            string alias = effects
                .Where(pair => !string.Equals(pair.Key, ability.Canonical, StringComparison.Ordinal) &&
                    ReferenceEquals(pair.Value, expected))
                .Select(pair => pair.Key)
                .OrderBy(key => key, StringComparer.Ordinal)
                .FirstOrDefault();
            return alias ?? ability.Canonical;
        }
    }

    // The ONLY dispatch boundary that can submit a native cast from the
    // casting-first path, and only for one probe run. Default-refusing: with
    // no allowance every Submit is refused and recorded. With an allowance
    // it accepts exactly one SingleCastProbe projection whose recomputed
    // identity equals the approved id, consumes the allowance BEFORE
    // starting (never a second submission, never a retry), and runs it
    // through the existing executor under ExplicitCastingRunCoordinator with
    // maximumSubmissions = 1. The owner pumps ActiveRun each frame and MUST
    // dispose the boundary on stop, deadline or teardown; disposal
    // propagates to the executor's cleanup (review L2). The production
    // workspace never constructs this type.
    public sealed class SingleCastProbeBoundary : ICastingDispatchBoundary, IDisposable
    {
        private readonly SingleCastProbeAllowance _allowance;
        private readonly Func<ICastExecutor> _executorFactory;
        private readonly Func<SingleCastProbeRuntimeIdentity> _runtimeIdentity;
        private bool _consumed;
        private bool _disposed;

        public SingleCastProbeBoundary(SingleCastProbeAllowance allowance,
            Func<ICastExecutor> executorFactory,
            Func<SingleCastProbeRuntimeIdentity> runtimeIdentity)
        {
            _allowance = allowance;
            _executorFactory = executorFactory;
            _runtimeIdentity = runtimeIdentity;
        }

        // The last measured identity (evidence), null until measured.
        public SingleCastProbeRuntimeIdentity MeasuredIdentity { get; private set; }

        // Review N1: every check that can refuse, run BEFORE any observation,
        // executor construction or submission. Submit re-runs it.
        public string Preflight(ExplicitCastingPlan plan, CastingApplyDecision decision,
            ExplicitStepConversion projection)
        {
            return Validate(plan, decision, projection);
        }

        public readonly List<string> Refusals = new List<string>();
        public IEnumerator ActiveRun { get; private set; }
        public ExplicitCastingRunOutcome Outcome { get; private set; }
        public bool AllowanceConsumed { get { return _consumed; } }

        public string DispositionReason
        {
            get
            {
                return _allowance == null
                    ? "native-submission-disabled:no-probe-allowance"
                    : _consumed ? "probe-allowance-consumed" : "probe-allowance-armed";
            }
        }

        public CastingDispatchOutcome Submit(ExplicitCastingPlan plan,
            CastingApplyDecision decision, string scopeRoutineId,
            ExplicitStepConversion projection)
        {
            string refusal = Validate(plan, decision, projection);
            if (refusal != null)
            {
                Refusals.Add(refusal);
                return new CastingDispatchOutcome(false, refusal,
                    decision == null ? new string[0] : decision.ExecutableCastingIds);
            }
            ICastExecutor executor;
            try { executor = _executorFactory(); }
            catch (Exception exception)
            {
                _consumed = true;
                string failure = "probe-executor-unavailable:" + exception.GetType().Name;
                Refusals.Add(failure);
                return new CastingDispatchOutcome(false, failure, projection.CastingIds);
            }
            // One shot: consumed before anything can reach the game.
            _consumed = true;
            if (executor == null)
            {
                Refusals.Add("probe-executor-null");
                return new CastingDispatchOutcome(false, "probe-executor-null", projection.CastingIds);
            }
            ActiveRun = new ExplicitCastingRunCoordinator(executor, 1)
                .Run(projection, outcome => Outcome = outcome);
            return new CastingDispatchOutcome(true, "probe-submitted:" + projection.ProjectionId,
                projection.CastingIds);
        }

        private string Validate(ExplicitCastingPlan plan, CastingApplyDecision decision,
            ExplicitStepConversion projection)
        {
            if (_disposed) return "probe-boundary-disposed";
            if (_allowance == null) return "native-submission-disabled:no-probe-allowance";
            if (_consumed) return "probe-allowance-consumed";
            if (_executorFactory == null) return "probe-executor-factory-missing";
            string identity = IdentityRefusal();
            if (identity != null) return identity;
            if (plan == null || decision == null || !decision.Allowed)
                return "probe-decision-not-allowed";
            if (projection == null || !projection.Converted)
                return "probe-projection-not-converted";
            if (projection.Scope != ExplicitProjectionScope.SingleCastProbe)
                return "probe-scope-required:" + projection.Scope;
            if (projection.Plan.Steps.Count != 1 || projection.CastingIds.Count != 1 ||
                decision.ExecutableCastingIds.Count != 1 ||
                !string.Equals(decision.ExecutableCastingIds[0], projection.CastingIds[0],
                    StringComparison.Ordinal))
                return "probe-requires-exactly-one-casting";
            // Recompute the identity from the steps actually handed over: a
            // projection object whose steps differ from its recorded contract
            // is refused, as is any id other than the approved one.
            string recomputed = ExplicitCastingStepConverter.Identity(
                projection.Plan.Steps.ToList(), projection.Scope);
            if (!string.Equals(recomputed, projection.ProjectionId, StringComparison.Ordinal))
                return "probe-projection-tampered";
            if (!string.Equals(projection.ProjectionId, _allowance.ApprovedProjectionId,
                    StringComparison.Ordinal))
                return "probe-projection-not-approved";
            CastStep step = projection.Plan.Steps[0];
            if (!string.Equals(step.Provider.CasterUnitId, _allowance.CasterUnitId, StringComparison.Ordinal) ||
                step.TargetUnitIds.Count != 1 ||
                !string.Equals(step.TargetUnitIds[0], _allowance.TargetUnitId, StringComparison.Ordinal) ||
                !string.Equals(step.SourceId, _allowance.SourceId, StringComparison.Ordinal))
                return "probe-selection-not-approved";
            return null;
        }

        private string IdentityRefusal()
        {
            SingleCastProbeRuntimeIdentity measured;
            try { measured = _runtimeIdentity == null ? null : _runtimeIdentity(); }
            catch (Exception exception)
            {
                return "probe-runtime-identity-unavailable:" + exception.GetType().Name;
            }
            MeasuredIdentity = measured;
            if (measured == null) return "probe-runtime-identity-unavailable";
            if (!string.Equals(measured.SourceCommit, _allowance.SourceCommit, StringComparison.Ordinal))
                return "probe-identity-mismatch:commit";
            if (!string.Equals(measured.PackageSha256, _allowance.PackageSha256, StringComparison.Ordinal))
                return "probe-identity-mismatch:package";
            if (!string.Equals(measured.DllSha256, _allowance.DllSha256, StringComparison.Ordinal))
                return "probe-identity-mismatch:dll";
            if (!string.Equals(measured.AssemblyMvid, _allowance.AssemblyMvid, StringComparison.Ordinal))
                return "probe-identity-mismatch:mvid";
            return null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var run = ActiveRun as IDisposable;
            ActiveRun = null;
            if (run != null) run.Dispose();
        }
    }

    // What one probe run did, filled by the run owner and judged here
    // (Unity-free, unit-tested). Selection-only runs must never construct
    // the boundary; the casting run passes only with a valid allowance, one
    // confirmed casting, a verified effect transition and the exact
    // expected resource delta from FRESH before/after observations (review
    // M2), and a clean, owned terminal (review M3). Nothing here claims more
    // than the run observed; spent resources and applied effects are never
    // described as rolled back.
    public sealed class SingleCastProbeRunRecord
    {
        public bool CastingScenario { get; set; }
        public bool Selected { get; set; }
        public string SelectionEvidence { get; set; }
        public string ProjectionId { get; set; }
        public string AllowanceStatus { get; set; } = "not-read";
        public bool BoundaryConstructed { get; set; }
        public bool Submitted { get; set; }
        public string SubmitReason { get; set; }
        public ExplicitCastingRunOutcome Outcome { get; set; }
        public SingleCastProbeObservationSession Observation { get; set; }
        public bool BoundaryDisposed { get; set; }
        public bool WorkspaceClosed { get; set; }
        public bool InputLeaseReleased { get; set; }
        // "completed", "deadline", "stop", "mod-disabled", "mod-unload",
        // "host-exception:..." - how the owner ended the run.
        public string TerminalReason { get; set; }
        public string MeasuredIdentity { get; set; }
        public bool CleanupRecorded { get; set; }
        public List<string> CleanupFailures { get; } = new List<string>();

        public IList<string> Violations()
        {
            var violations = new List<string>();
            if (!Selected) violations.Add("probe-selection:" + (SelectionEvidence ?? "missing"));
            if (!CleanupRecorded) violations.Add("probe-cleanup-not-recorded");
            if (TerminalReason != null && TerminalReason != "completed")
                violations.Add("probe-terminated:" + TerminalReason);
            foreach (string failure in CleanupFailures) violations.Add("probe-cleanup-failed:" + failure);
            if (!WorkspaceClosed || !InputLeaseReleased)
                violations.Add("probe-close:closed=" + WorkspaceClosed + ";leaseReleased=" + InputLeaseReleased);
            if (!CastingScenario)
            {
                if (BoundaryConstructed || Submitted)
                    violations.Add("selection-only-run-touched-dispatch");
                return violations;
            }
            if (AllowanceStatus != "valid")
            {
                violations.Add("probe-allowance:" + AllowanceStatus);
                if (Submitted) violations.Add("submitted-without-valid-allowance");
                return violations;
            }
            if (!Submitted) { violations.Add("probe-not-submitted:" + (SubmitReason ?? "missing")); return violations; }
            if (!BoundaryDisposed) violations.Add("probe-boundary-not-disposed");
            if (Outcome == null) violations.Add("probe-outcome-missing");
            else
            {
                if (Outcome.Entries.Count != 1 || !Outcome.AllConfirmed)
                    violations.Add("probe-not-confirmed:" + Outcome.HaltReason);
                if (Outcome.ProjectionId != ProjectionId)
                    violations.Add("probe-outcome-projection-mismatch");
            }
            if (Observation == null) violations.Add("probe-observation-missing");
            else foreach (string violation in Observation.Violations())
                    violations.Add("probe-observation:" + violation);
            return violations;
        }
    }

    public sealed class ProbeWorkspaceCloseResult
    {
        public ProbeWorkspaceCloseResult(bool closed, bool inputLeaseReleased, string failure)
        {
            Closed = closed;
            InputLeaseReleased = inputLeaseReleased;
            Failure = failure;
        }

        public bool Closed { get; private set; }
        public bool InputLeaseReleased { get; private set; }
        public string Failure { get; private set; }
    }

    // Review M3: the single owner of a probe run's boundary, observation and
    // terminal cleanup. The runtime host delegates to it for normal
    // completion, stop marker, deadline, unexpected host failure, mod disable
    // and unload. Terminate is idempotent: it disposes the active boundary
    // (propagating to the executor's cleanup) BEFORE abandoning it, takes a
    // fresh after-observation when a cast was submitted, closes the owned
    // workspace and releases the input lease, records cleanup, and only then
    // publishes the record exactly once. The primary reason is kept; cleanup
    // failures are appended, never substituted.
    public sealed class SingleCastProbeRunOwner
    {
        private readonly SingleCastProbeRunRecord _record;
        private readonly Func<ProbeWorkspaceCloseResult> _closeWorkspace;
        private readonly Action<SingleCastProbeRunRecord> _publish;
        private SingleCastProbeBoundary _boundary;
        private long _runStartedMillis = -1;
        private bool _terminated;

        public SingleCastProbeRunOwner(SingleCastProbeRunRecord record,
            Func<ProbeWorkspaceCloseResult> closeWorkspace,
            Action<SingleCastProbeRunRecord> publish)
        {
            _record = record ?? throw new ArgumentNullException("record");
            _closeWorkspace = closeWorkspace ?? throw new ArgumentNullException("closeWorkspace");
            _publish = publish ?? throw new ArgumentNullException("publish");
        }

        public SingleCastProbeRunRecord Record { get { return _record; } }

        // Review N2: selection may begin only while the request is live.
        // Once terminated (disable, unload, host failure, stop) it never
        // becomes live again.
        public bool BeginSelection()
        {
            return !_terminated;
        }
        public bool Terminated { get { return _terminated; } }
        public bool Running { get { return !_terminated && _boundary != null && _boundary.ActiveRun != null; } }
        public int PublishCount { get; private set; }

        // Takes a FRESH before-observation, marks the submission sequence and
        // submits through the one-shot boundary. A failed before-read refuses
        // the submission (nothing reaches the game).
        public CastingDispatchOutcome Submit(SingleCastProbeBoundary boundary,
            SingleCastProbeObservationSession observation, ExplicitCastingPlan plan,
            CastingApplyDecision decision, ExplicitStepConversion projection, long nowMillis)
        {
            if (boundary == null) throw new ArgumentNullException("boundary");
            // Review N2: a terminated request never arms or submits.
            if (_terminated)
                return new CastingDispatchOutcome(false, "probe-owner-terminated:" +
                    _record.TerminalReason, projection == null ? new string[0] : projection.CastingIds);
            _boundary = boundary;
            _record.BoundaryConstructed = true;
            _record.Observation = observation;
            // Review N1: identity and every other refusal come first - no
            // observation, executor construction or submission on refusal.
            string preflight = boundary.Preflight(plan, decision, projection);
            if (boundary.MeasuredIdentity != null)
                _record.MeasuredIdentity = boundary.MeasuredIdentity.Describe();
            if (preflight != null)
            {
                _record.Submitted = false;
                _record.SubmitReason = preflight;
                return new CastingDispatchOutcome(false, preflight, projection.CastingIds);
            }
            ProbeObservation before = observation.ObserveBefore();
            if (!before.Succeeded)
            {
                _record.Submitted = false;
                _record.SubmitReason = "before-observation-unavailable:" + before.Failure;
                return new CastingDispatchOutcome(false, _record.SubmitReason, projection.CastingIds);
            }
            observation.MarkSubmission();
            CastingDispatchOutcome outcome = boundary.Submit(plan, decision, "long", projection);
            _record.Submitted = outcome.Submitted;
            _record.SubmitReason = outcome.Reason;
            _runStartedMillis = nowMillis;
            return outcome;
        }

        // One step per frame. Returns true once the run has terminated.
        public bool Pump(long nowMillis, long deadlineMillis, bool stopRequested)
        {
            if (_terminated) return true;
            if (_boundary == null || _boundary.ActiveRun == null)
            {
                Terminate("completed");
                return true;
            }
            if (stopRequested) { Terminate("stop"); return true; }
            if (_runStartedMillis >= 0 && nowMillis - _runStartedMillis > deadlineMillis)
            {
                Terminate("deadline");
                return true;
            }
            bool moved;
            try { moved = _boundary.ActiveRun.MoveNext(); }
            catch (Exception exception)
            {
                Terminate("run-pump-exception:" + exception.GetType().Name + ":" + exception.Message);
                return true;
            }
            if (moved) return false;
            Terminate("completed");
            return true;
        }

        public void Terminate(string reason)
        {
            if (_terminated) return;
            _terminated = true;
            _record.TerminalReason = string.IsNullOrEmpty(reason) ? "unspecified" : reason;
            if (_boundary != null)
            {
                try
                {
                    _boundary.Dispose();
                    _record.BoundaryDisposed = true;
                }
                catch (Exception exception)
                {
                    _record.CleanupFailures.Add("boundary-dispose:" + exception.GetType().Name + ":" +
                        exception.Message);
                }
                _record.Outcome = _boundary.Outcome;
            }
            if (_record.Submitted && _record.Observation != null && _record.Observation.After == null)
                _record.Observation.ObserveAfter();
            try
            {
                ProbeWorkspaceCloseResult closed = _closeWorkspace();
                _record.WorkspaceClosed = closed != null && closed.Closed && string.IsNullOrEmpty(closed.Failure);
                _record.InputLeaseReleased = closed != null && closed.InputLeaseReleased;
                if (closed != null && !string.IsNullOrEmpty(closed.Failure))
                    _record.CleanupFailures.Add("workspace-close:" + closed.Failure);
            }
            catch (Exception exception)
            {
                _record.CleanupFailures.Add("workspace-close:" + exception.GetType().Name + ":" +
                    exception.Message);
            }
            _record.CleanupRecorded = true;
            try
            {
                PublishCount++;
                _publish(_record);
            }
            catch (Exception exception)
            {
                _record.CleanupFailures.Add("publish:" + exception.GetType().Name + ":" + exception.Message);
            }
        }
    }
}
