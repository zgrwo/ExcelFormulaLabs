using System;
using System.IO;
using System.Text;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    /// <summary>
    /// 打包模式下的原生 DLL 提取 / 完整性维护。
    ///
    /// 同目录覆写提取有两个致命缺陷，会使 SHA-256 完整性加固完全失效：
    ///   ① Sha256Equals 把资源流读到末尾且不复位 → CopyTo 写出 0 字节；
    ///   ② File.Move(src,dst) 在 dst 已存在时抛 IOException（net48/net8 皆然），
    ///      被误判为"另一实例已完成提取" → 同尺寸篡改 DLL 仍被加载、升级换版本
    ///      时旧 DLL 永远无法替换。
    ///
    /// 实现原则：
    ///   - 目标路径由嵌入字节的 SHA-256 派生（内容寻址），并**每次调用都重新比对**
    ///     盘上文件的哈希与嵌入字节，不一致即以原子方式替换。仅靠内容寻址不够——
    ///     路径是确定性的，本地攻击者仍可预写该路径，故必须依赖逐次重验。
    ///   - 替换用 File.Replace（目标存在时原子替换）/ File.Move（目标不存在时移动），
    ///     兼容 net48 与 net8 两 TFM。
    ///   - 每次仅读取并哈希 ~2MB 原生 DLL，在 AutoOpen 里一次执行，开销可忽略。
    /// </summary>
    internal static class NativeDllStore
    {
        /// <summary>同目标路径的进程内互斥：GUID 临时名只解决临时文件互踩，最终
        /// File.Move/File.Replace 的检查-后-行动竞态仍会让并发提取抛"当文件已存在时，无法创建
        /// 该文件"。跨进程竞争由 catch + 重验兜底。</summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> TargetLocks
            = new System.Collections.Concurrent.ConcurrentDictionary<string, object>(StringComparer.Ordinal);

        /// <summary>目标 DLL 路径（内容寻址）。必要时原子写入 / 替换。</summary>
        /// <param name="rootDir">根目录（如 %LOCALAPPDATA%\\ExcelFormulaLabs\\DataToolkit）。</param>
        /// <param name="subDir">子目录（如 "native"）。</param>
        /// <param name="content">嵌入资源的原始字节。</param>
        /// <param name="fileName">DLL 文件名（如 e_sqlite3.dll）。</param>
        public static string GetOrExtract(string rootDir, string subDir, byte[] content, string fileName)
        {
            string hash = Sha256Hex(content);
            string targetDir = Path.Combine(rootDir, subDir, hash);
            string target = Path.Combine(targetDir, fileName);

            lock (TargetLocks.GetOrAdd(target, static _ => new object()))
            {
                if (File.Exists(target) && FileHashEquals(target, content))
                    return target; // 盘上内容与嵌入一致，无需写入

                Directory.CreateDirectory(targetDir);
                // 临时名仅含 PID 时同进程并发提取会互相踩踏；追加进程内唯一 GUID，
                // 互不共享临时路径。
                string temp = Path.Combine(targetDir,
                    fileName + $".tmp.{System.Diagnostics.Process.GetCurrentProcess().Id}.{Guid.NewGuid():N}");
                try
                {
                    File.WriteAllBytes(temp, content);
                    AtomicMove(temp, target);
                    return target;
                }
                catch (IOException ex) when (ExceptionFilters.IsCatchable(ex))
                {
                    // 跨进程实例竞争（先到者已写入）或目标被临时占用导致替换失败。
                    try { File.Delete(temp); }
                    catch (Exception cleanupEx) when (ExceptionFilters.IsCatchable(cleanupEx))
                    { /* best-effort */ }

                    // 无论哪种情况都重新核验盘上文件：一致（发完整正确内容）→ 使用；
                    // 不一致（替换未生效，坏文件残留）→ 抛错，由调用方跳过加载——
                    // 宁可 SQL 原生库加载失败，也不加载不可信的 DLL（fail-safe）。
                    if (FileHashEquals(target, content))
                        return target;
                    throw new IOException(
                        $"Native DLL 完整性校验失败且无法原子替换：{target}（原文件被占用或内容不一致）", ex);
                }
            }
        }

        /// <summary>覆盖式原子移动：目标存在用 File.Replace，缺失用 File.Move。
        /// 两者均为 net48 与 net8 可用。</summary>
        private static void AtomicMove(string temp, string target)
        {
            if (File.Exists(target))
                File.Replace(temp, target, null);   // 原子替换（目标必须存在）
            else
                File.Move(temp, target);            // 目标不存在即移动
        }

        /// <summary>True when the file on disk hashes to the expected embedded bytes.
        /// AddIn 的「文件优先」分支复用本方法做逐次重验。</summary>
        internal static bool FileHashEquals(string path, byte[] expected)
        {
            try
            {
                byte[] onDisk = File.ReadAllBytes(path);
                return Sha256Hex(onDisk) == Sha256Hex(expected);
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                return false; // 读失败（IO/权限）视作不一致 → 触发重写，绝不用不可信文件
            }
        }

        private static string Sha256Hex(byte[] data)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] hash = sha.ComputeHash(data);
            var sb = new StringBuilder(64);
            foreach (byte b in hash)
                sb.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
