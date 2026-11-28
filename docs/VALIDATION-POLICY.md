# Validation policy

Owner directive of October 11, 2026. This document governs validation frequency
for development, review, packaging, runtime wrappers and release work. Earlier
mission/handoff chronology describes its original campaign; it cannot silently
select a broad campaign for an ordinary checkpoint.

1. Ordinary fixes end at a reviewable development checkpoint: inspect the diff,
   compile affected production code when needed, and test changed behavior and
   actual dependencies. State the focused pass's limits. Documentation changes
   need relevant text/link checks, without a DLL, package or acceptance ledger.
2. Review the coherent change before broad qualification. A review correction
   gets the failing reproduction and affected neighbors first. Batch related
   corrections before an expensive candidate build. Broaden only for an
   identified shared dependency, unresolved regression risk, or selected release
   or milestone qualification.
3. Full compatibility, historical replay, whole-roster persistence, live-lab
   purity and release reproducibility belong to a frozen candidate or justified
   broad task. They are not prerequisites for every edit, commit or review.
   Inspect nested child calls; a wrapper must not silently broaden the scope.
4. Build an unchanged candidate once and reuse its verified artifact. Distinguish
   product source, DLL/package hashes, harness/reader/fixture versions, environment
   and documentation provenance. Keep evidence bound to its original inputs;
   never label old PASS as execution on changed bytes. Documentation updates
   preserve a qualified artifact, using its frozen worktree when identity guards
   require it, rather than rebuilding to stamp a newer documentation HEAD.
5. A reader-only correction may re-evaluate immutable raw evidence only when all
   needed observations exist. Retain old and new verdicts. A changed observer or
   fixture needs its affected native scenario again. Observer/diagnostic code
   compiled into the product changes the binary identity.
6. Preserve per-transaction locks, current installation and save protection,
   package validation, controlled staging, rollback and exact restoration. Never
   kill an active transaction or omit cleanup for a time budget. Safety changes
   need focused positive and negative behavior checks before live use.
7. Ordinary validation has a 15-minute planning target and 30-minute ceiling,
   excluding necessary safe completion/restoration already in progress. If a
   selected check will exceed the ceiling, do not automatically launch broad work.
   Report useful evidence and the precise remaining check. Explicit release campaigns have
   separately declared budgets. These are workflow limits, not measured speeds.
8. Prefer behavior and stable contracts; reuse existing infrastructure. Add tests
   for concrete risks, not implementation text, cosmetic edits, arbitrary counts
   or prose wording unless wording is the contract. Reject zero-test selections.
   Reproduce a suspected baseline failure once under matched conditions, classify
   it accurately and retain it. Do not repeatedly rerun unrelated suites or
   convert known failures to PASS.
9. Use one supervised invocation with captured logs and a concise completion
   summary; preserve required child-process isolation and resumable receipts for
   unchanged inputs. Avoid repeated polling/restarts. Report tier/scope, changes,
   actual commands/durations, pass/fail counts, deferred release coverage and
   artifact identity where relevant. A focused pass is not full qualification.

## Selected entry points

The ordinary offline selector compiles production with Build.ps1, compiles the
current C# runner, sets KBP_TEST_GAME_PATH through the existing helper, runs
explicit name prefixes, rejects zero matches and restores its environment:

```powershell
# Local presentation/padding work: relevant view inspection remains separate.
.\scripts\Test-Development.ps1 -Only 'presentation-view-models'
# Catalogue ownership/removal: affected integration tests and relevant native case.
.\scripts\Test-Development.ps1 -Only 'optional-blueprint-ownership,variant-membership'
```

Choose prefixes for the actual change; full layout/resolution matrices belong to
substantial layout/input changes or final acceptance. The selector's optional
checks are existing bounded filesystem harness or evidence-reader tests. It
cannot select deployment, launcher live purity, restore-install or publisher.
`-Check RuntimeHarness` explicitly runs the existing bounded harness test and
requires its already built, matching local package; the selector does not
silently package a product to satisfy that prerequisite.
`-Check PhysicalSearchEvidence` runs the bounded physical-search record reader
against its positive and negative contract cases without launching Kingmaker.

For launcher arguments, `scripts/Test-RuntimeLauncherArguments.ps1` exercises
bounded direct invocation. A -File consumer repair also uses
`scripts/Test-RuntimeLauncherFileArguments.ps1`: the shared real child-process
refusal assertion runs before external imports for the native UI cases, without
starting the broad gate's preceding live-purity windows. Both are focused checks
and provide no live-purity or runtime qualification receipt.

Broad routes remain explicit:

```powershell
.\scripts\Test-SourceOnly.ps1
.\scripts\Test-RuntimeLauncherFileWhatIf.ps1
.\scripts\Test-RestoreInstallLocal.ps1
.\scripts\Test-PublishReleaseGate.ps1
```

Test-SourceOnly is an explicit broad diagnostic/qualification aggregate, not the
development default. Every real launch still follows the existing safety,
ownership, current Mods/save checks, lock, rollback and restoration path.
Dependency-bound live-purity reuse is not established by this patch; its guards
and whole-history audits remain intact on the explicit live route. Cheap refusal
checks run before its expensive purity windows.

Build.ps1 stamps product Git HEAD; existing runtime identity guards remain. Use
the frozen candidate/worktree and separate documentation provenance for later
reporting. The publisher still runs its own aggregate and release builds; this
patch neither supplies a publisher reuse receipt nor claims it consumes one.
Frozen 0.4.2 evidence remains attributed to its recorded bytes; no new game
coverage or 0.4.3 qualification is inferred from this workflow patch.

## Kingmaker native-test reservation

Every native-test transaction of this lab takes part in the machine-wide
Kingmaker native-test reservation (`docs/NATIVE-RESERVATION.md`, protocol 1,
lab identity `KingmakerBuffPlanner`), implemented here on its own in
`scripts/RuntimeHarness.Common.ps1`. It is a convention, not a dependency: no
other lab's repository, installation, folders or scripts are needed, and this
lab acquires and releases it the same way when installed alone.

Order: reservation, then this lab's own lock (`deployment.lock` or
`fixture.lock`); release in reverse, the reservation last and only after
verified restoration. The existing process, foreign-lease, lock, journal and
protected-save checks all remain.

Participating entrypoints:

- `Enter-KbpRuntimeTransaction` (through `Deploy-Local.ps1`,
  `Invoke-KingmakerRuntimeTest.ps1` and
  `compatibility\Enter-KingmakerCompatibilityProfile.ps1`): reserves before the
  unresolved-state scan, the lock and the live Mods read; stores the token as
  `reservationToken` in `transaction.json`; a refused or pre-state failed entry
  releases it. The reservation persists with the Active transaction across
  processes.
- `Restore-KbpRuntimeTransaction` (through `Restore-Local.ps1`, the launcher's
  restoration, and `compatibility\Restore-KingmakerCompatibilityProfile.ps1`):
  the record must still be this run's (a lost or replaced record refuses, leaves
  the record and the transaction unresolved, and the first loss is kept as
  `reservation-incident.json` beside the transaction); it proceeds only while
  the reservation's writer process (the entry, or the launcher that entered
  in-process) is itself or has exited, so two processes of this lab never
  interleave on one transaction; ownership is checked again after the blocking
  foreign-lease wait, before the first move. It releases after the verified
  restoration and lock release, and only when the run has nothing left to
  resolve (no unacknowledged protected-save violation, pending comparison,
  unrestored registry or unverified display snapshot); otherwise the
  reservation stays and a later `Restore-Local.ps1 -RunId` releases it. A failed
  restoration keeps it. A transaction recorded before this change reserves
  before anything moves, only when no record exists. `Restore-Local.ps1` checks the
  reservation before its registry, display and protected-save steps;
  `RuntimeDisplayRecovery.ps1` before and after its Mods/display recovery.
- `New-KbpAutomationFixture.ps1` bootstrap, teardown and `-Recover`: reserve
  before the fixture lock, take the protected-save baseline again under both,
  release after the lock; an interrupted bootstrap keeps the reservation, which
  only `-Recover` of that run adopts, by the token stored in its transaction
  record.

Adoption is limited to this lab's own record for the same state root and the
token a transaction stored durably when it reserved. Nothing else is adopted
automatically: an orphan record (an entry or teardown interrupted before its
token was stored), another lab's record, a live writer's record and a
malformed record (parsed strictly as UTF-8: exact protocol, lab, run id, 32-hex
lower-case token, positive writer process id, writer start time) are refused
and left byte-identical for the owner to inspect and remove.

Not participating, by design: `Install-Local.ps1` and `Restore-InstallLocal.ps1`
install or roll back a release in the owner's own game at the owner's request
(they are denied to automated agents); that is owner activity, not native-test
automation. Read-only scripts (`Collect-KbpLiveBootstrapDiagnostics.ps1`,
inspectors) mutate no shared resource.

Offline tests never use the machine-wide reservation: a harness test root
(`artifacts\runtime-harness-tests`) gets a private convention.
`scripts/Test-NativeReservation.ps1` proves the protocol with separate
contender processes running the actual entry and restoration against two
stand-in labs that share only a private reservation: both arrival orders with
the loser refused (no lock, no run folders, no Mods change) while the winner is
prepared, active (its process gone) and restoring; simultaneous arrivals from
a shared start barrier; recovery admission and malformed records; a second
process refused while the writer lives; a lost reservation and its first
incident; a reservation kept over an unacknowledged save violation; refused
and failed entries releasing it; a lab alone; and the fixture script. It also
runs the affected `Test-DisplayModsRecovery.ps1`.
