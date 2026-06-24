using System.Linq;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class PivotUdf
    {
        [ExcelFunction(Name="PIVOT.PIVOT", Description="Create a pivot table. key_col=row labels, pivot_col=column labels, value_col=values, agg=sum/avg/count/min/max")] public static object UDF_PIVOT_PIVOT([ExcelArgument("source_range")] object d, [ExcelArgument("row_field")] object kc, [ExcelArgument("col_field")] object pc, [ExcelArgument("value_field")] object vc, [ExcelArgument("aggregation")] object agg)=>OutputWrapper.WrapError(()=>PivotCore.Pivot(InputNormalizer.NormalizeTo2D(d)!,(int)InputNormalizer.ToLong(kc),(int)InputNormalizer.ToLong(pc),(int)InputNormalizer.ToLong(vc),InputNormalizer.ToString(agg)));
        [ExcelFunction(Name="PIVOT.UNPIVOT", Description="Unpivot: convert wide columns to key-value rows")] public static object UDF_PIVOT_UNPIVOT([ExcelArgument("source_range")] object d, [ExcelArgument("id_fields")] object ic, [ExcelArgument("value_fields")] object vc)=>OutputWrapper.WrapError(()=>{var ids=InputNormalizer.NormalizeTo1D(ic).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray();var vs=InputNormalizer.NormalizeTo1D(vc).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray();return PivotCore.Unpivot(InputNormalizer.NormalizeTo2D(d)!,ids,vs);});
        [ExcelFunction(Name="PIVOT.GROUPBY", Description="Group by columns and aggregate. agg=sum/avg/count/min/max")] public static object UDF_PIVOT_GROUPBY([ExcelArgument("source_range")] object d, [ExcelArgument("group_fields")] object gc, [ExcelArgument("agg_column")] object ac, [ExcelArgument("aggregation")] object agg)=>OutputWrapper.WrapError(()=>{var g=InputNormalizer.NormalizeTo1D(gc).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray();return PivotCore.GroupBy(InputNormalizer.NormalizeTo2D(d)!,g,(int)InputNormalizer.ToLong(ac),InputNormalizer.ToString(agg));});
        [ExcelFunction(Name="PIVOT.CROSSJOIN", Description="Cross join (Cartesian product) of two tables")] public static object UDF_PIVOT_CROSSJOIN([ExcelArgument("table1")] object a, [ExcelArgument("table2")] object b)=>OutputWrapper.WrapError(()=>PivotCore.CrossJoin(InputNormalizer.NormalizeTo2D(a)!,InputNormalizer.NormalizeTo2D(b)!));
    }
}
