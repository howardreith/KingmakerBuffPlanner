using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Discovery;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // rc4 lead review, finding 1: the 0.4.0 audit removed real buffs
        // under marker rules that read "no mechanics of its own" as "not a
        // buff". Every fixture below is the exact fact set the rc3 catalogue
        // export recorded for the named ability (generated from
        // kbp040-rc3-catalog-*/native-buff-catalog.json, not restated from a
        // rule); facts rc4 adds - an ability's self-gating facts, a buff's own
        // fact actions, the effects the exact Call of the Wild adapters now
        // read - are added explicitly and cite their source.
        private static void RunCatalogAdjudicationTests()
        {
            Run("catalog-adjudication-buffs-without-own-mechanics-retained", TestAdjudicationFlagBuffs);
            Run("catalog-adjudication-hex-ward-is-its-ward-not-its-cooldown", TestAdjudicationHexWard);
            Run("catalog-adjudication-restorations-and-their-markers-excluded", TestAdjudicationRestorations);
            Run("catalog-adjudication-hidden-buff-actions-carrier-marker-or-opaque", TestAdjudicationHiddenActions);
        }

        private static NativeCandidateAuditFacts Gated(NativeCandidateAuditFacts facts, params string[] ids)
        {
            facts.SelfGatedFactIds = ids;
            return facts;
        }

        private static NativeCandidateFactActions FactList(string list, IEnumerable<NativeCandidateEffectFacts> applied,
            IEnumerable<string> restorative = null, IEnumerable<string> unrecognized = null)
        {
            return new NativeCandidateFactActions
            {
                List = list,
                AppliedEffects = (applied ?? new NativeCandidateEffectFacts[0]).ToArray(),
                Restorative = (restorative ?? new string[0]).ToArray(),
                Offensive = new string[0],
                Unrecognized = (unrecognized ?? new string[0]).ToArray()
            };
        }

        // Retained under both rule sets, and excluded by the rc3 rule that
        // treated a buff with no mechanics of its own as a marker.
        private static NativeCandidateAuditDecision ExpectRestored(NativeCandidateAuditFacts facts, string what)
        {
            NativeCandidateAuditDecision now = new NativeCandidateClassifier().Classify(facts);
            NativeCandidateAuditDecision before = new NativeCandidateClassifier(NativeCandidateRuleSet.Pre040)
                .Classify(facts);
            if (now.Disposition != "include" || now.Payloads.Count == 0 || before.Disposition != "include")
                throw new InvalidOperationException(what + " is not a buff in the catalogue: " + now.Reason);
            return now;
        }

        private static void TestAdjudicationFlagBuffs()
        {
            // Targeted Bomb Admixture: the visible buff has no components;
            // the alchemist's bombs read it. Its presence is the state.
            NativeCandidateAuditDecision bomb = ExpectRestored(RealTargetedBombAdmixture(), "Targeted Bomb Admixture");
            if (bomb.Payloads.Single().EffectId != "768b4b33721a36d4c8030e4878a13d28")
                throw new InvalidOperationException("Targeted Bomb Admixture lost its buff as payload.");
            // Light and Daylight: utility light, a deliberate product-scope
            // inclusion (their buffs carry UniqueBuff / no components).
            ExpectRestored(RealLight(), "Light");
            ExpectRestored(RealDaylight(), "Daylight");
            // Elemental Bastion; and the same buff guarded against stacking
            // (the ability forbids its caster to have it) is still the
            // ability's own effect, not a lockout: nothing else is done.
            ExpectRestored(RealElementalBastion(), "Elemental Bastion");
            ExpectRestored(Gated(RealElementalBastion(), "99953956704788444964899b5b8e96ab"),
                "Elemental Bastion guarded against stacking");
            // Call of the Wild's Venomous Strike selection (the brawler's
            // strikes read the selected buff) and Arcanist School
            // Understanding activation.
            ExpectRestored(RealVenomousStrikeBlind(), "Venomous Strike: Blindness");
            ExpectRestored(RealSchoolUnderstandingAbjuration(), "Activate School Understanding (Abjuration)");
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

        // ShamanBattleWardHexAbility: rc3 saw only the hex cooldown, because
        // the ward buff is applied through Call of the Wild's
        // RunActionsDependingOnContextValue. The exact adapter now reads its
        // value-selected lists: ShamanBattleWard1Buff..5Buff (GUIDs from Call
        // of the Wild's own loaded_blueprints.txt; AddStatBonus AC deflection
        // plus AddTargetAttackRollTrigger, ShamanHexes.createBattleWardHex).
        // AbilityTargetHasNoFactUnlessBuffsFromCaster forbids recasting over
        // the cooldown (HexEngine.addWitchHexCooldownScaling).
        private static void TestAdjudicationHexWard()
        {
            NativeCandidateAuditFacts ward = RealShamanBattleWard();
            NativeCandidateEffectFacts cooldown = ward.Effects.Single();
            string[] wardBuffs =
            {
                "219440d0fc5843d5a1942aacd43a92d0", "77d0424e938f455cb1ef14423bcf59ad",
                "6884f0602a594144afb8365318494a32", "57b097b06d294ab9b69f06a9eadf6e95",
                "68966a356c1b43d28c199b740f19dcbd"
            };
            const string adapter = "8bc6ba5c82d44deaaef24cec75490d05/0:ActionList/0:RunActionsDependingOnContextValue";
            ward.Effects = wardBuffs.Select((guid, index) => new NativeCandidateEffectFacts
            {
                EffectId = guid,
                EffectName = "ShamanBattleWard" + (index + 1) + "Buff",
                Kind = "Buff",
                Target = "CurrentTarget",
                Harmful = false,
                ComponentTypes = new[]
                {
                    "Kingmaker.UnitLogic.FactLogic.AddStatBonus",
                    "Kingmaker.UnitLogic.Mechanics.Components.AddTargetAttackRollTrigger"
                },
                GrantedConditions = new string[0],
                SourceContract = "ContextActionApplyBuff",
                ActionPath = adapter + string.Concat(Enumerable.Repeat("/false", index)) +
                    (index == wardBuffs.Length - 1 ? string.Empty : "/true") + "/0:ContextActionApplyBuff"
            }).Concat(new[] { cooldown }).ToArray();
            ward.DiagnosticContracts = new string[0];
            ward.Diagnostics = new NativeCandidateDiagnosticFacts[0];
            NativeCandidateAuditDecision decision = new NativeCandidateClassifier()
                .Classify(Gated(ward, cooldown.EffectId));
            if (decision.Disposition != "include" ||
                !decision.Payloads.Select(e => e.EffectId).OrderBy(v => v, StringComparer.Ordinal)
                    .SequenceEqual(wardBuffs.OrderBy(v => v, StringComparer.Ordinal)))
                throw new InvalidOperationException("Battle Ward is not its ward buff (the cooldown is a lockout): " +
                    decision.Reason + " payloads=" + string.Join(",", decision.Payloads.Select(e => e.EffectName).ToArray()));
        }

        private static void TestAdjudicationRestorations()
        {
            // Treat Affliction (the Heal skill): a dispel and its visible
            // zero-component cooldown, with and without its own gate.
            ExpectAudit(RealTreatAffliction(), "reactive-restoration-marker-only", "Treat Affliction");
            ExpectAudit(Gated(RealTreatAffliction(), RealTreatAffliction().Effects.Single().EffectId),
                "reactive-restoration-marker-only", "Treat Affliction, gated");
            // Treat Deadly Wounds: the exact adapter now reads Call of the
            // Wild's ContextActionTreatDeadlyWounds as a restoration (it heals
            // hit points and ability damage, HealingMechanics), and the
            // ability forbids recasting over its cooldown
            // (createAbilityTargetHasFact(inverted: true), SkillUnlocks).
            NativeCandidateAuditFacts deadly = RealTreatDeadlyWounds();
            deadly.Diagnostics = deadly.Diagnostics.Select(d => new NativeCandidateDiagnosticFacts
            {
                Code = "restorative-action",
                Contract = d.Contract,
                Detail = "restorative-action",
                ActionPath = d.ActionPath
            }).ToArray();
            deadly.DiagnosticContracts = deadly.Diagnostics.Select(d => d.Contract + "|restorative-action").ToArray();
            ExpectAudit(Gated(deadly, "45b17e675f524da4b6a0198ac0427f79"), "reactive-restoration-marker-only",
                "Treat Deadly Wounds");
            // Kinetic Healer's Burn Offload: the hidden BurnOtherBuff adds
            // nonlethal damage to the healed ally (KineticistFix: StatType
            // DamageNonLethal) - the heal's cost, not a buff.
            ExpectAudit(RealKineticHealerBurnOffload(), "reactive-restoration-marker-only", "Burn Offload");
            // Counter Curse: a hidden dispel-check bonus the same action list
            // removes after the dispel.
            ExpectAudit(RealCounterCurseDispel(), "reactive-restoration-marker-only", "Counter Curse");
            // Warpriest channel (Heal Living): a one-round, component-free
            // enabler the major Repose blessing ability requires of its
            // caster (Warpriest.cs) - the heal is the point.
            ExpectAudit(RealChannelHealLiving(), "reactive-restoration-marker-only", "Channel Positive Energy");
        }

        // Inspiring Recovery's hidden check buff (ReplaceAbilityParamsWithContext
        // + AddFactContextActions) beside a heal: what its own actions do
        // decides it - never the assumption that such a buff is bookkeeping.
        private static void TestAdjudicationHiddenActions()
        {
            Func<NativeCandidateFactActions[], NativeCandidateAuditFacts> recovery = lists =>
            {
                NativeCandidateAuditFacts facts = RealInspiringRecovery();
                facts.Effects.Single().FactActions = lists;
                // The rule demonstration below isolates the check buff from
                // the heal and the undead-damage branch.
                facts.Diagnostics = new NativeCandidateDiagnosticFacts[0];
                facts.DiagnosticContracts = new string[0];
                return facts;
            };
            var morale = new NativeCandidateEffectFacts
            {
                EffectId = "fixture-applied", EffectName = "AppliedMoraleBuff", Kind = "Buff",
                Target = "CurrentTarget", Harmful = false,
                ComponentTypes = new[] { "Kingmaker.UnitLogic.FactLogic.AddContextStatBonus" },
                GrantedConditions = new string[0], SourceContract = "ContextActionApplyBuff",
                ActionPath = "fixture/0:ContextActionApplyBuff"
            };
            // A hidden buff that applies a beneficial buff carries it.
            NativeCandidateAuditDecision carrier = new NativeCandidateClassifier().Classify(
                recovery(new[] { FactList("Activated", new[] { morale }) }));
            if (carrier.Disposition != "include")
                throw new InvalidOperationException("A hidden carrier of a beneficial buff was dropped: " + carrier.Reason);
            // One whose actions only remove buffs is proved bookkeeping.
            NativeCandidateAuditDecision marker = new NativeCandidateClassifier().Classify(recovery(new[]
            {
                FactList("Deactivated", null,
                    new[] { "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff, Assembly-CSharp, Version=0.0.0.0" })
            }));
            if (marker.Disposition != "exclude")
                throw new InvalidOperationException("A hidden bookkeeping buff was kept: " + marker.Reason);
            // One running an action the adapter cannot read is unsupported,
            // with the action named - neither assumed bookkeeping nor a buff.
            NativeCandidateAuditDecision opaque = new NativeCandidateClassifier().Classify(recovery(new[]
            {
                FactList("NewRound", null, null, new[] { "unknown-node:Fixture.UnknownAction, Fixture, Version=1.0.0.0" })
            }));
            if (opaque.Disposition != "unsupported-with-reason" || opaque.ReasonCode != "opaque-hidden-buff-actions" ||
                opaque.Reason.IndexOf("Fixture.UnknownAction", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("A hidden buff with unread actions was not unsupported: " + opaque.Reason);
            // Under the rules released through 0.3.0 the check buff was a
            // payload (ReplaceAbilityParamsWithContext was no marker
            // component): the audit only ever removes.
            if (new NativeCandidateClassifier(NativeCandidateRuleSet.Pre040).Classify(recovery(new[]
                    { FactList("NewRound", null, null, new[] { "unknown-node:Fixture.UnknownAction" }) }))
                .Disposition != "include")
                throw new InvalidOperationException("Inspiring Recovery's 0.3.0 classification changed.");
        }

        // Targeted Bomb Admixture (24afb2c948c731440a3aaf5411904c89, TargetedBombAdmixture): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealTargetedBombAdmixture()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "None", EffectOnEnemy = "None", Range = "Personal",
                AbilityComponentTypes = new[] { "Kingmaker.Blueprints.Classes.Spells.SpellComponent", "Kingmaker.Blueprints.Classes.Spells.SpellListComponent", "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "768b4b33721a36d4c8030e4878a13d28", EffectName = "TargetedBombAdmixtureBuff", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = false,
                        ComponentTypes = new string[0],
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "24afb2c948c731440a3aaf5411904c89/0:ActionList/0:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new string[0],
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {

                }
            };
        }

        // Light (95f206566c5261c42aa5b3e7e0d1e36c, MageLight): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealLight()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "None", EffectOnEnemy = "None", Range = "Touch",
                AbilityComponentTypes = new[] { "Kingmaker.Blueprints.Classes.Spells.CantripComponent", "Kingmaker.Blueprints.Classes.Spells.SpellComponent", "Kingmaker.Blueprints.Classes.Spells.SpellListComponent", "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.AbilityExecuteActionOnCast", "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetIsPartyMember" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "571baa4cf65bbcb4996fe429ca77d1a5", EffectName = "MageLightBuff", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = false,
                        ComponentTypes = new[] { "Kingmaker.UnitLogic.FactLogic.UniqueBuff" },
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "95f206566c5261c42aa5b3e7e0d1e36c/0:ActionList/0:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new string[0],
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {

                }
            };
        }

        // Daylight (2b877386976817a429002e8bb10bb3fc, DayLight): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealDaylight()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "None", EffectOnEnemy = "None", Range = "Touch",
                AbilityComponentTypes = new[] { "Kingmaker.Blueprints.Classes.Spells.SpellComponent", "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.AbilityExecuteActionOnCast", "Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx", "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetIsPartyMember", "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "5da5d4e1e4ac5db428999a88df4f6bfe", EffectName = "DayLightBuff", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = false,
                        ComponentTypes = new string[0],
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "2b877386976817a429002e8bb10bb3fc/0:ActionList/0:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new string[0],
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {

                }
            };
        }

        // Elemental Bastion (af6e27aa6e300454580d7de074ff315a, ElementalBastionAbility): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealElementalBastion()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = false, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "None", EffectOnEnemy = "None", Range = "Personal",
                AbilityComponentTypes = new[] { "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasNoFacts" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "99953956704788444964899b5b8e96ab", EffectName = "ElementalBastionBuff", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = true,
                        ComponentTypes = new string[0],
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "af6e27aa6e300454580d7de074ff315a/0:ActionList/0:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new[] { "Kingmaker.UnitLogic.Class.Kineticist.Actions.ContextActionAcceptBurn, Assembly-CSharp, Version=0.0.0.0|unsupported-action" },
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "Kingmaker.UnitLogic.Class.Kineticist.Actions.ContextActionAcceptBurn, Assembly-CSharp, Version=0.0.0.0", Detail = "unsupported-action", ActionPath = "af6e27aa6e300454580d7de074ff315a/0:ActionList/1:Kingmaker.UnitLogic.Class.Kineticist.Actions.ContextActionAcceptBurn, Assembly-CSharp, Version=0.0.0.0" }
                }
            };
        }

        // Venomous Strike: Blindness (a15cc14cb4ae4f7eadca522accc93fcb, BlindVenomousStrikeAbility): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealVenomousStrikeBlind()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = false, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "Helpful", EffectOnEnemy = "None", Range = "Personal",
                AbilityComponentTypes = new[] { "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "Kingmaker.UnitLogic.Abilities.Components.AbilityShowIfCasterHasFact", "Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateAbilityParamsBasedOnClass" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "8afa153ed36f4ce085972cddad694d6e", EffectName = "VenomousStrikeBlind", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = false,
                        ComponentTypes = new string[0],
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "a15cc14cb4ae4f7eadca522accc93fcb/0:ActionList/1:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new[] { "CallOfTheWild.NewMechanics.ContextActionRemoveBuffs, CallOfTheWild, Version=1.0.0.0|unsupported-action" },
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "CallOfTheWild.NewMechanics.ContextActionRemoveBuffs, CallOfTheWild, Version=1.0.0.0", Detail = "unsupported-action", ActionPath = "a15cc14cb4ae4f7eadca522accc93fcb/0:ActionList/0:CallOfTheWild.NewMechanics.ContextActionRemoveBuffs, CallOfTheWild, Version=1.0.0.0" }
                }
            };
        }

        // Activate School Understanding (Specialist School — Abjuration) (9e6fa939eab643d4a1ba6c984a4bcdaa, SchoolUnderstangingAbjurationBuffAbility): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealSchoolUnderstandingAbjuration()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = false, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "Helpful", EffectOnEnemy = "None", Range = "Personal",
                AbilityComponentTypes = new[] { "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "fd04b43c3d8c49fabb5833d1bf3ec60f", EffectName = "SchoolUnderstangingAbjurationBuff", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = false,
                        ComponentTypes = new string[0],
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "9e6fa939eab643d4a1ba6c984a4bcdaa/0:ActionList/0:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new string[0],
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {

                }
            };
        }

        // Kinetic Healer: Burn Offload (ff91b86df91d42549bb319e75c47df66, KineticHealerBurnOtherAbility): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealKineticHealerBurnOffload()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = false, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "None", EffectOnEnemy = "None", Range = "Touch",
                AbilityComponentTypes = new[] { "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.ActionPanelLogic", "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact", "Kingmaker.UnitLogic.Class.Kineticist.AbilityKineticist", "Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateSharedValue", "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "ee84221d7b2949719f49196557c256d6", EffectName = "BurnOtherBuff", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = true, IsClassFeature = false,
                        ComponentTypes = new[] { "Kingmaker.UnitLogic.FactLogic.AddContextStatBonus", "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig" },
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/3:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new[] { "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealStatDamage, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealStatDamage, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealEnergyDrain, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx, Assembly-CSharp, Version=0.0.0.0|unsupported-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0|restorative-action" },
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealStatDamage, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/1:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:f2115ac1148256b4ba20788f7e966830/0:f2115ac1148256b4ba20788f7e966830/0:ActionList/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealStatDamage, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealStatDamage, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/1:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:f2115ac1148256b4ba20788f7e966830/0:f2115ac1148256b4ba20788f7e966830/0:ActionList/1:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealStatDamage, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/1:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:f2115ac1148256b4ba20788f7e966830/0:f2115ac1148256b4ba20788f7e966830/0:ActionList/2:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealEnergyDrain, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/1:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:f2115ac1148256b4ba20788f7e966830/0:f2115ac1148256b4ba20788f7e966830/0:ActionList/3:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealEnergyDrain, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx, Assembly-CSharp, Version=0.0.0.0", Detail = "unsupported-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/2:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/4:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/5:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/6:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/7:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/8:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/9:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/10:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/11:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/12:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/13:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/14:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/15:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/16:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/17:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "ff91b86df91d42549bb319e75c47df66/0:ActionList/18:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor, Assembly-CSharp, Version=0.0.0.0" }
                }
            };
        }

        // Counter Curse: Dispel (41523716359c4fc88c40a44a6179d6ca, CounterCurseDispel1Ability): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealCounterCurseDispel()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "None", EffectOnEnemy = "None", Range = "Medium",
                AbilityComponentTypes = new[] { "Kingmaker.Blueprints.Classes.Spells.SpellComponent", "Kingmaker.Blueprints.Classes.Spells.SpellListComponent", "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "cd1bf0df10024d4c8ddfa9674828da37", EffectName = "CounterCurse1Buff", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = true, IsClassFeature = false,
                        ComponentTypes = new[] { "Kingmaker.Designers.Mechanics.Facts.DispelCasterLevelCheckBonus" },
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "41523716359c4fc88c40a44a6179d6ca/0:ActionList/0:reflected:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionOnContextCaster, Assembly-CSharp, Version=0.0.0.0/0:ActionList/0:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new[] { "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff, Assembly-CSharp, Version=0.0.0.0|restorative-action" },
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "41523716359c4fc88c40a44a6179d6ca/0:ActionList/1:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "41523716359c4fc88c40a44a6179d6ca/0:ActionList/2:reflected:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionOnContextCaster, Assembly-CSharp, Version=0.0.0.0/0:ActionList/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff, Assembly-CSharp, Version=0.0.0.0" }
                }
            };
        }

        // Channel Positive Energy — Heal Living (5d20cd71566d4e2d8a2fd2de5d63454b, WarpriestChannelEnergyHealLiving): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealChannelHealLiving()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "None", EffectOnEnemy = "Harmful", Range = "Personal",
                AbilityComponentTypes = new[] { "CallOfTheWild.NewMechanics.ContextCalculateAbilityParamsBasedOnClasses", "Kingmaker.Blueprints.Classes.Spells.SpellDescriptorComponent", "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.AbilityResourceLogic", "Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround", "Kingmaker.UnitLogic.Abilities.Components.AbilityUseOnRest", "Kingmaker.UnitLogic.Abilities.Components.ActionPanelLogic", "Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx", "Kingmaker.UnitLogic.Mechanics.Components.ContextCalculateSharedValue", "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "e3049d37d3574844bd46d6cfb6bc7988", EffectName = "WarpriestReposeBlessingMajorBuff", Kind = "Buff", Target = "AlliedAreaRecipients",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = false,
                        ComponentTypes = new string[0],
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "5d20cd71566d4e2d8a2fd2de5d63454b/0:AbilityTargetsAround/0:ActionList/1:reflected:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionOnContextCaster, Assembly-CSharp, Version=0.0.0.0/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new[] { "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx, Assembly-CSharp, Version=0.0.0.0|unsupported-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx, Assembly-CSharp, Version=0.0.0.0|unsupported-action" },
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "5d20cd71566d4e2d8a2fd2de5d63454b/0:AbilityTargetsAround/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx, Assembly-CSharp, Version=0.0.0.0", Detail = "unsupported-action", ActionPath = "5d20cd71566d4e2d8a2fd2de5d63454b/0:AbilityTargetsAround/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/1:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "5d20cd71566d4e2d8a2fd2de5d63454b/0:AbilityTargetsAround/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx, Assembly-CSharp, Version=0.0.0.0", Detail = "unsupported-action", ActionPath = "5d20cd71566d4e2d8a2fd2de5d63454b/0:AbilityTargetsAround/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/1:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx, Assembly-CSharp, Version=0.0.0.0" }
                }
            };
        }

        // Treat Affliction (4843cb4c23951f54290c5149a4907f54, LoreReligionUseAbility): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealTreatAffliction()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "Helpful", EffectOnEnemy = "None", Range = "Medium",
                AbilityComponentTypes = new[] { "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "b89dcf508d48da74f8dd5234e6a5eb84", EffectName = "LoreReligionCooldown", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = true,
                        ComponentTypes = new string[0],
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "4843cb4c23951f54290c5149a4907f54/0:ActionList/1:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new[] { "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0|restorative-action" },
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "4843cb4c23951f54290c5149a4907f54/0:ActionList/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic, Assembly-CSharp, Version=0.0.0.0" }
                }
            };
        }

        // Treat Deadly Wounds (54c9837b07de4afc9e86516b22c460bb, TreatDeadlyWoundsHealSkillAbility): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealTreatDeadlyWounds()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "Helpful", EffectOnEnemy = "None", Range = "Touch",
                AbilityComponentTypes = new[] { "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact", "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "45b17e675f524da4b6a0198ac0427f79", EffectName = "TreatDeadlyWoundsCooldown", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = false,
                        ComponentTypes = new string[0],
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "54c9837b07de4afc9e86516b22c460bb/0:ActionList/1:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new[] { "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0|unsupported-action", "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0|unsupported-action", "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0|unsupported-action", "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0|unsupported-action", "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0|unsupported-action", "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0|unsupported-action", "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0|unsupported-action" },
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0", Detail = "unsupported-action", ActionPath = "54c9837b07de4afc9e86516b22c460bb/0:ActionList/0:reflected:CallOfTheWild.SkillMechanics.ContextActionSkillCheckWithFailures, CallOfTheWild, Version=1.0.0.0/0:ActionList/0:CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0", Detail = "unsupported-action", ActionPath = "54c9837b07de4afc9e86516b22c460bb/0:ActionList/0:reflected:CallOfTheWild.SkillMechanics.ContextActionSkillCheckWithFailures, CallOfTheWild, Version=1.0.0.0/1:ActionList/0:CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0", Detail = "unsupported-action", ActionPath = "54c9837b07de4afc9e86516b22c460bb/0:ActionList/0:reflected:CallOfTheWild.SkillMechanics.ContextActionSkillCheckWithFailures, CallOfTheWild, Version=1.0.0.0/5:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0", Detail = "unsupported-action", ActionPath = "54c9837b07de4afc9e86516b22c460bb/0:ActionList/0:reflected:CallOfTheWild.SkillMechanics.ContextActionSkillCheckWithFailures, CallOfTheWild, Version=1.0.0.0/5:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0", Detail = "unsupported-action", ActionPath = "54c9837b07de4afc9e86516b22c460bb/0:ActionList/0:reflected:CallOfTheWild.SkillMechanics.ContextActionSkillCheckWithFailures, CallOfTheWild, Version=1.0.0.0/5:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0", Detail = "unsupported-action", ActionPath = "54c9837b07de4afc9e86516b22c460bb/0:ActionList/0:reflected:CallOfTheWild.SkillMechanics.ContextActionSkillCheckWithFailures, CallOfTheWild, Version=1.0.0.0/5:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0", Detail = "unsupported-action", ActionPath = "54c9837b07de4afc9e86516b22c460bb/0:ActionList/0:reflected:CallOfTheWild.SkillMechanics.ContextActionSkillCheckWithFailures, CallOfTheWild, Version=1.0.0.0/5:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds, CallOfTheWild, Version=1.0.0.0" }
                }
            };
        }

        // Battle Ward (8bc6ba5c82d44deaaef24cec75490d05, ShamanBattleWardHexAbility): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealShamanBattleWard()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "Helpful", EffectOnEnemy = "None", Range = "Touch",
                AbilityComponentTypes = new[] { "CallOfTheWild.NewMechanics.AbilityTargetHasNoFactUnlessBuffsFromCaster", "CallOfTheWild.NewMechanics.ContextCalculateAbilityParamsBasedOnClasses", "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx", "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "926b9e0d2ac44c298f246afd5ca180aa", EffectName = "ShamanBattleWardHexAbilityCooldownBuff", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = false, IsClassFeature = false,
                        ComponentTypes = new string[0],
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "8bc6ba5c82d44deaaef24cec75490d05/0:ActionList/1:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new[] { "CallOfTheWild.NewMechanics.RunActionsDependingOnContextValue, CallOfTheWild, Version=1.0.0.0|unsupported-action" },
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "CallOfTheWild.NewMechanics.RunActionsDependingOnContextValue, CallOfTheWild, Version=1.0.0.0", Detail = "unsupported-action", ActionPath = "8bc6ba5c82d44deaaef24cec75490d05/0:ActionList/0:CallOfTheWild.NewMechanics.RunActionsDependingOnContextValue, CallOfTheWild, Version=1.0.0.0" }
                }
            };
        }

        // Inspiring Recovery (788d72e7713cf90418ee1f38449416dc, InspiringRecovery): exact facts from the rc3 catalogue export
        private static NativeCandidateAuditFacts RealInspiringRecovery()
        {
            return new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true, IsStickyTouch = false, HasVariants = false,
                CanTargetSelf = true, CanTargetFriends = true, CanTargetEnemies = false, CanTargetPoint = false,
                EffectOnAlly = "Helpful", EffectOnEnemy = "Harmful", Range = "Medium",
                AbilityComponentTypes = new[] { "Kingmaker.Blueprints.Classes.Spells.SpellComponent", "Kingmaker.Blueprints.Classes.Spells.SpellDescriptorComponent", "Kingmaker.Blueprints.Classes.Spells.SpellListComponent", "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction", "Kingmaker.UnitLogic.Abilities.Components.AbilityUseOnRest", "Kingmaker.UnitLogic.Abilities.Components.Base.AbilitySpawnFx", "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetBreathOfLife", "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig" },
                Effects = new[]
                {
                    new NativeCandidateEffectFacts
                    {
                        EffectId = "dc9a8ddf45adbc74aaff7b309f232072", EffectName = "InspiringRecoveryCheckBuff", Kind = "Buff", Target = "CurrentTarget",
                        Harmful = false, IsHiddenInUi = true, IsClassFeature = false,
                        ComponentTypes = new[] { "Kingmaker.Designers.Mechanics.Facts.ReplaceAbilityParamsWithContext", "Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions" },
                        GrantedConditions = new string[0],
                        SourceContract = "ContextActionApplyBuff",
                        ActionPath = "788d72e7713cf90418ee1f38449416dc/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/1:ContextActionApplyBuff"
                    }
                },
                DiagnosticContracts = new[] { "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage, Assembly-CSharp, Version=0.0.0.0|offensive-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0|restorative-action", "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionBreathOfLife, Assembly-CSharp, Version=0.0.0.0|unsupported-action" },
                Diagnostics = new NativeCandidateDiagnosticFacts[]
                {
                    new NativeCandidateDiagnosticFacts { Code = "offensive-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage, Assembly-CSharp, Version=0.0.0.0", Detail = "offensive-action", ActionPath = "788d72e7713cf90418ee1f38449416dc/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:reflected:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSavingThrow, Assembly-CSharp, Version=0.0.0.0/0:ActionList/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "restorative-action", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0", Detail = "restorative-action", ActionPath = "788d72e7713cf90418ee1f38449416dc/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget, Assembly-CSharp, Version=0.0.0.0" },
                    new NativeCandidateDiagnosticFacts { Code = "unknown-node", Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionBreathOfLife, Assembly-CSharp, Version=0.0.0.0", Detail = "unsupported-action", ActionPath = "788d72e7713cf90418ee1f38449416dc/0:ActionList/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/false/0:Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional/true/0:Kingmaker.UnitLogic.Mechanics.Actions.ContextActionBreathOfLife, Assembly-CSharp, Version=0.0.0.0" }
                }
            };
        }

    }
}
