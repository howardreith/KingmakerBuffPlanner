# Definition of Done Matrix

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
| Complete setup, target matrix, provider controls, search/filter/sort, routines, preview/results | FAIL — REPAIR ACTIVE | Human playtesting found a floating prototype rather than an opaque full-screen service window |
| Three HUD executions; resolution/scale; no stale subscriptions | FAIL — REPAIR ACTIVE | Prior gate required zero blockers/subscriptions and did not dispatch pointer input; click-through and silent Long were observed |

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
| Mandatory no-save scenarios | PASS | Load, catalog, UI, optional catalog, composed core |
| Mandatory save-backed scenarios | DEFER — EVIDENCED | Scenario execution cannot be safely bound without baseline/working fixture |
| Applicable core suite twice in fresh processes | PASS | `phase12-no-save-core-1/2`, 22/22 each |
| Clean source/deterministic build/package | PASS at checkpoint | 15/15; 52/52; two identical builds; package 4/4 |
| Install/use/architecture/discovery/execution/qualification docs | PASS | `docs/` set and reports |
| Release ZIP from clean HEAD | PASS at checkpoint; final-head rerun pending | `scripts/Build-Release.ps1` |
| Coherent local history | PASS | Dedicated feature branch and checkpoint commits |
| Remote branch equals local | IN PROGRESS | Guarded push WhatIf 6/6; actual helper push pending final commit |

## Optional rows

| Requirement | Status |
|---|---|
| Shield Other exact proof | FEATURE-NOT-PRESENT-IN-SNAPSHOT |
| Optional combined profile | UNAVAILABLE-LOCAL-REFERENCE |
| Consumables, combat-start automation, reserves, controller navigation | NOT APPLICABLE |

## Remaining hard-stop condition

No project-owned `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair exists. Every present save is protected. Once final clean-head packaging, no-save repetition, and guarded branch publication finish, section 26.2 applies: baseline identity cannot be distinguished from working because neither authorized fixture exists. The mission cannot truthfully reach `COMPLETE` without importing or creating that guarded pair under future explicit authority.
