using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Foundation.Tests;

public class ValuesEqualTests
{
    [Fact] public void Both_null_are_equal() => ComparisonUtils.ValuesEqual(null, null).Should().BeTrue();
    [Fact] public void Null_and_DBNull_are_equal() => ComparisonUtils.ValuesEqual(null, System.DBNull.Value).Should().BeTrue();
    [Fact] public void Null_and_value_are_not_equal() => ComparisonUtils.ValuesEqual(null, "hello").Should().BeFalse();
    [Fact] public void Value_and_null_are_not_equal() => ComparisonUtils.ValuesEqual(42, null).Should().BeFalse();
    [Fact] public void Both_empty_are_equal() => ComparisonUtils.ValuesEqual(ExcelEmpty.Value, ExcelEmpty.Value).Should().BeTrue();
    [Fact] public void Empty_and_null_are_not_equal() => ComparisonUtils.ValuesEqual(ExcelEmpty.Value, null).Should().BeFalse();
    [Fact] public void Empty_and_zero_are_not_equal() => ComparisonUtils.ValuesEqual(ExcelEmpty.Value, 0).Should().BeFalse();
    [Fact] public void Empty_and_empty_string_are_not_equal() => ComparisonUtils.ValuesEqual(ExcelEmpty.Value, "").Should().BeFalse();
    [Fact] public void Same_error_codes_are_equal() => ComparisonUtils.ValuesEqual(ExcelError.Value, ExcelError.Value).Should().BeTrue();
    [Fact] public void Different_error_codes_are_not_equal() => ComparisonUtils.ValuesEqual(ExcelError.Value, ExcelError.NA).Should().BeFalse();
    [Fact] public void Error_and_value_are_not_equal() => ComparisonUtils.ValuesEqual(ExcelError.Value, "#VALUE!").Should().BeFalse();
    [Fact] public void Boolean_true_and_numeric_minus_one_are_not_equal() => ComparisonUtils.ValuesEqual(true, -1).Should().BeFalse();
    [Fact] public void Both_true_are_equal() => ComparisonUtils.ValuesEqual(true, true).Should().BeTrue();
    [Fact] public void Same_integers_are_equal() => ComparisonUtils.ValuesEqual(42, 42).Should().BeTrue();
    [Fact] public void Int_and_double_same_value_are_equal() => ComparisonUtils.ValuesEqual(1, 1.0).Should().BeTrue();
    [Fact] public void Tiny_difference_within_epsilon_is_equal() => ComparisonUtils.ValuesEqual(1.0, 1.0 + 1e-13).Should().BeTrue();
    [Fact] public void Difference_exceeding_epsilon_is_not_equal() => ComparisonUtils.ValuesEqual(1.0, 1.0 + 1e-10, 1e-12).Should().BeFalse();
    [Fact] public void Same_strings_are_equal() => ComparisonUtils.ValuesEqual("hello", "hello").Should().BeTrue();
    [Fact] public void Case_sensitive_difference_is_not_equal() => ComparisonUtils.ValuesEqual("Hello", "hello").Should().BeFalse();
    [Fact] public void Same_dates_are_equal() => ComparisonUtils.ValuesEqual(new System.DateTime(2025, 1, 15), new System.DateTime(2025, 1, 15)).Should().BeTrue();
    [Fact] public void Different_dates_are_not_equal() => ComparisonUtils.ValuesEqual(new System.DateTime(2025, 1, 15), new System.DateTime(2025, 1, 16)).Should().BeFalse();
    [Fact] public void Very_close_doubles_within_default_epsilon() => ComparisonUtils.ValuesEqual(1.0, 1.0 + 1e-15).Should().BeTrue();
    [Fact] public void NaN_and_NaN_behavior() => ComparisonUtils.ValuesEqual(double.NaN, double.NaN).Should().BeFalse();
}

public class CompareTests
{
    [Fact] public void Null_sorts_before_empty() => ComparisonUtils.Compare(null, ExcelEmpty.Value).Should().Be(-1);
    [Fact] public void Empty_sorts_before_value() => ComparisonUtils.Compare(ExcelEmpty.Value, 0).Should().Be(-1);
    [Fact] public void Value_sorts_before_error() => ComparisonUtils.Compare("hello", ExcelError.Value).Should().Be(-1);
    [Fact] public void Two_nulls_are_equal() => ComparisonUtils.Compare(null, null).Should().Be(0);
    [Fact] public void Two_errors_are_equal() => ComparisonUtils.Compare(ExcelError.Value, ExcelError.Div0).Should().Be(0);
    [Fact] public void Three_less_than_five() => ComparisonUtils.Compare(3, 5).Should().Be(-1);
    [Fact] public void String_case_insensitive_compare() => ComparisonUtils.Compare("Apple", "banana").Should().Be(-1);
    [Fact] public void Compare_mixed_types_string_vs_number() => ComparisonUtils.Compare("hello", 42).Should().Be(1);
}

public class SafeKeyTests
{
    [Fact] public void Null_key() => ComparisonUtils.SafeKey(null).Should().Be("Null:##NULL##");
    [Fact] public void Empty_key() => ComparisonUtils.SafeKey(ExcelEmpty.Value).Should().Be("Empty:##EMPTY##");
    [Fact] public void Error_key() => ComparisonUtils.SafeKey(ExcelError.Value).Should().Be("Error:#ERR(2015)");
    [Fact] public void Boolean_true_key() => ComparisonUtils.SafeKey(true).Should().Be("Boolean:True");
    [Fact] public void Numeric_key() => ComparisonUtils.SafeKey(1.0).Should().StartWith("Numeric:");
    [Fact] public void String_key() => ComparisonUtils.SafeKey("hello").Should().Be("String:hello");
    [Fact] public void Date_key() => ComparisonUtils.SafeKey(new System.DateTime(2025, 6, 15, 10, 30, 0)).Should().Be("Date:2025-06-15 10:30:00");
    [Fact] public void SafeKey_null_element_in_1D_array() => ComparisonUtils.SafeKey(new object?[] { "a", null, "c" }).Should().Be("Array(3):String:a|Null:##NULL##|String:c");
    [Fact] public void SafeKey_empty_1D_array() => ComparisonUtils.SafeKey(System.Array.Empty<object>()).Should().Be("Array(0):##EMPTY##");
}
