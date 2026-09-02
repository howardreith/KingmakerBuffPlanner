# Kingmaker Buff Planner 0.0.17

This release restores dependable communal coverage and adds optional
native Brown Fur Share Transmutation support while keeping Personal targeting
strict and Alchemist Infusion passive.

## Communal coverage is visible again

Select one legal anchor for a structurally party-wide or allied-area buff and
every other recipient of that same cast now receives the light `COVERED`
portrait state. Covered allies are not silently selected, do not create extra
casts, and do not consume extra slots.

The repair follows the actual party-member action or friendly-area radius,
including the relationship between a selected variant and its declared source.
Resist Energy, Communal; Remove Fear; Good Hope; and Protection from Arrows,
Communal are regression canaries, not a production allowlist. Ordinary direct
spells and unmodified Personal spells remain unchanged, while hostile,
ambiguous, or malformed areas fail closed.

## Optional Share Transmutation

When the exact compatible Brown Fur implementation is installed and a caster
owns its validated Share feature/toggle, an eligible Personal Transmutation
spell gains an independent Share Transmutation checkbox. Turning it on exposes
only party or controlled units accepted by the provider's own
`AbilityData.CanTarget` behavior. Turning it off immediately removes stale
ally selections. Touch delivery below level 20 and the exact 30-foot
Transmutation Supremacy boundary remain native behavior.

Share can be combined with one Powerful Change score. The planner forecasts one
Arcane Reservoir point for either feature or two for both while reserving only
one spell slot. It never spends the live reservoir itself: the optional mod's
native animated-command transaction remains the sole successful-cast debit and
one-shot authority. If the mod or any exact feature/component contract is
absent or changed, Share is omitted or shown unavailable without broadening
targeting.

## Passive Alchemist Infusion

Qualifying Personal extracts automatically use Kingmaker's native Infusion
targeting when the Alchemist owns Infusion. There is no planner Infusion toggle,
no enhancement ID, and no added resource surcharge; the ordinary extract slot
is still consumed once.

## Safer enhancement composition

Temporary cast enhancements now use explicit exclusivity and native activation
groups instead of assuming every class feature conflicts. Shared resource costs
are summed before planning and checked again immediately before command
creation. Multi-enhancement summaries and existing profile collections remain
deterministic and backward compatible without a schema migration.

## Validation boundary

The source-only suite passes 41/41 source contracts, 135/135 behavior/protocol
tests, 8/8 harness filesystem tests, 4/4 package-fixture checks, 5/5 guarded
deployment-WhatIf checks, and 1/1 aggregate suite. The installed optional Brown
Fur 0.0.113 assembly contract passes 57/57, and the product retains no
compile-time gameplay-mod reference.

The qualified deterministic ZIP SHA-256 is
`e8991848e9d11168f2f7a4f6ea67a7ff233661e497e0d8867505f384286f963d`;
its DLL SHA-256/MVID are
`0451807d8c0f7431467c2cb3be22ba4e20edc9b552bfb6f66445fd69128e8d01` /
`983a62c2-5e63-4261-b7d0-996cbd836aaa`.

The owner accepted the mechanically qualified candidate and authorized public
publication. Save-backed gameplay scenarios could not be performed because
the guarded resolver reports zero exact `KBP_AUTOMATION_BASELINE` and
`KBP_AUTOMATION_WORKING` fixtures. This remains an explicit post-release
runtime-evidence boundary; no player save was substituted.
