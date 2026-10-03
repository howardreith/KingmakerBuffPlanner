# AUTONOMOUS RESUME — updated 2026-10-03 early morning (Claude takeover, r15 next)

## Where the work stands

- Candidate under the gate: branch `codex/kingmaker-buff-planner-everyday-use`,
  commit `8d7681d03752f3f7170f25f7d45029f71c46a884` (pushed head is `bff840a`;
  648608f and 8d7681d are local until their gate passes). Full gate log:
  `artifacts/gate-8d7681d.log` (~4 h: four lab-wide purity windows).
- Build-Local at 8d7681d (`artifacts/build-local-8d7681d.log`): package
  `5b24e2eafee69b18898894b6e9a5cfb9b2c8e625a561494cb2234766097d8d17`, DLL
  `deee7b1de54daee9e890f6680c8e0d4db09674b12c394e2d2d6c2d3e460cf65b`, MVID
  `1e762004-8200-430e-853e-d61456a627ae`. Freeze it after the gate passes
  (scratchpad `freeze-candidate.ps1 -Commit <sha> -Purpose ...`).
- Records are on the local branch `codex/kingmaker-buff-planner-everyday-use-records`
  in the scratch worktree `...\scratchpad\records-wt`; fast-forward the main
  branch onto it only when no batch is running.

## Batches so far

- r13 (3c1c5d4): 24 done (prior-build), phys-sel and w1080 failed-reconciled
  (launcher judge expectedScreen; H1 letterbox mapping), 15 deferred.
- r14 (bff840a, gate PASS `artifacts/gate-bff840a.log`, pushed): done and
  verified - reload-01, phys-sel-01, imp-01, insp-shared-personal,
  sel-shared-personal, insp-finite. Failed-reconciled - allow-phys (H3: the
  cf-physical writer demanded the pre-v1.2 seed note), cast-shared-personal-
  instant (H4: the shared-personal purpose was 441 chars, the launcher bound
  is 400; refused in preflight, nothing staged). The phys-sel frames showed
  D12 (description titled "heighten-0") and D13 (the graph never overflowed,
  so physical graph scrolling was never demonstrated in any run). r14 was
  stopped between jobs (owner-pause hold, removed) and its remaining 32 jobs
  deferred so every native result is produced on the repaired candidate.
- Repairs since bff840a: 648608f (D12, D13, H3, exact Long grant cap),
  8d7681d (H4). Each has regressions; protocol 398, the WhatIf writer section
  now covers cf-physical and both shared recipes.

## The exact next action

1. When `artifacts/gate-8d7681d.log` ends with `gate exit=0`: push through the
   guarded helper, freeze 8d7681d, build r15 with
   `dispatch\build-jobs.ps1 -Commit <sha> -PackageHash <PKG upper> -Suffix r15`
   (scratchpad `build-r14.ps1` shows the checks; copy it with the new suffix),
   and start `dispatch\dispatcher.ps1` (operator stop = a `stop` file in the
   dispatch folder; `owner-pause` is the owner's hold).
2. On a failure: preserve evidence, confirm restoration (tx Restored, no locks,
   no Kingmaker, protected saves clean), diagnose, smallest repair +
   regression, full gate, freeze, rebuild with a fresh suffix.
3. After r15: matrix on the ORIGINAL E01-E27 rows (handoff
   03_ACCEPTANCE_MATRIX.md), guide (draft in records-wt), journal (draft in the
   scratchpad), QUALIFICATION / IMPLEMENTATION-REPORT / MANUAL-ACCEPTANCE,
   fast-forward and push.
4. Private delivery to draft release 399014941 (stays a draft): re-verify the
   prior asset `KingmakerBuffPlanner-0.2.0-rc6-casting-graph+ec34705c.zip`
   (sha256 214b795b...8b20564), upload the frozen bytes as
   `KingmakerBuffPlanner-0.2.0-rc6-everyday-use-v1.2+<commit8>.zip`, download
   back, verify with scratchpad `verify-zip.ps1`, update the draft notes
   (template `release-notes-v12.template.md`).
