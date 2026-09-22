# Supervised manual usability session — handoff for Howie

This session exercises the workspace with REAL mouse and keyboard input
while the harness performs NO automatic input. It runs inside the
existing guarded transaction against the designated disposable
`KBP_AUTOMATION_WORKING` campaign only. Native casting and legacy quick
execution remain disabled; nothing can cast a buff.

## Build identity (record at session start)

- Branch/HEAD: `codex/kingmaker-buff-planner-casting-first` at the
  pushed HEAD recorded in AUTONOMOUS-RESUME.md's top section.
- Package: `artifacts/local-runtime/0.1.1-rc3/KingmakerBuffPlanner-0.1.1-rc3-local-runtime.zip`
  (SHA-256 recorded by Build-Local in the run log).
- Loaded-module identity, fixture profile (full-user, 15 mods), and save
  identity are asserted by the launcher before the game starts; the run
  ID appears in every evidence path.

## Entry (verified path — no new flags)

1. Z launches: `scripts/Invoke-KingmakerRuntimeTest.ps1 -Scenario
   live-workspace-qual -CompatibilityProfileId full-user -RunId
   casting-ws-manual-<time> -TimeoutSeconds 3600 -Confirm:$false` from
   PowerShell (the supported direct invocation).
2. The harness performs its read-only preflights, stages the exact
   transaction, launches the game, loads the WORKING campaign, closes
   UMM, and — instead of continuing automation — pauses after the
   workspace opens. (Z confirms the pause in the run log before you
   touch anything.)
3. You operate the workspace with your own mouse/keyboard. The harness
   sends NO further keys or clicks while you interact: automatic input
   markers are already consumed and physical deliveries are suppressed
   by the terminal-state markers written before your session begins.

## Checklist (record each action and what you saw)

1. Pick a named buff, caster, and recipient with the mouse; press Add
   Casting; switch caster and add a second card. Check source names,
   character names (not GUIDs), readiness labels, and full captions.
2. Edit one card: change a supported enhancement or target, press Undo,
   press Done — back to next casting, then add another card. Try
   group → single target → group, a legal origin choice, intended vs
   predicted coverage; confirm no hidden extra casting appears.
3. Close with Escape/hotkey while work is UNSAVED and reopen: the work
   must be unchanged (retained session). Then press Save, and press
   Reload to prove persisted loading — do not conflate the two.
4. Scroll to the last buff and last enhancement row; inspect long
   labels, footer controls, hover/pressed/disabled states, tooltips;
   confirm the game is input-isolated while the workspace is open and
   native click sounds play at the current volume. No buff casting
   should occur at any point.

For anything the party cannot supply, report it untested — do not add
characters, spells, equipment, buffs, or facts.

## Exit and restoration (either outcome)

- Say "done" or "stop": Z stops any pending input, captures your
  game-window observations/screenshots if offered, then lets the
  launcher complete: the game exits and the transaction restores the
  exact pre-run Mods state with a verified receipt. Incomplete
  restoration takes priority over any further run.
- Screenshots you take are separately labeled manual evidence; they are
  never described as automated captures.

## Coverage boundaries

This manual session qualifies HUMAN input on the visible controls and
native presentation observations. It does not qualify the synthetic
input/launcher seam (owned separate checks), exact rods, native class
adapters, or any native casting (disabled). Automated control-callback
coverage and persistence are already proven at this HEAD (protocol
223/223; run casting-ws-gseries-081000).
