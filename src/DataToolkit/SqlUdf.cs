using System.Collections.Generic;
using ExcelDna.Integration;
using FormulaLabs.Foundation;

namespace FormulaLabs.DataToolkit
{
    public static class SqlUdf
    {
        [ExcelFunction(Name = "SQL.QUERY", Description = "Execute SQL on a range. Table = 'data'. First row = headers. Example: SELECT Name, SUM(Amount) FROM data GROUP BY Name")]
        public static object UDF_SQL_QUERY([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object data, [ExcelArgument(Name="sql_query", Description="A SQL query string; table name is data")] object sql)
            => OutputWrapper.WrapError(() => SqlCore.SqlQuery(InputNormalizer.NormalizeTo2D(data)!, InputNormalizer.ToString(sql))!);

        [ExcelFunction(Name = "SQL.JOIN", Description = "SQL with 2 tables 'data' + 'extra'. Example: SELECT a.*, b.x FROM data a JOIN extra b ON a.ID=b.ID")]
        public static object UDF_SQL_JOIN([ExcelArgument(Name="source_range", Description="The input range or 2D array")] object data, [ExcelArgument(Name="join_table", Description="A second range to join with the data table")] object extra, [ExcelArgument(Name="sql_query", Description="A SQL query string; table name is data")] object sql)
            => OutputWrapper.WrapError(() => { var t = new Dictionary<string, object[,]> { ["extra"] = InputNormalizer.NormalizeTo2D(extra)! }; return SqlCore.SqlQuery(InputNormalizer.NormalizeTo2D(data)!, InputNormalizer.ToString(sql), t)!; });

        [ExcelFunction(Name = "SQL.QUERY3", Description = "SQL with 3 tables 'data','b','c'.")]
        public static object UDF_SQL_QUERY3([ExcelArgument(Name="table1", Description="First data range for multi-table SQL")] object a, [ExcelArgument(Name="table2", Description="Second data range for multi-table SQL")] object b, [ExcelArgument(Name="table3", Description="Third data range for multi-table SQL")] object c, [ExcelArgument(Name="sql_query", Description="A SQL query string; table name is data")] object sql)
            => OutputWrapper.WrapError(() => { var t = new Dictionary<string, object[,]> { ["b"] = InputNormalizer.NormalizeTo2D(b)!, ["c"] = InputNormalizer.NormalizeTo2D(c)! }; return SqlCore.SqlQuery(InputNormalizer.NormalizeTo2D(a)!, InputNormalizer.ToString(sql), t)!; });
    }
}
