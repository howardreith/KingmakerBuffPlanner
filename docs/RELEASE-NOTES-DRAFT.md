# Kingmaker Buff Planner 0.0.15

Version 0.0.15 makes quick routines quieter, removes catalog entries the
current party cannot genuinely use, and adds explicit per-buff caster policy
controls for splitting a routine across multiple providers.

## Quieter quick actions

Long, Important, and Short no longer create the temporary lower-left result
panel. Quick execution still returns its complete result and writes structured
diagnostics to the Unity Mod Manager log. When the full planner is open, Apply
can still show that result in the planner's own footer. Nothing is redirected
to Kingmaker's common, combat, or event logs.

## Party-owned, beneficial catalog sources

Declared ability variants now enter the catalog only when the exact owned
source context establishes that the character can select that child. A child
that is directly owned remains valid, and an owned source with depleted slots
or resources remains visible as temporarily unavailable. A stale or unlearned
child is not recovered from the parent's declaration or replaced with a
sibling.

Discovery now preserves allied, enemy-only, and ambiguous area recipients as
well as offensive delivery and action-branch provenance. A planner entry needs
a persistent beneficial payload that can affect a proven safe player
recipient. Instant damage, hostile delivery, enemy-only or ambiguous areas,
instant healing without a persistent effect, and hidden carrier markers that
only accompany an offensive action no longer qualify. Legitimate hidden
self-buffs remain supported when their structure proves the hidden effect is
the actual beneficial payload.

## Caster Policy

The selected-buff panel now opens a Caster Policy dialog with one row for each
exact provider, including distinct spellbooks and source instances. Each row
shows the caster and source, spell level where relevant, casts remaining or At
will, and any temporary unavailable reason.

For each provider, a routine can explicitly choose Use or Do not use, move the
provider earlier or later, and set a maximum per run such as Unlimited, 1, or
2. A provider can be both preferred and capped. Reset Automatic clears only
the selected buff's provider preferences. Preview allocation updates after
every change, so configurations such as one cast from Felix followed by the
remaining casts from Akasa are visible before execution. Disabled providers
and exhausted combined caps produce unfulfilled casts instead of being
silently bypassed.

The policy keeps the existing exact provider identity and schema 4 persistence
fields. No profile schema migration is required, and stale provider keys remain
harmless ignored data rather than binding to a different caster or source.

## Validation boundary

The deterministic suite covers HUD callbacks and diagnostics, variant
ownership and depletion, exact child execution and single resource spending,
beneficial/offensive branch classification, provider ordering/bans/caps,
persistence, preview allocation, and modal layout/input isolation. Exact
Kingmaker 2.1.7b contracts were inspected from the installed assembly.

Save-backed in-game checks could not be run because the repository guard found
zero exact `KBP_AUTOMATION_BASELINE` and `KBP_AUTOMATION_WORKING` fixtures. No
ordinary campaign save was substituted. The bounded in-game checklist is in
`docs/MANUAL-ACCEPTANCE.md`.
