using System;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// Async UDF wrappers for heavy LINALG computations.
    /// Uses ExcelAsyncUtil.Run to offload matrix decompositions to a thread-pool thread,
    /// preventing Excel UI freezes on large matrices (500×500+).
    /// <para>
    /// While computing, the cell shows #N/A (Excel's "calculating" indicator).
    /// Once complete, the result appears automatically.
    /// </para>
    /// <remarks>
    /// Only pure-computation functions are wrapped here — no COM calls, no side effects.
    /// Sync versions remain available for small matrices where async overhead is unnecessary.
    /// </remarks>
    /// </summary>
    public static class LinalgAsyncUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        // ── SVD (async) ─────────────────────────────────────────────

        // review 2026-08-31（深度审查 P1-20）：M(d)/V(d) 原在 ExcelAsyncUtil.Run 的 lambda 内——
        // 委托在线程池线程执行，而 PrepM/PrepV → NormalizeTo2D → TryExtractComRangeValue 会做
        // Marshal.IsComObject + dynamic COM 派发，跨线程触碰 COM 是 Excel-DNA 异步契约禁止的
        // （经典随机崩溃模式）。全部改为在调用线程完成转换，lambda 只接收纯 double[,]/double[]。
        // review 2026-09-05（R24）：topic key 原为单 64 位 31-进制哈希——哈希碰撞会使 RTD 把
        // 另一个矩阵/向量的缓存结果静默返回给本单元格（错值无任何信号）。改为复用 LinalgCore
        // 的 128 位双 FNV-1a 内容哈希（与 DecompCache.MatrixHash 同一实现：32 个十六进制位
        // + 维度/长度后缀），碰撞概率与分解缓存对齐。
        private static object AsyncKey(double[,] m) => LinalgCore.MatrixHash(m);

        private static object AsyncKeyV(double[] v) => LinalgCore.VectorHash(v);

        [ExcelFunction(Name = "LINALG.SVD_U_ASYNC", Description = "SVD left singular vectors (U matrix), computed asynchronously.")]
        public static object UDF_LINALG_SVD_U_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
        {
            double[,] m = M(d);
            return ExcelAsyncUtil.Run(nameof(UDF_LINALG_SVD_U_ASYNC), AsyncKey(m), () =>
                OutputWrapper.WrapError(() => LinalgCore.SvdU(m)));
        }

        [ExcelFunction(Name = "LINALG.SVD_S_ASYNC", Description = "SVD singular values (S vector), computed asynchronously.")]
        public static object UDF_LINALG_SVD_S_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
        {
            double[,] m = M(d);
            return ExcelAsyncUtil.Run(nameof(UDF_LINALG_SVD_S_ASYNC), AsyncKey(m), () =>
                OutputWrapper.WrapError(() => LinalgCore.SvdS(m)));
        }

        [ExcelFunction(Name = "LINALG.SVD_VT_ASYNC", Description = "SVD right singular vectors transposed (Vt), computed asynchronously.")]
        public static object UDF_LINALG_SVD_VT_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
        {
            double[,] m = M(d);
            return ExcelAsyncUtil.Run(nameof(UDF_LINALG_SVD_VT_ASYNC), AsyncKey(m), () =>
                OutputWrapper.WrapError(() => LinalgCore.SvdVt(m)));
        }

        // ── QR (async) ──────────────────────────────────────────────

        [ExcelFunction(Name = "LINALG.QR_Q_ASYNC", Description = "QR decomposition orthogonal matrix Q, computed asynchronously.")]
        public static object UDF_LINALG_QR_Q_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
        {
            double[,] m = M(d);
            return ExcelAsyncUtil.Run(nameof(UDF_LINALG_QR_Q_ASYNC), AsyncKey(m), () =>
                OutputWrapper.WrapError(() => LinalgCore.QrQ(m)));
        }

        [ExcelFunction(Name = "LINALG.QR_R_ASYNC", Description = "QR decomposition upper-triangular matrix R, computed asynchronously.")]
        public static object UDF_LINALG_QR_R_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
        {
            double[,] m = M(d);
            return ExcelAsyncUtil.Run(nameof(UDF_LINALG_QR_R_ASYNC), AsyncKey(m), () =>
                OutputWrapper.WrapError(() => LinalgCore.QrR(m)));
        }

        // ── Eigen (async) ───────────────────────────────────────────

        [ExcelFunction(Name = "LINALG.EIGEN_ASYNC", Description = "Eigenvalues (symmetric matrix), computed asynchronously.")]
        public static object UDF_LINALG_EIGEN_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
        {
            double[,] m = M(d);
            return ExcelAsyncUtil.Run(nameof(UDF_LINALG_EIGEN_ASYNC), AsyncKey(m), () =>
                OutputWrapper.WrapError(() => LinalgCore.Eigenvalues(m)));
        }

        // ── Solve (async) ───────────────────────────────────────────

        [ExcelFunction(Name = "LINALG.SOLVE_ASYNC", Description = "Solve Ax=b, computed asynchronously. Use for large systems.")]
        public static object UDF_LINALG_SOLVE_ASYNC(
            [ExcelArgument(Name = "array1", Description = "Coefficient matrix A")] object A,
            [ExcelArgument(Name = "array2", Description = "Right-hand side vector b")] object b)
        {
            double[,] mA = M(A);
            double[] vB = V(b);
            return ExcelAsyncUtil.Run(nameof(UDF_LINALG_SOLVE_ASYNC), new object[] { AsyncKey(mA), AsyncKeyV(vB) }, () =>
                OutputWrapper.WrapError(() => LinalgCore.Solve(mA, vB)));
        }

        // ── Cholesky (async) ────────────────────────────────────────

        [ExcelFunction(Name = "LINALG.CHOLESKY_ASYNC", Description = "Cholesky decomposition, computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_CHOLESKY_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
        {
            double[,] m = M(d);
            return ExcelAsyncUtil.Run(nameof(UDF_LINALG_CHOLESKY_ASYNC), AsyncKey(m), () =>
                OutputWrapper.WrapError(() => LinalgCore.Cholesky(m)));
        }

        // ── Pseudo-Inverse (async) ──────────────────────────────────

        [ExcelFunction(Name = "LINALG.PINV_ASYNC", Description = "Moore-Penrose pseudo-inverse, computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_PINV_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
        {
            double[,] m = M(d);
            return ExcelAsyncUtil.Run(nameof(UDF_LINALG_PINV_ASYNC), AsyncKey(m), () =>
                OutputWrapper.WrapError(() => LinalgCore.PseudoInverse(m)));
        }
    }
}
