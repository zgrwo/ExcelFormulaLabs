using System;
using System.Collections.Generic;
using ExcelVbaLibraries.DataToolkit;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    public class JsonXmlCoreTests
    {
        [Fact] public void JsonValidate_valid() => JsonXmlCore.JsonValidate("{\"a\":1}").Should().BeTrue();
        [Fact] public void JsonValidate_invalid() => JsonXmlCore.JsonValidate("{bad}").Should().BeFalse();
        [Fact] public void JsonQuery() => JsonXmlCore.JsonQuery("{\"a\":{\"b\":42}}","a.b").Should().Be(42L);
        [Fact] public void JsonPrettify() => JsonXmlCore.JsonPrettify("{\"a\":1}").Should().Contain("\n");
        [Fact] public void XmlValidate_valid() => JsonXmlCore.XmlValidate("<r><a/></r>").Should().BeTrue();
        [Fact] public void XmlValidate_invalid() => JsonXmlCore.XmlValidate("<a><b>").Should().BeFalse();
        [Fact] public void XmlXPath() => JsonXmlCore.XmlXPath("<r><n>1</n><n>2</n></r>","//n").Should().Equal("1","2");
        [Fact] public void JsonParse_object() => JsonXmlCore.JsonParse("{\"a\":1,\"b\":\"hello\"}").Should().BeAssignableTo<Dictionary<string,object>>();
        [Fact] public void JsonParse_array() => JsonXmlCore.JsonParse("[1,2,3]").Should().BeAssignableTo<object[]>();
        [Fact] public void JsonParse_invalid()
        {
            Action act = () => JsonXmlCore.JsonParse("{bad}");
            act.Should().Throw<Exception>();
        }
        [Fact] public void JsonToTable()
        {
            var r = JsonXmlCore.JsonToTable("[{\"x\":1,\"y\":2},{\"x\":3,\"y\":4}]");
            r![0,0].Should().Be("x"); r![1,0].Should().Be(1L); r![2,1].Should().Be(4L);
        }
        [Fact] public void JsonToTable_empty() => JsonXmlCore.JsonToTable("[]").Should().BeNull();
        [Fact] public void JsonToTable_nonArray() => JsonXmlCore.JsonToTable("{\"a\":1}").Should().BeNull();
        [Fact] public void JsonValidate_nullOrEmpty()
        {
            JsonXmlCore.JsonValidate(null!).Should().BeFalse();
            JsonXmlCore.JsonValidate("").Should().BeFalse();
        }
        [Fact] public void XmlValidate_nullOrEmpty()
        {
            JsonXmlCore.XmlValidate(null!).Should().BeFalse();
            JsonXmlCore.XmlValidate("").Should().BeFalse();
        }
        [Fact] public void XmlToTable()
        {
            var r = JsonXmlCore.XmlToTable("<r><n><a>1</a><b>x</b></n><n><a>2</a><b>y</b></n></r>");
            r!.GetLength(0).Should().BeGreaterThan(0);
        }

        // =====================================================================
        // EDGE CASE & INPUT VALIDATION TESTS
        // (systematic coverage — insertion-order, missing keys, nested/mixed data)
        // =====================================================================

        [Fact] public void JsonToTable_key_insertion_order_preserved()
        {
            // Recent fix: List+HashSet preserves key insertion order (was HashSet-only → random order)
            var json = "[{\"z\":3,\"a\":1,\"m\":2},{\"a\":4,\"z\":6,\"m\":5}]";
            var r = JsonXmlCore.JsonToTable(json);
            r![0, 0].Should().Be("z");
            r![0, 1].Should().Be("a");
            r![0, 2].Should().Be("m");
        }

        [Fact] public void JsonToTable_duplicate_keys_across_objects()
        {
            // Same key appearing in multiple objects → only one column, first-seen order
            var json = "[{\"x\":1},{\"x\":2,\"y\":3}]";
            var r = JsonXmlCore.JsonToTable(json);
            r!.GetLength(1).Should().Be(2);  // x,y (2 unique keys)
            r![0, 0].Should().Be("x");
            r![0, 1].Should().Be("y");
        }

        [Fact] public void JsonToTable_missing_key_in_some_objects()
        {
            // Key present in first object but missing in second → ExcelEmpty for missing
            var json = "[{\"a\":1,\"b\":2},{\"a\":3}]";
            var r = JsonXmlCore.JsonToTable(json);
            r!.GetLength(0).Should().Be(3);  // header + 2 rows
            r![2, 1].Should().Be(ExcelVbaLibraries.Foundation.ExcelEmpty.Value);
        }

        [Fact] public void JsonToTable_non_object_elements_skipped()
        {
            // Array with mixed types (string, object) → string elements skipped
            var json = "[\"text\",{\"a\":1},42,{\"a\":2}]";
            var r = JsonXmlCore.JsonToTable(json);
            r!.GetLength(0).Should().Be(3);  // header + 2 object rows
            r![0, 0].Should().Be("a");
        }

        [Fact] public void JsonQuery_missing_property_returns_null()
        {
            JsonXmlCore.JsonQuery("{\"a\":1}", "b").Should().BeNull();
        }

        [Fact] public void JsonQuery_array_index_oob_returns_null()
        {
            JsonXmlCore.JsonQuery("[1,2]", "[5]").Should().BeNull();
        }

        [Fact] public void JsonQuery_array_index()
        {
            var r = JsonXmlCore.JsonQuery("{\"items\":[10,20,30]}", "items[1]");
            r.Should().Be(20L);
        }

        [Fact] public void JsonValidate_whitespace_only()
        {
            // Empty/whitespace is not valid JSON
            JsonXmlCore.JsonValidate("   ").Should().BeFalse();
        }

        [Fact] public void JsonPrettify_preserves_structure()
        {
            // After prettify, the string should still be parseable back
            var pretty = JsonXmlCore.JsonPrettify("[{\"a\":1}]");
            JsonXmlCore.JsonValidate(pretty).Should().BeTrue();
        }

        // Infinity/NaN guard (ElmNumber: 对齐 RangeExportCore.JsonVal，防 IEEE 754 极值进入 Excel)
        [Fact] public void JsonParse_infinity_returns_null()
        {
            var r = JsonXmlCore.JsonParse("1e999");
            r.Should().BeNull();  // Infinity → null
        }
        [Fact] public void JsonParse_negative_infinity_returns_null()
        {
            var r = JsonXmlCore.JsonParse("-1e999");
            r.Should().BeNull();  // -Infinity → null
        }
        [Fact] public void JsonParse_infinity_in_array_returns_null_element()
        {
            var r = JsonXmlCore.JsonParse("[1, 1e999, 3]");
            r.Should().BeAssignableTo<object[]>();
            var arr = (object[])r!;
            arr[0].Should().Be(1L);
            arr[1].Should().BeNull();  // Infinity element → null
            arr[2].Should().Be(3L);
        }

        [Fact] public void XmlToTable_missing_child_element()
        {
            var xml = "<r><n><a>1</a></n><n><a>2</a><b>x</b></n></r>";
            var r = JsonXmlCore.XmlToTable(xml);
            r!.GetLength(0).Should().Be(3);  // header + 2 rows
            r!.GetLength(1).Should().Be(2);  // a, b
        }
    }

    // Python ref: pivot→pandas.pivot_table, groupby→pandas.groupby
    public class PivotCoreTests
    {
        [Fact] public void Pivot_basic()
        {
            var d = new object[,]{{"Name","Month","Val"},{"A","Jan",100},{"A","Feb",200},{"B","Jan",300}};
            var r = PivotCore.Pivot(d,0,1,2); r.GetLength(0).Should().Be(3); r[0,0].Should().Be("Key \\ Pivot");
        }
        [Fact] public void CrossJoin()
        { var a=new object[,]{{1},{2}}; var b=new object[,]{{"x"},{"y"}}; PivotCore.CrossJoin(a,b).GetLength(0).Should().Be(4); }
        [Fact] public void Unpivot()
        {
            var d = new object[,] { { "ID", "Q1", "Q2" }, { "A", 10, 20 }, { "B", 30, 40 } };
            var r = PivotCore.Unpivot(d, new[] { 0 }, new[] { 1, 2 });
            r.GetLength(0).Should().Be(4);  // 2 data rows × 2 value cols (header row skipped)
        }
        [Fact] public void GroupBy_SUM()
        {
            var d = new object[,] { { "Grp", "Val" }, { "A", 100 }, { "A", 200 }, { "B", 300 } };
            var r = PivotCore.GroupBy(d, new[] { 0 }, 1);
            r.GetLength(0).Should().Be(2);
        }
        [Fact] public void GroupBy_COUNT()
        {
            var d = new object[,] { { "Grp", "Val" }, { "A", 100 }, { "A", 200 }, { "B", 300 } };
            var r = PivotCore.GroupBy(d, new[] { 0 }, 1, "COUNT");
            r.GetLength(0).Should().Be(2);
        }
        [Fact] public void CrossJoin_empty()
        {
            var a = new object[0, 1];
            var b = new object[,] { { "x" }, { "y" } };
            PivotCore.CrossJoin(a, b).GetLength(0).Should().Be(0);
        }
        [Fact] public void CrossJoin_exceeds_limit_throws()
        {
            var a = new object[2000, 1];
            var b = new object[500, 1];
            var act = () => PivotCore.CrossJoin(a, b);
            act.Should().Throw<ArgumentException>().WithMessage("*Cross join*");
        }
        [Fact] public void GroupBy_lowercase_agg()
        {
            var d = new object[,] { { "G", "V" }, { "A", 10 }, { "A", 20 }, { "B", 30 } };
            var r = PivotCore.GroupBy(d, new[] { 0 }, 1, "sum");
            r[0, 1].Should().Be(30.0); r[1, 1].Should().Be(30.0);
        }
        [Fact] public void Pivot_deterministic_order()
        {
            var d = new object[,] { { "K", "P", "V" }, { "B", "X", 10 }, { "A", "Y", 20 }, { "C", "X", 30 } };
            var r1 = PivotCore.Pivot(d, 0, 1, 2);
            var r2 = PivotCore.Pivot(d, 0, 1, 2);
            // Same input → same output (insertion order preserved)
            for (int i = 0; i < r1.GetLength(0); i++)
                for (int j = 0; j < r1.GetLength(1); j++)
                    r1[i, j].Should().Be(r2[i, j]);
        }
        [Fact] public void Unpivot_invalid_column_index()
        {
            var d = new object[,] { { "ID", "V1" }, { "A", 10 } };
            var act = () => PivotCore.Unpivot(d, new[] { 0 }, new[] { 5 });
            act.Should().Throw<ArgumentException>();
        }
        [Fact] public void Pivot_unknown_agg_throws()
        {
            var d = new object[,] { { "K", "P", "V" }, { "A", "X", 10 }, { "A", "X", 5 } };
            var act = () => PivotCore.Pivot(d, 0, 1, 2, "UNKNOWN");
            act.Should().Throw<ArgumentException>().WithMessage("*Unknown aggregation*");
        }
        [Fact] public void Pivot_AVG_aggregation()
        {
            var d = new object[,] { { "K", "P", "V" }, { "A", "X", 10 }, { "A", "X", 20 } };
            var r = PivotCore.Pivot(d, 0, 1, 2, "AVG");
            r[1, 1].Should().Be(15.0);  // (10+20)/2
        }
        [Fact] public void Pivot_COUNT_aggregation()
        {
            var d = new object[,] { { "K", "P", "V" }, { "A", "X", 10 }, { "A", "X", 20 }, { "A", "Y", 5 } };
            var r = PivotCore.Pivot(d, 0, 1, 2, "COUNT");
            r[1, 1].Should().Be(2L);  // A,X appears twice
        }
        [Fact] public void Pivot_empty_cells_are_ExcelEmpty()
        {
            var d = new object[,] { { "K", "P", "V" }, { "A", "X", 10 }, { "B", "Y", 20 } };
            var r = PivotCore.Pivot(d, 0, 1, 2);
            r[1, 1].Should().Be(10.0);                          // A,X has data
            r[2, 1].Should().Be(ExcelVbaLibraries.Foundation.ExcelEmpty.Value); // B,X no data → empty
        }

        // =====================================================================
        // EDGE CASE TESTS — PIVOT
        // =====================================================================

        [Fact] public void Pivot_MAX_aggregation()
        {
            var d = new object[,] { { "K", "P", "V" }, { "A", "X", 10 }, { "A", "X", 50 } };
            var r = PivotCore.Pivot(d, 0, 1, 2, "MAX");
            r[1, 1].Should().Be(50.0);  // MAX of 10 and 50
        }

        [Fact] public void Pivot_MIN_aggregation()
        {
            var d = new object[,] { { "K", "P", "V" }, { "A", "X", 10 }, { "A", "X", 50 } };
            var r = PivotCore.Pivot(d, 0, 1, 2, "MIN");
            r[1, 1].Should().Be(10.0);  // MIN of 10 and 50
        }

        [Fact] public void Pivot_nan_values_skipped()
        {
            var d = new object[,] { { "K", "P", "V" }, { "A", "X", double.NaN }, { "A", "X", 30 } };
            var r = PivotCore.Pivot(d, 0, 1, 2);
            r[1, 1].Should().Be(30.0);  // NaN skipped, only 30 used
        }

        [Fact] public void Pivot_single_row_data()
        {
            var d = new object[,] { { "K", "P", "V" }, { "A", "X", 42 } };
            var r = PivotCore.Pivot(d, 0, 1, 2);
            r.GetLength(0).Should().Be(2);  // header + 1 key
            r.GetLength(1).Should().Be(2);  // label + 1 pivot
            r[1, 1].Should().Be(42.0);
        }

        [Fact] public void Pivot_keys_and_pivots_preserve_insertion_order()
        {
            var d = new object[,] { { "K", "P", "V" }, { "C", "Z", 1 }, { "B", "Y", 2 }, { "A", "X", 3 } };
            var r = PivotCore.Pivot(d, 0, 1, 2);
            // Keys: C, B, A (insertion order)
            r[1, 0].Should().Be("C");
            r[2, 0].Should().Be("B");
            r[3, 0].Should().Be("A");
            // Pivots: Z, Y, X
            r[0, 1].Should().Be("Z");
            r[0, 2].Should().Be("Y");
            r[0, 3].Should().Be("X");
        }

        // =====================================================================
        // EDGE CASE TESTS — UNPIVOT
        // =====================================================================

        [Fact] public void Unpivot_single_value_column()
        {
            var d = new object[,] { { "ID", "V1" }, { "A", 10 }, { "B", 20 } };
            var r = PivotCore.Unpivot(d, new[] { 0 }, new[] { 1 });
            r.GetLength(0).Should().Be(2);  // 2 data rows × 1 value col (header row skipped)
            r.GetLength(1).Should().Be(3);  // ID + header + value
        }

        [Fact] public void Unpivot_multiple_id_columns()
        {
            var d = new object[,] { { "ID1", "ID2", "Q1", "Q2" }, { "A", "X", 10, 20 }, { "B", "Y", 30, 40 } };
            var r = PivotCore.Unpivot(d, new[] { 0, 1 }, new[] { 2, 3 });
            r.GetLength(0).Should().Be(4);  // 2 data rows × 2 value cols (header row skipped)
            r.GetLength(1).Should().Be(4);  // ID1 + ID2 + header + value
        }

        [Fact] public void Unpivot_zero_id_columns()
        {
            var d = new object[,] { { "V1", "V2" }, { 10, 20 }, { 30, 40 } };
            var r = PivotCore.Unpivot(d, new int[0], new[] { 0, 1 });
            r.GetLength(0).Should().Be(4);  // 2 data rows × 2 value cols (header row skipped)
        }

        [Fact] public void Unpivot_no_headers()
        {
            // hasHeaders=false: data starts at row 0, column names auto-generated
            var d = new object[,] { { 10, 20 }, { 30, 40 } };
            var r = PivotCore.Unpivot(d, new int[0], new[] { 0, 1 }, hasHeaders: false);
            r.GetLength(0).Should().Be(4);  // 2 data rows × 2 value cols
            r[0, 0].Should().Be("Var1");   // auto-generated name for col 0 (1-based)
            r[1, 0].Should().Be("Var2");   // auto-generated name for col 1
        }

        // =====================================================================
        // EDGE CASE TESTS — GROUPBY
        // =====================================================================

        [Fact] public void GroupBy_AVG_aggregation()
        {
            var d = new object[,] { { "G", "V" }, { "A", 10 }, { "A", 20 }, { "B", 30 } };
            var r = PivotCore.GroupBy(d, new[] { 0 }, 1, "AVG");
            r.GetLength(0).Should().Be(2);
            r[0, 1].Should().Be(15.0);  // (10+20)/2
            r[1, 1].Should().Be(30.0);  // 30/1
        }

        [Fact] public void GroupBy_MAX_aggregation()
        {
            var d = new object[,] { { "G", "V" }, { "A", 10 }, { "A", 50 }, { "B", 30 } };
            var r = PivotCore.GroupBy(d, new[] { 0 }, 1, "MAX");
            r[0, 1].Should().Be(50.0);  // MAX of 10, 50
            r[1, 1].Should().Be(30.0);  // only 30
        }

        [Fact] public void GroupBy_MIN_aggregation()
        {
            var d = new object[,] { { "G", "V" }, { "A", 10 }, { "A", 50 }, { "B", 30 } };
            var r = PivotCore.GroupBy(d, new[] { 0 }, 1, "MIN");
            r[0, 1].Should().Be(10.0);  // MIN of 10, 50
            r[1, 1].Should().Be(30.0);
        }

        [Fact] public void GroupBy_unknown_agg_throws()
        {
            var d = new object[,] { { "G", "V" }, { "A", 10 }, { "A", 20 } };
            var act = () => PivotCore.GroupBy(d, new[] { 0 }, 1, "UNKNOWN");
            act.Should().Throw<ArgumentException>().WithMessage("*Unknown aggregation*");
        }

        [Fact] public void GroupBy_multiple_group_columns()
        {
            var d = new object[,] { { "G1", "G2", "V" }, { "A", "X", 10 }, { "A", "X", 20 }, { "A", "Y", 5 } };
            var r = PivotCore.GroupBy(d, new[] { 0, 1 }, 2);
            r.GetLength(0).Should().Be(2);  // A|X and A|Y
            r.GetLength(1).Should().Be(3);  // G1, G2, SUM(V)
        }

        [Fact] public void GroupBy_nan_values_skipped()
        {
            var d = new object[,] { { "G", "V" }, { "A", double.NaN }, { "A", 20 }, { "A", 30 } };
            var r = PivotCore.GroupBy(d, new[] { 0 }, 1);
            r[0, 1].Should().Be(50.0);  // NaN skipped, 20+30
        }

        [Fact] public void GroupBy_lowercase_avg()
        {
            var d = new object[,] { { "G", "V" }, { "A", 10 }, { "A", 30 } };
            var r = PivotCore.GroupBy(d, new[] { 0 }, 1, "avg");
            r[0, 1].Should().Be(20.0);  // (10+30)/2
        }

        [Fact] public void CrossJoin_single_row_each()
        {
            var a = new object[,] { { 1 } };
            var b = new object[,] { { "x" } };
            var r = PivotCore.CrossJoin(a, b);
            r.GetLength(0).Should().Be(1);  // 1×1
            r.GetLength(1).Should().Be(2);  // col from a + col from b
        }
    }
}
