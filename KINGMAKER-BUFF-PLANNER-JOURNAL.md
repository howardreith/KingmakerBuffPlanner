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
