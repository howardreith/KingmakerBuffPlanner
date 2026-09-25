# BagOfTricks fixture — bounded owner-approval packet (new versioned baseline)

**Status:** PREPARED, NOT APPROVED. This packet records the exact decision
the owner (Howie) is asked to make. No approval is granted, implied, or
acted upon here; the bootstrap lane remains blocked until the owner
decides through the established rebind workflow.

## 1. What the evidence establishes (and what it cannot)

Verified read-only on 2026-09-20 under the manifest's own inventory rules
(`Get-KbpDirectoryManifest`: relative path + length + sha256 per file,
recursively):

- **Current live inventory** of
  `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Mods\BagOfTricks`:
  `directoryManifestSha256 = c4487d11b2643a213b777a22cd1a79f5255aae320ca771e43afc7dd06aa2e205`,
  40 files, 1,805,725 bytes. Primary identities unchanged:
  `BagOfTricks.dll` sha256 `d03626594ece0f339aeef03ec7259684af7ddeb8b2edf41936f144848059a0e6`,
  `Info.json` sha256 `891c4a7864d2dd556a4628bcb19a8f6239a2bfbec9399f81e2414badc6de2974`.
- **Supporting receipts** (both re-verified: every per-file sha256 of the
  current directory matches both archived transaction manifests):
  - `runtime-evidence/install-rc2fix-liveui-3/install-result.json`
    (receipt sha256 `25939552a3621be86a1088cfe01a3f71469fae2bdc217d9be3c962bab43ecca1`)
  - `runtime-evidence/install-rc3-published-install-1/install-result.json`
    (receipt sha256 `7aa90657799ce33166524efe0cf1842a60840ad477bb2c4f9960a9b9c09dbdcc`)
  - Receipt provenance: guarded installation transactions (Sept 19) that
    verified other-mods-unchanged around the planner install. Their
    approval covers **installation and recorded bytes**, not a general
    fixture-use seal for runtime scenarios.
- **The unresolved historical difference:** the sealed profile aggregate
  (commit `3574069`, Aug 23: 41 files / 1,805,907 bytes /
  `34d89823…`) differs from the current inventory by one file of 182
  bytes. **This difference cannot be reconstructed from the retained
  evidence** (the August record is aggregate-only; per-file dumps from
  that era were not preserved; no approved-bytes snapshot exists). No
  claim is made about the file's role, mutability, or harmlessness.

## 2. Proposed decision (owner choice)

Approve a **new versioned fixture baseline** bound to the verified current
inventory (`c4487d11…`, 40 files, 1,805,725 bytes). This is a new baseline
for the going-forward runtime scenarios; it is **not** a certification
that the August contents matched, and it does not reinterpret history.

Alternative route the owner may prefer: obtain and restore an actually
available approved matching fixture through the authorized repair
workflow (none is currently known to exist in the lab).

## 3. Exact binding change proposed

In `compatibility/profiles/human-reproduction.json`, the BagOfTricks mod
entry changes only:
- `directoryManifestSha256`: `34d89823b1dc4e4be21a8a9053724abb444cd9f9a6857c6c5406afd62197f426`
  → `c4487d11b2643a213b777a22cd1a79f5255aae320ca771e43afc7dd06aa2e205`
- `fileCount`: 41 → 40
- `totalBytes`: 1805907 → 1805725

All other members (version, assemblyName, infoSha256, assemblySha256)
already match and are unchanged. CallOfTheWild and CheatMenu entries are
untouched. The original seal (Aug 23 record) is retained in git history
and in this packet — not overwritten silently. At approval time, run
`Write-KbpFixtureSealInventory` (new prospective harness function) so the
new baseline carries a per-file inventory and any future drift is exactly
explainable.

## 4. Permitted runtime scope of the approved baseline

Guarded `live-ui-bootstrap` (and campaign-context lanes derived from it)
with the `human-reproduction` profile against the disposable
`KBP_AUTOMATION_WORKING` save only, under the existing ownership, lock,
deployment-isolation, and restoration contracts.

## 5. Protected data (untouched in every case)

`KBP_AUTOMATION_SEED`, `KBP_AUTOMATION_BASELINE`, all ordinary player
saves, UserSettings profiles, other mods' bytes, credentials, and the
Steam account state. The fixture decision changes no file content — only
the recorded identity in the profile JSON.

## 6. Recovery and pre-use checks

- Rollback of the binding: revert the profile JSON commit (git); no live
  file is affected by the binding itself.
- Before each run the harness re-verifies the fixture identity, game
  process ownership, locks, and deployment isolation; a later mismatch of
  the NEW baseline requires fresh evaluation — approval does not expand.
- Every run restores transactionally; restoration receipts are verified
  by hash; an unrestorable state halts further runs.

## 7. Owner action requested

Reply with an explicit approval (or rejection/alternative) of section 2.
Until then, `live-ui-bootstrap` remains blocked, and this packet makes no
claim that the current fixture is authorized.
