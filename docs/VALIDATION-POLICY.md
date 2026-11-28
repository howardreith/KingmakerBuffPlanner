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

Ownership is the record's lab and token plus its registered writer process.
Only the process that wrote or last recovered the record acts under it; any
other process of this lab takes it over only by recovering the token its
journal stored, under the mutex, once that writer has exited (a live writer is
refused byte-for-byte). A missing or replaced journaled record is an incident:
the first one is kept as `reservation-incident.json` beside the run (written
once, with the progress made), nothing further changes, and the new record is
left alone. Ownership is checked again before every shared write and after
every blocking wait or bounded retry. A release whose record is missing or
replaced fails durably; only a release the journal records
(`reservationReleasedAtUtc`) counts as done.

Participating entrypoints:

- `Invoke-KingmakerRuntimeTest.ps1` (the public launcher): after its WhatIf
  decision and before its first write, it reserves, then takes its admission
  inputs (disposable save pair, installed loader, session display) again under
  the reservation and refuses (releasing, nothing written) if they changed. It
  passes the reservation through `Deploy-Local.ps1 -ReservationToken`, takes
  every scenario restoration baseline (protected saves, Heroism working backup,
  physical display, game registry) inside the entry after its lock and before
  anything is staged or activated (`-CaptureBaselines`), keeps the reservation
  through `Restore-Local.ps1 -RetainReservation`, and releases it only after the
  completion record's last process observation, once every obligation of the run
  is settled.
- `Enter-KbpRuntimeTransaction` (through `Deploy-Local.ps1` standalone and
  `compatibility\Enter-KingmakerCompatibilityProfile.ps1`): reserves (or checks
  the launcher's reservation) before the unresolved-state scan, the lock and the
  live Mods read; journals the token as `reservationToken`; checks ownership
  before each live Mods rename. A clean refusal releases a reservation it took;
  a lock it took that it could not remove keeps the reservation.
- `Restore-KbpRuntimeTransaction` (through `Restore-Local.ps1`, the launcher's
  restoration, an entry's own rollback and
  `compatibility\Restore-KingmakerCompatibilityProfile.ps1`): validates the
  journal (schema, run, a status this lab writes), recovers writership,
  verifies the original backup against the recorded manifest before the live
  Mods folder moves, checks ownership at every move, and on a retry after an
  interrupted cleanup reconciles its own lock before releasing.
- `Restore-Local.ps1` and `RuntimeDisplayRecovery.ps1`: one recovery admission
  rule (valid journal; the journaled token recovered with its writer exited;
  or, for a transaction recorded before this lab took part, a free reservation
  journaled first) before any registry, display, save-evidence or Mods write;
  the per-run restore mutex serializes recovering processes. Display recovery
  runs its read-only checks first and admits right before its first write.
- `New-KbpAutomationFixture.ps1` bootstrap, teardown and `-Recover`: reserve
  before the fixture lock; under both, enumerate and validate the exact inputs
  and destinations again (a change since the preflight refuses before
  anything is written), take the protected baseline, recheck ownership before
  each publication or deletion, and finish the protected-save comparison
  before releasing. A protected-save change keeps a first incident record, the
  lock and the reservation for the owner. `-Recover` takes over only the
  bootstrap's journaled token (refused while that bootstrap still runs; a
  missing or replaced record is an incident), keeps the same token so an
  interrupted recovery is itself recoverable, and finishes a release
  interrupted after the lock was removed.

The release of a run requires affirmative, correctly typed evidence of every
existing obligation: the transaction Restored and verified with its own lock
gone; no unacknowledged protected-save violation; the protected-save
comparison made (`compared` is the JSON boolean `true`); the game registry
restored and the physical display verified (boolean `true`); and an existing
Heroism preparation journal `Prepared` or `RolledBack` (or `BackedUp` with the
WORKING save still at its original bytes). Otherwise the reservation stays,
and a later `Restore-Local.ps1 -RunId` releases it.

Adoption is limited to this lab's own record for the same state root and the
token a transaction stored durably when it reserved. Nothing else is adopted
automatically: an orphan record (an entry or teardown interrupted before its
token was stored), another lab's record, a live writer's record and a
malformed record are refused and left byte-identical for the owner to inspect
and remove. Records are parsed strictly: valid UTF-8 (an optional UTF-8 BOM
aside), every common protocol field present and typed, the protocol name
exact; this lab's own records also need its 32-hex token and state root, while
a peer's token may be any non-empty value.

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
  protected-save violation in this lab's state (`protected-save-violations\acknowledged`);
  it changes no save, and the retained reservation of that run is released by
  the next `Restore-Local.ps1 -RunId`.
- Read-only scripts (`Collect-KbpLiveBootstrapDiagnostics.ps1`, inspectors)
  mutate no shared resource.

Offline tests never use the machine-wide reservation. The test seam
`KBP_TEST_RESERVATION_ROOT` names a private reservation folder inside this
checkout's `artifacts\runtime-harness-tests`, inherited by child processes;
private mode is refused unless every mutable target of the operation lies in a
harness test root too, so a real operation can never run under a test
reservation. `Test-RuntimeHarness.ps1`, `Test-DisplayModsRecovery.ps1` and
`Test-LoaderBaseline.ps1` run their transaction cases in such boundaries.

`scripts/Test-NativeReservation.ps1` is the focused gate. Separate processes
run the actual entry, restoration, public recovery and fixture scripts against
disposable stand-ins: both arrival orders and simultaneous arrivals; recovery
admission; strict parsing; lease loss during a blocked move and before release;
an interrupted cleanup; missing baselines; unfinished preparation; the
launcher's ownership interval; fixture recovery across interruptions, delayed
callers and protected-save drift. It then runs `Test-DisplayModsRecovery.ps1`
and `Test-RuntimeHarness.ps1` with commit-bound logs in `artifacts\review-logs`,
and observes the machine-wide record before and after. A required case that
cannot run (a real Kingmaker process is running, or the local package is not
built) makes the gate incomplete (exit 2), never a pass.
