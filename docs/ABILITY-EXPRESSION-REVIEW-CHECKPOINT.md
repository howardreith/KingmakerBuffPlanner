# Selected ability expressions and shared catalogue identities

2026-11-04. PR #16 follow-up to review of a2f226d. Development repair complete;
native rendering, mouse/controller/scrolling and live spending remain NOT RUN.

Branch `codex/kingmaker-buff-planner-duplicate-items-stale-castings-pr`.
Exact tested product/test commit `d904c1e2974fe908806ab0be8938d819971ac42c`.
Version unchanged: 0.6.7 / 0.6.7.0. Later documentation HEAD does not change the
tested DLL's source stamp. Existing worktrees and historical pass/fail results
remain intact.

## Cause and repair

The previous source-ID correction correctly returned a canonical catalogue ID,
but consumers retrieved that entry's representative expression for validation.
Catalogue equivalence ignores the blueprint ID on discovery reference wrappers;
two distinct spells applying the same buff can therefore share a catalogue ID.
The representative's wrapper correctly fails validation against the other spell.

`DiscoveredEffectLookup.ForAbility` now retrieves the selected ability's exact
discovered expression. Only legacy inputs lacking that ability key fall back to
the authored source ID. A present invalid or null expression never borrows a
valid alias. The probe selector, qualification selectors/forecasts and explicit
step converter use that lookup for effect checks and execution projection.
Canonical IDs remain in saved castings, graph authoring and projection source
identity. Existing rejection of references to another spell is unchanged.
There is no global expression merging or relaxation of structural validation.

Two new regressions use the real action scanner to create separate reference
wrappers for two distinct base blueprint IDs applying the same buff. The real
catalogue publishes unavailable A as representative; B remains available.
The real compiler and execution gate admit B, its SingleCastProbe projection
and selector succeed with B's exact expression, and the shared-spell selector
and selection-only qualification driver complete authoring/persistence. The
probe test also checks legacy source-only fallback and refusal when the exact
ability graph references another spell despite a valid legacy alias.

## Validation

Before the production fix, reviewed a2f226d plus only the new regression tests
ran once through the official Windows gate: source **47/0**, build **1/0**,
tests **0 PASS / 2 FAIL**. The probe failed `probe-unsupported:effect-shape`;
shared qualification failed `no-eligible-shared-recipe`.

```powershell
./scripts/Test-Development.ps1 -Only 'probe-catalogue-distinct-,shared-recipes-distinct-'
```

At exact product d904c1e, the official Windows PowerShell 5.1 / MSBuild / .NET
Framework 4.7 / C# 7.3 build compiled all production (including Unity screen)
against the installed game assemblies and compiled the official test project.
Source validation **47 PASS / 0 FAIL**, build **1 PASS / 0 FAIL**, focused
tests **151 PASS / 0 FAIL**, 21.7 seconds. Relevant converter/projection checks
on that same compiled runner passed **6 PASS / 0 FAIL** separately.

```powershell
$prefixes = Get-Content docs/evidence/ability-expression-review-2026-11-04/focused-prefixes.json -Raw | ConvertFrom-Json
./scripts/Test-Development.ps1 -Only ($prefixes -join ',')
# Same runner, KBP_TEST_GAME_PATH set via scripts/Common.ps1 Get-KbpGamePath:
./artifacts/tests/KingmakerBuffPlanner.Tests.exe --only explicit-casting-step-conversion projection-identity-is-complete probe-scope-enforces-whole-subset converter-refuses-unsupported-contracts casting-c5-projection-carries-effects-forward zero-cost-native-source-integration
```

This includes all original duplicate-item/stale-casting regressions, both prior
review regressions, scanner-wrapper regressions, shared/policy recipes,
qualification, probes, native fact-resource and affected controller neighbors.
Two optional selector guesses (`explicit-step-`, `explicit-projection-`) matched
zero and supplied no coverage; the six exact converter/projection names above
were then run successfully. Registration counts are recorded without relabeling
unmatched prefixes. This is focused development coverage, not full qualification.

Current logs, selector audit and hashes:
[evidence directory](evidence/ability-expression-review-2026-11-04/).
Built `artifacts/build/Release/KingmakerBuffPlanner.dll` SHA-256:
`616b0269c61411e296e5308d22812e574582dd08534db221a7e62efc91833679`.
Runner SHA-256:
`5d7b9e5591cf9615cf930a58133e463d7ad41720d0e7ba602062bc967efd9d8d`.
No dependencies, warning settings, gates or assertions were weakened.

The review's whitespace discrepancy was confirmed: full PR diff checking found
whitespace-only lines and a blank EOF in the earlier source-ID baseline log.
Readable baseline logs now have trailing whitespace/blank EOF normalized.
Their original exact bytes and SHA-256 remain in
[baseline-original-bytes.json](evidence/ability-expression-review-2026-11-04/baseline-original-bytes.json)
as base64, preserving the original evidence. Historical pass/fail conclusions
are unchanged. Current incremental and full-PR whitespace checks pass; prior
blanket wording did not establish a full-PR clean result.

Exact next action: review the pushed correction; when native acceptance is
selected, follow the existing package, source/live and launcher WhatIf purity,
disposable-save, transactional staging/restoration and run-bound allowance
requirements in the [original PR checkpoint](ITEM-SOURCE-PR-CHECKPOINT.md).
No game launch, staging, installation/save modification, merge or release
publication occurred during this correction.
