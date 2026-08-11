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
        public string EffectOnAlly { get; set; }
        public string EffectOnEnemy { get; set; }
        public IReadOnlyList<NativeCandidateEffectFacts> Effects { get; set; }
        public IReadOnlyList<string> DiagnosticContracts { get; set; }
    }

    public sealed class NativeCandidateEffectFacts
    {
        public string Kind { get; set; }
        public string Target { get; set; }
        public bool? Harmful { get; set; }
        public string SourceContract { get; set; }
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
            if (!facts.IsPlayerAccessible)
                return Exclude("not-player-accessible",
                    "The ability is not reachable from the exact native player class/race/feat source graph.");
            if (effects.Any(e => Contains(e.ActionPath, "ContextActionSpawnMonster")) ||
                diagnostics.Any(d => Contains(d, "excluded-summoning-action")))
                return Exclude("summoning",
                    "Summoning is outside the product definition; after-spawn buffs are not planner effects.");
            if (facts.CanTargetPoint)
                return Exclude("point-target-without-placement",
                    "Point-target abilities are excluded until a deterministic safe placement rule exists.");
            if (effects.Count == 0)
                return Exclude("no-persistent-effect",
                    "No persistent unit buff, area buff, or safely resolvable worn-item enchantment was detected.");

            if (facts.HasVariants && !facts.CanTargetSelf && !facts.CanTargetFriends &&
                !facts.CanTargetEnemies && !facts.CanTargetPoint)
                return Exclude("non-castable-variant-container",
                    "The parent groups castable variants but is not itself a targetable source.");

            bool controlledTransform = effects.Any(e =>
                e.Target == "Caster" || e.Target == "Pet" || e.Target == "Party");
            bool hostileOnly = facts.CanTargetEnemies &&
                !string.Equals(facts.EffectOnAlly, "Helpful", StringComparison.Ordinal) &&
                !controlledTransform &&
                (!facts.CanTargetFriends ||
                    string.Equals(facts.EffectOnEnemy, "Harmful", StringComparison.Ordinal));
            if (hostileOnly)
                return Exclude("hostile-only",
                    "The ability targets enemies and has no structurally controlled caster, pet, or party effect.");

            if (effects.All(e => e.Harmful == true))
                return Exclude("harmful-only",
                    "Every resolved persistent BlueprintBuff effect is explicitly marked harmful.");

            bool hasSafeTarget = effects.Any(e =>
                e.Target == "Caster" || e.Target == "Pet" || e.Target == "Party" ||
                e.Target == "AreaRecipients" ||
                (e.Target == "CurrentTarget" && (facts.CanTargetSelf || facts.CanTargetFriends)));
            if (!hasSafeTarget)
                return new NativeCandidateAuditDecision(
                    "unsupported-with-reason", "none",
                    "No deterministic controllable-unit target can be selected for the persistent effect.",
                    "FAIL-unsupported");

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
                dynamicEnchantPool
                    ? "The exact native signal buff supplies duration/presence semantics; native RuleCastSpell execution applies the currently selected enchant pool."
                    : diagnostics.Count == 0
                    ? "Player-accessible graph has a persistent beneficial effect and deterministic target semantics."
                    : "Persistent beneficial semantics are recognized; remaining diagnostics are non-persistent native adjunct actions.",
                "DEFER-runtime-qualification");
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
