# Autonomous resume — WP2A, 2026-10-08

Active mission: find blocked castings immediately. Branch
codex/kbp-not-ready-deep-linking-2026-10-07, worktree private/worktrees/WP2A.
Base b707c1f47859f2ecae517b2cd162d20ca18c7583; exact HEAD before this
documentation checkpoint: f031f87e71aca99c09118495b9659e24bdcaace1.
Version remains 0.3.0. No Work Package 1 changes inherited.

Implemented and regressed: ordered structured blockers; shared planner/HUD
focus; transient problem navigation and repair recomputation; measured
one-shot reveal; global refusal lifecycle; blocked-only physical scenario.
Focused C# regressions PASS=15 FAIL=0. The complete C# runner PASS=413
FAIL=0 on the current implementation. Independent evidence tests PASS=23
FAIL=0 after correcting their actual acknowledgement/signature contracts.

A complete Test-SourceOnly passed on 783af83684146cbbd9abda13e5ac62dd3818fdf8
(artifacts/wp2a-source-gate-5.log). Its clean candidate's physical run
wp2a-783af83-problems-01 produced game PASS and visible offscreen-to-onscreen
HUD navigation, Next/Previous and nested Escape, with no native submission,
resource/effect change, authored change or profile write. The original
outer completion is FAILED because of the evidence-reader schema defects;
it is preserved unchanged. Restoration and protected-save comparisons
passed. The corrected independent recheck passed. A fresh complete outer
run on the corrected candidate is still required.

The current complete gate is NOT complete. Gate 10 on f031f87 passed source 42 / C# 413 / filesystem 38 / evidence 23 / package 4, then refused foreign PID 5420 and exited 1 (artifacts/wp2a-source-gate-10.log). It began after twenty observed quiet minutes, which did not guarantee the several-hour window. No new deployment or physical trial occurred. Gate 6 was interrupted; gates
7, 8 and 9 stopped safely when the Gunslinger lab launched Kingmaker.
Gate 9 passed source 42 / C# 413 / filesystem 38 / evidence 23 / package 4,
then the deployment WhatIf refused foreign PID 11240. Source-only archive
purity checks need a sustained quiet shared-installation window (the prior
complete gate took about six hours). Do not weaken or skip those checks,
repeat them between short launch gaps, or interfere with the foreign lease.
The owner instructed "Use the next idle window" for physical qualification.

Next: wait for the other lab to finish; build a prerequisite ZIP on the
clean documented HEAD; finish unchanged scripts/Test-SourceOnly.ps1 with
zero failures; Build-Local after that gate; record exact package/DLL/MVID/
manifest identity; run candidate-bound live-workspace-physical with
PhysicalExpectation problems, full-user, Automation, owner display, no
casting allowance, and measured desktop input idle >=180 seconds.
Verify fresh complete orchestration, actual clipped card visibility,
translated reason and problem position; restore and reconcile all claims
and protected saves. Record final evidence and use the guarded push helper.
Stop at a clean pushed branch. No PR, merge, tag, release, permanent install,
native-effect claim, owner-acceptance claim or later work package.

Exact counts, hashes, evidence and rejected assumptions:
docs/WP2A-NOT-READY-NAVIGATION.md. The ignored
artifacts/wp2a-running-checkpoint.json carries the latest wait observation.
