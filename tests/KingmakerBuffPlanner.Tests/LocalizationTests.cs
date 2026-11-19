using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private static void RunLocalizationTests(string root)
        {
            Run("localization-selection", () =>
            {
                foreach (string game in new[] { null, "enGB", "zhCN", "deDE", "zhTW" })
                {
                    foreach (string pref in new[] { null, "automatic", "future", "enGB", "zhCN" })
                    {
                        string expected = pref == "enGB" || pref == "zhCN" ? pref : game == "zhCN" ? "zhCN" : "enGB";
                        Expect(PlannerLanguage.Resolve(pref, game) == expected, "Incorrect preference resolution.");
                    }
                }
            });
            Run("localization-catalog-and-fallback", () =>
            {
                var warnings = new List<string>();
                PlannerText.Initialize(warnings.Add);
                Expect(warnings.Count == 0, string.Join("\n", warnings.ToArray()));
                const string english = "{\"count\":\"<b>{0}</b> used\",\"name\":\"Name\"}";
                foreach (string chinese in new[] { "{}", "{", "{\"count\":\"\",\"name\":\"名\"}",
                    "{\"count\":\"{1}\",\"name\":\"名\"}", "{\"count\":\"<b>{0</b>\"}",
                    "{\"count\":\"<i>{0}</i>\"}", "{\"count\":\"<b>{0}</b><broken\"}",
                    "{\"name\":\"名\",\"name\":\"名称\"}" })
                {
                    warnings.Clear();
                    var catalog = new MessageCatalog(new StringReader(english), new StringReader(chinese), warnings.Add);
                    Expect(warnings.Count != 0 && catalog.Format("zhCN", "count", 2) == "<b>2</b> used", "Missing fallback.");
                }
                bool rejected = false;
                try { new MessageCatalog(new StringReader("{\"x\":\"1\",\"x\":\"2\"}"), new StringReader("{}"), warnings.Add); }
                catch (ArgumentException) { rejected = true; }
                Expect(rejected, "Duplicate English keys accepted.");
            });
            Run("localization-counts-reasons-search", () => WithChinese(() =>
            {
                foreach (int count in new[] { 0, 1, 7 })
                {
                    var hint = WorkspaceFooterTooltips.ClearConfirm(count);
                    Expect(hint.Title == "清空计划" && hint.Body.Contains(count.ToString()) && hint.Body.Contains("撤销"), hint.Text);
                }
                Expect(WorkspaceReasonText.Describe("caster-unresolved:unit-a") == "尚未选择施法者", "Reason mapping.");
                foreach (string native in new[] { "法师护甲", "Mage Armor", "Magische Rüstung", "Third Party Ward" })
                {
                    var option = new WorkspaceSourceOption("ability-guid", native, false, "");
                    Expect(WorkspaceSourceLabels.Matches(option, native.Substring(0, 2)) &&
                        !WorkspaceSourceLabels.Matches(option, "不存在"), "Displayed name search.");
                }
            }));
            Run("localization-plan-edit-round-trip", () => TestLanguagePlan(root));
        }

        private static void WithChinese(Action action)
        {
            string prior = PlannerText.Language;
            try { PlannerText.Language = "zhCN"; action(); }
            finally { PlannerText.Language = prior; }
        }

        private static void TestLanguagePlan(string root)
        {
            string directory = Path.Combine(root, "language-switch");
            const string campaign = "language-campaign";
            var repo = new CastingPlanRepository(directory);
            var profile = CastingPlanProfile.CreateDefault(campaign);
            // This represents a plan written by the older strict 6.5 writer.
            profile.FormatRevision = 5;
            foreach (var stored in profile.Routines) stored.HasDefaultCaption = false;
            profile.Routines[0].Name = "黎明・Linzi";
            repo.Save(profile);
            var defaults = CastingPlanProfile.CreateDefault("fresh-campaign");
            repo.Save(defaults);
            WithChinese(() => {
                var fresh = new CastingWorkspaceSession(directory, "fresh-campaign");
                Expect(fresh.RoutineDisplayName("long") == PlannerText.Routine("long"), "Default provenance lost on reload.");
            });
            var session = new CastingWorkspaceSession(directory, campaign);
            PartyProviderSnapshot snapshot;
            var inputs = WorkspaceInputs(out snapshot);
            session.SelectRoutine("long");
            session.Draft.SourceId = "source-bulls";
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.DirectTargetUnitId = "unit-t1";
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.Draft.State = CastingAuthoringState.Ready;
            Expect(session.AddCastingFromDraft(inputs).Applied, "English plan edit failed.");
            string before = session.DocumentIntentSignature();
            byte[] bytes = File.ReadAllBytes(repo.GetProfilePath(campaign));
            var reopened = new CastingWorkspaceSession(directory, campaign);
            WithChinese(() =>
            {
                reopened.InvalidatePresentation();
                var graph = reopened.BuildGraph(inputs);
                Expect(reopened.RoutineDisplayName("short") == "Short", "An existing literal default-like name was renamed.");
                Expect(graph.SelectedRoutineName == "黎明・Linzi" && !reopened.CanUndo && !reopened.IsDirty &&
                    reopened.DocumentIntentSignature() == before && bytes.SequenceEqual(File.ReadAllBytes(repo.GetProfilePath(campaign))),
                    "Presentation altered saved intent or edit history.");
                reopened.FocusGraphCasting(reopened.Document.Castings.Single().CastingId);
                Expect(reopened.SetFocusedRecastPolicy(ExistingEffectPolicy.Overwrite).Applied, "Chinese policy edit failed.");
            });
            reopened.InvalidatePresentation();
            var english = new CastingWorkspaceSession(directory, campaign);
            Expect(english.DocumentIntentSignature() == reopened.DocumentIntentSignature() &&
                english.DocumentIntentSignature() != before && english.RoutineDisplayName("long") == "黎明・Linzi",
                "English reopen lost the legitimate edit or Unicode name.");
            var after = repo.Load(campaign).Profile;
            var original = Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            var updated = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(repo.GetProfilePath(campaign)));
            var oldCasting = (Newtonsoft.Json.Linq.JObject)original["castings"][0];
            var newCasting = (Newtonsoft.Json.Linq.JObject)updated["castings"][0];
            string[] changed = oldCasting.Properties().Where(prop =>
                !Newtonsoft.Json.Linq.JToken.DeepEquals(prop.Value, newCasting[prop.Name])).Select(prop => prop.Name).ToArray();
            Expect(changed.Length == 1 && after.Castings.Count == 1, "Unexpected casting fields changed: " + string.Join(",", changed));
            oldCasting[changed[0]] = newCasting[changed[0]].DeepClone();
            Expect(Newtonsoft.Json.Linq.JToken.DeepEquals(original, updated), "Presentation or edit changed unrelated plan fields.");
        }
    }
}
