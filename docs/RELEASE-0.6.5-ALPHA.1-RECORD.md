# 0.6.5-alpha.1 release record

Status: **published GitHub prerelease** on 2026-10-21 at approximately
01:11 UTC. Stable 0.6.4 remains GitHub Latest. The owner requested this alpha
to test the controller navigation and focus repair from PR #14.

## Identity

- Branch: `codex/kingmaker-buff-planner-controller-navigation-review` (PR #14).
- Product/tag commit: `1f5d1af5c8ea3f92bc41e0e3165ecef57cdf5341`.
- Annotated tag object: `ed8ad1f312d529706d115bad3a947123a216bbe9`;
  tag: `v0.6.5-alpha.1`.
- Version: `0.6.5-alpha.1`; [published prerelease](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.5-alpha.1).
- UMM ZIP: `KingmakerBuffPlanner-0.6.5-alpha.1.zip`, SHA-256
  `6d8e4985dbecb7355fff4a73aea16f284a17ab60b91a1cbf917aa273ec8f0cd1`.
- DLL SHA-256:
  `b39620ed4f71e303e19acfd5211d8765176781c095837a4ef09e213ced7798f0`;
  assembly MVID: `ba9b2683-8edc-4c3b-8032-6a0847086a85`.
- Checksum asset SHA-256:
  `f5aeb8e052631700eab3e6c41bcc092e325bdaf040ef0969189ac36aece6b8b0`.
- Frozen local source and artifacts:
  `private/release/065-alpha-controller-navigation` (ignored isolated clone),
  with `artifacts/release/0.6.5-alpha.1/release-manifest.json`, ZIP,
  `SHA256SUMS.txt`, and
  `artifacts/validation/publish-0.6.5-alpha.1.log`.

## Validation and publication

- Alpha version, release-note disposition text, relative links and
  `git diff --check`: passed. `scripts/Validate-Source.ps1`: **47/0**.
- `scripts/Test-GuardedPush.ps1`: **6/0**. The project-owned guarded helper
  pushed the exact product commit before release publication.
- `scripts/Publish-Release.ps1 -Publish
  -ConfirmOwnerAuthorizedPreRuntimeRelease -AllowFeatureBranchPrerelease
  -ReleaseNotesPath docs/RELEASE-NOTES-0.6.5-ALPHA.1.md`: source **47/0**,
  complete offline C# suite **597/0**, publisher gate **8/0**, two matching
  deterministic release builds, strict package **6/0**. The publisher created
  and pushed the annotated tag, then published the prerelease and checksum.
- Independent GitHub verification: `isDraft=false`, `isPrerelease=true`, tag
  peels to the exact product commit, and the downloaded ZIP's SHA-256 matches
  the local ZIP and published checksum: **2/0**. Stable v0.6.4 remains Latest.

The first publisher invocation in the long-lived review worktree stopped
before validation or publication because historical local tag objects differ
from GitHub's. No tags were deleted or overwritten. The successful invocation
used a clean isolated clone of the already pushed alpha source, with the
existing machine-local game assembly path supplied through ignored
`GamePath.props`. The local release manifest's `publicationStatus=local-only`
describes the package before upload; GitHub's release is published.

## Native test boundary and next action

This exact ZIP has **not been run in the game**. No game installation, save,
profile, or setting was changed by publication. The owner's next test is the
[focused controller sequence](RELEASE-NOTES-0.6.5-ALPHA.1.md): hold focus on
Spells for six seconds; traverse filters and columns; and scroll through an
Inspector notice section with no visible button. Record the candidate ZIP
hash, a screenshot or recording if focus disappears or scroll reverses, and
the matching UMM/Unity log. Do not treat the offline suite or earlier video as
native visual acceptance. The stable-release and PR merge decisions remain
separate from this prerelease.
