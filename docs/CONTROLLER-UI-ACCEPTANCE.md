# Controller execution acceptance

Review follow-up: [entry cleanup, persistence and latest-outcome corrections](CONTROLLER-UI-REVIEW-FIXES.md).
No live status is promoted by these source regressions. The next native test is
the entry/exit handoff only, before any casting.

October 17, 2026. Scope is the
[execution-first revision 2](../planning/KBP-CONTROLLER-UI-MISSION.md).
Support level: **implemented with named qualification gaps**. Cold-controller
startup and physical-controller execution have **not been demonstrated**.
Additional usability is limited to paged per-casting results, blocker reasons,
setup/recovery notes and visible semantic focus in the new selector (source-tested;
rendering unqualified). Full controller editor: **DEFERRED**.
Owner-authorized [alpha publication](evidence/controller-alpha-0.5.4-controller.1.md)
reuses the existing candidate without new tests. Stable controller release and
permanent installation are **not authorized or performed**; live statuses below
are unchanged pending owner testing on another PC.

## Identity and environment

Working base: `2194d0f0f597ebe8b8423f91f8e1fb6b1f65fdb7`, existing 0.5.2
human-feedback branch, continued on `codex/kingmaker-buff-planner-controller-ui`.
Entry/source milestone: `afa108f1fd2cc76f49f466c62bd1572f31663d15`.
Current candidate: **0.5.4-controller.1** (CLR 0.5.4.0), frozen integrated
product `9aec44e374b26b3561fa4402912c77a23a4e08ac`, focused 45/0 and
deterministic package PASS. Its [receipt](CONTROLLER-UI-CANDIDATE-0.5.4-CONTROLLER.1.md)
records exact paths/hashes/MVID. Main's 0.5.3 branding/licensing/publisher changes
and the three PR corrections are included. The original `d04c1a7` /
[0.5.2-controller.1 receipt](CONTROLLER-UI-CANDIDATE-0.5.2-CONTROLLER.1.md)
and ZIP remain historical and unchanged, not selected for the next live run.
Both candidates are NOT_RUN; no historical runtime verdict applies.

DATA, Windows PowerShell 5.1, configured Kingmaker 2.1.7b references, legacy
Harmony12, C# 7.3/.NET Framework 4.7. Assembly-CSharp MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Owner connected a USB controller;
Windows reports `Xbox 360 Controller for Windows` and the HID game controller
as OK. Game recognition/mapped input on this candidate remains unobserved.
No virtual controller was used. No game resolution was changed or qualified.
The 16:48:33Z read-only audit found foreign Kingmaker PID 29980/lease active,
all 562 project transactions Restored and no unresolved project transaction;
`artifacts/controller-ui/runtime-intake.json`. No foreign state was changed.
Follow-up read-only audit at 19:51:18Z found no game or foreign lease, 562
transactions with no unresolved transaction and no pending protected-save review;
`artifacts/controller-ui/review-runtime-intake.json`. This is not a Steam/session
preflight or live restoration PASS.

## Primary matrix

Statuses below describe the required observation, not merely its source code.
PARTIAL means the documented offline slice passed while live observations remain.
BLOCKED identifies a missing live qualification route/prerequisite, not a failed
product observation. No runtime PASS is claimed.

| ID | Status | Available evidence / outstanding observation |
| --- | --- | --- |
| E01 | BLOCKED | Cold session owner loads the saved file without a workspace view in offline tests. Need fresh native-controller process, UMM Load/update and physical entry with editor-open count zero. |
| E02 | BLOCKED | Native entry/action/exit assembly contracts and focus model pass. Need physical open, all-three navigation with no run, deliberate confirm, empty/missing state and Back. |
| E03 | PARTIAL | Each saved routine reaches canonical projection/host with exact caster/book/ability/target in tests. Need real buffs and finite before/after resources for each. |
| E04 | PARTIAL | Real gate/executors preserve strict/subset, both modes, combat and save refusal. Need representative native execution/refusal/omission observations and zero spend on refusals. |
| E05 | PARTIAL | Real animated host finishes its in-flight cast then stops later casts; immediate Stop preserves terminal report. Need physical Stop while a suitable native run remains active; no artificial delay. |
| E06 | PARTIAL | Canonical report entries/omissions/blockers feed paged controller text; refusal suppresses editor construction. Reconstructed latest-refusal precedence and authoritative persistence status pass regressions. Need rendered readable pages and physical dismissal. |
| E07 | PARTIAL | Neutral-edge/transition and owned-binding/base-layer/native-takeover tests pass. Need correlated input plus world/camera/radial observations, including held Back/Run and reopen. |
| E08 | PARTIAL | Browsing preserves exact file bytes/timestamps, document and authoring selection; current/failed edits use canonical durability. Need mouse authoring -> supported controller switch/restart -> unchanged real execution, plus cold process. |
| E09 | PARTIAL | Context/ownership cleanup, retained-layer unload/reinstall and actual-native unbound entry detach pass offline regressions. Need live subscription removal and actual reopen/area/save/window/focus/device/toggle cycles and stale-campaign checks. |
| E10 | PARTIAL | Shared exact-source projection and policy/admission neighbors pass. Need native available/unavailable enhancement/resource cases. Optional-provider/device-family coverage unrun. |
| E11 | PARTIAL | Existing HUD/hotkey/escape/lease/Classic/session neighbors pass; mouse navigation/highlight code unchanged. Need fresh no-controller mouse startup and ordinary editor/hover observations on candidate. |
| E12 | BLOCKED | Owned selector/result layout implemented. Need screenshots and human review at owner resolution and supported compact/windowed configuration. No pixel/clipping claim from source. |
| E13 | NOT_RUN | No mission-owned live transaction, settings/profile/save mutation or staging occurred. Restoration outcome is NOT_RUN, not PASS. Every future run must prove all existing save/install/settings/lock restoration assertions. |

Historical C-series evidence stays under its original IDs/artifacts. Mapping:
E01/E02/E07/E09/E12 require new controller input/surface evidence; E03–E06/E10
reuse canonical services but require new controller-path gameplay observations;
E08/E11 require new workflow/regression observations; E13 requires each new run's
own restoration receipt. No historical C result is promoted or relabeled here.

## Guarded physical qualification procedure

The owner's offer of DATA/controller access persists. No repeated permission for
that hardware is needed. Exclusive use, current safety prerequisites and any
larger validation budget still need to be selected at the actual boundary.

1. Preserve the exact execution candidate and clean product checkout. Re-check
   both labs' leases, game/Steam processes, unresolved transactions and protected
   save review state. An observed foreign lease/game is never ours to stop.
2. Prepare a narrowly scoped controller observation/allowance extension to the
   existing repository launcher and request/observer protocol, with positive and
   negative tests. Existing `live-workspace-manual` waits for an open mouse
   workspace; `live-workspace-physical` drives mouse/hotkey authoring and scopes
   its allowance to that scenario. Neither currently proves E01–E13 or grants
   controller casts. Do not just rename them or remove their session locks.
3. Read the actual launcher, RuntimeAutomation/RuntimeHarness helpers, deployment,
   fixture and restoration child calls before selecting that extension's live
   route. Preserve source/safety and WhatIf purity on the frozen source and exact
   package. Existing full-history purity previously exceeded the ordinary
   30-minute ceiling; no matching receipt exists for this product. Estimate the
   newly selected route and obtain a separate budget selection if it exceeds
   policy. No old seven-hour campaign is selected by this document.
4. Use only authorized `KBP_AUTOMATION_WORKING` and its immutable baseline,
   transactional profile/settings protection and run-bound exact plan/mode/
   routine/submission allowances. Arrange mouse/keyboard authoring once in that
   owned profile, recording all exact castings/settings and expected finite
   resources. Close the editor. Never substitute an ordinary save or owner seed.
5. Verify the game's supported controller-mode switch/restart through local
   contracts and the guarded session. Do not change global settings, bindings,
   drivers or loader configuration. Cold-controller proof needs a separate fresh
   process if the switch sequence did not establish it. Record Main.Load,
   callbacks, native mode, campaign, session and zero editor opens before entry.
6. First run is [entry/exit only](CONTROLLER-ENTRY-EXIT-PLAN.md), no casting:
   fresh native-controller startup -> native campaign menu -> Buff Planner ->
   older menu closes while planner stays visible/current -> navigate -> Back ->
   neutral -> normal gameplay input. Correlate physical mapped input, frames,
   current layer, OnUnbind ordering and zero host starts. Stop here if it fails.
7. Only after that passes, separately select the authorized finite-resource cases
   for all three routines. Observe actual buffs/spend, not dispatch counters.
   Include strict/subset and both engines, combat/unresolved/failed-save refusal,
   a valid/unavailable enhancement, existing-effect skip and a suitable active
   animated Stop. Hold and repeat controls, Back and reopen. While the selector
   owns input, movement/camera/radial/pause must not react. A paused run waits;
   Back releases the selector so native pause controls can be used normally.
8. Batch reopen, native takeover, focus, area/save, disconnect/reconnect and
   disable/re-enable checks where compatible. Check later durable mouse edits
   and browsing without profile writes. Test fresh mouse startup separately.
   Capture owner/compact layout without changing global display settings.
9. End each transaction through the existing protected-save/profile/settings/
   Mods restoration and lease release, inspecting all receipts before another
   run. Stop on unknown Steam/account/cloud/update/credential/purchase UI or any
   failed restoration. Record PASS/FAIL/BLOCKED only in machine schemas; use
   PARTIAL/NOT_RUN here as appropriate. Human review remains distinct.

No runnable controller live command is claimed until step 2's protocol exists.
This is an explicit qualification gap of the delivered source-tested candidate,
not an instruction to deploy it manually or launch an old scenario. The smallest
owner-dependent next action is review of the preserved execution candidate and
selection of an exclusive, separately budgeted guarded controller qualification
session after that protocol preparation. Optional editor work remains deferred.
