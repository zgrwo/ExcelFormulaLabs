using System;
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

        // review 2026-08-31（深度审查 P1-20）：M/V 转换移出 ExcelAsyncUtil.Run lambda（同 LinalgAsyncUdf）——
        // 调用线程完成 COM/类型转换，lambda 只接收纯 double[,]/double[]。
        // review 2026-09-05（R24）：topic key 原为单 64 位 31-进制哈希——碰撞会使 RTD 把
        // 另一组输入的缓存结果静默返回给本单元格。改为复用 LinalgCore 的 128 位双 FNV-1a
        // 内容哈希（与 DecompCache.MatrixHash 同一实现），对齐同模块 key 方案。
        private static object AsyncKey(double[,] m) => LinalgCore.MatrixHash(m);

        private static object AsyncKeyV(double[] v) => LinalgCore.VectorHash(v);

        [ExcelFunction(Name = "REGRESS.OLS_ASYNC",
          Description = "OLS regression, computed asynchronously on a background thread.")]
        public static object UDF_REGRESS_OLS_ASYNC(
            [ExcelArgument(Name = "known_y", Description = "The Y variable range (dependent variable)")] object y,
            [ExcelArgument(Name = "known_x", Description = "The X variable range (independent variables)")] object X)
        {
            double[,] mX = M(X);
            double[] vY = V(y);
            return ExcelAsyncUtil.Run(nameof(UDF_REGRESS_OLS_ASYNC), new object[] { AsyncKey(mX), AsyncKeyV(vY) }, () =>
                OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(RegressionCore.FitOLS(mX, vY))));
        }

        [ExcelFunction(Name = "REGRESS.WLS_ASYNC",
          Description = "Weighted Least Squares, computed asynchronously on a background thread.")]
        public static object UDF_REGRESS_WLS_ASYNC(
            [ExcelArgument(Name = "known_y", Description = "The Y variable range (dependent variable)")] object y,
            [ExcelArgument(Name = "known_x", Description = "The X variable range (independent variables)")] object X,
            [ExcelArgument(Name = "weights", Description = "Weight values for weighted least squares")] object w)
        {
            double[,] mX = M(X);
            double[] vY = V(y);
            double[] vW = V(w);
            return ExcelAsyncUtil.Run(nameof(UDF_REGRESS_WLS_ASYNC), new object[] { AsyncKey(mX), AsyncKeyV(vY), AsyncKeyV(vW) }, () =>
                OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(RegressionCore.FitWLS(mX, vY, vW))));
        }

        [ExcelFunction(Name = "REGRESS.RIDGE_ASYNC",
          Description = "Ridge regression, computed asynchronously on a background thread.")]
        public static object UDF_REGRESS_RIDGE_ASYNC(
            [ExcelArgument(Name = "known_y", Description = "The Y variable range (dependent variable)")] object y,
            [ExcelArgument(Name = "known_x", Description = "The X variable range (independent variables)")] object X,
            [ExcelArgument(Name = "[lambda]", Description = "Regularization parameter; default is 1.0")] object lambda = null)
        {
            double[,] mX = M(X);
            double[] vY = V(y);
            double lam = lambda == null || lambda is ExcelMissing ? 1.0 : InputNormalizer.ToDouble(lambda);
            return ExcelAsyncUtil.Run(nameof(UDF_REGRESS_RIDGE_ASYNC), new object[] { AsyncKey(mX), AsyncKeyV(vY), lam }, () =>
                OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(
                    RegressionCore.FitRidge(mX, vY, lam))));
        }
    }
}
