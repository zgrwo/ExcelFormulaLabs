using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.Foundation.Tests;

public class MapOverTests
{
    [Fact] public void Scalar_to_scalar()
        => ElementWiseMapper.MapOver<string, string>("hello", s => s.ToUpper()).Should().Be("HELLO");

    [Fact] public void Scalar_int_mapped()
        => ElementWiseMapper.MapOver(5, (int x) => x * 2).Should().Be(10);

    [Fact] public void Array1D_preserves_shape()
    {
        var result = ElementWiseMapper.MapOver(
            new object[] { "a", "b", "c" }, (string s) => s.ToUpper());
        result.Should().BeEquivalentTo(new object[] { "A", "B", "C" }, o => o.WithStrictOrdering());
    }

    [Fact] public void Array2D_preserves_shape()
    {
        var input = new object[,] { { "a", "b" }, { "c", "d" } };
        var result = ElementWiseMapper.MapOver(input, (string s) => s.ToUpper());
        result.Should().BeOfType<object[,]>();
        var arr = (object[,])result;
        arr.GetLength(0).Should().Be(2);
        arr.GetLength(1).Should().Be(2);
        arr[0, 0].Should().Be("A");
        arr[1, 1].Should().Be("D");
    }

    [Fact] public void Error_passthrough()
    {
        var input = new object[] { "ok", ExcelError.Value, "also" };
        var result = (object[])ElementWiseMapper.MapOver(input, (string s) => s.ToUpper());
        result[1].Should().Be(ExcelError.Value);
        result[0].Should().Be("OK");
    }

    [Fact] public void Empty_passthrough()
        => ElementWiseMapper.MapOver(ExcelEmpty.Value, (string s) => s.ToUpper())
            .Should().Be(ExcelEmpty.Value);

    [Fact] public void Null_passthrough()
    {
        var input = new object?[] { null, "text" };
        var result = (object[])ElementWiseMapper.MapOver(
            input, (string s) => s?.ToUpper() ?? "");
        result[0].Should().BeNull();
        result[1].Should().Be("TEXT");
    }

    [Fact] public void MapOverFlat_2D_to_1D()
    {
        var input = new object[,] { { 1, 2 }, { 3, 4 } };
        ElementWiseMapper.MapOverFlat(input, (double x) => x * 10)
            .Should().BeEquivalentTo(new object[] { 10.0, 20.0, 30.0, 40.0 });
    }

    [Fact] public void Multi_broadcast_scalar_to_array()
    {
        var input = new object[] { "a", "b", "c" };
        var result = (object[])ElementWiseMapper.MapOverMulti(
            input, "!", (string s, string suffix) => s + suffix);
        result.Should().BeEquivalentTo(new object[] { "a!", "b!", "c!" });
    }

    [Fact] public void Multi_mismatched_sizes_error()
        => ElementWiseMapper.MapOverMulti(
            new object[] { 1, 2, 3 }, new object[] { 1, 2 }, (int a, int b) => a + b)
            .Should().Be(ExcelError.Value);

    [Fact] public void MapOver_large_1D_array()
    {
        var input = new object[10000];
        for (int i = 0; i < 10000; i++) input[i] = i;
        var result = (object[])ElementWiseMapper.MapOver(input, (int x) => x * 2);
        result.Length.Should().Be(10000);
    }

    [Fact] public void MapOverFlat_scalar_returns_1D_single()
    {
        var result = ElementWiseMapper.MapOverFlat(42, (int x) => x * 2);
        result.Should().BeAssignableTo<object[]>();
        ((object[])result).Length.Should().Be(1);
    }

    [Fact] public void MapOverMulti_both_null_elements()
    {
        var input = new object?[] { null, null };
        var result = (object[])ElementWiseMapper.MapOverMulti(
            input, input, (string? a, string? b) => (a ?? "") + (b ?? ""));
        result.Length.Should().Be(2);
    }
}

public class MapOverMultiThreeArgTests
{
    [Fact] public void Three_scalars()
        => ElementWiseMapper.MapOverMulti("a", "b", "c",
            (string x, string y, string z) => x + y + z).Should().Be("abc");

    [Fact] public void Three_with_broadcasting()
    {
        var input = new object[] { "1", "2" };
        var result = (object[])ElementWiseMapper.MapOverMulti(
            input, "+", "0", (string n, string op, string suffix) => n + op + suffix);
        result.Should().BeEquivalentTo(new object[] { "1+0", "2+0" });
    }

    [Fact] public void Three_mismatched_sizes_error()
        => ElementWiseMapper.MapOverMulti(
            new object[] { 1, 2, 3 }, new object[] { 1, 2 }, new object[] { 1 },
            (int a, int b, int c) => a + b + c)
            .Should().Be(ExcelError.Value);
}
