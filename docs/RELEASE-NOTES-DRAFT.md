# Kingmaker Buff Planner 0.2.0-rc2 — Casting-first planner (release candidate)

**This is a local release candidate for the owner's final review, not a
public release.** Nothing is published, tagged or permanently installed
before that review. The classic planner stays the default; the
casting-first planner is an opt-in, experimental mode.

## What changed since 0.2.0-rc1

- **Cantrips now cast in animated mode.** In rc1, a cantrip from a
  spontaneous caster's spellbook (for example Resistance cast by a bard or
  a sorcerer) failed when cast with animation. The planner gave the game
  the spellbook's level-0 entry. The game casts that entry only with a
  level-0 spell slot, and these classes have none. Kingmaker casts
  cantrips at will through the ability each class grants for them, and
  the planner now does the same in both casting modes. The fix is in the
  casting code both planners share; the classic planner was not
  separately checked in the game.
- **Nothing the game would refuse is sent.** Before every cast, the
  planner asks the game the same availability question the game's own
  cast command asks. rc1's instant mode cast the spellbook entry anyway,
  because the instant cast skips that check. It now uses the at-will
  ability too.
- **Level-0 spells without the at-will ability are budgeted.** Such a
  spell spends a level-0 slot, or a memorized slot that the cast uses up,
  as the game does. Only a cantrip backed by the at-will ability is free.
- **Now checked in the game:**
  - Animated casting, the default mode.
  - Stopping a routine: the player's press during a cast in progress. The
    cast finished and nothing after it started.
  - Disabling the planner during a run. The cast in progress was
    interrupted and nothing landed, and runs were possible again once the
    planner was enabled.
  - Every run went through the planner's own per-frame execution.

## What it lets a player do

- **Choose the planner mode** in the Unity Mod Manager panel: Classic
  (default) or Casting-first (experimental). The choice is saved in
  `UserSettings/planner-mode.json`.
- **Plan buffs one casting at a time.** The workspace follows the Bubble
  Buffs layout on Kingmaker's own book and parchment:
  - a searchable icon grid of buffs, with Spells / Abilities / Other tabs;
  - routine tabs (Long, Important, Short) with counts;
  - the selected buff's castings as cards, and an inspector.

  Each card is exactly one cast and can be edited on its own. It shows
  the caster and the recipient or group origin, its source, enhancements,
  cost, readiness and routine. Nothing is expanded, merged or substituted.
- **See costs and refusals in plain words.** A cost names whose resource
  it uses and what kind ("Linzi: level 2 spell slot", "Hedwirg: free").
  The header shows routine-wide readiness. A refusal says what to do next
  ("choose who receives this casting first").
- **Review, accept, then run.** Accept Plan records the routine's
  contents. Review & Apply, the HUD routine buttons, the hotkey and the
  spellbook button all run through one production host. That host:
  - re-reads the party first;
  - skips buffs already active, and recasts weaker, expiring or
    unprovable ones;
  - stops at the first failed or uncertain cast and never retries.

  Pressing a routine button while a routine runs stops it after the cast
  in progress.
- **Keep the classic plan.** On first open, a campaign's classic plan is
  imported into a separate casting-first plan. The classic file is never
  modified, and a byte-exact copy is archived.

See `docs/CASTING-FIRST-PLAYER-GUIDE.md` for the full guide.

## What was checked, by evidence layer

**Source tests** (Windows PowerShell 5.1, non-interactive, at the
candidate commit; the release-candidate receipt has the counts) cover:
- source validation and protocol tests;
- the runtime-harness filesystem;
- deployment WhatIf and launcher WhatIf;
- fixture inventory;
- install rollback in isolated state;
- the guarded publisher gate.

Every guard added since rc1 has a mutant that the tests catch.

**Native gameplay.** These are guarded runs on the disposable automation
campaign, never an ordinary save. Each run checked in game that it had
loaded the build it staged (commit, package, DLL and MVID). The receipt
(`docs/evidence/rc-0.2.0-rc2-receipt.md`, added after the freeze) lists
the runs repeated on the frozen candidate itself. These development runs
found and then confirmed the change:

| Run | What it showed |
| --- | --- |
| `casting-qual-cast-20260923-a1-anim-01` (development build `9f2bdf6`) | Animated mode, before the fix. The player's stop landed during the first cast, as designed. The game's own cast command then failed that cantrip, and the routine halted: the failed casting was reported, nothing was spent, and the other two castings were not attempted. |
| `casting-qual-select-20260923-d1-01` | The game's own view of each caster's level-0 spells: 0 level-0 slots per day, and cantrips usable at will through the class ability |
| `casting-qual-cast-20260923-a2-anim-01` (fixed build `70f135a`) | Animated mode: stop by the player's press during the first cast; complete, skipping the active buff; repeat, with nothing to cast; recast after a reopen; disable during a cast, which was interrupted with nothing landing, then enable. Every step was as forecast, resources were unchanged (at will), and no save was written. |
| `casting-qual-cast-20260923-a2-inst-01` (fixed build `70f135a`) | Instant mode: the same steps. The disable landed before the run's first step, so nothing was submitted. |

**Restoration:** every run moved the owner's Mods folder aside, staged
only the candidate and the approved mod set, and restored the folder
byte-exact afterwards, including the installed KingmakerGunslinger
0.0.136. Every save was compared before and after.

**Manual acceptance:** not yet. The consolidated supervised session is
described in `docs/MANUAL-USABILITY-HANDOFF.md`.

**Not yet checked in the game:**
- spells that spend prepared slots or spontaneous levels;
- ability pools;
- group spells, metamagic variants and rods;
- an area change during a run;
- pets;
- layouts at resolutions other than 1920×1200.

The finite-resource qualification is prepared and waits for an
owner-designated advanced test save (`docs/ADVANCED-SEED-COMPATIBILITY.md`).

## Not supported in this version (shown on the card, refused by Apply)

- Targeting modifiers such as Share Transmutation.
- A specific physical rod or item.
- Group castings whose required recipients are outside the predicted
  area.

## Install and roll back (local, guarded)

Exit Kingmaker and Unity Mod Manager first.

```powershell
# From the candidate's clean checkout:
.\scripts\Build-Release.ps1
.\scripts\Install-Local.ps1 -ReleaseManifestPath .\artifacts\release\0.2.0-rc2\release-manifest.json `
    -InstallId <id> -ExpectedPriorVersion 0.1.1-rc3
# To return to the prior version, keeping settings edited since:
.\scripts\Restore-InstallLocal.ps1 -InstallId <id>
```

The installer:
- verifies the package, DLL and manifest;
- keeps `UserSettings` exactly;
- refuses when any other mod would change;
- rolls itself back if a step fails.

`Restore-InstallLocal` restores the prior planner and keeps newer
settings. A casting-first plan the older version cannot read is moved to
the rollback evidence folder.
