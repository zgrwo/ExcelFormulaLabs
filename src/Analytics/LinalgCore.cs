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
        internal static class DecompCache
        {
            private static readonly Dictionary<string, (object Value, LinkedListNode<string> Node, long Elems)> Store = new();
            private static readonly LinkedList<string> LruList = new(); // front = LRU, back = MRU
            private static readonly object Lock = new();
            internal const int MaxEntries = 32;
            // 按累计元素数限流（2000 万元素 ≈ 160MB）：单条 2000×2000 SVD ≈ 64MB，
            // 按条目数（32）限流可达 ≈2GB；元素预算使大矩阵条目数更少但总内存有界。
            internal const long MaxTotalElems = 20_000_000;
            /// <summary>Effective element budget; tests may lower it to exercise LRU eviction
            /// without allocating 20M-element matrices. Defaults to <see cref="MaxTotalElems"/>.</summary>
            internal static long TotalElementBudget = MaxTotalElems;

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
                // 泛型 T 无约束，编译器认为 factory() 可能为 null；但所有 decomp 工厂
                // （Svd/Qr/Lu 及其包装）均返回非 null 数组（见各方法签名与调用点），
                // 此处不可能为 null —— 显式 `!` 标注该契约。
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

                    // 单条目自身超 MaxTotalElems 时直接放弃缓存该条目：下面的 while 会
                    // 清空整个缓存后仍插入（与"总内存有界"口径不符）。结果照常返回
                    // （下次访问重算）。当前最大合法条目 2000 万元素 ≈160MB。
                    if (elems > TotalElementBudget)
                    {
                        return result;
                    }

                    // Evict LRU entries until both count and total-element budgets fit.
                    long total = 0;
                    foreach (var kv in Store) total += kv.Value.Elems;
                    while (Store.Count > 0 &&
                           (Store.Count >= MaxEntries || total + elems > TotalElementBudget))
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

            // 三个分解元组须逐分量求和的显式 case：`_ => 1` 会把 SVD/QR/LU 的元组结果
            // 计为 1 元素 → 20M 元素预算失效（600×600 SVD 真实 ≈72 万元素，最坏 ~2GB
            // 缓存 → 32 位 Excel OOM）。
            internal static long ElementCount(object value) => value switch
            {
                double[,] m2 => (long)m2.GetLength(0) * m2.GetLength(1),
                double[] v1 => v1.Length,
                (double[,] u, double[] s, double[,] vt) => (long)u.Length + s.Length + (long)vt.Length,
                (double[,] q, double[,] r) => (long)q.Length + (long)r.Length,
                (double[,] l, double[,] u2, double[,] p2) => (long)l.Length + (long)u2.Length + (long)p2.Length,
                _ => 1,
            };

            /// <summary>Test hook: current entry count and accounted element total.
            /// Used by LinalgCoreTests to verify the element budget and LRU eviction.</summary>
            internal static (int Count, long TotalElems) Snapshot()
            {
                lock (Lock)
                {
                    long total = 0;
                    foreach (var kv in Store) total += kv.Value.Elems;
                    return (Store.Count, total);
                }
            }

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

        // ── 尺度归一化（分解族公共前置） ──────────────────────────────
        // MathNet/LAPACK 在列平方和 > DBL_MAX（|元素| > √DBL_MAX ≈ 1.34e154）时溢出：
        // QR 返回 Q=0/全零、R=-∞，SVD/Evd 抛 NonConvergence 或内部越界；≲1e-155
        // 的小量纲同理下溢。各分解对公共正缩放 c = maxAbs 有解析回缩关系，
        // 归一化到 |A'| ≤ 1 后分解再回缩即可无损覆盖全量纲，c=1 时位级不变：
        //   QR:  Q(cA)=Q(A),   R(cA)=c·R(A)
        //   SVD: U(cA)=U(A),   S(cA)=c·S(A),  Vt(cA)=Vt(A)
        //   LU:  L(cA)=c·L(A), U(cA)=U(A),    P(cA)=P(A)
        //   Evd: V(cA)=V(A),   λ(cA)=c·λ(A)
        //   Cholesky: L(cA)=√c·L(A)
        //   PInv(cA)=PInv(A)/c；Cond/Rank 不变；Solve(A,b)=Solve(cA,cb)
        // 全零矩阵 c=0：不缩放（分解结果平凡，缩放因子无意义）。
        /// <summary>逐元素最大绝对值；空矩阵/全零 → 0。</summary>
        private static double MaxAbs(double[,] m)
        {
            double max = 0;
            for (int r = 0; r < m.GetLength(0); r++)
                for (int c = 0; c < m.GetLength(1); c++)
                {
                    double a = Math.Abs(m[r, c]);
                    if (a > max) max = a;
                }
            return max;
        }

        /// <summary>返回 m/c（c=maxAbs）；c∈{0,1} 时原样返回（调用方不修改该数组）。</summary>
        private static double[,] NormalizedCopy(double[,] m, double c)
        {
            if (c == 0 || c == 1) return m;
            int rows = m.GetLength(0), cols = m.GetLength(1);
            var o = new double[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int col = 0; col < cols; col++)
                    o[r, col] = m[r, col] / c;
            return o;
        }

        /// <summary>逐元素乘 factor（返回新数组）。</summary>
        private static double[,] Multiply(double[,] m, double factor)
        {
            int rows = m.GetLength(0), cols = m.GetLength(1);
            var o = new double[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    o[r, c] = m[r, c] * factor;
            return o;
        }

        /// <summary>逐元素乘 factor（返回新数组）。</summary>
        private static double[] Multiply(double[] v, double factor)
        {
            var o = new double[v.Length];
            for (int i = 0; i < v.Length; i++) o[i] = v[i] * factor;
            return o;
        }

        /// <summary>逐元素乘 factor（原地，用于分解输出的新数组）。</summary>
        private static void MultiplyInPlace(double[] v, double factor)
        {
            if (factor == 1) return;
            for (int i = 0; i < v.Length; i++) v[i] *= factor;
        }

        /// <summary>Inf → NaN 封顶（模块输出约定，对齐 MatMul/NormFrobenius）。</summary>
        private static void CapInfToNaN(double[] v)
        {
            for (int i = 0; i < v.Length; i++)
                if (double.IsInfinity(v[i])) v[i] = double.NaN;
        }

        /// <summary>Inf → NaN 封顶（模块输出约定）。</summary>
        private static void CapInfToNaN(double[,] m)
        {
            for (int r = 0; r < m.GetLength(0); r++)
                for (int c = 0; c < m.GetLength(1); c++)
                    if (double.IsInfinity(m[r, c])) m[r, c] = double.NaN;
        }

        /// <summary>MathNet SVD 包装：非收敛/内部错误 → 显式参数错误（#VALUE! 语义）。</summary>
        private static Svd<double> SvdOf(Matrix<double> a, bool computeVectors)
        {
            try { return a.Svd(computeVectors); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                throw new ArgumentException(
                    $"SVD failed ({ex.GetType().Name}: {ex.Message}) for this input.", ex);
            }
        }

        /// <summary>MathNet Evd 包装：非收敛/内部越界 → 显式参数错误（#VALUE! 语义）。</summary>
        private static Evd<double> EvdOf(Matrix<double> a)
        {
            try { return a.Evd(); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                throw new ArgumentException(
                    $"Eigenvalue decomposition failed ({ex.GetType().Name}: {ex.Message}) for this input.", ex);
            }
        }

        internal static (double[,] U, double[] S, double[,] Vt) Svd(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            // 0×0/空维矩阵须显式 ArgumentException → #VALUE!：否则落 MathNet 内部
            // IndexOutOfRangeException（裸 CLR 异常）。
            if (m.GetLength(0) == 0 || m.GetLength(1) == 0)
                throw new ArgumentException("SVD requires a non-empty matrix.");
            double c = MaxAbs(m);
            var A = Matrix<double>.Build.DenseOfArray(NormalizedCopy(m, c));
            var svd = SvdOf(A, computeVectors: true);
            int rows = A.RowCount, cols = A.ColumnCount, k = Math.Min(rows, cols);
            var s = svd.S.ToArray();
            MultiplyInPlace(s, c); // S(cA)=c·S(A)
            var u = svd.U.SubMatrix(0, rows, 0, k).ToArray();
            var vt = svd.VT.SubMatrix(0, k, 0, cols).ToArray();
            CapInfToNaN(s);
            CapInfToNaN(u);
            CapInfToNaN(vt);
            return (u, s, vt);
        }

        internal static double[,] PseudoInverse(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            if (m.GetLength(0) == 0 || m.GetLength(1) == 0)
                throw new ArgumentException("Pseudo-inverse requires a non-empty matrix.");
            double c = MaxAbs(m);
            var A = Matrix<double>.Build.DenseOfArray(NormalizedCopy(m, c));
            var p = A.PseudoInverse().ToArray();
            if (c > 0) p = Multiply(p, 1.0 / c); // (cA)⁺ = A⁺/c
            CapInfToNaN(p);
            return p;
        }

        internal static (double[,] Q, double[,] R) Qr(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            int rows = m.GetLength(0), cols = m.GetLength(1);
            if (rows == 0 || cols == 0)
                throw new ArgumentException("QR decomposition requires a non-empty matrix.");
            if (rows >= cols)
            {
                // Tall or square: MathNet QR directly supported.
                double c = MaxAbs(m);
                var A = Matrix<double>.Build.DenseOfArray(NormalizedCopy(m, c));
                var qr = A.QR(QRMethod.Full);
                var q = qr.Q.SubMatrix(0, rows, 0, cols).ToArray();
                var r = Multiply(qr.R.SubMatrix(0, cols, 0, cols).ToArray(), c); // R(cA)=c·R(A)
                CapInfToNaN(q);
                CapInfToNaN(r);
                return (q, r);
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
            int mRows = m.GetLength(0), mCols = m.GetLength(1);
            if (mRows == 0 || mCols == 0)
                throw new ArgumentException("LU decomposition requires a non-empty matrix.");
            double c = MaxAbs(m);
            var A = Matrix<double>.Build.DenseOfArray(NormalizedCopy(m, c));
            var lu = A.LU();
            // perm[i] = row index of original A that ends up at row i of the permuted matrix.
            // Build P element-wise: P[i, perm[i]] = 1.0 avoids the swap-in-place bug
            // where cycling permutations (length > 2) would overwrite previously placed rows.
            var perm = lu.P;
            var P = Matrix<double>.Build.Dense(A.RowCount, A.RowCount);
            for (int i = 0; i < A.RowCount; i++)
                P[i, perm[i]] = 1.0;
            // A=P·L·U ⇒ cA=P·L·(cU)：保持 L 单位下三角（既有文档/测试约定），
            // 仅对 U 回缩；cU 真值不可表示时由 CapInfToNaN 封顶。
            var L = lu.L.ToArray();
            var U = Multiply(lu.U.ToArray(), c);
            CapInfToNaN(L);
            CapInfToNaN(U);
            return (L, U, P.ToArray());
        }

        internal static double Determinant(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            var r = Matrix<double>.Build.DenseOfArray(m).Determinant();
            // 溢出真值不可表示 → NaN 封顶（模块约定，对齐 COND/Sum）。
            return double.IsInfinity(r) ? double.NaN : r;
        }

        internal static double[] Solve(double[,] A, double[] b)
        {
            NumericGuard.AgainstNonFinite(A);
            // 空矩阵/非方阵显式拒绝（MathNet 对空矩阵直接抛裸 CLR 异常）。
            int an = A.GetLength(0), am = A.GetLength(1);
            if (an == 0 || am == 0)
                throw new ArgumentException("Solve requires a non-empty coefficient matrix.");
            if (an != am)
                throw new ArgumentException($"Solve requires a square matrix (got {an}×{am}).");
            if (b.Length != an)
                throw new ArgumentException(
                    $"Right-hand side length ({b.Length}) must equal the matrix size ({an}).");
            if (b.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                throw new ArgumentException(ErrorMsg.Get("LINALG_RhsNotFinite"));
            // 仅输出侧拦 NaN/Inf 不够：近奇异系统（cond→1e16）经 MathNet LU 会静默返回
            // 错得离谱但全部有限的解（条件数主导精度）。求解前加条件数守卫：cond 非有限
            // （精确奇异）或 > 1e14 → 显式拒绝。1e14 与 double 16 位有效数字对应，超过后
            // 解的有效位数不足 2 位，必然不可靠。条件数对公共正缩放不变，故在归一化域判定。
            double c = MaxAbs(A);
            var matA = Matrix<double>.Build.DenseOfArray(NormalizedCopy(A, c));
            // SVD 仅在此入口执行一次（n 通常小，成本可接受）；消息含实测 cond 值便于诊断。
            // 注意：消息含 "singular"——精确奇异用例的既有断言（WithMessage("*singular*")）
            // 由此守卫先行触发，保持契约不破。
            var svd = SvdOf(matA, computeVectors: false);
            double cond = svd.ConditionNumber;
            if (double.IsNaN(cond) || double.IsInfinity(cond) || cond > 1e14)
                throw new ArgumentException(
                    "Matrix is singular or too ill-conditioned for a reliable solution " +
                    $"(condition number = {cond.ToString("E3", System.Globalization.CultureInfo.InvariantCulture)}; " +
                    "guard threshold 1e14). Use LINALG.PINV for singular systems.");
            // 解的尺度不变性：x = (cA)⁻¹(cb)。b 与 A 同缩 c（c = maxAbs(A)），
            // 避免 A 大量纲时 LU 内部溢出（diag(1e308) 本可精确求解）。
            var rhs = c == 0 || c == 1 ? b : Multiply(b, 1.0 / c);
            var x = matA.Solve(Vector<double>.Build.Dense(rhs));
            var arr = x.ToArray();
            // MathNet Solve silently returns NaN/±Inf for singular systems; the api-reference
            // contract says singular → #VALUE! (guard, not silent propagation — 防错原则1)。
            // 条件数守卫之后仍保留为纵深防御（良态系统不应到达）。
            for (int i = 0; i < arr.Length; i++)
                if (double.IsNaN(arr[i]) || double.IsInfinity(arr[i]))
                    throw new ArgumentException(ErrorMsg.Get("LINALG_SingularMatrix"));
            return arr;
        }

        internal static double[,] Cholesky(double[,] m)
        {
            // 与 Eigenvalues/Eigen 同族对齐：MathNet Cholesky 只读三角，非对称输入
            // （如 {1,0.5;0,1}）会被静默按 {1,0;0.5,1} 分解（错误结果）。
            // 复用 EnsureSymmetric（含方阵检查、非有限守卫、相对对称判据），与 Eigen 同一
            // 拒绝路径。
            EnsureSymmetric(m, "Cholesky decomposition");
            double c = MaxAbs(m);
            var A = Matrix<double>.Build.DenseOfArray(NormalizedCopy(m, c));
            // L(cA)=√c·L(A)（A=LLᵀ ⇒ cA=(√c L)(√c L)ᵀ）；√c 恒有限。
            var L = Multiply(A.Cholesky().Factor.ToArray(), c > 0 ? Math.Sqrt(c) : 1.0);
            CapInfToNaN(L);
            return L;
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
            double c = MaxAbs(m);
            var A = Matrix<double>.Build.DenseOfArray(NormalizedCopy(m, c));
            var values = EvdOf(A).EigenValues.Real().ToArray();
            MultiplyInPlace(values, c); // λ(cA)=c·λ(A)
            CapInfToNaN(values);
            return values;
        }

        /// <summary>
        /// Real eigenvalues and eigenvectors via symmetric decomposition.
        /// Same symmetry requirement as <see cref="Eigenvalues"/>.
        /// </summary>
        internal static (double[] values, double[,] vectors) Eigen(double[,] m)
        {
            EnsureSymmetric(m);
            double c = MaxAbs(m);
            var A = Matrix<double>.Build.DenseOfArray(NormalizedCopy(m, c));
            var evd = EvdOf(A);
            var values = evd.EigenValues.Real().ToArray();
            MultiplyInPlace(values, c); // λ(cA)=c·λ(A)；特征向量 V(cA)=V(A) 不缩放
            CapInfToNaN(values);
            var vectors = evd.EigenVectors.ToArray();
            CapInfToNaN(vectors);
            return (values, vectors);
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
            if (n == 0)
                throw new ArgumentException($"{op} requires a non-empty matrix.");
            if (n != m.GetLength(1))
                throw new ArgumentException(ErrorMsg.Get("LINALG_EigenNotSquare", n, m.GetLength(1)));
            NumericGuard.AgainstNonFinite(m); // Replaces inline NaN/Inf scan
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    // 相对判据：绝对阈值 1e-8 在 1e9 量级矩阵下 ULP≈1.2e-7 > 1e-8，
                    // 理论对称矩阵因浮点舍入被误判为非对称。阈值须随元素量级缩放
                    // （1e9 量级 → ≈1e1，远大于 ULP；小矩阵保持 1e-8 等效行为）。
                    // scale 不得带 `Math.Max(1.0, …)` 下限——小量纲矩阵（如 [[0,0],[1e-9,0]]，
                    // 相对 100% 非对称）会退化为绝对阈值 diff<1e-8 → 误判对称 → Evd 静默
                    // 按错误矩阵分解。纯相对判据 scale = max(|aij|,|aji|)；
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
            if (m.GetLength(0) == 0 || m.GetLength(1) == 0)
                throw new ArgumentException("Condition number requires a non-empty matrix.");
            // 条件数对公共正缩放不变：归一化后计算覆盖大量纲（diag(1e308) → 1.0）。
            var cond = Matrix<double>.Build.DenseOfArray(NormalizedCopy(m, MaxAbs(m))).ConditionNumber();
            // 奇异矩阵 cond=+∞ 须按模块 Inf→NaN 输出封顶约定（对齐 Sum/Range/CapNaN
            // 的写法）封顶为 NaN，语义 = "条件数不可表示"。
            return double.IsInfinity(cond) ? double.NaN : cond;
        }

        internal static int Rank(double[,] m, double tol = 0)
        {
            NumericGuard.AgainstNonFinite(m);
            if (m.GetLength(0) == 0 || m.GetLength(1) == 0)
                throw new ArgumentException("Rank requires a non-empty matrix.");
            // 数值秩对公共正缩放不变：归一化后 SVD（大量纲下 MathNet 会 NonConvergence）。
            var A = Matrix<double>.Build.DenseOfArray(NormalizedCopy(m, MaxAbs(m)));
            var svd = SvdOf(A, computeVectors: false);
            // Use relative tolerance (MATLAB/numpy convention) when tol <= 0
            double effectiveTol = tol > 0
                ? tol
                : (svd.S.Count > 0 ? svd.S.Maximum() * Math.Max(m.GetLength(0), m.GetLength(1)) * 1e-16 : 1e-10);
            return svd.S.Count(s => s > effectiveTol);
        }

        internal static double NormFrobenius(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            // MathNet FrobeniusNorm 朴素平方和——[[1e200,1e200]] → 1e400 溢出 Inf
            // （真值 1.41e200 可表示，实测确认）。尺度化：先取最大 |x| 归一再平方
            // 求和，避免中间溢出。
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
            // 真值超出 double 表示（如 [[1e308,1e308]]）时 max·√s 溢出 →
            // 模块约定 Inf 封顶为 NaN（否则返回 +Inf）。
            double norm = max * Math.Sqrt(s);
            return double.IsInfinity(norm) ? double.NaN : norm;
        }

        internal static double[,] Identity(int n)
        {
            // 上限 2000：DenseIdentity(10000).ToArray() = 800MB，32 位 Excel 单公式
            // OOM 风险（2000 → 32MB）。
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
            var r = (Matrix<double>.Build.DenseOfArray(A) * Matrix<double>.Build.DenseOfArray(B)).ToArray();
            // 逐元素 Inf → NaN 封顶（1e300×1e300 会直漏 +Inf 进单元格）。
            for (int i = 0; i < r.GetLength(0); i++)
                for (int j = 0; j < r.GetLength(1); j++)
                    if (double.IsInfinity(r[i, j])) r[i, j] = double.NaN;
            return r;
        }

        internal static double[,] Transpose(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            return Matrix<double>.Build.DenseOfArray(m).Transpose().ToArray();
        }

        internal static double Trace(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            var r = Matrix<double>.Build.DenseOfArray(m).Trace();
            // 对角和溢出 → NaN 封顶（模块约定）。
            return double.IsInfinity(r) ? double.NaN : r;
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
        /// collision silently return another matrix's cached result.
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