using System;
using System.Linq;
using System.Text;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
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
            text.Append(report.Confirmed).Append(" of ").Append(report.Planned)
                .Append(report.Planned == 1 ? " cast" : " casts").Append(" confirmed");
            if (report.Skipped != 0)
                text.Append("; ").Append(report.Skipped).Append(" already active");
            if (report.Omitted != 0)
                text.Append("; ").Append(report.Omitted).Append(" omitted");
            CastingOutcomeEntry failed = report.Entries.FirstOrDefault(
                entry => entry.State == CastingOutcomeState.Failed);
            if (failed != null)
                text.Append("; failed: ").Append(label(failed.CastingId))
                    .Append(" (").Append(ShortDetail(failed.Detail)).Append(")");
            if (report.CancelledCastings != 0)
                text.Append("; ").Append(report.CancelledCastings).Append(" interrupted");
            if (report.NotProcessed != 0)
                text.Append("; ").Append(report.NotProcessed).Append(" not attempted");
            int free = report.Entries.Count(entry => entry.SpendReported && entry.FreeCast);
            text.Append(". Resources spent: ").Append(report.ResourcesSpent);
            if (free != 0) text.Append(" (plus ").Append(free).Append(" free)");
            text.Append(".");
            if (report.CleanupFailures.Count != 0)
                text.Append(" Cleanup uncertain: ")
                    .Append(string.Join("; ", report.CleanupFailures.ToArray())).Append(".");
            return text.ToString();
        }


        // One casting's outcome in the last run, as its card shows it. The
        // effect and the resource are stated separately: a failed or
        // interrupted cast may still have spent its slot.
        internal static string DescribeEntry(CastingOutcomeEntry entry)
        {
            if (entry == null) return null;
            string spent = entry.ResourceSpent ? "; its resource was spent" : string.Empty;
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

        // Refusals are explained in terms the player can act on; the exact
        // machine reason stays appended for the log and bug reports.
        internal static string DescribeRefusal(string routineName, WorkspaceApplyResult result)
        {
            string reason = result == null ? "unknown" : result.ReviewReason ?? string.Empty;
            string text;
            if (reason.StartsWith("nothing-to-cast", StringComparison.Ordinal))
                text = "nothing to cast - every buff is already active, disabled or omitted";
            else if (reason == "nothing-presented" || reason == "not-accepted")
                text = "open the planner, review this routine and press Accept Plan first";
            else if (reason == "material-change-requires-review")
                text = "the plan changed since you accepted it - open the planner to review it";
            else if (reason.StartsWith("apply-refused", StringComparison.Ordinal))
                text = "some castings are blocked (" + (result.GateDecision == null ? 0
                    : result.GateDecision.BlockingReasons.Count) +
                    ") - fix them or use Ready Casts Only in the planner";
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

        // An executor-contract limitation (the converter refusal) in words.
        internal static string DescribeLimitation(string limitation)
        {
            string value = limitation ?? string.Empty;
            if (value.Contains(":targeting-modifier:"))
                return "a targeting modifier (such as Share Transmutation) is not executed yet";
            if (value.Contains(":exact-enhancement-source:"))
                return "an exact rod or item identity is not executed yet";
            if (value.Contains(":required-coverage-incomplete:"))
                return "required recipients are outside the predicted area of effect";
            return value;
        }

        // One live existing-effect verdict in words.
        internal static string DescribeExistingEffectNote(string note)
        {
            string value = note ?? string.Empty;
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

        // Refusals the player resolves in the planner (review, blocked or
        // unsupported castings, import review): the planner opens on that
        // routine. Informational refusals (nothing to cast, a run already in
        // progress, casting unavailable in this session) do not.
        internal static bool OpensPlanner(string reason)
        {
            string value = reason ?? string.Empty;
            return value == "nothing-presented" || value == "not-accepted" ||
                value == "material-change-requires-review" ||
                value.StartsWith("apply-refused", StringComparison.Ordinal) ||
                value.StartsWith("import-notices-pending", StringComparison.Ordinal) ||
                value.StartsWith("legacy-import-unresolved", StringComparison.Ordinal) ||
                value.StartsWith("execution-projection-refused", StringComparison.Ordinal);
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
