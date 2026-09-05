using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// Linear algebra: SVD, QR, LU, Cholesky, PINV, Eigen.
    /// Ported from LinearUtils.bas. Backed by MathNet.Numerics.
    /// </summary>
    internal static class LinalgCore
    {
        /// <summary>
        /// Lightweight decomposition cache. Avoids recomputing SVD/QR/LU
        /// when individual matrix accessors (SVD_U, SVD_S, SVD_VT, etc.)
        /// are called consecutively with the same input in Excel.
        /// Thread-safe; max 32 entries with LRU eviction.
        /// </summary>
        private static class DecompCache
        {
            private static readonly Dictionary<string, (object Value, LinkedListNode<string> Node, long Elems)> Store = new();
            private static readonly LinkedList<string> LruList = new(); // front = LRU, back = MRU
            private static readonly object Lock = new();
            private const int MaxEntries = 32;
            // review 2026-08-31（深度审查 P2-34）：原按条目数（32）限流——单条 2000×2000 SVD
            // ≈ 64MB，32 条 ≈ 2GB。改为按累计元素数限流（2000 万元素 ≈ 160MB），
            // 大矩阵条目数更少但总内存有界。
            private const long MaxTotalElems = 20_000_000;

            internal static T GetOrAdd<T>(string key, Func<T> factory)
            {
                // Fast path: check cache without blocking other keys
                lock (Lock)
                {
                    if (Store.TryGetValue(key, out var entry))
                    {
                        // Move to back (MRU) — O(1) with LinkedList
                        LruList.Remove(entry.Node);
                        LruList.AddLast(entry.Node);
                        return (T)entry.Value;
                    }
                }

                // Slow path: compute outside lock so concurrent callers
                // for different keys are not serialised by decomposition cost
                var result = factory();
                // review 2026-09-05（R10/CS8604）：泛型 T 无约束，编译器认为 factory() 可能为
                // null；但所有 decomp 工厂（Svd/Qr/Lu 及其包装）均返回非 null 数组（见各方法
                // 签名与调用点），此处不可能为 null —— 显式 `!` 标注该契约。
                long elems = ElementCount(result!);

                lock (Lock)
                {
                    // Double-check: another thread may have computed the same key
                    if (Store.TryGetValue(key, out var entry))
                    {
                        LruList.Remove(entry.Node);
                        LruList.AddLast(entry.Node);
                        return (T)entry.Value;
                    }

                    // Evict LRU entries until both count and total-element budgets fit.
                    long total = 0;
                    foreach (var kv in Store) total += kv.Value.Elems;
                    while (Store.Count > 0 &&
                           (Store.Count >= MaxEntries || total + elems > MaxTotalElems))
                    {
                        var oldest = LruList.First!;
                        LruList.RemoveFirst();
                        total -= Store[oldest.Value].Elems;
                        Store.Remove(oldest.Value);
                    }

                    var node = LruList.AddLast(key);
                    Store[key] = (result!, node, elems);
                    return result;
                }
            }

            private static long ElementCount(object value) => value switch
            {
                double[,] m2 => (long)m2.GetLength(0) * m2.GetLength(1),
                double[] v1 => v1.Length,
                _ => 1,
            };

            /// <summary>
            /// Clear all cached decompositions. Called on add-in unload to release
            /// references to MathNet types before the AssemblyLoadContext is unloaded.
            /// Thread-safe.
            /// </summary>
            internal static void Clear()
            {
                lock (Lock)
                {
                    Store.Clear();
                    LruList.Clear();
                }
            }

            /// <summary>Content-based hash of a 2D double array.
            /// Hashes every element for correctness — the decomposition cost
            /// (SVD/LU/QR) dominates by orders of magnitude, so full hashing
            /// has negligible overhead.</summary>
            internal static string MatrixHash(double[,] m)
            {
                int rows = m.GetLength(0), cols = m.GetLength(1);
                unchecked
                {
                    // 128-bit hash (two independent 64-bit FNV-1a) to minimize collision risk
                    long h1 = unchecked((long)14695981039346656037); // FNV offset basis
                    long h2 = unchecked((long)14695981039346656037);
                    const long prime1 = unchecked((long)1099511628211); // FNV prime
                    const long prime2 = unchecked((long)6364136223846793005); // secondary prime
                    h1 = (h1 ^ rows) * prime1;
                    h2 = (h2 ^ cols) * prime2;
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            long bits = BitConverter.DoubleToInt64Bits(m[r, c]);
                            h1 = (h1 ^ bits) * prime1;
                            h2 = (h2 ^ (bits >> 16 ^ bits)) * prime2;
                        }
                    }
                    return $"{h1:X16}{h2:X16}_{rows}x{cols}";
                }
            }
        }

        internal static (double[,] U, double[] S, double[,] Vt) Svd(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            var A = Matrix<double>.Build.DenseOfArray(m);
            var svd = A.Svd(computeVectors: true);
            int rows = A.RowCount, cols = A.ColumnCount, k = Math.Min(rows, cols);
            return (svd.U.SubMatrix(0, rows, 0, k).ToArray(),
                    svd.S.ToArray(),
                    svd.VT.SubMatrix(0, k, 0, cols).ToArray());
        }

        internal static double[,] PseudoInverse(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            var A = Matrix<double>.Build.DenseOfArray(m);
            return A.PseudoInverse().ToArray();
        }

        internal static (double[,] Q, double[,] R) Qr(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            int rows = m.GetLength(0), cols = m.GetLength(1);
            if (rows >= cols)
            {
                // Tall or square: MathNet QR directly supported.
                var A = Matrix<double>.Build.DenseOfArray(m);
                var qr = A.QR(QRMethod.Full);
                return (qr.Q.SubMatrix(0, rows, 0, cols).ToArray(),
                        qr.R.SubMatrix(0, cols, 0, cols).ToArray());
            }
            // Wide (rows < cols): MathNet QR requires m ≥ n.
            // Zero-padding to a square matrix and extracting sub-matrices does NOT
            // produce a valid QR factorisation (Q_sub is not orthogonal and
            // Q_sub * R_sub ≠ A).  Throw instead of silently returning wrong results.
            throw new NotSupportedException(
                $"QR decomposition requires rows >= columns, but input has {rows} rows and {cols} columns. " +
                "For wide matrices, use SVD (LINALG.SVD_*) for a full decomposition, " +
                "or transpose the input (LINALG.TRANSPOSE) to compute the tall-skinny QR.");
        }

        internal static (double[,] L, double[,] U, double[,] P) Lu(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            var A = Matrix<double>.Build.DenseOfArray(m);
            var lu = A.LU();
            // perm[i] = row index of original A that ends up at row i of the permuted matrix.
            // Build P element-wise: P[i, perm[i]] = 1.0 avoids the swap-in-place bug
            // where cycling permutations (length > 2) would overwrite previously placed rows.
            var perm = lu.P;
            var P = Matrix<double>.Build.Dense(A.RowCount, A.RowCount);
            for (int i = 0; i < A.RowCount; i++)
                P[i, perm[i]] = 1.0;
            return (lu.L.ToArray(), lu.U.ToArray(), P.ToArray());
        }

        internal static double Determinant(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            return Matrix<double>.Build.DenseOfArray(m).Determinant();
        }

        internal static double[] Solve(double[,] A, double[] b)
        {
            NumericGuard.AgainstNonFinite(A);
            if (b.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                throw new ArgumentException(ErrorMsg.Get("LINALG_RhsNotFinite"));
            var matA = Matrix<double>.Build.DenseOfArray(A);
            // review 2026-09-05（R21）：原仅输出侧拦 NaN/Inf——近奇异系统（cond→1e16）经
            // MathNet LU 静默返回错得离谱但全部有限的解（历史 P0-1 同族：条件数主导精度）。
            // 求解前加条件数守卫：cond 非有限（精确奇异）或 > 1e14 → 显式拒绝。1e14 与
            // double 16 位有效数字对应，超过后解的有效位数不足 2 位，必然不可靠。
            // SVD 仅在此入口执行一次（n 通常小，成本可接受）；消息含实测 cond 值便于诊断。
            // 注意：消息含 "singular"——精确奇异用例的既有断言（WithMessage("*singular*")）
            // 现由此守卫先行触发，保持契约不破。
            var svd = matA.Svd(computeVectors: false);
            double cond = svd.ConditionNumber;
            if (double.IsNaN(cond) || double.IsInfinity(cond) || cond > 1e14)
                throw new ArgumentException(
                    "Matrix is singular or too ill-conditioned for a reliable solution " +
                    $"(condition number = {cond.ToString("E3", System.Globalization.CultureInfo.InvariantCulture)}; " +
                    "guard threshold 1e14). Use LINALG.PINV for singular systems.");
            var x = matA.Solve(Vector<double>.Build.Dense(b));
            var arr = x.ToArray();
            // P2 (pre-release review): MathNet Solve silently returns NaN/±Inf for singular
            // systems; the api-reference contract says singular → #VALUE! (guard, not
            // silent propagation — 防错原则1)。R21 守卫后保留为纵深防御（良态系统不应到达）。
            for (int i = 0; i < arr.Length; i++)
                if (double.IsNaN(arr[i]) || double.IsInfinity(arr[i]))
                    throw new ArgumentException(ErrorMsg.Get("LINALG_SingularMatrix"));
            return arr;
        }

        internal static double[,] Cholesky(double[,] m)
        {
            // review 2026-09-05（N04）：与 Eigenvalues/Eigen 同族对齐——MathNet Cholesky 只读
            // 三角，非对称输入（如 {1,0.5;0,1}）原先被静默按 {1,0;0.5,1} 分解（错误结果）。
            // 复用 EnsureSymmetric（含方阵检查、非有限守卫、相对对称判据），与 Eigen 同一
            // 拒绝路径。
            EnsureSymmetric(m, "Cholesky decomposition");
            return Matrix<double>.Build.DenseOfArray(m).Cholesky().Factor.ToArray();
        }

        /// <summary>
        /// Real eigenvalues via symmetric eigenvalue decomposition (Evd).
        /// The input matrix MUST be approximately symmetric (|aᵢⱼ − aⱼᵢ| ≤ 1e-8).
        /// MathNet's Evd is defined only for symmetric/Hermitian matrices;
        /// non-symmetric input is rejected rather than returning silently wrong values.
        /// </summary>
        internal static double[] Eigenvalues(double[,] m)
        {
            EnsureSymmetric(m);
            return Matrix<double>.Build.DenseOfArray(m).Evd().EigenValues.Real().ToArray();
        }

        /// <summary>
        /// Real eigenvalues and eigenvectors via symmetric decomposition.
        /// Same symmetry requirement as <see cref="Eigenvalues"/>.
        /// </summary>
        internal static (double[] values, double[,] vectors) Eigen(double[,] m)
        {
            EnsureSymmetric(m);
            var evd = Matrix<double>.Build.DenseOfArray(m).Evd();
            return (evd.EigenValues.Real().ToArray(), evd.EigenVectors.ToArray());
        }

        /// <summary>
        /// Reject non-square, non-finite, or non-symmetric input (relative tolerance).
        /// Shared by the symmetric-only decomposition family (Evd, Cholesky).
        /// </summary>
        /// <param name="m">Input matrix.</param>
        /// <param name="op">Operation name used in the error message.</param>
        private static void EnsureSymmetric(double[,] m, string op = "Eigenvalue decomposition (Evd)")
        {
            int n = m.GetLength(0);
            if (n != m.GetLength(1))
                throw new ArgumentException(ErrorMsg.Get("LINALG_EigenNotSquare", n, m.GetLength(1)));
            NumericGuard.AgainstNonFinite(m); // Replaces inline NaN/Inf scan
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    // review 2026-08-31（深度审查 P1-5）：原绝对阈值 1e-8 在 1e9 量级矩阵下
                    // ULP≈1.2e-7 > 1e-8，理论对称矩阵因浮点舍入被误判为非对称。改为相对判据
                    // （阈值随元素量级缩放，1e9 量级 → ≈1e1，远大于 ULP；小矩阵保持原 1e-8 行为）。
                    // review 2026-09-05（R05）：scale 原带 `Math.Max(1.0, …)` 下限——小量纲矩阵
                    // （如 [[0,0],[1e-9,0]]，相对 100% 非对称）退化为绝对阈值 diff<1e-8 → 误判
                    // 对称 → Evd 静默按错误矩阵分解。改纯相对判据 scale = max(|aij|,|aji|)；
                    // 全零对称对 diff=0、scale=0 → `0 > 0` 不触发（判据无除法，无除零风险）。
                    double diff = Math.Abs(m[i, j] - m[j, i]);
                    double scale = Math.Max(Math.Abs(m[i, j]), Math.Abs(m[j, i]));
                    if (diff > 1e-8 * scale)
                        throw new ArgumentException(
                            $"Matrix is not symmetric: |m[{i},{j}] − m[{j},{i}]| = {diff:E2} > {1e-8 * scale:E2}. " +
                            $"{op} requires a symmetric matrix.");
                }
            }
        }

        internal static double ConditionNumber(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            // review 2026-09-05（N05）：奇异矩阵 cond=+∞ 原样返回，违反模块 Inf→NaN 输出
            // 封顶约定（对齐 Sum/Range/CapNaN 的写法）。封顶为 NaN，语义 = "条件数不可表示"。
            var cond = Matrix<double>.Build.DenseOfArray(m).ConditionNumber();
            return double.IsInfinity(cond) ? double.NaN : cond;
        }

        internal static int Rank(double[,] m, double tol = 0)
        {
            NumericGuard.AgainstNonFinite(m);
            var A = Matrix<double>.Build.DenseOfArray(m);
            var svd = A.Svd(computeVectors: false);
            // Use relative tolerance (MATLAB/numpy convention) when tol <= 0
            double effectiveTol = tol > 0
                ? tol
                : (svd.S.Count > 0 ? svd.S.Maximum() * Math.Max(m.GetLength(0), m.GetLength(1)) * 1e-16 : 1e-10);
            return svd.S.Count(s => s > effectiveTol);
        }

        internal static double NormFrobenius(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            // review 2026-08-31（深度审查 P2-35）：MathNet FrobeniusNorm 朴素平方和——
            // [[1e200,1e200]] → 1e400 溢出 Inf（真值 1.41e200 可表示，实测确认）。
            // 尺度化：先取最大 |x| 归一再平方求和，避免中间溢出。
            double max = 0.0;
            for (int r = 0; r < m.GetLength(0); r++)
                for (int c = 0; c < m.GetLength(1); c++)
                    max = Math.Max(max, Math.Abs(m[r, c]));
            if (max == 0) return 0.0;
            double s = 0.0;
            for (int r = 0; r < m.GetLength(0); r++)
                for (int c = 0; c < m.GetLength(1); c++)
                {
                    double t = m[r, c] / max;
                    s += t * t;
                }
            return max * Math.Sqrt(s);
        }

        internal static double[,] Identity(int n)
        {
            // review 2026-08-29：上限 10000 → DenseIdentity(10000).ToArray() = 800MB，
            // 32 位 Excel 单公式 OOM 风险。收紧至 2000（32MB）。
            if (n < 0 || n > 2_000)
                throw new ArgumentException(
                    $"Identity matrix size must be between 0 and 2000 (got {n}).");
            return Matrix<double>.Build.DenseIdentity(n).ToArray();
        }

        internal static double[,] Diagonal(double[] v)
        {
            for (int i = 0; i < v.Length; i++)
                if (double.IsNaN(v[i]) || double.IsInfinity(v[i]))
                    throw new ArgumentException(
                        $"Diagonal array contains non-finite value at index {i}.");
            return Matrix<double>.Build.DenseOfDiagonalArray(v).ToArray();
        }

        internal static double[,] MatMul(double[,] A, double[,] B)
        {
            NumericGuard.AgainstNonFinite(A);
            NumericGuard.AgainstNonFinite(B);
            return (Matrix<double>.Build.DenseOfArray(A) * Matrix<double>.Build.DenseOfArray(B)).ToArray();
        }

        internal static double[,] Transpose(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            return Matrix<double>.Build.DenseOfArray(m).Transpose().ToArray();
        }

        internal static double Trace(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            return Matrix<double>.Build.DenseOfArray(m).Trace();
        }

        // ── Cached decomposition accessors ──────────────────────────
        // Each returns one component of a decomposition. The full result
        // is cached on first access so consecutive calls (e.g. SVD_U +
        // SVD_S + SVD_VT in Excel) only compute the decomposition once.

        /// <summary>
        /// Clear the decomposition cache. Safe to call at any time;
        /// subsequent UDF calls will recompute decompositions as needed.
        /// Called by <see cref="AddIn.AutoClose"/> on add-in unload.
        /// </summary>
        internal static void ClearDecompCache() => DecompCache.Clear();

        /// <summary>
        /// 128-bit content hash of a 2D double array (two independent FNV-1a streams
        /// + dimension suffix), shared by the decomposition cache and the async RTD
        /// topic keys. A single 64-bit hash would make an ExcelAsyncUtil.Run key
        /// collision silently return another matrix's cached result (R24).
        /// </summary>
        internal static string MatrixHash(double[,] m) => DecompCache.MatrixHash(m);

        /// <summary>128-bit content hash of a double vector — same dual-FNV-1a scheme
        /// as <see cref="MatrixHash"/> (length participates in both streams).</summary>
        internal static string VectorHash(double[] v)
        {
            unchecked
            {
                long h1 = unchecked((long)14695981039346656037); // FNV offset basis
                long h2 = unchecked((long)14695981039346656037);
                const long prime1 = unchecked((long)1099511628211); // FNV prime
                const long prime2 = unchecked((long)6364136223846793005); // secondary prime
                h1 = (h1 ^ v.Length) * prime1;
                h2 = (h2 ^ v.Length) * prime2;
                for (int i = 0; i < v.Length; i++)
                {
                    long bits = BitConverter.DoubleToInt64Bits(v[i]);
                    h1 = (h1 ^ bits) * prime1;
                    h2 = (h2 ^ (bits >> 16 ^ bits)) * prime2;
                }
                return $"V{v.Length}:{h1:X16}{h2:X16}";
            }
        }

        private static TResult GetDecompPart<TDecomp, TResult>(
            double[,] m, string prefix,
            Func<double[,], TDecomp> decomp, Func<TDecomp, TResult> select)
        {
            var key = DecompCache.MatrixHash(m);
            return select(DecompCache.GetOrAdd(prefix + key, () => decomp(m)));
        }

        internal static double[,] SvdU(double[,] m) => GetDecompPart(m, "svd:", Svd, d => d.U);
        internal static double[]   SvdS(double[,] m) => GetDecompPart(m, "svd:", Svd, d => d.S);
        internal static double[,] SvdVt(double[,] m) => GetDecompPart(m, "svd:", Svd, d => d.Vt);
        internal static double[,] QrQ(double[,] m) => GetDecompPart(m, "qr:", Qr, d => d.Q);
        internal static double[,] QrR(double[,] m) => GetDecompPart(m, "qr:", Qr, d => d.R);
        internal static double[,] LuL(double[,] m) => GetDecompPart(m, "lu:", Lu, d => d.L);
        internal static double[,] LuU(double[,] m) => GetDecompPart(m, "lu:", Lu, d => d.U);
        internal static double[,] LuP(double[,] m) => GetDecompPart(m, "lu:", Lu, d => d.P);
    }
}