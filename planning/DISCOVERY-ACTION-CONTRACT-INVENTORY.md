# Discovery Action Contract Inventory

0.0.11 performance-repair regression: discovery code is unchanged. Native `perf-fix-0.0.11-exact-native-1` passes 12/12; Call of the Wild `perf-fix-0.0.11-exact-cotw-2` passes 26/26 with 9,064 abilities, 5,907 candidates, 2,096 optional inclusions, zero unsupported candidates, and exact restoration. The performance root cause is UI host discovery, not blueprint/action-graph discovery.

0.0.8 UI replacement checkpoint: no discovery action interpretation changed. The full deterministic and exact native/Call of the Wild regressions remain release gates.

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
| `ContextActionSelectByValue` | private `m_Variants[]`, each wrapper has exact `Action: ActionList` | preserve runtime-selected alternatives as conditional/AnyOf branches | PASS |
| `ContextActionRandomize` | private `m_Actions[]`, each wrapper has exact `Action: ActionList` | preserve randomized alternatives as conditional/AnyOf branches | PASS |
| `MagicFang` | `Enchantment[]`, duration, greater/level contracts | emit exact worn-item enchantment leaves alongside the duration buff | PASS |
| `ContextActionSpawnMonster` | `AfterSpawn: ActionList`, summon blueprint/pool/duration | classify as summoning; never reinterpret after-spawn creature buffs as planner effects | PASS |
| `ContextActionWeaponEnchantPool` | default enchantments, duration, pool/group | retain exact signal buff and structured dynamic-pool diagnostic; native execution applies selected pool | PASS structurally; runtime deferred |
| `BlueprintBuff` | harmful/visibility/class-feature/rest/death flags and exact components | record polarity/component evidence without assuming friendly flags imply benefit | PASS |
| player source roots | `BlueprintRoot.Progression` classes/races/feats, spellbooks, progressions, archetypes, selections, fact/spell grants | establish player-accessible candidate inventory | PASS |

Initial runtime evidence was the Phase 2 catalog at commit `07dc2380...`. The completed schema-4 audit is from commit `fba6e24`, guarded runs `20260811T2126328544602Z-native-buff-catalog` and `20260811T2127318144905Z-native-buff-catalog`, byte-identical SHA-256 `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa`. Unknown non-wrapper actions remain explicit row diagnostics rather than being silently treated as supported effects.
