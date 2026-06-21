using System;
using ExcelVbaLibraries.Analytics;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Analytics.Tests
{
    public class RegressionUdfTests
    {
        private static readonly double[,] X = { { 1, 1 }, { 1, 2 }, { 1, 3 } };
        private static readonly double[]   y = { 5, 8, 11 };

        // ── Regression methods (return object[,] from Dict2Row) ──
        [Fact] public void OLS_returns_2xn()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_OLS(X, y);
            r.GetLength(0).Should().Be(2);
            r.GetLength(1).Should().BeGreaterOrEqualTo(5);
        }
        [Fact] public void WLS_equal_weights()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_WLS(X, y, new double[] { 1.0, 1.0, 1.0 });
            r.GetLength(0).Should().Be(2);
        }
        [Fact] public void Ridge_keys()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_RIDGE(X, y, 0.1);
            r.GetLength(0).Should().Be(2);
        }
        [Fact] public void Anova1_keys()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_ANOVA1(new double[,] { { 5, 8 }, { 6, 9 }, { 7, 10 } });
            r.GetLength(0).Should().Be(2);
        }

        // ── Factor importance (returns long[]) ──
        [Fact] public void FactorImportance_length()
        {
            var r = (long[])RegressionUdf.UDF_REGRESS_FACTORIMP(X, y);
            r.Length.Should().Be(2);
        }

        // ── OLS coefficients / R-squared (return double[] and double) ──
        [Fact] public void Coef_values()
        {
            var c = (double[])RegressionUdf.UDF_REGRESS_COEF(X, y);
            c[0].Should().BeApproximately(2.0, 1e-8);
            c[1].Should().BeApproximately(3.0, 1e-8);
        }
        [Fact] public void Rsq_is_one_for_perfect_fit() => ((double)RegressionUdf.UDF_REGRESS_RSQ(X, y)).Should().BeApproximately(1.0, 1e-10);
    }
}
