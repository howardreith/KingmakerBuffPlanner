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
