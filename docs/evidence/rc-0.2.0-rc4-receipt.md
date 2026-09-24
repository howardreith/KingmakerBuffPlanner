# Release candidate 0.2.0-rc4 — receipt (2026-09-24)

The exact candidate for the owner's final review. Local only: nothing is
published, tagged or permanently installed, and the pull request stays a
draft. It supersedes 0.2.0-rc3 (`ea2a027`, superseded after its final
review); the rc3, rc2 and rc1 receipts and frozen packages stay unchanged
as history.

## Identity

| Item | Value |
| --- | --- |
| Commit | `863a182047950a75d7aafc548aa7b52a54c71b99` |
| Version | 0.2.0-rc4 (assembly 0.2.0.0) |
| Package | `9093cfd0b2c82b4b579a0d4d9fdb11a117cba5d0c789d72f636759048a67fab9` (the release ZIP and the harness package are byte-identical) |
| DLL SHA-256 | `d7081c0dc8002f2934dcab81f3b1a6b6de024e668ff53c1eb0ead099133dd744` |
| MVID | `61b9aaa5-786d-4e18-b929-a6a6caa5b443` |
| Frozen copy | `C:\Dev\KingmakerBuffPlannerLab\runtime-backups\rc-frozen\863a182047950a75d7aafc548aa7b52a54c71b99\` (read-only, `FREEZE.json`, frozen 2026-09-24T09:09:44Z) |
| Clean checkout | `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC4` (detached at the commit) |
| Release artifact | `...\KingmakerBuffPlanner-RC4\artifacts\release\0.2.0-rc4\` (`KingmakerBuffPlanner-0.2.0-rc4.zip`, `release-manifest.json`, `RELEASE-NOTES-DRAFT.md`) |

Reproducibility: two deterministic Release builds in `KingmakerBuffPlanner-RC4`,
one build in `KingmakerBuffPlanner-RC4-repro` and the gate build in
`KingmakerBuffPlanner-G2` (three differently named checkouts) produced the
same package, DLL and MVID.

Later commits on the branch after `863a182` are receipts and handoff
documents only; they change no source, script, test or version file.

## What changed since rc3

rc3's final independent review (three read-only reviewers: A domain,
planning, execution and game adapters; B UI, persistence, Main and
lifecycle; C harness, installer and runtime testing) found real defects,
so rc3 was superseded. A second round of three independent reviews of
those fixes (R-A casting and the Classic path, R-B UI and persistence,
R-C harness) found more; both rounds are listed finding by finding, with
commits, in `docs/CASTING-FIRST-REVIEW-INDEX.md`. In short:

- **Classic (the default mode).** A Classic routine advances only while
  the game runs; APPLY from the open Classic screen runs its checks at
  the press (a refusal is shown there), closes the screen and casts; the
  result is shown when the screen opens again (A1, R-A1).
- **Confirmation.** A cast is confirmed only by an unsuppressed instance
  of an expected effect and kind that this attempt put there (new, or the
  same instance with a later end), read before and after submission; an
  empty recipient set or a missing read confirms nothing (A2, A3, R-A3).
- **Planning.** Group castings with no legal origin or no predicted
  recipient are blocked with reasons; a party buff that also reaches pets
  is planned as a party buff; the budget records a casting's demand and
  reservation (A2, A4, R-A4, R-A5).
- **Import.** Classic plans saved by 0.0.19 (schema 4) import through the
  Classic loader, from the Classic planner's own in-memory copy of the
  same campaign (B1, R-B11).
- **Editing one casting.** Caster and exact source (named by spellbook,
  spell level and slot kind), routine and order, recast policy, single or
  group, enhancements that follow the caster (named when dropped), the
  out-of-combat rule, review items in words; a source a casting cannot
  pin is refused with the reason (B2, B3, R-B2..R-B6, R-B12, R-B13).
- **Persistence.** Unreadable or newer files are announced (a missing
  file told apart from an unreadable one), never overwritten, and the
  remedy works in the session: move the file and its backups aside and
  press Reload (B4, R-B1, R-B9, R-B10).
- **Smaller fixes.** No Classic save while casting-first is active; the
  spellbook handoff; campaign-scoped HUD tooltips; casting ids never
  reissued after load, removal or reload; the one-pass footer counts real
  shortages (B5..B7, R-B7, R-B8).
- **Harness.** Protected saves read and compared while the run holds its
  lock, which stays until the comparison is made; save violations block
  every later run and fixture change until the owner's typed, record-bound
  acknowledgement; fixture and runtime entries re-check each other's lock
  under their own; the stop rule on every frame; no incomplete run exits
  as a success (C1..C8, R-C1..R-C11).

Known limitations carried into rc4 (release notes, "Known limitations"):
a cast over an instance the game keeps is reported unconfirmed and stops
the routine (the default skip-if-present avoids it); no Classic
press-again-to-stop; confirmation waits counted in frames; a spell known
at two levels of one spellbook cannot be pinned; an unfunded linked
opposition pair shows one requested slot.

## Source tests at the candidate

Full gate at `863a182` (Windows PowerShell 5.1, non-interactive, clean
tree): source validation 42/42, protocol 331/331, runtime harness 37/37,
package 4/4, deployment WhatIf 5/5, launcher WhatIf 12/12, fixture
inventory 3/3, Restore-InstallLocal 16/16, guarded publisher 3/3.

## Reviews and mutation

- rc3's final review (A domain/planning/execution/adapters, B UI/
  persistence/lifecycle, C harness/installer/runtime testing), then three
  further independent read-only review rounds of the fixes (a re-review,
  a focused review and a targeted review) and a last short review of the
  final batch; every finding and its disposition is in
  `docs/CASTING-FIRST-REVIEW-INDEX.md`. The last review found nothing of
  high severity.
- Mutation after the final review of rc3: 52 C# mutants (re-review 29,
  focused 15, targeted 6, last 2) and 41 PowerShell mutants (24, 10, 5,
  2), each killed by the check meant for it; one C# mutant was equivalent
  (its line was redundant and was removed) and three survivors were
  answered with new tests. Mutants show that the tests notice a broken
  guard; they are not evidence of behaviour in the game.

## Game runs on the frozen build

All on the Automation WORKING fixture (the disposable test campaign; party
Hedwirg, Linzi, Tartuccio), from the RC4 checkout, run one after another
by the evidence chain (no retry). Each run loaded the exact build it staged
(commit, package, DLL and MVID checked in the game), read the save folder
and kept its baseline once it held the deployment lock, compared every save
clean before releasing it, and restored the Mods folder byte-exact.

| Run | Result | What it showed |
| --- | --- | --- |
| `classic-select-20260924-rc4-inst-01` | PASS, complete | The Classic (default) planner's Long routine recorded with its digest; nothing cast |
| `classic-cast-20260924-rc4-inst-01` | PASS, complete | Instant: Resistance from Linzi's level-0 spellbook entry, resolved to the at-will cantrip ability (provenance recorded: carrier, pool, candidate count), available count -1 before and after; confirmed by a new effect instance under the new rule; finite pools unchanged (bard 2 to 2, sorcerer 5 to 5); the single-use grant consumed once and disarmed at the end |
| `classic-select-20260924-rc4-anim-01` | PASS, complete | The same plan in Animated mode |
| `classic-cast-20260924-rc4-anim-01` | PASS, complete | Animated: the same resolution and confirmation (queued and started; the animated executor records no "submitted"), pools unchanged, grant once |
| `casting-qual-select-20260924-rc4-01` | PASS, complete | Zero-cost recipe selected; 5 projections forecast; the allowance (5 ids, at most 8 submissions) written for each mode, bound to this build |
| `casting-qual-cast-20260924-rc4-anim-01` | PASS, complete | Every step as forecast: stop (the player's routine press, landing while the first cast was in flight), complete, repeat (refused `nothing-to-cast:3`), recast, disable (in flight; held 5 updates with nothing running or accepted), recover (a new run after enabling); 5 runs started and 5 reported; lifecycle probe unchanged; 8 of 8 submissions; zero violations |
| `casting-qual-cast-20260924-rc4-inst-01` | PASS, complete | The same in Instant mode; the disable landed before the step started and is labelled disable-before-start (no in-flight instant interruption is claimed) |
| `casting-ws-import-20260924-rc4-01` | PASS, complete | A genuine schema-4 (0.0.19) Classic file imported on first open: migrated, the Classic file unchanged and archived, 2 castings (one per target), none Ready (drafts awaiting review) |
| `casting-qual-select-20260924-rc4-finite-01` | Refused, as expected | `finite-direct-mixed`: `no-eligible-qualification-recipe` (this party's finite spell levels hold no buff); nothing cast; restored, saves clean |
| reload, layout at 1920x1200 and 1920x1080, manual-session rehearsal | Deferred | The owner's remote session was disconnected, so game frames render black; these runs were not started (a changed prerequisite, never a blind retry) |

## Install and rollback of the exact artifact

Temporary install `rc4-temp-deploy-20260924-01` of the frozen release
package through the real installer, then the real rollback (evidence
`runtime-evidence\install-rc4-temp-deploy-20260924-01`,
`rollback-rc4-temp-deploy-20260924-01` and
`rc-deploy-rc4-temp-deploy-20260924-01\deploy-check.json`):

- Local install PASS: 0.2.0-rc4, package `9093cfd0...`, DLL `d7081c0d...`;
  no other mod changed.
- Install rollback PASS: 0.1.1-rc3 restored exactly (DLL `78407dd4...`);
  the Mods folder equal to its state before the install (1117 entries).
- The normal installation was checked again afterwards: 0.1.1-rc3, DLL
  `78407dd4c7240ce25c7654715854e83061da744a18d8eb4a2198fdffec5e3080`.

## Artifact identity after merge

The tested candidate is the frozen package, not the source. A merge
creates a new commit, and the build embeds its commit, so a rebuild from
the merged branch produces a different DLL and MVID; that rebuild is not
the tested candidate even when the source tree is identical. Merging with
a merge commit (not a squash or rebase) keeps `863a182047950a75d7aafc548aa7b52a54c71b99` in `main`'s
history, so the frozen package's manifest keeps naming a reachable commit.

To install exactly what was tested, install the frozen release package by
its hash (no rebuild):

```powershell
# From the candidate's clean checkout (C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC4):
.\scripts\Install-Local.ps1 -ReleaseManifestPath .\artifacts\release\0.2.0-rc4\release-manifest.json `
    -InstallId <id> -ExpectedPriorVersion 0.1.1-rc3
```

Post-install check (it must print `True` twice):

```powershell
$dll = 'C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Mods\KingmakerBuffPlanner\KingmakerBuffPlanner.dll'
(Get-FileHash $dll -Algorithm SHA256).Hash.ToLowerInvariant() -eq 'd7081c0dc8002f2934dcab81f3b1a6b6de024e668ff53c1eb0ead099133dd744'
[Reflection.Assembly]::ReflectionOnlyLoadFrom($dll).ManifestModule.ModuleVersionId.ToString() -eq '61b9aaa5-786d-4e18-b929-a6a6caa5b443'
```

Rollback, keeping settings edited since:

```powershell
.\scripts\Restore-InstallLocal.ps1 -InstallId <id>
```

If a build from a merged commit is to be released instead, it is a new
candidate:
1. Confirm the source is unchanged: `git diff 863a182047950a75d7aafc548aa7b52a54c71b99 <merged> -- src
   scripts tests Version.props` must be empty.
2. Build it with `Build-Release`.
3. Freeze it.
4. Repeat the candidate's native set on that build: Classic select and
   cast in both modes, the casting-first selection and both casting
   modes, the import, and the temporary install and rollback (plus the
   frame-judged runs once a connected session is available).

A matching source diff alone does not make the new bytes tested.

## What is not yet shown

- **Finite resources, group buffs, metamagic and rods, pets**: implemented
  and source-tested; not proven in the game. The automation party has no
  buff in its finite spell levels, no group buff and no pet; the finite and
  group qualification waits for an owner-designated `KBP_ADVANCED_SEED`.
- **Frame-judged runs** (deferred): the in-game reload, the workspace
  layout with the new controls at 1920x1200 and 1920x1080, the manual
  session rehearsal, and physical keyboard and mouse input. They need the
  owner's session connected; the commands are in
  `docs/MANUAL-USABILITY-HANDOFF.md`. 2560x1440 is larger than this display
  and is refused without changing any setting.
  With the session connected, the deferred runs are, from the RC4
  checkout (each restores the Mods folder and compares the saves itself):

  ```powershell
  .\scripts\Invoke-KingmakerRuntimeTest.ps1 -Scenario live-workspace-reload -CompatibilityProfileId full-user -TimeoutSeconds 900 -RunId casting-ws-reload-20260924-rc4-01 -Confirm:$false
  .\scripts\Invoke-KingmakerRuntimeTest.ps1 -Scenario live-workspace-qual -CompatibilityProfileId full-user -TimeoutSeconds 900 -RunId casting-ws-qual-20260924-rc4-1200-01 -Confirm:$false
  .\scripts\Invoke-KingmakerRuntimeTest.ps1 -Scenario live-workspace-qual -CompatibilityProfileId full-user -DisplayMode windowed-1920x1080 -TimeoutSeconds 900 -RunId casting-ws-qual-20260924-rc4-1080-01 -Confirm:$false
  .\scripts\Invoke-KingmakerRuntimeTest.ps1 -Scenario live-workspace-manual -CompatibilityProfileId full-user -ManualHoldSeconds 300 -ManualRehearseDone -TimeoutSeconds 900 -RunId casting-ws-manual-20260924-rc4-rehearsal -Confirm:$false
  .\scripts\Invoke-KingmakerRuntimeTest.ps1 -Scenario live-workspace-physical -CompatibilityProfileId full-user -TimeoutSeconds 900 -RunId ws-physical-20260924-rc4-01 -Confirm:$false
  ```
- **The owner's usability verdict** (the supervised manual session),
  including the Classic planner closing itself on APPLY - a change to the
  default planner that is the owner's to accept.
- **An area change during a run**: handled in code, not exercised live (the
  test campaign's only exit autosaves).
- The known limitations in the release notes.
