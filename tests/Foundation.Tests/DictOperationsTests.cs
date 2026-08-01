using System;
using System.Collections.Generic;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Foundation.Tests;

public class DictCreateTests
{
    [Fact] public void Case_insensitive_default()
    {
        var dict = DictOperations.Create();
        dict["Key"] = 1;
        dict["key"] = 2;
        dict.Count.Should().Be(1);
    }

    [Fact] public void Case_sensitive_when_requested()
    {
        var dict = DictOperations.Create(StringComparison.Ordinal);
        dict["Key"] = 1;
        dict["key"] = 2;
        dict.Count.Should().Be(2);
    }
}

public class FromKeysTests
{
    [Fact] public void Basic_from_keys()
    {
        var dict = DictOperations.FromKeys(new object[] { "a", "b", "c" }, 0);
        dict.Count.Should().Be(3);
        dict["a"].Should().Be(0);
    }

    [Fact] public void Skips_invalid_keys()
    {
        var dict = DictOperations.FromKeys(new object[] { "a", ExcelError.Value, null!, "b" });
        dict.Count.Should().Be(2);
    }

    [Fact] public void Duplicate_first_wins()
    {
        DictOperations.FromKeys(new object[] { "dup", "dup" }, "first")["dup"].Should().Be("first");
    }

    [Fact] public void Empty_array_returns_empty()
        => DictOperations.FromKeys(Array.Empty<object>()).Count.Should().Be(0);

    [Fact] public void All_invalid_keys_returns_empty()
        => DictOperations.FromKeys(new object[] { ExcelError.Value, null!, ExcelError.NA }).Count.Should().Be(0);
}

public class ToArrayTests
{
    [Fact] public void Exports_2D()
    {
        var dict = new Dictionary<string, object> { ["k1"] = "v1", ["k2"] = 42 };
        var result = DictOperations.ToArray(dict);
        result.Should().NotBeNull();
        result!.GetLength(0).Should().Be(2);
        result.GetLength(1).Should().Be(2);
    }

    [Fact] public void Null_returns_null() => DictOperations.ToArray(null).Should().BeNull();
    [Fact] public void Empty_returns_null() => DictOperations.ToArray(new Dictionary<string, object>()).Should().BeNull();

    [Fact] public void Single_entry_returns_1x2_array()
    {
        var dict = new Dictionary<string, object> { ["key"] = "value" };
        var result = DictOperations.ToArray(dict);
        result.Should().NotBeNull();
        result!.GetLength(0).Should().Be(1);
        result.GetLength(1).Should().Be(2);
    }
}

public class MergeTests
{
    [Fact] public void No_overwrite_first_wins()
    {
        var a = new Dictionary<string, object> { ["key"] = "A" };
        var b = new Dictionary<string, object> { ["key"] = "B", ["extra"] = "B2" };
        var merged = DictOperations.Merge(a, b);
        merged["key"].Should().Be("A");
        merged["extra"].Should().Be("B2");
    }

    [Fact] public void Overwrite_second_wins()
    {
        var a = new Dictionary<string, object> { ["key"] = "A" };
        var b = new Dictionary<string, object> { ["key"] = "B" };
        DictOperations.Merge(a, b, overwrite: true)["key"].Should().Be("B");
    }

    [Fact] public void Both_null_returns_empty()
        => DictOperations.Merge(null, null).Count.Should().Be(0);

    [Fact] public void Merge_explicit_no_overwrite_first_wins()
    {
        var a = new Dictionary<string, object> { ["key"] = "first" };
        var b = new Dictionary<string, object> { ["key"] = "second" };
        DictOperations.Merge(a, b, overwrite: false)["key"].Should().Be("first");
    }

    [Fact] public void Merge_one_null_dict_preserves_other()
    {
        var dict = new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 };
        var merged = DictOperations.Merge(dict, null);
        merged.Count.Should().Be(2);
        merged["a"].Should().Be(1);
        merged["b"].Should().Be(2);
    }
}
