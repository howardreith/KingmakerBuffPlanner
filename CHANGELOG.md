# Changelog

## 0.0.4 — 2026-08-12 (recovery in progress)

- Moved the F10 fallback into the UMM update callback so HUD construction cannot disable hotkey diagnostics.
- Added structured `[KBP-BOOT]` load/toggle/update/EventBus/scene/area/HUD/modal/F10 diagnostics, full exception stacks, and a UMM bootstrap snapshot.
- Deferred HUD and modal raycast-ownership validation across Unity frames and kept transient failures retryable without acquiring gameplay input.
- Added an exact disposable-save live-campaign scenario that delivers a physical F10 key, exercises 20 open/close cycles, and checks object uniqueness and save hashes.

## 0.0.3 — 2026-08-12

Human verdict: **FAILED** — UMM showed the assembly active, but the loaded campaign had no Setup/Long/Important/Short row and F10 produced no visible planner. Its earlier no-save and synthetic results did not qualify live campaign initialization.

- Replaced cloned native HUD objects with four fresh retained-mode buttons in the exact Setup/Long/Important/Short row, anchored above the native cluster with top-raycast ownership validation.
- Made full-screen opening presentation-first and transactional; an invisible/zero-size/non-raycast root now aborts before input acquisition, and every later validation failure rolls back.
- Replaced “fired” success accounting with queued/submitted/started/spent/effect-confirmed terminal outcomes. Missing expected facts now report `TimedOutUnconfirmed` and fail the routine.
- Audited the installed schema-2 profile: the Long Bless assignment and target were preserved without migration; the next execution records the automatically resolved provider and exact Bless fact result.
- Strengthened deterministic and guarded runtime contracts to reject HUD overlap/click-through, invisible modal state, input-before-presentation ordering, and unconfirmed casts.

## 0.0.2 — 2026-08-12

- Replaced the rejected floating IMGUI text strip with one native-anchored setup icon and Long, Important, and Short quick-action icons.
- Added an opaque, proportional, full-screen planner with catalog filters, routine tabs, buff details, party/pet portraits, provider/resource controls, plan summary, results, and settings.
- Added scoped Kingmaker full-screen mode and selection isolation plus a raycasting canvas and pointer consumption for click, drag, scroll, and cancel.
- Added explicit quick-action feedback and stage diagnostics; empty Long now reports `No Long buffs are configured.`
- Corrected the automated UI gate so the main menu cannot certify campaign UI behavior.
- Added a guarded 0.0.1-to-0.0.2 local replacement installer with planner-only backup, profile preservation, non-planner identity verification, and rollback.

## 0.0.1 — 2026-08-11

- Initial standalone development build. Its UI acceptance result was subsequently invalidated by human playtesting and must not be treated as current evidence.
