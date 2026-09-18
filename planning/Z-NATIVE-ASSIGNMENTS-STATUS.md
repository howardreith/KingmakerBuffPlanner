# Z Native Assignments — Checkpoint Status

Single source of truth for mission progress. Linked from `AUTONOMOUS-RESUME.md`.
Mission: `planning/Z-NATIVE-ASSIGNMENTS-MISSION.md`.

## Baseline intake — 2026-09-18

- Implementation base: branch `codex/kingmaker-buff-planner-z-native-assignments`,
  created from `codex/kingmaker-buff-planner-instant-share-routing-diagnosis` HEAD
  `164737efd19e3c9db1efe3504813ef36d445b1fc` (pushed, clean).
  `main`/`origin/main` are at the mission reference anchor
  `fd0e6dc1c32dfc929a56dbc575163e641b150746` (version 0.0.19); the diagnosis
  branch adds three pushed commits (Share cast diagnostics, production bridge
  probe, records) that this branch preserves rather than reverts.
- Styling reference `howardreith/KingmakerDiceRoller` cloned at
  `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerDiceRoller`; local HEAD
  `f185603` equals the mission reference anchor exactly.
- Baseline mechanical gates (before any edit):
  `Test-SourceOnly.ps1` — source validation 42/42, protocol 150/150,
  runtime harness filesystem 8/8, package 4/4, deployment WhatIf 5/5,
  source-only 1/1. All PASS.
- Game install: `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker`;
  `Mods\KingmakerBuffPlanner` present (0.0.19). Kingmaker not running.
  Dice Roller is installed as a sibling mod (read-only reference; do not modify).
- **Runtime lane blocker (BLOCKED)**: the authorized
  `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` save pair is absent.
  The current save list contains only `Manual_298_KMG_AUTOMATION_BASELINE.zks`
  and `Manual_299_KMG_AUTOMATION_WORKING.zks`, which belong to another product
  and are protected. Per mission rules the live game-launch lanes
  (rendered layout measurements, live spellbook handoff, live visual matrix)
  are recorded BLOCKED; no substitute save is used. Mechanical, assembly, and
  build/package lanes continue.

## Implementation map (Checkpoint A intake)

Architecture follows the AGENTS.md separation. Key production files for this
mission (all under `src/KingmakerBuffPlanner/`):

- UI: `UI/KingmakerUiFactory.cs` (shared control factory + `PlannerUiTheme`
  with primitive native sprite path resolution), `UI/PlannerViews.cs`
  (BuffGrid virtualized grid, `PlannerEnhancementChooserView`,
  `PlannerCasterPolicyChooserView`, casting/settings panels),
  `UI/BuffPlannerScreenView.cs` (screen layout, `PlannerDescriptionModal`),
  `UI/BuffPlannerUiRoot.cs`, `UI/BuffPlannerScreenController.cs`,
  `UI/PlannerUiSession.cs`, `UI/PlannerSetupModel.cs`,
  `UI/PlannerPresentationModels.cs` (view models + layout contracts),
  `UI/PlannerScreenViewModel.cs`, HUD in `UI/BuffPlannerHudButtonController.cs`.
- Planning: `Planning/RoutinePlanService.cs` (request compilation),
  `Planning/CastPlanner.cs` (allocation), `Planning/ResourceLedger.cs`,
  `Domain/Planning/PlanningModels.cs` (requests/steps),
  `Domain/Planning/CastEnhancements.cs`, `Domain/Planning/EffectiveTargeting.cs`.
- Persistence: `Persistence/ProfileModels.cs` (schema + validation),
  `Persistence/ProfileRepository.cs` (atomic load/save).
- Execution: `Execution/{AnimatedCastExecutor,InstantCastExecutor,HybridCastExecutor}.cs`,
  `Execution/ExecutionModels.cs`; adapters in `GameAdapters/`.
- Enhancement targeting: `GameAdapters/KingmakerCastEnhancementAdapter.cs`,
  `GameAdapters/KingmakerShareTargetingModifier.cs`,
  `Compatibility/BrownFurShareTransmutationCompatibility.cs`.
- Tests: single custom runner `tests/KingmakerBuffPlanner.Tests/Program.cs`
  (no Unity player construction; deterministic domain/protocol/persistence
  coverage with optional installed-assembly resolution via `KBP_TEST_GAME_PATH`).
- Scroll factory consumers (audit scope for Checkpoint A):
  1. `BuffGrid` (`PlannerViews.cs:383`) — destroys the shared VerticalLayoutGroup
     and self-measures content height (`SetSizeWithCurrentAnchors`); virtualized.
  2. `PlannerDescriptionModal` (`BuffPlannerScreenView.cs:897`) — owns content
     height through `ContentSizeFitter.PreferredSize` on content and body text.
  3. `PlannerEnhancementChooserView` (`PlannerViews.cs:801`) — **no content
     height owner**; rows are fixed 68f `LayoutElement`s under the shared
     VerticalLayoutGroup; no visible scrollbar. Suspected overflow defect.
  4. `PlannerCasterPolicyChooserView` (`PlannerViews.cs:940`) — same contract;
     party-sized row counts (6–10) currently fit, but shares the defect.

## Checkpoint A — enhancement chooser overflow repair

Status: IN PROGRESS (see journal + resume for the completed record).

## Checkpoint B — native theme foundation

Status: NOT STARTED.

## Checkpoint C — assignment model, identities, migration, planner

Status: NOT STARTED.

## Checkpoint D — assignment editor and resource UX

Status: NOT STARTED.

## Checkpoint E — spellbook lifecycle integration

Status: NOT STARTED.

## Checkpoint F — full reskin, regression qualification, candidate

Status: NOT STARTED.
