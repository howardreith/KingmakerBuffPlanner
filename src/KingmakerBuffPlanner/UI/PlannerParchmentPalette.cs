namespace KingmakerBuffPlanner.UI
{
    // The casting-first planner's inks on the native paper, and the paper
    // they sit on. The paper colours were MEASURED from the native
    // dialogue_backsheet pixels (858x551, the Roll for Stats research
    // extraction kept outside this repository; only these sampled numbers
    // are recorded, never the image): the mean of the writing area (sprite
    // pixels 80-780 x 120-460) and its darkest interior pixel. The fallback
    // ground is the flat ParchmentPanel tint (0.965,0.890,0.725 at 0.88) the
    // planner drew before WP7, over a white scene dimmed by the 0.55 black
    // blocker: the LIGHTEST ground the old look could give any ink.
    public static class PlannerParchmentPalette
    {
        public static readonly UiRgb PaperWritingArea = new UiRgb(0.931f, 0.848f, 0.685f);
        public static readonly UiRgb PaperDarkestInterior = UiRgb.FromBytes(205, 183, 139);
        public static readonly UiRgb FallbackGroundLightest = new UiRgb(
            0.965f * 0.88f + 0.45f * 0.12f, 0.890f * 0.88f + 0.45f * 0.12f, 0.725f * 0.88f + 0.45f * 0.12f);

        // The sheet's darker edge zone, in sprite pixels from each edge (the
        // baked shadow margin, the curled edges and the folded top and bottom
        // bands), measured along the sheet's middle rows and columns.
        public const float EdgeZoneLeftPixels = 66f;
        public const float EdgeZoneBottomPixels = 67f;
        public const float EdgeZoneRightPixels = 72f;
        public const float EdgeZoneTopPixels = 60f;

        // Inks drawn straight on the paper. Primary and Secondary are the
        // factory theme's DarkBrownText and MutedBrownText; Heading is the
        // native heading reddish brown (RGB 150/62/27, the factory's
        // GoldAccent); Burgundy, Blocked and Legal are the workspace's
        // selection, blocked and legal inks.
        public static readonly UiRgb PrimaryInk = new UiRgb(0.235f, 0.22f, 0.188f);
        public static readonly UiRgb SecondaryInk = new UiRgb(0.541f, 0.392f, 0.271f);
        public static readonly UiRgb HeadingInk = new UiRgb(0.588f, 0.243f, 0.106f);
        public static readonly UiRgb Burgundy = new UiRgb(0.55f, 0.13f, 0.08f);
        public static readonly UiRgb BlockedInk = new UiRgb(0.72f, 0.26f, 0.12f);
        public static readonly UiRgb LegalInk = new UiRgb(0.22f, 0.42f, 0.22f);
        // The spell scroll's duration/meta line: darker than SecondaryInk so
        // small italic meta text reads at 4.5:1 on the paper.
        public static readonly UiRgb MetaInk = new UiRgb(0.42f, 0.30f, 0.20f);

        // Casting chips keep their own opaque grounds on the paper; the
        // selected chip is outlined in Burgundy, a blocked one in BlockedInk.
        public static readonly UiRgb ChipGround = new UiRgb(0.99f, 0.95f, 0.86f);
        public static readonly UiRgb ChipGroundSelected = new UiRgb(1f, 0.93f, 0.80f);

        // On the paper the lanes' flat wells become a light wash so the sheet
        // shows through while each lane keeps its edge; the footer's budget
        // ledger keeps more of its ground for its small text.
        public const float WellWashAlpha = 0.30f;
        public const float LedgerWashAlpha = 0.60f;
        // The Teleport modal's divider opacity.
        public const float HairlineAlpha = 0.45f;
        public const float ShadowAlpha = 0.24f;
        public static readonly UiRgb Shadow = new UiRgb(0.16f, 0.10f, 0.06f);

        public const double BodyTextMinimum = 4.5;
        public const double PreferredBodyText = 7.0;
        public const double LargeTextMinimum = 3.0;
        public const double NonTextMinimum = 3.0;
    }
}
