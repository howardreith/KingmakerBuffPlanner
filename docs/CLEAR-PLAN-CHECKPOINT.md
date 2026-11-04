> **Input review correction, 2026-11-04:** the reviewed 502df08 input paths had two defects. Corrected product 8de9e8e passes 39 focused tests, including six compiled-view/lifecycle regressions. Native acceptance remains NOT RUN. [Current correction and evidence](CLEAR-PLAN-REVIEW-CHECKPOINT.md). Historical artifact identities below are unchanged.

# Clear plan development checkpoint — 2026-11-04

Implemented on `codex/kingmaker-buff-planner-clear-plan`, from freshly fetched
`origin/main` `38f777aac4b9946ef674a1fb316f07b2925268f4`. The original clean
checkout/branch at `845f25e012d265c73d20979eb2c2701437c378fb` is preserved.
Product/test commit: `4aac21c759c3c6d505d25f7c7f4a0d961df500cd`.
Version remains **0.6.8**; no schema, dependency, merge or release changes.
This is an ordinary focused development checkpoint under
[the validation policy](VALIDATION-POLICY.md), not runtime qualification.

The current `CastingWorkspaceScreenView` places **Clear plan** immediately
before Undo in the footer. Both input modes use the existing two-row footer
pattern, with save/feedback above the full-size actions and the larger Run
caption retained. The button uses total document count, so an empty selected
routine does not disable clearing a populated plan. Disabled controls keep
the existing semantic inspection convention and explain empty/busy state.

The confirmation uses native PaperSlip/ThinFrame artwork, an opaque parchment
backing and a click-intercepting backdrop. Its title is **Clear entire plan?**;
the body counts the authoritative document with singular/plural handling,
explains that active buffs stay, and offers session Undo. Cancel and Clear plan
remain outside any scrolling content. Cancel starts focused; an exclamation
mark identifies the destructive action in addition to its label/color.

`CastingWorkspaceSession.BeginClearPlan` creates one pending token bound to
the authoring instance and revision. Edits, settings changes, reload attempts,
authoring replacement and screen disposal invalidate it. Confirm rechecks run
availability through an injected live execution-host callback (independent of the cached display snapshot) and uses one `CastingAuthoringService.RemoveCastings` call with
every current casting ID: one announcement, one ordinary autosave attempt and
one Undo entry. Empty/repeated/stale commands do not save or add history.
Routine definitions/order, campaign identity, import notices/acknowledgements
and execution/UI settings survive. Other campaign files and legacy profiles
are untouched; ordinary backup rotation and recovery protections still apply.
No executor, active effects, resources or execution history are changed.

Cancel/back/Escape do not edit. A neutral-frame edge latch prevents opening,
held and duplicate input from confirming; the controller boundary also clears
pending commands/repeat state at modal transitions. The dialog owns semantic
focus and blocks routine shoulders, inspection, background authoring and the
planner hotkey. Background interaction remains blocked until the closing input
is released. Cancel restores Clear plan; success restores Undo after the normal
rebuild and input release, with an enabled fallback. Clearing cancels armed
graph gestures/focus without resetting issued casting IDs. Tab/Shift+Tab,
arrows and Enter/Space use the existing semantic cursor in pointer mode;
native EventSystem Submit is suppressed/restored for that owned workspace
lifetime so one key cannot activate two different controls.

## Validation

Official Windows PowerShell/MSBuild, C# 7.3/.NET Framework 4.7 and installed
Kingmaker assemblies compiled the complete production project, including the
Unity view. No portable exclusions, assertion weakening or new framework.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'clear-plan-,casting-authoring-,casting-workspace-,autosave-,undo-history-,controller-workspace-,controller-input-,controller-editor-back-,direct-manipulation-,duplicate-items-,stale-item-casting-'
git diff --check
```

| Check | Result |
| --- | --- |
| Source validation (Build.ps1 child) | 47 PASS / 0 FAIL |
| Complete production build | 1 PASS / 0 FAIL |
| Focused C# runner at product commit | **69 PASS / 0 FAIL**, selector 16.9 seconds |
| New Clear plan regressions | **9 PASS / 0 FAIL**, included in 69 |
| Selection audit | All 11 prefixes nonzero; 69 registered tests |
| Diff whitespace | PASS |
| Native mouse/keyboard, physical controller and visual layouts | **NOT RUN** |

The real session/authoring/repository tests cover hidden and unresolved entries
in multiple/custom routines, empty selected routine, exact document Undo and
earlier history, one save via the preserved populated first backup, fresh-session
empty reload without legacy import, another campaign and settings isolation,
empty/repeated/cancelled/stale tokens, replacement with restarted revisions,
busy confirmation, locked-file failure/retry, protected corrupt-primary
recovery, continued graph editing and ID high-water preservation. Input checks
exercise the real edge latch and semantic focus model, not Unity rendering or
a physical controller. Existing exact-source/stale-item and spatial neighbors
remain in the focused selection.

Evidence: [final gate](evidence/clear-plan-2026-11-04/focused.log),
[prefix counts](evidence/clear-plan-2026-11-04/selection-audit.txt),
[artifact hashes](evidence/clear-plan-2026-11-04/hashes.txt).
DLL SHA256 `3ee3fd9a36744e6470b408c9c3f0469e4c849f2f730bc1b2a9fecbcc7d0fda9d`.
Later documentation commits do not relabel/rebuild this product artifact.

Earlier attempts are retained separately: the first production build passed,
but the test build missed the new linked partial and used the wrong graph
property. The next run was **62 PASS / 7 FAIL** because its unresolved fixture
violated the required non-null ability contract. Those test setup errors were
corrected; they are not baseline/runtime reproductions. A working-tree pass
of 69/0 preceded the input-ownership corrections. The 6f2ea82 pass was also 69/0 before the live-host review correction; the final 4aac21c pass includes the cached-snapshot regression and the updated footer spatial-navigation neighbor.
Rejected approaches: count visible cards or executable steps, remove each row
individually, delete the plan file, replace the authoring service/history,
approve only by revision without authoring identity, trust a cached display snapshot for current execution state, or rely on EventSystem
selection for controller focus.

## Pending native acceptance

No game launch, staging, installation/save mutation, package build, source/live
purity campaign or release publication occurred. The installed lab is present;
native interaction is pending because the existing current-product
source/live/launcher-WhatIf prerequisites require a separately budgeted guarded
boundary (historically 112–154 minutes, beyond the ordinary 30-minute ceiling).
See [the existing runtime boundary](LIMITED-USE-REPAIR-CHECKPOINT.md) and
[validation policy](VALIDATION-POLICY.md). Compilation and simulated focus
are not native acceptance. No visual or physical-controller observation is
claimed, including text wrapping, focus visibility or parchment layering.

Exact next action: prepare a matching runtime candidate for the product commit,
satisfy the unchanged launcher safety/source/WhatIf prerequisites, then use
`scripts/Invoke-KingmakerRuntimeTest.ps1` and its supervised
`live-workspace-manual` route with the protected disposable test pair and
transactional staging/restoration. Do not bypass a controller/scenario refusal
or use an unguarded launch. Only `KBP_AUTOMATION_WORKING` may be mutable;
`KBP_AUTOMATION_BASELINE` and all other saves remain protected.

At normal **1920x1200** and compact **1280x720**, record the button/dialog
layout, text wrapping, opaque layering, visible focus and both reachable actions.
With mouse/keyboard: open a populated multi-routine plan from a filtered or
different-routine view; open/cancel by button and Escape; reopen/confirm;
inspect empty cards/counts/inspector/forecast/Run/Undo/save status; Undo and
inspect exact restoration. Also reach Clear plan with Tab and confirm/cancel
with keyboard alone. Repeat the complete flow using only a controller,
including held/repeated opening Confirm, shoulders/underlying Run while modal,
Back cancelling only the dialog, focus returning to Clear plan/Undo, and no
second action from the closing press. Reopen to verify Cancel starts focused.
After clearing, add a casting and verify normal authoring. Reopen a cleared
campaign to verify the durable empty plan. Retain captures/input observations
and exact restoration receipts; leave unresolved native criteria NOT RUN.

## Draft PR and guarded push

Draft [PR #17](https://github.com/howardreith/KingmakerBuffPlanner/pull/17) is open
against main. The unchanged project-owned helper passed
`scripts/Test-GuardedPush.ps1`: **6 PASS / 0 FAIL** on clean checkpoint
`4f9a2de5d79694fb924457942a6ea8be8b9a537d`, then pushed and verified that exact
branch HEAD. See [helper result](evidence/clear-plan-2026-11-04/guarded-push-test.log).
The final receipt commit changes documentation/evidence only; it does not change
the product/test commit or DLL hash above. Merging, version changes and release
publication remain outside this mission.

The first `powershell.exe -File` helper invocation refused its string-valued
`-Confirm` switch before execution. A `-Command` invocation pushed the branch,
but PowerShell's serialized Git informational stderr interrupted its receipt.
Remote read-only verification found the intended exact HEAD. An ignored local
PowerShell wrapper then invoked the same unchanged guarded helper with a typed
`-Confirm:$false`; it completed **Guarded push PASS**, including remote
verification. No force push or alternative publication path was used.

Next action: review PR #17 and perform the guarded native steps above. Native
mouse/keyboard/controller and compact-layout acceptance remain NOT RUN.
