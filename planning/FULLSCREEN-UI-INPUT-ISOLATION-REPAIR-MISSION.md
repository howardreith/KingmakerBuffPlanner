# Codex Mission: Kingmaker Buff Planner Full-Screen UI and Input-Isolation Repair

Continue work in:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

This is a targeted player-facing repair based on direct human playtesting of the installed `0.0.1` build. It is not permission to redesign or weaken the already implemented catalog, planner, resource, persistence, execution, compatibility, or runtime-safety contracts.

Verify the actual clean branch and HEAD before editing. The human-tested baseline was reported as:

```text
branch: codex/kingmaker-buff-planner
commit: ec153837401c8815b1909cb15e85ab658a1ee26a
version: 0.0.1
```

Trust a clean descendant if the repository has advanced, but record the exact starting commit.

## Human playtest is authoritative

The prior automated statement that the UI gate was complete is disproven by player-facing evidence. Reclassify that gate and strengthen it rather than merely rerunning it.

Observed defects:

1. The HUD exposes small text controls such as `Buff Planner (F10)`, `Long`, `Important`, and `Short` as a floating/debug-style overlay instead of native-looking icon buttons.
2. Clicking planner controls also clicks the game world behind them and moves the selected character.
3. The `Long` control appears not to perform its intended action. Do not assume click-through is the only cause: instrument and prove whether the UI callback, group selection/execution, plan generation, and result path actually fire.
4. The planner is not visually distinct from gameplay. It needs an opaque, full-screen service-window experience comparable in usability to Kingmaker's Inventory, Character, or Spellbook screens.
5. F10 may remain as a fallback/configurable shortcut, but a visible text label saying `Buff Planner (F10)` is not an acceptable primary interface.

When present, inspect the private screenshots read-only under:

```text
C:\Dev\KingmakerBuffPlannerLab\incoming\ui-revision\
```

Expected names:

```text
01-current-kbp-overlay-clickthrough.png
02-bubblebuffs-hud-button-reference.png
03-bubblebuffs-fullscreen-reference.png
```

The BubbleBuffs screenshots are UX references only. Do not copy Wrath-specific code blindly, and do not copy BubbleBuffs art, icons, textures, or other assets. Use original or game-native presentation.

## Required product decision

Implement this interaction model:

### Native HUD controls

1. Add one native-styled **Buff Planner setup icon** to Kingmaker's existing bottom-left menu/button cluster.
2. Clicking the setup icon opens the full-screen Buff Planner.
3. Keep F10 as a fallback/configurable open/close shortcut and mention it only in the icon tooltip.
4. Add three native-styled quick-execution icon buttons adjacent to the setup button:
   - Long
   - Important
   - Short
5. The quick-execution buttons execute their group without opening the setup screen.
6. Tooltips must state the exact group/action and any unavailable reason.
7. Remove the current floating text-strip implementation from the production HUD.
8. Use a stable native layout/prefab/hierarchy anchor rather than screen-pixel coordinates.
9. Install exactly one set of buttons after load, save/load, area transition, UI rebuild, enable/disable, and repeated open/close.
10. Button hitboxes must match visible bounds and must not permit world input through them.

A different exact arrangement is acceptable only if it preserves the same clear setup-plus-three-quick-actions model and is demonstrably more native to Kingmaker.

### Full-screen planner

Create a dedicated, fully opaque, full-screen Buff Planner service window. It should feel like a distinct game screen, not a translucent HUD overlay.

Required behavior:

1. Stretch to the complete active UI canvas at all supported resolutions/aspect ratios.
2. Use a fully opaque Kingmaker-appropriate background/frame—preferably safe runtime reuse of game-native presentation.
3. Display a clear `BUFF PLANNER` title and native close control.
4. Close through:
   - the close button;
   - Escape;
   - F10 when already open.
5. Hide or fully cover gameplay HUD elements that should not remain interactive.
6. Preserve the existing useful planner content, reorganized into a readable full-screen layout:
   - search and filters;
   - Long/Important/Short group selector;
   - buff list;
   - selected-buff details;
   - provider priority/ban/cap controls;
   - target portraits for party members and pets;
   - plan summary;
   - apply/execute and result feedback;
   - settings.
7. In the full-screen window, Long/Important/Short should be clearly styled group tabs/selectors. On the HUD, the corresponding icons are quick-execution actions. Do not leave ambiguous identical-looking controls with different behavior.
8. Use native fonts, hover/pressed/selected states, tooltips, scrolling, and disabled-state presentation where practical.
9. The world must not remain visually readable through the planner background.
10. Do not require literal integration into Kingmaker's existing Spellbook implementation. A standalone full-screen service window using Kingmaker's service-window lifecycle is preferred over a brittle spellbook patch. If exact assembly inspection proves a safe native tab/host integration, document the evidence before choosing it.

## Input isolation: mandatory

The current click-through is a release-blocking defect.

Inspect the exact Kingmaker 2.1.7b implementation of Inventory, Character, Spellbook, Journal, and other service windows. Determine how the game:

- enters/leaves service-window mode;
- suppresses world click-to-move and unit selection;
- routes Escape/back;
- hides/restores HUD;
- blocks or redirects gameplay shortcuts;
- handles pause state;
- handles mouse wheel and dragging;
- restores state after close or exception.

Use the narrowest native lifecycle/API available.

Defense in depth is required:

1. Enter an appropriate native full-screen/service-window UI mode that suppresses gameplay input.
2. Add a full-screen UI raycast blocker under planner controls:
   - visible/opaque background graphic;
   - `raycastTarget = true`;
   - appropriate `CanvasGroup.interactable`;
   - appropriate `CanvasGroup.blocksRaycasts`;
   - correct `GraphicRaycaster`/EventSystem context.
3. Ensure every interactive control is parented to the correct raycast canvas and is above the blocker.
4. Consume pointer down/up/click, drag, scroll, and relevant cancel events within the planner.
5. While open, prevent:
   - click-to-move commands;
   - party/unit selection changes caused by planner clicks;
   - world interaction;
   - ability activation caused by planner clicks;
   - camera zoom from planner scrolling;
   - camera drag from planner dragging.
6. Restore the exact prior gameplay/UI/pause/input state on every close path, disable/unload, scene transition, exception, and hot reload.
7. Implement the acquired native UI/input state as an explicit scoped lease/disposable lifecycle where possible, with idempotent release.
8. Do not solve this with a broad permanent global input patch. Any fallback interception must be active only while this exact planner instance is open and must be fully tested for release/cleanup.

## Group actions and feedback

Instrument the exact click and execution flow.

For each setup/group control, prove:

```text
pointer event received
button listener invoked exactly once
intended group resolved
plan recalculated/revalidated
execution invoked or deliberately refused
result presented to the user
no world command issued
```

Quick-execution buttons must never silently do nothing.

When a group has no requested buffs, show a clear non-error message such as:

```text
No Long buffs are configured.
```

When the group cannot execute, provide the real reason:

- no valid provider;
- insufficient slots/resources/materials;
- prohibited in combat;
- stale party/profile;
- invalid target;
- existing effects skipped;
- other exact planner diagnostic.

Preserve the current combat and overwrite policies unless a proven bug requires repair.

## Architecture and cleanup

Prefer explicit components such as:

```text
BuffPlannerHudButtonController
BuffPlannerScreenController
BuffPlannerInputLease
BuffPlannerScreenView
BuffPlannerQuickExecuteController
BuffPlannerUiLifecycleDiagnostics
```

Names may follow repository conventions.

Requirements:

1. Remove or permanently retire the old floating/debug overlay from production.
2. Keep UI callbacks thin. Planner, persistence, and execution logic remain in services.
3. Dispose all listeners/subscriptions and destroy owned GameObjects on uninstall/rebuild.
4. Do not duplicate source-of-truth state in UI widgets.
5. Do not regress dynamic native or Call of the Wild discovery.
6. Do not change stable buff/provider/save identities merely for UI convenience.
7. Do not add a new third-party UI library.
8. Do not add or ship Wrath assets.
9. Do not copy BubbleBuffs icon art.
10. Record the exact native Kingmaker prefabs/hierarchies/APIs reused and why.

## Required tests

The existing UI gate is insufficient. Replace or expand it.

### Deterministic/fixture tests

Prove, where feasible:

- exactly one setup button and three quick-action buttons;
- no legacy text strip;
- stable hierarchy/anchors;
- full-screen opaque blocker exists;
- blocker receives raycasts;
- controls sort above blocker;
- open/close state machine is idempotent;
- repeated UI installation does not duplicate buttons/listeners;
- area transition and UI rebuild cleanup;
- Escape/F10/close route through one close lifecycle;
- input lease releases after every failure path;
- group click listener fires exactly once;
- empty group produces visible feedback;
- modal open state suppresses gameplay command dispatch through the chosen adapter boundary.

Do not mock the entire UI or game input module merely to make tests pass. Use real service objects/temporary Unity objects where the repository's existing test architecture permits, and narrow adapters for exact engine boundaries.

### Guarded runtime scenarios

Add or strengthen runtime scenarios that prove:

1. HUD setup icon exists, is active, and has the expected listener/tooltip identity.
2. Three quick-execution buttons exist and resolve distinct groups.
3. Opening the planner creates one active full-screen root.
4. Planner root is opaque and covers the active canvas.
5. Planner is in the expected service-window/input-blocking state.
6. Clicking or programmatically dispatching a planner background/control pointer event does not increase movement-command count, change world selection, or activate a world ability.
7. Clicking a group selector changes the active group and visible selected state.
8. Quick-executing an empty group returns an explicit result rather than a silent no-op.
9. Closing restores the exact prior UI/input/pause state.
10. Open/close repeated at least 20 times creates no duplicate root/button/subscriber.
11. Area transition or safe UI reconstruction preserves exactly one button set.
12. No save is written by UI-only scenarios.
13. The real `Mods` directory is restored exactly by the guarded harness.

Do not use screenshots alone as mechanics proof. Use structured state and command counters. Screenshots remain useful for human visual acceptance.

### Manual acceptance checklist

Create/update a concise checklist for the user:

- bottom-left setup icon looks native and is easy to find;
- quick buttons are understandable;
- no text-strip debug overlay remains;
- world is not visible through the full-screen planner;
- every click stays in the planner;
- clicking empty background does not move anyone;
- clicking Long/Important/Short visibly responds;
- list scrolling does not zoom the world;
- dragging does not move the camera;
- Escape/F10/close work;
- reopen works;
- layout is readable at the user's actual resolution;
- target portraits and provider controls are legible;
- tooltips and disabled explanations are useful.

Human playtest verdict is authoritative for these visual/interaction criteria.

## Version, packaging, and local installation

Bump the development/test package to:

```text
0.0.2
```

or the next repository-consistent version if a clean descendant already advanced.

Run all applicable gates:

- focused UI/input tests;
- complete deterministic suite;
- repository validation;
- exact-reference Release build;
- build-output validation;
- strict package creation/validation;
- `git diff --check`;
- existing native catalog/planner/execution regression gates;
- Call of the Wild profile gates materially affected by UI lifecycle;
- guarded runtime UI scenarios;
- exact `Mods` restoration.

Create a validated local-only release package with recorded:

```text
commit
version
package SHA-256
DLL SHA-256
MVID
runtime evidence IDs
```

Update the project-owned guarded local installer so it can safely replace the existing validated `0.0.1` installation with `0.0.2`:

1. require Kingmaker and UMM closed;
2. validate exact package/release manifest;
3. inventory all pre-existing mods;
4. back up only the existing `KingmakerBuffPlanner` directory;
5. stage and hash-verify the replacement;
6. atomically replace/install when possible;
7. verify every non-planner mod remains byte-identical;
8. roll back the prior planner version on any failure;
9. remove owned lock/staging state in `finally`;
10. preserve exact install evidence.

Install the validated `0.0.2` locally for human testing after all automated gates pass. Do not launch the game unless the existing guarded mission policy explicitly authorizes that scenario.

## Documentation and truthfulness

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

Correct the prior UI-complete claim. Preserve history, but state clearly that human playtesting found click-through and prototype presentation, invalidating the former UI acceptance gate.

This repair does not waive the existing save-safety blocker. Do not claim the entire product is fully qualified unless the distinct disposable `KBP_AUTOMATION_BASELINE` and `KBP_AUTOMATION_WORKING` lifecycle has also been satisfied and live resource/save semantics pass.

## Autonomy and stop policy

Work continuously through investigation, implementation, repair, tests, runtime qualification, packaging, guarded replacement install, and handoff.

Do not stop merely because:

- one UI approach fails;
- a native hierarchy differs from Wrath;
- a click blocker alone is insufficient;
- a test or runtime scenario fails;
- one listener does not fire;
- a prefab cannot be safely cloned;
- another coherent commit is complete;
- context compaction occurs.

Before any compaction, update `AUTONOMOUS-RESUME.md` with exact HEAD, state, and next command.

Stop only for a true mission-defined critical hard stop: unsafe or unrecoverable live `Mods` state, protected-save risk, unsupported exact game environment, licensing barrier, required credential/dialog, or an irreducible product decision not answered here.

Begin now by reading the human evidence, inspecting the exact current HUD/full-screen UI code and native Kingmaker service-window input lifecycle, and writing the root-cause findings before selecting the replacement implementation. Continue without waiting for routine permission.
