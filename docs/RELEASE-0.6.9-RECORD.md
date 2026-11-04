# 0.6.9 release record

Published stable GitHub **Latest** release, November 4, 2026, 18:10:06 UTC.
The owner merged PR #18 and explicitly requested a release and version increment.
[Release and installable assets](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.9).

## Identity

- Merged-main starting point: `07c31f2da0092d965e1e1649f807849b0a1f7b25`.
  The release includes merged PRs #17 (Clear plan/input) and #18 (diagnostics).
- Preparation branch: `codex/kingmaker-buff-planner-release-0.6.9`.
  Version/notes commit: `11dc48aa1f7276dacf95198ace99a06b3d36a087`.
- Final main/product: `c206b78aa36255d569cd90cf574ec20c8c4eaaa6`.
- UMM/informational version 0.6.9; CLR version 0.6.9.0. Framework, Harmony12,
  standalone identity, save formats and minimum game-version gate are unchanged.
- Annotated `v0.6.9`, tag object `91897e6e5d3fd688e24252fe8703bdce6cd129b1`,
  peels to the exact final product above.
- GitHub release ID `434274191`; independently verified non-draft,
  non-prerelease and Latest.
- ZIP `KingmakerBuffPlanner-0.6.9.zip`, SHA-256:
  `119cf6148c64876bfb6e727b14916ce6e6f8ad2c0e07b63fc09c628896d8ced8`.
- DLL SHA-256:
  `3334b53ae4e9c75ccb9978d72091b9e8e4684c92cfd5b6baf4f763fffa26aa3b`.
- DLL MVID: `f8fef3ac-c81c-4988-a59e-391417271d8a`.
- Downloaded `SHA256SUMS.txt` SHA-256:
  `3b07547fe49972a093c84349fe51a72ecfe47a77e9c329e2433da6d4930ee82c`.

The frozen product checkout is `private/release/069-stable` at c206b78.
Local assets/manifest are under its `artifacts/release/0.6.9`; public downloads
and API receipts are under its `artifacts/verification/0.6.9`. Documentation is
maintained separately in `private/release/069-record`, branch
`codex/kingmaker-buff-planner-release-0.6.9-record`. The original repair worktree
remains untouched. No worktree was reset, cleaned or repurposed.

## Release qualification

Selected existing **OwnerAuthorizedPreRuntime** route with a 30-minute release
budget. The owner's release request follows the disclosed native limits; no
human acceptance or in-game qualification is inferred. The publisher performs
the complete official offline suite, deterministic builds and package checks.

| Check | Actual result |
| --- | --- |
| Source validation | 47 PASS / 0 FAIL per invocation |
| Final complete official offline C# suite | 653 PASS / 0 FAIL |
| Publisher gate tests | 8 PASS / 0 FAIL |
| Guarded push helper test | 6 PASS / 0 FAIL |
| Deterministic release build | 3 PASS / 0 FAIL; two identical DLL/ZIP builds |
| Package validation | 6 PASS / 0 FAIL per invocation |
| Independently downloaded package validation | 6 PASS / 0 FAIL |
| Local ZIP / published API digest / downloaded checksum | Exact match |
| Downloaded DLL / UMM version / build manifest | Exact match |
| Annotated tag / frozen product | Exact match |
| Native casting, UI/input and Linux acceptance | NOT RUN |

Windows PowerShell/MSBuild compiles against installed assemblies under .NET
Framework 4.7 and C# 7.3. The final suite includes the three review-identified
Animated qualification tests and the strengthened combined-evidence regression.
Final publisher duration: **66.791 seconds**. It stopped on the initial failure
below before any tag or release existed; no failing result is relabelled PASS.

Commands, from the indicated isolated clone:

```powershell
# Preparation branch: source/publisher/guarded push preflight.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Validate-Source.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-PublishReleaseGate.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-GuardedPush.ps1

# Main advanced only by --ff-only; actual branch publication used the owned helper.
& C:/Dev/KingmakerBuffPlannerLab/codex-policy/Push-KingmakerBuffPlanner.ps1 -RepositoryRoot C:/Dev/KingmakerBuffPlannerLab/repo/KingmakerBuffPlanner/private/release/069-stable -AllowDefaultBranch -Confirm:$false

# Exact clean pushed main at c206b78.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.9.md

# Independent public verification.
gh release download v0.6.9 --repo howardreith/KingmakerBuffPlanner --pattern KingmakerBuffPlanner-0.6.9.zip --pattern SHA256SUMS.txt --dir artifacts/verification/0.6.9
gh api repos/howardreith/KingmakerBuffPlanner/releases/tags/v0.6.9
gh api repos/howardreith/KingmakerBuffPlanner/releases/latest
git rev-parse 'v0.6.9^{}'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/validate-package.ps1 -PackagePath artifacts/verification/0.6.9/KingmakerBuffPlanner-0.6.9.zip
```

[Publisher log](evidence/release-0.6.9/publish.log),
[duration](evidence/release-0.6.9/publish-time.txt),
[release manifest](evidence/release-0.6.9/release-manifest.json), and
[independent verification](evidence/release-0.6.9/verification.json) bind the
results to the published bytes. The manifest's `publicationStatus=local-only`
is its pre-upload build state; current public state is established by the API
receipts and independent download, not by rewriting that manifest. Tracked
evidence copies normalize whitespace/encoding; raw originals remain in the
frozen clone. SHA-256 of the archived publisher log:
`39ec7826339cd0ec8e0a20258600acfbf7adb67ec500d234a66bfa2976a140a9`.

## First attempt and correction

The first publisher attempt on 11dc48a ran the complete suite:
**652 PASS / 1 FAIL**, **43.251 seconds**. The failure was
`sticky-qual-route-markers-are-the-instant-adapter-record`: its source-contract
assertion expects `";cleanup-state:" + cleanup.Detail;`. The merged diagnostic
change appended evidence within that statement instead. The final correction
c206b78 retains the established native cleanup-detail statement, then appends
optional evidence separately. Runtime text and verdicts are unchanged; the test,
validator and qualification checks were kept intact.

Before rerunning the publisher, this correction passed **29/0** in **10.169 s**:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'sticky-qual-route-markers-are-the-instant-adapter-record,heroism-repeat,qualification-enhanced-direct-recipe,qualification-rod-extend-recipe,sticky-qual-driver-passes-both-modes-and-pool-kinds,execution-policy-settled-touch-detail-survives-confirmation'
```

[Initial failure](evidence/release-0.6.9/publish-first-attempt.log) and
[focused correction](evidence/release-0.6.9/lifecycle-contract-correction.log)
are retained separately. The first failure created no tag or public release.
The corrected publisher ran on changed source bytes and passed all 653 tests.

## Scope and remaining uncertainty

0.6.9 adds undoable all-routine Clear plan confirmation/input fixes and more
accurate casting diagnostics. Successful reports retain both confirmation and
actual spending/route evidence. The reported Heroism gameplay defect remains
open; no unchanged-effect success exemption or continuation policy was added.

This exact release has not been run in the game. The next Heroism native action
is executable recipe preparation with a distinct sentinel, followed by a frozen
candidate, completed passing guarded selection, selection-bound allowance and
guarded casts in both modes. Native prepared-slot reuse across runs without
replenishment is also unverified. All launcher source/WhatIf purity, protected
save, transactional staging/locking and exact restoration requirements remain.

See [release notes](RELEASE-NOTES-0.6.9.md) and
[the investigation's ordered native handoff](HEROISM-REPEAT-CAST-REPAIR.md).
No game, permanent installation, Mods staging, saves or tester contact changed.
Publication, version metadata, tag/assets and durable release records were the
only external writes authorized and performed for this release.
Release documentation local-link checks passed 17/0; the complete documentation
diff from frozen product c206b78 passes `git diff --check`. Documentation does not
change the published tag, assets or product qualification identity.
