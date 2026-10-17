# Controller execution implementation

## Current owner correction: actual editor access and layout repair

The [access mission](../planning/KBP-CONTROLLER-ACCESS-REPAIR-MISSION.md)
supersedes the old run-only acceptance boundary below. Local candidate
0.5.4-controller.2 adds a focusable **Configure plans** action (first displayed
row, default for empty/blocked plans), using the SAME CastingWorkspaceSession
and actual CastingWorkspaceScreenView. No profile copy, hidden editor startup,
global input-mode switch or gameplay-mod dependency is introduced.

Installed TMP Awake calls LoadDefaultSettings, which can replace sizeDelta.
Controller labels now disable owned auto-container/font sizing, set margins and
final geometry AFTER activation, and borrow the font's base material rather
than a donor label's stencil material. Bounded optional diagnostics report
component/activation/final/settled geometry, bounds, masks/material clip and
1920x1200 or actual screen identity. These are not yet native-rendered evidence.
Configure, three authored counts, Stop, contextual Confirm and the essential
status need no explanation pager. Explicit View results opens a full-width
masked ScrollRect with scrollbar; navigation there does not move action focus.

Controller authoring resolves the native origin Canvas as a Component host;
mouse authoring still uses StaticCanvas and its existing boundary. The actual
workspace is constructed with an exit before acquiring ControllerEditorInputBoundary.
That adapter reuses the existing Rewired EventSystem/module, temporarily selects
the installed Unity physical-mouse fallback through PlayerMice, suppresses Unity
navigation submit, shows the OS pointer, and owns a native Back-only layer plus
FullScreenUi mode. Mouse input arms only after neutral. Text-edit Back deselects
the input field first, then existing Resources/description/casting focus handling
applies. Native base/gameplay dispatch is isolated only during owned lifetimes.
The controller-opened editor's Run explains returning to quick access instead
of submitting under fullscreen ownership; ordinary mouse Run is unchanged.

Configure and Close are queued semantic requests outside view/input callbacks.
Native Game.StartMode/StopMode can queue: cancel only this adapter's exact pending
Start delegate, and guard an owned deferred Stop against newer native ownership.
Return waits for actual Default mode and expected layer; timeout never authorizes
opening. Session edits, failed-save intent, import recovery and authoritative
FooterSaveState remain canonical. Empty runs do not submit. Nonempty/corrupt/
unresolved state is not inferred from a screenshot or overwritten.

The console SpellBookView CreateInputs/Hide and SpellBookVM.Back contracts are
separate from the mouse entry. Y/Func02 is reserved for native book switching.
An owned footer chooses RightStickButton, otherwise FuncAdditional, ONLY if
free in the book and always-bound base layer; older pushed gameplay layers are
unbound. It uses the matching native glyph, omits entry on conflict, and is
hidden/inert during Metamagic, context/modal takeover or lost ownership.
Exact owned registrations are detached on Hide/disable/unload. The source-aware
ControllerSurfaceHandoff uses VM.Back then observes real book/layer/mode release,
with campaign/area/UI guards and neutral gating, before opening the ACTUAL editor.
It is party-wide, not selected-spell authoring. Deliberate editor Close returns
to quick access; original book/selection reconstruction is not implemented.

Native card: DEFERRED until the required repaired authoring route is verified;
native collection/VM contracts were inspected, but no live donor/listener clone
audit or card/navigation acceptance occurred. No reference-project code or
assets were copied. Full controller graph editing remains DEFERRED. None of
these source changes establishes mixed-input or rendering usability in-game.

Historical execution candidate identities and evidence follow unchanged.

PR review follow-up: [three corrected defects and their regressions](CONTROLLER-UI-REVIEW-FIXES.md).
The original candidate identities below are historical, not builds of the fixes.
Current integrated frozen product is `9aec44e374b26b3561fa4402912c77a23a4e08ac`,
**0.5.4-controller.1**, focused 45/0 and deterministic local package PASS:
[current receipt](CONTROLLER-UI-CANDIDATE-0.5.4-CONTROLLER.1.md).

October 17, 2026; execution-first revision 2 of the
[owner mission](../planning/KBP-CONTROLLER-UI-MISSION.md). Controller execution
is implemented with qualification gaps. Full controller editing is deferred.
This is a local review candidate, with no installation or publication performed.

## Original baseline and checkpoint identities (historical)

Branch: `codex/kingmaker-buff-planner-controller-ui`, preserving the clean
existing mission base `2194d0f0f597ebe8b8423f91f8e1fb6b1f65fdb7` (0.5.2).
Read-only `git ls-remote origin refs/heads/main` returned
`dcf718fd7b70cf453dd25aa693fefc6aa503d826`, matching the supplied 0.5.3
branding/licensing reference. Initial intake did not replace that legitimate
mission ancestry. Later feature-branch integration preserves both histories;
stable releases/archives are untouched and PR #10 remains draft.
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
Owned entry registrations are retained by native layer/binding identity and
detached individually on stale-view cleanup or uninstall. Repeated augmentation
is idempotent; a later foreign binding is preserved and blocks replacement.
See the correction receipt for actual-native unbound cleanup coverage and the
remaining live-subscription boundary.

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
Save labels and explanations use the authoritative session `FooterSaveState`
through `WorkspaceFooterText`, including blocked imports and backup recovery.

## Refusals, results, Stop and ownership

One optional presentation argument suppresses the existing automatic editor-open
on a controller refusal. Mouse behavior keeps its default. Controller details
use the same refusal text, blocker identities/reason formatter and per-casting
outcome formatter, with paged text and a mouse repair explanation. No automatic
Reload, Retry, discard, repair or policy change is added. Completed reports list
each casting, including skips/omissions, without hover.
Reconstruction gives the session's `LastAttemptMessage` precedence over its
historical `LastRunReport`; a later completed report clears that refusal through
the existing session contract. No parallel outcome store is used.

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
