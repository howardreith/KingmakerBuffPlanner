# Mr. Bard's Kingmaker Buff Planner 0.6.14

Owner-authorized repair following PR #23.
This exact release has not been run in the game.

## Changes

- **Item castings stay connected.** A saved casting from an item, such as
  Blessed Signet, no longer disconnects from its source after saving and
  loading. The casting now remembers "the item of this type this caster has
  equipped", and each run still spends the charges of that one live item.
- **Identical copies are never chosen for you.** With identical copies equipped
  on one caster, choose a copy once under Change caster/source. The choice is
  remembered by equipment slot, and each copy keeps its own charges.
- **Older castings are repaired once.** A disconnected casting saved by an
  earlier version shows "Source needed". Select it, choose Change caster/source
  and click the current Item charge row. Its routine, order, recipient, recast
  choice and enhancements are kept, Undo works, and the repair lasts.
- **Clearer messages.** A missing or ambiguous saved item is no longer reported
  as the character being unable to cast the buff. The "(select again after
  loading)" warning is gone, and a repaired casting's line attaches to its
  source row.

## Known limits

This repair has not been run in the game, including area transitions, ordinary
rest and Bag of Tricks Instant Rest. The game saves no identity for an individual
item, so another equipped copy of the same type satisfies a casting and spends its
own charges. A slot choice is a position: swapping identical copies swaps which
one is used. Moving an item out of its chosen slot makes that casting need a new
choice. If the item is unequipped or given to another character, the casting
shows "Source needed" and does not follow the item.

Castings saved by this version do not resolve if you roll back to an earlier
version.

## Verification boundary

The owner-authorized pre-runtime publisher runs source validation, the complete
offline C# behavior suite and publisher gate tests, followed by two deterministic
release builds and strict package validation. These compile against the installed
Windows Kingmaker assemblies under .NET Framework 4.7 and C# 7.3. Details and the
remaining in-game steps are in the
[durable item-source checkpoint](DURABLE-ITEM-SOURCE-CHECKPOINT.md).

The saved plan file format and standalone mod identity are unchanged. This release
does not modify an installed game or its saves.
