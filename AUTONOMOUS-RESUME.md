# AUTONOMOUS RESUME — updated 2026-10-02 evening (Claude takeover, r14 batch)

## Where the work stands

- Tested source candidate: branch `codex/kingmaker-buff-planner-everyday-use`,
  commit `bff840a220e561016a8ee9ffbfdfc34b47554e42` (9 commits on top of the
  pushed `49c8917`). Full mandatory gate at that exact tree:
  `artifacts/gate-bff840a.log`. (The gate at 9b788e8,
  `artifacts/gate-9b788e8.log`, FAILED at the launcher WhatIf meta-check that
  still pinned six gestures; bff840a pins seven. Every stage was then run
  standalone at bff840a before the full gate reran.)
- Candidate package (Build-Local at bff840a, `artifacts/build-local-bff840a.log`):
  package `24f404ea25faf959569463dbbfbcacd65227e4a39bf38ff82bcf2d5502dacf41`,
  DLL `75b3ac48d4a95014cca2ae611cb1a894d5c7b1966512df176c771a3c445a99e7`,
  MVID `de7526c7-4f5c-4cd6-b1db-ff7567161628`; frozen with verified bytes
  under `runtime-backups/qualification-frozen/bff840a2.../FREEZE.json`.
- The main checkout (`C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner`)
  MUST stay at bff840a and clean while the r14 batch runs (the dispatcher
  refuses any other HEAD).
- Records are written on the local branch
  `codex/kingmaker-buff-planner-everyday-use-records` in the scratch worktree
  (`...\scratchpad\records-wt`) and are fast-forwarded onto the main branch
  only after the batch: `git merge --ff-only codex/kingmaker-buff-planner-everyday-use-records`
  in the main checkout, then push through the guarded helper
  `C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1`
  (fast-forward only; `scripts/Test-GuardedPush.ps1` is its WhatIf test).

## r13 (prior build 3c1c5d4, evidence labelled prior-build)

24 done + verified, 2 failed-reconciled, 15 deferred (dispatch/jobs.r13-final.json):
- `ui-phys-select` (beta-3c1c5d4ar13-phys-sel-01): in-game PASS, launcher judge
  threw on the absent `expectedScreen` (repaired a6c8b95).
- `ui-windowed` (beta-3c1c5d4ar13-w1080-01): every product assertion PASS,
  hover sweeps 8-10 missed - harness H1 (letterboxed 1920x1080 surface on
  the owner's new 1920x1200 RDP desktop; repaired 9b788e8). Also H2: every
  "windowed" run reports fullScreen=True (labelled fullscreen; not repaired).
- Share chains deferred: the r13 harness could not run them (repaired 45c46c6).
- Defects found by source/evidence reading and repaired before r14: D1-D11
  (see the journal checkpoint; D11 = the right-click description panel never
  rendered - E05/E06 were wrongly marked PASS before).

## The exact next action

1. Watch the r14 batch (`dispatch/jobs.json`, run IDs `beta-bff840a2r14-*`):
   `powershell -NoProfile -ExecutionPolicy Bypass -File
   C:\Dev\KingmakerBuffPlannerLab\dispatch\dispatcher.ps1` in the dispatch
   folder (waits for 3 min owner idle, honours `dispatch\owner-pause`).
2. On a failure: preserve evidence, confirm restoration (tx Restored, no
   locks, no Kingmaker, protected saves clean), diagnose from the record,
   smallest repair + regression, full gate, Build-Local, freeze, rebuild jobs
   with a fresh suffix, rerun affected chains only.
3. After the batch: fill docs/E01-E27-ACCEPTANCE-MATRIX.md (ORIGINAL row
   definitions from the v1.2 handoff 03_ACCEPTANCE_MATRIX.md), the guide,
   journal, QUALIFICATION/IMPLEMENTATION-REPORT/MANUAL-ACCEPTANCE, then
   fast-forward and push through the guarded helper.
4. Private delivery: draft release 399014941 (tag
   v0.2.0-rc6-casting-graph-preview, stays a draft). Re-verify the prior
   asset `KingmakerBuffPlanner-0.2.0-rc6-casting-graph+ec34705c.zip`
   (214b795b...8b20564) unchanged; upload the frozen bytes as
   `KingmakerBuffPlanner-0.2.0-rc6-everyday-use-v1.2+bff840a2.zip`; download
   back and verify size, package SHA-256, DLL SHA-256 inside, MVID,
   manifest commit; update the draft notes (template in the scratchpad:
   release-notes-v12.template.md).
