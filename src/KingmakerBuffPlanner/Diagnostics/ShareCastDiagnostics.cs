using System;
using System.Linq;
using System.Reflection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;
using Kingmaker.RuleSystem.Rules.Abilities;
using KingmakerBuffPlanner.Compatibility;

namespace KingmakerBuffPlanner.Diagnostics
{
    internal static class ShareCastDiagnostics
    {
        // Once per requested Instant routine. Inspect only assemblies already
        // loaded by the game; never introduce another provider or game load.
        internal static void Capture(Action<string> log, out string providerVersion,
            out bool directCapable, out string reason)
        {
            directCapable = BrownFurDirectCastCompatibility.TryValidateContract(out reason);
            Assembly[] providers = BrownFurDirectCastCompatibility.LoadedProviderAssemblies();
            providerVersion = providers.Length == 0 ? "not loaded" :
                string.Join(",", providers.Select(value =>
                    value.GetName().Version.ToString()).ToArray());
            log("[KBP-DIRECT-CAPABILITY] directCast=" + directCapable +
                ";directReason=" + (directCapable ? "contract-v1" : reason) +
                ";providerVersion=" + providerVersion +
                ";providerAssemblyCount=" + providers.Length);
            foreach (Assembly assembly in providers.Concat(new[] {
                typeof(ShareCastDiagnostics).Assembly }).Take(8))
                log("[KBP-LOADED-ASSEMBLY] " + Describe(assembly));
            foreach (Type type in new[] { typeof(AbilityData), typeof(TargetWrapper),
                typeof(RuleCastSpell) })
                log("[KBP-DIRECT-TYPE] expected=" + type.AssemblyQualifiedName +
                    ";mvid=" + type.Module.ModuleVersionId);
            if (directCapable) return;
            foreach (Assembly assembly in providers.Take(8))
            {
                try
                {
                    foreach (Type type in BrownFurDirectCastCompatibility.DiagnosticContractTypes(assembly))
                    {
                        foreach (MethodInfo method in type.GetMethods(
                            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                            BindingFlags.DeclaredOnly).Where(value => value.Name == "Validate" ||
                                value.Name == "Begin" || value.Name == "CompleteRule").Take(6))
                            log("[KBP-DIRECT-TYPE] actual=" + type.AssemblyQualifiedName +
                                "::" + method.Name + "(" + string.Join(",", method.GetParameters()
                                    .Select(value => value.ParameterType.AssemblyQualifiedName).ToArray()) +
                                ")->" + method.ReturnType.AssemblyQualifiedName);
                    }
                }
                catch (Exception exception)
                {
                    log("[KBP-DIRECT-TYPE] inspection-failed=" + exception.GetType().FullName);
                }
            }
        }

        private static string Describe(Assembly assembly)
        {
            try
            {
                return "identity=" + assembly.FullName +
                    ";loadedMvid=" + assembly.ManifestModule.ModuleVersionId +
                    ";location=" + assembly.Location;
            }
            catch (Exception exception)
            {
                return "identity=" + assembly.FullName +
                    ";metadata-failed=" + exception.GetType().FullName;
            }
        }
    }
}
