using System;
using System.IO;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal static class HeroismFixturePreparationPolicy
    {
        internal static string Refusal(string scenario, string profile, string condition, string name, string file,
            string saveCampaign, string loadedCampaign, string expectedCampaign, string path, string saveRoot)
        {
            if (!RuntimeTestProtocol.IsHeroismFixtureScenario(scenario) || profile != "full-user") return "fixture-scope";
            if (condition != "absent" && condition != "present") return "fixture-condition";
            if (name != "KBP_AUTOMATION_WORKING" || string.IsNullOrWhiteSpace(file) ||
                !System.Text.RegularExpressions.Regex.IsMatch(file, @"^Manual_[0-9]+_KBP_AUTOMATION_WORKING(?:\.zks)?$"))
                return "fixture-exact-working-descriptor-required";
            if (string.IsNullOrWhiteSpace(expectedCampaign) || saveCampaign != expectedCampaign || loadedCampaign != expectedCampaign)
                return "fixture-campaign-mismatch";
            string root = Path.GetFullPath(saveRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(path);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetDirectoryName(full), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFileNameWithoutExtension(full), Path.GetFileNameWithoutExtension(file), StringComparison.Ordinal))
                return "fixture-save-path-mismatch";
            return null;
        }
    }
}
