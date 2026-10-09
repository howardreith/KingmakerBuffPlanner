using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.GameAdapters;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Discovery
{
    internal sealed class NativeCatalogExporter
    {
        private readonly EffectOverrideRegistry _overrides;
        private readonly string _profileId;
        private readonly BlueprintOwnershipIndex _ownership;

        internal NativeCatalogExporter(
            EffectOverrideRegistry overrides = null,
            string profileId = "native-only",
            BlueprintOwnershipIndex ownership = null)
        {
            _overrides = overrides ?? EffectOverrideRegistry.Empty();
            _profileId = profileId ?? "native-only";
            _ownership = ownership ?? BlueprintOwnershipIndex.NativeOnly();
        }

        internal NativeCatalogExport Export()
        {
            var entries = new List<NativeCatalogEntry>();
            NativeAccessibilityIndex accessibility = NativeAccessibilityIndex.Build();
            var classifier = new NativeCandidateClassifier();
            var before040 = new NativeCandidateClassifier(NativeCandidateRuleSet.Pre040);
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
                    EffectOverrideApplication overrideApplication =
                        _overrides.Apply(ability.AssetGuid, scan.Expression);
                    EffectExpression effectiveExpression = overrideApplication.Expression;
                    bool detected = EffectExpressionAnalysis.ContainsLeaf(effectiveExpression);
                    string[] accessibilitySources = accessibility.GetSources(ability.AssetGuid);
                    NativeSpellListRecord[] spellLists = accessibility.GetSpellLists(ability.AssetGuid);
                    bool candidate = accessibilitySources.Length != 0;
                    NativeEffectRecord[] effects = GetEffects(effectiveExpression);
                    var entry = new NativeCatalogEntry
                    {
                        AbilityGuid = ability.AssetGuid,
                        ParentGuid = ability.Parent == null ? string.Empty : ability.Parent.AssetGuid,
                        VariantGuids = (ability.Variants ?? new BlueprintAbility[0])
                            .Where(v => v != null).Select(v => v.AssetGuid).ToArray(),
                        InternalName = ability.name ?? string.Empty,
                        DisplayName = ability.Name ?? string.Empty,
                        SourceAssembly = ability.GetType().Assembly.FullName,
                        Ownership = _ownership.GetOwnership(ability.AssetGuid),
                        IsSpell = ability.IsSpell,
                        IsCandidate = candidate,
                        HasDetectedEffect = detected,
                        AbilityType = ability.Type.ToString(),
                        AbilityComponentTypes = (ability.ComponentsArray ?? new BlueprintComponent[0])
                            .Where(c => c != null).Select(c => c.GetType().FullName)
                            .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToArray(),
                        ActionType = ability.ActionType.ToString(),
                        Range = ability.Range.ToString(),
                        EffectOnAlly = ability.EffectOnAlly.ToString(),
                        EffectOnEnemy = ability.EffectOnEnemy.ToString(),
                        CanTargetSelf = ability.CanTargetSelf,
                        CanTargetFriends = ability.CanTargetFriends,
                        CanTargetEnemies = ability.CanTargetEnemies,
                        CanTargetPoint = ability.CanTargetPoint,
                        IsStickyTouch = ability.StickyTouch != null,
                        IsMass = effects.Any(e => e.Target == EffectTarget.Party.ToString() ||
                            e.Target == EffectTarget.AlliedAreaRecipients.ToString()),
                        IsArea = effects.Any(e => e.Kind == EffectKind.AreaBuff.ToString() ||
                            e.Target == EffectTarget.AlliedAreaRecipients.ToString() ||
                            e.Target == EffectTarget.EnemyAreaRecipients.ToString() ||
                            e.Target == EffectTarget.AmbiguousAreaRecipients.ToString()),
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
                        RestorativeActions = scan.Diagnostics
                            .Where(d => d.Code == "restorative-action")
                            .Select(d => new NativeActionRecord
                            {
                                Contract = d.NodeIdentity,
                                Detail = d.Detail,
                                ActionPath = d.ActionPath
                            }).ToArray(),
                        Expression = effectiveExpression,
                        Diagnostics = scan.Diagnostics.ToArray(),
                        ManualOverride = overrideApplication.Entry == null
                            ? string.Empty : overrideApplication.Entry.Disposition + ": " +
                                overrideApplication.Entry.Reason,
                        RuntimeEvidence = new string[0]
                    };
                    NativeCandidateAuditFacts facts = AuditFacts(ability, candidate, effects, scan.Diagnostics);
                    NativeCandidateAuditDecision decision = classifier.Classify(facts);
                    entry.Disposition = decision.Disposition;
                    entry.SupportClass = decision.SupportClass;
                    entry.DispositionReason = decision.Reason;
                    entry.QualificationStatus = decision.QualificationStatus;
                    entry.DispositionBefore040 = before040.Classify(facts).Disposition;
                    // Live discovery classifies every ability a party member
                    // actually has as reachable (KingmakerBuffSourceDiscovery):
                    // the live scope is that classification, so sources the
                    // static index cannot reach (the Heal skill's Treat
                    // Affliction) are audited too.
                    NativeCandidateAuditFacts live = AuditFacts(ability, true, effects, scan.Diagnostics);
                    NativeCandidateAuditDecision liveDecision = classifier.Classify(live);
                    entry.LiveDisposition = liveDecision.Disposition;
                    entry.LiveDispositionReason = liveDecision.Reason;
                    entry.LiveDispositionBefore040 = before040.Classify(live).Disposition;
                    entry.Payloads = liveDecision.Payloads.Select(e => e.EffectId).ToArray();
                    if (overrideApplication.Entry != null)
                    {
                        EffectOverrideEntry applied = overrideApplication.Entry;
                        entry.Disposition = applied.Disposition == "exclude" ? "exclude" :
                            applied.Disposition == "unsupported-with-reason"
                                ? "unsupported-with-reason" : "include";
                        entry.SupportClass = entry.Disposition == "include"
                            ? "override" : entry.Disposition == "exclude"
                                ? "excluded-by-override" : "none";
                        entry.DispositionReason = applied.Reason;
                        entry.QualificationStatus = entry.Disposition == "include"
                            ? "DEFER-runtime-qualification" : entry.Disposition == "exclude"
                                ? "PASS-excluded-by-definition" : "FAIL-unsupported";
                        // An override decides both scopes under both rule sets.
                        entry.DispositionBefore040 = entry.Disposition;
                        entry.LiveDisposition = entry.Disposition;
                        entry.LiveDispositionReason = entry.DispositionReason;
                        entry.LiveDispositionBefore040 = entry.Disposition;
                        entry.Payloads = entry.Disposition == "include"
                            ? effects.Select(e => e.EffectGuid).Distinct(StringComparer.Ordinal).ToArray()
                            : new string[0];
                    }
                    entries.Add(entry);
                }
                catch (Exception exception)
                {
                    entries.Add(new NativeCatalogEntry
                    {
                        AbilityGuid = ability.AssetGuid,
                        InternalName = ability.name ?? string.Empty,
                        DisplayName = ability.Name ?? string.Empty,
                        SourceAssembly = ability.GetType().Assembly.FullName,
                        Ownership = _ownership.GetOwnership(ability.AssetGuid),
                        VariantGuids = new string[0],
                        ResourceIds = new string[0],
                        Expression = new EmptyEffectExpression(),
                        Diagnostics = new[]
                        {
                            new DiscoveryDiagnostic("scanner-exception", ability.AssetGuid,
                                exception.GetType().FullName + ": " + exception.Message)
                        },
                        Disposition = "scanner-exception",
                        DispositionBefore040 = "scanner-exception",
                        LiveDisposition = "scanner-exception",
                        LiveDispositionBefore040 = "scanner-exception",
                        Payloads = new string[0]
                    });
                }
            }

            return new NativeCatalogExport
            {
                SchemaVersion = 5,
                Profile = _profileId,
                GeneratorCommit = BuildInfo.Commit,
                AbilityCount = entries.Count,
                CandidateCount = entries.Count(e => e.IsCandidate),
                DetectedEffectCount = entries.Count(e => e.HasDetectedEffect),
                DiagnosticAbilityCount = entries.Count(e => e.Diagnostics.Length != 0),
                SupportedAutomaticallyCount = entries.Count(e => e.IsCandidate &&
                    e.Disposition == "include" && e.SupportClass == "automatic"),
                SupportedGenericReflectionCount = entries.Count(e => e.IsCandidate &&
                    e.Disposition == "include" && e.SupportClass == "generic-reflection-wrapper"),
                SupportedExplicitAdapterCount = entries.Count(e => e.IsCandidate &&
                    e.Disposition == "include" && e.SupportClass == "explicit-adapter"),
                SupportedOverrideCount = entries.Count(e => e.IsCandidate &&
                    e.Disposition == "include" && e.SupportClass == "override"),
                ExcludedByDefinitionCount = entries.Count(e => e.IsCandidate && e.Disposition == "exclude"),
                UnsupportedCount = entries.Count(e => e.IsCandidate &&
                    e.Disposition == "unsupported-with-reason"),
                RuntimeQualifiedDirectCount = 0,
                RuntimeQualifiedEquivalenceClassCount = 0,
                OptionalAbilityCount = entries.Count(e => e.Ownership != "native"),
                OptionalCandidateCount = entries.Count(e => e.Ownership != "native" && e.IsCandidate),
                OptionalIncludedCount = entries.Count(e => e.Ownership != "native" && e.Disposition == "include"),
                OptionalUnsupportedCount = entries.Count(e => e.Ownership != "native" &&
                    e.Disposition == "unsupported-with-reason"),
                Audit040 = NativeCatalogAudit.Summarize(entries.Select(AuditInput)),
                Abilities = entries.ToArray()
            };
        }

        // One exported entry as the 0.4.0 catalogue audit reads it.
        internal static NativeCatalogAuditInput AuditInput(NativeCatalogEntry entry)
        {
            return new NativeCatalogAuditInput
            {
                AbilityGuid = entry.AbilityGuid,
                InternalName = entry.InternalName,
                DisplayName = entry.DisplayName,
                Ownership = entry.Ownership,
                AbilityType = entry.AbilityType,
                IsSpell = entry.IsSpell,
                IsCandidate = entry.IsCandidate,
                FirstAccessibilitySource = (entry.AccessibilitySources ?? new string[0]).FirstOrDefault(),
                CanTargetSelf = entry.CanTargetSelf,
                CanTargetFriends = entry.CanTargetFriends,
                IsStickyTouch = entry.IsStickyTouch,
                SupportClass = entry.SupportClass,
                QualificationStatus = entry.QualificationStatus,
                ManualOverride = entry.ManualOverride,
                Disposition = entry.Disposition,
                DispositionReason = entry.DispositionReason,
                DispositionBefore040 = entry.DispositionBefore040,
                LiveDisposition = entry.LiveDisposition,
                LiveDispositionReason = entry.LiveDispositionReason,
                LiveDispositionBefore040 = entry.LiveDispositionBefore040,
                Effects = (entry.Effects ?? new NativeEffectRecord[0]).Select(e => new NativeCatalogAuditEffect
                {
                    EffectGuid = e.EffectGuid,
                    EffectName = e.EffectName,
                    Kind = e.Kind,
                    Target = e.Target
                }).ToArray(),
                Payloads = entry.Payloads ?? new string[0]
            };
        }

        // The classifier facts of one ability, shared by the catalogue export
        // and live discovery so both judge exactly the same facts.
        internal static NativeCandidateAuditFacts AuditFacts(BlueprintAbility ability, bool accessible,
            IEnumerable<NativeEffectRecord> effects, IEnumerable<DiscoveryDiagnostic> diagnostics)
        {
            DiscoveryDiagnostic[] scanned = (diagnostics ?? new DiscoveryDiagnostic[0]).ToArray();
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = accessible,
                CanTargetSelf = ability.CanTargetSelf,
                CanTargetFriends = ability.CanTargetFriends,
                CanTargetEnemies = ability.CanTargetEnemies,
                CanTargetPoint = ability.CanTargetPoint,
                HasVariants = (ability.Variants ?? new BlueprintAbility[0]).Any(v => v != null),
                IsStickyTouch = ability.StickyTouch != null,
                EffectOnAlly = ability.EffectOnAlly.ToString(),
                EffectOnEnemy = ability.EffectOnEnemy.ToString(),
                Range = ability.Range.ToString(),
                AbilityComponentTypes = (ability.ComponentsArray ?? new BlueprintComponent[0])
                    .Where(c => c != null).Select(c => c.GetType().FullName)
                    .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToArray(),
                Effects = (effects ?? new NativeEffectRecord[0]).Select(e => new NativeCandidateEffectFacts
                {
                    EffectId = e.EffectGuid,
                    EffectName = e.EffectName,
                    Kind = e.Kind,
                    Target = e.Target,
                    Harmful = e.Harmful,
                    IsHiddenInUi = e.IsHiddenInUi,
                    IsClassFeature = e.IsClassFeature,
                    RemoveOnRest = e.RemoveOnRest,
                    StayOnDeath = e.StayOnDeath,
                    ComponentTypes = e.ComponentTypes,
                    GrantedConditions = e.GrantedConditions,
                    SourceContract = e.SourceContract,
                    ActionPath = e.ActionPath
                }).ToArray(),
                DiagnosticContracts = scanned.Select(d => d.NodeIdentity + "|" + d.Detail).ToArray(),
                Diagnostics = scanned.Select(d => new NativeCandidateDiagnosticFacts
                {
                    Code = d.Code,
                    Contract = d.NodeIdentity,
                    Detail = d.Detail,
                    ActionPath = d.ActionPath
                }).ToArray()
            };
        }

        internal static NativeEffectRecord[] GetEffects(EffectExpression expression)
        {
            var leaves = new List<EffectLeafExpression>();
            CollectLeaves(expression, leaves);
            return leaves.Select(leaf =>
            {
                bool? harmful = null;
                string name = string.Empty;
                string[] componentTypes = new string[0];
                string[] grantedConditions = new string[0];
                bool hidden = false;
                bool classFeature = false;
                bool removeOnRest = false;
                bool stayOnDeath = false;
                if (leaf.Kind == EffectKind.Buff || leaf.Kind == EffectKind.AreaBuff)
                {
                    var buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(leaf.EffectId);
                    if (buff != null)
                    {
                        harmful = buff.Harmful;
                        name = buff.name ?? string.Empty;
                        componentTypes = (buff.ComponentsArray ?? new BlueprintComponent[0])
                            .Where(c => c != null).Select(c => c.GetType().FullName)
                            .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToArray();
                        grantedConditions = (buff.ComponentsArray ?? new BlueprintComponent[0])
                            .Select(c => c is AddCondition ? ((AddCondition)c).Condition.ToString()
                                : c is BuffStatusCondition ? ((BuffStatusCondition)c).Condition.ToString() : null)
                            .Where(v => v != null).Distinct(StringComparer.Ordinal)
                            .OrderBy(v => v, StringComparer.Ordinal).ToArray();
                        hidden = buff.IsHiddenInUI;
                        classFeature = buff.IsClassFeature;
                        removeOnRest = buff.RemoveOnRest;
                        stayOnDeath = buff.StayOnDeath;
                    }
                }
                else if (leaf.Kind == EffectKind.WornItemEnchantment)
                {
                    var enchantment = ResourcesLibrary.TryGetBlueprint<BlueprintItemEnchantment>(leaf.EffectId);
                    if (enchantment != null)
                    {
                        name = enchantment.name ?? string.Empty;
                        componentTypes = (enchantment.ComponentsArray ?? new BlueprintComponent[0])
                            .Where(c => c != null).Select(c => c.GetType().FullName)
                            .Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToArray();
                    }
                }
                return new NativeEffectRecord
                {
                    Kind = leaf.Kind.ToString(),
                    EffectGuid = leaf.EffectId,
                    EffectName = name,
                    Target = leaf.Target.ToString(),
                    Harmful = harmful,
                    IsHiddenInUi = hidden,
                    IsClassFeature = classFeature,
                    RemoveOnRest = removeOnRest,
                    StayOnDeath = stayOnDeath,
                    ComponentTypes = componentTypes,
                    GrantedConditions = grantedConditions,
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
        [JsonProperty("supportedAutomaticallyCount", Order = 8)] public int SupportedAutomaticallyCount { get; set; }
        [JsonProperty("supportedGenericReflectionCount", Order = 9)] public int SupportedGenericReflectionCount { get; set; }
        [JsonProperty("supportedExplicitAdapterCount", Order = 10)] public int SupportedExplicitAdapterCount { get; set; }
        [JsonProperty("supportedOverrideCount", Order = 11)] public int SupportedOverrideCount { get; set; }
        [JsonProperty("excludedByDefinitionCount", Order = 12)] public int ExcludedByDefinitionCount { get; set; }
        [JsonProperty("unsupportedCount", Order = 13)] public int UnsupportedCount { get; set; }
        [JsonProperty("runtimeQualifiedDirectCount", Order = 14)] public int RuntimeQualifiedDirectCount { get; set; }
        [JsonProperty("runtimeQualifiedEquivalenceClassCount", Order = 15)] public int RuntimeQualifiedEquivalenceClassCount { get; set; }
        [JsonProperty("optionalAbilityCount", Order = 16)] public int OptionalAbilityCount { get; set; }
        [JsonProperty("optionalCandidateCount", Order = 17)] public int OptionalCandidateCount { get; set; }
        [JsonProperty("optionalIncludedCount", Order = 18)] public int OptionalIncludedCount { get; set; }
        [JsonProperty("optionalUnsupportedCount", Order = 19)] public int OptionalUnsupportedCount { get; set; }
        [JsonProperty("audit040", Order = 20)] public NativeCatalogAuditSummary Audit040 { get; set; }
        [JsonProperty("abilities", Order = 21)]
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
        [JsonProperty("abilityComponentTypes", Order = 12)] public string[] AbilityComponentTypes { get; set; }
        [JsonProperty("actionType", Order = 13)] public string ActionType { get; set; }
        [JsonProperty("range", Order = 14)] public string Range { get; set; }
        [JsonProperty("effectOnAlly", Order = 15)] public string EffectOnAlly { get; set; }
        [JsonProperty("effectOnEnemy", Order = 16)] public string EffectOnEnemy { get; set; }
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
        [JsonProperty("restorativeActions", Order = 30)] public NativeActionRecord[] RestorativeActions { get; set; }
        [JsonProperty("expression", Order = 31)]
        public EffectExpression Expression { get; set; }
        [JsonProperty("diagnostics", Order = 32)]
        public DiscoveryDiagnostic[] Diagnostics { get; set; }
        [JsonProperty("disposition", Order = 33)]
        public string Disposition { get; set; }
        [JsonProperty("supportClass", Order = 34)] public string SupportClass { get; set; }
        [JsonProperty("dispositionReason", Order = 35)] public string DispositionReason { get; set; }
        [JsonProperty("manualOverride", Order = 36)] public string ManualOverride { get; set; }
        [JsonProperty("runtimeEvidence", Order = 37)] public string[] RuntimeEvidence { get; set; }
        [JsonProperty("qualificationStatus", Order = 38)] public string QualificationStatus { get; set; }
        // The 0.4.0 catalogue audit (WP5): the disposition under the rules
        // released through 0.3.0, and the live-discovery scope (the ability
        // classified as reachable) under both rule sets.
        [JsonProperty("dispositionBefore040", Order = 39)] public string DispositionBefore040 { get; set; }
        [JsonProperty("liveDisposition", Order = 40)] public string LiveDisposition { get; set; }
        [JsonProperty("liveDispositionReason", Order = 41)] public string LiveDispositionReason { get; set; }
        [JsonProperty("liveDispositionBefore040", Order = 42)] public string LiveDispositionBefore040 { get; set; }
        [JsonProperty("payloads", Order = 43)] public string[] Payloads { get; set; }
    }

    internal sealed class NativeEffectRecord
    {
        [JsonProperty("kind", Order = 1)] public string Kind { get; set; }
        [JsonProperty("effectGuid", Order = 2)] public string EffectGuid { get; set; }
        [JsonProperty("effectName", Order = 3)] public string EffectName { get; set; }
        [JsonProperty("target", Order = 4)] public string Target { get; set; }
        [JsonProperty("harmful", Order = 5)] public bool? Harmful { get; set; }
        [JsonProperty("isHiddenInUi", Order = 6)] public bool IsHiddenInUi { get; set; }
        [JsonProperty("isClassFeature", Order = 7)] public bool IsClassFeature { get; set; }
        [JsonProperty("removeOnRest", Order = 8)] public bool RemoveOnRest { get; set; }
        [JsonProperty("stayOnDeath", Order = 9)] public bool StayOnDeath { get; set; }
        [JsonProperty("componentTypes", Order = 10)] public string[] ComponentTypes { get; set; }
        [JsonProperty("sourceContract", Order = 11)] public string SourceContract { get; set; }
        [JsonProperty("actionPath", Order = 12)] public string ActionPath { get; set; }
        [JsonProperty("grantedConditions", Order = 13)] public string[] GrantedConditions { get; set; }
    }

    internal sealed class NativeActionRecord
    {
        [JsonProperty("contract", Order = 1)] public string Contract { get; set; }
        [JsonProperty("detail", Order = 2)] public string Detail { get; set; }
        [JsonProperty("actionPath", Order = 3)] public string ActionPath { get; set; }
    }
}
