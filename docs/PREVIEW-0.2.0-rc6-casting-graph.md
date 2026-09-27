# Kingmaker Buff Planner — casting-graph private preview

**Status: development preview for the owner's own trial. Not live-qualified
at this exact build yet, not publicly released, not merged.** This is a
private artifact identified below — it is NOT the published `v0.2.0-rc6`
alpha (different commit, hashes and MVID), even though the version string is
the same development convention.

## Exact artifact identity

| Item | Value |
| --- | --- |
| Source commit | `8b387dc6d154dacf8863a8cb00fd9d9009ea17dd` (branch `codex/kingmaker-buff-planner-casting-graph`, draft PR #3) |
| Package | `artifacts/release/0.2.0-rc6/KingmakerBuffPlanner-0.2.0-rc6.zip` |
| Package SHA-256 | `8412ac109ef9343401de817a215e6db7003071bbce7b5f8578f92f625304fea2` |
| DLL SHA-256 | `bfdefd5783cf6fe9d0045b6087f01c0063e7a2138585eced37b16ffaff506c92` |
| Assembly MVID | `b8e3e120-3dae-4fdb-ae13-73d61f711254` |
| Build | deterministic ×2, package validation 4/4, `publicationStatus: local-only` |

## What you are trying

The rebuilt casting-first workspace (addendum v1.1): the buff catalogue on
the left; for the selected buff, casters with their exact sources and what
the whole plan leaves of each pool; one ink line and one chip per planned
casting; targets on the right; the focused casting's inspector with its
per-casting enhancements; two labelled budgets in the footer (this routine
alone, and the whole plan in one pass). Classic mode is still included and
every screen states which planner is active.

## Installing (normal UMM install)

1. Close the game.
2. Copy the zip's `KingmakerBuffPlanner` folder into
   `...\Pathfinder Kingmaker\Mods\` (replacing any older copy), or install
   through Unity Mod Manager as usual.
3. Launch the game; open UMM (`CTRL+U` by default); enable
   `Kingmaker Buff Planner`.

## Entering Casting-first

- The planner keeps a mode setting. In this build Casting-first is selected
  in the mod's UMM settings panel (Planner mode), and the Classic screen
  offers a deliberate in-planner switch into Casting-first; the workspace
  header always states `Planner: Casting-first` (Classic states
  `Planner: Classic`), so a screenshot is always attributable.
- Open the planner with the usual hotkey.

## Authoring one casting

1. Pick a buff in the catalogue (search or the type tabs).
2. Pick a caster; where the caster has several materially different sources,
   pick the exact source row (each row names its pool and what is left after
   the whole plan).
3. Click a legal target: exactly one line and one chip appear, and the chip
   is focused. Clicking a target that already has this buff shows that
   casting — use the explicit Duplicate action for a parallel one.
4. Click the line (wide hit corridor) or the chip: the inspector shows only
   that casting — its caster/source/target, routine and order, cost, and the
   enhancements that apply to its exact caster/source.
5. Add an enhancement (for example Extend) there; the budget lines update
   globally. Undo removes the last edit.
6. Save; close with Escape (first Escape leaves the inspector); reopen or
   reload — the castings, lines and counts reconstruct from the saved plan.

## Rollback

- Disable the mod in UMM, or delete `Mods\KingmakerBuffPlanner`. The saved
  plan lives in the mod's `UserSettings` folder and is not touched by game
  saves. To return to a previous version, restore the old folder (your
  normal installation on this machine — 0.1.1-rc3 — was never modified by
  this work; automated runs stage and restore transactionally).

## Evidence at this build (honest state)

| Area | Status at `8b387dc` |
| --- | --- |
| Source validation / protocol tests | 42/42; 364/364 (new: budget-evidence judging, physical-input DPI contract) |
| Full source-only gate | PASS at this exact HEAD (build, package 4/4, harness 38/38, deploy WhatIf 5/5, launcher WhatIf 12/12, fixture 3/3, Restore-InstallLocal 16/16, publisher 3/3) |
| Graph interaction, live | Passed in-game at earlier commits of this branch (`casting-graph-qual-1200-03`, `-adv-1200-01`: one-casting authoring, 3 castings/2 casters, corridor+chip focus, retarget, Undo, Save, reopen, one root, stable control counts); not yet rerun at this exact build |
| Hover ownership, live | Shipped behaviour clean at 1920×1200 (35 readings, single-owner, aligned, no planner selection); the rc6 ghost did NOT reproduce in the controlled reproduction — the sticky-selection mechanism exists (rc6 takes selection, shipped does not) but the visible ghost remains unexplained; the theory is not proved |
| Cross-buff / shared-enhancement budget proof | Source-proved (exact deltas, atomic refusal); the live scenario phase is new at this build and not yet exercised in game |
| 1920×1080 windowed input | Root-caused to a DPI-space mismatch, repaired (`SetProcessDPIAware`), contract-tested; live re-verification pending |
| Save/reload, live | Not yet rerun on this branch (source-proved; the reload scenario remains to run) |
| Contrast | Palette math: normal ≈8.9:1, hover ≈6.6:1, pressed ≈11.2:1, selected ≈10.0:1, disabled ≈4.5:1 (worst-case bevel); presented-frame pixel measurement and your visual judgement pending |

## Known limitations

- Not live-qualified at this exact commit yet; the table above says exactly
  what is verified where.
- Casting is disabled inside automated/supervised test sessions only; normal
  gameplay casting works in this preview like the rc6 candidate's.
- Share Transmutation execution, exact physical-rod identity, pets and new
  adapters remain outside this preview (unchanged charter boundaries).
