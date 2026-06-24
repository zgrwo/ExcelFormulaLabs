using System;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.Analytics
{
    public static class StatsUdf
    {
        private static double[] V(object d)=>AnalyticsHelpers.PrepV(d);
        [ExcelFunction(Name="STATS.MEAN", Description="Arithmetic mean of a numeric array")] public static object UDF_STAT_MEAN([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Mean(V(d)));
        [ExcelFunction(Name="STATS.GEOMEAN", Description="Geometric mean of a positive numeric array")] public static object UDF_STAT_GEO([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.GeometricMean(V(d)));
        [ExcelFunction(Name="STATS.HARMEAN", Description="Harmonic mean of a positive numeric array")] public static object UDF_STAT_HAR([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.HarmonicMean(V(d)));
        [ExcelFunction(Name="STATS.MEDIAN", Description="Median of a numeric array")] public static object UDF_STAT_MED([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Median(V(d)));
        [ExcelFunction(Name="STATS.VARP", Description="Population variance (divides by n)")] public static object UDF_STAT_VP([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.VarianceP(V(d)));
        [ExcelFunction(Name="STATS.VAR", Description="Sample variance (divides by n-1)")] public static object UDF_STAT_VAR([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Variance(V(d)));
        [ExcelFunction(Name="STATS.STDEVP", Description="Population standard deviation (divides by n)")] public static object UDF_STAT_STDP([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.StdevP(V(d)));
        [ExcelFunction(Name="STATS.STDEV", Description="Sample standard deviation (divides by n-1)")] public static object UDF_STAT_STD([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Stdev(V(d)));
        [ExcelFunction(Name="STATS.SKEW", Description="Sample skewness of a numeric array")] public static object UDF_STAT_SKEW([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Skewness(V(d)));
        [ExcelFunction(Name="STATS.KURT", Description="Sample excess kurtosis of a numeric array")] public static object UDF_STAT_KURT([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Kurtosis(V(d)));
        [ExcelFunction(Name="STATS.MIN", Description="Minimum value in a numeric array")] public static object UDF_STAT_MIN([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Min(V(d)));
        [ExcelFunction(Name="STATS.MAX", Description="Maximum value in a numeric array")] public static object UDF_STAT_MAX([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Max(V(d)));
        [ExcelFunction(Name="STATS.RANGE", Description="Range (max - min) of a numeric array")] public static object UDF_STAT_RNG([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Range(V(d)));
        [ExcelFunction(Name="STATS.SUM", Description="Sum of a numeric array")] public static object UDF_STAT_SUM([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Sum(V(d)));
        [ExcelFunction(Name="STATS.PRODUCT", Description="Product of a numeric array")] public static object UDF_STAT_PROD([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Product(V(d)));
        [ExcelFunction(Name="STATS.PERCENTILE", Description="Percentile at p (0-100) using R7 quantile definition")] public static object UDF_STAT_PCT([ExcelArgument("data")] object d, [ExcelArgument("p")] object p)=>OutputWrapper.WrapError(()=>(object)StatsCore.Percentile(V(d),InputNormalizer.ToDouble(p)));
        [ExcelFunction(Name="STATS.IQR", Description="Inter-quartile range (Q3 - Q1) using R7 quantile")] public static object UDF_STAT_IQR([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.IQR(V(d)));
        [ExcelFunction(Name="STATS.SUMMARY", Description="Descriptive stats: returns 1x9 [n, mean, stdev, min, q1, median, q3, max, iqr]. R7 quantile (matches Python scipy default).")] public static object UDF_STAT_SUMM([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>StatsCore.Summary(V(d)));
        [ExcelFunction(Name="STATS.COVARP", Description="Population covariance of two arrays (divides by n)")] public static object UDF_STAT_CVP([ExcelArgument("a")] object a, [ExcelArgument("b")] object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.CovarianceP(V(a),V(b)));
        [ExcelFunction(Name="STATS.COVAR", Description="Sample covariance of two arrays (divides by n-1)")] public static object UDF_STAT_CV([ExcelArgument("a")] object a, [ExcelArgument("b")] object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.Covariance(V(a),V(b)));
        [ExcelFunction(Name="STATS.PEARSON", Description="Pearson correlation coefficient (r). Range -1 to 1")] public static object UDF_STAT_PEAR([ExcelArgument("a")] object a, [ExcelArgument("b")] object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.Pearson(V(a),V(b)));
        [ExcelFunction(Name="STATS.SPEARMAN", Description="Spearman rank correlation coefficient. Range -1 to 1")] public static object UDF_STAT_SPR([ExcelArgument("a")] object a, [ExcelArgument("b")] object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.Spearman(V(a),V(b)));
        [ExcelFunction(Name="STATS.TTEST1", Description="One-sample two-tailed t-test p-value vs mu0. p<0.05 = mean differs significantly from mu0")] public static object UDF_STAT_T1([ExcelArgument("data")] object d, [ExcelArgument("mu0")] object mu)=>OutputWrapper.WrapError(()=>(object)StatsCore.TTestOneSample(V(d),InputNormalizer.ToDouble(mu)));
        [ExcelFunction(Name="STATS.TTEST2", Description="Welch two-sample two-tailed t-test p-value (unequal variance). p<0.05 = means differ significantly")] public static object UDF_STAT_T2([ExcelArgument("a")] object a, [ExcelArgument("b")] object b)=>OutputWrapper.WrapError(()=>(object)StatsCore.TTestTwoSample(V(a),V(b)));
        [ExcelFunction(Name="STATS.ZSCORE", Description="Standardize to z-scores: (x - mean) / stdev")] public static object UDF_STAT_ZS([ExcelArgument("data")] object d)=>OutputWrapper.WrapError(()=>StatsCore.ZScore(V(d)));
        [ExcelFunction(Name="STATS.COUNT", Description="Count of numeric elements in an array")] public static object UDF_STAT_CNT([ExcelArgument("x")] object d)=>OutputWrapper.WrapError(()=>V(d).Length);
        [ExcelFunction(Name="STATS.MODE", Description="Most frequent value. Returns NaN if all unique (matches Excel MODE.SNGL)")] public static object UDF_STAT_MODE([ExcelArgument("x")] object d)=>OutputWrapper.WrapError(()=>(object)StatsCore.Mode(V(d)));
        [ExcelFunction(Name="STATS.ABS", Description="Element-wise absolute value")] public static object UDF_STAT_ABS([ExcelArgument("x")] object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,Math.Abs));
        [ExcelFunction(Name="STATS.SQRT", Description="Element-wise square root")] public static object UDF_STAT_SQRT([ExcelArgument("x")] object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,Math.Sqrt));
        [ExcelFunction(Name="STATS.LN", Description="Element-wise natural logarithm")] public static object UDF_STAT_LN([ExcelArgument("x")] object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,Math.Log));
        [ExcelFunction(Name="STATS.LOG10", Description="Element-wise base-10 logarithm")] public static object UDF_STAT_LOG10([ExcelArgument("x")] object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,Math.Log10));
        [ExcelFunction(Name="STATS.EXP", Description="Element-wise exponential (e^x)")] public static object UDF_STAT_EXP([ExcelArgument("x")] object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,Math.Exp));
        [ExcelFunction(Name="STATS.SIGN", Description="Element-wise sign: -1, 0, or 1")] public static object UDF_STAT_SGN([ExcelArgument("x")] object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,StatsCore.Sign));
    }
}
