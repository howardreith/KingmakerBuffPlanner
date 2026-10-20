# 0.6.4 release record

Status: **published stable GitHub release** on 2026-10-20 at 23:47:49 UTC.
The owner tested `v0.6.4-alpha.1` on the main laptop, reported that it is
functional with further improvements needed, and authorized the merge and
stable release. No detailed native acceptance log was supplied for that test.
The exact stable ZIP has not been run in the game.

## Identity

- [PR #13](https://github.com/howardreith/KingmakerBuffPlanner/pull/13)
  merged into `main` as `df61617dc592df54bd3285c48d7eafe5aa66373c`.
- Release preparation branch:
  `codex/kingmaker-buff-planner-release-0.6.4`, commit
  `21a42f3519a7d1b763f78329f2ae0b9addcb8d94`.
- Product/tag commit on `main`:
  `891ae2f7d47f21dbdcbf0fa6ee4836b4a98c2f95`. Annotated tag object:
  `c216c19360fa6ac8f690d7ea6fbbf50226f58f5f`.
- Version: `0.6.4`; tag: `v0.6.4`;
  [published release](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.4).
- ZIP: `KingmakerBuffPlanner-0.6.4.zip`, SHA-256
  `8e5580f90b4213b15ea94ddde98ee3c7bd8aee0cf3b0882ca057ba29e9572655`.
- DLL SHA-256:
  `a1b758fe8e4c3bc2bf4250987e70d84b0a015a0e5eebfb87e6b48bc99366b256`;
  assembly MVID: `d1a697cc-0ce1-4c14-8f3e-80f504c4dd99`.
- Local evidence:
  `private/release/063-main/artifacts/release/0.6.4/release-manifest.json`,
  ZIP and `SHA256SUMS.txt`. Independently downloaded assets are under
  `private/release/063-main/artifacts/release064-download/`.

## Checks and publication

- Release version/link targets: PASS=6 FAIL=0. `git diff --check`: PASS.
  `scripts/Validate-Source.ps1`: PASS=47 FAIL=0 before the release commit.
- `scripts/Test-GuardedPush.ps1`: PASS=6 FAIL=0. The project-owned helper
  checked and pushed the release branch and both `main` commits, including
  the release-note wording correction.
- `scripts/Publish-Release.ps1 -Publish
  -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath
  docs/RELEASE-NOTES-0.6.4.md`: source validation PASS=47 FAIL=0; complete
  offline C# suite PASS=583 FAIL=0; publisher gate PASS=8 FAIL=0; two
  deterministic release builds with equal DLL and ZIP hashes; final package
  validation PASS=6 FAIL=0. The publisher created the annotated tag and
  non-draft, non-prerelease GitHub release.
- GitHub's `releases/latest` API reports `v0.6.4`. The downloaded ZIP and
  `SHA256SUMS.txt` match local hashes: PASS=2 FAIL=0. The checksum file's
  SHA-256 is
  `3076825e43a02fe65c0e690c6e0b2b7724a3b0f25d912a568a89645a6fde6665`.

The first publisher invocation stopped before validation, tag creation or
publication because its note check requires the exact phrase "not been run in
the game" on one line. Commit `891ae2f` corrected that wording; the successful
publisher run used that commit. The manifest's `publicationStatus=local-only`
describes the build before upload; publication was independently verified.

## Evidence, limits, and next action

The 0.6.3 laptop log established card selection followed by
`request-refused:root-absent`, with no planner update tick or controller
construction. The source path from `Main.cs` through the controller spellbook
adapter had not changed since `v0.5.4-controller.4`, so that log did not show a
source regression in those paths. The reason UMM's update host did not spawn
on that laptop remains unknown. The earlier null-versus-base-layer theory was
rejected for the inspected Windows 2.1.7b native getter. The alpha's owned
update driver addresses the observed absence of a planner tick; the owner's
functional report supports basic controller use, without supplying a detailed
card/Back/reopen or spellbook trace.

No game installation, save, profile, or setting was changed by this release
workflow. The broad source-only aggregate, guarded native launch, detailed
controller acceptance, and Linux 2.1.5d trial were not run for this stable
package. Next, collect the owner's specific remaining improvements and a log
from 0.6.4 if an entry route fails. Verify card entry/Back/reopen and the
spellbook footer on the installed build before claiming those native cases
accepted; keep subsequent fixes in a separate reviewable change.
