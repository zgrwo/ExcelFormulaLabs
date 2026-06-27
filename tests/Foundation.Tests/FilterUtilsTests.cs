using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Foundation.Tests;

public class FilterPassesTests
{
    [Fact] public void Isblank_null_true() => FilterUtils.FilterPasses(null, null, "isblank").Should().BeTrue();
    [Fact] public void Isblank_empty_true() => FilterUtils.FilterPasses(ExcelEmpty.Value, null, "isblank").Should().BeTrue();
    [Fact] public void Isblank_whitespace_true() => FilterUtils.FilterPasses("   ", null, "isblank").Should().BeTrue();
    [Fact] public void Isblank_nonblank_false() => FilterUtils.FilterPasses("hello", null, "isblank").Should().BeFalse();
    [Fact] public void Isnotblank_nonblank_true() => FilterUtils.FilterPasses("hello", null, "isnotblank").Should().BeTrue();

    [Fact] public void Equals_operator() => FilterUtils.FilterPasses(42, 42, "=").Should().BeTrue();
    [Fact] public void NotEquals_operator() => FilterUtils.FilterPasses(42, 99, "<>").Should().BeTrue();
    [Fact] public void Less_than() => FilterUtils.FilterPasses(3, 5, "<").Should().BeTrue();
    [Fact] public void Less_equal() => FilterUtils.FilterPasses(5, 5, "<=").Should().BeTrue();
    [Fact] public void Greater_than() => FilterUtils.FilterPasses(10, 5, ">").Should().BeTrue();
    [Fact] public void Greater_equal() => FilterUtils.FilterPasses(10, 10, ">=").Should().BeTrue();

    [Fact] public void Contains_true() => FilterUtils.FilterPasses("Hello World", "world", "contains").Should().BeTrue();
    [Fact] public void NotContains_true() => FilterUtils.FilterPasses("Hello", "xyz", "notcontains").Should().BeTrue();
    [Fact] public void StartsWith_true() => FilterUtils.FilterPasses("Hello World", "hello", "startswith").Should().BeTrue();
    [Fact] public void EndsWith_true() => FilterUtils.FilterPasses("Hello World", "world", "endswith").Should().BeTrue();

    [Fact] public void Regex_match() => FilterUtils.FilterPasses("abc123", @"\d+", "regex").Should().BeTrue();
    [Fact] public void Regex_nomatch() => FilterUtils.FilterPasses("abc", @"\d+", "regex").Should().BeFalse();

    [Fact] public void Error_element_false() => FilterUtils.FilterPasses(ExcelError.Value, "test", "=").Should().BeFalse();
    [Fact] public void Null_element_false() => FilterUtils.FilterPasses(null, "test", "=").Should().BeFalse();
    [Fact] public void Array_element_false() => FilterUtils.FilterPasses(new object[0], "test", "=").Should().BeFalse();
    [Fact] public void Equals_with_null_matchValue() => FilterUtils.FilterPasses("hello", null, "=").Should().BeFalse();
    [Fact] public void Equals_empty_string_element() => FilterUtils.FilterPasses("", "", "=").Should().BeTrue();
    [Fact] public void Regex_invalid_pattern_returns_false() => FilterUtils.FilterPasses("test", "[invalid", "regex").Should().BeFalse();
    [Fact] public void DBNull_value() => FilterUtils.FilterPasses(System.DBNull.Value, "test", "=").Should().BeFalse();
    [Fact] public void Numeric_string_eq() => FilterUtils.FilterPasses("3.14", "3.14", "=").Should().BeTrue();
    [Fact] public void Numeric_int_vs_string_eq() => FilterUtils.FilterPasses(42, "42", "=").Should().BeTrue();
    [Fact] public void Numeric_gt_string() => FilterUtils.FilterPasses("10", "5", ">").Should().BeTrue();
    [Fact] public void Contains_case_insensitive() => FilterUtils.FilterPasses("HELLO", "hello", "contains").Should().BeTrue();
    // IEEE 754 NaN guards — NaN is unordered, all ordered comparisons must reject it
    [Fact] public void NaN_lt_false() => FilterUtils.FilterPasses(double.NaN, 5.0, "<").Should().BeFalse();
    [Fact] public void NaN_le_false() => FilterUtils.FilterPasses(double.NaN, 5.0, "<=").Should().BeFalse();
    [Fact] public void NaN_gt_false() => FilterUtils.FilterPasses(double.NaN, 5.0, ">").Should().BeFalse();
    [Fact] public void NaN_ge_false() => FilterUtils.FilterPasses(double.NaN, 5.0, ">=").Should().BeFalse();
    [Fact] public void Finite_gt_NaN_false() => FilterUtils.FilterPasses(5.0, double.NaN, ">").Should().BeFalse();
    [Fact] public void Finite_lt_NaN_false() => FilterUtils.FilterPasses(5.0, double.NaN, "<").Should().BeFalse();
    [Fact] public void NaN_eq_NaN_true() => FilterUtils.FilterPasses(double.NaN, double.NaN, "=").Should().BeTrue();
    [Fact] public void NaN_ne_NaN_false() => FilterUtils.FilterPasses(double.NaN, double.NaN, "<>").Should().BeFalse();
}
