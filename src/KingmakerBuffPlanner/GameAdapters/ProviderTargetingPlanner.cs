using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Which units one provider may reach, which units are legal anchors, and
    // whom each anchor's cast is predicted to reach.
    internal sealed class ProviderTargetingDecision
    {
        internal string[] ReachableIds;
        internal string[] Anchors;
        internal Dictionary<string, IEnumerable<string>> RecipientIdsByAnchor;
        // Every unit an anchor's native actions reach (review R3-F1).
        internal Dictionary<string, IEnumerable<string>> AffectedIdsByAnchor =
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
        // The affected units whose distance from the centre was not read
        // (recovery R9-F5): possibly reached, never covered.
        internal Dictionary<string, IEnumerable<string>> UncertainIdsByAnchor =
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
        // unit -> fixed recipient fact -> read value (review R4-F3).
        internal Dictionary<string, Dictionary<string, bool>> FixedRecipientFacts =
            new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
        // unit -> every recipient-fact gate key -> read value (review R7-F6).
        internal Dictionary<string, Dictionary<string, bool>> RecipientFactReads =
            new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
        internal bool Party;
        internal bool AreaRecipients;
        internal bool UnsafeArea;
        // Review R10-F1: the native selector admits only allies of the caster.
        internal bool RequiresAlly;
        internal bool InfusedPersonal;
        internal string TargetClass;
    }

    // The game-free targeting decisions of KingmakerProviderOptionBuilder
    // (review R1-F8). The builder supplies two live reads: the native
    // AbilityData.CanTarget verdict for a unit (false when either side is
    // unreadable) and the distance between two live units (null when
    // unreadable). Offline tests inject both, so the anchor, fallback and
    // recipient decisions they assert are these production decisions.
    // A point-capable allied burst (missing-beneficial-buffs §3.C) is an
    // AlliedAreaRecipients effect: its anchors are exactly the units native
    // CanTarget admits - with NO caster fallback - so a game that refuses a
    // unit centre leaves no anchor and the planner refuses the casting.
    internal static class ProviderTargetingPlanner
    {
        internal static ProviderTargetingDecision Decide(
            ProviderSnapshot provider,
            EffectExpression expression,
            IEnumerable<UnitSnapshot> snapshotUnits,
            bool casterReadable,
            bool infusedPersonal,
            AlliedAreaCoverage areaCoverage,
            Func<string, bool> canTarget,
            Func<string, string, float?> distance,
            IReadOnlyDictionary<string, bool> casterFacts = null,
            Func<string, string, bool?> unitHasFact = null,
            Func<string, bool> isFixedFact = null)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            Func<string, bool> legal = canTarget ?? (id => false);
            Func<string, string, float?> raw = distance ?? ((a, b) => null);
            // Review R9 (R8-F6): a failed live distance read is unavailable
            // for that pair only; it never interrupts the other centres.
            // Review R10 (R8-F6): a non-finite or negative reading is unusable
            // geometry, the same as no reading.
            Func<string, string, float?> measure = (a, b) =>
            {
                float? value;
                try { value = raw(a, b); }
                catch (Exception) { return null; }
                return value.HasValue && !float.IsNaN(value.Value) && !float.IsInfinity(value.Value) &&
                    value.Value >= 0f ? value : null;
            };
            UnitSnapshot[] units = (snapshotUnits ?? new UnitSnapshot[0]).Where(u => u.TargetValidation.Alive &&
                u.TargetValidation.Friendly && u.TargetValidation.Targetable).ToArray();
            string casterId = provider.Key.CasterUnitId;
            var decision = new ProviderTargetingDecision
            {
                RecipientIdsByAnchor = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                Party = EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Party),
                AreaRecipients = EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.AlliedAreaRecipients),
                UnsafeArea = EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.EnemyAreaRecipients) ||
                    EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.AmbiguousAreaRecipients),
                InfusedPersonal = infusedPersonal
            };
            // Greater Magic Fang repair (review): a caster-level tier
            // selection this provider's caster facts leave unresolved (its
            // caster level was unreadable) cannot say what the cast applies,
            // so it could never be confirmed: no target is offered and
            // nothing is reserved.
            if (CasterLevelPredicate.HasUnresolved(EffectConditionBinding.Bind(expression, casterFacts)))
            {
                decision.ReachableIds = new string[0];
                decision.Anchors = new string[0];
                decision.TargetClass = "CasterLevelUnread";
                return decision;
            }
            IEnumerable<UnitSnapshot> reachable;
            string[] anchors;
            bool personal = EffectExpressionTargetAnalysis.ContainsOnly(expression, EffectTarget.Caster);
            if (personal && !infusedPersonal)
            {
                // Review R11-F4: the owner is a legal anchor only when the
                // live caster is readable and native CanTarget admits it (a
                // lost live ability or a refused owner target offers nothing
                // to reserve).
                reachable = units.Where(u => u.UnitId == casterId && casterReadable && legal(u.UnitId));
                anchors = reachable.Select(u => u.UnitId).ToArray();
            }
            // Re-review: a party effect that also reaches pets is planned as
            // a party effect (every unit, pets included, from a legal
            // origin); only a pet-only effect is limited to the pet.
            else if (decision.Party)
            {
                reachable = units;
                anchors = LegalAnchorIds(units, legal);
                if (anchors.Length == 0 && units.Any(unit => unit.UnitId == casterId))
                    anchors = new[] { casterId };
                foreach (string anchor in anchors)
                    decision.RecipientIdsByAnchor.Add(anchor, units.Select(unit => unit.UnitId).ToArray());
            }
            else if (EffectExpressionTargetAnalysis.Contains(expression, EffectTarget.Pet))
            {
                reachable = units.Where(u => u.IsPet && u.MasterUnitId == casterId);
                anchors = reachable.Select(u => u.UnitId).ToArray();
            }
            // Review R11-F5: a non-finite or non-positive radius is no usable
            // disk; no centre is offered.
            else if (decision.AreaRecipients && areaCoverage != null && casterReadable &&
                !float.IsNaN(areaCoverage.Radius) && !float.IsInfinity(areaCoverage.Radius) && areaCoverage.Radius > 0f)
            {
                float radius = areaCoverage.Radius;
                // Review R10-F1: an explicit native Ally selector
                // (AbilityTargetsAround TargetType.Ally) selects only units
                // allied to the caster, independently of the anchor's
                // CanTarget. A unit read as not allied is not reached at all; a
                // unit whose relation is unread may be reached (affected) but
                // gets no coverage claim.
                bool requiresAlly = areaCoverage.RequiresAlly;
                decision.RequiresAlly = requiresAlly;
                Func<string, bool?> allyRead = unitId => unitHasFact == null ? null
                    : unitHasFact(unitId, RecipientFactGate.AllyOfCaster);
                // Review R8-F6: a centre whose own position cannot be read has
                // no measurable burst; it is not offered as a legal anchor (it
                // could otherwise inherit coverage from other anchors).
                // Review R9 (R8-F6): the legal anchors are exactly the centres
                // whose coverage was completely built below (one self read
                // per centre, at the moment its disk is measured).
                var completed = new List<string>();
                // Review R2-F5: inside the burst is not the same as eligible.
                // Recipient-fact conditions (Mass Blessing of Luck and Resolve
                // skips constructs, and undead unless the caster has Undead
                // Arcana) are resolved per unit from the caster's facts and
                // the unit's own facts; a unit whose bound payload applies
                // nothing is not covered, and one whose facts cannot be read
                // gets no definite coverage claim.
                EffectExpression casterBound = EffectConditionBinding.Bind(expression, casterFacts);
                bool gated = EffectRecipientFactBinding.HasGates(casterBound);
                // Review R4-F5: with no recipient condition, a known-false
                // caster gate that removed the only application, or an unread
                // caster gate still controlling it, yields no beneficiaries.
                // Review R5-F3: an application behind an unmodelled
                // conditional (one native checker mixing a caster and a
                // recipient fact) is not unconditional either.
                bool casterPayload = EffectExpressionAnalysis.ContainsDefiniteLeaf(casterBound) &&
                    !EffectExpressionAnalysis.HasCasterGate(casterBound);
                Func<string, bool?> noFacts = id => null;
                Func<UnitSnapshot, bool> eligible = unit =>
                {
                    if (requiresAlly && allyRead(unit.UnitId) != true) return false;
                    if (!gated) return casterPayload;
                    string unitId = unit.UnitId;
                    bool unresolved;
                    EffectExpression bound = EffectRecipientFactBinding.Bind(casterBound,
                        unitHasFact == null ? noFacts : fact => unitHasFact(unitId, fact), out unresolved);
                    // Review R2 (R2-F5): a caster predicate left unresolved on
                    // the recipient's own branch (an undead ally when the
                    // caster's Undead Arcana is unread) decides whether it
                    // receives anything: no definite coverage. A branch that
                    // never reads it (a living ally) stays covered.
                    // Review R9-F3: a unit the relation predicate reads as
                    // hostile to the caster gets the enemy branch (Prayer's
                    // penalty): affected, not a beneficiary.
                    return !unresolved && EffectExpressionAnalysis.ContainsDefiniteLeaf(bound) &&
                        !EffectExpressionAnalysis.HasCasterGate(bound) &&
                        !EffectRecipientFactBinding.HostileOnPath(casterBound,
                            unitHasFact == null ? noFacts : fact => unitHasFact(unitId, fact));
                };
                foreach (string anchorId in LegalAnchorIds(units, legal))
                {
                    // The centre's own position is read once; that read places
                    // it inside its own burst. Another unit is inside only on a
                    // successful read of its own distance from this centre.
                    if (!measure(anchorId, anchorId).HasValue) continue;
                    completed.Add(anchorId);
                    string id = anchorId;
                    // Recovery (R9-F5): a unit whose distance from this centre
                    // cannot be read is not proven outside the disk either; it
                    // is possibly reached (affected, never a beneficiary).
                    var unmeasured = new List<string>();
                    UnitSnapshot[] inside = units.Where(unit =>
                    {
                        if (requiresAlly && allyRead(unit.UnitId) == false) return false;
                        if (unit.UnitId == id) return true;
                        // Review R11-F5: the recorded radius exactly (no
                        // margin without native evidence for one).
                        float? metres = measure(id, unit.UnitId);
                        if (!metres.HasValue) unmeasured.Add(unit.UnitId);
                        return metres.HasValue && metres.Value <= radius;
                    }).ToArray();
                    if (requiresAlly)
                        foreach (UnitSnapshot unit in inside)
                        {
                            bool? ally = allyRead(unit.UnitId);
                            if (!ally.HasValue) continue;
                            Dictionary<string, bool> reads, known;
                            if (!decision.RecipientFactReads.TryGetValue(unit.UnitId, out reads))
                                decision.RecipientFactReads[unit.UnitId] = reads =
                                    new Dictionary<string, bool>(StringComparer.Ordinal);
                            reads[RecipientFactGate.AllyOfCaster] = ally.Value;
                            if (isFixedFact == null || !isFixedFact(RecipientFactGate.AllyOfCaster)) continue;
                            if (!decision.FixedRecipientFacts.TryGetValue(unit.UnitId, out known))
                                decision.FixedRecipientFacts[unit.UnitId] = known =
                                    new Dictionary<string, bool>(StringComparer.Ordinal);
                            known[RecipientFactGate.AllyOfCaster] = ally.Value;
                        }
                    // Review R3-F1: every unit inside the burst is affected
                    // by the native actions (an ally that only loses an effect
                    // included); only the eligible ones are beneficiaries.
                    decision.AffectedIdsByAnchor.Add(anchorId,
                        inside.Select(unit => unit.UnitId).Concat(unmeasured).ToArray());
                    if (unmeasured.Count != 0) decision.UncertainIdsByAnchor.Add(anchorId, unmeasured.ToArray());
                    decision.RecipientIdsByAnchor.Add(anchorId, inside.Where(eligible)
                        .Select(unit => unit.UnitId).ToArray());
                    // Review R4-F3: the FIXED recipient facts read for these
                    // units (creature types; never a buff a casting can add
                    // or remove) go with the option, so conflict analysis can
                    // tell a unit the cast does nothing to (an animal under
                    // Enlarge/Reduce Person, Mass) from one it may change.
                    // Review R7-F6: and every read (fixed or not), the starting
                    // state of a sequential forecast.
                    if (gated && unitHasFact != null)
                        foreach (UnitSnapshot unit in inside)
                        {
                            Dictionary<string, bool> known, reads;
                            if (!decision.RecipientFactReads.TryGetValue(unit.UnitId, out reads))
                                decision.RecipientFactReads[unit.UnitId] = reads =
                                    new Dictionary<string, bool>(StringComparer.Ordinal);
                            foreach (string fact in EffectRecipientFactBinding.FactIds(casterBound))
                            {
                                bool? has = unitHasFact(unit.UnitId, fact);
                                if (!has.HasValue) continue;
                                reads[fact] = has.Value;
                                if (isFixedFact == null || !isFixedFact(fact)) continue;
                                if (!decision.FixedRecipientFacts.TryGetValue(unit.UnitId, out known))
                                    decision.FixedRecipientFacts[unit.UnitId] = known =
                                        new Dictionary<string, bool>(StringComparer.Ordinal);
                                known[fact] = has.Value;
                            }
                        }
                }
                anchors = completed.ToArray();
                reachable = units.Where(unit => decision.RecipientIdsByAnchor.Values
                    .Any(recipients => recipients.Contains(unit.UnitId)));
            }
            else if (decision.AreaRecipients || decision.UnsafeArea)
            {
                reachable = new UnitSnapshot[0];
                anchors = new string[0];
            }
            else
            {
                reachable = units.Where(u => legal(u.UnitId));
                anchors = reachable.Select(u => u.UnitId).ToArray();
            }
            decision.ReachableIds = reachable.Select(u => u.UnitId).Distinct(StringComparer.Ordinal).ToArray();
            decision.Anchors = anchors;
            decision.TargetClass = decision.Party ? "Party" : decision.AreaRecipients
                ? "AlliedAreaRecipients" : decision.UnsafeArea
                    ? "UnsafeArea" : infusedPersonal
                        ? "NativeAlchemistInfusion" : personal ? "Caster" : "Direct";
            return decision;
        }

        // The caster facts one provider's expression gates on, as the option
        // builder reads them: a caster-level predicate (Greater Magic Fang's
        // tier) is answered from the provider's effective caster level and
        // left unread when that level is unknown (0); every other key is the
        // native read (null: unread, never false).
        internal static IReadOnlyDictionary<string, bool> CasterFacts(EffectExpression expression,
            int effectiveCasterLevel, Func<string, bool?> nativeFact)
        {
            var facts = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (string id in EffectConditionBinding.FactIds(expression))
            {
                int level;
                bool? value = CasterLevelPredicate.TryParse(id, out level)
                    ? CasterLevelPredicate.Read(id, effectiveCasterLevel)
                    : nativeFact == null ? null : nativeFact(id);
                if (value.HasValue) facts[id] = value.Value;
            }
            return facts;
        }

        // The planning option for one provider and its targeting decision.
        internal static ProviderPlanningOption BuildOption(ProviderSnapshot provider,
            ProviderTargetingDecision decision, CastExecutionStrategy strategy, string strategyReason,
            IReadOnlyDictionary<string, bool> casterFacts = null,
            IReadOnlyDictionary<string, EffectExpression> effectsByTarget = null)
        {
            return new ProviderPlanningOption(provider, decision.ReachableIds, decision.Anchors,
                provider.EffectiveCasterLevel, provider.ExpectedDurationRounds, strategy, strategyReason,
                decision.RecipientIdsByAnchor, casterFacts, effectsByTarget, decision.AffectedIdsByAnchor,
                decision.FixedRecipientFacts.ToDictionary(pair => pair.Key,
                    pair => (IReadOnlyDictionary<string, bool>)pair.Value, StringComparer.Ordinal),
                decision.RecipientFactReads.ToDictionary(pair => pair.Key,
                    pair => (IReadOnlyDictionary<string, bool>)pair.Value, StringComparer.Ordinal),
                decision.RequiresAlly, decision.UncertainIdsByAnchor);
        }

        private static string[] LegalAnchorIds(IEnumerable<UnitSnapshot> units, Func<string, bool> canTarget)
        {
            return (units ?? new UnitSnapshot[0]).Where(unit => canTarget(unit.UnitId))
                .Select(unit => unit.UnitId).Distinct(StringComparer.Ordinal)
                .OrderBy(unitId => unitId, StringComparer.Ordinal).ToArray();
        }
    }
}
