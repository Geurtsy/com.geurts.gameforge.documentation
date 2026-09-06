using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;

namespace Geurts.GameForge.Documentation
{
    internal interface IProjectInstallCommitStore
    {
        string Read();
        void Write(string commit);
    }

    internal sealed class ProjectInstallCommitStore : IProjectInstallCommitStore
    {
        private readonly string key;

        internal ProjectInstallCommitStore(string projectRoot)
        {
            if (projectRoot == null)
            {
                throw new ArgumentNullException(nameof(projectRoot));
            }

            string normalizedRoot = Path.GetFullPath(projectRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (Path.DirectorySeparatorChar == '\\')
            {
                normalizedRoot = normalizedRoot.ToUpperInvariant();
            }

            key = DocumentationPackageConstants.PackageName + ".last-successful-commit." +
                  ComputeSha256(normalizedRoot);
        }

        public string Read()
        {
            string value = EditorPrefs.GetString(key, string.Empty);
            return IsCommit(value) ? value.ToLowerInvariant() : null;
        }

        public void Write(string commit)
        {
            if (!IsCommit(commit))
            {
                throw new InvalidDataException("The last-successful commit identity is invalid.");
            }

            string normalizedCommit = commit.ToLowerInvariant();
            EditorPrefs.SetString(key, normalizedCommit);
            string persisted = EditorPrefs.GetString(key, string.Empty);
            if (!string.Equals(persisted, normalizedCommit, StringComparison.Ordinal))
            {
                throw new IOException("The per-project commit signal did not persist.");
            }
        }

        internal void ClearForTests()
        {
            EditorPrefs.DeleteKey(key);
        }

        private static string ComputeSha256(string value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                foreach (byte item in hash)
                {
                    builder.Append(item.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static bool IsCommit(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 40)
            {
                return false;
            }

            foreach (char character in value)
            {
                bool isDigit = character >= '0' && character <= '9';
                bool isLowerHex = character >= 'a' && character <= 'f';
                bool isUpperHex = character >= 'A' && character <= 'F';
                if (!isDigit && !isLowerHex && !isUpperHex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
