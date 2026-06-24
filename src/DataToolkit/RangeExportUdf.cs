using System.Linq;
using System.Runtime.InteropServices;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class RangeExportUdf
    {
        private static object[,] D(object d)=>InputNormalizer.NormalizeTo2D(d)!;
        [ExcelFunction(Name="RANGE.TOHTML", Description="Export range to HTML table")] public static object UDF_RANGE_HTML([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object d, [ExcelArgument(Name="has_headers", Description="TRUE if the first row contains column headers")] object h, [ExcelArgument(Name="[css_class]", Description="CSS class name for the HTML table element")] object c=null)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToHtml(D(d),InputNormalizer.ToBool(h),InputNormalizer.ToString(c)));
        [ExcelFunction(Name="RANGE.TOJSON", Description="Export range to JSON string")] public static object UDF_RANGE_JSON([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object d, [ExcelArgument(Name="has_headers", Description="TRUE if the first row contains column headers")] object h, [ExcelArgument(Name="[pretty_print]", Description="TRUE for indented, human-readable output")] object p=null)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToJson(D(d),InputNormalizer.ToBool(h),InputNormalizer.ToBool(p)));
        [ExcelFunction(Name="RANGE.TOMD", Description="Export range to Markdown table")] public static object UDF_RANGE_MD([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object d, [ExcelArgument(Name="has_headers", Description="TRUE if the first row contains column headers")] object h)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToMarkdown(D(d),InputNormalizer.ToBool(h)));
        [ExcelFunction(Name="RANGE.TOCSV", Description="Export range to CSV with custom delimiter. 3rd param: TRUE to quote fields")] public static object UDF_RANGE_CSV([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object d, [ExcelArgument(Name="delimiter", Description="The delimiter character or string")] object dl, [ExcelArgument(Name="[quote_fields]", Description="TRUE to quote all fields; FALSE for minimal quoting")] object q=null)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToCsv(D(d),InputNormalizer.ToString(dl),InputNormalizer.ToBool(q)));
        [ExcelFunction(Name="RANGE.TOCSVTAB", Description="Export range to tab-separated values (TSV)")] public static object UDF_RANGE_TSV([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object d)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToCsv(D(d),"\t",false));
        [ExcelFunction(Name="RANGE.TOCSVSEMI", Description="Export range to semicolon-separated CSV")] public static object UDF_RANGE_SCSV([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object d)=>OutputWrapper.WrapError(()=>RangeExportCore.RangeToCsv(D(d),";",true));
        [ExcelFunction(Name="RANGE.TRANSPOSE", Description="Transpose rows and columns")] public static object UDF_RANGE_TRANS(object d)=>OutputWrapper.WrapError(()=>RangeExportCore.Transpose(D(d)));
        [ExcelFunction(Name="RANGE.SELCOLS", Description="Select specified columns from a range")] public static object UDF_RANGE_SELC([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object d, [ExcelArgument(Name="column_indices", Description="Array of 0-based column indices to select")] object c)=>OutputWrapper.WrapError(()=>RangeExportCore.SelectColumns(D(d),InputNormalizer.NormalizeTo1D(c).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray()));
        [ExcelFunction(Name="RANGE.SELROWS", Description="Select specified rows from a range")] public static object UDF_RANGE_SELR([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object d, [ExcelArgument(Name="row_indices", Description="Array of 0-based row indices to select")] object r)=>OutputWrapper.WrapError(()=>RangeExportCore.SelectRows(D(d),InputNormalizer.NormalizeTo1D(r).Select(x=>(int)InputNormalizer.ToLong(x)).ToArray()));
    }
}
