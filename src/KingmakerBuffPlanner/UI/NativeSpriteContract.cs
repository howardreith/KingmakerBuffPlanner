using System;
using System.Globalization;

namespace KingmakerBuffPlanner.UI
{
    // What the Unity adapter read from a native Image and its sprite, as
    // plain values, so one exact contract judges the live donor and the
    // deterministic fixtures alike.
    internal sealed class NativeSpriteFacts
    {
        internal NativeSpriteFacts(string name, float width, float height, float borderLeft,
            float borderBottom, float borderRight, float borderTop, float pixelsPerUnit,
            bool sliced, bool hasTexture)
        {
            Name = name ?? string.Empty;
            Width = width;
            Height = height;
            BorderLeft = borderLeft;
            BorderBottom = borderBottom;
            BorderRight = borderRight;
            BorderTop = borderTop;
            PixelsPerUnit = pixelsPerUnit;
            Sliced = sliced;
            HasTexture = hasTexture;
        }

        internal string Name { get; private set; }
        internal float Width { get; private set; }
        internal float Height { get; private set; }
        // Unity's Sprite.border order: x=left, y=bottom, z=right, w=top.
        internal float BorderLeft { get; private set; }
        internal float BorderBottom { get; private set; }
        internal float BorderRight { get; private set; }
        internal float BorderTop { get; private set; }
        internal float PixelsPerUnit { get; private set; }
        internal bool Sliced { get; private set; }
        internal bool HasTexture { get; private set; }

        internal string Describe()
        {
            return "'" + Name + "' " + Number(Width) + "x" + Number(Height) + " border L" +
                Number(BorderLeft) + "/B" + Number(BorderBottom) + "/R" + Number(BorderRight) + "/T" +
                Number(BorderTop) + " ppu " + Number(PixelsPerUnit) + (Sliced ? " sliced" : " not-sliced") +
                (HasTexture ? string.Empty : " no-texture");
        }

        internal static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    // An exact native sprite contract: name, pixel size, nine-slice border,
    // pixels per unit and the Sliced draw type. The planner borrows a donor
    // only when every field matches, and otherwise keeps its readable
    // fallback with the first mismatch recorded; a sheet with another
    // border would draw stretched corners, and a same-named sheet of
    // another size is not the verified artwork.
    internal sealed class NativeSpriteContract
    {
        private const float Tolerance = 0.01f;

        // The native DialogMessageBox paper (sharedassets6 sprite 339): the
        // same sheet the Roll for Stats panel borrows, here read from the
        // in-game StaticCanvas CharacterBuild colour selector.
        internal static readonly NativeSpriteContract ScrollPaper = new NativeSpriteContract(
            "dialogue_backsheet", 858f, 551f, 268f, 169f, 258f, 165f, 200f);

        // The native scroll rule (sharedassets4 sprite 458) the character
        // build draws under its description blocks.
        internal static readonly NativeSpriteContract ScrollRule = new NativeSpriteContract(
            "blockscroll_bottom", 147f, 11f, 20f, 0f, 20f, 0f, 200f);

        internal NativeSpriteContract(string name, float width, float height, float borderLeft,
            float borderBottom, float borderRight, float borderTop, float pixelsPerUnit)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("A sprite contract needs a name.", "name");
            if (pixelsPerUnit <= 0f) throw new ArgumentOutOfRangeException("pixelsPerUnit");
            Name = name;
            Width = width;
            Height = height;
            BorderLeft = borderLeft;
            BorderBottom = borderBottom;
            BorderRight = borderRight;
            BorderTop = borderTop;
            PixelsPerUnit = pixelsPerUnit;
        }

        internal string Name { get; private set; }
        internal float Width { get; private set; }
        internal float Height { get; private set; }
        internal float BorderLeft { get; private set; }
        internal float BorderBottom { get; private set; }
        internal float BorderRight { get; private set; }
        internal float BorderTop { get; private set; }
        internal float PixelsPerUnit { get; private set; }

        // Null when the facts are exactly this contract; otherwise the first
        // mismatch, phrased for the theme diagnostics.
        internal string Failure(NativeSpriteFacts facts)
        {
            if (facts == null) return "native sprite unavailable for '" + Name + "'";
            if (!string.Equals(facts.Name, Name, StringComparison.Ordinal))
                return "sprite name is " + facts.Describe() + ", expected '" + Name + "'";
            if (!facts.HasTexture) return "sprite has no texture: " + facts.Describe();
            if (!facts.Sliced) return "donor image is not Sliced: " + facts.Describe();
            if (!Same(facts.Width, Width) || !Same(facts.Height, Height))
                return "sprite size changed: " + facts.Describe() + ", expected " +
                    NativeSpriteFacts.Number(Width) + "x" + NativeSpriteFacts.Number(Height);
            if (!Same(facts.BorderLeft, BorderLeft) || !Same(facts.BorderBottom, BorderBottom) ||
                !Same(facts.BorderRight, BorderRight) || !Same(facts.BorderTop, BorderTop))
                return "nine-slice border changed: " + facts.Describe() + ", expected L" +
                    NativeSpriteFacts.Number(BorderLeft) + "/B" + NativeSpriteFacts.Number(BorderBottom) +
                    "/R" + NativeSpriteFacts.Number(BorderRight) + "/T" + NativeSpriteFacts.Number(BorderTop);
            if (!Same(facts.PixelsPerUnit, PixelsPerUnit))
                return "pixels per unit changed: " + facts.Describe() + ", expected " +
                    NativeSpriteFacts.Number(PixelsPerUnit);
            return null;
        }

        private static bool Same(float actual, float expected)
        {
            return !float.IsNaN(actual) && Math.Abs(actual - expected) <= Tolerance;
        }
    }
}
