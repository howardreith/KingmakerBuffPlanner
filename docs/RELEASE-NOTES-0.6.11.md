# Mr. Bard's Kingmaker Buff Planner 0.6.11

Owner-authorized playtest repair following merged PR #20.
This exact release has not been run in the game.

## Changes

- **Heroism no longer stops a routine.** Confirmation now follows the spell's
  real structure: Call of the Wild's Infectious Charms additions only count when
  the caster actually has that feat. A fresh or refreshed Heroism on the right
  recipient confirms, and later planned casts continue. Genuine failures still
  stop the run safely: no effect, an unchanged old buff, a suppressed or
  unreadable effect, or the wrong unit or kind.
- Effects that the planner can't observe on any party member or pet are now
  caught before spending. Cast available omits them with an explained game-log
  line and casts the rest. Require full plan refuses before anything is cast.
- **Workspace text no longer disappears.** Each workspace owns its text materials
  and releases them when it closes, falling back to the native font when needed.
  The title, selected buff, headings, search placeholder, footer feedback and
  **Require full plan** caption are meant to stay visible after reopening and
  loading.
- **Clear plan confirmation uses ordinary native buttons.** Cancel and Clear plan
  hover and unhover normally. The red caption and exclamation mark are gone.
  Cancel is still the safe keyboard and controller default, and Undo still
  restores the plan.
- **Clearer item and area wording.** Item sources read **Item charge**,
  area-targeted buffs read **Area buff**, and selected origins read **Centred on
  <name>**. Predicted reach keeps its note that actual recipients depend on who
  is in the area. Casting mechanics, charges and saved choices are unchanged.

## Known limits

Lab testing of the frozen pre-release candidate confirmed real Heroism followed
by Mirror Image in instant mode, on the first cast and on a recast, with native
slot spending. That build is not byte-identical to this release. Animated mode,
the skip-if-active case with real Heroism, 1920x1200 rendering, physical
mouse hover and the Blessed Signet view were not run in the game. In the lab's
artificially levelled test character, Heroism lasted 3 rounds instead of the
expected 10 minutes per level, so the planner (correctly, under its rules)
treated it as too short to skip. Its cause was not determined; the owner's
campaign showed the normal duration.

## Verification boundary

The owner-authorized pre-runtime publisher runs source validation, the complete
offline C# behavior suite and publisher gate tests, followed by two deterministic
release builds and strict package validation. These compile against the installed
Windows Kingmaker assemblies under .NET Framework 4.7 and C# 7.3.

Saved plan formats, standalone mod identity and gameplay dependencies are
unchanged. This release does not modify an installed game or its saves.
