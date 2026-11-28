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

Every native-test tool of this lab takes part in the machine-wide Kingmaker
native-test reservation (`docs/NATIVE-RESERVATION.md`, protocol 1, lab
identity `KingmakerBuffPlanner`), implemented here on its own in
`scripts/RuntimeHarness.Common.ps1`. It is a convention, not a dependency: no
other lab's repository, installation, folders or scripts are needed, and this
lab acquires and releases it the same way when installed alone.

### The rule (deliberately small; it fails closed)

1. A participating tool acquires the reservation as its first action after
   argument and WhatIf handling, before any mutable read (saves, installation,
   display, Steam, run-ID availability, this lab's state), and before this
   lab's own lock. WhatIf never reserves.
2. It holds the reservation for its whole process and checks it again before
   each shared change: before every attempt of a live Mods rename, before the
   game is launched, before cleanup, before fixture publication, deletion and
   rollback. A lost, replaced or unreadable record stops the tool there; the
   new record is left alone and nothing more is changed.
3. It releases the reservation last - after its lock - and only after its own
   existing verification succeeded (restoration verified, protected saves
   compared clean, the run's own lock gone). A refusal or failure before any
   shared change releases it. Anything unresolved after a shared change keeps
   it, for the owner.
4. Nothing is adopted, resumed or reconciled automatically. A record is a
   reservation whoever wrote it and whether or not its writer still runs.

Ownership binds lab, token, run and state root: a tool borrows the
reservation only for exactly the run, token and state root its record names,
and a transaction continues only while its journal names that reservation.

### Participating tools

- `Invoke-KingmakerRuntimeTest.ps1` (single process): reserves first; the
  in-process `Deploy-Local.ps1` and `Restore-Local.ps1` borrow it for the run
  (`-ReservationToken`); releases after its final process observation and
  before the completion record, which carries a release failure.
- `Deploy-Local.ps1` standalone (and `compatibility\Enter-KingmakerCompatibilityProfile.ps1`):
  reserves first; the entry journals the token (`reservationToken` in
  `transaction.json`) and the reservation stays with the active transaction;
  it is released if the deployment was refused or rolled back verified.
- `Restore-Local.ps1` (and `compatibility\Restore-KingmakerCompatibilityProfile.ps1`,
  display recovery): validates the journal (schema, run, a phase this lab
  writes) before anything, then continues only the journaled reservation
  (lab, token, run, state root), or takes a free one for a finished run or a
  transaction recorded before this lab took part; the per-run restore mutex
  (also held by the entry while it runs) keeps two processes off one run. It
  releases after the verified recovery left the run settled. Restoration
  verifies the original Mods tree against its recorded manifest at its current
  location before every move that would install it.
- `New-KbpAutomationFixture.ps1` bootstrap, teardown and `-Recover` (single
  process): reserve first (before reading the save folder), release after the
  script's own success verification with its fixture lock gone, or when it
  stopped before changing the save folder.

Not participating, with their data boundaries:

- `Install-Local.ps1` and `Restore-InstallLocal.ps1` install or roll back a
  release in the owner's own game at the owner's request (they are denied to
  automated agents): owner activity, not native-test automation.
- `compatibility\Copy-KbpSealedCompatibilityFixture.ps1` copies verified bytes
  from this lab's `runtime-backups` into this lab's sealed fixture folder
  (`examples`); it never writes the live installation or a save.
- `New-KbpRunAllowance.ps1` writes owner-authorized allowance records into this
  lab's private `approvals` folder only.
- `Confirm-KbpProtectedSaveReview.ps1` records the owner's acknowledgement of a
  protected-save violation in this lab's state; it changes no save.
- Read-only scripts (`Collect-KbpLiveBootstrapDiagnostics.ps1`, inspectors).

### Owner procedure for a kept reservation

A kept reservation means a run of this lab stopped after a shared change, or
lost its reservation, or the record is unreadable. Every participating lab
refuses until it is resolved:

1. Read `%ProgramData%\KingmakerNativeTest\reservation.v1.json` (lab, runId,
   holderProcessId). If it names another lab, resolve it there.
2. Make sure no Kingmaker process and no process of that run is still running.
3. Inspect and settle the run with this lab's existing tools: `Restore-Local.ps1
   -RunId <run>` for a runtime transaction (it continues the journaled
   reservation and releases it after a verified recovery); for a fixture run,
   check the save folder, `runtime-fixture-state\<run>\transaction.json` and
   `fixture.lock`; review any protected-save violation
   (`Confirm-KbpProtectedSaveReview.ps1`).
4. Only when the shared state is verified as intended, delete the record by
   hand (then, for a fixture run, `New-KbpAutomationFixture.ps1 -Recover -RunId
   <run>` may roll back its own partial artifacts under a fresh reservation).

### Tests

Offline tests never use the machine-wide reservation. The test seam
`KBP_TEST_RESERVATION_ROOT` (never set by a production entrypoint) names a
private reservation folder inside this checkout's
`artifacts\runtime-harness-tests`; private mode is refused unless every
mutable target of the operation - including the journal's lock path - lies in
a harness test root, and a run that restores a game registry key is refused
in private mode. Tests set it and restore it in `finally`.
`Test-RuntimeHarness.ps1`, `Test-DisplayModsRecovery.ps1` and
`Test-LoaderBaseline.ps1` run their transaction cases in such boundaries.

`scripts/Test-NativeReservation.ps1` is the focused gate: separate processes
run the actual entry, restoration, `Restore-Local.ps1` and fixture scripts
against disposable stand-ins (arrival orders, simultaneous arrivals, parsing,
run binding, loss during a blocked move, backup verification, refusal versus
kept reservations, the per-run mutex, public recovery admission, the fixture
script, and the launcher's and deployment's order). It runs
`Test-DisplayModsRecovery.ps1`, `Test-RuntimeHarness.ps1` and the
loader-baseline transaction cases with their output in the log, and observes
the machine-wide record before and after. A required case that cannot run
makes the gate incomplete (exit 2), never a pass.
