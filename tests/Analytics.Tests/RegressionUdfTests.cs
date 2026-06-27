using System;
using System.Collections.Generic;
using FormulaLabs.Analytics;
using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.Analytics.Tests
{
    public class RegressionUdfTests
    {
        private static readonly double[,] X_test = { { 1, 1 }, { 1, 2 }, { 1, 3 } };
        private static readonly double[]   y_test = { 5, 8, 11 };

        /// <summary>
        /// Extract the values of a named field from a DictToReport table.
        /// Column 0 = field name, columns 1+ = scalar or unpacked array values.
        /// </summary>
        private static double[] FindRow(object[,] r, string key)
        {
            for (int i = 0; i < r.GetLength(0); i++)
            {
                if (r[i, 0] is string s && s == key)
                {
                    var vals = new List<double>();
                    for (int j = 1; j < r.GetLength(1); j++)
                    {
                        if (r[i, j] is double d) vals.Add(d);
                        else if (r[i, j] is long l) vals.Add(l);
                        else break;
                    }
                    return vals.ToArray();
                }
            }
            return Array.Empty<double>();
        }

        /// <summary>Extract a scalar value from a DictToReport table.</summary>
        private static double FindScalar(object[,] r, string key)
        {
            var vals = FindRow(r, key);
            return vals.Length > 0 ? vals[0] : double.NaN;
        }

        // ── Regression methods (return object[,] from DictToReport) ──
        [Fact] public void OLS_returns_report()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_OLS(y_test, X_test);
            // Report: N fields × (maxLen+1) columns. 11 fields for OLS.
            r.GetLength(0).Should().Be(11);
            r.GetLength(1).Should().BeGreaterOrEqualTo(2);
            // Coefficients are unpacked scalars across columns 1+
            var coef = FindRow(r, "coefficients");
            coef.Should().HaveCount(2);
            coef[0].Should().BeApproximately(2.0, 1e-10);
            coef[1].Should().BeApproximately(3.0, 1e-10);
        }
        [Fact] public void WLS_equal_weights()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_WLS(y_test, X_test, new double[] { 1.0, 1.0, 1.0 });
            r.GetLength(0).Should().Be(11);
            var coef = FindRow(r, "coefficients");
            coef[0].Should().BeApproximately(2.0, 1e-10);
            coef[1].Should().BeApproximately(3.0, 1e-10);
        }
        [Fact] public void Ridge_keys()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_RIDGE(y_test, X_test, 0.1);
            r.GetLength(0).Should().Be(8);
            var coef = FindRow(r, "coefficients");
            coef[0].Should().BeApproximately(2.0, 0.15);
            coef[1].Should().BeApproximately(3.0, 0.15);
        }
        [Fact] public void Anova1_keys()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_ANOVA1(new double[,] { { 5, 8 }, { 6, 9 }, { 7, 10 } });
            r.GetLength(0).Should().Be(12);
            r.GetLength(1).Should().BeGreaterOrEqualTo(2);
            var fStat = FindScalar(r, "f_stat");
            fStat.Should().BeGreaterThan(0);
        }

        // ── Factor importance (returns long[]) ──
        [Fact] public void FactorImportance_length()
        {
            var r = (long[])RegressionUdf.UDF_REGRESS_FACTORIMP(y_test, X_test);
            r.Length.Should().Be(2);
        }

        // ── OLS coefficients / R-squared (return double[] and double) ──
        [Fact] public void Coef_values()
        {
            var c = (double[])RegressionUdf.UDF_REGRESS_COEF(y_test, X_test);
            c[0].Should().BeApproximately(2.0, 1e-8);
            c[1].Should().BeApproximately(3.0, 1e-8);
        }
        [Fact] public void Rsq_is_one_for_perfect_fit() => ((double)RegressionUdf.UDF_REGRESS_RSQ(y_test, X_test)).Should().BeApproximately(1.0, 1e-10);

        // ── Report content verification ──────────────────────────────
        [Fact] public void OLS_report_content()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_OLS(y_test, X_test);
            FindScalar(r, "sse").Should().BeApproximately(0.0, 1e-10);
            FindScalar(r, "r_squared").Should().BeApproximately(1.0, 1e-10);
            FindScalar(r, "adj_r_squared").Should().BeApproximately(1.0, 1e-10);
            FindScalar(r, "n").Should().Be(3);
            FindScalar(r, "df").Should().Be(1);
            var resid = FindRow(r, "residuals");
            resid.Should().HaveCount(3);
            foreach (var v in resid) v.Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void WLS_report_content()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_WLS(y_test, X_test, new double[] { 1.0, 1.0, 1.0 });
            FindScalar(r, "r_squared").Should().BeApproximately(1.0, 1e-10);
            FindScalar(r, "n").Should().Be(3);
            var resid = FindRow(r, "residuals");
            foreach (var v in resid) v.Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void Ridge_report_content()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_RIDGE(y_test, X_test, 0.1);
            FindScalar(r, "r_squared").Should().BeGreaterThan(0.99);
            FindScalar(r, "lambda").Should().Be(0.1);
        }
        [Fact] public void Anova1_report_content()
        {
            var r = (object[,])RegressionUdf.UDF_REGRESS_ANOVA1(new double[,] { { 5, 8 }, { 6, 9 }, { 7, 10 } });
            FindScalar(r, "ss_total").Should().BeGreaterThan(0);
            FindScalar(r, "f_stat").Should().BeGreaterThan(0);
            FindScalar(r, "p_value").Should().BeLessThan(0.05);
            FindRow(r, "group_means").Should().HaveCount(2);
        }

        // ── P0 guard UDF-level: WrapError → #VALUE! ──
        [Fact] public void OLS_constant_y_returns_error()
        {
            var constY = new double[] { 5, 5, 5 };
            RegressionUdf.UDF_REGRESS_OLS(constY, X_test).Should().Be(ExcelError.Value);
        }
        [Fact] public void Ridge_constant_y_returns_error()
        {
            var constY = new double[] { 5, 5, 5 };
            RegressionUdf.UDF_REGRESS_RIDGE(constY, X_test, 0.1).Should().Be(ExcelError.Value);
        }
        [Fact] public void Anova1_single_group_returns_error()
        {
            RegressionUdf.UDF_REGRESS_ANOVA1(new double[,] { { 1 }, { 2 }, { 3 } })
                .Should().Be(ExcelError.Value);
        }
        [Fact] public void FactorImportance_single_row_returns_error()
        {
            var singleX = new double[,] { { 1, 5 } };
            var singleY = new double[] { 7 };
            RegressionUdf.UDF_REGRESS_FACTORIMP(singleY, singleX).Should().Be(ExcelError.Value);
        }
    }
}
