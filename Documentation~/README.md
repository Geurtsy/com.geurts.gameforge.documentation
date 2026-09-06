# Documentation Updater Package

Open **Tools > Geurts Game Forge > Documentation** in the Unity Editor to view the startup check status or run a confirmed update.

At startup the package requests only the current `main` commit identity. It never changes project files during that check. The Update action downloads a ZIP pinned to the resolved commit, validates the documentation contract and required source files, and then directly replaces only the displayed managed targets.

Generic Geurts guidance is intentionally absent from this package and remains owned by `Geurtsy/GeurtsGameForge_Documentation`.
