using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // WP7 parchment and spell-scroll overhaul: the native scroll paper and
    // rule are borrowed only under their exact sprite contracts through the
    // capability resolver, drawn at a computed layer scale that keeps the
    // nine-slice borders at their verified size, and fall back exactly; and
    // the inks stay legible on the measured paper.
    internal static partial class Program
    {
        private static void RunParchmentThemeTests()
        {
            Run("wp7-scroll-sprite-contracts-are-exact", TestScrollSpriteContractsAreExact);
            Run("wp7-scroll-donors-resolve-per-capability", TestScrollDonorsResolvePerCapability);
            Run("wp7-paper-layer-scale-keeps-native-borders", TestPaperLayerScaleKeepsNativeBorders);
            Run("wp7-scroll-rules-follow-paper-and-structure", TestScrollRulesFollowPaperAndStructure);
            Run("wp7-inks-stay-legible-on-the-paper", TestInksStayLegibleOnThePaper);
            Run("wp7-workspace-wires-the-scroll-paper", TestWorkspaceWiresTheScrollPaper);
        }

        private static NativeSpriteFacts PaperFacts(string name = "dialogue_backsheet", float width = 858f,
            float height = 551f, float left = 268f, float bottom = 169f, float right = 258f, float top = 165f,
            float pixelsPerUnit = 200f, bool sliced = true, bool texture = true)
        {
            return new NativeSpriteFacts(name, width, height, left, bottom, right, top, pixelsPerUnit, sliced, texture);
        }

        private static NativeSpriteFacts RuleFacts()
        {
            return new NativeSpriteFacts("blockscroll_bottom", 147f, 11f, 20f, 0f, 20f, 0f, 200f, true, true);
        }

        // A sheet with any other name, size, border, density or draw type
        // would draw stretched or oversized corners: only the exact verified
        // artwork is borrowed, and the refusal names the mismatch.
        private static void TestScrollSpriteContractsAreExact()
        {
            NativeSpriteContract paper = NativeSpriteContract.ScrollPaper;
            Expect(paper.Failure(PaperFacts()) == null, "the verified dialogue_backsheet was rejected");
            Expect(NativeSpriteContract.ScrollRule.Failure(RuleFacts()) == null,
                "the verified blockscroll_bottom rule was rejected");
            var rejected = new Dictionary<string, NativeSpriteFacts>
            {
                { "sprite name", PaperFacts(name: "Inventory_Book_Clear") },
                { "no texture", PaperFacts(texture: false) },
                { "not Sliced", PaperFacts(sliced: false) },
                { "size changed", PaperFacts(width: 860f) },
                { "size changed ", PaperFacts(height: 550f) },
                { "border changed", PaperFacts(left: 134f) },
                { "border changed ", PaperFacts(bottom: 168f) },
                { "border changed  ", PaperFacts(right: 259f) },
                { "border changed   ", PaperFacts(top: 0f) },
                { "pixels per unit", PaperFacts(pixelsPerUnit: 100f) }
            };
            foreach (KeyValuePair<string, NativeSpriteFacts> pair in rejected)
            {
                string failure = paper.Failure(pair.Value);
                Expect(failure != null, "a changed paper sheet was accepted: " + pair.Value.Describe());
                Expect(failure.IndexOf(pair.Key.Trim(), StringComparison.Ordinal) >= 0,
                    "the refusal does not name the mismatch '" + pair.Key.Trim() + "': " + failure);
            }
            Expect(paper.Failure(null) != null, "a missing sprite passed the paper contract");
            Expect(paper.Failure(RuleFacts()) != null && NativeSpriteContract.ScrollRule.Failure(PaperFacts()) != null,
                "the paper and rule contracts accept each other's sprite");
            // Within float noise of the serialized values is still the sheet.
            Expect(paper.Failure(PaperFacts(left: 268.004f, pixelsPerUnit: 199.995f)) == null,
                "float noise rejected the verified sheet");
        }

        private static void AddCharacterBuildDonors(ThemeNode owner, out ThemeNode paper, out ThemeNode rule)
        {
            ThemeNode content = owner.Add("CharacterBuild").Add("Body").Add("Content");
            paper = content.Add("ClothColorSelector").Add("PrimarySelectorPlace").Add("ColorSelector")
                .Add("Background", Comp(NativeThemeComponent.Image, new ThemeToken()));
            rule = content.Add("RaceRightSide").Add("Constitution").Add("DescriptionView")
                .Add("Decor (1)", Comp(NativeThemeComponent.Image, new ThemeToken()));
        }

        // The two scroll capabilities resolve through the bounded literal
        // paths decoded from the in-game scene, are validated against their
        // exact contracts, and fail alone: a missing character-build subtree,
        // an ambiguous sibling, or a changed sheet drops only that
        // capability, with its reason, and runs only its fallback binding.
        private static void TestScrollDonorsResolvePerCapability()
        {
            var source = new FixtureThemeSource();
            ThemeNode owner = BuildDonorHierarchy(source);
            ThemeNode paperNode = FindByName(owner, "Background");
            ThemeNode ruleNode = FindByName(owner, "Decor (1)");
            var facts = new Dictionary<object, NativeSpriteFacts>
            {
                { paperNode.Components[NativeThemeComponent.Image][0], PaperFacts() },
                { ruleNode.Components[NativeThemeComponent.Image][0], RuleFacts() }
            };
            Action<NativeThemeCapability, object[]> exactContracts = (capability, components) =>
            {
                NativeSpriteContract contract = capability == NativeThemeCapability.ScrollPaper
                    ? NativeSpriteContract.ScrollPaper
                    : capability == NativeThemeCapability.ScrollRule ? NativeSpriteContract.ScrollRule : null;
                if (contract == null) return;
                NativeSpriteFacts read;
                string failure = contract.Failure(facts.TryGetValue(components[0], out read) ? read : null);
                if (failure != null) throw new InvalidOperationException(failure);
            };
            source.ValidateHook = exactContracts;
            NativeThemeResolution resolved = NativeThemeResolver.Resolve(owner, source);
            Expect(resolved.IsAvailable(NativeThemeCapability.ScrollPaper) &&
                resolved.IsAvailable(NativeThemeCapability.ScrollRule),
                "the verified scroll donors were rejected: " + resolved.Summary);
            Expect(ReferenceEquals(resolved.Get(NativeThemeCapability.ScrollPaper).Nodes[0], paperNode) &&
                ReferenceEquals(resolved.Get(NativeThemeCapability.ScrollRule).Nodes[0], ruleNode),
                "a scroll capability resolved a node other than its literal path");
            Expect(resolved.Summary.Contains("ScrollPaper=ok(candidate)") &&
                resolved.Summary.Contains("ScrollRule=ok(candidate)"),
                "the scroll donors are not recorded as unverified-live candidates: " + resolved.Summary);
            Expect(NativeThemeResolver.ScrollPaperPath ==
                "CharacterBuild/Body/Content/ClothColorSelector/PrimarySelectorPlace/ColorSelector/Background" &&
                NativeThemeResolver.ScrollRulePath ==
                "CharacterBuild/Body/Content/RaceRightSide/Constitution/DescriptionView/Decor (1)",
                "the decoded in-game donor paths changed");

            // A changed sheet: only the paper falls back, with the reason.
            facts[paperNode.Components[NativeThemeComponent.Image][0]] = PaperFacts(left: 134f);
            NativeThemeResolution changed = NativeThemeResolver.Resolve(owner, source);
            Expect(!changed.IsAvailable(NativeThemeCapability.ScrollPaper) &&
                changed.Failure(NativeThemeCapability.ScrollPaper).Contains("nine-slice border changed"),
                "a changed paper sheet was borrowed or lost its reason: " + changed.Summary);
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
                Expect(capability == NativeThemeCapability.ScrollPaper || changed.IsAvailable(capability),
                    "a changed paper sheet also dropped " + capability);
            var applied = new List<NativeThemeCapability>();
            var fellBack = new List<NativeThemeCapability>();
            var bindings = new NativeThemeBindings(reason => { });
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
                bindings.Add(capability, components => applied.Add(capability), () => fellBack.Add(capability));
            bindings.Apply(changed);
            Expect(fellBack.SequenceEqual(new[] { NativeThemeCapability.ScrollPaper }) &&
                applied.Contains(NativeThemeCapability.ScrollRule),
                "the paper fallback did not run alone: fell back " + string.Join(",", fellBack));

            // No character build in the scene: both scroll capabilities fall
            // back with the missing segment, nothing else does.
            var bare = new FixtureThemeSource();
            ThemeNode bareOwner = BuildDonorHierarchy(bare);
            ThemeNode build = FindByName(bareOwner, "CharacterBuild");
            build.Parent.Children.Remove(build);
            NativeThemeResolution missing = NativeThemeResolver.Resolve(bareOwner, bare);
            Expect(!missing.IsAvailable(NativeThemeCapability.ScrollPaper) &&
                !missing.IsAvailable(NativeThemeCapability.ScrollRule) &&
                missing.Failure(NativeThemeCapability.ScrollPaper).Contains("child not found: 'CharacterBuild'"),
                "a scene without the character build still borrowed scroll art: " + missing.Summary);
            Expect(missing.AvailableCount == NativeThemeResolution.Capabilities.Length - 2,
                "a missing character build dropped an unrelated capability: " + missing.Summary);

            // Two same-named sheets: refuse instead of guessing.
            var ambiguous = new FixtureThemeSource();
            ThemeNode ambiguousOwner = BuildDonorHierarchy(ambiguous);
            FindByName(ambiguousOwner, "ColorSelector").Add("Background",
                Comp(NativeThemeComponent.Image, new ThemeToken()));
            NativeThemeResolution twice = NativeThemeResolver.Resolve(ambiguousOwner, ambiguous);
            Expect(!twice.IsAvailable(NativeThemeCapability.ScrollPaper) &&
                twice.Failure(NativeThemeCapability.ScrollPaper).Contains("ambiguous direct children") &&
                twice.IsAvailable(NativeThemeCapability.ScrollRule),
                "an ambiguous paper sheet was accepted: " + twice.Summary);
        }

        // The scale math (Unity 2018.4 Image: a sliced border of b sprite
        // pixels draws b * refPPU / spritePPU canvas units; no
        // pixelsPerUnitMultiplier): the layer scale puts every border at
        // 0.25 units per sprite pixel whatever the canvas reference, and the
        // anchors make the scaled layer cover its bounds exactly.
        private static void TestPaperLayerScaleKeepsNativeBorders()
        {
            NativeSpriteContract paper = NativeSpriteContract.ScrollPaper;
            ParchmentLayerGeometry workspace = ParchmentLayerGeometry.For(paper, 100f);
            Expect(workspace.Valid && Near(workspace.Scale, 0.5f) && Near(workspace.AnchorMin, -0.5f) &&
                Near(workspace.AnchorMax, 1.5f), "the 100-PPU workspace canvas is not drawn at the 2x/0.5 layer: " +
                workspace.Describe(0f, 0f));
            Expect(Near(workspace.Borders.Left, 67f) && Near(workspace.Borders.Bottom, 42.25f) &&
                Near(workspace.Borders.Right, 64.5f) && Near(workspace.Borders.Top, 41.25f),
                "the drawn borders are not the verified L67/B42.25/R64.5/T41.25: " + workspace.Borders.Describe());
            // Unscaled, Unity would draw the 268-pixel border at 134 units.
            Expect(Near(ParchmentLayerGeometry.DrawnUnits(268f, 200f, 100f, 1f), 134f) &&
                Near(ParchmentLayerGeometry.DrawnUnits(268f, 200f, 100f, workspace.Scale), 67f),
                "the native sliced-border formula is not Unity's");
            foreach (float reference in new[] { 50f, 100f, 128f, 200f, 400f })
            {
                ParchmentLayerGeometry geometry = ParchmentLayerGeometry.For(paper, reference);
                Expect(geometry.Valid && Near(geometry.Borders.Left, 268f * ParchmentLayerGeometry.UnitsPerSpritePixel) &&
                    Near(geometry.Borders.Top, 165f * ParchmentLayerGeometry.UnitsPerSpritePixel),
                    "the border size depends on the canvas reference " + reference + ": " + geometry.Borders.Describe());
                // Bounds of width W: the layer's local width is W * (max-min)
                // = W / scale, drawn at scale: exactly W, centred.
                foreach (float bounds in new[] { 784f, 1896f })
                    Expect(Near(bounds * (geometry.AnchorMax - geometry.AnchorMin) * geometry.Scale, bounds) &&
                        Near((geometry.AnchorMin + geometry.AnchorMax) / 2f, 0.5f),
                        "the scaled layer does not cover its bounds at reference " + reference);
            }
            Expect(Near(ParchmentLayerGeometry.For(paper, 200f).Scale, 0.25f) &&
                Near(ParchmentLayerGeometry.For(paper, 50f).Scale, 1f), "the layer scale is not 0.25 * spritePPU / refPPU");
            foreach (float unusable in new[] { 0f, -100f, 0.5f, float.NaN, float.PositiveInfinity })
            {
                ParchmentLayerGeometry invalid = ParchmentLayerGeometry.For(paper, unusable);
                Expect(!invalid.Valid && invalid.Failure.Contains("canvas reference") &&
                    !invalid.DrawsUndistorted(2000f, 2000f),
                    "an unusable canvas reference drew paper: " + unusable);
            }
            // Below the border sums Unity squashes every corner; every
            // supported surface stays above them, including the frame on the
            // smallest supported screen (1280x720: 1232x636 plus outsets).
            Expect(Near(workspace.MinimumWidth, 131.5f) && Near(workspace.MinimumHeight, 83.5f),
                "the corner-safe minimum size changed");
            ParchmentInsets frame = ParchmentSurfaces.WorkspaceFrameOutsets;
            ParchmentInsets scroll = ParchmentSurfaces.SpellScrollOutsets;
            Expect(workspace.DrawsUndistorted(1232f + frame.Left + frame.Right, 636f + frame.Bottom + frame.Top) &&
                workspace.DrawsUndistorted(760f + scroll.Left + scroll.Right, 520f + scroll.Bottom + scroll.Top) &&
                !workspace.DrawsUndistorted(130f, 600f) && !workspace.DrawsUndistorted(600f, 80f),
                "a supported parchment surface would squash its corners");
            // Unscaled, the 760-wide scroll would spend 263 of its units on
            // borders (oversized); the layer halves that.
            Expect(ParchmentLayerGeometry.DrawnUnits(268f + 258f, 200f, 100f, 1f) > 260f &&
                workspace.MinimumWidth < 0.2f * 760f,
                "the scroll's borders are oversized");
            // The rule at its native height on the canvas, whole units.
            Expect(ParchmentLayerGeometry.RuleHeight(NativeSpriteContract.ScrollRule, 100f) == 6f &&
                ParchmentLayerGeometry.RuleHeight(NativeSpriteContract.ScrollRule, 200f) == 11f &&
                ParchmentRulePolicy.HairlineHeight == 2f,
                "the rule height is wrong");
            // The outsets keep the sheet's darker folded top and bottom
            // bands outside the frame (its header and footer sit on the
            // writing area); the curled side edges reach under 8 units in.
            float zoneTop = PlannerParchmentPalette.EdgeZoneTopPixels * ParchmentLayerGeometry.UnitsPerSpritePixel;
            float zoneBottom = PlannerParchmentPalette.EdgeZoneBottomPixels * ParchmentLayerGeometry.UnitsPerSpritePixel;
            float zoneLeft = PlannerParchmentPalette.EdgeZoneLeftPixels * ParchmentLayerGeometry.UnitsPerSpritePixel;
            float zoneRight = PlannerParchmentPalette.EdgeZoneRightPixels * ParchmentLayerGeometry.UnitsPerSpritePixel;
            Expect(zoneTop <= frame.Top && zoneBottom <= frame.Bottom && zoneLeft - frame.Left <= 8f &&
                zoneRight - frame.Right <= 8f, "the frame's text margins meet the sheet's darker edge zone");
        }

        private static void TestScrollRulesFollowPaperAndStructure()
        {
            // A planner rule belongs to the paper: a fully failed theme is
            // exactly the previous flat look.
            Expect(ParchmentRulePolicy.Resolve(true, false, false) == ParchmentRuleMode.Hidden &&
                ParchmentRulePolicy.Resolve(false, false, false) == ParchmentRuleMode.Hidden,
                "a planner rule shows without the paper");
            Expect(ParchmentRulePolicy.Resolve(true, true, false) == ParchmentRuleMode.Ornament &&
                ParchmentRulePolicy.Resolve(false, true, false) == ParchmentRuleMode.Hairline,
                "a planner rule on the paper is not the ornament, else the hairline");
            // The description's rule structures title/meta/body: always drawn.
            Expect(ParchmentRulePolicy.Resolve(true, false, true) == ParchmentRuleMode.Ornament &&
                ParchmentRulePolicy.Resolve(false, false, true) == ParchmentRuleMode.Hairline &&
                ParchmentRulePolicy.Resolve(false, true, true) == ParchmentRuleMode.Hairline,
                "the description's structural rule disappears with a donor");
        }

        // The paper is lighter than every ground the flat look ever gave, so
        // no ink loses contrast on it; body ink reads at 7:1 on the writing
        // area and 4.5:1 even on its darkest stain; headings and the meta
        // line pass for their sizes; and the selected and blocked chip
        // states stay distinct on their own opaque grounds.
        private static void TestInksStayLegibleOnThePaper()
        {
            UiRgb paper = PlannerParchmentPalette.PaperWritingArea;
            UiRgb darkest = PlannerParchmentPalette.PaperDarkestInterior;
            UiRgb before = PlannerParchmentPalette.FallbackGroundLightest;
            var inks = new Dictionary<string, UiRgb>
            {
                { "primary", PlannerParchmentPalette.PrimaryInk },
                { "secondary", PlannerParchmentPalette.SecondaryInk },
                { "heading", PlannerParchmentPalette.HeadingInk },
                { "burgundy", PlannerParchmentPalette.Burgundy },
                { "blocked", PlannerParchmentPalette.BlockedInk },
                { "legal", PlannerParchmentPalette.LegalInk },
                { "meta", PlannerParchmentPalette.MetaInk }
            };
            foreach (KeyValuePair<string, UiRgb> ink in inks)
                Expect(UiContrast.Ratio(ink.Value, paper) >= UiContrast.Ratio(ink.Value, before),
                    "the paper lowers the " + ink.Key + " ink's contrast below the flat look's");
            Expect(UiContrast.Ratio(PlannerParchmentPalette.PrimaryInk, paper) >= PlannerParchmentPalette.PreferredBodyText &&
                UiContrast.Ratio(PlannerParchmentPalette.PrimaryInk, darkest) >= PlannerParchmentPalette.BodyTextMinimum,
                "body ink is not comfortably legible on the paper");
            foreach (UiRgb heading in new[] { PlannerParchmentPalette.HeadingInk, PlannerParchmentPalette.Burgundy })
                Expect(UiContrast.Ratio(heading, paper) >= PlannerParchmentPalette.BodyTextMinimum &&
                    UiContrast.Ratio(heading, darkest) >= PlannerParchmentPalette.LargeTextMinimum,
                    "a heading ink is not legible on the paper: " + heading);
            Expect(UiContrast.Ratio(PlannerParchmentPalette.MetaInk, paper) >= PlannerParchmentPalette.BodyTextMinimum &&
                UiContrast.Ratio(PlannerParchmentPalette.MetaInk, paper) >
                    UiContrast.Ratio(PlannerParchmentPalette.SecondaryInk, paper),
                "the scroll's meta line is not legible on the paper");
            foreach (UiRgb ground in new[] { PlannerParchmentPalette.ChipGround, PlannerParchmentPalette.ChipGroundSelected })
                foreach (UiRgb outline in new[] { PlannerParchmentPalette.Burgundy, PlannerParchmentPalette.BlockedInk })
                    Expect(UiContrast.Ratio(outline, ground) >= PlannerParchmentPalette.NonTextMinimum,
                        "a chip state outline does not stand out on its ground");
            UiRgb selected = PlannerParchmentPalette.Burgundy;
            UiRgb blocked = PlannerParchmentPalette.BlockedInk;
            double distance = Math.Sqrt(Math.Pow(selected.R - blocked.R, 2) + Math.Pow(selected.G - blocked.G, 2) +
                Math.Pow(selected.B - blocked.B, 2));
            Expect(distance >= 0.15 && UiContrast.Ratio(selected, blocked) >= 1.5,
                "selected and blocked inks are not distinct: " + distance.ToString("0.000"));
            // The palette is the factory theme's own inks, not a copy that
            // drifted from what the planner draws.
            string factory = UiSource("KingmakerUiFactory.cs");
            Expect(factory.Contains("internal Color DarkBrownText = new Color(0.235f, 0.22f, 0.188f, 1f);") &&
                factory.Contains("internal Color MutedBrownText = new Color(0.541f, 0.392f, 0.271f, 1f);") &&
                factory.Contains("internal Color GoldAccent = new Color(0.588f, 0.243f, 0.106f, 1f);") &&
                factory.Contains("internal Color ParchmentPanel = new Color(0.965f, 0.890f, 0.725f, 0.88f);"),
                "the measured palette no longer matches the factory theme");
        }

        // Unity-bound wiring that cannot run here, checked in source: the
        // frame registers its paper and rules with the
        // native theme surface; the scroll capabilities are validated with
        // their exact contracts; the workspace never runs the localScale
        // reset that would double the paper layer.
        private static void TestWorkspaceWiresTheScrollPaper()
        {
            string view = UiSource("CastingWorkspaceScreenView.cs");
            string paper = SourceBlock(view, "private void ApplyScrollPaper()");
            Expect(paper != null && paper.Contains("_nativeTheme.RegisterParchment(_frameParchment);") &&
                paper.Contains("_nativeTheme.RegisterRule(header);") && paper.Contains("_nativeTheme.RegisterRule(footer);") &&
                paper.Contains("PlannerParchmentPalette.WellWashAlpha") && paper.Contains("PlannerParchmentPalette.LedgerWashAlpha"),
                "the workspace frame does not take the scroll paper");
            Expect(!view.Contains("ForceLayoutAndSnap"), "the workspace resets localScale under its paper layers");
            string theme = UiSource("PlannerNativeTheme.cs");
            Expect(theme.Contains("RequireContract((Image)values[0], NativeSpriteContract.ScrollPaper);") &&
                theme.Contains("RequireContract((Image)values[0], NativeSpriteContract.ScrollRule);"),
                "the live scroll donors are not held to their exact contracts");
            string surface = UiSource("PlannerNativeThemeSurface.cs");
            Expect(surface.Contains("_bindings.Add(NativeThemeCapability.ScrollPaper,") &&
                surface.Contains("delegate { _scrollPaperDonor = null; RefreshParchment(); });"),
                "the scroll paper has no exact fallback binding");
        }

        private static bool Near(float actual, float expected)
        {
            return Math.Abs(actual - expected) <= 0.001f;
        }
    }
}
