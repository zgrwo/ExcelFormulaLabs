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
        [Fact] public void PrepV_empty() { var result = AnalyticsHelpers.PrepV(ExcelEmpty.Value); result.Should().BeEmpty(); }
        // NaN/Inf guards (防错原则1: PrepM/PrepV now uniformly throw)
        [Fact] public void ToDoubleMatrix_NaN_throws() { var a = () => AnalyticsHelpers.ToDoubleMatrix(new object[,] { { double.NaN, 1.0 }, { 2.0, 3.0 } }); a.Should().Throw<ArgumentException>().WithMessage("*non-numeric*"); }
        [Fact] public void ToDoubleMatrix_Inf_throws() { var a = () => AnalyticsHelpers.ToDoubleMatrix(new object[,] { { double.PositiveInfinity, 1.0 }, { 2.0, 3.0 } }); a.Should().Throw<ArgumentException>().WithMessage("*non-numeric*"); }
        [Fact] public void PrepM_null_throws() { var a = () => AnalyticsHelpers.PrepM(null!); a.Should().Throw<ArgumentException>(); }
        [Fact] public void PrepV_NaN_throws() { var a = () => AnalyticsHelpers.PrepV(new object[] { double.NaN }); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void PrepV_Inf_throws() { var a = () => AnalyticsHelpers.PrepV(new object[] { double.PositiveInfinity }); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); } // ToDouble L1 guard now converts Infinity→NaN before PrepV sees it
        [Fact] public void PrepV_null_returns_empty() { var result = AnalyticsHelpers.PrepV(null!); result.Should().BeEmpty(); }
        [Fact] public void PrepM_single_element() { var r = AnalyticsHelpers.PrepM(new object[,] { { 42.0 } }); r[0, 0].Should().Be(42.0); r.GetLength(0).Should().Be(1); }
    }
}
