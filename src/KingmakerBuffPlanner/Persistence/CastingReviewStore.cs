using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Persistence
{
    public sealed class CastingReviewStoreLoad
    {
        internal CastingReviewStoreLoad(IDictionary<string, string> accepted, string warning)
        {
            AcceptedDigests = new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(accepted ?? new Dictionary<string, string>(),
                    StringComparer.Ordinal));
            Warning = warning ?? string.Empty;
        }

        public IReadOnlyDictionary<string, string> AcceptedDigests { get; private set; }
        public string Warning { get; private set; }
    }

    // Accepted review state of the casting-first planner: per routine, the
    // SHA-256 digest of the material plan contents the player accepted. It
    // is kept apart from the plan document (never authored intent) so that
    // a missing or unreadable file only means the player accepts again, and
    // a restored digest authorizes nothing unless the freshly compiled plan
    // matches it exactly.
    public sealed class CastingReviewStore
    {
        internal const int SchemaVersion = 1;
        private readonly string _settingsDirectory;

        public CastingReviewStore(string modPath)
        {
            if (string.IsNullOrWhiteSpace(modPath) || !Path.IsPathRooted(modPath))
                throw new ArgumentException("Absolute mod path is required.", "modPath");
            _settingsDirectory = Path.Combine(Path.GetFullPath(modPath), "UserSettings");
        }

        public CastingReviewStoreLoad Load(string campaignId)
        {
            string path = PathFor(campaignId);
            if (!File.Exists(path))
                return new CastingReviewStoreLoad(null, string.Empty);
            try
            {
                JObject root = JObject.Parse(File.ReadAllText(path));
                if (root.Value<int?>("schemaVersion") != SchemaVersion)
                    return Refused("schema-version");
                if (!string.Equals(root.Value<string>("campaignId"), campaignId,
                        StringComparison.Ordinal))
                    return Refused("campaign-id-mismatch");
                var accepted = root["accepted"] as JObject;
                if (accepted == null) return Refused("accepted-missing");
                var result = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (JProperty property in accepted.Properties())
                {
                    string digest = property.Value.Type == JTokenType.String
                        ? (string)property.Value : null;
                    if (!IsDigest(digest)) return Refused("digest-invalid:" + property.Name);
                    result[property.Name] = digest;
                }
                return new CastingReviewStoreLoad(result, string.Empty);
            }
            catch (Exception exception)
            {
                return Refused("unreadable:" + exception.GetType().Name);
            }
        }

        public void Save(string campaignId, IReadOnlyDictionary<string, string> acceptedDigests)
        {
            string path = PathFor(campaignId);
            var accepted = new JObject();
            foreach (KeyValuePair<string, string> pair in (acceptedDigests ??
                    new Dictionary<string, string>())
                .OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (!IsDigest(pair.Value))
                    throw new ArgumentException("Invalid review digest.", "acceptedDigests");
                accepted[pair.Key] = pair.Value;
            }
            var root = new JObject
            {
                { "schemaVersion", SchemaVersion },
                { "campaignId", campaignId },
                { "accepted", accepted }
            };
            Directory.CreateDirectory(_settingsDirectory);
            AtomicFile.WriteUtf8(path, root.ToString(Formatting.Indented));
        }

        internal string PathFor(string campaignId)
        {
            if (string.IsNullOrWhiteSpace(campaignId) || campaignId.Length > 512)
                throw new ArgumentException("Exact campaign ID is required.", "campaignId");
            return Path.Combine(_settingsDirectory,
                "kingmaker-buff-planner-review-" + CastingPlanRepository.CampaignHashFor(campaignId) +
                ".json");
        }

        private static CastingReviewStoreLoad Refused(string reason)
        {
            return new CastingReviewStoreLoad(null, "review-state-ignored:" + reason);
        }

        private static bool IsDigest(string value)
        {
            return value != null && value.Length == 64 &&
                value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
        }
    }
}
