using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.Foundation.Tests;

public class SortTests
{
    [Fact] public void Sort_ascending()
    {
        var arr = new[] { 3, 1, 4, 1, 5 };
        ArrayOperations.Sort(arr);
        arr.Should().BeInAscendingOrder();
    }

    [Fact] public void Sort_descending()
    {
        var arr = new[] { 3, 1, 4, 1, 5 };
        ArrayOperations.Sort(arr, ascending: false);
        arr.Should().BeInDescendingOrder();
    }

    [Fact] public void Sort_numeric_mode_non_numeric_sinks()
    {
        var input = new object[] { 3, "hello", 1, "world" };
        ArrayOperations.Sort(input, ascending: true, ComparerMode.Numeric);
        input[0].Should().Be(1);
        input[1].Should().Be(3);
    }

    [Fact] public void Sorted_returns_new_copy()
    {
        var original = new[] { 3, 1, 2 };
        var sorted = ArrayOperations.Sorted(original);
        sorted.Should().BeInAscendingOrder();
        original.Should().Equal(new[] { 3, 1, 2 });
    }

    [Fact] public void Sort_empty_noop() => ArrayOperations.Sort(System.Array.Empty<int>());
    [Fact] public void Sort_null_noop() { int[]? n = null; ArrayOperations.Sort(n!); }
}

public class SliceTests
{
    [Fact] public void Slice_middle()
        => ArrayOperations.Slice(new[] { 1, 2, 3, 4, 5 }, 1, 3).Should().Equal(2, 3, 4);

    [Fact] public void Slice_negative_start()
        => ArrayOperations.Slice(new[] { 1, 2, 3, 4, 5 }, -2).Should().Equal(4, 5);

    [Fact] public void Slice_clamped_length()
        => ArrayOperations.Slice(new[] { 1, 2, 3 }, 1, 10).Should().Equal(2, 3);

    [Fact] public void Slice_invalid_returns_empty()
        => ArrayOperations.Slice(new[] { 1, 2 }, 5).Should().BeEmpty();

    [Fact] public void Slice_length_exceeding_bounds_clamped()
        => ArrayOperations.Slice(new[] { 1, 2, 3 }, 1, 100).Should().Equal(2, 3);

    [Fact] public void Slice_start_beyond_array_returns_empty()
        => ArrayOperations.Slice(new[] { 1, 2, 3 }, 10).Should().BeEmpty();

    [Fact] public void Slice_negative_length_full_copy()
        => ArrayOperations.Slice(new[] { 1, 2, 3 }, 0, -1).Should().Equal(1, 2, 3);
}

public class IndexOfTests
{
    [Fact] public void Found_index()
        => ArrayOperations.IndexOf(new[] { 10, 20, 30 }, 20).Should().Be(1);

    [Fact] public void NotFound_minus_one()
        => ArrayOperations.IndexOf(new[] { 10, 20, 30 }, 99).Should().Be(-1);

    [Fact] public void Double_within_tolerance()
        => ArrayOperations.IndexOf(new[] { 1.0, 2.0 }, 2.0000000000001, 1e-10).Should().Be(1);
}

public class FlattenTests
{
    [Fact] public void Row_major()
    {
        var input = new int[,] { { 1, 2 }, { 3, 4 } };
        ArrayOperations.Flatten(input).Should().Equal(1, 2, 3, 4);
    }

    [Fact] public void Col_major()
    {
        var input = new int[,] { { 1, 2 }, { 3, 4 } };
        ArrayOperations.Flatten(input, NormalizeOrder.ColumnMajor).Should().Equal(1, 3, 2, 4);
    }

    [Fact] public void Flatten_1x1_single_element()
        => ArrayOperations.Flatten(new int[,] { { 42 } }).Should().Equal(42);

    [Fact] public void Flatten_3x1_column_vector()
        => ArrayOperations.Flatten(new int[,] { { 1 }, { 2 }, { 3 } }).Should().Equal(1, 2, 3);
}

public class CollectNumericColumnsTests
{
    [Fact] public void Finds_numeric_columns()
    {
        var data = new object[,] { { "Name", "Age", "Score" }, { "Alice", 30, 85 }, { "Bob", 25, 92 } };
        var cols = ArrayOperations.CollectNumericColumns(data, 3, 3, out var names);
        cols.Should().Equal(1, 2);
        names[0].Should().Be("Name");
    }

    [Fact] public void All_text_columns_returns_empty()
    {
        var data = new object[,] { { "A", "B" }, { "x", "y" } };
        var cols = ArrayOperations.CollectNumericColumns(data, 2, 2, out var names);
        cols.Should().BeEmpty();
    }

    [Fact] public void Mixed_with_no_header()
    {
        var data = new object[,] { { 1, "text", 3.0 }, { 4, "more", 6.0 } };
        var cols = ArrayOperations.CollectNumericColumns(data, 2, 3, out var names, hasHeader: false);
        cols.Should().Equal(0, 2);
    }
}

public class SortIndicesTests
{
    [Fact] public void SortIndices_ascending()
    {
        var values = new[] { 3, 1, 2 };
        var indices = new[] { 0, 1, 2 };
        ArrayOperations.SortIndices(values, indices);
        indices.Should().Equal(1, 2, 0);
    }

    [Fact] public void SortIndices_descending()
    {
        var values = new[] { 3, 1, 2 };
        var indices = new[] { 0, 1, 2 };
        ArrayOperations.SortIndices(values, indices, ascending: false);
        indices.Should().Equal(0, 2, 1);
    }

    [Fact] public void SortIndices_with_duplicates()
    {
        var values = new[] { 2, 1, 2 };
        var indices = new[] { 0, 1, 2 };
        ArrayOperations.SortIndices(values, indices);
        indices[0].Should().Be(1);
        indices[1].Should().BeOneOf(0, 2);
        indices[2].Should().BeOneOf(0, 2);
    }

    [Fact] public void SortIndices_empty_no_throw()
        => ArrayOperations.SortIndices(System.Array.Empty<int>(), System.Array.Empty<int>());

    [Fact] public void SortIndices_single_element()
    {
        var values = new[] { 42 };
        var indices = new[] { 0 };
        ArrayOperations.SortIndices(values, indices);
        indices.Should().Equal(0);
    }
}
