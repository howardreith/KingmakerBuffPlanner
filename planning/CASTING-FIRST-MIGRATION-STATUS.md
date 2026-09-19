# Casting-First Migration — Checkpoint Status

Single source of truth for the casting-first migration mission progress.
Linked from `AUTONOMOUS-RESUME.md`. Specification: the adopted
`Kingmaker-Buff-Planner-Casting-First-Migration-Charter.md` (casting-first
migration and native scroll UI charter v1.0, 2026-09-19).

## Phase 1 checkpoint 1 — casting contract core — 2026-09-19 (CURRENT)

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
