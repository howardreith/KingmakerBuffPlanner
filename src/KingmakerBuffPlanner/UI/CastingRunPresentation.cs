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
            var text = new StringBuilder();
            text.Append(routineName);
            if (report.Cancelled)
                text.Append(" stopped (").Append(StopReason(report.TerminalReason)).Append("): ");
            else if (report.Halted)
                text.Append(" stopped after a cast failed: ");
            else
                text.Append(": ");
            text.Append(report.Confirmed).Append(" confirmed of ").Append(report.Entries.Count)
                .Append(report.Entries.Count == 1 ? " saved casting" : " saved castings");
            if (report.Skipped != 0)
                text.Append("; ").Append(report.Skipped).Append(" already active");
            int disabled = report.Entries.Count(IsDisabled);
            if (report.Omitted - disabled != 0)
                text.Append("; ").Append(report.Omitted - disabled).Append(" unavailable");
            if (disabled != 0) text.Append("; ").Append(disabled).Append(" disabled");
            CastingOutcomeEntry failed = report.Entries.FirstOrDefault(
                entry => entry.State == CastingOutcomeState.Failed);
            if (failed != null)
                text.Append("; failed: ").Append(label(failed.CastingId))
                    .Append(" (").Append(ShortDetail(failed.Detail)).Append(")");
            if (report.CancelledCastings != 0)
                text.Append("; ").Append(report.CancelledCastings).Append(" interrupted");
            if (report.NotProcessed != 0)
                text.Append("; ").Append(report.NotProcessed).Append(" not attempted");
            int free = report.Entries.Count(entry => entry.FreeCast);
            text.Append(". Casts with observed resource use: ").Append(report.ResourcesSpent);
            if (free != 0) text.Append(" (plus ").Append(free).Append(" free)");
            int failedEffects = report.Entries.Count(entry => entry.EffectObserved && !entry.EffectConfirmed);
            if (failedEffects != 0)
                text.Append("; effect observed on ").Append(failedEffects).Append(" unvalidated cast(s)");
            text.Append(".");
            if (report.CleanupFailures.Count != 0)
                text.Append(" Cleanup uncertain: ")
                    .Append(string.Join("; ", report.CleanupFailures.ToArray())).Append(".");
            return text.ToString();
        }


        // The visible summary counts the full scoped outcome. Named detail
        // lines follow for every uncast entry; native successes are not replayed.
        internal static string GameLogLine(CastingRunReport report, string routineName)
        {
            if (report == null) throw new ArgumentNullException("report");
            var text = new StringBuilder("Buff Planner: ").Append(routineName);
            if (report.Cancelled) text.Append(" stopped");
            else if (report.Halted || report.Failed != 0) text.Append(" finished with a failure");
            else if (report.Omitted > report.Entries.Count(IsDisabled)) text.Append(" finished with omissions");
            else text.Append(" done");
            text.Append(" - ").Append(report.Confirmed).Append(" cast");
            if (report.Skipped != 0) text.Append(", ").Append(report.Skipped).Append(" already active");
            int disabled = report.Entries.Count(IsDisabled);
            if (report.Omitted - disabled != 0)
                text.Append(", ").Append(report.Omitted - disabled).Append(" unavailable");
            if (disabled != 0) text.Append(", ").Append(disabled).Append(" disabled");
            if (report.Failed != 0) text.Append(", ").Append(report.Failed).Append(" failed");
            if (report.CancelledCastings != 0) text.Append(", ").Append(report.CancelledCastings).Append(" interrupted");
            if (report.NotProcessed != 0) text.Append(", ").Append(report.NotProcessed).Append(" not attempted");
            return text.Append(".").ToString();
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
                string why = entry.State == CastingOutcomeState.Skipped ? "already active"
                    : IsDisabled(entry) ? "explicitly disabled"
                    : entry.State == CastingOutcomeState.Omitted
                        ? string.Join("; ", entry.OmissionReasons.Select(code => reason(entry.CastingId, code)).ToArray())
                        : DescribeEntry(entry);
                if (string.IsNullOrEmpty(why)) why = WorkspaceReasonText.Describe(entry.Detail);
                lines.Add("Not cast: " + label(entry.CastingId) + " — " + why + ".");
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
            string summary = "Buff Planner: " + name + " — 0 cast";
            if (unavailable != 0) summary += ", " + unavailable + " unavailable";
            if (active != 0) summary += ", " + active + " already active";
            if (disabled != 0) summary += ", " + disabled + " disabled";
            if (strict) summary += ", " + gate.ExecutableCastingIds.Count + " withheld by Require full plan";
            else if (gate.ExecutableCastingIds.Count != 0)
                summary += ". " + DescribeRefusal(name, result);
            else if (empty) summary += ": nothing configured to cast";
            else if (active != 0 && unavailable == 0 && disabled == 0)
                summary += ": the needed effects are already present";
            var lines = new List<string> { summary.TrimEnd('.') + "." };
            var omissions = gate.Omissions.ToDictionary(o => o.CastingId, StringComparer.Ordinal);
            foreach (string id in orderedIds)
            {
                CastingOmission omission;
                string why;
                if (omissions.TryGetValue(id, out omission))
                    why = omission.Readiness == ResolvedCastingReadiness.Disabled ? "explicitly disabled"
                        : omission.Readiness == ResolvedCastingReadiness.AlreadySatisfied ? "already active"
                        : string.Join("; ", omission.Reasons.Select(code => reason(id, code)).ToArray());
                else if (gate.ExecutableCastingIds.Contains(id))
                    why = strict ? "ready, withheld by Require full plan"
                        : "not attempted: " + DescribeRefusal(name, result);
                else continue;
                lines.Add("Not cast: " + label(id) + " — " + why + ".");
            }
            return lines.AsReadOnly();
        }

        // A global refusal's concise visible sentence.
        internal static string GameLogRefusal(string refusal)
        {
            string value = (refusal ?? string.Empty).Trim();
            if (value.Length == 0) return "Buff Planner: nothing was cast.";
            int end = value.IndexOf(". ", StringComparison.Ordinal);
            string first = end < 0 ? value : value.Substring(0, end + 1);
            return "Buff Planner: " + first.TrimEnd('.') + ".";
        }

        // One casting's outcome in the last run, as its card shows it. The
        // effect and the resource are stated separately: a failed or
        // interrupted cast may still have spent its slot.
        internal static string DescribeEntry(CastingOutcomeEntry entry)
        {
            if (entry == null) return null;
            string spent = entry.ResourceSpent ? "; resource use observed" : string.Empty;
            if (entry.EffectObserved && !entry.EffectConfirmed) spent += "; effect observed, cast unvalidated";
            switch (entry.State)
            {
                case CastingOutcomeState.EffectConfirmed:
                    return entry.FreeCast ? "cast, effect confirmed (free)"
                        : "cast, effect confirmed";
                case CastingOutcomeState.Skipped:
                    return "skipped: the effect was already active";
                case CastingOutcomeState.Omitted:
                    return "left out of the run (" + ShortDetail(entry.Detail) + ")";
                case CastingOutcomeState.Failed:
                    return "failed (" + ShortDetail(entry.Detail) + ")" + spent;
                case CastingOutcomeState.Cancelled:
                    return "interrupted when the run stopped" + spent;
                default:
                    return "not attempted: the run stopped earlier";
            }
        }

        internal static string DescribeProblem(CastingWorkspaceSession session)
        {
            CastingProblemNavigation problems = session.ProblemNavigation;
            if (!problems.Active) return string.Empty;
            return session.RoutineDisplayName(problems.RoutineId) + " was not cast. Showing Problem " +
                problems.Position + " of " + problems.Count + ": " +
                session.CastingLabel(problems.Current.CastingId) + ". Not ready: " +
                string.Join("; ", problems.Current.Reasons.Select(WorkspaceReasonText.Describe).ToArray()) + ".";
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
                text = "nothing to cast - every buff is already active, disabled or omitted";
            else if (reason == "nothing-presented" || reason == "not-accepted")
                // v1.2 §5: the Run press itself authorizes the plan, so
                // these are internal faults, never a step for the player.
                text = "the run could not be authorized (an internal check failed) - nothing was cast";
            else if (reason == "material-change-requires-review")
                text = "the plan changed while the run was starting - nothing was cast; press again " +
                    "to run the current plan";
            else if (reason.StartsWith("persistence-failed", StringComparison.Ordinal))
                text = "your latest edit is not saved yet, so nothing was cast - the planner footer " +
                    "shows why and offers Retry save";
            else if (reason.StartsWith("apply-refused", StringComparison.Ordinal))
                text = "some castings are blocked (" + (result.GateDecision == null ? 0
                    : result.GateDecision.BlockingReasons.Count) +
                    ") - fix them or uncheck Require full plan in the planner";
            else if (reason.StartsWith("import-notices-pending", StringComparison.Ordinal) ||
                reason.StartsWith("legacy-import-unresolved", StringComparison.Ordinal))
                text = "the imported plan still needs your review in the planner";
            else if (reason.StartsWith("execution-projection-refused", StringComparison.Ordinal))
                text = "a casting uses a setting this version cannot execute yet";
            else if (reason.StartsWith("execution-in-progress", StringComparison.Ordinal))
                text = "another routine is already running";
            else if (reason.StartsWith("native-submission-disabled", StringComparison.Ordinal) ||
                reason.StartsWith("native-casting-unavailable", StringComparison.Ordinal))
                text = "native casting is not available in this session";
            else
                text = "refused";
            return routineName + " was not cast: " + text + ". (" + reason + ")";
        }

        internal const string CombatRefusalText = "Buff routines cannot run during combat.";

        // v1.2 §5: a HUD routine button's tooltip in casting-first mode. A
        // click runs that routine at once after a fresh check of the party;
        // there is no acceptance state to describe. castings is null when
        // no plan session is loaded yet (a cold click loads it).
        internal static string RoutineTooltip(string routineName, int? castings,
            string executionMode, string lastPress)
        {
            string text = castings == null
                ? "Cast " + routineName + ": one click runs your saved " + routineName + " routine now"
                : "Cast " + routineName + ": " + castings.Value +
                    (castings.Value == 1 ? " casting" : " castings") + ", " + executionMode +
                    " mode. One click runs " + (castings.Value == 1 ? "it" : "them") + " now";
            text += " (the party is checked first; available castings run in authored order unless " +
                "Require full plan is enabled; omissions are explained in the game log).";
            if (string.IsNullOrEmpty(lastPress)) return text;
            return text + " Last: " + (lastPress.Length <= 180 ? lastPress
                : lastPress.Substring(0, 177) + "...");
        }

        // An executor-contract limitation (the converter refusal) in words.
        internal static string DescribeLimitation(string limitation)
        {
            string value = limitation ?? string.Empty;
            if (value.Contains(":targeting-modifier:"))
                return "a targeting modifier (such as Share Transmutation) that is not verified as " +
                    "executable for this caster";
            if (value.Contains(":exact-enhancement-source:"))
                return "an exact rod or item identity is not executed yet";
            return value;
        }

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
                    "existing-active-recast:", "existing-insufficient:", "existing-incomplete:" })
                    if (value.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        string unit = FirstSegment(value, prefix);
                        string name = unitName(unit);
                        if (!string.IsNullOrEmpty(name) && name.IndexOf(':') < 0)
                            value = prefix + name + value.Substring(prefix.Length + unit.Length);
                        break;
                    }
            if (value.StartsWith("already-active:", StringComparison.Ordinal))
                return "already active on " + FirstSegment(value, "already-active:") + " (skipped)";
            if (value.StartsWith("already-covered:", StringComparison.Ordinal))
                return "already active on " + FirstSegment(value, "already-covered:") +
                    " (the cast goes ahead for the others)";
            if (value.StartsWith("already-covered-longer:", StringComparison.Ordinal))
                return "already active on " + FirstSegment(value, "already-covered-longer:") +
                    ", lasting longer than this cast (the cast goes ahead for the others and may shorten it)";
            if (value.StartsWith("existing-active-recast:", StringComparison.Ordinal))
                return "active on " + FirstSegment(value, "existing-active-recast:") +
                    " but set to always recast";
            if (value.StartsWith("existing-incomplete:", StringComparison.Ordinal))
                return "partly present on " + FirstSegment(value, "existing-incomplete:") + " (will cast)";
            if (value.StartsWith("existing-insufficient:", StringComparison.Ordinal))
            {
                string unit = FirstSegment(value, "existing-insufficient:");
                string why = value.Substring("existing-insufficient:".Length + unit.Length);
                string reason = why.Contains("weaker-caster-level")
                    ? "weaker (lower caster level)"
                    : why.Contains("remaining-duration-short") || why.Contains("expiring")
                        ? "about to expire"
                        : why.Contains("missing-metamagic") || why.Contains("metamagic-unverified")
                            ? "without the planned metamagic"
                            : why.Contains("equivalence-unproven") ||
                              why.Contains("caster-level-unverified")
                                ? "not provably as strong"
                                : why.Contains("suppression-unreadable")
                                    ? "possibly suppressed"
                                    : "not sufficient";
                return "present on " + unit + " but " + reason + " (will recast)";
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
                case "player-stopped": return "you stopped it";
                case "deadline": return "it took too long";
                case "mod-disabled": return "the mod was disabled";
                case "mod-unload": return "the mod was unloaded";
                case "area-unloading": return "the area changed";
                case "root-teardown": return "the planner shut down";
                default: return reason.Length == 0 ? "stopped" : reason;
            }
        }

        private static string ShortDetail(string detail)
        {
            if (string.IsNullOrEmpty(detail)) return "no detail";
            return detail.Length <= 120 ? detail : detail.Substring(0, 117) + "...";
        }
    }
}
