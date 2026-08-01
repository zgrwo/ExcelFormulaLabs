using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// Async UDF wrappers for heavy REGRESS computations.
    /// Uses ExcelAsyncUtil.Run to offload regression fitting to a thread-pool thread,
    /// preventing Excel UI freezes on large datasets (10,000+ rows × many predictors).
    /// </summary>
    public static class RegressionAsyncUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        [ExcelFunction(Name = "REGRESS.OLS_ASYNC",
          Description = "OLS regression (async). Same output as REGRESS.OLS but computed on a background thread. Use for large datasets.")]
        public static object UDF_REGRESS_OLS_ASYNC(
            [ExcelArgument(Name = "known_y", Description = "The Y variable range (dependent variable)")] object y,
            [ExcelArgument(Name = "known_x", Description = "The X variable range (independent variables)")] object X)
            => ExcelAsyncUtil.Run(nameof(UDF_REGRESS_OLS_ASYNC), new object[] { y, X }, () =>
                OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(RegressionCore.FitOLS(M(X), V(y)))));

        [ExcelFunction(Name = "REGRESS.WLS_ASYNC",
          Description = "Weighted Least Squares (async). Same output as REGRESS.WLS but computed on a background thread. Use for large datasets.")]
        public static object UDF_REGRESS_WLS_ASYNC(
            [ExcelArgument(Name = "known_y", Description = "The Y variable range (dependent variable)")] object y,
            [ExcelArgument(Name = "known_x", Description = "The X variable range (independent variables)")] object X,
            [ExcelArgument(Name = "weights", Description = "Weight values for weighted least squares")] object w)
            => ExcelAsyncUtil.Run(nameof(UDF_REGRESS_WLS_ASYNC), new object[] { y, X, w }, () =>
                OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(RegressionCore.FitWLS(M(X), V(y), V(w)))));

        [ExcelFunction(Name = "REGRESS.RIDGE_ASYNC",
          Description = "Ridge regression (async). Same output as REGRESS.RIDGE but computed on a background thread. Use for large datasets.")]
        public static object UDF_REGRESS_RIDGE_ASYNC(
            [ExcelArgument(Name = "known_y", Description = "The Y variable range (dependent variable)")] object y,
            [ExcelArgument(Name = "known_x", Description = "The X variable range (independent variables)")] object X,
            [ExcelArgument(Name = "[lambda]", Description = "Regularization parameter; default is 1.0")] object lambda = null)
            => ExcelAsyncUtil.Run(nameof(UDF_REGRESS_RIDGE_ASYNC), new object[] { y, X, lambda }, () =>
                OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(
                    RegressionCore.FitRidge(M(X), V(y),
                        lambda == null || lambda is ExcelMissing ? 1.0 : InputNormalizer.ToDouble(lambda)))));
    }
}
