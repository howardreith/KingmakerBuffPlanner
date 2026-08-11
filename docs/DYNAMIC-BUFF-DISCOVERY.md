# Dynamic Buff Discovery

Status: IN PROGRESS

The primary catalog is derived structurally from live spellbooks and abilities after all loaded mods finish blueprint registration. A spell name or hardcoded GUID list is never the primary inclusion mechanism.

The scanner walks ordered `ActionList` graphs with reference-cycle and depth guards. It emits an expression tree, retaining conditional alternatives, target transforms, recursion edges, duration information, and effect leaves. Native leaves include applied buffs, pet actions, party-member actions, area-effect buffs, and worn-item enchantments. Cast-spell, sticky-touch, and variant edges retain both origin and delivery/provider identities.

Unknown objects are not blindly reflected. Bounded reflection may traverse proven instance fields or properties whose exact runtime type is `ActionList` (or a small, explicitly tested wrapper contract). Unknown types, cycles, inaccessible members, and rejected wrappers are structured diagnostics. They do not silently turn a candidate into a false positive.

The discovery layers are:

1. exact native Kingmaker action contracts;
2. bounded generic `ActionList` wrapper discovery;
3. optional assembly/type adapters loaded without compile-time gameplay-mod references;
4. versioned include/exclude/effect overrides backed by exact evidence.

Planner eligibility is a separate decision from structural discovery. Harmful, instantaneous, combat-only, hostile-only, ambiguous, or unsupported resource/target contracts remain visible in catalog diagnostics but are excluded or evidentially deferred.
