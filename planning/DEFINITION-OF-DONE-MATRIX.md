# Definition of Done Matrix

## 0.0.7 parchment/BubbleBuffs-inspired presentation

| Criterion | Result | Exact evidence |
|---|---|---|
| Preserve frozen MVP mechanics | PASS | Source 30/30; behavior/protocol 63/63; harness 8/8; deployment WhatIf 5/5 after every phase. |
| Native theme inventory/fallbacks | PASS | `planning/PARCHMENT-BUBBLEBUFFS-UI-FORENSICS.md`; runtime theme resolution names exact background/button/toggle/portrait/font candidates. |
| Icon-first alphabetical catalog | PASS | Final live diagnostics: 11 bound cards, 11 blueprint icons, 0 fallbacks; default all supported non-hidden, alphabetical. |
| Portrait target editor | PASS | Bless screenshot shows selected green portrait/check; preview and bulk-save behavior have deterministic tests. |
| Simplified controls/disclosure | PASS | 1 Casting mode control, 0 retired technical labels; Casting Source collapsed and advanced controls separately captured. |
| Five visual evidence states | PASS | Catalog, selected details, target colors, collapsed Casting Source, and advanced settings PNGs in both final physical run directories. |
| Animated and Instant | PASS | `ui-polish-0.0.7-animated-1` and `ui-polish-0.0.7-instant-1`: 71/71 each; planned/submitted/confirmed 1/1/1. |
| Native and Call of the Wild | PASS | `ui-polish-0.0.7-native-final` 12/12; `ui-polish-0.0.7-cotw-final` 26/26; exact restoration. |
| Human visual acceptance | PENDING HUMAN | Automation cannot decide native feel, clipping, responsiveness, or subjective legibility. |
| Merge/public release | NOT AUTHORIZED | Feature branch and local package only. |

Final release source/package/DLL/MVID: `2f125f9f1024692d83a1b2570209d1858d62eff1` / `9feed6dffa668812ed826c75b743d72892e6e8371b0f81585fb557aea8fcf453` / `bf8c72874377d56f91bcdb6daedaa8b28b340a948aee06583a32954d61b38927` / `966b7d8f-bd5f-46b9-beda-62774f82ccac`. Guarded install `ui-polish-0.0.7-install` passed with settings preserved and all other mods verified unchanged.

## 0.0.6 live row-rendering recovery

| Criterion | Result | Exact evidence |
|---|---|---|
| Independent renderer-path audit | PASS | A/B canary screenshots `4b3f7e05...` absent and `71a6bbf...` visible; full mask/stencil/renderer/font/material/canvas evidence. |
| Production rows and details visible | PASS | `production-3` and `production-4`, 71/71 each; screenshot `cb234368...`; ten named rows and Bless details visibly rendered. |
| Pixel proof independent of UI booleans | PASS | Five row luminance ranges `162,159,184,163,157`; details title `125`; exact rectangles/hash in `live-row-pixel-evidence.json`. |
| Canary removed | PASS | Production diagnostics `canary=absent`; source validator rejects canary construction; final screenshots contain none. |
| Bless generic accounting | PASS | Native data `require=False,item=none,count=1,hasEnough=False`; lazy gate test; live confirmed/spent `1/1` twice. |
| Preserve accepted UI/input/lifecycle | PASS | Both final physical runs retain four HUD controls, five-second tooltip, zero world/selection/camera/native leakage, 21 cycles, balanced close/restoration. |
| Discovery and compatibility | PASS | Counts remain 11/11; native 12/12; Call of the Wild 26/26, zero unsupported/overlap. |
| Package/install | PASS | Source `e656812`; deterministic package `ce7492b2...`; DLL `6144256c...`; MVID `bff11809-...`; guarded install preserves settings/other mods. |
| Public release | NOT AUTHORIZED | Package is local-only; no merge or publication performed. |

## 0.0.5 catalog/HUD/input/tooltip repair

| Criterion | Result | Exact evidence |
|---|---|---|
| Preserve live bootstrap/modal | PASS | Both final physical runs: four ordered HUD buttons, physical F10 armed/observed, opaque 100% screen coverage, balanced close/restoration. |
| Populate live catalog/details | PASS | 11 entries -> 10 available VMs -> 10 instantiated/active rows -> 5 viewport-visible; details bound; no binding failure. |
| Bless generic vertical slice | PASS | Blueprint `90e59f4a4ada87243b7b3535a06d0638`; spellbook source; provider 1; prepared/available; visible/active row with non-zero bounds; selectable/configurable. |
| Filter/empty recovery | PASS | Default available/non-hidden behavior plus deterministic all-hiding diagnostics and Reset Filters; filter counts and active filter labels are explicit. |
| HUD/modal input ownership | PASS | Physical deltas player/move/ability/selection/target all 0; selection/camera unchanged; underlying native activation 0. |
| Tooltip lifecycle | PASS | Five-second hover, one enter delta, four listeners, zero raycast graphics, no blocking, bounds fully in screen. |
| Quick-action feedback/confirmation | PASS | Three exact empty-group outcomes; configured Bless refused exactly at material validation, submitted/confirmed 0, no false success. |
| Rebuild/close cleanliness | PASS | 21 cycles; 22 creates/22 destroys after close; no duplicate root or stale input lease. |
| Disposable saves and Mods safety | PASS | Only exact Working loaded; baseline `afca8ac5...` immutable; every final transaction restoration verified. |
| Compatibility | PASS | Final native 12/12; Call of the Wild 26/26. |
| Package/install | PASS | Deterministic 2/2 local-only ZIP; guarded install exact; settings preserved; other mods unchanged. |
| Human visual confirmation | PENDING HUMAN | Installed 0.0.5 awaits authoritative check for smooth tooltip appearance and ordinary interaction. |

Qualified source: `390bb8b5f514a38edf1c553962813e29a1b526fd`. Package: `3eba3158aa92a6b66e249ec35aa297500eb4c5decdf73974c26992219922349c`. DLL: `6999284085bd6898f6bd871900783f6f81343a6f801b2d2c95acd208c6513b56`. MVID: `d2fed415-bfa2-47a7-90ba-f50fa8d1c7de`.

## 0.0.4 live-bootstrap recovery correction

The live-bootstrap recovery rows are complete and supersede stale “no authorized save” or “campaign pending” text retained below as project history. The explicit baseline/working pair exists. Runs `bootstrap-0.0.4-human-live-6` and `bootstrap-0.0.4-human-live-7` passed 65/65 each in fresh processes and proved one four-button HUD row, independently observed physical F10, visible planner presentation before input acquisition, 21 balanced open/close cycles, no duplicates/click-through/world input, immutable baseline, and exact Mods restoration. Native 12/12 and Call of the Wild 26/26 exact-release regressions also pass. Release `5b96f3b` is deterministic, local-only, and guarded-installed with settings preserved and other mods unchanged.

Broader prepared/spontaneous resource/effect/executor combinations remain under their existing `DEFER — EVIDENCED` rows; they do not reopen this bounded recovery mission.

R2 correction notice: direct human testing invalidated both 0.0.2 UI rows. Fresh retained-mode HUD, transactional modal, and confirmed-execution behavior are implemented and guarded-installed in 0.0.3. The campaign gate is honestly `BLOCKED` without an authorized `KBP_` campaign, so direct human HUD/modal/input/Bless acceptance remains pending. Historical 0.0.2 PASS/implemented text below is superseded where it conflicts.

Status: IN PROGRESS — human playtesting invalidated the 0.0.1 UI gate; full-screen UI/input repair is active; save-backed core rows remain `DEFER — EVIDENCED`

This matrix follows section 27 of the authoritative mission. A core row marked `DEFER — EVIDENCED` is still unmet and prevents `COMPLETE`; it is not being reclassified as optional.

## Standalone identity

| Requirement | Status | Evidence |
|---|---|---|
| Independent repository, assembly, UMM ID, namespace, persistence, package | PASS | Exact origin/branch; runtime identity twice; package allowlist |
| No modification/dependency on Tabletop Added Rules or Gunslinger | PASS | No compile reference/payload; unavailable report; Git/file audit |
| No third-party/game DLL payloads | PASS | Package validation 4/4; four-file exact allowlist |

## Native and mod-added discovery

| Requirement | Status | Evidence |
|---|---|---|
| Full native candidate catalog and classification | PASS | 1,722 abilities; 974 candidates; 413 included; 561 excluded; 0 unsupported |
| Ordinary native buffs dynamically discovered | PASS | Structural action contracts; byte-identical guarded catalogs |
| Exceptions bounded and counts reconcile | PASS | 396 automatic, 1 wrapper, 13 adapters, 3 overrides; strict registry |
| Mod abilities present after optional mod load | PASS | Exact Call of the Wild load; 7,342 owned abilities after load |
| Ordinary mod buffs require no per-spell code | PASS | 2,008 automatic optional inclusions; representative Dazzling Blade |
| Unknown wrappers fail closed with diagnostics | PASS | Bounded wrapper diagnostics; 61 classified wrappers; 0 unsupported |
| Exact available optional evidence | PASS | Call of the Wild twice; Tabletop/combined `UNAVAILABLE-LOCAL-REFERENCE` |
| Runtime party scan sees a mod-added prepared/spontaneous provider | DEFER — EVIDENCED | No authorized `KBP_` party/save fixture |

## Planning, resources, effects, and targets

| Requirement | Source behavior | Runtime proof |
|---|---|---|
| Prepared slots, domain/special slots, opposition/linked slots | PASS | DEFER — EVIDENCED: no `KBP_` save |
| Spontaneous shared pools and cantrips | PASS | DEFER — EVIDENCED: no `KBP_` save |
| Variants/metamagic identities and sharing | PASS | DEFER — EVIDENCED: no `KBP_` save |
| Resource abilities and material components | PASS | DEFER — EVIDENCED: no `KBP_` save |
| Deterministic priority/ban/cap and mass single cost | PASS | DEFER — EVIDENCED: no `KBP_` save |
| Self/friendly/pet legality and invalid-target no-spend | PASS | DEFER — EVIDENCED: no `KBP_` save |
| Sticky touch safe fallback and bounded area behavior | PASS | DEFER — EVIDENCED: no `KBP_` save |
| Weapon/item enchantment and typed active-effect presence | PASS | DEFER — EVIDENCED: no `KBP_` save |

The source behaviors use realistic immutable provider/resource/effect fixtures and exact installed-assembly adapters. They are not mislabeled as live resource/effect proof.

## Persistence and UI

| Requirement | Status | Evidence |
|---|---|---|
| Versioned external per-campaign JSON; no save dependency | PASS | Schema 2; hashed campaign key; mod-local `UserSettings` |
| Atomic write, bounded valid backups, malformed recovery, migration | PASS | Behavior suite |
| Party reorder pure proof | PASS | Stable-ID model/profile behavior |
| Save/reload and live party reorder proof | DEFER — EVIDENCED | No authorized `KBP_` save |
| Complete setup, target matrix, provider controls, search/filter/sort, routines, preview/results | IMPLEMENTED — HUMAN PASS PENDING | 0.0.3 retained-mode full-screen uGUI service view is installed; human verdict required |
| Three HUD executions; resolution/scale; no stale subscriptions | IMPLEMENTED — CAMPAIGN/HUMAN PASS PENDING | Fresh buttons/top-hit proof, transactional input lease, corrected structured gate; no authorized campaign fixture |

## Execution

| Requirement | Source behavior | Runtime proof |
|---|---|---|
| Animated native-command executor | PASS | DEFER — EVIDENCED |
| Instant native-rule/native-spend executor | PASS | DEFER — EVIDENCED |
| Exact slot/resource/component spend; failure no-spend | PASS | DEFER — EVIDENCED |
| Representative prepared/spontaneous executor equivalence | PASS as narrow fake contract | DEFER — EVIDENCED |
| Bounded batching and no leaked state/subscriptions | PASS | UI subscription gate PASS; live casts deferred |

## Automation and release

| Requirement | Status | Evidence |
|---|---|---|
| Request/result runner and transactional profile staging | PASS | Protocol 52/52; transaction 6/6 |
| Exact restore and protected-save policy | PASS for every executed run | All transactions restored; no protected save accessed |
| Mandatory no-save scenarios | PASS except campaign UI | 0.0.3 native 12/12 twice; Call of the Wild 26/26 twice; corrected UI gate blocks at main menu |
| Mandatory save-backed scenarios | DEFER — EVIDENCED | Scenario execution cannot be safely bound without baseline/working fixture |
| Applicable core suite twice in fresh processes | PASS | `phase12-no-save-core-1/2`, 22/22 each |
| Clean source/deterministic build/package | PASS at checkpoint | 15/15; 52/52; two identical builds; package 4/4 |
| Install/use/architecture/discovery/execution/qualification docs | PASS | `docs/` set and reports |
| Release ZIP from clean HEAD | PASS | 0.0.3 deterministic 2/2; package `42f823d6...`; guarded installed |
| Coherent local history | PASS | Dedicated feature branch and checkpoint commits |
| Feature branch publication | PASS at evidence checkpoint | Guarded helper verified local/remote `fc94060a861db6356a5bdb8d2520f377ec52b0c5`; no main merge or public release |

## Optional rows

| Requirement | Status |
|---|---|
| Shield Other exact proof | FEATURE-NOT-PRESENT-IN-SNAPSHOT |
| Optional combined profile | UNAVAILABLE-LOCAL-REFERENCE |
| Consumables, combat-start automation, reserves, controller navigation | NOT APPLICABLE |

## Remaining hard-stop condition

No project-owned `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair exists. Every present save is protected. Once final clean-head packaging, no-save repetition, and guarded branch publication finish, section 26.2 applies: baseline identity cannot be distinguished from working because neither authorized fixture exists. The mission cannot truthfully reach `COMPLETE` without importing or creating that guarded pair under future explicit authority.
Live-bootstrap correction (2026-08-12): 0.0.3 is HUMAN FAIL, not UI complete. UMM load and no-save/catalog passes do not prove campaign HUD or F10 initialization. The 0.0.4 source repair passes 23/23 validation and 59/59 behavior/protocol tests; all live UI rows remain PENDING until two exact disposable-save fresh-process runs pass.
