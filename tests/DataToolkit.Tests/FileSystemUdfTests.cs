using ExcelVbaLibraries.DataToolkit;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;
namespace ExcelVbaLibraries.DataToolkit.Tests
{
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
        [Fact] public void MkDir_empty() => ((bool)FileSystemUdf.UDF_FS_MKDIR("")).Should().BeFalse();
        [Fact] public void MkDir_null() => FileSystemUdf.UDF_FS_MKDIR(null!).Should().BeNull();
        [Fact] public void Ls_empty() { var r=(object[])FileSystemUdf.UDF_FS_LS("","*"); r.Should().BeEmpty(); }
        [Fact] public void Ls_null_pat() { var r=(object[])FileSystemUdf.UDF_FS_LS("",null!); r.Should().BeEmpty(); }
        [Fact] public void LsDir_empty() { var r=(object[])FileSystemUdf.UDF_FS_LSDIR("","*"); r.Should().BeEmpty(); }
        [Fact] public void Read_empty() => ((string)FileSystemUdf.UDF_FS_READ("")).Should().Be("");
        [Fact] public void Read_null() => FileSystemUdf.UDF_FS_READ(null!).Should().BeNull();
        [Fact] public void Write_empty_path() => ((bool)FileSystemUdf.UDF_FS_WRITE("","content")).Should().BeFalse();
        [Fact] public void Write_null_path() => FileSystemUdf.UDF_FS_WRITE(null!,"content").Should().BeAssignableTo<ExcelEmpty>();
        [Fact] public void Append_empty_path() => ((bool)FileSystemUdf.UDF_FS_APPEND("","content")).Should().BeFalse();
        [Fact] public void Copy_empty_src() => ((bool)FileSystemUdf.UDF_FS_COPY("","dest")).Should().BeFalse();
        [Fact] public void Move_empty_src() => ((bool)FileSystemUdf.UDF_FS_MOVE("","dest")).Should().BeFalse();
        [Fact] public void Delete_empty() => ((bool)FileSystemUdf.UDF_FS_DEL("")).Should().BeTrue();
        [Fact] public void Delete_null() => FileSystemUdf.UDF_FS_DEL(null!).Should().BeNull();
        [Fact] public void DelDir_empty() => ((bool)FileSystemUdf.UDF_FS_DELDIR("")).Should().BeTrue();
        [Fact] public void DelDir_null() => FileSystemUdf.UDF_FS_DELDIR(null!).Should().BeNull();
        [Fact] public void Drives_not_empty() { var r=(object[])FileSystemUdf.UDF_FS_DRVS(); r.Should().NotBeEmpty(); }
        [Fact] public void Pwd_not_empty() => ((string)FileSystemUdf.UDF_FS_PWD()).Should().NotBeNullOrEmpty();
        [Fact] public void Temp_not_empty() => ((string)FileSystemUdf.UDF_FS_TEMP()).Should().NotBeNullOrEmpty();
    }
}
