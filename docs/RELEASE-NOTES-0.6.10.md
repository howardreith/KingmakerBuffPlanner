# Mr. Bard's Kingmaker Buff Planner 0.6.10

Owner-authorized stable update following merged PR #19.
This exact release has not been run in the game.

## Changes

- Routines now default to **Cast available**: cast the affordable planned buffs
  in your saved order and retain every unavailable assignment for later.
- Visible lines in the game's normal log identify every uncast entry, including
  its caster, target/group and reason. No-op and refused attempts log too, even
  with the planner open. Availability refusals no longer force the planner open.
- **Require full plan** is an unchecked native checkbox in the execution footer,
  beside Resources and Mode. Check it to prevent starting when a needed planned
  casting is unavailable. Otherwise-ready casts are reported as withheld.
- An active run retains its original reporting snapshot if you reopen the mouse
  planner and attempt another routine. Casting numbers and preflight reservation
  explanations remain accurate after spending.

## One-time profile upgrade

Format **6.5** initializes Cast available once for supported older profiles,
including files that serialized Require full plan. Old files did not distinguish
an untouched default from a deliberate strict choice, so players who want strict
mode must enable it once after upgrading. Subsequent valid choices persist.

Assignments, routine order, exact selected sources, targets, enhancements and
other preferences are preserved. The upgrade uses normal atomic persistence and
must become durable before native casting can start. Existing backup, import and
protected-file safeguards remain in effect.

## Execution and verification limits

Already-active/recast and disabled rules retain their meaning. Unexpected late
validation or execution failures, unconfirmed applications and uncertain cleanup
still stop later casts. Require full plan checks readiness before execution; it
cannot roll back native casts or resources already spent after a run starts.

Native rendering, physical input, game-log display, actual casting and Linux
acceptance have not been verified for this release. This update does not establish
a native Heroism repair or native prepared-slot reuse between runs without
replenishment. The owner-authorized publisher runs the complete offline behavior
suite, deterministic release builds and strict package validation; those checks
are not in-game acceptance.
