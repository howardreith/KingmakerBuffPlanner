using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal sealed class HarmonyPatchInventoryExporter
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal HarmonyPatchInventory Export(string profileId)
        {
            Assembly harmonyAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                string.Equals(a.GetName().Name, "0Harmony12", StringComparison.Ordinal));
            if (harmonyAssembly == null) throw new InvalidOperationException("Harmony12 assembly is not loaded.");
            Type harmonyType = harmonyAssembly.GetType("Harmony12.HarmonyInstance", true);
            MethodInfo create = harmonyType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            MethodInfo getPatchedMethods = harmonyType.GetMethod("GetPatchedMethods",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo getPatchInfo = harmonyType.GetMethod("GetPatchInfo",
                BindingFlags.Instance | BindingFlags.Public);
            if (create == null || getPatchedMethods == null || getPatchInfo == null)
                throw new MissingMethodException("Harmony12 patch inventory API is unavailable.");
            object harmony = create.Invoke(null, new object[] { "KingmakerBuffPlanner.Inventory" });

            var methods = ((IEnumerable)getPatchedMethods.Invoke(harmony, null)).Cast<MethodBase>()
                .OrderBy(GetMethodIdentity, StringComparer.Ordinal).ToList();
            var targets = new List<HarmonyPatchTarget>();
            foreach (MethodBase method in methods)
            {
                object patches = getPatchInfo.Invoke(harmony, new object[] { method });
                if (patches == null) continue;
                var records = new List<HarmonyPatchRecord>();
                AddRecords(records, patches, "prefix", "Prefixes");
                AddRecords(records, patches, "postfix", "Postfixes");
                AddRecords(records, patches, "transpiler", "Transpilers");
                records = records.OrderBy(r => r.Kind, StringComparer.Ordinal)
                    .ThenBy(r => r.Sequence).ThenBy(r => r.Owner, StringComparer.Ordinal)
                    .ThenBy(r => r.PatchMethod, StringComparer.Ordinal).ToList();
                string[] owners = records.Select(r => r.Owner).Where(o => !string.IsNullOrEmpty(o))
                    .Distinct(StringComparer.Ordinal).OrderBy(o => o, StringComparer.Ordinal).ToArray();
                targets.Add(new HarmonyPatchTarget
                {
                    Target = GetMethodIdentity(method),
                    Owners = owners,
                    IsMultiOwner = owners.Length > 1,
                    IncludesBuffPlanner = owners.Any(IsBuffPlannerOwner),
                    Patches = records
                });
            }

            return new HarmonyPatchInventory
            {
                SchemaVersion = 1,
                ProfileId = profileId,
                TargetCount = targets.Count,
                PatchCount = targets.Sum(t => t.Patches.Count),
                MultiOwnerTargetCount = targets.Count(t => t.IsMultiOwner),
                BuffPlannerOverlapTargetCount = targets.Count(t => t.IsMultiOwner && t.IncludesBuffPlanner),
                Targets = targets
            };
        }

        private static void AddRecords(
            ICollection<HarmonyPatchRecord> destination, object patches, string kind, string fieldName)
        {
            FieldInfo collectionField = patches.GetType().GetField(fieldName, Fields);
            if (collectionField == null) throw new MissingFieldException(patches.GetType().FullName, fieldName);
            int sequence = 0;
            foreach (object patch in (IEnumerable)collectionField.GetValue(patches))
            {
                destination.Add(new HarmonyPatchRecord
                {
                    Kind = kind,
                    Sequence = sequence++,
                    Index = ReadField<int>(patch, "index"),
                    Owner = ReadField<string>(patch, "owner") ?? string.Empty,
                    Priority = ReadField<int>(patch, "priority"),
                    Before = (ReadField<string[]>(patch, "before") ?? new string[0])
                        .OrderBy(v => v, StringComparer.Ordinal).ToArray(),
                    After = (ReadField<string[]>(patch, "after") ?? new string[0])
                        .OrderBy(v => v, StringComparer.Ordinal).ToArray(),
                    PatchMethod = GetMethodIdentity(ReadField<MethodInfo>(patch, "patch"))
                });
            }
        }

        private static T ReadField<T>(object instance, string name)
        {
            FieldInfo field = instance.GetType().GetField(name, Fields);
            if (field == null) throw new MissingFieldException(instance.GetType().FullName, name);
            return (T)field.GetValue(instance);
        }

        internal static string GetMethodIdentity(MethodBase method)
        {
            if (method == null) return string.Empty;
            string assembly = method.Module == null || method.Module.Assembly == null
                ? string.Empty : method.Module.Assembly.GetName().Name;
            string declaring = method.DeclaringType == null ? "<global>" : method.DeclaringType.FullName;
            string parameters = string.Join(",", method.GetParameters().Select(p =>
                p.ParameterType == null ? string.Empty : p.ParameterType.FullName).ToArray());
            return assembly + "|" + declaring + "|" + method.Name + "(" + parameters + ")";
        }

        private static bool IsBuffPlannerOwner(string owner)
        {
            return owner.IndexOf("KingmakerBuffPlanner", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    internal sealed class HarmonyPatchInventory
    {
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [JsonProperty("profileId", Order = 2)] public string ProfileId { get; set; }
        [JsonProperty("targetCount", Order = 3)] public int TargetCount { get; set; }
        [JsonProperty("patchCount", Order = 4)] public int PatchCount { get; set; }
        [JsonProperty("multiOwnerTargetCount", Order = 5)] public int MultiOwnerTargetCount { get; set; }
        [JsonProperty("buffPlannerOverlapTargetCount", Order = 6)] public int BuffPlannerOverlapTargetCount { get; set; }
        [JsonProperty("targets", Order = 7)] public List<HarmonyPatchTarget> Targets { get; set; }
    }

    internal sealed class HarmonyPatchTarget
    {
        [JsonProperty("target", Order = 1)] public string Target { get; set; }
        [JsonProperty("owners", Order = 2)] public string[] Owners { get; set; }
        [JsonProperty("isMultiOwner", Order = 3)] public bool IsMultiOwner { get; set; }
        [JsonProperty("includesBuffPlanner", Order = 4)] public bool IncludesBuffPlanner { get; set; }
        [JsonProperty("patches", Order = 5)] public List<HarmonyPatchRecord> Patches { get; set; }
    }

    internal sealed class HarmonyPatchRecord
    {
        [JsonProperty("kind", Order = 1)] public string Kind { get; set; }
        [JsonProperty("sequence", Order = 2)] public int Sequence { get; set; }
        [JsonProperty("index", Order = 3)] public int Index { get; set; }
        [JsonProperty("owner", Order = 4)] public string Owner { get; set; }
        [JsonProperty("priority", Order = 5)] public int Priority { get; set; }
        [JsonProperty("before", Order = 6)] public string[] Before { get; set; }
        [JsonProperty("after", Order = 7)] public string[] After { get; set; }
        [JsonProperty("patchMethod", Order = 8)] public string PatchMethod { get; set; }
    }
}
