# Release 0.6.14 record

Owner-authorized pre-runtime release of the PR #23 durable item-source repair,
published 2026-11-08 18:45:13 UTC. **This exact release has not been run in the
game.**

| Identity | Value |
| --- | --- |
| Tag / product commit | `v0.6.14` -> `2eab694aeef5aec11b73ee63cf0072f6e07d78ce` (main) |
| PR | #23, accepted by the owner and merged 2026-11-08 18:43:55 UTC by fast-forwarding main to `d0c0cd9`; release-preparation commit `2eab694` (version stamp 0.6.13 -> 0.6.14, notes, README, dated changelog) pushed through the guarded push helper |
| Release | https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.14 (Latest, not draft/prerelease) |
| ZIP SHA-256 | `be0a2ffa72586c611cc146592011261b59510aac1f56fccfc389c55f0d343ebb` |
| Release DLL SHA-256 | `4d1453cd9e6a76257d15366328e4f155f9f14eccd86ac8a1803c805700c4c998` |

Commands, from an isolated fresh clone at `private/release/0614-stable` (the
ignored machine-local `GamePath.props` copied in first):

```powershell
git merge --ff-only origin/codex/kingmaker-buff-planner-durable-item-source
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\Build-Local.ps1"
gh pr ready 23
& C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1 -RepositoryRoot <clone> -AllowDefaultBranch -WhatIf
& C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1 -RepositoryRoot <clone> -AllowDefaultBranch -Confirm:$false
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.14.md"
gh release download v0.6.14 --pattern KingmakerBuffPlanner-0.6.14.zip --pattern SHA256SUMS.txt --dir artifacts/verification/0.6.14
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "& .\scripts\validate-package.ps1 -PackagePath artifacts/verification/0.6.14/KingmakerBuffPlanner-0.6.14.zip"
```

A first publisher attempt stopped after 1 second, before any tag or release: the
operator's `*>&1` wrapper turned `git fetch` progress on stderr into a terminating
error. The command above, without that redirect, succeeded unchanged.

| Check | Actual result |
| --- | --- |
| PR #23 candidate 5ebb83b (pre-merge) | focused 44/0, full offline 731/0, source 47/0 |
| Build-Local (source / build / package / local package) | 47/0, 1/0, 6/0, 1/0 |
| Publisher: source validation | 47 PASS / 0 FAIL |
| Publisher: complete offline C# suite | 731 PASS / 0 FAIL |
| Publisher gate tests | 8 PASS / 0 FAIL |
| Deterministic release build | 3 PASS / 0 FAIL; two identical builds |
| Package validation (local and downloaded) | 6 PASS / 0 FAIL each |
| Downloaded ZIP / published SHA256SUMS / local ZIP | Exact match |
| Downloaded ZIP Info.json version / DLL | 0.6.14 / `4d1453cd...c998` |
| Tag target / origin main | Exact match (`2eab694`) |
| Native casting, rendering, physical input, Linux | NOT RUN for these bytes |

Pending native acceptance, including area transitions, ordinary rest, Bag of Tricks
Instant Rest and the slot-rule cases, is described in the
[durable item-source checkpoint](DURABLE-ITEM-SOURCE-CHECKPOINT.md).
