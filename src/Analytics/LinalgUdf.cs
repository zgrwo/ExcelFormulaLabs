using System.Runtime.InteropServices;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
{
    public static class LinalgUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        [ExcelFunction(Name = "LINALG.SVD", Description = "SVD: A = U*diag(S)*Vt. Returns 1x3 horizontal array {U matrix, S vector, Vt matrix}.")]
        public static object UDF_LINALG_SVD([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => { var (U,S,Vt)=LinalgCore.Svd(M(d)); return new object[]{U,S,Vt}; });

        [ExcelFunction(Name = "LINALG.PINV", Description = "Moore-Penrose pseudo-inverse.")]
        public static object UDF_LINALG_PINV([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => LinalgCore.PseudoInverse(M(d)));

        [ExcelFunction(Name = "LINALG.QR", Description = "QR: A = Q*R. Returns 1x2 horizontal array {Q matrix, R upper-triangular matrix}.")]
        public static object UDF_LINALG_QR([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => { var (Q,R)=LinalgCore.Qr(M(d)); return new[]{Q,R}; });

        [ExcelFunction(Name = "LINALG.LU", Description = "LU: PA = LU with partial pivoting. Returns 1x3 horizontal array {L lower-triangular, U upper-triangular, P permutation matrix}.")]
        public static object UDF_LINALG_LU([ExcelArgument(Name="array", Description="A range or 2D array")] object d)
            => OutputWrapper.WrapError(() => { var (L,U,P)=LinalgCore.Lu(M(d)); return new[]{L,U,P}; });

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
        public static object UDF_LINALG_TRANSPOSE(object d)
            => OutputWrapper.WrapError(() => LinalgCore.Transpose(M(d)));

        [ExcelFunction(Name = "LINALG.TRACE", Description = "Trace.")]
        public static object UDF_LINALG_TRACE(object d)
            => OutputWrapper.WrapError(() => LinalgCore.Trace(M(d)));
    }
}
