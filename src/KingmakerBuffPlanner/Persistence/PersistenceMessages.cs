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
                return "Not saved: the plan file for this campaign holds another campaign's plan. " +
                    "It was left unchanged.";
            if (detail.StartsWith("refusing-to-overwrite-invalid-primary", StringComparison.Ordinal) ||
                detail.StartsWith("refusing-to-overwrite-unreadable-or-newer-primary", StringComparison.Ordinal))
                return "Not saved: the plan file could not be read or comes from a newer planner. " +
                    "It was left unchanged; move it aside to save here.";
            if (detail.StartsWith("Candidate persistence is blocked: legacy-import", StringComparison.Ordinal))
                return "Not saved: your classic plan could not be imported, and nothing is saved until it is. " +
                    "Repair or restore it, then press Reload.";
            if (detail.StartsWith("Candidate persistence is blocked", StringComparison.Ordinal))
                return "Not saved: your casting plan file could not be read or comes from a newer planner. " +
                    "Move it and its backups (.bak1 to .bak3) out of UserSettings, then press Reload.";
            return "Not saved: " + (detail.Length == 0 ? "the plan could not be written." : detail);
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
                return "Your saved planner setup could not be read or comes from a newer planner. It was " +
                    "left unchanged and a new setup is shown; changes are not saved while that file is " +
                    "there - move it aside to save here.";
            if (primaryUnreadable && recoveredFromBackup)
                return "Your saved planner setup could not be read, so its latest backup was loaded. The " +
                    "unreadable file was left unchanged; changes are not saved while it is there - move " +
                    "it aside to save here.";
            if (recoveredFromBackup)
                return "Your saved planner setup file was missing, so its latest readable backup was loaded.";
            // Focused re-review: a missing file whose backups cannot be read
            // saves normally, but the saves replace those backups in turn.
            if (string.IsNullOrEmpty(sourcePath) && !string.IsNullOrEmpty(warning))
                return "Your saved planner setup file was missing and its backups could not be read, so a new " +
                    "setup is shown. Saving works, but it replaces those backups in turn - move them out of " +
                    "UserSettings to keep them.";
            return null;
        }

        public static string ForClassicSaveRefusal(string refusal)
        {
            if (string.IsNullOrEmpty(refusal)) return null;
            return "Not saved: the saved planner setup for this campaign could not be read or comes " +
                "from a newer planner. It was left unchanged; move it aside to save here.";
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
                    return "Your casting plan could not be read or comes from a newer planner" +
                        (files.Length == 0 ? string.Empty : " (" + files + ")") + ", so nothing was loaded " +
                        "from it and saving is blocked. It was left unchanged: move " +
                        (files.Length == 0 ? "that file" : "those files") + " out of UserSettings, then press " +
                        "Reload - the newest readable backup is then loaded, or the planner starts over.";
                case CastingPlanLoadStatus.RecoveredFromBackup:
                    return primaryFileExists
                        ? "Your casting plan could not be read, so its latest backup was loaded. The " +
                            "unreadable file was left unchanged; saving is refused while it is there - move " +
                            "it aside to save here."
                        : "Your casting plan file was missing, so its latest backup was loaded.";
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
