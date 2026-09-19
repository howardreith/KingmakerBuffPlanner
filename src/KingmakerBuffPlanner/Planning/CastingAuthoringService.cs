using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;

namespace KingmakerBuffPlanner.Planning
{
    // One authoring command outcome. AffectedCastingIds states the exact edit
    // scope: every persisted casting whose representation changed, so batch
    // consequences such as order shifts after a move are disclosed, never
    // silent.
    public sealed class AuthoringEditResult
    {
        internal AuthoringEditResult(
            bool applied, string reason, string scope, IEnumerable<string> affectedCastingIds)
        {
            Applied = applied;
            Reason = reason ?? string.Empty;
            Scope = scope ?? string.Empty;
            AffectedCastingIds = new ReadOnlyCollection<string>(
                (affectedCastingIds ?? new string[0]).Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        public bool Applied { get; private set; }
        public string Reason { get; private set; }
        public string Scope { get; private set; }
        public IReadOnlyList<string> AffectedCastingIds { get; private set; }

        internal static AuthoringEditResult Accept(
            string scope, IEnumerable<string> affectedCastingIds)
        {
            return new AuthoringEditResult(true, string.Empty, scope, affectedCastingIds);
        }

        internal static AuthoringEditResult Refuse(string reason)
        {
            return new AuthoringEditResult(false, reason, string.Empty, new string[0]);
        }
    }

    // The one mutation authority over a CastingPlanDocument. Browsing,
    // reading, and compiling never route through this service; only explicit
    // editing commands do, and every accepted command is undoable.
    public sealed class CastingAuthoringService
    {
        private const int HistoryLimit = 64;

        private readonly Stack<CastingPlanDocument> _history =
            new Stack<CastingPlanDocument>();
        private CastingPlanDocument _document;

        public CastingAuthoringService(CastingPlanDocument document)
        {
            _document = document ?? throw new ArgumentNullException("document");
        }

        public CastingPlanDocument Document { get { return _document; } }

        public bool CanUndo { get { return _history.Count != 0; } }

        // Appends a casting at the end of its routine. The casting ID is the
        // player-visible stable identity; colliding IDs are refused, never
        // silently renumbered.
        public AuthoringEditResult AddCasting(PlannedCasting casting)
        {
            if (casting == null) return AuthoringEditResult.Refuse("casting-null");
            if (_document.Routines.All(value => value.RoutineId != casting.RoutineId))
                return AuthoringEditResult.Refuse("routine-unknown:" + casting.RoutineId);
            if (_document.Castings.Any(value => value.CastingId == casting.CastingId))
                return AuthoringEditResult.Refuse("casting-id-collision:" + casting.CastingId);
            var castings = new List<PlannedCasting>(_document.Castings);
            // Persisted order is routine declaration order then position;
            // appending means after the last casting of that routine, keeping
            // the list grouped by routine.
            int position = castings.Count(
                value => value.RoutineId == casting.RoutineId);
            int insertAt = InsertionIndex(castings, casting.RoutineId, position);
            castings.Insert(insertAt, WithOrder(casting, position));
            return Commit("add-casting:" + casting.CastingId,
                new[] { casting.CastingId }, castings);
        }

        // Replaces one casting in place (same routine, same position). Use
        // MoveCasting to change routine membership or position; there is no
        // combined edit that can smuggle a routine move into a content edit.
        public AuthoringEditResult UpdateCasting(PlannedCasting replacement)
        {
            if (replacement == null) return AuthoringEditResult.Refuse("casting-null");
            int index = IndexOf(replacement.CastingId);
            if (index < 0)
                return AuthoringEditResult.Refuse("casting-unknown:" + replacement.CastingId);
            PlannedCasting current = _document.Castings[index];
            if (replacement.RoutineId != current.RoutineId)
                return AuthoringEditResult.Refuse(
                    "routine-change-requires-move:" + current.RoutineId + ">" + replacement.RoutineId);
            var castings = new List<PlannedCasting>(_document.Castings);
            castings[index] = WithOrder(replacement, current.Order);
            return Commit("update-casting:" + replacement.CastingId,
                new[] { replacement.CastingId }, castings);
        }

        public AuthoringEditResult RemoveCasting(string castingId)
        {
            int index = IndexOf(castingId);
            if (index < 0) return AuthoringEditResult.Refuse("casting-unknown:" + castingId);
            var before = _document.Castings;
            var castings = new List<PlannedCasting>(before);
            castings.RemoveAt(index);
            castings = NormalizeAll(castings);
            return Commit("remove-casting:" + castingId,
                new[] { castingId }.Concat(ShiftedIds(before, castings)), castings);
        }

        // Moves one casting to an explicit routine and position. Sibling
        // order shifts in the affected routines are disclosed in the result.
        public AuthoringEditResult MoveCasting(
            string castingId, string targetRoutineId, int targetPosition)
        {
            int index = IndexOf(castingId);
            if (index < 0) return AuthoringEditResult.Refuse("casting-unknown:" + castingId);
            if (_document.Routines.All(value => value.RoutineId != targetRoutineId))
                return AuthoringEditResult.Refuse("routine-unknown:" + targetRoutineId);
            string sourceRoutineId = _document.Castings[index].RoutineId;
            int sourceCount = CountInRoutine(sourceRoutineId);
            int targetCount = CountInRoutine(targetRoutineId);
            if (string.Equals(sourceRoutineId, targetRoutineId, StringComparison.Ordinal))
                targetCount--;
            if (targetPosition < 0 || targetPosition > targetCount)
                return AuthoringEditResult.Refuse(
                    "position-out-of-range:" + targetPosition + ">" + targetCount);
            var before = _document.Castings;
            var castings = new List<PlannedCasting>(before);
            // Routine membership changes with the move; the persisted order
            // change alone would silently keep the old routine assignment.
            PlannedCasting moving = WithRoutine(castings[index], targetRoutineId);
            castings.RemoveAt(index);
            int insertAt = InsertionIndex(castings, targetRoutineId, targetPosition);
            castings.Insert(insertAt, moving);
            castings = NormalizeAll(castings);
            return Commit("move-casting:" + castingId,
                new[] { castingId }.Concat(ShiftedIds(before, castings)), castings);
        }

        public AuthoringEditResult SetCastingState(string castingId, CastingAuthoringState state)
        {
            int index = IndexOf(castingId);
            if (index < 0) return AuthoringEditResult.Refuse("casting-unknown:" + castingId);
            PlannedCasting current = _document.Castings[index];
            if (current.State == state)
                return AuthoringEditResult.Refuse("state-unchanged:" + castingId);
            if (state == CastingAuthoringState.Ready && current.CasterUnitId == null)
                return AuthoringEditResult.Refuse("ready-requires-caster:" + castingId);
            var replacement = new PlannedCasting(
                current.CastingId, current.RoutineId, current.Order, current.SourceId,
                current.Ability, current.CasterUnitId, current.SpellbookGuid,
                current.TargetMode, current.DirectTargetUnitId, current.Origin,
                current.RequiredCoverageUnitIds, current.TargetingModifiers,
                current.Enhancements, current.ExistingEffectPolicy,
                current.IgnoredPresenceMarkers, state, current.Provenance);
            var castings = new List<PlannedCasting>(_document.Castings);
            castings[index] = replacement;
            return Commit("set-state:" + castingId, new[] { castingId }, castings);
        }

        // Restores the document as it was before the most recent accepted
        // command. Refused commands never enter history, so an undo always
        // undoes an actual edit.
        public bool Undo()
        {
            if (_history.Count == 0) return false;
            _document = _history.Pop();
            return true;
        }

        private AuthoringEditResult Commit(
            string scope, IEnumerable<string> affected, List<PlannedCasting> castings)
        {
            var replacement = new CastingPlanDocument(
                _document.CampaignId, _document.Routines, castings);
            _history.Push(_document);
            if (_history.Count > HistoryLimit)
            {
                var retained = new List<CastingPlanDocument>();
                while (_history.Count > 0) retained.Add(_history.Pop());
                retained.RemoveRange(0, retained.Count - HistoryLimit);
                foreach (CastingPlanDocument document in retained) _history.Push(document);
            }
            _document = replacement;
            return AuthoringEditResult.Accept(scope, affected);
        }

        private int IndexOf(string castingId)
        {
            if (string.IsNullOrWhiteSpace(castingId)) return -1;
            for (int i = 0; i < _document.Castings.Count; i++)
                if (string.Equals(_document.Castings[i].CastingId, castingId, StringComparison.Ordinal))
                    return i;
            return -1;
        }

        private int CountInRoutine(string routineId)
        {
            return _document.Castings.Count(
                value => string.Equals(value.RoutineId, routineId, StringComparison.Ordinal));
        }

        // Insertion point for a position inside one routine in the persisted
        // global order: before the next same-routine casting after
        // targetPosition siblings, or before the first casting of a routine
        // declared after the target routine when the target has no further
        // castings (keeping the list grouped by routine declaration order).
        private int InsertionIndex(
            List<PlannedCasting> castings, string routineId, int targetPosition)
        {
            var rank = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < _document.Routines.Count; i++)
                rank[_document.Routines[i].RoutineId] = i;
            int targetRank = rank[routineId];
            int seen = 0;
            for (int i = 0; i < castings.Count; i++)
            {
                if (string.Equals(castings[i].RoutineId, routineId, StringComparison.Ordinal))
                {
                    if (seen == targetPosition) return i;
                    seen++;
                    continue;
                }
                if (rank[castings[i].RoutineId] > targetRank && seen >= targetPosition)
                    return i;
            }
            return castings.Count;
        }

        // Any casting whose routine or order pair changed is part of the
        // disclosed edit scope.
        private static IEnumerable<string> ShiftedIds(
            IReadOnlyList<PlannedCasting> before, List<PlannedCasting> after)
        {
            var beforePlacement = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (PlannedCasting casting in before)
                beforePlacement[casting.CastingId] = casting.RoutineId + "#" + casting.Order;
            return after.Where(casting =>
                beforePlacement[casting.CastingId] != casting.RoutineId + "#" + casting.Order)
                .Select(casting => casting.CastingId);
        }

        // Re-derives every casting's order from its persisted position; the
        // caller passes castings already in final intended order.
        private static List<PlannedCasting> NormalizeAll(List<PlannedCasting> castings)
        {
            var positionInRoutine = new Dictionary<string, int>(StringComparer.Ordinal);
            var result = new List<PlannedCasting>(castings.Count);
            foreach (PlannedCasting casting in castings)
            {
                int prior;
                int position = positionInRoutine.TryGetValue(casting.RoutineId, out prior)
                    ? prior + 1 : 0;
                positionInRoutine[casting.RoutineId] = position;
                result.Add(WithOrder(casting, position));
            }
            return result;
        }

        private static PlannedCasting WithOrder(PlannedCasting casting, int order)
        {
            return PlannedCasting.WithOrder(casting, order);
        }

        private static PlannedCasting WithRoutine(PlannedCasting casting, string routineId)
        {
            if (string.Equals(casting.RoutineId, routineId, StringComparison.Ordinal))
                return casting;
            return new PlannedCasting(
                casting.CastingId, routineId, casting.Order, casting.SourceId,
                casting.Ability, casting.CasterUnitId, casting.SpellbookGuid,
                casting.TargetMode, casting.DirectTargetUnitId, casting.Origin,
                casting.RequiredCoverageUnitIds, casting.TargetingModifiers,
                casting.Enhancements, casting.ExistingEffectPolicy,
                casting.IgnoredPresenceMarkers, casting.State, casting.Provenance);
        }
    }
}
