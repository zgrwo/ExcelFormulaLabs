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
        // 调用线程完成 COM/类型转换，lambda 只接收纯 double[,]/double[]；key 用紧凑哈希。
        private static object AsyncKey(double[,] m)
        {
            unchecked
            {
                long h = 17;
                for (int r = 0; r < m.GetLength(0); r++)
                    for (int c = 0; c < m.GetLength(1); c++)
                        h = h * 31 + BitConverter.DoubleToInt64Bits(m[r, c]);
                return $"{m.GetLength(0)}x{m.GetLength(1)}:{h:X16}";
            }
        }

        private static object AsyncKeyV(double[] v)
        {
            unchecked
            {
                long h = 17;
                for (int i = 0; i < v.Length; i++)
                    h = h * 31 + BitConverter.DoubleToInt64Bits(v[i]);
                return $"V{v.Length}:{h:X16}";
            }
        }

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
