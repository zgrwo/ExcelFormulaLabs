using System;
using System.IO;
using System.Text;
using System.Threading;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    /// <summary>
    /// Immutable sandbox configuration. Once set via <see cref="FileSystemCore.Initialize"/>,
    /// the configuration cannot be changed — eliminating all race conditions between
    /// UDF execution threads and the AutoClose cleanup thread.
    /// </summary>
    /// <param name="Root">Sandbox root directory. Null means unrestricted access.</param>
    /// <param name="MaxReadBytes">Maximum file size for read operations. 0 = unlimited.</param>
    /// <param name="MaxWriteBytes">Maximum content size for write operations. 0 = unlimited.</param>
    internal sealed record SandboxConfig(string? Root, long MaxReadBytes = 100_000_000, long MaxWriteBytes = 100_000_000);

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
        private static volatile SandboxConfig _config = new(null);
        private static int _initialized;
        private static volatile bool _sessionEnded;
        private static int _warningEmitted;

        /// <summary>
        /// One-time initialization of sandbox configuration.
        /// Must be called before any FS.* UDF executes (typically in AutoOpen).
        /// Calling more than once throws <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="config">Immutable sandbox configuration.</param>
        public static void Initialize(SandboxConfig config)
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
                throw new InvalidOperationException(
                    "[FileSystemCore] Sandbox already initialized. Configuration is immutable — " +
                    "reload the add-in to change sandbox settings.");
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sessionEnded = false;
            System.Diagnostics.Trace.WriteLine(
                $"[FileSystemCore] Initialized: Root='{config.Root ?? "(unrestricted)"}', " +
                $"MaxRead={config.MaxReadBytes}, MaxWrite={config.MaxWriteBytes}");
        }

        /// <summary>
        /// Resets initialization state. FOR UNIT TESTS ONLY.
        /// Allows tests to call <see cref="Initialize"/> multiple times.
        /// </summary>
        internal static void ResetForTesting()
        {
            Interlocked.Exchange(ref _initialized, 0);
            Interlocked.Exchange(ref _warningEmitted, 0);
            _config = new SandboxConfig(null);
            _sessionEnded = false;
        }

        /// <summary>
        /// Marks the session as ended (called from AutoClose).
        /// Subsequent FS.* calls will throw <see cref="InvalidOperationException"/>.
        /// The sandbox config remains immutable — no race window on teardown.
        /// </summary>
        internal static void EndSession()
        {
            _sessionEnded = true;
            System.Diagnostics.Trace.WriteLine("[FileSystemCore] Session ended.");
        }

        /// <summary>
        /// Gets the current sandbox root directory (read-only).
        /// Null means file operations are unrestricted.
        /// </summary>
        public static string? SandboxRoot => _config.Root;

        /// <summary>Maximum file size in bytes for read operations. 0 = unlimited.</summary>
        public static long MaxReadSizeBytes => _config.MaxReadBytes;

        /// <summary>Maximum content length in bytes for write operations. 0 = unlimited.</summary>
        public static long MaxWriteSizeBytes => _config.MaxWriteBytes;

        /// <summary>
        /// Throws <see cref="UnauthorizedAccessException"/> if <paramref name="path"/>
        /// (after normalization) is outside <see cref="SandboxRoot"/>.
        /// No-op when <see cref="SandboxRoot"/> is null, but emits a one-time
        /// diagnostic warning so operators are aware the sandbox is disabled.
        /// Throws <see cref="InvalidOperationException"/> if the session has ended.
        /// </summary>
        internal static void ValidatePath(string path)
        {
            EnsureSessionActive();

            var root = _config.Root; // single read — immutable, no race
            if (string.IsNullOrEmpty(root))
            {
                if (Interlocked.CompareExchange(ref _warningEmitted, 1, 0) == 0)
                {
                    System.Diagnostics.Trace.WriteLine(
                        "[FileSystemCore] SandboxRoot is null — file operations are unrestricted. " +
                        "Call FileSystemCore.Initialize(new SandboxConfig(\"...\")) before loading untrusted workbooks.");
                }
                return;
            }
            NormalizePath(path); // sandbox check (throws UnauthorizedAccessException if outside sandbox)
        }

        /// <summary>Throws <see cref="InvalidOperationException"/> after AutoClose —
        /// shared by ValidatePath and the session-aware info/temp helpers below.</summary>
        private static void EnsureSessionActive()
        {
            if (_sessionEnded)
                throw new InvalidOperationException(
                    "[FileSystemCore] Session ended. Reload the add-in to use FS.* functions.");
        }

        internal static string NormalizePath(string p)
        {
            string normalized = Path.GetFullPath(p);
            // Sandbox check (inline to avoid recursion: ValidatePath calls NormalizePath internally)
            var sandboxRoot = _config.Root; // single read — immutable, no race
            if (!string.IsNullOrEmpty(sandboxRoot))
            {
                string root = Path.GetFullPath(sandboxRoot);
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
        internal static bool IsPathValid(string p) { if(string.IsNullOrEmpty(p))return false; if(p.IndexOfAny(System.IO.Path.GetInvalidPathChars())>=0)return false; try{Path.GetFullPath(p);return true;}catch(Exception ex) when(ExceptionFilters.IsCatchable(ex)){return false;} }
        internal static bool FileExists(string p) { ValidatePath(p); return File.Exists(p); }
        internal static long GetFileSize(string p) { ValidatePath(p); if (!File.Exists(p)) throw new System.IO.FileNotFoundException($"File not found: {p}"); return new FileInfo(p).Length; }
        internal static string ReadTextFile(string p, Encoding? e = null) { ValidatePath(p); var enc = e ?? Encoding.UTF8; using var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read); if (MaxReadSizeBytes > 0 && fs.Length > MaxReadSizeBytes) throw new ArgumentException(ErrorMsg.Get("FS_ReadLimitExceeded", MaxReadSizeBytes)); using var sr = new StreamReader(fs, enc); return sr.ReadToEnd(); }
        internal static string[] ReadAllLines(string p, Encoding? e = null) { ValidatePath(p); var enc = e ?? Encoding.UTF8; using var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read); if (MaxReadSizeBytes > 0 && fs.Length > MaxReadSizeBytes) throw new ArgumentException(ErrorMsg.Get("FS_ReadLimitExceeded", MaxReadSizeBytes)); using var sr = new StreamReader(fs, enc); var lines = new System.Collections.Generic.List<string>(); string? line; while ((line = sr.ReadLine()) != null) lines.Add(line); return lines.ToArray(); }
        internal static bool WriteTextFile(string p, string c, Encoding? e = null) { ValidatePath(p); var enc = e ?? Encoding.UTF8; if (MaxWriteSizeBytes > 0 && enc.GetByteCount(c) > MaxWriteSizeBytes) throw new ArgumentException(ErrorMsg.Get("FS_WriteLimitExceeded", MaxWriteSizeBytes)); File.WriteAllText(p, c, enc); return true; }
        internal static bool AppendTextFile(string p, string c, Encoding? e = null) { ValidatePath(p); var enc = e ?? Encoding.UTF8; if (MaxWriteSizeBytes > 0 && enc.GetByteCount(c) > MaxWriteSizeBytes) throw new ArgumentException(ErrorMsg.Get("FS_WriteLimitExceeded", MaxWriteSizeBytes)); File.AppendAllText(p, c, enc); return true; }
        internal static bool DeleteFile(string p) { ValidatePath(p); if (File.Exists(p)) File.Delete(p); return true; }
        internal static bool CopyFile(string s, string d, bool o = false) { ValidatePath(s); ValidatePath(d); File.Copy(s, d, o); return true; }
        internal static bool MoveFile(string s, string d) { ValidatePath(s); ValidatePath(d); File.Move(s, d); return true; }
        internal static bool FolderExists(string p) { ValidatePath(p); return Directory.Exists(p); }
        internal static bool EnsureFolder(string p) { ValidatePath(p); if (!Directory.Exists(p)) Directory.CreateDirectory(p); return true; }
        internal static string[] ListFiles(string p, string pat = "*") { ValidatePath(p); GuardSearchPattern(pat); return Directory.GetFiles(p, pat); }
        internal static string[] ListFolders(string p, string pat = "*") { ValidatePath(p); GuardSearchPattern(pat); return Directory.GetDirectories(p, pat); }

        /// <summary>P2 (pre-release review): a pattern like "..\*.txt" can resolve through
        /// the parent segment on unpatched .NET Framework runtimes (FindFirstFile resolves
        /// ".." before validation), escaping the sandbox root. Reject ".." segments explicitly.</summary>
        private static void GuardSearchPattern(string pat)
        {
            if (string.IsNullOrEmpty(pat)) return;
            var segments = pat.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
                if (segments[i] == "..")
                    throw new ArgumentException("Search pattern must not contain '..' path segments.");
        }
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
            // review-2026-08-29 P2-1：原递归实现在深层目录树下可致未捕获 StackOverflow
            // （StackOverflow 被异常过滤器排除，不可 catch，会直接崩溃 Excel）。
            // 改为显式栈的深度优先遍历：第一遍删除文件/重解析点并收集目录路径（DFS 前序），
            // 第二遍逆序删除目录（子目录必然先于父目录被访问，逆序即自底向上）。
            var dirs = new System.Collections.Generic.List<string>();
            var pending = new System.Collections.Generic.Stack<string>();
            pending.Push(p);
            while (pending.Count > 0)
            {
                string current = pending.Pop();
                dirs.Add(current);
                foreach (var entry in Directory.EnumerateFileSystemEntries(current))
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
                        pending.Push(entry);
                    }
                    else
                    {
                        // File.Delete on a symlink deletes the link itself (does not follow),
                        // so no special ReparsePoint handling is needed here.
                        File.Delete(entry);
                    }
                }
            }
            for (int i = dirs.Count - 1; i >= 0; i--)
                Directory.Delete(dirs[i]);
        }
        /// <summary>Enumerate logical drives. When <see cref="SandboxRoot"/> is set,
        /// returns only the sandbox root drive to limit filesystem reconnaissance.</summary>
        internal static string[] GetDrives()
        {
            EnsureSessionActive();
            var root = _config.Root;
            if (!string.IsNullOrEmpty(root))
                return new[] { Path.GetPathRoot(Path.GetFullPath(root))! };
            return Array.ConvertAll(DriveInfo.GetDrives(), d => d.Name);
        }
        /// <summary>Returns the current working directory. When <see cref="SandboxRoot"/> is set,
        /// returns the sandbox root to avoid leaking the real working directory.</summary>
        internal static string GetCurrentFolder()
        {
            EnsureSessionActive();
            var root = _config.Root;
            return !string.IsNullOrEmpty(root) ? Path.GetFullPath(root) : Directory.GetCurrentDirectory();
        }
        /// <summary>Returns the system temporary folder. When <see cref="SandboxRoot"/> is set,
        /// returns the sandbox root so temp files created outside FS.* stay within the sandbox.</summary>
        internal static string GetTempPath()
        {
            EnsureSessionActive();
            var root = _config.Root;
            return !string.IsNullOrEmpty(root) ? Path.GetFullPath(root) : Path.GetTempPath();
        }
        /// <summary>
        /// Returns a zero-byte temporary file path. When <see cref="SandboxRoot"/> is set,
        /// the file is created inside the sandbox; otherwise the system TEMP directory is used.
        /// </summary>
        internal static string GetTempFileName()
        {
            EnsureSessionActive();
            var root = _config.Root;
            if (!string.IsNullOrEmpty(root))
            {
                EnsureFolder(root!);
                string path = Path.Combine(root!, Path.GetRandomFileName());
                using (File.Create(path)) { }
                return path;
            }
            return Path.GetTempFileName();
        }
    }
}