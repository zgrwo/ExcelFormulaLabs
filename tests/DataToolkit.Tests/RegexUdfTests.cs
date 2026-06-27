using ExcelFormulaLabs.DataToolkit;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.DataToolkit.Tests
{
    public class RegexUdfTests
    {
        // REGEX.TEST
        [Fact] public void Test_match() => ((bool)RegexUdf.UDF_RX_TEST("hello world","hello",true)).Should().BeTrue();
        [Fact] public void Test_no_match() => ((bool)RegexUdf.UDF_RX_TEST("hello","\\d+",true)).Should().BeFalse();
        [Fact] public void Test_invalid_pattern() => RegexUdf.UDF_RX_TEST("hello","[",true).Should().Be(ExcelError.Value);

        // REGEX.COUNT
        [Fact] public void Count_matches() => ((long)RegexUdf.UDF_RX_COUNT("one two three","\\w+",true)).Should().Be(3);
        [Fact] public void Count_no_match() => ((long)RegexUdf.UDF_RX_COUNT("abc","\\d+",true)).Should().Be(0);
        [Fact] public void Count_invalid_pattern() => RegexUdf.UDF_RX_COUNT("abc","[",true).Should().Be(ExcelError.Value);

        // REGEX.MATCH
        [Fact] public void Match_first() => ((string)RegexUdf.UDF_RX_MATCH("abc123def","\\d+",true)).Should().Be("123");
        [Fact] public void Match_2nd() => ((string)RegexUdf.UDF_RX_MATCH("a1b2c3","\\d+",true,2)).Should().Be("2");
        [Fact] public void Match_last() => ((string)RegexUdf.UDF_RX_MATCH("a1b2c3","\\d+",true,-1)).Should().Be("3");
        [Fact] public void Match_2nd_last() => ((string)RegexUdf.UDF_RX_MATCH("a1b2c3","\\d+",true,-2)).Should().Be("2");
        [Fact] public void Match_n_exceeds() => ((string)RegexUdf.UDF_RX_MATCH("a1b2","\\d+",true,5)).Should().Be("");
        [Fact] public void Match_no_match() => ((string)RegexUdf.UDF_RX_MATCH("abc","\\d+",true)).Should().Be("");
        [Fact] public void Match_invalid_pattern() => RegexUdf.UDF_RX_MATCH("abc","[",true).Should().Be(ExcelError.Value);

        // REGEX.MATCHALL
        [Fact] public void MatchAll_multiple() { var r=(object[])RegexUdf.UDF_RX_MALL("a1b2c3","\\d",true); r.Should().Equal("1","2","3"); }
        [Fact] public void MatchAll_no_match() { var r=(object[])RegexUdf.UDF_RX_MALL("abc","\\d+",true); r.Should().BeEmpty(); }
        [Fact] public void MatchAll_invalid_pattern() => RegexUdf.UDF_RX_MALL("abc","[",true).Should().Be(ExcelError.Value);

        // REGEX.REPLACE
        [Fact] public void Replace_basic() => ((string)RegexUdf.UDF_RX_REPL("abc123def","\\d+","#",true)).Should().Be("abc#def");
        [Fact] public void Replace_1st() => ((string)RegexUdf.UDF_RX_REPL("a1b2c3","\\d","X",true,1)).Should().Be("aXb2c3");
        [Fact] public void Replace_last() => ((string)RegexUdf.UDF_RX_REPL("a1b2c3","\\d","X",true,-1)).Should().Be("a1b2cX");
        [Fact] public void Replace_n_exceeds() => ((string)RegexUdf.UDF_RX_REPL("a1b2","\\d","X",true,5)).Should().Be("a1b2");
        [Fact] public void Replace_no_match() => ((string)RegexUdf.UDF_RX_REPL("abc","\\d+","#",true)).Should().Be("abc");
        [Fact] public void Replace_invalid_pattern() => RegexUdf.UDF_RX_REPL("abc","[","#",true).Should().Be(ExcelError.Value);

        // REGEX.SPLIT
        [Fact] public void Split_basic() { var r=(object[])RegexUdf.UDF_RX_SPLIT("a,b,c",",",true); r.Should().Equal("a","b","c"); }
        [Fact] public void Split_n1() { var r=(object[])RegexUdf.UDF_RX_SPLIT("a,b,c,d",",",true,1); r.Should().Equal("a","b,c,d"); }
        [Fact] public void Split_n2() { var r=(object[])RegexUdf.UDF_RX_SPLIT("a,b,c,d",",",true,2); r.Should().Equal("a","b","c,d"); }
        [Fact] public void Split_no_separator() { var r=(object[])RegexUdf.UDF_RX_SPLIT("abc",",",true); r.Should().Equal("abc"); }
        [Fact] public void Split_invalid_pattern() => RegexUdf.UDF_RX_SPLIT("abc","[",true).Should().Be(ExcelError.Value);

        // REGEX.GROUPS (returns object[2,n]: row0=group names, row1=values)
        [Fact] public void Groups_basic() {
            var r = (object[,])RegexUdf.UDF_RX_GRP("John:30",@"(\w+):(\d+)",true);
            r.GetLength(0).Should().Be(2);
            r.GetLength(1).Should().Be(3);
            r[1,0].Should().Be("John:30");
            r[1,1].Should().Be("John");
            r[1,2].Should().Be("30");
        }
        [Fact] public void Groups_no_match() {
            var r = (object[,])RegexUdf.UDF_RX_GRP("abc",@"(\d+)",true);
            r.GetLength(0).Should().Be(0);
            r.GetLength(1).Should().Be(0);
        }
        [Fact] public void Groups_invalid_pattern() => RegexUdf.UDF_RX_GRP("abc","[",true).Should().Be(ExcelError.Value);

        // REGEX.ESCAPE
        [Fact] public void Escape_basic() => ((string)RegexUdf.UDF_RX_ESC("hello.world")).Should().Be(@"hello\.world");
        [Fact] public void Escape_empty() => ((string)RegexUdf.UDF_RX_ESC("")).Should().Be("");
        [Fact] public void Escape_null() => RegexUdf.UDF_RX_ESC(null!).Should().BeNull();

        // REGEX.ISMATCH
        [Fact] public void IsMatch_true() => ((bool)RegexUdf.UDF_RX_ISMATCH("hello","hello")).Should().BeTrue();
        [Fact] public void IsMatch_false() => ((bool)RegexUdf.UDF_RX_ISMATCH("hello","\\d+")).Should().BeFalse();
        [Fact] public void IsMatch_invalid_pattern() => RegexUdf.UDF_RX_ISMATCH("hello","[").Should().Be(ExcelError.Value);

        // ── Array input tests (MapOver element-wise) ─────────────────
        [Fact] public void Test_array() { var r=(object[])RegexUdf.UDF_RX_TEST(new object[]{"ab","12","cd"},"\\d+",true); r.Should().Equal(false,true,false); }
        [Fact] public void Count_array() { var r=(object[])RegexUdf.UDF_RX_COUNT(new object[]{"a1","b2c3","d"},"\\d+",true); r.Should().Equal(1L,2L,0L); }
        [Fact] public void Match_array() { var r=(object[])RegexUdf.UDF_RX_MATCH(new object[]{"a1","b2","c3"},"\\d+",true); r.Should().Equal("1","2","3"); }
        [Fact] public void Replace_array() { var r=(object[])RegexUdf.UDF_RX_REPL(new object[]{"a1","b2","c3"},"\\d","X",true); r.Should().Equal("aX","bX","cX"); }
        [Fact] public void Escape_array() { var r=(object[])RegexUdf.UDF_RX_ESC(new object[]{"a.b","c.d"}); r.Should().Equal(@"a\.b",@"c\.d"); }
        [Fact] public void IsMatch_array() { var r=(object[])RegexUdf.UDF_RX_ISMATCH(new object[]{"a1","bc","d2"},"\\d+"); r.Should().Equal(true,false,true); }

        // MapOverMulti L100-101: NormalizeTo1D(null)→empty → returns ExcelEmpty.Value
        [Fact] public void Test_null() => RegexUdf.UDF_RX_TEST(null!,"\\d+",true).Should().Be(ExcelEmpty.Value);
        [Fact] public void Count_null() => RegexUdf.UDF_RX_COUNT(null!,"\\d+",true).Should().Be(ExcelEmpty.Value);
        [Fact] public void Match_null() => RegexUdf.UDF_RX_MATCH(null!,"\\d+",true).Should().Be(ExcelEmpty.Value);
        // MapOver (single-arg): MapSingleCell(null)→returns null
        [Fact] public void Replace_null() => RegexUdf.UDF_RX_REPL(null!,"\\d","X",true).Should().BeNull();
        // Direct Core call: ToString(null)→"" → regex groups on "" → empty array
        [Fact] public void Groups_null() { var r = (object[,])RegexUdf.UDF_RX_GRP(null!,"(\\d+)",true); r.GetLength(0).Should().Be(0); }
        [Fact] public void MatchAll_null() { var r = (string[])RegexUdf.UDF_RX_MALL(null!,"\\d+",true); r.Should().BeEmpty(); }
        [Fact] public void Split_null() { var r = (object[])RegexUdf.UDF_RX_SPLIT(null!,",",true); r.Should().HaveCount(1); }

        // Pattern length guard (MaxPatternLength = 10000)
        [Fact] public void Test_too_long_pattern_returns_error()
        {
            var longPattern = new string('x', 10001);
            RegexUdf.UDF_RX_TEST("test", longPattern, true).Should().Be(ExcelError.Value);
        }
    }
}
