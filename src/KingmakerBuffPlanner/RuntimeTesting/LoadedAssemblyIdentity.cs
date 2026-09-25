using System;
using System.IO;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Resolves the canonical assembly file for identity hashing. Mono may
    // serve a persisted assembly-image sidecar ("Assembly.dll.12345.cache")
    // generated from the same assembly on an earlier run; compatibility
    // profiles pin the canonical assembly bytes, so hashing must target the
    // canonical file when the runtime reports a sidecar location.
    internal static class LoadedAssemblyIdentity
    {
        internal static string ResolveCanonicalFile(string loadedLocation)
        {
            if (string.IsNullOrEmpty(loadedLocation)) return loadedLocation;
            if (!loadedLocation.EndsWith(".cache", StringComparison.OrdinalIgnoreCase))
                return loadedLocation;
            string withoutCache = loadedLocation.Substring(
                0, loadedLocation.Length - ".cache".Length);
            int dot = withoutCache.LastIndexOf('.');
            if (dot <= 0 || dot == withoutCache.Length - 1) return loadedLocation;
            string digits = withoutCache.Substring(dot + 1);
            for (int i = 0; i < digits.Length; i++)
                if (digits[i] < '0' || digits[i] > '9') return loadedLocation;
            string canonical = withoutCache.Substring(0, dot);
            return File.Exists(canonical) ? canonical : loadedLocation;
        }
    }
}
