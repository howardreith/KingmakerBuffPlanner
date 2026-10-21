# Mr. Bard's Kingmaker Buff Planner

**Release: [0.6.6](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.6).**
This owner-authorized release includes spatial controller navigation, a
persistent focus border, and Inspector text scrolling without snap-back. The
exact stable ZIP has not been run in the game. See the
[release notes](docs/RELEASE-NOTES-0.6.6.md) and
[controller checklist](docs/CONTROLLER-NAVIGATION-REPAIR-2026-10-20.md).

**Controller navigation preview: [0.6.5-alpha.1](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.5-alpha.1).**
See the [alpha notes](docs/RELEASE-NOTES-0.6.5-ALPHA.1.md).

**Release: [0.6.4](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.4).**
This owner-authorized release promotes the controller entry repair tested in
`v0.6.4-alpha.1`. The owner reports that alpha's controller functionality is
usable, with further improvements needed. The exact stable package has not
been run in the game, and detailed spellbook and wider controller acceptance
remain unverified. The 2.1.5 UMM minimum is unchanged; native Linux 2.1.5d
loading and gameplay remain unverified. See the
[release notes](docs/RELEASE-NOTES-0.6.4.md) and
[0.6.3 log investigation](docs/CONTROLLER-ENTRY-0.6.3-LOG-INVESTIGATION.md).

**Experimental [0.6.1-linux.1 prerelease](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.1-linux.1):**
lowers Unity Mod Manager's numeric minimum game version to 2.1.5 for a native
Linux 2.1.5d trial. Native Linux loading and gameplay remain **NOT RUN /
unverified**. See the [candidate and manual test](docs/LINUX-2.1.5D-CANDIDATE.md).
The 0.6.1-linux.1 prerelease and earlier 0.6.0 release remain available.

## Historical releases and controller previews

The version-specific statements below record earlier releases. Prior alphas
and their receipts remain unchanged.

[Controller test Alpha4 **0.5.4-controller.4**](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.4-controller.4)
targets full controller authoring, its
own native menu card and actual workspace rendering repair. No mouse prerequisite
is accepted for this assignment. Source paths exist, but native acceptance is
**NOT_RUN** and the assignment is not complete. Owner-authorized experimental
prerelease, not stable support; [manual notes](docs/CONTROLLER-ALPHA-0.5.4-CONTROLLER.4-NOTES.md),
[capability inventory](docs/CONTROLLER-AUTHORING-CAPABILITIES.md) and
[required acceptance](docs/CONTROLLER-UI-ACCEPTANCE.md) separate source from proof.
Earlier mixed-input/card/editor deferrals below describe the older alphas only.

[Controller test alpha **0.5.4-controller.3**](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.4-controller.3)
includes the same-session Configure
route below, plus review fixes for text-field Escape ordering and long-results
scrolling. This remains an experimental, source-tested build: in-game pointer,
authoring and rendered-layout acceptance are pending. See the
[alpha notes](docs/CONTROLLER-ALPHA-0.5.4-CONTROLLER.3-NOTES.md).

Historical local access/layout repair **0.5.4-controller.2** adds Configure plans into the
actual editor from a controller-started session, with mouse/keyboard authoring,
same-session return and a console-book entry. Essential quick-menu text is no
longer paged. This is source-tested, **not yet native-qualified or published**;
the existing public alpha below is unchanged. See
[implementation](docs/CONTROLLER-UI-IMPLEMENTATION.md) and
[new acceptance gate](docs/CONTROLLER-UI-ACCEPTANCE.md). Native card and full
gamepad graph editing remain deferred.

**Controller Alpha 1:** [download the experimental prerelease](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.4-controller.1).
It reuses the unchanged candidate below for owner testing; native controller
behavior is not yet qualified. Back up the mod folder/UserSettings and saves,
and test entry/Back before casting. Stable 0.5.3 remains Latest.

Local development candidate `0.5.4-controller.1` adds controller execution of
mouse-authored saved routines. It is source-tested with pending native/physical
qualification, not full controller editing. See the
[controller implementation](docs/CONTROLLER-UI-IMPLEMENTATION.md) and
[acceptance matrix](docs/CONTROLLER-UI-ACCEPTANCE.md). It is now published only
as the owner-authorized alpha above, without local installation; stable release
information below is preserved.

**Release:** `0.5.3` (2026-10-15) — the mod's public name and author are now
**Mr. Bard's Kingmaker Buff Planner** by **Mr. Bard**, and it is released under
the [MIT License](LICENSE). It was published as **Kingmaker Buff Planner**
through 0.5.2. The planner code is the 0.5.2 planner: the UMM ID
`KingmakerBuffPlanner`, the `KingmakerBuffPlanner.dll` assembly, the
`Mods\KingmakerBuffPlanner` folder and its `UserSettings` profiles are
unchanged, so 0.5.3 installs over an earlier version as the same Unity Mod
Manager entry. The owner authorized 0.5.3 without a further playtest; it has
not been run in the game.

**Planner (since 0.5.2):** casting-first: plan every cast explicitly (caster,
exact spell source, target, enhancements) and edit castings directly in the
graph, with autosave and one-click Long / Important / Short routines from the
HUD that never run in combat. See the
[release notes](docs/RELEASE-NOTES-DRAFT.md) and the
[player guide](docs/CASTING-FIRST-PLAYER-GUIDE.md). 0.5.2 keeps the approved
native window and HUD while adding continuous assignment, explicit one-shot
editing, readable Resources accounting, a remembered Require full plan / Cast
available policy, responsiveness instrumentation and ordinary-cased Run labels.

Mr. Bard's Kingmaker Buff Planner is a standalone Unity Mod Manager mod built
against **Windows Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b**. The
0.6.6 retains the native Linux 2.1.5d version gate but has not passed a
native Linux trial.

It plans and applies party buffs while preserving Kingmaker targeting,
spell-slot, resource, duration, material-component, and metamagic semantics. It
is an independent product with assembly, namespace, UMM ID, profiles,
packaging, and runtime automation owned by this repository.

## Install and use

Download `KingmakerBuffPlanner-0.6.6.zip` from the GitHub Release's **Assets**
section. Do not download GitHub's automatically generated source-code archives.

Back up your existing `Mods\KingmakerBuffPlanner` folder (including
`UserSettings`) outside `Mods` first. Then install the ZIP through Unity Mod
Manager, or extract its single `KingmakerBuffPlanner` directory into
Kingmaker's `Mods` directory so the final layout includes:

```text
Mods\KingmakerBuffPlanner\Info.json
Mods\KingmakerBuffPlanner\KingmakerBuffPlanner.dll
```

Unity Mod Manager lists 0.5.2 and earlier as **Kingmaker Buff Planner**; from
0.5.3 the same entry is listed as **Mr. Bard's Kingmaker Buff Planner**.

In native controller mode, load a campaign and select the Buff Planner card in
the RT/R2 campaign menu. Follow the displayed mapped controls for focus,
routine selection, search, authoring and Back. The controller spellbook footer
opens the same planner; the displayed quick shortcut remains available. The
owner reports basic controller functionality in the 0.6.4 alpha; detailed
acceptance of the exact stable package is pending. Start with no-cast editing
on a disposable plan.

In mouse mode, load a campaign, then open the planner with the HUD's gear button, the
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

## License

Mr. Bard's Kingmaker Buff Planner is released under the [MIT License](LICENSE),
which ships in the release ZIP. [Third-party notices](THIRD-PARTY-NOTICES.md)
credit the MIT-licensed projects studied as design references; no third-party
code, binary or game asset is included. Pathfinder: Kingmaker and its assets
belong to their respective owners.

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

An owner-authorized release without completed human acceptance uses
`-ConfirmOwnerAuthorizedPreRuntimeRelease` instead of `-ConfirmHumanAcceptance`;
see [Build and release](docs/BUILD-AND-RELEASE.md). The final switch is
required only while the repository is private. This
repository is currently PUBLIC, so a published release is downloadable by
anyone; only a DRAFT release (never published) stays visible to the
repository's collaborators alone, which is how private previews are shared.

Runtime qualification may be launched only through the project-owned guarded
harness documented in
[Windows Autonomous Runtime Testing](docs/WINDOWS-AUTONOMOUS-RUNTIME-TESTING.md).
Never deploy by directly replacing the complete live `Mods` tree.
