using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KingmakerBuffPlanner.Discovery
{
    public sealed class SelectableAbilityBlueprint
    {
        public SelectableAbilityBlueprint(
            string blueprintGuid,
            string displayName,
            string iconIdentity,
            bool eligible)
        {
            if (string.IsNullOrWhiteSpace(blueprintGuid))
                throw new ArgumentException("Blueprint GUID is required.", "blueprintGuid");
            BlueprintGuid = blueprintGuid;
            DisplayName = displayName ?? string.Empty;
            IconIdentity = iconIdentity ?? string.Empty;
            Eligible = eligible;
        }

        public string BlueprintGuid { get; private set; }
        public string DisplayName { get; private set; }
        public string IconIdentity { get; private set; }
        public bool Eligible { get; private set; }
    }

    public sealed class SelectableAbilityEntry
    {
        internal SelectableAbilityEntry(
            SelectableAbilityBlueprint source,
            SelectableAbilityBlueprint concrete,
            int variantOrder,
            bool concreteVariant)
        {
            Source = source ?? throw new ArgumentNullException("source");
            Concrete = concrete ?? throw new ArgumentNullException("concrete");
            if (variantOrder < 0) throw new ArgumentOutOfRangeException("variantOrder");
            VariantOrder = variantOrder;
            IsConcreteVariant = concreteVariant;
            DisplayName = AbilityDisplayNameFormatter.Format(
                source.DisplayName, concrete.DisplayName, concreteVariant);
            SearchText = AbilityDisplayNameFormatter.SearchText(
                DisplayName, source.DisplayName);
            IconIdentity = AbilityDisplayNameFormatter.PreferredIcon(
                concrete.IconIdentity, source.IconIdentity);
            StableIdentity = source.BlueprintGuid + "|" +
                (concreteVariant ? concrete.BlueprintGuid : string.Empty);
        }

        public SelectableAbilityBlueprint Source { get; private set; }
        public SelectableAbilityBlueprint Concrete { get; private set; }
        public int VariantOrder { get; private set; }
        public bool IsConcreteVariant { get; private set; }
        public string DisplayName { get; private set; }
        public string SearchText { get; private set; }
        public string IconIdentity { get; private set; }
        public string StableIdentity { get; private set; }
    }

    public static class SelectableAbilityVariantCatalog
    {
        public static IReadOnlyList<SelectableAbilityEntry> Expand(
            SelectableAbilityBlueprint source,
            IEnumerable<SelectableAbilityBlueprint> declaredVariants)
        {
            if (source == null) throw new ArgumentNullException("source");
            var variants = new List<SelectableAbilityBlueprint>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (SelectableAbilityBlueprint variant in declaredVariants ??
                new SelectableAbilityBlueprint[0])
            {
                if (variant != null && seen.Add(variant.BlueprintGuid)) variants.Add(variant);
            }

            if (variants.Count == 0)
            {
                return new ReadOnlyCollection<SelectableAbilityEntry>(source.Eligible
                    ? new List<SelectableAbilityEntry>
                    {
                        new SelectableAbilityEntry(source, source, 0, false)
                    }
                    : new List<SelectableAbilityEntry>());
            }

            return new ReadOnlyCollection<SelectableAbilityEntry>(variants
                .Select((variant, order) => new { Variant = variant, Order = order })
                .Where(value => value.Variant.Eligible)
                .Select(value => new SelectableAbilityEntry(
                    source, value.Variant, value.Order, true)).ToList());
        }
    }

    public static class AbilityDisplayNameFormatter
    {
        public static string Format(
            string sourceDisplayName,
            string concreteDisplayName,
            bool concreteVariant)
        {
            string source = sourceDisplayName ?? string.Empty;
            string concrete = concreteDisplayName ?? string.Empty;
            if (concreteVariant && !string.IsNullOrWhiteSpace(concrete))
            {
                if (string.IsNullOrWhiteSpace(source)) return concrete;
                if (string.Equals(source, concrete,
                    StringComparison.OrdinalIgnoreCase)) return concrete;
                return source + " \u2014 " + DistinguishingText(source, concrete);
            }
            if (!string.IsNullOrWhiteSpace(source)) return source;
            return concrete;
        }

        public static string SearchText(string displayName, string sourceDisplayName)
        {
            string display = displayName ?? string.Empty;
            string source = sourceDisplayName ?? string.Empty;
            return string.IsNullOrWhiteSpace(source) ||
                display.IndexOf(source, StringComparison.OrdinalIgnoreCase) >= 0
                ? display
                : display + "\n" + source;
        }

        private static string DistinguishingText(string source, string concrete)
        {
            var sourceTokens = new HashSet<string>(TokenSpans(source)
                .Select(value => value.Text), StringComparer.OrdinalIgnoreCase);
            List<TokenSpan> different = TokenSpans(concrete)
                .Where(value => !sourceTokens.Contains(value.Text)).ToList();
            if (different.Count == 0) return concrete;
            int start = different[0].Start;
            int end = different[different.Count - 1].End;
            string distinction = concrete.Substring(start, end - start).Trim()
                .Trim(',', ';', ':', '-', '\u2013', '\u2014', '(', ')', '[', ']')
                .Trim();
            return string.IsNullOrWhiteSpace(distinction) ? concrete : distinction;
        }

        private static IEnumerable<TokenSpan> TokenSpans(string value)
        {
            string input = value ?? string.Empty;
            int start = -1;
            for (int index = 0; index < input.Length; index++)
            {
                if (char.IsLetterOrDigit(input[index]))
                {
                    if (start < 0) start = index;
                    if (index + 1 == input.Length)
                        yield return new TokenSpan(
                            input.Substring(start, index + 1 - start),
                            start, index + 1);
                    continue;
                }
                if (start < 0) continue;
                yield return new TokenSpan(
                    input.Substring(start, index - start), start, index);
                start = -1;
            }
        }

        private sealed class TokenSpan
        {
            internal TokenSpan(string text, int start, int end)
            {
                Text = text;
                Start = start;
                End = end;
            }

            internal string Text { get; private set; }
            internal int Start { get; private set; }
            internal int End { get; private set; }
        }

        public static string PreferredIcon(string concreteIconIdentity, string sourceIconIdentity)
        {
            return !string.IsNullOrWhiteSpace(concreteIconIdentity)
                ? concreteIconIdentity
                : sourceIconIdentity ?? string.Empty;
        }
    }
}
