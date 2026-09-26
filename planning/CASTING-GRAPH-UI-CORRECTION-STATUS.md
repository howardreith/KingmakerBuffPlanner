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
of `codex/kingmaker-buff-planner-casting-first` (`e8496ee`, PR #2 head);
published by the owner's decision (2026-09-26) as
`codex/kingmaker-buff-planner-casting-graph` through the guarded helper,
draft stacked PR #3. Continue on the `codex/` branch.
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
| G10 | Hover sweep at 1920x1200 and 1920x1080 | 1920x1200 sweep live in `casting-graph-qual-1200-01`: every judged reading single-owner and aligned; rc6 owner-sequence reproduction not yet run (harness lookup defect, repaired in `590ca28`); 1920x1080 not run; root cause **not yet proved** |
| G11 | Button states and contrast | Palette source pass; five states drawn in game (1200-01, tints recorded); presented-frame capture for the pixel contrast measurement pending (1200-01 used the camera lane) |
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

### Checkpoint 2 — 2026-09-25 20:30, review repaired; first live run

- Independent read-only review of `e8496ee..757fc6b`: no correctness defect
  in the session, capacity, compiler or undo logic; one blocking harness
  defect and seven lesser ones, all repaired in `8e6a4e0` with tests:
  - blocking: the installed Unity UI never stores `Disabled` in
    `m_CurrentSelectionState` (Disabled is applied at transition time from
    `IsInteractable`), so the disabled button state could never pass. Button
    states are now judged from interactability, the stored state and the
    drawn tint; a hovered disabled control no longer counts as an owner;
  - the rc6 reproduction was true by construction (the harness clicked a
    routine tab itself). It now follows the owner's report (mission §1: "the
    correct hover plus a second visible hover effect offset up and left of
    the cursor"): hover a Classic portrait with the real cursor before any
    click, click a buff card, hover again; rc6 first, then shipped. Ghost
    geometry relative to the cursor is recorded; a ghost without a click is
    reported as a cause the selection theory does not explain;
  - the rc6 switch now resets in the host's own shutdown (deadline, abort,
    exception, disable, unload); sweeps aim only inside scroll viewports
    and cover every visible target; transitions settle before readings;
  - product: a party-shared pool (item charges) is disclosed under every
    caster; an at-will source never shows "0 of 0 left"; `CasterById(null)`
    no longer returns the "Needs a caster" node; the footer puts the one-pass
    shortfall first and shrinks long budget lists.
- `b1cb4e0`: live-workspace-qual gets 300 s more live budget for the
  diagnostic (the launcher timeout still bounds the run).
- **Live run `casting-graph-qual-1200-01`** (`b1cb4e0`, package `b5f41e29…`,
  DLL `4ae3ab0a…`, MVID `cec009cf-…`; profile full-user; Automation
  fixture; 1920x1200; owner session Active over RDP; 19:27–19:30):
  - FAIL at `workspace-hover-validation` only. Every other assertion PASS:
    frames non-black (open luma mean 0.548, changed fraction 0.906 against
    the control frame); interaction sequence (buff chosen through its tile,
    3 castings from 2 casters through caster → source row → target, refused
    click clean, line corridor and card focus, inspector retarget, Undo,
    Done, Save); reopen preserved (3 castings, clean); optional mods 16/16
    identical.
  - Hover record: 35 readings, all 30 judged (shipped-behaviour) readings
    single-owner; the other 5 are rc6 evidence. The real cursor
    landed within 2 px of every aim; graph sweep of 10 controls (casters,
    source row, card, 3 targets, catalogue tile, Save, routine tab), Classic
    sweep of all 3 portraits, reopen hover and exit; no planner control ever
    held the selection; one planner root after reopen, 45 → 45 controls.
    Five button states drawn with their palette tints (normal `#8F8578`,
    hover `#B8A894`, pressed `#70665C`, selected `#CC4C38`, disabled
    `#808080`). rc6: a portrait hovered before any click showed exactly one
    highlight (no ghost without a click).
  - Why it failed: the owner-sequence click never happened
    (`classic-card-missing`): a bound Classic card is named
    `Source.<sourceId>`, not its pool name `BuffCard`. Harness defect.
  - Also found in that run's frames: the camera-path lane renders planner
    text washed out and cannot see the Classic screen at all (a screen-space
    overlay); the presented frame `workspace-frame.png` is legible (dark ink,
    burgundy headings, ivory captions on dark stone). And a real product
    defect: the footer's budget lines sat on the book's dark lower edge.
  - Safety: Kingmaker exited; Mods restoration verified; protected saves
    compared clean; transaction Restored.
- Repaired in `590ca28`: the card lookup, hover/button frames from the
  presented frame, and a parchment ground behind the footer text. Gate at
  `590ca28`: source 42, protocol 362, harness 38, package 4, deploy WhatIf
  5, launcher WhatIf 12, fixture 3, Restore-InstallLocal 16, publisher 3.
- Blocked since 19:41: the owner's session connects only briefly (10–90 s
  at 19:25–19:41, 20:11, 20:17, 20:25); a frame run needs two minutes of
  stable connection before it starts and a connection through its ~3–5
  minutes.
- Next: on a stable connection, rebuild at HEAD, then `live-workspace-qual`
  at 1920x1200 (`-02`), at `windowed-1920x1080`, and `live-workspace-reload`;
  then the supervised manual session and the owner's verdict.

### Checkpoint 3 — 2026-09-26, handoff to Z.AI

- The Gunslinger lab ended its work at 01:44 (owner's KMG 0.0.136 restored
  and byte-verified); KBP reconciled clean (239/239 transactions Restored),
  rebuilt at `3ebdfe1` (package `e895d39c…`, DLL `9e85417a…`, MVID
  `f5f7b1ef-3680-428c-a55c-5b619e45b23c`) and passed the full gate (source
  42, protocol 362, harness 38, package 4, deploy WhatIf 5, launcher WhatIf
  12, fixture 3, Restore-InstallLocal 16, publisher 3).
- Live `casting-graph-qual-1200-02` (`3ebdfe1`, owner session Active,
  05:45–05:50): FAIL at `workspace-hover-validation` — 18 physical moves
  refused with `Kingmaker foreground activation failed` (the game window was
  not in front; the planner hotkey was also not delivered and the host used
  its programmatic open). Evidence still showed the mechanism: rc6 Classic
  card click `tookSelection=True`; shipped `tookSelection=False`; the shipped
  real-cursor click-then-hover had exactly one highlight; the five button
  states drawn. Safety clean; 240/240 transactions Restored.
- Owner decisions: publish under the `codex/` name through the unchanged
  helper (pushed `3ebdfe1`, draft PR #3); the in-game review "later today";
  Claude's quota ended, so Z.AI continues from
  `planning/CASTING-GRAPH-HANDOFF.md`.
- Next: the handoff's §3, from reconciliation and the gate at HEAD onward.
