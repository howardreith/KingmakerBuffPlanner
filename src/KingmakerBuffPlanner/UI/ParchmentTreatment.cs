using System;
using System.Globalization;

namespace KingmakerBuffPlanner.UI
{
    // Edge distances in canvas units, in Unity's border order.
    internal struct ParchmentInsets
    {
        internal ParchmentInsets(float left, float bottom, float right, float top)
        {
            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }

        internal float Left;
        internal float Bottom;
        internal float Right;
        internal float Top;

        internal string Describe()
        {
            return Number(Left) + "/" + Number(Bottom) + "/" + Number(Right) + "/" + Number(Top);
        }

        internal static string Number(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }

    // How the nine-sliced native paper is drawn on an owned surface without
    // stretched or oversized corners.
    //
    // Unity 2018.4's Image draws a Sliced border of b sprite pixels as
    // b * canvasReferencePPU / spritePPU canvas units (Image.pixelsPerUnit)
    // and has no pixelsPerUnitMultiplier. The donor sheet is never edited:
    // the planner scales its own layer instead. A uniform localScale s draws
    // that border as b * refPPU / spritePPU * s units, so
    // s = UnitsPerSpritePixel * spritePPU / refPPU draws every border pixel
    // as UnitsPerSpritePixel units whatever the canvas reference is. The
    // layer is anchored at 0.5 -/+ 0.5/s of its bounds with a centred pivot:
    // after the scale it covers the bounds exactly, and it follows every
    // resize through its anchors with no per-frame code.
    internal sealed class ParchmentLayerGeometry
    {
        // 0.25 canvas units per sprite pixel: the Roll for Stats panel's
        // verified look (dialogue_backsheet, 200 sprite PPU, on the native
        // 100-PPU canvas at layer scale 0.5: borders L67/B42.25/R64.5/T41.25).
        // The workspace canvas has no scaler, so a canvas unit is a screen
        // pixel there and the paper edge keeps one size at every resolution,
        // like the rest of the workspace's fixed-pixel layout.
        internal const float UnitsPerSpritePixel = 0.25f;

        private ParchmentLayerGeometry(NativeSpriteContract contract, float referencePixelsPerUnit,
            string failure)
        {
            Contract = contract;
            CanvasReferencePixelsPerUnit = referencePixelsPerUnit;
            Failure = failure;
            if (failure != null) return;
            Scale = UnitsPerSpritePixel * contract.PixelsPerUnit / referencePixelsPerUnit;
            AnchorMin = 0.5f - 0.5f / Scale;
            AnchorMax = 0.5f + 0.5f / Scale;
            Borders = new ParchmentInsets(
                DrawnUnits(contract.BorderLeft, contract.PixelsPerUnit, referencePixelsPerUnit, Scale),
                DrawnUnits(contract.BorderBottom, contract.PixelsPerUnit, referencePixelsPerUnit, Scale),
                DrawnUnits(contract.BorderRight, contract.PixelsPerUnit, referencePixelsPerUnit, Scale),
                DrawnUnits(contract.BorderTop, contract.PixelsPerUnit, referencePixelsPerUnit, Scale));
        }

        internal static ParchmentLayerGeometry For(NativeSpriteContract contract,
            float canvasReferencePixelsPerUnit)
        {
            if (contract == null) throw new ArgumentNullException("contract");
            string failure = null;
            if (float.IsNaN(canvasReferencePixelsPerUnit) || float.IsInfinity(canvasReferencePixelsPerUnit) ||
                canvasReferencePixelsPerUnit < 1f)
                failure = "canvas reference pixels per unit is unusable: " +
                    canvasReferencePixelsPerUnit.ToString(CultureInfo.InvariantCulture);
            return new ParchmentLayerGeometry(contract, canvasReferencePixelsPerUnit, failure);
        }

        internal NativeSpriteContract Contract { get; private set; }
        internal float CanvasReferencePixelsPerUnit { get; private set; }
        internal string Failure { get; private set; }
        internal bool Valid { get { return Failure == null; } }
        internal float Scale { get; private set; }
        internal float AnchorMin { get; private set; }
        internal float AnchorMax { get; private set; }
        // The drawn border widths after the layer scale, in canvas units.
        internal ParchmentInsets Borders { get; private set; }

        internal float MinimumWidth { get { return Borders.Left + Borders.Right; } }
        internal float MinimumHeight { get { return Borders.Bottom + Borders.Top; } }

        // Below the border sums Unity shrinks every border proportionally
        // (Image.GetAdjustedBorders): the corners would be squashed.
        internal bool DrawsUndistorted(float width, float height)
        {
            return Valid && width >= MinimumWidth && height >= MinimumHeight;
        }

        // Canvas units Unity draws for a run of sprite pixels at layer scale.
        internal static float DrawnUnits(float spritePixels, float spritePixelsPerUnit,
            float canvasReferencePixelsPerUnit, float layerScale)
        {
            return spritePixels * canvasReferencePixelsPerUnit / spritePixelsPerUnit * layerScale;
        }

        // A rule sprite drawn at its native height (layer scale 1), rounded
        // to whole canvas units so the line never lands between pixels.
        internal static float RuleHeight(NativeSpriteContract rule, float canvasReferencePixelsPerUnit)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            float native = DrawnUnits(rule.Height, rule.PixelsPerUnit, canvasReferencePixelsPerUnit, 1f);
            return Math.Max(1f, (float)Math.Round(native, MidpointRounding.AwayFromZero));
        }

        internal string Describe(float width, float height)
        {
            if (!Valid) return "geometry=invalid(" + Failure + ")";
            return "scale=" + Scale.ToString("0.000", CultureInfo.InvariantCulture) +
                ";refPPU=" + ParchmentInsets.Number(CanvasReferencePixelsPerUnit) +
                ";spritePPU=" + ParchmentInsets.Number(Contract.PixelsPerUnit) +
                ";borders=" + Borders.Describe() +
                ";size=" + width.ToString("0", CultureInfo.InvariantCulture) + "x" +
                height.ToString("0", CultureInfo.InvariantCulture) +
                ";undistorted=" + (DrawsUndistorted(width, height) ? "true" : "false");
        }
    }

    internal enum ParchmentRuleMode
    {
        Hidden,
        Hairline,
        Ornament
    }

    // The scroll rules (the native blockscroll ornament, or the Teleport
    // modal's hairline divider) that separate a scroll's heading from its
    // body. A planner rule belongs to the paper treatment: without the paper
    // it hides, so a fully failed theme is exactly the previous flat look.
    // A rule that structures a description (title / meta / body) keeps an
    // owned hairline even without any donor.
    internal static class ParchmentRulePolicy
    {
        internal static ParchmentRuleMode Resolve(bool ornamentAvailable, bool paperAvailable,
            bool structuralWithoutPaper)
        {
            if (!paperAvailable && !structuralWithoutPaper) return ParchmentRuleMode.Hidden;
            return ornamentAvailable ? ParchmentRuleMode.Ornament : ParchmentRuleMode.Hairline;
        }

        // The Teleport modal's divider: a sprite-less 2-unit line.
        internal const float HairlineHeight = 2f;
    }

    // The owned surfaces that carry the paper, and how far the sheet reaches
    // past each one. The outsets put the sheet's baked shadow margin and its
    // darker folded top/bottom bands (about 4-28 units at 0.25 units per
    // sprite pixel) outside the surface's text, so headers and footers stay
    // on the plain writing area.
    internal static class ParchmentSurfaces
    {
        internal const string WorkspaceFrame = "Frame";
        internal const string SpellScroll = "SpellScroll";

        // The workspace frame sits 24 units inside the screen's left, right
        // and bottom edges and 60 below its top: these outsets stay on screen.
        internal static readonly ParchmentInsets WorkspaceFrameOutsets = new ParchmentInsets(12f, 18f, 12f, 24f);
        internal static readonly ParchmentInsets SpellScrollOutsets = new ParchmentInsets(10f, 12f, 10f, 14f);

        // The soft second silhouette under the sheet (Roll for Stats).
        internal const float ShadowOffsetX = 2f;
        internal const float ShadowOffsetY = -3f;

        // Unity's Outline effect draws four offset copies of the WHOLE graphic
        // in its colour, under it. Under an opaque panel only the copies'
        // edges show; under a translucent wash they fill the panel. With the
        // lanes washed to 30% that filled every well with the outline's
        // reddish brown (kbp040-wp7-sel-1080-01: the paper showed only in the
        // margins). A wash on the paper therefore never keeps an Outline; it
        // gets it back exactly on fallback, when the panel is opaque again.
        internal static bool WashKeepsOutline(bool paperNative, float washAlpha, bool outlineBefore)
        {
            if (!paperNative) return outlineBefore;
            return outlineBefore && washAlpha >= 1f;
        }
    }
}
