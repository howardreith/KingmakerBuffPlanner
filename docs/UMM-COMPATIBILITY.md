# UMM compatibility baseline

## Contract

As requested on 2026-11-23, `src/KingmakerBuffPlanner/Info.json` declares UMM **0.31.1** and
the production DLL is compiled against **UnityModManager 0.31.1.0** and **0Harmony12 1.2.0.1**,
whatever loader is installed on the build machine. Earlier releases declared 0.28.2 but were
compiled against the installed loader (the published 0.6.20 DLL references UnityModManager
0.33.0.0); their records and published packages are unchanged.

- `scripts/loader-reference-pins.json` pins exact upstream bytes and must define exactly these two
  references under their canonical file names:
  - UnityModManager 0.31.1.0 from the author's NuGet package
    (`https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.31.1/unitymodmanager.0.31.1.nupkg`,
    package SHA-256 `05a82b66bc44d56d1246f146d16e9fa8f9a42c450c5bc334bcfa9efddbdb53c9`, entry
    `lib/net35/UnityModManager.dll`, DLL SHA-256
    `2e038ff23ab01d538dfef792096e1f5c04802d1edac3660db20d0d54c167c2c4`);
  - 0Harmony12 1.2.0.1 from newman55/unity-mod-manager commit `10cfb1cc`
    (`lib/Harmony/1.2/0Harmony12.dll`, SHA-256
    `aa1cd48317254985d8b700cc74953477d1b40c3022ce9aa4c95ed2b8327e1292`).
- `scripts/Build.ps1` (used by `Build-Local.ps1`, `Build-Release.ps1` and `Test-Development.ps1`)
  runs `scripts/Get-PinnedLoaderReferences.ps1`, which downloads the references into the ignored
  `artifacts/references` cache and verifies archive hash, DLL hash, assembly identity, the canonical
  pin set and that `Info.json` declares the pinned minimum as a JSON string. A missing, corrupt or
  different file (including the installed newer loader) fails the build; nothing falls back to the
  installed UMM.
- MSBuild builds import `LoaderReferences.targets`: on every build it re-verifies the cache; after
  reference-assembly selection, immediately before compilation, the list Csc receives
  (`@(ReferencePathWithRefAssemblies)`, each loader assembly found by file name or assembly identity)
  must contain exactly the two verified files by full path. The fast up-to-date check is disabled and
  the pin, verifier, guard and manifest are compile inputs. The supported routes are the repository
  scripts and command-line MSBuild; IDE builds are not a supported or verified route.
- `scripts/validate-package.ps1` (run by `Build-Local.ps1` and `Build-Release.ps1`) keeps its exact
  file allow-list and identity checks and now also requires the packaged `ManagerVersion` to be the
  string 0.31.1, the packaged DLL's actual AssemblyRefs to name exactly the pinned UnityModManager
  0.31.1.0 and 0Harmony12 1.2.0.1, and the documentation/data entries to be UTF-8 text. Loader,
  Harmony, game and Unity DLLs cannot be packaged.

## Evidence for this change (2026-11-24)

- `Build.ps1`: DLL references UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1.
- `Build-Local.ps1`: package built and validated (11 checks).
- The genuine published 0.6.20 release (ZIP SHA-256
  `54153abb96c9aa5a3e0cb87dc18401dc23aeb47e603b9a69148b5a944668f780`, DLL compiled against
  UnityModManager 0.33.0.0) with only its manifest lowered to 0.31.1 is refused by
  `validate-package.ps1`: "Packaged DLL must reference pinned UnityModManager 0.31.1.0".
- `Test-Development.ps1 -Only localization`: source validation 47/47, pinned build, 14/14 tests.
- Static loader API audit of the candidate DLL (8 types, 24 members into UnityModManager, 0Harmony12
  and 0Harmony, by name and decoded signature): all resolved on 0.31.1, 0.32.4, the owner's 0.33.0 and
  0.33.4 (newest stable on the author's NuGet feed, 2026-11-19). Profiles are component-assembled from
  official packages, not UMM distribution ZIPs. Static evidence only; not a compatibility claim.

| Observation | Result |
| --- | --- |
| Declared minimum | UMM 0.31.1 |
| Verified compile baseline | UnityModManager 0.31.1.0 + 0Harmony12 1.2.0.1 (pinned bytes) |
| Static/API compatibility | Resolved on 0.31.1, 0.32.4, 0.33.0 (owner), 0.33.4 |
| Observed native behaviour | **NOT RUN** on every loader version |

Native runtime is not run for this change: the repository's runtime harness transacts against the
live installation, and running a different loader would replace the owner's UMM. The runtime
profile plumbing (preflight versus in-process loader identity, Harmony2 identity) is therefore not
exercised by this change. Before any runtime support claim, run settings/language round trip,
planner open/close, persisted plans and a representative cast on a disposable game copy for each
loader, on the same candidate DLL, with a fresh-process reload, recording exact UMM/Harmony
identities.
