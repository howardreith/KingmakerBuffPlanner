# Autonomous blockers — 0.4.0 owner-review candidate, 2026-10-09

Open items for rc4 (details in `docs/REMAINING-WORK-0.4.0-HANDOFF.md`):
owner acceptance of every package; whether Light and Daylight (utility light)
belong in the catalogue (owner's product-scope decision); the
human-reproduction compatibility profile's BagOfTricks fixture identity no
longer matches (owner decision); no native run is possible for the
strict-Instant blocker (no animated-only source in any guarded fixture), group
re-centring or Share visibility (not in the physical fixture); OS-input
physical runs need the desktop idle (Windows refuses the game foreground
activation while the desktop is in use). The WP2A snapshot below is
historical.

# Autonomous blockers — WP2A qualification freeze, 2026-10-08T22:37Z

Dated pre-qualification snapshot. Check artifacts/wp2a-running-checkpoint.json
and the exact candidate-bound gate/runtime records for any later outcome.

Review corrections P2/P3 are committed and guarded-pushed at checkpoint HEAD
adbddd14d66861e26247ebb7e4f39158a2db7c6e on
codex/kbp-not-ready-deep-linking-2026-10-07; version remains 0.3.0.
Focused regressions 20/0, complete C# 418/0, source validation 42/0, push-helper
checks 6/0; no compiler warnings. Five new regressions failed before the fix.
Duplicate/Reload exit mode; active navigator and inspector align through
canonical focus; known group origins appear. See the detailed WP2A report.

External blocker: the other lab repeatedly uses the shared installation.
Owner explicitly answered "Keep waiting for the other lab to finish."
At 22:37 UTC no lease/game was present, but completion was not confirmed.
Brief gaps have repeatedly ended in another launch, including after a
twenty-minute quiet interval. The unchanged full gate takes about six hours;
do not start it between those gaps or weaken its archive/purity checks.

The adbddd14 clean prerequisite is frozen in
artifacts/prerequisite-wp2a-adbddd1; it is not final qualification.
This documentation checkpoint changes HEAD and requires a matching clean
prerequisite. Final source gate, post-gate build and fresh physical run must
share one frozen clean commit. Planned gate log:
artifacts/wp2a-source-gate-11.log; final evidence directory:
artifacts/qualified-wp2a-review-final; unused fresh trial:
wp2a-review-final-problems-02. Write final measured evidence there rather
than changing the qualified source commit afterwards.

Prior wp2a-783af83-problems-01 has positive, independently rechecked game
evidence but a preserved FAILED outer completion. Fresh complete successful
orchestration is mandatory. Its install restoration/protected-save comparison
passed; all 399 own transactions were Restored at the 18:37 audit. No new
WP2A live staging, input or save mutation in this correction.

Do not touch the foreign lease/processes or protected/ordinary owner saves.
Measured input idle >=180 seconds is required; "Use the next idle window"
remains authorized. After lab completion, finish the unchanged full gate,
build/freeze the final exact candidate, run the guarded physical problems
trial, verify all restoration/claims/save evidence, and hand off a clean
guarded-pushed branch. No PR/merge/tag/release/permanent install/later package.
Exact procedure and evidence contracts: AUTONOMOUS-RESUME.md.
