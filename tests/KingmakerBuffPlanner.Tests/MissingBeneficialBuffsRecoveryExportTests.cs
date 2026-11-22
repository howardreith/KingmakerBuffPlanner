using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.GameAdapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    // Missing-beneficial-buffs recovery, R12-F1 (export and replay parity).
    // The catalogue entry is built by the production exporter
    // (NativeCatalogExporter.BuildEntry, the per-ability step of Export())
    // from the committed native blueprint, serialized with the runtime
    // host's catalogue settings, and read back by the candidate audit's
    // reader (AuditFactsFromExport). The fb075cb behavior was established by
    // the baseline probe recorded in
    // planning/MISSING-BENEFICIAL-BUFFS-RECOVERY-LEDGER.md (BuildEntry did not
    // exist there; Export() needs the loaded game library).
    internal static partial class Program
    {
        private static void RunMissingBeneficialBuffsRecoveryExportTests()
        {
            Run("missing-beneficial-buffs-recovery-r12f1-on-cast-fact-survives-export-and-replay", TestMbrExportReplay);
        }

        private static JObject MbrExportedEntry(NativeBlueprintDump dump, string guid)
        {
            var exporter = new NativeCatalogExporter(MbbOverrides());
            NativeCatalogEntry entry = exporter.BuildEntry(dump.Get<BlueprintAbility>(guid), new[] { "fixture-source" },
                new NativeSpellListRecord[0], new NativeCandidateClassifier(),
                new NativeCandidateClassifier(NativeCandidateRuleSet.Pre040), new KingmakerActionGraphAdapter(),
                new ActionGraphScanner(), dump.Lookup);
            string json = JsonConvert.SerializeObject(entry, Formatting.Indented, new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                TypeNameHandling = TypeNameHandling.None,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
            });
            return JObject.Parse(json);
        }

        // (a) modified Feather Step, Mass (robustness test): the damaging
        // on-cast action. The exported entry carries the on-cast diagnostic
        // with its contract, detail and action path; replaying the export
        // reproduces the live and static exclusions. (b) native control:
        // unmodified Feather Step, Mass replays as included. (c) a historical
        // entry (no marker) is reported as lacking the on-cast facts.
        private static void TestMbrExportReplay()
        {
            string feather = MbbPointGroupSpells[1][1], featherBuff = MbbPointGroupSpells[1][2];
            var current = new NativeCandidateClassifier();
            MbbSubcases(
                MbbCase("modified: damaging on-cast action", () =>
                {
                    var dump = new NativeBlueprintDump(feather, featherBuff);
                    BlueprintAbility spell = dump.Get<BlueprintAbility>(feather);
                    var onCast = NativeObject<Kingmaker.UnitLogic.Abilities.Components.AbilityExecuteActionOnCast>();
                    SetNative(onCast, "Actions", new Kingmaker.ElementsSystem.ActionList
                    {
                        Actions = new Kingmaker.ElementsSystem.GameAction[]
                        {
                            NativeObject<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage>()
                        }
                    });
                    SetNative(spell, "Components", spell.ComponentsArray.Concat(
                        new Kingmaker.Blueprints.BlueprintComponent[] { onCast }).ToArray());
                    JObject entry = MbrExportedEntry(dump, feather);
                    JObject diagnostic = ((JArray)entry["diagnostics"]).OfType<JObject>()
                        .FirstOrDefault(d => (string)d["code"] == NativeCatalogExporter.OnCastActionCode);
                    MbbExpect(diagnostic != null && ExportRecordsOnCastFacts(entry) &&
                        (string)diagnostic["nodeIdentity"] ==
                            typeof(Kingmaker.UnitLogic.Abilities.Components.AbilityExecuteActionOnCast).FullName &&
                        ((string)diagnostic["detail"]).Contains("ContextActionDealDamage") &&
                        (string)diagnostic["actionPath"] == "on-cast",
                        "The exported entry lost the on-cast diagnostic: " + entry["diagnostics"]);
                    NativeCandidateAuditDecision live = current.Classify(AuditFactsFromExport(entry, true));
                    NativeCandidateAuditDecision reachable = current.Classify(AuditFactsFromExport(entry,
                        (bool)entry["isCandidate"]));
                    MbbExpect(live.Disposition == "exclude" && live.Disposition == (string)entry["liveDisposition"] &&
                        reachable.Disposition == (string)entry["disposition"] &&
                        live.ReasonCode == ((string)entry["liveDispositionReason"]).Split(':')[0],
                        "Replaying the export no longer reproduces the exporter's verdicts: replay live=" +
                        live.Disposition + " (" + live.Reason + ") export live=" + entry["liveDisposition"] +
                        " export static=" + entry["disposition"]);
                }),
                MbbCase("native control: Feather Step, Mass", () =>
                {
                    JObject entry = MbrExportedEntry(new NativeBlueprintDump(feather, featherBuff), feather);
                    NativeCandidateAuditDecision live = current.Classify(AuditFactsFromExport(entry, true));
                    MbbExpect(live.Disposition == "include" && (string)entry["liveDisposition"] == "include" &&
                        ExportRecordsOnCastFacts(entry) && !((JArray)entry["diagnostics"]).OfType<JObject>()
                            .Any(d => (string)d["code"] == NativeCatalogExporter.OnCastActionCode),
                        "Unmodified Feather Step, Mass no longer exports and replays as included: " + live.Reason);
                }),
                MbbCase("historical entry stays explicitly incomplete", () =>
                {
                    JObject entry = MbrExportedEntry(new NativeBlueprintDump(feather, featherBuff), feather);
                    entry.Remove("onCastFactsRecorded");
                    MbbExpect(!ExportRecordsOnCastFacts(entry),
                        "An entry without the marker was taken to record its on-cast facts.");
                }));
        }
    }
}
