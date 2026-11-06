# Mr. Bard's Kingmaker Buff Planner 0.6.12

Owner-authorized final refinement following merged PR #21.
This exact release has not been run in the game.

## Changes

- **Plan policy is now a button.** The footer's Require full plan checkbox is a
  native button that states the current policy, **Plan: Cast available** or
  **Plan: Require full**, and toggles the same saved choice. Existing choices
  are kept; nothing is migrated again.
- **Tooltips on every footer action.** Resources, Plan, Mode, Clear plan, Undo,
  Run and Retry save, plus the Clear dialog's Cancel and Clear plan, explain
  what they do, what a click changes and why a button is unavailable. They
  appear on mouse hover and on keyboard/controller focus, and stay out of the
  way of dialogs.
- **More room.** The idle explanation line and the "Saved" text are gone, and
  their row is removed, so the catalogue, casting graph, recipients and inspector
  are taller (about 97 units at 1920x1200, by layout calculation). Autosave is
  unchanged. A failed save still shows a notice with **Retry save**, and a run
  still waits for the latest edit to be saved.
- **Clear plan looks like a game dialog.** It uses the game's own message-box
  scroll paper with a centred title and ordinary buttons. A successful Clear also
  removes an old error notice from a previous run attempt.
- **Honest resources after Clear.** A caster whose uses are genuinely spent now
  reads **No prepared uses remaining** (or **No uses remaining**) instead of
  blaming the plan. Clearing never refunds anything.
- **Heroism with Infectious Charms (Call of the Wild).** The feat's three-second
  follow-up markers no longer decide whether an existing Heroism counts as
  already active, so skip-if-active stops recasting and spending a slot after
  they expire. Confirmation now follows the nearby ally the game actually picks
  when the spell lands, instead of the one predicted when the run started.
  Missing, wrongly placed, stale or unverifiable markers still stop the run
  safely.

## Known limits

None of these changes has been run in the game. Rendering at 1920x1200 and
1920x1080, tooltip placement, the Clear dialog's appearance, physical mouse,
keyboard and controller input, and Heroism with Infectious Charms in both
execution modes remain to be checked in play. If an ally outside the party (for
example a summon) is the nearest Infectious Charms target, the planner cannot
observe it and reports the cast as unconfirmed rather than successful.

## Verification boundary

The owner-authorized pre-runtime publisher runs source validation, the complete
offline C# behavior suite and publisher gate tests, followed by two deterministic
release builds and strict package validation. These compile against the installed
Windows Kingmaker assemblies under .NET Framework 4.7 and C# 7.3. Details are in
the [final refinement checkpoint](FINAL-REFINEMENT-0.6.12-CHECKPOINT.md).

Saved plan formats, standalone mod identity and gameplay dependencies are
unchanged. This release does not modify an installed game or its saves.
