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
        private static int _lastLocale = -1;
        private static string _localeName;
        internal static string Current
        {
            get
            {
                if (LocalizationManager.CurrentPack == null) return null;
                var locale = LocalizationManager.CurrentLocale;
                if ((int)locale != _lastLocale)
                {
                    _lastLocale = (int)locale;
                    _localeName = locale.ToString();
                }
                return _localeName;
            }
        }
    }
}
