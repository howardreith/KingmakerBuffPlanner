# Codex Continuation Mission: Populate the Live Catalog, Own HUD Input, and Stabilize Tooltips

Continue work in:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

Use High reasoning.

This is a direct human-playtest correction. The latest installed revision is a **partial pass**, not a complete failure and not a completed UI.

## Preserve the behaviors that now work

Human testing confirms:

1. The four planner icons are now visible and positioned in a substantially better row above the bottom-left native HUD cluster.
2. F10 successfully opens a visible, opaque, full-screen Buff Planner.
3. The live bootstrap problem from 0.0.3 is therefore repaired.

Do not regress those gains. Do not perform another wholesale bootstrap or modal rewrite unless exact evidence proves it necessary.

## Human-observed remaining failures

The latest live campaign shows:

1. Hovering the planner HUD icons produces a flickering tooltip.
2. The tooltip extends too far to the right and is poorly clamped to the screen.
3. Clicking the planner HUD icons still issues click-to-move/world actions behind the UI.
4. The full-screen planner opens, but the buff list and details pane are completely empty.
5. The screen itself reports:

   ```text
   1 party/pet targets
   11 discovered buff sources
   11 providers
   ```

   Therefore discovery appears to find data, but no rows are rendered or all entries are being filtered away.
6. The tested party includes a Cleric. Bless and other valid buffs should be visible when actually prepared/available; if Bless is not currently available, the UI must show the exact reason rather than an unexplained empty list.
7. The Long/Important/Short HUD actions do not provide useful visible behavior or feedback.
8. The human cannot currently configure or execute buffs because the live list is empty.

Human evidence is read-only under:

```text
C:\Dev\KingmakerBuffPlannerLab\incoming\ui-catalog-input-r3\
```

Expected files:

```text
01-hud-icons-visible-position-improved.png
02-hud-tooltip-flicker-clickthrough.png
03-fullscreen-planner-empty-catalog.png
```

Treat the screenshots and this human verdict as authoritative over synthetic UI tests.

## Required first step: exact current-state intake

Before editing:

1. Verify branch, HEAD, source version, installed version, installed DLL SHA-256/MVID, package identity, and repository cleanliness.
2. Verify no unresolved deployment/runtime transaction.
3. Read:
   - `AGENTS.md`;
   - the original autonomous mission;
   - all prior UI/bootstrap repair missions;
   - `AUTONOMOUS-RESUME.md`;
   - `AUTONOMOUS-BLOCKERS.md`;
   - `KINGMAKER-BUFF-PLANNER-JOURNAL.md`;
   - current UI/catalog architecture;
   - current runtime UI and catalog tests.
4. Read the latest clean live logs.
5. Record the currently passing behaviors and explicitly freeze them against regression.

Do not begin by changing colors or moving the row. The current root problems are catalog-to-view binding, pointer ownership, quick-action result flow, and tooltip lifecycle.

# Part I — Diagnose the empty live catalog

The screen reports 11 discovered sources and 11 providers but renders zero rows. Instrument the complete pipeline in the actual loaded campaign.

For the active party/profile, record:

```text
party/pet unit count
unit IDs/names/classes
spellbook count and identities
raw spell/ability source count
beneficial candidates
normalized BuffCatalogEntry count
included/excluded count
provider count
entries assigned to Long/Important/Short
entries after each UI filter
row view-model count
instantiated row GameObject count
active row count
visible row count
content RectTransform size
viewport RectTransform size
layout rebuild result
selected row identity
```

For each filter stage, record before/after counts separately:

```text
group
search
configured/requested-only
duration
source
hidden
sort
availability
target/provider validity
```

Do not collapse these into one final count.

## Determine whether this is data or rendering

Prove one of these exact states:

1. `visible view-model count == 0` because filters or classification removed all entries; or
2. view models exist but row GameObjects are not created; or
3. rows exist but are inactive, zero-sized, transparent, clipped, behind another graphic, or outside the viewport; or
4. row binding throws and the exception is swallowed; or
5. the screen is bound to a stale/empty catalog instance rather than the live catalog shown in the header.

Inspect:

```text
ScrollRect
viewport
content
mask
VerticalLayoutGroup/GridLayoutGroup
ContentSizeFitter
anchors/pivots
row preferred/min height
activeSelf/activeInHierarchy
CanvasGroup alpha
Graphic colors/alpha
sibling order
event/listener exceptions
catalog refresh event subscription
screen-open refresh ordering
```

Log full exceptions and stack traces. Do not silently convert binding failures to an empty panel.

## Required user behavior for empty states

The default first-open state must show **all available non-hidden beneficial entries**, not only configured/requested entries.

If a persisted UI filter would hide everything:

- show `0 of N buffs shown because of filters`;
- show the active filters;
- provide a visible Reset Filters action;
- do not present a blank unexplained panel.

If the catalog genuinely contains zero available entries:

- show an explicit empty-state explanation;
- show raw/included/provider counts;
- provide Refresh;
- explain whether spells are unprepared, exhausted, unsupported, or unavailable.

## Bless vertical slice

Inspect the active Cleric and Bless specifically.

Record:

```text
Bless blueprint GUID/key
spellbook identity
spell level
prepared/known/custom/special source
slot availability
provider key
beneficial-effect classification
duration group
target legality
hidden/filter state
view-model identity
row object identity and bounds
saved assignment/group state
```

If Bless is prepared/available, it must appear in the visible list and be selectable.

If Bless is not prepared/available, the UI must not silently omit all context. Diagnostics must say why, and another actually available buff among the reported sources must still be visible.

Do not hardcode Bless into production discovery merely to satisfy the test. Repair the generic live catalog/view pipeline.

# Part II — Make HUD buttons own input

The visible HUD icons still permit click-to-move/world actions.

Do not assume `Graphic.raycastTarget = true` alone is sufficient. Kingmaker may read raw mouse input or use a separate world-input path even when the Unity EventSystem hits UI.

Inspect how native Kingmaker HUD buttons suppress world clicks. Reuse the narrowest proven native mechanism.

Required defense in depth:

1. Real retained-mode Unity buttons on the correct active canvas and `GraphicRaycaster`.
2. Correct visible hitboxes and `raycastTarget = true`.
3. No parent `CanvasGroup.blocksRaycasts = false`.
4. Pointer down/up/click handlers consume the planner event.
5. A narrowly scoped world-input suppression boundary active only while the pointer is within the planner HUD row or full-screen planner.
6. No broad permanent global input patch.
7. Exact cleanup on UI rebuild, area transition, disable/unload, and exception.

Prove for each button:

```text
pointer raycast top result is the planner button
planner listener fires exactly once
underlying native listener count does not change
world movement command count does not change
selection does not change
camera state does not change
```

Test Setup, Long, Important, and Short independently.

If Kingmaker's world click-to-move code ignores EventSystem consumption, patch/intercept only the exact command boundary and only when the planner pointer-capture service confirms the pointer is over an owned planner region.

Do not suppress normal world input when the pointer is elsewhere.

# Part III — Repair HUD quick actions and feedback

The three quick-action buttons must never silently do nothing.

For Long, Important, and Short, prove:

```text
pointer event received
listener invoked once
group resolved
current profile loaded
requested assignments found
plan refreshed
validation result
execution invoked or refused
confirmed result
user-visible feedback
```

Required user-visible outcomes include:

```text
No Long buffs are configured.
Long: 3 applied, 1 skipped, 1 failed.
Bless: unavailable because no prepared slot remains.
Bless: queued while paused.
Bless: effect confirmed on 4 targets.
```

Do not report `applied`, `fired`, or success from selection, planning, queueing, submission, or cast start alone. Preserve the confirmed-effect outcome model from the prior repair.

If no buffs are configured because the old profile did not migrate, show that explicitly and preserve/migrate valid prior assignments.

# Part IV — Stabilize tooltip behavior

The planner HUD tooltip currently flickers and extends off to the right.

Determine whether flicker is caused by:

```text
tooltip graphics intercepting pointer raycasts
tooltip repeatedly entering/leaving the source hitbox
tooltip object rebuilding every Update
layout/position oscillation
native and custom tooltip systems both active
stale duplicate tooltip listeners
tooltip anchored beneath the pointer
```

Required tooltip architecture:

1. Prefer Kingmaker's native tooltip system when a stable API is proven.
2. Otherwise use one cached tooltip instance, not create/destroy every frame.
3. Tooltip root and all tooltip graphics must not intercept pointer input:
   - `raycastTarget = false`;
   - `CanvasGroup.blocksRaycasts = false`;
   - `CanvasGroup.interactable = false`.
4. Show on pointer enter and hide on pointer exit using stable source ownership.
5. Clamp the tooltip completely inside the active canvas/safe screen bounds.
6. Use a reasonable maximum width with wrapping.
7. Place it above or inward from the four-button row; flip left/right when required.
8. Do not overlap the pointer/source hitbox in a way that creates enter/exit oscillation.
9. Exactly one tooltip listener per button.
10. Hide/destroy safely on row rebuild, area change, modal open, disable, and unload.

Automated proof:

- hold hover for at least five seconds;
- tooltip show count remains one;
- no alternating enter/exit events;
- bounds stay inside screen;
- no duplicate tooltip objects;
- no underlying native tooltip fires;
- source button remains the top raycast result.

Human acceptance remains authoritative for visual smoothness and placement.

# Part V — Preserve the working modal and improve live usability

The opaque full-screen planner now opens through F10. Preserve:

```text
visible root
opaque background
correct input lease ordering
F10/Escape/close lifecycle
HUD restoration
```

Do not regress these while repairing the catalog.

Within the modal:

1. Long/Important/Short tabs must visibly show selected state.
2. Row selection must populate the details pane.
3. Search and filter controls must visibly indicate active state.
4. Refresh must rebuild from the same live catalog represented in the header.
5. The first visible row may be selected automatically when appropriate.
6. Scroll input must remain within the planner and not zoom the world.
7. Clicking modal controls/background must not issue world actions.
8. Provide a clear loading state during catalog refresh rather than a blank panel.

# Required live diagnostics

Add one bounded UMM/debug action such as:

```text
Print live Buff Planner catalog/UI diagnostics
```

It should report without changing gameplay:

```text
bootstrap/controller identity
current campaign/profile identity
catalog counts
filter counts
visible row count
row hierarchy/bounds
selected row
HUD button raycast results
tooltip state
quick-action last result
modal state
last full exception
```

Keep diagnostics release-safe and bounded.

# Automated qualification

Strengthen the current tests. Existing source and synthetic UI passes are insufficient.

## Deterministic/UI fixture tests

Cover:

- default filters show all non-hidden entries;
- persisted all-hiding filters produce an explicit filtered-empty state;
- Reset Filters restores rows;
- catalog refresh event reaches the active screen;
- rows instantiate and receive non-zero layout bounds;
- selected row populates details;
- tooltip non-raycast and screen clamping;
- pointer-capture state scoped to owned regions;
- quick-action explicit feedback;
- no success before effect confirmation;
- repeated rebuild creates no duplicate rows/buttons/tooltips/listeners.

## Real campaign scenario

Using the explicitly authorized disposable `KBP_AUTOMATION_WORKING` save, prove:

1. screen header raw counts;
2. at least one visible buff row;
3. Bless row visible when prepared/available, otherwise exact unavailable reason;
4. row click selects and populates details;
5. Refresh preserves/repopulates the list;
6. Setup HUD click opens modal and does not move;
7. each quick-action click does not move;
8. empty group provides explicit feedback;
9. configured controlled buff produces confirmed outcome or exact failure;
10. tooltip remains stable and inside screen bounds;
11. no world input from HUD or modal clicks;
12. 20 reopen/rebuild cycles leave no duplicates or stale locks;
13. UI-only scenario writes no save;
14. `Mods` restoration remains exact.

Do not claim a campaign UI pass from detached objects or synthetic party data.

If the disposable save pair is still absent, complete all safe source/fixture/log work and stop only at the exact save-creation gate. Do not fabricate live qualification.

# Versioning and guarded install

Use the next repository-consistent version; do not reuse the human-tested version.

Before installation:

```text
focused catalog/UI/input/tooltip tests
complete deterministic suite
repository validation
exact-reference Release build
package validation
native regressions
Call of the Wild regressions materially affected
real disposable-campaign UI scenario
repeated fresh-process pass
exact Mods restoration
git diff --check
```

Use the project-owned guarded installer to replace only the currently installed Buff Planner revision while proving every other mod remains byte-identical and rolling back on any failure.

Present the next build with:

```text
commit
version
package SHA-256
DLL SHA-256
MVID
live campaign run IDs
visible row count/evidence
HUD input-isolation evidence
tooltip stability evidence
quick-action confirmed-result evidence
```

# Documentation truthfulness

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

Record the latest human verdict as:

```text
PARTIAL PASS:
- HUD row visible and positioned acceptably.
- F10 opens visible opaque planner.

FAILED:
- live buff list empty despite discovered sources/providers;
- HUD clicks still reach world click-to-move;
- quick actions provide no useful confirmed behavior;
- tooltip flickers and exceeds screen bounds.
```

Do not rewrite history or overclaim UI completion.

# Continuation policy

Continue autonomously through diagnosis, implementation, tests, runtime qualification, package, guarded replacement, and handoff.

Do not stop because:

- one list-binding theory is wrong;
- Bless is unavailable;
- a filter migration is needed;
- native UI input ignores EventSystem consumption;
- one tooltip implementation fails;
- a test/runtime run fails;
- a commit is complete;
- context compaction occurs.

Before compaction, update `AUTONOMOUS-RESUME.md` with exact HEAD, external state, and next command.

Stop only for a true critical boundary:

- protected-save risk;
- absent required disposable save pair at the live campaign gate;
- unresolved/unrestorable Mods transaction;
- unsupported exact game/UMM environment;
- required credential/dialog;
- licensing barrier;
- irreducible product decision not answered here.

Begin by freezing the now-working bootstrap/modal behavior, then trace the 11 discovered sources through every catalog/filter/view/layout stage in the actual campaign.
