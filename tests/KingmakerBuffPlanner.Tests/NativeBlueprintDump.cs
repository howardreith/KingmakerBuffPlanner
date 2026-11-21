using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using KingmakerBuffPlanner.Discovery;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    // Missing-beneficial-buffs mission §4: faithful native blueprint
    // fixtures. Each requested blueprint is materialized from the
    // Kingmaker 2.1.7b blueprint dump (the owner-staged, read-only
    // Blueprints2.1.7b.zip extraction; never committed) as a DETACHED real
    // Assembly-CSharp object: every serialized field is set exactly as
    // dumped - including misleading metadata such as EffectOnAlly = Harmful -
    // with no normalization. Blueprints the test does not request in full
    // are reference stubs that carry only their identity (spell lists,
    // creature types, the buffs a condition reads). Unity assets (icons,
    // prefabs) and localized text are not materialized. No Unity native
    // constructor or live Game state is invoked (cf. NativeObject<T>).
    internal sealed class NativeBlueprintDump
    {
        internal const string Root = @"C:\Dev\KingmakerBuffPlannerLab\native-blueprints-2.1.7b\extracted";
        private static Dictionary<string, string> _paths;

        private readonly HashSet<string> _full;
        private readonly Dictionary<string, BlueprintScriptableObject> _loaded =
            new Dictionary<string, BlueprintScriptableObject>(StringComparer.Ordinal);

        internal NativeBlueprintDump(params string[] fullyLoadedGuids)
        {
            _full = new HashSet<string>(fullyLoadedGuids ?? new string[0], StringComparer.Ordinal);
        }

        internal static bool Available { get { return Directory.Exists(Root); } }

        internal T Get<T>(string guid) where T : BlueprintScriptableObject
        {
            if (!_full.Contains(guid))
                throw new InvalidOperationException("Fixture blueprint not requested in full: " + guid);
            var value = Resolve(guid, typeof(T)) as T;
            if (value == null) throw new InvalidOperationException("Native dump has no " + typeof(T).Name + " " + guid);
            return value;
        }

        // The exporter's blueprint lookup over the requested blueprints: an
        // effect whose blueprint is not requested resolves to nothing, as an
        // unknown blueprint does in the game.
        internal NativeBlueprintLookup Lookup
        {
            get
            {
                return new NativeBlueprintLookup(
                    id => _full.Contains(id ?? string.Empty) ? Resolve(id, typeof(BlueprintBuff)) as BlueprintBuff : null,
                    id => _full.Contains(id ?? string.Empty)
                        ? Resolve(id, typeof(BlueprintItemEnchantment)) as BlueprintItemEnchantment : null);
            }
        }

        private static Dictionary<string, string> Paths()
        {
            if (_paths != null) return _paths;
            if (!Available)
                throw new InvalidOperationException("The native 2.1.7b blueprint dump is absent: " + Root);
            var paths = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string path in Directory.EnumerateFiles(Root, "*.json", SearchOption.AllDirectories))
            {
                string[] parts = Path.GetFileNameWithoutExtension(path).Split('.');
                if (parts.Length >= 2 && parts[parts.Length - 1].Length == 32) paths[parts[parts.Length - 1]] = path;
            }
            _paths = paths;
            return paths;
        }

        private object Resolve(string guid, Type declared)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            BlueprintScriptableObject known;
            if (_loaded.TryGetValue(guid, out known)) return declared.IsInstanceOfType(known) ? known : null;
            string path;
            Paths().TryGetValue(guid, out path);
            Type type = path == null ? declared : NativeType(Path.GetFileName(Path.GetDirectoryName(path)));
            if (type == null || type.IsAbstract || !typeof(BlueprintScriptableObject).IsAssignableFrom(type) ||
                !declared.IsAssignableFrom(type))
                return null;
            var blueprint = (BlueprintScriptableObject)Detached(type);
            _loaded[guid] = blueprint;
            if (path != null && _full.Contains(guid))
                Populate(blueprint, JObject.Parse(File.ReadAllText(path)), new Dictionary<string, object>(StringComparer.Ordinal));
            else
            {
                SetField(blueprint, "m_AssetGuid", guid);
                SetField(blueprint, "Components", new BlueprintComponent[0]);
            }
            return blueprint;
        }

        private static Type NativeType(string fullName)
        {
            string name = (fullName ?? string.Empty).Split(',')[0].Trim();
            return typeof(BlueprintScriptableObject).Assembly.GetType(name, false);
        }

        private static object Detached(Type type)
        {
            object value = FormatterServices.GetUninitializedObject(type);
            if (value is UnityEngine.Object) SetField(value, "m_CachedPtr", new IntPtr(1));
            return value;
        }

        private sealed class Skip { internal static readonly Skip Value = new Skip(); }

        private void Populate(object target, JObject json, Dictionary<string, object> ids)
        {
            foreach (JProperty property in json.Properties())
            {
                if (property.Name == "$type" || property.Name == "$id" || property.Name == "name") continue;
                FieldInfo field = Field(target.GetType(), property.Name);
                if (field == null) continue;
                object value = Convert(property.Value, field.FieldType, ids);
                if (value is Skip) continue;
                if (value == null && field.FieldType.IsValueType) continue;
                field.SetValue(target, value);
            }
        }

        private object Convert(JToken token, Type type, Dictionary<string, object> ids)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token.Type == JTokenType.String)
            {
                string text = (string)token;
                if (text.StartsWith("Blueprint:", StringComparison.Ordinal))
                {
                    if (!typeof(BlueprintScriptableObject).IsAssignableFrom(type)) return Skip.Value;
                    string[] parts = text.Split(':');
                    return parts.Length < 2 || parts[1].Length == 0 ? null : Resolve(parts[1], type);
                }
                if (text.StartsWith("LocalizedString:", StringComparison.Ordinal) ||
                    text.StartsWith("Resource:", StringComparison.Ordinal))
                    return Skip.Value;
                if (type.IsEnum) return Enum.Parse(type, text);
                if (type == typeof(string)) return text;
                return Skip.Value;
            }
            if (token.Type == JTokenType.Array)
            {
                Type element = type.IsArray ? type.GetElementType()
                    : type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
                        ? type.GetGenericArguments()[0] : null;
                if (element == null) return Skip.Value;
                var values = ((JArray)token).Select(item => Convert(item, element, ids))
                    .Select(item => item is Skip ? null : item).ToList();
                if (type.IsArray)
                {
                    Array array = Array.CreateInstance(element, values.Count);
                    for (int index = 0; index < values.Count; index++)
                        if (values[index] != null || !element.IsValueType) array.SetValue(values[index], index);
                    return array;
                }
                var list = (IList)Activator.CreateInstance(type);
                foreach (object item in values) list.Add(item);
                return list;
            }
            if (token.Type == JTokenType.Object)
            {
                var json = (JObject)token;
                JToken reference = json["$ref"];
                if (reference != null)
                {
                    object shared;
                    return ids.TryGetValue((string)reference, out shared) ? shared : Skip.Value;
                }
                Type concrete = type;
                string declaredType = (string)json["$type"];
                if (declaredType != null)
                {
                    if (!declaredType.Contains("Assembly-CSharp")) return Skip.Value;
                    concrete = NativeType(declaredType);
                }
                if (concrete == null || !type.IsAssignableFrom(concrete) || concrete.IsAbstract) return Skip.Value;
                object value;
                if (typeof(UnityEngine.Object).IsAssignableFrom(concrete)) value = Detached(concrete);
                else if (concrete.IsValueType) value = Activator.CreateInstance(concrete);
                else
                {
                    try { value = Activator.CreateInstance(concrete, true); }
                    catch (MissingMethodException) { value = FormatterServices.GetUninitializedObject(concrete); }
                }
                string id = (string)json["$id"];
                if (id != null) ids[id] = value;
                Populate(value, json, ids);
                return value;
            }
            if (type.IsEnum) return Enum.ToObject(type, token.ToObject<long>());
            if (type.IsPrimitive || type == typeof(decimal)) return token.ToObject(type);
            return Skip.Value;
        }

        private static FieldInfo Field(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            return null;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = Field(target.GetType(), name);
            if (field == null) throw new InvalidOperationException("Native member absent: " + target.GetType() + "." + name);
            field.SetValue(target, value);
        }
    }
}
