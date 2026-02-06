using System.IO;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace SOMToolsArchitectureRhino
{
    public class SOMOpenCommand : Command
    {
        public override string EnglishName => "SOMOPEN";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var fd = new Rhino.UI.OpenFileDialog
            {
                Filter = "Rhino 3D Models (*.3dm)|*.3dm",
                Title = "SOM Open"
            };
            if (!fd.ShowOpenDialog())
                return Result.Cancel;

            string path = fd.FileName;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                RhinoApp.WriteLine("File not found.");
                return Result.Failure;
            }

            // Lock = .rhl present and/or (when DriveGuard is running) metadata for shared-drive files. Never fails if DriveGuard is absent.
            bool locked = SomHelpers.IsFileLocked(path);
            if (locked)
            {
                SomHelpers.SetFileReadOnly(path, true);
                RhinoApp.WriteLine("File is locked; opening read-only.");
            }

            string escaped = path.Replace("\\", "\\\\").Replace("\"", "\\\"");
            RhinoApp.RunScript("_-Open \"" + escaped + "\"", false);
            return Result.Success;
        }
    }
}
