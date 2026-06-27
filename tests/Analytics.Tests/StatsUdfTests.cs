using System;
using FormulaLabs.Analytics;
using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.Analytics.Tests
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
        [Fact] public void Mode_ties_returns_smallest() => ((double)StatsUdf.UDF_STAT_MODE(new double[] { 2, 2, 1, 1 })).Should().Be(1.0); // ties: returns smallest, matches scipy

        // ── Covariance / Correlation ──
        [Fact] public void CovarianceP_perfect_linear() => ((double)StatsUdf.UDF_STAT_CVP(X, Y)).Should().BeApproximately(4.0, 1e-10);
        [Fact] public void Covariance_perfect_linear() => ((double)StatsUdf.UDF_STAT_CV(X, Y)).Should().BeApproximately(5.0, 1e-10);
        [Fact] public void Pearson_perfect() => ((double)StatsUdf.UDF_STAT_PEAR(new double[] { 1, 2, 3 }, new double[] { 2, 4, 6 })).Should().BeApproximately(1.0, 1e-10);
        [Fact] public void Spearman_perfect() => ((double)StatsUdf.UDF_STAT_SPR(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 })).Should().BeApproximately(1.0, 1e-10);

        // ── T-Tests ──
        [Fact] public void TTest1_mu0() => ((double)StatsUdf.UDF_STAT_T1(D, 0.0)).Should().BeApproximately(0.0132356, 1e-6);
        [Fact] public void TTest2_two_groups() => ((double)StatsUdf.UDF_STAT_T2(new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 })).Should().BeLessThan(0.05);

        // ── MapOverFlat: ALWAYS returns object[], even for scalar ──
        [Fact] public void Abs_scalar() => ((double)StatsUdf.UDF_STAT_ABS(-5.0)).Should().Be(5.0);
        [Fact] public void Sqrt_scalar() => ((double)StatsUdf.UDF_STAT_SQRT(4.0)).Should().Be(2.0);
        [Fact] public void Ln_scalar() => ((double)StatsUdf.UDF_STAT_LN(Math.E)).Should().BeApproximately(1.0, 1e-10);
        [Fact] public void Log10_scalar() => ((double)StatsUdf.UDF_STAT_LOG10(100.0)).Should().BeApproximately(2.0, 1e-10);
        [Fact] public void Exp_scalar() => ((double)StatsUdf.UDF_STAT_EXP(0.0)).Should().BeApproximately(1.0, 1e-10);
        [Fact] public void Sign_scalar() => ((long)StatsUdf.UDF_STAT_SGN(-3.0)).Should().Be(-1L);

        // ── Multi-arg methods (CVP, CV, PEAR, SPR, T1, T2) ──
        // Size mismatch -> MapOverMulti returns ExcelError.Value
        // Null first arg -> MapOverMulti returns ExcelEmpty.Value
        // Pearson negative correlation (data: {1,2,3,4,5} vs {10,8,6,4,2} -> r=-1)
        [Fact] public void Pear_negative() => ((double)StatsUdf.UDF_STAT_PEAR(new double[]{1,2,3,4,5},new double[]{10,8,6,4,2})).Should().BeApproximately(-1.0,1e-10);
        // TTest: single value (n<2) -> NaN
        // TTest: same groups -> p~1.0
        [Fact] public void T2_same_group() => ((double)StatsUdf.UDF_STAT_T2(D,D)).Should().BeApproximately(1.0,0.1);

        // ── MapOver element-wise edge cases (Abs, Sqrt, Ln, Log10, Exp, Sign) ──
        // Abs: array input
        [Fact] public void Abs_array() { var r=(object[])StatsUdf.UDF_STAT_ABS(new object[]{-1.0,2,-3}); r.Should().Equal(1.0,2.0,3.0); }
        [Fact] public void Abs_zero() => ((double)StatsUdf.UDF_STAT_ABS(0.0)).Should().Be(0.0);
        [Fact] public void Sqrt_zero() => ((double)StatsUdf.UDF_STAT_SQRT(0.0)).Should().Be(0.0);
        [Fact] public void Sqrt_negative_NaN() => ((double)StatsUdf.UDF_STAT_SQRT(-1.0)).Should().Be(double.NaN);
        // Sqrt: array
        [Fact] public void Sqrt_array() { var r=(object[])StatsUdf.UDF_STAT_SQRT(new object[]{4.0,9,16}); r.Should().Equal(2.0,3.0,4.0); }
        [Fact] public void Ln_one() => ((double)StatsUdf.UDF_STAT_LN(1.0)).Should().Be(0.0);
        [Fact] public void Ln_zero() => ((double)StatsUdf.UDF_STAT_LN(0.0)).Should().Be(double.NaN); // Excel =LN(0) → #NUM! — L1 guard rejects Infinity
        [Fact] public void Ln_negative_NaN() => ((double)StatsUdf.UDF_STAT_LN(-1.0)).Should().Be(double.NaN);
        [Fact] public void Log10_one() => ((double)StatsUdf.UDF_STAT_LOG10(1.0)).Should().Be(0.0);
        [Fact] public void Log10_zero() => ((double)StatsUdf.UDF_STAT_LOG10(0.0)).Should().Be(double.NaN); // L1 guard rejects -Infinity
        [Fact] public void Log10_negative_NaN() => ((double)StatsUdf.UDF_STAT_LOG10(-1.0)).Should().Be(double.NaN);
        [Fact] public void Exp_one() => ((double)StatsUdf.UDF_STAT_EXP(1.0)).Should().BeApproximately(Math.E,1e-10);
        [Fact] public void Exp_negative() => ((double)StatsUdf.UDF_STAT_EXP(-10.0)).Should().BeApproximately(Math.Exp(-10),1e-10);
        [Fact] public void Exp_large_infinity() => ((double)StatsUdf.UDF_STAT_EXP(1000.0)).Should().Be(double.PositiveInfinity);
        [Fact] public void Sign_positive() => ((long)StatsUdf.UDF_STAT_SGN(42.0)).Should().Be(1L);
        [Fact] public void Sign_negative() => ((long)StatsUdf.UDF_STAT_SGN(-7.0)).Should().Be(-1L);
        [Fact] public void Sign_zero() => ((long)StatsUdf.UDF_STAT_SGN(0.0)).Should().Be(0L);
        // Sign: array
        [Fact] public void Sign_array() { var r=(object[])StatsUdf.UDF_STAT_SGN(new object[]{5.0,-2.0,0.0}); r.Should().Equal(1L,-1L,0L); }

        // ── Error path: null/empty/degenerate inputs ──
        // V-based UDFs: empty/null → NaN (matching scipy convention)
        [Fact] public void Mean_null() => ((double)StatsUdf.UDF_STAT_MEAN(null!)).Should().Be(double.NaN);
        [Fact] public void Mean_empty() => ((double)StatsUdf.UDF_STAT_MEAN(new double[0])).Should().Be(double.NaN);
        [Fact] public void Median_null() => ((double)StatsUdf.UDF_STAT_MED(null!)).Should().Be(double.NaN);
        [Fact] public void Stdev_null() => ((double)StatsUdf.UDF_STAT_STD(null!)).Should().Be(double.NaN);
        [Fact] public void Stdev_insufficient_data() => ((double)StatsUdf.UDF_STAT_STD(new double[]{42})).Should().Be(double.NaN);
        [Fact] public void Min_null() => ((double)StatsUdf.UDF_STAT_MIN(null!)).Should().Be(double.NaN);
        [Fact] public void Variance_null() => ((double)StatsUdf.UDF_STAT_VAR(null!)).Should().Be(double.NaN);

        // ZScore: constant data → #VALUE!
        [Fact] public void ZScore_constant_returns_error() => StatsUdf.UDF_STAT_ZS(new double[]{5,5,5}).Should().Be(ExcelError.Value);

        // Multi-arg edge cases: V() passes directly to Core (NOT MapOverMulti). Mismatch/null -> NaN.
        [Fact] public void Cvp_mismatch() => ((double)StatsUdf.UDF_STAT_CVP(X, new double[]{1.0,2})).Should().Be(double.NaN);
        [Fact] public void Cv_mismatch() => ((double)StatsUdf.UDF_STAT_CV(X, new double[]{1.0,2})).Should().Be(double.NaN);
        [Fact] public void Pear_mismatch() => ((double)StatsUdf.UDF_STAT_PEAR(X, new double[]{1.0,2})).Should().Be(double.NaN);
        [Fact] public void Spr_mismatch() => ((double)StatsUdf.UDF_STAT_SPR(X, new double[]{1.0,2})).Should().Be(double.NaN);
        // Null/empty input → NaN (consistent with Covariance/TTest pattern across StatsCore)
        [Fact] public void Cvp_null_first() => ((double)StatsUdf.UDF_STAT_CVP(null!, Y)).Should().Be(double.NaN);
        [Fact] public void Cv_null_first() => ((double)StatsUdf.UDF_STAT_CV(null!, Y)).Should().Be(double.NaN);
        [Fact] public void Pear_null_first() => ((double)StatsUdf.UDF_STAT_PEAR(null!, Y)).Should().Be(double.NaN);
        // TTest: single value (n<2) -> NaN; mismatch -> NaN
        [Fact] public void T1_single_value() => ((double)StatsUdf.UDF_STAT_T1(new double[]{5.0},0.0)).Should().Be(double.NaN);
        [Fact] public void T2_mismatch() => ((double)StatsUdf.UDF_STAT_T2(X, new double[]{1.0})).Should().Be(double.NaN);
    }
}
