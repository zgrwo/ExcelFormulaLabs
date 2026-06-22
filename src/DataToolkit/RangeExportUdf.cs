using System.Linq;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class RangeExportUdf
    {
        private static object[,] D(object d)=>InputNormalizer.NormalizeTo2D(d)!;
        [ExcelFunction(Name="RANGE.TOHTML", Description="Export range to HTML table")] public static object UDF_RANGE_HTML(object d,object h,object c)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToHtml(D(d),InputNormalizer.ToBool(h),InputNormalizer.ToString(c)));
        [ExcelFunction(Name="RANGE.TOJSON", Description="Export range to JSON string")] public static object UDF_RANGE_JSON(object d,object h,object p)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToJson(D(d),InputNormalizer.ToBool(h),InputNormalizer.ToBool(p)));
        [ExcelFunction(Name="RANGE.TOMD", Description="Export range to Markdown table")] public static object UDF_RANGE_MD(object d,object h)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToMarkdown(D(d),InputNormalizer.ToBool(h)));
        [ExcelFunction(Name="RANGE.TOCSV", Description="Export range to CSV with custom delimiter. 3rd param: TRUE to quote fields")] public static object UDF_RANGE_CSV(object d,object dl,object q)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToCsv(D(d),InputNormalizer.ToString(dl),InputNormalizer.ToBool(q)));
        [ExcelFunction(Name="RANGE.TOCSVTAB", Description="Export range to tab-separated values (TSV)")] public static object UDF_RANGE_TSV(object d)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToCsv(D(d),"\t",false));
        [ExcelFunction(Name="RANGE.TOCSVSEMI", Description="Export range to semicolon-separated CSV")] public static object UDF_RANGE_SCSV(object d)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToCsv(D(d),";",true));
        [ExcelFunction(Name="RANGE.TRANSPOSE", Description="Transpose rows and columns")] public static object UDF_RANGE_TRANS(object d)=>OutputWrapper.WrapError(()=>RangeExportCore.Transpose(D(d)));
        [ExcelFunction(Name="RANGE.SELCOLS", Description="Select specified columns from a range")] public static object UDF_RANGE_SELC(object d,object c)=>OutputWrapper.WrapError(()=>RangeExportCore.SelectColumns(D(d),InputNormalizer.NormalizeTo1D(c).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray()));
        [ExcelFunction(Name="RANGE.SELROWS", Description="Select specified rows from a range")] public static object UDF_RANGE_SELR(object d,object r)=>OutputWrapper.WrapError(()=>RangeExportCore.SelectRows(D(d),InputNormalizer.NormalizeTo1D(r).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray()));
    }
}
