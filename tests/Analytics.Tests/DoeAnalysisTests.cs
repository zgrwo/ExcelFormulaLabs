using System;
using ExcelFormulaLabs.Analytics;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class DoeAnalysisTests
    {
        // 3-factor full factorial (2^3 = 8 runs), main-effects-only analysis.
        // y = [5,8,7,11,6,9,8,12] (cross-validated with scipy below).
        private static readonly double[,] X = {
            {-1,-1,-1},{ 1,-1,-1},{-1, 1,-1},{ 1, 1,-1},
            {-1,-1, 1},{ 1,-1, 1},{-1, 1, 1},{ 1, 1, 1}
        };
        private static readonly double[] y = { 5.0, 8.0, 7.0, 11.0, 6.0, 9.0, 8.0, 12.0 };

        private static double Cell(object[,] m, int r, int c) => (double)m[r, c];
        private static string CellS(object[,] m, int r, int c) => (string)m[r, c];

        // ── Effect table (main effects) ───────────────────────────────
        [Fact] public void Analyze_shape_and_header()
        {
            var m = DoeAnalysisCore.Analyze(X, y, 1, false);
            m.GetLength(0).Should().Be(4);  // header + A, B, C
            m.GetLength(1).Should().Be(5);
            CellS(m, 0, 0).Should().Be("Term");
            CellS(m, 0, 2).Should().Be("Effect");
        }

        [Fact] public void Analyze_coefficients()
        {
            var m = DoeAnalysisCore.Analyze(X, y, 1, false);
            CellS(m, 1, 0).Should().Be("A");
            Cell(m, 1, 1).Should().BeApproximately(1.75, 1e-10);   // A coef
            CellS(m, 2, 0).Should().Be("B");
            Cell(m, 2, 1).Should().BeApproximately(1.25, 1e-10);   // B coef
            CellS(m, 3, 0).Should().Be("C");
            Cell(m, 3, 1).Should().BeApproximately(0.5, 1e-10);    // C coef
        }

        [Fact] public void Analyze_effects_are_2x_coef()
        {
            var m = DoeAnalysisCore.Analyze(X, y, 1, false);
            Cell(m, 1, 2).Should().BeApproximately(3.5, 1e-10);  // A: 2×1.75
            Cell(m, 2, 2).Should().BeApproximately(2.5, 1e-10);  // B: 2×1.25
            Cell(m, 3, 2).Should().BeApproximately(1.0, 1e-10);  // C: 2×0.5
        }

        [Fact] public void Analyze_t_and_p()
        {
            var m = DoeAnalysisCore.Analyze(X, y, 1, false);
            Cell(m, 1, 3).Should().BeApproximately(14.0, 1e-10);
            Cell(m, 1, 4).Should().BeApproximately(0.0001510114, 1e-8);
            Cell(m, 2, 3).Should().BeApproximately(10.0, 1e-10);
            Cell(m, 3, 3).Should().BeApproximately(4.0, 1e-10);
            Cell(m, 3, 4).Should().BeApproximately(0.0161300899, 1e-8);
        }

        // ── ANOVA table ───────────────────────────────────────────────
        [Fact] public void Anova_effect_rows()
        {
            var m = DoeAnalysisCore.Anova(X, y, 1, false);
            m.GetLength(0).Should().Be(6);  // header + A,B,C + Error + Total
            CellS(m, 0, 0).Should().Be("Source");

            CellS(m, 1, 0).Should().Be("A");
            Cell(m, 1, 1).Should().BeApproximately(24.5, 1e-10);   // SS = MSE×t²
            Cell(m, 1, 4).Should().BeApproximately(196.0, 1e-10);  // F = t² = 14²
            CellS(m, 2, 0).Should().Be("B");
            Cell(m, 2, 1).Should().BeApproximately(12.5, 1e-10);
            CellS(m, 3, 0).Should().Be("C");
            Cell(m, 3, 1).Should().BeApproximately(2.0, 1e-10);
        }

        [Fact] public void Anova_error_and_total()
        {
            var m = DoeAnalysisCore.Anova(X, y, 1, false);
            CellS(m, 4, 0).Should().Be("Error");
            Cell(m, 4, 1).Should().BeApproximately(0.5, 1e-10);    // SSE
            Cell(m, 4, 3).Should().BeApproximately(0.125, 1e-10);  // MSE
            CellS(m, 5, 0).Should().Be("Total");
            Cell(m, 5, 1).Should().BeApproximately(39.5, 1e-10);   // TSS
        }

        // ── Pareto ────────────────────────────────────────────────────
        [Fact] public void Pareto_sorted_descending()
        {
            var m = DoeAnalysisCore.Pareto(X, y, 1, false);
            CellS(m, 1, 0).Should().Be("A");
            Cell(m, 1, 1).Should().BeApproximately(3.5, 1e-10);
            CellS(m, 2, 0).Should().Be("B");
            Cell(m, 2, 1).Should().BeApproximately(2.5, 1e-10);
            CellS(m, 3, 0).Should().Be("C");
            Cell(m, 3, 1).Should().BeApproximately(1.0, 1e-10);
        }

        // ── Term expansion: interactions and quadratic ───────────────
        [Fact] public void Analyze_2way_term_count()
        {
            var m = DoeAnalysisCore.Analyze(X, y, 2, false);
            // 3 main + 3 two-way = 6 terms + header
            m.GetLength(0).Should().Be(7);
            CellS(m, 4, 0).Should().Be("AB");
            CellS(m, 5, 0).Should().Be("AC");
            CellS(m, 6, 0).Should().Be("BC");
        }

        [Fact] public void Analyze_quadratic_term_count()
        {
            // Quadratic terms need 3 levels (CCD has ±1, ±α, 0) — with 2-level factors
            // the squared columns collapse to a constant and are collinear with the intercept.
            var Xccd = DoeCore.RsmCcd(3); // 22 × 3
            var yccd = new double[22];
            for (int i = 0; i < 22; i++) yccd[i] = i + 1.0;
            var m = DoeAnalysisCore.Analyze(Xccd, yccd, 2, true);
            // 3 main + 3 two-way + 3 quadratic = 9 terms + header
            m.GetLength(0).Should().Be(10);
            CellS(m, 7, 0).Should().Be("A^2");
            CellS(m, 8, 0).Should().Be("B^2");
            CellS(m, 9, 0).Should().Be("C^2");
        }

        // ── UDF layer ─────────────────────────────────────────────────
        [Fact] public void UDF_analyze_returns_table()
        {
            var r = (object[,])DoeAnalysisUdf.UDF_DOE_ANALYZE(X, y, "main");
            r.GetLength(0).Should().Be(4);
            ((double)r[1, 1]).Should().BeApproximately(1.75, 1e-10);
        }

        [Fact] public void UDF_anova_returns_table()
        {
            var r = (object[,])DoeAnalysisUdf.UDF_DOE_ANOVA(X, y, "main");
            r.GetLength(0).Should().Be(6);
            ((double)r[5, 1]).Should().BeApproximately(39.5, 1e-10);
        }

        [Fact] public void UDF_terms_unknown_returns_error()
            => DoeAnalysisUdf.UDF_DOE_ANALYZE(X, y, "bogus").Should().Be(ExcelError.Value);

        // ── Guard paths ───────────────────────────────────────────────
        [Fact] public void Analyze_length_mismatch_throws()
            => new Action(() => DoeAnalysisCore.Analyze(X, new[] { 1.0, 2.0 }, 1, false))
                .Should().Throw<ArgumentException>();

        [Fact] public void Analyze_no_factors_throws()
            => new Action(() => DoeAnalysisCore.Analyze(new double[8, 0], y, 1, false))
                .Should().Throw<ArgumentException>();
    }
}
