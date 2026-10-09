# Buff Planner: player guide (0.4.0)

This guide describes the casting-first planner of 0.4.0 (an owner-review candidate; 0.3.0 is the
published release). It is
the only normal planner: there is
no Save button, no Accept Plan step and no Classic switch in the planner.
"What has been checked in the game" at the end says exactly which parts a
guarded in-game run has observed at this build, and which have not.

## Opening the planner

- **Ctrl+Shift+B**, or the HUD's **Setup (gear)** button, opens the
  planner. The mod's page in Unity Mod Manager (Ctrl+F10) is a normal entry
  point too and names both.
- A game that still uses the Classic planner (chosen in an earlier version)
  keeps it until you choose **Switch to the casting-first planner** on the
  mod's settings page. The switch is one way: there is no route back to
  Classic inside the game. Your classic plan file is never changed.
- New and unset profiles start in the casting-first planner with **Instant**
  casting. An explicit **Animated** choice from an earlier version is kept.

## The first open after upgrading

The first time a campaign opens, its classic plan (if it has one) is
imported into a separate casting-first plan:

- the classic plan file is not modified, and a byte-exact copy of it is
  archived beside it (`kbp-casting-<hash>.orig`);
- anything the import cannot decide for you stays a **Draft** with its
  reason in words. A classic casting that let the planner choose "any
  caster" is imported with no caster: pick one under **Cast by** - the
  planner never guesses it from today's party;
- the imported plan saves itself, so closing the planner or restarting the
  game keeps it exactly as imported.

## Authoring castings

Each casting is exactly one cast: one caster, one spell source and variant,
one target (or one group origin), and its own enhancements. Nothing is
expanded, merged or substituted behind your back.

1. Pick a buff in the list on the left (search, or the All / Spells /
   Abilities / Other tabs).
2. Pick a caster in the left lane. When the caster can cast the buff in
   more than one way, pick the exact source row (spellbook and level, item
   or ability); each row says what the whole plan leaves of that pool.
3. Click a target on the right: one line and one casting card appear, and
   the new casting is selected. Two allies are two castings.
4. Work on the selected casting directly in the graph (0.4.0):
   - click its recipient's portrait again to **remove** it;
   - click another portrait to **move** it there (a group casting is
     **re-centred** there instead); a portrait it cannot reach is refused
     with the reason and nothing changes;
   - click another caster, or another exact source row, on the left to
     **change who casts it** - its target, routine, order, enhancements the
     new caster has and "already there" choice stay; an enhancement only
     the old caster had is named and dropped (Undo brings it back).
   With nothing selected, a portrait that already has a casting of this buff
   selects that casting instead of adding a second one, and **Done** (or
   Escape) clears the selection.
5. The panel on the right shows the selected casting's status and reasons,
   its target or group centre, enhancements, routine and order, **If the
   buff is already there** (skip, or cast it again anyway) and **Remove**.
   There is no Disable or Duplicate any more: remove a casting you do not
   want. A casting an earlier version left **Disabled** still loads, is
   never cast, blocks nothing, and can be removed.
6. **Undo** undoes the last edit (and saves).

**Group castings** (0.4.0) are one cast centred on one party member (or
the caster). The cast affects whoever the spell's area reaches from that
centre when it is cast; the panel shows who is expected to be reached, but
nobody is "required" and a member standing outside the area never blocks
the cast. Plans saved by 0.3.0 with required recipients still load; those
choices are archived beside the plan (`*.pre-0.4.0.orig`) the first time
the plan is saved and no longer constrain anything.

**Enhancements** show only what is meaningful for that exact caster,
source and spell (for example Extend or Brown-Fur Powerful Change when they
apply; Piercing or Persistent are not offered for a beneficial spell such
as Good Hope). An enhancement you chose earlier that no longer applies stays
visible on the casting with the reason, so you can remove it; it is never
dropped silently.

**Share Transmutation** (Brown-Fur Transmuter, from the KingmakerGunslinger
mod): for an eligible personal transmutation, after choosing the exact
caster and source, choose **Share with an ally** before clicking the
target, then click the ally. The choice appears only for a caster and
source that can use it; a saved casting whose Share no longer applies keeps
it visible with the reason so you can untick it. Without Share the spell stays self-only. The
Arcane Reservoir cost is budgeted together with the spell's own cost; when
either is short the casting is blocked as a whole. Turning Share off on a
casting keeps its ally target visible and blocked (so you can repair it or
Undo); it does not change castings you already made.

## Everything saves itself

Every deliberate edit - adding, moving, retargeting, changing a caster or
source, choosing enhancements, Undo, changing a setting or the casting
mode - is saved the moment you make it. Browsing, hovering, reading a
description and selecting never write anything.

The footer shows the save state: **Saved**, **Saving...** or **Not saved**.
If a save fails, the footer says why, your edits stay in the planner, the
last good file on disk is kept, and runs refuse until the newest edit is
saved. The footer then offers **Retry save** (or **Reload** when the stored
plan itself could not be read).

## Reading a spell's full description

Right-click a buff in the list, the selected buff's header, or a casting
card: the game's own full description of that exact spell opens as a spell
scroll, with the spell's name, a line with its duration (and whether you
are reading the exact source you selected), and the description below.
Long descriptions scroll with the mouse wheel; the wheel never moves the
planner behind the scroll. **Escape** closes the description first, then
the planner; the planner's Escape never opens the game's own menu.

Clicking anywhere outside the scroll (on the dimmed planner) also closes
the description, and that click does nothing else: it does not select,
add or change anything underneath, and never reaches the game. To read
another spell, right-click it once the description has closed. Reading
descriptions never changes the plan.

When the game's own parchment can be borrowed, the planner and the scroll
are drawn on it; otherwise they keep the plain parchment colour. Both look
and work the same way.

## Running a routine

- On the HUD, the **moon** runs **Long**, the **diamond** runs
  **Important** and the **sun** runs **Short** - one left-click each. The
  planner does not open. The planner's own **Run** button runs the routine
  selected in the planner (the planner closes while the party casts).
- Every route uses the same checks: the latest saved plan, the party as it
  is at that moment, and every casting re-checked before it is cast.
  Nothing runs on a stale plan and no routine needs an acceptance step.
- **Blocked castings.** A routine is refused as a whole if any of its
  castings is blocked (for example no slot left, or the target is gone);
  the refusal names the reason. **Ready Casts Only** runs the ready
  castings and lists what it left out; it never waives a required
  enhancement.
- **Never during combat (0.4.0).** A routine pressed while the party is in
  combat is refused with "Buff routines cannot run during combat." Nothing
  is spent and no casting is singled out; press again once combat is over.
  This is no longer a setting.
- **Instant or Animated (0.4.0).** The planner's **Mode** button chooses.
  Instant never falls back to a normal animated cast: a casting that cannot
  be cast instantly is Not Ready while Instant is chosen, and the planner
  shows it with the reason. Choose **Animated** to cast it with the game's
  normal casting. The old "animate buffs that cannot be instant" checkbox is
  gone.
- **Already active.** "Skip if already active" (the default) skips a
  casting only when its recipient already has an effect at least as good;
  a weaker or expiring effect is recast, and the card says why.
- **Stopping.** Press the running routine's HUD button again: the cast in
  progress finishes and nothing after it starts. Changing area, disabling
  the mod or closing the game stop a run at once.
- **Results.** The footer shows the last run (casts confirmed, skipped,
  omitted, a failed cast and why, resources spent). The full per-casting
  record is in the Unity Mod Manager log (`[KBP-CF-RUN]`).

## Files

Everything the planner stores is in the mod folder under `UserSettings`:

| File | Content |
| --- | --- |
| `planner-mode.json` | The planner mode (casting-first unless an older Classic choice is still stored) |
| `kingmaker-buff-planner-casting-<campaign>.json` (+ `.bak1..3`) | The casting-first plan and its settings |
| `kingmaker-buff-planner-<campaign>.json` | The classic plan, never modified by the casting-first planner |
| `kbp-casting-<hash>.orig` | Byte-exact archive of the classic plan taken at import |

A plan file this version cannot read (for example one written by a newer
build) is never overwritten: the planner says so and refuses to save until
it is resolved.

## Installing and rolling back

Install archive-first: back up the whole `Mods\KingmakerBuffPlanner` folder
(including `UserSettings`) outside the `Mods` folder before replacing it,
then confirm Unity Mod Manager lists version 0.4.0 (its log line
`[KBP-BOOT] Main.Load exited;version=0.4.0;commit=<commit>` names the
release commit). To roll back, copy the new `UserSettings` outside `Mods`,
delete the new folder and copy your backup back. The release notes give the
exact steps and the package checksum.

## What has been checked in the game

0.4.0 (candidate `c452e01b`, guarded runs `kbp040-rc3-*` on 2026-10-09, disposable
test campaigns only; full record in `docs/REMAINING-WORK-0.4.0-HANDOFF.md`):

| What | Checked in the game |
| --- | --- |
| Spellbook Buff Planner button | Yes: opened the planner three times from the native spellbook, closed with Escape; a simulated failure reopened the spellbook |
| Routines during combat | Yes, Instant and Animated: refused with "Buff routines cannot run during combat.", nothing spent, nothing opened; the next press after combat behaved normally |
| Direct graph editing | Yes (mouse): add, same-portrait remove, move, change caster, Undo, Escape leaves the selection then closes; edits saved themselves. Group re-centring and Share visibility: not in the test party (checked in code) |
| Magic Circle against Alignment | Yes, Instant and Animated: cast on an ally, slot spent once, circle confirmed, nothing left held |
| Buff catalogue | Yes: Light and the Heal skill's Treat Affliction / Treat Deadly Wounds no longer listed; catalogue exports before/after for three mod profiles |
| Scroll paper and spell scroll | Yes at 1920x1080 and 1280x720 windowed; the description scrolls on its own; Escape closes it first, never the game menu |
| Instant "Not Ready" for an animated-only casting | No: no test party has such a source after the Magic Circle repair (checked in code) |

0.3.0 and earlier:


Every row below was observed by a guarded run of the qualified candidate
(source commit `8d7681d0`, batch r15, 2026-10-03) on disposable test
campaigns, never an ordinary save; the 0.2.0 and 0.3.0 release commits add only the
version number and documentation to it. The full record with run ids is
`docs/E01-E27-ACCEPTANCE-MATRIX.md`.

| What | Checked in the game |
| --- | --- |
| Moon click with the planner never opened | Yes: Long ran once from a plan stored by an earlier session, only Long (Important and Short castings untouched), the effect landed, the planner stayed closed, nothing extra was submitted |
| Right-click description | Yes: the panel shows the spell's own game text and name; the wheel scrolls long text and not the graph; Escape closes it, then the planner, never the game menu |
| Continuous scroll | Yes: an overflowing plan scrolled under the mouse wheel on one uninterrupted parchment |
| Autosave, reopen and save reload | Yes: three castings, a retarget and Undo survived closing, reopening and reloading the save with no save step |
| Upgrade from a classic plan | Yes: imported once, classic file unchanged and archived, "any caster" castings kept as Drafts with no caster |
| Share Transmutation (and with Powerful Change) | Yes, Instant and Animated: the personal transmutation landed on the ally once; slot and Arcane Reservoir charged exactly (Share 1; Share + Powerful Change 2); a plain cast by the same caster afterwards was normal; a casting its source could not fund was refused whole |
| Spell slots, Stop, repeat, Always recast | Yes, Instant and Animated |
| Group spells, Powerful Change, Extend rod, single-use ability pools | Yes (Instant) |
| Shared resources across buffs and rods; all-or-nothing reservation | Yes (Advanced test campaign) |
| Windowed display | No: every 1920x1080 run was fullscreen |
| Important and Short HUD buttons clicked physically | No (they share the moon's code path; source-tested) |
| Quitting to the desktop and restarting the game | No (a save reload and a fresh load from disk were checked) |
| A save failure in the game | No (source-tested with real files) |
| Metamagic spell variants, pets, multi-use ability pools, area change during a run | No |
