using System;
using ExcelFormulaLabs.Analytics;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class AnalyticsHelpersTests
    {
        [Fact] public void ToDoubleMatrix_basic()
        { var result = AnalyticsHelpers.ToDoubleMatrix(new object[,]{{1.0,2.0},{3.0,4.0}}); result[0,0].Should().Be(1.0); result[1,1].Should().Be(4.0); }
        [Fact] public void ToDoubleMatrix_with_non_numeric()
        { var act = () => AnalyticsHelpers.ToDoubleMatrix(new object[,]{{1.0,"text"},{3.0,4.0}}); act.Should().Throw<ArgumentException>().WithMessage("*non-numeric*"); }
        [Fact] public void ToDoubleMatrix_empty() { var result = AnalyticsHelpers.ToDoubleMatrix(new object[0,0]); result.GetLength(0).Should().Be(0); }
        [Fact] public void PrepM_scalar() { var result = AnalyticsHelpers.PrepM(42); result[0,0].Should().Be(42.0); }
        [Fact] public void PrepM_2D() { var result = AnalyticsHelpers.PrepM(new object[,]{{1.0,2.0},{3.0,4.0}}); result.GetLength(0).Should().Be(2); }
        [Fact] public void PrepM_null_or_empty()
        { var act = () => AnalyticsHelpers.PrepM(ExcelEmpty.Value); act.Should().Throw<ArgumentException>().WithMessage("*non-numeric*"); }
        [Fact] public void PrepV_scalar() { var result = AnalyticsHelpers.PrepV(42); result.Length.Should().Be(1); result[0].Should().Be(42.0); }
        [Fact] public void PrepV_1D() { var result = AnalyticsHelpers.PrepV(new object[]{1.0,2,3}); result.Length.Should().Be(3); }
        [Fact] public void PrepV_2D_flattens() { var result = AnalyticsHelpers.PrepV(new object[,]{{1.0},{2.0},{3.0}}); result.Length.Should().Be(3); }
        [Fact] public void PrepV_empty() { var a = () => AnalyticsHelpers.PrepV(ExcelEmpty.Value); a.Should().Throw<ArgumentException>(); }
        // NaN/Inf guards (防错原则1: PrepM/PrepV now uniformly throw)
        [Fact] public void ToDoubleMatrix_NaN_throws() { var a = () => AnalyticsHelpers.ToDoubleMatrix(new object[,] { { double.NaN, 1.0 }, { 2.0, 3.0 } }); a.Should().Throw<ArgumentException>().WithMessage("*non-numeric*"); }
        [Fact] public void ToDoubleMatrix_Inf_throws() { var a = () => AnalyticsHelpers.ToDoubleMatrix(new object[,] { { double.PositiveInfinity, 1.0 }, { 2.0, 3.0 } }); a.Should().Throw<ArgumentException>().WithMessage("*non-numeric*"); }
        [Fact] public void PrepM_null_throws() { var a = () => AnalyticsHelpers.PrepM(null!); a.Should().Throw<ArgumentException>(); }
        [Fact] public void PrepV_NaN_throws() { var a = () => AnalyticsHelpers.PrepV(new object[] { double.NaN }); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void PrepV_Inf_throws() { var a = () => AnalyticsHelpers.PrepV(new object[] { double.PositiveInfinity }); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); } // ToDouble L1 guard now converts Infinity→NaN before PrepV sees it
        [Fact] public void PrepV_null_returns_empty() { var result = AnalyticsHelpers.PrepV(null!); result.Should().BeEmpty(); }
        [Fact] public void PrepM_single_element() { var r = AnalyticsHelpers.PrepM(new object[,] { { 42.0 } }); r[0, 0].Should().Be(42.0); r.GetLength(0).Should().Be(1); }
    }

    public class AnalyticsHelpersCoverageGapTests
    {
        [Fact] public void DictToReport_unpacks_object_and_typed_arrays()
        {
            var d = new System.Collections.Generic.Dictionary<string, object>
            {
                ["obj"] = new object[] { 1, "x", null! },
                ["ints"] = new[] { 7, 8 },
                ["scalar"] = 42,
            };
            var r = AnalyticsHelpers.DictToReport(d);
            int RowOf(string key)
            {
                for (int i = 0; i < r.GetLength(0); i++)
                    if ((string)r[i, 0] == key) return i;
                return -1;
            }
            r.GetLength(1).Should().Be(4);
            var objRow = RowOf("obj");
            r[objRow, 1].Should().Be(1);
            r[objRow, 2].Should().Be("x");
            r[objRow, 3].Should().BeNull();
            r[RowOf("ints"), 1].Should().Be(7);
            r[RowOf("ints"), 2].Should().Be(8);
            r[RowOf("scalar"), 1].Should().Be(42);
        }

        [Fact] public void ToJaggedColumns_skips_blank_header_cells()
        {
            var data = new object[2, 2] { { null!, "h" }, { 1.0, 2.0 } };
            var g = AnalyticsHelpers.ToJaggedColumns(data);
            g[0].Should().Equal(1.0);
            g[1].Should().Equal(2.0);
        }
    }
}
