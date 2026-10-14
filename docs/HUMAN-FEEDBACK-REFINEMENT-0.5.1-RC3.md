# Human-feedback refinement 0.5.1-rc3 preparation receipt

## Outcome

The bounded preparation pass is complete. It preserves the four corrections
reviewed on product source `e06d803400893f4cd68d0066ae2421d0085db243`,
the approved native appearance and the HUD button skin.

The responsiveness observer now begins at the casting workspace's common open
request boundary and measures through frame end without calling frame end a
correct rendered frame. The existing guarded casting qualification route now
has a policy-specific shared/Powerful Change recipe that first proves strict
whole-plan refusal, then proves remembered `Cast available` execution through
the same session/planner and HUD policy resolver. The retained runtime-harness
failure is reproduced, classified and corrected without weakening hashing or
`-WhatIf` purity.

This candidate is compiled, focused-tested and packaged. It has **not** been
installed or run in Kingmaker. No new visual, runtime, casting or player-visible
latency PASS is claimed.

## Exact identity and preservation

- Machine: DATA; Windows 10.0.19045; Windows PowerShell 5.1.
- Branch: `codex/kingmaker-buff-planner-human-feedback`.
- Reviewed starting HEAD:
  `90d658d2506184bc09d65989d6145ae066a6b337`.
- Reviewed product source whose four findings remain closed:
  `e06d803400893f4cd68d0066ae2421d0085db243`.
- rc3 packaged product source:
  `debd40674ed2a6ed9f6e3423671f6ec2180bead0`.
- Version: `0.5.1-rc3`; CLR assembly/file version `0.5.1.0`.
- No merge, tag, release, permanent install, game launch or broad history/
  publisher campaign was performed.

Validated local installable archive:

```text
artifacts/local-runtime/0.5.1-rc3/KingmakerBuffPlanner-0.5.1-rc3-local-runtime.zip
ZIP SHA-256: 0d30c7fa430e618be1b7d871c68bdc894b79206018771b17b90ca9b8d3037e62
DLL SHA-256: 6875da1340cae51b6fb12b5c2bccbc94f102a1ab096251ec35346fcedeac7f7a
DLL MVID:    407d6a8b-139f-4800-8ee8-b259578ccdfa
Source:      debd40674ed2a6ed9f6e3423671f6ec2180bead0
```

The adjacent `*.zip.build-local.json` binds those values and records
`validated: true`. The preserved archives remain byte-identical:

- rc1 ZIP: `2e4e1a9fe3fd56eaf11eac39f09132be18c3358ebc9aee45f3b6fad8e8d2ffbc`.
- rc2 ZIP: `6e6a5822c771514981b4bd788398864c56f98df7fabf96e4479ad84cccbcaa27`.

## Responsiveness measurement correction

`OpenCastingWorkspace` captures the timestamp before open admission, discovery,
session binding/load, input-lease acquisition, view construction and spellbook
reconciliation. It carries that timestamp into the first workspace refresh and
then into a correlated end-of-frame observation. The synchronous record keeps
these fields distinct where the existing boundaries permit it:

- `requestToRefreshMs` (cumulative pre-refresh interval);
- `discoveryMs`;
- `sessionBindingAndPersistenceMs`;
- `inputLeaseMs`;
- `viewConstructionMs`;
- `openReconciliationMs`;
- `displaySnapshotMs`;
- `commandAndPersistenceMs`;
- `calculationMs`, `reviewMs`, `regionsMs`, `themeMs`, and `layoutMs`.

The asynchronous record is now named `handlerToFrameEndMs`. It includes the
Unity frame number and a lightweight expected-state observation. For the graph,
that observation requires the still-current graph model and expected banner.
For Resources, it requires the requested scope, the native UI layer and at
least one visible accounting cell. It reports `expected-state-observed` or
`state-not-confirmed`; it does not claim pixel correctness. A newer interaction
records the prior probe as `superseded`, and close/disposal records it as
`cancelled`, so a later refresh cannot silently inherit an earlier start time.

Resources open, reopen and both scope changes keep their incremental path. They
record input snapshot, resource calculation, region rebuild and layout time,
with `themeMs=0` because no full theme/application pass occurs. No full
workspace rebuild was restored.

The retained 67-casting/77-catalogue service fixture remains calculation-only.
The final focused run observed cold calculation 6.508 ms, warm median/slowest
0.806/1.454 ms, buff-switch median/slowest 0.673/0.921 ms, and recipient toggle
including atomic persistence median/slowest 34.217/37.520 ms. These values are
diagnostic only and are not a replacement for the owner-reported one-to-two-
second input-to-visible-result measurement. No additional optimization was
made without a live stage attribution.

## Policy-aware guarded qualification

The new `policy-shared-powerful` recipe extends `live-cast-qual`; it does not
create another executor or catch-and-continue path. Selection produces three
ordered, allowance-bound projections: shared Share + Powerful Change, its plain
same-caster witness, and the `ReadyCastsOnly` affordable shortage subset.

The casting run uses the production session, compiler, execution gate,
conversion, guarded boundary and executor:

1. It authors and saves the shared enhanced casting and witness, executes them,
   then adds one more enhanced casting than the source can fund.
2. With the default `Require full plan`, the normal planner mode is `Ordinary`;
   the partial plan is refused with no dispatch, native spend, enhancement
   spend or effect transition.
3. It changes only the persisted availability policy, reopens the session, and
   compares a canonical authored-intent signature covering routines, order,
   assignments, exact variants, modifiers and enhancements.
4. Both the reopened planner mode and the ordinary HUD resolver read
   `ReadyCastsOnly`. The HUD path calls the same small `CastingRunPolicy`
   resolver/delegator used by the qualification step.
5. The available step recompiles from fresh inputs, submits only the forecast
   affordable casting IDs in authored order, reports every omission/reason,
   and observes complete native plus shared enhancement-pool costs.
6. A second permissive attempt proves `nothing-to-cast`; a combat-active attempt
   proves the global refusal. Neither postcheck submits, spends or changes
   authored intent.

The outcome schema is version 2 for current qualification records and contains
typed policy evidence, per-step omissions and projected per-casting costs. The
launcher reader independently requires strict no-spend availability reads,
ordered confirmed affordable IDs, exact step/policy omission agreement,
non-empty native and enhancement costs, persisted policy, planner/HUD mode
agreement, no-op and combat refusal, and no-spend postchecks. Historical
records remain attributed to their original schema and bytes.

Focused C# coverage includes a clean end-to-end route, a run-bound allowance
missing the affordable projection (refused before any fire/spend), and negative
judgement for lost persistence, unexplained omissions and postcheck spend. The
focused PowerShell reader accepts one complete schema-2 record and rejects six
independent corruptions.

## Runtime-harness failure classification

The retained rc2 failure was reproduced once with the direct focused command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-RuntimeHarness.ps1
```

It failed at fixture bootstrap `-WhatIf`: Windows PowerShell's
`Get-FileHash -LiteralPath` asks the filesystem provider for `ProviderPath`.
With inherited `$WhatIfPreference`, that read was simulated and no hash object
was returned, so the helper's `.Hash` contract failed. `Get-KbpSha256` predates
the human-feedback changes; the matched invocation was valid. Classification:
**pre-existing helper/test-fixture contract defect, not a product regression or
an invalid invocation**.

The correction opens the exact filesystem path read-only and supplies that
stream to the built-in `Get-FileHash -InputStream -Algorithm SHA256`. Real bytes
are still hashed and exactly one nonblank digest is required; `WhatIf` remains
armed for all mutations. The corrected invocation passed **38/0**, including
fixture ownership, failure rollback, recovery, teardown, purity and protected-
save checks. The original failure remains recorded in the rc2 receipt; it is
not relabelled.

## Focused validation

Final focused development command on product source `debd406`:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  .\scripts\Test-Development.ps1 `
  -Only execution-policy,shared-recipes,policy-route,human-feedback `
  -Check PolicyQualificationEvidence
```

Result: source validation 43/0; production build 1/0; selected C# behavior
20/0; independent policy reader 7/0; exit 0. This includes existing execution-
policy and shared-recipe neighbors.

Additional bounded checks:

- `Test-RuntimeHarness.ps1`: 38/0 after the retained failing reproduction.
- `Test-RuntimeLauncherArguments.ps1`: 26/0, including accepted and rejected
  policy recipe binding.
- `Test-RuntimeLauncherFileArguments.ps1`: 8/0.
- `Build-Local.ps1`: source 43/0, build 1/0, strict package validation 4/0,
  local package creation 1/0.
- PowerShell source parse and `git diff --check`: no errors.

The broad `Test-RuntimeLauncherFileWhatIf.ps1`, `Test-SourceOnly.ps1`, restore-
install suite and publisher gate were deliberately not run. The broad wrapper's
cheap opening checks were run through their direct bounded scripts as required.

## UX-01 through UX-06 receipt

| ID | rc3 preparation status | Remaining limitation |
| --- | --- | --- |
| UX-01 Resources | rc2 native-layer/accounting corrections and regressions are preserved; open/reopen/scope timing now requires the expected scope, native layer and visible cells. | Actual opacity, cells, clipping, scroll and input/focus at both resolutions are **NOT_RUN**. |
| UX-02 responsiveness | The true open-request timestamp and separate stages now reach a correlated frame-end expected-state record; Resources has targeted timing. | No actual frame/pixel observation exists. Calculation-only numbers cannot close the reported delay. |
| UX-03 assignment | Continuous normal assignment, inspection-only focus, source selection and explicit one-shot edits remain covered and unchanged. | Actual in-game gestures/hints are **NOT_RUN**. |
| UX-04 draft choices | Same-caster cleanup, direct source transitions, same-source batching and saved-casting preservation remain covered and unchanged. | Actual in-game checkbox/backing-state observation is **NOT_RUN**. |
| UX-05 policy | Product policy tests plus the new guarded strict/permissive driver and independent reader pass offline on rc3 bytes. | Actual Kingmaker dispatch and native resource/effect observations are **NOT_RUN**. |
| UX-06 Run labels | Ordinary casing and the native skin remain unchanged and source/focused-tested. | Actual two-size rendering is **NOT_RUN**. |

New actual candidate screenshots: **none**. The four owner packet images remain
reference input only; no older screenshot is relabelled as rc3 evidence.

## One remaining live-validation batch

Run from a separately owned clean worktree detached at packaged product commit
`debd40674ed2a6ed9f6e3423671f6ec2180bead0`, using the exact rc3 ZIP/manifest.
Before selection, create the established exclusive
`runtime-backups\qualification-frozen\debd406...\FREEZE.json` plus byte-identical
ZIP/manifest copy; re-hash the copied ZIP and its contained DLL. Do not rebuild.

The existing Advanced inspection receipt
`advanced-inspect-20260924-01` **can legitimately serve** the policy cast under
the current guard: it matches Advanced game ID
`cb1f405d-0b0c-4281-8b76-3b4f3b690abb`, WORKING SHA-256
`9787613753f38abf08dceeac8f7a66a6b0b1a5cb28ee0a7cd8805f0f265f1e7d`,
profile `advanced-gunslinger-0136`, and compatibility identity
`088553867688dd3c09a5c5ad593d551682ed0977a5ed5db3f0163d6305fc02b7`.
Re-run the guard immediately before use; any pair/profile drift invalidates
reuse and requires one new `live-advanced-inspect` run. No old product/runtime
receipt is reused for rc3 UI, selection or casting evidence.

Use all five existing inventory targets and the required bounded arguments:

```powershell
$ErrorActionPreference = 'Stop'
. .\scripts\RuntimeHarness.Common.ps1
$game = Get-KbpGamePath
$targets = @(
    $script:KbpRuntimeStateRoot,
    $script:KbpRuntimeStagingRoot,
    $script:KbpRuntimeBackupRoot,
    $script:KbpRuntimeEvidenceRoot,
    (Join-Path $game 'Mods'))
$batch = 'kbphf-rc3-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')

Invoke-KbpLivePurityWindow `
    -Label 'Human feedback rc3 UI and policy selection WhatIf' `
    -Targets $targets -Attempts 1 -LeaseWaitSeconds 120 -Action {
    & .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
        -Scenario live-workspace-native-ui -NativeUiStage qualify `
        -CompatibilityProfileId full-user -FixtureFamily Automation `
        -DisplayMode owner -TimeoutSeconds 600 `
        -RunId ($batch + '-ui-owner-whatif') -WhatIf -Confirm:$false
    & .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
        -Scenario live-workspace-native-ui -NativeUiStage qualify `
        -CompatibilityProfileId full-user -FixtureFamily Automation `
        -DisplayMode windowed-1280x720 -TimeoutSeconds 600 `
        -RunId ($batch + '-ui-720-whatif') -WhatIf -Confirm:$false
    & .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
        -Scenario live-cast-qual-select `
        -QualificationRecipe policy-shared-powerful -ExecutionMode instant `
        -CompatibilityProfileId advanced-gunslinger-0136 -FixtureFamily Advanced `
        -TimeoutSeconds 900 -RunId ($batch + '-policy-select-whatif') `
        -WhatIf -Confirm:$false
}
```

After that window passes, use three different fresh RunIds and run the two UI
observations and policy selection serially. Require clean protected-save and
exact restoration receipts after each:

```powershell
& .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
    -Scenario live-workspace-native-ui -NativeUiStage qualify `
    -CompatibilityProfileId full-user -FixtureFamily Automation `
    -DisplayMode owner -TimeoutSeconds 600 `
    -RunId ($batch + '-ui-owner') -Confirm:$false
& .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
    -Scenario live-workspace-native-ui -NativeUiStage qualify `
    -CompatibilityProfileId full-user -FixtureFamily Automation `
    -DisplayMode windowed-1280x720 -TimeoutSeconds 600 `
    -RunId ($batch + '-ui-720') -Confirm:$false
& .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
    -Scenario live-cast-qual-select `
    -QualificationRecipe policy-shared-powerful -ExecutionMode instant `
    -CompatibilityProfileId advanced-gunslinger-0136 -FixtureFamily Advanced `
    -TimeoutSeconds 900 -RunId ($batch + '-policy-select') -Confirm:$false
```

Mechanically create the cast allowance:

```powershell
& .\scripts\New-KbpRunAllowance.ps1 -Kind qualification `
    -RunId ($batch + '-policy-cast') `
    -SelectionRunId ($batch + '-policy-select') `
    -ExpectedCommit debd40674ed2a6ed9f6e3423671f6ec2180bead0 `
    -FreezeKind qualification-frozen -ExecutionMode instant `
    -ApprovedBy 'Howie Reith' `
    -Authority 'Buff Planner human-feedback rc3 targeted policy execution authorization; no release authorization'
```

Because that run-bound allowance does not exist before selection, it cannot be
honestly covered by the first purity window. Run one separate five-target
window, again with the bounded parameters:

```powershell
Invoke-KbpLivePurityWindow `
    -Label 'Human feedback rc3 policy cast WhatIf' `
    -Targets $targets -Attempts 1 -LeaseWaitSeconds 120 -Action {
    & .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
        -Scenario live-cast-qual `
        -QualificationRecipe policy-shared-powerful -ExecutionMode instant `
        -QualificationAllowancePath `
          ('C:\Dev\KingmakerBuffPlannerLab\approvals\' + $batch + '-policy-cast.json') `
        -CompatibilityProfileId advanced-gunslinger-0136 -FixtureFamily Advanced `
        -TimeoutSeconds 900 -RunId ($batch + '-policy-cast') `
        -WhatIf -Confirm:$false
}
```

Only after it passes, run the matching actual command:

```powershell
& .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
    -Scenario live-cast-qual `
    -QualificationRecipe policy-shared-powerful -ExecutionMode instant `
    -QualificationAllowancePath `
      ('C:\Dev\KingmakerBuffPlannerLab\approvals\' + $batch + '-policy-cast.json') `
    -CompatibilityProfileId advanced-gunslinger-0136 -FixtureFamily Advanced `
    -TimeoutSeconds 900 -RunId ($batch + '-policy-cast') -Confirm:$false
```

Verify every proposed evidence/transaction/staging/backup/approval RunId is
absent before use. The allowance-bound cast `-WhatIf` necessarily uses the same
RunId as its later actual run; purity requires that the `-WhatIf` leave it
absent. Stop on unexpected Steam, account, cloud, update, purchase or credential
UI. Never bypass ownership, fixture binding, run-bound allowance, fresh-input
validation, save comparison or restoration.

Review at owner 1920x1200 and compact 1280x720:

- actual visible Resources accounting cells and useful row text on first open,
  both scope switches and reopen; opacity, clipping, scroll, focus and input;
- successive normal assignment, source-for-next behavior, exact action hints,
  group inspection, explicit one-shot edits and draft reset/preservation;
- ordinary policy and Run captions/native skins;
- request-to-correlated-frame-end records for cold open, buff switch, Resources
  open/reopen/scope, with every synchronous stage retained separately;
- strict zero-submission/no-spend refusal and permissive ordered subset,
  omissions/reasons, saved authored intent, exact native/enhancement/shared-pool
  costs, no-op and combat refusal from the policy record—not a caption.

Complete planned cost is approximately **7 hours 30 minutes**:

- two required five-target purity windows: about 5 h 10 m total (including up
  to 120 seconds lease wait per window and their selected `-WhatIf` actions);
- four bounded live game runs: up to 50 m of launcher deadlines (10 + 10 + 15
  + 15 minutes);
- freeze plus mechanical allowance work: about 10 m;
- explicit restoration/protected-save receipt review after four runs: about
  20 m (restoration execution itself remains inside each launcher lifecycle);
- two-size screenshot, log, timing and policy-record review: about 60 m.

If the reusable Advanced inspection no longer matches, add one guarded
15-minute inspection game-run deadline plus its required selected `-WhatIf`,
restoration and review; do not bypass it. This is the single remaining live
batch. It is a cost estimate, not authorization in this preparation pass and
not a runtime PASS.

## Delivery provenance

`scripts/Test-GuardedPush.ps1` passed 6/0 without mutation. The project-owned
helper then fast-forward pushed and remotely verified the product commit plus
first complete receipt checkpoint at
`d89dd46189d6fdebf000aa0c224ba7486e2e6dec` on
`codex/kingmaker-buff-planner-human-feedback`. This delivery addendum is
documentation-only and does not rebuild or relabel packaged product source
`debd40674ed2a6ed9f6e3423671f6ec2180bead0`, its DLL or ZIP. No main merge,
tag or release was made.
