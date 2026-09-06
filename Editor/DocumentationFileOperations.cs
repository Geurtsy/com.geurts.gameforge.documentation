using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Geurts.GameForge.Documentation
{
    internal static class DocumentationFileOperations
    {
        internal static string GetSafeFullPath(string root, string projectRelativePath)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("A root path is required.", nameof(root));
            }

            ValidateRelativePath(projectRelativePath);

            string fullRoot = Path.GetFullPath(root);
            string combined = Path.GetFullPath(Path.Combine(
                fullRoot,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));

            string rootedPrefix = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                  Path.DirectorySeparatorChar;
            if (!combined.StartsWith(rootedPrefix, PathComparison) &&
                !string.Equals(combined, fullRoot, PathComparison))
            {
                throw new InvalidDataException("A managed path escaped its allowed root: " + projectRelativePath);
            }

            return combined;
        }

        internal static void ValidateRelativePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new InvalidDataException("Managed paths must be non-empty relative paths.");
            }

            string normalized = relativePath.Replace('\\', '/');
            if (Path.IsPathRooted(relativePath) ||
                normalized.StartsWith("/", StringComparison.Ordinal) ||
                normalized.IndexOf(':') >= 0 ||
                relativePath.IndexOf('\\') >= 0)
            {
                throw new InvalidDataException(
                    "Managed paths must be project-relative and use '/' separators: " + relativePath);
            }

            string[] segments = normalized.Split('/');
            foreach (string segment in segments)
            {
                if (string.IsNullOrEmpty(segment) || segment == "." || segment == "..")
                {
                    throw new InvalidDataException("Managed paths cannot contain empty or traversal segments: " + relativePath);
                }
            }
        }

        internal static void EnsureRegularTree(string root)
        {
            string fullRoot = Path.GetFullPath(root);
            if (!Directory.Exists(fullRoot))
            {
                throw new DirectoryNotFoundException("The candidate documentation root is missing: " + fullRoot);
            }

            Queue<string> directories = new Queue<string>();
            directories.Enqueue(fullRoot);

            while (directories.Count > 0)
            {
                string directory = directories.Dequeue();
                FileAttributes directoryAttributes = File.GetAttributes(directory);
                if ((directoryAttributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidDataException("The documentation candidate contains a linked directory.");
                }

                foreach (string child in Directory.EnumerateFileSystemEntries(directory))
                {
                    FileAttributes attributes = File.GetAttributes(child);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        throw new InvalidDataException("The documentation candidate contains a linked entry.");
                    }

                    if (string.Equals(Path.GetFileName(child), ".git", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("The documentation candidate must not contain Git metadata.");
                    }

                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        directories.Enqueue(child);
                    }
                }
            }
        }

        internal static void EnsureManagedTargetIsRegular(string projectRoot, string relativePath, bool expectDirectory)
        {
            string target = GetSafeFullPath(projectRoot, relativePath);
            string fullRoot = Path.GetFullPath(projectRoot);
            string current = target;

            while (!string.Equals(current, fullRoot, PathComparison))
            {
                if (TryGetWindowsAttributes(current, out FileAttributes attributes))
                {
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        throw new IOException("A managed target or its parent is a linked path: " + relativePath);
                    }
                }

                DirectoryInfo parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            if (expectDirectory && File.Exists(target))
            {
                throw new IOException("The managed documentation destination is a file, not a directory.");
            }

            if (!expectDirectory && Directory.Exists(target))
            {
                throw new IOException("A managed AI instruction destination is a directory: " + relativePath);
            }

            string targetParent = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(targetParent) && File.Exists(targetParent))
            {
                throw new IOException("A managed target parent is a file: " + relativePath);
            }
        }

        internal static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            ClearReadOnlyAttributes(path);
            Directory.Delete(path, true);
        }

        internal static void ClearReadOnlyFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }
        }

        internal static void DeleteDirectoryBestEffort(string path)
        {
            try
            {
                DeleteDirectory(path);
            }
            catch
            {
                // Ephemeral archive cleanup must not hide the update result.
            }
        }

        internal static void ClearReadOnlyAttributes(string root)
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                FileAttributes attributes = File.GetAttributes(file);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                }
            }

            foreach (string directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
            {
                FileAttributes attributes = File.GetAttributes(directory);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(directory, attributes & ~FileAttributes.ReadOnly);
                }
            }
        }

        internal static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relative = GetRelativePath(source, directory);
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }

            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = GetRelativePath(source, file);
                string target = Path.Combine(destination, relative);
                string parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(file, target, true);
            }
        }

        internal static bool FilesEqual(string left, string right)
        {
            FileInfo leftInfo = new FileInfo(left);
            FileInfo rightInfo = new FileInfo(right);
            if (!leftInfo.Exists || !rightInfo.Exists || leftInfo.Length != rightInfo.Length)
            {
                return false;
            }

            const int bufferSize = 81920;
            byte[] leftBuffer = new byte[bufferSize];
            byte[] rightBuffer = new byte[bufferSize];

            using (FileStream leftStream = File.OpenRead(left))
            using (FileStream rightStream = File.OpenRead(right))
            {
                while (true)
                {
                    int leftRead = leftStream.Read(leftBuffer, 0, leftBuffer.Length);
                    int rightRead = rightStream.Read(rightBuffer, 0, rightBuffer.Length);
                    if (leftRead != rightRead)
                    {
                        return false;
                    }

                    if (leftRead == 0)
                    {
                        return true;
                    }

                    for (int index = 0; index < leftRead; index++)
                    {
                        if (leftBuffer[index] != rightBuffer[index])
                        {
                            return false;
                        }
                    }
                }
            }
        }

        internal static void WriteUtf8(string path, string text)
        {
            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        internal static string GetRelativePath(string root, string path)
        {
            string fullRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(fullRoot, PathComparison))
            {
                throw new InvalidDataException("A path is outside the expected root: " + path);
            }

            return fullPath.Substring(fullRoot.Length);
        }

        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        private static bool TryGetWindowsAttributes(string path, out FileAttributes attributes)
        {
            const uint invalidAttributes = 0xffffffff;
            const int fileNotFound = 2;
            const int pathNotFound = 3;

            uint nativeAttributes = GetFileAttributes(path);
            if (nativeAttributes != invalidAttributes)
            {
                attributes = (FileAttributes)nativeAttributes;
                return true;
            }

            int error = Marshal.GetLastWin32Error();
            if (error == fileNotFound || error == pathNotFound)
            {
                attributes = default(FileAttributes);
                return false;
            }

            throw new IOException(
                "A managed target path could not be inspected: " + path,
                new Win32Exception(error));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetFileAttributesW")]
        private static extern uint GetFileAttributes(string fileName);
    }
}
