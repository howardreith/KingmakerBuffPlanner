# Native UI development checkpoint - October 11, 2026

Development checkpoint only. [VALIDATION-POLICY.md](VALIDATION-POLICY.md) governs;
final qualification and release work are deliberately deferred. Receipt remains
PARTIAL: UI-07, UI-08 and UI-11 retain their original PASS attribution; all other
UI criteria retain PARTIAL. New observations below have not run in game.

## Checkout and effective instructions

- Host DATA; lab `C:\Dev\KingmakerBuffPlannerLab`.
- Active development worktree `repo\KingmakerBuffPlanner-NUI`; branch
  `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10`.
- Accepted handoff HEAD `fa5f4a277558c998255b74d839eb831850c3ee29`.
- Fetched origin/main `358418f6f86ef0c34687a2f4b96374042c7393d2`, containing PR #9
  and repair `f658726d215d59d41d3a950a90be953653e5d96f`.
- Workflow merge `0463f553a859ecc195f862fba9632ee91586e08c`; source/observer HEAD
  `a652441b07bb1f427c42e3036b10e67d8e9163d1`. Later records commits do not change
  these source bytes or qualify a package.
- CODEX_HOME unset; effective home `C:\Users\howar\.codex`. Read its actual
  config, with no home AGENTS/override or ancestor/nested override found.
  The checked-in home template was not installed. Initial sandbox fetch refused
  FETCH_HEAD access; the authorized escalated fetch succeeded.
- Existing NUI and NUIR checkouts were clean. Other worktrees and unrelated DATA
  projects were preserved. The merge updated only the feature branch.

## Workflow integration

The four merge conflicts were resolved deliberately. The early display guard
retains `KBP_DISPLAY_MODE_SCENARIO` and now admits native UI windowed runs.
Native UI requires an explicit facts/qualify stage, its approved Automation
fixture/profile, and the existing minimum 600-second timeout before imports,
source/package validation or expensive purity work. Misplaced stages are refused.
These early comparisons accept supported casing; later canonicalization remains.
Existing downstream ownership, fixture, source, WhatIf, lock, allowance, package,
controlled staging, save protection, rollback and restoration checks remain.

The broad FileWhatIf test still invokes the cheap argument checks and retains
native stage and Advanced-fixture cases. Its stale display-message expectation
was replaced by the stable-ID checks in the cheap runner. No purity helper was
stubbed or used as the development workflow. The current handoff/resume explicitly
supersede the old automatic six-hour sequence; original resume chronology is
[archived intact](history/NATIVE-UI-FINISH-RESUME-at-Claude-handoff-2026-10-11.md).

## Old gate audit and preserved fallback

The one-time audit at 2026-10-11T16:08Z found Test-SourceOnly ACTIVE as PID 16712,
created 2026-10-11T12:49:38.1955690Z, on frozen source
`4dbfa33e1b36bf59bc9bb1a4ac3ebde860c52302` in NUIR. No GATE-EXIT line was present;
this is neither PASS nor INTERRUPTED. Log length 15197 bytes, last write
2026-10-11T14:33:28.9347261Z, at the original path in the
[Claude handoff](NATIVE-UI-FINISH-0.5.0-CODEX-HANDOFF.md). Last observed counts:
source 43/0, C# 517/0, harness filesystem 38/0, problem reader 23/0, spellbook
reader 26/0, package 4/0, deployment WhatIf 5/0. None is the aggregate exit result.

No runtime lock files were present; all 495 transaction status records were
Restored. The other 385 JSON files were snapshots without transaction status.
No process was stopped, lock removed, runtime directory written, save changed or
native scenario launched. NUIR and its frozen artifacts stay at 4dbfa33. Useful
source work continued independently; no repeated gate polling was selected.

Rehashed both local-runtime and release ZIPs:
`cf658914130a010d7525ee47bc41a1f5c05f9bcde929852a1e31430ab3be8fcb`;
packaged DLL `b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69`;
version 0.5.0, MVID `526e6663-b368-4e64-9afa-08f979dbcfe0`. Four original final
runs were read and verified: `kbpnui-final-qual-07`, `kbpnui-final-qual-720-05`,
`kbpnui-final-cast-select-04`, `kbpnui-final-cast-03`. Each reports loaded commit
4dbfa33, the matching DLL/package, game PASS, completed orchestration, verified
restoration and clean protected saves. Full-user profile identity
`c9a1664ca57e1bbe8b7b9f193a4f4633bd80498bdfcb2a636aa6a37ea110ca43`;
Automation baseline `f13de02d7c59a70a34d335a413f0d90fc7e63cdcb327224ecf5885ed6e1f8300`,
working `eded233c784fd658ddf2c46b2b54d5a310c39bff62506f0324b0f35c045bbec7`.
Those are the runs' recorded environment identities, not a new fixture receipt.

3757758 changes records only. 57f136a fixes product caption layout; it and the new
observer are absent from the fallback package. No fallback artifact was rebuilt
or relabelled. Gate completion remains unobserved at this checkpoint.

## Focused UI triage and implementation

Reviewed the design/acceptance specification, all R01-R15 through a hash-verified
local contact sheet, and the original popup and 720p planner captures. Reference
originals and runtime evidence were preserved. No new visual PASS is inferred.

| ID | Classification and action | Remaining observation |
| --- | --- | --- |
| UI-01 | Missing casting-card observation. Added its existing right-click callback, actual resolved caster/ability identity, native title/presenter, capture, Escape and unchanged plan signature. | Run on new bytes; physical dispatch and genuine overflowing descriptions still absent. |
| UI-02 | Existing observations need precise limits: seven callback/HUD routes already recorded; they do not prove keyboard dispatch. Window lifecycle neighbors pass offline. | Paused switch, dispatch preconditions, remapping and physical keys. |
| UI-03 | Actual crowded-caption defect fixed in 57f136a. Existing caption regression passes; observer now records actual TMP widths/rooms and shared sizes, rejecting clipping or size below 10. | Inspect the fix rendered at both comparison sizes and larger UI scale. |
| UI-04 | Missing state observations, not a demonstrated new product defect. | Genuine active skip/recast, unavailable and hover states. An active-state check after guarded casting is an option, not an automatic casting campaign. |
| UI-05 | Changed-caster behavior already checked by the planning script; added its missing capture. Existing disabled Cast earlier reference is `kbpnui-final-qual-07/nui-planner-casting.png` (also 720-05). | New capture; groups, enhancement scope and alternative providers need an applicable guarded fixture/manual case. |
| UI-06 | Filter tabs were not invoked. Added all four existing controls, captures, actual active row IDs vs expected filter membership, and unchanged plan signature; return to All. Selected entries intentionally remain listed. | Run on new bytes; row hover and genuine long-list scrolling remain unobserved. |
| UI-09 | Existing stepped enclosure, idle/hover/pressed evidence retained. Rejected forced `interactable=false` as proof of the availability condition. | Observe genuine `_session.IsExecuting` disabling and independent native controls, plus rendered disabled treatment. |
| UI-10 | Missing Metamagic action observation; existing pair layout retained. | Native Metamagic toggle and independent planner action, including unavailable Metamagic. |

Native UI records now emit schema 2. Schema 1 retains its original judgement;
missing schema-2 observations cannot be inferred from old PASS. The new checker
refuses incorrect popup context, title, dismissal or plan changes; missing/invalid
caption measurements; and missing filters, incorrect row identities or plan edits.
Seventeen negative mutations plus legacy/current-schema checks extend the existing
behaviour test. Observations reuse existing controls and read-only seams; no forced
availability or fabricated long content was added.

## Commands, costs and limits

All checks used supervised Windows PowerShell 5.1 via `-NoProfile -NonInteractive
-ExecutionPolicy Bypass -Command`, from NUI. Logs are under the worktree's
`artifacts/native-ui-codex-checkpoint/`, outside watched lab runtime paths.

| Actual command/check | Result | Elapsed |
| --- | --- | --- |
| `scripts/Test-RuntimeLauncherArguments.ps1` | 24 PASS, 0 FAIL; positive/negative, stage, casing, fixture and timeout checks before external import | 0.583 s |
| `scripts/Test-DevelopmentSelector.ps1` | 6 PASS, 0 FAIL; bounded selector/environment/zero-match checks | 1.026 s |
| Parser on launcher, FileWhatIf, arguments, selector, selector tests and source aggregate | 6 syntax checks PASS, 0 FAIL | part of the 1.34 s supervision invocation |
| `scripts/Test-Development.ps1 -Only 'native-ui,native-source-tabs,native-window,presentation-view-models,spellbook-handoff,catalog-filter-selected-category'` | Source 43/0, production/test builds PASS, selected C# 12 PASS, 0 FAIL | wrapper 4.8 s; supervision 5.028 s |
| Same selection before the fixture edit had applied | Source/build PASS; selected C# 11 PASS, 1 FAIL (old synthetic record lacked the new required observations) | 11.117 s; preserved as `development-incomplete-fixture.log` |
| Installed contract inspection (read-only helper, inspected before use) | Verified AbilityData.Caster = UnitDescriptor and UnitDescriptor.Unit = UnitEntityData | 1.560 s |
| `scripts/Test-GuardedPush.ps1` | 6 PASS, 0 FAIL; no writes | 2.210 s |
| Original reference image hashes | 15 PASS, 0 FAIL | 1.021 s with local contact-sheet rendering |
| Changed/new Markdown link checks | 17 PASS, 0 FAIL | part of the 0.417 s text-check invocation |
| Active policy/conflict text checks | 5 PASS, 0 FAIL | same text-check invocation |

This is focused offline validation. No full C# aggregate, source-only aggregate,
live-lab purity, restore-install, publisher, reproducibility, layout matrix,
casting run or final frozen-candidate campaign was launched. Ordinary validation
stayed within the 15-minute target and 30-minute ceiling. The optional Python
contact-sheet helper lacked Pillow; native System.Drawing produced the sheet
without installing a dependency or modifying originals.

The unpackaged development DLL was built on the reviewed observer diff at stamped
Git HEAD `0463f553a859ecc195f862fba9632ee91586e08c`; the same source patch was then
committed as `a652441b07bb1f427c42e3036b10e67d8e9163d1`.
DLL SHA-256 `b653c6795c4c876870e3e7656df7dc49640d754f36354b55b6f94c1e62d0f2bd`,
MVID `9d36fb59-9656-4d32-ad29-f94a65515e43`, version 0.5.0. It is not packaged or
native-qualified. Documentation commits do not require rebuilding it. Before
any native run, build a clean candidate through the guarded route and attribute
observations to its actual bytes. Local audit/receipt hashes are retained in
`handoff-audit.json`; final log hashes are retained in `validation-manifest.json`.

Next action: inspect the exact key-dispatch and Metamagic contracts, then implement
a paused-window switch and native Metamagic action observation with focused
`native-window`, `native-ui-record` and `spellbook-handoff` checks. At a later
explicit runtime boundary, establish that the old gate has exited and no live
transaction conflicts, then select the smallest two-size native UI observation
batch on a new clean candidate under all unchanged live prerequisites. Preserve
NUIR/fallback and retain the short manual/fixture list in
[MANUAL-ACCEPTANCE.md](MANUAL-ACCEPTANCE.md). Do not restart the historical campaign.
