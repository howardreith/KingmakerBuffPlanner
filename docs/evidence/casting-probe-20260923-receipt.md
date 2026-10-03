# One-cast Resistance probe on the current candidate — receipt (2026-09-23)

Two identified runs of the reviewed one-cast probe boundary on the WORKING
automation fixture, `full-user` profile with the owner-approved exact copy of
the sealed KingmakerGunslinger 0.0.133 (the installed 0.0.136 was recorded
before each run and restored byte-exact after it). Each run allowed exactly
one native invocation, never retried, and wrote no save. The allowances were
written by Claude under the owner's delegated mission authority.

## Run 1: `casting-probe-cast-20260923-p1-01` — FAIL (world held)

| Item | Value |
| --- | --- |
| Candidate | commit `7664f2fd680b043cc90ce25463f9836959be1438`, package `fe19afd841de7b131480e7b00bf31534fedad1fb1e3412dca3ab5cf1613bc179`, DLL `a248e5f4d292875c6dd134cce9f5d28929bc4fc6c100d1b5626694ba2b3f993a`, MVID `35237c7a-0c63-488a-b44f-688af3c5e49d` |
| Selection | `casting-probe-select-20260923-p1-01`: Resistance, caster `2b56df7d-636e-4993-af38-5d54c6217e74`, recipient `050aa19a-1cf1-40f3-b28e-59d8c2fbfebf`, projection `ee8e76b211eb8aa6deb213a5f9fd7699eba5061e67ffc00e245186068937cb89` (identical to the historical `e964d2f` proposal) |
| Submission | Rule cast submitted inside the open casting workspace (FullScreenUi); rule success, free spend invoked |
| Outcome | `TimedOutUnconfirmed`: ResistanceBuff absent through the confirmation window and in the fresh after-read; resources free-unchanged |
| Cleanup and restoration | Workspace closed, lease released, boundary disposed; Mods restored and verified; protected saves unchanged |
| Cause | The workspace input lease holds the game in `FullScreenUi`, where queued ability execution does not advance. Fixed in `320a1b6` |

## Run 2: `casting-probe-cast-20260923-p2-02` — PASS

| Item | Value |
| --- | --- |
| Candidate | commit `320a1b62b01fb27749e378cf6e87985a0a36e7fe`, package `5d948fd7023c54d09378e3bf13d32d44be322e4e62bacf044fbae5551e373ad7`, DLL `ef73954016f672eca1b5f566a3e2cb848b3586ea196c84e97db025586c79c393`, MVID `974c793f-8a5c-4af3-8c1b-eecd00e582e5` (measured in game) |
| Selection | `casting-probe-select-20260923-p2-01`: the same Resistance source, caster, recipient and projection |
| World at submit | `workspaceClosed=True;leaseReleased=True;mode=Default;paused=False` |
| Outcome | `EffectConfirmed`: a new ResistanceBuff (`df680f6687f935e408eba6fb5124930e`) instance on the recipient in the fresh after-read; resources free-unchanged; zero violations |
| Cleanup and restoration | Workspace closed, lease released, boundary disposed; Mods restored and verified, including the installed Gunslinger; protected saves unchanged |

The run id `casting-probe-cast-20260923-p2-01` is void: its allowance was
written bound to the P1 candidate by a script path error, was never used,
and is left unmodified with a VOID notice beside it.

Evidence (local, not published): `runtime-evidence\<run id>\` (selection,
outcome, protected saves, orchestration, frames) and
`runtime-state\transactions\<run id>\transaction.json` (Mods transaction,
installed-dependency identity before and after). Frozen artifacts:
`runtime-backups\probe-frozen\<commit>\`.
