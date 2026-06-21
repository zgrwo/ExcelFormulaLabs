using System;
using ExcelVbaLibraries.Analytics;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Analytics.Tests
{
    public class AnalyticsHelpersTests
    {
        [Fact] public void ToDoubleMatrix_basic()
        { var result = AnalyticsHelpers.ToDoubleMatrix(new object[,]{{1.0,2.0},{3.0,4.0}}); result[0,0].Should().Be(1.0); result[1,1].Should().Be(4.0); }
        [Fact] public void ToDoubleMatrix_with_non_numeric()
        { var result = AnalyticsHelpers.ToDoubleMatrix(new object[,]{{1.0,"text"},{3.0,4.0}}); result[0,1].Should().Be(double.NaN); }
        [Fact] public void ToDoubleMatrix_empty() { var result = AnalyticsHelpers.ToDoubleMatrix(new object[0,0]); result.GetLength(0).Should().Be(0); }
        [Fact] public void PrepM_scalar() { var result = AnalyticsHelpers.PrepM(42); result[0,0].Should().Be(42.0); }
        [Fact] public void PrepM_2D() { var result = AnalyticsHelpers.PrepM(new object[,]{{1.0,2.0},{3.0,4.0}}); result.GetLength(0).Should().Be(2); }
        [Fact] public void PrepM_null_or_empty() { var result = AnalyticsHelpers.PrepM(ExcelEmpty.Value); result[0,0].Should().Be(double.NaN); }
        [Fact] public void PrepV_scalar() { var result = AnalyticsHelpers.PrepV(42); result.Length.Should().Be(1); result[0].Should().Be(42.0); }
        [Fact] public void PrepV_1D() { var result = AnalyticsHelpers.PrepV(new object[]{1.0,2,3}); result.Length.Should().Be(3); }
        [Fact] public void PrepV_2D_flattens() { var result = AnalyticsHelpers.PrepV(new object[,]{{1.0},{2.0},{3.0}}); result.Length.Should().Be(3); }
        [Fact] public void PrepV_empty() { var result = AnalyticsHelpers.PrepV(ExcelEmpty.Value); result.Should().BeEmpty(); }
    }
}
