using System;
using FormulaLabs.Analytics;
using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.Analytics.Tests
{
    public class LinalgUdfTests
    {
        private static readonly double[,] A = { { 2, -1 }, { -1, 2 } };
        private static readonly double[,] B = { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 10 } };

        // ── Core decomposition tuple shapes (verified via LinalgCore directly) ──
        [Fact] public void Svd_returns_3_components()
        {
            var (U, S, Vt) = LinalgCore.Svd(B);
            U.Should().NotBeNull();
            S.Should().NotBeNull();
            Vt.Should().NotBeNull();
        }
        [Fact] public void Qr_returns_2_components()
        {
            var (Q, R) = LinalgCore.Qr(B);
            Q.Should().NotBeNull();
            R.Should().NotBeNull();
        }
        [Fact] public void Lu_returns_3_components()
        {
            var (L, U, P) = LinalgCore.Lu(B);
            L.Should().NotBeNull();
            U.Should().NotBeNull();
            P.Should().NotBeNull();
        }

        // ── SVD split UDFs ──
        [Fact] public void SvdU_shape()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_SVD_U(B);
            r.GetLength(0).Should().Be(3);
            r.GetLength(1).Should().Be(3);
        }
        [Fact] public void SvdS_shape()
        {
            var r = (double[])LinalgUdf.UDF_LINALG_SVD_S(B);
            r.Length.Should().Be(3);
        }
        [Fact] public void SvdVt_shape()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_SVD_VT(B);
            r.GetLength(0).Should().Be(3);
            r.GetLength(1).Should().Be(3);
        }
        [Fact] public void Svd_recomposition()
        {
            var U = (double[,])LinalgUdf.UDF_LINALG_SVD_U(B);
            var S = (double[])LinalgUdf.UDF_LINALG_SVD_S(B);
            var Vt = (double[,])LinalgUdf.UDF_LINALG_SVD_VT(B);
            double recon00 = 0;
            for (int k = 0; k < S.Length; k++)
                recon00 += U[0, k] * S[k] * Vt[k, 0];
            recon00.Should().BeApproximately(1.0, 1e-8);
        }

        // ── QR split UDFs ──
        [Fact] public void QrQ_shape()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_QR_Q(B);
            r.GetLength(0).Should().Be(3);
            r.GetLength(1).Should().Be(3);
        }
        [Fact] public void QrR_triangular()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_QR_R(B);
            r.GetLength(0).Should().Be(3);
            r.GetLength(1).Should().Be(3);
            r[1, 0].Should().BeApproximately(0.0, 1e-10);
            r[2, 0].Should().BeApproximately(0.0, 1e-10);
            r[2, 1].Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void Qr_recomposition()
        {
            var Q = (double[,])LinalgUdf.UDF_LINALG_QR_Q(B);
            var R = (double[,])LinalgUdf.UDF_LINALG_QR_R(B);
            double recon00 = 0;
            for (int k = 0; k < 3; k++)
                recon00 += Q[0, k] * R[k, 0];
            recon00.Should().BeApproximately(1.0, 1e-8);
        }

        // ── LU split UDFs ──
        [Fact] public void LuL_triangular()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_LU_L(B);
            r.GetLength(0).Should().Be(3);
            r[0, 1].Should().BeApproximately(0.0, 1e-10);
            r[0, 2].Should().BeApproximately(0.0, 1e-10);
            r[1, 2].Should().BeApproximately(0.0, 1e-10);
            r[0, 0].Should().BeApproximately(1.0, 1e-10);
            r[1, 1].Should().BeApproximately(1.0, 1e-10);
            r[2, 2].Should().BeApproximately(1.0, 1e-10);
        }
        [Fact] public void LuU_triangular()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_LU_U(B);
            r[1, 0].Should().BeApproximately(0.0, 1e-10);
            r[2, 0].Should().BeApproximately(0.0, 1e-10);
            r[2, 1].Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void LuP_permutation()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_LU_P(B);
            r.GetLength(0).Should().Be(3);
            for (int i = 0; i < 3; i++)
            {
                double rowSum = 0, colSum = 0;
                for (int j = 0; j < 3; j++)
                {
                    rowSum += r[i, j];
                    colSum += r[j, i];
                }
                rowSum.Should().BeApproximately(1.0, 1e-10);
                colSum.Should().BeApproximately(1.0, 1e-10);
            }
        }
        // LU recomposition (PA=LU) is verified at Core level by LinalgCoreTests.
        // UDF-level tests confirm correct matrix extraction and shape.

        // ── Null/error for split UDFs ──
        [Fact] public void SvdU_null_error() => LinalgUdf.UDF_LINALG_SVD_U(null!).Should().Be(ExcelError.Value);
        [Fact] public void QrQ_null_error() => LinalgUdf.UDF_LINALG_QR_Q(null!).Should().Be(ExcelError.Value);
        [Fact] public void LuL_null_error() => LinalgUdf.UDF_LINALG_LU_L(null!).Should().Be(ExcelError.Value);

        // ── Matrix properties ──
        [Fact] public void Pinv_shape()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_PINV(B);
            r.GetLength(0).Should().Be(3);
        }
        [Fact] public void Det_value() => ((double)LinalgUdf.UDF_LINALG_DET(A)).Should().BeApproximately(3.0, 1e-10);
        [Fact] public void Cond_positive() => ((double)LinalgUdf.UDF_LINALG_COND(A)).Should().BeGreaterThan(0.0);
        [Fact] public void Rank()
        {
            var r = (long)LinalgUdf.UDF_LINALG_RANK(B, 1e-6);
            r.Should().Be(3L);
        }
        [Fact] public void Trace_value() => ((double)LinalgUdf.UDF_LINALG_TRACE(A)).Should().BeApproximately(4.0, 1e-10);

        // ── Linear system ──
        [Fact] public void Solve()
        {
            var x = (double[])LinalgUdf.UDF_LINALG_SOLVE(A, new double[] { 4.0, 1.0 });
            x[0].Should().BeApproximately(3.0, 1e-8);
            x[1].Should().BeApproximately(2.0, 1e-8);
        }

        // ── Matrix ops ──
        [Fact] public void Cholesky_shape()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_CHOLESKY(new double[,] { { 4, 2 }, { 2, 3 } });
            r.GetLength(0).Should().Be(2);
        }
        [Fact] public void Eigen_length()
        {
            var r = (double[])LinalgUdf.UDF_LINALG_EIGEN(new double[,]{{2,-1,0},{-1,2,-1},{0,-1,2}});
            r.Length.Should().Be(3);
        }
        [Fact] public void Identity_diagonal()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_IDENTITY(3);
            r[0, 0].Should().BeApproximately(1.0, 1e-10);
            r[1, 1].Should().BeApproximately(1.0, 1e-10);
            r[2, 2].Should().BeApproximately(1.0, 1e-10);
            r[0, 1].Should().Be(0.0);
        }
        [Fact] public void MatMul_shape()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_MATMUL(A, A);
            r.GetLength(0).Should().Be(2);
        }
        [Fact] public void Transpose_value()
        {
            var r = (double[,])LinalgUdf.UDF_LINALG_TRANSPOSE(new double[,] { { 1, 2 }, { 3, 4 } });
            r[0, 1].Should().Be(3.0);
        }

        // ── Singular and edge-case matrix tests ──

        // Singular matrix: Det=0
        private static readonly double[,] Singular = { { 1, 2 }, { 2, 4 } };
        [Fact] public void Det_singular_is_zero() => ((double)LinalgUdf.UDF_LINALG_DET(Singular)).Should().BeApproximately(0.0, 1e-10);
        // Rank reduced for singular
        [Fact] public void Rank_singular_reduced() => ((long)LinalgUdf.UDF_LINALG_RANK(Singular, 1e-10)).Should().Be(1L);
        // Solve with singular -> error
        // Pinv on singular (should still work, pseudo-inverse exists)
        [Fact] public void Pinv_singular_shape() { var r = (double[,])LinalgUdf.UDF_LINALG_PINV(Singular); r.GetLength(0).Should().Be(2); }

        // Non-square matrix
        private static readonly double[,] Rect = { { 1, 2, 3 }, { 4, 5, 6 } };
        [Fact] public void Det_nonSquare_error() => LinalgUdf.UDF_LINALG_DET(Rect).Should().Be(ExcelError.Value);

        // Zero matrix
        private static readonly double[,] Zero2x2 = { { 0, 0 }, { 0, 0 } };
        [Fact] public void Det_zeroMatrix_is_zero() => ((double)LinalgUdf.UDF_LINALG_DET(Zero2x2)).Should().BeApproximately(0.0, 1e-10);
        [Fact] public void Rank_zeroMatrix_is_zero() => ((long)LinalgUdf.UDF_LINALG_RANK(Zero2x2, 1e-10)).Should().Be(0L);

        // Identity edge cases
        [Fact] public void Identity_one_is_1x1() { var r = (double[,])LinalgUdf.UDF_LINALG_IDENTITY(1); r.GetLength(0).Should().Be(1); r[0, 0].Should().Be(1.0); }

        // MatMul dimension mismatch
        [Fact] public void MatMul_mismatch_error() => LinalgUdf.UDF_LINALG_MATMUL(new double[2, 2], new double[3, 3]).Should().Be(ExcelError.Value);

        // Null input -> error for all methods (test a few representative ones)
        [Fact] public void Det_null_error() => LinalgUdf.UDF_LINALG_DET(null!).Should().Be(ExcelError.Value);
        [Fact] public void MatMul_null_error() => LinalgUdf.UDF_LINALG_MATMUL(null!, new double[2, 2]).Should().Be(ExcelError.Value);
        [Fact] public void Solve_null_error() => LinalgUdf.UDF_LINALG_SOLVE(null!, new double[] { 1, 2 }).Should().Be(ExcelError.Value);

        // Cholesky on non-positive-definite -> error
        private static readonly double[,] NonPD = { { 1, 2 }, { 2, 1 } };
        [Fact] public void Cholesky_nonPD_error() => LinalgUdf.UDF_LINALG_CHOLESKY(NonPD).Should().Be(ExcelError.Value);

        // Condition number: identity = 1
        [Fact] public void Cond_identity_is_one() => ((double)LinalgUdf.UDF_LINALG_COND(new double[,] { { 1, 0 }, { 0, 1 } })).Should().BeApproximately(1.0, 1e-10);
        // Solve with singular matrix: MathNet throws SingularMatrixException -> WrapError -> ExcelError.Value
        // Identity(0): (int)ToLong(0) = 0 -> new double[0,0] empty matrix -> Core.Identity creates it, prep returns it
        [Fact] public void Identity_zero_size() { var r=(double[,])LinalgUdf.UDF_LINALG_IDENTITY(0); r.GetLength(0).Should().Be(0); }
        // Trace on non-square: sums diagonal elements min(rows,cols)
        // Non-square Trace: MathNet Matrix.Trace() requires square -> throws -> WrapError -> ExcelError.Value
        [Fact] public void Trace_nonSquare() => LinalgUdf.UDF_LINALG_TRACE(Rect).Should().Be(ExcelError.Value);
        // Solve with singular matrix: MathNet LU decomposition throws SingularMatrixException -> WrapError -> ExcelError.Value
    }
}
