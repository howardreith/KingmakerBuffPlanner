using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Discovery
{
    public sealed class EffectOverrideRegistry
    {
        private readonly Dictionary<string, EffectOverrideEntry> _entries;

        private EffectOverrideRegistry(IEnumerable<EffectOverrideEntry> entries)
        {
            _entries = entries.ToDictionary(e => e.AbilityGuid, StringComparer.Ordinal);
        }

        public static EffectOverrideRegistry Empty()
        {
            return new EffectOverrideRegistry(new EffectOverrideEntry[0]);
        }

        public static EffectOverrideRegistry Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
                throw new ArgumentException("Absolute override path is required.", "path");
            return Parse(File.ReadAllText(path));
        }

        public static EffectOverrideRegistry Parse(string json)
        {
            RejectDuplicateProperties(json);
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                TypeNameHandling = TypeNameHandling.None
            };
            EffectOverrideDocument document = JsonConvert.DeserializeObject<EffectOverrideDocument>(json, settings);
            if (document == null || document.SchemaVersion != 1 || document.Entries == null)
                throw new InvalidDataException("override-schema");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (EffectOverrideEntry entry in document.Entries)
            {
                if (entry == null || !IsGuid(entry.AbilityGuid) || !seen.Add(entry.AbilityGuid) ||
                    string.IsNullOrWhiteSpace(entry.Reason) || entry.Effects == null ||
                    (entry.SourceAssembly != "native" && entry.SourceAssembly != "optional"))
                    throw new InvalidDataException("invalid-override-entry");
                if (!new[] { "include", "exclude", "replace-detected-effects",
                    "augment-detected-effects", "unsupported-with-reason" }.Contains(entry.Disposition))
                    throw new InvalidDataException("invalid-override-disposition");
                if (entry.EffectMode != "allOf" && entry.EffectMode != "anyOf")
                    throw new InvalidDataException("invalid-override-effect-mode");
                if ((entry.Disposition == "include" ||
                    entry.Disposition == "replace-detected-effects" ||
                    entry.Disposition == "augment-detected-effects") && entry.Effects.Length == 0)
                    throw new InvalidDataException("empty-override-effects");
                foreach (EffectOverrideEffect effect in entry.Effects)
                    if (effect == null || !IsGuid(effect.Guid) ||
                        !new[] { "UnitBuff", "AreaBuff", "PrimaryWeaponEnchant",
                            "SecondaryWeaponEnchant", "ArmorOrShieldEnchant" }.Contains(effect.Kind))
                        throw new InvalidDataException("invalid-override-effect");
            }
            return new EffectOverrideRegistry(document.Entries);
        }

        public EffectOverrideApplication Apply(string abilityGuid, EffectExpression detected)
        {
            EffectOverrideEntry entry;
            if (!_entries.TryGetValue(abilityGuid, out entry))
                return new EffectOverrideApplication(detected, null);
            if (entry.Disposition == "exclude" || entry.Disposition == "unsupported-with-reason")
                return new EffectOverrideApplication(new EmptyEffectExpression(), entry);
            EffectExpression replacement = BuildExpression(entry);
            if (entry.Disposition == "augment-detected-effects")
                replacement = new SequenceEffectExpression(new[] { detected, replacement });
            return new EffectOverrideApplication(replacement, entry);
        }

        private static EffectExpression BuildExpression(EffectOverrideEntry entry)
        {
            EffectExpression[] leaves = entry.Effects.Select(effect =>
                (EffectExpression)new EffectLeafExpression(
                    ToKind(effect.Kind), effect.Guid, ToTarget(effect.Kind),
                    "EffectOverrideRegistry", "override:" + entry.AbilityGuid)).ToArray();
            if (entry.EffectMode == "allOf") return new SequenceEffectExpression(leaves);
            return BuildAlternatives(leaves, 0, entry.AbilityGuid);
        }

        private static EffectExpression BuildAlternatives(
            IReadOnlyList<EffectExpression> effects, int index, string abilityGuid)
        {
            if (index >= effects.Count) return new EmptyEffectExpression();
            if (index == effects.Count - 1) return effects[index];
            return new ConditionalEffectExpression(
                "override-anyOf:" + abilityGuid,
                effects[index], BuildAlternatives(effects, index + 1, abilityGuid));
        }

        private static EffectKind ToKind(string kind)
        {
            if (kind == "UnitBuff") return EffectKind.Buff;
            if (kind == "AreaBuff") return EffectKind.AreaBuff;
            return EffectKind.WornItemEnchantment;
        }

        private static EffectTarget ToTarget(string kind)
        {
            if (kind == "AreaBuff") return EffectTarget.AmbiguousAreaRecipients;
            return EffectTarget.CurrentTarget;
        }

        private static bool IsGuid(string value)
        {
            if (value == null || value.Length != 32) return false;
            return value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
        }

        private static void RejectDuplicateProperties(string json)
        {
            var properties = new Stack<HashSet<string>>();
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                        properties.Push(new HashSet<string>(StringComparer.Ordinal));
                    else if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if (properties.Count == 0 || !properties.Peek().Add((string)reader.Value))
                            throw new InvalidDataException("duplicate-property");
                    }
                    else if (reader.TokenType == JsonToken.EndObject) properties.Pop();
                }
            }
            if (properties.Count != 0) throw new InvalidDataException("malformed-object");
        }
    }

    public sealed class EffectOverrideApplication
    {
        internal EffectOverrideApplication(EffectExpression expression, EffectOverrideEntry entry)
        {
            Expression = expression;
            Entry = entry;
        }
        public EffectExpression Expression { get; private set; }
        public EffectOverrideEntry Entry { get; private set; }
    }

    public sealed class EffectOverrideDocument
    {
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [JsonProperty("entries", Order = 2)] public EffectOverrideEntry[] Entries { get; set; }
    }

    public sealed class EffectOverrideEntry
    {
        [JsonProperty("abilityGuid", Order = 1)] public string AbilityGuid { get; set; }
        [JsonProperty("disposition", Order = 2)] public string Disposition { get; set; }
        [JsonProperty("sourceAssembly", Order = 3)] public string SourceAssembly { get; set; }
        [JsonProperty("effectMode", Order = 4)] public string EffectMode { get; set; }
        [JsonProperty("effects", Order = 5)] public EffectOverrideEffect[] Effects { get; set; }
        [JsonProperty("reason", Order = 6)] public string Reason { get; set; }
    }

    public sealed class EffectOverrideEffect
    {
        [JsonProperty("kind", Order = 1)] public string Kind { get; set; }
        [JsonProperty("guid", Order = 2)] public string Guid { get; set; }
    }
}
