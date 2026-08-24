# Kingmaker Buff Planner

**Release status:** `0.0.11` is the current published GitHub release. A `0.0.12`
HUD-lifecycle hotfix is being prepared on a separate branch and is not published.

Kingmaker Buff Planner is a standalone Unity Mod Manager mod for **Pathfinder:
Kingmaker Enhanced Plus Edition 2.1.7b**.

It provides a BubbleBuffs-style workflow for discovering, configuring,
planning, and applying party buffs while preserving Kingmaker targeting,
spell-slot, resource, duration, material-component, and metamagic semantics. It
is an independent product with assembly, namespace, UMM ID, profiles,
packaging, and runtime automation owned by this repository.

## Install and use

Download `KingmakerBuffPlanner-0.0.11.zip` from the GitHub Release's **Assets**
section. Do not download GitHub's automatically generated source-code archives.

Install the ZIP through Unity Mod Manager, or extract its single
`KingmakerBuffPlanner` directory into Kingmaker's `Mods` directory so the final
layout includes:

```text
Mods\KingmakerBuffPlanner\Info.json
Mods\KingmakerBuffPlanner\KingmakerBuffPlanner.dll
```

Load a campaign, then use Ctrl+Shift+B or the lower-left planner controls.
Configure Long, Important, and Short routines in the setup window and preview
resource and target diagnostics before running them.

Detailed instructions and qualification boundaries are in
[Installation and Use](docs/INSTALLATION-AND-USE.md),
[Qualification](docs/QUALIFICATION.md), and
[Manual Acceptance](docs/MANUAL-ACCEPTANCE.md).

## Features

- Structural native and optional-mod buff discovery.
- Long, Important, and Short routine configuration.
- Direct portrait assignment and deterministic resource-aware planning.
- Animated and Instant execution engines.
- Provider consolidation with automatic caster/resource selection.
- Metamagic-rod discovery and a visible enhancement chooser.
- Four-column vertical catalog with real blueprint icons, search, categories,
  and routine-local **Selected only**.
- Player-facing selected, covered, unavailable, invalid, and neutral target
  states.
- External profile persistence with no save-owned mod content.
- Optional, read-only Call of the Wild and gameplay-mod compatibility inputs.

## Product boundary

This repository is completely independent from Kingmaker Gunslinger and
Tabletop Added Rules. It does not compile against or require them. Call of the
Wild and other gameplay mods are optional, read-only compatibility inputs
discovered after load; no third-party mod payload is bundled.

Profiles live under the mod's UserSettings directory and are not written into
Kingmaker saves. Removing the standalone mod does not create a missing-content
save dependency.

## Build, package, and release

The project targets .NET Framework 4.7 and C# 7.3 against exact locally
installed Kingmaker, Unity, and UMM references with Copy Local disabled.

```powershell
.\scripts\Test-SourceOnly.ps1
.\scripts\Build-Release.ps1
```

The guarded GitHub publisher rebuilds twice, rejects non-deterministic output,
validates the exact UMM ZIP, creates the version tag, writes checksums, and
uploads release assets:

```powershell
.\scripts\Publish-Release.ps1 `
  -Publish `
  -ConfirmHumanAcceptance `
  -AllowPrivateRepositoryRelease
```

The final switch acknowledges that this repository is currently private. A
private-repository release is downloadable only by authorized GitHub users.

Runtime qualification may be launched only through the project-owned guarded
harness documented in
[Windows Autonomous Runtime Testing](docs/WINDOWS-AUTONOMOUS-RUNTIME-TESTING.md).
Never deploy by directly replacing the complete live `Mods` tree.
