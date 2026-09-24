# Kingmaker Buff Planner 0.2.0-rc4 — Casting-first planner (release candidate)

**This is a local release candidate for the owner's final review, not a
public release.** Nothing is published, tagged or permanently installed
before that review. The classic planner stays the default; the
casting-first planner is an opt-in, experimental mode.

## What changed since 0.2.0-rc3

rc3's final independent review found defects, and rc4 fixes them. For a
player:

- **Classic routines no longer give up while the game is paused.** A
  classic routine now advances only while the game runs. **APPLY** in the
  open classic planner starts casting once you close the planner (the
  planner pauses the game), and the planner says so. In rc3 a paused game
  made the first cast look unconfirmed, and the rest of the routine was
  abandoned.
- **Plans saved by 0.0.19 import.** The first time the casting-first
  planner opens a campaign, a classic plan saved by the released 0.0.19
  (or older) is imported through the classic planner's own reading. Its
  file is not rewritten. rc3 refused such plans.
- **A cast counts only if it landed.** A cast is confirmed only when this
  cast put the effect there: a new effect, or the old one renewed to a
  later end. An effect that was already there, or one the game
  suppresses, never confirms a cast. A group casting that would reach
  nobody, or whose caster cannot be its origin, is blocked with a reason
  instead of running.
- **Every casting can be fully edited.** Editing one casting now changes
  its caster and exact source (the spellbook level, item or ability), its
  routine and its place in it, and whether it is cast again when the buff
  is already there. An imported casting whose classic plan let the
  planner pick any caster gets its caster here and can be marked Ready in
  place. A caster who can cast a buff in more than one way picks the
  exact one when adding it. The animated fallback for Instant mode has a
  switch.
- **Unreadable files are announced.** Both planners say when their saved
  file could not be read or comes from a newer version, when a backup was
  loaded instead, and when a change was not saved.
- **Smaller fixes.** The casting-first planner never rewrites the classic
  plan file. The spellbook button opens the casting-first planner
  properly. The HUD tooltip never describes another campaign. A new
  casting never reuses a removed casting's number. The footer says when
  all routines together would run short of a resource. A linked
  opposition-school slot pair counts as two slots in the budget.
- **Test harness (not player-facing).** A changed ordinary save is
  reported with every other failure and blocks later test runs until the
  owner has reviewed it. The save comparison survives a launcher that
  stops early. A live test run stops itself before its time limit and on
  the launcher's abort signal. Finite-resource test approvals can be
  written. A rehearsal of the manual session is labelled as one. More of
  the game's own checks run before a test launches.

## What changed in 0.2.0-rc3 (history)

The classic planner was checked in the game in both casting modes; a
classic routine stops at the first unconfirmed cast; free sources stay
free and paid ones stay paid (the plan's reservation decides the route,
an unreadable count is uncertainty); files the planner cannot read are
never overwritten; test approvals name their exact mod set, save and
purpose; builds are byte-identical across checkouts. rc3 was superseded
before release (`docs/evidence/rc-0.2.0-rc3-receipt.md`).

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
- **Edit any one casting.** Change who casts it and from which spellbook
  level, item or ability, its routine and its place in it, whether it is
  cast again when the buff is already there, its target or group origin,
  and its enhancements. An imported casting gets its caster in place.
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
(`docs/evidence/rc-0.2.0-rc4-receipt.md`, added after the freeze) lists
the runs made on the frozen rc4 candidate itself: the Classic Long
routine in both casting modes, the casting-first zero-cost qualification
in both modes (stop, complete, repeat, recast, disable, recover), the
first-open import of a genuine 0.0.19 (schema 4) plan, and the finite
selection on this party. The rc3 receipt keeps the runs made on rc3.

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
- keyboard and mouse input from a person, and any check that judges game
  frames (the workspace layout at 1920×1200 and 1920×1080, the in-game
  reload, the manual-session rehearsal): game frames render black while
  the owner's remote session is disconnected, so these wait for a
  connected session.

The finite-resource and group qualification is prepared and waits for an
owner-designated advanced test save (`docs/ADVANCED-SEED-COMPATIBILITY.md`):
the automation party's finite spell levels hold no buff.

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
.\scripts\Install-Local.ps1 -ReleaseManifestPath .\artifacts\release\0.2.0-rc4\release-manifest.json `
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
