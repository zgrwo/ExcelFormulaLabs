using ExcelFormulaLabs.DataToolkit;
using FluentAssertions;
using System;
using System.IO;
using Xunit;

namespace ExcelFormulaLabs.DataToolkit.Tests
{
    // SandboxConfig is an immutable static field shared across all FileSystem tests.
    // [Collection("Sandbox")] serializes FileSystemCoreTests + FileSystemUdfTests
    // so no parallel test sees a concurrently mutated config.
    // Use FileSystemCore.ResetForTesting() + Initialize() to change sandbox per test.
    [CollectionDefinition("Sandbox", DisableParallelization = true)]
    public class SandboxCollection { }

    [Collection("Sandbox")]
    [Trait("Category", "Security")]
    public class FileSystemCoreTests
    {
        // Original tests
        [Fact] public void PathCombine() => FileSystemCore.PathCombine("C:\\a","b.txt").Should().Be("C:\\a\\b.txt");
        [Fact] public void GetFileName() => FileSystemCore.GetFileName("C:\\a\\b.txt").Should().Be("b.txt");
        [Fact] public void GetBaseName() => FileSystemCore.GetBaseName("report.xlsx").Should().Be("report");
        [Fact] public void GetExtension() => FileSystemCore.GetExtension("file.txt").Should().Be(".txt");
        [Fact] public void GetFolderPath() => FileSystemCore.GetFolderPath("C:\\a\\b.txt").Should().Be("C:\\a");
        [Fact] public void IsPathValid_true() => FileSystemCore.IsPathValid("C:\\").Should().BeTrue();
        [Fact] public void IsPathValid_empty() => FileSystemCore.IsPathValid("").Should().BeFalse();
        [Fact] public void CurrentFolder()
        {
            var p = FileSystemCore.GetCurrentFolder();
            System.IO.Path.IsPathRooted(p).Should().BeTrue();
            System.IO.Directory.Exists(p).Should().BeTrue();
        }
        [Fact] public void TempPath()
        {
            var p = FileSystemCore.GetTempPath();
            System.IO.Path.IsPathRooted(p).Should().BeTrue();
            System.IO.Directory.Exists(p).Should().BeTrue();
        }
        [Fact] public void TempFile()
        {
            var p = FileSystemCore.GetTempFileName();
            System.IO.File.Exists(p).Should().BeTrue();
            System.IO.Path.IsPathRooted(p).Should().BeTrue();
        }

        // FileExists tests
        // hardcoded notepad.exe is missing on some Windows images — use a self-contained
        // temp file so the test is deterministic on any machine.
        [Fact] public void FileExists_true()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "efl_" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(tmp, "x");
            try { FileSystemCore.FileExists(tmp).Should().BeTrue(); }
            finally { File.Delete(tmp); }
        }
        [Fact] public void FileExists_false() => FileSystemCore.FileExists(@"C:\nonexistent\file.txt").Should().BeFalse();
        [Fact] public void FileExists_empty() => FileSystemCore.FileExists("").Should().BeFalse();

        // GetFileSize tests
        [Fact] public void GetFileSize_knownFile()
        {
            // self-contained temp file with known content (deterministic size).
            var tmp = Path.Combine(Path.GetTempPath(), "efl_" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(tmp, new byte[1234]);
            try { FileSystemCore.GetFileSize(tmp).Should().Be(1234); }
            finally { File.Delete(tmp); }
        }
        [Fact] public void GetFileSize_nonexistent() { var a = () => FileSystemCore.GetFileSize(@"C:\nonexistent\file.txt"); a.Should().Throw<System.IO.FileNotFoundException>(); }

        // FolderExists tests
        [Fact] public void FolderExists_true() => FileSystemCore.FolderExists(@"C:\Windows").Should().BeTrue();
        [Fact] public void FolderExists_false() => FileSystemCore.FolderExists(@"C:\nonexistent\folder").Should().BeFalse();
        [Fact] public void FolderExists_empty() => FileSystemCore.FolderExists("").Should().BeFalse();

        // NormalizePath tests
        [Fact] public void NormalizePath_forwardSlash() => FileSystemCore.NormalizePath(@"C:/Windows/System32").Should().EndWith("System32");
        [Fact] public void NormalizePath_noExcept() => FileSystemCore.NormalizePath(@"C:\Windows\").Should().Be(@"C:\Windows\");

        // EnsureFolder test
        [Fact] public void EnsureFolder_createsAndExists()
        {
            var path = FileSystemCore.PathCombine(FileSystemCore.GetTempPath(), "test_" + Guid.NewGuid().ToString("N"));
            try
            {
                FileSystemCore.EnsureFolder(path).Should().BeTrue();
                FileSystemCore.FolderExists(path).Should().BeTrue();
            }
            finally { if (FileSystemCore.FolderExists(path)) FileSystemCore.DeleteFolder(path); }
        }

        // GetDrives test
        [Fact] public void GetDrives_returnsArray()
        {
            var drives = FileSystemCore.GetDrives();
            drives.Should().NotBeEmpty();
            drives.Should().OnlyContain(d => System.IO.Path.IsPathRooted(d));
        }

    
        // search patterns containing .. segments can traverse outside the sandbox root on
        // unpatched .NET Framework runtimes (FindFirstFile resolves .. before
        // Directory.GetFiles validates); reject them explicitly.
        [Fact] public void ListFiles_dotdot_pattern_throws()
        {
            var act = () => FileSystemCore.ListFiles(Path.GetTempPath(), "..\\*.txt");
            act.Should().Throw<ArgumentException>();
        }

        [Fact] public void ListFolders_dotdot_pattern_throws()
        {
            var act = () => FileSystemCore.ListFolders(Path.GetTempPath(), "..\\*");
            act.Should().Throw<ArgumentException>();
        }
    // ListFiles test
        [Fact] public void ListFiles_in_temp_dir()
        {
            // System32/notepad is machine-dependent — use a temp dir.
            var dir = Path.Combine(Path.GetTempPath(), "efl_ls_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
                File.WriteAllText(Path.Combine(dir, "beta.log"), "b");
                var files = FileSystemCore.ListFiles(dir, "*.txt");
                files.Should().ContainSingle(f => Path.GetFileName(f) == "alpha.txt");
                files.Should().NotContain(f => Path.GetFileName(f) == "beta.log");
            }
            finally { Directory.Delete(dir, true); }
        }

        // ListFolders test
        [Fact] public void ListFolders_in_temp_dir()
        {
            // C:\Windows scan is machine-dependent — use a temp dir.
            var dir = Path.Combine(Path.GetTempPath(), "efl_lsd_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "subA"));
            Directory.CreateDirectory(Path.Combine(dir, "subB"));
            try
            {
                var folders = FileSystemCore.ListFolders(dir, "sub*");
                folders.Should().Contain(f => Path.GetFileName(f) == "subA");
                folders.Should().Contain(f => Path.GetFileName(f) == "subB");
            }
            finally { Directory.Delete(dir, true); }
        }

        // WriteTextFile + ReadTextFile test
        [Fact] public void WriteAndReadTextFile()
        {
            var path = FileSystemCore.GetTempFileName();
            try
            {
                FileSystemCore.WriteTextFile(path, "Hello World").Should().BeTrue();
                FileSystemCore.ReadTextFile(path).Should().Be("Hello World");
            }
            finally { if (FileSystemCore.FileExists(path)) FileSystemCore.DeleteFile(path); }
        }

        // WriteTextFile + ReadAllLines test
        [Fact] public void WriteAndReadAllLines()
        {
            var path = FileSystemCore.GetTempFileName();
            try
            {
                FileSystemCore.WriteTextFile(path, "Line1\r\nLine2").Should().BeTrue();
                var lines = FileSystemCore.ReadAllLines(path);
                lines.Should().HaveCount(2);
                lines[0].Should().Be("Line1");
                lines[1].Should().Be("Line2");
            }
            finally { if (FileSystemCore.FileExists(path)) FileSystemCore.DeleteFile(path); }
        }
        // AppendTextFile test
        [Fact]
        public void AppendTextFile_appends()
        {
            var path = FileSystemCore.GetTempFileName();
            try
            {
                FileSystemCore.WriteTextFile(path, "First").Should().BeTrue();
                FileSystemCore.AppendTextFile(path, "Second").Should().BeTrue();
                FileSystemCore.ReadTextFile(path).Should().Be("FirstSecond");
            }
            finally { if (FileSystemCore.FileExists(path)) FileSystemCore.DeleteFile(path); }
        }

        // 单块限制之外还须累计上限。
        [Fact]
        public void AppendTextFile_cumulative_limit_enforced()
        {
            var tmp = FileSystemCore.GetTempPath();
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(tmp, MaxWriteBytes: 10));
            var path = System.IO.Path.Combine(tmp, "efl_append_" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                // ASCII：无 BOM，文件字节数 == 字符数（Encoding.UTF8 会写 3 字节 BOM）。
                FileSystemCore.WriteTextFile(path, "123456", System.Text.Encoding.ASCII).Should().BeTrue();
                var act = () => FileSystemCore.AppendTextFile(path, "67890", System.Text.Encoding.ASCII);
                act.Should().Throw<ArgumentException>().WithMessage("*maximum file size limit*");
                FileSystemCore.AppendTextFile(path, "7890", System.Text.Encoding.ASCII).Should().BeTrue(); // 6+4 == 10
                FileSystemCore.ReadTextFile(path, System.Text.Encoding.ASCII).Should().Be("1234567890");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch (Exception) { }
                FileSystemCore.ResetForTesting();
            }
        }

        // FS.NORM 与同模块其他 FS.* 一致受 EndSession 守卫。
        [Fact]
        public void NormalizePath_after_EndSession_throws()
        {
            try
            {
                FileSystemCore.EndSession();
                var act = () => FileSystemCore.NormalizePath(@"C:\Windows");
                act.Should().Throw<InvalidOperationException>();
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        // DeleteFile test
        [Fact] public void DeleteFile_removes()
        {
            var path = FileSystemCore.GetTempFileName();
            FileSystemCore.FileExists(path).Should().BeTrue();
            FileSystemCore.DeleteFile(path).Should().BeTrue();
            FileSystemCore.FileExists(path).Should().BeFalse();
        }

        // CopyFile test
        [Fact] public void CopyFile_copies()
        {
            var src = FileSystemCore.GetTempFileName();
            var dst = FileSystemCore.GetTempFileName();
            try
            {
                FileSystemCore.WriteTextFile(src, "CopyTest").Should().BeTrue();
                FileSystemCore.CopyFile(src, dst, true).Should().BeTrue();
                FileSystemCore.FileExists(dst).Should().BeTrue();
                FileSystemCore.ReadTextFile(dst).Should().Be("CopyTest");
            }
            finally { FileSystemCore.DeleteFile(src); FileSystemCore.DeleteFile(dst); }
        }

        // MoveFile test
        [Fact] public void MoveFile_moves()
        {
            var src = FileSystemCore.GetTempFileName();
            var dst = FileSystemCore.PathCombine(FileSystemCore.GetTempPath(), "moved_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                FileSystemCore.WriteTextFile(src, "MoveTest").Should().BeTrue();
                FileSystemCore.MoveFile(src, dst).Should().BeTrue();
                FileSystemCore.FileExists(src).Should().BeFalse();
                FileSystemCore.FileExists(dst).Should().BeTrue();
                FileSystemCore.ReadTextFile(dst).Should().Be("MoveTest");
            }
            finally { FileSystemCore.DeleteFile(src); FileSystemCore.DeleteFile(dst); }
        }

        // DeleteFolder recursive test
        [Fact] public void DeleteFolder_recursive()
        {
            var root = FileSystemCore.PathCombine(FileSystemCore.GetTempPath(), "deltest_" + Guid.NewGuid().ToString("N"));
            var sub = FileSystemCore.PathCombine(root, "sub");
            try
            {
                FileSystemCore.EnsureFolder(sub);
                FileSystemCore.WriteTextFile(FileSystemCore.PathCombine(sub, "f.txt"), "x");
                FileSystemCore.DeleteFolder(root, true).Should().BeTrue();
                FileSystemCore.FolderExists(root).Should().BeFalse();
            }
            finally { if (FileSystemCore.FolderExists(root)) FileSystemCore.DeleteFolder(root, true); }
        }

        // PathCombine edge cases
        [Fact] public void PathCombine_emptySecond() => FileSystemCore.PathCombine(@"C:\a", "").Should().Be(@"C:\a");
        [Fact] public void PathCombine_secondIsRooted() => FileSystemCore.PathCombine(@"C:\a", @"D:\b").Should().Be(@"D:\b");

        // GetBaseName edge: no extension
        [Fact] public void GetBaseName_noExtension() => FileSystemCore.GetBaseName("README").Should().Be("README");

        // GetExtension edge: double extension (.tar.gz)
        [Fact] public void GetExtension_doubleExt() => FileSystemCore.GetExtension("file.tar.gz").Should().Be(".gz");
        [Fact] public void Sandbox_blocks_path_traversal()
        {
            var tmp = FileSystemCore.GetTempPath();
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(tmp));
            try { var a = () => FileSystemCore.ReadTextFile(@"..\..\outside.txt"); a.Should().Throw<UnauthorizedAccessException>(); }
            finally { FileSystemCore.ResetForTesting(); }
        }
        [Fact] public void Sandbox_blocks_sibling_directory()
        {
            var tmp = FileSystemCore.GetTempPath();
            var root = System.IO.Path.Combine(tmp, "Sandbox");
            var evil = root + "Evil";  // C:\...\SandboxEvil should NOT match C:\...\Sandbox\
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(root));
            try
            {
                var act = () => FileSystemCore.ValidatePath(evil);
                act.Should().Throw<UnauthorizedAccessException>();
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        // =====================================================================
        // SANDBOX EDGE CASES
        // =====================================================================

        // 共享静态状态必须 finally 复位，否则泄漏到后续测试（本组 sandbox 测试同此约束）。
        [Fact] public void Sandbox_null_root_allows_access()
        {
            FileSystemCore.ResetForTesting();
            try
            {
                // Default config has Root=null — unrestricted
                var act = () => FileSystemCore.ValidatePath(@"C:\any\path");
                act.Should().NotThrow();
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        [Fact] public void Sandbox_path_exactly_equals_root()
        {
            var tmp = FileSystemCore.GetTempPath();
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(tmp));
            try
            {
                var act = () => FileSystemCore.ValidatePath(tmp);
                act.Should().NotThrow();
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        [Fact] public void Sandbox_empty_string_root()
        {
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(""));
            try
            {
                var act = () => FileSystemCore.ValidatePath(@"C:\temp");
                act.Should().NotThrow();
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        /// <summary>
        /// P3-1：AddIn 的"是否需要提示沙箱未启用"必须与 <see cref="FileSystemCore.ValidatePath"/>
        /// 的设防判定同口径。空串 root 在 ValidatePath 里 = 未设防（不拦截），
        /// 提示通道就不能判成"已启用"而提前返回（否则用户既看不到提示也不受保护）。
        /// </summary>
        [Fact]
        public void Sandbox_empty_string_root_counts_as_disabled_for_visibility()
        {
            FileSystemCore.ResetForTesting();
            try
            {
                AddIn.ShouldReportSandboxStatus().Should().BeTrue();   // 出厂 null = 未启用
                FileSystemCore.Initialize(new SandboxConfig(""));
                AddIn.ShouldReportSandboxStatus().Should().BeTrue();   // 空串同 null（旧实现判 False）
                // 与执行判定一致：空串不设防（既有 Sandbox_empty_string_root 语义，不得破坏）
                var act = () => FileSystemCore.ValidatePath(@"C:\Windows\System32\kernel32.dll");
                act.Should().NotThrow();
            }
            finally { FileSystemCore.ResetForTesting(); }

            FileSystemCore.ResetForTesting();
            try
            {
                FileSystemCore.Initialize(new SandboxConfig(FileSystemCore.GetTempPath()));
                AddIn.ShouldReportSandboxStatus().Should().BeFalse();  // 真设防 → 无需提示
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        [Fact] public void ValidatePath_normalized_same()
        {
            var tmp = FileSystemCore.GetTempPath();
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(tmp));
            try
            {
                var act = () => FileSystemCore.ValidatePath(tmp + System.IO.Path.DirectorySeparatorChar + ".");
                act.Should().NotThrow();
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        [Fact] public void Sandbox_FileExists_outside_root_throws()
        {
            var tmp = FileSystemCore.GetTempPath();
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(tmp));
            try
            {
                var act = () => FileSystemCore.FileExists(@"C:\Windows\System32\kernel32.dll");
                act.Should().Throw<UnauthorizedAccessException>().WithMessage("*outside*sandbox*");
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        [Fact] public void Sandbox_GetFileSize_outside_root_throws()
        {
            var tmp = FileSystemCore.GetTempPath();
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(tmp));
            try
            {
                var act = () => FileSystemCore.GetFileSize(@"C:\Windows\notepad.exe");
                act.Should().Throw<UnauthorizedAccessException>().WithMessage("*outside*sandbox*");
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        [Fact] public void Sandbox_FolderExists_outside_root_throws()
        {
            var tmp = FileSystemCore.GetTempPath();
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(tmp));
            try
            {
                var act = () => FileSystemCore.FolderExists(@"C:\Windows\System32");
                act.Should().Throw<UnauthorizedAccessException>().WithMessage("*outside*sandbox*");
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        [Fact] public void Sandbox_NormalizePath_outside_root_throws()
        {
            var tmp = System.IO.Path.GetTempPath();
            FileSystemCore.ResetForTesting();
            FileSystemCore.Initialize(new SandboxConfig(tmp));
            try
            {
                var act = () => FileSystemCore.NormalizePath(System.IO.Path.Combine(tmp, "..", "outside.txt"));
                act.Should().Throw<UnauthorizedAccessException>().WithMessage("*outside*sandbox*");
            }
            finally { FileSystemCore.ResetForTesting(); }
        }

        // =====================================================================
        // DELETE FOLDER EDGE CASES (regression coverage)
        // =====================================================================

        [Fact] public void DeleteFolder_recursive_empty_directory()
        {
            var root = FileSystemCore.PathCombine(FileSystemCore.GetTempPath(), "deltest_empty_" + Guid.NewGuid().ToString("N"));
            try
            {
                FileSystemCore.EnsureFolder(root);
                FileSystemCore.FolderExists(root).Should().BeTrue();
                FileSystemCore.DeleteFolder(root, true).Should().BeTrue();
                FileSystemCore.FolderExists(root).Should().BeFalse();
            }
            finally { if (FileSystemCore.FolderExists(root)) FileSystemCore.DeleteFolder(root, true); }
        }

        [Fact] public void DeleteFolder_recursive_file_only()
        {
            var root = FileSystemCore.PathCombine(FileSystemCore.GetTempPath(), "deltest_fileonly_" + Guid.NewGuid().ToString("N"));
            try
            {
                FileSystemCore.EnsureFolder(root);
                FileSystemCore.WriteTextFile(FileSystemCore.PathCombine(root, "f.txt"), "x");
                FileSystemCore.DeleteFolder(root, true).Should().BeTrue();
                FileSystemCore.FolderExists(root).Should().BeFalse();
            }
            finally { if (FileSystemCore.FolderExists(root)) FileSystemCore.DeleteFolder(root, true); }
        }

        [Fact] public void DeleteFolder_recursive_deep_nesting()
        {
            var root = FileSystemCore.PathCombine(FileSystemCore.GetTempPath(), "deltest_deep_" + Guid.NewGuid().ToString("N"));
            var l1 = FileSystemCore.PathCombine(root, "L1");
            var l2 = FileSystemCore.PathCombine(l1, "L2");
            var l3 = FileSystemCore.PathCombine(l2, "L3");
            try
            {
                FileSystemCore.EnsureFolder(l3);
                FileSystemCore.WriteTextFile(FileSystemCore.PathCombine(root, "root.txt"), "a");
                FileSystemCore.WriteTextFile(FileSystemCore.PathCombine(l1, "l1.txt"), "b");
                FileSystemCore.WriteTextFile(FileSystemCore.PathCombine(l3, "l3.txt"), "c");
                FileSystemCore.FolderExists(root).Should().BeTrue();
                FileSystemCore.DeleteFolder(root, true).Should().BeTrue();
                FileSystemCore.FolderExists(root).Should().BeFalse();
            }
            finally { if (FileSystemCore.FolderExists(root)) FileSystemCore.DeleteFolder(root, true); }
        }

        /// <summary>Sandbox must reject paths that cross NTFS junction points,
        /// since Path.GetFullPath does not resolve them but System.IO follows them.</summary>
        [Fact] public void Sandbox_rejects_junction_path()
        {
            var tmp = FileSystemCore.GetTempPath();
            var root = System.IO.Path.Combine(tmp, "Sandbox_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            var inner = System.IO.Path.Combine(root, "inner");
            var link  = System.IO.Path.Combine(root, "link");   // junction → inner
            try
            {
                System.IO.Directory.CreateDirectory(inner);
                // Create junction: link → inner (works without admin on Windows for directories)
                var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe",
                    $"/c mklink /J \"{link}\" \"{inner}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var proc = System.Diagnostics.Process.Start(psi)!;
                proc.WaitForExit(5000);
                // mklink /J 依赖权限，受限 CI 下可能失败——junction 未创建时后续断言无意义，
                // 环境敏感测试降级为跳过而非 FAIL。
                if (proc.ExitCode != 0 || !System.IO.Directory.Exists(link))
                {
                    FileSystemCore.ResetForTesting();
                    return;   // junction 创建失败 → 跳过（非 CI 阻塞）
                }
                FileSystemCore.ResetForTesting();
                FileSystemCore.Initialize(new SandboxConfig(root));
                // Traversing the junction should be blocked by the reparse-point check
                var act = () => FileSystemCore.NormalizePath(System.IO.Path.Combine(link, "test.txt"));
                act.Should().Throw<UnauthorizedAccessException>()
                    .WithMessage("*junction*");
            }
            finally
            {
                FileSystemCore.ResetForTesting();
                // Delete junction (it's a reparse point, not followed by our code)
                if (System.IO.Directory.Exists(link))
                { System.IO.File.SetAttributes(link, System.IO.FileAttributes.Directory); System.IO.Directory.Delete(link); }
                if (System.IO.Directory.Exists(root))
                { System.IO.Directory.Delete(root, true); }
            }
        }
    }

    /// <summary>
    /// 沙箱"默认关闭"必须对用户可见（诚信要求，非安全加固）。
    /// 这里只钉住文案里的**事实**：出厂为 null、唯一开启路径、进程内不可切换、指向 SECURITY.md。
    /// 文件追加本身是 I/O 副作用（写入真实 %LOCALAPPDATA%），不在单元测试里制造日志噪声。
    /// </summary>
    public class SandboxStatusTests
    {
        [Fact]
        public void Disabled_notice_states_default_off_and_the_only_enable_path()
        {
            var notice = SandboxStatus.BuildDisabledNotice();
            notice.Should().Contain("SandboxRoot = null");
            notice.Should().Contain("DISABLED");
            notice.Should().Contain("FileSystemCore.Initialize(new SandboxConfig(");
            notice.Should().Contain("immutable per process");   // 无运行时开关
            notice.Should().Contain("SECURITY.md");
            SandboxStatus.BuildStatusBarText().Should().Contain("沙箱未启用");
        }
    }

    /// <summary>
    /// SandboxStatus 的文件 I/O 契约（P2-2）：此前只有两个字符串构建器有测试，
    /// <c>TryAppend</c> / <c>RotateIfTooLarge</c> 零命中——DataToolkit 行覆盖率余量被压到 1.5 点。
    /// 一律走**临时目录 + 路径注入**（<c>TryAppend(msg, logPath)</c>），
    /// 不向 %LOCALAPPDATA% 的真实日志写测试噪声；用后清理（<see cref="Dispose"/>）。
    /// </summary>
    public class SandboxStatusIoTests : IDisposable
    {
        // 与实现常量 MaxLogBytes 一致的边界值（硬编码，不从实现回读）。
        private const int LogLimitBytes = 1_000_000;

        private readonly string _dir = Path.Combine(
            Path.GetTempPath(), "efl_sandboxstatus_" + Guid.NewGuid().ToString("N"));

        private string LogFile => Path.Combine(_dir, "sandbox-status.log");

        public void Dispose()
        {
            SandboxStatus.ResetUnavailableForTesting();
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }
            catch (IOException) { /* 临时目录清理失败不掩盖测试结论 */ }
        }

        [Fact]
        public void TryAppend_creates_directory_and_writes_timestamped_line()
        {
            Directory.Exists(_dir).Should().BeFalse();           // 目录由 TryAppend 自建
            SandboxStatus.TryAppend("[FileSystemCore] first", LogFile).Should().BeTrue();
            File.Exists(LogFile).Should().BeTrue();
            var lines = File.ReadAllLines(LogFile);
            lines.Should().HaveCount(1);
            // 期望：yyyy-MM-dd HH:mm:ss + 空格 + 原文（格式硬编码，非回读实现产物）
            System.Text.RegularExpressions.Regex.IsMatch(
                lines[0], @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} \[FileSystemCore\] first$")
                .Should().BeTrue($"实际行：{lines[0]}");
        }

        [Fact]
        public void TryAppend_appends_second_line_without_truncating()
        {
            SandboxStatus.TryAppend("first", LogFile).Should().BeTrue();
            SandboxStatus.TryAppend("second", LogFile).Should().BeTrue();
            var lines = File.ReadAllLines(LogFile);
            lines.Should().HaveCount(2);
            lines[0].Should().EndWith(" first");
            lines[1].Should().EndWith(" second");
        }

        [Fact]
        public void TryAppend_below_size_limit_does_not_rotate()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllBytes(LogFile, new byte[LogLimitBytes]);   // 恰好等于上限 → 不轮转
            SandboxStatus.TryAppend("kept", LogFile).Should().BeTrue();
            File.Exists(LogFile + ".1").Should().BeFalse();
            new FileInfo(LogFile).Length.Should().BeGreaterThan(LogLimitBytes);   // 追加而非截断
        }

        [Fact]
        public void TryAppend_above_size_limit_rotates_to_dot1()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllBytes(LogFile, new byte[LogLimitBytes + 1]);   // 超过上限 → 轮转
            SandboxStatus.TryAppend("after-rotation", LogFile).Should().BeTrue();
            File.Exists(LogFile + ".1").Should().BeTrue();
            new FileInfo(LogFile + ".1").Length.Should().Be(LogLimitBytes + 1);   // 旧内容整代保留
            var lines = File.ReadAllLines(LogFile);
            lines.Should().HaveCount(1);                                          // 新文件只含本行
            lines[0].Should().EndWith(" after-rotation");
        }

        [Fact]
        public void TryAppend_existing_archive_is_replaced_on_rotation()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllBytes(LogFile, new byte[LogLimitBytes + 1]);
            File.WriteAllText(LogFile + ".1", "stale-archive");
            SandboxStatus.TryAppend("fresh", LogFile).Should().BeTrue();
            new FileInfo(LogFile + ".1").Length.Should().Be(LogLimitBytes + 1);   // 旧 .1 被替换
            File.ReadAllText(LogFile + ".1").Should().NotContain("stale-archive");
        }

        [Fact]
        public void TryAppend_write_failure_is_silent_and_marks_log_unavailable()
        {
            // 契约（不得削弱）：日志写入失败一律静默（不抛），仅返回 false 并降级标注。
            Directory.CreateDirectory(_dir);
            string asDirectory = Path.Combine(_dir, "not-a-file");
            Directory.CreateDirectory(asDirectory);   // 追加到目录路径 → UnauthorizedAccessException
            SandboxStatus.ResetUnavailableForTesting();
            SandboxStatus.LogUnavailable.Should().BeFalse();
            var act = () => SandboxStatus.TryAppend("boom", asDirectory);
            act.Should().NotThrow();
            SandboxStatus.TryAppend("boom", asDirectory).Should().BeFalse();
            SandboxStatus.LogUnavailable.Should().BeTrue();
            Directory.Exists(asDirectory).Should().BeTrue();   // 未产生文件系统副作用
        }
    }
}