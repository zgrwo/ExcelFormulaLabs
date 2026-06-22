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
        internal static Dictionary<string, object> FitOLS(double[,] X, double[] y)
        {
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
            double r2 = 1.0 - sse / tss;
            double adjR2 = 1.0 - (1.0 - r2) * (n - 1) / (n - p);

            int df = n - p;
            double sigma2 = sse / df;
            var XtXInv = XtX.Inverse();
            var se = new double[p];
            var tStat = new double[p];
            var pVal = new double[p];
            for (int j = 0; j < p; j++)
            {
                se[j] = Math.Sqrt(sigma2 * XtXInv[j, j]);
                tStat[j] = beta[j] / se[j];
                pVal[j] = TStatPValue(Math.Abs(tStat[j]), df);
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

        internal static Dictionary<string, object> FitWLS(double[,] X, double[] y, double[] w)
        {
            int n = X.GetLength(0), p = X.GetLength(1);
            var Xw = new double[n, p];
            var yw = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sw = Math.Sqrt(w[i]);
                for (int j = 0; j < p; j++) Xw[i, j] = X[i, j] * sw;
                yw[i] = y[i] * sw;
            }
            return FitOLS(Xw, yw);
        }

        internal static Dictionary<string, object> FitRidge(double[,] X, double[] y, double lambda = 1.0)
        {
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

        internal static Dictionary<string, object> AnovaOneWay(double[][] groups)
        {
            int k = groups.Length;
            var means = groups.Select(g => g.Average()).ToArray();
            var counts = groups.Select(g => (long)g.Length).ToArray();
            double grand = groups.SelectMany(g => g).Average();
            int totalN = groups.Sum(g => g.Length);

            double ssB = 0;
            for (int i = 0; i < k; i++) ssB += counts[i] * Math.Pow(means[i] - grand, 2);
            double ssW = 0;
            for (int i = 0; i < k; i++) ssW += groups[i].Sum(x => Math.Pow(x - means[i], 2));

            double dfB = k - 1, dfW = totalN - k;
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

        internal static int[] FactorImportance(double[,] X, double[] y)
        {
            int n = X.GetLength(0), p = X.GetLength(1);
            var Xs = new double[n, p];
            for (int j = 0; j < p; j++)
            {
                double mean = 0, sd = 0;
                for (int i = 0; i < n; i++) mean += X[i, j];
                mean /= n;
                for (int i = 0; i < n; i++) sd += Math.Pow(X[i, j] - mean, 2);
                sd = Math.Sqrt(sd / (n - 1));
                if (sd < 1e-12) sd = 1;
                for (int i = 0; i < n; i++) Xs[i, j] = (X[i, j] - mean) / sd;
            }
            var result = FitOLS(Xs, y);
            var t = (double[])result["t_stats"];
            return Enumerable.Range(0, p).OrderByDescending(j => Math.Abs(t[j])).ToArray();
        }

        private static double TStatPValue(double t, double df)
        {
            double x = df / (df + t * t);
            return MathNet.Numerics.SpecialFunctions.BetaRegularized(df / 2.0, 0.5, x);
        }

        private static double FDistPValue(double f, double df1, double df2)
        {
            double x = df2 / (df2 + df1 * f);
            // BetaRegularized(df2/2, df1/2, x) = P(F > f) — the upper-tail p-value directly.
            // Do NOT add 1.0- here; TStatPValue uses a different parameterisation that
            // returns the two-tailed p-value directly.
            return MathNet.Numerics.SpecialFunctions.BetaRegularized(df2 / 2.0, df1 / 2.0, x);
        }
    }
}
