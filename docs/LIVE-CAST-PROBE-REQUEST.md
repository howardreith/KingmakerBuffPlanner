# First live casting probe — approval REQUEST (not executed)

Status: **REQUEST ONLY. Nothing here has run.** Native dispatch stays
disabled in every build on this branch. This document asks the owner for
one separate, explicit approval; without it no part of section 3 is
built into a runnable scenario and no cast is attempted.

Prepared on branch `codex/kingmaker-buff-planner-casting-first` after
review K1–K7 were answered (K6 at `b243b02`). The approval should name
the exact commit the probe code lands on.

## 1. What would be approved

Exactly **one native cast** of **one discovered spell** by **one party
caster** on **one other party member**, out of combat, in the approved
`KBP_AUTOMATION_WORKING` fixture, through the casting-first path:

authored casting → compiler → apply gate → `ExplicitCastingStepConverter`
with `ExplicitProjectionScope.SingleCastProbe` → the exact projection
(its `ProjectionId`) → `ExplicitCastingRunCoordinator` with
`maximumSubmissions = 1` → the existing, unchanged `InstantCastExecutor`
and `KingmakerInstantCastAdapter`.

Not included: rods, metamagic, any enhancement, targeting modifiers
(Share Transmutation and similar), group or mass castings, material
costs, a second casting, retries, combat, ordinary saves, the owner's
real campaigns, or any global "enable native casting" setting.

## 2. Exact identity the run must prove before the cast

| Item | Required value |
| --- | --- |
| Source commit | the approved commit, equal to the build manifest commit; clean tree |
| Package | fresh `Build-Local.ps1` ZIP; SHA-256 recorded in the request receipt |
| DLL | SHA-256 and module MVID recorded; the runtime host logs the same MVID |
| Run id | fresh, `casting-probe-<yyyyMMdd-HHmmss>` |
| Fixture | `Manual_403_KBP_AUTOMATION_WORKING.zks` (only mutable fixture); `Manual_401_KBP_AUTOMATION_SEED.zks` and every other save byte-identical before and after |
| Candidate | isolated: the probe document is written by the run into the run's own staging `UserSettings`; **no automatic legacy import** (the run starts from a probe-only document and never calls the migration service) |
| Compatibility profile | `full-user` (the proven profile) |
| Deployment | the existing transactional stage/restore (`Enter-KbpRuntimeTransaction`), with restoration verified against the original Mods manifest |

## 3. Code the approval would add (none of it exists yet)

1. A `live-cast-probe` scenario in the launcher and runtime host. The
   capability is **run-scoped**: the host receives it only through that
   run's protocol file and only for that run id; no setting, hotkey or
   UI element can enable it.
2. A probe dispatch boundary used only by that scenario. It accepts a
   projection only when `Scope == SingleCastProbe`, `Converted`, exactly
   one casting, and a `ProjectionId` equal to the id computed at review
   time. Any other input is refused and recorded; the production
   workspace keeps `DisabledCastingDispatchBoundary`.
3. Discovery-driven choice, not hard-coded content: the host picks the
   first party caster with a discovered direct-target buff spell (not
   personal range) that has a native slot or cast available, no material
   component and no enhancement applied, and a legal second party member
   as target. The chosen caster, source, spellbook,
   level and target are written to the evidence **before** the cast, and
   the run fails closed as `Not Run` if none qualifies.

## 4. Observation and stop rules

- **Before:** target buff list (the expected effect absent or its
  duration recorded), caster's spellbook resource for that source
  (slots/casts remaining), in-combat flag false, both units conscious
  and in range.
- **Submit once** through the coordinator; `maximumSubmissions = 1`.
- **After:** the expected effect present on the target, the caster's
  resource decreased by exactly the reserved units, no other unit's
  buffs or resources changed, no residual command/animation state.
- **No retry** on any outcome. A validation refusal, submission
  failure, unconfirmed effect, timeout or residual state ends the run
  with that outcome recorded (the K5 halt).
- **Deadline:** the scenario's `TimeoutSeconds` (proposed 600). On
  expiry: no further input, owned game exit, restoration.
- **Cancel:** the existing run-bound stop marker ends the run before the
  submit if present; after the submit it only shortens observation.
- **Exit and restoration:** owned exit of the game process, Mods tree
  restored and compared to the original manifest, fixture hashes
  compared, `deployment.lock` released, transaction `Restored`.
- The WORKING fixture is **not saved** by the scenario; the gameplay
  effect lives only in the process that is then exited.

## 5. Negative gates already proven in source tests

These run in the unit/protocol suite on every gate (recording runtimes,
not gameplay):

- `converter-refuses-unsupported-contracts`: an enabled targeting
  modifier, an exact-source enhancement, and incomplete required group
  coverage each refuse the whole projection.
- Probe-scope cases in the same suite: two castings, a fully covered
  group casting, an enhancement, a (disabled) targeting-modifier
  selection and a material cost each convert or are allowed under the
  standard scope (hard fixture preconditions) yet are refused under
  `SingleCastProbe`; one plain direct casting is accepted with a
  deterministic 64-hex `ProjectionId`. Each probe refusal was
  mutation-checked: removing it fails the test.
- `explicit-run-stops-after-failure-both-modes`: in instant and animated
  modes, a failure of casting 1 (validation, enhancement preparation,
  thrown submission, rejected submission or unconfirmed effect for
  instant; validation, enhancement, thrown start, failed operation or
  timeout for animated) leads to zero validation or submission of
  casting 2 and no retry of casting 1; `maximumSubmissions = 1` stops
  the second casting of a clean plan.

## 6. What a PASS would and would not mean

A PASS proves one native cast of one plain spell through the
casting-first projection on the WORKING fixture, and clean restoration.
It does not qualify rods, enhancements, modifiers, groups, multi-cast
routines, animated mode in gameplay, or any release.

## 7. Approval wording requested

"Approved: one live-cast-probe run on commit <sha>, WORKING fixture,
single direct-target cast as described in LIVE-CAST-PROBE-REQUEST.md."
