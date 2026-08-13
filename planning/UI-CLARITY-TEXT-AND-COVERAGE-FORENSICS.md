# UI Clarity: Text, Coverage, and HUD Forensics

## Frozen baseline

Branch `codex/ui-clarity-presentation` starts from documentation HEAD `6dcdfa5`. The installed and preserved rollback is qualified 0.0.9 source `f026a4a9974af8e4191ff7fb104e472f11c2016f`, package `471e86e0043b47bc899322b640fb448105bfc1689f796b56611ed5d980d4bbe8`, DLL `d66edcacedcfe9d862e5cd433e2e58166abbc5a5a5404b9b7c5d6fd39ae898a1`, and MVID `174e2e17-9006-4667-b06d-85d372a2bb77`. A rollback copy is under `artifacts/release-candidate-backups/ui-clarity-start-0.0.9-6dcdfa5/`.

Discovery, semantic source aggregation, provider ranking/resources, routine identities and assignments, persistence, Animated and Instant execution, confirmed effects, hotkey isolation, modal ownership, HUD listeners/hitboxes/pointer ownership, compatibility, and guarded deployment are frozen.

## Existing runtime evidence

The exact 0.0.9 campaign inventory at `runtime-evidence/ui-polish-0.0.9-release-animated/native-ui-contract.json` records `StaticCanvas` as the one active native canvas: `ScreenSpaceCamera`, sorting order 10, no override sorting, and no second native canvas. Its exact legacy `UnityEngine.UI.Text` font is Arial, size 20, normal style, with Default UI material. Kingmaker service-window copy is predominantly TMP, but this net47 project has no TMP assembly reference; forcing an unproven TMP path would add a dependency and is outside this bounded pass. The safe proven path remains the native Arial legacy font and `UI/Default` material.

The 0.0.9 planner instead creates an independent `ScreenSpaceOverlay` root with a `CanvasScaler` using 1920x1080 and a 0.5 width/height match. At the captured 1280x720 window, card widths of 269.84 canvas units become roughly 179.9 screen pixels and multiple selected-detail rectangles land on fractional coordinates. That scale/resampling path explains the reported softness more directly than the font identity, which already resolves to native Arial. The correction retains a dedicated top-level modal canvas for proven input/sorting ownership, removes its nested/scaling component, sets a pixel-space root explicitly from the active screen, preserves `localScale=Vector3.one`, uses fixed font sizes with `resizeTextForBestFit=false`, reuses the native font/material path, and snaps owned text/card rectangles after forced layout.

## Coverage boundary

The 0.0.9 `TargetPortraitViewModel` computes indirect coverage from `EffectTarget.Party`/`AreaRecipients` plus generic legality. That is not the actual selected-card plan and can mark recipients which no cast step covers. `CastPlan` already contains provider, cast anchor, exact step targets, expected effects, mass flag, and per-request outcomes, but routine plans lack a source ID on steps/outcomes. The bounded domain addition is provenance only: `SourceId` travels from the existing `BuffCastRequest` into `CastStep` and `TargetPlanOutcome`. It changes no allocation, provider choice, reservation, or execution behavior.

`TargetCoverageViewModel` will filter the active routine preview to the selected aggregate source and derive, in precedence order:

1. invalid target;
2. explicitly selected but unfulfilled;
3. explicitly selected and fulfilled/covered;
4. non-explicit actual expected recipient of a selected-source mass/area/caster-centered step;
5. neutral.

For a normal per-target step only the explicit target is a recipient. For mass/party/area steps, expected recipients come from that step's reachable normalized target set. A caster-centered expression does not create a fake direct selection; the provider caster is recorded as `IsCastAnchor`, while non-requested recipients are indirect. Failure text comes from the selected-source `TargetPlanOutcome.Reason`, translated into exact player language.

## HUD boundary

The row is already parented to the exact formation-button cluster, but `anchoredPosition.x=0` assumes the native parent's origin is its visual left edge. The correction derives the formation/grid left edge through native world corners and converts that point back into the cluster parent's local coordinates. Width, height, order, hitboxes, listeners, tooltips, and actions remain unchanged.

0.0.9 also stretches glyphs with asymmetric 13px/7px insets. Every owned icon sprite is generated at 64x64 with a centered 0.5 pivot, yet the geometry intentionally moves it right. The correction uses a centered fixed safe-area RectTransform (`anchorMin=anchorMax=pivot=0.5`, zero anchored position, unit scale, preserve aspect) and centralized optional optical offsets. Runtime diagnostics will measure sprite alpha bounds and screen-space glyph/button centers without changing hitboxes.

## Summary language

The selected-buff panel currently formats the whole routine as `N casts | X of Y targets covered | Z blocked`. It will instead filter the actual preview to the selected aggregate source and show `Available: ...` and `Planned: ...`, plus optional explicit/additional recipient counts. Per-target amber/red states and tooltips carry failures. Routine tabs use `N ready` and `M issue(s)` rather than an unexplained fraction.
