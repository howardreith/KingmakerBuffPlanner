# Casting-First Migration — GitHub Review Index

Branch: `codex/kingmaker-buff-planner-casting-first`
Reviewed baseline: `c182061354e9e761c09648ca779ab334588ba379`
(`codex/kingmaker-buff-planner-z-native-assignments`). The PR against
`main` additionally shows the earlier migration lineage
(`fd0e6dc..c182061`); this index covers the casting-first commits
`c182061..HEAD` (61 commits at first publication).

Status: **work in progress — development branch, not a gameplay-qualified
release.** Native casting submission is explicitly disabled at the
workspace dispatch boundary. The workspace renders in presenting
sessions (display-path acceptance run `casting-ws-gseries-081000`);
human usability and the native aesthetic pass remain open.

## Current review dispositions — M-series at `1e3c95b` (answered 2026-09-23)

Normal dispatch stays disabled; no cast was run. Evidence is source and
recording-runtime tests plus selection-only live runs (no boundary
constructed).

| Finding | Disposition | Commit | Tests (mutants caught) |
| --- | --- | --- | --- |
| M1 — zero-cost native sources could not be projected | A known Unlimited pool is one zero-unit native demand reserved by the existing ledger and flagged verified; flag carried by cost line, budget line (shown "unlimited"), converter, projection identity v3 and both executors; unknown/stripped/missing costs refused; spend on a free source fails; feature costs still count | `a023293` | `zero-cost-native-source-integration` (5/5); four legacy fixtures moved to finite pools because their runtimes report a spend |
| M2 — probe after-state came from cached UI discovery | Fresh native reads (ability availability, target buff instances + end time) stamped from one sequence; failed reads recorded, never replaced; before < submission < after; new instance or verified refresh; free = zero change, finite = exactly one; missing ≠ zero; target without the effect preferred | `0a4c382` | `probe-observations-are-authoritative` (6/6) |
| M3 — host did not own probe cancellation | `SingleCastProbeRunOwner`: one idempotent terminal (dispose boundary, fresh after-read, close workspace, release lease, record cleanup, publish once) for completion, stop, deadline, host exception, disable and unload | `0a4c382` | `probe-owner-terminal-cleanup` (3/3), `probe-owner-wiring-in-host-and-main` (1/1) |
| Selection finding | Discovery wraps effects in a self-reference; accepted only for the cast ability itself | `ea45807` | `probe-scope-enforces-whole-subset` (1/1) |

Concrete, unapproved cast proposal: `docs/LIVE-CAST-PROBE-REQUEST.md`.
Advanced-copy inspection request: `docs/ADVANCED-SAVE-COPY-INSPECTION-REQUEST.md`.

## Previous review dispositions — L-review at `475d2b9` (history)

Native dispatch remains disabled in the production workspace. Every row
below is **source/isolated-test evidence only** (protocol suite, isolated
PowerShell fixtures, mutation checks); no row claims gameplay. The
single-cast probe is prepared but has **not** cast anything.

| Finding | Disposition | Commit | Tests (mutants caught) |
| --- | --- | --- | --- |
| L1 — imported constraints bypassable (Ready Casts Only; generic Mark Ready) | Pending plan-wide notices refuse EVERY apply mode in the gate and the session; a refused gate decision never reaches the boundary; unresolved per-casting review items block compiler readiness whatever the state; Mark Ready/Add/Update refuse Ready with unresolved review; content edits cannot change provenance; explicit, undoable, disclosed ResolveImportReview / AcknowledgeImportNotices record resolution beside the kept history (persisted); inspector controls added | `58a1aaf` | `import-requirements-stay-enforced` (6/6) |
| L2 — cancellation did not dispose the active executor | The coordinator owns the nested iterator: disposed exactly once on exhaustion, failure and OUTER disposal; acquisition/MoveNext/Current/Dispose failures halt without replacing the first failure; cancelled runs report once; Processed vs NativeSubmissionReported. Animated executor now reports abandonment and cancel-cleanup failures (reporting only) | `2929985` | `explicit-run-cancellation-disposes-executor` (6/6) |
| L3 — ProjectionId omitted executable fields | Versioned (identityVersion 2) canonical JSON of every step field in order, incl. token ids, enhancement pool usage, source id, expected effect tree, omitted enhancements; set-like values normalized; unrepresentable effect refuses; canonical text exposed | `cd5bc4a` | `projection-identity-is-complete` (6/7; the survivor is equivalent — CastStep already sorts recipients) |
| L4 — partial swap could release an Installed record | Each destructive substep tracked; only a verified byte-identical return yields Installed/not-applied; a failed second move with failed reversal records RollbackRecoveryNeeded with paths/identities and keeps the lock; lock released only after a clean state is recorded | `aa3b95c` | `Test-RestoreInstallLocal.ps1` 16/16 (2/2 script mutants) |
| L5 — file-name scan is not format compatibility | Schema-6 format revisions (1/2/3); writer stamps formatRevision 3; repository refuses newer revisions; the assembly declares `KingmakerBuffPlanner.CandidateProfileFormat = 6.3`, read from the restored binary in a separate reflection-only process; candidates kept only if every member belongs to a revision the target reads; undeclared targets keep none; byte-exact archives | `aa3b95c` | `Test-RestoreInstallLocal.ps1` (4/4 script mutants), `rollback-candidate-format-members-match-model` |
| L6 — probe scope admitted excluded categories | Converter refuses non-spellbook/special source, metamagic, provider/caster mismatch, self-target, unverified target, non-DirectRuleCast strategy, non-plain effect shape; undefined scope enum refused | `cd5bc4a` | `probe-scope-enforces-whole-subset` (5/5) |
| Probe preparation | Dormant selector, strict owner allowance, default-refusing one-shot boundary, gated `live-cast-probe-select` / `live-cast-probe` scenarios; request `docs/LIVE-CAST-PROBE-REQUEST.md` | `6f22a78`, `2117438` | `single-cast-probe-is-dormant-and-one-shot`, `probe-scenario-request-validation`, `single-cast-probe-run-record-rules` (8/8), launcher gating 4 cases |

The first live cast is **not** ready: two selection-only live runs found no
eligible casting on the WORKING fixture (cantrips only). See
`docs/LIVE-CAST-PROBE-REQUEST.md`.

## Previous review dispositions — K-review at `258a1d0` (history)

Native dispatch remains disabled. Evidence for every row below is
**source tests only** (protocol suite / isolated script fixtures); no row
claims gameplay.

| Finding | Disposition | Commit | Tests |
| --- | --- | --- | --- |
| K1 — a failed legacy import activated an empty plan / default replacement | A failed import blocks the workspace with a visible reason: no candidate written, Save and Apply refused, old plan not replaced; legacy `ProfileRepository.Save` quarantines an unresolved primary instead of replacing it (explicit `ReplaceUnresolvedPrimary` recovery) | `e0509fe` | malformed primary, newer primary + valid backup, archive/candidate write failures, retry after repair, legacy save refusal/recovery |
| K2 — import ids could collide | Ids `m5:<routine>:<child>:<recipient key>`; reuse only on exact persisted provenance; any other collision refuses the import (K1 blocks) | `0c7b760` | duplicate child ids across routines, repeated import, unrelated collision, earlier-format reuse without renumbering |
| K3 — unresolved legacy constraints were dropped | Unknown grouping, provider pins, enhancement requiredness, automatic/missing casters and target-less children become Draft castings with durable review items; bans/caps/priorities become plan-wide import notices that block Apply until acknowledged (undoable) | `0c7b760` | unknown grouping, pin, optional enhancement, priority/cap, target-less child, save/reopen, Apply refusal + acknowledgement |
| K4 — converter could drop contracts it cannot carry | Standard scope refuses the whole projection for an enabled targeting modifier, an exact-source enhancement, or incomplete required group coverage; `SingleCastProbe` scope admits one plain direct casting only; deterministic `ProjectionId`; the dispatch boundary receives the exact projection | `b69f873` + probe-case tests | `converter-refuses-unsupported-contracts` (every probe refusal mutation-checked) |
| K5 — a failed/uncertain cast must stop later submissions | `ExplicitCastingRunCoordinator` halts after any casting not positively confirmed, marks the rest NotAttempted, never retries; optional submission limit | `20bbe82` | `explicit-run-stops-after-failure-both-modes` (instant + animated, five failure kinds each; limit 1) |
| K6 — install rollback could mis-record state after a post-swap failure | Prior build prepared and verified in staging (backup never modified); RollingBack + phase recorded before each transition; post-swap failure reverses the swap; unrecoverable reversal records RollbackRecoveryNeeded and keeps the lock; candidate compatibility read from the restored binary; candidates from both sides archived | `b243b02` | `Test-RestoreInstallLocal.ps1` 12 isolated cases incl. injected failures at candidate-archive, settings-merge, identity, verify, record and reversal |
| K7 — selected-buff coverage depended on the lane scope | Coverage computed from every casting of the selected buff, independent of the this-buff/whole-routine toggle | `93408bd` | scope toggle changes cards only, never coverage |

First live cast: **request prepared, not executed** —
`docs/LIVE-CAST-PROBE-REQUEST.md`.

## Previous review dispositions — J-review at `19ecbe8` (history)

| Finding | Disposition | Code | Tests |
| --- | --- | --- | --- |
| J1 — automatic evidence producer/consumer adjacency mismatch | Repaired: structured record; the host fills it through `WorkspaceCastStepEvaluator.Evaluate` and accepts it only through `WorkspaceInteractionRecord.Violations()`; `saved` is observed via `!IsDirty` | `src/KingmakerBuffPlanner/RuntimeTesting/WorkspaceScenarioContracts.cs`, `RuntimeTestHost.cs` (`UpdateWorkspaceInteraction`, workspace result block) | `workspace-interaction-evidence-contract`, `workspace-cast-step-evaluator-real-session`, `runtime-host-scenario-contract-wiring` |
| J2 — manual terminal accepted capture by filename, ignoring failure/restoration | Repaired: `ManualTerminalCoordinator` — bounded 20 s capture wait, failure + restoration verdict consumed, close postcondition (view + input lease) always recorded, four separate manual assertions; camera routine now always reports `RestorationClean` | `WorkspaceScenarioContracts.cs`, `RuntimeTestHost.cs` (phases 31/32, manual result block), `MenuRenderDiagnostic.cs` | `manual-terminal-policy-done-stop-deadline`, `manual-terminal-capture-restoration-cleanup`, `runtime-host-scenario-contract-wiring` |
| C1 — stale current-state summaries | Updated: tracker CURRENT STATE section, this index, `AUTONOMOUS-RESUME.md`, PR body; history kept | `planning/CASTING-FIRST-MIGRATION-STATUS.md` | — |
| C2 — rehearsal-6 evidence distinctions | Raw records re-verified; sanitized receipt published | `docs/evidence/casting-ws-manual-rehearsal-6-receipt.md` | — |
| C3 — manual hold numeric boundary | Protocol reader matches launcher 30–1200 s and range-checks before narrowing | `RuntimeTestProtocol.ReadManualHoldSeconds` | `runtime-manual-scenario-validation` (29, 2^32+300 long, -1 rejected) |

Every new regression was mutation-checked: re-introducing the defect
(already-ready rejected, capture failure ignored, unclean restoration
ignored, unbounded capture wait, lease ignored, sibling change ignored,
done preferred over stop) makes the suite fail.

## What to review, by area

### Casting contract core (domain/compiler/budgets)

- `src/KingmakerBuffPlanner/Domain/Authoring/CastingIntentModels.cs` —
  one saved casting record = one invocation; Draft/Ready/Disabled;
  explicit order.
- `src/KingmakerBuffPlanner/Planning/ExplicitCastingCompiler.cs` —
  deterministic allocation with spell-scoped atomic budgets.
- `src/KingmakerBuffPlanner/Planning/CastingReviewCoordinator.cs` —
  presented-plan review with material-content signatures.
- `src/KingmakerBuffPlanner/Planning/CastingExecutionGate.cs`,
  `CastingForecast.cs`, `CastingTargetingModifiers.cs`.
- Regression tests: `tests/KingmakerBuffPlanner.Tests/Program.cs`
  (custom runner; 231 protocol tests at the J-review repair, incl.
  `casting-workspace-mixed-caster-flow`,
  `casting-workspace-review-and-apply-policy`,
  `casting-workspace-save-reopen-and-protection`,
  `casting-workspace-sibling-intent-vs-derived-changes`,
  `casting-workspace-disabled-dispatch-attempt-only`).

### Workspace UI (session + view)

- `src/KingmakerBuffPlanner/UI/CastingWorkspaceSession.cs` — session
  controller: browse-never-mutates, explicit commands with Undo,
  review integration, `DisabledCastingDispatchBoundary` (records
  attempts, never claims gameplay).
- `src/KingmakerBuffPlanner/UI/CastingWorkspaceScreenView.cs` —
  parchment workspace view (caster lane, casting cards, inspector,
  Review & Apply footer) built against installed Unity 2018.4
  contracts.
- `src/KingmakerBuffPlanner/UI/CastingWorkspaceDevSelection.cs` —
  session-scoped selection; while selected, every legacy quick-exec
  route refuses (`BuffPlannerUiRoot.ExecuteLegacyRoutine`).

### Harness repairs this session (each with a regression test or a fail-closed gate)

- **Launcher `-File` crash** — `scripts/Invoke-KingmakerRuntimeTest.ps1`:
  a top-level `$PSCmdlet.ShouldProcess` throws NullReferenceException
  under `powershell.exe -File` on both the WhatIf and confirmation paths
  (reproduced minimally; works under `-Command`/direct invocation).
  Guard contract: a decision that cannot be evaluated is a REFUSED
  decision for real runs (error propagation, nothing staged); a `-WhatIf`
  request is honored because its outcome is deterministically negative
  and can never create permission. Regression:
  `scripts/Test-RuntimeLauncherFileWhatIf.ps1` (four layers: pattern
  refusal, guard-shape source check, `-File -WhatIf` purity, and a REAL
  `-File` run proving non-zero exit plus zero mutation of every
  protected root; wired into `Test-SourceOnly.ps1`).
- **Workspace routing** —
  `src/KingmakerBuffPlanner/RuntimeTesting/RuntimeTestHost.cs`: the
  workspace dev-selection enable sat dead twice (inside the non-live
  UI-smoke gate; then after `Update()`'s live-UI return); now at the
  top of `Update()`.
- **Honest visual gates** — the `live-workspace-qual` scenario now
  requires `workspaceRoot=active;legacyScreen=closed`
  (`IsCastingWorkspaceOpen`, not the legacy `IsScreenOpen`), dual-path
  frame capture (end-of-frame ReadPixels + luma stats + engine second
  path), `workspace-frame-nonblack` (blackFraction<0.98), and
  `workspace-hierarchy-presents` (active hierarchy, alpha, renderable
  texts). A persistent black frame FAILs the run at
  `workspace-visual-validation`.

## Evidence layer status (honest)

| Layer | Status |
| --- | --- |
| Source/protocol tests | 231/231 protocol PASS locally at the J-review repair (the full `Test-SourceOnly.ps1` gate counts for the pushed HEAD are recorded in `AUTONOMOUS-RESUME.md`). Historical: 217/217 at checkpoint 12. |
| Live campaign qualification | Campaign load, UMM close, workspace open through the production hotkey path: PROVEN (run `casting-ws-root-200200`, game log) |
| Visual capture | Backbuffer presentation dies with a disconnected session (classified by matched control/open/closed bisection); the camera-render capture lane bypasses the display path and produced rendered workspace frames including the live authoring run. Display-path acceptance still pending a connected session. |
| Interaction/execution | **Corrected control-path LIVE-PASSED** (run `casting-ws-gseries-081000`; supersedes the `casting-ws-controls-051500` claim, whose assertions could not detect refused Adds): every action through real ACTIVE buttons — source, caster, recipient, state, Add (×3 with one-record growth, distinct identities, exact authored fields, unchanged siblings), a deliberately refused Add leaving the document untouched, Edit, focused retarget, Undo restoring the exact pre-edit canonical document, Done clearing focus, Save, and production-route reopen with the full canonical signature preserved and a clean dirty state. Protocol layer additionally proves group transitions, focused enhancements, campaign binding, and a fresh-session persisted round trip (223/223). Native submission remains disabled; physical-input (real pointer/keyboard) acceptance and the native aesthetic pass remain OPEN. |

## Published evidence images (game-window-only, authorized runs)

| File | Original run artifact (identical SHA-256) | What it shows |
| --- | --- | --- |
| `docs/evidence/casting-ws-gseries-081000-full-acceptance.png` | `runtime-evidence/casting-ws-gseries-081000/workspace-frame.png` (`38fb627f…`) | **Corrected full-acceptance run, display path**: the open workspace (mean 0.73 vs control 0.14, changed 0.95) from the run whose sequence requires real active controls, record-level Add assertions, a refused-Add negative, and intent-exact Undo. The earlier `casting-ws-controls-051500` frames are superseded by this run (its assertions could recycle a last-record ID). |
| `docs/evidence/casting-ws-controls-051500-control-backbuffer.png` | `runtime-evidence/casting-ws-controls-051500/workspace-control-frame.png` (`0ea31e28…`) | Matched control frame (workspace closed) — retained for the superseded 051500 run. |
| `docs/evidence/casting-ws-author-033000-interact-authored.png` | `runtime-evidence/casting-ws-author-033000/ws-interact-authored.png` (`62c2a167…`) | The authoring workspace LIVE with three authored casting cards (cast-1/2/3, two casters), draft editor in the inspector — captured after the scripted interaction sequence (browse → 3 casts → edit), through the camera lane. The run's behavior assertions PASSED: `workspace-interaction-sequence` (browseNoMutation=True; capableCasters=3; cast1/2/3=applied; edit=applied; undo=True; saved=True) and `workspace-reopen-preserves-intent` (exact IDs/order, loadStatus=Loaded). |
| `docs/evidence/casting-ws-author-033000-interact-reopened.png` | `runtime-evidence/casting-ws-author-033000/ws-interact-reopened.png` (`45e0cdeb…`) | The workspace after close + production-route reopen with the saved candidate intact (cast-1,cast-2,cast-3 preserved in order). |
| `docs/evidence/casting-ws-layer-030000-workspace-camera-frame.png` | `runtime-evidence/casting-ws-layer-030000/workspace-camera-frame.png` (`0a9a2fc9…`) | **The casting workspace rendered** — parchment panel, header "Casting Workspace", caster lane with three casters + Focus buttons, casting cards, inspector, footer Undo/Accept Plan/Review & Apply. Captured through the camera-render lane (display-independent) in a session whose backbuffer was black. Known defects visible: caster names show unit GUIDs (display names not reaching the row model); lane/card label truncation. |
| `docs/evidence/casting-ws-layer-030000-camera-control.png` | `runtime-evidence/casting-ws-layer-030000/workspace-camera-control.png` (`ce4b468c…`) | Matched control frame, same session/build/resolution, workspace closed: game scene + HUD (mean luma 0.41 vs the open frame's 0.73). |
| `docs/evidence/casting-ws-visual-183000-legacy-misroute-frame.png` | `runtime-evidence/casting-ws-visual-183000/workspace-frame.png` (`3c54fe28…`) | The run that PASSED identity checks while showing the LEGACY catalog screen — the misroute that the routing fixes address. Non-black dual-path capture (blackFraction 0.0285). |
| `docs/evidence/casting-ws-root-200200-hud-only-open-workspace.png` | `runtime-evidence/casting-ws-root-200200/workspace-frame.png` (`f6aa382a…`) | The run where the REAL workspace opened (log-proven, `workspaceRoot=active`, legacy closed) yet the frame shows the plain game HUD — later explained by the layer-culling defect fixed at `72209fc`. |

No image edits: the published files are byte-identical copies of the
run-time captures. Black-frame and missing-content checks were never
relaxed to obtain a PASS; the two runs above are recorded failures of
the visual gate on their own terms.

## Durable records for the full chain

`planning/CASTING-FIRST-MIGRATION-STATUS.md` (CURRENT STATE section),
`AUTONOMOUS-RESUME.md`, `AUTONOMOUS-BLOCKERS.md`,
`KINGMAKER-BUFF-PLANNER-JOURNAL.md`.
