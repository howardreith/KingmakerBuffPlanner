# Autonomous Resume

## Production run 2 duplicate-gate checkpoint — 2026-08-12

- Run `row-render-0.0.6-production-2` (`877c618`, package `674733b8...`, DLL `642d4df9...`) has 71/71 named assertions PASS, confirmed Bless `1/1/1/1/1`, and production screenshot SHA-256 `cb2343683ebc4d3dfbb066de4b030c1745c518063354a6357a331a6d53d75c19`, but overall status FAIL because a duplicated aggregate condition still required the spent Bless row to remain visible.
- Transaction is restored/verified. The worktree replaces that duplicate with pre-cast Bless row/material evidence and the captured five-row/selection/no-canary/screenshot contract.
- Exact next command: commit, rebuild local 0.0.6, run `row-render-0.0.6-production-3`, then a second fresh-process PASS.

## Canary-free production run 1 checkpoint — 2026-08-12

- Source/package/DLL: `3a4b503` / `9591b25c...` / `78263b64...`. Run `row-render-0.0.6-production-1` restored exactly but overall status is FAIL because one stale final-state assertion required a spent prepared Bless to remain in the available-only list.
- Production visual evidence itself passed: screenshot SHA-256 `d3f271a09152663e1db0bd650706e903c9fd69b2a297433d4b7d819dc78712e2`; ten readable rows; Bless title/details; canary absent; five row pixel ranges 157–184 and title range 125.
- Exact Bless evidence is `require=False,item=none,count=1,hasEnough=False,consumableRequired=False`. Corrected execution outcome is confirmed success with planned/submitted/started/confirmed/spent `1/1/1/1/1`; post-cast spellbook Bless is correctly unavailable as `resource pool exhausted`.
- Worktree changes only the runtime assertion to accept post-cast catalog availability while adding explicit pre-cast screenshot and Bless material-contract assertions. Local source-only suite remains source 30/30, behavior 62/62, harness 7/7, package 4/4, deployment WhatIf 5/5.
- Exact next command: commit, rebuild `0.0.6`, and run `row-render-0.0.6-production-2`.

## Live row production qualification checkpoint — 2026-08-12

- Status: IN PROGRESS. The A/B canary has proved and rechecked the shared viewport-stencil root cause. `row-render-0.0.6-canary-fixed-1` passed 69/69 and restored exactly; visible screenshot SHA-256 is `71a6bbf6ddafd3c5eb25903c223d4052e9a8b63bb5e0e07651c5d18bf12496dd`.
- Current committed HEAD before this worktree checkpoint is `ac5c384af29e4540c2e5eb07a13b54d37add37b2`. Worktree removes the canary, enforces actual requested row/detail heights, records production renderer/screenshot evidence in the runtime result, adds an independent screenshot pixel-contrast gate, and corrects material validation to short-circuit unless `RequireMaterialComponent` is true.
- Exact local result: source 30/30, protocol/behavior 62/62, runtime filesystem 7/7, package 4/4, deployment WhatIf 5/5, source-only 1/1; production build SHA-256 `731489e683b50d78b1f82c74f54ce812608d6463623ffc2f961f394915aa6ea1` (interim build from pre-checkpoint HEAD).
- External state: no retained canary package is installed; the canary run transaction is `Restored` with `restorationVerified=true`; immutable baseline remains `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`.
- Exact next command: commit this checkpoint, run `./scripts/Build-Local.ps1`, then guarded `live-ui-bootstrap` with run ID `row-render-0.0.6-production-1`.

## Live row rendering recovery intake — 2026-08-12

- Status: IN PROGRESS. Human evidence invalidates 0.0.5 row/details visibility while preserving HUD icons, tooltips, pointer isolation, F10, opaque modal, and close/input/HUD restoration.
- Branch/HEAD: `codex/kingmaker-buff-planner` / `94cbca8810d908d320eec0a2ca89533c7d4e0e05`; source/installed version remains 0.0.5 at intake. Worktree contains the user-supplied live-row mission and the independent intake record.
- Exact installed identity: package `3eba3158aa92a6b66e249ec35aa297500eb4c5decdf73974c26992219922349c`; DLL `6999284085bd6898f6bd871900783f6f81343a6f801b2d2c95acd208c6513b56`; MVID `d2fed415-bfa2-47a7-90ba-f50fa8d1c7de`.
- Earliest unproven stage: both blank panes share `ScrollRect/Viewport/Mask/Content`; no prior CanvasRenderer, stencil/clip, or pixel evidence exists. Internal active/geometry/intersection values are invalidated as visibility proof.
- External state: Kingmaker/UMM closed; latest runtime transactions restored and verified; no top-level lock; installed 0.0.5 unchanged.
- Exact next command: implement the temporary same-Content render canary plus screenshot/render-chain diagnostics, build an exact diagnostic package, and run one guarded `live-ui-bootstrap` pass against `KBP_AUTOMATION_WORKING`.

## Live row canary root cause — 2026-08-12

- Diagnostic commit/package: `a5e551f4899f3680fc9aa5fbd59283704a8ef121`; package `ad8b26bcc6b7622a1e5002f102e2d33b8d1bc98db5ebecb1a44911a841e64007`; DLL `a3bc89b6be2cdfb7256dd0c9aadddddf6df154865a340606f62fa8561a1e2b5f`.
- Guarded run `row-render-0.0.6-canary-3` passed its existing mechanics and restored exactly. Screenshot `planner-render-canary.png` SHA-256 `4b3f7e05a47d830831582c1d2ff0e99ad14fbdeff51f6b42325784b31a08d886` visibly contains neither canary nor rows/details.
- Exact cause: viewport `Mask` uses `UI/Default` with `AlphaClip=True`, but code sets its source Image alpha to `0.001`; masked children use stencil `Comp:Equal` and therefore render no pixels despite non-culled alpha-1 CanvasRenderers. Both blank panes share this contract.
- External state: canary run and the earlier permission-denied transaction are both restored/verified; installed 0.0.5 remains exact; no process or deployment lock.
- Exact next command: retain the canary temporarily, make the hidden mask source opaque, rebuild, and repeat the guarded canary screenshot to prove rows/details pixels before removing the canary.

## Catalog/HUD R3 complete and installed — 2026-08-12

- Status: COMPLETE for automated acceptance; installed 0.0.5 is ready for authoritative human visual retest. Branch `codex/kingmaker-buff-planner`; qualified release source `390bb8b5f514a38edf1c553962813e29a1b526fd`; documentation-only checkpoint follows it.
- Release identity: package `3eba3158aa92a6b66e249ec35aa297500eb4c5decdf73974c26992219922349c`; DLL `6999284085bd6898f6bd871900783f6f81343a6f801b2d2c95acd208c6513b56`; MVID `d2fed415-bfa2-47a7-90ba-f50fa8d1c7de`; CLR version 0.0.5.0; deterministic builds 2/2; local-only.
- Exact final campaign runs: `catalog-input-0.0.5-five-second-physical-1` and `catalog-input-0.0.5-five-second-physical-2`, 69/69 each. Both show 11 total entries, 10 available view models, 10 instantiated/active rows, 5 viewport-visible rows after final layout, selected details bound, and a visible/active/available spellbook Bless row.
- Physical input evidence is `0/0/0/0/0/True/True/0` for player/movement/ability/selection/target deltas, unchanged selection/camera, and native activation. Tooltip held continuously for 5,010 ms across 344 frames with one new enter, four listeners, zero raycast graphics, `blocksRaycasts=false`, and in-screen bounds.
- Empty Long/Important/Short outcomes are explicit. Configured Bless produced exact refusal `FailedValidation ... material-component-unavailable`, with planned 1, submitted 0, confirmed 0; no success was claimed.
- Native run `catalog-input-0.0.5-five-second-native` passed 12/12; Call of the Wild `catalog-input-0.0.5-five-second-cotw` passed 26/26. All Mods restorations verified. Immutable baseline remains `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`; only Working changed through permitted game loads.
- Install `catalog-input-0.0.5-five-second-install` is `Installed`; external settings preserved and every non-planner mod unchanged. No Kingmaker process or unresolved transaction remains.
- Exact next action: human retest installed 0.0.5 using the checklist in `docs/MANUAL-ACCEPTANCE.md`; do not rebuild, merge, or publish this qualified binary.

## Catalog/HUD R3 physical qualification checkpoint — 2026-08-12

- Current HEAD `122e560` on `codex/kingmaker-buff-planner`; version 0.0.5. `catalog-input-0.0.5-live-diag-2` passed/restored and proved 10 live active rows, 4 viewport-visible rows, bound details, and visible/prepared/available Bless, but used the prior synthetic HUD click gate.
- The committed harness now sends OS cursor hover/click events to exact native-canvas HUD centers, monitors tooltip stability for 60 frames, probes world/selection/camera/native control effects, exercises all four HUD controls, selects/configures the live spellbook Bless row, records the confirmed or exact configured outcome, and runs 20 rebuild cycles.
- No process, active lock, or unrestored transaction exists. Installed 0.0.4 remains unchanged.
- Exact next action: build the clean 0.0.5 package, then run `./scripts/Invoke-KingmakerRuntimeTest.ps1 -Scenario live-ui-bootstrap -CompatibilityProfileId human-reproduction -RunId catalog-input-0.0.5-physical-1 -TimeoutSeconds 420 -Confirm:$false`.

## Catalog/HUD R3 first live correction — 2026-08-12

- Current implementation HEAD is `157b1d8`; the first 0.0.5 run `catalog-input-0.0.5-live-diag-1` failed in `Main.Load` because exact `get_InGui` has no `PropertyInfo` metadata row. The direct `GetMethod` correction and callback-first fail-soft ordering pass all local gates.
- Transaction `catalog-input-0.0.5-live-diag-1` is restored and verified after terminating only its owned PID 4148. No Kingmaker process, active lock, or unrestored state remains.
- Exact next action: commit these evidence records, rebuild the clean 0.0.5 package from that commit, and run fresh `catalog-input-0.0.5-live-diag-2`.

## Catalog/HUD input/tooltip R3 implementation checkpoint — 2026-08-12

- Status: IN PROGRESS. Root cause evidence is committed at `f99f0b1`; the first repair implementation is committed at `06ac7177d0b63e4f77ea7e25347c035bc4342cf5` on `codex/kingmaker-buff-planner`; version is `0.0.5`.
- Implemented: end-to-end catalog/filter/layout diagnostics, explicit nonblank catalog states, manual scroll-content sizing, selection/details binding checks, generic provider availability, Bless vertical-slice logging, one cached non-layout/non-raycast/clamped tooltip, independently refreshed quick actions with exact refusal/result logs, and a Harmony12 postfix limited to `PointerController.InGui` while the pointer is inside registered planner rectangles.
- Local gates: source `27/27`, protocol `60/60`, runtime-harness filesystem `7/7`, package `4/4`, deployment WhatIf `5/5`, source-only suite `1/1`; `git diff --check` has no errors.
- External state remains unchanged: Kingmaker/UMM closed; installed 0.0.4 remains exact; no deployment lock or unresolved transaction; immutable baseline remains protected.
- Exact next action: commit this checkpoint record, build the clean 0.0.5 package, then run `./scripts/Invoke-KingmakerRuntimeTest.ps1 -Scenario live-ui-bootstrap -CompatibilityProfileId human-reproduction -RunId catalog-input-0.0.5-live-diag-1 -Confirm:$false` to obtain real-campaign catalog geometry and Bless evidence before strengthening the physical mouse scenario.

## Catalog/HUD input/tooltip R3 intake — 2026-08-12

- Status: IN PROGRESS. Human verdict partially passes 0.0.4 bootstrap/presentation but blocks release on blank catalog/details, tooltip flicker/width, HUD click-to-move, and unhelpful quick actions.
- Starting branch/HEAD: `codex/kingmaker-buff-planner` / `d2aecb00e02a63b5ea33976e34ff1eefc9765a1a`; current version `0.0.4`; user mission plus intake/root-cause records are the intended worktree changes.
- Exact installed/release identity: source `5b96f3b4e713489ce677db3ac5acb83a10f80f01`; package `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`; DLL `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`; MVID `305a8a6c-2b49-4e3b-a365-286638cbfafa`.
- Earliest catalog failure: after the 11-source normalized model and source/detail bind loops, at unmeasured content/row geometry or clipping. Tooltip and physical-input causes are exact in `planning/CATALOG-HUD-INPUT-TOOLTIP-ROOT-CAUSE.md`.
- Runtime/external state: Kingmaker/UMM closed; no active/non-restored transaction or lock; installed 0.0.4 and protected human profile unchanged.
- Exact next action: add catalog trace and layout evidence, explicit scroll content sizing, single non-layout tooltip, conditional `PointerController.InGui` planner-region capture, ordered quick flow, and physical-input live scenario coverage.

## Live UI bootstrap recovery complete — 2026-08-12

- Status: COMPLETE for the authoritative recovery mission; 0.0.4 is guarded-installed for human visual confirmation. No merge or public release occurred.
- Release source: `5b96f3b4e713489ce677db3ac5acb83a10f80f01`; deterministic package `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`; DLL `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`; MVID `305a8a6c-2b49-4e3b-a365-286638cbfafa`.
- Real-campaign PASS twice: `bootstrap-0.0.4-human-live-6` and `bootstrap-0.0.4-human-live-7`, 65/65 each. Both prove one active Setup/Long/Important/Short row, F10 armed and physically observed once, visible planner root, 21 balanced open/close cycles, no duplicates, no native/world click-through, restored mode/selection, exact Working load, and immutable baseline `afca8ac5...`.
- Exact HUD object paths/instance IDs/active states/screen centers/world corners are in each `runtime-result.json` under `uiHudObjectEvidence`. Result hashes: run 6 `cd1d22813a7f6a3131b9074b851e13d5a8a2feba7965c8fcc609274d81a05378`; run 7 `4ad411a5536ec25f1fba99138db18ea6fbb5279134d7c2f771e3c46551d6f918`.
- Release regressions: native `bootstrap-0.0.4-release-native-regression` 12/12; Call of the Wild `bootstrap-0.0.4-release-cotw-regression` 26/26; exact restoration for both.
- Guarded install `bootstrap-0.0.4-local-install`: `Installed`; installed DLL/MVID exact; settings preserved; other mods verified unchanged; no Kingmaker process or deployment lock.
- Final records commit is documentation-only and follows the exact installed release-source commit above. No further command is required for this recovery mission.

## Live UI bootstrap campaign checkpoint — 2026-08-12 12:08Z

- End-to-end diagnostic `bootstrap-0.0.4-human-live-5` proved the requested behavior: 1 UI root, 4 HUD buttons/listeners in Setup/Long/Important/Short order, row above native cluster, owned hitboxes, physical F10 armed/observed once, 21 open cycles, 21 balanced closes, opaque 100% planner coverage, zero duplicate roots after close, zero native activation/world input, and exact Working load. Its result is still FAIL because the test captured mode/selection after F10 opened the planner and expected Cheat Menu's primary DLL while UMM loaded its exact `.cache` file. Pre-F10 baseline capture and distinct exact loaded-assembly hash are now fixed; UI behavior itself needs fresh repetition before PASS.
- Third campaign diagnostic: `bootstrap-0.0.4-human-live-4` confirmed correct screen-space hit coordinates, but UMM 0.28.2 `Params.xml` has `ShowOnStart=1` and its still-open `UMM blocking UI/Image` owned every button center. The live harness now writes an exact blocker marker, physically sends Escape to the foreground Kingmaker window, waits for HUD ownership, and only then physically sends F10. Production remains retryable and F10 remains independently armed. Run 4 restored exactly; evidence output-log SHA-256 `93a50f87cad38d89d7ce75aec4a5d3801e69a0da163092c339236cd813c1faff`.
- Second campaign diagnostic: `bootstrap-0.0.4-human-live-3` atomically failed and restored exactly. The out-of-layout row now passes the above-cluster predicate, but hit validation used world center `(-845.1,-505.2)` as `PointerEventData.position`. The current repair converts every HUD button world center through the native raycaster event camera into screen coordinates for both validation and runtime click dispatch. Evidence: `runtime-evidence/bootstrap-0.0.4-human-live-3`; output-log SHA-256 `0a1c31985ba1d27012778537895021b09df4c34aa4c720bcbdee54dfaf1d713a`.
- Transaction recovery update: the first attempt to start `bootstrap-0.0.4-human-live-2` failed before game launch while moving staged Mods into Program Files. Its automatic restore misclassified a bound null PID as a running process. Read-only audit proved live Mods absent, the exact original parked at the owned backup, no process, and matching lock/token; the guarded restore then returned `Restored`, `restorationVerified=true`, exact manifest equality, no lock, and no staging residue. The null-PID filter and regression are now implemented.
- Status: the exact human-reproduction campaign now loads successfully under the guarded harness. The first real-campaign 0.0.4 attempt is a diagnostic failure, not qualification.
- Branch/HEAD before the current repair: `codex/kingmaker-buff-planner` at `db48cd50fb809b7606b6f46b76b4e9f178840c8e`; the three current source edits are intentionally uncommitted until their gates pass.
- Run `bootstrap-0.0.4-human-live-1` proved `Main.Load`, `OnToggle(true)`, first `OnUpdate`, independent F10 arming, retained controller construction, EventBus subscription, five scene/area lifecycle callbacks, exact Working save selection, and campaign load.
- Earliest live failure: the HUD candidate participated in the native `Menu_Buttons48px` layout, which overrode its above-cluster anchor. Exact predicate: `row-not-above-native-cluster:rootBottom=-579.0969;clusterTop=-534.0093`; 77 bounded install attempts remained retryable while F10 stayed armed.
- Current repair: set the HUD root `LayoutElement.ignoreLayout = true`; latch live-scenario exceptions and atomically write one failure result instead of throwing on every UMM update.
- Diagnostic evidence: `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\bootstrap-0.0.4-human-live-1`; transaction status `Restored`, `restorationVerified=true`; full output log SHA-256 `afd2513c3667802b05ea035ded08d2cab3313bffe8fc9213d032e1745bfb11db`.
- Save safety: baseline remains immutable at `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`; mutable Working is now `75519ac954f8cf7a010366af24c03666ae911fb29d57902c1a1c7b0f7cd75414` after the permitted load/write.
- Current local gates: source 24/24, protocol/behavior 60/60, filesystem harness 6/6, package 4/4, deployment WhatIf 5/5, build 1/1.
- Exact next action: commit the out-of-layout HUD repair, rebuild the clean commit, and run `bootstrap-0.0.4-human-live-2` from a fresh process.

## Live UI bootstrap recovery checkpoint — 2026-08-12

- Status: 0.0.3 is human-failed (`UMM active, but live campaign HUD controls absent and F10 unregistered/nonfunctional` from the user's perspective); 0.0.4 repair is source-complete and awaiting clean-commit live qualification.
- Branch/intake: `codex/kingmaker-buff-planner`; forensic HEAD `d069fffb788147de3c76f2bd0d752f7b2db20f3d`; release source `d5a20aa7ddbb2ec7d131a4bed44f1ca65ecaaa65`.
- Exact failed identity: installed/release DLL `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950`, MVID `f3f691a4-d691-4112-90a4-7beb9f06aad2`, package `42f823d6b8454ffe4497f4f652752a07d50738d5990c5a5243d091ba92d363e0`.
- Root cause: both retained UI paths synchronously demanded EventSystem top-hit ownership in their graphics-creation frame. The live modal was active, opaque, 1280x720, and raycaster-backed, but its same-frame hit list was empty. The screen rolled back and the HUD silently destroyed its row through the identical timing assumption.
- Rejected loader theories: logs and UMM 0.28.2 IL prove `Main.Load`, callback assignment, `OnToggle(true)`, `OnUpdate`, retained controller construction, and the F10-originated screen attempt occurred. Production applies zero Harmony patches; 0.0.3 had no persistent scene/area observer.
- Repair: `[KBP-BOOT]` lifecycle/exception diagnostics; F10 polling directly in `Main.OnUpdate`; UMM diagnostics panel; EventBus scene/area observer; two-frame readiness gates; retryable exact HUD/modal failures; one retained/disposed controller; save-backed `live-ui-bootstrap` scenario with physical F10 delivery.
- Authorized saves proven present: baseline `Manual_296_KBP_AUTOMATION_BASELINE.zks` SHA-256 `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`; working `Manual_297_KBP_AUTOMATION_WORKING.zks` SHA-256 `961c4721d31de5740416ae3c864e63351f6916cf61a0b4327094701f5579e1b2`; game ID `3d556254-8ba9-4e9f-8d11-755eecd0b661`.
- Current gates: source validation 23/23, behavior/protocol 59/59, runtime harness 6/6, package 4/4, deployment WhatIf 5/5. No live claim yet.
- Exact next command after checkpoint commit and clean build: `./scripts/Invoke-KingmakerRuntimeTest.ps1 -Scenario live-ui-bootstrap -CompatibilityProfileId native-only -RunId bootstrap-0.0.4-native-live-1 -Confirm:$false`.

## R2 installed handoff — 2026-08-12

- Status: validated 0.0.3 is guarded-installed for authoritative human campaign retest; automated campaign UI and save-backed execution are not claimed.
- Branch/release source: `codex/kingmaker-buff-planner`; release commit `d5a20aa7ddbb2ec7d131a4bed44f1ca65ecaaa65`; evidence-record checkpoint `fc94060a861db6356a5bdb8d2520f377ec52b0c5` was guarded-pushed and remote-verified.
- Release/install identity: package `42f823d6b8454ffe4497f4f652752a07d50738d5990c5a5243d091ba92d363e0`; DLL `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950`; MVID `f3f691a4-d691-4112-90a4-7beb9f06aad2`; installed version `0.0.3`.
- Gates: source 21/21, behavior 57/57, harness 6/6, package 4/4, deploy WhatIf 5/5, install WhatIf 5/5; native runs `r2-0.0.3-release-native-1/2` 12/12; Call of the Wild `r2-0.0.3-release-cotw-1/2` 26/26; all restoration exact.
- Campaign UI boundary: `r2-0.0.3-release-ui-boundary` is correctly `BLOCKED` at `campaign-ui-unavailable`; no authorized `KBP_` save exists. This is not a UI/input/Bless pass.
- Install state: `r2-0.0.3-local-install` is `Installed`; other mods verified unchanged, settings preserved, profile SHA-256 still `3723e3181c56bff6427a15b2ba85ffd76fd40e98f3f482253b15910f038d6b48`; no Kingmaker/UMM process or deployment lock.
- Persisted Long: Bless ability `90e59f4a4ada87243b7b3535a06d0638`, target `8d7086b2-a4d5-43d5-aed6-51c789971b53`, expected fact `87b8c6270ea85c743afc734dfe99afee`; no prior provider preference or migration. The next live run supplies the actual provider/outcome diagnostics.
- Exact next action: run the mandatory campaign checklist in `docs/MANUAL-ACCEPTANCE.md` against installed 0.0.3; record the human verdict and exact provider/Bless outcome. Do not run automated save-backed qualification unless an authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair exists.

## R2 correction checkpoint — 2026-08-12

- Status: R2 CORRECTION IN PROGRESS; installed 0.0.2 failed direct human playtesting.
- Active mission: `planning/HUD-MODAL-EXECUTION-R2-CORRECTION-MISSION.md`; its human observations supersede the R1 UI-complete result.
- Exact repository state at intake: branch `codex/kingmaker-buff-planner`, HEAD `a1e30a8d9e55eef3aac959e66039fe6ab0d578f3`; only the user-supplied R2 mission was untracked before durable intake edits.
- External state: installed DLL `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`, MVID `e43f060b-a2b7-48db-b19f-b45704ef77c4`; no game/UMM process, deployment lock, unresolved transaction, or unrestored external state.
- Profile state: schema-2 profile preserves one Long Bless assignment (`90e59f4a4ada87243b7b3535a06d0638`) to unit `8d7086b2-a4d5-43d5-aed6-51c789971b53`; expected fact `87b8c6270ea85c743afc734dfe99afee`; no provider preference saved and no migration ran.
- Confirmed failures: native hierarchy cloning leaves uncontrolled hit geometry and disables visible-icon raycasts; modal input lease precedes all presentation proof; queued animated commands are called fired and can be reported complete without expected facts.
- Exact next command: implement the fresh retained-mode HUD row and transactional presentation-first screen lifecycle, then run focused behavior tests before execution-outcome changes.

Status: INSTALLED FOR HUMAN UI RETEST — campaign UI and save-backed qualification pending

- Repository/branch: standalone Kingmaker Buff Planner / `codex/kingmaker-buff-planner`
- Starting repair HEAD: `ec153837401c8815b1909cb15e85ab658a1ee26a`; release commit: `447bbd288c803a4aec609db84a4c6076cbfe94f3`; final records checkpoint pending
- Development/installed version: `0.0.2`; installed DLL SHA-256 `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`
- Worktree: final install/release records only
- Authoritative repair mission: `planning/FULLSCREEN-UI-INPUT-ISOLATION-REPAIR-MISSION.md`
- Confirmed root cause: the production UI is fixed-coordinate IMGUI, while exact Kingmaker `PointerController.InGui` only recognizes EventSystem pointer-over-GameObject state. No native full-screen mode or raycast surface is acquired.
- Exact native evidence: `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; full-screen event enters/stops `GameModeType.FullScreenUi`; native service canvas blocks raycasts and hides/restores HUD.
- Invalid prior gate: it hardcoded routine count/layout and required zero blockers/subscriptions without dispatching real UI/world input.
- Long finding: an empty/zero-step routine reports only inside the closed setup window, so HUD execution can appear silent even if its coroutine ran.
- Runtime state: Kingmaker/UMM closed; no deployment lock; live `Mods\KingmakerBuffPlanner` is guarded-installed validated 0.0.2; all non-planner mods verified byte-identical; no save accessed.
- Separate existing hard stop: no authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair; this does not block UI-only no-save repair qualification.
- Last runtime gates: native `ui-repair-0.0.2-final-native-1/2` 12/12 each and Call of the Wild `ui-repair-0.0.2-cotw-1/2` 26/26 each, exact restoration. Corrected UI gate rejected main-menu-only evidence and restored exactly.
- Exact release: package `1f328e26fdf2524fc85e8482077e5330bc5f7a48ce0d8841bd372997685d652f`, DLL `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`, MVID `e43f060b-a2b7-48db-b19f-b45704ef77c4`, commit `447bbd288c803a4aec609db84a4c6076cbfe94f3`.
- Install evidence: `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\install-ui-repair-0.0.2-local-install\install-result.json`.
- Exact-release runtime: native `ui-repair-0.0.2-release-native-1/2` 12/12; Call of the Wild `ui-repair-0.0.2-release-cotw-1/2` 26/26; UI boundary `ui-repair-0.0.2-release-ui-boundary` structured `BLOCKED` because no campaign fixture; all restored exactly.
- Exact next command: commit these final evidence records, rerun guarded push, then hand off the installed build for authoritative human playtesting.
