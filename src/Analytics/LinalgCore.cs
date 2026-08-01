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
            private static readonly Dictionary<string, (object Value, LinkedListNode<string> Node)> Store = new();
            private static readonly LinkedList<string> LruList = new(); // front = LRU, back = MRU
            private static readonly object Lock = new();
            private const int MaxEntries = 32;

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

                lock (Lock)
                {
                    // Double-check: another thread may have computed the same key
                    if (Store.TryGetValue(key, out var entry))
                    {
                        LruList.Remove(entry.Node);
                        LruList.AddLast(entry.Node);
                        return (T)entry.Value;
                    }

                    if (Store.Count >= MaxEntries)
                    {
                        // Evict front (LRU) — O(1)
                        var oldest = LruList.First!;
                        LruList.RemoveFirst();
                        Store.Remove(oldest.Value);
                    }

                    var node = LruList.AddLast(key);
                    Store[key] = (result!, node);
                    return result;
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
            return Matrix<double>.Build.DenseOfArray(A).Solve(Vector<double>.Build.Dense(b)).ToArray();
        }

        internal static double[,] Cholesky(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
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

        private static void EnsureSymmetric(double[,] m)
        {
            int n = m.GetLength(0);
            if (n != m.GetLength(1))
                throw new ArgumentException(ErrorMsg.Get("LINALG_EigenNotSquare", n, m.GetLength(1)));
            NumericGuard.AgainstNonFinite(m); // Replaces inline NaN/Inf scan
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (Math.Abs(m[i, j] - m[j, i]) > 1e-8)
                        throw new ArgumentException(
                            $"Matrix is not symmetric: |m[{i},{j}] − m[{j},{i}]| = {Math.Abs(m[i, j] - m[j, i]):E2} > 1e-8. " +
                            "Eigenvalue decomposition (Evd) requires a symmetric matrix.");
                }
            }
        }

        internal static double ConditionNumber(double[,] m)
        {
            NumericGuard.AgainstNonFinite(m);
            return Matrix<double>.Build.DenseOfArray(m).ConditionNumber();
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
            return Matrix<double>.Build.DenseOfArray(m).FrobeniusNorm();
        }

        internal static double[,] Identity(int n)
        {
            if (n < 0 || n > 10_000)
                throw new ArgumentException(
                    $"Identity matrix size must be between 0 and 10000 (got {n}).");
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
