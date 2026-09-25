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
  `4cb12c0d-…`) in its default Classic mode on his own machine (one
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

| ID | Scenario | Status |
| --- | --- | --- |
| G01 | Screenshot identity | Done (Phase 0 above) |
| G02 | Buff → caster/source → target creates one casting and one connection | Planned |
| G03 | Two casters, three targets, edit one only | Planned |
| G04 | Multiple sources, non-double-counted capacity | Planned |
| G05 | Spontaneous pool propagation across buffs; delete restores | Planned |
| G06 | Shared enhancement pool, combined demand, atomic refusal | Planned |
| G07 | Line/chip opens inspector; Extend / Powerful Change per casting | Planned |
| G08 | Parallel same caster–target castings stay distinct | Planned |
| G09 | Group casting: one origin/card, derived branches, missed coverage | Planned |
| G10 | Hover sweep at 1920x1200 and 1920x1080 | Planned (needs live reproduction first) |
| G11 | Button states and contrast | Planned |
| G12 | Save/reload reconstruction | Planned |
| G13 | Long names, crowded plan | Planned |
| G14 | Input/lifecycle, no stale listeners | Planned |
| G15 | No per-frame discovery or rebuild; stable object counts | Planned |

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
