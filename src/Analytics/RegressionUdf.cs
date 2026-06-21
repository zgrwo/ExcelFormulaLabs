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

        [ExcelFunction(Name = "REGRESS.OLS", Description = "OLS regression. Returns 2×n: keys row0, values row1.")]
        public static object UDF_REGRESS_OLS(object X, object y)
            => OutputWrapper.WrapError(() => Dict2Row(RegressionCore.FitOLS(M(X), V(y))));

        [ExcelFunction(Name = "REGRESS.WLS", Description = "Weighted Least Squares.")]
        public static object UDF_REGRESS_WLS(object X, object y, object w)
            => OutputWrapper.WrapError(() => Dict2Row(RegressionCore.FitWLS(M(X), V(y), V(w))));

        [ExcelFunction(Name = "REGRESS.RIDGE", Description = "Ridge Regression.")]
        public static object UDF_REGRESS_RIDGE(object X, object y, object lambda)
            => OutputWrapper.WrapError(() => Dict2Row(RegressionCore.FitRidge(M(X), V(y), InputNormalizer.ToDouble(lambda))));

        [ExcelFunction(Name = "REGRESS.ANOVA1", Description = "One-way ANOVA. Groups as columns.")]
        public static object UDF_REGRESS_ANOVA1(object data)
            => OutputWrapper.WrapError(() => {
                var m=InputNormalizer.NormalizeTo2D(data)!; int nc=m.GetLength(1); var g=new double[nc][];
                for(int c=0;c<nc;c++){var l=new List<double>();for(int r=0;r<m.GetLength(0);r++){double v=InputNormalizer.ToDouble(m[r,c]);if(!double.IsNaN(v))l.Add(v);}g[c]=l.ToArray();}
                return Dict2Row(RegressionCore.AnovaOneWay(g)); });

        [ExcelFunction(Name = "REGRESS.FACTORIMP", Description = "Factor importance rank.")]
        public static object UDF_REGRESS_FACTORIMP(object X, object y)
            => OutputWrapper.WrapError(() => RegressionCore.FactorImportance(M(X), V(y)).Select(i=>(long)i).ToArray());

        [ExcelFunction(Name = "REGRESS.COEF", Description = "OLS coefficients.")]
        public static object UDF_REGRESS_COEF(object X, object y)
            => OutputWrapper.WrapError(() => (double[])RegressionCore.FitOLS(M(X), V(y))["coefficients"]);

        [ExcelFunction(Name = "REGRESS.RSQ", Description = "OLS R-squared.")]
        public static object UDF_REGRESS_RSQ(object X, object y)
            => OutputWrapper.WrapError(() => (double)RegressionCore.FitOLS(M(X), V(y))["r_squared"]);

        private static object[,] Dict2Row(Dictionary<string,object> d)
        { var k=d.Keys.ToArray(); var r=new object[2,k.Length]; for(int i=0;i<k.Length;i++){r[0,i]=k[i];r[1,i]=d[k[i]];} return r; }
    }
}
