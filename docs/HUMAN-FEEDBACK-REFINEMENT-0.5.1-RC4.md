# Human-feedback refinement 0.5.1-rc4 decision receipt

## Decision

`0.5.1-rc4` is the installable successor to rc3 for the next guarded live
batch. It closes two defects demonstrated by the retained rc3 owner-display
run: the Resources parchment did not cover its whole dialog opaquely, and the
native observer pre-seeded too many of the three legal Automation recipients
to execute its continuous-assignment sequence.

The rc4 source, observer and package pass focused offline checks and are frozen.
They have **not** been launched in Kingmaker. The one actual rc3 game run is a
preserved **FAIL**, not a runtime qualification of rc4. No merge, tag, release,
upload or permanent installation occurred.

## Exact identities

| Item | Identity |
| --- | --- |
| Branch | `codex/kingmaker-buff-planner-human-feedback` |
| Reviewed feature HEAD | `685dd5c0a9c5ebadb5c527ecb1cd121910968770` |
| Reviewed rc3 product | `debd40674ed2a6ed9f6e3423671f6ec2180bead0` |
| Hash-helper correction | `d6a57d114096cb362212a586faa1a83018d8c67e` |
| rc4 packaged product/observer source | `19e2e777261ff7887f0b8f62c8adcc75161220c5` |
| Version | `0.5.1-rc4` (CLR assembly/file version `0.5.1.0`) |
| ZIP SHA-256 | `1a38d33439392a9c1da5202c1537357213177432e8bca174b550c88ab0c68036` |
| DLL SHA-256 | `093b08fc1f50ba334a05e75dd24eb785808885c5826d1ee6fffa63149591a287` |
| DLL MVID | `be795e3c-7c39-41a7-98e2-fdc5c177c326` |
| Freeze record SHA-256 | `cb4053fb48b6ecc660b6310cd4c492d7de9ee2ba43c597484fe2aa1549ffc66f` |

Installable ZIP:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\artifacts\local-runtime\0.5.1-rc4\KingmakerBuffPlanner-0.5.1-rc4-local-runtime.zip
```

Its original adjacent `*.build-local.json` remains path-bound to that exact
archive and records `validated: true`. The exclusive verified freeze is:

```text
C:\Dev\KingmakerBuffPlannerLab\runtime-backups\qualification-frozen\19e2e777261ff7887f0b8f62c8adcc75161220c5\FREEZE.json
```

The freezer revalidated the original path-bound package, copied the ZIP and
manifest without overwriting an existing target, extracted the contained DLL
independently, and checked its SHA-256 and MVID. The rc1, rc2 and rc3 artifacts,
the rc3 freeze and all historical evidence remain unchanged.

## Relative-path hash correction

`Get-KbpSha256` keeps read-only stream hashing so inherited `-WhatIf` cannot
simulate away a digest. Before opening the stream it now uses PowerShell's
`Resolve-Path -LiteralPath`, requires exactly one FileSystem provider path, and
then hashes that provider path. It does not change the process current
directory.

The focused regression deliberately used different PowerShell and process
directories containing different bytes under the same filename. Before the
fix it failed because the relative path resolved against the process directory.
After the fix `Test-CommonHashing.ps1` passed **8/0**: relative and absolute
selection, literal special characters, a real digest while `WhatIf` remains
inherited, mutation protection, provider rejection, missing input failure and
locked/unreadable input failure. This is a harness correction; rc3 product ZIP
and DLL identities were not changed or relabelled.

## Selected rc3 milestone: actual outcome

Two supervised operation roots are retained.

1. `kbphf-rc3-20261014-01` exited numerically `1` before freeze, purity or a
   game launch. Windows PowerShell promoted Git's successful checkout progress
   on stderr into `NativeCommandError`. The checkout was safely returned. The
   supervisor was corrected to judge native Git by numeric exit status; this
   failure is not a product or runtime result.
2. `kbphf-rc3-20261014-02` ran from 2026-10-14T04:32:27Z through
   06:45:26Z and exited numerically `1`. Product source was rc3 `debd406...`;
   harness source was hash-fixed `d6a57d1...`. The original rc3 receipt was
   checked at its recorded absolute package path, and rc3 was frozen without
   rebuilding.

The first selected five-target purity window used `-Attempts 1` and
`-LeaseWaitSeconds 120`. Its three selected `-WhatIf` launcher calls (owner UI,
1280x720 UI, and Advanced policy selection) passed and all five manifests were
byte-identical. As designed, those WhatIf RunIds created no evidence.

The only actual game RunId was:

```text
kbphf-rc3-20261014-02-ui-owner
```

It loaded the exact rc3 package/DLL/MVID, exited Kingmaker, restored the live
Mods transaction, and compared all protected saves clean. Its result was
**FAIL**, however, because the observer authored two castings in a three-member
fixture and then required two still-free recipients. The retained note is
`planning:fixture-shape;free=1;other=True`; all eight required planning checks
were absent. No native casting was submitted (`RunsStarted=0`). This is an
observer/fixture-contract defect, not evidence that continuous assignment in
the product failed.

The supervisor stopped there as required. It did not launch the 1280x720 run,
Advanced selection, allowance-bound cast, or second purity window. It did not
silently retry the failed owner run.

Key immutable evidence:

| Evidence | SHA-256 |
| --- | --- |
| supervisor status | `d941771def2b453aa62ae739b3a35f682a2b3b100b41477326b64bd95bf43f73` |
| supervisor transcript | `e923263265d1fb6b5866f3eba971b0729b39f90ad3242321a75cfd32fb57f1a3` |
| preserved Unity log | `d19a7de03ed8951bb614de0d17aa60a3c901d46054faa7e3b2ada8296799da26` |
| runtime result | `b08dde7a5e00b6ed6f9e96b421c9de2b55634a0c369f01f2ccbbd443edc91949` |
| native UI record | `cb1770a222046fb7699420bfe3d863c7ac6e80f93419e66f678fb6f890d37cfe` |
| completion record | `18e837691f6f73d508d49abd1a7c543fc32f82654d4b161a5288c4bac4d92413` |
| protected-save comparison | `41ebaa93c5df41eef26e9f14b184d2b4d9ce6cdbacb19c965774f2d5aacfc017` |

The completion record says `gameResultStatus=FAIL`, `complete=false`, while
`kingmakerExited`, `restorationVerified`, `protectedSavesCompared`, and
`protectedSavesClean` are all true. That is the precise restoration claim:
Mods/save/owned-process restoration passed; the runtime verdict did not.

### Resolution qualification

The preflight physical desktop probe reported 1920x1200, but the retained
Kingmaker owner settings selected 1920x1080 fullscreen and the Unity record and
every screenshot are 1920x1080. A later physical-mode probe also reported
1920x1080. Therefore this run is not relabelled as the owner's required
1920x1200 observation, and physical display restoration to the preflight mode
is not established by the launcher completion record. The next owner-mode run
must snapshot and guard both the OS display mode and Kingmaker owner settings,
prove 1920x1200 in the Unity record, and restore both values transactionally.

## Actual rc3 visual findings

The following original-resolution screenshots were inspected, not merely
source-asserted:

| Capture | Size | SHA-256 |
| --- | --- | --- |
| `nui-planner-casting.png` | 1920x1080 | `967e8031828afb3e06c842f56b671985183d9880f9f72789678656043e960931` |
| `nui-resources.png` | 1920x1080 | `b235004efe71e3403193b8e07793a7186e6925f441b8d46c703e42ba685923a0` |
| `nui-resources-all.png` | 1920x1080 | `60a7c354bcedbb80943f135c3f23c72f91a597b40d9cf720f9dd072dc8298864` |
| `nui-resources-selected-again.png` | 1920x1080 | `6895ab79f24276efb66242e38e7bb9584b00d3900501b657247f2bb8831dc555` |
| `nui-resources-reopened.png` | 1920x1080 | `144e93e838539ba91eee281b7a1303866a91a483b9b411856a4579a17975fba8` |

They are under
`C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\kbphf-rc3-20261014-02-ui-owner`.
The accounting table itself is readable, bounded and opaque. All four states
show ten visible native-layer cells and useful content (`Linzi`, `free`,
`Unlimited`, `2 casts`, `2 casts`, `—`). Selected, All, Selected-again and
reopened scopes are correct; clipping, topmost ownership, focused close,
close-between-opens, reopen and Escape closure are all true.

Visual inspection also found a real defect: the approved textured PaperSlip is
translucent above the table, allowing the planner's headings and selected-buff
title to overlap the Resources title, scope controls and summary. rc4 places a
fully opaque parchment sibling beneath the existing PaperSlip over the complete
dialog bounds. It preserves the native texture and buttons; schema 7 now
requires that backing to be active, opaque and below the dialog. Runtime visual
confirmation on rc4 remains required.

The planner capture shows ordinary `Run Long` and `Require full plan` casing
with the approved native skins. It also shows provider-independent removal and
add hints, but it does not prove the skipped multi-click assignment sequence.

## Responsiveness observed on rc3

These are handler/request-to-Unity-frame-end measurements with correlated
expected-state observations. They are not pixel correctness claims.

- First cold open: **463.494 ms** from the common open-request boundary to
  frame end, expected state confirmed. Its pre-refresh interval was 366.546 ms:
  discovery 82.020, session/persistence 18.512, input lease 2.607, view
  construction 248.699, reconciliation 14.513 ms. Refresh attribution was
  calculation 24.502, review 0.534, region rebuild 42.536, theme 11.771,
  layout 1.125 and presented refresh 80.872 ms.
- Later cold opens were usually **106.783–121.409 ms**. Two outliers were
  368.654 ms (270.341 ms reconciliation) and 398.747 ms (282.276 ms discovery);
  one earlier open was 161.229 ms. The owner's reported one-to-two-second delay
  did not reproduce in this partial 1920x1080 run, but the outliers show that
  discovery/reconciliation, not sub-millisecond calculation, still dominate.
- Resources first open: 11.981 ms synchronous and **30.930 ms** to frame end;
  calculation 4.943, region rebuild 5.289, layout 0.067 ms.
- Scope to All: 10.896 ms synchronous and **28.895 ms** to frame end;
  calculation 0.262, region rebuild 6.212, layout 4.398 ms.
- Scope back to Selected: 8.921 ms synchronous and **33.435 ms** to frame end;
  calculation 0.245, region rebuild 5.207, layout 3.456 ms.
- Reopen: 5.825 ms synchronous and **31.547 ms** to frame end; calculation
  0.243, region rebuild 4.480, layout 0.052 ms.

Each Resources record required the exact scope and ten visible native-layer
cells. A superseded warm refresh was explicitly recorded as superseded. The
observer failure prevented valid buff-switch and recipient-toggle gestures, so
no live timing is claimed for them.

## rc4 correction and focused validation

The observer now computes a seed count that preserves two legal recipients:
three legal recipients seed one casting; four or more seed at most two; fewer
than three refuse the scenario explicitly. This keeps representative authored
state while allowing successive normal recipient clicks, next-source changes
and explicit one-shot edits to run. It does not change product assignment
business rules.

Focused results on the corrected source:

- `Test-Development.ps1 -Only native-ui-record,native-ui-planning-seed`:
  source 43/0, build 1/0, selected behavior 3/0.
- `Test-Development.ps1 -Only native-ui-`: source 43/0, build 1/0, native UI
  neighbors 11/0.
- `Test-CommonHashing.ps1`: 8/0.
- `Test-RuntimeLauncherArguments.ps1`: 26/0; no live launch.
- `Build-Local.ps1`: source 43/0, build 1/0, package 4/0, local package 1/0.
- `git diff --check`: clean.

No broad `Test-SourceOnly`, FileWhatIf aggregate, publisher or release path was
run. No offline result is called runtime or visual PASS.

## UX-01 through UX-06 status

| ID | Decision status |
| --- | --- |
| UX-01 Resources | **PARTIAL / rc4 runtime required.** rc3 proves actual visible native-layer accounting cells, useful content, clipping, scope changes, reopen and focus/input ownership at 1920x1080; visual review found whole-dialog translucency. rc4 fixes and observes the backing offline, but has no game screenshot. |
| UX-02 responsiveness | **PARTIAL.** rc3 supplies honest request/handler-to-frame-end data and stage attribution at 1920x1080. Required 1920x1200, 1280x720, buff-switch and recipient-toggle measurements remain. |
| UX-03 continuous assignment | **OFFLINE PASS / LIVE NOT RUN.** Product regressions remain green. The rc3 live observer skipped the sequence; rc4 corrects that observer contract. |
| UX-04 draft choices | **OFFLINE PASS / LIVE NOT RUN.** Same-context batching and context-change reset regressions remain; no valid rc3 gesture sequence reached them. |
| UX-05 availability policy | **OFFLINE PASS / LIVE NOT RUN.** The selected run stopped before Advanced selection or casting. There is no strict/permissive execution evidence, no allowance and no physical HUD press. The existing shared HUD resolver coverage remains resolver integration only. |
| UX-06 Run labels | **PARTIAL.** rc3 visually shows ordinary `Run Long` casing/native skin at 1920x1080; rc4 preserves it. The two required resolutions remain. |

## One remaining live-validation batch

No rc3 purity or game receipt may qualify changed rc4 observer/product bytes.
Use the frozen rc4 candidate at `19e2e777...` and a separate clean harness
checkout of the same source. Re-run the current Advanced inspection guard
immediately before use. The last pre-run guard matched inspection
`advanced-inspect-20260924-01`, game ID
`cb1f405d-0b0c-4281-8b76-3b4f3b690abb`, WORKING SHA-256
`9787613753f38abf08dceeac8f7a66a6b0b1a5cb28ee0a7cd8805f0f265f1e7d`
and compatibility identity
`088553867688dd3c09a5c5ad593d551682ed0977a5ed5db3f0163d6305fc02b7`.
If any current guard differs, stop and run only its prescribed replacement
inspection and prerequisites; do not reuse this dated match.

The batch is the same bounded topology, with fresh rc4 RunIds:

1. One five-target UI/selection purity window, `-Attempts 1` and
   `-LeaseWaitSeconds 120`, containing owner 1920x1200 UI, windowed 1280x720 UI
   and `policy-shared-powerful` Advanced selection WhatIf calls.
2. Serial actual owner 1920x1200 UI, 1280x720 UI and Advanced selection runs,
   each followed by process, Mods, display/settings and protected-save
   restoration verification. The owner run must refuse before launch unless
   both configured and observable size are 1920x1200.
3. Create the existing mechanically run-bound instant qualification allowance
   from that fresh selection and the rc4 qualification freeze.
4. One separate five-target cast purity window with the same bounded
   parameters. Require its WhatIf to leave the allowance unused and the actual
   cast RunId absent.
5. Run the matching policy cast once. Require strict zero-submission/no-spend
   refusal, the ordered affordable permissive subset, omission reasons,
   unchanged authored assignments/variants/modifiers/enhancements, exact
   shared and complete per-casting costs, no-op and supplied combat-active
   refusal. This is not a claim of a physical HUD press or a real combat
   transition.
6. Inspect the original PNGs and correlated timing/evidence records. Confirm
   the Resources header is now opaque as well as the table, and measure cold
   open, buff switch, recipient toggle, Resources open/scopes/reopen at both
   resolutions.

Complete estimated cost remains approximately **7 hours 30 minutes**: two
five-target purity windows 5 h 10 m, four launcher deadlines up to 50 m,
transactional 1920x1200 setup plus allowance work 10 m, four restoration/save
reviews 20 m, and visual/timing/policy review 60 m. The rc4 freeze is already
complete. If the Advanced guard has drifted, add its prescribed separate
inspection purity, 15-minute game deadline, restoration and review; determine
that added inventory cost from the current guard rather than inventing reuse.

Smallest remaining human review after a successful guarded batch:

1. Compare the Resources header/table, clipping and scroll at both sizes.
2. Confirm the required Seamantle and Resinous Skin/Share gestures, hints,
   one-shot explicit edits and draft reset/preservation feel correct.
3. Judge cold-open/buff-switch/recipient-toggle latency from the paired frames
   and timings.
4. Read the strict/permissive report for ordered casts, omissions and resource
   accounting; no additional release or publication review is implied.
