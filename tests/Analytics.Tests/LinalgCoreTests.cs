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
        [Fact] public void Eigenvalues() => LinalgCore.Eigenvalues(B).Length.Should().Be(3);
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
    }
}
