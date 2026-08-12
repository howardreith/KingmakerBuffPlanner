# HUD, Modal, and Execution R2 Root Cause

Current handoff: validated 0.0.3 is installed for authoritative human retest. The status line below records the earlier implementation checkpoint.

Status: CONFIRMED — 0.0.2 human acceptance invalidated; 0.0.3 correction implemented and awaiting guarded runtime/package qualification

## Exact rejected installation

- Version: `0.0.2`
- DLL SHA-256: `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`
- MVID: `e43f060b-a2b7-48db-b19f-b45704ef77c4`
- Source/release commit: `447bbd288c803a4aec609db84a4c6076cbfe94f3`

Human screenshots are authoritative. Their SHA-256 identities are recorded in the journal. They prove native HUD/tooltip activation beneath the four planner controls and a hidden-HUD/input-suppressed F10 state with no rendered planner.

## HUD failure

0.0.2 instantiated the entire private formation `ButtonPF` hierarchy four times. Removing known tooltip triggers did not bound unknown child graphics, components, or native hit geometry. The only planner-owned visible icon explicitly had `raycastTarget=false`. The row was positioned from one native button's anchored position and extended right through native turn-based/pause regions. No check required the visible center of each icon to be the first EventSystem raycast hit.

0.0.3 creates four fresh `Image` + `Button` objects, with no cloned `ButtonPF` hierarchy. Their exact order is `Setup | Long | Important | Short`. The row's bottom is derived from the actual native cluster parent top edge. Installation fails closed unless the active native `GraphicRaycaster` exists, inherited canvas groups permit interaction, every visible center resolves first to the corresponding planner button/child, and the row is geometrically above the native cluster. The runtime probe temporarily instruments all native buttons in that cluster and requires zero underlying activations.

## Invisible modal failure

0.0.2 treated an input lease as the screen state. `Open()` acquired `FullScreenUi` and disabled selection before refreshing the campaign model, checking `StaticCanvas`, constructing the view, forcing layout, or checking rendered visibility. Its view flags only described component configuration at construction time; they did not prove nonzero geometry, screen coverage, active hierarchy, top sorting, or an EventSystem hit. This ordering directly permitted an invisible locked state.

0.0.3 uses explicit states `Closed`, `OpeningPresentation`, `AcquiringInputLease`, `Open`, `Closing`, and `FaultedRollback`. A dedicated top-level screen-space overlay canvas is constructed, activated, laid out, and validated before the lease factory is called. Validation records the complete hierarchy/canvas/RectTransform/corner/screen/CanvasGroup/background/raycaster/EventSystem/top-hit contract and requires at least 98% screen coverage. It is repeated after native mode acquisition and periodically while open. Any constructor, validation, acquisition, or later visibility failure destroys the presentation, releases only an actually acquired lease, and returns to `Closed`.

## False execution success

0.0.2 added `Fired` immediately after `UnitUseAbility` was added to the command queue. Its quick result could say complete when no failed record existed even if no expected effect was observed. A planner click that also paused the game could therefore expose a queued command as “fired.”

0.0.3 distinguishes `Queued`, `Submitted`, `CastStarted`, `ResourceSpent`, `EffectConfirmed`, validation/submission/execution failures, and `TimedOutUnconfirmed`. Animated commands are only started when the native command reports `IsStarted`; instant rules are submitted explicitly. Both engines use a bounded post-lifecycle observation window. Only `EffectConfirmed` contributes to applied success, and routine completion requires every planned step to be confirmed with no terminal failure. Provider, ability, targets, resource pool/tokens, available-cast delta, expected effect GUIDs, and terminal detail are logged per record.

## Actual human profile and Bless

- Profile: `Mods\KingmakerBuffPlanner\UserSettings\kingmaker-buff-planner-a9d1cfe775e8e5830e55ab18.json`
- SHA-256 at intake: `3723e3181c56bff6427a15b2ba85ffd76fd40e98f3f482253b15910f038d6b48`
- Campaign ID: `11e251be-321b-47bd-ad3e-6d221cccd081`
- Schema: 2; no migration ran; primary loaded normally; no entry was discarded or remapped
- Long: one assignment
- Bless ability/source GUID: `90e59f4a4ada87243b7b3535a06d0638`
- Wanted target: `8d7086b2-a4d5-43d5-aed6-51c789971b53`
- Expected native fact: `BlessBuff` / `87b8c6270ea85c743afc734dfe99afee`
- Provider controls: none saved; automatic provider selection is intentionally derived from the current party snapshot

The prior `.bak1` contains the same Bless assignment with no wanted target. The primary therefore preserves a later target edit; schema migration did not create or lose it. The automatic provider chosen during the failed human run was not persisted or instrumented by 0.0.2 and cannot be truthfully reconstructed after the process ended. The 0.0.3 plan/outcome diagnostics record it on the next execution.

## Qualification boundary

Deterministic source behavior currently passes. Campaign visual/input and Bless resource/fact proof still require a campaign process. No project-owned `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair exists, so guarded automation must not open the valued human campaign. The validated 0.0.3 package will be installed for authoritative human retest after applicable no-save and packaging gates pass.
