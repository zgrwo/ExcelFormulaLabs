using ExcelFormulaLabs.DataToolkit;
using FluentAssertions;
using System;
using System.IO;
using Xunit;

namespace ExcelFormulaLabs.DataToolkit.Tests
{
    // review-2026-08-29 B1 回归守卫：
    //   v2.2.1 的 NativeDllStore 前身（AddIn.Sha256Equals + 同目录覆写）有两大缺陷——
    //   stream 不复位写 0 字节 + File.Move 无法覆写。本套测试锁定"每次重验 + 原子替换"
    //   的正确语义：篡改盘上文件后再次提取必须变回真实内容，版本升级必须落到新路径。
    // [Collection("Sandbox")] 不需要——本类不触碰共享的 SandboxConfig 静态字段。
    public class NativeDllStoreTests : IDisposable
    {
        private readonly string _root;

        public NativeDllStoreTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "efl_native_" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
            catch (Exception) { /* best-effort cleanup */ }
        }

        private static byte[] Gen(int seed, int length = 4096)
        {
            var b = new byte[length];
            for (int i = 0; i < b.Length; i++)
                b[i] = (byte)(seed + i);
            return b;
        }

        [Fact]
        public void First_extract_writes_exact_bytes()
        {
            byte[] content = Gen(1);
            string p = NativeDllStore.GetOrExtract(_root, "native", content, "interop.dll");

            File.Exists(p).Should().BeTrue();
            File.ReadAllBytes(p).Should().Equal(content);
            // 内容寻址：路径含 sha256 子目录
            Path.GetFileName(Path.GetDirectoryName(p)!).Should().HaveLength(64);
        }

        [Fact]
        public void Idempotent_returns_same_path_no_error()
        {
            byte[] content = Gen(2);
            string a = NativeDllStore.GetOrExtract(_root, "native", content, "interop.dll");
            string b = NativeDllStore.GetOrExtract(_root, "native", content, "interop.dll");

            a.Should().Be(b);
            File.ReadAllBytes(a).Should().Equal(content);
        }

        [Fact]
        public void Tampered_file_is_replaced_on_next_extract()
        {
            // B1 核心回归守卫：篡改盘上已提取文件后，再次提取必须还原为真实内容。
            // v2.2.1 在这里失效（File.Move 无法覆写 → 加载被篡改 DLL）。
            byte[] content = Gen(3);
            string p = NativeDllStore.GetOrExtract(_root, "native", content, "interop.dll");

            File.WriteAllBytes(p, Gen(99)); // 篡改

            string q = NativeDllStore.GetOrExtract(_root, "native", content, "interop.dll");
            q.Should().Be(p); // 同一内容寻址路径
            File.ReadAllBytes(q).Should().Equal(content); // 已还原
        }

        [Fact]
        public void Tampered_larger_file_is_replaced()
        {
            // 同尺寸篡改之外：不同尺寸（更大）也被还原。
            byte[] content = Gen(4);
            string p = NativeDllStore.GetOrExtract(_root, "native", content, "interop.dll");

            File.WriteAllBytes(p, new byte[8192]);

            NativeDllStore.GetOrExtract(_root, "native", content, "interop.dll");
            File.ReadAllBytes(p).Should().Equal(content);
        }

        [Fact]
        public void Version_upgrade_lands_on_new_hash_path()
        {
            // 内容变化（升级换版本）→ 不同 hash → 新路径，且两者并存、各自正确。
            byte[] olde = Gen(5);
            byte[] newe = Gen(6);
            string a = NativeDllStore.GetOrExtract(_root, "native", olde, "interop.dll");
            string b = NativeDllStore.GetOrExtract(_root, "native", newe, "interop.dll");

            a.Should().NotBe(b);
            File.Exists(a).Should().BeTrue();   // 旧版本保留（不覆盖）
            File.Exists(b).Should().BeTrue();
            File.ReadAllBytes(a).Should().Equal(olde);
            File.ReadAllBytes(b).Should().Equal(newe);
        }
    }
}
