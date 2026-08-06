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

                    // 简单版本检查：文件大小一致则跳过提取
                    bool needExtract = true;
                    if (File.Exists(extractedPath))
                    {
                        var fi = new FileInfo(extractedPath);
                        if (fi.Length == stream.Length)
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
