# Kingmaker Buff Planner 0.0.13

Version 0.0.13 repairs Powerful Change discovery and execution for the optional
Brown-Fur Transmuter implementation used with Call of the Wild's Arcanist.
The owner accepted the validated, guarded-installed candidate on 2026-08-27 and
authorized this public release.

## Root cause and repair

Buff Planner's enhancement pipeline implemented only native metamagic rods.
Although the domain enum reserved a class-feature category, class features were
always rejected, no Powerful Change fact or score toggle was discovered, and
execution could arm only a rod. The UI therefore correctly rendered its empty
candidate list as `Enhancement: None available`.

The repair detects the exact live Powerful Change feature, validates the six
native score activatables against their marker buffs and Arcane Reservoir
component shape, and represents each score as a caster-owned enhancement. The
six choices share one usage pool because the provider spends the same reservoir.

Spell eligibility is structural. A candidate must be a genuine Transmutation
spell from the exact Arcanist casting spellbook, and its resulting buff graph
must contain a supported positive ability-score carrier such as `AddStatBonus`,
`AddContextStatBonus`, `AddGenericStatBonus`, `AddStatBonusAbilityValue`, or
`Polymorph`. Bull's Strength is covered by its native Strength +4 Enhancement
carrier; there is no Bull's Strength name or spell-GUID exception.

## Execution behavior

Selecting Powerful Change arms the provider's real native score toggle. Even
when Buff Planner is configured for Instant mode, this one enhancement uses the
native animated command path because the optional provider starts its immutable
transaction when `UnitUseAbility` is constructed. The provider remains the sole
authority for changing the original modifier, preserving its descriptor,
spending exactly one reservoir point on a successful eligible cast, and
consuming the selection.

Canceled or rejected casts restore the prior activatable state. A successful
one-shot transaction leaves the entire score group consumed, preventing a prior
score selection from being resurrected and affecting a later cast.

## Compatibility and diagnostics

The adapter has no compile-time dependency on another gameplay mod. If the
optional Powerful Change blueprints are absent, the caster lacks the feature,
or the toggle/marker/resource contract differs, it contributes no option and
Buff Planner continues normally. Ordinary casters and spells from other
spellbooks never inherit the capability.

Opening the selected buff's casting section emits a focused
`[KBP][Enhancement]` line with caster, ability and resulting buff identities,
spellbook, school, descriptors, feature detection, component/carrier evidence,
matched ability scores, qualification status, exact rejection reason, and the
available enhancement list.

## Validation status

The deterministic suite covers all six direct ability scores, polymorph bonus
carriers, the expected Bull's Strength capability cases, wrong caster and
spellbook rejection, unrelated spells, selected mass variants, shared reservoir
reservation, mandatory native-command routing, and one-shot cleanup.

Direct real-campaign evidence has not yet been captured for the cross-mod
Harmony transaction, one-point reservoir debit, final +6 Bull's Strength
modifier (+8 with Transmutation Supremacy), ordinary unselected +4 result, and
repeated-cast non-stacking behavior. The release keeps focused diagnostics for
that verification boundary; see `docs/MANUAL-ACCEPTANCE.md` for the short
procedure. These numerical runtime results are not claimed by the automated
qualification.
