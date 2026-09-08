// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    /// <summary>Bounded, read-only version metadata pinned to a resolved Git commit.</summary>
    internal sealed class GitVersionMetadata : IDisposable
    {
        internal const string OfficialPackageUrl = "https://github.com/Geurtsy/com.geurts.gameforge.documentation.git";
        internal const string ManifestPath = "GeurtsTechniqueManifest.md";
        private readonly HttpClient _client;

        internal GitVersionMetadata(HttpMessageHandler handler = null)
        {
            _client = handler == null ? new HttpClient() : new HttpClient(handler);
            _client.Timeout = TimeSpan.FromSeconds(45);
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(DocumentationPackageConstants.PackageName);
            _client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
        }

        internal async Task<string> ReadTextAsync(string url, CancellationToken token, string accept = null)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (accept != null) request.Headers.Accept.ParseAdd(accept);
                using (HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token))
                {
                    response.EnsureSuccessStatusCode();
                    return Encoding.UTF8.GetString(await GitHubDocumentationTransport.ReadBoundedAsync(
                        response, DocumentationPackageConstants.MetadataLimitBytes, token));
                }
            }
        }

        internal async Task<GitPackageVersion> ReadPackageAsync(string gitReference, CancellationToken token,
            Action<UpdateProgress> progress = null)
        {
            GitHubPackageSource source = GitHubPackageSource.Parse(gitReference);
            progress?.Invoke(new UpdateProgress("Resolving the package's selected Git revision..."));
            // The SHA media type returns only the hash, never a potentially huge commit diff.
            string commit = (await ReadTextAsync(source.CommitUrl, token, "application/vnd.github.sha")).Trim();
            if (!Regex.IsMatch(commit, "^[0-9a-fA-F]{40}$"))
                throw new InvalidDataException("GitHub did not return a valid package commit identity.");
            commit = commit.ToLowerInvariant();
            progress?.Invoke(new UpdateProgress("Reading package.json at Git commit " + commit.Substring(0, 8) + "..."));
            string json = await ReadTextAsync(source.VersionUrl(commit), token);
            PackageVersionJson metadata = JsonUtility.FromJson<PackageVersionJson>(json);
            if (metadata == null || metadata.name != DocumentationPackageConstants.PackageName || !IsVersion(metadata.version))
                throw new InvalidDataException("Git package.json has a missing or invalid package name or version.");
            return new GitPackageVersion(metadata.version, commit);
        }

        internal static string ParseDocumentationVersion(string manifest)
        {
            Match match = Regex.Match(manifest ?? "", @"(?m)^\*\*Version:\*\*[ \t]+([^\s]+)[ \t]*\r?$", RegexOptions.CultureInvariant);
            if (!match.Success || !IsVersion(match.Groups[1].Value))
                throw new InvalidDataException("The documentation manifest has no valid version number.");
            return match.Groups[1].Value;
        }

        internal static string ReadInstalledDocumentationVersion(string projectRoot)
        {
            string directory = Path.Combine(projectRoot, DocumentationPackageConstants.ManagedDocumentationDirectory);
            if (!Directory.Exists(directory)) return "Not installed";
            try
            {
                // Only the manifest is read. No tree traversal or local-content integrity scan.
                string file = Path.Combine(directory, ManifestPath);
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0 ||
                    (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                    return "Unknown (linked manifest)";
                using (var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (input.Length > DocumentationPackageConstants.MetadataLimitBytes)
                        return "Unknown (manifest too large)";
                    using (var reader = new StreamReader(input, Encoding.UTF8, true))
                        return ParseDocumentationVersion(reader.ReadToEnd());
                }
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException ||
                                              exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                return "Unknown (manifest unavailable or invalid)";
            }
        }

        internal static bool IsVersion(string version) => !string.IsNullOrEmpty(version) &&
            Regex.IsMatch(version, @"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant);

        /// <summary>Disposes the metadata HTTP client.</summary>
        public void Dispose() => _client.Dispose();

        [Serializable]
        private sealed class PackageVersionJson
        {
            public string name;
            public string version;
        }
    }

    internal sealed class GitPackageVersion
    {
        internal GitPackageVersion(string version, string commit) { Version = version; Commit = commit; }
        internal string Version { get; }
        internal string Commit { get; }
    }

    internal sealed class GitHubPackageSource
    {
        private string _repository;
        private string _revision;
        private string _path;
        internal string CommitUrl => "https://api.github.com/repos/" + _repository + "/commits/" + Uri.EscapeDataString(_revision);
        internal string VersionUrl(string commit) => "https://raw.githubusercontent.com/" + _repository + "/" + commit + "/" + _path + "package.json";

        internal static GitHubPackageSource Parse(string reference)
        {
            string value = reference ?? "";
            if (value.StartsWith("git+", StringComparison.Ordinal)) value = value.Substring(4);
            if (value.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
                value = "ssh://git@github.com/" + value.Substring("git@github.com:".Length);
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) || uri.Host != "github.com" ||
                (uri.Scheme != "https" && uri.Scheme != "ssh" && uri.Scheme != "git"))
                throw new NotSupportedException("Version checking supports public github.com Git sources. This package can still be updated from its configured Git URL.");
            string repository = uri.AbsolutePath.Trim('/');
            if (repository.EndsWith(".git", StringComparison.Ordinal)) repository = repository.Substring(0, repository.Length - 4);
            if (!Regex.IsMatch(repository, @"^[\w.-]+/[\w.-]+$"))
                throw new NotSupportedException("The configured GitHub repository URL could not be read.");
            string path = "";
            if (!string.IsNullOrEmpty(uri.Query))
            {
                if (!uri.Query.StartsWith("?path=", StringComparison.Ordinal) || uri.Query.Contains("&"))
                    throw new NotSupportedException("The configured Git URL has unsupported query parameters.");
                string decoded = Uri.UnescapeDataString(uri.Query.Substring(6)).TrimStart('/');
                foreach (string segment in decoded.Split('/'))
                {
                    if (segment.Length == 0 || segment == "." || segment == ".." || segment.Contains("\\"))
                        throw new NotSupportedException("The configured package subfolder is invalid.");
                    path += Uri.EscapeDataString(segment) + "/";
                }
            }
            return new GitHubPackageSource
            {
                _repository = repository,
                _revision = string.IsNullOrEmpty(uri.Fragment) ? "HEAD" : Uri.UnescapeDataString(uri.Fragment.Substring(1)),
                _path = path
            };
        }
    }
}
