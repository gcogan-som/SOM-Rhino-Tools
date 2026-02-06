# Source

**C# (RhinoCommon) plugin** – recommended:

- Open `SOMToolsArchitectureRhino.sln` in Visual Studio 2022.
- Build **SOMToolsArchitectureRhino** (Debug or Release). Output is in `SOMToolsArchitectureRhino\bin\Debug` or `bin\Release` (e.g. `SOMToolsArchitectureRhino.rhp` or `.dll` depending on Rhino SDK; copy to Rhino Plug-ins folder).
- Requires [RhinoCommon development tools](https://developer.rhino3d.com/guides/rhinocommon/installing-tools-windows/) and Rhino 8 (or adjust target).

**Project layout:**

- `SOMToolsArchitectureRhino/` – C# RhinoCommon project
  - `SOMToolsArchitectureRhinoPlugIn.cs` – Plug-in class
  - `SOMOpenCommand.cs`, `SOMSaveCommand.cs`, `SOMInsertCommand.cs` – Commands
  - `SomHelpers.cs` – IsGoogleDrivePath, RhlExists, SetFileReadOnly

**C++ (alternative):** Create a **Rhino 3D Plugin (C++)** project in Visual Studio and add command source files as documented in Architecture.md.
