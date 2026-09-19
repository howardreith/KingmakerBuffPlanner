# Z Native Assignments — Checkpoint Status

Single source of truth for mission progress. Linked from `AUTONOMOUS-RESUME.md`.
Mission: `planning/Z-NATIVE-ASSIGNMENTS-MISSION.md`.

## RC2 product recovery — 2026-09-19 (CURRENT)

- Trigger: owner acceptance of v0.1.1-rc1 failed (native appearance
  absent, spellbook entry absent, chooser implied unlimited rod
  allocation, numeric metamagic labels). Live Mods folder holds 0.0.19 —
  the owner rolled back after testing.
- Source findings confirmed at `5da102f` and repaired (fix commit
  `ab155c2`; version bump `5d94502`; install-guard prerelease CLR
  derivation `3625534`):
  - P1 `PlannerNativeThemeSurface.Attach` resolved native donors from its
    own overlay root. Donor lookup is now StaticCanvas-scoped
    (`PlannerNativeTheme.Resolve(nativeLookupRoot)`); `DiscardStale`
    owner semantics follow the native scope.
  - P2 paper targeted three direct child names; nested modal frames never
    matched. Owned surfaces register explicitly
    (`RegisterPaperSurface`), `ApplyPaper` disables fallback outlines and
    neutralizes factory tints (status colors preserved), and `OnEnable`
    adds a bounded missing-capability retry next to the stale retry.
  - P3 spellbook entry: `SpellbookWindowLocator` (exact path - bounded
    tolerant scan - refuse ambiguity) drives attachment; corner-anchored
    design-sized caption-fitted button; theme resolved against the live
    canvas; screen-rect logged on attach.
  - P4 `EnhancementBudgetModel` exposes the plan's
    `ResourcePoolAllocation` lines (`enhancement:<pool>`): native now /
    requested / allocated / unmet / projected / affected casts; chooser
    budget block + per-row notes + this-assignment coverage + card
    warning + plan-summary shortage line; header caption
    `Assignments & Resources`; selected-spell `Edit Assignments` action.
  - P5 `CastEnhancementNaming` + `CallOfTheWildMetamagicNames`
    (fail-soft reflection over `MetamagicFeats+MetamagicExtender`;
    offline-verified constants) + item-derived fallback;
    `PlannerSetupModel.EffectName` sanitizes digit-carrying names.
- New regressions (6): native-theme-lookup-requires-native-root-not-owned-overlay;
  spellbook-window-locator-is-exact-then-tolerant-and-refuses-ambiguity;
  chooser-budget-derives-from-authoritative-plan (4- and 9-request rod);
  chooser-budget-follows-reordered-assignment-priority;
  metamagic-labels-never-show-raw-masks;
  installed-call-of-the-wild-metamagic-name-contract-is-exact.
- Gates: source 42/42; protocol 178/178; harness 27/27; package 4/4;
  WhatIf 5/5; publisher gate 3/3; deterministic Release build 2/2.
  Candidate `KingmakerBuffPlanner-0.1.1-rc2.zip` SHA-256
  `f6aa4de188b08392acecdb04759ca0cebf2ad329732a5d8a014e0ea863981a00`.
- Runtime lane: guarded install + `live-ui-bootstrap` attempted and
  BLOCKED by an owner-controlled Kingmaker process (PID 1896); guards
  refused with zero mutations (first install attempt also rolled back
  exactly after the prerelease CLR-version guard defect, then fixed).
  Rendered appearance / visible spellbook button / in-game budgets:
  source-verified only, disclosed in the rc2 notes with a five-minute
  owner check. A7 actual charge spending: untested lane.
- Offline contract evidence: game `Kingmaker.UnitLogic.Abilities.Metamagic`
  = {Empower 1, Maximize 2, Quicken 4, Extend 8, Heighten 16, Reach 32};
  CotW `MetamagicExtender` constants cover the owner's observed numerics
  (268435456 Persistent, 524288 Piercing, 33554432 Selective, 8192
  ThrenodicSpell).

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

Status: SOURCE/BUILD COMPLETE — live lanes BLOCKED (fixture absent).

- Schema 5 (`Persistence/ProfileModels.cs`): `SourceAssignmentProfile` keeps
  source-level policy (ability, existing-effect policy, ignored markers) and
  nests `CastingAssignmentProfile` children (`assignmentId` stable identity,
  routine-wide `order`, `casterUnitId`/`spellbookGuid`/`providerKey` pins
  (null = Automatic), ordered `targetUnitIds`, `enhancements` with
  required-by-default policy). Source-level target/enhancement summaries are
  derived read-only ([JsonIgnore]) from children — one writable copy.
- Migration v4→v5 (`Persistence/ProfileRepository.cs`): each source becomes
  exactly one legacy-equivalent Automatic child (`legacy-<sourceId>`, targets,
  required enhancement selections), with the legacy source-ID allocation order
  made explicit as child order. The exact pre-migration original is archived
  once as `UserSettings/kbp-pre-schema-<hash>.orig` outside the rotating .bak
  chain (short name keeps MAX_PATH safe; directory ensured). Repository
  validation enforces unique assignment IDs, unique routine-wide order values,
  unique child targets, and unique child enhancement IDs; duplicate enhancement
  requests dedupe deterministically at compile time.
- Domain/planning (`Domain/Planning/PlanningModels.cs`, `Planning/CastPlanner.cs`,
  `Planning/RoutinePlanService.cs`): `BuffCastRequest` carries
  `AssignmentId`, `Order`, caster/spellbook/provider pins, ordered targets,
  and `EnhancementRequest` selections (required/optional); `TargetPlanOutcome`,
  `CastStep`, and execution records carry `AssignmentId` (plus omitted
  enhancement IDs on steps/records). Allocation runs in explicit assignment
  order with stable tie-breaks — never source-ID/catalog order. Pins are hard
  pre-ranking constraints with `pin-unresolved` diagnostics (no silent
  fallback). Optional-enhancement policy drops only non-targeting modifiers
  from the end of the request order and revalidates the whole remaining set;
  Share/AffectsTargeting selections are never omissible. One
  `ResourcePoolAllocation` accounting per native/enhancement pool
  (available/requested/allocated/unmet/forecast + assignment traces) is built
  by the planner and consumed by everything downstream; enhancement usage
  pools colliding with native pool keys are diagnosed, never double-spent.
  `PlannerSetupModel` routes the simple workflow through the single Automatic
  child and exposes assignment-level editing (add/split/atomic move/remove
  target/pin/provider/enhancement policy/reorder) plus derived summaries;
  enhancement changes never prune targets (repairable intent instead).
- C-gate proofs (deterministic, `tests/KingmakerBuffPlanner.Tests/Program.cs`):
  - `casting-assignments-route-mixed-casters-exactly` — the canonical
    Leinna/Felix example produces four correctly routed casts with Share only
    on the two configured non-self casts, reservoir accounting traces to
    `cast-3`, and an unavailable pinned caster stays unresolved with a
    pin diagnostic (T01/T02).
  - `assignment-order-and-shortage-allocate-explicitly` — nine enhanced casts
    against three charges report requested 9 / available 3 / allocated 3 /
    unmet 6 / forecast 0 with the first three explicit targets funded; the
    optional policy plans the last six labeled as omissions without changing
    accounting; already-active skips reserve nothing; the one-charge race is
    won by the lower explicit order against catalog order (T04/T05/T07).
  - `profile-migrates-schema-one` — v1/v2/v4 documents (genuine historical
    shapes) migrate to schema 5 preserving targets/enhancements as one
    automatic child, archive the original, preserve legacy multi-source order
    explicitly, and stay idempotent across save/reload (T10).
  - `effective-targeting-is-routine-and-assignment-aware` now proves the
    no-silent-prune contract: a Share-disabled stale target remains configured,
    surfaces unfulfilled, and is cleanly removable (T08).
- Suite: source 42/42, protocol 155/155, harness 8/8, package 4/4, WhatIf
  5/5, Release build PASS (DLL sha256
  `5428911716f13e5690d2a9388e1c61e81c9a83965f9cb891ff54781929cae493`).
- BLOCKED: exact-item rod identity (T03 durable instance binding) cannot be
  proven without the installed-game contract probe; legacy pooled selections
  remain pooled (no guessed exact pins) and identical-rod separation is
  documented as unproven until a live inventory exists.

## Checkpoint D — assignment editor and resource UX

Status: SOURCE/BUILD COMPLETE — rendered/live lanes BLOCKED (fixture absent).

- Partial execution (`UI/AssignmentPresentationModels.cs` +
  `UI/PlannerUiSession.cs` + `UI/BuffPlannerUiContracts.cs`): the pure
  `PartialExecutionGate` separates requested coverage from successful casts.
  Default Apply — planner button and HUD quick-run share the same gate —
  refuses an incomplete routine with counts and unmet reasons; only the
  explicit "APPLY READY CASTS ONLY" button (visible exactly while the preview
  is incomplete) takes the `readyOnlyExplicit` lane, and that lane logs
  requested/planned/unfulfilled/skipped honestly.
- Assignment editor surface (`UI/PlannerViews.cs` `PlannerCastingOrderView`,
  opened by the header "Order" button): numbered child assignments in explicit
  routine-wide order with Earlier/Later controls (model ops from C), caster
  text (Automatic vs pinned + spellbook/provider), target names, enhancement
  selections with optional-policy labels, resolved status (planned counts /
  unmet / pinned-unavailable), per-pool resource lines with
  available/requested/allocated/unmet/forecast plus competing configured
  demand from other routines labeled "(their own runs)", and the read-only
  combined forecast (`RoutineSequenceForecast`: one run per selected routine
  L/I/S toggles, balances carried in the selected order, assumptions stated).
  All content derives from the planner's plan results — the view holds no
  competing calculations. Rows rebuild through the native-theme surface.
- Simple automatic portrait assignment is unchanged (C's Automatic-child
  routing); the casting-order view is additive.
- D-gate proofs: `partial-apply-gate-distinguishes-coverage-from-casts` (T13),
  `casting-order-rows-and-resource-lines-derive-from-plan` (9/3/3/6 line
  wording, row derivation, missing-pin visibility; T08),
  `sequence-forecast-carries-balances-per-selected-routine` (T11 — carried
  balances, independent previews see full native balance, assumption label).
- Suite: source 42/42, protocol 158/158, harness 8/8, package 4/4, WhatIf
  5/5, Release build PASS (DLL sha256
  `100850e84efb40ba2a5957ed45873256e9585b8f18b81b2e6a10255a247cf3e7`).
- BLOCKED: rendered navigation of the 9/3/3/6 fixture, reorder interaction,
  and forecast rendering require the live campaign harness.

## Checkpoint E — spellbook lifecycle integration

Status: SOURCE/BUILD COMPLETE — ALL LIVE SPELLBOOK LANES BLOCKED (fixture
absent; placement/handoff/return not runtime-qualified).

- `UI/SpellbookHandoffStateMachine.cs` (pure): deterministic handoff
  transitions — begin, bounded mode-release wait (90 frames), completed,
  failed-with-reason; a completed handoff can never be rolled back and a
  failed one never opens the planner.
- `UI/BuffPlannerSpellbookEntryController.cs` (Unity): owned BUFF PLANNER
  button attached to the native spellbook window (`ServiceWindow/SpellBook`,
  the same proven StaticCanvas path family), out-of-layout
  (`LayoutElement.ignoreLayout`, mirroring the live-qualified HUD row
  precedent), keyed by window instance identity, discovered on a bounded
  15-tick cadence via a single-path Find (no per-frame global scans). Only
  the owned button is ever created/destroyed. Clicking runs the guarded
  handoff: refuse when the planner is already open; when a full-screen mode
  is active, request closure through the spellbook's OWN close button
  (the planner never seizes the mode), wait the bounded release, then enter
  the existing planner opening lifecycle (input lease, pause, selection).
  Failure/timeout rolls back to a usable spellbook with the button restored.
  Wire-up: `BuffPlannerUiRoot` ticks it beside the existing screen/HUD tick
  and releases it in `ReleaseAll`. HUD/hotkey entry is untouched.
- Proof: `spellbook-handoff-waits-bounded-and-rolls-back` (159/159 protocol).
- BLOCKED and explicitly NOT claimed: rendered placement in verified free
  header/footer space at supported resolutions, duplicate-prevention across
  real spellbook owners, the same-context return trip after
  close-without-casting, Escape/failure/scene-change navigation, and mod
  disable behavior. The native-close affordance lookup (`Close` button name)
  is a structural assumption pending the live inventory lane. The E gate
  ("a button present in the hierarchy is not sufficient acceptance") is
  therefore OPEN.

## Checkpoint F — full reskin, regression qualification, candidate

Status: RECORDS/CANDIDATE COMPLETE — full native reskin and every live lane
remain BLOCKED (fixture absent); regression matrix is the deterministic suite
above plus the open manual checklist.

- Version 0.1.0 chosen from actual repository metadata (0.0.19 published).
- Docs updated: release notes draft, CHANGELOG, README, IMPLEMENTATION-REPORT,
  QUALIFICATION, MANUAL-ACCEPTANCE, journal/resume/blockers, this tracker.
- Candidate (local-only, commit `ac0b91ba44192779624ec2774e94351ac27e8388`):
  package `artifacts/release/0.1.0/KingmakerBuffPlanner-0.1.0.zip` SHA-256
  `0d1a1af312dc768b229c174a43982cdc469ab7746249527bfa237ded5e064b92`; DLL
  SHA-256 `08f48a343fb96163fd539e97a6623de020831efa6827455b43e18d9e05d9b3ea`;
  MVID `86a94f76-7676-48bf-adfb-92b731dbfc92`; deterministic builds 2/2.
- Full reskin note: the B capability layer themes all planner-root buttons,
  paper frames, search input, chooser scrollbars, and rebuilt rows; the
  remaining surfaces intentionally keep the readable parchment style until
  the live inventory promotes the scan donors — propagating unverified
  donor assumptions is explicitly out of scope per the mission.
- No merge, no tag, no guarded push, no publication.


## Continuation mission — 2026-09-18 (checkpoint 1-4 records)

- Checkpoint 1: branch pushed through the guarded helper
  (`f1256b929be1fc6f3de46fed19c750c55a1c80bc` verified on origin); draft PR
  https://github.com/howardreith/KingmakerBuffPlanner/pull/1 opened with the
  implementation gaps and unrun live lanes stated up front. Candidate hash
  `0d1a1af3...` verified against the release manifest; package source
  `ac0b91b` differs from handoff HEAD `f1256b9` only by documentation-only
  commits (verified: 28 lines across two .md files), so the executable
  source tree is identical.
- Checkpoint 2: the audit confirmed the mission's finding — assignment
  editing existed only at model level. Implemented the player-facing editor
  in the Casting Order view (add/remove rows, Automatic/pinned caster
  cycling, per-assignment enhancements with REQUIRED/OPTIONAL toggles in
  the chooser, per-target remove/move/split) with resolved providers and
  exact expected recipients surfaced per row; the material-change gate
  (caster/item, enhancement/omission, cost, order, coverage vs the reviewed
  baseline, routine-scoped) now refuses execution until renewed review; and
  the editor test exposed and fixed a real planner defect (optional-policy
  targeting modifiers were silently dropped even with charges available).
  Suite 161/161 (commit `461d999`).
- Checkpoint 4 (rod identity, bounded conclusion): assembly evidence from
  the installed Assembly-CSharp (compile-verified reflection; MVID
  07fa1e4d-8618-41b3-9b8d-faa17d3b26f7) — `Kingmaker.Items.ItemEntity`
  and `Kingmaker.Items.ItemsCollection` expose no instance-identity
  member (no Guid/UniqueId/persistent id; identity is blueprint + owner +
  collection position with `TryMerge` stacking), and
  `ActivatableAbility.ResourceCount` lives on the owner+blueprint fact.
  **No qualified durable instance contract was found in the inspected
  implementation.** This is not proof of absence in the engine: a base
  type of `System.Object` alone does not prove it, and serialization or
  deeper state may hold identity the managed surface does not expose.
  Pooled "any matching rod for the caster" remains the honest behavior;
  exact-item selection stays explicitly unresolved (not silently
  dropped), pending a live fixture to test whether two identical rods
  keep separate native charges and whether any durable binding exists.
  No invented IDs, no slot/ordinal pins, no production rule changes.
- Checkpoint 3 (fixture bootstrap): `scripts/New-KbpAutomationFixture.ps1`
  added — a guarded, source-tested (harness 12/12) bootstrap that seals a
  deliberately disposable `Manual_<n>_KBP_AUTOMATION_SEED.zks` (header Name
  exactly `KBP_AUTOMATION_SEED`) into the immutable
  `KBP_AUTOMATION_BASELINE` + mutable `KBP_AUTOMATION_WORKING` pair with
  only header.json's Name rewritten, byte-archives the seed and sealed
  baseline offline with a hash manifest, proves every other save
  byte-identical before and after, refuses any pre-existing automation
  artifact, and offers an exact-contract teardown. The ONLY remaining
  dependency is the human-only step below.
- HUMAN-ONLY PREREQUISITE (live lanes): in a Kingmaker session the user
  controls, create one deliberately disposable campaign (New Game, any
  quick character) and save once with the exact name
  `KBP_AUTOMATION_SEED` at any controllable moment. Do not rename or copy
  an existing ordinary campaign for this. Then run
  `powershell -ExecutionPolicy Bypass -File scripts/New-KbpAutomationFixture.ps1 -Confirm:$false`;
  success = "Fixture bootstrap PASS" with sealed baseline/working hashes,
  after which Checkpoint 5 live runs are unblocked.


## PR1 repair follow-up — 2026-09-18 (findings F1-F7)

Reviewed HEAD `bd2f536`; continuation commits `4e0123c..20a472e` repair
every source-level finding. Per-finding record (reproducer → fix →
regression) lives in the commit messages; the tracker entries above are
updated in place. Gates: source 42/42, protocol 165/165, harness 18/18,
package 4/4, WhatIf 5/5. Live lanes remain unrun: no
`KBP_AUTOMATION_SEED` exists on this machine (verified again before this
record); the repaired bootstrap refuses until one is created by the
user. Theme donor promotion, rendered layout, spellbook placement, and
casting/resource acceptance remain live-only.


## Targeted-repairs continuation (R1-R4) — 2026-09-18

- R1 real-save-use safety gate: **PASS** — exercised only against isolated
  temporary roots so far; the real save directory has NOT been touched
  because no `KBP_AUTOMATION_SEED` exists (re-verified this pass). Gate
  basis: production script itself, 23/23 harness assertions covering
  live-process recovery refusal, run/token-bound lock release, containment
  + role + campaign-provenance validation before every removal, foreign/
  substituted/escaped/unknown-state refusals, byte-for-byte WhatIf purity
  of all three modes (locks, transaction JSON, archives, timestamps), and
  write-ahead publication with after-move interruption recovery. When a
  genuine seed exists, run the repaired helper against the real directory.
- R2 fixed: preview computation is side-effect-free; acknowledgment is an
  explicit post-binding presentation call via PlannerReviewCoordinator.
- R3 fixed: prepared tokens consumed exactly (linked included) with
  aggregate derived from token state; effect projection justified-only
  (conditionals unproven, kinds/recipients preserved).
- R4 fixed: per-child resolver requests (no union merge), pinned-routing
  protection in the portrait strip, reconciled select-all/clear, target
  display names, non-overlapping header bands.
- Gates after repairs: source 42/42, protocol 169/169, harness 23/23,
  package 4/4, WhatIf 5/5. Candidate rebuilt deterministically (see
  QUALIFICATION for identity). Spellbook same-context return remains the
  known incomplete feature, pending the live native-reopen contract.


## N1-N4 review continuation — 2026-09-18

- All four findings from the 92f13fc review closed with production-path
  regressions. Commit `ebeaab6`.
- N1: per-assignment identity preserved end to end
  (`AssignmentProviderOption`/`AssignmentEligibility`); source summaries
  aggregate reach instead of first-option collapse; picker legality is
  assignment-specific; the production resolver enforces
  caster/spellbook/provider pins before ranking. Both creation orders
  proven (`same-provider-targeting-survives-both-orders`).
- N2: picker toggle removes from the selected child only; sibling-owned
  targets refuse with the owner named and the picker labels them;
  coverage never silently disappears
  (`picker-toggle-never-drops-sibling-coverage`).
- N3: caster-directed forecast effects project onto
  `Provider.CasterUnitId`, never the anchor; target wrappers project
  only with provable recipients
  (`forecast-caster-effects-land-on-caster`).
- N4: journaled-but-absent destinations reconcile as unperformed;
  rollback tolerates completed cleanup and binds staging to the exact
  run directory; the original failure always repropagates (a swallowed
  stage failure previously printed PASS); the lock is retained on
  rollback failure for -Recover and released only against verified
  ownership; teardown validates the whole set through the containment
  validator (including escape refusal and mandatory campaign identity)
  before deleting. Harness 26/26: journal-window (baseline+working),
  rollback-failure lock retention + repeated recovery, escaped-manifest
  teardown refusal, plus all prior suites.
- Gates: source 42/42, protocol 172/172, harness 26/26, package 4/4,
  WhatIf 5/5. No `KBP_AUTOMATION_SEED` exists (re-verified); no live
  lanes run. Spellbook same-context return remains the known incomplete
  feature pending the live native-reopen contract.


## First live fixture + smoke attempt — 2026-09-19

- FIXTURE BOOTSTRAP (real save directory, first production use): PASS.
  Seed `Manual_303_KBP_AUTOMATION_SEED.zks` (header Name exact, GameId
  `df33d1ff-4ec8-4707-bfa0-5e059bf9a049`, area JamandisMansionThroneroom)
  sealed into `Manual_304_KBP_AUTOMATION_BASELINE.zks` (sha256
  `d095c02c3d12d511b20254e03b7e741f6b989c21600734c365990f11a3446edd`) and
  `Manual_305_KBP_AUTOMATION_WORKING.zks` (sha256
  `5f8e33363202ed9fdfa1268051962982df41328e2f22338380ba818ebde048bb`).
  All 87 pre-existing saves verified byte-identical after; no lock/state
  residue; manifest + offline archive at
  `runtime-backups/automation-fixture/bootstrap-automation-fixture`.
- Integration defect found and fixed by the first real run (`d4a095a`):
  fixture transaction records lived under the deployment guard's state
  root, so every guarded runtime run would refuse (guard only recognizes
  Restored as terminal; fixture terminal is Completed). Fixture state now
  defaults to its own `runtime-fixture-state` root; harness regression
  added (27/27).
- Environment change recorded (`0c2c28c`-era commit): installed UMM
  updated 0.32.4 -> 0.33.0 between missions; the exact-match pin now
  records the verified installed identity (file version 0.33.0, SHA-256
  `63e5baf7b1738e4091b5fd17ccb738ecdb4d1dbf246061dfd55dc52835d52691`).
- First live smoke run `n-fixtures-first-live-smoke-6`: the guarded launch,
  staging, UMM dismissal, exact-pair proof, and load invocation all
  executed; the Mods transaction restored exactly. The run FAILED inside
  the game engine: `NullReferenceException at Kingmaker.Player.PostLoad()`
  during `SaveManager.LoadRoutine` (LoadGameException), then repeated
  WeatherSystem NREs. Evidence:
  `runtime-evidence/n-fixtures-first-live-smoke-6/` + game output_log
  lines 129-189 (exact pair proven; OnButtonSaveLoad invoked;
  OnAreaBeginUnloading; engine crash). Diagnosis: the seed was saved at
  the start-prologue moment (second area entry
  `...jamandismansionthroneroom_startprologue.json`), a save point this
  engine cannot load from the main menu. The historical
  Repair-KbpAutomationWorkingSave contract targets a different sub-shape
  and correctly refuses this one; no save-format fix is fabricated.
- Fixture pair torn down through the manifest-bound path (exact pair
  removed, 87 saves byte-identical, archive preserved). The seed file
  itself remains for replacement.
- NEXT HUMAN ACTION (smallest): see AUTONOMOUS-RESUME top section.


## Seed-2 fixture and load A/B — 2026-09-19 (second session)

- Pre-mutation verification: no Kingmaker process; prior transactions all
  settled (deployment 2 Restored, fixture 1 Completed); no locks; live
  Mods = user original (16 dirs, KBP 0.0.19). Replacement seed recorded:
  `Manual_303_KBP_AUTOMATION_SEED.zks` sha256
  `2fdce5b62206d51e67ba04b8b497a9bec24b5036f03bfab04974adbcfaff7b8d`,
  same campaign GameId `df33d1ff-...`, human-verified loading with movement
  and inventory. UMM enabled state: only CallOfTheWild (true) and
  KingmakerBuffPlanner (true) have Params.xml entries.
- Bootstrap-2 PASS (real directory): derived indices 304/305 from actual
  state; baseline sha256
  `f13de02d7c59a70a34d335a413f0d90fc7e63cdcb327224ecf5885ed6e1f8300`,
  working `41a19e0ac196bea6ce1466dacdcd4cd6717b06a1b2b372e8a10e85cdfebcbae5`;
  all 87 pre-existing saves byte-identical.
- Load A/B (evidence per run under runtime-evidence/):
  1. `seed2-first-load-proof-1` (native-only): FAILED — engine
     `Player.PostLoad` NRE at a DIFFERENT offset (0x00022) than seed-1;
     same campaign-load-completion timeout class.
  2. `seed2-load-full-config-1/2` (new full-user profile, all 15 mods):
     the game boots all mods (log shows every mod initializing) but never
     completes first-idle-update within 240-420s windows in this
     environment; harness-owned processes terminated; exact restoration
     verified after each. Automated full-config lane NOT VIABLE here.
  3. `seed2-load-cotw-3` (call-of-the-wild): failed a DIFFERENT gate —
     my RuntimeTestHost counted zero loaded optional mods under UMM 0.33
     (candidate UMM-0.33 counting defect in the host, independent of the
     save question). Save-load itself again timed out.
- FIRST CAUSAL DIFFERENCE (evidence-backed): the save references 1076
  distinct GUIDs; cross-referencing the installed mod content shows at
  least 246 are provided by SIX absent-under-native-only mods:
  CallOfTheWild 106, CraftMagicItems 70, ZFavoredClass 35,
  KingmakerGunslinger 30, TweakOrTreat 3, BagOfTricks 2
  (`artifacts/save-mod-dependencies.json`). The Player.PostLoad failure
  under native-only is consistent with missing save dependencies, NOT an
  engine-only defect — the earlier "engine-level incompatibility"
  conclusion for seed-1 is superseded: seed-1's save had the same class
  of mod references (same campaign).
- Environment drift handled this session: full-user profile added
  (15 mods, exact identities, validation PASS); call-of-the-wild profile
  re-pinned to the installed CallOfTheWild identity; UMM 0.33.0 pin from
  the prior session.
- NEXT: (1) fix RuntimeTestHost optional-mod counting under UMM 0.33;
  (2) seed-dependencies profile staging exactly the six GUID providers
  and retry the load; (3) if still failing, next factor is UMM
  enabled-state/overlay handling; then the rendered smoke.


## Release closeout — 2026-09-19

- Testing preview **published and verified**:
  https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.1.1-rc1
  (non-draft prerelease, latest channel unchanged; stable v0.0.19 intact).
  ZIP SHA-256
  `ea1fa19e839df4a5c5ddaf095d15a2933acec3f573b6ff3868430028c50b4c7b`,
  re-downloaded and validated 4/4. Tag == source `5da102f` (feature
  branch, pushed via the guarded helper).
- Engineering added this session: prerelease versioning (CLR 0.1.1.0 /
  semver 0.1.1-rc1) with source-validator support; guarded publisher
  `-AllowFeatureBranchPrerelease` (prerelease-only, pushed-HEAD required,
  stable versions still default-branch-only; 3/3 gate regression);
  honest preview release notes separating implemented/tested/limited/unrun.
- No further seed, profile synthesis, or engine investigation is
  authorized or needed; the fixture save's GUID dependency evidence
  supersedes the engine-only theory. Manual owner testing is the
  acceptance path for this preview.
