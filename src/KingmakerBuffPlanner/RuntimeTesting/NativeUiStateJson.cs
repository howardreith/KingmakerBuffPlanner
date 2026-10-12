using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Game JSON defaults can suppress anonymous properties. These bounded
    // evidence snapshots write their primitive state explicitly instead.
    internal static class NativeUiStateJson
    {
        internal static JObject Casting(string id, string caster, string target,
            string status, string readiness, string provider)
        {
            return new JObject
            {
                { "CastingId", id }, { "CasterUnitId", caster }, { "TargetUnitId", target },
                { "StatusLabel", status }, { "Readiness", readiness }, { "SourceProviderKey", provider }
            };
        }

        internal static JObject HudButton(string name, bool interactable, float[] highlight)
        {
            return new JObject
            {
                { "name", name }, { "interactable", interactable },
                { "highlight", highlight == null ? (JToken)JValue.CreateNull()
                    : new JArray(highlight.Select(value => new JValue(value)).ToArray()) }
            };
        }
    }
}
