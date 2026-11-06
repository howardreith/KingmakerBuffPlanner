# Release 0.6.11 record

Owner-authorized pre-runtime release of the PR #20 playtest repairs, published
2026-11-06 (UTC). **This exact release has not been run in the game.**

| Identity | Value |
| --- | --- |
| Tag / product commit | `v0.6.11` -> `3379d15c986169a525e8d3ab16ca17cd65fa1740` (main, fast-forward) |
| PR | #20 merged by fast-forward of main through the guarded push helper |
| Release | https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.11 (latest, not draft/prerelease) |
| ZIP SHA-256 | `bce5700923c81ac559eabc398d84559104757c0950608ff247c209a5872f890e` |
| Release DLL SHA-256 | `c84837dc84f04d7284f448a89879eb6ed812031ceec500c68db25a4b88978142` |

Commands, from an isolated fresh clone at `private/release/0611-stable` (the
ignored machine-local `GamePath.props` copied in first):

```powershell
& C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1 -RepositoryRoot <clone> -AllowDefaultBranch -Confirm:$false
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\Build-Local.ps1"
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.11.md"
gh release download v0.6.11 --pattern KingmakerBuffPlanner-0.6.11.zip --pattern SHA256SUMS.txt --dir artifacts/verification/0.6.11
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\validate-package.ps1 -PackagePath artifacts/verification/0.6.11/KingmakerBuffPlanner-0.6.11.zip"
```

| Check | Actual result |
| --- | --- |
| Focused pre-merge development selection (PR branch) | 81 PASS / 0 FAIL, 13.9 s |
| Build-Local (source / build / package / local package) | 47/0, 1/0, 6/0, 1/0 |
| Publisher: source validation | 47 PASS / 0 FAIL |
| Publisher: complete offline C# suite | 688 PASS / 0 FAIL |
| Publisher gate tests | 8 PASS / 0 FAIL |
| Deterministic release build | 3 PASS / 0 FAIL; two identical builds |
| Package validation (local and downloaded) | 6 PASS / 0 FAIL each |
| Downloaded ZIP / published SHA256SUMS / local ZIP | Exact match |
| Tag target / origin main | Exact match (`3379d15`) |
| Publisher duration | 48.4 s |
| Native casting, rendering, physical input, Linux | NOT RUN for these bytes |

Native evidence from the frozen pre-release candidate 1e1a312 (instant first and
recast Heroism plus Mirror Image confirmed; skip phase refused) is described in
[the PR #20 checkpoint](PLAYTEST-REPAIR-OWNER-PREFLIGHT-CHECKPOINT.md) and belongs
to those bytes only. The owner will playtest this release using the checklist in
that checkpoint.
