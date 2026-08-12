# Kingmaker Buff Planner 0.0.3 — Draft Release Notes

Publication status: local qualification build only. Not a public release.

Kingmaker Buff Planner is a standalone Unity Mod Manager mod for Pathfinder: Kingmaker 2.1.7b. It dynamically discovers beneficial abilities, builds deterministic resource-aware Long/Important/Short routines, stores versioned per-campaign profiles outside saves, and provides animated and instant native-semantics execution engines.

Highlights:

- fresh retained-mode Setup/Long/Important/Short HUD buttons with top-raycast ownership checks, ordered in one row above the native cluster;
- presentation-first transactional modal opening: the opaque full-screen root must render and validate before gameplay input is leased;
- effect-confirmed execution outcomes; queued/submitted commands are never reported as applied;
- structural native and optional-mod buff discovery with branch-preserving effect expressions;
- prepared, spontaneous, special-slot, resource, material, mass, provider-priority, ban, and cap planning;
- native-styled lower-left setup plus Long/Important/Short quick-action icons;
- a distinct fully opaque full-screen planner with scoped native full-screen input isolation;
- HUD-visible quick-action results, including explicit empty-routine feedback;
- animated execution plus bounded instant execution with safe sticky-touch fallback;
- exact Call of the Wild 1.14.4c-2.1 discovery qualification without a compile-time dependency;
- transactional runtime harness, exact restoration, protected-save policy, and deterministic package validation.

Known qualification boundary: no project-owned `KBP_` save was available on the qualification desktop. The exact campaign-only UI gate therefore returned `BLOCKED`; HUD/modal/input/Bless behavior requires the mandatory human retest and is not claimed as passed. Save-backed provider/resource/effect/executor equivalence and save/reload interaction remain `DEFER — EVIDENCED`. Tabletop Added Rules was unavailable locally.
