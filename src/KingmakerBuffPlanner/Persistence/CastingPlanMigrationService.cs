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
            IDictionary<string, CastGroupingKind> groupingsBySourceId = null)
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
        private static string ArchiveBoundaryOriginal(
            string legacyPath, string legacyBytes)
        {
            string directory = Path.GetDirectoryName(legacyPath);
            string archive = Path.Combine(directory,
                "kbp-casting-" + BoundaryKey(legacyBytes) + ".orig");
            if (File.Exists(archive)) return archive;
            Directory.CreateDirectory(directory);
            AtomicFile.WriteUtf8(archive, legacyBytes);
            return archive;
        }

        private static string BoundaryKey(string legacyBytes)
        {
            string hash = Hashing.Sha256Text(legacyBytes);
            return hash.Length <= 24 ? hash : hash.Substring(0, 24);
        }

        private static BuffPlannerProfile JsonConvertDeserialize(
            string json, string campaignId)
        {
            // Reuse the legacy repository's strict parsing by round-tripping
            // through its own loader contract: a profile that the old UI
            // could not load is not migrated.
            return ParseLegacy(json, campaignId);
        }

        private static BuffPlannerProfile ParseLegacy(string json, string campaignId)
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Error
            };
            BuffPlannerProfile profile = Newtonsoft.Json.JsonConvert
                .DeserializeObject<BuffPlannerProfile>(json, settings);
            if (profile == null ||
                profile.SchemaVersion != BuffPlannerProfile.CurrentSchemaVersion)
                throw new InvalidDataException("schema-version");
            if (!string.Equals(profile.CampaignId, campaignId,
                    StringComparison.Ordinal))
                throw new InvalidDataException("campaign-id-mismatch");
            return profile;
        }
    }
}
