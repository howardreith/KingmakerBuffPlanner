using System;

namespace KingmakerBuffPlanner.UI
{
    // Campaign donor locators. ProvenPath entries are the StaticCanvas paths
    // this mod has resolved and rendered in live campaigns since 0.0.6.
    // BoundedScan entries discover the donor structurally under a proven
    // native screen because no verified literal path is recorded yet; the
    // live inventory lane (see planning/Z-NATIVE-ASSIGNMENTS-STATUS.md) can
    // promote them to proven paths without changing any consumer.
    // CandidatePath entries are literal paths decoded from the serialized
    // in-game scene (UI_Ingame_Scene, StaticCanvas/CharacterBuild, unique at
    // every segment) that this mod has not yet rendered live; the exact
    // sprite contract, not the path, decides whether they are borrowed.
    internal static class NativeThemeResolver
    {
        internal const string PaperPath = "ServiceWindow/CharacterScreen/BookBackground";
        // The character build's colour-selector sheet: the native
        // DialogMessageBox paper (dialogue_backsheet), possibly inactive.
        internal const string ScrollPaperPath =
            "CharacterBuild/Body/Content/ClothColorSelector/PrimarySelectorPlace/ColorSelector/Background";
        // The rule under the character build's Constitution description.
        internal const string ScrollRulePath =
            "CharacterBuild/Body/Content/RaceRightSide/Constitution/DescriptionView/Decor (1)";
        internal const string ButtonPath = "ServiceWindow/CharacterScreen/LevelBox/Button_LevelUp";
        internal const string OrnamentPath = "Party/Character/Highlight";
        internal const string CharacterScreenPath = "ServiceWindow/CharacterScreen";
        internal const string InventoryPath = "ServiceWindow/Inventory";
        internal const string SpellBookPath = "ServiceWindow/SpellBook";

        internal static NativeThemeResolution Resolve(object owner, INativeThemeSource source)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            var result = new NativeThemeResolution(owner);
            var lookup = new NativeThemeDonorLookup(source);
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
            {
                NativeThemeResource resource = null;
                NativeThemeLocator locator = null;
                try
                {
                    resource = ResolveCapability(owner, lookup, source, capability, out locator);
                    source.Validate(capability, resource.Components);
                    result.Accept(capability, resource, locator);
                }
                catch (Exception exception)
                {
                    result.Reject(capability, exception.Message +
                        (resource == null ? string.Empty : "; " + resource.Identity));
                }
            }
            return result;
        }

        private static NativeThemeResource ResolveCapability(object owner,
            NativeThemeDonorLookup lookup, INativeThemeSource source,
            NativeThemeCapability capability, out NativeThemeLocator locator)
        {
            switch (capability)
            {
                case NativeThemeCapability.Paper:
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.ProvenPath, PaperPath);
                    return PathResource(owner, lookup, NativeThemeComponent.Image, PaperPath);
                case NativeThemeCapability.Buttons:
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.ProvenPath, ButtonPath);
                    return PathResource(owner, lookup, NativeThemeComponent.Button, ButtonPath);
                case NativeThemeCapability.ButtonText:
                {
                    object button = lookup.RequirePath(owner, ButtonPath);
                    object label = lookup.ScanForComponent(button,
                        NativeThemeComponent.Text, "label under " + ButtonPath);
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.BoundedScan,
                        "Text under " + ButtonPath);
                    return new NativeThemeResource
                    {
                        Nodes = new[] { button, label },
                        Components = new[]
                        {
                            lookup.RequireComponent(label, NativeThemeComponent.Text,
                                "scan", "Text under " + ButtonPath)
                        },
                        Identity = "locator=scan Text under '" + ButtonPath + "'"
                    };
                }
                case NativeThemeCapability.Body:
                {
                    object screen = lookup.RequirePath(owner, CharacterScreenPath);
                    object text = lookup.ScanForComponent(screen, NativeThemeComponent.Text,
                        "Text under " + CharacterScreenPath);
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.BoundedScan,
                        "Text under " + CharacterScreenPath);
                    return new NativeThemeResource
                    {
                        Nodes = new[] { screen, text },
                        Components = new[]
                        {
                            lookup.RequireComponent(text, NativeThemeComponent.Text,
                                "scan", "Text under " + CharacterScreenPath)
                        },
                        Identity = "locator=scan Text under '" + CharacterScreenPath + "'"
                    };
                }
                case NativeThemeCapability.Input:
                {
                    object inventory = lookup.RequirePath(owner, InventoryPath);
                    object field = lookup.ScanForComponent(inventory,
                        NativeThemeComponent.InputField, "InputField under " + InventoryPath);
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.BoundedScan,
                        "InputField under " + InventoryPath);
                    return new NativeThemeResource
                    {
                        Nodes = new[] { inventory, field },
                        Components = new[]
                        {
                            lookup.RequireComponent(field, NativeThemeComponent.InputField,
                                "scan", "InputField under " + InventoryPath)
                        },
                        Identity = "locator=scan InputField under '" + InventoryPath + "'"
                    };
                }
                case NativeThemeCapability.Scrollbar:
                {
                    object spellbook = lookup.RequirePath(owner, SpellBookPath);
                    object scrollbar = lookup.ScanForComponent(spellbook,
                        NativeThemeComponent.Scrollbar, "Scrollbar under " + SpellBookPath);
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.BoundedScan,
                        "Scrollbar under " + SpellBookPath);
                    return new NativeThemeResource
                    {
                        Nodes = new[] { spellbook, scrollbar },
                        Components = new[]
                        {
                            lookup.RequireComponent(scrollbar, NativeThemeComponent.Scrollbar,
                                "scan", "Scrollbar under " + SpellBookPath)
                        },
                        Identity = "locator=scan Scrollbar under '" + SpellBookPath + "'"
                    };
                }
                case NativeThemeCapability.Ornament:
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.ProvenPath, OrnamentPath);
                    return PathResource(owner, lookup, NativeThemeComponent.Image, OrnamentPath);
                case NativeThemeCapability.Sound:
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.BoundedScan,
                        "UICommon.UISound");
                    return source.SoundResource();
                case NativeThemeCapability.ScrollPaper:
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.CandidatePath, ScrollPaperPath);
                    return PathResource(owner, lookup, NativeThemeComponent.Image, ScrollPaperPath);
                case NativeThemeCapability.ScrollRule:
                    locator = new NativeThemeLocator(NativeThemeLocatorKind.CandidatePath, ScrollRulePath);
                    return PathResource(owner, lookup, NativeThemeComponent.Image, ScrollRulePath);
                default:
                    throw new ArgumentOutOfRangeException("capability");
            }
        }

        private static NativeThemeResource PathResource(object owner,
            NativeThemeDonorLookup lookup, NativeThemeComponent component, string path)
        {
            object node = lookup.RequirePath(owner, path);
            return new NativeThemeResource
            {
                Nodes = new[] { node },
                Components = new[] { lookup.RequireComponent(node, component, "path", path) },
                Identity = "locator=path '" + path + "'; owner=" + lookup.Location(owner)
            };
        }
    }
}
