# Execution Semantics

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
