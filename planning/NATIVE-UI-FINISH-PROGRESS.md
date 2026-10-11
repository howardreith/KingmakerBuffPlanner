# Buff Planner native UI finish — progress

Mission packet: `KBP_NATIVE_UI_FINISH_2026-10-10` (owner ZIP
`C:\Dev\KingmakerBuffPlannerLab\KBP_Native_UI_Finish_Mission_2026-10-10.zip`,
SHA-256 `94ff31499bc374859366da2deb9cb7908c67bea96479b19cf5d86950e41a0470`),
extracted read-only to
`C:\Dev\KingmakerBuffPlannerLab\handoff\kbp-native-ui-finish-2026-10-10-94ff3149\`;
`scripts/verify_packet.py` PASS (30 files against MANIFEST.json). The packet
stays reference material; this file follows its `templates/PROGRESS.md`.

Updated: 2026-10-11 02:30 -04:00

## Current review correction - October 11, 2026

DATA/NUI, same feature branch, clean built source HEAD
`673d1e3a62356af3d1b4d5dcbd19f233fd521734`, version 0.5.0. Launcher consumer fix
`3af7171a70f95ef13da3359245d00b030bc62843` preserves the producer/direct checks;
shared real -File assertion checks 8/0 (1.542 s), original direct checks 24/0
(0.263 s), syntax 3/0; affected development selection source 43/0, builds PASS,
C# 6/0 (7.712 s); guarded push 6/0 (2.042 s). Native Metamagic and key-route
measurements advance UI-02/UI-10 observation readiness, with schema 3 preserving
schemas 1/2. New observations NOT_RUN; receipt remains PARTIAL. Development DLL
SHA-256 `ca3d6bc5a4a674397e1baab60601c972dc008086d93c247c7451ca97fb8a7250`,
MVID `a956e985-a762-4994-84e6-07f586a947ec`; no package or native qualification.
Frozen fallback/gate paths were not changed or polled. Precise manual/fixture
limits and next guarded observation action are in the
[current review checkpoint](../docs/NATIVE-UI-FINISH-0.5.0-REVIEW-CHECKPOINT.md).

## Previous Codex development checkpoint - October 11, 2026, 16:32 UTC

- DATA/NUI, branch `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10`,
  source HEAD `a652441b07bb1f427c42e3036b10e67d8e9163d1`, version 0.5.0.
  Main PR #9 merged at `0463f553a859ecc195f862fba9632ee91586e08c`.
- [Current policy](../docs/VALIDATION-POLICY.md) supersedes broad automatic
  sequences below. This is an ordinary reviewable checkpoint.
- Gate on frozen 4dbfa33 was ACTIVE at the one-time 16:08Z audit (PID 16712,
  no GATE-EXIT). Preserve NUIR and watched runtime paths; no cancellation,
  new runtime scenario or broad gate selected.
- Launcher arguments 24/0 (0.583 s), selector 6/0 (1.026 s), PowerShell syntax 6/0;
  Test-Development affected prefixes: source 43/0, builds PASS, C# 12/0
  (5.028 s supervision). First incomplete-fixture pass 11/1 retained (11.117 s).
  Guarded-push helper 6/0 (2.210 s); reference hashes 15/0.
- UI-01, UI-03, UI-05 and UI-06: targeted observations prepared and checked
  offline; NOT_RUN in game. No per-ID acceptance promotion. Native schema 2
  retains schema-1 verdict provenance. Product caption fix 57f136a stays in source.
- Fallback 4dbfa33/package cf658914/DLL b32f320e preserved; all four original
  final runtime/restoration receipts and exact hashes verified.
- Commands, full hashes, logs, rejected theories, fixture/manual limits and exact
  next action: [development checkpoint](../docs/NATIVE-UI-FINISH-0.5.0-DEVELOPMENT-CHECKPOINT.md).

## Historical Claude checkpoint (retained)

- Active objective: delivery. Implementation, qualification and the 0.5.0
  package are done; the complete `Test-SourceOnly` gate on the product
  commit `4dbfa33` is running since 2026-10-11T12:49:38Z; the first, on `cd1b554`, failed on a stale launcher meta-test
  expectation (fixed in `4dbfa33`).
- Repository / branch / HEAD:
  `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-NUI` /
  `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10` / product
  `4dbfa33e1b36bf59bc9bb1a4ac3ebde860c52302` (base `a9c6d0f`, main 0.4.3); later commits are records
  only. Runtime builds come from the detached run worktree
  `repo/KingmakerBuffPlanner-NUIR` at the product commit (clean tree,
  `Build-Local.ps1`).
- Working tree: clean at each runtime build; ignored `GamePath.props` only.
- Version: 0.5.0 (Version.props, Info.json, AssemblyInfo; release notes,
  README, installation guide and player guide).
- C# protocol suite PASS=517 FAIL=0; Validate-Source PASS=43 FAIL=0 at the
  product commit.
- Candidate: package `cf658914130a010d7525ee47bc41a1f5c05f9bcde929852a1e31430ab3be8fcb`, DLL `b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69`, MVID `526e6663-b368-4e64-9afa-08f979dbcfe0`; standard ZIP
  `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-NUIR\artifacts\release\0.5.0\KingmakerBuffPlanner-0.5.0.zip` (`cf658914130a010d7525ee47bc41a1f5c05f9bcde929852a1e31430ab3be8fcb`, byte-identical).
- Guard/lease state: every runtime run in the ledger completed with
  restoration verified and protected saves clean; no open transaction.
- Delivery receipt: `docs/NATIVE-UI-FINISH-0.5.0-DELIVERY.md`; evidence
  manifest: `docs/NATIVE-UI-FINISH-0.5.0-EVIDENCE.md`.

## Reference review

- Reviewed references: R01–R15, every image opened and inspected.
- Native contracts confirmed offline (installed Assembly-CSharp 2.1.7b,
  SHA-256 `3b6450ff…5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`):
  - Text: native UI text is TextMeshPro (`TMPro.TextMeshProUGUI`, in
    `Assembly-CSharp-firstpass.dll`); the only legacy `Text` in the campaign
    canvas is Arial (ContextMenu). The planner's legacy `Text` + Arial-family
    donor is why it reads sans-serif.
  - Spell popup (R04): `StaticCanvas/Tooltips/DescWinConstructor`
    (`DescriptionWindow`), opened through
    `IDescriptionWindowUIHandler.HandleOpenDescription(TooltipData)`; the
    spellbook's own route is `TooltipTrigger.OnRightClick`. Bricks are built
    by `DescriptionTemplatesAbility` (header, Target, Duration, Saving Throw,
    Spell Resistance, Descriptors, Casting Time, description).
  - Windows: hotkeys run `ServiceWindowController.OnHotKeyPressed` →
    `FullScreenTabsWindow.OnShow`, which raises `IFullScreenUIHandler(true,
    type)`. `StaticCanvas.HandleFullScreenUiChanged` keeps ONE boolean: the
    planner's own `(false, Unknown)` on close would stop FullScreenUi mode
    under a native window that took over. `KeyboardAccess.Tick` skips every
    binding only while a `TMP_InputField` is selected; the planner's search
    is a legacy `InputField`, so native window hotkeys fire while typing.
  - HUD (contract kbp042-removal-06): `HUDLayout/Menu_Buttons48px`
    (GridLayoutGroupWorkaround, 345x101) with `Background` = `TBM_backButton`
    (668x252 px, border L196 B97 R253 T130, PPU 200, Tiled, 364x117); each
    cell is a baked `Ingame_Menu_<X>_Normal` (106 px) with a `Highlight`
    child (`_Hover`, ColorTint, normal alpha 0). The planner row borrowed
    only that invisible overlay, hence R11's bare white glyphs.
  - Spellbook footer: `Container_Book/MetamagicButtonPlace/ButtonPlace`
    (`main_menu_back`, sliced, 324x67).
- Design decisions: see the Feature status rows as they are implemented.

## Feature status

| ID | Status | Implementation checkpoint | Evidence / remaining criterion |
|---|---|---|---|
| UI-01 Popup | PARTIAL | `d5a1d99`; probe `93b29b1`, `f31fea9` | Same spell in the spellbook's and the planner's native window at both sizes; Escape and the close button. Remaining: long-description scrolling (fixture descriptions fit), casting-card right-click, physical input. |
| UI-02 Window switching | PARTIAL | `c57f490`, `16f749c`; probe `60d6611` | 7 routes (HUD and key bindings incl. journal), reopen, search gate, Escape order. Remaining: remapped keys, paused game, physical input. |
| UI-03 Font | PARTIAL | `aad17d1` | NexusSerif TMP for all 63 texts, ornamented title. Remaining: larger UI scale; small "Abilities" caption at 1280x720. |
| UI-04 Casting cards | PARTIAL | `c57f490`, `aad17d1` | Slips, frames, selected and Ready states. Remaining: active and unavailable cards, hover (fixture). |
| UI-05 Sidebar | PARTIAL | `aad17d1`; probe `f31fea9` | Owner's order; New casting with nothing selected. Remaining: group, provider, enhancement states (fixture). |
| UI-06 Catalogue | PARTIAL | `aad17d1` | Slips, selection, native scrollbar. Remaining: list scrolling, filter tabs, hover. |
| UI-07 Chrome text | PASS | `aad17d1` | Retired labels absent from the running surface; title "Buff Planner". |
| UI-08 Footer/native log | PASS | `c57f490`, `aad17d1`, `545a5cf` | Save failure/Retry/recovery; one game-log line per refused press and per run (casting run: five runs, five lines). |
| UI-09 HUD frame | PARTIAL | `fa0bfc4`; probe `cd1b554` | Stepped enclosure, native cell art, idle/hover/pressed tints judged. Remaining: disabled state (classic-mode execution only) not captured. |
| UI-10 Spellbook pair | PARTIAL | `bbe8426`, `d5a1d99` | Pair for two casters, alone for a non-caster, handoff. Remaining: METAMAGIC's own action. |
| UI-11 Shared parchment | PASS | `aad17d1`, `16f749c` | Full planner at 1920x1080 and 1280x720 inspected. |

## Verification ledger

| Check / runtime case A–F | Commit + loaded build | Command / scenario / configuration | Status | Evidence / observed result |
|---|---|---|---|---|
| Baseline build | a9c6d0f, 0.4.3 | `scripts\Build.ps1 -Configuration Release` | PASS | DLL 2b5750e3… |
| Baseline C# suite | a9c6d0f, 0.4.3 | tests exe (no args) | PASS | PASS=511 FAIL=0 |
| Facts stage (no input) | `c8a5a21`, DLL 5f900661… | live-workspace-native-ui facts, kbpnui-facts-02 | FAIL | `native-popup-not-opened`; the probe was fixed in `3fd5fe9` (it reads a spell row of the open book) |
| Facts stage | `3fd5fe9`, DLL a6a379c8… | kbpnui-facts-03 (1920x1200) | PASS | native facts and the before captures |
| Facts stage | `fa0bfc4`, DLL a6c3e141… | kbpnui-facts-04 | PASS | HUD enclosure live (UI-09) |
| Facts stage | `bbe8426`, DLL bf107186… | kbpnui-facts-05 | PASS | spellbook pair live (UI-10) |
| Facts stage | `d5a1d99`, DLL 639accd4… | kbpnui-facts-06 | PASS | native description window live (UI-01) |
| Facts stage | `aad17d1`, DLL 1ba4ecbc… | kbpnui-facts-07 (1920x1080 from here: session display changed outside the harness) | PASS | rewritten planner live (UI-03..08, UI-11) |
| Qualify stage | `16f749c`, DLL df682a06… | kbpnui-qual-01 | FAIL | `escape-order`, `game-log-planner-lines:0`: the description counted as open during its closing animation; fixed in `545a5cf` |
| Qualify stage | `545a5cf`, DLL 4c1da709… | kbpnui-qual-02, kbpnui-qual-720-01 (1280x720) | PASS | |
| Qualify stage (0.5.0) | `8ae985d`, DLL b354bb9e… | kbpnui-final-qual-01, kbpnui-final-qual-720-01 | PASS | planning 13/13; superseded (native popup on a different spell) |
| Qualify stage | `93b29b1`, DLL c9b4ddcc… | kbpnui-final-qual-02 | PASS | same-spell popups (Resistance) |
| Qualify stage | `60d6611`, DLL 83653ab5… | kbpnui-final-qual-03, kbpnui-final-qual-720-02 | PASS | 7 switch routes incl. key bindings |
| Casting (D/E) | `60d6611` frozen | kbpnui-final-cast-select-01, kbpnui-final-cast-01 (zero-cost-mixed, instant) | PASS | five runs, five game-log lines; superseded by later probe commits |
| Qualify stage | `f31fea9`, DLL 595ce75a… | kbpnui-final-qual-04 | FAIL | `hud-disabled-cells:0`: the HUD refreshes availability every tick; probe removed in `1938aaf` |
| Qualify stage | `1938aaf`, DLL ed11adbe… | kbpnui-final-qual-05, kbpnui-final-qual-720-03 | PASS | close button, empty selection; its pressed frame showed idle (probe never entered the cell); fixed in `cd1b554` |
| Selection | `1938aaf` frozen | kbpnui-final-cast-select-02 | PASS | superseded before its casting run |
| Qualify stage | `cd1b554`, DLL a8db3a16… | kbpnui-final-qual-06 (1920x1080), kbpnui-final-qual-720-04 (1280x720) | PASS | no violations; same-spell popups; 7/7 switches; close button; planning 13/13; HUD tints idle 0.0, hover 1.0, pressed 0.784 |
| Casting (D/E) | `cd1b554` frozen | kbpnui-final-cast-select-03, kbpnui-final-cast-02 | PASS | five runs, five game-log lines; resources unchanged (cantrips); no save written |
| Standard package | `cd1b554` | `scripts\Build-Release.ps1` | PASS | ZIP `d94418b0…`, byte-identical to the tested package; superseded |
| Complete gate | `cd1b554` | `scripts\Test-SourceOnly.ps1` (06:24:49Z-12:28:41Z) | FAIL | exit 1 in `Test-RuntimeLauncherFileWhatIf`: `display-mode-other-scenario` expected the refusal text from before `c8a5a21`; every earlier step passed |
| Gate fix fast checks | `4dbfa33`, DLL b32f320e… | scratch launcher meta-test (purity windows stubbed, stdin closed); `Test-FixtureInventoryEvidence`, `Test-RestoreInstallLocal`, `Test-PublishReleaseGate` | PASS | 13/13; 3/3; 16/16; 3/3 |
| Qualify stage (final) | `4dbfa33`, DLL b32f320e… | kbpnui-final-qual-07 (1920x1080), kbpnui-final-qual-720-05 (1280x720) | PASS | no violations; same-spell popups; 7/7 switches; close button; planning 13/13; HUD tints idle 0.0, hover 1.0, pressed 0.784 |
| Casting (D/E, final) | `4dbfa33` frozen | kbpnui-final-cast-select-04, kbpnui-final-cast-03 | PASS | five runs, five game-log lines; resources unchanged (cantrips); no save written |
| Standard package | `4dbfa33` | `scripts\Build-Release.ps1` | PASS | PASS=3 FAIL=0, deterministic 2/2; ZIP `cf658914…` is byte-identical to the tested package |
| Complete gate | `4dbfa33` | `scripts\Test-SourceOnly.ps1` | RUNNING | running since 2026-10-11T12:49:38Z |

- Before/after screenshot manifest: `docs/NATIVE-UI-FINISH-0.5.0-EVIDENCE.md`.
- Runtime staging and restoration: every run above restored with
  verification; protected saves clean.

## Handoff (2026-10-11 ~13:30 UTC)

The owner handed the remaining work to Codex. Triage of UI-01..06, UI-09, UI-10 (owner's
continuation of 2026-10-11) is in `docs/NATIVE-UI-FINISH-0.5.0-CODEX-HANDOFF.md` section 4
and on the owner's triage page. `57f136a` (source-tab captions: one shared size that fits,
UI-03/UI-06) is committed with its unit test (C# 518/0) but has no package and no in-game
run. The complete gate on `4dbfa33` was running at handoff.

## Continuation

Remaining blocker: none for this delivery. Open items are listed in the
delivery receipt (physical input, fixture-limited states, the owner's
1920x1200 display).

Next action: the owner's review of the receipt and the package. No push,
merge, tag or release without the owner's authorization.
