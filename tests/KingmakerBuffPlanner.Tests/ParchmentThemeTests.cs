using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // WP7 parchment and spell-scroll overhaul: the native scroll paper and
    // rule are borrowed only under their exact sprite contracts through the
    // capability resolver, drawn at a computed layer scale that keeps the
    // nine-slice borders at their verified size, and fall back exactly; the
    // spell scroll's input policy, text composition and layout hierarchy;
    // and the inks stay legible on the measured paper.
    internal static partial class Program
    {
        private static void RunParchmentThemeTests()
        {
            Run("wp7-scroll-sprite-contracts-are-exact", TestScrollSpriteContractsAreExact);
            Run("wp7-scroll-donors-resolve-per-capability", TestScrollDonorsResolvePerCapability);
            Run("wp7-paper-layer-scale-keeps-native-borders", TestPaperLayerScaleKeepsNativeBorders);
            Run("wp7-scroll-rules-follow-paper-and-structure", TestScrollRulesFollowPaperAndStructure);
            Run("wp7-spell-scroll-input-policy", TestSpellScrollInputPolicy);
            Run("wp7-spell-scroll-content-is-native-and-read-only", TestSpellScrollContentIsNative);
            Run("wp7-spell-scroll-layout-hierarchy", TestSpellScrollLayoutHierarchy);
            Run("wp7-inks-stay-legible-on-the-paper", TestInksStayLegibleOnThePaper);
            Run("wp7-workspace-wires-the-scroll-paper", TestWorkspaceWiresTheScrollPaper);
            Run("wp7-translucent-wash-drops-its-outline", TestTranslucentWashDropsItsOutline);
            Run("wp7-header-rule-only-where-there-is-room", TestHeaderRuleOnlyWhereThereIsRoom);
            Run("paper-042-native-sheet-and-table-contracts", TestNativeSheetAndTableContracts);
            Run("paper-042-sheet-and-table-resolve-and-fall-back", TestNativeSheetAndTableResolve);
            Run("paper-042-sheet-geometry-keeps-native-edges", TestSheetGeometryKeepsNativeEdges);
            Run("paper-042-sheet-is-drawn-untinted-on-the-table", TestSheetWiringInSource);
            Run("sound-042-one-native-paper-cue-per-open", TestWorkspaceOpenSoundCue);
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
            // The colour selector's sheet (ServiceWindow/Background is the 0.4.2 table).
            ThemeNode paperNode = FindByName(owner, "ColorSelector").Children.Single(node => node.Name == "Background");
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
                workspace.DrawsUndistorted(SpellScrollLayout.PanelWidth + scroll.Left + scroll.Right,
                    SpellScrollLayout.PanelHeight + scroll.Bottom + scroll.Top) &&
                !workspace.DrawsUndistorted(130f, 600f) && !workspace.DrawsUndistorted(600f, 80f),
                "a supported parchment surface would squash its corners");
            // Unscaled, the 760-wide scroll would spend 263 of its units on
            // borders (oversized); the layer halves that.
            Expect(ParchmentLayerGeometry.DrawnUnits(268f + 258f, 200f, 100f, 1f) > 260f &&
                workspace.MinimumWidth < 0.2f * SpellScrollLayout.PanelWidth,
                "the scroll's borders are oversized");
            // The rule at its native height on the canvas, whole units.
            Expect(ParchmentLayerGeometry.RuleHeight(NativeSpriteContract.ScrollRule, 100f) == 6f &&
                ParchmentLayerGeometry.RuleHeight(NativeSpriteContract.ScrollRule, 200f) == 11f &&
                SpellScrollLayout.RuleSlotHeight >= ParchmentLayerGeometry.RuleHeight(NativeSpriteContract.ScrollRule, 100f) &&
                SpellScrollLayout.RuleSlotHeight >= ParchmentRulePolicy.HairlineHeight,
                "the rule height or its reserved slot is wrong");
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

        // Escape closes the description first; an outside click closes it
        // and is consumed; inside clicks and stray wheels are swallowed; the
        // wheel over the text scrolls only the text. Closed, nothing is
        // claimed, so the planner's own Escape and wheel apply again.
        private static void TestSpellScrollInputPolicy()
        {
            var state = new SpellScrollModalState();
            foreach (SpellScrollInput input in Enum.GetValues(typeof(SpellScrollInput)))
                Expect(state.Handle(input) == SpellScrollOutcome.NotHandled && !state.IsOpen,
                    "a closed description claimed " + input);
            SpellScrollContent first = SpellScrollContent.Compose("#1 Resistance", "Native text.", "1 minute", true);
            state.Open(first);
            Expect(state.IsOpen && ReferenceEquals(state.Content, first), "the description did not open");
            Expect(state.Handle(SpellScrollInput.WheelOverDescription) == SpellScrollOutcome.ScrollsDescription &&
                state.Handle(SpellScrollInput.WheelElsewhere) == SpellScrollOutcome.Consumed &&
                state.Handle(SpellScrollInput.ClickInsideScroll) == SpellScrollOutcome.Consumed && state.IsOpen,
                "the wheel or a click inside leaked past or closed the description");
            Expect(state.Handle(SpellScrollInput.ClickOutsideScroll) == SpellScrollOutcome.Closed && !state.IsOpen,
                "an outside click did not close the description");
            Expect(state.Handle(SpellScrollInput.ClickOutsideScroll) == SpellScrollOutcome.NotHandled,
                "the closing click was handled twice");
            state.Open(first);
            SpellScrollContent probe = SpellScrollContent.Compose("Layout probe", "Long text.", string.Empty, false);
            state.Open(probe);
            Expect(state.IsOpen && ReferenceEquals(state.Content, probe), "re-showing did not replace the content in place");
            Expect(state.Handle(SpellScrollInput.Escape) == SpellScrollOutcome.Closed && !state.IsOpen &&
                state.Handle(SpellScrollInput.Escape) == SpellScrollOutcome.NotHandled,
                "Escape did not close the description first and then fall through to the planner");
            Expect(SpellScrollModalState.OutsideClickPolicy == "outside-click-closes;consumed;never-clicks-through",
                "the documented outside-click policy changed");
        }

        private static void TestSpellScrollContentIsNative()
        {
            const string native = "  You gain <b>acid</b> resistance 10.\r\n\r\nAt 7th level: 20.  ";
            SpellScrollContent exact = SpellScrollContent.Compose("#2 Resist Energy", native, "  10 minutes/level ", true);
            Expect(exact.Body == native && exact.HasNativeDescription,
                "the native description was altered: '" + exact.Body + "'");
            Expect(exact.Title == "#2 Resist Energy" &&
                exact.Meta == "10 minutes/level" + SpellScrollContent.MetaSeparator + SpellScrollContent.ExactNote,
                "the title or meta line changed: " + exact.Meta);
            SpellScrollContent base_ = SpellScrollContent.Compose(null, " \n ", null, false);
            Expect(base_.Title.Length == 0 && base_.Meta == SpellScrollContent.BaseNote &&
                base_.Body == SpellScrollContent.MissingDescription && !base_.HasNativeDescription,
                "an empty description was not stated honestly");
            Expect(SpellScrollContent.BaseNote == "base spell — select a caster/source for exact values" &&
                SpellScrollContent.MissingDescription == "The game provides no description for this spell.",
                "the player-facing description wording changed");
        }

        private static void TestSpellScrollLayoutHierarchy()
        {
            SpellScrollLayout layout = SpellScrollLayout.Compute(SpellScrollLayout.PanelWidth,
                SpellScrollLayout.PanelHeight);
            SpellScrollRect[] parts = { layout.Title, layout.Meta, layout.Rule, layout.Body, layout.Close };
            foreach (SpellScrollRect part in parts)
                Expect(part.X >= 0f && part.Y >= 0f && part.Right <= layout.Width && part.Bottom <= layout.Height &&
                    part.Width > 0f && part.Height > 0f, "a scroll part leaves the sheet");
            Expect(layout.Title.Bottom <= layout.Meta.Y && layout.Meta.Bottom <= layout.Rule.Y &&
                layout.Rule.Bottom <= layout.Body.Y, "title, meta, rule and body are out of order");
            for (int first = 0; first < parts.Length; first++)
                for (int second = first + 1; second < parts.Length; second++)
                    Expect(!parts[first].Overlaps(parts[second]), "scroll parts " + first + "/" + second + " overlap");
            Expect(Near(layout.Title.X, layout.Width - layout.Title.Right) &&
                Near(layout.Rule.X, layout.Width - layout.Rule.Right),
                "the title or rule is not centred on the sheet");
            Expect(layout.Body.Height >= 0.7f * layout.Height, "the description body is cramped: " + layout.Body.Height);
            // The physical wheel probe aims at the scroll's centre: it must
            // land on the description, never on the title or backdrop.
            Expect(layout.Body.X < layout.Width / 2f && layout.Body.Right > layout.Width / 2f &&
                layout.Body.Y < layout.Height / 2f && layout.Body.Bottom > layout.Height / 2f,
                "the scroll's centre is not over the description");
            // Every text and the Close button sit on the plain writing area:
            // inside the sheet's darker edge zone reach (zone minus outset).
            ParchmentInsets outsets = ParchmentSurfaces.SpellScrollOutsets;
            float unit = ParchmentLayerGeometry.UnitsPerSpritePixel;
            float reachLeft = PlannerParchmentPalette.EdgeZoneLeftPixels * unit - outsets.Left;
            float reachRight = PlannerParchmentPalette.EdgeZoneRightPixels * unit - outsets.Right;
            float reachTop = PlannerParchmentPalette.EdgeZoneTopPixels * unit - outsets.Top;
            float reachBottom = PlannerParchmentPalette.EdgeZoneBottomPixels * unit - outsets.Bottom;
            foreach (SpellScrollRect part in parts)
                Expect(part.X >= reachLeft && layout.Width - part.Right >= reachRight && part.Y >= reachTop &&
                    layout.Height - part.Bottom >= reachBottom, "a scroll part sits on the sheet's darker edge");
            // The panel keeps the physical record's on-screen floor at the
            // supported resolutions (25% of 1920x1080 and 2560x1440).
            Expect(layout.Width >= 0.25f * 2560f && layout.Height >= 0.25f * 1440f,
                "the spell scroll is too small for the physical judgement at 2560x1440");
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
            // 0.4.2: the native card is darker than the 0.4.1 sheet, so the
            // inks are held to explicit thresholds on every paper the
            // planner can show: the native card, the 0.4.1 scroll sheet
            // fallback, and the flat fallback ground.
            foreach (KeyValuePair<string, UiRgb> ink in inks)
                foreach (UiRgb ground in new[] { paper, PlannerParchmentPalette.ScrollPaperWritingArea, before })
                    Expect(UiContrast.Ratio(ink.Value, ground) >= PlannerParchmentPalette.BodyTextMinimum,
                        "the " + ink.Key + " ink is below 4.5:1 on a paper the planner shows: " +
                        UiContrast.Ratio(ink.Value, ground).ToString("0.00"));
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
            Expect(factory.Contains("internal Color DarkBrownText = new Color(0.16f, 0.14f, 0.11f, 1f);") &&
                factory.Contains("internal Color MutedBrownText = new Color(0.38f, 0.26f, 0.16f, 1f);") &&
                factory.Contains("internal Color GoldAccent = new Color(0.52f, 0.20f, 0.08f, 1f);") &&
                factory.Contains("internal Color ParchmentPanel = new Color(0.965f, 0.890f, 0.725f, 0.88f);"),
                "the measured palette no longer matches the factory theme");
        }

        // Unity-bound wiring that cannot run here, checked in source: the
        // frame and the description register their paper and rules with the
        // native theme surface; the scroll capabilities are validated with
        // their exact contracts; the workspace never runs the localScale
        // reset that would double the paper layer; Escape and the backdrop
        // go through the description's policy before the planner's own.
        // Regression for kbp040-wp7-sel-1080-01: an Outline under a 30% lane
        // wash filled the lane with its reddish brown. On the native paper a
        // translucent wash never keeps its Outline; an opaque one may; the
        // fallback restores exactly what the panel had.
        private static void TestTranslucentWashDropsItsOutline()
        {
            Expect(!ParchmentSurfaces.WashKeepsOutline(true, PlannerParchmentPalette.WellWashAlpha, true) &&
                !ParchmentSurfaces.WashKeepsOutline(true, PlannerParchmentPalette.LedgerWashAlpha, true) &&
                !ParchmentSurfaces.WashKeepsOutline(true, 0f, true),
                "a translucent wash on the paper kept an Outline that fills it");
            Expect(ParchmentSurfaces.WashKeepsOutline(true, 1f, true) &&
                !ParchmentSurfaces.WashKeepsOutline(true, 1f, false),
                "an opaque panel lost or gained its own Outline");
            Expect(ParchmentSurfaces.WashKeepsOutline(false, PlannerParchmentPalette.WellWashAlpha, true) &&
                !ParchmentSurfaces.WashKeepsOutline(false, PlannerParchmentPalette.WellWashAlpha, false),
                "the fallback did not restore the panel's own Outline");
            string adapter = UiSource("PlannerParchment.cs");
            Expect(adapter.Contains("ParchmentSurfaces.WashKeepsOutline(Native, alpha, wash.OutlineFallback)") &&
                adapter.Contains("float alpha = _mode == Paper.Sheet ? wash.SheetAlpha : wash.PaperAlpha;"),
                "the wash adapter does not apply the outline policy to the alpha it draws");
            Expect(!ParchmentSurfaces.WashKeepsOutline(true, PlannerParchmentPalette.SheetWellWashAlpha, true) &&
                !ParchmentSurfaces.WashKeepsOutline(true, PlannerParchmentPalette.SheetLedgerWashAlpha, true),
                "a wash on the native card kept an Outline that fills it");
        }

        // Regression for kbp040-wp7-sel-720-01: the header rule, placed 45
        // units below the frame's top, struck through the routine tabs at
        // 1280x720 because the routine bar follows the frame's height. Frame
        // heights are the live ones (screen height minus 84).
        private static void TestHeaderRuleOnlyWhereThereIsRoom()
        {
            float? hd = ParchmentHeaderRule.OffsetBelowTop(996f);
            float? qhd = ParchmentHeaderRule.OffsetBelowTop(1356f);
            Expect(hd.HasValue && hd.Value > ParchmentHeaderRule.HeaderControlsBottom + 2f &&
                hd.Value < 996f * (1f - ParchmentHeaderRule.RoutineBarTopAnchor) - 2f,
                "at 1920x1080 the rule is not between the header row and the routine bar");
            Expect(qhd.HasValue && qhd.Value > hd.Value, "at 2560x1440 the rule does not follow the routine bar");
            Expect(!ParchmentHeaderRule.OffsetBelowTop(636f).HasValue &&
                !ParchmentHeaderRule.OffsetBelowTop(816f).HasValue,
                "at 1280x720 or 1600x900 the rule would strike through the routine tabs");
            Expect(!ParchmentHeaderRule.OffsetBelowTop(0f).HasValue &&
                !ParchmentHeaderRule.OffsetBelowTop(float.NaN).HasValue, "an unmeasured frame placed the rule");
            string view = UiSource("CastingWorkspaceScreenView.cs");
            Expect(view.Contains("ParchmentHeaderRule.RoutineBarTopAnchor,") &&
                SourceBlock(view, "internal void RefreshView()").Contains("PlaceHeaderRule();"),
                "the routine bar and the header rule do not share one placement rule");
        }

        private static void TestWorkspaceWiresTheScrollPaper()
        {
            string view = UiSource("CastingWorkspaceScreenView.cs");
            string paper = SourceBlock(view, "private void ApplyScrollPaper()");
            Expect(paper != null && paper.Contains("_nativeTheme.RegisterParchment(_frameParchment);") &&
                paper.Contains("_nativeTheme.RegisterRule(header);") && paper.Contains("_nativeTheme.RegisterRule(footer);") &&
                paper.Contains("PlannerParchmentPalette.WellWashAlpha") && paper.Contains("PlannerParchmentPalette.LedgerWashAlpha"),
                "the workspace frame does not take the scroll paper");
            Expect(!view.Contains("ForceLayoutAndSnap"), "the workspace resets localScale under its paper layers");
            string build = SourceBlock(view, "private void BuildSpellInspect()");
            Expect(build != null &&
                build.Contains("RectTransform backdrop = KingmakerUiFactory.CreateRect(\"Backdrop\", _inspectRoot);") &&
                build.Contains("RectTransform panel = KingmakerUiFactory.CreateRect(\"Panel\", _inspectRoot);") &&
                build.Contains("sink.OutsideClick = () => SpellInspectInput(SpellScrollInput.ClickOutsideScroll);") &&
                build.Contains("_nativeTheme.RegisterParchment(_inspectParchment);") &&
                build.Contains("_nativeTheme.RegisterRule(_inspectRule);") &&
                build.IndexOf("CreateRect(\"Backdrop\"", StringComparison.Ordinal) <
                    build.IndexOf("CreateRect(\"Panel\"", StringComparison.Ordinal),
                "the spell scroll's backdrop is not a sibling drawn beneath the scroll");
            string escape = SourceBlock(view, "internal bool HandleEscape()");
            Expect(escape != null && escape.IndexOf("_inspectState.Handle(SpellScrollInput.Escape)", StringComparison.Ordinal) >= 0 &&
                escape.IndexOf("_inspectState.Handle(SpellScrollInput.Escape)", StringComparison.Ordinal) <
                    escape.IndexOf("_session.ClearGraphFocus();", StringComparison.Ordinal),
                "Escape does not close the description before the focused casting");
            string theme = UiSource("PlannerNativeTheme.cs");
            Expect(theme.Contains("RequireContract((Image)values[0], NativeSpriteContract.ScrollPaper);") &&
                theme.Contains("RequireContract((Image)values[0], NativeSpriteContract.ScrollRule);"),
                "the live scroll donors are not held to their exact contracts");
            string surface = UiSource("PlannerNativeThemeSurface.cs");
            Expect(surface.Contains("_bindings.Add(NativeThemeCapability.ScrollPaper,") &&
                surface.Contains("delegate { _scrollPaperDonor = null; RefreshParchment(); });"),
                "the scroll paper has no exact fallback binding");
        }

        private static NativeSpriteFacts SheetFacts(string name = "Card_Big", float width = 2048f,
            float height = 1566.8f, float pixelsPerUnit = 84.27984f, bool sliced = false, bool texture = true,
            float border = 0f)
        {
            return new NativeSpriteFacts(name, width, height, border, border, border, border, pixelsPerUnit,
                sliced, texture);
        }

        private static NativeSpriteFacts TableFacts(string name = "ServiceWindow_TableBackGruond_3840_2022",
            float width = 2048f, float height = 1024f, float pixelsPerUnit = 100f)
        {
            return new NativeSpriteFacts(name, width, height, 0f, 0f, 0f, 0f, pixelsPerUnit, false, true);
        }

        // 0.4.2: the native card sheet and table are held to their exact
        // live contracts (runtime kbp042-donors-02). The game draws both
        // Simple, so a Sliced draw is not required of them (the planner
        // draws the card as its own nine-slice); everything else is exact.
        private static void TestNativeSheetAndTableContracts()
        {
            NativeSpriteContract sheet = NativeSpriteContract.SheetPaper;
            NativeSpriteContract table = NativeSpriteContract.TableBackdrop;
            Expect(sheet.Failure(SheetFacts()) == null && sheet.Failure(SheetFacts(sliced: true)) == null &&
                sheet.Failure(SheetFacts(height: 1566.79f)) == null,
                "the verified Card_Big sheet was rejected");
            Expect(table.Failure(TableFacts()) == null, "the verified table was rejected");
            var rejected = new Dictionary<string, NativeSpriteFacts>
            {
                { "sprite name", SheetFacts(name: "Card_Small") },
                { "no texture", SheetFacts(texture: false) },
                { "size changed", SheetFacts(width: 1166f) },
                { "size changed ", SheetFacts(height: 1567.2f) },
                { "nine-slice border changed", SheetFacts(border: 20f) },
                { "pixels per unit", SheetFacts(pixelsPerUnit: 100f) }
            };
            foreach (KeyValuePair<string, NativeSpriteFacts> pair in rejected)
            {
                string failure = sheet.Failure(pair.Value);
                Expect(failure != null && failure.IndexOf(pair.Key.Trim(), StringComparison.Ordinal) >= 0,
                    "a changed card sheet was accepted or the refusal is unnamed: " + pair.Value.Describe());
            }
            Expect(table.Failure(TableFacts(name: "Map_back")) != null &&
                table.Failure(TableFacts(width: 1922f)) != null && table.Failure(TableFacts(pixelsPerUnit: 50f)) != null &&
                sheet.Failure(TableFacts()) != null && table.Failure(SheetFacts()) != null,
                "the table contract accepts other art");
            // The 0.4.1 scroll sheet keeps its Sliced requirement.
            Expect(NativeSpriteContract.ScrollPaper.Failure(PaperFacts(sliced: false)) != null &&
                NativeSpriteContract.ScrollPaper.Failure(PaperFacts(height: 551.05f)) != null,
                "the scroll sheet contract was loosened");
        }

        // Both donors resolve from their recorded live paths, are validated
        // with their exact contracts, and fall back alone.
        private static void TestNativeSheetAndTableResolve()
        {
            var source = new FixtureThemeSource();
            ThemeNode owner = BuildDonorHierarchy(source);
            ThemeNode cart = FindByName(owner, "Cart");
            ThemeNode table = FindByName(owner, "ServiceWindow").Children.Single(node => node.Name == "Background");
            var facts = new Dictionary<object, NativeSpriteFacts>
            {
                { cart.Components[NativeThemeComponent.Image][0], SheetFacts() },
                { table.Components[NativeThemeComponent.Image][0], TableFacts() }
            };
            source.ValidateHook = (capability, components) =>
            {
                NativeSpriteContract contract = capability == NativeThemeCapability.SheetPaper
                    ? NativeSpriteContract.SheetPaper
                    : capability == NativeThemeCapability.TableBackdrop ? NativeSpriteContract.TableBackdrop : null;
                if (contract == null) return;
                NativeSpriteFacts read;
                string failure = contract.Failure(facts.TryGetValue(components[0], out read) ? read : null);
                if (failure != null) throw new InvalidOperationException(failure);
            };
            NativeThemeResolution resolved = NativeThemeResolver.Resolve(owner, source);
            Expect(resolved.IsAvailable(NativeThemeCapability.SheetPaper) &&
                resolved.IsAvailable(NativeThemeCapability.TableBackdrop) &&
                ReferenceEquals(resolved.Get(NativeThemeCapability.SheetPaper).Nodes[0], cart) &&
                ReferenceEquals(resolved.Get(NativeThemeCapability.TableBackdrop).Nodes[0], table) &&
                resolved.Summary.Contains("SheetPaper=ok(candidate)") &&
                resolved.Summary.Contains("TableBackdrop=ok(candidate)"),
                "the card sheet or table did not resolve from its live path: " + resolved.Summary);
            Expect(NativeThemeResolver.SheetPaperPath == "ServiceWindow/Journal/Cart" &&
                NativeThemeResolver.TableBackdropPath == "ServiceWindow/Background",
                "the recorded live donor paths changed");
            facts[cart.Components[NativeThemeComponent.Image][0]] = SheetFacts(name: "Card_Small");
            NativeThemeResolution changed = NativeThemeResolver.Resolve(owner, source);
            Expect(!changed.IsAvailable(NativeThemeCapability.SheetPaper) &&
                changed.Failure(NativeThemeCapability.SheetPaper).Contains("sprite name") &&
                NativeThemeResolution.Capabilities.All(capability =>
                    capability == NativeThemeCapability.SheetPaper || changed.IsAvailable(capability)),
                "a changed card sheet was borrowed or dropped another capability: " + changed.Summary);
        }

        // The card drawn as the planner's own nine-slice: borders of the
        // planner slice at the screen-scaled density, the sprite density
        // Unity needs for them, outsets that keep the visible edge zones off
        // the text, corners that fit every supported surface, and a writing
        // area never drawn above one pixel per texel at 1920-wide screens.
        private static void TestSheetGeometryKeepsNativeEdges()
        {
            ParchmentSheetGeometry hd = ParchmentSheetGeometry.For(ParchmentSurfaces.FrameSheetUnitsPerTexel, 1080f, 100f);
            Expect(hd.Valid && Near(hd.ScreenScale, 1f) && Near(hd.UnitsPerTexel, 0.5f) &&
                Near(hd.SpritePixelsPerUnit, 200f) && Near(hd.Borders.Left, 60f) && Near(hd.Borders.Bottom, 65f) &&
                Near(hd.Borders.Right, 65f) && Near(hd.Borders.Top, 55f),
                "the 1080p sheet is not the planner slice at 0.5 units per texel: " + hd.Describe(0f, 0f));
            // Unity's sliced border: b texels * refPPU / spritePPU units.
            Expect(Near(ParchmentLayerGeometry.DrawnUnits(ParchmentSheetGeometry.SliceLeft, hd.SpritePixelsPerUnit,
                100f, 1f), hd.Borders.Left), "the sheet density does not draw the planned border");
            ParchmentSheetGeometry owner = ParchmentSheetGeometry.For(ParchmentSurfaces.FrameSheetUnitsPerTexel, 1200f, 100f);
            ParchmentSheetGeometry small = ParchmentSheetGeometry.For(ParchmentSurfaces.FrameSheetUnitsPerTexel, 720f, 100f);
            ParchmentSheetGeometry huge = ParchmentSheetGeometry.For(ParchmentSurfaces.FrameSheetUnitsPerTexel, 4320f, 100f);
            Expect(owner.ScreenScale > 1.1f && owner.ScreenScale < 1.12f && Near(small.ScreenScale, 0.666667f) &&
                Near(huge.ScreenScale, ParchmentSheetGeometry.MaximumScreenScale) &&
                Near(ParchmentSheetGeometry.For(0.5f, 300f, 100f).ScreenScale, ParchmentSheetGeometry.MinimumScreenScale),
                "the density does not follow the screen height within its bounds");
            foreach (float reference in new[] { 50f, 100f, 200f })
                Expect(Near(ParchmentSheetGeometry.For(0.5f, 1080f, reference).Borders.Top, 55f),
                    "the sheet borders depend on the canvas reference " + reference);
            ParchmentInsets frameOutsets = ParchmentSurfaces.WorkspaceFrameSheetOutsets;
            foreach (ParchmentSheetGeometry geometry in new[] { small, hd, owner })
            {
                ParchmentInsets outsets = geometry.Scaled(frameOutsets);
                // The visible edge zones end within a few units of the
                // frame's edge, under its outer margin, never under text.
                Expect(geometry.EdgeZones.Left - outsets.Left <= 2f && geometry.EdgeZones.Right - outsets.Right <= 2f &&
                    geometry.EdgeZones.Bottom - outsets.Bottom <= 6f && geometry.EdgeZones.Top - outsets.Top <= 0f,
                    "the sheet's edge zone reaches into the frame's text: " + geometry.Describe(0f, 0f));
            }
            // Corner-safe at the smallest supported frame (1280x720) and the
            // spell scroll at its fixed size.
            ParchmentInsets smallOutsets = small.Scaled(frameOutsets);
            Expect(small.DrawsUndistorted(1232f + smallOutsets.Left + smallOutsets.Right,
                636f + smallOutsets.Bottom + smallOutsets.Top), "the 1280x720 frame squashes the sheet corners");
            ParchmentSheetGeometry scroll = ParchmentSheetGeometry.For(ParchmentSurfaces.ScrollSheetUnitsPerTexel, 1200f, 100f);
            ParchmentInsets scrollOutsets = scroll.Scaled(ParchmentSurfaces.SpellScrollSheetOutsets);
            Expect(scroll.DrawsUndistorted(SpellScrollLayout.PanelWidth + scrollOutsets.Left + scrollOutsets.Right,
                    SpellScrollLayout.PanelHeight + scrollOutsets.Bottom + scrollOutsets.Top) &&
                scroll.MinimumWidth < 0.4f * SpellScrollLayout.PanelWidth &&
                scroll.EdgeZones.Top - scrollOutsets.Top <= 0f && scroll.EdgeZones.Bottom - scrollOutsets.Bottom <= 6f,
                "the spell scroll's sheet corners are squashed, oversized or under its text");
            // The writing area (2048 minus the slice columns) is drawn at no
            // more than one pixel per texel across a 1920-wide frame.
            float centreTexels = 2048f - ParchmentSheetGeometry.SliceLeft - ParchmentSheetGeometry.SliceRight;
            foreach (ParchmentSheetGeometry geometry in new[] { hd, owner })
            {
                ParchmentInsets outsets = geometry.Scaled(frameOutsets);
                float drawn = 1872f + outsets.Left + outsets.Right - geometry.MinimumWidth;
                Expect(drawn / centreTexels <= 1.05f, "the card's grain is enlarged across the frame: " +
                    (drawn / centreTexels).ToString("0.000"));
            }
            foreach (float bad in new[] { 0f, float.NaN, -2f })
                Expect(!ParchmentSheetGeometry.For(0.5f, 1080f, bad).Valid &&
                    !ParchmentSheetGeometry.For(0.5f, bad, 100f).Valid &&
                    !ParchmentSheetGeometry.For(bad, 1080f, 100f).Valid, "an unusable input drew the sheet");
        }

        // Unity-bound wiring, checked in source: the card is drawn untinted
        // through the planner-owned slice, preferred over the scroll sheet
        // with the flat look as the last fallback; the backdrop is the native
        // table with its tint as fallback; the donors are exact-validated.
        private static void TestSheetWiringInSource()
        {
            string adapter = UiSource("PlannerParchment.cs");
            Expect(adapter.Contains("Show(_paper, sliced, donor.material, 0f, 1f, 1f, Vector2.zero, Color.white);") &&
                adapter.Contains("Sprite.Create(native.texture, rect, new Vector2(0.5f, 0.5f),") &&
                adapter.Contains("geometry.SpritePixelsPerUnit, 0, SpriteMeshType.FullRect,") &&
                adapter.IndexOf("ShowSheet(sheetDonor, sliced, sheet);", StringComparison.Ordinal) <
                    adapter.IndexOf("ParchmentLayerGeometry.For(NativeSpriteContract.ScrollPaper,", StringComparison.Ordinal),
                "the card is not preferred, untinted, through the planner-owned slice");
            Expect(adapter.Contains("_image.color = Color.white;") && adapter.Contains("_image.color = _fallback;"),
                "the backdrop does not draw the table untinted with its tint as fallback");
            string theme = UiSource("PlannerNativeTheme.cs");
            Expect(theme.Contains("RequireContract((Image)values[0], NativeSpriteContract.SheetPaper);") &&
                theme.Contains("RequireContract((Image)values[0], NativeSpriteContract.TableBackdrop);"),
                "the card sheet or table is not held to its exact contract");
            string surface = UiSource("PlannerNativeThemeSurface.cs");
            Expect(surface.Contains("delegate { _sheetPaperDonor = null; RefreshParchment(); });") &&
                surface.Contains("delegate { _tableBackdropDonor = null; RefreshParchment(); });"),
                "the card sheet or table has no exact fallback binding");
            string view = UiSource("CastingWorkspaceScreenView.cs");
            Expect(view.Contains("_backdrop = new ParchmentBackdrop(blocker.GetComponent<Image>());") &&
                view.Contains("_nativeTheme.RegisterBackdrop(_backdrop);") &&
                view.Contains("ParchmentSurfaces.FrameSheetUnitsPerTexel") &&
                view.Contains("ParchmentSurfaces.ScrollSheetUnitsPerTexel"),
                "the workspace does not take the card sheet and the table");
        }

        // One native paper cue per successful closed->open transition: never
        // for an open of an already open planner, a pending retry, a failed
        // open, or anything after; a missing or failing sound player is
        // fail-soft and never counted as played.
        private static void TestWorkspaceOpenSoundCue()
        {
            int posted = 0;
            bool available = true;
            bool throws = false;
            var cue = new WorkspaceOpenSoundCue(() =>
            {
                if (throws) throw new InvalidOperationException("audio engine down");
                if (!available) return false;
                posted++;
                return true;
            });
            // Hotkey, HUD, spellbook handoff and blocked-run auto-open all
            // reach the same open path: one transition, one cue.
            Expect(cue.BeginOpen(false) && cue.CompleteOpen(true) && posted == 1 && cue.PlayedCount == 1,
                "a successful open did not post exactly one cue");
            Expect(!cue.BeginOpen(true) && !cue.CompleteOpen(true) && posted == 1,
                "opening an already open planner posted a cue");
            // Closed again; a second open posts a second cue.
            Expect(cue.BeginOpen(false) && cue.CompleteOpen(true) && posted == 2, "a reopen did not post its cue");
            // A retry while an open is pending begins nothing new.
            Expect(cue.BeginOpen(false) && !cue.BeginOpen(false) && cue.CompleteOpen(true) && posted == 3,
                "a pending open's retry posted twice");
            // A failed open posts nothing and leaves no pending transition.
            Expect(cue.BeginOpen(false) && !cue.CompleteOpen(false) && posted == 3 && !cue.CompleteOpen(true) &&
                posted == 3, "a failed open posted a cue");
            cue.BeginOpen(false);
            cue.Cancel();
            Expect(!cue.CompleteOpen(true) && posted == 3, "a cancelled open posted a cue");
            available = false;
            Expect(cue.BeginOpen(false) && !cue.CompleteOpen(true) && cue.UnavailableCount == 1 && cue.PlayedCount == 3,
                "an unavailable player was counted as played");
            available = true;
            throws = true;
            Expect(cue.BeginOpen(false) && !cue.CompleteOpen(true) && cue.LastFailure != null &&
                cue.PlayedCount == 3, "a failing player was not fail-soft");
            // The workspace open path is the only caller: begin before the
            // view exists, complete after it rendered, cancel on every
            // failure path; the native event is the character sheet's.
            string root = UiSource("BuffPlannerUiRoot.cs");
            string open = SourceBlock(root, "private bool OpenCastingWorkspace()");
            Expect(open != null &&
                open.Contains("if (!_workspaceOpenSound.BeginOpen(_castingWorkspace != null)) return false;") &&
                open.IndexOf("_castingWorkspace.RefreshView();", StringComparison.Ordinal) <
                    open.IndexOf("EmitWorkspaceOpenSound();", StringComparison.Ordinal) &&
                open.Split(new[] { "_workspaceOpenSound.Cancel();" }, StringSplitOptions.None).Length == 4,
                "the workspace open path does not bind the cue to the transition");
            Expect(root.Split(new[] { "EmitWorkspaceOpenSound();" }, StringSplitOptions.None).Length == 2 &&
                root.Contains("Game.Instance.UI.Common.UISound.Play(UISoundType.CharacterScreenOpen);"),
                "the cue is posted elsewhere or is not the native character-sheet event");
        }

        private static bool Near(float actual, float expected)
        {
            return Math.Abs(actual - expected) <= 0.001f;
        }
    }
}
