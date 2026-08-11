using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.GameAdapters;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Discovery
{
    internal sealed class NativeCatalogExporter
    {
        internal NativeCatalogExport Export()
        {
            var entries = new List<NativeCatalogEntry>();
            NativeAccessibilityIndex accessibility = NativeAccessibilityIndex.Build();
            var adapter = new KingmakerActionGraphAdapter();
            var scanner = new ActionGraphScanner();
            foreach (BlueprintAbility ability in ResourcesLibrary.GetBlueprints<BlueprintAbility>()
                .Where(a => a != null)
                .OrderBy(a => a.AssetGuid, StringComparer.Ordinal)
                .ThenBy(a => a.name, StringComparer.Ordinal))
            {
                try
                {
                    DiscoveryScanResult scan = scanner.Scan(adapter.Adapt(ability));
                    bool detected = EffectExpressionAnalysis.ContainsLeaf(scan.Expression);
                    string[] accessibilitySources = accessibility.GetSources(ability.AssetGuid);
                    NativeSpellListRecord[] spellLists = accessibility.GetSpellLists(ability.AssetGuid);
                    bool candidate = accessibilitySources.Length != 0;
                    NativeEffectRecord[] effects = GetEffects(scan.Expression);
                    entries.Add(new NativeCatalogEntry
                    {
                        AbilityGuid = ability.AssetGuid,
                        ParentGuid = ability.Parent == null ? string.Empty : ability.Parent.AssetGuid,
                        VariantGuids = (ability.Variants ?? new BlueprintAbility[0])
                            .Where(v => v != null).Select(v => v.AssetGuid)
                            .OrderBy(v => v, StringComparer.Ordinal).ToArray(),
                        InternalName = ability.name ?? string.Empty,
                        DisplayName = ability.Name ?? string.Empty,
                        SourceAssembly = ability.GetType().Assembly.FullName,
                        Ownership = "native",
                        IsSpell = ability.IsSpell,
                        IsCandidate = candidate,
                        HasDetectedEffect = detected,
                        AbilityType = ability.Type.ToString(),
                        ActionType = ability.ActionType.ToString(),
                        Range = ability.Range.ToString(),
                        EffectOnAlly = ability.EffectOnAlly.ToString(),
                        EffectOnEnemy = ability.EffectOnEnemy.ToString(),
                        CanTargetSelf = ability.CanTargetSelf,
                        CanTargetFriends = ability.CanTargetFriends,
                        CanTargetEnemies = ability.CanTargetEnemies,
                        CanTargetPoint = ability.CanTargetPoint,
                        IsStickyTouch = ability.StickyTouch != null,
                        IsMass = effects.Any(e => e.Target == EffectTarget.Party.ToString()),
                        IsArea = effects.Any(e => e.Kind == EffectKind.AreaBuff.ToString() ||
                            e.Target == EffectTarget.AreaRecipients.ToString()),
                        AccessibilitySources = accessibilitySources,
                        SpellLists = spellLists,
                        ResourceIds = (ability.GetResourceIds() ?? new string[0])
                            .OrderBy(v => v, StringComparer.Ordinal).ToArray(),
                        MaterialItemGuid = ability.MaterialComponent.Item == null
                            ? string.Empty : ability.MaterialComponent.Item.AssetGuid,
                        MaterialCount = ability.MaterialComponent.Item == null
                            ? 0 : ability.MaterialComponent.Count,
                        RecognizedActionContracts = effects.Select(e => e.SourceContract)
                            .Where(v => !string.IsNullOrEmpty(v)).Distinct(StringComparer.Ordinal)
                            .OrderBy(v => v, StringComparer.Ordinal).ToArray(),
                        Effects = effects,
                        Expression = scan.Expression,
                        Diagnostics = scan.Diagnostics.ToArray(),
                        Disposition = GetPreliminaryDisposition(candidate, effects, scan.Diagnostics.Count),
                        DispositionReason = GetPreliminaryReason(candidate, effects, scan.Diagnostics.Count)
                    });
                }
                catch (Exception exception)
                {
                    entries.Add(new NativeCatalogEntry
                    {
                        AbilityGuid = ability.AssetGuid,
                        InternalName = ability.name ?? string.Empty,
                        DisplayName = ability.Name ?? string.Empty,
                        SourceAssembly = ability.GetType().Assembly.FullName,
                        VariantGuids = new string[0],
                        ResourceIds = new string[0],
                        Expression = new EmptyEffectExpression(),
                        Diagnostics = new[]
                        {
                            new DiscoveryDiagnostic("scanner-exception", ability.AssetGuid,
                                exception.GetType().FullName + ": " + exception.Message)
                        },
                        Disposition = "scanner-exception"
                    });
                }
            }

            return new NativeCatalogExport
            {
                SchemaVersion = 2,
                Profile = "native-only",
                GeneratorCommit = BuildInfo.Commit,
                AbilityCount = entries.Count,
                CandidateCount = entries.Count(e => e.IsCandidate),
                DetectedEffectCount = entries.Count(e => e.HasDetectedEffect),
                DiagnosticAbilityCount = entries.Count(e => e.Diagnostics.Length != 0),
                Abilities = entries.ToArray()
            };
        }

        private static NativeEffectRecord[] GetEffects(EffectExpression expression)
        {
            var leaves = new List<EffectLeafExpression>();
            CollectLeaves(expression, leaves);
            return leaves.Select(leaf =>
            {
                bool? harmful = null;
                string name = string.Empty;
                if (leaf.Kind == EffectKind.Buff || leaf.Kind == EffectKind.AreaBuff)
                {
                    var buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(leaf.EffectId);
                    if (buff != null)
                    {
                        harmful = buff.Harmful;
                        name = buff.name ?? string.Empty;
                    }
                }
                else if (leaf.Kind == EffectKind.WornItemEnchantment)
                {
                    var enchantment = ResourcesLibrary.TryGetBlueprint<BlueprintItemEnchantment>(leaf.EffectId);
                    if (enchantment != null) name = enchantment.name ?? string.Empty;
                }
                return new NativeEffectRecord
                {
                    Kind = leaf.Kind.ToString(),
                    EffectGuid = leaf.EffectId,
                    EffectName = name,
                    Target = leaf.Target.ToString(),
                    Harmful = harmful,
                    SourceContract = leaf.SourceContract,
                    ActionPath = leaf.ActionPath
                };
            }).OrderBy(e => e.ActionPath, StringComparer.Ordinal)
                .ThenBy(e => e.EffectGuid, StringComparer.Ordinal).ToArray();
        }

        private static void CollectLeaves(EffectExpression expression, List<EffectLeafExpression> leaves)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) { leaves.Add(leaf); return; }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children) CollectLeaves(child, leaves);
                return;
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                CollectLeaves(conditional.WhenTrue, leaves);
                CollectLeaves(conditional.WhenFalse, leaves);
                return;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) { CollectLeaves(targeted.Child, leaves); return; }
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null) CollectLeaves(referenced.Child, leaves);
        }

        private static string GetPreliminaryDisposition(
            bool candidate, NativeEffectRecord[] effects, int diagnosticCount)
        {
            if (!candidate) return "not-player-accessible";
            if (effects.Length == 0)
                return diagnosticCount == 0 ? "exclude" : "audit-unknown-action-no-effect";
            if (effects.All(e => e.Harmful == true)) return "exclude";
            if (effects.Any(e => e.Harmful == null) ||
                (effects.Any(e => e.Harmful == true) && effects.Any(e => e.Harmful == false)))
                return "audit-mixed-or-unresolved";
            return diagnosticCount == 0 ? "include-structural" : "audit-diagnostic-adjunct";
        }

        private static string GetPreliminaryReason(
            bool candidate, NativeEffectRecord[] effects, int diagnosticCount)
        {
            if (!candidate) return "Not reachable from native player class spellbooks, class/archetype progression, race features, or feat progression.";
            if (effects.Length == 0)
                return diagnosticCount == 0
                    ? "No persistent buff, area-buff, or worn-item enchantment effect was detected."
                    : "No persistent effect was detected, but unsupported action nodes require exception audit.";
            if (effects.All(e => e.Harmful == true))
                return "All detected persistent unit/area buffs are marked harmful by the exact native blueprint contract.";
            if (effects.Any(e => e.Harmful == null))
                return "At least one persistent effect has no BlueprintBuff harmful-polarity contract.";
            if (effects.Any(e => e.Harmful == true))
                return "The graph contains both harmful and beneficial persistent effects; safe branch semantics require audit.";
            return diagnosticCount == 0
                ? "Player-accessible graph contains only resolved non-harmful persistent buff effects."
                : "Resolved non-harmful persistent effects coexist with unsupported action nodes; adjunct semantics require audit.";
        }

    }

    internal sealed class NativeCatalogExport
    {
        [JsonProperty("schemaVersion", Order = 1)]
        public int SchemaVersion { get; set; }
        [JsonProperty("profile", Order = 2)]
        public string Profile { get; set; }
        [JsonProperty("generatorCommit", Order = 3)]
        public string GeneratorCommit { get; set; }
        [JsonProperty("abilityCount", Order = 4)]
        public int AbilityCount { get; set; }
        [JsonProperty("candidateCount", Order = 5)]
        public int CandidateCount { get; set; }
        [JsonProperty("detectedEffectCount", Order = 6)]
        public int DetectedEffectCount { get; set; }
        [JsonProperty("diagnosticAbilityCount", Order = 7)]
        public int DiagnosticAbilityCount { get; set; }
        [JsonProperty("abilities", Order = 8)]
        public NativeCatalogEntry[] Abilities { get; set; }
    }

    internal sealed class NativeCatalogEntry
    {
        [JsonProperty("abilityGuid", Order = 1)]
        public string AbilityGuid { get; set; }
        [JsonProperty("parentGuid", Order = 2)]
        public string ParentGuid { get; set; }
        [JsonProperty("variantGuids", Order = 3)]
        public string[] VariantGuids { get; set; }
        [JsonProperty("internalName", Order = 4)]
        public string InternalName { get; set; }
        [JsonProperty("displayName", Order = 5)]
        public string DisplayName { get; set; }
        [JsonProperty("sourceAssembly", Order = 6)]
        public string SourceAssembly { get; set; }
        [JsonProperty("ownership", Order = 7)]
        public string Ownership { get; set; }
        [JsonProperty("isSpell", Order = 8)]
        public bool IsSpell { get; set; }
        [JsonProperty("isCandidate", Order = 9)]
        public bool IsCandidate { get; set; }
        [JsonProperty("hasDetectedEffect", Order = 10)]
        public bool HasDetectedEffect { get; set; }
        [JsonProperty("abilityType", Order = 11)] public string AbilityType { get; set; }
        [JsonProperty("actionType", Order = 12)] public string ActionType { get; set; }
        [JsonProperty("range", Order = 13)] public string Range { get; set; }
        [JsonProperty("effectOnAlly", Order = 14)] public string EffectOnAlly { get; set; }
        [JsonProperty("effectOnEnemy", Order = 15)] public string EffectOnEnemy { get; set; }
        [JsonProperty("canTargetSelf", Order = 16)]
        public bool CanTargetSelf { get; set; }
        [JsonProperty("canTargetFriends", Order = 17)]
        public bool CanTargetFriends { get; set; }
        [JsonProperty("canTargetEnemies", Order = 18)]
        public bool CanTargetEnemies { get; set; }
        [JsonProperty("canTargetPoint", Order = 19)]
        public bool CanTargetPoint { get; set; }
        [JsonProperty("isStickyTouch", Order = 20)]
        public bool IsStickyTouch { get; set; }
        [JsonProperty("isMass", Order = 21)] public bool IsMass { get; set; }
        [JsonProperty("isArea", Order = 22)] public bool IsArea { get; set; }
        [JsonProperty("accessibilitySources", Order = 23)] public string[] AccessibilitySources { get; set; }
        [JsonProperty("spellLists", Order = 24)] public NativeSpellListRecord[] SpellLists { get; set; }
        [JsonProperty("resourceIds", Order = 25)]
        public string[] ResourceIds { get; set; }
        [JsonProperty("materialItemGuid", Order = 26)] public string MaterialItemGuid { get; set; }
        [JsonProperty("materialCount", Order = 27)] public int MaterialCount { get; set; }
        [JsonProperty("recognizedActionContracts", Order = 28)] public string[] RecognizedActionContracts { get; set; }
        [JsonProperty("effects", Order = 29)] public NativeEffectRecord[] Effects { get; set; }
        [JsonProperty("expression", Order = 30)]
        public EffectExpression Expression { get; set; }
        [JsonProperty("diagnostics", Order = 31)]
        public DiscoveryDiagnostic[] Diagnostics { get; set; }
        [JsonProperty("disposition", Order = 32)]
        public string Disposition { get; set; }
        [JsonProperty("dispositionReason", Order = 33)] public string DispositionReason { get; set; }
    }

    internal sealed class NativeEffectRecord
    {
        [JsonProperty("kind", Order = 1)] public string Kind { get; set; }
        [JsonProperty("effectGuid", Order = 2)] public string EffectGuid { get; set; }
        [JsonProperty("effectName", Order = 3)] public string EffectName { get; set; }
        [JsonProperty("target", Order = 4)] public string Target { get; set; }
        [JsonProperty("harmful", Order = 5)] public bool? Harmful { get; set; }
        [JsonProperty("sourceContract", Order = 6)] public string SourceContract { get; set; }
        [JsonProperty("actionPath", Order = 7)] public string ActionPath { get; set; }
    }
}
