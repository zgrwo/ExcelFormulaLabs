using System;
using ExcelFormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class DoeBbTests
    {
        private static int CenterPoints(int k)
        {
            int[] points = { 0, 0, 0, 3, 3, 6, 6, 6, 8, 9, 10, 12, 12, 13, 14, 15, 16 };
            return k <= 16 ? points[k] : k;
        }

        private static void AssertBb(double[,] m, int k)
        {
            int edgePoints = 2 * k * (k - 1);
            int center = CenterPoints(k);
            m.GetLength(0).Should().Be(edgePoints + center);
            m.GetLength(1).Should().Be(k);

            // Edge points: exactly two coordinates are ±1, the rest 0.
            for (int r = 0; r < edgePoints; r++)
            {
                int nonZero = 0;
                for (int c = 0; c < k; c++)
                {
                    double v = m[r, c];
                    if (Math.Abs(v) > 1e-12)
                    {
                        nonZero++;
                        Math.Abs(v).Should().BeApproximately(1.0, 1e-12);
                    }
                }
                nonZero.Should().Be(2);
            }

            // Center points: all zeros.
            for (int r = edgePoints; r < m.GetLength(0); r++)
                for (int c = 0; c < k; c++)
                    m[r, c].Should().Be(0.0);
        }

        [Fact] public void Bb_3_factors() => AssertBb(DoeCore.RsmBb(3), 3);
        [Fact] public void Bb_4_factors() => AssertBb(DoeCore.RsmBb(4), 4);
        [Fact] public void Bb_5_factors() => AssertBb(DoeCore.RsmBb(5), 5);

        // ── Run counts match pyDOE2 bbdesign ──────────────────────────
        [Fact] public void Run_counts()
        {
            DoeCore.RsmBb(3).GetLength(0).Should().Be(15); // 12 + 3
            DoeCore.RsmBb(4).GetLength(0).Should().Be(27); // 24 + 3
            DoeCore.RsmBb(5).GetLength(0).Should().Be(46); // 40 + 6
            DoeCore.RsmBb(6).GetLength(0).Should().Be(66); // 60 + 6
        }

        // ── PlanBb output shape ───────────────────────────────────────
        [Fact] public void PlanBb_shape()
        {
            var r = DoeCore.PlanBb(3, 2, 0, 2, false, null);
            r.GetLength(0).Should().Be(16);  // 1 header + 15 runs
            r.GetLength(1).Should().Be(5);   // StdOrder, RunOrder, A, B, C
            r[0, 0].Should().Be("StdOrder");
            r[0, 4].Should().Be("C");
        }

        [Fact] public void PlanBb_seed_reproducible()
        {
            var a = DoeCore.PlanBb(4, 2, 0, 2, true, 123);
            var b = DoeCore.PlanBb(4, 2, 0, 2, true, 123);
            for (int i = 1; i <= 27; i++) a[i, 1].Should().Be(b[i, 1]);
        }

        // ── Method dispatch ───────────────────────────────────────────
        [Fact] public void Plan_dispatch_bb()
        {
            var r = DoeCore.Plan(3, 2, 0, 2, "bb", false, null);
            r.GetLength(0).Should().Be(16);
            var r2 = DoeCore.Plan(3, 2, 0, 2, "boxbehnken", false, null);
            r2.GetLength(0).Should().Be(16);
        }

        // ── Guard paths ───────────────────────────────────────────────
        [Fact] public void Fewer_than_3_factors_throws()
            => new Action(() => DoeCore.RsmBb(2))
                .Should().Throw<ArgumentException>().WithMessage("*3*");

        [Fact] public void Too_many_runs_throws()
            => new Action(() => DoeCore.RsmBb(800)) // 2*800*799 + 800 > MaxRuns
                .Should().Throw<ArgumentException>().WithMessage("*runs*");
    }
}
