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

The ZIP contains exactly one `KingmakerBuffPlanner/` folder with `Info.json`,
`KingmakerBuffPlanner.dll`, `NativeEffectOverrides.json`, the MIT `LICENSE`
(byte-identical to the repository root file) and `THIRD-PARTY-NOTICES.md`.

## GitHub release publisher

Create a draft GitHub release for review:

```powershell
.\scripts\Publish-Release.ps1
```

The publisher:

1. requires a clean checkout of the fully pushed default branch;
2. verifies the local origin matches the GitHub repository;
3. runs the complete source-only suite (or, for an owner-authorized
   pre-runtime release, the focused offline validation described below);
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

### Owner-authorized pre-runtime release

`-ConfirmHumanAcceptance` asserts that the candidate's human acceptance gates
are complete. When the owner authorizes a public release without that
acceptance, use the separate disposition instead:

```powershell
.\scripts\Publish-Release.ps1 `
  -Publish `
  -ConfirmOwnerAuthorizedPreRuntimeRelease
```

The two switches are mutually exclusive, and the owner switch is valid only
with `-Publish`. This path:

- keeps every default-branch, clean-tree, pushed-HEAD, origin, existing-release,
  tag, deterministic-build and strict package gate;
- replaces `Test-SourceOnly.ps1` with focused offline validation:
  `Validate-Source.ps1`, the complete offline C# behavior suite (read-only
  reflection over the installed game assemblies) and
  `Test-PublishReleaseGate.ps1`. It runs no launcher, runtime harness,
  deployment, restore or filesystem-purity step;
- requires the current section of the release notes (above the first `---`)
  to state the owner authorization and that the release has not been run in
  the game; and
- writes generated release notes that name the checks actually run and state
  that the source-only aggregate, in-game runs and human acceptance were not
  performed.

The plan, notes requirement and generated text live in
`scripts\PublishRelease.Common.ps1` and are covered by
`Test-PublishReleaseGate.ps1`.

This repository may be private. A release in a private repository is available
only to authorized GitHub users. The publisher blocks `-Publish` on a private
repository unless `-AllowPrivateRepositoryRelease` is supplied deliberately;
that switch acknowledges a private release and does not make it public.

Published version tags and assets are permanent project history. Never replace
an existing release ZIP with different bytes; advance the version instead.
