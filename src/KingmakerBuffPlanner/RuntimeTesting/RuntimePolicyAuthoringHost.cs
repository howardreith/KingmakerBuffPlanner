using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.UI;
using UnityEngine;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // The 0.4.0 physical expectations of live-workspace-physical beside
    // select / cast (WP4 and WP3 native evidence).
    //
    // combat: the cold seed of the selection run, then the moon pressed
    // physically while a party member is held in the game's own combat state
    // (UnitEntityData.JoinCombat with no enemy; the host keeps the group's
    // leave timer at zero so the state cannot lapse mid-press). The press
    // must be refused for combat before anything is compiled or dispatched,
    // spending and landing nothing. The state is then released and the
    // ordinary selection press must reach the session lock - the combat check
    // passes once combat is over. The rest of the selection run follows.
    //
    // authoring: the cold selection press, then - instead of the browse and
    // description gestures - WP3's direct manipulation through the OS
    // pointer: a caster and its exact source row, a portrait (adds and
    // focuses one casting), the same portrait (removes it), the portrait
    // again, another portrait (retargets), optionally another caster
    // (provider change), Undo, a card click (focus), Escape (leaves the
    // focus, the planner stays open) and Escape (closes the planner).
    internal sealed partial class RuntimeTestHost
    {
        private const int CombatStartStep = 110;
        private const int AuthoringStartStep = 200;
        private const double AuthoringSettleSeconds = 0.75;

        private UnitEntityData _combatUnit;
        private int _combatRunsBefore;
        private int _combatDispatchRefusalsBefore;
        private int _combatAvailableBefore;
        private double _combatReleasedAt = -1;
        private bool _combatEditorSeen;

        private string _authoringRoutine;
        private string _authoringSource;
        private string _authoringCaster;
        private string _authoringRow;
        private string _authoringTargetA;
        private string _authoringTargetB;
        private string _authoringOtherCaster;
        private string _authoringFirstId;
        private string _authoringSecondId;
        private HashSet<string> _authoringIdsBefore;
        private bool _authoringProviderChanged;

        private bool PressColdMoon()
        {
            CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "physical-cf-moon-before.png"));
            _physicalRunsBeforeMoon = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
            _physicalMoonEditorSeen = false;
            return RequestPhysical("cf-moon", "click",
                BuffPlannerUiRoot.HudButtonCenterForRuntime("long"), null, 102);
        }

        private static string NormalizedMoonExpectation(string value)
        {
            return value == "select" || value == "combat" || value == "authoring" || value == "removal"
                ? value : "cast";
        }

        private void HoldCombat()
        {
            if (_combatUnit == null) return;
            if (!_combatUnit.IsInCombat) _combatUnit.JoinCombat();
            if (_combatUnit.Group != null) _combatUnit.Group.LeaveCombatTimer = 0f;
            Kingmaker.Game.Instance.Player.UpdateIsInCombat();
            _physicalRecord.CombatHeldUpdates++;
        }

        private int DispatchRefusalCount()
        {
            CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            return session == null ? 0 : session.DispatchRefusalsForRuntime.Count;
        }

        private bool UpdateCombatPress(CastingWorkspaceScreenView view, double settled)
        {
            if (_physicalStep == CombatStartStep)
            {
                Kingmaker.Player player = Kingmaker.Game.Instance.Player;
                _combatUnit = player.Party.FirstOrDefault(unit => unit != null && unit.IsInGame &&
                    !unit.Descriptor.State.IsDead);
                if (_combatUnit == null) return FinishPhysical("combat:no-party-member");
                _physicalRecord.CombatUnitId = _combatUnit.UniqueId;
                _physicalRecord.CombatInCombatBefore = player.IsInCombat;
                HoldCombat();
                if (!player.IsInCombat) return FinishPhysical("combat:state-not-entered");
                CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
                if (inputs == null) return FinishPhysical("combat:no-inputs");
                _combatAvailableBefore = SeedCastsAvailable(inputs, _physicalSeedLong);
                _combatRunsBefore = BuffPlannerUiRoot.CastingRunsStartedForRuntime;
                _combatDispatchRefusalsBefore = DispatchRefusalCount();
                _combatEditorSeen = false;
                _physicalRecord.AddNote("combat:held;unit=" + _combatUnit.UniqueId +
                    ";stimulus=UnitEntityData.JoinCombat+LeaveCombatTimer=0;enemy=none");
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "physical-combat-before.png"));
                return RequestPhysical("cf-moon-combat", "click",
                    BuffPlannerUiRoot.HudButtonCenterForRuntime("long"), null, CombatStartStep + 1);
            }
            if (_physicalStep == CombatStartStep + 1)
            {
                // The state is held on every update until the press is read.
                HoldCombat();
                if (BuffPlannerUiRoot.IsCastingWorkspaceOpen) _combatEditorSeen = true;
                if (settled < 3) return false;
                Kingmaker.Player player = Kingmaker.Game.Instance.Player;
                _physicalRecord.CombatAtPress = player.IsInCombat;
                _physicalRecord.CombatRunStarted = BuffPlannerUiRoot.CastingRunsStartedForRuntime > _combatRunsBefore;
                _physicalRecord.CombatEditorOpened = _combatEditorSeen || BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                _physicalRecord.CombatDispatchRefusals = DispatchRefusalCount() - _combatDispatchRefusalsBefore;
                QuickExecutionResult refused = BuffPlannerUiRoot.QuickResultForRuntime("long");
                _physicalRecord.CombatRefusal = refused == null ? "no-result"
                    : refused.Disposition + ":" + refused.Message;
                // rc4 review finding 2: the Classic route, in the same held
                // combat, before the resources and effects are read again.
                HoldCombat();
                ClassicCombatProbe classic = BuffPlannerUiRoot.ClassicCombatProbeForRuntime("long");
                if (classic != null)
                {
                    _physicalRecord.ClassicCombatRefusal = classic.Refusal;
                    _physicalRecord.ClassicCombatYielded = classic.Yielded;
                    _physicalRecord.ClassicCombatRefreshes = classic.Refreshes;
                    _physicalRecord.ClassicCombatPreviews = classic.Previews;
                    _physicalRecord.ClassicCombatReportChanged = classic.ReportChanged;
                    _physicalRecord.ClassicCombatExecuting = classic.ExecutingAfter;
                    _physicalRecord.ClassicCombatProfileUnchanged = classic.ProfileUnchanged;
                }
                else _physicalRecord.ClassicCombatRefusal = "no-classic-session";
                CastingWorkspaceInputs after = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
                if (after != null)
                {
                    _physicalRecord.CombatAvailability = _combatAvailableBefore + ">" +
                        SeedCastsAvailable(after, _physicalSeedLong);
                    _physicalRecord.CombatEffectAfter = SeedEffectActive(after, _physicalSeedLong);
                }
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "physical-combat-refused.png"));
                // Release: the unit leaves the state it was put into.
                if (_combatUnit != null && _combatUnit.IsInCombat) _combatUnit.LeaveCombat();
                player.UpdateIsInCombat();
                _combatReleasedAt = settled;
                _physicalStep = CombatStartStep + 2;
                return false;
            }
            if (_physicalStep == CombatStartStep + 2)
            {
                Kingmaker.Player player = Kingmaker.Game.Instance.Player;
                player.UpdateIsInCombat();
                if (player.IsInCombat && settled - _combatReleasedAt < 10) return false;
                _physicalRecord.CombatClearedBeforeMoon = !player.IsInCombat;
                if (player.IsInCombat) return FinishPhysical("combat:not-cleared");
                // The ordinary selection press, now out of combat.
                return PressColdMoon();
            }
            return false;
        }

        // A screen point the OS pointer can click, or null.
        private static Vector2? OnScreen(CastingWorkspaceScreenView view, string part)
        {
            Vector2? point = view == null ? null : view.ScreenPointForRuntime(part);
            if (point == null) return null;
            Vector2 value = point.Value;
            return value.x < 0 || value.y < 0 || value.x > Screen.width || value.y > Screen.height
                ? (Vector2?)null : value;
        }

        private static PlannedCasting CastingById(CastingWorkspaceSession session, string castingId)
        {
            return session.Document.Castings.FirstOrDefault(value =>
                value != null && string.Equals(value.CastingId, castingId, StringComparison.Ordinal));
        }

        // A source, caster and source row of the shown routine with two legal
        // recipients that no casting of the buff in this routine reaches,
        // preferring the cold seed's own source. Probing selects the buff and
        // the caster only (focus, never a document mutation).
        private bool ChooseAuthoringGesture(CastingWorkspaceSession session, CastingWorkspaceInputs inputs)
        {
            List<string> order = session.BuildGraph(inputs).Catalogue.Select(entry => entry.SourceId).ToList();
            if (_physicalSeedLong != null && order.Remove(_physicalSeedLong.SourceId))
                order.Insert(0, _physicalSeedLong.SourceId);
            foreach (string sourceId in order.Take(80))
            {
                session.SelectGraphBuff(sourceId, inputs);
                CastingGraphView graph = session.BuildGraph(inputs);
                foreach (string caster in GraphCapableCasters(graph))
                {
                    session.SelectGraphCaster(caster, inputs);
                    int row = UsableSourceRow(session, inputs, caster);
                    if (row < 0) continue;
                    session.ClickGraphSource(session.BuildGraph(inputs).CasterById(caster).Sources[row].ProviderKey,
                        inputs);
                    CastingGraphView chosen = session.BuildGraph(inputs);
                    List<string> free = chosen.Targets.Where(target =>
                            target.Legality == CastingGraphTargetLegality.Legal && target.CastingIds.Count == 0)
                        .OrderBy(target => string.Equals(target.UnitId, caster, StringComparison.Ordinal) ? 1 : 0)
                        .ThenBy(target => target.UnitId, StringComparer.Ordinal)
                        .Select(target => target.UnitId).ToList();
                    if (free.Count < 2) continue;
                    _authoringSource = sourceId;
                    _authoringCaster = caster;
                    _authoringRow = row.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    _authoringTargetA = free[0];
                    _authoringTargetB = free[1];
                    _authoringOtherCaster = GraphCapableCasters(graph).FirstOrDefault(other =>
                        !string.Equals(other, caster, StringComparison.Ordinal));
                    return true;
                }
            }
            return false;
        }

        private bool UpdatePhysicalAuthoring(CastingWorkspaceScreenView view, double settled)
        {
            if (view == null) return FinishPhysical("authoring:workspace-closed:step" + _physicalStep);
            CastingWorkspaceSession session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
            CastingWorkspaceInputs inputs = BuffPlannerUiRoot.CastingWorkspaceFreshInputsForRuntime();
            if (session == null || inputs == null) return FinishPhysical("authoring:no-session-or-inputs");
            if (_physicalStep > AuthoringStartStep && settled < AuthoringSettleSeconds) return false;
            int step = _physicalStep - AuthoringStartStep;
            if (step == 0)
            {
                _authoringRoutine = session.SelectedRoutineId;
                string shown = session.SelectedSourceId;
                if (!ChooseAuthoringGesture(session, inputs))
                    return FinishPhysical("authoring:no-buff-with-two-free-recipients");
                // The probe chose; the gesture is the player's. Leave the
                // probe's draft and show the buff the player had.
                session.ClearGraphFocus();
                if (!string.IsNullOrEmpty(shown)) session.SelectGraphBuff(shown, inputs);
                RefreshWorkspaceForRuntime();
                _physicalRecord.AuthoringRoutine = _authoringRoutine;
                _physicalRecord.AuthoringSource = _authoringSource;
                _physicalRecord.AuthoringCaster = _authoringCaster;
                _physicalRecord.AuthoringTargets = _authoringTargetA + "," + _authoringTargetB;
                _physicalRecord.AuthoringCountBefore = session.Document.Castings.Count;
                Vector2? tile = OnScreen(view, "tile:" + _authoringSource);
                if (tile != null)
                    return RequestPhysical("auth-tile", "click", tile.Value, null, AuthoringStartStep + 1);
                // Choosing the buff is browsing, not the WP3 gesture under
                // test; a tile scrolled out of the catalogue is chosen the
                // way its search would, and that is recorded.
                session.SelectGraphBuff(_authoringSource, inputs);
                RefreshWorkspaceForRuntime();
                _physicalRecord.AddNote("authoring:tile-off-screen:buff-selected-directly");
                _physicalStep = AuthoringStartStep + 1;
                return false;
            }
            if (step == 1)
            {
                _physicalRecord.AuthoringBuffSelected = string.Equals(session.SelectedSourceId, _authoringSource,
                    StringComparison.Ordinal);
                Vector2? caster = OnScreen(view, "caster:" + _authoringCaster);
                if (caster == null) return FinishPhysical("authoring:caster-not-on-screen");
                return RequestPhysical("auth-caster", "click", caster.Value, null, AuthoringStartStep + 2);
            }
            if (step == 2)
            {
                Vector2? row = OnScreen(view, "source:" + _authoringCaster + ":" + _authoringRow);
                if (row == null) return FinishPhysical("authoring:source-row-not-on-screen");
                return RequestPhysical("auth-source", "click", row.Value, null, AuthoringStartStep + 3);
            }
            if (step == 3)
            {
                _authoringIdsBefore = new HashSet<string>(session.Document.Castings
                    .Where(value => value != null).Select(value => value.CastingId), StringComparer.Ordinal);
                Vector2? target = OnScreen(view, "target:" + _authoringTargetA);
                if (target == null) return FinishPhysical("authoring:target-a-not-on-screen");
                return RequestPhysical("auth-add", "click", target.Value, null, AuthoringStartStep + 4);
            }
            if (step == 4)
            {
                List<PlannedCasting> added = session.Document.Castings
                    .Where(value => value != null && !_authoringIdsBefore.Contains(value.CastingId)).ToList();
                PlannedCasting created = added.Count == 1 ? added[0] : null;
                _authoringFirstId = created == null ? null : created.CastingId;
                _physicalRecord.AuthoringAdded = created != null &&
                    string.Equals(created.SourceId, _authoringSource, StringComparison.Ordinal) &&
                    string.Equals(created.CasterUnitId, _authoringCaster, StringComparison.Ordinal) &&
                    string.Equals(created.DirectTargetUnitId, _authoringTargetA, StringComparison.Ordinal) &&
                    string.Equals(session.EditingFocusCastingId, created.CastingId, StringComparison.Ordinal);
                _physicalRecord.AddNote("authoring:add=" + (created == null ? "count+" + added.Count
                    : created.CastingId + "/" + created.CasterUnitId + ">" + created.DirectTargetUnitId) +
                    ";focus=" + (session.EditingFocusCastingId ?? "none"));
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "physical-authoring-added.png"));
                Vector2? target = OnScreen(view, "target:" + _authoringTargetA);
                if (target == null) return FinishPhysical("authoring:target-a-not-on-screen:remove");
                return RequestPhysical("auth-remove", "click", target.Value, null, AuthoringStartStep + 5);
            }
            if (step == 5)
            {
                _physicalRecord.AuthoringRemoved = _authoringFirstId != null &&
                    CastingById(session, _authoringFirstId) == null &&
                    session.Document.Castings.Count == _authoringIdsBefore.Count &&
                    session.EditingFocusCastingId == null;
                Vector2? target = OnScreen(view, "target:" + _authoringTargetA);
                if (target == null) return FinishPhysical("authoring:target-a-not-on-screen:readd");
                return RequestPhysical("auth-readd", "click", target.Value, null, AuthoringStartStep + 6);
            }
            if (step == 6)
            {
                List<PlannedCasting> added = session.Document.Castings
                    .Where(value => value != null && !_authoringIdsBefore.Contains(value.CastingId)).ToList();
                PlannedCasting created = added.Count == 1 ? added[0] : null;
                _authoringSecondId = created == null ? null : created.CastingId;
                _physicalRecord.AuthoringReadded = created != null &&
                    string.Equals(created.DirectTargetUnitId, _authoringTargetA, StringComparison.Ordinal) &&
                    string.Equals(session.EditingFocusCastingId, created.CastingId, StringComparison.Ordinal);
                Vector2? target = OnScreen(view, "target:" + _authoringTargetB);
                if (target == null) return FinishPhysical("authoring:target-b-not-on-screen");
                return RequestPhysical("auth-retarget", "click", target.Value, null, AuthoringStartStep + 7);
            }
            if (step == 7)
            {
                PlannedCasting moved = _authoringSecondId == null ? null : CastingById(session, _authoringSecondId);
                _physicalRecord.AuthoringRetargeted = moved != null &&
                    string.Equals(moved.DirectTargetUnitId, _authoringTargetB, StringComparison.Ordinal) &&
                    string.Equals(moved.CasterUnitId, _authoringCaster, StringComparison.Ordinal) &&
                    session.Document.Castings.Count == _authoringIdsBefore.Count + 1;
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "physical-authoring-retargeted.png"));
                // Provider change through the graph: another caster of the
                // buff, when one is on screen (a caster with several ways to
                // cast it asks for the exact row, which then is clicked).
                Vector2? other = _authoringOtherCaster == null ? null
                    : OnScreen(view, "caster:" + _authoringOtherCaster);
                if (other != null)
                    return RequestPhysical("auth-provider-caster", "click", other.Value, null, AuthoringStartStep + 8);
                _physicalRecord.AddNote("authoring:provider-change:no-second-caster-on-screen");
                _physicalStep = AuthoringStartStep + 10;
                return false;
            }
            if (step == 8)
            {
                PlannedCasting focused = CastingById(session, _authoringSecondId);
                if (focused != null &&
                    string.Equals(focused.CasterUnitId, _authoringOtherCaster, StringComparison.Ordinal))
                {
                    _physicalStep = AuthoringStartStep + 9;
                    return false;
                }
                int row = UsableSourceRow(session, inputs, _authoringOtherCaster);
                Vector2? rowPoint = row < 0 ? null : OnScreen(view, "source:" + _authoringOtherCaster + ":" +
                    row.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (rowPoint == null)
                {
                    _physicalRecord.AddNote("authoring:provider-change:other-caster-rows-not-shown");
                    _physicalStep = AuthoringStartStep + 10;
                    return false;
                }
                return RequestPhysical("auth-provider-source", "click", rowPoint.Value, null,
                    AuthoringStartStep + 9);
            }
            if (step == 9)
            {
                PlannedCasting changed = CastingById(session, _authoringSecondId);
                _authoringProviderChanged = changed != null &&
                    string.Equals(changed.CasterUnitId, _authoringOtherCaster, StringComparison.Ordinal);
                _physicalRecord.AuthoringProviderChanged = _authoringProviderChanged;
                _physicalRecord.AuthoringProviderKeptTarget = changed != null &&
                    string.Equals(changed.DirectTargetUnitId, _authoringTargetB, StringComparison.Ordinal);
                _physicalRecord.AddNote("authoring:provider=" + (changed == null ? "missing"
                    : changed.CasterUnitId + ">" + changed.DirectTargetUnitId));
                _physicalStep = AuthoringStartStep + 10;
                return false;
            }
            if (step == 10)
            {
                Vector2? undo = OnScreen(view, "undo");
                if (undo == null) return FinishPhysical("authoring:undo-not-on-screen");
                // With a provider change there are two edits to undo.
                return RequestPhysical("auth-undo", "click", undo.Value, null,
                    _authoringProviderChanged ? AuthoringStartStep + 11 : AuthoringStartStep + 12);
            }
            if (step == 11)
            {
                PlannedCasting undone = CastingById(session, _authoringSecondId);
                _physicalRecord.AuthoringProviderUndone = undone != null &&
                    string.Equals(undone.CasterUnitId, _authoringCaster, StringComparison.Ordinal) &&
                    string.Equals(undone.DirectTargetUnitId, _authoringTargetB, StringComparison.Ordinal);
                Vector2? undo = OnScreen(view, "undo");
                if (undo == null) return FinishPhysical("authoring:undo-not-on-screen:2");
                return RequestPhysical("auth-undo-retarget", "click", undo.Value, null, AuthoringStartStep + 12);
            }
            if (step == 12)
            {
                PlannedCasting restored = CastingById(session, _authoringSecondId);
                _physicalRecord.AuthoringUndoRestored = restored != null &&
                    string.Equals(restored.DirectTargetUnitId, _authoringTargetA, StringComparison.Ordinal) &&
                    string.Equals(restored.CasterUnitId, _authoringCaster, StringComparison.Ordinal);
                CaptureScreenshot(Path.Combine(_request.EvidenceDirectory, "physical-authoring-undone.png"));
                Vector2? chip = _authoringSecondId == null ? null : OnScreen(view, "chip:" + _authoringSecondId);
                if (chip == null) return FinishPhysical("authoring:card-not-on-screen");
                return RequestPhysical("auth-focus", "click", chip.Value, null, AuthoringStartStep + 13);
            }
            if (step == 13)
            {
                _physicalRecord.AuthoringFocusedBeforeEscape = _authoringSecondId != null &&
                    string.Equals(session.EditingFocusCastingId, _authoringSecondId, StringComparison.Ordinal);
                return RequestPhysical("auth-escape-focus", "key-escape", Vector2.zero, null, AuthoringStartStep + 14);
            }
            if (step == 14)
            {
                _physicalRecord.AuthoringEscapeClearedFocus = session.EditingFocusCastingId == null;
                _physicalRecord.AuthoringOpenAfterFirstEscape = BuffPlannerUiRoot.IsCastingWorkspaceOpen;
                _physicalRecord.AuthoringDurable = !session.IsDirty;
                _physicalRecord.AuthoringCountAfter = session.Document.Castings.Count;
                // The closing Escape and the shared close judgement (step 7).
                return RequestPhysical("cf-escape-close", "key-escape", Vector2.zero, null, 7);
            }
            return false;
        }
    }
}
