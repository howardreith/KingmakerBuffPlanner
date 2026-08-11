using System;
using System.IO;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal static class RuntimePaths
    {
        internal static string GetGameRoot(string modEntryPath)
        {
            if (string.IsNullOrWhiteSpace(modEntryPath))
                throw new ArgumentException("The mod entry path is required.", "modEntryPath");
            string normalized = Path.GetFullPath(modEntryPath).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            var modDirectory = new DirectoryInfo(normalized);
            DirectoryInfo modsDirectory = modDirectory.Parent;
            if (modsDirectory == null ||
                !string.Equals(modsDirectory.Name, "Mods", StringComparison.OrdinalIgnoreCase) ||
                modsDirectory.Parent == null)
                throw new InvalidDataException("The mod entry path is not an immediate child of the game Mods directory.");
            return modsDirectory.Parent.FullName;
        }
    }
}
