# Casting-First Migration — GitHub Review Index

Branch: `codex/kingmaker-buff-planner-casting-first`
Reviewed baseline: `c182061354e9e761c09648ca779ab334588ba379`
(`codex/kingmaker-buff-planner-z-native-assignments`). The PR against
`main` additionally shows the earlier migration lineage
(`fd0e6dc..c182061`); this index covers the casting-first commits
`c182061..HEAD` (61 commits at first publication).

Status: **release candidate 0.2.0-rc2 frozen at `ae0181d` for the owner's
final review** (receipt `docs/evidence/rc-0.2.0-rc2-receipt.md`); 0.2.0-rc1
(`f8562a6`) has the animated cantrip defect described below and its
receipt stays as history. Not a fully gameplay-qualified release. Casting-first is an opt-in planner mode (UMM setting, Classic
by default). In ordinary play every routine route reaches the production
dispatch boundary and the execution host; in an automated test session
both player routes (casting-first and classic) refuse, and native casts
happen only through the owner-authorized harness boundaries (single-cast
probe, casting qualification). First native evidence: the one-cast
Resistance probe confirmed its effect in game on
`320a1b6` (`casting-probe-cast-20260923-p2-02`), and the zero-cost
qualification passed in game on `d35b38f`
(`casting-qual-cast-20260923-q1-01`: stop, complete, repeat and a recast
after a close and reopen, each exactly as forecast). Finite-resource
qualification waits for an owner-designated advanced seed. Human usability and the native aesthetic
pass remain open.

## Findings from live qualification after rc1 (2026-09-23)

The animated-mode qualification (the player default) found a defect in the
frozen rc1 that source tests could not see. It is fixed at `70f135a`.

| Finding | Disposition | Evidence |
| --- | --- | --- |
| In animated mode, the game's own cast command failed every spontaneous caster's cantrip: the planner submitted the spellbook's level-0 entry, and the game casts a cantrip at will only through the ability its class grants for it (no spellbook, count -1); the spellbook entry needs a level-0 slot and these books have 0 per day. Instant mode cast the entry only because the cast rule skips the availability check | A level-0 entry executes through the caster's at-will cantrip ability first (both modes), exactly as the game's action bar; validation is the cast command's own `IsAvailable`; discovery prices level 0 as free only with that ability behind it (otherwise a level-0 slot or a consumable prepared slot) | Failure `casting-qual-cast-20260923-a1-anim-01` (halted after the failed cast: nothing spent, nothing else started); diagnostics `casting-qual-select-20260923-d1-01`; the game's `UnitUseAbility.OnAction`, `AbilityData.IsAvailable`, `Spellbook.SpendInternal` read from its assembly |
| The qualification's stop was a host cancel between castings, not the player's stop | The stop is the player's routine press through the HUD's routine entry while the first cast is in progress (the host's `RequestStop`); the cast completes and nothing after it starts | Source and mutation tests; in game on the fixed build |
| The qualification used a private host its driver pumped | It runs on the planner's own host, pumped by the planner root once per frame while the world runs; the root's tick precedes the test host in each mod update, so the press lands before the next pump | Source checks; in game on the fixed build |
| No in-game interruption of a cast in progress | Disable step: the planner's own disable and enable during a run; animated: while the cast is in progress (interrupted, cleaned up, nothing lands); instant: before the first step (nothing submitted); the host accepts runs again once enabled | Tests with an interrupted cast that lands anyway and a host that never resumes; in game on the fixed build |
| An allowance did not state the casting mode | Schema 4 names instant or animated; the launcher, request protocol, host and boundary each refuse any other mode | Tests; 21 mutants killed |

## Previous review dispositions — independent review of `1332ed8..542cd66` (answered 2026-09-23)

A read-only review of the in-game reload, the grid order, the version bump
and the docs found no P0. Both P1s concern the reload's save safety; no
run wrote a save (the WORKING hash stayed the same across the runs), but
the guarantee rested on the wrapper's counters alone.

| Finding | Disposition |
| --- | --- |
| P1-1: the reload restored the writable native saver after the header commit but before the load finished, with no write sentinels | The read-only saver stays until the reload's after-load callback; the first load's write sentinels (SaveRoutine, SaveStashedArea, DeleteSave, RemoveSaveFromList) are installed for the reload window together with a correlation hook on Game.LoadGame; any write, a foreign or repeated load, or a missing correlation fails the run; the hooks are removed on success and failure; rejected header updates and commits are recorded before they throw |
| P1-2: the harness still allowed the WORKING save to change in the reload scenario | The reload and import scenarios allow no save change and block new save files; the reload needs `-TimeoutSeconds` of at least 600 and has its own live budget |
| P2-1: three reload checks could not detect their failures | The saved plan is read back from disk as a fresh session would and compared with what was saved; exactly one area unload and one loading-complete delivery are required; HUD roots are counted including inactive ones; session reuse is reported |
| P2-2: the highlighted first buff was not the draft's buff | The first buff is committed as the selection, and a draft authored without a click uses it (it would otherwise have been saved as "unsourced") |
| P2-3: one version label, several builds | The candidate is frozen at one commit; its receipt pins the identities and repeats the runs on it |
| P2-4: release-note evidence errors | Corrected (the failed s1-01 run, per-run build checks, which frames showed what) |
| P2-5: tests weaker than claimed | The grid test uses three buffs whose ids sort against their names and adds without a click; the reload source check pins the callback gating and the sentinels; the family refusal asserts its reason |
| P3 | Failure evidence is flushed by the host's timeout and every loader failure path; the reload runs only after a passing authoring and reopen; grid names sort culture-aware; the scale evidence reads the native root canvas with invariant formatting |

## Previous review dispositions — independent review of `f7726c9..1332ed8` (history)

A second read-only review of the fixes above found no P0, P1 or P2 issue.
All ten P3 items are addressed:

| Finding | Disposition |
| --- | --- |
| P3-A: a restoration failure in a failed run was only a console warning; the completion record ignored it | `run-completion.json` carries `restorationFailure` and is never verified while it is set; the text is saved as `restoration-failure.txt`; the launcher holds the run's own error until the records are written and folds the restoration failure into it (a restoration failure and a save violation are reported together) |
| P3-B: the harness source check could not catch a `throw` in the restoration branch | The check extracts the restoration branch (no `throw`, no `Write-Error`, both paths assign the failure) and pins the save policy feeding the comparison, the failure reaching the record and the held run error |
| P3-C: the header-count test did not prove routine-wide counting | The test adds the same buff to another routine: Long counts 2 while the buff shows 3 cards |
| P3-D: enhancement and material footer rows read "resource"; labels collided; a shortage could be truncated | Rows are named by kind (enhancement pools by owner and pool name, material components, ability pools by their source); same-looking finite pools are numbered; the footer lists shortages first and merges free pools |
| P3-E: some refusals lost their advice | `targeting-requires-direct-target` and `targeting-invalid` are mapped; shape refusals are keyed on the mod's own messages |
| P3-F: the editing label fell back to "Editing one casting" after a buff switch | The focused casting is found in the whole plan |
| P3-G: the probe's `Pump` defaulted to a running world | The world state is a required argument |
| P3-H: the probe's time budget had no launcher minimum | Probe scenarios need `-TimeoutSeconds` of at least 600 |
| P3-I: the matrix overstated tab clicks, the player stop and the production pump | Worded as callback coverage, the host's immediate cancel and "not in game" for the production pump |
| P3-J: `ValidateSet` binds case-insensitively; `live-cast-probe` had the loose save policy | The launcher continues with each value's canonical spelling; the probe allows no save change |

## Previous review dispositions — independent review of `e7c5207..f7726c9` (history)

A read-only independent review of the RC1-RC4 fixes, the world-running
gate and the external Gunslinger fixture found no P0 or P1 issue. Fixed in
`548a90d` (each with a caught mutant):

| Finding | Disposition |
| --- | --- |
| P2-1: the probe checked the world once, a frame before its rule fired, and pumped its confirmation frames without a check; the wait was bounded by 300 updates | Every probe pump (the fire and each confirmation frame) waits while the world is held; a stop and the wall-clock deadline still apply. The record carries the world state at the first step and the held pump count; the wait before submitting is bounded in elapsed time (`ProbeWorldWaitSeconds`) |
| P3-1: the production world clock truncated each frame to whole milliseconds | `CastingWorldClock` accumulates double seconds; unit-tested at 60 fps and above 1000 updates a second |
| P3-2: the qualification host deadline counted held time although this index said it did not | The qualification host uses the same world clock as production; the run's own 240-second deadline stays wall-clock as the hard bound. A driver test holds a step for 200 s: it completes under the world clock and fails under a wall clock |
| P3-3: a failed restoration or a still-running Kingmaker skipped the protected-save comparison and `run-completion.json` | Restoration failures are captured and no longer skip `run-completion.json` or, once the game has exited, the protected-save comparison. Since the follow-up review (P3-A) the record carries the failure (`restorationFailure`), it is saved as `restoration-failure.txt`, and the launcher folds it into the run's own error |
| P3-4: an inspection that changed the WORKING save was recorded complete but could never qualify | The casting selection and every advanced-copy run, the inspection included, allow no save change |
| P3-5: several tests were weaker than their commits claimed | The launcher computes its completion record and save policy through tested functions; the held-world driver tests cover the Begin and Wait gates separately; the source checks test the gate blocks themselves |

## Previous review dispositions — Pro review of `e7c5207` (history)

| Finding | Disposition |
| --- | --- |
| RC1: native slot ids (`level-N\|type-T\|index-I`) contain the "\|" the token evidence was joined and split on | Fixed in `2a443c3`: typed readings (casting, opaque token id, before, after) and a list of castings read on one side only; the judge reads them directly and text is presentation only. One `PreparedSlotIds.Format` serves the snapshot and the cast adapter, and the tests build native ids with it: linked pairs, wrong-token spend, partial consumption, missing readings |
| RC2: a failed before-read did not stop the step before Apply | Fixed in `2a443c3`: every casting the step observes needs a succeeded read of its own target with effect, availability and every reserved token; otherwise the run ends before Apply with zero submissions. Driver tests: observer exception, failed read, null read, wrong target, missing prepared tokens |
| RC3: an in-game PASS could authorize advanced casting before restoration and the protected-save check | Fixed in `1af543c`: the launcher writes `run-completion.json` last (game result, harness success, owned exit, verified restoration, protected saves compared and clean, fixture files and hashes, campaign, binding manifest, compatibility identity); only a complete record of the same pair and identity qualifies. `harness-error.txt` is written only for failed runs. The 900-second floor is documented as a time budget with truthful recovery |
| RC4: zero-cost selection banned both casters as recipients and so needed four units | Fixed in `1af543c`: recipients are chosen per casting (never the casting's own caster); a three-unit party qualifies, end to end. The record now carries the roster the selection saw |

**Live finding (not from the review).** The first one-cast probe on the
current candidate (`casting-probe-cast-20260923-p1-01`, frozen `7664f2f`)
submitted its rule while the casting workspace was open. The workspace
lease holds the game in `FullScreenUi`, where queued ability execution
does not advance, so the Resistance effect never appeared
(`TimedOutUnconfirmed`). One invocation, no retry; restoration and the
protected-save check passed. Fixed in `320a1b6`: the probe closes the
planner and submits only once the world runs (Default mode, not
paused), the production host and the qualification driver advance only
while the world runs, and the host deadlines count only running time
(the qualification host since `548a90d`).

## Previous review dispositions — review of `54d330b..47caeef` (history)

A second independent read-only review covered the qualification core,
driver and scenarios, the run request and the RC review fixes. Every
finding is fixed in `af5f0ca` with a regression test and a mutant the
tests catch. Full gate at `af5f0ca`: source 42/42, protocol 286/286,
harness 33/33, deploy WhatIf 5/5, launcher WhatIf 11/11, fixture 3/3,
Restore-InstallLocal 16/16, publisher 3/3. No qualification run has
happened, so no native cast was affected.

| Finding | Disposition |
| --- | --- |
| P0: the driver judged the steps only at the end, so after a failed, uncertain or cancelled step (an unconfirmed first cast, a spend on a free source) it kept submitting | Fixed: each step is judged by the record's step rule the moment it ends, and repeat continues only on the exact no-op; anything else ends the run before another submission. Tests drive an unconfirmed cast, a spend on a free source and a refused repeat: each run fires exactly the casts before the failure |
| P2: the harness could time out and abandon a live qualification run with the Mods folder unrestored | Fixed: the launcher refuses a qualification scenario with less than 900 seconds; the documented commands carry it |
| P3: an exhausted rod could hide an incompatible enhancement set | Fixed: compatibility counts exhausted required enhancements |
| P3: the selection-only assertion was vacuous | Fixed: it requires zero qualification runs, the locked player routes, zero production runs and a closed workspace |
| P3: the reopen check accepted any stored acceptance | Fixed: the reopened session must restore exactly the digest just accepted |
| P3: judge and evidence gaps (cleanup failures, other targets on recast, interrupted steps, shutdown) | Fixed: step rules check cleanup failures and the other targets; interrupted steps keep their report and reads; shutdown publishes the record |
| P3: the workspace close result was ignored | Fixed: an unclosed workspace or a held input lease fails the run before any authoring |
| P3: guide wording ("more than two rounds") | Fixed: "at least two rounds" |
| P3: the protected-save check tolerated a changed WORKING save for the casting run | Fixed: a casting qualification must leave every save unchanged and create none |

After the fixes, `finite-direct-mixed` (`ebf2329`) adds the
advanced-copy recipe for finite prepared slots, spontaneous levels and
metamagic variants, judged per step down to the exact prepared tokens.

## Previous review dispositions — RC review of `ca0d636..325e4b3` (history)

An independent read-only review of RC slice 1 found no P0. Every finding
is fixed in `47caeef` with a regression test; the eight new mutants and
two scenario-drift mutants were caught. Full gate at `47caeef`: source
42/42, protocol 285/285, harness 33/33, deploy WhatIf 5/5, launcher
WhatIf 11/11, fixture 3/3, Restore-InstallLocal 16/16, publisher 3/3.

| Finding | Disposition |
| --- | --- |
| P1: `live-advanced-inspect` could not build its request (the request builder repeated an older scenario list) | Fixed: the builder accepts the three new scenarios; the harness builds a request for every launcher scenario and requires identical sets |
| P2: an exhausted rod blocked (required) or re-shaped (optional) a casting whose effect was already active | Fixed: an exhausted required enhancement is a resource shortage that a sufficient live effect waives; the skip keeps the would-be cost shape, so the digest is unchanged |
| P2: opening the planner revoked a stored acceptance on a temporary difference, and saved that | Fixed: presenting never revokes (an acceptance only ever authorizes its exact digest); viewing writes no review state |
| P2: casting-first refusals were invisible in game | Fixed in RC slice 1 (`7e864e7`): refusals are kept per routine and shown on the HUD tooltip and planner footer |
| P2: nearly expired effects counted as sufficient when durations were not comparable | Fixed: under two rounds left is never sufficient, whatever the duration text |
| P3: the stop tooltip promised a graceful stop the host did not provide | Fixed: the player stop now lands after the cast in progress; area change, disable and teardown still stop at once |
| P3: lock comments overstated coverage (the classic path still cast in automation) | Fixed: the lock also refuses the classic routine execution; `live-ui-bootstrap` asserts that refusal |
| P3: the HUD tooltip called a stale stored digest "accepted"; execution settings outside the signature were undocumented | Fixed: the tooltip states current / on file / changed; the signature comment and player guide say why the Animated/Instant choice is not signed |
| P3: coverage gaps | Closed by the tests listed above |

## Previous review dispositions — O1 at `e964d2f` (history)

The frozen probe checkout and artifact (`e964d2f`,
`runtime-backups/probe-frozen/e964d2f…/`) are unchanged. Development now
continues in a separate worktree on the same PR branch; the original
checkout is detached at `e964d2f`.

**O1 (P2) — a refused fixture retry deleted prior evidence before preflight.**
Pro found this by source inspection; it was not reproduced under Windows
PowerShell.

- **Location:** `scripts/New-KbpAutomationFixture.ps1`, in the bootstrap
  branch that handles an existing `$runRoot`.
- **Behaviour at `e964d2f`:** when the prior transaction was
  `RolledBack`, the script ran `Remove-Item -LiteralPath $runRoot -Recurse -Force`.
  That happened before `Assert-KbpFixturePreconditions`, before the seed
  lookup and validation, and before the outer `$PSCmdlet.ShouldProcess`
  decision.
- **Failure sequence:** a real, non-WhatIf retry reusing a rolled-back
  RunId deleted the old run directory. A prerequisite then failed (for
  example, a missing `KBP_ADVANCED_SEED`, a running game or an active
  deployment), so the retry was refused but the previous
  transaction and evidence were gone.
- **Scope:** the path is inherited and is now shared by `-Family Advanced`;
  the family parameter did not introduce it. The affected data were the
  old fixture transaction and evidence, not ordinary saves. `-WhatIf`
  itself did not delete these files; the finding is the mutation during a
  real retry, ahead of the prerequisites and the outer decision.

**Disposition (the preferred repair):** the script never removes an
existing run directory, whatever its status. A same-ID retry is refused
before anything else happens, the prior attempt is kept as recovery
history, and a new attempt needs a fresh `-RunId`. Interrupted-run,
`-Recover` and lock guards are unchanged.

**Isolated regressions** (`Test-RuntimeHarness.ps1`, the real script
against temporary roots, with byte snapshots of the save, state and
archive roots):

| Case | Result |
| --- | --- |
| A | A same-ID retry, both the Advanced family without an advanced seed and the automation family, is refused; the snapshots are unchanged. |
| B | With a Kingmaker-named process running, a same-ID attempt and a valid fresh attempt are both refused. The fresh one is refused by the game-running precondition itself. No state, evidence, archive, save or lock change. |
| C | `-WhatIf` and a genuinely declined confirmation ("N" on the prompt) on a fresh ID both reach the outer decision and mutate nothing. |
| D | A fresh attempt completes. The prior history is byte-identical, the seed and the unrelated and other-family saves are unchanged, and teardown removes exactly the owned pair. |
| E | The rolled-back attempt's retained evidence and an unexpected nested file survive every path. |

Three existing harness tests had asserted the old "same-ID retry after
rollback works" contract. They now assert both halves of the new one:
the same-ID retry is refused with the record byte-identical, and a fresh
ID succeeds. A mutant restoring the old delete-on-RolledBack behaviour
fails case A.

## Previous review dispositions — N-series at `7379dc8` (history)

No cast was run; normal dispatch stays disabled. Source and recording-runtime evidence only.

| Finding | Disposition | Commit | Tests (mutants caught) |
| --- | --- | --- | --- |
| N1 — approval did not bind the frozen artifact before execution | Allowance schema 2 binds commit, package, DLL and MVID; the launcher refuses a manifest mismatch before deploy; the owner runs the boundary preflight first, which measures the LOADED commit/package/DLL/MVID and refuses any mismatch with no observation, executor construction or submission | `b9721c3` | `probe-requires-frozen-artifact-identity` (3/3), 6 launcher binding cases |
| N2 — shutdown was not terminal before the owner existed | The owner is created with the runtime host; after Shutdown, BeginSelection/Submit refuse and Update completes once as a shut-down failure before any work | `b9721c3` | `probe-shutdown-before-selection-is-terminal` (3/3) |
| N3 — prepared-slot observation required a spendable slot | `ProbeSourceSlots.ReservedExactly` reads exactly the reserved tokens, consumed or not, never substituting; a prepared cast is judged by those tokens going available to consumed | `b9721c3` | `prepared-slot-observation-reads-exact-source` (3/3) |

Advanced-copy tooling (`bdac45f`): `New-KbpAutomationFixture.ps1 -Family Advanced`, isolated test only.

## Previous review dispositions — M-series at `1e3c95b` (history)

Normal dispatch stays disabled; no cast was run. Evidence is source and
recording-runtime tests plus selection-only live runs (no boundary
constructed).

| Finding | Disposition | Commit | Tests (mutants caught) |
| --- | --- | --- | --- |
| M1 — zero-cost native sources could not be projected | A known Unlimited pool is one zero-unit native demand reserved by the existing ledger and flagged verified; flag carried by cost line, budget line (shown "unlimited"), converter, projection identity v3 and both executors; unknown/stripped/missing costs refused; spend on a free source fails; feature costs still count | `a023293` | `zero-cost-native-source-integration` (5/5); four legacy fixtures moved to finite pools because their runtimes report a spend |
| M2 — probe after-state came from cached UI discovery | Fresh native reads (ability availability, target buff instances + end time) stamped from one sequence; failed reads recorded, never replaced; before < submission < after; new instance or verified refresh; free = zero change, finite = exactly one; missing ≠ zero; target without the effect preferred | `0a4c382` | `probe-observations-are-authoritative` (6/6) |
| M3 — host did not own probe cancellation | `SingleCastProbeRunOwner`: one idempotent terminal (dispose boundary, fresh after-read, close workspace, release lease, record cleanup, publish once) for completion, stop, deadline, host exception, disable and unload | `0a4c382` | `probe-owner-terminal-cleanup` (3/3), `probe-owner-wiring-in-host-and-main` (1/1) |
| Selection finding | Discovery wraps effects in a self-reference; accepted only for the cast ability itself | `ea45807` | `probe-scope-enforces-whole-subset` (1/1) |

Concrete, unapproved cast proposal: `docs/LIVE-CAST-PROBE-REQUEST.md`.
Advanced-copy inspection request: `docs/ADVANCED-SAVE-COPY-INSPECTION-REQUEST.md`.

## Previous review dispositions — L-review at `475d2b9` (history)

Native dispatch remains disabled in the production workspace. Every row
below is **source/isolated-test evidence only** (protocol suite, isolated
PowerShell fixtures, mutation checks); no row claims gameplay. The
single-cast probe is prepared but has **not** cast anything.

| Finding | Disposition | Commit | Tests (mutants caught) |
| --- | --- | --- | --- |
| L1 — imported constraints bypassable (Ready Casts Only; generic Mark Ready) | Pending plan-wide notices refuse EVERY apply mode in the gate and the session; a refused gate decision never reaches the boundary; unresolved per-casting review items block compiler readiness whatever the state; Mark Ready/Add/Update refuse Ready with unresolved review; content edits cannot change provenance; explicit, undoable, disclosed ResolveImportReview / AcknowledgeImportNotices record resolution beside the kept history (persisted); inspector controls added | `58a1aaf` | `import-requirements-stay-enforced` (6/6) |
| L2 — cancellation did not dispose the active executor | The coordinator owns the nested iterator: disposed exactly once on exhaustion, failure and OUTER disposal; acquisition/MoveNext/Current/Dispose failures halt without replacing the first failure; cancelled runs report once; Processed vs NativeSubmissionReported. Animated executor now reports abandonment and cancel-cleanup failures (reporting only) | `2929985` | `explicit-run-cancellation-disposes-executor` (6/6) |
| L3 — ProjectionId omitted executable fields | Versioned (identityVersion 2) canonical JSON of every step field in order, incl. token ids, enhancement pool usage, source id, expected effect tree, omitted enhancements; set-like values normalized; unrepresentable effect refuses; canonical text exposed | `cd5bc4a` | `projection-identity-is-complete` (6/7; the survivor is equivalent — CastStep already sorts recipients) |
| L4 — partial swap could release an Installed record | Each destructive substep tracked; only a verified byte-identical return yields Installed/not-applied; a failed second move with failed reversal records RollbackRecoveryNeeded with paths/identities and keeps the lock; lock released only after a clean state is recorded | `aa3b95c` | `Test-RestoreInstallLocal.ps1` 16/16 (2/2 script mutants) |
| L5 — file-name scan is not format compatibility | Schema-6 format revisions (1/2/3); writer stamps formatRevision 3; repository refuses newer revisions; the assembly declares `KingmakerBuffPlanner.CandidateProfileFormat = 6.3`, read from the restored binary in a separate reflection-only process; candidates kept only if every member belongs to a revision the target reads; undeclared targets keep none; byte-exact archives | `aa3b95c` | `Test-RestoreInstallLocal.ps1` (4/4 script mutants), `rollback-candidate-format-members-match-model` |
| L6 — probe scope admitted excluded categories | Converter refuses non-spellbook/special source, metamagic, provider/caster mismatch, self-target, unverified target, non-DirectRuleCast strategy, non-plain effect shape; undefined scope enum refused | `cd5bc4a` | `probe-scope-enforces-whole-subset` (5/5) |
| Probe preparation | Dormant selector, strict owner allowance, default-refusing one-shot boundary, gated `live-cast-probe-select` / `live-cast-probe` scenarios; request `docs/LIVE-CAST-PROBE-REQUEST.md` | `6f22a78`, `2117438` | `single-cast-probe-is-dormant-and-one-shot`, `probe-scenario-request-validation`, `single-cast-probe-run-record-rules` (8/8), launcher gating 4 cases |

The first live cast is **not** ready: two selection-only live runs found no
eligible casting on the WORKING fixture (cantrips only). See
`docs/LIVE-CAST-PROBE-REQUEST.md`.

## Previous review dispositions — K-review at `258a1d0` (history)

Native dispatch remains disabled. Evidence for every row below is
**source tests only** (protocol suite / isolated script fixtures); no row
claims gameplay.

| Finding | Disposition | Commit | Tests |
| --- | --- | --- | --- |
| K1 — a failed legacy import activated an empty plan / default replacement | A failed import blocks the workspace with a visible reason: no candidate written, Save and Apply refused, old plan not replaced; legacy `ProfileRepository.Save` quarantines an unresolved primary instead of replacing it (explicit `ReplaceUnresolvedPrimary` recovery) | `e0509fe` | malformed primary, newer primary + valid backup, archive/candidate write failures, retry after repair, legacy save refusal/recovery |
| K2 — import ids could collide | Ids `m5:<routine>:<child>:<recipient key>`; reuse only on exact persisted provenance; any other collision refuses the import (K1 blocks) | `0c7b760` | duplicate child ids across routines, repeated import, unrelated collision, earlier-format reuse without renumbering |
| K3 — unresolved legacy constraints were dropped | Unknown grouping, provider pins, enhancement requiredness, automatic/missing casters and target-less children become Draft castings with durable review items; bans/caps/priorities become plan-wide import notices that block Apply until acknowledged (undoable) | `0c7b760` | unknown grouping, pin, optional enhancement, priority/cap, target-less child, save/reopen, Apply refusal + acknowledgement |
| K4 — converter could drop contracts it cannot carry | Standard scope refuses the whole projection for an enabled targeting modifier, an exact-source enhancement, or incomplete required group coverage; `SingleCastProbe` scope admits one plain direct casting only; deterministic `ProjectionId`; the dispatch boundary receives the exact projection | `b69f873` + probe-case tests | `converter-refuses-unsupported-contracts` (every probe refusal mutation-checked) |
| K5 — a failed/uncertain cast must stop later submissions | `ExplicitCastingRunCoordinator` halts after any casting not positively confirmed, marks the rest NotAttempted, never retries; optional submission limit | `20bbe82` | `explicit-run-stops-after-failure-both-modes` (instant + animated, five failure kinds each; limit 1) |
| K6 — install rollback could mis-record state after a post-swap failure | Prior build prepared and verified in staging (backup never modified); RollingBack + phase recorded before each transition; post-swap failure reverses the swap; unrecoverable reversal records RollbackRecoveryNeeded and keeps the lock; candidate compatibility read from the restored binary; candidates from both sides archived | `b243b02` | `Test-RestoreInstallLocal.ps1` 12 isolated cases incl. injected failures at candidate-archive, settings-merge, identity, verify, record and reversal |
| K7 — selected-buff coverage depended on the lane scope | Coverage computed from every casting of the selected buff, independent of the this-buff/whole-routine toggle | `93408bd` | scope toggle changes cards only, never coverage |

First live cast: **request prepared, not executed** —
`docs/LIVE-CAST-PROBE-REQUEST.md`.

## Previous review dispositions — J-review at `19ecbe8` (history)

| Finding | Disposition | Code | Tests |
| --- | --- | --- | --- |
| J1 — automatic evidence producer/consumer adjacency mismatch | Repaired: structured record; the host fills it through `WorkspaceCastStepEvaluator.Evaluate` and accepts it only through `WorkspaceInteractionRecord.Violations()`; `saved` is observed via `!IsDirty` | `src/KingmakerBuffPlanner/RuntimeTesting/WorkspaceScenarioContracts.cs`, `RuntimeTestHost.cs` (`UpdateWorkspaceInteraction`, workspace result block) | `workspace-interaction-evidence-contract`, `workspace-cast-step-evaluator-real-session`, `runtime-host-scenario-contract-wiring` |
| J2 — manual terminal accepted capture by filename, ignoring failure/restoration | Repaired: `ManualTerminalCoordinator` — bounded 20 s capture wait, failure + restoration verdict consumed, close postcondition (view + input lease) always recorded, four separate manual assertions; camera routine now always reports `RestorationClean` | `WorkspaceScenarioContracts.cs`, `RuntimeTestHost.cs` (phases 31/32, manual result block), `MenuRenderDiagnostic.cs` | `manual-terminal-policy-done-stop-deadline`, `manual-terminal-capture-restoration-cleanup`, `runtime-host-scenario-contract-wiring` |
| C1 — stale current-state summaries | Updated: tracker CURRENT STATE section, this index, `AUTONOMOUS-RESUME.md`, PR body; history kept | `planning/CASTING-FIRST-MIGRATION-STATUS.md` | — |
| C2 — rehearsal-6 evidence distinctions | Raw records re-verified; sanitized receipt published | `docs/evidence/casting-ws-manual-rehearsal-6-receipt.md` | — |
| C3 — manual hold numeric boundary | Protocol reader matches launcher 30–1200 s and range-checks before narrowing | `RuntimeTestProtocol.ReadManualHoldSeconds` | `runtime-manual-scenario-validation` (29, 2^32+300 long, -1 rejected) |

Every new regression was mutation-checked: re-introducing the defect
(already-ready rejected, capture failure ignored, unclean restoration
ignored, unbounded capture wait, lease ignored, sibling change ignored,
done preferred over stop) makes the suite fail.

## What to review, by area

### Casting contract core (domain/compiler/budgets)

- `src/KingmakerBuffPlanner/Domain/Authoring/CastingIntentModels.cs` —
  one saved casting record = one invocation; Draft/Ready/Disabled;
  explicit order.
- `src/KingmakerBuffPlanner/Planning/ExplicitCastingCompiler.cs` —
  deterministic allocation with spell-scoped atomic budgets.
- `src/KingmakerBuffPlanner/Planning/CastingReviewCoordinator.cs` —
  presented-plan review with material-content signatures.
- `src/KingmakerBuffPlanner/Planning/CastingExecutionGate.cs`,
  `CastingForecast.cs`, `CastingTargetingModifiers.cs`.
- Regression tests: `tests/KingmakerBuffPlanner.Tests/Program.cs`
  (custom runner; 231 protocol tests at the J-review repair, incl.
  `casting-workspace-mixed-caster-flow`,
  `casting-workspace-review-and-apply-policy`,
  `casting-workspace-save-reopen-and-protection`,
  `casting-workspace-sibling-intent-vs-derived-changes`,
  `casting-workspace-disabled-dispatch-attempt-only`).

### Workspace UI (session + view)

- `src/KingmakerBuffPlanner/UI/CastingWorkspaceSession.cs` — session
  controller: browse-never-mutates, explicit commands with Undo,
  review integration, `DisabledCastingDispatchBoundary` (records
  attempts, never claims gameplay).
- `src/KingmakerBuffPlanner/UI/CastingWorkspaceScreenView.cs` —
  parchment workspace view (caster lane, casting cards, inspector,
  Review & Apply footer) built against installed Unity 2018.4
  contracts.
- `src/KingmakerBuffPlanner/UI/CastingWorkspaceDevSelection.cs` —
  session-scoped selection; while selected, every legacy quick-exec
  route refuses (`BuffPlannerUiRoot.ExecuteLegacyRoutine`).

### Harness repairs this session (each with a regression test or a fail-closed gate)

- **Launcher `-File` crash** — `scripts/Invoke-KingmakerRuntimeTest.ps1`:
  a top-level `$PSCmdlet.ShouldProcess` throws NullReferenceException
  under `powershell.exe -File` on both the WhatIf and confirmation paths
  (reproduced minimally; works under `-Command`/direct invocation).
  Guard contract: a decision that cannot be evaluated is a REFUSED
  decision for real runs (error propagation, nothing staged); a `-WhatIf`
  request is honored because its outcome is deterministically negative
  and can never create permission. Regression:
  `scripts/Test-RuntimeLauncherFileWhatIf.ps1` (four layers: pattern
  refusal, guard-shape source check, `-File -WhatIf` purity, and a REAL
  `-File` run proving non-zero exit plus zero mutation of every
  protected root; wired into `Test-SourceOnly.ps1`).
- **Workspace routing** —
  `src/KingmakerBuffPlanner/RuntimeTesting/RuntimeTestHost.cs`: the
  workspace dev-selection enable sat dead twice (inside the non-live
  UI-smoke gate; then after `Update()`'s live-UI return); now at the
  top of `Update()`.
- **Honest visual gates** — the `live-workspace-qual` scenario now
  requires `workspaceRoot=active;legacyScreen=closed`
  (`IsCastingWorkspaceOpen`, not the legacy `IsScreenOpen`), dual-path
  frame capture (end-of-frame ReadPixels + luma stats + engine second
  path), `workspace-frame-nonblack` (blackFraction<0.98), and
  `workspace-hierarchy-presents` (active hierarchy, alpha, renderable
  texts). A persistent black frame FAILs the run at
  `workspace-visual-validation`.

## Evidence layer status (honest)

Current (0.2.0-rc1 work, 2026-09-23):

| Layer | Status |
| --- | --- |
| Source tests | Full gate at every pushed source commit (Windows PowerShell 5.1, non-interactive); 294 protocol tests at `542cd66`. Every guard added in the RC iteration has a caught mutant. |
| Native gameplay | One-cast probe (`casting-probe-cast-20260923-p2-02`) and the zero-cost qualification (`casting-qual-cast-20260923-q1-01`): real casts through the production Apply, stop, skip, repeat and a recast after a reopen, each exactly as forecast. Finite resources, group, metamagic, rods and animated mode not yet cast in game. |
| In-game persistence | Save, close and reopen (`casting-ws-qual-*`); an in-game reload of the exact test save with the saved plan and one set of handlers after it (`casting-ws-reload-20260923-s2-01`). |
| Workspace presentation | Game frames at 1920×1200 (scale 1.000, like the native UI); other resolutions untested. |
| Restoration | Every live run restored the Mods folder byte-exact and compared every save. |
| Manual acceptance | Not yet; the supervised session is prepared. |

History:

| Layer | Status |
| --- | --- |
| Source/protocol tests | 231/231 protocol PASS locally at the J-review repair (the full `Test-SourceOnly.ps1` gate counts for the pushed HEAD are recorded in `AUTONOMOUS-RESUME.md`). Historical: 217/217 at checkpoint 12. |
| Live campaign qualification | Campaign load, UMM close, workspace open through the production hotkey path: PROVEN (run `casting-ws-root-200200`, game log) |
| Visual capture | Backbuffer presentation dies with a disconnected session (classified by matched control/open/closed bisection); the camera-render capture lane bypasses the display path and produced rendered workspace frames including the live authoring run. Display-path acceptance still pending a connected session. |
| Interaction/execution | **Corrected control-path LIVE-PASSED** (run `casting-ws-gseries-081000`; supersedes the `casting-ws-controls-051500` claim, whose assertions could not detect refused Adds): every action through real ACTIVE buttons — source, caster, recipient, state, Add (×3 with one-record growth, distinct identities, exact authored fields, unchanged siblings), a deliberately refused Add leaving the document untouched, Edit, focused retarget, Undo restoring the exact pre-edit canonical document, Done clearing focus, Save, and production-route reopen with the full canonical signature preserved and a clean dirty state. Protocol layer additionally proves group transitions, focused enhancements, campaign binding, and a fresh-session persisted round trip (223/223). Native submission remains disabled; physical-input (real pointer/keyboard) acceptance and the native aesthetic pass remain OPEN. |

## Published evidence images (game-window-only, authorized runs)

| File | Original run artifact (identical SHA-256) | What it shows |
| --- | --- | --- |
| `docs/evidence/casting-ws-gseries-081000-full-acceptance.png` | `runtime-evidence/casting-ws-gseries-081000/workspace-frame.png` (`38fb627f…`) | **Corrected full-acceptance run, display path**: the open workspace (mean 0.73 vs control 0.14, changed 0.95) from the run whose sequence requires real active controls, record-level Add assertions, a refused-Add negative, and intent-exact Undo. The earlier `casting-ws-controls-051500` frames are superseded by this run (its assertions could recycle a last-record ID). |
| `docs/evidence/casting-ws-controls-051500-control-backbuffer.png` | `runtime-evidence/casting-ws-controls-051500/workspace-control-frame.png` (`0ea31e28…`) | Matched control frame (workspace closed) — retained for the superseded 051500 run. |
| `docs/evidence/casting-ws-author-033000-interact-authored.png` | `runtime-evidence/casting-ws-author-033000/ws-interact-authored.png` (`62c2a167…`) | The authoring workspace LIVE with three authored casting cards (cast-1/2/3, two casters), draft editor in the inspector — captured after the scripted interaction sequence (browse → 3 casts → edit), through the camera lane. The run's behavior assertions PASSED: `workspace-interaction-sequence` (browseNoMutation=True; capableCasters=3; cast1/2/3=applied; edit=applied; undo=True; saved=True) and `workspace-reopen-preserves-intent` (exact IDs/order, loadStatus=Loaded). |
| `docs/evidence/casting-ws-author-033000-interact-reopened.png` | `runtime-evidence/casting-ws-author-033000/ws-interact-reopened.png` (`45e0cdeb…`) | The workspace after close + production-route reopen with the saved candidate intact (cast-1,cast-2,cast-3 preserved in order). |
| `docs/evidence/casting-ws-layer-030000-workspace-camera-frame.png` | `runtime-evidence/casting-ws-layer-030000/workspace-camera-frame.png` (`0a9a2fc9…`) | **The casting workspace rendered** — parchment panel, header "Casting Workspace", caster lane with three casters + Focus buttons, casting cards, inspector, footer Undo/Accept Plan/Review & Apply. Captured through the camera-render lane (display-independent) in a session whose backbuffer was black. Known defects visible: caster names show unit GUIDs (display names not reaching the row model); lane/card label truncation. |
| `docs/evidence/casting-ws-layer-030000-camera-control.png` | `runtime-evidence/casting-ws-layer-030000/workspace-camera-control.png` (`ce4b468c…`) | Matched control frame, same session/build/resolution, workspace closed: game scene + HUD (mean luma 0.41 vs the open frame's 0.73). |
| `docs/evidence/casting-ws-visual-183000-legacy-misroute-frame.png` | `runtime-evidence/casting-ws-visual-183000/workspace-frame.png` (`3c54fe28…`) | The run that PASSED identity checks while showing the LEGACY catalog screen — the misroute that the routing fixes address. Non-black dual-path capture (blackFraction 0.0285). |
| `docs/evidence/casting-ws-root-200200-hud-only-open-workspace.png` | `runtime-evidence/casting-ws-root-200200/workspace-frame.png` (`f6aa382a…`) | The run where the REAL workspace opened (log-proven, `workspaceRoot=active`, legacy closed) yet the frame shows the plain game HUD — later explained by the layer-culling defect fixed at `72209fc`. |

No image edits: the published files are byte-identical copies of the
run-time captures. Black-frame and missing-content checks were never
relaxed to obtain a PASS; the two runs above are recorded failures of
the visual gate on their own terms.

## Durable records for the full chain

`planning/CASTING-FIRST-MIGRATION-STATUS.md` (CURRENT STATE section),
`AUTONOMOUS-RESUME.md`, `AUTONOMOUS-BLOCKERS.md`,
`KINGMAKER-BUFF-PLANNER-JOURNAL.md`.
