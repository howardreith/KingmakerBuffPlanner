using System;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.Infrastructure;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Identity of a loader assembly actually bound in the running game (not the file on disk), so a runtime result
    // records the Harmony2 module the loader stack really used.
    internal static class RuntimeLoaderIdentity
    {
        internal const string NotLoaded = "not-loaded";

        internal static string LoadedVersion(string simpleName)
        {
            Assembly assembly = Find(simpleName);
            return assembly == null ? NotLoaded : assembly.GetName().Version.ToString();
        }

        internal static string LoadedSha256(string simpleName)
        {
            Assembly assembly = Find(simpleName);
            return assembly == null || string.IsNullOrEmpty(assembly.Location) ? NotLoaded : Hashing.Sha256(assembly.Location);
        }

        private static Assembly Find(string simpleName)
        {
            Assembly[] matches = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && string.Equals(a.GetName().Name, simpleName, StringComparison.Ordinal))
                .ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }
    }
}
