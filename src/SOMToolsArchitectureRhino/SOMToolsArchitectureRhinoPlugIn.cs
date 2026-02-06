using System;
using Rhino;
using Rhino.PlugIns;

namespace SOMToolsArchitectureRhino
{
    /// <summary>
    /// SOM Tools - Architecture - Rhino plug-in. Provides SOMOPEN, SOMSAVE, SOMINSERT for DriveGuard integration.
    /// </summary>
    public class SOMToolsArchitectureRhinoPlugIn : Rhino.PlugIns.PlugIn
    {
        public SOMToolsArchitectureRhinoPlugIn()
        {
            Instance = this;
        }

        public static SOMToolsArchitectureRhinoPlugIn Instance { get; private set; }

        public override PlugInLoadTime LoadTime => PlugInLoadTime.WhenNeeded;

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            // Menu items (File → SOM Open, etc.) can be added here via Rhino's menu API when available.
            return LoadReturnCode.Success;
        }
    }
}
