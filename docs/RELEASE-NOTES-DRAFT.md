# Kingmaker Buff Planner 0.0.19

This release fixes Share Transmutation ignoring Instant mode. Resinous Skin
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

The owner authorized committing, merging, pushing, and publishing both mods.
Save-backed gameplay: NOT RUN. The required protected automation save pair is
unavailable; no ordinary campaign save was substituted and no live mod staging
or game launch occurred. Mechanical evidence is not live gameplay evidence.

Install the paired Kingmaker Gunslinger 0.0.115 update for Instant Share support.
Older providers safely retain animated Share casting with a diagnostic reason.
On an approved disposable working save, cast Felix's Resinous Skin with Share
on four different allies out of combat, checking the effect, one normal spell
use and one reservoir point per cast, including casts three and four. Follow
with an ordinary buff. Separately check eligible Share plus Powerful Change
(two reservoir points), applicable Supremacy, and Animated/manual controls.
