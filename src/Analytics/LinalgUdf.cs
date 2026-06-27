using ExcelDna.Integration;
using FormulaLabs.Foundation;

namespace FormulaLabs.Analytics
{
    public static class LinalgUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        // ── SVD (split into 3 individual UDFs) ──────────────────────

        [ExcelFunction(Name = "LINALG.SVD_U", Description = "SVD left singular vectors (U matrix). A = U*diag(S)*Vt.")]
        public static object UDF_LINALG_SVD_U([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.SvdU(M(d)));

        [ExcelFunction(Name = "LINALG.SVD_S", Description = "SVD singular values (S vector). Sorted descending.")]
        public static object UDF_LINALG_SVD_S([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.SvdS(M(d)));

        [ExcelFunction(Name = "LINALG.SVD_VT", Description = "SVD right singular vectors transposed (Vt matrix). A = U*diag(S)*Vt.")]
        public static object UDF_LINALG_SVD_VT([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.SvdVt(M(d)));

        // ── QR (split into 2 individual UDFs) ───────────────────────

        [ExcelFunction(Name = "LINALG.QR_Q", Description = "QR decomposition orthogonal matrix Q. A = Q*R.")]
        public static object UDF_LINALG_QR_Q([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.QrQ(M(d)));

        [ExcelFunction(Name = "LINALG.QR_R", Description = "QR decomposition upper-triangular matrix R. A = Q*R.")]
        public static object UDF_LINALG_QR_R([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.QrR(M(d)));

        // ── LU (split into 3 individual UDFs) ───────────────────────

        [ExcelFunction(Name = "LINALG.LU_L", Description = "LU decomposition lower-triangular matrix L. PA = LU.")]
        public static object UDF_LINALG_LU_L([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.LuL(M(d)));

        [ExcelFunction(Name = "LINALG.LU_U", Description = "LU decomposition upper-triangular matrix U. PA = LU.")]
        public static object UDF_LINALG_LU_U([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.LuU(M(d)));

        [ExcelFunction(Name = "LINALG.LU_P", Description = "LU decomposition permutation matrix P. PA = LU.")]
        public static object UDF_LINALG_LU_P([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.LuP(M(d)));

        // ── Other LINALG functions ──────────────────────────────────

        [ExcelFunction(Name = "LINALG.PINV", Description = "Moore-Penrose pseudo-inverse.")]
        public static object UDF_LINALG_PINV([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.PseudoInverse(M(d)));

        [ExcelFunction(Name = "LINALG.DET", Description = "Determinant.")]
        public static object UDF_LINALG_DET([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.Determinant(M(d)));

        [ExcelFunction(Name = "LINALG.SOLVE", Description = "Solve Ax=b.")]
        public static object UDF_LINALG_SOLVE([ExcelArgument(Name="array1", Description="First range or array")] object A, [ExcelArgument(Name="array2", Description="Second range or array")] object b)
            => OutputWrapper.WrapError(() => LinalgCore.Solve(M(A), V(b)));

        [ExcelFunction(Name = "LINALG.CHOLESKY", Description = "Cholesky decomposition.")]
        public static object UDF_LINALG_CHOLESKY([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.Cholesky(M(d)));

        [ExcelFunction(Name = "LINALG.EIGEN", Description = "Eigenvalues.")]
        public static object UDF_LINALG_EIGEN([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.Eigenvalues(M(d)));

        [ExcelFunction(Name = "LINALG.COND", Description = "Condition number.")]
        public static object UDF_LINALG_COND([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.ConditionNumber(M(d)));

        [ExcelFunction(Name = "LINALG.RANK", Description = "Numerical rank (default tol=1e-10).")]
        public static object UDF_LINALG_RANK([ExcelArgument(Name="array", Description="A range or 2D array")] object d, [ExcelArgument(Name="[tolerance]", Description="Tolerance threshold for numerical rank detection; default 1e-10")] object tol=null)
            => OutputWrapper.WrapError(() => (long)LinalgCore.Rank(M(d), tol==null||tol is ExcelDna.Integration.ExcelMissing?1e-10:InputNormalizer.ToDouble(tol)));

        [ExcelFunction(Name = "LINALG.IDENTITY", Description = "Identity matrix.")]
        public static object UDF_LINALG_IDENTITY([ExcelArgument(Name="size", Description="The matrix dimension, e.g. 3 for a 3x3 identity matrix")] object n)
            => OutputWrapper.WrapError(() => LinalgCore.Identity((int)InputNormalizer.ToLong(n)));

        [ExcelFunction(Name = "LINALG.MATMUL", Description = "Matrix multiplication.")]
        public static object UDF_LINALG_MATMUL([ExcelArgument(Name="array1", Description="First range or array")] object A, [ExcelArgument(Name="array2", Description="Second range or array")] object B)
            => OutputWrapper.WrapError(() => LinalgCore.MatMul(M(A), M(B)));

        [ExcelFunction(Name = "LINALG.TRANSPOSE", Description = "Transpose.")]
        public static object UDF_LINALG_TRANSPOSE([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.Transpose(M(d)));

        [ExcelFunction(Name = "LINALG.TRACE", Description = "Trace.")]
        public static object UDF_LINALG_TRACE([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.Trace(M(d)));
    }
}
