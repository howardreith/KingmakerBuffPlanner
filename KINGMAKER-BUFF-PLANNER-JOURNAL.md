# Kingmaker Buff Planner Journal

## 2026-08-12 — 0.0.5 catalog/HUD/input/tooltip repair complete

- Authoritative intake was a human PARTIAL PASS: the four-button row and opaque F10 planner worked; the catalog/details were blank despite 11 sources/providers, HUD clicks reached the world, quick actions were silent, and the tooltip flickered/offscreen.
- Earliest catalog defect was after the populated 11-source model: rows were bound without reliable scroll-content geometry. Production now records every filter count, forces bounded content layout, reports row/details binding exceptions, selects a visible row, and provides explicit filtered/genuine-empty recovery. The production filter policy is covered by default, all-hiding, and reset tests.
- Tooltip flicker was caused by tooltip/feedback objects participating in the HUD horizontal layout and moving the hovered source. One cached `ignoreLayout` tooltip is non-raycastable, wrapped, inward-placed, and canvas-clamped. World/camera suppression is a narrow Harmony boundary on `PointerController.Tick` and `CameraRig.GetCameraScrollShiftByMouse`, active only over registered planner rectangles.
- Quick actions now trace receipt through confirmed result presentation, never equate queue/submission with success, and surface unexpected coroutine failures. The controlled live Bless plan was deliberately refused with exact `material-component-unavailable`; it was not called applied.
- Exact release source is `390bb8b5f514a38edf1c553962813e29a1b526fd`; 0.0.5 package `3eba3158aa92a6b66e249ec35aa297500eb4c5decdf73974c26992219922349c`; DLL `6999284085bd6898f6bd871900783f6f81343a6f801b2d2c95acd208c6513b56`; MVID `d2fed415-bfa2-47a7-90ba-f50fa8d1c7de`.
- Fresh-process physical runs `catalog-input-0.0.5-five-second-physical-1` and `catalog-input-0.0.5-five-second-physical-2` passed 69/69 each and held the physical tooltip continuously for at least five elapsed seconds. Native `catalog-input-0.0.5-five-second-native` passed 12/12; Call of the Wild `catalog-input-0.0.5-five-second-cotw` passed 26/26. Every transaction restored exactly; baseline stayed `afca8ac5...`.
- The first final transaction was denied by managed staging permissions; its owned backup was restored and verified. One later run correctly refused HUD ownership while UMM's blocker remained after synthetic Escape; it restored exactly. The first install attempt then caught stale CLR 0.0.4.0 metadata before activation and verified rollback. Assembly metadata and its validation gate were corrected, and every exact-package gate was rerun.
- Guarded install `catalog-input-0.0.5-five-second-install` is `Installed`: CLR 0.0.5.0, exact DLL/MVID, `settingsPreserved=true`, and `otherModsVerified=true`. No merge or public release occurred.

## 2026-08-12 — first 0.0.5 campaign diagnostic and exact contract correction

- `catalog-input-0.0.5-live-diag-1` failed before callback registration: exact 2.1.7b IL has `PointerController.get_InGui`, but reflection exposes no `PropertyInfo` for `InGui`. The first repair incorrectly required that metadata row.
- The timed-out harness left only its owned PID 4148 running and restoration blocked by design. After reading the exact exception, that PID alone was terminated and guarded restoration returned `verified=True`; installed 0.0.4 and every other mod were restored.
- Commit `157b1d8` resolves the exact method directly and assigns UMM callbacks before the fail-soft Harmony attempt, preserving independently armed F10 even if the scoped patch fails.
- Corrected local gates remain source `27/27`, protocol `60/60`, harness filesystem `7/7`, package `4/4`, deployment WhatIf `5/5`.

## 2026-08-12 — catalog/HUD input/tooltip R3 human rejection and read-only intake

Status: 0.0.4 PARTIAL HUMAN PASS; FOUR RELEASE BLOCKERS CONFIRMED; SOURCE REPAIR PENDING

- Starting branch/HEAD/version: `codex/kingmaker-buff-planner` / `d2aecb00e02a63b5ea33976e34ff1eefc9765a1a` / `0.0.4`; the only initial worktree entry was the user-supplied R3 mission.
- Installed DLL, UMM cache, release DLL, and manifest reconcile exactly: DLL `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`; MVID `305a8a6c-2b49-4e3b-a365-286638cbfafa`; package `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`; release source `5b96f3b4e713489ce677db3ac5acb83a10f80f01`.
- Preserved human PASS: visible/acceptable four-button row, visible opaque F10 planner, and actual campaign bootstrap.
- Earliest catalog failure is after the 11-source model and bind loops, at unmeasured scroll-content/row/detail layout visibility. Neutral defaults and zero hidden IDs cannot filter the list to zero; a bind exception would have rolled the full screen back.
- Tooltip root cause: activating a 620-pixel tooltip child inside the four-button `HorizontalLayoutGroup` moves the source button, causing enter/exit oscillation; feedback has the same defect.
- Input proof defect: the gate invoked synthetic `ExecuteEvents`, while exact Kingmaker `PointerController.Tick` samples raw mouse state through `InGui`; human physical click-through supersedes the synthetic PASS.
- Quick-action defects: quick buttons are disabled until setup refresh creates a model, layout-moving feedback is unstable, and current counters do not prove ordered plan/execution stages.
- Exact Bless/profile slice retained: Long -> Bless `90e59f4a4ada87243b7b3535a06d0638`, target `8d7086b2-a4d5-43d5-aed6-51c789971b53`, expected fact `87b8c6270ea85c743afc734dfe99afee`, zero hidden sources.
- External state: no Kingmaker/UMM process, active lock, non-restored transaction, or mutation.
- Exact next action: implement reconciled catalog/layout diagnostics first, then repair explicit content sizing, cached non-layout tooltip, conditional planner pointer capture, and quick-action session/feedback stages.

## 2026-08-12 — live UI bootstrap recovery complete

Status: PASS TWICE; 0.0.4 GUARDED-INSTALLED; NO PUBLIC RELEASE

- Exact release source `5b96f3b4e713489ce677db3ac5acb83a10f80f01`; package `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`; DLL `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`; MVID `305a8a6c-2b49-4e3b-a365-286638cbfafa`; deterministic 2/2, local-only.
- `bootstrap-0.0.4-human-live-6` and `bootstrap-0.0.4-human-live-7` passed 65/65 each in distinct fresh processes. Both used the exact human-reproduction mod set and normal Working save-slot load, physically dismissed UMM ShowOnStart, then physically delivered F10.
- Each proved one retained controller and HUD row, four ordered active controls/listeners, owned hitboxes above the native cluster, one F10 keydown, opaque/raycast-owning planner, 21 opens and 21 post-close destroys, balanced input leases, Default/selection restoration, zero duplicate objects, zero native activation, and zero world input.
- Immutable baseline remained `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`; every transaction restored exactly. Run result hashes are `cd1d2281...` and `4ad411a5...`.
- Exact-release native regression `bootstrap-0.0.4-release-native-regression` passed 12/12; Call of the Wild `bootstrap-0.0.4-release-cotw-regression` passed 26/26; both restored exactly.
- Install WhatIf passed without mutation. Guarded install `bootstrap-0.0.4-local-install` replaced only 0.0.3; installed identity matches release, `settingsPreserved=true`, `otherModsVerified=true`, and no process/lock remains.
- No main merge, direct push, or public release was performed.

## 2026-08-12 — exact campaign load and HUD layout diagnosis

Status: DIAGNOSTIC FAIL REPAIRED IN SOURCE; REQUALIFICATION PENDING

- End-to-end campaign diagnostic `bootstrap-0.0.4-human-live-5` reached physical UMM dismissal and physical F10. UI evidence: singleton root; four ordered HUD controls and listeners; above-native, owned hitboxes; one F10 keydown; 21 opens and 21 closes; 100% opaque/raycast coverage; balanced input lease; no duplicate object, native activation, selection/ability target, movement, ability, player, or camera input; exact Working save action once. It is not a PASS because the test captured baseline after the first planner opened and because UMM loaded exact Cheat Menu cache hash `8a7525e0...` rather than primary fixture hash `7d659eb0...`. Both evidence contracts are fixed without weakening UI assertions or fixture identity.
- Campaign diagnostic `bootstrap-0.0.4-human-live-4` converted the same button center correctly, and EventSystem then identified the real top hit as `UMM blocking UI/Image`. Installed UMM parameters prove `ShowOnStart=1` and no manager hotkey, so unattended launch kept the manager overlay open. The run restored exactly. The live protocol now emits `umm-overlay-ready.json`, physically sends Escape to the foreground process, waits for KBP ownership, and then separately sends physical F10; it never edits UMM configuration.
- Campaign diagnostic `bootstrap-0.0.4-human-live-3` proved the row now clears the above-cluster predicate, then failed at `button-raycast-empty` because world center `(-845.1,-505.2)` was passed directly as a screen pointer position. The runtime host committed exactly one failure result and the transaction restored. HUD validation and runtime dispatch now convert through `GraphicRaycaster.eventCamera`; output log is `runtime-evidence/bootstrap-0.0.4-human-live-3/output_log.txt`, SHA-256 `0a1c31985ba1d27012778537895021b09df4c34aa4c720bcbdee54dfaf1d713a`.
- Safety recovery: a pre-launch attempt for `bootstrap-0.0.4-human-live-2` moved original Mods to its owned backup but could not activate staging. Automatic restoration then treated a bound null PID as a running game. Audit proved no process, absent live destination, exact owned backup, and matching transaction lock/token. The project-owned restore was invoked without the null parameter and verified the complete original manifest; status is `Restored`, with no lock or staging residue. The harness now filters null/nonpositive IDs and has a regression test.
- Three initial save attempts (`bootstrap-0.0.4-native-live-1`, `bootstrap-0.0.4-cotw-live-1`, and `bootstrap-0.0.4-cotw-live-normalized-1`) failed in `Player.PostLoad`; each exact transaction restored. Archive evidence showed the human save depends on the same Bag of Tricks, Call of the Wild, Cheat Menu, and Craft Magic Items set used by the clean reproduction.
- Added and exact-hash-bound the `human-reproduction` compatibility profile. Run `bootstrap-0.0.4-human-live-1` then loaded `KBP_AUTOMATION_WORKING` through the normal save-slot action. Baseline remained SHA-256 `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`.
- Live evidence proves `Main.Load`, `OnToggle(true)`, UMM `OnUpdate`, zero required Harmony patches, independent F10 arming, retained controller construction, EventBus subscription, five lifecycle callbacks, and campaign UI readiness all occurred.
- Earliest failure moved to HUD placement: native layout participation overrode the row anchor, producing `rootBottom=-579.0969;clusterTop=-534.0093`. The row remained absent after 77 retry attempts, so the run is diagnostic FAIL.
- The guarded process was stopped by exact PID after the internal timeout; transaction status is `Restored` and `restorationVerified=true`. Evidence: `runtime-evidence/bootstrap-0.0.4-human-live-1`; output-log SHA-256 `afd2513c3667802b05ea035ded08d2cab3313bffe8fc9213d032e1745bfb11db`.
- Repair sets `ignoreLayout=true` only on the KBP HUD root and commits runtime failures once. Gates now pass source 24/24, behavior/protocol 60/60, harness 6/6, package 4/4, deployment WhatIf 5/5, and build 1/1.
- Exact next action: commit, clean-build, and repeat the human-reproduction live scenario from a fresh process.

## 2026-08-12 — R2 correction qualification and guarded installation

Status: 0.0.3 INSTALLED FOR AUTHORITATIVE HUMAN RETEST; campaign/save-backed mechanics remain blocked by the protected-save boundary.

- Implementation checkpoints: `79e13b5a1e75485ff1df27d55fdd278b47dd1f8d` (retained HUD, transactional modal, confirmed execution), `31c86b6417a5ab15d7bc0031c05e4a402f2a16ef` (invalidated acceptance records), and release-source commit `d5a20aa7ddbb2ec7d131a4bed44f1ca65ecaaa65` (0.0.2 replacement preflight).
- Exact 0.0.3 release: package SHA-256 `42f823d6b8454ffe4497f4f652752a07d50738d5990c5a5243d091ba92d363e0`, DLL SHA-256 `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950`, MVID `f3f691a4-d691-4112-90a4-7beb9f06aad2`; deterministic builds 2/2 and package validation 4/4.
- Source gates: source validation 21/21, behavior/protocol 57/57, runtime-harness filesystem 6/6, deployment WhatIf 5/5, installer WhatIf 5/5, warning-free exact-reference Release build, and `git diff --check` PASS.
- Exact-release native runs `r2-0.0.3-release-native-1/2` passed 12/12 each with identical catalog `50d091a3f17499885b483723ca69acb357e8ae153b301ccb672aa1425e5d4f2d` and Harmony `b5605e22bde458a238d63c6ffe33a99eb712bd22bf3cbc74c42d443ad479efb4` hashes. Exact Call of the Wild runs `r2-0.0.3-release-cotw-1/2` passed 26/26 each with identical catalog `cdc8d34c8f90f746f18dc278dcadf1580ed3d66700b71d54258ab26a6d80d3e2` and Harmony `a883dd60218a1f9e989a4e6b03d99318242d401d33f914cd6c068f767b308427` hashes. Every transaction restored exactly.
- Campaign-only UI run `r2-0.0.3-release-ui-boundary` loaded the exact 0.0.3 identity and returned structured `BLOCKED` at `campaign-ui-unavailable` (4/5 precondition assertions), then restored exactly. It did not certify HUD geometry, modal visibility, click ownership, Long, or Bless from the main menu.
- Guarded install `r2-0.0.3-local-install` replaced only validated 0.0.2 with 0.0.3. Installed Info/assembly versions are `0.0.3`/`0.0.3.0`; installed DLL/MVID match the release; `otherModsVerified=true`, `settingsPreserved=true`, lock absent, game/UMM closed. Evidence: `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\install-r2-0.0.3-local-install\install-result.json`.
- The external profile remained byte-identical at SHA-256 `3723e3181c56bff6427a15b2ba85ffd76fd40e98f3f482253b15910f038d6b48`. It still contains Long → Bless (`90e59f4a4ada87243b7b3535a06d0638`) targeting `8d7086b2-a4d5-43d5-aed6-51c789971b53`; no schema migration, discard, or remap occurred. No provider was persisted by 0.0.2, so the prior provider cannot be reconstructed; 0.0.3 reports the live resolved provider, resource lifecycle, expected `BlessBuff` `87b8c6270ea85c743afc734dfe99afee`, targets, and confirmed/failure outcome on retest.
- Guarded push WhatIf passed, then the project-owned helper pushed and verified feature-branch evidence checkpoint `fc94060a861db6356a5bdb8d2520f377ec52b0c5` without merging or publishing a release.
- Exact next action: hand off the installed 0.0.3 build for the mission's mandatory 11-point human campaign retest. Do not claim R2 UI or Bless PASS until that verdict exists.

## 2026-08-12 — R2 human rejection, exact intake, and confirmed causes

Status: 0.0.2 HUD/modal/execution acceptance INVALIDATED; R2 correction IN PROGRESS

- Starting branch/HEAD/version: `codex/kingmaker-buff-planner` / `a1e30a8d9e55eef3aac959e66039fe6ab0d578f3` / source and installed `0.0.2`. The only starting worktree entry is the user-supplied untracked `planning/HUD-MODAL-EXECUTION-R2-CORRECTION-MISSION.md`.
- Installed identity is proven rather than inferred: DLL SHA-256 `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`, MVID `e43f060b-a2b7-48db-b19f-b45704ef77c4`, matching the prior 0.0.2 release identity. Kingmaker and UMM are closed. All latest runtime transactions report `Restored` with `restorationVerified=true`; no deployment lock or unresolved Git operation is present.
- R2 human screenshots SHA-256: HUD overlap `9622b0c37746af5d37ed0d0d8823f2831cca691037f878c75a4f2cae8b58d848`; tooltip/click-through `a0480a9993027256bf828ccac4c4a2c8c36b2bd2cac8709ffea3a0c9d1c03302`; invisible F10 modal `65895452c61f57b55f217063089f787501623e1d16081343ef7c2d848088493c`. The evidence proves the four planner objects occupy native turn-based/pause hit and tooltip regions, and proves `FullScreenUi` can hide the HUD without a rendered planner.
- HUD root cause: `BuffPlannerHudButtonController.CreateNativeButton` clones the complete native formation `ButtonPF` GameObject. It removes known tooltip triggers but retains unknown native child graphics/components and hit geometry, then places a four-button right-extending row from one button's anchored position. The visible icon is deliberately `raycastTarget=false`; the cloned hierarchy, not an intentionally bounded planner graphic, owns the hit surface. The row has no dedicated raycast canvas or proof that its visible center is the top EventSystem hit. Relocation alone cannot repair this structure.
- Modal root cause: `BuffPlannerScreenController.Open` calls `_state.Open()` first, which immediately acquires `BuffPlannerInputLease` and raises native `FullScreenUi`, before session refresh, `StaticCanvas` validation, view construction, layout, rendering, or visibility validation. `BuffPlannerScreenView` only records constructor-time component flags; it never forces layout, checks world corners/screen coverage, raycasts the rendered root, or revalidates after native HUD-mode transition. The state machine equates lease presence with `IsOpen`, so F10 can report open and suppress gameplay without any presentation proof.
- Execution root cause: animated execution adds `CastExecutionStatus.Fired` immediately after `AddToQueue`, before `UnitCommand` start/completion or effect observation. The UI then foregrounds `fired=` and can return `Completed` whenever no `Failed` record exists, even if `SuccessfullyObserved` is zero. This made a queued Bless command appear successful without the Bless fact.
- Exact external profile: `Mods\\KingmakerBuffPlanner\\UserSettings\\kingmaker-buff-planner-a9d1cfe775e8e5830e55ab18.json`, SHA-256 `3723e3181c56bff6427a15b2ba85ffd76fd40e98f3f482253b15910f038d6b48`, schema 2, exact campaign ID `11e251be-321b-47bd-ad3e-6d221cccd081`. The Long routine contains one preserved Bless assignment, ability/source GUID `90e59f4a4ada87243b7b3535a06d0638`, source kind `Spellbook`, target unit ID `8d7086b2-a4d5-43d5-aed6-51c789971b53`, and no provider preference/ban/priority/cap. Its prior backup has the same Bless assignment but no wanted target; 0.0.2 updated the target without schema migration. No assignment was dropped or remapped.
- Exact Bless structural expectation: native Bless ability `90e59f4a4ada87243b7b3535a06d0638` expects `BlessBuff` GUID `87b8c6270ea85c743afc734dfe99afee` on every planned target. Provider identity cannot be reconstructed from the profile because provider preferences are intentionally absent and the selected automatic provider is derived from the live party snapshot; the failed build did not persist execution diagnostics. R2 must surface the resolved provider, target, reservation, command/rule, resource delta, expected effect, and confirmation/failure in durable runtime/UI diagnostics.
- Rejected theories: the overlap is not merely a bad anchored position; the modal is not acceptable because a GameObject exists; a queued/submitted cast is not applied; the schema-2 profile did not lose Bless.
- Exact next action: replace the cloned HUD hierarchy with fresh retained-mode buttons and top-hit validation, implement presentation-first modal transactions with rendered-coverage diagnostics and rollback, then replace execution accounting with confirmed terminal outcomes and regression tests.

## 2026-08-12 — Human UI rejection and repair intake

Status: prior UI gate INVALIDATED; repair investigation IN PROGRESS

- Starting branch/HEAD/version: `codex/kingmaker-buff-planner` / `ec153837401c8815b1909cb15e85ab658a1ee26a` / `0.0.1`; worktree initially contained only the user-supplied repair mission.
- Human evidence: installed overlay screenshot `6a1b6de2d98e68731ef6d695d69facb52497c77fcdfb16cefff82905e44000ba`; BubbleBuffs HUD/full-screen UX references `c3c55e9ad0b295094c78d547410e4f808baba16ea9f95e2b554261ecbbfc7ea8` / `cec431dbe2ca1809b37ce1e7d2e5dcd72bcbfe9cd8658d6a40c8d76adbadaf2a` (read-only UX references, no assets/code reusable).
- Authoritative defects: debug-style text strip, world click-through/movement, apparently silent Long, non-opaque non-service setup, and F10 overemphasis.
- Exact source cause: `BuffPlannerUiRoot` uses fixed `OnGUI` rectangles and `GUI.Window`; it creates no EventSystem raycast target and acquires no native UI mode.
- Exact assembly cause: installed `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; `PointerController.InGui` calls `EventSystem.current.IsPointerOverGameObject()`. Native `FullScreenTabsWindow` raises the full-screen event; `StaticCanvas` starts/stops `GameModeType.FullScreenUi`, hides/restores HUD, and native service canvas raycasts are enabled only while shown.
- Gate defect: former smoke returned a hardcoded routine count, arithmetic fit, and explicitly asserted zero blockers/subscriptions. It never dispatched pointer events or observed world command/selection/camera boundaries.
- Long defect: the quick coroutine has no HUD-visible feedback channel; zero-step execution status exists only inside the closed window, with no stage instrumentation.
- Tools: added `scripts/Inspect-KingmakerUiContracts.ps1`, a read-only exact-assembly IL contract inspector; no external dependency added.
- Runtime/save state: game closed; 0.0.1 installation unchanged; no save touched; no deployment lock.
- Rejected theory: click-through is not adequately explained or repaired as an isolated raycast omission. Native mode/controller deactivation plus a raycast/event surface and scoped restoration are both required, and silent execution needs independent instrumentation/feedback.
- Exact next action: guarded no-save hierarchy/input-contract probe, then production HUD/screen/input lease implementation and corrected deterministic/runtime gates.
- First probe attempt `20260812-ui-native-contract-probe` did not launch: `New-KbpRuntimeRequest` retained the old scenario `ValidateSet`. The transaction restored exactly. The inner allowlist was corrected before retry; this was orchestration validation, not game/runtime behavior.
- Second probe `20260812-ui-native-contract-probe-2` launched through the guarded no-save harness and restored the live `Mods` tree exactly, but the main menu never instantiated `StaticCanvas`; this rejects treating main-menu evidence as campaign HUD/service-window evidence. No save was opened or written.
- Replacement source now uses four cloned native `ButtonPF` controls anchored to exact `IngameMenuController.m_FormationButton` hierarchy, original programmatic icons, HUD-visible quick-result feedback, a fully opaque full-canvas uGUI service window, native full-screen event/mode acquisition, selection suppression, a disposable input lease, and a pointer sink consuming click/drag/scroll/cancel.
- The former false UI gate is retired. `final-no-save-core` no longer claims campaign UI coverage; `ui-root-smoke` now requires a real campaign UI and checks one setup plus three quick buttons/listeners, native anchor, opaque blocker/raycaster, native mode, exact pre/post input state, 20 open/close cycles, rebuild uniqueness, dispatched pointer events, zero player movement/ability/selection events, group-tab change, and exactly-once empty-Long feedback.
- Version advanced to 0.0.2. Source-only checkpoint: source validation 19/19, behavior/protocol 56/56, runtime harness filesystem 6/6, package allowlist 4/4, deployment WhatIf 5/5. Interim package SHA-256 `68d685f344c51319c5d8a7713c4293f8f2d293df6370f1a58eff86c233569a54`, DLL SHA-256 `93795bf2de26fc1ef34f1114f7108771b9ddfdb907688bc0ea0515b1f8b2c7d5`; these are dirty-worktree development artifacts and are not release identities.
- Added a guarded local replacement installer with exact release-manifest validation, game/UMM closure checks, global deployment lock, planner-only backup, external-profile preservation, staged hash verification, non-planner byte identity, rollback, and durable evidence. Actual installation remains pending final clean-head release qualification.
- Exact next action: commit the 0.0.2 UI/input implementation checkpoint, rebuild from exact clean HEAD, run corrected no-save/catalog regressions and the campaign UI gate as far as the authorized environment permits, then finalize docs/release/install evidence.
- Implementation checkpoint `849259e2cbd3a382dd0d7cb72f1354dd01549dd7` committed the 0.0.2 HUD/full-screen/input repair and guarded installer.
- Call of the Wild preflight then failed closed because its live directory had regenerated `loaded_blueprints.txt` and no longer matched the immutable profile. No live third-party file was changed. Checkpoint `3bd519b000f3126b19462888aefeabe29374873d` routes staging from the existing read-only lab fixture, whose whole-directory identity is `c0b163d79faf5a4220f6cadefe731d5fff8484e02ba2e7fa467243d19577b45b` (264 files, 55,258,405 bytes), while preserving exact primary DLL/info hashes.
- Final clean-head 0.0.2 local package at `3bd519b`: package `651d9ce3f92649f86d6e619e46fe3293ace1019e10e3b086c2a8c3617452b68f`, DLL `f039f436fb948c7acb203e60979dec3bb500e03e85ff8f6a73ae6753b293b850`, MVID `5f57af25-8876-400a-99b9-5971d8bfd8f4`.
- Guarded runs passed with exact restoration: native `ui-repair-0.0.2-final-native-1/2` 12/12 each; Call of the Wild `ui-repair-0.0.2-cotw-1/2` 26/26 each. Per-profile catalog and Harmony evidence repeated byte-for-byte.
- Corrected UI run `ui-repair-0.0.2-ui-main-menu-boundary` reached only the main menu, recorded `Campaign UI is required for the full-screen input-isolation scenario`, exited, and restored `Mods` exactly. This is the required rejection of the former false acceptance pattern, not a mechanics PASS.
- Exact next action: commit documentation/failure-envelope changes, rebuild final clean HEAD, repeat applicable tests, create deterministic release package, run installer WhatIf, guarded-install 0.0.2, verify installed/non-planner identities, push feature branch, and hand off human checklist.
- First final `Build-Release.ps1` attempt failed on its required second rebuild because the newly added in-process reflection-only MVID read held the first DLL open. The deterministic gate was preserved; `Build-Local.ps1` now performs MVID inspection in a short-lived child PowerShell process so the assembly lock terminates before repetition.
- The first installer WhatIf exposed inherited `$WhatIfPreference` leaking into read-only package ZIP enumeration. The installer now suppresses WhatIf only during validation/preflight and restores it immediately before its single `ShouldProcess` boundary, matching the guarded runtime deploy pattern.
- Final deterministic release commit `447bbd288c803a4aec609db84a4c6076cbfe94f3`: package SHA-256 `1f328e26fdf2524fc85e8482077e5330bc5f7a48ce0d8841bd372997685d652f`, DLL SHA-256 `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`, MVID `e43f060b-a2b7-48db-b19f-b45704ef77c4`; two builds matched and package validation passed 4/4.
- Installer WhatIf passed 5/5. Guarded install `ui-repair-0.0.2-local-install` then replaced validated 0.0.1 with exact 0.0.2, preserved `UserSettings`, verified all non-planner mods byte-identical, retained the prior planner-only backup, removed its lock/staging state, and wrote evidence at `runtime-evidence/install-ui-repair-0.0.2-local-install/install-result.json`. Post-audit: installed DLL hash exact, install status `Installed`, `otherModsVerified=true`, `settingsPreserved=true`, no Kingmaker/UMM process, no deployment lock.
- Exact next action: commit final durable handoff records, run complete source/package/install/push WhatIf gates, push the feature branch with the guarded helper, and hand off human campaign UI checklist. Do not claim human/campaign UI PASS or save-backed completion.
- The exact installed/release commit `447bbd2` was checked out temporarily without modifying history. Exact-package native runs `ui-repair-0.0.2-release-native-1/2` passed 12/12 each; exact-package Call of the Wild runs `ui-repair-0.0.2-release-cotw-1/2` passed 26/26 each. Every transaction restored the already-installed 0.0.2 tree exactly.
- Exact-package corrected UI run `ui-repair-0.0.2-release-ui-boundary` produced the intended structured `BLOCKED` / `campaign-ui-unavailable` result with five identity/precondition assertions, then restored exactly. It did not reinterpret the main menu as campaign input evidence.
- Returned to clean feature branch HEAD `acf355b4c17291b69772b9e53fd7640676aa6f61`; it had already been guarded-pushed with exact remote verification. Final records update and one final guarded push remain.

## 2026-08-11 — Phase 0 intake

Status: IN PROGRESS

- Branch: `codex/kingmaker-buff-planner`
- Intake HEAD: `c7913b1cbca699cde5b24b1bcd1d36344402bb0c`
- Remote branch and `origin/main` at intake: same SHA
- Worktree at intake: clean; no repository lock or concurrent agent found
- Mission source/copy SHA-256: `3682d33c3c37e4f8cf67983f2a147599164e7da0df5b9d884c427f9ab1c17308`
- Active version: not assigned; Phase 1 not yet scaffolded
- Last successful gate: authoritative contract and mission fully read; standalone repository identity established
- Exact local target: Kingmaker 2.1.7b, Unity 2018.4.10f1, UMM 0.28.2, Harmony12 1.2.0.1, net47/C# 7.3
- Reference validation: BubbleBuffs, BuffIt2TheLimit, PathfinderAutoBuff, and KingmakerRebalance/COTW licenses and immutable identities recorded
- Exact-assembly work: action-graph, spellbook, resource, `AbilityData`, `RuleCastSpell`, and `UnitUseAbility` contracts inspected
- Rejected theory: a transferred laptop UMM version is the desktop target; exact local UMM is 0.28.2
- Rejected theory: `RuleCastSpell` spends resources; exact IL shows native command code triggers the rule and then calls `AbilityData.Spend()` once when appropriate
- Runtime/profile state: no transaction active; runtime roots empty; no game process; live external state untouched
- Known constraint: no `KBP_` disposable save; save-backed scenarios cannot truthfully run
- Current files being changed: governance, architecture, contract inventories, matrices, journal, blockers, and resume state
- Exact next action: finish read-only harness intake, create redacted fingerprint schema/exact private fingerprint, then scaffold Phase 1

## 2026-08-11 — Phase 0 checkpoint

Status: PASS

- Branch: `codex/kingmaker-buff-planner`
- Commit: `ff54df0c06b4a366f715a4cac2654e358cc84c66` (`docs: record autonomous mission intake`)
- Work completed: first coherent intake commit; exact mission copy, environment/reference fingerprint, architecture/source matrix, discovery/resource contracts, coverage/runtime/compatibility matrices, blockers, journal, and resume records
- Validation: `git diff --cached --check` PASS (0 defects); prohibited payload review PASS (0 payloads)
- External state: none altered; no deployment/runtime transaction
- Current uncertainty: desktop UMM deviation and missing `KBP_` save remain recorded, not concealed
- Exact next action: commit the four remaining mission-required architecture/runtime documents before production scaffold

## 2026-08-11 — Phase 1 source/build/package scaffold

Status: IN PROGRESS

- Branch/HEAD: `codex/kingmaker-buff-planner` at `4ca6008d873577e8e6263b54658620b649f81cd1`
- Active version: 0.0.1
- Work completed: classic net47/C# 7.3 project, exact UMM 0.28.2 lifecycle, Info.json, atomic strict request/result skeleton, build/package/source validation, deterministic project-owned test runner
- `scripts/Test-SourceOnly.ps1`: source PASS 14/14; protocol PASS 11/11; suite PASS 1/1
- `scripts/Build.ps1 -Configuration Release`: PASS 1/1, zero warnings
- `scripts/Build-Local.ps1` repeated twice: package validation PASS 4/4 each; identical hashes
- Pre-checkpoint DLL SHA-256/MVID: `3b21bec7b283ab3f760e1c7fe81de6b9d06794696d323e35c9c2d498c4a78cee` / `e07bf29a-cd45-44bf-870c-6cfa19988822`
- Pre-checkpoint package SHA-256: `f109c51b8afcd0af931c556ad5999ea62c1b88f951178da8d563253c18de4d21`
- Failure repaired: generated build-info semicolons were split by MSBuild item syntax; encoded semicolons and line items now generate valid C#
- Failure repaired: ZIP timestamps are timezone-free DOS wall-clock values; validator now compares the deterministic wall-clock representation
- UMM dependency finding: UMM 0.28.2 references runtime-host Harmony 2.3.1/net48 internally. It is marked `ExternallyResolved` so net47 compilation uses the exact UMM public API without resolving or shipping the host’s transitive internals; no warning is suppressed
- Runtime/profile state: no transaction active; no external state altered
- Exact next action: checkpoint scaffold, rebuild from clean exact commit, then implement and source-qualify the guarded deployment/runtime transaction

## 2026-08-11 — Guarded runtime transaction and Steam safety

Status: IN PROGRESS

- Branch/HEAD: `codex/kingmaker-buff-planner` at `aa3c4c521394a94fbab54526f22c907580ade0cd`
- Active version: 0.0.1
- Clean checkpoint rebuild before harness changes: source 15/15, protocol 12/12, runtime transaction 5/5, deployment WhatIf 5/5
- Whole-`Mods` transaction: project lock/token, state, isolated staging, backup, live sentinel, mutation observation, exact manifest restore, and fail-closed recovery
- Synthetic scenarios PASS 5, FAIL 0: existing tree, absent tree, staged mutation, running-process refusal, foreign-lock refusal
- Public `Deploy-Local -WhatIf`: PASS 5 compared roots, FAIL 0; live `Mods` and all runtime roots unchanged
- Steam finding refined: startup attempted protected-save downloads, then the same session logged off and App 640820 recorded `Sync Disabled` with `offlineMode=true` after transfers; current NO-SAVE launch preflight can be proven
- Privacy decision: raw Steam/account log lines are not copied into evidence; only sanitized state and timestamps are retained
- Runtime state: no transaction active; no game launched; live `Mods` unchanged
- Exact next action: checkpoint/rebuild the guarded harness, run top-level runtime WhatIf, then request the guarded fresh-process mod-load run

## 2026-08-11 — First guarded mod-load attempt and recovery

Status: FAIL

- Source commit: `3ae93f8653afa90bb606ffccea1c24d5869ba22e`
- Active version: 0.0.1
- Package/DLL SHA-256: `8c9d041bc120ae583bf808ce96ce436df308655b6397e2abbeceb978f230547b` / `f18ce1a81fb8df7b9142bd89af9a3e521a748a7c82d9b3ade3a57a2f133cefed`
- Runtime evidence ID: `20260811T1931581708021Z-mod-load-smoke`
- UMM evidence: exact 0.28.2 manager loaded 1/1 mods; Kingmaker Buff Planner 0.0.1 and exact embedded commit loaded/enabled
- Failure: `RuntimePaths` equivalent logic interpreted a trailing UMM mod path one level too high and looked for `Mods\Kingmaker.exe`; atomic result status FAIL, stage `unhandled-exception`
- Safety behavior: game requested clean exit; orchestrator observed it during the exit race and conservatively withheld restoration
- Recovery: after proving Kingmaker absent and sentinel/token ownership exact, guarded `Restore-Local.ps1` completed; status Restored, `restorationVerified=true`, original/live manifest equality true, lock absent
- No save was selected, loaded, or written
- Regression: explicit path helper tests trailing and non-trailing paths; protocol suite now PASS 14/14; orchestrator waits up to 30 seconds for its process in `finally`
- Exact next action: checkpoint fix, rebuild exact package, repeat top-level WhatIf, then run fresh-process mod-load qualification again

## 2026-08-11 — Second mod-load attempt, validator mismatch

Status: FAIL

- Source commit: `c0423d3068506b50a255cc264e93b5e1c5e48708`
- Evidence ID: `20260811T1935262721216Z-mod-load-smoke`
- Package/DLL SHA-256: `108ae1b8c8f702729759f35c6e422a1390c576455d7ca1370199fd048aabea60` / `491447f43c748fd2e091d7dc474eebc135aa7d4d0a5c4ea1f9485eddd821e2cb`
- In-game result: PASS, 5 assertions, MVID `16d7db94-a319-4242-b036-e303d8ca9ddf`; exact game executable, UMM, Harmony, DLL, package, version, and commit hashes recorded
- Orchestration failure: expected UMM display version `0.28.2`, observed exact runtime API version `0.28.2.0`
- Restoration: PASS in `finally`, original manifest verified; no lock and no game process remain
- Staged mutation observed: true, consistent with UMM runtime cache behavior; future states retain the observed staged manifest before quarantine disposal
- Fix: validator now requires `0.28.2.0` and reports exact field mismatches
- Qualification decision: not counted as one of the required two orchestration PASS runs
- Exact next action: checkpoint validator/evidence improvement, rebuild clean package, run WhatIf and two fresh-process PASS attempts

## 2026-08-11 — Phase 1 fresh-process qualification

Status: PASS

- Source commit: `d3f4dc1fec9970b1c3c8eed5100052edd996c870`
- Active version: 0.0.1
- Package/DLL SHA-256: `885113c4f8e1bb3a271579188823ceb3704a3b1f531ddce32efe26fa5f295764` / `1470d6f3bc45d7612f7668e87ce862ff7d89aa415e5634581e8b23924a4ba235`
- MVID: `e268ff99-af2e-4def-8511-746cbd1b5106`
- Source gates: validation 15/15, protocol 14/14, transaction 5/5, deployment WhatIf 5/5, package 4/4
- Runtime PASS 1: `20260811T1938252394711Z-mod-load-smoke`, assertions 5/5, restoration exact
- Runtime PASS 2: `20260811T1939275269790Z-mod-load-smoke`, assertions 5/5, restoration exact
- Runtime platform: Kingmaker 2.1.7/2.1.7b executable identity, UMM 0.28.2.0, Harmony12 1.2.0.1
- Staged UMM cache mutation recorded on each run; original live tree restored without it
- Final external state: no Kingmaker process, no deployment lock, live/original manifest equality true, no save accessed
- Last successful gate: Phase 1 minimal loadable standalone mod complete
- Exact next action: Phase 2 normalized effect-expression domain and generic action-graph scanner with realistic pure fixtures

## 2026-08-11 — Phase 2 structural discovery and deterministic catalog

Status: PASS

- Branch/source commit: `codex/kingmaker-buff-planner` / `07dc2380abbac74228eed88ce73113aeeabe61db`
- Active version: 0.0.1
- Source gates: validation 15/15, protocol/domain 20/20, runtime transaction 5/5, deployment WhatIf 5/5, package 4/4, warning-free Release build
- Package/DLL/MVID: `b128386d8d6b6715a4d51190faf1d971304bd0fb748c7c0d4f57d6293ec9b20c` / `61b63cfa352ae9bc9e15b8aa10a08ff9f2fe2ac3ff2c5ee059f38d2f4e0df975` / `7f7ba872-03ce-4554-ae1d-3a5942e62e8d`
- Runtime PASS runs: `20260811T1958580589645Z-native-buff-catalog` and `20260811T2000040593873Z-native-buff-catalog`, each 8/8 assertions and `restorationVerified=true`
- Determinism: both native-only processes produced catalog SHA-256 `bcacbe69bc71c85c5299b8fe8254c18baa33d66e9e3ecf53f6d4aa6b37094878`
- Catalog: 1,722 abilities; 1,353 preliminary candidates; 1,095 effect-bearing graphs; 755 abilities/455 candidates with explicit unknown-action diagnostics; zero scanner exceptions
- Failure repaired: the first exporter root serialized as `{}` under the game's Json.NET defaults; explicit DTO wire contracts and root reconciliation added
- Failure repaired: a later inspection showed nested expressions serialized as `{}`; explicit expression contracts, per-row discriminator validation, and a regression test added
- Qualification boundary: Phase 2 structural catalog is PASS; no candidate is yet claimed fully supported until provider/resource/planning/execution audits complete
- External state: game absent, runtime lock absent, original whole `Mods` tree exactly restored; no save selected or accessed
- Exact next action: implement Phase 3 normalized party/provider/resource snapshot domain and pure snapshot tests, then add the guarded save-backed scenario only when a project-owned fixture exists

## 2026-08-11 — Phases 3–5 source implementation checkpoint

Status: PASS for source/pure behavior; runtime gates DEFER — EVIDENCED

- Branch/HEAD: `codex/kingmaker-buff-planner` at `40b233a52aebf1f87d9fc671ae24d9cf86f7150e`
- Active version: 0.0.1
- Provider/resource commit: `18776f1a650e311c4400c22d0f2764665cc6b046`
- Planner/effect detector commit: `5cbc9bb86fe9fcb79c16f8297c5a8754f34eda05`
- Persistence commit: `40b233a52aebf1f87d9fc671ae24d9cf86f7150e`
- Source gates: validation 15/15, protocol/domain/persistence 37/37, runtime transaction 5/5, deployment WhatIf 5/5, package validation 4/4
- Proven pure behaviors: stable variant/metamagic/provider identities; no spontaneous pool multiplication; primary/linked opposition spend; domain isolation; explicit unlimited casts; deterministic priorities/bans/caps; prepared-before-flexible tie-break; mass one-cast cost; typed AllOf/conditional-AnyOf presence; exact skip marker; material availability/count reservation
- Persistence: schema 2 external profile keyed by hash of exact `Player.GameId`; three bounded prior-valid backups; atomic fsync/replace; schema-1 migration; malformed, duplicate, unknown, and campaign-mismatch validation; no campaign save facts
- Runtime boundary: `party-provider-scan`, prepared/spontaneous plans, active-effect policy, profile reorder/reload remain deferred because the live inventory contains no authorized `KBP_` save
- External state: no game process, no runtime lock, no transaction; no live mod or save mutation
- Rejected shortcut: treating compilation/pure fixtures as save-backed party/resource/execution qualification
- Exact next action: implement Phase 6 standalone diagnostic setup UI and source-level lifecycle/controller tests, while retaining runtime UI gates as deferred until an authorized fixture exists

## 2026-08-11 — Phase 6 runtime UI and Phase 7–8 source engines

Status: Phase 6 partial PASS; executor source PASS; save-backed executor gates DEFER — EVIDENCED

- UI source commit: `4eca8b2894e9451398d2046ea8f74d70efe365f8`
- Strengthened UI runtime commit: `c65fec1c83dd9bdae3ea5dd0b445436eff933102`
- Animated executor commit: `41174bb61c72d2f6246460afaada71086ec24385`
- Instant executor commit/current HEAD: `ebca55bab80eb2dc54cb1a725fff7b853101daa1`
- Source gates: validation 15/15, protocol/domain/UI/execution 41/41, transaction 5/5, deployment WhatIf 5/5, package 4/4
- UI runtime: `20260811T2036033407315Z-ui-root-smoke`, 9/9 assertions, 2 cycles, 12 open render frames, 1 root, 2560x1440, exact restoration
- Runtime package/DLL/MVID for UI proof: `ed21469ffe335f6111a477ef3bda0d4690a82ae1bf446882662726b769f6ca9f` / `ae459a27720feec946bb29efb12b3dd742b15291f835efbf575db022785032d9` / `c488303c-b96e-4346-a3fb-31ee28ddd5cc`
- Animated engine: no global command patch; native command queued without interrupting unrelated commands; final validation, scoped polling, effects/reporting
- Instant engine: final revalidation; one `RuleCastSpell`; one `AbilityData.Spend()` iff not UMD-failed; batches default to 8; no direct buff/enchantment application
- Uncertainty retained: exact resource/effect equivalence, invalid/no-spend, material spend, sticky touch, scene transitions, and campaign UI require an authorized save fixture
- External state: restored; no game, lock, or save mutation
- Exact next action: add compatibility profile automation and Call of the Wild generic-discovery qualification, then continue native candidate classification without hiding unknowns

## 2026-08-11 — Phase 9 complete native structural audit

Status: PASS for classification/reconciliation; runtime equivalence DEFER — EVIDENCED

- Branch/runtime source: `codex/kingmaker-buff-planner` at `fba6e24`
- Source gates: validation 15/15, behavior 43/43, runtime transaction 5/5, deployment WhatIf 5/5, package 4/4
- Exact player-accessible inventory: 974 candidates from player class/race/feat roots out of 1,722 native abilities
- Reconciled disposition: automatic 396; generic reflection 1; explicit adapters 13; overrides 3; excluded 561; unsupported/unclassified 0
- Generated schema-4 catalog SHA-256: `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa`
- Deterministic guarded runs: `20260811T2126328544602Z-native-buff-catalog` and `20260811T2127318144905Z-native-buff-catalog`; byte-identical, exact restoration, no save
- Package/DLL: `766514afc64d96cd719b2237d3435156987f05ba46d6f10b0f09f0806647ca79` / `0e0423f4d33f733421a9181299b661b608b8a328e16ba479661c66c82318cb35`
- Repaired theories: broad spell/effect candidacy; summon after-buffs as source effects; sticky carrier buffs; hostile self/current-target markers; cooldown-only healing markers; flattened selected/random alternatives
- Exception layer: strict packaged schema-1 registry, three friendly include overrides and two direct-healing cooldown exclusions; structural discovery remains primary
- Qualification boundary: all 413 included rows await save-backed resource/effect/executor equivalence; no runtime PASS inferred
- External state: game absent, lock absent, whole original `Mods` tree restored exactly
- Exact next action: checkpoint generated audit/docs, then Phase 10 HUD routine execution and service composition

## 2026-08-11 — Phase 10 routine composition and first HUD smoke

Status: source behavior PASS; no-save HUD smoke 1/2 PASS; full Phase 10 gate IN PROGRESS

- Branch/runtime source: `codex/kingmaker-buff-planner` at `420d7a1f1f20f49706a34d953df5f0d39f67e4a8`
- Implementation commits: `9191d4a` executable routine workflow; `420d7a1` runtime HUD assertions
- Source gates: validation 15/15; behavior 45/45; runtime transaction 5/5; deployment WhatIf 5/5; package 4/4
- Routine planner shares resource pools, material inventory, and provider cast caps across every source in one deterministic routine
- Player controls: persistent Long/Important/Short HUD buttons, setup preview/run controls, execution-mode and combat-policy controls, preview counts, and actual execution reports
- Runtime PASS: `20260811T2144351145396Z-ui-root-smoke`, assertions 11/11, 3 routine buttons, critical controls on-screen, 2 open/close cycles, 12 rendered frames, singleton root, 2560x1440
- Package/DLL/MVID: `e544b7b2940a455c9ac886237ae5d42420c8b32b6657736af06661c369406e72` / `bd3248bb56314fa68d8ecaf16761410c114d2578df24fcdd1da4cdcd1e35bdfb` / `cfa3b527-3186-4c0a-ba07-21fbaa49bf36`
- Transaction: `Restored`, `restorationVerified=true`; exact original whole `Mods` manifest restored; no save selected or modified
- Qualification boundary: this is the first of two required HUD smokes and one resolution. Campaign interaction, other required resolutions/scales, setup polish gaps, and save-backed routine execution remain open
- Exact next action: complete setup filters/sorting/provider-resource/unsupported-fallback detail and reset affordance, then repeat clean no-save HUD smokes

## 2026-08-11 — Phase 10 final no-save HUD qualification

Status: PASS for the mission-defined no-save gate; configured save-backed execution DEFER — EVIDENCED

- Branch/runtime source: `codex/kingmaker-buff-planner` at `cfce05c921735c33617342e9b409eeae556a16f9`
- Source suite: validation 15/15, behavior 46/46, harness 5/5, WhatIf purity 5/5, package 4/4
- UI completion: configured/unconfigured, duration, hidden, and source-category filters; name/level sorting; localized description/duration; unsupported saved-source display; provider caster level, remaining casts, pool and rejection reason; unfulfilled target states; bounded clear confirmation; pre/post result panels
- Instant fallback completion: sticky-touch providers are marked per step; hybrid executor uses animated fallback when allowed and fails before fire/spend when blocked
- Runtime PASS 1: `20260811T2201533093853Z-ui-root-smoke`, 15/15 assertions
- Runtime PASS 2: `20260811T2202528930958Z-ui-root-smoke`, 15/15 assertions
- Both: one final root after one forced reconstruction, two open cycles, 12 rendered frames, 3 HUD routines, 3 layout profiles (1920x1080@1.0, 2560x1440@1.25, 3840x2160@1.5), zero full-screen blockers, zero event subscriptions, actual 2560x1440 process
- Package/DLL: `c2aaac56568366e275302650dbaecd15d984158b5af4f98a670bd90b478937ba` / `961ac1a3b2c5f9194fb67413e12ba0c5e034ff2d11f2e240f16d1d02cf4e75f6`
- Both transactions: `Restored`, `restorationVerified=true`; no save selected or modified
- Qualification boundary: source behavior and no-save Phase 10 gate PASS. Configured routine firing/effects/resources remain deferred to the missing project-owned `KBP_` fixture
- Exact next action: Phase 11 exact optional-mod manifests and generic discovery profiling, beginning with locally installed Call of the Wild

## 2026-08-11 — Phase 11 compatibility foundation

Status: source and guarded-staging contracts PASS; Call of the Wild runtime catalog pending

- Branch/base HEAD: `codex/kingmaker-buff-planner` at `cfce05c921735c33617342e9b409eeae556a16f9`
- Added strict schema-1 profiles for native-only, Call of the Wild, Tabletop Added Rules, and combined configurations; unavailable local references fail closed instead of fabricating support
- Exact Call of the Wild fixture: version `1.14.4c-2.1`; 266 files; 66,201,967 bytes; full directory manifest SHA-256 `26ce134fda9a6421519959d9cc9c3f8c5d4cf3288f48ba7f768df47c7704541a`; DLL SHA-256 `4ebf8e1ed3e66ffed72ea33ea325595629423dacd5bffa23e3c9109144b26915`
- Transactional harness now verifies the read-only fixture, copies it into transaction-owned staging, re-verifies the staged copy, activates the complete staged tree, and restores the original whole `Mods` tree exactly in `finally`
- Runtime protocol binds profile ID, exact optional assembly expectations, and representative blueprint GUID expectations; results expose profile, assembly, and optional catalog counts
- Blueprint ownership is derived from the exact staged Call of the Wild `loaded_blueprints.txt` inventory without a compile-time gameplay-mod dependency
- Gates: source validation 15/15; protocol/domain 48/48; runtime transaction 6/6; deployment WhatIf 5/5; package validation 4/4; production build 1/1
- Rejected shortcut: treating the optional assembly loading as proof that its abilities were discovered or classified
- External state: no game, lock, transaction, live mod, or save mutation
- Exact next action: commit this foundation, build/package that commit, and run the guarded Call of the Wild structural catalog to derive representative blueprint assertions and audit every optional unsupported row

## 2026-08-11 — Call of the Wild diagnostic catalog

Status: PASS as diagnostic evidence; final exact-profile repetition pending

- Run `20260811T2218236669629Z-native-buff-catalog` loaded exact Call of the Wild `1.14.4c-2.1` and exact Kingmaker Buff Planner commit `0a5dd4a85ba0e58dc8b9425ced90f76829df18d3`
- Catalog SHA-256 `b943093a8edbfa9c62187a5f795ea1263291adbdc8853212c8d80676bcec9951`; 9,064 abilities, 5,907 candidates, 7,342 Call of the Wild-owned abilities, 4,937 optional candidates, 2,096 optional included, 0 optional unsupported
- Optional support classes: 2,008 automatic, 61 bounded generic-reflection wrappers, 27 explicit adapters; 5,246 excluded by definition
- Evidence-derived representatives: Dazzling Blade `0027cbfe0a484380ab76df1ad3d7326a` (automatic apply buff), Regenerative Sinew `03963bcf8dd64abea3757311c1e8a79c` (bounded wrapper), Bless Weapon `151b1f365c4217e5062a1fe50f7a63d3` (worn-item enchantment), Globe of Invulnerability `4421fff35fed4afb9ea20cbd6e6a7c0d` (area-effect buff)
- No optional adapter was added: the existing structural contracts and bounded wrapper policy classified every in-scope optional candidate
- Transaction restored exact original `Mods`; no save was selected or written; game and lock absent afterward
- The run did not yet enforce representative GUIDs or emit the required Harmony owner/order inventory, so it is retained as diagnostic rather than final Phase 11 qualification
- Exact next action: checkpoint strict representatives plus Harmony inventory instrumentation, then repeat the exact Call of the Wild profile twice from fresh processes

## 2026-08-11 — Harmony inventory runtime defect

Status: runtime FAIL repaired; regression PASS; exact restoration PASS

- Run `20260811T2226321948779Z-native-buff-catalog` failed at `unhandled-exception` because the exporter initially treated legacy Harmony12 `GetPatchedMethods` and `GetPatchInfo` as static APIs
- Exact assembly inspection proved both methods are public instance methods on `Harmony12.HarmonyInstance`; `Create(string)` remains the public static factory
- The exporter now creates an inventory-only instance and invokes the exact installed instance API; a new assembly-backed regression test calls the installed `0Harmony12.dll` and reconciles target/patch counts
- Updated source gates: validation 15/15, behavior/protocol 51/51, transaction 6/6, deployment WhatIf 5/5, package 4/4
- Failed-run transaction restoration verified exact; no save was selected or written; game and lock absent afterward
- Rejected shortcut: removing Harmony evidence or accepting an empty placeholder inventory to make the compatibility gate pass
- Exact next action: commit the repair and rerun the exact-profile qualification twice

## 2026-08-11 — Call of the Wild final repetition and native zero-patch edge

Status: Call of the Wild PASS twice; native-only runtime defect repaired

- Exact commit `fa5a525842fc095514d76da4869fe06f2d65defc`, package `e133d1002e0e0193579f98b7f1e3b104206cb9062cc2065811561ebb2bf45d39`, DLL `00a7bfa85cb8fbd867ce40094d720c1b889629324913a8b08e3b95581d4e813a`
- Call of the Wild runs `20260811T2229574550340Z-native-buff-catalog` and `20260811T2231510699549Z-native-buff-catalog` each passed 21/21 assertions and restored exactly
- Both emitted byte-identical catalog SHA-256 `357e2148c6f4610c3456a9082369fe4bea77580e0faee4de55e637c409bfc8f8` and Harmony inventory SHA-256 `a883dd60218a1f9e989a4e6b03d99318242d401d33f914cd6c068f767b308427`
- Harmony inventory: 207 targets, 228 ordered patch records; owners CallOfTheWild 225, UnityModManager 2, UnityModManager.UI 1; zero multi-owner targets and zero Buff Planner overlaps
- Native-only run `20260811T2234085703022Z-native-buff-catalog` exposed that UMM does not load Harmony when no staged mod consumes it; the catalog was written but inventory failed before result assembly
- Repair: the inventory exporter now loads only the exact installed `0Harmony12.dll` path when the assembly is absent, validates its assembly identity, and then inventories the valid zero-or-more patch state
- Failed native transaction restored exactly; no save selected; game and lock absent
- Exact next action: commit, rebuild, and repeat the native-only profile twice

## 2026-08-11 — Native profile repetition and UMM identity audit

Status: native-only PASS twice; compatibility identity gate strengthened before final report

- Native runs `20260811T2236376737183Z-native-buff-catalog` and `20260811T2237377143159Z-native-buff-catalog` each passed 10/10 and restored exactly
- Both emitted byte-identical catalog SHA-256 `0b758785ef211fcaf50ae481ea48b5da278aa0a6410ab1b034edf5edf8f3d869` and Harmony inventory SHA-256 `b5605e22bde458a238d63c6ffe33a99eb712bd22bf3cbc74c42d443ad479efb4`
- Native counts remained 1,722 abilities / 974 candidates / 413 included / 561 excluded / 0 unsupported; Harmony recorded three UMM-owned patches and zero multi-owner or Buff Planner overlaps
- Final report audit found that staged exact hashes plus assembly load did not independently prove the optional UMM entry ID/version or reject duplicate entries/assemblies
- Runtime identity validation now requires exactly one Buff Planner assembly and UMM entry, exactly one expected optional assembly and UMM entry, exact optional UMM version, exact optional assembly hash, and matching loaded counts
- Rejected shortcut: describing an assembly hash match as full proof of the UMM identity/version requirement
- Exact next action: commit the strengthened gate and repeat native-only and Call of the Wild twice on one exact commit

## 2026-08-11 — Phase 11 final compatibility qualification

Status: PASS for every locally available no-save profile; unavailable and save-backed boundaries recorded

- Exact commit `57ed740d6fd6bbdf68dcdfe8c26368c744a3c91f`, package `29ee2ddb86f6ace36d9b9ec1cc7e75a2468f81721956836e39dbdae26752deb2`, DLL `2c79eefe2afc9a93ec0574a0056c7c9331ebb1add170561f6da4c72468687b13`, MVID `18be2ec9-702a-4ba1-ae15-306765e4231d`
- Call of the Wild runs `20260811T2241040368066Z-native-buff-catalog` and `20260811T2242392501436Z-native-buff-catalog`: 26/26 each; exact UMM ID/version and unique entries/assemblies; 7,342 optional abilities, 4,937 candidates, 2,096 included, 0 unsupported; four representative contracts; catalog hash `7b54f3f9f6d90d339c4cabeedb04c9d15bcb4d51d8e7d830150a18ab6eced659`
- Native-only runs `20260811T2244134771182Z-native-buff-catalog` and `20260811T2245180617144Z-native-buff-catalog`: 12/12 each; 1,722 abilities, 974 candidates, 413 included, 561 excluded, 0 unsupported; catalog hash `50f66299912bef24a50984d9d8398ba2bb340a4f85b551a0ad6ff97c41393f3d`
- Call of the Wild Harmony inventory `a883dd60218a1f9e989a4e6b03d99318242d401d33f914cd6c068f767b308427`: 207 targets / 228 records / 0 multi-owner / 0 Buff Planner overlap; native inventory `b5605e22bde458a238d63c6ffe33a99eb712bd22bf3cbc74c42d443ad479efb4`: 3 / 3 / 0 / 0
- Every run used a fresh process, passed its compatibility-specific WhatIf/preflight, restored the original whole `Mods` tree exactly, and selected or wrote no save; final process count 0 and lock absent
- Tabletop Added Rules and combined profiles: `UNAVAILABLE-LOCAL-REFERENCE`; Shield Other: `FEATURE-NOT-PRESENT-IN-SNAPSHOT`; no external download or unrelated-project mutation
- Save-backed optional planning/execution remains `DEFER — EVIDENCED`, not PASS, because no authorized `KBP_` fixture exists
- Exact next action: commit reports and proceed through Phase 12 hardening, deterministic final packaging, final core repetitions, guarded branch publication, and handoff

## 2026-08-11 — Phase 12 release and publication tooling

Status: implementation/source PASS; clean-head execution pending checkpoint commit

- Added lab-local `codex-policy/Push-KingmakerBuffPlanner.ps1` with exact root, branch, clean-tree, unresolved-operation, single expected origin, prohibited payload, potential secret, size, current-branch-only push, and post-push SHA equality guards
- Added repository test `scripts/Test-GuardedPush.ps1`; its real WhatIf requires a clean tree and will run after this checkpoint
- Added `scripts/Build-Release.ps1`: clean-tree requirement, two deterministic local builds, package/DLL equality, release copy, allowlist validation, local-only manifest, and draft-notes copy
- Replaced future-tense README and added installation/use plus local-only draft release notes; no public release action is authorized or implemented
- PowerShell parser: 3/3 scripts zero errors; source suite: validation 15/15, behavior/protocol 51/51, transaction 6/6, package 4/4, deployment purity 5/5
- Rejected shortcut: direct `git push`; all publication must pass the lab-local project-specific helper
- External state: no game or deployment lock; no save accessed; no live mod mutation
- Exact next action: commit tooling/docs, run guarded-push WhatIf, produce and validate the deterministic clean-head release ZIP, then audit and execute final runtime gates

## 2026-08-11 — Phase 12 clean release and composed core scenario

Status: release tooling PASS; composed scenario source PASS; runtime pending checkpoint

- Guarded push helper WhatIf passed 6/6 from clean commit `4c4693b511627da0950ce470ac025f3f876bb244`, without changing HEAD/status or contacting/mutating the remote
- Clean-head release builder ran two identical builds: validated ZIP `artifacts/release/0.0.1/KingmakerBuffPlanner-0.0.1.zip` SHA-256 `2df128d74a08c0ce2dd77f7f3e784912fd291a75d02d9c9f47791619372844b5`, DLL `1904c5cb5c66999b64ebc2bbf3d4818778163553816bff7bde55326b965f6726`; publication status local-only
- Definition-of-Done audit confirmed that three separate no-save runtime gates should be composed for the final fresh-process repetition instead of counting packaging or a main-menu load
- Added `final-no-save-core`: exact identity and uniqueness, full catalog/expression reconciliation, Harmony inventory, UI singleton/reconstruction/layout/blocker/subscription checks, and normal transactional restoration in one process
- Added optional unique `-RunId`; evidence/transaction reuse is rejected before mutation. Source suite now passes validation 15/15, behavior/protocol 52/52, transaction 6/6, package 4/4, deployment purity 5/5
- Qualification boundary remains: the composed scenario is NO-SAVE and cannot prove the mandatory save-backed resource/effect/executor scenarios
- Exact next action: commit, build exact package, pass compatibility-specific WhatIf, and run the composed native core twice from fresh processes

## 2026-08-11 — Phase 12 composed core repetition and final audit

Status: all applicable no-save gates PASS; save-backed core remains unmet

- Exact clean commit `3af45f3329df300dc0616da9393480abee8547ce`, package `c2ea8a3dbbfb1cbe670be422b33e11a8afddfeaac4ae27902d69f8c5f6febc19`, DLL `a4b56a59104f5ddcac8a4184ebc4f779216f83aaeb4c0b7e199da9dcfa650413`, MVID `193901f8-0863-4622-8885-f35880e5daf9`
- `phase12-no-save-core-1` and `phase12-no-save-core-2` each passed 22/22 in fresh processes: identity/uniqueness, 1,722-ability/974-candidate catalog, zero exceptions, Harmony inventory, UI root/reconstruction, 12 frames, 2 cycles, 3 layout profiles, 0 blockers, 0 subscriptions
- Catalog hash `df2a48e61677723d1687b828d261ba4c103d4351b0f393d1e97276b84d7b8cb6` and Harmony hash `b5605e22bde458a238d63c6ffe33a99eb712bd22bf3cbc74c42d443ad479efb4` repeated byte-for-byte
- Both transactions restored exactly; final game process count 0; deployment lock absent; no save selected or written
- Added exhaustive `planning/DEFINITION-OF-DONE-MATRIX.md`. All save-backed planning/resource/effect/execution/persistence rows remain core and unmet, with source evidence separated from runtime proof
- The exact remaining safety condition is absence of a distinct `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair; every present save is protected
- Exact next action: commit the audit, rebuild and rerun the composed core twice on final HEAD, push only the feature branch through the guarded helper, then record the section 26.2 hard stop
## 2026-08-12 — live UI bootstrap recovery diagnosis and source checkpoint

Status: ROOT CAUSE PROVEN; 0.0.4 SOURCE PASS; LIVE QUALIFICATION PENDING

- Human intake superseded the prior 0.0.3 handoff: green UMM status did not correspond to a usable live campaign UI.
- Newest clean diagnostic capture: `incoming/ui-bootstrap-0.0.3-failure/diagnostics-20260812T111644855Z`; installed/release identity is exact, with DLL `5d95368e...`, MVID `f3f691a4-d691-4112-90a4-7beb9f06aad2`, and source commit `d5a20aa7...`.
- Exact UMM 0.28.2 IL and logs prove Load, toggle, active update, retained root, and an F10 screen attempt. The live phase-A record proves a fully sized/opaque/raycaster-backed root whose same-creation-frame EventSystem hit list was empty.
- Earliest common fault: immediate raycast-ownership validation after graphic construction. The screen destroyed itself with an error; the HUD used the same sequence and destroyed itself silently.
- Implemented 0.0.4 lifecycle diagnostics, full stack traces, independent update-path F10, EventBus area/scene callbacks, deferred readiness and bounded retry, and a UMM snapshot.
- Added guarded exact-save loading and physical-F10 live scenario. Baseline `afca8ac...` is immutable; Working `961c4721...` is the only selectable save.
- Gates: validation 23/23; behavior/protocol 59/59; filesystem harness 6/6; package 4/4; deployment WhatIf 5/5; warning-free exact-reference build 1/1.
- Rejected: moving icons, changing opacity, stale-DLL theory, absent UMM callback theory, and treating another detached UI build as qualification.
- Exact next action: commit this source checkpoint, rebuild from clean HEAD, then execute `bootstrap-0.0.4-native-live-1` in a fresh process.
