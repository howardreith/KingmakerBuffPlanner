# Qualification

Status: PASS for all applicable no-save gates; save-backed core remains `DEFER — EVIDENCED`

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

Phase 10 executable routine workflow and first no-save HUD smoke PASS; final Phase 10 gate remains IN PROGRESS:

- source commit: `420d7a1f1f20f49706a34d953df5f0d39f67e4a8`;
- source gates: validation 15/15, behavior 45/45, harness 5/5, deployment purity 5/5, package 4/4;
- runtime run: `20260811T2144351145396Z-ui-root-smoke`, 11/11 assertions;
- observed: one UI root, 12 open frames, two open/close cycles, three routine buttons, critical controls on-screen, 2560x1440;
- package / DLL / MVID: `e544b7b2940a455c9ac886237ae5d42420c8b32b6657736af06661c369406e72` / `bd3248bb56314fa68d8ecaf16761410c114d2578df24fcdd1da4cdcd1e35bdfb` / `cfa3b527-3186-4c0a-ba07-21fbaa49bf36`;
- transaction: `Restored`, `restorationVerified=true`, no save selected or mutated.

This proves the no-save HUD controls render and fit at one representative resolution. It is smoke 1/2, not the Phase 10 gate: remaining setup polish, common scale/resolution rows, transition/reload subscription evidence, and configured campaign execution require further work or the authorized `KBP_` fixture.

Phase 10 final no-save HUD gate PASS twice:

| Run ID | Assertions | Root/reconstruction | Layout profiles | Blockers/subscriptions | Restoration |
|---|---:|---|---:|---|---|
| `20260811T2201533093853Z-ui-root-smoke` | 15/15 | 1 / 1 | 3/3 | 0 / 0 | exact PASS |
| `20260811T2202528930958Z-ui-root-smoke` | 15/15 | 1 / 1 | 3/3 | 0 / 0 | exact PASS |

Both runs used commit `cfce05c921735c33617342e9b409eeae556a16f9`, package SHA-256 `c2aaac56568366e275302650dbaecd15d984158b5af4f98a670bd90b478937ba`, and DLL SHA-256 `961ac1a3b2c5f9194fb67413e12ba0c5e034ff2d11f2e240f16d1d02cf4e75f6`. Each rendered 12 frames across two cycles, destroyed/reconstructed the owned root once, ended with one root, exposed all three routines, and validated layout bounds for 1920x1080 at 1.0, 2560x1440 at 1.25, and 3840x2160 at 1.5. Actual process resolution was 2560x1440. No save was selected. Configured campaign execution remains `DEFER — EVIDENCED`, not PASS.

Phase 11 exact compatibility qualification PASS for all locally available no-save profiles:

| Profile | Run IDs | Assertions | Catalog SHA-256 | Harmony targets/records/overlaps | Restoration |
|---|---|---:|---|---|---|
| `call-of-the-wild` | `20260811T2241040368066Z`, `20260811T2242392501436Z` | 26/26 each | `7b54f3f9f6d90d339c4cabeedb04c9d15bcb4d51d8e7d830150a18ab6eced659` | 207 / 228 / 0 | exact PASS twice |
| `native-only` | `20260811T2244134771182Z`, `20260811T2245180617144Z` | 12/12 each | `50f66299912bef24a50984d9d8398ba2bb340a4f85b551a0ad6ff97c41393f3d` | 3 / 3 / 0 | exact PASS twice |

All four used commit `57ed740d6fd6bbdf68dcdfe8c26368c744a3c91f`, package SHA-256 `29ee2ddb86f6ace36d9b9ec1cc7e75a2468f81721956836e39dbdae26752deb2`, DLL SHA-256 `2c79eefe2afc9a93ec0574a0056c7c9331ebb1add170561f6da4c72468687b13`, and MVID `18be2ec9-702a-4ba1-ae15-306765e4231d`. The optional runs proved exactly one UMM entry and assembly for both products, exact Call of the Wild version/hash, four representative owned/included abilities, 2,096 optional inclusions, and zero optional unsupported candidates. Catalog and Harmony inventory hashes repeated byte-for-byte per profile. No save was selected or written. Tabletop and combined profiles are unavailable locally; save-backed native/optional execution remains `DEFER — EVIDENCED`. See `docs/CALL-OF-THE-WILD-COMPATIBILITY.md` and `docs/TABLETOP-ADDED-RULES-COMPATIBILITY.md`.

Phase 12 composed no-save core qualification PASS twice:

| Run ID | Assertions | Catalog / Harmony SHA-256 | UI proof | Restoration |
|---|---:|---|---|---|
| `phase12-no-save-core-1` | 22/22 | `df2a48e61677723d1687b828d261ba4c103d4351b0f393d1e97276b84d7b8cb6` / `b5605e22bde458a238d63c6ffe33a99eb712bd22bf3cbc74c42d443ad479efb4` | root 1, reconstruction 1, frames 12, cycles 2, layouts 3, blockers/subscriptions 0/0 | exact PASS |
| `phase12-no-save-core-2` | 22/22 | same byte-for-byte | same | exact PASS |

Both fresh processes used clean commit `3af45f3329df300dc0616da9393480abee8547ce`, package SHA-256 `c2ea8a3dbbfb1cbe670be422b33e11a8afddfeaac4ae27902d69f8c5f6febc19`, DLL SHA-256 `a4b56a59104f5ddcac8a4184ebc4f779216f83aaeb4c0b7e199da9dcfa650413`, and MVID `193901f8-0863-4622-8885-f35880e5daf9`. Each composed identity, full catalog/expression reconciliation, ordered Harmony inventory, and UI lifecycle/layout checks in one process. This is the applicable NO-SAVE core, not executor equivalence; the save-backed core remains unmet.
