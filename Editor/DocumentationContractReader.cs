using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Geurts.GameForge.Documentation
{
    internal static class DocumentationContractReader
    {
        private const string PackageFilesBegin = "<!-- GEURTS-PACKAGE-FILES:BEGIN -->";
        private const string PackageFilesEnd = "<!-- GEURTS-PACKAGE-FILES:END -->";

        internal static DocumentationCompanionContract LoadAndValidate(string candidateRoot)
        {
            DocumentationFileOperations.EnsureRegularTree(candidateRoot);

            string contractPath = DocumentationFileOperations.GetSafeFullPath(
                candidateRoot,
                DocumentationPackageConstants.ContractRelativePath);
            if (!File.Exists(contractPath))
            {
                throw new InvalidDataException(
                    "The documentation archive does not contain " +
                    DocumentationPackageConstants.ContractRelativePath + ".");
            }

            string contractJson;
            try
            {
                contractJson = File.ReadAllText(contractPath);
            }
            catch (Exception exception)
            {
                throw new InvalidDataException("The documentation companion contract could not be read.", exception);
            }

            ContractDto dto;
            try
            {
                DocumentationContractJsonShapeValidator.Validate(contractJson);
                dto = JsonUtility.FromJson<ContractDto>(contractJson);
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new InvalidDataException("The documentation companion contract is not valid JSON.", exception);
            }

            if (dto == null)
            {
                throw new InvalidDataException("The documentation companion contract is empty.");
            }

            ValidateContractShape(dto);

            IReadOnlyList<string> validationEntries = Array.AsReadOnly(dto.validationEntries);
            ManagedAiRoute[] routes = dto.routeMappings
                .Select(mapping => new ManagedAiRoute(mapping.template, mapping.target))
                .ToArray();

            DocumentationCompanionContract contract = new DocumentationCompanionContract(
                dto.schemaVersion,
                dto.packageVersion,
                dto.source.repository,
                dto.source.branch,
                dto.destination.projectRelativePath,
                validationEntries,
                Array.AsReadOnly(routes));

            HashSet<string> readableEntries = new HashSet<string>(
                contract.RequiredCandidateEntries,
                StringComparer.Ordinal);
            foreach (string entry in DocumentationPackageConstants.RequiredRoutingEntries)
            {
                readableEntries.Add(entry);
            }

            readableEntries.Add(DocumentationPackageConstants.ContractRelativePath);
            foreach (ManagedAiRoute route in contract.ManagedAiRoutes)
            {
                readableEntries.Add(route.Source);
            }

            ValidateCandidateEntries(candidateRoot, readableEntries);
            ValidateManifestRegistry(candidateRoot, contract.PackageVersion);
            return contract;
        }

        private static void ValidateContractShape(ContractDto dto)
        {
            RequireEqual(dto.schemaVersion, DocumentationPackageConstants.ExpectedSchemaVersion, "schemaVersion");
            if (string.IsNullOrWhiteSpace(dto.packageVersion) ||
                !Regex.IsMatch(dto.packageVersion, @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$"))
            {
                throw new InvalidDataException("packageVersion must be a valid three-part semantic version.");
            }

            if (dto.source == null || dto.destination == null || dto.updateUi == null)
            {
                throw new InvalidDataException("The documentation companion contract is missing a required object.");
            }

            RequireEqual(dto.source.repository, DocumentationPackageConstants.RepositoryUrl, "source.repository");
            RequireEqual(dto.source.branch, DocumentationPackageConstants.RepositoryBranch, "source.branch");
            RequireEqual(dto.source.selection, "exact-resolved-head-commit-archive", "source.selection");
            RequireEqual(dto.destination.projectRelativePath, DocumentationPackageConstants.ManagedDocumentationDirectory, "destination.projectRelativePath");
            RequireEqual(dto.destination.replacement, "complete-directory", "destination.replacement");
            RequireEqual(dto.destination.access, "logically-read-only", "destination.access");

            RequireEqual(dto.updateUi.actionLabel, DocumentationPackageConstants.UpdateActionLabel, "updateUi.actionLabel");
            RequireEqual(dto.updateUi.confirmationDefault, "cancel", "updateUi.confirmationDefault");
            RequireEqual(dto.updateUi.cancelResult, "no-network-or-filesystem-change", "updateUi.cancelResult");

            ValidateValidationEntries(dto.validationEntries, "validationEntries");
            ValidateRouteMappings(dto.routeMappings);
            ValidateConfirmationTargets(dto.updateUi.confirmationTargets);
        }

        private static void ValidateRouteMappings(RouteMappingDto[] actual)
        {
            if (actual == null || actual.Length != DocumentationPackageConstants.ExpectedManagedAiRoutes.Count)
            {
                throw new InvalidDataException("The documentation contract must declare exactly four managed AI routes.");
            }

            for (int index = 0; index < actual.Length; index++)
            {
                RouteMappingDto mapping = actual[index];
                ManagedAiRoute expected = DocumentationPackageConstants.ExpectedManagedAiRoutes[index];
                if (mapping == null)
                {
                    throw new InvalidDataException("The documentation contract contains an empty route mapping.");
                }

                RequireEqual(mapping.template, expected.Source, "routeMappings.template");
                RequireEqual(mapping.target, expected.Destination, "routeMappings.target");
                DocumentationFileOperations.ValidateRelativePath(mapping.template);
                ValidateProjectTarget(mapping.target);
            }
        }

        private static void ValidateConfirmationTargets(ConfirmationTargetDto[] actual)
        {
            if (actual == null || actual.Length != 5)
            {
                throw new InvalidDataException("The documentation contract must display exactly five confirmation targets.");
            }

            RequireEqual(actual[0]?.path, DocumentationPackageConstants.ManagedDocumentationDirectory, "updateUi.confirmationTargets.path");
            RequireEqual(actual[0]?.effect, "replace-complete-directory", "updateUi.confirmationTargets.effect");

            for (int index = 0; index < DocumentationPackageConstants.ExpectedManagedAiRoutes.Count; index++)
            {
                ConfirmationTargetDto target = actual[index + 1];
                RequireEqual(target?.path, DocumentationPackageConstants.ExpectedManagedAiRoutes[index].Destination, "updateUi.confirmationTargets.path");
                RequireEqual(target?.effect, "replace-complete-file", "updateUi.confirmationTargets.effect");
            }
        }

        private static void ValidateValidationEntries(string[] actual, string field)
        {
            if (actual == null || actual.Length == 0)
            {
                throw new InvalidDataException(field + " must contain at least one entry.");
            }

            HashSet<string> actualSet = new HashSet<string>(actual, StringComparer.Ordinal);
            if (actualSet.Count != actual.Length)
            {
                throw new InvalidDataException(field + " must contain unique entries.");
            }

            foreach (string item in actual)
            {
                DocumentationFileOperations.ValidateRelativePath(item);
            }
        }

        private static void ValidateCandidateEntries(string candidateRoot, IEnumerable<string> entries)
        {
            foreach (string entry in entries)
            {
                string path = DocumentationFileOperations.GetSafeFullPath(candidateRoot, entry);
                if (!File.Exists(path))
                {
                    throw new InvalidDataException("The documentation candidate is missing required file: " + entry);
                }

                using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length < 0)
                    {
                        throw new InvalidDataException("A required documentation file could not be read: " + entry);
                    }
                }
            }
        }

        private static void ValidateManifestRegistry(string candidateRoot, string contractPackageVersion)
        {
            string manifestPath = DocumentationFileOperations.GetSafeFullPath(candidateRoot, "GeurtsTechniqueManifest.md");
            string manifest = File.ReadAllText(manifestPath);
            Match manifestVersion = Regex.Match(
                manifest,
                @"(?m)^\*\*Version:\*\*\s*((?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*))\s*$");
            if (!manifestVersion.Success)
            {
                throw new InvalidDataException("The documentation manifest does not declare a valid package version.");
            }

            if (!string.Equals(
                    contractPackageVersion,
                    manifestVersion.Groups[1].Value,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException("The contract packageVersion does not match the archive manifest.");
            }

            int begin = manifest.IndexOf(PackageFilesBegin, StringComparison.Ordinal);
            int end = manifest.IndexOf(PackageFilesEnd, StringComparison.Ordinal);
            if (begin < 0 || end <= begin)
            {
                throw new InvalidDataException("The documentation manifest package-file registry is missing.");
            }

            string registry = manifest.Substring(begin + PackageFilesBegin.Length, end - begin - PackageFilesBegin.Length);
            MatchCollection matches = Regex.Matches(registry, @"(?m)^\|\s*`([^`]+)`\s*\|");
            if (matches.Count == 0)
            {
                throw new InvalidDataException("The documentation manifest package-file registry is empty.");
            }

            HashSet<string> registered = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in matches)
            {
                string relativePath = match.Groups[1].Value;
                DocumentationFileOperations.ValidateRelativePath(relativePath);
                if (!registered.Add(relativePath))
                {
                    throw new InvalidDataException("The documentation manifest repeats a package path: " + relativePath);
                }

                string path = DocumentationFileOperations.GetSafeFullPath(candidateRoot, relativePath);
                if (!File.Exists(path))
                {
                    throw new InvalidDataException("The documentation manifest references a missing package file: " + relativePath);
                }
            }

            HashSet<string> candidateFiles = new HashSet<string>(
                Directory.EnumerateFiles(candidateRoot, "*", SearchOption.AllDirectories)
                    .Select(path => DocumentationFileOperations.GetRelativePath(candidateRoot, path)
                        .Replace('\\', '/')),
                StringComparer.Ordinal);
            if (!candidateFiles.SetEquals(registered))
            {
                throw new InvalidDataException(
                    "The documentation candidate does not exactly match its package-file registry.");
            }
        }

        private static void ValidateProjectTarget(string target)
        {
            DocumentationFileOperations.ValidateRelativePath(target);
            string normalized = target.Replace('\\', '/');
            if (normalized.Equals("Docs/GameDesign", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("Docs/GameDesign/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The documentation contract cannot manage Docs/GameDesign.");
            }
        }

        private static void RequireEqual(string actual, string expected, string field)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Unsupported documentation contract value for " + field + ". Expected '" +
                    expected + "'.");
            }
        }

        [Serializable]
        private sealed class ContractDto
        {
            public string schemaVersion;
            public string packageVersion;
            public SourceDto source;
            public DestinationDto destination;
            public string[] validationEntries;
            public UpdateUiDto updateUi;
            public RouteMappingDto[] routeMappings;
        }

        [Serializable]
        private sealed class SourceDto
        {
            public string repository;
            public string branch;
            public string selection;
        }

        [Serializable]
        private sealed class DestinationDto
        {
            public string projectRelativePath;
            public string replacement;
            public string access;
        }

        [Serializable]
        private sealed class UpdateUiDto
        {
            public string actionLabel;
            public string confirmationDefault;
            public string cancelResult;
            public ConfirmationTargetDto[] confirmationTargets;
        }

        [Serializable]
        private sealed class ConfirmationTargetDto
        {
            public string path;
            public string effect;
        }

        [Serializable]
        private sealed class RouteMappingDto
        {
            public string template;
            public string target;
        }
    }
}
