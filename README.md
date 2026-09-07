# Geurts Game Forge Documentation

An independent Windows Unity Editor package that installs and updates the project-local copy of the authoritative [Geurts Game Forge documentation](https://github.com/Geurtsy/GeurtsGameForge_Documentation).

The package contains no generic Geurts documentation. It checks the authoritative repository's `main` commit when Unity opens and shows an available update in **Tools > Geurts Game Forge > Documentation**. Nothing is replaced until the user chooses **Update Geurts Game Forge Documentation** and confirms the five listed managed targets.

## Install through Unity Package Manager

In Unity, open **Window > Package Manager**, choose **Install package from git URL**, and enter:

```text
https://github.com/Geurtsy/com.geurts.gameforge.documentation.git
```

Unity 2022.3 or newer on Windows is required. Import your licensed **Odin Inspector** installation into the Unity project to use the dashboard. Odin is distributed separately and is not bundled or downloaded by this package. The repository is public, so the Git URL does not require package-specific credentials.

## Odin documentation dashboard

The window uses `OdinEditorWindow` with grouped actions, a prominent source-status card, a large documentation Update button, and a separate secondary .gitignore card. **Source and managed files** expands to show the repository, full copyable commit identifiers, and all five documentation-update targets. Content scrolls in small or docked windows, and actions are disabled during an update. Colours and text distinguish the state without claiming that a matching commit verifies local files.

Without Odin, the package still compiles and the Documentation menu displays installation guidance. The existing startup metadata check and standalone .gitignore menu remain available. The dashboard becomes available when Odin defines `ODIN_INSPECTOR`; keep Odin's assemblies enabled and auto-referenced as in its standard installation.

## Install the project .gitignore

After installing the documentation, choose **Tools > Geurts Game Forge > Install Geurts .gitignore**, or use the same button in the Documentation window.

The tool reads the approved fenced payload from `GeurtsGameForgeDocumentation/GeurtsTechniques/GeurtsGitIgnoreTechnique.md` and checks its version against `GeurtsTechniqueManifest.md`, plus its markers, encoding, line count, and checksum. No template is bundled with this Unity package or downloaded by this action. If the installed documentation is missing or invalid, installation stops before changing `.gitignore`; run the documentation Update first.

The warning names the full project-root destination and explains that **the existing `.gitignore` will be overwritten in full and all custom rules will be lost**. Cancel is focused initially; Cancel, Enter, Escape, and closing the dialog leave the file untouched. Choose **Install and Overwrite** to continue. A missing file is created. There is no backup or merge, and Git tracking state is not changed. A write failure is reported and may require running the installer again.

This separately confirmed tool is an explicitly requested extension to the original companion's update-only scope and the template technique's create-only policy. It does not expand the documentation Update contract or run at startup.

## Documentation update boundary

A confirmed update replaces only:

- `GeurtsGameForgeDocumentation/`
- `AGENTS.md`
- `.github/copilot-instructions.md`
- `.github/instructions/geurts-unity.instructions.md`
- `.github/instructions/geurts-game-design.instructions.md`

The destination and four-route list are pinned by schema 1.0.0 of `GeurtsTechniques/GeurtsDocumentationCompanionContract.json` and are verified against the downloaded archive before any replacement begins. Documentation release versions and validation entries may advance without changing that closed update boundary. The separate .gitignore installer writes only the project-root `.gitignore`. In particular, `Docs/GameDesign/` is never inspected or changed by either action.

## Scope

This package is Windows-only and Editor-only. Its dashboard requires Odin Inspector; it has no dependency on Geurts Game Forge God, Brick Manager, Quantum Console, or GameForgeIntelligence. The Odin dashboard is an explicitly requested change to the original companion's no-Odin requirement. It does not change the documentation replacement contract or copy generic Geurts guidance into this package. It does not provide preview, backup, rollback, journaling, migration, recovery, or local-drift preservation.

## Validation

Run `Tools/ValidatePackage.ps1 -DocumentationPath <path-to-GeurtsGameForgeDocumentation>` to run Editor tests in a disposable project. The documentation path supplies only the manifest and Git Ignore Technique as an external integration fixture; the source is not changed or bundled with the package. Without this parameter, tests requiring the real template are reported as skipped. `-StaticOnly` checks package structure without launching Unity.

Run again with `-OdinPath <path-to-Assets/Plugins/Sirenix> -ProjectPath <package-root>/work~/UnityValidationOdin` to compile and test the Odin dashboard against your installed copy. Omit `-OdinPath` to test the missing-Odin screen. The validation copy stays under the ignored `work~/` directory and must not be committed or distributed.
