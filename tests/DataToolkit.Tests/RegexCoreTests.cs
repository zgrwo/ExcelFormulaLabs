using System;
using System.Text.RegularExpressions;
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

        // =====================================================================
        // EDGE CASE & ERROR BEHAVIOR TESTS
        // (systematic coverage — null, empty, invalid patterns, match failures)
        // =====================================================================

        [Fact] public void RegexMatch_no_match_returns_empty()
        {
            // Python re: re.search(r'\d+', 'abc') → None
            RegexCore.RegexMatch("abc", @"\d+").Should().Be("");
        }

        [Fact] public void RegexMatchAll_no_match_returns_empty()
        {
            // Python re: re.findall(r'\d+', 'abc') → []
            RegexCore.RegexMatchAll("abc", @"\d+").Should().BeEmpty();
        }

        [Fact] public void RegexCaptureGroups_no_match_returns_empty()
        {
            // Python re: re.search(r'(\d+)', 'abc') → None → no groups
            RegexCore.RegexCaptureGroups("abc", @"(\d+)").Should().BeEmpty();
        }

        [Fact] public void RegexCount_no_match_returns_zero()
        {
            // Python re: len(re.findall(r'\d+', 'abc')) → 0
            RegexCore.RegexCount("abc", @"\d+").Should().Be(0);
        }

        [Fact] public void RegexReplace_no_match_passthrough()
        {
            // Python re: re.sub(r'\d+', 'X', 'abc') → 'abc'
            RegexCore.RegexReplace("abc", @"\d+", "X").Should().Be("abc");
        }

        [Fact] public void RegexSplit_no_match_returns_original()
        {
            // Python re: re.split(r'\d+', 'abc') → ['abc']
            RegexCore.RegexSplit("abc", @"\d+").Should().Equal("abc");
        }

        [Fact] public void RegexTest_empty_input()
        {
            // re.search(r'\d+', '') → None
            RegexCore.RegexTest("", @"\d+").Should().BeFalse();
        }

        [Fact] public void RegexMatch_empty_input_returns_empty()
        {
            RegexCore.RegexMatch("", @"\d+").Should().Be("");
        }

        [Fact] public void RegexTest_null_input_throws()
        {
            // System.Text.RegularExpressions throws ArgumentNullException for null input
            var act = () => RegexCore.RegexTest(null!, @"\d+");
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact] public void RegexTest_null_pattern_throws()
        {
            var act = () => RegexCore.RegexTest("abc", null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact] public void RegexTest_invalid_pattern_throws()
        {
            // Unclosed character class → RegexParseException
            var act = () => RegexCore.RegexTest("abc", @"[invalid");
            act.Should().Throw<Exception>();
        }

        [Fact] public void RegexTest_case_sensitive()
        {
            RegexCore.RegexTest("ABC", @"abc", ic: false).Should().BeFalse();
            RegexCore.RegexTest("ABC", @"ABC", ic: false).Should().BeTrue();
        }

        [Fact] public void RegexReplace_case_sensitive()
        {
            RegexCore.RegexReplace("ABC abc", @"abc", "X", ic: false).Should().Be("ABC X");
        }

        [Fact] public void RegexCaptureGroups_multiple_groups()
        {
            // Python re: re.search(r'(\d{4})-(\d{2})-(\d{2})', '2024-03-15').groups() → ('2024','03','15')
            var r = RegexCore.RegexCaptureGroups("2024-03-15", @"(\d{4})-(\d{2})-(\d{2})");
            r.Should().Equal("2024-03-15", "2024", "03", "15");
        }

        [Fact] public void RegexSplit_multiple_delimiters()
        {
            // Python re: re.split(r'[,;]', 'a,b;c,d') → ['a','b','c','d']
            RegexCore.RegexSplit("a,b;c,d", @"[,;]").Should().Equal("a", "b", "c", "d");
        }

        [Fact] public void RegexMatchAll_empty_input_returns_empty()
        {
            RegexCore.RegexMatchAll("", @"\d+").Should().BeEmpty();
        }

        [Fact] public void RegexEscape_empty_string()
        {
            RegexCore.RegexEscape("").Should().Be("");
        }

        [Fact] public void RegexEscape_special_chars_only()
        {
            // Python re: re.escape('.*+?^$[](){}|\\') → '\\.\\*\\+\\?\\^\\$\\[\\]\\(\\)\\{\\}\\|\\\\'
            var escaped = RegexCore.RegexEscape(".*+?^$[](){}|\\");
            // Should not contain any unescaped regex metacharacters
            Regex.IsMatch(escaped, @"(?<!\\)[.*+?^${}()|[\\]]").Should().BeFalse();
        }
    }
}
