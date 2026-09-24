# Casting-first planner: player guide (experimental)

This guide covers the casting-first planner in the release candidate. The
classic planner stays the default and is unchanged. Only part of what is
described here has been checked in the game so far; the table in "What
runs in this version" says which, and "Qualification status" at the end
has the details.

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

## Adding and editing castings

**The next casting** (the inspector when no card is selected): choose the
buff in the grid, then **Cast by** (the caster). If that caster can cast
the buff in more than one way (the same spell in two spellbooks, or a
spell and an item), **Cast from** lists each way by name - for a spell,
the spellbook, its spell level and the kind of slot it spends (for
example "Linzi: Bard level 1 (spell slot), caster level 5"); for an item
or ability, the resource it spends - and its caster level; pick one, or
Add is refused. A spell the caster knows at two levels of the same
spellbook cannot be pinned to one of them in this version: that choice is
refused with the reason. Then choose the target (or the group origin and
the recipients the group must reach), the enhancements (choosing another
caster keeps only the enhancements that caster has), and **If the buff is
already there**: skip this casting (the default) or cast it again anyway.
**Plan settings** holds **Instant mode: animate buffs that cannot be
instant** (in Instant mode, a buff that cannot be cast instantly is cast
with its animation when this is on, and refused when it is off) and
**Cast only out of combat**; both are saved with the plan.

**One existing casting** (press Edit on its card): the inspector edits only
that casting.

- **Cast by (this casting)** lists every way anyone in the party can cast
  its buff: the caster and the exact spellbook and level, item or ability.
  Picking one changes the caster and the source together and keeps
  everything else. The same caster keeps all of its enhancements; another
  caster keeps only the ones it has, and the footer names each one it
  could not take (a rod stays with its owner; Undo brings it back).
- **Routine and order** moves the casting to another routine (at its end)
  or one place earlier or later in its own.
- **If the buff is already there** chooses skip or cast again.
- Targets, group origin and coverage, enhancements, **Mark Ready**,
  **Disable** and **Remove**, as before. A single-target casting can
  become a group casting centred on its caster (its target becomes the
  required coverage), and a group casting can become a single-target
  casting on any member (**Or a single target**).
- Review items of an imported casting are listed in words.

An imported casting whose classic plan let the planner pick any caster
gets its caster here: pick one under **Cast by**, press **Resolve review**,
then **Mark Ready**. One whose classic plan did not say single target or
group becomes Ready the same way after you choose **Or a single target**
(or keep it a group). Each keeps its place and its import record.

The footer notes when running every routine in one pass would leave
castings of the selected routine short of a resource that they have when
their routine runs alone.

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
  where you allowed the animated fallback (**Plan settings**). The choice
  is saved with the plan. It changes only how the game performs the castings, never which
  castings run, their sources, targets or costs, so switching it does not
  need a new review; the mode in use is shown on the HUD tooltip and
  recorded in the log with every run.

## Buffs that are already active

Each casting has an existing-effect choice. **Skip if already active**
(the default) skips a casting only when every intended recipient already
has the complete effect, and the existing effect is at least as good as
what this casting would give:

- it is not suppressed, and the game let the planner read that;
- it does not come from a lower caster level than this caster, with both
  caster levels readable (an effect whose caster level cannot be read is
  never assumed to be as strong);
- it carries every strength-changing metamagic the casting would apply
  (Empower, Maximize, Extend, Heighten; Quicken and Reach do not count);
- at least half of the duration this casting would give remains. This is
  compared only when the spell duration is "per level"; a permanent or
  worn-item effect always has enough;
- whatever the spell, at least two rounds of it remain (an effect about
  to expire is always recast).

A weaker, expiring or unprovable existing effect is recast, and the card
says why. **Cast it again anyway** (Always recast) casts regardless. A
cast counts as done only when this cast put the effect there: a new
effect, or the old one renewed to a later end. An effect that was already
there and did not change, or one the game suppresses, never confirms a
cast; if nothing landed, the routine stops there and says so. Skip if
already active therefore does not prevent this whenever the casting
still casts over an existing effect: if the game keeps that effect
instead of replacing it, the cast is reported as not confirmed.

One case is handled: a group casting that still casts because some of
its recipients lack the buff, while others already have a provably
good-enough one (for example a longer-lasting casting from earlier). It
is cast once for the others, at its usual cost; the card says "already
active on ... (the cast goes ahead for the others)". Those recipients may
keep their effect unchanged; every other recipient still needs the effect
to land. The game may instead replace their effect with this cast's
shorter one (in the qualification run it replaced a fighter's longer
Protection from Alignment with the communal form's): when a recipient's
effect lasts longer than this cast gives, the card says "... lasting
longer than this cast (the cast goes ahead for the others and may shorten
it)", and a casting that gave the longer effect may be due again the next
time the routine runs. The recipient must have had its buff before the
routine started: a buff given by an earlier casting in the same routine
does not count, so the group cast must reach that recipient too. For a
group casting the
intended recipients are its required coverage, or everyone it would
reach when no coverage is required. A casting whose buff is already
active does not need a free slot, so running a routine again right after
it ran skips the castings that are still in effect instead of refusing.

## What runs in this version

"Checked in the game" means a guarded run on a disposable test campaign
observed the real result (effect, resources, submissions). Everything
else has passed source and recorded-runtime tests only.

| Casting setting | In this version | Checked in the game |
| --- | --- | --- |
| Cantrips (cast at will through the ability the class grants, as the game's action bar casts them) and other verified free sources, direct target | Runs, no resource spent | Yes, in both casting modes: Resistance cast by a bard and a sorcerer on other party members; resources unchanged |
| Animated casting (the default) and Instant | Runs | Yes, both |
| Several casters in one routine, each casting with its own caster | Runs | Yes (two casters, three castings) |
| Skip if already active / Always recast | Runs | Yes, with free sources: an active effect is skipped, a repeat press casts nothing, Always recast casts again |
| Stopping a running routine (press a routine button) | Runs | Yes: pressed during a cast in progress, that cast finished and nothing after it started |
| Disabling the mod during a run | The run ends; a cast in progress is interrupted and cleaned up | Yes: in animated mode during a cast (interrupted, nothing landed), in instant mode before the first cast; after the planner was enabled again a new run completed, with the planner's subscriptions and HUD unchanged |
| A cast the game refuses | The routine stops there; nothing after it runs | Yes, once, before the cantrip fix: the failed cast was reported, nothing was spent and the rest were not attempted |
| A free cantrip whose class ability is gone when the routine runs | Refused; it is never cast from a spell slot instead | Not in the game (source tests) |
| A cast whose resource use the game cannot report | Treated as uncertain; the routine stops there | Not in the game (source tests) |
| Close and reopen the planner; the accepted plan survives | Runs | Yes |
| Spellbook spells from prepared slots or spontaneous levels, direct target | Runs, budgeted in order | Not yet (the test party has only cantrips) |
| Exact prepared slots and ability pools shared across castings | Runs, budgeted in order | Not yet |
| Group spells, caster-centred or anchored origin, predicted coverage | Runs | Not yet |
| Metamagic spell variants, metamagic rods (any matching rod) | Runs | Not yet |
| Targeting modifiers (for example Share Transmutation) | Shown as "cannot run in this version"; Apply refuses | n/a |
| A specific physical rod or item | Shown as "cannot run in this version"; Apply refuses | n/a |
| Group castings whose required recipients are outside the predicted area | Shown as "cannot run in this version"; Apply refuses | n/a |

A casting that cannot run is never silently changed into something that
can: it has to be edited, disabled, or left out with Ready Casts Only.

The classic planner (the default mode) shares the casting code. Its Long
routine was checked in the game in both casting modes: Resistance cast
through the at-will class ability, the effect landed, and no spell slot or
ability pool changed. A classic routine now also stops at the first cast
that is not confirmed, and it casts only while the game runs: **APPLY**
in the open classic planner checks the routine at once, then closes the
planner and casts (the game is paused while the planner is open); the
result is shown when you open the planner again. If you open the planner
again while the routine is still running, it waits until you close it,
and the planner and the HUD button say so.

## Files

All player data lives in the mod folder under `UserSettings`:

| File | Content |
| --- | --- |
| `planner-mode.json` | The chosen planner mode |
| `kingmaker-buff-planner-casting-<campaign>.json` (+ `.bak1..3`) | The casting-first plan and its execution settings |
| `kingmaker-buff-planner-review-<campaign>.json` | Which routine contents you accepted (digests only) |
| `kingmaker-buff-planner-<campaign>.json` | The classic plan, never modified by the casting-first planner |
| `kbp-casting-<hash>.orig` | Byte-exact archive of the classic plan taken at import |

A plan, review or mode file this version cannot read (for example one
written by a newer planner) is never overwritten. Both planners say so
when they open: the classic planner shows a new setup and **changes are
not saved** in its status line, and the casting-first planner shows an
empty plan with **not saved** in its header for as long as saving is
blocked. A backup loaded because the main file could not be read is
named as such (saving stays refused until that file is moved aside); a
backup loaded because the main file is missing saves normally. To save
again, move the unreadable casting plan and its backups (`.bak1` to
`.bak3`) out of `UserSettings` and press **Reload**: the casting-first
planner then starts over (importing the classic plan into an empty plan,
or keeping the castings you added meanwhile, which the next Save writes).
A classic plan saved by version 0.0.19 or earlier is read and imported as
it is (its file is not rewritten); the import uses the classic planner's
own copy of the plan, matched to the party's current abilities.

Installing a new version keeps `UserSettings` exactly. Rolling back to an
older version keeps every file; a casting-first plan the older version
cannot read is moved to the rollback evidence folder instead of being left
where the older version would misread it.

## Qualification status

The casting-first execution path has passed source tests, mutation tests
and recorded-runtime tests, and on a disposable test campaign (never an
ordinary save) it has cast in the game:

- a two-caster Resistance routine, in animated and in instant mode,
  through the same Apply a player uses and the planner's own per-frame
  execution: stopped by a routine press during its first cast, completed
  (skipping the buff already active), pressed again (nothing to cast),
  recast with Always recast after closing and reopening the planner, and
  disabled during a cast (interrupted, nothing landed, runs possible again
  once enabled), then run again as a new routine after the enable, which
  completed with the planner's event subscriptions and HUD unchanged.
  Every step matched its prediction; no save was written.

The animated run also found a defect in the previous candidate: a
spontaneous caster's cantrip failed because it was cast from the
spellbook's level-0 entry, which needs a level-0 slot these classes do
not have. Cantrips are now cast at will through the ability the class
grants, as in the game's own action bar.

Spells that spend slots, group spells, metamagic and rods have not been
cast in the game yet; they need a test campaign with a more advanced
party. The workspace itself, the save and reopen cycle and the planner's
text were checked in game frames; the owner has not yet accepted the
workspace's usability. Treat casting-first mode as experimental and keep
the classic planner for normal play.
