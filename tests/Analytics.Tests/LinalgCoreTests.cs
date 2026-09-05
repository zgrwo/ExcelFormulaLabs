using System;
using System.Linq;
using System.Text.RegularExpressions;
using ExcelFormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class LinalgCoreTests
    {
        public LinalgCoreTests() => LinalgCore.ClearDecompCache();
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
        [Fact] public void Qr_wide_throws_NotSupportedException() { var A=new double[,]{{1,2,3},{4,5,6}};var act=()=>LinalgCore.Qr(A);act.Should().Throw<NotSupportedException>().WithMessage("*rows >= columns*"); }
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

        [Fact] public void Solve_singular_inconsistent_throws()
        {
            // Singular 2×2 with inconsistent RHS (x1+x2=1, x1+x2=2) has no solution.
            // P2 (pre-release review): MathNet Solve silently returned ±∞ components;
            // the api-reference contract says singular → #VALUE!, so the Core guard
            // must throw instead of silently propagating non-finite values.
            var act = () => LinalgCore.Solve(
                new double[,] { { 1, 1 }, { 1, 1 } },
                new[] { 1.0, 2.0 });
            act.Should().Throw<ArgumentException>().WithMessage("*singular*");
        }

        [Fact] public void Solve_singular_dependent_rows_throws()
        {
            // Linearly dependent rows ([[1,2],[2,4]]) — same guard path.
            var act = () => LinalgCore.Solve(
                new double[,] { { 1, 2 }, { 2, 4 } },
                new[] { 3.0, 6.0 });
            act.Should().Throw<ArgumentException>().WithMessage("*singular*");
        }

        [Fact] public void Eigen_non_symmetric_throws()
        {
            var act = () => LinalgCore.Eigen(B);  // B is not symmetric
            act.Should().Throw<ArgumentException>().WithMessage("*symmetric*");
        }

        [Fact] public void ConditionNumber_singular_returns_NaN()
        {
            // review 2026-09-05（N05）：奇异矩阵 cond=+∞ 原样返回违反模块 Inf→NaN 约定。
            // 原断言 BeGreaterThan(1e10) 隐式接受 +Inf，按新语义更新为 NaN（封顶后）。
            var cond = LinalgCore.ConditionNumber(
                new double[,] { { 1, 2 }, { 2, 4 } });
            double.IsNaN(cond).Should().BeTrue();
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
            // QR with rows=2, cols=2001: wide path now throws NotSupportedException
            var wide = new double[2, 2001];
            var act = () => LinalgCore.Qr(wide);
            act.Should().Throw<NotSupportedException>()
                .WithMessage("*rows >= columns*");
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

        // =====================================================================
        // PYTHON CROSS-VALIDATION (numpy.linalg / scipy.linalg reference)
        //
        // Tests above use self-consistency (A ≈ Q·R, PA ≈ LU).
        // These tests add INDEPENDENT verification against known reference
        // values — identity, diagonal, and SPD matrices whose decompositions
        // are known analytically and verified against numpy/scipy output.
        // =====================================================================

        private static readonly double[,] I3 = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
        private static readonly double[,] Diag3 = { { 2, 0, 0 }, { 0, 3, 0 }, { 0, 0, 5 } };
        private static readonly double[,] SPD2 = { { 4, 2 }, { 2, 3 } };

        [Fact] public void CrossVal_SVD_Identity() { var (_, S, _) = LinalgCore.Svd(I3); S.Should().AllSatisfy(v => v.Should().BeApproximately(1.0, 1e-10)); }
        [Fact] public void CrossVal_SVD_Diagonal() { var (_, S, _) = LinalgCore.Svd(Diag3); var s = S.OrderByDescending(v => v).ToArray(); s[0].Should().BeApproximately(5.0, 1e-10); s[1].Should().BeApproximately(3.0, 1e-10); s[2].Should().BeApproximately(2.0, 1e-10); }
        [Fact] public void CrossVal_QR_Identity() { var (Q, R) = LinalgCore.Qr(I3); DiffFro(I3, LinalgCore.MatMul(Q, R)).Should().BeApproximately(0.0, 1e-10); }
        [Fact] public void CrossVal_Cholesky_SPD() { var L = LinalgCore.Cholesky(SPD2); L[0, 0].Should().BeApproximately(2.0, 1e-10); L[1, 0].Should().BeApproximately(1.0, 1e-10); L[0, 1].Should().BeApproximately(0.0, 1e-10); L[1, 1].Should().BeApproximately(1.414213562373095, 1e-10); }
        [Fact] public void CrossVal_Det_Identity() => LinalgCore.Determinant(I3).Should().BeApproximately(1.0, 1e-10);
        [Fact] public void CrossVal_Det_Diagonal() => LinalgCore.Determinant(Diag3).Should().BeApproximately(30.0, 1e-10);
        [Fact] public void CrossVal_Det_SPD() => LinalgCore.Determinant(SPD2).Should().BeApproximately(8.0, 1e-10);
        [Fact] public void CrossVal_Solve_Identity() { var x = LinalgCore.Solve(I3, new[] { 3.0, 5, 7 }); x[0].Should().BeApproximately(3.0, 1e-10); x[1].Should().BeApproximately(5.0, 1e-10); x[2].Should().BeApproximately(7.0, 1e-10); }
        [Fact] public void CrossVal_Solve_Diagonal() { var x = LinalgCore.Solve(Diag3, new[] { 2.0, 6, 10 }); x[0].Should().BeApproximately(1.0, 1e-10); x[1].Should().BeApproximately(2.0, 1e-10); x[2].Should().BeApproximately(2.0, 1e-10); }
        [Fact] public void CrossVal_Solve_SPD() { var x = LinalgCore.Solve(SPD2, new[] { 10.0, 11 }); x[0].Should().BeApproximately(1.0, 1e-10); x[1].Should().BeApproximately(3.0, 1e-10); }
        [Fact] public void CrossVal_Eigenvalues_Symmetric() { var v = LinalgCore.Eigenvalues(new double[,] { { 2, -1, 0 }, { -1, 2, -1 }, { 0, -1, 2 } }); var s = v.OrderBy(x => x).ToArray(); s[0].Should().BeApproximately(0.585786437626905, 1e-8); s[1].Should().BeApproximately(2.0, 1e-10); s[2].Should().BeApproximately(3.414213562373095, 1e-8); }
        [Fact] public void CrossVal_Cond_Identity() => LinalgCore.ConditionNumber(I3).Should().BeApproximately(1.0, 1e-10);
        [Fact] public void CrossVal_Cond_Diagonal() => LinalgCore.ConditionNumber(Diag3).Should().BeApproximately(2.5, 1e-10);
        [Fact] public void CrossVal_Trace_Diagonal() => LinalgCore.Trace(Diag3).Should().BeApproximately(10.0, 1e-10);
        [Fact] public void CrossVal_NormFrobenius_Diagonal() => LinalgCore.NormFrobenius(Diag3).Should().BeApproximately(6.164414002968976, 1e-10);
        [Fact] public void CrossVal_Pinv_Diagonal() { var Ap = LinalgCore.PseudoInverse(Diag3); Ap[0, 0].Should().BeApproximately(0.5, 1e-10); Ap[1, 1].Should().BeApproximately(1.0 / 3.0, 1e-10); Ap[2, 2].Should().BeApproximately(0.2, 1e-10); }

        // =====================================================================
        // CROSS-VALIDATION: Python scipy.linalg Reference Values
        //
        // Reference script: tests/TestData/generate_python_refs.py
        // Run `python tests/TestData/generate_python_refs.py` to regenerate.
        // =====================================================================

        private static readonly double[] PySvdS_B = { 17.412505166808593, 0.875161350110436, 0.196866521117430 };
        [Fact] public void CrossVal_Svd_S_B() { var (_, S, _) = LinalgCore.Svd(B); for (int i = 0; i < 3; i++) S[i].Should().BeApproximately(PySvdS_B[i], 1e-10); }
        [Fact] public void CrossVal_Svd_S_symmetric() { var (_, S, _) = LinalgCore.Svd(A); S[0].Should().BeApproximately(3.0, 1e-10); S[1].Should().BeApproximately(1.0, 1e-10); }
        private static readonly double[] PyEigen_A3x3 = { 3.414213562373091, 2.000000000000000, 0.585786437626905 };
        [Fact] public void CrossVal_Eigen_tridiagonal() { var v = LinalgCore.Eigenvalues(new double[,] { { 2, -1, 0 }, { -1, 2, -1 }, { 0, -1, 2 } }); var s = v.OrderByDescending(x => x).ToArray(); for (int i = 0; i < 3; i++) s[i].Should().BeApproximately(PyEigen_A3x3[i], 1e-10); }
        [Fact] public void CrossVal_QR_R_diag_abs() { var (_, R) = LinalgCore.Qr(B); Math.Abs(R[0, 0]).Should().BeApproximately(8.124038404635959, 1e-8); Math.Abs(R[1, 1]).Should().BeApproximately(0.904534033733293, 1e-8); Math.Abs(R[2, 2]).Should().BeApproximately(0.408248290463862, 1e-8); }
        [Fact] public void CrossVal_LU_components() { var (L, U, _) = LinalgCore.Lu(B); Math.Abs(L[1, 0]).Should().BeApproximately(0.142857142857143, 1e-8); Math.Abs(L[2, 0]).Should().BeApproximately(0.571428571428571, 1e-8); Math.Abs(L[2, 1]).Should().BeApproximately(0.5, 1e-8); Math.Abs(U[0, 0]).Should().BeApproximately(7.0, 1e-10); Math.Abs(U[1, 1]).Should().BeApproximately(0.857142857142857, 1e-8); Math.Abs(U[2, 2]).Should().BeApproximately(0.5, 1e-8); }
        [Fact] public void CrossVal_Cholesky_L() { var L = LinalgCore.Cholesky(new double[,] { { 4, 2 }, { 2, 3 } }); L[0, 0].Should().BeApproximately(2.0, 1e-10); L[1, 0].Should().BeApproximately(1.0, 1e-10); Math.Abs(L[1, 1]).Should().BeApproximately(1.414213562373095, 1e-10); }
        [Fact] public void CrossVal_Pinv_B_element() { var Ap = LinalgCore.PseudoInverse(B); Ap[0, 0].Should().BeApproximately(-0.666666666666671, 1e-8); }
        [Fact] public void CrossVal_Det_B() => LinalgCore.Determinant(B).Should().BeApproximately(-3.0, 1e-10);
        [Fact] public void CrossVal_Cond_B() => LinalgCore.ConditionNumber(B).Should().BeApproximately(88.448279920698738, 1e-6);
        [Fact] public void CrossVal_Rank_B() => LinalgCore.Rank(B).Should().Be(3);
        [Fact] public void CrossVal_Rank_deficient() => LinalgCore.Rank(new double[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 5, 7, 9 } }).Should().Be(2);
        [Fact] public void CrossVal_NormFro_B() => LinalgCore.NormFrobenius(B).Should().BeApproximately(17.435595774162696, 1e-10);

        private static readonly double[,] NaNM = { { double.NaN, 1 }, { 1, 1 } };
        private static readonly double[,] InfM = { { 1.0, 1 }, { 1, double.PositiveInfinity } };
        private static readonly double[,] I2 = { { 1, 0 }, { 0, 1 } };

    // ── review-2026-08-31：P1-6 / P1-1 回归守卫 ──
    [Fact] public void Rank_relative_tolerance_small_scale()
    {
        // P1-6：diag(1e-11,1e-11) 真秩 2。UDF 默认容差改为 0（相对，numpy 约定）后应返回 2；
        // 修复前默认绝对 1e-10 → 0（错）。
        var m = new double[,] { { 1e-11, 0.0 }, { 0.0, 1e-11 } };
        LinalgCore.Rank(m, tol: 0).Should().Be(2);
    }

    [Fact] public void Lu_docs_convention_A_equals_PLU()
    {
        // P1-1：锁死实现约定 A = P·L·U（文档已同步为 A = P*L*U）。
        // 若实现漂移到 PA=LU（置换非对合时），正向后向断言同时锁定。
        var M = new double[,] { { 0, 2, 1 }, { 1, 0, 3 }, { 4, 5, 6 } };
        var (L, U, P) = LinalgCore.Lu(M);
        DiffFro(M, LinalgCore.MatMul(P, LinalgCore.MatMul(L, U))).Should().BeApproximately(0.0, 1e-9);
        // 文档约定反向断言：P·A ≠ L·U（按 PA=LU 使用会得错误行序）
        DiffFro(LinalgCore.MatMul(P, M), LinalgCore.MatMul(L, U)).Should().BeGreaterThan(1e-6);
    }

    [Fact] public void NormFrobenius_large_scale_no_overflow()
    {
        // P2-35：MathNet FrobeniusNorm 朴素平方和溢出（[[1e200,1e200]] → Inf）；
        // 尺度化实现应返回 1.4142135623730951e200。
        var m = new double[,] { { 1e200, 1e200 } };
        LinalgCore.NormFrobenius(m).Should().BeApproximately(1.4142135623730951e200, 1e185);
    }

    // ── review-2026-09-05（R05）：EnsureSymmetric 纯相对判据 ──
    [Fact] public void Eigenvalues_small_scale_asymmetric_rejected()
    {
        // [[0,0],[1e-9,0]]：相对 100% 非对称。修复前 scale 下限 1.0 → 阈值 1e-8 →
        // diff=1e-9 < 1e-8 被误判对称 → Evd 静默按错误矩阵分解。现与大量纲同路径拒绝。
        var act = () => LinalgCore.Eigenvalues(new double[,] { { 0.0, 0.0 }, { 1e-9, 0.0 } });
        act.Should().Throw<ArgumentException>().WithMessage("*symmetric*");
    }

    [Fact] public void Eigenvalues_zero_matrix_accepted()
    {
        // 全零矩阵：diff=0、scale=0 → `0 > 0` 不触发（无除法，无除零），正常分解。
        var e = LinalgCore.Eigenvalues(new double[2, 2]);
        e.Length.Should().Be(2);
        e.Should().AllBeEquivalentTo(0.0);
    }

    [Fact] public void Eigenvalues_large_scale_rounding_asymmetry_tolerated()
    {
        // P1-5 既有行为锁定：1e9 量纲 ULP≈2.4e-7，理论对称矩阵因舍入差 1 ULP 仍判对称
        // （阈值 = 1e-8×scale，scale 由 Math.Max(1.0,·) 改 maxAbs 后在大量纲下不变）。
        double ulp = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(1e9) + 1) - 1e9;
        var m = new double[,] { { 1e9, 1e9 }, { 1e9 + ulp, 1e9 } };
        LinalgCore.Eigenvalues(m).Length.Should().Be(2);
    }

    // ── review-2026-09-05（N04）：Cholesky 非对称拒绝（与 Eigen 同族对齐）──
    [Fact] public void Cholesky_non_symmetric_throws()
    {
        // 非对称 {1,0.5;0,1} 原先被 MathNet 静默按 {1,0;0.5,1} 分解（只读三角）。
        // 现复用 EnsureSymmetric → 与 Eigenvalues 相同的拒绝路径与异常类型。
        var act = () => LinalgCore.Cholesky(new double[,] { { 1, 0.5 }, { 0, 1 } });
        act.Should().Throw<ArgumentException>().WithMessage("*symmetric*");
    }

    [Fact] public void Cholesky_small_scale_symmetric_ok()
    {
        // R05 联动：小量纲对称矩阵（scale < 1）不再被下限干扰，正常分解。
        // SPD：[[1e-9,0],[0,1e-9]] → L = diag(√1e-9)=diag(3.1622776601683795e-5)。
        var l = LinalgCore.Cholesky(new double[,] { { 1e-9, 0.0 }, { 0.0, 1e-9 } });
        l[0, 0].Should().BeApproximately(3.1622776601683795e-5, 1e-20);
        l[1, 1].Should().BeApproximately(3.1622776601683795e-5, 1e-20);
        l[0, 1].Should().Be(0.0);
        l[1, 0].Should().Be(0.0);
    }

    // ── review-2026-09-05（R21）：求解前条件数守卫 ──
    private static double[,] Hilbert(int n)
    {
        var h = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                h[i, j] = 1.0 / (i + j + 1);
        return h;
    }
    // b = H·ones → 构造解恒为全 1（独立于被测实现构造 b）。
    private static double[] HilbertTimesOnes(int n)
    {
        var b = new double[n];
        for (int i = 0; i < n; i++)
        {
            double s = 0;
            for (int j = 0; j < n; j++) s += 1.0 / (i + j + 1);
            b[i] = s;
        }
        return b;
    }

    [Fact] public void Solve_hilbert12_near_singular_throws()
    {
        // Hilbert 12×12：cond≈1.6e16 > 1e14。修复前 MathNet LU 静默返回错得离谱但
        // 全部有限的解。现求解前显式拒绝；消息含 "singular" 与实测 cond 值。
        var act = () => LinalgCore.Solve(Hilbert(12), HilbertTimesOnes(12));
        act.Should().Throw<ArgumentException>().WithMessage("*singular*condition number*");
    }

    [Fact] public void Solve_hilbert8_ill_but_solvable()
    {
        // Hilbert 8×8：cond≈1.5e10 < 1e14 → 病态但可解区间不得误拒。
        // b = H·ones（手工构造）→ 解 ≈ 全 1（前向误差 ~cond·eps ≈ 3e-6，容差 1e-4）。
        var x = LinalgCore.Solve(Hilbert(8), HilbertTimesOnes(8));
        for (int i = 0; i < 8; i++) x[i].Should().BeApproximately(1.0, 1e-4);
    }

    [Fact] public void Solve_exact_singular_still_throws_singular_message()
    {
        // 精确奇异（cond=+∞ 非有限）→ 新守卫先行抛出；消息保留 "singular" 契约
        // （既有 WithMessage("*singular*") 断言由此继续成立）。
        var act = () => LinalgCore.Solve(new double[,] { { 1, 1 }, { 1, 1 } }, new[] { 1.0, 2.0 });
        act.Should().Throw<ArgumentException>().WithMessage("*singular*condition number*");
    }

    // ── review-2026-09-05（R24）：128 位内容哈希（async topic key / 分解缓存共用）──
    [Fact] public void MatrixHash_is_128bit_and_content_sensitive()
    {
        var k1 = LinalgCore.MatrixHash(new double[,] { { 1, 2 }, { 3, 4 } });
        var k2 = LinalgCore.MatrixHash(new double[,] { { 1, 2 }, { 3, 4.0000001 } });
        var k3 = LinalgCore.MatrixHash(new double[,] { { 1, 2 }, { 3, 4 } });
        k1.Should().Be(k3);      // 同内容 → 同 key
        k1.Should().NotBe(k2);   // 内容不同 → key 不同
        k1.Should().NotBe(LinalgCore.MatrixHash(new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } })); // 维度参与
        // 128 位 = 32 个十六进制位 + "_RxC" 后缀。
        Regex.IsMatch(k1, @"^[0-9A-F]{32}_2x2$").Should().BeTrue();
    }

    [Fact] public void VectorHash_is_128bit_and_content_sensitive()
    {
        var k1 = LinalgCore.VectorHash(new[] { 1.0, 2.0, 3.0 });
        k1.Should().Be(LinalgCore.VectorHash(new[] { 1.0, 2.0, 3.0 }));   // 同内容 → 同 key
        k1.Should().NotBe(LinalgCore.VectorHash(new[] { 1.0, 2.0, 4.0 })); // 内容不同
        k1.Should().NotBe(LinalgCore.VectorHash(new[] { 1.0, 2.0 }));      // 长度参与两路哈希
        k1.Should().NotBe(LinalgCore.MatrixHash(new double[,] { { 1 }, { 2 }, { 3 } })); // 与矩阵 key 域分离
        Regex.IsMatch(k1, @"^V3:[0-9A-F]{32}$").Should().BeTrue();
    }
}
}
