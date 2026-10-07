using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    /// <summary>
    /// 把"沙箱未启用"这一事实写入用户可发现的通道。
    /// </summary>
    /// <remarks>
    /// <b>动机是诚信，不是安全加固。</b><c>SandboxRoot</c> 出厂为 <c>null</c>，此时
    /// <see cref="FileSystemCore.ValidatePath"/> 的越界拦截完全空转；而该状态的既有提示只走
    /// <c>Trace.WriteLine</c>——<c>Trace</c> 默认没有 <c>TraceListener</c>，Excel 终端用户
    /// 永远看不到。于是"项目文档承诺了一套路径校验、出厂即空转"就成了用户无从知晓的事实。
    /// 本类把同一事实写进一个真实存在消费者（人 / 日志采集器）的通道：<c>%LOCALAPPDATA%</c>
    /// 下的追加日志文件。
    /// <para>
    /// <b>不改变默认行为：</b>本类只记录状态，绝不调用
    /// <see cref="FileSystemCore.Initialize"/>，沙箱仍然默认关闭。
    /// </para>
    /// <para>
    /// 写失败一律静默（不抛）——日志是尽力而为的旁路，任何失败都不得影响加载项启动。
    /// </para>
    /// </remarks>
    internal static class SandboxStatus
    {
        private const string Tag = "[FileSystemCore]";

        /// <summary>单文件上限；超过即轮转为 <c>.1</c>，避免长期使用下无界增长。</summary>
        private const long MaxLogBytes = 1_000_000;

        private static readonly object Gate = new object();
        private static int _logUnavailable;

        /// <summary>日志文件的绝对路径；无法解析（无 LOCALAPPDATA）时为 <c>null</c>。</summary>
        internal static string? LogPath { get; } = ResolveLogPath();

        /// <summary>日志写入是否已失败（供状态栏提示如实标注"日志未写入"）。</summary>
        internal static bool LogUnavailable => Volatile.Read(ref _logUnavailable) != 0;

        /// <summary>
        /// 沙箱未启用时的完整状态说明——日志与状态栏共用同一事实，避免两处措辞漂移。
        /// </summary>
        internal static string BuildDisabledNotice()
            => Tag + " WARNING: sandbox is DISABLED (SandboxRoot = null) — FS.* file operations are " +
               "unrestricted (no path confinement; path-traversal / junction blocking is inactive). " +
               "To enable: call FileSystemCore.Initialize(new SandboxConfig(@\"<sandbox-root>\")) in " +
               "src/DataToolkit/AddIn.cs AutoOpen(), then rebuild the XLL — SandboxConfig is immutable " +
               "per process (ADR-0005), so it cannot be switched at runtime. " +
               "Docs: SECURITY.md § File System Sandbox (default OFF), README § 文件系统沙箱.";

        /// <summary>状态栏文案——短、非阻塞、指明权威记录位置。</summary>
        internal static string BuildStatusBarText()
        {
            string where = LogPath != null && !LogUnavailable
                ? "详见 " + LogPath
                : "日志写入失败，详见 SECURITY.md";
            return "ExcelFormulaLabs: FS.* 文件系统沙箱未启用（出厂默认），路径不受限制 — " + where;
        }

        /// <summary>重置"日志不可用"标记，使测试之间不互相污染。FOR UNIT TESTS ONLY.</summary>
        internal static void ResetUnavailableForTesting() => Volatile.Write(ref _logUnavailable, 0);

        /// <summary>
        /// 以追加方式写入一条带时间戳的状态记录。返回 <c>false</c> 表示"未写入"，
        /// 但**不抛异常**（调用方可据此在状态栏如实降级说明）。
        /// </summary>
        /// <param name="message">写入内容（调用方已带 <see cref="Tag"/> 前缀）。</param>
        /// <param name="logPath">
        /// 落盘路径注入点：<c>null</c>（生产唯一用法）走 <see cref="LogPath"/> 的
        /// %LOCALAPPDATA% 派生路径；单元测试传入临时目录，避免把测试噪声写进真实日志。
        /// <b>注入只改变落盘位置，不改变任何失败语义</b>——写失败仍静默返回 <c>false</c>。
        /// </param>
        internal static bool TryAppend(string message, string? logPath = null)
        {
            string? path = logPath ?? LogPath;
            if (path == null)
            {
                Volatile.Write(ref _logUnavailable, 1);
                return false;
            }
            try
            {
                lock (Gate)
                {
                    string? dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    RotateIfTooLarge(path);
                    File.AppendAllText(
                        path,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                            + " " + message + Environment.NewLine,
                        new UTF8Encoding(false));
                }
                return true;
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                // 尽力而为：日志故障不得冒泡到 Excel 加载路径。
                Volatile.Write(ref _logUnavailable, 1);
                System.Diagnostics.Debug.WriteLine($"{Tag} sandbox-status log write failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>超过上限时轮转为 <c>.1</c>（保留一代，写失败由 <see cref="TryAppend"/> 兜住）。</summary>
        private static void RotateIfTooLarge(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length <= MaxLogBytes) return;
            string archived = path + ".1";
            if (File.Exists(archived)) File.Delete(archived);
            File.Move(path, archived);
        }

        private static string? ResolveLogPath()
        {
            try
            {
                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(baseDir)) return null;
                return Path.Combine(baseDir, "ExcelFormulaLabs", "logs", "sandbox-status.log");
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                return null;
            }
        }
    }
}
