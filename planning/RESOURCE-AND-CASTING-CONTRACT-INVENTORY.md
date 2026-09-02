# Resource and Casting Contract Inventory

## 0.0.18 sticky-touch source, delivery, and spend ownership

Installed base-game contract: `Assembly-CSharp.dll` SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`,
MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

Freedom of Movement is the exact regression canary, not a production rule:

| Role | Installed identity and shape |
|---|---|
| carrier | `0087fc2d64b6095478bc7b8d7d512caf` / `FreedomOfMovementCast`; `AbilityEffectStickyTouch`; Touch range; source spellbook/slot ownership |
| delivery | `4c349361d720e844e846ad8c19959b1e` / `FreedomOfMovement`; `AbilityDeliverTouch` + `AbilityEffectRunAction`; self/friends true, enemies/point false |
| expected effect | `1533e782fca42b84ea370fc1dcbf4fc1`; delivery path `4c349361d720e844e846ad8c19959b1e/0:ActionList/0:ContextActionApplyBuff` |

`AbilityData(AbilityData, BlueprintAbility)` copies caster, fact, spellbook
blueprint, and metamagic context, but does not copy every public calculated
parameter/slot override. The adapter therefore explicitly retains
`ConvertedFrom`, `MetamagicData`, `OverrideDC`, `OverrideSpellLevel`,
`ParamSpellbook`, `ParamSpellLevel`, `ParamSpellSlot`, `PotionForOther`, and
`SpellSource`, then verifies caster/fact/spellbook/conversion identity and Unit
targeting.

`AbilityData.Spend()` first spends the receiver blueprint's material
component, then item charges, spellbook, and the receiver blueprint's ability
resource logic. `SpendFromSpellbook()` follows `ConvertedFrom`, and
`Spellbook.SpendInternal` spends the exact available prepared `AbilityData` or
one shared spontaneous level use. `SpellSlot.Spend()` also marks its linked
opposition slots unavailable. Therefore the proven transaction is:

1. rule-cast the derived delivery `AbilityData` once;
2. invoke `Spend()` exactly once on the original reserved source
   `AbilityData` when the rule is not UMD-failed;
3. never spend the delivery, both objects, or a planner-inferred pool.

`AbilityEffectStickyTouch.Apply` initializes `UnitPartTouch` and, for a
non-self target, queues a generated delivery `UnitUseAbility` at the front.
`UnitPartTouch.Init` removes an existing held touch before installing the new
one. The old animated operation watched only the carrier and a short effect
window, so advancing could cause the next carrier to replace an unresolved
prior held delivery. This is the confirmed structural cause of cross-iteration
interference; the exact live frequency near target three remains unobserved
because the protected save inventory is still baseline=0/working=0.

Rejected theories at this checkpoint: reservation-token order (the planner
already allocated distinct tokens; runtime now additionally rejects spent
slots), duplicate `RuleCastSpell` or double `Spend()` in the old Instant path
(Freedom never reached that path), material-component failure (the canary has
no material), variant selection, and an effect-confirmation-only timeout. The
proven fault was sticky-touch being classified as animated-only plus completion
being scoped to the carrier rather than the full native delivery lifecycle.

## 0.0.17 composable enhancements, Share, and passive Infusion

`CastEnhancementSnapshot` now records `ExclusiveGroupId`,
`NativeActivationGroupId`, `UsagePoolId`, `UsageUnitsPerCast`,
`AffectsTargeting`, and `RequiresNativeCommand`. Duplicate IDs fail closed.
Metamagic rods share one exclusivity group; all Powerful Change score toggles
share another; Share uses its own group. Share plus one Powerful Change choice
is valid and uses the same caster-scoped Arcane Reservoir pool.

Planning aggregates each selected set before acceptance:

| Selection / current reservoir | Forecast | Result |
|---|---:|---|
| Share / 1 | 1 | one cast accepted |
| Powerful Change / 1 | 1 | one cast accepted |
| Share + Powerful Change / 2 | 2 | one cast and one spell slot accepted |
| Share + Powerful Change / 1 | 2 | rejected before command |
| two Share casts / 1 | 2 total | only one cast accepted |
| two Share + Powerful casts / 3 | 4 total | only one cast accepted |

The planner subtracts requirements once from its local enhancement forecast,
never the live resource. Execution resolves all selected activatables again,
requires their shared live amount to agree, compares the summed requirement,
arms them, and only then performs native target/resource preflight. The native
Brown Fur transaction remains sole successful-cast debit authority: Share costs
one and Powerful Change adds one. Cancellation/rejection restores prior toggle
states and incurs no planner debit. Successful native one-shot consumption is
tracked per activation group so it is not rearmed.

Exact installed optional-provider inspection:
`KingmakerGunslinger.dll` version 0.0.113.0, SHA-256
`97a1ad535a7b384759272cf37c0fe8705843b9d149a61e9e8b6c41df39437913`,
MVID `685d2575-41e1-4897-881c-a314229ad7cf`; contract assertions
57/57. `KingmakerBuffPlanner.dll` retains no optional gameplay-mod assembly
reference.

Exact base-game inspection confirms Personal Alchemist extracts become Unit
targeted only through native `IsAlchemistSpell && AlchemistInfusion`.
Provider option construction inherits `CanTarget` for that passive case.
Infusion creates no enhancement, no profile selection, no extra pool, and no
debit beyond the one ordinary extract-slot reservation.

## 0.0.14 membership/availability and per-buff provider policy

Catalog ownership is established before resource availability. Direct actual
spell/fact/resource children remain owned, and parent-expanded children require
native source-context visibility. Current spontaneous/prepared slots, ability
resources, item charges/materials, and caster state remain provider
availability. Therefore an owned exhausted provider stays visible and keeps its
saved policy, while an ungranted child never becomes a provider.

The existing exact `ProviderKey` scope and `ProviderSelectionPolicy` remain
unchanged. Explicit UI operations edit `Banned`, normalized `Priority`, and
`MaximumCasts`; a provider can be both preferred and capped. `CastPlanner`
continues to own allocation and its shared resource ledger. It assigns the
first eligible cast to the preferred provider until its cap, then uses enabled
fallbacks; bans/caps are never bypassed. Exhausted policy produces exact
fulfilled/unfulfilled outcomes plus structured counts for providers, banned,
at-cap, and policy-eligible candidates. Preview uses the same allocator without
resource mutation.

Schema remains 4. Preferences are exact-provider data across campaign routines,
so Felix/Blur cannot cap Felix/Bulls Strength and distinct spellbooks/source
instances cannot alias. Stale keys are ignored rather than rebound. Pure
regressions cover Automatic equivalence, split 1/remainder allocation, all-ban,
insufficient caps, ability scope, normalized reorder, selected-source reset,
round trip, stale keys, and temporarily unavailable chooser rows. Actual native
debit/execution remains a guarded manual row because the save inventory is
baseline=0/working=0.

## 0.0.14 parent-backed concrete variant casting

Reflection-only IL inspection proves `AbilityData(AbilityData,
BlueprintAbility)` copies caster, fact/spellbook blueprint, and metamagic data,
then sets `m_ConvertedFrom` to the source data when a concrete child is supplied.
The child reports its own blueprint name/icon/description, while `SpellLevel`
and `GetAvailableForCastCount()` delegate to `ConvertedFrom`.
`SpendFromSpellbook()` also delegates and returns before the child can perform a
second spend. Thus the supported model is one concrete child cast with one
parent availability/resource context, not independent parent and child costs.

Prepared child providers retain the parent slot token IDs. Spontaneous children
share the parent caster/spellbook/level pool. Fact/resource children share the
source fact/resource context. Animated resolution and Instant rule submission
both require an exact parent GUID, exact child GUID, metamagic mask, and source
instance match; no resolver falls back to `Variants[0]` or a sibling.

0.0.13 Powerful Change checkpoint: the six score activatables all validate one
exact CotW Arcane Reservoir reference with
`ActivatableAbilityResourceLogic.ResourceSpendType.Never`. Toggle activation is
free; Buff Planner plans their common current amount under one caster-specific
usage-pool ID and never debits it. The independent provider's transaction is the
sole debit authority and spends one point only after a successful eligible
native-command commit. Because it arms on `UnitUseAbility` construction, a
selected Powerful Change step is mandatory animated/native execution even when
ordinary Instant-mode fallback is disabled. Failed or canceled casts restore
state; successful consumption does not rearm any one-shot score member.
Deterministic planning/routing/cleanup passes; actual debit and resulting
modifier remain in-game acceptance items.

0.0.11 performance-repair checkpoint: production planning and Animated/Instant execution contracts are unchanged, and deterministic behavior remains green within the 78/78 suite. Fresh save-backed resource/effect execution is not claimed because the current machine has no exact `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair. The guard rejected the campaign scenario before deployment/save access.

0.0.8 UI replacement checkpoint: automatic providers, resource accounting, material checks, confirmed-effect semantics, and both execution engines are frozen. Separate physical Animated and Instant runs remain release gates.

Status: IN PROGRESS

These contracts were established from the exact installed Kingmaker assembly, not from Wrath assumptions.

| Contract | Exact behavior relevant to the planner | Status |
|---|---|---|
| `AbilityData.Spend()` | material component, item charges, spellbook, then ability-resource logic | PASS |
| `AbilityData.SpendFromSpellbook()` | follows `ConvertedFrom`, then calls `Spellbook.Spend(this, false)` | PASS |
| `Spellbook.GetAvailableForCastSpellCount()` | current spontaneous slots or available matching prepared slots; opposition cost reflected | PASS |
| `SpellSlot.Spend()` | marks slot and linked slots unavailable | PASS |
| `Spellbook.SpendInternal()` | decrements one spontaneous level pool or spends exact prepared slot | PASS |
| `Spellbook.CalcSlotsCost()` | opposition costs two, otherwise one | PASS |
| `AbilityResourceLogic.CalculateCost()` | custom calculator or base amount plus increasing facts | PASS |
| `UnitAbilityResourceCollection` | current amount, availability and native spend | PASS |
| `AbilityData.RequireMaterialComponent` then `HasEnoughMaterialComponent` | native callers short-circuit inventory sufficiency unless a consumable component is actually required; evaluating sufficiency alone is not a valid cast gate | PASS — corrected generic adapter and callback short-circuit test |
| `RuleCastSpell.OnTrigger()` | executes cast/event semantics but does not spend or call `CanTarget` | PASS |
| `UnitUseAbility.OnAction()` | final availability/target/charge validation, trigger rule, spend once when not UMD-failed | PASS |
| `UnitUseAbility.CreateCastCommand()` | native command selection including touch/magus cases | PASS |
| `UnitEntityData.UniqueId` / `Player.GameId` | stable unit and exact campaign identity used in snapshots/profiles | PASS |
| `Player.Party` / `UnitDescriptor.Pet` | active party plus master-linked controllable pet intake | PASS — adapter compiled; save-backed runtime pending |
| `Spellbook.GetAllMemorizedSpells()` / `SpellSlot` | discrete primary tokens, availability, domain type, opposition and linked slots | PASS — pure allocation fixtures |
| `Spellbook.GetSpontaneousSlots(level)` | one shared remaining pool per caster/spellbook/level | PASS — no-double-count fixture |
| `BlueprintSpellbook.CantripsType` / spell level 0 | explicit unlimited pool, never arbitrary large credits | PASS — pure fixture |
| `Player.Inventory.Count(material.Item)` | exact component inventory count reserved once per planned cast | PASS — pure fixture; runtime spend pending |
| `UnitDescriptor.Buffs` / `Buff.SourceAreaEffectId` | distinguish direct unit buffs from area-applied buffs | PASS — adapter compiled; runtime pending |
| `UnitBody.CurrentEquipmentSlots` / item enchantments | worn-item enchantment presence remains distinct from unit buffs | PASS — adapter compiled; runtime pending |

Instant execution must mirror the native ordering: final validation, one `RuleCastSpell`, then one native `AbilityData.Spend()` when the rule is not UMD-failed. It must never directly decrement inferred pools or directly apply buffs.

Live 0.0.5 evidence exposed an invalid unconditional `HasEnoughMaterialComponent` check for native Bless. The planner snapshot already creates a material reservation only when `RequireMaterialComponent` is true and the blueprint has a positive-count item. Execution now uses the same native short-circuit contract. Final live evidence records the exact Bless `RequireMaterialComponent`, item, count, standalone `HasEnoughMaterialComponent`, and derived consumable-required values before cast.

Pure planning status at commit `5cbc9bb86fe9fcb79c16f8297c5a8754f34eda05`: deterministic provider order, bans/priorities/caps, prepared-before-flexible tie-break, shared pools, linked slots, domain eligibility, mass single-cost grouping, material reservation, target outcomes, and typed AllOf/conditional-AnyOf presence behavior are covered. Runtime rows remain deferred rather than inferred from these fixtures.
