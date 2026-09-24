using System;

namespace KingmakerBuffPlanner.Persistence
{
    // Player-facing words for a refused or failed save (batch 3 review B6):
    // a protected file is named as kept, never as lost, and the player is
    // told what to do.
    public static class PersistenceMessages
    {
        public static string ForSaveFailure(Exception exception)
        {
            string detail = exception == null ? string.Empty : exception.Message ?? string.Empty;
            if (detail.StartsWith("refusing-to-overwrite-another-campaigns-primary", StringComparison.Ordinal))
                return "Not saved: the plan file for this campaign holds another campaign's plan. " +
                    "It was left unchanged.";
            if (detail.StartsWith("refusing-to-overwrite-invalid-primary", StringComparison.Ordinal) ||
                detail.StartsWith("refusing-to-overwrite-unreadable-or-newer-primary", StringComparison.Ordinal))
                return "Not saved: the plan file could not be read or comes from a newer planner. " +
                    "It was left unchanged; move it aside to save here.";
            if (detail.StartsWith("Candidate persistence is blocked", StringComparison.Ordinal))
                return "Not saved: " + detail;
            return "Not saved: " + (detail.Length == 0 ? "the plan could not be written." : detail);
        }

        // Null when the review state was saved (or nothing is wrong).
        public static string ForReviewWarning(string warning)
        {
            if (string.IsNullOrEmpty(warning) ||
                !warning.StartsWith("review-state-save-failed:", StringComparison.Ordinal))
                return null;
            return warning.IndexOf("review-state-file-protected:", StringComparison.Ordinal) >= 0
                ? "Plan accepted for this session only: the saved review file could not be read or comes " +
                    "from a newer planner, and was left unchanged."
                : "Plan accepted for this session only: the review could not be saved.";
        }
    }
}
