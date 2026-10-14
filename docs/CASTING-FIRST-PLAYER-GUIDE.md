# Buff Planner: player guide (0.5.1-rc1 local candidate)

This guide describes the casting-first planner including the local 0.5.1-rc1
human-feedback candidate. The published release remains 0.5.0, and the exact
0.5.1-rc1 package has not yet been run in Kingmaker. It is
the only normal planner: there is
no Save button, no Accept Plan step and no Classic switch in the planner.
"What has been checked in the game" at the end says exactly which parts a
guarded in-game run has observed at this build, and which have not.

## Opening the planner

- **Ctrl+Shift+B**, or the HUD's **Setup (gear)** button, opens the
  planner. The mod's page in Unity Mod Manager (Ctrl+F10) is a normal entry
  point too and names both. The four HUD buttons are a row of the game's own
  button panel, above its first four columns (0.5.0).
- In the spellbook, **BUFF PLANNER** beside **METAMAGIC** closes the
  spellbook and opens the planner. It is there for every character,
  including one who cannot cast or has no Metamagic (0.5.0).
- The planner is one of the game's windows (0.5.0): opening the inventory,
  the character sheet, the spellbook or another game window - by its key,
  its HUD button or its tab - closes the planner and shows that window.
  Reopening the planner returns to the same buff and plan. Typing in the
  planner's search never triggers the game's keys.
- A game that still uses the Classic planner (chosen in an earlier version)
  keeps it until you choose **Switch to the casting-first planner** on the
  mod's settings page. The switch is one way: there is no route back to
  Classic inside the game. Your classic plan file is never changed.
- New and unset profiles start in the casting-first planner with **Instant**
  casting. An explicit **Animated** choice from an earlier version is kept.
- The planner opens on the game's own aged page over its table and plays the
  game's paper sound once, as the character sheet and the journal do (0.4.2).

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
   Abilities / Other tabs). Spells with the same effect share one buff; every
   class ability is its own buff (0.4.2), so the Ninja's Shadow Clone is
   listed under Abilities beside, not inside, Mirror Image.
2. Pick a caster in the left lane. When the caster can cast the buff in
   more than one way, pick the exact source row (spellbook and level, item
   or ability); each row says what the whole plan leaves of that pool. A
   normal caster/source click always chooses the source for the **next**
   assignment, even while you are inspecting an existing casting.
3. Click a legal recipient on the right to add one casting. Keep clicking
   other recipients to add more castings with the same source and draft
   options; no Done step is required. Clicking a recipient already assigned
   this exact buff/variant in this routine removes that assignment, and Undo
   restores it. Two allies are two castings. A group/area spell remains one
   casting at its authored origin; clicking a covered member does not create
   a duplicate group cast.
4. Click a casting card to inspect its individual enhancements, recast policy,
   order and Remove control. Inspection does not change what ordinary
   caster/source or recipient clicks mean. To edit that one casting, choose
   **Change recipient** or **Change caster/source**, then make the one intended
   choice; the edit control turns itself off after success. **Done** or Escape
   leaves inspection, but is not part of normal multi-recipient assignment.
5. The **Casting options** panel on the right (0.5.1-rc1) shows, in order:
   **Done**; who casts it on whom ("Linzi » Hedwirg") and from which
   source; explicit **Change recipient** and **Change caster/source** controls;
   its state ("Ready", "Active · Will skip", "Active · Will
   recast", or why it is not ready); **Recast if active**; its
   **Enhancements**, each with what it does, its source and what is left
   (one that cannot be used says why); what it spends ("If cast" when the
   run will skip it, because it spends nothing then) with **Resource
   details**; **Cast earlier** / **Cast later**; and **Remove casting**.
   With nothing selected it shows **New casting**: only the next step, and
   **Recast if active** for the castings you add next. To move a casting to
   another routine, select that routine above, add it there and remove the
   old one.
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
target, then click each ally you want. Share stays selected while the buff,
caster, exact provider and routine remain unchanged, so one batch can add
several recipients. It clears when any part of that draft context changes;
choices already saved on earlier castings do not change. The choice appears
only for a caster and source that can use it; a saved casting whose Share no longer applies keeps
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

## When a spell leaves a spellbook

If you remove a spell from a caster's spellbook, the castings that depended
on it are removed too (0.4.2). Removing means retraining it away for a
spontaneous caster, or un-preparing the last prepared copy for a prepared
caster. This happens the next time you open the planner or press a routine,
with the spellbook closed. The edit saves itself, and the footer says what
it did, for example: "Removed 2 Mind Blank castings: no longer prepared in
Felix's and Leinna's spellbooks. Undo available." **Undo** brings them back,
and they then stay until that spellbook changes again. The plan from before
the first such removal is kept beside it (`*.pre-spellbook-removal`).

Nothing is ever removed for:
- a spent slot or a caster with no uses left (the spell is still in the book);
- a caster who is dead, unconscious or out of the party;
- loading;
- a spellbook the planner could not read with certainty;
- combat, or a running routine.

Another prepared copy of the spell keeps its castings.

## Reading a spell's full description

Right-click a buff in the list, the selected buff's header, or a casting
card: the game's own spell description window opens over the planner, the
same window the spellbook opens (0.5.0). A casting shows its caster's own
spell; a buff with no caster and source chosen shows the base spell. Long
descriptions scroll in the window. **Escape** (or the window's close
button) closes it first, then the selected casting, then the planner; the
planner's Escape never opens the game's own menu. Reading descriptions
never changes the plan.

The planner is drawn with the game's own fonts, paper, frames, buttons and
checkboxes, borrowed from the running game (no game art ships with the
mod); if any of them cannot be found, the planner falls back to the
earlier look for that part.

## Running a routine

- On the HUD, the **moon** runs **Long**, the **diamond** runs
  **Important** and the **sun** runs **Short** - one left-click each. The
  planner does not open. The planner's own **Run** button runs the routine
  selected in the planner (the planner closes while the party casts).
- Every route uses the same checks: the latest saved plan, the party as it
  is at that moment, and every casting re-checked before it is cast.
  Nothing runs on a stale plan and no routine needs an acceptance step.
- **Unavailable castings (0.5.1-rc1).** The footer policy starts at
  **Require full plan** for old and new profiles: one blocked in-scope casting
  refuses the whole routine and names the reason. Choose **Cast available**
  to run the valid, affordable subset in authored order and list every
  omission. The choice is remembered for this profile and is used by both
  planner Run and the HUD routine buttons. It never deletes assignments,
  waives a required enhancement or bypasses combat, save, import, identity,
  session-lock or dispatch safeguards.
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
- **Already active.** With **Recast if active** unticked (the default) a
  casting is skipped only when its recipient already has an effect at least
  as good (the card says "Active · Will skip"); a weaker or expiring effect
  is recast ("Active · Will recast"), and so is any casting with **Recast
  if active** ticked.
- **Stopping.** Press the running routine's HUD button again: the cast in
  progress finishes and nothing after it starts. Changing area, disabling
  the mod or closing the game stop a run at once.
- **Results (0.5.0).** Each run adds one line to the game's own log, for
  example "Buff Planner: Long done - 3 of 3 cast, 1 already active.";
  hover the line for the full result. A HUD press that is refused while the
  planner is closed is logged the same way; a refusal you resolve in the
  planner opens it on that routine with the reason. The game still logs
  each cast itself. The full per-casting record is in the Unity Mod Manager
  log (`[KBP-CF-RUN]`).
- **The footer (0.5.1-rc1)** shows the save state, one line saying what **Run**
  will do now or exactly why it cannot (for example "Long cannot run: 1
  casting is not ready (marked in red)."), and **Resources**, the availability
  policy, **Mode**, **Undo** and **Run Long** / **Run Important** / **Run Short**.
  **Resources** opens an opaque, scrollable parchment table. Choose the selected
  routine or all routines without rest; rows are grouped by caster/shared owner
  and show Available now, Saved need, Would spend and Short under the current
  policy. Close it with Close, Escape or by opening a peer window.

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
then confirm Unity Mod Manager lists the version you intended: 0.5.0 for the
published release, or 0.5.1-rc1 for this explicitly selected local candidate.
Its log line
`[KBP-BOOT] Main.Load exited;version=<version>;commit=<commit>` names the
source commit. To roll back, copy the new `UserSettings` outside `Mods`,
delete the new folder and copy your backup back. The release notes give the
exact steps and the package checksum.

## What has been checked in the game

0.5.1-rc1 (`a4c4342`, local candidate): **not run in Kingmaker**. Focused
source/build/behavior checks and the local package pass, including the exact
Seamantle and Resinous Skin/Share sequences. The guarded observer is prepared
to capture Resources, continuous assignment, policy and Run labels at the
owner's 1920x1200 setting and 1280x720, but the mandatory purity prerequisite
is estimated at about 101 minutes for filesystem inventories alone and was not
started under the ordinary budget. See the
[exact receipt and remaining batch](HUMAN-FEEDBACK-REFINEMENT-0.5.1-RC1.md).

0.5.0 (guarded runs `kbpnui-*` on 2026-10-10/11 on the disposable automation
campaign only, at the session display (1920x1200, later 1920x1080) and at
1280x720; full record with run ids, builds and captures in
`docs/QUALIFICATION.md`):

| What | Checked in the game |
| --- | --- |
| HUD row and frame | Yes: four cells on the native panel's first four columns, the frame enclosing them, the old rail hidden; idle and hover use the game's own cell art; tooltips unchanged |
| Spellbook pair | Yes: METAMAGIC and BUFF PLANNER at one height and baseline for two casters; BUFF PLANNER alone for a character who cannot cast; it opens the planner |
| Spell description | Yes: the game's own window from a right-click, drawn above the planner; Escape closes it, then the selection, then the planner; the plan unchanged |
| Other windows | Yes: the inventory, character sheet and spellbook each closed the planner and showed; pause and selection unchanged; reopening kept the buff |
| Typing in the search | Yes: with the search focused, the game's own key handler stands down |
| Save failure | Yes: a plan file held by another program showed "Not saved" with Retry save; Retry save saved; Undo restored the plan |
| Planning | Yes, without moving the mouse: select, move, Cast later, Done, add, change caster and remove, each undone; the plan ended exactly as it started |
| Game log | Yes: a refused Long press added one line; the routines of the guarded casting run added their result lines |
| Physical mouse and keyboard | No: the checks press the same controls without moving the mouse or pressing keys |
| Remapped keys, prepared casters, pets, metamagic variants | No |

0.4.3 (the owner-accepted 0.4.2 build `7a0842f` - 0.4.3 adds only the version
number and documentation - guarded runs `kbp042-*` on 2026-10-09/10, disposable
test campaigns only; full record in `docs/OWNER-FEEDBACK-0.4.2-HANDOFF.md`):

| What | Checked in the game |
| --- | --- |
| Aged page, table and ink | Yes at 1920x1200 (`kbp042-paper-01`): the game's own page (`Card_Big`) and table resolved and drew untinted; the inventory, character sheet and map were captured in the same session for comparison; the spell description is on the same page |
| Opening sound | The game's paper sound was posted once per open: 1/1 on opening, still 1/1 after two refreshes and the spell description, 2/2 and 3/3 after two close-and-reopen cycles. Whether it is audible: the owner's check |
| Castings of a spell removed from a spellbook | Yes (`kbp042-removal-05`, repeated on the final build as `kbp042-removal-06`, without mouse input): the game's own removal took Resistance out of Tartuccio's book; exactly its two castings were removed, saved and archived, the planner showed "Removed 2 Resistance castings: no longer known in Tartuccio's spellbook. Undo available.", and Linzi's casting of the same spell stayed |
| Spent slots keep castings | Yes: every level-1 slot of Linzi's book was spent with the game's own spend; her castings stayed and no resource changed when the routine was pressed (her planned spells are cantrips, so a buff whose own uses run out is checked in code only) |
| Unrelated problems are not waived | Yes: an unrelated Draft casting stayed and still blocked; the Long routine evaluated only what was left |
| Un-preparing a prepared spell | No: no mutable test party has a prepared caster (proven against the game's own spellbook code) |
| Shadow Clone | No: no test party has a ninja. Its card, legality and plan are proven against the ability's own native blueprint |

0.4.1 (the owner-accepted 0.4.0 candidate rc4 `009b6dd` - 0.4.1 adds only the version
number and documentation - guarded runs `kbp040-rc4-*` on 2026-10-09, disposable test
campaigns only; full record in `docs/REMAINING-WORK-0.4.0-HANDOFF.md`):

| What | Checked in the game |
| --- | --- |
| Spellbook Buff Planner button | Yes: opened the planner three times from the native spellbook, closed with Escape; a simulated failure reopened the spellbook |
| Routines during combat | Yes, Instant and Animated, casting-first and Classic: refused with "Buff routines cannot run during combat.", nothing spent, saved or opened; the next press after combat behaved normally |
| Direct graph editing | Yes (mouse): add, same-portrait remove, move, change caster, Undo, Escape leaves the selection then closes; edits saved themselves. Group re-centring and Share visibility: not in the test party (checked in code) |
| Magic Circle against Alignment | Yes, Instant and Animated: cast on an ally, slot spent once, circle confirmed, nothing left held |
| Buff catalogue | Yes: catalogue exports before/after for three mod profiles; Hideous Laughter, the Heal skill's Treat Affliction / Treat Deadly Wounds and Inspiring Recovery no longer listed; Targeted Bomb Admixture, Light, Daylight, Elemental Bastion and Call of the Wild's hex wards listed; every removal adjudicated in `planning/CATALOG-AUDIT-0.4.0.md` |
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
