using System;
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
        internal static (double[,] U, double[] S, double[,] Vt) Svd(double[,] m)
        {
            var A = Matrix<double>.Build.DenseOfArray(m);
            var svd = A.Svd(computeVectors: true);
            int rows = A.RowCount, cols = A.ColumnCount, k = Math.Min(rows, cols);
            return (svd.U.SubMatrix(0, rows, 0, k).ToArray(),
                    svd.S.ToArray(),
                    svd.VT.SubMatrix(0, k, 0, cols).ToArray());
        }

        internal static double[,] PseudoInverse(double[,] m)
        {
            var A = Matrix<double>.Build.DenseOfArray(m);
            return A.PseudoInverse().ToArray();
        }

        internal static (double[,] Q, double[,] R) Qr(double[,] m)
        {
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

        internal static double Determinant(double[,] m) =>
            Matrix<double>.Build.DenseOfArray(m).Determinant();

        internal static double[] Solve(double[,] A, double[] b) =>
            Matrix<double>.Build.DenseOfArray(A).Solve(Vector<double>.Build.Dense(b)).ToArray();

        internal static double[,] Cholesky(double[,] m) =>
            Matrix<double>.Build.DenseOfArray(m).Cholesky().Factor.ToArray();

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
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (Math.Abs(m[i, j] - m[j, i]) > 1e-8)
                        throw new ArgumentException(
                            $"Matrix is not symmetric: |m[{i},{j}] − m[{j},{i}]| = {Math.Abs(m[i, j] - m[j, i]):E2} > 1e-8. " +
                            "Eigenvalue decomposition (Evd) requires a symmetric matrix.");
        }

        internal static double ConditionNumber(double[,] m) =>
            Matrix<double>.Build.DenseOfArray(m).ConditionNumber();

        internal static int Rank(double[,] m, double tol = 1e-10)
        {
            var A = Matrix<double>.Build.DenseOfArray(m);
            var svd = A.Svd(computeVectors: false);
            return svd.S.Count(s => s > tol);
        }

        internal static double NormFrobenius(double[,] m) =>
            Matrix<double>.Build.DenseOfArray(m).FrobeniusNorm();

        internal static double[,] Identity(int n) =>
            Matrix<double>.Build.DenseIdentity(n).ToArray();

        internal static double[,] Diagonal(double[] v) =>
            Matrix<double>.Build.DenseOfDiagonalArray(v).ToArray();

        internal static double[,] MatMul(double[,] A, double[,] B) =>
            (Matrix<double>.Build.DenseOfArray(A) * Matrix<double>.Build.DenseOfArray(B)).ToArray();

        internal static double[,] Transpose(double[,] m) =>
            Matrix<double>.Build.DenseOfArray(m).Transpose().ToArray();

        internal static double Trace(double[,] m) =>
            Matrix<double>.Build.DenseOfArray(m).Trace();
    }
}
