# Kingmaker Buff Planner 0.0.10

This is the first owner-authorized GitHub release of Kingmaker Buff Planner for
Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b.

## Highlights

- Discovers native and supported optional-mod buff abilities structurally rather
  than through a fixed spell-name list.
- Provides Long, Important, and Short routines with direct portrait assignment,
  deterministic planning, resource previews, and Animated or Instant execution.
- Consolidates mechanically equivalent provider-backed abilities into one card
  while retaining every eligible caster and resource source for automatic
  provider selection.
- Preserves targeting, spell-slot, resource, duration, material-component, and
  metamagic semantics.
- Supports metamagic rods through a visible enhancement chooser with readable
  effect names, remaining-use information, persisted selections, and explicit
  unavailable states.
- Opens full spell details on right-click and excludes invalid personal-spell
  targets.
- Uses a pooled vertical four-column catalog with real blueprint icons, search,
  category tabs, routine-local **Selected only**, and no horizontal scrolling.
- Shows selected, covered, unavailable, invalid, and neutral portrait states
  with player-facing explanations.
- Provides retained lower-left HUD controls and the configurable Ctrl+Shift+B
  shortcut while isolating planner input from world selection and camera motion.
- Stores profiles externally under the mod's UserSettings directory and creates
  no Kingmaker save dependency.
- Treats Call of the Wild and other gameplay mods as optional, read-only
  compatibility inputs; no third-party mod payload is bundled.

## Installation

1. Download `KingmakerBuffPlanner-0.0.10.zip` from **Assets** below.
2. In Unity Mod Manager, select Pathfinder: Kingmaker.
3. Drag the ZIP into the **Mods** tab.
4. Launch the game and enable **Kingmaker Buff Planner**.
5. Load a campaign and open the planner with Ctrl+Shift+B or the lower-left HUD
   controls.

Do not download GitHub's automatically generated **Source code** archives; they
are not the Unity Mod Manager package.

## Qualification

The repository's deterministic gates have covered source validation,
behavior/protocol tests, runtime-filesystem safety, deployment WhatIf purity,
strict package validation, native discovery, Call of the Wild discovery,
Animated execution, Instant execution, exact Mods restoration, immutable
baseline-save verification, and guarded installation. The repository owner
accepted the current feature set and presentation for release on 2026-08-23.

The current repository is private, so a release remains visible only to GitHub
users authorized for this repository unless repository visibility is changed.

## Release policy

This version is the permanent release identity for the current artifact. Any
later code or presentation change will use a new version instead of replacing
published bytes.
