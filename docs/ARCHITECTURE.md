# Architecture

## 0.0.10 plan-derived clarity and native rendering boundary

The consolidated-card and provider-selection architecture from 0.0.9 is unchanged. `CastStep` and `TargetPlanOutcome` now retain aggregate `SourceId` provenance, while each step exposes the recipients already computed by the planning engine. `TargetPortraitViewModel` is a presentation adapter over that selected-source plan slice; it does not recalculate spell mechanics. Its deterministic precedence is `InvalidTarget`, `DirectSelectedAndCovered`, `DirectSelectedButUnavailable`, `IndirectlyCovered`, then `Neutral`. Area/party expected recipients can therefore be shown as light-green `COVERED`, while ordinary single-target plans do not leak coverage and caster-centered previews do not invent a receiver.

`SelectedBuffPlanSummaryViewModel` reports only the selected aggregate's resource availability and planned cast count, with an optional selected/additional-recipient line. It deduplicates shared resource pools and preserves the implicit provider ranking. Generic coverage fractions and blocked counts are not presentation contracts.

HUD construction still uses the same native host, four hitboxes, listeners, tooltips, pointer ownership, and actions. The owned row's horizontal origin is derived from the minimum left edge of the native cluster's direct button children. Each glyph uses a fixed safe area, centered anchors/pivot, unit scale, preserve-aspect, and one centralized alpha-bounds optical correction. Runtime qualification separately proves left-edge alignment, glyph centering, hit ownership, and zero native activation.

The modal remains a dedicated top-level overlay because that boundary owns the already-qualified input lease. It no longer adds a `CanvasScaler`. Text uses the exact native legacy `Text` font/material path (Arial and `UI/Default` in the installed 2.1.7b service UI), fixed integer sizes, disabled best-fit, and unit-scale parents. After forced layout, rendered text and active cards are snapped to the canvas pixel grid. Global/native canvas settings are never changed.

## 0.0.9 effect-semantic card aggregation and polish boundary

The four-column planner now groups provider-backed catalog rows by a deterministic normalized-effect fingerprint described in `planning/UI-POLISH-AND-SOURCE-CONSOLIDATION.md`. The key includes stable effect identity, kind, target semantics, sequence/conditional structure, and branch contracts. It excludes caster, provider, spellbook/resource state, discovery paths, and wrapper ability IDs. It never falls back to display-name matching, and unresolved effects retain exact ability identities. Each aggregate retains all member ability keys, so the unchanged provider ranking and resource ledger choose among every eligible source implicitly during planning and execution.

Legacy exact-ability assignments are rebound to aggregate source IDs at catalog binding and colliding assignments are unioned without discarding targets. `TargetPortraitState` is the presentation boundary for direct selection, indirect party/area coverage, valid-neutral, invalid, and selected-but-unfulfillable states. HUD changes remain styling-only: native anchor discovery, hit rectangles, listeners, tooltip behavior, pointer ownership, actions, and lifecycle are frozen.

## 0.0.8 four-column presentation boundary

`BuffPlannerScreenView` is a new shell over the unchanged `PlannerUiSession` and `PlannerSetupModel`. `PlannerRoutineTabsView`, `PlannerCategoryTabsView`, `BuffGridView`, `BuffCardView`, `BuffCardPool`, `PlannerTargetStripView`, `PlannerSelectedBuffView`, and `PlannerSettingsView` own rendering only. `PlannerScreenViewModel` formats alphabetical cards, routine-local target state, readiness, and concise plan summaries; callbacks invoke existing model commands.

The catalog is a vertical `ScrollRect` with exactly four computed columns and a fixed pool of 32 cards. Only Search, All/Spells/Abilities/Other, and Selected only exist in the normal catalog. Selected only reads assignments with at least one target in the active routine. The selected lower panel owns direct portrait toggle, Select All Valid, Clear Targets, icon/name/source/duration/description, and plan summary. Provider selection remains automatic and provider/resource preferences remain persistence-compatible but have no production view.

Profile schema 3 clears legacy hidden IDs and migrates blank/F10 hotkeys to `Ctrl+Shift+B` while retaining routine assignments, target IDs, provider preferences, execution settings, and campaign identity. A narrow legacy Harmony12 prefix on exact `KeyboardAccess.Binding.InputMatched()` suppresses native B only while Ctrl+Shift+B is down. The existing modal input lease and HUD pointer ownership remain separate.

HUD anchors, RectTransforms, hitboxes, listeners, tooltip ownership, and lifecycle are unchanged. Only the owned tile/glyph treatment is specialized: near-black brown tiles and generated antique-gold sprite ink. Guarded runtime evidence samples both tile and sprite pixels. Human visual acceptance remains pending.

Status: 0.0.7 presentation architecture qualified in exact Kingmaker 2.1.7b; human visual verdict pending

## 0.0.7 presentation boundary

Presentation remains a consumer of `PlannerUiSession` and `PlannerSetupModel`; it does not own discovery, identities, allocation, resource accounting, persistence, or execution. `BuffCardViewModel`, `TargetPortraitViewModel`, `CastingSourceSummaryViewModel`, `RoutineSummaryViewModel`, and `PlannerSettingsViewModel` translate existing state into player-facing text and deterministic neutral/success/warning/failure states.

`PlannerUiTheme` is the only native visual adapter. It inventories exact 2.1.7b sprites/fonts, records resolution or fallback, and centralizes every parchment, brown, burgundy, gold, green, amber, red, and disabled token. Native objects remain game-owned. Proportion-sensitive large frame/card sprites are inventoried but deliberately not stretched; safe programmatic parchment surfaces and borders are used instead. Native button and portrait treatments remain optional, bounded references.

`BuffPlannerScreenView` renders alphabetical icon cards and portrait targets. All callbacks invoke existing model commands. Hover preview is transient, target bulk operations save once, filters are non-persistent view state, Casting Source is disclosure over existing provider preferences, and Settings is the sole mode editor. Runtime presentation diagnostics are opt-in test code: they count real/fallback icons and mode controls, reject retired labels, capture five hashed views, and never become mechanics state.

## 0.0.6 masked scroll rendering and visual evidence

Both catalog panes use `ScrollRect -> Viewport(Image + Mask) -> Content`. `Mask.showMaskGraphic=false` suppresses viewport color through the UI stencil material's color mask; the source Image remains opaque so alpha clipping cannot prevent the stencil write. Child rows/details use the standard `UI/Default` material and stencil equality. Layout elements set a positive RectTransform height and the vertical group controls child height.

Runtime visual qualification is intentionally outside production-domain logic. The campaign host selects the first real row, waits for rendered frames, captures a PNG, and exports names, screen rectangles, selected/details text, CanvasRenderer, alpha, font, material, mask, canvas, parent, sibling, and layout evidence. The external guarded harness verifies the screenshot hash and independently samples every first-five row rectangle plus the details title for non-uniform, high-contrast pixels. Active objects, geometry, intersection flags, and code-authored visibility booleans are never sufficient by themselves.

Material-component planning and execution share Kingmaker's requirement boundary. A consumable reservation exists only for a positive-count item when `RequireMaterialComponent` is true. Runtime sufficiency is evaluated lazily under the same flag; native `AbilityData.Spend()` remains responsible for actual spending.

## 0.0.5 catalog presentation and pointer ownership

`KingmakerPartySnapshotBuilder` emits structural source/provider/effect data and bounded discovery counts. `PlannerUiSession` owns refresh, profile/plan orchestration, quick-result state, and full exception reporting. `PlannerSetupModel` exposes stable source/provider/availability state. `CatalogFilterState` is the non-Unity policy for ordered search/configuration/duration/source/hidden/availability filtering; the retained view consumes its values and diagnostics, then measures instantiated, active, and viewport-visible rows separately.

The retained screen explicitly rebuilds scroll content, binds the first visible source when needed, surfaces row/details exceptions, and distinguishes filtered-empty from genuinely unavailable catalogs. Filter controls display their current mode and Reset Filters restores the available/non-hidden default.

`PlannerPointerOwnership` registers only live KBP rectangles. Legacy Harmony prefixes `PointerController.Tick` only while the pointer is inside one of those rectangles, and the narrow camera postfix zeros edge-scroll shift under the same condition. Outside planner regions native input is untouched. Full-screen input remains controlled by the existing validated input lease.

The HUD owns one cached tooltip outside horizontal-layout participation. Its `CanvasGroup` and all graphics are non-raycastable, it is width-bounded/wrapped and clamped into the active screen, and its listeners are installed once per button. Quick execution remains session/service orchestration and reports only confirmed effects as applied.

Kingmaker Buff Planner is one standalone Unity Mod Manager mod. The assembly, namespace, UMM ID, persistence, package, runtime runner, and compatibility profiles are all owned by this repository.

The dependency direction is:

```text
Integration/UI/RuntimeTesting -> Services -> Domain
GameAdapters/Discovery/Execution/Persistence -> Domain contracts
Planning -> Domain only
Compatibility -> Discovery contracts through bounded reflection
```

Static Kingmaker, Unity, UMM, and Harmony state is confined to narrow adapters and the composition root. Discovery emits normalized immutable effect expressions; planning consumes catalog/provider snapshots without Unity dependencies; execution consumes an immutable plan through animated or instant engines. UI controllers orchestrate services and never implement scanning, allocation, persistence, or casting rules.

Runtime automation is a separate, opt-in request/result boundary. It cannot deploy itself, select arbitrary saves, or mutate the live mod directory outside the guarded PowerShell transaction.

Compatibility profiles are data contracts, not dependencies. The harness verifies exact optional directory and file identities, stages read-only fixtures into its transaction-owned tree, and binds expected UMM entries, assemblies, hashes, and representative blueprints into the runtime request. Blueprint ownership comes from the optional mod's exact emitted inventory; ordinary content continues through structural discovery. Runtime diagnostics enumerate legacy Harmony12 targets and ordered owners through bounded reflection, while the production planner applies no Harmony patches.

Planning operates only on `PartyProviderSnapshot`, stable keys, normalized effects, and immutable requests. Its mutable `ResourceLedger` is plan-local and never edits Kingmaker objects. Prepared sources use discrete primary/linked tokens; spontaneous sources share one level pool; unlimited sources have an explicit kind; material reservations are plan-local. External profiles serialize DTO keys and settings only, never `UnitEntityData`, `AbilityData`, `SpellSlot`, facts, or Unity objects.

The 0.0.3 player UI uses fresh retained-mode controls; no native `ButtonPF` GameObject is cloned. `BuffPlannerHudButtonController` uses the private `IngameMenuController.m_FormationButton` only to locate the actual cluster and its active `GraphicRaycaster`. It creates one bounded row immediately above that cluster, ordered Setup/Long/Important/Short, and rejects installation unless every visible center is the first EventSystem hit. The runtime gate attaches temporary sentinel listeners to the native cluster and requires that planner clicks activate none of them.

Modal opening is a two-phase transaction. `BuffPlannerScreenController` first creates a dedicated top-level screen-space overlay canvas, forces layout, and validates active hierarchy, root-canvas identity, sorting, opacity, raycast ownership, required controls, real world corners, and at least 98% screen coverage. Only then may `PlannerScreenStateMachine` enter `AcquiringInputLease` and acquire `FullScreenUi`/selection suppression. The same presentation is revalidated after acquisition and while open. Any failure destroys the partial view and rolls back to `Closed`; an input lease is never the definition of a visible screen. No permanent input patch is installed.

The four icons are original textures generated in memory by project code and are not packaged assets. Native font and safe presentation sprites are reused when available. The full-screen content uses proportional anchors and scrolling rather than fixed screen coordinates.
## 0.0.4 live bootstrap lifecycle

`Main.OnUpdate` owns the minimal fallback path: it logs its first tick, arms and polls F10, ensures exactly one strongly referenced `DontDestroyOnLoad` controller, and only then advances UI installation. F10 therefore remains observable when no HUD host exists or a HUD candidate fails readiness. The controller subscribes once to Kingmaker scene, area-loading, and area-activation events; startup, late load, area transitions, and polling converge on the same idempotent installation path.

New Unity graphics remain candidates for at least two update frames before EventSystem ownership is required. HUD candidates are noninteractive during this interval. A full-screen candidate acquires the native input lease only after deferred visible-presentation validation, then validates again. Any failure logs its exact predicate and complete stack, disposes only owned objects, releases only an acquired lease, and remains retryable.

The HUD root is marked `LayoutElement.ignoreLayout` so the native cluster may host it without overriding its explicitly above-cluster anchor; the KBP root's own horizontal child layout remains active. EventSystem probes and dispatched pointer events convert button world centers to screen coordinates through the native `GraphicRaycaster.eventCamera`. These are integration boundaries, not domain/UI layout policy leaks.
