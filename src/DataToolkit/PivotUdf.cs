using System.Linq;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class PivotUdf
    {
        [ExcelFunction(Name="PIVOT.PIVOT", Description="Create a pivot table. key_col=row labels, pivot_col=column labels, value_col=values, agg=sum/avg/count/min/max")] public static object UDF_PIVOT_PIVOT(object d,object kc,object pc,object vc,object agg)=>OutputWrapper.WrapError(()=>PivotCore.Pivot(InputNormalizer.NormalizeTo2D(d)!,(int)InputNormalizer.ToLong(kc),(int)InputNormalizer.ToLong(pc),(int)InputNormalizer.ToLong(vc),InputNormalizer.ToString(agg)));
        [ExcelFunction(Name="PIVOT.UNPIVOT", Description="Unpivot: convert wide columns to key-value rows")] public static object UDF_PIVOT_UNPIVOT(object d,object ic,object vc)=>OutputWrapper.WrapError(()=>{var ids=InputNormalizer.NormalizeTo1D(ic).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray();var vs=InputNormalizer.NormalizeTo1D(vc).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray();return PivotCore.Unpivot(InputNormalizer.NormalizeTo2D(d)!,ids,vs);});
        [ExcelFunction(Name="PIVOT.GROUPBY", Description="Group by columns and aggregate. agg=sum/avg/count/min/max")] public static object UDF_PIVOT_GROUPBY(object d,object gc,object ac,object agg)=>OutputWrapper.WrapError(()=>{var g=InputNormalizer.NormalizeTo1D(gc).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray();return PivotCore.GroupBy(InputNormalizer.NormalizeTo2D(d)!,g,(int)InputNormalizer.ToLong(ac),InputNormalizer.ToString(agg));});
        [ExcelFunction(Name="PIVOT.CROSSJOIN", Description="Cross join (Cartesian product) of two tables")] public static object UDF_PIVOT_CROSSJOIN(object a,object b)=>OutputWrapper.WrapError(()=>PivotCore.CrossJoin(InputNormalizer.NormalizeTo2D(a)!,InputNormalizer.NormalizeTo2D(b)!));
    }
}
