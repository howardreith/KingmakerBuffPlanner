# Source-ID producer compatibility review correction

2026-11-04. Addresses PR #16 review of `5ab600a164dc9e90f7c94e2c7eaed1448d89b4ec`.
Completed development correction; version remains 0.6.7 / 0.6.7.0.

Branch: `codex/kingmaker-buff-planner-duplicate-items-stale-castings-pr`.
Exact tested product/test commit: `8ff524d81c79fc393a6082338497a70fd6b82244`.
Later documentation commits preserve this build's source stamp.

The review is reproduced. `SingleCastProbeSelector.SourceIdFor` still selected
aliases through expression reference equality. An eligible plain spell whose
catalogue representative was another caster's independently scanned Extend
form therefore acquired its raw ability key. Canonical workspace membership
rejected that key, and the production selection-only qualification driver failed
with `author:graph-source-row-missing` despite successful shared-spell selection.

The shared helper now prefers `CatalogSourceIdentity.For(ability, expression)`
when the normalized entry is published. With no normalized entry it retains the
existing ordinal-first explicit alias and raw ability-key fallback; missing
discovery expressions still return null. Catalogue enumeration and matching are
unchanged, retaining exclusion of raw keys and provider-specific expressions.
Broadening graph acceptance or re-interning expressions was unnecessary.

Two regressions were added to `ShareQualificationDriverTests`:

- The existing `SharedBuffWorld` supplies discovered inputs, augmented with an
  equivalent Extend form on a second caster and a distinct expression. The real
  `PlannerSetupModel` publishes that form as representative. The actual shared
  selector and selection-only `CastingQualificationDriver` must complete graph
  authoring, preserve canonical casting source IDs, produce two forecasts,
  persist Share intent, and dispatch/spend nothing.
- The helper preserves legacy explicit aliases, raw fallback and missing-source
  refusal, and prefers a published canonical entry over an available legacy alias.

## Verification

Original reviewed production plus the new tests ran once before the fix. Source
validation **47 PASS / 0 FAIL**, production build **1 PASS / 0 FAIL**, regressions
**0 PASS / 2 FAIL**. The pipeline failed at the reported graph-source-row-missing;
the helper test failed at canonical-entry precedence. These are retained in
[baseline.log](evidence/source-id-review-2026-11-04/baseline.log).

```powershell
./scripts/Test-Development.ps1 -Only 'shared-recipes-catalogue-,probe-source-id-'
```

At exact product/test commit `8ff524d`, the official Windows PowerShell 5.1,
MSBuild, .NET Framework 4.7 / C# 7.3 gate compiled the entire production project
and official test runner against the installed game assemblies. No exclusions
or new dependencies. Source checks **47 PASS / 0 FAIL**, production build
**1 PASS / 0 FAIL**, tests **149 PASS / 0 FAIL**, 13.6 seconds.

```powershell
$prefixes = Get-Content docs/evidence/source-id-review-2026-11-04/focused-prefixes.json -Raw | ConvertFrom-Json
./scripts/Test-Development.ps1 -Only ($prefixes -join ',')
```

The selection extends the prior PR's affected prefixes with `shared-recipes-`,
`probe-source-id-`, `single-cast-probe-`, `qualification-`, and `policy-route-`.
Every selected prefix matches registered tests; the original eight regressions
remain included. The current pass and source/artifact hashes are retained in
[the new evidence directory](evidence/source-id-review-2026-11-04/).
Prior 127/0 evidence remains attributed to its original product.
`git diff --check` and current checkpoint links passed.

Built `artifacts/build/Release/KingmakerBuffPlanner.dll` SHA-256:
`e68918aa9fb8a9cc1b0b117b149962f9b305dae9b34a763e533f51a2c96c69c8`.
Official runner SHA-256:
`526c114081caae1142518782680afab4a9d24e12a5cb468e711331c2b03b3213`.

No game launch, Mods staging, installation or save modification, merge or
release publication. Native button rendering, physical mouse/controller input,
scrolling and gameplay spending remain NOT RUN; this correction adds no native
qualification. The exact next action is review of the pushed correction and,
when selected, the guarded native prerequisites/checks in the
[original PR checkpoint](ITEM-SOURCE-PR-CHECKPOINT.md).


## Selected ability expression review correction - 2026-11-04

Canonical source identity is retained for authoring while exact discovered ability expressions drive structural checks/projection. Reproduction 0/2; Windows gate on d904c1e passes 151/0 plus converter neighbors 6/0. Foreign references still refuse; native acceptance remains NOT RUN. Full-PR baseline-log whitespace was corrected while preserving original bytes in the new evidence. [Current correction evidence and next action](ABILITY-EXPRESSION-REVIEW-CHECKPOINT.md).
