# Brown-Fur Powerful Change repair

Status: implementation and deterministic diagnostic packaging complete;
real-campaign verification pending

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

## Deterministic diagnostic artifact checkpoint - 2026-08-27

- Branch: `codex/brown-fur-powerful-change-fix`
- Exact release-source HEAD:
  `f086c0257c8c8636cd5af0df9ca37c4f5ac7f794`
- Version: `0.0.13`
- Command: `./scripts/Build-Release.ps1`
  - source validation PASS `34`, FAIL `0` on both builds;
  - product build PASS `1`, FAIL `0` on both builds;
  - package validation PASS `4`, FAIL `0` on both local builds and the copied
    release artifact;
  - deterministic byte equality PASS `2`, FAIL `0`;
  - release builder PASS `3`, FAIL `0`.
- Command: `./scripts/Test-SourceOnly.ps1`
  - source validation PASS `34`, FAIL `0`;
  - protocol/domain tests PASS `95`, FAIL `0`;
  - runtime-harness filesystem tests PASS `8`, FAIL `0`;
  - package validation PASS `4`, FAIL `0`;
  - deployment WhatIf purity PASS `5`, FAIL `0`;
  - source-only wrapper PASS `1`, FAIL `0`.
- Command: `./scripts/validate-package.ps1 -PackagePath
  ./artifacts/release/0.0.13/KingmakerBuffPlanner-0.0.13.zip`: PASS `4`,
  FAIL `0`.
- Package:
  `artifacts/release/0.0.13/KingmakerBuffPlanner-0.0.13.zip`
- Package SHA-256:
  `9182e45cc5e31c137062ac9d2252a80836effc7bf8506676f303ed5276a7aa63`
- DLL SHA-256:
  `6e88ea23d54fb1e3ab7e7dc264129592ea36739c96fe6bb49f9d75890b216551`
- Assembly MVID: `3a61d90c-74b2-4944-b68d-6e2229fd3eb4`
- Package allowlist: `Info.json`, `KingmakerBuffPlanner.dll`,
  `NativeEffectOverrides.json`, and `THIRD-PARTY-NOTICES.md`, all under the
  one correct mod directory with deterministic timestamps.
- Reflection-only assembly audit: version `0.0.13.0`, zero optional gameplay-mod
  references; only the established Kingmaker/Unity/UMM/Harmony/Newtonsoft/system
  references are present.
- Publication status: local-only. The artifact was not installed, pushed,
  tagged, or published. No file in another mod was changed.

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

## Install acceptance and publication authorization - 2026-08-27

- Pre-record branch/HEAD/version:
  `codex/brown-fur-powerful-change-fix` /
  `07370364b3ff4e5fc3cc2b2842b24cd079fdec63` / `0.0.13`.
- Guarded installer dry-run passed; live transaction
  `brown-fur-powerful-change-0.0.13-install-20260827` upgraded UMM from 0.0.12
  to the exact candidate, preserved settings, verified other mods unchanged,
  and removed staging. Install-result SHA-256 is
  `e546995c3c6fe0f63ef16e7ba729894794f6d545c9fb96c83cb34bb6f0dc957a`.
- The owner accepted the candidate and authorized final commit, merge to the
  default `main` branch, remote `main` push, and a new public release.
- This authorization accepts publication with the explicitly documented runtime
  caveat; it does not relabel the unexecuted cross-mod numerical checklist as a
  PASS. The release-facing record edit passes `Test-SourceOnly.ps1`: source
  34/34, protocol/domain 95/95, runtime filesystem 8/8, package 4/4, deployment
  WhatIf 5/5, aggregate 1/1. Exact next action is the deterministic release gate,
  guarded integration/publication, and remote artifact verification.

## Published completion - 2026-08-27

- Release/default-branch/tag commit:
  `3c329cfff3530fe8397012565c238a81d55cec1d`; annotated `v0.0.13` tag object:
  `1ac6387f9d969053a4ca2a608021e106bae3b9ee`.
- Guarded-push WhatIf passes 6/6. Publisher repeats source 34/34,
  protocol/domain 95/95, runtime filesystem 8/8, package 4/4, deployment WhatIf
  5/5, aggregate 1/1, and two deterministic builds.
- Published ZIP/DLL/MVID:
  `67768176032d6d980f09b708a636dfa8f07e5b052530deb327d833e8e4882d96` /
  `b41f31da57f9b7ee69a4e693792bf4bb1a6f7e5ea7dbff0e723c72f24d02bf86` /
  `995ed895-bb45-412c-b626-692816b1f833`. Independent download matches GitHub
  digest/checksum and validates 4/4.
- Public release:
  `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.13`.
- Guarded install `brown-fur-powerful-change-0.0.13-published-install-20260827`
  installs those exact bytes, preserves settings, verifies other mods unchanged,
  and leaves no staging or running process. Install-result SHA-256:
  `49e81844fd1d111b09e4c69389a5764a9692c95b7210a2b81df34268c41afa2c`.
- No release-engineering work remains. The manual cross-mod numerical procedure
  is retained as an explicitly unclaimed diagnostic follow-up.

Exact next action: use the artifact above with the bounded procedure in
`docs/MANUAL-ACCEPTANCE.md` and record the real reservoir/modifier/toggle
outcomes. Do not claim runtime qualification until that evidence exists.
