using Kingmaker.Localization;
using UnityModManagerNet;

namespace KingmakerBuffPlanner.GameAdapters
{
    public sealed class PlannerLanguageSettings : UnityModManager.ModSettings
    {
        public string Language = PlannerLanguage.Automatic;

        public override void Save(UnityModManager.ModEntry entry) { Save(this, entry); }
    }

    internal static class KingmakerLanguage
    {
        internal static string Current
        {
            get { return LocalizationManager.CurrentPack == null ? null : LocalizationManager.CurrentLocale.ToString(); }
        }
    }
}
