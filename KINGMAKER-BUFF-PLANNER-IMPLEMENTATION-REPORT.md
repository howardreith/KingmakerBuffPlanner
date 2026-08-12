# Kingmaker Buff Planner Implementation Report

## 0.0.3 R2 correction

The installed 0.0.2 UI verdict is FAIL by direct human playtesting. Version 0.0.3 replaces cloned native HUD hierarchies with fresh bounded retained-mode buttons, makes visible presentation validation precede the full-screen input lease, and requires expected-effect confirmation for execution success. The installed schema-2 profile was audited read-only and preserves one Long Bless assignment and its target; no migration ran or discarded data. Applicable runtime/package/install gates and the next authoritative human verdict remain pending.

Status: IN PROGRESS — 0.0.2 UI repair implemented; human/campaign UI acceptance and save-backed core remain open

Version 0.0.2 is a standalone .NET Framework 4.7 / C# 7.3 Unity Mod Manager mod for exact Kingmaker 2.1.7b. It replaces the rejected 0.0.1 IMGUI text strip with native-anchored icon controls and an opaque full-screen planner using Kingmaker's full-screen mode plus a scoped input lease and raycast/pointer isolation.

Current verified scope:

- Native catalog: 1,722 abilities; 974 in-scope candidates; 413 included; 561 excluded; 0 unsupported.
- Exact Call of the Wild 1.14.4c-2.1: 7,342 owned abilities; 4,937 candidates; 2,096 included; 0 unsupported; four representative discovery paths; ordered Harmony inventory; two deterministic passes.
- Pure/source gates: validation 19/19; behavior/protocol 56/56; runtime transaction 6/6; deployment WhatIf 5/5; package validation 4/4.
- UI source: one setup plus three quick icons, full-screen planner content, one opaque blocker/raycaster, native mode/selection boundary, idempotent cleanup, and explicit Long instrumentation/result presentation.
- Release: deterministic clean-head local-only ZIP builder, exact package allowlist, install/use guide, draft notes, and guarded feature-branch push policy.

The corrected 0.0.2 no-save native core passed 12/12 twice and Call of the Wild passed 26/26 twice at commit `3bd519b000f3126b19462888aefeabe29374873d`. The old 0.0.1 UI result is not reused. Campaign UI mechanics remain pending because there is no authorized `KBP_` fixture and require human playtesting after local installation.

Unmet core boundary: no project-owned `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair exists. Consequently, live prepared/spontaneous/resource/material spend, effect/duration equivalence, invalid/no-spend, save/reload persistence, and real configured execution cannot be truthfully runtime-qualified. These are retained as core rows in `planning/DEFINITION-OF-DONE-MATRIX.md`, not downgraded to optional.
