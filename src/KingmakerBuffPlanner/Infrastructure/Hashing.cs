using System.IO;
using System.Security.Cryptography;

namespace KingmakerBuffPlanner.Infrastructure
{
    internal static class Hashing
    {
        internal static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
            {
                return ToHex(hash.ComputeHash(stream));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            const string digits = "0123456789abcdef";
            var chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = digits[bytes[i] >> 4];
                chars[(i * 2) + 1] = digits[bytes[i] & 15];
            }

            return new string(chars);
        }
    }
}
