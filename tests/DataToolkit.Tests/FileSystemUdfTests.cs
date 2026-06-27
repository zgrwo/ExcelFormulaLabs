using FormulaLabs.DataToolkit;
using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;
namespace FormulaLabs.DataToolkit.Tests
{
    // In the Sandbox collection with FileSystemCoreTests because several UDFs
    // (FS.DELDIR, FS.FSIZE, etc.) call Core methods that check SandboxRoot.
    // Serialization prevents parallel-sandbox-mutation race conditions.
    [Collection("Sandbox")]
    public class FileSystemUdfTests
    {
        [Fact] public void Norm_relative() => ((string)FileSystemUdf.UDF_FS_NORM(".")).Should().NotBeNullOrEmpty();
        [Fact] public void Norm_empty() => FileSystemUdf.UDF_FS_NORM("").Should().Be(ExcelError.Value);
        [Fact] public void Norm_null() => FileSystemUdf.UDF_FS_NORM(null!).Should().BeNull();
        [Fact] public void Norm_absolute() => ((string)FileSystemUdf.UDF_FS_NORM(@"C:\Windows")).Should().Be(@"C:\Windows");
        [Fact] public void Combine_basic() => ((string)FileSystemUdf.UDF_FS_COMB(@"C:\a","b.txt")).Should().Be(@"C:\a\b.txt");
        [Fact] public void Combine_empty_first() => ((string)FileSystemUdf.UDF_FS_COMB("","b.txt")).Should().Be("b.txt");
        [Fact] public void Combine_empty_second() => ((string)FileSystemUdf.UDF_FS_COMB(@"C:\a","")).Should().Be(@"C:\a");
        [Fact] public void Combine_null_first() => FileSystemUdf.UDF_FS_COMB(null!,"b.txt").Should().BeAssignableTo<ExcelEmpty>();
        [Fact] public void Combine_null_second() => FileSystemUdf.UDF_FS_COMB(@"C:\a",null!).Should().BeAssignableTo<ExcelEmpty>();
        [Fact] public void Combine_both_empty() => ((string)FileSystemUdf.UDF_FS_COMB("","")).Should().Be("");
        [Fact] public void FName_with_ext() => ((string)FileSystemUdf.UDF_FS_FNAME(@"C:\a\b.txt")).Should().Be("b.txt");
        [Fact] public void FName_empty() => ((string)FileSystemUdf.UDF_FS_FNAME("")).Should().Be("");
        [Fact] public void FName_null() => FileSystemUdf.UDF_FS_FNAME(null!).Should().BeNull();
        [Fact] public void BName_with_ext() => ((string)FileSystemUdf.UDF_FS_BNAME(@"C:\a\b.txt")).Should().Be("b");
        [Fact] public void BName_double_ext() => ((string)FileSystemUdf.UDF_FS_BNAME("archive.tar.gz")).Should().Be("archive.tar");
        [Fact] public void BName_null() => FileSystemUdf.UDF_FS_BNAME(null!).Should().BeNull();
        [Fact] public void Ext_with_ext() => ((string)FileSystemUdf.UDF_FS_EXT(@"C:\a\b.txt")).Should().Be(".txt");
        [Fact] public void Ext_null() => FileSystemUdf.UDF_FS_EXT(null!).Should().BeNull();
        [Fact] public void Folder_with_file() => ((string)FileSystemUdf.UDF_FS_FDR(@"C:\a\b.txt")).Should().Be(@"C:\a");
        [Fact] public void Folder_null() => FileSystemUdf.UDF_FS_FDR(null!).Should().BeNull();
        [Fact] public void FExists_empty() => ((bool)FileSystemUdf.UDF_FS_FEX("")).Should().BeFalse();
        [Fact] public void FExists_null() => FileSystemUdf.UDF_FS_FEX(null!).Should().BeNull();
        [Fact] public void FSize_empty() => FileSystemUdf.UDF_FS_FSZ("").Should().Be(-1L);
        [Fact] public void FSize_null() => FileSystemUdf.UDF_FS_FSZ(null!).Should().BeNull();
        [Fact] public void FDExists_empty() => ((bool)FileSystemUdf.UDF_FS_FDEX("")).Should().BeFalse();
        [Fact] public void FDExists_null() => FileSystemUdf.UDF_FS_FDEX(null!).Should().BeNull();
        [Fact] public void MkDir_empty() => FileSystemUdf.UDF_FS_MKDIR("").Should().Be(ExcelError.Value);
        [Fact] public void MkDir_null() => FileSystemUdf.UDF_FS_MKDIR(null!).Should().BeNull();
        [Fact] public void Ls_empty() => FileSystemUdf.UDF_FS_LS("","*").Should().Be(ExcelError.Value);
        [Fact] public void Ls_null_pat() { var r = FileSystemUdf.UDF_FS_LS("",null!); if (r is object[] arr) arr.Should().BeEmpty(); else r.Should().Be(ExcelError.Value); }
        [Fact] public void LsDir_empty() => FileSystemUdf.UDF_FS_LSDIR("","*").Should().Be(ExcelError.Value);
        [Fact] public void Read_empty() => FileSystemUdf.UDF_FS_READ("").Should().Be(ExcelError.Value);
        [Fact] public void Read_null() => FileSystemUdf.UDF_FS_READ(null!).Should().BeNull();
        [Fact] public void Write_empty_path() => FileSystemUdf.UDF_FS_WRITE("","content").Should().Be(ExcelError.Value);
        [Fact] public void Write_null_path() => FileSystemUdf.UDF_FS_WRITE(null!,"content").Should().BeAssignableTo<ExcelEmpty>();
        [Fact] public void Append_empty_path() => FileSystemUdf.UDF_FS_APPEND("","content").Should().Be(ExcelError.Value);
        [Fact] public void Copy_empty_src() => FileSystemUdf.UDF_FS_COPY("","dest").Should().Be(ExcelError.Value);
        [Fact] public void Move_empty_src() => FileSystemUdf.UDF_FS_MOVE("","dest").Should().Be(ExcelError.Value);
        [Fact] public void Delete_empty() => ((bool)FileSystemUdf.UDF_FS_DEL("")).Should().BeTrue();
        [Fact] public void Delete_null() => FileSystemUdf.UDF_FS_DEL(null!).Should().BeNull();
        [Fact] public void DelDir_empty() => ((bool)FileSystemUdf.UDF_FS_DELDIR("")).Should().BeTrue();
        [Fact] public void DelDir_null() => FileSystemUdf.UDF_FS_DELDIR(null!).Should().BeNull();
        [Fact] public void Drives_not_empty() { var r=(object[])FileSystemUdf.UDF_FS_DRVS(); r.Should().NotBeEmpty(); }
        [Fact] public void Pwd_not_empty() => ((string)FileSystemUdf.UDF_FS_PWD()).Should().NotBeNullOrEmpty();
        [Fact] public void Temp_not_empty() => ((string)FileSystemUdf.UDF_FS_TEMP()).Should().NotBeNullOrEmpty();

        // ── Real file I/O tests ──────────────────────────────────────
        [Fact] public void Write_and_read_back()
        {
            var tmp = System.IO.Path.GetTempFileName();
            try { ((bool)FileSystemUdf.UDF_FS_WRITE(tmp, "hello")).Should().BeTrue(); ((string)FileSystemUdf.UDF_FS_READ(tmp)).Should().Be("hello"); }
            finally { System.IO.File.Delete(tmp); }
        }
        [Fact] public void Append_then_read()
        {
            var tmp = System.IO.Path.GetTempFileName();
            try { FileSystemUdf.UDF_FS_WRITE(tmp, "a"); ((bool)FileSystemUdf.UDF_FS_APPEND(tmp, "b")).Should().BeTrue(); ((string)FileSystemUdf.UDF_FS_READ(tmp)).Should().Be("ab"); }
            finally { System.IO.File.Delete(tmp); }
        }
        [Fact] public void Copy_and_verify()
        {
            var s = System.IO.Path.GetTempFileName(); var d = System.IO.Path.GetTempFileName();
            try { System.IO.File.Delete(d); FileSystemUdf.UDF_FS_WRITE(s, "x"); ((bool)FileSystemUdf.UDF_FS_COPY(s, d)).Should().BeTrue(); ((string)FileSystemUdf.UDF_FS_READ(d)).Should().Be("x"); }
            finally { System.IO.File.Delete(s); try { System.IO.File.Delete(d); } catch { } }
        }
        [Fact] public void Move_and_verify()
        {
            var s = System.IO.Path.GetTempFileName(); var d = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fsm_{System.Guid.NewGuid():N}.tmp");
            try { FileSystemUdf.UDF_FS_WRITE(s, "m"); ((bool)FileSystemUdf.UDF_FS_MOVE(s, d)).Should().BeTrue(); ((string)FileSystemUdf.UDF_FS_READ(d)).Should().Be("m"); ((bool)FileSystemUdf.UDF_FS_FEX(s)).Should().BeFalse(); }
            finally { try { System.IO.File.Delete(s); } catch { } try { System.IO.File.Delete(d); } catch { } }
        }
        [Fact] public void File_exists_and_size()
        {
            var tmp = System.IO.Path.GetTempFileName();
            try { FileSystemUdf.UDF_FS_WRITE(tmp, "1234567890"); ((bool)FileSystemUdf.UDF_FS_FEX(tmp)).Should().BeTrue(); var sz = (long)FileSystemUdf.UDF_FS_FSZ(tmp); sz.Should().BeGreaterThan(0); }
            finally { System.IO.File.Delete(tmp); }
        }
        [Fact] public void Delete_and_verify_gone()
        {
            var tmp = System.IO.Path.GetTempFileName();
            FileSystemUdf.UDF_FS_WRITE(tmp, "del"); ((bool)FileSystemUdf.UDF_FS_DEL(tmp)).Should().BeTrue(); ((bool)FileSystemUdf.UDF_FS_FEX(tmp)).Should().BeFalse();
        }
        [Fact] public void MkDir_and_list()
        {
            var d = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fsd_{System.Guid.NewGuid():N}");
            try { ((bool)FileSystemUdf.UDF_FS_MKDIR(d)).Should().BeTrue(); ((bool)FileSystemUdf.UDF_FS_FDEX(d)).Should().BeTrue(); ((object[])FileSystemUdf.UDF_FS_LSDIR(d, "*")).Should().BeEmpty(); }
            finally { try { System.IO.Directory.Delete(d); } catch { } }
        }
    }
}
