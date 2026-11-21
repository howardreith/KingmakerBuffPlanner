using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KingmakerBuffPlanner.Discovery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // Missing-beneficial-buffs mission §3.D: the candidate audit. Every
        // ability of a guarded in-game catalogue export (loaded-blueprint
        // facts recorded by NativeCatalogExporter in the running game) is
        // classified again by the classifier compiled into this runner,
        // under BOTH rule sets (Current and Pre040) and in both scopes
        // (static: the native accessibility index; live: any ability a party
        // member has). Nothing is filtered: rows excluded under both rule
        // sets are kept, so a false negative cannot hide behind an audit
        // that lists only included or newly removed rows. The exports'
        // own historical dispositions are NOT read - only their facts.
        //
        // The report is written beside the build artifacts, named by the
        // SHA-256 of the classifier source it judged, so a baseline and a
        // repaired classifier produce two comparable files
        // (git diff --no-index). The exports live outside the repository
        // (the lab's guarded runtime-evidence root, or
        // KBP_CANDIDATE_AUDIT_EVIDENCE_ROOT); review R1-F9: these are
        // explicitly selected evidence checks (RunExplicit, "evidence-*"),
        // never part of an ordinary or no-prefix run, and they fail with the
        // missing path when a requested input is absent.
        internal const string LabCandidateAuditEvidenceRoot = @"C:\Dev\KingmakerBuffPlannerLab\runtime-evidence";

        internal static string CandidateAuditEvidenceRoot
        {
            get
            {
                string configured = Environment.GetEnvironmentVariable("KBP_CANDIDATE_AUDIT_EVIDENCE_ROOT");
                return string.IsNullOrWhiteSpace(configured) ? LabCandidateAuditEvidenceRoot : configured;
            }
        }

        internal const string FullUserExport = "kbp041-rel-catalog-full";

        private static void RunMissingBeneficialBuffsAuditTests()
        {
            RunExplicit("evidence-candidate-audit-missing-beneficial-buffs-native-only", () => AuditCatalogExport(
                "native-only", Path.Combine(CandidateAuditEvidenceRoot, "kbp040-rc4p-catalog-native", "native-buff-catalog.json")));
            RunExplicit("evidence-candidate-audit-missing-beneficial-buffs-full-user", () => AuditCatalogExport(
                "full-user", Path.Combine(CandidateAuditEvidenceRoot, FullUserExport, "native-buff-catalog.json")));
        }

        private static string ClassifierSourceLabel()
        {
            string path = Path.Combine(FindRepositoryRoot(), "src", "KingmakerBuffPlanner", "Discovery",
                "NativeCandidateClassifier.cs");
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(File.ReadAllBytes(path));
                return "classifier-" + BitConverter.ToString(hash).Replace("-", string.Empty)
                    .Substring(0, 12).ToLowerInvariant();
            }
        }

        private static void AuditCatalogExport(string profile, string exportPath)
        {
            if (!File.Exists(exportPath))
                throw new InvalidOperationException("Requested evidence input absent (set " +
                    "KBP_CANDIDATE_AUDIT_EVIDENCE_ROOT or run in the lab): " + exportPath);
            string label = ClassifierSourceLabel();
            string directory = Path.Combine(FindRepositoryRoot(), "artifacts", "candidate-audit");
            Directory.CreateDirectory(directory);
            string output = Path.Combine(directory, label + "-" + profile + "-all.tsv");
            string changesOutput = Path.Combine(directory, label + "-" + profile + "-changes.tsv");
            var current = new NativeCandidateClassifier();
            var pre040 = new NativeCandidateClassifier(NativeCandidateRuleSet.Pre040);
            var baseline = new Baseline.NativeCandidateClassifier();
            var baselinePre040 = new Baseline.NativeCandidateClassifier(Baseline.NativeCandidateRuleSet.Pre040);
            var lines = new List<string>();
            var changes = new List<string>();
            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            string generator = string.Empty;
            int abilities = 0;
            foreach (JObject entry in ReadExportAbilities(exportPath, value => generator = value))
            {
                abilities++;
                bool candidate = (bool?)entry["isCandidate"] ?? false;
                NativeCandidateAuditFacts live = AuditFactsFromExport(entry, true);
                NativeCandidateAuditFacts reachable = AuditFactsFromExport(entry, candidate);
                NativeCandidateAuditDecision liveNow = current.Classify(live);
                NativeCandidateAuditDecision livePre = pre040.Classify(live);
                NativeCandidateAuditDecision staticNow = current.Classify(reachable);
                NativeCandidateAuditDecision staticPre = pre040.Classify(reachable);
                Baseline.NativeCandidateAuditDecision liveBase = baseline.Classify(BaselineFacts(live));
                Baseline.NativeCandidateAuditDecision livePreBase = baselinePre040.Classify(BaselineFacts(live));
                Baseline.NativeCandidateAuditDecision staticBase = baseline.Classify(BaselineFacts(reachable));
                Baseline.NativeCandidateAuditDecision staticPreBase = baselinePre040.Classify(BaselineFacts(reachable));
                string manual = (string)entry["manualOverride"] ?? string.Empty;
                string[] judged =
                {
                    "live-current=" + liveBase.Disposition + ":" + liveBase.ReasonCode + ">" +
                        liveNow.Disposition + ":" + liveNow.ReasonCode,
                    "live-pre040=" + livePreBase.Disposition + ":" + livePreBase.ReasonCode + ">" +
                        livePre.Disposition + ":" + livePre.ReasonCode,
                    "static-current=" + staticBase.Disposition + ":" + staticBase.ReasonCode + ">" +
                        staticNow.Disposition + ":" + staticNow.ReasonCode,
                    "static-pre040=" + staticPreBase.Disposition + ":" + staticPreBase.ReasonCode + ">" +
                        staticPre.Disposition + ":" + staticPre.ReasonCode
                };
                bool changed = liveBase.Disposition != liveNow.Disposition ||
                    livePreBase.Disposition != livePre.Disposition ||
                    staticBase.Disposition != staticNow.Disposition ||
                    staticPreBase.Disposition != staticPre.Disposition;
                string key = "live-current:" + liveBase.Disposition + ":" + liveBase.ReasonCode + ">" +
                    liveNow.Disposition + ":" + liveNow.ReasonCode;
                int count;
                counts.TryGetValue(key, out count);
                counts[key] = count + 1;
                string line = string.Join("\t", new[]
                {
                    (string)entry["abilityGuid"],
                    Clean((string)entry["internalName"]),
                    Clean((string)entry["displayName"]),
                    (string)entry["ownership"] ?? string.Empty,
                    candidate ? "candidate" : "not-candidate",
                    (string)entry["range"] ?? string.Empty,
                    Flags(entry),
                    "ally=" + ((string)entry["effectOnAlly"] ?? string.Empty),
                    "enemy=" + ((string)entry["effectOnEnemy"] ?? string.Empty),
                    manual.Length == 0 ? "no-override" : "override:" + Clean(manual).Split(':')[0],
                    judged[0], judged[1], judged[2], judged[3],
                    "effects=" + string.Join(",", live.Effects.Select(e => e.Kind + ":" + e.Target + ":" +
                        (e.EffectName.Length == 0 ? e.EffectId : e.EffectName) +
                        (e.Harmful == true ? ":harmful" : string.Empty)).Distinct().ToArray()),
                    "payloads=" + string.Join(",", liveNow.Payloads.Select(e => e.EffectId).Distinct()
                        .OrderBy(v => v, StringComparer.Ordinal).ToArray()),
                    "baseline-reason=" + Truncate(Clean(liveBase.Reason), 140),
                    "repaired-reason=" + Truncate(Clean(liveNow.Reason), 140)
                });
                lines.Add(line);
                if (changed) changes.Add(line);
            }
            if (abilities == 0) throw new InvalidOperationException("The export held no abilities: " + exportPath);
            lines.Sort(StringComparer.Ordinal);
            changes.Sort(StringComparer.Ordinal);
            string header = "# candidate audit; profile=" + profile + "; export=" + exportPath + "; exportGenerator=" +
                generator + "; abilities=" + abilities.ToString(CultureInfo.InvariantCulture) + "; baseline=dd3e997 classifier;" +
                " repaired=" + label + "; columns: guid, internal name, display name, ownership, static scope," +
                " range, S/F/E/P/T(sticky)/V(variants), ally, enemy, override, baseline>repaired for live/static x Current/Pre040," +
                " effects, repaired payloads, reasons";
            File.WriteAllLines(output, new[] { header }.Concat(lines).ToArray(), new UTF8Encoding(false));
            File.WriteAllLines(changesOutput, new[] { header }.Concat(changes).ToArray(), new UTF8Encoding(false));
            Console.WriteLine("CANDIDATE-AUDIT " + profile + " abilities=" + abilities + " changed=" + changes.Count +
                " report=" + output + " changes=" + changesOutput);
            // Only the live-scope Current transitions that changed (the
            // complete per-row record, all four scopes, is in the reports).
            foreach (KeyValuePair<string, int> pair in counts)
            {
                string[] parts = pair.Key.Substring("live-current:".Length).Split('>');
                if (parts[0] != parts[1])
                    Console.WriteLine("CANDIDATE-AUDIT " + profile + " " + pair.Key + "=" + pair.Value);
            }
        }

        // The same facts for the frozen baseline classifier's own types.
        private static Baseline.NativeCandidateAuditFacts BaselineFacts(NativeCandidateAuditFacts facts)
        {
            return new Baseline.NativeCandidateAuditFacts
            {
                IsPlayerAccessible = facts.IsPlayerAccessible,
                CanTargetSelf = facts.CanTargetSelf,
                CanTargetFriends = facts.CanTargetFriends,
                CanTargetEnemies = facts.CanTargetEnemies,
                CanTargetPoint = facts.CanTargetPoint,
                HasVariants = facts.HasVariants,
                IsStickyTouch = facts.IsStickyTouch,
                EffectOnAlly = facts.EffectOnAlly,
                EffectOnEnemy = facts.EffectOnEnemy,
                Range = facts.Range,
                AbilityComponentTypes = facts.AbilityComponentTypes,
                Effects = (facts.Effects ?? new NativeCandidateEffectFacts[0]).Select(BaselineEffect).ToArray(),
                DiagnosticContracts = facts.DiagnosticContracts,
                Diagnostics = (facts.Diagnostics ?? new NativeCandidateDiagnosticFacts[0])
                    .Select(d => new Baseline.NativeCandidateDiagnosticFacts
                    {
                        Code = d.Code, Contract = d.Contract, Detail = d.Detail, ActionPath = d.ActionPath
                    }).ToArray(),
                SelfGatedFactIds = facts.SelfGatedFactIds
            };
        }

        private static Baseline.NativeCandidateEffectFacts BaselineEffect(NativeCandidateEffectFacts effect)
        {
            return new Baseline.NativeCandidateEffectFacts
            {
                EffectId = effect.EffectId,
                EffectName = effect.EffectName,
                Kind = effect.Kind,
                Target = effect.Target,
                Harmful = effect.Harmful,
                IsHiddenInUi = effect.IsHiddenInUi,
                IsClassFeature = effect.IsClassFeature,
                RemoveOnRest = effect.RemoveOnRest,
                StayOnDeath = effect.StayOnDeath,
                ComponentTypes = effect.ComponentTypes,
                GrantedConditions = effect.GrantedConditions,
                SourceContract = effect.SourceContract,
                ActionPath = effect.ActionPath,
                FactActions = (effect.FactActions ?? new NativeCandidateFactActions[0])
                    .Select(list => new Baseline.NativeCandidateFactActions
                    {
                        List = list.List,
                        AppliedEffects = (list.AppliedEffects ?? new NativeCandidateEffectFacts[0])
                            .Select(BaselineEffect).ToArray(),
                        Restorative = list.Restorative,
                        Offensive = list.Offensive,
                        Unrecognized = list.Unrecognized
                    }).ToArray()
            };
        }

        private static string Flags(JObject entry)
        {
            Func<string, string, string> flag = (name, label) => ((bool?)entry[name] ?? false) ? label : "-";
            return flag("canTargetSelf", "S") + flag("canTargetFriends", "F") + flag("canTargetEnemies", "E") +
                flag("canTargetPoint", "P") + flag("isStickyTouch", "T") +
                (((JArray)entry["variantGuids"] ?? new JArray()).Count != 0 ? "V" : "-");
        }

        private static string Clean(string value)
        {
            return (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string Truncate(string value, int length)
        {
            return value.Length <= length ? value : value.Substring(0, length);
        }

        // Streams the export's abilities array (the full-user export is
        // several hundred megabytes of JSON).
        private static IEnumerable<JObject> ReadExportAbilities(string path, Action<string> generator)
        {
            using (var text = new StreamReader(path, Encoding.UTF8))
            using (var reader = new JsonTextReader(text))
            {
                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.PropertyName || reader.Depth != 1) continue;
                    string name = (string)reader.Value;
                    if (name == "generatorCommit")
                    {
                        reader.Read();
                        generator((string)reader.Value ?? string.Empty);
                        continue;
                    }
                    if (name != "abilities") continue;
                    reader.Read();
                    if (reader.TokenType != JsonToken.StartArray)
                        throw new InvalidOperationException("Export abilities is not an array.");
                    while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                        if (reader.TokenType == JsonToken.StartObject)
                            yield return JObject.Load(reader);
                }
            }
        }

        // NativeCatalogExporter.AuditFacts, rebuilt from one exported entry:
        // the exact facts the in-game export recorded for the ability.
        private static NativeCandidateAuditFacts AuditFactsFromExport(JObject entry, bool accessible)
        {
            JArray diagnostics = (JArray)entry["diagnostics"] ?? new JArray();
            return new NativeCandidateAuditFacts
            {
                // The current versioned registry's evidence-backed
                // ally-disposition exceptions, as live discovery applies them.
                AllyDispositionExceptionEffectIds = MbbOverrides()
                    .AllyDispositionExceptions((string)entry["abilityGuid"]),
                IsPlayerAccessible = accessible,
                CanTargetSelf = (bool?)entry["canTargetSelf"] ?? false,
                CanTargetFriends = (bool?)entry["canTargetFriends"] ?? false,
                CanTargetEnemies = (bool?)entry["canTargetEnemies"] ?? false,
                CanTargetPoint = (bool?)entry["canTargetPoint"] ?? false,
                HasVariants = ((JArray)entry["variantGuids"] ?? new JArray()).Count != 0,
                IsStickyTouch = (bool?)entry["isStickyTouch"] ?? false,
                EffectOnAlly = (string)entry["effectOnAlly"] ?? string.Empty,
                EffectOnEnemy = (string)entry["effectOnEnemy"] ?? string.Empty,
                Range = (string)entry["range"] ?? string.Empty,
                AbilityComponentTypes = Strings(entry["abilityComponentTypes"]),
                Effects = (((JArray)entry["effects"]) ?? new JArray()).OfType<JObject>().Select(ExportEffectFacts).ToArray(),
                SelfGatedFactIds = Strings(entry["selfGatedFactIds"]),
                DiagnosticContracts = diagnostics.OfType<JObject>()
                    .Select(d => ((string)d["nodeIdentity"] ?? string.Empty) + "|" + ((string)d["detail"] ?? string.Empty))
                    .ToArray(),
                Diagnostics = diagnostics.OfType<JObject>().Select(d => new NativeCandidateDiagnosticFacts
                {
                    Code = (string)d["code"] ?? string.Empty,
                    Contract = (string)d["nodeIdentity"] ?? string.Empty,
                    Detail = (string)d["detail"] ?? string.Empty,
                    ActionPath = (string)d["actionPath"] ?? string.Empty
                }).ToArray()
            };
        }

        private static NativeCandidateEffectFacts ExportEffectFacts(JObject effect)
        {
            return new NativeCandidateEffectFacts
            {
                EffectId = (string)effect["effectGuid"] ?? string.Empty,
                EffectName = (string)effect["effectName"] ?? string.Empty,
                Kind = (string)effect["kind"] ?? string.Empty,
                Target = (string)effect["target"] ?? string.Empty,
                Harmful = (bool?)effect["harmful"],
                IsHiddenInUi = (bool?)effect["isHiddenInUi"] ?? false,
                IsClassFeature = (bool?)effect["isClassFeature"] ?? false,
                RemoveOnRest = (bool?)effect["removeOnRest"] ?? false,
                StayOnDeath = (bool?)effect["stayOnDeath"] ?? false,
                ComponentTypes = Strings(effect["componentTypes"]),
                GrantedConditions = Strings(effect["grantedConditions"]),
                // Absent from the 0.4.0/0.4.1 exports (review R6): empty.
                ModifierSigns = Strings(effect["modifierSigns"]),
                SourceContract = (string)effect["sourceContract"] ?? string.Empty,
                ActionPath = (string)effect["actionPath"] ?? string.Empty,
                FactActions = (((JArray)effect["factActions"]) ?? new JArray()).OfType<JObject>()
                    .Select(list => new NativeCandidateFactActions
                    {
                        List = (string)list["list"] ?? string.Empty,
                        AppliedEffects = (((JArray)list["appliedEffects"]) ?? new JArray()).OfType<JObject>()
                            .Select(ExportEffectFacts).ToArray(),
                        Restorative = Strings(list["restorative"]),
                        Offensive = Strings(list["offensive"]),
                        Unrecognized = Strings(list["unrecognized"])
                    }).ToArray()
            };
        }

        private static string[] Strings(JToken token)
        {
            JArray array = token as JArray;
            return array == null ? new string[0] : array.Select(value => (string)value ?? string.Empty).ToArray();
        }
    }
}
