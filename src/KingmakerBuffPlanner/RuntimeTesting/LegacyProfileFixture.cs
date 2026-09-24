using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // A genuine schema-4 Classic document, as the released 0.0.19 wrote it
    // (flat target and enhancement lists on each source assignment, no
    // casting children), made from a current schema-5 document. It seeds
    // the live import scenario so the first open meets a real upgrade (final
    // review B1). Test support only.
    internal static class LegacyProfileFixture
    {
        internal static string ToSchema4Json(string schema5Json)
        {
            JObject document = JObject.Parse(schema5Json);
            JToken schema = document["schemaVersion"];
            if (schema == null || schema.Type != JTokenType.Integer || (int)schema != 5)
                throw new InvalidDataException("schema-5-expected");
            document["schemaVersion"] = 4;
            foreach (JObject routine in ((JArray)document["routines"]).OfType<JObject>())
                foreach (JObject assignment in ((JArray)routine["assignments"]).OfType<JObject>())
                {
                    JArray children = assignment["castingAssignments"] as JArray;
                    JObject child = children != null && children.Count > 0 ? children[0] as JObject : null;
                    assignment["wantedTargetUnitIds"] = child == null || child["targetUnitIds"] == null
                        ? new JArray() : child["targetUnitIds"];
                    JArray enhancements = child == null ? null : child["enhancements"] as JArray;
                    assignment["selectedEnhancementIds"] = new JArray((enhancements ?? new JArray())
                        .OfType<JObject>().Select(selection => selection["enhancementId"]));
                    assignment.Remove("castingAssignments");
                }
            return document.ToString(Formatting.Indented) + Environment.NewLine;
        }
    }
}
