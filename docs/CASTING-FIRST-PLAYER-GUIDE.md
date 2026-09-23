# Casting-first planner: player guide (experimental)

This guide covers the casting-first planner in the release candidate. The
classic planner stays the default and is unchanged. Nothing described here
has been qualified in gameplay yet; see "Qualification status" at the end.

## Turning it on

1. Open the Unity Mod Manager window (Ctrl+F10 by default) and select
   Kingmaker Buff Planner.
2. Under **Planner mode**, choose **Casting-first planner (experimental)**.
   The choice is saved in `UserSettings/planner-mode.json` and applies to
   every campaign.
3. Open the planner (the HUD Setup button, or Ctrl+Shift+B).

The first time a campaign opens in this mode, its classic plan is
imported into a separate casting-first plan. The classic plan file is
never modified; a byte-exact copy of it is also archived. Imported
castings that need a decision are marked for review, and plan-wide
legacy constraints (provider bans, caps, priorities) must be acknowledged
before anything can run.

To switch back, choose **Classic planner** in the same panel. The classic
plan is exactly as it was; the casting-first plan stays in its own file
for the next time. The mode cannot be changed while a routine is running.

## How a plan runs

Each saved casting is exactly one cast: one caster, one spell source and
variant, one target (or one group origin), and its own enhancements.
Nothing is expanded, merged or substituted behind your back.

- **Review, then accept.** The planner shows each routine (Long,
  Important, Short) as cards. Press **Accept Plan** once you have checked
  it. A routine runs only while its accepted contents still match. If a
  casting changes (caster, source, target, origin, enhancements, cost, the
  existing-effect choice, or it becomes blocked), the routine needs a new
  review. Buffs expiring or being applied do not. The HUD tooltip says
  whether an accepted plan is on file; a press still re-checks it and
  refuses if anything material changed.
- **Run.** Use **Review & Apply** in the planner (the planner closes while
  the party casts) or the routine buttons on the HUD. Both use the same
  checks: the party state is re-read at that moment, and nothing runs on
  a stale plan.
- **Blocked castings.** Ordinary Apply refuses the whole routine if any
  casting is blocked (for example no slot left, or the target is gone).
  **Ready Casts Only** runs the ready castings and lists what it left out.
  It never waives a required enhancement or an unacknowledged legacy
  constraint.
- **Stopping.** Press the running routine's HUD button again. The cast in
  progress finishes normally, and nothing after it starts. Changing area,
  disabling the mod or closing the game stop a run at once instead: the
  cast in progress is interrupted and cleaned up, and its slot may be
  spent without the effect (the result says so).
- **Results.** The planner footer shows the last run: casts confirmed,
  castings skipped because the buff was already active, omitted castings,
  a failed cast and why, and resources spent (free casts counted
  separately). The full per-casting record is in the Unity Mod Manager
  log (`[KBP-CF-RUN]`). A failed or uncertain cast stops the rest of the
  routine; nothing is retried automatically, and spent resources or
  applied effects are never described as undone.
- **Casting mode.** The footer button switches between **Animated**
  (native casting animations, the default) and **Instant**. Instant mode
  still uses animated casting where a step needs a native command, or
  where you allowed the animated fallback. The choice is saved with the
  plan. It changes only how the game performs the castings, never which
  castings run, their sources, targets or costs, so switching it does not
  need a new review; the mode in use is shown on the HUD tooltip and
  recorded in the log with every run.

## Buffs that are already active

Each casting has an existing-effect choice. **Skip if already active**
(the default) skips a casting only when every intended recipient already
has the complete effect, and the existing effect is at least as good as
what this casting would give:

- it is not suppressed;
- it does not come from a lower caster level than this caster;
- it carries every strength-changing metamagic the casting would apply
  (Empower, Maximize, Extend, Heighten; Quicken and Reach do not count);
- at least half of the duration this casting would give remains. This is
  compared only when the spell duration is "per level"; a permanent or
  worn-item effect always has enough;
- whatever the spell, at least two rounds of it remain (an effect about
  to expire is always recast).

A weaker, expiring or unprovable existing effect is recast, and the card
says why. **Always recast** casts regardless. For a group casting the
intended recipients are its required coverage, or everyone it would
reach when no coverage is required. A casting whose buff is already
active does not need a free slot, so running a routine again right after
it ran skips the castings that are still in effect instead of refusing.

## What runs in this version

| Casting setting | Status |
| --- | --- |
| Spellbook spells, prepared or spontaneous, direct target | Runs |
| Cantrips and other verified free sources | Runs, no resource spent |
| Exact prepared slots and spontaneous or ability pools, shared across castings | Runs, budgeted in order |
| Group spells, caster-centred or anchored origin, predicted coverage | Runs |
| Metamagic spell variants, metamagic rods (any matching rod) | Runs |
| Several casters in one routine, each casting with its own caster | Runs |
| Skip if already active / Always recast | Runs |
| Targeting modifiers (for example Share Transmutation) | Shown as "cannot run in this version"; Apply refuses |
| A specific physical rod or item | Shown as "cannot run in this version"; Apply refuses |
| Group castings whose required recipients are outside the predicted area | Shown as "cannot run in this version"; Apply refuses |

A casting that cannot run is never silently changed into something that
can: it has to be edited, disabled, or left out with Ready Casts Only.

## Files

All player data lives in the mod folder under `UserSettings`:

| File | Content |
| --- | --- |
| `planner-mode.json` | The chosen planner mode |
| `kingmaker-buff-planner-casting-<campaign>.json` (+ `.bak1..3`) | The casting-first plan and its execution settings |
| `kingmaker-buff-planner-review-<campaign>.json` | Which routine contents you accepted (digests only) |
| `kingmaker-buff-planner-<campaign>.json` | The classic plan, never modified by the casting-first planner |
| `kbp-casting-<hash>.orig` | Byte-exact archive of the classic plan taken at import |

Installing a new version keeps `UserSettings` exactly. Rolling back to an
older version keeps every file; a casting-first plan the older version
cannot read is moved to the rollback evidence folder instead of being left
where the older version would misread it.

## Qualification status

The casting-first execution path has passed source tests, mutation tests
and recorded-runtime tests. It has **not** yet cast a spell in the game:
the first live casting (one Resistance cantrip on a disposable test
campaign) is waiting for the owner, and the broader in-game
qualification follows it. Until then, treat casting-first mode as
experimental and keep the classic planner for normal play.
