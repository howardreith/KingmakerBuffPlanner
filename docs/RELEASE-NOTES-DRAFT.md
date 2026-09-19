# Kingmaker Buff Planner 0.1.1-rc2 — Corrected Testing Preview

**This is a corrected testing preview for the owner's inspection, not a
stable release.** It repairs the three failures you observed in the
installed 0.1.1-rc1: the missing native appearance, the missing spellbook
button, and the enhancement chooser that looked like one rod could enhance
unlimited spells.

**What is and is not verified:** all repairs below are exercised by
deterministic source tests through production callers (178 protocol tests,
including six new regressions that reproduce the released failures), plus
offline reflection against your installed game and Call of the Wild
assemblies. A guarded in-game run of this exact build was attempted but
blocked because a Kingmaker session was already running on the machine, so
**rendered appearance, the visible spellbook button, and in-game budget
displays have not yet been observed in a live session by the developer**.
The five-minute check at the end settles those in your installation.
Native charge spending is a separate lane and remains untested in-game.

## What changed since 0.1.1-rc1

- **Native theme lookup repaired.** The planner previously searched for
  native book/button/font donors starting from its own overlay, so every
  donor lookup failed and the flat fallback panels stayed on screen.
  Donors now resolve from the native StaticCanvas while artwork applies
  only to planner-owned controls. Paper now reaches the full owned
  surface tree — main frame, full-screen backdrop, enhancement chooser,
  caster policy, assignments/resources editor, target picker, and
  description modal — including rebuilt rows, with fallback tints and
  outlines yielding to the borrowed book artwork while deliberate status
  colors stay. If native windows were not built yet when the planner
  opened, a bounded retry now picks the donors up later instead of
  staying flat forever.
- **Spellbook button repaired.** The entry is discovered through the
  proven exact window path with a bounded tolerant fallback (ambiguous
  candidates refuse rather than guess). The button no longer sizes
  itself as a sliver of the native window: it is corner-anchored with an
  explicit readable size, caption-fitted, styled from the live native
  canvas, and logs its exact on-screen rectangle when attached. Clicking
  keeps the existing guarded handoff (native close, bounded wait,
  rollback on failure). Returning to the same spellbook page after
  closing remains a disclosed deferred refinement.
- **Enhancement budgets where you select them.** The normal enhancement
  chooser, the selected-spell card, and the assignment editor now show
  the authoritative plan's shared-pool budget for the active routine:
  native charges now, uses requested, casts allocated, unmet demand, and
  projected remaining, with owner and affected spells/targets named.
  Each row distinguishes current allocations elsewhere from what
  selecting it would reserve, and per-assignment rows show that
  assignment's own coverage. Reordering assignments moves the scarce
  charge to the new higher-priority cast immediately. The chooser
  explains that editing a plan never consumes charges and that native
  charge counts stay game-owned. There is no second charge counter —
  every number comes from the production planner that execution uses.
- **The hidden editor is discoverable.** The header's `Order` button is
  now captioned `Assignments & Resources` and fits its full label; the
  selected spell also gains a direct `Edit Assignments` action into the
  same editor.
- **Readable metamagic names.** Provider-extended metamagic (Persistent,
  Piercing, Selective, Threnodic from Call of the Wild) now displays
  meaningful names through a fail-soft provider contract verified against
  your installed assembly, with the rod item's own name as fallback. Raw
  numeric masks like `524288 Spell` no longer reach the interface; they
  appear only in the diagnostic log.

## Exact tests performed

- Source validation 42/42; protocol tests 178/178 — the six new
  regressions cover: native donor lookup scope (owned-overlay resolution
  must fail, native-canvas resolution must succeed, stale checks
  discriminate the two scopes), the spellbook locator (exact/tolerant/
  ambiguity-refused), chooser budgets from the authoritative plan (the
  four-request and nine-request scarce-rod cases: native 3, requested
  4→allocated 3/unmet 1 and 9→3/6, projected 0, assignment coverage and
  reorder priority), and metamagic naming (provider contract, item
  fallback, no digit ever reaches effect text, installed Call of the Wild
  value/name pairs exact). Runtime harness filesystem tests 27/27;
  package validation 4/4; deployment WhatIf purity 5/5; guarded publisher
  gate tests 3/3; deterministic Release build reproduced twice.
- Offline contract probes against the installed game: `Metamagic` enum
  values and the Call of the Wild `MetamagicExtender` constants
  (Persistent=0x10000000, Piercing=0x80000, Selective=0x2000000,
  ThrenodicSpell=0x2000 — the exact values rendered as numbers in your
  rc1 screenshot).

## Five-minute check

1. Open the planner (HUD or hotkey): the main window should show the
   native book/parchment texture, not flat salmon panels; buttons should
   use native gray artwork with hover/pressed states.
2. Open the spellbook: a readable BUFF PLANNER button should appear in
   the window's top-right; click it — the spellbook closes and the
   planner opens.
3. Select a spell with a three-charge rod owner and open the enhancement
   chooser: pick the rod for four requested casts. The chooser should
   show native now 3, requested 4, allocated 3, unmet 1, projected 0 and
   name the affected casts; the selected card should carry the shortage.
4. Swap the two assignments' order in Assignments & Resources: the
   funded/unmet casts should swap accordingly.
5. Extended metamagic rods should read Persistent/Piercing/Selective/
   Threnodic (plus ordinary Quicken/Extend), never raw numbers.

Actual charge spending at execution is not covered by this check and has
not been re-verified in-game for this build.

## Installation / rollback

Close the game first. In Unity Mod Manager, install
`KingmakerBuffPlanner-0.1.1-rc2.zip` (it installs over 0.1.1-rc1 or over
the 0.0.19 stable if that is what your Mods folder currently holds). The
planner profile lives under `Mods\KingmakerBuffPlanner\UserSettings\`
(one JSON per campaign, with the pre-schema-5 original archived beside it
as `.orig`); migrating forward happens on first load, and replacing the
DLL alone does not reverse it. To roll back to 0.0.19 later: close the
game, reinstall the 0.0.19 ZIP from the v0.0.19 release page, and delete
or restore the archived `UserSettings` profile files for campaigns you
want the old version to rebuild. Your saves are never touched.
