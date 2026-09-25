# CallOfTheWild + CheatMenu fixture — approval request for the remaining human-reproduction entries

**Status:** APPROVAL REQUESTED — NOT YET GRANTED.

The approved BagOfTricks rebinding is applied and passes the guard, but
the `live-ui-bootstrap` lane stages ALL human-reproduction mods and the
guard refuses the other two entries: their profile aggregates (recorded
Aug 23 at `3574069`) are stale relative to the current live directories.

## What the evidence establishes

Per-file comparison (read-only) proves the current live directories are
**byte-identical** to the September 19 guarded-install transaction
receipts (`install-rc2fix-liveui-3` and `install-rc3-published-install-1`):
zero files added, removed, or changed for either mod. The difference from
the August profile aggregates cannot be reconstructed from retained
evidence (the same limitation as BagOfTricks).

## Proposed binding changes

In `compatibility/profiles/human-reproduction.json`, the exact
three-field changes for each mod:

### CallOfTheWild
```diff
-      "directoryManifestSha256": "3f1758b235820f3f1e8f4e21a965b2c74c9ba1ff50dbb4a4b8ad0afcceff2c28",
-      "fileCount": 266,
-      "totalBytes": 66186003,
+      "directoryManifestSha256": "988e6130e4d60bdda1be400f2c055f40ae6e91a44b75f456eb4822c77b31dc15",
+      "fileCount": 266,
+      "totalBytes": 66186003,
```
(only the manifest hash changes; count and bytes already match)

### CheatMenu
```diff
-      "directoryManifestSha256": "97f78227fb945c8e7f28307bd89507255682ac61105929348ab5b6986e576756",
-      "fileCount": 3,
-      "totalBytes": 30594,
+      "directoryManifestSha256": "7960517c82371ec89f372cdbf0690ee627e1dc9e7e6679d11324b06480726ce5",
+      "fileCount": 3,
+      "totalBytes": 30594,
```
(only the manifest hash changes; count and bytes already match)

## Per-file inventories (already recorded for verification)

- `runtime-state/fixture-inventories/fixture-inventory-CallOfTheWild-988e6130e4d6.json`
- `runtime-state/fixture-inventories/fixture-inventory-CheatMenu-7960517c8237.json`

## Proposed owner statement (PROPOSED — NOT YET GRANTED)

> I, Howie Reith, approve rebinding the CallOfTheWild entry to directory
> manifest sha256 `988e6130e4d60bdda1be400f2c055f40ae6e91a44b75f456eb4822c77b31dc15`
> and the CheatMenu entry to `7960517c82371ec89f372cdbf0690ee627e1dc9e7e6679d11324b06480726ce5`
> in `compatibility/profiles/human-reproduction.json`, within the same
> scope and protections as the BagOfTricks approval.
