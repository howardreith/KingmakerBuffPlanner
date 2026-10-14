# Kingmaker Buff Planner 0.5.2 release receipt (2026-10-14)

The owner explicitly authorized merging the human-feedback branch into the
default branch and publishing a full release, then selected stable version
0.5.2 and requested minimal testing instead of another multi-hour suite.

## Published identity

| Item | Value |
| --- | --- |
| Release | https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.2 |
| State | Published 2026-10-14T19:47:53Z; public; draft=false; prerelease=false; GitHub `releases/latest` |
| Release database ID | `413118463` |
| Feature commit | `2194d0f0f597ebe8b8423f91f8e1fb6b1f65fdb7` |
| Default-branch release commit | `a94e0e586685f8577d32357f289090bc28f1148b` (explicit merge of `5e1e6d2` and `2194d0f`) |
| Annotated tag | `v0.5.2`; object `ba84c15b38191b864ea490dd619771fee1a93983`; peeled commit `a94e0e5` |
| ZIP | `KingmakerBuffPlanner-0.5.2.zip`; 886,501 bytes |
| ZIP SHA-256 | `84dd4b6cfabb84664029d53af0be0740b982dda6f1d800a230ac81d185290d70` |
| DLL SHA-256 | `8c6f6a4198de826a886111f64d33b17683173e8bb979164bf63be12d21f19e82` |
| DLL MVID | `4a2c100a-2b5d-400e-8251-3560f7a3e616` |
| Checksums SHA-256 | `9c6aa5965c192bda1cd91ca3f7affce16614aa9746bb3f2960022414e7fedade` |
| Build-manifest SHA-256 | `eda86e3fb23911b292a134fee6055ca01f801b0fcc6027ce4925d5dd93074df3` |

Durable local installable copy:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\artifacts\release\0.5.2\KingmakerBuffPlanner-0.5.2.zip
```

The release has no prerelease suffix, `Info.json` reports 0.5.2, and GitHub
marks it Latest. It therefore supersedes stable 0.4.3 and preview 0.5.0-rc.1
for ordinary update discovery.

## Integration and minimal validation

The original checkout's old local `main` belonged to a parallel rewritten
history and was checked out in another worktree. It was not reset or modified.
An isolated clean clone of actual `origin/main` was used instead. The feature
was a clean fast-forward descendant of remote main and merged without conflict.
Remote `main` was pushed only through the project-owned guarded helper after a
successful WhatIf; local, remote and release commit all equalled `a94e0e5`.

Selected checks, in order:

- `Test-PublishReleaseGate.ps1`: PASS 3, FAIL 0.
- Feature source validation: PASS 43, FAIL 0.
- `Build-Release.ps1` on exact merge commit `a94e0e5`: two deterministic
  `Build-Local` passes, each source 43/0, build 1/0 and package 4/0; final
  package 4/0; release build 3/0; both ZIP and DLL hashes were identical.
- Post-publication GitHub metadata: public, non-draft, non-prerelease, Latest;
  tag and asset digests agree with the local build.
- Fresh download: checksum text matched the downloaded ZIP; strict package
  validation PASS 4, FAIL 0; packaged version, DLL hash and MVID matched the
  release manifest.

The first strict-validator call against the fresh download was refused because
the file was outside the validator's required repository `artifacts` root.
This was an invalid invocation and the guard behaved correctly. The refusal was
preserved. The same downloaded bytes were copied into a fresh allowed evidence
directory and the one corrected invocation passed 4/4. No hashing, path or
package guard was changed.

At the owner's request, `Test-SourceOnly.ps1`, the multi-hour filesystem purity
windows, and game scenarios were not run for this publication. The guarded
publisher was not invoked because it unconditionally runs that broad aggregate;
its gate was not weakened. The release used the repository release builder,
strict validator, guarded branch pushes, annotated tag, checksummed assets and
independent download verification directly.

## Evidence boundary

Product behavior is the reviewed human-feedback source at
`ec3d5f865707e540b00fccabd3f1a98f04aa3095`, followed only by records and the
stable version/documentation surfaces before the merge. The 0.5.2 build has a
new DLL identity because version and embedded commit metadata changed.

The retained rc5 milestone failed closed before game launch when DATA's
physical display changed during its first purity prerequisite. Accordingly,
0.5.2 does not claim fresh rendered Resources/assignment observations,
input-to-first-correct-frame measurements, or live strict/permissive casting.
Historical 0.5.0 evidence remains attributable only to its own bytes and covers
the unchanged native appearance and HUD. The calculation-only 67-casting probe
does not rule out the reported one-to-two-second populated-campaign delay.

No game launch, installation, Mods transaction, save access, or protected-save
change occurred during this release. No main-history rewrite, existing tag
replacement, prerelease promotion, or release asset replacement occurred.

## Next action

Install the named ZIP through Unity Mod Manager after backing up the complete
existing `Mods\KingmakerBuffPlanner` folder, including `UserSettings`, then use
ordinary play to review Resources, continuous assignment, policy execution and
populated-plan responsiveness. Any defect should begin a new correction thread;
no release-engineering blocker remains.
