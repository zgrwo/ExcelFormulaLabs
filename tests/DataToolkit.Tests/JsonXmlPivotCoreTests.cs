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
            r.GetLength(0).Should().Be(6);
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
        [Fact] public void Pivot_unknown_agg_defaults_to_min()
        {
            var d = new object[,] { { "K", "P", "V" }, { "A", "X", 10 }, { "A", "X", 5 } };
            var r = PivotCore.Pivot(d, 0, 1, 2, "UNKNOWN");
            r[1, 1].Should().Be(5.0);  // MIN is the fallback
        }
    }
}
