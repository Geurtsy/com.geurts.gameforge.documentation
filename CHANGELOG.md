# Changelog

## 0.6.0 - 2026-09-08

- Give Odin Inspector and Quantum Console dependency cards the same green ready, orange missing, blue checking and red error palette as package/documentation update cards, in the dashboard and Unity Package Manager extension.
- Add owned-asset download/import access through Unity's My Assets view and interactive import of a locally downloaded licensed .unitypackage. Unity retains its ownership checks, import review and download/import progress.
- Report blocked and failed installation actions clearly; defer both actions until UI drawing finishes. Readiness refreshes during Unity import/compilation and after script reload; it does not claim vendor-version currency or ownership.

## 0.5.3 - 2026-09-08

- Label Odin Inspector and Quantum Console as required external dependencies in Unity Package Manager's package details.
- Show green dependency boxes marked Installed and ready when the required tool types are loaded and Unity has no script compilation errors. Refresh status during compilation/import and show missing or unavailable tools without a green box.
- Include the same explicit requirements in the package description so they remain readable before package scripts compile. Commercial assets remain separate imports, with no invented registry dependencies.

## 0.5.2 - 2026-09-08

- Fix the dashboard confirmation path: defer actions until Odin has finished drawing, collect confirmation in a UI Toolkit dialog, then start the approved documentation update after the modal window closes.
- Report a blocked update attempt instead of silently discarding it when Unity or another operation is busy.
- Show installed and available Git revision identifiers alongside versions, and explain same-version revision changes or missing installation records explicitly.
- Cover real dashboard clicks, confirmation acceptance/cancellation, file replacement, commit persistence, and a fresh up-to-date result with an Editor integration test.

## 0.5.1 - 2026-09-08

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
