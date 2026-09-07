# Documentation Updater Package

Open **Tools > Geurts Game Forge > Documentation** in the Unity Editor to view the startup check status or run a confirmed update.

The dashboard requires a separately installed licensed copy of Odin Inspector. It presents the source status first, documentation Update as the primary action, and the .gitignore installer in its own secondary card. Expand **Source and managed files** for copyable commit identifiers and the five exact managed paths. A matching commit compares source history only; it does not verify local contents. Without Odin, the menu shows installation guidance without breaking compilation.

**Package update > Update package from Git** updates the editor package itself without opening Package Manager. It refreshes the existing Git URL, preserving the repository and any chosen branch, tag, or pinned commit. A pinned commit does not advance. The card shows the installed version, progress, and the result or error. Unity handles package resolution and any recompilation; request tracking survives script reloads and closing the window. Local, embedded, and indirect installations are not replaced. No package update runs automatically.

**Dependencies** reports Odin Inspector as installed or missing, explains how it improves UI quality, and links to installation guidance. Odin is mandatory: all documentation tools, including package updates and the standalone .gitignore action, are unavailable until Odin is installed. It is licensed and installed separately.

At startup the package requests only the current `main` commit identity. It never changes project files during that check. The Update action downloads a ZIP pinned to the resolved commit, validates the documentation contract and required source files, and then directly replaces only the displayed managed targets.

Generic Geurts guidance is intentionally absent from this package and remains owned by `Geurtsy/GeurtsGameForge_Documentation`.

To install the documentation-owned `.gitignore`, choose **Tools > Geurts Game Forge > Install Geurts .gitignore** or the matching button in the Documentation window. The installed documentation must contain the valid approved payload in `GeurtsTechniques/GeurtsGitIgnoreTechnique.md` and its manifest entry. The tool does not download or bundle a fallback template.

The confirmation warns that the project-root `.gitignore` will be overwritten and custom rules lost. Cancel is the initial selection; Cancel, Enter, Escape, or closing the dialog changes nothing. **Install and Overwrite** validates the source first, then creates or replaces only `.gitignore`. No backup, merge, or Git tracking changes are performed. This separate action does not run with documentation Update or at startup.
