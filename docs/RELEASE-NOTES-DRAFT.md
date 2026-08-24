# Kingmaker Buff Planner 0.0.12

Version 0.0.12 repairs the lower-left Buff Planner controls disappearing during
campaign load while retaining the 0.0.11 performance correction.

## HUD lifecycle repair

Version 0.0.11 correctly stopped the closed planner from searching Unity's
global object graph every rendered frame. Its one-shot invalidation gate could,
however, consume an installation request before the inner Kingmaker HUD was
ready. If a provisional row later expired, an unchanged outer HUD did not
generate another request and the four controls could disappear permanently.

Version 0.0.12 now distinguishes:

- no active campaign HUD;
- temporarily unavailable inner HUD controls;
- a newly created or still-pending candidate;
- a stable installed row;
- candidate expiry; and
- a stale or inactive hosting chain.

Retryable readiness, expiry, and staleness re-arm one scoped attempt after a
30-frame active-HUD settling interval. A live provisional candidate is not
recreated, an unchanged stable installation performs no new discovery, and an
absent campaign HUD performs no discovery. Area unload and mod disable suspend
host-triggered attempts until a later load or enable signal.

Installed and provisional rows are rejected when their owned root is missing,
detached, inactive, or reparented; when the inner controller or native button
cluster is unavailable; when the active outer HUD changes; or when the native
raycaster is inactive. Cleanup destroys only Buff Planner-owned objects.

Placement, left-alignment, glyph-centering, and top-raycast ownership validation
remain intact. `IngameMenuController` discovery remains bounded beneath the
known active `StaticCanvas.HUDController`; the normal HUD path performs no global
`FindObjectOfType` or `Resources.FindObjectsOfTypeAll` search.

## Diagnostics and validation

`[KBP-BOOT]` now records host transitions, dispatches, typed attempt results,
candidate transitions, expiry/staleness reasons, retry counts, outer/inner
object identities, and the last exact validation failure without logging an
identical pending state every frame.

The accepted installed-campaign log shows one candidate staying pending while
Kingmaker's loading screen owned the raycast, then the same candidate installing
with four buttons and four listeners after 57 validation frames. Setup then
passed both presentation phases and opened successfully. The repository owner
accepted the repaired behavior.

Deterministic tests cover retryable readiness, exact 120-frame candidate expiry,
same-host recreation, every required hosting-chain liveness predicate, stable
absent/installed/provisional frames, bounded temporary-readiness attempts, area
and host transitions, unload/load, disable/re-enable, and hotkey invalidation.

The complete source-only suite passes 34/34 source checks, 91/91 behavior and
protocol tests, 8/8 runtime-filesystem tests, 4/4 package checks, and 5/5
deployment-WhatIf checks. Two deterministic release builds reproduce the same
ZIP and DLL.

The guarded performance probe passes at 88.780 average FPS with all 18
moving-camera samples at 90.710-90.896 FPS, zero global HUD searches, zero
absent-HUD install dispatches, and exact restoration. Native and Call of the
Wild catalog/Harmony runs pass 12/12 and 26/26 respectively, with zero Buff
Planner Harmony overlap.
