# Live UI Bootstrap 0.0.3 Root-Cause Record

Date: 2026-08-12  
Branch: `codex/kingmaker-buff-planner`  
Forensic HEAD: `d069fffb788147de3c76f2bd0d752f7b2db20f3d`  
Released source commit: `d5a20aa7ddbb2ec7d131a4bed44f1ca65ecaaa65`

This record is the required read-only checkpoint before repair. Direct live-campaign evidence takes precedence over earlier synthetic UI claims.

## Exact intake identities

- Human capture: `C:\Dev\KingmakerBuffPlannerLab\incoming\ui-bootstrap-0.0.3-failure\diagnostics-20260812T111644855Z`
- Installed and release DLL SHA-256: `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950`
- Installed and release DLL MVID: `f3f691a4-d691-4112-90a4-7beb9f06aad2`
- Package SHA-256: `42f823d6b8454ffe4497f4f652752a07d50738d5990c5a5243d091ba92d363e0`
- Installed `Info.json`: version `0.0.3`, UMM `0.28.2`, entry point `KingmakerBuffPlanner.Main.Load`
- The installed DLL, release manifest DLL, and release artifact DLL are byte-identical. The released UI types are present and correspond to the current production source at the release commit.
- No Kingmaker process and no unresolved deployment transaction or lock existed at capture time.

## Bootstrap evidence table

| Stage | Actual campaign evidence | Verdict |
|---|---|---|
| UMM discovers assembly | UMM screenshot shows Kingmaker Buff Planner 0.0.3 active; log starts the mod load | Occurred |
| `Main.Load` enters and returns true | Log contains `Loaded Kingmaker Buff Planner 0.0.3 commit=d5a20aa...`; UMM reports successful load | Occurred |
| Callback assignment | Exact source and installed IL assign `OnToggle`, `OnUnload`, and `OnUpdate` before returning | Occurred |
| UMM startup toggle | UMM 0.28.2 IL calls `ModEntry.Active = true`; the setter invokes `OnToggle(entry, true)` before logging Active | Occurred |
| `OnToggle(true)` | Log contains `Enabled.` followed by UMM `Active.` | Occurred |
| `OnUpdate` | UMM 0.28.2 invokes it each frame only for active mods. A later F10-driven screen attempt can only originate from the retained UI root created by `OnUpdate` | Occurred |
| Harmony bootstrap | Production source applies no Harmony patches; none are required for the 0.0.3 polling path | Not applicable, but previously unreported |
| EventBus / scene-area subscription | Production source contains no persistent EventBus, area, or Unity scene lifecycle observer | Missing resilience path |
| Root/controller construction | `BuffPlannerUiRoot.Ensure` creates a `DontDestroyOnLoad` object held in static `_instance`; the live screen attempt used its controller/session | Occurred |
| Controller retention | Static `_instance` strongly retains root and controllers | Occurred |
| F10 polling | Campaign log contains profile refresh and presentation validation immediately following the reported F10 attempt; those calls are reachable from the root's F10 branch | Occurred, but uninstrumented and perceived as no-op |
| Campaign UI readiness | `StaticCanvas` and EventSystem existed. The modal root was active, 1280x720, fully opaque, interactive, and had a GraphicRaycaster | Geometry/render prerequisites occurred |
| Full-screen raycast readiness | Phase-A validation reported `center-raycast-not-owned`, `ownsCenter=False`, and an empty top hit in the same frame as graphic creation | Failed |
| Modal input lease | The lease is acquired only after phase-A validation; failure occurred first | Correctly not acquired |
| HUD creation/readiness | HUD uses the same create, force-canvas-update, immediate `EventSystem.RaycastAll`, destroy-on-failure sequence. Its failure path emits no exact diagnostic | Earliest HUD failure is the same premature readiness gate; exact rejected predicate was hidden |
| Retry behavior | HUD retries by reconstruction every update, but immediately destroys the candidate again; modal destroys and rolls back per F10 press | Ineffective retry |
| Exception visibility | `ModLog.Error` records only type and message, omitting stack trace | Insufficient |

## Earliest failed stage

The common live failure is not assembly loading, UMM callback registration, root retention, or absent F10 polling. It is **same-frame presentation-readiness validation immediately after Unity graphics are constructed**.

The real campaign log proves that the full-screen candidate had correct nonzero geometry, active hierarchy, opaque blocker, GraphicRaycaster, and EventSystem, but the EventSystem hit list had not yet registered the new graphic. `BuffPlannerScreenController.Open` treated that transient state as permanent, destroyed the candidate, rolled back, and returned false. `BuffPlannerHudButtonController.TryInstall` performs the same immediate hit-ownership test and silently destroys its new four-button row. This explains the simultaneous absence of all four controls and a visible F10 result without inventing a common loader failure.

`Canvas.ForceUpdateCanvases()` updates layout/canvases but does not prove that a newly added `Graphic` is already present in the EventSystem's raycast registry during that same Unity frame. The implementation made that unproven timing assumption in both paths.

## First repaired-campaign diagnostic

Guarded run `bootstrap-0.0.4-human-live-1` loaded the exact Working campaign with the four-mod human-reproduction profile and independently confirmed every loader/lifecycle stage: `Main.Load`, `OnToggle(true)`, UMM `OnUpdate`, F10 arming, retained controller construction, EventBus subscription, five scene/area signals, and the normal exact-save action.

After the same-frame readiness defect was removed, this run exposed the next and now-earliest HUD-specific failure. The new root was parented under `StaticCanvas/HUDLayout/Menu_Buttons48px`, but it was also treated as a child by that native layout. The layout therefore overrode the requested above-cluster anchored position. Deferred validation recorded `rootBottom=-579.0969` and `clusterTop=-534.0093`, expired each failed candidate, and retried 77 times while F10 remained armed. The bounded correction is to mark only the KBP row's root `LayoutElement.ignoreLayout = true`; its own horizontal child layout remains active.

Evidence is preserved under `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\bootstrap-0.0.4-human-live-1`; its Mods transaction is `Restored` with exact verification. This diagnostic is not a UI qualification pass.

## Rejected theories

- Wrong or stale installed DLL: rejected by exact SHA-256 and MVID agreement.
- `Main.Load` did not execute or returned false: rejected by the mod log and UMM success state.
- UMM did not invoke `OnToggle(true)`: rejected by exact UMM 0.28.2 IL and `Enabled.`/`Active.` ordering.
- `OnUpdate` never ran or root was collected: rejected by the F10-originated session refresh and live presentation record; `_instance` is a strong static reference.
- F10 was gated behind successful HUD installation: rejected by the source call graph. However, polling inside the UI MonoBehaviour made the independence implicit and poorly observable, so the repair will put it explicitly in the UMM update path.
- The defect is button position, opacity, or modal styling: rejected. The failed gate precedes any usable retained UI and the human evidence shows total absence.
- An invisible modal/input lock remained: rejected by both human evidence and the phase-A-before-lease state machine.

## Bounded repair

1. Keep one strongly retained root, but poll and log F10 from `Main.OnUpdate` before any HUD work.
2. Add release-safe `[KBP-BOOT]` lifecycle diagnostics, full exception text, and a bounded UMM diagnostics snapshot.
3. Subscribe one lifecycle observer to EventBus area/scene callbacks and Unity scene loading; every signal requests the same idempotent installation path.
4. Let newly created HUD and full-screen candidates survive for at least two Unity update frames before raycast-ownership validation.
5. Keep candidates noninteractive until validation. On failure, log the exact predicate, dispose without acquiring input, and remain retryable with bounded backoff.
6. Acquire the modal input lease only after deferred visible-presentation validation succeeds, and validate again afterward.
7. Add behavior/source regressions and qualify only in two fresh processes loading the exact authorized `KBP_AUTOMATION_WORKING` save while proving the immutable baseline hash.
