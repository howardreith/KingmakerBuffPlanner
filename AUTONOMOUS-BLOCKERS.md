# Autonomous blockers — WP2A review correction, 2026-10-08

P2 focus-ownership and P3 group-origin review corrections are implemented and
regressed. Duplicate/Reload exit mode; refresh restores a lost active focus
through FocusGraphCasting and requests one reveal. Known origins are named.
Focused PASS=20 FAIL=0; complete C# PASS=418 FAIL=0; no compiler warnings.
Exact pre-correction HEAD 8d4e4f3c6f9810a1b02606f82bf1722f5256b9cf;
branch codex/kbp-not-ready-deep-linking-2026-10-07; version stays 0.3.0.
Production changes require a new complete gate and fresh runtime trial.

External qualification blocker: the Gunslinger lab repeatedly holds
C:/Dev/KingmakerGunslingerLab/compatibility-state/compatibility.lock and
launches Kingmaker against the shared installation. At 18:27 UTC PID 17384
was active. The unchanged complete source/mechanical gate cannot finish
until a sustained quiet installation window is available. Its prior archive
checks took about six hours. Gate 10 passed source 42 / C# 413 / filesystem
38 / evidence 23 / package 4, then refused foreign PID 5420 (exit 1).
Do not restart the full gate between brief launch gaps.

Prior physical UI evidence is positive and independently rechecked, but its
original outer completion is FAILED and preserved. Fresh fully successful
candidate-bound orchestration is mandatory. Its prior installation was
restored and protected saves compared clean; all 399 own deployment
transactions were Restored at the preceding audit. This correction has made
no new live deployment/input or save mutation.

Do not seize the foreign lease, stop foreign processes, relax archive purity,
bypass measured input idle >=180 seconds or mutate protected saves. Owner
instructed "Use the next idle window"; no repeated permission is needed.
Wait for the other lab to finish or for owner coordination in that session.

Next: clean correction commit and guarded push, clean prerequisite build,
sustained quiet window, complete unchanged gate, final exact candidate build,
fresh guarded HUD trial, restoration/protected-save verification and evidence.
No PR/merge/tag/release, permanent install or later work package.
Exact commands/evidence: docs/WP2A-NOT-READY-NAVIGATION.md and
AUTONOMOUS-RESUME.md. Linked-worktree writes use authorized reviewed calls.
