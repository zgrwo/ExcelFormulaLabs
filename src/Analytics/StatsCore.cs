using System;
using System.Linq;
using MathNet.Numerics.Statistics;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
{
    /// <summary>
    /// Descriptive statistics, inference, distributions, and correlation.
    /// Ported from StatsUtils.bas. All methods are <c>internal static</c>.
    /// UDF wrappers in StatsUdf.cs call these via <see cref="ElementWiseMapper"/>.
    /// </summary>
    internal static class StatsCore
    {
        internal static double Mean(double[] d) =>
            d.Length == 0 ? double.NaN : Statistics.Mean(d);

        internal static double GeometricMean(double[] d) =>
            d.Length == 0 ? double.NaN : Statistics.GeometricMean(d);

        internal static double HarmonicMean(double[] d) =>
            d.Length == 0 ? double.NaN : Statistics.HarmonicMean(d);

        internal static double Median(double[] d) =>
            d.Length == 0 ? double.NaN : Statistics.Median(d);

        internal static double VarianceP(double[] d) =>
            d.Length < 1 ? double.NaN : Statistics.PopulationVariance(d);

        internal static double Variance(double[] d) =>
            d.Length < 2 ? double.NaN : Statistics.Variance(d);

        internal static double StdevP(double[] d) =>
            d.Length < 1 ? double.NaN : Math.Sqrt(VarianceP(d));

        internal static double Stdev(double[] d) =>
            d.Length < 2 ? double.NaN : Math.Sqrt(Variance(d));

        internal static double Skewness(double[] d) =>
            d.Length < 3 ? double.NaN : Statistics.Skewness(d);

        internal static double Kurtosis(double[] d) =>
            d.Length < 4 ? double.NaN : Statistics.Kurtosis(d);

        internal static double Min(double[] d) =>
            d.Length == 0 ? double.NaN : Statistics.Minimum(d);

        internal static double Max(double[] d) =>
            d.Length == 0 ? double.NaN : Statistics.Maximum(d);

        internal static double Range(double[] d) =>
            d.Length == 0 ? double.NaN : Max(d) - Min(d);

        internal static double Sum(double[] d) { var r = d.Sum(); return double.IsInfinity(r) ? double.NaN : r; }
        internal static double Product(double[] d) { if (d.Length == 0) return 0.0; var r = d.Aggregate(1.0, (a, x) => a * x); return double.IsInfinity(r) ? double.NaN : r; }

        /// <summary>Sign of a numeric value. NaN → 0 (explicit guard; Math.Sign would throw for NaN).</summary>
        internal static long Sign(double x) => double.IsNaN(x) ? 0 : Math.Sign(x);

        /// <summary>Most frequent value. Single-pass O(n) with Dictionary.
        /// Returns NaN for empty input or when all values are unique (matches Excel MODE #N/A).
        /// On ties, returns the smallest value (matches scipy.stats.mode and Excel MODE.SNGL).</summary>
        internal static double Mode(double[] d)
        {
            if (d.Length == 0) return double.NaN;
            var counts = new System.Collections.Generic.Dictionary<double, int>();
            foreach (double x in d)
                counts[x] = counts.TryGetValue(x, out int c) ? c + 1 : 1;
            int maxCount = 0; double mode = double.NaN;
            foreach (var kv in counts)
            {
                if (kv.Value > maxCount) { maxCount = kv.Value; mode = kv.Key; }
                else if (kv.Value == maxCount && kv.Key < mode) { mode = kv.Key; }
            }
            if (maxCount == 1 && d.Length > 1) return double.NaN;  // all unique → no mode
            return mode;
        }

        internal static double CovarianceP(double[] a, double[] b)
        {
            if (a.Length != b.Length || a.Length == 0) return double.NaN;
            return Statistics.Covariance(a, b) * (a.Length - 1) / a.Length;
        }

        internal static double Covariance(double[] a, double[] b)
        {
            if (a.Length != b.Length || a.Length < 2) return double.NaN;
            return Statistics.Covariance(a, b);
        }

        /// <summary>
        /// Default quantile definition for Percentile, IQR, and Summary.
        /// R7 matches Python numpy/scipy default ('linear' interpolation).
        /// Change to R8 (MathNet/Median), R6 (SPSS), R5 (Hydrology), etc. via QuantileDefinition enum.
        /// </summary>
        internal static readonly QuantileDefinition DefaultQuantileDefinition = QuantileDefinition.R7;

        /// <summary>Descriptive summary: [n, mean, stdev, min, q1, median, q3, max, iqr].
        /// Respects <see cref="DefaultQuantileDefinition"/>.</summary>
        internal static double[] Summary(double[] d, QuantileDefinition? def = null)
        {
            if (d.Length == 0) return Array.Empty<double>();
            if (d.Length == 1) return new[] { 1.0, d[0], double.NaN, d[0], d[0], d[0], d[0], d[0], 0.0 };
            var qd = def ?? DefaultQuantileDefinition;
            double q1 = Statistics.QuantileCustom(d, 0.25, qd);
            double q3 = Statistics.QuantileCustom(d, 0.75, qd);
            return new[] { (double)d.Length, Statistics.Mean(d), Math.Sqrt(Variance(d)),
                Statistics.Minimum(d), q1, Statistics.Median(d), q3,
                Statistics.Maximum(d), q3 - q1 };
        }

        /// <summary>Percentile using configurable quantile definition.
        /// Default <see cref="DefaultQuantileDefinition"/> (R7) matches Python numpy/scipy 'linear'.</summary>
        internal static double Percentile(double[] d, double p, QuantileDefinition? def = null) =>
            d.Length == 0 ? double.NaN : Statistics.QuantileCustom(d, p / 100.0, def ?? DefaultQuantileDefinition);

        /// <summary>Inter-quartile range using configurable quantile definition.
        /// Default <see cref="DefaultQuantileDefinition"/> (R7) matches Python scipy.stats.iqr.</summary>
        internal static double IQR(double[] d, QuantileDefinition? def = null)
        {
            if (d.Length == 0) return double.NaN;
            var qd = def ?? DefaultQuantileDefinition;
            return Statistics.QuantileCustom(d, 0.75, qd) - Statistics.QuantileCustom(d, 0.25, qd);
        }

        internal static double Pearson(double[] a, double[] b)
        {
            if (a.Length != b.Length || a.Length < 2) return double.NaN;
            return Correlation.Pearson(a, b);
        }

        internal static double Spearman(double[] a, double[] b)
        {
            if (a.Length != b.Length || a.Length < 2) return double.NaN;
            return Correlation.Spearman(a, b);
        }

        internal static double[,] CorrelationMatrix(double[,] data)
        {
            int cols = data.GetLength(1);
            var r = new double[cols, cols];
            var colCache = new double[cols][];
            for (int i = 0; i < cols; i++) colCache[i] = ExtractColumn(data, i);
            for (int i = 0; i < cols; i++)
                for (int j = i; j < cols; j++)
                    r[i, j] = r[j, i] = Pearson(colCache[i], colCache[j]);
            return r;
        }

        internal static double TTestOneSample(double[] d, double mu0 = 0)
        {
            if (d.Length < 2) return double.NaN;
            double va = Variance(d);
            if (va < 1e-15)
            {
                // Zero variance: all values equal. If mean ≈ mu0, no evidence
                // against H0 → p=1.0; otherwise undefined → NaN.
                // Mirrors TTestTwoSample zero-variance guard (M4 fix).
                return Math.Abs(Statistics.Mean(d) - mu0) < 1e-15 ? 1.0 : double.NaN;
            }
            double se = Math.Sqrt(va) / Math.Sqrt(d.Length);
            double t = (Statistics.Mean(d) - mu0) / se;
            return TStatPValue(Math.Abs(t), d.Length - 1);
        }

        internal static double TTestTwoSample(double[] a, double[] b)
        {
            if (a.Length < 2 || b.Length < 2) return double.NaN;
            double ma = Statistics.Mean(a), mb = Statistics.Mean(b);
            double va = Variance(a), vb = Variance(b);
            if (va + vb < 1e-15) return ma == mb ? 1.0 : double.NaN;
            double se = Math.Sqrt(va / a.Length + vb / b.Length);
            double t = (ma - mb) / se;
            double num = (va / a.Length + vb / b.Length);
            num *= num;
            double den = (va / a.Length) * (va / a.Length) / (a.Length - 1)
                       + (vb / b.Length) * (vb / b.Length) / (b.Length - 1);
            return TStatPValue(Math.Abs(t), num / den);
        }

        internal static double[] ZScore(double[] d)
        {
            double m = Statistics.Mean(d);
            double sd = Math.Sqrt(Variance(d));
            if (sd < 1e-15) throw new ArgumentException("Cannot compute z-scores for constant data (zero variance).");
            return d.Select(x => (x - m) / sd).ToArray();
        }

        /// <summary>Two-tailed p-value from t-statistic using Beta regularised.
        /// Returns NaN for degenerate inputs (df ≤ 0, NaN, ±∞) as a defence-in-depth
        /// measure — current callers already guard these, but future callers may not.</summary>
        internal static double TStatPValue(double t, double df)
        {
            if (df <= 0 || double.IsNaN(t) || double.IsNaN(df) || double.IsInfinity(t) || double.IsInfinity(df))
                return double.NaN;
            double x = df / (df + t * t);
            return MathNet.Numerics.SpecialFunctions.BetaRegularized(df / 2.0, 0.5, x);
        }

        private static double[] ExtractColumn(double[,] data, int col)
        {
            int rows = data.GetLength(0);
            var r = new double[rows];
            for (int i = 0; i < rows; i++) r[i] = data[i, col];
            return r;
        }
    }
}
