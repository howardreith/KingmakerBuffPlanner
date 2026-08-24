# Kingmaker Buff Planner 0.0.11 - Draft Release Notes

Publication status: local qualification build only. Not a public release.

Version 0.0.11 fixes a severe closed-window performance regression. Earlier builds attempted to discover the campaign HUD on every Unity Mod Manager update. When no campaign HUD existed, that attempt called Unity's global `FindObjectOfType<IngameMenuController>()` once per frame. Exact-build profiling measured 18.875 seconds inside those searches during a 20.080-second opening-camera interval and only 11.358 average FPS.

HUD discovery is now invalidation-driven. KBP observes the known `StaticCanvas.HUDController` identity/active state, reacts to real area lifecycle signals, and performs one bounded lookup under that HUD host. Unchanged frames with no HUD do no hierarchy discovery. Two fresh fixed processes averaged 89.236 and 88.671 FPS under the local 90 FPS cap; a later exact package averaged 89.237 FPS with 18 moving-camera samples and zero HUD searches.

Native discovery remains 12/12 and exact Call of the Wild compatibility remains 26/26. The source-only suite passes 32/32 source, 78/78 behavior/protocol, 8/8 filesystem, 4/4 package, and 5/5 deployment dry-run checks.

The current machine has no authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair. Campaign HUD/workflow, animated/instant execution, ordinary-area, cutscene, and world-map checks therefore remain explicitly unqualified rather than using another product's automation save or an ordinary player save.

## 0.0.10 visual clarity

Publication status: local qualification build only. Not a public release.

Version 0.0.10 aligns the lower-left planner row from the native Kingmaker button grid and gives every dark/gold glyph a centered preserve-aspect safe area with measured optical correction. Button dimensions, hitboxes, listeners, tooltips, pointer capture, and quick actions are unchanged.

Target portraits now consume the selected consolidated card's real plan slice. Explicit fulfilled selections are strongly green, unavailable selections amber with an exact reason, additional expected recipients softly green and labeled `COVERED`, invalid targets red with an exact reason, and unrelated valid targets neutral. Selected-buff text reports available uses and planned casts instead of unexplained covered/blocked fractions.

The modal retains its proven top-level canvas and input lease, but no longer adds a CanvasScaler. It uses the native legacy Arial/default UI material path, fixed font sizes, unit transform scales, forced layout, and pixel-snapped owned rectangles. Human visual acceptance remains authoritative.

## 0.0.9 source consolidation

Version 0.0.9 polishes the accepted four-column planner without replacing its workflow. Provider-backed abilities with the same normalized effect semantics now appear as one card while the established planning backend automatically chooses among every eligible caster and resource source. Earlier per-ability routine assignments are rebound and unioned without losing targets.

Direct targets now receive a strong full green portrait treatment; indirect party/area beneficiaries receive a distinct lighter green `COVERED` treatment. The grid uses balanced symmetric margins, and the unchanged lower-left HUD hitboxes receive a shared native-toned backing, neighboring button skin where available, inset antique-gold frames, and improved icon padding.

Discovery, targeting commands, Animated and Instant execution, resource accounting, provider ranking, persistence durability, Ctrl+Shift+B isolation, HUD actions/tooltips/pointer capture, modal lifecycle, optional-mod compatibility, and guarded runtime transactions remain preserved. Cosmetic acceptance remains human-gated.

Highlights:

- Long/Important/Short edit-context tabs and one Apply action;
- pooled vertical four-column grid with actual blueprint icons;
- portrait clicks directly add/remove targets in the active routine;
- Search, All/Spells/Abilities/Other, and routine-local Selected only as the complete normal catalog controls;
- compact selected-buff source, duration, description, targets, and plan summary;
- automatic provider selection with no normal provider-management interface;
- schema-3 migration that reveals hidden entries and replaces F10 with configurable Ctrl+Shift+B;
- exact native B suppression for the planner chord and restored dark/gold HUD treatment;
- hashed campaign views plus physical Animated and Instant qualification;
- no BubbleBuffs/Wrath code, assets, shaders, textures, or hierarchy paths.

Automated qualification includes source 30/30, behavior/protocol 67/67, runtime filesystem 8/8, deployment WhatIf 5/5, physical Animated/Instant, native 12/12, Call of the Wild 26/26, exact Mods restoration, and immutable baseline-save verification. The package remains human-gated for visual acceptance.
