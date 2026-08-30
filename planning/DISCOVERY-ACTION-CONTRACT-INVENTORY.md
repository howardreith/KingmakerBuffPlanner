# Discovery Action Contract Inventory

## 0.0.14 current-caster and recipient correction

Exact inspection extends the earlier variant record:

- native action-bar conversion enumerates declared children, constructs
  `AbilityData(parent, child)`, and filters with `IsVisible()`;
- `IsVisible()` invokes child visibility providers in actual source context,
  while `IsAvailableForCast` and `GetAvailableForCastCount` include transient
  cast/resource state;
- `TargetType` values are Enemy=0, Ally=1, Any=2, and
  `AbilityTargetsAround.Select` enforces those dispositions;
- `AbilityAreaEffectBuff.Condition` exposes the area recipient filter;
  exact non-negated ally/enemy conditions are recognized, all other forms fail
  closed as ambiguous; and
- `ContextActionDealDamage`, `AbilityDeliverProjectile`, and
  `AbilityDeliverAttackWithWeapon` are offensive action/delivery evidence.

The scanner now carries every diagnostic action path, so an offensive action
removes a purported payload only on its compatible conditional branch.
Delivery components remain ability-level carrier evidence. Effect facts also
carry harmful, hidden, class-feature, component-type, source-contract, target,
and branch data. New dispositions include enemy-only-area,
ambiguous-area-recipient, no-persistent-beneficial-party-effect,
offensive-carrier-only, harmful-only, hidden-marker-only,
valid-beneficial-self-effect, and valid-beneficial-party-effect.

The inspection command exits 0 against exact assembly SHA-256/MVID
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb` /
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Structured conclusions and probe
evidence are in `planning/BUFF-CATALOG-CASTER-CONTROLS-EVIDENCE.md`.

## 0.0.14 selectable variant correction

Exact Kingmaker 2.1.7b inspection establishes two different contracts that must
not be conflated: `BlueprintAbility.Variants` returns the declared
`AbilityVariants.Variants` blueprint array, while `AbilityData.Variants` is
literally `ldnull; ret` and `AbilityData.InitVariants()` is `ret`. Discovery now
enumerates declared child blueprints in their native array order and creates
concrete runtime data explicitly. The parent variant container is not traversed
as one flattened union of mutually exclusive child effects.

Each child independently enters `KingmakerActionGraphAdapter` and the existing
beneficial persistent-effect classifier. This prevents unrelated summoning or
choice menus from entering merely because they use `AbilityVariants`, while
allowing independently eligible energy, alignment, form, size, and analogous
buff children. `AbilityTargetsAround` wraps the child's expression as
`AreaRecipients`; it does not erase its concrete child identity.

The inspection helper is
`scripts/Inspect-KingmakerVariantContracts.ps1`. Exact assembly evidence is
SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`
and MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

## 0.0.13 Powerful Change consumer

`KingmakerPowerfulChangeBlueprintAnalyzer` reuses
`KingmakerActionGraphAdapter` and `ActionGraphScanner` rather than introducing a
second spell-name catalog. Resulting `Buff`/`AreaBuff` leaves are resolved to
their native blueprints and inspected for the optional provider's proven
positive ability-score carrier families. Conditional structure remains in the
catalog expression; the enhancement classifier only determines whether the
provider supports the selected score somewhere in the cast's proven resulting
buff graph. Unknown/malformed carriers block enhancement qualification rather
than becoming guessed support.

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
