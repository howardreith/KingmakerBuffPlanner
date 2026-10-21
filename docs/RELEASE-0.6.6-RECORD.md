# 0.6.6 release record

Status: **published stable GitHub release** on 2026-10-21 at 01:20 UTC;
GitHub marks it Latest. The owner requested 0.6.6 after PR #14 merged.

## Identity

- Preparation branch: `codex/kingmaker-buff-planner-release-0.6.6`, based on
  merged `main` at `25510dac2229def5c176f110bc0f847228d4a149`.
- Product commit and default-branch HEAD at publication:
  `5b28efefee47448dbf187e8e37c7336314e054d0`.
- Annotated tag: `v0.6.6`, tag object
  `4d15d81ecef5c33f172a2a39c14331cc7557b951`; it peels to the product
  commit. [GitHub release](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.6).
- Installable ZIP: `KingmakerBuffPlanner-0.6.6.zip`, SHA-256
  `3685de56e61124b7456afddc15fcc7e29a478df0517452a84465ea3faeca6c1d`.
- DLL SHA-256:
  `e5a4d2fff453a314247b6ecf8cc9277f136549dae227b3a6365441b196a23b81`;
  assembly MVID: `789f26dd-3b03-4ee2-b746-65f0d17b9982`.
- Downloaded `SHA256SUMS.txt` SHA-256:
  `674cc590b547c7e9159d5e79d55c17f0ffcc6fe13f3911cddbec6979b6f24f5a`.
- Frozen local source and release artifacts: ignored isolated clone
  `private/release/066-stable`, including
  `artifacts/release/0.6.6/release-manifest.json` and independently downloaded
  assets in `artifacts/verification/0.6.6`.

## Checks and publication

- `git diff --check` and the release-note relative link: passed.
  `scripts/Validate-Source.ps1`: **47/0**.
- `scripts/Test-GuardedPush.ps1`: **6/0**. The project-owned guarded helper
  advanced `main` to the product commit.
- The release manifest reports two deterministic builds and package validation.
  The published release is non-draft, non-prerelease and Latest, and names the
  exact product commit.
- Independent checks after publication: `Invoke-KbpOfflineCSharpSuite` built
  the product DLL and passed **597/0** C# tests; the build's DLL SHA-256
  matched the release manifest. `scripts/Test-PublishReleaseGate.ps1` passed
  **8/0**. `scripts/validate-package.ps1` on the independently downloaded ZIP
  passed **6/0**. The downloaded ZIP matched the local ZIP, published asset
  digest and `SHA256SUMS.txt`: **3 matching hashes**.

The first supervised invocation of `scripts/Publish-Release.ps1 -Publish
-ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath
docs/RELEASE-NOTES-0.6.6.md` stopped during offline test build because the
test executable was held open by process 13596. That process had exited when
checked. A subsequent invocation stopped at the publisher's no-replace gate:
the release had appeared meanwhile. No tag or asset was replaced. The
published tag and ZIP match this frozen candidate; the independent checks
above verify the released bytes. The local release manifest's
`publicationStatus=local-only` describes its pre-upload state.

## Native test boundary and next action

This exact ZIP has **not been run in the game** in this workflow. The owner's
next action is the [focused controller test](RELEASE-NOTES-0.6.6.md): hold
Spells focus for six seconds and scroll through a text-only Inspector notice
section, checking that Confirm cannot activate an offscreen button. Capture
the UMM/Unity log and a screenshot or recording for any focus or scroll issue.
The offline checks do not establish rendered or physical-controller acceptance.
