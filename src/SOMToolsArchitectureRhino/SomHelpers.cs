using System;
using System.IO;
using System.Net;
using Rhino;

namespace SOMToolsArchitectureRhino
{
    /// <summary>
    /// Helpers for .rhl lock files and optional DriveGuard integration.
    /// Plugin works standalone (lock files only) or enhanced when DriveGuard is running (metadata too).
    /// </summary>
    public static class SomHelpers
    {
        /// <summary>Configurable root for Google Drive (e.g. G:\ or a network path).</summary>
        public static string GoogleDriveRoot { get; set; } = "G:\\";

        /// <summary>DriveGuard API base URL (e.g. http://127.0.0.1:5000). Empty or null = do not query DriveGuard.</summary>
        public static string DriveGuardApiBase { get; set; } = "http://127.0.0.1:5000";

        /// <summary>Timeout in ms when calling DriveGuard API. Does not block if DriveGuard is not running.</summary>
        public static int DriveGuardTimeoutMs { get; set; } = 2000;

        /// <summary>Returns true if the path is under the configured Google Drive root.</summary>
        public static bool IsGoogleDrivePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            string root = GoogleDriveRoot?.TrimEnd('\\', '/');
            if (string.IsNullOrEmpty(root)) return false;
            try
            {
                string full = Path.GetFullPath(filePath);
                return full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Returns true if filename.3dm.rhl exists in the same folder as the given file. Works with or without DriveGuard.</summary>
        public static bool RhlExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                string name = Path.GetFileName(filePath);
                string rhlPath = Path.Combine(dir, name + ".rhl");
                return File.Exists(rhlPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if DriveGuard reports the file as locked (e.g. shared-drive metadata).
        /// Returns false if DriveGuard is not installed, not running, or the request fails. Never throws.
        /// Only call for paths where IsGoogleDrivePath is true if you want to avoid unnecessary requests.
        /// </summary>
        public static bool IsFileLockedByDriveGuard(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            string baseUrl = DriveGuardApiBase?.Trim();
            if (string.IsNullOrEmpty(baseUrl)) return false;
            try
            {
                string pathEncoded = Uri.EscapeDataString(filePath);
                string url = baseUrl.TrimEnd('/') + "/api/is-file-locked?path=" + pathEncoded;
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = Math.Max(500, DriveGuardTimeoutMs);
                req.ReadWriteTimeout = req.Timeout;
                req.KeepAlive = false;
                using (var resp = (HttpWebResponse)req.GetResponse())
                {
                    if (resp.StatusCode != HttpStatusCode.OK) return false;
                    using (var reader = new StreamReader(resp.GetResponseStream()))
                    {
                        string body = reader.ReadToEnd()?.Trim().ToUpperInvariant();
                        return body == "TRUE" || body == "1" || body == "YES";
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the file should be opened read-only: .rhl exists and/or (for drive paths) DriveGuard reports locked.
        /// Standalone: only .rhl is used. With DriveGuard running: also considers shared-drive metadata. Never fails if DriveGuard is absent.
        /// </summary>
        public static bool IsFileLocked(string filePath)
        {
            if (RhlExists(filePath)) return true;
            if (IsGoogleDrivePath(filePath) && IsFileLockedByDriveGuard(filePath)) return true;
            return false;
        }

        /// <summary>Sets or clears the read-only attribute on the file.</summary>
        public static void SetFileReadOnly(string filePath, bool readOnly)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            try
            {
                var attrs = File.GetAttributes(filePath);
                if (readOnly)
                    attrs |= FileAttributes.ReadOnly;
                else
                    attrs &= ~FileAttributes.ReadOnly;
                File.SetAttributes(filePath, attrs);
            }
            catch
            {
                // Ignore; Rhino may still open read-only if we can't set attrib
            }
        }
    }
}
