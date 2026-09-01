# Kingmaker Buff Planner 0.0.16

This maintenance release improves structural catalog accuracy, cross-routine
clarity, communal coverage, and lower-left HUD integration.

## Correct caster spellbooks

For an optional Call of the Wild preparation/casting companion arrangement,
Buff Planner now keeps only the actual cast-capable spellbook. This is proven
from the installed component relationship rather than a class name, display
name, or GUID. Ordinary prepared and spontaneous books, multiclass books, and
currently exhausted casting books remain available.

## A stricter persistent-buff catalog

Reactive healing, restoration, condition removal, resurrection, dispel, and
cleanup abilities no longer appear solely because their action graphs contain a
temporary implementation marker. Branch-aware discovery preserves a real
lasting protection when one exists. Lay on Hands is therefore absent for a
general structural reason, not a special-case rule.

## Routine membership at a glance

Every catalog card now shows compact L, I, and S chips for its Long, Important,
and Short assignments. The active routine is emphasized; other routine
memberships remain visibly indicated with a full-text hover explanation and a
legend. The existing persisted assignments remain the sole source of truth.

## Communal coverage that agrees with planning

Allied communal/mass effects now carry conservative per-anchor recipient maps
from structural discovery through preview, indirect portrait coverage, grouping,
and execution planning. The repair covers Protection from Arrows, Communal and
Good Hope without spell-name production conditions, while enemy and ambiguous
areas remain non-party-wide.

## Native lower-left controls

The Setup, Long, Important, and Short buttons are now fresh owned `ButtonPF`
controls styled from the live native formation button's image, transition,
state, navigation, material, tint, and sound flags. Their white alpha-mask
glyphs receive native tinting. Native parchment tooltips replace the custom
black box, and successfully opening Setup plays the inspected native
`CharacterScreenOpen` UI sound once.

## Validation boundary

The deterministic suite covers provider roles, restoration semantics,
membership/persistence/layout, per-anchor coverage/grouping, native-style
contracts, tooltip ownership, and setup sound gating. Exact installed game and
Call of the Wild contracts were inspected. Live save-backed scenarios were not
performed because the guarded workflow reports zero exact
`KBP_AUTOMATION_BASELINE` and `KBP_AUTOMATION_WORKING` fixtures; no ordinary
save was substituted.
