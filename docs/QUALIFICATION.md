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
