# Casting-First Migration — GitHub Review Index

Branch: `codex/kingmaker-buff-planner-casting-first`
Reviewed baseline: `c182061354e9e761c09648ca779ab334588ba379`
(`codex/kingmaker-buff-planner-z-native-assignments`). The PR against
`main` additionally shows the earlier migration lineage
(`fd0e6dc..c182061`); this index covers the casting-first commits
`c182061..HEAD` (61 commits at first publication).

Status: **work in progress — development branch, not a gameplay-qualified
release.** Native casting submission is explicitly disabled at the
workspace dispatch boundary. The visual qualification campaign is live
and its current blocker is documented below.

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
  (custom runner; 217 protocol tests incl.
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
| Source/protocol tests | 217/217 PASS locally (`Test-SourceOnly.ps1`: source 42/42, harness 27/27, deployment WhatIf 5/5, launcher -File WhatIf 3/3, fixture 3/3, rollback 4/4, publisher 3/3) |
| Live campaign qualification | Campaign load, UMM close, workspace open through the production hotkey path: PROVEN (run `casting-ws-root-200200`, game log) |
| Visual capture | BLOCKED by fully-black presentation while the owner's RDP session is disconnected (classified via open/closed bisection + `qwinsta`/`quser`; see `AUTONOMOUS-BLOCKERS.md`) |
| Interaction/execution | NOT RUN (follows visual capture); native submission stays disabled |

## Published evidence images (game-window-only, authorized runs)

| File | Original run artifact (identical SHA-256) | What it shows |
| --- | --- | --- |
| `docs/evidence/casting-ws-visual-183000-legacy-misroute-frame.png` | `runtime-evidence/casting-ws-visual-183000/workspace-frame.png` (`3c54fe28…`) | The run that PASSED identity checks while showing the LEGACY catalog screen — the misroute that the routing fixes address. Non-black dual-path capture (blackFraction 0.0285). |
| `docs/evidence/casting-ws-root-200200-hud-only-open-workspace.png` | `runtime-evidence/casting-ws-root-200200/workspace-frame.png` (`f6aa382a…`) | The run where the REAL workspace opened (log-proven, `workspaceRoot=active`, legacy closed) yet the frame shows the plain game HUD — the open draw question, plus the basis of the later bisection. |

No image edits: the published files are byte-identical copies of the
run-time captures. Black-frame and missing-content checks were never
relaxed to obtain a PASS; the two runs above are recorded failures of
the visual gate on their own terms.

## Durable records for the full chain

`planning/CASTING-FIRST-MIGRATION-STATUS.md` (checkpoint 12),
`AUTONOMOUS-RESUME.md`, `AUTONOMOUS-BLOCKERS.md`,
`KINGMAKER-BUFF-PLANNER-JOURNAL.md`.
