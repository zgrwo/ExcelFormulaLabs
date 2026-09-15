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
                string resName = IntPtr.Size == 8 ? resX64 : resX86;

                // 先读取嵌入资源字节（打包模式）；无嵌入资源 = 非打包/开发模式。
                byte[]? embedded = null;
                using (var probe = typeof(AddIn).Assembly.GetManifestResourceStream(resName))
                {
                    if (probe != null)
                    {
                        using var ms = new MemoryStream();
                        probe.CopyTo(ms);
                        embedded = ms.ToArray();
                    }
                }

                // 1) 文件系统优先（非打包模式）
                // 有嵌入基线时逐次重验防篡改：打包发行时 x86\x64\ 旁路的篡改 DLL 会被直接
                // 加载，绕过嵌入资源 SHA-256；不一致则落回内容寻址提取（fail-safe）。
                if (File.Exists(dllPath)
                    && (embedded == null || NativeDllStore.FileHashEquals(dllPath, embedded)))
                {
                    LoadNativeLibrary(dllPath);
                    return;
                }

                // 2) 从嵌入资源提取（打包模式）
                if (embedded != null)
                {
                    // 内容寻址提取（NativeDllStore）：目标路径由嵌入字节的 SHA-256 派生，
                    // 且每次调用重新比对盘上文件哈希与嵌入字节，不一致即原子替换。
                    // 仅靠内容寻址不足——路径是确定性的，本地攻击者可预写；覆写方案若 stream
                    // 不复位（写 0 字节）或 File.Move 无法覆写，完整性检查即成空转。
                    string localDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ExcelFormulaLabs", "DataToolkit");
                    string extractedPath = NativeDllStore.GetOrExtract(
                        localDir, "native", embedded, dllName);

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
            // 沙箱默认未启用（SandboxConfig(null)，FS.* 不受限）。
            // 产品决策点：如需默认受限，改为在此调用
            //   FileSystemCore.Initialize(new SandboxConfig(Path.Combine(
            //       Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            //       "ExcelFormulaLabs", "sandbox")));
            // 未启用时输出警告。注意：Trace.WriteLine 默认无 TraceListener，
            // 仅调试器/ETW 可见，对 Excel 终端用户不可见——用户警示由 README § 文件系统沙箱 与 SECURITY.md 承担。
            if (FileSystemCore.SandboxRoot == null)
            {
                System.Diagnostics.Trace.WriteLine(
                    "[FileSystemCore] ⚠ SandboxRoot is null — FS.* file operations are UNRESTRICTED (debug-only; " +
                    "user guidance: README § 文件系统沙箱).");
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
