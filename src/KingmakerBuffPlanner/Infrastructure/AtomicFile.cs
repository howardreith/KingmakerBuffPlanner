using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace KingmakerBuffPlanner.Infrastructure
{
    internal static class AtomicFile
    {
        internal static void WriteUtf8(string path, string content)
        {
            WriteBytes(path, new UTF8Encoding(false).GetBytes(content ?? string.Empty));
        }

        // Exact bytes, written atomically (temporary file + replace/move).
        internal static void WriteBytes(string path, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A path is required.", "path");
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);

            string temporary = Path.Combine(
                directory,
                "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
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

        internal const int ArchiveAlternates = 9;

        // An archive of exact bytes, never trusted by its name alone (review
        // of rc4): a file already at the name is reused only when it holds
        // exactly these bytes; a different file there is kept, never
        // overwritten, and the bytes go to the next free numbered name
        // (stem.1.orig to stem.9.orig), then to the name keyed by the
        // bytes' own hash (so the names never run out: a readable settings
        // file must not turn unreadable because ten other originals were
        // archived); a new archive is read back and compared before its
        // path is returned.
        internal static string WriteExactArchive(string directory, string stem, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            Directory.CreateDirectory(directory);
            for (int index = 0; index <= ArchiveAlternates; index++)
            {
                string path = Path.Combine(directory, stem +
                    (index == 0 ? string.Empty : "." + index.ToString(CultureInfo.InvariantCulture)) + ".orig");
                if (File.Exists(path))
                {
                    if (SameBytes(File.ReadAllBytes(path), bytes)) return path;
                    continue;
                }
                WriteBytes(path, bytes);
                if (!SameBytes(File.ReadAllBytes(path), bytes))
                    throw new IOException("archive-read-back-differs:" + Path.GetFileName(path));
                return path;
            }
            string keyed = Path.Combine(directory, stem + ".sha256-" +
                Hashing.Sha256Bytes(bytes).Substring(0, 16) + ".orig");
            if (File.Exists(keyed))
            {
                if (SameBytes(File.ReadAllBytes(keyed), bytes)) return keyed;
                throw new IOException("archive-names-hold-other-content:" + stem);
            }
            WriteBytes(keyed, bytes);
            if (!SameBytes(File.ReadAllBytes(keyed), bytes))
                throw new IOException("archive-read-back-differs:" + Path.GetFileName(keyed));
            return keyed;
        }

        private static bool SameBytes(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++)
                if (left[index] != right[index]) return false;
            return true;
        }
    }
}
