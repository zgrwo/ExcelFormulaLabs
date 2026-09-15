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
        public void Type_inference_scans_all_rows()
        {
            // Type inference scans every data row; a late type change reclassifies
            // the whole column (mixed → TEXT) instead of being coerced by the
            // affinity inferred from the old 10-row window.
            var data = new object[13, 2];
            data[0, 0] = "Num"; data[0, 1] = "Val";
            for (int i = 1; i <= 10; i++) { data[i, 0] = (double)i; data[i, 1] = (long)(i * 10); }
            data[11, 0] = 11.0; data[11, 1] = 115.5;  // beyond the old 10-row window
            data[12, 0] = 12.0; data[12, 1] = (long)120;
            var r = SqlCore.SqlQuery(data, "SELECT Val FROM data WHERE Val > 50");
            r!.GetLength(0).Should().Be(8);  // header + 7 rows (>50)
            var all = SqlCore.SqlQuery(data, "SELECT Val FROM data ORDER BY Val");
            all![10, 0].Should().Be(100.0);
            all![11, 0].Should().Be(115.5);  // preserved, not truncated to 115
            all![12, 0].Should().Be(120.0);  // long promoted by REAL column
        }

        [Fact]
        public void Late_text_in_numeric_column_reclassifies_as_text()
        {
            // Regression: text beyond the old 10-row window used to be converted
            // with the inferred numeric DbType (net48 → FormatException → #VALUE!)
            // or silently stored inside a REAL column (net8). Full-table scan
            // reclassifies the column as TEXT for every value.
            var data = new object[12, 2];
            data[0, 0] = "k"; data[0, 1] = "v";
            for (int i = 1; i <= 10; i++) { data[i, 0] = "a" + i; data[i, 1] = (double)i; }
            data[11, 0] = "a11"; data[11, 1] = "oops";
            var r = SqlCore.SqlQuery(data, "SELECT v, typeof(v) AS t FROM data WHERE k='a11'");
            r![1, 0].Should().Be("oops");
            r![1, 1].Should().Be("text");
            var r2 = SqlCore.SqlQuery(data, "SELECT v FROM data WHERE k='a1'");
            r2![1, 0].Should().Be("1");  // canonical invariant text, not REAL 1.0
        }

        [Fact]
        public void Excel_dna_error_values_are_null_and_do_not_pollute_aggregates()
        {
            // Real Excel error cells arrive as ExcelDna.Integration.ExcelError —
            // a different type from Foundation.ExcelError. They must become NULL,
            // never the provider's ToString() garbage ("15"/"ExcelErrorValue").
            var data = new object[,]
            {
                { "Name", "Score" },
                { "Alice", ExcelDna.Integration.ExcelError.ExcelErrorValue },
                { "Bob", 90.0 }
            };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![1, 1].Should().BeNull();
            r![2, 1].Should().Be(90.0);
            var sum = SqlCore.SqlQuery(data, "SELECT SUM(Score) AS s FROM data");
            sum![1, 0].Should().Be(90.0);  // error row contributes nothing
        }

        [Fact]
        public void Bool_and_datetime_values_use_invariant_text_forms()
        {
            // Canonical forms must not depend on the provider (net48 used
            // CurrentCulture / bool.ToString, net8 numeric "1"/"0").
            var data = new object[,]
            {
                { "Flag", "When" },
                { true, new DateTime(2026, 9, 14) },
                { false, new DateTime(2026, 9, 15) }
            };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r![1, 0].Should().Be("TRUE");
            r![2, 0].Should().Be("FALSE");
            r![1, 1].Should().Be("2026-09-14 00:00:00");
            r![2, 1].Should().Be("2026-09-15 00:00:00");
        }

        [Fact]
        public void Select_keyword_needs_no_trailing_whitespace()
        {
            // "SELECT*FROM data" is valid SQLite (native cross-check in audit);
            // the prefix guard must not require whitespace after the keyword.
            var data = new object[,] { { "Name" }, { "Alice" } };
            var r = SqlCore.SqlQuery(data, "SELECT*FROM data");
            r![1, 0].Should().Be("Alice");
        }

        [Fact]
        public void Leading_comments_are_allowed_before_select()
        {
            var data = new object[,] { { "Name" }, { "Alice" } };
            SqlCore.SqlQuery(data, "-- note\nSELECT * FROM data")![1, 0].Should().Be("Alice");
            SqlCore.SqlQuery(data, "/* note */ SELECT * FROM data")![1, 0].Should().Be("Alice");
        }

        // 注释拆分关键字的绕过向量：黑名单必须在注释剥离后的归一化文本上匹配，
        // 且拼接处恢复为独立 token。
        [Fact]
        public void Comment_split_forbidden_keywords_are_rejected()
        {
            var data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 } };
            var cases = new[]
            {
                "REPLACE/**/INTO data VALUES ('X',1)",
                "REPLACE--x\nINTO data VALUES ('X',1)",
                "WITH x AS (SELECT 1) REPLACE/**/INTO data VALUES ('X',1)",
                "INSERT/**/INTO data VALUES ('X',1)",
                "WITH x AS (SELECT 1) DELETE/**/FROM data",
                "UPDATE/**/data SET Age = 0",
            };
            foreach (var sql in cases)
            {
                var act = () => SqlCore.SqlQuery(data, sql);
                act.Should().Throw<ArgumentException>($"comment-split keyword must be rejected: {sql}");
            }
            // The would-be DML must not have executed — table still intact.
            var after = SqlCore.SqlQuery(data, "SELECT * FROM data");
            after!.GetLength(0).Should().Be(2);
        }

        // 注释剥离器不得误伤字符串字面量。
        [Fact]
        public void Comment_stripping_preserves_string_literals()
        {
            var data = new object[,] { { "Name" }, { "Alice" } };
            var r1 = SqlCore.SqlQuery(data, "SELECT '--' AS c");
            r1![1, 0].Should().Be("--");
            var r2 = SqlCore.SqlQuery(data, "SELECT '/*' AS c");
            r2![1, 0].Should().Be("/*");
            var r3 = SqlCore.SqlQuery(data, "SELECT 'it''s -- not a comment' AS c");
            r3![1, 0].Should().Be("it's -- not a comment");
        }

        // 单值 10MB 上限必须覆盖文本型巨值。
        [Theory]
        [InlineData("SELECT hex(randomblob(11000000))")]
        [InlineData("SELECT quote(randomblob(11000000))")]
        [InlineData("SELECT CAST(randomblob(11000000) AS TEXT)")]
        public void Oversized_text_values_are_rejected(string sql)
        {
            var data = new object[,] { { "Name" }, { "Alice" } };
            var act = () => SqlCore.SqlQuery(data, sql);
            act.Should().Throw<ArgumentException>().WithMessage("*10 MB*");
        }

        [Fact]
        public void Non_select_prefixes_are_still_rejected()
        {
            var data = new object[,] { { "Name" }, { "Alice" } };
            var act1 = () => SqlCore.SqlQuery(data, "SELECTED * FROM data");
            act1.Should().Throw<ArgumentException>();
            var act2 = () => SqlCore.SqlQuery(data, "-- comment only");
            act2.Should().Throw<ArgumentException>();
            var act3 = () => SqlCore.SqlQuery(data, "/* unterminated");
            act3.Should().Throw<ArgumentException>();
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
            r![1, 1].Should().BeNull();
            r![2, 1].Should().Be(90.0);
        }

        [Fact]
        public void ExcelEmpty_values_handled_as_null()
        {
            var data = new object[,] { { "Name", "Score" }, { "Alice", ExcelEmpty.Value }, { "Bob", 85.0 } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r!.GetLength(0).Should().Be(3);
            r![1, 1].Should().BeNull();
            r![2, 1].Should().Be(85.0);
        }

        [Fact]
        public void All_null_column_returns_excelempty()
        {
            var data = new object[,] { { "Name", "Extra" }, { "Alice", null! }, { "Bob", null! } };
            var r = SqlCore.SqlQuery(data, "SELECT * FROM data");
            r!.GetLength(0).Should().Be(3);
            r![1, 1].Should().BeNull();
            r![2, 1].Should().BeNull();
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
            r![2, 1].Should().BeNull(); // no match → DBNull → ExcelEmpty
        }

        [Fact]
        public void With_cte_delete_is_rejected()
        {
            var data = new object[,] { { "Name" }, { "Alice" } };
            var act = () => SqlCore.SqlQuery(data, "WITH x AS (SELECT 1) DELETE FROM data");
            act.Should().Throw<ArgumentException>(); // data-modifying CTE bypass closed
        }

        [Fact]
        public void With_cte_update_is_rejected()
        {
            var data = new object[,] { { "Name" }, { "Alice" } };
            var act = () => SqlCore.SqlQuery(data, "WITH x AS (SELECT 1) UPDATE data SET Name='x'");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void With_cte_select_is_allowed()
        {
            var data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 } };
            var r = SqlCore.SqlQuery(data, "WITH t AS (SELECT * FROM data) SELECT Name FROM t");
            r!.GetLength(0).Should().Be(2); // header + 1 row
            r[1, 0].Should().Be("Alice");
        }

        [Fact]
        public void SqlQuery_without_headers_generates_column_names()
        {
            // hasHeaders=false: row 0 is data, columns get generated Col1..ColN names
            var data = new object[,] { { "Alice", 30.0 }, { "Bob", 25.0 } };
            var r = SqlCore.SqlQuery(data, "SELECT Col1, Col2 FROM data", hasHeaders: false);
            r!.GetLength(0).Should().Be(3); // generated header + 2 data rows
            r[0, 0].Should().Be("Col1");
            r[0, 1].Should().Be("Col2");
            r[1, 0].Should().Be("Alice");
            r[2, 1].Should().Be(25.0);
        }

    [Fact] public void Recursive_cte_blocked()
    {
        // 无限递归 CTE 原可通过全部过滤（无 RECURSIVE 关键字拦截）→ 输出无界 → OOM。
        var data = new object[,] { { "x" }, { 1.0 } };
        var act = () => SqlCore.SqlQuery(data,
            "WITH RECURSIVE x(n) AS (SELECT 1 UNION ALL SELECT n+1 FROM x) SELECT count(*) FROM x", null, true);
        act.Should().Throw<ArgumentException>().WithMessage("*RECURSIVE*");
    }

    [Fact] public void Cross_join_row_limit_guard()
    {
        // 1000×1000 交叉连接 = 1e6 行 > 200k 上限 → 显式拒绝（不可捕获 OOM 防线的行数部分）。
        var a = new object[1000, 1];
        a[0, 0] = "v";
        for (int i = 1; i < 1000; i++) a[i, 0] = (double)i;
        var act = () => SqlCore.SqlQuery(a, "SELECT * FROM data a, data b", null, true);
        act.Should().Throw<ArgumentException>().WithMessage("*more than*");
    }

    // R3-16：LOAD_EXTENSION 纵深防御（EnableExtensions 未开启时执行必败，但黑名单给出
    // 结构性拒绝；同时避免未来默认开启扩展后成为静默利用面）。
    [Fact]
    public void Load_extension_blocked()
    {
        var data = new object[,] { { "x" }, { 1.0 } };
        var act = () => SqlCore.SqlQuery(data, "SELECT load_extension('evil')");
        act.Should().Throw<ArgumentException>().WithMessage("*forbidden*");
    }

    [Fact]
    public void Pre_first_row_runaway_hits_wall_clock_budget()
    {
        // R1-3：500³ 交叉连接聚合在首行前完成计算——旧实现循环内秒表永不触发
        // （实测 1000³ 阻塞 10.4s 后成功返回）。看门狗 300ms 必须中止并给出预算消息。
        var a = new object[501, 1];
        a[0, 0] = "v";
        for (int i = 1; i <= 500; i++) a[i, 0] = (double)i;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var act = () => SqlCore.SqlQuery(a, "SELECT count(*) FROM data a, data b, data c", null, true, 300);
        act.Should().Throw<ArgumentException>().WithMessage("*budget*");
        sw.ElapsedMilliseconds.Should().BeLessThan(3000);
    }
}
}
