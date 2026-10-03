using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Non-casting inspection of a loaded campaign copy (the advanced-copy
    // family first, the automation fixture as its smoke test): campaign and
    // area identity, the roster with classes and pets, every discovered
    // resource pool and provider, enhancements, live effects and the
    // casting-first catalogue shape. Read-only: nothing here authors,
    // saves, submits or changes game state.
    internal static class AdvancedCopyInspection
    {
        internal static JObject Collect(CastingWorkspaceInputs inputs)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            PartyProviderSnapshot snapshot = inputs.Snapshot;
            var root = new JObject
            {
                { "schemaVersion", 1 },
                { "campaign", Campaign() },
                { "roster", Roster(snapshot) },
                { "resourcePools", new JArray(snapshot.ResourcePools.Select(pool => (object)new JObject
                    {
                        { "poolKey", pool.PoolKey },
                        { "kind", pool.Kind.ToString() },
                        { "capacity", pool.Capacity },
                        { "remaining", pool.Remaining },
                        { "tokens", pool.Tokens.Count },
                        { "availableTokens", pool.Tokens.Count(token => token.Available) }
                    }).ToArray()) },
                { "providers", new JArray(snapshot.Providers.Select(provider => (object)new JObject
                    {
                        { "provider", provider.Key.Canonical },
                        { "caster", provider.Key.CasterUnitId },
                        { "name", provider.DisplayName },
                        { "sourceKind", provider.Key.Ability.SourceKind.ToString() },
                        { "spellLevel", provider.SpellLevel },
                        { "pool", provider.ResourcePoolKey },
                        { "unitsPerCast", provider.UnitsPerCast },
                        { "casterLevel", provider.EffectiveCasterLevel },
                        { "metamagicMask", provider.Key.Ability.MetamagicMask },
                        { "material", provider.MaterialComponent == null ? null
                            : provider.MaterialComponent.ItemGuid + "x" + provider.MaterialComponent.RequiredCount }
                    }).ToArray()) },
                { "providerOptions", inputs.ProviderOptions.Count },
                { "buffSources", inputs.EffectsBySource.Count },
                { "enhancements", new JArray(inputs.Enhancements.Select(enhancement => (object)new JObject
                    {
                        { "id", enhancement.EnhancementId },
                        { "name", enhancement.DisplayName },
                        { "caster", enhancement.CasterUnitId },
                        { "category", enhancement.Category.ToString() },
                        { "metamagicMask", enhancement.MetamagicMask },
                        { "remainingUses", enhancement.RemainingUses },
                        { "requiresNativeCommand", enhancement.RequiresNativeCommand }
                    }).ToArray()) },
                { "liveEffects", LiveEffects(snapshot, inputs.LiveEffects) }
            };
            return root;
        }

        private static JObject Campaign()
        {
            Game game = Game.Instance;
            var area = game == null ? null : game.CurrentlyLoadedArea;
            return new JObject
            {
                { "gameId", game == null || game.Player == null ? null : game.Player.GameId },
                { "areaGuid", area == null ? null : area.AssetGuid },
                { "areaName", area == null ? null : SafeString(() => area.AreaDisplayName) },
                { "mode", game == null ? null : game.CurrentMode.ToString() },
                { "partyLevel", game == null || game.Player == null ? 0 : game.Player.PartyLevel }
            };
        }

        private static JArray Roster(PartyProviderSnapshot snapshot)
        {
            var byId = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
            if (Game.Instance != null && Game.Instance.Player != null)
                foreach (UnitEntityData unit in Game.Instance.Player.Party ?? new List<UnitEntityData>())
                {
                    if (unit == null || string.IsNullOrEmpty(unit.UniqueId)) continue;
                    byId[unit.UniqueId] = unit;
                    UnitEntityData pet = unit.Descriptor == null ? null : unit.Descriptor.Pet;
                    if (pet != null && !string.IsNullOrEmpty(pet.UniqueId)) byId[pet.UniqueId] = pet;
                }
            var roster = new JArray();
            foreach (UnitSnapshot unit in snapshot.Units)
            {
                UnitEntityData native;
                byId.TryGetValue(unit.UnitId, out native);
                roster.Add(new JObject
                {
                    { "unitId", unit.UnitId },
                    { "name", unit.DisplayName },
                    { "isPet", unit.IsPet },
                    { "master", unit.MasterUnitId },
                    { "alive", unit.TargetValidation.Alive },
                    { "conscious", unit.TargetValidation.Conscious },
                    { "characterLevel", native == null || native.Descriptor == null ? 0
                        : SafeInt(() => native.Descriptor.Progression.CharacterLevel) },
                    { "classes", Classes(native) },
                    { "providers", snapshot.Providers.Count(provider => string.Equals(
                        provider.Key.CasterUnitId, unit.UnitId, StringComparison.Ordinal)) }
                });
            }
            return roster;
        }

        private static JArray Classes(UnitEntityData unit)
        {
            var classes = new JArray();
            if (unit == null || unit.Descriptor == null || unit.Descriptor.Progression == null)
                return classes;
            foreach (ClassData data in unit.Descriptor.Progression.Classes ?? new List<ClassData>())
            {
                if (data == null || data.CharacterClass == null) continue;
                classes.Add(new JObject
                {
                    { "class", SafeString(() => data.CharacterClass.Name) },
                    { "guid", data.CharacterClass.AssetGuid },
                    { "level", data.Level }
                });
            }
            return classes;
        }

        private static JObject LiveEffects(PartyProviderSnapshot snapshot,
            ActiveEffectSnapshot live)
        {
            var result = new JObject();
            if (live == null) return result;
            foreach (UnitSnapshot unit in snapshot.Units)
            {
                IReadOnlyList<ActiveEffectInstance> instances = live.GetInstances(unit.UnitId);
                result[unit.UnitId] = new JObject
                {
                    { "instances", instances.Count },
                    { "timed", instances.Count(value => value.RemainingRounds != null) },
                    { "suppressed", instances.Count(value => value.Suppressed) },
                    // Review of rc4: an unreadable caster level or suppression
                    // flag never proves an existing effect sufficient; these
                    // show how often the game leaves one unreadable (worn
                    // enchantments carry no caster level by design).
                    { "buffCasterLevelUnread", instances.Count(value =>
                        value.Kind != EffectKind.WornItemEnchantment && value.CasterLevel == null) },
                    { "suppressionUnread", instances.Count(value => !value.SuppressionReadable) }
                };
            }
            return result;
        }

        private static string SafeString(Func<string> read)
        {
            try { return read(); }
            catch (Exception exception) { return "unreadable:" + exception.GetType().Name; }
        }

        private static int SafeInt(Func<int> read)
        {
            try { return read(); }
            catch (Exception) { return -1; }
        }
    }
}
