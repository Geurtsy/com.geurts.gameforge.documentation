# Documentation Updater Package

Open **Tools > Geurts Game Forge > Documentation** in the Unity Editor to view the startup check status or run a confirmed update.

At startup the package requests only the current `main` commit identity. It never changes project files during that check. The Update action downloads a ZIP pinned to the resolved commit, validates the documentation contract and required source files, and then directly replaces only the displayed managed targets.

Generic Geurts guidance is intentionally absent from this package and remains owned by `Geurtsy/GeurtsGameForge_Documentation`.

To install the documentation-owned `.gitignore`, choose **Tools > Geurts Game Forge > Install Geurts .gitignore** or the matching button in the Documentation window. The installed documentation must contain the valid approved payload in `GeurtsTechniques/GeurtsGitIgnoreTechnique.md` and its manifest entry. The tool does not download or bundle a fallback template.

The confirmation warns that the project-root `.gitignore` will be overwritten and custom rules lost. Cancel is the initial selection; Cancel, Enter, Escape, or closing the dialog changes nothing. **Install and Overwrite** validates the source first, then creates or replaces only `.gitignore`. No backup, merge, or Git tracking changes are performed. This separate action does not run with documentation Update or at startup.
