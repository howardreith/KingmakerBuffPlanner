> Superseded by the [owner-preflight and fixture-setup checkpoint](PLAYTEST-REPAIR-OWNER-PREFLIGHT-CHECKPOINT.md). These cee1060 receipts remain historical and unchanged.

# PR #20 review correction checkpoint

Status recorded at **2026-11-05 04:19 UTC**, with continuation below.
Version **0.6.10**, unreleased.
PR branch: `codex/kingmaker-buff-planner-playtest-repairs`.
Frozen product HEAD: `cee1060fffb824a6d4838d5925727cbaff1833f4`.
[Draft PR #20](https://github.com/howardreith/KingmakerBuffPlanner/pull/20) remains unmerged.

## Source repair

Required effects are now judged on their owners. Direct spell targets remain
primary recipients; observation owners additionally include the caster and
explicitly bound secondary recipients. A new or refreshed expected effect on
the primary recipient remains necessary. Completeness, typed identity,
suppression, readability, spending and cleanup are separate requirements.
Already-active assessment uses the same recipient bindings and retains strength
and duration checks. A wholly personal payload still follows its effective
recipient through established Infusion/Share targeting.

The installed CotW `ApplyActionToTargetsInRange` wrapper excludes the spell
target and selects nearest eligible other units. Generic ActionList traversal
had lost that transformation. Optional structural metadata now preserves its
verified radius, maximum and allegiance flags. Native snapshots bind owners;
unknown/ambiguous ownership cannot become ordinary-target coverage. Catalogue
and saved source IDs stay stable, while concrete leaf-owner assignments enter
the authorized execution projection. There is no gameplay-mod compile dependency
or ordinary-casting Heroism GUID exception.

The `heroism-successor` diagnostic recipe is **implemented**, including its
selection, forecasts, driver phases, outcome reader and existing allowance writer.
It selects loaded real Heroism and a distinct affordable Mirror Image or Heightened
Awareness. Heroism targets a different unit from its caster. First and recast
phases execute both spells; the third phase skips adequate Heroism and executes
the still-needed successor. The forecast binds **2 + 2 + 1 submissions**, using
genuine remaining reservations without refills or invisible prepared-slot reuse.

Selection captures raw/bound expressions, actual caster facts, all loaded Heroism
providers' names/facts, successor identity and observation owners. Terminal native
confirmation retains before/after owner reads and eight bounded poll samples.
The native UI observer captures both modal buttons' idle/hover/exit states,
Cancel, rejected opening-frame activation, semantic Confirm on default Cancel,
actual Clear, and production Undo restoring exact authored intent. Semantic
command automation is distinguished from physical keyboard/controller input.

The original text-material ownership, normal modal styling and Blessed Signet
area-wording changes remain. Their rendered/gameplay effectiveness is not inferred
from offline checks.

## Automated evidence

| Check | Actual result | Receipt under repository artifacts |
| --- | --- | --- |
| Feat-present mixed-owner baseline, both modes | 0 passed / 2 failed before correction | playtest-mixed-owner-baseline.log |
| Shared correction and neighbors | 57/0 | playtest-candidate-focused.log |
| Final confirmation/UI/recipe neighbors | 66/0 | playtest-observer-correction.log |
| Complete frozen C# runner | 684/0 | playtest-cee1060-launch-prerequisites.log |
| Source validation | 47/0 | playtest-replacement-build-local.log |
| Candidate build / ZIP / local packaging | 1/0; 6/0; 1/0 | playtest-replacement-build-local.log |
| Allowance writer and six before-write refusals | 8/0 | playtest-cee1060-writer.log |
| Manifest equivalence, mutation/refusal and WhatIf checks | 9/0 | playtest-manifest-reader-tests.log |
| Full-user sealed profile, 15 mods | 1/0 | playtest-profile-fast-reader.log |
| Guarded push helper, remote access enabled | 6/0 | playtest-cee1060-guarded-push-tests-network.log |

Documentation-only continuation: changed-text/link checks passed **25/0**
across 12 Markdown files and 12 new links, including byte identity of the mission
copies. Command: `node artifacts/Check-Pr20ReviewDocs.cjs`; receipt:
`artifacts/playtest-review-doc-checks.log`. No candidate rebuild was selected.

Focused command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'confirmation,playtest-heroism,heroism-repeat,clear-plan,qualification,native-boundary'
```

The first frozen prerequisite attempt at `813a69f` stopped 683/1 at an unchanged
confirmation source guard after the poll observer changed call structure. The
direct judgment helper was restored; no assertion was weakened. The replacement
`cee1060` passed all 684 C# tests. Its following harness step could not start
PowerShell 7 from the restricted process. The unchanged remainder was resumed with
the required execution access, without rebuilding or rerunning the completed C# pass.

## Profile and prerequisite boundary

ZFavoredClass's only drift was generated `loaded_blueprints.txt`. The sealed
historical directory was recovered exactly into the existing external-fixture
workflow; every expected hash, version, count and compatibility identity is unchanged.
The live directory was not reset or resealed. [Exact drift hashes, copy guards,
source contracts and earlier attempts](evidence/playtest-repair/REVIEW-WORK.md).

Completed resumed stages at this checkpoint: filesystem harness **38/0**, physical
search reader **20/0**, problem navigation reader **35/0**, spellbook reader **50/0**,
and deployment WhatIf purity **5/0** (3,129.25 seconds). The remaining launcher
`-File` purity checks are **IN PROGRESS**; the complete aggregate is not PASS.
Fixture-inventory, isolated restore-install and publisher-guard tests follow them.

Each full snapshot covers about **76 GB** and 449,000 files. All history, contents,
timestamps and ordering remain covered; hashing uses four bounded readers without
caching. An overlapping Gunslinger-lab game was identified from its own request
and lease and left untouched. No KBP native launch occurred. The guard waited for
quiet as applicable and the completed deployment window compared unchanged state.

Source-only resume receipt: `artifacts/playtest-cee1060-prerequisites-resumed.log`;
per-stage state: `artifacts/playtest-prerequisite-stages.json`. The owned read-only
supervisor is PID **12888**. Its declared prerequisite budget ends at
**2026-11-05 07:12 UTC**; native acceptance has a separate 90-minute budget and
active restoration is never interrupted for a budget.

## Frozen artifact and continuation

| Identity | Value |
| --- | --- |
| DLL SHA-256 | ee638eaa5ba0929186a71980f07077178b554dbcd7f86e99faee6ba483e9f995 |
| ZIP SHA-256 | 8e51f0d9aee3268fcd1bbf2559d70c0329ac463a1e5efca0c93bbb1e7df81f0e |
| MVID | a4249a79-8da2-4342-a0b8-ac94877819f8 |

At the freeze and during both native runs, the candidate checkout was at
`C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner`, on
`codex/kingmaker-buff-planner-playtest-repairs-frozen-cee1060` with exact HEAD above.
Its original package and generated manifest remain untouched. The immutable ZIP
and FREEZE.json are under the lab's `runtime-backups/qualification-frozen/<HEAD>`.
Source/records continuation uses the PR branch worktree at
`artifacts/pr20-continuation`; documentation commits must not restamp the candidate.

The approved Automation working save has SHA-256
`eded233c784fd658ddf2c46b2b54d5a310c39bff62506f0324b0f35c045bbec7`.
A read-only serialized inventory found Heroism and Mirror Image references. This
does **not** establish loaded affordability or Leinna's native fact state. Only
`KBP_AUTOMATION_WORKING` is mutable; baseline and all other saves remain protected.

## Exact next actions (supersedes the earlier campaign sequence)

1. Obtain the answer to the pending fixture choice: prepare the existing
   Automation working copy, or use an owner-designated higher-level disposable
   fixture through the approved workflow. The native selection below proves the
   current fixture cannot run this recipe. Do not retry it unchanged, infer
   usable spells from serialized references, or load a real campaign save.
2. Prepare only the authorized disposable working save with real learned,
   affordable Heroism and its successor. Preserve baseline and protected saves.
   The guarded recipe and allowance writer are implemented; no duplicate recipe
   is needed. Leinna and the applicable feat branch still need native coverage.
3. Freeze the combined source/observer candidate after fixture setup is ready.
   The schema-9 changes below alter the product assembly: they require a new
   candidate and affected native UI observation. Do not attribute cee1060 receipts
   to that later binary or rebuild cee1060 just to stamp documentation.
4. Complete the required checks for that candidate, select non-casting first,
   then require PASS plus complete restored selection before writing allowances
   using `New-KbpRunAllowance.ps1`. Execute the guarded `heroism-successor` recipe
   in both modes and verify owners, spending, distinct successor and restoration.
5. Repeat affected native UI checks, inspect both policy states and lifecycle
   captions, and obtain the outstanding 1920x1200, physical-input and item/area
   coverage when its environment and fixture are available. Keep PR #20 draft;
   **do not merge or release**.

The owner's uploaded 6c75425 log remains original-failure evidence only.

## 06:35 UTC: prerequisites complete, native selection started

All four required global history windows completed on the frozen candidate.
The full launcher test then failed at a **later reader assertion**, after
8,081.50 seconds: Native UI stage-on-another-scenario was correctly refused
with `KBP_NATIVE_UI_STAGE_SCENARIO`, but formatted stderr inserted whitespace
into its prose and the old message matcher rejected that correct refusal.
The original full-script FAIL is retained, not relabeled as PASS.

The two Native UI parameter cases now use the shared real `-File` child-process
assertion and exact typed error markers. Nonzero exit remains mandatory; legacy
refusal and wrong-marker/success-exit controls remain intact. Focused checks
passed **11/0**, including a new wrong-Native-UI-code rejection. Only the test
consumer changed; the frozen launcher, product, package and manifest did not.

The unchanged bounded remainder (Layer 6 onward, with no history windows)
was resumed using its original script-directory context and the corrected
reader. Every assertion was preserved; only the final footer was relabeled
to identify a composed result. It completed in **71.85 seconds**. The three
remaining scripts passed: fixture inventory **3/0**, isolated restore **16/0**,
publisher guard **8/0**. Prerequisites are complete **across these retained and
resumed components**, not from a new uninterrupted aggregate.

Receipts under the original checkout's artifacts:
`playtest-native-refusal-reader-corrected.log`, `playtest-bounded-launcher-resume.log`,
`playtest-cee1060-fixture-inventory.log`, `playtest-cee1060-restore-tests.log`,
`playtest-cee1060-publisher-guard.log`, and
`playtest-cee1060-prerequisite-completion.json`.
The executed reader source SHA-256 was
`72912ce0b1f67d965b99f462855e5a15c72b1ad507e5cfa4250a8a583cd60ed8`.

The guarded **non-casting** `pr20-cee1060-heroism-select` run was started at
06:35 UTC from the unchanged frozen checkout. It must complete its whole
restored lifecycle before any spending allowance is written. Gameplay and
rendered UI acceptance remain pending. Continue this run; do not restart the
completed history scans or rebuild the candidate for this reader correction.

## 06:56 UTC: actual native outcomes and bounded observer correction

Source is now `87d15b12ff1d57e876b970ae5201c5ffa301fc53` on the PR branch,
version 0.6.10 unreleased. Both runs below used **frozen cee1060**, its exact DLL,
ZIP, MVID and unchanged launcher. All 15 full-user mod identity assertions passed,
including ZFavoredClass. The compatibility identity was
`c9a1664ca57e1bbe8b7b9f193a4f4633bd80498bdfcb2a636aa6a37ea110ca43`.

| Native run | Result | Spending | Safe completion |
| --- | --- | --- | --- |
| `pr20-cee1060-heroism-select` | FAIL: `no-affordable-real-heroism-successor` | 0 submissions; no allowance written | exited, Mods restored, protected saves clean |
| `pr20-cee1060-ui-qualify` | pre-mutation refusal: physical 1920x1080 cannot fit windowed 1920x1200 | none | no transaction or game started |
| `pr20-cee1060-ui-1080` | FAIL: 84 assertions pass / 1 fails (`game-log-planner-lines:2`) | 0 runs started; casting disabled | exited, Mods/display restored, protected saves clean |

The selection roster was Hedwirg, Linzi and Tartuccio. Loaded inventory contained
14 providers, **zero Heroism and zero Mirror Image providers**, with caster level
1 and only level-one spell pools. Leinna was absent. This disproves the theory
that the existing serialized spell references establish a usable native fixture.
It does not establish Leinna's feat state or invalidate the offline positive
mixed-owner regression. The pending owner fixture question is necessary because
the mission protects the baseline and every other save; no alternate real save,
fake effect, free spell injection or weakened allowance is substituted.

The UI captures were visually inspected: Casting options labels, Change
recipient, Require full plan, resources including a reopened resources panel,
and the Clear modal were legible. Idle modal had neither selected button nor red
warning treatment; hover appeared on the corresponding button, and exit
captures returned to idle. The five modal checks were true: Cancel preserves
intent, opening-edge activation is rejected, semantic Confirm cancels, Clear
applies with Undo available, and Undo restores exact authored intent.
The observer uses native pointer callbacks and semantic controller commands;
this is **not physical mouse, keyboard or controller dispatch acceptance**.
Only the unchecked policy state was captured; 1200-height and Blessed Signet
content were not covered. No overall visual/gameplay qualification is claimed.

The aggregate UI FAIL is an observer contract mismatch: current product emits
one zero-cast summary plus one per-casting refusal detail, while schema 8 demanded
exactly one line. The two actual lines state native submission is disabled by
the absent grant; no cast was attempted. The original FAIL and raw evidence
remain unchanged. Source schema 9 requires that zero-cast summary, the exact
native refusal code, one correctly ordered/labelled detail per planned casting,
and the existing refused-run / zero-start controls. Negative controls reject
missing/duplicate lines, wrong owners/reasons, a nonzero cast summary and started
runs. Historical schema 8 retains its original verdict.

The batched new observer toggles the actual AvailabilityPolicy control twice,
holds each state for a completed rendered capture, records its real policy and
caption, and requires both states plus exact intent restoration. These changes
are **not present in frozen cee1060**, so no old UI receipt qualifies them.
They require the next affected native observation after fixture preparation.

Focused command in the continuation worktree:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'native-ui-,playtest-text-,human-feedback-native-ui-'
```

Final result **14/0**, source **47/0**, Release compile **1/0**, 7.3 seconds.
The first run was **13/1** because the old unknown-schema negative fixture used
newly valid schema 9; that negative was moved to still-unknown schema 10 and the
focused suite reran. Both logs remain in continuation `artifacts/`:
`playtest-ui-observer-schema9.log`, `playtest-ui-observer-schema9-corrected.log`.
The development DLL SHA was
`9d2036f569f650f7ee2177f3492374033a7e29c42606144c53685b3212c1150d`,
compiled from precommit b1da33 plus these observer changes; it is not a frozen
or deployable qualification artifact. No new broad campaign was started.

Native commands used `powershell.exe -NoProfile -ExecutionPolicy Bypass -Command`
and the repository's guarded `Invoke-KingmakerRuntimeTest.ps1`, `-Confirm:$false`,
profile `full-user`, fixture `Automation`, timeout 900, and the exact run IDs above.
Selection used `-Scenario live-cast-qual-select -QualificationRecipe heroism-successor
-ExecutionMode instant` with owner display. UI used `-Scenario live-workspace-native-ui
-NativeUiStage qualify -DisplayMode windowed-1920x1080` (1200 for the refused attempt).

Raw evidence: lab `runtime-evidence/<run-id>/` containing `runtime-result.json`,
`run-completion.json`, `qual-outcome.json`/`capability-inventory.json` for selection,
and `native-ui.json`, protected-save/display receipts and rendered PNGs for UI.
Launcher logs are in the original checkout's `artifacts/playtest-native-heroism-select.log`,
`playtest-native-ui-qualify.log` and `playtest-native-ui-1080.log`.
Both run-completion receipts say `complete:false` because their scenario failed;
neither was relabelled complete because restoration succeeded.

[Immutable raw-receipt hashes](evidence/playtest-repair/NATIVE-REVIEW-RECEIPTS.json).

Documentation checks passed **13/0** (11 Markdown files, one new local link,
and authoritative mission byte identity); `git diff --check` was clean.
The guarded helper pushed records HEAD
`2c2181c30e722ff1be1772d007e04111787c3d6c`; PR #20 was verified draft/unmerged.
After clean native restoration, the original workspace returns to the PR branch;
the frozen cee1060 branch, immutable archive and receipts remain preserved.
Its old package must not be presented as a build of the newer observer source.
