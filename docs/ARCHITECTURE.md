# Architecture

## 0.0.17 effective targeting, communal coverage, and native Share

The planner now has one assignment-aware targeting boundary:
`EffectiveProviderOptionResolver`. Its input is the current party/provider
snapshot, normalized source request, candidate provider, selected enhancement
IDs, enhancement catalog, and base `ProviderPlanningOption` set. It rejects an
unknown, duplicate, incompatible, or provider-inapplicable persisted selection
instead of falling back to broader targeting. `PlannerSetupModel`,
`BuffCardViewModel`, portrait legality/coverage, routine preview,
`RoutinePlanService`, and `CastPlanner` consume this same result. Because the
request contains one routine assignment, a targeting enhancement in Long cannot
alter the same source in Important or Short.

Base provider options remain the authority for unenhanced native behavior.
Personal effects are self-only except when exact live `AbilityData` proves the
passive Alchemist `IsAlchemistSpell && AlchemistInfusion` contract. No
Infusion enhancement, profile value, or surcharge exists. Explicit targeting
changes are narrow `ICastTargetingModifier` implementations. The Share
modifier recognizes only the exact optional profile, temporarily arms the
caster's validated native Share activatable, asks native `TargetAnchor` and
`CanTarget(TargetWrapper)` for every planner-controlled unit, and restores
the original state in `finally`. The resulting option requires animated
execution so the optional mod retains command, touch/30-foot targeting,
transaction, debit, and one-shot authority.

Communal geometry stays in the base option builder. Party-member actions cover
all currently valid party recipients. Structurally allied
`AbilityTargetsAround` effects produce one map per legal anchor from the exact
runtime radius. `KingmakerAreaCoverageResolver` may combine a selected
concrete variant with its proven declared source, but only after validating that
parent/child relationship. Missing, contradictory, unreadable, hostile, or
ambiguous geometry produces no legal option; it never degrades to ordinary
direct targeting. `CastStep.ExpectedRecipientUnitIds` is built from that same
map, so portrait `COVERED` state and execution evidence agree.

`CastEnhancementSnapshot` now carries explicit exclusivity group, native
activation group, shared usage pool, units-per-cast, and targeting/native-command
flags. Duplicate IDs and collisions within one explicit group fail closed;
Share and Powerful Change use different groups while sharing one
caster-specific Arcane Reservoir pool. Planning groups selected enhancements by
pool and reserves the summed forecast once per cast. Execution repeats the
aggregate live check after resolving the exact ability, then arms the native
toggles before target validation. Activation restoration tracks consumption by
native group, so native consumption of Share cannot suppress restoration of an
independent Powerful Change state, or vice versa. These reservations never
mutate Kingmaker resources; the optional Brown Fur transaction is the only
Arcane Reservoir debit authority.

## 0.0.16 structural spellbook roles, payload semantics, and native HUD

`KingmakerPartySnapshotBuilder` now asks the compatibility-layer
`KingmakerSpellbookRoleAdapter` and Unity-free `SpellbookRoleResolver` to
classify every owned spellbook before provider construction. The optional
adapter reflects only the installed Call of the Wild
`CanNotUseSpells`, `CompanionSpellbook.spellbook`, and
`GetKnownSpellsFromMemorizationSpellbook.spellbook` contracts. A book is
excluded only when a cannot-cast component and an owned same-unit companion
relationship prove it is preparation-only. Ambiguous/missing optional contracts
retain the book. The resolver has no class, display-name, GUID, or temporary
availability rule; `KingmakerAnimatedCastAdapter` repeats its inclusion gate at
execution resolution.

Discovery represents proven restorative actions as `DiscoveryNode` semantic
nodes with branch paths. The classifier evaluates each branch without flattening
conditionals: direct restoration is rejected only when it has no safe
substantive persistent payload, or all safe lasting leaves are internal marker,
carrier, cleanup, or activation effects. A branch containing lasting protection
remains eligible even if another action restores or removes a condition. Catalog
audit records preserve both persistent leaves and restorative action paths.

`AreaRecipientSemantics` keeps `Enemy`, `Ally`, `Any`, and unknown selectors
separate. `Any` is refined to allied only where the enclosing ability proves
friends are targetable while enemies and points are impossible. Provider options
then carry `RecipientIdsByAnchor`; `CastPlanner` and planner presentation use
the same `CoveredTargetIdsForAnchor` map for indirect coverage and mass
allocation. This avoids source-name overrides and avoids broadening genuinely
ambiguous/caster-centered effects.

Routine overlap remains persisted only in existing routine assignments.
`BuffCardViewModel` derives structured L/I/S membership chips from those
assignments; the views render active membership as emphasized and other
membership as secondary text-backed chips. No schema or deduplication layer was
introduced.

The HUD controller creates owned `ButtonPF` controls under the native lower-left
cluster and copies target image, transition, complete `ColorBlock`,
`SpriteState`, navigation, button sound flags, and child icon material/tint from
the live formation button. It uses native `TooltipTrigger` data rather than a
planner-rendered tooltip. `SetupOpenSoundGate` centralizes exactly one
`UISoundType.CharacterScreenOpen` dispatch after a successful hidden-to-visible
screen transition; HUD clicks and the hotkey both use the same `OpenSetup`
path.

## 0.0.14 buff membership, persistent payloads, and caster policy

Catalog membership and temporary cast availability are now separate contracts.
`KingmakerPartySnapshotBuilder` still begins at actual current-party and pet
spellbook/fact/resource collections. `KingmakerAbilityVariants` treats a
concrete source encountered there as owned. When an owned parent declares
children, the game adapter constructs each child from the actual source
`AbilityData` and uses the same `IsVisible()` gate as native action-bar
conversion. It does not consult current slots or resources for membership.
Discovery and execution resolution share this gate and fail closed, so an
ineligible saved child cannot be reconstructed from the parent array.

The branch-preserving discovery model distinguishes
`AlliedAreaRecipients`, `EnemyAreaRecipients`, and
`AmbiguousAreaRecipients`. Exact `TargetType` and
`AbilityAreaEffectBuff.Condition` adapters establish disposition; ambiguous
filters remain ambiguous. Only proven allied areas create mass grouping or
indirect party coverage. Offensive actions are explicit graph nodes with
action paths. The classifier combines target, harmful/hidden/class-feature
flags, component types, source contract, delivery components, and conditional
path. A persistent safe-party payload must remain after hidden marker and
offensive-branch analysis. This preserves substantive hidden self-buffs while
excluding hostile carriers whose only leaf is an internal marker.

Provider policy remains domain allocation policy rather than UI allocation.
`ProviderPreferenceProfile` continues to key `Banned`, `Priority`, and
`MaximumCasts` by exact `ProviderKey`. `PlannerSetupModel` exposes explicit
set, cap, reorder, and selected-source reset operations. Reorder normalizes
priorities; `CastPlanner` retains deterministic automatic fallback for
unprioritized providers and fails closed when bans/caps exhaust the set. The
full-screen caster-policy chooser edits these values, saves through the
existing repository callback, and obtains its visible allocation from a fresh
pure preview. No persistence schema change was required.

Quick-result construction is unchanged. The composition root now has two
result sinks only: the planner-local footer (when open) and UMM logging. The HUD
controller no longer owns a Feedback object or timer, and no native game-log
adapter was introduced.

## 0.0.14 concrete spell variants and complete-name layout

Selectable variants are resolved before domain provider construction.
`KingmakerAbilityVariants` reads the parent `BlueprintAbility.Variants` array in
declared order and materializes each child with the exact game constructor
`new AbilityData(parentData, childBlueprint)`. `KingmakerAbilitySelection`
therefore retains both the source `AbilityData`/parent blueprint and the
concrete child `AbilityData`; the normalized `AbilityKey` stores the parent in
`BaseAbilityGuid` and the chosen child in `VariantGuid`. Ordinary abilities keep
an empty variant GUID and their prior path.

Each concrete child independently traverses the existing branch-preserving
action graph and eligibility policy. The unresolved parent is not emitted, and
encountering a child both through its parent and independently is deduplicated
by the full provider key. The UI aggregation identity for a variant is
`variant|parent-guid|child-guid`, so sibling choices cannot collapse merely
because they apply the same buff. Caster, spellbook, metamagic, prepared token,
resource, and material state remain on provider snapshots rather than being
folded into catalog identity.

The formatter uses localized parent and child blueprint text only. It renders
the complete parent plus the localized distinguishing child text, retains the
parent in search metadata, and groups siblings by localized parent, parent GUID,
then declared variant order. Child icons are preferred with a parent fallback.
Catalog cards use measured wrapped text and per-row variable heights; ordinary
short names retain the compact baseline while long/localized names move the
availability and configuration controls down instead of clipping or adding an
ellipsis. Selected-detail and description views also use wrapped, overflowing
full names.

Persistence continues to serialize stable `AbilityKeyProfile` fields. A saved
concrete selection round-trips both parent and child GUIDs. A legacy parent-only
assignment migrates only when exactly one currently eligible child exists;
otherwise it remains unsupported and produces a localized reselection notice.
No energy type or first declared child is inferred.

## 0.0.13 Powerful Change enhancement boundary

Powerful Change is an optional compatibility adapter, not a compile-time
gameplay-mod dependency. `BrownFurPowerfulChangeCompatibility` resolves the
proven live feature and six score activatables by centralized stable identities,
then validates every activatable's marker buff and `ResourceSpendType.Never`
Arcane Reservoir component before publishing a domain snapshot. Missing or
changed optional blueprints contribute no enhancement and do not affect native
or metamagic-rod discovery.

`KingmakerPowerfulChangeBlueprintAnalyzer` reuses the bounded native action-graph
adapter to identify resulting buffs. It inspects their component semantics and
passes normalized carrier evidence to the Unity-free
`PowerfulChangeEligibilityClassifier`. Qualification requires a genuine
Transmutation spell, the exact Arcanist casting spellbook, an applied buff, and
a supported positive bonus to the selected ability score. It does not match
spell display names or maintain an ability-spell GUID allowlist. The original
effect expression retains its conditional branches; this classification only
answers whether the external provider's proven modifier adapter supports a
score carried by the cast.

`CastEnhancementSnapshot` owns exact caster/spellbook/selected-variant
applicability, a shared usage-pool identity, and a native-command requirement.
`CastPlanner` reserves finite uses by pool, so the six score choices consume one
logical reservoir rather than six independent counters. Persistence continues
to store only stable enhancement IDs.

Execution activates the exact native score toggle through
`KingmakerCastEnhancementAdapter`. A selected Powerful Change step is routed to
`UnitUseAbility` even in Instant mode because only that native command invokes
the optional provider's cast transaction. Buff Planner never edits the target's
stats and never debits the reservoir itself. The provider changes the original
modifier, preserves its descriptor, commits the exact debit, and consumes its
one-shot selection. Failed/canceled casts restore prior activation state; a
consumed one-shot group is not resurrected.

## Test/runtime evidence boundary

Production runtime requests remain confined to the guarded external `RuntimeTestProtocol.EvidenceRoot`. Protocol unit tests inject a distinct unique directory beneath the operating-system temp root and separately test that the production root rejects those paths. The console entry point owns resolver registration and fixture disposal through completion; it prints PASS only after cleanup. Infrastructure setup failures are reported to stderr with exit 2, so they cannot escape as unhandled CLR exceptions or masquerade as successful tests.

## 0.0.12 retryable HUD lifecycle

`HudInstallInvalidationGate` now coordinates explicit `HudInstallAttemptResult`,
`HudCandidateTickResult`, and `HudInstallationState` values. Host appearance,
reactivation, replacement, lifecycle callbacks, enable, and the planner hotkey can
request an attempt. A temporarily unready inner HUD, expired candidate, or stale
hosting chain schedules at most one retry per 30 active-HUD frames. An absent
outer HUD, a live provisional candidate, and a stable installation dispatch no
hierarchy discovery on unchanged frames.

Area unload and mod disable place the coordinator in `Suspended`, so observing a
still-active or transient outer HUD cannot undo cancellation. Area load or enable
explicitly resumes it. Candidate expiry and staleness are returned to
`BuffPlannerUiRoot`; they are no longer private self-destruction events that can
consume the only request.

`BuffPlannerHudButtonController` receives the already-observed active
`UISectionHUDController` and resolves `IngameMenuController` only below it. Its
steady-state liveness check uses held references: the owned root must exist, have
the native cluster as parent, be active, and remain under the active HUD; the
inner anchor and cluster must exist, remain active, and belong to that HUD; and
the native raycaster must remain active. This constant-time reference validation
does not reacquire the hierarchy. A failure destroys only the owned
`KingmakerBuffPlanner.HudButtons` tree and re-arms scoped discovery.

The two-frame readiness delay and 120-frame placement, left-alignment,
glyph-centering, and top-raycast ownership validation remain unchanged in
meaning. Validation failures are logged at bounded milestones with the exact
first failing predicate. Lifecycle snapshots expose no-HUD, retry, candidate,
installed, expired, stale, and suspended states plus identities and counters.

The intake proof is in
`planning/HUD-LIFECYCLE-REGRESSION-ROOT-CAUSE.md`.

## 0.0.11 invalidation-driven HUD lifecycle

The UMM composition root remains frame-driven because it must poll the configured hotkey and advance active execution/UI state. Native HUD discovery is no longer frame-driven. `BuffPlannerUiRoot` holds a `HudInstallInvalidationGate` and observes only the exact `StaticCanvas.HUDController` identity and active state. Enable, planner-hotkey, area-loaded, scenes-loaded, loading-complete, area-activated, host replacement, and host reactivation mark discovery dirty; unloading cancels it. An unchanged or absent host never consumes/repeats the pending request.

`BuffPlannerHudButtonController` resolves `IngameMenuController` only below the active `UISectionHUDController`. The global Unity object graph is not a UI adapter. Once a candidate exists, the established two-frame presentation validator, native formation-button anchor, hit ownership, listeners, tooltip, and pointer region remain unchanged. `Hud.Tick` may maintain already-owned controls, but it does not reacquire the native host.

The performance observer is an opt-in runtime-testing boundary. Ordinary runs execute only disabled guard branches. A strict `performance-probe` request enables `Stopwatch` aggregates, one-second samples, camera motion/state observations, and optional diagnostic suppression for causal bisection; it does not alter frame/time/camera settings.

The full root-cause and exact A/B are in `planning/PERFORMANCE-REGRESSION-ROOT-CAUSE.md`.

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
