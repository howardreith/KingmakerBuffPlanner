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

The candidate checkout remains at
`C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner`, on
`codex/kingmaker-buff-planner-playtest-repairs-frozen-cee1060` with exact HEAD above.
Its original package and generated manifest remain untouched. The immutable ZIP
and FREEZE.json are under the lab's `runtime-backups/qualification-frozen/<HEAD>`.
Documentation continuation uses the PR branch worktree at
`artifacts/pr20-continuation`; documentation commits must not restamp the candidate.

The approved Automation working save has SHA-256
`eded233c784fd658ddf2c46b2b54d5a310c39bff62506f0324b0f35c045bbec7`.
A read-only serialized inventory found Heroism and Mirror Image references. This
does **not** establish loaded affordability or Leinna's native fact state. Only
`KBP_AUTOMATION_WORKING` is mutable; baseline and all other saves remain protected.

## Exact next actions

1. Continue the existing prerequisite supervisor. Retain completed receipts and
   inspect every remaining verdict. Do not start gameplay before the required
   safety/source and WhatIf purity checks pass; do not restart the old campaign.
2. From the clean frozen checkout, run `live-cast-qual-select` with recipe
   `heroism-successor`, profile `full-user`, fixture `Automation`, timeout 900,
   run ID `pr20-cee1060-heroism-select`. Qualification uses the owner's display
   mode because explicit window sizes are admitted only for the designated UI routes.
3. After a PASS and complete restored selection lifecycle, use the existing
   `New-KbpRunAllowance.ps1` writer for `pr20-cee1060-heroism-instant` and
   `pr20-cee1060-heroism-animated`, bound to that selection and this exact freeze.
   Record mechanical authority from the owner's PR #20 review instruction; never
   imply individual approval-file inspection or fabricate an allowance.
4. Execute both modes through the guarded `live-cast-qual` launcher. Check real
   effects on their owners, first/recast/successor/skip outcomes, actual resource
   deltas, cleanup, owned process exit, exact Mods restoration and protected saves.
5. Run guarded `live-workspace-native-ui -NativeUiStage qualify` at explicit
   `windowed-1920x1200` on the same candidate. Inspect rendered lifecycle text,
   Require full plan in both states, modal transitions and safe actions, and
   available item/area wording. Report missing fixture coverage explicitly.
6. Update this checkpoint with actual native outcomes, publish records through the
   guarded helper, and keep PR #20 draft. **Do not merge or release.**

Native gameplay and rendered UI acceptance remain **NOT RUN** at this checkpoint.
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
