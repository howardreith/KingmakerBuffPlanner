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

Unexpected dialogs, account state, cloud conflict, updates, credentials, purchases, unknown saves, ambiguous ownership, or failed restoration stop the harness. It sends no keyboard/mouse input and never force-terminates a process without separate explicit authority.

The Steam preflight requires exactly one already-running client at the exact expected path, a current-session logoff occurring no earlier than the last login, an App 640820 `Sync Disabled`/`offlineMode=true` record after the last successful transfer, and the exact fully-installed app manifest/build. Raw account IDs, tokens, and log lines are not copied into evidence. The state is re-evaluated before every launch.

Statuses are `PASS`, `FAIL`, or `BLOCKED`, with assertion IDs and exact package/DLL/MVID/commit/platform identities. Compilation or main-menu load is never substituted for a gameplay assertion.
