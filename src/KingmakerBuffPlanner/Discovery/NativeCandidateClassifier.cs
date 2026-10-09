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
        // Identity for audit reports only; the rules never read it.
        public string EffectId { get; set; }
        public string EffectName { get; set; }
        public string Kind { get; set; }
        public string Target { get; set; }
        public bool? Harmful { get; set; }
        public bool IsHiddenInUi { get; set; }
        public bool IsClassFeature { get; set; }
        public bool RemoveOnRest { get; set; }
        public bool StayOnDeath { get; set; }
        public IReadOnlyList<string> ComponentTypes { get; set; }
        // The UnitCondition names the buff grants its bearer (AddCondition,
        // BuffStatusCondition), read from the blueprint (0.4.0).
        public IReadOnlyList<string> GrantedConditions { get; set; }
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
            string disposition, string supportClass, string reason, string qualificationStatus,
            IReadOnlyList<NativeCandidateEffectFacts> payloads = null)
        {
            Disposition = disposition;
            SupportClass = supportClass;
            Reason = reason;
            QualificationStatus = qualificationStatus;
            Payloads = payloads ?? new NativeCandidateEffectFacts[0];
        }

        public string Disposition { get; private set; }
        public string SupportClass { get; private set; }
        public string Reason { get; private set; }
        public string QualificationStatus { get; private set; }
        // The persistent beneficial effects an included source is planned
        // and confirmed by (empty when excluded).
        public IReadOnlyList<NativeCandidateEffectFacts> Payloads { get; private set; }

        // The leading reason code ("valid-beneficial-party-effect",
        // "hostile-ability-rider", ...).
        public string ReasonCode
        {
            get
            {
                int colon = (Reason ?? string.Empty).IndexOf(':');
                return colon < 0 ? Reason ?? string.Empty : Reason.Substring(0, colon);
            }
        }
    }

    // The rules a classifier applies. Pre040 is the catalogue as released
    // through 0.3.0; Current adds the 0.4.0 audit rules (WP5). Discovery
    // always uses Current; the catalogue export also classifies with Pre040
    // so every export states what the audit rules changed.
    public enum NativeCandidateRuleSet
    {
        Pre040,
        Current
    }

    public sealed class NativeCandidateClassifier
    {
        private readonly NativeCandidateRuleSet _rules;

        public NativeCandidateClassifier()
            : this(NativeCandidateRuleSet.Current)
        {
        }

        public NativeCandidateClassifier(NativeCandidateRuleSet rules)
        {
            _rules = rules;
        }

        private bool AuditRules { get { return _rules == NativeCandidateRuleSet.Current; } }

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
            if (AuditRules && effects.All(e => e.Harmful == true || IsHarmfulCondition(e)))
                return Exclude("harmful-only",
                    "Every resolved persistent effect is marked harmful or only imposes a harmful condition (or changes the bearer's faction).");

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
            List<NativeCandidateEffectFacts> legacySafe = effects
                .Where(e => e.Harmful != true && IsSafeRecipient(e, facts)).ToList();
            List<NativeCandidateEffectFacts> safe = legacySafe
                .Where(e => AuditRefusal(e, facts) == null).ToList();
            if (safe.Count == 0 && legacySafe.Count != 0)
            {
                // Only a 0.4.0 audit rule removed the last safe payload:
                // name that rule (an offensive carrier keeps its own code).
                if (hasOffensiveCarrier || offensive.Count != 0)
                    return Exclude("offensive-carrier-only",
                        "Offensive delivery or damage semantics leave only hidden carrier, save, activation, or cleanup markers.");
                List<string> refusals = legacySafe.Select(e => AuditRefusal(e, facts)).ToList();
                if (refusals.Contains("hostile-ability-rider"))
                    return Exclude("hostile-ability-rider",
                        "The ability is aimed at enemies (harmful to enemies and not helpful to allies, or unable to target allies); a buff it leaves on its caster or target is a rider of that attack, not a party buff.");
                if (refusals.Contains("harmful-condition"))
                    return Exclude("harmful-only",
                        "The remaining persistent effect only imposes a harmful condition (or changes the bearer's faction).");
                return Exclude("save-gated-effect",
                    "The persistent effect lands only when its recipient fails a saving throw, so it is aimed at an unwilling target, not a party buff.");
            }
            if (safe.Count == 0)
                return Exclude(
                    effects.Any(e => e.Target == "EnemyAreaRecipients")
                        ? "enemy-only-area" :
                    effects.Any(e => e.Target == "AmbiguousAreaRecipients")
                        ? "ambiguous-area-recipient" :
                    effects.Any(e => e.Harmful == true)
                        ? "harmful-only" :
                    string.Equals(facts.EffectOnAlly, "Harmful", StringComparison.Ordinal)
                        ? "harmful-ally-disposition" : "no-persistent-beneficial-party-effect",
                    "No persistent beneficial payload has deterministic controllable-party targeting.");

            bool dynamicEnchantPool = diagnostics.Any(d =>
                Contains(d, "ContextActionWeaponEnchantPool"));
            bool instantRestoration = AuditRules && (
                diagnosticFacts.Any(d => IsInstantRestoration(d.Contract) || IsInstantRestoration(d.Detail)) ||
                diagnostics.Any(IsInstantRestoration));
            Func<NativeCandidateEffectFacts, bool> isMarker = effect =>
                IsMarker(effect) || IsAuditMarker(effect, dynamicEnchantPool, instantRestoration);
            List<NativeCandidateEffectFacts> payloads = safe.Where(e => !isMarker(e)).ToList();
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
                if ((restorative.Count != 0 || instantRestoration) && safe.All(isMarker))
                    return Exclude("reactive-restoration-marker-only",
                        "Exact restorative actions leave only hidden carrier, activation, cooldown, or cleanup marker buffs on every safe branch.");
                if (restorative.Count != 0)
                    return Exclude("restorative-action-without-substantive-buff",
                        "Exact restorative actions do not establish a substantive persistent beneficial state on a safe branch.");
                if (safe.All(isMarker) && safe.Any(e => !e.IsHiddenInUi))
                    return Exclude("mechanics-free-marker-only",
                        "The only persistent effects are cooldown, activation, or bookkeeping buffs with no mechanics of their own.");
                if (safe.All(isMarker))
                    return Exclude("hidden-marker-only",
                        "Only hidden class-feature, activation, or cleanup marker effects were detected.");
                return Exclude("no-persistent-beneficial-party-effect",
                    "No persistent beneficial payload remains on a safe controllable-party branch.");
            }

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
                "DEFER-runtime-qualification", payloads.ToArray());
        }

        private static bool IsSafeRecipient(
            NativeCandidateEffectFacts effect, NativeCandidateAuditFacts facts)
        {
            if (effect == null) return false;
            // The game's own ally-disposition data is authoritative: an
            // ability marked harmful to allies is never a beneficial party
            // buff even when the buff blueprint itself forgets m_Harmful
            // (everyday-use v1.2: the owner's Irresistible Dance report).
            if (string.Equals(facts.EffectOnAlly, "Harmful", StringComparison.Ordinal))
                return false;
            return effect.Target == "Caster" || effect.Target == "Pet" ||
                effect.Target == "Party" || effect.Target == "AlliedAreaRecipients" ||
                (effect.Target == "CurrentTarget" &&
                    (facts.CanTargetSelf || facts.CanTargetFriends));
        }

        // The 0.4.0 audit refusal of an otherwise safe effect (WP5), or null.
        // hostile-ability-rider: the ability is aimed at enemies, so what it
        // leaves on its caster or current target rides on an attack (Call of
        // the Wild's Infectious Charms rider on Hideous Laughter, a delivery
        // or maneuver marker). save-gated-effect: the effect lands only on a
        // failed save. harmful-condition: the buff only imposes a harmful
        // condition (Daze) or changes the bearer's faction.
        private string AuditRefusal(NativeCandidateEffectFacts effect, NativeCandidateAuditFacts facts)
        {
            if (!AuditRules || effect == null) return null;
            if (IsHarmfulCondition(effect)) return "harmful-condition";
            if ((effect.Target == "Caster" || effect.Target == "CurrentTarget") && IsAimedAtEnemies(facts))
                return "hostile-ability-rider";
            if (Contains(effect.ActionPath, "ContextActionSavingThrow") ||
                Contains(effect.ActionPath, "ContextActionConditionalSaved"))
                return "save-gated-effect";
            return null;
        }

        private static bool IsAimedAtEnemies(NativeCandidateAuditFacts facts)
        {
            if (!facts.CanTargetEnemies) return false;
            if (!facts.CanTargetFriends) return true;
            return string.Equals(facts.EffectOnEnemy, "Harmful", StringComparison.Ordinal) &&
                !string.Equals(facts.EffectOnAlly, "Helpful", StringComparison.Ordinal);
        }

        // Kingmaker 2.1.7b UnitCondition values that only hinder their bearer.
        private static readonly HashSet<string> HarmfulConditions = new HashSet<string>(new[]
        {
            "Blindness", "Nauseated", "Fatigued", "Paralyzed", "DeathDoor", "Staggered", "Petrified",
            "Dazed", "Slowed", "Entangled", "DifficultTerrain", "Frightened", "Prone", "Sickened",
            "Sleeping", "CantMove", "Shaken", "LoseDexterityToAC", "Dazzled", "Stunned", "Helpless",
            "Confusion", "SpellCastingIsDifficult", "ForceMove", "CantAct", "DisableAttacksOfOpportunity",
            "AttackNearest", "Unconscious", "CanNotAttack", "MovementBan", "StealthForbidden", "Cowering",
            "Exhausted"
        }, StringComparer.Ordinal);

        // Components that only keep a buff's books (duration, rank, stacking,
        // descriptors, stored context) and give its bearer no mechanics.
        private static readonly HashSet<string> BookkeepingComponents = new HashSet<string>(new[]
        {
            "ContextRankConfig", "SpellDescriptorComponent", "UniqueBuff", "StoreBuff",
            "ReplaceAbilityParamsWithContext", "ContextCalculateSharedValue",
            "ContextCalculateAbilityParamsBasedOnClasses", "ContextCalculateAbilityParamsBasedOnClass",
            "ContextCalculateAbilityParams", "RemoveBuffIfCasterIsMissing", "AddStoredSpellToCaption",
            "RecalculateOnStatChange", "BuffRemoveOnSave", "RemoveOnSave"
        }, StringComparer.Ordinal);

        private static readonly HashSet<string> ConditionComponents = new HashSet<string>(new[]
        {
            "AddCondition", "BuffStatusCondition", "DoNotBenefitFromConcealment", "AddFactContextActions"
        }, StringComparer.Ordinal);

        private static string ShortName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return string.Empty;
            string name = typeName.Split(',')[0].Trim();
            int dot = name.LastIndexOf('.');
            return dot < 0 ? name : name.Substring(dot + 1);
        }

        // A buff that changes its bearer's faction (charm, domination), or
        // whose only mechanics impose harmful conditions, harms its bearer
        // whatever its m_Harmful flag says (Daze, Hideous Laughter).
        private static bool IsHarmfulCondition(NativeCandidateEffectFacts effect)
        {
            IReadOnlyList<string> components = effect.ComponentTypes ?? new string[0];
            if (components.Any(value => ShortName(value) == "ChangeFaction")) return true;
            bool imposes = (effect.GrantedConditions ?? new string[0]).Any(HarmfulConditions.Contains) ||
                components.Any(value => ShortName(value) == "DoNotBenefitFromConcealment");
            return imposes && components.All(value =>
                BookkeepingComponents.Contains(ShortName(value)) ||
                ConditionComponents.Contains(ShortName(value)));
        }

        // The 0.4.0 marker rules (WP5). A buff blueprint whose components only
        // keep its books has no effect of its own: a visible cooldown (the
        // Heal skill's Treat Affliction / Treat Deadly Wounds cooldowns), an
        // activation or selection marker, or a cosmetic buff - except the
        // proven enchant-pool signal buff. A hidden buff whose components are
        // bookkeeping plus on-apply or cleanup actions is a marker too, and so
        // is any hidden buff beside an instantaneous heal, restoration,
        // removal or dispel: the restoration is the ability's point, the
        // hidden buff only tracks it.
        private bool IsAuditMarker(NativeCandidateEffectFacts effect, bool dynamicEnchantPool,
            bool instantRestoration)
        {
            if (!AuditRules || effect == null) return false;
            IReadOnlyList<string> components = effect.ComponentTypes ?? new string[0];
            if (effect.Kind == "Buff" && !dynamicEnchantPool &&
                components.All(value => BookkeepingComponents.Contains(ShortName(value))))
                return true;
            if (!effect.IsHiddenInUi) return false;
            if (components.All(value => BookkeepingComponents.Contains(ShortName(value)) ||
                    ShortName(value) == "AddFactContextActions" || ShortName(value) == "RemoveBuff"))
                return true;
            return instantRestoration;
        }

        // Instantaneous restoration actions (no persistent state of their
        // own). Removing one buff is excluded: toggles remove their own.
        private static bool IsInstantRestoration(string value)
        {
            return Contains(value, "ContextActionHealTarget") ||
                Contains(value, "ContextActionHealEnergyDrain") ||
                Contains(value, "ContextActionHealStatDamage") ||
                Contains(value, "ContextActionResurrect") ||
                Contains(value, "ContextActionRemoveDeathDoor") ||
                Contains(value, "ContextActionDispelMagic") ||
                Contains(value, "ContextActionRemoveBuffsByDescriptor") ||
                Contains(value, "ContextActionBreathOfLife");
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
