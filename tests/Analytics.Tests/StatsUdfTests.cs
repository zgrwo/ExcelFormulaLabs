using System;
using ExcelVbaLibraries.Analytics;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Analytics.Tests
{
    public class StatsUdfTests
    {
        private static readonly double[] D = { 1, 2, 3, 4, 5 };
        private static readonly double[] D5 = { 1, 2, 3, 4, 9 };
        private static readonly double[] X = { 1, 2, 3, 4, 5 };
        private static readonly double[] Y = { 2, 4, 6, 8, 10 };

        // ── Basic stats (V-based, return boxed double) ──
        [Fact] public void Mean() => ((double)StatsUdf.UDF_STAT_MEAN(D)).Should().BeApproximately(3.0, 1e-10);
        [Fact] public void GeoMean() => ((double)StatsUdf.UDF_STAT_GEO(D)).Should().BeApproximately(2.6051710846973517, 1e-10);
        [Fact] public void HarMean() => ((double)StatsUdf.UDF_STAT_HAR(D)).Should().BeApproximately(2.18978102189781, 1e-10);
        [Fact] public void Median() => ((double)StatsUdf.UDF_STAT_MED(D)).Should().BeApproximately(3.0, 1e-10);
        [Fact] public void VarianceP() => ((double)StatsUdf.UDF_STAT_VP(D)).Should().BeApproximately(2.0, 1e-10);
        [Fact] public void Variance() => ((double)StatsUdf.UDF_STAT_VAR(D)).Should().BeApproximately(2.5, 1e-10);
        [Fact] public void StdevP() => ((double)StatsUdf.UDF_STAT_STDP(D)).Should().BeApproximately(1.4142135623730951, 1e-10);
        [Fact] public void Stdev() => ((double)StatsUdf.UDF_STAT_STD(D)).Should().BeApproximately(Math.Sqrt(2.5), 1e-10);
        [Fact] public void Skew() => ((double)StatsUdf.UDF_STAT_SKEW(D5)).Should().BeApproximately(1.5, 0.5);
        [Fact] public void Kurt() => ((double)StatsUdf.UDF_STAT_KURT(D5)).Should().BeApproximately(2.6750983101285986, 1e-10);
        [Fact] public void Min() => ((double)StatsUdf.UDF_STAT_MIN(D)).Should().Be(1.0);
        [Fact] public void Max() => ((double)StatsUdf.UDF_STAT_MAX(D)).Should().Be(5.0);
        [Fact] public void Range() => ((double)StatsUdf.UDF_STAT_RNG(D)).Should().Be(4.0);
        [Fact] public void Sum() => ((double)StatsUdf.UDF_STAT_SUM(D)).Should().Be(15.0);
        [Fact] public void Product() => ((double)StatsUdf.UDF_STAT_PROD(D)).Should().BeApproximately(120.0, 1e-10);

        // ── Percentile / IQR ──
        [Fact] public void Percentile_p50() => ((double)StatsUdf.UDF_STAT_PCT(D, 50.0)).Should().BeApproximately(3.0, 1e-10);
        [Fact] public void IQR() => ((double)StatsUdf.UDF_STAT_IQR(D5)).Should().BeGreaterThan(0.0);

        // ── Summary / ZScore / Count / Mode ──
        [Fact] public void Summary_length()
        {
            var r = (double[])StatsUdf.UDF_STAT_SUMM(D);
            r.Length.Should().Be(9);
        }
        [Fact] public void ZScore_length()
        {
            var r = (double[])StatsUdf.UDF_STAT_ZS(D);
            r.Length.Should().Be(5);
        }
        [Fact] public void Count() => ((int)StatsUdf.UDF_STAT_CNT(D)).Should().Be(5);
        [Fact] public void Mode() => ((double)StatsUdf.UDF_STAT_MODE(new double[] { 1, 2, 2, 3 })).Should().Be(2.0);

        // ── Covariance / Correlation ──
        [Fact] public void CovarianceP_perfect_linear() => ((double)StatsUdf.UDF_STAT_CVP(X, Y)).Should().BeApproximately(4.0, 1e-10);
        [Fact] public void Covariance_perfect_linear() => ((double)StatsUdf.UDF_STAT_CV(X, Y)).Should().BeApproximately(5.0, 1e-10);
        [Fact] public void Pearson_perfect() => ((double)StatsUdf.UDF_STAT_PEAR(new double[] { 1, 2, 3 }, new double[] { 2, 4, 6 })).Should().BeApproximately(1.0, 1e-10);
        [Fact] public void Spearman_perfect() => ((double)StatsUdf.UDF_STAT_SPR(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 })).Should().BeApproximately(1.0, 1e-10);

        // ── T-Tests ──
        [Fact] public void TTest1_mu0() => ((double)StatsUdf.UDF_STAT_T1(D, 0.0)).Should().BeApproximately(0.0132356, 1e-6);
        [Fact] public void TTest2_two_groups() => ((double)StatsUdf.UDF_STAT_T2(new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 })).Should().BeLessThan(0.05);

        // ── MapOverFlat: ALWAYS returns object[], even for scalar ──
        [Fact] public void Abs_scalar_returns_object_array()
        {
            var r = (object[])StatsUdf.UDF_STAT_ABS(-5.0);
            ((double)r[0]).Should().Be(5.0);
        }
        [Fact] public void Sqrt_scalar_returns_object_array()
        {
            var r = (object[])StatsUdf.UDF_STAT_SQRT(4.0);
            ((double)r[0]).Should().Be(2.0);
        }
        [Fact] public void Ln_scalar_returns_object_array()
        {
            var r = (object[])StatsUdf.UDF_STAT_LN(Math.E);
            ((double)r[0]).Should().BeApproximately(1.0, 1e-10);
        }
        [Fact] public void Log10_scalar_returns_object_array()
        {
            var r = (object[])StatsUdf.UDF_STAT_LOG10(100.0);
            ((double)r[0]).Should().BeApproximately(2.0, 1e-10);
        }
        [Fact] public void Exp_scalar_returns_object_array()
        {
            var r = (object[])StatsUdf.UDF_STAT_EXP(0.0);
            ((double)r[0]).Should().BeApproximately(1.0, 1e-10);
        }
        [Fact] public void Sign_scalar_returns_object_array()
        {
            var r = (object[])StatsUdf.UDF_STAT_SGN(-3.0);
            r.Should().BeOfType<object[]>();
            ((long)r[0]).Should().Be(-1L);
        }
    }
}
