# 0.6.4-alpha.1 release record

Status: **published GitHub prerelease** on 2026-10-20. Stable 0.6.3 remains
GitHub's Latest release. The owner explicitly requested this alpha publication
after receiving the 0.6.3 log investigation and native-test limitation.

## Identity

- Branch: `codex/kingmaker-buff-planner-controller-entry-investigation` (PR #13).
- Product/tag commit: `a68052a30a6169394ead567ee6005d09ac45d4b1`.
- Version: `0.6.4-alpha.1`; tag: `v0.6.4-alpha.1`.
- Release: https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.4-alpha.1
- ZIP: `KingmakerBuffPlanner-0.6.4-alpha.1.zip`.
- ZIP SHA-256: `42d4be080702597471e81927eba774516f253420e3b0b1c46a89c8abaad2df61`.
- DLL SHA-256: `9abcc8dbbf081248c41d988c017b76c38c5ac0f500aa55707cb7ff464641ae6e`.
- Assembly MVID: `c572d50a-af34-4e5a-a063-6923c019f0ac`.
- Local evidence: `artifacts/release/0.6.4-alpha.1/release-manifest.json`,
  `SHA256SUMS.txt`, and the validated ZIP. GitHub's downloaded ZIP matched
  the same SHA-256. The manifest's `publicationStatus=local-only` describes
  the build before upload; GitHub's release is published and marked prerelease.

## Checks and publication

- Release note version and relative-link/text check: PASS.
- `scripts/Test-GuardedPush.ps1`: PASS=6 FAIL=0. The project-owned helper
  pushed the alpha branch commit and verified its remote HEAD.
- `scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease
  -AllowFeatureBranchPrerelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.4-ALPHA.1.md`:
  source validation PASS=47 FAIL=0; complete offline C# suite PASS=583 FAIL=0;
  publisher gate PASS=8 FAIL=0; two deterministic release builds; package
  validation PASS=6 FAIL=0. Tag and prerelease were published.
- Independent GitHub check: release `isDraft=false`, `isPrerelease=true`;
  tag resolves to the product commit; ZIP downloaded from GitHub and its hash
  matched the local validated package. Stable 0.6.3 still reports Latest.

The first publisher invocation stopped before validation or publication because
this isolated clone fetched only `main` and lacked the alpha branch's
`origin/<branch>` tracking ref. Fetching that exact ref resolved the guard;
the successful invocation then completed all checks.

## Evidence and next action

The 0.6.3 laptop log showed card selection and refusal before handoff, with no
mod first update tick or controller construction. Its SHA-256 is
`8427BCF6FC3AFEA45746EAC53140D85C959FAA57688980882051FFEC78DA71E8`;
the [investigation](CONTROLLER-ENTRY-0.6.3-LOG-INVESTIGATION.md) records exact
lines and rejected layer-mismatch theory. The reason UMM's UI update host did
not spawn on the laptop remains unknown. The alpha's owned update driver is a
source repair, not native proof.

This exact release has **not been run in the game**. No game installation,
save, profile, or setting was changed by this publication. Next, run a guarded
no-cast controller observation with this exact ZIP on the owner's laptop:
RT/R2 campaign menu -> Buff Planner card -> Confirm/A -> destination visible
and usable -> Back -> ordinary gameplay -> repeat; inspect the spellbook
footer. Capture the matching UMM/Unity log and terminal handoff stages before
any plan edits, casting, stable release, or merge decision.
