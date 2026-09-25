# Supervised manual usability session — implemented procedure

## Casting-graph workspace review (2026-09-25, current)

This session asks for your verdict on the rebuilt casting-first workspace
(addendum v1.1). It runs a development build of the branch
`claude/casting-graph-ui-correction` from this checkout, rebuilt at the
exact HEAD just before the session, in the same guarded transaction as
every other run: the disposable `KBP_AUTOMATION_WORKING` campaign, Mods
staged and restored exactly, native casting disabled (the planner authors
and saves; nothing casts). It starts only after the automated frame runs
of the same build have passed with your session connected.

The command (the operating agent runs it and tells you when the
workspace is ready):

```powershell
& 'C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\scripts\Invoke-KingmakerRuntimeTest.ps1' `
    -Scenario live-workspace-manual -CompatibilityProfileId full-user `
    -RunId casting-graph-manual-HHmm -ManualHoldSeconds 1200 `
    -TimeoutSeconds 1620 -Confirm:$false
```

What to judge (in any order; say what you see, in your own words):

1. **Which planner.** The header reads `Planner: Casting-first`. (The
   Classic screen reads `Planner: Classic` and offers the switch.)
2. **Layout.** A narrow searchable buff list on the left; the chosen buff
   large at the top; casters and their sources on the left of the graph;
   castings as lines and cards in the middle; targets on the right; the
   inspector for one casting on the right; budgets and actions in the
   footer.
3. **Primary gesture.** Choose a buff, a caster (and its exact source row
   when it has several), then click a target: exactly one line and one
   card appear, and the card is focused. Clicking a target that already has
   this buff shows that casting instead of adding a second.
4. **Capacity.** Each source row says how many more castings it can pay
   for after everything else in the plan (a shared pool is named as
   shared). Add castings until one runs out: every row that draws on that
   pool updates; remove one and the counts come back.
5. **One casting at a time.** Click a line (it has a wide hit area) or a
   card: the inspector shows only that casting. Enhancements list their
   mechanism, whether required, cost, what is left of their pool after the
   plan, the expected effect, and why an unavailable one cannot be taken.
   Add one (for example Extend) and see the budget change; Undo.
6. **Group buffs.** One card per casting with dashed branches to whoever
   the area is predicted to reach; mark recipients as required; a required
   recipient outside the area is shown as missed, never silently added.
7. **Hover.** Sweep the pointer over rows, cards, lines, targets, tabs and
   buttons, and click a tab or button then move away: exactly one
   highlight, always the control under the pointer, and nothing stays lit.
8. **Buttons.** Captions readable on every button: normal, hover, pressed,
   disabled and selected (the chosen routine tab).
9. **Save and reopen.** Save, close (Escape or the hotkey), reopen: the
   same castings, lines and counts. Reload.
10. **Crowded plan and long names.** Many castings between the same people
    stay distinct and selectable; nothing overlaps.

Say **"done"** (or **"stop"**) to end; the terminal procedure below is
unchanged. Your verdict — accept, or reject with what is wrong — is quoted
exactly in the status file; automated evidence is never presented as your
acceptance.

## Release-candidate session (0.2.0-rc6, history)

**Release-candidate session (0.2.0-rc6).** This is the one consolidated
manual acceptance session the mission asks for near the end. It runs the
frozen candidate from its own clean checkout,
`C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC6`
(commit `24d9967`; identities in `docs/evidence/rc-0.2.0-rc6-receipt.md`).
The procedure was rehearsed end to end on rc2
(`casting-ws-manual-20260923-rc2-rehearsal`); its rehearsal on the rc6
build judges game frames, so it runs first once your session is connected
(with the other deferred frame-judged runs listed in the receipt), before
you are asked to take part. In this session the planner authors and saves but cannot cast:
native casting is disabled in every automated or supervised test session,
and the candidate's casting was qualified separately by the guarded runs
listed in the receipt.

What to look at first, because it changed since your last session (rc2).
rc6 changes how a casting runs, not the workspace: a casting now uses
exactly the enhancements it chose, and a rod or Brown-Fur switch you left
on is switched off for the other castings and back on afterwards. Casting
is disabled in this session, so this was qualified by the guarded runs in
the receipt; you would see it only in ordinary play after installing.

New in rc5:
- **A buff already on some of a group's recipients**: the card says
  "already active on (the character) (the cast goes ahead for the
  others)", and warns when the cast may shorten a longer buff.
- **Enhancement chips** list only what the casting can take (a chosen one
  stays listed so you can remove it); Share Transmutation is never
  offered, and a casting that still holds it says why it cannot run.
- **Skip if already active** casts again when the existing buff's
  strength cannot be read (the card says "not provably as strong" or
  "possibly suppressed").

Since rc2:
- **Edit one casting** (press Edit on a card): **Cast by (this
  casting)** names each exact source (for example "Linzi: Bard level 1
  (spell slot), caster level 5"); **Routine and order**; **If the buff is
  already there** (skip or cast again); the single/group switch where the
  buff allows it; enhancements that follow the caster (the footer names
  any the new caster does not have; Undo brings them back).
- **The next casting**: **Cast from** when a caster has more than one way
  to cast the buff; a spell known at two levels of one spellbook is
  refused with its reason.
- **Plan settings**: the animated fallback for Instant mode and **Cast
  only out of combat**.
- **Save / Reload**: the header says "not saved" while saving is refused;
  a Reload that could replace unsaved changes asks for a second press.
- **The classic planner** (the default mode): APPLY now closes the planner
  so the party casts at once, and the result appears when you reopen it.
  This changes the default planner's behaviour and is yours to confirm;
  casting is disabled in this session, so it can only be seen in ordinary
  play after installing the candidate.
- The buff grid tabs, routine tabs with counts, card costs, the footer
  naming whose resource and what kind, and refusals that say what to do.

Every mechanism below is implemented in the pushed harness (scenario
`live-workspace-manual`), rehearsed end to end before you are asked to
participate, and bounded by the existing guarded transaction against the
designated disposable `KBP_AUTOMATION_WORKING` campaign. Native casting
and legacy quick execution remain disabled; nothing can cast a buff.

## The supported command (exact)

```powershell
& 'C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC6\scripts\Invoke-KingmakerRuntimeTest.ps1' `
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
2. The operating agent (Claude since the 2026-09-22 takeover; Z
   before that) confirms `manual-ready.json` and the `[KBP-MANUAL]
   manual-ready` game-log line, reports the exact build identity, and
   only THEN tells you the session is ready. The paused state is a non-blocking frame
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

Say **"done"** or **"stop"** to the operating agent, who performs the
implemented terminal operation: atomically writing `manual-done.json`
(completion) or `manual-stop.json` (cancellation) into **that run's**
evidence directory, recording the run id, the instruction's origin
(you) and the time. The harness consumes the marker (marker presence;
stop wins if both are seen), then:

1. requests one final camera-path capture and waits for its callback
   for at most 20 s — capture can never hold the session open;
2. consumes that capture's own failure and camera-restoration verdict
   (review J2) instead of trusting the file name;
3. closes the workspace through its production lifecycle and records
   the postcondition (view closed, input lease released) even if the
   capture failed or never reported;
4. writes the result, exits, and the launcher restores the exact
   pre-run Mods state with a verified receipt.

The result keeps separate assertions for the operator request
(`manual-session-outcome`), final evidence (`manual-final-capture`),
camera restoration (`manual-camera-restoration`) and cleanup
(`manual-workspace-closed`). A failed or missing capture is a visible
FAIL of the evidence, not of your session; an unclean or unobserved
camera restoration is never a clean PASS. None of these is a usability
verdict — your observations are recorded separately.

If the hold deadline passes with no marker, the run ends as
`manual-deadline` — **a deadline is never acceptance** and the
transaction still restores. Incomplete restoration takes priority over
any further run.

## Evidence labeling

Automated callback coverage (runs `casting-ws-gseries-*`; source-only
protocol suite 231/231 at the J-review repair), rendered captures, your manual observations, and any
screenshots you take are kept as distinct evidence layers; manual
artifacts are labeled as manual and never counted as automated
acceptance, and vice versa.
