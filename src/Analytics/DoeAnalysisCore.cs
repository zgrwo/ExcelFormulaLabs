using System;
using System.Collections.Generic;
using System.Linq;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// DOE analysis: effect estimation, multi-factor ANOVA, and Pareto ranking for
    /// a coded design matrix (the factor columns produced by <see cref="DoeCore"/>).
    /// The heavy math (OLS fit, t/p values) is reused from <see cref="RegressionCore"/>
    /// — this layer only adds design-aware term expansion (main effects, interactions,
    /// quadratic terms) and the DOE-standard effect/ANOVA tables.
    /// </summary>
    internal static class DoeAnalysisCore
    {
        /// <summary>
        /// Effect table: one row per term — [Term, Coef, Effect, t, p].
        /// Effect = 2×Coef (the standard DOE effect estimate for ±1-coded factors).
        /// </summary>
        internal static object[,] Analyze(double[,] X, double[] y, int maxOrder, bool quadratic)
        {
            var (Xe, terms) = ExpandTerms(X, maxOrder, quadratic);
            var fit = RegressionCore.FitOLS(Xe, y);

            var coefs = (double[])fit["coefficients"]; // [0] = intercept
            var ts = (double[])fit["t_stats"];
            var ps = (double[])fit["p_values"];

            int nTerms = terms.Length;
            var result = new object[nTerms + 1, 5];
            result[0, 0] = "Term"; result[0, 1] = "Coef"; result[0, 2] = "Effect";
            result[0, 3] = "t"; result[0, 4] = "p";

            for (int j = 0; j < nTerms; j++)
            {
                double coef = coefs[j + 1];
                result[j + 1, 0] = terms[j];
                result[j + 1, 1] = coef;
                result[j + 1, 2] = 2.0 * coef;
                result[j + 1, 3] = ts[j + 1];
                result[j + 1, 4] = ps[j + 1];
            }
            return result;
        }

        /// <summary>
        /// Multi-factor ANOVA table: [Source, SS, df, MS, F, p] per term, then an
        /// Error row and a Total row. Uses Type-III-style sums of squares derived
        /// from the single-DF identity SS = MSE × t², F = t² (each term has 1 df).
        /// </summary>
        internal static object[,] Anova(double[,] X, double[] y, int maxOrder, bool quadratic)
        {
            var (Xe, terms) = ExpandTerms(X, maxOrder, quadratic);
            var fit = RegressionCore.FitOLS(Xe, y);

            var ts = (double[])fit["t_stats"];
            var ps = (double[])fit["p_values"];
            double sse = (double)fit["sse"];
            long dfError = (long)fit["df"]; // n - p (p = nTerms + 1)
            double mse = sse / dfError;

            int nTerms = terms.Length;
            var result = new object[nTerms + 3, 6];
            result[0, 0] = "Source"; result[0, 1] = "SS"; result[0, 2] = "df";
            result[0, 3] = "MS"; result[0, 4] = "F"; result[0, 5] = "p";

            for (int j = 0; j < nTerms; j++)
            {
                double t = ts[j + 1];
                // review 2026-09-05（N07）：原守卫只挡输入侧 t（NaN/Inf）——|t| ≤ 1e154 的
                // 有限大 t 仍可能使 mse·t² / t² 溢出 ±Inf 直漏。结果侧补非有限→NaN 封顶
                // （CapNaN，与 PhyChemCore 同一模块约定）。
                // 静态依据（无法经公共输入触发，封顶为纵深防御）：上游 FitOLSCore 已守卫
                // tss/sse 非有限；含截距时 x̃_j ⊥ 1 → SS_j = mse·t² ≤ tss（有限），且残差
                // 量化下限 sse ≳ (ULP·‖y‖)² 使 |t| ≲ √(n·df)/eps ≈ 1e16（实测 y=4e153·±1
                // + 1ULP 噪声 → F=3.15e31、SS=1.28e308 全部有限）——t 路径当前不可达 Inf，
                // 封顶仅防御上游守卫被移除/语义变更后的回归。
                double ssJ = double.IsNaN(t) || double.IsInfinity(t) ? double.NaN : CapNaN(mse * t * t);
                double fJ = double.IsNaN(t) || double.IsInfinity(t) ? double.NaN : CapNaN(t * t);
                result[j + 1, 0] = terms[j];
                result[j + 1, 1] = ssJ;
                result[j + 1, 2] = 1L;
                result[j + 1, 3] = ssJ; // MS = SS / 1
                result[j + 1, 4] = fJ;
                result[j + 1, 5] = ps[j + 1];
            }

            result[nTerms + 1, 0] = "Error";
            result[nTerms + 1, 1] = sse;
            result[nTerms + 1, 2] = dfError;
            result[nTerms + 1, 3] = mse;

            // Total SS = Σ(y - ȳ)² (the true total sum of squares; Type-III effect SS
            // are not additive, so this is computed directly rather than summed).
            double mean = y.Average();
            double tss = 0;
            for (int i = 0; i < y.Length; i++) { double d = y[i] - mean; tss += d * d; }
            result[nTerms + 2, 0] = "Total";
            result[nTerms + 2, 1] = tss;
            result[nTerms + 2, 2] = dfError + nTerms; // n - 1
            return result;
        }

        /// <summary>
        /// Pareto ranking: terms sorted by descending |effect| — [Term, Effect].
        /// </summary>
        internal static object[,] Pareto(double[,] X, double[] y, int maxOrder, bool quadratic)
        {
            var (Xe, terms) = ExpandTerms(X, maxOrder, quadratic);
            var fit = RegressionCore.FitOLS(Xe, y);
            var coefs = (double[])fit["coefficients"]; // [0] = intercept

            int nTerms = terms.Length;
            var order = Enumerable.Range(0, nTerms)
                .OrderByDescending(j => Math.Abs(coefs[j + 1]))
                .ToArray();

            var result = new object[nTerms + 1, 2];
            result[0, 0] = "Term"; result[0, 1] = "Effect";
            for (int r = 0; r < nTerms; r++)
            {
                int j = order[r];
                result[r + 1, 0] = terms[j];
                result[r + 1, 1] = 2.0 * coefs[j + 1];
            }
            return result;
        }

        /// <summary>
        /// Expand a coded design matrix into main effects, interactions (up to
        /// <paramref name="maxOrder"/>), and optionally quadratic terms. Returns the
        /// expanded matrix (without intercept — FitOLS adds it) and term names.
        /// </summary>
        private static (double[,] Xe, string[] terms) ExpandTerms(double[,] X, int maxOrder, bool quadratic)
        {
            int n = X.GetLength(0);
            int k = X.GetLength(1);
            if (k < 1)
                throw new ArgumentException(ErrorMsg.Get("DOE_NoFactors"));
            if (maxOrder < 1 || maxOrder > 3)
                throw new ArgumentException($"Interaction order must be 1, 2, or 3 (got {maxOrder}).");
            // review 2026-08-31（深度审查 P1-8）：展开项数无守卫——k=100、maxOrder=3 时
            // p=166,750，Xe（n=1000）即 1.33GB → 不可捕获 OOM → Excel 崩溃。
            // DOE.PLAN 允许 MaxFactors=1000，因此这条调用链合法。先算 p（long 防乘法溢出）
            // 再分配：超过 5,000 项（≈ n×p 数千万元素）直接拒绝。
            long termCount = k; // main effects
            if (maxOrder >= 2) termCount += (long)k * (k - 1) / 2;
            if (maxOrder >= 3) termCount += (long)k * (k - 1) * (k - 2) / 6;
            if (quadratic) termCount += k;
            if (termCount > 5000)
                throw new ArgumentException(
                    $"DOE analysis would expand to {termCount:N0} terms — exceeds the 5,000-term limit. " +
                    "Reduce factor count or interaction order.");
            // review 2026-09-04（reaudit B2）：原守卫只量 p（termCount），n 无上限——
            // k=45（2way p=1035 < 5000 放行）、n=200,000 时 Xe = 2.07e8 doubles ≈ 1.66 GB，
            // 且 Column() 先物化同尺寸 cols 副本 → 不可捕获 OOM → Excel 进程崩溃。
            // 补二维守卫 n·p ≤ 2e6（≈16 MB，含中间副本峰值 < 64 MB），放在任何列物化之前。
            if ((long)n * termCount > 2_000_000)
                throw new ArgumentException(
                    $"DOE analysis would expand to {n:N0} observations × {termCount:N0} terms " +
                    $"({(long)n * termCount:N0} cells) — exceeds the 2,000,000-cell limit. " +
                    "Reduce factor count, interaction order, or the number of observations.");

            var terms = new List<string>();
            var cols = new List<double[]>();

            // Main effects.
            for (int i = 0; i < k; i++)
            {
                terms.Add(DoeCore.ColumnName(i));
                cols.Add(Column(X, i, -1, -1, -1));
            }

            // 2-way interactions.
            if (maxOrder >= 2)
                for (int i = 0; i < k; i++)
                    for (int j = i + 1; j < k; j++)
                    {
                        terms.Add(DoeCore.ColumnName(i) + DoeCore.ColumnName(j));
                        cols.Add(Column(X, i, j, -1, -1));
                    }

            // 3-way interactions.
            if (maxOrder >= 3)
                for (int i = 0; i < k; i++)
                    for (int j = i + 1; j < k; j++)
                        for (int l = j + 1; l < k; l++)
                        {
                            terms.Add(DoeCore.ColumnName(i) + DoeCore.ColumnName(j) + DoeCore.ColumnName(l));
                            cols.Add(Column(X, i, j, l, -1));
                        }

            // Quadratic terms.
            if (quadratic)
                for (int i = 0; i < k; i++)
                {
                    terms.Add(DoeCore.ColumnName(i) + "^2");
                    cols.Add(Column(X, i, i, -1, -1));
                }

            int p = cols.Count;
            var Xe = new double[n, p];
            for (int c = 0; c < p; c++)
            {
                var col = cols[c];
                for (int r = 0; r < n; r++) Xe[r, c] = col[r];
            }
            return (Xe, terms.ToArray());
        }

        /// <summary>
        /// Build a term column from factor indices. A factor index of -1 means "not
        /// used". With two equal indices (i==j) the term is the square of one factor.
        /// </summary>
        private static double[] Column(double[,] X, int i, int j, int l, int m)
        {
            int n = X.GetLength(0);
            var col = new double[n];
            for (int r = 0; r < n; r++)
            {
                double v = 1.0;
                if (i >= 0) v *= X[r, i];
                if (j >= 0) v *= X[r, j];
                if (l >= 0) v *= X[r, l];
                if (m >= 0) v *= X[r, m];
                col[r] = v;
            }
            return col;
        }

        /// <summary>Non-finite result → NaN（模块约定：不向 Excel 泄漏 ±Inf）。</summary>
        private static double CapNaN(double v) => double.IsInfinity(v) ? double.NaN : v;
    }
}
