using ExcelVbaLibraries.DataToolkit;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    public class DictSetUdfTests
    {
        // DICT.FREQUENCY
        [Fact] public void Frequency_basic() { var r=(object[,])DictSetUdf.UDF_DICT_FREQ(new object[]{"a","b","a","c","a"}); r.GetLength(0).Should().Be(3); r[0,0].Should().Be("a"); r[0,1].Should().Be(3L); }
        [Fact] public void Frequency_null() { var r=(object[,])DictSetUdf.UDF_DICT_FREQ(null!); r.GetLength(0).Should().Be(0); }

        // DICT.INTERSECT
        [Fact] public void Intersect_basic() { var r=(object[])DictSetUdf.UDF_DICT_INTER(new object[]{1,2,3},new object[]{2,3,4}); r.Should().Equal(2,3); }
        [Fact] public void Intersect_null_both() { var r=(object[])DictSetUdf.UDF_DICT_INTER(null!,null!); r.Should().BeEmpty(); }

        // DICT.UNION
        [Fact] public void Union_basic() { var r=(object[])DictSetUdf.UDF_DICT_UNION(new object[]{1,2},new object[]{2,3}); r.Should().Equal(1,2,3); }
        [Fact] public void Union_null_both() { var r=(object[])DictSetUdf.UDF_DICT_UNION(null!,null!); r.Should().BeEmpty(); }

        // DICT.EXCEPT
        [Fact] public void Except_basic() { var r=(object[])DictSetUdf.UDF_DICT_EXCEPT(new object[]{1,2,3,4},new object[]{2,4}); r.Should().Equal(1,3); }
        [Fact] public void Except_null_both() { var r=(object[])DictSetUdf.UDF_DICT_EXCEPT(null!,null!); r.Should().BeEmpty(); }

        // DICT.DICT
        [Fact] public void Dict_basic() { var r=(object[,])DictSetUdf.UDF_DICT_DICT(new object[]{"k1","k2"},new object[]{10,20}); r[0,0].Should().Be("k1"); r[0,1].Should().Be(10); r[1,0].Should().Be("k2"); r[1,1].Should().Be(20); }
        [Fact] public void Dict_null() { var r=(object[,])DictSetUdf.UDF_DICT_DICT(null!,null!); r.GetLength(0).Should().Be(0); }

        // DICT.COUNT
        [Fact] public void Count_basic() => ((long)DictSetUdf.UDF_DICT_COUNT(new object[,]{{"k1",10},{"k2",20}})).Should().Be(2);
        [Fact] public void Count_null() => DictSetUdf.UDF_DICT_COUNT(null!).Should().Be(ExcelError.Value);

        // DICT.KEYS
        [Fact] public void Keys_basic() { var r=(object[])DictSetUdf.UDF_DICT_KEYS(new object[,]{{"a",1},{"b",2}}); r.Should().Equal("a","b"); }
        [Fact] public void Keys_null() => DictSetUdf.UDF_DICT_KEYS(null!).Should().Be(ExcelError.Value);

        // DICT.VALUES
        [Fact] public void Values_basic() { var r=(object[])DictSetUdf.UDF_DICT_VALS(new object[,]{{"a",1},{"b",2}}); r.Should().Equal(1,2); }
        [Fact] public void Values_null() => DictSetUdf.UDF_DICT_VALS(null!).Should().Be(ExcelError.Value);
    }
}
