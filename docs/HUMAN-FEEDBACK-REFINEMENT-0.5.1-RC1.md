# Human-feedback refinement 0.5.1-rc1

## Outcome

The 0.5.1-rc1 local candidate implements UX-01 through UX-06 in source. A
player can keep one buff open while adding or removing several recipients,
choose a different caster/source for the next assignment without pressing
Done, inspect an existing casting without arming an implicit retarget, open a
structured Resources dialog, choose whether unavailable castings block a run,
and read ordinary-cased Run Long / Run Important / Run Short controls.

The exact candidate is compiled, focused-tested and packaged. It has **not**
been installed or run in Kingmaker. Consequently this is not a runtime or
visual PASS, and no new screenshot is attributed to it. The mandatory live
purity prerequisite is estimated at about 101 minutes for its two filesystem
inventories alone, beyond the ordinary 30-minute validation ceiling. The one
remaining guarded batch is specified below.

## Identity and preservation

- Machine: DATA; Windows 10.0.19045; Windows PowerShell 5.1.
- Branch: `codex/kingmaker-buff-planner-human-feedback`.
- Integrated baseline: `origin/main` at
  `5e1e6d22c9cc03826f867ee9e068089fb06521b4`.
- Main implementation commit:
  `5688cc5762ba6aead3f23a60d14180de329c841f`.
- Runtime-observer correction and exact packaged product commit:
  `a4c4342e1296966cbae1d64eeb53b47f23265627`.
- Product version: `0.5.1-rc1`; assembly version `0.5.1.0`.
- The public 0.5.0 prerelease, previous candidates, local installations,
  protected saves, runtime evidence and unrelated work were not changed.
- No merge to main, tag, release upload, public publication, permanent install,
  game launch or publisher campaign was performed.

Validated local installable archive:

```text
artifacts/local-runtime/0.5.1-rc1/KingmakerBuffPlanner-0.5.1-rc1-local-runtime.zip
ZIP SHA-256: 2e4e1a9fe3fd56eaf11eac39f09132be18c3358ebc9aee45f3b6fad8e8d2ffbc
DLL SHA-256: bdc1aa3c512ba884a6c8b4476f4c92ea9362dec794b7fb1addf95f07288af10e
DLL MVID:    e21f2f39-0305-4bb3-aa02-067f4ac78252
Source:      a4c4342e1296966cbae1d64eeb53b47f23265627
```

The adjacent `*.zip.build-local.json` binds those identities and records
`validated: true`.

## Packet verification

The exact archive was read from
`C:\Dev\KingmakerBuffPlannerLab\handoff\KBP_Human_Feedback_Refinement.zip`.
Its SHA-256 is
`60fd5af6b1ac9b25551a9c4d8daff4724158614cd74e8086e8f3363291562c56`.
Before extraction, all nine ZIP entries were enumerated: one
`KBP_HUMAN_FEEDBACK_REFINEMENT` root, zero absolute/traversal/drive paths,
zero symlink-like entries and zero duplicate destinations. It was extracted
without overwrite into the ignored reference directory:

```text
artifacts/reference-packets/human-feedback-refinement-20261013/
  KBP_HUMAN_FEEDBACK_REFINEMENT/
```

Built-in SHA-256 verification matched all eight payloads in
`SHA256SUMS.txt` (8 PASS, 0 FAIL). `MISSION.md`, `OWNER_FEEDBACK.md` and
`SOURCE_MAP.md` were read. All four 1920x1200 reference PNGs were viewed at
original detail: the initial/incomplete Resources view, overflowing Resources
text, Seamantle caster handoff and Resinous Skin/Share batch. They are owner
input evidence only, not screenshots of this candidate.

## UX receipt

| ID | Source/focused result | Runtime/visual status |
| --- | --- | --- |
| UX-01 Resources | Implemented a topmost modal with native parchment/frame, dimming blocker, Close focus, Escape/outside/peer-window dismissal, focus restoration, selected-routine/all-routines scope, clipped scroll table and fixed ellipsized columns. Accounting is grouped by caster/shared owner and reports Resource, Available now, Saved need, Would spend and Short under the current policy. Opening and switching scope are read-only and do not rebuild the whole workspace. | **NOT_RUN.** Schema-5 guarded observation now requires `nui-resources.png` and `nui-resources-all.png`, plus bounds, topmost, focus, clipping, save-state and unchanged-plan evidence. Actual opacity, overflow and first-correct-frame behavior still require the two-size batch. |
| UX-02 responsiveness | Added stage timings for discovery/snapshot, command+persistence, graph/review, region rebuilding, theme, layout and total refresh. One immutable display snapshot is reused while the view is open. Compilation and forecast caches are keyed by authoring identity/revision, exact input snapshot and execution mode; run preflight still refreshes and captures authoritative inputs. Warm calculation medians improved substantially. | Calculation timing **PASS**; actual Unity first-correct-rendered-frame timing **NOT_RUN**. Recipient toggles remain dominated by the synchronous transactional autosave and were intentionally not weakened. |
| UX-03 continuous assignment | Normal caster/source clicks configure the next casting. Normal recipient clicks add/remove the exact buff/variant in the selected routine. Adding does not focus the new casting. Inspection is separate; explicit one-shot Change recipient and Change caster/source retain individual editing. Ambiguous legacy duplicates require explicit card selection; exhausted providers do not prevent removal. Covered group members focus the one origin instead of creating duplicate area casts. | Exact Seamantle and Resinous/Share service regressions **PASS**. The guarded observer was updated from its obsolete implicit-retarget assumption and is compiled/focused-tested, but has not run in game. |
| UX-04 draft choices | Draft modifier/enhancement state is scoped to buff/variant, caster, exact provider/spellbook and routine. It persists across same-context recipient batching, refresh and inspection, and clears in backing state when that context changes. Saved castings retain their own choices. | Felix Resinous/Share batching, Protection from Evil/Sayan navigation, non-Share provider, remove/Undo and reload regressions **PASS**; live UI observation **NOT_RUN**. |
| UX-05 availability policy | Added profile-scoped `Require full plan` / `Cast available`. Missing, old or malformed values normalize to strict. Planner and HUD use the session policy through the existing `CastingExecutionGate`, `ReadyCastsOnly`, omissions, compiler, coordinator and executor. Fresh execution validation, authored order, shared pools, atomic modifiers/costs, no-op truthfulness and global refusals remain intact. | Persistence, strict/permissive allocation, shared-budget order, costs, combat/global refusal, planner/HUD agreement and no-op regressions **PASS**; actual game dispatch **NOT_RUN**. |
| UX-06 Run label | The Run control now uses ordinary native button typography, with `Run ` plus the routine's preserved display casing; forced Action/uppercase styling was removed only from this control. | Source/control contract **PASS**; actual 1920x1200 and compact rendering **NOT_RUN**. |

### Required gesture regressions

- Seamantle: Felix Arcanist -> Felix, then Leinna Wizard -> Leinna produces
  two independent self-casts without Done, provider mutation or reopening the
  buff. Removing Leinna is provider-independent and Undo restores it.
- Resinous Skin: Felix Arcanist + Share -> Tias -> Raine produces two Felix
  castings with Share and its cost. Clicking Tias removes only Tias; Raine
  remains; Undo and reload restore the exact saved intent.
- Draft lifetime: Share survives the unchanged Felix/Resinous context, then
  clears both visibly and in backing state for Sayan/Protection from Evil or a
  different non-Share provider. Previously saved Share castings are unchanged.

## Responsiveness measurements

The deterministic representative fixture contains 67 castings, 77 catalogue
entries and multiple providers. Times below are calculation/command completion
in milliseconds; they are not invented Unity rendered-frame measurements.
The after column is the median of three isolated runs; “slowest” is the maximum
sample observed across those runs. Measurements were taken on implementation
commit `5688cc5`; `a4c4342` changes only guarded observation/read-only seams and
does not change the measured calculation or authoring paths.

| Gesture | Before median | Before slowest | After median | After slowest | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| Cold open/calculation | 66.131 | 66.131 | 65.211 | 67.248 | essentially unchanged (~1% faster median) |
| Warm open/calculation | 1.458 | 1.973 | 0.771 | 1.752 | ~47% faster median |
| Buff switch/calculation | 1.528 | 1.882 | 0.689 | 1.270 | ~55% faster median |
| Recipient toggle including autosave | 35.034 | 45.624 | 36.559 | 51.593 | no material gain; ~4% slower median |

The remaining measured toggle cost is the required atomic, synchronous
autosave. Removing, delaying or weakening that durability contract was rejected.
Runtime `[KBP-WORKSPACE-PERF]` stage logs are ready to separate calculation
from the first correct frame in the deferred observation.

## Focused validation

Final affected suite on exact product source `a4c4342`:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-Development.ps1 -Only 'human-feedback-,native-ui-,direct-manipulation,graph-,blocked-navigation-,execution-policy-,casting-a07,casting-a09,casting-a11,casting-a05,partial-apply-gate,casting-workspace-review-and-apply-policy,casting-workspace-routine-scoped-apply,session-save-preserves-player-settings,session-apply-nothing-to-cast,production-execution-wiring-and-session-lock,shared-personal-selection-and-forecast,shared-powerful-combined-selection-and-demand,share-,player-facing-resource-labels,footer-names-the-run-and-no-ceremony,footer-save-status-is-honest-and-recoverable,hud-routine-buttons-describe-the-click,profile-round-trip-preserves-stable-ids,persistence-round-trip-gaps-and-campaign-isolation'
```

Result: source validation 43/0, production build 1/0, selected protocol tests
101/0, exit 0 in 8.5 seconds. The selected set includes the nine new
human-feedback behaviors, old direct-manipulation expectations updated for
explicit editing, native schema-5 judgement and relevant planning,
persistence, resource, HUD and execution neighbors.

An earlier post-observer attempt was source 43/0, build 1/0 and protocol 49/1:
`native-ui-planner-copy` still expected the superseded “click to select it”
recipient hint. The expectation was corrected to the new normal remove gesture;
the affected rerun passed 50/0 before the final 101/0 run. One manual direct
test-runner invocation also omitted the selector's required game-path/dependency
environment and produced Newtonsoft load errors; it is classified as an invalid
invocation, excluded from product results, and was replaced by the repository
selector above.

The final package command was:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Local.ps1
```

Result: source validation 43/0, build 1/0, strict package validation 4/0,
local package 1/0, exit 0. Both changed PowerShell files also parsed with the
built-in PowerShell parser. The whole-history launcher/purity test was not
run because its mandatory live-purity inventories are the over-budget work
described below. No broad/source-only result is implied by the focused suite.

## Screenshots and runtime boundary

New actual screenshots: **none**. The four packet PNGs were inspected only as
owner-provided before/reference evidence. Older 0.5.0 screenshots are not
relabelled as 0.5.1-rc1 evidence. Compilation, deterministic fixtures and
observer source do not prove rendered opacity, clipping, input ownership,
frame latency or gameplay dispatch.

DATA currently reports a 1920x1200 session display and no Kingmaker process.
The exact-byte live prerequisite inventories runtime state, staging, all
preserved backups, evidence and live Mods before and after the selected
WhatIf calls. The current inputs previously measured 357,318 backup files,
15,132 evidence files, 888 state files and 1,057 live Mods files (374,395
files total; staging empty). A read-only 1,038-file sample took 8.410868 s;
linear scaling estimates about 101 minutes for the two inventories alone.
That estimate is not a purity receipt. The two earlier native observations
took about 140 and 135 game seconds plus boot/preflight/restoration; this
schema adds two bounded capture waits. Plan approximately two hours for the
single complete batch, with each live run still bounded by its 600-second
launcher timeout.

## One exact remaining guarded batch

Run this only from a separately owned, clean worktree frozen at product commit
`a4c4342e1296966cbae1d64eeb53b47f23265627`, with the exact ZIP/manifest above
in its expected `artifacts/local-runtime/0.5.1-rc1` path. First run the
unchanged full-history purity window around both WhatIf calls; only after it
passes, run the owner 1920x1200 observation and the compact 1280x720 observation
serially, verifying complete restoration/protected-save receipts after each.

```powershell
$ErrorActionPreference = 'Stop'
. .\scripts\RuntimeHarness.Common.ps1
$game = Get-KbpGamePath
$targets = @($script:KbpRuntimeStateRoot, $script:KbpRuntimeStagingRoot,
    $script:KbpRuntimeBackupRoot, $script:KbpRuntimeEvidenceRoot,
    (Join-Path $game 'Mods'))

Invoke-KbpLivePurityWindow -Label 'Human feedback native UI selected WhatIf' `
    -Targets $targets -Action {
    foreach ($mode in @('owner', 'windowed-1280x720')) {
        & .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
            -Scenario live-workspace-native-ui -NativeUiStage qualify `
            -CompatibilityProfileId full-user -FixtureFamily Automation `
            -DisplayMode $mode -TimeoutSeconds 600 -WhatIf -Confirm:$false
    }
}

& .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
    -Scenario live-workspace-native-ui -NativeUiStage qualify `
    -CompatibilityProfileId full-user -FixtureFamily Automation `
    -DisplayMode owner -TimeoutSeconds 600 `
    -RunId kbphf-a4c-owner-01 -Confirm:$false

# Continue only after the first run's exact restoration and protected-save receipt passes.
& .\scripts\Invoke-KingmakerRuntimeTest.ps1 `
    -Scenario live-workspace-native-ui -NativeUiStage qualify `
    -CompatibilityProfileId full-user -FixtureFamily Automation `
    -DisplayMode windowed-1280x720 -TimeoutSeconds 600 `
    -RunId kbphf-a4c-720-01 -Confirm:$false
```

Inspect the original PNGs for `nui-resources.png`,
`nui-resources-all.png`, `nui-planner-casting.png`,
`nui-planning-added.png` and `nui-planning-changed-caster.png` at both sizes.
Confirm opaque parchment, no text outside the modal, clipped scrolling,
underlay input blocking, policy and Run captions, continuous assignment state,
and the first correct frame/performance logs. Stop on any unexpected Steam,
account, cloud, update, purchase or credential UI. Do not substitute an older
receipt, prune history, bypass the purity window or use a protected campaign.

## Rejected shortcuts and remaining uncertainty

- The blank first Resources frame was not dismissed as merely a transient
  label issue; the fixed text overlay was replaced with a composed modal.
- Cached calculations are not trusted across authoring revision, input snapshot
  identity or execution mode; execution always refreshes authoritative inputs.
- “Cast available” does not create another executor or catch-and-continue path.
  It reuses the existing gate, omission and dispatch contracts.
- Group coverage was not converted into one direct casting per beneficiary.
- Autosave was not weakened to make the toggle benchmark look faster.
- Source assertions, compilation, older screenshots and main-menu loading are
  not treated as runtime qualification.

Remaining uncertainty is deliberately narrow but material: actual rendered
Resources behavior and first-frame latency at 1920x1200/1280x720, the revised
native observer's live fixture path, and planner/HUD execution against the
running game. Until the exact batch passes and its images are reviewed,
UX-01/UX-02 visual acceptance and all 0.5.1-rc1 runtime claims remain NOT_RUN.

