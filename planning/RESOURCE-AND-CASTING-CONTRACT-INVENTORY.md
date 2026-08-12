# Resource and Casting Contract Inventory

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
