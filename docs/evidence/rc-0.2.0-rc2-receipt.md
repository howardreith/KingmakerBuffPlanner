# Release candidate 0.2.0-rc2 — receipt (2026-09-23)

The exact candidate for the owner's final review. Local only: nothing is
published, tagged or permanently installed. The pull request stays a
draft. It replaces 0.2.0-rc1 (`f8562a6`), whose animated casting of
spontaneous casters' cantrips failed in game (see "What changed"); the
rc1 receipt stays as history.

## Identity

| Item | Value |
| --- | --- |
| Commit | `ae0181d7f7716ab9fc33fce9494e06ea012cb144` (code as at `70f135a`; the later commit adds the version and docs) |
| Version | 0.2.0-rc2 (assembly 0.2.0.0) |
| Package | `3538273142441caf636bde814e100ec18ec4ef830f323ad50602ae3d00f08368` (the release ZIP and the harness package are byte-identical; two deterministic builds) |
| DLL SHA-256 | `5ec74d2f056b173bc25de0050d5e8281b1589c7012420be038d023fcaf2e62a1` |
| MVID | `5d780e27-580d-4ef4-9ab1-b0efd79eaa9e` |
| Frozen copy | `C:\Dev\KingmakerBuffPlannerLab\runtime-backups\rc-frozen\ae0181d7f7716ab9fc33fce9494e06ea012cb144\` (read-only, `FREEZE.json`) |
| Clean checkout | `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC2` (detached at the commit) |
| Release artifact | `...\KingmakerBuffPlanner-RC2\artifacts\release\0.2.0-rc2\` (`KingmakerBuffPlanner-0.2.0-rc2.zip`, `release-manifest.json`, `RELEASE-NOTES-DRAFT.md`) |

Builds are deterministic within one checkout, but not across checkouts:
the build path is embedded, so the gate checkout's build of the same
commit has another DLL hash and MVID. The candidate is the RC2
checkout's build, and every run below confirmed that build's MVID in
game.

## What changed since rc1

- **Cantrips at will (defect fix).** Kingmaker casts a cantrip at will
  through the ability its class grants for it. That ability has no
  spellbook, the game judges it available, and its count is unlimited
  (-1). A spellbook's own level-0 entry needs a level-0 slot, and these
  spellbooks have 0 per day. rc1 submitted the spellbook entry: in
  animated mode the game's cast command failed it, and instant mode cast
  it only because the cast rule skips the availability check.
  - A level-0 entry now executes through the caster's at-will cantrip
    ability, in both modes.
  - Validation is the cast command's own `IsAvailable`.
  - Discovery prices level 0 as free only with that ability behind it.
- **Qualification.**
  - It runs on the planner's own host, with its per-frame pump.
  - The stop is the player's routine press while the first cast is in
    progress.
  - A disable step runs the planner's own disable and enable during a run.
  - Allowance schema 4 binds one casting mode.

## Source tests at the candidate

Full gate at `ae0181d` (Windows PowerShell 5.1, non-interactive, clean
tree): source validation 42/42, protocol tests 300/300, runtime-harness
filesystem 35/35, package validation 4/4, deployment WhatIf 5/5, launcher
WhatIf 12/12, fixture inventory 3/3, Restore-InstallLocal 16/16, guarded
publisher 3/3. Every guard added since rc1 has a mutant the tests catch:
- 21 for the qualification host, the stop and the casting mode;
- 2 for the protocol mode rule;
- 16 for the disable step and the cantrip fix.

## Development runs that found and confirmed the fix

| Run | Build | Result |
| --- | --- | --- |
| `casting-qual-select-20260923-a1-01` | `9f2bdf6` | PASS (85): the selection, mode-independent (same projection ids as rc1) |
| `casting-qual-cast-20260923-a1-anim-01` | `9f2bdf6` | FAIL, restored, saves clean. The player's stop landed in flight. The game's cast command failed the first cantrip (`original-command-result:Fail`), and the routine halted: casting Failed, nothing spent, the other two not attempted. This is how the defect was found. |
| `casting-qual-select-20260923-d1-01` | `46eaabf` | PASS (85). `cantrip-diagnostics.json`: bard and sorcerer spellbooks with level-0 spells per day 0 and level-0 slots 0; each spellbook level-0 entry `available=False`, count 0; each class cantrip ability `available=True`, count -1, no spellbook |
| `casting-qual-select-20260923-a2-01` | `70f135a` | PASS (85): four forecast steps (the disable step's projection equals the recast's) |
| `casting-qual-cast-20260923-a2-anim-01` | `70f135a` | PASS (85), animated: every step as forecast, disable in flight, resources -1 → -1, host runs 0 → 4, zero violations |
| `casting-qual-cast-20260923-a2-inst-01` | `70f135a` | PASS (85), instant: every step as forecast, disable before the first step, zero violations |

## Native gameplay on the candidate

Guarded runs of the frozen build on the disposable automation campaign
(WORKING save; never an ordinary save), `full-user` profile with the
owner-approved exact copy of the sealed KingmakerGunslinger 0.0.133 and
isolated test-mod settings. Every run confirmed in game that it loaded
commit `ae0181d` and MVID `5d780e27...`. Each run restored the owner's Mods
folder byte-exact (verified), left every save unchanged, and wrote a
complete `run-completion.json`. The chain ran each run once and would
have stopped at the first one not PASS and complete.

| Run | Assertions | What it showed |
| --- | --- | --- |
| `casting-qual-select-20260923-rc2-01` | 85 PASS | Recipe `zero-cost-mixed`: Resistance by a bard and a sorcerer on the other party members; four forecast projections (stop, complete, recast, and the disable step equal to the recast) |
| `casting-qual-cast-20260923-rc2-anim-01` | 85 PASS | **Animated**, through the production Apply, the planner's own host and per-frame pump, and the real animated executor. **Stop:** the player's routine press landed while the first cast was in progress; that cast finished with a new effect and nothing after it started. **Complete:** the active buff was skipped, and two castings were confirmed through the game's own cast commands. **Repeat:** nothing to cast. **Recast:** confirmed after a close and reopen. **Disable:** the planner's own disable landed while the cast was in progress; it was interrupted, nothing landed, and the host accepted runs again once enabled. Resources -1 → -1 (at will); host runs 0 → 4, exactly the boundary's submissions; zero violations |
| `casting-qual-cast-20260923-rc2-inst-01` | 85 PASS | **Instant**: the same steps. The disable landed before the run's first step, so nothing was submitted. Zero violations |
| `casting-ws-import-20260923-rc2-01` | 84 PASS | First open with a classic plan: imported by the production migration as two drafts needing review; the classic file byte-unchanged and archived byte-exact |
| `casting-ws-reload-20260923-rc2-01` | 92 PASS | Authoring, Save, close and reopen, then the exact WORKING save loaded again in game under the read-only saver and the write sentinels: no write; the reopened planner showed the saved plan (also read back from disk), one subscription, one HUD root, no run |
| `casting-ws-manual-20260923-rc2-rehearsal` | 88 PASS | Rehearsal of the supervised manual session (not acceptance): manual-ready with no synthetic input, done marker written automatically, final capture, camera restored, planner closed |

The two casting allowances (`approvals\casting-qual-cast-20260923-rc2-anim-01.json`,
`...-rc2-inst-01.json`, schema 4) were written exclusively by Claude
under the owner's delegated mission authority. Each is bound to the
frozen commit, package, DLL and MVID, the fixture campaign, the recipe,
one casting mode, the four projection ids, and a budget of 8 submissions.

## Temporary guarded deployment (mission section 11)

`rc2-temp-deploy-20260923-01`: the release package was installed over the
owner's installed planner with the real installer, then rolled back with
the real rollback tool, with an independent record of the Mods folder.

| Stage | Planner | DLL |
| --- | --- | --- |
| Before | 0.1.1-rc3 | `78407dd4c724...` |
| Installed | 0.2.0-rc2 | `5ec74d2f056b...` (the candidate); every other mod unchanged |
| After rollback | 0.1.1-rc3 | `78407dd4c724...`; planner restored exactly; the whole Mods manifest equal to before (1117 entries) |

Evidence: `runtime-evidence\install-rc2-temp-deploy-20260923-01\`,
`...\rollback-rc2-temp-deploy-20260923-01\`,
`...\rc-deploy-rc2-temp-deploy-20260923-01\deploy-check.json`.

## Migration and persistence audit

Unchanged since rc1 (`docs/evidence/rc-0.2.0-rc1-receipt.md`, same
table): the first-open import and the save path are the production ones,
covered by the same tests; in game the import, save, close and reopen and
the reload ran again on this candidate.

## What is not established

- **Manual acceptance:** the supervised session has only been rehearsed;
  the owner has not accepted the workspace's usability or look.
- **Finite resources in game:** prepared slots, spontaneous levels,
  ability pools, metamagic variants and rods have not been cast in game.
  The automation party casts only cantrips (at will). The
  `finite-direct-mixed` qualification waits for an owner-designated
  `KBP_ADVANCED_SEED` (`docs/ADVANCED-SEED-COMPATIBILITY.md`).
- Not in game either:
  - group spells;
  - an area change during a run;
  - pets;
  - resolutions other than 1920×1200.

  The classic planner's animated cantrips use the same fixed code but
  were not checked separately. A failed cast halting the rest was seen
  once, before the fix (`casting-qual-cast-20260923-a1-anim-01`).
