# Release 0.6.13 record

Owner-authorized pre-runtime release of the PR #22 prepared-slot spend repair,
published 2026-11-06 13:48:40 UTC. **This exact release has not been run in the
game.**

| Identity | Value |
| --- | --- |
| Tag / product commit | `v0.6.13` -> `a9ba4c4345f738cd166159005a6d6aeb3ae76bcb` (main) |
| PR | #22 merged by the owner (`5cef7c2`); release-preparation commit `a9ba4c4` (version stamp 0.6.12 -> 0.6.13, notes, README, dated changelog) pushed to main through the guarded push helper |
| Release | https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.13 (Latest, not draft/prerelease) |
| ZIP SHA-256 | `3e0399f3aeb5b89573e10336804f425a9f7cad9c39aa4d160b7a50e1b51dbe11` |
| Release DLL SHA-256 | `b638b0c46a790f4ad6a4c54dae30c90534ff52e1c94e072d77a79a6a63214719` |

Commands, from an isolated fresh clone at `private/release/0613-stable` (the
ignored machine-local `GamePath.props` copied in first):

```powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\Build-Local.ps1"
& C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1 -RepositoryRoot <clone> -AllowDefaultBranch -WhatIf
& C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1 -RepositoryRoot <clone> -AllowDefaultBranch -Confirm:$false
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.13.md"
gh release download v0.6.13 --pattern KingmakerBuffPlanner-0.6.13.zip --pattern SHA256SUMS.txt --dir artifacts/verification/0.6.13
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\validate-package.ps1 -PackagePath artifacts/verification/0.6.13/KingmakerBuffPlanner-0.6.13.zip"
```

| Check | Actual result |
| --- | --- |
| PR #22 candidate ff2a237 (pre-merge) | focused 65/0, full offline 713/0, source 47/0 |
| Build-Local (source / build / package / local package) | 47/0, 1/0, 6/0, 1/0 |
| Publisher: source validation | 47 PASS / 0 FAIL |
| Publisher: complete offline C# suite | 713 PASS / 0 FAIL |
| Publisher gate tests | 8 PASS / 0 FAIL |
| Deterministic release build | 3 PASS / 0 FAIL; two identical builds |
| Package validation (local and downloaded) | 6 PASS / 0 FAIL each |
| Downloaded ZIP / published SHA256SUMS / local ZIP | Exact match |
| Downloaded ZIP Info.json version / DLL | 0.6.13 / `b638b0c4...4719` |
| Tag target / origin main | Exact match (`a9ba4c4`) |
| Publisher duration | 52 s |
| Native casting, rendering, physical input, Linux | NOT RUN for these bytes |

Pending native acceptance (one uninterrupted Long through both Heroisms and
the successor) is described in the
[prepared-slot repair checkpoint](PREPARED-SLOT-SPEND-REPAIR-CHECKPOINT.md).
