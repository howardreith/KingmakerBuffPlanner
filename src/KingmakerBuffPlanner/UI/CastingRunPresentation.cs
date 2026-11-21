using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    // One policy resolver shared by the HUD entry and guarded qualification.
    // It chooses only the requested/remembered gate mode; compilation,
    // validation, projection and dispatch remain owned by the session.
    internal static class CastingRunPolicy
    {
        internal static CastingApplyMode Resolve(CastingWorkspaceSession session,
            CastingApplyMode? requestedMode)
        {
            if (session == null) throw new ArgumentNullException("session");
            return requestedMode ?? session.ApplyMode;
        }

        internal static WorkspaceApplyResult Apply(CastingWorkspaceSession session,
            CastingApplyMode? requestedMode, string routineId,
            CastingWorkspaceInputs inputs)
        {
            return session.Apply(Resolve(session, requestedMode), routineId, inputs);
        }
    }

    // Player-facing text for casting-first runs and refusals. Effects and
    // resource spending are stated separately; nothing claims more than
    // the run reported, and nothing is described as rolled back.
    internal static class CastingRunPresentation
    {
        internal static QuickExecutionResult ToQuickResult(CastingRunReport report,
            string routineName, Func<string, string> castingLabel)
        {
            if (report == null) throw new ArgumentNullException("report");
            return new QuickExecutionResult(report.ScopeRoutineId, routineName,
                report.Succeeded ? QuickExecutionDisposition.Completed
                    : QuickExecutionDisposition.Failed,
                Describe(report, routineName, castingLabel),
                report.Planned, report.Submitted, report.Confirmed);
        }

        internal static string Describe(CastingRunReport report, string routineName,
            Func<string, string> castingLabel)
        {
            Func<string, string> label = castingLabel ?? (id => id);
            int disabled = report.Entries.Count(IsDisabled);
            var facts = new List<string>
            {
                PlannerText.Get(report.Entries.Count == 1 ? "run.confirmed.one" : "run.confirmed.many",
                    report.Confirmed, report.Entries.Count)
            };
            AddCount(facts, "run.count.active", report.Skipped);
            AddCount(facts, "run.count.unavailable", report.Omitted - disabled);
            AddCount(facts, "run.count.disabled", disabled);
            CastingOutcomeEntry failed = report.Entries.FirstOrDefault(entry => entry.State == CastingOutcomeState.Failed);
            if (failed != null)
                facts.Add(PlannerText.Get("run.failure.detail", label(failed.CastingId), ShortDetail(failed.Detail)));
            AddCount(facts, "run.count.interrupted", report.CancelledCastings);
            AddCount(facts, "run.count.unattempted", report.NotProcessed);
            string detail = string.Join(PlannerText.Get("run.join.detail"), facts.ToArray());
            string summary = report.Cancelled
                ? PlannerText.Get("run.summary.stopped", routineName, StopReason(report.TerminalReason), detail)
                : report.Halted ? PlannerText.Get("run.summary.halted", routineName, detail)
                : PlannerText.Get("run.summary.normal", routineName, detail);
            int free = report.Entries.Count(entry => entry.FreeCast);
            var observations = new List<string> { free == 0
                ? PlannerText.Get("run.spent", report.ResourcesSpent)
                : PlannerText.Get("run.spent.free", report.ResourcesSpent, free) };
            AddCount(observations, "run.unvalidated.effects", report.Entries.Count(e => e.EffectObserved && !e.EffectConfirmed));
            string message = PlannerText.Get("run.summary.observations", summary, string.Join(PlannerText.Get("run.join.detail"), observations.ToArray()));
            return report.CleanupFailures.Count == 0 ? message : PlannerText.Get("run.summary.cleanup", message, string.Join(PlannerText.Get("run.join.detail"), report.CleanupFailures.ToArray()));
        }


        // The visible summary counts the full scoped outcome. Named detail
        // lines follow for every uncast entry; native successes are not replayed.
        internal static string GameLogLine(CastingRunReport report, string routineName)
        {
            if (report == null) throw new ArgumentNullException("report");
            int disabled = report.Entries.Count(IsDisabled);
            var counts = new List<string> { PlannerText.Get("run.count.confirmed", report.Confirmed) };
            AddCount(counts, "run.count.active", report.Skipped);
            AddCount(counts, "run.count.unavailable", report.Omitted - disabled);
            AddCount(counts, "run.count.disabled", disabled);
            AddCount(counts, "run.count.failed", report.Failed);
            AddCount(counts, "run.count.interrupted", report.CancelledCastings);
            AddCount(counts, "run.count.unattempted", report.NotProcessed);
            string key = report.Cancelled ? "run.log.stopped"
                : report.Halted || report.Failed != 0 ? "run.log.failed"
                : report.Omitted > disabled ? "run.log.omitted" : "run.log.done";
            return PlannerText.Get(key, routineName, string.Join(PlannerText.Get("run.join.count"), counts.ToArray()));
        }

        private static void AddCount(ICollection<string> labels, string key, int count)
        {
            if (count != 0) labels.Add(PlannerText.Get(key, count));
        }

        private static bool IsDisabled(CastingOutcomeEntry entry)
        {
            return entry.State == CastingOutcomeState.Omitted &&
                entry.OmissionReadiness == ResolvedCastingReadiness.Disabled;
        }

        // Visible details are separate native log entries, never tooltip-only.
        // No cap: diagnostic evidence limits must not hide planned omissions.
        internal static IReadOnlyList<string> GameLogLines(CastingRunReport report, string name,
            Func<string, string> label, Func<string, string, string> reason = null)
        {
            reason = reason ?? ((id, code) => WorkspaceReasonText.Describe(code));
            var lines = new List<string> { GameLogLine(report, name) };
            foreach (CastingOutcomeEntry entry in report.Entries)
            {
                if (entry.State == CastingOutcomeState.EffectConfirmed) continue;
                string why = entry.State == CastingOutcomeState.Skipped ? PlannerText.Get("run.reason.active")
                    : IsDisabled(entry) ? PlannerText.Get("run.reason.disabled")
                    : entry.State == CastingOutcomeState.Omitted
                        ? string.Join(PlannerText.Get("run.join.detail"), entry.OmissionReasons.Select(code => reason(entry.CastingId, code)).ToArray())
                        : DescribeEntry(entry);
                if (string.IsNullOrEmpty(why)) why = WorkspaceReasonText.Describe(entry.Detail);
                lines.Add(PlannerText.Get("run.log.detail", label(entry.CastingId), why));
            }
            return lines.AsReadOnly();
        }

        internal static IReadOnlyList<string> AdmissionLogLines(string name, WorkspaceApplyResult result,
            IEnumerable<string> orderedIds, Func<string, string> label, Func<string, string, string> reason = null)
        {
            reason = reason ?? ((id, code) => WorkspaceReasonText.Describe(code));
            CastingApplyDecision gate = result == null ? null : result.GateDecision;
            if (gate == null) return new[] { GameLogRefusal(DescribeRefusal(name, result)) };
            int active = gate.Omissions.Count(o => o.Readiness == ResolvedCastingReadiness.AlreadySatisfied);
            int disabled = gate.Omissions.Count(o => o.Readiness == ResolvedCastingReadiness.Disabled);
            int unavailable = gate.Omissions.Count - active - disabled;
            bool strict = !gate.Allowed && gate.BlockingCastings.Count != 0;
            bool empty = gate.ExecutableCastingIds.Count + gate.Omissions.Count == 0;
            var counts = new List<string> { PlannerText.Get("run.count.confirmed", 0) };
            AddCount(counts, "run.count.unavailable", unavailable);
            AddCount(counts, "run.count.active", active);
            AddCount(counts, "run.count.disabled", disabled);
            if (strict) counts.Add(PlannerText.Get("run.count.withheld", gate.ExecutableCastingIds.Count));
            string detail = string.Join(PlannerText.Get("run.join.count"), counts.ToArray());
            string summary;
            if (!strict && gate.ExecutableCastingIds.Count != 0)
                summary = PlannerText.Get("run.admission.refused", name, detail, DescribeRefusal(name, result).TrimEnd('.', '。'));
            else
            {
                string key = empty ? "run.admission.empty"
                    : active != 0 && unavailable == 0 && disabled == 0 ? "run.admission.satisfied" : "run.admission.summary";
                summary = PlannerText.Get(key, name, detail);
            }
            var lines = new List<string> { summary };
            var omissions = gate.Omissions.ToDictionary(o => o.CastingId, StringComparer.Ordinal);
            foreach (string id in orderedIds)
            {
                CastingOmission omission;
                string why;
                if (omissions.TryGetValue(id, out omission))
                    why = omission.Readiness == ResolvedCastingReadiness.Disabled ? PlannerText.Get("run.reason.disabled")
                        : omission.Readiness == ResolvedCastingReadiness.AlreadySatisfied ? PlannerText.Get("run.reason.active")
                        : string.Join(PlannerText.Get("run.join.detail"), omission.Reasons.Select(code => reason(id, code)).ToArray());
                else if (gate.ExecutableCastingIds.Contains(id))
                    why = strict ? PlannerText.Get("run.reason.strict")
                        : PlannerText.Get("run.reason.notattempted", DescribeRefusal(name, result));
                else continue;
                lines.Add(PlannerText.Get("run.log.detail", label(id), why));
            }
            return lines.AsReadOnly();
        }

        // A global refusal's concise visible sentence.
        internal static string GameLogRefusal(string refusal)
        {
            string value = (refusal ?? string.Empty).Trim();
            if (value.Length == 0) return PlannerText.Get("run.log.empty");
            int end = value.IndexOf(". ", StringComparison.Ordinal);
            int fullStop = value.IndexOf('。');
            if (fullStop >= 0 && (end < 0 || fullStop < end)) end = fullStop;
            string first = end < 0 ? value : value.Substring(0, end + 1);
            return PlannerText.Get("run.log.refusal", first.TrimEnd('.', '。'));
        }

        // One casting's outcome in the last run, as its card shows it. The
        // effect and the resource are stated separately: a failed or
        // interrupted cast may still have spent its slot.
        internal static string DescribeEntry(CastingOutcomeEntry entry)
        {
            if (entry == null) return null;
            string spent = entry.ResourceSpent ? PlannerText.Get("run.entry.resource") : string.Empty;
            if (entry.EffectObserved && !entry.EffectConfirmed) spent += PlannerText.Get("run.entry.unvalidated");
            switch (entry.State)
            {
                case CastingOutcomeState.EffectConfirmed:
                    return entry.FreeCast ? PlannerText.Get("run.entry.free")
                        : PlannerText.Get("run.entry.confirmed");
                case CastingOutcomeState.Skipped:
                    return PlannerText.Get("run.entry.skipped");
                case CastingOutcomeState.Omitted:
                    return PlannerText.Get("run.entry.omitted", ShortDetail(entry.Detail));
                case CastingOutcomeState.Failed:
                    return PlannerText.Get("run.entry.failed", ShortDetail(entry.Detail), spent);
                case CastingOutcomeState.Cancelled:
                    return PlannerText.Get("run.entry.interrupted", spent);
                default:
                    return PlannerText.Get("run.entry.unattempted");
            }
        }

        internal static string DescribeProblem(CastingWorkspaceSession session)
        {
            CastingProblemNavigation problems = session.ProblemNavigation;
            if (!problems.Active) return string.Empty;
            return PlannerText.Get("run.problem", session.RoutineDisplayName(problems.RoutineId), problems.Position, problems.Count, session.CastingLabel(problems.Current.CastingId), string.Join(PlannerText.Get("run.join.detail"), problems.Current.Reasons.Select(WorkspaceReasonText.Describe).ToArray()));
        }

        internal static string DescribeRefusal(string routineName, WorkspaceApplyResult result,
            CastingWorkspaceSession session)
        {
            return result != null && result.BlockingCastings.Count != 0 &&
                session != null && session.ProblemNavigation.Active
                    ? DescribeProblem(session) : DescribeRefusal(routineName, result);
        }

        // Refusals are explained in terms the player can act on; the exact
        // machine reason stays appended for the log and bug reports.
        internal static string DescribeRefusal(string routineName, WorkspaceApplyResult result)
        {
            string reason = result == null ? "unknown" : result.ReviewReason ?? string.Empty;
            // WP4: the global combat refusal is one plain sentence; it names
            // no routine or casting because none of them is at fault.
            if (reason == CastingExecutionPolicy.CombatActive) return CombatRefusalText;
            string text;
            if (reason.StartsWith("nothing-to-cast", StringComparison.Ordinal))
                text = PlannerText.Get("run.refusal.empty");
            else if (reason == "nothing-presented" || reason == "not-accepted")
                // v1.2 §5: the Run press itself authorizes the plan, so
                // these are internal faults, never a step for the player.
                text = PlannerText.Get("run.refusal.authorization");
            else if (reason == "material-change-requires-review")
                text = PlannerText.Get("run.refusal.changed");
            else if (reason.StartsWith("persistence-failed", StringComparison.Ordinal))
                text = PlannerText.Get("run.refusal.persistence");
            else if (reason.StartsWith("apply-refused", StringComparison.Ordinal))
                text = PlannerText.Get("run.refusal.blocked", result.GateDecision == null ? 0 : result.GateDecision.BlockingReasons.Count);
            else if (reason.StartsWith("import-notices-pending", StringComparison.Ordinal) ||
                reason.StartsWith("legacy-import-unresolved", StringComparison.Ordinal))
                text = PlannerText.Get("run.refusal.import");
            else if (reason.StartsWith("execution-projection-refused", StringComparison.Ordinal))
                text = PlannerText.Get("run.refusal.projection");
            else if (reason.StartsWith("execution-in-progress", StringComparison.Ordinal))
                text = PlannerText.Get("run.refusal.busy");
            else if (reason.StartsWith("native-submission-disabled", StringComparison.Ordinal) ||
                reason.StartsWith("native-casting-unavailable", StringComparison.Ordinal))
                text = PlannerText.Get("run.refusal.native");
            else
                text = PlannerText.Get("run.refusal.generic");
            return PlannerText.Get("run.refusal", routineName, text, reason);
        }

        internal static string CombatRefusalText { get { return PlannerText.Get("run.combat"); } }

        // v1.2 §5: a HUD routine button's tooltip in casting-first mode. A
        // click runs that routine at once after a fresh check of the party;
        // there is no acceptance state to describe. castings is null when
        // no plan session is loaded yet (a cold click loads it).
        internal static string RoutineTooltip(string routineName, int? castings,
            string executionMode, string lastPress)
        {
            string text = castings == null
                ? PlannerText.Get("run.tooltip.cold", routineName)
                : PlannerText.Get(castings.Value == 1 ? "run.tooltip.one" : "run.tooltip.many",
                    routineName, castings.Value, executionMode);
            text = PlannerText.Get("run.tooltip.policy", text);
            if (string.IsNullOrEmpty(lastPress)) return text;
            return PlannerText.Get("run.tooltip.last", text, lastPress.Length <= 180 ? lastPress : lastPress.Substring(0, 177) + "...");
        }

        // An executor-contract limitation (the converter refusal) in words.
        internal static string DescribeLimitation(string limitation)
        {
            string value = limitation ?? string.Empty;
            if (value.Contains(":targeting-modifier:"))
                return PlannerText.Get("run.limit.target");
            if (value.Contains(":exact-enhancement-source:"))
                return PlannerText.Get("run.limit.exact");
            return value;
        }

        // A live "already active" verdict an earlier casting of the same
        // routine invalidates (missing-beneficial-buffs review R1-F1).
        internal const string ExistingRemovedEarlierPrefix = ExplicitCastingCompiler.ExistingRemovedEarlierNote;

        // One live existing-effect verdict in words.
        internal static string DescribeExistingEffectNote(string note)
        {
            return DescribeExistingEffectNote(note, null);
        }

        // The same, naming the unit through the given resolver (the
        // workspace's character names; review: raw ids told the player
        // nothing).
        internal static string DescribeExistingEffectNote(string note, Func<string, string> unitName)
        {
            string value = note ?? string.Empty;
            if (unitName != null)
                foreach (string prefix in new[] { "already-active:", "already-covered:", "already-covered-longer:",
                    "existing-active-recast:", "existing-insufficient:", "existing-incomplete:",
                    ExistingRemovedEarlierPrefix, ExplicitCastingCompiler.ExistingKeptBlockedNote })
                    if (value.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        string unit = FirstSegment(value, prefix);
                        string name = unitName(unit);
                        if (!string.IsNullOrEmpty(name) && name.IndexOf(':') < 0)
                            value = prefix + name + value.Substring(prefix.Length + unit.Length);
                        break;
                    }
            if (value.StartsWith(ExistingRemovedEarlierPrefix, StringComparison.Ordinal))
                return PlannerText.Get("run.effect.removed", FirstSegment(value, ExistingRemovedEarlierPrefix));
            if (value.StartsWith(ExplicitCastingCompiler.ExistingKeptBlockedNote, StringComparison.Ordinal))
                return PlannerText.Get("run.effect.keptblocked",
                    FirstSegment(value, ExplicitCastingCompiler.ExistingKeptBlockedNote));
            if (value.StartsWith("already-active:", StringComparison.Ordinal))
                return PlannerText.Get("run.effect.active", FirstSegment(value, "already-active:"));
            if (value.StartsWith("already-covered:", StringComparison.Ordinal))
                return PlannerText.Get("run.effect.covered", FirstSegment(value, "already-covered:"));
            if (value.StartsWith("already-covered-longer:", StringComparison.Ordinal))
                return PlannerText.Get("run.effect.longer", FirstSegment(value, "already-covered-longer:"));
            if (value.StartsWith("existing-active-recast:", StringComparison.Ordinal))
                return PlannerText.Get("run.effect.recast", FirstSegment(value, "existing-active-recast:"));
            if (value.StartsWith("existing-incomplete:", StringComparison.Ordinal))
                return PlannerText.Get("run.effect.incomplete", FirstSegment(value, "existing-incomplete:"));
            if (value.StartsWith("existing-insufficient:", StringComparison.Ordinal))
            {
                string unit = FirstSegment(value, "existing-insufficient:");
                string why = value.Substring("existing-insufficient:".Length + unit.Length);
                string reason = why.Contains("weaker-caster-level")
                    ? PlannerText.Get("run.effect.weak")
                    : why.Contains("remaining-duration-short") || why.Contains("expiring")
                        ? PlannerText.Get("run.effect.expiring")
                        : why.Contains("missing-metamagic") || why.Contains("metamagic-unverified")
                            ? PlannerText.Get("run.effect.metamagic")
                            : why.Contains("equivalence-unproven") ||
                              why.Contains("caster-level-unverified")
                                ? PlannerText.Get("run.effect.unproven")
                                : why.Contains("suppression-unreadable")
                                    ? PlannerText.Get("run.effect.suppressed")
                                    : PlannerText.Get("run.effect.insufficient");
                return PlannerText.Get("run.effect.recastexplain", unit, reason);
            }
            return value;
        }

        private static string FirstSegment(string value, string prefix)
        {
            string rest = value.Substring(prefix.Length);
            int split = rest.IndexOf(":", StringComparison.Ordinal);
            return split < 0 ? rest : rest.Substring(0, split);
        }

        // Availability refusals are noninterrupting. The unrelated recovery
        // and import workflows retain their existing opening policy.
        internal static bool OpensPlanner(WorkspaceApplyResult result)
        {
            return result != null && OpensPlanner(result.ReviewReason);
        }

        internal static bool OpensPlanner(string reason)
        {
            string value = reason ?? string.Empty;
            return value == "nothing-presented" || value == "not-accepted" ||
                value == "material-change-requires-review" ||
                value.StartsWith("import-notices-pending", StringComparison.Ordinal) ||
                value.StartsWith("legacy-import-unresolved", StringComparison.Ordinal);
        }

        private static string StopReason(string terminal)
        {
            string reason = terminal ?? string.Empty;
            if (reason.StartsWith("cancelled:", StringComparison.Ordinal))
                reason = reason.Substring("cancelled:".Length);
            switch (reason)
            {
                case "player-stopped": return PlannerText.Get("run.stop.player");
                case "deadline": return PlannerText.Get("run.stop.deadline");
                case "mod-disabled": return PlannerText.Get("run.stop.disabled");
                case "mod-unload": return PlannerText.Get("run.stop.unload");
                case "area-unloading": return PlannerText.Get("run.stop.area");
                case "root-teardown": return PlannerText.Get("run.stop.teardown");
                default: return reason.Length == 0 ? PlannerText.Get("run.stop.generic") : PlannerText.Get("run.stop.detail", reason);
            }
        }

        private static string ShortDetail(string detail)
        {
            if (string.IsNullOrEmpty(detail)) return PlannerText.Get("run.detail.missing");
            return detail.Length <= 120 ? detail : detail.Substring(0, 117) + "...";
        }
    }
}
