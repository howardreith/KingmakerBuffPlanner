# Casting-First Migration — Checkpoint Status

Single source of truth for the casting-first migration mission progress.
Linked from `AUTONOMOUS-RESUME.md`. Specification: the adopted
`Kingmaker-Buff-Planner-Casting-First-Migration-Charter.md` (casting-first
migration and native scroll UI charter v1.0, 2026-09-19).

## Checkpoint 12 — workspace visual honesty chain; five runs, every claim re-classified — 2026-09-20 (CURRENT)

HEAD `41893a2`. Gates: source 42/42; protocol 217/217; harness 27/27;
deployment WhatIf 5/5; **launcher -File WhatIf 3/3 (new)**; fixture
3/3; rollback 4/4; publisher 3/3. Every run below used the guarded
launcher, full-user 15-mod profile, and restored verified transactions.

- **casting-ws-visual-183000** (PASS, then corrected): the first
  honest dual-path capture (blackFraction 0.0285, zero retries, both
  paths bit-identical, focused=False) proved the original 30 KB black
  png was a capture-timing artifact — but the frame showed the LEGACY
  catalog screen: the workspace dev-selection enable was dead code
  inside the non-live UI-smoke gate. `workspace-screen-open` had also
  been satisfiable by the legacy screen's `IsScreenOpen`.
- **casting-ws-root-190500** (FAIL by design): first relocation of the
  enable still sat after `Update()`'s live-UI return; the tightened
  gate refused the legacy screen and timed out at phase=1.
- **7c76090** placed the enable before the live-UI dispatch.
  **casting-ws-root-200200** then opened the REAL workspace through
  the production hotkey path (`casting-first workspace
  opened;dispatch=native-submission-disabled`), legacy screen closed,
  all assertions PASS — but the frame showed the plain game HUD: the
  workspace drew nothing despite existing (two-update settle suspect;
  now 750 ms wall-clock).
- **casting-ws-present-204500** (FAIL by design): new
  `workspace-hierarchy-presents` evidence captured a FULLY presented
  hierarchy (active, 1920x1200 under StaticCanvas, canvas enabled,
  alpha 1.00, 19 renderable texts) while the frame was 100% black over
  all 31 attempts.
- **casting-ws-bisect-211500** (FAIL by design, decisive): the
  closed-workspace control frame was ALSO 100% black — the workspace
  does not blacken the screen; the session hit the documented
  intermittent black-presentation defect (menuinput-4/5 precedent;
  suspected RDP-console related, NOT classified). Recorded in
  AUTONOMOUS-BLOCKERS.
- Harness repair on the way: `powershell.exe -File` crashes
  `$PSCmdlet.ShouldProcess` with NullReferenceException (reproduced
  minimally; works under -Command). Launcher repaired with a narrow
  catch + WhatIf-contract fallback (f3d6b48) plus a three-layer
  regression test in Test-SourceOnly (9808102). Eight sibling scripts
  carry the latent defect, recorded not swept.
- Next: casting-ws-bisect-214500 (750 ms settle) classifies the
  workspace draw in a presenting session, or re-confirms the
  presentation blocker; owner may need to control the console/RDP
  session state for a presenting run.

## Checkpoint 11 — first workspace run qualified honestly; visual gate rebuilt — 2026-09-20

Run `casting-ws-final-171029` (live-workspace-qual, full-user 15-mod
profile) completed the full chain for the first time: campaign loaded
through the observed native chain, UMM closed through verified
`UI.Instance.ToggleWindow(false)` (wasOpened=True → nowOpened=False),
planner opened programmatically after the foreground-hotkey fallback,
workspace screen open, evidence written, transaction restored with
receipt. Two honesty defects in that PASS were then found and repaired:

1. The 75 passing assertions were **identity/mod-integrity checks only**
   — no assertion covered workspace-open or visual state; the
   "workspace active" claim lived in stage text.
2. The single async `ScreenCapture.CaptureScreenshot` produced a black
   1920×1200 png (30 KB). Black presentation is the documented
   intermittent focus-correlated state (launchdiag-1 non-black 1.67 MB;
   menuinput-4/5 black on BOTH capture paths), so one engine-path frame
   is not classifiable evidence.

Repair (commit after this checkpoint): the workspace capture now uses
the proven menu-diagnostic machinery — end-of-frame ReadPixels via
`MenuDiagnosticCaptureHost` with luma/blackFraction stats and bounded
black-frame retries (30 × 1 s), engine capture as the second path, a
`workspace-render.json` marker recording `focused`/`runInBackground`/
resolution/fullscreen plus both SHA-256 hashes — and the scenario gained
real assertions that fail closed: `workspace-screen-open`,
`workspace-frame-captured`, `workspace-frame-engine-capture`,
`workspace-frame-nonblack` (blackFraction<0.98; a persistent black frame
now FAILS the run at stage `workspace-visual-validation` instead of
passing on integrity checks alone). Gates at this checkpoint: source
42/42; protocol **217/217**; harness 27/27; fixture-inventory 3/3;
rollback 4/4; publisher 3/3. Next: guarded live re-run to capture
classified visual evidence of the actual workspace.

## Checkpoint 10 — decision-packet exposure + narrowed rod conclusion — 2026-09-20

On top of `e46f18e` (clean; no newer work). Gates re-run: source 42/42;
protocol 210/210; harness 27/27; package 4/4; WhatIf 5/5;
fixture-inventory 3/3; rollback 4/4; publisher 3/3
(`artifacts/casting-first-checkpoint10-gate.log`). Commit `6721d8e`
narrows the exact-rod conclusion per the next-step decision packet:
the serialization/reference path was inspected read-only (ItemEntity is
a Newtonsoft-JSON persisted POCO — blueprint, count, POSITIONAL slot
index, enchantments, facts; ItemsCollection is a positional list;
`JsonUtility.UnitSerialization.Serialize` entry; no Owlcat/ZeroFormatter
assemblies in this build), and the doc now separates runtime identity /
within-save graph references / durable cross-save identity. Established:
no usable native durable per-item identity in the inspected
persisted-state contract — NOT a claim that every strategy is absent.
The per-item persisted `Fact[]` is the named candidate requiring live
proof (fact survival across save/load/reorder + permission to attach);
not implemented. The fixture approval packet
(SHA-256 40e1dab2b5ebe53157a33a298d429f07675894bf60bf3b3515e24f471b23af17
at `e46f18e`) is reproduced INLINE in the checkpoint-10 handoff with the
proposed owner statement — PROPOSED, NOT GRANTED. Bootstrap stays
blocked; the campaign-UI lane awaits that decision. Bundle regenerated
locally at HEAD (10 patches + SHA256SUMS; still LOCAL, not delivered).

## Checkpoint 9 — runtime-qualification continuation: review hardening, narrow checks, fixture packet — 2026-09-20 (commit `e46f18e`)

**Branch** `codex/kingmaker-buff-planner-casting-first`, reconciled at
`9a2e57388316ef1172927f56f715b8927f83a0d5` (clean, single worktree).
Gate at this checkpoint: source 42/42; **protocol 210/210**; harness
27/27; package 4/4; WhatIf 5/5; **fixture-inventory evidence 3/3** (new);
rollback 4/4; publisher 3/3
(`artifacts/casting-first-checkpoint9-gate.log`).

### Review-bundle hardening (local, not delivered)

The 8-patch bundle was validated mechanically: applying it with `git am`
to the declared base `c182061` in an isolated disposable worktree
reproduced the exact source tree `119c24f14fc4caebdd6966993f6df772f73f406f`;
a SHA256SUMS manifest covers every patch. No authorized push/attachment
route exists for this branch, so the bundle remains **local** to the DATA
machine; the applicability proof above replaces path-only claims.

### Narrow integration checks (both were verification questions; no defect)

- **Sibling intent vs derived changes (3.1)** — proven by
  `casting-workspace-sibling-intent-vs-derived-changes`: a card edit
  leaves sibling casting instances reference-identical (authored intent
  untouched) while a sibling's DERIVED readiness may legitimately change
  (shared-budget recalculation after a caster edit/move); a routine move
  changes only the mover's membership plus disclosed necessary order
  renumbering; Undo restores the exact prior serialized document AND the
  prior resolution state.
- **Disabled dispatch is attempt-only (3.2)** — proven by
  `casting-workspace-disabled-dispatch-attempt-only`: the boundary
  records the reviewed identity and refuses (`Submitted=false`; the
  outcome type exposes no expenditure/beneficiary/success surface), the
  limitation is visible before the player acts, a throwing boundary
  releases the in-flight guard so later applies still work, and — new
  production guard — `CastingWorkspaceDevSelection.LegacyExecutionPermitted`
  now refuses every legacy quick-execution entry (screen, HUD, hotkey,
  spellbook) while the workspace is selected, with a logged reason.
  Source wiring is compiled; runtime clickability remains unobserved.

### Prospective fixture-seal hardening (2.2)

`Write-KbpFixtureSealInventory` / `Compare-KbpFixtureInventory` /
`Get-KbpFixtureSealInventory` added to the harness (narrow, additive;
no existing check changed). `Test-FixtureInventoryEvidence.ps1` (wired
into the source gate) proves: a detailed seal explains exact
added/removed/changed paths; matching state reports a match; an
aggregate-only mismatch without a bound inventory stays refused by the
existing guard (insufficient historical evidence — never a pass, never
invented from today's directory). This is prospective only: it does not
recover the August difference and does not authorize the current fixture.

### Owner-approval packet prepared (not granted)

`planning/BAGOFTRICKS-FIXTURE-APPROVAL-PACKET.md`: current inventory
identity (`c4487d11…`, 40 files, 1,805,725 bytes), both re-verified
receipt identities (with the distinction that their approval covers
installation and recorded bytes, not a fixture-use seal), the
unreconstructable historical difference (no role/mutability claim), the
exact three-field binding change proposed, permitted runtime scope,
protected data, recovery procedure, and the explicit request for the
owner's decision. Bootstrap stays blocked; the known-refused invocation
was not repeated.

### A06 exact-rod inspection (bounded, read-only)

`planning/EXACT-ROD-IDENTITY-INSPECTION.md`: member-level reflection of
the installed `Assembly-CSharp.dll` shows `ItemEntity` has NO per-instance
identity member (only descriptors and provenance prose;
`PreSave`/`PostLoad` hooks) and `ItemsCollection` is a positional list —
a durable per-physical-rod identity is absent, not merely unproven, in
the inspected surface. Pooled selections stay honestly unresolved;
affected castings block; A06 remains blocked on a contract that does not
exist in the inspected assembly.

### Campaign UI qualification — status

BLOCKED on the fixture decision (unchanged prerequisite; packet
prepared). No game launch, deployment, screenshot, or audio check was
performed this checkpoint; the workspace view remains
source-integrated-but-not-visually-qualified. That is the precise
remaining gate after the owner decision: bootstrap → donor inventory →
workspace screenshots/input qualification at matching resolution.

### Acceptance matrix (evidence layers, unchanged unless noted)

Domain: A01–A05, A07–A11, A12-import, A13-isolated — PASS. Session
integration: mixed-caster/group/review/persistence/sibling-intent/
dispatch-attempt-only — PASS (deterministic). Unity observed
interactions: NOT RUN. Visual/audio: NOT RUN. Native casting: NOT RUN.
A06: BLOCKED (contract absent in inspected surface). Live A12/A13:
BLOCKED (fixture decision).

## Phase 4 checkpoint 8 — fixture diagnosis + the connected workspace — 2026-09-19 (commit `9a2e573`)

**Branch** `codex/kingmaker-buff-planner-casting-first`, reconciled at
`81c46486cebe6580f753b59334fe7615b1daadbb` (checkpoints 1–7 preserved, clean
tree). Gate at this checkpoint: source 42/42; **protocol 208/208**; harness
27/27; package 4/4; WhatIf 5/5; rollback 4/4; publisher 3/3
(`artifacts/casting-first-checkpoint8-gate.log`). Review bundle regenerated
at `artifacts/review-bundles/casting-first-migration/` (patches 0001–0007 +
manifest; still local — no authorized push route for this branch).

### Fixture drift: precise diagnosis, bootstrap stays blocked

Read-only comparison under the manifest's own inventory rules
(`Get-KbpDirectoryManifest`: path+length+sha256 per file):

- The CURRENT live BagOfTricks directory is **byte-identical** (all 40
  sha256 hashes match) to the per-file manifests recorded by BOTH guarded
  install transactions `install-rc2fix-liveui-3` and
  `install-rc3-published-install-1` (Sept 19 evidence).
- The stale record is the profile aggregate (sealed Aug 23 at 41 files /
  1,805,907 bytes / `34d89823…`); the current inventory differs by one
  file of 182 bytes. **This difference cannot be reconstructed from the
  retained evidence** (the August record is aggregate-only; per-file dumps
  from that era were not preserved; no approved-bytes snapshot exists —
  `fixtureRelativePath` absent for these mods). No claim is made about
  that file's role or mutability.
- Disposition per the continuation's rules: the August difference stays
  unexplained (age alone does not invalidate the seal; missing detail is
  not permission to guess), and resealing is not authorized here →
  **live-ui-bootstrap remains blocked**. Prerequisite: an explicit owner
  decision — see the prepared approval packet
  `planning/BAGOFTRICKS-FIXTURE-APPROVAL-PACKET.md` (new versioned
  baseline bound to the transaction-verified current inventory, with
  original seal retained). No guard was bypassed, no manifest
  regenerated, no fixture file invented.

### The connected workspace (primary deliverable)

- `UI/CastingWorkspaceSession.cs` — the production session connecting the
  real services: browsing selection that never mutates; draft defaults
  that only configure the NEXT casting; explicit commands
  (Add/Update/Remove/Move/SetState/Undo) through the one mutation
  authority; `BuildView` read models (caster rows with capability vs
  readiness, casting cards with target/origin/coverage/gaps/
  enhancements/costs/readiness, budget drill-down with responsible
  casting IDs) derived from the shared resolved plan — no second ledger;
  save/reopen through the candidate repository; corrupt/unsupported
  candidates block persistence instead of default-overwriting.
- Review/Apply integration (the actual caller path):
  `PresentForReview`/`AcceptPresentedPlan`/`Apply` route every submission
  through `CastingExecutionGate` + `CastingReviewCoordinator`
  (unpresented, unaccepted, and materially-changed plans refuse; a
  harmless refresh keeps acceptance; an in-flight guard prevents double
  submission) and terminate at `DisabledCastingDispatchBoundary`, which
  refuses native submission with
  `native-submission-disabled:no-qualified-casting-first-executor` while
  recording the exact submission identity — policy proof, never gameplay.
- `UI/CastingWorkspaceScreenView.cs` — the Unity parchment surface
  (header with routine/readiness summary, caster lane, casting-card lane
  with per-card Edit, inspector with editing-scope label and
  Enable/Disable/Remove, footer with budget conflicts, Accept, Review &
  Apply, Ready Casts Only) using the existing factory/theme seams and the
  verified `PlayClick` route. Source-integrated and compiled; **not
  visually or audibly qualified** (no runtime capture was possible this
  checkpoint). Wired behind `CastingWorkspaceDevSelection` (session-
  scoped, default off) at `BuffPlannerUiRoot.OpenSetup()`, consuming the
  same discovery data as the legacy screen through newly exposed
  read-only model properties; legacy and new authoring paths are never
  active together.

### Verified behavior (integration layer, deterministic inputs)

Tests through the real session (game-boundary inputs are deterministic
fixtures; no planner service is mocked):
`casting-workspace-mixed-caster-flow` (three independent cards from two
casters with distinct settings; focus changes without reassignment;
single-card edit + Undo; mismatched replacement refused; one group card
with caster origin, 5 predicted beneficiaries, 1 honest gap, and no extra
casting), `casting-workspace-review-and-apply-policy` (unpresented/
unaccepted refusals; incidental previews approve nothing; safe quick-run
without ceremony; unseen material change refuses; Ready Casts Only after
re-presentation with disclosed omissions; dispatch disabled with recorded
identity), `casting-workspace-save-reopen-and-protection` (round trip
retains ids/order/disabled/unresolved; corrupt candidate blocks
overwriting).

### Acceptance-matrix adjustments

A01–A05, A07–A11, A12 import half, A13 isolated half: PASS at the
deterministic layer (unchanged). The workspace session integration adds
production-caller-path evidence for A09 (presented-plan) and the editing
contracts. Still open: A06 (exact rods), A14 equivalence beyond
structural, A15 execution, A16–A20 native/UI/runtime, live A12/A13
rehearsal, and ALL visual/audio acceptance of the new view.

### Next executable step

1. Comparator follow-up: archive per-file manifests at seal/mismatch time
   (additive), then resolve the BagOfTricks seal under owner authority and
   re-run `live-ui-bootstrap` for the donor inventory + workspace
   screenshots. 2. Runtime-qualify the workspace view (open/close, input
   isolation, listener lifetime, Escape) through the guarded lane once (1)
   unblocks. 3. Exact-rod durable-identity inspection (A06).

## Phase 2 checkpoint 7 — migration boundary + guarded live-lane findings — 2026-09-19 (commit `81c4648`)

**Branch** `codex/kingmaker-buff-planner-casting-first`, on top of
checkpoint 6 (`29f1ac8`). Gate: source 42/42; **protocol 205/205**;
harness 27/27; package 4/4; WhatIf 5/5; rollback 4/4; publisher 3/3
(`artifacts/casting-first-checkpoint7-gate.log`).

### A13 isolated migration boundary (implemented, deterministic layer)

`Persistence/CastingPlanMigrationService.cs` + `Hashing.Sha256Text`:
migrates schema-5 -> candidate schema-6 entirely inside candidate
storage — hashes and archives the exact legacy bytes once per boundary
(`kbp-casting-<24-char content hash>.orig`, non-rotating, MAX_PATH
safe), imports (idempotent), saves through the guarded candidate
repository, reopens and revalidates before reporting Migrated. The
legacy schema-5 file is never modified in any outcome. A torn candidate
primary is reported `CandidateUnusable` with recovery instructions (the
repository's refuse-to-bury guard is preserved); a newer-schema
candidate is `NewerCandidateRefused`; absent/unreadable legacy reported
distinctly; nothing is fabricated over unresolved data. Test
`casting-a13-migration-boundary-is-recoverable` covers the full flow,
byte-exact archival, idempotent re-migration, torn-candidate recovery,
newer-candidate refusal, and absent-legacy. Live installation/save-load
rehearsal of rollback remains open.

### Guarded live-lane findings (runtime evidence, separate layer)

- Package built from CURRENT source (`29f1ac8`): local-runtime ZIP
  SHA-256 `2b3138e4a48760a1b7e27c545c6eaee566d9dc55aabbaa3aee1414fc355b550d`,
  DLL `f9dd84b3ef37df42707b824674277ef01c9a17aaea98ca60c5b7bdd13321acbc`;
  WhatIf preflight PASS (no mutation).
- **`ui-native-contract-probe` run `casting-first-nativeprobe-1`**: the
  guarded transaction deployed, launched, and RESTORED VERIFIED
  (`runtime-evidence/casting-first-nativeprobe-1/runtime-result.json`:
  commit `29f1ac8`, MVID `c6cf326b-...`, game 2.1.7, UMM 0.33.0). The
  scenario itself FAILED honestly: `Native UI contract is not ready` —
  the probe requires campaign UI (`Game.Instance.UI.Canvas` +
  StaticCanvas + EventSystem) which never appears at the main menu
  (nativeCampaignUiAvailable=false, no EventSystem, ~62 s = boot + the
  bounded 600-update wait). Conclusion: this lane needs a campaign
  context, i.e. the save-loading lanes.
- **`live-ui-bootstrap` (human-reproduction) REFUSED pre-deployment**:
  `Compatibility fixture identity mismatch: BagOfTricks`. Read-only
  diagnosis: the installed BagOfTricks DLL and Info.json hashes MATCH
  the profile record; the mismatch is the directory manifest — record
  expects 41 files / 1,805,907 bytes, installed holds 40 / 1,805,725
  (one small mutable file removed since sealing). **Blocked operation:**
  the guarded compatibility-fixture inventory refresh for
  human-reproduction (re-bind the manifest to the current exact
  directory identity, per the established read-only rebind workflow);
  after that, re-run `live-ui-bootstrap`. No bypass attempted; nothing
  was staged or mutated by the refusal.

### Next executable step

1. Refresh the human-reproduction fixture manifest through its guarded
   workflow, then re-run `live-ui-bootstrap` for the campaign-context
   donor inventory. 2. Build the connected parchment workspace on the
   canonical model behind the session-scoped flag (Phase 4 start),
   wiring `CastingReviewCoordinator` into its Apply. 3. Exact-rod
   durable-identity inspection against installed item/serialization
   contracts (A06).

## Phase 2 checkpoint 6 — continuation contract checks C1–C5 — 2026-09-19 (commit `29f1ac8`)

**Branch** `codex/kingmaker-buff-planner-casting-first`, reconciled at
`061968d4b7ec3b7c1247feab72c065b4a7fd4d41` (checkpoint 5, clean tree,
single worktree) before this checkpoint. Reviewable evidence:
`artifacts/review-bundles/casting-first-checkpoints-1-6/` (format-patch
bundle for checkpoints 1–5 + this checkpoint's commit, with manifest).

### C1 — unvalidated required modifiers (DEFECT REPRODUCED AND REPAIRED)

The checkpoint-5 behavior was a confirmed defect: with no host registry,
an enabled modifier emitted a `targeting-modifier-unvalidated` diagnostic
and the casting STAYED EXECUTABLE — silent execution of the unmodified
spell, even when the recipient was legal for the base spell. Repaired:
`targeting-modifier-unresolved:<id>` now blocks. The unknown-id (against a
registry) and known-but-unavailable cases were already correct. Test
`casting-c1-unvalidated-modifiers-never-execute-unmodified` exercises all
three conditions through the compiler-to-gate path, both recipient
legalities, Ordinary refusal, Ready-Casts-Only omission without
submission, and registry repair preserving CastingId with no duplicate.

### C2 — unresolved draft is not an implicit opt-out (GAP REPAIRED)

Added `CastingAuthoringState.Disabled` (explicit player parking, distinct
from unresolved `Draft`) and `ResolvedCastingReadiness.Disabled`/
`AlreadySatisfied`. The gate gained a routine scope: a saved Draft in
scope blocks Ordinary Apply (`unresolved-saved-request`) — requiring
explicit resolution, disabling, or the deliberate Ready Casts Only
action; a Disabled record is disclosed (`explicitly-disabled`) and never
blocks; out-of-scope records are ignored entirely. Test
`casting-c2-draft-is-not-an-implicit-opt-out` covers scoped/unscoped
runs, disabled disclosure, and unrelated-run isolation. A11 updated to
the corrected contract (draft now blocks Ordinary).

### C3 — presented-plan acceptance (MISSING LAYER IMPLEMENTED)

The pure gate proved nothing about presentation; new
`Planning/CastingReviewCoordinator.cs` implements the deterministic core:
`CastingPlanSignature.For(plan)` captures material contents (identity,
order, exact source, targets/origin, coverage, enabled modifiers,
enhancement selections, cost vectors, readiness);
Present/Accept/TrySubmit enforce that only presented-and-accepted
contents matching the submitted plan may execute. Material changes
refuse (`material-change-requires-review`); refusal authorizes nothing;
a same-contents refresh keeps acceptance (no ceremonial loop);
incidental previews never present. Test
`casting-c3-presented-plan-gates-submission` covers edit-between-
presentation-and-submission, refused-then-retry, refresh, and preview
isolation. The workspace view and executor still must call it (marked
unintegrated; Apply stays safely disabled until then).

### C4 — modifier costs in the complete cost vector (MISSING, IMPLEMENTED)

`ICastingTargetingModifier.UsageDemands` added; applied modifiers'
verified pool demands merge into the same per-pool grouping in
`CastingBudgetLedger.DemandsFor`, so a modifier and a class feature
spending one reservoir validate as combined demand atomically. Test
`casting-c4-modifier-costs-enter-the-atomic-vector`: each affordable
alone, combined blocked with `enhancement-pool-exhausted:reservoir:1<2`,
zero leakage, deficit line exposed. Unknown modifier costs can no longer
be free (unvalidated modifiers block, so their cost is unresolved, not
zero). Native setup/execution/restoration of cost-charging modifiers
remains execution-phase work (A05 native half open).

### C5 — forecast effect projection (MISSING, IMPLEMENTED)

One-pass forecasts now carry structural effect presence forward:
`Compile(..., projectEffects: true)` marks a SkipAlreadyActive casting
`AlreadySatisfied` (no invocation, no reservation) when a
proven-equal earlier executing casting covered all its beneficiaries.
Equivalence is deliberately minimal — identical ability identity and no
strength-affecting enhancements on either side; enhanced or Overwrite
requests still cast. Test
`casting-c5-projection-carries-effects-forward`: overlap satisfaction,
Always-Recast remains a casting, enhanced requests gain no invented
satisfaction, authored-order reversal flips the executing casting with
identical totals, and independent previews mutate neither the document
nor later results. Assumption label added
(`projected-effects-are-structural-presence-only`).

### Verification at this checkpoint

Full gate: **source 42/42; protocol 204/204** (5 new C-tests; A11/A05
updated to repaired contracts); harness 27/27; package 4/4; WhatIf 5/5;
rollback 4/4; publisher 3/3
(`artifacts/casting-first-checkpoint6-gate.log`). Deterministic
domain/policy layer only.

### Acceptance-matrix adjustments

A05: domain+policy layers pass; native cost/state cleanup open. A09:
forecast layer passes; presented-plan integration is implemented at the
coordinator layer but not wired to a view/executor (open). A11: policy
layer passes with the corrected draft contract. A06, A12 (live accept),
A13 (live rollback), A14 (beyond structural), A15–A20: open.

### Next executable step

The guarded native donor investigation (Build-Local package + the
`ui-native-contract-probe` / `live-ui-bootstrap` lanes with restore
receipts), then the connected parchment workspace on the canonical
model; isolated A13 persistence/recovery tests advance alongside.

## Phase 2 checkpoint 5 — targeting-modifier resolution — 2026-09-19 (commit `061968d`)

**Branch** `codex/kingmaker-buff-planner-casting-first`, on top of
checkpoint 4 (`f9cc4fe`). Version remains `0.1.1-rc3`.

### Implemented (production code)

- `Planning/CastingTargetingModifiers.cs` (A05 domain half): the
  new-model `ICastingTargetingModifier` seam — a modifier may only
  narrow or expand the proven option's targeting (reachability,
  anchors, coverage), never the invocation count or the caster.
  Implementations must be pure.
- `ExplicitCastingCompiler` applies a casting's enabled modifiers in
  authored order after option resolution: applied modifiers transform
  eligibility; an unavailable modifier blocks the casting with a
  repairable reason (`targeting-modifier-unavailable:<id>:<reason>` —
  fixing the selection restores the casting, nothing is deleted); an
  unknown id against a provided registry blocks
  (`targeting-modifier-unknown`); with no host registry at all,
  enabled selections stay `targeting-modifier-unvalidated` diagnostics
  instead of being guessed. Disabled selections change nothing.

### Verified behavior (deterministic domain layer)

New test `casting-a05-targeting-modifiers-change-eligibility` covers
legal-target eligibility, narrowed eligibility blocking an out-of-set
recipient, unavailable-modifier repairable intent, disabled-selection
neutrality, unknown-id blocking against a registry, and purity across
recompilations (no leaked one-shot state in the domain layer).

Full gate: **source 42/42; protocol 199/199; harness 27/27; package
4/4; WhatIf 5/5; rollback 4/4; publisher 3/3**
(`artifacts/casting-first-checkpoint5-gate.log`). Domain layer only;
the one-shot native state restoration half of A05 belongs to the
execution phase.

### Acceptance matrix standing (A01–A20)

A01–A05 (domain layer), A07–A11 (policy layer), A12 import half: PASS
at the deterministic layer. A06 (exact rods — blocked on a durable
identity contract) and A13–A20: NOT RUN. Runtime, visual, and live
migration evidence: none claimed.

### Next executable step

The Phase 3 live lane is now the critical path and was verified
actionable read-only this session: no Kingmaker process running, the
automation save trio present (SEED 303 / BASELINE 304 / WORKING 305),
and a clean worktree. The next session should build the local-runtime
package (`scripts/Build-Local.ps1`), then run the guarded
`ui-native-contract-probe` / `live-ui-bootstrap` lanes for the donor
inventory capture per `docs/MANUAL-ACCEPTANCE.md`, restoring after each
run. Remaining Phase 2 items: exact-source identity plumbing (A06,
pending a durable contract decision) and effect-strength satisfaction
modeling (A14).

## Phase 2 checkpoint 4 — forecast views and apply gate — 2026-09-19 (commit `f9cc4fe`)

**Branch** `codex/kingmaker-buff-planner-casting-first`, on top of
checkpoint 3 (`3d1bc0b`). Version remains `0.1.1-rc3`.

### Implemented (production code)

- `Planning/CastingForecast.cs` (A09): `CastingForecastService` builds
  preview forecasts over the same resolved representation — a
  routine-scoped view budgets exactly one routine (fresh ledger per
  view; previewing several routines separately never multiplies
  charges), and the one-pass view budgets every routine in declared
  sequence against one shared ledger with balances carried forward.
  Both views expose their assumption labels (no-rest,
  no-elapsed-game-time, resources carried within the view,
  predictions-not-observations). `ExplicitCastingCompiler.Compile`
  gained an optional `budgetRoutineScope` for the selected-run view;
  out-of-scope castings keep compile-time readiness and reserve
  nothing.
- `Planning/CastingExecutionGate.cs` (§5.2, A10/A11): a stateless,
  deterministic apply policy over the compiled plan. Ordinary Apply
  counts every saved casting and refuses when any request is blocked
  (`blocked-casting:<id>:<reason>`); drafts are disclosed as omissions
  (`draft-not-enabled`). The explicit `ReadyCastsOnly` mode executes
  ready work while listing every omitted casting with its reasons and
  preserving the saved plan. Evaluation is pure — a preview or refused
  attempt never authorizes the next by having been computed.

### Verified behavior (deterministic domain layer)

New tests: `casting-a09-forecast-views-share-budgets-correctly`,
`casting-a10-required-enhancement-policy` (required-unavailable blocks
without downgrade; optional intent omits with disclosure; import keeps
legacy optional as optional),
`casting-a11-apply-gate-cannot-hide-omitted-work`.

Full gate: **source 42/42; protocol 198/198; harness 27/27; package
4/4; WhatIf 5/5; rollback 4/4; publisher 3/3**
(`artifacts/casting-first-checkpoint4-gate.log`). Domain layer only.

### Acceptance matrix standing (A01–A20)

A01–A04, A07–A11 (A10/A11 at the policy layer), A12 import half: PASS
(domain layer). A05 (Share modifier eligibility), A06 (exact rods —
blocked on durable identity contract), A13–A20: NOT RUN. Runtime,
visual, and live migration evidence: none claimed.

### Next executable step

Phase 3 preparation: native donor inventory via the now-available
automation fixture (guarded harness lanes), or — while live access is
unavailable — A05 targeting-modifier resolution modeling on the same
compiler seams.

## Phase 2 checkpoint 3 — shared atomic budget reservation — 2026-09-19 (commit `3d1bc0b`)

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
