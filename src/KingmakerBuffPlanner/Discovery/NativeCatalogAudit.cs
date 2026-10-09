using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Discovery
{
    // One catalogue entry as the 0.4.0 catalogue audit (WP5) reads it. The
    // exporter fills it from its own entry; the audit never reads the game.
    public sealed class NativeCatalogAuditInput
    {
        public string AbilityGuid { get; set; }
        public string InternalName { get; set; }
        public string DisplayName { get; set; }
        public string Ownership { get; set; }
        public string AbilityType { get; set; }
        public bool IsSpell { get; set; }
        public bool IsCandidate { get; set; }
        public string FirstAccessibilitySource { get; set; }
        public bool CanTargetSelf { get; set; }
        public bool CanTargetFriends { get; set; }
        public bool IsStickyTouch { get; set; }
        public string SupportClass { get; set; }
        public string QualificationStatus { get; set; }
        public string ManualOverride { get; set; }
        public string Disposition { get; set; }
        public string DispositionReason { get; set; }
        public string DispositionBefore040 { get; set; }
        public string LiveDisposition { get; set; }
        public string LiveDispositionReason { get; set; }
        public string LiveDispositionBefore040 { get; set; }
        public IReadOnlyList<NativeCatalogAuditEffect> Effects { get; set; }
        public IReadOnlyList<string> Payloads { get; set; }
    }

    public sealed class NativeCatalogAuditEffect
    {
        [JsonProperty("effectGuid", Order = 1)] public string EffectGuid { get; set; }
        [JsonProperty("effectName", Order = 2)] public string EffectName { get; set; }
        [JsonProperty("kind", Order = 3)] public string Kind { get; set; }
        [JsonProperty("target", Order = 4)] public string Target { get; set; }
    }

    public sealed class NativeCatalogAuditCounts
    {
        // Static scope: abilities the exact player class/race/feat graph
        // reaches (the catalogue export's own candidates).
        [JsonProperty("staticIncludedBefore040", Order = 1)] public int StaticIncludedBefore040 { get; set; }
        [JsonProperty("staticIncluded", Order = 2)] public int StaticIncluded { get; set; }
        // Live scope: every ability classified as reachable, which is how live
        // discovery judges the abilities a party member actually has.
        [JsonProperty("liveIncludedBefore040", Order = 3)] public int LiveIncludedBefore040 { get; set; }
        [JsonProperty("liveIncluded", Order = 4)] public int LiveIncluded { get; set; }
    }

    public sealed class NativeCatalogAuditSummary
    {
        [JsonProperty("totals", Order = 1)] public NativeCatalogAuditCounts Totals { get; set; }
        [JsonProperty("byOwnership", Order = 2)]
        public SortedDictionary<string, NativeCatalogAuditCounts> ByOwnership { get; set; }
        // Entries the 0.4.0 rules removed (in either scope), by the reason
        // code that removed them.
        [JsonProperty("removedByReason", Order = 3)]
        public SortedDictionary<string, int> RemovedByReason { get; set; }
        // Entries included now that were not before (the audit only removes;
        // anything here is a defect).
        [JsonProperty("addedCount", Order = 4)] public int AddedCount { get; set; }
    }

    public sealed class NativeCatalogAuditRecord
    {
        [JsonProperty("displayName", Order = 1)] public string DisplayName { get; set; }
        [JsonProperty("internalName", Order = 2)] public string InternalName { get; set; }
        [JsonProperty("abilityGuid", Order = 3)] public string AbilityGuid { get; set; }
        [JsonProperty("ownership", Order = 4)] public string Ownership { get; set; }
        [JsonProperty("sourceKind", Order = 5)] public string SourceKind { get; set; }
        [JsonProperty("provider", Order = 6)] public string Provider { get; set; }
        [JsonProperty("scope", Order = 7)] public string Scope { get; set; }
        [JsonProperty("status", Order = 8)] public string Status { get; set; }
        [JsonProperty("inclusionRule", Order = 9)] public string InclusionRule { get; set; }
        [JsonProperty("persistentBeneficialEffects", Order = 10)]
        public NativeCatalogAuditEffect[] PersistentBeneficialEffects { get; set; }
        [JsonProperty("targetSemantics", Order = 11)] public string TargetSemantics { get; set; }
        [JsonProperty("executionSupport", Order = 12)] public string ExecutionSupport { get; set; }
        [JsonProperty("qualificationStatus", Order = 13)] public string QualificationStatus { get; set; }
        [JsonProperty("exclusionReason", Order = 14)] public string ExclusionReason { get; set; }
        [JsonProperty("manualOverride", Order = 15)] public string ManualOverride { get; set; }
        // The entry's own place in each count (rc4): the summary is exactly
        // these flags summed, so a report can regroup the counts (by a
        // resolved owner) without the game.
        [JsonProperty("staticIncludedBefore040", Order = 16)] public bool StaticIncludedBefore040 { get; set; }
        [JsonProperty("staticIncluded", Order = 17)] public bool StaticIncluded { get; set; }
        [JsonProperty("liveIncludedBefore040", Order = 18)] public bool LiveIncludedBefore040 { get; set; }
        [JsonProperty("liveIncluded", Order = 19)] public bool LiveIncluded { get; set; }
    }

    public sealed class NativeCatalogAuditOwnership
    {
        [JsonProperty("basis", Order = 1)] public string Basis { get; set; }
        [JsonProperty("sources", Order = 2)] public BlueprintOwnershipSource[] Sources { get; set; }
    }

    public sealed class NativeCatalogAuditDocument
    {
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [JsonProperty("profile", Order = 2)] public string Profile { get; set; }
        [JsonProperty("generatorCommit", Order = 3)] public string GeneratorCommit { get; set; }
        [JsonProperty("summary", Order = 4)] public NativeCatalogAuditSummary Summary { get; set; }
        [JsonProperty("ownership", Order = 5)] public NativeCatalogAuditOwnership Ownership { get; set; }
        [JsonProperty("records", Order = 6)] public NativeCatalogAuditRecord[] Records { get; set; }
    }

    public static class NativeCatalogAudit
    {
        public const string Included = "included";
        public const string Removed = "removed-by-0.4.0-audit";

        public static NativeCatalogAuditSummary Summarize(IEnumerable<NativeCatalogAuditInput> inputs)
        {
            List<NativeCatalogAuditInput> all = (inputs ?? new NativeCatalogAuditInput[0])
                .Where(value => value != null).ToList();
            var removed = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (NativeCatalogAuditInput input in all.Where(IsRemoved))
            {
                string code = RemovalCode(input);
                int count;
                removed.TryGetValue(code, out count);
                removed[code] = count + 1;
            }
            var byOwnership = new SortedDictionary<string, NativeCatalogAuditCounts>(StringComparer.Ordinal);
            foreach (IGrouping<string, NativeCatalogAuditInput> group in all
                .GroupBy(value => value.Ownership ?? string.Empty, StringComparer.Ordinal))
                byOwnership[group.Key] = Count(group);
            return new NativeCatalogAuditSummary
            {
                Totals = Count(all),
                ByOwnership = byOwnership,
                RemovedByReason = removed,
                AddedCount = all.Count(value =>
                    (StaticNow(value) && !StaticBefore(value)) || (LiveNow(value) && !LiveBefore(value)))
            };
        }

        public static NativeCatalogAuditDocument Document(string profile, string commit,
            IEnumerable<NativeCatalogAuditInput> inputs, string ownershipBasis = null,
            IEnumerable<BlueprintOwnershipSource> ownershipSources = null)
        {
            List<NativeCatalogAuditInput> all = (inputs ?? new NativeCatalogAuditInput[0])
                .Where(value => value != null).ToList();
            return new NativeCatalogAuditDocument
            {
                SchemaVersion = 1,
                Profile = profile,
                GeneratorCommit = commit,
                Summary = Summarize(all),
                Ownership = new NativeCatalogAuditOwnership
                {
                    Basis = ownershipBasis ?? string.Empty,
                    Sources = (ownershipSources ?? new BlueprintOwnershipSource[0]).ToArray()
                },
                Records = all.Where(value => LiveNow(value) || StaticNow(value) || IsRemoved(value))
                    .OrderBy(value => value.DisplayName ?? string.Empty, StringComparer.Ordinal)
                    .ThenBy(value => value.AbilityGuid, StringComparer.Ordinal)
                    .Select(Record).ToArray()
            };
        }

        private static NativeCatalogAuditRecord Record(NativeCatalogAuditInput input)
        {
            bool removed = IsRemoved(input);
            bool staticScope = input.IsCandidate;
            string reason = staticScope ? input.DispositionReason : input.LiveDispositionReason;
            var payloadIds = new HashSet<string>(input.Payloads ?? new string[0], StringComparer.Ordinal);
            NativeCatalogAuditEffect[] payloads = (input.Effects ?? new NativeCatalogAuditEffect[0])
                .Where(effect => payloadIds.Contains(effect.EffectGuid ?? string.Empty))
                .GroupBy(effect => (effect.EffectGuid ?? string.Empty) + "|" + effect.Target, StringComparer.Ordinal)
                .Select(group => group.First()).ToArray();
            var targets = payloads.Select(effect => effect.Target).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList();
            if (targets.Contains("CurrentTarget"))
                targets[targets.IndexOf("CurrentTarget")] = input.CanTargetFriends
                    ? "CurrentTarget(self-or-ally)" : "CurrentTarget(self)";
            return new NativeCatalogAuditRecord
            {
                DisplayName = input.DisplayName ?? string.Empty,
                InternalName = input.InternalName ?? string.Empty,
                AbilityGuid = input.AbilityGuid,
                Ownership = input.Ownership,
                SourceKind = (input.AbilityType ?? string.Empty) + (input.IsSpell ? "/spell" : string.Empty),
                Provider = string.IsNullOrEmpty(input.FirstAccessibilitySource)
                    ? "live-only: reachable when a party member has it" : input.FirstAccessibilitySource,
                Scope = staticScope ? "static" : "live-only",
                Status = removed ? Removed : Included,
                InclusionRule = removed ? string.Empty : Code(reason),
                PersistentBeneficialEffects = removed ? new NativeCatalogAuditEffect[0] : payloads,
                TargetSemantics = removed ? string.Empty : string.Join(",", targets.ToArray()),
                ExecutionSupport = (input.SupportClass ?? string.Empty) +
                    (input.IsStickyTouch ? "+sticky-touch" : string.Empty),
                QualificationStatus = input.QualificationStatus ?? string.Empty,
                ExclusionReason = removed ? reason ?? string.Empty : string.Empty,
                ManualOverride = input.ManualOverride ?? string.Empty,
                StaticIncludedBefore040 = StaticBefore(input),
                StaticIncluded = StaticNow(input),
                LiveIncludedBefore040 = LiveBefore(input),
                LiveIncluded = LiveNow(input)
            };
        }

        private static NativeCatalogAuditCounts Count(IEnumerable<NativeCatalogAuditInput> inputs)
        {
            List<NativeCatalogAuditInput> list = inputs.ToList();
            return new NativeCatalogAuditCounts
            {
                StaticIncludedBefore040 = list.Count(StaticBefore),
                StaticIncluded = list.Count(StaticNow),
                LiveIncludedBefore040 = list.Count(LiveBefore),
                LiveIncluded = list.Count(LiveNow)
            };
        }

        private static bool StaticBefore(NativeCatalogAuditInput value)
        {
            return value.IsCandidate && value.DispositionBefore040 == "include";
        }

        private static bool StaticNow(NativeCatalogAuditInput value)
        {
            return value.IsCandidate && value.Disposition == "include";
        }

        private static bool LiveBefore(NativeCatalogAuditInput value)
        {
            return value.LiveDispositionBefore040 == "include";
        }

        private static bool LiveNow(NativeCatalogAuditInput value)
        {
            return value.LiveDisposition == "include";
        }

        private static bool IsRemoved(NativeCatalogAuditInput value)
        {
            return (StaticBefore(value) && !StaticNow(value)) || (LiveBefore(value) && !LiveNow(value));
        }

        private static string RemovalCode(NativeCatalogAuditInput value)
        {
            return Code(StaticBefore(value) && !StaticNow(value)
                ? value.DispositionReason : value.LiveDispositionReason);
        }

        private static string Code(string reason)
        {
            int colon = (reason ?? string.Empty).IndexOf(':');
            return colon < 0 ? reason ?? string.Empty : reason.Substring(0, colon);
        }
    }
}
