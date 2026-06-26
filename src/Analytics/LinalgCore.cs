using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
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
        /// Thread-safe; max 8 entries with LRU eviction.
        /// </summary>
        private static class DecompCache
        {
            private static readonly Dictionary<string, object> Store = new();
            private static readonly List<string> LruOrder = new(); // most-recently-used at end
            private static readonly object Lock = new();
            private const int MaxEntries = 8;

            internal static T GetOrAdd<T>(string key, Func<T> factory)
            {
                lock (Lock)
                {
                    if (Store.TryGetValue(key, out var existing))
                    {
                        // Touch: move to end of LRU list
                        LruOrder.Remove(key);
                        LruOrder.Add(key);
                        return (T)existing;
                    }

                    if (Store.Count >= MaxEntries)
                    {
                        // Evict least-recently-used single entry
                        var oldest = LruOrder[0];
                        Store.Remove(oldest);
                        LruOrder.RemoveAt(0);
                    }

                    var result = factory();
                    Store[key] = result!;
                    LruOrder.Add(key);
                    return result;
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
                    int hash = 17;
                    hash = hash * 31 + rows;
                    hash = hash * 31 + cols;
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            long bits = BitConverter.DoubleToInt64Bits(m[r, c]);
                            hash = hash * 31 + (int)(bits ^ (bits >> 32));
                        }
                    }
                    // Append dimensions to disambiguate same-hash different-shape matrices
                    return $"{hash:X8}_{rows}x{cols}";
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
            // Wide (rows &lt; cols): MathNet QR requires m ≥ n. Zero-pad to square n×n,
            // decompose, then extract Q[0:m, 0:m] and R[0:m, 0:n].
            // Guard against excessive memory: cols×cols doubles = cols² × 8 bytes.
            // Limit cols to 2000 (≈ 32 MB) to prevent OOM from accidentally wide input.
            const int maxCols = 2000;
            if (cols > maxCols)
                throw new ArgumentException(
                    $"QR decomposition: matrix has {cols} columns but only up to {maxCols} are supported " +
                    $"for wide matrices (rows={rows} < cols={cols}). For wide input, transpose and use tall-skinny QR.");
            var pad = Matrix<double>.Build.Dense(cols, cols);
            var Aorig = Matrix<double>.Build.DenseOfArray(m);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    pad[i, j] = Aorig[i, j];
            var q = pad.QR(QRMethod.Full);
            return (q.Q.SubMatrix(0, rows, 0, rows).ToArray(),
                    q.R.SubMatrix(0, rows, 0, cols).ToArray());
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
                throw new ArgumentException("Right-hand side vector contains NaN or Infinity. Solve requires finite values.");
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
                throw new ArgumentException($"Eigenvalue decomposition requires a square matrix (got {n}×{m.GetLength(1)}).");
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

        internal static int Rank(double[,] m, double tol = 1e-10)
        {
            NumericGuard.AgainstNonFinite(m);
            var A = Matrix<double>.Build.DenseOfArray(m);
            var svd = A.Svd(computeVectors: false);
            return svd.S.Count(s => s > tol);
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

        internal static double[,] Diagonal(double[] v) =>
            Matrix<double>.Build.DenseOfDiagonalArray(v).ToArray();

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

        internal static double[,] SvdU(double[,] m)
        {
            var key = DecompCache.MatrixHash(m);
            return DecompCache.GetOrAdd("svd:" + key, () => Svd(m)).U;
        }

        internal static double[] SvdS(double[,] m)
        {
            var key = DecompCache.MatrixHash(m);
            return DecompCache.GetOrAdd("svd:" + key, () => Svd(m)).S;
        }

        internal static double[,] SvdVt(double[,] m)
        {
            var key = DecompCache.MatrixHash(m);
            return DecompCache.GetOrAdd("svd:" + key, () => Svd(m)).Vt;
        }

        internal static double[,] QrQ(double[,] m)
        {
            var key = DecompCache.MatrixHash(m);
            return DecompCache.GetOrAdd("qr:" + key, () => Qr(m)).Q;
        }

        internal static double[,] QrR(double[,] m)
        {
            var key = DecompCache.MatrixHash(m);
            return DecompCache.GetOrAdd("qr:" + key, () => Qr(m)).R;
        }

        internal static double[,] LuL(double[,] m)
        {
            var key = DecompCache.MatrixHash(m);
            return DecompCache.GetOrAdd("lu:" + key, () => Lu(m)).L;
        }

        internal static double[,] LuU(double[,] m)
        {
            var key = DecompCache.MatrixHash(m);
            return DecompCache.GetOrAdd("lu:" + key, () => Lu(m)).U;
        }

        internal static double[,] LuP(double[,] m)
        {
            var key = DecompCache.MatrixHash(m);
            return DecompCache.GetOrAdd("lu:" + key, () => Lu(m)).P;
        }
    }
}
