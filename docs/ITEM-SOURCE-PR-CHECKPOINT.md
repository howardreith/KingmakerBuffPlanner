# Duplicate item sources and stale casting inspection

2026-11-04. Completed development fix prepared for PR review. Version remains
0.6.7; no release, staging, installation change or game/save mutation.

| Identity | Value |
| --- | --- |
| Branch | `codex/kingmaker-buff-planner-duplicate-items-stale-castings-pr` |
| Current main base | `76c030511f6d44bda5acb82f90fbe0167b4d060c` |
| Tested product/test commit | `e1f8c916613b4d015d8022b1195f403d9498307d` |
| Version | Unchanged 0.6.7 / 0.6.7.0 |
| Evidence | [Current PR evidence](evidence/duplicate-items-pr-2026-11-04/) |

The initial checkout was clean on b8237a4 (0.6.2). The repair was implemented
there at 067c2af with records at 845f25e. On the owner's subsequent request to
open a PR, current main was fetched and this separate worktree created at
`private/worktrees/KBP-DUPLICATE-ITEMS-PR`. Those earlier branches, worktrees and
evidence remain intact. Main already contains the native item-resource repair
and newer controller work, so the PR applies only the seven-file code/test
candidate after a successful `git apply --check`; it imports no prerequisite
repair and reverts no newer controller or release changes.

## Behavior and diagnosis

Catalogue membership previously relied on expression-object reference equality.
Independent scans of identical item copies therefore lost peer copies/owners.
Membership now uses cached canonical catalogue identities without replacing
provider-specific expressions, item tokens, execution bindings or charge pools.
Raw ability keys cannot appear as false legacy catalogue aliases. Equivalent
spells aggregate while distinct class abilities and concrete variants retain
their groups. Multiple available copies require an exact choice.

A stale nonspell inspector could display a replacement provider. Item and legacy
free-Fact selections now remain visibly unresolved until explicitly repaired.
An unresolved inspector offers one numbered `Remove casting #N` action near its
top; resolved castings retain one bottom action. Changing casting focus stops
inertia and resets inspector scroll; same-casting refresh preserves position.
Exact CastingId deletion, autosave, persisted Undo, controller registration and
the ambiguous-recipient guard are unchanged.

The screenshot establishes that Miria matches both #1 and #4; it does not prove
the exact deletion command failed. The prior Windows baseline reproduced all
eight regressions (0/8) in an isolated b8237a4 worktree; both stale failures were
inspector-identity failures. That evidence remains on branch
`codex/kingmaker-buff-planner-duplicate-items-stale-castings`, not relabeled as
a baseline execution against this main revision.

Rejected approaches remain item-name/GUID exceptions, pooled copy charges,
global expression interning, silent saved-token replacement and exposing
arbitrary unequipped inventory. Session item identity survives same-object
rescans/reopen, but full game reload may require explicit reselection. The
reporter's equipment state and precise chip-to-Remove interaction remain unknown.

## Current validation and next action

At the exact product commit above, Windows PowerShell 5.1 and the official
MSBuild/.NET Framework 4.7/C# 7.3 gate compiled all production, including the
Unity screen, against installed 2.1.7b game assemblies and compiled the official
test runner without exclusions. Source validation **47 PASS / 0 FAIL**;
build **1 PASS / 0 FAIL**; selected tests **127 PASS / 0 FAIL**, 10.7 seconds.
This includes six duplicate-item regressions, two stale-casting regressions,
all bundled affected neighbors, native fact-resource tests, current controller
tests and relevant execution/binding neighbors. This is a focused pass.

```powershell
$prefixes = Get-Content docs/evidence/duplicate-items-pr-2026-11-04/focused-prefixes.json -Raw | ConvertFrom-Json
./scripts/Test-Development.ps1 -Only ($prefixes -join ',')
```

`git diff --check` passed. Matching test registration counts and artifact hashes
are retained in the evidence directory. Installed `Assembly-CSharp.dll` SHA-256:
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`.
Built `artifacts/build/Release/KingmakerBuffPlanner.dll` SHA-256:
`a6ca7fb8a2d0d04b603ae7d5a30f67a582a7b6f7f75beaed545d78b2e58c0256`.
The later documentation HEAD does not change that artifact's source stamp.

Native Unity rendering, mouse/controller input and live resource spending are
**NOT RUN**. Detached native property/binding/Spend tests do not qualify live
gameplay. Exact next action after review is to prepare this candidate's guarded
runtime package, satisfy current source/live and launcher WhatIf purity
prerequisites, and use the approved disposable save pair with transactional
staging and exact restoration. Verify mouse/controller chip #1 to numbered
Remove, retention of #4, Undo/reopen, one removal control, focus-change scroll
reset, same-focus preservation and controller ownership. Observe all genuinely
available copies/owners and their independent budgets; perform actual spending
only through the guarded run-bound casting allowance workflow. No native gate
is weakened and no full-suite, layout, compatibility or release campaign is
selected by this PR preparation.


## PR #16 review correction - 2026-11-04

Source-ID producers now prefer published canonical catalogue membership while retaining legacy fallback. Reproduced 0/2; official Windows gate on 8ff524d passed source 47/0, build 1/0 and tests 149/0. Historical counts above retain their original provenance. Native rendering/input/gameplay remain NOT RUN. [Current review checkpoint and evidence](SOURCE-ID-REVIEW-CHECKPOINT.md).


## Selected ability expression review correction - 2026-11-04

Canonical source identity is retained for authoring while exact discovered ability expressions drive structural checks/projection. Reproduction 0/2; Windows gate on d904c1e passes 151/0 plus converter neighbors 6/0. Foreign references still refuse; native acceptance remains NOT RUN. Full-PR baseline-log whitespace was corrected while preserving original bytes in the new evidence. [Current correction evidence and next action](ABILITY-EXPRESSION-REVIEW-CHECKPOINT.md).
