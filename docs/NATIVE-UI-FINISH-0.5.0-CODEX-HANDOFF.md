# Native UI finish 0.5.0 — handoff to Codex

Written 2026-10-11 ~13:30 UTC by Claude at the owner's request ("find a stopping
point, commit it, push it, and create a handoff document that will be taken over by
Codex"). Read `AGENTS.md` first; every rule there applies. No merge, tag or public
release is authorized.

## 1. What to read

| Record | Purpose |
|---|---|
| `docs/NATIVE-UI-FINISH-0.5.0-DELIVERY.md` | The delivery receipt for the tested candidate `4dbfa33`: per-ID and per-case statuses, identities, remaining issues |
| `planning/NATIVE-UI-FINISH-PROGRESS.md` | Feature table and the full run ledger (28 guarded runs, 3 FAIL, all restored, saves clean) |
| `docs/NATIVE-UI-FINISH-0.5.0-EVIDENCE.md` | Before/after captures with SHA-256; crops and `manifest.json` in `C:\Dev\KingmakerBuffPlannerLab\native-ui-finish-0.5.0-evidence\` |
| Mission packet | `C:\Dev\KingmakerBuffPlannerLab\handoff\kbp-native-ui-finish-2026-10-10-94ff3149\KBP_NATIVE_UI_FINISH_2026-10-10\` (`04_ACCEPTANCE_AND_EVIDENCE.md`, `templates/DELIVERY.md`, references R01–R15) |
| Triage page (owner's private artifact) | https://claude.ai/artifact/MtLzAwwxB3KXYffupNUSYz — the table in section 4 with current screenshots |

The owner's latest instruction (2026-10-11), in order: triage UI-01..06, UI-09 and UI-10;
keep the running gate on frozen inputs; keep provenance exact (records commits never
relabel a binary); implement the approved refinements, prioritising window switching,
the exact stepped HUD enclosure and the native spell popup; run cheap launcher/refusal
checks early; never stub the final gate; then freeze, package, run the final
qualification and report each UI ID with evidence. Additional run counts are not
progress on unresolved criteria.

## 2. State at handoff

- **Dev worktree:** `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-NUI`, branch
  `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10` (base main `a9c6d0f`, v0.4.3),
  pushed to origin through the guarded helper with this handoff.
- **Run worktree:** `...\repo\KingmakerBuffPlanner-NUIR`, detached at `4dbfa33`, clean;
  `artifacts\local-runtime\0.5.0` and `artifacts\release\0.5.0` hold the `4dbfa33` package.
- **Tested candidate (complete in-game evidence):** build commit `4dbfa33`, version 0.5.0,
  package/ZIP `cf658914130a010d7525ee47bc41a1f5c05f9bcde929852a1e31430ab3be8fcb` (local-runtime and
  standard ZIP byte-identical), DLL `b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69`,
  MVID `526e6663-b368-4e64-9afa-08f979dbcfe0`. Frozen at
  `runtime-backups\qualification-frozen\4dbfa33e1b36bf59bc9bb1a4ac3ebde860c52302\`; archived at
  `package-archive\0.5.0-native-ui-finish\`. Final runs on these bytes: `kbpnui-final-qual-07`
  (1920x1080), `kbpnui-final-qual-720-05` (1280x720), `kbpnui-final-cast-select-04`,
  `kbpnui-final-cast-03` — all PASS.
- **Records commit `3757758`** changes only `docs/`, `planning/` and the journal files; the package
  (DLL, `Info.json`, `NativeEffectOverrides.json`, `THIRD-PARTY-NOTICES.md`) is unaffected and stays
  labelled as built from `4dbfa33`.
- **Commit `57f136a` (product code, NOT in any package, NOT verified in game):** the source-tab
  caption fix (`UI/SourceTabLayout.cs`, `CastingWorkspaceScreenView.FitSourceTabs`, runtime seam
  `SourceTabFitForRuntime`, unit test `native-source-tabs-share-one-caption-size`). C# suite
  PASS=518 FAIL=0, Validate-Source 43/43 at `57f136a`. Any package built from here on is a new
  candidate (the DLL embeds its commit).
- **Complete gate on `4dbfa33`:** `Test-SourceOnly.ps1` started 2026-10-11T12:49:38Z in the run
  worktree; log
  `C:\Users\howar\AppData\Local\Temp\claude\C--Dev-KingmakerBuffPlannerLab-repo-KingmakerBuffPlanner\4ba182c1-b903-4132-8c08-b14cea864802\scratchpad\kbpnui-gate-4dbfa33.log`
  (its last line is `GATE-EXIT=<code>` then the end time when it finishes; ~6 h). At handoff
  source 43/43, protocol 517/517, harness filesystem 38/38, problem navigation 23/23, spellbook
  entry 26/26 had passed. **While it runs, write nothing under `runtime-evidence`,
  `runtime-state`, `runtime-staging`, `runtime-backups` or the game's `Mods`, and launch no
  runtime scenario** (its purity windows hash those). If the log never gets a `GATE-EXIT` line
  and no `powershell.exe` runs it, it was interrupted (e.g. this session closed): record it as
  interrupted, not as a result. Every gate step is WhatIf-only or uses temp folders, so nothing
  needs restoring; still check `runtime-state` for a transaction or lock newer than 12:49Z.
  If it passes, the `4dbfa33` package is a fully gated fallback.
- **First gate (on `cd1b554`) FAILED** after 6 h 4 min on a stale launcher meta-test expectation
  (log copied to `native-ui-finish-0.5.0-evidence\kbpnui-gate-cd1b554.log`); fixed in `4dbfa33`.
- **Statuses (receipt):** UI-07, UI-08, UI-11 PASS; UI-01..06, UI-09, UI-10 PARTIAL. Outcome PARTIAL.

## 3. Verified facts you can rely on

- HUD (UI-09) matches the owner's R12 silhouette in game: the frame rises over the four new
  cells, runs flat across them, steps down after the fourth column into the original rail; no
  doubled rail. Idle/hover/pressed highlight tints are judged against the native ColorTint
  (0.0 / (1,1,1,1) / (0.784,0.784,0.784,1)).
- The spell popup (UI-01) is the game's own `DescriptionWindow`; the same spell from the spellbook
  and from the planner renders identically (`PopupSameSpell=true`).
- Window switching (UI-02): seven routes per run (three HUD service-window routes, four key
  bindings incl. the journal) close the planner and restore it. The planner never touches
  `KeyboardAccess.Disabled`; `KeyboardAccess.Tick` dispatches only when no `TMP_InputField` is
  selected, the `Disabled` guard is off and no rebinding is in progress, then
  `OnCallbackByBinding` checks the game mode, pause (`WorksWhenPaused`) and the key.
- The planner canvas has no `CanvasScaler`: its text keeps pixel sizes while its layout is
  proportional, so 1280x720 is the stress case for text fit.

## 4. Remaining work (triage)

| ID | Remaining criterion | Kind | Smallest next action (design worked out) |
|---|---|---|---|
| UI-01 | Casting-card right-click (caster's own spell) not shown in game; long-description scrolling unproven (Resistance 539 of 650 units); physical right-click | New check; blocked | After the close-button step (opened-machine case 22) right-click `chip:<castingId>` (`view.RightClickForRuntime`), record presenter/title (`DescriptionTitle`, `NativeUiRecord.SameDescriptionTitle`), Escape. Then right-click each catalogue tile, read `DescriptionWindow.Scroll` content vs viewport heights; scroll the first that overflows (`verticalNormalizedPosition = 0`), capture, record the move. Judge presenter/title/plan unchanged. |
| UI-02 | Key routes call the binding callback; record the dispatch preconditions; a switch from a paused game; remapped/physical keys | New check; blocked | In `InvokeKeyBinding` record `IsInputFieldSelected` (private static), `Disabled` (`CountingGuard` field, `op_Implicit`), `NewBindingSelection()` (private instance), the binding's mode, key and `WorksWhenPaused`; add `KeyDispatchOpen` to `NativeUiWindowSwitch` and a violation for a closed key route. Add a route `paused:key:OpenInventory`: set `Game.Instance.IsPaused = true` with the planner open (the Space "Pause" binding has no FullScreenUi mode), switch, verify `PausedAfterClose == true`, then unpause. |
| UI-03 | Tab captions overran their tabs | Fix done in `57f136a`; verify | Record `view.SourceTabFitForRuntime` in the record; violation unless `fits=True` and size ≥ 10; check the captures at both sizes. |
| UI-04 | "Active · Will skip / Will recast", an unavailable recipient and card hover never captured | New check | Unavailable: from the probe, select each catalogue entry with its first usable caster/source until a target is `CastingGraphTargetLegality.Illegal`, capture, then restore the authored selection (Resistance feature, Linzi, its source; the planning script depends on it) and verify the plan signature unchanged. Active: in the casting-qualification host after `_qualificationDriver.Completed` and before `PublishQualificationRecord`, open the planner (`BuffPlannerUiRoot.HandlePlannerHotkey`), select the qual buff (`selection.sourceId`), capture, record labels containing "Active", close; start nothing. Hover: `Pointer(..., pointerEnterHandler)` on a `Casting.<id>` card, capture, exit. |
| UI-05 | Changed-caster state not captured; group/enhancement states | New check; fixture-limited | In the planning loop add `if (name == "change-caster") NativeUiCapture("nui-planning-changed-caster.png")`. Disabled "Cast earlier" is already in `nui-planner-casting.png` (link it). Group/enhancement need the Advanced fixture, which `live-workspace-native-ui` deliberately refuses (`c8a5a21`, launcher meta-test `advanced-native-ui`); keep as a limitation unless the owner widens that guard. |
| UI-06 | Filter tabs never pressed; row hover; list too short to scroll | New check; fixture-limited | Invoke `SourceTab.Spells/Abilities/Other/All` via `BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime`, record visible rows per tab (add a view seam counting active `Source.*` rows), capture each; hover one row. |
| UI-09 | Disabled state never captured | New check | The host update runs after the HUD's per-frame `RefreshAvailability`, and `ScreenCapture` grabs the end of that frame. After the pressed capture: for the four cells set `interactable = false` and invoke `Selectable.DoStateTransition(SelectionState.Disabled, true)` by reflection, record the highlight tint (expect (0.784,0.784,0.784,0)) and capture `nui-hud-disabled-planner.png` in the same update; next step verify the HUD re-enabled them. `f31fea9` failed only because it waited 600 ms first. |
| UI-10 | METAMAGIC's own action never pressed | New check | In the pre-open machine before the planner-entry press (case 7 → 8): find the native `Button` under `Container_Book/MetamagicButtonPlace` (BUFF PLANNER lives in its own `BuffPlannerButtonPlace`), `onClick.Invoke()`, check `SpellBookController.IsMetamagicMode` true and capture, press again, check false. |

Free step numbers: pre-open machine 12–15; opened machine 23 and up. Each new judgement goes
into `NativeUiRecord.QualifyViolations` with a defect case in
`TestNativeUiQualifyJudgement` (the launcher re-check only needs `violations` empty).

Fixture-limited or blocked regardless: physical mouse/keyboard (harness refuses synthetic input
while a console owns the foreground), remapped keys, group/enhancement states, long lists, the
owner's 1920x1200 display (the session display is 1920x1080 since `kbpnui-facts-07`).

## 5. Procedure to the next candidate

Run every script with Windows PowerShell 5.1 as
`env -u PSModulePath powershell.exe -NoProfile -NonInteractive -Command "& .\scripts\X.ps1 ..."`
(never `-File`), from the clean run worktree for runtime work.

1. Implement section 4 in the dev worktree; C# suite (`tools\run-tests.ps1`, or the tests exe),
   `scripts\Validate-Source.ps1`; commit.
2. If any launcher script (`Invoke-KingmakerRuntimeTest.ps1`, `RuntimeAutomation.Common.ps1`)
   changed: run the launcher meta-test fast first —
   `python tools\make_metatest_copy.py <scratch>\Test-RuntimeLauncherFileWhatIf.ps1` (it repoints
   `$PSScriptRoot` to the run worktree's scripts and stubs only the purity windows; the variable
   must sit after `param()`), run it with stdin closed (`< /dev/null`) after `Build-Local`, then
   `Test-FixtureInventoryEvidence`, `Test-RestoreInstallLocal`, `Test-PublishReleaseGate`. The
   complete gate keeps the real purity windows; never stub it.
3. After the gate on `4dbfa33` has exited: in the run worktree
   `git checkout --detach <commit>`, `scripts\Build-Local.ps1`, then
   `Invoke-KingmakerRuntimeTest.ps1 -Scenario live-workspace-native-ui -NativeUiStage qualify -CompatibilityProfileId full-user -TimeoutSeconds 900 -RunId <fresh> -Confirm:$false`
   and the same with `-DisplayMode windowed-1280x720`. Read `runtime-evidence\<run>\native-ui.json`
   (`violations`, `Failures`) and inspect the captures yourself.
4. Freeze the exact package: copy the local-runtime ZIP and its `.build-local.json` to
   `runtime-backups\qualification-frozen\<commit>\` and write `FREEZE.json` exclusively (see the
   existing ones for the schema). Then
   `-Scenario live-cast-qual-select -CompatibilityProfileId full-user -TimeoutSeconds 900 -RunId <sel>`,
   `scripts\New-KbpRunAllowance.ps1 -Kind qualification -RunId <cast> -SelectionRunId <sel> -ExpectedCommit <full sha> -FreezeKind qualification-frozen -ExecutionMode instant -ApprovedBy 'Howie Reith' -Authority 'Explicit native UI finish mission authorization (KBP_NATIVE_UI_FINISH_2026-10-10, runtime cases D and E): an already qualified deterministic casting scenario on the approved automation fixture to prove the changed game-log integration; no release authorization'`,
   `-Scenario live-cast-qual ... -RunId <cast> -QualificationAllowancePath C:\Dev\KingmakerBuffPlannerLab\approvals\<cast>.json`.
   Expect five runs and five `gameLogPlannerLines` in `qual-outcome.json`.
5. `scripts\Build-Release.ps1` (two deterministic builds; the ZIP must equal the tested package),
   archive it, run step 2's fast checks, then the complete `scripts\Test-SourceOnly.ps1` (~6 h).
6. Records: receipt per ID with actual evidence, ledger rows, evidence document
   (`tools\manifest.py` then `tools\evidence_doc.py`; `tools\crops.ps1` with `crops-final.json`
   retargeted), journal, resume. Records-only commits never rebuild or relabel the package.
7. Push only with `C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1 -RepositoryRoot <dev worktree>`
   (`-WhatIf` first). No merge or release.

## 6. Traps learned in this mission

- The DLL embeds the commit: any commit, even scripts-only, gives a new DLL; repeat the final
  runtime set on the bytes you deliver.
- `KeyboardAccess.GetBindingByName` returns a name's first binding, which its callback ignores in
  another game mode; pick the binding of `Game.Instance.CurrentMode`.
- A `Selectable` shows Pressed only while the pointer is inside it: enter before pressing.
- The HUD's `Tick` calls `RefreshAvailability` every frame.
- The description window clears `m_IsShowed` at once and deactivates after its animation; pace
  Escape presses (the planner's Escape counts it closed via `NativeDescriptionPresenter.IsOpen`).
- The casting qualification applies its refused steps through its own session, so they add no
  game-log line; refusal lines are proven by the native-UI qualify stage.
- Bash heredocs mangle backslashes: write Python/PowerShell edit scripts to files.
- Probe evidence crops: `tools\crops.ps1` (nearest-neighbour) — never edit originals.

Tools copied from Claude's scratchpad to
`C:\Dev\KingmakerBuffPlannerLab\native-ui-finish-0.5.0-evidence\tools\` (`build.ps1` and
`run-tests.ps1` hard-code the dev worktree; `il.ps1`/`inspect.ps1` read the installed game
assemblies).
