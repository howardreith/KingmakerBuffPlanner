using System;
using System.IO;
using System.Text;

namespace KingmakerBuffPlanner.Infrastructure
{
    internal static class AtomicFile
    {
        internal static void WriteUtf8(string path, string content)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A path is required.", "path");
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);

            string temporary = Path.Combine(
                directory,
                "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(content ?? string.Empty);
                using (var stream = new FileStream(
                    temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
