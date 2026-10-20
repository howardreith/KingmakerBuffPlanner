# 0.6.2 release record

## Published result

[PR #11](https://github.com/howardreith/KingmakerBuffPlanner/pull/11) merged
on 2026-10-20 at 12:10:28 UTC. The isolated, non-fast-forward main integration
preserved both parents `2bc6cc19e3e4a1c5feb1c8cd511d48dfb568606e`
and `474d944c8e2b93343f5b9b0fa52469f5b5dcecea`. The project-owned
guarded main push verified product commit
`6090e6232c9dcec7d8e890585aef8976ef87c70a`. The existing occupied
main worktree was not touched.

[Full release v0.6.2](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.2)
was published on 2026-10-20 at 12:11:41 UTC: Draft=false,
Prerelease=false, GitHub Latest=v0.6.2. Annotated tag object
`37e31c5abbddfa15198ed2e558fcf9ffcdacaf45` peels to the exact product
commit above. The previous 0.6.1-linux.1 prerelease and all older releases
and assets remain unchanged.

| Identity | Value |
| --- | --- |
| Version / CLR | 0.6.2 / 0.6.2.0 |
| ZIP bytes | 924226 |
| ZIP SHA256 | `b9a465691a07d8927b9cf46b66da57167659284282c8da551c483ed3a3dddfb1` |
| DLL SHA256 | `d3cd91b2c76f2f8a4ccefeea77b96aee9ee80dfc16532aee7250d6e43972dcfb` |
| DLL MVID | `127124da-f12c-4919-a9db-8c3441f3e6c8` |
| SHA256SUMS.txt SHA256 | `6a5ff7430b2b971f4ac22b662e9fefc6910068d72ab0ec0c1399419b85fa572a` |

The repository publisher's package is under
`private/release/linux-062-main/artifacts/release/0.6.2/`. Independently
downloaded GitHub assets are under `artifacts/release062-download/`. Their
bytes and hashes match the published asset digests and checksum contents.
The build-generated `release-manifest.json` records its local packaging
step; this receipt records subsequent publication without editing it.

## Commands and evidence

- Clean feature branch: version, manifest, CLR and release notes committed at
  `474d944c8e2b93343f5b9b0fa52469f5b5dcecea`; focused metadata and link
  checks and `git diff --check` passed. `scripts/Test-GuardedPush.ps1`
  **6/0**, then the project-owned guarded helper pushed exact feature HEAD.
- Fresh isolated main clone: `git merge --no-ff` created `6090e62` with no
  conflicts. `scripts/Validate-Source.ps1` **47/0**; guarded main WhatIf
  passed and real push verified exact remote main.
- Clean/pushed main: `scripts/Publish-Release.ps1 -Publish
  -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath
  docs/RELEASE-NOTES-0.6.2.md`. Source **47/0**, complete offline C#
  **579/0**, publisher gate **8/0**. Two clean builds produced identical
  DLL and ZIP hashes; strict package validation **6/0** on each local
  package and the final release ZIP. Publisher duration **50.26 s**. The
  supervised log is `artifacts/linux-2.1.5d/release062-publish.log`.
- GitHub release API and tag refs verified final flags, Latest, annotated
  tag target, asset digests and sizes. `gh release download v0.6.2` into
  `artifacts/release062-download/` independently reproduced both hashes.
  Documentation-only changes after this point do not rebuild or relabel the
  frozen tagged product.

## Boundary and next action

This is an owner-authorized **pre-runtime** release. The version gate now
allows a reported native Linux 2.1.5d game to attempt loading, but no exact
native Linux 2.1.5d installation or assemblies were available. Native Linux
loading, mouse UI, plan persistence, Instant/Animated casts, Stop, and
controller API behavior are **NOT RUN / unverified**. The game and mods were
not staged or launched for this release, and no Windows gameplay regression
run was selected because no C# logic changed from the prior prerelease.
No controller contract absence is inferred. The broad source-only, live
purity, and human acceptance routes were not run; their absence is not a
PASS. No commenter reply or permanent installation was made.

Exact next action: ask a native Linux 2.1.5d user to follow the
[small disposable-campaign trial](LINUX-2.1.5D-CANDIDATE.md) with the exact
0.6.2 ZIP, then inspect the first relevant exception and UMM/Unity logs
before changing APIs or claiming confirmed Linux compatibility.
