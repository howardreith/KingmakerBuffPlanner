# Kingmaker Buff Planner 0.2.0 — the casting-first planner

0.2.0 replaces the classic buff planner with the **casting-first planner**:
you plan every cast explicitly — who casts it, from which spell source, on
whom, with which enhancements — and run whole routines with one click.

## Highlights

- **One planner, no ceremony.** Every edit saves itself; there is no Save
  button and no Accept Plan step. The footer shows Saved / Saving... /
  Not saved; if a save ever fails your edits stay in the planner, runs wait
  until the newest edit is saved, and the footer offers Retry save (or
  Reload).
- **One-click routines.** On the HUD the moon runs **Long**, the diamond
  **Important**, the sun **Short** — the planner stays closed while the
  party casts. The gear button or Ctrl+Shift+B opens the planner. Press a
  running routine's button again to stop after the cast in progress.
- **Exact castings.** Each casting is one cast: one caster, one spell source
  (spellbook level, prepared slot, item or ability), one target or group
  origin, and its own enhancements. Nothing is substituted behind your
  back; a blocked casting is refused with its reason.
- **A continuous casting graph.** Casters and their sources on the left, one
  line and card per casting, recipients on the right, on one scrolling
  parchment; the inspector edits the selected casting.
- **Right-click any buff, header or casting card** for the game's own full
  description of that exact spell. Escape closes the description, then the
  planner — never the game's menu.
- **Share Transmutation** (Brown-Fur Transmuter, from the KingmakerGunslinger
  mod): choose the caster and source, then "Share with an ally" before the
  target. The spell slot and the Arcane Reservoir are budgeted together; a
  shortage blocks the whole casting. Powerful Change combines with it.
- **Meaningful enhancements only:** metamagic rods and class features are
  offered when they make sense for that spell and caster; an older choice
  that no longer applies stays visible and removable.
- **Shared resources are accounted across the whole plan** (spontaneous
  slots, rod uses, the reservoir), and reservations are all-or-nothing.
- **Instant casting by default**, with Animated available; an explicit
  Animated choice from an earlier version is kept.

## Upgrading

- **Back up first:** copy your whole `Mods\KingmakerBuffPlanner` folder,
  including `UserSettings`, to a folder outside `Mods` before installing.
- The first time a campaign opens, its classic plan (0.0.19 or 0.1.x) is
  imported once into the casting-first plan. The classic plan file is never
  modified and a byte-exact copy is archived beside it
  (`kbp-casting-<hash>.orig`). A classic casting that let the planner choose
  "any caster" becomes a Draft with no caster — pick one; the planner never
  guesses. A casting-first plan saved by a 0.2.0 preview is loaded as it is.
- A game that explicitly chose the Classic planner in an earlier preview
  keeps it until you use "Switch to the casting-first planner" on the mod's
  settings page (one way).
- To roll back, restore your backup folder (it contains its own
  `UserSettings`).

## Requirements and compatibility

- Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b with Unity Mod Manager.
- Checked alongside BagOfTricks, BetterVendors, CallOfTheWild, CheatMenu,
  CraftMagicItems, EddicKingmakerRespec, KingmakerBugfixes,
  KingmakerDiceRoller, KingmakerGunslinger (0.0.133 and 0.0.136),
  KingmakerLastAzlantiPreserver, ProperFlanking2, RacesUnleashed, SkipIntro,
  TweakOrTreat and ZFavoredClass. None of them is required; Share
  Transmutation needs KingmakerGunslinger's Brown-Fur Transmuter.

## Known limitations

- Windowed display mode has not been qualified (tested at 1920x1080
  fullscreen and the owner's own display settings).
- Share Transmutation is supported for the Brown-Fur Transmuter only.
- Metamagic spell variants, pets, ability pools with more than one use, and
  an area change during a run have not been checked in the game.
- 0.2.0 is the first release of the casting-first planner; feedback is
  welcome and adjustments are planned.

## Qualification

The 0.2.0 code was qualified as candidate `8d7681d03752f3f7170f25f7d45029f71c46a884`:
the full source-only gate (source validation, 398 protocol tests, runtime
harness, package, deployment, launcher, fixture, rollback and publisher
checks) and 41 of 41 guarded in-game runs on disposable test campaigns —
the one-click Long routine from a cold start, the right-click description,
the continuous scroll, autosave with reopen and save reload, classic-plan
import, Share Transmutation and Share + Powerful Change (Instant and
Animated), paid spell slots with Stop / repeat / recast, group spells,
Powerful Change, an Extend rod, ability pools and plan-wide resource
accounting. The 0.2.0 release commit adds only the version number and
documentation to that candidate, and its build was checked again in the
game before publication. The row-by-row record is
`docs/E01-E27-ACCEPTANCE-MATRIX.md`; the player guide is
`docs/CASTING-FIRST-PLAYER-GUIDE.md`.
