# Nexus Mods release copy

Nexus Mods page copy for the owner-authorized 0.5.3 release (GitHub release
`v0.5.3`). Upload the exact GitHub release asset `KingmakerBuffPlanner-0.5.3.zip`
without repackaging it; its SHA-256 is listed in the release's
`SHA256SUMS.txt`.

## Title

Mr. Bard's Kingmaker Buff Planner

## Short description

Plan every party buff once (caster, exact spell source, target, metamagic),
then cast whole Long, Important or Short routines with one HUD click. Never in
combat. Plans live outside your saves.

## Description

Mr. Bard's Kingmaker Buff Planner, by Mr. Bard, is a standalone Unity Mod
Manager mod for Pathfinder: Kingmaker Enhanced Plus Edition. It was published
on GitHub as **Kingmaker Buff Planner** through version 0.5.2. It is free
software under the MIT License.

- **Casting-first planning.** Each casting is one cast with its own caster,
  exact spell source, target or group origin, and enhancements, shown as a
  caster -> casting -> recipient graph. Every edit saves itself.
- **One-click routines.** The HUD's moon, diamond and sun buttons run your
  Long, Important and Short routines. Routines are refused during combat.
- **Real game rules.** Casting respects Kingmaker's targeting, spell slots,
  class-feature and rod resources, durations, material components and
  metamagic. Resources shows what a routine or the whole plan will spend.
- **Instant or Animated.** Instant mode (the default) never silently falls
  back to an animated cast: a casting that cannot be instant is shown as Not
  Ready with the reason. Animated mode casts with the game's normal casting.
- **Require full plan or Cast available.** Each profile remembers whether one
  blocked casting stops the routine or the valid, affordable castings run in
  order with every omission listed.
- **Built from the game's own UI.** The planner, HUD row and spellbook button
  use the running game's fonts, paper, frames and controls. Right-click a buff
  to open the game's own spell description. No game art is shipped.
- **Structural buff discovery.** Buffs are found from the game's ability data
  rather than a hard-coded list, including buffs added by other mods such as
  Call of the Wild.

Open the planner with the HUD's gear button, the spellbook's **BUFF PLANNER**
button or **Ctrl+Shift+B**.

## Requirements

- Pathfinder: Kingmaker Enhanced Plus Edition **2.1.7b**.
- Unity Mod Manager **0.28.2** or later, installed for Kingmaker. The mod uses
  the Harmony 1.2 library (`0Harmony12.dll`) that Unity Mod Manager provides;
  nothing else needs installing.

## Installation

1. Exit Kingmaker.
2. Download `KingmakerBuffPlanner-0.5.3.zip`.
3. In Unity Mod Manager, select Pathfinder: Kingmaker and drag the ZIP into
   the **Mods** tab. Or extract it into Kingmaker's `Mods` folder so that
   `Mods\KingmakerBuffPlanner\Info.json` exists.
4. Start the game. Unity Mod Manager lists **Mr. Bard's Kingmaker Buff
   Planner** 0.5.3 once.
5. Load a campaign and press **Ctrl+Shift+B**.

## Upgrading from Kingmaker Buff Planner

This is the same mod under a new name, not a separate mod. The UMM ID, DLL,
`Mods\KingmakerBuffPlanner` folder and plan files are unchanged, so it
replaces your existing entry instead of appearing twice.

1. Exit Kingmaker.
2. Back up the whole `Mods\KingmakerBuffPlanner` folder, including
   `UserSettings`, somewhere outside `Mods`.
3. Install the new ZIP as above, over the existing folder.
4. Your plans in `UserSettings` should load unchanged: 0.5.3 reads the same
   plan format as 0.5.2. This follows from the unchanged persistence code; an
   in-place upgrade was not exercised in a running game for 0.5.3.

Upgrading from an older version applies that version's usual one-time
conversions. For example, a classic plan from an earlier version is imported
once and the original file is kept beside it. If a plan file is in a format the
installed version cannot read, for example one written by a newer version, the
planner leaves it untouched and refuses to save until that is resolved. To roll
back, exit the game and restore your complete backup.

## Uninstalling

Remove `Mods\KingmakerBuffPlanner`. Plans are stored as JSON in the mod's
`UserSettings` folder, never in your saves, so saves do not depend on the mod.

## Compatibility

- **Standalone.** Requires no other gameplay mod and bundles none.
- **Call of the Wild (optional).** Its buffs, hexes and revelations are read
  after load. Catalogue discovery was checked against Call of the Wild
  1.14.4c-2.1. Save-backed casting with that profile was not part of that
  check.
- **Kingmaker Gunslinger (optional).** When installed, Brown-Fur Transmuter
  Powerful Change and Share Transmutation and Magic Circle against Alignment
  are offered where they apply. Without it, the planner works normally
  without those options.
- **Tabletop Added Rules.** No compatibility claim is made. No reference copy
  was available to check against.

## Known limitations and testing

- **0.5.3 has not been run in the game.** It is a renaming and licensing
  release: the planner code is 0.5.2's. Only the display name, author, DLL
  title/product/company, one Unity Mod Manager settings-page label and the
  added `LICENSE` file changed. Offline source validation, the complete
  offline C# behavior suite, two deterministic release builds and strict
  package validation ran for this release; the broad runtime/purity suite and
  human playtesting did not.
- **0.5.2's own boundary still applies.** Its newest paths (the Resources
  dialog, continuous assignment, Require full plan / Cast available) passed
  source, build and behavior checks, but were not observed in a fresh
  in-game run. A delay of one to two seconds reported in a large real
  campaign has not been ruled out.
- **0.5.0 in-game checks did not cover** remapped keys, prepared casters,
  pets or metamagic spell variants.
- Routines never run during combat. Instant mode never substitutes an animated
  cast.

## Credits and permissions

- Author: Mr. Bard.
- Design references (no code or assets included): BubbleBuffs (Sean Petrie /
  Vek17), BuffIt2TheLimit / Wrath Epic Buffing (Sean Petrie, factubsio, Gh05d),
  PathfinderAutoBuff (Ilkar, Holic92, Narria, Hsinyu) and KingmakerRebalance /
  Call of the Wild (Holic75), all MIT-licensed. See `THIRD-PARTY-NOTICES.md` in
  the download.
- Pathfinder: Kingmaker is by Owlcat Games. The planner borrows the running
  game's own sprites, fonts and sounds at runtime. No game asset is
  distributed.
- License: **MIT** (`LICENSE` in the download, `Copyright (c) 2026 Mr. Bard`).
  You may use, copy, modify, merge, publish, distribute, sublicense and sell
  copies of the mod, provided the copyright and permission notice is kept.
- Nexus permission settings, matching the MIT terms: upload to other sites
  allowed; modification allowed; conversion allowed; asset use allowed;
  commercial use allowed. In every case keep the MIT copyright and permission
  notice and credit Mr. Bard. These permissions cover this mod only, not
  Pathfinder: Kingmaker or its assets, which belong to their owners and are
  not included.

## Bug reports

Please report bugs at https://github.com/howardreith/KingmakerBuffPlanner/issues
and include your Unity Mod Manager log and the mod version.
