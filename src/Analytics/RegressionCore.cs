using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
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
        /// <returns>
        /// Dictionary: coefficients, sse, r_squared, adj_r_squared, residuals,
        /// fitted_values, standard_errors, t_stats, p_values, n, df.
        /// p&lt;0.05 = significant; R² near 1 = good fit.
        /// </returns>
        internal static Dictionary<string, object> FitOLS(double[,] X, double[] y)
        {
            NumericGuard.AgainstNonFinite(X, y);
            int n = X.GetLength(0), p = X.GetLength(1);
            var matX = Matrix<double>.Build.DenseOfArray(X);
            var vecY = Vector<double>.Build.Dense(y);

            var XtX = matX.TransposeThisAndMultiply(matX);
            var Xty = matX.TransposeThisAndMultiply(vecY);
            var beta = XtX.Solve(Xty);

            var fitted = matX * beta;
            var residuals = vecY - fitted;
            double sse = residuals.DotProduct(residuals);
            double tss = vecY.DotProduct(vecY) - Math.Pow(vecY.Sum(), 2) / n;
            if (Math.Abs(tss) < 1e-15)
                throw new ArgumentException(
                    "Cannot fit OLS: total sum of squares is zero (constant response variable y).");
            double r2 = 1.0 - sse / tss;
            int df = n - p;
            if (df <= 0)
                throw new ArgumentException(
                    $"Cannot compute standard errors: degrees of freedom is {df} (n={n}, p={p}). Need n > p.");
            double adjR2 = 1.0 - (1.0 - r2) * (n - 1) / (double)df;
            double sigma2 = sse / df;
            var XtXInv = XtX.Inverse();
            var se = new double[p];
            var tStat = new double[p];
            var pVal = new double[p];
            for (int j = 0; j < p; j++)
            {
                se[j] = Math.Sqrt(sigma2 * XtXInv[j, j]);
                tStat[j] = beta[j] / se[j];
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
        /// <returns>
        /// Same keys as FitOLS: coefficients, sse, r_squared, adj_r_squared,
        /// residuals, fitted_values, standard_errors, t_stats, p_values, n, df.
        /// SSE and R² are on the weighted scale (matching Python statsmodels);
        /// residuals and fitted_values are on the original scale.
        /// </returns>
        internal static Dictionary<string, object> FitWLS(double[,] X, double[] y, double[] w)
        {
            NumericGuard.AgainstNonFinite(X, y);
            int n = X.GetLength(0), p = X.GetLength(1);
            // Reject negative/NaN/Infinity weights — Sqrt produces NaN/Inf which would silently propagate
            for (int i = 0; i < w.Length; i++)
                if (w[i] < 0 || double.IsNaN(w[i]) || double.IsInfinity(w[i]))
                    throw new ArgumentException($"Weight at index {i} is invalid ({w[i]}). All weights must be >= 0 and finite.");
            var Xw = new double[n, p];
            var yw = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sw = Math.Sqrt(w[i]);
                for (int j = 0; j < p; j++) Xw[i, j] = X[i, j] * sw;
                yw[i] = y[i] * sw;
            }
            // Coefficients, SE, t-stats, p-values are correct from the weighted fit.
            // SSE and R² are on the weighted scale (consistent with Python statsmodels).
            var result = FitOLS(Xw, yw);
            // Override residuals and fitted_values to ORIGINAL scale so they are
            // comparable to the user's input y (matching Python statsmodels behaviour).
            var beta = (double[])result["coefficients"];
            double[] fittedOrig = new double[n];
            double[] residualsOrig = new double[n];
            for (int i = 0; i < n; i++)
            {
                double fit = 0;
                for (int j = 0; j < p; j++) fit += X[i, j] * beta[j];
                fittedOrig[i] = fit;
                residualsOrig[i] = y[i] - fit;
            }
            result["fitted_values"] = fittedOrig;
            result["residuals"] = residualsOrig;
            return result;
        }

        /// <summary>
        /// Ridge regression with L2 regularization. Adds lambda*I to X'X before solving.
        /// Shrinks coefficients to reduce overfitting. No standard errors or p-values
        /// (inferential statistics are not valid under regularization).
        /// Used by REGRESS.RIDGE.
        /// </summary>
        /// <returns>
        /// Dictionary: coefficients, sse, r_squared, residuals, fitted_values,
        /// lambda, n, df. NOTE: standard_errors, t_stats, p_values are NOT returned.
        /// </returns>
        internal static Dictionary<string, object> FitRidge(double[,] X, double[] y, double lambda = 1.0)
        {
            NumericGuard.AgainstNonFinite(X, y);
            if (double.IsNaN(lambda) || double.IsInfinity(lambda))
                throw new ArgumentException($"Lambda must be a finite value (got {lambda}).");
            int n = X.GetLength(0), p = X.GetLength(1);
            var matX = Matrix<double>.Build.DenseOfArray(X);
            var vecY = Vector<double>.Build.Dense(y);
            var XtX = matX.TransposeThisAndMultiply(matX);
            var Xty = matX.TransposeThisAndMultiply(vecY);
            var ridge = XtX + Matrix<double>.Build.DenseIdentity(p) * lambda;
            var beta = ridge.Solve(Xty);
            var fitted = matX * beta;
            var residuals = vecY - fitted;
            double sse = residuals.DotProduct(residuals);
            double tss = vecY.DotProduct(vecY) - Math.Pow(vecY.Sum(), 2) / n;
            if (Math.Abs(tss) < 1e-15)
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
                for (int j = 0; j < groups[i].Length; j++)
                    if (double.IsNaN(groups[i][j]) || double.IsInfinity(groups[i][j]))
                        throw new ArgumentException(
                            $"Group {i} contains {(double.IsNaN(groups[i][j]) ? "NaN" : "Infinity")} at index {j}. ANOVA requires finite values.");
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
            var Xs = new double[n, p];
            for (int j = 0; j < p; j++)
            {
                double mean = 0, sd = 0;
                for (int i = 0; i < n; i++) mean += X[i, j];
                mean /= n;
                for (int i = 0; i < n; i++) { double d = X[i, j] - mean; sd += d * d; }
                sd = Math.Sqrt(sd / (n - 1));
                if (sd < 1e-12)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"[FactorImportance] Column {j} has zero variance (constant); ranked least important.");
                    sd = 1;
                }
                for (int i = 0; i < n; i++) Xs[i, j] = (X[i, j] - mean) / sd;
            }
            var result = FitOLS(Xs, y);
            var t = (double[])result["t_stats"];
            return Enumerable.Range(0, p).OrderByDescending(j => Math.Abs(t[j])).ToArray();
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
