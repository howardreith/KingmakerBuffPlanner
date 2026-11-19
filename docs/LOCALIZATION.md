# Simplified Chinese support

The language control is in the mod's Unity Mod Manager settings. **Automatic** follows Kingmaker's active `zhCN` locale and uses English for every other locale. Before the game's localization pack is ready, Automatic shows English and checks again on update. **English** and **简体中文** select the planner language independently. Spell, item, spellbook, class, school and companion text comes from Kingmaker or the content provider. Third-party content can therefore appear in English in a Chinese game.

UMM stores the stable preference in its mod settings. A missing or unrecognized value resolves to Automatic. A switch waits for active casts, pending quick actions, focused input, composition, mouse interaction and modal operations. The casting workspace can rebuild its view around the same authoring session. A Classic or quick-controller view adopts the change when closed. A game-locale change waits for an open workspace to close so the next discovery snapshot supplies fresh names and the search index uses those names.

## Text ownership and checks

| Player surface | Implementation and catalog coverage | Verification |
|---|---|---|
| UMM settings, hotkey explanation, mode and diagnostic controls | `Main`, `PlannerSettingsText`; `settings.*` and diagnostic strings | Locale matrix, catalog audit, R03–R04 |
| Casting workspace, catalog search, enhancements, source, target and policy controls | `CastingWorkspaceScreenView`, `CastingWorkspaceSession` and partials, `CastingGraphModels`, `CastingWorkspaceModels`; workspace, authoring and graph messages | Plan/edit round trip, Unicode search, view models, R01–R05 |
| Counts, resources, empty states, tooltips, Undo and Clear | `CastingWorkspaceResources`, `WorkspaceFooterModels`, `CastingWorkspaceScreenView.ClearPlan` and tooltips | Count and confirmation tests, resource table and modal contracts, R05 |
| HUD, spellbook entry and controller routes | `BuffPlannerHudButtonController`, `BuffPlannerSpellbookEntryController`, controller integrations, `ControllerRoutineModel`, `ControllerWorkspaceNavigator` | Entry/control wiring and native-UI contracts, R01–R05 |
| Application, execution and visible game log | `CastingRunPresentation`, `BuffPlannerUiRoot`, `CastingWorkspaceSession`; progress, counts, withheld casts, skips and refusal details | Native-log and execution fixtures. Historical English log readers require separate observation for Chinese |
| Save/load, legacy import, migration and failures | `PersistenceMessages`, session presentation and reason mappings; file recovery, review and refusal messages | Corruption, newer-format refusal, backup recovery and exact persisted-plan comparison |
| Retained Classic, routine execution and description cards | `BuffPlannerScreenView`, `PlannerViews`, `PlannerSetupModel`, `PlannerUiSession`, `PlannerPresentationModels`, `AssignmentPresentationModels`, `NativeDescriptionPresenter` | Classic view models, session/run and native description contracts, R05 |

Keep object and control names, Harmony and protocol identifiers, scenario evidence fields, blueprint and provider IDs, JSON members, enum tokens and reason codes stable. Translate where a result becomes a player message. Diagnostic logs and technical exception details retain their original wording. Existing plan/routine names are literals, including names such as “Long”. Newly created built-in captions carry an optional `hasDefaultCaption` bit so they can change language after reload. Format 6.6 identifies that addition to strict readers. Documents with only literal names continue to use 6.5 after editing. Switching language never writes a plan.

## Contributing catalog changes

The UTF-8 files [enGB.json](../src/KingmakerBuffPlanner/Localization/enGB.json) and [zhCN.json](../src/KingmakerBuffPlanner/Localization/zhCN.json) are embedded in the mod DLL. Add a semantic key to both files and pass typed values to one formatted message. Preserve positional placeholder multiplicity and supported rich-text tags. The catalog loader checks values once and falls back to English for an invalid Chinese entry. The package validator reads the two actual manifest resources and compares each with its source. The runtime uses the game's Source Han Serif SC assets and a planner-owned TMP fallback chain. No font distribution or network service is required.

Run `scripts/Test-LocalizationCatalogs.ps1` from the project root for computed and literal key coverage. Run `scripts/Test-Development.ps1 -Only 'localization-,duration-'` for behavioral and assembly-backed tests. The primary English edits move word order and counts into complete messages. The display layer supplies explanatory text around otherwise unknown technical reasons.

## Glossary and player review

| English | 简体中文 | Distinction |
|---|---|---|
| plan / routine | 计划 / 施法序列 | A plan contains routines such as 长效、重要、短效 |
| casting / caster / recipient | 施法条目（动作称为施法）/ 施法者 / 受术者 | An authored entry, the unit performing it, and the unit receiving the effect |
| spell source / spell slot / charge | 法术来源 / 法术位 / 充能（物品也使用游戏内的“发数”） | Keep source identity separate from spendable uses |
| remaining duration | 剩余持续时间 | Time left on the active effect |
| skip / recast / overwrite | 跳过 / 重新施放 / 覆盖 | Skip preserves an effect, recast describes a new cast, overwrite names the selected policy |

The translation received internal review. Installed Chinese strings informed terminology including 法术位、施法者等级 and 法术延时. Native-player review is still welcome for 施法序列, 受术者 and context-specific charge wording.

**中文玩家检查步骤**

1. 在 UMM 中选择“跟随游戏”，用中文游戏打开规划器。搜索游戏中实际显示的法术名，例如队伍拥有的“法师护甲”。检查法术说明、队友姓名和来源。
2. 在英文游戏中将规划器设为“简体中文”。从 HUD 和法术书打开，检查标题、资源表、完整提示、“清空计划”和“撤销”。再将中文游戏中的规划器设为 English，确认游戏提供的名称仍为中文。
3. 使用中文输入法实际组词，分别试选词、回车确认、Esc 取消、方向键、删除、粘贴和清空。记录任何意外关闭、施法或触发游戏快捷键的情况。粘贴和输入法组词分开记录。
4. 在可命名的位置使用中英文混合名称，保存并重开。切换语言后只进行一次有意的编辑，比较名称、序列顺序、目标与来源。用手柄打开相同页面，并在窄窗口检查长提示和滚动。

## Duration contract

`KingmakerDurationAdapter` inspects the concrete source's application graph and the effective caster level. It returns **base, unextended** rounds and extendability. Supported leaves include direct buff and equipped-item enchantment actions, native fixed seconds and deterministic duration values. Supported ranks use the native caster-level progression and clamps. A conditional requires both branches to prove the same estimate. Multiple leaves must agree. Unsupported delivery actions, mixed branches, random dice, unsupported ranks and bounded traversal exhaustion yield **unknown**. Explicit permanent effects are distinct. The adapter never casts the ability or reads localized duration prose.

Identity, recipient, strength, caster level, suppression and metamagic checks still apply. An active effect under two rounds fails. For a proven per-level duration the active instance must retain at least half the planned duration. Fixed or unprovable estimates do not enable that percentage rule. The sufficiency check and the “already covered longer” observation use the same final estimate. The source and rod Extend bits are combined once. The independent maximum-duration calculation for transient confirmation markers is unchanged.

## Candidate and evidence

**Current frozen candidate 0.6.17** · Product and launcher source `d6e341fcc4951764f7b650cef481014507ef8e98`. Base `639b5d2a3119efa2d2db9323f663d6aa69cd1d65`. The package and manifest are in `KBP-ZHCN-InputReview/artifacts/local-runtime/0.6.17/`, frozen on `codex/kingmaker-buff-planner-zhcn-0617-candidate`. Later acceptance records live on the PR branch. The ZIP and staging DLL were rehashed on resumption and matched the manifest.

| 0.6.17 artifact | Identity |
|---|---|
| DLL SHA-256 | `6e900b4bcab34afb7a8badad413ec74ef8fb25e4d13c28bde301e526d4eb5329` |
| ZIP SHA-256 | `d8e4c8a561749e653a8e814fc026b8770e881f83b699a121aca9e0918b63ace8` |
| Assembly MVID | `861fd6cd-c084-4734-8285-7f20feec727d` |

The 0.6.17 request reader admits the launcher's two optional locale fields for controlled native UI runs. It checks supported values, screen and scenario scope, and continues to reject extra fields. The host compares both observed languages for Automatic and manual fixtures.

| Check | 0.6.17 result and receipt |
|---|---|
| Focused behavior | `Test-Development.ps1 -Only 'native-ui-scenario-admission,localization-,controller-workspace,clear-plan-view,casting-workspace-persistence-round-trip'` passed 39/39 on the product changes before the freeze (6.2 seconds, console receipt `c2e5e3`). The request reproduction initially failed 0/1 with `invalid-request:live-save-parameters`. |
| Build and archive | Source 47/47, build 1/1, package validation 8/8 and local package 1/1. See `artifacts/zhcn17-build.log` in the frozen worktree. |
| Packaged DLL and catalogs | `Test-LocalizationSettings.ps1` against the staged DLL passed 6/6. `Test-LocalizationCatalogs.ps1` found 1,082 entries in each language and 1,079 lookup sites with zero errors. Captured in invocation 71491, console receipt `41a078`. |
| Pre-live gate | Recovered the original `Test-SourceOnly.ps1` invocation 71491, exit 0. `artifacts/zhcn17-source.log` ends with aggregate 1/1 and independently records 746/746 C# tests for **0.6.17**. Launcher argument cases 40, source 47, filesystem harness 38, physical search 20, navigation 35, spellbook 50, deployment purity 5, launcher file purity 13, fixture inventory 3, install rollback 16 and publisher gate 8 all passed. |

### Historical 0.6.16 evidence

The guarded English/Automatic run `zhcn16-en-auto-02` loaded the 0.6.16 DLL and rejected the request with `invalid-request:live-save-parameters` before the workspace opened. The game exited normally. The launcher's completion receipt records `complete=false`, `gameResultStatus=none`, `kingmakerExited=true`, `restorationVerified=true`, and `protectedSavesClean=true`. The protected-save comparison found zero changes or violations. See `runtime-evidence/zhcn16-en-auto-02/{orchestration,run-completion,protected-saves}.json` in the lab and `KBP-ZHCN-Review/artifacts/zhcn16-en-auto-02.log` for the display, registry and installation rollback.

The historical 0.6.16 candidate came from `4bf737406060200661556452588308baeeef4851`, following reviewed PR head `cfac4bce02274b4f0e7f3ecb0f4aaf2659759c40` on the same base. The original 0.6.15 package and evidence remain in KBP-ZHCN. Its frozen worktree is KBP-ZHCN-Review on branch `codex/kingmaker-buff-planner-zhcn-input-candidate`. The review branch is `codex/kingmaker-buff-planner-zhcn-localization`.

| 0.6.16 artifact | Identity |
|---|---|
| DLL SHA-256 | `c61bd0a3b656059e78b0781fbe5906a8e8837aca821406a244c7da14416d7ef6` |
| Local runtime ZIP SHA-256 | `3750e4cac9f447d59ec33bbefcb69058014a588158a23c1421fb7acead5426c9` |
| Assembly MVID | `44647b34-1720-424d-ba49-1e17d3b65191` |
| Package and build manifest | `artifacts/local-runtime/0.6.16/` in KBP-ZHCN-Review |

During a language refresh the old workspace releases keyboard navigation before the replacement captures it. The replacement retains the authoring session and restores its search and category. Construction or presentation failure closes the failed view and releases the workspace input lease. Rewired command capture and dispatch both discard pending input during owned IME preedit and its grace frame. Confirm, Inspect and both routine-switch commands require a subsequent neutral frame before another press can act.

**Corrected-candidate offline verification.** Eight new behavior regressions exercise initially enabled and disabled native navigation, three successive replacements and close, failure during construction or presentation restoration, and all four controller commands across preedit, release, grace and rearming. They use the shipped lifecycle, installed EventSystem property and production command methods with detached UI and frame inputs. Focused selection passed 38/38 and the complete C# suite passed 746/746. Installed UMM settings passed 6/6. Package validation passed 8/8, including both catalogs. The audit resolved 1,079 lookup sites and all 1,082 keys per catalog with zero errors.

Evidence is local to KBP-ZHCN-Review under `artifacts/zhcn16-focused.log`, `zhcn16-source.log`, `zhcn16-build.log`, `zhcn16-umm.log`, `zhcn16-catalog.json`, `zhcn16-preflight.json` and `zhcn16-ime.json`. Commands include `Test-Development.ps1 -Only 'localization-,controller-workspace,clear-plan-view,casting-workspace-persistence-round-trip'`, `Build-Local.ps1`, `Test-SourceOnly.ps1`, `Test-LocalizationCatalogs.ps1` and `Test-LocalizationSettings.ps1 -CandidateDll artifacts/local-runtime/0.6.16/staging/KingmakerBuffPlanner/KingmakerBuffPlanner.dll`. The package is reused across downstream checks.

The corrected candidate's full pre-live aggregate passed 1/0. It includes 40 launcher argument cases, 47 source checks, the 746-test C# suite, 38 filesystem harness cases, 20 physical-search, 35 problem-navigation and 50 spellbook-entry evidence cases, 5 deployment purity checks, 14 child-argument checks, 13 launcher purity checks, 3 fixture inventory, 16 rollback and 8 publisher-gate checks. All reported zero failures. The first English admission attempt (`zhcn16-en-auto-01`) was interrupted after creating an empty evidence directory and before entering a staging transaction. There was no game process, lock, active candidate sentinel or transaction record on resumption.

### Historical 0.6.15 evidence

Base `639b5d2a3119efa2d2db9323f663d6aa69cd1d65`, frozen 0.6.15 source `b48e9ebebcff8747f8b5e76dd479fd0d91ebab28`. The PR branch is `codex/kingmaker-buff-planner-zhcn-localization`. The candidate worktree stays on the same source under `codex/kingmaker-buff-planner-zhcn-candidate`.

| Artifact | Identity |
|---|---|
| KingmakerBuffPlanner.dll | SHA-256 `2d58c34b5c14cfae8f61b704ff10c34a235da6622e10e0475cd39b54984fd316`, MVID `a13472e5-c19d-42e0-9668-a1f43264ffc6` |
| KingmakerBuffPlanner-0.6.15-local-runtime.zip | SHA-256 `769978fed783138309098752885fe7a03133dc79dbf66f58cc2c9aecb453a199` |
| Assembly-CSharp.dll | Installed Kingmaker 2.1.7b, SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb` |
| Automation fixture | BASELINE `f13de02d7c59a70a34d335a413f0d90fc7e63cdcb327224ecf5885ed6e1f8300`, WORKING at preflight `c567fd46c79482df5c45fc4ef79173ce47a1e448912a307f13b867e0359af0af` |

Local evidence paths are relative to the frozen KBP-ZHCN worktree. The package and its build manifest are in `artifacts/local-runtime/0.6.15/`. The catalog receipt is `artifacts/zhcn-catalog-audit.json` and the pre-live transcript is `artifacts/zhcn-source-only.log`. The post-stop receipt `artifacts/zhcn-stop-evidence.json` records enGB, zero game processes, an absent deployment lock and sentinel, and the unchanged hashes of both fixtures.

| Checks | Result |
|---|---|
| L01–L03 | Locale matrix including startup, invalid and unsupported values, malformed optional catalogs, placeholder, markup, mapped reasons and count fixtures passed. Static audit covered all 1,082 keys per language across 1,079 lookup sites, including 19 computed sites |
| L04–L06 | Presentation invalidation, Unicode/native-name filtering and the English/Chinese/edit/save/reload/English fixture passed. Byte-level comparison before editing and JSON comparison after editing preserved unrelated fields |
| D01–D05 | Assembly-backed structural estimates, English/Chinese/blank duration parity, readiness, resource demand, forecast contract, two-round and half-duration thresholds, source/rod overlap, permanent/fixed/unknown/mixed graphs and existing-effect/transient fixtures passed |
| Build and offline | 47 source checks, one build, eight archive checks, one local package, 40 launcher argument cases, 62 focused tests and 738 full offline tests passed with zero failures in each. The 38 filesystem harness, 20 physical-search, 35 problem-navigation and 50 spellbook-entry reader cases passed |
| Installed UMM | Six isolated checks against the packaged DLL and installed UMM passed, including the actual Settings.xml path, freshly deserialized saved preferences, invalid value fallback without a rewrite, and the guarded startup XML fixture |
| Pre-live guards | Deployment WhatIf purity 5/0 and launcher child-argument checks 14/0 passed. The source-only aggregate was stopped before its non-WhatIf real-launch refusal case when an unrelated Steam launch window was observed. The full launcher purity receipt is pending. Standalone isolated fixture evidence 3/0, install rollback 16/0, publisher-gate tests 8/0 and display transaction 10/0 passed afterward |
| Live | **BLOCKED.** Steam displayed an unrelated “Launching...” window alongside Civilization V and a World Builder error dialog. The repository stop condition applied before staging. There were zero Kingmaker processes, no deployment lock and no candidate transaction. No game-language, game-save or installation changes were made. Asset inspection covered all 539 distinct Han characters in the catalog. Rendered glyphs, layout, clipboard input and IME composition still need observations |

Commands used for the candidate and focused regressions:

```powershell
.\scripts\Test-Development.ps1 -Only 'localization-,duration-,rollback-candidate,quick-result-has-no-floating,focused-casting-routine-order,planners-show-unusable,provider-choices-name,classic-run-advances,planner-mode-is-stated,planner-escape-never,spellbook-entry-pairs,direct-manipulation-sidebar,human-feedback-native-resources,presentation-view-models,clear-plan,run-presentation,native-ui-game-log,controller-workspace,casting-workspace-persistence-round-trip,live-existing-effect,cast-available-migration,infectious-charms-transient'
.\scripts\Test-LocalizationCatalogs.ps1
.\scripts\Test-LocalizationSettings.ps1 -CandidateDll .\artifacts\local-runtime\0.6.15\staging\KingmakerBuffPlanner\KingmakerBuffPlanner.dll
.\scripts\Test-RuntimeLauncherArguments.ps1
.\scripts\Build-Local.ps1
.\scripts\Test-SourceOnly.ps1
```

## Runtime acceptance

The 0.6.17 pre-live suite passed. Run from the frozen worktree in Windows PowerShell 5.1. Each observation requires a unique run ID. Inspect the actual `gameLocale`, `plannerLanguage`, captured frames, loaded WORKING identity and transaction restoration records. The native facts probe does not cast. Only a separately admitted casting scenario can qualify gameplay.

Before 0.6.16 admission the previous unrelated launch window had cleared. The current-session Steam safety check passed. The physical input inventory found US English (0409:00000409) and no matching game controller. The BASELINE and WORKING hashes matched the values recorded above.

```powershell
$facts = @{ Scenario='live-workspace-native-ui'; NativeUiStage='facts'; DisplayMode='windowed-1280x720'; FixtureFamily='Automation'; CompatibilityProfileId='full-user'; TimeoutSeconds=900 }
& .\scripts\Invoke-KingmakerRuntimeTest.ps1 @facts -RunId zhcn-en-auto-01 -AcceptanceGameLocale enGB -Confirm:$false
& .\scripts\Invoke-KingmakerRuntimeTest.ps1 @facts -RunId zhcn-zh-auto-01 -AcceptanceGameLocale zhCN -Confirm:$false
& .\scripts\Invoke-KingmakerRuntimeTest.ps1 @facts -RunId zhcn-en-zh-01 -AcceptanceGameLocale enGB -AcceptancePlannerLanguage zhCN -Confirm:$false
& .\scripts\Invoke-KingmakerRuntimeTest.ps1 @facts -RunId zhcn-zh-en-01 -AcceptanceGameLocale zhCN -AcceptancePlannerLanguage enGB -Confirm:$false
```

R01–R05 require rendered screens and direct operation, including forced-Chinese UMM controls and the HUD's native tooltip under English startup. R06 additionally requires a Chinese IME and actual controller checks for R05 require a connected controller. At baseline Windows reported only US English input and no matching controller. R07 needs a verified normal and Extend-capable provider in WORKING, a fresh guarded selection and run-bound casting allowance for each authorized execution. Historical inventory of this WORKING fixture had no Extend source or rod. R08 requires signed-off restoration evidence for every transaction, including the pre-run game registry for controlled startups. Never mutate BASELINE or another save to prepare a test.

| ID | 0.6.17 status and observation still required |
|---|---|
| R01 | **PENDING.** Open from the English HUD and spellbook in Automatic. Inspect the plan, change selection and close normally |
| R02 | **PENDING.** Start in zhCN with Automatic. Inspect actual native names and rendered planner labels, search a party provider by its Chinese name, then make a representative allowance-bound cast |
| R03 | **PENDING.** Start in enGB with the forced zhCN fixture. Inspect UMM, TMP, uGUI, editable fields and the native HUD tooltip. Check English native content alongside Chinese planner text |
| R04 | **PENDING.** Start in zhCN with the English override. Change the preference at a safe boundary, reopen and restart with the saved preference. Compare plan and assignment identities |
| R05 | **PENDING.** At 1280×720 and the normal supported layout inspect resources, status, Clear/Undo, long tooltips and scroll limits. Repeat the controller routes with a connected controller |
| R06 | **PENDING.** Use a Chinese IME for real candidate selection, commit, cancellation and focus transitions. Exercise paste, edit and clear separately. Save/reload a Unicode name where supported |
| R07 | **PENDING.** Use `live-cast-qual-select` with `-QualificationRecipe rod-extend-direct` to establish current WORKING capability. Prepare an approved WORKING-only fixture if absent. With run-specific admission, compare instant and animated normal/extended skips, recasts and resource counts across languages |
| R08 | **Per transaction.** Restoration passed for the failed 0.6.16 run above. Verify game registry, normal Mods and protected saves for each 0.6.17 run |

The English-duration parser and the maximum transient lifetime were both rejected as mechanical estimators. A UI source assertion that required an inline English caption was updated to check the new binding and its actual English output. An early full offline run found ten such outdated checks. A subsequent run found a test naming supported revision 6 as “future”. The fixture now uses the next unsupported revision. The final 738/0 result supersedes those offline failures. Native rendering, IME safety and native gameplay outcomes remain unverified on these bytes.
