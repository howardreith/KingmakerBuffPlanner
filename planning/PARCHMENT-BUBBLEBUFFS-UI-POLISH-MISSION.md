# Codex Mission: Kingmaker-Native Parchment UI and BubbleBuffs-Inspired Presentation Polish

Work in:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

Use **High reasoning**.

This is a presentation-focused refinement of an already successful MVP. Direct human testing confirms that the core mod works:

- buffs are discovered and visibly listed;
- the player can select targets;
- assignments persist into Long;
- animated execution successfully casts the configured spells;
- instant execution successfully applies them;
- the HUD icons, tooltips, click isolation, F10 modal, close lifecycle, and input restoration work.

Treat those behaviors as frozen regression contracts. This mission is not permission to redesign discovery, planning, resource accounting, persistence, animated execution, instant execution, or the guarded runtime infrastructure.

## Human objective

Make the setup experience feel like a native Pathfinder: Kingmaker service window and borrow the strongest information-design ideas from BubbleBuffs without copying Wrath assets or blindly porting Wrath-specific code.

The current interface is functionally successful but visually technical and hard to understand:

- the dark brown/black palette does not resemble Kingmaker's parchment service screens;
- the catalog rows are text-only and do not use spell/ability icons;
- the target state is not communicated as clearly as BubbleBuffs;
- `CONFIG: ALL`, `DURATION: ALL`, `SOURCE: ALL`, `SORT: NAME`, `HIDDEN: OFF`, and `AVAIL: ONLY` read like developer controls rather than player-facing UI;
- the bottom `MODE` button duplicates the mode control already present in Settings;
- `PROVIDERS AND RESOURCES` exposes implementation vocabulary without explaining the player's decision;
- technical strings such as `Fact`, `CL 1`, `Unlimited`, `remaining unlimited`, `CAP ANY`, and similar fields are too prominent for the default view.

## Human reference evidence

Read-only screenshots are available under:

```text
C:\Dev\KingmakerBuffPlannerLab\incoming\ui-parchment-bubblebuffs\
```

Expected files:

```text
01-current-working-kbp-ui.png
02-current-working-buffs-applied.png
03-bubblebuffs-ui-reference.png
```

The BubbleBuffs screenshot is an interaction/information-design reference only. Do not copy or redistribute its visual assets.

## Source study

The open-source BubbleBuffs repository already exists locally at:

```text
C:\Dev\KingmakerBuffPlannerLab\reference-source\BubbleBuffs
```

Read at minimum:

```text
BubbleBuffs\BubbleBuffer.cs
BubbleBuffs\UIHelpers.cs
BubbleBuffs\Utilities\Searchbar.cs
BubbleBuffs\Utilities\AssetLoader.cs
BubbleBuffs\SaveState.cs
LICENSE
```

Focus on these BubbleBuffs structures:

```text
BubbleBuffSpellbookController.CreateWindow
MakeFilters
MakeDetailsView
MakeGroupHolder
BufferView.MakeBuffsList
BubbleSpellView.BindBuffToView
BufferView.PreviewReceivers
BufferView.UpdateTargetBuffColor
BufferView.UpdateCasterDetails
BufferView.MakeSummary
GlobalBubbleBuffer.TryInstallUI / AddButton
```

Document what is learned and what is deliberately not copied.

## BubbleBuffs design principles to adapt

### 1. Native game components create visual coherence

BubbleBuffs builds its screen by cloning and adapting the game's own:

- spellbook known-spell cards;
- service-window frame;
- search field;
- toggle controls;
- standard buttons;
- selected-state ornament;
- party portrait frames;
- HUD button prefab;
- native fonts and tooltips.

For Kingmaker, inspect the exact installed 2.1.7b hierarchy and reuse the safest equivalent Kingmaker-native sprites, fonts, panels, frames, buttons, toggles, and portrait treatments.

Do not copy Wrath hierarchy paths. Do not create brittle dependencies on a screen that may be absent. Every reused native prefab/sprite must have:

```text
exact source hierarchy/path
required components
fallback behavior
lifecycle/cleanup owner
runtime validation
```

If a native prefab cannot be reused safely, keep the existing working programmatic UI structure and apply game-native sprites/fonts/colors to it rather than replacing working behavior.

### 2. Buffs are icon-first cards, not technical text rows

BubbleBuffs uses the ability's actual blueprint icon, name, and resource/assignment summary in a native spell card. It color-codes the card according to whether a requested routine is fulfillable.

Replace the current text-only rows with readable Kingmaker-themed cards.

Each card must show:

```text
ability/spell icon
buff name
optional small group badge: L / I / S
short availability summary
optional provider portrait/badge only when useful
selected state
configured/fulfillment state
```

Default availability text should be player language, for example:

```text
1 prepared
3 available
At will
2 targets configured
1 of 2 targets available
```

Do not show internal terms such as provider keys, source IDs, `Fact`, or raw implementation enums in the card.

### 3. Color communicates configuration state

Use a restrained Kingmaker-appropriate status language.

Suggested semantics:

```text
Neutral parchment:
  not configured for any routine

Burgundy/gold selected border:
  currently selected card

Green accent/background:
  configured and fully fulfillable now

Amber/gold accent:
  configured but only partially fulfillable, queued, or lacking some resources

Red accent:
  configured but currently invalid/unfulfillable

Muted/gray:
  hidden, unavailable, or intentionally disabled
```

Do not tint the entire interface neon green/red. Use borders, ribbons, small backgrounds, or ornaments consistent with Kingmaker.

Include a small legend or tooltips the first time status colors appear.

### 4. Party portraits are the primary target editor

Move target selection visually to the foreground.

For the selected buff, show party and pet portraits in a clear row similar to BubbleBuffs.

Required portrait state language:

```text
Green overlay/check/border:
  selected and the planner can fulfill it

Amber overlay/check/border:
  selected but not currently fulfillable

Red overlay/disabled:
  cannot legally target this unit

Neutral or muted:
  not selected

Secondary translucent overlay:
  indirect beneficiary of a mass/area cast, when applicable
```

Hovering a buff card should preview its target state without changing saved configuration.

Clicking a portrait should toggle that target exactly once.

Provide:

```text
Select All Valid
Clear Targets
```

Use native portrait frames when safe. Do not copy BubbleBuffs bubble shaders or art; simple color overlays/checkmarks/borders are sufficient.

### 5. Advanced provider controls should not dominate the screen

The current `PROVIDERS AND RESOURCES` section contains real and important mechanics, but the label and presentation are confusing.

Rename it to:

```text
CASTING SOURCE
```

Default collapsed summary:

```text
Automatic
Ret — Cleric — 1 prepared
```

or:

```text
Automatic — best available caster
```

When there is only one legal provider, show one simple read-only summary.

When several providers exist, allow selection or priority in an expandable:

```text
Advanced Casting Source
```

Only the advanced drawer should expose:

```text
provider priority
enable/disable provider
per-provider cap
spellbook identity
remaining casts
self-only limitation
special resource options
```

Rename controls into player language. Avoid `CAP ANY`; prefer:

```text
No limit
Maximum 2 casts
Disabled
Automatic priority
```

Add tooltips explaining why someone would change these settings.

Do not remove provider logic. Change only its presentation and disclosure level.

### 6. Filters should be minimal and comprehensible

Remove the current always-visible cycling filter row:

```text
CONFIG
DURATION
SOURCE
SORT
HIDDEN
AVAIL
```

The normal setup workflow does not need six technical filters.

Default catalog behavior:

```text
show all non-hidden supported buffs
sort alphabetically
show available and configured-but-unavailable buffs
search by name
```

Primary controls:

```text
Search
Show configured only
Show hidden
Reset
```

`Show configured only` and `Show hidden` may be compact checkboxes/toggles beneath Search.

Sorting should default to Name and need no visible control unless another genuinely useful sort exists.

If source/category filtering remains necessary for large modded parties, place it inside a compact `Advanced Filters` drawer using player-facing labels:

```text
Spells
Abilities
Items
Consumables
Available now
Unavailable
Long duration
Short duration
```

Hide empty categories. Do not show `Items` or `Consumables` if production support is not actually available.

Persist filter preferences only when that is already safe, but never allow a stale filter to create an unexplained empty catalog.

### 7. Group/routine controls need one clear model

Preserve Long, Important, and Short execution semantics.

Use one clear interaction model:

- the top strip shows routine summaries:

  ```text
  Long     4/4 ready
  Important 0/0
  Short    0/0
  ```

- selecting a routine tab chooses the current routine being edited/executed;
- each card may show its assigned routine badge;
- the selected-buff detail uses one segmented choice:

  ```text
  Long | Important | Short
  ```

  or a clear `Add to <current routine>` / `Remove from routine` control.

Do not create two different controls that appear to perform the same group assignment.

Top counts must mean:

```text
fulfilled target assignments / requested target assignments
```

Explain that in a tooltip.

### 8. Settings should be compact and non-duplicated

Remove the bottom `MODE` button.

Keep one Settings panel or gear button containing:

```text
Casting mode: Animated | Instant
Combat use: Blocked | Allowed
Existing buffs: Skip active | Recast/overwrite
Fallback: Allowed | Disabled
```

Use segmented controls or native toggles with explanatory tooltips.

Do not expose the same setting in two places.

The bottom action bar should contain only actions, for example:

```text
Refresh
Apply Long
Close
```

or:

```text
Refresh
Apply Current Routine
```

If Refresh can safely happen automatically, make it a small secondary icon/button and leave the primary action visually dominant.

### 9. Selected-buff details should read like a game screen

The selected details area should show:

```text
large spell/ability icon
name
spell level/source type
duration
description
routine assignment
target portraits
casting source summary
concise plan status
advanced disclosure controls
```

Replace `Fact` with a player-facing source type:

```text
Spell
Ability
Item
Consumable
```

Do not display raw enum names.

Plan summary example:

```text
1 cast planned
1 of 1 targets covered
Existing buff will be skipped
```

Show failures in direct language:

```text
No prepared slot remains.
No legal target is selected.
This spell can target only the caster.
```

### 10. Keep HUD quick actions exactly as they now work

The four HUD icons, placement, stable tooltips, and input isolation are a successful part of the current build.

Do not redesign or reposition them in this mission unless a cosmetic sprite treatment can be changed without touching hierarchy, hitboxes, listeners, input capture, or lifecycle.

# Kingmaker visual direction

Create a theme token layer sourced from inspected Kingmaker assets rather than scattering colors through UI code.

Suggested token roles:

```text
ParchmentBackground
ParchmentPanel
ParchmentRaised
DarkBrownText
MutedBrownText
BurgundyPrimary
GoldAccent
GreenSuccess
AmberWarning
RedFailure
DisabledGray
NativeFrameSprite
NativeButtonNormal/Pressed/Hover
NativeToggleOn/Off
NativePortraitFrame
NativeSelectedOrnament
NativeHeaderFont
NativeBodyFont
```

Prefer colors sampled from or already used by the exact Kingmaker prefab/sprites. Hardcoded fallback colors may exist only behind centralized tokens.

The target visual should resemble Kingmaker's Character and Spellbook service windows:

- warm parchment;
- burgundy;
- antique gold;
- brown text;
- restrained dark outlines;
- native decorative borders;
- readable contrast.

Do not make it look like Wrath's purple spellbook screen. Adapt BubbleBuffs's information architecture to Kingmaker's visual language.

# Architecture and regression safety

## Create a UI-only feature branch

From the exact clean successful MVP commit, create or reuse:

```text
codex/ui-parchment-bubblebuffs
```

Do not work directly on `main`.

Before editing:

1. record exact branch/HEAD/version/package hashes;
2. preserve the currently working package under a release-candidate backup directory;
3. verify no unresolved runtime/deployment transaction;
4. run and record the complete unchanged baseline suite;
5. update the journal/resume with the cosmetic-only mission boundary.

## Freeze mechanics

Do not modify:

```text
catalog discovery/classification
BuffKey/ProviderKey identities
resource ledgers
cast planning
profile persistence schema or migrations
animated executor
instant executor
confirmed-effect semantics
HUD input isolation
modal input lease
guarded runtime transaction framework
```

A presentation adapter may consume those services, but UI code must not become a new source of truth.

Use explicit UI view models such as:

```text
BuffCardViewModel
TargetPortraitViewModel
CastingSourceSummaryViewModel
RoutineSummaryViewModel
PlannerSettingsViewModel
```

View models may format player-facing text and status colors, but cannot change domain semantics.

Every UI callback should invoke existing application/service commands.

## Incremental implementation

Implement in coherent phases:

### Phase A — Kingmaker theme shell

- parchment background and panels;
- native fonts/buttons/frames;
- centralized theme tokens;
- preserve existing layout and behavior.

Run full regressions before continuing.

### Phase B — icon-first buff cards

- icons;
- names;
- group badges;
- concise availability text;
- selection/status colors;
- native tooltips.

Run full regressions before continuing.

### Phase C — target portrait editor and details hierarchy

- portrait overlays;
- Select All Valid / Clear Targets;
- selected-buff icon/details;
- concise plan status.

Run full regressions before continuing.

### Phase D — simplify filters/settings/provider presentation

- remove technical primary filters;
- advanced filter drawer;
- one Mode control;
- Casting Source summary plus advanced drawer;
- remove duplicate/technical labels.

Run full regressions before final qualification.

Do not wait for routine human approval between phases. Preserve exact state and continue.

# Required tests

## Deterministic/UI tests

Prove:

- theme tokens resolve from native assets or documented fallbacks;
- no Wrath-only asset or type enters the package;
- every included buff card has an icon when the blueprint supplies one;
- missing icons use a stable neutral fallback;
- card name and player-facing availability text are nonempty;
- status color semantics are deterministic;
- selected card state is distinct;
- target portrait states map correctly to target legality/request/fulfillment;
- hover preview does not mutate saved targets;
- Casting Source summary matches provider planner state;
- advanced provider controls preserve existing behavior;
- only one Mode control exists;
- removed technical filters are absent from the primary view;
- Reset Filters restores the default catalog;
- no duplicate listeners or UI objects after 20 refresh/reopen cycles;
- current HUD and modal lifecycle tests remain green.

## Real campaign acceptance scenario

Using only the authorized disposable working save, prove:

1. the same catalog and row names remain available;
2. Bless and representative ability entries retain the same keys/providers;
3. selecting targets creates the same plan;
4. Animated execution retains the same confirmed effects and resource deltas;
5. Instant execution retains the same confirmed effects and resource deltas;
6. profile save/reload retains assignments;
7. HUD quick execution remains unchanged;
8. UI clicks do not reach world input;
9. screenshot evidence exists for:
   - catalog;
   - selected details;
   - target colors;
   - casting source collapsed;
   - advanced settings;
10. exact Mods restoration passes.

The visual scenario must not change a valued campaign.

## Human visual acceptance

Automation cannot claim the visual work complete.

Create/update a checklist for the user:

- screen looks like Kingmaker rather than a debug tool;
- parchment, burgundy, gold, fonts, and borders feel native;
- buff cards show recognizable spell icons;
- configured/ready/warning/error states are understandable;
- target portrait colors are immediately understandable;
- card selection and portrait selection feel responsive;
- filters no longer require guessing;
- only one Mode control exists;
- Casting Source is understandable without knowing mod internals;
- advanced provider controls are available but unobtrusive;
- Long/Important/Short workflow is clear;
- no text is cramped, clipped, or too low-contrast;
- current working Animated/Instant functionality remains intact.

Human verdict is authoritative.

# Versioning, package, and guarded installation

Use the next repository-consistent development version.

Before local installation:

```text
focused UI tests
complete deterministic suite
repository validation
exact-reference Release build
package validation
native regression
Call of the Wild regression
animated physical execution
instant physical execution
profile roundtrip
UI/input lifecycle
two fresh-process final visual/functional runs
exact Mods restoration
clean HEAD
git diff --check
```

Use the guarded installer to replace only the existing Buff Planner version while preserving every other mod byte-for-byte and rolling back on failure.

Final handoff must include:

```text
feature branch
starting MVP commit/version/package hash
final commit/version
package SHA-256
DLL SHA-256
MVID
native asset/prefab inventory
before/after screenshots
runtime run IDs
functional regression results
remaining visual uncertainty
explicit no-merge/no-public-release statement
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

Record BubbleBuffs source paths/commit and exact adapted ideas. Preserve its MIT notice if code is copied or substantially adapted.

Do not rewrite the successful MVP history.

# Stop policy

Continue autonomously through design inventory, incremental implementation, tests, screenshots, package, guarded install, and human handoff.

Do not stop because:

- one Kingmaker prefab is unsuitable;
- a safe fallback token is needed;
- one visual phase fails;
- a screenshot differs from BubbleBuffs;
- a test/runtime run fails;
- a commit completes;
- context compaction occurs.

Before compaction, update `AUTONOMOUS-RESUME.md` with exact HEAD, completed phase, current external state, and next command.

Stop only for a true critical boundary:

- regression of core function that cannot be isolated safely;
- protected-save risk;
- unresolved/unrestorable Mods transaction;
- unsupported exact game/UMM environment;
- required credential/dialog;
- licensing barrier;
- an irreducible product decision not answered here.

Begin by preserving and qualifying the current MVP baseline, then produce the BubbleBuffs/Kingmaker UI forensic design document before changing presentation code.
