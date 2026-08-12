# Live Row Rendering Recovery — Independent Intake

Date: 2026-08-12  
Branch: `codex/kingmaker-buff-planner`  
Forensic HEAD: `94cbca8810d908d320eec0a2ca89533c7d4e0e05`  
Status: ROOT CAUSE PROVEN — first live canary absent; mask repair pending recheck

## Exact rejected identity and external state

- Human-tested/installed version: `0.0.5`.
- Release source: `390bb8b5f514a38edf1c553962813e29a1b526fd`.
- Installed/release DLL SHA-256: `6999284085bd6898f6bd871900783f6f81343a6f801b2d2c95acd208c6513b56`.
- Installed/release MVID: `d2fed415-bfa2-47a7-90ba-f50fa8d1c7de`.
- Release package SHA-256: `3eba3158aa92a6b66e249ec35aa297500eb4c5decdf73974c26992219922349c`.
- Human screenshot SHA-256 values:
  - full-screen: `c47c383c0ce89de37ece9914f9a057679a299042e32708063c838b58953a66bc`;
  - windowed: `e01d40ee9ca415947f802a1a5d2d5b42d89fc1f73512c09635001145bae4148b`.
- No Kingmaker/UMM process is running. The newest runtime transactions are `Restored` with `restorationVerified=true`; no top-level deployment lock exists.

## Frozen human PASS behavior

The four HUD icons, stable tooltips, HUD pointer isolation, F10 bootstrap, opaque modal, close/input/HUD restoration, persistence, native discovery, and Call of the Wild discovery are frozen against regression.

## Rendering evidence table

| Stage | Existing evidence | Verdict |
|---|---|---|
| Catalog/domain entries | Human header says 11 sources/11 providers; runtime model reported 11 | Proven to exist |
| Filtered view models | 0.0.5 runtime self-report says 10 | Internal state only |
| Row GameObjects | Runtime self-report says 10 instantiated/active | Internal state only |
| Layout geometry | Runtime self-report says content `625.4x1044.0`, viewport `625.4x500.0`, five overlapping rectangles | Geometry only; not visual proof |
| CanvasRenderer/material/font/alpha | Not recorded for rows or details | Unproven |
| Mask/clipping/stencil | Both content panes use the same `Mask` viewport; no live clip/cull/material evidence exists | Earliest shared unproven stage |
| Rendered pixels | No 0.0.5 screenshot or pixel-region evidence was captured | Failed proof obligation |
| Human visibility | Both human screenshots show pixel-uniform empty list and details panes | FAIL |

## Exact current hierarchy and first hypothesis

Both failed regions are constructed by `KingmakerUiFactory.CreateScrollView` as `ScrollRect -> Viewport(Image alpha 0.001 + Mask(showMaskGraphic=false)) -> Content(VerticalLayoutGroup)`. Header, tabs, filters, summary, footer, and opaque pane backgrounds outside those masked Content subtrees render correctly. Source rows and every details child share the masked Content path and are simultaneously absent in the screenshots. The same theme/font renders visible text elsewhere, so a catalog or global font failure is not the leading explanation.

The earliest common stage at which output stops is the viewport mask/stencil/content render path. Guarded run `row-render-0.0.6-canary-3` placed an opaque magenta Image and plain Arial Text directly under the live source `Content`; neither canary nor production rows appeared in screenshot `planner-render-canary.png` SHA-256 `4b3f7e05a47d830831582c1d2ff0e99ad14fbdeff51f6b42325784b31a08d886`.

The same run recorded the exact renderer chain. The viewport Mask material is `UI/Default`, `AlphaClip:True`, stencil `Op:Replace`, but its source Image color alpha is `0.001`. Every canary/row/details graphic is non-culled, alpha 1, uses the same canvas, and has a stencil material with `Comp:Equal, ReadMask:1`. Because the alpha-clipped mask source does not write a reliable stencil at that threshold, all masked child pixels fail the stencil comparison even though their CanvasRenderers and geometry look valid. `Mask.showMaskGraphic=false` already prevents visible mask color through `ColorMask:0`; lowering the source alpha was unnecessary and broke stencil generation.

This explains both blank panes and reconciles the misleading internal evidence: object state and geometry were real, but the stencil rejected their pixels. The bounded repair keeps the existing hierarchy and production renderer and makes only the hidden viewport mask source opaque.

## Invalidated prior claims

`activeSelf`, `activeInHierarchy`, non-zero `RectTransform` bounds, rectangle intersection, `detailsBound`, and the code-authored `rowVisible=True` value do not establish player-visible output. The two 0.0.5 physical runs remain valid for their input/tooltips/lifecycle evidence but are invalid as row/details rendering evidence.

## Exact next action

Run the same guarded live canary package with the opaque mask source. Require a screenshot-visible canary, production rows, and details; then remove the canary before final production qualification.
