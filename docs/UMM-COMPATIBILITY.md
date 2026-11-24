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
- `scripts/validate-package.ps1` (run by `Build-Local.ps1`, `Build-Release.ps1` and the runtime receipt check)
  works offline on a supplied ZIP. It applies the same pin-definition contract as the build
  (`Get-PinnedLoaderReferences.ps1 -DefinitionOnly`: canonical pin set, compiler pin equal to the declared
  minimum, string manifests), inspects one private snapshot, keeps the exact file allow-list and identity
  checks, requires the packaged `ManagerVersion` to equal the pinned minimum and the packaged DLL's actual
  AssemblyRefs to name exactly the pinned UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1 and no other
  UnityModManager or Harmony assembly, requires the documentation/data entries to be UTF-8 text, and
  reports the snapshot's ZIP digest, DLL digest, MVID and embedded source revision (`BuildInfo.Commit`).
- `scripts/Build-Local.ps1` records in `<zip>.build-local.json` the commit and dirty state read before
  building, a build-input fingerprint (tracked or new files under `src/`, the solution, `Version.props`,
  the pins, verifier, MSBuild guard and build wrappers, plus the inputs MSBuild evaluates for the
  production compile: the exact Csc references including the ignored game assemblies selected through
  `GamePath.props`, source/resource items and compile-affecting properties) that must not change while
  building, and the package/DLL/MVID/revision identities from the validator's snapshot.
- Runtime qualification (`Invoke-KingmakerRuntimeTest.ps1`, via `Read-KbpBuildManifest`) reuses a receipt
  only before staging and only when it is typed correctly, names a clean producing commit equal to HEAD,
  matches the current build-input fingerprint, and agrees with the revalidated package (ZIP digest, DLL
  digest, MVID, embedded revision). A dirty development build is never reused as a frozen receipt.
- Runtime loader identity: each available compatibility profile records the verified loader identity of
  the installation it describes (UnityModManager, 0Harmony12 and the Harmony2 module `0Harmony` that
  0Harmony12 forwards to; currently the owner's UMM 0.33.0 installation). The runtime test refuses to
  start unless the installed files match it, the in-game result now reports the Harmony2 module actually
  bound in the game process, results are admitted only when all three rows match the profile, and the
  compatibility identity used by allowances and earlier-pass admission includes the loader identity
  (earlier allowances must be re-issued).

## Evidence for this change (2026-11-24, round 2)

The product code is frozen at `75aeeff`; later commits change only documentation and committed evidence.
Because `Build.ps1` stamps the current commit into `BuildInfo.Commit`, a build of a later commit (for
example the controller's focused gate) produces different DLL bytes; the committed evidence shows that a
build of the later commit stamped with `75aeeff` reproduces the frozen DLL exactly.

- `scripts/Test-LoaderBaseline.ps1` on `75aeeff` (temporary clone whose path contains a space; each check
  in its own process; this checkout's artifacts untouched): 62/62. In addition to the round-1 cases (cache
  bytes, pin definition and manifests, direct and incremental MSBuild, Csc-input probes, package
  validator, the 0.6.20 regression, same-host validation) it covers offline validation without a cache,
  a missing Harmony12 pin and a UMM pin inconsistent with the minimum, relative cache paths resolved
  from the PowerShell location (corrupt and genuine caches in distinct process and PowerShell
  directories, and a not-yet-created cache), and frozen-receipt reuse: a clean receipt accepted; wrong
  embedded revision, DLL digest, MVID, dirty-flag type or inputs-digest type refused; a second real
  same-version build (dirty) not reused; swapped receipt and swapped archive refused; reference-helper,
  project-file and build-wrapper changes at the same HEAD refused; identical local references relocated
  accepted and one changed byte refused.
- `scripts/Test-RuntimeLoaderIdentity.ps1` (offline): 21/21 - every available profile carries the loader
  identity; malformed rows refused; preflight on a private copy of the installed loader accepts the
  matching identity and refuses a different UMM row, a Harmony2-only mismatch, a swapped file and a
  missing Harmony2; result admission accepts matching rows and refuses a different UMM row, a
  Harmony2-only mismatch, an unloaded Harmony2, a result without Harmony2 fields and a different expected
  Harmony2; the compatibility identity changes when only the loader changes.
- The genuine published 0.6.20 release (ZIP SHA-256
  `54153abb96c9aa5a3e0cb87dc18401dc23aeb47e603b9a69148b5a944668f780`, DLL compiled against
  UnityModManager 0.33.0.0) with only its manifest lowered to 0.31.1 is refused by
  `validate-package.ps1`: "Packaged DLL must reference pinned UnityModManager 0.31.1.0".
- Machine-generated records (structured, no logs; local path prefixes replaced; frozen product
  identities, receipt, validator report, static audit and profile inventory of the frozen DLL, the
  revision-stamp equivalence, suite results and link checks) are committed in
  [`docs/evidence/umm-0311/`](evidence/umm-0311/INDEX.md).
- Static loader API audit of the frozen DLL (into UnityModManager, 0Harmony12 and 0Harmony, by name and
  decoded signature) on 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4 (newest stable on the author's
  NuGet feed, 2026-11-19); see `api-audit.json`. Profiles are component-assembled from official packages,
  not UMM distribution ZIPs. Static evidence only; not a compatibility claim.

| Observation | Result |
| --- | --- |
| Declared minimum | UMM 0.31.1 |
| Verified compile baseline | UnityModManager 0.31.1.0 + 0Harmony12 1.2.0.1 (pinned bytes) |
| Static/API compatibility | Resolved on 0.31.1, 0.32.4, 0.33.0 (owner), 0.33.4 |
| Observed native behaviour | **NOT RUN** on every loader version |

Native runtime is not run for this change: the repository's runtime harness transacts against the
live installation, and running a different loader would replace the owner's UMM. The runtime loader
identity checks above are exercised offline only (preflight on a private copy of the installed files and
result admission on constructed results); no in-game result with the new Harmony2 fields exists yet.
Before any runtime support claim, run settings/language round trip, planner open/close, persisted plans
and a representative cast on a disposable game copy for each loader, on the same candidate DLL, with a
fresh-process reload, recording exact UMM/Harmony identities.
