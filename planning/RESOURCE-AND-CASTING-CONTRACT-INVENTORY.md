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
| `AbilityData.HasEnoughMaterialComponent` | validates inventory item/count | PASS |
| `RuleCastSpell.OnTrigger()` | executes cast/event semantics but does not spend or call `CanTarget` | PASS |
| `UnitUseAbility.OnAction()` | final availability/target/charge validation, trigger rule, spend once when not UMD-failed | PASS |
| `UnitUseAbility.CreateCastCommand()` | native command selection including touch/magus cases | PASS |

Instant execution must mirror the native ordering: final validation, one `RuleCastSpell`, then one native `AbilityData.Spend()` when the rule is not UMD-failed. It must never directly decrement inferred pools or directly apply buffs.
