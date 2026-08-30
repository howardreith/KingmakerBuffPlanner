# Buff Catalog and Caster Controls Evidence

## Final clean-head handoff qualification

The implementation plus durable-record/mirror HEAD qualified for handoff is
`ce7099b089440e40716cbbd39c4e377c4fbe21c2` on
`codex/buff-catalog-caster-controls`, version 0.0.14. The final record commit
that follows this checkpoint is documentation-only.

- Contract inspection: 1/1, exact assembly SHA-256/MVID and TargetType values
  unchanged.
- Source-only: source 38/38, protocol/domain 119/119, runtime filesystem 8/8,
  package 4/4, deployment WhatIf 5/5, aggregate 1/1.
- Exact `.\scripts\Build.ps1 -Configuration Release`: source 38/38,
  build 1/1.
- Clean-head `.\scripts\Build-Release.ps1`: two identical builds (2/2),
  release builder 3/3, each source 38/38, build 1/1, local package 1/1, package
  validation 4/4.
- Final qualified local-only ZIP SHA-256:
  `239eb9de3657de030c88dabfffeaa3fab344ec01e8d561e6649281d7a9cf0571`.
- Final qualified DLL SHA-256:
  `6fe6d6837f5155b5ad1b1cdd1e64d47974cadbab44a824efa42dc0edea48b4d6`;
  MVID `80732098-e9da-45b2-b402-7b4ca6f52752`.
- `git diff --check`: pass; worktree clean. The two authoritative mission
  files are byte-identical at SHA-256
  `abd96d3bea9f4d0bc7e9b4996e4a83d6dfa852acb989035b32b16404f254106d`.
- Guarded runtime remains blocked at exactly
  `Disposable save ambiguity: baseline=0; working=0.`

## Checkpoint identity

- Mission branch: `codex/buff-catalog-caster-controls`.
- Intake HEAD: `011a57ff0565b5954745a8b0e742726a74b4315f`.
- Implementation checkpoint HEAD:
  `ec718fa96ae1cbbb1feeb5b3acd1900e867b699a`.
- Intake version: 0.0.14. The worktree was clean and contained no pre-existing
  uncommitted changes.
- Exact installed Kingmaker 2.1.7b `Assembly-CSharp.dll`: SHA-256
  `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`;
  MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.
- Historical structured probe input:
  `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\hud-lifecycle-0.0.12-cotw-1\native-buff-catalog.json`;
  SHA-256
  `a4e5d2206225dc5ad9dcf54e9b7a46bb0d8f5dcb58d1d617dd3534e1872356b9`.
  This is diagnostic input, not a claim that the current build ran in game.

## Exact root causes

### Floating quick result

`BuffPlannerUiRoot.PresentQuickResult` deliberately sent every
`QuickExecutionResult` to three sinks: the full-screen planner, UMM logging,
and `BuffPlannerHudButtonController.Present`. The HUD controller owned a
`Feedback` Text object, a feedback root, and an eight-second timeout. The
floating lower-left result was therefore an explicit second presentation, not
a Kingmaker common/combat-log side effect.

The HUD feedback object and lifecycle are removed. Quick results retain their
full message and counts, continue through the existing callback and
`Routine UI result` UMM line, and still reach the planner footer only when the
full-screen view exists. A repository search finds no `MessageLogThread`,
`AddMessage`, `CombatLog`, or `EventLog` route in production source.

### Declared children were treated as owned

The prior adapter marked every non-null child in
`BlueprintAbility.Variants` eligible once its parent `AbilityData` appeared
in an owned collection. That array proves declaration only. It does not prove
that the current caster has the feat/fact/condition required to select a child.
Execution used the same unconditional expansion and could therefore resurrect
a stale child from the parent declaration.

Exact IL inspection establishes:

- `AbilityData.HasVariant` and `BlueprintAbility.HasVariant` test blueprint
  membership only.
- `AbilityVariants.Validate` enforces only that a variant container has at
  least two children.
- Native action-bar conversion constructs
  `AbilityData(parentData, childBlueprint)` for declared children and retains
  only child data whose `AbilityData.IsVisible()` succeeds.
- `IsVisible()` evaluates the child's exact `IAbilityVisibilityProvider`
  components in the actual caster/fact/spellbook context.
- `IsAvailableForCast` checks transient caster state, material availability,
  active facts, item charges, and other current conditions.
- `GetAvailableForCastCount` reads current spontaneous/prepared resources.

The production ownership adapter now follows the native action-bar gate:
directly enumerated concrete sources are owned; a parent-expanded child is
owned only when actual child data constructed from that source passes
`IsVisible()`. Failures close the child with UMM-only reasons
`variant-not-granted`, `variant-native-validation-failed`, or
`variant-contract-unavailable`. Discovery and execution both call the same
expansion. Resource exhaustion remains a separate provider-availability
condition and cannot remove an owned child from the catalog.

### Non-buffs were rescued by generic recipients or marker buffs

The prior action adapter collapsed every `AbilityTargetsAround.TargetType`
into one generic `AreaRecipients` value. The classifier considered that value
safe and allowed any non-harmful persistent leaf to establish support. Damage
actions were merely unknown diagnostics, while hidden carrier/activation/save
markers looked like harmless persistent buffs. Offensive abilities could
therefore be rescued by an implementation marker.

Exact installed contracts establish `TargetType.Enemy=0`,
`TargetType.Ally=1`, and `TargetType.Any=2`.
`AbilityTargetsAround.Select` applies the corresponding enemy/ally filter.
`AbilityAreaEffectBuff.Condition` is likewise only classified as allied or
enemy when one exact, non-negated `ContextConditionIsAlly` or
`ContextConditionIsEnemy` proves it; other filters are ambiguous.
`ContextActionDealDamage`, `AbilityDeliverProjectile`, and
`AbilityDeliverAttackWithWeapon` are proven offensive action/delivery
contracts.

The domain now preserves allied, enemy-only, and ambiguous area recipients.
Only allied areas can infer mass grouping or indirect coverage. Classification
requires a persistent non-harmful payload on a safe self/friend/party/pet
branch, records action paths, and removes payloads on the same offensive
conditional branch. A hidden class-feature, activation, cleanup, or save
marker cannot qualify an offensive carrier. A substantive hidden self-buff
without hostile-carrier semantics remains eligible.

## Diagnostic probes

The historical structured catalog explains the reports without becoming an
allowlist:

| Probe | Structural evidence | Generic correction |
|---|---|---|
| Effortless Aid | Declared child `007b3510...` has `AbilityShowIfCasterHasFact` and a real visible AC buff. The old party expansion ignored the visibility fact. | Actual parent/child data must pass native visibility for the current caster. |
| Flying Kick | Declared child `1ae752ad...` has `AbilityShowIfCasterHasFact` and real Extra Attack/Flying Kick buffs. Beneficial effects do not prove the current monk learned the option. | The same current-caster native visibility gate excludes an unlearned child. |
| Channel Positive Energy - Damage Undead | Representative child `0013d37d...` has repeated `ContextActionDealDamage` diagnostics plus hidden `HolyVindicatorBloodBuff` leaves containing only remove-on-save/context-action machinery. | Damage is an offensive action with branch provenance; hidden marker-only leaves cannot rescue it. |
| Quick/Swift damage variants | The same damage-plus-hidden-marker structure appears on quick/swift declared children, including projectile-delivered forms. | Eligibility and structural payload rules apply per concrete child, not by display name. |
| Alchemist's Fire family | The evidence contains an attack-roll-trigger carrier buff; offensive delivery fixtures demonstrate why carrier persistence is not a planner buff. | Delivery/action semantics and persistent payload semantics are evaluated together. |

No production decision parses these names or hard-codes their GUIDs. No entry
was added to `NativeEffectOverrides.json`.

## Caster-policy behavior

The existing exact `ProviderKey` remains the persistence scope:
caster + spellbook + ability/variant/metamagic + source instance. The UI did
not replace the allocator and did not add a caster dropdown. A dropdown cannot
express a split such as Felix max 1 followed by Akasa.

The selected-buff panel now opens a full-screen-blocked `CASTER POLICY`
chooser. Each exact provider row shows portrait, caster, source identity, spell
level, current casts or At will, transient unavailability, enabled state,
order, and `MAX/RUN`. Controls explicitly set Use/Do not use, move earlier or
later, cycle Unlimited/1/2/etc., and reset only the selected buff's providers
to Automatic. Temporarily depleted owned providers remain in the rows.

Every change writes through the existing profile save callback, rebuilds the
preview, refreshes the selected panel, and does not reserve or spend a runtime
resource. Reordering stores normalized unique priorities. Ban, priority, and
maximum can coexist. Unconfigured providers retain the allocator's stable
automatic fallback. The preview summarizes actual casts by provider when
targets exist and configured policy otherwise. Disabled or exhausted caps
produce unfulfilled outcomes and
`[KBP-PLAN-DIAGNOSTIC]` UMM records; the allocator never bypasses policy.

The existing profile fields were sufficient. Schema version remains 4.
Nullable priority/maximum values are explicitly accepted, positive values are
validated, and stale exact provider keys remain ignored data rather than
binding to a different provider.

## Changed implementation and test files

- Contract/source gates:
  `scripts/Inspect-KingmakerVariantContracts.ps1`,
  `scripts/Validate-Source.ps1`.
- Discovery/domain:
  `Discovery/ActionGraphScanner.cs`, `Discovery/DiscoveryNode.cs`,
  `Discovery/EffectOverrideRegistry.cs`,
  `Discovery/NativeCandidateClassifier.cs`,
  `Discovery/NativeCatalogExporter.cs`, and
  `Domain/Effects/EffectExpression.cs`.
- Game adapters:
  `GameAdapters/KingmakerAbilityVariants.cs`,
  `GameAdapters/KingmakerActionGraphAdapter.cs`,
  `GameAdapters/KingmakerBuffSourceDiscovery.cs`,
  `GameAdapters/KingmakerPartySnapshotBuilder.cs`, and
  `GameAdapters/KingmakerProviderOptionBuilder.cs`.
- Planning/persistence:
  `Planning/CastPlanner.cs`, `Planning/RoutinePlanService.cs`,
  `Persistence/ProfileModels.cs`, and
  `Persistence/ProfileRepository.cs`.
- UI/composition:
  `UI/BuffPlannerHudButtonController.cs`,
  `UI/BuffPlannerScreenView.cs`, `UI/BuffPlannerUiRoot.cs`,
  `UI/PlannerPresentationModels.cs`, `UI/PlannerSetupModel.cs`,
  `UI/PlannerUiSession.cs`, and `UI/PlannerViews.cs`.
- Regression runner: `tests/KingmakerBuffPlanner.Tests/Program.cs`.

## Automated validation at the implementation checkpoint

- `.\scripts\Inspect-KingmakerVariantContracts.ps1`: exit 0; exact assembly
  hash/MVID and contracts above.
- `.\scripts\Test-SourceOnly.ps1`: source 38/38, protocol/domain 119/119,
  runtime-harness filesystem 8/8, package fixture 4/4, deployment WhatIf 5/5,
  aggregate 1/1.
- `.\scripts\Build.ps1 -Configuration Release`: source 38/38 and build 1/1.
- `.\scripts\Build-Release.ps1`: two deterministic local builds, each
  source 38/38, build 1/1, local package 1/1, package validation 4/4; release
  builder 3/3 and deterministic equality 2/2.
- `git diff --check`: pass.
- Local-only ZIP:
  `artifacts/release/0.0.14/KingmakerBuffPlanner-0.0.14.zip`;
  SHA-256
  `665077eb9fbb24077c73bdd726ad114dfa8930ea2cc754bfd860ee101f3a1eab`.
- Release DLL SHA-256:
  `ac826993249878188133e90eb12c3171b3aedd01129503d01555576f8d909613`;
  MVID `799ce2f9-6928-4180-88de-860c06bcbc12`.

The guarded save inventory still fails closed with exactly
`Disposable save ambiguity: baseline=0; working=0.` No game launch, live Mods
staging, or save access occurred. These results do not claim in-game
qualification. The exact next action is the bounded checklist at the top of
`docs/MANUAL-ACCEPTANCE.md` after an authorized save pair exists.

## Rejected approaches

- English-name or GUID blacklists for the probes.
- Treating blueprint declaration, `HasVariant`, or the first sibling as
  ownership.
- Using `IsAvailableForCast` or remaining resources as catalog membership.
- Flattening conditional branches or treating every area as allied.
- Letting hidden/internal markers rescue offensive delivery.
- UI-specific allocation, a single fixed-caster dropdown, policy bypass, or
  stale-key remapping.
- Redirecting quick results to any Kingmaker log UI.
