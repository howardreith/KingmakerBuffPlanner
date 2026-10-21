## Published 0.6.5-alpha.1 controller navigation preview — 2026-10-20

Owner-requested prerelease `v0.6.5-alpha.1` is published from PR #14 source
`1f5d1af5c8ea3f92bc41e0e3165ecef57cdf5341`. ZIP SHA-256
`6d8e4985dbecb7355fff4a73aea16f284a17ab60b91a1cbf917aa273ec8f0cd1`;
DLL SHA-256
`b39620ed4f71e303e19acfd5211d8765176781c095837a4ef09e213ced7798f0`.
The clean-clone publisher passed source 47/0, offline C# 597/0, publisher
gate 8/0, two deterministic builds and package 6/0. GitHub download matched
the local ZIP; stable 0.6.4 remains Latest. This exact alpha has NOT been run
in the game. The [release record](docs/RELEASE-0.6.5-ALPHA.1-RECORD.md)
separates tag identity from this later documentation commit. Exact next action:
owner's focused native test of six-second Spells focus, spatial movement and
text-only Inspector scrolling, with a matching UMM/Unity log. Do not infer a
merge or stable release from the alpha.

## PR #14 Inspector scroll review correction — 2026-10-20

On `codex/kingmaker-buff-planner-controller-navigation-review`, source commit
`fec94dfd7b548eb7c093d6f2fb4404caaf435ed6` addresses the review of `1116309`: right-stick scrolling through a
text-only Inspector notice section now retains pane-level reading focus, and
the following frame no longer reveals the offscreen acknowledgement button.
Confirm/Inspect cannot act on that button until it is visible. Version 0.6.4.
The source-stamped Windows selector `Test-Development.ps1 -Only
'controller-workspace-'` passed source 47/0, build 1/0, focused tests 19/0
in 6.3 seconds. DLL SHA-256
`128da9e32a38dd00fb48930a64979c0c943af0ce27a7af314b61a79ce1ddfac2`;
local ignored log `artifacts/validation/controller-navigation-review-fec94df.log`.
The first focused run was 18/1 on an existing empty-scope message assertion;
the final behavior retains that contract. Native rendered/controller acceptance
is NOT RUN. Exact next action: commit this documentation checkpoint, guarded
push to PR #14, then follow the [focused native checklist](docs/CONTROLLER-NAVIGATION-REPAIR-2026-10-20.md)
before claiming the controller issue is resolved.

## Controller navigation Windows integration — 2026-10-20

Owner-requested PR preparation uses branch
`codex/kingmaker-buff-planner-controller-navigation-review` from current
`origin/main` `6914ef2`. Handoff source/documentation became `0887225` and
`8acf31c`; version is now 0.6.4 from main. The exact Windows development
selector `Test-Development.ps1 -Only 'controller-workspace-'` passed source
47/0, production build 1/0, and focused tests 18/0 in 10.5 seconds. DLL SHA-256
`aac0ca90bf9f5574cf25da9e6748085020558725bb9cce35b2b9098d7851472b`;
local log `artifacts/validation/controller-navigation-development.log`.
The earlier 16/0 portable handoff result remains historical. Native rendered
focus and controller acceptance are NOT RUN. Exact next action: finish the
reviewable documentation checkpoint, use the guarded push helper, open a PR to
main, then perform the [focused native checklist](docs/CONTROLLER-NAVIGATION-REPAIR-2026-10-20.md)
before claiming visual acceptance. Do not relabel the published 0.6.4 binary.

## Controller navigation source repair — October 20, 2026

Owner video confirms logical Spells focus with no visible marker at roughly
0:09.25–0:14.5. Local branch
`codex/kingmaker-buff-planner-controller-navigation`, product source
`e9dee2156de48a60e64f283fea4a4d3416e316c6`, base `df61617`. Spatial navigation,
retained horizontal aim, persistent opaque focus border, and visible scroll
focus are implemented. Focused pure C# **16/0**, changed C# syntax **6/0**;
full production build and native visual/controller acceptance **NOT RUN**.
Version remains 0.6.4-alpha.1; no DLL, install, save change, push or release.
Windows device and external guarded push helper unavailable. Transfer the source
bundle to an isolated Windows checkout, run the existing focused production
selector, then the [specific native checklist](docs/CONTROLLER-NAVIGATION-REPAIR-2026-10-20.md).
Do not relabel old release or runtime evidence as validation of this repair.

## 0.6.3 release published — 2026-10-20

Full v0.6.3 is GitHub Latest at tagged product
`f82555c2c52d2580b187be7006555c855a8862fe` (version 0.6.3).
Owner-authorized pre-runtime checks: source 47/0, offline C# 582/0,
publisher 8/0, deterministic builds 2 and package 6/0. ZIP SHA-256
`ee1ca6c22e684514a0ecd880d4c896160361b04a4484f905cdbe326f653d56c7`.
No game, install, save, or native F02 run occurred. See
[release record](docs/RELEASE-0.6.3-RECORD.md). Exact next action:
guarded native no-cast RT/R2 card → editor → Back → repeat observation,
then active-run card → quick controls → Stop, recording diagnostic stages.

## Full release 0.6.2 published and verified — native Linux acceptance pending

PR11 MERGED; guarded main product `6090e6232c9dcec7d8e890585aef8976ef87c70a`.
Owner-authorized pre-runtime publisher source47/0, complete offline C#579/0,
publisher8/0, two deterministic builds and final package6/0. Full/non-draft/
non-prerelease GitHub Latest v0.6.2 published2026-10-20T12:11:41Z. Annotated
tag peels to exact product; downloaded ZIP and checksum verified. ZIP SHA256
`b9a465691a07d8927b9cf46b66da57167659284282c8da551c483ed3a3dddfb1`;
DLL `d3cd91b2c76f2f8a4ccefeea77b96aee9ee80dfc16532aee7250d6e43972dcfb`.
No native Linux 2.1.5d environment/gameplay or Windows game regression was
run; Linux compatibility **NOT RUN / unverified**. No installation, other
mod, save, or settings mutation. [Full receipt](docs/RELEASE-0.6.2-RECORD.md)
and [manual trial](docs/LINUX-2.1.5D-CANDIDATE.md). This documentation HEAD
postdates the tagged product; do not rebuild or relabel the published ZIP.
Exact next action: Linux user's disposable-campaign test and first logs if
any step fails.

## Public Linux 2.1.5d trial prerelease — native acceptance pending

Owner-authorized `v0.6.1-linux.1` published as prerelease, not Latest,
2026-10-20T03:27:08Z. Tag peels to product `be68ca0ef00137e5fef344b4ea43ad81d073fa96`;
ZIP SHA256 `adf4c974e0579675665a35968f3f12ebbf6c5ac15b2aa59f373990b7e8b82942`,
DLL `b513a28fb3d65e89f34f145a806ca7867ace267a09e4e5fb2eee1856d8f3489e`.
Offline source47/0, C#579/0, publisher8/0, two deterministic builds and
final package6/0. Published assets downloaded and hashes verified. Initial
publisher stopped on pre-existing local tag conflicts before qualification;
fresh clone of exact pushed HEAD completed. No native Linux environment or
gameplay run, Linux acceptance NOT_RUN. Stable0.6.0 Latest and draft PR11
preserved, no merge. [Full receipt/manual test](docs/LINUX-2.1.5D-CANDIDATE.md).
Next action: Linux user's disposable-campaign trial and first exception/logs.

## Linux 2.1.5d experimental candidate — native acceptance pending

Branch `codex/kingmaker-buff-planner-linux-2.1.5d`; product commit
`2d30ff970fa65f9a1f1422e4eef15d854511c73b`, version 0.6.1. Manifest-only
diagnostic retains published 0.6.0 DLL; versioned candidate ZIP
`2f0b1247aa3a12935c6c077d6117fc423ce5402c0025407110b811660d0f37b7`
passed deterministic build and strict package checks. No native Linux 2.1.5d
installation/assemblies available; Linux acceptance **NOT RUN**. No Windows game
run; C# logic unchanged. Exact package paths, commands, pass/fail counts,
manual procedure, rejected controller theory, and next action are in
[the candidate record](docs/LINUX-2.1.5D-CANDIDATE.md). Existing 0.6.0 public
release and historical controller evidence remain unchanged.

## Current owner-authorized 0.6.0 full release — DONE

PR10 MERGED; main product c98b43446c13534a2d165b6ca18a264602957758. Full GitHub
release v0.6.0 published02:52:33Z, non-draft/non-prerelease and Latest; annotated
tag peels to exact product. Complete offline579/0, source47/0, publisher8/0,
two deterministic release builds/package checks passed via unchanged established
OwnerAuthorizedPreRuntime route. Downloaded ZIP/checksum verified. No native
acceptance asserted; F01-F10 remain NOT_RUN. Old artifacts/occupied main worktree
and separate Alpha4 safety process preserved. [Exact receipt](docs/RELEASE-0.6.0-RECORD.md).
This documentation HEAD is separate from compiled/tagged c98b434; do not rebuild
or relabel the existing release. Await controller usability evidence/next owner
direction; no unsolicited new release, gameplay-mod repair or permanent install.

## 0.6.0 preparation checkpoint (historical)

Owner now explicitly requests merge to default branch, push and a full release
with a new version. Use existing OwnerAuthorizedPreRuntime publisher disposition,
not ConfirmHumanAcceptance; F01-F10 remain NOT_RUN. Selected0.6.0/CLR0.6.0.0.
Base5276b21. Preserve occupied main worktree and old alpha artifacts/process26124.
Prepare/test feature version commit, guarded push; isolated clone main merge,
guarded -AllowDefaultBranch push; unchanged guarded publisher. No permanent
install, game/save work or unrelated mod changes. [Release record](docs/RELEASE-0.6.0-RECORD.md).

## Historical full-controller implementation checkpoint

**Alpha4 published** 2026-10-18T02:18:48Z; product69a48a4, non-Latest prerelease,
downloaded hashes verified. [Publication/check record](docs/CONTROLLER-ALPHA4-PUBLICATION.md).
Broad check stopped577/1 before purity on brittle Classic source adjacency;
matched Alpha3 fails once too. Corrected compiled-order reader has missing/late/
wrong-value negatives, focused39/0. Test-ControllerAlpha4Safety runs that corrected
reader and all remaining original guard children with separate harness identity.
Original FAIL and frozen Alpha4 bytes remain preserved; no native PASS.
First corrected-reader sequence passed C#579/0, arguments28/0, source47/0,
package6/0, then failed its synthetic retry fixture because the wrapper applied
its real-scan attempt limit too broadly. Limit now scoped to the two real purity
stages, not bounded fixture tests. v2 again579/0 then failed on scratch-file
cleanup; isolated unchanged filesystem harness38/0. Keep combined failure and
isolated pass separate. Remaining safety children running since02:31:31Z in
WindowsPS5.1 PID26124, exec83416; buffered remaining-safety-stages.log. Do not
launch duplicate scans; inspect/wait that owned session and record its outcome.
Full original source gate is still FAIL, not waived by reader-only evidence.

Owner requested publication of unchanged .4 before extended guarded checks;
publication is DONE. Continue the authorized extended window without rebuilding
or relabeling frozen69a48a4. No main merge/permanent install. Preserve all guards.
[Manual alpha notes](docs/CONTROLLER-ALPHA-0.5.4-CONTROLLER.4-NOTES.md).

Local .4 source adds semantic authoring/navigation/search, the real native card,
shared editor/run handoffs and controller workspace text lifetime/material repair.
Foundation e4dcbfc, card117b7f1, integration baf221b; frozen product69a48a4.
Frozen focused **64/0**, 7.3 s, deterministic local packaging PASS. See
[exact .4 artifact receipt](docs/CONTROLLER-UI-CANDIDATE-0.5.4-CONTROLLER.4.md) and
[capabilities and exact evidence limits](docs/CONTROLLER-AUTHORING-CAPABILITIES.md).
F01-F10 remain NOT_RUN. Do not call this completion: no new native pixels,
physical controller authoring/card input, or effects/resources demonstrated.
Latest owner clue: spellbook-origin rendering worse than main-menu origin.
Owner confirmed DATA free; read-only Steam, lease/transaction/review and
Automation-pair prerequisites PASS at intake. Extended checks NOW authorized;
no native launch yet. [Preflight evidence](docs/CONTROLLER-NATIVE-PREFLIGHT-2026-10-17.md).
New screenshot not present at inspection. Guarded controller-specific no-cast observer/launcher
route remains unfinished. Do not run an old mouse scenario as a substitute.
No desktop/game/install/Mods/settings/profile/save mutations occurred. Prepare
controller-specific guarded route. Alpha4 publication/associated branch push was
separately authorized and completed; no new artifact/merge authorization inferred.

## Historical Alpha 3 / earlier access handoffs (superseded scope)

> **Current controller ACCESS repair (owner correction):**
> **ACTIVE SCOPE REPLACEMENT:** [full controller mission](planning/KBP-FULL-CONTROLLER-MISSION.md).
> Readable workspace, real native menu card and controller-only authoring/search
> are ALL required. Earlier mixed-input/deferred-card banners are history, not
> current acceptance. Preserve Alpha 3's Escape/scroll fixes. New work local only;
> no publication/push/merge/install selected. Owner says spellbook entry rendered
> worse than campaign-menu entry: compare hosts/materials/initialization, not saves.
> Owner-requested [Alpha 3](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.4-controller.3)
> published 2026-10-18T00:39:48Z as non-Latest prerelease; exact product/tag,
> download-verified hashes and pending native checks are in the .3 receipt.
> PR #10 remains draft. Stable/main/old alphas/installation/saves unchanged.
> Frozen .3 product 7a7faec9a0824ff192271d12d1a9cde4199452cf, focused 62/0
> and deterministic packaging PASS; [receipt](docs/CONTROLLER-UI-CANDIDATE-0.5.4-CONTROLLER.3.md).
> Native gates unchanged. Reuse private/worktrees/KBP-CONTROLLER-ALPHA03 bytes.
> Owner's new review requests two fixes, draft PR update and a new alpha.
> .3 implements pre-request owned TMP cancellation tracking and overlapping
> viewport result scrolling; focused development 38/0. Native authoring/layout
> remain NOT_RUN; do not restore old broad campaigns or change other mods.
> The .2 identities/publication boundaries below are historical, not new authority.
> The owner's subsequent request to open a PR authorizes guarded publication of
> this feature branch and updating existing draft PR #10, not a duplicate PR,
> release, merge or installation. Guarded-push WhatIf check: 6/0. The earlier
> no-push statements below describe the completed local-only checkpoint.
> Frozen local 0.5.4-controller.2 product 32f6385206694b7cdb102d1b938f7b2b1acb2d1b,
> 60/0 focused tests, deterministic two-build package PASS; exact ZIP/DLL/MVID
> and native gaps: [candidate receipt](docs/CONTROLLER-UI-CANDIDATE-0.5.4-CONTROLLER.2.md).
> Clean detached product worktree: private/worktrees/KBP-CONTROLLER-ACCESS02-VERIFIED.
> Parent checkout is documentation/review; do not rebuild its later HEAD over
> the frozen package. PR #10 was remote 2a04579 at the pre-push audit, open/draft;
> now selected for this owner's authorized feature-branch update.
>
> [mission](planning/KBP-CONTROLLER-ACCESS-REPAIR-MISSION.md) supersedes the old
> run-only gate. Real Configure -> existing editor -> same-session return is
> mandatory, mouse/keyboard authoring allowed. Source now includes that route,
> final-after-Awake TMP geometry, unpaged basic status, scrollable results and a
> context-scoped console-book editor entry. Focused 42/0 at the integration
> checkpoint; native rendering/input/authoring NOT_RUN. DATA read-only intake
> at 2026-10-17T23:50Z found foreign Kingmaker PID 22596 and the KMG lease active;
> neither touched. The installed lab KBP is 0.1.1.0, SHA-256 78407dd4c7240ce25c7654715854e83061da744a18d8eb4a2198fdffec5e3080,
> MVID c0cc9cbc-8075-4d98-a32e-29972aafa7fa, NOT the owner's alpha.
> Keep old artifacts and working KMG/CotW fixes. Native card follows verified
> authoring workflow; full controller graph editing deferred. No push, release,
> merge or install selected. Native tests were not run: foreign ownership and
> the guarded controller observer gap remain. All three owner photos inspected;
> they corroborate the old clipping/entry state, not a new-candidate PASS.
>
> **Historical October 17 owner alpha:** [Controller Alpha 1](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.4-controller.1)
> is published as a non-Latest prerelease of the unchanged 9aec44e candidate.
> The owner explicitly requested artifact-only publication without new tests or
> rebuilds for testing on another PC. [Release receipt](docs/evidence/controller-alpha-0.5.4-controller.1.md).
> Await owner entry/exit-first feedback. PR #10 remains draft; stable 0.5.3/main,
> DATA installation and all saves are untouched. Do not resume a broad campaign.
>
> **Previous October 17 controller execution-first mission:** follow
> [revision 2](planning/KBP-CONTROLLER-UI-MISSION.md) and the
> [implementation](docs/CONTROLLER-UI-IMPLEMENTATION.md) /
> [acceptance](docs/CONTROLLER-UI-ACCEPTANCE.md) records. Continue
> `codex/kingmaker-buff-planner-controller-ui` from preserved base
> `2194d0f0f597ebe8b8423f91f8e1fb6b1f65fdb7`. Source execution is implemented;
> native/physical qualification is pending. Full controller editor is DEFERRED,
> not an execution acceptance gate. Do not resume older runtime/push/publication
> campaigns below. The owner's subsequent October 17 request authorizes pushing
> this feature branch and opening a draft PR. The subsequent review requested
> the three corrections and main integration; default-branch merge, release and
> permanent installation remain unauthorized. Review fixes are recorded in the
> [correction receipt](docs/CONTROLLER-UI-REVIEW-FIXES.md). The integrated local
> candidate is 0.5.4-controller.1, not a relabeling of the preserved source milestone
> `afa108f1fd2cc76f49f466c62bd1572f31663d15`; frozen product
> `d04c1a781770194966fa55df15000d627e14662d`, 0.5.2-controller.1, focused
> 38/0 and two-build deterministic package PASS. Exact artifacts/hashes/MVID:
> [candidate receipt](docs/CONTROLLER-UI-CANDIDATE-0.5.2-CONTROLLER.1.md).
> Current integrated product: `9aec44e374b26b3561fa4402912c77a23a4e08ac`,
> [0.5.4-controller.1 receipt](docs/CONTROLLER-UI-CANDIDATE-0.5.4-CONTROLLER.1.md),
> focused 45/0 and deterministic package PASS. Three P2 fixes and main integration
> are complete; the older artifact above is historical. Next is the narrow
> [entry/exit protocol](docs/CONTROLLER-ENTRY-EXIT-PLAN.md), positive/negative
> guards, then an exclusive separately budgeted entry-only session. No runnable
> controller scenario exists yet. Do not deploy an old mouse scenario or rebuild
> later documentation HEAD over either frozen artifact.
>
> **Current October 14 stable release:** the owner accepted the
> human-feedback candidate for full ordinary testing, selected minimal release
> checks, and authorized default-branch integration and a full release. Stable
> `v0.5.2` is public, non-prerelease and GitHub Latest at release commit
> `a94e0e586685f8577d32357f289090bc28f1148b`. ZIP SHA-256
> `84dd4b6cfabb84664029d53af0be0740b982dda6f1d800a230ac81d185290d70`;
> DLL `8c6f6a4198de826a886111f64d33b17683173e8bb979164bf63be12d21f19e82`;
> MVID `4a2c100a-2b5d-400e-8251-3560f7a3e616`. Deterministic release builds and
> independent download/package verification pass. The broad source-only/purity/
> game campaign was deliberately not rerun; the retained rc5 pre-launch display
> failure and runtime limits remain. Exact record:
> [0.5.2 release receipt](docs/evidence/release-0.5.2-receipt.md).
>
> **Current October 14 human-feedback resume:** exact frozen rc5 product
> `ec3d5f865707e540b00fccabd3f1a98f04aa3095`, version 0.5.1-rc5; ZIP
> `63c12d519dd49d514f6ddd31ea0514b9a8af5644680aa1dde82ddc77e63bfb03`;
> DLL `8bbf4f1a515cdbd186f8cdd515250fd12c864d2f643a38945f84d786b61e9f92`;
> MVID `95f36b3c-7e36-4005-9880-898841e1fd72`. Planning-sequence and explicit
> 1920x1200 corrections pass focused checks. Selected operation
> `kbphf-rc5-20261014-01` stopped, without retry or game launch, when DATA's
> display changed from preflight 1920x1200 to 1920x1080 during the first purity
> inventory. No transaction, RunId path, allowance or save change exists.
> Preserve that FAIL. Resume only after the owner restores and can hold physical
> 1920x1200, then explicitly selects one fresh ~7 h 30 m batch from a clean
> runtime checkout at the path-bound product/harness commit `ec3d5f8`, as
> described in the
> [rc5 receipt](docs/HUMAN-FEEDBACK-REFINEMENT-0.5.1-RC5.md).
>
> **Current October 14 human-feedback resume:** branch
> `codex/kingmaker-buff-planner-human-feedback`; exact frozen successor product
> `19e2e777261ff7887f0b8f62c8adcc75161220c5`; version 0.5.1-rc4; ZIP
> `1a38d33439392a9c1da5202c1537357213177432e8bca174b550c88ab0c68036`;
> DLL `093b08fc1f50ba334a05e75dd24eb785808885c5826d1ee6fffa63149591a287`;
> MVID `be795e3c-7c39-41a7-98e2-fdc5c177c326`. Preserve the rc3 FAIL and its
> clean Mods/save restoration. rc4 is **NOT_RUN**. Resume only with the fresh
> rc4 two-purity/four-game batch, first guarding an actual transactional
> 1920x1200 owner mode; exact scope and cost are in the
> [rc4 receipt](docs/HUMAN-FEEDBACK-REFINEMENT-0.5.1-RC4.md).
>
> **Current October 14 human-feedback resume:** branch
> `codex/kingmaker-buff-planner-human-feedback`; packaged product
> `debd40674ed2a6ed9f6e3423671f6ec2180bead0`; version 0.5.1-rc3; ZIP
> `0d30c7fa430e618be1b7d871c68bdc894b79206018771b17b90ca9b8d3037e62`;
> DLL `6875da1340cae51b6fb12b5c2bccbc94f102a1ab096251ec35346fcedeac7f7a`;
> MVID `407d6a8b-139f-4800-8ee8-b259578ccdfa`. Bounded source, focused checks,
> harness correction and local package are complete; candidate is **NOT_RUN**.
> Preserve rc1/rc2. Resume only with the single fully specified 7 h 30 m live
> batch in the [rc3 receipt](docs/HUMAN-FEEDBACK-REFINEMENT-0.5.1-RC3.md).
>
> **Current October 13 human-feedback correction resume:** branch
> `codex/kingmaker-buff-planner-human-feedback`; exact packaged product
> `e06d803400893f4cd68d0066ae2421d0085db243`; version 0.5.1-rc2; ZIP
> `6e6a5822c771514981b4bd788398864c56f98df7fabf96e4479ad84cccbcaa27`;
> DLL `695f65d02e8253da81a96238312e32d64489793788ad35dbb5e0c41417354548`;
> MVID `bfd140ba-1d58-40fb-a71c-4255371b42f2`. Four source-review findings,
> focused checks and local packaging are complete; candidate is **NOT_RUN**.
> Preserve rc1 and do not install, merge, release or relabel older evidence.
> Remaining live scope and its exact selected-purity parameters/cost are in the
> [rc2 correction receipt](docs/HUMAN-FEEDBACK-REFINEMENT-0.5.1-RC2.md).
>
> **Current October 13 human-feedback resume:** branch
> `codex/kingmaker-buff-planner-human-feedback`; exact packaged product
> `a4c4342e1296966cbae1d64eeb53b47f23265627`; version 0.5.1-rc1; ZIP
> `2e4e1a9fe3fd56eaf11eac39f09132be18c3358ebc9aee45f3b6fad8e8d2ffbc`;
> DLL `bdc1aa3c512ba884a6c8b4476f4c92ea9362dec794b7fb1addf95f07288af10e`;
> MVID `e21f2f39-0305-4bb3-aa02-067f4ac78252`. Source/focused/package work is
> complete and **NOT_RUN**. Do not install, relabel 0.5.0 evidence or launch an
> unbudgeted campaign. Resume with the single frozen-product purity plus owner
> 1920x1200/compact 1280x720 batch in the
> [human-feedback receipt](docs/HUMAN-FEEDBACK-REFINEMENT-0.5.1-RC1.md).
>
> **Current October 13 resume:** exact product `dfbaa5f`, internal version
> 0.5.0, is published as artifact-only human-review prerelease `v0.5.0-rc.1`.
> The annotated tag targets the product commit; later reader/evidence records do
> not relabel its DLL or ZIP. Main and the native UI feature branch include the
> conservative schema-1 reader correction. Receipt remains PARTIAL (7 PASS,
> 4 PARTIAL) pending the three owner observations in the
> [final evidence record](docs/NATIVE-UI-FINISH-0.5.0-FINAL-EVIDENCE.md).
> Do not restart the interrupted stable publisher or a broad campaign. Stable
> publication, Latest status, permanent installation, and human acceptance are
> not claimed.
>
> **Active October 12 completion:** product `b6016c710c24a6118d4e92eb391bdb0b1c036488`,
> version 0.5.0, has a validated local ZIP but is **NOT_RUN** in Kingmaker.
> The earlier 624b5e9 two-size native pictures remain attributed to those bytes;
> its physical Spellbook boot request FAILED and was fully restored after a
> verified graceful game exit. The exact new-source eleven-action full-history
> purity is supervised, PID 26120, since 2026-10-12T10:54:47Z, one
> attempt, 120-second lease wait, all five original targets. Preserve its watched
> paths and launch no conflicting runtime route. Acceptance stays **PARTIAL**.
> Continue the authorized observations, local delivery, and guarded push after
> the actual prerequisite result. The 24-hour total forecast is a plan, not a
> stop timer. [Exact protocol repair and current evidence](docs/NATIVE-UI-FINISH-0.5.0-PROTOCOL-REPAIR-CHECKPOINT.md).

> Current continuous mission: clean product `624b5e929eb24a8d31ef9fe50cb4520d6fa2445d`,
> version 0.5.0 in owned NUICZ; validated installable local archive **NOT_RUN**.
> Selected-source pictures now focus the enhanced casting and its hover; the group
> recipe already selected its group source. Explicit copy/source/focused-sidebar
> metadata permits strict visible-row checks while retaining complete mixed-recipe
> identities. No additional cast or manufactured availability is introduced.
> Focused C#10/0, selected-context reader32/0, finite reader14/0 and eight parsers passed.
> The owned prior 96b2f4b scan remains PID25996/session79092; preserve its watched
> paths through safe natural completion and retain its original-input numeric result.
> Current 624b5e9 prerequisite and game routes are NOT_RUN; do not reuse the older scan.
> Eleven-action full-target prerequisite, native1080/720, physical and read-only
> Advanced selections remain selected; the later six-action prerequisite uses fresh
> mechanical zero-cost/Classic/finite allowances and includes genuine Advanced720 content.
> Every Advanced save stays unchanged; NativeUI/Advanced remains refused.
> Revised **twenty-hour** forecast is 2026-10-12T14:28:29Z, allowing the required
> changed-byte prerequisite and safe completion of the old one. It is not a stop timer.
> Continue actual observations, pictures, exact restoration, local delivery and
> guarded push. Acceptance PARTIAL. Earlier banners retain chronology and original inputs.

> Current continuous mission: clean product `96b2f4b6396d3c9a961cc2dc6bd4b6a8327d9f18`,
> version 0.5.0 in owned `KingmakerBuffPlanner-NUICY`; validated local ZIP **NOT_RUN**.
> Reused bounded observers now hold actual selected group/enhancement plan pictures
> and observe genuine spent finite prepared resources after an approved qualification.
> They restore temporary plan edits through Undo and submit no additional casts.
> Physical search and deliberately paused Planner-to-Spellbook B dispatch remain selected.
> Old 3b5a2d7 prerequisite completed **PASS, exit 0, 6534.7207909 seconds** with
> all five inventory targets, six WhatIf calls, Attempts 1 and lease wait 120.
> It establishes no purity or game evidence for these changed compiled bytes.
> Current eleven-action selection/physical prerequisite and sixteen actual routes
> are selected, followed by a five-action casting prerequisite with fresh mechanical
> allowances: zero-cost instant, one-plan animated Classic, existing finite-direct-mixed
> instant, physical authoring and blocker navigation. Every Advanced save, including
> both fixture copies, must remain unchanged; unsupported NativeUI/Advanced remains refused.
> New candidate runtime **NOT_RUN**, acceptance **PARTIAL** until actual runs and PNG review.
> Continue the authorized longer milestone through restoration, pictures, local delivery
> and guarded push. Eighteen-hour planning estimate remains a forecast, not a deadline.
> Earlier banners below retain chronology and their original source attribution.

> Current continuous milestone: clean product `bad10d978b8e3c79bead55bb4e8969e6126769c2`,
> version 0.5.0 in owned NUICW; local package validated, **NOT_RUN**.
> Physical search uses real OS typing. Spellbook schema 2 now requires an
> actual B press from the deliberately paused open Planner, native window
> shown/planner dismissed/lease released, physical Escape and original pause
> restoration. All ten spellbook PNGs hold state through the shared ten-second
> capture boundary. Historical schema 1 retains its original interpretation.
> Focused C# 13/0, spellbook reader 50/0, search reader 14/0, direct 24/0
> and real -File 8/0 passed, exit 0 in 10.3245286 seconds. Clean package
> built once in 4.5511808 seconds. 95cb40b is preserved validated NOT_RUN.
> The unchanged 3b5a2d7 six-action purity is still owned by PID 26140.
> Its final result retains those inputs; preserve all watched runtime paths.
> New bad10d9 eight-action purity and actual native 1080/720, physical and
> read-only Advanced observations remain selected, followed by fresh exact
> casting allowances and their four-action prerequisite. No Advanced save
> mutation or unsupported NativeUI fixture is selected. Continue through
> actual pictures, restoration, local delivery and guarded push. Receipt PARTIAL.
> Eighteen-hour planning estimate remains a forecast, not a stop deadline.
> Earlier banners below are chronological evidence for their named inputs.

> Current continuous milestone: clean product source `95cb40b229a2aee954cfcecf8de2bb9c16ab5d2c`,
> version 0.5.0, owned NUICQ. Local ZIP/DLL are validated and **NOT_RUN**.
> Physical select schema 2 adds actual guarded search typing, B containment,
> active-row membership and two held captures; callback cleanup remains separate.
> Focused C# 11/0, raw-state reader 14/0, launcher direct 24/0 and actual -File
> 8/0 passed, exit 0 in 7.2342248 seconds. Clean local build completed once
> in 4.1396548 seconds after a preserved pre-compilation local-properties refusal.
> The unchanged 3b5a2d7 six-action purity remains owned by PID 26140; preserve
> its freeze and watched runtime paths. Its result will retain its original inputs.
> The new candidate's eight-action purity is NOT_RUN: native two sizes, both
> selections, physical spellbook/search, and existing approved read-only Advanced
> inspect/workspace. Advanced routes compare every save unchanged; NativeUI still
> refuses Advanced. No new casting allowance or save-mutation authority is inferred.
> Continue through actual new-byte native/input/casting pictures and local delivery.
> Eighteen-hour planning estimate is not a stopping deadline. Receipt PARTIAL.
> Earlier banners below are chronological evidence for their named inputs.

> Current continuous milestone: clean product source
> `3b5a2d747da8cd69de66d7459218af0fac99e915`, version 0.5.0, NUICV.
> The new-byte six-action purity is supervised (PID 26140), Attempts 1 /
> LeaseWaitSeconds 120, all five targets; both native resolutions stay inside
> this same required window. Preserve watched runtime directories and freeze.
> New local package is validated, NOT_RUN. No earlier PASS is relabeled.
> Physical input now works through the guard: earlier 2c40f06 selection passed
> 83/0 in 276.4977708 seconds, exit 0, with full protected-save restoration.
> Spellbook delivered all twelve real actions but retained FAIL 82/1 because
> both readers required retired placement prose; source 3b5a2d7 validates the
> actual visible footer geometry. Focused C# 6/0 and consumer 34/0 passed.
> Continue the authorized final native/physical/casting observations and actual
> pictures. Sixteen-hour planning estimate is not a stop deadline. Receipt PARTIAL.
> Earlier banners below are chronological evidence for their named inputs.

> Current continuous milestone (October 12): clean product source
> `10557953311bef81d5d85c693500e1c459b97981` preserves the 720p footer repair,
> the held native success-log picture and explicit casting/HUD JSON state.
> Its NUICS 0.5.0 local package is validated, NOT_RUN. Prior archives remain intact.
> The 2c40f06 casting purity completed PASS, exit 0, 6711.8549948 seconds;
> its instant game qualification passed 85/0, exit 0, 293.8677189 seconds,
> with installation/protected-save restoration verified. Three actual active
> screenshots were reviewed; its omitted casting JSON fields are not evidence.
> Focused snapshot correction: C# 10/0, source 43/0, build 1/0; strict raw-state
> reader 11/0 and helper parsers 4/0. Existing old Classic grant remains unused;
> final genuine availability will be observed on repaired bytes, without extra casts.
> Actual foreground access was observed during the cast; fresh guarded physical
> spellbook retry `kbpnui-complete-spellbook-02` is supervised, with no guard bypass.
> The revised sixteen-hour estimate is approximately 2026-10-12T10:28:29Z:
> two measured ~112-minute final-input prerequisites plus feasible observations
> and visual review. It is not a cancellation deadline. Receipt remains PARTIAL.
> Historical banners below describe their original inputs and times.

> October 12 current candidate: source 354aed5 includes the footer repair and
> one bounded native success-log picture after releasing the planner; clean
> NUICG local ZIP validated, NOT_RUN. See [continuous milestone](docs/NATIVE-UI-FINISH-0.5.0-COMPLETION.md).
> The owned 2c40f06 casting purity still runs with Attempts 1 / lease wait 120;
> preserve its five watched targets and complete its cleanup before game writes.
> Physical input remains limited by actual foreground refusal; original
> restoration/protected saves are verified. Fourteen-hour estimate is not a
> cancellation deadline. Keep current-policy focused corrections and continue.

> October 12 continuous work: see [current completion record](docs/NATIVE-UI-FINISH-0.5.0-COMPLETION.md).
> Source eb36973 repairs an actual 720p save-message defect, has a validated
> local ZIP, and remains NOT_RUN. The earlier 2c40f06 native runs passed 85/0
> at both sizes; all 74 original pictures were reviewed. Its owned casting
> purity remains supervised (PID 8964; Attempts 1, LeaseWaitSeconds 120).
> Physical spellbook failed before key delivery (82/1, exit 1), with original
> installation/protected-save restoration verified. Foreground access is an
> observed environment limitation; do not assume a historical console cause.
> The revised fourteen-hour estimate is not a cancellation deadline. Continue
> useful work and current-policy focused corrections; do not resume old gates.

> Current milestone state: clean source `2c40f0633079248072c2f23a754905d665ef41d6`
> and its unchanged 0.5.0 package passed native UI qualify at 1920x1080
> and 1280x720: 85/0 assertions, 37 completed captures and nine ordinary/paused
> switches in each run; original pause state, game exit, registry/mod restoration
> and protected-save checks are verified. The full six-action purity window
> passed with exit 0 in 5902.4443505 seconds (one attempt, lease wait 120).
> Visual review has begun on original PNGs. Casting selection, genuine active
> and disabled availability observations, physical routes and their remaining
> pictures are still in progress; acceptance remains PARTIAL.
> Revised eleven-hour planning estimate remains approximately 2026-10-12T05:28:29Z;
> this estimate is not a cancellation deadline. Earlier candidates and failures
> below retain their original inputs and chronology.

> Follow the [current milestone](docs/NATIVE-UI-FINISH-0.5.0-COMPLETION.md).

# Current native UI capture checkpoint - October 11, 2026

Read [validation policy](docs/VALIDATION-POLICY.md),
[workflow reset](docs/WORKFLOW-RESET-HANDOFF.md) and the
[current capture checkpoint](docs/NATIVE-UI-FINISH-0.5.0-CAPTURE-CHECKPOINT.md).
The owner policy supersedes automatic broad/six-hour sequences in older records:
ordinary explicit test prefixes, 15-minute planning target / 30-minute ceiling.
Live launcher/source/WhatIf, ownership and restoration safeguards still apply.

DATA development worktree: `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-NUI`;
branch `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10`. Starting reviewed
HEAD 9dd89974ddec4eea6ab8d19e05665a8486461ef5. Clean product/observer source and
package commit `7410f92604621aabfba97e26cb1c9f6f17044b63`, version 0.5.0.
Workflow merge 0463f55 includes origin/main 358418f6 and repair f658726.

Capture requests now hold the displayed state until a later frame and complete,
readable fresh PNG, then Return/Escape/next-mode changes run in a later Update.
Ten-second timeout records failure and uses existing cleanup. Related sequences
are corrected together. Focused development: source 43/0, builds PASS, C# 9/0
(5.108625 s); local builder 3.361913 s, package validation 4/0. Guarded push tests
6/0. Previous launcher 24/0 direct and 8/0 shared child checks are unchanged.

Local package ZIP SHA-256
`1d75a0038d2f99eaa55ebc9b774a2c9e72211e8af0991258fd24fd1728299088`, DLL
`568dec0091a13f65cecfeb4a262c3f762cbae75b04dbfe996f437f97df65d1c6`,
MVID `95959d77-29f9-488a-87cd-a55fb5ca5eb4`. Compiled identity verified.
Preserved under `artifacts/native-ui-capture-2026-10-11/`; packaged, NOT_RUN,
never staged. Later documentation/publication HEAD does not relabel these bytes.

One-time 17:56Z boundary audit: old frozen 4dbfa33 gate has exited and its log
ends with the old decision-refusal assertion at FileWhatIf:104 (17:08:42Z).
Status FAILED; numeric exit unrecorded. PID 16712/owned children absent, no
runtime/fixture/foreign locks found, 495 transaction records Restored. No
cancellation or rerun; NUIR and watched paths preserved. The 16:08Z ACTIVE audit
and prior unobserved-completion checkpoint remain dated historical results.

Receipt stays PARTIAL. Fallback remains source 4dbfa33/version 0.5.0, ZIP
`cf658914130a010d7525ee47bc41a1f5c05f9bcde929852a1e31430ab3be8fcb`, DLL
`b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69`.
Its four original in-game PASS/restoration receipts keep their attribution;
the old aggregate gate is FAILED. Caption fix 57f136a and newer observations
require game evidence on the new bytes. No criterion is promoted by offline tests.

Next: separately budget the unchanged current whole-history WhatIf purity route
and the selected 1920x1080/1280x720 native qualify batch, on clean source 7410f92;
verify restoration and inspect actual captures against R01-R15. A read-only
1,038-file manifest sample took 8.41 s; 374,395 current files before/after imply
about 101 minutes for required inventories. This exceeds the ordinary ceiling;
no live-purity window, gate, game or publisher was launched. No receipt reuse or
pruning is authorized by this checkpoint. Physical/remapped input, paused
switching, real availability and fixture/manual limits remain in the current
checkpoint. The package is ready for that explicit milestone without restamping.

Previous source 673d1e3 and launcher repair 3af7171, exact earlier commands and
identities are in the [previous review checkpoint](docs/NATIVE-UI-FINISH-0.5.0-REVIEW-CHECKPOINT.md).
The original resume chronology is retained in
[Claude handoff history](docs/history/NATIVE-UI-FINISH-RESUME-at-Claude-handoff-2026-10-11.md)
and [pre-reset history](docs/history/AUTONOMOUS-RESUME-before-workflow-reset-2026-10-11.md).
