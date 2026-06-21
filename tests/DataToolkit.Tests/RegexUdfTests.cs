using ExcelVbaLibraries.DataToolkit;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
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
        [Fact] public void Match_no_match() => ((string)RegexUdf.UDF_RX_MATCH("abc","\\d+",true)).Should().Be("");
        [Fact] public void Match_invalid_pattern() => RegexUdf.UDF_RX_MATCH("abc","[",true).Should().Be(ExcelError.Value);

        // REGEX.MATCHALL
        [Fact] public void MatchAll_multiple() { var r=(object[])RegexUdf.UDF_RX_MALL("a1b2c3","\\d",true); r.Should().Equal("1","2","3"); }
        [Fact] public void MatchAll_no_match() { var r=(object[])RegexUdf.UDF_RX_MALL("abc","\\d+",true); r.Should().BeEmpty(); }
        [Fact] public void MatchAll_invalid_pattern() => RegexUdf.UDF_RX_MALL("abc","[",true).Should().Be(ExcelError.Value);

        // REGEX.REPLACE
        [Fact] public void Replace_basic() => ((string)RegexUdf.UDF_RX_REPL("abc123def","\\d+","#",true)).Should().Be("abc#def");
        [Fact] public void Replace_no_match() => ((string)RegexUdf.UDF_RX_REPL("abc","\\d+","#",true)).Should().Be("abc");
        [Fact] public void Replace_invalid_pattern() => RegexUdf.UDF_RX_REPL("abc","[","#",true).Should().Be(ExcelError.Value);

        // REGEX.SPLIT
        [Fact] public void Split_basic() { var r=(object[])RegexUdf.UDF_RX_SPLIT("a,b,c",",",true); r.Should().Equal("a","b","c"); }
        [Fact] public void Split_no_separator() { var r=(object[])RegexUdf.UDF_RX_SPLIT("abc",",",true); r.Should().Equal("abc"); }
        [Fact] public void Split_invalid_pattern() => RegexUdf.UDF_RX_SPLIT("abc","[",true).Should().Be(ExcelError.Value);

        // REGEX.GROUPS
        [Fact] public void Groups_basic() { var r=(object[])RegexUdf.UDF_RX_GRP("John:30","(\\w+):(\\d+)",true); r.Should().Equal("John:30","John","30"); }
        [Fact] public void Groups_no_match() { var r=(object[])RegexUdf.UDF_RX_GRP("abc","(\\d+)",true); r.Should().BeEmpty(); }
        [Fact] public void Groups_invalid_pattern() => RegexUdf.UDF_RX_GRP("abc","[",true).Should().Be(ExcelError.Value);

        // REGEX.ESCAPE
        [Fact] public void Escape_basic() => ((string)RegexUdf.UDF_RX_ESC("hello.world")).Should().Be(@"hello\.world");
        [Fact] public void Escape_empty() => ((string)RegexUdf.UDF_RX_ESC("")).Should().Be("");
        [Fact] public void Escape_null() => RegexUdf.UDF_RX_ESC(null!).Should().BeNull();

        // REGEX.ISMATCH
        [Fact] public void IsMatch_true() => ((bool)RegexUdf.UDF_RX_ISMATCH("hello","hello")).Should().BeTrue();
        [Fact] public void IsMatch_false() => ((bool)RegexUdf.UDF_RX_ISMATCH("hello","\\d+")).Should().BeFalse();
        [Fact] public void IsMatch_invalid_pattern() => RegexUdf.UDF_RX_ISMATCH("hello","[").Should().Be(ExcelError.Value);
    }
}
