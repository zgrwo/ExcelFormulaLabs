using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ExcelDna.Integration;
#if NET48
using ExcelDna.IntelliSense;
#endif
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    public class AddIn : IExcelAddIn
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary>
        /// 预加载 SQLite 原生 DLL。
        /// 当 ExcelDnaPack 将托管 SQLite 程序集打包进 .xll 后，托管程序集从内存加载
        /// （Assembly.Location 为空），内置的 interop 搜索机制无法定位原生 DLL。
        /// 在打开任何连接前调用 LoadLibrary，使 GetModuleHandle 命中已加载模块。
        ///
        /// 加载策略：
        /// 1) 文件系统 (x86\x64\ 旁路 DLL) — 非打包 / 开发模式
        /// 2) 嵌入资源提取 — 打包模式，运行时提取到 %LOCALAPPDATA%
        /// </summary>
        private static void PreLoadNativeDependencies()
        {
            try
            {
                string xllDir = Path.GetDirectoryName(ExcelDnaUtil.XllPath)
                    ?? Environment.CurrentDirectory;
                string arch = IntPtr.Size == 8 ? "x64" : "x86";
#if NET48
                string dllName = "SQLite.Interop.dll";
                string resX86 = "sqlite_interop_x86";
                string resX64 = "sqlite_interop_x64";
#else
                string dllName = "e_sqlite3.dll";
                string resX86 = "sqlite_native_x86";
                string resX64 = "sqlite_native_x64";
#endif
                string dllPath = Path.Combine(xllDir, arch, dllName);

                // 1) 文件系统优先（非打包模式）
                if (File.Exists(dllPath))
                {
                    LoadNativeLibrary(dllPath);
                    return;
                }

                // 2) 从嵌入资源提取（打包模式）
                string resName = IntPtr.Size == 8 ? resX64 : resX86;
                using var stream = typeof(AddIn).Assembly.GetManifestResourceStream(resName);
                if (stream != null)
                {
                    string localDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ExcelFormulaLabs", "DataToolkit");
                    Directory.CreateDirectory(localDir);
                    string extractedPath = Path.Combine(localDir, dllName);

                    // 完整性校验：嵌入资源与已提取文件大小 + SHA256 一致才跳过提取。
                    // review-2026-08-29 P2-13：原实现仅比文件大小——同尺寸恶意 DLL 可被替换后
                    // LoadLibrary 加载进 Excel 进程（本地提权腹地）。
                    bool needExtract = true;
                    if (File.Exists(extractedPath))
                    {
                        var fi = new FileInfo(extractedPath);
                        if (fi.Length == stream.Length && Sha256Equals(stream, extractedPath))
                            needExtract = false;
                    }
                    if (needExtract)
                    {
                        // 原子写入：先写临时文件再 rename，避免多进程并发写入导致 DLL 损坏。
                        string tempPath = extractedPath + $".tmp.{System.Diagnostics.Process.GetCurrentProcess().Id}";
                        using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                            stream.CopyTo(fs);
                        try { File.Move(tempPath, extractedPath); }
                        catch (IOException ex) when (ExceptionFilters.IsCatchable(ex))
                        {
                            // 另一 Excel 实例已完成提取；清理临时文件后使用已有 DLL。
                            try { File.Delete(tempPath); }
                            catch (Exception cleanupEx) when (ExceptionFilters.IsCatchable(cleanupEx))
                            { /* best-effort */ }
                        }
                    }
                    LoadNativeLibrary(extractedPath);
                    return;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[AddIn] 原生 DLL 未找到 (非打包模式可忽略): {dllPath}");
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AddIn] PreLoadNativeDependencies 失败: {ex.Message}");
            }
        }

        /// <summary>SHA-256 字节比对：嵌入资源流与已提取 DLL 是否一致。
        /// 失败（IO/权限/哈希不符）一律返回 false → 触发重新提取，绝不用不可信文件。</summary>
        private static bool Sha256Equals(System.IO.Stream expected, string path)
        {
            try
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                expected.Position = 0;
                var a = sha.ComputeHash(expected);
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                var b = sha.ComputeHash(fs);
                if (a.Length != b.Length) return false;
                for (int i = 0; i < a.Length; i++)
                    if (a[i] != b[i]) return false;
                return true;
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            { return false; }
        }

        private static void LoadNativeLibrary(string path)
        {
            IntPtr handle = LoadLibrary(path);
            if (handle == IntPtr.Zero)
                System.Diagnostics.Debug.WriteLine(
                    $"[AddIn] LoadLibrary 失败 (err {Marshal.GetLastWin32Error()}): {path}");
        }

        public void AutoOpen()
        {
            PreLoadNativeDependencies();
            // review-2026-08-29 P1-1：沙箱默认未启用（SandboxConfig(null)，FS.* 不受限）。
            // 产品决策点：如需默认受限，改为在此调用
            //   FileSystemCore.Initialize(new SandboxConfig(Path.Combine(
            //       Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            //       "ExcelFormulaLabs", "sandbox")));
            // 未启用时输出醒目警告（加载即提示，而非首次 FS 调用才提示）。
            if (FileSystemCore.SandboxRoot == null)
            {
                System.Diagnostics.Trace.WriteLine(
                    "[FileSystemCore] ⚠ SandboxRoot is null — FS.* file operations are UNRESTRICTED. " +
                    "Before loading untrusted workbooks, call FileSystemCore.Initialize(new SandboxConfig(\"...\")) in AutoOpen. " +
                    "See README § 文件系统沙箱.");
            }
#if NET48
            ExcelAsyncUtil.QueueAsMacro(() => IntelliSenseServer.Install());
#endif
        }

        public void AutoClose()
        {
            FilterUtils.ClearRegexCache();
#if NET48
            try { System.Data.SQLite.SQLiteConnection.ClearAllPools(); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            { System.Diagnostics.Debug.WriteLine($"[AddIn.AutoClose] ClearAllPools failed: {ex.Message}"); }
            try { IntelliSenseServer.Uninstall(); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            { /* best-effort: server may already be unloaded */ }
#endif
            FileSystemCore.EndSession();
        }
    }
}
