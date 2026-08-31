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

        // review 2026-08-29（发行前 max level 复审）：MaxRuns/MaxCells 只量 runs/cells，
        // 不量因子数。此前 `PlanFull(巨大 qty, ...)` 在 `new int[totalFactors]` 处分配数 GB
        // → 32 位 Excel OOM 崩溃。以下各守卫回归：必须在按因子数分配数组之前抛异常。
        // 测试用 MaxFactors+1 而非 10 亿——守卫对任何超限值都在分配前抛错，且避免回归时
        // 真实 4GB 分配 OOM 测试宿主。
        [Fact] public void Full_huge_factor_count_throws_before_allocation()
            => new Action(() => DoeCore.PlanFull(DoeCore.MaxFactors + 1, 2, 0, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        [Fact] public void Full_factors_over_max_throws()
            => new Action(() => DoeCore.PlanFull(DoeCore.MaxFactors + 1, 2, 0, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        [Fact] public void Rsm_huge_factor_count_throws_before_allocation()
            => new Action(() => DoeCore.PlanRsm(DoeCore.MaxFactors + 1, 2, 0, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        [Fact] public void Rsm_coded_huge_k_throws()
            => new Action(() => DoeCore.RsmCcd(DoeCore.MaxFactors + 1))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        [Fact] public void Fractional_huge_factor_count_throws()
            => new Action(() => DoeCore.PlanFractional(DoeCore.MaxFactors + 1, 2, 0, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        [Fact] public void Bb_huge_factor_count_throws()
            => new Action(() => DoeCore.PlanBb(DoeCore.MaxFactors + 1, 2, 0, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        // review 2026-08-29（max level 复审）：qty1+qty2 求和改 long——原 int 回绕恒为负
        // （最大和 2³²-2 回绕后 ∈ [-2³¹,-2]），错误落回 DOE_NoFactors 误导消息；long 后正确报 TooManyFactors。
        [Fact] public void Full_qty_sum_overflow_reports_too_many_factors()
            => new Action(() => DoeCore.PlanFull(int.MaxValue, 2, int.MaxValue, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*maximum supported*");

    // ── review-2026-08-31：P1-7 回归守卫 ──
    [Fact] public void Taguchi_2level_main_effect_columns_clean()
    {
        // P1-7：2 水平田口因子必须优先落在主效应列。L8（5 因子）列序应为 A,B,C,ABC,AB,…——
        // 前 3 列（主效应）不得是任何两列的 ±乘积（修复前第 3 列即 AB 交互列，因子 3 与 1×2 完全别名）。
        var plan = DoeCore.PlanTaguchi(5, 2, 0, 2, false, null);
        int runs = plan.GetLength(0) - 1, cols = plan.GetLength(1);
        var f = new double[runs, cols - 2];
        for (int r = 0; r < runs; r++)
            for (int c = 0; c < cols - 2; c++)
                f[r, c] = Convert.ToDouble(plan[r + 1, 2 + c]);
        // ±1 编码下交互列 = ±(两列乘积)；主效应列（前 3 列）不可能是其他两列的 ±乘积。
        for (int c = 0; c < Math.Min(3, cols - 2); c++)
            for (int a = 0; a < c; a++)
                for (int b = a + 1; b < c; b++)
                {
                    bool pos = true, neg = true;
                    for (int r = 0; r < runs; r++)
                    {
                        if (Math.Abs(f[r, c] - f[r, a] * f[r, b]) > 1e-9) pos = false;
                        if (Math.Abs(f[r, c] + f[r, a] * f[r, b]) > 1e-9) neg = false;
                    }
                    bool aliased = pos || neg;
                    aliased.Should().BeFalse($"主效应列 {c} 与列 {a}×{b} 别名");
                }
    }
}
}
