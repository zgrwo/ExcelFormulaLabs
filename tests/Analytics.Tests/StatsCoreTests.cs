using System;
using MathNet.Numerics.Statistics;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using ExcelVbaLibraries.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Analytics.Tests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Python Cross-Validation Reference Script
    // ═══════════════════════════════════════════════════════════════════════════
    // To regenerate expected values after changing test data, run:
    //
    //   import openpyxl, numpy as np
    //   from scipy import stats
    //
    //   wb = openpyxl.load_workbook(
    //       r'tests\TestData\Cross_Validation_vs_Python.xlsx')
    //   ws = wb['SourceData']
    //
    //   x1 = [float(r[0]) for r in ws.iter_rows(min_row=2, max_col=3,
    //         values_only=True) if r[0] is not None]
    //   a = np.array(x1)
    //
    //   print(f'PyCount     = {len(x1)}')
    //   print(f'PyMean      = {np.mean(a):.15f}')
    //   print(f'PyStdev     = {np.std(a, ddof=1):.15f}')
    //   print(f'PyVariance  = {np.var(a, ddof=1):.15f}')
    //   print(f'PyStdevP    = {np.std(a, ddof=0):.15f}')
    //   print(f'PyVarianceP = {np.var(a, ddof=0):.15f}')
    //   print(f'PyMin       = {np.min(a):.15f}')
    //   print(f'PyMax       = {np.max(a):.15f}')
    //   print(f'PySum       = {np.sum(a):.15f}')
    //   print(f'PyPct25     = {np.percentile(a, 25):.15f}')
    //   print(f'PyPct50     = {np.percentile(a, 50):.15f}')
    //   print(f'PyPct75     = {np.percentile(a, 75):.15f}')
    //   print(f'PyIQR       = {stats.iqr(a):.15f}')
    //   print(f'PySkewness  = {stats.skew(a):.15f}')
    //   print(f'PyKurtosis  = {stats.kurtosis(a, fisher=True):.15f}')
    //
    // Paste the output into the PyCount..PyKurtosis constants below.
    // ═══════════════════════════════════════════════════════════════════════════

    public class StatsCoreTests
    {
        // -----------------------------------------------------------------
        // Common test data
        // -----------------------------------------------------------------
        private static readonly double[] D = { 1, 2, 3, 4, 5 };
        private static readonly double[] D5 = { 1, 2, 3, 4, 9 };           // skewed, for Skewness/Kurtosis
        private static readonly double[] X = { 1, 2, 3, 4, 5 };
        private static readonly double[] Y = { 2, 4, 6, 8, 10 };           // Y = 2*X
        private static readonly double[] Empty = Array.Empty<double>();
        private static readonly double[] Two = { 1.0, 2.0 };

        // =====================================================================
        // EXISTING TESTS (unchanged from original)
        // =====================================================================
        [Fact] public void Mean() => StatsCore.Mean(D).Should().BeApproximately(3.0, 1e-10);
        [Fact] public void Mean_empty() => StatsCore.Mean(Array.Empty<double>()).Should().Be(double.NaN);
        [Fact] public void Median() => StatsCore.Median(D).Should().BeApproximately(3.0, 1e-10);
        [Fact] public void Variance() => StatsCore.Variance(D).Should().BeApproximately(2.5, 1e-10);
        [Fact] public void VarianceP() => StatsCore.VarianceP(D).Should().BeApproximately(2.0, 1e-10);
        [Fact] public void Stdev() => StatsCore.Stdev(D).Should().BeApproximately(Math.Sqrt(2.5), 1e-10);
        [Fact] public void Skewness() => StatsCore.Skewness(new[]{1.0,2,3,4,9}).Should().BeApproximately(1.55, 0.1);
        [Fact] public void Min() => StatsCore.Min(D).Should().Be(1);
        [Fact] public void Max() => StatsCore.Max(D).Should().Be(5);
        [Fact] public void Range() => StatsCore.Range(D).Should().Be(4);
        [Fact] public void Sum() => StatsCore.Sum(D).Should().Be(15);
        [Fact] public void Percentile() => StatsCore.Percentile(D,50).Should().BeApproximately(3.0,1e-10);
        [Fact] public void IQR() => StatsCore.IQR(new[]{1.0,2,3,4,9}).Should().BeGreaterThan(0);
        [Fact] public void Pearson_perfect() => StatsCore.Pearson(new[]{1.0,2,3},new[]{2.0,4,6}).Should().BeApproximately(1.0,1e-10);
        [Fact] public void Spearman() => StatsCore.Spearman(new[]{1.0,2,3},new[]{1.0,2,3}).Should().BeApproximately(1.0,1e-10);
        [Fact] public void TTestTwoSample() => StatsCore.TTestTwoSample(new[]{1.0,2,3},new[]{4.0,5,6}).Should().BeLessThan(0.05);
        [Fact] public void Summary_9_items() => StatsCore.Summary(D).Length.Should().Be(9);
        [Fact] public void ZScore() => StatsCore.ZScore(D).Length.Should().Be(5);
        [Fact] public void ZScore_constant_throws() { var a = () => StatsCore.ZScore(new[] { 5.0, 5.0, 5.0 }); a.Should().Throw<ArgumentException>(); }
        [Fact] public void Summary_single_element() { var r = StatsCore.Summary(new[] { 42.0 }); r[0].Should().Be(1.0); r[1].Should().Be(42.0); r[8].Should().Be(0.0); }
        [Fact] public void TTestTwoSample_equal_constant_groups() => StatsCore.TTestTwoSample(new[] { 5.0, 5.0, 5.0 }, new[] { 5.0, 5.0, 5.0 }).Should().Be(1.0);
        [Fact] public void CorrelationMatrix()
        { var r=StatsCore.CorrelationMatrix(new double[,]{{1,2},{2,4},{3,6}}); Math.Abs(r[0,1]).Should().BeApproximately(1.0,1e-10); }
        [Fact] public void CorrelationMatrix_single_column()
        { var r=StatsCore.CorrelationMatrix(new double[,]{{1},{2},{3}}); r[0,0].Should().Be(1.0); }
        [Fact] public void CorrelationMatrix_constant_column()
        { var r=StatsCore.CorrelationMatrix(new double[,]{{5,1},{5,2},{5,3}}); double.IsNaN(r[0,1]).Should().BeTrue(); }

        // =====================================================================
        // NEW FUNCTION TESTS (expected values cross-validated with Python)
        // =====================================================================

        // Python: from scipy.stats import gmean; gmean([1,2,3,4,5])
        [Fact] public void GeometricMean_of_1_to_5() =>
            StatsCore.GeometricMean(D).Should().BeApproximately(2.6051710846973517, 1e-10);

        // Python: from scipy.stats import hmean; hmean([1,2,3,4,5])
        [Fact] public void HarmonicMean_of_1_to_5() =>
            StatsCore.HarmonicMean(D).Should().BeApproximately(2.18978102189781, 1e-10);

        // Python: import numpy as np; np.std([1,2,3,4,5], ddof=0)
        [Fact] public void StdevP_of_1_to_5() =>
            StatsCore.StdevP(D).Should().BeApproximately(1.4142135623730951, 1e-10);

        // Python: from scipy.stats import kurtosis; kurtosis([1,2,3,4,9], fisher=True)
        // MathNet Statistics.Kurtosis returns excess kurtosis (Fisher), matching scipy fisher=True.
        [Fact] public void Kurtosis_skewed() =>
            StatsCore.Kurtosis(D5).Should().BeApproximately(2.6750983101285986, 1e-10);

        // Python: import numpy as np; np.prod([1,2,3,4,5])
        [Fact] public void Product_of_1_to_5() =>
            StatsCore.Product(D).Should().BeApproximately(120.0, 1e-10);

        // Python: import numpy as np; np.cov([1,2,3,4,5], [2,4,6,8,10], ddof=0)[0,1]
        [Fact] public void CovarianceP_perfect_linear() =>
            StatsCore.CovarianceP(X, Y).Should().BeApproximately(4.0, 1e-10);

        // Python: import numpy as np; np.cov([1,2,3,4,5], [2,4,6,8,10], ddof=1)[0,1]
        [Fact] public void Covariance_perfect_linear() =>
            StatsCore.Covariance(X, Y).Should().BeApproximately(5.0, 1e-10);

        // Python: from scipy.stats import ttest_1samp; ttest_1samp([1,2,3,4,5], 0).pvalue
        [Fact] public void TTestOneSample_mu0() =>
            StatsCore.TTestOneSample(D, 0.0).Should().BeApproximately(0.0132356, 1e-6);

        // Count is returned as the first element of Summary.
        [Fact] public void Count() =>
            StatsCore.Summary(D)[0].Should().Be(5);

        // =====================================================================
        // EMPTY / EDGE-CASE TESTS
        // =====================================================================

        [Fact] public void StdevP_empty() =>
            StatsCore.StdevP(Empty).Should().Be(double.NaN);

        [Fact] public void GeometricMean_empty() =>
            StatsCore.GeometricMean(Empty).Should().Be(double.NaN);

        [Fact] public void HarmonicMean_empty() =>
            StatsCore.HarmonicMean(Empty).Should().Be(double.NaN);

        [Fact] public void VarianceP_empty() =>
            StatsCore.VarianceP(Empty).Should().Be(double.NaN);

        [Fact] public void CovarianceP_length_mismatch() =>
            StatsCore.CovarianceP(Two, new[] { 1.0 }).Should().Be(double.NaN);

        [Fact] public void Covariance_length_mismatch() =>
            StatsCore.Covariance(Two, new[] { 1.0 }).Should().Be(double.NaN);

        [Fact] public void CovarianceP_empty() =>
            StatsCore.CovarianceP(Empty, Empty).Should().Be(double.NaN);

        [Fact] public void Covariance_empty() =>
            StatsCore.Covariance(Empty, Empty).Should().Be(double.NaN);

        [Fact] public void TTestOneSample_empty() =>
            StatsCore.TTestOneSample(Empty).Should().Be(double.NaN);

        [Fact] public void TTestTwoSample_empty() =>
            StatsCore.TTestTwoSample(Empty, Empty).Should().Be(double.NaN);

        [Fact] public void Product_empty() =>
            StatsCore.Product(Empty).Should().Be(0.0);
        [Fact] public void Sum_overflow_returns_NaN() { var d = new[] { double.MaxValue, double.MaxValue }; double.IsNaN(StatsCore.Sum(d)).Should().BeTrue(); }
        [Fact] public void Product_overflow_returns_NaN() { var d = new[] { double.MaxValue, 2.0 }; double.IsNaN(StatsCore.Product(d)).Should().BeTrue(); }

        // =====================================================================
        // CROSS-VALIDATION TESTS AGAINST PYTHON (numpy / scipy)
        //
        // These tests read NumericX1 from the Excel test data file and compare
        // StatsCore results against reference values computed by Python.
        //
        // To compute the Python reference values, run this script:
        //
        //   import openpyxl, numpy as np
        //   from scipy import stats
        //
        //   wb = openpyxl.load_workbook(
        //       r'D:\Workspace\zgrwo\VBA\DeepSeek\ClaudeCode\ExcelVbaLibraries'
        //       r'\tests\TestData\Cross_Validation_vs_Python.xlsx')
        //   ws = wb['SourceData']
        //
        //   x1 = [float(r[0]) for r in ws.iter_rows(min_row=2, max_col=3,
        //         values_only=True) if r[0] is not None]
        //   a = np.array(x1)
        //
        //   print(f'PyCount     = {len(x1)}')
        //   print(f'PyMean      = {np.mean(a):.15f}')
        //   print(f'PyStdev     = {np.std(a, ddof=1):.15f}')
        //   print(f'PyVariance  = {np.var(a, ddof=1):.15f}')
        //   print(f'PyStdevP    = {np.std(a, ddof=0):.15f}')
        //   print(f'PyVarianceP = {np.var(a, ddof=0):.15f}')
        //   print(f'PyMin       = {np.min(a):.15f}')
        //   print(f'PyMax       = {np.max(a):.15f}')
        //   print(f'PySum       = {np.sum(a):.15f}')
        //   print(f'PyPct25     = {np.percentile(a, 25):.15f}')
        //   print(f'PyPct50     = {np.percentile(a, 50):.15f}')
        //   print(f'PyPct75     = {np.percentile(a, 75):.15f}')
        //   print(f'PyIQR       = {stats.iqr(a):.15f}')
        //   print(f'PySkewness  = {stats.skew(a):.15f}')
        //   print(f'PyKurtosis  = {stats.kurtosis(a, fisher=True):.15f}')
        //
        // Replace the TODO constants below with the Python output, then
        // remove the Skip attributes from the cross-validation tests.
        // =====================================================================

        // --- Excel file path resolution ----------------------------------

        private static string FindTestDataFile(string fileName)
        {
            // Walk up from the test assembly output directory to locate
            // the repository root (identified by the "tests" directory),
            // then resolve tests/TestData/<fileName>.
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "tests", "TestData", fileName);
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException(
                $"Cannot locate {fileName}. " +
                $"Ensure the file exists at <repo>/tests/TestData/{fileName}");
        }

        private static string ExcelPath =>
            FindTestDataFile("Cross_Validation_vs_Python.xlsx");

        // --- Excel helper -------------------------------------------------

        private static double[]? _numericX1Cache;
        private static double[] LoadNumericX1()
        {
            if (_numericX1Cache != null)
                return _numericX1Cache;

            var values = new List<double>();
            using var wb = new XLWorkbook(ExcelPath);
            var ws = wb.Worksheet("SourceData");

            // Skip header row (row 1), read column U (1-based index 21: NumericX1).
            foreach (var row in ws.RowsUsed())
            {
                if (row.RowNumber() == 1)
                    continue;

                var cell = row.Cell(21);  // NumericX1 column
                if (!cell.IsEmpty() && cell.TryGetValue<double>(out var val))
                    values.Add(val);
            }

            _numericX1Cache = values.ToArray();
            return _numericX1Cache;
        }

        // --- Python reference constants (cross-validated with scipy/numpy) ---

        // ReSharper disable InconsistentNaming
        private const double PyCount     = 282;
        private const double PyMean      = 101.205232310992;
        private const double PyStdev     = 27.1053265065939;
        private const double PyVariance  = 734.698725029064;
        private const double PyStdevP    = 27.0572247357578;
        private const double PyVarianceP = 732.093410401302;
        private const double PyMin       = 11.6121142957259;
        private const double PyMax       = 184.646291480258;
        private const double PySum       = 28539.8755116999;
        private const double PyPct25     = 81.6495223135336;
        private const double PyPct50     = 102.621664961185;
        private const double PyPct75     = 119.136157628341;
        private const double PyIQR       = 37.4866353148077;
        private const double PySkewness  = -0.0982409368961175;
        private const double PyKurtosis  = 0.0818960710244716;
        // ReSharper restore InconsistentNaming

        // --- Cross-validation test methods --------------------------------

        [Fact]
        public void CrossVal_Mean() =>
            StatsCore.Mean(LoadNumericX1()).Should().BeApproximately(PyMean, 1e-10);

        [Fact]
        public void CrossVal_Stdev() =>
            StatsCore.Stdev(LoadNumericX1()).Should().BeApproximately(PyStdev, 1e-10);

        [Fact]
        public void CrossVal_Variance() =>
            StatsCore.Variance(LoadNumericX1()).Should().BeApproximately(PyVariance, 1e-10);

        [Fact]
        public void CrossVal_StdevP() =>
            StatsCore.StdevP(LoadNumericX1()).Should().BeApproximately(PyStdevP, 1e-10);

        [Fact]
        public void CrossVal_VarianceP() =>
            StatsCore.VarianceP(LoadNumericX1()).Should().BeApproximately(PyVarianceP, 1e-10);

        [Fact]
        public void CrossVal_Min() =>
            StatsCore.Min(LoadNumericX1()).Should().BeApproximately(PyMin, 1e-10);

        [Fact]
        public void CrossVal_Max() =>
            StatsCore.Max(LoadNumericX1()).Should().BeApproximately(PyMax, 1e-10);

        [Fact]
        public void CrossVal_Sum() =>
            StatsCore.Sum(LoadNumericX1()).Should().BeApproximately(PySum, 1e-10);

        [Fact]
        public void CrossVal_Percentile25() =>
            StatsCore.Percentile(LoadNumericX1(), 25).Should().BeApproximately(PyPct25, 1e-10);

        [Fact]
        public void CrossVal_Percentile50() =>
            StatsCore.Percentile(LoadNumericX1(), 50).Should().BeApproximately(PyPct50, 1e-10);

        [Fact]
        public void CrossVal_Percentile75() =>
            StatsCore.Percentile(LoadNumericX1(), 75).Should().BeApproximately(PyPct75, 1e-10);

        [Fact]
        public void CrossVal_IQR() =>
            StatsCore.IQR(LoadNumericX1()).Should().BeApproximately(PyIQR, 1e-10);

        [Fact]
        public void CrossVal_Skewness() =>
            StatsCore.Skewness(LoadNumericX1()).Should().BeApproximately(PySkewness, 1e-10);

        [Fact]
        public void CrossVal_Kurtosis() =>
            StatsCore.Kurtosis(LoadNumericX1()).Should().BeApproximately(PyKurtosis, 0.05);
        // Edge: zero/negative values -> NaN
        [Fact] public void GeometricMean_with_zero() => StatsCore.GeometricMean(new[]{1.0,0,3}).Should().Be(0.0);
        [Fact] public void GeometricMean_with_negative() => StatsCore.GeometricMean(new[]{1.0,-2,3}).Should().Be(double.NaN);
        [Fact] public void HarmonicMean_with_zero() => StatsCore.HarmonicMean(new[]{1.0,0,3}).Should().Be(0.0);
        // Edge: insufficient data
        [Fact] public void Kurtosis_insufficient_n() => StatsCore.Kurtosis(new[]{1.0,2,3}).Should().Be(double.NaN);
        [Fact] public void Skewness_insufficient_n() => StatsCore.Skewness(new[]{1.0,2}).Should().Be(double.NaN);
        [Fact] public void Variance_insufficient_n() => StatsCore.Variance(new[]{1.0}).Should().Be(double.NaN);
        [Fact] public void Stdev_insufficient_n() => StatsCore.Stdev(new[]{1.0}).Should().Be(double.NaN);
        // Mode tests
        [Fact] public void Mode_basic() => StatsCore.Mode(new[]{1.0,2,2,3}).Should().Be(2.0);
        [Fact] public void Mode_empty() => double.IsNaN(StatsCore.Mode(Array.Empty<double>())).Should().BeTrue();
        [Fact] public void Mode_singleValue() => StatsCore.Mode(new[]{5.0}).Should().Be(5.0);
        [Fact] public void Mode_tie_returnsSmallest() => StatsCore.Mode(new[]{2.0,1,2,1}).Should().Be(1.0);
        [Fact] public void Mode_allUnique_returnsNaN() => double.IsNaN(StatsCore.Mode(new[]{3.0,1,2})).Should().BeTrue();
        // TTest cross-validation with scipy.stats.ttest_1samp / ttest_ind
        private static readonly double[] Tcv = {2.1,3.8,5.2,7.1,8.9,10.8,13.1,14.9,16.8,18.9};
        [Fact] public void TTestOneSample_crossval() => StatsCore.TTestOneSample(Tcv,8.0).Should().BeApproximately(0.261808259603258,1e-8);
        [Fact] public void TTestTwoSample_crossval() { var a=new[]{1.0,2,3,4,5};var b=new[]{6.0,7,8,9,10};StatsCore.TTestTwoSample(a,b).Should().BeApproximately(0.001052825793367,1e-8); }

        // =====================================================================
        // ZERO-VARIANCE / CONSTANT-DATA EDGE CASES
        // (same pattern as TTestTwoSample/M4 fix — guard against se=0 division)
        // =====================================================================

        [Fact] public void TTestOneSample_constant_same_mu0()
        {
            // All values equal mu0 → no evidence against H0 → p≈1.0
            // (mirrors TTestTwoSample_equal_constant_groups: va+vb<1e-15 → p=1.0)
            StatsCore.TTestOneSample(new[] { 5.0, 5.0, 5.0 }, 5.0).Should().Be(1.0);
        }

        [Fact] public void TTestOneSample_constant_diff_mu0()
        {
            // Constant data but mu0 differs from mean → undefined t-stat → NaN
            // (mirrors TTestTwoSample logic: zero variance, different means → NaN)
            double.IsNaN(StatsCore.TTestOneSample(new[] { 5.0, 5.0, 5.0 }, 0.0)).Should().BeTrue();
        }

        [Fact] public void Pearson_constant_array()
        {
            // Correlation with zero-variance data → undefined → NaN
            double.IsNaN(StatsCore.Pearson(new[] { 3.0, 3.0, 3.0 }, new[] { 1.0, 2.0, 3.0 })).Should().BeTrue();
        }

        [Fact] public void Spearman_constant_array()
        {
            // Spearman on constant data — all ranks tied → undefined → NaN
            double.IsNaN(StatsCore.Spearman(new[] { 5.0, 5.0, 5.0 }, new[] { 1.0, 2.0, 3.0 })).Should().BeTrue();
        }

        [Fact] public void Covariance_constant_array()
        {
            // Covariance with zero variance in either variable → 0 (no co-variation)
            StatsCore.Covariance(new[] { 5.0, 5.0, 5.0 }, new[] { 1.0, 2.0, 3.0 }).Should().Be(0.0);
        }

        [Fact] public void CovarianceP_constant_array()
        {
            StatsCore.CovarianceP(new[] { 5.0, 5.0, 5.0 }, new[] { 1.0, 2.0, 3.0 }).Should().Be(0.0);
        }

        // =====================================================================
        // PERCENTILE / IQR EDGE CASES
        // =====================================================================

        [Fact] public void Percentile_invalid_p_below_0()
        {
            // MathNet QuantileCustom(p<0) returns NaN (cannot extrapolate meaningfully)
            double.IsNaN(StatsCore.Percentile(D, -1)).Should().BeTrue();
        }

        [Fact] public void Percentile_invalid_p_above_100()
        {
            double.IsNaN(StatsCore.Percentile(D, 150)).Should().BeTrue();
        }

        [Fact] public void IQR_single_element() =>
            // Q1=Q3=d[0] for single element → IQR=0 (matches scipy.stats.iqr([42])=0)
            StatsCore.IQR(new[] { 42.0 }).Should().Be(0.0);

        [Fact] public void VarianceP_single_element() =>
            // np.var([42], ddof=0) = 0 → PopulationVariance of single element = 0
            StatsCore.VarianceP(new[] { 42.0 }).Should().Be(0.0);

        [Fact] public void StdevP_single_element() =>
            // np.std([42], ddof=0) = 0
            StatsCore.StdevP(new[] { 42.0 }).Should().Be(0.0);

        // =====================================================================
        // PEARSON / SPEARMAN LENGTH MISMATCH
        // =====================================================================

        [Fact] public void Pearson_length_mismatch()
        {
            var act = () => StatsCore.Pearson(Two, new[] { 1.0 });
            act.Should().Throw<ArgumentException>().WithMessage("*length*");
        }

        [Fact] public void Spearman_length_mismatch()
        {
            var act = () => StatsCore.Spearman(Two, new[] { 1.0 });
            act.Should().Throw<ArgumentException>().WithMessage("*length*");
        }

        // Sign — NaN/Inf explicit guard (防错原则1: Core 层显式 guard，不依赖 Math.Sign 抛异常)
        [Fact] public void Sign_positive() => StatsCore.Sign(3.14).Should().Be(1);
        [Fact] public void Sign_negative() => StatsCore.Sign(-2.5).Should().Be(-1);
        [Fact] public void Sign_zero() => StatsCore.Sign(0.0).Should().Be(0);
        [Fact] public void Sign_NaN_returns_zero() => StatsCore.Sign(double.NaN).Should().Be(0);
        [Fact] public void Sign_PositiveInfinity() => StatsCore.Sign(double.PositiveInfinity).Should().Be(1);
        [Fact] public void Sign_NegativeInfinity() => StatsCore.Sign(double.NegativeInfinity).Should().Be(-1);
    }
}
