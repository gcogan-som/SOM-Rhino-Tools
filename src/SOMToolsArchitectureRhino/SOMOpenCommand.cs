using System;
using System.IO;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;

namespace SOMToolsArchitectureRhino
{
    public class SOMOpenCommand : Command
    {
        public override string EnglishName => "SOMOPEN";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var fd = new OpenFileDialog
            {
                Filter = "Rhino 3D Models (*.3dm)|*.3dm|All files (*.*)|*.*",
                Title = "SOM Open"
            };
            if (!fd.ShowOpenDialog())
                return Result.Cancel;

            string path = fd.FileName;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                RhinoApp.WriteLine("SOMOPEN: File not found.");
                return Result.Failure;
            }

            // ------------------------------------------------------------------
            // Lock check: .rhl + DriveGuard API. Shows who has the file locked.
            // Works standalone (rhl only) or enhanced with DriveGuard (name, email).
            // ------------------------------------------------------------------
            var lockInfo = SomHelpers.GetFileLockInfo(path);
            bool wasReadOnly = false;

            if (lockInfo.IsLocked && !lockInfo.IsLockedByMe)
            {
                string who = lockInfo.Description ?? "another user";
                RhinoApp.WriteLine("SOMOPEN: File is locked by " + who + "; opening read-only.");

                // Remember original read-only state so we can restore it after open
                try
                {
                    var attrs = File.GetAttributes(path);
                    wasReadOnly = (attrs & FileAttributes.ReadOnly) != 0;
                }
                catch { }

                // Set read-only so Rhino opens it as read-only
                SomHelpers.SetFileReadOnly(path, true);

                // Inform user who has the lock
                Dialogs.ShowMessage(
                    "This file is locked by " + who + ".\n\n" +
                    "It will be opened in read-only mode to prevent conflicts.",
                    "SOMOPEN - File Locked",
                    ShowMessageButton.OK,
                    ShowMessageIcon.Information);
            }
            else if (lockInfo.IsLocked && lockInfo.IsLockedByMe)
            {
                RhinoApp.WriteLine("SOMOPEN: You already have this file open.");
            }

            // Open the file. Rhino's _-Open expects the path as-is (no double-escaping).
            // Only quote the path to handle spaces; Rhino handles backslashes natively.
            RhinoApp.RunScript("_-Open \"" + path.Replace("\"", "\"\"") + "\"", false);

            // ------------------------------------------------------------------
            // Restore the file's original read-only attribute after Rhino has opened it.
            // Without this, the file stays read-only on disk permanently.
            // ------------------------------------------------------------------
            if (lockInfo.IsLocked && !lockInfo.IsLockedByMe && !wasReadOnly)
            {
                // Brief delay to let Rhino finish opening before we clear the attribute
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(2000);
                    try
                    {
                        SomHelpers.SetFileReadOnly(path, false);
                    }
                    catch { }
                });
            }

            return Result.Success;
        }
    }
}
