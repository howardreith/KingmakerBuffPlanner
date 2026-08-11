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
