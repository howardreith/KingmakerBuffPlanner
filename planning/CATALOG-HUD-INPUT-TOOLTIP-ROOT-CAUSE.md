# Catalog, HUD Input, Quick Action, and Tooltip Root Cause

Date: 2026-08-12  
Branch: `codex/kingmaker-buff-planner`  
Forensic HEAD: `d2aecb00e02a63b5ea33976e34ff1eefc9765a1a`

Status: READ-ONLY INTAKE COMPLETE; 0.0.4 HUMAN ACCEPTANCE PARTIAL/FAILED

## Exact rejected identity and human evidence

- Installed and release version: `0.0.4`.
- Release source: `5b96f3b4e713489ce677db3ac5acb83a10f80f01`.
- Installed/release DLL SHA-256: `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`.
- Installed/release MVID: `305a8a6c-2b49-4e3b-a365-286638cbfafa`.
- Release package SHA-256: `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`.
- The installed DLL and UMM cache are byte-identical to the release DLL. The release source differs from current HEAD only by later documentation.
- Human captures:
  - HUD visible/acceptable: `b80def2cf7277fa433849ebb5b77d703e4f55a290b0d96503ea990cf419020e6`.
  - Tooltip flicker/click-through: `5b2064038245dbc95d8c42159de69d5ec8838b39e707c0a6c63c6b4f91d55473`.
  - Visible planner/empty catalog: `168333a7e5100cbc3de20409a5effb1d34f847c02efb6a9b93dd27cc1e70b331`.
- No Kingmaker/UMM process, deployment lock, or non-restored transaction existed at intake.

## Preserved human PASS rows

- One Setup/Long/Important/Short row is visible and acceptably positioned above the lower-left native HUD cluster.
- F10 opens a visible, opaque, full-screen planner.
- The actual campaign bootstrap path runs.

These rows are frozen. This mission does not authorize a speculative bootstrap, modal, opacity, or icon-position rewrite.

## Earliest catalog failure

The blank panel is not an upstream discovery or default-filter failure:

1. `PlannerUiSession.Refresh` builds the party snapshot and constructs `PlannerSetupModel` before it formats the visible header.
2. The human screenshot shows `1 party/pet targets; 11 discovered buff sources; 11 providers.` The value `11 discovered buff sources` is `Model.Sources.Count`, after beneficial classification and normalization by stable ability key.
3. The first-open filter fields are neutral: empty search, neither configured nor unconfigured only, duration `0`, source kind `-1`, and hidden disabled. The installed schema-2 profile has zero hidden source IDs and retains Long -> Bless.
4. `RefreshSourceList` enumerates every source that passes those predicates and constructs a row. `RefreshDetails` binds `Model.SelectedSource`, which is initialized to the first source. An exception in either path propagates through the view constructor, disposes the screen, and triggers transactional open rollback. The human-visible screen therefore proves those bind loops returned rather than being silently converted to the surviving blank window.
5. The earliest remaining stage is post-bind Unity layout/visibility: active row count, row bounds, content bounds, viewport overlap, clipping, and selected-detail child geometry. Version 0.0.4 records none of those values, and its runtime gate accepts a screen solely from full-root coverage and a center raycast.

The repair must instrument every catalog/filter/layout count and row/detail bound before qualifying the corrected layout. It must not replace structural discovery with a Bless special case.

## Tooltip flicker and excessive width

`BuffPlannerHudButtonController` adds `_tooltip` and `_feedback` as direct children of the same `_root` that owns the `HorizontalLayoutGroup` for the four buttons. Each message is 620 pixels wide and has no `LayoutElement.ignoreLayout`. When hover activates the tooltip, the layout inserts that child ahead of the buttons and moves the source button away from the pointer. Pointer exit then hides the tooltip, moving the button back. This creates the observed enter/exit oscillation. The fixed 620-pixel top-left placement also has no canvas clamp or inward flip and explains the excessive rightward extent.

The feedback message has the same structural defect and can displace the HUD row for eight seconds after a quick result.

## Physical click-through and invalid prior proof

Exact installed Kingmaker IL shows `PointerController.Tick` reads raw mouse state and uses `PointerController.InGui` to decide whether to capture and later dispatch a world click. `InGui` delegates to `EventSystem.current.IsPointerOverGameObject()`. A tooltip-driven layout oscillation can make the KBP hit surface disappear at the raw-input sample even though a prior static center raycast succeeded.

The 0.0.4 runtime probe did not send a physical HUD click. It invoked `ExecuteEvents` directly on a preselected top-hit GameObject and then observed command counters. That proves a synthetic UI callback, not raw mouse suppression or absence of click-to-move. Human evidence invalidates its click-through PASS claim.

The narrow repair boundary is the exact `PointerController.InGui` decision, conditional only while the current pointer is inside an active KBP HUD button or full-screen planner. Outside planner-owned regions the original result must remain unchanged. Full-screen mode remains the primary modal isolation mechanism.

## Quick-action silence

- HUD quick buttons are disabled while `_session.Model` is null. The session is refreshed only when the setup screen opens, so a loaded campaign can display an installed row whose quick actions are initially non-interactable.
- Quick-result feedback uses the same layout-participating 620-pixel message described above, so a result can shift off the stable HUD location rather than provide useful feedback.
- Existing stage diagnostics record `plan revalidated` only when the completion callback returns, and classify any non-refusal result as `execution invoked`; those counters do not prove the required ordered stages.
- The installed human profile retains one Long Bless assignment and one target, while Important and Short are empty. Bless ability GUID is `90e59f4a4ada87243b7b3535a06d0638`; expected fact is `87b8c6270ea85c743afc734dfe99afee`. Provider selection remains correctly derived from the live party.

## Rejected theories

- Missing/stale assembly or bootstrap: rejected by exact identities and human-visible live UI.
- Default filters hid all eleven sources: rejected by neutral first-open state and zero hidden IDs.
- Catalog row binding threw and was swallowed: rejected by the transactional constructor call graph; such an exception closes the screen.
- Icon position, modal opacity, or F10 lifecycle require redesign: rejected by the preserved human PASS rows.
- Synthetic `ExecuteEvents` proves physical click ownership: rejected by direct human evidence and raw-input IL.

## Bounded repair

1. Add a reconciled catalog trace from raw candidates through beneficial classification, stable entries, providers, every filter, view models, GameObjects, active/visible bounds, viewport/content layout, and details selection.
2. Make first-open defaults show available, non-hidden beneficial entries; add explicit filtered-empty/genuinely-unavailable states and Reset Filters/Refresh actions.
3. Force and validate scroll-content geometry after binding; surface row-binding exceptions as visible failures.
4. Add a single cached tooltip outside the button layout, make it non-raycastable, wrap/clamp/flip it inward, and keep source hitboxes stationary.
5. Add planner-owned pointer-region capture at the exact Kingmaker `InGui` boundary, conditional only inside KBP regions.
6. Refresh the campaign session independently for quick actions and emit exact ordered stage/result feedback.
7. Replace synthetic-only acceptance with physical hover/click evidence plus command, selection, camera, row, details, Bless, duplicate, and save-safety assertions in fresh disposable campaigns.
