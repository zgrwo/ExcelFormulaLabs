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
            d.Length < 1 ? double.NaN : Math.Sqrt(Statistics.PopulationVariance(d));

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
        internal static QuantileDefinition DefaultQuantileDefinition = QuantileDefinition.R7;

        /// <summary>Descriptive summary: [n, mean, stdev, min, q1, median, q3, max, iqr].
        /// Respects <see cref="DefaultQuantileDefinition"/>.</summary>
        internal static double[] Summary(double[] d, QuantileDefinition? def = null)
        {
            if (d.Length == 0) return Array.Empty<double>();
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

        internal static double Pearson(double[] a, double[] b) =>
            Correlation.Pearson(a, b);

        internal static double Spearman(double[] a, double[] b) =>
            Correlation.Spearman(a, b);

        internal static double[,] CorrelationMatrix(double[,] data)
        {
            int cols = data.GetLength(1);
            var r = new double[cols, cols];
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < cols; j++)
                    r[i, j] = Pearson(ExtractColumn(data, i), ExtractColumn(data, j));
            return r;
        }

        internal static double TTestOneSample(double[] d, double mu0 = 0)
        {
            if (d.Length < 2) return double.NaN;
            double se = Math.Sqrt(Variance(d)) / Math.Sqrt(d.Length);
            double t = (Statistics.Mean(d) - mu0) / se;
            return TStatPValue(Math.Abs(t), d.Length - 1);
        }

        internal static double TTestTwoSample(double[] a, double[] b)
        {
            if (a.Length < 2 || b.Length < 2) return double.NaN;
            double ma = Statistics.Mean(a), mb = Statistics.Mean(b);
            double va = Variance(a), vb = Variance(b);
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
            return d.Select(x => (x - m) / sd).ToArray();
        }

        /// <summary>Two-tailed p-value from t-statistic using Beta regularised.</summary>
        private static double TStatPValue(double t, double df)
        {
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
