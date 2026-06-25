using System;
using System.IO;
using System.Text;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
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
        /// <summary>
        /// Optional sandbox root directory. When set, all file I/O is restricted to
        /// paths within this directory. Set to null to disable (default).
        /// Set this before loading workbooks that call FS.* UDFs.
        /// </summary>
        public static string? SandboxRoot { get; set; }

        /// <summary>
        /// Throws <see cref="UnauthorizedAccessException"/> if <paramref name="path"/>
        /// (after normalization) is outside <see cref="SandboxRoot"/>.
        /// No-op when <see cref="SandboxRoot"/> is null.
        /// </summary>
        internal static void ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(SandboxRoot)) return;
            string normalized = NormalizePath(path);
            string root = NormalizePath(SandboxRoot);
            if (root.Length == 0 || root[root.Length - 1] != Path.DirectorySeparatorChar)
                root += Path.DirectorySeparatorChar;
            if (!(normalized + Path.DirectorySeparatorChar).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException(
                    $"Path '{path}' is outside the sandbox root '{SandboxRoot}'.");
        }

        internal static string NormalizePath(string p) => Path.GetFullPath(p);
        internal static string PathCombine(string a, string b) => Path.Combine(a, b);
        internal static string GetFileName(string p) => Path.GetFileName(p);
        internal static string GetBaseName(string p) => Path.GetFileNameWithoutExtension(p);
        internal static string GetExtension(string p) => Path.GetExtension(p);
        internal static string GetFolderPath(string p) => Path.GetDirectoryName(p) ?? "";
        internal static bool IsPathValid(string p) { if(string.IsNullOrEmpty(p))return false; if(p.IndexOfAny(System.IO.Path.GetInvalidPathChars())>=0)return false; try{Path.GetFullPath(p);return true;}catch(Exception ex) when(ex is not OutOfMemoryException and not StackOverflowException){return false;} }
        internal static bool FileExists(string p) { ValidatePath(p); return File.Exists(p); }
        internal static long GetFileSize(string p) { ValidatePath(p); return File.Exists(p) ? new FileInfo(p).Length : -1; }
        internal static string ReadTextFile(string p, Encoding? e = null) { ValidatePath(p); return File.ReadAllText(p, e ?? Encoding.UTF8); }
        internal static string[] ReadAllLines(string p, Encoding? e = null) { ValidatePath(p); return File.ReadAllLines(p, e ?? Encoding.UTF8); }
        internal static bool WriteTextFile(string p, string c, Encoding? e = null) { ValidatePath(p); File.WriteAllText(p, c, e ?? Encoding.UTF8); return true; }
        internal static bool AppendTextFile(string p, string c, Encoding? e = null) { ValidatePath(p); File.AppendAllText(p, c, e ?? Encoding.UTF8); return true; }
        internal static bool DeleteFile(string p) { ValidatePath(p); if (File.Exists(p)) File.Delete(p); return true; }
        internal static bool CopyFile(string s, string d, bool o = false) { ValidatePath(s); ValidatePath(d); File.Copy(s, d, o); return true; }
        internal static bool MoveFile(string s, string d) { ValidatePath(s); ValidatePath(d); File.Move(s, d); return true; }
        internal static bool FolderExists(string p) { ValidatePath(p); return Directory.Exists(p); }
        internal static bool EnsureFolder(string p) { ValidatePath(p); if (!Directory.Exists(p)) Directory.CreateDirectory(p); return true; }
        internal static string[] ListFiles(string p, string pat = "*") { ValidatePath(p); return Directory.GetFiles(p, pat); }
        internal static string[] ListFolders(string p, string pat = "*") { ValidatePath(p); return Directory.GetDirectories(p, pat); }
        internal static bool DeleteFolder(string p, bool r = false) { ValidatePath(p); if (Directory.Exists(p)) Directory.Delete(p, r); return true; }
        internal static string[] GetDrives() { return Array.ConvertAll(DriveInfo.GetDrives(), d => d.Name); }
        internal static string GetCurrentFolder() => Directory.GetCurrentDirectory();
        internal static string GetTempPath() => Path.GetTempPath();
        internal static string GetTempFileName() => Path.GetTempFileName();
    }
}
