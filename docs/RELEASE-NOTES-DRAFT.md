# Kingmaker Buff Planner 0.0.12 — Draft

Version 0.0.12 repairs the lower-left Buff Planner controls disappearing during
campaign load while retaining the 0.0.11 performance correction.

This is a local release candidate only. It has not been merged to `main`, tagged,
or published. Guarded local installation for human testing was separately
authorized and completed; publication still requires separate authorization.

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

Exact source `083dfbfcf651d44bb01b302ccbbabac823e236e9` passes the complete
source-only suite and two deterministic release builds. The local-only ZIP is
`eabb4785be75129cbc6cffcab030afc9e4afac32957fb7b82af2c16b0e0ac72a`; the DLL
is `6db3693e0ba38b3672bd0eac36b06df6e5965de29a3e78f9b97513c6accfe9e1`; its
MVID is `920b3246-e7f7-4818-92f4-e54294ef2db0`.

The guarded performance run passes at 88.780 average FPS, with all 18 moving
samples at 90.710-90.896 FPS, zero global HUD searches, zero absent-HUD install
dispatches, and exact restoration. Native and Call of the Wild no-save runs pass
12/12 and 26/26. A repeat retained the unchanged 50 FPS threshold and rejected
its 49.810 FPS startup bucket; all moving samples remained at least 88.940 FPS.

The repository-authorized save resolver found zero `KBP_AUTOMATION_BASELINE`
and zero `KBP_AUTOMATION_WORKING` fixtures. Campaign button survival, clicks,
hotkey interaction, transition/cutscene/world-map rows, and installed-state log
evidence remain explicitly blocked; no unrelated save was substituted. This
candidate is guarded-installed for human testing, but is not merged, tagged, or
published.
