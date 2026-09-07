// IMPORTANT: This script must comply with GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsTechnicalTechnique.md and folder placement rules in GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsFolderStructureTechnique.md.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Geurts.GameForge.Documentation
{
    internal static class GitIgnoreTemplateReader
    {
        internal const string TechniquePath = "GeurtsTechniques/GeurtsGitIgnoreTechnique.md";
        internal const string TemplateVersion = "1.0.0";
        internal const string TemplateSha256 = "7223a9449718942d3a5cad00cf4d4e0dee9c89eb64951541fa4ebfb803acb45b";
        private const string BeginMarker = "<!-- GEURTS-GITIGNORE-BEGIN";
        private const string EndMarker = "<!-- GEURTS-GITIGNORE-END -->";
        private const string RegistryBegin = "<!-- GEURTS-PACKAGE-FILES:BEGIN -->";
        private const string RegistryEnd = "<!-- GEURTS-PACKAGE-FILES:END -->";
        private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false, true);

        internal static byte[] Load(string projectRoot)
        {
            string document = ReadDocumentationFile(projectRoot, TechniquePath);
            string manifest = ReadDocumentationFile(projectRoot, "GeurtsTechniqueManifest.md");
            return Extract(document, manifest);
        }

        internal static byte[] Extract(string document, string manifest)
        {
            document = Normalize(document);
            manifest = Normalize(manifest);
            MatchCollection versions = Regex.Matches(document, @"(?m)^\*\*Version:\*\*[ \t]*([^\n]+)$");
            string registry = ExtractRegion(manifest, RegistryBegin, RegistryEnd);
            MatchCollection entries = Regex.Matches(registry,
                @"(?m)^\|[ \t]*`" + Regex.Escape(TechniquePath) + @"`[ \t]*\|[ \t]*([^|\n]+)\|");
            if (versions.Count != 1 || versions[0].Groups[1].Value.Trim() != TemplateVersion ||
                entries.Count != 1 || entries[0].Groups[1].Value.Trim() != TemplateVersion)
            {
                throw InvalidTemplate("The technique and manifest must select template version " + TemplateVersion + ".");
            }

            string region = ExtractRegion(document, BeginMarker, EndMarker);
            string header = " version=\"" + TemplateVersion + "\" target=\".gitignore\" sha256=\"" +
                            TemplateSha256 + "\" -->\n```gitignore\n";
            if (!region.StartsWith(header, StringComparison.Ordinal) ||
                !region.EndsWith("```\n", StringComparison.Ordinal))
            {
                throw InvalidTemplate("The payload markers or fenced block are invalid.");
            }

            string payload = region.Substring(header.Length, region.Length - header.Length - 4);
            byte[] bytes = _utf8.GetBytes(payload);
            string hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            }

            if (payload.StartsWith("\uFEFF", StringComparison.Ordinal) ||
                !payload.EndsWith("\n", StringComparison.Ordinal) ||
                payload.EndsWith("\n\n", StringComparison.Ordinal) ||
                payload.Count(character => character == '\n') != 376 || hash != TemplateSha256)
            {
                throw InvalidTemplate("The payload does not match the approved line count, newlines, or SHA-256.");
            }

            return bytes;
        }

        private static string ReadDocumentationFile(string projectRoot, string relativePath)
        {
            string projectRelativePath = DocumentationPackageConstants.ManagedDocumentationDirectory + "/" + relativePath;
            DocumentationFileOperations.EnsureManagedTargetIsRegular(projectRoot, projectRelativePath, false);
            string path = DocumentationFileOperations.GetSafeFullPath(projectRoot, projectRelativePath);
            if (!File.Exists(path))
            {
                throw InvalidTemplate("Missing " + projectRelativePath + ".");
            }

            // Decode explicitly so UTF-16 or invalid UTF-8 cannot be silently accepted by StreamReader.
            return _utf8.GetString(File.ReadAllBytes(path));
        }

        private static string Normalize(string text)
        {
            if (text.StartsWith("\uFEFF", StringComparison.Ordinal))
            {
                text = text.Substring(1);
            }

            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static string ExtractRegion(string text, string begin, string end)
        {
            int start = text.IndexOf(begin, StringComparison.Ordinal);
            int finish = text.IndexOf(end, StringComparison.Ordinal);
            if (start < 0 || finish < start + begin.Length ||
                (start > 0 && text[start - 1] != '\n') ||
                text[finish - 1] != '\n' ||
                (finish + end.Length < text.Length && text[finish + end.Length] != '\n') ||
                text.IndexOf(begin, start + begin.Length, StringComparison.Ordinal) >= 0 ||
                text.IndexOf(end, finish + end.Length, StringComparison.Ordinal) >= 0)
            {
                throw InvalidTemplate("The documentation must contain one complete, unambiguous marked region.");
            }

            return text.Substring(start + begin.Length, finish - start - begin.Length);
        }

        private static InvalidDataException InvalidTemplate(string detail)
        {
            return new InvalidDataException(detail + " Run Update Geurts Game Forge Documentation and try again. " +
                                            "The project .gitignore has not been changed.");
        }
    }
}
