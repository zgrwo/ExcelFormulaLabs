using System.Linq;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class RangeExportUdf
    {
        private static object[,] D(object d)=>InputNormalizer.NormalizeTo2D(d)!;
        [ExcelFunction(Name="RANGE.TOHTML")] public static object UDF_RANGE_HTML(object d,object h,object c)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToHtml(D(d),InputNormalizer.ToBool(h),InputNormalizer.ToString(c)));
        [ExcelFunction(Name="RANGE.TOJSON")] public static object UDF_RANGE_JSON(object d,object h,object p)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToJson(D(d),InputNormalizer.ToBool(h),InputNormalizer.ToBool(p)));
        [ExcelFunction(Name="RANGE.TOMD")] public static object UDF_RANGE_MD(object d,object h)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToMarkdown(D(d),InputNormalizer.ToBool(h)));
        [ExcelFunction(Name="RANGE.TOCSV")] public static object UDF_RANGE_CSV(object d,object dl,object q)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToCsv(D(d),InputNormalizer.ToString(dl),InputNormalizer.ToBool(q)));
        [ExcelFunction(Name="RANGE.TOCSVTAB")] public static object UDF_RANGE_TSV(object d)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToCsv(D(d),"\t",false));
        [ExcelFunction(Name="RANGE.TOCSVSEMI")] public static object UDF_RANGE_SCSV(object d)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToCsv(D(d),";",true));
        [ExcelFunction(Name="RANGE.TRANSPOSE")] public static object UDF_RANGE_TRANS(object d)=>OutputWrapper.WrapError(()=>RangeExportCore.Transpose(D(d)));
        [ExcelFunction(Name="RANGE.SELCOLS")] public static object UDF_RANGE_SELC(object d,object c)=>OutputWrapper.WrapError(()=>RangeExportCore.SelectColumns(D(d),c is int[] ci?ci:InputNormalizer.NormalizeTo1D(c).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray()));
        [ExcelFunction(Name="RANGE.SELROWS")] public static object UDF_RANGE_SELR(object d,object r)=>OutputWrapper.WrapError(()=>RangeExportCore.SelectRows(D(d),r is int[] ri?ri:InputNormalizer.NormalizeTo1D(r).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray()));
    }
}
