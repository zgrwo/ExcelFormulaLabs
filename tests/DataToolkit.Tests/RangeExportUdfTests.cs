using FormulaLabs.DataToolkit;
using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;
namespace FormulaLabs.DataToolkit.Tests
{
    public class RangeExportUdfTests
    {
        private static readonly object[,] Data = new object[,] { { "Name", "Age" }, { "Alice", 30 }, { "Bob", 25 } };
        [Fact] public void ToHtml_contains_table() => ((string)RangeExportUdf.UDF_RANGE_HTML(Data,true,null!)).Should().Contain("<table");
        [Fact] public void ToHtml_null_data() => RangeExportUdf.UDF_RANGE_HTML(null!,true,null!).Should().Be(ExcelError.Value);
        [Fact] public void ToJson_contains_name() => ((string)RangeExportUdf.UDF_RANGE_JSON(Data,true,false)).Should().Contain("\"Name\"");
        [Fact] public void ToJson_null_data() => RangeExportUdf.UDF_RANGE_JSON(null!,true,false).Should().Be(ExcelError.Value);
        [Fact] public void ToMd_contains_header() => ((string)RangeExportUdf.UDF_RANGE_MD(Data,true)).Should().Contain("Name");
        [Fact] public void ToMd_null_data() => RangeExportUdf.UDF_RANGE_MD(null!,true).Should().Be(ExcelError.Value);
        [Fact] public void ToCsv_contains_comma() => ((string)RangeExportUdf.UDF_RANGE_CSV(Data,",",true)).Should().Contain(",");
        [Fact] public void ToCsv_null_data() => RangeExportUdf.UDF_RANGE_CSV(null!,",",true).Should().Be(ExcelError.Value);
        [Fact] public void ToTsv_contains_tab() => ((string)RangeExportUdf.UDF_RANGE_TSV(Data)).Should().Contain("\t");
        [Fact] public void ToTsv_null_data() => RangeExportUdf.UDF_RANGE_TSV(null!).Should().Be(ExcelError.Value);
        [Fact] public void ToCsvSemi_contains_semicolon() => ((string)RangeExportUdf.UDF_RANGE_SCSV(Data)).Should().Contain(";");
        [Fact] public void ToCsvSemi_null_data() => RangeExportUdf.UDF_RANGE_SCSV(null!).Should().Be(ExcelError.Value);
        [Fact] public void Transpose_shape() { var r=(object[,])RangeExportUdf.UDF_RANGE_TRANS(Data); r.GetLength(0).Should().Be(2); r.GetLength(1).Should().Be(3); }
        [Fact] public void Transpose_null_data() => RangeExportUdf.UDF_RANGE_TRANS(null!).Should().Be(ExcelError.Value);
        [Fact] public void SelCols_single() { var r=(object[,])RangeExportUdf.UDF_RANGE_SELC(Data,new int[]{0}); r.GetLength(0).Should().Be(3); r.GetLength(1).Should().Be(1); }
        [Fact] public void SelCols_null_data() => RangeExportUdf.UDF_RANGE_SELC(null!,new int[]{0}).Should().Be(ExcelError.Value);
        [Fact] public void SelRows_data_rows() { var r=(object[,])RangeExportUdf.UDF_RANGE_SELR(Data,new int[]{1,2}); r.GetLength(0).Should().Be(2); }
        [Fact] public void SelRows_null_data() => RangeExportUdf.UDF_RANGE_SELR(null!,new int[]{0}).Should().Be(ExcelError.Value);

        // Format verification
        [Fact] public void ToJson_pretty() => ((string)RangeExportUdf.UDF_RANGE_JSON(Data,true,true)).Should().Contain("\n");
        [Fact] public void ToJson_no_header() => ((string)RangeExportUdf.UDF_RANGE_JSON(Data,false,false)).Should().Contain("\"Col1\"");
        [Fact] public void ToJson_NaN_null()
        { var d=new object[,]{{"Val"},{double.NaN}}; ((string)RangeExportUdf.UDF_RANGE_JSON(d,true,false)).Should().Contain("null"); }
        [Fact] public void ToMd_structure()
        { var md=(string)RangeExportUdf.UDF_RANGE_MD(Data,true); md.Should().Contain("---").And.Contain("Alice"); }
        [Fact] public void ToHtml_class()
        { var h=(string)RangeExportUdf.UDF_RANGE_HTML(Data,true,"tbl"); h.Should().Contain("class=\"tbl\""); }
        [Fact] public void ToCsv_quoting()
        { var d=new object[,]{{"Name"},{"Doe, John"}}; ((string)RangeExportUdf.UDF_RANGE_CSV(d,",",true)).Should().Contain("\"Doe, John\""); }
        [Fact] public void Transpose_content()
        { var r=(object[,])RangeExportUdf.UDF_RANGE_TRANS(Data); r[0,0].Should().Be("Name"); r[0,1].Should().Be("Alice"); }
        [Fact] public void SelCols_content()
        { var r=(object[,])RangeExportUdf.UDF_RANGE_SELC(Data,new int[]{0}); r[0,0].Should().Be("Name"); r[1,0].Should().Be("Alice"); }

        // hasHeader=false tests — verify first row treated as data, not header
        [Fact] public void ToHtml_no_header_all_td()
        {
            var d = new object[,] { { "Alice", 30 }, { "Bob", 25 } };
            var html = RangeExportCore.RangeToHtml(d, hasHeader: false);
            html.Should().NotContain("<th>").And.Contain("<td>Alice</td>");
        }

        [Fact] public void ToCsv_no_header_includes_all_rows()
        {
            var d = new object[,] { { "Alice", 30 }, { "Bob", 25 } };
            var csv = RangeExportCore.RangeToCsv(d, ",", true, hasHeader: false);
            csv.Should().Contain("Alice").And.Contain("Bob");
        }
    }
}
