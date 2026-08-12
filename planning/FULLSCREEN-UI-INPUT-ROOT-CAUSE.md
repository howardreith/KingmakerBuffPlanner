# Full-Screen UI and Input-Isolation Root Cause

Status: CONFIRMED — 0.0.1 UI acceptance invalidated; replacement implementation not yet qualified

## Baseline and human evidence

- Starting branch/HEAD: `codex/kingmaker-buff-planner` / `ec153837401c8815b1909cb15e85ab658a1ee26a`
- Starting version: `0.0.1`
- Installed DLL SHA-256: `3d356e6b1dbf422b4aa8e721fb4d71051dc10c2097547473bbfdd135b8c11be8`
- Current-overlay screenshot SHA-256: `6a1b6de2d98e68731ef6d695d69facb52497c77fcdfb16cefff82905e44000ba`
- HUD reference screenshot SHA-256: `c3c55e9ad0b295094c78d547410e4f808baba16ea9f95e2b554261ecbbfc7ea8`
- Full-screen reference screenshot SHA-256: `cec431dbe2ca1809b37ce1e7d2e5dcd72bcbfe9cd8658d6a40c8d76adbadaf2a`

The first screenshot and direct human observations are authoritative product evidence. The other two screenshots are usability references only; their artwork, assets, and Wrath-specific implementation are not reusable.

## Confirmed 0.0.1 causes

1. `BuffPlannerUiRoot.OnGUI` draws the setup and routine controls as fixed immediate-mode screen rectangles. It creates no Unity UI `Canvas`, `GraphicRaycaster`, raycast-target graphic, or EventSystem pointer surface and never enters a native service-window lifecycle.
2. Exact installed `Assembly-CSharp.dll` inspection (SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`) shows `Kingmaker.Controllers.Clicks.PointerController.InGui` delegates to `EventSystem.current.IsPointerOverGameObject()`. An IMGUI rectangle is therefore invisible to the game-world pointer controller. World click handling, selection, and drag handling can run beneath the planner.
3. `BuffPlannerUiRoot` never enters `GameModeType.FullScreenUi`. Native `FullScreenTabsWindow.OnShow` raises `IFullScreenUIHandler.HandleFullScreenUiChanged(true, type)`. `StaticCanvas.HandleFullScreenUiChanged` then calls `Game.StartMode(FullScreenUi)`; its false path calls `Game.StopMode(FullScreenUi)`. The mode stack deactivates the previous mode controllers and activates only the new mode's registered controllers.
4. `GameModesFactory.Initialize` registers the default world `PointerController` only for `Default` and `Pause`, not `FullScreenUi`. This is the native world-command isolation boundary that 0.0.1 omitted.
5. `ServiceWindowTabs.OnShow` enables its `CanvasGroup.blocksRaycasts`; `OnHide` disables it and restores HUD visibility. `StaticCanvas.SetHUDVisible(FullScreenUi)` selects the hidden HUD state. The old planner omitted both the mode boundary and the raycast defense.
6. Native `CameraZoom.UpdateInputFromMouse` reads the mouse wheel directly. The repair therefore cannot rely solely on pointer-over-UI; it must use the full-screen mode and an explicit planner event surface that consumes scroll and drag.
7. Quick execution has no HUD-visible result channel. An empty routine legitimately produces a zero-step plan, but `PlannerUiSession.ExecuteRoutine` reports the result only through `Status`, which 0.0.1 renders inside the closed setup window. From the HUD, `Long` can therefore receive its callback and still appear to do nothing. There are also no stage counters proving pointer receipt, listener invocation, group resolution, revalidation, execution/refusal, and result presentation.

## Why the former automated gate was false confidence

`BuffPlannerUiRoot.EndRuntimeSmoke` returned hardcoded/derived presentation counters without dispatching real pointer events or observing world-command boundaries. Runtime validation explicitly required `FullScreenBlockerCount == 0` and `EventSubscriptionCount == 0`. Those expectations certified the absence of the two mechanisms now required by human evidence. The gate also hardcoded `RoutineButtonCount = 3`, inferred layout from arithmetic, opened `_open` directly, and never proved visible controls, listener uniqueness, native hierarchy anchoring, input suppression, opaque coverage, state restoration, or quick-action feedback.

## Selected replacement boundary

The narrow native lifecycle is `GameModeType.FullScreenUi`, acquired and released through an explicit idempotent planner-owned lease, paired with a full-screen Unity UI raycast surface. The screen will be a standalone service-style window rather than a brittle patch into Inventory or Spellbook. HUD controls will attach to a runtime-verified bottom-left native hierarchy anchor, clone only safe game-native button chrome where available, and use original planner icon content. No BubbleBuffs assets or Wrath implementation will ship.

The runtime hierarchy/prefab anchor and exact restore invariants remain subject to the guarded native UI probe and will be recorded before the production controller binds to them.
