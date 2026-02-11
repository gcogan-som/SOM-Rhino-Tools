using System;
using System.IO;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;

namespace SOMToolsArchitectureRhino
{
    public class SOMSaveCommand : Command
    {
        public override string EnglishName => "SOMSAVE";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // ------------------------------------------------------------------
            // 1. Safety check: is this a new/unsaved document?
            // ------------------------------------------------------------------
            string filePath = doc.Path;
            bool isNewFile = string.IsNullOrEmpty(filePath) || !File.Exists(filePath);

            if (isNewFile)
            {
                // New file -- go straight to SaveAs without textures
                RhinoApp.WriteLine("SOMSAVE: New file -- saving without embedded textures.");
                return SaveWithoutTextures(doc);
            }

            // ------------------------------------------------------------------
            // 2. Lock protection: is someone else editing this file?
            //    Check .rhl (works standalone) + DriveGuard API (enhanced info).
            // ------------------------------------------------------------------
            var lockInfo = SomHelpers.GetFileLockInfo(filePath);

            if (lockInfo.IsLocked && !lockInfo.IsLockedByMe)
            {
                // Another user has this file open -- warn before saving
                string who = lockInfo.Description ?? "another user";
                string message =
                    "WARNING: This file is currently open by " + who + ".\n\n" +
                    "Saving now may create conflicts or overwrite their changes.\n\n" +
                    "Do you want to save anyway?";

                var result = Dialogs.ShowMessage(
                    message,
                    "SOMSAVE - File In Use",
                    ShowMessageButton.YesNo,
                    ShowMessageIcon.Warning);

                if (result != ShowMessageResult.Yes)
                {
                    RhinoApp.WriteLine("SOMSAVE: Save cancelled by user.");
                    return Result.Cancel;
                }
                RhinoApp.WriteLine("SOMSAVE: User chose to save despite lock by " + who);
            }

            // ------------------------------------------------------------------
            // 3. Read-only check: is the file read-only on disk?
            // ------------------------------------------------------------------
            try
            {
                var attrs = File.GetAttributes(filePath);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                {
                    RhinoApp.WriteLine("SOMSAVE: File is read-only. Use SaveAs to save a copy.");
                    Dialogs.ShowMessage(
                        "This file is read-only and cannot be overwritten.\n\n" +
                        "Use File > Save As to save a copy, or ask the file owner to release the lock.",
                        "SOMSAVE - Read-Only File",
                        ShowMessageButton.OK,
                        ShowMessageIcon.Information);
                    return Result.Cancel;
                }
            }
            catch
            {
                // Can't check attributes -- proceed anyway
            }

            // ------------------------------------------------------------------
            // 4. Save without embedded textures
            // ------------------------------------------------------------------
            return SaveWithoutTextures(doc);
        }

        /// <summary>
        /// Save the document without embedding textures.
        /// Uses Rhino's scripted Save command with SaveTextures=No.
        /// Textures are referenced by path but not stored inside the .3dm file,
        /// keeping file sizes small for shared drive workflows.
        /// </summary>
        private Result SaveWithoutTextures(RhinoDoc doc)
        {
            // Use the scripted command to save without embedding textures.
            // _-Save enters command-line mode; SaveTextures No disables embedding.
            // If the file has never been saved, Rhino will prompt for a filename.
            string filePath = doc.Path;
            bool isNewFile = string.IsNullOrEmpty(filePath) || !File.Exists(filePath);

            if (isNewFile)
            {
                // For new files, use SaveAs so the user picks a location
                RhinoApp.RunScript("_-SaveAs _SaveTextures=_No _Enter", false);
            }
            else
            {
                // For existing files, save in place without textures
                RhinoApp.RunScript("_-Save _SaveTextures=_No _Enter", false);
            }

            RhinoApp.WriteLine("SOMSAVE: Saved without embedded textures.");
            return Result.Success;
        }
    }
}
