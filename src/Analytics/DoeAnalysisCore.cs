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
                double ssJ = double.IsNaN(t) || double.IsInfinity(t) ? double.NaN : mse * t * t;
                double fJ = double.IsNaN(t) || double.IsInfinity(t) ? double.NaN : t * t;
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
    }
}
