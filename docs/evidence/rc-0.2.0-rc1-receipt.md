# Release candidate 0.2.0-rc1 — receipt (2026-09-23)

The exact candidate for the owner's final review. Local only: nothing is
published, tagged or permanently installed. The pull request stays a
draft.

## Identity

| Item | Value |
| --- | --- |
| Commit | `f8562a65246952b7f86496d0823996f1044ce71e` |
| Version | 0.2.0-rc1 (assembly 0.2.0.0) |
| Package | `fa8b70193478c66dd457e251bddbaa0ed5f127658ed9fcf86dbb44b8de227f87` (the release ZIP and the harness package are byte-identical; two deterministic builds) |
| DLL SHA-256 | `200924e76723315a3470b474be53e9b01c04b559250b94b0e928b86d87fcf3b9` |
| MVID | `aeac680d-c930-4437-b1e4-a5204fd2478a` |
| Frozen copy | `C:\Dev\KingmakerBuffPlannerLab\runtime-backups\rc-frozen\f8562a65246952b7f86496d0823996f1044ce71e\` (read-only, `FREEZE.json`) |
| Clean checkout | `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC1` (detached at the commit) |
| Release artifact | `...\KingmakerBuffPlanner-RC1\artifacts\release\0.2.0-rc1\` (`KingmakerBuffPlanner-0.2.0-rc1.zip`, `release-manifest.json`, `RELEASE-NOTES-DRAFT.md`) |

## Source tests at the candidate

Full gate (Windows PowerShell 5.1, non-interactive, clean tree): source
validation 42/42, protocol tests 296/296, runtime-harness filesystem
35/35, package validation 4/4, deployment WhatIf 5/5, launcher WhatIf
12/12, fixture inventory 3/3, Restore-InstallLocal 16/16, guarded
publisher 3/3. Every guard added in this iteration has a mutant the tests
catch.

## Native gameplay on the candidate

Guarded runs of the frozen build on the disposable automation campaign
(WORKING save; never an ordinary save), `full-user` profile with the
owner-approved exact copy of the sealed KingmakerGunslinger 0.0.133,
isolated test-mod settings. Every run confirmed in game that it loaded
commit `f8562a6` and MVID `aeac680d...`, restored the owner's Mods folder
byte-exact (verified), and left every save unchanged. Each run's
`run-completion.json` is complete.

| Run | Assertions | What it showed |
| --- | --- | --- |
| `casting-qual-select-20260923-rc1-01` | 85 PASS | Recipe `zero-cost-mixed`: Resistance by two casters on the other party members; the same three projection ids as the earlier qualification |
| `casting-qual-cast-20260923-rc1-01` | 85 PASS | Native casts through the production Apply, host and executor under the run-bound allowance: stop (first casting confirmed, the others not started), complete (first skipped as active, two confirmed), repeat (`nothing-to-cast:3`), recast after a close and reopen (confirmed). Exactly the three approved submissions; zero violations; no save changed |
| `casting-ws-import-20260923-rc1-01` | 84 PASS | First open with a classic plan seeded from live discovery: the production migration imported it as two drafts needing review (none Ready, both without a caster); the classic file byte-unchanged and archived byte-exact; cards explain themselves in words |
| `casting-ws-reload-20260923-rc1-01` | 92 PASS | Authoring (three castings, refused Add, focused edit, retarget, Undo, Done, Save), close and reopen, then the exact WORKING save loaded again in game under the read-only saver and the write sentinels: no write observed, Game.LoadGame correlated to the exact descriptor, native saver restored only after the after-load callback; the reopened planner showed the saved plan (also read back from disk), one area unload and one loading-complete, one subscription, one HUD root (inactive ones counted), no run |
| `casting-ws-manual-20260923-rc1-rehearsal` | 88 PASS | Rehearsal of the supervised manual session (not acceptance): manual-ready with no synthetic input, done marker written automatically after 20 s, final capture, camera restored, planner closed |

The allowance for the casting run
(`approvals\casting-qual-cast-20260923-rc1-01.json`, schema 3) was written
exclusively by Claude under the owner's delegated mission authority,
bound to the frozen commit, package, DLL, MVID, fixture campaign, recipe,
the three projection ids and a budget of 6 submissions.

## Temporary guarded deployment (mission section 11)

`rc1-temp-deploy-20260923-01`: the release package was installed over the
owner's installed planner with the real installer, then rolled back with
the real rollback tool, with an independent record of the Mods folder.

| Stage | Planner | DLL |
| --- | --- | --- |
| Before | 0.1.1-rc3 | `78407dd4c724...` |
| Installed | 0.2.0-rc1 | `200924e76723...` (the candidate); every other mod unchanged |
| After rollback | 0.1.1-rc3 | `78407dd4c724...`; planner restored exactly; the whole Mods manifest equal to before (1117 entries) |

Evidence: `runtime-evidence\install-rc1-temp-deploy-20260923-01\`,
`...\rollback-rc1-temp-deploy-20260923-01\`,
`...\rc-deploy-rc1-temp-deploy-20260923-01\deploy-check.json`.

## Migration and persistence audit (mission section 11)

The first-open and save paths are the production ones: the session
constructor runs `CastingPlanMigrationService` against the mod folder's
`UserSettings`, and Save goes through `CastingPlanRepository`. The tests
below drive those classes against real files on disk; in game, the
first-open import, save, close/reopen and reload ran on the candidate.

| Required behavior | Covered by |
| --- | --- |
| Missing profile differs from unreadable or unsupported | `workspace-legacy-import-failures-block` |
| A failed import never creates a writable empty replacement | `workspace-legacy-import-failures-block`, `casting-a13-migration-boundary-is-recoverable` |
| Imported identities collision-safe and stable | `casting-import-identity-collision-safe`, `casting-import-is-idempotent-and-orderly` |
| Unknown grouping, pins, restrictions, requirements and policies stay unresolved intent | `casting-import-preserves-unresolved-intent`, `casting-import-automatic-becomes-review-drafts`, `casting-import-group-preserves-coverage-for-review`; in game `casting-ws-import-20260923-rc1-01` |
| Edits, Mark Ready, other Apply modes, reload or Undo cannot bypass review | `import-requirements-stay-enforced`, `material-plan-change-requires-renewed-review`, `review-acknowledgment-follows-production-orchestration` |
| Explicit resolution is disclosed, undoable and keeps history | `import-requirements-stay-enforced` (resolve, Undo, re-resolve, Mark Ready, Save and Reload) |
| Same-campaign reopen keeps the session; another campaign never reuses its storage | `casting-workspace-campaign-bound-session-reuse`; in game `casting-ws-reload-20260923-rc1-01` |
| A fresh-session disk read keeps the whole document | `casting-workspace-persistence-round-trip`, `casting-roundtrip-and-load-states-are-exact`; in game the reload's disk read-back |
| Newer schema or revision data is never overwritten by older code | `casting-roundtrip-and-load-states-are-exact`, `casting-a13-migration-boundary-is-recoverable`, `profile-malformed-json-recovers-default` |
| Install keeps settings; rollback keeps newer settings and quarantines unreadable plans | `Test-RestoreInstallLocal.ps1` (16 isolated cases); the temporary deployment above |

## What is not established

- **Manual acceptance:** the supervised session has only been rehearsed;
  the owner has not accepted the workspace's usability or look.
- **Finite resources in game:** prepared slots, spontaneous levels,
  ability pools, metamagic variants and rods have not been cast in game;
  the automation party has only cantrips. The `finite-direct-mixed`
  qualification is prepared and waits for an owner-designated
  `KBP_ADVANCED_SEED` (`docs/ADVANCED-SEED-COMPATIBILITY.md`).
- Not in game either: group spells, animated casting mode, area change or
  disable during a run, a failed casting halting the rest, the player's
  graceful stop (only the host's cancel), pets, and resolutions other than
  1920×1200.

## Environment notes for this evening

- The owner's Windows session was a disconnected RDP session for part of
  the evening; two workspace runs on development commits captured black
  display-path frames then (camera-path frames rendered). The candidate's
  runs above captured both paths.
- Another Claude session (the owner's KingmakerGunslinger project) also
  deployed to and launched the same Kingmaker installation. One of this
  lab's restorations (`casting-ws-import-20260923-i2-01`, a development
  commit) hit a transient "access denied", failed closed and was
  recovered by `Restore-Local.ps1` a minute later (verified). The two
  sessions now check each other's lock before any launch or Mods change,
  and restoration retries a brief sharing lock.
