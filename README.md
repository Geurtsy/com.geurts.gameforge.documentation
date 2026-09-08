# Geurts Game Forge Documentation

An independent Windows Unity Editor package that installs and updates the project-local copy of the authoritative [Geurts Game Forge documentation](https://github.com/Geurtsy/GeurtsGameForge_Documentation).

The package contains no generic Geurts documentation. It checks package and documentation Git versions when Unity opens and shows an available update in **Tools > Geurts Game Forge > Documentation**. Nothing is replaced until the user chooses **Update Geurts Game Forge Documentation** and confirms the five listed managed targets.

## Install through Unity Package Manager

In Unity, open **Window > Package Manager**, choose **Install package from git URL**, and enter:

```text
https://github.com/Geurtsy/com.geurts.gameforge.documentation.git
```

Version 0.5.1 is available on main and targets **Unity 6000.3 on Windows**. Import your licensed **Odin Inspector** and **Quantum Console**, including Quantum Console's Input System and TextMesh Pro dependencies, before compiling this package. Both commercial tools are required, used through their actual assembly references, and installed separately. Neither is bundled or downloaded by this package. The repository is public, so the Git URL does not require package-specific credentials.

## Odin documentation dashboard

The `OdinEditorWindow` dashboard automatically checks **both the editor package and documentation whenever it opens**. **Check for updates** refreshes both sources manually. Each update card shows **Installed** and **Available on Git** version numbers, a last-check time, and a plain-language result. Orange means the installed Git commit differs from the available revision (or no successful documentation install is recorded). This also detects changes published without a version bump. Each version now includes its short Git revision; when both version numbers match but revisions differ, the card explains that Git changed without a version bump. A missing installed revision is described as unverified rather than claiming a newer version exists. Matching commits are green; unknown and failed checks are clearly labelled, with unavailable remote versions shown as **Unavailable** rather than stale numbers.

Checking and installation each show an animated activity bar and detailed status. Documentation downloads show bytes received and a percentage when the server supplies a total size; unknown-duration steps use activity animation without inventing a percentage. The package bar stays active while Unity resolves, installs, and recompiles. **Source and managed files** contains copyable source URLs, both package/documentation commit identifiers, and the five documentation-update targets. Content scrolls in small or docked windows.

**Dependencies** explains Odin's dashboard integration. The assembly references require both Odin Inspector and Quantum Console; missing commercial assemblies must be installed before this package compiles. Keep Odin's standard `ODIN_INSPECTOR` define enabled. `GeurtsGameForge.Documentation.Status` exposes the existing dashboard status through Quantum Console as a read-only Editor command. This tool requirement supersedes older optional or no-tool compilation guidance; Documentation remains independent of God.

## Update the editor package from Git

In **Tools > Geurts Game Forge > Documentation**, use **Package update > Update package from Git**. The card shows installed and available package versions, the Git comparison, and update progress. Its configured URL is in Source and managed files. This refreshes the Unity package code; the separate **Documentation update** action replaces the shared documentation and AI instructions.

The button uses Unity's `Client.Add` API with this package's existing Git URL, so the Package Manager window does not need to open. Unity resolves the latest commit for that same reference and manages its package cache, manifest, and lock file. A chosen branch or tag is retained; a pinned commit remains pinned. An unchanged source is reported separately from an update. Git and any credentials needed by the configured source must already work in Unity.

Package updates run only when you press the button. Unity may recompile scripts; the active request and its result are retained across script reloads for the Editor session, and closing the dashboard does not cancel the request. Other documentation actions are disabled during a package update. Errors appear in the package card and allow another attempt. Updates are disabled during compilation, import, and Play mode. A local, embedded, or indirect installation is identified in the UI and is not converted or overwritten; maintain that installation through its source checkout or install the package directly from Git.

This package-maintenance action is an explicitly requested extension to the original companion scope. It does not run the documentation-content update or the .gitignore installer, and it does not directly rewrite Unity's package files.

## Version checks

Dashboard actions run after the current Odin draw finishes. Documentation confirmation is a UI Toolkit dialog with Cancel focused by default; accepting it starts the update after the modal window closes. If Unity cannot start the action, the dashboard or Console explains the reason instead of silently returning.

Package version checks support public `github.com` repositories, including configured forks, branches, tags, pinned commits, and package subfolders. They resolve that exact source and read `package.json` at its resolved commit. Local/embedded installations display the official Git release version but cannot claim Git equality or use self-update. Other Git hosts, local Git URLs, private repositories without anonymous access, and network/rate-limit errors show an explicit unavailable check result; the existing Unity Git update action still uses its configured URL.

Documentation checks resolve official `main`, then read `GeurtsTechniqueManifest.md` at that exact commit. The installed version comes only from the local documentation manifest. A missing installation says **Not installed**; an unreadable/invalid manifest says **Unknown**. Commit comparisons use the per-project last-successful-install signal and do not certify the contents of local files. Versions are informational; Git revision equality determines whether an update is available.

Checks never acquire an installation archive, run an installer, or modify project files. Overlapping open/startup/manual checks share the same active requests. A successful package installation refreshes both checks. Window-open and manual refresh plus the narrow local-manifest read are explicit user-requested extensions to the original startup-only metadata policy; the documentation replacement boundary and confirmation remain unchanged.

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

This package is Windows-only and Editor-only. Its dashboard requires Odin Inspector and its status command requires Quantum Console; it has no dependency on Geurts Game Forge God, Brick Manager, or GameForgeIntelligence. These required tool integrations are explicitly requested changes to the original companion's tool exemptions. They do not change the documentation replacement contract or copy generic Geurts guidance into this package. It does not provide preview, backup, rollback, journaling, migration, recovery, or local-drift preservation.

## Validation

Run `Tools/ValidatePackage.ps1 -DocumentationPath <path-to-GeurtsGameForgeDocumentation>` to run Editor tests in a disposable project. The documentation path supplies only the manifest and Git Ignore Technique as an external integration fixture; the source is not changed or bundled with the package. Without this parameter, tests requiring the real template are reported as skipped. `-StaticOnly` checks package structure without launching Unity.

Provide `-OdinPath <path-to-Assets/Plugins/Sirenix> -QuantumConsolePath <path-to-Assets/Plugins/QFSW/Quantum Console> -ProjectPath <package-root>/work~/UnityValidationOdin` to compile and test against your installed licensed tools. Both paths are required for the integration tests. The validation copy stays under the ignored `work~/` directory and must not be committed or distributed. `-StaticOnly` checks the manifest and assembly contract without importing tools.
