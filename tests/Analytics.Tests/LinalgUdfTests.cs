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
    }
}
