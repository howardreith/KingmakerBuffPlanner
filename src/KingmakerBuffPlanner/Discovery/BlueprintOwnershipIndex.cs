using System;
using System.Collections.Generic;
using System.IO;

namespace KingmakerBuffPlanner.Discovery
{
    internal sealed class BlueprintOwnershipIndex
    {
        private readonly HashSet<string> _callOfTheWildGuids;

        private BlueprintOwnershipIndex(IEnumerable<string> callOfTheWildGuids)
        {
            _callOfTheWildGuids = new HashSet<string>(
                callOfTheWildGuids ?? new string[0], StringComparer.Ordinal);
        }

        internal static BlueprintOwnershipIndex NativeOnly()
        {
            return new BlueprintOwnershipIndex(new string[0]);
        }

        internal static BlueprintOwnershipIndex Load(string modsPath, string profileId)
        {
            if (profileId != "call-of-the-wild") return NativeOnly();
            string path = Path.Combine(modsPath, "CallOfTheWild", "loaded_blueprints.txt");
            if (!File.Exists(path)) throw new FileNotFoundException(
                "Call of the Wild blueprint ownership inventory is missing.", path);
            return Parse(File.ReadAllLines(path));
        }

        internal static BlueprintOwnershipIndex Parse(IEnumerable<string> lines)
        {
            var guids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string line in lines ?? new string[0])
            {
                string[] fields = (line ?? string.Empty).Split('\t');
                if (fields.Length < 3 || !IsGuid(fields[1])) continue;
                guids.Add(fields[1]);
            }
            if (guids.Count == 0)
                throw new InvalidDataException("Optional blueprint ownership inventory is empty.");
            return new BlueprintOwnershipIndex(guids);
        }

        internal string GetOwnership(string blueprintGuid)
        {
            return _callOfTheWildGuids.Contains(blueprintGuid)
                ? "call-of-the-wild" : "native";
        }

        private static bool IsGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32) return false;
            foreach (char c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }
    }
}
