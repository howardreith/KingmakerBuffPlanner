# Kingmaker Buff Planner

Standalone Unity Mod Manager mod for **Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b**.

The project will provide a BubbleBuffs-style interface for discovering, configuring, planning, and applying party buffs while preserving Kingmaker's real targeting, spell-slot, resource, duration, and material-component semantics.

## Product boundary

This repository is completely independent from Kingmaker Gunslinger and the broader Tabletop Added Rules mod. It must not compile against them, share their UMM identity, or make them runtime dependencies.

Optional compatibility is dynamic and fail-soft: ordinary Kingmaker blueprint/action patterns from Call of the Wild, Tabletop Added Rules, and other mods should be discovered structurally after those mods load; exceptional custom mechanics may receive isolated adapters or versioned GUID overrides.

## Expected desktop lab

```text
C:\Dev\KingmakerBuffPlannerLab
```

Expected checkout:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

Read before substantive work:

```text
AGENTS.md
planning/CODEX-KINGMAKER-BUFF-PLANNER-AUTONOMOUS-MISSION.md
docs/KINGMAKER-BUFF-PLANNER-DESKTOP-SETUP.md
```

## Public repository versus private transfer

This repository contains only public-safe project instructions, templates, and transfer tooling.

Do **not** commit:

- installed game or Unity DLLs;
- Unity Mod Manager binaries;
- third-party mod packages or compiled mod DLLs;
- saves;
- credentials or Codex authentication data;
- machine-local paths and environment fingerprints;
- runtime evidence or backups.

Use these scripts instead:

```text
scripts/New-KingmakerBuffPlannerPrivateTransfer.ps1
scripts/Import-KingmakerBuffPlannerPrivateTransfer.ps1
scripts/Initialize-KingmakerBuffPlannerDesktopCheckout.ps1
```

The laptop exporter creates a SHA-256-manifested private ZIP. The desktop importer verifies every file before placing immutable references beneath the standalone lab.

## Development branch

Codex development belongs on:

```text
codex/kingmaker-buff-planner
```

No merge to `main` and no public release should occur autonomously.
