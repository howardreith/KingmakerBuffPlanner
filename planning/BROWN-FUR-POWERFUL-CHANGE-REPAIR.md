# Brown-Fur Powerful Change repair

Status: implementation and deterministic regression slice complete; release
qualification pending

## Intake checkpoint — 2026-08-27

- Branch: `codex/brown-fur-powerful-change-fix`
- Starting HEAD: `1a568c8dd402c3783b120117f29d3a34e921a334`
- Starting version: `0.0.12`
- Starting worktree: clean
- Remote mutation/publication: prohibited; none performed
- Unrelated mod mutation: prohibited; none performed

The installed optional-mod environment was inspected read-only. No files outside
this repository were changed.

## Existing Buff Planner architecture

The current end-to-end enhancement path is:

1. `PlannerUiSession.Refresh` builds the live party/provider snapshot and invokes
   `KingmakerCastEnhancementAdapter.Discover`.
2. `KingmakerCastEnhancementAdapter.Entries` enumerates caster-owned native
   `ActivatableAbility` instances, but recognizes only an activatable whose buff
   has `MetamagicRodMechanics` and whose `RodAbility` is the same blueprint.
3. `CastEnhancementSnapshot` reserves a `ClassFeature` category, but
   `IsApplicable` explicitly returns false for every category other than
   `MetamagicRod`.
4. `PlannerSetupModel.GetApplicableEnhancements` filters snapshots by remaining
   uses and `source.Providers.Any(snapshot.IsApplicable)`.
5. `PlannerSetupModel.GetEnhancementSummary` renders the exact string
   `Enhancement: None available` when that filtered list is empty. The view only
   renders that model string; it performs no additional Brown-Fur filtering.
6. Persistence stores the selected stable enhancement ID on the source
   assignment. `CastPlanner` revalidates applicability and reserves finite uses
   before adding the enhancement ID to each `CastStep`.
7. Both executors call `ICastEnhancementRuntimeAdapter.PrepareEnhancements`.
   `KingmakerCastEnhancementAdapter.Prepare` currently resolves and toggles only
   metamagic rods, then restores their prior state through a lease.
8. Animated execution creates a native `UnitUseAbility` command. Instant
   execution directly triggers `RuleCastSpell` and then spends the ability.

No Powerful Change snapshot is currently discovered, applicable, persisted,
prepared, or executed. The UI is reporting the empty model accurately; it is
not hiding an internally available option.

## Actual installed Brown-Fur contract

The installed `CallOfTheWild` `1.14.4c-2.1` assembly does not itself contain a
Brown-Fur archetype. The installed `KingmakerGunslinger` `0.0.104` optional
module publishes Brown-Fur onto CotW's Arcanist when its independent module is
enabled. This provider remains outside Buff Planner's product boundary and was
inspected read-only.

Evidence:

- Installed provider DLL SHA-256:
  `4003C284C116D8BF1E2019692D035BE563E87F1021B6C26C6470246905B916CC`
- Installed provider DLL MVID:
  `7a9cd325-48f9-4cbc-be10-a9fc898a6edd`
- Read-only provider source HEAD:
  `0fe38002fc022ad5a04d65430eb461046cd9cc3c`
- Current game log records `brown-fur-transmuter=True`, registration of all 25
  stable identities, successful publication, and a compatible CotW contract.
- CotW Arcanist casting spellbook:
  `0c21cfcab6ce4395bd4df330ab3cf715`
- CotW Arcane Reservoir:
  `3b775ee982444493b3de8f7bc31bd872`
- Powerful Change feature:
  `b3bbed7e12463e4c434cd81eda7ab2dd`

`BrownFurBlueprints` grants six native, mutually exclusive, immediately
activated score toggles. Each toggle owns a hidden marker buff and an
`ActivatableAbilityResourceLogic` tied to the CotW reservoir with
`ResourceSpendType.Never`. Activation is free. The provider's cast transaction
is the only debit authority.

`BrownFurCastIntentRuntime` accepts Powerful Change only when all of these hold:

- the caster owns the Powerful Change feature;
- the selected score toggle is on;
- the cast is a genuine `AbilityType.Spell` from a spellbook, not an item;
- the ability school is `SpellSchool.Transmutation`;
- the source spellbook is the exact CotW Arcanist casting spellbook;
- the structurally inventoried action/buff graph contains a supported positive
  bonus carrier for the selected ability score; and
- at least one reservoir point is available.

Supported carrier families are `AddStatBonus`, `AddContextStatBonus`,
`AddGenericStatBonus`, `AddStatBonusAbilityValue`, and `Polymorph` (with
`ChangeUnitSize` as an auxiliary carrier). The provider modifies the original
registered modifier and preserves its descriptor.

Bull's Strength is not a name/GUID exception. Its native ability
`4c3d08935262b6544ae97599b3a9556d` applies buff
`b175001b42b1a02479881b72fe132116`, whose supported carrier is
`AddStatBonus{Stat=Strength,Value=4,Descriptor=Enhancement}`. The same structural
contract covers the other ability-score buffs and qualifying polymorph/size
transmutations.

Powerful Change has one level-3 feature. Level 20's Transmutation Supremacy is
a separate fact that changes the increase from +2 to +4; it is not a second
Powerful Change rank or replacement feature. The six legacy score abilities
are hidden save-compatibility identities; the six activatables are the live
player-intent surface.

The provider arms its immutable cast transaction in the Harmony postfix on the
native `UnitUseAbility` constructor. Therefore Buff Planner's direct instant
`RuleCastSpell` path cannot carry Powerful Change. A selected Powerful Change
enhancement must use the animated/native-command engine. The provider then
reserves and spends exactly one reservoir point on successful rule commitment,
adjusts the matching modifier, and consumes the selected toggle. Canceled or
ineligible commands spend nothing.

## Proven root cause

The stale Buff Planner implementation was written when the installed optional
environment genuinely exposed no Brown-Fur provider. It left a generic
`ClassFeature` enum value but implemented only metamagic rods. The later
optional Brown-Fur publication did not add a Buff Planner compatibility
adapter. Consequently:

- caster capability detection never runs;
- spell qualification never runs;
- no Powerful Change snapshot reaches the domain model;
- `ClassFeature` would be rejected even if a snapshot were injected; and
- execution knows only how to arm metamagic rods.

That complete missing integration—not Bull's Strength blueprint shape, wrong
caster selection, a Powerful Change rank mismatch, persistence, deduplication,
or view filtering—is why the selector says `Enhancement: None available`.

## Implemented repair

The repair now:

1. add a fail-soft optional compatibility contract for the proven provider
   identities and exact CotW spellbook/resource boundary, with no compile-time
   gameplay-mod dependency;
2. classify eligible spells from spell school and supported positive
   ability-score carriers in the actual action/applied-buff graph;
3. add class-feature applicability, exact caster/spellbook/selected-variant
   constraints, and shared-reservoir reservation to the domain model;
4. arm the provider's real native score activatable and let its own transaction
   spend the reservoir and modify the resulting buff;
5. force selected class-feature enhancements through native animated commands;
6. emit selected caster/ability/school/descriptor/feature/qualification and
   rejection-reason diagnostics; and
7. add deterministic regression coverage for capability, eligibility,
   multiclass spellbook isolation, shared uses, UI visibility, and mandatory
   animated routing.

No spell-name special case and no synthesized stat bonus were introduced.

The concrete implementation is split across the required boundaries:

- `BrownFurPowerfulChangeCompatibility` owns optional blueprint/fact/toggle
  integration and fails closed when the provider contract is absent or its
  feature, marker, reservoir, or spend-type shape differs.
- `KingmakerPowerfulChangeBlueprintAnalyzer` uses the existing bounded
  Kingmaker action-graph adapter and inspects resulting native buff components.
- `PowerfulChangeEligibilityClassifier` is the deterministic domain policy for
  genuine-spell, school, exact-spellbook, applied-buff, carrier-family, and
  ability-score qualification.
- `CastEnhancementSnapshot` now models exact spellbook/ability applicability,
  shared usage pools, rejection reasons, and mandatory native-command needs for
  class features while preserving rod behavior.
- `CastPlanner` reserves all six score options against one caster-specific
  reservoir pool.
- `KingmakerCastEnhancementAdapter` prepares the exact native toggle. Its lease
  restores state after canceled/failed casts but does not resurrect any member
  of the one-shot group after the provider consumes a successful selection.
- `HybridCastExecutor` treats the native command as mandatory for this selected
  enhancement even when optional animated fallback is disabled.
- `PlannerUiSession` records option-contract data and emits the selected
  caster/ability/buffs/spellbook/school/descriptors/feature/carriers/scores,
  qualification result, exact rejection reason, and available options.

## Implementation validation checkpoint - 2026-08-27

Branch: `codex/brown-fur-powerful-change-fix`

Implementation/test commit:
`650605aaf2c1c1f7272893074b5e7ad7ed9a9224`. Product version is now
`0.0.13`; the exact clean release-source commit will be recorded after this
version/durable-record checkpoint is committed.

Commands and exact results:

- `./scripts/Build.ps1 -Configuration Release`: source validation PASS `34`,
  FAIL `0`; product build PASS `1`, FAIL `0`; DLL SHA-256
  `8a2d82319455462d064607f0a646d978ce8488cf510851450649d810afbee37d`.
- `./scripts/Test-SourceOnly.ps1`: source validation PASS `34`, FAIL `0`;
  protocol/domain tests PASS `95`, FAIL `0`; runtime-harness filesystem tests
  PASS `8`, FAIL `0`; package validation PASS `4`, FAIL `0`; deployment WhatIf
  purity PASS `5`, FAIL `0`; wrapper PASS `1`, FAIL `0`.

Regression additions prove all six direct ability-score categories, supported
polymorph carriers, Bull's Strength availability with the discovered class
feature, absence without that catalog capability, ordinary-caster and wrong
spellbook rejection, unrelated-spell rejection, selected mass-variant
identity, shared reservoir reservation across score options, mandatory native
command routing, and non-restoration of a consumed one-shot selection.

## Baseline validation

Command: `./scripts/Test-SourceOnly.ps1`

- Source validation: PASS `34`, FAIL `0`
- Protocol tests: PASS `91`, FAIL `0`
- Runtime-harness filesystem tests: PASS `8`, FAIL `0`
- Package validation: PASS `4`, FAIL `0`
- Deployment WhatIf purity: PASS `5`, FAIL `0`
- Source-only wrapper: PASS `1`, FAIL `0`
- Baseline package SHA-256:
  `1cbb2b215a78ab4dea2af5c99ebae211fe21e1f3532eb1865af3420a90ea8494`

## Remaining uncertainty

Static and deterministic evidence can prove discovery/classification/planning,
native-toggle preparation, cleanup policy, and command-path selection. Only an
in-game cast on a real Brown-Fur character can prove the cross-mod Harmony
transaction, exact reservoir debit, resulting +6/+8 modifier, non-stacking
recast behavior, and ordinary unselected +4 result for the diagnostic build.

Exact next action: create the clean release-source checkpoint, run deterministic
release build/package validation, and record the distributable/DLL/MVID hashes
without installing, pushing, or publishing it.
