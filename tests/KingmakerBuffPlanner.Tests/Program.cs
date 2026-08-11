using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using KingmakerBuffPlanner.RuntimeTesting;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Tests
{
    internal static class Program
    {
        private static int _passed;
        private static readonly List<string> Failures = new List<string>();

        private static int Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveInstalledAssembly;
            string root = Path.Combine(
                RuntimeTestProtocol.EvidenceRoot,
                "source-only-protocol-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                Run("absent-activation-is-inert", TestAbsentActivation);
                Run("valid-request-is-accepted", () => TestValidRequest(root));
                Run("duplicate-flag-rejected", () => TestDuplicateFlag(root));
                Run("outside-path-rejected", TestOutsidePath);
                Run("unknown-member-rejected", () => TestMutation(root, "unknown-member", AddUnknownMember));
                Run("duplicate-member-rejected", () => TestDuplicateMember(root));
                Run("wrong-scenario-rejected", () => TestMutation(root, "wrong-scenario", o => o["scenario"] = "unknown"));
                Run("wrong-version-rejected", () => TestMutation(root, "wrong-version", o => o["expectedModVersion"] = "9.9.9"));
                Run("wrong-commit-rejected", () => TestMutation(root, "wrong-commit", o => o["expectedCommit"] = "WRONG"));
                Run("invalid-hash-rejected", () => TestMutation(root, "invalid-hash", o => o["expectedDllSha256"] = "not-a-hash"));
                Run("parameters-rejected", () => TestMutation(root, "parameters", o => o["parameters"] = new Dictionary<string, object> { { "saveName", "KBP_AUTOMATION_BASELINE" } }));
                Run("result-reuse-rejected", () => TestResultReuse(root));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }

            Console.WriteLine("Protocol tests: PASS=" + _passed + " FAIL=" + Failures.Count);
            foreach (string failure in Failures) Console.WriteLine("FAIL " + failure);
            return Failures.Count == 0 ? 0 : 1;
        }

        private static Assembly ResolveInstalledAssembly(object sender, ResolveEventArgs args)
        {
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            if (string.IsNullOrWhiteSpace(game)) return null;
            string name = new AssemblyName(args.Name).Name + ".dll";
            string path = Path.Combine(game, "Kingmaker_Data", "Managed", name);
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }

        private static void Run(string name, Action action)
        {
            try
            {
                action();
                _passed++;
            }
            catch (Exception exception)
            {
                Failures.Add(name + ": " + exception.Message);
            }
        }

        private static void TestAbsentActivation()
        {
            string rejection;
            if (RuntimeTestProtocol.TryRead(new[] { "Kingmaker.exe" }, out rejection) != null || rejection.Length != 0)
                throw new InvalidOperationException("Ordinary launch was not inert.");
        }

        private static void TestValidRequest(string root)
        {
            string path = WriteRequest(root, "valid", null);
            string rejection;
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 || request.RunId != "valid")
                throw new InvalidOperationException("Valid request was rejected: " + rejection);
        }

        private static void TestDuplicateFlag(string root)
        {
            string path = WriteRequest(root, "duplicate-flag", null);
            AssertRejected(new[]
            {
                "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path,
                RuntimeTestProtocol.ActivationFlag, path
            });
        }

        private static void TestOutsidePath()
        {
            AssertRejected(new[]
            {
                "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag,
                Path.Combine(Path.GetTempPath(), "outside-request.json")
            });
        }

        private static void TestMutation(
            string root,
            string runId,
            Action<Dictionary<string, object>> mutation)
        {
            string path = WriteRequest(root, runId, mutation);
            AssertRejected(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path });
        }

        private static void AddUnknownMember(Dictionary<string, object> request)
        {
            request["unexpected"] = true;
        }

        private static void TestDuplicateMember(string root)
        {
            string path = Path.Combine(root, "duplicate-member-request.json");
            string json = JsonConvert.SerializeObject(NewRequest(root, "duplicate-member"));
            json = json.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1");
            File.WriteAllText(path, json);
            AssertRejected(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path });
        }

        private static void TestResultReuse(string root)
        {
            string path = WriteRequest(root, "reuse", null);
            File.WriteAllText(Path.Combine(root, "runtime-result.json"), "{}");
            try
            {
                AssertRejected(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path });
            }
            finally
            {
                File.Delete(Path.Combine(root, "runtime-result.json"));
            }
        }

        private static void AssertRejected(string[] args)
        {
            string rejection;
            if (RuntimeTestProtocol.TryRead(args, out rejection) != null || string.IsNullOrWhiteSpace(rejection))
                throw new InvalidOperationException("Invalid request was not rejected.");
        }

        private static string WriteRequest(
            string root,
            string runId,
            Action<Dictionary<string, object>> mutation)
        {
            Dictionary<string, object> request = NewRequest(root, runId);
            if (mutation != null) mutation(request);
            string path = Path.Combine(root, runId + "-request.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(request));
            return path;
        }

        private static Dictionary<string, object> NewRequest(string root, string runId)
        {
            return new Dictionary<string, object>
            {
                { "schemaVersion", 1 },
                { "enabled", true },
                { "runId", runId },
                { "scenario", "mod-load-smoke" },
                { "expectedModVersion", "0.0.1" },
                { "expectedCommit", "TEST-COMMIT" },
                { "evidenceDirectory", root },
                { "expectedPackageSha256", new string('a', 64) },
                { "expectedDllSha256", new string('b', 64) },
                { "timeoutSeconds", 30 },
                { "exitAfterCompletion", false },
                { "parameters", new Dictionary<string, object>() }
            };
        }
    }
}
