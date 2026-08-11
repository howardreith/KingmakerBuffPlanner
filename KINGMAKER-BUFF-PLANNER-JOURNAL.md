# Kingmaker Buff Planner Journal

## 2026-08-11 — Phase 0 intake

Status: IN PROGRESS

- Branch: `codex/kingmaker-buff-planner`
- Intake HEAD: `c7913b1cbca699cde5b24b1bcd1d36344402bb0c`
- Remote branch and `origin/main` at intake: same SHA
- Worktree at intake: clean; no repository lock or concurrent agent found
- Mission source/copy SHA-256: `3682d33c3c37e4f8cf67983f2a147599164e7da0df5b9d884c427f9ab1c17308`
- Active version: not assigned; Phase 1 not yet scaffolded
- Last successful gate: authoritative contract and mission fully read; standalone repository identity established
- Exact local target: Kingmaker 2.1.7b, Unity 2018.4.10f1, UMM 0.28.2, Harmony12 1.2.0.1, net47/C# 7.3
- Reference validation: BubbleBuffs, BuffIt2TheLimit, PathfinderAutoBuff, and KingmakerRebalance/COTW licenses and immutable identities recorded
- Exact-assembly work: action-graph, spellbook, resource, `AbilityData`, `RuleCastSpell`, and `UnitUseAbility` contracts inspected
- Rejected theory: a transferred laptop UMM version is the desktop target; exact local UMM is 0.28.2
- Rejected theory: `RuleCastSpell` spends resources; exact IL shows native command code triggers the rule and then calls `AbilityData.Spend()` once when appropriate
- Runtime/profile state: no transaction active; runtime roots empty; no game process; live external state untouched
- Known constraint: no `KBP_` disposable save; save-backed scenarios cannot truthfully run
- Current files being changed: governance, architecture, contract inventories, matrices, journal, blockers, and resume state
- Exact next action: finish read-only harness intake, create redacted fingerprint schema/exact private fingerprint, then scaffold Phase 1

## 2026-08-11 — Phase 0 checkpoint

Status: PASS

- Branch: `codex/kingmaker-buff-planner`
- Commit: `ff54df0c06b4a366f715a4cac2654e358cc84c66` (`docs: record autonomous mission intake`)
- Work completed: first coherent intake commit; exact mission copy, environment/reference fingerprint, architecture/source matrix, discovery/resource contracts, coverage/runtime/compatibility matrices, blockers, journal, and resume records
- Validation: `git diff --cached --check` PASS (0 defects); prohibited payload review PASS (0 payloads)
- External state: none altered; no deployment/runtime transaction
- Current uncertainty: desktop UMM deviation and missing `KBP_` save remain recorded, not concealed
- Exact next action: commit the four remaining mission-required architecture/runtime documents before production scaffold

## 2026-08-11 — Phase 1 source/build/package scaffold

Status: IN PROGRESS

- Branch/HEAD: `codex/kingmaker-buff-planner` at `4ca6008d873577e8e6263b54658620b649f81cd1`
- Active version: 0.0.1
- Work completed: classic net47/C# 7.3 project, exact UMM 0.28.2 lifecycle, Info.json, atomic strict request/result skeleton, build/package/source validation, deterministic project-owned test runner
- `scripts/Test-SourceOnly.ps1`: source PASS 14/14; protocol PASS 11/11; suite PASS 1/1
- `scripts/Build.ps1 -Configuration Release`: PASS 1/1, zero warnings
- `scripts/Build-Local.ps1` repeated twice: package validation PASS 4/4 each; identical hashes
- Pre-checkpoint DLL SHA-256/MVID: `3b21bec7b283ab3f760e1c7fe81de6b9d06794696d323e35c9c2d498c4a78cee` / `e07bf29a-cd45-44bf-870c-6cfa19988822`
- Pre-checkpoint package SHA-256: `f109c51b8afcd0af931c556ad5999ea62c1b88f951178da8d563253c18de4d21`
- Failure repaired: generated build-info semicolons were split by MSBuild item syntax; encoded semicolons and line items now generate valid C#
- Failure repaired: ZIP timestamps are timezone-free DOS wall-clock values; validator now compares the deterministic wall-clock representation
- UMM dependency finding: UMM 0.28.2 references runtime-host Harmony 2.3.1/net48 internally. It is marked `ExternallyResolved` so net47 compilation uses the exact UMM public API without resolving or shipping the host’s transitive internals; no warning is suppressed
- Runtime/profile state: no transaction active; no external state altered
- Exact next action: checkpoint scaffold, rebuild from clean exact commit, then implement and source-qualify the guarded deployment/runtime transaction

## 2026-08-11 — Guarded runtime transaction and Steam safety

Status: IN PROGRESS

- Branch/HEAD: `codex/kingmaker-buff-planner` at `aa3c4c521394a94fbab54526f22c907580ade0cd`
- Active version: 0.0.1
- Clean checkpoint rebuild before harness changes: source 15/15, protocol 12/12, runtime transaction 5/5, deployment WhatIf 5/5
- Whole-`Mods` transaction: project lock/token, state, isolated staging, backup, live sentinel, mutation observation, exact manifest restore, and fail-closed recovery
- Synthetic scenarios PASS 5, FAIL 0: existing tree, absent tree, staged mutation, running-process refusal, foreign-lock refusal
- Public `Deploy-Local -WhatIf`: PASS 5 compared roots, FAIL 0; live `Mods` and all runtime roots unchanged
- Steam finding refined: startup attempted protected-save downloads, then the same session logged off and App 640820 recorded `Sync Disabled` with `offlineMode=true` after transfers; current NO-SAVE launch preflight can be proven
- Privacy decision: raw Steam/account log lines are not copied into evidence; only sanitized state and timestamps are retained
- Runtime state: no transaction active; no game launched; live `Mods` unchanged
- Exact next action: checkpoint/rebuild the guarded harness, run top-level runtime WhatIf, then request the guarded fresh-process mod-load run

## 2026-08-11 — First guarded mod-load attempt and recovery

Status: FAIL

- Source commit: `3ae93f8653afa90bb606ffccea1c24d5869ba22e`
- Active version: 0.0.1
- Package/DLL SHA-256: `8c9d041bc120ae583bf808ce96ce436df308655b6397e2abbeceb978f230547b` / `f18ce1a81fb8df7b9142bd89af9a3e521a748a7c82d9b3ade3a57a2f133cefed`
- Runtime evidence ID: `20260811T1931581708021Z-mod-load-smoke`
- UMM evidence: exact 0.28.2 manager loaded 1/1 mods; Kingmaker Buff Planner 0.0.1 and exact embedded commit loaded/enabled
- Failure: `RuntimePaths` equivalent logic interpreted a trailing UMM mod path one level too high and looked for `Mods\Kingmaker.exe`; atomic result status FAIL, stage `unhandled-exception`
- Safety behavior: game requested clean exit; orchestrator observed it during the exit race and conservatively withheld restoration
- Recovery: after proving Kingmaker absent and sentinel/token ownership exact, guarded `Restore-Local.ps1` completed; status Restored, `restorationVerified=true`, original/live manifest equality true, lock absent
- No save was selected, loaded, or written
- Regression: explicit path helper tests trailing and non-trailing paths; protocol suite now PASS 14/14; orchestrator waits up to 30 seconds for its process in `finally`
- Exact next action: checkpoint fix, rebuild exact package, repeat top-level WhatIf, then run fresh-process mod-load qualification again

## 2026-08-11 — Second mod-load attempt, validator mismatch

Status: FAIL

- Source commit: `c0423d3068506b50a255cc264e93b5e1c5e48708`
- Evidence ID: `20260811T1935262721216Z-mod-load-smoke`
- Package/DLL SHA-256: `108ae1b8c8f702729759f35c6e422a1390c576455d7ca1370199fd048aabea60` / `491447f43c748fd2e091d7dc474eebc135aa7d4d0a5c4ea1f9485eddd821e2cb`
- In-game result: PASS, 5 assertions, MVID `16d7db94-a319-4242-b036-e303d8ca9ddf`; exact game executable, UMM, Harmony, DLL, package, version, and commit hashes recorded
- Orchestration failure: expected UMM display version `0.28.2`, observed exact runtime API version `0.28.2.0`
- Restoration: PASS in `finally`, original manifest verified; no lock and no game process remain
- Staged mutation observed: true, consistent with UMM runtime cache behavior; future states retain the observed staged manifest before quarantine disposal
- Fix: validator now requires `0.28.2.0` and reports exact field mismatches
- Qualification decision: not counted as one of the required two orchestration PASS runs
- Exact next action: checkpoint validator/evidence improvement, rebuild clean package, run WhatIf and two fresh-process PASS attempts

## 2026-08-11 — Phase 1 fresh-process qualification

Status: PASS

- Source commit: `d3f4dc1fec9970b1c3c8eed5100052edd996c870`
- Active version: 0.0.1
- Package/DLL SHA-256: `885113c4f8e1bb3a271579188823ceb3704a3b1f531ddce32efe26fa5f295764` / `1470d6f3bc45d7612f7668e87ce862ff7d89aa415e5634581e8b23924a4ba235`
- MVID: `e268ff99-af2e-4def-8511-746cbd1b5106`
- Source gates: validation 15/15, protocol 14/14, transaction 5/5, deployment WhatIf 5/5, package 4/4
- Runtime PASS 1: `20260811T1938252394711Z-mod-load-smoke`, assertions 5/5, restoration exact
- Runtime PASS 2: `20260811T1939275269790Z-mod-load-smoke`, assertions 5/5, restoration exact
- Runtime platform: Kingmaker 2.1.7/2.1.7b executable identity, UMM 0.28.2.0, Harmony12 1.2.0.1
- Staged UMM cache mutation recorded on each run; original live tree restored without it
- Final external state: no Kingmaker process, no deployment lock, live/original manifest equality true, no save accessed
- Last successful gate: Phase 1 minimal loadable standalone mod complete
- Exact next action: Phase 2 normalized effect-expression domain and generic action-graph scanner with realistic pure fixtures
