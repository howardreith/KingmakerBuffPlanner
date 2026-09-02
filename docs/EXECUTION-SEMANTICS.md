# Execution Semantics

## 0.0.18 sticky-touch transaction boundary

Provider capability and configured mode are now separate. Every `CastStep`
retains one structural capability: `DirectRuleCast`,
`StickyTouchDeliveryRuleCast`, `AnimatedFallback`, or
`NativeCommandRequired`. A beneficial sticky-touch carrier is eligible for the
second capability only when it has one non-null delivery blueprint with
`AbilityDeliverTouch`, Unit targeting, friendly/self targeting, and no hostile
or point ambiguity. Unsupported shapes request an explicit animated fallback;
they never silently become ordinary direct casts.

For instant sticky delivery, the runtime resolves the exact reserved source
`AbilityData`, constructs `AbilityData(source, TouchDeliveryAbility)`, and
copies the source's conversion, metamagic, spell-parameter, slot, override,
potion, and spell-source context. It submits exactly one `RuleCastSpell` for
that derived delivery data. The sole `Spend()` call belongs to the original
source data. Exact Kingmaker 2.1.7b IL proves why: derived data delegates
spellbook spending through `ConvertedFrom`, but `Spend()` reads material and
ability-resource components from the receiver's blueprint. Spending the
delivery could therefore omit carrier-only requirements; spending both would
double charge. Prepared resolution also requires the exact planned token's
`SpellSlot.Available` state immediately before submission.

The executor keeps enhancement activation leased through validation, rule
submission, effect confirmation, settlement inspection, and cleanup. It will
not advance while a matching held touch or delivery command remains. A failed
cleanup emits `ResidualStateUnsettled` and blocks later steps in the same
hybrid run. No delay, direct buff insertion, inferred pool debit, blueprint
mutation, or global command scheduler is used.

Animated sticky casts are cast-scoped two-stage operations. They retain the
carrier command identity, ignore the command that was already previous at
submission, identify the generated delivery `UnitUseAbility`, and require the
carrier, delivery, expected effect, and held-touch state to settle. Self casts
structurally omit the second command. Timeout, failure, cancellation, and
exception paths interrupt only owned unfinished commands, remove only the
matching held touch, and release enhancement state in `finally`.

Structured records independently expose configured mode, selected strategy
and reason, source/provider/caster/target IDs, exact reservation tokens,
carrier/delivery GUIDs, source/execution `AbilityData` identities, rule
submission, native rule flags, `Spend()` invocation, resource delta, effect
confirmation, native command stages, held state, cleanup, and failure.

Status: IN PROGRESS

Animated and instant modes execute the same immutable plan behind one executor contract. Both report each planned allocation, final validation result, native cast result, resource delta, effect delta, skip reason, and failure without silently replanning midway.

The animated executor uses Kingmaker’s native ability command creation and observes completion through a cast-scoped boundary. It does not install a global command scheduler or mutate blueprints.

The instant executor preserves the exact installed Kingmaker ordering established from `UnitUseAbility.OnAction()`:

1. validate current availability, target existence, `CanTarget`, material/component/item charges, spellbook availability, and ability resources;
2. trigger exactly one `RuleCastSpell` for the selected provider and target;
3. when the rule is not UMD-failed, call native `AbilityData.Spend()` exactly once;
4. observe native success/execution and effect/resource deltas;
5. continue or stop according to the routine’s explicit failure policy.

`RuleCastSpell` alone does not spend resources or validate targets. Direct `AddBuff`, inferred pool decrement, duplicate `Spend`, or global blueprint mutation are forbidden. Mass spells are represented as one provider allocation with multiple observed recipients, so their source cost remains one native cast.

Cancellation prevents unstarted allocations and cleans scoped handlers; it cannot roll back an already committed native cast. Batching is bounded to protect the Unity main thread.
