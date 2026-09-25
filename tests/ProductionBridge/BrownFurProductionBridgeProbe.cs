using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;

internal static class BrownFurProductionBridgeProbe
{
    private static string[] _roots;
    private static int _passes;

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 5) throw new ArgumentException(
                "Expected planner DLL, provider DLL (or missing), game root, expected reason (or accepted), and diagnostics requirement.");
            string planner = Path.GetFullPath(args[0]);
            string game = Path.GetFullPath(args[2]);
            _roots = new[] { Path.GetDirectoryName(planner),
                Path.Combine(game, "Kingmaker_Data", "Managed"),
                Path.Combine(game, "Kingmaker_Data", "Managed", "UnityModManager"),
                Path.Combine(game, "Mods", "CallOfTheWild") };
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            Console.WriteLine("Runtime=detached .NET Framework; machine=" +
                Environment.MachineName + "; clr=" + Environment.Version);
            Assembly product = Assembly.LoadFrom(planner);
            Describe(product);
            if (args[1] != "missing")
            {
                Assembly provider = Assembly.LoadFrom(Path.GetFullPath(args[1]));
                Describe(provider);
                Type api = provider.GetType(
                    "KingmakerGunslinger.BrownFur.BrownFurDirectCastApi", false);
                if (api != null)
                    foreach (MethodInfo method in api.GetMethods(
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                        Console.WriteLine("Actual contract: " + method.Name +
                            "(" + string.Join(",", method.GetParameters()
                                .Select(p => p.ParameterType.AssemblyQualifiedName)) +
                            ") -> " + method.ReturnType.AssemblyQualifiedName);
            }
            Check(product, args[3], args[4] == "diagnostics");
            Console.WriteLine("Production bridge: PASS=" + _passes + " FAIL=0; gameplay=NOT VERIFIED");
            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            Console.WriteLine("Production bridge: PASS=" + _passes + " FAIL=1");
            return 1;
        }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name + ".dll";
        foreach (string root in _roots)
        {
            string path = Path.Combine(root, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    private static void Describe(Assembly assembly)
    {
        using (var hash = SHA256.Create())
        using (var input = File.OpenRead(assembly.Location))
            Console.WriteLine("Assembly=" + assembly.FullName + "; path=" +
                assembly.Location + "; mvid=" + assembly.ManifestModule.ModuleVersionId +
                "; sha256=" + BitConverter.ToString(hash.ComputeHash(input))
                    .Replace("-", "").ToLowerInvariant());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Check(Assembly product, string expected, bool requireDiagnostics)
    {
        Type bridge = product.GetType(
            "KingmakerBuffPlanner.Compatibility.BrownFurDirectCastCompatibility", true);
        object[] output = { null };
        bool accepted = (bool)bridge.GetMethod("TryValidateContract",
            BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, output);
        string reason = (string)output[0];
        Console.WriteLine("Production TryValidateContract: accepted=" +
            accepted + "; reason=" + reason);
        bool intended = expected == "accepted";
        Assert(accepted == intended && (intended ? reason == "" : reason == expected),
            "Real production capability result and exact rejection reason");
        foreach (string name in new[] { "Kingmaker.UnitLogic.Abilities.AbilityData",
            "Kingmaker.Utility.TargetWrapper", "Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell" })
        {
            Type expectedType = Assembly.Load("Assembly-CSharp").GetType(name, true);
            Console.WriteLine("Expected game type=" + expectedType.AssemblyQualifiedName +
                "; mvid=" + expectedType.Module.ModuleVersionId);
        }

        if (requireDiagnostics)
        {
            var messages = new List<string>();
            object[] captured = { (Action<string>)messages.Add, null, null, null };
            product.GetType("KingmakerBuffPlanner.Diagnostics.ShareCastDiagnostics", true)
                .GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, captured);
            Assert((bool)captured[2] == accepted && (string)captured[3] == reason,
                "Diagnostic capture reports actual production capability");
            Assert(messages.Any(value => value.Contains("[KBP-LOADED-ASSEMBLY]") &&
                value.Contains(product.ManifestModule.ModuleVersionId.ToString())) &&
                messages.Any(value => value.StartsWith("[KBP-DIRECT-CAPABILITY]", StringComparison.Ordinal) &&
                    value.Contains("directCast=" + accepted)),
                "Diagnostic capture identifies the actual loaded product");
        }
        const string book = "0c21cfcab6ce4395bd4df330ab3cf715";
        const string resinous = "41ceee31b77741e99d3b0990bbe40a2a";
        CastEnhancementSnapshot share = (CastEnhancementSnapshot)Snapshot(product,
            "BrownFurShareTransmutationCompatibility", new object[] {
                "brown", null, 8, new[] { resinous }, new[] { book } });
        Type profile = product.GetType(
            "KingmakerBuffPlanner.Compatibility.BrownFurPowerfulChangeProfile", true);
        object toggle = ((IEnumerable)profile.GetProperty("Toggles",
            BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null))
            .Cast<object>().First();
        CastEnhancementSnapshot powerful = (CastEnhancementSnapshot)Snapshot(product,
            "BrownFurPowerfulChangeCompatibility", new object[] {
                "brown", toggle, null, 8, new[] { resinous } });
        foreach (CastEnhancementSnapshot value in new[] { share, powerful })
        {
            Assert(value.RequiresNativeCommand == !intended &&
                value.DirectCastProviderId == (intended ? "brown-fur-direct-cast-v1" : ""),
                "Production snapshot: " + value.DisplayName);
        }

        // Engine-owned spell eligibility and target legality are inputs here.
        // This is composition coverage, not a claim Resinous Skin qualifies for Powerful Change.
        foreach (CastEnhancementSnapshot[] selected in new[] {
            new[] { share }, new[] { share, powerful } })
        {
            var ability = new AbilityKey(resinous, "", 0, SourceKind.Spellbook, "");
            var provider = new ProviderSnapshot(new ProviderKey("brown", book, ability, ""),
                "Probe source", 4, "slots", 1, null);
            var source = new BuffSourceDefinition("probe", ability,
                new EmptyEffectExpression(), CastGroupingKind.PerTarget);
            var request = new BuffCastRequest(source, new[] { "ally" },
                ExistingEffectPolicy.Overwrite, null, selected.Select(s => s.EnhancementId));
            var snapshot = new PartyProviderSnapshot(new UnitSnapshot[0],
                new ProviderSnapshot[0], new ResourcePoolSnapshot[0]);
            var options = new EffectiveProviderOptionResolver().Resolve(snapshot, request,
                new[] { new ProviderPlanningOption(provider, new[] { "ally" },
                    new[] { "ally" }, 12, 120) }, selected);
            Assert(options.Count == 1 && options[0].ExecutionStrategy ==
                (intended ? CastExecutionStrategy.ProviderDirectRuleCast :
                    CastExecutionStrategy.NativeCommandRequired),
                "Production resolver after native eligibility: selected=" + selected.Length);
        }
    }

    private static object Snapshot(Assembly product, string type, object[] args)
    {
        return product.GetType("KingmakerBuffPlanner.Compatibility." + type, true)
            .GetMethod("Snapshot", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, args);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _passes++;
        Console.WriteLine("PASS " + message);
    }
}
