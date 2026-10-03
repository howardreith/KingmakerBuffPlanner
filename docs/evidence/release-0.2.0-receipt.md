# Kingmaker Buff Planner 0.2.0 — release receipt (2026-10-03)

Published on the owner's instruction ("make this a full official release").

| Item | Value |
| --- | --- |
| Release | https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.2.0 (published 2026-10-03T14:09:29Z, Latest, not a prerelease) |
| Tag | `v0.2.0` (annotated) -> `bbb238c5af1dd5b96172a474664941f4687bf57d` |
| `main` | `bbb238c` — merge of `codex/kingmaker-buff-planner-everyday-use` (PR #2 closed as merged by it) |
| Asset | `KingmakerBuffPlanner-0.2.0.zip`, 701309 bytes, SHA-256 `589e0c6155e2eb0f5408ba7e70ad5c2294cbcb4d3441ba4d30fa507c365fc91d` (+ `SHA256SUMS.txt`) |
| DLL SHA-256 / MVID | `d6495682f460480bb8d079d2a8d6a2900bc7a2ed0917e3c5e68fefb7980da82e` / `45941e90-a506-4ef5-9456-3f6e001bccfb` |
| Bytes | the frozen Build-Local package of bbb238c (`runtime-backups/qualification-frozen/bbb238c5.../`), renamed; package validation 4/4; downloaded back and checksum-verified |

## Qualification basis

- Code: identical to the qualified candidate `8d7681d` except the version
  surfaces (Version.props, Info.json, informational assembly version) and
  documentation. 8d7681d passed the full source-only gate
  (`artifacts/gate-8d7681d.log`) and 41/41 guarded in-game runs (r15; see
  `docs/E01-E27-ACCEPTANCE-MATRIX.md`).
- Release bytes in game: `beta-bbb238c5r16-reload-01` PASS (version 0.2.0,
  commit bbb238c and the DLL hash checked in game; workspace interaction,
  reopen and save reload; restoration verified).
- `beta-bbb238c5r16-phys-sel-01` could not deliver physical input: the owner
  session was RDP-attached and Windows refused the game window focus
  (environmental; restoration verified). The physical cold-moon chain was not
  rerun on the release bytes; its code is identical to r15's PASS at 8d7681d.
- At the owner's request the guarded publisher's full gate rerun (~4 h, four
  lab-wide purity windows) was stopped before it tagged anything; the tag and
  release were created directly from the in-game-checked build.

The private draft release 399014941 (preview assets) is unchanged.
