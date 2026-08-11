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

## 2026-08-11 — Phase 2 structural discovery and deterministic catalog

Status: PASS

- Branch/source commit: `codex/kingmaker-buff-planner` / `07dc2380abbac74228eed88ce73113aeeabe61db`
- Active version: 0.0.1
- Source gates: validation 15/15, protocol/domain 20/20, runtime transaction 5/5, deployment WhatIf 5/5, package 4/4, warning-free Release build
- Package/DLL/MVID: `b128386d8d6b6715a4d51190faf1d971304bd0fb748c7c0d4f57d6293ec9b20c` / `61b63cfa352ae9bc9e15b8aa10a08ff9f2fe2ac3ff2c5ee059f38d2f4e0df975` / `7f7ba872-03ce-4554-ae1d-3a5942e62e8d`
- Runtime PASS runs: `20260811T1958580589645Z-native-buff-catalog` and `20260811T2000040593873Z-native-buff-catalog`, each 8/8 assertions and `restorationVerified=true`
- Determinism: both native-only processes produced catalog SHA-256 `bcacbe69bc71c85c5299b8fe8254c18baa33d66e9e3ecf53f6d4aa6b37094878`
- Catalog: 1,722 abilities; 1,353 preliminary candidates; 1,095 effect-bearing graphs; 755 abilities/455 candidates with explicit unknown-action diagnostics; zero scanner exceptions
- Failure repaired: the first exporter root serialized as `{}` under the game's Json.NET defaults; explicit DTO wire contracts and root reconciliation added
- Failure repaired: a later inspection showed nested expressions serialized as `{}`; explicit expression contracts, per-row discriminator validation, and a regression test added
- Qualification boundary: Phase 2 structural catalog is PASS; no candidate is yet claimed fully supported until provider/resource/planning/execution audits complete
- External state: game absent, runtime lock absent, original whole `Mods` tree exactly restored; no save selected or accessed
- Exact next action: implement Phase 3 normalized party/provider/resource snapshot domain and pure snapshot tests, then add the guarded save-backed scenario only when a project-owned fixture exists

## 2026-08-11 — Phases 3–5 source implementation checkpoint

Status: PASS for source/pure behavior; runtime gates DEFER — EVIDENCED

- Branch/HEAD: `codex/kingmaker-buff-planner` at `40b233a52aebf1f87d9fc671ae24d9cf86f7150e`
- Active version: 0.0.1
- Provider/resource commit: `18776f1a650e311c4400c22d0f2764665cc6b046`
- Planner/effect detector commit: `5cbc9bb86fe9fcb79c16f8297c5a8754f34eda05`
- Persistence commit: `40b233a52aebf1f87d9fc671ae24d9cf86f7150e`
- Source gates: validation 15/15, protocol/domain/persistence 37/37, runtime transaction 5/5, deployment WhatIf 5/5, package validation 4/4
- Proven pure behaviors: stable variant/metamagic/provider identities; no spontaneous pool multiplication; primary/linked opposition spend; domain isolation; explicit unlimited casts; deterministic priorities/bans/caps; prepared-before-flexible tie-break; mass one-cast cost; typed AllOf/conditional-AnyOf presence; exact skip marker; material availability/count reservation
- Persistence: schema 2 external profile keyed by hash of exact `Player.GameId`; three bounded prior-valid backups; atomic fsync/replace; schema-1 migration; malformed, duplicate, unknown, and campaign-mismatch validation; no campaign save facts
- Runtime boundary: `party-provider-scan`, prepared/spontaneous plans, active-effect policy, profile reorder/reload remain deferred because the live inventory contains no authorized `KBP_` save
- External state: no game process, no runtime lock, no transaction; no live mod or save mutation
- Rejected shortcut: treating compilation/pure fixtures as save-backed party/resource/execution qualification
- Exact next action: implement Phase 6 standalone diagnostic setup UI and source-level lifecycle/controller tests, while retaining runtime UI gates as deferred until an authorized fixture exists

## 2026-08-11 — Phase 6 runtime UI and Phase 7–8 source engines

Status: Phase 6 partial PASS; executor source PASS; save-backed executor gates DEFER — EVIDENCED

- UI source commit: `4eca8b2894e9451398d2046ea8f74d70efe365f8`
- Strengthened UI runtime commit: `c65fec1c83dd9bdae3ea5dd0b445436eff933102`
- Animated executor commit: `41174bb61c72d2f6246460afaada71086ec24385`
- Instant executor commit/current HEAD: `ebca55bab80eb2dc54cb1a725fff7b853101daa1`
- Source gates: validation 15/15, protocol/domain/UI/execution 41/41, transaction 5/5, deployment WhatIf 5/5, package 4/4
- UI runtime: `20260811T2036033407315Z-ui-root-smoke`, 9/9 assertions, 2 cycles, 12 open render frames, 1 root, 2560x1440, exact restoration
- Runtime package/DLL/MVID for UI proof: `ed21469ffe335f6111a477ef3bda0d4690a82ae1bf446882662726b769f6ca9f` / `ae459a27720feec946bb29efb12b3dd742b15291f835efbf575db022785032d9` / `c488303c-b96e-4346-a3fb-31ee28ddd5cc`
- Animated engine: no global command patch; native command queued without interrupting unrelated commands; final validation, scoped polling, effects/reporting
- Instant engine: final revalidation; one `RuleCastSpell`; one `AbilityData.Spend()` iff not UMD-failed; batches default to 8; no direct buff/enchantment application
- Uncertainty retained: exact resource/effect equivalence, invalid/no-spend, material spend, sticky touch, scene transitions, and campaign UI require an authorized save fixture
- External state: restored; no game, lock, or save mutation
- Exact next action: add compatibility profile automation and Call of the Wild generic-discovery qualification, then continue native candidate classification without hiding unknowns

## 2026-08-11 — Phase 9 complete native structural audit

Status: PASS for classification/reconciliation; runtime equivalence DEFER — EVIDENCED

- Branch/runtime source: `codex/kingmaker-buff-planner` at `fba6e24`
- Source gates: validation 15/15, behavior 43/43, runtime transaction 5/5, deployment WhatIf 5/5, package 4/4
- Exact player-accessible inventory: 974 candidates from player class/race/feat roots out of 1,722 native abilities
- Reconciled disposition: automatic 396; generic reflection 1; explicit adapters 13; overrides 3; excluded 561; unsupported/unclassified 0
- Generated schema-4 catalog SHA-256: `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa`
- Deterministic guarded runs: `20260811T2126328544602Z-native-buff-catalog` and `20260811T2127318144905Z-native-buff-catalog`; byte-identical, exact restoration, no save
- Package/DLL: `766514afc64d96cd719b2237d3435156987f05ba46d6f10b0f09f0806647ca79` / `0e0423f4d33f733421a9181299b661b608b8a328e16ba479661c66c82318cb35`
- Repaired theories: broad spell/effect candidacy; summon after-buffs as source effects; sticky carrier buffs; hostile self/current-target markers; cooldown-only healing markers; flattened selected/random alternatives
- Exception layer: strict packaged schema-1 registry, three friendly include overrides and two direct-healing cooldown exclusions; structural discovery remains primary
- Qualification boundary: all 413 included rows await save-backed resource/effect/executor equivalence; no runtime PASS inferred
- External state: game absent, lock absent, whole original `Mods` tree restored exactly
- Exact next action: checkpoint generated audit/docs, then Phase 10 HUD routine execution and service composition

## 2026-08-11 — Phase 10 routine composition and first HUD smoke

Status: source behavior PASS; no-save HUD smoke 1/2 PASS; full Phase 10 gate IN PROGRESS

- Branch/runtime source: `codex/kingmaker-buff-planner` at `420d7a1f1f20f49706a34d953df5f0d39f67e4a8`
- Implementation commits: `9191d4a` executable routine workflow; `420d7a1` runtime HUD assertions
- Source gates: validation 15/15; behavior 45/45; runtime transaction 5/5; deployment WhatIf 5/5; package 4/4
- Routine planner shares resource pools, material inventory, and provider cast caps across every source in one deterministic routine
- Player controls: persistent Long/Important/Short HUD buttons, setup preview/run controls, execution-mode and combat-policy controls, preview counts, and actual execution reports
- Runtime PASS: `20260811T2144351145396Z-ui-root-smoke`, assertions 11/11, 3 routine buttons, critical controls on-screen, 2 open/close cycles, 12 rendered frames, singleton root, 2560x1440
- Package/DLL/MVID: `e544b7b2940a455c9ac886237ae5d42420c8b32b6657736af06661c369406e72` / `bd3248bb56314fa68d8ecaf16761410c114d2578df24fcdd1da4cdcd1e35bdfb` / `cfa3b527-3186-4c0a-ba07-21fbaa49bf36`
- Transaction: `Restored`, `restorationVerified=true`; exact original whole `Mods` manifest restored; no save selected or modified
- Qualification boundary: this is the first of two required HUD smokes and one resolution. Campaign interaction, other required resolutions/scales, setup polish gaps, and save-backed routine execution remain open
- Exact next action: complete setup filters/sorting/provider-resource/unsupported-fallback detail and reset affordance, then repeat clean no-save HUD smokes

## 2026-08-11 — Phase 10 final no-save HUD qualification

Status: PASS for the mission-defined no-save gate; configured save-backed execution DEFER — EVIDENCED

- Branch/runtime source: `codex/kingmaker-buff-planner` at `cfce05c921735c33617342e9b409eeae556a16f9`
- Source suite: validation 15/15, behavior 46/46, harness 5/5, WhatIf purity 5/5, package 4/4
- UI completion: configured/unconfigured, duration, hidden, and source-category filters; name/level sorting; localized description/duration; unsupported saved-source display; provider caster level, remaining casts, pool and rejection reason; unfulfilled target states; bounded clear confirmation; pre/post result panels
- Instant fallback completion: sticky-touch providers are marked per step; hybrid executor uses animated fallback when allowed and fails before fire/spend when blocked
- Runtime PASS 1: `20260811T2201533093853Z-ui-root-smoke`, 15/15 assertions
- Runtime PASS 2: `20260811T2202528930958Z-ui-root-smoke`, 15/15 assertions
- Both: one final root after one forced reconstruction, two open cycles, 12 rendered frames, 3 HUD routines, 3 layout profiles (1920x1080@1.0, 2560x1440@1.25, 3840x2160@1.5), zero full-screen blockers, zero event subscriptions, actual 2560x1440 process
- Package/DLL: `c2aaac56568366e275302650dbaecd15d984158b5af4f98a670bd90b478937ba` / `961ac1a3b2c5f9194fb67413e12ba0c5e034ff2d11f2e240f16d1d02cf4e75f6`
- Both transactions: `Restored`, `restorationVerified=true`; no save selected or modified
- Qualification boundary: source behavior and no-save Phase 10 gate PASS. Configured routine firing/effects/resources remain deferred to the missing project-owned `KBP_` fixture
- Exact next action: Phase 11 exact optional-mod manifests and generic discovery profiling, beginning with locally installed Call of the Wild

## 2026-08-11 — Phase 11 compatibility foundation

Status: source and guarded-staging contracts PASS; Call of the Wild runtime catalog pending

- Branch/base HEAD: `codex/kingmaker-buff-planner` at `cfce05c921735c33617342e9b409eeae556a16f9`
- Added strict schema-1 profiles for native-only, Call of the Wild, Tabletop Added Rules, and combined configurations; unavailable local references fail closed instead of fabricating support
- Exact Call of the Wild fixture: version `1.14.4c-2.1`; 266 files; 66,201,967 bytes; full directory manifest SHA-256 `26ce134fda9a6421519959d9cc9c3f8c5d4cf3288f48ba7f768df47c7704541a`; DLL SHA-256 `4ebf8e1ed3e66ffed72ea33ea325595629423dacd5bffa23e3c9109144b26915`
- Transactional harness now verifies the read-only fixture, copies it into transaction-owned staging, re-verifies the staged copy, activates the complete staged tree, and restores the original whole `Mods` tree exactly in `finally`
- Runtime protocol binds profile ID, exact optional assembly expectations, and representative blueprint GUID expectations; results expose profile, assembly, and optional catalog counts
- Blueprint ownership is derived from the exact staged Call of the Wild `loaded_blueprints.txt` inventory without a compile-time gameplay-mod dependency
- Gates: source validation 15/15; protocol/domain 48/48; runtime transaction 6/6; deployment WhatIf 5/5; package validation 4/4; production build 1/1
- Rejected shortcut: treating the optional assembly loading as proof that its abilities were discovered or classified
- External state: no game, lock, transaction, live mod, or save mutation
- Exact next action: commit this foundation, build/package that commit, and run the guarded Call of the Wild structural catalog to derive representative blueprint assertions and audit every optional unsupported row

## 2026-08-11 — Call of the Wild diagnostic catalog

Status: PASS as diagnostic evidence; final exact-profile repetition pending

- Run `20260811T2218236669629Z-native-buff-catalog` loaded exact Call of the Wild `1.14.4c-2.1` and exact Kingmaker Buff Planner commit `0a5dd4a85ba0e58dc8b9425ced90f76829df18d3`
- Catalog SHA-256 `b943093a8edbfa9c62187a5f795ea1263291adbdc8853212c8d80676bcec9951`; 9,064 abilities, 5,907 candidates, 7,342 Call of the Wild-owned abilities, 4,937 optional candidates, 2,096 optional included, 0 optional unsupported
- Optional support classes: 2,008 automatic, 61 bounded generic-reflection wrappers, 27 explicit adapters; 5,246 excluded by definition
- Evidence-derived representatives: Dazzling Blade `0027cbfe0a484380ab76df1ad3d7326a` (automatic apply buff), Regenerative Sinew `03963bcf8dd64abea3757311c1e8a79c` (bounded wrapper), Bless Weapon `151b1f365c4217e5062a1fe50f7a63d3` (worn-item enchantment), Globe of Invulnerability `4421fff35fed4afb9ea20cbd6e6a7c0d` (area-effect buff)
- No optional adapter was added: the existing structural contracts and bounded wrapper policy classified every in-scope optional candidate
- Transaction restored exact original `Mods`; no save was selected or written; game and lock absent afterward
- The run did not yet enforce representative GUIDs or emit the required Harmony owner/order inventory, so it is retained as diagnostic rather than final Phase 11 qualification
- Exact next action: checkpoint strict representatives plus Harmony inventory instrumentation, then repeat the exact Call of the Wild profile twice from fresh processes

## 2026-08-11 — Harmony inventory runtime defect

Status: runtime FAIL repaired; regression PASS; exact restoration PASS

- Run `20260811T2226321948779Z-native-buff-catalog` failed at `unhandled-exception` because the exporter initially treated legacy Harmony12 `GetPatchedMethods` and `GetPatchInfo` as static APIs
- Exact assembly inspection proved both methods are public instance methods on `Harmony12.HarmonyInstance`; `Create(string)` remains the public static factory
- The exporter now creates an inventory-only instance and invokes the exact installed instance API; a new assembly-backed regression test calls the installed `0Harmony12.dll` and reconciles target/patch counts
- Updated source gates: validation 15/15, behavior/protocol 51/51, transaction 6/6, deployment WhatIf 5/5, package 4/4
- Failed-run transaction restoration verified exact; no save was selected or written; game and lock absent afterward
- Rejected shortcut: removing Harmony evidence or accepting an empty placeholder inventory to make the compatibility gate pass
- Exact next action: commit the repair and rerun the exact-profile qualification twice

## 2026-08-11 — Call of the Wild final repetition and native zero-patch edge

Status: Call of the Wild PASS twice; native-only runtime defect repaired

- Exact commit `fa5a525842fc095514d76da4869fe06f2d65defc`, package `e133d1002e0e0193579f98b7f1e3b104206cb9062cc2065811561ebb2bf45d39`, DLL `00a7bfa85cb8fbd867ce40094d720c1b889629324913a8b08e3b95581d4e813a`
- Call of the Wild runs `20260811T2229574550340Z-native-buff-catalog` and `20260811T2231510699549Z-native-buff-catalog` each passed 21/21 assertions and restored exactly
- Both emitted byte-identical catalog SHA-256 `357e2148c6f4610c3456a9082369fe4bea77580e0faee4de55e637c409bfc8f8` and Harmony inventory SHA-256 `a883dd60218a1f9e989a4e6b03d99318242d401d33f914cd6c068f767b308427`
- Harmony inventory: 207 targets, 228 ordered patch records; owners CallOfTheWild 225, UnityModManager 2, UnityModManager.UI 1; zero multi-owner targets and zero Buff Planner overlaps
- Native-only run `20260811T2234085703022Z-native-buff-catalog` exposed that UMM does not load Harmony when no staged mod consumes it; the catalog was written but inventory failed before result assembly
- Repair: the inventory exporter now loads only the exact installed `0Harmony12.dll` path when the assembly is absent, validates its assembly identity, and then inventories the valid zero-or-more patch state
- Failed native transaction restored exactly; no save selected; game and lock absent
- Exact next action: commit, rebuild, and repeat the native-only profile twice

## 2026-08-11 — Native profile repetition and UMM identity audit

Status: native-only PASS twice; compatibility identity gate strengthened before final report

- Native runs `20260811T2236376737183Z-native-buff-catalog` and `20260811T2237377143159Z-native-buff-catalog` each passed 10/10 and restored exactly
- Both emitted byte-identical catalog SHA-256 `0b758785ef211fcaf50ae481ea48b5da278aa0a6410ab1b034edf5edf8f3d869` and Harmony inventory SHA-256 `b5605e22bde458a238d63c6ffe33a99eb712bd22bf3cbc74c42d443ad479efb4`
- Native counts remained 1,722 abilities / 974 candidates / 413 included / 561 excluded / 0 unsupported; Harmony recorded three UMM-owned patches and zero multi-owner or Buff Planner overlaps
- Final report audit found that staged exact hashes plus assembly load did not independently prove the optional UMM entry ID/version or reject duplicate entries/assemblies
- Runtime identity validation now requires exactly one Buff Planner assembly and UMM entry, exactly one expected optional assembly and UMM entry, exact optional UMM version, exact optional assembly hash, and matching loaded counts
- Rejected shortcut: describing an assembly hash match as full proof of the UMM identity/version requirement
- Exact next action: commit the strengthened gate and repeat native-only and Call of the Wild twice on one exact commit

## 2026-08-11 — Phase 11 final compatibility qualification

Status: PASS for every locally available no-save profile; unavailable and save-backed boundaries recorded

- Exact commit `57ed740d6fd6bbdf68dcdfe8c26368c744a3c91f`, package `29ee2ddb86f6ace36d9b9ec1cc7e75a2468f81721956836e39dbdae26752deb2`, DLL `2c79eefe2afc9a93ec0574a0056c7c9331ebb1add170561f6da4c72468687b13`, MVID `18be2ec9-702a-4ba1-ae15-306765e4231d`
- Call of the Wild runs `20260811T2241040368066Z-native-buff-catalog` and `20260811T2242392501436Z-native-buff-catalog`: 26/26 each; exact UMM ID/version and unique entries/assemblies; 7,342 optional abilities, 4,937 candidates, 2,096 included, 0 unsupported; four representative contracts; catalog hash `7b54f3f9f6d90d339c4cabeedb04c9d15bcb4d51d8e7d830150a18ab6eced659`
- Native-only runs `20260811T2244134771182Z-native-buff-catalog` and `20260811T2245180617144Z-native-buff-catalog`: 12/12 each; 1,722 abilities, 974 candidates, 413 included, 561 excluded, 0 unsupported; catalog hash `50f66299912bef24a50984d9d8398ba2bb340a4f85b551a0ad6ff97c41393f3d`
- Call of the Wild Harmony inventory `a883dd60218a1f9e989a4e6b03d99318242d401d33f914cd6c068f767b308427`: 207 targets / 228 records / 0 multi-owner / 0 Buff Planner overlap; native inventory `b5605e22bde458a238d63c6ffe33a99eb712bd22bf3cbc74c42d443ad479efb4`: 3 / 3 / 0 / 0
- Every run used a fresh process, passed its compatibility-specific WhatIf/preflight, restored the original whole `Mods` tree exactly, and selected or wrote no save; final process count 0 and lock absent
- Tabletop Added Rules and combined profiles: `UNAVAILABLE-LOCAL-REFERENCE`; Shield Other: `FEATURE-NOT-PRESENT-IN-SNAPSHOT`; no external download or unrelated-project mutation
- Save-backed optional planning/execution remains `DEFER — EVIDENCED`, not PASS, because no authorized `KBP_` fixture exists
- Exact next action: commit reports and proceed through Phase 12 hardening, deterministic final packaging, final core repetitions, guarded branch publication, and handoff

## 2026-08-11 — Phase 12 release and publication tooling

Status: implementation/source PASS; clean-head execution pending checkpoint commit

- Added lab-local `codex-policy/Push-KingmakerBuffPlanner.ps1` with exact root, branch, clean-tree, unresolved-operation, single expected origin, prohibited payload, potential secret, size, current-branch-only push, and post-push SHA equality guards
- Added repository test `scripts/Test-GuardedPush.ps1`; its real WhatIf requires a clean tree and will run after this checkpoint
- Added `scripts/Build-Release.ps1`: clean-tree requirement, two deterministic local builds, package/DLL equality, release copy, allowlist validation, local-only manifest, and draft-notes copy
- Replaced future-tense README and added installation/use plus local-only draft release notes; no public release action is authorized or implemented
- PowerShell parser: 3/3 scripts zero errors; source suite: validation 15/15, behavior/protocol 51/51, transaction 6/6, package 4/4, deployment purity 5/5
- Rejected shortcut: direct `git push`; all publication must pass the lab-local project-specific helper
- External state: no game or deployment lock; no save accessed; no live mod mutation
- Exact next action: commit tooling/docs, run guarded-push WhatIf, produce and validate the deterministic clean-head release ZIP, then audit and execute final runtime gates

## 2026-08-11 — Phase 12 clean release and composed core scenario

Status: release tooling PASS; composed scenario source PASS; runtime pending checkpoint

- Guarded push helper WhatIf passed 6/6 from clean commit `4c4693b511627da0950ce470ac025f3f876bb244`, without changing HEAD/status or contacting/mutating the remote
- Clean-head release builder ran two identical builds: validated ZIP `artifacts/release/0.0.1/KingmakerBuffPlanner-0.0.1.zip` SHA-256 `2df128d74a08c0ce2dd77f7f3e784912fd291a75d02d9c9f47791619372844b5`, DLL `1904c5cb5c66999b64ebc2bbf3d4818778163553816bff7bde55326b965f6726`; publication status local-only
- Definition-of-Done audit confirmed that three separate no-save runtime gates should be composed for the final fresh-process repetition instead of counting packaging or a main-menu load
- Added `final-no-save-core`: exact identity and uniqueness, full catalog/expression reconciliation, Harmony inventory, UI singleton/reconstruction/layout/blocker/subscription checks, and normal transactional restoration in one process
- Added optional unique `-RunId`; evidence/transaction reuse is rejected before mutation. Source suite now passes validation 15/15, behavior/protocol 52/52, transaction 6/6, package 4/4, deployment purity 5/5
- Qualification boundary remains: the composed scenario is NO-SAVE and cannot prove the mandatory save-backed resource/effect/executor scenarios
- Exact next action: commit, build exact package, pass compatibility-specific WhatIf, and run the composed native core twice from fresh processes
