# Changelog

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
