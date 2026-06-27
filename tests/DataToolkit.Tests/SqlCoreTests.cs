using System;
using System.Collections.Generic;
using ExcelFormulaLabs.DataToolkit;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.DataToolkit.Tests
{
    /// <summary>
    /// Core tests for SqlCore — in-memory SQLite query engine for Excel ranges.
    /// Covers: SqlQuery (basic SELECT, JOIN, type inference, null/ExcelEmpty handling,
    /// column name sanitization, error handling, extra tables).
    /// </summary>
    public class SqlCoreTests
    {
        // ─────────────────────────────────────────────────────────────
        // BASIC QUERY FUNCTIONALITY
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void Select_all_columns_and_rows()
        {
            var data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 }, { "Bob", 25.0 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r!.GetLength(0).Should().Be(3);        // header + 2 data rows
            r!.GetLength(1).Should().Be(2);        // Name, Age
            r![0, 0].Should().Be("Name");          // header row
            r![0, 1].Should().Be("Age");
            r![1, 0].Should().Be("Alice");
            r![1, 1].Should().Be(30.0);            // REAL column → double
            r![2, 0].Should().Be("Bob");
            r![2, 1].Should().Be(25.0);
        }

        [Fact]
        public void Select_with_where_clause()
        {
            var data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 }, { "Bob", 25.0 } };
            var r = SqlCore.SqlQuery(data, "SELECT Name FROM data WHERE Age > 28");
            r!.GetLength(0).Should().Be(2);        // header + 1 row
            r![1, 0].Should().Be("Alice");
        }

        [Fact]
        public void Select_with_aggregate_count()
        {
            var data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 }, { "Bob", 25.0 } };
            var r = SqlCore.SqlQuery(data, "SELECT COUNT(*) AS cnt FROM data");
            r!.GetLength(0).Should().Be(2);        // header + 1 aggregate row
            r![0, 0].Should().Be("cnt");
            r![1, 0].Should().Be(2L);              // COUNT returns long/int64
        }

        [Fact]
        public void Select_with_order_by()
        {
            var data = new object[,] { { "Name", "Score" }, { "Bob", 90.0 }, { "Alice", 95.0 }, { "Charlie", 85.0 } };
            var r = SqlCore.SqlQuery(data, "SELECT Name FROM data ORDER BY Name");
            r![1, 0].Should().Be("Alice");
            r![2, 0].Should().Be("Bob");
            r![3, 0].Should().Be("Charlie");
        }

        [Fact]
        public void Select_with_group_by_and_sum()
        {
            var data = new object[,] { { "Dept", "Salary" }, { "Eng", 100.0 }, { "Eng", 150.0 }, { "Sales", 200.0 } };
            var r = SqlCore.SqlQuery(data, "SELECT Dept, SUM(Salary) AS Total FROM data GROUP BY Dept ORDER BY Dept");
            r!.GetLength(0).Should().Be(3);        // header + Eng, Sales
            r![1, 0].Should().Be("Eng");
            r![1, 1].Should().Be(250.0);
            r![2, 0].Should().Be("Sales");
            r![2, 1].Should().Be(200.0);
        }

        // ─────────────────────────────────────────────────────────────
        // TYPE INFERENCE (INTEGER / REAL / TEXT)
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void Type_inference_integer_column()
        {
            var data = new object[,] { { "ID", "Count" }, { 1, 100 }, { 2, 200 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![1, 0].Should().Be(1L);              // INTEGER → long
            r![1, 1].Should().Be(100L);
        }

        [Fact]
        public void Type_inference_real_column()
        {
            var data = new object[,] { { "Name", "Value" }, { "A", 10.5 }, { "B", 20 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![1, 1].Should().Be(10.5);            // REAL → double
            r![2, 1].Should().Be(20.0);            // int 20 stored in REAL column → double
        }

        [Fact]
        public void Type_inference_real_overrides_integer()
        {
            var data = new object[,] { { "K", "V" }, { "A", 10 }, { "B", 20.5 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![1, 1].Should().Be(10.0);            // int 10 stored in REAL → double
            r![2, 1].Should().Be(20.5);
        }

        [Fact]
        public void Type_inference_mixed_types_fallback_to_text()
        {
            var data = new object[,] { { "Name", "Value" }, { "A", 10 }, { "B", "hello" } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![1, 1].Should().Be("10");            // int stored in TEXT → string
            r![2, 1].Should().Be("hello");
        }

        [Fact]
        public void Type_inference_beyond_first_10_rows()
        {
            var data = new object[13, 2];
            data[0, 0] = "Num"; data[0, 1] = "Val";
            for (int i = 1; i <= 10; i++) { data[i, 0] = (double)i; data[i, 1] = (double)(i * 10); }
            data[11, 0] = 11.0; data[11, 1] = "text_value";
            data[12, 0] = 12.0; data[12, 1] = 120.0;
            var r = SqlCore.SqlQuery(data, "SELECT Val FROM data WHERE Val = 'text_value'");
            r!.GetLength(0).Should().Be(2);        // header + 1 matching row
            r![1, 0].Should().Be("text_value");    // stored as TEXT, returned as string
        }

        // ─────────────────────────────────────────────────────────────
        // NULL / DBNull / ExcelEmpty HANDLING
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void Null_values_become_excelempty_in_query_results()
        {
            var data = new object[,] { { "Name", "Score" }, { "Alice", null! }, { "Bob", 90.0 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r!.GetLength(0).Should().Be(3);        // header + 2 rows
            r![0, 0].Should().Be("Name");
            r![1, 1].Should().Be(ExcelEmpty.Value);
            r![2, 1].Should().Be(90.0);
        }

        [Fact]
        public void ExcelEmpty_values_handled_as_null()
        {
            var data = new object[,] { { "Name", "Score" }, { "Alice", ExcelEmpty.Value }, { "Bob", 85.0 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r!.GetLength(0).Should().Be(3);
            r![1, 1].Should().Be(ExcelEmpty.Value);
            r![2, 1].Should().Be(85.0);
        }

        [Fact]
        public void All_null_column_returns_excelempty()
        {
            var data = new object[,] { { "Name", "Extra" }, { "Alice", null! }, { "Bob", null! } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r!.GetLength(0).Should().Be(3);
            r![1, 1].Should().Be(ExcelEmpty.Value);
            r![2, 1].Should().Be(ExcelEmpty.Value);
        }

        // ─────────────────────────────────────────────────────────────
        // COLUMN NAME SANITIZATION
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void Empty_column_name_becomes_colN()
        {
            var data = new object[,] { { "", "Valid" }, { 1, 2 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![0, 0].Should().Be("Col1");
            r![0, 1].Should().Be("Valid");
        }

        [Fact]
        public void Whitespace_column_name_becomes_colN()
        {
            var data = new object[,] { { "   ", "X" }, { 1, 2 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![0, 0].Should().Be("Col1");
        }

        [Fact]
        public void Special_characters_in_column_name_replaced_with_underscore()
        {
            var data = new object[,] { { "Na-me", "V@l" }, { 1, 2 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![0, 0].Should().Be("Na_me");
            r![0, 1].Should().Be("V_l");
        }

        [Fact]
        public void Leading_digit_column_name_prefixed_with_underscore()
        {
            var data = new object[,] { { "123ABC", "X" }, { 1, 2 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![0, 0].Should().Be("_123ABC");
        }

        [Fact]
        public void Spaces_in_column_names_replaced_with_underscore()
        {
            var data = new object[,] { { "First Name", "Last Name" }, { "John", "Doe" } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![0, 0].Should().Be("First_Name");
            r![0, 1].Should().Be("Last_Name");
        }

        [Fact]
        public void Duplicate_column_names_de_duplicated()
        {
            var data = new object[,] { { "Val", "Val" }, { 10, 20 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r!.GetLength(1).Should().Be(2);
            r![0, 0].Should().Be("Val");
            r![0, 1].Should().Be("Val_2");
        }

        [Fact]
        public void Triple_duplicate_column_names()
        {
            var data = new object[,] { { "X", "X", "X" }, { 1, 2, 3 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![0, 0].Should().Be("X");
            r![0, 1].Should().Be("X_2");
            r![0, 2].Should().Be("X_3");
        }

        [Fact]
        public void Case_insensitive_duplicate_detection()
        {
            var data = new object[,] { { "Val", "val" }, { 10, 20 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![0, 0].Should().Be("Val");
            r![0, 1].Should().Be("val_2");
        }

        // ─────────────────────────────────────────────────────────────
        // MULTI-TABLE QUERIES (JOIN)
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void Join_with_one_extra_table()
        {
            var data = new object[,] { { "ID", "Name" }, { 1, "Alice" }, { 2, "Bob" } };
            var extra = new Dictionary<string, object[,]>
            {
                ["extra"] = new object[,] { { "ID", "Score" }, { 1, 95.0 }, { 2, 87.0 } }
            };
            var r = SqlCore.SqlQuery(data, "SELECT a.Name, b.Score FROM data a JOIN extra b ON a.ID = b.ID", extra);
            r!.GetLength(0).Should().Be(3);        // header + 2 rows
            r![0, 0].Should().Be("Name");
            r![0, 1].Should().Be("Score");
            r![1, 0].Should().Be("Alice");
            r![1, 1].Should().Be(95.0);
        }

        [Fact]
        public void Join_with_two_extra_tables()
        {
            var data = new object[,] { { "ID", "Name" }, { 1, "Alice" }, { 2, "Bob" } };
            var extras = new Dictionary<string, object[,]>
            {
                ["b"] = new object[,] { { "ID", "City" }, { 1, "NYC" }, { 2, "LA" } },
                ["c"] = new object[,] { { "ID", "Dept" }, { 1, "Eng" }, { 2, "Sales" } }
            };
            var r = SqlCore.SqlQuery(data, "SELECT a.Name, b.City, c.Dept FROM data a JOIN b ON a.ID = b.ID JOIN c ON a.ID = c.ID", extras);
            r!.GetLength(0).Should().Be(3);
            r![1, 0].Should().Be("Alice");
            r![1, 1].Should().Be("NYC");
            r![1, 2].Should().Be("Eng");
        }

        [Fact]
        public void Extra_table_with_special_column_names()
        {
            var data = new object[,] { { "ID", "Name" }, { 1, "Alice" } };
            var extras = new Dictionary<string, object[,]>
            {
                ["t2"] = new object[,] { { "col 1", "col-2" }, { 10, 20 } }
            };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data JOIN t2 ON data.ID = t2.col_1", extras);
            r!.GetLength(1).Should().Be(4);        // ID, Name, col_1, col_2
        }

        // ─────────────────────────────────────────────────────────────
        // EDGE CASES & ERROR HANDLING
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void Header_only_range_returns_header_no_data()
        {
            var data = new object[,] { { "Name", "Age" } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r!.GetLength(0).Should().Be(1);        // just the header
            r![0, 0].Should().Be("Name");
            r![0, 1].Should().Be("Age");
        }

        [Fact]
        public void Empty_range_throws()
        {
            var data = new object[0, 2];
            var act = () => SqlCore.SqlQuery(data, "SELECT * FROM data");
            act.Should().Throw<Exception>();        // SQLite: no such table
        }

        [Fact]
        public void Invalid_sql_throws()
        {
            var data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 } };
            var act = () => SqlCore.SqlQuery(data, "INVALID SQL STATEMENT");
            act.Should().Throw<Exception>();        // SQLite syntax error
        }

        [Fact]
        public void Select_nonexistent_table_throws()
        {
            var data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 } };
            var act = () => SqlCore.SqlQuery(data, "SELECT * FROM nonexistent");
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Select_nonexistent_column_throws()
        {
            var data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 } };
            var act = () => SqlCore.SqlQuery(data, "SELECT NonExistent FROM data");
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Large_dataset_query()
        {
            var data = new object[101, 2];
            data[0, 0] = "ID"; data[0, 1] = "Value";
            for (int i = 1; i <= 100; i++) { data[i, 0] = i; data[i, 1] = (double)(i * 10); }
            var r = SqlCore.SqlQuery(data, "SELECT COUNT(*) AS cnt FROM data");
            r![1, 0].Should().Be(100L);
        }

        [Fact]
        public void Numeric_query_with_calculated_column()
        {
            var data = new object[,] { { "Price", "Qty" }, { 10.0, 3 }, { 15.0, 2 } };
            var r = SqlCore.SqlQuery(data, "SELECT Price, Qty, Price * Qty AS Total FROM data");
            r!.GetLength(1).Should().Be(3);        // Price, Qty, Total
            r![1, 2].Should().Be(30.0);            // 10 * 3
            r![2, 2].Should().Be(30.0);            // 15 * 2
        }

        [Fact]
        public void Query_returns_excelempty_for_missing_left_join()
        {
            var data = new object[,] { { "ID", "Name" }, { 1, "Alice" }, { 3, "Charlie" } };
            var extra = new Dictionary<string, object[,]>
            {
                ["extra"] = new object[,] { { "ID", "Score" }, { 1, 95.0 }, { 2, 87.0 } }
            };
            var r = SqlCore.SqlQuery(data, "SELECT a.Name, b.Score FROM data a LEFT JOIN extra b ON a.ID = b.ID", extra);
            r!.GetLength(0).Should().Be(3);        // header + 2 rows
            r![1, 0].Should().Be("Alice");
            r![1, 1].Should().Be(95.0);            // matched
            r![2, 0].Should().Be("Charlie");
            r![2, 1].Should().Be(ExcelEmpty.Value); // no match → DBNull → ExcelEmpty
        }
    }
}
