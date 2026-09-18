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

Status: SOURCE/BUILD COMPLETE — live rendered gate BLOCKED (fixture absent).

- Root cause (deterministic): both modal choosers consumed
  `KingmakerUiFactory.CreateScrollView` without any content-height owner. The
  shared `VerticalLayoutGroup` stacks rows but never resizes the content rect,
  so `content.sizeDelta` stayed zero and the overflowing rows were laid out
  below the masked viewport with no bounded scroll range and no visible
  scrollbar. `BuffGrid` (explicit measured sizing) and `PlannerDescriptionModal`
  (`ContentSizeFitter.PreferredSize`) each already own their height; the two
  choosers owned nothing.
- Repair (commit follows this record): new pure
  `ChooserScrollLayoutContract` (`UI/PlannerPresentationModels.cs`) computing
  content height (padding 4 + rows + spacing 4), max/clamped scroll offsets,
  selected-row reveal offset, and scrollbar handle ratio; both choosers now
  size the content rect explicitly (single owner: the view), clamp/preserve
  the offset across refresh rebuilds (toggle → `RefreshAll` → reopen keeps
  `activeSelf` true, so refresh is detected from activation state), reveal
  the first selected row on fresh open, and drive a new optional scrollbar.
- Factory audit (shared consumers of `CreateScrollView`):
  1. `BuffGrid` — unaffected; additive optional parameter defaults to no
     scrollbar; it already destroys the shared layout group and self-measures.
  2. `PlannerDescriptionModal` — unaffected; keeps its ContentSizeFitter
     owners; no scrollbar requested.
  3. `PlannerEnhancementChooserView` — repaired (row height 68f contract).
  4. `PlannerCasterPolicyChooserView` — same defect, repaired identically
     (row height = `CastingPanelLayoutContract.MinimumCasterPolicyRowHeight`);
     every policy action rebuilds rows through `RefreshCasterPolicyChooser`,
     and the offset now survives those rebuilds.
- Scrollbar wiring: `CreateScrollView(name, parent, theme, out content,
  scrollbarWidth=0)` gains an optional right-gutter `Scrollbar`
  (BottomToTop, Permanent visibility); viewport right inset widens only when
  the scrollbar is requested. Value is synced by the owning `ScrollRect`;
  handle size is set by the chooser after content sizing (one driver each).
- Evidence (deterministic): `chooser-scroll-layout-owns-content-bounds`
  covers 0/1/2/30/100-row content heights, bounded offsets, shrink clamping,
  selected-row/last-row reveal bounds, no-selection clamping, and handle
  ratio including the minimum-draggable floor. Suite:
  source 42/42, protocol 151/151, harness 8/8, package 4/4, WhatIf 5/5,
  Release build PASS (DLL sha256
  `681a0aeb7442aa55c504dcc8579da730125ea910b7fe3e5befd79b19836720b5`).
- BLOCKED lane: rendered content/viewport measurements, wheel/scrollbar reach
  evidence, and world-input-leak checks require the guarded live campaign
  harness; the `KBP_AUTOMATION` save pair is absent. Structural input
  isolation is unchanged (full-screen blocker, Escape close, fullscreen input
  lease), and no claim of live visual acceptance is made.

## Checkpoint B — native theme foundation

Status: SOURCE/BUILD COMPLETE — UNQUALIFIED LIVE (fixture absent; game session
detected running during qualification, not touched).

- New Buff-Planner-owned capability layer (patterned after Dice Roller's
  separation, no runtime dependency on it):
  - `UI/NativeThemeModel.cs` — `NativeThemeCapability` (Paper, Buttons,
    ButtonText, Body, Input, Scrollbar, Ornament, Sound — resolved
    independently), `INativeThemeSource`, `NativeThemeResource`,
    `NativeThemeResolution` with per-capability accept/reject, stale discard,
    and a summary that records locator provenance.
  - `UI/NativeThemeDonorLookup.cs` — bounded lookup (depth 32, 512 children,
    2048 scan nodes) with ambiguity rejection.
  - `UI/NativeThemeResolver.cs` — campaign locators. ProvenPath entries are
    StaticCanvas paths live-qualified through 0.0.19 (BookBackground paper,
    Button_LevelUp, Party/Character/Highlight ornament). BoundedScan entries
    structurally discover donors under proven native screens
    (CharacterScreen/Inventory/SpellBook) because no verified literal path is
    recorded; the live inventory lane can promote them without consumer
    changes.
  - `UI/NativeThemeRecovery.cs` — bounded re-resolution (≤3 attempts/owner)
    and `NativeThemeBindings` (apply-once per donor identity; failed apply →
    readable parchment fallback for that capability only).
  - `UI/ControlCaptionFit.cs` — pure grow-from-design-floor caption policy;
    `KingmakerUiFactory.FitButtonToCaption` applies it at rebuild boundaries
    only (chooser CLOSE/RESET buttons).
  - `UI/PlannerNativeTheme.cs` — Unity adapter. Structural donor validation
    (component presence, sliced/borderless-safe images, fonts, scrollbar
    handle) — exact sprite-name/border/pPu contracts deliberately NOT asserted
    until the live inventory proves them; complete-state button borrowing
    uses SpriteSwap only when the donor supplies all four states, otherwise a
    documented partial borrow keeps the tint transition.
  - `UI/PlannerNativeThemeSurface.cs` — attaches to the planner root; one
    bounded pass applies buttons/fonts/paper/input/scrollbar artwork to owned
    controls, adds the single native click-sound route
    (`UICommon.UISound.Play(UISoundType.ButtonClick)` — distinct from the
    existing CharacterScreenOpen route, so no duplicate open/click cues),
    re-covers rebuilt chooser rows via `ApplyTo`, and re-resolves on
    OnEnable within recovery bounds. Theme evidence is appended to
    `ThemeResolution` diagnostics (`native[...]` summary).
- Representative surfaces themed: all owned buttons under the planner root
  (including rebuilt chooser rows), paper frames, search input, chooser
  scrollbars, native click sound. Full-surface reskin continues in F.
- Evidence: protocol 153/153 including
  `native-theme-resolves-and-falls-back-per-capability` (full/partial/
  ambiguous/stale-node/stale-component resolution, bounded recovery, binding
  apply/fallback) and `control-caption-fit-grows-only-from-design-floor`;
  source 42/42; package 4/4; Release build PASS (DLL sha256
  `d7c2be619544df61dbd626d8bd3b730ca49482cac9114fa4920e91668f712874`).
- BLOCKED lanes: deployment WhatIf purity and source-only suite final gate
  were interrupted when a Kingmaker process (PID 15696) started mid-run; the
  guard behaved correctly and nothing was touched. Live donor inventory,
  rendered state checks, and visual acceptance remain BLOCKED on the absent
  `KBP_AUTOMATION` save pair. The theme is explicitly UNQUALIFIED-LIVE;
  partial donors must not be reported as verified native artwork.

## Checkpoint C — assignment model, identities, migration, planner

Status: NOT STARTED.

## Checkpoint D — assignment editor and resource UX

Status: NOT STARTED.

## Checkpoint E — spellbook lifecycle integration

Status: NOT STARTED.

## Checkpoint F — full reskin, regression qualification, candidate

Status: NOT STARTED.
