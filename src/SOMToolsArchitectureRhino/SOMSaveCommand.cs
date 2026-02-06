using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace SOMToolsArchitectureRhino
{
    public class SOMSaveCommand : Command
    {
        public override string EnglishName => "SOMSAVE";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: Enumerate materials/textures; if any locally mapped, show warning.
            // TODO: Save with option to not embed textures when SDK supports it.
            RhinoApp.RunScript("_-Save", false);
            return Result.Success;
        }
    }
}
