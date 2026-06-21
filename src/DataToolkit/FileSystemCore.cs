using System;
using System.IO;
using System.Text;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    /// <summary>File I/O and path operations. Ported from FileSystemUtils.bas.</summary>
    internal static class FileSystemCore
    {
        internal static string NormalizePath(string p) => Path.GetFullPath(p);
        internal static string PathCombine(string a, string b) => Path.Combine(a, b);
        internal static string GetFileName(string p) => Path.GetFileName(p);
        internal static string GetBaseName(string p) => Path.GetFileNameWithoutExtension(p);
        internal static string GetExtension(string p) => Path.GetExtension(p);
        internal static string GetFolderPath(string p) => Path.GetDirectoryName(p) ?? "";
        internal static bool IsPathValid(string p) { if(string.IsNullOrEmpty(p))return false; if(p.IndexOfAny(System.IO.Path.GetInvalidPathChars())>=0)return false; try{Path.GetFullPath(p);return true;}catch{return false;} }
        internal static bool FileExists(string p) => File.Exists(p);
        internal static long GetFileSize(string p) => File.Exists(p) ? new FileInfo(p).Length : -1;
        internal static string ReadTextFile(string p, Encoding? e = null) { try { return File.ReadAllText(p, e ?? Encoding.UTF8); } catch { return ""; } }
        internal static string[] ReadAllLines(string p, Encoding? e = null) { try { return File.ReadAllLines(p, e ?? Encoding.UTF8); } catch { return Array.Empty<string>(); } }
        internal static bool WriteTextFile(string p, string c, Encoding? e = null) { try { File.WriteAllText(p, c, e ?? Encoding.UTF8); return true; } catch { return false; } }
        internal static bool AppendTextFile(string p, string c, Encoding? e = null) { try { File.AppendAllText(p, c, e ?? Encoding.UTF8); return true; } catch { return false; } }
        internal static bool DeleteFile(string p) { try { if (File.Exists(p)) File.Delete(p); return true; } catch { return false; } }
        internal static bool CopyFile(string s, string d, bool o = false) { try { File.Copy(s, d, o); return true; } catch { return false; } }
        internal static bool MoveFile(string s, string d) { try { File.Move(s, d); return true; } catch { return false; } }
        internal static bool FolderExists(string p) => Directory.Exists(p);
        internal static bool EnsureFolder(string p) { try { if (!Directory.Exists(p)) Directory.CreateDirectory(p); return true; } catch { return false; } }
        internal static string[] ListFiles(string p, string pat = "*") { try { return Directory.GetFiles(p, pat); } catch { return Array.Empty<string>(); } }
        internal static string[] ListFolders(string p, string pat = "*") { try { return Directory.GetDirectories(p, pat); } catch { return Array.Empty<string>(); } }
        internal static bool DeleteFolder(string p, bool r = false) { try { if (Directory.Exists(p)) Directory.Delete(p, r); return true; } catch { return false; } }
        internal static string[] GetDrives() { try { return Array.ConvertAll(DriveInfo.GetDrives(), d => d.Name); } catch { return Array.Empty<string>(); } }
        internal static string GetCurrentFolder() => Directory.GetCurrentDirectory();
        internal static string GetTempPath() => Path.GetTempPath();
        internal static string GetTempFileName() => Path.GetTempFileName();
    }
}
