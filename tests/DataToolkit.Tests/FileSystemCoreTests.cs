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
        [Fact] public void CurrentFolder() => FileSystemCore.GetCurrentFolder().Should().NotBeEmpty();
        [Fact] public void TempPath() => FileSystemCore.GetTempPath().Should().NotBeEmpty();
        [Fact] public void TempFile() => FileSystemCore.GetTempFileName().Should().NotBeEmpty();

        // FileExists tests
        // P2 (review): replaced hardcoded notepad.exe (missing on some Windows images) with
        // a self-contained temp file so the test is deterministic on any machine.
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
            // P2 (review): self-contained temp file with known content (deterministic size).
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
        [Fact] public void NormalizePath_noExcept() => FileSystemCore.NormalizePath(@"C:\Windows\").Should().NotBeNullOrEmpty();

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
        [Fact] public void GetDrives_returnsArray() => FileSystemCore.GetDrives().Should().NotBeEmpty();

    
        // P2 (pre-release review): search patterns containing .. segments can traverse
        // outside the sandbox root on unpatched .NET Framework runtimes (FindFirstFile
        // resolves .. before Directory.GetFiles validates); reject them explicitly.
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
            // P2 (review): System32/notepad was machine-dependent — use a temp dir.
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
            // P2 (review): C:\Windows scan was machine-dependent — use a temp dir.
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
        [Fact] public void AppendTextFile_appends()
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

        // P2-22 (review-2026-08-31): 10 处 sandbox 测试中 9 处有 finally 复位，唯独此条漏了
        // （共享静态状态泄漏到后续测试）。
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
                // P2-23 (review-2026-08-31): mklink /J 依赖权限，受限 CI 下可能失败——
                // 此时 junction 未创建，后续断言无意义。环境敏感测试降级为跳过而非 FAIL。
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
}