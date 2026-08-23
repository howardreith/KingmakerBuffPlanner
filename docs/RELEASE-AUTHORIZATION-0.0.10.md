# Release authorization — Kingmaker Buff Planner 0.0.10

## Decision

The repository owner authorized `0.0.10` for GitHub release on 2026-08-23.
The current feature set and presentation are accepted as sufficient for this
version. Later improvements are permitted but must use a new version and new
release assets.

## Branch state

Every remaining development branch was compared against `main` before this
authorization. None contains a commit absent from `main`; no additional feature
merge is required for this release.

## Qualification basis

The repository records deterministic source, behavior/protocol,
runtime-filesystem, deployment WhatIf, package, native-discovery,
Call-of-the-Wild discovery, Animated execution, Instant execution, Mods
restoration, protected-save, and guarded-install evidence for the product line.
The owner accepts the current post-qualification cast-enhancement and metamagic
rod work for release.

The final published package must be rebuilt and strictly validated from the
exact fully pushed release commit. Historical package hashes must not be reused
for newly built bytes.

## Publication

The guarded release command is:

```powershell
.\scripts\Publish-Release.ps1 `
  -Publish `
  -ConfirmHumanAcceptance `
  -AllowPrivateRepositoryRelease
```

The final switch acknowledges that the repository is private; it does not make
the release public. General public distribution requires changing repository
visibility before publication.
