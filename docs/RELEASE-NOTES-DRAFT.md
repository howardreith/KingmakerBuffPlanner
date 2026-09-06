# Kingmaker Buff Planner 0.0.19

This candidate fixes Share Transmutation ignoring Instant mode. Resinous Skin
is the reported reproduction spell, but neither its name nor its GUID, Felix,
or any party position appears in production routing.

## Provider-owned direct transaction

When the installed Brown-Fur provider exposes the exact version-1 direct-cast
contract, Share Transmutation and eligible Share-plus-Powerful Change casts use
the provider's existing transaction without a queued animated command. The
provider validates the real caster, exact spell source and selected variant,
recipient, native Share legality, live selections, qualified effect adapters,
and available Arcane Reservoir before Buff Planner submits `RuleCastSpell`.

The transaction is attached to the exact `AbilityData`, target, rule, context,
and execution process. Provider targeting, modifier adjustment, passive
Transmutation Supremacy, one-shot consumption, debit, rollback, and scoped
cleanup remain provider-owned. Buff Planner invokes `Spend()` once on the exact
planned spell source after provider commit; it never debits Arcane Reservoir.

Provider rejection cannot fall through to an ordinary unenhanced cast or
consume the spell source. A delayed effect process remains tracked until it is
terminal. Cleanup failure is reported as failure and unresolved state blocks
the next planned cast.

## Capability-aware fallback

Both Share's targeting strategy and the complete selected-enhancement set use
the same provider capability. Explicit Animated mode and ordinary native/manual
casting retain the existing `UnitUseAbility` path. If the provider is absent,
older, duplicated, or signature-incompatible, Share remains on the safe legacy
Animated route with a structured reason; it is never silently made free or
reported as instant.

## Validation boundary

Focused deterministic coverage exercises four sequential Share recipients,
the third and fourth cast, a subsequent ordinary instant buff, exact source and
reservoir ownership, combined Share plus Powerful Change, self-casting,
explicit Animated behavior, provider reservation/commit failure, delayed
completion, iterator cancellation, and cleanup failure. Exact final build and
package identities are recorded in `docs/QUALIFICATION.md` after the clean
release build.

Save-backed gameplay qualification is not claimed. No live mod installation,
game launch, or save mutation is authorized by this candidate build, and public
release publication requires separate owner authorization.
