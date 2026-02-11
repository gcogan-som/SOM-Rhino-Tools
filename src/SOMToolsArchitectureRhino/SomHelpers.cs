using System;
using System.IO;
using System.Net;
using System.Text;
using Rhino;

namespace SOMToolsArchitectureRhino
{
    /// <summary>
    /// Lock info returned by GetFileLockInfo. Contains details about who has the file locked.
    /// </summary>
    public class FileLockInfo
    {
        /// <summary>True if the file is locked by anyone (rhl or DriveGuard metadata).</summary>
        public bool IsLocked { get; set; }

        /// <summary>True if the .rhl lock was created by the current machine (i.e. this user).</summary>
        public bool IsLockedByMe { get; set; }

        /// <summary>Machine name from the .rhl file, or null if no .rhl.</summary>
        public string RhlMachine { get; set; }

        /// <summary>Display name of the lock owner from DriveGuard, or null if unavailable.</summary>
        public string LockedByName { get; set; }

        /// <summary>Email of the lock owner from DriveGuard, or null if unavailable.</summary>
        public string LockedByEmail { get; set; }

        /// <summary>Human-readable description of who has the lock.</summary>
        public string Description
        {
            get
            {
                if (!IsLocked) return null;
                if (!string.IsNullOrEmpty(LockedByName)) return LockedByName;
                if (!string.IsNullOrEmpty(RhlMachine))
                    return IsLockedByMe ? "you (this machine)" : "another user (" + RhlMachine + ")";
                return "another user";
            }
        }
    }

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

        /// <summary>Returns the .rhl path for a given file.</summary>
        public static string GetRhlPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                string name = Path.GetFileName(filePath);
                return Path.Combine(dir, name + ".rhl");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Returns true if filename.3dm.rhl exists in the same folder as the given file.</summary>
        public static bool RhlExists(string filePath)
        {
            string rhlPath = GetRhlPath(filePath);
            return rhlPath != null && File.Exists(rhlPath);
        }

        /// <summary>
        /// Reads the .rhl lock file and returns the machine name that created the lock.
        /// Rhino writes the computer's NetBIOS name into the .rhl file.
        /// Returns null if no .rhl exists or it cannot be read.
        /// </summary>
        public static string GetRhlMachineName(string filePath)
        {
            string rhlPath = GetRhlPath(filePath);
            if (rhlPath == null || !File.Exists(rhlPath)) return null;
            try
            {
                // .rhl files contain the machine name as a null-terminated string
                // followed by possible binary data. Read raw bytes and extract
                // the printable portion.
                byte[] bytes = File.ReadAllBytes(rhlPath);
                if (bytes.Length == 0) return null;

                // Build string from printable ASCII/extended chars, stop at first null
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    if (b == 0) break;
                    if (b >= 32 && b < 127)
                        sb.Append((char)b);
                }
                string name = sb.ToString().Trim();
                return string.IsNullOrEmpty(name) ? null : name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true if the .rhl lock was created by this machine (i.e. the current user).
        /// Returns false if no .rhl exists or it was created by a different machine.
        /// </summary>
        public static bool IsRhlOwnedByMe(string filePath)
        {
            string rhlMachine = GetRhlMachineName(filePath);
            if (string.IsNullOrEmpty(rhlMachine)) return false;
            string myMachine = Environment.MachineName;
            return rhlMachine.Equals(myMachine, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if DriveGuard reports the file as locked (e.g. shared-drive metadata).
        /// Returns false if DriveGuard is not installed, not running, or the request fails. Never throws.
        /// </summary>
        public static bool IsFileLockedByDriveGuard(string filePath)
        {
            var info = GetDriveGuardLockInfo(filePath);
            return info != null && info.IsLocked;
        }

        /// <summary>
        /// Queries DriveGuard for detailed lock info (locked_by name, email, etc.).
        /// Returns null if DriveGuard is not running or the request fails. Never throws.
        /// </summary>
        public static FileLockInfo GetDriveGuardLockInfo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;
            string baseUrl = DriveGuardApiBase?.Trim();
            if (string.IsNullOrEmpty(baseUrl)) return null;
            try
            {
                string pathEncoded = Uri.EscapeDataString(filePath);
                string url = baseUrl.TrimEnd('/') + "/api/is-file-locked?path=" + pathEncoded + "&details=true";
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = Math.Max(500, DriveGuardTimeoutMs);
                req.ReadWriteTimeout = req.Timeout;
                req.KeepAlive = false;
                using (var resp = (HttpWebResponse)req.GetResponse())
                {
                    if (resp.StatusCode != HttpStatusCode.OK) return null;
                    using (var reader = new StreamReader(resp.GetResponseStream()))
                    {
                        string body = reader.ReadToEnd()?.Trim();
                        if (string.IsNullOrEmpty(body)) return null;
                        // Parse simple JSON: {"locked":true,"locked_by":"Name","locked_by_email":"email"}
                        return ParseLockInfoJson(body);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Minimal JSON parser for lock info (avoids Newtonsoft dependency).</summary>
        private static FileLockInfo ParseLockInfoJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            // Handle plain text "true"/"false" responses (backwards compat)
            string upper = json.Trim().ToUpperInvariant();
            if (upper == "TRUE" || upper == "FALSE")
                return new FileLockInfo { IsLocked = upper == "TRUE" };

            var info = new FileLockInfo();
            info.IsLocked = json.Contains("\"locked\":true") || json.Contains("\"locked\": true");
            info.LockedByName = ExtractJsonString(json, "locked_by");
            info.LockedByEmail = ExtractJsonString(json, "locked_by_email");
            return info;
        }

        /// <summary>Extract a string value from JSON by key (simple, no nested objects).</summary>
        private static string ExtractJsonString(string json, string key)
        {
            string pattern = "\"" + key + "\":\"";
            int start = json.IndexOf(pattern, StringComparison.Ordinal);
            if (start < 0)
            {
                pattern = "\"" + key + "\": \"";
                start = json.IndexOf(pattern, StringComparison.Ordinal);
            }
            if (start < 0) return null;
            start += pattern.Length;
            int end = json.IndexOf("\"", start, StringComparison.Ordinal);
            if (end < 0) return null;
            string val = json.Substring(start, end - start).Trim();
            return string.IsNullOrEmpty(val) ? null : val;
        }

        /// <summary>
        /// Get comprehensive lock info for a file: checks .rhl + DriveGuard.
        /// Returns a FileLockInfo with all available details.
        /// Works standalone (rhl only) or enhanced with DriveGuard (name, email).
        /// </summary>
        public static FileLockInfo GetFileLockInfo(string filePath)
        {
            var info = new FileLockInfo();

            // 1. Check .rhl
            string rhlMachine = GetRhlMachineName(filePath);
            if (rhlMachine != null)
            {
                info.IsLocked = true;
                info.RhlMachine = rhlMachine;
                info.IsLockedByMe = rhlMachine.Equals(
                    Environment.MachineName, StringComparison.OrdinalIgnoreCase);
            }

            // 2. Check DriveGuard API for richer info (name, email)
            if (IsGoogleDrivePath(filePath))
            {
                var dgInfo = GetDriveGuardLockInfo(filePath);
                if (dgInfo != null && dgInfo.IsLocked)
                {
                    info.IsLocked = true;
                    if (!string.IsNullOrEmpty(dgInfo.LockedByName))
                        info.LockedByName = dgInfo.LockedByName;
                    if (!string.IsNullOrEmpty(dgInfo.LockedByEmail))
                        info.LockedByEmail = dgInfo.LockedByEmail;
                }
            }

            return info;
        }

        /// <summary>
        /// Returns true if the file should be opened read-only.
        /// Standalone: only .rhl is used. With DriveGuard: also considers shared-drive metadata.
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
