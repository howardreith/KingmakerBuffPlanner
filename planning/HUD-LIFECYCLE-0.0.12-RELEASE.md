# 0.0.12 HUD Lifecycle Release Record

Date: 2026-08-24

Status: PUBLISHED, VERIFIED, AND EXACT PUBLISHED BYTES INSTALLED

## Git and release identity

- Starting default branch: clean fetched `origin/main` at
  `4a83aec19e0f6098e23b2965b3992c328136c576`, version 0.0.11.
- Hotfix branch: `codex/kbp-hud-lifecycle-hotfix-0.0.12`.
- Human-acceptance checkpoint: `b51da8e4129645a523969a0e691db64ab59285d5`.
- Integration and release commit: `a48bfae2185a50f1c50d9151666e0b5ce0a0bc3e`.
- Annotated tag: `v0.0.12`; tag object
  `909253851300962204a7efdef36a251f60651dc1`; peeled commit
  `a48bfae2185a50f1c50d9151666e0b5ce0a0bc3e`.
- Release: `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.12`.
- GitHub state: published `2026-08-24T18:54:29Z`; draft=false;
  prerelease=false; target branch `main`.

The release was created through `scripts/Publish-Release.ps1 -Publish
-ConfirmHumanAcceptance -AllowPrivateRepositoryRelease`. It created a new tag
and release; it did not replace or mutate `v0.0.11`. GitHub still reports the
separate published 0.0.11 ZIP with SHA-256
`89cbebd2a1eb594d2307c4388c19588e1d4ea9c845284d36081c3e72d492795c`.

## Publisher gates and artifact identity

- Source validation: 34/34.
- Behavior/protocol tests: 91/91.
- Runtime filesystem tests: 8/8.
- Strict package validation: 4/4.
- Deployment WhatIf purity: 5/5.
- Source-only aggregate: 1/1.
- Release builder: 3/3 with two deterministic builds.
- Published ZIP: `KingmakerBuffPlanner-0.0.12.zip`, 227,942 bytes, SHA-256
  `1cbb2b215a78ab4dea2af5c99ebae211fe21e1f3532eb1865af3420a90ea8494`.
- Published DLL SHA-256:
  `1964b0220fd0ddd4a15009900a30ee3ec3af83c4d90b022eebb87d27cde03cac`.
- Published assembly version: `0.0.12.0`.
- Published MVID: `3947e19e-fd3b-4b11-95d8-8a1b360cf9a4`.
- `SHA256SUMS.txt`: 99 bytes, SHA-256
  `ae072c3398cd518d4735a8b764640563d07eee9b123808d49d6d4e21055324ed`.

The assets were downloaded again from GitHub. The GitHub digest, downloaded
ZIP, local release ZIP, and `SHA256SUMS.txt` agree. The downloaded package
passes the strict package validator 4/4 and contains only the four allowed mod
files.

## Published-byte installation

Guarded install `hud-lifecycle-0.0.12-published-install-20260824` replaced the
accepted pre-integration 0.0.12 build with the exact published bytes. Result:

- status `Installed`;
- source commit `a48bfae2185a50f1c50d9151666e0b5ce0a0bc3e`;
- live DLL SHA-256 and MVID exactly match the published identity;
- `otherModsVerified=true`;
- `settingsPreserved=true`;
- failure null;
- install-result SHA-256
  `bc7435f972acd52b4466781f2fcb2ead515364fe5d82528d7a03be1a4adefb76`;
- no Kingmaker or Unity Mod Manager process and no deployment lock remain.

Evidence is
`C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\install-hud-lifecycle-0.0.12-published-install-20260824\install-result.json`.

## Runtime boundary

The owner's campaign acceptance applies to the guarded-installed repair source
`083dfbfcf651d44bb01b302ccbbabac823e236e9`. Its log proves the same candidate
survived loading-screen raycast ownership and installed after 57 validation
frames with four buttons and four listeners, followed by successful Setup
presentation. The release commit contains the accepted production code plus
integration/release records; its changed binary identity comes from embedded
commit metadata. The exact published bytes were installed after publication but
were not launched in a second campaign during this release transaction.

The guarded save inventory remains baseline=0 and working=0. No unrelated save
was substituted, and unavailable save-backed automation rows are not claimed as
executed. Deterministic candidate-expiry, retry, stale-anchor, and steady-state
performance coverage remains green, and the accepted performance run remains
88.780 average FPS with zero global searches and exact restoration.

Exact next action: none for release engineering. The installed published build
is ready for ordinary use; any later campaign retest is additional confirmation,
not an unrecorded release gate.
