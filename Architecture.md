# SOM Rhino Tools – Architecture

## Overview

Rhino C++ plugin that adds three custom commands for SOM workflows and DriveGuard integration:

1. **SOMOPEN** – Open with read-only protection when `.rhl` exists or Drive metadata indicates someone is editing
2. **SOMSAVE** – Save with DriveGuard-friendly options (no texture embedding, warn on local textures)
3. **SOMINSERT** – Insert with sensible defaults, skip dialogs, file browse first

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

### Flow

1. User runs `SOMOPEN` (or File → SOM Open).
2. Plugin shows file picker (`CRhinoGetFile` or equivalent).
3. User selects a `.3dm` file.
4. **Before opening:**
   - Check if path is on Google Drive (configurable root, e.g. `G:\`).
   - If yes: check for `filename.3dm.rhl` in the same folder.
   - Optionally query DriveGuard: `GET /api/is-file-locked?path=...`
   - If `.rhl` exists OR metadata says someone else is editing:
     - Set file read-only: `attrib +R filepath`
     - Open the file (Rhino opens it read-only).
   - If not locked: open normally.

### Metadata Check

- **Option A:** DriveGuard HTTP API
- **Option B:** `.rhl` only (simpler)
- **Option C:** Google Drive API (heavy)

Recommendation: Start with Option B.

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

## Plugin Structure (C++ / Rhino SDK)

```
SOMRhinoTools.rhp
├── SOMOpenCommand    – CRhinoCommand
├── SOMSaveCommand    – CRhinoCommand
├── SOMInsertCommand  – CRhinoCommand
├── SomEventWatcher   – CRhinoEventWatcher (optional)
│   └── OnBeginSaveDocument – warn on local textures
└── Helpers
    ├── IsGoogleDrivePath(path)
    ├── RhlExists(filepath)
    ├── SetFileReadOnly(filepath, bool)
    └── HasLocallyMappedTextures(doc)
```

---

## Automatic Installation

- DriveGuard installer detects Rhino via `HKLM\SOFTWARE\McNeel\Rhinoceros\`.
- Copies `SOMRhinoTools.rhp` to `%APPDATA%\McNeel\Rhino\X.0\Plug-ins\` for each version (7, 8, 9).
- Rhino loads the plugin on next launch.
- No user steps required.

---

## Open Questions

1. **Aliasing Open:** Can File → Open run SOMOPEN via alias?
2. **DriveGuard API:** Expose `GET /api/is-file-locked?path=...`?
3. **Texture API:** SDK calls for locally mapped textures and save-without-embed.
4. **SOMINSERT:** `_Insert` command-line flags for EmbeddedAndLinked and block conflict resolution.

---

## References

- [Rhino C++ API](https://developer.rhino3d.com/api/cpp/)
- [Creating your first C++ plugin](https://developer.rhino3d.com/guides/cpp/your-first-plugin-windows/)
- [Insert command](https://docs.mcneel.com/rhino/8/help/en-us/commands/insert.htm)
