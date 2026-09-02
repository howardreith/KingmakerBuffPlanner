# Changelog

## 0.0.18 - reliable instant sticky-touch delivery

- Separates supported beneficial sticky-touch delivery from abilities that
  genuinely require a native queued command. Instant profiles now execute the
  structurally validated delivery blueprint directly through `RuleCastSpell`.
- Preserves the exact planned source `AbilityData` and prepared-slot token,
  charges that source exactly once through native `Spend()`, and never charges
  both the carrier and delivery ability.
- Adds state-based transaction completion, delayed effect confirmation, held
  touch cleanup, and distinct submission/rule/spend/effect diagnostics.
- Makes deliberate or enhancement-required Animated execution observe both the
  carrier and its generated delivery command before advancing.
- Adds four-target prepared and spontaneous regressions, no-double-spend and
  UMD coverage, animated two-stage lifecycle coverage, and failure-cleanup
  coverage.

Published at `2026-09-02T02:23:47Z` from annotated tag `v0.0.18` and exact
release commit `1477979fa8b44e220adc3ece0afc85581a8b5811`. Save-backed Freedom
of Movement execution remains blocked because the protected automation
baseline/working save pair is absent; the owner accepted that explicit evidence
boundary without relabeling it as runtime PASS.

## 0.0.17 - communal coverage, Share Transmutation, and Infusion

- Restores one-cast party/allied-area recipient maps for structurally proven
  communal effects, including per-anchor radius coverage and the existing
  indirect `COVERED` portrait state, while hostile/ambiguous geometry fails
  closed.
- Adds one routine/assignment-aware effective-targeting seam shared by portrait
  legality, stale-target pruning, preview, provider selection, planning, and
  native execution preflight.
- Adds optional, exact-profile Brown Fur Share Transmutation support without a
  compile-time gameplay-mod dependency. Share is an independent toggle, uses
  native `AbilityData` targeting, forces native animated execution, and leaves
  successful Arcane Reservoir debit to the provider's transaction.
- Replaces category-only enhancement compatibility with explicit exclusivity
  and native activation groups. Share composes with one Powerful Change choice,
  and shared-pool costs are summed before planning and again at execution.
- Inherits passive Alchemist Infusion targeting from native
  `AbilityData.CanTarget`; no Infusion toggle or planner surcharge is added.
- Adds deterministic structural, planning, presentation, persistence,
  execution-lease, shared-resource, optional-contract, and Infusion regressions.

Published at `2026-09-02T00:17:46Z` from annotated tag `v0.0.17`. Save-backed
runtime behavior remains explicitly unverified because the exact protected
automation save pair is absent; no player save was used.

## 0.0.16 - catalog semantics and native HUD maintenance

- Normalizes optional Call of the Wild preparation/casting companion spellbooks
  at the party snapshot boundary, so a preparation-only Arcanist book cannot be
  selected by catalog, policy, planning, or execution while ordinary prepared,
  spontaneous, multiclass, and exhausted cast-capable books remain available.
- Adds branch-aware restorative-action semantics. Pure healing, restoration,
  condition-removal, resurrection, dispel, and cleanup paths whose only lasting
  leaves are internal markers no longer become catalog buffs; a substantive
  persistent beneficial leaf on a safe branch still qualifies.
- Propagates conservative allied area recipient semantics through per-anchor
  coverage maps, so structurally allied communal/mass effects use one anchor
  cast and visibly cover their legal nearby recipients without making enemy or
  ambiguous areas party-wide.
- Adds profile-derived L/I/S routine-membership chips to every card, including
  active-routine emphasis, secondary cross-routine indication, text tooltips,
  and a compact legend. Existing assignments and persistence schema 4 remain
  authoritative.
- Rebuilds the lower-left HUD controls as owned `ButtonPF` instances styled
  from the exact live native formation button, with neutral alpha-mask glyphs,
  native `TooltipTrigger` parchment presentation, and one native
  `CharacterScreenOpen` sound per successful setup transition.

Published 2026-09-01 from annotated tag `v0.0.16`.

## 0.0.13 - Brown-Fur Powerful Change diagnostic repair

- Discovers Powerful Change from the live caster fact and six validated native
  score-toggle contracts supplied by the optional Brown-Fur implementation.
- Qualifies genuine Transmutation spells from the exact Arcanist spellbook by
  structural positive ability-score bonus carriers; Bull's Strength is not a
  name or spell-GUID special case.
- Models caster/spellbook/variant applicability and the six score selections as
  one shared Arcane Reservoir usage pool.
- Arms the provider's real one-shot toggle and requires a native animated
  `UnitUseAbility` command so its own transaction adjusts the original modifier,
  spends exactly one reservoir point, and consumes the selection.
- Emits focused `[KBP][Enhancement]` qualification/rejection diagnostics and
  fails safely when the optional blueprints or their proven contract are absent.
- Adds deterministic coverage for all six ability scores, polymorph carriers,
  missing capability, ordinary casters, wrong spellbooks, unrelated spells,
  mass variants, shared uses, native routing, and one-shot cleanup.

The owner accepted the validated candidate, and version 0.0.13 was published on
2026-08-27 from tag commit
`3c329cfff3530fe8397012565c238a81d55cec1d`. The exact published bytes are
guarded-installed. Direct cross-mod numerical cast evidence is not claimed; the
focused runtime diagnostic procedure remains documented.

## 0.0.12 - HUD lifecycle retry repair

- Replaces the ambiguous Boolean HUD installation result with explicit attempt,
  candidate-validation, and lifecycle states.
- Retries temporarily unavailable inner HUD controls at one bounded attempt per
  30 active-HUD frames without polling while the campaign HUD is absent.
- Reports provisional-candidate expiry to the owner, waits for the HUD to settle,
  and recreates the four controls without requiring an outer-HUD identity change.
- Detects inactive, detached, reparented, or replaced owned roots, inner anchors,
  native clusters, active HUD hosts, and raycasters before treating installation
  as live.
- Suspends host-triggered discovery during area unload and mod disable, preventing
  candidates from being created against transient loading HUDs.
- Preserves deferred placement, alignment, glyph, and raycast validation and keeps
  discovery scoped beneath `StaticCanvas.HUDController`; no global Unity search
  was restored.
- Adds transition-level diagnostics and deterministic retry, expiry, staleness,
  lifecycle, and steady-state performance regression coverage.

The owner accepted the guarded-installed campaign repair on 2026-08-24. Version
0.0.12 is published from tag commit
`a48bfae2185a50f1c50d9151666e0b5ce0a0bc3e`; version 0.0.11 remains a separate
immutable release and was not overwritten.

## 0.0.11 - Runtime performance repair

- Stops polling the entire Unity object graph every frame when the campaign HUD is absent.
- Dispatches HUD discovery only after enable, lifecycle, host-identity, host-active-state, or planner-hotkey invalidation.
- Resolves the native in-game menu only beneath the exact active `StaticCanvas.HUDController` instead of using global `Object.FindObjectOfType`.
- Adds an opt-in, aggregate one-second runtime performance probe and a deterministic unchanged-frame/host-transition regression contract.
- Updates guarded runtime identity and fixture records for the exact current local UMM and optional-mod environment without installing or mutating third-party mods.
- Separates disposable protocol-test fixtures from the guarded production evidence root and reports setup failures as controlled nonzero test exits instead of unhandled CLR exceptions.

No 0.0.10 artifact was replaced. Version 0.0.11 is published from tag commit `3661f5c31a1060bca67758c2369b2ef361a339c9`.

## 0.0.10 — Visual clarity and plan coverage

- Aligns the HUD row to the native lower-left button grid and centers glyph alpha bounds in fixed safe areas without changing hitboxes or listeners.
- Derives direct, unavailable, indirect, invalid, and neutral portrait states from the selected consolidated buff's real cast-plan steps and outcomes.
- Replaces ambiguous routine-wide coverage fractions with selected-buff availability, planned casts, and explicit/additional recipient counts.
- Removes the planner CanvasScaler, retains the proven modal canvas, reuses the native Arial/UI material path, disables best-fit, and pixel-snaps owned text/cards after layout.

## 0.0.8 — Four-column routine planner

- Replaces the narrow technical catalog with one pooled, vertically scrolling four-card grid.
- Makes portrait clicks directly add or remove targets in the active Long, Important, or Short routine.
- Reduces catalog controls to Search, All/Spells/Abilities/Other, and Selected only.
- Migrates hidden entries to visible while preserving routine targets and automatic provider behavior.
- Replaces F10 with configurable Ctrl+Shift+B (or Ctrl+Shift+P) and isolates the chord from native B/P actions.
- Restores the HUD buttons to a dark tile with antique-gold glyph treatment without changing their interaction geometry or lifecycle.

## 0.0.7 — Kingmaker-native planner presentation

- Replaced the diagnostic-looking setup screen with a centralized Kingmaker parchment, burgundy, antique-gold, and brown-text theme using exact 2.1.7b native candidates with safe fallbacks.
- Replaced text rows with alphabetical icon-first buff cards showing routine, availability, selection, and neutral/ready/partial/blocked status.
- Made party and pet portraits the primary target editor with legal, requested, fulfilled, blocked, and indirect-beneficiary overlays plus Select All Valid and Clear Targets.
- Simplified primary filters to Search, Configured only, Show hidden, Reset, and a closed Advanced Filters drawer.
- Renamed provider/resource presentation to Casting Source, collapsed it by default, and moved priorities, disablement, spellbook identity, and caps under Advanced Casting Source.
- Removed the duplicate footer Mode button; Settings now contains the only Animated/Instant control alongside combat, existing-buff, and fallback choices.
- Added exact native-theme inventory, icon/control diagnostics, five hashed visual-acceptance screenshots, and an explicit physical Animated/Instant runtime parameter.
- Preserved discovery, planning, resource accounting, persistence schema/identities, confirmed-effect execution, HUD input isolation, modal lifecycle, and guarded staging contracts.

## 0.0.6 — live row-rendering recovery

- Repaired both empty scroll panes by keeping their visually hidden Unity Mask source opaque so it writes the stencil consumed by row/detail graphics.
- Added and removed a high-contrast same-Content diagnostic canary after an actual live screenshot A/B isolated the viewport path.
- Enforced explicit positive row/detail heights and report matched versus actually bound rows.
- Capture real campaign screenshots with five expected names/rectangles, selected details, CanvasRenderer/font/material/mask evidence, hash, and independent pixel contrast.
- Corrected generic material validation to check inventory sufficiency only when Kingmaker reports a consumable component requirement; native Bless now confirms and spends normally.
- Preserved HUD icons, stable tooltips, pointer isolation, F10, opaque modal, lifecycle restoration, persistence, native discovery, and Call of the Wild support.

## 0.0.5 - catalog, HUD input, quick-action, and tooltip repair

- Trace every live catalog/filter/layout stage and surface binding or empty-state reasons instead of a blank pane.
- Show available non-hidden beneficial entries on first open, with Reset Filters and Refresh recovery actions.
- Display each filter's active mode and cover default, all-hiding, and Reset Filters behavior in the deterministic suite.
- Size scroll content explicitly and validate visible row/detail geometry in the actual campaign.
- Stabilize one clamped, wrapped, non-raycastable HUD tooltip outside button layout participation.
- Suppress Kingmaker world clicks only while the pointer is inside an active planner-owned HUD or full-screen region.
- Refresh quick actions independently and report exact refusal, validation, execution, and confirmed-effect outcomes.
- Align CLR assembly/file/informational versions with 0.0.5 and validate them against `Version.props` before packaging.

## 0.0.4 — 2026-08-12 (recovery in progress)

- Moved the F10 fallback into the UMM update callback so HUD construction cannot disable hotkey diagnostics.
- Added structured `[KBP-BOOT]` load/toggle/update/EventBus/scene/area/HUD/modal/F10 diagnostics, full exception stacks, and a UMM bootstrap snapshot.
- Deferred HUD and modal raycast-ownership validation across Unity frames and kept transient failures retryable without acquiring gameplay input.
- Excluded the planner row from the native menu layout after real-campaign evidence proved that layout participation overrode its above-cluster anchor.
- Converted native HUD world centers through the raycaster event camera before EventSystem hit tests and synthetic pointer dispatch.
- Made live-scenario failures commit one atomic diagnostic result instead of escaping into every UMM update frame.
- Corrected guarded restoration to ignore a bound null process ID instead of falsely reporting an unnamed running Kingmaker process.
- Made fresh-process live qualification physically dismiss UMM's configured ShowOnStart blocking overlay before proving HUD ownership and delivering physical F10.
- Captured input/mode baseline before physical F10 and recorded exact UMM cache-assembly identity separately from the primary optional-mod fixture file.
- Added an exact disposable-save live-campaign scenario that delivers a physical F10 key, exercises 20 open/close cycles, and checks object uniqueness and save hashes.

## 0.0.3 — 2026-08-12

Human verdict: **FAILED** — UMM showed the assembly active, but the loaded campaign had no Setup/Long/Important/Short row and F10 produced no visible planner. Its earlier no-save and synthetic results did not qualify live campaign initialization.

- Replaced cloned native HUD objects with four fresh retained-mode buttons in the exact Setup/Long/Important/Short row, anchored above the native cluster with top-raycast ownership validation.
- Made full-screen opening presentation-first and transactional; an invisible/zero-size/non-raycast root now aborts before input acquisition, and every later validation failure rolls back.
- Replaced “fired” success accounting with queued/submitted/started/spent/effect-confirmed terminal outcomes. Missing expected facts now report `TimedOutUnconfirmed` and fail the routine.
- Audited the installed schema-2 profile: the Long Bless assignment and target were preserved without migration; the next execution records the automatically resolved provider and exact Bless fact result.
- Strengthened deterministic and guarded runtime contracts to reject HUD overlap/click-through, invisible modal state, input-before-presentation ordering, and unconfirmed casts.

## 0.0.2 — 2026-08-12

- Replaced the rejected floating IMGUI text strip with one native-anchored setup icon and Long, Important, and Short quick-action icons.
- Added an opaque, proportional, full-screen planner with catalog filters, routine tabs, buff details, party/pet portraits, provider/resource controls, plan summary, results, and settings.
- Added scoped Kingmaker full-screen mode and selection isolation plus a raycasting canvas and pointer consumption for click, drag, scroll, and cancel.
- Added explicit quick-action feedback and stage diagnostics; empty Long now reports `No Long buffs are configured.`
- Corrected the automated UI gate so the main menu cannot certify campaign UI behavior.
- Added a guarded 0.0.1-to-0.0.2 local replacement installer with planner-only backup, profile preservation, non-planner identity verification, and rollback.

## 0.0.1 — 2026-08-11

- Initial standalone development build. Its UI acceptance result was subsequently invalidated by human playtesting and must not be treated as current evidence.
