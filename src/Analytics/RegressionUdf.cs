using System.Collections.Generic;
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
          Description = "OLS. Returns 2xn: coefficients, sse, r_squared, adj_r_squared, residuals, fitted_values, standard_errors, t_stats, p_values, n, df. p<0.05 = significant; R虏 near 1 = good fit.")]
        public static object UDF_REGRESS_OLS([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X)
            => OutputWrapper.WrapError(() => DictToReport(RegressionCore.FitOLS(M(X), V(y))));

        [ExcelFunction(Name = "REGRESS.WLS",
          Description = "Weighted Least Squares (heteroskedastic data). Returns 2xn with same 11 keys as OLS: coefficients, sse, r_squared, adj_r_squared, residuals, fitted_values, standard_errors, t_stats, p_values, n, df.")]
        public static object UDF_REGRESS_WLS([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X, [ExcelArgument(Name="weights", Description="Weight values for weighted least squares")] object w)
            => OutputWrapper.WrapError(() => DictToReport(RegressionCore.FitWLS(M(X), V(y), V(w))));

        [ExcelFunction(Name = "REGRESS.RIDGE",
          Description = "Ridge regression (L2 regularization, default lambda=1.0). Returns 2xn: coefficients, sse, r_squared, residuals, fitted_values, lambda, n, df. No se/t/p values (inference invalid under regularization).")]
        public static object UDF_REGRESS_RIDGE([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X, [ExcelArgument(Name="[lambda]", Description="Regularization parameter; default is 1.0")] object lambda=null)
            => OutputWrapper.WrapError(() => DictToReport(RegressionCore.FitRidge(M(X), V(y), lambda==null||lambda is ExcelDna.Integration.ExcelMissing?1.0:InputNormalizer.ToDouble(lambda))));

        [ExcelFunction(Name = "REGRESS.ANOVA1",
          Description = "One-way ANOVA (groups as columns). Returns 2xn: ss_between, ss_within, ss_total, df_between, df_within, df_total, ms_between, ms_within, f_stat, p_value, group_means, group_counts. p<0.05 = groups differ significantly.")]
        public static object UDF_REGRESS_ANOVA1([ExcelArgument(Name="input_range", Description="Input data range with groups as columns")] object data)
            => OutputWrapper.WrapError(() => {
                var m=InputNormalizer.NormalizeTo2D(data)!; int nc=m.GetLength(1); var g=new double[nc][];
                for(int c=0;c<nc;c++){var l=new List<double>();for(int r=0;r<m.GetLength(0);r++){double v=InputNormalizer.ToDouble(m[r,c]);if(!double.IsNaN(v))l.Add(v);}g[c]=l.ToArray();}
                return DictToReport(RegressionCore.AnovaOneWay(g)); });

        [ExcelFunction(Name = "REGRESS.FACTORIMP", Description = "Rank predictor importance by |t| from standardized OLS. Returns 0-based column index array most-to-least important.")]
        public static object UDF_REGRESS_FACTORIMP([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X)
            => OutputWrapper.WrapError(() => RegressionCore.FactorImportance(M(X), V(y)).Select(i=>(long)i).ToArray());

        [ExcelFunction(Name = "REGRESS.COEF", Description = "OLS regression coefficients only (beta vector).")]
        public static object UDF_REGRESS_COEF([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X)
            => OutputWrapper.WrapError(() => (double[])RegressionCore.FitOLS(M(X), V(y))["coefficients"]);

        [ExcelFunction(Name = "REGRESS.RSQ", Description = "OLS R-squared (coefficient of determination). 0-1; 1 = perfect fit.")]
        public static object UDF_REGRESS_RSQ([ExcelArgument(Name="known_y", Description="The Y variable range (dependent variable)")] object y, [ExcelArgument(Name="known_x", Description="The X variable range (independent variables)")] object X)
            => OutputWrapper.WrapError(() => (double)RegressionCore.FitOLS(M(X), V(y))["r_squared"]);

        /// <summary>
        /// Convert a Dictionary{string,object} to an Excel-compatible report table.
        /// Each field becomes a row: column 0 = field name, columns 1.. = scalar or
        /// unpacked array elements. This replaces the old Dict2Row format which put
        /// arrays into single cells — unrenderable in Excel.
        /// </summary>
        private static object[,] DictToReport(Dictionary<string, object> d)
        {
            var keys = d.Keys.ToArray();
            int n = keys.Length;

            // Determine max width from array-valued fields (minimum 1 for scalars)
            int maxLen = 1;
            foreach (var key in keys)
            {
                var val = d[key];
                if (val is double[] da && da.Length > maxLen) maxLen = da.Length;
                else if (val is long[] la && la.Length > maxLen) maxLen = la.Length;
                else if (val is object[] oa && oa.Length > maxLen) maxLen = oa.Length;
                else if (val is System.Array arr && arr.Length > maxLen) maxLen = arr.Length;
            }

            var result = new object[n, maxLen + 1];

            for (int i = 0; i < n; i++)
            {
                result[i, 0] = keys[i];
                var val = d[keys[i]];
                int len = 1;

                if (val is double[] da)
                {
                    for (int j = 0; j < da.Length; j++)
                        result[i, j + 1] = da[j];
                    len = da.Length;
                }
                else if (val is long[] la)
                {
                    for (int j = 0; j < la.Length; j++)
                        result[i, j + 1] = la[j];
                    len = la.Length;
                }
                else if (val is object[] oa)
                {
                    for (int j = 0; j < oa.Length; j++)
                        result[i, j + 1] = oa[j] ?? Foundation.ExcelEmpty.Value;
                    len = oa.Length;
                }
                else if (val is System.Array arr)
                {
                    for (int j = 0; j < arr.Length; j++)
                        result[i, j + 1] = arr.GetValue(j) ?? Foundation.ExcelEmpty.Value;
                    len = arr.Length;
                }
                else
                {
                    result[i, 1] = val ?? Foundation.ExcelEmpty.Value;
                }

                // Pad remaining cells in this row with ExcelEmpty
                for (int j = len + 1; j <= maxLen; j++)
                    result[i, j] = Foundation.ExcelEmpty.Value;
            }

            return result;
        }
    }
}
