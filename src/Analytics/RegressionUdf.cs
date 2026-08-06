using System.Linq;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    public static class RegressionUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        [ExcelFunction(Name = "REGRESS.OLS",
          Description = "OLS regression; returns an 11-row report table (coefficients, fit, inference).")]
        public static object UDF_REGRESS_OLS([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X)
            => OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(RegressionCore.FitOLS(M(X), V(y))));

        [ExcelFunction(Name = "REGRESS.WLS",
          Description = "Weighted Least Squares for heteroskedastic data; same 11-row report as OLS.")]
        public static object UDF_REGRESS_WLS([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X, [ExcelArgument(Name="weights", Description="Weight values for weighted least squares")] object w)
            => OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(RegressionCore.FitWLS(M(X), V(y), V(w))));

        [ExcelFunction(Name = "REGRESS.RIDGE",
          Description = "Ridge regression (L2, default lambda=1.0); no se/t/p (inference invalid).")]
        public static object UDF_REGRESS_RIDGE([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X, [ExcelArgument(Name="[lambda]", Description="Regularization parameter; default is 1.0")] object lambda=null)
            => OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(RegressionCore.FitRidge(M(X), V(y), lambda==null||lambda is ExcelDna.Integration.ExcelMissing?1.0:InputNormalizer.ToDouble(lambda))));

        [ExcelFunction(Name = "REGRESS.ANOVA1",
          Description = "One-way ANOVA (groups as columns); returns SS, df, MS, F and p-value report.")]
        public static object UDF_REGRESS_ANOVA1([ExcelArgument(Name="input_range", Description="Input data range with groups as columns")] object data)
            => OutputWrapper.WrapError(() => AnalyticsHelpers.DictToReport(RegressionCore.AnovaOneWay(AnalyticsHelpers.ToJaggedColumns(data))));

        [ExcelFunction(Name = "REGRESS.FACTORIMP", Description = "Rank predictor importance by |t| from standardized OLS (0-based columns).")]
        public static object UDF_REGRESS_FACTORIMP([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X)
            => OutputWrapper.WrapError(() => RegressionCore.FactorImportance(M(X), V(y)).Select(i=>(double)i).ToArray());

        [ExcelFunction(Name = "REGRESS.COEF", Description = "OLS regression coefficients only (beta vector).")]
        public static object UDF_REGRESS_COEF([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X)
            => OutputWrapper.WrapError(() => (double[])RegressionCore.FitOLS(M(X), V(y))["coefficients"]);

        [ExcelFunction(Name = "REGRESS.RSQ", Description = "OLS R-squared (coefficient of determination). 0-1; 1 = perfect fit.")]
        public static object UDF_REGRESS_RSQ([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X)
            => OutputWrapper.WrapError(() => (double)RegressionCore.FitOLS(M(X), V(y))["r_squared"]);
    }
}
