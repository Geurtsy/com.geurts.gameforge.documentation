# Geurts Game Forge Documentation

An independent Windows Unity Editor package that installs and updates the project-local copy of the authoritative [Geurts Game Forge documentation](https://github.com/Geurtsy/GeurtsGameForge_Documentation).

The package contains no generic Geurts documentation. It checks the authoritative repository's `main` commit when Unity opens and shows an available update in **Tools > Geurts Game Forge > Documentation**. Nothing is replaced until the user chooses **Update Geurts Game Forge Documentation** and confirms the five listed managed targets.

## Install through Unity Package Manager

In Unity, open **Window > Package Manager**, choose **Install package from git URL**, and enter:

```text
https://github.com/Geurtsy/com.geurts.gameforge.documentation.git
```

Unity 2022.3 or newer on Windows is required. The repository is public, so the Git URL does not require package-specific credentials.

## Managed boundary

A confirmed update replaces only:

- `GeurtsGameForgeDocumentation/`
- `AGENTS.md`
- `.github/copilot-instructions.md`
- `.github/instructions/geurts-unity.instructions.md`
- `.github/instructions/geurts-game-design.instructions.md`

The destination and four-route list are pinned by schema 1.0.0 of `GeurtsTechniques/GeurtsDocumentationCompanionContract.json` and are verified against the downloaded archive before any replacement begins. Documentation release versions and validation entries may advance without changing that closed project boundary. Every other project path is outside the package boundary. In particular, `Docs/GameDesign/` is never inspected or changed.

## Scope

This package is Windows-only and Editor-only, and has no dependency on Geurts Game Forge God, Brick Manager, Odin Inspector, Quantum Console, GameForgeIntelligence, or another optional Unity package. It does not provide preview, backup, rollback, journaling, migration, recovery, or local-drift preservation.
