using System;
using ExcelVbaLibraries.Analytics;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Analytics.Tests
{
    public class LinalgUdfTests
    {
        private static readonly double[,] A = { { 2, -1 }, { -1, 2 } };
        private static readonly double[,] B = { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 10 } };

        // ── Decompositions ──
        [Fact] public void Svd_returns_3_elements()
        {
            var r = (object[])LinalgUdf.UDF_LINALG_SVD(B);
            r.Length.Should().Be(3);
        }
        [Fact] public void Qr_returns_2_elements()
        {
            var r = (object[])LinalgUdf.UDF_LINALG_QR(B);
            r.Length.Should().Be(2);
        }
        [Fact] public void Lu_returns_3_elements()
        {
            var r = (object[])LinalgUdf.UDF_LINALG_LU(B);
            r.Length.Should().Be(3);
        }

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
            var r = (double[])LinalgUdf.UDF_LINALG_EIGEN(B);
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
        [Fact] public void Svd_null_error() => LinalgUdf.UDF_LINALG_SVD(null!).Should().Be(ExcelError.Value);
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
    }
}
