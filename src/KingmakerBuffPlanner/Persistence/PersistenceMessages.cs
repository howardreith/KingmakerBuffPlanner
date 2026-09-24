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

        // Final review B4: what the player is told when a planner opens and
        // its saved file could not be used, or a change was not saved. Null
        // when all is well. A file that could not be read is never replaced
        // by an ordinary save, so every notice also says saving is refused.

        // Classic: the load result names no source when nothing could be
        // loaded; its warning lists the files that could not be read.
        public static string ForClassicLoad(string sourcePath, bool recoveredFromBackup, string warning)
        {
            if (string.IsNullOrEmpty(warning)) return null;
            if (string.IsNullOrEmpty(sourcePath))
                return "Your saved planner setup could not be read or comes from a newer planner. It was " +
                    "left unchanged and a new setup is shown; changes are not saved while that file is " +
                    "there - move it aside to save here.";
            if (recoveredFromBackup)
                return "Your saved planner setup could not be read, so its latest backup was loaded. The " +
                    "unreadable file was left unchanged; changes are not saved while it is there - move " +
                    "it aside to save here.";
            return null;
        }

        public static string ForClassicSaveRefusal(string refusal)
        {
            if (string.IsNullOrEmpty(refusal)) return null;
            return "Not saved: the saved planner setup for this campaign could not be read or comes " +
                "from a newer planner. It was left unchanged; move it aside to save here.";
        }

        // Casting-first: the plan file as the session loaded it.
        public static string ForCastingLoad(CastingPlanLoadStatus status)
        {
            switch (status)
            {
                case CastingPlanLoadStatus.Corrupt:
                case CastingPlanLoadStatus.UnsupportedSchema:
                    return "Your casting plan could not be read or comes from a newer planner. It was " +
                        "left unchanged and an empty plan is shown; saving is blocked until that file " +
                        "is moved aside.";
                case CastingPlanLoadStatus.RecoveredFromBackup:
                    return "Your casting plan could not be read, so its latest backup was loaded. The " +
                        "unreadable file was left unchanged; saving is refused while it is there - move " +
                        "it aside to save here.";
                default:
                    return null;
            }
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
