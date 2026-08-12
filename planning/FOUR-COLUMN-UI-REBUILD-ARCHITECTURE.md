# Four-Column Planner Replacement Architecture

Status: implementation authorized; baseline preserved; human visual acceptance remains required

## Starting checkpoint

- Branch created from `c2bc534827997fb75c1839a5cee5d1342a860369` on 2026-08-12.
- Starting version: `0.0.7`.
- Qualified release source: `2f125f9f1024692d83a1b2570209d1858d62eff1`.
- Package SHA-256: `9feed6dffa668812ed826c75b743d72892e6e8371b0f81585fb557aea8fcf453`.
- DLL SHA-256: `bf8c72874377d56f91bcdb6daedaa8b28b340a948aee06583a32954d61b38927`.
- MVID: `966b7d8f-bd5f-46b9-beda-62774f82ccac`.
- Backup: `artifacts/release-candidate-backups/ui-grid-rebuild-start-0.0.7-c2bc534`.
- Installed 0.0.7 DLL and MVID match the qualified release exactly.
- Intake state: Kingmaker and UMM closed; deployment lock absent; unresolved transactions zero.
- Unchanged baseline: source validation 30/30, protocol 63/63, runtime-harness filesystem 8/8, package 4/4, deployment WhatIf 5/5, aggregate source-only 1/1.

## Reference findings

BubbleBuffs commit `f4871f763a23251284422ef0945a85e9f3fb788e` is an MIT-licensed design reference. The useful ideas are its broad grid, stable widget reuse, blueprint icon/name binding, selected-buff portrait editing, readiness summaries, and visually distinct target coverage. No source or asset is copied. Wrath hierarchy paths and types are not used.

The human screenshots establish three concrete decisions: keep parchment/burgundy/antique-gold for the planner; replace the narrow-list/large-technical-detail composition; and restore the pre-parchment HUD treatment of dark tiles with gold glyphs. Git history shows the HUD glyph generator is already the preferred gold implementation. The regression came from routing HUD buttons through the later parchment `CreateButton` surface; the repair will specialize only the HUD background/tint while retaining anchors, rectangles, hit ownership, listeners, tooltip lifecycle, and scene lifecycle.

## Retain/delete map

Retain unchanged:

- `PlannerUiSession`: discovery, profile loading/saving, plan preview, Animated/Instant execution, confirmed-effect reporting.
- `PlannerSetupModel` provider/resource and target-legality queries, with presentation commands narrowed as described below.
- `BuffPlannerScreenController`: two-phase presentation validation and modal input lease.
- `BuffPlannerUiRoot`, `BuffPlannerQuickExecuteController`, pointer ownership, HUD layout/input/lifecycle, guarded runtime harness, compatibility adapters.
- Stable source/provider/unit identities and existing profile assignments/preferences.

Replace in production:

- The complete `BuffPlannerScreenView` composition: narrow catalog, always-visible technical details, advanced filter drawer, hiding controls, casting-source disclosure, provider controls, separate add-to-routine action, refresh-heavy footer, and duplicate mode affordances.
- The old `CatalogFilterState` semantics for configured-anywhere, duration, availability, and hidden filtering.
- The legacy F10 polling/default and F10-specific diagnostics.

Retire after the new screen is active:

- `ToggleRoutine`, `ToggleHidden`, provider-priority/cap UI callbacks, advanced casting listeners, `Configured only`, `Show hidden`, `Hide`, and old layout hierarchy names/listeners.
- Legacy hidden values remain deserializable only long enough for the versioned migration to clear them; production filtering ignores them.

## Replacement components and dependency direction

```text
PlannerScreenView
  -> PlannerRoutineTabsView
  -> PlannerCategoryTabsView
  -> BuffGridView -> BuffCardPool -> BuffCardView
  -> PlannerSelectedBuffView -> PlannerTargetStripView
  -> PlannerSettingsView
  -> PlannerScreenViewModel -> PlannerSetupModel / PlannerUiSession commands
```

Views render and dispatch callbacks. `PlannerScreenViewModel`, `BuffCardViewModel`, and `TargetPortraitViewModel` format deterministic presentation state only. Direct portrait toggles call one model command that creates the active-routine assignment on the first selected target, removes the target on a second click, and removes the assignment when the final target is cleared. Each command persists atomically through the existing repository callback. Provider selection remains automatic and invisible.

## Grid contract

The grid is a vertical `ScrollRect` with one pooled row per four cards. It has no horizontal movement. Layout calculation always reports four columns at 1920x1080 and keeps four bounded cells at 1600x900. A card pool rebinds existing cards and row containers on dataset refresh; it does not instantiate per frame. Selection and normalized scroll position are retained when the selected key survives a harmless refresh.

The only dataset controls are Search, All, Spells, Abilities, Other, and Selected only. Selected only means at least one wanted target for the active routine. Results are ordered by player-facing name, then spell level, then stable source ID.

## Persistence and hotkey migration

Schema version 3 clears legacy `hiddenSourceIds` while preserving every routine assignment, wanted target, provider preference, execution option, campaign identity, and UI scale. The retained legacy field serializes as an empty list for backward DTO stability but is ignored by production presentation.

The UI binding is a parsed configurable chord. Blank and legacy `F10` values migrate to `Ctrl+Shift+B`. Polling requires both modifiers and consumes the B key through a narrow Harmony input prefix while the exact chord is down, preventing native B handling. F10 is neither polled nor armed by default. The HUD tooltip and Settings show the effective binding.

## Qualification boundary

Automation must prove structure, four-column geometry, filter semantics, direct assignment, migration, input consumption, HUD treatment without geometry/listener drift, pooling/reopen stability, frozen mechanics, exact package identity, and guarded restoration. Runtime screenshots are evidence for the human gate; they do not authorize a claim of cosmetic completion.
