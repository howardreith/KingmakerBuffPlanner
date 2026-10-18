# Controller Alpha 3 — 0.5.4-controller.3

Experimental test release requested by the owner. Not native-qualified, not full
gamepad graph editing, and not a replacement for stable 0.5.3's support boundary.

Includes Configure plans opening the actual planner in a controller-started
campaign, mouse/keyboard authoring, autosave, same-session return to routines,
console-spellbook editor entry, and controller text-initialization/layout repairs.
Keeps the accepted campaign menu -> displayed Buff Planner shortcut.

This alpha additionally fixes the two focused review findings:

- Escape/Back preserves text cancellation even if TMP deactivates the field
  before planner polling. One press leaves text editing; another deliberate
  press can dismiss the next layer. Native and keyboard requests share one latch.
- Results move by overlapping viewport steps, not a percentage of the entire
  report, so long reports do not skip whole sections.

These are source-tested corrections. Actual native editor pointer/click/scroll/
text entry, repaired pixels, physical-controller isolation and real resource
outcomes still need testing. No claim that screenshots from earlier versions
verify this build. Native fan card, original-book return and full gamepad graph
editing are deferred. No Gunslinger/CotW update or dependency is introduced.

## Installation and first check

Back up the complete current Buff Planner mod folder (including UserSettings)
and saves. Download the installable `KingmakerBuffPlanner-0.5.4-controller.3.zip`
asset, not GitHub's Source code archive, and install through Unity Mod Manager
with the game closed. Do not remove unrelated mods or alter saves to test this.

Use a disposable test campaign first. At 1920x1200, before casting:

1. Start in native controller mode; campaign menu -> displayed planner shortcut.
2. Check readable initial/settled text; Configure -> visible mouse pointer.
3. Actually click, scroll and type in the existing editor. Escape while typing
   must leave editing only, including when no other editor layer is open.
4. Configure one casting, allow canonical autosave, then Close. Check the correct
   routine count/save state and controller Back returning normal gameplay input.
5. Check a long Results view in both directions; successive screens overlap and
   the first/last entries are readable. Check native window takeover/return and
   a compact supported size without changing global display configuration.

Only after no-cast access/authoring passes test finite-resource routines/Stop.
Report initial/settled screenshots and logs with exact build/controller details.
Existing failed-save/import recovery and canonical execution safeguards remain.
The selector itself does not pause; the editor must be closed before running.

Publication is an owner-authorized non-Latest alpha only. No main merge,
permanent DATA installation, game launch, save editing or unrelated mod changes.
