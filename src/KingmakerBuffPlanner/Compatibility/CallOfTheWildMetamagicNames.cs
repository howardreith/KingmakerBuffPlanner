using System;
using System.Collections.Generic;
using System.Reflection;
using KingmakerBuffPlanner.GameAdapters;

namespace KingmakerBuffPlanner.Compatibility
{
    // Fail-soft display-name contract for extended metamagic values added by
    // Call of the Wild. The provider defines its extension values as a nested
    // literal enum (CallOfTheWild.MetamagicFeats+MetamagicExtender); the
    // installed assembly was probed offline and its constant assignments
    // verified (Persistent=0x10000000, Piercing=0x80000, Selective=0x2000000,
    // ThrenodicSpell=0x2000). No compile-time dependency exists: when the
    // provider is absent, unloaded, or reshaped, Describe returns null and
    // the caller falls back to the item-derived descriptor.
    internal static class CallOfTheWildMetamagicNames
    {
        private const string TypeName = "CallOfTheWild.MetamagicFeats+MetamagicExtender";
        private static Dictionary<int, string> _names;
        private static bool _attempted;

        internal static string ContractSummary { get; private set; }

        // Returns the readable provider name for an exact extended value, or
        // null when the provider contract is unavailable or the value is not
        // one of its named constants.
        internal static string Describe(int metamagicMask)
        {
            if (metamagicMask <= 0) return null;
            Dictionary<int, string> names = Load();
            if (names == null) return null;
            string name;
            return names.TryGetValue(metamagicMask, out name) ? name : null;
        }

        private static Dictionary<int, string> Load()
        {
            if (_attempted) return _names;
            _attempted = true;
            try
            {
                Assembly provider = null;
                foreach (Assembly candidate in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (candidate == null || candidate.IsDynamic) continue;
                    if (string.Equals(candidate.GetName().Name, "CallOfTheWild",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        provider = candidate;
                        break;
                    }
                }
                if (provider == null)
                {
                    ContractSummary = "assembly-absent";
                    return null;
                }
                Type extender = provider.GetType(TypeName, false);
                if (extender == null || !extender.IsEnum)
                {
                    ContractSummary = "extender-missing";
                    return null;
                }
                var names = new Dictionary<int, string>();
                foreach (FieldInfo field in extender.GetFields(
                    BindingFlags.Public | BindingFlags.Static))
                {
                    if (!field.IsLiteral) continue;
                    int value;
                    try { value = Convert.ToInt32(field.GetRawConstantValue()); }
                    catch (Exception) { continue; }
                    if (value == 0) continue;
                    names[value] = CleanName(field.Name);
                }
                _names = names;
                ContractSummary = "loaded:" + names.Count;
                return _names;
            }
            catch (Exception exception)
            {
                ContractSummary = "failed:" + exception.GetType().Name;
                return null;
            }
        }

        private static string CleanName(string fieldName)
        {
            // "ThrenodicSpell"/"VerdantSpell" -> "Threnodic"/"Verdant";
            // "ImprovedSpellSharing" -> "Improved Spell Sharing" at display.
            string trimmed = fieldName.EndsWith("Spell", StringComparison.Ordinal) &&
                fieldName.Length > "Spell".Length
                ? fieldName.Substring(0, fieldName.Length - "Spell".Length)
                : fieldName;
            return CastEnhancementNaming.Humanize(trimmed);
        }
    }
}
