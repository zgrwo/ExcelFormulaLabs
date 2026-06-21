using System;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Foundation.Tests;

public class ProbeTests
{
    [Fact] public void IsEmptyArray_null_false() => InputNormalizer.IsEmptyArray(null).Should().BeFalse();
    [Fact] public void IsEmptyArray_scalar_false() => InputNormalizer.IsEmptyArray(42).Should().BeFalse();
    [Fact] public void IsEmptyArray_empty_true() => InputNormalizer.IsEmptyArray(Array.Empty<object>()).Should().BeTrue();
    [Fact] public void IsEmptyArray_nonempty_false() => InputNormalizer.IsEmptyArray(new object[] { 1, 2, 3 }).Should().BeFalse();
    [Fact] public void ArrayDims_scalar_zero() => InputNormalizer.ArrayDims(42).Should().Be(0);
    [Fact] public void ArrayDims_1D_one() => InputNormalizer.ArrayDims(new object[] { 1, 2 }).Should().Be(1);
    [Fact] public void ArrayDims_2D_two() => InputNormalizer.ArrayDims(new object[2, 3]).Should().Be(2);
    [Fact] public void Is1D_true() => InputNormalizer.Is1D(new object[] { 1, 2 }).Should().BeTrue();
    [Fact] public void Is2D_true() => InputNormalizer.Is2D(new object[2, 3]).Should().BeTrue();
    [Fact] public void IsNumericCell_int_true() => InputNormalizer.IsNumericCell(42).Should().BeTrue();
    [Fact] public void IsNumericCell_bool_false() => InputNormalizer.IsNumericCell(true).Should().BeFalse();
    [Fact] public void IsNumericCell_error_false() => InputNormalizer.IsNumericCell(ExcelError.Value).Should().BeFalse();
    [Fact] public void IsNumericCell_empty_false() => InputNormalizer.IsNumericCell(ExcelEmpty.Value).Should().BeFalse();
    [Fact] public void IsNumericCell_null_false() => InputNormalizer.IsNumericCell(null).Should().BeFalse();
    [Fact] public void IsNumericCell_numeric_string_true() => InputNormalizer.IsNumericCell("3.14").Should().BeTrue();
}

public class CoercionTests
{
    [Fact] public void ToString_null_empty() => InputNormalizer.ToString(null).Should().Be("");
    [Fact] public void ToString_empty_empty() => InputNormalizer.ToString(ExcelEmpty.Value).Should().Be("");
    [Fact] public void ToString_error_empty() => InputNormalizer.ToString(ExcelError.Value).Should().Be("");
    [Fact] public void ToString_string_passthrough() => InputNormalizer.ToString("hello").Should().Be("hello");
    [Fact] public void ToDouble_null_nan() => InputNormalizer.ToDouble(null).Should().Be(double.NaN);
    [Fact] public void ToDouble_empty_nan() => InputNormalizer.ToDouble(ExcelEmpty.Value).Should().Be(double.NaN);
    [Fact] public void ToDouble_int_works() => InputNormalizer.ToDouble(42).Should().Be(42.0);
    [Fact] public void ToDouble_string_works() => InputNormalizer.ToDouble("3.14").Should().Be(3.14);
    [Fact] public void ToDouble_bad_string_nan() => InputNormalizer.ToDouble("hello").Should().Be(double.NaN);
    [Fact] public void ToLong_null_zero() => InputNormalizer.ToLong(null).Should().Be(0);
    [Fact] public void ToBool_null_false() => InputNormalizer.ToBool(null).Should().BeFalse();
    [Fact] public void ToBool_zero_false() => InputNormalizer.ToBool(0).Should().BeFalse();
    [Fact] public void ToBool_nonzero_true() => InputNormalizer.ToBool(1).Should().BeTrue();
    [Fact] public void ToDateTime_int() => InputNormalizer.ToDateTime(45000).Should().Be(DateTime.MinValue);
    [Fact] public void ToDateTime_double() => InputNormalizer.ToDateTime(45000.5).Should().Be(new DateTime(2023, 3, 15, 12, 0, 0));
    [Fact] public void ToDateTime_null() => InputNormalizer.ToDateTime(null).Should().Be(DateTime.MinValue);
    [Fact] public void ToDateTime_empty() => InputNormalizer.ToDateTime(ExcelEmpty.Value).Should().Be(DateTime.MinValue);
    [Fact] public void ToDateTime_error() => InputNormalizer.ToDateTime(ExcelError.Value).Should().Be(DateTime.MinValue);
    [Fact] public void ToDateTime_string_date() => InputNormalizer.ToDateTime("2024-01-01").Should().Be(new DateTime(2024, 1, 1));
    [Fact] public void ToBool_string_false() => InputNormalizer.ToBool("false").Should().BeFalse();
    [Fact] public void ToBool_string_zero() => InputNormalizer.ToBool("0").Should().BeFalse();
    [Fact] public void ToBool_string_true() => InputNormalizer.ToBool("true").Should().BeTrue();
    [Fact] public void ToBool_string_one() => InputNormalizer.ToBool("1").Should().BeTrue();
    [Fact] public void ToBool_string_yes_is_not_true() => InputNormalizer.ToBool("yes").Should().BeFalse();
    [Fact] public void ToLong_double_truncates() => InputNormalizer.ToLong(3.14).Should().Be(3);
    [Fact] public void ToLong_string() => InputNormalizer.ToLong("42").Should().Be(42);
    [Fact] public void ToDouble_bool_true_one() => InputNormalizer.ToDouble(true).Should().Be(1.0);
}

public class NormalizationTests
{
    [Fact] public void NormalizeTo1D_scalar_wraps()
    {
        InputNormalizer.NormalizeTo1D(42).Should().BeEquivalentTo(new object[] { 42 });
    }

    [Fact] public void NormalizeTo1D_1D_passthrough()
    {
        var input = new object[] { 1, 2, 3 };
        InputNormalizer.NormalizeTo1D(input).Should().BeSameAs(input);
    }

    [Fact] public void NormalizeTo1D_2D_row_major()
    {
        var input = new object[,] { { 1, 2 }, { 3, 4 } };
        InputNormalizer.NormalizeTo1D(input).Should().BeEquivalentTo(
            new object[] { 1, 2, 3, 4 }, o => o.WithStrictOrdering());
    }

    [Fact] public void NormalizeTo1D_2D_col_major()
    {
        var input = new object[,] { { 1, 2 }, { 3, 4 } };
        InputNormalizer.NormalizeTo1D(input, NormalizeOrder.ColumnMajor).Should().BeEquivalentTo(
            new object[] { 1, 3, 2, 4 }, o => o.WithStrictOrdering());
    }

    [Fact] public void NormalizeTo2D_scalar_1x1()
    {
        var result = InputNormalizer.NormalizeTo2D(42);
        result.Should().NotBeNull();
        result!.GetLength(0).Should().Be(1);
        result.GetLength(1).Should().Be(1);
        result[0, 0].Should().Be(42);
    }

    [Fact] public void NormalizeTo2D_1D_column_vector()
    {
        var result = InputNormalizer.NormalizeTo2D(new object[] { 10, 20, 30 });
        result.Should().NotBeNull();
        result!.GetLength(0).Should().Be(3);
        result.GetLength(1).Should().Be(1);
    }

    [Fact] public void ToDoubles_skips_non_numeric()
    {
        InputNormalizer.ToDoubles(new object?[] { 1, null, 2, ExcelEmpty.Value, 3 })
            .Should().BeEquivalentTo(new double[] { 1, 2, 3 });
    }

    [Fact] public void ToDoubles_all_bad_returns_empty()
        => InputNormalizer.ToDoubles(new object[] { "a", "b" }).Should().BeEmpty();

    [Fact] public void NormalizeTo2D_null_returns_null()
        => InputNormalizer.NormalizeTo2D(null).Should().BeNull();

    [Fact] public void NormalizeTo1D_large_array()
    {
        var input = new object[500];
        for (int i = 0; i < 500; i++) input[i] = i;
        var result = InputNormalizer.NormalizeTo1D(input);
        result.Should().NotBeNull();
        result!.Length.Should().Be(500);
    }

    [Fact] public void ToDoubles_mixed_extracts_numeric()
    {
        var result = InputNormalizer.ToDoubles(new object?[] { 1.0, "text", 2.5, null, 3 });
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(new double[] { 1.0, 2.5, 3.0 });
    }
}
