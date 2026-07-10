using System;
using System.IO;
using System.Text;
using System.Threading;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    /// <summary>File I/O and path operations. Ported from FileSystemUtils.bas.</summary>
    /// <remarks>
    /// <b>Return value convention:</b> I/O methods (Write, Delete, Copy, Move,
    /// EnsureFolder, etc.) return <c>true</c> on success and throw on failure.
    /// The <c>true</c> return is for callers that need a boolean; the UDF layer
    /// relies on <see cref="Foundation.OutputWrapper.WrapError"/> to convert
    /// exceptions to <c>#VALUE!</c>, so the return value is not user-visible
    /// in Excel.
    /// </remarks>
    internal static class FileSystemCore
    {
        private static string? _sandboxRoot;
        private static bool _sandboxWarningEmitted;

        /// <summary>
        /// Optional sandbox root directory. When set, all file I/O is restricted to
        /// paths within this directory. Set to null to disable (default).
        /// Set this before loading workbooks that call FS.* UDFs.
        /// Thread-safe: uses Volatile.Read/Write to prevent data races between
        /// the AutoClose cleanup thread and concurrently executing UDF threads.
        /// </summary>
        public static string? SandboxRoot
        {
            get => Volatile.Read(ref _sandboxRoot);
            set { if (_sandboxRoot != value) System.Diagnostics.Trace.WriteLine($"[FileSystemCore] SandboxRoot changed from '{_sandboxRoot}' to '{value}'."); Volatile.Write(ref _sandboxRoot, value); Volatile.Write(ref _sandboxWarningEmitted, value != null); }
        }

        /// <summary>Maximum file size in bytes for read operations. Default 100 MB. Set to 0 for unlimited.</summary>
        public static long MaxReadSizeBytes { get; set; } = 100_000_000;

        /// <summary>Maximum content length in bytes for write operations. Default 100 MB. Set to 0 for unlimited.</summary>
        public static long MaxWriteSizeBytes { get; set; } = 100_000_000;

        /// <summary>
        /// Throws <see cref="UnauthorizedAccessException"/> if <paramref name="path"/>
        /// (after normalization) is outside <see cref="SandboxRoot"/>.
        /// No-op when <see cref="SandboxRoot"/> is null, but emits a one-time
        /// diagnostic warning so operators are aware the sandbox is disabled.
        /// </summary>
        internal static void ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(SandboxRoot))
            {
                bool warned = Volatile.Read(ref _sandboxWarningEmitted);
                if (!warned)
                {
                    Volatile.Write(ref _sandboxWarningEmitted, true);
                    System.Diagnostics.Trace.WriteLine(
                        "[FileSystemCore] SandboxRoot is null — file operations are unrestricted. " +
                        "Set FileSystemCore.SandboxRoot before loading untrusted workbooks.");
                }
                return;
            }
            NormalizePath(path); // sandbox check (throws UnauthorizedAccessException if outside sandbox)
        }

        internal static string NormalizePath(string p)
        {
            string normalized = Path.GetFullPath(p);
            // Sandbox check (inline to avoid recursion: ValidatePath calls NormalizePath internally)
            if (!string.IsNullOrEmpty(SandboxRoot))
            {
                string root = Path.GetFullPath(SandboxRoot);
                if (root.Length > 0 && root[root.Length - 1] != Path.DirectorySeparatorChar)
                    root += Path.DirectorySeparatorChar;
                if (!(normalized + Path.DirectorySeparatorChar).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException(
                        "Path is outside the sandbox root.");
                // Check path components beyond sandbox root for reparse points
                // (junctions/symlinks) — Path.GetFullPath does not resolve them,
                // but System.IO APIs follow them, so a junction could bypass the
                // string-prefix sandbox check above.
                if (normalized.Length > root.Length)
                {
                    string remaining = normalized.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
                    string checkPath = root.TrimEnd(Path.DirectorySeparatorChar);
                    foreach (var segment in remaining.Split(new[] { Path.DirectorySeparatorChar },
                             StringSplitOptions.RemoveEmptyEntries))
                    {
                        checkPath = Path.Combine(checkPath, segment);
                        if (Directory.Exists(checkPath) || File.Exists(checkPath))
                        {
                            var attr = File.GetAttributes(checkPath);
                            if ((attr & FileAttributes.ReparsePoint) != 0)
                                throw new UnauthorizedAccessException(
                                    "Path crosses a junction point or symbolic link and is blocked by the sandbox.");
                        }
                    }
                }
            }
            return normalized;
        }
        internal static string PathCombine(string a, string b) => Path.Combine(a, b);
        internal static string GetFileName(string p) => Path.GetFileName(p);
        internal static string GetBaseName(string p) => Path.GetFileNameWithoutExtension(p);
        internal static string GetExtension(string p) => Path.GetExtension(p);
        internal static string GetFolderPath(string p) => Path.GetDirectoryName(p) ?? "";
        /// <summary>
        /// Validates path SYNTAX only — checks for null/empty, invalid characters,
        /// and whether <c>Path.GetFullPath</c> succeeds.
        /// </summary>
        /// <remarks>
        /// This is a pure format check. It does NOT validate against <see cref="SandboxRoot"/> —
        /// sandbox enforcement is the responsibility of individual I/O methods
        /// (<see cref="FileExists"/>, <see cref="ReadTextFile"/>, etc.) which call
        /// <see cref="ValidatePath"/> before accessing the file system.
        /// Callers who need sandbox authorisation should call <see cref="ValidatePath"/> directly.
        /// </remarks>
        internal static bool IsPathValid(string p) { if(string.IsNullOrEmpty(p))return false; if(p.IndexOfAny(System.IO.Path.GetInvalidPathChars())>=0)return false; try{Path.GetFullPath(p);return true;}catch(Exception ex) when(ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException){return false;} }
        internal static bool FileExists(string p) { ValidatePath(p); return File.Exists(p); }
        internal static long GetFileSize(string p) { ValidatePath(p); if (!File.Exists(p)) throw new System.IO.FileNotFoundException($"File not found: {p}"); return new FileInfo(p).Length; }
        internal static string ReadTextFile(string p, Encoding? e = null) { ValidatePath(p); if (MaxReadSizeBytes > 0 && new FileInfo(p).Length > MaxReadSizeBytes) throw new ArgumentException($"File size exceeds maximum read limit of {MaxReadSizeBytes} bytes."); return File.ReadAllText(p, e ?? Encoding.UTF8); }
        internal static string[] ReadAllLines(string p, Encoding? e = null) { ValidatePath(p); if (MaxReadSizeBytes > 0 && new FileInfo(p).Length > MaxReadSizeBytes) throw new ArgumentException($"File size exceeds maximum read limit of {MaxReadSizeBytes} bytes."); return File.ReadAllLines(p, e ?? Encoding.UTF8); }
        internal static bool WriteTextFile(string p, string c, Encoding? e = null) { ValidatePath(p); var enc = e ?? Encoding.UTF8; if (MaxWriteSizeBytes > 0 && enc.GetByteCount(c) > MaxWriteSizeBytes) throw new ArgumentException($"Content byte length exceeds maximum write limit of {MaxWriteSizeBytes} bytes."); File.WriteAllText(p, c, enc); return true; }
        internal static bool AppendTextFile(string p, string c, Encoding? e = null) { ValidatePath(p); var enc = e ?? Encoding.UTF8; if (MaxWriteSizeBytes > 0 && enc.GetByteCount(c) > MaxWriteSizeBytes) throw new ArgumentException($"Content byte length exceeds maximum write limit of {MaxWriteSizeBytes} bytes."); File.AppendAllText(p, c, enc); return true; }
        internal static bool DeleteFile(string p) { ValidatePath(p); if (File.Exists(p)) File.Delete(p); return true; }
        internal static bool CopyFile(string s, string d, bool o = false) { ValidatePath(s); ValidatePath(d); File.Copy(s, d, o); return true; }
        internal static bool MoveFile(string s, string d) { ValidatePath(s); ValidatePath(d); File.Move(s, d); return true; }
        internal static bool FolderExists(string p) { ValidatePath(p); return Directory.Exists(p); }
        internal static bool EnsureFolder(string p) { ValidatePath(p); if (!Directory.Exists(p)) Directory.CreateDirectory(p); return true; }
        internal static string[] ListFiles(string p, string pat = "*") { ValidatePath(p); return Directory.GetFiles(p, pat); }
        internal static string[] ListFolders(string p, string pat = "*") { ValidatePath(p); return Directory.GetDirectories(p, pat); }
        internal static bool DeleteFolder(string p, bool r = false)
        {
            ValidatePath(p);
            if (!Directory.Exists(p)) return true;
            if (!r) { Directory.Delete(p); return true; }
            DeleteFolderRecursive(p);
            return true;
        }

        /// <summary>
        /// Recursively delete a directory without following NTFS junction points or
        /// symbolic links — <see cref="Directory.Delete(string, bool)"/> follows them,
        /// which could delete content outside the sandbox.
        /// </summary>
        private static void DeleteFolderRecursive(string p)
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(p))
            {
                var attr = File.GetAttributes(entry);
                if ((attr & FileAttributes.ReparsePoint) != 0)
                {
                    // Junction / symlink: delete the link itself, don't follow it
                    if ((attr & FileAttributes.Directory) != 0)
                        Directory.Delete(entry);
                    else
                        File.Delete(entry);
                }
                else if ((attr & FileAttributes.Directory) != 0)
                {
                    DeleteFolderRecursive(entry);
                }
                else
                {
                    File.Delete(entry);
                }
            }
            Directory.Delete(p);
        }
        /// <summary>Enumerate logical drives. When <see cref="SandboxRoot"/> is set,
        /// returns only the sandbox root drive to limit filesystem reconnaissance.</summary>
        internal static string[] GetDrives()
        {
            if (!string.IsNullOrEmpty(SandboxRoot))
                return new[] { Path.GetPathRoot(Path.GetFullPath(SandboxRoot))! };
            return Array.ConvertAll(DriveInfo.GetDrives(), d => d.Name);
        }
        /// <summary>Returns the current working directory. When <see cref="SandboxRoot"/> is set,
        /// returns the sandbox root to avoid leaking the real working directory.</summary>
        internal static string GetCurrentFolder()
            => !string.IsNullOrEmpty(SandboxRoot) ? Path.GetFullPath(SandboxRoot) : Directory.GetCurrentDirectory();
        /// <summary>Returns the system temporary folder. When <see cref="SandboxRoot"/> is set,
        /// returns the sandbox root so temp files created outside FS.* stay within the sandbox.</summary>
        internal static string GetTempPath()
            => !string.IsNullOrEmpty(SandboxRoot) ? Path.GetFullPath(SandboxRoot) : Path.GetTempPath();
        /// <summary>
        /// Returns a zero-byte temporary file path. When <see cref="SandboxRoot"/> is set,
        /// the file is created inside the sandbox; otherwise the system TEMP directory is used.
        /// </summary>
        internal static string GetTempFileName()
        {
            if (!string.IsNullOrEmpty(SandboxRoot))
            {
                EnsureFolder(SandboxRoot!);
                string path = Path.Combine(SandboxRoot!, Path.GetRandomFileName());
                using (File.Create(path)) { }
                return path;
            }
            return Path.GetTempFileName();
        }
    }
}
