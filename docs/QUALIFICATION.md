# Qualification

Status: TODO

Current source-only Phase 1 evidence (pre-checkpoint build from HEAD `4ca6008d873577e8e6263b54658620b649f81cd1`):

- source validation: PASS 14, FAIL 0;
- strict runtime protocol tests: PASS 11, FAIL 0;
- Release compilation: PASS 1, FAIL 0, zero warnings;
- package validation: PASS 4, FAIL 0;
- deterministic package repetition: PASS 2 identical builds;
- DLL SHA-256: `3b21bec7b283ab3f760e1c7fe81de6b9d06794696d323e35c9c2d498c4a78cee`;
- DLL MVID: `e07bf29a-cd45-44bf-870c-6cfa19988822`;
- package SHA-256: `f109c51b8afcd0af931c556ad5999ea62c1b88f951178da8d563253c18de4d21`;
- package entries: 3 allowlisted project-owned files, 0 prohibited payloads.

These hashes will change after the implementation checkpoint because the exact Git commit is embedded in the DLL. No runtime claim has been made. The Phase 1 gate remains IN PROGRESS pending guarded mod-load smoke twice from fresh processes and exact live-state restoration.

First guarded runtime attempt `20260811T1931581708021Z-mod-load-smoke`: FAIL at `unhandled-exception`. UMM loaded exactly 1/1 mods and the production entry point/commit, but a trailing-separator path bug resolved the game root as `Mods`; the runtime result was atomic and reported missing `Mods\Kingmaker.exe`. The game exited, guarded recovery then restored the exact original manifest (`restorationVerified=true`, manifest equality true, lock absent). This is not a mod-load PASS. Regression tests now cover both trailing and non-trailing UMM mod paths, and the orchestrator waits for the launched process to finish before its final restore decision.

Second guarded attempt `20260811T1935262721216Z-mod-load-smoke`: in-game result PASS with 5/5 assertions and all exact hashes, MVID `16d7db94-a319-4242-b036-e303d8ca9ddf`; restoration verified in `finally`. Orchestration then failed because its validator expected display form `0.28.2` while UMM’s exact runtime API returned `0.28.2.0`. The validator now uses the exact runtime identity and reports field-specific mismatches. Because orchestration did not complete PASS, this run is retained as diagnostic evidence rather than counted toward the required two-pass gate.

Phase 1 fresh-process qualification PASS:

| Run ID | Assertions | Commit | MVID | Restoration |
|---|---:|---|---|---|
| `20260811T1938252394711Z-mod-load-smoke` | 5/5 PASS | `d3f4dc1fec9970b1c3c8eed5100052edd996c870` | `e268ff99-af2e-4def-8511-746cbd1b5106` | PASS, exact manifest |
| `20260811T1939275269790Z-mod-load-smoke` | 5/5 PASS | `d3f4dc1fec9970b1c3c8eed5100052edd996c870` | `e268ff99-af2e-4def-8511-746cbd1b5106` | PASS, exact manifest |

Both runs used package SHA-256 `885113c4f8e1bb3a271579188823ceb3704a3b1f531ddce32efe26fa5f295764` and DLL SHA-256 `1470d6f3bc45d7612f7668e87ce862ff7d89aa415e5634581e8b23924a4ba235`. Runtime identities were Kingmaker 2.1.7 (displayed 2.1.7b; executable hash already fingerprinted), UMM 0.28.2.0 with exact hash, and Harmony12 1.2.0.1 with exact hash. UMM created its normal generated DLL cache only in the transaction-owned staged tree; both states recorded the mutation and discarded it before exact restoration. Final checks: game process count 0, deployment lock absent, live/original manifest equality true.

Phase 2 structural catalog qualification PASS:

| Run ID | Assertions | Catalog SHA-256 | Counts (abilities/candidates/effects/diagnostic abilities) | Restoration |
|---|---:|---|---|---|
| `20260811T1958580589645Z-native-buff-catalog` | 8/8 PASS | `bcacbe69bc71c85c5299b8fe8254c18baa33d66e9e3ecf53f6d4aa6b37094878` | 1,722 / 1,353 / 1,095 / 755 | PASS, exact manifest |
| `20260811T2000040593873Z-native-buff-catalog` | 8/8 PASS | `bcacbe69bc71c85c5299b8fe8254c18baa33d66e9e3ecf53f6d4aa6b37094878` | 1,722 / 1,353 / 1,095 / 755 | PASS, exact manifest |

Both runs used commit `07dc2380abbac74228eed88ce73113aeeabe61db`, DLL SHA-256 `61b63cfa352ae9bc9e15b8aa10a08ff9f2fe2ac3ff2c5ee059f38d2f4e0df975`, MVID `7f7ba872-03ce-4554-ae1d-3a5942e62e8d`, and package SHA-256 `b128386d8d6b6715a4d51190faf1d971304bd0fb748c7c0d4f57d6293ec9b20c`. Source-only gates were validation 15/15, protocol/domain 20/20, harness 5/5, package 4/4, deployment WhatIf purity 5/5. No save was selected or accessed.

Rejected evidence: run `20260811T1952280447102Z-native-buff-catalog` exposed an internal-type JSON opt-in defect (`native-buff-catalog.json` was `{}`); later runs before the full expression contract showed `{}` expressions. These are defects found and repaired, not qualification passes. Runtime and PowerShell validators now reconcile the root array/counts and require an expression discriminator on every row.

Phase 6 no-save UI root qualification PASS:

- run: `20260811T2036033407315Z-ui-root-smoke`;
- commit: `c65fec1c83dd9bdae3ea5dd0b445436eff933102`;
- assertions: 9/9;
- singleton roots: 1;
- repeated open/close cycles: 2;
- rendered open frames: 12;
- observed resolution: 2560x1440;
- DLL / MVID: `ae459a27720feec946bb29efb12b3dd742b15291f835efbf575db022785032d9` / `c488303c-b96e-4346-a3fb-31ee28ddd5cc`;
- package: `ed21469ffe335f6111a477ef3bda0d4690a82ae1bf446882662726b769f6ca9f`;
- transaction: `Restored`, `restorationVerified=true`; no save selected.

This proves exact UI module load, repeated lifecycle, IMGUI render, singleton behavior, and one representative resolution. It does not prove campaign configuration interaction, scene transition, 1920x1080, or 3840x2160.

Phase 9 native classification qualification PASS; included-source runtime equivalence DEFER — EVIDENCED:

| Run ID | Commit | Catalog SHA-256 | Audited/support/exclude/unsupported | Restoration |
|---|---|---|---|---|
| `20260811T2126328544602Z-native-buff-catalog` | `fba6e24` | `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa` | 974 / 413 / 561 / 0 | PASS exact |
| `20260811T2127318144905Z-native-buff-catalog` | `fba6e24` | `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa` | 974 / 413 / 561 / 0 | PASS exact |

Both runs were byte-identical. Support classes are 396 automatic, 1 generic reflection wrapper, 13 explicit adapters, and 3 overrides. The package/DLL hashes were `766514afc64d96cd719b2237d3435156987f05ba46d6f10b0f09f0806647ca79` / `0e0423f4d33f733421a9181299b661b608b8a328e16ba479661c66c82318cb35`. Source gates were validation 15/15, behavior 43/43, harness 5/5, deployment purity 5/5, package 4/4. No save was selected. The 413 included rows remain runtime-deferred because provider/resource/effect/executor equivalence needs an authorized project-owned save.
