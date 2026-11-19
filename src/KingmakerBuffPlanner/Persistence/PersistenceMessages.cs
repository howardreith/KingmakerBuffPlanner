using System;
using System.Collections.Generic;

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
                return PlannerText.Get("persistence.save.otherCampaign");
            if (detail.StartsWith("refusing-to-overwrite-invalid-primary", StringComparison.Ordinal) ||
                detail.StartsWith("refusing-to-overwrite-unreadable-or-newer-primary", StringComparison.Ordinal))
                return PlannerText.Get("persistence.save.protected");
            if (detail.StartsWith("Candidate persistence is blocked: legacy-import", StringComparison.Ordinal))
                return PlannerText.Get("persistence.save.importBlocked");
            if (detail.StartsWith("Candidate persistence is blocked", StringComparison.Ordinal))
                return PlannerText.Get("persistence.save.loadBlocked");
            return (detail.Length == 0 ? PlannerText.Get("persistence.save.failed") : PlannerText.Get("persistence.save.error", detail));
        }

        // Final review B4: what the player is told when a planner opens and
        // its saved file could not be used, or a change was not saved. Null
        // when all is well. A file that could not be read is never replaced
        // by an ordinary save, so every notice also says saving is refused.

        // Classic: the load result names no source when nothing could be
        // loaded; its warning lists the files that could not be read, by
        // name. Saves are refused only while the primary file itself cannot
        // be read (re-review): a missing primary is simply written again.
        public static bool ClassicPrimaryUnreadable(string warning, string primaryFileName)
        {
            return !string.IsNullOrEmpty(warning) && !string.IsNullOrEmpty(primaryFileName) &&
                warning.Contains(primaryFileName + ":");
        }

        public static string ForClassicLoad(string sourcePath, bool recoveredFromBackup, string warning,
            string primaryFileName)
        {
            bool primaryUnreadable = ClassicPrimaryUnreadable(warning, primaryFileName);
            if (primaryUnreadable && string.IsNullOrEmpty(sourcePath))
                return PlannerText.Get("persistence.classic.loadUnreadable");
            if (primaryUnreadable && recoveredFromBackup)
                return PlannerText.Get("persistence.classic.loadProtectedBackup");
            if (recoveredFromBackup)
                return PlannerText.Get("persistence.classic.loadBackup");
            // Focused re-review: a missing file whose backups cannot be read
            // saves normally, but the saves replace those backups in turn.
            if (string.IsNullOrEmpty(sourcePath) && !string.IsNullOrEmpty(warning))
                return PlannerText.Get("persistence.classic.loadNoBackup");
            return null;
        }

        public static string ForClassicSaveRefusal(string refusal)
        {
            if (string.IsNullOrEmpty(refusal)) return null;
            return PlannerText.Get("persistence.classic.saveRefused");
        }

        // The casting plan files a load could not use, by name: a newer one
        // (the load stops there) or every unreadable one in the warning.
        public static string UnusableCastingFiles(CastingPlanLoadStatus status, string sourcePath, string warning)
        {
            var names = new List<string>();
            // Unreadable files named in the warning (a file name is the part
            // before its first colon), then the newer file the load stopped at.
            foreach (string entry in (warning ?? string.Empty).Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = entry.IndexOf(':');
                string name = (colon < 0 ? entry : entry.Substring(0, colon)).Trim();
                if (name.IndexOf(".json", StringComparison.OrdinalIgnoreCase) >= 0 && !names.Contains(name))
                    names.Add(name);
            }
            if (status == CastingPlanLoadStatus.UnsupportedSchema && !string.IsNullOrEmpty(sourcePath))
            {
                string newer = System.IO.Path.GetFileName(sourcePath);
                if (!names.Contains(newer)) names.Add(newer);
            }
            return string.Join(", ", names.ToArray());
        }

        // Casting-first: the plan file as the session loaded it; a backup
        // loaded because the primary is missing (not unreadable) saves
        // normally (re-review).
        public static string ForCastingLoad(CastingPlanLoadStatus status, bool primaryFileExists,
            string sourcePath = null, string warning = null)
        {
            switch (status)
            {
                case CastingPlanLoadStatus.Corrupt:
                case CastingPlanLoadStatus.UnsupportedSchema:
                    // Focused re-review: the files it could not use are named,
                    // and only those are moved (a readable backup behind them
                    // then loads).
                    string files = UnusableCastingFiles(status, sourcePath, warning);
                    return (files.Length == 0 ? PlannerText.Get("persistence.load.blocked") : PlannerText.Get("persistence.load.blockedFiles", files));
                case CastingPlanLoadStatus.RecoveredFromBackup:
                    return primaryFileExists
                        ? PlannerText.Get("persistence.load.protectedBackup")
                        : PlannerText.Get("persistence.load.backup");
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
                ? PlannerText.Get("persistence.review.protected")
                : PlannerText.Get("persistence.review.saveFailed");
        }
    }
}
