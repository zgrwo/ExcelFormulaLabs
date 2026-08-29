using System;
using ExcelFormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class DoeRsmTests
    {
        private static double D(double[,] m, int r, int c) => m[r, c];

        private static double Alpha(int k) => Math.Pow(Math.Pow(2, k), 0.25); // 2^(k/4)

        private static void AssertCcd(double[,] m, int k)
        {
            double alpha = Alpha(k);
            long nf = 1L << k;
            int centerPerBlock = 4;
            int nAxial = 2 * k;
            long total = nf + centerPerBlock + nAxial + centerPerBlock;

            m.GetLength(0).Should().Be((int)total);
            m.GetLength(1).Should().Be(k);

            // 1. Factorial block: all coordinates ±1.
            for (long r = 0; r < nf; r++)
                for (int c = 0; c < k; c++)
                    Math.Abs(m[r, c]).Should().BeApproximately(1.0, 1e-12);

            // 2. Center block (factorial): all zeros.
            for (long r = nf; r < nf + centerPerBlock; r++)
                for (int c = 0; c < k; c++)
                    m[r, c].Should().Be(0.0);

            // 3. Axial block: one coordinate ±α, rest zero.
            long ax = nf + centerPerBlock;
            for (int i = 0; i < nAxial; i++)
            {
                var row = m[ax + i, 0];
                int nonZero = 0;
                for (int c = 0; c < k; c++)
                {
                    double v = m[ax + i, c];
                    if (Math.Abs(v) > 1e-12) { nonZero++; Math.Abs(v).Should().BeApproximately(alpha, 1e-12); }
                }
                nonZero.Should().Be(1);
            }

            // 4. Center block (axial): all zeros.
            for (long r = ax + nAxial; r < total; r++)
                for (int c = 0; c < k; c++)
                    m[r, c].Should().Be(0.0);
        }

        [Fact] public void Ccd_2_factors() => AssertCcd(DoeCore.RsmCcd(2), 2);
        [Fact] public void Ccd_3_factors() => AssertCcd(DoeCore.RsmCcd(3), 3);
        [Fact] public void Ccd_4_factors() => AssertCcd(DoeCore.RsmCcd(4), 4);

        // ── Alpha value is rotatable (2^(k/4)) ────────────────────────
        [Fact] public void Alpha_rotatable()
        {
            var m = DoeCore.RsmCcd(2);
            // Axial block starts at nf + center = 4 + 4 = 8.
            double axVal = Math.Abs(m[8, 0]);
            axVal.Should().BeApproximately(Math.Sqrt(2), 1e-12);

            var m3 = DoeCore.RsmCcd(3);
            double axVal3 = Math.Abs(m3[12, 0]); // 8 + 4 = 12
            axVal3.Should().BeApproximately(Math.Pow(2, 0.75), 1e-12);
        }

        // ── PlanRsm output shape ──────────────────────────────────────
        [Fact] public void PlanRsm_shape()
        {
            var r = DoeCore.PlanRsm(2, 2, 0, 2, false, null);
            r.GetLength(0).Should().Be(17);  // 1 header + 16 runs
            r.GetLength(1).Should().Be(4);   // StdOrder, RunOrder, A, B
            r[0, 0].Should().Be("StdOrder");
            r[0, 2].Should().Be("A");
            r[0, 3].Should().Be("B");
        }

        [Fact] public void PlanRsm_seed_reproducible()
        {
            var a = DoeCore.PlanRsm(3, 2, 0, 2, true, 7);
            var b = DoeCore.PlanRsm(3, 2, 0, 2, true, 7);
            for (int i = 1; i <= 22; i++) a[i, 1].Should().Be(b[i, 1]);
        }

        // ── Method dispatch ───────────────────────────────────────────
        [Fact] public void Plan_dispatch_rsm()
        {
            var r = DoeCore.Plan(2, 2, 0, 2, "rsm", false, null);
            r.GetLength(0).Should().Be(17);
            var r2 = DoeCore.Plan(2, 2, 0, 2, "ccd", false, null);
            r2.GetLength(0).Should().Be(17);
        }

        // ── Guard paths ───────────────────────────────────────────────
        [Fact] public void Fewer_than_2_factors_throws()
            => new Action(() => DoeCore.RsmCcd(1))
                .Should().Throw<ArgumentException>().WithMessage("*2*");

        // review 2026-08-29：位移回绕 + cells 守卫——原 `1L<<k` 在 k≥64 时掩码回绕可绕过守卫。
        [Fact] public void RsmCcd_shift_wrap_throws()
            => new Action(() => DoeCore.RsmCcd(83))
                .Should().Throw<ArgumentException>();

        // review 2026-08-29：BB 守卫原只量 runs（O(k²)）不量 cells——k=700 时分配可达 5.5GB。
        [Fact] public void RsmBb_cells_guard_throws()
            => new Action(() => DoeCore.RsmBb(700))
                .Should().Throw<ArgumentException>();
    }
}
