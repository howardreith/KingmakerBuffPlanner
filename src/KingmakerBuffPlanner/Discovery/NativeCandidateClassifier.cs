using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.Discovery
{
    public sealed class NativeCandidateAuditFacts
    {
        public bool IsPlayerAccessible { get; set; }
        public bool CanTargetSelf { get; set; }
        public bool CanTargetFriends { get; set; }
        public bool CanTargetEnemies { get; set; }
        public bool CanTargetPoint { get; set; }
        public bool HasVariants { get; set; }
        public bool IsStickyTouch { get; set; }
        public string EffectOnAlly { get; set; }
        public string EffectOnEnemy { get; set; }
        public string Range { get; set; }
        public IReadOnlyList<string> AbilityComponentTypes { get; set; }
        public IReadOnlyList<NativeCandidateEffectFacts> Effects { get; set; }
        public IReadOnlyList<string> DiagnosticContracts { get; set; }
        public IReadOnlyList<NativeCandidateDiagnosticFacts> Diagnostics { get; set; }
    }

    public sealed class NativeCandidateEffectFacts
    {
        public string Kind { get; set; }
        public string Target { get; set; }
        public bool? Harmful { get; set; }
        public bool IsHiddenInUi { get; set; }
        public bool IsClassFeature { get; set; }
        public bool RemoveOnRest { get; set; }
        public bool StayOnDeath { get; set; }
        public IReadOnlyList<string> ComponentTypes { get; set; }
        public string SourceContract { get; set; }
        public string ActionPath { get; set; }
    }

    public sealed class NativeCandidateDiagnosticFacts
    {
        public string Code { get; set; }
        public string Contract { get; set; }
        public string Detail { get; set; }
        public string ActionPath { get; set; }
    }

    public sealed class NativeCandidateAuditDecision
    {
        internal NativeCandidateAuditDecision(
            string disposition, string supportClass, string reason, string qualificationStatus)
        {
            Disposition = disposition;
            SupportClass = supportClass;
            Reason = reason;
            QualificationStatus = qualificationStatus;
        }

        public string Disposition { get; private set; }
        public string SupportClass { get; private set; }
        public string Reason { get; private set; }
        public string QualificationStatus { get; private set; }
    }

    public sealed class NativeCandidateClassifier
    {
        public NativeCandidateAuditDecision Classify(NativeCandidateAuditFacts facts)
        {
            if (facts == null) throw new ArgumentNullException("facts");
            IReadOnlyList<NativeCandidateEffectFacts> effects =
                facts.Effects ?? new NativeCandidateEffectFacts[0];
            IReadOnlyList<string> diagnostics = facts.DiagnosticContracts ?? new string[0];
            IReadOnlyList<NativeCandidateDiagnosticFacts> diagnosticFacts =
                facts.Diagnostics ?? new NativeCandidateDiagnosticFacts[0];
            IReadOnlyList<NativeCandidateDiagnosticFacts> restorative = diagnosticFacts
                .Where(IsRestorative).ToArray();
            if (restorative.Count == 0)
            {
                restorative = diagnostics.Where(IsRestorativeContract)
                    .Select(value => new NativeCandidateDiagnosticFacts
                    {
                        Code = "restorative-action",
                        Contract = value,
                        Detail = value,
                        ActionPath = string.Empty
                    }).ToArray();
            }
            IReadOnlyList<string> abilityComponents =
                facts.AbilityComponentTypes ?? new string[0];
            if (!facts.IsPlayerAccessible)
                return Exclude("not-player-accessible",
                    "The ability is not reachable from the exact native player class/race/feat source graph.");
            if (effects.Any(e => Contains(e.ActionPath, "ContextActionSpawnMonster")) ||
                diagnostics.Any(d => Contains(d, "excluded-summoning-action")))
                return Exclude("summoning",
                    "Summoning is outside the product definition; after-spawn buffs are not planner effects.");
            if (facts.HasVariants)
                return Exclude("non-castable-variant-container",
                    "The parent is an unresolved choice container; independently eligible concrete children are cataloged instead.");
            if (facts.CanTargetPoint)
                return Exclude("point-target-without-placement",
                    "Point-target abilities are excluded until a deterministic safe placement rule exists.");
            if (effects.Count == 0)
            {
                if (restorative.Count != 0)
                    return Exclude("instantaneous-restoration-without-substantive-buff",
                        "Only exact healing, recovery, removal, resurrection, or dispel actions were reachable; no persistent beneficial payload was detected.");
                return Exclude("no-persistent-beneficial-party-effect",
                    "No persistent unit buff, area buff, or safely resolvable worn-item enchantment was detected.");
            }

            if (facts.IsStickyTouch && effects.All(e => e.Target == "Caster"))
                return Exclude("sticky-touch-carrier-only",
                    "Only the transient caster-side delivery carrier was detected; no persistent target effect remains.");
            if (facts.Range == "Weapon" && facts.CanTargetEnemies && !facts.CanTargetFriends)
                return Exclude("hostile-weapon-carrier",
                    "The caster-side marker belongs to a hostile weapon action, not a standalone beneficial cast.");

            bool hostileCurrentTarget = effects.Any(e =>
                    e.Target == "CurrentTarget") &&
                facts.CanTargetEnemies &&
                string.Equals(facts.EffectOnEnemy, "Harmful",
                    StringComparison.Ordinal) &&
                !string.Equals(facts.EffectOnAlly, "Helpful",
                    StringComparison.Ordinal) &&
                !effects.Any(e => e.Target == "Caster" ||
                    e.Target == "Pet" || e.Target == "Party" ||
                    e.Target == "AlliedAreaRecipients");
            if (hostileCurrentTarget)
                return Exclude("no-persistent-beneficial-party-effect",
                    "The current-target payload has harmful enemy disposition and no exact beneficial-party branch.");

            if (effects.All(e => e.Target == "EnemyAreaRecipients"))
                return Exclude("enemy-only-area",
                    "The persistent area payload is expressly restricted to enemy recipients.");
            if (effects.All(e => e.Target == "AmbiguousAreaRecipients"))
                return Exclude("ambiguous-area-recipient",
                    "The persistent area payload does not prove allied recipient disposition.");
            if (effects.All(e => e.Harmful == true))
                return Exclude("harmful-only",
                    "Every resolved persistent BlueprintBuff effect is explicitly marked harmful.");

            List<NativeCandidateEffectFacts> safe = effects
                .Where(e => e.Harmful != true && IsSafeRecipient(e, facts)).ToList();
            if (safe.Count == 0)
                return Exclude(
                    effects.Any(e => e.Target == "EnemyAreaRecipients")
                        ? "enemy-only-area" :
                    effects.Any(e => e.Target == "AmbiguousAreaRecipients")
                        ? "ambiguous-area-recipient" :
                    effects.Any(e => e.Harmful == true)
                        ? "harmful-only" : "no-persistent-beneficial-party-effect",
                    "No persistent beneficial payload has deterministic controllable-party targeting.");

            List<NativeCandidateEffectFacts> payloads = safe.Where(e => !IsMarker(e)).ToList();
            bool hasOffensiveCarrier = IsHostileCarrier(facts, abilityComponents);
            IReadOnlyList<NativeCandidateDiagnosticFacts> offensive = diagnosticFacts
                .Where(IsOffensive).ToArray();
            if (offensive.Count == 0)
            {
                offensive = diagnostics.Where(IsOffensiveContract)
                    .Select(value => new NativeCandidateDiagnosticFacts
                    {
                        Code = "offensive-action",
                        Contract = value,
                        Detail = value,
                        ActionPath = string.Empty
                    }).ToArray();
            }
            if (hasOffensiveCarrier)
                payloads.Clear();
            else if (offensive.Count != 0)
                payloads = payloads.Where(effect => !offensive.Any(action =>
                    SameConditionalBranch(effect.ActionPath, action.ActionPath))).ToList();
            if (payloads.Count == 0)
            {
                if (hasOffensiveCarrier || offensive.Count != 0)
                    return Exclude("offensive-carrier-only",
                        "Offensive delivery or damage semantics leave only hidden carrier, save, activation, or cleanup markers.");
                if (restorative.Count != 0 && safe.All(IsMarker))
                    return Exclude("reactive-restoration-marker-only",
                        "Exact restorative actions leave only hidden carrier, activation, or cleanup marker buffs on every safe branch.");
                if (restorative.Count != 0)
                    return Exclude("restorative-action-without-substantive-buff",
                        "Exact restorative actions do not establish a substantive persistent beneficial state on a safe branch.");
                if (safe.All(IsMarker))
                    return Exclude("hidden-marker-only",
                        "Only hidden class-feature, activation, or cleanup marker effects were detected.");
                return Exclude("no-persistent-beneficial-party-effect",
                    "No persistent beneficial payload remains on a safe controllable-party branch.");
            }

            bool dynamicEnchantPool = diagnostics.Any(d =>
                Contains(d, "ContextActionWeaponEnchantPool"));
            bool explicitAdapter = dynamicEnchantPool || effects.Any(e =>
                e.SourceContract == "MagicFang" ||
                e.SourceContract == "ContextActionEnchantWornItem" ||
                e.SourceContract == "ContextActionSpawnAreaEffect+AbilityAreaEffectBuff" ||
                e.SourceContract == "ContextActionsOnPet" ||
                e.SourceContract == "ContextActionPartyMembers");
            bool reflectionWrapper = effects.Any(e => Contains(e.ActionPath, "reflected:"));
            string supportClass = explicitAdapter ? "explicit-adapter" :
                reflectionWrapper ? "generic-reflection-wrapper" : "automatic";
            return new NativeCandidateAuditDecision(
                "include", supportClass,
                (payloads.Any(e => e.Target == "Party" ||
                    e.Target == "AlliedAreaRecipients" ||
                    e.Target == "Pet" ||
                    (e.Target == "CurrentTarget" && facts.CanTargetFriends))
                    ? "valid-beneficial-party-effect: "
                    : "valid-beneficial-self-effect: ") +
                (dynamicEnchantPool
                    ? "The exact native signal buff supplies duration/presence semantics; native RuleCastSpell execution applies the currently selected enchant pool."
                    : diagnostics.Count == 0
                    ? "Player-accessible graph has a persistent beneficial effect and deterministic target semantics."
                    : "Persistent beneficial semantics are recognized; remaining diagnostics are non-persistent native adjunct actions."),
                "DEFER-runtime-qualification");
        }

        private static bool IsSafeRecipient(
            NativeCandidateEffectFacts effect, NativeCandidateAuditFacts facts)
        {
            if (effect == null) return false;
            return effect.Target == "Caster" || effect.Target == "Pet" ||
                effect.Target == "Party" || effect.Target == "AlliedAreaRecipients" ||
                (effect.Target == "CurrentTarget" &&
                    (facts.CanTargetSelf || facts.CanTargetFriends));
        }

        private static bool IsMarker(NativeCandidateEffectFacts effect)
        {
            if (effect == null || !effect.IsHiddenInUi) return false;
            if (effect.IsClassFeature) return true;
            IReadOnlyList<string> components = effect.ComponentTypes ?? new string[0];
            if (components.Count == 0) return true;
            return components.All(value =>
                Contains(value, "AddFactContextActions") ||
                Contains(value, "RemoveOnSave") ||
                Contains(value, "RemoveBuff") ||
                Contains(value, "ContextRankConfig"));
        }

        private static bool IsHostileCarrier(
            NativeCandidateAuditFacts facts, IEnumerable<string> components)
        {
            bool hostileDisposition = facts.CanTargetEnemies &&
                (!facts.CanTargetFriends ||
                 string.Equals(facts.EffectOnEnemy, "Harmful", StringComparison.Ordinal) ||
                 string.Equals(facts.EffectOnAlly, "Harmful", StringComparison.Ordinal));
            if (!hostileDisposition) return false;
            if (string.Equals(facts.Range, "Weapon", StringComparison.Ordinal)) return true;
            return (components ?? new string[0]).Any(value =>
                Contains(value, "AbilityDeliverProjectile") ||
                Contains(value, "AbilityDeliverAttackWithWeapon") ||
                Contains(value, "AbilityDeliverChain") ||
                Contains(value, "AbilityDeliverTouch") ||
                Contains(value, "AbilityDeliverBomb"));
        }

        private static bool IsOffensive(NativeCandidateDiagnosticFacts diagnostic)
        {
            return diagnostic != null &&
                (string.Equals(diagnostic.Code, "offensive-action", StringComparison.Ordinal) ||
                 IsOffensiveContract(diagnostic.Contract) ||
                 IsOffensiveContract(diagnostic.Detail));
        }

        private static bool IsRestorative(NativeCandidateDiagnosticFacts diagnostic)
        {
            return diagnostic != null &&
                (string.Equals(diagnostic.Code, "restorative-action", StringComparison.Ordinal) ||
                 IsRestorativeContract(diagnostic.Contract) ||
                 IsRestorativeContract(diagnostic.Detail));
        }

        private static bool IsOffensiveContract(string value)
        {
            return Contains(value, "ContextActionDealDamage") ||
                Contains(value, "ContextActionDealDirectDamage") ||
                Contains(value, "ContextActionAttack") ||
                Contains(value, "ContextActionRangedAttack");
        }

        private static bool IsRestorativeContract(string value)
        {
            return Contains(value, "ContextActionHealTarget") ||
                Contains(value, "ContextActionHealEnergyDrain") ||
                Contains(value, "ContextActionHealStatDamage") ||
                Contains(value, "ContextActionResurrect") ||
                Contains(value, "ContextActionRemoveBuff") ||
                Contains(value, "ContextActionRemoveDeathDoor") ||
                Contains(value, "ContextActionDispelMagic");
        }

        private static bool SameConditionalBranch(string firstPath, string secondPath)
        {
            string[] first = Branches(firstPath);
            string[] second = Branches(secondPath);
            if (first.Length == 0 || second.Length == 0) return true;
            int common = Math.Min(first.Length, second.Length);
            for (int index = 0; index < common; index++)
                if (!string.Equals(first[index], second[index], StringComparison.Ordinal))
                    return false;
            return true;
        }

        private static string[] Branches(string path)
        {
            return (path ?? string.Empty).Split('/')
                .Where(value => value == "true" || value == "false").ToArray();
        }

        private static NativeCandidateAuditDecision Exclude(string code, string reason)
        {
            return new NativeCandidateAuditDecision(
                "exclude", "excluded-by-definition", code + ": " + reason,
                "PASS-excluded-by-definition");
        }

        private static bool Contains(string value, string fragment)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(fragment, StringComparison.Ordinal) >= 0;
        }
    }
}
