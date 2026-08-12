# Architecture

Status: 0.0.6 live row-rendering architecture qualified twice with screenshots and guarded-installed

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
