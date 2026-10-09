using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal sealed class BlueprintReferenceConsumer
    {
        [JsonProperty("guid", Order = 1)] public string Guid { get; set; }
        [JsonProperty("name", Order = 2)] public string Name { get; set; }
        [JsonProperty("type", Order = 3)] public string Type { get; set; }
        [JsonProperty("path", Order = 4)] public string Path { get; set; }
    }

    internal sealed class BlueprintReferenceTarget
    {
        [JsonProperty("effectGuid", Order = 1)] public string EffectGuid { get; set; }
        [JsonProperty("effectName", Order = 2)] public string EffectName { get; set; }
        [JsonProperty("why", Order = 3)] public string Why { get; set; }
        [JsonProperty("consumerCount", Order = 4)] public int ConsumerCount { get; set; }
        [JsonProperty("consumers", Order = 5)] public BlueprintReferenceConsumer[] Consumers { get; set; }
    }

    internal sealed class BlueprintReferenceDocument
    {
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [JsonProperty("profile", Order = 2)] public string Profile { get; set; }
        [JsonProperty("generatorCommit", Order = 3)] public string GeneratorCommit { get; set; }
        [JsonProperty("scannedBlueprints", Order = 4)] public int ScannedBlueprints { get; set; }
        [JsonProperty("failedBlueprints", Order = 5)] public string[] FailedBlueprints { get; set; }
        [JsonProperty("targets", Order = 6)] public BlueprintReferenceTarget[] Targets { get; set; }
    }

    // Which blueprints refer to a set of buffs (rc4 review finding 1): for a
    // buff with no mechanics of its own, the blueprints that read it - an
    // alchemist bomb's conditional on Targeted Bomb Admixture's buff, an
    // ability's caster or target check - are where its ongoing behaviour
    // lives. A bounded, read-only walk of every blueprint's serialized fields,
    // its components and its action and condition elements; it stops at every
    // other blueprint it references. Diagnostic evidence for the catalogue
    // audit only: the classifier never reads it.
    internal static class BlueprintReferenceIndex
    {
        private const int MaximumDepth = 16;
        private const int MaximumItems = 4096;
        private const int MaximumConsumersPerTarget = 64;
        private static readonly Dictionary<Type, FieldInfo[]> Fields = new Dictionary<Type, FieldInfo[]>();

        internal static BlueprintReferenceDocument Build(string profile, string commit,
            IDictionary<string, KeyValuePair<string, string>> targets)
        {
            var found = new Dictionary<string, List<BlueprintReferenceConsumer>>(StringComparer.Ordinal);
            foreach (string guid in targets.Keys) found[guid] = new List<BlueprintReferenceConsumer>();
            var failed = new List<string>();
            int scanned = 0;
            foreach (BlueprintScriptableObject blueprint in ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values
                .Where(value => value != null).Distinct()
                .OrderBy(value => value.AssetGuid, StringComparer.Ordinal))
            {
                scanned++;
                try
                {
                    var visited = new HashSet<object>(Discovery.ReferenceEqualityComparer<object>.Instance);
                    Walk(blueprint, blueprint, new List<string>(), 0, visited, found);
                }
                catch (Exception exception)
                {
                    failed.Add(blueprint.AssetGuid + ":" + exception.GetType().Name);
                }
            }
            return new BlueprintReferenceDocument
            {
                SchemaVersion = 1,
                Profile = profile,
                GeneratorCommit = commit,
                ScannedBlueprints = scanned,
                FailedBlueprints = failed.ToArray(),
                Targets = targets.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
                {
                    List<BlueprintReferenceConsumer> consumers = found[pair.Key];
                    return new BlueprintReferenceTarget
                    {
                        EffectGuid = pair.Key,
                        EffectName = pair.Value.Key,
                        Why = pair.Value.Value,
                        ConsumerCount = consumers.Count,
                        Consumers = consumers.OrderBy(c => c.Guid, StringComparer.Ordinal)
                            .ThenBy(c => c.Path, StringComparer.Ordinal)
                            .Take(MaximumConsumersPerTarget).ToArray()
                    };
                }).ToArray()
            };
        }

        // The path is kept as segments and only joined for a reference found.
        private static void Walk(BlueprintScriptableObject root, object value, List<string> path, int depth,
            HashSet<object> visited, Dictionary<string, List<BlueprintReferenceConsumer>> found)
        {
            if (value == null || depth > MaximumDepth) return;
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal || value is Type ||
                value is Delegate)
                return;
            var blueprint = value as BlueprintScriptableObject;
            if (blueprint != null && !ReferenceEquals(blueprint, root))
            {
                List<BlueprintReferenceConsumer> consumers;
                if (blueprint.AssetGuid != null && found.TryGetValue(blueprint.AssetGuid, out consumers))
                    consumers.Add(new BlueprintReferenceConsumer
                    {
                        Guid = root.AssetGuid,
                        Name = root.name ?? string.Empty,
                        Type = root.GetType().FullName,
                        Path = string.Concat(path.ToArray())
                    });
                return;
            }
            // Only the root blueprint, its components and its action and
            // condition elements are walked into; every other Unity object
            // (sprites, shared strings, prefabs) is a leaf.
            if (value is UnityEngine.Object && blueprint == null && !(value is BlueprintComponent) &&
                !(value is Element))
                return;
            if (!type.IsValueType && !visited.Add(value)) return;
            var sequence = value as IEnumerable;
            if (sequence != null)
            {
                int index = 0;
                foreach (object item in sequence)
                {
                    if (index >= MaximumItems) break;
                    if (item != null && !item.GetType().IsPrimitive && !(item is string))
                    {
                        path.Add("[" + index + "]:" + item.GetType().Name);
                        Walk(root, item, path, depth + 1, visited, found);
                        path.RemoveAt(path.Count - 1);
                    }
                    index++;
                }
                return;
            }
            foreach (FieldInfo field in FieldsOf(type))
            {
                path.Add("." + field.Name);
                Walk(root, field.GetValue(value), path, depth + 1, visited, found);
                path.RemoveAt(path.Count - 1);
            }
        }

        private static FieldInfo[] FieldsOf(Type type)
        {
            FieldInfo[] fields;
            if (Fields.TryGetValue(type, out fields)) return fields;
            var list = new List<FieldInfo>();
            for (Type current = type; current != null && current != typeof(object) &&
                current != typeof(UnityEngine.Object) && current != typeof(UnityEngine.ScriptableObject);
                current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    // A prototype link points into another blueprint's data.
                    if (field.Name == "PrototypeLink") continue;
                    if (field.FieldType.IsPointer) continue;
                    list.Add(field);
                }
            }
            fields = list.ToArray();
            Fields[type] = fields;
            return fields;
        }
    }
}
