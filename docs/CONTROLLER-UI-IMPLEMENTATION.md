# Controller execution implementation

October 17, 2026; execution-first revision 2 of the
[owner mission](../planning/KBP-CONTROLLER-UI-MISSION.md). Controller execution
is implemented with qualification gaps. Full controller editing is deferred.
This is a local review candidate, with no installation or publication performed.

## Baseline and checkpoint identities

Branch: `codex/kingmaker-buff-planner-controller-ui`, preserving the clean
existing mission base `2194d0f0f597ebe8b8423f91f8e1fb6b1f65fdb7` (0.5.2).
Read-only `git ls-remote origin refs/heads/main` returned
`dcf718fd7b70cf453dd25aa693fefc6aa503d826`, matching the supplied 0.5.3
branding/licensing reference. No reset, merge, push or changes to those releases.
Entry/source checkpoint: `afa108f1fd2cc76f49f466c62bd1572f31663d15`.
The candidate is `0.5.2-controller.1` using the existing prerelease/numeric
assembly-version mechanism (CLR 0.5.2.0). No new gameplay logic follows that
entry checkpoint. The [candidate receipt](CONTROLLER-UI-CANDIDATE-0.5.2-CONTROLLER.1.md)
identifies frozen product `d04c1a781770194966fa55df15000d627e14662d`, its
deterministic ZIP/DLL/MVID and separate documentation provenance. Frozen
validation: 38/0 in 6.2 s; deterministic packaging 8.294 s. These bytes remain
NOT_RUN in Kingmaker.

## Native integration

`ControllerRoutineIntegration` adds one conflict-checked `Func01`
`ButtonJustPressed` binding to `BaseGameMenuView.GetInputLayer()` through an
optional Harmony12 postfix. An owned label and the game's own action icon appear
on the native campaign menu (the menu containing Inventory/Character/Spellbook
cards). It does not depend on the mouse HUD, Formation button or UMM overlay.
If another adapter already uses that action, this entry is omitted with a bounded
diagnostic. Missing contracts/font/icon fail softly. No native object is cloned.

The entry constructs the complete usable selector, including Back, before
`GamePad.PushLayer` acquires input. The native menu is closed through its exact
private `Deselect()` method, which calls `SetSelected(false)` then `Confirm()`;
that prevents a previously highlighted native card from also executing. The
newer planner layer remains on top while the older native menu layer is removed
by identity. This sequence is assembly-inspected; actual rendering/input is
pending. Bindings use installed Rewired/UniRx references with Copy Local false;
neither library nor any game asset is packaged.

| Selector operation | Installed mapped action |
| --- | --- |
| Native menu entry | `RewiredConsts.Action.Func01` |
| Previous/next action | `DPadUp` / `DPadDown` |
| Run selected routine or explicit Stop row | `Confirm` |
| Close after controls return to neutral | `Decline` |
| Previous/next result page | `DPadLeft` / `DPadRight` |

All operations use native `InputLayer.AddButton`, `ButtonJustPressed`; there is
no Unity submit or second polling dispatcher. Neutral detection alone reads the
game's mapped actions, including both sticks. Native Confirm/Decline swapping is
honored for bindings and icons. Physical button legends/devices/remappings have
not been qualified: the player follows the game-provided icon.

Verified installed Assembly-CSharp identity:

```text
SHA-256 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb
MVID    07fa1e4d-8618-41b3-9b8d-faa17d3b26f7
```

The existing inspector now accepts explicit additional `-TypeName` contracts and
can show constants/constructors. Private IL evidence remains in ignored
`artifacts/controller-ui/assembly-contracts.private.txt`, outside source control.
Focused assembly tests verify native entry/exit signatures, action constants,
stack bind/unbind/removal and dispatch contracts. These are detached assembly
observations, not evidence that UMM loaded in a cold controller process.

## Shared session and run path

The controller entry uses `EnsureCastingSession` and the existing
`CastingSessionOwner`/recovery factory. A first open refreshes the ordinary party
model if none exists or its campaign differs. Reopening the same campaign reuses
its session. The selector never constructs `CastingWorkspaceScreenView`, the
Classic editor or a controller copy of the profile. Mode/profile persistence and
canonical initialization/import remain unchanged. Browse/focus/reading do not
call SelectRoutine, compile, reconcile or save.

Run calls `StartCastingFirstRoutine` with controller refusal presentation.
Every attempt still performs `BuildFreshCastingWorkspaceInputs`, campaign
binding, `CastingRunPolicy.Apply`, the existing durability/reconciliation/gate,
exact projection, native dispatch boundary and `CastingExecutionHost`. Current
in-memory edits and failed-save intent remain authoritative. The controller
requests neither an acceptance token nor a different availability policy.
Canonical spellbook reconciliation at Run can legitimately archive/remove/save
castings; that is execution admission, separately covered by existing tests.
Routine focus itself does none of these things.

The selector shows authored casting counts (no affordability forecast), actual
mode, strict/subset policy and save state. Empty/corrupt/missing sessions explain
setup/recovery. A retained Classic selection stays Classic: controller execution
of Classic is not implemented. Its message directs the player to mouse/keyboard
mod settings and import review; it never silently changes mode.

## Refusals, results, Stop and ownership

One optional presentation argument suppresses the existing automatic editor-open
on a controller refusal. Mouse behavior keeps its default. Controller details
use the same refusal text, blocker identities/reason formatter and per-casting
outcome formatter, with paged text and a mouse repair explanation. No automatic
Reload, Retry, discard, repair or policy change is added. Completed reports list
each casting, including skips/omissions, without hover.

Stop is a separate fourth action, focused when the selector sees a running host.
A Run row pressed during a run explains Stop rather than reentering execution.
`RequestStop(PlayerStopReason)` preserves canonical current-cast semantics. A
Stop before the first pump can complete synchronously; its terminal report wins
over the pending-stop message. The controller overlay owns no fullscreen, pause,
selection or time-scale snapshot. It stays usable while the ordinary host pumps
in Default mode; an already paused game remains paused.

`GamePad.PushLayer` unbinds the previous pushed layer, but the native base layer
remains bound. A scoped `BindDescription.Handler` prefix therefore allows only
this selector's bindings while its layer is current and its context is valid.
This also excludes native callbacks captured before gameplay was unbound.
Native takeover is recognized immediately by the current layer identity, before
the next UMM update. Removal of an older layer never restores a saved native
stack or fullscreen state. With the selector closed, this prefix admits native
dispatch unchanged.

`ControllerActivationLatch` requires a neutral frame before an operation, rejects
same-frame callbacks and rearms only after release. Back retains the visible
exit surface until controls are still neutral on the following frame; a new
press during that window restarts it. No held Run can immediately become Stop.
Callbacks queue one semantic operation; the owner processes it outside native
input dispatch. All navigation uses edges, so repeat is bounded by deliberate
press/release.

Close, native takeover, focus loss, controller absence, loading, area/campaign/UI
context change, disable and unload dispose the owned layer/view. Failed
construction removes only its partial layer and uses finally for view cleanup.
The model tests cover ownership and takeover at the external native-stack
boundary; actual native lifecycle/disconnect/restoration observations are pending.

Idle work is bounded: context checks, mapped neutral reads and host state. Native
host discovery occurs in its menu construction callback; font lookup is scoped
to that native view. Discovery occurs at first required session binding and
fresh Run, not per frame or navigation. Rendering refreshes on operations,
result/run/pause changes, not idle discovery/layout rebuilding.

## Validation and limits

Ordinary development used the registered prefixes recorded in ignored
`artifacts/controller-ui/prefix-selection.json`, through:

```powershell
.\scripts\Test-Development.ps1 -Only 'controller-,execution-policy-,production-apply-end-to-end-through-host,host-stops-between-castings,host-player-stop-,autosave-,failed-flush-,casting-workspace-campaign-bound-,native-window-takes-,planner-hotkey-,input-lease-,classic-file-and-hud-,refusal-feedback-,casting-world-clock-,first-open-import-,hud-routine-buttons-,planner-escape-'
```

Reviewed pass: source validation 43/0, production build 1/0, current test build
successful, behavior 38/0 in 6.5 seconds (6.73 seconds child wall time).
There are 13 controller tests, plus 25 admission/execution/durability/session/
mouse-boundary neighbors. An earlier focused pass was 21/0 in 6.2 seconds;
intermediate new-code compiler errors were corrected before these passes. No
baseline suite failure was reclassified. The final frozen-product validation
and deterministic package identity are recorded in the delivery receipt.

Tests use real owned temporary profile files, authoring, session owner, compiler,
gate, policies, projection, dispatch boundary, host and executors. Only device,
native stack and actual game effect/resource boundaries are replaced. Scripted
effects/spend assertions prove downstream behavior, not actual applied Kingmaker
buffs or controller mapping. Native startup, screenshots, actual resources,
physical input isolation, resolution and human usability remain unqualified.

On DATA the owner connected USB hardware; a read-only Windows PnP query found
`Xbox 360 Controller for Windows` and `HID-compliant game controller`, both OK.
A foreign game/lease was observed and left untouched; a brief released window
was followed by another active run. At 16:48:33Z the read-only audit recorded
game PID 29980 and the foreign lease, with all 562 Buff Planner transactions
Restored and no unresolved project transaction. No project runtime
launch/staging was attempted. Existing
manual/physical harness scenarios open/drive mouse surfaces and cannot be
relabeled controller evidence. Controller-specific guarded observation and
allowance selection are remaining qualification work; see the
[acceptance procedure](CONTROLLER-UI-ACCEPTANCE.md).

Rejected approaches: global editor navigation changes; hidden mouse-editor
initialization; a second saved plan/executor; mouse hotkey mapping as the feature;
fullscreen ownership for Stop; treating PushLayer alone as base-layer isolation;
substituting button callbacks/screenshots/Windows device detection for physical
controller execution. No optional editor work was started.
