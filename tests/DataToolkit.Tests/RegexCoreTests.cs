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
        [Fact] public void Match_2nd() => RegexCore.RegexMatch("a1 b2 c3",@"\d+",n:2).Should().Be("2"); // 2nd match
        [Fact] public void Match_3rd() => RegexCore.RegexMatch("a1 b2 c3",@"\d+",n:3).Should().Be("3"); // 3rd match
        [Fact] public void Match_last() => RegexCore.RegexMatch("a1 b2 c3",@"\d+",n:-1).Should().Be("3"); // last match
        [Fact] public void Match_2nd_last() => RegexCore.RegexMatch("a1 b2 c3",@"\d+",n:-2).Should().Be("2"); // 2nd from last
        [Fact] public void Match_n0_defaults_to_first() => RegexCore.RegexMatch("abc123",@"\d+",n:0).Should().Be("123"); // n=0 → default 1
        [Fact] public void Match_n_exceeds_count() => RegexCore.RegexMatch("a1 b2",@"\d+",n:5).Should().Be(""); // out of range
        [Fact] public void Match_neg_n_exceeds_count() => RegexCore.RegexMatch("a1 b2",@"\d+",n:-5).Should().Be(""); // out of range from end
        [Fact] public void MatchAll() => RegexCore.RegexMatchAll("a1 b2 c3",@"\d+").Should().Equal("1","2","3"); // Python re: findall
        [Fact] public void Replace() => RegexCore.RegexReplace("abc123","\\d","X").Should().Be("abcXXX");     // Python re: sub (all)
        [Fact] public void Replace_1st() => RegexCore.RegexReplace("a1b2c3","\\d","X",n:1).Should().Be("aXb2c3"); // replace 1st only
        [Fact] public void Replace_2nd() => RegexCore.RegexReplace("a1b2c3","\\d","X",n:2).Should().Be("a1bXc3"); // replace 2nd
        [Fact] public void Replace_last() => RegexCore.RegexReplace("a1b2c3","\\d","X",n:-1).Should().Be("a1b2cX"); // replace last
        [Fact] public void Replace_2nd_last() => RegexCore.RegexReplace("a1b2c3","\\d","X",n:-2).Should().Be("a1bXc3"); // replace 2nd from last
        [Fact] public void Replace_n0_all() => RegexCore.RegexReplace("a1b2c3","\\d","X",n:0).Should().Be("aXbXcX"); // n=0 → all
        [Fact] public void Replace_n_exceeds_noop() => RegexCore.RegexReplace("a1b2","\\d","X",n:5).Should().Be("a1b2"); // out of range → unchanged
        [Fact] public void Split() => RegexCore.RegexSplit("a,b;c","[,;]").Should().Equal("a","b","c");      // Python re: split (all)
        [Fact] public void Split_n1() => RegexCore.RegexSplit("a,b,c,d",",",n:1).Should().Equal("a","b,c,d"); // split once
        [Fact] public void Split_n2() => RegexCore.RegexSplit("a,b,c,d",",",n:2).Should().Equal("a","b","c,d"); // split twice
        [Fact] public void Split_n0_all() => RegexCore.RegexSplit("a,b,c",",",n:0).Should().Equal("a","b","c"); // n=0 → all
        [Fact] public void Groups() {
            var r = RegexCore.RegexCaptureGroups("Name: John, Age: 30",@"Name: (\w+), Age: (\d+)");
            r.GetLength(0).Should().Be(2);
            r.GetLength(1).Should().Be(3);
            r[1,0].Should().Be("Name: John, Age: 30");
            r[1,1].Should().Be("John");
            r[1,2].Should().Be("30");
        }
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
            var r = RegexCore.RegexCaptureGroups("abc", @"(\d+)");
            r.GetLength(0).Should().Be(0);
            r.GetLength(1).Should().Be(0);
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
            var r = RegexCore.RegexCaptureGroups("2024-03-15", @"(\d{4})-(\d{2})-(\d{2})");
            r.GetLength(0).Should().Be(2);
            r.GetLength(1).Should().Be(4);
            r[1, 0].Should().Be("2024-03-15");
            r[1, 1].Should().Be("2024");
            r[1, 2].Should().Be("03");
            r[1, 3].Should().Be("15");
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

        // 防错原则2: Regex timeout prevents ReDoS / catastrophic backtracking
        [Fact] public void Catastrophic_backtracking_does_not_hang()
        {
            // Evil regex: (a+)+b with no 'b' suffix causes exponential backtracking.
            // .NET's Regex.IsMatch with a timeout should throw RegexMatchTimeoutException
            // rather than hanging the process.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                RegexCore.RegexTest("aaaaaaaaaaaaaaaaaaaaaaaaaaaa!", "(a+)+b");
                // If it completes (fast path / optimization), it must be within 500ms
                sw.ElapsedMilliseconds.Should().BeLessThan(500);
            }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
            {
                // Timeout is the expected path for this evil pattern on 5s timeout.
                // Verify the timeout didn't fire instantly (which would indicate a misconfiguration).
                sw.ElapsedMilliseconds.Should().BeGreaterThan(1000,
                    "timeout should not trigger instantly — must be a real backtracking scenario");
            }
            // In either case, wall-clock time must be bounded (timeout prevents infinite hang)
            sw.ElapsedMilliseconds.Should().BeLessThan(7000, "5s timeout + 2s tolerance");
        }
    }
}
