using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Discovery;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // WP5 (0.4.0): the beneficial-buff catalogue audit. Each fixture is
        // the exact fact shape the catalogue export recorded for the named
        // ability (abilities, effects, components, action paths and
        // diagnostics), classified under the 0.4.0 rules and under the rules
        // released through 0.3.0, so every regression also proves which rule
        // removed it.
        private static void RunCatalogAuditTests()
        {
            Run("catalog-audit-hideous-laughter-rider-excluded", TestAuditHideousLaughter);
            Run("catalog-audit-treat-affliction-cooldown-excluded", TestAuditTreatAffliction);
            Run("catalog-audit-treat-deadly-wounds-cooldown-excluded", TestAuditTreatDeadlyWounds);
            Run("catalog-audit-harmful-condition-touch-excluded", TestAuditDazingTouch);
            Run("catalog-audit-restoration-tracker-excluded", TestAuditRestorationTracker);
            Run("catalog-audit-hidden-marker-and-cooldown-excluded", TestAuditMarkers);
            Run("catalog-audit-legitimate-buffs-retained", TestAuditLegitimateBuffs);
            Run("catalog-audit-mixed-graphs-keep-only-understood-paths", TestAuditMixedGraphs);
            Run("catalog-audit-summary-counts-and-records", TestAuditSummary);
        }

        private static NativeCandidateEffectFacts AuditEffect(string id, string name, string target,
            string path, bool hidden, params string[] components)
        {
            return new NativeCandidateEffectFacts
            {
                EffectId = id,
                EffectName = name,
                Kind = "Buff",
                Target = target,
                Harmful = false,
                IsHiddenInUi = hidden,
                ComponentTypes = components,
                GrantedConditions = new string[0],
                SourceContract = "ContextActionApplyBuff",
                ActionPath = path
            };
        }

        private static void ExpectAudit(NativeCandidateAuditFacts facts, string expectedCode, string what)
        {
            NativeCandidateAuditDecision now = new NativeCandidateClassifier().Classify(facts);
            NativeCandidateAuditDecision before = new NativeCandidateClassifier(NativeCandidateRuleSet.Pre040)
                .Classify(facts);
            if (before.Disposition != "include")
                throw new InvalidOperationException(what + " was not in the 0.3.0 catalogue: " + before.Reason);
            if (now.Disposition != "exclude" || now.ReasonCode != expectedCode || now.Payloads.Count != 0)
                throw new InvalidOperationException(what + " was not excluded as " + expectedCode + ": " +
                    now.Disposition + " " + now.Reason);
        }

        private static NativeCandidateAuditDecision ExpectRetained(NativeCandidateAuditFacts facts,
            string expectedCode, string what)
        {
            NativeCandidateAuditDecision now = new NativeCandidateClassifier().Classify(facts);
            NativeCandidateAuditDecision before = new NativeCandidateClassifier(NativeCandidateRuleSet.Pre040)
                .Classify(facts);
            if (now.Disposition != "include" || now.ReasonCode != expectedCode || now.Payloads.Count == 0 ||
                before.Disposition != "include")
                throw new InvalidOperationException(what + " was lost: " + now.Reason);
            return now;
        }

        // DomainHideousLaughter (36c3cdad...): Call of the Wild's Infectious
        // Charms adds a caster rider and a stored target marker to the
        // save-gated laughter. 0.3.0 offered the rider as a self buff.
        private static void TestAuditHideousLaughter()
        {
            const string root = "36c3cdad1bae3a55c6039fa53f5371cd/0:ActionList/";
            NativeCandidateEffectFacts laughter = AuditEffect("4b1f07a71a982824988d7f48cd49f3f8",
                "HideousLaughterBuff", "CurrentTarget",
                root + "0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved, Assembly-CSharp, Version=0.0.0.0/0:ActionList/0:ContextActionApplyBuff",
                false, "Kingmaker.Blueprints.Classes.Spells.SpellDescriptorComponent",
                "Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition");
            laughter.GrantedConditions = new[] { "Prone" };
            var facts = new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetEnemies = true,
                EffectOnAlly = "None",
                EffectOnEnemy = "Harmful",
                Range = "Close",
                AbilityComponentTypes = new[]
                {
                    "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction",
                    "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact"
                },
                Effects = new[]
                {
                    laughter,
                    AuditEffect("ca69d5431a723d23cf8cf05397d31758", "InfectiousCharmsHideousLaughterBuff", "Caster",
                        root + "1:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:ContextActionApplyBuff",
                        false, "CallOfTheWild.NewMechanics.ReplaceAbilityParamsWithContext",
                        "Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions"),
                    AuditEffect("65f11ca32dab3a89fa31df259a775677", "InfectiousCharmsHideousLaughterTargetBuff",
                        "CurrentTarget",
                        root + "1:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/1:reflected:CallOfTheWild.AoeMechanics.ApplyActionToTargetsInRange, CallOfTheWild, Version=1.0.0.0/0:ActionList/0:ContextActionApplyBuff",
                        false, "CallOfTheWild.NewMechanics.StoreBuff")
                },
                DiagnosticContracts = new string[0]
            };
            ExpectAudit(facts, "hostile-ability-rider", "Hideous Laughter's Infectious Charms rider");
        }

        // LoreReligionUseAbilityChild (e4af2b1a...): the Heal skill's Treat
        // Affliction dispels and leaves a visible zero-component cooldown.
        // Live discovery judges a party member's ability as reachable.
        private static void TestAuditTreatAffliction()
        {
            const string dispel = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0";
            NativeCandidateEffectFacts cooldown = AuditEffect("b89dcf508d48da74f8dd5234e6a5eb84",
                "LoreReligionCooldown", "CurrentTarget",
                "e4af2b1a6c55435cacc9a305bba93431/0:ActionList/1:ContextActionApplyBuff", false);
            cooldown.IsClassFeature = true;
            ExpectAudit(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetFriends = true,
                EffectOnAlly = "Helpful",
                EffectOnEnemy = "None",
                Range = "Touch",
                Effects = new[] { cooldown },
                DiagnosticContracts = new[] { dispel + "|restorative-action" },
                Diagnostics = new[]
                {
                    new NativeCandidateDiagnosticFacts
                    {
                        Code = "restorative-action",
                        Contract = dispel,
                        Detail = "restorative-action",
                        ActionPath = "e4af2b1a6c55435cacc9a305bba93431/0:ActionList/0:" + dispel
                    }
                }
            }, "reactive-restoration-marker-only", "Treat Affliction's cooldown");
        }

        // TreatDeadlyWoundsHealSkillAbility (54c9837b...): the healing is a
        // Call of the Wild action the scanner does not model; only the
        // visible, component-free cooldown buff remains.
        private static void TestAuditTreatDeadlyWounds()
        {
            const string action = "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0";
            ExpectAudit(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetFriends = true,
                EffectOnAlly = "Helpful",
                EffectOnEnemy = "None",
                Range = "Touch",
                Effects = new[]
                {
                    AuditEffect("45b17e675f524da4b6a0198ac0427f79", "TreatDeadlyWoundsCooldown", "CurrentTarget",
                        "54c9837b07de4afc9e86516b22c460bb/0:ActionList/1:ContextActionApplyBuff", false)
                },
                DiagnosticContracts = new[] { action + "|unsupported-action" },
                Diagnostics = new[]
                {
                    new NativeCandidateDiagnosticFacts
                    {
                        Code = "unknown-node",
                        Contract = action,
                        Detail = "unsupported-action",
                        ActionPath = "54c9837b07de4afc9e86516b22c460bb/0:ActionList/0:" + action
                    }
                }
            }, "mechanics-free-marker-only", "Treat Deadly Wounds' cooldown");
        }

        // EnchantmentSchoolBaseAbilityCast (7b3cb9ad...): a held touch whose
        // only payload dazes the touched creature; its buff forgets
        // m_Harmful, so 0.3.0 offered it as a self buff.
        private static void TestAuditDazingTouch()
        {
            NativeCandidateEffectFacts daze = AuditEffect("9934fedff1b14994ea90205d189c8759", "DazeBuff",
                "CurrentTarget",
                "2acf05a8cb573744db69f65ea6b60619/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:ContextActionApplyBuff",
                false, "Kingmaker.Blueprints.Classes.Spells.SpellDescriptorComponent",
                "Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition",
                "Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions");
            daze.GrantedConditions = new[] { "Dazed" };
            var facts = new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                IsStickyTouch = true,
                CanTargetSelf = true,
                CanTargetEnemies = true,
                EffectOnAlly = "Helpful",
                EffectOnEnemy = "None",
                Range = "Touch",
                AbilityComponentTypes = new[] { "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectStickyTouch" },
                Effects = new[] { daze },
                DiagnosticContracts = new string[0]
            };
            ExpectAudit(facts, "harmful-only", "Dazing Touch");
            // The same Dazed condition beside real mechanics is a side effect,
            // not harm: such a buff is judged by its targeting, not dropped.
            daze.ComponentTypes = daze.ComponentTypes.Concat(new[]
                { "Kingmaker.UnitLogic.FactLogic.AddStatBonus" }).ToArray();
            facts.CanTargetFriends = true;
            facts.CanTargetEnemies = false;
            facts.IsStickyTouch = false;
            ExpectRetained(facts, "valid-beneficial-party-effect", "A buff with a condition side effect");
        }

        // A hidden tracking buff beside an instantaneous heal (Inspiring
        // Recovery's check buff): the heal is the ability's point.
        private static void TestAuditRestorationTracker()
        {
            const string heal = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0";
            ExpectAudit(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetFriends = true,
                EffectOnAlly = "Helpful",
                Range = "Touch",
                Effects = new[]
                {
                    AuditEffect("dc9a8ddf45adbc74aaff7b309f232072", "InspiringRecoveryCheckBuff", "CurrentTarget",
                        "788d72e7713cf90418ee1f38449416dc/0:ActionList/1:ContextActionApplyBuff", true,
                        "Kingmaker.UnitLogic.Buffs.Components.AddStatBonusIfHasFact")
                },
                DiagnosticContracts = new[] { heal + "|restorative-action" },
                Diagnostics = new[]
                {
                    new NativeCandidateDiagnosticFacts
                    {
                        Code = "restorative-action",
                        Contract = heal,
                        Detail = "restorative-action",
                        ActionPath = "788d72e7713cf90418ee1f38449416dc/0:ActionList/0:" + heal
                    }
                }
            }, "reactive-restoration-marker-only", "An instantaneous restoration's tracker");
        }

        private static void TestAuditMarkers()
        {
            // A hidden activation marker whose components only keep its books
            // and run on-apply actions (UniqueBuff was not a 0.3.0 marker
            // component).
            ExpectAudit(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                Effects = new[]
                {
                    AuditEffect("24afb2c948c731440a3aaf5411904c89", "TargetedBombAdmixtureBuff", "Caster",
                        "root/0:ContextActionApplyBuff", true, "Kingmaker.Designers.Mechanics.Buffs.UniqueBuff",
                        "Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions")
                },
                DiagnosticContracts = new string[0]
            }, "hidden-marker-only", "A hidden activation marker");
            // A visible hex cooldown (Battle Ward): bookkeeping only.
            ExpectAudit(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetFriends = true,
                EffectOnAlly = "Helpful",
                Effects = new[]
                {
                    AuditEffect("a16cc3943b08420da6f50f036ed8b616", "SpiritWhispererBattleWardHexAbilityCooldownBuff",
                        "CurrentTarget", "root/1:ContextActionApplyBuff", false,
                        "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig")
                },
                DiagnosticContracts = new string[0]
            }, "mechanics-free-marker-only", "A visible hex cooldown");
            // The proven enchant-pool signal buff keeps its explicit adapter.
            NativeCandidateAuditDecision pool = ExpectRetained(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                Effects = new[]
                {
                    AuditEffect("signal", "ArcaneWeaponSignalBuff", "CurrentTarget", "root/0:ContextActionApplyBuff",
                        false)
                },
                DiagnosticContracts = new[] { "ContextActionWeaponEnchantPool|unsupported-action" }
            }, "valid-beneficial-self-effect", "The enchant-pool signal buff");
            if (pool.SupportClass != "explicit-adapter")
                throw new InvalidOperationException("The enchant-pool signal lost its explicit adapter.");
        }

        private static void TestAuditLegitimateBuffs()
        {
            // Self: Mirror Image.
            ExpectRetained(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                Range = "Personal",
                Effects = new[]
                {
                    AuditEffect("98dc7e7cc6ef59f4abe20c65708ac623", "MirrorImageBuff", "CurrentTarget",
                        "3e4ab69ada402d145a5e0ad3ad4b8564/0:ActionList/0:ContextActionApplyBuff", false,
                        "Kingmaker.UnitLogic.Buffs.Components.MirrorImage",
                        "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig")
                },
                DiagnosticContracts = new string[0]
            }, "valid-beneficial-self-effect", "Mirror Image (self)");
            // Single ally: Bull's Strength.
            ExpectRetained(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetFriends = true,
                EffectOnAlly = "Helpful",
                Range = "Touch",
                Effects = new[]
                {
                    AuditEffect("4c3d08935262b6544ae97599b3a9556d", "BullsStrengthBuff", "CurrentTarget",
                        "4c3d08935262b6544ae97599b3a9556d/0:ActionList/0:ContextActionApplyBuff", false,
                        "Kingmaker.UnitLogic.FactLogic.AddContextStatBonus",
                        "Kingmaker.Blueprints.Classes.Spells.SpellDescriptorComponent")
                },
                DiagnosticContracts = new string[0]
            }, "valid-beneficial-party-effect", "Bull's Strength (single ally)");
            // Party / area: Bless.
            ExpectRetained(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                Range = "Personal",
                Effects = new[]
                {
                    AuditEffect("87ab2fed7feaaff47b62a3320a57ad8d", "BlessBuff", "AlliedAreaRecipients",
                        "90e59f4a4ada87243b7b3535a06d0638/0:AbilityTargetsAround/0:ActionList/0:ContextActionApplyBuff",
                        false, "Kingmaker.UnitLogic.FactLogic.AddStatBonus",
                        "Kingmaker.Designers.Mechanics.Buffs.BuffAllSavesBonus")
                },
                DiagnosticContracts = new string[0]
            }, "valid-beneficial-party-effect", "Bless (party)");
            // Ability / activatable: Rage, a supernatural self buff.
            ExpectRetained(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                Range = "Personal",
                Effects = new[]
                {
                    AuditEffect("da8ce41ac3cd74742b80984ccc3c9613", "StandartRageBuff", "Caster",
                        "root/0:ContextActionApplyBuff", false,
                        "Kingmaker.UnitLogic.FactLogic.AddStatBonus",
                        "Kingmaker.UnitLogic.FactLogic.AddCondition")
                },
                DiagnosticContracts = new string[0]
            }, "valid-beneficial-self-effect", "Rage (ability)");
            // Item enchantment: Magic Weapon on the wielded weapon.
            NativeCandidateEffectFacts weapon = AuditEffect("d42fc23b92c640846ac137dc26e000d4",
                "Enhancement1", "CurrentTarget", "root/0:ContextActionEnchantWornItem", false,
                "Kingmaker.Designers.Mechanics.EquipmentEnchants.WeaponEnhancementBonus");
            weapon.Kind = "WornItemEnchantment";
            weapon.SourceContract = "ContextActionEnchantWornItem";
            NativeCandidateAuditDecision item = ExpectRetained(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetFriends = true,
                EffectOnAlly = "Helpful",
                Effects = new[] { weapon },
                DiagnosticContracts = new string[0]
            }, "valid-beneficial-party-effect", "Magic Weapon (item enchantment)");
            if (item.SupportClass != "explicit-adapter")
                throw new InvalidOperationException("The worn-item enchantment lost its explicit adapter.");
        }

        private static void TestAuditMixedGraphs()
        {
            // Magic Circle against Alignment (KingmakerGunslinger): an ally
            // branch applies the carrier, the other branch applies it only on
            // a failed Will save. The understood ally path is the payload;
            // the save-gated path is not.
            const string conditional = "root/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/";
            NativeCandidateAuditDecision circle = ExpectRetained(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetFriends = true,
                CanTargetEnemies = true,
                IsStickyTouch = true,
                EffectOnAlly = "Helpful",
                EffectOnEnemy = "Helpful",
                Effects = new[]
                {
                    AuditEffect("carrier", "KMG_MagicCircle_Evil_Carrier", "CurrentTarget",
                        conditional + "true/0:ContextActionApplyBuff", false,
                        "Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect"),
                    AuditEffect("carrier", "KMG_MagicCircle_Evil_Carrier", "CurrentTarget",
                        conditional + "false/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSavingThrow, Assembly-CSharp, Version=0.0.0.0/0:ActionList/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved, Assembly-CSharp, Version=0.0.0.0/false/0:ContextActionApplyBuff",
                        false, "Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect")
                },
                DiagnosticContracts = new string[0]
            }, "valid-beneficial-party-effect", "Magic Circle against Alignment");
            if (circle.Payloads.Count != 1 || !circle.Payloads[0].ActionPath.EndsWith(
                    "true/0:ContextActionApplyBuff", StringComparison.Ordinal))
                throw new InvalidOperationException("The save-gated branch became a planned payload.");

            // A buff on the same branch as damage fails closed.
            ExpectExcluded(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetFriends = true,
                EffectOnAlly = "Helpful",
                Effects = new[]
                {
                    AuditEffect("vampiric", "VampiricTouchBuff", "Caster", "root/0:ContextActionApplyBuff", false,
                        "Kingmaker.UnitLogic.FactLogic.TemporaryHitPointsFromAbilityValue")
                },
                Diagnostics = new[]
                {
                    new NativeCandidateDiagnosticFacts
                    {
                        Code = "offensive-action",
                        Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage",
                        Detail = "offensive-action",
                        ActionPath = "root/1:damage"
                    }
                }
            }, "offensive-carrier-only", "A buff beside damage on the same branch");

            // A charm whose only persistent effect changes the bearer's
            // faction is harm, even when a friend could be targeted.
            NativeCandidateEffectFacts charm = AuditEffect("charm", "CharmPersonBuff", "CurrentTarget",
                "root/0:ContextActionApplyBuff", false, "Kingmaker.UnitLogic.FactLogic.ChangeFaction",
                "Kingmaker.UnitLogic.FactLogic.AddStatBonus");
            ExpectExcluded(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetFriends = true,
                CanTargetEnemies = true,
                EffectOnAlly = "None",
                EffectOnEnemy = "None",
                Effects = new[] { charm },
                DiagnosticContracts = new string[0]
            }, "harmful-only", "A faction-changing charm");
        }

        private static void ExpectExcluded(NativeCandidateAuditFacts facts, string expectedCode, string what)
        {
            NativeCandidateAuditDecision now = new NativeCandidateClassifier().Classify(facts);
            if (now.Disposition != "exclude" || now.ReasonCode != expectedCode)
                throw new InvalidOperationException(what + " was not excluded as " + expectedCode + ": " +
                    now.Reason);
        }

        private static void TestAuditSummary()
        {
            Func<string, string, bool, string, string, string, string, NativeCatalogAuditInput> input =
                (guid, name, candidate, before, now, liveBefore, liveNow) => new NativeCatalogAuditInput
                {
                    AbilityGuid = guid,
                    DisplayName = name,
                    Ownership = guid.StartsWith("c", StringComparison.Ordinal) ? "call-of-the-wild" : "native",
                    AbilityType = "Spell",
                    IsSpell = true,
                    IsCandidate = candidate,
                    FirstAccessibilitySource = candidate ? "player-class:x/spell-list:y/level:1" : null,
                    CanTargetFriends = true,
                    SupportClass = "automatic",
                    QualificationStatus = "DEFER-runtime-qualification",
                    DispositionBefore040 = before,
                    Disposition = now,
                    DispositionReason = now == "include" ? "valid-beneficial-party-effect: kept"
                        : "hostile-ability-rider: removed",
                    LiveDispositionBefore040 = liveBefore,
                    LiveDisposition = liveNow,
                    LiveDispositionReason = liveNow == "include" ? "valid-beneficial-party-effect: kept"
                        : "reactive-restoration-marker-only: removed",
                    Effects = new[]
                    {
                        new NativeCatalogAuditEffect { EffectGuid = "e-" + guid, EffectName = "Buff", Kind = "Buff",
                            Target = "CurrentTarget" }
                    },
                    Payloads = now == "include" || liveNow == "include" ? new[] { "e-" + guid } : new string[0]
                };
            var inputs = new[]
            {
                input("aaaa", "Bull's Strength", true, "include", "include", "include", "include"),
                input("cccc", "Hideous Laughter", true, "include", "exclude", "include", "exclude"),
                input("bbbb", "Treat Affliction", false, "exclude", "exclude", "include", "exclude"),
                input("dddd", "Fireball", true, "exclude", "exclude", "exclude", "exclude")
            };
            NativeCatalogAuditDocument document = NativeCatalogAudit.Document("full-user", "commit", inputs);
            NativeCatalogAuditCounts totals = document.Summary.Totals;
            if (totals.StaticIncludedBefore040 != 2 || totals.StaticIncluded != 1 ||
                totals.LiveIncludedBefore040 != 3 || totals.LiveIncluded != 1 || document.Summary.AddedCount != 0)
                throw new InvalidOperationException("Audit totals are wrong.");
            if (document.Summary.RemovedByReason["hostile-ability-rider"] != 1 ||
                document.Summary.RemovedByReason["reactive-restoration-marker-only"] != 1 ||
                document.Summary.RemovedByReason.Count != 2)
                throw new InvalidOperationException("Audit removal reasons are wrong.");
            if (document.Summary.ByOwnership["call-of-the-wild"].StaticIncluded != 0 ||
                document.Summary.ByOwnership["native"].LiveIncludedBefore040 != 2)
                throw new InvalidOperationException("Audit ownership counts are wrong.");
            if (document.Records.Length != 3 || document.Records.Any(r => r.DisplayName == "Fireball"))
                throw new InvalidOperationException("Audit records are not exactly the included and removed sources.");
            NativeCatalogAuditRecord kept = document.Records.Single(r => r.DisplayName == "Bull's Strength");
            NativeCatalogAuditRecord rider = document.Records.Single(r => r.DisplayName == "Hideous Laughter");
            NativeCatalogAuditRecord treat = document.Records.Single(r => r.DisplayName == "Treat Affliction");
            if (kept.Status != NativeCatalogAudit.Included || kept.InclusionRule != "valid-beneficial-party-effect" ||
                kept.PersistentBeneficialEffects.Length != 1 || kept.TargetSemantics != "CurrentTarget(self-or-ally)" ||
                kept.Scope != "static" || kept.ExclusionReason != string.Empty)
                throw new InvalidOperationException("An included audit record is incomplete.");
            if (rider.Status != NativeCatalogAudit.Removed ||
                !rider.ExclusionReason.StartsWith("hostile-ability-rider:", StringComparison.Ordinal) ||
                rider.PersistentBeneficialEffects.Length != 0)
                throw new InvalidOperationException("A removed static record does not say why.");
            if (treat.Status != NativeCatalogAudit.Removed || treat.Scope != "live-only" ||
                !treat.Provider.StartsWith("live-only", StringComparison.Ordinal) ||
                !treat.ExclusionReason.StartsWith("reactive-restoration-marker-only:", StringComparison.Ordinal))
                throw new InvalidOperationException("A removed live-only record does not say why.");
        }
    }
}
