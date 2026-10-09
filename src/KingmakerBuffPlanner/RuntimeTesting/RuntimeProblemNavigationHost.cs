using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal sealed partial class RuntimeTestHost
    {
        private readonly ProblemNavigationRecord _problemRecord = new ProblemNavigationRecord();
        private int _problemRunsBefore;
        private string _problemProfilePath;

        private bool PhysicalProblemsRequested
        {
            get
            {
                object expectation;
                return _request.Parameters.TryGetValue("physicalExpectation", out expectation) &&
                    string.Equals(expectation as string, "problems", StringComparison.Ordinal);
            }
        }

        private bool UpdateProblemNavigation(CastingWorkspaceScreenView view, double settled)
        {
            if (_physicalStep == 0)
            {
                // Allow the launcher's pending menu-dismissal input to finish.
                if (_physicalClock.Elapsed.TotalSeconds < 4) return false;
                if (BuffPlannerUiRoot.NativeEscMenuOpenForRuntime)
                {
                    if (++_physicalMenuCloseAttempts > 3) return FinishProblemNavigation("menu-veil");
                    return RequestPhysical("problem-menu-close-" + _physicalMenuCloseAttempts,
                        "key-escape", Vector2.zero, null, 0);
                }
                CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
                string campaign = Kingmaker.Game.Instance.Player.GameId;
                var earlier = new CastingWorkspaceSession(_modEntry.Path, campaign,
                    new DisabledCastingDispatchBoundary());
                if (earlier.Document.Castings.Count != 0) return FinishProblemNavigation("fixture-plan-not-empty");
                PlannedCasting seed = ColdSeedCasting(inputs, "long", new System.Collections.Generic.HashSet<string>());
                if (seed == null) return FinishProblemNavigation("fixture-no-free-source");
                // Twenty early castings of one buff ensure the two late Drafts
                // start below the real viewport, including at 1440p.
                for (int index = 0; index < 22; index++)
                {
                    string id = index == 20 ? "wp2a-blocked-late" : index == 21 ? "wp2a-blocked-last"
                        : "wp2a-ready-" + index.ToString("D2", CultureInfo.InvariantCulture);
                    var casting = new PlannedCasting(id, "long", index, seed.SourceId, seed.Ability,
                        seed.CasterUnitId, seed.SpellbookGuid, CastingTargetMode.DirectTarget,
                        seed.DirectTargetUnitId, null, null, null, null, ExistingEffectPolicy.SkipAlreadyActive,
                        null, index < 20 ? CastingAuthoringState.Ready : CastingAuthoringState.Draft, null);
                    AuthoringEditResult added = earlier.AddCastingForRuntime(casting);
                    if (!added.Applied) return FinishProblemNavigation("fixture-add:" + added.Reason);
                }
                CastingApplyDecision gate = new CastingExecutionGate().Evaluate(
                    earlier.CompileForRuntime(inputs, "long"), CastingApplyMode.Ordinary, "long");
                if (!gate.BlockingCastings.Select(blocker => blocker.CastingId).SequenceEqual(
                        new[] { "wp2a-blocked-late", "wp2a-blocked-last" }))
                    return FinishProblemNavigation("fixture-blockers-not-exact");
                _problemRecord.RunId = _request.RunId;
                _problemRecord.SourceCommit = _request.ExpectedCommit;
                _problemRecord.PackageSha256 = _request.ExpectedPackageSha256;
                _problemRecord.DllSha256 = _request.ExpectedDllSha256;
                _problemRecord.AssemblyMvid = typeof(RuntimeTestHost).Assembly.ManifestModule.ModuleVersionId.ToString("D");
                _problemRecord.ScreenWidth = Screen.width;
                _problemRecord.ScreenHeight = Screen.height;
                _problemRecord.PlannerClosedBeforeHud = !BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                _problemRecord.ColdSessionBeforeHud = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime() == null;
                _problemRecord.SourceId = seed.SourceId;
                _problemRecord.FirstCastingId = gate.BlockingCastings[0].CastingId;
                _problemRecord.SecondCastingId = gate.BlockingCastings[1].CastingId;
                _problemRecord.DocumentBefore = earlier.DocumentIntentSignature();
                _problemProfilePath = new CastingPlanRepository(_modEntry.Path).GetProfilePath(campaign);
                _problemRecord.ProfileBeforeSha256 = ProblemFileHash(_problemProfilePath);
                _problemRecord.ResourcesBefore = ProblemResources(inputs);
                _problemRecord.EffectsBefore = ProblemEffects(inputs);
                _problemRunsBefore = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                return RequestPhysical("problem-moon", "click",
                    BuffPlannerUiRoot.HudButtonCenterForRuntime("long"), null, 1);
            }
            if (settled < 0.75) return false;
            CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            if (_physicalStep >= 1 && _physicalStep <= 3)
            {
                if (session == null || view == null || !session.ProblemNavigation.Active)
                    return FinishProblemNavigation("problem-inspection-missing:step" + _physicalStep);
                if (_physicalStep == 1)
                {
                    _problemRecord.PlannerOpenedByHud = BuffPlannerUiRoot.IsCastingWorkspaceOpen &&
                        !_liveHotkeyMarkerWritten && !_workspaceProgrammaticOpen;
                    _problemRecord.GraphOverflow = view.GraphScrollOverflowForRuntime;
                    _problemRecord.FirstChipVisibleBeforeReveal = view.ProblemChipVisibleBeforeRevealForRuntime;
                    _problemRecord.GraphScrollBefore = view.ProblemGraphScrollBeforeRevealForRuntime;
                    _problemRecord.GraphScrollAfter = view.GraphScrollPositionForRuntime ?? 1f;
                    _problemRecord.UndoBefore = session.CanUndo;
                }
                ObserveProblem(view, session);
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory,
                    "problem-" + _physicalStep + ".png"));
                string part = _physicalStep == 1 ? "problem-next" : "problem-previous";
                if (_physicalStep == 3)
                    return RequestPhysical("problem-escape-focus", "key-escape", Vector2.zero, null, 4);
                Vector2? point = view.ScreenPointForRuntime(part);
                if (point == null) return FinishProblemNavigation("navigation-control-offscreen:" + part);
                return RequestPhysical(part, "click", point.Value, null, _physicalStep + 1);
            }
            if (_physicalStep == 4)
            {
                _problemRecord.EscapeLeftFocus = view != null && session != null &&
                    session.EditingFocusCastingId == null && !session.ProblemNavigation.Active &&
                    session.ProblemNavigation.PendingRevealCastingId == null;
                return RequestPhysical("problem-escape-close", "key-escape", Vector2.zero, null, 5);
            }
            if (_physicalStep == 5)
            {
                _problemRecord.PlannerClosedAfterEscape = !BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                _problemRecord.InputLeaseReleased = !BuffPlannerUiRoot.IsCastingWorkspaceInputLeaseHeldForRuntime;
                _problemRecord.DocumentAfter = session == null ? null : session.DocumentIntentSignature();
                _problemRecord.UndoAfter = session != null && session.CanUndo;
                _problemRecord.AcceptedDigestAfter = session == null ? "session-missing" : session.AcceptedDigestFor("long");
                _problemRecord.DispatchAttempts = session == null ? -1 : session.DispatchRefusalsForRuntime.Count;
                _problemRecord.RunsStarted = BuffPlannerUiRoot.CastingRunsStartedForRuntime - _problemRunsBefore;
                _problemRecord.ProfileAfterSha256 = ProblemFileHash(_problemProfilePath);
                CastingWorkspaceInputs after = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
                _problemRecord.ResourcesAfter = ProblemResources(after);
                _problemRecord.EffectsAfter = ProblemEffects(after);
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "problem-closed.png"));
                return FinishProblemNavigation(null);
            }
            return false;
        }

        private void ObserveProblem(CastingWorkspaceScreenView view, CastingWorkspaceSession session)
        {
            CastingProblemNavigation problems = session.ProblemNavigation;
            Vector2? chip = view.ScreenPointForRuntime("chip:" + problems.Current.CastingId);
            Vector2? tile = view.ScreenPointForRuntime("tile:" + session.SelectedSourceId);
            Vector2? routine = view.ScreenPointForRuntime("routine:" + session.SelectedRoutineId);
            var observation = new ProblemScreenObservation {
                CastingId = session.EditingFocusCastingId, RoutineId = session.SelectedRoutineId,
                SourceId = session.SelectedSourceId, Position = problems.Position, Count = problems.Count,
                ChipX = chip.HasValue ? (float?)chip.Value.x : null,
                ChipY = chip.HasValue ? (float?)chip.Value.y : null,
                ChipVisibleFraction = view.ProblemChipVisibleFractionForRuntime(problems.Current.CastingId),
                CatalogueX = tile.HasValue ? (float?)tile.Value.x : null,
                CatalogueY = tile.HasValue ? (float?)tile.Value.y : null,
                RoutineX = routine.HasValue ? (float?)routine.Value.x : null,
                RoutineY = routine.HasValue ? (float?)routine.Value.y : null,
                InspectorScroll = view.InspectorScrollPositionForRuntime,
                InspectorText = view.VisibleInspectorTextForRuntime, FooterText = view.FooterResultForRuntime,
                PreviousEnabled = view.ProblemControlEnabledForRuntime(false),
                NextEnabled = view.ProblemControlEnabledForRuntime(true),
                DocumentSignature = session.DocumentIntentSignature()
            };
            observation.MachineReasons.AddRange(problems.Current.Reasons);
            _problemRecord.Observations.Add(observation);
        }

        private bool FinishProblemNavigation(string failure)
        {
            if (failure != null) _problemRecord.Failures.Add(failure);
            _problemRecord.Acknowledged.Clear();
            _problemRecord.Acknowledged.AddRange(_physicalRecord.Acknowledged);
            PublishProblemRecord();
            _completed = true;
            return true;
        }

        private void PublishProblemRecord()
        {
            _physicalPublished = true;
            var serializer = JsonSerializer.Create(new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver() });
            JObject record = JObject.FromObject(_problemRecord, serializer);
            record["schemaVersion"] = 1;
            record["violations"] = new JArray(_problemRecord.Violations());
            AtomicFile.WriteUtf8(Path.Combine(_request.EvidenceDirectory, "physical-workspace-problems.json"),
                record.ToString(Formatting.Indented));
            _log.Info("[KBP-PROBLEMS] " + string.Join("|", _problemRecord.Violations().ToArray()));
        }

        private static string ProblemFileHash(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        private static string ProblemResources(CastingWorkspaceInputs inputs)
        {
            return string.Join("|", inputs.Snapshot.ResourcePools.OrderBy(pool => pool.PoolKey, StringComparer.Ordinal)
                .Select(pool => pool.PoolKey + "=" + pool.Remaining + ":" +
                    string.Join(",", pool.Tokens.Select(token => token.TokenId + "=" + token.Available)
                        .OrderBy(value => value, StringComparer.Ordinal).ToArray())).ToArray());
        }

        private static string ProblemEffects(CastingWorkspaceInputs inputs)
        {
            if (inputs.LiveEffects == null || !inputs.LiveEffects.HasInstanceDetail)
                throw new InvalidOperationException("Live effect instance evidence is unavailable.");
            // Presence, multiplicity, caster level, metamagic and suppression;
            // remaining duration naturally elapses and is not a cast effect.
            return string.Join("|", inputs.Snapshot.Units.OrderBy(unit => unit.UnitId, StringComparer.Ordinal)
                .Select(unit => unit.UnitId + "=" + string.Join(",", inputs.LiveEffects.GetInstances(unit.UnitId)
                    .Select(effect => effect.Kind + ":" + effect.EffectId + ":" + effect.CasterLevel + ":" +
                        effect.MetamagicMask + ":" + effect.Suppressed)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray())).ToArray());
        }
    }
}
