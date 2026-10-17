# Mr. Bard's Kingmaker Buff Planner 0.5.3 release receipt (2026-10-15)

The owner authorized the Mr. Bard branding, the MIT license, merging into the
default branch and a stable 0.5.3 release without a further human playtest.

## Published identity

| Item | Value |
| --- | --- |
| Release | https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.3 |
| State | Published 2026-10-15T22:22:12Z; public; draft=false; prerelease=false; GitHub `releases/latest` |
| Title | `Mr. Bard's Kingmaker Buff Planner v0.5.3` |
| Release commit | `3ec93b49113d8f18de13b78b95bf79eb907a23af` (merge of `ff6cac4` and `0b46bb5`) |
| Annotated tag | `v0.5.3`; peeled commit `3ec93b4` |
| ZIP | `KingmakerBuffPlanner-0.5.3.zip`; 887,387 bytes |
| ZIP SHA-256 | `951311dd97f62b662a5895067ce27c39ef9ade5763d21b33753cb5a4a1988ef3` |
| Entries | `Info.json`, `KingmakerBuffPlanner.dll`, `LICENSE`, `NativeEffectOverrides.json`, `THIRD-PARTY-NOTICES.md` |
| License | MIT, `Copyright (c) 2026 Mr. Bard`; packaged `LICENSE` byte-identical to the repository file |

## Integration

A fresh clone of GitHub `main` (GitHub tag state only; the original
checkout's divergent local tags were not imported) received the branding
branch with `--no-tags`. The branch and `main` were pushed only through the
project-owned guarded helper, each after a WhatIf PASS.

The first publication attempt on merge `ff6cac4` failed closed before any tag
or release: on the owner-authorized path the offline C# suite reported
PASS=543 FAIL=1 because `share-ordinary-discovery-never-arms-the-native-toggle`
inspects `artifacts\build\Release\KingmakerBuffPlanner.dll`, which a fresh
clone had not yet built. `0b46bb5` makes that path compile the exact HEAD with
`Build.ps1` first, as `Test-Development.ps1` does, and pins the order in
`Test-PublishReleaseGate.ps1` (the previous body is rejected). No product,
plan, validator or generated-text change was involved.

## Validation actually run (guarded publisher, owner-authorized path)

- Source validation PASS=47 FAIL=0.
- Production build of `3ec93b4`, then the complete offline C# suite PASS=544 FAIL=0.
- Publisher gate tests PASS=8 FAIL=0.
- `Build-Release.ps1`: two deterministic builds, package validation 6/0,
  release build 3/0, identical ZIP hashes.
- Independent post-publication check: the downloaded asset matched the local
  artifact and `SHA256SUMS.txt` byte-for-byte; packaged manifest keeps Id
  `KingmakerBuffPlanner`, `KingmakerBuffPlanner.dll` and
  `KingmakerBuffPlanner.Main.Load` with the branded DisplayName/Author; the DLL
  reports assembly 0.5.3.0, product `Mr. Bard's Kingmaker Buff Planner` and
  company `Mr. Bard`.

Not run under the owner-authorized exception: `Test-SourceOnly.ps1` (runtime
harness, launcher, deployment, restore and filesystem-purity checks), any game
launch, installation, Mods transaction, save access or human acceptance. 0.5.3
has not been run in the game; 0.5.2's recorded boundaries apply to the
unchanged planner code. Nexus Mods upload was not performed.
