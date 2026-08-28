# Kingmaker Buff Planner 0.0.14

Version 0.0.14 keeps complete localized spell names visible and adds
first-class concrete entries for buff spells whose parent requires a variant
choice.

## Complete names

Catalog cards no longer rely on a fixed one-line name region. The planner
measures wrapped localized text, grows only the affected grid row, and moves the
availability and configuration text below it. Short names remain compact.
Selected-detail and description views also render the complete catalog name.
This keeps distinctions such as Communal, Greater, Mass, and localized or
mod-added suffixes in the primary UI rather than hiding them behind clipping,
ellipsis, or a tooltip.

## Concrete selectable variants

Eligible `AbilityVariants` children are discovered structurally and exposed as
separate catalog entries. The complete localized parent is combined with the
localized distinguishing child text through one formatter. Parent search finds
all siblings, child terms find the requested choice, and siblings stay together
in the blueprint's declared order. Child icons are preferred with a safe parent
fallback.

The unresolved parent container is not selectable. Each child independently
must satisfy the planner's existing persistent beneficial-buff rules, so this
does not indiscriminately import summoning, weapon, transformation, or other
choice menus.

The exact Kingmaker 2.1.7b native catalog contains five supported children for
both Resist Energy and Resist Energy, Communal, including Sonic. The two
families retain their separate parent identities and native declared orders.

## Casting, resources, and saved plans

Each entry stores both the parent/source ability GUID and the concrete child
GUID. The parent establishes spellbook ownership, spell level, prepared token
or spontaneous pool, metamagic, fact/resource context, and material state. The
child supplies the exact effect, localized presentation, target rules, and cast
blueprint.

Execution constructs the concrete child through Kingmaker's own
parent-data/child-blueprint `AbilityData` path. Availability and spending then
delegate through the parent exactly once; neither executor chooses the first
variant or falls back to a sibling.

Saved concrete choices round-trip by stable blueprint identifiers. A legacy
parent-only assignment migrates only if one eligible child is unambiguous.
Otherwise the planner leaves it unsupported and reports that the complete
parent requires reselection; it never invents an energy type.

## Validation boundary

The deterministic suite covers complete-name layout, generic expansion and
filtering, stable identities, order/deduplication, parent/child search,
persistence and legacy behavior, parent-backed availability, exact-child
planning, single resource reservation, ordinary casting, icon fallback, and
non-English formatting.

Exact assembly IL and blueprint-catalog evidence were inspected locally. No
save-backed in-game cast or visual qualification is claimed for this build
because the repository guard found no authorized `KBP_AUTOMATION_BASELINE` or
`KBP_AUTOMATION_WORKING` save. See `docs/MANUAL-ACCEPTANCE.md` for the bounded
runtime checklist.
