using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
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
            var A = Matrix<double>.Build.DenseOfArray(m);
            var qr = A.QR();
            int rows = A.RowCount, cols = A.ColumnCount, k = Math.Min(rows, cols);
            return (qr.Q.SubMatrix(0, rows, 0, k).ToArray(),
                    qr.R.SubMatrix(0, k, 0, cols).ToArray());
        }

        internal static (double[,] L, double[,] U, double[,] P) Lu(double[,] m)
        {
            var A = Matrix<double>.Build.DenseOfArray(m);
            var lu = A.LU();
            // P is a Permutation, format as identity matrix permuted
            var P = Matrix<double>.Build.DenseIdentity(A.RowCount);
            var perm = lu.P;
            for (int i = 0; i < A.RowCount; i++)
            {
                int row = perm[i];
                var temp = P.Row(i).ToArray();
                P.SetRow(i, P.Row(row));
                P.SetRow(row, temp);
            }
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

        internal static int Rank(double[,] m, double tol = 1e-10) =>
            Matrix<double>.Build.DenseOfArray(m).Rank();

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
