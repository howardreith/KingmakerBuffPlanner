using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
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
                    bool detected = ContainsLeaf(scan.Expression);
                    bool candidate = ability.IsSpell || detected;
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
                        IsSpell = ability.IsSpell,
                        IsCandidate = candidate,
                        HasDetectedEffect = detected,
                        CanTargetSelf = ability.CanTargetSelf,
                        CanTargetFriends = ability.CanTargetFriends,
                        CanTargetEnemies = ability.CanTargetEnemies,
                        CanTargetPoint = ability.CanTargetPoint,
                        IsStickyTouch = ability.StickyTouch != null,
                        ResourceIds = (ability.GetResourceIds() ?? new string[0])
                            .OrderBy(v => v, StringComparer.Ordinal).ToArray(),
                        Expression = scan.Expression,
                        Diagnostics = scan.Diagnostics.ToArray(),
                        Disposition = scan.Diagnostics.Count != 0
                            ? "scanner-diagnostic"
                            : detected ? "detected-effect" : "no-detected-effect"
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
                SchemaVersion = 1,
                Profile = "native-only",
                GeneratorCommit = BuildInfo.Commit,
                AbilityCount = entries.Count,
                CandidateCount = entries.Count(e => e.IsCandidate),
                DetectedEffectCount = entries.Count(e => e.HasDetectedEffect),
                DiagnosticAbilityCount = entries.Count(e => e.Diagnostics.Length != 0),
                Abilities = entries.ToArray()
            };
        }

        private static bool ContainsLeaf(EffectExpression expression)
        {
            if (expression is EffectLeafExpression) return true;
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.Any(ContainsLeaf);
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return ContainsLeaf(conditional.WhenTrue) || ContainsLeaf(conditional.WhenFalse);
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) return ContainsLeaf(targeted.Child);
            var referenced = expression as ReferencedAbilityExpression;
            return referenced != null && ContainsLeaf(referenced.Child);
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
        [JsonProperty("isSpell", Order = 7)]
        public bool IsSpell { get; set; }
        [JsonProperty("isCandidate", Order = 8)]
        public bool IsCandidate { get; set; }
        [JsonProperty("hasDetectedEffect", Order = 9)]
        public bool HasDetectedEffect { get; set; }
        [JsonProperty("canTargetSelf", Order = 10)]
        public bool CanTargetSelf { get; set; }
        [JsonProperty("canTargetFriends", Order = 11)]
        public bool CanTargetFriends { get; set; }
        [JsonProperty("canTargetEnemies", Order = 12)]
        public bool CanTargetEnemies { get; set; }
        [JsonProperty("canTargetPoint", Order = 13)]
        public bool CanTargetPoint { get; set; }
        [JsonProperty("isStickyTouch", Order = 14)]
        public bool IsStickyTouch { get; set; }
        [JsonProperty("resourceIds", Order = 15)]
        public string[] ResourceIds { get; set; }
        [JsonProperty("expression", Order = 16)]
        public EffectExpression Expression { get; set; }
        [JsonProperty("diagnostics", Order = 17)]
        public DiscoveryDiagnostic[] Diagnostics { get; set; }
        [JsonProperty("disposition", Order = 18)]
        public string Disposition { get; set; }
    }
}
