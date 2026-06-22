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
            // WARNING: allocates a cols×cols matrix — O(cols²) memory. For wide matrices
            // (cols &gt;&gt; rows), consider transposing the input or using a thin QR alternative.
            // Since padded rows are all zeros, the extracted Q remains orthogonal and
            // A = Q_thin · R_thin holds exactly.
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

        internal static double[] Eigenvalues(double[,] m) =>
            Matrix<double>.Build.DenseOfArray(m).Evd().EigenValues.Real().ToArray();

        internal static (double[] values, double[,] vectors) Eigen(double[,] m)
        {
            var evd = Matrix<double>.Build.DenseOfArray(m).Evd();
            return (evd.EigenValues.Real().ToArray(), evd.EigenVectors.ToArray());
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
