# Human-feedback refinement 0.5.1-rc2 review correction

## Outcome

The corrected local candidate resolves all four source-review findings without
changing the approved native appearance or HUD-button skin. Resources rows and
cells now render on the native UI layer; normal assignment keeps inspection
focus separate from one-shot edit intent; a same-caster move from an exact
source back to source selection clears the next-casting choices; and recipient
actions and hints come from one session/domain decision with exact-variant and
group-coverage semantics.

The exact candidate is compiled, focused-tested and packaged. It has **not**
been installed or run in Kingmaker. No runtime, visual, casting or responsiveness
PASS is claimed. The rc1 artifact and its evidence remain unchanged.

## Identity and preservation

- Machine: DATA; Windows 10.0.19045; Windows PowerShell 5.1.
- Branch: `codex/kingmaker-buff-planner-human-feedback`.
- Reviewed starting HEAD:
  `754cf637457bee58744ba47765d52f97639a8da4`.
- Preserved rc1 packaged source:
  `a4c4342e1296966cbae1d64eeb53b47f23265627`.
- Corrected packaged product source:
  `e06d803400893f4cd68d0066ae2421d0085db243`.
- Product version: `0.5.1-rc2`; assembly version `0.5.1.0`.
- No merge, tag, release, upload, permanent install, game launch or publisher
  campaign was performed.

Validated local installable archive:

```text
artifacts/local-runtime/0.5.1-rc2/KingmakerBuffPlanner-0.5.1-rc2-local-runtime.zip
ZIP SHA-256: 6e6a5822c771514981b4bd788398864c56f98df7fabf96e4479ad84cccbcaa27
DLL SHA-256: 695f65d02e8253da81a96238312e32d64489793788ad35dbb5e0c41417354548
DLL MVID:    bfd140ba-1d58-40fb-a71c-4255371b42f2
Source:      e06d803400893f4cd68d0066ae2421d0085db243
```

The adjacent `*.zip.build-local.json` binds those identities and records
`validated: true`. The preserved rc1 ZIP still hashes to
`2e4e1a9fe3fd56eaf11eac39f09132be18c3358ebc9aee45f3b6fad8e8d2ffbc`.

## Four-finding receipt

### 1. Resources row UI layer

`ResourceTableRow` assigns every dynamically rebuilt row to `_uiLayer` before
constructing its cells, and `ResourceCell` explicitly assigns each generated
text object to the same layer. This is a targeted correction; opening Resources
or switching scope does not restore full-workspace rebuilding.

Native evidence schema 6 now performs this sequence: first selected-routine
open, All-routines switch, selected-routine switch back, close, and reopen. It
captures all four states and requires actual active, unculled, viewport-
intersecting `Pool.*` accounting cells with non-empty content and native-layer
rows/cells. Static summaries, panel geometry and scrollbar existence cannot
satisfy the check. This observer is focused-tested but **NOT_RUN**.

### 2. Add focus and continuous assignment

The explicit contract is: a successful add may focus its new casting for
inspection, but it always leaves `GraphEditIntent == None`. Focus never arms an
implicit edit. A normal recipient click therefore continues to add or remove
the displayed exact buff/variant; a normal caster/source click configures the
next assignment. `Change recipient` and `Change caster/source` remain explicit,
one-shot operations.

The schema-6 observer no longer asserts a particular null focus value. It
instead proves two successive normal recipient clicks create two independent
castings, a normal source change affects only the next assignment, neither
gesture leaves edit intent armed, both normal removals work, and explicit
retarget/provider editing is still one-shot. Schema-5 historical records retain
their original interpretation.

### 3. Same-caster source transition

The caster-selection service captures the previous exact provider before
recomputing the next context. Clicking the already selected header for a caster
with multiple sources now transitions from source A to an unresolved exact
source and clears transient enhancements/modifiers in backing draft state.
Selecting source B cannot inherit A-only choices. An unchanged exact context,
including direct same-source reselection, preserves choices for batching.
Previously saved castings keep their owned choices.

Regression coverage includes header A -> unresolved -> B, direct A -> B,
same-source reselection, unchanged-context batching and saved enhanced-casting
preservation.

### 4. Recipient presentation equals action

`CastingWorkspaceSession` now produces one `GraphRecipientDecision` for Add,
Remove, Inspect group coverage, Disambiguate, Retarget/Recentre or Refuse. Both
the graph view model and the click handler consume that decision. Saved
membership is filtered by source, routine, exact ability variant and direct
target/origin before provider reach is considered.

Consequently a removable exact assignment stays enabled even when the current
provider cannot add there; a different variant advertises Add rather than
removing another variant; a covered group beneficiary advertises inspection
rather than another group cast; and duplicate legacy matches require explicit
disambiguation. Unity handlers still render/dispatch the session result and do
not own these business rules.

## UX-01 through UX-06 status

| ID | Corrected candidate status | Remaining limitation |
| --- | --- | --- |
| UX-01 Resources | Source and focused observer checks pass, including native layers and useful visible-cell evidence for first open, two scope switches and reopen. | Actual opacity, cell visibility, scrolling, clipping, focus and input ownership at both resolutions are **NOT_RUN**. |
| UX-02 responsiveness | Calculation caches and stage timings remain; a new end-of-rendered-frame probe reports `inputToFirstCorrectRenderedFrameMs` separately from calculation, persistence, region rebuild, theme and layout. | No Unity rendered-frame sample exists for rc2. Calculation improvements alone do not resolve the reported one-to-two-second delay. |
| UX-03 assignment | Inspection focus is permitted but cannot arm editing; successive add/source-change/remove behavior and explicit one-shot edits pass focused regressions. | In-game gestures/hints are **NOT_RUN**. |
| UX-04 draft choices | Same-caster header and direct source transitions now clear backing draft choices exactly when provider context changes; unchanged context and saved castings are preserved. | In-game checkbox/state observation is **NOT_RUN**. |
| UX-05 policy | Existing strict/permissive planning, persistence, Resources and HUD/planner service tests remain passing. | No live partial-plan execution was performed. A policy caption is not execution evidence. |
| UX-06 Run labels | Ordinary casing and native skin remain covered by focused source/control tests. | Actual two-size rendering is **NOT_RUN**. |

## Responsiveness measurements

The representative fixture contains 67 castings and 77 catalogue entries.
The corrected after figures are the median of five fresh-process run medians;
“slowest” is the maximum sample across those five runs. They are service/
command measurements, not Unity rendered frames.

| Gesture | Original before median | Before slowest | Corrected median | Corrected slowest | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| Cold open/calculation | 66.131 ms | 66.131 ms | 70.274 ms | 71.448 ms | no gain; about 6% slower in process-start noise |
| Warm open/calculation | 1.458 ms | 1.973 ms | 0.805 ms | 2.260 ms | about 45% faster median |
| Buff switch/calculation | 1.528 ms | 1.882 ms | 0.729 ms | 2.197 ms | about 52% faster median |
| Recipient toggle including atomic autosave | 35.034 ms | 45.624 ms | 37.495 ms | 52.347 ms | no gain; about 7% slower median |

One aggregate test-process cold figure of 3.175 ms was excluded because the
process had already warmed the relevant code and is not comparable with the
fresh-process baseline. The retained stage logger separates input,
command+persistence, calculation, review, region rebuilding, theme, layout and
presented refresh. The added end-of-frame line uses the same sequence/cause and
will supply the missing player-visible latency in the live batch. Atomic
synchronous save behavior was not weakened to improve the toggle number.

## Focused validation

Exact post-commit command on product source `e06d803`:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-Development.ps1 -Only 'human-feedback,direct-manipulation,native-ui,graph-'
```

Result: source validation 43 PASS / 0 FAIL; production build 1/0; selected
behavior protocol 47/0; exit 0 in 8.8 seconds. The selection includes the four
reported paths and their graph/native-UI neighbors. Both changed PowerShell
files parse with the built-in parser, and `git diff --check` reports no patch
errors.

Packaging used the unchanged product commit:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Local.ps1
```

Result: source validation 43/0; build 1/0; strict package validation 4/0;
package creation 1/0; exit 0.

Two harness observations are deliberately not promoted to PASS. The broad
`Test-RuntimeLauncherFileWhatIf.ps1` wrapper passed its cheap launcher argument
cases 24/0 and real `-File` argument cases 8/0, then was stopped when it entered
the unchanged multi-hour full-history purity inventory. A focused
`Test-RuntimeHarness.ps1` attempt reached fixture bootstrap `-WhatIf` and failed
closed because the mocked `Get-FileHash` result lacked a `Hash` property. No
safeguard or assertion was weakened, no deployment occurred, and neither result
is runtime evidence for this product.

## Screenshots and runtime boundary

New actual candidate screenshots: **none**. The owner's four 1920x1200 packet
images remain input/reference evidence. Older screenshots are not relabelled.
No Kingmaker process was launched and the candidate was not installed.

## Precise remaining live batch

### A. Native UI and responsiveness

Use a separately owned, clean worktree frozen at product commit `e06d803` with
the exact rc2 package/manifest above. The selected purity invocation must use
one attempt, a 120-second foreign-lease wait and all five existing inventory
targets:

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
$batch = 'kbphf-rc2-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')

Invoke-KbpLivePurityWindow `
    -Label 'Human feedback rc2 selected native UI WhatIf' `
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
}
```

Verify both generated RunIds are absent before invocation and use different,
fresh RunIds for the two actual runs. Continue serially only after each exact
restoration and protected-save receipt passes. At owner 1920x1200 and compact
1280x720 inspect the four Resources captures (selected, all, selected again,
reopened), visible useful `Pool.*` cells, scroll clipping, focus/input ownership,
assignment actions and hints, draft reset/preservation, policy and Run labels.
Correlate each interaction with its `inputToFirstCorrectRenderedFrameMs` line
and keep calculation, persistence, region, theme and layout fields separate.

Current inputs previously counted 374,395 files across the required targets;
a 1,038-file sample took 8.410868 seconds, estimating about 101 minutes for the
two manifests alone. Add up to 120 seconds for the foreign-lease wait and the
two complete launcher `-WhatIf` preflights. Comparable completed five-target
purity windows took 8,735.9–9,235.6 seconds (145.6–153.9 minutes). Reserve about
2 hours 35 minutes for this prerequisite, before either live observation; this
is a cost forecast, not a PASS or deadline.

### B. Targeted policy execution

Run one allowance-bound `live-cast-qual-select` / `live-cast-qual` recipe on
the authorized disposable fixture, with fresh RunIds, the frozen rc2 identity,
the mechanically generated run-bound allowance, protected-save checks and exact
restoration. Its acceptance record must show, from the same saved authored
routine:

1. `Require full plan` refuses the partially unavailable plan with zero native
   submissions and no resource/effect/plan mutation.
2. `Cast available` uses `ReadyCastsOnly`, submits the affordable castings in
   authored order, and reports every omitted casting and reason.
3. The saved assignments, exact variants, modifiers and enhancements are
   unchanged after dispatch; shared pools and complete per-casting costs are
   charged atomically; skipped entries reserve nothing.
4. Already-active/disabled no-op behavior remains truthful, and combat,
   session-lock, disabled-mod and stale/fresh-validation global refusals remain
   whole-run refusals in both policy modes.
5. Planner and HUD actions resolve the same remembered policy.

The existing qualification driver's shared shortage step currently proves only
item 1: it hard-codes `CastingApplyMode.Ordinary`. Therefore its current
`live-cast-qual` record cannot honestly prove items 2–5, and merely observing
the policy caption in batch A is insufficient. Before spending the live budget,
the existing guarded qualification route (not a parallel executor or relaxed
launcher) needs a bounded policy-aware record step and focused driver/reader
checks. That instrumentation limitation is the one remaining independently
actionable validation-harness item; the product policy implementation and rc2
artifact are otherwise complete. Do not run the old strict-only recipe and
label it a Cast-available PASS.

Stop on unexpected Steam, account, cloud, update, purchase or credential UI.
Never substitute older evidence, a protected campaign, a policy-caption
observation or compilation for actual dispatch evidence.

## Final limitations

- All four review findings are corrected and focused-tested on exact rc2 bytes.
- UX-01 through UX-06 remain **NOT_RUN** in Kingmaker on rc2.
- Actual first-correct-rendered-frame latency is unknown until batch A.
- Actual strict/permissive partial-plan dispatch is unknown until the existing
  guarded casting route gains and runs the bounded policy-aware observation.
- The fixture-bootstrap meta-test failure and the deliberately stopped broad
  wrapper are retained as failures/partial attempts, not hidden as PASS.

## Delivery provenance

`scripts/Test-GuardedPush.ps1` passed 6/0 without mutation. The project-owned
helper then fast-forward pushed and remotely verified the correction plus first
complete receipt checkpoint at
`d69e9a3d6b05e26fb82f5fa8d61ffdaf5ff48494` on
`codex/kingmaker-buff-planner-human-feedback`. This delivery record is a later
documentation-only delta; it does not change or relabel packaged product source
`e06d803400893f4cd68d0066ae2421d0085db243`, its DLL or its ZIP. No main merge,
tag or release was made.

