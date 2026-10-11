using System;

namespace KingmakerBuffPlanner.UI
{
    // Native UI finish (UI-10): METAMAGIC and BUFF PLANNER as a pair in the
    // spellbook's footer. Units are the spellbook book container's own
    // (Container_Book); centres are its local x. Measured natively
    // (kbpnui-facts-03): the native Metamagic place is 290.28 x 40 centred at
    // x 629.34 on the footer paper, whose usable width runs from about x 410
    // to 885; its plate (main_menu_back) reaches 17 units past each side.
    //
    // Paired, the two native-sized buttons share the native plate: METAMAGIC
    // narrowed to the left, BUFF PLANNER on the right, the plate widened
    // over both, the pair centred on the paper. Without a Metamagic button
    // (a character who cannot cast) BUFF PLANNER keeps the same place on its
    // own copy of the plate, so it never moves between characters and never
    // depends on Metamagic being available.
    public sealed class SpellbookFooterLayout
    {
        public const float ButtonWidth = 210f;
        public const float ButtonGap = 10f;
        // From the native place's centre to the footer paper's centre.
        public const float PaperCentreShift = 18f;

        private SpellbookFooterLayout()
        {
        }

        public bool Paired { get; private set; }
        public float MetamagicCentre { get; private set; }
        public float MetamagicWidth { get; private set; }
        public float PlannerCentre { get; private set; }
        public float PlannerWidth { get; private set; }
        // How far the native plate's right edge moves right to enclose the
        // planner button as well.
        public float PlateExtension { get; private set; }

        public static SpellbookFooterLayout Compute(float nativeCentre, float nativeWidth, bool metamagicShown)
        {
            if (nativeWidth <= 0f) throw new ArgumentException("The native Metamagic place has no width.");
            float pairCentre = nativeCentre + PaperCentreShift;
            float half = (ButtonWidth + ButtonGap) / 2f;
            return new SpellbookFooterLayout
            {
                Paired = metamagicShown,
                MetamagicCentre = metamagicShown ? pairCentre - half : nativeCentre,
                MetamagicWidth = metamagicShown ? ButtonWidth : nativeWidth,
                PlannerCentre = pairCentre + half,
                PlannerWidth = ButtonWidth,
                PlateExtension = metamagicShown ? ButtonWidth + ButtonGap : 0f
            };
        }

        public string Describe()
        {
            return "paired=" + Paired + ";metamagic=" + Format(MetamagicCentre) + "/" + Format(MetamagicWidth) +
                ";planner=" + Format(PlannerCentre) + "/" + Format(PlannerWidth) +
                ";plateExtension=" + Format(PlateExtension);
        }

        private static string Format(float value)
        {
            return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
