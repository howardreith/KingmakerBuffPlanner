# Codex Fresh-Session Mission: Recover Actual Live Buff-Row Rendering

Work in:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

Use **High reasoning**.

This is a focused fresh-session visual rendering audit. Preserve the current branch history and all proven working mechanics. Do not reset, rewrite history, or discard prior commits.

## Human verdict

The latest installed revision is a **partial pass**.

Confirmed working in a real campaign:

- the four HUD icons are visible and positioned acceptably;
- their tooltips are stable and readable;
- HUD clicks no longer activate world movement or native controls;
- F10 opens a visible opaque full-screen Buff Planner;
- close/input/HUD restoration works.

Still failed:

- the planner header reports `11 discovered buff sources`, `11 providers`, and `10 of 11 shown`;
- the left buff-list viewport is visually empty;
- the right details pane is visually empty;
- no actual buff name, including Bless, can be seen or selected;
- therefore the user still cannot configure or use the planner.

Human evidence is read-only under:

```text
C:\Dev\KingmakerBuffPlannerLab\incoming\live-row-render-r4\
```

Expected files:

```text
01-live-planner-counts-but-no-visible-rows.png
02-windowed-live-planner-counts-but-no-visible-rows.png
```

## Critical contradiction

The prior automated report claimed:

```text
10 active rows
5 viewport-visible rows
details binding succeeds
Bless visible/selectable
```

The human screenshot disproves the visual portion of that claim.

Do not reuse `activeSelf`, `activeInHierarchy`, non-zero `RectTransform`, or synthetic `visible=true` as proof that a player can see a row.

Correct all tests and documentation that equate internal object state with actual rendered output.

## Freeze current working behavior

Do not regress:

- live bootstrap;
- four-button HUD row;
- tooltip stability;
- HUD input isolation;
- F10 opening;
- opaque modal;
- input-lease ordering;
- close/Escape lifecycle;
- HUD restoration;
- native and Call of the Wild catalog discovery;
- persistence and execution contracts.

This mission is not permission for another bootstrap, input, or full-screen redesign unless exact evidence shows those working paths are inseparable from the rendering defect.

## First actions

Before editing:

1. Verify branch, HEAD, remote relationship, clean/dirty status, source version, installed version, package hash, installed DLL hash/MVID, and transaction state.
2. Read:
   - `AGENTS.md`;
   - the original autonomous mission;
   - all prior UI/bootstrap/catalog repair missions;
   - `AUTONOMOUS-RESUME.md`;
   - `AUTONOMOUS-BLOCKERS.md`;
   - `KINGMAKER-BUFF-PLANNER-JOURNAL.md`;
   - current UI source and tests;
   - current runtime evidence for the claimed 10 active/5 visible rows.
3. Inspect the human screenshots.
4. Write an evidence table distinguishing:
   - catalog/view-model existence;
   - Unity object existence;
   - layout geometry;
   - CanvasRenderer state;
   - clipping/culling;
   - actual rendered pixels;
   - human visibility.
5. Identify the earliest stage where the claimed visible row ceases to be demonstrably rendered.

Do not begin by changing discovery or adding a hardcoded Bless entry. The header counts prove the live catalog exists.

# 1. Inspect the exact live rendering chain

For every purported visible row, capture:

```text
catalog key/name
view-model identity
row GameObject instance ID
row component types
activeSelf
activeInHierarchy
parent hierarchy
sibling index
layer
canvas identity/render mode/sorting
CanvasGroup alpha/interactable/blocksRaycasts
CanvasRenderer cull
CanvasRenderer absoluteDepth
CanvasRenderer material count
CanvasRenderer inherited alpha
Image/Text enabled state
text string
font identity
font size
text color RGBA
material/shader identity
RectTransform anchors/pivot
anchoredPosition
sizeDelta
rect width/height
world corners
screen-space corners
viewport rect
content rect
mask/RectMask2D state
clip rect
whether row corners intersect viewport corners
layout preferred/min/flexible height
content anchoredPosition
content calculated height
scroll normalized position
```

Inspect the exact left-pane hierarchy:

```text
ScrollRect
Viewport
Mask or RectMask2D
Content
LayoutGroup
ContentSizeFitter
row roots
row backgrounds
row text and icons
```

Look specifically for:

- rows parented to the wrong object;
- content anchored outside the viewport;
- zero or negative row height;
- layout not rebuilt after row creation;
- a parent CanvasGroup alpha of zero;
- black or transparent row text/background;
- a missing/null font;
- a shader/material incompatible with the active canvas;
- rows behind the opaque panel background;
- sibling order placing rows beneath the panel;
- Mask/RectMask2D clipping everything;
- content/viewport on different canvases;
- CanvasRenderer culling;
- use of a world-space canvas or wrong camera;
- stale rows created under a previous/inactive modal root;
- exceptions during row binding that leave empty shells;
- a list rebuild that destroys rows after counting them.

Log complete exceptions and stack traces.

# 2. Add a controlled visual canary before repairing production rows

Create a temporary, diagnostic-only visual canary under the **same live Content transform** used by production rows:

- opaque high-contrast background;
- plain `UnityEngine.UI.Text`;
- a known available built-in/game font;
- text such as `KBP RENDER CANARY — <first catalog entry name>`;
- explicit non-zero `LayoutElement.preferredHeight`;
- explicit text color and alpha;
- no custom material;
- no complex cloned prefab.

Open the planner in the disposable live campaign and capture a screenshot.

Interpretation:

1. If the canary is visible, the panel/canvas/viewport path works and the production row prefab/binding/material is broken.
2. If the canary is not visible, the Content/viewport/mask/canvas/layout path is broken.
3. If it appears outside the expected pane, the anchors/parent/layout are broken.
4. If it appears behind the background, the sibling/canvas sorting is broken.

Do not ship the diagnostic canary in the final package. Preserve its evidence and remove it after the root cause is proven.

# 3. Prefer a simple, reliable production row renderer

If the current cloned/custom row prefab cannot be proven visually correct quickly, replace only the row renderer with a straightforward retained-mode Unity UI row built programmatically from:

```text
Button
Image background
LayoutElement
HorizontalLayoutGroup
ability icon Image when available
Text name
Text source/provider/availability summary
selected-state indicator
```

Requirements:

- use a proven Kingmaker/game font;
- use readable non-transparent colors;
- explicit row height;
- explicit padding/spacing;
- proper anchors and pivot;
- parent directly to the active ScrollRect Content;
- row background and text sort above the panel background;
- no custom shader required;
- no Wrath asset dependency;
- no third-party UI library;
- one listener per row;
- stable pooling/rebuild cleanup;
- selection updates the details pane.

After rows are created:

```text
Canvas.ForceUpdateCanvases()
LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect)
LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect)
Canvas.ForceUpdateCanvases()
```

Record the final geometry after the rebuild, not before it.

The first visible row should be auto-selected when no persisted selection exists, so the details pane cannot remain blank without explanation.

# 4. Require actual rendered evidence

Strengthen the real campaign scenario so it captures an actual screenshot after:

1. opening the planner;
2. waiting for catalog refresh and two rendered frames;
3. selecting the first row;
4. waiting for details binding.

Store the screenshot in runtime evidence.

The scenario must also emit:

```text
first five expected visible row names
first five row screen rectangles
first selected row name
details title text
screenshot path/hash
```

Automated visual evidence must establish more than object existence.

At minimum:

- the screenshot file exists and has the expected dimensions;
- the list viewport contains non-background pixel variation in at least one expected row rectangle;
- the text/image CanvasRenderers are not culled and have non-zero alpha;
- expected row screen rectangles intersect the viewport;
- the selected row and details title are populated.

Pixel checks are supporting evidence, not a replacement for human acceptance. The next build remains human-gated.

Do not claim `visible` from a Boolean written by the same code under test.

# 5. Reconcile catalog counts and visible list behavior

The UI currently says:

```text
10 of 11 shown
```

but visibly shows zero.

After repair, the displayed count must be based on rows successfully bound into the active screen, and diagnostics should distinguish:

```text
10 matching filters
10 row view models
10 rows instantiated
10 rows laid out
5 intersect viewport
5 rendered/non-culled
```

If rendering fails, show an explicit in-screen error:

```text
10 buffs matched, but the list failed to render. See KBP diagnostics.
```

Never present a positive shown count with a visually blank unexplained list.

Ensure Reset Filters and Refresh operate on the active live screen and do not bind to a stale catalog/root.

# 6. Bless vertical slice and execution correction

Once rows are visibly rendered, inspect Bless generically.

Bless in Pathfinder/Kingmaker is a divine spell and should not be rejected merely because of a nonexistent consumable material component. Inspect the exact native `BlueprintAbility.MaterialComponent`, spellbook data, and actual game spending contract.

Do not assume the previous `material-component-unavailable` result was correct.

Record:

```text
Bless blueprint GUID
native components/material-component fields
whether any inventory item is actually required
prepared/available slot
provider
target set
row visibility
assignment state
execution path
resource/slot delta
expected Bless buff GUID
effect confirmation
```

If the prior executor treated a divine focus or absent component as a required consumable, repair the generic component-accounting logic and add regression tests. Do not special-case Bless unless an exact native exception requires it.

# 7. Required tests

## Deterministic/UI tests

Cover:

- production row receives a real font;
- row text is nonempty and nontransparent;
- row has explicit positive height;
- row is parented to active Content;
- sibling order places it above pane background;
- content layout is rebuilt after row changes;
- row/viewport intersection is computed correctly;
- Mask/RectMask2D does not cull valid rows;
- first row auto-selection populates details;
- positive shown count cannot coexist with zero successfully rendered rows without an explicit error state;
- repeated Refresh/reopen does not create stale or duplicate rows;
- row selection listeners are singular;
- diagnostic canary is absent from production builds.

## Real disposable-campaign tests

Using only the authorized `KBP_AUTOMATION_WORKING` save:

1. open planner;
2. catalog reports expected counts;
3. at least one actual row is rendered;
4. at least one row name is visible in screenshot evidence;
5. Bless is visibly rendered when prepared/available, otherwise an exact reason is visible;
6. select first row;
7. details pane visibly contains the selected name;
8. Refresh preserves visible rows;
9. Reset Filters restores visible rows;
10. twenty reopen/refresh cycles leave no stale rows or duplicate listeners;
11. HUD icons/input/modal remain working;
12. UI-only scenario writes no save;
13. exact Mods restoration passes.

Run two fresh-process passes after the fix.

# 8. Version, package, and guarded installation

Use the next repository-consistent version. Do not reuse the human-failed installed version.

Before installation:

```text
focused rendering tests
complete deterministic suite
repository validation
exact-reference Release build
package validation
native regression
Call of the Wild regression
two physical live-campaign visual-row scenarios
exact Mods restoration
clean HEAD
git diff --check
```

Use the guarded installer to replace only the existing Buff Planner installation and preserve every non-planner mod byte-for-byte.

Present the next package with:

```text
source commit
version
package SHA-256
DLL SHA-256
MVID
live run IDs
screenshot evidence paths/hashes
expected row names and rectangles
selected details title
Bless component-accounting result
```

# 9. Documentation truthfulness

Update:

```text
KINGMAKER-BUFF-PLANNER-JOURNAL.md
AUTONOMOUS-RESUME.md
AUTONOMOUS-BLOCKERS.md
planning/DEFINITION-OF-DONE-MATRIX.md
docs/ARCHITECTURE.md
docs/IMPLEMENTATION-REPORT.md
docs/MANUAL-ACCEPTANCE.md
docs/QUALIFICATION.md
CHANGELOG.md
```

Mark the current human-tested revision:

```text
PARTIAL PASS:
- HUD icons, tooltips, pointer isolation, F10, modal, close lifecycle.

FAILED:
- actual live row rendering and details visibility.
- prior automated active/visible row claims did not match rendered human output.
```

Preserve history while removing unsupported claims.

# 10. Fresh-session continuation policy

This is intentionally a fresh independent session because the prior agent repeatedly treated internal object state as visual proof.

Continue autonomously through diagnosis, visual canary, row-render repair, tests, live screenshot qualification, package, guarded install, and handoff.

Do not stop because:

- the prior row renderer must be replaced;
- one mask/layout theory fails;
- a screenshot gate fails;
- Bless component accounting is wrong;
- a test/runtime run fails;
- a commit completes;
- context compaction occurs.

Before compaction, update `AUTONOMOUS-RESUME.md` with exact HEAD, current evidence, external state, and the next command.

Stop only for a true critical safety boundary:

- protected-save risk;
- absent authorized disposable save at the live gate;
- unresolved/unrestorable Mods transaction;
- unsupported exact environment;
- required credential/dialog;
- licensing barrier;
- irreducible product decision not answered here.

Begin with the current runtime evidence and the diagnostic canary. Do not begin by changing discovery counts or adding another synthetic visibility Boolean.
