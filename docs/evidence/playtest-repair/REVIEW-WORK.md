# PR 20 correction work

Branch: `codex/kingmaker-buff-planner-playtest-repairs`. Version: `0.6.10`.
Starting HEAD: `6ca595e01e3ebbfed990a2a4c3c4eaf40ef48d89`.
These are development observations; native acceptance remains separate.

## Manifest throughput checkpoint

The previous read-only prerequisite audit did not complete. The backup root
currently contains 426,893 files (metadata enumeration: 22.93 seconds). Every
purity window still enumerates and hashes every file; no history, path,
timestamp, comparison, or guard is omitted. Hashing now uses four bounded
readers over the same PowerShell enumeration and ordering, without caching.

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-RuntimeManifestReader.ps1`
passed **9/0**, including byte-identical serialized manifests against the prior
reader, literal/hidden/empty files, same-size content changes, timestamps,
addition/removal, directories, unreadable files, missing roots and WhatIf.
Receipt: `artifacts/playtest-manifest-reader-tests.log`.

This is a harness performance correction, not a completed launch prerequisite.
Next: finish the recipe/effect-owner correction batch, freeze its candidate,
then complete the unchanged launch safety and WhatIf purity boundaries.

## Effect owners and native recipe checkpoint

Starting HEAD for the final correction batch:
`d613606308b849809f05a9e91bb899c4021fbe74`. Builds below contain the working
correction batch and are not frozen native candidates.

The requested feat-present, different-caster/recipient reproduction failed
**0/2** before correction (`artifacts/playtest-mixed-owner-baseline.log`).
The final focused selector passed **57/0**, source **47/0**, build **1/0**:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'playtest-heroism,heroism-repeat,existing-effect,live-existing-effect,group-existing-effect,group-mixed-coverage,native-boundary,execution-host,qualification,casting-qualification,effect-presence,conditional,discovery,source-identity'
```

Receipt: `artifacts/playtest-candidate-focused.log` (14.2 seconds). Both modes
confirm the correctly owned effects and execute a distinct successor. Missing,
wrong-owner, wrong-kind, suppressed, unreadable and unchanged effects still
stop continuation. Misplaced effects do not satisfy the already-active check.
An added projection test initially hit the existing `step-not-exact` guard
(24/2); its synthetic step was corrected to declare its exact enhancements.
The guard remains unchanged; the corrected focused projection checks were 26/0.

The exact installed CotW wrapper has an additional recipient transformation:
`ApplyActionToTargetsInRange` excludes the spell target and chooses nearest
eligible other units. The historical generic ActionList graph did not express
that transformation. Discovery now records the verified optional wrapper's
radius/limit/allegiance flags. Native provider snapshots bind concrete owners;
confirmation and sufficiency use those owners. Unreadable or ambiguous owners
cannot become ordinary-target coverage. Source identity is unchanged, while
concrete owner assignments enter the authorized projection hash. Observation
remains conservative for a secondary owner outside the readable party snapshot.
No gameplay-mod compile dependency was added.

`heroism-successor` selects loaded, affordable real Heroism plus Mirror Image
or Heightened Awareness, with different Heroism caster and primary recipient.
It forecasts first/recast/skip phases (2 + 2 + 1 native submissions) with real
resource reservations, no refill or forced effect. Selection records raw and
bound graphs, actual caster facts, successor identity and observation owners.
Native terminal evidence retains before/after owner snapshots and eight bounded
confirmation-poll samples. The native UI observer adds rendered modal idle,
both hover/exit states and safe Cancel without changing the authored plan.

`Test-HeroismQualificationEvidence.ps1` passed **8/0**: both-mode five-submission
allowances through the existing writer/readers, plus six before-write refusals.
Receipt: `artifacts/playtest-heroism-allowance-checks.log`.
The full C# runner passed **684/0** before the final projection/poll observer
edits (`artifacts/playtest-review-offline.log`); it is not a receipt for the
later bytes. Native launch remains NOT RUN at this checkpoint.

The mission copy was restored byte-for-byte to its authoritative sibling after
the earlier checkpoint's status prepend broke their existing equality check.
No validator or mission requirement was relaxed. Next: commit the batch and
profile location correction, build/freeze once, complete launch prerequisites,
then run selection, allowance-authorized gameplay in both modes, and native UI
acceptance. Do not merge or release.

## Profile drift resolution checkpoint

Product source: `892ada6` (full SHA recorded by the subsequent freeze receipt).
The full-user ZFavoredClass seal remains
`75f4ad7cc401cfe3a7672a69faf66b2682ab08f3fcadc770425bc089368275b8`
(24 files, 1,419,177 bytes). The verified historical archive is
`runtime-backups/kbp041-rel-catalog-full/Mods.staged/ZFavoredClass` in the lab.
The live directory is 24 files, 1,419,640 bytes, digest
`110347bd03e7442144ead8a15bdc7caf91235101d6fdea347783fe63c9459835`.
Exactly one path differs: generated `loaded_blueprints.txt`, sealed SHA-256
`8d4e3fbd00315631ef63ad559c855e6a33521bab13946f4dd9f05d9db9e1cadd`,
live SHA-256
`590ca0e2f5400097d215aeb3764072e4f4da56058a5820588d510751d3a64123`.

The full-user and advanced profiles now point to the exact external copy
`examples/ZFavoredClass-sealed-75f4ad7cc401`, using their existing
`fixtureRelativePath` contract. Every expected content/primary-file hash,
count, version and the compatibility identity digest remains unchanged.
No live file, dependency version, mod enablement or profile seal was changed.

`Copy-KbpSealedCompatibilityFixture.ps1` verifies archive containment and the
unchanged seal before copying, refuses an existing mismatched destination,
and verifies the result. Real WhatIf passed; wrong archived source and live
source both refused before copy. The real copy then verified. Receipts:
`artifacts/playtest-profile-copy-whatif.log`, `playtest-profile-copy-wrong-source.log`,
`playtest-profile-copy-live-refusal.log`, `playtest-profile-copy.log`.
Full-user identity checks passed **1/0** for all 15 mods with both old and new
manifest readers (`artifacts/playtest-profile-corrected.log`,
`artifacts/playtest-profile-fast-reader.log`). The live drift inventory is
retained privately in `artifacts/playtest-profile-drift.json`.

Next: freeze the clean candidate including these source-location changes,
complete launcher safety/source and WhatIf purity, then native acceptance.
