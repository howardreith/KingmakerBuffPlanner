# Kingmaker Buff Planner

Local development candidate `0.5.2-controller.1` adds controller execution of
mouse-authored saved routines. It is source-tested with pending native/physical
qualification, not full controller editing. See the
[controller implementation](docs/CONTROLLER-UI-IMPLEMENTATION.md) and
[acceptance matrix](docs/CONTROLLER-UI-ACCEPTANCE.md). It has not been published
or permanently installed; the stable release information below is preserved.

**Release:** `0.5.2` — the casting-first planner: plan every cast explicitly (caster,
exact spell source, target, enhancements) and edit castings directly in the
graph, with autosave and one-click Long / Important / Short routines from the
HUD that never run in combat. See the
[release notes](docs/RELEASE-NOTES-DRAFT.md) and the
[player guide](docs/CASTING-FIRST-PLAYER-GUIDE.md). 0.5.2 keeps the approved
native window and HUD while adding continuous assignment, explicit one-shot
editing, readable Resources accounting, a remembered Require full plan / Cast
available policy, responsiveness instrumentation and ordinary-cased Run labels.

Kingmaker Buff Planner is a standalone Unity Mod Manager mod for **Pathfinder:
Kingmaker Enhanced Plus Edition 2.1.7b**.

It plans and applies party buffs while preserving Kingmaker targeting,
spell-slot, resource, duration, material-component, and metamagic semantics. It
is an independent product with assembly, namespace, UMM ID, profiles,
packaging, and runtime automation owned by this repository.

## Install and use

Download `KingmakerBuffPlanner-0.5.2.zip` from the GitHub Release's **Assets**
section. Do not download GitHub's automatically generated source-code archives.

Back up your existing `Mods\KingmakerBuffPlanner` folder (including
`UserSettings`) outside `Mods` first. Then install the ZIP through Unity Mod
Manager, or extract its single `KingmakerBuffPlanner` directory into
Kingmaker's `Mods` directory so the final layout includes:

```text
Mods\KingmakerBuffPlanner\Info.json
Mods\KingmakerBuffPlanner\KingmakerBuffPlanner.dll
```

Load a campaign, then open the planner with the HUD's gear button, the
spellbook's **BUFF PLANNER** button or Ctrl+Shift+B. Pick a buff, a caster
and its exact source, then click the
recipient: each line is one cast, and every edit saves itself. The HUD's
moon, diamond and sun buttons run the Long, Important and Short routines
with one click. A classic plan from an earlier version is imported once on
first open; the original file is kept unchanged and archived beside it.

Detailed instructions and qualification boundaries are in
[Installation and Use](docs/INSTALLATION-AND-USE.md),
[Qualification](docs/QUALIFICATION.md), and
[Manual Acceptance](docs/MANUAL-ACCEPTANCE.md).

## Features

- Structural native and optional-mod buff discovery.
- Casting-first planning: each casting is one cast with its own caster, exact
  spell source, target or group origin, and enhancements, shown as a
  continuous caster -> casting -> recipient graph.
- Autosave persistence (no Save or Accept step) with explained recovery when a
  save cannot be written.
- One-click Long, Important, and Short routines from the HUD (a row of the
  game's own button panel), with Stop; each run's result in the game log.
- Plan-wide resource accounting (spell slots, rod uses, class-feature pools)
  with all-or-nothing reservation.
- Instant (default) and Animated execution engines.
- Metamagic rods and Brown-Fur Powerful Change / Share Transmutation (via the
  KingmakerGunslinger mod, fail-soft when absent), offered only where they are
  meaningful.
- Right-click for the game's own spell description window.
- Drawn with the game's own fonts, paper, frames and controls, and closed by
  any other game window like the inventory or the spellbook.
- One-time import of classic plans, with the original archived.
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

The final switch is required only while the repository is private. This
repository is currently PUBLIC, so a published release is downloadable by
anyone; only a DRAFT release (never published) stays visible to the
repository's collaborators alone, which is how private previews are shared.

Runtime qualification may be launched only through the project-owned guarded
harness documented in
[Windows Autonomous Runtime Testing](docs/WINDOWS-AUTONOMOUS-RUNTIME-TESTING.md).
Never deploy by directly replacing the complete live `Mods` tree.
