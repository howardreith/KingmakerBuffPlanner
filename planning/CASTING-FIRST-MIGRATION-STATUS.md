# Casting-First Migration — Checkpoint Status

Single source of truth for the casting-first migration mission progress.
Linked from `AUTONOMOUS-RESUME.md`. Specification: the adopted
`Kingmaker-Buff-Planner-Casting-First-Migration-Charter.md` (casting-first
migration and native scroll UI charter v1.0, 2026-09-19).

## Phase 2 checkpoint 3 — shared atomic budget reservation — 2026-09-19 (CURRENT)

**Branch** `codex/kingmaker-buff-planner-casting-first`, on top of
checkpoint 2 (`df6d04b`). Version remains `0.1.1-rc3`.

### Implemented (production code)

- `Planning/CastingBudget.cs` — the charter §5.1 shared-budget layer:
  - `CastingBudgetLedger` normalizes native slots (shared units and
    linked prepared-token pairs via the existing `ResourceLedger`),
    enhancement usage pools (shared reservoirs take the minimum of
    reported balances, never a per-item sum; all-unknown pools stay
    null/unknown — unknown is never zero), and material components
    (per-item availability decremented across castings).
  - Each Ready casting's **complete cost vector reserves atomically**:
    every component is validated before any balance mutates; the native
    reservation commits first so a late native surprise cannot follow an
    already-committed enhancement or material charge. A deficit blocks
    the casting (`enhancement-pool-exhausted`,
    `resource-pool-exhausted`, `prepared-slots-exhausted`,
    `material-unavailable` with have<need detail) and reserves nothing
    anywhere.
  - `CastingBudgetLine` per pool: available now (nullable for unknown),
    requested/allocated demand, unmet demand, forecast remaining, and
    traces naming the responsible casting IDs; unfunded demand is
    recorded so deficits and competing castings stay visible.
- `ExplicitCastingCompiler` now runs the budget pass over Ready castings
  in persisted order; `ResolvedCasting.Cost` carries each casting's
  reserved cost lines and `ExplicitCastingPlan.BudgetLines`/
  `BudgetLineFor` expose the authoritative per-pool read model.

### Verified behavior (deterministic domain layer)

New tests: `casting-a07-shared-enhancement-pool-is-atomic` (A07),
`casting-a08-complete-cost-reservation-leaks-nothing` (A08: linked
prepared pairs + material + rod; the rod-deficient candidate reserves
nothing and later castings still receive tokens and materials).

Full gate: **source 42/42; protocol 195/195; harness 27/27; package
4/4; WhatIf 5/5; rollback 4/4; publisher 3/3**
(`artifacts/casting-first-checkpoint3-gate.log`). Domain-layer evidence
only; no live reservation or native spending is claimed (A07's live
lane remains open per the rc3 blockers).

### Acceptance matrix standing (A01–A20)

A01–A04, A07, A08: PASS (domain layer). A12 import half: PASS (domain
layer, checkpoint 2). A05, A06, A09–A11, A13–A20: NOT RUN.

### Next executable step

Exact-source identity plumbing for enhancement selections (persisted
`ExactSourceRef` validation, still honestly pooled until a durable
contract exists), then the selected-run / one-pass routine forecast
views on the same budget lines (A09), ahead of the Phase 3 native
donor inventory which is now unblocked by the automation fixture.

## Phase 2 checkpoint 2 — schema-5 → schema-6 import converter — 2026-09-19 (commit `df6d04b`)

**Branch** `codex/kingmaker-buff-planner-casting-first`, on top of Phase 1
checkpoint `4398588`. Version remains `0.1.1-rc3`.

### Implemented (production code)

- `Persistence/CastingPlanImporter.cs` — the charter §7.2 converter:
  - **Pinned single-target children** split into one casting per
    recipient preserving target order, enhancements (with pooled-rod
    provenance note, no invented exact identity), source-level
    existing-effect policy and ignored markers (moved into each
    casting), and relative routine order driven by the legacy explicit
    per-routine child order.
  - **Automatic children** become Draft castings with targets preserved
    and an unresolved caster — never today's best caster silently
    pinned.
  - **Group children** import as exactly one Draft group casting with
    required coverage preserved and origin/count marked
    pending-review in provenance (even with a pinned caster).
  - **Idempotency**: casting IDs derive deterministically from legacy
    provenance (`m5:<assignmentId>:<recipient|group>`); re-import onto a
    document already holding those identities reuses them (mapping
    disposition `reused`) and merging routine-interleaved legacy work
    keeps the routine-major persisted-order invariant with re-derived
    orders.
  - **Target-less legacy children** cannot become castings in the
    explicit model; they stay visible as `unresolved-no-recipient`
    mappings plus warnings instead of phantom records or silent drops.
  - **Report**: legacy routine/child counts, resulting castings,
    ready/draft split, unresolved casters, group reviews, pooled
    enhancements, provider-policy notices, per-child mappings,
    de-duplicated warnings.
- `Domain/Authoring` gained `PlannedCasting.WithOrder` (internal); the
  authoring service and test fixtures now share it instead of
  duplicating the rebuild constructor.

### Verified behavior (deterministic domain layer)

New tests: `casting-import-splits-pinned-single-target`,
`casting-import-automatic-becomes-review-drafts`,
`casting-import-group-preserves-coverage-for-review`,
`casting-import-is-idempotent-and-orderly`,
`casting-import-report-counts-honestly` (A12 import half; A13's
corruption/rollback side is covered by the checkpoint-1 repository
tests).

Full gate: **source 42/42; protocol 193/193; harness 27/27; package
4/4; WhatIf 5/5; rollback 4/4; publisher 3/3**
(`artifacts/casting-first-checkpoint2-gate.log`). Evidence layer:
deterministic domain fixtures over the real
`CastingPlanImporter`/`BuffPlannerProfile` types — no player profile
was read or converted, and no migration of live data has run.

### Defects found and fixed during the checkpoint

- First converter draft produced all import orders as 0 (document
  invariant caught it) and attempted a phantom DirectTarget casting
  for target-less children (domain invariant caught it); both were
  redesigned before commit rather than weakening the invariants.

### Acceptance matrix standing (A01–A20)

A01–A04: PASS (domain layer, checkpoint 1). A12 import half: PASS
(domain layer, this checkpoint; live migration rehearsal remains).
A05–A11, A13–A20: NOT RUN. Runtime/visual evidence: none claimed.

### Next executable step

Phase 2 continuation: shared atomic budget reservation in
`ExplicitCastingCompiler` (full cost vectors across native and
enhancement pools, no partial reservations on failure — A07/A08),
then exact-source identity plumbing for enhancement selections.

## Phase 1 checkpoint 1 — casting contract core — 2026-09-19 (commit `4398588`)

**Branch** `codex/kingmaker-buff-planner-casting-first` (descendant of
`codex/kingmaker-buff-planner-z-native-assignments` at
`c182061354e9e761c09648ca779ab334588ba379`, the exact charter-reviewed
commit; clean tree at start). Version remains `0.1.1-rc3`; legacy schema 5
is unchanged and still authoritative for the shipped UI.

### Baseline receipt (Phase 0)

- HEAD `c182061` == charter review commit; no dirty state; no worktrees
  beyond the primary; `main` not merged.
- Baseline gates before any edit (log
  `artifacts/baseline-source-gate.log`): source 42/42; protocol 181/181;
  harness 27/27; package 4/4; WhatIf 5/5; rollback 4/4; publisher 3/3 —
  exactly matching the rc3 record. No pre-existing failures.
- Schema baseline: `BuffPlannerProfile.CurrentSchemaVersion` = 5; new
  casting document adopts schema 6 in separate candidate storage
  (`kingmaker-buff-planner-casting-<hash>.json`), never sharing a writer
  with the schema-5 file.
- **Fixture change since the charter review:** the authorized automation
  save trio now exists in the real save directory (read-only check):
  `Manual_303_KBP_AUTOMATION_SEED.zks`,
  `Manual_304_KBP_AUTOMATION_BASELINE.zks`,
  `Manual_305_KBP_AUTOMATION_WORKING.zks`. The previously documented
  "no automation pair" live-lane blocker is resolved; live lanes for
  later phases (donor inventory, rendered smoke) are actionable through
  the guarded harness. Nothing was launched or staged for this
  checkpoint.

### Implemented (production code)

- `Domain/Authoring/CastingIntentModels.cs` — canonical per-casting
  intent: `PlannedCasting` (stable `CastingId`, routine + explicit order,
  exact `AbilityKey` variant identity, caster/spellbook constraints,
  `CastingTargetMode` DirectTarget/CasterCenteredOrigin/AnchoredOrigin
  with shape invariants, required coverage, targeting modifiers,
  per-casting enhancement selections incl. `ExactSourceRef`,
  per-casting existing-effect policy, Draft/Ready state, migration
  provenance). A Ready casting requires an explicit caster. A
  `CastingPlanDocument` enforces unique IDs, known routines, contiguous
  per-routine orders, and routine-major persisted order.
- `Planning/CastingAuthoringService.cs` — the one mutation authority:
  Add/Update/Remove/Move/SetState commands with disclosed
  `AffectedCastingIds` scope (sibling order shifts are reported, never
  silent), bounded Undo (refused commands consume no undo slot), and
  routine changes routed exclusively through MoveCasting.
- `Planning/ExplicitCastingCompiler.cs` — compiles the document against
  a party snapshot into `ResolvedCasting` records: exactly one resolved
  casting per planned casting (never expanded), distinct blocked
  readiness reasons (caster-unresolved / not-in-party / not-capable /
  spellbook-constraint / pool-exhausted / prepared-slots-exhausted /
  target-mode-mismatch / enhancement-unavailable / exhausted /
  incompatible / exact-source-ambiguous), capability
  (`CapableCasterUnitIds`) separated from current readiness, group
  coverage with derived beneficiaries and visible `CoverageGap`s (no
  automatic second casting), duplicate-effect warnings as diagnostics,
  and required enhancements that block rather than downgrade. Phase 1
  scope is documented in-source: cross-casting atomic budget
  reservation and exact physical item identity are explicitly NOT
  claimed yet (Phase 2).
- `Persistence/CastingPlanProfileModels.cs` +
  `Persistence/CastingPlanRepository.cs` — schema-6 candidate storage:
  versioned per-casting JSON, deterministic serialization, atomic
  writes with rotating backups, duplicate-property rejection, and the
  charter's distinct load states: Absent / Loaded /
  RecoveredFromBackup / UnsupportedSchema (newer OR older-pending) /
  Corrupt. No default profile is fabricated over unresolved data; Save
  refuses to overwrite an unreadable or newer-schema primary.

### Verified behavior (deterministic domain layer)

New tests (registered in `tests/KingmakerBuffPlanner.Tests/Program.cs`):

| Test | Charter scenario |
|---|---|
| `casting-a01-two-casters-three-independent` | A01 |
| `casting-a02-three-recipients-three-invocations` | A02 |
| `casting-a03-group-one-invocation-six-beneficiaries` | A03 |
| `casting-a04-missed-coverage-no-auto-second-cast` | A04 |
| `casting-authoring-scope-undo-and-read-only-compile` | 3.4 editing scope + Undo; 3.1 browsing never mutates |
| `casting-blocked-readiness-reasons-are-distinct` | 3.5 readiness language; capability vs readiness |
| `casting-roundtrip-and-load-states-are-exact` | 4.1 identity/order through save/load; 7.1 load states |

Full suite: **protocol 188/188** (181 pre-existing + 7 new; zero
regressions). Evidence layer: deterministic domain fixtures exercising
the production services — NOT runtime or visual evidence.

### Defects found and fixed during the checkpoint

- `MoveCasting` initially changed persisted position without rewriting
  routine membership (caught by the undo/move regression before commit).
- Test-fixture corrections (order normalization, single provider option
  per ability to respect the exact-source ambiguity guard, backup
  recovery scenario) — fixture bugs, not product changes.

### Acceptance matrix standing (A01–A20)

A01–A04: PASS at the deterministic authoring/compiler layer (this
checkpoint). A05–A20: NOT RUN (targeting-modifier resolution, exact rod
identity, shared atomic budgets, migration import, execution, native UI,
and live lanes are later phases). Runtime/visual evidence: none claimed.

### Next executable step

Phase 1 continuation → Phase 2: extend the compiler with the shared
atomic budget reservation (full cost vectors across native/enhancement
pools), then the schema-5 → schema-6 import converter with the charter's
conversion rules (pinned multi-recipient split, automatic-assignment
drafts, group origin/count review, pooled-rod provenance) and its
import-report tests.
