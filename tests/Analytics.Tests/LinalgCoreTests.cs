using System;
using System.Linq;
using ExcelVbaLibraries.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Analytics.Tests
{
    public class LinalgCoreTests
    {
        private static readonly double[,] A = {{2,-1},{-1,2}};
        private static readonly double[,] B = {{1,2,3},{4,5,6},{7,8,10}};
        [Fact] public void Svd() { var (U,S,Vt)=LinalgCore.Svd(B); U.GetLength(0).Should().Be(3); S.Length.Should().Be(3); }
        [Fact] public void Pinv() => LinalgCore.PseudoInverse(B).GetLength(0).Should().Be(3);
        // ── SVD correctness: A ≈ U * Σ * Vt ─────────────────────────────
        [Fact] public void Svd_Correctness_square() { var A=B;var(U,S,Vt)=LinalgCore.Svd(A);var US=LinalgCore.MatMul(U,LinalgCore.Diagonal(S));DiffFro(A,LinalgCore.MatMul(US,Vt)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Svd_Correctness_tall() { var A=new double[,]{{1,2},{3,4},{5,6}};var(U,S,Vt)=LinalgCore.Svd(A);var US=LinalgCore.MatMul(U,LinalgCore.Diagonal(S));DiffFro(A,LinalgCore.MatMul(US,Vt)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Svd_Correctness_wide() { var A=new double[,]{{1,2,3},{4,5,6}};var(U,S,Vt)=LinalgCore.Svd(A);var US=LinalgCore.MatMul(U,LinalgCore.Diagonal(S));DiffFro(A,LinalgCore.MatMul(US,Vt)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Svd_Correctness_rankDeficient() { var A=new double[,]{{1,2,3},{4,5,6},{5,7,9}};var(U,S,Vt)=LinalgCore.Svd(A);var US=LinalgCore.MatMul(U,LinalgCore.Diagonal(S));DiffFro(A,LinalgCore.MatMul(US,Vt)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Svd_Correctness_zeroRow() { var A=new double[,]{{1,2,3},{0,0,0},{4,5,6}};var(U,S,Vt)=LinalgCore.Svd(A);var US=LinalgCore.MatMul(U,LinalgCore.Diagonal(S));DiffFro(A,LinalgCore.MatMul(US,Vt)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Svd_Correctness_1x1() { var A=new double[,]{{42}};var(U,S,Vt)=LinalgCore.Svd(A);var US=LinalgCore.MatMul(U,LinalgCore.Diagonal(S));DiffFro(A,LinalgCore.MatMul(US,Vt)).Should().BeApproximately(0.0,1e-9); }
        // ── QR correctness: A ≈ Q * R  and  Qᵀ * Q ≈ I ──────────────────
        [Fact] public void Qr_Correctness_square() { var A=B;var(Q,R)=LinalgCore.Qr(A);DiffFro(A,LinalgCore.MatMul(Q,R)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Qr_Correctness_tall() { var A=new double[,]{{1,2},{3,4},{5,6}};var(Q,R)=LinalgCore.Qr(A);DiffFro(A,LinalgCore.MatMul(Q,R)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Qr_Correctness_wide() { var A=new double[,]{{1,2,3},{4,5,6}};var(Q,R)=LinalgCore.Qr(A);DiffFro(A,LinalgCore.MatMul(Q,R)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Qr_Correctness_rankDeficient() { var A=new double[,]{{1,2,3},{4,5,6},{5,7,9}};var(Q,R)=LinalgCore.Qr(A);DiffFro(A,LinalgCore.MatMul(Q,R)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Qr_Orthogonal_Q() { var A=B;var(Q,_)=LinalgCore.Qr(A);var Qt=LinalgCore.Transpose(Q);int k=Q.GetLength(1);var QtQ=LinalgCore.MatMul(Qt,Q);var I=LinalgCore.Identity(k);DiffFro(I,QtQ).Should().BeApproximately(0.0,1e-9); }
        // ── PINV correctness: Moore-Penrose A * A⁺ * A ≈ A ──────────────
        [Fact] public void Pinv_Correctness_square() { var A=B;var Ap=LinalgCore.PseudoInverse(A);DiffFro(A,LinalgCore.MatMul(LinalgCore.MatMul(A,Ap),A)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Pinv_Correctness_tall() { var A=new double[,]{{1,2},{3,4},{5,6}};var Ap=LinalgCore.PseudoInverse(A);DiffFro(A,LinalgCore.MatMul(LinalgCore.MatMul(A,Ap),A)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Pinv_Correctness_wide() { var A=new double[,]{{1,2,3},{4,5,6}};var Ap=LinalgCore.PseudoInverse(A);DiffFro(A,LinalgCore.MatMul(LinalgCore.MatMul(A,Ap),A)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Pinv_Correctness_rankDeficient() { var A=new double[,]{{1,2,3},{4,5,6},{5,7,9}};var Ap=LinalgCore.PseudoInverse(A);DiffFro(A,LinalgCore.MatMul(LinalgCore.MatMul(A,Ap),A)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Pinv_Correctness_zeroRow() { var A=new double[,]{{1,2,3},{0,0,0},{4,5,6}};var Ap=LinalgCore.PseudoInverse(A);DiffFro(A,LinalgCore.MatMul(LinalgCore.MatMul(A,Ap),A)).Should().BeApproximately(0.0,1e-9); }
        [Fact] public void Det() => LinalgCore.Determinant(A).Should().BeApproximately(3.0,1e-10);
        [Fact] public void Solve() { var x=LinalgCore.Solve(A,new[]{4.0,1}); x[0].Should().BeApproximately(3.0,1e-8); x[1].Should().BeApproximately(2.0,1e-8); }
        [Fact] public void Cholesky() => LinalgCore.Cholesky(new double[,]{{4,2},{2,3}}).GetLength(0).Should().Be(2);
        [Fact] public void Eigenvalues() => LinalgCore.Eigenvalues(new double[,]{{2,-1,0},{-1,2,-1},{0,-1,2}}).Length.Should().Be(3);
        [Fact] public void Eigenvalues_NonSymmetric_Throws() { var a=()=>LinalgCore.Eigenvalues(B); a.Should().Throw<ArgumentException>().WithMessage("*symmetric*"); }
        [Fact] public void Eigen() { var r=LinalgCore.Eigen(A); r.values.Length.Should().Be(2); r.vectors.GetLength(0).Should().Be(2); }
        [Fact] public void Cond() => LinalgCore.ConditionNumber(A).Should().BeGreaterThan(0);
        [Fact] public void Rank() => LinalgCore.Rank(B).Should().Be(3);
        [Fact] public void Identity() { var I=LinalgCore.Identity(3); for(int i=0;i<3;i++) I[i,i].Should().BeApproximately(1,1e-10); }
        [Fact] public void MatMul() => LinalgCore.MatMul(A,A).GetLength(0).Should().Be(2);
        [Fact] public void Transpose() { var t=LinalgCore.Transpose(new double[,]{{1,2},{3,4}}); t[0,1].Should().Be(3); }
        [Fact] public void Trace() => LinalgCore.Trace(A).Should().BeApproximately(4.0,1e-10);
        [Fact] public void Qr() { var r=LinalgCore.Qr(B); r.Q.GetLength(0).Should().Be(3); }
        [Fact] public void Lu() { var r=LinalgCore.Lu(B); r.L.GetLength(0).Should().Be(3); }
        // MathNet LU convention: A = P * L * U  (perm[i] = row of original A that ends up at permuted row i)
        [Fact] public void Lu_Correctness_A_eq_PxLxU_noPivot() {
            var A=new double[,]{{1,2,3},{4,5,6},{7,8,10}};var(L,U,P)=LinalgCore.Lu(A);
            var lu=LinalgCore.MatMul(L,U);var pa=LinalgCore.MatMul(P,lu);
            DiffFro(A,pa).Should().BeApproximately(0.0,1e-10); }
        [Fact] public void Lu_Correctness_A_eq_PxLxU_needsPivot() {
            var A=new double[,]{{0,1},{1,0}};var(L,U,P)=LinalgCore.Lu(A);
            var lu=LinalgCore.MatMul(L,U);var pa=LinalgCore.MatMul(P,lu);
            DiffFro(A,pa).Should().BeApproximately(0.0,1e-10); }
        [Fact] public void Lu_Correctness_A_eq_PxLxU_3x3() {
            var A=new double[,]{{0,2,3},{4,5,6},{7,8,10}};var(L,U,P)=LinalgCore.Lu(A);
            var lu=LinalgCore.MatMul(L,U);var pa=LinalgCore.MatMul(P,lu);
            DiffFro(A,pa).Should().BeApproximately(0.0,1e-10); }
        private static double DiffFro(double[,] a,double[,] b){double s=0;for(int i=0;i<a.GetLength(0);i++)for(int j=0;j<a.GetLength(1);j++){var d=a[i,j]-b[i,j];s+=d*d;}return Math.Sqrt(s);}
        [Fact] public void NormFrobenius() => LinalgCore.NormFrobenius(new double[,]{{1,2},{3,4}}).Should().BeApproximately(5.477225575051661, 1e-10);
        [Fact] public void Diagonal() { var d=LinalgCore.Diagonal(new[]{1.0,2,3}); d[0,0].Should().Be(1); d[1,1].Should().Be(2); d[2,2].Should().Be(3); d[0,1].Should().Be(0); }
        [Fact] public void Eigenvalues_Known() { var e=LinalgCore.Eigenvalues(new double[,]{{1,2},{2,1}}).OrderByDescending(x=>x).ToArray(); e[0].Should().BeApproximately(3,1e-8); e[1].Should().BeApproximately(-1,1e-8); }
        [Fact] public void NormFrobenius_zero_matrix() => LinalgCore.NormFrobenius(new double[2,2]).Should().Be(0.0);
        [Fact] public void NormFrobenius_identity() => LinalgCore.NormFrobenius(new double[,]{{1,0},{0,1}}).Should().BeApproximately(Math.Sqrt(2),1e-10);
        [Fact] public void Diagonal_empty() { var d=LinalgCore.Diagonal(new double[0]); d.GetLength(0).Should().Be(0); }
        [Fact] public void Rank_with_tolerance()
        {
            var m = new double[,] { { 1, 1 }, { 1, 1 + 1e-8 } };
            LinalgCore.Rank(m, 1e-10).Should().Be(2);  // tight tol → full rank
            LinalgCore.Rank(m, 1e-6).Should().Be(1);   // loose tol → rank 1
        }

        // =====================================================================
        // EDGE CASE & ERROR BEHAVIOR TESTS
        // (derived from M7 Qr pattern + systematic matrix edge cases)
        // =====================================================================

        [Fact] public void Cholesky_non_positive_definite_throws()
        {
            // [[1,2],[2,1]] is indefinite (eigenvalues 3, -1) → Cholesky must fail
            var act = () => LinalgCore.Cholesky(new double[,] { { 1, 2 }, { 2, 1 } });
            act.Should().Throw<Exception>();
        }

        [Fact] public void Determinant_singular_returns_zero()
        {
            // Linearly dependent rows → det ≈ 0
            LinalgCore.Determinant(new double[,] { { 1, 2 }, { 2, 4 } })
                .Should().BeApproximately(0.0, 1e-10);
        }

        [Fact] public void Identity_zero()
        {
            var I = LinalgCore.Identity(0);
            I.GetLength(0).Should().Be(0);
            I.GetLength(1).Should().Be(0);
        }

        [Fact] public void Solve_singular_inconsistent_returns_infinity()
        {
            // Singular 2×2 with inconsistent RHS:
            //   x1+x2=1, x1+x2=2  has no solution.
            // MathNet Solve → LU factorization fails → returns ±∞ components.
            var x = LinalgCore.Solve(
                new double[,] { { 1, 1 }, { 1, 1 } },
                new[] { 1.0, 2.0 });
            x.Length.Should().Be(2);
            // Singular inconsistent: result contains non-finite values
            x.Should().Contain(v => double.IsInfinity(v));
        }

        [Fact] public void Eigen_non_symmetric_throws()
        {
            var act = () => LinalgCore.Eigen(B);  // B is not symmetric
            act.Should().Throw<ArgumentException>().WithMessage("*symmetric*");
        }

        [Fact] public void ConditionNumber_singular()
        {
            // Singular matrix → condition number approaches ∞
            var cond = LinalgCore.ConditionNumber(
                new double[,] { { 1, 2 }, { 2, 4 } });
            cond.Should().BeGreaterThan(1e10);
        }

        [Fact] public void NormFrobenius_non_square()
        {
            // Frobenius norm well-defined for any shape
            var n = LinalgCore.NormFrobenius(new double[,] { { 1, 2, 3 }, { 4, 5, 6 } });
            n.Should().BeApproximately(Math.Sqrt(1 + 4 + 9 + 16 + 25 + 36), 1e-10);
        }

        [Fact] public void Rank_all_zero_matrix()
        {
            LinalgCore.Rank(new double[3, 3]).Should().Be(0);
        }

        [Fact] public void Qr_wide_exceeds_maxCols()
        {
            // QR with rows=2, cols=2001 > maxCols=2000 → throws ArgumentException
            var wide = new double[2, 2001];
            var act = () => LinalgCore.Qr(wide);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*QR decomposition*");
        }

        [Fact] public void Trace_non_square()
        {
            // MathNet Trace requires square matrix → throws
            var act = () => LinalgCore.Trace(new double[,] { { 1, 2, 3 }, { 4, 5, 6 } });
            act.Should().Throw<ArgumentException>().WithMessage("*square*");
        }

        [Fact] public void Svd_zero_matrix()
        {
            var (U, S, Vt) = LinalgCore.Svd(new double[2, 2]);
            // All singular values should be 0
            S.Should().AllBeEquivalentTo(0.0);
            U.GetLength(0).Should().Be(2);
            Vt.GetLength(1).Should().Be(2);
        }

        [Fact] public void Pinv_rank_deficient()
        {
            // PseudoInverse of a rank-1 matrix (row 2 = 2×row 1)
            // should still produce correct dimensions
            var Ap = LinalgCore.PseudoInverse(new double[,] { { 1, 2, 3 }, { 2, 4, 6 } });
            Ap.GetLength(0).Should().Be(3);
            Ap.GetLength(1).Should().Be(2);
        }
    }
}
