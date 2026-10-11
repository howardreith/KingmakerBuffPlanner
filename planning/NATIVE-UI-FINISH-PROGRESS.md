# Buff Planner native UI finish — progress

Mission packet: `KBP_NATIVE_UI_FINISH_2026-10-10` (owner ZIP
`C:\Dev\KingmakerBuffPlannerLab\KBP_Native_UI_Finish_Mission_2026-10-10.zip`,
SHA-256 `94ff31499bc374859366da2deb9cb7908c67bea96479b19cf5d86950e41a0470`),
extracted read-only to
`C:\Dev\KingmakerBuffPlannerLab\handoff\kbp-native-ui-finish-2026-10-10-94ff3149\`;
`scripts/verify_packet.py` PASS (30 files against MANIFEST.json). The packet
stays reference material; this file follows its `templates/PROGRESS.md`.

Updated: 2026-10-10 23:40 -04:00

## Current checkpoint

- Active objective: Stage 1 — native contracts and a no-input native-UI runtime
  scenario (facts + before evidence), then Stage 2 implementation.
- Repository / branch / HEAD:
  `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-NUI` /
  `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10` / base
  `a9c6d0f766370cdfb5176665fc6279b6620c4884` (main, v0.4.3, PR #8 merge).
- Working tree: new isolated worktree from `main`; ignored machine-local
  `GamePath.props` copied from the OP042 worktree. The main checkout
  (`repo/KingmakerBuffPlanner`, old 0.3.0 branch) and every other worktree
  are untouched.
- Baseline build: 0.4.3, Release `Build.ps1` PASS (DLL
  `2b5750e3dc9e1ce045217a245956d579b602f7c5d5edf0533eadfdaf57bbb5ac`);
  C# protocol suite PASS=511 FAIL=0 (20 s).
- Installed in the live game: `Mods\KingmakerBuffPlanner` 0.1.1-rc3, DLL
  `78407dd4c7240ce25c7654715854e83061da744a18d8eb4a2198fdffec5e3080`
  (restored by the harness after every run; never edited by hand).
- Runtime context: Kingmaker Enhanced Plus 2.1.7b, owner display 1920x1200,
  `full-user` profile, Automation fixture (`KBP_AUTOMATION_WORKING`).
- Guard/lease state (2026-10-10 23:10): no Kingmaker or launcher process; no
  open transaction in `runtime-state\transactions`; latest run
  `kbp042-removal-06` complete with restoration verified.

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
| UI-01 Popup | NOT_RUN | | |
| UI-02 Window switching | NOT_RUN | | |
| UI-03 Font | NOT_RUN | | |
| UI-04 Casting cards | NOT_RUN | | |
| UI-05 Sidebar | NOT_RUN | | |
| UI-06 Catalogue | NOT_RUN | | |
| UI-07 Chrome text | NOT_RUN | | |
| UI-08 Footer/native log | NOT_RUN | | |
| UI-09 HUD frame | NOT_RUN | | |
| UI-10 Spellbook pair | NOT_RUN | | |
| UI-11 Shared parchment | NOT_RUN | | |

## Verification ledger

| Check / runtime case A–F | Commit + loaded build | Command / scenario / configuration | Status | Evidence / observed result |
|---|---|---|---|---|
| Baseline build | a9c6d0f, 0.4.3 | `scripts\Build.ps1 -Configuration Release` | PASS | DLL 2b5750e3… |
| Baseline C# suite | a9c6d0f, 0.4.3 | tests exe (no args) | PASS | PASS=511 FAIL=0 |

- Final frozen-commit gate: not started.
- Before/after screenshot manifest: not started.
- Runtime staging and restoration: no transaction opened by this mission yet.

## Continuation

Remaining blocker: none known.

Next action: add the no-input `live-workspace-native-ui` scenario (native
facts, sprite previews, before screenshots), build the local package, run it
under the guarded launcher with an explicit non-casting configuration.
