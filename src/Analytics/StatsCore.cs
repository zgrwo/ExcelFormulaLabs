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
        // review 2026-09-05（R22）：均值累加对 1e308 级输入可溢出 ±Inf → NaN 封顶
        // （对齐 Sum/Range 的既有封顶约定）。
        internal static double Mean(double[] d)
        {
            if (d.Length == 0) return double.NaN;
            var r = Statistics.Mean(d);
            return double.IsInfinity(r) ? double.NaN : r;
        }

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

        // review 2026-09-05（R22）：方差两遍平方和对 |x| > 1e154 级输入溢出 ±Inf → NaN 封顶。
        // VAR/STDEV 溢出真值本不可表示，封顶后语义 = "不可表示"（对齐 Sum/Range 封顶约定）。
        internal static double VarianceP(double[] d)
        {
            if (d.Length < 1) return double.NaN;
            var r = Statistics.PopulationVariance(d);
            return double.IsInfinity(r) ? double.NaN : r;
        }

        internal static double Variance(double[] d)
        {
            if (d.Length < 2) return double.NaN;
            var r = Statistics.Variance(d);
            return double.IsInfinity(r) ? double.NaN : r;
        }

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
        /// review 2026-08-31（深度审查 P2-11）：朴素左折叠顺序依赖——Product(1e300,1e300,1e-300) →
        /// 1e300×1e300=1e600 溢出 → Inf → NaN（真值 1e300 可表示）。按 |x| 升序相乘，先消掉
        /// 小量避免中间溢出（极端下溢场景如 1e-320² 仍可能，属 double 极限，接受）。
        /// </summary>
        internal static double Product(double[] d)
        {
            if (d.Length == 0) return 1.0;
            double r = 1.0;
            foreach (double x in d.OrderBy(v => Math.Abs(v)))
            {
                r *= x;
                if (double.IsInfinity(r)) return double.NaN;
            }
            return r;
        }

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

        // review 2026-09-05（R22）：协方差两遍平方和对 |x| > 1e154 级输入溢出 ±Inf → NaN 封顶
        // （对齐 Sum/Range 封顶约定）。
        internal static double CovarianceP(double[] a, double[] b)
        {
            if (a.Length != b.Length || a.Length < 1) return double.NaN;
            if (a.Length == 1) return 0.0; // single-point population covariance is 0 (MathNet sample form would yield 0/0=NaN)
            var r = Statistics.Covariance(a, b) * (a.Length - 1) / a.Length;
            return double.IsInfinity(r) ? double.NaN : r;
        }

        internal static double Covariance(double[] a, double[] b)
        {
            if (a.Length != b.Length || a.Length < 2) return double.NaN;
            var r = Statistics.Covariance(a, b);
            return double.IsInfinity(r) ? double.NaN : r;
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

        // review 2026-09-05（R22）：MathNet 两遍平方和（Σx²、Σxy）在序列量纲 > 1e154 时
        // 溢出 → 尺度不变量输出 NaN/Inf（真值有限）。相关系数对每条序列的正缩放不变 →
        // 先按 1/maxAbs 预缩放（逐序列，不改变 r）；maxAbs==0（全零序列）保持既有 NaN 路径。
        internal static double Pearson(double[] a, double[] b)
        {
            if (a.Length != b.Length || a.Length < 2) return double.NaN;
            return Correlation.Pearson(PreScaled(a), PreScaled(b));
        }

        /// <summary>R22 复制型预缩放：v/maxAbs（不改输入数组）；全零序列原样返回。</summary>
        private static double[] PreScaled(double[] v)
        {
            double max = 0;
            foreach (double x in v) max = Math.Max(max, Math.Abs(x));
            if (max == 0) return v;
            var r = new double[v.Length];
            for (int i = 0; i < v.Length; i++) r[i] = v[i] / max;
            return r;
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
            // review 2026-09-05（R22）：均值/协方差两遍平方和在列量纲 > 1e154 时溢出 →
            // 尺度不变量输出 NaN/Inf。相关系数对列的正缩放不变 → 先按各列 1/maxAbs 预缩放
            // （写入新矩阵，不改输入）。maxAbs==0（全零列）保持原值 → 既有常量列 NaN 路径
            // 不变；常量列 c>0 缩放后各元素 = c/c = 1.0（IEEE 精确）→ sd=0 精确 → 常量分支
            // 结果与修复前逐位一致（verify-manual CORRMATRIX_CONST 用 tol=0）。
            var scaled = new double[rows, cols];
            for (int j = 0; j < cols; j++)
            {
                double max = 0;
                for (int i = 0; i < rows; i++) max = Math.Max(max, Math.Abs(data[i, j]));
                for (int i = 0; i < rows; i++) scaled[i, j] = max > 0 ? data[i, j] / max : data[i, j];
            }
            data = scaled;
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
            // review 2026-08-31（深度审查 P1-4）：原 `sds[i] < 1e-15` 绝对阈值在两个方向都错——
            // ① 溢出/NaN 路径：sd=Inf 或 NaN 时 `Inf < 1e-15`/`NaN < 1e-15` 恒为 false → 走 else 分支
            //    产生"对角线 1.0、非对角 NaN"的自相矛盾矩阵（{1e308,1} 列实测 r[0,0]=1 而 r[0,1]=NaN）；
            // ② 小量纲路径：相关系数是尺度不变量，列数据在 1e-16 量级时 sd 恒 < 1e-15 → 整行被误判常量。
            // 判据改为"非有限或非正"（NaN/Inf/0 都进常量分支）。
            for (int i = 0; i < cols; i++)
            {
                if (double.IsNaN(sds[i]) || double.IsInfinity(sds[i]) || sds[i] <= 0)
                { for (int j = 0; j < cols; j++) r[i, j] = r[j, i] = double.NaN; }
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
            // review 2026-08-31（深度审查 P1-5）：原 `va < 1e-15` 绝对阈值把 1e-9 量纲样本
            // （va~1e-18）误判为常量 → 返回 NaN（真值 t=0.866, p=0.478）。t 检验是尺度不变量，
            // 常量判据应为精确零方差；NaN/Inf 防御性返回 NaN（哨兵）。
            if (double.IsNaN(va) || double.IsInfinity(va)) return double.NaN;
            if (va == 0)
            {
                // Zero variance: all values equal. If the mean is exactly mu0, no
                // evidence against H0 → p=1.0; otherwise undefined → NaN.
                // review 2026-09-05（R03）：原 `Abs(mean−mu0) < 1e-15` 是绝对阈值——
                // 小量纲数据（{1e-16}×4 vs mu0=2e-16，均值差 1e-16）被误判"均值相等"
                // → 错误 p=1。t 检验零方差分支是尺度不变判据，改为精确相等（跨量纲一致）。
                // Mirrors TTestTwoSample zero-variance guard (M4 fix).
                return Statistics.Mean(d) == mu0 ? 1.0 : double.NaN;
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
            // review 2026-08-31（深度审查 P1-5）：绝对阈值 → 精确零判据（同 TTestOneSample）。
            double vab = va + vb;
            if (double.IsNaN(vab) || double.IsInfinity(vab)) return double.NaN;
            // review 2026-09-05（R03）：零方差分支均值判据改精确相等（同 TTestOneSample）——
            // 原 `Abs(ma−mb) < 1e-15` 绝对阈值把小量纲均值差（如 {1e-16}×4 vs {2e-16}×4，
            // 差 1e-16）误判"相等"→ 错误 p=1；{1}×4 vs {2}×4（差 1）反而 NaN，量纲不一致。
            if (vab == 0) return ma == mb ? 1.0 : double.NaN;
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
            // review 2026-08-31（深度审查 P1-5）：绝对阈值 → 精确零判据（小量纲数据 sd 与数据同尺度）。
            if (double.IsNaN(sd) || double.IsInfinity(sd) || sd == 0) throw new ArgumentException(ErrorMsg.Get("STATS_ZeroVariance"));
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