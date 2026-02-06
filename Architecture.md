# SOM Tools - Architecture - Rhino – Architecture

## Overview

**SOM Tools - Architecture - Rhino** is a Rhino plugin that adds three custom commands for SOM workflows. It can be used **standalone** (lock files only) or **with DriveGuard** (lock files + shared-drive metadata). It never fails if DriveGuard is not installed or not running.

1. **SOMOPEN** – Open with read-only when locked: always checks `.rhl`; if DriveGuard is running, also considers metadata for shared-drive files. Works without DriveGuard.
2. **SOMSAVE** – Save with DriveGuard-friendly options (no texture embedding, warn on local textures)
3. **SOMINSERT** – Insert with sensible defaults, skip dialogs, file browse first

---

## Plugin Language: C# vs C++

**C# (RhinoCommon)** is the typical and more flexible choice for Rhino plugin development:

- **Faster to develop** – RhinoCommon templates, simpler build, easier debugging.
- **Cross-platform** – Same plugin can target Rhino for Windows and Mac (C++ plugins are Windows-only).
- **Recommended** for commercial/complex plugins; scales well to large codebases.
- **Commands:** Inherit from `Rhino.Commands.Command`; add commands via “Empty RhinoCommon Command” template.

**C++** is used when:

- Performance is critical, or you need the lowest-level C++ SDK.
- Windows-only is acceptable.

**Recommendation:** Implement SOM Tools - Architecture - Rhino in **C# (RhinoCommon)** unless there is a specific need for C++. Use the RhinoCommon project wizard and add SOMOPEN, SOMSAVE, SOMINSERT as command classes.

---

## Eliminating Redundancy (One Entry Point)

Rhino does **not** allow replacing built-in command names. Command names must be unique; you cannot register a plugin command named "Open" (it would conflict with the built-in). **Aliases cannot shadow built-in commands either:** Rhino’s `AddAlias` (and typical alias import) states that the alias name “cannot match command names or existing aliases,” so an alias named "Open" that runs SOMOPEN is **not** supported and may be rejected on import.

**Correct approach:**

1. **Register plugin commands** with unique names: **SOMOPEN**, **SOMSAVE**, **SOMINSERT** (required).
2. **Add menu items** from the plugin so users have one clear path:
   - Use the plugin API to insert items into Rhino’s menu (e.g. **File → SOM Open**, **SOM Save**, **SOM Insert**, or a **SOM** submenu under File). C++: `FindMenuItem` + `InsertPlugInItemToRhinoMenu`; C#: equivalent in RhinoCommon PlugIn API.
3. **Optional abbreviations via aliases:** If you want short typing, define aliases that do *not* match built-in names (e.g. **SO** → SOMOPEN, **SS** → SOMSAVE, **SI** → SOMINSERT). See `aliases/SOM-Rhino-Aliases.txt` for an abbreviation-only alias file.

So: **menu items** provide the single entry point from the UI; **command names** stay SOMOPEN/SOMSAVE/SOMINSERT; **aliases** are only for optional abbreviations, not for replacing Open/Save/Insert.

---

## Can We Intercept the Built-in Open?

### Rhino SDK: `OnBeginOpenDocument`

```cpp
virtual void OnBeginOpenDocument(CRhinoDoc& doc, const wchar_t* filename, BOOL32 bMerge, BOOL32 bReference);
```

- Fired **before** the document load completes
- We get the filename and can check `.rhl` / Drive metadata here

**Constraint (from McNeel docs):**  
*"WARNING: Never modify the Rhino document in an OnBeginOpenDocument() override."*

We **cannot** cancel or abort the open. The SDK does not expose a cancelable pre-open hook.

### Conclusion

No true intercept. Use custom commands instead.

---

## SOMOPEN

### Standalone vs DriveGuard

- **Standalone (no DriveGuard):** SOMOPEN only checks for `filename.3dm.rhl` in the same folder. If present, opens read-only. Does not require DriveGuard.
- **With DriveGuard:** In addition to `.rhl`, for paths under the configured Google Drive root the plugin optionally calls `GET /api/is-file-locked?path=...`. If DriveGuard is not installed, not running, or the request fails, the plugin simply skips the metadata check and uses `.rhl` only. It never fails or blocks when DriveGuard is absent (short timeout, no throw).

### Flow

1. User runs `SOMOPEN` (or File → SOM Open).
2. Plugin shows file picker.
3. User selects a `.3dm` file.
4. **Before opening:**
   - Lock = `.rhl` exists **or** (for drive paths only) DriveGuard API reports locked. DriveGuard call is best-effort; if unavailable, only `.rhl` is used.
   - If locked: set file read-only, then open.
   - If not locked: open normally.
5. Open the file (Rhino).

### Config

- `SomHelpers.GoogleDriveRoot` – Path treated as Google Drive (e.g. `G:\`).
- `SomHelpers.DriveGuardApiBase` – DriveGuard API base (e.g. `http://127.0.0.1:5000`). Empty = do not query DriveGuard.
- `SomHelpers.DriveGuardTimeoutMs` – Timeout for API call (e.g. 2000). Does not block if DriveGuard is not running.

---

## SOMSAVE

### Flow

1. User runs `SOMSAVE`.
2. **Before saving:** Enumerate materials/textures; if any are locally mapped, show warning.
3. Save with options to **not** embed textures (if SDK supports it).
4. Proceed with save.

### Hooking Save

`OnBeginSaveDocument` exists but has “don’t modify document” restriction. Use SOMSAVE as custom command.

---

## SOMINSERT

### Behavior

1. **File browse first** – Show file picker immediately (no Insert options dialog).
2. **Skip Insert settings** – No dialog defaulting to World plane.
3. **No overrides** – Defaults for rotation, scale, placement (user prompted).
4. **Block definition type** – **Embedded and linked**.
5. **Block conflicts** – **Replace with imported** (File block) for all; no Block Name Conflict dialog.

### Rhino Insert Options

| Option | SOMINSERT value |
|--------|-----------------|
| Insert as | Block instance |
| Block definition type | Embedded and linked |
| Insertion point | Prompt (user picks) |
| Scale | Uniform, prompt |
| Rotation | Prompt |
| Block conflict | File block (replace all) / Do this for all |

---

## Plugin Structure

**C# (RhinoCommon) – recommended:**

```
SOMToolsArchitectureRhino.rhp (or .rhp from C# build)
├── SOMOpenCommand    – Rhino.Commands.Command
├── SOMSaveCommand    – Rhino.Commands.Command
├── SOMInsertCommand  – Rhino.Commands.Command
├── Menu registration – InsertPlugInItemToRhinoMenu (File → SOM Open, etc.)
├── SomEventWatcher   – optional (e.g. warn on save if local textures)
└── Helpers
    ├── IsGoogleDrivePath(path)
    ├── RhlExists(filepath)
    ├── SetFileReadOnly(filepath, bool)
    └── HasLocallyMappedTextures(doc)
```

**C++ (alternative):** Same logical structure with `CRhinoCommand`, `CRhinoEventWatcher`, and C++ helpers.

---

## Automatic Installation

- DriveGuard installer detects Rhino via `HKLM\SOFTWARE\McNeel\Rhinoceros\`.
- Copies the plugin .rhp (e.g. `SOMToolsArchitectureRhino.rhp`) to `%APPDATA%\McNeel\Rhino\X.0\Plug-ins\` for each version (7, 8, 9).
- Rhino loads the plugin on next launch.
- No user steps required.

---

## Open Questions

1. **DriveGuard API:** Expose `GET /api/is-file-locked?path=...`?
2. **Texture API:** SDK calls for locally mapped textures and save-without-embed.
3. **SOMINSERT:** `_Insert` command-line flags for EmbeddedAndLinked and block conflict resolution.
4. **C# menu API:** Confirm RhinoCommon equivalent of C++ `InsertPlugInItemToRhinoMenu` for adding File → SOM Open / Save / Insert.

---

## References

- [RhinoCommon – Adding commands](https://developer.rhino3d.com/guides/rhinocommon/adding-commands-to-projects/)
- [Rhino C++ API](https://developer.rhino3d.com/api/cpp/)
- [Creating your first C++ plugin](https://developer.rhino3d.com/guides/cpp/your-first-plugin-windows/)
- [Adding a custom menu (C++)](https://developer.rhino3d.com/guides/cpp/adding-a-custom-menu/)
- [Run a Rhino command from a plugin (C#)](https://developer.rhino3d.com/guides/rhinocommon/run-rhino-command-from-plugin/)
- [Insert command](https://docs.mcneel.com/rhino/8/help/en-us/commands/insert.htm)
- [AddAlias – alias name cannot match command names](https://developer.rhino3d.com/api/rhinoscript/application_methods/addalias.htm)
