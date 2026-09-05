using System;
using System.Text.RegularExpressions;
using ExcelFormulaLabs.DataToolkit;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.DataToolkit.Tests
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
                // P2 (review): removed the elapsed>1000ms lower-bound assertion — it depends
                // on engine internals (fast-reject paths in newer .NET can fail earlier) and
                // machine speed; the only contract is: bounded wall-clock time (below).
            }
            // In either case, wall-clock time must be bounded (timeout prevents infinite hang)
            sw.ElapsedMilliseconds.Should().BeLessThan(7000, "5s timeout + 2s tolerance");
        }

        [Fact] public void ValidatePattern_exceeds_max_length_throws()
        {
            var longPattern = new string('x', RegexCore.MaxPatternLength + 1);
            var act = () => RegexCore.RegexTest("test", longPattern);
            act.Should().Throw<ArgumentException>().WithMessage("*exceeds maximum length*");
        }

        [Fact] public void ValidatePattern_at_max_length_passes()
        {
            var maxPattern = new string('x', RegexCore.MaxPatternLength);
            var act = () => RegexCore.RegexTest("test", maxPattern);
            act.Should().NotThrow();
        }

        // ── Release-review regression guards ────────────────────────────────
        // Literal replacement semantics: '$' patterns are NOT interpreted, for every n.
        [Fact] public void Replace_all_dollar_is_literal() => RegexCore.RegexReplace("a1b2", @"\d", "$1").Should().Be("a$1b$1");
        [Fact] public void Replace_first_dollar_is_literal() => RegexCore.RegexReplace("a1b2", @"\d", "$1", n: 1).Should().Be("a$1b2");
        [Fact] public void Replace_nth_dollar_is_literal() => RegexCore.RegexReplace("a1b2", @"\d", "$&", n: -1).Should().Be("a1b$&");

        // ── R01 回归守卫（review 2026-09-05）─────────────────────────────────
        // 原实现 `new List<string>((int)n + 1)` 在任何 regex 求值前执行：巨型 n →
        // 8.6–17.2GB 预分配（OOM 不可捕获）或 (int) 回绕负容量（ArgumentOutOfRangeException）。
        // 现契约：n 饱和到 100_000、不抛异常；对纪律内输入等价于全拆分。期望硬编码。
        [Fact] public void Split_n2_normal_unchanged() => RegexCore.RegexSplit("a,b,c,d", ",", n: 2).Should().Equal("a", "b", "c,d");
        [Fact] public void Split_int_max_n_full_split_no_throw()
            => RegexCore.RegexSplit("a,b,c,d", ",", n: 2147483647L).Should().Equal("a", "b", "c", "d");
        [Fact] public void Split_n_beyond_int_max_full_split_no_throw()
            => RegexCore.RegexSplit("a,b,c,d", ",", n: 2147483648L).Should().Equal("a", "b", "c", "d");
        [Fact] public void Split_huge_n_full_split_no_throw()
            => RegexCore.RegexSplit("a,b,c,d", ",", n: 5000000000L).Should().Equal("a", "b", "c", "d");
        [Fact] public void Split_negative_n_falls_back_to_unlimited()
            => RegexCore.RegexSplit("a,b,c,d", ",", n: -1L).Should().Equal("a", "b", "c", "d");
        [Fact] public void Split_n_over_cap_saturates_to_cap()
        {
            // n=100_001 饱和到 100_000（上限语义）：100_002 个逗号的输入在无限拆分下
            // 应得 100_003 段；上限语义截为 100_001 段（100_000 次拆分 + 1 段尾部）。
            var s = new string(',', 100_002);
            var parts = RegexCore.RegexSplit(s, ",", n: 100_001L);
            parts.Length.Should().Be(100_001);
        }
        [Fact] public void Split_n_at_cap_boundary_unchanged()
        {
            // 边界内 n（100_000）行为与既有语义一致：3 个匹配的输入全拆。
            RegexCore.RegexSplit("a,b,c,d", ",", n: 100_000L).Should().Equal("a", "b", "c", "d");
        }
    }
}