# Remaining work — 0.4.0 release train (owner-review candidate)

Program handoff for the consolidated 0.4.0 candidate. One concise record;
bulky evidence stays under the lab's `runtime-evidence\<runId>\`.

- Branch: `claude/kbp-complete-remaining-work-2026-10-09` (worktree
  `private\worktrees\KBP040`), created from `origin/main`
  `16f87ed1dc9b00b683ac7192f4ae8d95da7853fc` (WP2A merged, PR #4).
- Public release: `v0.3.0`. No 0.4.0 tag, merge, or release is authorized.
- Shell rule: run every gate/harness/launcher script through Windows
  PowerShell 5.1 as `powershell.exe -NoProfile -NonInteractive -Command
  "& .\scripts\Name.ps1 ..."`. Launched with `-File`, Test-RuntimeHarness
  fails spuriously (its script-scoped `$WhatIfPreference` reaches the 5.1
  `Get-FileHash`, which then returns nothing); the qualified WP2A head
  reproduces the same failure under `-File`.

## Package status

| Package | Commits | Regressed | Source-qualified | Native-qualified | Owner |
|---|---|---|---|---|---|
| WP2B spellbook entry | `e84a892` `e29c4ec` `11a30db` | yes | focused | physical PASS (`kbp040-wp2b-spellbook-02`) | pending |
| WP4 execution policy | `d0d43e4` `512af36` | yes | focused + full unit | physical `combat` expectation built (`fe06e26`); runs in the campaign | pending |
| WP3 direct manipulation | `0274b7c` `898cfeb` `14c7902` | yes | focused + full unit | physical `authoring` expectation built (`fe06e26`); runs in the campaign | pending |
| WP6 Magic Circle | `3c80ed0` + recipe `ad7ca01` `2dde708` `2ec8e63` | yes | focused + full unit | `sticky-touch-direct` recipe built; runs in the campaign | pending |
| WP5 catalogue audit | `d8614ef` | yes | focused + full unit | catalogue exports PASS (native-only, full-user); in-game buff list spot check | pending |
| WP7 parchment | `d85e6e5` `ff9e877` `17d7420` `91d1c6e` `7383347` `d8b659e` | yes | focused + full unit | live paper drawn at 1920x1080 and 1280x720 (`kbp040-wp7-sel-*`) | pending (visual judgement is the owner's) |

"Full unit" is the complete `KingmakerBuffPlanner.Tests.exe` suite (446/446 at
`d8614ef`), not the complete `Test-SourceOnly.ps1` gate.

The complete `Test-SourceOnly.ps1` gate runs once on the final integrated
head.

## WP2B — spellbook Buff Planner button

Root cause (from the recorded native UI contract,
`runtime-evidence\beta-0dd82937-adv-01\native-ui-contract.json`, 1920x1080):

1. The owned button was pinned 20/18 units inside the spellbook window's
   top-right corner. `SpellBook/BookBackground` is full screen and the
   service window's top-bar close button `ServiceWindow/Top/Close`
   (143x137, anchored top-right) occupies exactly that area, so a click
   reached the native Close, not the planner.
2. The handoff searched the spellbook subtree for a `Button` named
   `Close`. The only such button is `ServiceWindow/Top/Close`, outside that
   subtree (the spellbook's own "Close" nodes are per-slot containers whose
   buttons are named `Button`), so every click that did arrive was refused
   with `native-close-affordance-missing`.

Repair (`e84a892`): the button is the last child of the service-window root
(drawn and raycast above the top bar) and is placed by
`SpellbookEntryPlacement` — an ordered candidate list checked against the
live native controls. The locator resolves the native close by its exact
path. Every handoff closes the window through it and waits until both the
FullScreenUi mode and the window have released; a failure after closure
disposes any half-open planner and reopens the spellbook through the game's
`IServiceWindowUIHandler.HandleOpenSpellbook`. Strays from an earlier
controller are retired. No second entry route was added.

Guarded scenario (`e29c4ec`, corrected in `11a30db`): `live-workspace-physical
-PhysicalExpectation spellbook`. The host presses the game's own `OpenSpells`
key binding (read at runtime: `B`), clicks the button with the OS pointer and
presses Escape, three times, then repeats once with one simulated opener
refusal (`SpellbookHandoffFaults`, armable only in a locked runtime-test
session). The launcher gained a single unmodified A–Z `key` action.
`Test-SpellbookEntryEvidence.ps1` (26 cases) runs in Test-SourceOnly.

Evidence:

- `kbp040-wp2b-spellbook-01` on `e29c4ec` — FAIL by the check, not the
  product: every cycle handed off correctly, but the record required the
  FullScreenUi mode to be inactive after the click, and the planner's own
  input lease raises that mode on open (it refuses to open while another
  owner holds it). Rejected theory: "FullScreenUi still active means the
  spellbook kept ownership" — the native window was hidden and the
  workspace had opened, which its lease forbids under a foreign owner.
- `kbp040-wp2b-spellbook-02` on `11a30db` — PASS, complete, restoration
  verified, protected saves compared and clean. Package
  `a32ea34b16ac0ae436e2278455092b11c7211627104d37de6e1031be6b97e9c1`, DLL
  `a30c4e9de10bd90c7ca7d4aca70cc95602509da4979982ea16d37e66e6a7cbee`, MVID
  `b470a9d8-20c8-4a28-828b-04255484b3ec`. Each ordinary cycle: one owned
  button, one runtime listener, the first EventSystem hit at its centre is
  the button, placement `below-native-close` conflict free, one native
  release, one native close, one opener call, one workspace open, lease
  held, service window hidden, FullScreenUi owned by the planner, Escape
  closes the planner with the lease released and no native menu. Fault
  cycle: opener refused, no planner, spellbook reopened with its button.
  World input: no movement/ability commands, selection unchanged.
  Screenshots `spellbook-*-open.png` / `spellbook-*-after-click.png`.

Uncertainty: placement was observed at 1920x1080 only; other resolutions are
covered by the WP7 visual runs.

## WP4 — execution policy

`CastingExecutionPolicy` (Domain/Planning) is the single contract:
`OutOfCombatOnly = true`, `AllowAnimatedFallback = false`.

- **Never in combat.** `CastingWorkspaceSession.Apply` refuses first, before
  compilation, persistence flush, authorization or projection, while
  `Player.IsInCombat`, with exactly `Buff routines cannot run during
  combat.`; no casting is named or focused, the planner is not opened,
  nothing is spent. Every route (HUD moon/diamond/sun, planner Run, Ready
  Casts Only, Classic) goes through it; the executors keep their per-step
  refusal for combat that starts mid-run.
- **Strict Instant.** `ExplicitCastingCompiler.Compile(strictInstant)` makes
  a casting whose strategy is not instant-capable Blocked with
  `instant-route-unavailable:<strategy>:<reason>`, so WP2A navigation
  focuses it; an already-active skip stays a skip; `HybridCastExecutor`
  runs strict and refuses such a step before any native work. Animated is
  explicit through the Mode button.
- **Profiles.** 0.3.0's `outOfCombatOnly` / `allowAnimatedFallback` stay in
  the schema, are read but not honoured
  (`LegacyExecutionPreferencesOverridden`), are not rewritten on load, and
  the next deliberate save writes the enforced values. The two toggles are
  gone from the workspace and the classic settings view (shown as fixed
  rows).
- Tests: `ExecutionPolicyTests.cs` (combat refusal with a counting dispatch
  boundary, strict blocker + WP2A focus, legacy fallback profile, strict
  hybrid); legacy expectations updated in AutosaveLifecycle and
  ProductionExecution tests.
- Native evidence still owed: combat refusal in both modes with no spend,
  the strict-Instant blocker deep link, an explicit Animated run.

## WP3 — direct manipulation and the reduced inspector

- Portrait clicks (`CastingWorkspaceSession.DirectManipulation.cs`):
  nothing focused - add, or focus the existing casting of that buff on that
  portrait (first by order, then id); a focused direct casting - its own
  recipient removes it, another portrait retargets it, an unreachable one is
  refused with the reason and nothing changes; a focused group casting -
  re-centres; caster/source clicks change the provider (keeping target,
  routine, order, existing-effect policy and enhancements the new provider
  has; dropped ones are named; Undo restores).
- Removed from the normal UI: Disable/Enable (a legacy Disabled casting
  loads, never casts, blocks nothing, can be removed; the qualification
  driver keeps a test-only seam), Duplicate, Cast By, the retarget/coverage
  menus, the required-recipient group menu and the obsolete header text.
  Share Transmutation rows appear only where the mechanism applies (a saved
  inapplicable Share stays visible with its reason).
- Group semantics: nobody is "required"; a member outside the area never
  blocks or doubles a cast; confirmation needs at least one reached
  recipient for a mass cast. 0.3.0 required-recipient choices load, are
  archived byte-exact once (`*.pre-0.4.0.orig`) on the first save, and no
  longer constrain anything.
- Tests: `DirectManipulationTests.cs` (11) plus updated graph, navigation,
  converter and importer tests.
- Physical evidence still owed: add/remove/retarget/recentre/provider
  change/Share visibility/Undo, and input isolation.

## WP6 — Magic Circle against Alignment

Diagnosis: Magic Circle is registered by KingmakerGunslinger
(`KMG.Spells.MagicCircle.*`; installed 0.0.140, advanced fixture 0.0.136).
Its castable ability holds a touch (`AbilityEffectStickyTouch`) whose
delivery can target enemies but is `EffectOnAlly = Helpful`,
`EffectOnEnemy = Helpful`, with an ally branch that applies the carrier and
a non-ally branch behind a Will save. The planner classified every
enemy-capable delivery `AnimatedFallback`
(`sticky-delivery-hostile-targeting-ambiguous`), and 0.3.0's default
`allowAnimatedFallback = true` cast it through the slow two-command animated
path; under WP4's strict Instant it would have been Not Ready. Ownership:
the Buff Planner (the Gunslinger shape is correct). No Gunslinger branch or
change.

Repair (`3c80ed0`): an enemy-capable delivery that is Helpful to allies and
not Harmful to enemies is `StickyTouchDeliveryRuleCast`
(`supported-willing-target-sticky-touch-delivery`); the instant adapter
requires the native touch auto-hit condition (caster, or non-enemy,
non-neutral, unconfused target) before each delivery
(`sticky-delivery-not-auto-hit:<reason>`). Structural only.
Native evidence: a new `sticky-touch-direct` qualification recipe is being
added so the guarded casting harness can cast it on an ally in Instant and
Animated on the advanced fixture.

## WP5 — beneficial-buff catalogue audit

Diagnosis (from the full-user catalogue export of `wp1-4480e15-core-01`):
Hideous Laughter entered through Call of the Wild's Infectious Charms caster
rider (a Caster-target effect is always a safe recipient); Treat Affliction
and Treat Deadly Wounds through a visible, component-free cooldown counted
as a payload (live discovery classifies a party member's ability as
reachable, so the static accessibility index does not protect it).

Repair (`d8614ef`): structural rules in the classifier (hostile ability
rider, save-gated effect, harmful condition / faction change, mechanics-free
buff, hidden bookkeeping marker, restoration tracker), one shared facts
builder for live discovery and the export, and an in-game audit
(`native-buff-catalog-audit.json`, catalog schema 5) with before/after per
profile computed in the same run. Details and the rule table:
`planning/NATIVE-BUFF-COVERAGE-MATRIX.md`. Rejected: a display-name or GUID
list (not needed: every reported entry has a structural cause); an
enemy-branch rule (only 3 extra removals, all also caught by the
harmful-condition rule, and it would change the conditional identity that
saved plans match on).

WP5 native catalogue evidence (package `aeb5c52b14d827856b495bcb96b75cadc55f775b701eb31484f371c44f8a5bbc`,
DLL `74f151fc4e71a83d3921da838177db72bc198390877a7d95f615b25e830c4491`, MVID
`a94a4493-df0e-4647-aaa7-9f45d08f6b99`, commit `d8614ef`; the discovery code is
unchanged after it). Static = the exact player graph; live = every ability
classified as reachable (how live discovery judges a party member's
abilities). Before = the 0.3.0 rules, computed in the same run.

| Profile | Run | Result | Static before -> after | Live before -> after |
|---|---|---|---|---|
| native-only | `kbp040-wp5-catalog-native-01` | PASS, restored | 379 -> 374 | 516 -> 505 |
| call-of-the-wild | `kbp040-wp5-catalog-call-of-the-wild-01` | FAIL by the profile's stale expected blueprints (see below); restored | 2355 -> 2268 | 3413 -> 3301 |
| full-user | `kbp040-wp5-catalog-full-user-01` | PASS, restored | 2418 -> 2323 | 3490 -> 3366 |
| human-reproduction | `kbp040-wp5-catalog-human-reproduction-01` | BLOCKED before deployment: `Compatibility fixture identity mismatch: BagOfTricks` (environment drift; the profile is the owner's) | - | - |
| advanced-gunslinger-0136 | - | not runnable: the advanced copy only admits its listed scenarios | - | - |

Removed by rule (full-user): hostile-ability-rider 38, mechanics-free-marker-only
39, reactive-restoration-marker-only 36, harmful-only 6, offensive-carrier-only 5.
Native-only removes 11: Dazing Touch, Eyebite, Inspiring Recovery, Light,
Targeted Bomb Admixture (static) and Daylight, Elemental Bastion, Stunning
Barrier stun, The Binding of the Prince, Treat Affliction, Unburrow (live).
Spot checks in the full-user audit: Hideous Laughter, Treat Affliction and
Treat Deadly Wounds removed; Mage Armor, Haste, Bless, Prayer and Freedom of
Movement kept. The full-user ownership split is not meaningful (the
ownership index attributes only Call of the Wild), so per-mod counts come
from the per-profile runs.

The call-of-the-wild run's own check required Regenerative Sinew:
Restoration (now deliberately excluded) and Globe of Invulnerability (already
excluded since 0.0.17, so the check was stale on the 0.3.0 baseline). The
profile now asserts three still-included representatives (Dazzling Blade,
Bless Weapon, Fortune; see `docs/CALL-OF-THE-WILD-COMPATIBILITY.md`); the
profile must be re-run on the candidate.

Uncertainty for the owner: Light, Daylight and Elemental Bastion leave the
catalogue because their buffs carry no mechanics of their own (cosmetic or
presence-keyed). The same structural rule removes about twenty activation
and selection markers (School Understanding, Cold Snap, Deadeye, Heaven's
Leap, Wild Flanking Partner) that must not be planned; Elemental Bastion is
the one entry whose exclusion may be a loss. An include override in
`NativeEffectOverrides.json` would restore it if wanted.

## WP7 — parchment and spell-scroll visual overhaul

References (details, provenance and licensing: `docs/VISUAL-THEME.md`):
the Teleport modal of KingmakerGunslinger (it is the native world-map
`GlobalMapMessageBox`; the mod draws only hairline dividers) and Roll for
Stats in KingmakerDiceRoller (it borrows the native `dialogue_backsheet`
sheet at runtime and draws it as a half-scale nine-sliced layer with a soft
shadow). Neither ships game art, and neither does the planner.

Implementation (`d85e6e5` `ff9e877` `17d7420` `91d1c6e`, merged): two new
capabilities in the existing native-theme resolver, each with an exact
contract checked before use and failing on its own:
`ScrollPaper` (`dialogue_backsheet`, 858x551, border 268/169/258/165, 200 PPU)
and `ScrollRule` (`blockscroll_bottom`, 147x11, border 20/0/20/0), both found
under the in-game `CharacterBuild` UI. The workspace frame and the spell
description draw the paper as a layer at scale 0.25 x spritePPU / canvas
reference PPU (0.5 at the expected 100), so the borders are drawn at
67/42.25/64.5/41.25 units and never stretched; the paper overhangs the frame
so its folded bands stay outside the header and footer. Any failure restores
the previous flat look exactly. The description is a spell scroll (title,
duration/meta line, rule, native read-only body with its own wheel); Escape
closes it first; a complete click outside closes it and is consumed. The
theme summary and `physical-workspace.json` (`workspacePaperEvidence`,
`inspectPaperEvidence`) record what was drawn. Tests: `wp7-*` (9).

Display: this session's display is 1920x1080, so the launcher refuses
`windowed-2560x1440`; `168ad8d` adds `windowed-1600x900` and
`windowed-1280x720` as the meaningfully different supported sizes. An
alternate in-game UI scale would change the owner's game settings and was not
practical.

## Integration

Integration branch `claude/kbp-040-integration` (worktree
`private\worktrees\KBP040-INT`): `fe06e26` + merge of the WP6 qualification
recipe branch `claude/kbp-040-wp6-sticky-recipe` (`ad7ca01` `2dde708`
`2ec8e63`) + merge of `claude/kbp-040-wp7-parchment`; conflicts were only
the two test-registration lines (union). Full unit suite 468/468;
Validate-Source 42/42.

Live WP7 findings (each fixed with a regression):

- `kbp040-wp7-sel-1080-01` (on `3cb3f90`, windowed 1920x1080, PASS): both
  donors resolved (`paper=native`, refPPU 100, borders 67/42.3/64.5/41.3,
  undistorted; ornament rules drawn), the description read as a spell
  scroll, but every lane well looked like an opaque reddish-brown slab:
  Unity's Outline effect fills a translucent wash with its colour. Fixed in
  `7383347` (a translucent wash drops its Outline).
- `kbp040-wp7-sel-1080-02` (on `7383347`, PASS): the paper shows through
  every lane; cards, portraits and inks legible. `kbp040-wp7-sel-720-01`
  (1280x720, PASS): every key control on screen and the description scroll
  intact, but the header rule struck through the "Routine:" row. Fixed in
  `d8b659e` (the rule sits in the gap above the routines, or is hidden).
  Known 720p limitation, not WP7: the catalogue's Spells/Abilities tab
  captions wrap at that width.
- The same runs are the in-game WP5 spot check on the standard party: the
  buff list shows Aid Another (AC / Attack) and Resistance only - Light,
  Treat Affliction and Treat Deadly Wounds, offered by 0.3.0, are gone.

WP3 defect found while building the physical gesture run (`14c7902`): with
a casting focused, a caster who can cast the buff in more than one way was
refused, but the graph draws source rows for the chosen caster only and the
refusal left the previous caster chosen, so that caster's rows never
appeared and the provider could not be changed to it. The ambiguous click
now shows that caster's rows (focus kept); the regression fails without it.

Physical evidence harness (`fe06e26`, `3cb3f90`): `live-workspace-physical`
gains `-PhysicalExpectation combat` (a moon press while the host holds a
party member in the game's combat state - `UnitEntityData.JoinCombat`, no
enemy, the group's leave timer kept at zero - must be refused for combat
before dispatch with nothing spent; then the ordinary press reaches the
lock) and `-PhysicalExpectation authoring` (caster, exact row, portrait add,
same-portrait remove, re-add, retarget, provider change when a second caster
is on screen, Undo, card focus, Escape leaves the focus, Escape closes).
The launcher's outcome checks and their fixtures cover both. The launcher
meta-test (`Test-RuntimeLauncherFileWhatIf.ps1`) passes all its layers with
the three lab-wide purity windows stubbed (`PASS=13`); the windows run in the
final complete gate.

Strict-Instant blocker, native: not runnable on the 0.4.0 candidate. After
WP6 no guarded fixture has an animated-only source (the advanced fixture's
only ones were the Magic Circle variants; the standard party has Resistance,
Aid Another and the Heal skill). The blocker, its WP2A deep link and the
strict hybrid refusal are proven by the real compiler, gate, navigation and
executor in `ExecutionPolicyTests`. Explicit Animated is exercised natively
by the `sticky-touch-direct` animated casting run.

