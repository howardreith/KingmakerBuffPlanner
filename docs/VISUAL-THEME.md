# Visual theme: the scroll paper and the spell scroll (0.4.0 WP7)

The casting-first planner is drawn on the game's own parchment sheet, and the
right-click spell description is a spell scroll on the same paper. Nothing
graphical is shipped: the paper and its rule are read from the running game's
own UI objects, validated against exact sprite contracts and only displayed.
When anything is missing or different, the planner draws exactly its previous
flat parchment look.

Status: implemented and unit-tested; **not yet qualified in the game**. The
guarded screenshot runs at 1920x1080 and 2560x1440 decide the visual verdict
(see "What only the game can confirm").

## References

| Reference | Where | What it is | What WP7 emulates |
| --- | --- | --- | --- |
| Teleport destination modal (Kingmaker Gunslinger) | `C:\Dev\KingmakerGunslingerLab\repo\KingmakerGunslinger`, master `bf8a1e41b`, `src/KingmakerGunslinger/Spells/Teleportation/` (`WorldMapPointSpellActionPatches.cs`, `TeleportationUiDivider.cs`) | The native `GlobalMapMessageBox` dialog itself; the mod draws only hairline rules: a sprite-less 2-unit Image in the label colour at alpha 0.45 with 12% side margins | The hairline rule (the spell scroll's rule when the native ornament is unavailable, the frame rules' fallback on the paper) and the centred title / divider / body hierarchy. The dialog exists only on the global map, so it is not a donor |
| Roll for Stats (Kingmaker Dice Roller) | `C:\Dev\KingmakerDiceRollerLab\repo\KingmakerDiceRoller`, main `f185603` (v0.1.9), `docs/NATIVE-UI-STYLE.md`, `src/KingmakerDiceRoller/UI/NativeBookTheme.cs`, `src/KingmakerDiceRoller/UI/NativeRollPanelHost.cs` (`CreatePaperLayer`) | Borrows the native `dialogue_backsheet` sheet from the character-build UI under an exact sprite contract and draws it as a paper layer plus a soft shadow layer at double size and half scale; a native `blockscroll_bottom` rule under its heading | The exact contract, the paper and shadow layers (shadow offset 2,-3, brown at alpha 0.24), the drawn border size (L67/B42.25/R64.5/T41.25) and the native rule |

Both references are the owner's own projects under the MIT License. Neither
ships game art. No code was copied: the Buff Planner reimplements the
technique in its existing native-theme capability machinery (which shares the
same lineage) and adds the computed scale described below.

## Runtime donors

Both donors are looked up from the native `StaticCanvas` (never the planner's
own canvas) through the bounded literal-path lookup
(`NativeThemeDonorLookup`): exact ordinal names, inactive objects included,
ambiguous siblings rejected, depth and child counts capped. The paths were
decoded from the serialized in-game scene (`UI_Ingame_Scene`, every segment
unique); this mod has not yet rendered them live, so the resolver records them
as `candidate` locators.

| Capability | Path under `StaticCanvas` | Exact contract (`NativeSpriteContract`) | Drawn as |
| --- | --- | --- | --- |
| `ScrollPaper` | `CharacterBuild/Body/Content/ClothColorSelector/PrimarySelectorPlace/ColorSelector/Background` | `dialogue_backsheet` (sharedassets6 sprite 339, the native DialogMessageBox paper), 858x551, border L268/B169/R258/T165, 200 pixels per unit, Image type Sliced, texture present | The workspace frame's sheet and the spell scroll's sheet, each with a shadow silhouette |
| `ScrollRule` | `CharacterBuild/Body/Content/RaceRightSide/Constitution/DescriptionView/Decor (1)` | `blockscroll_bottom` (sharedassets4 sprite 458), 147x11, border L20/B0/R20/T0, 200 pixels per unit, Sliced | The rules under the frame's header, above its footer, and between the spell scroll's meta line and body |

The Unity adapter (`PlannerNativeTheme`) builds the sprite facts and rejects
any mismatch (name, size, border, pixels per unit, draw type, texture) with
the first difference as the reason. A rejected or missing donor drops only its
own capability; the late-donor recovery stays bounded (three attempts per
native owner). The donor objects are only read: nothing native is cloned,
re-parented, renamed or modified, and no event or controller is copied.

## Provenance and licensing

- No Owlcat game asset, extracted image or third-party artwork is in this
  repository or the release package. The sheet and the rule are the running
  game's own sprites, referenced at runtime and displayed by planner-owned
  Images.
- Owned "assets": none. The shadow, the washes and the hairline are
  sprite-less Images and tints defined in code.
- The measured numbers in `PlannerParchmentPalette` (the paper's mean
  writing-area colour, its darkest interior pixel and its edge zones) were
  sampled locally from the Roll for Stats research extraction
  (`artifacts/native-ui-reskin/dialogue_backsheet.png` in the Dice Roller lab,
  git-ignored there and never redistributed). Only the numbers are recorded.
- The game's sprites remain Owlcat's and are not redistributed. The reference
  mods are MIT-licensed by the owner.

## Scale: the borders are neither stretched nor oversized

Unity 2018.4's `Image` draws a Sliced border of `b` sprite pixels as
`b * canvasReferencePPU / spritePPU` canvas units and has no
`pixelsPerUnitMultiplier`. The workspace is its own top-level canvas with no
CanvasScaler, so its reference is expected to be the Canvas default of 100
pixels per unit (the evidence records the value actually read) and one canvas
unit is one screen pixel. Drawn as is, the 268-pixel left border
would be 134 units and a 760-unit scroll would spend 263 units on its borders.

`ParchmentLayerGeometry` reads the nearest canvas's `referencePixelsPerUnit` at
apply time and scales the owned layer instead of the sprite:

```text
layer scale s   = 0.25 * spritePPU / canvasReferencePPU      (0.5 on the workspace canvas)
drawn border    = b * canvasReferencePPU / spritePPU * s = 0.25 * b
                = L67 / B42.25 / R64.5 / T41.25 units         (the Roll for Stats look)
layer anchors   = 0.5 - 0.5/s .. 0.5 + 0.5/s of its bounds    (-0.5 .. 1.5), centred pivot
```

After the scale the layer covers its bounds exactly and follows every resize
through its anchors (no per-frame code). Below the border sums (131.5 x 83.5
units) Unity would squash the corners; the frame at the smallest supported
screen (1280x720) and the 760x520 scroll are far above them. An unusable
canvas reference (below 1, NaN, infinite) is a fallback, never a guess. The
rule is drawn at its native height (5.5 units, rounded to 6). The layers'
scale lives on their transforms: `KingmakerUiFactory.ForceLayoutAndSnap`
(which resets `localScale`) must never run over a parchment surface; the
workspace does not call it, and a unit test holds that.

Because the workspace canvas is in screen pixels, the paper edge keeps one
size at every resolution, like the rest of the workspace's fixed-pixel layout.

## Surfaces

**Workspace frame.** The sheet reaches 12 units past the frame's sides, 18
below and 24 above it, so the sheet's darker folded top and bottom bands lie
outside the header and footer and its curled side edges reach under 8 units
in (lanes start 2.8% of the frame in). The frame's flat tint goes transparent
(it stays the hit surface) and its outline yields. The three lanes' wells
become a 30% wash of their tint with their gold outline kept, so the sheet
shows through while each lane keeps its edge; the footer's budget ledger keeps
60% of its ground for its small text. Two rules sit under the header row and
in the gap above the footer, drawn right above the paper and beneath every
control. Casting chips keep their opaque grounds, and chips, caster and source
rows and target cards keep their own grounds, outlines and state inks
unchanged, so selected and blocked states read exactly as before.

**Spell scroll** (760x520, the sheet reaching 10/12/10/14 units past it): a
centred bold title in the native heading reddish brown, an italic meta line
(the native duration text and whether the exact selected variant is shown -
the read model has no school or level, so none is invented), a rule, and the
scrolling body in the body ink. The body sits straight on the paper; its
scrollbar appears only when the text overflows. The Close button keeps the
planner's native stone.

**Legibility** (`wp7-inks-stay-legible-on-the-paper`): the paper's writing
area is lighter than every ground the flat look could give, so no ink loses
contrast; body ink reads at 7:1 on the writing area and 4.5:1 on its darkest
stain; headings and the meta line reach 4.5:1; the selected (burgundy) and
blocked outlines stand out at 3:1 on the chip grounds and stay distinct from
each other.

## Fallback

`ScrollPaper` missing, rejected, or unmeasurable: the paper and shadow layers
are hidden, the frame and the scroll get back their exact flat tint and
outline, every wash gets back its exact colour and outline, and the frame
rules hide. The result is the pre-WP7 planner pixel for pixel. The spell
scroll keeps its title / meta / rule / body hierarchy on its flat panel with
the hairline rule. `ScrollRule` missing: rules on the paper draw the hairline.

## The spell scroll's input policy

`SpellScrollModalState` decides, Unity-free:

- **Escape** closes the description first; the next Escape leaves the focused
  casting, then closes the planner (never the game's own menu).
- **Clicking outside** the scroll (any mouse button, pressed and released on
  the dimmed backdrop) closes the description. The click is consumed: it never
  reaches the planner, the graph or the game, and selects or edits nothing. To
  read another spell, right-click it after the description has closed. A
  press that starts inside the scroll and is released outside is not a click.
  The visible paper edge beyond the scroll's rectangle counts as inside.
- **Clicking inside** the scroll does nothing but its own Close button.
- **The wheel** over the description scrolls only the description; anywhere
  else it is swallowed, so the planner's scrolls never move while it is open.
- The body is the exact native localized description in a read-only Text,
  never edited, trimmed or truncated. Opening or reading it never authors,
  targets, casts or saves anything.

## Diagnostics

- Game log, once per distinct outcome:
  `[KBP-THEME] parchment scrollPaper=ok|scrollRule=ok|Frame:paper=native;sprite=dialogue_backsheet;scale=0.500;refPPU=100.0;spritePPU=200.0;borders=67.0/42.3/64.5/41.3;size=1896x1038;undistorted=true;outsets=12.0/18.0/12.0/24.0;washes=4|HeaderRule:rule=ornament(blockscroll_bottom)|...`
  or `Frame:paper=fallback(<reason>)` with the resolver's or validator's
  reason.
- The workspace presentation evidence (`workspace-hierarchy-presents`) carries
  the same string after `page=continuous-scroll;book-art-retired;`.
- The physical record (`physical-workspace.json`) adds
  `workspacePaperEvidence` and `inspectPaperEvidence` (the open scroll's
  paper, its rule and `input=outside-click-closes;consumed;never-clicks-through`),
  read while the description is open. They are diagnostics, never violations:
  the flat fallback is a legitimate outcome.

## What only the game can confirm

- That both donors resolve in a live campaign (character build present on the
  `StaticCanvas`, exact contracts met) and the log shows `paper=native`.
- That the stretched centre of the sheet looks like paper and not a smear at
  1920x1080 and 2560x1440; that no corner is visibly stretched; that the folded
  bands sit outside the header and footer and the curled edges clear the lanes.
- That the 0.25 units-per-pixel border reads as a scroll at both resolutions
  (the constant and the outsets are the tuning points).
- Text legibility over the real texture, the 30%/60% washes, the selected and
  blocked chips, the rules' placement at each resolution.
- That the long graph still scrolls smoothly and the description's wheel
  never moves it; Escape and the outside click behave as above, physically.
