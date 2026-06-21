import pathlib
base = pathlib.Path(r"D:\Workspace\zgrwo\VBA\DeepSeek\ClaudeCode\ExcelVbaLibraries	ests")

def w(dir, name, content):
    (base / dir / name).write_text(content, encoding="utf-8")
    print(f"OK: {name}")

# ====== ArrayUdfTests.cs ======
w("DataToolkit.Tests", "ArrayUdfTests.cs", """using ExcelVbaLibraries.DataToolkit;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    public class ArrayUdfTests
    {
        private static readonly object[] A3 = { 3, 1, 2 };
        [Fact] public void Sort_asc() { var r = (object[])ArrayUdf.UDF_ARR_SORT(A3, true, "auto"); r.Should().Equal(1, 2, 3); }
        [Fact] public void Sort_desc() { var r = (object[])ArrayUdf.UDF_ARR_SORT(A3, false, "auto"); r.Should().Equal(3, 2, 1); }
        [Fact] public void Sort_null() => ArrayUdf.UDF_ARR_SORT(null!, true, "auto").Should().Be(ExcelError.Value);
        [Fact] public void SortAsc() { var r = (object[])ArrayUdf.UDF_ARR_SORTASC(A3); r.Should().Equal(1, 2, 3); }
        [Fact] public void SortDesc() { var r = (object[])ArrayUdf.UDF_ARR_SORTDESC(A3); r.Should().Equal(3, 2, 1); }
        [Fact] public void SortNum() { var r = (object[])ArrayUdf.UDF_ARR_SORTNUM(new object[] { 10.0, 2, 5 }); r.Should().Equal(2.0, 5, 10.0); }
        [Fact] public void SortText() { var r = (object[])ArrayUdf.UDF_ARR_SORTTEXT(new object[] { "c", "a", "b" }); r.Should().Equal("a", "b", "c"); }
        [Fact] public void Unique() { var r = (object[])ArrayUdf.UDF_ARR_UNIQUE(new object[] { 1, 2, 1, 3, 2 }); r.Should().Equal(1, 2, 3); }
        [Fact] public void Unique_null() => ArrayUdf.UDF_ARR_UNIQUE(null!).Should().Be(ExcelError.Value);
        [Fact] public void IndexOf_found() => ((long)ArrayUdf.UDF_ARR_INDEXOF(new object[] { 1, 2, 3 }, 2)).Should().Be(1);
        [Fact] public void IndexOf_notfound() => ((long)ArrayUdf.UDF_ARR_INDEXOF(new object[] { 1, 2, 3 }, 99)).Should().Be(-1);
        [Fact] public void Slice_mid() { var r = (object[])ArrayUdf.UDF_ARR_SLICE(new object[] { 1, 2, 3, 4, 5 }, 1.0, 3.0); r.Should().Equal(2, 3, 4); }
        [Fact] public void Slice_neg() { var r = (object[])ArrayUdf.UDF_ARR_SLICE(new object[] { 1, 2, 3 }, -1.0, -1.0); r.Should().Equal(3); }
        [Fact] public void Flatten() { var r = (object[])ArrayUdf.UDF_ARR_FLATTEN(new object[,] { { 1, 2 }, { 3, 4 } }); r.Should().Equal(1, 2, 3, 4); }
        [Fact] public void Flatten_null() => ArrayUdf.UDF_ARR_FLATTEN(null!).Should().Be(ExcelError.Value);
        [Fact] public void Filter_eq() { var r = (object[])ArrayUdf.UDF_ARR_FILTER(new object[] { 1, 2, 3 }, 2, "="); r.Should().Equal(2); }
        [Fact] public void Filter_gt() { var r = (object[])ArrayUdf.UDF_ARR_FILTER(new object[] { 1, 2, 3 }, 1, ">"); r.Should().Equal(2, 3); }
        [Fact] public void FilterEq() { var r = (object[])ArrayUdf.UDF_ARR_FEQ(new object[] { 1, 2, 3 }, 2); r.Should().Equal(2); }
        [Fact] public void FilterNe() { var r = (object[])ArrayUdf.UDF_ARR_FNE(new object[] { 1, 2, 3 }, 2); r.Should().Equal(1, 3); }
        [Fact] public void FilterGt() { var r = (object[])ArrayUdf.UDF_ARR_FGT(new object[] { 1, 2, 3 }, 1); r.Should().Equal(2, 3); }
        [Fact] public void FilterLt() { var r = (object[])ArrayUdf.UDF_ARR_FLT(new object[] { 1, 2, 3 }, 3); r.Should().Equal(1, 2); }
        [Fact] public void Concat() { var r = (object[])ArrayUdf.UDF_ARR_CONCAT(new object[] { 1, 2 }, new object[] { 3, 4 }); r.Should().Equal(1, 2, 3, 4); }
        [Fact] public void Reverse() { var r = (object[])ArrayUdf.UDF_ARR_REVERSE(new object[] { 1, 2, 3 }); r.Should().Equal(3, 2, 1); }
        [Fact] public void Count() => ((long)ArrayUdf.UDF_ARR_COUNT(new object[] { 1, 2, 3 })).Should().Be(3);
        [Fact] public void Count_null() => ArrayUdf.UDF_ARR_COUNT(null!).Should().Be(ExcelError.Value);
        [Fact] public void Contains_true() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(new object[] { 1, 2, 3 }, 2)).Should().BeTrue();
        [Fact] public void Contains_false() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(new object[] { 1, 2, 3 }, 99)).Should().BeFalse();
        [Fact] public void ToSet() { var r = (object[])ArrayUdf.UDF_ARR_TOSET(new object[] { 1, 2, 1, 3 }); r.Should().Equal(1, 2, 3); }
        [Fact] public void Fill() { var r = (object[])ArrayUdf.UDF_ARR_FILL("x", 3.0); r.Should().Equal("x", "x", "x"); }
        [Fact] public void Range_basic() { var r = (object[])ArrayUdf.UDF_ARR_RANGE(1.0, 3.0, 1.0); r.Should().Equal(1.0, 2.0, 3.0); }
        [Fact] public void Shuffle_keeps_length() { var r = (object[])ArrayUdf.UDF_ARR_SHUFFLE(new object[] { 1, 2, 3 }); r.Length.Should().Be(3); }
        [Fact] public void Shuffle_null() => ArrayUdf.UDF_ARR_SHUFFLE(null!).Should().Be(ExcelError.Value);
    }
}
""")
