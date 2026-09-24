# Kingmaker Buff Planner 0.2.0-rc3 — Casting-first planner (release candidate)

**This is a local release candidate for the owner's final review, not a
public release.** Nothing is published, tagged or permanently installed
before that review. The classic planner stays the default; the
casting-first planner is an opt-in, experimental mode.

## What changed since 0.2.0-rc2

- **The classic planner is now checked in the game.** Its Long routine,
  authored with the Classic screen's own controls, cast the Resistance
  cantrip through the HUD's routine button, in both animated and instant
  mode. The cast went through the caster's at-will cantrip ability (the
  evidence records which one). The effect reached the target, the at-will
  count stayed unlimited, and no spell slot or ability pool changed.
- **A failed cast stops a classic routine.** A classic routine now casts
  one step at a time and stops at the first cast that is not confirmed;
  the rest are reported as not attempted. Disabling the mod or leaving
  the area during a classic routine ends it the same way a casting-first
  run ends: the cast in progress is interrupted and cleaned up.
- **Free stays free, paid stays paid.** The cast follows the budget the
  plan reserved:
  - A cantrip planned as free is cast only through the at-will ability.
    If that ability is gone, the casting is refused. It never spends a
    level-0 slot instead.
  - A level-0 spell planned against a slot never turns into a free cast.
  - An ability granted by a feature is cast from the same kind of source
    (free or a resource pool) and the same pool the plan reserved.
  - A cantrip the planner cannot attribute to one class ability (two
    classes at different caster levels) is not offered, neither as a
    spellbook cantrip nor as a class ability, rather than being priced as a
    slot or cast at a guessed level.
  - When the game cannot report a source's use count before and after a
    cast, the cast is treated as uncertain and the routine stops; a free
    source must also read as unlimited on both sides. A count the game
    could not report is never taken as "nothing spent".
- **Files the planner cannot read are never overwritten.** A plan, review
  or mode file from a newer planner, or one it cannot read, is kept
  byte-for-byte. Save and Accept say so in plain words ("Not saved: …
  It was left unchanged").
- **Test harness (not player-facing).** A test run's approval now names
  the exact mod set, save and purpose it is for. The launcher reads each
  run's evidence itself before accepting a pass. The disable step keeps
  the planner disabled for several frames, the way the mod manager's
  toggle does. Builds from different checkouts are now byte-identical.

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

Every guard added since rc2 has a mutant that the tests catch. Mutants
show that the tests would notice a broken guard; they are not evidence
of behavior in the game.

**Native gameplay.** These are guarded runs on the disposable automation
campaign, never an ordinary save. Each run checked in game that it had
loaded the build it staged (commit, package, DLL and MVID). The receipt
(`docs/evidence/rc-0.2.0-rc3-receipt.md`, added after the freeze) lists
the runs repeated on the frozen candidate itself. These development runs
checked the changes first:

| Run | What it showed |
| --- | --- |
| `classic-cast-20260924-00aca73-inst-01` | Classic planner, instant: the Long routine cast Resistance (Linzi on Hedwirg) through the at-will ability; new effect; count -1 → -1; spell levels unchanged |
| `classic-cast-20260924-da0ee32-anim-02` | Classic planner, animated: the same, through the game's own cast command |
| `casting-qual-cast-20260924-13f6d37-anim-01` | Casting-first, animated: stop, complete, repeat, recast, disable during a cast, then a new run after the enable that completed, with the planner's subscriptions and HUD unchanged |
| `casting-qual-cast-20260924-13f6d37-inst-01` | Casting-first, instant: the same steps; the disable landed before the run's first step (disable before start, not an interruption of a cast) |

**Restoration:** every run moved the owner's Mods folder aside, staged
only the candidate and the approved mod set, and restored the folder
byte-exact afterwards, including the installed KingmakerGunslinger.
Every save was compared before and after.

**Manual acceptance:** not yet. The consolidated supervised session is
described in `docs/MANUAL-USABILITY-HANDOFF.md`.

**Not yet checked in the game:**
- spells that spend prepared slots or spontaneous levels;
- ability pools;
- group spells, metamagic variants and rods;
- an area change during a run (the test campaign's only exit autosaves
  and ends the prologue);
- pets;
- keyboard and mouse input from a person, and layouts at resolutions
  other than 1920×1200 (they need a connected desktop session).

The finite-resource and group qualification is prepared and waits for an
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
.\scripts\Install-Local.ps1 -ReleaseManifestPath .\artifacts\release\0.2.0-rc3\release-manifest.json `
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
