> Historical checkpoint before the screenshot sequencing repair. The
> [current capture checkpoint](NATIVE-UI-FINISH-0.5.0-CAPTURE-CHECKPOINT.md)
> records source 7410f92, its packaged NOT_RUN identity, the old gate's actual
> FAILED outcome and the budget-limited live prerequisite. Results below keep
> their original sources and dates.

# Native UI review checkpoint - October 11, 2026

Development checkpoint on DATA, after the review of `0844421b222824acbc649773db51565dbc547431`.
The [current validation policy](VALIDATION-POLICY.md) is effective on the feature
branch. Receipt stays PARTIAL: UI-07, UI-08 and UI-11 retain their historical PASS;
UI-01 through UI-06, UI-09 and UI-10 remain PARTIAL. No new game observation ran.

## Source and review identities

- Worktree: `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-NUI`.
- Branch: `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10`; version 0.5.0.
- Reviewed starting HEAD and remote: `0844421b222824acbc649773db51565dbc547431`.
- Workflow merge remains `0463f553a859ecc195f862fba9632ee91586e08c`, incorporating
  origin/main `358418f6f86ef0c34687a2f4b96374042c7393d2` and repair `f658726d215d59d41d3a950a90be953653e5d96f`.
- Launcher consumer repair: `3af7171a70f95ef13da3359245d00b030bc62843`.
- Clean product/observer source HEAD built and tested:
  `673d1e3a62356af3d1b4d5dcbd19f233fd521734`.
- Later documentation commits record these inputs and do not relabel the DLL.
  The guarded push's exact final local/remote HEAD and duration are retained in
  `artifacts/native-ui-review-2026-10-11/publication-receipt.json`.

## Launcher consumer repair

The owner's P2 finding was correct: the Advanced native UI guard emits stable ID
`KBP_NATIVE_UI_FIXTURE`, while the broad -File test still expected the other
family scenarios' prose. The producer and the original 24 direct argument cases
are unchanged. Restoring the old producer wording was rejected.

The seven family cases and their actual `powershell.exe -File` invocation/assertion
now live in `scripts/RuntimeLauncherRefusalTest.Common.ps1`, shared by the broad
consumer and `scripts/Test-RuntimeLauncherFileArguments.ps1`. Native UI expects
`KBP_NATIVE_UI_FIXTURE`; the other six retain their existing non-casting refusal
matcher. All require a nonzero child exit. The broad gate also runs the focused
checks before its expensive windows.

The focused check ran the broad consumer's exact native facts case against the
real launcher. Captured child output includes `FullyQualifiedErrorId :
KBP_NATIVE_UI_FIXTURE,Invoke-KingmakerRuntimeTest.ps1`; exit was 1. Real qualify
with uppercase arguments and the incompatible Advanced profile also exit 1.
A different real refusal (missing stage) fails the fixture assertion. Real child
consumer sentinels prove the retained legacy matcher, reject the wrong marker
in either direction, and reject a success exit even with the right marker.
Sentinels are consumer tests, not runtime guards or live-purity evidence.
No broad preceding work, package lookup, staging or game process was invoked.

## Native action observations prepared

UI-02: read the installed `KeyboardAccess.Tick` and `OnCallbackByBinding` IL.
Tick first checks the selected input field, keyboard Disabled guard and binding
selection. The callback then checks mode, pause support, gamepad and registered
callbacks; `force=true` bypasses `InputMatched`. This confirms why the existing
observer proves downstream callback behavior only. New records read the actual
binding name/key/mode, keyboard Disabled flag, input-field gate, pause support
and gamepad state immediately before the same callback. Their dispatch kind is
explicitly `binding-callback-forced`. No OS key or remapping was exercised, and
binding-selection activity is not observed by these new fields.

UI-10: the original records show active/interactable Metamagic for Linzi and
Tartuccio, and an inactive Metamagic button for Hedwirg. The observer now invokes
only an already available `Container_Book/MetamagicButtonPlace/Apply`, reads the
native mode after settling, captures it, invokes the same native return control,
then checks mode restoration and the offered planner entry. The existing later
planner handoff remains. It never writes a spell or enables an unavailable
button. The verified 2.1.7b mode getter is private, so its exact read-only getter
uses bounded reflection; `SwitchToMetamagicMode` is public. Completion and host
shutdown restore the mode owned by the probe and release its plan-file lock.
A failed observed return stays a violation even if cleanup restores the mode.

New records emit schema 3. Schema 1 preserves its original judgement; schema 2
still requires the caption, popup and filter observations introduced in a652441.
Schema 3 additionally requires the native action and key-route measurements.
Sixteen new negative mutations, missing-observation checks and schema-2 historical
acceptance extend the existing record behavior test. Structural success remains
separate from visual acceptance, physical input, paused switching and real
availability conditions. No UI criterion is promoted to PASS by observer code.

## Focused commands and costs

Supervised Windows PowerShell 5.1 from NUI, `-NoProfile -NonInteractive
-ExecutionPolicy Bypass -Command`. Logs and input hashes are under
`artifacts/native-ui-review-2026-10-11/`, outside the watched runtime paths.

| Actual check | Result | Elapsed |
| --- | --- | --- |
| `scripts/Test-RuntimeLauncherFileArguments.ps1` | 8 PASS, 0 FAIL, shared real child-process assertion | 1.541760 s |
| `scripts/Test-RuntimeLauncherArguments.ps1` | Original 24 PASS, 0 FAIL | 0.263195 s |
| PowerShell parser: common refusal helper, focused -File check, broad FileWhatIf | 3 PASS, 0 FAIL | Same launcher supervision (2.236 s total) |
| `scripts/Test-Development.ps1 -Only 'native-ui-record,native-window,spellbook-handoff,native-source-tabs'` | Source 43/0, production/test builds PASS, selected C# 6 PASS, 0 FAIL | Wrapper 7.6 s; supervision 7.712219 s |
| `scripts/Test-GuardedPush.ps1` | 6 PASS, 0 FAIL; no repository writes | 2.041977 s |
| Changed/new Markdown links and current policy/provenance text | 44/0 links; 3/0 policy/provenance | 0.119 s |
| Compiled `BuildInfo.Commit` / `Version` against preserved DLL | Exact clean source and 0.5.0 verified | Read-only metadata check |
| Inspected `tools/inspect.ps1` native contract member read | Read-only contract evidence | 1.451925 s |
| Inspected `tools/il.ps1` dispatch/Metamagic implementation read | Read-only contract evidence | 0.636261 s |

The selection matched six real test names. Selector implementation is unchanged;
its earlier selector regression pass was not repeated.
Validation stayed inside the ordinary 15-minute planning target/30-minute ceiling.
No source-only aggregate, live-purity window, restore-install, publisher,
reproducibility, full layout matrix or frozen-candidate campaign was selected.

## Artifact, observer and environment provenance

Production was built once, after committing the reviewed source, from clean
`673d1e3a62356af3d1b4d5dcbd19f233fd521734`. Development DLL:

- Version 0.5.0; SHA-256 `ca3d6bc5a4a674397e1baab60601c972dc008086d93c247c7451ca97fb8a7250`.
- MVID `a956e985-a762-4994-84e6-07f586a947ec`.
- Preserved copy: `artifacts/native-ui-review-2026-10-11/development-673d1e3.dll`.
- Not packaged, staged, installed or qualified in Kingmaker; observations NOT_RUN.
- C# reader/observer comes from that same source commit. Test runner SHA-256
  `8b4bf6ba58ed4dda91d0baaa74c11b22e389649963dbfa0dece387b0314f66fd`.
- Offline inspected game assembly:
  `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed\Assembly-CSharp.dll`,
  SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`.
  Exact supplementary helper and guarded-push hashes: `contract-inputs.json`.
  Reading that assembly is not a new fixture, environment or runtime receipt.

The fallback stays at frozen NUIR `4dbfa33e1b36bf59bc9bb1a4ac3ebde860c52302`,
version 0.5.0, ZIP `cf658914130a010d7525ee47bc41a1f5c05f9bcde929852a1e31430ab3be8fcb`,
DLL `b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69`.
Its four original final game/restoration receipts retain their original
attribution. 3757758 was records only; 57f136a and both Codex observer changes
are absent from that package. Earlier uncommitted development build provenance
is retained in the [previous checkpoint](NATIVE-UI-FINISH-0.5.0-DEVELOPMENT-CHECKPOINT.md).

The old 4dbfa33 gate was ACTIVE at the prior one-time 16:08Z audit, with no
GATE-EXIT then. This turn did not poll it, cancel it, alter its frozen source or
watched directories, or infer completion. Current completion/exit status is
unobserved. The new consumer repair affects only the development branch.

## Remaining acceptance and exact next action

- Execute the new caption/popup/filter/key-route/Metamagic observations on an
  exact clean package, then inspect the actual screenshots at both comparison sizes.
- Physically dispatch I/C/B/journal and remapped keys, input-field suppression,
  right-click and hover; callback invocation cannot close those gaps.
- Deliberately observe paused window switching. Existing seven switch records
  were all unpaused; read-only pause-support fields cannot prove a paused switch.
- Inspect real execution-disabled HUD controls and independent native controls.
  Forced appearance does not establish `_session.IsExecuting` availability.
- Unavailable-Metamagic planner action, active skip/recast, groups, enhancement
  scope, other providers, genuine long content/scrolling and larger UI scale/
  1920x1200 still need applicable guarded fixtures or manual observations.

Next: review source 673d1e3 and the launcher repair, inspect the live prerequisite
call graph, and select the smallest guarded native UI qualify batch at 1920x1080
and 1280x720 once the old gate is verified exited and ownership/restoration is
clear at that runtime boundary. Declare its prerequisite cost before launch;
retain all current source/WhatIf, package, fixture, lock, save and restoration
requirements. If the needed route exceeds the ordinary budget, report that
specific remaining route instead of starting an automatic broad campaign.
No merge into main, tag, release or permanent installation is selected.
