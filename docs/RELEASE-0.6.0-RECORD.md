# 0.6.0 release record

## Published result

[Full release0.6.0](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.0)
published2026-10-18T02:52:33Z; Draft=false, Prerelease=false, GitHub Latest=v0.6.0.
[PR10](https://github.com/howardreith/KingmakerBuffPlanner/pull/10) is MERGED,
mergedAt02:51:49Z. Local non-fast-forward integration preserved both parents:
main dcf718fd7b70cf453dd25aa693fefc6aa503d826 and feature
7e08f8f23d127253aabdb98fa3be26333cf37946. Guarded main push verified exact
product **c98b43446c13534a2d165b6ca18a264602957758**. Annotated tag object
0c4aa17b1278f076f0aae985f164fabe5ad0313a peels to that exact product, not a
subsequent documentation commit. Old releases/tags/assets unchanged.

| Identity | Value |
| --- | --- |
| Version / CLR | 0.6.0 / 0.6.0.0 |
| ZIP bytes | 924222 |
| ZIP SHA256 | 54a7b98535e2127e09200fc7ea330de7bfc7569e05834ab3e210066619921dfa |
| DLL SHA256 | 240aeaa8c699a4ca2f1c651943619233815256dbbab97522c8edfe9bd8125da2 |
| DLL MVID | a6339ffd-16b2-4126-a7e3-43eda0fd32d4 |
| SHA256SUMS.txt SHA256 | 9ad7a2d78b9257774fe5a029967383cf466e1296321bf75fa90308d75b92bc7d |

Both published assets were downloaded after publication. ZIP hash, checksum
contents and asset digests match; final release flags, Latest and remote peeled
tag verified. Local artifacts are under
private/release/KingmakerBuffPlanner-0.6.0/artifacts/release/0.6.0;
downloaded copies under artifacts/release060-download. The build-generated
release-manifest still records its historical local-only build step; this receipt
and GitHub verification record the later publication, without editing that manifest.

## Commands and evidence

- Before merge at7e08f8f: Invoke-KbpReleaseValidation with OwnerAuthorizedPreRuntime;
  source47/0, production build1/0, complete offline C#579/0, publisher8/0,
  measured33.7353412s. Log artifacts/controller-alpha4/release060-premerge.log.
- Changed-document local links157/0; git diff --check clean. Guarded push helper
  tests6/0; feature push WhatIf then exact guarded fast-forward5276b21 ->7e08f8f.
- Fresh isolated main clone (no reset/shared-object hard links); fetch verified
  exact feature head, git merge --no-ff, guarded -AllowDefaultBranch WhatIf and
  real push dcf718f ->c98b434. Existing occupied main worktree untouched.
- On clean/pushed main c98b434:
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.0.md`.
  Source47/0, complete offline C#579/0, publisher8/0. Two clean builds produced
  identical DLL/ZIP hashes; release assertions3/0, every strict package pass6/0.
  Publication log artifacts/controller-alpha4/release060-publish.log spans
 02:51:53Z–02:52:34Z (~41s filesystem timestamps, not a stopwatch measurement).
- gh release view/API Latest, git ls-remote main/tag, asset download and SHA256
  checks verified publication. No full broad source aggregate PASS is asserted.

Disposition remains OwnerAuthorizedPreRuntime. Exact0.6.0 has not been run in the
game; F01-F10 still NOT_RUN. No game launch/staging, permanent installation,
save/profile/settings mutation or unrelated-mod change; native restoration NOT_RUN.
The existing Alpha4 full-history safety process remains separate and was not
stopped/replaced or promoted to evidence for0.6.0. Subsequent receipt-only commits
do not change/rebuild/retag the published product.

## Preparation checkpoint (historical)

Preparation checkpoint: owner explicitly authorizes merging PR10/controller work
into the default branch, pushing it and publishing a full release with a new
version. Selected0.6.0 / CLR0.6.0.0; prior tags/assets remain immutable.

Disposition: OwnerAuthorizedPreRuntime, using the established unchanged guarded
publisher, not ConfirmHumanAcceptance. Native F01-F10 remain NOT_RUN; publication
does not complete the controller mission. No gameplay-mod/installation/save work.

Preserve the active Alpha4 safety process26124/exec83416 and frozen69a48a4; its
eventual outcome is separate. No repeated full-history inventory is selected by
this release route. Pre-merge validation and final product/DLL/ZIP/MVID identities
are recorded after their actual completion, not inferred from an earlier alpha.

Default branch main has a legitimate existing local worktree. Release integration
therefore uses an isolated clone under private/release, without resetting or
checking out that occupied worktree. Feature and main pushes use the project-owned
guarded helper; local non-fast-forward merge preserves feature history. The final
publisher requires clean main exactly matching origin/main and an unused tag.
