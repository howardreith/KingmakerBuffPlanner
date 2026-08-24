# Kingmaker Buff Planner 0.0.12 — Draft

Version 0.0.12 repairs the lower-left Buff Planner controls disappearing during
campaign load while retaining the 0.0.11 performance correction.

This is a local release candidate only. It has not been merged to `main`, tagged,
or published. Install or publication requires separate human authorization.

## HUD lifecycle repair

Version 0.0.11 correctly stopped the closed planner from searching Unity's global
object graph every rendered frame. Its one-shot invalidation gate, however,
consumed an installation request before it knew whether the inner Kingmaker HUD
was ready or whether a provisional Buff Planner row still needed validation. If
that row expired after 120 failed validation frames, the unchanged outer HUD did
not generate another request and the four controls could disappear permanently.

The 0.0.12 coordinator now distinguishes:

- no active campaign HUD;
- temporarily unavailable inner HUD controls;
- a newly created or still-pending candidate;
- a stable installed row;
- candidate expiry; and
- a stale or inactive hosting chain.

Retryable readiness, expiry, and staleness re-arm one scoped attempt after a
30-frame active-HUD settling interval. A live provisional candidate is not
recreated, an unchanged stable installation performs no new discovery, and an
absent campaign HUD performs no discovery at all. Area unload and mod disable
suspend host-triggered attempts until a later load or enable signal.

Installed and provisional rows are rejected when their owned root is missing,
detached, inactive, or reparented; when the inner `IngameMenuController` or native
button cluster is missing or inactive; when the active outer HUD is unavailable;
when the row no longer belongs to that HUD hierarchy; or when its native
raycaster is inactive. Cleanup destroys only Buff Planner-owned objects.

Placement, left-alignment, glyph-centering, and top-raycast ownership validation
remain intact. `IngameMenuController` discovery remains bounded beneath the known
active `StaticCanvas.HUDController`; global `FindObjectOfType` and
`Resources.FindObjectsOfTypeAll` searches are not used by the normal HUD path.

## Diagnostics and tests

`[KBP-BOOT]` now records host transitions, dispatches, typed attempt results,
candidate transitions, expiry/staleness reasons, retry counts, outer/inner object
identities, and the last exact validation failure without logging identical
pending state every frame. The UMM snapshot distinguishes no HUD, retry pending,
candidate pending, installed, candidate expired, stale anchor, and suspended.

Deterministic tests cover retryable readiness, exact 120-frame candidate expiry,
same-host recreation, every required hosting-chain liveness predicate, stable
absent/installed/provisional frames, bounded temporary-readiness attempts, area
and host transitions, unload/load, disable/re-enable, and hotkey invalidation.

Final mechanical, deterministic-build, package, guarded-runtime, artifact hash,
and MVID results will be recorded in the qualification documents after the exact
clean candidate is built. Save-backed campaign claims remain conditional on the
repository-authorized `KBP_AUTOMATION_BASELINE` and `KBP_AUTOMATION_WORKING`
fixtures; no unrelated save may be substituted.
