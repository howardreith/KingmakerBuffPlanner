# Casting-Graph UI Correction — Status

Mission: `planning/CASTING-GRAPH-UI-CORRECTION-MISSION.md` (owner Howie,
2026-09-25). Adopted specification:
`planning/CASTING-GRAPH-UI-ADDENDUM-v1.1.md` (refines the casting-first
migration charter v1.0; it does not replace the canonical model, compiler,
budget ledger, execution host, persistence safeguards or guarded runtime
procedures).

Handoff package: `Kingmaker-Buff-Planner-Casting-Graph-UI-Handoff.zip`,
SHA-256 `f4d6b174f6e17c7234f11016fc998439098a4d6be1ac09761c1c88c67899da09`;
addendum `899378da…9b6fe6`, mission `e271c96c…eca086` (copied verbatim).

Branch: `claude/casting-graph-ui-correction`, created from the remote head
of `codex/kingmaker-buff-planner-casting-first` (`e8496ee`, PR #2 head).
No release, tag, merge or permanent installation is authorized.

## Phase 0 — identity of the screen the owner rejected (2026-09-25)

**Conclusion: the rejected screen was the Classic planner view, not the
casting-first workspace.** Its labels are rendered only by Classic view
classes in every candidate binary, and the casting-first workspace renders
none of them. The owner's rejection of the observed experience stands as a
product decision; it is not, by itself, a casting-first regression.

### Direct evidence

| Item | Value | How it was obtained |
| --- | --- | --- |
| Observed labels | "Casters: Automatic", "Edit Assignments", "Assignments & Resources", "Enhancement: None", Long/Important/Short tabs, four-column grid, target portrait toggles | Owner's report (mission §1) |
| Classes rendering them, rc6 `24d9967` | `UI/PlannerViews.cs`, `UI/BuffPlannerScreenView.cs`, `UI/PlannerPresentationModels.cs`, `UI/PlannerSetupModel.cs` (Classic) | `git grep -F` on the frozen tree |
| Classes rendering them, 0.1.1-rc3 `2aad5d4` | the same Classic files | `git grep -F` on the frozen tree |
| Casting-first view `CastingWorkspaceScreenView` | renders none of the labels (its header is "Casting Workspace — <buff>", routine tabs "Long (n)") | source, and rc6 frames `runtime-evidence/casting-ws-reload-20260924-rc6-01/ws-interact-*.png` |
| Installed on this machine (DATA) | KingmakerBuffPlanner **0.1.1-rc3**, commit `2aad5d434db5065a0fcb772960f74e82e7701469` (tag `v0.1.1-rc3`) | `Mods/KingmakerBuffPlanner/Info.json`, UMM log `[KBP-BOOT] Main.Load exited;version=0.1.1-rc3;commit=2aad5d4…` |
| Installed DLL SHA-256 | `78407dd4c7240ce25c7654715854e83061da744a18d8eb4a2198fdffec5e3080` (matches the rc1–rc6 rollback receipts) | `Get-FileHash` |
| Installed DLL MVID | `c0cc9cbc-8075-4d98-a32e-29972aafa7fa`; informational version `0.1.1-rc3` | `System.Reflection.Metadata` |
| Installed files unchanged since | 2026-09-24 23:57:20 (every file's creation time = the rc6 temporary-deploy rollback) | file times |
| `planner-mode.json` on DATA | absent (0.1.1-rc3 has no planner-mode concept; rc6 treats absent as Classic) | `Mods/KingmakerBuffPlanner/UserSettings` does not exist |
| Casting-first code in 0.1.1-rc3 | none (`PlannerModeStore`, `CastingWorkspace*`, `CastingAuthoringService`, `ExplicitCastingCompiler` absent) | `git ls-tree 2aad5d4` |
| Manual game launches on DATA, 2026-09-25 | **zero**: all 107 Kingmaker launches carry a harness argument (106 `-kmgRuntimeTestRequest` Gunslinger runs, 1 `-kbpRuntimeTestRequest` at 09:17); the last argument-free launch was 2026-09-24 07:37:32 (the advanced-seed save session) | `Steam/logs/gameprocess_log.txt` |
| Published alpha `v0.2.0-rc6` | zip downloaded 2 times, its `SHA256SUMS.txt` once (the publisher's own verification fetched both) | `gh release view v0.2.0-rc6` |
| rc6 default planner mode | Classic ("Classic stays the default"; casting-first is opt-in in the UMM settings panel) | `PlannerModeStore.Load`, rc6 release notes |

### Interpretation (what is and is not proved)

- **Proved:** the active view class was the Classic planner
  (`BuffPlannerScreenView` with the `PlannerViews` selected-buff panel), in
  whichever binary was loaded. The casting-first workspace was not on
  screen.
- **Proved:** the owner's test did not run on DATA's installation today
  (no argument-free launch), so DATA's 0.1.1-rc3 is not the binary the
  owner viewed today.
- **Most likely, not proved from DATA:** the owner ran the published
  `v0.2.0-rc6` alpha (package `f271f3e6…`, DLL `35d6cbb2…`, MVID
  `4cb12c0d-…`) in its default Classic mode on the owner's own machine (one
  download beyond the publisher's verification). The owner's own UMM log
  line `[KBP-BOOT] Main.Load exited;version=…;commit=…` would pin the exact
  binary; nothing on DATA can.
- The prior rc6 casting-first composition (captured in game at 1920x1200
  in `casting-ws-reload-20260924-rc6-01`) does **not** satisfy the addendum
  either: a four-column catalogue fills the upper half of the page; the
  casting lane is a list of cards with a text arrow, not caster → casting →
  target lanes with connections; targets are chosen inside the inspector and
  committed with an Add Casting button; casters show no per-source
  capacity; the gray stone buttons carry light cream text. The correction
  below is therefore required regardless of which Classic binary the owner
  saw.

### What changes because of this

- Mode identity becomes unmistakable: every planner screen states
  `Planner: Classic` or `Planner: Casting-first`, derived from the view that
  actually rendered (the effective route), and Classic offers a deliberate
  route into the casting-first workspace.
- The casting-first primary composition is rebuilt as the addendum's graph
  workspace on the existing canonical services (below).

## Correction plan and ledger

Backend preserved: `CastingPlanDocument` (schema 6), `CastingAuthoringService`,
`CastingWorkspaceSession`, `ExplicitCastingCompiler`, `CastingBudgetLedger`,
`CastingForecastService`, provider/enhancement discovery,
`CastingExecutionHost`, review/preflight, guarded persistence. The view stays
a read-model/command client; no second writable model, no view-local
counters, no graph coordinates persisted.

"Source" below means the production session/compiler/ledger (or the
predicate the live run applies) under the source-only suite; "live" means a
guarded in-game run on the disposable fixture. No live claim is made yet.

| ID | Scenario | Status |
| --- | --- | --- |
| G01 | Screenshot identity | Done (Phase 0 above) |
| G02 | Buff → caster/source → target creates one casting and one connection | Source pass (`graph-buff-caster-target-creates-one-casting`, `graph-selection-and-focus-never-mutate`); live scripted in `live-workspace-qual`, not run |
| G03 | Two casters, three targets, edit one only | Source pass (`graph-two-casters-three-targets-edit-one`); live scripted, not run |
| G04 | Multiple sources, non-double-counted capacity | Source pass (`graph-multiple-sources-exact-counts`, `graph-capacity-comes-from-the-plan-ledger`, `graph-capacity-text-says-what-is-left`) |
| G05 | Spontaneous pool propagation across buffs; delete restores | Source pass (`graph-spontaneous-pool-propagates-across-buffs`) |
| G06 | Shared enhancement pool, combined demand, atomic refusal | Source pass (`graph-shared-enhancement-pool-blocks-atomically`) |
| G07 | Line/chip opens inspector; Extend / Powerful Change per casting | Source pass (`graph-enhancement-edits-one-casting-and-budgets`); line corridor + chip focus scripted live, not run |
| G08 | Parallel same caster–target castings stay distinct | Source pass (`graph-parallel-castings-stay-distinct`) |
| G09 | Group casting: one origin/card, derived branches, missed coverage | Source pass (`graph-group-casting-one-origin-derived-branches`) |
| G10 | Hover sweep at 1920x1200 and 1920x1080 | Fix and predicate in source (`planner-controls-own-one-pointer-highlight`, `hover-record-*`); live rc6 reproduction + sweep implemented in `live-workspace-qual`, **not run** (root cause not yet proved live) |
| G11 | Button states and contrast | Palette source pass (`contrast-ratio-follows-wcag`, `button-palette-readable-in-every-state`); five-state in-game capture implemented, not run |
| G12 | Save/reload reconstruction | Source pass (`graph-save-reload-reconstructs-connections`, `graph-unresolved-casting-keeps-its-connection`); live `live-workspace-reload`, not run |
| G13 | Long names, crowded plan | Partial: chip geometry source pass (`graph-layout-chips-never-overlap`); crowded live frame not captured |
| G14 | Input/lifecycle, no stale listeners | Partial: one planner root + single owner after close/reopen judged live (not run); reload ownership as in rc6 scenario |
| G15 | No per-frame discovery or rebuild; stable object counts | Partial: the view rebuilds only on construction and commands (no Update/Tick); live object counts recorded (not run) |

## Checkpoints

### Checkpoint 0 — 2026-09-25, reconciliation

- Branch `claude/casting-graph-ui-correction` at `e8496ee` (no upstream set,
  so nothing can be pushed to PR #2 by accident).
- `main` `fd0e6dc`; PR #2 head `e8496ee` (open, draft); PR #1 closed
  (superseded, as already recorded).
- All 21 worktrees clean. The only local-only branch
  (`codex/kingmaker-buff-planner-next`, `caea6ea`) is patch-identical to
  `b4eed30` on PR #2 (`git cherry`), so nothing is lost.
- KBP runtime state after the reboot: 238/238 transactions `Restored`,
  every temporary deploy `RolledBack`, no lock, sentinel or
  protected-save violation. No Kingmaker process. The previous session's
  frame watcher is not running (its last act was the failed
  `casting-ws-reload-20260924-rc6-01` at 09:19, already recorded).
- Baseline source tests at `e8496ee`: protocol tests PASS=338 FAIL=0.
- The rc6 acceptance items still open in the previous status (frame-judged
  runs, supervised session) are superseded by the owner's direction and are
  closed without being run.
- Next: capacity query in the planning layer, graph read model and
  commands in the session (with tests), then the Unity graph view.

### Checkpoint 1 — 2026-09-25, graph workspace in source; live runs waiting

- Branch `claude/casting-graph-ui-correction`; commits since `e8496ee`:
  `767ca5c` (Phase 0), `5840b74` (capacity, graph read model, commands),
  `0191a26` (graph view, palette, one pointer highlight, mode labels),
  `757fc6b` (guarded scenario drives the graph; hover diagnostic).
  Version string unchanged (`0.2.0-rc6`, the development convention between
  candidates; no new candidate version before owner acceptance).
- Source-only: `Source validation: PASS=42 FAIL=0`; `Protocol tests:
  PASS=360 FAIL=0` (baseline 338; 22 graph/hover tests added, three
  source-contract tests retargeted to the graph view without dropping an
  assertion). The harness/WhatIf part of `Test-SourceOnly.ps1` refused to
  run because Kingmaker was running (the Gunslinger lab's guarded batch,
  PID 17744) — a correct refusal, rerun pending.
- Local package from `757fc6b`: `KingmakerBuffPlanner-0.2.0-rc6-local-runtime.zip`
  `3cb61c50…0618080`, DLL `762acc71…b39d2e2c`, MVID
  `12536e30-50a9-4338-a7e8-23214d2f5230` (a development build of that
  commit, not the published rc6). A documentation commit moves HEAD, so the
  package is rebuilt from the exact HEAD before any guarded run.
- Real defect found by the new tests and fixed: a material component with
  zero count did not block a casting (the ledger kept the first observed
  count); regression test in the graph suite.
- Hover root cause, current theory (from the installed UnityEngine.UI IL,
  not yet proved live): a pointer press selects any control whose
  navigation is not None, and `Selectable.IsHighlighted` ORs in
  `hasSelection`, so the clicked control stays lit while another is
  hovered. The fix gives planner controls navigation None. The live
  scenario reproduces the ghost with only `PlannerUiReproduction` switched
  on the Classic screen before measuring the fix; until that run passes the
  cause is a theory. Rejected as primary cause so far: stale rebuilt
  controls (rebuilds deactivate before destroy and the probe reads active
  controls only); a canvas-camera mismatch would show as
  `highlight-not-under-cursor` / `aimed-control-not-topmost` in the sweep.
- Live runs not started: the Gunslinger lab holds
  `compatibility-state/compatibility.lock` for its batch and has priority.
- Next: when the lock is released and no Kingmaker runs, rebuild the
  package from HEAD, rerun the full source-only gate, then guarded
  `live-workspace-qual` (default 1920x1200 and `-DisplayMode
  windowed-1920x1080`) and `live-workspace-reload`; judge frames and
  `hover-ownership.json`.
