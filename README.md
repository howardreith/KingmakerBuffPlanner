# Kingmaker Buff Planner

Standalone Unity Mod Manager mod for **Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b**.

Kingmaker Buff Planner provides a BubbleBuffs-style workflow for discovering, configuring, planning, and applying party buffs while preserving Kingmaker targeting, spell-slot, resource, duration, and material-component semantics. It is an independent product with assembly, namespace, UMM ID, profiles, packaging, and runtime automation owned by this repository.

## Install and use

Extract the validated release ZIP into Kingmaker's `Mods` directory, producing `Mods\KingmakerBuffPlanner\Info.json`. Load a campaign, then press F10 or use the lower-left `Buff Planner (F10)` button. Configure Long, Important, and Short routines in the setup window and preview resource/target diagnostics before running them.

Detailed instructions and qualification boundaries are in [Installation and Use](docs/INSTALLATION-AND-USE.md), [Qualification](docs/QUALIFICATION.md), and [Manual Acceptance](docs/MANUAL-ACCEPTANCE.md).

## Product boundary

This repository is completely independent from Kingmaker Gunslinger and Tabletop Added Rules. It does not compile against or require them. Call of the Wild and other gameplay mods are optional, read-only compatibility inputs discovered after load; no third-party mod payload is bundled.

Profiles live under the mod's `UserSettings` directory and are not written into Kingmaker saves. Removing the standalone mod does not create a save dependency.

## Build and qualification

The project targets .NET Framework 4.7 and C# 7.3 against exact locally installed Kingmaker/Unity/UMM references with Copy Local disabled.

```powershell
.\scripts\Test-SourceOnly.ps1
.\scripts\Build-Release.ps1
```

Runtime qualification may be launched only through the project-owned guarded harness documented in [Windows Autonomous Runtime Testing](docs/WINDOWS-AUTONOMOUS-RUNTIME-TESTING.md). Never deploy by directly replacing the live `Mods` tree.

Development remains on `codex/kingmaker-buff-planner`. Do not merge to `main` or publish a public release without separate authorization.
