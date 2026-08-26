using System;
using System.Linq;
using ExcelFormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class DoeCoreTests
    {
        private static double D(object[,] r, int row, int col) => (double)r[row, col];
        private static long L(object[,] r, int row, int col) => (long)r[row, col];

        // ── Full factorial: 2² (2 factors, 2 levels each) ──────────────
        [Fact] public void Full_2x2_shape_and_header()
        {
            var r = DoeCore.PlanFull(2, 2, 0, 2, false, null);
            r.GetLength(0).Should().Be(5);  // 1 header + 4 runs
            r.GetLength(1).Should().Be(4);  // StdOrder, RunOrder, A, B
            r[0, 0].Should().Be("StdOrder");
            r[0, 1].Should().Be("RunOrder");
            r[0, 2].Should().Be("A");
            r[0, 3].Should().Be("B");
        }

        [Fact] public void Full_2x2_standard_order()
        {
            var r = DoeCore.PlanFull(2, 2, 0, 2, false, null);
            // Minitab/JMP standard order: first factor varies fastest.
            // StdOrder: (-1,-1), (+1,-1), (-1,+1), (+1,+1)
            D(r, 1, 2).Should().BeApproximately(-1.0, 1e-10);
            D(r, 1, 3).Should().BeApproximately(-1.0, 1e-10);
            D(r, 2, 2).Should().BeApproximately(1.0, 1e-10);
            D(r, 2, 3).Should().BeApproximately(-1.0, 1e-10);
            D(r, 3, 2).Should().BeApproximately(-1.0, 1e-10);
            D(r, 3, 3).Should().BeApproximately(1.0, 1e-10);
            D(r, 4, 2).Should().BeApproximately(1.0, 1e-10);
            D(r, 4, 3).Should().BeApproximately(1.0, 1e-10);
        }

        [Fact] public void Full_2x2_no_randomize_runorder_equals_stdorder()
        {
            var r = DoeCore.PlanFull(2, 2, 0, 2, false, null);
            for (int i = 1; i <= 4; i++)
                L(r, i, 1).Should().Be(L(r, i, 0));
        }

        [Fact] public void Full_2x2_randomize_is_permutation()
        {
            var r = DoeCore.PlanFull(2, 2, 0, 2, true, 42);
            var runs = Enumerable.Range(1, 4).Select(i => L(r, i, 1)).OrderBy(x => x).ToArray();
            runs.Should().Equal(1L, 2L, 3L, 4L);
            // StdOrder column unchanged by randomization.
            for (int i = 1; i <= 4; i++) L(r, i, 0).Should().Be(i);
        }

        [Fact] public void Full_2x2_seed_is_reproducible()
        {
            var a = DoeCore.PlanFull(2, 2, 0, 2, true, 12345);
            var b = DoeCore.PlanFull(2, 2, 0, 2, true, 12345);
            for (int i = 1; i <= 4; i++)
            {
                L(a, i, 1).Should().Be(L(b, i, 1));
                D(a, i, 2).Should().Be(D(b, i, 2));
            }
        }

        // ── Mixed level 2×3 (A: 2 levels, B: 3 levels) ────────────────
        [Fact] public void Full_2x3_mixed_standard_order_and_coding()
        {
            var r = DoeCore.PlanFull(1, 2, 1, 3, false, null);
            r.GetLength(0).Should().Be(7);  // 1 header + 6 runs
            r.GetLength(1).Should().Be(4);  // StdOrder, RunOrder, A, B
            r[0, 3].Should().Be("B");

            // A (2-level): -1,+1 alternating fastest; B (3-level): -1,0,+1 slowest.
            // StdOrder 1..6:
            double[] A = { -1, 1, -1, 1, -1, 1 };
            double[] B = { -1, -1, 0, 0, 1, 1 };
            for (int i = 0; i < 6; i++)
            {
                D(r, i + 1, 2).Should().BeApproximately(A[i], 1e-10);
                D(r, i + 1, 3).Should().BeApproximately(B[i], 1e-10);
            }
        }

        // ── 3-level coding: -1, 0, +1 ─────────────────────────────────
        [Fact] public void Full_single_factor_3_level()
        {
            var r = DoeCore.PlanFull(0, 3, 1, 3, false, null);
            r.GetLength(0).Should().Be(4);  // header + 3 runs
            D(r, 1, 2).Should().BeApproximately(-1.0, 1e-10);
            D(r, 2, 2).Should().BeApproximately(0.0, 1e-10);
            D(r, 3, 2).Should().BeApproximately(1.0, 1e-10);
        }

        // ── Degenerate / guard paths ──────────────────────────────────
        [Fact] public void Full_zero_factors_throws()
            => new Action(() => DoeCore.PlanFull(0, 2, 0, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        [Fact] public void Full_zero_level_throws()
            => new Action(() => DoeCore.PlanFull(1, 0, 0, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*level*");

        [Fact] public void Full_negative_qty_throws()
            => new Action(() => DoeCore.PlanFull(-1, 2, 0, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*quantit*");

        [Fact] public void Plan_unknown_method_throws()
            => new Action(() => DoeCore.Plan(1, 2, 0, 2, "bogus", false, null))
                .Should().Throw<ArgumentException>().WithMessage("*bogus*");

        [Fact] public void Plan_fractional_not_implemented()
            => new Action(() => DoeCore.Plan(1, 2, 0, 2, "fractional", false, null))
                .Should().Throw<ArgumentException>().WithMessage("*fractional*");

        [Fact] public void Plan_method_case_insensitive()
        {
            var a = DoeCore.Plan(2, 2, 0, 2, "FULL", false, null);
            var b = DoeCore.Plan(2, 2, 0, 2, "fullfact", false, null);
            a.GetLength(0).Should().Be(b.GetLength(0));
            a.GetLength(1).Should().Be(b.GetLength(1));
        }
    }
}
