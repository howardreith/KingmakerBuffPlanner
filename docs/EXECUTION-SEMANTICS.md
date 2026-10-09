# Execution Semantics

## 0.4.0 execution policy (current contract)

Two rules are enforced policy, no longer player settings. They supersede the
0.3.0 "Cast only out of combat" and "Instant mode: animate buffs that cannot
be instant" controls.

**Never during combat.** Every routine route (HUD buttons, the planner's Run,
Ready Casts Only, the Classic planner) refuses while `Player.IsInCombat` is
true with the single global message `Buff routines cannot run during combat.`
The refusal happens at the shared `CastingWorkspaceSession.Apply` boundary,
before compilation, persistence flush, authorization, projection, or native
submission; it names and focuses no casting and never opens the planner. A
new press after combat ends runs normally. The executors keep their
per-step combat refusal (always on) for a combat that starts mid-run.

**Strict Instant.** In Instant mode a casting runs only through a qualified
instant rule-cast route (`DirectRuleCast`, `StickyTouchDeliveryRuleCast`,
`ProviderDirectRuleCast`). The compiler marks a casting whose effective
strategy is `AnimatedFallback` or `NativeCommandRequired` Blocked with
`instant-route-unavailable:<strategy>:<reason>` while Instant is selected, so
the gate reports it as a casting-specific blocker and Not Ready navigation
(WP2A) focuses it; nothing is spent. An already-active skip casts nothing and
stays a skip. The hybrid executor runs in strict mode and refuses such a step
before any native work if one ever reaches it. The explicit **Animated** mode
(the mode button) runs every casting through the normal native command.

**Stored 0.3.0 preferences.** The `outOfCombatOnly` and
`allowAnimatedFallback` fields remain in the schema because every earlier
reader requires them. A loaded value is read but not honoured
(`LegacyExecutionPreferencesOverridden` records that it differed); loading
writes nothing, and the next deliberate save writes the enforced values
(`true`/`false`), rotating the previous file into the `.bak` chain. A rolled
back 0.3.0 reads those values and therefore the same strict behaviour.

## 0.4.0 willing-target touch buffs (WP6)

Magic Circle against Alignment (all four variants, and the paladin and
antipaladin families) is a sticky-touch carrier whose delivery
(`AbilityDeliverTouch`) may also be aimed at an enemy. 0.3.0 classified every
such delivery `AnimatedFallback` (`sticky-delivery-hostile-targeting-ambiguous`),
so in 0.3.0 Instant mode it was cast through the normal two-command animated
path (carrier, then delivery) - the slowness the owner observed - and in
0.4.0's strict Instant it would have been Not Ready.

The classifier now treats an enemy-capable delivery as a willing-target buff
when the delivery is `EffectOnAlly == Helpful` and **not**
`EffectOnEnemy == Harmful`; the reason is
`supported-willing-target-sticky-touch-delivery` and the strategy is
`StickyTouchDeliveryRuleCast`. A delivery harmful to enemies, unhelpful to
allies, or point-capable keeps the fallback. The decision is structural: no
spell name or GUID is consulted.

Kingmaker 2.1.7b `RuleAttackRoll.OnTrigger` auto-hits a touch attack when the
target is the caster, or is not the caster's enemy, not of a neutral faction
and not confused. Before every instant sticky delivery the instant adapter
checks exactly that (`KingmakerStickyTouchCastAdapter.AutoHitRefusal`) and
otherwise fails validation with `sticky-delivery-not-auto-hit:<reason>`
before anything is spent, so the ally branch of the delivery is the one that
runs and no attack is rolled. The rest of the sticky contract above is
unchanged: one `RuleCastSpell` for the derived delivery data, one `Spend()` on
the source data, effect confirmation through the expected buff, and cleanup
of the held touch.

Ownership: Magic Circle against Alignment is not a native Kingmaker 2.1.7b
spell; KingmakerGunslinger registers it (`KMG.Spells.MagicCircle.*`, installed
0.0.140, fixture 0.0.136). Its delivery is a faithful willing-target touch:
`CanTargetEnemies`, `EffectOnAlly = Helpful`, `EffectOnEnemy = Helpful`, and an
action graph whose `ContextConditionIsAlly` branch applies the carrier while
the other branch requires a failed Will save. Nothing in that shape is wrong;
the slowness came from the planner's over-conservative classification and
0.3.0's animated-fallback default, so the correction belongs to the Buff
Planner. No Gunslinger change is made or required, and the planner still has
no compile-time Gunslinger dependency.

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
