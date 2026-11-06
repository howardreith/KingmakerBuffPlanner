# Release 0.6.12 record

Owner-authorized pre-runtime release of the PR #21 final refinement, published
2026-11-06 03:31:06 UTC. **This exact release has not been run in the game.**

| Identity | Value |
| --- | --- |
| Tag / product commit | `v0.6.12` -> `cedb6230173348c188fafcffe117d970f04927f8` (main) |
| PR | #21 merged by the owner (`8685366`); release-preparation commit `cedb623` pushed to main through the guarded push helper |
| Release | https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.12 (Latest, not draft/prerelease) |
| ZIP SHA-256 | `205703a736661b51b687c50b6bbdacce9fdb692fcc2faa60cf274ab834ad970e` |
| Release DLL SHA-256 | `805a1719d00fcbdb53384d985a70b1a525f59bb968b683669863ea4413cf25de` |

The version was incremented from released 0.6.11 to 0.6.12 (stamped during PR
#21, released here).

Commands, from an isolated fresh clone at `private/release/0612-stable` (the
ignored machine-local `GamePath.props` copied in first):

```powershell
& C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1 -RepositoryRoot <clone> -AllowDefaultBranch -Confirm:$false
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\Build-Local.ps1"
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.12.md"
gh release download v0.6.12 --pattern KingmakerBuffPlanner-0.6.12.zip --pattern SHA256SUMS.txt --dir artifacts/verification/0.6.12
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\validate-package.ps1 -PackagePath artifacts/verification/0.6.12/KingmakerBuffPlanner-0.6.12.zip"
```

| Check | Actual result |
| --- | --- |
| PR #21 final candidate 54d26f1 (pre-merge) | focused 179/0, full offline 706/0, reader 19/0 |
| Build-Local (source / build / package / local package) | 47/0, 1/0, 6/0, 1/0 |
| Publisher: source validation | 47 PASS / 0 FAIL |
| Publisher: complete offline C# suite | 706 PASS / 0 FAIL |
| Publisher gate tests | 8 PASS / 0 FAIL |
| Deterministic release build | 3 PASS / 0 FAIL; two identical builds |
| Package validation (local and downloaded) | 6 PASS / 0 FAIL each |
| Downloaded ZIP / published SHA256SUMS / local ZIP | Exact match |
| Downloaded ZIP Info.json version / DLL | 0.6.12 / `805a1719...25de` |
| Tag target / origin main | Exact match (`cedb623`) |
| Publisher duration | 61 s |
| Native casting, rendering, physical input, Linux | NOT RUN for these bytes |

The pending in-game checks and the owner playtest checklist are in the
[final refinement checkpoint](FINAL-REFINEMENT-0.6.12-CHECKPOINT.md).
