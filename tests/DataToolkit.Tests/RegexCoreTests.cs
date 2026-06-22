using ExcelVbaLibraries.DataToolkit;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    // Python ref: all tests cross-validated with Python `re` module (the reference regex implementation)
    public class RegexCoreTests
    {
        [Fact] public void Test_match() => RegexCore.RegexTest("abc123",@"\d+").Should().BeTrue();   // Python re: re.search
        [Fact] public void Test_nomatch() => RegexCore.RegexTest("abc",@"\d+").Should().BeFalse();   // Python re: None
        [Fact] public void Count() => RegexCore.RegexCount("a1b2c3",@"\d").Should().Be(3);           // Python re: len(findall)
        [Fact] public void Match() => RegexCore.RegexMatch("abc123",@"\d+").Should().Be("123");       // Python re: .group()
        [Fact] public void MatchAll() => RegexCore.RegexMatchAll("a1 b2 c3",@"\d+").Should().Equal("1","2","3"); // Python re: findall
        [Fact] public void Replace() => RegexCore.RegexReplace("abc123","\\d","X").Should().Be("abcXXX");     // Python re: sub
        [Fact] public void Split() => RegexCore.RegexSplit("a,b;c","[,;]").Should().Equal("a","b","c");      // Python re: split
        [Fact] public void Groups() => RegexCore.RegexCaptureGroups("Name: John, Age: 30",@"Name: (\w+), Age: (\d+)").Should().Equal("Name: John, Age: 30","John","30"); // Python re: groups()
        [Fact] public void Escape() => RegexCore.RegexEscape("a.b(c)").Should().Be(@"a\.b\(c\)");             // Python re: escape
    }
}
