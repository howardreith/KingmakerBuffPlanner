# 0.6.3 release record

## Published result

[PR #12](https://github.com/howardreith/KingmakerBuffPlanner/pull/12) merged
on 2026-10-20 at 17:30:33 UTC as
`cc2f16512173cdb0e9d7e5501b4ed10264672afd`. The isolated release branch
`codex/kingmaker-buff-planner-release-0.6.3` committed the version, README,
and notes at `59eda0fb3fca75dd3a344070164ee1bc132af2f5`. The non-fast-forward
main integration retained that commit and the PR merge as parents of product
commit `f82555c2c52d2580b187be7006555c855a8862fe`. The project-owned
guarded helper pushed exact main.

[Full release v0.6.3](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.3)
was published on 2026-10-20 at 17:35:34 UTC. It is non-draft,
non-prerelease, and GitHub Latest. Annotated tag object
`a49bd0d0645889b8eb01dafb3a90102a08976477` peels to the product commit
above. The previous releases and their assets were not changed.

| Identity | Value |
| --- | --- |
| Version / CLR | 0.6.3 / 0.6.3.0 |
| ZIP bytes | 926992 |
| ZIP SHA-256 | `ee1ca6c22e684514a0ecd880d4c896160361b04a4484f905cdbe326f653d56c7` |
| DLL SHA-256 | `9d251916f2835119ef4a906b522593d846af5f70043485d94c84e816185165df` |
| DLL MVID | `37f984a3-3112-4806-a759-6d9b4de53eb1` |
| SHA256SUMS.txt SHA-256 | `d13ce9a6c3aa5b40d822042174684d62d411919c9e8467f9eef2819a2c996021` |

The project-owned release package, generated notes, manifest, and supervised
publisher log are under
`private/release/063-main/artifacts/release/0.6.3/` and
`private/release/063-main/artifacts/release063-publish.log` in the lab.
Independently downloaded GitHub assets are under
`private/release/063-main/artifacts/release063-download/`; both asset hashes
match their local counterparts, and the downloaded checksum names the ZIP.
The generated manifest records the local packaging step as `local-only`;
publication was separately verified through GitHub release and tag queries.

## Commands and validation

- The isolated clone began at merged main `cc2f165`. Its first
  `scripts/Validate-Source.ps1` invocation stopped because the fresh clone
  lacked ignored `GamePath.props`. Copying the existing lab setting into the
  isolated checkout allowed source validation to pass **47/0**. No tracked
  file or game installation changed for that recovery.
- Release metadata/link checks **5/0** and `git diff --check` passed.
  `scripts/Test-GuardedPush.ps1` passed **6/0**. The project-owned guarded
  helper pushed the feature commit, then passed `-AllowDefaultBranch -WhatIf`
  and pushed the exact main integration commit.
- From clean, pushed main, `powershell -NoProfile -ExecutionPolicy Bypass
  -File scripts/Publish-Release.ps1 -Publish
  -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath
  docs/RELEASE-NOTES-0.6.3.md` passed source validation **47/0**, the
  complete offline C# suite **582/0**, and publisher gate **8/0**.
  Two clean release builds had identical DLL and ZIP hashes. Strict package
  validation passed **6/0** for each local build and the final release ZIP.
- `gh release view v0.6.3`, GitHub's `releases/latest` API, and local and
  remote annotated-tag refs verified publication flags and the exact commit.
  `gh release download v0.6.3` followed by SHA-256 comparisons verified the
  ZIP, `SHA256SUMS.txt`, and the checksum's ZIP entry (**3/0**).

## Scope, uncertainty, and next action

This is an owner-authorized **pre-runtime** release of the controller
campaign-card repair. The exact 0.6.3 binary has **not been run in the game**.
F02 and the other native controller acceptance cases remain **NOT_RUN**. The
owner's original A-press failure stage has not been observed in a current
native trace. The null-versus-base-layer hypothesis was rejected for the
inspected Windows 2.1.7b managed assembly: its empty-stack getter returns
`BaseLayer`. Native Linux 2.1.5d behavior also remains unverified. The broad
source-only/live-purity aggregate, game launch, installation, save mutation,
and human acceptance were not performed for this release.

Exact next action: with the released ZIP and the repository-owned guarded
runtime workflow, first observe cold controller mode → RT/R2 campaign menu →
Buff Planner card → mapped Confirm/A → release controls → editor visible and
usable → Back → gameplay → repeat, without casting or plan edits. Record
highlight/Back, held/repeated Confirm, a neighboring native card, first and
settled frames, and the `[KBP-CONTROLLER-CARD]` stage sequence. After basic
entry is observed, test active-run card → quick controls → Stop. Stop at the
first failed stage and retain its logs. No successful native behavior is
inferred from this publication.
