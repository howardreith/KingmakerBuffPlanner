# Mr. Bard's Kingmaker Buff Planner — Controller Execution First

Owner mission, revision 2, October 17, 2026. Repository:
`howardreith/KingmakerBuffPlanner`. Audience: Claude Code or Codex in the
existing Windows Buff Planner lab. This task-specific record preserves the
requirements of the supplied revision; the conversation retains its original
wording. It supersedes the earlier mission's contradictory full-editor scope,
sequencing and acceptance requirements. Repository validation, save protection,
runtime guards and publication boundaries remain authoritative.

## 1. Success and scope

Implement, test and deliver controller execution of existing saved routines
first. Mouse/keyboard plan creation is acceptable. A player in PC Kingmaker's
native controller mode must reach a discoverable selector from campaign play,
deliberately execute Long, Important or Short through the mouse interface's
canonical validation/execution services, request Stop during a run, read
completion/refusal information, dismiss the interface and continue by controller.

Cold startup matters: an existing campaign profile must bind in a fresh process
started in controller mode without opening the mouse editor in that session.
Mouse initial authoring is permitted; mouse initialization every controller
session is not the solution. Controller-to-mouse emulation or mapping
Ctrl+Shift+B is diagnostic assistance, not the everyday feature.

A tested execution-only implementation satisfying primary acceptance is a
complete controller execution feature eligible for owner review. A source-tested
candidate with named runtime gaps is useful but unqualified for those gaps.
Neither is full controller editor support. Initial qualification is limited to
the authorized Windows lab, the game-recognized controller actually tested and
its observed mapping. Console, Steam Deck, Linux, other devices and remappings
need separate evidence.

Secondary work follows the preserved execution milestone: clearer routine/status
presentation, readable results/explanations and bounded read-only information or
navigation. Full graph editing, search/text entry and virtual keyboards are
optional follow-on work. They must not consume or destabilize execution. A
substantial editor project may be deferred with a concrete support boundary.

## 2. Authority and safety

Authorized: scoped source changes, focused tests, local builds, coherent local
commits, documentation, read-only remote inspection/fetch and a validated local
candidate archive. Continue a legitimate existing mission branch or use a clearly
named descendant such as `codex/kingmaker-buff-planner-controller-ui`.

Not authorized: push, remote PR creation/editing, merge, tag, publication,
permanent installation or replacing the owner's normal installation. An older
release authorization does not transfer to this mission.

Inspect branch, exact HEAD, worktrees, staged/unstaged/untracked work, processes
and transactions. Preserve unknown work. Do not reset, clean, force-checkout,
silently stash, force-push or replace branches destructively. Isolate worktrees
and outputs if needed. Work only on Buff Planner; do not modify Mounted Combat,
Gunslinger/Tabletop Added Rules, Expanded Summoning or another mod, and do not
interrupt another agent or game process. Reuse cross-lab locks and leases.

Runtime requires existing prerequisites and owner authorization for the selected
machine/session. Do not take over an active desktop. Without a safe authorized
session, continue offline and report the exact blocked checks. Only project-owned
guarded scripts may stage/deploy/launch: preserve source/safety checks, WhatIf
purity, exact identities/package validation, Steam preflight, locking,
transactional staging, rollback and exact restoration. Never hot-copy a DLL.

`KBP_AUTOMATION_BASELINE` is immutable. Only the currently authorized
`KBP_AUTOMATION_WORKING` fixture may be mutable. Protect ordinary saves, owner
seeds, unrelated fixtures and real UserSettings; no arbitrary campaign save or
writable hard-link to a protected original. Temporary settings/profiles belong
inside transactional protection/restoration. Recover failed transactions through
their established protocol before further runtime work. Never acknowledge a
protected-save violation for the owner.

Stop on unexpected Steam/account/cloud/update/credential/purchase/unknown UI.
Do not change UMM, loader configuration, Steam bindings, global display settings
or drivers without separate permission. Routine successful implementation steps
need no repeated approval. Missing hardware blocks physical qualification, not
safe source work.

## 3. Intake and contract discovery

Read AGENTS.md and docs/VALIDATION-POLICY.md first, then current README, release
notes, Version.props/project, player guide and relevant resume/blocker/journal
records. Historical current banners neither restart old campaigns nor authorize
live use. Read actual runtime entry points and nested calls before selecting
live checks. Record the execution-first correction in the current handoff.

The preparation snapshot was main
`dcf718fd7b70cf453dd25aa693fefc6aa503d826`, release 0.5.3 (October 15),
Mr. Bard's Kingmaker Buff Planner, assembly/UMM ID
`KingmakerBuffPlanner.dll` / `KingmakerBuffPlanner`, Kingmaker Enhanced Plus
2.1.7b, C# 7.3/.NET Framework 4.7, legacy Harmony12. This is a reference,
not a reset instruction or new runtime audit. Preserve legitimate later work;
0.5.3 branding/licensing retained the 0.5.2 planner and did not add qualification.

Inspect current symbols around BuffPlannerUiRoot, CastingExecutionHost,
session binding, run policy, HUD command handling, input and persistence.
Earlier observations to verify locally:

| Preparation observation | Consequence |
| --- | --- |
| PlannerNativeKit buttons/checkboxes use Navigation.Mode.None | Full-editor navigation is separate. |
| KingmakerUiFactory disables navigation for correct mouse hover | Do not globally undo that fix. |
| Mouse HUD discovery includes Formation | Verify actual controller hosts. |
| Existing input boundary uses canvas/selection/fullscreen contracts | Raycasts do not establish controller isolation. |
| Player guide defines profiles, autosave, exact choices and run policy | Reuse these contracts. |

Distinguish inaccessible UMM settings, missing planner UI, missing update
callbacks and actual loader failure. A controller-startup UMM problem was raised
but not demonstrated. Do not speculate about a universal loader blocker or
change loader settings.

## 4. Product and input invariants

Use one campaign-keyed profile, authoring/session, persistence, compiler/resource
allocator, admission and execution path. No controller profile copy, JSON writer,
second discovery system or executor. Retain standalone identity and optional
integration boundaries. Initialize the correct session independently of mouse
editor construction; do not invisibly construct that editor as initialization.

Use latest valid session intent and the existing durability barrier. Unsaved or
failed-save changes must not cause a stale disk run. Switching input mode must
not discard edits, reset settings/enhancements/policy, change campaigns or create
another profile. Preserve exact caster/source/variant, targets/group origins,
enhancements, recast/skip behavior, shared resources, authored order, strict/subset
policy and Instant/Animated. No automatic substitutions, waived prerequisites,
combat exception or fallback engine.

Keep canonical import/reconciliation at appropriate initialization/run boundaries,
with archives, notices and unresolved/unsaved refusal. Navigation, highlighting
and reading must not author/save/spend. Test legitimate reconciliation changes
separately; never add them to per-frame rendering or call them browsing. Retain
Classic until deliberately changed. Follow its correct route where feasible;
otherwise explain mouse-based conversion/setup. A controller Classic editor and
legacy conversion controls are not required.

Use verified native input/mode indicators, mapped action IDs, confirm/cancel
conventions and dispatch order. Do not invent Rewired/Wrath APIs, device indices
or a newer Unity input dependency. Use one dispatcher. Opening or selecting does
not Run. Run/Stop/Confirm are edges; only navigation may repeat at a bounded rate.
Latch opening/closing inputs until neutral to prevent Run->Stop or world actions
from a held press.

Acquire ownership only for a usable surface with controller exit. Release on
close, failed construction, native takeover, focus loss, disconnect handling,
scene/session changes, disable and unload. Restore only owned state; never
overwrite a newer native owner with an old snapshot. Movement, camera, selection,
radial/action menus, pause and other native actions must not also consume owned
input. Closed integration leaves unrelated input alone except its verified
contextual entry. EventSystem selection, EventData.Use and raycasts alone are
insufficient isolation evidence.

Respect native mode switching, whether live, menu-mediated or restart-only; no
global mode switch. Navigation works while paused; execution preserves canonical
pause behavior. Preserve mouse HUD/hotkey/spellbook, graph, continuous assignment,
one-shot edits, descriptions, search, Resources and the single hover highlight.
No redesign or Save/Accept ceremony.

## 5. Stage A — bounded entry proof

Inspect exact local assemblies and logs first: cold UMM/Main.Load/update/enable,
campaign binding without editor, dependencies on HUD/spellbook/theme/StaticCanvas;
input owner, mappings, mode, confirm/cancel, order, menu host/fullscreen ownership
and supported mode lifecycle; narrow suppression of underlying gameplay and
exception-safe release. Inspect only execution's needs first.

Build a visible production-path surface with focused routine navigation, one
confirm and Back. Prefer a suitable native controller menu. If extending one
would be invasive, use a small owned menu with a documented, discoverable,
conflict-checked entry action/chord. Do not require mouse HUD or UMM access. A
direct test callback is not controller wiring. If loading genuinely fails,
record the earliest demonstrated external failure and prerequisite; no loader
replacement. Continue verified offline work.

Checkpoint A: commit bounded integration and focused tests; record startup,
input provenance, session initialization, ownership and remaining uncertainty.

## 6. Stage B — execution feature

Provide legible Long/Important/Short, active-run Stop, status/results, unmistakable
focus and contextual hints; Back exits. Distinguish focus from running state.
Show selected routine and actual policy/mode. Use real read models for any counts;
omit stale/partial affordability previews. Empty/missing profiles must explain
mouse/keyboard setup and permit return. Do not create guessed plans or offer a
Setup action that traps the controller player in a mouse-only editor.

Each routine reaches fresh canonical admission/coordinator/host state. Never run
all routines, change order, mint a second acceptance token or bypass a disabled
action. Do not require editor initialization after launch/load/mode changes.
Avoid repeated discovery on idle/navigation/menu reopening; use appropriate
invalidation. Serialize execution and retain combat, latest-save and identity
checks. Holding Confirm or rapid reopen must not duplicate spend.

Reuse refusal/report/omission data. Essential reasons and details must be readable
without hover. Adapt the narrow refusal presentation boundary so a controller
request cannot open an unusable mouse editor. Retain the same admission decision,
diagnostic detail and safe dismiss route; explain later mouse repair. No automatic
reload, discard, repair or weakened gate. Preserve Retry save/Reload safeguards.

Keep Stop reachable while working without suspending the executor or retaining a
fullscreen/pause lease. Preserve canonical in-progress-cast behavior: later casts
stop; no refund or rollback of completed buffs. Release input at appropriate
boundaries; held Run cannot become Stop or operate underlying native UI. Rapid
completion is normal, with no artificial delay.

Handle repeated open/close, area/save changes, native takeover, focus loss,
disconnect/reconnect and disable/re-enable fail-softly with bounded diagnostics.
Commit/review the coherent execution milestone and preserve its exact product
commit and local artifact before optional editing. Capture available evidence;
name blocked physical checks. Optional changes need later commits or isolated
outputs and cannot inherit binary runtime PASS.

## 7. Stage C — bounded usability and optional editor

Prioritize useful result explanations, efficient selection, read-only resource
information, safe verified Setup handoff or straightforward navigation. Do not
expand to superficial full support. Graph/text/source/recipient/modal redesign
may be DEFERRED after preserving Stage B.

If editing is implemented, use native navigation or narrow semantic focus with
stable buff/source/provider/casting/target IDs across rebuilds. Keep focus distinct
from authored selection/eligibility/hover; reveal in correct scroll container and
handle disabled/missing rows. Navigation never saves. Preserve continuous/group
assignment, one-shot edits, enhancements/order/Remove/Undo, policy/mode/durability
and conservative repair. Do not revive obsolete Duplicate/Disable/Classic controls.
Read-only descriptions/Resources/results scroll by controller. Back closes one
top layer and restores valid focus. A full-editor claim needs normal controller
search/input/clear/cancel without physical keyboard through a verified facility
or justified owned dialog; no virtual keyboard for execution. Inventory usable,
mouse-only and untested operations. Stop optional work when disproportionate.

## 8. Implementation and validation

Separate native input/ownership/host/navigation/presentation adapters from session
and domain logic. Refactor real dependencies only. No general UI rewrite, new
input library/SDK, language/runtime upgrade, bundled game assets, cloned foreign
listeners or unrelated mods. Reuse native fonts/art where practical; keep private
assemblies, decompilation, saves and binaries out of Git. No per-frame global
reflection, discovery, layout rebuild or focus graph reconstruction.

Follow docs/VALIDATION-POLICY.md: ordinary checks use registered explicit prefixes
through scripts/Test-Development.ps1 and reject zero matches. Documentation needs
text/link checks. Planning target 15 minutes, ordinary validation ceiling 30
minutes, excluding necessary safe restoration. Inspect nested wrappers; do not
restart historical broad source/live-purity campaigns. If required guarded live
work exceeds the budget, retain the candidate and give the precise selection and
needed authorization; never bypass guards. At a chosen candidate boundary use
existing build/package validation and its mandatory reproducibility rules.

Use real sessions/services, isolated owned temporary profiles and actual
compiler/resource decisions; stub only unavoidable external boundaries. Regress
cold binding without editor; correct current campaign/routine/settings and failed
saves; held/repeated/reconnect inputs; shared policy/resources/combat/enhancements;
readable safe errors; browsing without writes/spend; Stop/ownership cleanup;
mouse regression; bounded processing/logging. Add editor-only tests only for
implemented editing or a changed shared dependency. Classify a suspected baseline
failure once under matched conditions and retain its status.

Keep offline behavior, assembly inspection, rendering, mapped controller input,
gameplay/resource outcomes, human usability and publication separate. Direct
session calls prove downstream behavior, not mappings. Injection after mapping
bypasses mapping. Label virtual input honestly; it does not establish physical
usability. Screenshots show visible state, not its driver. Correlate input, visual
and gameplay evidence. Product commit, DLL/ZIP hashes/MVID, harness/fixture,
environment and documentation HEAD are distinct identities.

## 9. Acceptance

Use authorized disposable campaign and guarded orchestration. Extend protocol
requests/observers only as needed with positive/negative tests; never weaken
identity/save/restoration/parameters. Record PASS/FAIL/BLOCKED/NOT_RUN/PARTIAL in
docs/CONTROLLER-UI-ACCEPTANCE.md, with identity/environment/evidence/next action.
Use only harness-supported machine statuses. Optional human-document scope may
be DEFERRED. Preserve older C-series IDs/evidence and add a mapping.

| ID | Required primary observation |
| --- | --- |
| E01 | Fresh native-controller process, existing authorized campaign plan, routine access without opening editor. |
| E02 | Controller opens/navigates all three without running, reads focus/hints, deliberately confirms and returns with Back; empty/missing states escapable. |
| E03 | Long/Important/Short each use their own saved routine/common path, with actual expected effects and finite-resource observations. |
| E04 | Strict/subset and Instant/Animated preserved; combat/blocked/unresolved/unsaved refusals spend nothing and omissions are explained. |
| E05 | Reachable Stop during a suitable run preserves current-cast semantics, prevents later casts and does not pause/stall host; rapid completion is normal. |
| E06 | Results/skips/omissions/refusal reasons readable and dismissible without hover, editor trap or silent repair. |
| E07 | Held/repeated/transition inputs do not duplicate Run/Stop or activate world/camera/radial; Back closes one owned layer with correct restoration. |
| E08 | Mouse-authored plan survives supported mode switch/restart unchanged; later durable edits respected; navigation does not write. |
| E09 | Reopen/area/save/native takeover/focus/disconnect/reconnect/disable/re-enable leave no stranded input, duplicate action or stale campaign. |
| E10 | Exact-source/enhancement/resource logic shared; representative available/unavailable cases; unrun optional-provider/device coverage explicit. |
| E11 | Fresh mouse startup without controller and HUD/hotkey/spellbook/editor still usable, hover fix retained; expand affected editor checks. |
| E12 | Selector/results have visible focus, readable hints and no clipping at authorized resolution and supported compact/windowed size; no global display change. |
| E13 | Every live run passes protected-save/profile/install/settings/lock/transactional restoration; failed restoration blocks later runtime. |

One end-to-end sequence authors in the authorized mouse profile, closes editor,
uses verified switch/restart and executes entirely by controller. Prove cold
controller startup separately if needed. Observe real effects/resources, not
dispatch counters. Do not call permitted mouse authoring controller-only editing.
Batch compatible observations into a small number of justified runs; fresh-process
claims need fresh processes. No every-spell/device/resolution/mod cross-product.
Unavailable runtime/hardware is BLOCKED. Do not install drivers or seize another
machine. Provide the exact remaining physical procedure.

Optional full-editor acceptance is separate: catalogue/search/exact sources,
continuous/group/one-shot assignment, enhancements/order/Remove/Undo,
Resources/descriptions/recovery, long lists/rebuilt focus, controller text input,
controller-only author-to-execute and affected mouse regression. None gates
execution, and E01–E13 cannot establish them.

## 10. Delivery

Review duplicate planner logic, UI initialization coupling, global interception,
stale state, hidden mouse prerequisites, unowned mutations, guard weakening,
dependencies, profile duplication and unrelated changes. Fix concrete findings
with focused regressions before broader qualification. Keep stable release and
archives untouched. Choose an unambiguous local version through the established
build mechanism, preserving UMM ID/DLL/folder; pending live checks stay explicit.

Deliver local commits with base/execution/current identities; implementation and
acceptance documents; player guide and draft release notes; concise current
resume/blocker/journal; a validated local installable ZIP when possible with path,
version, product commit, ZIP/DLL SHA-256 and MVID (or exact packaging prerequisite).
For execution-only scope say: “Create/edit plans with mouse and keyboard; run
saved routines and request Stop with a controller in native controller mode.”
State setup/device/workflows only to the level observed.

Report separately: controller execution implemented and qualified / implemented
with named qualification gaps / blocked; exact additional usability or deferred;
full editor demonstrated / partial / deferred; publication/permanent installation
not authorized or performed. Final handoff includes actual work, cold/physical
evidence, source-only claims, commands/counts/durations, environment/hardware,
evidence/artifact paths, restoration and smallest owner-dependent next action.
End at review/human usability without push/merge/publication/permanent install.

Preparation references retained from the owner's supplied snapshot: S1 commit
dcf718fd7b70cf453dd25aa693fefc6aa503d826; S2 README/product/version; S3
PlannerNativeKit.cs:267–337; S4 KingmakerUiFactory.cs:278–308; S5
BuffPlannerHudButtonController.cs:195–274; S6 KingmakerPlannerInputBoundary.cs;
S7 CASTING-FIRST-PLAYER-GUIDE.md; S8 AGENTS.md and
WINDOWS-AUTONOMOUS-RUNTIME-TESTING.md; S9 VALIDATION-POLICY.md. These references
are preparation provenance, not new runtime or compatibility evidence.
