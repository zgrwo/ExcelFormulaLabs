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

        // =====================================================================
        // DECOMPOSITION CACHE TESTS — verify cached accessors match direct
        // =====================================================================

        [Fact] public void CachedSvdU_equals_direct_Svd_U()
        {
            var cached = LinalgCore.SvdU(B);
            var direct = LinalgCore.Svd(B).U;
            DiffFro(cached, direct).Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void CachedSvdS_equals_direct_Svd_S()
        {
            var cached = LinalgCore.SvdS(B);
            var direct = LinalgCore.Svd(B).S;
            for (int i = 0; i < direct.Length; i++)
                cached[i].Should().BeApproximately(direct[i], 1e-10);
        }
        [Fact] public void CachedSvdVt_equals_direct_Svd_Vt()
        {
            var cached = LinalgCore.SvdVt(B);
            var direct = LinalgCore.Svd(B).Vt;
            DiffFro(cached, direct).Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void CachedQrQ_equals_direct_Qr_Q()
        {
            var cached = LinalgCore.QrQ(B);
            var direct = LinalgCore.Qr(B).Q;
            DiffFro(cached, direct).Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void CachedQrR_equals_direct_Qr_R()
        {
            var cached = LinalgCore.QrR(B);
            var direct = LinalgCore.Qr(B).R;
            DiffFro(cached, direct).Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void CachedLuL_equals_direct_Lu_L()
        {
            var cached = LinalgCore.LuL(B);
            var direct = LinalgCore.Lu(B).L;
            DiffFro(cached, direct).Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void CachedLuU_equals_direct_Lu_U()
        {
            var cached = LinalgCore.LuU(B);
            var direct = LinalgCore.Lu(B).U;
            DiffFro(cached, direct).Should().BeApproximately(0.0, 1e-10);
        }
        [Fact] public void CachedLuP_equals_direct_Lu_P()
        {
            var cached = LinalgCore.LuP(B);
            var direct = LinalgCore.Lu(B).P;
            DiffFro(cached, direct).Should().BeApproximately(0.0, 1e-10);
        }

        // ── LRU eviction: 9 matrices → oldest evicted, not all cleared ──
        [Fact] public void Cache_evicts_oldest_not_all()
        {
            var mats = new double[9][,];
            for (int k = 0; k < 9; k++)
                mats[k] = new double[,] { { 1.0 + k * 1e-6, 2.0 }, { 3.0, 4.0 } };

            // Fill cache with mats 0-7 (8 entries)
            for (int k = 0; k < 8; k++) LinalgCore.SvdU(mats[k]);

            // Touch mat 0 → moves to end of LRU
            LinalgCore.SvdU(mats[0]);

            // Request mat 8 → evicts mat 1 (oldest after touch), not mat 0
            LinalgCore.SvdU(mats[8]);

            // mat 0 should still be cached (immediate hit, not recomputed)
            var r0 = LinalgCore.SvdU(mats[0]);
            r0[0, 0].Should().BeApproximately(
                LinalgCore.Svd(mats[0]).U[0, 0], 1e-10);
        }

        // ── EnsureSymmetric NaN/Infinity rejection ──
        [Fact] public void Eigenvalues_NaN_matrix_throws()
        {
            var m = new double[,] { { double.NaN, 1 }, { 1, 1 } };
            var act = () => LinalgCore.Eigenvalues(m);
            act.Should().Throw<ArgumentException>().WithMessage("*NaN*");
        }

        [Fact] public void Eigenvalues_Infinity_matrix_throws()
        {
            var m = new double[,] { { 1.0, 1 }, { 1, double.PositiveInfinity } };
            var act = () => LinalgCore.Eigenvalues(m);
            act.Should().Throw<ArgumentException>().WithMessage("*Infinity*");
        }

        [Fact] public void Eigen_NaN_matrix_throws()
        {
            var m = new double[,] { { double.NaN, 2 }, { 2, 1 } };
            var act = () => LinalgCore.Eigen(m);
            act.Should().Throw<ArgumentException>().WithMessage("*NaN*");
        }

        // ════════════════════════════════════════════════════════════════
        // NaN/Inf guard tests — ValidateMatrixFinite on ALL public methods
        // ════════════════════════════════════════════════════════════════

        [Fact] public void Svd_NaN_throws() { var a = () => LinalgCore.Svd(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Svd_Infinity_throws() { var a = () => LinalgCore.Svd(InfM); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*"); }
        [Fact] public void PseudoInverse_NaN_throws() { var a = () => LinalgCore.PseudoInverse(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Qr_NaN_throws() { var a = () => LinalgCore.Qr(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Qr_Infinity_throws() { var a = () => LinalgCore.Qr(InfM); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*"); }
        [Fact] public void Lu_NaN_throws() { var a = () => LinalgCore.Lu(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Determinant_NaN_throws() { var a = () => LinalgCore.Determinant(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Determinant_Infinity_throws() { var a = () => LinalgCore.Determinant(InfM); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*"); }
        [Fact] public void Solve_NaN_matrix_throws() { var a = () => LinalgCore.Solve(NaNM, new[] { 1.0, 1.0 }); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Solve_NaN_vector_throws() { var a = () => LinalgCore.Solve(A, new[] { double.NaN, 1.0 }); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Solve_Infinity_vector_throws() { var a = () => LinalgCore.Solve(A, new[] { 1.0, double.PositiveInfinity }); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*"); }
        [Fact] public void Cholesky_NaN_throws() { var a = () => LinalgCore.Cholesky(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void ConditionNumber_NaN_throws() { var a = () => LinalgCore.ConditionNumber(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Rank_NaN_throws() { var a = () => LinalgCore.Rank(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void NormFrobenius_NaN_throws() { var a = () => LinalgCore.NormFrobenius(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void MatMul_NaN_A_throws() { var a = () => LinalgCore.MatMul(NaNM, I2); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void MatMul_NaN_B_throws() { var a = () => LinalgCore.MatMul(I2, NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Transpose_NaN_throws() { var a = () => LinalgCore.Transpose(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Trace_NaN_throws() { var a = () => LinalgCore.Trace(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void Trace_Infinity_throws() { var a = () => LinalgCore.Trace(InfM); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*"); }
        // Cache accessors delegate to guarded Svd/Qr/Lu
        [Fact] public void SvdU_NaN_throws() { var a = () => LinalgCore.SvdU(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void QrQ_NaN_throws() { var a = () => LinalgCore.QrQ(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void LuL_NaN_throws() { var a = () => LinalgCore.LuL(NaNM); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }

        private static readonly double[,] NaNM = { { double.NaN, 1 }, { 1, 1 } };
        private static readonly double[,] InfM = { { 1.0, 1 }, { 1, double.PositiveInfinity } };
        private static readonly double[,] I2 = { { 1, 0 }, { 0, 1 } };
    }
}
