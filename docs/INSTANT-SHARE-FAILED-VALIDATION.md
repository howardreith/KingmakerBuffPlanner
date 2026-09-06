# Instant Share failed human validation — 2026-09-06

## Diagnostic candidate delivery checkpoint

Product-bearing commit: `de57d90b38711c4c641d470900339bd8815a3fa8`.
Branch: `codex/kingmaker-buff-planner/instant-share-routing-diagnosis`.
Version remains **0.0.19**; this is a separately named local diagnostic build,
not a replacement release or a confirmed casting fix. The paired provider is
the installed/released Gunslinger **0.0.115**, whose contract-v1 acceptance was
executed. No provider update or dependency change is required for this probe.

Candidate ZIP:
`artifacts/instant-share-diagnosis/candidate/KingmakerBuffPlanner-0.0.19-instant-share-diagnostics.zip`.

| Candidate identity | Exact value |
| --- | --- |
| ZIP SHA-256 | ce1f13aac799747b891f4dde5af4593514bf9b464cf4e2d8ce94f616c05a1668 |
| DLL SHA-256 | 9ea78daf5d8914abe9a35c608fea27920bf7d0611cfb384e73ea69077bb3a70a |
| Loaded-candidate MVID expected in the reproduction log | 5541178e-3fb3-4530-b58d-6324de862e7e |
| Build source commit | de57d90b38711c4c641d470900339bd8815a3fa8 |

Final commands and exact counts:

- `scripts/Test-SourceOnly.ps1`: source PASS=42 FAIL=0; behavior
  PASS=150 FAIL=0; harness PASS=8 FAIL=0; package PASS=4 FAIL=0;
  deployment WhatIf PASS=5 FAIL=0; aggregate PASS=1 FAIL=0.
  Evidence: `final-source-tests.log`.
- `scripts/Build.ps1 -Configuration Debug` and Release: PASS=1 FAIL=0 each.
- `scripts/Build-Local.ps1`, twice at the exact product commit:
  Release compile PASS=1 FAIL=0 and package PASS=4 FAIL=0 each;
  local package PASS=1 FAIL=0 each; deterministic builds PASS=2 FAIL=0.
  Evidence: `candidate-package-first.log`, `candidate-package-second.log`,
  `deterministic-package.log`, and `candidate/source-build-manifest.json`.
- `scripts/Test-BrownFurProductionBridge.ps1 -ProductAssemblyPath <candidate DLL> -ProviderAssemblyPath <installed 0.0.115 DLL> -IncompatibleProviderAssemblyPath <preserved actual 0.0.114 DLL> -RequireRoutingDiagnostics`:
  PASS=21 FAIL=0 assertions, PASS=3 FAIL=0 isolated process cases.
  Evidence: `final-package-bridge.log` and `final-package-bridge/`.
- `scripts/Inspect-BrownFurShareContracts.ps1 -AssemblyPath <installed 0.0.115 DLL> -ProductAssemblyPath <candidate DLL>`:
  PASS=87 FAIL=0, evidence `final-metadata-contract.log`.
- `scripts/Test-DeploymentWhatIf.ps1` against the candidate package:
  PASS=5 FAIL=0, evidence `candidate-deployment-whatif.log`.
- `scripts/Test-InstallWhatIf.ps1 -ExpectedPriorVersion 0.0.19` against
  the retained public release manifest: PASS=5 FAIL=0,
  evidence `release-install-whatif.log`. This is release-installer purity,
  not an installation of the candidate.
- `scripts/Test-GuardedPush.ps1`: PASS=6 FAIL=0,
  evidence `guarded-push-whatif.log`.
- `git diff --check`: no whitespace errors.

The installer-purity test initially failed because its old literal prior
version was 0.0.16, while the owner's actual installation is now 0.0.19.
It now accepts an explicit `ExpectedPriorVersion` input, retaining its old
default and all five filesystem/identity assertions. A proposed candidate
manifest input was rejected by the installer's release-root restriction; that
restriction was retained and the unnecessary manifest parameter removed.
Neither rejected invocation staged anything. The candidate instead passed
the existing deployment WhatIf workflow. The documentation mission mirror
also rejected an unsynchronized checkpoint during editing; both required
copies were synchronized byte-for-byte and the unchanged validator passed.

Prior generated runtime release artifacts were preserved and hash-compared at
`preserved-release-runtime/` before rebuilding the same-version local output.
The installed DLLs, other Mods, public release assets and saves were untouched.

These results close the local executable capability/diagnostic gap. They do
not close the missing Unity/Mono cast-routing observation. Exact next action
remains the reproducing-machine log capture described below. Gameplay:
**NOT VERIFIED**.

## Finding and evidence boundary

The reported casting defect remains unresolved. Gameplay is **NOT VERIFIED**.
No affected cast has been observed in the available logs. Do not treat this
diagnostic checkpoint, the previous release, or detached .NET acceptance as a
successful in-game repair.

The local installation on machine **DATA**, Windows profile
`C:/Users/howar`, contains the exact released pair. The real production
`BrownFurDirectCastCompatibility.TryValidateContract` accepts that pair in a
separate .NET Framework process. Real Share and Powerful Change snapshot
builders retain direct capability; the real effective-option resolver chooses
`ProviderDirectRuleCast` after supplied native eligibility/legality inputs.
The older actual provider 0.0.114 is rejected and preserves the native route.

This rules out a stale **local** installation and a detached production bridge
failure for these bytes. It does not establish which assemblies or executor
Howie's reproducing Unity/Mono process used. The reproduction machine remains
unconfirmed; the clarification requested during this investigation has not
been answered. No remote-machine evidence is substituted with local results.

## Exact repository starting point

| Repository | Branch at inspection | Exact HEAD | Product version |
| --- | --- | --- | --- |
| Planner | main, clean | fd0e6dc1c32dfc929a56dbc575163e641b150746 | 0.0.19 |
| Gunslinger | master, clean | dfd551080a1aad38cdd0b19714fbcb12c81ca4ca | 0.0.115 |

Planner work is on
`codex/kingmaker-buff-planner/instant-share-routing-diagnosis`.
The provider repository at
`C:/Dev/KingmakerGunslingerLab/repo/KingmakerGunslinger` was inspected,
including its AGENTS.md and provider-owned direct transaction. It was not
edited, rebuilt, merged, staged, or released. No histories were reset.

The owner authorizes final merge/push/release after the issue is fixed.
A diagnostic-only checkpoint does not satisfy that condition. No default
branch merge, new public release, or replacement of v0.0.19 assets is claimed
or justified here. A local diagnostic candidate and focused branch checkpoint
are reviewable independently.

## Actual installation

UMM's installer log records this selected game root and successful unpacking
of both release ZIPs at 13:13 local time on 2026-09-06:

`C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker`

This agrees with GamePath.props and the only Kingmaker entry in the local
Steam library configuration. Doorstop points at
`Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll`, not a guessed
game-root UnityModManager folder. The installer ran from
`C:/Users/howar/OneDrive/Desktop/UnityModManager/UnityModManager.exe`.

Paths below are relative to that exact game root.

| Product | Info.json version | DLL path | Assembly identity |
| --- | --- | --- | --- |
| Planner | 0.0.19 | Mods/KingmakerBuffPlanner/KingmakerBuffPlanner.dll | KingmakerBuffPlanner, Version=0.0.19.0, Culture=neutral, PublicKeyToken=null |
| Provider | 0.0.115 | Mods/KingmakerGunslinger/KingmakerGunslinger.dll | KingmakerGunslinger, Version=0.0.115.0, Culture=neutral, PublicKeyToken=null |

| Product | Installed SHA-256, also exact documented released SHA-256 | Installed MVID, also exact documented released MVID |
| --- | --- | --- |
| Planner | 31f9a604c4d2bb7c048b7db470373b8d508c29471cca95ed9bc9adc7149e2d37 | 56444044-8eeb-46c4-b9ec-f33c5ad8a65f |
| Provider | f93ddb0375fa37f266855579eee948fc35d1987d0ddf63247f0bbe31cf0dfa65 | 88a6a42a-8648-4a7a-a0fa-4dd567701986 |

The provider DLL contains the public direct-cast API. Inventory found one
matching mod entry and one product DLL for each product under this Mods root.
UMM's installer cache is outside Mods and does not prove a runtime duplicate.
No Kingmaker process was present at initial inspection or diagnostic capture.
UMM was initially running and later closed independently; the agent did not
close either program. On-disk identities are not running-process identities.

## Existing logs and first divergence

There is no Owlcat Kingmaker directory under this local Windows profile.
The collector also found no game-root, managed-UMM, or local-profile game
output log at the checked paths. The available UMM **installer** log proves
installation operations, not gameplay or loaded assembly identity. It contains
no affected routine to analyze.

Consequently the configured mode, actual caster/source/recipient/selected
enhancements, runtime capability rejection, native callback result, and
provider Fire/Begin/commit entry of the human failure are all **unobserved**.
There is no evidence-supported first divergence or confirmed gameplay root
cause yet.

The earliest executable observation is instead:

```text
Released pair: Production TryValidateContract: accepted=True; reason=
Older 0.0.114: accepted=False; reason=provider-direct-api-type-mismatch
Missing provider: accepted=False; reason=provider-direct-assembly-count-0
```

The original exact game type expectations remain intact. Expected and actual
AbilityData, TargetWrapper and RuleCastSpell assembly-qualified identities are
recorded in each probe log. The local Assembly-CSharp MVID is
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. No type-name-only matching was added.

Rejected or still unproven theories:

- Stale local release pair: rejected by both installed hashes and MVIDs.
- Production resolver necessarily rejects the released API: rejected for this
  actual paired .NET process.
- Snapshot reconstruction necessarily loses the direct marker: rejected for
  the actual Share/Powerful snapshot builders and post-eligibility policy.
- Unity/Mono type identity, a different installed pair, selected native
  enhancement/callback override, or direct-path-generated processes in the
  reproducing game: still unproven; none is called the root cause.
- A planned direct strategy proves Instant execution: false. The Hybrid
  callback can still select Animated; the new regression demonstrates this
  distinction without asserting it happened in Howie's game.

## Minimal changes

- `scripts/Test-BrownFurProductionBridge.ps1` and
  `tests/ProductionBridge/BrownFurProductionBridgeProbe.cs` execute the actual
  product DLL's resolver, private production snapshot builders, and public
  effective-option resolver in fresh controlled processes. Actual current,
  incompatible, and missing providers are mandatory cases, not skipped passes.
  No game starts or mod entry points execute.
- `HybridCastExecutor` records `ExecutorSelected` and invokes an observer
  before entering the selected executor. The record distinguishes planned
  strategy, actual selected executor, native callback, native strategy,
  legacy callback, fallback strategy, and fallback permission. Selection logic
  and callback evaluation semantics remain unchanged.
- `ShareCastDiagnostics` records the real capability result, provider version,
  loaded product identities/MVIDs/locations and exact expected/actual type
  identities on rejection. All foreign assembly/type names remain within
  `BrownFurShareTransmutationCompatibility.cs`; the existing boundary validator
  was retained unchanged. Diagnostics inspect already-loaded assemblies only.
- `KingmakerInstantCastAdapter` emits bounded UTC phase records at Fire,
  Begin, accepted reservation, RuleCastSpell, and completed provider rule.
  Diagnostic failures cannot alter source/provider spending.
- `PlannerUiSession` emits the decisive route immediately and shows a
  player-facing animated-fallback warning, including provider version and
  exact capability failure when relevant. Successful effect confirmation is
  retained; `QuickExecutionResult.CompletedWithFallback` and
  `UsedAnimatedFallback` separate it from satisfaction of Instant mode.
- `scripts/Collect-ShareCastDiagnostics.ps1` copies logs and Info.json files
  and records file hashes, metadata, process paths and duplicate DLL candidates.
  It reads the explicitly supplied game root, preserves originals, and never
  reads or loads campaign saves.

The original direct transaction, RuleCastSpell execution, native eligibility,
target legality, reservoir debit, source spending, enhancement activation and
cleanup policies were not rewritten.

## Focused verification and evidence

All raw machine-local evidence is ignored under
`artifacts/instant-share-diagnosis/`.

| Command / evidence | Result |
| --- | --- |
| Initial installed-pair executable probe, initial-probe.log | accepted=True, exact released hashes/MVIDs |
| Test-BrownFurProductionBridge.ps1 against installed release pair, released-bridge/ | 3/3 process cases; 15/15 assertions; FAIL=0 |
| Same runner against diagnostic candidate with -RequireRoutingDiagnostics, candidate-bridge/ | 3/3 process cases; 21/21 assertions; FAIL=0 |
| Test-SourceOnly.ps1, source-tests.log | source 42/42, behavior 150/150, harness 8/8, package 4/4, deployment WhatIf 5/5, aggregate 1/1; FAIL=0 |
| Build.ps1 -Configuration Release, candidate-build.log | exact installed-reference Release compile 1/1 |
| Collect-ShareCastDiagnostics.ps1, local-capture-with-umm/ | current-disk pair and original installer-log copy; gameplay not inferred |
| Get-KbpDisposableSavePair, save-preflight.log | The exact Kingmaker save root is unavailable. |

The older rejection input is the preserved actual 0.0.114 build at
`C:/Dev/KingmakerGunslingerLab/repo/KingmakerGunslinger/artifacts/local-runtime/0.0.114/exact-build/KingmakerGunslinger/KingmakerGunslinger.dll`.
Its SHA-256 is
`6cbcb6b1de2a508d3cb366b2ac8ab3dc9947f6ab1e4bb0ad36240e7cd71b4e79`;
MVID `0a345819-a6e4-461c-bf29-917f97de72db`.

The new behavior regressions prove pre-cast routing evidence survives
cancellation and that native-strategy versus callback fallback is distinct.
They also prove confirmed effects retain their counts without reporting
ordinary Completed when Instant used Animated.

The executable composition test supplies eligibility/target legality inputs;
it does not exercise live KingmakerShareTargetingModifier, live unit discovery,
UI Refresh, RuleCastSpell or gameplay. In particular, the combined snapshot
test does not claim Resinous Skin is eligible for Powerful Change. A separately
eligible actual spell must be used for that gameplay test.

During development the unchanged source validator rejected foreign provider
names outside the bounded adapter; the names were moved behind the existing
adapter boundary. One test compile then rejected an unqualified IEnumerator;
the test was corrected to System.Collections.IEnumerator. No failing gate,
warning, assertion, package allowlist or runtime guard was weakened. Early
PowerShell Core reflection-only and local sandbox/tool startup failures are
not test passes; the final executable probe runs on .NET Framework
4.0.30319.42000 in its own process.

## Exact next action and manual acceptance

Obtain one affected cast log from the machine where failure occurs. With the
diagnostic candidate loaded there, confirm the loaded planner MVID and provider
0.0.115 identity, then examine, in order:

1. `[KBP-DIRECT-CAPABILITY]` and `[KBP-LOADED-ASSEMBLY]`.
2. `[KBP-ENHANCEMENT-OPTION]`, `[KBP-SHARE-TARGETING]`, and `Routine plan:`.
3. The pre-cast `[KBP-ROUTE]` record, including selected and native enhancements.
4. `[KBP-PROVIDER-DIRECT]` Fire/Begin/rule/commit-status records, if entered.

If actual Instant/Fire is observed with the normal command delay, narrow the
next investigation to that exact ability's generated command/effect process.
Do not infer a direct-path bug from the planned strategy or visual particles.

After reproducing on that machine, run from its repository checkout:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Collect-ShareCastDiagnostics.ps1 -GameRoot '<actual game root>'
```

Return the generated `capture.json` and copied game log(s). This collector can
also capture an existing failed-release log before installing a candidate.
It does not start the game or select a save.

Autonomous gameplay remains unavailable: no protected KBP save pair exists.
Do not request it again or substitute an ordinary save. Human testing should
check Instant Share Resinous Skin on four distinct allies, no normal casting
delay, individual effects and exact spell/reservoir costs, targets three and
four, then an ordinary buff. Test an actually eligible Share plus Powerful
Change spell separately and preserve deliberate Animated/manual casting.

Gameplay status: **NOT VERIFIED**. The next gameplay repair and any release
must be driven by that actual routing discriminator.
