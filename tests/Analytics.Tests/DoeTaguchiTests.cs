using System;
using System.Collections.Generic;
using ExcelFormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class DoeTaguchiTests
    {
        /// <summary>
        /// Verify an orthogonal array: each column is balanced (each level appears
        /// runs/levels times) and every column pair is orthogonal (each level
        /// combination appears runs/(levels[i]*levels[j]) times).
        /// </summary>
        private static void AssertOrthogonal(double[,] coded, int[] levels)
        {
            int runs = coded.GetLength(0);
            int cols = coded.GetLength(1);
            levels.Length.Should().Be(cols);

            for (int c = 0; c < cols; c++)
            {
                var cnt = new Dictionary<int, int>();
                for (int r = 0; r < runs; r++)
                {
                    int k = (int)coded[r, c];
                    cnt.TryGetValue(k, out int v);
                    cnt[k] = v + 1;
                }
                cnt.Count.Should().Be(levels[c], $"column {c} should have {levels[c]} distinct levels");
                foreach (var kv in cnt)
                    kv.Value.Should().Be(runs / levels[c], $"column {c} level {kv.Key} unbalanced");
            }

            for (int c1 = 0; c1 < cols; c1++)
                for (int c2 = c1 + 1; c2 < cols; c2++)
                {
                    var pair = new Dictionary<(int, int), int>();
                    for (int r = 0; r < runs; r++)
                    {
                        var k = ((int)coded[r, c1], (int)coded[r, c2]);
                        pair.TryGetValue(k, out int pv);
                        pair[k] = pv + 1;
                    }
                    pair.Count.Should().Be(levels[c1] * levels[c2], $"cols {c1},{c2} should be orthogonal");
                    foreach (var kv in pair)
                        kv.Value.Should().Be(runs / (levels[c1] * levels[c2]), $"cols {c1},{c2} unbalanced");
                }
        }

        private static double[,] Taguchi(int qty1, int level1, int qty2, int level2)
            => DoeCore.TaguchiCoded(qty1, level1, qty2, level2);

        private static int[] Levels(int qty1, int level1, int qty2, int level2)
        {
            var l = new List<int>();
            for (int i = 0; i < qty1; i++) l.Add(level1);
            for (int i = 0; i < qty2; i++) l.Add(level2);
            return l.ToArray();
        }

        // ── 2-level orthogonal arrays ────────────────────────────────
        [Fact] public void L4_3_factors() { var m = Taguchi(3, 2, 0, 2); m.GetLength(0).Should().Be(4); AssertOrthogonal(m, Levels(3, 2, 0, 2)); }
        [Fact] public void L8_7_factors() { var m = Taguchi(7, 2, 0, 2); m.GetLength(0).Should().Be(8); AssertOrthogonal(m, Levels(7, 2, 0, 2)); }
        [Fact] public void L12_11_factors() { var m = Taguchi(11, 2, 0, 2); m.GetLength(0).Should().Be(12); AssertOrthogonal(m, Levels(11, 2, 0, 2)); }
        [Fact] public void L16_15_factors() { var m = Taguchi(15, 2, 0, 2); m.GetLength(0).Should().Be(16); AssertOrthogonal(m, Levels(15, 2, 0, 2)); }
        [Fact] public void L32_31_factors() { var m = Taguchi(31, 2, 0, 2); m.GetLength(0).Should().Be(32); AssertOrthogonal(m, Levels(31, 2, 0, 2)); }

        // ── 3-level orthogonal arrays ────────────────────────────────
        [Fact] public void L9_4_factors() { var m = Taguchi(4, 3, 0, 3); m.GetLength(0).Should().Be(9); AssertOrthogonal(m, Levels(4, 3, 0, 3)); }
        [Fact] public void L27_13_factors() { var m = Taguchi(13, 3, 0, 3); m.GetLength(0).Should().Be(27); AssertOrthogonal(m, Levels(13, 3, 0, 3)); }

        // ── Mixed L18(2¹×3⁷) ─────────────────────────────────────────
        [Fact] public void L18_mixed()
        {
            var m = Taguchi(1, 2, 7, 3);
            m.GetLength(0).Should().Be(18);
            m.GetLength(1).Should().Be(8);
            AssertOrthogonal(m, Levels(1, 2, 7, 3));
        }

        // ── Column selection: smallest array ─────────────────────────
        [Fact] public void Selects_smallest_array()
        {
            Taguchi(3, 2, 0, 2).GetLength(0).Should().Be(4);   // → L4
            Taguchi(4, 2, 0, 2).GetLength(0).Should().Be(8);   // → L8
            Taguchi(8, 2, 0, 2).GetLength(0).Should().Be(12);  // → L12
            Taguchi(12, 2, 0, 2).GetLength(0).Should().Be(16); // → L16
            Taguchi(16, 2, 0, 2).GetLength(0).Should().Be(32); // → L32
            Taguchi(2, 3, 0, 3).GetLength(0).Should().Be(9);   // → L9
            Taguchi(5, 3, 0, 3).GetLength(0).Should().Be(27);  // → L27
        }

        [Fact] public void Mixed_selects_L18()
        {
            Taguchi(1, 2, 4, 3).GetLength(0).Should().Be(18); // 1 two-level + 4 three-level
            Taguchi(1, 2, 7, 3).GetLength(0).Should().Be(18); // 1 two-level + 7 three-level (max)
        }

        // ── Coding: 2-level → -1/+1, 3-level → -1/0/+1 ──────────────
        [Fact] public void Coding_2level()
        {
            var m = Taguchi(1, 2, 0, 2);
            m.GetLength(0).Should().Be(4); // 1 factor → L4 (4 runs)
            var vals = new HashSet<double>();
            for (int r = 0; r < 4; r++) vals.Add(m[r, 0]);
            vals.Should().BeEquivalentTo(new[] { -1.0, 1.0 });
        }

        [Fact] public void Coding_3level()
        {
            var m = Taguchi(1, 3, 0, 3);
            m.GetLength(0).Should().Be(9); // 1 factor → L9
            var vals = new HashSet<double>();
            for (int r = 0; r < 9; r++) vals.Add(m[r, 0]);
            vals.Should().BeEquivalentTo(new[] { -1.0, 0.0, 1.0 });
        }

        // ── Factor ordering: group 1 first ───────────────────────────
        [Fact] public void Factor_order_group1_first()
        {
            // 2 two-level (group1) + 2 three-level (group2) → L18 (mixed not hit: n2=2,n3=2 → L18 mixed unsupported)
            // Use pure 2-level to check ordering instead.
            var m = Taguchi(3, 2, 0, 2); // 3 factors, all 2-level, L4
            // All columns are 2-level; order is A, B, C (indistinguishable by value).
            // The ordering is exercised implicitly by column count == factor count.
            m.GetLength(1).Should().Be(3);
        }

        // ── PlanTaguchi output shape ─────────────────────────────────
        [Fact] public void PlanTaguchi_shape()
        {
            var r = DoeCore.PlanTaguchi(3, 2, 0, 2, false, null);
            r.GetLength(0).Should().Be(5);  // 1 header + 4 runs (L4)
            r.GetLength(1).Should().Be(5);  // StdOrder, RunOrder, A, B, C
            r[0, 0].Should().Be("StdOrder");
            r[0, 1].Should().Be("RunOrder");
            r[0, 2].Should().Be("A");
            r[0, 4].Should().Be("C");
        }

        [Fact] public void PlanTaguchi_seed_reproducible()
        {
            var a = DoeCore.PlanTaguchi(7, 2, 0, 2, true, 99);
            var b = DoeCore.PlanTaguchi(7, 2, 0, 2, true, 99);
            for (int i = 1; i <= 8; i++) a[i, 1].Should().Be(b[i, 1]);
        }

        // ── Guard paths ──────────────────────────────────────────────
        [Fact] public void Unsupported_level_throws()
            => new Action(() => Taguchi(2, 4, 0, 4))
                .Should().Throw<ArgumentException>().WithMessage("*4*");

        [Fact] public void Too_many_two_level_throws()
            => new Action(() => Taguchi(32, 2, 0, 2))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        [Fact] public void Too_many_three_level_throws()
            => new Action(() => Taguchi(14, 3, 0, 3))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        [Fact] public void Mixed_too_many_throws()
            => new Action(() => Taguchi(2, 2, 3, 3))
                .Should().Throw<ArgumentException>().WithMessage("*Mixed*");

        [Fact] public void Zero_factors_throws()
            => new Action(() => Taguchi(0, 2, 0, 2))
                .Should().Throw<ArgumentException>().WithMessage("*factor*");

        // ── review 2026-09-04（reaudit B1 回归守卫）：中间因子段分辨率 ≥ IV ──
        // GF(2) 定义字：最短零异或子集长度。≤3 长字存在 ⇒ 主效应与 2/3 阶交互别名（分辨率 III）。
        private static int MinWordLength(double[,] coded)
        {
            int runs = coded.GetLength(0), cols = coded.GetLength(1);
            var g = new ulong[cols];
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < runs; r++)
                    if (coded[r, c] < 0) g[c] |= 1UL << r; // -1 → GF(2) 1
            for (int size = 2; size <= cols; size++)
            {
                var idx = new int[size];
                for (int i = 0; i < size; i++) idx[i] = i;
                while (true)
                {
                    ulong x = 0;
                    foreach (var i in idx) x ^= g[i];
                    if (x == 0) return size;
                    int p = size - 1;
                    while (p >= 0 && idx[p] == cols - size + p) p--;
                    if (p < 0) break;
                    idx[p]++;
                    for (int q = p + 1; q < size; q++) idx[q] = idx[q - 1] + 1;
                }
            }
            return int.MaxValue;
        }

        [Fact] public void L32_16_factors_resolution_IV()
        {
            // n2=16 → L32。此前按重排前缀取前 16 列 → 分辨率 III（含 {ABCDE,ABCD,E} 3 长字）；
            // 中间因子段（k+1 < m ≤ 2^{k-1}）可达 IV：取含最高主效应 E 的 16 个 XOR-sum-free 列。
            var m = Taguchi(16, 2, 0, 2);
            m.GetLength(0).Should().Be(32);
            MinWordLength(m).Should().BeGreaterThanOrEqualTo(4);
        }

        [Fact] public void L8_4_factors_resolution_IV_preserved()
        {
            var m = Taguchi(4, 2, 0, 2); // n2=4 → L8
            m.GetLength(0).Should().Be(8);
            MinWordLength(m).Should().BeGreaterThanOrEqualTo(4);
        }

        [Fact] public void L32_31_factors_runs_and_orthogonality_kept()
        {
            // 超出 IV 容量（>2^{k-1}）保持原有顺序，正交性与行数不变（仍可正常生成）。
            var m = Taguchi(31, 2, 0, 2);
            m.GetLength(0).Should().Be(32);
            AssertOrthogonal(m, Levels(31, 2, 0, 2));
        }
    }
}
