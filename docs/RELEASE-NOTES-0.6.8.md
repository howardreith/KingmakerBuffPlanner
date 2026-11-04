# Mr. Bard's Kingmaker Buff Planner 0.6.8

Owner-authorized stable update for duplicate item sources and stale casting
handling, following merged PR #16. This exact release has not been run in the
game. Native rendering, mouse/controller navigation, scrolling and actual
in-game charge spending remain unverified.

## Changes

- Every genuinely available identical item copy and capable owner appears under
  one buff entry. Multiple copies on one caster remain separate exact choices;
  choosing a copy is explicit, with its own charges and execution binding.
- Saved Item and legacy free-Fact selections whose exact source is unavailable
  remain visibly unresolved instead of displaying a replacement provider.
- Unresolved castings expose a numbered Remove action near the inspector top.
  Resolved castings retain one bottom Remove action. Selecting a different
  casting resets inspector scroll; refreshing the same one preserves position.
  Exact removal, autosave, Undo and ambiguous-recipient protection are retained.
- Probe and qualification selection retain canonical catalogue IDs for authoring
  while validating and projecting the selected spell's own discovery expression.
  Equivalent spell forms and distinct spells can share a catalogue entry without
  borrowing one another's blueprint reference wrappers.

## Saved plans and item-source limits

Saved casting intent and targets are preserved. Item selections remain tied to
the exact live item object and can require explicit reselection after loading a
save. No persistent item tracking is added. Unavailable equipment and arbitrary
unequipped inventory are not made castable. Per-copy charges and shared charges
between abilities of the same item retain the 0.6.7 native resource contracts.

If a recipient matches several castings, select the exact numbered casting card
to inspect or remove it. Undo restores an exact removal.

## Verification boundary

The owner-authorized pre-runtime publisher runs source validation, the complete
offline C# behavior suite and publisher gate tests, followed by two deterministic
release builds and strict package validation. These checks compile against the
installed Windows Kingmaker assemblies. Detached native-method tests do not
establish physical input, rendering or live gameplay acceptance.

For native verification, use the repository's guarded workflow and approved
disposable save pair, including current source/WhatIf purity and transactional
restoration. Check all genuinely available item copies, independent charges,
stale numbered Remove, #4 survival, Undo/reopen and mouse/controller scrolling.
Actual casting requires the existing run-bound allowance workflow. No native
qualification or Linux compatibility qualification is asserted by this release.
