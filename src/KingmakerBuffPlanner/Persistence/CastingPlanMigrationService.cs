using System;
using System.Collections.Generic;
using System.IO;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Infrastructure;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.Persistence
{
    public enum CastingMigrationStatus
    {
        Migrated,
        LegacyAbsent,
        LegacyUnreadable,
        CandidateUnusable,
        NewerCandidateRefused
    }

    public sealed class CastingMigrationResult
    {
        internal CastingMigrationResult(
            CastingMigrationStatus status,
            CastingImportReport importReport,
            string archivePath,
            string legacySha256,
            string candidatePath,
            string warning)
        {
            Status = status;
            ImportReport = importReport;
            ArchivePath = archivePath ?? string.Empty;
            LegacySha256 = legacySha256 ?? string.Empty;
            CandidatePath = candidatePath ?? string.Empty;
            Warning = warning ?? string.Empty;
        }

        public CastingMigrationStatus Status { get; private set; }
        public CastingImportReport ImportReport { get; private set; }
        public string ArchivePath { get; private set; }
        public string LegacySha256 { get; private set; }
        public string CandidatePath { get; private set; }
        public string Warning { get; private set; }
    }

    // Orchestrates the schema-5 -> schema-6 boundary under the charter's
    // safety rules, entirely inside candidate storage: the exact legacy
    // bytes are hashed and archived once per migration boundary outside the
    // rotating backup chain, the converted plan is written through the
    // candidate repository, and the written candidate is reopened and
    // revalidated before the migration is reported as complete. The legacy
    // schema-5 file itself is never modified — an error at any step leaves
    // the original bytes recoverable and the old UI unaffected.
    // Re-review (focused): the classic planner's in-memory plan of a campaign
    // and the SHA-256 of the primary file it was read from or last saved to
    // (null when it came from a backup or is a new default).
    public sealed class ClassicPlanInMemory
    {
        public ClassicPlanInMemory(BuffPlannerProfile profile, string primarySha256)
        {
            Profile = profile;
            PrimarySha256 = primarySha256;
        }

        public BuffPlannerProfile Profile { get; private set; }
        public string PrimarySha256 { get; private set; }
    }

    public sealed class CastingPlanMigrationService
    {
        private readonly CastingPlanImporter _importer = new CastingPlanImporter();
        private readonly CastingPlanRepository _candidateRepository;
        private readonly ProfileRepository _legacyRepository;

        public CastingPlanMigrationService(string modPath)
        {
            if (string.IsNullOrWhiteSpace(modPath) || !Path.IsPathRooted(modPath))
                throw new ArgumentException("Absolute mod path is required.", "modPath");
            _candidateRepository = new CastingPlanRepository(modPath);
            _legacyRepository = new ProfileRepository(modPath);
        }

        public CastingMigrationResult Migrate(
            string campaignId,
            IDictionary<string, CastGroupingKind> groupingsBySourceId = null,
            ClassicPlanInMemory legacyInMemory = null,
            CastingPlanDocument unsavedDocument = null)
        {
            if (string.IsNullOrWhiteSpace(campaignId))
                throw new ArgumentException("Exact campaign ID is required.", "campaignId");
            string legacyPath = _legacyRepository.GetProfilePath(campaignId);
            if (!File.Exists(legacyPath))
                return new CastingMigrationResult(
                    CastingMigrationStatus.LegacyAbsent, null, string.Empty,
                    string.Empty, string.Empty, string.Empty);
            string legacyBytes;
            try
            {
                legacyBytes = File.ReadAllText(legacyPath);
            }
            catch (Exception exception)
            {
                return new CastingMigrationResult(
                    CastingMigrationStatus.LegacyUnreadable, null, string.Empty,
                    string.Empty, string.Empty, "unreadable:" + exception.Message);
            }
            string legacyHash = Hashing.Sha256(legacyPath);
            BuffPlannerProfile legacy;
            try
            {
                legacy = JsonConvertDeserialize(legacyBytes, campaignId);
            }
            catch (Exception exception)
            {
                return new CastingMigrationResult(
                    CastingMigrationStatus.LegacyUnreadable, null, string.Empty,
                    legacyHash, string.Empty, "invalid:" + exception.Message);
            }
            // Re-review: the classic planner's own in-memory plan of this
            // campaign (its sources rebound to the party's current abilities)
            // is imported in place of the file's - only while the file is
            // still exactly the bytes that plan was read from or saved to
            // (focused re-review: a default made because the file could not
            // be read, or a plan read before the file changed, never stands
            // in for it). The file above still had to be readable, and it is
            // archived exactly and never written.
            if (legacyInMemory != null && legacyInMemory.Profile != null &&
                string.Equals(legacyInMemory.Profile.CampaignId, campaignId, StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(legacyInMemory.PrimarySha256) &&
                string.Equals(legacyInMemory.PrimarySha256, legacyHash, StringComparison.OrdinalIgnoreCase))
                legacy = legacyInMemory.Profile;
            // A newer-schema candidate must not be buried by a migration;
            // the operator resolves it explicitly first.
            CastingPlanLoadResult existingCandidate =
                _candidateRepository.Load(campaignId);
            if (existingCandidate.Status == CastingPlanLoadStatus.UnsupportedSchema)
                return new CastingMigrationResult(
                    CastingMigrationStatus.NewerCandidateRefused, null, string.Empty,
                    legacyHash, existingCandidate.SourcePath,
                    existingCandidate.Warning);
            CastingPlanDocument existingDocument =
                existingCandidate.Status == CastingPlanLoadStatus.Loaded ||
                existingCandidate.Status == CastingPlanLoadStatus.RecoveredFromBackup
                    ? existingCandidate.Profile.ToDocument()
                    : null;
            // Focused re-review: castings added in the session while no plan
            // file existed are imported into, never replaced.
            if (existingDocument == null && existingCandidate.Status == CastingPlanLoadStatus.Absent &&
                unsavedDocument != null &&
                string.Equals(unsavedDocument.CampaignId, campaignId, StringComparison.Ordinal))
                existingDocument = unsavedDocument;
            CastingImportResult imported = _importer.Import(
                legacy, existingDocument, groupingsBySourceId);
            string archivePath = ArchiveBoundaryOriginal(legacyPath, legacyBytes);
            // Write the candidate, reopen it, and revalidate before the
            // migration is reported complete; the legacy bytes are never
            // touched by any failure here.
            CastingPlanProfile candidate = CastingPlanProfile.FromDocument(
                imported.Document,
                legacy.Ui, legacy.Execution);
            try
            {
                _candidateRepository.Save(candidate);
            }
            catch (InvalidDataException exception)
            {
                // The repository refuses to bury an unreadable or newer
                // primary; the migration reports that as an unusable
                // candidate with recovery instructions instead of forcing
                // the write. The legacy original is untouched.
                return new CastingMigrationResult(
                    CastingMigrationStatus.CandidateUnusable, imported.Report,
                    archivePath, legacyHash, string.Empty,
                    "refused:" + exception.Message);
            }
            CastingPlanLoadResult reopened = _candidateRepository.Load(campaignId);
            if (reopened.Status != CastingPlanLoadStatus.Loaded)
                return new CastingMigrationResult(
                    CastingMigrationStatus.CandidateUnusable, imported.Report,
                    archivePath, legacyHash, reopened.SourcePath,
                    "reopen:" + reopened.Status + ":" + reopened.Warning);
            return new CastingMigrationResult(
                CastingMigrationStatus.Migrated, imported.Report, archivePath,
                legacyHash, reopened.SourcePath, string.Empty);
        }

        // A non-rotating, boundary-named archival copy: keyed by a bounded
        // prefix of the exact legacy content hash, so a later migration of
        // changed legacy bytes archives a new boundary instead of relying on
        // a much older original, while the file name stays far below
        // MAX_PATH even under deep settings directories.
        // Byte-exact (review of §7.1): the archive copies the file's raw
        // bytes — a BOM or non-UTF-8 encoding survives — and is keyed by the
        // same file hash the migration reports.
        private static string ArchiveBoundaryOriginal(
            string legacyPath, string legacyBytes)
        {
            string directory = Path.GetDirectoryName(legacyPath);
            string hash = Hashing.Sha256(legacyPath);
            string archive = Path.Combine(directory,
                "kbp-casting-" + (hash.Length <= 24 ? hash : hash.Substring(0, 24)) + ".orig");
            if (File.Exists(archive)) return archive;
            Directory.CreateDirectory(directory);
            AtomicFile.WriteBytes(archive, File.ReadAllBytes(legacyPath));
            return archive;
        }

        // The Classic loader's own reading of the primary Classic file (final
        // review B1): a file it reads is imported, including one written by a
        // released version with an older schema (0.0.19 wrote schema 4),
        // migrated in memory only; a file it refuses is not imported, and a
        // backup it would fall back to is not imported in its place. The
        // Classic file's bytes are never rewritten here.
        private static BuffPlannerProfile JsonConvertDeserialize(
            string json, string campaignId)
        {
            return ProfileRepository.ReadForImport(json, campaignId);
        }
    }
}
