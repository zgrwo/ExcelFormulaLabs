using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// Regression: OLS, WLS, Ridge, ANOVA, factor importance.
    /// Ported from RegressUtils.bas. Backed by MathNet.Numerics.
    /// </summary>
    internal static class RegressionCore
    {
        /// <summary>
        /// Ordinary Least Squares regression. Minimizes sum of squared residuals.
        /// Used by REGRESS.OLS.
        /// </summary>
        /// <param name="X">Design matrix (n observations × p predictors).</param>
        /// <param name="y">Response vector (length n).</param>
        /// <param name="addIntercept">If true (default), prepends a column of 1s to X
        /// so the model includes an intercept (y = b0 + b1*X1 + ...).
        /// Set false when design matrix already contains an intercept column.</param>
        internal static Dictionary<string, object> FitOLS(double[,] X, double[] y, bool addIntercept = true)
        {
            NumericGuard.AgainstNonFinite(X, y);
            int n = X.GetLength(0), origP = X.GetLength(1);
            if (n == 0 || y.Length == 0)
                throw new ArgumentException(
                    "Input data is empty. Regression requires at least one observation.");
            if (y.Length != n)
                throw new ArgumentException(
                    $"Y length ({y.Length}) must equal X row count ({n}).");
            int p; double[,] Xaug;
            if (addIntercept) { p = origP + 1; Xaug = PrependIntercept(X); }
            else { p = origP; Xaug = X; }
            var matX = Matrix<double>.Build.DenseOfArray(Xaug);
            var vecY = Vector<double>.Build.Dense(y);
            return FitOLSCore(matX, vecY, n, p);
        }

        /// <summary>
        /// Shared OLS solver called by FitOLS (from double[,]) and FitWLS
        /// (from pre-weighted MathNet matrices). Avoids a double allocation
        /// in the WLS path: FitWLS builds MathNet matrices directly instead of
        /// going through an intermediate managed array.
        /// </summary>
        private static Dictionary<string, object> FitOLSCore(
            Matrix<double> matX, Vector<double> vecY, int n, int p)
        {
            // df 检查提前到 QR 之前：Thin QR 需要 n ≥ p（MathNet 对宽矩阵抛 NotSupportedException）。
            int df = n - p;
            if (df <= 0)
                throw new ArgumentException(
                    $"Cannot compute standard errors: degrees of freedom is {df} (n={n}, p={p}). Need n > p.");
            // review 2026-08-31（深度审查 P0-1）：正规方程 X'X 把设计矩阵条件数**平方**，
            // cond(X) > 1e8 时在 double 精度下静默返回错误系数（Hilbert 12×8 实测 ‖β−βtrue‖≈10.9
            // 而 r²=1.000000000000，报表看似完美）。改为 Thin QR 求解——QR 是后向稳定分解，
            // 精度只随 cond(X)（非 cond(X)²）退化；(X'X)⁻¹ 对角线由 R⁻¹ 求（R 良态，避免二次平方）。
            QR<double> qr;
            Vector<double> beta;
            try
            {
                qr = matX.QR(QRMethod.Thin);
                beta = qr.Solve(vecY);
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            { throw new ArgumentException(ErrorMsg.Get("REGRESS_RankDeficient"), ex); }

            var fitted = matX * beta;
            var residuals = vecY - fitted;
            double sse = residuals.DotProduct(residuals);
            // review 2026-08-29：TSS 改单遍中心化形式 Σ(y−ȳ)²——原两遍公式 y'y−(Σy)²/n 在
            // 大均值 y（量级/散布比 ≥1e12）时灾难性抵消，R² 静默错误（DoeAnalysisCore 已用稳定形式）。
            double yMean = vecY.Sum() / n;
            double tss = 0;
            for (int i = 0; i < n; i++) { double d = vecY[i] - yMean; tss += d * d; }
            if (double.IsNaN(tss) || double.IsInfinity(tss))
                throw new ArgumentException(
                    "Cannot fit OLS: total sum of squares is numerically unstable " +
                    "(response values too large for double precision).");
            if (tss == 0)  // review-2026-08-31（max-level 全量审查）：原 Math.Abs(tss) < 1e-15 绝对阈值把 1e-9 量纲 y 的 tss=2e-18 误判为常量响应抛错——P1-5 修复遗漏。TSS 是平方和（非负），真常量时精确为 0
                throw new ArgumentException(
                    "Cannot fit OLS: total sum of squares is zero (constant response variable y).");
            double r2 = 1.0 - sse / tss;
            double adjR2 = 1.0 - (1.0 - r2) * (n - 1) / (double)df;
            double sigma2 = sse / df;
            // (X'X)⁻¹ = R⁻¹ R⁻ᵀ（X = QR ⇒ X'X = R'R）。对角线 = Σ_k R⁻¹[j,k]²。
            // R 对角线相对守卫：|R[j,j]| ≤ eps·maxDiag 即数值秩亏（共线列），显式抛错
            // 而非静默返回 NaN 标准误。阈值取机器精度级（不误伤多项式趋势等高 cond 但可解输入）。
            var R = qr.R;
            double maxDiag = 0.0;
            for (int j = 0; j < p; j++)
            {
                double d = Math.Abs(R[j, j]);
                if (d > maxDiag) maxDiag = d;
            }
            double diagTol = Math.Max(maxDiag, 1e-300) * 2.220446049250313e-16;
            for (int j = 0; j < p; j++)
                if (Math.Abs(R[j, j]) <= diagTol)
                    throw new ArgumentException(
                        "Cannot fit OLS: design matrix X is near-singular (highly collinear columns). " +
                        "Consider removing redundant predictors or using ridge regression (REGRESS.RIDGE).");
            var Rinv = R.Inverse();
            var xtxInvDiag = new double[p];
            for (int j = 0; j < p; j++)
            {
                double s = 0.0;
                for (int k = j; k < p; k++) { double v = Rinv[j, k]; s += v * v; }
                xtxInvDiag[j] = s;
            }
            // P1-6: defence-in-depth — residual squares can still overflow for extreme
            // y values even when X is stable (guard placed after the near-singular
            // check so the more specific X diagnosis wins).
            if (double.IsNaN(sse) || double.IsInfinity(sse))
                throw new ArgumentException(
                    "Cannot fit OLS: residual sum of squares is numerically unstable " +
                    "(response values too large for double precision).");
            var se = new double[p];
            var tStat = new double[p];
            var pVal = new double[p];
            for (int j = 0; j < p; j++)
            {
                // Clamp tiny negative diagonal values from numerical noise before sqrt —
                // otherwise NaN would silently leak into t_stats/p_values.
                double varJ = sigma2 * xtxInvDiag[j];
                se[j] = Math.Sqrt(varJ > 0.0 ? varJ : 0.0);
                tStat[j] = beta[j] / se[j];
                // L4 output sentinel: se==0 (perfect fit / degenerate diagonal) yields
                // ±Inf or 0/0 — normalise to NaN so no non-finite value leaks out.
                if (double.IsNaN(tStat[j]) || double.IsInfinity(tStat[j])) tStat[j] = double.NaN;
                pVal[j] = StatsCore.TStatPValue(Math.Abs(tStat[j]), df);
            }

            return new Dictionary<string, object>
            {
                ["coefficients"] = beta.ToArray(),
                ["sse"] = sse, ["r_squared"] = r2, ["adj_r_squared"] = adjR2,
                ["residuals"] = residuals.ToArray(), ["fitted_values"] = fitted.ToArray(),
                ["standard_errors"] = se, ["t_stats"] = tStat, ["p_values"] = pVal,
                ["n"] = (long)n, ["df"] = (long)df,
            };
        }

        /// <summary>
        /// Weighted Least Squares regression. Minimises Σ wᵢ(yᵢ - xᵢβ)².
        /// Computes coefficients via sqrt(w)-transformed OLS (standard approach),
        /// then reports residuals and fitted values on the original (unweighted)
        /// scale so they are directly comparable to the input y.
        /// Used by REGRESS.WLS.
        /// </summary>
        /// <param name="X">Design matrix (n observations × p predictors).</param>
        /// <param name="y">Response vector (length n).</param>
        /// <param name="w">Weight vector (length n); must be non-negative.</param>
        /// <param name="addIntercept">If true (default), prepends a column of 1s
        /// before applying weights. The intercept column is weighted along with
        /// the data columns, producing correct WLS estimates.</param>
        internal static Dictionary<string, object> FitWLS(double[,] X, double[] y, double[] w, bool addIntercept = true)
        {
            NumericGuard.AgainstNonFinite(X, y);
            int n = X.GetLength(0), origP = X.GetLength(1);
            int p; double[,] Xaug;
            if (addIntercept) { p = origP + 1; Xaug = PrependIntercept(X); }
            else { p = origP; Xaug = X; }
            // Dimension validation
            if (y.Length != n)
                throw new ArgumentException(ErrorMsg.Get("REGRESS_YLengthMismatch", y.Length, n));
            if (w.Length != n)
                throw new ArgumentException(ErrorMsg.Get("REGRESS_WeightLengthMismatch", w.Length, n));
            // Reject negative/NaN/Infinity weights
            for (int i = 0; i < w.Length; i++)
                if (w[i] < 0 || double.IsNaN(w[i]) || double.IsInfinity(w[i]))
                    throw new ArgumentException(ErrorMsg.Get("REGRESS_InvalidWeight", i, w[i]));
            var matXw = Matrix<double>.Build.Dense(n, p);
            var vecYw = Vector<double>.Build.Dense(n);
            for (int i = 0; i < n; i++)
            {
                double sw = Math.Sqrt(w[i]);
                for (int j = 0; j < p; j++) matXw[i, j] = Xaug[i, j] * sw;
                vecYw[i] = y[i] * sw;
            }
            var result = FitOLSCore(matXw, vecYw, n, p); // Xw already has intercept column
            // Override residuals and fitted_values to ORIGINAL scale
            var beta = (double[])result["coefficients"];
            double[] fittedOrig = new double[n];
            double[] residualsOrig = new double[n];
            for (int i = 0; i < n; i++)
            {
                double fit = 0;
                for (int j = 0; j < p; j++) fit += Xaug[i, j] * beta[j];
                fittedOrig[i] = fit;
                residualsOrig[i] = y[i] - fit;
            }
            result["fitted_values"] = fittedOrig;
            result["residuals"] = residualsOrig;
            // Recompute SSE, TSS and R² in ORIGINAL scale to match residuals/fitted_values.
            // The SSE/R² returned by FitOLSCore are in the sqrt(w)-transformed scale
            // and are not comparable with the original-scale residuals.
            double sseOrig = 0, tssOrig = 0;
            double yMean = y.Sum() / n;
            for (int i = 0; i < n; i++)
            {
                sseOrig += residualsOrig[i] * residualsOrig[i];
                double dev = y[i] - yMean;
                tssOrig += dev * dev;
            }
            if (tssOrig == 0)  // review-2026-08-31：同上（WLS 原尺度）
                throw new ArgumentException(
                    "Cannot fit WLS: total sum of squares is zero (constant response variable y).");
            // review 2026-08-29：sseOrig/tssOrig 平方溢出（y≈1e154 + 小权重）可致 r2Orig=1−Inf/Inf=NaN
            // 静默泄漏（FitOLSCore/FitRidge 已守卫同场景）。
            if (double.IsNaN(sseOrig) || double.IsInfinity(sseOrig) ||
                double.IsNaN(tssOrig) || double.IsInfinity(tssOrig))
                throw new ArgumentException(
                    "Cannot fit WLS: residual/total sum of squares is numerically unstable " +
                    "(response values too large for double precision).");
            double r2Orig = 1.0 - sseOrig / tssOrig;
            result["sse"] = sseOrig;
            result["r_squared"] = r2Orig;
            result["adj_r_squared"] = 1.0 - (1.0 - r2Orig) * (n - 1) / (double)(n - p);
            return result;
        }

        /// <summary>
        /// Ridge regression with L2 regularization. Adds lambda*I to X'X before solving.
        /// Shrinks coefficients to reduce overfitting. No standard errors or p-values
        /// (inferential statistics are not valid under regularization).
        /// Used by REGRESS.RIDGE.
        /// </summary>
        /// <param name="X">Design matrix (n observations × p predictors).</param>
        /// <param name="y">Response vector (length n).</param>
        /// <param name="lambda">L2 regularisation strength (must be finite, ≥ 0).</param>
        /// <param name="addIntercept">If true (default), prepends a column of 1s and
        /// sets I[0,0]=0 so the intercept is not penalised by the L2 regularisation.</param>
        internal static Dictionary<string, object> FitRidge(double[,] X, double[] y, double lambda = 1.0, bool addIntercept = true)
        {
            NumericGuard.AgainstNonFinite(X, y);
            if (double.IsNaN(lambda) || double.IsInfinity(lambda))
                throw new ArgumentException(ErrorMsg.Get("REGRESS_LambdaNotFinite", lambda));
            // P2 (pre-release review): negative lambda makes XtX+λI non-positive-definite
            // and silently returns wrong coefficients; documented contract is lambda >= 0.
            if (lambda < 0)
                throw new ArgumentException(
                    $"Cannot fit Ridge: lambda must be non-negative (got {lambda}).");
            int n = X.GetLength(0), origP = X.GetLength(1);
            int p; double[,] Xaug;
            if (addIntercept) { p = origP + 1; Xaug = PrependIntercept(X); }
            else { p = origP; Xaug = X; }
            var matX = Matrix<double>.Build.DenseOfArray(Xaug);
            var vecY = Vector<double>.Build.Dense(y);
            var XtX = matX.TransposeThisAndMultiply(matX);
            var Xty = matX.TransposeThisAndMultiply(vecY);
            // Identity penalty matrix: don't penalise the intercept term
            var I = Matrix<double>.Build.DenseIdentity(p);
            if (addIntercept) I[0, 0] = 0.0;
            var ridge = XtX + I * lambda;
            var beta = ridge.Solve(Xty);
            // Guard against near-singular XtX with insufficient lambda:
            // ridge matrix XtX+λI is mathematically positive-definite for λ>0, but when
            // λ is subnormal relative to the data scale (e.g. λ=1e-310 with X entries ~1e20),
            // the diagonal addition is numerically zero and Solve may produce degenerate results.
            for (int j = 0; j < p; j++)
                if (double.IsNaN(beta[j]) || double.IsInfinity(beta[j]))
                    throw new ArgumentException(
                        "Cannot fit Ridge: coefficients contain NaN/Infinity. " +
                        "The design matrix may be near-singular and lambda is too small to regularize effectively. " +
                        "Try increasing lambda (e.g. lambda=10 or larger).");
            var fitted = matX * beta;
            var residuals = vecY - fitted;
            double sse = residuals.DotProduct(residuals);
            // review 2026-08-29：TSS 改单遍中心化形式（同 FitOLSCore，防灾难性抵消）
            double yMean = vecY.Sum() / n;
            double tss = 0;
            for (int i = 0; i < n; i++) { double d = vecY[i] - yMean; tss += d * d; }
            // P1-6: same numerical-stability guard as FitOLSCore (Inf−Inf=NaN silent leak).
            if (double.IsNaN(sse) || double.IsInfinity(sse))
                throw new ArgumentException(
                    "Cannot fit Ridge: residual sum of squares is numerically unstable " +
                    "(response values too large for double precision).");
            if (double.IsNaN(tss) || double.IsInfinity(tss))
                throw new ArgumentException(
                    "Cannot fit Ridge: total sum of squares is numerically unstable " +
                    "(response values too large for double precision).");
            if (tss == 0)  // review-2026-08-31（max-level 全量审查）：原 Math.Abs(tss) < 1e-15 绝对阈值把 1e-9 量纲 y 的 tss=2e-18 误判为常量响应抛错——P1-5 修复遗漏。TSS 是平方和（非负），真常量时精确为 0
                throw new ArgumentException(
                    "Cannot fit Ridge: total sum of squares is zero (constant response variable y).");

            return new Dictionary<string, object>
            {
                ["coefficients"] = beta.ToArray(),
                ["sse"] = sse,
                ["r_squared"] = 1.0 - sse / tss,
                ["residuals"] = residuals.ToArray(),
                ["fitted_values"] = fitted.ToArray(),
                ["lambda"] = lambda,
                ["n"] = (long)n, ["df"] = (long)p,
            };
        }

        /// <summary>
        /// One-way Analysis of Variance. Tests whether group means differ significantly.
        /// Groups passed as a jagged array (one array per group column).
        /// Used by REGRESS.ANOVA1.
        /// </summary>
        /// <returns>
        /// Dictionary: ss_between, ss_within, ss_total, df_between, df_within,
        /// df_total, ms_between, ms_within, f_stat, p_value, group_means, group_counts.
        /// p&lt;0.05 = at least one group mean differs significantly from the others.
        /// </returns>
        internal static Dictionary<string, object> AnovaOneWay(double[][] groups)
        {
            int k = groups.Length;
            if (k < 2)
                throw new ArgumentException(
                    "ANOVA requires at least 2 groups.");
            // Reject NaN/Inf in group data (防错原则1: avoid silent NaN propagation in means/SS)
            for (int i = 0; i < k; i++)
            {
                if (groups[i].Length == 0)
                    throw new ArgumentException(
                        $"Group {i} is empty. All groups must have at least one observation.");
                for (int j = 0; j < groups[i].Length; j++)
                    if (double.IsNaN(groups[i][j]) || double.IsInfinity(groups[i][j]))
                        throw new ArgumentException(
                            $"Group {i} contains {(double.IsNaN(groups[i][j]) ? "NaN" : "Infinity")} at index {j}. ANOVA requires finite values.");
            }
            var means = groups.Select(g => g.Average()).ToArray();
            var counts = groups.Select(g => (long)g.Length).ToArray();
            double grand = groups.SelectMany(g => g).Average();
            int totalN = groups.Sum(g => g.Length);

            double ssB = 0;
            for (int i = 0; i < k; i++) ssB += counts[i] * Math.Pow(means[i] - grand, 2);
            double ssW = 0;
            for (int i = 0; i < k; i++) ssW += groups[i].Sum(x => Math.Pow(x - means[i], 2));

            double dfB = k - 1, dfW = totalN - k;
            if (dfW <= 0)
                throw new ArgumentException(
                    $"ANOVA requires at least 2 observations per group (df_within={dfW}).");

            // review 2026-08-29（发行前 max level 复审）：输入虽已拒绝 NaN/Inf，但有限极大值
            // （如 1e200）平方后仍可溢出为 Inf。原守卫生效于 `Math.Abs(ssW)<1e-15`，而
            // `Abs(Inf)<1e-15` 为 false → 绕过守卫 → f=Inf/Inf=NaN 静默泄漏（与 FitOLS/FitRidge 的 Inf 守卫不一致）。
            if (double.IsNaN(ssB) || double.IsInfinity(ssB) || double.IsNaN(ssW) || double.IsInfinity(ssW))
                throw new ArgumentException(
                    "ANOVA failed: sums of squares are non-finite. Input values are too large in magnitude.");

            // Guard against degenerate data where all observations are identical
            // (within-group variance = 0 → F = 0/0 = NaN with no diagnostic message).
            // review 2026-08-31（深度审查 P1-5）：原 `Math.Abs(ssW) < 1e-15` 绝对阈值把
            // 小量纲数据（ppm/ppb/nm 级）误判为"组内完全一致"并抛错（{1e-9,2e-9,3e-9} 的
            // ssW=2e-18 < 1e-15）。方差平方和是尺度相关量，判据必须是精确零（真常量组），
            // 非有限值已在上方显式守卫。
            if (ssW == 0)
                throw new ArgumentException(
                    "ANOVA failed: within-group sum of squares is zero. " +
                    "All observations within each group are effectively identical.");
            double msB = ssB / dfB, msW = ssW / dfW;
            double f = msB / msW;
            double p = FDistPValue(f, dfB, dfW);

            return new Dictionary<string, object>
            {
                ["ss_between"] = ssB, ["ss_within"] = ssW, ["ss_total"] = ssB + ssW,
                ["df_between"] = dfB, ["df_within"] = dfW, ["df_total"] = totalN - 1,
                ["ms_between"] = msB, ["ms_within"] = msW,
                ["f_stat"] = f, ["p_value"] = p,
                ["group_means"] = means, ["group_counts"] = counts,
            };
        }

        /// <summary>
        /// Rank predictors by absolute t-statistic after standardizing columns.
        /// Standardizes X, fits OLS, then orders indices 0..p-1 by descending |t|.
        /// Higher rank = greater predictive importance.
        /// Used by REGRESS.FACTORIMP.
        /// </summary>
        /// <returns>Column indices sorted most-to-least important.</returns>
        internal static int[] FactorImportance(double[,] X, double[] y)
        {
            NumericGuard.AgainstNonFinite(X, y);
            int n = X.GetLength(0), p = X.GetLength(1);
            if (n < 2)
                throw new ArgumentException(
                    "Factor importance requires at least 2 observations.");
            // First pass: compute means and standard deviations; flag constant columns
            var means = new double[p];
            var sds = new double[p];
            var constCols = new bool[p];
            int activeCols = 0;
            for (int j = 0; j < p; j++)
            {
                double mean = 0, sd = 0;
                for (int i = 0; i < n; i++) mean += X[i, j];
                mean /= n;
                means[j] = mean;
                for (int i = 0; i < n; i++) { double d = X[i, j] - mean; sd += d * d; }
                sd = Math.Sqrt(sd / (n - 1));
                sds[j] = sd;
                // review 2026-08-31（深度审查 P1-5）：原 `sd < 1e-12` 绝对阈值在 1e-9 量级
                // 数据（sd~1e-9）下误判常数列。标准差与数据同尺度，判据应为精确零（真常量列）；
                // sd=NaN/Inf（防御性）同样视为常量列跳过标准化。
                if (!(sd > 0) || double.IsInfinity(sd))
                {
                    constCols[j] = true;
                    System.Diagnostics.Debug.WriteLine(
                        $"[FactorImportance] Column {j} has zero variance (constant); ranked least important.");
                }
                else activeCols++;
            }
            // All columns constant → no meaningful ranking
            if (activeCols == 0)
                return Enumerable.Range(0, p).ToArray();
            // Build reduced design matrix excluding constant columns
            var Xs = new double[n, activeCols];
            var colMap = new int[activeCols];
            int aj = 0;
            for (int j = 0; j < p; j++)
            {
                if (constCols[j]) continue;
                colMap[aj] = j;
                for (int i = 0; i < n; i++) Xs[i, aj] = (X[i, j] - means[j]) / sds[j];
                aj++;
            }
            // Fit OLS to reduced model (standardized columns already centered — no intercept needed)
            var result = FitOLS(Xs, y, addIntercept: false);
            var tReduced = (double[])result["t_stats"];
            var tFull = new double[p]; // constCols entries remain 0.0
            for (int rj = 0; rj < activeCols; rj++)
                tFull[colMap[rj]] = tReduced[rj];
            return Enumerable.Range(0, p).OrderByDescending(j => Math.Abs(tFull[j])).ToArray();
        }

        /// <summary>
        /// Prepend a column of 1.0 to the design matrix as the intercept term.
        /// Returns a new array; does not mutate the input.
        /// </summary>
        private static double[,] PrependIntercept(double[,] X)
        {
            int n = X.GetLength(0), p = X.GetLength(1);
            var Xaug = new double[n, p + 1];
            for (int i = 0; i < n; i++)
            {
                Xaug[i, 0] = 1.0;
                for (int j = 0; j < p; j++) Xaug[i, j + 1] = X[i, j];
            }
            return Xaug;
        }

        private static double FDistPValue(double f, double df1, double df2)
        {
            double x = df2 / (df2 + df1 * f);
            // BetaRegularized(df2/2, df1/2, x) = P(F > f) — the upper-tail p-value directly.
            // Do NOT add 1.0- here; StatsCore.TStatPValue uses a different
            // parameterisation that returns the two-tailed p-value directly.
            return MathNet.Numerics.SpecialFunctions.BetaRegularized(df2 / 2.0, df1 / 2.0, x);
        }
    }
}