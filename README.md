# SOM Tools - Architecture - Rhino

Rhino 3D plugin (.rhp) with SOM-standard commands for file handling and block insertion. Works **standalone** (lock files only) or **with [DriveGuard](https://github.com/gcogan-som/DriveGuard)** (lock files + shared-drive metadata). Never requires or fails if DriveGuard is not installed or not running.

**Repo name:** This repository can be renamed to **SOM-Tools-Architecture-Rhino** on GitHub (Settings → Repository name) to match the plugin name.

## Commands

| Command   | Purpose                                                                 |
|----------|-------------------------------------------------------------------------|
| **SOMOPEN**  | Open with read-only when `.rhl` exists or file is locked (DriveGuard)   |
| **SOMSAVE**  | Save without embedding textures; warn on locally mapped textures        |
| **SOMINSERT**| Insert block: file browse first, embedded+linked, replace conflicts     |

**Single entry point:** The plugin adds **menu items** (e.g. File → SOM Open, SOM Save, SOM Insert) so users have one path from the UI. Rhino does not allow replacing built-in command names or aliasing "Open"/"Save"/"Insert" to our commands; see [Architecture](Architecture.md). Optional: import `aliases/SOM-Rhino-Aliases.txt` for **abbreviations** (SO → SOMOPEN, SS → SOMSAVE, SI → SOMINSERT).

## Requirements

- Rhino 8 (or 7/9 – adjust build target)
- **C# (recommended):** Visual Studio 2022 + [RhinoCommon / Rhino plugin tools](https://developer.rhino3d.com/guides/rhinocommon/installing-tools-windows/)
- **C++ (alternative):** Visual Studio 2022 + Rhino C++ Plugin Template (Windows only)

## Build

**C# (recommended):**

1. Install [RhinoCommon development tools](https://developer.rhino3d.com/guides/rhinocommon/installing-tools-windows/) and Rhino 8.
2. Open `src/SOMToolsArchitectureRhino.sln` in Visual Studio 2022 and build (Debug or Release).
3. Output is in `src/SOMToolsArchitectureRhino/bin/Debug` or `bin/Release`. Copy the built assembly (e.g. `SOMToolsArchitectureRhino.dll`) to Rhino’s Plug-ins folder; some setups expect the file to be named `.rhp`.

**C++:**

1. Install [Rhino C++ plugin development tools](https://developer.rhino3d.com/guides/cpp/installing-tools-windows/).
2. Create a new **Rhino 3D Plugin (C++)** project; add the command source files from `src/`.
3. Build – outputs the plugin .rhp (e.g. `SOMToolsArchitectureRhino.rhp`).

## Installation

**Manual:** Copy the plugin .rhp to:
- `%APPDATA%\McNeel\Rhino\8.0\Plug-ins\`

**Automatic:** The DriveGuard installer deploys the plugin to all detected Rhino versions.

## Relationship to DriveGuard

- **SOMOPEN** – Always uses `.rhl` for lock detection. If DriveGuard is running, also queries its API for shared-drive metadata; if not, only `.rhl` is used. Never fails when DriveGuard is absent.
- **SOMSAVE** – Shared-Drive-friendly save options.
- **SOMINSERT** – SOM-standard block insert workflow.

The plugin can be used without DriveGuard. When DriveGuard is installed, the installer can optionally deploy this plugin; both can also be used independently.

## License

Internal SOM use.
