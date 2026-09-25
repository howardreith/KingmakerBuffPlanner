using System;
using System.Linq;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Readable effect naming for metamagic rods. Unnamed extended-mask
    // values (for example provider-added metamagic) must never reach the UI
    // as raw integers: a qualified provider display-name contract is tried
    // through the injected resolver, then the rod item's own name yields a
    // descriptor. The numeric mask belongs in diagnostics only.
    internal static class CastEnhancementNaming
    {
        // Returns a readable effect name, or null when no readable source
        // exists (the snapshot then keeps its neutral "Metamagic" default).
        internal static string EffectDisplayName(int metamagicMask,
            string itemDisplayName, Func<int, string> namedMaskResolver)
        {
            if (metamagicMask <= 0) return null;
            string named = namedMaskResolver == null
                ? null : namedMaskResolver(metamagicMask);
            if (IsReadableName(named)) return Humanize(named);
            string derived = DeriveFromItemName(itemDisplayName);
            if (IsReadableName(derived)) return derived;
            return "Metamagic";
        }

        // A name is readable only when it carries letters and no digits:
        // enum ToString falls back to the raw numeric value precisely when
        // no named constant matches the mask.
        internal static bool IsReadableName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            bool letter = false;
            foreach (char character in value)
            {
                if (char.IsDigit(character)) return false;
                if (char.IsLetter(character)) letter = true;
            }
            return letter;
        }

        internal static string Humanize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Metamagic";
            var result = new System.Text.StringBuilder(value.Length + 4);
            for (int index = 0; index < value.Length; index++)
            {
                // Split camel humps only after a letter or digit so an
                // already-spaced combination ("Empower, Maximize") is not
                // double-spaced.
                if (index != 0 && char.IsUpper(value[index]) &&
                    char.IsLetterOrDigit(value[index - 1]))
                    result.Append(' ');
                result.Append(value[index]);
            }
            return result.ToString();
        }

        // "Lesser Persistent Metamagic Rod" -> "Persistent",
        // "Metamagic rod of threnodic spell" -> "Threnodic". Tier words
        // (lesser/normal/greater) describe the spell-level cap, which the
        // snapshot already exposes separately, so they never become the
        // effect name.
        internal static string DeriveFromItemName(string itemDisplayName)
        {
            if (string.IsNullOrWhiteSpace(itemDisplayName)) return null;
            string[] tokens = itemDisplayName.Split(
                new[] { ' ', ',', '\t', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
            var kept = new System.Collections.Generic.List<string>();
            foreach (string token in tokens)
            {
                string cleaned = new string(token.Where(char.IsLetter).ToArray());
                if (cleaned.Length == 0) continue;
                if (IsStopWord(cleaned)) continue;
                if (kept.Count != 0 && kept[kept.Count - 1].Equals(cleaned,
                        StringComparison.OrdinalIgnoreCase)) continue;
                kept.Add(char.ToUpperInvariant(cleaned[0]) + cleaned.Substring(1));
            }
            return kept.Count == 0 ? null : string.Join(" ", kept.ToArray());
        }

        private static bool IsStopWord(string token)
        {
            switch (token.ToLowerInvariant())
            {
                case "metamagic":
                case "rod":
                case "rods":
                case "lesser":
                case "normal":
                case "medium":
                case "greater":
                case "of":
                case "the":
                case "a":
                case "an":
                case "spell":
                case "spells":
                    return true;
                default:
                    return false;
            }
        }
    }
}
