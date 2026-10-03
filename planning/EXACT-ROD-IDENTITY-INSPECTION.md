# Exact rod identity — installed-contract inspection (A06)

**Inspected:** installed `Assembly-CSharp.dll` (Kingmaker 2.1.7b,
`Kingmaker_Data\Managed`), types `Kingmaker.Items.ItemEntity` and
`Kingmaker.Items.ItemsCollection`, via read-only reflection-only load on
2026-09-20. No game state, save, or mod file was changed.

## Observed contracts (member-level evidence)

`ItemEntity` public surface relevant to identity:
- `UnitDescriptor Wielder`, `UnitDescriptor Owner` — owning-character
  descriptors, not item identities.
- `string Name`, `bool IsIdentified`, `bool HasUniqueOriginArea`,
  `bool HasUniqueVendor`, `bool HasUniqueSourceDescription`,
  `string UniqueSourceDescription` — presentation/provenance prose; not a
  stable key, and empty for ordinary shop-bought rods.
- Serialization lifecycle: `PreSave()` / `PostLoad()` hooks only.

No member matching id/guid/serial/instance-hash exists on `ItemEntity`
(field scan including non-public backing fields found only
identify-roll state). `ItemsCollection` stores a positional
`List<ItemEntity>` (`m_Items`): item "identity" is list membership.

## Serialization/reference-path inspection (2026-09-20, read-only)

Installed `Assembly-CSharp.dll` sha256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`
(matches the repository's recorded game-build identity). No `Owlcat.*`
serializer assemblies and no ZeroFormatter/MessagePack/protobuf
assemblies exist in this Kingmaker build. State serialization goes
through Newtonsoft JSON attribute contracts (entry point observed:
`Kingmaker.EntitySystem.Persistence.JsonUtility.UnitSerialization.Serialize`
returning `JToken`).

`ItemEntity` persisted fields (via `JsonPropertyAttribute`):
`m_Blueprint` (blueprint reference), `m_Count`, **`m_InventorySlotIndex`
(positional)**, `m_Enchantments`, `m_FactsAppliedToWielder` (`Fact[]`,
nullable), `m_SkinningSuccessful`. No identifier member exists;
`ItemsCollection` persists a positional `List<ItemEntity>` (`m_Items`).

## Corrected conclusion — three identity tiers, stated separately

The earlier "identity is absent, not merely unproven" wording was too
broad. What the inspected evidence supports is:

1. **Runtime (current-process) identity** — object references /
   `GetInstanceID`: invalid across sessions by definition; the charter
   forbids substituting them.
2. **Within-save reference identity** — if the save writer enables
   Newtonsoft reference preservation (`$id`/`$ref`), those ids are
   assignment artifacts of one serialization graph. Nothing inspected
   binds them stably across a save/load/save cycle; the settings-
   construction site was not located within this bounded scan. Such ids
   are graph-scoped references, not physical-item identity, and would
   require empirical cross-save stability proof before any use.
3. **Durable cross-save identity** — the persisted state tuple is
   (blueprint, count, positional slot, enchantments, facts). None is a
   stable physical key: slot position is exactly what reorder
   invalidates; an enchantment/charge fingerprint conflates two
   genuinely identical rods. **No usable native durable per-item
   identity was found in the inspected persisted-state contract** — this
   does not prove every association strategy impossible (see below).

## Named candidate requiring live proof (not implemented, not claimed)

`ItemEntity` persists `Fact[]` per item. A mod-applied persistent fact
could in principle carry a mod-chosen per-physical-item key across
save/load IF (a) facts survive per-item with stable dedup semantics
across save/load/reorder, and (b) attaching one is permitted without
corrupting native behavior. Proving either requires the authorized live
fixture (currently blocked by the BagOfTricks baseline decision), and
attaching facts mutates native item state — outside this prompt's
authority. A sidecar file keyed by anything weaker than a verified
native association solves nothing and is not a candidate.

## Product consequence (unchanged)

Pooled selections stay honestly unresolved; affected castings block
rather than silently choosing the first available rod; `ExactSourceRef`
stays reserved. A06 remains blocked on unproven prerequisites — not on
missing search effort — and does not gate non-rod workspace
qualification.

## What could change the conclusion

- A future game/mod update exposing a stable per-instance member, or an
  Owlcat serialization detail outside the inspected surface (e.g., a
  save-file-level stable key proven by comparing two identical rods
  across save/load/reorder). Proving that requires the authorized live
  fixture (currently blocked by the BagOfTricks baseline decision) and
  must compare observed keys, never assume one.
