using System;
using System.Linq;
using MathNet.Numerics.Statistics;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
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

        internal static double HarmonicMean(double[] d)
        {
            if (d.Length == 0) return double.NaN;
            // P1-5: harmonic mean is undefined for negative input (scipy → nan, Excel → #NUM!).
            // MathNet returns +Inf for [-1,1] (2/0) and a meaningless value for [1,-2,3].
            for (int i = 0; i < d.Length; i++)
                if (d[i] < 0) return double.NaN;
            var r = Statistics.HarmonicMean(d);
            return double.IsInfinity(r) ? double.NaN : r;  // output cap (file convention)
        }

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
            d.Length < 3 ? double.NaN : Statistics.Skewness(d); // 无偏样本偏度（type 2，对应 Excel SKEW / scipy bias=False）

        internal static double Kurtosis(double[] d) =>
            d.Length < 4 ? double.NaN : Statistics.Kurtosis(d); // 无偏样本超额峰度（type 2，对应 Excel KURT / scipy fisher=True, bias=False）

        // review 2026-08-29：逐元素数学函数下沉（原 L1 守卫写在 UDF lambda，红线① UDF 仅分发）
        internal static double SqrtSafe(double x) => x < 0 ? double.NaN : Math.Sqrt(x);
        internal static double LogSafe(double x) => x <= 0 ? double.NaN : Math.Log(x);
        internal static double Log10Safe(double x) => x <= 0 ? double.NaN : Math.Log10(x);
        internal static double ExpSafe(double x) { var r = Math.Exp(x); return double.IsInfinity(r) ? double.NaN : r; }

        internal static double Min(double[] d) =>
            d.Length == 0 ? double.NaN : Statistics.Minimum(d);

        internal static double Max(double[] d) =>
            d.Length == 0 ? double.NaN : Statistics.Maximum(d);

        internal static double Range(double[] d)
        {
            if (d.Length == 0) return double.NaN;
            var r = Max(d) - Min(d);
            return double.IsInfinity(r) ? double.NaN : r; // overflow guard: extreme Max-Min → NaN
        }

        /// <summary>
        /// Sum of array elements. NaN/Inf input is guarded upstream by <see cref="AnalyticsHelpers.PrepV"/>
        /// (the mandatory gateway for all STATS UDFs), so this method can rely on clean input.
        /// Infinity result is capped to NaN to avoid propagating ±∞ into Excel cells.
        /// </summary>
        internal static double Sum(double[] d) { if (d.Length == 0) return 0.0; var r = d.Sum(); return double.IsInfinity(r) ? double.NaN : r; }
        /// <summary>
        /// Product of array elements. NaN/Inf input is guarded upstream by <see cref="AnalyticsHelpers.PrepV"/>.
        /// Infinity result is capped to NaN.
        /// </summary>
        internal static double Product(double[] d) { if (d.Length == 0) return 1.0; var r = d.Aggregate(1.0, (a, x) => a * x); return double.IsInfinity(r) ? double.NaN : r; }

        /// <summary>Sign of a numeric value. NaN → 0 (explicit guard; Math.Sign would throw for NaN).
        /// ±Infinity → ±1 (explicit guard; CLR behaviour made auditable per 防错原则1).</summary>
        internal static long Sign(double x) => double.IsNaN(x) ? 0 : double.IsPositiveInfinity(x) ? 1 : double.IsNegativeInfinity(x) ? -1 : Math.Sign(x);

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
            if (a.Length != b.Length || a.Length < 1) return double.NaN;
            if (a.Length == 1) return 0.0; // single-point population covariance is 0 (MathNet sample form would yield 0/0=NaN)
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
            d.Length == 0 || p < 0 || p > 100 || double.IsNaN(p)
                ? double.NaN
                : Statistics.QuantileCustom(d, p / 100.0, def ?? DefaultQuantileDefinition);

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
            NumericGuard.AgainstNonFinite(data);
            int rows = data.GetLength(0), cols = data.GetLength(1);
            var r = new double[cols, cols];
            // Sample correlation requires at least 2 observations.  With 0 or 1 rows,
            // every entry is undefined — fill NaN and return early (avoids 0/0 → NaN
            // which then fails the sds < 1e-15 check because NaN comparisons are always
            // false in IEEE 754, producing a spurious 1.0 on the diagonal).
            if (rows < 2)
            {
                for (int i = 0; i < cols; i++)
                    for (int j = 0; j < cols; j++)
                        r[i, j] = double.NaN;
                return r;
            }
            // Pre-compute column means and stddevs (one pass per column).
            var means = new double[cols]; var sds = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                double sum = 0;
                for (int i = 0; i < rows; i++) sum += data[i, j];
                means[j] = sum / rows;
                double ss = 0;
                for (int i = 0; i < rows; i++) { double d = data[i, j] - means[j]; ss += d * d; }
                sds[j] = Math.Sqrt(ss / (rows - 1));  // sample stddev
            }
            // Fill diagonal = 1.0 (or NaN for constant columns)
            for (int i = 0; i < cols; i++)
            {
                if (sds[i] < 1e-15) { for (int j = 0; j < cols; j++) r[i, j] = r[j, i] = double.NaN; }
                else r[i, i] = 1.0;
            }
            // Off-diagonal: Pearson r from pre-computed means/sds → O(cols²×rows)
            for (int i = 0; i < cols; i++)
            {
                if (double.IsNaN(r[i, i])) continue;
                for (int j = i + 1; j < cols; j++)
                {
                    if (double.IsNaN(r[j, j])) continue;
                    double cov = 0;
                    for (int k = 0; k < rows; k++)
                        cov += (data[k, i] - means[i]) * (data[k, j] - means[j]);
                    cov /= (rows - 1);
                    r[i, j] = r[j, i] = cov / (sds[i] * sds[j]);
                }
            }
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
            if (va + vb < 1e-15) return Math.Abs(ma - mb) < 1e-15 ? 1.0 : double.NaN;
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
            if (d.Length == 0) return Array.Empty<double>();
            double m = Statistics.Mean(d);
            double sd = Math.Sqrt(VarianceP(d));
            if (double.IsNaN(sd) || sd < 1e-15) throw new ArgumentException(ErrorMsg.Get("STATS_ZeroVariance"));
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

        /// <summary>Count of numeric elements (Excel COUNT semantics): values convertible to
        /// a finite double are counted; text/empty/error cells are skipped, never thrown.
        /// review-2026-08-29 P2-3：原实现经 PrepV（遇 NaN/Inf 抛异常），含文本区域返回 #VALUE!
        /// 而非文档所述的“元素个数”。</summary>
        internal static long CountNumeric(object data)
        {
            var raw = InputNormalizer.NormalizeTo1D(data);
            long n = 0;
            foreach (var x in raw)
            {
                double v = InputNormalizer.ToDouble(x);
                if (!double.IsNaN(v) && !double.IsInfinity(v)) n++;
            }
            return n;
        }
    }
}