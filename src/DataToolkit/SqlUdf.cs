using System.Collections.Generic;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class SqlUdf
    {
        [ExcelFunction(Name = "SQL.QUERY", Description = "Execute SQL on a range. Table = 'data'. First row = headers. Example: SELECT Name, SUM(Amount) FROM data GROUP BY Name")]
        public static object UDF_SQL_QUERY(object data, object sql)
            => OutputWrapper.WrapError(() => SqlCore.SqlQuery(InputNormalizer.NormalizeTo2D(data)!, InputNormalizer.ToString(sql))!);

        [ExcelFunction(Name = "SQL.JOIN", Description = "SQL with 2 tables 'data' + 'extra'. Example: SELECT a.*, b.x FROM data a JOIN extra b ON a.ID=b.ID")]
        public static object UDF_SQL_JOIN(object data, object extra, object sql)
            => OutputWrapper.WrapError(() => { var t = new Dictionary<string, object[,]> { ["extra"] = InputNormalizer.NormalizeTo2D(extra)! }; return SqlCore.SqlQuery(InputNormalizer.NormalizeTo2D(data)!, InputNormalizer.ToString(sql), t)!; });

        [ExcelFunction(Name = "SQL.QUERY3", Description = "SQL with 3 tables 'data','b','c'.")]
        public static object UDF_SQL_QUERY3(object a, object b, object c, object sql)
            => OutputWrapper.WrapError(() => { var t = new Dictionary<string, object[,]> { ["b"] = InputNormalizer.NormalizeTo2D(b)!, ["c"] = InputNormalizer.NormalizeTo2D(c)! }; return SqlCore.SqlQuery(InputNormalizer.NormalizeTo2D(a)!, InputNormalizer.ToString(sql), t)!; });
    }
}
