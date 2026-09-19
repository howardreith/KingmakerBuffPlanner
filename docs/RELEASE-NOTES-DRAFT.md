# Kingmaker Buff Planner 0.1.1-rc1 — Testing Preview

**This is a testing preview for the owner's inspection, not a stable release.**
No automated candidate gameplay session has yet completed on this machine:
guarded launches staged and restored the mod correctly, but the automated
environment could not finish loading the disposable campaign save (see
Known Limitations). Source, build, package, and migration gates all pass;
the newest feature work is documented below and exercised by 172 protocol
tests and 27 harness tests through production callers.

## What you can try

- **Precise casting assignments.** One buff can hold several casting
  assignments, each with its own caster pin, explicit targets, and
  enhancements — for example Leinna self-cast, Felix self-cast, and Felix
  casting on Tias and Raine through Share, all from one catalog entry.
  Pins are hard constraints: an unavailable pinned caster stays visible and
  unresolved instead of silently switching casters.
- **Casting Order & Resources.** The header's Order button opens numbered
  assignments with Earlier/Later, per-target remove/move/split, a
  per-assignment target picker, caster cycling, per-assignment enhancement
  selection with required/optional policy, resolved providers/recipients,
  per-pool accounting (available/requested/allocated/unmet/forecast), and
  an explicit combined forecast that carries balances across routines in
  the order you select.
- **Honest partial application.** Apply (and the HUD quick-run) refuse to
  run only part of an incomplete routine and report the gap; an explicit
  "APPLY READY ONLY" control runs the ready subset and reports unmet
  requests.
- **Overflow repair.** Enhancement and caster-policy choosers scroll
  correctly with a visible scrollbar; the last option is reachable and the
  position survives toggles.
- **Spellbook entry (preview).** An owned Buff Planner button appears in
  the spellbook and hands off to the planner through the game's own close
  flow with bounded waits and rollback.

## Exact tests performed

- Source validation 42/42; protocol tests 172/172 (mixed-caster routing,
  shortage accounting 9/3/3/6, child-aware targeting, sibling-safe moves,
  prepared-token forecast consumption, justified-only effect projection,
  caster-identity projection, unsupported-request coverage, review
  acknowledgment orchestration, migration round-trips, unresolvable-request
  protection); runtime harness filesystem tests 27/27 (fixture bootstrap/
  recovery/teardown safety, write-ahead publication windows, WhatIf purity);
  package validation 4/4; deployment WhatIf purity 5/5; guarded publisher
  gate tests 3/3; deterministic Release build reproduced twice.
- Guarded runtime launches staged and restored the live Mods tree exactly;
  save-load automation did not complete (disclosed below).

## Known limitations

- **No automated gameplay acceptance yet.** The disposable campaign save
  references blueprints provided by several installed mods; loading it
  under the harness's minimal mod configuration fails inside the game's
  load routine, and booting the full installed mod set inside the
  automated environment did not finish within the harness windows. Your
  own installation loads the save (you verified this manually). Manual
  testing in your normal setup is therefore the acceptance path for this
  preview.
- **Spellbook return is one-way for now:** closing the planner returns to
  normal gameplay, not to the exact spellbook character/page you opened
  from. Reopening the spellbook manually is required.
- **Rod selection is pooled per caster:** "any matching rod for the
  original caster". The installed game exposes no qualified durable
  identity for one specific physical rod, so choosing between two
  otherwise identical rods is not offered.
- Theme borrows native parchment/button artwork where the running game
  provides it and falls back to readable parchment styling elsewhere;
  fallback styling is not verified native artwork.

## Installation and rollback

Install the ZIP through Unity Mod Manager (Pathfinder: Kingmaker). The
previous release remains available on the releases page; to roll back,
install the older ZIP again. Profiles migrate automatically on first load
and keep an archived copy of the pre-migration file next to the profile;
restoring an older DLL does not un-migrate a profile — use the archived
original if you need the old format back.
