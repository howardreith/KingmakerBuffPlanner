# PR #20: observation-owner admission and disposable fixture setup

## Current checkpoint: 2026-11-05 19:15 UTC

PR branch remains `codex/kingmaker-buff-planner-playtest-repairs`.
The original checkout is clean on
`codex/kingmaker-buff-planner-playtest-repairs-frozen-1e1a312` at
**1e1a31200628ade5f19eed1d839624ce5ecb33ea**, version 0.6.10 unreleased.
The separate records worktree remains `artifacts/pr20-continuation`.
This section supersedes the historical next actions and in-progress statements below.
No merge or release is authorized or performed.

### Completed launch boundary and actual native evidence on 9716c5d

The supervised `powershell.exe -NoProfile -ExecutionPolicy Bypass -File
scripts/Test-SourceOnly.ps1` completed with exit 0 and aggregate **1/0**.
It ran 14:37:58–18:42:02 UTC (about 4h4m). The original four-hour budget
was explicitly extended by 30 minutes after an actual other-lab overlap
discarded/repeated the File-refused comparison. No history or assertions were
excluded. Full C# **688/0**, source **47/0**, harness **38/0**, physical-reader
**20/0**, problem-reader **35/0**, spellbook-reader **50/0**, deployment purity
**5/0**, File purity **13/0**, File arguments **14/0**, fixture inventory **3/0**,
isolated restore **16/0**, publisher refusal tests **8/0**. Evidence:
`artifacts/playtest-9716c5d-launch-prerequisites.log`.

| Run ID under lab runtime-evidence | Actual result |
| --- | --- |
| pr20-9716c5d-prepare-absent | PASS, complete:true; Linzi bard book CL1→10; real Heroism/Mirror Image; five native level-two uses; Infectious Charms absent; native save committed; zero casts |
| pr20-9716c5d-select-absent | Game PASS, launcher FAIL, complete:false; real sources selected, Linzi→Hedwirg, fact false and first/recast/skip forecasts captured; reader rejected dictionary metadata; no allowance or cast |
| pr20-9716c5d-ui-1080 | PASS **85/0**, schema 9, complete:true; both actual policy states/captures and modal/lifecycle checks |
| pr20-9716c5d-physical-authoring | FAIL **82/1**, complete:false; OS add/remove/re-add/Undo/focus/Escape succeeded, retarget/provider expectations failed |

All four runs exited and verified Mods and protected saves restored cleanly.
UI/display runs also restored registry and physical display. Only the approved
working save changed during preparation. Its SHA changed from
`eded233c784fd658ddf2c46b2b54d5a310c39bff62506f0324b0f35c045bbec7` to
`c567fd46c79482df5c45fc4ef79173ce47a1e448912a307f13b867e0359af0af`.
The immutable baseline and all other saves remained protected. This is Linzi's
explicitly prepared spellbook fixture, with class levels unchanged, not Leinna's
build. No gameplay casts have yet been submitted in this revision.

The policy captures and modal idle/Clear-hover PNGs were visually inspected:
legible captions, ordinary button treatment, both visible policy states. Native
modal pointer callbacks/semantic commands remain distinct from physical input.
The physical authoring aggregate remains FAIL; its successful subchecks do not
qualify the failed retarget/provider gestures. The actual display is 1920x1080;
1920x1200 and rendered Blessed Signet/item-area coverage remain open.

### Batched native test corrections

Reader commit **74339d448aa4341b3f20356fa1d991a4619d6782** accepts only the native
numeric-string `$id` metadata field while still requiring real GUID-to-boolean
facts. Empty dictionaries, invalid metadata, references, string booleans, wrong
sources/owners/forecasts remain rejected. Focused writer/reader tests **16/0**:
`playtest-heroism-native-metadata-reader-final.log`. The immutable raw selection
recheck passed **1/0**, with SHA
`cd486433d08849edfa885c021c72e15a9537d31ff687ab339a36830b7d48e2c3`;
`playtest-heroism-native-selection-recheck.log`. The original failed completion
was not changed and creates no spending authority. An initial recheck command
used the wrong working directory and failed; the corrected invocation completed.

Observer commit **1e1a31200628ade5f19eed1d839624ce5ecb33ea** fixes the physical
setup exposed by the capture: a different portrait in ordinary add mode added
another casting. The observer now physically clicks Change recipient or Change
caster/source, verifies the actual armed intent and focused casting, then clicks
the recipient or provider. Read-only screen-point seams expose those existing
controls. Required acknowledgments and negative controls remain enforced; product
authoring behavior and spending controls are unchanged.

`scripts/Test-Development.ps1 -Only
'physical-,native-ui-,playtest-heroism-,cast-available-,explicit-run-stops-after-failure-both-modes'`
passed **53/0**, source **47/0**, Release compile **1/0**, in 17.8 seconds.
Evidence: `playtest-physical-observer-explicit-edits.log`. Changes to the compiled
observer require a new artifact and corresponding native runs; none of the old
native receipts is relabelled as execution on new bytes.

### Frozen combined candidate and exact next actions

| Identity | 1e1a312 |
| --- | --- |
| DLL SHA-256 | 66af19c93ab4feb1719af57e4a88749cdf64efe7151d924e4fee9723035d68a6 |
| ZIP SHA-256 | 5f2098872cfe43d6cc7e4dc6d150f3bdb6d1a9841bedd9effa5c1bfa006451c8 |
| MVID | 97207552-2cfe-449b-b038-50c5bdfab6f5 |

Built once with Build-Local: source47/build1/package6/local1. Immutable package,
manifest and FREEZE.json are under lab
`runtime-backups/qualification-frozen/1e1a31200628ade5f19eed1d839624ce5ecb33ea/`.
The previous archive and all previous FAIL/PASS receipts are retained.

The new selected launch aggregate started **19:12:57 UTC**, with an explicit
four-hour budget through **23:13 UTC**, log
`artifacts/playtest-1e1a312-launch-prerequisites.log`, supervisor PID30980,
tool session43688. Full offline C# **688/0** has passed, but the launch aggregate
is still active and **not yet PASS**. The unchanged invocation must finish;
do not start another aggregate or modify this frozen checkout.

At **20:14 UTC**, the raw log recorded an actual Deployment WhatIf overlap
with the other lab. The guard discarded that comparison and repeats it when
quiet. The existing invocation received an explicit 30-minute extension through
**23:43 UTC**. The progress watcher initially filtered only launcher retry labels;
its filter was corrected without restarting or changing validation. Only the
owned read-only watcher was replaced; supervisor PID30980 is unchanged. Synthetic
foreign-once/foreign-always/game-running harness cases are not real-lab retries.

1. Inspect its final log and actual exit status; require all launch guards.
2. Fresh Heroism selection on the already prepared absent-feat working copy,
   with new 1e1a312 run IDs. Old failed selection is not allowance authority.
3. Existing allowance writer binds the new frozen package, working SHA, forecasts
   and five native submissions per mode; run instant and animated separately.
4. Guarded present-feat preparation, fresh selection, new allowances and both
   modes. Record actual owners/effects, spending, continuation, repeat and skip.
5. Repeat matching native UI/physical authoring on the combined candidate.
   Gameplay/native work has a separately selected 90-minute budget after guards;
   safe completion/restoration cannot be interrupted for a deadline.


Recorded 2026-11-05 14:24 UTC. Branch
`codex/kingmaker-buff-planner-playtest-repairs`; version 0.6.10 unreleased.
Admission fix: `2bfb09c` (ancestor of source below).
Combined source: `74e9d40390fb75a160d2bc1d18a716e811a65d68`.
[PR #20](https://github.com/howardreith/KingmakerBuffPlanner/pull/20) stays draft;
no merge or release. This checkpoint supersedes the earlier pending fixture
choice. The owner requested preparation of the existing disposable Automation
working copy in the latest review direction. Baseline and other saves remain
protected. The [previous checkpoint](PLAYTEST-REPAIR-REVIEW-CHECKPOINT.md)
retains its cee1060 receipts and native FAIL results unchanged.

## Source correction

The compiler now evaluates observation ownership from the same bound expression
and recipient scopes the execution reader will use. A placeholder becomes
`effect-owner-unresolved`; a required owner outside the readable party/pet
snapshot becomes `effect-owner-not-readable:<id>`. These are per-casting
structural blockers before already-active assessment and resource reservation.
Native selection still includes actual nearby units: required effects are not
discarded and targeting is not artificially restricted to party members.

Cast available omits the blocked casting, explains the required effect owner,
and executes the affordable successor. Require full plan refuses before any
casting starts. Both tests use real compiler, gate, converter, executors,
coordinator and reporting over simulated external native observations.
Both execution modes cover unresolved selection and a selected out-of-party
owner under both policies. Heroism's prepared slots remain untouched; the
distinct successor spends only its own pool. Saved intent remains unchanged.
Existing runtime validation, baseline reading, confirmation and stop-on-failure
protections remain in place for genuinely unexpected failures after admission.

## Verification before native qualification

| Check | Result | Evidence under original `artifacts/` |
| --- | --- | --- |
| New admission reproduction before fix | 0 PASS / 2 FAIL | `playtest-owner-preflight-baseline.log` |
| Corrected admission, both modes (eight policy/owner subcases) | 2/0 | `playtest-owner-preflight-corrected.log` |
| Admission, mixed-owner, spending, availability, unexpected-stop and UI neighbors | 44/0 | `playtest-owner-preflight-neighbors.log` |
| Combined neighbors including fixture scope/request controls | 45/0 | `playtest-owner-preflight-combined-neighbors.log` |
| Fixture descriptor/request scope and UI focused checks | 15/0 | `playtest-fixture-policy-tests-final.log` |
| Fixture backup, rollback, protected policy and evidence reader | 23/0 | `playtest-fixture-preparation-guards-complete.log` |
| Native UI schema-9 launcher reader and legacy negatives | 12/0 | `playtest-native-ui-reader-schema9.log` |
| Launcher argument selection | 35/0 | `playtest-fixture-launcher-arguments.log` |
| Real `-File` child refusal checks | 14/0 | `playtest-fixture-file-arguments.log` |

Development checks used `scripts/Test-Development.ps1` with explicit prefixes
`playtest-heroism-,cast-available-,explicit-run-stops-after-failure-both-modes,native-ui-`.
The final combined pass took 12.2 seconds, source validation 47/0 and Release
compile 1/0. These development DLLs were compiled before the source commit;
they are not frozen native artifacts. The earlier 684/0 receipt belongs only
to cee1060; it is not a full-suite receipt for this revision.

Initial setup iterations retained their failed compile/guard logs. They exposed
a missing test source link/namespace, PowerShell's null backup-path overload for
`File.Replace`, and two test-side scalar/empty-array assumptions. The fixes use
an explicit replacement backup and preserve failed bytes. Missing-target
recovery and native temporary-file ownership controls are included in 23/0.
No real save was mutated by these isolated offline tests.

## Guarded fixture preparation

The separate `live-heroism-fixture-prepare` scenario is non-casting, no-input,
Automation/full-user only, requires an explicit `-HeroismCondition absent|present`
and a timeout of at least 900 seconds. Host validation independently checks its
exact request keys, campaign, working descriptor and immediate save-directory
path; the baseline and other campaign names are refused. It requires an exact
working-save backup before changing any native state.

The diagnostic uses Linzi's existing bard spellbook in the disposable copy.
Native `AddCasterLevel` advances that book to caster level 10, native `AddKnown`
learns real Heroism and Mirror Image from its verified level-two spell list,
native fact APIs set the specified Infectious Charms condition, and native
`Rest` initializes genuine slots once. At least five real shared uses must be
available. Character class progression is **not changed**; this is an explicitly
prepared spellbook fixture, not a normally levelled character or a reproduction
of Leinna's build. This difference is recorded in evidence. No effects or
castings are fabricated; qualification phases never rest, refill or grant casts.

The verified native `SaveRoutine(saveInfo, forceAuto:false)` serializes this
state. Installed IL shows a temporary archive, then replacement of the original
save's exact path. The setup records its temporary filename before saving;
the launcher keeps an original-byte backup. Failure rolls back only the owned
working file and a proven newly created native temporary file, preserving failed
bytes as evidence. Baseline and every other save remain in the existing protected
snapshot comparison; unexpected new files block completion. A running game
prevents rollback or Mods restoration. The new route creates no allowance and
cannot be used as qualification selection evidence. A fresh ordinary non-casting
selection is required after its completed restored lifecycle, binding the new
working-save SHA before the existing allowance writer can authorize gameplay.

Installed Assembly-CSharp SHA-256:
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`;
MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Exact spellbook methods, save lookup,
save-path getters, coroutine and threaded replacement were inspected locally.
Receipts: `heroism-preparation-spellbook-il.log`,
`heroism-preparation-save-lookup-il.log`,
`heroism-preparation-save-coroutine-il-corrected.log`,
`heroism-native-PrepareSave-il.log`, `heroism-native-SerializeAndSaveThread-il.log`.
An initial metadata tool decoder failed on a two-byte opcode; its identified
read-only helper was cleaned up and corrected. It never loaded game state.

## Observer and next native boundary

The launcher also had a second one-line game-log assumption. Its schema-9 reader
now requires the zero-cast summary, correctly ordered and owned refusal details,
both real policy captions/states and their captures, plus exact intent restoration.
Its JSON read uses UTF-8 explicitly; legacy schema-8 verdicts are unchanged.
The original native 84/1 FAIL remains a FAIL. No new native UI PASS is claimed.

Historical next actions at 14:24 UTC (superseded above):

1. Build and archive the combined candidate once. Preserve the earlier cee1060
   immutable archive and receipts; do not attribute them to this new assembly.
2. Run the selected frozen launch prerequisite boundary, including whole-history
   WhatIf purity. Its existing first window now also exercises fixture preparation
   WhatIf; no history is excluded or assertions weakened. No gameplay runs before
   the required guards pass. Retain resumable component receipts if interrupted.
3. Run guarded preparation with condition absent, require PASS and complete
   restoration, then fresh `heroism-successor` selection. Use the existing writer
   for exact frozen/profile/working-SHA forecasts and five submissions per mode.
4. Repeat with condition present, fresh selection and separate allowances; inspect
   actual effect owners, native spending and a distinct successor in both modes.
5. Run schema-9 native UI and applicable physical-input checks, inspect captures,
   and report 1920x1200 or item/area coverage only when actually represented.

The earlier ZFavoredClass sealed-fixture repair remains unchanged. New gameplay,
rendered and physical acceptance are **NOT RUN for this revision** at this
source checkpoint. Native selection of the unchanged level-one fixture will not
be retried. No owner permission question remains for preparing its working copy.

## Final setup audit before launch

Source 74e9d40390fb75a160d2bc1d18a716e811a65d68 additionally defers host
termination while its native save coroutine or commit is pending. A deadline,
abort or disable cannot make the host quit Unity during that write. A stuck
save leaves the game and restoration blocked. The focused fixture scope/save
terminal check passed 1/0, source47/0 and compile1/0, log
artifacts/playtest-fixture-save-terminal-guard.log (7.1 seconds).

The first archive, 341b95ead6ad20bfb38c7e23828162c39def3a09, is superseded
before any native attempt or expensive gate. Its DLL SHA is
c2db9b40670f9be05af7c965971ed8b3094f0db9df960581adc542a912aabd29; ZIP
a843bc49ed89aa1ef26a926290d72f27b656330ebcbe4f7765387bd9dff68184;
MVID 28864e13-7661-4f5d-bc78-9dbd4dc165a0. It passed source47/build1/
package6/local1, but has no native qualification. Its immutable archive remains
in lab runtime-backups/qualification-frozen/341b95ead6ad20bfb38c7e23828162c39def3a09.
The replacement is justified by the save-terminal source change; no unchanged
package is rebuilt. The final clean package identity will be in its FREEZE.json.

Documentation text/link checks passed 26/0 on the initial checkpoint (12 files,
13 new links, authoritative mission byte identity). No merge or release.

## Historical 14:42 UTC: previous frozen candidate and then-active checks

Frozen HEAD **9716c5da0fb550a001a54714717435ae62c9acdc**, version 0.6.10,
original checkout on `codex/kingmaker-buff-planner-playtest-repairs-frozen-9716c5d`.
The PR branch/records worktree is `artifacts/pr20-continuation`. Both retain
separate purposes: do not restamp the unchanged native package for records.

| Frozen identity | Value |
| --- | --- |
| DLL SHA-256 | a1a340dbb9d7f517734f5be0d1494594a0557cbf7931066c9ca56bb0c2351f57 |
| ZIP SHA-256 | 4370a8c295463244198ba7153f95ea2eca3cf5c51c46ac0f2515d524e573a7fd |
| MVID | 16c4ae05-7481-4293-bbd3-4f98a7301c95 |

Build-Local passed source47/build1/package6/local1. Its package and manifest
were copied byte-verified into lab `runtime-backups/qualification-frozen/<HEAD>/`
with FREEZE.json. Logs `playtest-9716c5d-build-local.log` and
`playtest-9716c5d-freeze.log` are in the original artifacts directory.
The guarded push helper published this HEAD; PR #20 was verified draft/unmerged.

The explicitly selected `powershell.exe -NoProfile -ExecutionPolicy Bypass
-File scripts/Test-SourceOnly.ps1` started **14:37:58 UTC**, supervised in
`artifacts/playtest-9716c5d-launch-prerequisites.log`. Budget: four hours to
18:38 UTC, with native preparation/acceptance a separate 90-minute boundary
after the required gates pass. All assertions/history are retained. The
unchanged current invocation must continue; do not start another aggregate.

Completed components so far: **688/0 full offline C#**, source **47/0**,
launcher arguments **35/0**, runtime harness **38/0**, physical search
reader **20/0**, problem navigation **35/0**, spellbook reader **50/0**.
Deployment WhatIf and subsequent full-history launcher windows remain in
progress; **the aggregate is not yet PASS**. There is no live game yet.
No old 684/0 or cee1060 native result has been relabelled for this candidate.
