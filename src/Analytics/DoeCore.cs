using System;
using System.Collections.Generic;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// Design of Experiments (DOE): generates experimental design matrices
    /// (StandardOrder + RunOrder + coded factor columns) matching the standard
    /// orders used by Minitab / JMP. Pure combinatorial logic — zero Excel
    /// dependency, no MathNet. Factor levels are coded to [-1, +1] (coded
    /// units: 2-level → -1/+1, 3-level → -1/0/+1, n-level → equally spaced).
    /// </summary>
    internal static class DoeCore
    {
        /// <summary>Maximum number of runs a design may produce (safety guard).</summary>
        internal const long MaxRuns = 1_000_000;
        // review 2026-08-29：cells 上限（runs×k）。原守卫只量 runs——单公式如
        // =DOE.PLAN(84,2,0,2,"FRAC") 可分配 352MB+、"BB" 可到 5.5GB → 32 位 Excel OOM 崩溃
        // （OOM 被异常过滤器排除不可捕获）。
        internal const long MaxCells = 1_000_000;

        /// <summary>
        /// Unified entry point. Dispatches on <paramref name="method"/> (case-insensitive).
        /// </summary>
        /// <param name="qty1">Number of factors in group 1.</param>
        /// <param name="level1">Levels per factor in group 1.</param>
        /// <param name="qty2">Number of factors in group 2.</param>
        /// <param name="level2">Levels per factor in group 2.</param>
        /// <param name="method">Design method: FULL, TAGUCHI, FRACTIONAL, RSM, BB.</param>
        /// <param name="randomize">Randomize the run order (default true).</param>
        /// <param name="seed">Fixed seed for reproducible run order; null = random.</param>
        internal static object[,] Plan(int qty1, int level1, int qty2, int level2,
            string method, bool randomize, long? seed)
        {
            string m = (method ?? "").Trim().ToUpperInvariant();
            return m switch
            {
                "FULL" or "FULLFACT" or "FF" => PlanFull(qty1, level1, qty2, level2, randomize, seed),
                "TAGUCHI" or "OA" => PlanTaguchi(qty1, level1, qty2, level2, randomize, seed),
                "FRACTIONAL" or "FRACFACT" or "FRAC" => PlanFractional(qty1, level1, qty2, level2, randomize, seed),
                "RSM" or "CCD" => PlanRsm(qty1, level1, qty2, level2, randomize, seed),
                "BB" or "BOXBEHNKEN" => PlanBb(qty1, level1, qty2, level2, randomize, seed),
                _ => throw new ArgumentException(ErrorMsg.Get("DOE_UnknownMethod", m))
            };
        }

        // ─────────────────────────────────────────────────────────────
        // FULL (full factorial)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Full-factorial design over two factor groups. Group 1 contributes
        /// <paramref name="qty1"/> factors with <paramref name="level1"/> levels each;
        /// group 2 contributes <paramref name="qty2"/> factors with <paramref name="level2"/>
        /// levels each. Total runs = level1^qty1 * level2^qty2.
        /// Returns [runs + 1, 2 + totalFactors]: one header row then one row per run —
        /// [StdOrder, RunOrder, A, B, ...] with factor columns coded to [-1, +1].
        /// Standard order follows Minitab/JMP: the first factor varies fastest.
        /// </summary>
        internal static object[,] PlanFull(int qty1, int level1, int qty2, int level2,
            bool randomize, long? seed)
        {
            if (qty1 < 0 || qty2 < 0)
                throw new ArgumentException(ErrorMsg.Get("DOE_NegativeQty"));
            if (level1 < 1)
                throw new ArgumentException(ErrorMsg.Get("DOE_InvalidLevel", 1, level1));
            if (level2 < 1)
                throw new ArgumentException(ErrorMsg.Get("DOE_InvalidLevel", 2, level2));
            int totalFactors = qty1 + qty2;
            if (totalFactors < 1)
                throw new ArgumentException(ErrorMsg.Get("DOE_NoFactors"));

            // Per-factor level counts, group 1 factors first then group 2.
            var levels = new int[totalFactors];
            for (int i = 0; i < qty1; i++) levels[i] = level1;
            for (int i = 0; i < qty2; i++) levels[qty1 + i] = level2;

            return Assemble(FullFactorialCoded(levels), randomize, seed);
        }

        // ─────────────────────────────────────────────────────────────
        // TAGUCHI (orthogonal array)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Taguchi orthogonal-array design. Supports 2-level and 3-level factors
        /// (pure or mixed). Selects the smallest standard array that accommodates
        /// the factors: L4/L8/L12/L16/L32 (2-level), L9/L27 (3-level), L18 (mixed
        /// 2¹×3⁷). Returns [runs + 1, 2 + totalFactors] with the same shape as
        /// <see cref="PlanFull"/>; factor columns coded to [-1, +1].
        /// </summary>
        internal static object[,] PlanTaguchi(int qty1, int level1, int qty2, int level2,
            bool randomize, long? seed)
            => Assemble(TaguchiCoded(qty1, level1, qty2, level2), randomize, seed);

        // ─────────────────────────────────────────────────────────────
        // FRACTIONAL (2-level ½ fraction, resolution-maximal)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Two-level ½-fraction factorial design over <paramref name="qty1"/> +
        /// <paramref name="qty2"/> factors. Requires all factors to be 2-level and
        /// at least 4 factors (resolution IV or higher). Generator: the last factor
        /// equals the product of the preceding factors (Minitab default
        /// resolution-maximal generator). Returns [runs + 1, 2 + factors].
        /// </summary>
        internal static object[,] PlanFractional(int qty1, int level1, int qty2, int level2,
            bool randomize, long? seed)
        {
            if (qty1 < 0 || qty2 < 0)
                throw new ArgumentException(ErrorMsg.Get("DOE_NegativeQty"));
            int totalFactors = qty1 + qty2;
            if (totalFactors < 4)
                throw new ArgumentException(ErrorMsg.Get("DOE_FractionalTooFewFactors", totalFactors));
            if (qty1 > 0 && level1 != 2)
                throw new ArgumentException(ErrorMsg.Get("DOE_FractionalLevelUnsupported", level1));
            if (qty2 > 0 && level2 != 2)
                throw new ArgumentException(ErrorMsg.Get("DOE_FractionalLevelUnsupported", level2));

            return Assemble(FractionalCoded(totalFactors), randomize, seed);
        }

        // ─────────────────────────────────────────────────────────────
        // RSM (central composite design, rotatable)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Response-surface central composite design (circumscribed, rotatable).
        /// Factor count = <paramref name="qty1"/> + <paramref name="qty2"/> (continuous
        /// factors; level parameters are ignored). Returns [runs + 1, 2 + factors].
        /// </summary>
        internal static object[,] PlanRsm(int qty1, int level1, int qty2, int level2,
            bool randomize, long? seed)
        {
            if (qty1 < 0 || qty2 < 0)
                throw new ArgumentException(ErrorMsg.Get("DOE_NegativeQty"));
            int k = qty1 + qty2;
            if (k < 2)
                throw new ArgumentException(ErrorMsg.Get("DOE_RsmTooFewFactors", k));

            return Assemble(RsmCcd(k), randomize, seed);
        }

        // ─────────────────────────────────────────────────────────────
        // Box-Behnken (response surface, 3-level)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Box-Behnken response-surface design. Factor count = <paramref name="qty1"/>
        /// + <paramref name="qty2"/> (continuous factors; level parameters are ignored).
        /// Returns [runs + 1, 2 + factors].
        /// </summary>
        internal static object[,] PlanBb(int qty1, int level1, int qty2, int level2,
            bool randomize, long? seed)
        {
            if (qty1 < 0 || qty2 < 0)
                throw new ArgumentException(ErrorMsg.Get("DOE_NegativeQty"));
            int k = qty1 + qty2;
            if (k < 3)
                throw new ArgumentException(ErrorMsg.Get("DOE_BbTooFewFactors", k));

            return Assemble(RsmBb(k), randomize, seed);
        }

        // ─────────────────────────────────────────────────────────────
        // Coded matrices (exposed for CrossVal / tests)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Generate the coded design matrix for a full-factorial design over the
        /// given per-factor level counts. One row per run (standard order: the
        /// first factor varies fastest, matching Minitab/JMP and pyDOE2.fullfact),
        /// one column per factor, levels coded to [-1, +1].
        /// </summary>
        internal static double[,] FullFactorialCoded(int[] levels)
        {
            int k = levels.Length;
            if (k < 1)
                throw new ArgumentException(ErrorMsg.Get("DOE_NoFactors"));

            long total = 1;
            for (int f = 0; f < k; f++)
            {
                if (levels[f] < 1)
                    throw new ArgumentException(ErrorMsg.Get("DOE_InvalidLevel", f + 1, levels[f]));
                if (levels[f] > MaxRuns / total) // 防乘法溢出（levels[f] 巨大或 total 累积溢出 long）
                    throw new ArgumentException(ErrorMsg.Get("DOE_TooManyRuns", MaxRuns));
                total *= levels[f];
                if (total > MaxRuns)
                    throw new ArgumentException(ErrorMsg.Get("DOE_TooManyRuns", MaxRuns));
            }
            if (total * k > MaxCells)
                throw new ArgumentException(ErrorMsg.Get("DOE_TooManyCells", total, k, total * k, MaxCells));

            var m = new double[total, k];
            for (long r = 0; r < total; r++)
            {
                long rem = r;
                for (int f = 0; f < k; f++)
                {
                    int L = levels[f];
                    long j = rem % L;
                    rem /= L;
                    m[r, f] = Code(j, L);
                }
            }
            return m;
        }

        /// <summary>
        /// Generate the coded design matrix for a Taguchi orthogonal array. One row
        /// per run (standard order of the array), one column per factor (group 1
        /// factors first), levels coded to [-1, +1]. Exposed for CrossVal/tests.
        /// </summary>
        internal static double[,] TaguchiCoded(int qty1, int level1, int qty2, int level2)
        {
            if (qty1 < 0 || qty2 < 0)
                throw new ArgumentException(ErrorMsg.Get("DOE_NegativeQty"));
            int totalFactors = qty1 + qty2;
            if (totalFactors < 1)
                throw new ArgumentException(ErrorMsg.Get("DOE_NoFactors"));
            if (qty1 > 0 && level1 < 1)
                throw new ArgumentException(ErrorMsg.Get("DOE_InvalidLevel", 1, level1));
            if (qty2 > 0 && level2 < 1)
                throw new ArgumentException(ErrorMsg.Get("DOE_InvalidLevel", 2, level2));

            // Classify factors into 2-level and 3-level counts.
            int n2 = 0, n3 = 0;
            if (qty1 > 0)
            {
                if (level1 == 2) n2 += qty1;
                else if (level1 == 3) n3 += qty1;
                else throw new ArgumentException(ErrorMsg.Get("DOE_TaguchiLevelUnsupported", level1));
            }
            if (qty2 > 0)
            {
                if (level2 == 2) n2 += qty2;
                else if (level2 == 3) n3 += qty2;
                else throw new ArgumentException(ErrorMsg.Get("DOE_TaguchiLevelUnsupported", level2));
            }

            var (runs, twoCols, threeCols) = SelectOrthogonalArray(n2, n3);

            // Pick columns in factor order (group 1 first) and code to [-1, +1].
            var coded = new double[runs, totalFactors];
            int twoIdx = 0, threeIdx = 0;
            int f = 0;
            for (int i = 0; i < qty1; i++, f++)
                FillColumn(coded, f, level1 == 2 ? twoCols[twoIdx++] : threeCols[threeIdx++], level1);
            for (int i = 0; i < qty2; i++, f++)
                FillColumn(coded, f, level2 == 2 ? twoCols[twoIdx++] : threeCols[threeIdx++], level2);
            return coded;
        }

        private static void FillColumn(double[,] coded, int col, int[] levelIdx, int levels)
        {
            for (int r = 0; r < coded.GetLength(0); r++)
                coded[r, col] = Code(levelIdx[r], levels);
        }

        /// <summary>
        /// Generate the coded design matrix for a 2-level ½-fraction factorial design
        /// over <paramref name="k"/> factors. First k-1 factors are independent (full
        /// factorial in standard order, first factor varies fastest); the k-th factor
        /// is their product (Minitab default generator). Matches pyDOE2
        /// <c>fracfact</c> with generator <c>a b c … &lt;product&gt;</c>.
        /// </summary>
        internal static double[,] FractionalCoded(int k)
        {
            if (k < 4)
                throw new ArgumentException(ErrorMsg.Get("DOE_FractionalTooFewFactors", k));

            int indep = k - 1;
            // 防位移掩码回绕：1L<<indep 在 indep≥64 时按 63 掩码（1L<<83==1L<<19），守卫可被绕过。
            // 2^31 已远超 MaxRuns，indep≥31 直接判超限。
            long runs = indep >= 31 ? long.MaxValue : 1L << indep;
            if (runs > MaxRuns)
                throw new ArgumentException(ErrorMsg.Get("DOE_TooManyRuns", MaxRuns));
            if (runs * k > MaxCells)
                throw new ArgumentException(ErrorMsg.Get("DOE_TooManyCells", runs, k, runs * k, MaxCells));
            var m = new double[runs, k];
            for (long r = 0; r < runs; r++)
            {
                for (int f = 0; f < indep; f++)
                {
                    int v = (int)((r >> f) & 1);
                    m[r, f] = v == 0 ? -1.0 : 1.0;
                }
                double prod = 1.0;
                for (int f = 0; f < indep; f++) prod *= m[r, f];
                m[r, k - 1] = prod;
            }
            return m;
        }

        /// <summary>
        /// Generate the coded design matrix for a circumscribed, rotatable central
        /// composite design (CCD) over <paramref name="k"/> continuous factors.
        /// Structure: 2^k factorial points (±1), 4 center points, 2k axial points
        /// (±α where α = 2^(k/4)), 4 more center points. Matches pyDOE2
        /// <c>ccdesign(k, alpha='rotatable')</c> and Minitab's default rotatable CCD.
        /// </summary>
        internal static double[,] RsmCcd(int k)
        {
            if (k < 2)
                throw new ArgumentException(ErrorMsg.Get("DOE_RsmTooFewFactors", k));

            double alpha = Math.Pow(Math.Pow(2, k), 0.25); // rotatable: 2^(k/4)

            var levels = new int[k];
            for (int i = 0; i < k; i++) levels[i] = 2;
            var factorial = FullFactorialCoded(levels); // 2^k × k, coded ±1

            long nf = k >= 31 ? long.MaxValue : 1L << k; // 防位移回绕（同 FractionalCoded）
            int nAxial = 2 * k;
            const int centerPerBlock = 4;
            long total = nf + centerPerBlock + nAxial + centerPerBlock;
            if (total > MaxRuns)
                throw new ArgumentException(ErrorMsg.Get("DOE_TooManyRuns", MaxRuns));
            if (total * k > MaxCells)
                throw new ArgumentException(ErrorMsg.Get("DOE_TooManyCells", total, k, total * k, MaxCells));

            var m = new double[total, k];
            long row = 0;

            // 1. Factorial points (±1).
            for (long r = 0; r < nf; r++, row++)
                for (int c = 0; c < k; c++) m[row, c] = factorial[r, c];

            // 2. Center points (factorial block).
            for (int i = 0; i < centerPerBlock; i++, row++)
                for (int c = 0; c < k; c++) m[row, c] = 0.0;

            // 3. Axial points (±α, one per factor).
            for (int c = 0; c < k; c++)
                for (int s = 0; s < 2; s++, row++)
                {
                    for (int cc = 0; cc < k; cc++) m[row, cc] = 0.0;
                    m[row, c] = s == 0 ? -alpha : alpha;
                }

            // 4. Center points (axial block).
            for (int i = 0; i < centerPerBlock; i++, row++)
                for (int c = 0; c < k; c++) m[row, c] = 0.0;

            return m;
        }

        /// <summary>
        /// Generate the coded design matrix for a Box-Behnken design over
        /// <paramref name="k"/> continuous factors. For every factor pair (i,j)
        /// a 2² factorial (±1) is placed with all other factors at 0, followed by
        /// center points. Matches pyDOE2 <c>bbdesign(k)</c>.
        /// </summary>
        internal static double[,] RsmBb(int k)
        {
            if (k < 3)
                throw new ArgumentException(ErrorMsg.Get("DOE_BbTooFewFactors", k));

            var pairFactorial = FullFactorialCoded(new[] { 2, 2 }); // 4 × 2, coded ±1

            long nPairs = (long)k * (k - 1) / 2; // long 防 int 溢出（k 大时 k*(k-1) 溢出 int）
            long edgePoints = nPairs * 4;
            int center = BoxBehnkenCenterPoints(k);
            long total = edgePoints + center;
            if (total > MaxRuns)
                throw new ArgumentException(ErrorMsg.Get("DOE_TooManyRuns", MaxRuns));
            if (total * k > MaxCells)
                throw new ArgumentException(ErrorMsg.Get("DOE_TooManyCells", total, k, total * k, MaxCells));

            var m = new double[total, k];
            long row = 0;

            for (int i = 0; i < k - 1; i++)
                for (int j = i + 1; j < k; j++)
                    for (int r = 0; r < 4; r++, row++)
                    {
                        for (int c = 0; c < k; c++) m[row, c] = 0.0;
                        m[row, i] = pairFactorial[r, 0];
                        m[row, j] = pairFactorial[r, 1];
                    }

            for (int i = 0; i < center; i++, row++)
                for (int c = 0; c < k; c++) m[row, c] = 0.0;

            return m;
        }

        /// <summary>Default Box-Behnken center-point counts, matching pyDOE2 <c>bbdesign</c>.</summary>
        private static int BoxBehnkenCenterPoints(int k)
        {
            int[] points = { 0, 0, 0, 3, 3, 6, 6, 6, 8, 9, 10, 12, 12, 13, 14, 15, 16 };
            return k <= 16 ? points[k] : k;
        }

        // ─────────────────────────────────────────────────────────────
        // Output assembly (shared by all design methods)
        // ─────────────────────────────────────────────────────────────

        /// <summary>Wrap a coded matrix into a header + StdOrder + RunOrder table.</summary>
        private static object[,] Assemble(double[,] coded, bool randomize, long? seed)
        {
            long total = coded.GetLength(0);
            int factors = coded.GetLength(1);

            int cols = 2 + factors;
            var result = new object[total + 1, cols];
            result[0, 0] = "StdOrder";
            result[0, 1] = "RunOrder";
            for (int f = 0; f < factors; f++)
                result[0, 2 + f] = ColumnName(f);

            var stdOrder = new long[total];
            for (long r = 0; r < total; r++)
                stdOrder[r] = r + 1;

            // Run order: identity in standard order, shuffled when randomize=true.
            // A self-contained PRNG guarantees net48 and net8.0 produce identical
            // sequences for the same seed (System.Random differs across TFMs).
            var runOrder = (long[])stdOrder.Clone();
            if (randomize)
            {
                var rng = new XorShift64(seed.HasValue ? (ulong)seed.Value : NewSeed());
                for (long i = total - 1; i > 0; i--)
                {
                    long j = rng.NextLong(i + 1);
                    (runOrder[i], runOrder[j]) = (runOrder[j], runOrder[i]);
                }
            }

            for (long r = 0; r < total; r++)
            {
                result[r + 1, 0] = stdOrder[r];
                result[r + 1, 1] = runOrder[r];
                for (int f = 0; f < factors; f++)
                    result[r + 1, 2 + f] = coded[r, f];
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        // Orthogonal-array selection and construction
        // ─────────────────────────────────────────────────────────────

        /// <summary>Select the smallest orthogonal array accommodating n2 two-level and n3 three-level factors.</summary>
        private static (int runs, List<int[]> two, List<int[]> three) SelectOrthogonalArray(int n2, int n3)
        {
            if (n3 == 0)
            {
                if (n2 <= 3) { var c = Build2Level(2); return (4, c, new List<int[]>()); }
                if (n2 <= 7) { var c = Build2Level(3); return (8, c, new List<int[]>()); }
                if (n2 <= 11) { var c = BuildL12(); return (12, c, new List<int[]>()); }
                if (n2 <= 15) { var c = Build2Level(4); return (16, c, new List<int[]>()); }
                if (n2 <= 31) { var c = Build2Level(5); return (32, c, new List<int[]>()); }
                throw new ArgumentException(ErrorMsg.Get("DOE_TaguchiTooManyFactors", n2));
            }

            if (n2 == 0)
            {
                if (n3 <= 4) { var c = Build3Level(2); return (9, new List<int[]>(), c); }
                if (n3 <= 13) { var c = Build3Level(3); return (27, new List<int[]>(), c); }
                throw new ArgumentException(ErrorMsg.Get("DOE_TaguchiTooManyFactors", n3));
            }

            if (n2 <= 1 && n3 <= 7) { var (t2, t3) = BuildL18(); return (18, t2, t3); }
            throw new ArgumentException(ErrorMsg.Get("DOE_TaguchiMixedUnsupported", n2, n3));
        }

        /// <summary>
        /// 2-level orthogonal array L_{2^k}(2^{2^k-1}): k main-effect columns plus
        /// all XOR interaction columns, in standard Taguchi order
        /// (A, B, AB, C, AC, BC, ABC, D, ...). Returns level indices 0/1 per column.
        /// </summary>
        private static List<int[]> Build2Level(int k)
        {
            int runs = 1 << k;
            var mains = new int[k][];
            for (int b = 0; b < k; b++)
            {
                int bit = k - 1 - b; // A = most-significant bit (varies slowest)
                var col = new int[runs];
                for (int r = 0; r < runs; r++) col[r] = (r >> bit) & 1;
                mains[b] = col;
            }

            var cols = new List<int[]>(runs - 1) { mains[0] };
            for (int i = 1; i < k; i++)
            {
                cols.Add(mains[i]);
                int before = cols.Count - 1; // columns present before this main effect
                for (int j = 0; j < before; j++)
                {
                    var inter = new int[runs];
                    for (int r = 0; r < runs; r++) inter[r] = mains[i][r] ^ cols[j][r];
                    cols.Add(inter);
                }
            }
            return cols;
        }

        /// <summary>
        /// 3-level orthogonal array L_{3^k}(3^{(3^k-1)/2}): GF(3) main-effect columns
        /// followed by interaction columns. Returns level indices 0/1/2 per column.
        /// </summary>
        private static List<int[]> Build3Level(int k)
        {
            int runs = Pow3(k);
            var base_ = new int[k][];
            for (int b = 0; b < k; b++)
            {
                int div = Pow3(k - 1 - b);
                var col = new int[runs];
                for (int r = 0; r < runs; r++) col[r] = (r / div) % 3;
                base_[b] = col;
            }

            var cols = new List<int[]>((Pow3(k) - 1) / 2);
            // Main effects first (base_[0], base_[1], ...).
            for (int b = 0; b < k; b++) cols.Add(base_[b]);

            // Interaction columns: coefficient vectors (first non-zero = 1) with ≥ 2 non-zero entries.
            int total = Pow3(k);
            for (int code = 1; code < total; code++)
            {
                var rep = new int[k];
                int cc = code;
                for (int i = k - 1; i >= 0; i--) { rep[i] = cc % 3; cc /= 3; }

                int firstNonZero = 0, nonZeroCount = 0;
                for (int i = 0; i < k; i++)
                {
                    if (rep[i] != 0)
                    {
                        if (nonZeroCount == 0) firstNonZero = rep[i];
                        nonZeroCount++;
                    }
                }
                if (firstNonZero != 1 || nonZeroCount < 2) continue;

                var col = new int[runs];
                for (int r = 0; r < runs; r++)
                {
                    int sum = 0;
                    for (int i = 0; i < k; i++) sum += rep[i] * base_[i][r];
                    col[r] = sum % 3;
                }
                cols.Add(col);
            }
            return cols;
        }

        /// <summary>Plackett-Burman L12(2¹¹): 11 two-level columns, 12 runs.</summary>
        private static List<int[]> BuildL12()
        {
            int runs = 12;
            var gen = new[] { 1, 1, 0, 1, 1, 1, 0, 0, 0, 1, 0 }; // + = 1, - = 0
            var cols = new List<int[]>(11);
            for (int shift = 0; shift < 11; shift++)
            {
                var col = new int[runs];
                for (int r = 0; r < 11; r++) col[r] = gen[(shift + r) % 11];
                col[11] = 0; // last run all low
                cols.Add(col);
            }
            return cols;
        }

        /// <summary>Mixed L18(2¹×3⁷): one 2-level column + seven 3-level columns, 18 runs.</summary>
        private static (List<int[]> two, List<int[]> three) BuildL18()
        {
            var t = L18Table;
            int runs = t.GetLength(0);
            var two = new List<int[]>(1);
            var three = new List<int[]>(7);

            var col0 = new int[runs];
            for (int r = 0; r < runs; r++) col0[r] = t[r, 0];
            two.Add(col0);

            for (int c = 1; c < 8; c++)
            {
                var col = new int[runs];
                for (int r = 0; r < runs; r++) col[r] = t[r, c];
                three.Add(col);
            }
            return (two, three);
        }

        /// <summary>Standard L18(2¹×3⁷) orthogonal array: col 0 is 2-level, cols 1-7 are 3-level.</summary>
        private static readonly int[,] L18Table = new int[18, 8]
        {
            {0,0,0,0,0,0,0,0}, {0,0,1,1,1,1,1,1}, {0,0,2,2,2,2,2,2},
            {0,1,0,0,1,1,2,2}, {0,1,1,1,2,2,0,0}, {0,1,2,2,0,0,1,1},
            {0,2,0,1,0,2,1,2}, {0,2,1,2,1,0,2,0}, {0,2,2,0,2,1,0,1},
            {1,0,0,2,2,1,1,0}, {1,0,1,0,0,2,2,1}, {1,0,2,1,1,0,0,2},
            {1,1,0,1,2,0,2,1}, {1,1,1,2,0,1,0,2}, {1,1,2,0,1,2,1,0},
            {1,2,0,2,1,2,0,1}, {1,2,1,0,2,0,1,2}, {1,2,2,1,0,1,2,0}
        };

        // ─────────────────────────────────────────────────────────────
        // Shared helpers
        // ─────────────────────────────────────────────────────────────

        private static int Pow3(int n)
        {
            int r = 1;
            for (int i = 0; i < n; i++) r *= 3;
            return r;
        }

        /// <summary>Map a 0-based level index to a coded value in [-1, +1].</summary>
        private static double Code(long levelIndex, int levels)
            => levels <= 1 ? 0.0 : (2.0 * levelIndex / (levels - 1)) - 1.0;

        /// <summary>Excel-style column name: 0→A, 25→Z, 26→AA.</summary>
        internal static string ColumnName(int index)
        {
            var sb = new System.Text.StringBuilder();
            int n = index + 1;
            while (n > 0)
            {
                int rem = (n - 1) % 26;
                sb.Insert(0, (char)('A' + rem));
                n = (n - 1) / 26;
            }
            return sb.ToString();
        }

        /// <summary>Entropy source for null seed (non-deterministic by design).</summary>
        private static ulong NewSeed()
            => (ulong)Environment.TickCount ^ ((ulong)DateTime.UtcNow.Ticks << 20);
    }

    /// <summary>
    /// Deterministic PRNG (xorshift64*) so a fixed seed yields the same run order
    /// on both net48 and net8.0 — System.Random's seeded algorithm differs across
    /// target frameworks, which would break reproducible run orders.
    /// </summary>
    internal struct XorShift64
    {
        private ulong _state;

        public XorShift64(ulong seed)
            => _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;

        public ulong Next()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return x * 0x2545F4914F6CDD1DUL;
        }

        public long NextLong(long bound)
        {
            if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound));
            return (long)(Next() % (ulong)bound);
        }
    }
}
