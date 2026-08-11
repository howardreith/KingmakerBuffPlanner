using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.GameAdapters;

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
                DetectedEffectCount = entries.Count(e => e.Disposition == "detected-effect"),
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
        public int SchemaVersion { get; set; }
        public string Profile { get; set; }
        public string GeneratorCommit { get; set; }
        public int AbilityCount { get; set; }
        public int CandidateCount { get; set; }
        public int DetectedEffectCount { get; set; }
        public int DiagnosticAbilityCount { get; set; }
        public NativeCatalogEntry[] Abilities { get; set; }
    }

    internal sealed class NativeCatalogEntry
    {
        public string AbilityGuid { get; set; }
        public string ParentGuid { get; set; }
        public string[] VariantGuids { get; set; }
        public string InternalName { get; set; }
        public string DisplayName { get; set; }
        public string SourceAssembly { get; set; }
        public bool IsSpell { get; set; }
        public bool IsCandidate { get; set; }
        public bool CanTargetSelf { get; set; }
        public bool CanTargetFriends { get; set; }
        public bool CanTargetEnemies { get; set; }
        public bool CanTargetPoint { get; set; }
        public bool IsStickyTouch { get; set; }
        public string[] ResourceIds { get; set; }
        public EffectExpression Expression { get; set; }
        public DiscoveryDiagnostic[] Diagnostics { get; set; }
        public string Disposition { get; set; }
    }
}
