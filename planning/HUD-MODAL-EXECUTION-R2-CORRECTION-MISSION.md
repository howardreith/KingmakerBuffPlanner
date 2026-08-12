# Codex Continuation Mission: KBP HUD Re-anchor, Visible Modal Transaction, and Confirmed Execution Outcomes

Continue the active Kingmaker Buff Planner work in:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

This is a direct human-playtest repair. The latest installed UI revision is **FAILED**. Treat the human observations and screenshots as authoritative over prior automated UI claims.

Do not merely reposition the existing controls and do not return only a diagnosis. Continue through root-cause investigation, implementation, tests, guarded runtime qualification, packaging, guarded local replacement, and handoff until every acceptance criterion below passes or a true mission-defined critical hard stop occurs.

## Exact current-state intake

Before editing:

1. Read:
   - `AGENTS.md`
   - `planning/CODEX-KINGMAKER-BUFF-PLANNER-AUTONOMOUS-MISSION.md`
   - the prior full-screen/input-isolation repair mission
   - `AUTONOMOUS-RESUME.md`
   - `AUTONOMOUS-BLOCKERS.md`
   - `KINGMAKER-BUFF-PLANNER-JOURNAL.md`
   - the current definition-of-done/UI matrices
2. Record:
   - current branch;
   - clean/dirty status;
   - exact HEAD;
   - current source version;
   - currently installed Buff Planner version, DLL SHA-256, and MVID;
   - latest runtime evidence IDs;
   - whether any deployment transaction or lock is unresolved.
3. Preserve and restore external state before any stop or context compaction.
4. Do not assume the latest installed package corresponds to any remembered commit. Prove it.

Human evidence is available read-only at:

```text
C:\Dev\KingmakerBuffPlannerLab\incoming\ui-revision-r2\
```

Expected screenshots:

```text
01-controls-overlap-native-hud.png
02-tooltip-and-clickthrough-overlap.png
03-f10-invisible-modal-hud-hidden.png
```

## Human-observed failures

The current package exhibits all of the following:

1. The four Buff Planner controls overlap the native bottom-left HUD and its controls/tooltips.
2. Clicking `Long`, `Important`, or `Short` clicks through to the pause/hourglass region underneath.
3. Clicking the setup control clicks through to the native turn-based-mode control underneath.
4. Pressing F10 hides the gameplay HUD and disables world navigation/input, but no visible planner screen appears.
5. F10 therefore appears to enter a broken halfway-open modal state: gameplay input is locked, but the planner root is invisible, inactive, behind another canvas, zero-sized, transparent, or otherwise not rendered.
6. Quick-executing Long reported that one buff was “fired,” but the expected Bless effect did not appear.
7. A prior Bless assignment may have existed in the earlier profile, but it is unclear whether it migrated or loaded.
8. The four HUD controls should be moved into a dedicated horizontal row **above** the native bottom-left menu cluster, in this exact order:

```text
Setup | Long | Important | Short
```

The relocation is necessary, but it is not sufficient. Click ownership, visible modal construction, lifecycle rollback, persistence migration, and execution truthfulness must all be repaired.

## Required HUD architecture

Remove the current HUD control implementation completely if necessary. Do not cosmetically patch a structurally broken overlay.

Implement four real Unity UI buttons:

```text
Setup | Long | Important | Short
```

Requirements:

1. Place them in one horizontal row immediately above the native bottom-left menu cluster.
2. Anchor the row to the actual top edge/RectTransform of the native cluster—not to hard-coded screen coordinates.
3. Use a parent/canvas that participates in Kingmaker's real `GraphicRaycaster` and EventSystem.
4. Each visible button must own a matching hitbox.
5. Every icon graphic that should receive input must have `raycastTarget = true`.
6. No parent `CanvasGroup` may disable raycasts or interaction.
7. The row and its controls must sort/render/raycast above the native controls underneath.
8. Pointer down/up/click must be consumed by the planner buttons and must not reach native controls, world input, camera controls, or pause/turn-based controls.
9. Tooltips must not overlap native tooltip trigger regions because of incorrect hitboxes.
10. Repeated UI install/rebuild/area transition/save-load must produce exactly one row and one listener per control.
11. Remove the old right-extending/overlapping row and any invisible stale hitboxes.
12. F10 remains a fallback open/close shortcut, mentioned only in the Setup tooltip.
13. Use original or game-native icon presentation. Do not copy BubbleBuffs/Wrath art assets.

Instrument the exact pointer path and record:

```text
pointer entered visible button
button received pointer down
button listener invoked exactly once
underlying native control listener count remained unchanged
world command count remained unchanged
```

## Visible modal must be transactional

The F10 behavior proves that input locking is occurring before successful visible-window presentation.

Replace the opening lifecycle with an explicit transaction:

### Phase A — construct and validate presentation

Before acquiring any gameplay-input/modal lock:

1. Construct or activate the planner root.
2. Parent it to the correct active Kingmaker UI canvas/service-window host.
3. Stretch it to the complete active screen/canvas.
4. Make the background fully opaque.
5. Ensure it is active in hierarchy.
6. Ensure its canvas/render mode/sorting order place it above gameplay HUD and service content.
7. Ensure its `CanvasGroup` alpha is visible and it blocks raycasts.
8. Force/rebuild layout where required.
9. Validate real dimensions and screen coverage from `RectTransform.GetWorldCorners()`.
10. Validate that required child controls exist and are active.

Required diagnostics for every open attempt:

```text
root instance ID
activeSelf
activeInHierarchy
complete parent hierarchy
canvas identity
canvas render mode
sorting layer/order
overrideSorting
RectTransform anchorMin/anchorMax
pivot
sizeDelta
rect width/height
world corners
screen dimensions
CanvasGroup alpha
CanvasGroup interactable
CanvasGroup blocksRaycasts
background Graphic raycastTarget
background opacity
GraphicRaycaster identity
EventSystem identity
close/F10/Escape listener identities
```

### Phase B — acquire modal/input lease only after visible validation

Only after Phase A succeeds:

1. Enter the narrow native service-window/input-suppression mode.
2. Hide/suppress the appropriate gameplay HUD.
3. Acquire any pause/input lease.
4. Mark the planner lifecycle state as Open.
5. Emit a structured successful-open event.

### Failure rollback

If any Phase A or Phase B requirement fails:

1. Do not leave the modal/input lease active.
2. Release any partially acquired input/service-window state.
3. Restore HUD visibility and prior pause/input state.
4. Destroy or deactivate the partial planner root.
5. Return lifecycle state to Closed.
6. Show/log an exact diagnostic error.
7. Preserve normal gameplay input.

It must be impossible for an invisible planner to trap the player behind a retained input lock.

Use one idempotent open/close state machine for:

```text
Setup button
F10
Escape
close button
mod disable
UI rebuild
area transition
save/load transition
exception cleanup
```

Explicit states should be equivalent to:

```text
Closed
OpeningPresentation
AcquiringInputLease
Open
Closing
FaultedRollback
```

Repeated input during opening/closing must not double-acquire or double-release.

## Full-screen planner requirements

The visible planner must:

1. Be fully opaque and visually distinct from gameplay.
2. Cover the active screen.
3. Be above the gameplay HUD.
4. Contain a clear title and close control.
5. Render all required existing planner content.
6. Own input for background, controls, scrolling, and dragging.
7. Prevent:
   - click-to-move;
   - native HUD activation;
   - world selection changes;
   - world ability activation;
   - camera drag;
   - camera zoom from planner scrolling.
8. Restore exact prior state on every close/failure path.
9. Survive at least 20 open/close cycles without invisible state, duplicate roots, duplicate listeners, stale locks, or lost HUD restoration.

Do not claim success solely because the root object exists. It must be visibly rendered and cover the screen.

## Persisted Bless/profile investigation

Inspect the actual external planner profile used by the human-tested campaign.

Determine and report:

```text
profile path
schema version
migration path taken
whether the prior profile was found
whether a Long-group assignment exists
whether Bless is present
Bless ability GUID/key
provider key/unit/spellbook
requested targets
saved provider priority/ban/cap
whether migration dropped or remapped anything
whether stale entries were preserved or discarded
```

If the saved profile schema changed:

1. Implement an explicit migration.
2. Preserve valid assignments.
3. Record unmigratable entries.
4. Show a visible user notification when assignments could not be migrated.
5. Never silently lose or ambiguously ignore a configured buff.

Do not edit a valued save. Planner profile JSON is external mod state; handle it through the repository's atomic/migration contracts.

## Replace false “fired” accounting

The current “one fired” result is not acceptable unless the ability actually executed and its expected effects were observed.

Define distinct outcomes such as:

```text
Selected
Planned
Queued
Submitted
CastStarted
ResourceSpent
EffectConfirmed
SkippedExisting
FailedValidation
FailedSubmission
FailedExecution
TimedOutUnconfirmed
```

User-facing “applied” or equivalent success counts require `EffectConfirmed`, not merely queue submission.

For Bless specifically, instrument and prove:

```text
assignment loaded
provider resolved
targets resolved
slot/resource available
plan created
command/rule submitted
cast lifecycle started
slot/resource/material delta
expected Bless buff GUID(s)
expected target set
effect facts present after execution
duration/caster-level values
failure or timeout reason when absent
```

If the game is paused and the animated command is only queued, report `queued`, not `fired` or `applied`.

If instant execution is selected, confirm the expected facts after the rule completes before counting success.

If confirmation cannot be obtained within the bounded execution lifecycle, report an exact unconfirmed failure rather than success.

Quick-execution of an empty or invalid group must visibly explain the reason.

## Root-cause investigation before replacement

Inspect the exact current implementation and exact Kingmaker 2.1.7b UI/event lifecycle.

Identify why:

1. visible buttons do not own raycasts;
2. underlying turn-based/pause controls receive planner clicks;
3. planner root is invisible while input suppression is active;
4. execution result counts submission as success;
5. previous Bless configuration is ambiguous.

Write the findings into the journal/implementation report before or alongside the replacement.

Do not assume one cause. Verify:

```text
canvas hierarchy
sorting
GraphicRaycaster
EventSystem
Graphic.raycastTarget
CanvasGroup inheritance
RectTransform/hitbox positions
stale/invisible cloned objects
immediate-mode versus retained-mode UI
native HUD sibling order
service-window root parent
layout timing
input-lease ordering
execution-result state transitions
profile migration
```

## Required automated proof

Strengthen the prior UI/runtime gates. Existing passing tests are insufficient because human playtesting disproved them.

### HUD tests

Prove:

- one row above the native cluster;
- exact order Setup/Long/Important/Short;
- row anchored to native cluster top edge;
- visible bounds and hitboxes correspond;
- no legacy right-side/overlapping controls;
- raycast path resolves to planner control;
- native pause and turn-based listeners do not fire;
- world movement/selection counters remain unchanged;
- tooltips originate from the planner control, not the control beneath;
- repeated reinstall produces no duplicates.

### Modal tests

Prove:

- presentation validation occurs before input lease acquisition;
- invalid/invisible/zero-size presentation aborts without locking gameplay;
- successful root is active, opaque, full-screen, topmost, and raycast-blocking;
- F10 opens a visible root rather than merely hiding the HUD;
- F10/Escape/close use one lifecycle;
- every failure path releases the lease;
- 20 open/close cycles succeed;
- area/UI rebuild closes or recreates safely;
- no retained hidden root or lock remains.

### Execution tests

Prove:

- listener invoked exactly once;
- plan generated;
- persisted assignment disposition explicit;
- queued/submitted is not counted as applied;
- effect confirmation drives success;
- Bless or a controlled equivalent applies to exact intended targets;
- absent effect produces a real failure reason;
- no duplicate resource spending;
- no save write from UI-only tests.

### Guarded runtime proof

Use fresh-process guarded scenarios and structured evidence.

Do not use screenshots as sole mechanical proof. Human screenshots remain mandatory acceptance evidence.

## Human acceptance remains mandatory

Do not present the next revision as UI-fixed until it is installed locally and the user can retest:

1. four controls appear in one row above the bottom-left cluster;
2. no overlap with native controls or their tooltip regions;
3. every control click is owned by the planner;
4. Setup opens a visible opaque full-screen planner;
5. F10 opens the same visible planner;
6. F10 never leaves invisible input suppression;
7. Escape/close restore gameplay;
8. Long/Important/Short visibly respond;
9. quick execution reports confirmed outcomes;
10. Bless appears when configured and valid, or an exact failure is shown;
11. 20 open/close cycles leave no duplicates or retained locks.

Human verdict is authoritative.

## Versioning, package, and guarded replacement

Use the next repository-consistent development version; do not reuse the human-failed package version.

Run:

```text
focused tests
complete deterministic suite
repository validation
exact-reference Release build
build-output validation
package validation
native discovery/planner/execution regressions
Call of the Wild materially affected profiles
guarded UI/input/runtime scenarios
Mods restoration verification
git diff --check
```

Create a clean local-only release manifest with:

```text
commit
version
DLL SHA-256
package SHA-256
MVID
runtime evidence IDs
```

Update the project-owned guarded installer to replace the currently installed failed revision safely:

1. require game/UMM closed;
2. validate exact package/manifest;
3. inventory all other mods;
4. back up the current planner installation only;
5. stage and hash-verify the new revision;
6. install atomically where possible;
7. verify all other mods unchanged;
8. roll back on any failure;
9. clear owned locks/staging in `finally`;
10. preserve install evidence.

Install the validated replacement for human retesting. Do not launch the game outside authorized guarded workflows.

## Documentation and continuity

Update:

```text
KINGMAKER-BUFF-PLANNER-JOURNAL.md
AUTONOMOUS-RESUME.md
AUTONOMOUS-BLOCKERS.md
planning/DEFINITION-OF-DONE-MATRIX.md
docs/ARCHITECTURE.md
docs/IMPLEMENTATION-REPORT.md
docs/MANUAL-ACCEPTANCE.md
CHANGELOG.md
```

Correct any prior claim that:

- HUD input isolation passed;
- the modal was visibly full-screen;
- quick execution success was confirmed.

Preserve historical evidence, but mark the human-tested revision as failed.

Before context compaction, write exact HEAD, state, unresolved transaction status, and next command to `AUTONOMOUS-RESUME.md`.

## Stop policy

Do not stop because:

- a UI hierarchy theory fails;
- the current overlay must be replaced;
- a native prefab is unsuitable;
- a test/runtime scenario fails;
- Bless is not configured;
- a migration is needed;
- a commit is complete;
- context compaction occurs.

Stop only for a true critical safety boundary:

- unresolved or unrestorable live Mods state;
- protected-save risk;
- unsupported exact game environment;
- licensing barrier;
- required credential/dialog;
- irreducible product decision not answered here.

Begin now. Continue without waiting for routine input until the validated replacement is installed and ready for the exact human retest above.
