using System;
using ExcelVbaLibraries.DataToolkit;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    /// <summary>
    /// Core tests for RangeExportCore — Range-to-format exports (HTML, JSON, Markdown, CSV)
    /// and array transformations (Transpose, SelectColumns, SelectRows).
    /// </summary>
    public class RangeExportCoreTests
    {
        private static readonly object[,] BasicData = new object[,]
        {
            { "Name", "Age" },
            { "Alice", 30 },
            { "Bob", 25 }
        };

        // ─────────────────────────────────────────────────────────────
        // RangeToHtml
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void ToHtml_basic_with_header()
        {
            var html = RangeExportCore.RangeToHtml(BasicData);
            html.Should().Contain("<table>");
            html.Should().Contain("<th>Name</th>");
            html.Should().Contain("<th>Age</th>");
            html.Should().Contain("<td>Alice</td>");
            html.Should().Contain("<td>30</td>");
            html.Should().Contain("</table>");
        }

        [Fact]
        public void ToHtml_no_header()
        {
            var html = RangeExportCore.RangeToHtml(BasicData, hasHeader: false);
            html.Should().NotContain("<th>");
            html.Should().Contain("<td>Name</td>");     // first row is data, not header
            html.Should().Contain("<td>Alice</td>");
        }

        [Fact]
        public void ToHtml_with_custom_class()
        {
            var html = RangeExportCore.RangeToHtml(BasicData, tableClass: "my-table");
            html.Should().Contain("class=\"my-table\"");
            html.Should().Contain("<table class=\"my-table\">");
        }

        [Fact]
        public void ToHtml_null_class_no_class_attribute()
        {
            var html = RangeExportCore.RangeToHtml(BasicData, tableClass: null);
            html.Should().Contain("<table>");
            html.Should().NotContain("class=");
        }

        [Fact]
        public void ToHtml_html_encoding()
        {
            var data = new object[,] { { "Tag", "Value" }, { "<script>", "A & B" } };
            var html = RangeExportCore.RangeToHtml(data);
            html.Should().Contain("&lt;script&gt;");
            html.Should().Contain("A &amp; B");
            html.Should().NotContain("<script>");       // must be encoded
        }

        [Fact]
        public void ToHtml_empty_data()
        {
            var data = new object[0, 2];
            var html = RangeExportCore.RangeToHtml(data);
            html.Should().Contain("<table>");
            html.Should().Contain("</table>");
            html.Should().NotContain("<tr>");           // no rows
        }

        [Fact]
        public void ToHtml_single_cell()
        {
            var data = new object[,] { { "Hello" } };
            var html = RangeExportCore.RangeToHtml(data);
            html.Should().Contain("<th>Hello</th>");    // header row with 1 cell
        }

        [Fact]
        public void ToHtml_multi_column_alignment()
        {
            var data = new object[3, 2];
            data[0, 0] = "A"; data[0, 1] = "B";
            data[1, 0] = "1"; data[1, 1] = "2";
            data[2, 0] = "3"; data[2, 1] = "4";
            var html = RangeExportCore.RangeToHtml(data);
            html.Should().Contain("<th>A</th>");
            html.Should().Contain("<th>B</th>");
            html.Should().Contain("<td>1</td>");
            html.Should().Contain("<td>2</td>");
        }

        // ─────────────────────────────────────────────────────────────
        // RangeToJson
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void ToJson_basic_with_header()
        {
            var json = RangeExportCore.RangeToJson(BasicData);
            json.Should().Contain("\"Name\"");
            json.Should().Contain("\"Age\"");
            json.Should().Contain("\"Alice\"");
            json.Should().Contain("30");               // int → number, no quotes
            json.Should().StartWith("[");
            json.Should().EndWith("]");
        }

        [Fact]
        public void ToJson_no_header_uses_colN_keys()
        {
            var json = RangeExportCore.RangeToJson(BasicData, hasHeader: false);
            json.Should().Contain("\"col0\"");
            json.Should().Contain("\"col1\"");
            // All 3 rows are data when hasHeader=false, so "Name" IS a data value
            json.Should().Contain("\"Name\"");       // first row cell 0 as data
        }

        [Fact]
        public void ToJson_pretty_print()
        {
            var json = RangeExportCore.RangeToJson(BasicData, pretty: true);
            json.Should().Contain("\n");
            json.Should().Contain("  ");               // indentation
        }

        [Fact]
        public void ToJson_null_value_outputs_null_literal()
        {
            var data = new object[,] { { "Name", "Score" }, { "Alice", null! } };
            var json = RangeExportCore.RangeToJson(data, hasHeader: true);
            json.Should().Contain("null");             // null → "null" in JSON
        }

        [Fact]
        public void ToJson_excelempty_value_outputs_null()
        {
            var data = new object[,] { { "Name", "Score" }, { "Alice", ExcelEmpty.Value } };
            var json = RangeExportCore.RangeToJson(data, hasHeader: true);
            json.Should().Contain("null");             // ExcelEmpty → "null"
        }

        [Fact]
        public void ToJson_bool_values()
        {
            var data = new object[,] { { "Name", "Active" }, { "Alice", true }, { "Bob", false } };
            var json = RangeExportCore.RangeToJson(data);
            json.Should().Contain("true");
            json.Should().Contain("false");
        }

        [Fact]
        public void ToJson_nan_double_outputs_null()
        {
            var data = new object[,] { { "Name", "Value" }, { "A", double.NaN } };
            var json = RangeExportCore.RangeToJson(data);
            json.Should().Contain("null");             // NaN → "null"
        }

        [Fact]
        public void ToJson_string_with_special_chars_encoded()
        {
            var data = new object[,] { { "Text" }, { "line1\nline2" } };
            var json = RangeExportCore.RangeToJson(data);
            json.Should().Contain("\\n");              // newline escaped
        }

        [Fact]
        public void ToJson_multiple_rows()
        {
            var json = RangeExportCore.RangeToJson(BasicData);
            var objectCount = json.Split('{').Length - 1;  // count '{' = number of objects
            objectCount.Should().Be(2);                // "{...},{...}" → 2 objects
        }

        [Fact]
        public void ToJson_empty_data()
        {
            var data = new object[0, 2];
            var json = RangeExportCore.RangeToJson(data);
            json.Should().Be("[]");                    // empty array
        }

        [Fact]
        public void ToJson_header_only_no_data_rows()
        {
            var data = new object[,] { { "Name", "Age" } };
            var json = RangeExportCore.RangeToJson(data);
            json.Should().Be("[]");                    // no data rows → empty array
        }

        [Fact]
        public void ToJson_long_values()
        {
            var data = new object[,] { { "ID" }, { 1234567890123L } };
            var json = RangeExportCore.RangeToJson(data);
            json.Should().Contain("1234567890123");
        }

        // ─────────────────────────────────────────────────────────────
        // RangeToMarkdown
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void ToMarkdown_basic()
        {
            var md = RangeExportCore.RangeToMarkdown(BasicData);
            md.Should().Contain("Name");
            md.Should().Contain("Age");
            md.Should().Contain("---");                // separator row
            md.Should().Contain("Alice");
            md.Should().Contain("30");
        }

        [Fact]
        public void ToMarkdown_no_header()
        {
            var md = RangeExportCore.RangeToMarkdown(BasicData, hasHeader: false);
            md.Should().Contain("Col1");
            md.Should().Contain("Col2");
            md.Should().Contain("---");
            md.Should().Contain("Name");               // first row is treated as data
        }

        [Fact]
        public void ToMarkdown_empty_data()
        {
            var data = new object[0, 2];
            var md = RangeExportCore.RangeToMarkdown(data);
            md.Should().Be("");                        // rows == 0 → empty string
        }

        [Fact]
        public void ToMarkdown_separator_row_present()
        {
            var md = RangeExportCore.RangeToMarkdown(BasicData);
            var lines = md.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            lines.Length.Should().BeGreaterOrEqualTo(3); // header + sep + at least 1 data row
            lines[1].Should().Contain("---");
        }

        // ─────────────────────────────────────────────────────────────
        // RangeToCsv
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void ToCsv_basic_comma()
        {
            var csv = RangeExportCore.RangeToCsv(BasicData);
            csv.Should().Contain(",");
            csv.Should().Contain("Name,Age");
            csv.Should().Contain("Alice,30");
            csv.Should().Contain("Bob,25");
        }

        [Fact]
        public void ToCsv_custom_delimiter_tab()
        {
            var csv = RangeExportCore.RangeToCsv(BasicData, delim: "\t");
            csv.Should().Contain("\t");
            csv.Should().NotContain(",");              // no commas with tab delimiter
        }

        [Fact]
        public void ToCsv_semicolon_delimiter()
        {
            var csv = RangeExportCore.RangeToCsv(BasicData, delim: ";");
            csv.Should().Contain("Name;Age");
            csv.Should().Contain("Alice;30");
        }

        [Fact]
        public void ToCsv_no_quoting()
        {
            var data = new object[,] { { "Name", "Desc" }, { "A", "hello" } };
            var csv = RangeExportCore.RangeToCsv(data, quote: false);
            csv.Should().Be("Name,Desc\r\nA,hello\r\n");
        }

        [Fact]
        public void ToCsv_value_with_comma_is_quoted()
        {
            var data = new object[,] { { "Name", "Note" }, { "Alice", "Hello, World" } };
            var csv = RangeExportCore.RangeToCsv(data);
            csv.Should().Contain("\"Hello, World\"");  // comma in value → quoted
        }

        [Fact]
        public void ToCsv_value_with_quote_is_escaped_and_quoted()
        {
            var data = new object[,] { { "Text" }, { "He said \"Hi\"" } };
            var csv = RangeExportCore.RangeToCsv(data);
            csv.Should().Contain("\"He said \"\"Hi\"\"\""); // quotes doubled inside
        }

        [Fact]
        public void ToCsv_value_with_newline_is_quoted()
        {
            var data = new object[,] { { "Desc" }, { "Line1\nLine2" } };
            var csv = RangeExportCore.RangeToCsv(data);
            csv.Should().Contain("\"");                // must be quoted
            csv.Should().Contain("Line1\nLine2");
        }

        [Fact]
        public void ToCsv_value_with_delimiter_is_quoted()
        {
            var data = new object[,] { { "Name" }, { "A,B" } };
            var csv = RangeExportCore.RangeToCsv(data);
            csv.Should().Contain("\"A,B\"");
        }

        [Fact]
        public void ToCsv_empty_data()
        {
            var data = new object[0, 2];
            var csv = RangeExportCore.RangeToCsv(data);
            csv.Should().Be("");                       // no rows → empty
        }

        [Fact]
        public void ToCsv_number_values_not_quoted()
        {
            var csv = RangeExportCore.RangeToCsv(BasicData);
            csv.Should().Contain("30");                // number → no quotes
            csv.Should().NotContain("\"30\"");         // not quoted
        }

        // ─────────────────────────────────────────────────────────────
        // Transpose
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void Transpose_2x3_to_3x2()
        {
            var data = new object[,] { { "A", "B", "C" }, { 1, 2, 3 } };
            var t = RangeExportCore.Transpose(data);
            t.GetLength(0).Should().Be(3);             // 3 rows (was 3 cols)
            t.GetLength(1).Should().Be(2);             // 2 cols (was 2 rows)
            t[0, 0].Should().Be("A");
            t[0, 1].Should().Be(1);
            t[1, 0].Should().Be("B");
            t[1, 1].Should().Be(2);
            t[2, 0].Should().Be("C");
            t[2, 1].Should().Be(3);
        }

        [Fact]
        public void Transpose_1xN_to_Nx1()
        {
            var data = new object[,] { { "X", "Y", "Z" } };
            var t = RangeExportCore.Transpose(data);
            t.GetLength(0).Should().Be(3);
            t.GetLength(1).Should().Be(1);
            t[0, 0].Should().Be("X");
            t[1, 0].Should().Be("Y");
            t[2, 0].Should().Be("Z");
        }

        [Fact]
        public void Transpose_Nx1_to_1xN()
        {
            var data = new object[,] { { "A" }, { "B" }, { "C" } };
            var t = RangeExportCore.Transpose(data);
            t.GetLength(0).Should().Be(1);
            t.GetLength(1).Should().Be(3);
            t[0, 0].Should().Be("A");
            t[0, 1].Should().Be("B");
            t[0, 2].Should().Be("C");
        }

        [Fact]
        public void Transpose_empty_array()
        {
            var data = new object[0, 3];
            var t = RangeExportCore.Transpose(data);
            t.GetLength(0).Should().Be(3);             // 3 cols → 3 rows
            t.GetLength(1).Should().Be(0);             // 0 rows → 0 cols
        }

        // ─────────────────────────────────────────────────────────────
        // SelectColumns
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void SelectColumns_single_column()
        {
            var data = new object[,] { { "A", "B", "C" }, { 1, 2, 3 }, { 4, 5, 6 } };
            var r = RangeExportCore.SelectColumns(data, new[] { 0 });
            r.GetLength(0).Should().Be(3);
            r.GetLength(1).Should().Be(1);
            r[0, 0].Should().Be("A");
            r[1, 0].Should().Be(1);
            r[2, 0].Should().Be(4);
        }

        [Fact]
        public void SelectColumns_multiple_columns()
        {
            var data = new object[,] { { "A", "B", "C" }, { 1, 2, 3 }, { 4, 5, 6 } };
            var r = RangeExportCore.SelectColumns(data, new[] { 0, 2 });
            r.GetLength(1).Should().Be(2);
            r[0, 0].Should().Be("A");
            r[0, 1].Should().Be("C");
            r[1, 0].Should().Be(1);
            r[1, 1].Should().Be(3);
        }

        [Fact]
        public void SelectColumns_reverse_order()
        {
            var data = new object[,] { { "A", "B", "C" }, { 1, 2, 3 } };
            var r = RangeExportCore.SelectColumns(data, new[] { 2, 1, 0 });
            r[0, 0].Should().Be("C");
            r[0, 1].Should().Be("B");
            r[0, 2].Should().Be("A");
        }

        [Fact]
        public void SelectColumns_empty_index_array()
        {
            var data = new object[,] { { "A", "B" }, { 1, 2 } };
            var r = RangeExportCore.SelectColumns(data, new int[0]);
            r.GetLength(1).Should().Be(0);             // no columns selected
            r.GetLength(0).Should().Be(2);             // all rows preserved
        }

        [Fact]
        public void SelectColumns_out_of_range_index_throws()
        {
            var data = new object[,] { { "A", "B" }, { 1, 2 } };
            var act = () => RangeExportCore.SelectColumns(data, new[] { 5 });
            act.Should().Throw<IndexOutOfRangeException>();
        }

        // ─────────────────────────────────────────────────────────────
        // SelectRows
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void SelectRows_single_row()
        {
            var data = new object[,] { { "A", "B" }, { 1, 2 }, { 3, 4 } };
            var r = RangeExportCore.SelectRows(data, new[] { 1 });
            r.GetLength(0).Should().Be(1);
            r.GetLength(1).Should().Be(2);
            r[0, 0].Should().Be(1);
            r[0, 1].Should().Be(2);
        }

        [Fact]
        public void SelectRows_multiple_rows()
        {
            var data = new object[,] { { "A", "B" }, { 1, 2 }, { 3, 4 }, { 5, 6 } };
            var r = RangeExportCore.SelectRows(data, new[] { 1, 3 });
            r.GetLength(0).Should().Be(2);
            r[0, 0].Should().Be(1);
            r[1, 0].Should().Be(5);
        }

        [Fact]
        public void SelectRows_include_header()
        {
            var data = new object[,] { { "A", "B" }, { 1, 2 }, { 3, 4 } };
            var r = RangeExportCore.SelectRows(data, new[] { 0, 2 });
            r.GetLength(0).Should().Be(2);
            r[0, 0].Should().Be("A");                 // header
            r[1, 0].Should().Be(3);                   // row index 2
        }

        [Fact]
        public void SelectRows_empty_index_array()
        {
            var data = new object[,] { { "A", "B" }, { 1, 2 } };
            var r = RangeExportCore.SelectRows(data, new int[0]);
            r.GetLength(0).Should().Be(0);             // no rows selected
            r.GetLength(1).Should().Be(2);             // columns preserved
        }

        [Fact]
        public void SelectRows_out_of_range_index_throws()
        {
            var data = new object[,] { { "A", "B" }, { 1, 2 } };
            var act = () => RangeExportCore.SelectRows(data, new[] { 10 });
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}
