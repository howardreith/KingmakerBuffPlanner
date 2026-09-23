# Kingmaker Buff Planner 0.2.0-rc1 — Casting-first planner (release candidate)

**This is a local release candidate for the owner's final review, not a
public release.** Nothing is published, tagged or permanently installed
before that review. The classic planner stays the default and is
unchanged; the casting-first planner is an opt-in, experimental mode.

## What it lets a player do

- **Choose the planner mode** in the Unity Mod Manager panel: Classic
  (default) or Casting-first (experimental). The choice is saved in
  `UserSettings/planner-mode.json`.
- **Plan buffs one casting at a time.** The workspace follows the Bubble
  Buffs layout on Kingmaker's own book and parchment: a searchable icon
  grid of buffs with Spells / Abilities / Other tabs, routine tabs (Long,
  Important, Short) with counts, the selected buff's castings as cards,
  and an inspector. Each card is exactly one cast (caster → recipient or
  group origin, its source, enhancements, cost, readiness and routine)
  and can be edited on its own; nothing is expanded, merged or
  substituted.
- **See costs and refusals in plain words**: whose resource and what kind
  ("Linzi: level 2 spell slot", "Hedwirg: free"), routine-wide readiness
  in the header, and refusals that say what to do next ("choose who
  receives this casting first").
- **Review, accept, then run.** Accept Plan records the routine's
  contents; Review & Apply, the HUD routine buttons, the hotkey and the
  spellbook button all run through one production host that re-reads the
  party first, skips buffs already active (and recasts weaker, expiring
  or unprovable ones), stops at the first failed or uncertain cast, and
  never retries. Pressing the running routine again stops it after the
  cast in progress.
- **Keep the classic plan.** On first open, a campaign's classic plan is
  imported into a separate casting-first plan; the classic file is never
  modified and a byte-exact copy is archived.

See `docs/CASTING-FIRST-PLAYER-GUIDE.md` for the full guide.

## What was checked, by evidence layer

**Source tests** (Windows PowerShell 5.1, non-interactive, at the
candidate commit): source validation, protocol tests (293 at the reload
commit), runtime-harness filesystem, deployment WhatIf, launcher WhatIf,
fixture inventory, install rollback in isolated state and the guarded
publisher gate. Every guard added in this iteration has a mutant the
tests catch.

**Native gameplay** (guarded runs on the disposable automation campaign,
never an ordinary save; the exact candidate build loaded, verified by
commit, package, DLL and MVID):

| Run | What it showed |
| --- | --- |
| `casting-probe-cast-20260923-p2-02` | One Resistance cantrip cast through the one-cast probe: new effect instance, nothing spent, zero violations |
| `casting-qual-cast-20260923-q1-01` | A two-caster Resistance routine through the same Apply a player uses: stop after the first cast, complete (skipping the active buff), repeat (nothing to cast), and a recast after closing and reopening the planner; every step as forecast, no save written |
| `casting-ws-qual-20260923-q2-03`, `casting-ws-qual-20260923-r1-01` | The workspace in game frames: authoring, focused edit, Undo, Save, close and reopen; resource names, refusal text and header as described above |
| `casting-ws-reload-20260923-s1-01` | See the release-candidate receipt: the exact test save loaded again in game, the planner reopened with the saved plan and one set of handlers |

**Restoration:** every run moved the owner's Mods folder aside, staged
only the candidate and the approved mod set, and restored the folder
byte-exact afterwards (including the installed KingmakerGunslinger
0.0.136); every save was compared before and after.

**Manual acceptance:** not yet. The consolidated supervised session is
described in `docs/MANUAL-USABILITY-HANDOFF.md`.

**Not yet checked in the game:** spells that spend prepared slots or
spontaneous levels, ability pools, group spells, metamagic variants and
rods, animated casting mode, area change or disable during a run, pets,
and layouts at resolutions other than 1920×1200. The finite-resource
qualification is prepared and waits for an owner-designated advanced
test save (`docs/ADVANCED-SEED-COMPATIBILITY.md`).

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
.\scripts\Install-Local.ps1 -ReleaseManifestPath .\artifacts\release\0.2.0-rc1\release-manifest.json `
    -InstallId <id> -ExpectedPriorVersion 0.1.1-rc3
# To return to the prior version, keeping settings edited since:
.\scripts\Restore-InstallLocal.ps1 -InstallId <id>
```

The installer verifies the package, DLL and manifest, keeps
`UserSettings` exactly, refuses when any other mod would change, and rolls
itself back if a step fails. `Restore-InstallLocal` restores the prior
planner and keeps newer settings (a casting-first plan the older version
cannot read is moved to the rollback evidence folder).
