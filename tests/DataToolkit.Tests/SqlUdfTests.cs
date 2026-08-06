using ExcelFormulaLabs.DataToolkit;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;
namespace ExcelFormulaLabs.DataToolkit.Tests
{
    public class SqlUdfTests
    {
        private static readonly object[,] Data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 }, { "Bob", 25.0 } };
        private static readonly object[,] Extra = new object[,] { { "Name", "Score" }, { "Alice", 95.0 }, { "Bob", 87.0 } };
        private static readonly object[,] TableB = new object[,] { { "Name", "City" }, { "Alice", "NYC" }, { "Bob", "LA" } };
        private static readonly object[,] TableC = new object[,] { { "Name", "Dept" }, { "Alice", "Eng" }, { "Bob", "Sales" } };
        [Fact] public void Query_select_all() { var r=(object[,])SqlUdf.UDF_SQL_QUERY(Data,"SELECT * FROM data"); r.GetLength(0).Should().Be(3); }
        [Fact] public void Query_invalid_sql() => SqlUdf.UDF_SQL_QUERY(Data,"INVALID SQL").Should().Be(ExcelError.Value);
        [Fact] public void Query_nonexistent_column() => SqlUdf.UDF_SQL_QUERY(Data,"SELECT nonexistent FROM data").Should().Be(ExcelError.Value);
        [Fact] public void Join_basic() { var r=(object[,])SqlUdf.UDF_SQL_JOIN(Data,Extra,"SELECT a.Name, b.Score FROM data a JOIN extra b ON a.Name=b.Name"); r.GetLength(0).Should().Be(3); r[1,0].Should().Be("Alice"); r[1,1].Should().Be(95.0); }
        [Fact] public void Join_invalid_sql() => SqlUdf.UDF_SQL_JOIN(Data,Extra,"BAD JOIN").Should().Be(ExcelError.Value);
        [Fact] public void Query3_basic() { var r=(object[,])SqlUdf.UDF_SQL_QUERY3(Data,TableB,TableC,"SELECT a.Name, b.City, c.Dept FROM data a JOIN b ON a.Name=b.Name JOIN c ON a.Name=c.Name"); r.GetLength(0).Should().Be(3); }
        [Fact] public void Query3_invalid_sql() => SqlUdf.UDF_SQL_QUERY3(Data,TableB,TableC,"INVALID").Should().Be(ExcelError.Value);
        [Fact] public void Type_inference_scans_first_10_rows()
        {
            var d = new object[13, 2];
            d[0, 0] = "Num"; d[0, 1] = "Val";
            for (int i = 1; i <= 10; i++) { d[i, 0] = (double)i; d[i, 1] = (long)(i * 10); }
            d[11, 0] = 11.0; d[11, 1] = 115.5;   // beyond scan window → INTEGER affinity
            d[12, 0] = 12.0; d[12, 1] = (long)120;
            var r = (object[,])SqlUdf.UDF_SQL_QUERY(d, "SELECT * FROM data WHERE Val > 50");
            r.GetLength(0).Should().Be(8);  // header + 7 rows (>50)
        }

        // ── Error / null / edge case guards ──────────────────────────
        [Fact] public void Query_null_data() => SqlUdf.UDF_SQL_QUERY(null!, "SELECT * FROM data").Should().Be(ExcelError.Value);
        [Fact] public void Query_empty_data() => SqlUdf.UDF_SQL_QUERY(new object[0, 0], "SELECT * FROM data").Should().Be(ExcelError.Value);
        [Fact] public void Query_invalid_sql_returns_error() => SqlUdf.UDF_SQL_QUERY(Data, "INVALID SQL").Should().Be(ExcelError.Value);
        [Fact] public void Join_null_data() => SqlUdf.UDF_SQL_JOIN(null!, Extra, "SELECT * FROM data").Should().Be(ExcelError.Value);
        [Fact] public void Join_null_extra() => SqlUdf.UDF_SQL_JOIN(Data, null!, "SELECT * FROM data").Should().Be(ExcelError.Value);
        [Fact] public void Query3_null_table2() => SqlUdf.UDF_SQL_QUERY3(Data, null!, TableC, "SELECT * FROM data").Should().Be(ExcelError.Value);
        [Fact] public void Query3_null_table3() => SqlUdf.UDF_SQL_QUERY3(Data, TableB, null!, "SELECT * FROM data").Should().Be(ExcelError.Value);

        // ── Query variety ────────────────────────────────────────────
        [Fact] public void Query_with_where()
        {
            var r = (object[,])SqlUdf.UDF_SQL_QUERY(Data, "SELECT Name FROM data WHERE Age > 26");
            r.GetLength(0).Should().Be(2); r[1, 0].Should().Be("Alice");
        }
        [Fact] public void Query_group_by()
        {
            var d = new object[,] { { "Dept", "Salary" }, { "Eng", 100.0 }, { "Eng", 200.0 }, { "Sales", 150.0 } };
            var r = (object[,])SqlUdf.UDF_SQL_QUERY(d, "SELECT Dept, SUM(Salary) FROM data GROUP BY Dept");
            r.GetLength(0).Should().Be(3);
        }
        [Fact] public void Query_order_by()
        {
            var r = (object[,])SqlUdf.UDF_SQL_QUERY(Data, "SELECT * FROM data ORDER BY Age DESC");
            r[1, 0].Should().Be("Alice"); r[2, 0].Should().Be("Bob");
        }
        [Fact] public void Join_left_join()
        {
            var d = new object[,] { { "Name", "Age" }, { "Alice", 30.0 }, { "Charlie", 40.0 } };
            var r = (object[,])SqlUdf.UDF_SQL_JOIN(d, Data, "SELECT a.Name, b.Age FROM data a LEFT JOIN extra b ON a.Name = b.Name");
            r.GetLength(0).Should().Be(3);
        }
        [Fact] public void Query_with_nulls()
        {
            var d = new object[,] { { "Name", "Score" }, { "Alice", null! }, { "Bob", 85.0 } };
            var r = (object[,])SqlUdf.UDF_SQL_QUERY(d, "SELECT * FROM data");
            r.GetLength(0).Should().Be(3); // header + 2 data rows preserved
        }
        [Fact] public void Query_duplicate_columns()
        {
            var d = new object[,] { { "X", "X" }, { 1.0, 2.0 } };
            var r = (object[,])SqlUdf.UDF_SQL_QUERY(d, "SELECT * FROM data");
            r.GetLength(1).Should().Be(2);
        }
        [Fact] public void Query_column_name_with_spaces()
        {
            var d = new object[,] { { "First Name", "Last Name" }, { "John", "Doe" } };
            var r = (object[,])SqlUdf.UDF_SQL_QUERY(d, "SELECT * FROM data");
            r.GetLength(0).Should().Be(2);
        }

        // ── Release-review regression guards ────────────────────────────────
        [Fact] public void Query_replace_scalar_function_allowed()
        {
            // SQLite built-in REPLACE(X,Y,Z) must NOT be blocked by the DML blacklist
            var r = (object[,])SqlUdf.UDF_SQL_QUERY(Data, "SELECT REPLACE(Name,'li','LI') FROM data");
            r[1, 0].Should().Be("ALIce");
        }

        [Fact] public void Query_replace_into_rejected() =>
            SqlUdf.UDF_SQL_QUERY(Data, "REPLACE INTO data VALUES ('x', 1)").Should().Be(ExcelError.Value);

        [Fact] public void Query_literal_containing_keyword_rejected()
        {
            // Known trade-off (documented in api-reference): whole-word keywords
            // inside string literals are rejected too — frozen to prevent accidental loosening.
            SqlUdf.UDF_SQL_QUERY(Data, "SELECT * FROM data WHERE Name = 'do not delete'").Should().Be(ExcelError.Value);
        }

        [Fact] public void Query_has_headers_false_generates_columns()
        {
            var noHdr = new object[,] { { "Alice", 30.0 }, { "Bob", 25.0 } };
            var r = (object[,])SqlUdf.UDF_SQL_QUERY(noHdr, "SELECT Col1 FROM data", hh: false);
            r.GetLength(0).Should().Be(3); // generated header + 2 data rows
            r[0, 0].Should().Be("Col1");
            r[1, 0].Should().Be("Alice");
        }
    }
}
