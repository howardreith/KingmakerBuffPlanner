# WP2A — Not Ready problem navigation (unreleased 0.3.0 candidate)

Scope: find a blocked casting immediately. Base: current origin/main
b707c1f47859f2ecae517b2cd162d20ca18c7583, fetched 2026-10-07.
Branch: codex/kbp-not-ready-deep-linking-2026-10-07.
Worktree: private/worktrees/WP2A under the original checkout. The original
checkout and the WP1 branch are preserved; no WP1 commit is inherited.
Public version stays 0.3.0. No merge, tag, PR or release is authorized.

## Checkpoint: implementation before physical qualification

HEAD before this checkpoint: b707c1f47859f2ecae517b2cd162d20ca18c7583;
worktree dirty with the focused implementation. Remote main matches that
SHA; no open pull requests. Initial ownership audit found 398 deployment
transactions, all Restored, no game process, no deployment/foreign lease
and no dispatcher process. Steam is running. No live mutation occurred.

Previous behavior, traced in production: CastingExecutionGate exposed
sorted textual BlockingReasons only. Planner RunApply showed RunBlocked
and returned without focusing. The HUD StartCastingFirstRoutine selected
the routine and opened Setup with a generic footer; no casting identity
or viewport reveal was requested.

Implemented: CastingBlocker identities and exact reasons in compiled
execution order, propagated through CastingApplyDecision and
WorkspaceApplyResult. The shared session Apply boundary starts transient
navigation, uses FocusGraphCasting and recomputes from the existing
BuildGraph gate after edits. Previous/Next change inspection only.
Disabled/already-active omissions stay outside the blocker collection.
Ready Casts Only retains its existing gate and omission policy.
Global refusals supply no casting identity.

The view presents routine, Problem X of N, buff, caster/target and the
existing reason translation, and reveals card/catalogue/inspector through
actual RectTransform bounds. A consumed reveal is never requested by an
ordinary refresh; geometry changes may request a single new reveal.
Search/category settings are retained. Manual selection, Done and the
existing nested Escape behavior leave problem inspection.

Commands at this checkpoint:
- MSBuild tests/KingmakerBuffPlanner.Tests/KingmakerBuffPlanner.Tests.csproj
  /t:Rebuild /p:Configuration=Release /m /nologo /v:minimal
- artifacts/tests/KingmakerBuffPlanner.Tests.exe --blocked-navigation:
  PASS=11 FAIL=0.
- scripts/Build.ps1: source validation PASS=42 FAIL=0; build PASS=1 FAIL=0.
  No compiler warnings. Development DLL SHA-256
  60f973067c474bb83b48cd620344b513a1498a0d88468fce1ef0497c1980b74a
  was built at dirty baseline HEAD and is NOT a qualified candidate.

Rejected approaches: parsing refusal text; sorting identities; adding a UI
readiness predicate; permanently owning scroll; inheriting WP1. Initial
test defects corrected: an empty Draft uses the gate's existing fallback
reason; presentation state must be captured before inspecting navigation.

Uncertainty: physical Unity layout, masked visibility and automatic HUD
opening are not yet qualified. Complete mechanical gate not yet run.
Exact next action: extend the existing guarded physical-workspace scenario
with a blocked-only expectation; qualify the complete source gate, commit
and build a clean identity-bound candidate, then physically exercise the
HUD/Previous/Next/Escape paths with zero native submissions and exact
installation/save restoration.

## Checkpoint: guarded scenario, before complete source gate

Branch unchanged; exact HEAD d339811c44c6a8ae74c0d76ece8642f84b68d104,
version 0.3.0, dirty with the scenario/lifecycle changes. No runtime mutation.
Focused C# regression command now PASS=14 FAIL=0. The complete C# runner
at the preceding scenario revision passed 411/411. Production validation
42/42 and compilation 1/1 pass with no compiler warnings. Independent
scripts/Test-ProblemNavigationEvidence.ps1 under the absolute Windows
PowerShell 5.1 host: PASS=18 FAIL=0. Actual complete source gate remains next.

The existing live-workspace-physical launcher now accepts only the additional
physicalExpectation "problems", with no allowance permitted. The scenario
authors a disposable Long plan in the transactional mod directory, with
twenty early castings and two later Drafts. It requests physical moon,
Next, Previous and two Escape presses. Read-only seams measure masked
chip visibility before reveal, actual screen points and visible card
fraction afterward, catalogue/routine points, visible inspector text and
boundary controls. An independent record judge and launcher verifier
require unchanged document/profile/native state, zero dispatch/run,
unchanged Undo and no authorization, and a clean nested Escape close.
Existing cast/select expectations and allowance guards remain intact.

Supporting lifecycle repair: a global party-refresh refusal after problem
inspection clears stale problem focus and remains a global explanation.
Regression proves no document, profile or Undo change.

Push helper's authorized branch set was extended only for the owner's exact
requested branch; repository, payload, clean-tree and fast-forward guards
remain mandatory. Helper SHA-256:
55b7d87057655e61881b7acfcac0858789fdce198343ceab955433c2bbb9e3e2.
scripts/Test-GuardedPush.ps1 now validates the actual focused worktree.

Uncertainty: no physical claim yet. A charter annotation was rejected by
the authoritative-copy validator and removed; both mission copies remain
byte-identical. Development C# scope/string-literal defects were corrected,
then the affected tests/build rerun. Exact next action: commit this coherent
scenario checkpoint, execute scripts/Test-SourceOnly.ps1 to zero failures,
Build-Local on the clean commit, then bind the guarded physical run to the
resulting ZIP/DLL hashes and MVID.
## Checkpoint: unavailable-source reachability regression

Branch codex/kbp-not-ready-deep-linking-2026-10-07; exact pre-commit HEAD
8ec8cbf00c2030e4b86b8b0424795a8f816a37ce; version 0.3.0; dirty only with
this focused repair and evidence. The previously running complete source
gate was stopped at its archival manifest comparison so this change can
be qualified on a new clean candidate. Its completed components were C#
412/412, runtime filesystem 38/38 and problem-evidence 18/18. It did not
reach a complete source-qualified verdict or enter live runtime.

A real-service regression removed the saved buff from current discovery.
Ordinary Apply correctly identified its authored casting, but the graph
lacked the selected catalogue row required to finish reveal. The focused
runner reproduced PASS=14 FAIL=1 at artifacts/wp2a-missing-source-red.log.
The repair supplies an explicitly unavailable selected row only during
problem inspection, without a provider or readiness judgment. Reveal now
waits for all graph, catalogue and inspector rectangles before moving any
scroll. Actual imported Draft provenance is also exercised without
guessing its caster.

After repair: affected MSBuild test rebuild and the --blocked-navigation
runner PASS=15 FAIL=0 (artifacts/wp2a-focused-15.log). scripts/Build.ps1
source validation 42/42 and build 1/1 pass, with no compiler warnings
(artifacts/wp2a-unavailable-source-build.log). Development DLL SHA-256
1f315543524edbd48af18b531445166a47dc07d7f59e17ac2a6fc34699be87e5 is not
yet a qualified candidate.

Rejected theory: an unavailable saved source always has a catalogue row
because selected entries bypass filters. That rule applies only to entries
discovery actually supplied. No source catalogue audit or readiness
implementation was added.

Exact next action: commit; Build-Local for the harness's required ZIP
prerequisite; run the complete Test-SourceOnly gate to zero failures;
rebuild the final clean candidate; run the candidate-bound blocked-only
physical scenario; verify exact restoration and protected saves.
Physical visibility remains uncertain until that run passes.
## Checkpoint: structured HUD opening boundary

Branch unchanged; exact pre-commit HEAD
c7c3600fde5bb0b25a7f2b580dc9ea76eb095817; version 0.3.0; dirty with this
narrow contract change. HUD opening now consumes WorkspaceApplyResult:
casting blockers open directly; only global refusals consult the existing
opening policy. A real-gate regression changes only presentation refusal
text and still requires automatic opening, while an unknown global refusal
retains the prior closed-planner policy. No identity is parsed from prose.

MSBuild affected tests and --blocked-navigation: PASS=15 FAIL=0, no
compiler warnings (artifacts/wp2a-focused-structured-open.log).
The preceding complete gate (artifacts/wp2a-source-gate-3.log) passed
413 C# / 38 filesystem / 18 evidence checks with zero failures in those
components; it was deliberately stopped during archival purity hashing
to qualify this final contract on a new clean commit. No live action.
Rejected dependency: casting-specific automatic opening conditioned on
the wording/category of ReviewReason.

Exact next action: commit, rebuild prerequisite ZIP, complete unchanged
Test-SourceOnly, rebuild final clean candidate, then physical blocked-only
HUD qualification and exact installation/save restoration. Runtime
visibility remains unqualified. Earlier prerequisite identities are not
the final candidate.
## Checkpoint: last-problem repair under changing resources

Branch unchanged; exact pre-commit HEAD
7a63d6b6794f7259a7d123be4ab9936ec33073c0; version 0.3.0; dirty with this
focused lifecycle repair. The real compiler/authoring regression begins
with two Draft blockers and two ready castings. Explicitly disabling the
last Draft repairs it; refreshed depleted resources make the later ready
castings block. The old nearest-index fallback selected the middle of the
new three-problem list instead of its last problem.

Affected MSBuild rebuild / --blocked-navigation before repair:
PASS=14 FAIL=1 (artifacts/wp2a-last-refresh-red.log). Reconciliation now
remembers whether the repaired problem was last and selects the new last.
After repair: PASS=15 FAIL=0 (artifacts/wp2a-focused-final.log), no compiler
warnings. The complete gate at the preceding commit passed its 413 C#,
38 filesystem and 18 evidence checks, then was stopped during archival
purity hashing to qualify this strengthened lifecycle contract.

Rejected assumption: repairing one blocker always shrinks the collection.
A refreshed resource state may introduce other blockers; execution order
and the explicit last-problem rule still apply. No readiness predicate or
authoring operation was added to navigation.

Exact next action: commit this regression/repair, build the prerequisite
ZIP, finish the complete unchanged source/mechanical gate, rebuild the
clean candidate, and physically qualify blocked HUD navigation with exact
installation/save restoration. No live action has occurred; actual screen
visibility remains uncertain until that scenario passes.
## Checkpoint: first physical run and verifier contract repair

Branch codex/kbp-not-ready-deep-linking-2026-10-07; exact HEAD
783af83684146cbbd9abda13e5ac62dd3818fdf8; version 0.3.0.
The clean candidate completed scripts/Test-SourceOnly.ps1 with zero failures:
42 source / 413 C# / 38 filesystem / 18 problem evidence / 4 package /
5 deployment purity / 13 launcher purity / 3 fixture inventory /
16 rollback / 3 publisher-gate checks; suite PASS=1 FAIL=0.
No compiler warnings. Evidence: artifacts/wp2a-source-gate-5.log.
The archive comparisons read eight snapshots totalling about 322 GiB;
their assertions and targets were unchanged.

Build-Local after the full gate reproduced ZIP
d843cab807272c9702d8d4f86bc62827c525f15e13aa0b90f2b491121eb068b9,
DLL 8358d91b7d5839ef9d159a785ded467c58ee7c0865eaa4ea151946ac65f829d0,
MVID 89113394-8e2d-42d3-bd33-eac7eb9701e5. Clean build log:
artifacts/wp2a-final-build-783af83.log; manifest under artifacts/local-runtime/0.3.0.
This identity is preserved under artifacts/qualified-wp2a-783af83.

Exact scenario WhatIf passed, with no mutation. After the lab's measured
180-second input-idle guard and the owner's "Use the next idle window"
instruction, the owned launcher ran live-workspace-physical,
PhysicalExpectation problems, full-user, Automation, DisplayMode owner,
instant, timeout 1200, run wp2a-783af83-problems-01. No allowance was supplied.

Game result PASS; physical record violations empty. Physical moon opened a
cold closed planner at Long / Resistance / Linzi -> Hedwirg. The first late
Draft wp2a-blocked-late began masked offscreen; measured graph scroll moved
1.0 -> 0.115682006. Chip point (937.504,147.660172) on a 1920x1080 surface;
visible fraction approximately 1.0. The catalogue row and Long tab were
onscreen, inspector was at top, and the visible reason was:
"Not ready: this casting is a Draft; finish its choices and mark it Ready".
Actual Next and Previous clicks showed 1 of 2 -> 2 of 2 -> 1 of 2, with
correct boundary controls. Screenshots problem-1/2/3.png were visually
inspected (first at this checkpoint). Nested Escape cleared inspection,
then closed the planner and released the input lease. Document/profile,
all resource/effect signatures, Undo and review authorization stayed
unchanged; dispatch attempts and runs started both zero.

Outer orchestration FAILED solely at independent verifier schema handling:
successful OS acknowledgements omit deliveryFailed; the session intent
signature is campaign + U+0002 + normalized profile JSON, not SHA-256.
The original failed completion is retained and is not reported complete.
Restoration verified, game exited, protected saves compared clean, allowed
changed saves empty, transaction Restored. Evidence:
C:/Dev/KingmakerBuffPlannerLab/runtime-evidence/wp2a-783af83-problems-01/.

File-backed regression reproduced rejection of the actual valid contracts.
After repair, Test-ProblemNavigationEvidence PASS=23 FAIL=0 and independent
recheck of the original physical record PASS=1 FAIL=0
(artifacts/wp2a-actual-evidence-schema-green.log). Invalid/empty/unstructured
signatures and failed physical deliveries remain rejected. Exact string
equality and every visibility/native/save assertion remain mandatory.
Production C# is unchanged by this verifier repair.

Rejected assumptions: successful acknowledgements contain a false failure
flag; intent signatures are hashes; a profile signature is unprefixed JSON.
The verified ProfileIntentSignature implementation defines the representation.

Uncertainty: a fresh complete outer orchestration on the corrected verifier
is still required. Exact next action: commit the harness regression/repair,
build its prerequisite package, run the unchanged complete source gate to
zero failures, rebuild the clean candidate, repeat the physical blocked-only
scenario at the next measured eligible idle window, verify all restoration
and protected saves, then record/push the final evidence. No other package,
PR, merge, tag, release or permanent installation.

## Checkpoint: shared-lab qualification overlap, 2026-10-08

Branch codex/kbp-not-ready-deep-linking-2026-10-07; exact HEAD before this
documentation checkpoint 0d730845cb17e81a8279f405d11b0fd3f639ee68;
version 0.3.0; remote main remains b707c1f47859f2ecae517b2cd162d20ca18c7583.
The tracked worktree was clean and the focused remote matched HEAD at the
continuation audit. No Work Package 1 changes or other work package.

Command: scripts/Test-SourceOnly.ps1 under Windows PowerShell 5.1.
Gate 6 (artifacts/wp2a-source-gate-6.log) was interrupted, wrapper exit
1073807364, during launcher archive purity; the gate did not complete.
Gates 7 and 8 (matching numbered logs) each passed source 42 / C# 413,
then refused a foreign Kingmaker process and exited 1.
Gate 9 (artifacts/wp2a-source-gate-9.log) passed source 42 / C# 413 /
filesystem 38 / problem evidence 23 / package 4, then deployment WhatIf
refused foreign Kingmaker PID 11240; exit 1. No compiler warnings.
These are incomplete qualification attempts, not full-gate passes.

The unchanged full gate previously passed at clean 783af83 with suite
PASS=1 FAIL=0 (artifacts/wp2a-source-gate-5.log). Its archive checks read
eight snapshots, about 322 GiB, in approximately six hours. The Gunslinger
lab's repeated launches make brief idle gaps inadequate. No assertion,
archive target, safety policy or ownership control was weakened.

Current prerequisite build on clean 0d73084:
artifacts/wp2a-prerequisite-build-0d73084.log;
source PASS=42 FAIL=0 / build PASS=1 FAIL=0 /
package PASS=4 FAIL=0 / local build PASS=1 FAIL=0.
ZIP 842999863887d09b329c335196986dc6d299ae56486655bf0d2825dc4b1fc77f;
DLL e78d0df1fb566d4dfa3edb6060d07f08455b6ebe86143264b6389808635c9a0f;
MVID a7ae5eb7-d2ad-4b2c-ab36-25f1a1db3a15.
Manifest: artifacts/local-runtime/0.3.0/
KingmakerBuffPlanner-0.3.0-local-runtime.zip.build-local.json.
This prerequisite is not the final post-gate candidate.

The earlier physical run and its original failed outer completion are
preserved under C:/Dev/KingmakerBuffPlannerLab/runtime-evidence/
wp2a-783af83-problems-01/. Its game PASS, actual visible card/reason/
Problem 1 of 2, physical Next/Previous/Escape, exact intent/profile equality,
zero dispatch and unchanged native signatures remain evidence. The
corrected independent recheck passed; the failed completion was not edited.
Restoration verified and protected saves compared clean. Continuation
audit found every planner deployment transaction Restored (399).
No new physical trial or live deployment occurred during these overlaps.

Rejected assumption: a short gap between foreign trials is enough for
the full archival purity gate. Uncertainty: when the other lab will finish,
and whether fresh complete orchestration will pass on the corrected
verifier. The owner's instruction remains "Use the next idle window".

Exact next action: preserve both labs' legitimate state; wait for sustained
quiet without foreign ownership; build a clean prerequisite for the
documented HEAD; complete unchanged Test-SourceOnly with zero failures;
build the final clean candidate and record its exact identity; run the
guarded blocked-only physical scenario after measured input idle >=180s;
verify actual visibility, no native effects or debits, complete outer
orchestration, exact restoration and protected saves; record final evidence
and guarded-push the clean branch. No PR/merge/tag/release, permanent
installation or later work package. Current runtime qualification remains
pending.

## Checkpoint: gate 10 refused by foreign runtime; handoff remains pending

Branch codex/kbp-not-ready-deep-linking-2026-10-07; exact pre-documentation
HEAD f031f87e71aca99c09118495b9659e24bdcaace1; version 0.3.0.
That commit was clean and pushed using the owned helper. Documentation
validation PASS=42 FAIL=0; guarded push helper PASS=6 FAIL=0.
Logs: artifacts/wp2a-continuation-doc-validation.log and
artifacts/wp2a-continuation-guarded-push.log.

After twenty observed quiet minutes, unchanged scripts/Test-SourceOnly.ps1
started at 2026-10-08T16:02:37.8228860Z under Windows PowerShell 5.1,
PID 8828. Source PASS=42 FAIL=0; C# PASS=413 FAIL=0; filesystem PASS=38
FAIL=0; problem evidence PASS=23 FAIL=0; package PASS=4 FAIL=0.
At the deployment WhatIf boundary the guard refused foreign Kingmaker
PID 5420. Wrapper exit 1; complete gate NOT passed. No compiler warnings.
Evidence: artifacts/wp2a-source-gate-10.log and
artifacts/wp2a-source-gate-10-start.json.
The foreign lease identified
runtime-20261008T163555Z-c8ea60b6503940a6b04467deafc9d927.
No new WP2A live deployment, physical input or save mutation occurred.

Clean prerequisite identity, not a final qualified candidate:
commit f031f87e71aca99c09118495b9659e24bdcaace1;
package artifacts/local-runtime/0.3.0/KingmakerBuffPlanner-0.3.0-local-runtime.zip;
ZIP 249b2d5a10e233b412285af36a31fe07716c9fdd08a1696123968246a53a5449;
DLL ae95357a5699ec38fd149f0416828756e0d6567cd1cbb483ac5ebc43242bb4b7;
MVID f0ca5ab8-0325-41c7-9781-b08df7b4dc8c;
manifest at the package path plus .build-local.json.
Build log artifacts/wp2a-prerequisite-build-f031f87.log:
source 42 / build 1 / package 4 / local package 1, all zero failures.
Production C# remains identical to the fully source-qualified 783af83.

The preserved first physical record contains exact before/after document
equality (15,082 characters). Its UTF-8 SHA-256 fingerprint is
e685c17f9a52edd1654e0b022934142fd8ef05c356783664ffdbf7462edcb262
both before and after; the actual signature remains campaign + U+0002 +
normalized profile JSON, not this reporting fingerprint. The profile file
SHA-256 stayed 46c5c9a25864a0990504a553995bc8188c8f492123edfb0861b4d8554cf8ddea.
Physical moon opened Long / Resistance / Linzi -> Hedwirg at
wp2a-blocked-late: chip (937.504,147.660172), visible fraction ~1.0,
catalogue (197.776047,626.4199), inspector at top. Graph 1.0 -> 0.115682006;
the chip was masked before reveal. Problem 1 of 2 -> 2 of 2 -> 1 of 2,
with correct boundary controls. Raw reason unresolved-saved-request was
translated to "Not ready: this casting is a Draft; finish its choices and
mark it Ready". Dispatch attempts/runs started 0; native signatures,
Undo and authorization unchanged. The original failed outer completion,
positive game result, independent recheck and verified restoration remain
separate facts. They are not promoted to a fresh complete run.

Self-review of the implementation:
1. No casting identity/order comes from BlockingReasons, ReviewReason or
   human-readable refusal prose. Existing global opening-category policy
   remains; it extracts no casting identity.
2. Structured CastingBlocker entries follow compiled execution order.
3. Disabled/already-satisfied omissions are outside the blocker list.
4. Planner and HUD use the same WorkspaceApplyResult/session Apply boundary.
5. FocusGraphCasting is the canonical source/routine/focus operation.
6. Selected filtered rows remain included; unavailable saved buffs retain
   a non-authoring reveal placeholder. Physical late-card reveal is proven.
7. Reveal is consumed once; unchanged refreshes preserve scrolling.
8. Manual selections, Done and nested Escape leave problem mode.
9. Navigation does not alter document/profile, Undo, save/review state or
   execution/resource state. Real-service regressions and physical equality
   cover these boundaries.
10. Global refusals create no casting navigation request.
11. Repairs recompile/regate; retain a remaining current blocker or move to
    the next/new last, and exit when none remain.
12. Ready Casts Only keeps the original omissions and enhancement policy.
13. A new successful submission clears pending navigation/reveal.
14. The first physical record has clipped screen points, substantial card
    overlap and inspected screenshots. A fresh complete candidate run is
    still pending; focus ID alone is not treated as visibility.
15. The previous live installation was restored; all 399 own deployment
    transactions were Restored at the continuation audit.
16. The WP2A physical trial compared protected/ordinary owner saves clean;
    no allowed changed saves, violations or blocking records. This source
    attempt made no live save changes.

Rejected assumption: a twenty-minute quiet interval proves the other lab
has finished. Uncertainty: a sustained installation window and a fresh
complete run on the corrected verifier. Implemented and regressed;
full source qualification of the current candidate and runtime
qualification remain pending. Not native-qualified, released or
owner-accepted.

Exact next action: wait for the other lab to finish or owner coordination;
do not seize its lease, kill its process or rerun between short gaps.
On a confirmed quiet window, build a clean prerequisite for the documented
HEAD, finish unchanged Test-SourceOnly with zero failures, build the final
clean candidate, record/freeze exact identity, run the guarded problems
scenario after measured desktop input idle >=180 seconds, verify actual
visibility and complete outer orchestration, restore/reconcile/protect
saves, record evidence and guarded-push. No PR, merge, tag, release,
permanent install or later work package.

## Checkpoint: engineering-review P2 and P3 corrected, qualification pending

Branch codex/kbp-not-ready-deep-linking-2026-10-07; exact pre-correction
HEAD 8d4e4f3c6f9810a1b02606f82bf1722f5256b9cf; version remains 0.3.0.
Origin/main was fetched and remains b707c1f47859f2ecae517b2cd162d20ca18c7583;
the focused remote matched the clean starting HEAD. No open pull requests.
Original checkout and WP1 state were preserved; no branch was inherited.

The review identified a real focus-ownership defect: Duplicate moved the
inspector to the copy while retaining the original problem; Reload cleared
focus while retaining the old navigator. A surviving blocker then prevented
reconciliation from restoring focus. Both could hide the problem controls
while the footer still reported an active problem. This corrects the earlier
self-review's assumption that all deliberate focus replacements left mode.

Implemented correction: DuplicateFocusedCasting and Reload explicitly leave
problem navigation before operating. The refresh boundary checks that an
active Current.CastingId equals EditingFocusCastingId. A lost focus is restored
through FocusGraphCasting and requests one reveal; later unchanged refreshes
do not reclaim scrolling. Duplicate retains its existing authored copy,
autosave and one Undo entry. Reload does not resurrect navigation; a new Run
starts from the current stored blocker set.

P3 is resolved, not waived. CastingLabel keeps direct-target display names
and now identifies a known group origin as "centered on <party name>" from
the casting's explicit Origin. Caster-centered origins use the saved caster;
anchored origins use the saved anchor. An origin absent from the current
party snapshot remains "group". No caster, provider or readiness is guessed.

Behavioral regressions use the real gate, compiler, session, authoring service
and temporary repository files. The five additions cover duplicate-current-
problem, Reload with the same blocker, Reload with different blockers, active
navigator/inspector consistency for both missing and wrong focus, and two
same-spell group blockers with different named anchors plus caster-centered,
unresolved-origin and direct-target cases. Navigation/recovery assertions
cover exact document intent, file bytes/timestamps, Undo, review authorization
and zero dispatch. Duplicate separately proves its existing authored autosave
and exactly one Undo operation, with no extra write from inspection.

Commands and exact outcomes:
- MSBuild tests/KingmakerBuffPlanner.Tests/KingmakerBuffPlanner.Tests.csproj
  /t:Rebuild /p:Configuration=Release /m /nologo /v:minimal.
- artifacts/tests/KingmakerBuffPlanner.Tests.exe --blocked-navigation:
  before production corrections PASS=15 FAIL=5, one failure for each finding;
  final corrected suite PASS=20 FAIL=0.
- artifacts/tests/KingmakerBuffPlanner.Tests.exe: PASS=418 FAIL=0.
- scripts/Validate-Source.ps1: PASS=42 FAIL=0; evidence
  artifacts/wp2a-review-source-validation.log.
- No compiler warnings. Evidence: artifacts/wp2a-review-regressions-red.log,
  artifacts/wp2a-review-regressions-green.log and
  artifacts/wp2a-review-complete-csharp.log.

An intermediate 19/1 result exposed a new test's incorrect assumption that
Duplicate does not autosave. The assertion was corrected to preserve the
existing authoring contract; the production persistence behavior was not
changed. The original red Duplicate regression failed earlier at the missing
navigation exit, so it detects the reviewed defect independently.

Full diff review: the corrections add no refusal-string parsing, readiness
predicate, second compile path, persisted navigation, authorization or native
submission. Structured blocker order, omissions, shared HUD/planner Apply,
manual exits, global refusal and Ready Casts Only policy are unchanged.
No production C# source qualification from 783af83 is transferred to these
new production changes. Implemented and regressed; complete final source gate
and fresh successful physical runtime qualification remain pending.

Ownership observation at 2026-10-08T18:27:28.6382466Z: the Gunslinger lab held
its compatibility lock for runtime-20261008T182340Z-f34335fa85ba4580ac604cf52c57b938
and Kingmaker PID 17384. No foreign process or lease was touched. No new WP2A
live staging, physical input or save mutation occurred. The previously
restored trial and its original failed outer completion remain unchanged.

Exact next action: commit and guarded-push the focused review correction,
build a clean prerequisite for that exact commit, and obtain a sustained
quiet installation window. Then run unchanged scripts/Test-SourceOnly.ps1
with zero failures, build/freeze the exact post-gate candidate, and run a
fresh candidate-bound blocked HUD trial through the corrected verifier after
measured desktop input idle >=180 seconds. Verify complete outer success,
actual masked card visibility, unchanged document/profile/native state,
restoration and protected-save integrity. Do not rerun the expensive full
gate between short foreign launches, seize ownership, merge, tag, release,
create a PR, permanently install or start another work package.
