# 0.0.11 Runtime Performance Regression Root Cause

Date: 2026-08-23  
Branch: `codex/kbp-performance-regression-0.0.11`  
Starting commit: `c06793d2238577093b96a2dc3172839070e7d69a`  
Production-fix commit: `f23a07b7560b2aa4cd7b3d1635436c3abffd575a`

## Incident statement

Released 0.0.10 performs a global Unity object search from KBP's Unity Mod Manager update callback whenever the campaign HUD button root is absent. Main-menu camera animation, world-map presentation, cutscenes, and area transitions commonly have no active campaign HUD. KBP therefore repeats the global search every rendered frame even though the planner is closed.

This is the demonstrated cause of the severe KBP-specific slowdown. It is not an NVIDIA device-selection problem, a CPU-affinity problem, a frame-cap change, a time-scale change, provider/catalog discovery, or a planner-window rendering problem.

## Starting state and evidence intake

- The work began on clean `main` at `c06793d2238577093b96a2dc3172839070e7d69a`, version 0.0.10. A stat-only status anomaly on `planning/KINGMAKER-BUFF-PLANNER-MISSION.md` had identical index and working-tree SHA-256 `1f6197b2...` and no diff; refreshing the index restored a clean status without changing content.
- Work moved to the dedicated branch `codex/kbp-performance-regression-0.0.11`.
- The user's isolated A/B is accepted as the incident reproduction: KBP disabled is smooth at the configured 60 FPS; KBP enabled is approximately 16-17 FPS during the opening camera approach and similarly juddery in cutscenes/world-map motion; ordinary gameplay is generally fine; the settled menu returns to 60 FPS on the user's machine.
- Exact files named `KBP-bad-output_log.txt` and `KBP-good-output_log.txt` were searched for in the repository, the lab root, and local user roots. Neither file is present. No paired-log comparison is claimed.
- The current ordinary `output_log.txt` belonged to a different no-KBP launch and was not used as the incident A/B.
- Exact local runtime identities used by the guarded probe are Kingmaker 2.1.7 (`Kingmaker.exe` SHA-256 `94a779c5423199fcb0470bd89884a3b3875dee2072eb1a7b1d7bc8e67accb1a1`), UMM 0.32.4.0 (`1387468bc3af41c50fe51859a3bb7af4922891aa8f13a6187e7a348ceaabfd88`), and Harmony 1.2.0.1 (`aa1cd48317254985d8b700cc74953477d1b40c3022ce9aa4c95ed2b8327e1292`). The harness contract was updated to these exact installed UMM version/hash values; no allowlist or identity assertion was relaxed.

## Continuous-path inventory

The static audit followed every production path reachable while the mod is enabled and the planner is closed.

| Path | Always-on behavior | Finding |
| --- | --- | --- |
| `Main.OnUpdate` / `OnUpdateCore` | Runs each UMM frame, polls the hotkey, ensures the retained root, and calls `BuffPlannerUiRoot.TickOwned` | Entry to the defect |
| `BuffPlannerUiRoot.Tick` | Ticks the closed screen controller and HUD controller | Before the fix, unconditionally called `_hud.TryInstall()` |
| `BuffPlannerHudButtonController.TryInstall` | Locates the native formation-button host when no KBP HUD root exists | Before the fix, called global `UnityEngine.Object.FindObjectOfType<IngameMenuController>()` on every attempt |
| `PlannerPointerOwnership` | Harmony prefix on `PointerController.Tick` and postfix on `CameraRig.GetCameraScrollShiftByMouse` | Menu probe recorded zero calls; regions are empty while planner UI is absent |
| `PlannerHotkey` | Harmony prefix on exact `KeyboardAccess.Binding.InputMatched()` | Called each frame in the menu but measured negligible relative cost; not causal |
| `Main.OnGUI` | Draws the UMM diagnostics panel | UMM-controlled and not the closed-window update path |
| Full-screen `Resources.FindObjectsOfTypeAll` calls | Duplicate/root diagnostics during full-screen construction and runtime evidence | Not reached continuously while the screen is closed |
| Provider/catalog/profile/resource work | Refresh and execution orchestration | Reached on screen open, explicit refresh, or quick execution; not reached by the failing closed-menu callback |
| Execution coroutines | Animated/instant execution enumerators | Bounded to explicit routine execution; no idle infinite coroutine exists |
| Logging and exception handling | First-update/lifecycle/status logging | No per-frame exception or logging flood in the demonstrated path |

The audit found no production assignment to `Application.targetFrameRate`, `QualitySettings.vSyncCount`, `Time.timeScale`, `Time.fixedDeltaTime`, `Time.maximumDeltaTime`, `Time.captureFramerate`, animator update modes, camera timing fields, or Kingmaker time-controller state. The diagnostic probe reads these values only. They stayed at target 90, vSync 0, time scale 1, fixed delta 0.02, maximum delta 0.04, and capture framerate 0 throughout the local runs.

## Causal profiling

Commit `e077082643af63aa4ed29503eda7ae44604ae4c8` added a strictly opt-in `performance-probe` runtime scenario and aggregate `Stopwatch` counters. Commit `f45d04f341007d988f64de8d8a809fd53144ebbc` corrected the probe clock to begin at the first actual UMM update instead of `Main.Load`. The first pre-correction probe is retained as rejected evidence because its 49.7-second load interval left only one measured frame.

Ordinary launches pay only disabled guard branches. Enabled probes emit one aggregate sample per approximately one second and never log each invocation.

The decisive runs used the same diagnostic DLL and package:

- Commit `f45d04f341007d988f64de8d8a809fd53144ebbc`
- DLL SHA-256 `1365a927ed8db8a1ab3170e6632079cc2cd29db5e31791f09b3757dd972daa44`
- Package SHA-256 `b93fe1b5b9ca928baaeae135bba6a669c60bdf0cf4556358b3a276ca5342637e`

| Run | Change | Frames / elapsed | FPS min / average / max | HUD global searches | Search time | Root time |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| `perf-0.0.11-unfixed-hud-on-2` | Normal unfixed path | 228 / 20.080 s | 7.511 / 11.358 / 11.838 | 228 | 18,874.614 ms, max 95.441 ms | 18,879.089 ms |
| `perf-0.0.11-unfixed-hud-off-1` | Same DLL; suppress only HUD discovery | 1,787 / 20.026 s | 60.680 / 89.234 / 90.908 | 0 | 0 ms | 5.590 ms |

The unfixed global search averaged approximately 82.78 ms per frame and consumed approximately 94% of the entire wall-clock interval. `Main.OnUpdate` consumed 18,982.417 ms. In the same-DLL suppression run it consumed 45.108 ms total. The pointer and camera Harmony callbacks recorded zero menu invocations. The hotkey prefix recorded only 14.494 ms total in the bad run and 63.661 ms across 1,787 calls in the fast suppression run.

This exact-build, single-subsystem suppression is causal bisection rather than correlation.

Profiles and hashes:

- Normal unfixed: `runtime-evidence/perf-0.0.11-unfixed-hud-on-2/performance-profile.json`, SHA-256 `5305c2de164e9253086a7109392e898d594e8cbbf086428c54d4d305092a22c0`.
- Same DLL, discovery suppressed: `runtime-evidence/perf-0.0.11-unfixed-hud-off-1/performance-profile.json`, SHA-256 `bdad5630a374924ff64928745972650bca2a083df238eba28cfdfbcac2753c19`.
- Every runtime transaction finished `Restored` with `restorationVerified=true`.

## Why motion-heavy states exposed it

KBP contains no camera-motion conditional. The relationship is indirect:

1. The campaign HUD is normally present and cached in ordinary gameplay. `TryInstall()` then returns immediately from the cached root.
2. Opening-book animation, world map, cutscenes, and transitions commonly omit, disable, or reconstruct the campaign HUD.
3. In those states the old root called `TryInstall()` each frame, and its global active-object search repeatedly failed.
4. A global Unity search competes with scene traversal, animation, transform/canvas updates, and rendering preparation on the main thread. Low overall CPU/GPU utilization is expected when one serialized main-thread operation dominates each frame.

The user's return to 60 FPS after the menu settled is consistent with a scene-lifecycle/object-graph effect, but the code does not explicitly detect camera settlement. On this test machine the unfixed search remained expensive even in the final settled samples. Therefore the proven statement is that HUD absence plus the repeated global search causes the frame cost; camera movement itself does not.

## Architectural correction

Production commit `f23a07b7560b2aa4cd7b3d1635436c3abffd575a` makes the smallest boundary-preserving change:

- `HudInstallInvalidationGate` retains a dirty request while no active HUD host exists.
- `BuffPlannerUiRoot.Tick` observes only the known `StaticCanvas.Instance.HUDController` identity and active state. It dispatches discovery once when the host appears/activates, once after a real lifecycle invalidation, or once after enable/hotkey invalidation. Repeated unchanged frames do not redispatch.
- Area unloading cancels pending installation and disposes owned UI. Area-loaded/scenes-loaded/loading-complete/activated callbacks request one installation. Host replacement or reactivation also requests one.
- `BuffPlannerHudButtonController.TryInstall` no longer searches the global Unity object graph. It resolves `IngameMenuController` only below the exact active `UISectionHUDController` with `GetComponentInChildren(..., true)`.
- Existing candidate construction, two-frame presentation readiness, hitbox/listener validation, HUD ticking, setup/open actions, input ownership, and disposal are unchanged.

This is invalidation and bounded discovery, not an FPS-dependent delay or arbitrary periodic throttle.

## Deterministic regression contract

`hud-install-discovery-is-invalidated-not-frame-polled` proves behavior across simulated frame sequences:

- 240 unchanged frames with no HUD produce zero discovery dispatches while retaining the pending request;
- host appearance produces exactly one dispatch;
- 240 unchanged active-host frames produce no repeat;
- an explicit lifecycle request produces exactly one new dispatch;
- inactive-to-active and host-replacement transitions each produce exactly one new dispatch.

The behavior suite now passes 78/78.

## Fixed runtime evidence

Two fresh-process fixed runs at the production-fix commit used normal discovery, not the suppression flag:

| Run | Frames / elapsed | Moving / settled samples | FPS min / average / max | HUD searches | Root time |
| --- | ---: | ---: | ---: | ---: | ---: |
| `perf-0.0.11-fixed-hud-on-1` | 1,786 / 20.014 s | 18 / 2 | 59.579 / 89.236 / 90.901 | 0 | 5.690 ms |
| `perf-0.0.11-fixed-hud-on-2` | 1,775 / 20.018 s | 18 / 2 | 50.739 / 88.671 / 90.935 | 0 | 4.892 ms |

The later exact checkpoint run `perf-0.0.11-exact-hud-on-1` also used normal discovery and passed 1,786 frames over 20.014 seconds, 18 moving samples, 59.559 / 89.237 / 90.991 FPS, zero HUD searches, and 5.685 ms total root time. Its profile SHA-256 is `cd5276a85afe13370a16d5236eeedafef4fd3eeeff58f9accffe6ceb145971c8`.

The local game is configured for 90 FPS, not 60. Reaching approximately 89-91 FPS throughout camera motion demonstrates that KBP no longer imposes the prior approximately 11 FPS ceiling. The user's 60 FPS environment should therefore remain at its configured cap, subject to ordinary loading transients.

## Functional and compatibility evidence

- Source-only suite: source validation 32/32, protocol/domain behavior 78/78, runtime-harness filesystem 8/8, package 4/4, deployment WhatIf purity 5/5, aggregate 1/1.
- Native catalog run `perf-fix-0.0.11-exact-native-1`: 12/12; 1,722 abilities, 974 candidates, 952 detected effects, zero scanner exceptions, zero KBP Harmony overlap; exact restoration.
- Call of the Wild run `perf-fix-0.0.11-exact-cotw-2`: 26/26; 9,064 abilities, 5,907 candidates, 2,096 optional inclusions, zero optional unsupported candidates, zero KBP Harmony overlap; exact restoration.

## Explicit qualification boundary

The current Windows profile has no save whose header is exactly `KBP_AUTOMATION_BASELINE` or `KBP_AUTOMATION_WORKING`. The repository-owned harness correctly rejects the save-backed scenario with `Disposable save ambiguity: baseline=0; working=0.` It does not select the unrelated KMG/BODYGUARD automation saves or ordinary campaign saves.

Consequently this checkout cannot freshly qualify campaign HUD installation, setup/full-screen interaction, profile round trip, animated/instant Bless execution, ordinary-area FPS, a cutscene, or world-map movement. Those behaviors passed the pre-incident 0.0.10 campaign suite, but that evidence is not relabeled as 0.0.11 evidence. The performance fix is runtime-qualified against the opening-camera reproduction and source/optional discovery; the listed campaign rows remain blocked pending an authorized KBP save pair.

No live install, merge, push, public release, or protected-branch action was performed.

## Final local artifact checkpoint

The deterministic local-only artifact was built twice from clean source commit `d3af9be4e62ab8aa796e29343ab30b75e918fb8c`:

- Version: 0.0.11
- ZIP: `artifacts/release/0.0.11/KingmakerBuffPlanner-0.0.11.zip`
- ZIP SHA-256: `eac7dd50afdb8b68f9d3a6577eb7fff9863883b966df8404fed67d623d407d34`
- DLL SHA-256: `1bde124702d013c7f66b159963c01a23eef691104ffa5523f9e944498238c4e7`
- Assembly MVID: `cab52ff1-e758-4f8f-ba92-5ad3cd4eb867`
- Release builder: 3/3 with two byte-identical builds; final package validation 4/4.
- Final clean-head source suite: 32/32 source, 78/78 behavior/protocol, 8/8 filesystem, 4/4 package, 5/5 deployment WhatIf, aggregate 1/1.

Run `perf-0.0.11-final-release-1` used that exact artifact and is retained as a threshold failure: its first non-moving startup/loading bucket was 39.945 FPS, while all 18 moving-camera buckets were 90.695-90.966 FPS, HUD search count was zero, and root work was 8.337 ms over the complete run. Restoration was verified. No threshold or validator was changed.

Unchanged exact-artifact repetition `perf-0.0.11-final-release-2` passed 9/9: 1,783 frames over 20.020 seconds; 18 moving and two settled samples; 56.687 / 89.061 / 90.873 minimum/average/maximum FPS; zero HUD searches; 5.868 ms total root work; profile SHA-256 `d3eae40417e57b08d844d84cd42825ad6d1018eab7c136075ecd70ea66daf144`; result SHA-256 `b3a83e472aac9246bb3354ee41f6efc03c8c2f527b1f1157fc53a8750a87293e`; exact restoration verified.
