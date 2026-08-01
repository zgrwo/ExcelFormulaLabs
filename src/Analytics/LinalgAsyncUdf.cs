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

        [ExcelFunction(Name = "LINALG.SVD_U_ASYNC", Description = "SVD left singular vectors (U matrix), computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_SVD_U_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
            => ExcelAsyncUtil.Run(nameof(UDF_LINALG_SVD_U_ASYNC), d, () =>
                OutputWrapper.WrapError(() => LinalgCore.SvdU(M(d))));

        [ExcelFunction(Name = "LINALG.SVD_S_ASYNC", Description = "SVD singular values (S vector), computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_SVD_S_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
            => ExcelAsyncUtil.Run(nameof(UDF_LINALG_SVD_S_ASYNC), d, () =>
                OutputWrapper.WrapError(() => LinalgCore.SvdS(M(d))));

        [ExcelFunction(Name = "LINALG.SVD_VT_ASYNC", Description = "SVD right singular vectors transposed (Vt matrix), computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_SVD_VT_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
            => ExcelAsyncUtil.Run(nameof(UDF_LINALG_SVD_VT_ASYNC), d, () =>
                OutputWrapper.WrapError(() => LinalgCore.SvdVt(M(d))));

        // ── QR (async) ──────────────────────────────────────────────

        [ExcelFunction(Name = "LINALG.QR_Q_ASYNC", Description = "QR decomposition orthogonal matrix Q, computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_QR_Q_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
            => ExcelAsyncUtil.Run(nameof(UDF_LINALG_QR_Q_ASYNC), d, () =>
                OutputWrapper.WrapError(() => LinalgCore.QrQ(M(d))));

        [ExcelFunction(Name = "LINALG.QR_R_ASYNC", Description = "QR decomposition upper-triangular matrix R, computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_QR_R_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
            => ExcelAsyncUtil.Run(nameof(UDF_LINALG_QR_R_ASYNC), d, () =>
                OutputWrapper.WrapError(() => LinalgCore.QrR(M(d))));

        // ── Eigen (async) ───────────────────────────────────────────

        [ExcelFunction(Name = "LINALG.EIGEN_ASYNC", Description = "Eigenvalues (symmetric matrix), computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_EIGEN_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
            => ExcelAsyncUtil.Run(nameof(UDF_LINALG_EIGEN_ASYNC), d, () =>
                OutputWrapper.WrapError(() => LinalgCore.Eigenvalues(M(d))));

        // ── Solve (async) ───────────────────────────────────────────

        [ExcelFunction(Name = "LINALG.SOLVE_ASYNC", Description = "Solve Ax=b, computed asynchronously. Use for large systems.")]
        public static object UDF_LINALG_SOLVE_ASYNC(
            [ExcelArgument(Name = "array1", Description = "Coefficient matrix A")] object A,
            [ExcelArgument(Name = "array2", Description = "Right-hand side vector b")] object b)
            => ExcelAsyncUtil.Run(nameof(UDF_LINALG_SOLVE_ASYNC), new object[] { A, b }, () =>
                OutputWrapper.WrapError(() => LinalgCore.Solve(M(A), V(b))));

        // ── Cholesky (async) ────────────────────────────────────────

        [ExcelFunction(Name = "LINALG.CHOLESKY_ASYNC", Description = "Cholesky decomposition, computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_CHOLESKY_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
            => ExcelAsyncUtil.Run(nameof(UDF_LINALG_CHOLESKY_ASYNC), d, () =>
                OutputWrapper.WrapError(() => LinalgCore.Cholesky(M(d))));

        // ── Pseudo-Inverse (async) ──────────────────────────────────

        [ExcelFunction(Name = "LINALG.PINV_ASYNC", Description = "Moore-Penrose pseudo-inverse, computed asynchronously. Use for large matrices.")]
        public static object UDF_LINALG_PINV_ASYNC([ExcelArgument(Name = "array", Description = "A range or 2D array")] object d)
            => ExcelAsyncUtil.Run(nameof(UDF_LINALG_PINV_ASYNC), d, () =>
                OutputWrapper.WrapError(() => LinalgCore.PseudoInverse(M(d))));
    }
}
