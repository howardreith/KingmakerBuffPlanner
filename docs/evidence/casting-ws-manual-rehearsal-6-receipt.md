# Receipt — `casting-ws-manual-rehearsal-6` (independently re-verified 2026-09-22)

Sanitized summary of the local raw records on DATA. Verified by Claude
(takeover implementer) from the raw files, not from the status sentence
that previously reported this run. Private originals remain local; no
save, installed DLL, or desktop capture is published here beyond the
already-published game-window frame below.

## What this run is — and is not

- **Is:** an unattended done-path rehearsal of `live-workspace-manual`
  (`-ManualHoldSeconds 60 -ManualRehearseDone`). The launcher wrote the
  labeled rehearsal marker `{"stage":"manual-done","by":"rehearsal"}`
  20 s after `manual-ready.json`.
- **Is not:** human participation, usability acceptance, stop/deadline
  runtime proof, native aesthetic acceptance, or native casting.
- It ran the build at functional commit `0551949`, **before** the J2
  repair. That build did not consume the final capture's failure or
  restoration verdict; the verdict below was recovered from the game
  log, not from the run's own acceptance contract.

## Identities (raw `runtime-result.json` / build receipt)

| Field | Value |
|---|---|
| Scenario / profile | `live-workspace-manual` / `full-user` (15 mods) |
| Result status / stage | `PASS` / `completed` |
| Source commit embedded in DLL | `05519495bccbb196a58b16d463c3d4affad08df0` |
| Build-local manifest commit | `05519495bccbb196a58b16d463c3d4affad08df0` |
| Package SHA-256 | `c40b624fe86b2d87fd8a8789345f4410aa048fbdaa54c815781439e06af47d57` |
| DLL SHA-256 | `ed41f01f78964aefc6cd66218ca81114c82e6c793d62edcaf6e1cb8fe1762662` |
| Assembly MVID | `566d8da3-9f90-4717-80ea-d0f3701cd813` |
| Kingmaker PID / started | 29948 / 2026-09-22T17:37:20Z |
| Result written | 2026-09-22T17:39:06Z |
| WORKING save | `KBP_AUTOMATION_WORKING` (`Manual_305_…`), area JamandisMansion |
| `runtime-result.json` SHA-256 | `6ce48697d469da58846baebd6a88e01b698ab2229162e80bd08b30d5ce785682` |
| `orchestration.json` SHA-256 | `0d3106418b1bc97ac3b345e716614b8e752bcea08d3e2098427fe6e23c1b29b3` |
| `transaction.json` SHA-256 | `5b77f5b490c11c314d6923f882a420ff3e9beb084b5adf02de609289feb6738d` |

All 15 optional mods passed per-assembly SHA-256 / version / uniqueness
assertions. Provenance hashes for the three separately approved fixtures
match their approved directory manifests (BagOfTricks `c4487d11…`,
CallOfTheWild `988e6130…`, CheatMenu `7960517c…`).

## Lifecycle observed in the raw records

1. `programmatic-open.json` — candidate opened with no input request.
2. `manual-ready.json` — `syntheticInputRequested=false`,
   `workspaceOpen=true`, `legacyScreenClosed=true`, `holdSeconds=60`.
3. `manual-done.json` — the labeled rehearsal marker.
4. Game log, in order: final camera capture
   `camera-path restoration;targetsRestored=True;activeRestored=True;cleanupFailures=0`,
   then `[KBP-MANUAL] manual phase terminal;outcome=manual-completed;by=done-marker;workspace=closed`.
5. `manual-final.png` SHA-256
   `6eb9f7113632acf8ad5190b9ce4e850bf2be20c441ee8cc2a138c12b185e40ab`
   (identical to the published `casting-ws-manual-rehearsal-6-final.png`).
6. Orchestration: 21 window samples, three transient `responding=false`
   samples during campaign load, result validated, owned exit.

## Restoration (independently re-checked)

- Transaction status `Restored`, `restorationVerified=true`,
  `restorationFailure=null`, restored at 2026-09-22T17:39:12Z.
- No `deployment.lock` present; backup/staging run directories empty.
- **Independent re-check:** the live `Mods` directory re-enumerated with
  the project's own `Get-KbpDirectoryManifest` matches the transaction's
  recorded `originalManifest` exactly — 1109 entries, 0 differences in
  path, length, SHA-256 or timestamp.
- No Kingmaker process running at takeover.

## The `harness-error.txt` file in this run

It contains five lines of `Cannot find a process with the name
"Kingmaker"`. These are non-terminating records from the launcher's
`Get-Process -Name Kingmaker -ErrorAction SilentlyContinue` polling
after the game exited (PowerShell still appends them to `$Error`, and
the launcher's `finally` persists `$Error` whenever it is non-empty).
They are not a failure of this run. Noted as launcher noise; not
changed in this checkpoint.
