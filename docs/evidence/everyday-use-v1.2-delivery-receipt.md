# Everyday-use v1.2 private preview — delivery receipt (2026-10-03)

Private draft release (never published, no tag created, not merged):
GitHub release id `399014941`, titled "Private previews: everyday-use v1.2
(8d7681d0) and casting-graph beta (ec34705c)", planned tag name
`v0.2.0-rc6-casting-graph-preview`, target `main`. It is visible only to the
repository's collaborators while it stays a draft.

Retrieval: the repository's Releases page,
`https://github.com/howardreith/KingmakerBuffPlanner/releases` (the draft is
listed there; its own `untagged-...` link changes whenever the draft is
edited), or
`gh release download v0.2.0-rc6-casting-graph-preview --repo howardreith/KingmakerBuffPlanner --pattern "KingmakerBuffPlanner-0.2.0-rc6-everyday-use-v1.2+8d7681d0.zip"`
(checked after the last edit: 701341 bytes, the package hash below).

Note: updating the draft's title and notes through the REST API without
re-sending `tag_name` reset the draft's planned tag name to an `untagged-...`
placeholder. It was restored immediately to `v0.2.0-rc6-casting-graph-preview`
(target `main`, still a draft, `published_at` null); no git tag exists and
nothing was published. Later edits re-send `tag_name`, `target_commitish` and
`draft=true` together.

## Delivered asset

| Item | Value |
| --- | --- |
| Asset | `KingmakerBuffPlanner-0.2.0-rc6-everyday-use-v1.2+8d7681d0.zip` (asset id `607817987`) |
| Bytes | the frozen package `runtime-backups/qualification-frozen/8d7681d03752f3f7170f25f7d45029f71c46a884/KingmakerBuffPlanner-0.2.0-rc6-local-runtime.zip`, renamed only |
| Size | 701341 bytes (frozen 701341) |
| Package SHA-256 | `5b24e2eafee69b18898894b6e9a5cfb9b2c8e625a561494cb2234766097d8d17` (GitHub digest identical) |
| DLL SHA-256 inside the ZIP | `deee7b1de54daee9e890f6680c8e0d4db09674b12c394e2d2d6c2d3e460cf65b` |
| Assembly MVID | `1e762004-8200-430e-853e-d61456a627ae` |
| Embedded source commit / version | `8d7681d03752f3f7170f25f7d45029f71c46a884` / `0.2.0-rc6` (Info.json Id `KingmakerBuffPlanner`, Version `0.2.0-rc6`) |
| Entries | `KingmakerBuffPlanner/Info.json`, `KingmakerBuffPlanner.dll`, `NativeEffectOverrides.json`, `THIRD-PARTY-NOTICES.md` |

Verification: the asset was downloaded back through the GitHub API after the
upload and checked against the freeze record (size, package hash, DLL hash
inside the ZIP, MVID read from the DLL, the build's embedded commit and
version, Info.json, exact entry set): all eight checks PASS. The same check
against the earlier 3c1c5d4 freeze record fails (negative control).

## Prior beta asset (rollback) untouched

`KingmakerBuffPlanner-0.2.0-rc6-casting-graph+ec34705c.zip` (asset id
`597866119`): 661250 bytes, SHA-256
`214b795b5e9777eb38a7f14517b5c7eaf5b1ad192065757a20ee4d59e8b20564`, GitHub
`updated_at` 2026-09-29T09:56:21Z - downloaded and hashed before the upload
and again after it: identical both times. The release stayed a draft
(`published_at` null) throughout.
