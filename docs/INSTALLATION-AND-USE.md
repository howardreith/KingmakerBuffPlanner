# Installation and Use

## Requirements

- Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b.
- Unity Mod Manager 0.28.2 installed for Kingmaker.
- The release ZIP for Kingmaker Buff Planner. Call of the Wild is optional and is not bundled.

## Install

1. Exit Kingmaker and back up the whole `Mods\KingmakerBuffPlanner` folder (including `UserSettings`) outside `Mods`.
2. Extract the release ZIP into Kingmaker's `Mods` directory. The resulting path must be `Mods\KingmakerBuffPlanner\Info.json`.
3. Start Kingmaker through Steam in the normal way and confirm Unity Mod Manager lists `KingmakerBuffPlanner` once, with the version of the ZIP you installed (0.4.3).
4. Load a campaign. Open the planner with the HUD's gear button or Ctrl+Shift+B.

Do not copy game DLLs, Harmony, Unity Mod Manager, Call of the Wild, or another mod into the Kingmaker Buff Planner folder.

## Plan and run

The planner is the casting-first planner; [the player guide](CASTING-FIRST-PLAYER-GUIDE.md) describes it in full. Each casting is one cast with its own caster, exact spell source, target and enhancements; every edit saves itself. The HUD's moon, diamond and sun buttons run the Long, Important and Short routines with one click, and every run reports what it cast, skipped or refused and why. The first open imports a classic plan from an earlier version once and archives the original.

Profiles are external JSON under `Mods\KingmakerBuffPlanner\UserSettings`. They are keyed to the current campaign and are not written into Kingmaker saves. The repository keeps up to three prior valid backups and never overwrites a plan file it cannot read.

## Update or uninstall

Exit the game before replacing the mod folder. To preserve settings across a manual reinstall, copy `UserSettings` first and restore it only to the same standalone mod folder. To uninstall, remove `Mods\KingmakerBuffPlanner`; Kingmaker saves do not depend on the mod or its external profiles.

## Qualification boundary

0.4.3 is the owner-accepted 0.4.2 build (`7a0842f`) with only the version number and documentation advanced; its source gate, guarded in-game runs and what they did not cover are in `docs/OWNER-FEEDBACK-0.4.2-HANDOFF.md`. 0.4.1 is the owner-accepted 0.4.0 candidate (rc4 `009b6dd`) with only the version number and documentation advanced; its qualification (the full source-only gate and guarded in-game runs on disposable test campaigns, with what each run did and did not cover) is recorded with exact run ids in `docs/REMAINING-WORK-0.4.0-HANDOFF.md`; earlier qualification is in `docs/E01-E27-ACCEPTANCE-MATRIX.md` and `docs/QUALIFICATION.md`. Routines never run during combat, and Instant mode never falls back to an animated cast.
