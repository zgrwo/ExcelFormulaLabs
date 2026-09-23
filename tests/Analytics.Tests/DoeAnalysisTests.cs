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

        // R3-9：计数须与 ExpandTerms 一致（含 3 阶交互段；此前缺 k(k-1)(k-2)/6）。
        [Fact] public void ExpandedTermCount_matches_expansion_up_to_3rd_order()
        {
            for (int k = 1; k <= 6; k++)
                for (int order = 1; order <= 3; order++)
                    DoeAnalysisCore.ExpandedTermCount(k, order, false)
                        .Should().Be(ExpandTermsCount(k, order));
        }

        private static int ExpandTermsCount(int k, int order)
        {
            // 独立枚举：（order≥1 主效应）+（order≥2 两阶）+（order≥3 三阶）
            int c = k;
            if (order >= 2) c += k * (k - 1) / 2;
            if (order >= 3) c += k * (k - 1) * (k - 2) / 6;
            return c;
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

        // terms 省略（Blank/Missing/DBNull）→ 默认 "2way"。
        [Theory]
        [MemberData(nameof(OmittedSentinelData.All), MemberType = typeof(OmittedSentinelData))]
        public void Udf_omitted_terms_use_2way_default(object? sentinel)
        {
            var expected = (object[,])DoeAnalysisUdf.UDF_DOE_ANALYZE(X, y, "2way");
            var actual = (object[,])DoeAnalysisUdf.UDF_DOE_ANALYZE(X, y, sentinel!);
            // R2-12：先钉硬编码锚点（独立于被测实现），再比较 sentinel 与显式调用——
            // 否则两路调用同坏时"自产期望"假绿（E5）。完整正交设计的 2way 主效应与 main 相同。
            expected.GetLength(0).Should().Be(7);          // header + 3 main + 3 two-way
            expected.GetLength(1).Should().Be(5);
            CellS(expected, 0, 0).Should().Be("Term");
            CellS(expected, 1, 0).Should().Be("A");
            Cell(expected, 1, 1).Should().BeApproximately(1.75, 1e-10);
            CellS(expected, 4, 0).Should().Be("AB");
            CellS(expected, 5, 0).Should().Be("AC");
            CellS(expected, 6, 0).Should().Be("BC");
            actual.GetLength(0).Should().Be(expected.GetLength(0));
            actual.GetLength(1).Should().Be(expected.GetLength(1));
            for (int r = 0; r < expected.GetLength(0); r++)
                for (int c = 0; c < expected.GetLength(1); c++)
                    actual[r, c].Should().Be(expected[r, c], $"cell [{r},{c}]");
        }

        // 2×2 饱和设计（n=4）用默认 terms（2way）时
        // 扩展项+截距=4 ≥ n → 自动降为 main（3 参数，df=1），而非 #VALUE!。
        [Fact]
        public void Udf_default_terms_on_saturated_2x2_auto_reduces_to_main()
        {
            double[,] design = { { -1, -1 }, { 1, -1 }, { -1, 1 }, { 1, 1 } };
            double[] resp = { 3.0, 5.0, 5.0, 9.0 };
            var r = (object[,])DoeAnalysisUdf.UDF_DOE_ANALYZE(design, resp, ExcelEmpty.Value);
            r.GetLength(0).Should().Be(3);  // header + A + B
            r[1, 0].Should().Be("A");
            r[2, 0].Should().Be("B");
            ((double)r[1, 1]).Should().BeApproximately(1.5, 1e-12);
        }

        // ── Guard paths ───────────────────────────────────────────────
        [Fact] public void Analyze_length_mismatch_throws()
            => new Action(() => DoeAnalysisCore.Analyze(X, new[] { 1.0, 2.0 }, 1, false))
                .Should().Throw<ArgumentException>();

        [Fact] public void Analyze_no_factors_throws()
            => new Action(() => DoeAnalysisCore.Analyze(new double[8, 0], y, 1, false))
                .Should().Throw<ArgumentException>();
    }

    public class DoeAnalysisCoverageGapTests
    {
        [Fact] public void Pareto_udf_returns_report()
        {
            var design = new object[,] { { -1.0, -1.0 }, { -1.0, 1.0 }, { 1.0, -1.0 }, { 1.0, 1.0 } };
            var response = new object[] { 1.0, 2.0, 3.0, 4.0 };
            var r = (object[,])DoeAnalysisUdf.UDF_DOE_PARETO(design, response);
            r.GetLength(0).Should().BeGreaterThan(1);
        }

        [Fact] public void Anova_udf_accepts_quadratic_keyword()
        {
            var design = new object[,]
            {
                { -1.0, -1.0 }, { -1.0, 1.0 }, { 1.0, -1.0 },
                { 1.0, 1.0 }, { 0.0, 0.0 }, { 0.0, 0.0 },
            };
            var response = new object[] { 1.0, 2.0, 3.0, 4.0, 2.5, 2.6 };
            DoeAnalysisUdf.UDF_DOE_ANOVA(design, response, "quadratic").Should().BeOfType<object[,]>();
        }

        [Fact] public void Anova_rejects_too_many_expanded_terms()
        {
            var X = new double[2, 101];
            var y = new[] { 1.0, 2.0 };
            new Action(() => DoeAnalysisCore.Anova(X, y, 2, false)).Should().Throw<ArgumentException>();
        }

        [Fact] public void Anova_rejects_cells_over_budget()
        {
            var X = new double[2000, 45];
            var y = new double[2000];
            new Action(() => DoeAnalysisCore.Anova(X, y, 2, false)).Should().Throw<ArgumentException>();
        }

        [Fact] public void Anova_supports_three_way_interactions()
        {
            var rnd = new Random(7);
            var X = new double[20, 4];
            for (int i = 0; i < 20; i++)
                for (int j = 0; j < 4; j++) X[i, j] = (i >> j & 1) == 1 ? 1.0 : -1.0;
            var y = new double[20];
            for (int i = 0; i < 20; i++) y[i] = 1.0 + i * 0.1 + rnd.NextDouble() * 0.01;
            var r = DoeAnalysisCore.Anova(X, y, 3, false);
            r.GetLength(0).Should().BeGreaterThan(1);
        }
    }
}
