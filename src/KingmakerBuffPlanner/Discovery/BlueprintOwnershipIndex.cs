using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Discovery
{
    // One mod's declared blueprint inventory as the ownership index read it.
    public sealed class BlueprintOwnershipSource
    {
        public BlueprintOwnershipSource(string owner, string directory, string inventory, int entryCount)
        {
            Owner = owner ?? string.Empty;
            Directory = directory ?? string.Empty;
            Inventory = inventory ?? string.Empty;
            EntryCount = entryCount;
        }

        [JsonProperty("owner", Order = 1)] public string Owner { get; private set; }
        [JsonProperty("directory", Order = 2)] public string Directory { get; private set; }
        // The inventory file read, or empty for a staged mod that declares none.
        [JsonProperty("inventory", Order = 3)] public string Inventory { get; private set; }
        [JsonProperty("entryCount", Order = 4)] public int EntryCount { get; private set; }
    }

    // Which mod owns a blueprint (rc4 review finding 3). Ownership is proved
    // only by a mod's own declared inventory: a Call of the Wild library
    // loaded_blueprints.txt (Call of the Wild and the mods built on its
    // library record every blueprint they register, literal GUIDs included)
    // or the Kingmaker Gunslinger identifier manifest. A blueprint that no
    // inventory claims is native only when that is proved too - the profile
    // stages no optional mod, or every staged mod declares an inventory -
    // and otherwise "unattributed": it may be native, or come from a staged
    // mod that publishes no inventory. Never guessed native.
    internal sealed class BlueprintOwnershipIndex
    {
        internal const string Native = "native";
        internal const string Unattributed = "unattributed";
        internal const string LoadedBlueprintsFile = "loaded_blueprints.txt";
        internal static readonly string IdentifierManifestFile = Path.Combine("blueprints", "blueprints.json");

        private readonly Dictionary<string, string> _owners;
        private readonly bool _uninventoriedAreNative;

        private BlueprintOwnershipIndex(Dictionary<string, string> owners, bool uninventoriedAreNative,
            IEnumerable<BlueprintOwnershipSource> sources)
        {
            _owners = owners;
            _uninventoriedAreNative = uninventoriedAreNative;
            Sources = (sources ?? new BlueprintOwnershipSource[0]).ToArray();
        }

        internal IReadOnlyList<BlueprintOwnershipSource> Sources { get; private set; }

        // How a blueprint no inventory claims is attributed in this index.
        internal string UninventoriedBasis
        {
            get
            {
                return _uninventoriedAreNative
                    ? (Sources.Count == 0 ? "native: no optional mod is staged"
                        : "native: every staged optional mod declares a complete inventory")
                    : "unattributed: staged mods without an inventory: " + string.Join(", ",
                        Sources.Where(s => s.Inventory.Length == 0).Select(s => s.Directory).ToArray());
            }
        }

        internal static BlueprintOwnershipIndex NativeOnly()
        {
            return new BlueprintOwnershipIndex(new Dictionary<string, string>(StringComparer.Ordinal),
                true, new BlueprintOwnershipSource[0]);
        }

        // Reads the staged Mods directory. The native-only profile stages no
        // optional mod (the harness verifies the staged set), so every
        // blueprint is native.
        internal static BlueprintOwnershipIndex Load(string modsPath, string profileId,
            string selfDirectory = "KingmakerBuffPlanner")
        {
            if (profileId == "native-only") return NativeOnly();
            if (string.IsNullOrEmpty(modsPath) || !Directory.Exists(modsPath))
                throw new DirectoryNotFoundException("The Mods directory is missing: " + modsPath);
            var inventories = new List<KeyValuePair<BlueprintOwnershipSource, IEnumerable<string>>>();
            foreach (string directory in Directory.GetDirectories(modsPath)
                .OrderBy(value => value, StringComparer.Ordinal))
            {
                string name = Path.GetFileName(directory);
                if (string.Equals(name, selfDirectory, StringComparison.OrdinalIgnoreCase)) continue;
                if (!File.Exists(Path.Combine(directory, "Info.json")) &&
                    !File.Exists(Path.Combine(directory, "info.json"))) continue;
                string owner = OwnerId(name);
                string loaded = Path.Combine(directory, LoadedBlueprintsFile);
                string manifest = Path.Combine(directory, IdentifierManifestFile);
                if (File.Exists(loaded))
                {
                    string[] guids = ParseLoadedBlueprints(File.ReadAllLines(loaded)).ToArray();
                    inventories.Add(Pair(new BlueprintOwnershipSource(owner, name, LoadedBlueprintsFile,
                        guids.Length), guids));
                }
                else if (File.Exists(manifest))
                {
                    string[] guids = ParseIdentifierManifest(File.ReadAllText(manifest)).ToArray();
                    inventories.Add(Pair(new BlueprintOwnershipSource(owner, name,
                        IdentifierManifestFile.Replace('\\', '/'), guids.Length), guids));
                }
                else inventories.Add(Pair(new BlueprintOwnershipSource(owner, name, string.Empty, 0),
                    new string[0]));
            }
            if (profileId == "call-of-the-wild" && !inventories.Any(value =>
                    value.Key.Owner == "call-of-the-wild" && value.Key.Inventory.Length != 0))
                throw new FileNotFoundException("Call of the Wild blueprint ownership inventory is missing.",
                    Path.Combine(modsPath, "CallOfTheWild", LoadedBlueprintsFile));
            return FromInventories(inventories);
        }

        internal static BlueprintOwnershipIndex FromInventories(
            IEnumerable<KeyValuePair<BlueprintOwnershipSource, IEnumerable<string>>> inventories)
        {
            var owners = new Dictionary<string, string>(StringComparer.Ordinal);
            var sources = new List<BlueprintOwnershipSource>();
            foreach (KeyValuePair<BlueprintOwnershipSource, IEnumerable<string>> inventory in
                inventories ?? new KeyValuePair<BlueprintOwnershipSource, IEnumerable<string>>[0])
            {
                sources.Add(inventory.Key);
                foreach (string guid in inventory.Value ?? new string[0])
                {
                    string existing;
                    // A blueprint two inventories both claim is not proved
                    // to belong to either.
                    if (owners.TryGetValue(guid, out existing) && existing != inventory.Key.Owner)
                        owners[guid] = Unattributed;
                    else owners[guid] = inventory.Key.Owner;
                }
            }
            bool complete = sources.All(source => source.Inventory.Length != 0);
            return new BlueprintOwnershipIndex(owners, complete, sources);
        }

        // Call of the Wild library format: name<TAB>guid<TAB>type per line.
        internal static IEnumerable<string> ParseLoadedBlueprints(IEnumerable<string> lines)
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
            return guids.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        // Kingmaker Gunslinger identifier manifest: {"entries":[{"guid":...}]};
        // every identifier the mod may register, retired ones included.
        internal static IEnumerable<string> ParseIdentifierManifest(string json)
        {
            JObject document = JObject.Parse(json ?? string.Empty);
            JArray entries = document["entries"] as JArray;
            if (entries == null)
                throw new InvalidDataException("The blueprint identifier manifest has no entries array.");
            var guids = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken entry in entries)
            {
                string guid = entry is JObject ? (string)entry["guid"] : null;
                if (!IsGuid(guid))
                    throw new InvalidDataException("A blueprint identifier manifest entry has no exact guid.");
                guids.Add(guid);
            }
            if (guids.Count == 0)
                throw new InvalidDataException("Optional blueprint ownership inventory is empty.");
            return guids.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        // A mod directory name as an owner id: CallOfTheWild -> call-of-the-wild.
        internal static string OwnerId(string directoryName)
        {
            string name = directoryName ?? string.Empty;
            var builder = new StringBuilder();
            for (int index = 0; index < name.Length; index++)
            {
                char c = name[index];
                if (char.IsUpper(c) && index > 0 &&
                    (char.IsLower(name[index - 1]) || char.IsDigit(name[index - 1]) ||
                     (index + 1 < name.Length && char.IsLower(name[index + 1]))))
                    builder.Append('-');
                builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString();
        }

        // Whether an ownership value names an optional mod (not the game, and
        // not an unproved attribution).
        internal static bool IsOptionalOwner(string ownership)
        {
            return !string.IsNullOrEmpty(ownership) && ownership != Native && ownership != Unattributed;
        }

        internal string GetOwnership(string blueprintGuid)
        {
            string owner;
            if (blueprintGuid != null && _owners.TryGetValue(blueprintGuid, out owner)) return owner;
            return _uninventoriedAreNative ? Native : Unattributed;
        }

        private static KeyValuePair<BlueprintOwnershipSource, IEnumerable<string>> Pair(
            BlueprintOwnershipSource source, IEnumerable<string> guids)
        {
            return new KeyValuePair<BlueprintOwnershipSource, IEnumerable<string>>(source, guids);
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
