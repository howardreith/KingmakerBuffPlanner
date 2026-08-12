# Codex Fresh-Session Mission: Rebuild the Planner UI Around a Four-Column Buff Grid

Work in:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

Use **High reasoning**.

This mission deliberately authorizes a **from-scratch replacement of the full-screen planner presentation layer**. Do not preserve the current planner-screen composition merely because it already exists.

Preserve the functioning domain/application/runtime layers and the successful HUD/input/modal lifecycle, but replace the planner screen's layout, interaction model, labels, filters, target workflow, and settings presentation according to this mission.

## Human product decision

The current parchment color direction is accepted.

The current UI plan is rejected.

The new UI must follow this workflow:

1. Choose the active routine at the top:

   ```text
   Long | Important | Short
   ```

2. Browse buffs in a large four-column icon grid.
3. Click one buff card to select it.
4. Click the portraits of the characters/pets who should receive that buff in the **currently active routine**.
5. The target click itself adds/removes the selected buff from the active routine.
6. There is no separate `Add to Long`, `Add to Important`, or `Add to Short` step.
7. Switch routine tabs to configure the same or another buff for a different routine.
8. Execute the active routine from one clear action button or from the existing HUD quick-action button.

This is the core interaction model. Do not preserve an old interaction that contradicts it.

## Human evidence

Read-only screenshots are available under:

```text
C:\Dev\KingmakerBuffPlannerLab\incoming\ui-grid-rebuild\
```

Expected files:

```text
01-current-white-hud-icons-rejected.png
02-current-parchment-layout-to-replace.png
03-prior-dark-gold-hud-icons-preferred.png
04-bubblebuffs-four-column-reference.png
```

Interpretation:

- `01` shows the current overly bright white HUD icons. Their treatment is rejected.
- `02` shows the current planner. Preserve its Kingmaker parchment/burgundy/gold direction, but replace its structure.
- `03` shows the earlier dark/gold HUD icon treatment that the user preferred.
- `04` is a BubbleBuffs information-design reference. Do not copy or redistribute its assets.

## BubbleBuffs source reference

Study the local source at:

```text
C:\Dev\KingmakerBuffPlannerLab\reference-source\BubbleBuffs
```

Read at minimum:

```text
BubbleBuffs\BubbleBuffer.cs
BubbleBuffs\UIHelpers.cs
BubbleBuffs\Utilities\Searchbar.cs
BubbleBuffs\BufferState.cs
BubbleBuffs\SaveState.cs
LICENSE
```

Focus on:

```text
BubbleBuffSpellbookController.CreateWindow
BufferView.MakeBuffsList
BubbleSpellView.BindBuffToView
MakeGroupHolder
MakeDetailsView
PreviewReceivers
UpdateTargetBuffColor
UpdateCasterDetails
GlobalBubbleBuffer.AddButton
```

Adapt these ideas, not Wrath-specific hierarchy paths or visual assets:

- broad multi-column icon grid;
- actual blueprint icons and names;
- selected buff drives portrait target editing;
- portrait overlays communicate selected/valid/fulfilled state;
- routine readiness counts;
- compact source-category tabs;
- native game UI components and tooltips.

## Branch and baseline safety

Create a fresh UI branch from the exact current clean successful build:

```text
codex/ui-grid-rebuild
```

If that branch already exists, inspect and resume it safely.

Before editing:

1. Record exact starting branch, commit, version, package SHA-256, DLL SHA-256, MVID, and installed identity.
2. Preserve the current validated package in a release-candidate backup directory.
3. Verify no unresolved deployment/runtime transaction.
4. Run and record the unchanged complete baseline.
5. Update `AUTONOMOUS-RESUME.md` and the journal with this mission's explicit authorization to replace the planner presentation layer.
6. Do not reset, rebase, force-checkout, or rewrite prior history.

## Frozen mechanics and lifecycle

Do not redesign or weaken:

```text
party/source discovery
beneficial-effect classification
catalog keys
provider keys
resource accounting
cast planning
saved assignment identities
profile atomic writes/migrations
animated execution
instant execution
confirmed-effect success semantics
HUD hitboxes/input capture
modal input lease
F10 replacement/hotkey lifecycle once migrated
guarded runtime transactions
optional-mod compatibility
```

The presentation layer consumes these services through view models and commands. It is not a new source of truth.

If current UI classes mix business logic and view construction, extract narrow application commands/adapters before deleting the old views.

## Replace, do not incrementally preserve, the planner screen

The old full-screen planner composition may be deleted and rebuilt.

Remove the production UI for:

```text
narrow left-hand vertical text list
large always-visible technical details layout
Add to Long / Add to Important / Add to Short buttons
Hide button
Show hidden control
hidden-entry workflow
Configured only wording
Advanced Filters
Casting Source section
Advanced Casting Source section
provider priority/cap controls
duplicate Mode button
technical provider/resource strings in the normal screen
```

Do not leave invisible obsolete controls, listeners, hitboxes, or alternate paths behind.

# Required new layout

At a 1920×1080 reference resolution, use this hierarchy:

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ BUFF PLANNER                                                ⚙ Settings   X │
│ Long 4/4 ready        Important 0/0        Short 0/0                       │
├──────────────────────────────────────────────────────────────────────────────┤
│ Search buffs...        All | Spells | Abilities | Other     [Selected only]│
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  [ICON] Bless        [ICON] Guidance     [ICON] Shield       [ICON] Light   │
│  name / status       name / status       name / status       name / status  │
│                                                                              │
│  [ICON] Aid          [ICON] Resistance   [ICON] Virtue       [ICON] ...     │
│                                                                              │
│           large scrollable four-column card grid across the screen           │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [large selected icon] Bless — level/source — duration                       │
│ Short description / status                                                   │
│                                                                              │
│ Targets for Long:  [portrait] [portrait] [portrait] [pet]                   │
│                    colored direct-selection states                           │
│                    Select All Valid | Clear Targets                           │
│                                                                              │
│ Plan: 1 cast • 4 of 4 targets covered                    Apply Long | Close  │
└──────────────────────────────────────────────────────────────────────────────┘
```

The exact ornamentation may vary, but this information architecture is required.

## Four-column buff grid

Requirements:

1. The grid occupies the broad top/middle region, not a narrow sidebar.
2. At 1920×1080 and normal UI scale, it displays exactly four cards across.
3. It remains usable at 1600×900 and common UI scales without horizontal scrolling.
4. Use a vertical `ScrollRect`; mouse wheel scroll stays inside the planner and never zooms the world.
5. Use pooling/virtualization or the existing widget cache where necessary for large Call of the Wild catalogs.
6. Do not instantiate thousands of expensive cards every frame.
7. Preserve selected card and scroll position during harmless refreshes when the entry still exists.
8. Search/category/routine changes may rebuild the visible dataset deterministically.

Each card shows:

```text
actual ability/spell icon
player-facing name
short availability text
optional small L/I/S assignment badges
configured/fulfillment status
selected-card state
```

Examples:

```text
Bless
1 prepared
2 targets selected

Guidance
At will
1 target selected
```

If two distinct entries have the same name/icon but different source semantics, show a compact player-facing badge such as `Spell` or `Ability`. Do not merge distinct domain keys in this UI mission.

## Card status colors

Preserve the accepted parchment palette.

Use restrained accents:

```text
Neutral:
  not selected for the active routine

Gold/burgundy selection:
  current card

Green:
  selected for the active routine and fully fulfillable

Amber:
  selected but partially fulfillable or currently resource-limited

Red:
  selected but invalid/unfulfillable

Muted:
  unavailable but still shown
```

Use borders/ribbons/background accents rather than neon full-card fills.

## Source category tabs

The only source-category controls in the normal planner are:

```text
All | Spells | Abilities | Other
```

Definitions:

```text
Spells:
  spellbook-backed prepared/spontaneous/special spell sources

Abilities:
  class, racial, resource, and activatable ability sources

Other:
  supported item, consumable, equipment, or uncategorized sources

All:
  every supported source
```

The labels must be clear buttons/tabs with selected state and counts where useful.

Do not include an Advanced Filters drawer.

Do not expose internal source enums.

## Search and Selected only

Primary controls:

```text
Search buffs...
Selected only
```

`Selected only` means:

> Show only buffs with at least one selected target in the currently active routine.

It does not mean selected in any routine and it does not mean the currently focused card.

Add a tooltip explaining this exact behavior.

Default is off.

Sorting is alphabetical by default and has no visible sort control.

Availability is represented on cards rather than used as a hidden default filter. Configured-but-unavailable entries remain visible with an amber/red state.

## Remove hiding completely

There is no Hide button and no Show hidden option.

Migrate existing profiles so entries previously marked hidden become visible.

Prefer a versioned migration that clears/ignores UI hidden flags while preserving:

```text
buff identities
routine assignments
targets
execution settings
provider preferences
```

If the persisted schema must retain a legacy hidden field for backward compatibility, production filtering must ignore it and no UI may modify it.

Document the migration.

## Direct routine assignment by target click

The active routine tab is the edit context.

Interaction:

1. Select a buff card.
2. Click a valid portrait.
3. If that portrait was not selected for the active routine, add it.
4. If it was selected, remove it.
5. Save atomically through the existing application command.
6. Recalculate the plan.
7. Update card, portrait, routine, and plan status immediately.

There is no separate Add-to-routine action.

Clearing the final target removes the buff from that routine naturally.

Switching Long/Important/Short displays the target set for that routine.

Hover preview must not mutate saved configuration.

Provide:

```text
Select All Valid
Clear Targets
```

These act on the selected buff and active routine.

## Portrait state language

Use native Kingmaker portrait frames/tints where safe.

```text
Green:
  selected and currently covered

Amber:
  selected but not currently coverable

Red/disabled:
  illegal target

Neutral:
  not selected

Secondary subtle overlay:
  indirect recipient of an area/mass cast
```

Portrait labels should use character names, not IDs.

## Remove casting-source UI

Remove all normal-screen provider/caster controls, including:

```text
Casting Source
Advanced Casting Source
priority +/- controls
caps
provider enable/disable
spellbook/resource technical rows
```

The planner continues to select the casting source automatically through the existing backend.

Do not delete provider/resource logic.

Do not change planning policy.

Preserve legacy saved provider preferences for compatibility, but do not expose them in this simplified UI. Add bounded diagnostics for legacy preferences if necessary.

New configurations use automatic provider selection.

User-facing failures should say:

```text
No eligible caster is currently available.
No prepared slot remains.
The selected targets cannot all be covered.
```

They should not require the user to understand provider internals.

## Settings and Mode

There is exactly one Settings entry, preferably the existing gear button.

It contains:

```text
Casting mode: Animated | Instant
Combat use: Blocked | Allowed
Existing buffs: Skip active | Recast
Fallback: Allowed | Disabled
Planner hotkey
```

Remove every duplicate Mode button from the main action bar.

The normal bottom actions are:

```text
Apply <active routine>
Close
```

Refresh may be automatic or a small secondary action if still necessary.

## Replace F10

Remove F10 as the default planner hotkey.

Use a configurable chord with default:

```text
Ctrl+Shift+B
```

Requirements:

1. Add the binding to Settings/UMM configuration.
2. Display it in the Setup HUD tooltip.
3. Do not trigger an underlying native `B` action when the chord is consumed.
4. If exact Kingmaker input inspection proves `Ctrl+Shift+B` cannot be isolated safely, use `Ctrl+Shift+P` as the documented fallback and explain why.
5. Do not use `Ctrl+F10`, which is reserved for the UMM console.
6. Do not leave F10 simultaneously active after migration unless the user explicitly rebinds it.
7. Existing F10-configured state migrates to the new default or an explicit saved custom binding.

Test the chord in gameplay, service windows, and while the planner is open.

## Restore the preferred HUD icon treatment

The current bright white HUD icons are rejected.

Restore the earlier dark/gold/native treatment shown in:

```text
03-prior-dark-gold-hud-icons-preferred.png
```

Prefer restoring the exact prior accepted sprites/tints from Git history.

Do not change:

```text
HUD row anchors
button RectTransforms
hitboxes
listeners
tooltip lifecycle
input capture
scene/UI rebuild lifecycle
```

This should be a sprite/tint treatment change only.

## Details area

Keep the selected-buff details concise:

```text
large icon
name
source badge
level when applicable
duration
description
target portraits
plan result
```

Remove technical data from the default display:

```text
Fact
provider keys
CL internals unless player-relevant
raw resource enums
CAP ANY
remaining unlimited
```

Show exact failures in plain language.

Do not make the detail area so tall that it competes with the grid.

# Presentation architecture

Create or use explicit presentation components:

```text
PlannerScreenView
PlannerRoutineTabsView
BuffGridView
BuffCardView
BuffCardPool
PlannerCategoryTabsView
PlannerTargetStripView
PlannerSelectedBuffView
PlannerSettingsView
PlannerScreenViewModel
BuffCardViewModel
TargetPortraitViewModel
```

The view model may format:

```text
name
icon
availability
category
routine badges
status color token
target state
plan summary
```

It may not alter domain state directly.

UI callbacks invoke existing application commands.

Delete or retire obsolete planner-screen classes after the replacement is proven. Do not leave both layouts active behind feature flags unless a temporary rollback flag is explicitly documented and removed before release.

# Implementation sequence

## Phase 0 — baseline and UI inventory

- preserve the current working package;
- record baseline tests/hashes;
- inspect BubbleBuffs grid/portrait patterns;
- inspect exact Kingmaker native assets;
- write a concise replacement architecture document;
- identify old planner-screen classes to delete/retire.

Continue automatically.

## Phase 1 — new screen shell behind a temporary development flag

- build routine tabs;
- search;
- category tabs;
- Selected only;
- empty four-column grid host;
- selected-buff/target panel;
- settings/action bar;
- preserve modal/input lifecycle.

Use detached/UI fixtures only initially.

## Phase 2 — icon card grid

- card view models;
- actual icons;
- pooling/virtualization;
- selected/status styling;
- search/category/selected-only datasets;
- scrolling.

## Phase 3 — direct target assignment

- portrait strip;
- direct current-routine toggle;
- select all/clear;
- readiness counts;
- plan summaries;
- persistence roundtrip.

## Phase 4 — remove obsolete UX

- remove Add-to-routine controls;
- remove Hide/Show hidden;
- migrate hidden flags;
- remove provider/casting-source presentation;
- remove technical filters;
- remove duplicate Mode;
- replace hotkey;
- restore dark/gold HUD icons.

## Phase 5 — production switch and cleanup

- enable only the new screen;
- delete/retire old views/listeners;
- verify no stale hitboxes or hidden UI;
- remove temporary flag.

Do not pause for routine approval between phases.

# Required deterministic tests

Prove:

```text
exactly four columns at 1920×1080
usable grid at 1600×900
no horizontal scrolling
vertical scroll consumes input
card icons use blueprint icon/fallback
card status maps deterministically
All/Spells/Abilities/Other filtering
Selected only uses active-routine targets
alphabetical default
no Hide/Show hidden UI
legacy hidden entries migrate visible
no Add to Long/Important/Short UI
target click directly toggles active-routine assignment
clear final target removes active-routine assignment
switching routine changes target state
Select All Valid and Clear Targets
no Casting Source/Advanced Casting Source UI
automatic provider backend unchanged
one Settings Mode control
no duplicate bottom Mode
Ctrl+Shift+B binding/migration/consumption
F10 no longer active by default
preferred HUD sprite/tint restored without hitbox changes
no duplicate cards/listeners after 20 refresh/open cycles
old layout absent from production hierarchy
```

# Required physical runtime regression

Using only the authorized disposable campaign:

1. open with the new hotkey;
2. verify the four-column grid;
3. verify actual icons and names;
4. filter All/Spells/Abilities/Other;
5. use Selected only in each routine;
6. select Bless;
7. click one portrait under Long;
8. verify Long assignment persists without an Add button;
9. switch to Important and verify independent targets;
10. switch back to Long and verify persistence;
11. use Select All Valid and Clear Targets;
12. execute Long in Animated mode and confirm effects/resources;
13. execute a controlled routine in Instant mode and confirm effects/resources;
14. save/reload profile and confirm assignments;
15. verify HUD quick actions unchanged;
16. verify new hotkey does not trigger native actions;
17. verify white icons are gone and preferred dark/gold treatment is present;
18. verify no obsolete Hide/provider/technical controls exist;
19. verify no world click/scroll leakage;
20. verify exact Mods restoration.

Run native and Call of the Wild regression profiles.

Test large catalogs for acceptable open/search/scroll latency and bounded allocations.

# Human visual acceptance

The next package remains human-gated.

Checklist:

```text
dark/gold HUD icons restored
new hotkey works and F10 is gone
four cards across
grid is easy to scroll
icons and names are immediately readable
routine tabs clearly define edit context
target click directly assigns/removes
no extra Add to Long click
Selected only wording and behavior are clear
no Hide/Show hidden controls
only All/Spells/Abilities/Other source tabs
no Casting Source/provider UI
only one Mode setting
parchment colors remain
portrait status colors make sense
no cramped/clipped text
no regression in Animated/Instant casting
```

Human verdict is authoritative.

# Versioning, packaging, and install

Use the next repository-consistent version.

Before guarded local install:

```text
focused new-UI tests
complete deterministic suite
repository validation
exact-reference Release build
package validation
native regression
Call of the Wild regression
animated physical execution
instant physical execution
profile migration/roundtrip
hotkey/input regression
large-catalog performance
two fresh-process final UI runs
exact Mods restoration
clean HEAD
git diff --check
```

Use the guarded installer to replace only the current Buff Planner version and preserve every other mod byte-for-byte.

Final handoff:

```text
branch
starting commit/version/package
final commit/version
package SHA-256
DLL SHA-256
MVID
migration result
runtime run IDs
performance measurements
before/after screenshots
remaining uncertainty
no merge/public release statement
```

# Documentation

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
THIRD_PARTY_NOTICES.md
```

Document BubbleBuffs source/commit and the adapted design ideas. Preserve required MIT notices for any copied or substantially adapted code.

Do not rewrite MVP history.

# Stop policy

Continue autonomously through architecture replacement, implementation, migration, tests, runtime qualification, package, guarded install, and handoff.

Do not stop because:

- most current screen code must be deleted;
- one grid implementation is unsuitable;
- virtualization requires another iteration;
- a UI test/runtime run fails;
- a phase or commit completes;
- context compaction occurs.

Before compaction, update `AUTONOMOUS-RESUME.md` with exact HEAD, external state, completed phase, and next command.

Stop only for a true critical boundary:

- regression of frozen mechanics that cannot be isolated safely;
- protected-save risk;
- unresolved/unrestorable Mods transaction;
- unsupported exact environment;
- required credential/dialog;
- licensing barrier;
- an irreducible product decision not answered here.

Begin by preserving the current functional build and writing the explicit delete/retain map for the old presentation layer. Then implement the new planner from the shell outward.
