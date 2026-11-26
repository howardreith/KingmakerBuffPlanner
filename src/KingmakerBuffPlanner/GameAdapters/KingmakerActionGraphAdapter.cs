using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Effects;

namespace KingmakerBuffPlanner.GameAdapters
{
    internal sealed class KingmakerActionGraphAdapter
    {
        private static readonly Dictionary<Type, MemberInfo[]> ActionListMembers =
            new Dictionary<Type, MemberInfo[]>();
        private readonly HashSet<string> _activeAbilities = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<BlueprintAbility> _activeAbilityObjects =
            new HashSet<BlueprintAbility>(ReferenceEqualityComparer<BlueprintAbility>.Instance);
        private readonly List<BlueprintAbility> _referenced = new List<BlueprintAbility>();
        // The abilities being adapted, innermost last: the blueprint whose
        // context runs the action now (MechanicsContext.AssociatedBlueprint).
        private readonly List<BlueprintAbility> _abilityStack = new List<BlueprintAbility>();

        // Missing-beneficial-buffs recovery (R11-F1): every ability this
        // adapter followed through a reference (ContextActionCastSpell, a
        // sticky-touch delivery), in order. The graph of a referenced ability
        // is read from its run action or delivery only; whatever its other
        // components execute is not, so a caller judging a whole cast can
        // demand positive evidence for them.
        internal IReadOnlyList<BlueprintAbility> ReferencedAbilities { get { return _referenced; } }

        internal DiscoveryNode Adapt(BlueprintAbility ability)
        {
            if (ability == null) throw new ArgumentNullException("ability");
            return AdaptAbility(ability);
        }

        // One action list of a buff's own components (AddFactContextActions),
        // adapted exactly as an ability's actions are (rc4 review finding 1).
        internal DiscoveryNode AdaptActions(ActionList list)
        {
            return AdaptList(list);
        }

        private DiscoveryNode AdaptAbility(BlueprintAbility ability)
        {
            string id = ability.AssetGuid;
            if (_activeAbilities.Contains(id) || _activeAbilityObjects.Contains(ability))
                return new DiscoveryNode(DiscoveryNodeKind.Unknown, id, sourceContract: "ability-cycle");
            _activeAbilities.Add(id);
            _activeAbilityObjects.Add(ability);
            _abilityStack.Add(ability);
            try
            {
                var children = new List<DiscoveryNode>();
                DiscoveryNode payload = null;
                AbilityEffectStickyTouch sticky = ability.GetComponent<AbilityEffectStickyTouch>();
                if (sticky != null && sticky.TouchDeliveryAbility != null)
                    payload = Reference(sticky.TouchDeliveryAbility, "AbilityEffectStickyTouch");
                else
                {
                    AbilityEffectRunAction run = ability.GetComponent<AbilityEffectRunAction>();
                    if (run != null) payload = AdaptList(run.Actions);
                }
                if (payload != null)
                {
                    AbilityTargetsAround targetsAround =
                        ability.GetComponent<AbilityTargetsAround>();
                    if (targetsAround != null)
                        payload = Target(AreaTarget(targetsAround.TargetType, ability), payload,
                            "AbilityTargetsAround");
                    children.Add(payload);
                }
                return new DiscoveryNode(DiscoveryNodeKind.AbilityReference, id, children,
                    referencedAbilityId: id, sourceContract: "BlueprintAbility");
            }
            finally
            {
                _abilityStack.RemoveAt(_abilityStack.Count - 1);
                _activeAbilities.Remove(id);
                _activeAbilityObjects.Remove(ability);
            }
        }

        private DiscoveryNode Reference(BlueprintAbility ability, string contract)
        {
            if (!_referenced.Contains(ability)) _referenced.Add(ability);
            return new DiscoveryNode(DiscoveryNodeKind.AbilityReference, ability.AssetGuid,
                new[] { AdaptAbility(ability) }, referencedAbilityId: ability.AssetGuid,
                sourceContract: contract);
        }

        private DiscoveryNode AdaptList(ActionList list)
        {
            GameAction[] actions = list == null || list.Actions == null ? new GameAction[0] : list.Actions;
            return new DiscoveryNode(DiscoveryNodeKind.Sequence, "ActionList",
                actions.Select(AdaptAction).ToArray(), sourceContract: "ActionList");
        }

        private DiscoveryNode AdaptAction(GameAction action)
        {
            if (action == null) return new DiscoveryNode(DiscoveryNodeKind.Empty, "null");
            var apply = action as ContextActionApplyBuff;
            if (apply != null && apply.Buff != null)
                return Effect(EffectKind.Buff, apply.Buff.AssetGuid,
                    apply.ToCaster ? EffectTarget.Caster : EffectTarget.CurrentTarget,
                    "ContextActionApplyBuff", MaximumDurationSeconds(apply), CasterLevelStrengthOf(apply.Buff),
                    IsCasterRelative(apply.Buff), apply.Permanent);
            var conditional = action as Conditional;
            if (conditional != null)
                return new DiscoveryNode(DiscoveryNodeKind.Conditional, action.GetType().FullName,
                    whenTrue: AdaptList(conditional.IfTrue), whenFalse: AdaptList(conditional.IfFalse),
                    conditionContract: DescribeConditions(conditional), sourceContract: "Conditional",
                    casterFactGate: CasterGate(conditional), recipientFactGate: RecipientGate(conditional));
            var cast = action as ContextActionCastSpell;
            if (cast != null && cast.Spell != null) return Reference(cast.Spell, "ContextActionCastSpell");
            var pet = action as ContextActionsOnPet;
            if (pet != null) return Target(EffectTarget.Pet, AdaptList(pet.Actions), "ContextActionsOnPet");
            var party = action as ContextActionPartyMembers;
            if (party != null) return Target(EffectTarget.Party, AdaptList(party.Action), "ContextActionPartyMembers");
            var enchant = action as ContextActionEnchantWornItem;
            if (enchant != null && enchant.Enchantment != null)
                return Effect(EffectKind.WornItemEnchantment, enchant.Enchantment.AssetGuid,
                    enchant.ToCaster ? EffectTarget.Caster : EffectTarget.CurrentTarget,
                    "ContextActionEnchantWornItem");
            var magicFang = action as MagicFang;
            if (magicFang != null)
                return AdaptMagicFang(magicFang,
                    _abilityStack.Count == 0 ? null : _abilityStack[_abilityStack.Count - 1]);
            if (action is ContextActionSpawnMonster)
                return new DiscoveryNode(DiscoveryNodeKind.Unknown,
                    DescribeType(action.GetType()), sourceContract: "excluded-summoning-action");
            // Missing-beneficial-buffs mission §3.B: the exact native removal
            // of one buff keeps which buff it removes and from whom, so the
            // planner can recognize mutually exclusive selections (each
            // applies its own buff and removes the others).
            var remove = action as ContextActionRemoveBuff;
            if (remove != null && action.GetType() == typeof(ContextActionRemoveBuff))
                return new DiscoveryNode(DiscoveryNodeKind.RestorativeAction,
                    DescribeType(action.GetType()), sourceContract: "restorative-action",
                    effectId: remove.Buff == null ? null : remove.Buff.AssetGuid,
                    target: remove.ToCaster ? EffectTarget.Caster : EffectTarget.CurrentTarget);
            if (IsRestorativeAction(action.GetType()))
                return new DiscoveryNode(DiscoveryNodeKind.RestorativeAction,
                    DescribeType(action.GetType()), sourceContract: "restorative-action");
            if (IsOffensiveAction(action.GetType()))
                return new DiscoveryNode(DiscoveryNodeKind.OffensiveAction,
                    DescribeType(action.GetType()), sourceContract: "offensive-action");
            if (action is ContextActionSelectByValue)
                return AdaptExactActionListAlternatives(action, "m_Variants", "Action",
                    "ContextActionSelectByValue");
            if (action is ContextActionRandomize)
                return AdaptExactActionListAlternatives(action, "m_Actions", "Action",
                    "ContextActionRandomize");
            // Call of the Wild (rc4 review finding 1): RunActionsDependingOnContextValue
            // runs exactly one of its "actions" lists, chosen by a context value
            // (the Battle, Bone and Wind Ward and Draconic Resilience hexes apply
            // their ward buff through it). Exact contract by type and field shape;
            // optional, no compile-time dependency.
            if (action.GetType().FullName == RunActionsDependingOnContextValue)
                return AdaptExactActionListArrayAlternatives(action, "actions",
                    "RunActionsDependingOnContextValue");
            var area = action as ContextActionSpawnAreaEffect;
            if (area != null && area.AreaEffect != null)
            {
                AbilityAreaEffectBuff areaBuff = area.AreaEffect.GetComponent<AbilityAreaEffectBuff>();
                if (areaBuff != null && areaBuff.Buff != null)
                    return Effect(EffectKind.AreaBuff, areaBuff.Buff.AssetGuid,
                        AreaEffectTarget(areaBuff),
                        "ContextActionSpawnAreaEffect+AbilityAreaEffectBuff");
            }
            DiscoveryNode reflected = AdaptProvenActionLists(action);
            NearbyRecipientRule nearby = ReadNearbyRecipientRule(action);
            if (action.GetType().FullName == "CallOfTheWild.AoeMechanics.ApplyActionToTargetsInRange" && nearby == null)
                return new DiscoveryNode(DiscoveryNodeKind.Unknown, DescribeType(action.GetType()),
                    sourceContract: "nearby-recipient-contract-unverified");
            if (reflected != null && nearby != null)
                return new DiscoveryNode(reflected.Kind, reflected.Identity, reflected.Children,
                    sourceContract: reflected.SourceContract, nearbyRecipients: nearby);
            return reflected ?? new DiscoveryNode(DiscoveryNodeKind.Unknown,
                DescribeType(action.GetType()), sourceContract: "unsupported-action");
        }

        // Optional CotW wrapper: excludes the primary target, filters native
        // allegiance flags and applies the child list to nearest other units.
        // A generic ActionList walk alone loses that recipient transformation.
        private static NearbyRecipientRule ReadNearbyRecipientRule(object action)
        {
            Type type = action == null ? null : action.GetType();
            if (type == null || type.FullName != "CallOfTheWild.AoeMechanics.ApplyActionToTargetsInRange") return null;
            var names = new[] { "Radius", "max_num_units", "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies", "actions" };
            var types = new[] { typeof(float), typeof(int), typeof(bool), typeof(bool), typeof(bool), typeof(ActionList) };
            FieldInfo[] fields = names.Select(name => type.GetField(name, BindingFlags.Public | BindingFlags.Instance)).ToArray();
            if (fields.Where((field, index) => field == null || field.FieldType != types[index]).Any()) return null;
            float radius = (float)fields[0].GetValue(action);
            int maximum = (int)fields[1].GetValue(action);
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0 || maximum < 0) return null;
            return new NearbyRecipientRule(radius, maximum, (bool)fields[2].GetValue(action),
                (bool)fields[3].GetValue(action), (bool)fields[4].GetValue(action));
        }

        private DiscoveryNode AdaptExactActionListAlternatives(
            GameAction action, string arrayFieldName, string actionFieldName, string contract)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo arrayField = action.GetType().GetField(arrayFieldName, flags);
            if (arrayField == null || !arrayField.FieldType.IsArray)
                return new DiscoveryNode(DiscoveryNodeKind.Unknown, DescribeType(action.GetType()),
                    sourceContract: contract + ":array-contract-missing");
            Array wrappers = arrayField.GetValue(action) as Array;
            if (wrappers == null || wrappers.Length == 0)
                return new DiscoveryNode(DiscoveryNodeKind.Empty, contract, sourceContract: contract);
            var alternatives = new List<DiscoveryNode>();
            foreach (object wrapper in wrappers)
            {
                if (wrapper == null) continue;
                FieldInfo actionField = wrapper.GetType().GetField(actionFieldName, flags);
                if (actionField == null || actionField.FieldType != typeof(ActionList))
                    return new DiscoveryNode(DiscoveryNodeKind.Unknown, DescribeType(wrapper.GetType()),
                        sourceContract: contract + ":ActionList-contract-missing");
                alternatives.Add(AdaptList((ActionList)actionField.GetValue(wrapper)));
            }
            return BuildAlternatives(alternatives, 0, contract);
        }

        internal const string RunActionsDependingOnContextValue =
            "CallOfTheWild.NewMechanics.RunActionsDependingOnContextValue";

        private DiscoveryNode AdaptExactActionListArrayAlternatives(
            GameAction action, string arrayFieldName, string contract)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo arrayField = action.GetType().GetField(arrayFieldName, flags);
            if (arrayField == null || arrayField.FieldType != typeof(ActionList[]))
                return new DiscoveryNode(DiscoveryNodeKind.Unknown, DescribeType(action.GetType()),
                    sourceContract: contract + ":ActionList-array-contract-missing");
            ActionList[] lists = arrayField.GetValue(action) as ActionList[];
            if (lists == null || lists.Length == 0)
                return new DiscoveryNode(DiscoveryNodeKind.Empty, contract, sourceContract: contract);
            return BuildAlternatives(lists.Select(AdaptList).ToArray(), 0, contract);
        }

        private static DiscoveryNode BuildAlternatives(
            IReadOnlyList<DiscoveryNode> alternatives, int index, string contract)
        {
            if (index >= alternatives.Count)
                return new DiscoveryNode(DiscoveryNodeKind.Empty, contract + ":none");
            if (index == alternatives.Count - 1) return alternatives[index];
            return new DiscoveryNode(DiscoveryNodeKind.Conditional,
                contract + ":alternative:" + index,
                whenTrue: alternatives[index],
                whenFalse: BuildAlternatives(alternatives, index + 1, contract),
                conditionContract: contract + ":runtime-selected-alternative",
                sourceContract: contract);
        }

        private DiscoveryNode AdaptProvenActionLists(GameAction action)
        {
            var lists = new List<DiscoveryNode>();
            foreach (MemberInfo member in GetActionListMembers(action.GetType()))
            {
                try
                {
                    var field = member as FieldInfo;
                    ActionList value = field != null
                        ? (ActionList)field.GetValue(action)
                        : (ActionList)((PropertyInfo)member).GetValue(action, null);
                    lists.Add(AdaptList(value));
                }
                catch (Exception exception)
                {
                    lists.Add(new DiscoveryNode(DiscoveryNodeKind.Unknown,
                        DescribeType(action.GetType()) + "." + member.Name,
                        sourceContract: "ActionList-read-failed:" + exception.GetType().FullName));
                }
            }
            return lists.Count == 0 ? null : new DiscoveryNode(
                DiscoveryNodeKind.Sequence, "reflected:" + DescribeType(action.GetType()), lists,
                sourceContract: "reflected-exact-ActionList-wrapper");
        }

        private static MemberInfo[] GetActionListMembers(Type type)
        {
            MemberInfo[] members;
            if (ActionListMembers.TryGetValue(type, out members)) return members;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            members = type.GetFields(flags)
                .Where(f => f.FieldType == typeof(ActionList)).Cast<MemberInfo>()
                .Concat(type.GetProperties(flags)
                    .Where(p => p.PropertyType == typeof(ActionList) && p.CanRead &&
                        p.GetIndexParameters().Length == 0).Cast<MemberInfo>())
                .OrderBy(m => m.Name, StringComparer.Ordinal)
                .ThenBy(m => m.MemberType)
                .ToArray();
            ActionListMembers.Add(type, members);
            return members;
        }

        // Greater Magic Fang repair. Native 2.1.7b MagicFang.RunAction (read
        // from its IL; pinned in the regression): an ordinary casting applies
        // Enchantment[0]; a greater one applies exactly ONE element,
        // Enchantment[Math.Min(EnchantLevel.Calculate(context), n) - 1], to the
        // target's empty-hand weapon and its natural weapons. The elements are
        // alternative tiers, never simultaneous effects, and the array order
        // is the tier order (Call of the Wild replaces the array with its own
        // five), so it is kept, never sorted. The tier is bound to the
        // provider's caster level through CasterLevelPredicate gates. A level
        // contract this adapter cannot prove is unknown, never "every tier"
        // or "any tier".
        private static DiscoveryNode AdaptMagicFang(MagicFang action, BlueprintAbility ability)
        {
            Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment[] enchantments =
                action.Enchantment ?? new Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment[0];
            if (!action.Greater)
                return enchantments.Length == 0 || enchantments[0] == null
                    ? new DiscoveryNode(DiscoveryNodeKind.Unknown, "MagicFang",
                        sourceContract: "MagicFang:enchantment-missing")
                    : new DiscoveryNode(DiscoveryNodeKind.Sequence, "MagicFang.Enchantment",
                        new[] { MagicFangTier(enchantments[0]) }, sourceContract: "MagicFang");
            int[] tiers = MagicFangTierIndices(action.EnchantLevel, enchantments.Length, ability);
            if (tiers == null || tiers.Skip(1).Any(index => enchantments[index] == null))
                return new DiscoveryNode(DiscoveryNodeKind.Unknown, "MagicFang",
                    sourceContract: "MagicFang:enchant-level-unverified");
            DiscoveryNode selection = MagicFangTier(enchantments[tiers[1]]);
            for (int level = 2; level < tiers.Length; level++)
            {
                if (tiers[level] == tiers[level - 1]) continue;
                selection = new DiscoveryNode(DiscoveryNodeKind.Conditional, "MagicFang.EnchantLevel",
                    whenTrue: MagicFangTier(enchantments[tiers[level]]), whenFalse: selection,
                    conditionContract: "MagicFang.EnchantLevel:caster-level-at-least-" +
                        level.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    sourceContract: "MagicFang",
                    casterFactGate: new CasterFactGate(false, new[]
                    {
                        new KeyValuePair<string, bool>(CasterLevelPredicate.Key(level), false)
                    }));
            }
            return selection.Kind == DiscoveryNodeKind.Effect
                ? new DiscoveryNode(DiscoveryNodeKind.Sequence, "MagicFang.Enchantment", new[] { selection },
                    sourceContract: "MagicFang")
                : selection;
        }

        private static DiscoveryNode MagicFangTier(
            Kingmaker.Blueprints.Items.Ecnchantments.BlueprintItemEnchantment enchantment)
        {
            return Effect(EffectKind.WornItemEnchantment, enchantment.AssetGuid, EffectTarget.CurrentTarget,
                "MagicFang");
        }

        // Caster levels the planner binds a tier for (1..MagicFangLevelProbe;
        // the selection at the probe must also hold for every higher level).
        internal const int MagicFangLevelProbe = 40;

        // The element index native MagicFang selects at each caster level
        // 1..MagicFangLevelProbe (index 0 unused), or null when that cannot be
        // proven: EnchantLevel must be a constant or the rank of exactly one
        // ContextRankConfig of the casting ability (MechanicsContext.
        // RecalculateRanks reads AssociatedBlueprint's rank configs) based on
        // caster level, with no ability-params calculator replacing the
        // context's parameters, giving a valid index at every level, never
        // decreasing as the level rises, and unchanged beyond the probe. The
        // rank itself is computed by that config's own native
        // ApplyProgression and ApplyMinMax (ContextRankConfig.GetValue), so no
        // progression formula is restated here.
        internal static int[] MagicFangTierIndices(ContextValue enchantLevel, int count, BlueprintAbility ability)
        {
            try
            {
                if (enchantLevel == null || count < 1) return null;
                Func<int, int> value;
                if (enchantLevel.ValueType == ContextValueType.Simple)
                {
                    int constant = enchantLevel.Value;
                    value = level => constant;
                }
                else if (enchantLevel.ValueType == ContextValueType.Rank)
                {
                    BlueprintComponent[] components = ability == null ? null : ability.ComponentsArray;
                    if (components == null || components.Any(component =>
                            component is Kingmaker.UnitLogic.Mechanics.Components.ContextAbilityParamsCalculator))
                        return null;
                    object rank = FieldValue(enchantLevel, "ValueRank");
                    var configs = components.OfType<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>()
                        .Where(candidate => Equals(FieldValue(candidate, "m_Type"), rank)).ToList();
                    if (rank == null || configs.Count != 1 ||
                        NamedValue(configs[0], "m_BaseValueType") != "CasterLevel")
                        return null;
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                    Type type = typeof(Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig);
                    MethodInfo progression = type.GetMethod("ApplyProgression", flags, null, new[] { typeof(int) }, null);
                    MethodInfo minMax = type.GetMethod("ApplyMinMax", flags, null, new[] { typeof(int) }, null);
                    if (progression == null || minMax == null || progression.ReturnType != typeof(int) ||
                        minMax.ReturnType != typeof(int))
                        return null;
                    object rankConfig = configs[0];
                    value = level => (int)minMax.Invoke(rankConfig, new object[]
                    {
                        (int)progression.Invoke(rankConfig, new object[] { level })
                    });
                }
                else return null;
                var indices = new int[MagicFangLevelProbe + 1];
                for (int level = 1; level <= MagicFangLevelProbe; level++)
                {
                    indices[level] = Math.Min(value(level), count) - 1;
                    if (indices[level] < 0 || indices[level] >= count ||
                        (level > 1 && indices[level] < indices[level - 1]))
                        return null;
                }
                foreach (int beyond in new[] { MagicFangLevelProbe + 1, 100, 1000 })
                    if (Math.Min(value(beyond), count) - 1 != indices[MagicFangLevelProbe]) return null;
                return indices;
            }
            catch (Exception) { return null; }
        }

        private static DiscoveryNode Effect(EffectKind kind, string id, EffectTarget target, string contract,
            float? maximumDurationSeconds = null, CasterLevelStrength casterLevelStrength = null,
            bool casterRelative = false, bool permanentApplication = false)
        {
            return new DiscoveryNode(DiscoveryNodeKind.Effect, contract, effectKind: kind,
                effectId: id, target: target, sourceContract: contract,
                maximumDurationSeconds: maximumDurationSeconds, casterLevelStrength: casterLevelStrength,
                casterRelative: casterRelative, permanentApplication: permanentApplication);
        }

        // Review R8-F1: native buff components whose benefit is relative to
        // the buff's caster (AddImmunityFirebrand grants immunity to that
        // caster's fire spells; its reads of the context caster are pinned
        // in the native contract regression).
        internal static readonly string[] CasterRelativeComponents =
        {
            "Kingmaker.UnitLogic.FactLogic.AddImmunityFirebrand"
        };

        internal static bool IsCasterRelative(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff buff)
        {
            try
            {
                return buff != null && (buff.ComponentsArray ?? new BlueprintComponent[0])
                    .Any(component => component != null &&
                        CasterRelativeComponents.Contains(component.GetType().FullName));
            }
            catch (Exception) { return false; }
        }

        // Missing-beneficial-buffs review R7-F3: the buff's caster-level
        // strength, recorded only when it is provably a single native
        // ContextRankConfig (m_BaseValueType CasterLevel, m_Progression
        // Custom) and every ContextValue of every component is a constant or
        // that rank; anything else (another value source, a nested value
        // container, a second rank config, an unreadable field) leaves it
        // null and the plain caster-level comparison applies.
        internal static CasterLevelStrength CasterLevelStrengthOf(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff buff)
        {
            try
            {
                BlueprintComponent[] components = buff == null ? null : buff.ComponentsArray;
                if (components == null) return null;
                var configs = components.OfType<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>().ToList();
                if (configs.Count != 1) return null;
                object config = configs[0];
                if (NamedValue(config, "m_BaseValueType") != "CasterLevel" ||
                    NamedValue(config, "m_Progression") != "Custom")
                    return null;
                object rankType = FieldValue(config, "m_Type");
                var progression = FieldValue(config, "m_CustomProgression") as Array;
                if (rankType == null || progression == null || progression.Length == 0) return null;
                var items = new List<KeyValuePair<int, int>>();
                foreach (object item in progression)
                {
                    object baseValue = FieldValue(item, "BaseValue"), value = FieldValue(item, "ProgressionValue");
                    if (!(baseValue is int) || !(value is int)) return null;
                    items.Add(new KeyValuePair<int, int>((int)baseValue, (int)value));
                }
                for (int index = 1; index < items.Count; index++)
                    if (items[index].Key <= items[index - 1].Key) return null;
                foreach (BlueprintComponent component in components)
                {
                    if (component == null || ReferenceEquals(component, config)) continue;
                    for (Type type = component.GetType(); type != null && type != typeof(BlueprintComponent);
                        type = type.BaseType)
                        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public |
                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                        {
                            Type fieldType = field.FieldType;
                            if (fieldType == typeof(ContextValue))
                            {
                                var value = (ContextValue)field.GetValue(component);
                                if (value == null || value.ValueType == ContextValueType.Simple) continue;
                                if (value.ValueType != ContextValueType.Rank ||
                                    !Equals(FieldValue(value, "ValueRank"), rankType))
                                    return null;
                                continue;
                            }
                            if ((fieldType.IsArray && fieldType.GetElementType().Namespace != null &&
                                    fieldType.GetElementType().Namespace.StartsWith("Kingmaker.UnitLogic.Mechanics",
                                        StringComparison.Ordinal)) ||
                                (fieldType.Namespace != null && fieldType.Namespace.StartsWith(
                                    "Kingmaker.UnitLogic.Mechanics", StringComparison.Ordinal) &&
                                    !fieldType.IsEnum))
                                return null;
                        }
                }
                return new CasterLevelStrength(items);
            }
            catch (Exception) { return null; }
        }

        private static object FieldValue(object target, string name)
        {
            for (Type type = target == null ? null : target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
            }
            return null;
        }

        private static string NamedValue(object target, string name)
        {
            object value = FieldValue(target, name);
            return value == null ? null : value.ToString();
        }

        // H1 (0.6.11 review): the exact installed ContextActionApplyBuff
        // lifetime contract (Permanent, UseDurationSeconds/DurationSeconds,
        // DurationValue Rate/DiceType/DiceCountValue/BonusValue/m_IsExtendable).
        // Only a structurally fixed lifetime is reported, doubled as a
        // worst-case extension; permanent, dice-, rank-, property- or
        // otherwise context-dependent durations are unknown (null), which
        // keeps the effect a lasting-coverage obligation. Installed Call of
        // the Wild applies its Infectious Charms markers with
        // UseDurationSeconds=true and DurationSeconds=3.
        internal static float? MaximumDurationSeconds(ContextActionApplyBuff apply)
        {
            try
            {
                if (apply == null || apply.Permanent) return null;
                if (apply.UseDurationSeconds)
                    return apply.DurationSeconds > 0 && !float.IsInfinity(apply.DurationSeconds) && !float.IsNaN(apply.DurationSeconds)
                        ? apply.DurationSeconds * 2 : (float?)null;
                ContextDurationValue duration = apply.DurationValue;
                if (duration == null || duration.BonusValue == null ||
                    duration.BonusValue.ValueType != ContextValueType.Simple) return null;
                bool noDice = duration.DiceType == Kingmaker.RuleSystem.DiceType.Zero ||
                    (duration.DiceCountValue != null && duration.DiceCountValue.ValueType == ContextValueType.Simple &&
                        duration.DiceCountValue.Value == 0);
                if (!noDice || duration.BonusValue.Value <= 0) return null;
                float rate;
                switch (duration.Rate)
                {
                    case DurationRate.Rounds: rate = 6; break;
                    case DurationRate.Minutes: rate = 60; break;
                    case DurationRate.TenMinutes: rate = 600; break;
                    case DurationRate.Hours: rate = 3600; break;
                    case DurationRate.Days: rate = 86400; break;
                    default: return null;
                }
                return duration.BonusValue.Value * rate * 2;
            }
            catch (Exception) { return null; }
        }

        private static DiscoveryNode Target(EffectTarget target, DiscoveryNode child, string contract)
        {
            return new DiscoveryNode(DiscoveryNodeKind.TargetTransform, contract,
                new[] { child }, target: target, sourceContract: contract);
        }

        private static EffectTarget AreaTarget(TargetType target, BlueprintAbility ability)
        {
            return AreaRecipientSemantics.Resolve(AreaSelection(target),
                ability != null && ability.CanTargetFriends,
                ability != null && ability.CanTargetEnemies,
                ability != null && ability.CanTargetPoint);
        }

        private static AreaSelectionTarget AreaSelection(TargetType target)
        {
            if (target == TargetType.Ally) return AreaSelectionTarget.Ally;
            if (target == TargetType.Enemy) return AreaSelectionTarget.Enemy;
            if (target == TargetType.Any) return AreaSelectionTarget.Any;
            return AreaSelectionTarget.Unknown;
        }

        private static EffectTarget AreaEffectTarget(
            AbilityAreaEffectBuff areaBuff)
        {
            Condition[] conditions = areaBuff == null ||
                areaBuff.Condition == null ||
                areaBuff.Condition.Conditions == null
                    ? new Condition[0]
                    : areaBuff.Condition.Conditions;
            if (conditions.Length != 1 || conditions[0] == null ||
                conditions[0].Not)
                return EffectTarget.AmbiguousAreaRecipients;
            if (conditions[0] is ContextConditionIsAlly)
                return EffectTarget.AlliedAreaRecipients;
            if (conditions[0] is ContextConditionIsEnemy)
                return EffectTarget.EnemyAreaRecipients;
            return EffectTarget.AmbiguousAreaRecipients;
        }

        private static bool IsOffensiveAction(Type type)
        {
            string name = type == null ? string.Empty : type.FullName;
            return name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionAttack" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRangedAttack" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDirectDamage";
        }

        private static bool IsRestorativeAction(Type type)
        {
            string name = type == null ? string.Empty : type.FullName;
            return name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealEnergyDrain" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealStatDamage" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionResurrect" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffsByDescriptor" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuffSingleStack" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveDeathDoor" ||
                name == "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDispelMagic" ||
                // Call of the Wild's Treat Deadly Wounds (rc4): heals hit points
                // and ability damage at once (HealingMechanics source).
                name == "CallOfTheWild.HealingMechanics.ContextActionTreatDeadlyWounds";
        }

        private static CasterFactGate CasterGate(Conditional conditional)
        {
            var checker = conditional.ConditionsChecker;
            if (checker == null || checker.Conditions == null || checker.Conditions.Length == 0) return null;
            var facts = new List<KeyValuePair<string, bool>>();
            foreach (Condition condition in checker.Conditions)
            {
                var fact = condition as ContextConditionCasterHasFact;
                // A subclass can implement another predicate. Unknown contracts
                // stay unresolved; absent effects never decide a condition.
                if (fact == null || fact.GetType() != typeof(ContextConditionCasterHasFact) || fact.Fact == null ||
                    string.IsNullOrWhiteSpace(fact.Fact.AssetGuid)) return null;
                facts.Add(new KeyValuePair<string, bool>(fact.Fact.AssetGuid, fact.Not));
            }
            string operation = checker.Operation.ToString();
            return operation == "And" || operation == "Or" ? new CasterFactGate(operation == "Or", facts) : null;
        }

        // Missing-beneficial-buffs review R2-F5: a conditional whose every
        // condition is the exact native ContextConditionHasFact (a fact of the
        // unit the action runs on: Mass Blessing of Luck and Resolve skips
        // constructs and undead this way) is a recipient-fact gate. Review
        // R5-F3: so is the exact native ContextConditionHasBuff (a buff of
        // that unit, such as an opposing debuff a spell removes), which the planner
        // reads the same way; once unmodelled conditionals withhold coverage,
        // leaving it unmodelled would deny every such remove-opposing /
        // otherwise-apply spell its coverage. Any other contract (a checker
        // mixing caster and recipient predicates included) stays unresolved.
        private static RecipientFactGate RecipientGate(Conditional conditional)
        {
            var checker = conditional.ConditionsChecker;
            if (checker == null || checker.Conditions == null || checker.Conditions.Length == 0) return null;
            var facts = new List<KeyValuePair<string, bool>>();
            foreach (Condition condition in checker.Conditions)
            {
                var fact = condition as ContextConditionHasFact;
                if (fact != null && fact.GetType() == typeof(ContextConditionHasFact) && fact.Fact != null &&
                    !string.IsNullOrWhiteSpace(fact.Fact.AssetGuid))
                {
                    facts.Add(new KeyValuePair<string, bool>(fact.Fact.AssetGuid, fact.Not));
                    continue;
                }
                // Review R6-F2: a buff predicate keeps its own key, so the
                // live read uses the native buff check (Buffs.HasFact).
                var buff = condition as ContextConditionHasBuff;
                if (buff != null && buff.GetType() == typeof(ContextConditionHasBuff) && buff.Buff != null &&
                    !string.IsNullOrWhiteSpace(buff.Buff.AssetGuid))
                {
                    facts.Add(new KeyValuePair<string, bool>(RecipientFactGate.BuffPredicate(buff.Buff.AssetGuid),
                        buff.Not));
                    continue;
                }
                // Review R6-F1: the recipient's relation to the caster (Prayer
                // routes a friendly recipient past ContextConditionIsEnemy to
                // its buff), read live with the native IsEnemy / IsAlly.
                if (condition != null && condition.GetType() == typeof(ContextConditionIsEnemy))
                {
                    facts.Add(new KeyValuePair<string, bool>(RecipientFactGate.EnemyOfCaster, condition.Not));
                    continue;
                }
                if (condition != null && condition.GetType() == typeof(ContextConditionIsAlly))
                {
                    facts.Add(new KeyValuePair<string, bool>(RecipientFactGate.AllyOfCaster, condition.Not));
                    continue;
                }
                return null;
            }
            string operation = checker.Operation.ToString();
            return operation == "And" || operation == "Or" ? new RecipientFactGate(operation == "Or", facts) : null;
        }

        private static string DescribeConditions(Conditional conditional)
        {
            if (conditional.ConditionsChecker == null || conditional.ConditionsChecker.Conditions == null)
                return string.Empty;
            return conditional.ConditionsChecker.Operation + ":" + string.Join(",",
                conditional.ConditionsChecker.Conditions.Select(c => c == null ? "null" : c.GetType().FullName).ToArray());
        }

        private static string DescribeType(Type type)
        {
            return type.FullName + ", " + type.Assembly.GetName().Name +
                ", Version=" + type.Assembly.GetName().Version;
        }
    }

    // Missing-beneficial-buffs review R6-F1/R6-F2: the live read of one
    // recipient-fact gate key, with the exact check the native predicate
    // makes (observed in the 2.1.7b IL, pinned by
    // missing-beneficial-buffs-native-condition-contracts-pinned):
    // ContextConditionHasFact -> UnitHelper.HasFact(descriptor, fact);
    // ContextConditionHasBuff -> descriptor.Buffs.HasFact(buff);
    // ContextConditionIsEnemy / IsAlly -> target.IsEnemy / IsAlly(caster).
    // Null when the read cannot be made (unknown, never false by default).
    internal static class NativeRecipientFactRead
    {
        internal static bool? Read(Kingmaker.EntitySystem.Entities.UnitEntityData unit,
            Kingmaker.EntitySystem.Entities.UnitEntityData caster, string factKey)
        {
            try
            {
                if (unit == null || string.IsNullOrEmpty(factKey)) return null;
                if (factKey == RecipientFactGate.EnemyOfCaster)
                    return caster == null ? (bool?)null : unit.IsEnemy(caster);
                if (factKey == RecipientFactGate.AllyOfCaster)
                    return caster == null ? (bool?)null : unit.IsAlly(caster);
                if (unit.Descriptor == null) return null;
                string buffId;
                if (RecipientFactGate.TryBuffPredicate(factKey, out buffId))
                {
                    var buff = ResourcesLibrary.TryGetBlueprint<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(buffId);
                    return buff == null ? (bool?)null : unit.Descriptor.Buffs.HasFact(buff);
                }
                var fact = ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Facts.BlueprintUnitFact>(factKey);
                return fact == null ? (bool?)null : Kingmaker.UnitLogic.UnitHelper.HasFact(unit.Descriptor, fact);
            }
            catch (Exception) { return null; }
        }

        // Review R4-F3: a key no buff casting can change - a relation to the
        // caster, or a unit fact that is not a buff (a creature type).
        internal static bool IsFixed(string factKey)
        {
            if (RecipientFactGate.IsRelation(factKey)) return true;
            string buffId;
            if (RecipientFactGate.TryBuffPredicate(factKey, out buffId)) return false;
            try
            {
                var fact = ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Facts.BlueprintUnitFact>(factKey);
                return fact != null && !(fact is Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff);
            }
            catch (Exception) { return false; }
        }
    }
}
