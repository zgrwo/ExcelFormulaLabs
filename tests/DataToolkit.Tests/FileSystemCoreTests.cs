using ExcelVbaLibraries.DataToolkit;
using FluentAssertions;
using System;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
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
        [Fact] public void FileExists_true() => FileSystemCore.FileExists(@"C:\Windows\System32\notepad.exe").Should().BeTrue();
        [Fact] public void FileExists_false() => FileSystemCore.FileExists(@"C:\nonexistent\file.txt").Should().BeFalse();
        [Fact] public void FileExists_empty() => FileSystemCore.FileExists("").Should().BeFalse();

        // GetFileSize tests
        [Fact] public void GetFileSize_knownFile() => FileSystemCore.GetFileSize(@"C:\Windows\System32\notepad.exe").Should().BeGreaterThan(0);
        [Fact] public void GetFileSize_nonexistent() => FileSystemCore.GetFileSize(@"C:\nonexistent\file.txt").Should().Be(-1);

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

        // ListFiles test
        [Fact] public void ListFiles_onSystem32()
        {
            var files = FileSystemCore.ListFiles(@"C:\Windows\System32", "notepad*");
            files.Should().Contain(f => f.EndsWith("notepad.exe", StringComparison.OrdinalIgnoreCase));
        }

        // ListFolders test
        [Fact] public void ListFolders_onWindows()
        {
            var folders = FileSystemCore.ListFolders(@"C:\Windows", "System*");
            folders.Should().Contain(f => f.EndsWith("System32", StringComparison.OrdinalIgnoreCase));
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
            FileSystemCore.SandboxRoot = tmp;
            try { var a = () => FileSystemCore.ReadTextFile(@"..\..\outside.txt"); a.Should().Throw<UnauthorizedAccessException>(); }
            finally { FileSystemCore.SandboxRoot = null; }
        }
        [Fact] public void Sandbox_blocks_sibling_directory()
        {
            var tmp = FileSystemCore.GetTempPath();
            var root = System.IO.Path.Combine(tmp, "Sandbox");
            var evil = root + "Evil";  // C:\...\SandboxEvil should NOT match C:\...\Sandbox\
            FileSystemCore.SandboxRoot = root;
            try
            {
                // ValidatePath directly — avoids File.ReadAllText side-effects on net48
                var act = () => FileSystemCore.ValidatePath(evil);
                act.Should().Throw<UnauthorizedAccessException>();
            }
            finally { FileSystemCore.SandboxRoot = null; }
        }

        // =====================================================================
        // SANDBOX EDGE CASES
        // (systematic coverage following C1 fix pattern)
        // =====================================================================

        [Fact] public void Sandbox_null_root_allows_access()
        {
            // SandboxRoot=null → sandbox disabled → all paths allowed
            FileSystemCore.SandboxRoot = null;
            var act = () => FileSystemCore.ValidatePath(@"C:\any\path");
            act.Should().NotThrow();
        }

        [Fact] public void Sandbox_path_exactly_equals_root()
        {
            var tmp = FileSystemCore.GetTempPath();
            FileSystemCore.SandboxRoot = tmp;
            try
            {
                // Path exactly matching the sandbox root should be allowed
                var act = () => FileSystemCore.ValidatePath(tmp);
                act.Should().NotThrow();
            }
            finally { FileSystemCore.SandboxRoot = null; }
        }

        [Fact] public void Sandbox_empty_string_root()
        {
            // Empty SandboxRoot should allow all access (no constraint)
            try
            {
                FileSystemCore.SandboxRoot = "";
                var act = () => FileSystemCore.ValidatePath(@"C:\temp");
                act.Should().NotThrow();
            }
            finally { FileSystemCore.SandboxRoot = null; }
        }

        [Fact] public void ValidatePath_normalized_same()
        {
            var tmp = FileSystemCore.GetTempPath();
            FileSystemCore.SandboxRoot = tmp;
            try
            {
                // Path with "." normalizes to parent itself → should match root
                var act = () => FileSystemCore.ValidatePath(tmp + System.IO.Path.DirectorySeparatorChar + ".");
                act.Should().NotThrow();
            }
            finally { FileSystemCore.SandboxRoot = null; }
        }
    }
}
