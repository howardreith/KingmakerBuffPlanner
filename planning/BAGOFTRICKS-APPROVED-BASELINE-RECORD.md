# BagOfTricks fixture — approved baseline record

## Owner authorization

Recorded 2026-09-20 from the owner's explicit message
(`Kingmaker-Buff-Planner-Z-Approved-Fixture-and-Campaign-UI.md`):
Howie Reith approves the new BagOfTricks fixture baseline identified in
`planning/BAGOFTRICKS-FIXTURE-APPROVAL-PACKET.md` at commit
`e46f18eec87716a43938d8cac4b80e910d2ac312` (packet SHA-256
`40e1dab2b5ebe53157a33a298d429f07675894bf60bf3b3515e24f471b23af17`).

Approved binding (applied verbatim to
`compatibility/profiles/human-reproduction.json`):

- `directoryManifestSha256` →
  `c4487d11b2643a213b777a22cd1a79f5255aae320ca771e43afc7dd06aa2e205`
- `fileCount` → 40
- `totalBytes` → 1805725

Scope: guarded `live-ui-bootstrap` and campaign-context lanes with the
human-reproduction profile against `KBP_AUTOMATION_WORKING` only.
Native casting stays disabled. The August difference remains
unreconstructable; this is a go-forward baseline, not historical
equivalence. Detailed per-file inventory recorded at
`runtime-state/fixture-inventories/fixture-inventory-BagOfTricks-c4487d11b264.json`.

## Discovered blocker (NOT covered by this approval)

The same guard that refused BagOfTricks also refuses **CallOfTheWild**
and **CheatMenu** — their aggregate manifests drifted since the Aug 23
rebind while file counts and byte totals match:

| Mod | Profile manifest | Live manifest | Count/bytes |
|---|---|---|---|
| CallOfTheWild | `3f1758b2…` | `988e6130…` | 266 / 66,186,003 (match) |
| CheatMenu | `97f78227…` | `7960517c…` | 3 / 30,594 (match) |

The owner's authorization covers only the BagOfTricks entry. The
`live-ui-bootstrap` lane will be refused by these entries until the
owner approves equivalent versioned baselines for them (or restores
matching content). No silent expansion of authority was performed.
