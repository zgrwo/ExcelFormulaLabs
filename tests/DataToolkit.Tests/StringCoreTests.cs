using System;
using FormulaLabs.DataToolkit;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.DataToolkit.Tests
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

        [Fact] public void Levenshtein_identical() => StringCore.LevenshteinDistance("abc", "abc").Should().Be(0);
        [Fact] public void Levenshtein_insertion() => StringCore.LevenshteinDistance("abc", "abcd").Should().Be(1);
        [Fact] public void Soundex_same_sounding() => StringCore.Soundex("Robert").Should().Be(StringCore.Soundex("Rupert"));
        [Fact] public void Soundex_different() => StringCore.Soundex("abc").Should().NotBe(StringCore.Soundex("xyz"));
        [Fact] public void HtmlEncode_all_entities() => StringCore.HtmlEncode("<&\">").Should().Contain("&lt;").And.Contain("&amp;");
        [Fact] public void CommonPrefix_no_match() => StringCore.CommonPrefix("abc", "xyz").Should().Be("");
        [Fact] public void CommonPrefix_case_insensitive() => StringCore.CommonPrefix("Hello", "HELP", false).Should().Be("Hel");
        [Fact] public void LeftOf_not_found() => StringCore.LeftOf("hello", ",").Should().Be("hello");
        [Fact] public void RightOf_not_found() => StringCore.RightOf("hello", ",").Should().Be("hello");
        [Fact] public void ExtractBetween_inclusive() => StringCore.ExtractBetween("[hello]", "[", "]", 1, true).Should().Be("[hello]");
        [Fact] public void Reverse_null_safe() => StringCore.ReverseString(null!).Should().Be("");
        [Fact] public void HtmlEncode_null_safe() => StringCore.HtmlEncode(null!).Should().Be("");
        [Fact] public void HtmlDecode_null_safe() => StringCore.HtmlDecode(null!).Should().Be("");
        [Fact] public void UrlEncode_null_safe() => StringCore.UrlEncode(null!).Should().Be("");
        [Fact] public void Base64Encode_null_safe() => StringCore.Base64Encode(null!).Should().Be("");
        [Fact] public void NthWord_negative() => StringCore.NthWord("a b c", -1).Should().Be("c");  // n=-1 → last word
        [Fact] public void NthWord_n0_defaults_to_first() => StringCore.NthWord("the quick brown fox", 0).Should().Be("the");
        [Fact] public void NthWord_negative_two() => StringCore.NthWord("the quick brown fox", -2).Should().Be("brown");
        [Fact] public void NthWord_single_word_negative() => StringCore.NthWord("hello", -1).Should().Be("hello");
        [Fact] public void NthWord_empty_negative() => StringCore.NthWord("", -1).Should().Be("");
        [Fact] public void NthWord_negative_exceeds_length() => StringCore.NthWord("a b c", -4).Should().Be("");
        // n=-1 via NthIdx (tested through LeftOf/RightOf/ExtractBetween since NthIdx is private)
        [Fact] public void LeftOf_last_occurrence() => StringCore.LeftOf("a,b,c", ",", -1).Should().Be("a,b");
        [Fact] public void LeftOf_n0_defaults_to_first() => StringCore.LeftOf("a,b,c", ",", 0).Should().Be("a");
        [Fact] public void LeftOf_negative_two() => StringCore.LeftOf("a,b,c,d", ",", -2).Should().Be("a,b");
        [Fact] public void LeftOf_last_not_found() => StringCore.LeftOf("hello", ",", -1).Should().Be("hello");
        [Fact] public void RightOf_last_occurrence() => StringCore.RightOf("a,b,c", ",", -1).Should().Be("c");
        [Fact] public void RightOf_n0_defaults_to_first() => StringCore.RightOf("a,b,c", ",", 0).Should().Be("b,c");
        [Fact] public void RightOf_negative_two() => StringCore.RightOf("a,b,c,d", ",", -2).Should().Be("c,d");
        [Fact] public void RightOf_last_not_found() => StringCore.RightOf("hello", ",", -1).Should().Be("hello");
        [Fact] public void ExtractBetween_last_occurrence() => StringCore.ExtractBetween("a(b)(c)", "(", ")", -1).Should().Be("c");
        [Fact] public void ExtractBetween_n0_defaults_to_first() => StringCore.ExtractBetween("a(b)(c)", "(", ")", 0).Should().Be("b");
        [Fact] public void ExtractBetween_last_not_found() => StringCore.ExtractBetween("hello", "[", "]", -1).Should().Be("");
        [Fact] public void ExtractBetween_inclusive_last() => StringCore.ExtractBetween("[a][b]", "[", "]", -1, true).Should().Be("[b]");
        [Fact] public void ExtractBetween_nth_occurrence() => StringCore.ExtractBetween("a(b)(c)", "(", ")", 2).Should().Be("c");
        [Fact] public void NormalizeWs_null_safe() => StringCore.NormalizeWhitespace(null!).Should().Be("");
        [Fact] public void PadLeft_overflow() => StringCore.PadLeft("hello", 3).Should().Be("hello");
        [Fact] public void PadRight_overflow() => StringCore.PadRight("hello", 3).Should().Be("hello");

        // =====================================================================
        // CROSS-VALIDATION: Python Reference Values
        // Script: tests/TestData/generate_python_refs.py
        // =====================================================================

        // -- Levenshtein distance (manual DP verified against textdistance) --
        [Fact] public void CrossVal_Lev_kitten_sitting() => StringCore.LevenshteinDistance("kitten", "sitting").Should().Be(3);
        [Fact] public void CrossVal_Lev_empty_a() => StringCore.LevenshteinDistance("", "abc").Should().Be(3);
        [Fact] public void CrossVal_Lev_a_empty() => StringCore.LevenshteinDistance("abc", "").Should().Be(3);
        [Fact] public void CrossVal_Lev_both_empty() => StringCore.LevenshteinDistance("", "").Should().Be(0);
        [Fact] public void CrossVal_Lev_identical() => StringCore.LevenshteinDistance("same", "same").Should().Be(0);
        [Fact] public void CrossVal_Lev_flaw_lawn() => StringCore.LevenshteinDistance("flaw", "lawn").Should().Be(2);
        [Fact] public void CrossVal_Lev_case_sensitive() => StringCore.LevenshteinDistance("abc", "ABC").Should().Be(3);
        [Fact] public void CrossVal_Lev_cafe_coffee() => StringCore.LevenshteinDistance("cafe", "coffee").Should().Be(3);
        [Fact] public void CrossVal_Lev_one_char_diff() => StringCore.LevenshteinDistance("abcdefghij", "abcdeFghij").Should().Be(1);
        [Fact] public void CrossVal_Lev_single_char() => StringCore.LevenshteinDistance("a", "b").Should().Be(1);

        // -- Soundex (American Soundex, verified against jellyfish) --
        [Fact] public void CrossVal_Soundex_Robert() => StringCore.Soundex("Robert").Should().Be("R163");
        [Fact] public void CrossVal_Soundex_Rupert() => StringCore.Soundex("Rupert").Should().Be("R163");
        [Fact] public void CrossVal_Soundex_Rubin() => StringCore.Soundex("Rubin").Should().Be("R150");
        [Fact] public void CrossVal_Soundex_Ashcraft() => StringCore.Soundex("Ashcraft").Should().Be("A261");
        [Fact] public void CrossVal_Soundex_Tymczak() => StringCore.Soundex("Tymczak").Should().Be("T520");
        [Fact] public void CrossVal_Soundex_Pfister() => StringCore.Soundex("Pfister").Should().Be("P236");
        [Fact] public void CrossVal_Soundex_empty() => StringCore.Soundex("").Should().Be("");
        [Fact] public void CrossVal_Soundex_single_A() => StringCore.Soundex("A").Should().Be("A000");

        // -- Base64 (Python stdlib base64) --
        [Fact] public void CrossVal_Base64_empty() => StringCore.Base64Encode("").Should().Be("");
        [Fact] public void CrossVal_Base64_hello() => StringCore.Base64Encode("hello").Should().Be("aGVsbG8=");
        [Fact] public void CrossVal_Base64_HelloWorld() => StringCore.Base64Encode("Hello World!").Should().Be("SGVsbG8gV29ybGQh");
        [Fact] public void CrossVal_Base64_pad_1() => StringCore.Base64Encode("ab").Should().Be("YWI=");
        [Fact] public void CrossVal_Base64_pad_0() => StringCore.Base64Encode("abc").Should().Be("YWJj");
        [Fact] public void CrossVal_Base64_pad_2() => StringCore.Base64Encode("abcd").Should().Be("YWJjZA==");
        [Fact] public void CrossVal_Base64_roundtrip() => StringCore.Base64Decode(StringCore.Base64Encode("Hello World!")).Should().Be("Hello World!");

        // -- URL Encode (Python urllib.parse.quote) --
        [Fact] public void CrossVal_UrlEncode_space() => StringCore.UrlEncode("hello world").Should().Be("hello%20world");
        [Fact] public void CrossVal_UrlEncode_special() => StringCore.UrlEncode("a+b=c").Should().Be("a%2Bb%3Dc");
        [Fact] public void CrossVal_UrlEncode_slash() => StringCore.UrlEncode("/path/to/file").Should().Be("%2Fpath%2Fto%2Ffile");
        [Fact] public void CrossVal_UrlEncode_empty() => StringCore.UrlEncode("").Should().Be("");
        [Fact] public void CrossVal_UrlEncode_percent() => StringCore.UrlEncode("100%").Should().Be("100%25");

        // -- HTML Encode (Python html.escape) --
        [Fact] public void CrossVal_HtmlEncode_script() { var r = StringCore.HtmlEncode("<script>"); r.Should().Contain("&lt;").And.Contain("&gt;"); }
        [Fact] public void CrossVal_HtmlEncode_amp() => StringCore.HtmlEncode("a & b").Should().Be("a &amp; b");
        [Fact] public void CrossVal_HtmlEncode_quote() => StringCore.HtmlEncode("\"quoted\"").Should().Contain("&quot;");
        [Fact] public void CrossVal_HtmlEncode_normal() => StringCore.HtmlEncode("normal text").Should().Be("normal text");
        [Fact] public void CrossVal_HtmlEncode_empty() => StringCore.HtmlEncode("").Should().Be("");
    }
}
