using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    // sticky-touch-direct (0.4.0 WP6): the guarded qualification that casts
    // a willing-target touch buff (Magic Circle against Alignment's shape)
    // once on an ally through the sticky-touch route, repeats it as a no-op
    // and is refused for want of the spent cast. The fixture mirrors the
    // advanced campaign structurally: a cleric with one prepared touch
    // spell, a Brown-Fur arcanist whose spontaneous level holds more casts,
    // party allies and a pet, and the distractors the recipe must not take.
    // The graphs are the action-graph adapter's own node shapes, scanned by
    // the production scanner; the instant route's record is the instant
    // adapter's exact detail format, run through the production executors.
    internal static partial class Program
    {
        private static void RunStickyTouchQualificationTests(string root)
        {
            Run("sticky-qual-installed-ally-split-and-save-contract", TestStickyQualInstalledContract);
            Run("sticky-qual-shape-is-the-ally-branch-buff", TestStickyQualShape);
            Run("sticky-qual-selects-the-willing-target-touch-on-an-ally", TestStickyQualSelection);
            Run("sticky-qual-records-why-distractors-were-refused", TestStickyQualRejections);
            Run("sticky-qual-forecast-is-one-executable-touch", TestStickyQualForecast);
            Run("sticky-qual-driver-passes-both-modes-and-pool-kinds", () => TestStickyQualDriver(root));
            Run("sticky-qual-driver-stops-at-a-held-touch-or-a-refilled-slot",
                () => TestStickyQualDriverFailures(root));
            Run("sticky-qual-judge-refuses-wrong-route-spend-effect-and-exhaustion",
                () => TestStickyQualJudge(root));
            Run("sticky-qual-recipe-registered-in-host-and-scripts", TestStickyQualRegistration);
            Run("sticky-qual-route-markers-are-the-instant-adapter-record", TestStickyQualRouteMarkers);
        }

        // One party with touch spells. "unit-cleric" prepared one Magic
        // Circle (and one each of the distractor touches); "unit-arcanist" (a
        // Brown-Fur Transmuter) casts it spontaneously from a level with more
        // casts, and holds a touch the classifier sent to the fallback.
        private sealed class StickyTouchWorld : IInstantCastRuntimeAdapter, ICastRuntimeAdapter,
            ICastEnhancementRuntimeAdapter
        {
            internal const string Cleric = "unit-cleric";
            internal const string Arcanist = "unit-arcanist";
            internal const string Alchemist = "unit-alchemist";
            internal const string Fighter = "unit-fighter";
            internal const string Wolf = "unit-a-wolf";
            internal const string ClericBook = "book-cleric";
            internal const string ArcanistBook = "book-arcanist";
            internal const string ClericPool = "pool-cleric-prepared";
            internal const string ArcanistPool = "pool-arcanist-level-3";
            internal const string CircleBuff = "circle-carrier-buff";
            internal const string CircleDelivery = "circle-touch-delivery";
            internal const string FreedomBuff = "freedom-buff";
            internal static readonly AbilityKey Circle = Spell("circle-carrier");
            internal static readonly AbilityKey Freedom = Spell("aaa-freedom-carrier");
            internal static readonly AbilityKey SelfWard = Spell("aa-self-ward");
            internal static readonly AbilityKey HarmTouch = Spell("ab-harm-touch");
            internal static readonly AbilityKey Hostile = Spell("hostile-touch");
            internal static readonly AbilityKey Plain = Spell("plain-buff");
            internal static readonly string CircleToken = PreparedSlotIds.Format(3, 0, 0);
            internal static readonly string FreedomToken = PreparedSlotIds.Format(4, 0, 0);
            internal static readonly string SelfWardToken = PreparedSlotIds.Format(1, 0, 0);
            internal static readonly string HarmToken = PreparedSlotIds.Format(2, 0, 0);
            internal static readonly string PlainToken = PreparedSlotIds.Format(1, 0, 1);
            internal static readonly string[] Party = { Cleric, Arcanist, Alchemist, Fighter, Wolf };

            // tokenId -> (spell, available)
            internal readonly SortedDictionary<string, KeyValuePair<AbilityKey, bool>> Tokens =
                new SortedDictionary<string, KeyValuePair<AbilityKey, bool>>(StringComparer.Ordinal);
            // "<unit>|<effect>" -> (instance key, end ticks fixed at landing)
            internal readonly Dictionary<string, KeyValuePair<string, long>> Active =
                new Dictionary<string, KeyValuePair<string, long>>(StringComparer.Ordinal);
            internal readonly List<string> Fired = new List<string>();
            internal int InstantFires;
            internal int AnimatedStarts;
            internal int ArcanistCasts = 5;
            // Failure shapes: the delivery's held touch is never released;
            // the spent slot is prepared again after the use step.
            internal bool HeldTouchStays;
            internal bool RefillAfterUse;
            internal long Now;
            private readonly Dictionary<CastStep, EffectBaseline> _baselines =
                new Dictionary<CastStep, EffectBaseline>();
            private int _instances;
            private long _sequence;

            internal StickyTouchWorld()
            {
                Tokens[CircleToken] = new KeyValuePair<AbilityKey, bool>(Circle, true);
                Tokens[FreedomToken] = new KeyValuePair<AbilityKey, bool>(Freedom, true);
                Tokens[SelfWardToken] = new KeyValuePair<AbilityKey, bool>(SelfWard, true);
                Tokens[HarmToken] = new KeyValuePair<AbilityKey, bool>(HarmTouch, true);
                Tokens[PlainToken] = new KeyValuePair<AbilityKey, bool>(Plain, true);
            }

            private static AbilityKey Spell(string guid)
            {
                return new AbilityKey(guid, null, 0, SourceKind.Spellbook, null);
            }

            internal void SetToken(string token, bool available)
            {
                Tokens[token] = new KeyValuePair<AbilityKey, bool>(Tokens[token].Key, available);
            }

            internal bool TokenAvailable(string token) { return Tokens[token].Value; }

            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            { return CastEnhancementPreparation.Pass(null); }

            // The instant adapter's own record of a sticky delivery (see
            // KingmakerInstantCastAdapter.Fire): one rule cast of the derived
            // delivery, Spend() on the source data, no native command.
            public InstantCastResult Fire(CastStep step)
            {
                InstantFires++;
                int before = Available(step);
                Land(step);
                int after = Available(step);
                bool observed = EffectsObserved(step);
                return new InstantCastResult(true, true, observed, before - after == 1, true,
                    "rule-success:True;umd-failed:False;spell-failed:False;spend-invoked:True;spend-failure:none" +
                    ";provider-direct:False;provider-status:none;provider-completion-failure:none" +
                    ";spend-owner:source-ability-data;available-before:" + before + ";available-after:" + after +
                    ";strategy:" + step.ExecutionStrategy + ";strategy-reason:" + step.ExecutionStrategyReason +
                    ";carrier-guid:" + step.Provider.Ability.BaseAbilityGuid + ";delivery-guid:" + CircleDelivery +
                    ";resolution:fixture-exact-source;source-ability-data:fixture#1;execution-ability-data:fixture#2" +
                    ";rule-cast-submitted:true;expected-effects:" + CircleBuff + ";targets:" +
                    string.Join(",", step.ExpectedRecipientUnitIds.ToArray()) +
                    ";carrier-command-created:false;delivery-command-created:false" +
                    ";effects-observed-at-submit:" + observed);
            }

            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                AnimatedStarts++;
                return new LandingAnimatedOperation(3, () => Land(step), () => EffectsObserved(step));
            }

            private string TouchState(CastStep step)
            {
                return "held-touch:" + HeldTouchStays + ";delivery-command-present:False;carrier-guid:" +
                    step.Provider.Ability.BaseAbilityGuid + ";delivery-guid:" + CircleDelivery;
            }

            public InstantCastCompletion InspectCompletion(CastStep step)
            {
                return HeldTouchStays ? InstantCastCompletion.Pending(TouchState(step))
                    : InstantCastCompletion.Settled(TouchState(step));
            }

            public InstantCastCompletion Cleanup(CastStep step)
            {
                return HeldTouchStays
                    ? InstantCastCompletion.Pending("sticky-delivery-cleanup;command-interrupted:False;" +
                        "held-touch-removed:False;residual-delivery-state:True")
                    : InstantCastCompletion.Settled(TouchState(step));
            }

            private List<ObservedEffectInstance> Instances(string unit)
            {
                var instances = new List<ObservedEffectInstance>();
                KeyValuePair<string, long> instance;
                if (Active.TryGetValue(unit + "|" + CircleBuff, out instance))
                    instances.Add(new ObservedEffectInstance(EffectKind.Buff, CircleBuff, instance.Key,
                        instance.Value, false));
                return instances;
            }

            private void Land(CastStep step)
            {
                Fired.Add(step.AssignmentId);
                _baselines[step] = new EffectBaseline(step.ExpectedRecipientUnitIds.ToDictionary(unit => unit,
                    unit => (IEnumerable<ObservedEffectInstance>)Instances(unit), StringComparer.Ordinal));
                Active[step.TargetUnitIds[0] + "|" + CircleBuff] = new KeyValuePair<string, long>(
                    "i" + (++_instances), Now * TimeSpan.TicksPerMillisecond + TimeSpan.FromHours(1).Ticks);
                if (step.Reservation.TokenIds.Count != 0)
                    foreach (string token in step.Reservation.TokenIds) SetToken(token, false);
                else ArcanistCasts -= step.Reservation.Units;
            }

            public bool EffectsObserved(CastStep step)
            {
                EffectBaseline baseline;
                return _baselines.TryGetValue(step, out baseline) &&
                    AppliedEffectJudgement.AllReached(step.ExpectedRecipientUnitIds, step.ExpectedEffects,
                        baseline, Instances);
            }

            // The game's own count of the source's casts: available slots of
            // the spell (prepared), or casts left at the level (spontaneous).
            private int Available(CastStep step)
            {
                return step.Reservation.TokenIds.Count != 0
                    ? Tokens.Count(pair => pair.Value.Value &&
                        pair.Value.Key.Canonical == step.Provider.Ability.Canonical)
                    : ArcanistCasts;
            }

            internal ProbeObservation Observe(CastStep step, string label)
            {
                string unit = step.TargetUnitIds[0];
                var instances = new List<ProbeEffectInstance>();
                KeyValuePair<string, long> instance;
                if (Active.TryGetValue(unit + "|" + CircleBuff, out instance))
                    instances.Add(new ProbeEffectInstance(CircleBuff, instance.Key, instance.Value));
                Dictionary<string, bool> reserved = step.Reservation.TokenIds.Count == 0 ? null
                    : step.Reservation.TokenIds.ToDictionary(id => id, id => Tokens[id].Value, StringComparer.Ordinal);
                return ProbeObservation.Read(label, ++_sequence, DateTime.UtcNow, unit, Available(step), instances,
                    reserved);
            }

            internal ActiveEffectSnapshot Live()
            {
                return LiveEffects(Active.Select(pair => On(pair.Key.Split('|')[0], pair.Key.Split('|')[1],
                    500, 9, 0)).ToArray());
            }

            internal CastingWorkspaceInputs Inputs()
            {
                if (RefillAfterUse && Fired.Count == 1) SetToken(CircleToken, true);
                List<UnitSnapshot> units = Party.Select(id => new UnitSnapshot(id, id, id == Wolf,
                    id == Wolf ? Arcanist : string.Empty, new TargetValidationSnapshot(true, true, true, true)))
                    .ToList();
                Func<string, string, AbilityKey, string, int, ProviderSnapshot> provider =
                    (caster, book, ability, pool, level) => new ProviderSnapshot(
                        new ProviderKey(caster, book, ability, "level-" + level), ability.BaseAbilityGuid, level,
                        pool, 1, book == ClericBook
                            ? Tokens.Where(pair => pair.Value.Key.Canonical == ability.Canonical).Select(pair => pair.Key)
                            : null,
                        null, 9, 900, string.Empty, "10 minutes/level");
                ProviderSnapshot clericCircle = provider(Cleric, ClericBook, Circle, ClericPool, 3);
                ProviderSnapshot arcanistCircle = provider(Arcanist, ArcanistBook, Circle, ArcanistPool, 3);
                ProviderSnapshot freedom = provider(Cleric, ClericBook, Freedom, ClericPool, 4);
                ProviderSnapshot selfWard = provider(Cleric, ClericBook, SelfWard, ClericPool, 1);
                ProviderSnapshot harm = provider(Cleric, ClericBook, HarmTouch, ClericPool, 2);
                ProviderSnapshot hostile = provider(Arcanist, ArcanistBook, Hostile, ArcanistPool, 3);
                ProviderSnapshot plain = provider(Cleric, ClericBook, Plain, ClericPool, 1);
                var pools = new[]
                {
                    new ResourcePoolSnapshot(ClericPool, ResourcePoolKind.PreparedSlots, Tokens.Count,
                        Tokens.Count(pair => pair.Value.Value), Tokens.Select(pair => new ResourceTokenSnapshot(
                            pair.Key, pair.Value.Key, int.Parse(pair.Key.Split('|')[0].Substring("level-".Length)),
                            PreparedSlotKind.Common, pair.Value.Value, true, null))),
                    new ResourcePoolSnapshot(ArcanistPool, ResourcePoolKind.SpontaneousLevel, 5, ArcanistCasts, null)
                };
                List<string> everyone = Party.ToList();
                var options = new List<ProviderPlanningOption>
                {
                    Option(clericCircle, everyone, CastExecutionStrategy.StickyTouchDeliveryRuleCast,
                        StickyTouchExecutionClassifier.WillingTargetReason),
                    Option(arcanistCircle, everyone, CastExecutionStrategy.StickyTouchDeliveryRuleCast,
                        StickyTouchExecutionClassifier.WillingTargetReason),
                    Option(freedom, everyone, CastExecutionStrategy.StickyTouchDeliveryRuleCast,
                        StickyTouchExecutionClassifier.BeneficialReason),
                    Option(selfWard, new List<string> { Cleric }, CastExecutionStrategy.StickyTouchDeliveryRuleCast,
                        StickyTouchExecutionClassifier.BeneficialReason),
                    Option(harm, everyone, CastExecutionStrategy.StickyTouchDeliveryRuleCast,
                        StickyTouchExecutionClassifier.WillingTargetReason),
                    Option(hostile, everyone, CastExecutionStrategy.AnimatedFallback,
                        "sticky-delivery-hostile-targeting-ambiguous"),
                    Option(plain, everyone, CastExecutionStrategy.DirectRuleCast, "ordinary-direct-rule-cast")
                };
                var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
                {
                    { Circle.Canonical, MagicCircleShape(Circle.BaseAbilityGuid, CircleDelivery, CircleBuff) },
                    { Freedom.Canonical, TouchShape(Freedom.BaseAbilityGuid, "freedom-delivery", FreedomBuff) },
                    { SelfWard.Canonical, TouchShape(SelfWard.BaseAbilityGuid, "self-ward-delivery", "self-ward-buff") },
                    { HarmTouch.Canonical, HarmTouchShape(HarmTouch.BaseAbilityGuid, "harm-delivery", "harm-buff") },
                    { Hostile.Canonical, TouchShape(Hostile.BaseAbilityGuid, "hostile-delivery", "hostile-buff") },
                    { Plain.Canonical, new EffectLeafExpression(EffectKind.Buff, "plain-buff-effect",
                        EffectTarget.CurrentTarget, "fixture", "fixture/plain") }
                };
                return new CastingWorkspaceInputs(new PartyProviderSnapshot(units,
                        new[] { clericCircle, arcanistCircle, freedom, selfWard, harm, hostile, plain }, pools),
                    options, effects, new CastEnhancementSnapshot[0], null, Live());
            }

            private static ProviderPlanningOption Option(ProviderSnapshot provider, List<string> reachable,
                CastExecutionStrategy strategy, string reason)
            {
                return new ProviderPlanningOption(provider, reachable, new[] { provider.Key.CasterUnitId }, 9, 900,
                    strategy, reason, new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));
            }
        }

        // The action-graph adapter's nodes for a carrier holding a touch
        // (KingmakerActionGraphAdapter: the carrier, the AbilityEffectStickyTouch
        // reference to its delivery, the delivery's own ability node and its
        // AbilityEffectRunAction list), scanned by the production scanner.
        private static EffectExpression StickyCarrierExpression(string carrier, string delivery,
            DiscoveryNode deliveryActions)
        {
            var deliveryAbility = new DiscoveryNode(DiscoveryNodeKind.AbilityReference, delivery,
                new[] { deliveryActions }, referencedAbilityId: delivery, sourceContract: "BlueprintAbility");
            var touch = new DiscoveryNode(DiscoveryNodeKind.AbilityReference, delivery, new[] { deliveryAbility },
                referencedAbilityId: delivery, sourceContract: "AbilityEffectStickyTouch");
            var root = new DiscoveryNode(DiscoveryNodeKind.AbilityReference, carrier, new[] { touch },
                referencedAbilityId: carrier, sourceContract: "BlueprintAbility");
            return new ActionGraphScanner().Scan(root).Expression;
        }

        private static DiscoveryNode StickyActionList(params DiscoveryNode[] actions)
        {
            return new DiscoveryNode(DiscoveryNodeKind.Sequence, "ActionList", actions, sourceContract: "ActionList");
        }

        private static DiscoveryNode StickyApplyBuff(string buff)
        {
            return new DiscoveryNode(DiscoveryNodeKind.Effect, "ContextActionApplyBuff", effectKind: EffectKind.Buff,
                effectId: buff, target: EffectTarget.CurrentTarget, sourceContract: "ContextActionApplyBuff");
        }

        // An exact reflected ActionList wrapper as the adapter adapts it: its
        // ActionList members in name order.
        private static DiscoveryNode StickyReflected(string type, params DiscoveryNode[] lists)
        {
            return new DiscoveryNode(DiscoveryNodeKind.Sequence,
                "reflected:Kingmaker.UnitLogic.Mechanics.Actions." + type + ", Assembly-CSharp, Version=0.0.0.0",
                lists, sourceContract: "reflected-exact-ActionList-wrapper");
        }

        private static DiscoveryNode StickyAllySplit(DiscoveryNode ally, DiscoveryNode other,
            string condition = CastingQualificationRecipe.AllyConditionType)
        {
            return new DiscoveryNode(DiscoveryNodeKind.Conditional,
                "Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional",
                whenTrue: ally, whenFalse: other, conditionContract: "And:" + condition, sourceContract: "Conditional");
        }

        // Magic Circle against Alignment's delivery: ContextConditionIsAlly
        // {ally: apply the carrier; otherwise: a Will save (ContextActionSavingThrow
        // .Actions) whose ContextActionConditionalSaved applies the carrier
        // when Failed and nothing when it Succeeded}.
        private static EffectExpression MagicCircleShape(string carrier, string delivery, string buff,
            string condition = CastingQualificationRecipe.AllyConditionType)
        {
            DiscoveryNode save = StickyReflected("ContextActionSavingThrow", StickyActionList(
                StickyReflected("ContextActionConditionalSaved", StickyActionList(StickyApplyBuff(buff)),
                    StickyActionList())));
            return StickyCarrierExpression(carrier, delivery, StickyActionList(
                StickyAllySplit(StickyActionList(StickyApplyBuff(buff)), StickyActionList(save), condition)));
        }

        // A touch that only buffs (Freedom of Movement's shape).
        private static EffectExpression TouchShape(string carrier, string delivery, string buff)
        {
            return StickyCarrierExpression(carrier, delivery, StickyActionList(StickyApplyBuff(buff)));
        }

        // A touch whose ally branch buffs and whose other branch harms.
        private static EffectExpression HarmTouchShape(string carrier, string delivery, string buff)
        {
            return StickyCarrierExpression(carrier, delivery, StickyActionList(StickyAllySplit(
                StickyActionList(StickyApplyBuff(buff)),
                StickyActionList(new DiscoveryNode(DiscoveryNodeKind.OffensiveAction,
                    "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage, Assembly-CSharp, Version=0.0.0.0",
                    sourceContract: "offensive-action")))));
        }

        // The installed Kingmaker 2.1.7b contract the shape relies on: the
        // ally condition's exact full name (the adapter's condition contract
        // names it so), and the save's and the saved-outcome's ActionList
        // members, which the adapter reflects (in name order) into the
        // sequences the fixture graph uses.
        private static void TestStickyQualInstalledContract()
        {
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            if (string.IsNullOrWhiteSpace(game)) throw new InvalidOperationException("KBP_TEST_GAME_PATH is missing.");
            Assembly assembly = Assembly.LoadFrom(Path.Combine(game, "Kingmaker_Data", "Managed", "Assembly-CSharp.dll"));
            if (assembly.ManifestModule.ModuleVersionId.ToString("D") != "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")
                throw new InvalidOperationException("Installed Assembly-CSharp is not the inspected 2.1.7b contract.");
            Type actionList = RequireType(assembly, "Kingmaker.ElementsSystem.ActionList");
            Type ally = RequireType(assembly, CastingQualificationRecipe.AllyConditionType);
            Type condition = RequireType(assembly, "Kingmaker.ElementsSystem.Condition");
            if (ally.FullName != CastingQualificationRecipe.AllyConditionType || !condition.IsAssignableFrom(ally))
                throw new InvalidOperationException("ContextConditionIsAlly is not the inspected condition.");
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Func<string, string> lists = name =>
            {
                Type type = RequireType(assembly, "Kingmaker.UnitLogic.Mechanics.Actions." + name);
                return string.Join(",", type.GetFields(all).Where(field => field.FieldType == actionList)
                    .Select(field => field.Name)
                    .Concat(type.GetProperties(all).Where(property => property.PropertyType == actionList &&
                        property.CanRead && property.GetIndexParameters().Length == 0).Select(property => property.Name))
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            };
            string save = lists("ContextActionSavingThrow");
            string saved = lists("ContextActionConditionalSaved");
            if (save != "Actions" || saved != "Failed,Succeed")
                throw new InvalidOperationException("The installed save action lists changed: " + save + " / " + saved);
            Type operation = RequireType(assembly, "Kingmaker.ElementsSystem.Operation");
            if (!operation.IsEnum || !Enum.GetNames(operation).Contains("And"))
                throw new InvalidOperationException("The installed condition operation has no And.");
        }

        private static void TestStickyQualShape()
        {
            EffectExpression circle = MagicCircleShape("circle", "delivery", "carrier");
            Func<EffectExpression, string> shape = CastingQualificationRecipe.StickyTouchShapeRefusal;
            if (shape(circle) != null || shape(TouchShape("freedom", "delivery", "buff")) != null)
                throw new InvalidOperationException("The touch shapes were refused: " + shape(circle));
            // The ally-branch rule for other callers is unchanged: the touch's
            // delivery is another ability and the save's empty Succeeded list
            // is not nothing, so Magic Circle is still not a plain buff.
            if (ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(circle, "circle"))
                throw new InvalidOperationException("The plain-buff rule was loosened.");
            // The planner confirms the carrier on the touched unit: complete
            // when present, from a new instance only.
            var marker = new HashSet<ActiveEffectMarker> { new ActiveEffectMarker(EffectKind.Buff, "carrier") };
            if (new EffectPresenceEvaluator().EvaluateTyped(circle, marker, null).Kind != EffectPresenceKind.Complete ||
                !AppliedEffectJudgement.Reached(circle, new ObservedEffectInstance[0],
                    new[] { new ObservedEffectInstance(EffectKind.Buff, "carrier", "i1", 100, false) }))
                throw new InvalidOperationException("The carrier does not confirm the touch.");
            var refusals = new Dictionary<string, EffectExpression>
            {
                { "ally-branches-differ", StickyCarrierExpression("c", "d", StickyActionList(StickyAllySplit(
                    StickyActionList(StickyApplyBuff("a")), StickyActionList(StickyApplyBuff("b"))))) },
                { "condition-not-ally-split", MagicCircleShape("c", "d", "carrier",
                    "Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionIsCaster") },
                { "unmodeled-action", HarmTouchShape("c", "d", "a") },
                { "more-than-one-condition", StickyCarrierExpression("c", "d", StickyActionList(
                    StickyAllySplit(StickyActionList(StickyApplyBuff("a")), StickyActionList(StickyApplyBuff("a"))),
                    StickyAllySplit(StickyActionList(StickyApplyBuff("b")), StickyActionList(StickyApplyBuff("b"))))) },
                { "leaf:Buff:Caster", StickyCarrierExpression("c", "d", StickyActionList(new DiscoveryNode(
                    DiscoveryNodeKind.Effect, "ContextActionApplyBuff", effectKind: EffectKind.Buff, effectId: "a",
                    target: EffectTarget.Caster, sourceContract: "ContextActionApplyBuff"))) },
                { "targeted:Party", StickyCarrierExpression("c", "d", StickyActionList(new DiscoveryNode(
                    DiscoveryNodeKind.TargetTransform, "ContextActionPartyMembers",
                    new[] { StickyActionList(StickyApplyBuff("a")) }, target: EffectTarget.Party,
                    sourceContract: "ContextActionPartyMembers"))) },
                { "no-buff", StickyCarrierExpression("c", "d", StickyActionList()) }
            };
            foreach (KeyValuePair<string, EffectExpression> refusal in refusals)
                if (shape(refusal.Value) != refusal.Key)
                    throw new InvalidOperationException("The touch shape " + refusal.Key + " was judged " +
                        (shape(refusal.Value) ?? "valid"));
        }

        private static void TestStickyQualSelection()
        {
            var world = new StickyTouchWorld();
            CastingWorkspaceInputs inputs = world.Inputs();
            CastingQualificationSelection selection = CastingQualificationRecipe.Select(
                CastingQualificationRecipe.StickyTouchDirect, inputs, "fixture-campaign");
            // The willing-target touch by the caster holding exactly one cast,
            // on a party member: not the caster, not the pet sorted first,
            // although the beneficial touch's source sorts first.
            if (!selection.Selected || selection.Recipe != CastingQualificationRecipe.StickyTouchDirect ||
                selection.Castings.Count != 1 ||
                selection.Castings[0].CastingId != "qual-cast-1" ||
                selection.Castings[0].Ability.Canonical != StickyTouchWorld.Circle.Canonical ||
                selection.Castings[0].CasterUnitId != StickyTouchWorld.Cleric ||
                selection.Castings[0].SpellbookGuid != StickyTouchWorld.ClericBook ||
                selection.Castings[0].DirectTargetUnitId != StickyTouchWorld.Alchemist ||
                selection.Castings[0].TargetMode != CastingTargetMode.DirectTarget ||
                selection.Castings[0].ExistingEffectPolicy != ExistingEffectPolicy.SkipAlreadyActive ||
                !selection.Coverage.SequenceEqual(new[] { "sticky-touch", "willing-target", "prepared", "single-cast", "ally" }))
                throw new InvalidOperationException("The touch selection was wrong: " + selection.Refusal + " " +
                    string.Join(",", selection.Castings.Select(value => value.CasterUnitId + ">" +
                        value.DirectTargetUnitId).ToArray()) + " " + string.Join(",", selection.Coverage.ToArray()) +
                    " " + string.Join(",", selection.Rejections.ToArray()));
            // Considered before it, in order: the arcanist's spontaneous Magic
            // Circle (more than one cast) and the cleric's harming touch.
            string arcanistCircle = "unit-arcanist|book-arcanist|" + StickyTouchWorld.Circle.Canonical + "|level-3";
            string harm = "unit-cleric|book-cleric|" + StickyTouchWorld.HarmTouch.Canonical + "|level-2";
            if (selection.CandidatesConsidered != 3 || selection.Rejections.Count != 2 ||
                selection.Rejections[0] != arcanistCircle + "|pool-not-single-cast:2" ||
                !selection.Rejections[1].StartsWith(harm + "|effect-shape:unmodeled-action:", StringComparison.Ordinal))
                throw new InvalidOperationException("The touch selection's considered candidates are wrong: " +
                    selection.CandidatesConsidered + " " + string.Join(" / ", selection.Rejections.ToArray()));
            // Deterministic: the same inputs select the same casting.
            CastingQualificationSelection again = CastingQualificationRecipe.SelectStickyTouch(world.Inputs(),
                "fixture-campaign");
            if (again.Castings[0].DirectTargetUnitId != selection.Castings[0].DirectTargetUnitId ||
                !again.Rejections.SequenceEqual(selection.Rejections))
                throw new InvalidOperationException("The touch selection is not deterministic.");
            // An ally that already holds the carrier is passed over.
            var covered = new StickyTouchWorld();
            covered.Active[StickyTouchWorld.Alchemist + "|" + StickyTouchWorld.CircleBuff] =
                new KeyValuePair<string, long>("old", 100);
            CastingQualificationSelection fresh = CastingQualificationRecipe.SelectStickyTouch(covered.Inputs(),
                "fixture-campaign");
            if (!fresh.Selected || fresh.Castings[0].DirectTargetUnitId != StickyTouchWorld.Arcanist)
                throw new InvalidOperationException("A covered ally was chosen: " +
                    (fresh.Selected ? fresh.Castings[0].DirectTargetUnitId : fresh.Refusal));
            // Without the prepared copy, the arcanist's last spontaneous cast.
            var spontaneous = new StickyTouchWorld { ArcanistCasts = 1 };
            spontaneous.SetToken(StickyTouchWorld.CircleToken, false);
            CastingQualificationSelection last = CastingQualificationRecipe.SelectStickyTouch(spontaneous.Inputs(),
                "fixture-campaign");
            if (!last.Selected || last.Castings[0].CasterUnitId != StickyTouchWorld.Arcanist ||
                last.Castings[0].DirectTargetUnitId != StickyTouchWorld.Alchemist ||
                !last.Coverage.SequenceEqual(new[] { "sticky-touch", "willing-target", "spontaneous", "single-cast", "ally" }))
                throw new InvalidOperationException("The spontaneous touch was not selected: " + last.Refusal + " " +
                    string.Join(",", last.Rejections.ToArray()));
        }

        // Without a willing-target touch that qualifies, every distractor is
        // refused for its own reason and the beneficial touch is the one taken;
        // a plain (non-touch) buff is never a candidate.
        private static void TestStickyQualRejections()
        {
            var world = new StickyTouchWorld();
            world.SetToken(StickyTouchWorld.CircleToken, false);
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectStickyTouch(world.Inputs(),
                "fixture-campaign");
            Func<AbilityKey, string, string, string> key = (ability, caster, level) =>
                caster + "|" + (caster == StickyTouchWorld.Cleric ? "book-cleric" : "book-arcanist") + "|" +
                ability.Canonical + "|" + level;
            string[] expected =
            {
                key(StickyTouchWorld.Circle, StickyTouchWorld.Arcanist, "level-3") + "|pool-not-single-cast:2",
                key(StickyTouchWorld.HarmTouch, StickyTouchWorld.Cleric, "level-2") + "|effect-shape:unmodeled-action:",
                key(StickyTouchWorld.Circle, StickyTouchWorld.Cleric, "level-3") + "|pool-not-single-cast:0",
                key(StickyTouchWorld.Hostile, StickyTouchWorld.Arcanist, "level-3") +
                    "|strategy:AnimatedFallback:sticky-delivery-hostile-targeting-ambiguous",
                key(StickyTouchWorld.SelfWard, StickyTouchWorld.Cleric, "level-1") + "|self-only"
            };
            if (!selection.Selected || selection.Castings[0].Ability.Canonical != StickyTouchWorld.Freedom.Canonical ||
                selection.Castings[0].DirectTargetUnitId != StickyTouchWorld.Alchemist ||
                !selection.Coverage.SequenceEqual(new[] { "sticky-touch", "beneficial", "prepared", "single-cast", "ally" }) ||
                selection.Rejections.Count != expected.Length ||
                expected.Where((value, index) => !selection.Rejections[index].StartsWith(value, StringComparison.Ordinal)).Any() ||
                selection.Rejections.Any(value => value.Contains(StickyTouchWorld.Plain.Canonical)) ||
                selection.CandidatesConsidered != 6)
                throw new InvalidOperationException("The touch distractors were not refused exactly: " +
                    selection.Refusal + " " + selection.CandidatesConsidered + " " +
                    string.Join(" / ", selection.Rejections.ToArray()));
            // Nothing left: every ally already holds the beneficial touch's buff.
            foreach (string unit in StickyTouchWorld.Party)
                world.Active[unit + "|" + StickyTouchWorld.FreedomBuff] = new KeyValuePair<string, long>("f-" + unit, 100);
            world.Active.Remove(StickyTouchWorld.Cleric + "|" + StickyTouchWorld.FreedomBuff);
            CastingWorkspaceInputs inputs = world.Inputs();
            var covered = new CastingWorkspaceInputs(inputs.Snapshot, inputs.ProviderOptions, inputs.EffectsBySource,
                inputs.Enhancements, inputs.TargetingModifiers, LiveEffects(StickyTouchWorld.Party
                    .Where(unit => unit != StickyTouchWorld.Cleric)
                    .Select(unit => On(unit, StickyTouchWorld.FreedomBuff, 500, 9, 0)).ToArray()));
            CastingQualificationSelection none = CastingQualificationRecipe.SelectStickyTouch(covered, "fixture-campaign");
            if (none.Selected || none.Refusal != "no-eligible-qualification-recipe" ||
                none.Recipe != CastingQualificationRecipe.StickyTouchDirect ||
                none.Rejections.Last() != key(StickyTouchWorld.Freedom, StickyTouchWorld.Cleric, "level-4") + "|no-fresh-ally")
                throw new InvalidOperationException("A touch without a fresh ally was selected: " +
                    string.Join(" / ", none.Rejections.ToArray()));
        }

        private static void TestStickyQualForecast()
        {
            var world = new StickyTouchWorld();
            CastingWorkspaceInputs inputs = world.Inputs();
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectStickyTouch(inputs,
                "fixture-campaign");
            IReadOnlyList<CastingQualificationStepForecast> forecast =
                CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign");
            if (CastingQualificationRecipe.ForecastSteps(CastingQualificationRecipe.StickyTouchDirect) != 1 ||
                forecast.Count != 1 || forecast[0].Name != CastingQualificationForecast.Use ||
                forecast[0].Refusal != null || forecast[0].ProjectionId == null ||
                !forecast[0].CastingIds.SequenceEqual(new[] { "qual-cast-1" }))
                throw new InvalidOperationException("The touch forecast is not one use step: " +
                    string.Join(" | ", forecast.Select(step => step.Name + ":" + (step.Refusal ??
                        string.Join(",", step.CastingIds.ToArray()))).ToArray()));
            CastStep use = forecast[0].Projection.Plan.Steps.Single();
            if (use.ExecutionStrategy != CastExecutionStrategy.StickyTouchDeliveryRuleCast ||
                use.ExecutionStrategyReason != StickyTouchExecutionClassifier.WillingTargetReason ||
                !use.TargetUnitIds.SequenceEqual(new[] { StickyTouchWorld.Alchemist }) ||
                use.Reservation.PoolKey != StickyTouchWorld.ClericPool ||
                !use.Reservation.TokenIds.SequenceEqual(new[] { StickyTouchWorld.CircleToken }) ||
                !CastingQualificationForecast.Leaves(use.ExpectedEffects).All(leaf => leaf.EffectId == StickyTouchWorld.CircleBuff))
                throw new InvalidOperationException("The touch use step is not the instant touch on the ally: " +
                    use.ExecutionStrategy + " " + string.Join(",", use.TargetUnitIds.ToArray()));
            // The same state forecasts the same projection (the allowance binds it).
            if (CastingQualificationForecast.Forecast(CastingQualificationRecipe.SelectStickyTouch(world.Inputs(),
                    "fixture-campaign"), world.Inputs(), "fixture-campaign")[0].ProjectionId != forecast[0].ProjectionId)
                throw new InvalidOperationException("The touch projection is not deterministic.");
            // Strict Instant keeps the touch Ready: it is an instant route.
            ExplicitCastingPlan strict = new ExplicitCastingCompiler().Compile(
                CastingQualificationForecast.BuildDocument("fixture-campaign", selection.Castings), inputs.Snapshot,
                inputs.ProviderOptions, inputs.EffectsBySource, inputs.Enhancements,
                CastingQualificationRecipe.RoutineId, inputs.TargetingModifiers, false, inputs.LiveEffects, true);
            if (!strict.Castings.Single().IsExecutable)
                throw new InvalidOperationException("The touch is Not Ready in strict Instant: " +
                    string.Join(",", strict.Castings.Single().ReadinessReasons.ToArray()));
        }

        private static CastingQualificationRecord RunStickyQualification(string dir, StickyTouchWorld world, string mode)
        {
            Directory.CreateDirectory(dir);
            long now = 0;
            Func<long> clock = () => now;
            CastingWorkspaceInputs inputs = world.Inputs();
            CastingQualificationSelection selection = CastingQualificationRecipe.Select(
                CastingQualificationRecipe.StickyTouchDirect, inputs, "fixture-campaign");
            IReadOnlyList<CastingQualificationStepForecast> forecast =
                CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign");
            string refusal;
            CastingQualificationAllowance allowance = CastingQualificationAllowance.Parse(
                QualificationAllowanceJson(o =>
                {
                    o["fixtureGameId"] = "fixture-campaign";
                    o["executionMode"] = mode;
                    o["recipe"] = CastingQualificationRecipe.StickyTouchDirect;
                    o["compatibilityProfileId"] = "advanced-gunslinger-0136";
                    o["approvedProjectionIds"] = new JArray(forecast.Select(step => (object)step.ProjectionId).ToArray());
                    o["maximumNativeSubmissions"] = 1;
                }), "qual-run-1", out refusal);
            if (allowance == null) throw new InvalidOperationException("The touch allowance was refused: " + refusal);
            // The production executors per mode (BuffPlannerUiRoot): Animated -
            // the native command; Instant - the strict hybrid.
            var host = new CastingExecutionHost(settings => settings != null && settings.Mode == "animated"
                ? (ICastExecutor)new AnimatedCastExecutor(world, true)
                : new HybridCastExecutor(world, world, false, true, null, null, strictInstant: true), clock);
            var record = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            var driver = new CastingQualificationDriver(record, allowance, "fixture-campaign", world.Inputs,
                boundary => new CastingWorkspaceSession(dir, "fixture-campaign", boundary),
                host, world.Observe, clock, 240000, CastingQualificationRecipe.StickyTouchDirect, null, false, null,
                null, () => "fixture-lifecycle=1", () => 0L);
            for (int i = 0; i < 4000 && !driver.Completed; i++) { now += 16; world.Now = now; driver.Update(); }
            return record;
        }

        // The whole recipe through the production driver, session, compiler,
        // gate, converter, boundary, host and executors, in both modes, from a
        // prepared slot and from a spontaneous level: one Apply, one cast on
        // the ally through the mode's route, one cast spent; a repeat casts
        // nothing; Always recast is refused naming the spent source exactly.
        private static void TestStickyQualDriver(string root)
        {
            foreach (string kind in new[] { "prepared", "spontaneous" })
                foreach (string mode in new[] { "instant", "animated" })
                {
                    var world = new StickyTouchWorld();
                    if (kind == "spontaneous")
                    {
                        world.ArcanistCasts = 1;
                        world.SetToken(StickyTouchWorld.CircleToken, false);
                    }
                    CastingQualificationRecord record = RunStickyQualification(
                        Path.Combine(root, "sq-" + kind[0] + mode[0]), world, mode);
                    IList<string> violations = record.Violations();
                    CastingQualificationStepResult use = record.Step("use");
                    CastingQualificationStepResult repeat = record.Step("repeat");
                    CastingQualificationStepResult exhausted = record.Step("exhausted");
                    string exhaustedReason = kind == "prepared"
                        ? "apply-refused:blocked-casting:qual-cast-1:prepared-slots-exhausted:" + StickyTouchWorld.ClericPool
                        : "apply-refused:blocked-casting:qual-cast-1:resource-pool-exhausted:" +
                            StickyTouchWorld.ArcanistPool + ":0<1";
                    string detail = use == null || use.Report == null ? string.Empty
                        : use.Report.Entries.Single().Detail;
                    bool routed = mode == "instant"
                        ? world.InstantFires == 1 && world.AnimatedStarts == 0 &&
                            detail.Contains(";strategy:StickyTouchDeliveryRuleCast;") &&
                            detail.Contains(";delivery-guid:" + StickyTouchWorld.CircleDelivery + ";") &&
                            detail.Contains(";cleanup-state:held-touch:False;")
                        : world.InstantFires == 0 && world.AnimatedStarts == 1 &&
                            detail.Contains("native-command-spend-completed");
                    bool spent = kind == "prepared"
                        ? !world.TokenAvailable(StickyTouchWorld.CircleToken) && world.ArcanistCasts == 5 &&
                            world.TokenAvailable(StickyTouchWorld.FreedomToken) &&
                            use != null && use.TokenReadings.Count == 1 &&
                            use.TokenReadings[0].TokenId == StickyTouchWorld.CircleToken &&
                            use.TokenReadings[0].Before == true && use.TokenReadings[0].After == false &&
                            exhausted != null && exhausted.TokenReadings.Count == 1 &&
                            exhausted.TokenReadings[0].Before == false && exhausted.TokenReadings[0].After == false
                        : world.ArcanistCasts == 0 && use != null && use.TokenReadings.Count == 0;
                    if (violations.Count != 0 || record.TerminalReason != "completed" || record.ExecutionMode != mode ||
                        !world.Fired.SequenceEqual(new[] { "qual-cast-1" }) || !routed || !spent ||
                        use == null || !use.Availability.SequenceEqual(new[] { "qual-cast-1:1>0" }) ||
                        use.TransitionOf("qual-cast-1") != "new-instance" ||
                        !world.Active.ContainsKey(StickyTouchWorld.Alchemist + "|" + StickyTouchWorld.CircleBuff) ||
                        repeat == null || repeat.ApplyReason != "nothing-to-cast:1" ||
                        exhausted == null || exhausted.ApplyAllowed || exhausted.ApplyReason != exhaustedReason ||
                        !exhausted.Availability.SequenceEqual(new[] { "qual-cast-1:0>0" }) ||
                        exhausted.TransitionOf("qual-cast-1") != "unchanged" || record.ExhaustedAccepted == null ||
                        record.Submissions.Count(value => value.StartsWith("submitted:", StringComparison.Ordinal)) != 1)
                        throw new InvalidOperationException("The touch qualification (" + kind + ", " + mode +
                            ") did not pass exactly: " + record.TerminalReason + "|" +
                            string.Join("|", violations.ToArray()) + "|fired=" + string.Join(",", world.Fired.ToArray()) +
                            "|routed=" + routed + "|spent=" + spent + "|" + (exhausted == null ? "no-exhausted"
                                : exhausted.ApplyReason + "|" + string.Join(",", exhausted.Availability.ToArray())) +
                            "|detail=" + detail);
                }
        }

        // Where the run must stop: a delivery whose held touch is never
        // released halts the use step (nothing else is cast); a slot prepared
        // again after the use makes the exhausted step not a refusal.
        private static void TestStickyQualDriverFailures(string root)
        {
            var held = new StickyTouchWorld { HeldTouchStays = true };
            CastingQualificationRecord heldRecord = RunStickyQualification(Path.Combine(root, "sq-held"), held, "instant");
            if (heldRecord.TerminalReason == "completed" || !held.Fired.SequenceEqual(new[] { "qual-cast-1" }) ||
                !heldRecord.Failures.Any(value => value.StartsWith("use-wait:step:", StringComparison.Ordinal)) ||
                heldRecord.Step("repeat") != null)
                throw new InvalidOperationException("A held touch did not stop the use step: " +
                    heldRecord.TerminalReason + "|" + string.Join("|", heldRecord.Failures.ToArray()));
            // The compiler finds the slot again, so only the allowance's
            // single approved projection stops the second cast at the
            // boundary: not the refusal for want of the cast.
            var refill = new StickyTouchWorld { RefillAfterUse = true };
            CastingQualificationRecord refillRecord = RunStickyQualification(Path.Combine(root, "sq-refill"), refill,
                "instant");
            if (refillRecord.TerminalReason == "completed" || !refill.Fired.SequenceEqual(new[] { "qual-cast-1" }) ||
                !refillRecord.Failures.Contains(
                    "exhausted:step:refused-for-another-reason:qualification-allowance-exhausted"))
                throw new InvalidOperationException("A refilled slot passed the exhausted step: " +
                    refillRecord.TerminalReason + "|" + string.Join("|", refillRecord.Failures.ToArray()));
        }

        // A copy of a clean record with one step changed.
        private static CastingQualificationRecord StickyRecordWith(CastingQualificationRecord source, string stepName,
            Action<CastingQualificationStepResult> change)
        {
            var copy = new CastingQualificationRecord
            {
                CastingScenario = source.CastingScenario, AllowanceStatus = source.AllowanceStatus,
                TerminalReason = source.TerminalReason, Selection = source.Selection, Roster = source.Roster,
                Forecast = source.Forecast, Submissions = source.Submissions,
                PlannedSubmissions = source.PlannedSubmissions, MaximumSubmissions = source.MaximumSubmissions,
                ExecutionMode = source.ExecutionMode, ExhaustedAccepted = source.ExhaustedAccepted
            };
            foreach (CastingQualificationStepResult step in source.Steps)
            {
                var clone = new CastingQualificationStepResult(step.Name)
                {
                    ApplyAllowed = step.ApplyAllowed, ApplyReason = step.ApplyReason,
                    ProjectionId = step.ProjectionId, Report = step.Report
                };
                clone.Transitions.AddRange(step.Transitions);
                clone.Availability.AddRange(step.Availability);
                clone.TokenReadings.AddRange(step.TokenReadings);
                clone.UnreadTokenCastings.AddRange(step.UnreadTokenCastings);
                if (step.Name == stepName) change(clone);
                copy.Steps.Add(clone);
            }
            return copy;
        }

        private static CastingRunReport StickyReportWith(CastingRunReport report,
            Func<CastingOutcomeEntry, CastingOutcomeEntry> change)
        {
            return new CastingRunReport(report.RunId, report.ScopeRoutineId, report.Mode, report.ProjectionId,
                report.TerminalReason, report.Cancelled, report.Halted, report.Entries.Select(change).ToList(),
                report.CleanupFailures.ToList());
        }

        private static CastingOutcomeEntry StickyEntry(CastingOutcomeEntry entry, string detail = null,
            CastingOutcomeState? state = null, bool? spendReported = null)
        {
            return new CastingOutcomeEntry(entry.CastingId, state ?? entry.State, entry.Planned, entry.Submitted,
                spendReported ?? entry.SpendReported, detail ?? entry.Detail, entry.FreeCast);
        }

        // The step rules on recorded outcomes: the clean runs pass; a wrong
        // route, a double or unreported spend, a missing effect and a wrongly
        // accepted exhausted step each fail with their own reason.
        private static void TestStickyQualJudge(string root)
        {
            CastingQualificationRecord instant = RunStickyQualification(Path.Combine(root, "sqj-i"),
                new StickyTouchWorld(), "instant");
            CastingQualificationRecord animated = RunStickyQualification(Path.Combine(root, "sqj-a"),
                new StickyTouchWorld(), "animated");
            if (instant.Violations().Count != 0 || animated.Violations().Count != 0 ||
                !instant.JudgedSteps.SequenceEqual(new[] { "use", "repeat", "exhausted" }))
                throw new InvalidOperationException("The judged runs are not clean: " +
                    string.Join("|", instant.Violations().ToArray()) + " / " +
                    string.Join("|", animated.Violations().ToArray()));
            string instantDetail = instant.Step("use").Report.Entries.Single().Detail;
            string animatedDetail = animated.Step("use").Report.Entries.Single().Detail;
            Func<CastingQualificationRecord, Func<CastingOutcomeEntry, CastingOutcomeEntry>, string> useWith =
                (source, change) => StickyRecordWith(source, "use", step =>
                    step.Report = StickyReportWith(step.Report, change)).StepFailure("use");
            Func<string, string, string> replaced = (from, to) =>
            {
                if (!instantDetail.Contains(from)) throw new InvalidOperationException("Fixture detail lacks " + from);
                return instantDetail.Replace(from, to);
            };
            var routeCases = new Dictionary<string, KeyValuePair<CastingQualificationRecord, string>>
            {
                // Instant mode, but the game's own command ran the touch.
                { "route:instant-touch:rule-success:True:", new KeyValuePair<CastingQualificationRecord, string>(
                    instant, animatedDetail) },
                // Animated mode, but the instant engine ran it.
                { "route:native-command:", new KeyValuePair<CastingQualificationRecord, string>(
                    animated, instantDetail) },
                { "route:instant-touch:provider-direct:False:", new KeyValuePair<CastingQualificationRecord, string>(
                    instant, replaced(";provider-direct:False;", ";provider-direct:True;")) },
                { "route:instant-touch:strategy:StickyTouchDeliveryRuleCast:",
                    new KeyValuePair<CastingQualificationRecord, string>(instant,
                        replaced(";strategy:StickyTouchDeliveryRuleCast;", ";strategy:DirectRuleCast;")) },
                { "route:instant-touch:strategy-reason:", new KeyValuePair<CastingQualificationRecord, string>(
                    instant, replaced(StickyTouchExecutionClassifier.WillingTargetReason,
                        StickyTouchExecutionClassifier.BeneficialReason)) },
                { "route:instant-touch:carrier-command-created:false;delivery-command-created:false:",
                    new KeyValuePair<CastingQualificationRecord, string>(instant,
                        replaced(";delivery-command-created:false;", ";delivery-command-created:true;")) },
                { "route:instant-touch:cleanup-complete:True;cleanup-state:held-touch:False;",
                    new KeyValuePair<CastingQualificationRecord, string>(instant,
                        replaced(";cleanup-state:held-touch:False;", ";cleanup-state:held-touch:True;")) },
                { "route:instant-touch:spend-invoked:True:", new KeyValuePair<CastingQualificationRecord, string>(
                    instant, replaced(";spend-invoked:True;", ";spend-invoked:False;")) },
                { "route:delivery-not-derived:", new KeyValuePair<CastingQualificationRecord, string>(
                    instant, replaced(";delivery-guid:" + StickyTouchWorld.CircleDelivery + ";resolution:",
                        ";delivery-guid:" + StickyTouchWorld.Circle.BaseAbilityGuid + ";resolution:")) },
                { "route:settled-other-touch:", new KeyValuePair<CastingQualificationRecord, string>(
                    instant, replaced(";transaction-state:held-touch:False;delivery-command-present:False;carrier-guid:" +
                        StickyTouchWorld.Circle.BaseAbilityGuid + ";delivery-guid:" + StickyTouchWorld.CircleDelivery,
                        ";transaction-state:held-touch:False;delivery-command-present:False;carrier-guid:" +
                        StickyTouchWorld.Circle.BaseAbilityGuid + ";delivery-guid:other-delivery")) }
            };
            foreach (KeyValuePair<string, KeyValuePair<CastingQualificationRecord, string>> item in routeCases)
            {
                string failure = useWith(item.Value.Key, entry => StickyEntry(entry, item.Value.Value));
                if (failure == null || !failure.StartsWith(item.Key, StringComparison.Ordinal))
                    throw new InvalidOperationException("A wrong touch route was judged " + (failure ?? "valid") +
                        ", expected " + item.Key);
            }
            // Spend: a double spend from the game's count, an unreported
            // spend, an unread count; the effect: missing, or not confirmed.
            var cases = new Dictionary<string, Func<string>>
            {
                { "resource:qual-cast-1:2>0:expected-spend=1", () => StickyRecordWith(instant, "use", step =>
                    { step.Availability.Clear(); step.Availability.Add("qual-cast-1:2>0"); }).StepFailure("use") },
                { "spend:not-reported", () => useWith(animated, entry => StickyEntry(entry, spendReported: false)) },
                { "resource:unobserved:", () => StickyRecordWith(instant, "use", step => step.Availability.Clear())
                    .StepFailure("use") },
                { "tokens:qual-cast-1:" + StickyTouchWorld.CircleToken + "=T>T", () => StickyRecordWith(instant, "use",
                    step =>
                    {
                        step.TokenReadings.Clear();
                        step.TokenReadings.Add(new CastingQualificationTokenReading("qual-cast-1",
                            StickyTouchWorld.CircleToken, true, true));
                    }).StepFailure("use") },
                { "effects:qual-cast-1:unchanged", () => StickyRecordWith(instant, "use", step =>
                    { step.Transitions.Clear(); step.Transitions.Add("qual-cast-1:unchanged"); }).StepFailure("use") },
                { "states:qual-cast-1=Failed", () => useWith(instant,
                    entry => StickyEntry(entry, state: CastingOutcomeState.Failed)) },
                // The repeat must cast nothing.
                { "not-a-no-op:", () => StickyRecordWith(instant, "repeat", step =>
                    { step.ApplyAllowed = true; step.ApplyReason = "run-started:run-2"; }).StepFailure("repeat") },
                // The exhausted step: accepted, refused for another reason
                // (the spontaneous wording on a prepared slot, another pool,
                // another cause), the slot available again, the count changed.
                { "not-refused:", () => StickyRecordWith(instant, "exhausted", step =>
                    { step.ApplyAllowed = true; step.ApplyReason = "qualification-allowance-exhausted"; })
                    .StepFailure("exhausted") },
                { "refused-for-another-reason:apply-refused:blocked-casting:qual-cast-1:resource-pool-exhausted:",
                    () => StickyRecordWith(instant, "exhausted", step => step.ApplyReason =
                        "apply-refused:blocked-casting:qual-cast-1:resource-pool-exhausted:" +
                        StickyTouchWorld.ClericPool + ":0<1").StepFailure("exhausted") },
                { "refused-for-another-reason:apply-refused:blocked-casting:qual-cast-1:prepared-slots-exhausted:other",
                    () => StickyRecordWith(instant, "exhausted", step => step.ApplyReason =
                        "apply-refused:blocked-casting:qual-cast-1:prepared-slots-exhausted:other-pool")
                        .StepFailure("exhausted") },
                { "refused-for-another-reason:apply-refused:blocked-casting:qual-cast-1:caster-", () =>
                    StickyRecordWith(instant, "exhausted", step => step.ApplyReason =
                        "apply-refused:blocked-casting:qual-cast-1:caster-cannot-cast").StepFailure("exhausted") },
                { "tokens:qual-cast-1:" + StickyTouchWorld.CircleToken + "=F>T", () => StickyRecordWith(instant,
                    "exhausted", step =>
                    {
                        step.TokenReadings.Clear();
                        step.TokenReadings.Add(new CastingQualificationTokenReading("qual-cast-1",
                            StickyTouchWorld.CircleToken, false, true));
                    }).StepFailure("exhausted") },
                { "tokens:qual-cast-1:unobserved", () => StickyRecordWith(instant, "exhausted",
                    step => step.TokenReadings.Clear()).StepFailure("exhausted") },
                { "resource:qual-cast-1:0>1", () => StickyRecordWith(instant, "exhausted", step =>
                    { step.Availability.Clear(); step.Availability.Add("qual-cast-1:0>1"); }).StepFailure("exhausted") }
            };
            foreach (KeyValuePair<string, Func<string>> item in cases)
            {
                string failure = item.Value();
                if (failure == null || !failure.StartsWith(item.Key, StringComparison.Ordinal))
                    throw new InvalidOperationException("A wrong touch outcome was judged " + (failure ?? "valid") +
                        ", expected " + item.Key);
            }
            // The spontaneous wording is exact too: the shortage must name the
            // reserved pool and fewer units than one cast.
            var spontaneousWorld = new StickyTouchWorld { ArcanistCasts = 1 };
            spontaneousWorld.SetToken(StickyTouchWorld.CircleToken, false);
            CastingQualificationRecord spontaneous = RunStickyQualification(Path.Combine(root, "sqj-s"),
                spontaneousWorld, "instant");
            if (spontaneous.Violations().Count != 0)
                throw new InvalidOperationException("The spontaneous judged run is not clean: " +
                    string.Join("|", spontaneous.Violations().ToArray()));
            foreach (string wording in new[]
            {
                "apply-refused:blocked-casting:qual-cast-1:prepared-slots-exhausted:" + StickyTouchWorld.ArcanistPool,
                "apply-refused:blocked-casting:qual-cast-1:resource-pool-exhausted:" + StickyTouchWorld.ArcanistPool + ":1<1",
                "apply-refused:blocked-casting:qual-cast-1:resource-pool-exhausted:" + StickyTouchWorld.ArcanistPool + ":0<2",
                "apply-refused:blocked-casting:qual-cast-1:resource-pool-exhausted:" + StickyTouchWorld.ArcanistPool + ":x<1",
                "apply-refused:blocked-casting:qual-cast-1:resource-pool-exhausted:" + StickyTouchWorld.ClericPool + ":0<1"
            })
            {
                string failure = StickyRecordWith(spontaneous, "exhausted", step => step.ApplyReason = wording)
                    .StepFailure("exhausted");
                if (failure == null || !failure.StartsWith("refused-for-another-reason:", StringComparison.Ordinal))
                    throw new InvalidOperationException("A wrong spontaneous exhaustion was accepted: " + wording);
            }
        }

        // The recipe is known to the host (allowance parsing, step counts,
        // the single-use flow) and to every script that names recipes: the
        // launcher's choices, the allowance's build binding, the allowance
        // writer's purposes and the launcher meta-test's list of approvable
        // recipes all name exactly the host's recipes.
        private static void TestStickyQualRegistration()
        {
            string recipe = CastingQualificationRecipe.StickyTouchDirect;
            if (recipe != "sticky-touch-direct" || !CastingQualificationRecipe.IsKnown(recipe) ||
                !CastingQualificationRecipe.IsSingleUseRecipe(recipe) ||
                !CastingQualificationRecipe.IsSingleUseRecipe(CastingQualificationRecipe.AbilityPoolDirect) ||
                CastingQualificationRecipe.MinimumCastings(recipe) != 1 ||
                CastingQualificationRecipe.ForecastSteps(recipe) != 1 ||
                CastingQualificationRecipe.IsTwoPhase(recipe) || CastingQualificationRecipe.HasDisableStep(recipe) ||
                CastingQualificationRecipe.IsKnown("improvised"))
                throw new InvalidOperationException("The touch recipe is not registered as a single-use recipe.");
            foreach (string mode in new[] { "instant", "animated" })
            {
                string refusal;
                CastingQualificationAllowance parsed = CastingQualificationAllowance.Parse(QualificationAllowanceJson(o =>
                {
                    o["recipe"] = recipe;
                    o["executionMode"] = mode;
                }), "qual-run-1", out refusal);
                if (parsed == null || parsed.Recipe != recipe || parsed.ExecutionMode != mode)
                    throw new InvalidOperationException("The touch allowance was refused: " + refusal);
            }
            List<string> host = typeof(CastingQualificationRecipe)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string) && field.Name != "RoutineId")
                .Select(field => (string)field.GetRawConstantValue())
                .OrderBy(value => value, StringComparer.Ordinal).ToList();
            if (!host.Contains(recipe) || host.Any(value => !CastingQualificationRecipe.IsKnown(value)))
                throw new InvalidOperationException("The host's recipe constants are not all known: " +
                    string.Join(",", host.ToArray()));
            string scripts = Path.Combine(FindRepositoryRoot(), "scripts");
            Func<string, string, string, List<string>> named = (file, pattern, item) =>
            {
                Match match = Regex.Match(File.ReadAllText(Path.Combine(scripts, file)), pattern, RegexOptions.Singleline);
                if (!match.Success) throw new InvalidOperationException("No recipe list in " + file);
                return Regex.Matches(match.Groups[1].Value, item, RegexOptions.Multiline).Cast<Match>()
                    .Select(value => value.Groups[1].Value).OrderBy(value => value, StringComparer.Ordinal).ToList();
            };
            var lists = new Dictionary<string, List<string>>
            {
                { "launcher", named("Invoke-KingmakerRuntimeTest.ps1",
                    @"\[ValidateSet\(([^)]*)\)\]\[string\]\$QualificationRecipe", "'([a-z-]+)'") },
                { "build-binding", named("RuntimeAutomation.Common.ps1",
                    @"if \(@\(([^)]*)\) -cnotcontains \[string\]\$allowance\.recipe\)", "'([a-z-]+)'") },
                { "writer", named("New-KbpRunAllowance.ps1", @"\$purposes = @\{(.*?)\r?\n    \}",
                    @"^\s*'([a-z-]+)' = ") },
                // The meta-test approves every recipe besides its default one.
                { "launcher-meta-test", named("Test-RuntimeLauncherFileWhatIf.ps1",
                    @"foreach \(\$knownRecipe in @\(([^)]*)\)\)", "'([a-z-]+)'")
                    .Concat(new[] { CastingQualificationRecipe.ZeroCostMixed })
                    .OrderBy(value => value, StringComparer.Ordinal).ToList() }
            };
            foreach (KeyValuePair<string, List<string>> list in lists)
                if (!list.Value.SequenceEqual(host, StringComparer.Ordinal))
                    throw new InvalidOperationException("The " + list.Key + " recipes differ from the host's: " +
                        string.Join(",", list.Value.ToArray()) + " != " + string.Join(",", host.ToArray()));
            string writer = File.ReadAllText(Path.Combine(scripts, "New-KbpRunAllowance.ps1"));
            Match purpose = Regex.Match(writer, "'sticky-touch-direct' = \"([^\"]*)\"");
            if (!purpose.Success || purpose.Groups[1].Value.Replace("$ExecutionMode", "animated").Length > 400 ||
                !purpose.Groups[1].Value.StartsWith("sticky-touch-direct casting-first qualification in $ExecutionMode mode",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("The touch allowance purpose is missing or longer than the launcher accepts.");
        }

        // What the judge requires of an instant touch is what the production
        // instant adapter and executor actually record (they need the game to
        // run, so their exact source is pinned here): the adapter's detail
        // fields and its touch-transaction state, and the executor's
        // transaction and cleanup suffix.
        private static void TestStickyQualRouteMarkers()
        {
            string adapter = ProductionSource("GameAdapters", "KingmakerInstantCastAdapter.cs");
            string executor = ProductionSource("Execution", "InstantCastExecutor.cs");
            string[] adapterMarkers =
            {
                "\"rule-success:\" + rule.Success + \";umd-failed:\" + rule.IsUMDFailed +",
                "\";spell-failed:\" + rule.IsSpellFailed + \";spend-invoked:\" + spendInvoked +",
                "\";spend-failure:\" + (spendFailure == null",
                "\";provider-direct:\" + providerDirect +",
                "\";spend-owner:source-ability-data\" +",
                "\";strategy:\" + step.ExecutionStrategy +",
                "\";strategy-reason:\" + step.ExecutionStrategyReason +",
                "\";carrier-guid:\" + carrierGuid +",
                "\";delivery-guid:\" + deliveryGuid +",
                "\";rule-cast-submitted:true;expected-effects:\" +",
                "\";carrier-command-created:false;delivery-command-created:false\" +",
                "string detail = \"held-touch:\" + heldTouch +",
                "\";delivery-command-present:\" + (deliveryCommand != null) +",
                "if (residual) return InstantCastCompletion.Pending(detail);",
                "return InstantCastCompletion.Settled(detail);"
            };
            string[] executorMarkers =
            {
                "string terminalDetail = result.Detail +",
                "\";transaction-complete:\" +",
                "\";transaction-state:\" +",
                "\";cleanup-complete:\" + cleanup.Complete +",
                "\";cleanup-state:\" + cleanup.Detail;",
                "InstantCastCompletion cleanup = completion;"
            };
            string missing = adapterMarkers.FirstOrDefault(marker => !adapter.Contains(marker)) ??
                executorMarkers.FirstOrDefault(marker => !executor.Contains(marker));
            if (missing != null)
                throw new InvalidOperationException("The instant touch record no longer has: " + missing);
            // The delivery's own GUID is the derived delivery blueprint's, the
            // carrier's the source's; the auto-hit check stays before the cast.
            if (!adapter.Contains("stickyResolution.DeliveryBlueprint.AssetGuid") ||
                !adapter.Contains("sourceAbility.Blueprint.AssetGuid") ||
                adapter.IndexOf("AutoHitRefusal(", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("The instant touch record does not name the derived delivery.");
        }
    }
}
