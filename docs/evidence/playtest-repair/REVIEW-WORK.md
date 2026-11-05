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
