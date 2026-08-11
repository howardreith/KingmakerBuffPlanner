# Discovery Action Contract Inventory

Status: IN PROGRESS

Exact target: installed Kingmaker 2.1.7b `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`.

| Contract | Exact observed members | Planned interpretation | Status |
|---|---|---|---|
| `AbilityEffectRunAction` | `Actions: ActionList` | root action list | PASS |
| `ContextActionApplyBuff` | `Buff`, duration/permanent flags, `ToCaster`, `AsChild` | unit/caster buff leaf | PASS |
| `Conditional` | `ConditionsChecker`, `IfTrue`, `IfFalse` | preserve alternatives as expression branches | PASS |
| `ContextActionCastSpell` | `Spell`, DC/level overrides | recursive spell edge with cycle guard | PASS |
| `ContextActionSpawnAreaEffect` | `AreaEffect`, duration, `OnUnit` | inspect area components | PASS |
| `AbilityAreaEffectBuff` | `Condition`, `Buff` | conditional area-buff leaf | PASS |
| `ContextActionsOnPet` | `Actions` | pet-target branch using descriptor pet | PASS |
| `ContextActionPartyMembers` | `Action` | party-target branch | PASS |
| `ContextActionEnchantWornItem` | `Enchantment`, slot, duration/permanent, caster flags | worn-item enchant leaf | PASS |
| `AbilityEffectStickyTouch` | `TouchDeliveryAbility` | normalize to delivery ability while retaining source | PASS |
| `AbilityVariants` | `Variants: BlueprintAbility[]` | variant family with shared provider/resource identity | PASS |
| `ActionList` | `Actions: GameAction[]` | ordered structural traversal | PASS |
| `ConditionsChecker` | operation and condition array | retain conjunction/disjunction metadata | PASS |
| Unknown ActionList wrapper | cached, deterministic, bounded exact-`ActionList` fields/properties; getter failures become diagnostics | recurse safely while preserving exact type/assembly/path | PASS |

Runtime evidence: native-only export commit `07dc2380abbac74228eed88ce73113aeeabe61db`, two byte-identical catalogs (SHA-256 `bcacbe69bc71c85c5299b8fe8254c18baa33d66e9e3ecf53f6d4aa6b37094878`), 0 scanner exceptions. Unknown non-wrapper actions remain explicit audit inputs rather than being silently treated as supported effects.
