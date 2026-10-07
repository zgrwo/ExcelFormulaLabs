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

        /// <summary>
        /// 沙箱出厂默认关闭（<c>SandboxConfig(null)</c>，FS.* 无路径限制）。此处**不启用**它
        /// ——启用会打断既有用户的工作流；只把这一事实送到用户看得见的地方。
        /// </summary>
        /// <remarks>
        /// <b>这是"让状态可见"，不是"启用沙箱"。</b>要真正启用，须在 <see cref="AutoOpen"/> 中按下面
        /// 的示例显式加入调用并重新构建 XLL（<c>SandboxConfig</c> 进程内不可变，无运行时开关，见 ADR-0005）：
        /// <code>
        /// FileSystemCore.Initialize(new SandboxConfig(Path.Combine(
        ///     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ///     "ExcelFormulaLabs", "sandbox")));
        /// </code>
        /// 可见通道见 <see cref="ReportSandboxStatus"/>。
        /// </remarks>
        public void AutoOpen()
        {
            PreLoadNativeDependencies();
            ReportSandboxStatus();
#if NET48
            ExcelAsyncUtil.QueueAsMacro(() => IntelliSenseServer.Install());
#endif
        }

        /// <summary>
        /// 是否需要提示"沙箱未启用"。判定口径与 <see cref="FileSystemCore.ValidatePath"/> 一致：
        /// <c>SandboxRoot</c> 为 <c>null</c> **或空串**都算未设防。
        /// </summary>
        /// <remarks>
        /// 旧实现此处用 <c>SandboxRoot != null</c>，而 <see cref="FileSystemCore.ValidatePath"/> 用
        /// <c>string.IsNullOrEmpty</c>：<c>Initialize(new SandboxConfig(""))</c> 时一个判"已启用"
        /// （早退，两个提示通道都不触发）、一个判"未设防"（不拦截）——用户既看不到提示也不受保护。
        /// 出厂默认（<c>null</c>）行为不变。
        /// </remarks>
        internal static bool ShouldReportSandboxStatus() =>
            string.IsNullOrEmpty(FileSystemCore.SandboxRoot);

        /// <summary>
        /// 沙箱未启用时，把该状态推送到两个**非阻塞**通道：
        /// ① <c>%LOCALAPPDATA%\ExcelFormulaLabs\logs\sandbox-status.log</c>——追加、有真实消费者、
        ///    可事后审计（权威记录）；
        /// ② Excel 状态栏一次性提示——不打断工作流、无需确认。
        /// 刻意不用 <c>MessageBox</c>：每次加载都弹窗会烦扰用户，且阻塞 Excel 启动宏。
        /// </summary>
        private static void ReportSandboxStatus()
        {
            if (!ShouldReportSandboxStatus()) return; // 已启用 → 无需提示

            string notice = SandboxStatus.BuildDisabledNotice();
            System.Diagnostics.Trace.WriteLine(notice); // 保留调试器/ETW 通道（不再作为唯一通道）
            bool logged = SandboxStatus.TryAppend(notice);

            // 状态栏必须等 Excel 就绪后再设，否则 AutoOpen 期间调用 COM 会失败。
            string statusBarText = SandboxStatus.BuildStatusBarText();
            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                try
                {
                    dynamic? app = ExcelDnaUtil.Application;
                    if (app != null) app.StatusBar = statusBarText;
                }
                catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
                {
                    // 尽力而为：非 Excel 宿主/Excel 正在关闭时静默降级，日志文件仍是权威记录。
                    System.Diagnostics.Debug.WriteLine(
                        $"[AddIn] 状态栏提示失败（不影响加载项）: {ex.Message} (logged={logged})");
                }
            });
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
