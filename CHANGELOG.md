# Changelog

## 0.5.1 - Unreleased

- Require the actual Odin Inspector and Quantum Console assemblies under the current Geurts Game Forge package contract. Both commercial tools remain separately installed and are never bundled.
- Add the read-only `GeurtsGameForge.Documentation.Status` Quantum Console command in the Editor assembly.
- Declare the Unity 6000.3 baseline. Preserve the existing documentation dashboard and its confirmation/update flows; no God dependency is introduced.

## 0.5.0 - 2026-09-08

- Automatically check package and documentation Git versions on window open; add a manual Check for updates action for both.
- Show installed and available versions in separate update cards, with orange highlighting when Git commits differ, even if version numbers match.
- Read bounded version metadata at the resolved commit; retain configured package repositories, branches, tags, pins, and subfolders.
- Show download byte progress plus animated activity bars and detailed checking, extraction, validation, installation, and failure status.
- Keep checks read-only, coalesce overlapping checks, and report unavailable remote versions explicitly.

## 0.4.0 - 2026-09-08

- Added Update package from Git inside the dashboard, with installed version, configured source, progress, success, and failure feedback.
- Refresh the package's existing Git reference through Unity's package client, preserving branches and pins without opening Package Manager or replacing local development checkouts.
- Retain the active package request across script reloads and block overlapping documentation operations.
- Added a Dependencies area with mandatory Odin Inspector status and installation guidance. Missing Odin blocks documentation tools, including the standalone .gitignore action.

## 0.3.0 - 2026-09-08

- Rebuilt the documentation dashboard with Odin Inspector, a clear status card, grouped actions, and collapsible source and managed-file details.
- Added missing-Odin installation guidance while preserving package compilation and the existing confirmation flows.
- Added validation against a locally installed Odin copy; Odin remains separately licensed and is never bundled.

## 0.2.0 - 2026-09-08

- Added Install Geurts .gitignore to the Tools menu and documentation window.
- Read the approved payload from the installed GeurtsGameForgeDocumentation package and validate its manifest version, markers, UTF-8, line count, and checksum before writing the project-root .gitignore.
- Require a cancel-default warning that the existing .gitignore and custom rules will be overwritten and lost.

## 0.1.1 - 2026-09-06

- Use GitHub's compact branch-reference metadata endpoint for bounded startup and Update checks.

## 0.1.0 - 2026-09-06

- Added startup metadata checks for the authoritative documentation repository.
- Added a Unity Editor window with one confirmed documentation update action.
- Added direct replacement of the managed documentation folder and the four contract-declared AI routes.
- Added package and Editor tests for the managed boundary and failure preservation.
