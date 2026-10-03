# Windows Autonomous Runtime Testing

Status: IN PROGRESS

Runtime testing is an explicit, source-controlled request/result protocol plus a transactional external orchestrator. Ordinary game launches cannot activate it.

The planned activation flag is exact and case-sensitive:

```text
-kbpRuntimeTestRequest "<absolute-request-json-path>"
```

The path must be a strict descendant of the project runtime-evidence root. The JSON schema rejects duplicate or unknown members, traversal, reused run IDs, unknown scenarios/parameters, wrong version/commit expectations, non-allowlisted saves, and non-project evidence paths. The in-game runner activates only after normal UMM load on a later Unity frame and writes atomic structured evidence, with the final result written last.

The PowerShell orchestrator must prove all of the following before live mutation:

- clean expected branch/commit and validated local package;
- source-only tests and `-WhatIf` purity;
- exact game/UMM/Harmony identities;
- no running Kingmaker and no unresolved project transaction;
- safe current-user Steam launch through App ID 640820, never direct executable launch;
- Steam/account/cloud/update preflight with fail-closed behavior;
- exact live `Mods` manifest captured before staging;
- a transaction-owned lock, state file, backup, staged sentinel, and hashes;
- restoration in `finally`, including byte-for-byte manifest verification.

Unexpected dialogs, account state, cloud conflict, updates, credentials, purchases, unknown saves, ambiguous ownership, or failed restoration stop the harness.

Protected saves: every save file is hashed once a live run holds this lab's deployment lock and compared before that lock is released (the other lab checks the lock, so it cannot have run in between). A comparison the launcher cannot make stays pending beside the run's transaction, keeps the lock (the Mods folder is not restored yet) and refuses later runs and fixture changes until `scripts\Restore-Local.ps1 -RunId <run>` finishes it; `-SkipProtectedSaveComparison` is refused while it is pending, and a comparison that still cannot be made leaves the Mods folder unrestored until `Restore-Local.ps1 -RunId <run> -CloseUnverifiableComparison` records it as unverifiable for the owner's review and restores. A pending comparison whose lock was already released, or is no longer this run's, is recorded as unverifiable instead of compared. A blocking change (a changed or removed ordinary save, or a new save where the scenario writes none) is reported with every other failure of the run, recorded under `runtime-state\protected-save-violations`, and refuses every later run and fixture change until the owner has looked at the save folder and run `scripts\Confirm-KbpProtectedSaveReview.ps1 -RunId <run> -ReviewedBy <name> -Note <what was reviewed>`, typing the run id when asked; the acknowledgement (in `protected-save-violations\acknowledged`) names that exact record. Automation never runs that script. A scenario without a fixture save compares no saves and records them as not known. A run whose completion record is incomplete fails even when no single step did.

Deadlines: a live run ends itself at its `-TimeoutSeconds`, counted from the game's start, less a margin of a tenth (10 to 45 seconds), taking no further cast; the host checks this on every update in every scenario and logs the deadline once. At its own deadline the launcher writes `abort.json` into the run's evidence folder, which the game obeys at once, and waits up to 120 seconds for the result before reporting the run as abandoned (a PASS published after the abort marker is a failure) (its restoration then waits for `Restore-Local.ps1 -RunId <run>`).

A fixture bootstrap or teardown in progress (its `fixture.lock`) refuses a runtime entry, and the fixture script re-checks the deployment lock and the other lab's lease right before it touches the save folder. It sends no keyboard/mouse input and never force-terminates a process without separate explicit authority.

The Steam preflight requires exactly one already-running client at the exact expected path, a current-session logoff occurring no earlier than the last login, an App 640820 `Sync Disabled`/`offlineMode=true` record after the last successful transfer, and the exact fully-installed app manifest/build. Raw account IDs, tokens, and log lines are not copied into evidence. The state is re-evaluated before every launch.

`final-no-save-core` composes exact identity, the full structural catalog, and Harmony owner/order inventory in one fresh process. Campaign UI/input qualification is intentionally separate in `ui-root-smoke`, which requires a real campaign `StaticCanvas` and returns `BLOCKED` at the main menu. Neither NO-SAVE scenario substitutes for save-backed resource/execution proof. A caller may supply a unique safe `-RunId`; evidence and transaction IDs are immutable and reuse is rejected.

Statuses are `PASS`, `FAIL`, or `BLOCKED`, with assertion IDs and exact package/DLL/MVID/commit/platform identities. Compilation or main-menu load is never substituted for a gameplay assertion.
