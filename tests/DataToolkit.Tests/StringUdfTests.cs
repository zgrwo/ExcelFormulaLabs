using FormulaLabs.DataToolkit;
using FormulaLabs.Foundation;
using FluentAssertions;
#pragma warning disable CS8625 // null literal for UDF null-input testing
using Xunit;

namespace FormulaLabs.DataToolkit.Tests
{
    public class StringUdfTests
    {
        // ══════════════════════════════════════════════════════════════════
        //  STR.REVERSE  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Rev_scalar() => StringUdf.UDF_STR_REV("hello").Should().Be("olleh");
        [Fact] public void Rev_empty() => StringUdf.UDF_STR_REV("").Should().Be("");
        [Fact] public void Rev_null() => StringUdf.UDF_STR_REV(null!).Should().BeNull();
        [Fact] public void Rev_error() => StringUdf.UDF_STR_REV(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void Rev_1D() { var r=(object[])StringUdf.UDF_STR_REV(new object[]{"ab","cd"}); r.Should().Equal("ba","dc"); }
        [Fact] public void Rev_2D() { var r=(object[,])StringUdf.UDF_STR_REV(new object[,]{{"ab"},{"cd"}}); r[0,0].Should().Be("ba"); }
        [Fact] public void Rev_unicode() => StringUdf.UDF_STR_REV("你好").Should().Be("好你");
        [Fact] public void Rev_palindrome() => StringUdf.UDF_STR_REV("radar").Should().Be("radar");
        [Fact] public void Rev_long_str() { var s=new string('a',1000); StringUdf.UDF_STR_REV(s).Should().Be(s); }
        [Fact] public void Rev_special_chars() => StringUdf.UDF_STR_REV("a!@#b").Should().Be("b#@!a");

        // ══════════════════════════════════════════════════════════════════
        //  STR.NORMWS  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Nws_collapse_multiple_spaces() => StringUdf.UDF_STR_NWS("a   b  c").Should().Be("a b c");
        [Fact] public void Nws_trim_leading_trailing() => StringUdf.UDF_STR_NWS("  hello world  ").Should().Be("hello world");
        [Fact] public void Nws_tabs_newlines() => StringUdf.UDF_STR_NWS("a\tb\nc").Should().Be("a b c");
        [Fact] public void Nws_empty() => StringUdf.UDF_STR_NWS("").Should().Be("");
        [Fact] public void Nws_only_spaces() => StringUdf.UDF_STR_NWS("    ").Should().Be("");
        [Fact] public void Nws_null() => StringUdf.UDF_STR_NWS(null!).Should().BeNull();
        [Fact] public void Nws_error() => StringUdf.UDF_STR_NWS(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void Nws_array() { var r=(object[])StringUdf.UDF_STR_NWS(new object[]{" a  b "," c  d "}); r.Should().Equal("a b","c d"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.TITLE  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Title_multi_word() => StringUdf.UDF_STR_TITLE("hello world").Should().Be("Hello World");
        [Fact] public void Title_already_capitalized() => StringUdf.UDF_STR_TITLE("Hello World").Should().Be("Hello World");
        [Fact] public void Title_single_word() => StringUdf.UDF_STR_TITLE("hello").Should().Be("Hello");
        [Fact] public void Title_all_caps() => StringUdf.UDF_STR_TITLE("HELLO WORLD").Should().Be("Hello World");
        [Fact] public void Title_empty() => StringUdf.UDF_STR_TITLE("").Should().Be("");
        [Fact] public void Title_null() => StringUdf.UDF_STR_TITLE(null!).Should().BeNull();
        [Fact] public void Title_error() => StringUdf.UDF_STR_TITLE(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void Title_array() { var r=(object[])StringUdf.UDF_STR_TITLE(new object[]{"one two","three four"}); r.Should().Equal("One Two","Three Four"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.REMOVE  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Rem_remove_chars() => StringUdf.UDF_STR_REM("hello", "l").Should().Be("heo");
        [Fact] public void Rem_remove_multiple() => StringUdf.UDF_STR_REM("abc123", "a1c3").Should().Be("b2");
        [Fact] public void Rem_nonexistent_chars() => StringUdf.UDF_STR_REM("hello", "xyz").Should().Be("hello");
        [Fact] public void Rem_empty_text() => StringUdf.UDF_STR_REM("", "abc").Should().Be("");
        [Fact] public void Rem_empty_chars() => StringUdf.UDF_STR_REM("hello", "").Should().Be("hello");
        [Fact] public void Rem_null_text() => StringUdf.UDF_STR_REM(null!, "a").Should().BeNull();
        [Fact] public void Rem_array() { var r=(object[])StringUdf.UDF_STR_REM(new object[]{"hello","world"}, "l"); r.Should().Equal("heo","word"); }
        [Fact] public void Rem_unicode() => StringUdf.UDF_STR_REM("héllo", "l").Should().Be("héo");

        // ══════════════════════════════════════════════════════════════════
        //  STR.KEEP  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Keep_keep_chars() => StringUdf.UDF_STR_KEEP("hello", "l").Should().Be("ll");
        [Fact] public void Keep_keep_digits() => StringUdf.UDF_STR_KEEP("a1b2c3", "0123456789").Should().Be("123");
        [Fact] public void Keep_none_matching() => StringUdf.UDF_STR_KEEP("abc", "123").Should().Be("");
        [Fact] public void Keep_empty_text() => StringUdf.UDF_STR_KEEP("", "abc").Should().Be("");
        [Fact] public void Keep_empty_chars() => StringUdf.UDF_STR_KEEP("hello", "").Should().Be("");
        [Fact] public void Keep_null_text() => StringUdf.UDF_STR_KEEP(null!, "a").Should().BeNull();
        [Fact] public void Keep_array() { var r=(object[])StringUdf.UDF_STR_KEEP(new object[]{"a1","b2"}, "0123456789"); r.Should().Equal("1","2"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.PADLEFT  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Padl_basic() => StringUdf.UDF_STR_PADL("42", 5, "0").Should().Be("00042");
        [Fact] public void Padl_already_longer() => StringUdf.UDF_STR_PADL("hello", 3, "x").Should().Be("hello");
        [Fact] public void Padl_empty() => StringUdf.UDF_STR_PADL("", 3, "*").Should().Be("***");
        [Fact] public void Padl_zero_length() => StringUdf.UDF_STR_PADL("abc", 0, "0").Should().Be("abc");
        [Fact] public void Padl_space_pad() => StringUdf.UDF_STR_PADL("hi", 5, " ").Should().Be("   hi");
        [Fact] public void Padl_null_text() => StringUdf.UDF_STR_PADL(null!, 5, "0").Should().BeNull();
        [Fact] public void Padl_null_pad_defaults_to_space() => ((string)StringUdf.UDF_STR_PADL("hi", 5, null!)).Should().Be("   hi");
        [Fact] public void Padl_empty_pad_defaults_to_space() => ((string)StringUdf.UDF_STR_PADL("hi", 5, "")).Should().Be("   hi");
        [Fact] public void Padl_array() { var r=(object[])StringUdf.UDF_STR_PADL(new object[]{"1","22"}, 3, "0"); r.Should().Equal("001","022"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.PADRIGHT  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Padr_basic() => StringUdf.UDF_STR_PADR("42", 5, "0").Should().Be("42000");
        [Fact] public void Padr_already_longer() => StringUdf.UDF_STR_PADR("hello", 3, "x").Should().Be("hello");
        [Fact] public void Padr_empty() => StringUdf.UDF_STR_PADR("", 3, ".").Should().Be("...");
        [Fact] public void Padr_zero_length() => StringUdf.UDF_STR_PADR("abc", 0, "0").Should().Be("abc");
        [Fact] public void Padr_space_pad() => StringUdf.UDF_STR_PADR("hi", 5, " ").Should().Be("hi   ");
        [Fact] public void Padr_null_text() => StringUdf.UDF_STR_PADR(null!, 5, "0").Should().BeNull();
        [Fact] public void Padr_null_pad_defaults_to_space() => ((string)StringUdf.UDF_STR_PADR("hi", 5, null!)).Should().Be("hi   ");
        [Fact] public void Padr_empty_pad_defaults_to_space() => ((string)StringUdf.UDF_STR_PADR("hi", 5, "")).Should().Be("hi   ");
        [Fact] public void Padr_array() { var r=(object[])StringUdf.UDF_STR_PADR(new object[]{"1","22"}, 3, "0"); r.Should().Equal("100","220"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.TRUNCATE  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Trunc_shortens_with_suffix() => StringUdf.UDF_STR_TRUNC("hello world", 5, "...").Should().Be("he...");
        [Fact] public void Trunc_no_truncation_needed() => StringUdf.UDF_STR_TRUNC("hi", 5, "...").Should().Be("hi");
        [Fact] public void Trunc_empty_string() => StringUdf.UDF_STR_TRUNC("", 5, "...").Should().Be("");
        [Fact] public void Trunc_empty_suffix() => StringUdf.UDF_STR_TRUNC("hello", 3, "").Should().Be("hel");
        [Fact] public void Trunc_null_text() => StringUdf.UDF_STR_TRUNC(null!, 5, "...").Should().BeNull();
        [Fact] public void Trunc_array() { var r=(object[])StringUdf.UDF_STR_TRUNC(new object[]{"hello","world"}, 3, "."); r.Should().Equal("he.","wo."); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.COUNTSUB  (MapOverMulti<string,string,long>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Cnt_basic() => ((long)StringUdf.UDF_STR_CNT("ababab", "ab", false)).Should().Be(3);
        [Fact] public void Cnt_case_sensitive() => ((long)StringUdf.UDF_STR_CNT("AbAbAb", "ab", true)).Should().Be(0);
        [Fact] public void Cnt_case_insensitive() => ((long)StringUdf.UDF_STR_CNT("AbAbAb", "ab", false)).Should().Be(3);
        [Fact] public void Cnt_not_found() => ((long)StringUdf.UDF_STR_CNT("hello", "xyz", false)).Should().Be(0);
        [Fact] public void Cnt_empty_text() => ((long)StringUdf.UDF_STR_CNT("", "a", false)).Should().Be(0);
        [Fact] public void Cnt_array() { var r=(object[])StringUdf.UDF_STR_CNT(new object[]{"aaa","aba"}, "a", false); ((long)r[0]).Should().Be(3); ((long)r[1]).Should().Be(2); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.STARTSWITH  (MapOverMulti<string,string,bool>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Sw_basic() => ((bool)StringUdf.UDF_STR_SW("hello world", "hello", false)).Should().BeTrue();
        [Fact] public void Sw_not_match() => ((bool)StringUdf.UDF_STR_SW("hello world", "world", false)).Should().BeFalse();
        [Fact] public void Sw_case_sensitive() => ((bool)StringUdf.UDF_STR_SW("Hello", "hello", true)).Should().BeFalse();
        [Fact] public void Sw_case_insensitive() => ((bool)StringUdf.UDF_STR_SW("Hello", "hello", false)).Should().BeTrue();
        [Fact] public void Sw_empty_prefix() => ((bool)StringUdf.UDF_STR_SW("hello", "", false)).Should().BeTrue();
        [Fact] public void Sw_empty_text() => ((bool)StringUdf.UDF_STR_SW("", "h", false)).Should().BeFalse();
        [Fact] public void Sw_array() { var r=(object[])StringUdf.UDF_STR_SW(new object[]{"apple","banana"}, "ap", false); ((bool)r[0]).Should().BeTrue(); ((bool)r[1]).Should().BeFalse(); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.ENDSWITH  (MapOverMulti<string,string,bool>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Ew_basic() => ((bool)StringUdf.UDF_STR_EW("hello world", "world", false)).Should().BeTrue();
        [Fact] public void Ew_not_match() => ((bool)StringUdf.UDF_STR_EW("hello world", "hello", false)).Should().BeFalse();
        [Fact] public void Ew_case_sensitive() => ((bool)StringUdf.UDF_STR_EW("Hello.World", "world", true)).Should().BeFalse();
        [Fact] public void Ew_case_insensitive() => ((bool)StringUdf.UDF_STR_EW("Hello.World", "world", false)).Should().BeTrue();
        [Fact] public void Ew_empty_suffix() => ((bool)StringUdf.UDF_STR_EW("hello", "", false)).Should().BeTrue();
        [Fact] public void Ew_empty_text() => ((bool)StringUdf.UDF_STR_EW("", "d", false)).Should().BeFalse();
        [Fact] public void Ew_array() { var r=(object[])StringUdf.UDF_STR_EW(new object[]{"test.cs","test.vb"}, ".cs", false); ((bool)r[0]).Should().BeTrue(); ((bool)r[1]).Should().BeFalse(); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.LEFTOF  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Lof_basic() => StringUdf.UDF_STR_LOF("name=value", "=", 1).Should().Be("name");
        [Fact] public void Lof_second_occurrence() => StringUdf.UDF_STR_LOF("a,b,c", ",", 2).Should().Be("a,b");
        [Fact] public void Lof_not_found() => StringUdf.UDF_STR_LOF("hello", "|", 1).Should().Be("hello");
        [Fact] public void Lof_empty() => StringUdf.UDF_STR_LOF("", "|", 1).Should().Be("");
        [Fact] public void Lof_null_text() => StringUdf.UDF_STR_LOF(null!, "|", 1).Should().BeNull();
        [Fact] public void Lof_nth_zero() => StringUdf.UDF_STR_LOF("a,b,c", ",", 0).Should().Be("a,b,c");
        [Fact] public void Lof_array() { var r=(object[])StringUdf.UDF_STR_LOF(new object[]{"a=x","b=y"}, "=", 1); r.Should().Equal("a","b"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.RIGHTOF  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Rof_basic() => StringUdf.UDF_STR_ROF("name=value", "=", 1).Should().Be("value");
        [Fact] public void Rof_second_occurrence() => StringUdf.UDF_STR_ROF("a,b,c", ",", 2).Should().Be("c");
        [Fact] public void Rof_not_found() => StringUdf.UDF_STR_ROF("hello", "|", 1).Should().Be("hello");
        [Fact] public void Rof_empty() => StringUdf.UDF_STR_ROF("", "|", 1).Should().Be("");
        [Fact] public void Rof_null_text() => StringUdf.UDF_STR_ROF(null!, "|", 1).Should().BeNull();
        [Fact] public void Rof_array() { var r=(object[])StringUdf.UDF_STR_ROF(new object[]{"a=x","b=y"}, "=", 1); r.Should().Equal("x","y"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.EXTRACT  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Ext_basic() => StringUdf.UDF_STR_EXT("a{b}c", "{", "}", 1, false).Should().Be("b");
        [Fact] public void Ext_second_occurrence() => StringUdf.UDF_STR_EXT("[1][2][3]", "[", "]", 2, false).Should().Be("2");
        [Fact] public void Ext_inclusive() => StringUdf.UDF_STR_EXT("a{b}c", "{", "}", 1, true).Should().Be("{b}");
        [Fact] public void Ext_not_found() => StringUdf.UDF_STR_EXT("hello", "(", ")", 1, false).Should().Be("");
        [Fact] public void Ext_empty() => StringUdf.UDF_STR_EXT("", "{", "}", 1, false).Should().Be("");
        [Fact] public void Ext_null_text() => StringUdf.UDF_STR_EXT(null!, "{", "}", 1, false).Should().BeNull();
        [Fact] public void Ext_array() { var r=(object[])StringUdf.UDF_STR_EXT(new object[]{"<a>","<b>"}, "<", ">", 1, false); r.Should().Equal("a","b"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.NTHWORD  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Nthw_first_word() => StringUdf.UDF_STR_NTHW("hello world foo bar", 1).Should().Be("hello");
        [Fact] public void Nthw_third_word() => StringUdf.UDF_STR_NTHW("hello world foo bar", 3).Should().Be("foo");
        [Fact] public void Nthw_out_of_range() => StringUdf.UDF_STR_NTHW("hello world", 5).Should().Be("");
        [Fact] public void Nthw_zero() => StringUdf.UDF_STR_NTHW("hello world", 0).Should().Be("hello");  // n≤0→1
        [Fact] public void Nthw_empty() => StringUdf.UDF_STR_NTHW("", 1).Should().Be("");
        [Fact] public void Nthw_null() => StringUdf.UDF_STR_NTHW(null!, 1).Should().BeNull();
        [Fact] public void Nthw_error() => StringUdf.UDF_STR_NTHW(ExcelError.NA, 1).Should().Be(ExcelError.NA);
        [Fact] public void Nthw_array() { var r=(object[])StringUdf.UDF_STR_NTHW(new object[]{"a b c","x y z"}, 2); r.Should().Equal("b","y"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.COMMONPFX  (MapOverMulti<string,string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Cpfx_basic() => StringUdf.UDF_STR_CPFX("hello world", "hello there", false).Should().Be("hello ");
        [Fact] public void Cpfx_no_common() => StringUdf.UDF_STR_CPFX("abc", "xyz", false).Should().Be("");
        [Fact] public void Cpfx_case_sensitive() => StringUdf.UDF_STR_CPFX("Hello", "hello", true).Should().Be("");
        [Fact] public void Cpfx_one_empty() => StringUdf.UDF_STR_CPFX("", "hello", false).Should().Be("");
        [Fact] public void Cpfx_array() { var r=(object[])StringUdf.UDF_STR_CPFX(new object[]{"apple","apply"}, "app", false); r.Should().Equal("app","app"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.TEXTJOIN  (manual multi-arg — null first → ExcelError.Value)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Join_basic() => StringUdf.UDF_STR_JOIN(",", false, new object[]{"a","b","c"}).Should().Be("a,b,c");
        [Fact] public void Join_empty_delimiter() => StringUdf.UDF_STR_JOIN("", false, new object[]{"a","b"}).Should().Be("ab");
        [Fact] public void Join_skip_empty() => StringUdf.UDF_STR_JOIN(",", true, new object[]{"a","","b"}).Should().Be("a,b");
        [Fact] public void Join_keep_empty() => StringUdf.UDF_STR_JOIN(",", false, new object[]{"a","","b"}).Should().Be("a,,b");
        [Fact] public void Join_single_item() => StringUdf.UDF_STR_JOIN(",", false, new object[]{"only"}).Should().Be("only");
        [Fact] public void Join_empty_array() => StringUdf.UDF_STR_JOIN(",", false, new object[0]).Should().Be("");
        [Fact] public void Join_null_in_array() => StringUdf.UDF_STR_JOIN(",", true, new object[]{"a",null,"b"}).Should().Be("a,b");

        // ══════════════════════════════════════════════════════════════════
        //  STR.LEVENSHTEIN  (MapOverMulti<string,string,long>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Lev_identical() => ((long)StringUdf.UDF_STR_LEV("hello", "hello")).Should().Be(0);
        [Fact] public void Lev_substitution() => ((long)StringUdf.UDF_STR_LEV("kitten", "sitten")).Should().Be(1);
        [Fact] public void Lev_insertion() => ((long)StringUdf.UDF_STR_LEV("cat", "cats")).Should().Be(1);
        [Fact] public void Lev_deletion() => ((long)StringUdf.UDF_STR_LEV("cats", "cat")).Should().Be(1);
        [Fact] public void Lev_completely_different() => ((long)StringUdf.UDF_STR_LEV("abc", "xyz")).Should().Be(3);
        [Fact] public void Lev_both_empty() => ((long)StringUdf.UDF_STR_LEV("", "")).Should().Be(0);
        [Fact] public void Lev_one_empty() => ((long)StringUdf.UDF_STR_LEV("hello", "")).Should().Be(5);

        // ══════════════════════════════════════════════════════════════════
        //  STR.SOUNDEX  (MapOver<string,string> — null→null, not error)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Sdx_basic() => StringUdf.UDF_STR_SDX("Robert").Should().Be("R163");
        [Fact] public void Sdx_same_sounding() { var r1=StringUdf.UDF_STR_SDX("Robert"); var r2=StringUdf.UDF_STR_SDX("Rupert"); r1.Should().Be(r2); }
        [Fact] public void Sdx_different() => StringUdf.UDF_STR_SDX("abc").Should().NotBe(StringUdf.UDF_STR_SDX("xyz"));
        [Fact] public void Sdx_empty() => StringUdf.UDF_STR_SDX("").Should().Be("");
        [Fact] public void Sdx_null() => StringUdf.UDF_STR_SDX(null!).Should().BeNull();
        [Fact] public void Sdx_error() => StringUdf.UDF_STR_SDX(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void Sdx_array() { var r=(object[])StringUdf.UDF_STR_SDX(new object[]{"Robert","hello"}); ((string)r[0]).Should().Be("R163"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.URLENCODE  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void UEnc_spaces() => ((string)StringUdf.UDF_STR_UENC("hello world")).Should().Contain("%20");
        [Fact] public void UEnc_special_chars() => ((string)StringUdf.UDF_STR_UENC("name=John&Doe")).Should().Contain("%26");
        [Fact] public void UEnc_plain_text() => StringUdf.UDF_STR_UENC("hello").Should().Be("hello");
        [Fact] public void UEnc_empty() => StringUdf.UDF_STR_UENC("").Should().Be("");
        [Fact] public void UEnc_null() => StringUdf.UDF_STR_UENC(null!).Should().BeNull();
        [Fact] public void UEnc_error() => StringUdf.UDF_STR_UENC(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void UEnc_array() { var r=(object[])StringUdf.UDF_STR_UENC(new object[]{"a b","c d"}); ((string)r[0]).Should().Contain("%20"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.URLDECODE  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void UDec_spaces() => StringUdf.UDF_STR_UDEC("hello%20world").Should().Be("hello world");
        [Fact] public void UDec_ampersand() => StringUdf.UDF_STR_UDEC("a%26b").Should().Be("a&b");
        [Fact] public void UDec_plain_text() => StringUdf.UDF_STR_UDEC("hello").Should().Be("hello");
        [Fact] public void UDec_empty() => StringUdf.UDF_STR_UDEC("").Should().Be("");
        [Fact] public void UDec_null() => StringUdf.UDF_STR_UDEC(null!).Should().BeNull();
        [Fact] public void UDec_error() => StringUdf.UDF_STR_UDEC(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void UDec_roundtrip() => StringUdf.UDF_STR_UDEC(StringUdf.UDF_STR_UENC("hello world").ToString()!).Should().Be("hello world");

        // ══════════════════════════════════════════════════════════════════
        //  STR.HTMLENCODE  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void HEnc_angle_brackets() => ((string)StringUdf.UDF_STR_HENC("<div>")).Should().Contain("&lt;");
        [Fact] public void HEnc_ampersand() => ((string)StringUdf.UDF_STR_HENC("a & b")).Should().Contain("&amp;");
        [Fact] public void HEnc_plain_text() => StringUdf.UDF_STR_HENC("hello").Should().Be("hello");
        [Fact] public void HEnc_empty() => StringUdf.UDF_STR_HENC("").Should().Be("");
        [Fact] public void HEnc_null() => StringUdf.UDF_STR_HENC(null!).Should().BeNull();
        [Fact] public void HEnc_error() => StringUdf.UDF_STR_HENC(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void HEnc_array() { var r=(object[])StringUdf.UDF_STR_HENC(new object[]{"<a>","<b>"}); ((string)r[0]).Should().Contain("&lt;"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.HTMLDECODE  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void HDec_angle_brackets() => StringUdf.UDF_STR_HDEC("&lt;div&gt;").Should().Be("<div>");
        [Fact] public void HDec_ampersand() => StringUdf.UDF_STR_HDEC("a&amp;b").Should().Be("a&b");
        [Fact] public void HDec_plain_text() => StringUdf.UDF_STR_HDEC("hello").Should().Be("hello");
        [Fact] public void HDec_empty() => StringUdf.UDF_STR_HDEC("").Should().Be("");
        [Fact] public void HDec_null() => StringUdf.UDF_STR_HDEC(null!).Should().BeNull();
        [Fact] public void HDec_error() => StringUdf.UDF_STR_HDEC(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void HDec_roundtrip() => StringUdf.UDF_STR_HDEC(StringUdf.UDF_STR_HENC("<div>").ToString()!).Should().Be("<div>");

        // ══════════════════════════════════════════════════════════════════
        //  STR.BASE64ENC  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void B64Enc_basic() => ((string)StringUdf.UDF_STR_B64ENC("hello")).Should().NotBeNullOrEmpty().And.NotBe("hello");
        [Fact] public void B64Enc_empty() => StringUdf.UDF_STR_B64ENC("").Should().Be("");
        [Fact] public void B64Enc_null() => StringUdf.UDF_STR_B64ENC(null!).Should().BeNull();
        [Fact] public void B64Enc_error() => StringUdf.UDF_STR_B64ENC(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void B64Enc_long_string() { var s=new string('x',200); StringUdf.UDF_STR_B64ENC(s).Should().NotBeNull(); }
        [Fact] public void B64Enc_roundtrip() { var decoded=StringUdf.UDF_STR_B64DEC(StringUdf.UDF_STR_B64ENC("hello").ToString()!); decoded.Should().Be("hello"); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.BASE64DEC  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void B64Dec_empty() => StringUdf.UDF_STR_B64DEC("").Should().Be("");
        [Fact] public void B64Dec_null() => StringUdf.UDF_STR_B64DEC(null!).Should().BeNull();
        [Fact] public void B64Dec_error() => StringUdf.UDF_STR_B64DEC(ExcelError.NA).Should().Be(ExcelError.NA);

        // ══════════════════════════════════════════════════════════════════
        //  STR.UUID  (no args — returns string with dashes)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Uuid_not_null() => StringUdf.UDF_STR_UUID().Should().NotBeNull();
        [Fact] public void Uuid_has_dashes() => StringUdf.UDF_STR_UUID().ToString().Should().Contain("-");
        [Fact] public void Uuid_length_36() => StringUdf.UDF_STR_UUID().ToString().Should().HaveLength(36);
        [Fact] public void Uuid_unique() { var r1=StringUdf.UDF_STR_UUID(); var r2=StringUdf.UDF_STR_UUID(); r1.Should().NotBe(r2); }
        [Fact] public void Uuid_valid_hex() { var s=StringUdf.UDF_STR_UUID().ToString()!; System.Guid.Parse(s).Should().NotBeEmpty(); }

        // ══════════════════════════════════════════════════════════════════
        //  STR.RNDSTR  (len, charset)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Rnd_correct_length() => StringUdf.UDF_STR_RND(10, "").ToString().Should().HaveLength(10);
        [Fact] public void Rnd_zero_length() => StringUdf.UDF_STR_RND(0, "").ToString().Should().BeEmpty();
        [Fact] public void Rnd_only_from_charset() { var s=StringUdf.UDF_STR_RND(20, "AB").ToString()!; s.Should().MatchRegex("^[AB]+$"); }
        [Fact] public void Rnd_long_string() => StringUdf.UDF_STR_RND(1000, "x").ToString().Should().HaveLength(1000);
        [Fact] public void Rnd_null_charset() => StringUdf.UDF_STR_RND(10, null!).ToString().Should().HaveLength(10);

        // ══════════════════════════════════════════════════════════════════
        //  STR.RNDALPHA  (len — uses A-Za-z charset)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void RndA_length() => StringUdf.UDF_STR_RNDA(15).ToString().Should().HaveLength(15);
        [Fact] public void RndA_zero() => StringUdf.UDF_STR_RNDA(0).ToString().Should().BeEmpty();
        [Fact] public void RndA_alpha_only() { var s=StringUdf.UDF_STR_RNDA(50).ToString()!; s.Should().MatchRegex("^[A-Za-z]+$"); }
        [Fact] public void RndA_not_null() => StringUdf.UDF_STR_RNDA(10).Should().NotBeNull();
        [Fact] public void RndA_long() => StringUdf.UDF_STR_RNDA(500).ToString().Should().HaveLength(500);

        // ══════════════════════════════════════════════════════════════════
        //  STR.RNDNUM  (len — uses 0-9 charset)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void RndN_length() => StringUdf.UDF_STR_RNDN(8).ToString().Should().HaveLength(8);
        [Fact] public void RndN_zero() => StringUdf.UDF_STR_RNDN(0).ToString().Should().BeEmpty();
        [Fact] public void RndN_digits_only() { var s=StringUdf.UDF_STR_RNDN(30).ToString()!; s.Should().MatchRegex("^[0-9]+$"); }
        [Fact] public void RndN_not_null() => StringUdf.UDF_STR_RNDN(5).Should().NotBeNull();
        [Fact] public void RndN_long() => StringUdf.UDF_STR_RNDN(100).ToString().Should().HaveLength(100);

        // ══════════════════════════════════════════════════════════════════
        //  STR.ISNULLEMPTY  (MapOver<string,bool>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Isne_empty() => ((bool)StringUdf.UDF_STR_ISNE("")).Should().BeTrue();
        [Fact] public void Isne_whitespace() => ((bool)StringUdf.UDF_STR_ISNE("   ")).Should().BeFalse();
        [Fact] public void Isne_non_empty() => ((bool)StringUdf.UDF_STR_ISNE("hello")).Should().BeFalse();

        // ══════════════════════════════════════════════════════════════════
        //  STR.ISNULLWS  (MapOver<string,bool>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Isnw_empty() => ((bool)StringUdf.UDF_STR_ISNW("")).Should().BeTrue();
        [Fact] public void Isnw_whitespace() => ((bool)StringUdf.UDF_STR_ISNW("  \t \n  ")).Should().BeTrue();
        [Fact] public void Isnw_non_empty() => ((bool)StringUdf.UDF_STR_ISNW("hello")).Should().BeFalse();

        // ══════════════════════════════════════════════════════════════════
        //  STR.COALESCE  (MapOverMulti<string,string,string>)
        //  null → fallback, empty → empty (not coalesced)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Coal_primary_non_empty() => StringUdf.UDF_STR_COAL("hello", "world").Should().Be("hello");
        [Fact] public void Coal_primary_empty_not_coalesced() => StringUdf.UDF_STR_COAL("", "fallback").Should().Be("");
        [Fact] public void Coal_primary_whitespace() => StringUdf.UDF_STR_COAL("   ", "fallback").Should().Be("   ");

        // ══════════════════════════════════════════════════════════════════
        //  STR.FORMAT  (MapOverMulti<object,string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Fmt_invalid_format() => StringUdf.UDF_STR_FMT("hello", "XYZ").Should().Be("hello");
        [Fact] public void Fmt_empty_value() => StringUdf.UDF_STR_FMT("", "D4").Should().Be("");
        [Fact] public void Fmt_percentage() => ((string)StringUdf.UDF_STR_FMT("0.25", "P0")).Should().Contain("25");

        // ══════════════════════════════════════════════════════════════════
        //  STR.STRIPHTML  (MapOver<string,string>)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void SHtml_strip_tags() => StringUdf.UDF_STR_SHTML("<b>hello</b>").Should().Be("hello");
        [Fact] public void SHtml_nested_tags() => StringUdf.UDF_STR_SHTML("<div><span>text</span></div>").Should().Be("text");
        [Fact] public void SHtml_no_html() => StringUdf.UDF_STR_SHTML("plain text").Should().Be("plain text");
        [Fact] public void SHtml_empty() => StringUdf.UDF_STR_SHTML("").Should().Be("");
        [Fact] public void SHtml_null() => StringUdf.UDF_STR_SHTML(null!).Should().BeNull();
        [Fact] public void SHtml_error() => StringUdf.UDF_STR_SHTML(ExcelError.NA).Should().Be(ExcelError.NA);
        [Fact] public void SHtml_attributes() => StringUdf.UDF_STR_SHTML("<a href='x'>link</a>").Should().Be("link");
        [Fact] public void SHtml_array() { var r=(object[])StringUdf.UDF_STR_SHTML(new object[]{"<b>a</b>","<i>b</i>"}); r.Should().Equal("a","b"); }

        // ══════════════════════════════════════════════════════════════════
        // Edge-case / corrected tests
        // ══════════════════════════════════════════════════════════════════

        [Fact] public void Trunc_zero_length() => StringUdf.UDF_STR_TRUNC("hello", 0, "").Should().Be("");
        [Fact] public void Trunc_unicode() => StringUdf.UDF_STR_TRUNC("你好世界", 3, "").Should().Be("你好世");

        [Fact] public void Sw_null_first() => StringUdf.UDF_STR_SW(null!, "hello", false).Should().Be(ExcelEmpty.Value);
        [Fact] public void Ew_null_first() => StringUdf.UDF_STR_EW(null!, "world", false).Should().Be(ExcelEmpty.Value);
        [Fact] public void Cnt_null_first() => StringUdf.UDF_STR_CNT(null!, "a", false).Should().Be(ExcelEmpty.Value);
        [Fact] public void Cpfx_null_first() => StringUdf.UDF_STR_CPFX(null!, "hello", false).Should().Be(ExcelEmpty.Value);

        [Fact] public void Cpfx_case_insensitive() => StringUdf.UDF_STR_CPFX("Hello", "hello", false).Should().Be("Hello");

        [Fact] public void Rnd_negative_length() => StringUdf.UDF_STR_RND(-5, "").Should().Be(ExcelError.Value);

        [Fact] public void Lev_null_first() => StringUdf.UDF_STR_LEV(null!, "hello").Should().Be(ExcelEmpty.Value);
        [Fact] public void Lev_array() { var r=(object[])StringUdf.UDF_STR_LEV(new object[]{"kitten","cat"}, "sitten"); ((long)r[0]).Should().Be(1); ((long)r[1]).Should().Be(5); }

        [Fact] public void Join_null_delimiter() => StringUdf.UDF_STR_JOIN(null!, false, new object[]{"a","b","c"}).Should().Be("abc");

        [Fact] public void Isnw_null() => StringUdf.UDF_STR_ISNW(null!).Should().BeNull();
        [Fact] public void Isnw_array() { var r=(object[])StringUdf.UDF_STR_ISNW(new object[]{"","hello","  "}); ((bool)r[0]).Should().BeTrue(); ((bool)r[1]).Should().BeFalse(); ((bool)r[2]).Should().BeTrue(); }

        [Fact] public void Isne_null() => StringUdf.UDF_STR_ISNE(null!).Should().BeNull();
        [Fact] public void Isne_array() { var r=(object[])StringUdf.UDF_STR_ISNE(new object[]{"","hello",null}); ((bool)r[0]).Should().BeTrue(); ((bool)r[1]).Should().BeFalse(); r[2].Should().BeNull(); }

        // STR.FORMAT strings ignore numeric-only format specifiers (e.g. "D4" on string → passthrough).
        [Fact] public void Fmt_string_ignore_d4() => StringUdf.UDF_STR_FMT("42", "D4").Should().Be("42");
        [Fact] public void Fmt_null_value() => StringUdf.UDF_STR_FMT(null!, "D4").Should().Be(ExcelEmpty.Value);
        [Fact] public void Fmt_currency_passthrough() => StringUdf.UDF_STR_FMT("100", "C").Should().Be("100");
        [Fact] public void Fmt_array() { var r=(object[])StringUdf.UDF_STR_FMT(new object[]{"42","100"}, "D4"); ((string)r[0]).Should().Be("42"); ((string)r[1]).Should().Be("100"); }
        // Numeric format specifiers now work with actual numeric types (double/int)
        [Fact] public void Fmt_double_N2() => StringUdf.UDF_STR_FMT(123.456, "N2").Should().Be("123.46");
        [Fact] public void Fmt_double_P0() => StringUdf.UDF_STR_FMT(0.25, "P0").Should().Be("25%");
        [Fact] public void Fmt_double_C() => ((string)StringUdf.UDF_STR_FMT(1234.5, "C")).Should().Contain("1,234.50");
        [Fact] public void Fmt_int_D4() => StringUdf.UDF_STR_FMT(42, "D4").Should().Be("0042");
        [Fact] public void Fmt_composite_format() => StringUdf.UDF_STR_FMT("world", "{0} hello").Should().Be("world hello");
        [Fact] public void Fmt_double_incompatible_format() => StringUdf.UDF_STR_FMT(42.0, "D4").Should().Be("42");  // D4 is integral-only, falls back to ToString

        [Fact] public void Coal_primary_null() => StringUdf.UDF_STR_COAL(null!, "fallback").Should().Be(ExcelEmpty.Value);
        [Fact] public void Coal_both_null() => StringUdf.UDF_STR_COAL(null!, null!).Should().Be(ExcelEmpty.Value);
        [Fact] public void Coal_array() { var r=(object[])StringUdf.UDF_STR_COAL(new object[]{"a",null,"b"}, "fallback"); ((string)r[0]).Should().Be("a"); r[1].Should().BeNull(); ((string)r[2]).Should().Be("b"); }
    }
}
