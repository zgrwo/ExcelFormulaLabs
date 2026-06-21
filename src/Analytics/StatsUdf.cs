using System;
using System.Linq;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
{
    public static class StatsUdf
    {
        private static double[] V(object d)=>AnalyticsHelpers.PrepV(d);
        [ExcelFunction(Name="STATS.MEAN")] public static object UDF_STAT_MEAN(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Mean(V(d)));
        [ExcelFunction(Name="STATS.GEOMEAN")] public static object UDF_STAT_GEO(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.GeometricMean(V(d)));
        [ExcelFunction(Name="STATS.HARMEAN")] public static object UDF_STAT_HAR(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.HarmonicMean(V(d)));
        [ExcelFunction(Name="STATS.MEDIAN")] public static object UDF_STAT_MED(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Median(V(d)));
        [ExcelFunction(Name="STATS.VARP")] public static object UDF_STAT_VP(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.VarianceP(V(d)));
        [ExcelFunction(Name="STATS.VAR")] public static object UDF_STAT_VAR(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Variance(V(d)));
        [ExcelFunction(Name="STATS.STDEVP")] public static object UDF_STAT_STDP(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.StdevP(V(d)));
        [ExcelFunction(Name="STATS.STDEV")] public static object UDF_STAT_STD(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Stdev(V(d)));
        [ExcelFunction(Name="STATS.SKEW")] public static object UDF_STAT_SKEW(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Skewness(V(d)));
        [ExcelFunction(Name="STATS.KURT")] public static object UDF_STAT_KURT(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Kurtosis(V(d)));
        [ExcelFunction(Name="STATS.MIN")] public static object UDF_STAT_MIN(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Min(V(d)));
        [ExcelFunction(Name="STATS.MAX")] public static object UDF_STAT_MAX(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Max(V(d)));
        [ExcelFunction(Name="STATS.RANGE")] public static object UDF_STAT_RNG(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Range(V(d)));
        [ExcelFunction(Name="STATS.SUM")] public static object UDF_STAT_SUM(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Sum(V(d)));
        [ExcelFunction(Name="STATS.PRODUCT")] public static object UDF_STAT_PROD(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Product(V(d)));
        [ExcelFunction(Name="STATS.PERCENTILE")] public static object UDF_STAT_PCT(object d,object p)=>OutputWrapper.WrapError(()=>(object)StatsCore.Percentile(V(d),InputNormalizer.ToDouble(p)));
        [ExcelFunction(Name="STATS.IQR")] public static object UDF_STAT_IQR(object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.IQR(V(d)));
        [ExcelFunction(Name="STATS.SUMMARY")] public static object UDF_STAT_SUMM(object d)=>OutputWrapper.WrapError(()=>StatsCore.Summary(V(d)));
        [ExcelFunction(Name="STATS.COVARP")] public static object UDF_STAT_CVP(object a,object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.CovarianceP(V(a),V(b)));
        [ExcelFunction(Name="STATS.COVAR")] public static object UDF_STAT_CV(object a,object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.Covariance(V(a),V(b)));
        [ExcelFunction(Name="STATS.PEARSON")] public static object UDF_STAT_PEAR(object a,object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.Pearson(V(a),V(b)));
        [ExcelFunction(Name="STATS.SPEARMAN")] public static object UDF_STAT_SPR(object a,object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.Spearman(V(a),V(b)));
        [ExcelFunction(Name="STATS.TTEST1")] public static object UDF_STAT_T1(object d,object mu)=>OutputWrapper.WrapError(()=>(object)StatsCore.TTestOneSample(V(d),InputNormalizer.ToDouble(mu)));
        [ExcelFunction(Name="STATS.TTEST2")] public static object UDF_STAT_T2(object a,object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.TTestTwoSample(V(a),V(b)));
        [ExcelFunction(Name="STATS.ZSCORE")] public static object UDF_STAT_ZS(object d)=>OutputWrapper.WrapError(()=>StatsCore.ZScore(V(d)));
        [ExcelFunction(Name="STATS.COUNT")] public static object UDF_STAT_CNT(object d)=>OutputWrapper.WrapError(()=>V(d).Length);
        [ExcelFunction(Name="STATS.MODE")] public static object UDF_STAT_MODE(object d)=>OutputWrapper.WrapError(()=>{var a=V(d);return a.GroupBy(x=>x).OrderByDescending(g=>g.Count()).First().Key;});
        [ExcelFunction(Name="STATS.ABS")] public static object UDF_STAT_ABS(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverFlat<double,double>(d,Math.Abs));
        [ExcelFunction(Name="STATS.SQRT")] public static object UDF_STAT_SQRT(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverFlat<double,double>(d,Math.Sqrt));
        [ExcelFunction(Name="STATS.LN")] public static object UDF_STAT_LN(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverFlat<double,double>(d,Math.Log));
        [ExcelFunction(Name="STATS.LOG10")] public static object UDF_STAT_LOG10(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverFlat<double,double>(d,Math.Log10));
        [ExcelFunction(Name="STATS.EXP")] public static object UDF_STAT_EXP(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverFlat<double,double>(d,Math.Exp));
        [ExcelFunction(Name="STATS.SIGN")] public static object UDF_STAT_SGN(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOverFlat<double,long>(d,x=>(long)Math.Sign(x)));
    }
}
