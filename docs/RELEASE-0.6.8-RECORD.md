# 0.6.8 release record

Status: published stable GitHub release, **Latest**, November 4, 2026,
14:31:33 UTC. The owner approved/merged PR #16 and explicitly requested a new
release and version increment from the present default branch.

[Release and installable assets](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.8).

## Identity and publication

- Current-main starting point: merged PR #16 at
  `2d591cd7b4a2a887eb0fc8100526d609f61e44a3` (version 0.6.7).
- Release preparation branch: `codex/kingmaker-buff-planner-release-0.6.8`.
- Version/notes commit: `e38894718b88aeba65c07718176bb38d69a35f41`.
- Final main/product: `30cda65691f65dea91e6c3eeca993e73e6e4cddf`.
- UMM/informational version 0.6.8; CLR version 0.6.8.0. Standalone identity,
  .NET Framework 4.7/C# 7.3/Harmony12 contract and manifest minimum are unchanged.
- Annotated `v0.6.8`, tag object `bf4a81dae940be9c03f7d5f912781df3795d371b`,
  peels to the exact final product above.
- Public GitHub release ID `434037088`, non-draft, non-prerelease and Latest,
  independently checked after publication.
- ZIP `KingmakerBuffPlanner-0.6.8.zip`, SHA-256:
  `c846b37bd4574607e580ab71889061c29ec1979ca9e02429d2b086334ac33205`.
- DLL SHA-256:
  `28dbade47ceb7dc5040c6f1bb8491235611c18c84edff7fc627f1dfb128cd355`.
- Downloaded `SHA256SUMS.txt` SHA-256:
  `571efd817bce202c7d727ac509bf2c4b5e6627aa9e0c31db06d2865dc2387702`.

The exact built product remains in isolated clone `private/release/068-stable`
at 30cda65. Local ZIP, checksums, generated notes and manifest are under
`artifacts/release/0.6.8`; downloaded public assets and API receipts are under
`artifacts/verification/0.6.8` in that clone. This documentation uses separate
clone `private/release/068-record`, branch
`codex/kingmaker-buff-planner-release-0.6.8-record`. No existing checkout was
reset, cleaned or repurposed. Documentation changes do not relabel the frozen
product or transfer its source stamp.

## Qualification and commands

Selected boundary: existing **OwnerAuthorizedPreRuntime** publisher route,
with a 30-minute planning budget. The request authorizes public release while
the already disclosed native acceptance remains unverified. No native/purity,
installation or live-game campaign was selected.

| Check | Actual result |
| --- | --- |
| Source validation | 47 PASS / 0 FAIL per invocation |
| Complete official offline C# suite on final product | 614 PASS / 0 FAIL |
| Publisher gate tests | 8 PASS / 0 FAIL |
| Guarded push helper test | 6 PASS / 0 FAIL |
| Deterministic release build | 3 PASS / 0 FAIL; two identical DLL/ZIP builds |
| Package validation | 6 PASS / 0 FAIL per package invocation |
| Independently downloaded package validation | 6 PASS / 0 FAIL |
| Local ZIP / published digest / downloaded checksum | All match |
| Downloaded DLL / build manifest | Match |
| Annotated tag / final main product | Exact match |
| Native rendering, mouse/controller, scrolling, gameplay spending | NOT RUN |

The Windows PowerShell 5.1/MSBuild release route compiles production and the
official tests against installed game assemblies under .NET Framework 4.7 and
C# 7.3, with repository warnings/assertions intact. The complete suite includes
the duplicate-item, stale-casting, source-ID and exact-expression regressions.
Detached native-method fixtures do not constitute native game qualification.

```powershell
# Source/version and publisher preflight, then test guarded push on preparation branch:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Validate-Source.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-PublishReleaseGate.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-GuardedPush.ps1

# Main advanced by fast-forward only, pushed through project-owned guarded helper:
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command '& C:/Dev/KingmakerBuffPlannerLab/codex-policy/Push-KingmakerBuffPlanner.ps1 -RepositoryRoot C:/Dev/KingmakerBuffPlannerLab/repo/KingmakerBuffPlanner/private/release/068-stable -AllowDefaultBranch -Confirm:$false'

# Exact clean pushed main at 30cda65:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.8.md

# Independent public verification:
gh release download v0.6.8 --repo howardreith/KingmakerBuffPlanner --pattern 'KingmakerBuffPlanner-0.6.8.zip' --pattern 'SHA256SUMS.txt' --dir artifacts/verification/0.6.8
gh api repos/howardreith/KingmakerBuffPlanner/releases/tags/v0.6.8
gh api repos/howardreith/KingmakerBuffPlanner/releases/latest
git rev-parse 'v0.6.8^{}'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/validate-package.ps1 -PackagePath artifacts/verification/0.6.8/KingmakerBuffPlanner-0.6.8.zip
```

[Publisher log](evidence/release-0.6.8/publish.log),
[release manifest](evidence/release-0.6.8/release-manifest.json), and
[independent verification](evidence/release-0.6.8/verification.json) bind the
counts and hashes to the published product. The manifest's
`publicationStatus=local-only` records its pre-upload state; current public
state is proven by the verification/API receipts. No published asset was replaced.

## Refusal, scope and next action

The first publisher attempt stopped before qualification/tagging/publication
because its disclosure regex requires `not been run in the game` on one line;
the truthful sentence in the new notes was line-wrapped. Final commit 30cda65
changes only notes formatting. The existing validator was retained and then
passed. The refusal log is preserved, with no passing result attributed to it.
All prior evidence remains attributed to its original product/version.

0.6.8 exposes every genuinely available identical item copy and capable owner,
keeps per-copy bindings/budgets, leaves stale saved sources visibly unresolved,
makes numbered removal discoverable, and separates catalogue identity from
exact ability expression validation/projection. Full game reload can still
require explicit item reselection; unavailable inventory is not made castable.

This exact release has not been run in the game. Next action is review of a
focused disposable-save trial or separately selected guarded native acceptance:
all available copies, independent charges, exact stale #1 removal without
changing #4, Undo/reopen and mouse/controller scrolling. Current source/live
and launcher WhatIf purity, approved save pair, transactional staging/exact
restoration and run-bound casting allowance requirements remain mandatory.
Native Linux compatibility likewise remains unverified.

No permanent installation, Mods staging, game launch, save modification or
tester contact occurred. Only the owner-authorized version, release metadata,
tag/public assets and durable publication records were published.
