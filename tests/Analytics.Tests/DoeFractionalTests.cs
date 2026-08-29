using System;
using System.Collections.Generic;
using ExcelFormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class DoeFractionalTests
    {
        private static double D(double[,] m, int r, int c) => m[r, c];

        private static void AssertFractional(double[,] m, int k)
        {
            long runs = 1L << (k - 1);
            m.GetLength(0).Should().Be((int)runs);
            m.GetLength(1).Should().Be(k);

            // Each column balanced: half -1, half +1.
            for (int c = 0; c < k; c++)
            {
                int neg = 0, pos = 0;
                for (int r = 0; r < runs; r++)
                {
                    double v = m[r, c];
                    if (v < 0) neg++; else pos++;
                    v.Should().BeOneOf(-1.0, 1.0);
                }
                neg.Should().Be((int)(runs / 2));
                pos.Should().Be((int)(runs / 2));
            }

            // First k-1 columns are mutually orthogonal.
            for (int c1 = 0; c1 < k - 1; c1++)
                for (int c2 = c1 + 1; c2 < k - 1; c2++)
                {
                    var combos = new HashSet<(int, int)>();
                    for (int r = 0; r < runs; r++)
                        combos.Add(((int)m[r, c1], (int)m[r, c2]));
                    combos.Count.Should().Be(4, $"cols {c1},{c2} should cover all 4 combinations");
                }

            // Last factor = product of the first k-1 factors (default generator).
            for (int r = 0; r < runs; r++)
            {
                double prod = 1.0;
                for (int c = 0; c < k - 1; c++) prod *= m[r, c];
                m[r, k - 1].Should().Be(prod);
            }
        }

        [Fact] public void Fractional_4_factors() => AssertFractional(DoeCore.FractionalCoded(4), 4);
        [Fact] public void Fractional_5_factors() => AssertFractional(DoeCore.FractionalCoded(5), 5);
        [Fact] public void Fractional_6_factors() => AssertFractional(DoeCore.FractionalCoded(6), 6);
        [Fact] public void Fractional_7_factors() => AssertFractional(DoeCore.FractionalCoded(7), 7);

        // ── Standard order: first factor varies fastest ───────────────
        [Fact] public void Fractional_4_first_factor_fastest()
        {
            var m = DoeCore.FractionalCoded(4);
            // A (col 0) alternates every row: -1, 1, -1, 1, ...
            double[] expected = { -1, 1, -1, 1, -1, 1, -1, 1 };
            for (int r = 0; r < 8; r++) D(m, r, 0).Should().Be(expected[r]);
        }

        // ── PlanFractional output shape ───────────────────────────────
        [Fact] public void PlanFractional_shape()
        {
            var r = DoeCore.PlanFractional(4, 2, 0, 2, false, null);
            r.GetLength(0).Should().Be(9);  // 1 header + 8 runs
            r.GetLength(1).Should().Be(6);  // StdOrder, RunOrder, A, B, C, D
            r[0, 0].Should().Be("StdOrder");
            r[0, 2].Should().Be("A");
            r[0, 5].Should().Be("D");
        }

        [Fact] public void PlanFractional_seed_reproducible()
        {
            var a = DoeCore.PlanFractional(5, 2, 0, 2, true, 42);
            var b = DoeCore.PlanFractional(5, 2, 0, 2, true, 42);
            for (int i = 1; i <= 16; i++) a[i, 1].Should().Be(b[i, 1]);
        }

        // ── Method dispatch ───────────────────────────────────────────
        [Fact] public void Plan_dispatch_fractional()
        {
            var r = DoeCore.Plan(4, 2, 0, 2, "fractional", false, null);
            r.GetLength(0).Should().Be(9);
            var r2 = DoeCore.Plan(4, 2, 0, 2, "fracfact", false, null);
            r2.GetLength(0).Should().Be(9);
        }

        // ── Guard paths ───────────────────────────────────────────────
        [Fact] public void Fewer_than_4_factors_throws()
            => new Action(() => DoeCore.FractionalCoded(3))
                .Should().Throw<ArgumentException>().WithMessage("*4*");

        [Fact] public void Too_many_runs_throws()
            => new Action(() => DoeCore.FractionalCoded(21)) // 2^20 = 1,048,576 > MaxRuns
                .Should().Throw<ArgumentException>().WithMessage("*runs*");

        // review 2026-08-29：1L<<indep 在 indep≥64 时按 63 掩码回绕（1L<<83==1L<<19），
        // 原守卫可被绕过 → 352MB 分配 → 32 位 Excel OOM 崩溃。修复后应抛异常。
        [Fact] public void Shift_wrap_factors_throw()
            => new Action(() => DoeCore.FractionalCoded(84))
                .Should().Throw<ArgumentException>();

        [Fact] public void Non_2_level_throws()
            => new Action(() => DoeCore.PlanFractional(4, 3, 0, 2, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*3*");

        [Fact] public void Group2_non_2_level_throws()
            => new Action(() => DoeCore.PlanFractional(4, 2, 1, 3, false, null))
                .Should().Throw<ArgumentException>().WithMessage("*3*");
    }
}
