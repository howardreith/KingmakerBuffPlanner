# Mr. Bard's Kingmaker Buff Planner 0.6.5-alpha.1

This owner-authorized pre-runtime alpha is for testing controller navigation
and focus visibility. This exact release has not been run in the game. It is a
GitHub prerelease; stable 0.6.4 remains the latest stable release.

The controller workspace now moves focus according to controls' on-screen
positions instead of cycling through abstract regions. It begins on the
selected buff when available, preserves the horizontal position when moving
through wide rows, and draws a persistent border outside scrolling content.
Right-stick scrolling keeps focus on a visible control when one exists. In a
text-only Inspector section, the border marks the viewport so the text can be
read without snapping back to an offscreen acknowledgement button. Confirm
cannot activate that hidden button.

## Suggested controller test

1. Open Buff Planner from its native campaign card. Confirm that the selected
   buff has a visible border.
2. Focus **Spells**, release the controls, and wait at least six seconds.
   Repeat after changing a filter and after rebuilding the workspace.
3. Move left and right through **All**, **Spells**, **Abilities**, and **Other**.
   Move down into the catalogue and back up; verify the same filter is focused.
   Cross the caster, casting, recipient, and Inspector columns, then the footer.
4. Scroll long lists with the right stick. In an imported plan with a long
   Inspector notice section, scroll into text above **Acknowledge imported
   constraints**. The viewport should stay at the requested position and
   Confirm must not activate the offscreen button. Scroll back until the button
   is visible and confirm that it can then be selected.
5. Try Back, Resources, search, and description modals. Navigation alone should
   leave the plan unchanged. Use only a disposable working save for any plan
   edit or run.

The [focused checklist](CONTROLLER-NAVIGATION-REPAIR-2026-10-20.md) has the
remaining layout and input cases. Please provide the UMM/Unity log and a
screenshot or short recording if the border disappears or scrolling reverses.
The original recording showed lost visible Spells focus; it did not establish
the exact Unity rendering cause. This alpha is for native observation of the
proposed repair, not a claim of in-game qualification.
