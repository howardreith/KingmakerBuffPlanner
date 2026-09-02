# Kingmaker Buff Planner 0.0.18 (local candidate)

This candidate fixes repeated Freedom of Movement execution in Instant mode
through a structural sticky-touch transaction. Freedom of Movement is the
regression canary; no spell name, spell GUID, caster, spellbook, target count,
or party size is hardcoded in production behavior.

## True instant sticky-touch delivery

A safe beneficial sticky-touch carrier with one valid friendly delivery
blueprint is now classified as `StickyTouchDeliveryRuleCast`, not as inherently
animated-only. Instant execution derives the delivery `AbilityData` from the
exact planned source instance, preserves its caster, spellbook, reservation,
variant, metamagic, and calculated context, and submits one `RuleCastSpell` for
that delivery.

Resource ownership remains with the exact source `AbilityData`. The executor
calls native `Spend()` once on that source after rule submission, never spends
the derived delivery instance, and never casts or charges both carrier and
delivery. Prepared reservation tokens and linked opposition slots therefore
remain exact; spontaneous spell pools lose one use per successful planned
cast.

## Reliable repeated targets

The executor now uses a state-based completion boundary. It confirms the
expected target effect and proves that no matching held touch or generated
delivery command remains before the next transaction may begin. Failure,
timeout, exception, or cancellation cleans up cast-scoped state and releases
enhancement leases.

Diagnostics distinguish strategy selection, rule submission, native rule
success, UMD/spell failure, `Spend()` invocation, observed resource delta,
effect confirmation, and residual touch state. Stable routine, step, source,
provider, unit, reservation-token, and carrier/delivery identifiers accompany
those results.

## Deliberate Animated mode

Animated mode remains available, and native-command-required enhancements still
force it. A sticky-touch animated operation now owns the complete native
lifecycle: carrier command, generated delivery command when applicable,
expected effect, and settled held-touch state. It cannot report success after
only the carrier command.

## Validation boundary

The focused suite passes 42/42 source contracts, 145/145 behavior/protocol
tests, 8/8 runtime-harness filesystem tests, 4/4 package-fixture checks, and
5/5 deployment-WhatIf checks. Regression coverage includes four consecutive
prepared and spontaneous targets, exact reservation tokens, delayed effects,
UMD spend policy, no double spend, animated two-stage completion, and cleanup
after failures and exceptions.

Save-backed gameplay qualification is still blocked: the guarded resolver
finds no exact `KBP_AUTOMATION_BASELINE` or `KBP_AUTOMATION_WORKING` save. No
ordinary player save was substituted, so the observed frequency of the live
third-target failure and the real Freedom of Movement canary remain manual
acceptance items. No public release is authorized or published by this local
candidate.
