# Visual theme: the scroll paper and the spell scroll (0.4.0 WP7, 0.4.2 aged sheet)

## 0.4.2: the aged service-window sheet, the table and the opening sound

The owner's 0.4.1 feedback: the planner's paper was too bright, too clean and
too regular next to the game's own inventory and character pages, and it
opened silently. 0.4.2 changes only the paper, the backdrop, the inks and the
opening sound. Layout, controls, input and every planner state are unchanged.

**Donors (runtime only; nothing shipped).** Run `kbp042-donors-02` listed the
paper sprites drawn by the native screens the owner compared against
(`native-paper-donors.json`, lab evidence only):

| Capability | Path under `StaticCanvas` | Exact contract | Drawn as |
| --- | --- | --- | --- |
| `SheetPaper` (new) | `ServiceWindow/Journal/Cart` | `Card_Big`, 2048x1566.8 (texture 2048x1567), no sprite border, 84.27984 pixels per unit, drawn Simple by the game, size tolerance 0.1 | The workspace frame's sheet and the spell scroll's sheet. The same sprite is the page of the character sheet (`CharacterScreen/Menu/Background`), the journal, the settings and the character build |
| `TableBackdrop` (new) | `ServiceWindow/Background` | `ServiceWindow_TableBackGruond_3840_2022` (the asset's own spelling), 2048x1024, no border, 100 pixels per unit, Simple | The dimmed full-screen backdrop behind the planner, untinted, as the service windows draw it |
| `ScrollPaper` (0.4.0) | unchanged | `dialogue_backsheet` | Second tier: used only when `SheetPaper` is missing or rejected (the exact 0.4.1 look) |
| `ScrollRule` (0.4.0) | unchanged | `blockscroll_bottom` | The rules, unchanged |

**Drawing the sheet.** `Card_Big` has no sprite border, so the planner does
not stretch the game's sprite. It creates its own sliced sprite over the
game's texture (`PlannerSheetSprites`, one per density, cached): full rect,
slice L120/B130/R130/T110 texels (wide enough to keep the corner folds whole),
and pixels per unit chosen so that one texel draws as `unitsPerTexel` canvas
units. `ParchmentSheetGeometry` (Unity-free, unit-tested) computes:

```text
screenScale   = clamp(screenHeight / 1080, 0.65, 1.35)
unitsPerTexel = density * screenScale          (frame 0.5, spell scroll 0.35)
spritePPU     = canvasReferencePPU / unitsPerTexel
borders       = slice * unitsPerTexel
edge zones    = (40, 56, 40, 12) texels * unitsPerTexel   (shadow margin + layered edges)
outsets       = base outsets * screenScale     (frame 20/24/20/10, scroll 14/17/14/7)
```

The sheet is drawn untinted (white), so its own aged tone, worn and irregular
edges, layered under-sheets and shading show as the game draws them. A soft
shadow silhouette sits at (3,-5) at alpha 0.45. Unlike 0.4.0's fixed screen
size, the edge now scales with the screen height, as the native pages do.
At the owner's 1920x1200 the frame logs
`Frame:paper=native-sheet;sprite=Card_Big;unitsPerTexel=0.556;screenScale=1.111;refPPU=100.0;spritePPU=180.0;borders=66.7/72.2/72.2/61.1;edges=22.2/31.1/22.2/6.7;size=1916x1154;undistorted=true;outsets=22.2/26.7/22.2/11.1;washes=4`
(`kbp042-paper-01`).

**Washes and inks.** On the sheet, the lane wells and the budget ledger drop
their washes (alpha 0), because the aged paper is the ground. The washes keep
their 0.4.0 behaviour on the second-tier scroll paper and the flat fallback.
`PlannerParchmentPalette` records the sheet's measured writing area (206,190,160)
and darkest interior (173,154,122). It darkens the inks to the native page
browns: body (0.16,0.14,0.11), secondary (0.38,0.26,0.16), heading
(0.52,0.20,0.08), burgundy selection (0.40,0.06,0.10), blocked (0.59,0.13,0.03),
legal (0.15,0.34,0.15) and meta (0.36,0.24,0.15). The chip grounds are
(0.93,0.89,0.80) and (0.96,0.88,0.74). The contrast floors of
`wp7-inks-stay-legible-on-the-paper` are kept with explicit thresholds and
checked against the darker sheet (`paper-042-*` tests).

**Measured result (same session, same display, `kbp042-paper-01` at
1920x1200).** These are mean luma values of the writing areas:

| Surface | Top of the page | Lower / right of the page |
| --- | --- | --- |
| 0.4.1 planner (`dialogue_backsheet`) | about 224 | about 224 (flat) |
| 0.4.2 planner (`Card_Big`) | about 195 | about 171-176 |
| Native inventory / character pages | about 205 | about 159-165 |

The planner is now in the native pages' range and carries the same top-to-bottom
shading. The native reference screens (`native-inventory.png`,
`native-character.png`, `native-map.png`) are captured in the same session,
right before the planner opens (`NativePaperReferenceCapture`, opened through
the game's own service-window handler and closed with its own Close button).

**Opening sound.** The planner posts the game's own UI sound
`UISoundType.CharacterScreenOpen` through `UISoundManager`. This is the Wwise
event `JournalOpen`, the paper/book sound the native character screen,
spellbook and journal post when they open (`ServiceWindowTabs.PlayShowSound`;
the table maps CharacterScreenOpen, SpellbookOpen and JournalOpen to
`JournalOpen`). No sound file is shipped. `WorkspaceOpenSoundCue`
(Unity-free) gives exactly one cue per successful open: it is armed when an
open starts, cancelled on each of the three failure paths, and emitted only
after the planner is open. A refresh, a reopen request while the planner is
already open, and the spell scroll post nothing. The game log records
`[KBP-WORKSPACE-SOUND] event=CharacterScreenOpen(Wwise JournalOpen);transition=closed-to-open;opens=N;played=N.`
(or `unavailable;...;failure=...`), and
`paper-sound-evidence.json` records the counts per cycle:
`kbp042-paper-01` shows 1/1, 1/1 after two refreshes, 1/1 after the spell scroll,
then 2/2 and 3/3 after two close-and-reopen cycles. The automation proves the
event call and its count, not what reaches the speakers. Hearing it is the
owner's check.

**Provenance.** Unchanged in kind: no Owlcat image or sound is in the
repository or the package. The sheet, table and sound are the running game's
own objects, referenced at runtime. The donor PNGs and screenshots written by
the runtime evidence (`runtime-evidence/kbp042-*`) stay in the lab and are
never committed or redistributed. The palette numbers above are measurements
only.

**Fallback.** Each capability fails alone. If `SheetPaper` is missing, rejected
or unmeasurable, the surfaces use the 0.4.1 scroll paper. If that is missing
too, they use the exact flat look. If `TableBackdrop` is missing, the backdrop
gets back its exact dim tint. A sound that cannot be posted is counted
(`UnavailableCount`, `LastFailure`) and never blocks the open.

**Still for the owner to judge in the game:** whether the aged sheet and table
read as native at their usual resolution, and that the opening sound is
audible and is the expected paper sound.

## 0.4.0 WP7 (the scroll paper, now the second tier)

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
become a 30% wash of their tint, so the sheet shows through; the footer's
budget ledger keeps 60% of its ground for its small text. A translucent wash
drops its Outline: Unity's Outline effect draws four offset copies of the
whole graphic under it, which a wash no longer hides, so the first live run
(`kbp040-wp7-sel-1080-01`) showed every lane filled with the outline's
reddish brown and the paper only in the margins
(`ParchmentSurfaces.WashKeepsOutline`, regression
`wp7-translucent-wash-drops-its-outline`). Two rules sit under the header row and
in the gap above the footer, drawn right above the paper and beneath every
control. The header rule is centred in the gap between the header row and
the routine bar, which follows the frame's height; when that gap is under 6
units (1600x900 and 1280x720) the rule is hidden rather than striking through
the routine tabs (`ParchmentHeaderRule`, first seen in
`kbp040-wp7-sel-720-01`). Casting chips keep their opaque grounds, and chips, caster and source
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
