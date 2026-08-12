# Installation and Use

## Requirements

- Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b.
- Unity Mod Manager 0.28.2 installed for Kingmaker.
- The release ZIP for Kingmaker Buff Planner. Call of the Wild is optional and is not bundled.

## Install

1. Exit Kingmaker.
2. Extract the release ZIP into Kingmaker's `Mods` directory. The resulting path must be `Mods\KingmakerBuffPlanner\Info.json`.
3. Start Kingmaker through Steam in the normal way and confirm Unity Mod Manager lists `KingmakerBuffPlanner` version 0.0.3 once.
4. Load a campaign. Use the lower-left setup icon; F10 is the fallback open/close shortcut.

Do not copy game DLLs, Harmony, Unity Mod Manager, Call of the Wild, or another mod into the Kingmaker Buff Planner folder.

## Configure routines

The setup window exposes Long, Important, and Short routines. Search, filter, and sort discovered sources; choose stable party or pet targets; then set provider priority, bans, and per-routine cast caps. Preview shows planned casts, remaining resources, rejected providers, skipped active effects, unsupported saved sources, and unfulfilled targets before execution.

Profiles are external JSON under `Mods\KingmakerBuffPlanner\UserSettings`. They are keyed to the current campaign and are not written into Kingmaker saves. The repository keeps up to three prior valid backups and recovers conservatively from malformed data.

## Execute

Use the adjacent Long, Important, or Short HUD icon after configuration. Every quick action displays a result or an exact unavailable reason. Animated mode queues native Kingmaker cast commands. Instant mode uses native cast rules and native spend semantics in bounded batches; sticky-touch sources use animated fallback when enabled or fail before submission when fallback is disabled. Queued or submitted is not success: the result panel reports success only after expected effects are confirmed on intended targets, alongside command, resource, component, and exact failure outcomes.

The default combat policy is conservative. Review preview diagnostics and save normally before using any gameplay mod.

## Update or uninstall

Exit the game before replacing the mod folder. To preserve settings across a manual reinstall, copy `UserSettings` first and restore it only to the same standalone mod folder. To uninstall, remove `Mods\KingmakerBuffPlanner`; Kingmaker saves do not depend on the mod or its external profiles.

## Qualification boundary

The standalone UI, native and Call of the Wild catalogs, structural planning, persistence, and executor logic have automated evidence. This desktop had no project-owned `KBP_` save, so real campaign resource/effect equivalence remains deferred. See `docs/QUALIFICATION.md` and `docs/MANUAL-ACCEPTANCE.md` for exact scope.
