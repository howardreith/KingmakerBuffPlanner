# AGENTS.md — Kingmaker Buff Planner

## Product boundary

This repository builds one standalone Unity Mod Manager mod:

```text
Product:      Kingmaker Buff Planner
Assembly:     KingmakerBuffPlanner.dll
UMM ID:       KingmakerBuffPlanner
Namespace:    KingmakerBuffPlanner
Framework:    .NET Framework 4.7
Language:     C# 7.3
Game:         Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b
Harmony:      legacy Harmony12 / installed 0Harmony12.dll
```

It must never become part of, or a required dependency of, Tabletop Added Rules, Gunslinger, Call of the Wild, or another gameplay mod.

## Required architecture

Keep these concerns separate:

1. **Game adapters** — exact Kingmaker/UMM/Harmony integration and bounded reflection.
2. **Discovery** — spellbook/ability enumeration and action-graph interpretation.
3. **Domain model** — normalized sources, effects, providers, resource pools, requests, groups, and plans.
4. **Planning** — deterministic allocation and validation; no Unity UI dependencies.
5. **Execution** — animated and instant engines behind an interface.
6. **Persistence** — versioned external JSON keyed by stable IDs.
7. **UI** — view/controller code that delegates to services.
8. **Compatibility** — optional, fail-soft adapters with no compile-time gameplay-mod dependency.
9. **Diagnostics/runtime testing** — structured evidence and guarded scenario execution.

Do not build a single large MonoBehaviour or UI class containing scanning, allocation, persistence, and execution logic.

## Dynamic buff discovery

The primary catalog is structural, not a hardcoded spell-name or GUID list.

Use, after exact local contract verification:

- `AbilityEffectRunAction`;
- `ContextActionApplyBuff`;
- `Conditional`;
- `ContextActionCastSpell`;
- `ContextActionSpawnAreaEffect` plus `AbilityAreaEffectBuff`;
- `ContextActionsOnPet`;
- `ContextActionPartyMembers`;
- `ContextActionEnchantWornItem`;
- sticky-touch and variant normalization;
- bounded reflection for proven `ActionList` wrappers.

Preserve branch semantics. Do not flatten conditional alternatives into an undifferentiated set of effect GUIDs. Use optional adapters and versioned JSON overrides only for proven exceptions.

## Code and test style

- Match established repository conventions once they exist; do not mix styles.
- C# production source must compile under language version 7.3.
- Prefer immutable request/result/domain types where practical.
- Keep Unity and Kingmaker static state behind narrow adapters.
- Use dependency injection through constructors or explicit composition roots, not service locators spread through the codebase.
- Tests assert behavior: discovered catalog entries, planned allocations, consumed resources, saved data, active effects, UI-observable state, and structured reports.
- Prefer realistic fixture graphs and exact assembly-backed reflection tests. Mock only true external boundaries that cannot be exercised locally.
- Every fixed runtime defect receives a regression test or scenario.
- Do not weaken warnings, validators, package allowlists, or assertions to make a gate pass.

## Runtime safety

- Only repository-owned guarded scripts may stage/deploy/launch runtime scenarios.
- The harness must prove source-only validation and `-WhatIf` purity before live use.
- Live `Mods` staging must be transactional, locked, recoverable, and restored exactly.
- Only `KBP_AUTOMATION_WORKING` may be mutable. `KBP_AUTOMATION_BASELINE` is immutable and every other save is protected.
- Stop on unexpected Steam, account, cloud, update, purchase, or credential UI.
- Never claim runtime qualification from compilation, detached reflection, or a main-menu load alone.

## Git and publication

- Work on a dedicated `codex/kingmaker-buff-planner` branch or a clearly named descendant.
- Create coherent reviewable commits throughout the mission.
- Never use destructive history/worktree commands to discard unknown state.
- Publish only through the project-owned guarded push helper after it exists and passes tests.
- A local validated release ZIP and draft release notes are permitted. Public release publication requires separate user authorization.

## Durable records

Maintain at minimum:

```text
planning/KINGMAKER-BUFF-PLANNER-MISSION.md
planning/NATIVE-BUFF-COVERAGE-MATRIX.md
planning/DISCOVERY-ACTION-CONTRACT-INVENTORY.md
planning/RESOURCE-AND-CASTING-CONTRACT-INVENTORY.md
KINGMAKER-BUFF-PLANNER-JOURNAL.md
AUTONOMOUS-RESUME.md
AUTONOMOUS-BLOCKERS.md
docs/ARCHITECTURE.md
docs/IMPLEMENTATION-REPORT.md
docs/QUALIFICATION.md
docs/MANUAL-ACCEPTANCE.md
```

Every checkpoint records branch, exact HEAD, version, commands, exact pass/fail counts, evidence paths, hashes when relevant, rejected theories, uncertainty, and the exact next action.
