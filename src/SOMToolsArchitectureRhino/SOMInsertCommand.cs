using System.IO;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace SOMToolsArchitectureRhino
{
    public class SOMInsertCommand : Command
    {
        public override string EnglishName => "SOMINSERT";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var fd = new Rhino.UI.OpenFileDialog
            {
                Filter = "Rhino 3D Models (*.3dm)|*.3dm|All files (*.*)|*.*",
                Title = "SOM Insert - Select file"
            };
            if (!fd.ShowOpenDialog())
                return Result.Cancel;

            string path = fd.FileName;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                RhinoApp.WriteLine("File not found.");
                return Result.Failure;
            }

            // Run Insert with file; exact flags for EmbeddedAndLinked and block conflict TBD per Rhino SDK.
            RhinoApp.RunScript($"_-Insert \"{path.Replace("\"", "\"\"")}\"", false);
            return Result.Success;
        }
    }
}
