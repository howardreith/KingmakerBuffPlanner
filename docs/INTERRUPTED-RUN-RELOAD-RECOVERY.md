# WP1 interrupted-run and reload recovery — owner closeout

Closed at the owner's request on 2026-10-07. Branch `codex/kbp-interrupted-run-reload-recovery-2026-10-07`; checkpoint/tested HEAD `4480e1529532321f82e639349e40a4145da75a34`; public version remains 0.3.0. A documentation-only commit follows this tested candidate.

The owner tested again and reports the issue no longer appears. No new released-product defect was demonstrated here. Stop/reload and active-cast cancellation/reload both recovered in Instant and Animated mode. No production recovery repair was selected; no blind reset or Resume call was added. The investigation is closed, not paused awaiting more tests.

Implemented: guarded diagnostic reproduction/evidence and a repository-owned guarded push helper. Regressed: two defects in the **new diagnostic judge**, not a demonstrated player lockout. Focused source/protocol checks passed; the complete current source gate was interrupted by the owner closeout. Native-qualified scope is only the four named diagnostic chains and mandatory candidate gate below. No recovery fix, release or candidate owner acceptance is claimed; the exact artifact in the owner's latest personal test was not specified.

## Investigation and clarification

The worktree was created from freshly fetched `origin/main` `b707c1f47859f2ecae517b2cd162d20ca18c7583`, released 0.3.0, with no newer main changes and zero open PRs at inspection. Historical casting/everyday-use worktrees were preserved. No foreign runtime lease, unresolved KBP transaction or game process existed before live use.

The production trace covered HUD routine press -> quick controller -> root-owned campaign casting session Apply -> durability barrier -> compiler/gate/immutable projection -> native dispatch -> single host/coordinator -> Hybrid/Instant/Animated iterator/adapter -> cancellation/disposal -> completion/session reporting -> save/area events -> HUD reconstruction -> second press. Main, session owner/recovery store, discovery refresh and enhancement cleanup were inspected. Area unloading already uses Cancel rather than permanent Shutdown. The passing chains showed no stale host, pending callback, campaign identity or HUD ownership.

The owner clarified that the observed trigger was **quickload during casting, in both modes**, probably with **Share Transmutation and rods**. This is more specific than the ordinary finite casting fixture. The installed game's exact `Game.LoadQuickSave` IL refreshes the catalog, selects `SaveManager.GetNewestQuickslot`, then calls `Game.LoadGame`. The selection filters quickslots to the current campaign (or the configured ironman slot). The guarded chains used Game.LoadGame with the exact approved manual WORKING descriptor; the physical quickload key/selection path was **NOT RUN**. No key was pressed that could select an ordinary save. [Read-only contract evidence](evidence/wp1-quickload-contracts.txt), Assembly-CSharp SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Broad reflection enumeration reported six missing type dependencies; the listed resolved methods were inspected without executing game code. Detached IL inspection is not native qualification.

Share/rod disposal and provider-transaction cleanup were traced after that clarification. The approved Advanced fixture has existing Share/rod coverage in historical qualification, but this mission did not execute an interrupted armed-lease chain before the owner closed it. Potential retained provider transactions, fallible completion before pending-clear, cross-campaign callback binding, and native-command cleanup remain **unproven hypotheses**, not reported new defects. Why restarting cleared the earlier owner observation remains unknown.

## Changes and preserved boundaries

`CastingQualification.cs` and `CastingQualificationDriver.cs` add exact `stop-reload` / `cancel-reload` recipes and terminal/effect/resource evidence. `RuntimeTestHost.cs` uses the existing root-owned session/host, installed HUD pointer event, exact-bound qualification allowance and guarded read-only loader. Runtime-only getters/HUD entry in `BuffPlannerUiRoot.cs` and narrowly locked qualification arming in `NativeCastingSessionPolicy.cs` / `CastingWorkspaceSession.cs` expose the ordinary production route without adding another writer, host or executor. The launcher/common/allowance scripts admit only the two exact recipes. Eight protocol cases exercise real owners and iterators with narrow native/Unity seams. The new repo-owned push helper preserves the lab helper's clean-tree/origin/secret/package/unfinished-Git/ancestry/fast-forward guards and permits only the owner's exact branch in addition to its prior branch scope; the original lab helper is unchanged.

Deliberately unchanged: normal host/coordinator/executor/adapter lifecycle, shutdown/resume policy, discovery and plan/resource rules, enhancement cleanup, projection identity, persistence/Undo recovery, public version, blueprints, optional mods and gameplay-mod dependencies. No retry, buff injection, rollback claim, singleton workaround, second execution owner or campaign-profile overwrite was introduced. The second diagnostic request deliberately disables the first casting in the staged test-owned plan and casts the remaining intent; it is not an automatic retry.

The existing lifecycle distinguishes Stop (finish the current terminal, report/dispose once, idle/accepting), unload Cancel (immediate owned terminal, preserve spend/effects/uncertainty, release UI, refresh after activation), deliberate Shutdown (refuse until explicit enable/Resume), and root teardown (terminal plus session flush/recovery). No new Resume was added. The diagnostics accept only the exact Animated cancellation entry `Cancelled:cancelled-in-flight;last:FailedExecution:animated-operation-abandoned-in-flight`; any additional stopping failure, ResidualStateUnsettled or cleanup failure still blocks a second request.

## Tests and gate status

Focused `artifacts/wp1-4480e15-protocol.log`: **PASS=406 FAIL=0**. Eight new cases cover fresh-world Stop/reload, refusal of unobserved reload, repeated in-flight Cancel, root session/controller entry, uncertain cleanup refusal, expected Animated iterator cancellation and retained later cleanup failures. They use the real host, coordinator, Instant/Animated iterator, compiler/gate/converter, session/recovery owner and temporary plan files, with narrow native-state boundaries.

The new judge initially hid retained ResidualStateUnsettled: red 403/1, green 404/0. It then rejected expected Animated abandonment: red 405/1, green 406/0. Later residual failures remain rejected. Logs and hashes are in [the evidence snapshot](evidence/wp1-recovery-20261007.json). Those red/green proofs concern prior **harness** mechanisms; no regression of the reported released-product lockout exists.

Complete gate command: `.\scripts\Test-SourceOnly.ps1`. The final exact-head attempt was stopped at owner request during exhaustive deployment WhatIf snapshot hashing. Completed stages: Source validation 42/0; Protocol tests 406/0; Runtime harness filesystem tests 38/0. No completed-stage failure or compiler warning occurred; process termination returned exit 1 and **is not a completed PASS**. Log: `artifacts/wp1-4480e15-source-gate.log`. The earlier full safety run completed with zero failures (`wp1-baseline-source-gate.log`), initially compiled protocol 398; it must not be relabeled as the complete final-candidate gate. Current complete source qualification is **NOT CLAIMED**.

`Build-Local.ps1` at the tested clean HEAD passed source/build/package/local checks 42/1/4/1, zero failures and no compiler warnings (`wp1-4480e15-build.log`). Guarded push checks passed 6/0 before closeout. Exact diagnostic candidate:

- commit `4480e1529532321f82e639349e40a4145da75a34`;
- package SHA-256 `8c593e082f24acc4d4441b8d76dec4766f9135f35e4b0980450afbab3b55b168`;
- DLL SHA-256 `4b0e26229e62c340089bec76bf30c20f1d468dfaed06b76cf44d846eaf80491c`;
- MVID `1b767261-afa8-4f3e-9022-3166fe884a08`;
- worktree ZIP `artifacts/local-runtime/0.3.0/KingmakerBuffPlanner-0.3.0-local-runtime.zip`;
- immutable ZIP/manifest/freeze directory `C:/Dev/KingmakerBuffPlannerLab/runtime-backups/qualification-frozen/4480e1529532321f82e639349e40a4145da75a34/`;
- manifest `KingmakerBuffPlanner-0.3.0-local-runtime.zip.build-local.json`, SHA-256 `373c029a05a408c63415c88bfe7863cf5db9713ab4f9dd8d07a3e94f92e80bd6`.

## Native observations

Evidence root `C:/Dev/KingmakerBuffPlannerLab/runtime-evidence/<run-id>/`; [snapshot](evidence/wp1-recovery-20261007.json) captures exact artifact identity, native outcomes, effect/resource reads, terminal/callback counts, evidence SHA-256 and restoration records.

| Final-candidate run | PASS/FAIL | PID |
| --- | ---: | ---: |
| wp1-4480e15-cancel-animated-01 | 85/0 | 40980 |
| wp1-4480e15-cancel-instant-01 | 85/0 | 33204 |
| wp1-4480e15-cancel-select-01 | 85/0 | 39116 |
| wp1-4480e15-core-01 | 87/0 | 7748 |
| wp1-4480e15-stop-animated-01 | 85/0 | 18400 |
| wp1-4480e15-stop-instant-01 | 85/0 | 13904 |
| wp1-4480e15-stop-select-01 | 85/0 | 40756 |

Selection runs never cast. Core verifies candidate integration/identity, not gameplay. Each of four casting chains performs start -> Stop or active-load Cancel -> exact approved save reload -> second routine in **one process**, with no relaunch between steps. The installed HUD entry is programmatic raycast/pointer dispatch, not physical mouse input.

| Chain | First observed terminal/state | Second observed casting |
| --- | --- | --- |
| Stop Instant / Animated | player-stopped; first EffectConfirmed/new instance, spontaneous 6->5; remaining NotProcessed/unspent | EffectConfirmed/new instance, prepared 1->0 and exact token True->False |
| Active-load Instant | area-unloading; first Cancelled, submitted/spent, spontaneous 6->5, no confirmed effect; remaining NotProcessed/unspent | EffectConfirmed/new instance, prepared 1->0 and token True->False |
| Active-load Animated | area-unloading; first Cancelled with exact expected abandonment, submitted/unspent, 6->6; effects absent, remaining NotProcessed | EffectConfirmed/new instance, prepared 1->0 and token True->False |

Exact prepared token `level-1|type-0|index-5`; distinct projections `16fc546b9bc4231ce6fb9b1e0dac4a4ac62af8e1069bcc403f43f1278a285234` and `ae707cae600c3f4d3812074d1dffcae54eef087ff203dceb2e7032d22608064e`. Save loading replaces world state normally; no cancellation rollback is claimed.

All four final outcomes have two starts/two reports, callbackFailure=null, empty cleanupFailures/violations. After reload: idle/accepting host, no shutdown reason, pending completion false, one completion owner, one planner root, one HUD and one lifecycle subscription. The authoritative session matches campaign `cb1f405d-0b0c-4281-8b76-3b4f3b690abb`; same-campaign session was reused while native Player reference changed. Load/unload and completion correlation were observed, writesObserved=false, and second Apply used fresh discovery. This mixed-caster fixture does not prove same-caster lease isolation.

Earlier failed diagnostics remain failures: Automation finite selection `wp1-71cce84-stop-select-01` 83/2 (no eligible finite source; no cast), and `wp1-71cce84-cancel-animated-01` 84/1 (new judge rejected expected abandonment before second request). Both restored and kept saves clean; success-completion=false. Other earlier 71cce84 chains passed, but the final table binds 4480e15 only. Earlier default run-count fields are not evidence of zero actual host runs.

## Safety, scope and self-review

Approved Advanced fixtures were read-only: `Manual_308_KBP_ADVANCED_BASELINE.zks` SHA-256 `6b5b7ea0ee36aca4b5ad01dc585aae28062c6d10e0b77c844f0eaffd5d182f81` and `Manual_309_KBP_ADVANCED_WORKING.zks` SHA-256 `9787613753f38abf08dceeac8f7a66a6b0b1a5cb28ee0a7cd8805f0f265f1e7d`; profile `advanced-gunslinger-0136`, identity `088553867688dd3c09a5c5ad593d551682ed0977a5ed5db3f0163d6305fc02b7`. Automation pair was only used for refused selection. No protected/ordinary save or foreign mod was changed or fabricated.

Every applicable attempt recorded protectedSavesClean=true and restorationVerified=true, including failed diagnostics. Original live planner **0.1.1-rc3** and prior Mods tree were restored transactionally; candidate is not installed. No unresolved transaction, foreign lease or Kingmaker process remained at closeout. Existing claims/records were preserved; no active foreign transaction was interfered with. The only stopped process was the positively identified, childless source-gate PowerShell (10504); no live deployment was active. No PR, merge, tag, release, asset replacement or version bump occurred.

**NOT RUN:** physical quickload key and safe quickslot binding; interrupted armed Share/rod/Powerful Change/sticky-touch state; distinct map/door transition; main-menu/cross-campaign replacement; native Stop/restart without reload; deliberately repeated full lifecycle callbacks/mod enable/root teardown; owner's exact routine and same-caster isolation. Repeated Cancel is source-tested; native root/HUD/subscription counts were observed. These limits prevent a recovery-complete verdict.

Self-review: (1) no stale state demonstrated; (2) reload refreshed the tested world; (3) restart mechanism unknown; (4) host/root/session/adapters own the investigated states; (5) only diagnostic-owner changes were justified; (6) no second/concurrent route; (7) deliberate Shutdown unchanged; (8) cross-campaign callback isolation unproved; (9) no recovery discard of intent/Undo; (10) cleanup uncertainty remains explicit; (11) regressions fail prior harness mechanisms only; (12) second real effects confirmed; (13) native resources/tokens observed; (14) installation restored. No source-only or detached reflection evidence is presented as gameplay qualification.

## Closure and possible future trial

The owner's latest successful personal test closes this task; no further trial or WP2 work is scheduled. If the report returns, the smallest useful sequence is the same routine in Animated mode -> quickload while a command is active -> wait for area/HUD -> routine again, in one process, with real effect/resource confirmation and a retained UMM log. First prove that quickload selects an approved disposable target; do not press it against ordinary/protected saves. Include the selected Share/rod intent and exact installed DLL identity. Reopen reproduction before selecting any production repair.

Commits before records-only closeout:

- 27cfc4b72c86165bd1010c32a880f9da33f7c540 test: add guarded same-process stop and reload reproduction
- 8c75d4834ba6a923127477fb386749ab9e812aaa test: trace guarded reload during an active casting
- 23b2f1e7055665ee3b2c9a7a62ad56ad055ebc19 chore: guard pushes for the owner-named recovery branch
- f343fbe114138c5955d838e5a7aaad715c8928e9 test: reproduce recovery through the owned HUD routine path
- 71cce84b2b82c515db3dca35a5efd8adb61e31d7 test: reject uncertain cleanup in reload qualification
- 4480e1529532321f82e639349e40a4145da75a34 test: distinguish owned animated cancellation from cleanup failure
