# SOM Rhino Tools

Rhino 3D plugin (.rhp) with SOM-standard commands for file handling and block insertion. Integrates with [DriveGuard](https://github.com/skidmore-owings-merrill/DriveGuard) for Google Drive read-only protection.

## Commands

| Command   | Purpose                                                                 |
|----------|-------------------------------------------------------------------------|
| **SOMOPEN**  | Open with read-only when `.rhl` exists or file is locked (DriveGuard)   |
| **SOMSAVE**  | Save without embedding textures; warn on locally mapped textures        |
| **SOMINSERT**| Insert block: file browse first, embedded+linked, replace conflicts     |

## Requirements

- Rhino 8 (or 7/9 – adjust build target)
- Visual Studio 2022 with Rhino C++ Plugin Template
- Windows only

## Build

1. Install [Rhino C++ plugin development tools](https://developer.rhino3d.com/guides/cpp/installing-tools-windows/).
2. Create a new **Rhino 3D Plugin (C++)** project in Visual Studio.
3. Add the command source files from `src/`.
4. Build Debug or Release – outputs `SOMRhinoTools.rhp`.

## Installation

**Manual:** Copy `SOMRhinoTools.rhp` to:
- `%APPDATA%\McNeel\Rhino\8.0\Plug-ins\`

**Automatic:** The DriveGuard installer deploys the plugin to all detected Rhino versions.

## Relationship to DriveGuard

- **SOMOPEN** – Uses `.rhl` (and optionally DriveGuard API) to open locked files read-only.
- **SOMSAVE** – Ensures shared-Drive-friendly saves.
- **SOMINSERT** – SOM-standard block insert workflow.

This plugin is distributed and installed automatically by DriveGuard but is developed in a separate repository.

## License

Internal SOM use.
