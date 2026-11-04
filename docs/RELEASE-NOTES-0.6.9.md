# Mr. Bard's Kingmaker Buff Planner 0.6.9

Owner-authorized stable update for Clear plan and casting diagnostics, following
merged PRs #17 and #18.
This exact release has not been run in the game.

## Changes

- Clear all routines' planned castings with a confirmation and one undoable,
  autosaved edit. The action refuses while a run owns the workspace. Keyboard
  and controller navigation retain the confirmation's input scope.
- Prevent workspace keyboard input from passing through native spell
  descriptions, including the release edge that closes them.
- Casting reports distinguish an unconfirmed application from an absent buff.
  Bounded before/after diagnostics retain effect identity, instance, expiry and
  suppression, with native strength/duration context where readable.
- Successful Animated reports preserve confirmation details alongside actual
  native spending and command-route evidence. Qualification checks retain their
  existing safeguards.
- Added stateful repeat-run regression coverage through the production planning,
  execution and reporting paths, including exhausted-source skips, replacement,
  refresh, expiry/replenishment and failed observations in both casting modes.

## Heroism investigation remains open

This release does not establish or repair the reporter's native Heroism casting
failure. The fixture demonstrates a possible unchanged-effect state; it does not
prove that the reporter's game produced it. An unchanged old buff does not count
as a confirmed refresh. Valid applications proceed, adequate existing coverage
can skip before spending, and real failures or unconfirmed attempts still halt
the routine with resource use reported separately.

No Heroism-specific success exemption, retained-effect success outcome, relaxed
resource check or changed continuation policy is included. Native prepared-slot
reuse between runs without replenishment remains unverified.

## Verification boundary

The owner-authorized pre-runtime publisher runs source validation, the complete
offline C# behavior suite and publisher gate tests, followed by two deterministic
release builds and strict package validation. These compile against the installed
Windows Kingmaker assemblies under .NET Framework 4.7 and C# 7.3.

Native casting, Clear plan rendering/input, mouse/controller behavior and Linux
compatibility have not been verified for this exact release. Native Heroism work
first requires an executable Heroism-plus-distinct-sentinel recipe, then a frozen
candidate, a completed passing guarded selection, a selection-bound allowance,
and guarded casting scenarios with protected saves and exact restoration.

Saved plan formats, standalone mod identity and gameplay dependencies are
unchanged. This release does not modify an installed game or its saves.
