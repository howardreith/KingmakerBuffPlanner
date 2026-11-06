# Mr. Bard's Kingmaker Buff Planner 0.6.13

Owner-authorized repair following merged PR #22.
This exact release has not been run in the game.

## Changes

- **Repeated prepared spells no longer stop a routine.** When a plan casts the
  same prepared spell twice from one caster (for example Heroism on two
  different allies), the second casting no longer stops the run with
  `provider-ability-not-found`. The game spends the last matching prepared
  copy of a spell level rather than the copy the planner reserved, so the
  first cast used up the copy meant for the second. The second casting now
  uses the identical copy the first one left behind. This works in both
  Instant and Animated execution, for one-slot and two-slot (opposition
  school) preparations.
- **Nothing extra is spent.** A casting never takes a copy reserved for a later
  casting, an unplanned copy, or a different preparation (another metamagic,
  heighten level, variant, slot type or slot cost), and never another caster,
  book or item. Nothing is refilled or cast twice. A real shortage still
  follows Cast available or Require full, and a genuine failure still stops
  the routine.
- **Clearer messages and details.** A refused casting now says whether its
  preparation is spent or no longer prepared. Each prepared cast's details
  name the slots the game actually spent.

## Known limits

This repair has not been run in the game. Animated execution of repeated
prepared spells is covered only by offline tests. If the game ever spends a
copy from a different slot type (for example a Favorite slot for a Common
reservation), that casting still stops, now with a clear "exhausted" message.

## Verification boundary

The owner-authorized pre-runtime publisher runs source validation, the complete
offline C# behavior suite and publisher gate tests, followed by two deterministic
release builds and strict package validation. These compile against the installed
Windows Kingmaker assemblies under .NET Framework 4.7 and C# 7.3. Details are in
the [prepared-slot repair checkpoint](PREPARED-SLOT-SPEND-REPAIR-CHECKPOINT.md).

Saved plan formats, standalone mod identity and gameplay dependencies are
unchanged. This release does not modify an installed game or its saves.
