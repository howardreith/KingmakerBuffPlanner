# Build, package, and publish

## Prerequisites

Release creation is performed on the configured Windows Kingmaker development
machine. It requires:

- the exact local Pathfinder: Kingmaker and Unity Mod Manager references already
  configured through ignored `GamePath.props`;
- Visual Studio/MSBuild and the .NET Framework 4.7 targeting pack;
- Windows PowerShell 5.1 or newer;
- Git and GitHub CLI (`gh`);
- `gh auth login` completed for an account with write access to this repository.

No game, Unity, Unity Mod Manager, Harmony, or third-party mod binary is
committed or uploaded as source.

## Qualified local release build

```powershell
.\scripts\Test-SourceOnly.ps1
.\scripts\Build-Release.ps1
```

`Build-Release.ps1` requires a clean worktree, produces the UMM-installable ZIP
under `artifacts\release\<version>`, builds twice, rejects non-deterministic
package or DLL output, and runs the strict package validator.

## GitHub release publisher

Create a draft GitHub release for review:

```powershell
.\scripts\Publish-Release.ps1
```

The publisher:

1. requires a clean checkout of the fully pushed default branch;
2. verifies the local origin matches the GitHub repository;
3. runs the complete source-only suite;
4. performs two deterministic clean release builds;
5. validates the final UMM ZIP;
6. writes `SHA256SUMS.txt`;
7. creates and pushes an annotated `v<Version.props version>` tag; and
8. uploads the UMM ZIP and checksum as GitHub Release assets.

The automatically generated GitHub **Source code** archives are repository
snapshots, not Unity Mod Manager packages. Users should download the named
`KingmakerBuffPlanner-<version>.zip` asset.

## Public publication gate

Public publication is intentionally explicit:

```powershell
.\scripts\Publish-Release.ps1 `
  -Publish `
  -ConfirmHumanAcceptance
```

Before running that command:

- advance `Version.props` when the current branch contains changes made after
  the previously qualified version;
- update every authoritative version surface together;
- complete the exact current candidate's mechanical and human acceptance gates;
- update `docs\RELEASE-NOTES-DRAFT.md` so it mentions the current version and
  no longer says the candidate is local-only, pending, or not a public release;
- ensure the version tag and GitHub release do not already exist.

Use `-ReleaseNotesPath <path>` to select another notes file.

This repository may be private. A release in a private repository is available
only to authorized GitHub users. The publisher blocks `-Publish` on a private
repository unless `-AllowPrivateRepositoryRelease` is supplied deliberately;
that switch acknowledges a private release and does not make it public.

Published version tags and assets are permanent project history. Never replace
an existing release ZIP with different bytes; advance the version instead.
