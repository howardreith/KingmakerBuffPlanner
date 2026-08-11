# Kingmaker Buff Planner Implementation Report

Status: IN PROGRESS — final-head verification/publication pending; save-backed core is `DEFER — EVIDENCED`

Version 0.0.1 is a standalone .NET Framework 4.7 / C# 7.3 Unity Mod Manager mod for exact Kingmaker 2.1.7b. The repository owns its assembly, UMM identity, namespace, structural discovery, immutable domain, deterministic planner/resource ledger, versioned external profiles, animated and instant executors, F10 setup/HUD UI, compatibility profiles, diagnostics, guarded runtime harness, packaging, and documentation.

Current verified scope:

- Native catalog: 1,722 abilities; 974 in-scope candidates; 413 included; 561 excluded; 0 unsupported.
- Exact Call of the Wild 1.14.4c-2.1: 7,342 owned abilities; 4,937 candidates; 2,096 included; 0 unsupported; four representative discovery paths; ordered Harmony inventory; two deterministic passes.
- Pure/source gates: validation 15/15; behavior/protocol 52/52; runtime transaction 6/6; deployment WhatIf 5/5; package validation 4/4.
- UI: complete setup/routine controls, three layout profiles, singleton/reconstruction lifecycle, zero blocker/subscription diagnostics, repeated no-save runtime proof.
- Release: deterministic clean-head local-only ZIP builder, exact package allowlist, install/use guide, draft notes, and guarded feature-branch push policy.

The composed Phase 12 no-save core passed 22/22 twice at commit `3af45f3329df300dc0616da9393480abee8547ce`. Exact evidence is in `docs/QUALIFICATION.md`; detailed design and phase history are in `docs/IMPLEMENTATION-REPORT.md`.

Unmet core boundary: no project-owned `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair exists. Consequently, live prepared/spontaneous/resource/material spend, effect/duration equivalence, invalid/no-spend, save/reload persistence, and real configured execution cannot be truthfully runtime-qualified. These are retained as core rows in `planning/DEFINITION-OF-DONE-MATRIX.md`, not downgraded to optional.
