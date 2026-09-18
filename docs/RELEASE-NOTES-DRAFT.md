# Kingmaker Buff Planner 0.1.0 — Draft Release Notes

## Precise casting assignments

One catalog entry can now hold several casting assignments. Each child
assignment has its own stable identity, an explicit position in the routine's
casting order, a pinned or automatic caster (with optional spellbook or exact
provider constraints), its own ordered target list, and its own enhancement
selections with a required-by-default policy and an explicit "cast without
this enhancement when unavailable" opt-in. Pins are hard constraints: an
unavailable pinned caster stays visible and unresolved instead of silently
falling back to another caster.

Example: Leinna casts Echolocation on herself unenhanced while Felix casts it
on himself unenhanced and on Tias and Raine through Share Transmutation —
four casts from one catalog entry, with Share charged only for the two
configured non-self casts.

## One authoritative allocation result

The planner now allocates strictly in the explicit assignment order — catalog
sorting, filtering, and reopening cannot change it — and produces one
per-pool accounting line (available now, requested, allocated, unmet, forecast
remaining) with traces back to the assignments that demanded them. The
canonical shortage is reported exactly: nine enhanced casts against three
charges show requested 9 / available 3 / allocated 3 / unmet 6, with the first
three explicit targets funded.

## Casting Order and Resource Usage view

A new Order button in the planner header opens the casting-order surface:
numbered assignments with Earlier/Later controls, resolved caster and pin
status, per-pool resource lines with competing configured demand from the
other routines, and a read-only combined forecast that carries balances
forward across one occurrence of each selected routine in the selected order
with its assumptions stated.

## Explicit partial application

Default Apply — and the HUD quick-run, which shares the same gate — refuses to
run only part of an incomplete routine. The refusal reports requested
coverage, planned casts, already-active skips, and unmet reasons. A dedicated
Apply Ready Casts Only control appears exactly while the routine is incomplete
and is the only way to run the ready subset.

## Spellbook entry (experimental)

An owned Buff Planner button attaches to the native spellbook window and
hands off into the planner through the spellbook's own close affordance with
a bounded wait and rollback. The HUD controls and hotkey are unchanged.
Rendered placement and the return-to-spellbook trip are still being qualified
and may change.

## Schema 5 migration

Profiles migrate losslessly from schema 4: each existing source becomes one
legacy-equivalent automatic assignment with the same targets, enhancements,
effect policy, and former allocation order. The exact pre-migration original
is archived once outside the rotating backup chain, migration is idempotent,
and a failed migration never overwrites the original. Configured intent is
never silently pruned when casters, items, or enhancements become
unavailable; it stays visible, diagnosable, and removable.

## Repairs

- Enhancement and caster-policy chooser overflow: content height now has a
  single measured owner, a native-styled scrollbar is wired, the final option
  is reachable by wheel and drag, and the scroll position survives toggle
  refreshes.
- Theme foundation: parchment/button/text/input/scrollbar/ornament/sound
  capabilities resolve independently with bounded recovery and readable
  fallbacks; full-state native button borrowing only when the donor supplies
  every state.

## Qualification status

Source validation 42/42, protocol tests 159/159, runtime harness filesystem
8/8, package validation 4/4, deployment WhatIf purity 5/5, deterministic
Release build PASS. Save-backed live qualification is BLOCKED: the authorized
`KBP_AUTOMATION_BASELINE`/`KBP_AUTOMATION_WORKING` fixture pair is absent from
this machine. Rendered spellbook placement, live theme qualification, exact
rod-instance identity, and live visual acceptance are explicitly unclaimed.
