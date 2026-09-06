using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    internal interface IDocumentationTransport
    {
        Task<string> ResolveHeadCommitAsync(CancellationToken cancellationToken);

        Task<DocumentationDownload> DownloadCommitAsync(
            string commit,
            string workingDirectory,
            CancellationToken cancellationToken);
    }

    internal sealed class GitHubDocumentationTransport : IDocumentationTransport, IDisposable
    {
        private readonly HttpClient client;

        internal GitHubDocumentationTransport()
        {
            client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(90)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                DocumentationPackageConstants.PackageName + "/0.1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        public async Task<string> ResolveHeadCommitAsync(CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(
                       HttpMethod.Get,
                       DocumentationPackageConstants.HeadCommitApiUrl))
            using (HttpResponseMessage response = await client.SendAsync(
                       request,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                byte[] bytes = await ReadBoundedAsync(
                    response,
                    DocumentationPackageConstants.MetadataLimitBytes,
                    cancellationToken);
                return ParseHeadCommitMetadata(bytes);
            }
        }

        public async Task<DocumentationDownload> DownloadCommitAsync(
            string commit,
            string workingDirectory,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(commit) ||
                !Regex.IsMatch(commit, "^[0-9a-f]{40}$", RegexOptions.CultureInvariant))
            {
                throw new ArgumentException("A resolved 40-character commit identity is required.", nameof(commit));
            }

            Directory.CreateDirectory(workingDirectory);
            string archivePath = Path.Combine(workingDirectory, "documentation.zip");
            string extractionRoot = Path.Combine(workingDirectory, "extracted");
            Directory.CreateDirectory(extractionRoot);

            string archiveUrl = string.Format(
                DocumentationPackageConstants.ArchiveUrlFormat,
                Uri.EscapeDataString(commit));

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, archiveUrl))
            using (HttpResponseMessage response = await client.SendAsync(
                       request,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength.HasValue &&
                    response.Content.Headers.ContentLength.Value > DocumentationPackageConstants.ArchiveLimitBytes)
                {
                    throw new InvalidDataException("The documentation archive exceeds the download limit.");
                }

                using (Stream input = await response.Content.ReadAsStreamAsync())
                using (FileStream output = new FileStream(
                           archivePath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           81920,
                           true))
                {
                    await CopyBoundedAsync(
                        input,
                        output,
                        DocumentationPackageConstants.ArchiveLimitBytes,
                        cancellationToken);
                }
            }

            string candidateRoot = ExtractCommitArchive(archivePath, extractionRoot, commit);
            return new DocumentationDownload(workingDirectory, candidateRoot);
        }

        public void Dispose()
        {
            client.Dispose();
        }

        internal static string ParseHeadCommitMetadata(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            ReferenceResponse payload = JsonUtility.FromJson<ReferenceResponse>(
                Encoding.UTF8.GetString(bytes));
            string sha = payload != null && payload.@object != null
                ? payload.@object.sha
                : null;
            if (string.IsNullOrWhiteSpace(sha) ||
                !Regex.IsMatch(sha, "^[0-9a-fA-F]{40}$", RegexOptions.CultureInvariant))
            {
                throw new InvalidDataException("GitHub did not return a valid main commit identity.");
            }

            return sha.ToLowerInvariant();
        }

        private static async Task<byte[]> ReadBoundedAsync(
            HttpResponseMessage response,
            long limit,
            CancellationToken cancellationToken)
        {
            if (response.Content.Headers.ContentLength.HasValue &&
                response.Content.Headers.ContentLength.Value > limit)
            {
                throw new InvalidDataException("The remote metadata response exceeds the allowed size.");
            }

            using (Stream input = await response.Content.ReadAsStreamAsync())
            using (MemoryStream output = new MemoryStream())
            {
                await CopyBoundedAsync(input, output, limit, cancellationToken);
                return output.ToArray();
            }
        }

        private static async Task CopyBoundedAsync(
            Stream input,
            Stream output,
            long limit,
            CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[81920];
            long total = 0;
            while (true)
            {
                int read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (read == 0)
                {
                    return;
                }

                total += read;
                if (total > limit)
                {
                    throw new InvalidDataException("The remote response exceeds the allowed size.");
                }

                await output.WriteAsync(buffer, 0, read, cancellationToken);
            }
        }

        internal static string ExtractCommitArchive(string archivePath, string extractionRoot, string commit)
        {
            string expectedWrapper = "GeurtsGameForge_Documentation-" + commit;
            long extractedBytes = 0;
            bool foundFile = false;

            using (FileStream archiveStream = File.OpenRead(archivePath))
            using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, false))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string archiveName = entry.FullName.Replace('\\', '/');
                    if (string.IsNullOrWhiteSpace(archiveName))
                    {
                        continue;
                    }

                    bool isDirectory = archiveName.EndsWith("/", StringComparison.Ordinal);
                    string normalizedArchiveName = isDirectory
                        ? archiveName.TrimEnd('/')
                        : archiveName;
                    string[] segments = normalizedArchiveName.Split('/');
                    if (segments.Length < 1 ||
                        !string.Equals(segments[0], expectedWrapper, StringComparison.Ordinal))
                    {
                        throw new InvalidDataException("The archive is not the resolved documentation commit.");
                    }

                    if (IsSymbolicLink(entry))
                    {
                        throw new InvalidDataException("The documentation archive contains a symbolic link.");
                    }

                    if (segments.Length == 1)
                    {
                        if (!isDirectory)
                        {
                            throw new InvalidDataException("The archive contains a file in place of its wrapper directory.");
                        }

                        continue;
                    }

                    string relativePath = string.Join("/", segments, 1, segments.Length - 1);

                    string target = DocumentationFileOperations.GetSafeFullPath(extractionRoot, relativePath);
                    if (isDirectory)
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }

                    extractedBytes += entry.Length;
                    if (extractedBytes > DocumentationPackageConstants.ExtractedLimitBytes)
                    {
                        throw new InvalidDataException("The extracted documentation exceeds the allowed size.");
                    }

                    string parent = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }

                    if (File.Exists(target) || Directory.Exists(target))
                    {
                        throw new InvalidDataException("The documentation archive contains a duplicate path.");
                    }

                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        input.CopyTo(output);
                    }

                    foundFile = true;
                }
            }

            if (!foundFile)
            {
                throw new InvalidDataException("The documentation archive contains no files.");
            }

            DocumentationFileOperations.EnsureRegularTree(extractionRoot);
            return extractionRoot;
        }

        private static bool IsSymbolicLink(ZipArchiveEntry entry)
        {
            const int unixFileTypeMask = 0xF000;
            const int unixSymbolicLink = 0xA000;
            int unixMode = (entry.ExternalAttributes >> 16) & unixFileTypeMask;
            return unixMode == unixSymbolicLink;
        }

        [Serializable]
        private sealed class ReferenceResponse
        {
            public ReferenceObject @object;
        }

        [Serializable]
        private sealed class ReferenceObject
        {
            public string sha;
        }
    }
}
