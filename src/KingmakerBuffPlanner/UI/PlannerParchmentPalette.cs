namespace KingmakerBuffPlanner.UI
{
    // The casting-first planner's inks on the native paper, and the paper
    // they sit on. 0.4.2: the paper is the native card sheet (Card_Big),
    // drawn untinted; its colours were MEASURED on the runtime preview of
    // the live sprite (runtime-evidence kbp042-donors-02, kept outside this
    // repository; only these sampled numbers are recorded, never the image):
    // the mean of the writing area (texels 170-1870 x 150-1410, alpha 255)
    // and the mean of its darkest 1%. The 0.4.1 scroll sheet
    // (dialogue_backsheet, now the fallback paper) was measured the same way
    // (sprite pixels 80-780 x 120-460 and its darkest interior pixel). The
    // fallback ground is the flat ParchmentPanel tint (0.965,0.890,0.725 at
    // 0.88) the planner drew before WP7, over a white scene dimmed by the
    // 0.55 black blocker.
    public static class PlannerParchmentPalette
    {
        public static readonly UiRgb PaperWritingArea = UiRgb.FromBytes(206, 190, 160);
        public static readonly UiRgb PaperDarkestInterior = UiRgb.FromBytes(173, 154, 122);
        public static readonly UiRgb ScrollPaperWritingArea = new UiRgb(0.931f, 0.848f, 0.685f);
        public static readonly UiRgb ScrollPaperDarkestInterior = UiRgb.FromBytes(205, 183, 139);
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
        // 0.4.2: darkened for the native card, which is ~15% darker than the
        // 0.4.1 sheet: body ink 7:1 on the writing area and 4.5:1 on its
        // darkest 1%; every other text ink 4.5:1 on the writing area; the
        // selected (wine) and blocked (brick) inks stay distinct.
        public static readonly UiRgb PrimaryInk = new UiRgb(0.16f, 0.14f, 0.11f);
        public static readonly UiRgb SecondaryInk = new UiRgb(0.38f, 0.26f, 0.16f);
        public static readonly UiRgb HeadingInk = new UiRgb(0.52f, 0.20f, 0.08f);
        public static readonly UiRgb Burgundy = new UiRgb(0.40f, 0.06f, 0.10f);
        public static readonly UiRgb BlockedInk = new UiRgb(0.59f, 0.13f, 0.03f);
        public static readonly UiRgb LegalInk = new UiRgb(0.15f, 0.34f, 0.15f);
        // The spell scroll's duration/meta line: darker than SecondaryInk so
        // small italic meta text reads at 4.5:1 on the paper.
        public static readonly UiRgb MetaInk = new UiRgb(0.36f, 0.24f, 0.15f);

        // Casting chips keep their own opaque grounds on the paper - lighter
        // cards on the sheet like the native screens' inset panels, toned
        // down so they no longer glare; the selected chip is outlined in
        // Burgundy, a blocked one in BlockedInk.
        public static readonly UiRgb ChipGround = new UiRgb(0.93f, 0.89f, 0.80f);
        public static readonly UiRgb ChipGroundSelected = new UiRgb(0.96f, 0.88f, 0.74f);

        // On the scroll sheet the lanes' flat wells become a light wash so
        // the sheet shows through while each lane keeps its edge; the
        // footer's budget ledger keeps more of its ground for its small text.
        public const float WellWashAlpha = 0.30f;
        public const float LedgerWashAlpha = 0.60f;
        // 0.4.2: on the native card the lanes are the sheet itself, as on
        // the native screens (no cream wash brightens or yellows them).
        public const float SheetWellWashAlpha = 0f;
        public const float SheetLedgerWashAlpha = 0f;
        public static readonly UiRgb SheetWash = UiRgb.FromBytes(173, 154, 122);
        // The Teleport modal's divider opacity.
        public const float HairlineAlpha = 0.45f;
        public const float ShadowAlpha = 0.24f;
        // The card's silhouette over the dark table.
        public const float SheetShadowAlpha = 0.45f;
        public static readonly UiRgb Shadow = new UiRgb(0.16f, 0.10f, 0.06f);

        public const double BodyTextMinimum = 4.5;
        public const double PreferredBodyText = 7.0;
        public const double LargeTextMinimum = 3.0;
        public const double NonTextMinimum = 3.0;
    }
}
