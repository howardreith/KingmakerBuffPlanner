using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Root;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Discovery
{
    internal sealed class NativeAccessibilityIndex
    {
        private readonly Dictionary<string, HashSet<string>> _sources =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<NativeSpellListRecord>> _spellLists =
            new Dictionary<string, List<NativeSpellListRecord>>(StringComparer.Ordinal);
        private readonly HashSet<string> _visitedFeatures = new HashSet<string>(StringComparer.Ordinal);

        internal static NativeAccessibilityIndex Build()
        {
            var index = new NativeAccessibilityIndex();
            ProgressionRoot progression = BlueprintRoot.Instance.Progression;
            var playerClasses = new HashSet<BlueprintCharacterClass>(
                progression.CharacterClasses ?? new BlueprintCharacterClass[0]);

            foreach (BlueprintCharacterClass characterClass in playerClasses
                .Where(c => c != null).OrderBy(c => c.AssetGuid, StringComparer.Ordinal))
            {
                string classSource = "player-class:" + characterClass.AssetGuid;
                if (characterClass.Spellbook != null && characterClass.Spellbook.SpellList != null)
                    index.AddSpellList(characterClass.Spellbook.SpellList, classSource);
                index.VisitFeature(characterClass.Progression, classSource);
                foreach (BlueprintArchetype archetype in (characterClass.Archetypes ?? new BlueprintArchetype[0])
                    .Where(a => a != null).OrderBy(a => a.AssetGuid, StringComparer.Ordinal))
                {
                    foreach (LevelEntry level in archetype.AddFeatures ?? new LevelEntry[0])
                        index.VisitLevelEntry(level, classSource + "/archetype:" + archetype.AssetGuid);
                }
            }

            foreach (BlueprintRace race in (progression.CharacterRaces ?? new BlueprintRace[0])
                .Where(r => r != null).OrderBy(r => r.AssetGuid, StringComparer.Ordinal))
            {
                foreach (BlueprintFeatureBase feature in race.Features ?? new BlueprintFeatureBase[0])
                    index.VisitFeature(feature, "player-race:" + race.AssetGuid);
            }

            index.VisitFeature(progression.FeatsProgression, "player-feats-progression");
            index.ExpandVariants();
            return index;
        }

        internal string[] GetSources(string abilityGuid)
        {
            HashSet<string> sources;
            return _sources.TryGetValue(abilityGuid, out sources)
                ? sources.OrderBy(v => v, StringComparer.Ordinal).ToArray()
                : new string[0];
        }

        internal NativeSpellListRecord[] GetSpellLists(string abilityGuid)
        {
            List<NativeSpellListRecord> records;
            return _spellLists.TryGetValue(abilityGuid, out records)
                ? records.OrderBy(r => r.SpellListGuid, StringComparer.Ordinal)
                    .ThenBy(r => r.Level).ThenBy(r => r.Source, StringComparer.Ordinal).ToArray()
                : new NativeSpellListRecord[0];
        }

        private void VisitFeature(BlueprintFeatureBase feature, string source)
        {
            if (feature == null) return;
            string visitKey = feature.AssetGuid + "|" + source;
            if (!_visitedFeatures.Add(visitKey)) return;

            var progression = feature as BlueprintProgression;
            if (progression != null)
            {
                foreach (LevelEntry level in progression.LevelEntries ?? new LevelEntry[0])
                    VisitLevelEntry(level, source + "/progression:" + progression.AssetGuid);
            }

            var selection = feature as BlueprintFeatureSelection;
            if (selection != null)
            {
                IEnumerable<BlueprintFeature> choices = (selection.AllFeatures ?? new BlueprintFeature[0])
                    .Concat(selection.Features ?? new BlueprintFeature[0]);
                foreach (BlueprintFeature choice in choices.Where(f => f != null)
                    .GroupBy(f => f.AssetGuid, StringComparer.Ordinal).Select(g => g.First())
                    .OrderBy(f => f.AssetGuid, StringComparer.Ordinal))
                    VisitFeature(choice, source + "/selection:" + selection.AssetGuid);
            }

            foreach (BlueprintComponent component in feature.ComponentsArray ?? new BlueprintComponent[0])
            {
                var addFacts = component as AddFacts;
                if (addFacts != null)
                {
                    foreach (BlueprintUnitFact fact in addFacts.Facts ?? new BlueprintUnitFact[0])
                    {
                        var ability = fact as BlueprintAbility;
                        if (ability != null) AddAbility(ability, source + "/AddFacts:" + feature.AssetGuid);
                        var nested = fact as BlueprintFeatureBase;
                        if (nested != null) VisitFeature(nested, source + "/AddFacts:" + feature.AssetGuid);
                    }
                }

                var addAbilities = component as AddAbilityToCharacterComponent;
                if (addAbilities != null)
                    foreach (BlueprintAbility ability in addAbilities.Abilities ?? new BlueprintAbility[0])
                        AddAbility(ability, source + "/AddAbilityToCharacterComponent:" + feature.AssetGuid);

                var known = component as AddKnownSpell;
                if (known != null) AddAbility(known.Spell, source + "/AddKnownSpell:" + feature.AssetGuid);
                var learned = component as LearnSpells;
                if (learned != null)
                    foreach (BlueprintAbility ability in learned.Spells ?? new BlueprintAbility[0])
                        AddAbility(ability, source + "/LearnSpells:" + feature.AssetGuid);

                var special = component as AddSpecialSpellList;
                if (special != null) AddSpellList(special.SpellList, source + "/AddSpecialSpellList:" + feature.AssetGuid);
                var archetypeSpecial = component as AddSpecialSpellListForArchetype;
                if (archetypeSpecial != null)
                    AddSpellList(archetypeSpecial.SpellList,
                        source + "/AddSpecialSpellListForArchetype:" + feature.AssetGuid);
                var learnList = component as LearnSpellList;
                if (learnList != null) AddSpellList(learnList.SpellList, source + "/LearnSpellList:" + feature.AssetGuid);
                var custom = component as AddCustomSpells;
                if (custom != null) AddSpellList(custom.SpellList, source + "/AddCustomSpells:" + feature.AssetGuid);
            }
        }

        private void VisitLevelEntry(LevelEntry level, string source)
        {
            if (level == null) return;
            foreach (BlueprintFeatureBase feature in level.Features ?? new List<BlueprintFeatureBase>())
                VisitFeature(feature, source + "/level:" + level.Level);
        }

        private void AddSpellList(BlueprintSpellList spellList, string source)
        {
            if (spellList == null) return;
            foreach (SpellLevelList level in spellList.SpellsByLevel ?? new SpellLevelList[0])
            {
                if (level == null) continue;
                foreach (BlueprintAbility ability in level.Spells ?? new List<BlueprintAbility>())
                {
                    if (ability == null) continue;
                    AddAbility(ability, source + "/spell-list:" + spellList.AssetGuid + "/level:" + level.SpellLevel);
                    List<NativeSpellListRecord> records;
                    if (!_spellLists.TryGetValue(ability.AssetGuid, out records))
                    {
                        records = new List<NativeSpellListRecord>();
                        _spellLists.Add(ability.AssetGuid, records);
                    }
                    if (!records.Any(r => r.SpellListGuid == spellList.AssetGuid &&
                        r.Level == level.SpellLevel && r.Source == source))
                        records.Add(new NativeSpellListRecord(spellList.AssetGuid,
                            spellList.name ?? string.Empty, level.SpellLevel, source));
                }
            }
        }

        private void AddAbility(BlueprintAbility ability, string source)
        {
            if (ability == null) return;
            HashSet<string> sources;
            if (!_sources.TryGetValue(ability.AssetGuid, out sources))
            {
                sources = new HashSet<string>(StringComparer.Ordinal);
                _sources.Add(ability.AssetGuid, sources);
            }
            sources.Add(source);
        }

        private void ExpandVariants()
        {
            bool changed;
            do
            {
                changed = false;
                foreach (BlueprintAbility ability in ResourcesLibrary.GetBlueprints<BlueprintAbility>()
                    .Where(a => a != null).OrderBy(a => a.AssetGuid, StringComparer.Ordinal))
                {
                    HashSet<string> parentSources;
                    if (!_sources.TryGetValue(ability.AssetGuid, out parentSources)) continue;
                    foreach (BlueprintAbility variant in ability.Variants ?? new BlueprintAbility[0])
                    {
                        if (variant == null) continue;
                        int before = GetSourceCount(variant.AssetGuid);
                        foreach (string source in parentSources.ToArray())
                            AddAbility(variant, source + "/variant-of:" + ability.AssetGuid);
                        changed |= GetSourceCount(variant.AssetGuid) != before;
                    }
                }
            } while (changed);
        }

        private int GetSourceCount(string abilityGuid)
        {
            HashSet<string> sources;
            return _sources.TryGetValue(abilityGuid, out sources) ? sources.Count : 0;
        }
    }

    internal sealed class NativeSpellListRecord
    {
        internal NativeSpellListRecord(string spellListGuid, string spellListName, int level, string source)
        {
            SpellListGuid = spellListGuid;
            SpellListName = spellListName;
            Level = level;
            Source = source;
        }

        [JsonProperty("spellListGuid", Order = 1)] public string SpellListGuid { get; private set; }
        [JsonProperty("spellListName", Order = 2)] public string SpellListName { get; private set; }
        [JsonProperty("level", Order = 3)] public int Level { get; private set; }
        [JsonProperty("source", Order = 4)] public string Source { get; private set; }
    }
}
