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
  building, the compiler inventory and a build-input fingerprint (tracked or new files under `src/`, the
  solution, `Version.props`, the pins, verifier, MSBuild guard and build wrappers, plus that inventory) that
  must not change while building, and the package/DLL/MVID/revision identities from the validator's
  snapshot. The compiler inventory is the exact Csc command line of the production compile, evaluated the
  way `Build.ps1` builds (the solution, the configuration and the commit stamped as `KbpSourceRevision`) with
  a `SkipCompilerExecution` pass through `PrepareResources` and `CoreCompile`. It is kept in argument order:
  first the effective compiler binary (from `CscToolPath`/`CscToolExe`, by content) and, without `/noconfig`,
  the implicit `csc.rsp`; then every option verbatim, references (including the ignored game assemblies
  `GamePath.props` selects), sources including the generated `BuildInfo` (whose content carries the version
  and revision), embedded resources and other file-valued inputs by content. The production compile writes
  its own command line, which must equal the evaluated one entry by entry. Because the commit is stamped into
  the DLL, a receipt is reusable only at its own commit.
- Bounded local configuration: the ignored `GamePath.props` may only set `KingmakerInstallDir` (no conditions,
  imports, items, targets or other properties), and no `Directory.Build.props`/`.targets` outside the
  repository may apply. A setting that only a production compile would see could otherwise change the DLL
  without changing the evaluated inventory, so building and receipt reuse both refuse it. Relocating
  byte-identical game references remains supported.
- Runtime qualification (`Invoke-KingmakerRuntimeTest.ps1`, via `Read-KbpBuildManifest`) reuses a receipt
  only before staging and only when it is typed correctly, names a clean producing commit equal to HEAD,
  matches the current build-input fingerprint (the recorded compiler inventory is compared with the one MSBuild
  resolves now, entry by entry in order), and agrees with the revalidated package (ZIP digest, DLL digest, MVID,
  embedded revision). A dirty development build is never reused as a frozen receipt.
- Runtime loader identity: a run selects one measured loader row (`-LoaderId`: `0.31.1`, `0.32.4`,
  `owner-0.33.0`, `0.33.4`; `compatibility/loaders/`), independently of the optional-mod profile. The launcher
  forwards the row to both of its `Deploy-Local.ps1` calls, and `Enter-KingmakerCompatibilityProfile.ps1` accepts
  and forwards `-LoaderId` (callers that omit it keep the `owner-0.33.0` default). The shared installation
  preflight (launcher, `Deploy-Local.ps1`, the compatibility wrapper, the standalone profile test) refuses to
  stage unless the installed UnityModManager, 0Harmony12 and 0Harmony files match the row; the request carries
  the row (`expectedLoader`); the launcher keeps the decoded request it wrote before launch and checks the result
  against that snapshot, never against a later read of the request file; the in-game result reports the loaded
  UnityModManager, 0Harmony12 and Harmony2 modules and the installed files beside them; results are admitted
  only when loaded and installed identities both equal the row; and the compatibility identity used by
  allowances and earlier-pass admission includes the row (earlier allowances must be re-issued).
- Deployment and release installation stage only a copy they own: the transaction copies the admitted
  package once into its own folder and refuses a copy whose digest differs from the admitted one before
  anything is staged or activated. If entry fails after that copy but before the transaction state exists,
  the transaction removes its own copy (including a partial one) and releases the lock, so the same run ID can
  retry. `Install-Local.ps1` validates a private copy of the release ZIP, reconciles the release manifest's ZIP
  and DLL digests, MVID, embedded revision and its `embeddedCommit` field with it before any state or staging,
  and stages that copy. `Build-Release.ps1` reads both deterministic build receipts while their outputs are
  current; both must name the clean release commit (also as the embedded revision) and their own packages and
  agree with each other, and the validated release ZIP must be byte-identical to them with the same DLL, MVID
  and embedded revision before the release manifest is written.

## Current frozen product (2026-11-24, round 4)

Product code and build tooling are frozen at `8ac5738`; later commits change only documentation and committed
evidence. `Build-Local.ps1` in a dedicated worktree at `8ac5738` built the retained pair:
`KingmakerBuffPlanner-0.6.20-local-runtime.zip`, ZIP SHA-256
`94e9a036d51a996540c9fb20d1ca23319fbc5425e53a0707a7da47c01d559133`, DLL SHA-256
`80e0a1fbee65d2f4dc7cee36a6583875d84c1991e7f5e17095a90189ddcb87cf`, MVID `b12670d5-f94f-49ca-b23d-1514086a1f4b`,
embedded revision `8ac5738`, clean, references UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1 (validator 13/13).
The boundary suite's own clean-clone build of `8ac5738` produced the same ZIP and DLL bytes.

- `scripts/Test-LoaderBaseline.ps1` on a temporary clone of `8ac5738`: 90/90 (`loader-baseline.json`). Beyond the
  earlier cases it covers a production-only local setting and a compiler-selection override (both refused, at build
  and at reuse), reordered and missing compiler entries refused with unchanged and restored controls, the
  transaction's state-write failure (no copy or lock left, same-run-ID retry succeeds), release reconciliation (a
  substituted release copy with the same DLL and different data refused, a dirty producing receipt refused, the
  matching clean pair accepted), the installer's `embeddedCommit` reconciliation, and `Deploy-Local.ps1` and the
  compatibility wrapper on a private game directory with `-WhatIf`.
- `scripts/Test-RuntimeLoaderIdentity.ps1` (offline) on `74a9efc`: 37/37 (`runtime-loader-identity.json`). The
  only later change before `8ac5738` is to `Test-LoaderBaseline.ps1`. For each of the four rows, `Deploy-Local.ps1 -WhatIf`
  and the compatibility wrapper on a private game directory holding that row's loader files clear the loader
  check; a crossed row is refused before staging; the private Mods folders stay empty. The host guard reached
  after a cleared loader check (an unrelated pending lab transaction, or another session's running game) is
  reported separately. The launcher passes
  `-LoaderId` to both deployment calls, and a result for another row is refused against the retained request after
  the request file is changed.
- Revision stamp: `Build.ps1` stamps the building commit into `BuildInfo.Commit`. `retained-frozen-product.json`
  rebuilds `8ac5738` and the documentation commit in one clone, stamped with each revision, and records
  whether the documentation commit stamped with `8ac5738` reproduces the frozen DLL.
- The controller's focused gate builds the candidate commit, so its DLL is stamped with that commit and has its own
  digest (controller receipt); it is not the retained DLL.
- Static loader API audit of the retained DLL `80e0a1fb` (into UnityModManager, 0Harmony12 and 0Harmony, by name and
  decoded signature): resolved on 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4 (newest stable on the author's
  NuGet feed, 2026-11-19), 8 types and 24 members each (`api-audit.json`). Profiles are component-assembled from
  official packages, not UMM distribution ZIPs. Static evidence only; not a compatibility claim.
- Machine-generated records, with the producer of each file, are in
  [`docs/evidence/umm-0311/`](evidence/umm-0311/INDEX.md).

## History (earlier rounds, superseded)

- Round 2 (2026-11-24): product frozen at `75aeeff`; boundary suite 62/62 and runtime loader identity 21/21 on
  `75aeeff`; records committed at `62b65e2`. The genuine published 0.6.20 release (ZIP SHA-256
  `54153abb96c9aa5a3e0cb87dc18401dc23aeb47e603b9a69148b5a944668f780`, DLL compiled against UnityModManager 0.33.0.0)
  with only its manifest lowered to 0.31.1 was refused by `validate-package.ps1` ("Packaged DLL must reference
  pinned UnityModManager 0.31.1.0"); the current suite repeats that case.
- Round 3 (2026-11-24): product frozen at `5cd32d0` (retained pair in its own worktree); boundary suite 71/71 and
  runtime loader identity 23/23; the revision-stamp comparison was measured with `f33b945` as the later commit;
  records committed at `7ad3069`. The round-3 controller gate built its own DLL stamped with its candidate commit.

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
