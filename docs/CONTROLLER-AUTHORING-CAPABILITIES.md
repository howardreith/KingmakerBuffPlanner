# Full controller planner — implementation and native evidence boundary

Active [owner mission](../planning/KBP-FULL-CONTROLLER-MISSION.md) requires ALL
three: readable workspace, real native card, controller-only authoring. Previous
mixed-input/card/editor deferrals are historical and no longer completion criteria.
Alpha4 was published for manual testing. The owner now authorizes merging and
full release0.6.0 through the existing pre-runtime publisher route. Neither
publication establishes native qualification or completion of the assignment;
F01-F10 require new native evidence. [0.6.0 notes](RELEASE-NOTES-0.6.0.md).
Frozen product69a48a4 and deterministic package/64-test frozen evidence:
[candidate receipt](CONTROLLER-UI-CANDIDATE-0.5.4-CONTROLLER.4.md).

## Implemented paths and exact boundary

No pointer coordinates, automatic Unity navigation or second Submit dispatcher.
D-pad/left stick moves semantic focus; horizontal movement changes regions.
Mapped LeftUp/RightUp actions (LB/RB on the recorded mapping) choose routines.
Confirm invokes only the focused existing control's canonical callback. Context
uses Func01 to read details; right-stick Y scrolls the focused list/read-only
view. Native glyphs describe Confirm/Back/Context/routines, including the game's
Confirm/Back swapping. Only navigation/scrolling has bounded repeat; authoring,
Run, Stop, toggles and Undo remain deliberate edges. Optional physical pointer
input uses the existing EventSystem but is not the controller route's prerequisite.

| Player operation | Source controller path | Available evidence / native gap |
| --- | --- | --- |
| Open planner | Native RT/R2 card -> direct workspace; shortcut -> quick Configure; console-book footer -> workspace | Exact installed card/VM/layer contracts; new card is NOT_RUN in-game. |
| Retained Classic | Configure -> explicit review -> second consent to one-way canonical mode switch/import; Back cancels review | Pure consent regression; canonical mode/save guards retained; native NOT_RUN. |
| Long/Important/Short | Mapped bumper actions or Header region's existing routine controls | No Run on navigation; native mapping/readability NOT_RUN. |
| All/Spells/Abilities/Other | Catalogue region, focused existing filter controls | Shared filters; focus keys survive rebuilds; native clipping/scroll NOT_RUN. |
| Search/input/clear/cancel | Search control -> owned character grid, Space/Delete/Clear/Apply/Cancel | Installed PCVirtualKeyboard requires physical typing, so it is not used; grid/model paths tested without physical typing. Rendered keyboard NOT_RUN. |
| Browse buff/complete description | Catalogue buff -> Confirm selection; Context opens full owned scrollable description/details | Same exact ability/caster resolver and blueprint description; no StaticCanvas description prerequisite. Native full-text display NOT_RUN. |
| Exact caster and source/variant | Casters region -> unit -> exact provider row | Keys contain unit/provider identities, not provider index/name; real authoring regression preserves caster; native input NOT_RUN. |
| Assign recipient/group origin | Recipients region -> explicit Confirm on legal target/origin | Existing ClickGraphRecipient toggle contract, no mutation on focus. Multi-recipient service/file test passes; native authoring NOT_RUN. |
| Inspect/change existing casting | Castings region -> casting ID -> Inspector Change recipient or caster/source -> one intended choice; Done/Back | Existing one-shot edit services unchanged; focus uses casting ID and inspector context. Native traversal NOT_RUN. |
| Recast/options/enhancements/Share | Inspector region's existing checkboxes and contextual source/availability details | Existing legality/resource services; disabled controls remain inspectable and cannot execute; no automatic substitutions. Native conditional-option coverage NOT_RUN. |
| Order/Remove/Undo | Inspector earlier/later/remove; Footer Undo | Real semantic authoring test exercises order/remove/Undo and saved records. Native input NOT_RUN. |
| Mode/strict-subset policy | Footer controls | Canonical session settings/autosave and execution admission unchanged; affected neighbors selected. Native NOT_RUN. |
| Resources | Footer/Inspector Resource details -> modal scope controls; right stick scroll; Back closes one layer | Exact top scope excludes underlying actions. Full native table/long-scroll acceptance NOT_RUN. |
| Import/problem/recovery | Inspector notices/review/problem controls; Footer Retry save/Reload | Canonical refusal/recovery/durability remains authoritative; no reload/discard just to execute. Native recovery traversal NOT_RUN. |
| Run | Footer Run -> release editor/fullscreen ownership -> focused selected-routine Run confirmation in quick surface -> separate deliberate Confirm | Fresh canonical discovery/admission/host at actual submission, no stale ticket or cast behind editing mode. Native handoff NOT_RUN. |
| Results/Stop | Existing controller quick status/Stop/scrollable Results | Alpha 3 cancellation/viewport scroll preserved; physical finite-resource checks remain later gates. |
| Back/teardown | Keyboard/details -> Resources -> casting inspection -> editor close -> quick/gameplay; native takeover/context loss cancels pending opens | Single-edge intent, root/context guards and owned transition input surface. Native lifecycle/order NOT_RUN. |

Mouse UI retains Navigation.None and native role art/materials. Controller visual
focus is a separate owned blue outline, not authored selection or hover. Focus
rebuild replaces live callbacks and restores semantic IDs with deterministic
nearby fallback; scrolling/focusing does not run discovery, save or cast.

## Rendering and entry differential

Owner says spellbook-origin rendering was worse than campaign-menu origin. No
new build-matched screenshot/log has yet established its cause. Both paths now
resolve the origin's root canvas, log host identity, and create an independent
owned workspace overlay. Workspace labels initialize unparented before attachment
so even inactive parents cannot defer TMP Awake past caller geometry. The mouse
font/role treatment is retained; controller materials are owned atlas-based
instances with inherited mask/stencil state reset, then normal LOCAL masks apply.
No shared donor/global TMP/default resolution is mutated. Per-workspace material
instances are disposed with their owner.

Bounded label creation/activation, pre-theme/post-layout, actual first/second
frame-end diagnostics include content, rect/anchors/pivot/scale/margins, preferred
and mesh bounds, glyph count/culling/alpha, font/atlas/material/keywords/masks,
canvas/screen identity. Resources first activation is sampled separately. The
controller layout gives catalogue names more room and stacks status above footer
actions; it does not shrink essential text to hide clipping. Pixel acceptance
at 1920x1200 and compact is still NOT_RUN.

## Native card and handoff

One owned InGameMenuEntityView/VM participates in the actual card collection and
navigation rows. A passive rect/image/text copy preserves native card art; no
donor component, listener, VM or animator is cloned. The icon is an owned code-
drawn ledger. The native VM's selected-card action runs during disposal, NOT on
highlight; this path never calls the shortcut's Deselect/Confirm again. Only owned
identities are removed, using native RemoveEntity against CURRENT navigation,
not restoring an old list over foreign additions. Reference MIT notice included.

After source closure an owned transition layer/visible cancel surface covers
the neutral-gated gap without fullscreen/pause ownership. Editor/run handoffs
likewise release exact ownership before their destination. Timeouts cancel,
native takeover never authorizes an overlay over the new owner, and lifecycle
cleanup cancels requests/releases only owned state. Exact live ordering remains
a gate, not established by callbacks or signature tests.

## Evidence and remaining boundary

Intake branch/HEAD e08031ba83c9b1fb5b0f167f8dc80c03e868fb6c, clean. Remote #10
matches, OPEN/DRAFT; no remote writes selected for the new mission. Foundation
e4dcbfc and card adapter 117b7f1 are preserved local checkpoints. Installed DATA
DLL is **0.1.1-rc3**, SHA-256
78407dd4c7240ce25c7654715854e83061da744a18d8eb4a2198fdffec5e3080,
MVID c0cc9cbc-8075-4d98-a32e-29972aafa7fa. It is not Alpha 3 or evidence for the
owner's new screenshot. No game/foreign KMG lease at intake; that read-only fact
is not authority to take over an actively used desktop. Fresh owner availability
confirmation was requested. Old logs/working Gunslinger/CotW fixes left untouched.

Affected integration/dependency selection: 64/0, 6.5 s reported, source47/build1/test
compile PASS; `artifacts/controller-access/full-controller-final-development.log`.
Prefixes: `controller-,planner-escape-,native-text-material-,native-source-tabs-,input-lease-,native-window-takes-,casting-workspace-campaign-bound-,spellbook-entry-,graph-,native-theme-`.
The earlier 45/0 review checkpoint and
4/0 rendering contracts and 37/0 authoring iterations remain separate evidence.
Two intermediate compile errors (routine Order assumption, Component/Canvas
signature) were corrected; logs retained, not baseline failures. Assembly-backed
checks and production focus/request algorithms are not Unity interaction proof.

Next: clean frozen candidate's focused dependency selection/package; then the
narrow guarded **no-cast** controller fixture route with current session authority,
prerequisites and exact restoration. Existing mouse scenarios cannot qualify it;
the controller-specific launcher/observer route remains an explicit gap. No live
game/desktop/staging/settings/profile/save changes; restoration NOT_RUN. No new
release/tag, branch push, PR mutation, main merge or permanent install authorized.
