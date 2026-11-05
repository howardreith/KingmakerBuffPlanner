using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace KbpRuntimeHarness
{
    public sealed class ManifestFile
    {
        public string Path;
        public long Length;
        public string LastWriteTimeUtc;
        public string Sha256;
    }

    // Read every enumerated file, without a cache or exclusions. PowerShell
    // retains responsibility for enumeration, sorting and manifest serialization.
    public static class RuntimeManifestReader
    {
        public static ManifestFile[] Read(string root, FileInfo[] files)
        {
            var result = new ManifestFile[files.Length];
            var rootUri = new Uri(System.IO.Path.GetFullPath(root).TrimEnd('\\') + "\\");
            Parallel.For(0, files.Length, new ParallelOptions { MaxDegreeOfParallelism = 4 }, index =>
            {
                FileInfo file = files[index];
                string relative = Uri.UnescapeDataString(rootUri.MakeRelativeUri(new Uri(
                    System.IO.Path.GetFullPath(file.FullName))).ToString()).Replace('/', '\\');
                string digest;
                using (var stream = File.OpenRead(file.FullName))
                using (var sha = SHA256.Create())
                    digest = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                result[index] = new ManifestFile
                {
                    Path = relative, Length = file.Length,
                    LastWriteTimeUtc = file.LastWriteTimeUtc.ToString("o"), Sha256 = digest
                };
            });
            return result;
        }
    }
}
