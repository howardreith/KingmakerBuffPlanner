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

## Conclusion (bounded to the inspected surface)

- There is **no candidate durable identity property to test**: the
  inspected contract does not merely leave persistence unproven — a
  per-physical-item identity is absent. A durable binding would have to
  be an invented scheme (owner+blueprint+position), and position is
  exactly what reorder/inventory changes invalidate; the charter forbids
  substituting `GetInstanceID`, inventory order, or a blueprint GUID for
  a physical identity.
- Ownership semantics remain as previously recorded: rod charges are
  facts of (owner, blueprint) pools, which is why pooled per-caster
  semantics are the honest model for now.
- **Product consequence:** an exact physical-rod selection cannot be
  represented against this contract. The schema-6 `ExactSourceRef`
  placeholder stays reserved; pooled legacy selections remain clearly
  labeled unresolved intent; the affected castings block rather than
  silently choosing the first available rod. A06 stays NOT RUN at the
  durability level and is blocked on a contract that does not exist in
  the inspected assembly — not on missing test effort.

## What could change the conclusion

- A future game/mod update exposing a stable per-instance member, or an
  Owlcat serialization detail outside the inspected surface (e.g., a
  save-file-level stable key proven by comparing two identical rods
  across save/load/reorder). Proving that requires the authorized live
  fixture (currently blocked by the BagOfTricks baseline decision) and
  must compare observed keys, never assume one.
