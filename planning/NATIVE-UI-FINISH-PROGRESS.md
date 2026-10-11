# Buff Planner native UI finish — progress

Mission packet: `KBP_NATIVE_UI_FINISH_2026-10-10` (owner ZIP
`C:\Dev\KingmakerBuffPlannerLab\KBP_Native_UI_Finish_Mission_2026-10-10.zip`,
SHA-256 `94ff31499bc374859366da2deb9cb7908c67bea96479b19cf5d86950e41a0470`),
extracted read-only to
`C:\Dev\KingmakerBuffPlannerLab\handoff\kbp-native-ui-finish-2026-10-10-94ff3149\`;
`scripts/verify_packet.py` PASS (30 files against MANIFEST.json). The packet
stays reference material; this file follows its `templates/PROGRESS.md`.

Updated: 2026-10-11 01:20 -04:00

## Current checkpoint

- Active objective: Stage 3 — qualification of the implemented UI-01..UI-11
  (native-UI qualify stage at the owner's 1920x1200, then the layout
  boundary, planning interactions and the casting/game-log case), then the
  version, records and the frozen-commit gate.
- Repository / branch / HEAD:
  `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-NUI` /
  `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10` / `16f749c`
  (base `a9c6d0f`, main 0.4.3). Runtime builds come from the detached run
  worktree `repo/KingmakerBuffPlanner-NUIR` checked out at the same commit
  (clean tree, `Build-Local.ps1`).
- Working tree: clean at each runtime build; ignored `GamePath.props` only.
- Version: still 0.4.3 in source; the version advance is scheduled with the
  final records so the qualified build is the delivered build.
- C# protocol suite PASS=517 FAIL=0; Validate-Source PASS=43 FAIL=0 at
  `16f749c`.
- Guard/lease state: every runtime run below completed with restoration
  verified and protected saves clean; no open transaction.

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
| UI-01 Popup | IMPLEMENTED, qualify pending | `d5a1d99` | kbpnui-facts-06: right-click opens the game's own DescriptionWindow above the planner (tooltip layer lifted by an owned override canvas), Escape closes it first, plan unchanged. |
| UI-02 Window switching | IMPLEMENTED, qualify pending | `c57f490`, `16f749c` | facts runs: three native routes take over and restore; search focus gate judged in qualify. |
| UI-03 Font | IMPLEMENTED, qualify pending | `aad17d1` | kbpnui-facts-07 planner capture: NexusSerif TMP by role, ornamental title. |
| UI-04 Casting cards | IMPLEMENTED, qualify pending | `c57f490`, `aad17d1` | Paper slips, fine lines, "Active · Will skip/recast". |
| UI-05 Sidebar | IMPLEMENTED, qualify pending | `aad17d1` | Owner's order; routine reassignment removed; new-castings scope named. |
| UI-06 Catalogue | IMPLEMENTED, qualify pending | `aad17d1` | Paper slips, framed icons, counts only when meaningful. |
| UI-07 Chrome text | IMPLEMENTED, qualify pending | `aad17d1` | Title "Buff Planner"; short lane headings; retired labels judged. |
| UI-08 Footer/native log | IMPLEMENTED, qualify pending | `c57f490`, `aad17d1` | One game-log line per run/refusal; footer Run status; Resources details; save failure judged in qualify. |
| UI-09 HUD frame | IMPLEMENTED, facts PASS | `fa0bfc4` | kbpnui-facts-04: enclosure installed, offsets 0, rail hidden, 4/4 top hits; captures nui-hud*.png. |
| UI-10 Spellbook pair | IMPLEMENTED, facts PASS | `bbe8426`, `d5a1d99` | kbpnui-facts-05: 210/210 pair, gap 10, same baseline/font; non-caster keeps the planner entry. |
| UI-11 Shared parchment | IMPLEMENTED, qualify pending | `aad17d1`, `16f749c` | Native slips, frames, portrait frames, rules, scrollbars. |

## Verification ledger

| Check / runtime case A–F | Commit + loaded build | Command / scenario / configuration | Status | Evidence / observed result |
|---|---|---|---|---|
| Baseline build | a9c6d0f, 0.4.3 | `scripts\Build.ps1 -Configuration Release` | PASS | DLL 2b5750e3… |
| Baseline C# suite | a9c6d0f, 0.4.3 | tests exe (no args) | PASS | PASS=511 FAIL=0 |
| Facts stage (no input) | `3fd5fe9`, 0.4.3 | live-workspace-native-ui facts, kbpnui-facts-03 | PASS | native facts and before captures; restoration verified |
| Facts stage | `fa0bfc4` | kbpnui-facts-04 | PASS | HUD enclosure live (UI-09) |
| Facts stage | `bbe8426` | kbpnui-facts-05 | PASS | spellbook pair live (UI-10) |
| Facts stage | `d5a1d99` | kbpnui-facts-06 | PASS | native description window live (UI-01) |
| Facts stage | `aad17d1`, DLL 1ba4ecbc…, MVID e198b338… | kbpnui-facts-07 | PASS | rewritten planner live (UI-03..08, UI-11) |

- Final frozen-commit gate: not started.
- Before/after screenshot manifest: not started.
- Runtime staging and restoration: no transaction opened by this mission yet.

## Continuation

Remaining blocker: none known.

Next action: read kbpnui-qual-01 (qualify stage, 1920x1200), fix any
violation, then the layout-boundary configuration, live-workspace-qual
(planning interactions) and the casting/game-log case.
