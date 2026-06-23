using System;
using ExcelVbaLibraries.DataToolkit;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    // Python ref: encoding→base64/urllib.parse/html, uuid→uuid, lev→python-Levenshtein, soundex→jellyfish
    public class StringCoreTests
    {
        [Fact] public void Reverse() => StringCore.ReverseString("hello").Should().Be("olleh");
        [Fact] public void Reverse_empty() => StringCore.ReverseString("").Should().Be("");
        [Fact] public void NormalizeWs() => StringCore.NormalizeWhitespace("  a   b  ").Should().Be("a b");
        [Fact] public void TitleCase() => StringCore.ToTitleCase("hello world").Should().Be("Hello World");
        [Fact] public void RemoveChars() => StringCore.RemoveChars("a-b-c", "-").Should().Be("abc");
        [Fact] public void KeepChars() => StringCore.KeepChars("a1b2c3", "0123456789").Should().Be("123");
        [Fact] public void PadLeft() => StringCore.PadLeft("hi",5,'.').Should().Be("...hi");
        [Fact] public void PadRight() => StringCore.PadRight("hi",4).Should().Be("hi  ");
        [Fact] public void Truncate() => StringCore.Truncate("hello world",8).Should().Be("hello...");
        [Fact] public void CountSubstring() => StringCore.CountSubstring("banana","ana").Should().Be(1);
        [Fact] public void CountSubstring_cs() => StringCore.CountSubstring("BANANA","ana",false).Should().Be(1);
        [Fact] public void StartsWith_true() => StringCore.StartsWithStr("hello","hel").Should().BeTrue();
        [Fact] public void EndsWith_true() => StringCore.EndsWithStr("hello","lo").Should().BeTrue();
        [Fact] public void LeftOf() => StringCore.LeftOf("a,b,c",",").Should().Be("a");
        [Fact] public void RightOf() => StringCore.RightOf("a,b,c",",").Should().Be("b,c");
        [Fact] public void ExtractBetween() => StringCore.ExtractBetween("[hello]","[","]").Should().Be("hello");
        [Fact] public void NthWord() => StringCore.NthWord("the quick brown fox",3).Should().Be("brown");
        [Fact] public void CommonPrefix() => StringCore.CommonPrefix("hello world","hello there").Should().Be("hello ");
        [Fact] public void Levenshtein() => StringCore.LevenshteinDistance("kitten","sitting").Should().Be(3);
        [Fact] public void Soundex() => StringCore.Soundex("Robert").Should().Be("R163");
        [Fact] public void UrlEncode() => StringCore.UrlEncode("hello world").Should().Contain("%20");
        [Fact] public void Base64_roundtrip() => StringCore.Base64Decode(StringCore.Base64Encode("test")).Should().Be("test");
        [Fact] public void UUID_has_dashes() => StringCore.UUID().Should().MatchRegex(@"^[0-9a-f-]{36}$");
        [Fact] public void RandomString_length() => StringCore.RandomString(10).Length.Should().Be(10);
        [Fact] public void IsNullOrEmpty() { StringCore.IsNullOrEmptyStr("").Should().BeTrue(); StringCore.IsNullOrEmptyStr("x").Should().BeFalse(); }
        [Fact] public void TextJoin() => StringCore.TextJoin(",",false,new[]{"a","b","c"}).Should().Be("a,b,c");
        [Fact] public void TextJoin_skipEmpty() => StringCore.TextJoin(",",true,new[]{"a","","c"}).Should().Be("a,c");
        [Fact] public void HtmlEncode() => StringCore.HtmlEncode("<script>").Should().Be("&lt;script&gt;");
        [Fact] public void HtmlDecode() => StringCore.HtmlDecode("&lt;tag&gt;").Should().Be("<tag>");
        [Fact] public void HtmlEncode_roundtrip() => StringCore.HtmlDecode(StringCore.HtmlEncode("<tag>")).Should().Be("<tag>");
        [Fact] public void IsNullOrWhitespaceStr() { StringCore.IsNullOrWhitespaceStr("   ").Should().BeTrue(); StringCore.IsNullOrWhitespaceStr(" a ").Should().BeFalse(); StringCore.IsNullOrWhitespaceStr("").Should().BeTrue(); }
        [Fact] public void Coalesce_first() => StringCore.Coalesce("hello","world").Should().Be("hello");
        [Fact] public void Coalesce_null() => StringCore.Coalesce(null!,"fallback").Should().Be("fallback");
        [Fact] public void Coalesce_empty() => StringCore.Coalesce("","fallback").Should().Be("");
        [Fact] public void RandomString_custom_charset() => StringCore.RandomString(100,"ABC").Should().MatchRegex("^[ABC]+$");

        // =====================================================================
        // EDGE CASE & ERROR BEHAVIOR TESTS
        // =====================================================================

        [Fact] public void Truncate_max_zero()
        {
            StringCore.Truncate("hello", 0).Should().Be("");
        }

        [Fact] public void Truncate_max_negative()
        {
            StringCore.Truncate("hello", -1).Should().Be("");
        }

        [Fact] public void Truncate_short_string()
        {
            StringCore.Truncate("hi", 10).Should().Be("hi");
        }

        [Fact] public void Base64Decode_invalid_throws()
        {
            var act = () => StringCore.Base64Decode("!!!invalid!!!");
            act.Should().Throw<Exception>();
        }

        [Fact] public void ExtractBetween_no_match()
        {
            StringCore.ExtractBetween("hello", "[", "]").Should().Be("");
        }

        [Fact] public void Levenshtein_empty_both()
        {
            StringCore.LevenshteinDistance("", "").Should().Be(0);
        }

        [Fact] public void Levenshtein_one_empty()
        {
            StringCore.LevenshteinDistance("abc", "").Should().Be(3);
        }

        [Fact] public void Soundex_empty()
        {
            StringCore.Soundex("").Should().Be("");
        }

        [Fact] public void CountSubstring_empty_needle()
        {
            StringCore.CountSubstring("hello", "").Should().Be(0);
        }

        [Fact] public void CountSubstring_null_needle()
        {
            StringCore.CountSubstring("hello", null!).Should().Be(0);
        }

        [Fact] public void CountSubstring_null_text()
        {
            StringCore.CountSubstring(null!, "a").Should().Be(0);
        }

        [Fact] public void CountSubstring_both_null()
        {
            StringCore.CountSubstring(null!, null!).Should().Be(0);
        }

        [Fact] public void CountSubstring_empty_text()
        {
            StringCore.CountSubstring("", "a").Should().Be(0);
        }

        [Fact] public void CountSubstring_multiple_occurrences()
        {
            StringCore.CountSubstring("aaa", "a").Should().Be(3);
            StringCore.CountSubstring("aaaa", "aa").Should().Be(2);
        }
    }
}
