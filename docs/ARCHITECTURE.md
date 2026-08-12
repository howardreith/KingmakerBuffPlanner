# Architecture

Status: R2 implementation installed; authoritative campaign UI and save-backed acceptance pending

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
