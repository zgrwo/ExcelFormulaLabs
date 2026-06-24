using System.Collections.Generic;
using System.Linq;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
{
    public static class RegressionUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        [ExcelFunction(Name = "REGRESS.OLS",
          Description = "OLS. Returns 2xn: coefficients, sse, r_squared, adj_r_squared, residuals, fitted_values, standard_errors, t_stats, p_values, n, df. p<0.05 = significant; R² near 1 = good fit.")]
        public static object UDF_REGRESS_OLS([ExcelArgument("X")] object X, [ExcelArgument("y")] object y)
            => OutputWrapper.WrapError(() => Dict2Row(RegressionCore.FitOLS(M(X), V(y))));

        [ExcelFunction(Name = "REGRESS.WLS",
          Description = "Weighted Least Squares (heteroskedastic data). Returns 2xn with same 11 keys as OLS: coefficients, sse, r_squared, adj_r_squared, residuals, fitted_values, standard_errors, t_stats, p_values, n, df.")]
        public static object UDF_REGRESS_WLS([ExcelArgument("X")] object X, [ExcelArgument("y")] object y, [ExcelArgument("w")] object w)
            => OutputWrapper.WrapError(() => Dict2Row(RegressionCore.FitWLS(M(X), V(y), V(w))));

        [ExcelFunction(Name = "REGRESS.RIDGE",
          Description = "Ridge regression (L2 regularization, default lambda=1.0). Returns 2xn: coefficients, sse, r_squared, residuals, fitted_values, lambda, n, df. No se/t/p values (inference invalid under regularization).")]
        public static object UDF_REGRESS_RIDGE([ExcelArgument("X")] object X, [ExcelArgument("y")] object y, [ExcelArgument("lambda")] object lambda)
            => OutputWrapper.WrapError(() => Dict2Row(RegressionCore.FitRidge(M(X), V(y), lambda is ExcelDna.Integration.ExcelMissing ? 1.0 : InputNormalizer.ToDouble(lambda))));

        [ExcelFunction(Name = "REGRESS.ANOVA1",
          Description = "One-way ANOVA (groups as columns). Returns 2xn: ss_between, ss_within, ss_total, df_between, df_within, df_total, ms_between, ms_within, f_stat, p_value, group_means, group_counts. p<0.05 = groups differ significantly.")]
        public static object UDF_REGRESS_ANOVA1([ExcelArgument("data")] object data)
            => OutputWrapper.WrapError(() => {
                var m=InputNormalizer.NormalizeTo2D(data)!; int nc=m.GetLength(1); var g=new double[nc][];
                for(int c=0;c<nc;c++){var l=new List<double>();for(int r=0;r<m.GetLength(0);r++){double v=InputNormalizer.ToDouble(m[r,c]);if(!double.IsNaN(v))l.Add(v);}g[c]=l.ToArray();}
                return Dict2Row(RegressionCore.AnovaOneWay(g)); });

        [ExcelFunction(Name = "REGRESS.FACTORIMP", Description = "Rank predictor importance by |t| from standardized OLS. Returns 0-based column index array most-to-least important.")]
        public static object UDF_REGRESS_FACTORIMP([ExcelArgument("X")] object X, [ExcelArgument("y")] object y)
            => OutputWrapper.WrapError(() => RegressionCore.FactorImportance(M(X), V(y)).Select(i=>(long)i).ToArray());

        [ExcelFunction(Name = "REGRESS.COEF", Description = "OLS regression coefficients only (beta vector).")]
        public static object UDF_REGRESS_COEF([ExcelArgument("X")] object X, [ExcelArgument("y")] object y)
            => OutputWrapper.WrapError(() => (double[])RegressionCore.FitOLS(M(X), V(y))["coefficients"]);

        [ExcelFunction(Name = "REGRESS.RSQ", Description = "OLS R-squared (coefficient of determination). 0-1; 1 = perfect fit.")]
        public static object UDF_REGRESS_RSQ([ExcelArgument("X")] object X, [ExcelArgument("y")] object y)
            => OutputWrapper.WrapError(() => (double)RegressionCore.FitOLS(M(X), V(y))["r_squared"]);

        private static object[,] Dict2Row(Dictionary<string,object> d)
        { var k=d.Keys.ToArray(); var r=new object[2,k.Length]; for(int i=0;i<k.Length;i++){r[0,i]=k[i];r[1,i]=d[k[i]];} return r; }
    }
}
