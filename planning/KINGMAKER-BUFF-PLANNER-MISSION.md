# CODEX AUTONOMOUS MISSION

## Build a Standalone Pathfinder: Kingmaker Buff Planner

You are the primary implementation agent for a new, independent Pathfinder: Kingmaker mod. Work continuously and autonomously until the full definition of done is met or a critical hard stop in this document is proven.

Do not ask the user ordinary implementation questions. Investigate the exact repository, installed assemblies, game behavior, and local references; make conservative engineering decisions; test them; record evidence; and continue.

This mission is self-contained. Copy it in full into a durable repository-local mission file before substantive implementation.

---

# 1. Mission identity and non-negotiable separation

Create and complete a **new standalone mod** with the provisional identity:

```text
Product name:       Kingmaker Buff Planner
Repository:         KingmakerBuffPlanner
Assembly:           KingmakerBuffPlanner.dll
UMM ID:             KingmakerBuffPlanner
Root namespace:     KingmakerBuffPlanner
Initial version:    0.0.1
Target release:     0.1.0 only after all core gates pass
Lab root:           C:\Dev\KingmakerBuffPlannerLab
Repository root:    C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
Reference source:   C:\Dev\KingmakerBuffPlannerLab\reference-source
Examples:           C:\Dev\KingmakerBuffPlannerLab\examples
Harness reference:  C:\Dev\KingmakerBuffPlannerLab\harness-reference
Runtime state:      C:\Dev\KingmakerBuffPlannerLab\runtime-state
Runtime staging:    C:\Dev\KingmakerBuffPlannerLab\runtime-staging
Runtime evidence:   C:\Dev\KingmakerBuffPlannerLab\runtime-evidence
Runtime backups:    C:\Dev\KingmakerBuffPlannerLab\runtime-backups
Policy root:        C:\Dev\KingmakerBuffPlannerLab\codex-policy
```

This repository and mod are completely separate from Tabletop Added Rules, Gunslinger, KingmakerGunslingerLab, and every laptop worktree.

You MUST NOT:

- modify, commit, reset, clean, rebase, push, or otherwise mutate the Tabletop Added Rules or Gunslinger repository;
- treat a copied Tabletop Added Rules source snapshot as a writable worktree;
- build this feature inside another mod assembly;
- reuse another mod's UMM ID, namespace, persistence file names, release package name, or blueprint ownership;
- create a compile-time dependency on Call of the Wild, Tabletop Added Rules, Gunslinger, or any other optional content mod;
- execute the Gunslinger push helper copied under `harness-reference`;
- deploy a Gunslinger build as part of this project.

Optional mods are read-only compatibility fixtures only.

---

# 2. User-authoritative product intent

The user wants the Kingmaker equivalent of BubbleBuffs:

- a robust in-game UI showing beneficial spells and abilities available to the current party;
- selection of which party members and pets receive each buff;
- grouping of configured buffs into routines;
- one-button execution of an entire routine;
- instant application for normal use, while consuming the correct real resources and invoking real game mechanics;
- configuration that remains usable across rests, party reordering, area transitions, save/reload, and ordinary character progression;
- complete native Kingmaker coverage;
- generic dynamic support for mod-added buffs wherever their mechanics can be understood structurally;
- specific runtime qualification against available local Call of the Wild and Tabletop Added Rules snapshots;
- Shield Other support through generic discovery when the copied Tabletop Added Rules snapshot contains it.

The user does not want a record-and-replay queue as the primary UX. The mod must reason from the party's current spellbooks, abilities, targets, resources, active effects, and saved preferences.

The user does not want to participate in normal development decisions. Continue independently until complete or critically blocked.

---

# 3. Exact technical target

Prove the actual desktop environment before relying on these expected values:

```text
Game:               Pathfinder: Kingmaker Enhanced Plus Edition
Game version:       2.1.7b
Steam app ID:       640820
Target framework:   .NET Framework 4.7
C# language level:  7.3
Unity:               game-installed Unity 2018-era assemblies
UMM:                 exact installed known-good 0.32.4 / 0.32.x baseline
Harmony:             1.2.0.1, Harmony12 namespace, 0Harmony12.dll
Shell:               native Windows PowerShell
```

Do not use:

- Wrath-only Owlcat MVVM namespaces;
- modern Harmony 2 APIs;
- .NET Standard or modern .NET runtime assumptions;
- C# language features unavailable in 7.3;
- APIs inferred from current Wrath without proving an exact Kingmaker equivalent;
- copied or bundled game DLLs.

Inspect exact local assemblies and bounded IL before choosing hooks or signatures.

---

# 4. Core definition: what counts as a buff

For this project, a **supported buff source** is a player-usable spell or ability that can produce at least one persistent beneficial effect on a controllable party unit, pet, qualifying area, or worn item.

Core included effect kinds:

```text
UnitBuff
PetBuff
AreaBuff
PrimaryWeaponEnchant
SecondaryWeaponEnchant
ArmorOrShieldEnchant, when safely resolvable
NestedBeneficialSpell
ConditionalBeneficialEffect
```

Core included source kinds:

```text
Prepared spellbook spell
Spontaneous spellbook spell
Cantrip/orison
Domain or special spell slot
Spell variant
Metamagic spell instance
Ordinary castable class/race/feature ability with a persistent beneficial result
Resource-bound beneficial ability
At-will beneficial ability
```

Core exclusions unless a later supported phase deliberately adds them:

```text
Direct healing with no persistent effect
Condition removal/restoration with no persistent effect
Summoning
Teleportation or movement
Dispel-only effects
Hostile-only effects
Damage-only effects
Purely cosmetic effects
Permanent always-on passive facts
Free combat toggles with no cast execution
Consumable inventory sources such as potions, scrolls, and wands
Equipment-granted abilities whose source/charge semantics cannot yet be proven
Point-target effects without a deterministic safe placement rule
Encounter/NPC-only blueprints unavailable to a player party
```

A dual-purpose ability may be included only when the planner can select a beneficial branch and valid friendly target without triggering a hostile or unintended branch.

Do not equate “friendly target flags” with “beneficial.” Do not equate a direct `ContextActionApplyBuff` with complete semantics. Do not build the catalog from names.

The native coverage report must list every audited candidate and its supported, excluded, or exceptional disposition with evidence.

---

# 5. Required discovery architecture

The primary catalog MUST be dynamically derived from live blueprints and party facts after all enabled mods have completed blueprint initialization.

Implement four layers.

## 5.1 Generic native action-graph discovery

Recursively inspect proven Kingmaker action/component structures, including exact equivalents of:

```text
AbilityEffectRunAction
ContextActionApplyBuff
Conditional
ContextActionCastSpell
ContextActionSpawnAreaEffect
AbilityAreaEffectBuff
ContextActionsOnPet
ContextActionPartyMembers
ContextActionEnchantWornItem
AbilityEffectStickyTouch
AbilityVariants
```

Inspect exact local types rather than assuming names or fields from Wrath.

The walker must have:

- cycle detection by object/reference identity and blueprint GUID;
- bounded depth with diagnostic output rather than stack overflow;
- cached reflection/member access;
- null safety;
- deterministic traversal order;
- action-path provenance in every effect descriptor;
- separate representations for sequential required effects and conditional alternatives;
- no flattening that loses branch meaning;
- graceful handling of unknown action types.

## 5.2 Generic ActionList-wrapper discovery

For unknown mod action wrappers, inspect fields/properties of exact `ActionList` type—or another proven Kingmaker action container—using bounded reflection. Recurse into those lists without compiling against the foreign assembly.

Do not perform unrestricted graph reflection over arbitrary objects. Restrict traversal to:

- known blueprint/action base classes;
- exact action-list member types;
- explicitly registered safe adapters.

Log unknown wrapper type, assembly identity, source blueprint GUID, source name, and path.

## 5.3 Optional assembly/type adapters

Create an optional adapter registry keyed by assembly name and exact type name. Adapters may use reflection after proving a local type contract. They must:

- load only when the optional assembly is present;
- never require a compile-time reference;
- fail closed and log a structured unsupported reason;
- record exact optional assembly version and SHA-256 in qualification evidence;
- remain isolated under `Compatibility/` or an equivalent module.

## 5.4 Explicit override registry

Create a versioned JSON override system with entries such as:

```json
{
  "schemaVersion": 1,
  "entries": [
    {
      "abilityGuid": "...",
      "disposition": "include",
      "sourceAssembly": "optional",
      "effectMode": "allOf",
      "effects": [
        { "kind": "UnitBuff", "guid": "..." }
      ],
      "reason": "Proven exceptional wrapper not structurally visible"
    }
  ]
}
```

Supported dispositions must include at least:

```text
include
exclude
replace-detected-effects
augment-detected-effects
unsupported-with-reason
```

Overrides are an exception layer. Do not hardcode the native catalog as the primary mechanism.

---

# 6. Required normalized domain model

Keep game scanning, planning, persistence, UI, and execution separated.

Names below describe architectural intent; adapt naming to a coherent C# 7.3 style.

```text
Core/
  AbilityKey
  ProviderKey
  BuffSourceDefinition
  EffectDescriptor
  EffectExpression
  TargetAssignment
  ResourcePool
  ResourceToken
  CastRequest
  CastPlan
  CastStep
  PlanDiagnostic
  ExecutionReport

Kingmaker/
  PartySnapshotBuilder
  SpellbookScanner
  AbilityScanner
  BeneficialEffectScanner
  TargetValidator
  ExistingEffectDetector
  KingmakerResourceReader
  KingmakerAbilityFactory

Planning/
  CastPlanner
  ProviderSelector
  ResourceAllocator
  MassCastResolver
  PlanValidator

Execution/
  ICastExecutor
  AnimatedCastExecutor
  InstantCastExecutor
  CastFinalizer

Persistence/
  ProfileRepository
  ProfileMigrator
  AtomicJsonWriter

UI/
  BuffPlannerWindow
  BuffListView
  BuffDetailsView
  TargetMatrixView
  CasterPreferenceView
  HudRoutineButtons
  ResultsView

Compatibility/
  AdapterRegistry
  OverrideRegistry
  CallOfTheWildAdapter, only where proven necessary
  TabletopAddedRulesAdapter, only where proven necessary

RuntimeTesting/
  request/result models
  scenario catalog
  runner
  evidence serializers
```

The planner must operate on immutable or effectively immutable snapshots and must not manipulate Unity objects during pure planning tests.

UI MonoBehaviours manage lifecycle, rendering, input, and coroutines only. They must not contain the resource-allocation algorithm.

Execution receives an already validated `CastPlan`. It must revalidate mutable game state at fire time.

---

# 7. Stable identities

Use stable keys, not character names or party indexes.

## 7.1 Unit identity

Use the exact persistent Kingmaker unit identifier proven to survive save/reload and party reorder. Expected candidate: `UnitEntityData.UniqueId` or exact Kingmaker equivalent.

## 7.2 Ability identity

A normalized ability key must be able to distinguish:

- base ability GUID;
- variant GUID;
- metamagic mask;
- special source identity where two mechanically distinct providers share a blueprint;
- source kind.

## 7.3 Provider identity

A provider key must be able to distinguish:

- caster unit ID;
- spellbook blueprint GUID;
- base ability GUID;
- variant GUID;
- metamagic mask;
- source kind;
- optional item/fact identity only when that source kind is implemented.

Do not serialize transient `AbilityData`, `SpellSlot`, `UnitEntityData`, Unity object, or runtime fact references.

---

# 8. Spellbook and resource model

Implement and prove exact accounting for:

## 8.1 Prepared spellbooks

- Treat usable prepared slots as discrete resource tokens.
- Preserve exact slot linkage/opposition semantics.
- Never count unavailable or spent slots as credits.
- Variants must spend the underlying prepared source correctly.
- Metamagic instances must retain the exact slotted version.

## 8.2 Spontaneous spellbooks

- Model shared remaining casts by spell level.
- Do not multiply one pool by the number of known spells.
- Treat variants and custom spell instances as consumers of the same proven pool where appropriate.
- Revalidate the pool immediately before execution.

## 8.3 Cantrips and orisons

- Model unlimited casts only when exact Kingmaker behavior proves they are unlimited.
- Do not use an arbitrary large integer as the permanent domain abstraction; use an explicit unlimited resource kind.

## 8.4 Domain/special slots

- Preserve slot eligibility.
- Do not allow an ordinary spell to spend a domain-only slot unless exact game rules allow it.

## 8.5 Opposition and linked slots

- Determine exact cost from the installed Kingmaker implementation.
- Spend all linked resources exactly once.
- Report insufficient linked resources before creating an executable task.

## 8.6 Resource-bound abilities

- Read the exact ability resource and calculated cost.
- Respect cost modifiers and remaining amount.
- Distinguish max amount from current amount.
- Spend through the game-supported path rather than editing raw counters unless exact IL proves that is the native path.

## 8.7 Material components

- Validate component availability before scheduling.
- Spend exactly once only after final target/resource validation.
- A canceled or invalid cast must not consume a component.

Every plan must expose:

```text
requested targets
fulfilled targets
unfulfilled targets and reasons
selected provider per cast
resource pool/token consumed
mass-cast grouping
expected effects
already-active skip decisions
```

---

# 9. Deterministic provider selection

Implement deterministic provider ordering with user overrides.

Required user controls:

- ban provider;
- explicit provider priority;
- per-provider maximum casts for a routine;
- reset to automatic;
- optional source preference once multiple source kinds are supported.

Default ordering must be documented and deterministic. A reasonable starting policy, subject to exact runtime evidence, is:

1. providers not banned and currently valid;
2. providers explicitly prioritized by the user;
3. providers that can legally reach all requested targets;
4. higher effective caster level / longer expected duration;
5. prepared duplicate slots before consuming a flexible spontaneous pool, unless user preference says otherwise;
6. stable provider-key tie-breaker.

Do not silently change provider ordering based on dictionary iteration or party reorder.

A self-only provider cannot satisfy another target unless an exact supported mechanic changes target legality. Optional-mod mechanics that share personal spells require a proven adapter; never fake them.

---

# 10. Mass, area, touch, pet, and item-enchantment semantics

## 10.1 Mass and burst spells

A mass/burst spell generally consumes one cast for all eligible configured targets covered by that cast.

The planner must:

- group targets into one cast when exact mechanics support it;
- not charge one slot per portrait;
- choose a legal cast anchor;
- prefer the caster as anchor only when `CanTarget` and effect geometry permit it;
- otherwise choose a deterministic legal configured unit;
- report configured targets outside the actual applicable set;
- never claim targets were buffed merely because the cast was scheduled.

## 10.2 Area effects

Represent the spawned beneficial area and its applied buff separately. Existing-effect checks must not mistake an unrelated active unit buff for proof that the area source is active when the distinction matters.

Point-target areas are unsupported until a deterministic safe placement rule is proven. Do not place them at arbitrary coordinates.

## 10.3 Sticky touch

Inspect exact Kingmaker sticky-touch behavior. The safe animated executor is the correctness oracle. The instant executor must either:

- correctly cast and deliver the touch effect through the game's supported path; or
- route that source through the safe executor and report the reason.

Do not merely apply the delivery buff directly.

## 10.4 Pets

Enumerate active controllable pets using exact Kingmaker APIs. Maintain stable master/pet identity. A pet-target action must resolve the correct pet type and must not crash when the expected pet is absent.

## 10.5 Worn-item enchantments

Normalize weapon/armor/shield enchantment effects separately from unit buffs. Presence detection must inspect the correct equipped item and slot. A missing or incompatible equipped item must produce an unfulfilled reason rather than an exception.

---

# 11. Existing-effect detection

Do not use only the source spell GUID. The applied fact is usually a different buff or enchantment blueprint.

Model effect expressions with sufficient semantics to represent:

```text
AllOf(required effects)
AnyOf(alternative effects)
Conditional(branch predicates and branch effects)
MarkerEffect
PetEffect
ItemSlotEffect
AreaEffect
```

The detector must distinguish:

- every required effect present;
- one of several mutually exclusive alternatives present;
- partial multi-effect presence;
- shared effects used by more than one source;
- conditional “apply X only when X is absent” patterns;
- effects deliberately ignored by the user for overwrite checks.

Required settings:

```text
Skip already-active effects (default)
Overwrite/recast active effects
Per-source ignored markers for exceptional shared-buff cases
```

Never report “skipped” without identifying the marker/effect that caused the decision.

---

# 12. Execution engines

Implement two engines behind one interface.

```csharp
public interface ICastExecutor
{
    IEnumerator Execute(CastPlan plan, ExecutionReport report);
}
```

Adapt syntax for C# 7.3 and existing style.

## 12.1 Animated safe executor

The safe executor must use normal Kingmaker ability commands and animations as closely as possible.

Requirements:

- use exact Kingmaker `AbilityData` construction;
- use normal `UnitUseAbility` or exact native command path;
- do not globally patch `UnitCommand.OnEnded` as the primary scheduler;
- do not interrupt unrelated player commands without explicit out-of-combat gating;
- observe only commands created by this mod through scoped identifiers/subscriptions;
- handle command failure, target invalidation, caster incapacitation, and sticky touch;
- generate exact execution results.

This engine is the reference behavior for proving resource spend and resulting effects.

## 12.2 Instant executor

The instant executor must invoke real Kingmaker casting mechanics. It MUST NOT implement buffs by calling `AddBuff` or directly adding enchantments as the normal path.

Investigate the exact Kingmaker `RuleCastSpell` path and the installed Call of the Wild reference behavior.

Required properties:

- final target legality checked immediately before any spend;
- final slot/resource/component availability checked immediately before any spend;
- canceled/invalid casts spend nothing;
- spell slot spent exactly once;
- ability resource spent exactly once;
- material component spent exactly once;
- opposition/linked slots spent correctly;
- effect duration, caster level, metamagic, save DC, source fact, and downstream event hooks match safe execution for supported sources;
- no duplicate game-driven secondary cast;
- no lingering EventBus subscription or temporary stat modification;
- no global blueprint mutation left behind;
- casts processed in bounded batches across frames;
- each actual fired cast recorded independently of the plan.

Start with a conservative batch size such as eight and a small delay, then tune only from runtime evidence.

## 12.3 Per-source fallback

The final mod may route a proven exceptional source to the animated engine while routine execution remains largely instant, but the UI and report must clearly identify the fallback.

Core completion still requires a proven instant path for ordinary native prepared and spontaneous buffs. A product that only plays an animated queue does not meet the mission.

## 12.4 Combat policy

Default to out-of-combat execution. Add an explicit opt-in only after combat behavior is proven. Do not add combat-start automation before the core release is complete.

---

# 13. Persistence

Store configuration externally rather than adding serialized mod facts to campaign saves.

Required persistence behavior:

- per-campaign profile keyed by exact game/campaign ID;
- versioned schema;
- stable unit/ability/provider keys;
- atomic write to temporary file followed by safe replacement;
- bounded backup of prior valid profile;
- graceful recovery from malformed JSON;
- migration tests for every schema change;
- preserve preferences for temporarily absent or benched characters;
- preserve configurations across party reorder and duplicate display names;
- never serialize runtime object references;
- no save dependency when the mod is uninstalled.

Suggested profile location:

```text
<ModPath>\UserSettings\kingmaker-buff-planner-<GameId>.json
```

or another exact external path proven writable and stable. Keep runtime automation requests/results outside user profiles.

The profile must cover at least:

```text
routine/group definitions
ability assignments
wanted targets
provider bans/priorities/caps
overwrite policy
hidden/excluded sources
ignored presence markers
UI settings and scale
execution mode/fallback policy
schema version
```

---

# 14. Required player-facing UI

The final release must have a real in-game UI, not only a UMM debug menu.

Use Kingmaker-native runtime UI structures or original project-owned assets. Do not reference Wrath MVVM classes and do not redistribute another mod's UI assets.

Required user experience:

## 14.1 Setup window

- open/close from a stable HUD button and optional configurable hotkey;
- standalone overlay attached to the correct Kingmaker canvas;
- survives area transitions and UI reconstruction without duplicate instances;
- scrollable buff list;
- search by localized/display name;
- filters for configured/unconfigured, short/long, hidden/unsupported, source category;
- deterministic sort by name or level;
- selected-source details panel;
- party and pet target portrait matrix;
- visual states for wanted, unavailable, already present, invalid target, and unfulfilled;
- provider/caster panel with priority, ban, cap, caster level, remaining casts, and rejection reason;
- routine/group assignment;
- reset/clear action with bounded confirmation behavior;
- tooltip using current game-localized ability text where available;
- no hardcoded English inside the underlying domain model.

## 14.2 Routine execution controls

Provide at least three routines:

```text
Long
Important
Short
```

Names may be user-editable later. The HUD must expose one-button execution for configured routines and a clear tooltip summary.

## 14.3 Preview and results

Before execution, show or expose:

```text
requested targets
planned casts
available resources
unfulfilled targets
already-active skips
animated fallbacks
unsupported sources
```

After execution, report:

```text
planned
actually fired
successfully observed
skipped
failed
unfulfilled
resources spent
```

Never claim success from plan creation alone.

## 14.4 Resolution and input

At minimum test:

```text
1920x1080
2560x1440
3840x2160 or a scaled equivalent
```

Test common UI scale settings. Avoid invisible raycast blockers, off-screen popouts, collapsed layout elements, and duplicate event subscriptions.

Initial spike UI may use a simple diagnostic panel, but final completion requires the full setup and routine UI.

---

# 15. Native catalog audit

“All native Kingmaker buffs” is a proof obligation.

After blueprint initialization, create a diagnostic catalog generator that inventories every native player-accessible spell/ability candidate from native spellbooks, class/race feature abilities, and variants.

For each candidate, record:

```text
source ability GUID
parent/variant GUID
internal name
display name
source assembly
spell lists and levels
native/mod-added ownership
caster/target flags
sticky-touch/mass/area classification
recognized action paths
detected effect expression
resource model
support disposition
manual override, if any
runtime scenario/evidence
known exclusions/reason
```

Produce durable files such as:

```text
planning/NATIVE-BUFF-CATALOG.json
planning/NATIVE-BUFF-COVERAGE.md
planning/NATIVE-BUFF-EXCEPTION-MATRIX.md
```

Do not hand-edit generated output without preserving the generator and provenance.

A native candidate is `PASS` only when:

- it is correctly discovered or deliberately excluded by the mission definition;
- target semantics are correct;
- resource planning is correct;
- already-active detection is correct enough for that effect shape;
- execution behavior is runtime-proven directly or covered by a justified representative equivalence class;
- unsupported exceptional behavior is not hidden.

The final report must give exact counts for:

```text
total audited candidates
supported automatically
supported by generic reflection wrapper
supported by explicit adapter
supported by override
excluded by definition
unsupported with reason
runtime-qualified direct cases
runtime-qualified equivalence classes
```

Do not claim 100% native coverage while any in-scope candidate is unclassified.

---

# 16. Optional-mod compatibility

No optional mod is a dependency.

## 16.1 Profiles

Create exact-hash compatibility profiles for every locally available fixture:

```text
native-only
call-of-the-wild
Tabletop-added-rules
a call-of-the-wild-plus-tabletop-added-rules combined profile
all-loadable-local, only when all exact local fixtures can safely coexist
```

Use stable profile IDs without spaces in actual manifests.

## 16.2 Call of the Wild

Most ordinary Call of the Wild spells should be discovered through standard Kingmaker actions. Audit unknown wrappers and add bounded adapters only when generic discovery cannot describe them.

Qualification claims apply only to the exact local Call of the Wild version/hash tested.

## 16.3 Tabletop Added Rules and Shield Other

Treat the copied Tabletop Added Rules package/source snapshot as read-only.

When Shield Other is present:

- prove the ability is discovered after all mods load;
- prove its paired caster/target effects are represented correctly;
- prove the target is legal;
- prove instant and safe execution produce the expected linked effects;
- prove resource and duration behavior;
- prove configuration survives save/reload.

When Shield Other is absent from the local snapshot:

- retain generic support architecture;
- record `FEATURE-NOT-PRESENT-IN-SNAPSHOT`;
- do not download or modify the active laptop project;
- do not block native release completion.

## 16.4 Compatibility meaning

A profile is not “compatible” merely because the game reached the main menu.

For each exact profile, prove as applicable:

- all expected UMM identities/versions loaded;
- exact buff-planner assembly/version/commit/MVID/hash loaded;
- no duplicate UMM IDs or assembly ambiguity;
- catalog scan completed without unhandled exceptions;
- expected representative optional buffs discovered;
- planning and execution scenarios passed;
- overlapping Harmony patch targets inventoried with owners/order;
- no protected save was written;
- live Mods and managed files restored exactly;
- claims limited to exact tested hashes.

---

# 17. Runtime automation harness

Adapt the laptop harness architecture into this repository; do not execute it from `harness-reference`.

Required scripts and docs, names may vary coherently:

```text
scripts/Invoke-KingmakerRuntimeTest.ps1
scripts/RuntimeAutomation.Common.ps1
scripts/RuntimeHarness.Common.ps1
scripts/Test-RuntimeRequest.ps1
scripts/Test-RuntimeResult.ps1
scripts/Build-Local.ps1
scripts/Validate-Package.ps1
scripts/compatibility/CompatibilityProfile.Common.ps1
scripts/compatibility/Enter-KingmakerCompatibilityProfile.ps1
scripts/compatibility/Restore-KingmakerCompatibilityProfile.ps1
scripts/compatibility/Test-KingmakerCompatibilityProfile.ps1
docs/WINDOWS-AUTONOMOUS-RUNTIME-TESTING.md
docs/WORKING-SAVE-SMOKE.md
```

## 17.1 Request/result protocol

Use versioned JSON request/result contracts under:

```text
C:\Dev\KingmakerBuffPlannerLab\runtime-state
```

A request must contain at least:

```text
schema version
run ID
scenario ID
expected mod version
expected commit/hash when available
profile ID
save policy/save name
launch/exit policy
timeout
requested evidence level
```

A result must contain at least:

```text
schema version
run ID
scenario ID
PASS/FAIL/BLOCKED
stage
assertion list with IDs and messages
loaded mod identity/version
commit/MVID/package/DLL hashes
exact game/UMM/Harmony identity
party/save identity where permitted
resource before/after values
effect before/after values
exception summaries
evidence paths
start/end timestamps
```

The in-game runner must reject stale, malformed, mismatched, or duplicate requests.

## 17.2 Transactional deployment

Never merge files into the live `Mods` directory without a transaction.

The harness must:

1. acquire an exclusive lock/sentinel;
2. record exact pre-state;
3. stage the new package in a separate directory;
4. verify package contents and hashes;
5. atomically preserve the original live mod state using a unique run ID;
6. stage only the exact profile;
7. launch through the established Steam App ID path;
8. wait for the matching result;
9. exit or kill only the process instance owned by the run when safely identifiable;
10. restore original state in a `finally` path;
11. verify restoration by names, counts, sizes, and hashes;
12. preserve all directories and stop on ambiguous restoration failure.

Do not clean unknown live mod files. Do not assume an empty Mods directory.

## 17.3 Save safety

The harness must recognize at least:

```text
NO-SAVE
KBP_AUTOMATION_BASELINE: protected and never writable
KBP_AUTOMATION_WORKING: only authorized working fixture
DISPOSABLE-CREATED-BY-RUN: only when exact lifecycle is implemented
```

Baseline is never loaded by a scenario that may save. Before and after every save-backed run, hash the working save and baseline. A scenario that claims no save mutation must prove byte identity.

No other save name is permitted without a future explicit user authorization.

## 17.4 Evidence

Each run gets an immutable directory:

```text
runtime-evidence/<runId>/
```

Include request/result, console output, relevant Player.log slice, package manifest, hashes, profile transaction record, restore verification, and screenshots only when useful and safely captured.

## 17.5 Steam behavior

The desktop is expected to be preconfigured for Offline Mode. Do not interact with Steam credentials or account prompts. A cloud/update/login conflict is a hard stop for runtime launching, not permission to click through.

---

# 18. Mandatory runtime scenario catalog

Implement representative deterministic scenarios before final qualification.

At minimum:

```text
mod-load-smoke
native-buff-catalog
party-provider-scan
prepared-resource-plan
spontaneous-resource-plan
domain-or-special-slot-plan
metamagic-variant-plan
opposition-linked-slot-plan
resource-ability-plan
existing-buff-skip
existing-buff-overwrite
partial-multi-effect-presence
mass-spell-single-cost
area-beneficial-effect
sticky-touch-buff
pet-buff
weapon-enchantment-buff
animated-execution
instant-execution
executor-equivalence
invalid-target-no-spend
insufficient-slot-no-spend
missing-component-no-spend
profile-persistence
party-reorder-persistence
save-reload-persistence
ui-window-smoke
ui-routine-buttons-smoke
call-of-the-wild-discovery
tabletop-shield-other-discovery
combined-profile-smoke
working-save-smoke
```

A scenario may be classified `NOT-APPLICABLE` or `UNAVAILABLE-LOCAL-REFERENCE` only with exact evidence.

Executor equivalence must compare, for representative prepared and spontaneous native buffs:

```text
before resources/components
ability parameters
actual cast event
applied fact/enchantment GUID
source/caster identity
duration/caster level/metamagic
post resources/components
failure behavior
```

Core scenarios must pass twice in consecutive fresh game processes on the same clean commit and package hashes.

---

# 19. Pure and integration-style tests

Match the user's testing preferences:

- test realistic behavior rather than private internals;
- prefer integration-style domain tests with real planner objects and serialized fixtures;
- avoid mocking broad modules;
- use fakes only at a narrow game-process boundary when exact runtime objects cannot exist outside Kingmaker;
- runtime scenarios are the authority for engine integration;
- no test may claim UI or game behavior from a detached unit assertion alone.

Required pure test areas:

```text
action graph traversal and cycle protection
effect-expression semantics
native/mod ownership classification
provider-key stability
prepared resource tokens
spontaneous shared pools
variants/metamagic resource sharing
mass grouping
provider priority/ban/cap
target assignment
existing-effect allOf/anyOf/conditional behavior
persistence round trip
schema migration
malformed profile recovery
request/result validation
transaction preflight and WhatIf safety
package manifest validation
```

Do not add a new test library merely for convenience. Inspect the established lab pattern and choose the smallest well-maintained option or a project-owned deterministic runner.

---

# 20. Repository and build requirements

## 20.1 Initial branch

From a clean, published `main`:

```text
codex/kingmaker-buff-planner
```

If the branch already exists, inspect it and resume only when ancestry and durable mission files prove it is the same mission. Never reset or overwrite an ambiguous branch.

## 20.2 Local path configuration

Use ignored `GamePath.props` or an equivalent local file for the desktop install path.

## 20.3 Reference behavior

All game/UMM/Harmony references must have Copy Local disabled. The build and release package must not contain installed game DLLs.

## 20.4 Package

Release ZIP root must contain exactly one loadable UMM mod folder or the exact established UMM package shape.

Include only project-owned or legally redistributable files:

```text
Info.json
KingmakerBuffPlanner.dll
project-owned runtime assets, when any
localization/config/override files
licenses and third-party notices
README/changelog as appropriate
```

Exclude:

```text
PDBs unless deliberately published
source reference trees
game DLLs
UMM/Harmony DLLs
Call of the Wild/Tabletop Added Rules payloads
saves
runtime evidence
machine paths
credentials
bin/obj/.vs
```

## 20.5 Versioning

Use coherent semantic pre-release increments during development. Do not mark `0.1.0` until final core gates pass. Every runtime result must record the exact version and commit.

---

# 21. Durable mission files

Before substantive production code, create and commit:

```text
planning/KINGMAKER-BUFF-PLANNER-MISSION.md
planning/ARCHITECTURE-AND-SOURCE-MATRIX.md
planning/NATIVE-BUFF-COVERAGE.md
planning/NATIVE-BUFF-EXCEPTION-MATRIX.md
planning/RUNTIME-SCENARIO-MATRIX.md
planning/COMPATIBILITY-PROFILE-MATRIX.md
KINGMAKER-BUFF-PLANNER-JOURNAL.md
KINGMAKER-BUFF-PLANNER-IMPLEMENTATION-REPORT.md
docs/ARCHITECTURE.md
docs/DYNAMIC-BUFF-DISCOVERY.md
docs/EXECUTION-SEMANTICS.md
docs/WINDOWS-AUTONOMOUS-RUNTIME-TESTING.md
docs/WORKING-SAVE-SMOKE.md
docs/MANUAL-ACCEPTANCE.md
docs/QUALIFICATION.md
AUTONOMOUS-RESUME.md
AUTONOMOUS-BLOCKERS.md
THIRD-PARTY-NOTICES.md
```

Copy this entire work order into `planning/KINGMAKER-BUFF-PLANNER-MISSION.md` verbatim or with only repository-local path normalization.

## 21.1 Journal rules

After every meaningful investigation, implementation checkpoint, runtime run, compatibility transaction, failed strategy, coherent commit, and publication action, append a concise entry containing:

```text
date/time
branch and exact HEAD
active version
work completed
commands/tests run
exact PASS/FAIL counts
runtime evidence IDs and paths
package/DLL hashes when relevant
rejected theories
current uncertainty
exact next action
```

The final line of every checkpoint must state the next concrete action.

## 21.2 Resume rules

Before context compaction, agent handoff, long runtime transition, or token exhaustion, update `AUTONOMOUS-RESUME.md` with:

```text
exact branch/HEAD/status
active version
last successful gate
current failure or hypothesis
exact files being changed
exact next command
runtime/profile state
unrestored external state, which should normally be none
```

Do not ask the user to restate the mission after compaction.

## 21.3 Status vocabulary

Use only explicit statuses:

```text
TODO
IN PROGRESS
PASS
FAIL
BLOCKED — CRITICAL
DEFER — EVIDENCED
UNAVAILABLE-LOCAL-REFERENCE
FEATURE-NOT-PRESENT-IN-SNAPSHOT
NOT APPLICABLE
```

A build is not runtime proof. A main-menu load is not full compatibility. A static blueprint inspection is not player-facing UI proof.

---

# 22. Mandatory intake before implementation

Perform and record all steps.

1. Confirm current working directory is the standalone repository.
2. Read the active global and repository `AGENTS.md` files.
3. Confirm clean working tree and no unresolved Git lock.
4. Confirm origin points only to the standalone repository.
5. Record branch, HEAD, status, remotes, and Git author.
6. Inventory the lab directories and read-only references.
7. Hash and record exact game, UMM, Harmony, and referenced DLLs.
8. Verify .NET Framework 4.7 targeting pack and C# 7.3 build path.
9. Inspect the laptop harness references but do not run them.
10. Inspect all source-reference commits/licenses.
11. Inventory exact optional-mod packages and hashes.
12. Verify no optional reference path is a linked worktree or writable active laptop share.
13. Verify Steam Cloud state and save policy from local evidence; do not change account settings autonomously.
14. Verify disposable save availability without loading a valued save.
15. Create the durable mission files and initial matrices.
16. Commit the intake before production implementation.
17. Create or update a guarded project-specific push helper under `codex-policy`.

Inspect at least these exact local source areas:

```text
BubbleBuffs:
  BufferState
  BubbleBuff/domain model
  beneficial-effect scanner
  save state
  executor
  instant/animated engines
  UI architecture

PathfinderAutoBuff:
  Kingmaker build conditionals
  PartySpellList / spellbook enumeration
  AbilityData construction
  target selection
  UnitUseAbility scheduling
  Kingmaker UI manager
  asset-bundle/UI lifecycle

Buff It 2 The Limit:
  architecture notes
  scanning gotchas
  casting gotchas
  UI gotchas
  persistence model
  mass/pet/item/source handling

KingmakerRebalance / Call of the Wild:
  exact RuleCastSpell usage
  spell/resource spending
  custom actions/wrappers
  Shield Other-like paired mechanics where present

Installed assemblies:
  exact types, methods, fields, constructors, and event interfaces needed
```

Do not start by copying the Wrath UI file and attempting to compile it.

---

# 23. Required implementation phases and gates

Proceed in order unless evidence supports a safer dependency-preserving reordering. Do not declare later phases complete while an earlier core gate is unresolved.

## Phase 0 — Intake and durable plan

Deliver:

- environment fingerprint;
- source/license matrix;
- initial architecture document;
- initial native candidate catalog strategy;
- branch and first coherent commit.

Gate:

```text
clean standalone identity
exact local technical target established
references/licenses inventoried
no external state altered
```

## Phase 1 — Minimal loadable standalone mod

Deliver:

- SDK/classic project style compatible with target;
- UMM `Info.json`;
- entry point and logging;
- versioning/build/package scripts;
- package validator;
- mod-load runtime request/result skeleton.

Gate:

```text
source validation PASS
clean build PASS
package validation PASS
mod-load-smoke PASS twice in fresh processes
live Mods restored
```

## Phase 2 — Blueprint/action forensics and catalog export

Deliver:

- generic action graph scanner;
- effect expression model;
- unknown wrapper diagnostics;
- native catalog exporter;
- pure tests and a runtime export scenario.

Gate:

```text
no unclassified scanner exceptions
catalog generated deterministically
same clean commit produces identical catalog from same profile
```

## Phase 3 — Party/provider/resource snapshot

Deliver:

- active party and pet snapshot;
- prepared/spontaneous/domain/cantrip/variant/metamagic/resource ability providers;
- stable provider keys;
- target validation snapshots.

Gate:

```text
party-provider-scan runtime PASS
prepared and spontaneous planning fixtures PASS
no double-counted shared pools
```

## Phase 4 — Planner and active-effect detector

Deliver:

- deterministic target-to-provider allocation;
- mass grouping;
- priority/ban/cap;
- allOf/anyOf/conditional presence detection;
- comprehensive diagnostics.

Gate:

```text
all pure planner tests PASS
prepared-resource-plan runtime PASS
spontaneous-resource-plan runtime PASS
mass-spell-single-cost planning proof PASS
existing-effect skip/overwrite runtime proof PASS
```

## Phase 5 — Persistence

Deliver:

- versioned per-campaign JSON;
- atomic writes/backups;
- migrations;
- party reorder/absence behavior.

Gate:

```text
round-trip/migration tests PASS
profile-persistence runtime PASS
party-reorder/save-reload proof PASS
no campaign save dependency introduced
```

## Phase 6 — Diagnostic setup UI

Deliver:

- standalone in-game window;
- list/search/filter;
- target matrix;
- provider details;
- routine assignment;
- preview.

Gate:

```text
UI opens/closes repeatedly without duplicates
scene transition rebuild works
representative resolutions usable
configuration changes persist
```

## Phase 7 — Animated safe executor

Deliver:

- normal command execution;
- scoped completion tracking;
- result reporting;
- failures and sticky touch handling.

Gate:

```text
animated-execution PASS
invalid target/no-spend PASS
resources/effects match ordinary manual cast for representatives
```

## Phase 8 — Instant executor

Deliver:

- real RuleCastSpell path;
- exact spend/finalization;
- batching;
- per-source fallback;
- equivalence evidence.

Gate:

```text
instant prepared buff PASS
instant spontaneous buff PASS
invalid target consumes nothing
insufficient resource consumes nothing
material component exact spend
mass single-cost runtime PASS
executor-equivalence PASS for representative classes
no leaked subscription/temp mutation
```

## Phase 9 — Complete native catalog and exceptions

Deliver:

- audited native coverage;
- overrides/adapters for in-scope exceptions;
- direct or equivalence-class runtime evidence;
- explicit exclusions.

Gate:

```text
every in-scope native candidate classified
no unsupported in-scope native candidate hidden
coverage counts reconcile exactly
```

## Phase 10 — Final player UX and HUD routines

Deliver:

- polished setup window;
- Long/Important/Short HUD buttons;
- pre/post execution results;
- tooltips and scale behavior;
- clear unsupported/fallback indicators.

Gate:

```text
UI and routine button smoke PASS twice
no click blockers or off-screen critical controls
no stale event subscriptions across reload/transition
```

## Phase 11 — Optional-mod compatibility

Deliver:

- exact profile manifests;
- generic discovery evidence;
- minimum necessary adapters;
- Call of the Wild and Tabletop Added Rules reports.

Gate:

```text
native-only PASS
available COTW profile PASS
available Tabletop profile PASS
combined profile PASS when locally possible
exact hashes and claims recorded
```

## Phase 12 — Hardening, release, and final qualification

Deliver:

- final source/build/package gates;
- clean-head deterministic builds;
- two consecutive fresh-process core runtime passes;
- package/DLL hashes;
- install/use documentation;
- manual acceptance document;
- release ZIP;
- coherent final commits and remote publication where authorized.

Gate:

```text
all Definition of Done rows PASS or explicitly allowed optional deferrals
clean working tree
local HEAD equals pushed remote branch when remote publication is available
release package validated from clean HEAD
```

Do not publish a public GitHub release, Nexus page, or other public artifact without explicit user authorization. A draft release or local release ZIP is acceptable.

---

# 24. Git and publication discipline

Commit after coherent checkpoints. Do not accumulate the entire project in one commit.

Use intentional messages such as:

```text
chore: establish standalone buff planner mission
build: add Kingmaker 2.1.7b mod scaffold
feat: discover beneficial effect graphs
feat: model party spell resources
feat: allocate deterministic buff plans
feat: persist campaign buff profiles
feat: add safe animated executor
feat: add resource-correct instant executor
feat: add buff planner UI
compat: qualify Call of the Wild discovery
compat: qualify Tabletop Added Rules discovery
test: add guarded runtime acceptance scenarios
docs: publish final qualification evidence
```

Never use destructive history operations.

The repository-specific guarded push helper must:

- verify exact repository root;
- verify permitted branch prefix;
- reject detached HEAD;
- reject unresolved merge/rebase;
- reject unexpected remote;
- reject secrets or prohibited payload paths;
- push only the current branch to the configured origin;
- print local and remote SHA.

Direct `git push` is prohibited by the supplied rules. Use the helper.

A GitHub authentication or remote outage does not justify discarding local work. Continue coherent local commits and record the publication blocker. Final remote equality remains unproven until restored.

---

# 25. Autonomy rules

Continue working without user input through:

- ordinary compile errors;
- failing tests;
- API signature investigation;
- reflection/IL inspection;
- UI layout iteration;
- runtime scenario failures with recoverable restoration;
- discovery false positives/negatives;
- exceptional native buffs;
- optional profile incompatibilities that do not threaten live state;
- context compaction;
- rate-limited subagents;
- absent optional reference features;
- lack of public product name/artwork.

Make conservative choices and document them.

Use subagents only for independent bounded tasks such as:

- read-only source comparison;
- native catalog audit subsets;
- test review;
- documentation consistency;
- Harmony overlap inventory.

Do not allow two agents to edit the same files or operate the runtime deployment transaction simultaneously. The primary agent owns integration, Git, versioning, external state, and final claims.

Do not wait passively for the user. When one path is blocked, continue independent phases that remain valid. Revisit the blocker later.

---

# 26. Critical hard stops

Stop only after preserving a durable checkpoint and exact evidence when one of these conditions is true.

## 26.1 Identity or repository ambiguity

- current directory is not the standalone repository;
- origin points at Tabletop Added Rules/Gunslinger or another unrelated remote;
- another process/agent is actively mutating the same worktree;
- an existing branch cannot be safely identified without reset/overwrite;
- source references are linked writable worktrees whose mutation risk cannot be removed.

## 26.2 External-state safety failure

- live `Mods` state cannot be restored exactly;
- a runtime transaction lock/sentinel is ambiguous;
- a protected save was modified or may have been modified;
- baseline save identity cannot be distinguished from working;
- an unexpected game/Steam process cannot be safely attributed;
- the harness would need to delete or overwrite unknown external files.

## 26.3 Steam/account boundary

- login, credential, Steam Guard, cloud conflict, purchase, update, or account prompt blocks launch;
- simultaneous account use produces an invalid-user-ticket condition;
- Offline Mode is not available and continuing would risk the laptop session or saves.

Do not automate credentials or click through these prompts.

## 26.4 Exact platform unavailable

- Kingmaker/UMM/Harmony identity is materially different and exact local assembly inspection cannot establish a coherent supported target;
- .NET Framework 4.7 reference assemblies or MSBuild cannot be installed/located by ordinary setup;
- required installed assemblies are corrupt or missing.

## 26.5 Licensing blocker

- a core implementation would require copying code/assets whose license does not permit it;
- required license/attribution cannot be established.

Choose an original implementation where possible before stopping.

## 26.6 Core semantic failure

After exhaustive exact-assembly investigation and documented safe experiments, ordinary native prepared/spontaneous buff casting cannot be made instant while preserving exact resource spend and game event semantics, and no truthful architecture can meet the one-button instant requirement.

Do not substitute direct `AddBuff` cheating and call the mission complete.

## 26.7 Irreducible product decision

A truly ambiguous player-facing behavior with major balance or save-compatibility consequences has no conservative default and cannot be deferred without invalidating the core product.

Ordinary names, layout details, sort order, colors, and optional features are not hard stops.

## 26.8 Tool/rate exhaustion

The active Codex environment cannot continue because of a hard service quota or unrecoverable tool failure. Before stopping, commit safe local work, update journal/resume/blockers, restore external state, and state the exact next command.

---

# 27. Definition of done

The mission is complete only when every core row is proven.

## 27.1 Standalone identity

```text
[ ] independent repository, assembly, UMM ID, namespace, persistence, package
[ ] no modification or dependency on Tabletop Added Rules/Gunslinger
[ ] no third-party/game DLL payloads
```

## 27.2 Native discovery

```text
[ ] full native candidate catalog generated
[ ] every in-scope native candidate classified
[ ] ordinary native buffs dynamically discovered
[ ] exceptions handled by bounded adapter/override
[ ] exact supported/excluded counts reconcile
```

## 27.3 Mod-added discovery

```text
[ ] runtime party scanning sees abilities introduced after other mods load
[ ] ordinary standard-action mod buffs work without per-spell code
[ ] unknown wrappers fail closed with useful diagnostics
[ ] exact available COTW/Tabletop profile evidence recorded
```

## 27.4 Planning and resources

```text
[ ] prepared slots exact
[ ] spontaneous pools exact
[ ] cantrips exact
[ ] domain/special slots exact
[ ] variants/metamagic exact
[ ] opposition/linked slots exact
[ ] resource abilities exact
[ ] material components exact
[ ] deterministic priority/ban/cap
[ ] mass spells consume one cast where appropriate
```

## 27.5 Effects and targets

```text
[ ] self/friendly/pet target legality
[ ] sticky touch supported or explicit safe fallback
[ ] area behavior safely bounded
[ ] weapon/item enchantment presence
[ ] allOf/anyOf/conditional active-effect detection
[ ] invalid targets consume nothing
```

## 27.6 Persistence

```text
[ ] versioned per-campaign external JSON
[ ] atomic write and backup
[ ] malformed recovery
[ ] migration tests
[ ] save/reload and party reorder proof
[ ] no save dependency after uninstall
```

## 27.7 UI

```text
[ ] complete setup window
[ ] target matrix
[ ] provider controls
[ ] search/filter/sort
[ ] routine assignment
[ ] preview diagnostics
[ ] Long/Important/Short HUD execution
[ ] results report
[ ] tested resolution/scale behavior
[ ] no duplicate lifecycle subscriptions
```

## 27.8 Execution

```text
[ ] animated safe reference executor
[ ] instant executor for ordinary native buffs
[ ] exact slot/resource/component spend
[ ] failure no-spend guarantees
[ ] representative executor equivalence
[ ] bounded batching
[ ] no leaked state or subscriptions
```

## 27.9 Automation and evidence

```text
[ ] request/result runtime runner
[ ] transactional mod/profile staging
[ ] exact restore verification
[ ] protected save policy
[ ] mandatory scenarios implemented
[ ] core suite passes twice in fresh processes
[ ] exact commit/MVID/package/DLL hashes in evidence
```

## 27.10 Release

```text
[ ] clean source validation
[ ] clean deterministic build
[ ] package validation
[ ] installation/use docs
[ ] architecture/discovery/execution docs
[ ] final qualification report
[ ] release ZIP from clean HEAD
[ ] coherent local Git history
[ ] remote branch equals local HEAD when remote publication is available
```

Optional rows may be `UNAVAILABLE-LOCAL-REFERENCE` without blocking native completion:

```text
[ ] Shield Other exact runtime proof, when present in copied snapshot
[ ] optional combined profile, when exact local mods safely coexist
[ ] consumable item sources
[ ] combat-start automation
[ ] reserve companions
[ ] controller-native navigation
```

Do not downgrade a core row to optional merely because it is difficult.

---

# 28. Final response contract

When complete or critically stopped, provide one concise but evidence-rich report containing:

```text
Status: COMPLETE or BLOCKED — CRITICAL
Repository and branch
Final local and remote SHA
Version
Game/UMM/Harmony identities
Architecture delivered
Native catalog counts
Optional profile results and hashes
Pure test totals
Runtime scenario totals and evidence IDs
Two-pass fresh-process qualification summary
Persistence/save safety result
Mods restoration result
Package path and SHA-256
DLL SHA-256 and MVID
Known limitations/allowed deferrals
Exact blocker and next command, only when blocked
```

Do not claim tests or compatibility that were not run. Do not describe a planned feature as delivered. Do not ask the user to infer evidence from logs; summarize the load-bearing proof directly.

Begin now with repository identity, active instructions, environment intake, and durable mission files.
