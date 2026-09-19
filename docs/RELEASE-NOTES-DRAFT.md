# Kingmaker Buff Planner 0.1.1-rc3 — Corrected Testing Preview

**This is a corrected testing preview for the owner's inspection, not a
stable release.** rc3 repairs the rc2 review findings (per-spell budget
accounting, assignment-scoped chooser behavior, budget-detail overflow,
and the documented rollback path) on top of the rc2 repairs for the three
failures you observed in 0.1.1-rc1 (missing native appearance, missing
spellbook button, chooser that implied unlimited rod allocation, numeric
metamagic labels).

**What is and is not verified:** all repairs are exercised by
deterministic source tests through production callers (181 protocol
tests, including the rc2-failure regressions and three new
review-finding regressions), by isolated-state tests of the rollback
tooling (4/4), and by offline reflection against your installed game and
Call of the Wild assemblies. Rendered appearance, the visible spellbook
button, and in-game budget displays have **not been observed in a live
session by the developer** (guarded in-game runs were refused while
Kingmaker was running — yours, untouched); the five-minute check below
settles those in your installation. Actual native charge spending
remains a separate untested lane.

## What changed since 0.1.1-rc2

- **Per-spell budget honesty.** The chooser row's "this spell/this
  assignment" note is now scoped to the selected spell (or child
  assignment) instead of aggregating the whole routine, and it
  distinguishes units: requested targets, funded targets, already-active
  skips, communal casts, and allocated charges. Four different spells
  sharing one three-charge rod now each show their own 1/1 or 0/1
  funding next to the correct routine-wide 4 requested / 3 allocated /
  1 unmet line.
- **Assignment chooser reads the assignment's own configuration.** The
  assignment-aware chooser builds selected state, notes, and
  availability from that child's selections and caster constraints, not
  the Automatic child's. An enhancement selected only on a pinned child
  shows as selected there; a rod owned by another caster is unavailable
  for a child pinned to that caster even though the source-wide union
  includes it.
- **Unavailable selections stay individually removable.** An exhausted
  or vanished enhancement that is still configured stays visible,
  selected, and clickable for removal — in both the ordinary and the
  assignment choosers — without clearing unrelated selections. Policy
  captions state the actual semantics: ordinary enhancements toggle
  REQUIRED/OPTIONAL; a targeting modifier (e.g. Share) states
  REQUIRED (targeting) and cannot present itself as optional.
- **Budget detail cannot overflow.** The chooser's sticky summary is a
  compact, length-bounded line (pool count, worst shortage,
  "editing never consumes charges"); the full per-pool detail with
  affected spells/targets lives on each row's tooltip and in
  Assignments & Resources, and row notes are length-bounded so any name
  lengths stay renderable inside the fixed row layout.
- **Real rollback documentation and tooling.** The previously documented
  rollback command targeted the wrong transaction type. A dedicated
  guarded `scripts/Restore-InstallLocal.ps1` now rolls a recorded
  installation back to its preserved prior version while keeping every
  profile you edited after installing (tested 4/4 in isolated state:
  WhatIf purity, profile preservation, evidence archive, refusal of
  non-Installed records and unknown ids).
- **Smaller review observations.** The native-theme late-donor retry now
  actually triggers at chooser-reopen rebuilds (bounded attempts), not
  only on component enable; the spellbook button borrows the complete
  native button state set (normal/hover/pressed/disabled) through the
  validated donor contract, fail-soft; the selected card's plan summary
  no longer shares a rectangle with the new Edit Assignments action.

## What changed in rc2 (unchanged here, for reference)

- Native theme donors resolve from the StaticCanvas (not the planner's
  own overlay); paper reaches every owned surface including nested modal
  frames and rebuilt rows; bounded retry when donors were not yet
  available.
- Spellbook entry: exact-path + tolerant bounded-scan locator (ambiguity
  refuses), corner-anchored caption-fitted readable button, placement
  logged; guarded handoff unchanged; same-page return still deferred.
- Routine-level shared-pool budgets (native now / requested / allocated
  / unmet / projected) from the production planner's own accounting;
  `Assignments & Resources` header caption and selected-spell `Edit
  Assignments` action; readable metamagic names through the Call of the
  Wild display-name contract with item-derived fallback (masks are
  diagnostic-only).

## Exact tests performed

- Source validation 42/42; protocol tests 181/181. The regressions added
  for this preview: spell-scoped notes with unit labels (four-spell
  scarce-rod case with correct per-spell 1/1 and 0/1 funding, communal
  cast wording, already-active labeling), assignment-scoped chooser
  choices/removal/policy captions (pinned-child selection, exhausted-rod
  individual removal with Share surviving, caster-mismatch
  availability, honest REQUIRED/OPTIONAL vs REQUIRED (targeting)),
  bounded summary/note text with full detail preserved on tooltips, and
  the rc2 set (native lookup scope, spellbook locator, authoritative
  4- and 9-request budgets, reorder priority, metamagic naming,
  installed Call of the Wild contract).
- Tooling: Restore-InstallLocal isolated-state tests 4/4; runtime
  harness filesystem 27/27; package validation 4/4; deployment WhatIf
  purity 5/5; guarded publisher gate 3/3; deterministic Release build
  reproduced twice.

## Five-minute check

1. Open the planner (HUD or hotkey): native book/parchment texture, not
   flat panels; native button artwork with hover/pressed states.
2. Open the spellbook: a readable BUFF PLANNER button in the window's
   top-right; click it — the spellbook closes and the planner opens.
3. Configure four *different* spells to each request one use of the same
   three-charge rod in Short. Each spell's chooser row shows its own
   funding (three rows 1/1, one row 0/1) beside the routine line
   "4 requested, 3 allocated, 1 unmet"; the unfunded spell is explicit.
4. In Assignments & Resources, open one spell's per-assignment
   enhancement editor: policy buttons read REQUIRED/OPTIONAL and toggle
   for ordinary rods; Share reads REQUIRED (targeting). Exhaust a
   configured rod (unequip/spend it) and reopen: it stays listed,
   selected, and clicking it removes just that selection.
5. Extended metamagic rods read Persistent/Piercing/Selective/Threnodic
   (plus ordinary Quicken/Extend), never raw numbers.

Actual charge spending at execution is not covered by this check and has
not been verified in-game for this build.

## Installation / rollback

Close the game first. In Unity Mod Manager, install
`KingmakerBuffPlanner-0.1.1-rc3.zip` (it installs over any earlier
0.1.x preview or the 0.0.19 stable). The planner profile lives under
`Mods\KingmakerBuffPlanner\UserSettings\` (one JSON per campaign, with
the pre-schema-5 original archived beside it as `.orig`); migration
happens on first load and replacing the DLL alone does not reverse it.

Rolling back a guarded local installation (the developer's machine
records) uses `scripts/Restore-InstallLocal.ps1 -InstallId <id>`, which
restores the recorded prior version while preserving every profile
edited after the install; for your own machine, simply reinstall the
0.0.19 ZIP from the v0.0.19 release page — your saves and other mods
are never touched, and the UserSettings profiles are plain files you can
delete or keep per campaign.
