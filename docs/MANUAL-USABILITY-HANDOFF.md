# Supervised manual usability session — implemented procedure

Every mechanism below is implemented in the pushed harness (scenario
`live-workspace-manual`), rehearsed end to end before you are asked to
participate, and bounded by the existing guarded transaction against the
designated disposable `KBP_AUTOMATION_WORKING` campaign. Native casting
and legacy quick execution remain disabled; nothing can cast a buff.

## The supported command (exact)

```powershell
& 'C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\scripts\Invoke-KingmakerRuntimeTest.ps1' `
    -Scenario live-workspace-manual -CompatibilityProfileId full-user `
    -RunId casting-ws-manual-HHmm -ManualHoldSeconds 900 `
    -TimeoutSeconds 1500 -Confirm:$false
```

- Replace `HHmm` in the RunId with the current local time (for example
  `casting-ws-manual-1435`); the id allows letters, digits, dot,
  underscore, and hyphen only.
- `-ManualHoldSeconds` (30–1200) bounds the human phase. The launcher
  refuses the run unless `-TimeoutSeconds` covers hold + 420 s of
  boot/load budget.
- In this scenario the harness performs **zero synthetic input**: no
  UMM escape chord, no hotkey chord, no pointer injection. Campaign
  load, UMM close, and workspace open are all programmatic in-game.

## Entry and the acknowledged paused state

1. The harness loads the exact WORKING campaign, closes UMM through its
   verified lifecycle, captures the matched control frame, opens the
   workspace programmatically, captures the open frame, and only then
   writes `manual-ready.json` into the run's evidence directory with
   `syntheticInputRequested=false`, `workspaceOpen=true`,
   `legacyScreenClosed=true`, and the hold deadline.
2. Z confirms `manual-ready.json` and the `[KBP-MANUAL] manual-ready`
   game-log line, reports the exact build identity, and only THEN tells
   you the session is ready. The paused state is a non-blocking frame
   loop — rendering, cursor, and input processing are fully alive; no
   scripted authoring runs.

## While it is your session

- Use your own mouse/keyboard on DATA (or the existing visible desktop).
  Nothing automatic competes with you.
- Work the checklist: named buff/caster/recipient authoring across two
  casters; Edit → enhancement or target change → Undo → Done → Add;
  group ↔ direct transitions with origin and intended coverage; unsaved
  close (Escape/hotkey) and reopen unchanged; Save then Reload; scroll
  to the last rows; long labels; hover/pressed/disabled states; tooltips;
  world-input isolation; native click sounds at current volume.
- Missing party capability is "Not Run" — never manufactured.

## Ending the session (implemented, run-bound)

Say **"done"** or **"stop"** to Z, who performs the implemented terminal
operation: writing `manual-done.json` (completion) or `manual-stop.json`
(cancellation) into that run's evidence directory. The harness consumes
the marker, captures a final frame, closes the workspace through its
production lifecycle, writes the result, exits, and restores the exact
pre-run Mods state with a verified receipt. If the hold deadline passes
with no marker, the run ends as `manual-deadline` — **a deadline is
never acceptance** and the transaction still restores. Incomplete
restoration takes priority over any further run.

## Evidence labeling

Automated callback coverage (runs `casting-ws-gseries-*`, protocol
225/225), rendered captures, your manual observations, and any
screenshots you take are kept as distinct evidence layers; manual
artifacts are labeled as manual and never counted as automated
acceptance, and vice versa.
