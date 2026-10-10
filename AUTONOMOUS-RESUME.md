# Autonomous resume — 0.4.2 owner-test candidate, 2026-10-10

Mission: `Claude_KBP_041_Followup_Prompt.md` plus the owner's continuation
instructions of 2026-10-10 (A Shadow Clone, B spellbook removal, C native paper
and sound). Branch `codex/kingmaker-buff-planner-041-owner-polish`, worktree
`repo/KingmakerBuffPlanner-OP042`, base v0.4.1 `ab62462`. Version 0.4.2.
Tested product commit `e9a4c5ed3d34825aa0332cb3d64067b206e29070`; later commits
are records only. Package 3f3d55af66aa33be94e28a50fa7247a78c5fb006ac292237617fa847326b3463, DLL b3f5023d74f539d5aa4452b7b0f43f64e69930b5f561fc9e4aff0b72498e7e0a,
MVID bcb3d03e-6bc9-48a3-8ff9-4b98dfb6f1c1. Complete gate PASS (exit 0, Source-only suite PASS=1 FAIL=0, 5 h 42 min).
Native: `kbp042-removal-05` PASS (no-input removal reconciliation),
`kbp042-paper-02` PASS (paper and open cue) on the tested bytes; earlier attempts
`-01`..`-04` kept as recorded. Pushed through the guarded helper (`aa053bb`, then this
publication record); draft PR #7 against main.
Full record: `docs/OWNER-FEEDBACK-0.4.2-HANDOFF.md`.
Next action: the owner's test of the candidate. No merge, tag, release or
permanent install without the owner's authorization.

# Autonomous resume — 0.4.0 owner-review candidate, 2026-10-09

Current: the 0.4.0 release train is complete through an owner-review
candidate, revised after the lead review of rc3. Branch
`claude/kbp-complete-remaining-work-2026-10-09` (worktree
`private/worktrees/KBP040`), published for review as
`codex/kingmaker-buff-planner-0.4.0`. Product candidate rc4
`009b6ddab80a5e70d7469962986c38222961f4ba`, version 0.4.0, package
`4be8a0b7dbf150d762350a88aa6a00eafac391fe0ac0e541d85dd53618f63adb`; later
commits are records only. rc3 `c452e01` is superseded. Every record, run id,
identity, the per-entry catalogue adjudication and the owner checklist are in
`docs/REMAINING-WORK-0.4.0-HANDOFF.md` and `planning/CATALOG-AUDIT-0.4.0.md`.
Next action: the owner's review; no merge, tag or release without his
authorization. The WP2A snapshot below is historical.

# Autonomous resume — WP2A qualification freeze, 2026-10-08T22:37Z

This is a dated pre-qualification checkpoint. Before resuming, read
artifacts/wp2a-running-checkpoint.json and the identity-bound evidence paths
below. A later complete, identity-matched record supersedes this snapshot.
Do not infer qualification from a prerequisite manifest or the old game PASS.

Mission: find blocked castings immediately. Branch
codex/kbp-not-ready-deep-linking-2026-10-07, worktree private/worktrees/WP2A.
Base origin/main b707c1f47859f2ecae517b2cd162d20ca18c7583; checkpoint HEAD
adbddd14d66861e26247ebb7e4f39158a2db7c6e, fix(ui): keep blocked navigation
aligned with inspector focus. Version 0.3.0. Original checkout and WP1
preserved; no WP1 branch inherited.

P2 and P3 are implemented and regressed. Duplicate and Reload leave problem
mode; refresh restores a lost active focus through FocusGraphCasting and
requests one reveal. Known group origins are named. Five regressions were
red against the prior code (15/5), then focused PASS=20 FAIL=0 and complete
C# PASS=418 FAIL=0. Source validation 42/0; no compiler warnings. Evidence:
artifacts/wp2a-review-regressions-{red,green}.log,
artifacts/wp2a-review-complete-csharp.log,
artifacts/wp2a-review-source-validation.log.
Guarded-push helper tests 6/0; the owned helper finished PASS, remote matched
adbddd14, worktree clean. Its first stderr-capturing wrapper failed after the
push had succeeded; the transcript-backed repeat verified PASS/up-to-date:
artifacts/wp2a-review-guarded-push-verified.log.

Clean prerequisite at adbddd14: artifacts/local-runtime/0.3.0/
KingmakerBuffPlanner-0.3.0-local-runtime.zip; manifest is that path plus
.build-local.json. ZIP 8af5b5d887bff40ef1dea90d48e7c3e18398b04f9f5aa41d787b12f5f29199c0;
DLL caf1bc50e691ee846a185bcde8dd928b6622cc5251bf07d078a378445e72285a;
MVID 81b584b4-6fee-43eb-9f16-61a3984dcd17. Frozen prerequisite:
artifacts/prerequisite-wp2a-adbddd1. Build-Local passed source 42/0, build
1/0, package 4/0, local 1/0. This is not the final qualified candidate.
This documentation checkpoint changes HEAD; rebuild a clean prerequisite
for that HEAD before the complete gate.

The full old gate at 783af83 does not qualify the production correction.
Gate 10 at f031f87 was incomplete (42 source / 413 C# / 38 filesystem /
23 evidence / 4 package passed, then foreign PID 5420 refused, exit 1).
No new full gate or live WP2A run has begun for this correction.

Owner's latest instruction: "Keep waiting for the other lab to finish."
The foreign lab repeatedly resumes after short gaps. At 22:37 UTC there
was no lease/game, but no completion signal. Observe only; do not seize
its lease, stop its processes or retry between brief gaps. Actual measured
desktop input idle >=180 seconds is still mandatory for physical input;
earlier authority "Use the next idle window" remains valid.

Next, after the other lab finishes:

1. Confirm ownership, clean HEAD, restored transactions and save integrity.
2. Run unchanged scripts/Test-SourceOnly.ps1 with zero failures on the
   frozen clean commit. Planned log: artifacts/wp2a-source-gate-11.log.
3. Run scripts/Build-Local.ps1 after that pass on the same clean commit;
   freeze exact ZIP, DLL SHA, MVID, manifest, gate log and commit in
   artifacts/qualified-wp2a-review-final.
4. Fresh run ID wp2a-review-final-problems-02 (unused at this checkpoint):
   Invoke-KingmakerRuntimeTest.ps1, live-workspace-physical,
   PhysicalExpectation problems, full-user, Automation, owner display,
   instant, TimeoutSeconds 1200, no native casting allowance. Use Windows
   PowerShell -Command, guarded WhatIf purity first, then the next eligible
   idle window. Never use -File or bypass ownership.
5. Inspect actual screenshots and clipped card geometry, translated reason,
   problem position/controls, exact intent/profile/Undo/review equality and
   zero dispatch/run/resource/effect changes. Require fresh complete outer
   orchestration plus verified restoration and protected-save comparison.
6. Write the measured handoff in the generated candidate evidence directory
   and update the ignored running checkpoint; keep the frozen source commit
   unchanged so gate, candidate, runtime record and clean pushed HEAD agree.

Fresh runtime evidence path:
C:/Dev/KingmakerBuffPlannerLab/runtime-evidence/wp2a-review-final-problems-02.
A final handoff must cite the exact frozen commit and manifest, complete
source-gate result, run-completion.json and verified screen evidence.

Preserve wp2a-783af83-problems-01: game PASS and strong physical UI evidence,
but original outer completion FAILED. Independent corrected recheck passed;
it does not retroactively qualify that run. Installation restored, protected
saves clean; all 399 own transactions were Restored at the 18:37 audit.
No new live staging, input or save mutation in this review correction.

At this checkpoint: implemented and regressed; final source-qualified and
runtime-qualified pending. Not native-qualified, released or owner-accepted.
No PR, merge, tag, release, permanent install, protected/ordinary save
mutation, foreign interference or later work package. See
docs/WP2A-NOT-READY-NAVIGATION.md for the dated engineering evidence.
