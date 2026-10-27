# 0.6.7 release record

Status: **published stable GitHub release, Latest**, on October 27, 2026 at
01:19:39 UTC (October 26 local time). The owner explicitly requested merge and
release of the limited-use source and casting-report update.

## Identity and merge

- [PR #15](https://github.com/howardreith/KingmakerBuffPlanner/pull/15) merged at
  01:18:31 UTC. Repair base: `e7b544f6b86bdd552737a99539dd34eb228c89b0`.
- Development source/test checkpoint: `eafb950a886c7ede741f0541d37b2140d9d4b500`;
  documentation checkpoint: `9338451dcde4d76fabcd98c57b9638d6b0438c99`.
  Their original evidence remains attributed to those bytes.
- Release preparation branch: `codex/kingmaker-buff-planner-release-0.6.7`;
  final candidate: `34b9e1e36053c40c8e63a804e36ec777a1740d71`.
- Merged main/product commit:
  `e60917511232c20f57bc833b8216222d7b07b081`.
- Annotated tag `v0.6.7`, tag object
  `318cd965423d526b17eda3105efdff354b2e1b07`, peels to that exact product commit.
- [Published release and installable assets](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.7).
- ZIP `KingmakerBuffPlanner-0.6.7.zip`, SHA-256:
  `76da15c0dfb7c5f23aa99b030225a7edf48c987011bab83e2275a4c80d69705f`.
- DLL SHA-256:
  `5f31f6a3be49a2c29bdc5e3de699bcde7f1fe60892ad70febf7a87339626b275`;
  MVID `9170f4a2-a403-4684-9802-5c5e86e3568f`.
- Downloaded checksum file SHA-256:
  `e363265e535633ce4184d233b9cd81771e4116c63fefea3aea4a0388b9ff470c`.
- CLR version 0.6.7.0; UMM version 0.6.7. The existing standalone identity,
  .NET 4.7/C# 7.3/Harmony12 contract and 2.1.5 manifest minimum remain intact.

The product is frozen in the ignored isolated clone `private/release/067-stable`
at the exact merged commit. Its `artifacts/release/0.6.7` contains the generated
release notes, ZIP, checksums and manifest; `artifacts/verification/0.6.7`
contains independently downloaded assets and API receipts. This record uses
the separate documentation-only clone `private/release/067-record`.
No existing worktree was reset, cleaned or repurposed.

## Qualification and publication

Selected boundary: existing **OwnerAuthorizedPreRuntime** release workflow.
Planning budget: 30 minutes for offline qualification, packaging, publication
and verification; no native/purity campaign selected. The final premerge pass
took 34.8 seconds. The guarded publisher completed successfully on main.

| Check | Result | Inputs |
|---|---|---|
| Focused Animated correction and affected neighbors | 19 PASS / 0 FAIL | Release preparation source and fixtures; `animated-correction-verified.log`. |
| Complete premerge offline C# suite | 602 PASS / 0 FAIL | Candidate `34b9e1e`; source 47/0, publisher gate 8/0, guarded push 6/0. |
| Complete publisher offline C# suite | 602 PASS / 0 FAIL | Exact merged product `e609175`; new version/source stamp. |
| Publisher source validation | 47 PASS / 0 FAIL per invocation | Merged product and each package build. |
| Publisher gate | 8 PASS / 0 FAIL | Existing guarded publication behavior. |
| Deterministic release build | 3 PASS / 0 FAIL | Two builds with identical DLL/ZIP hashes. |
| Package validation | 6 PASS / 0 FAIL per invocation | Local packages, final release package, independently downloaded ZIP. |
| Downloaded ZIP identity | 3 matching hashes | Local package, published GitHub asset digest, downloaded SHA256SUMS. |
| Downloaded DLL identity | PASS | Matches release manifest; tag peels to exact merged commit. |
| Native game/controller/mouse/Animated acceptance | NOT_RUN | Owner-authorized pre-runtime publication; no native claim. |

Commands (run in the relevant isolated checkout):

```powershell
# Candidate checks before merging; the wrapper dots Common.ps1 and
# PublishRelease.Common.ps1, then runs these existing functions/scripts:
Invoke-KbpReleaseValidation -Disposition OwnerAuthorizedPreRuntime -ScriptsRoot ./scripts
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-GuardedPush.ps1

# Concrete reporting correction and affected neighbors:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'fact-resource,qualification-enhanced-direct,qualification-rod-extend,shared-recipes,sticky-qual'

# Push only through the existing project-owned helper, then PR #15:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:/Dev/KingmakerBuffPlannerLab/codex-policy/Push-KingmakerBuffPlanner.ps1 -RepositoryRoot C:/Dev/KingmakerBuffPlannerLab/repo/KingmakerBuffPlanner/private/worktrees/KBP-RELEASE-067
gh pr merge 15 --merge --match-head-commit 34b9e1e36053c40c8e63a804e36ec777a1740d71 --subject 'Merge limited-use casting repair and prepare 0.6.7'

# Clean main at the merged product commit:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.7.md

# Independent public download and tag verification:
gh release download v0.6.7 --repo howardreith/KingmakerBuffPlanner --pattern 'KingmakerBuffPlanner-0.6.7.zip' --pattern 'SHA256SUMS.txt' --dir artifacts/verification/0.6.7
git rev-parse 'v0.6.7^{}'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/validate-package.ps1 -PackagePath artifacts/verification/0.6.7/KingmakerBuffPlanner-0.6.7.zip
```

[Captured qualification/publication output and manifest](evidence/release-0.6.7/hashes.json)
and [independent verification receipt](evidence/release-0.6.7/verification.json)
bind the counts to the actual product. The generated manifest's
`publicationStatus=local-only` describes its pre-upload state, not the current
GitHub release. GitHub was independently verified non-draft, non-prerelease
and Latest. The release-note evidence link was normalized to the tagged
repository URL through a notes-only edit; tag and asset identities stayed
unchanged. No published asset was replaced.

## Gate findings and corrections

The first candidate source gate stopped because the development handoff changed
only one of the two authoritative mission copies. Commit `2abf540` synchronizes
the mirror; neither validator nor earlier evidence was changed.

The next complete offline run reported **597 PASS / 5 FAIL**. Successful Animated
resource evidence had moved before effect confirmation and lost the established
`native-command-spend-completed` detail expected by the existing qualification
readers. Commit `34b9e1e` restores that successful evidence contract/order while
using `finally` to retain the observed decrease if a command or effect reader
fails. Existing enhanced, rod, shared and sticky qualification regressions pass
again. The native-item regression additionally checks that an effect-reader
failure retains observed spending, halts and never replays. The focused pass is
19/0; the complete final suite is 602/0. Original failing logs are preserved in
the evidence directory; no assertion was weakened to get a pass.

## Behavior, limits and next action

The release supports finite single-item charge budgets within a live session,
checks final source reservations in both engines, preserves native spending and
the unexpected-spend halt, and reports observed use/effects honestly. Saved
castings and targets remain; item selections require explicit reselection after
loading a save. Unsupported stacks/combined contracts stay refused. The
reporter's exact source/configuration is still unidentified.

This exact release has **not been run in the game**. The prior development
R1-R7 evidence is in the [repair checkpoint](LIMITED-USE-REPAIR-CHECKPOINT.md);
the complete release suite re-exercises those fixtures on the published product.
It does not turn detached native-method or simulated rule/command/effect results
into live acceptance. All native acceptance rows remain NOT_RUN.

Next action is a focused disposable-save trial of the published ZIP, or a
separately budgeted guarded lab qualification satisfying current launcher
source/WhatIf purity. Check legal multi-cast finite/unlimited sources, full-plan
shortage refusal, Instant controller/mouse and Animated reporting. Obtain the
full tester casting log independently. No permanent installation, game launch,
save mutation or tester contact occurred in this release workflow.
